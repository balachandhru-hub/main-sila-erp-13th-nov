using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeleteSilaInactiveRecipe
{
    /// <summary>
    /// Permanently removes an INACTIVE recipe: ingredients, outlet prices and the recipe row itself.
    /// Active / draft / pending recipes are refused — deactivate (or soft-delete a draft) first.
    /// </summary>
    public class DeleteSilaInactiveRecipeCommandHandler : IRequestHandler<DeleteSilaInactiveRecipeCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public DeleteSilaInactiveRecipeCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<Unit> Handle(DeleteSilaInactiveRecipeCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Permanently deleting inactive recipe. RecipeId: {request.RecipeId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Recipe recipe = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, request.RecipeId);
            if (recipe.Status != Common.SILA_RECIPE_INACTIVE)
            {
                _logger.LogError($"Recipe is not inactive. RecipeId: {recipe.Id}, Status: {recipe.Status}");
                throw new BadRequestCustomException(
                    "Only an inactive recipe can be deleted permanently.",
                    recipe.Status == Common.SILA_RECIPE_DRAFT
                        ? "Delete the draft first, or deactivate the recipe, then delete it permanently."
                        : "Deactivate the recipe first, then delete it permanently.");
            }

            string? usedIn = await _repository.RecipeIngredient
                .FindByCondition(x => x.IsActive
                    && x.SubRecipeId == recipe.Id
                    && x.Recipe.IsActive
                    && x.Recipe.Id != recipe.Id)
                .Select(x => x.Recipe.RecipeCode + " " + x.Recipe.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (usedIn != null)
            {
                _logger.LogError($"Inactive recipe is still used as a sub-recipe. RecipeId: {recipe.Id}, UsedIn: {usedIn}");
                throw new BadRequestCustomException("Recipe is used in a batch.", $"Remove it from {usedIn} first.");
            }

            List<RecipeIngredient> ingredients = await _repository.RecipeIngredient
                .FindByCondition(x => x.RecipeId == recipe.Id)
                .ToListAsync(cancellationToken);
            if (ingredients.Count > 0)
            {
                _repository.RecipeIngredient.DeleteRange(ingredients);
            }

            List<RecipeOutletPrice> prices = await _repository.RecipeOutletPrice
                .FindByCondition(x => x.RecipeId == recipe.Id)
                .ToListAsync(cancellationToken);
            if (prices.Count > 0)
            {
                _repository.RecipeOutletPrice.DeleteRange(prices);
            }

            List<RecipeSubstitutionProposal> proposals = await _repository.RecipeSubstitutionProposal
                .FindByCondition(x => x.RecipeId == recipe.Id)
                .ToListAsync(cancellationToken);
            if (proposals.Count > 0)
            {
                _repository.RecipeSubstitutionProposal.DeleteRange(proposals);
            }

            List<RecipeLegacyImage> legacyImages = await _repository.RecipeLegacyImage
                .FindByCondition(x => x.RecipeId == recipe.Id)
                .ToListAsync(cancellationToken);
            foreach (RecipeLegacyImage image in legacyImages)
            {
                image.RecipeId = null;
            }

            if (legacyImages.Count > 0)
            {
                _repository.RecipeLegacyImage.UpdateRange(legacyImages);
            }

            List<RecipeLegacyRecipe> legacyRecipes = await _repository.RecipeLegacyRecipe
                .FindByCondition(x => x.CreatedRecipeId == recipe.Id)
                .ToListAsync(cancellationToken);
            foreach (RecipeLegacyRecipe legacy in legacyRecipes)
            {
                legacy.CreatedRecipeId = null;
            }

            if (legacyRecipes.Count > 0)
            {
                _repository.RecipeLegacyRecipe.UpdateRange(legacyRecipes);
            }

            string? basePath = _configuration[Common.BASE_FOLDER_PATH];
            if (!string.IsNullOrWhiteSpace(basePath))
            {
                SilaRecipePictureRules.ClearSlot(basePath, buyer.Id, recipe.Id, SilaRecipePictureRules.SLOT_ACTIVE);
                SilaRecipePictureRules.ClearSlot(basePath, buyer.Id, recipe.Id, SilaRecipePictureRules.SLOT_DRAFT);
            }

            string code = recipe.RecipeCode;
            _repository.Recipe.Delete(recipe);
            new InventoryLedger(_repository, buyer.Id, request.UserId)
                .AddEvent(Common.SILA_REF_RECIPE, request.RecipeId, SilaRecipeRules.EVENT_PERMANENTLY_DELETED, $"Inactive recipe {code} permanently deleted.");
            await _repository.SaveAsync();

            _logger.LogInfo($"Inactive recipe permanently deleted. RecipeId: {request.RecipeId}, RecipeCode: {code}, Ingredients: {ingredients.Count}, Prices: {prices.Count}");
            return Unit.Value;
        }
    }
}
