using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SetSilaRecipePicture
{
    /// <summary>
    /// Uploads or replaces a recipe picture through managed file storage (FolderPath:BasePath).
    /// Approved recipes must start a Draft revision first; uploads then write the draft slot only.
    /// </summary>
    public class SetSilaRecipePictureCommandHandler : IRequestHandler<SetSilaRecipePictureCommand, SilaRecipePictureDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public SetSilaRecipePictureCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SilaRecipePictureDto> Handle(SetSilaRecipePictureCommand request, CancellationToken cancellationToken)
        {
            SilaRecipePictureUploadDto file = request.Request;
            _logger.LogInfo($"Setting recipe picture. RecipeId: {request.RecipeId}, Bytes: {file.Content.Length}, UserId: {request.UserId}");

            if (file.Content.Length == 0)
            {
                throw new BadRequestCustomException("Picture is empty.", "Choose a JPEG, PNG or WebP image.");
            }

            if (file.Content.Length > SilaRecipePictureRules.MAX_PICTURE_BYTES)
            {
                throw new BadRequestCustomException("Picture is too large.", "Upload a recipe picture of at most 5 MB.");
            }

            string? extension = SilaRecipePictureRules.DetectExtension(file.Content);
            if (extension == null)
            {
                throw new BadRequestCustomException("File type not accepted.", "Upload the picture as a JPEG, PNG or WebP image. SVG and executable files are not allowed.");
            }

            string basePath = SilaRecipePictureRules.RequireBasePath(_configuration, _logger);
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Recipe recipe = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, request.RecipeId);

            if (recipe.Status == Common.SILA_RECIPE_PENDING_APPROVAL)
            {
                throw new BadRequestCustomException("Recipe is waiting for approval.", "Pictures cannot be changed while the recipe is pending approval.");
            }

            if (recipe.Status == Common.SILA_RECIPE_INACTIVE)
            {
                throw new BadRequestCustomException("Recipe is inactive.", "An inactive recipe cannot be edited.");
            }

            if (recipe.Status == Common.SILA_RECIPE_APPROVED || recipe.Status == Common.SILA_RECIPE_REJECTED)
            {
                throw new BadRequestCustomException(
                    "Create a Draft revision first.",
                    "Approved and rejected recipes are read-only. Start a Draft revision before changing the picture.");
            }

            string slot = SilaRecipePictureRules.PreferDraftSlot(recipe)
                ? SilaRecipePictureRules.SLOT_DRAFT
                : SilaRecipePictureRules.SLOT_ACTIVE;

            if (slot == SilaRecipePictureRules.SLOT_DRAFT
                && SilaRecipePictureRules.FindSlotFile(basePath, buyer.Id, recipe.Id, SilaRecipePictureRules.SLOT_DRAFT) == null
                && SilaRecipePictureRules.FindSlotFile(basePath, buyer.Id, recipe.Id, SilaRecipePictureRules.SLOT_ACTIVE) != null)
            {
                SilaRecipePictureRules.SeedDraftFromActive(basePath, buyer.Id, recipe.Id, _logger);
            }

            SilaRecipePictureRules.WriteSlot(basePath, buyer.Id, recipe.Id, slot, file.Content, extension);
            InventoryLedger ledger = new InventoryLedger(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_UPDATED, $"Picture updated ({slot}).");
            await _repository.SaveAsync();

            _logger.LogInfo($"Recipe picture saved. RecipeId: {recipe.Id}, Slot: {slot}");
            return SilaRecipePictureRules.MapInfo(true, slot);
        }
    }
}
