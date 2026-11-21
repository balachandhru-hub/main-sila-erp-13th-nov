using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.RemoveSilaRecipePicture
{
    public class RemoveSilaRecipePictureCommandHandler : IRequestHandler<RemoveSilaRecipePictureCommand, SilaRecipePictureDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public RemoveSilaRecipePictureCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SilaRecipePictureDto> Handle(RemoveSilaRecipePictureCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Removing recipe picture. RecipeId: {request.RecipeId}, UserId: {request.UserId}");

            string basePath = SilaRecipePictureRules.RequireBasePath(_configuration, _logger);
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Recipe recipe = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, request.RecipeId);

            if (recipe.Status == Common.SILA_RECIPE_PENDING_APPROVAL
                || recipe.Status == Common.SILA_RECIPE_INACTIVE
                || recipe.Status == Common.SILA_RECIPE_APPROVED
                || recipe.Status == Common.SILA_RECIPE_REJECTED)
            {
                throw new BadRequestCustomException(
                    "Picture cannot be removed in this status.",
                    recipe.Status == Common.SILA_RECIPE_APPROVED || recipe.Status == Common.SILA_RECIPE_REJECTED
                        ? "Create a Draft revision before removing the picture."
                        : "This recipe cannot be edited in its current status.");
            }

            string slot = SilaRecipePictureRules.PreferDraftSlot(recipe)
                ? SilaRecipePictureRules.SLOT_DRAFT
                : SilaRecipePictureRules.SLOT_ACTIVE;

            if (slot == SilaRecipePictureRules.SLOT_DRAFT
                && SilaRecipePictureRules.FindSlotFile(basePath, buyer.Id, recipe.Id, SilaRecipePictureRules.SLOT_DRAFT) == null
                && SilaRecipePictureRules.FindSlotFile(basePath, buyer.Id, recipe.Id, SilaRecipePictureRules.SLOT_ACTIVE) != null)
            {
                // Draft revision with only an active picture: seed then clear draft so active (selling) stays.
                SilaRecipePictureRules.SeedDraftFromActive(basePath, buyer.Id, recipe.Id, _logger);
            }

            SilaRecipePictureRules.ClearSlot(basePath, buyer.Id, recipe.Id, slot);
            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_UPDATED, $"Picture removed ({slot}).");
            await _repository.SaveAsync();

            bool hasPicture = SilaRecipePictureRules.ResolveStudioFile(
                basePath, buyer.Id, recipe, SilaRecipePictureRules.PreferDraftSlot(recipe)) != null;
            _logger.LogInfo($"Recipe picture removed. RecipeId: {recipe.Id}, Slot: {slot}, HasPicture: {hasPicture}");
            return SilaRecipePictureRules.MapInfo(hasPicture, hasPicture ? slot : null);
        }
    }
}
