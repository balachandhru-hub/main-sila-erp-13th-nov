using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeleteSilaRecipeDraft
{
    /// <summary>
    /// Safe Draft deletion:
    /// - Never-approved DRAFT (ActiveVersion = 0): soft-delete via INACTIVE (same archive semantics as deactivate).
    /// - Draft revision of an approved recipe (ActiveVersion &gt; 0): discard the pending version only; restore APPROVED
    ///   and keep the selling ActiveVersion unchanged.
    /// PENDING_APPROVAL, APPROVED, REJECTED and INACTIVE are refused.
    /// </summary>
    public class DeleteSilaRecipeDraftCommandHandler : IRequestHandler<DeleteSilaRecipeDraftCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public DeleteSilaRecipeDraftCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<Unit> Handle(DeleteSilaRecipeDraftCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting recipe draft. RecipeId: {request.RecipeId}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Recipe recipe = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, request.RecipeId);

            if (recipe.Status != Common.SILA_RECIPE_DRAFT)
            {
                _logger.LogError($"Recipe is not a deletable draft. RecipeId: {recipe.Id}, Status: {recipe.Status}");
                throw new BadRequestCustomException(
                    "Only a Draft recipe can be deleted.",
                    recipe.Status == Common.SILA_RECIPE_PENDING_APPROVAL
                        ? "A recipe waiting for approval cannot be deleted."
                        : recipe.Status == Common.SILA_RECIPE_APPROVED
                            ? "An approved recipe cannot be deleted. Create a Draft revision to change it."
                            : $"Status {recipe.Status} does not allow Draft deletion.");
            }

            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            string? basePath = _configuration[Common.BASE_FOLDER_PATH];

            if (recipe.ActiveVersion > 0)
            {
                // Discard pending revision only — approved selling version stays.
                int discardedVersion = recipe.Version;
                await SilaRecipeRules.RetireVersionRowsAsync(_repository, recipe.Id, discardedVersion, cancellationToken);
                SilaRecipeRules.ClearDraftHeader(recipe);
                if (!string.IsNullOrWhiteSpace(basePath))
                {
                    SilaRecipePictureRules.ClearDraft(basePath, buyer.Id, recipe.Id);
                }

                recipe.Version = recipe.ActiveVersion;
                recipe.Status = Common.SILA_RECIPE_APPROVED;
                recipe.SubmittedBy = null;
                recipe.SubmittedOn = null;
                ledger.AddEvent(
                    Common.SILA_REF_RECIPE,
                    recipe.Id,
                    SilaRecipeRules.EVENT_DRAFT_DELETED,
                    $"Draft version {discardedVersion} deleted. Approved version {recipe.ActiveVersion} unchanged.");
                await _repository.SaveAsync();
                _logger.LogInfo($"Draft revision discarded. RecipeId: {recipe.Id}, RestoredVersion: {recipe.ActiveVersion}");
                return Unit.Value;
            }

            // Never-approved draft: soft-delete (INACTIVE), same archive path as deactivate — no hard delete.
            string? usedIn = await _repository.RecipeIngredient
                .FindByCondition(x => x.IsActive
                    && x.SubRecipeId == recipe.Id
                    && x.Recipe.IsActive
                    && x.Recipe.Status != Common.SILA_RECIPE_INACTIVE
                    && (x.Version == x.Recipe.ActiveVersion || x.Version == x.Recipe.Version))
                .Select(x => x.Recipe.RecipeCode + " " + x.Recipe.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (usedIn != null)
            {
                throw new BadRequestCustomException("Recipe is used in a batch.", $"Remove it from {usedIn} first.");
            }

            // Soft-delete (INACTIVE): retain managed picture files like deactivate — no Material/inventory/media hard delete.
            recipe.Status = Common.SILA_RECIPE_INACTIVE;
            SilaRecipeRules.ClearDraftHeader(recipe);

            ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_DRAFT_DELETED, "Draft recipe soft-deleted (INACTIVE).");
            await _repository.SaveAsync();
            _logger.LogInfo($"Never-approved draft soft-deleted. RecipeId: {recipe.Id}");
            return Unit.Value;
        }
    }
}
