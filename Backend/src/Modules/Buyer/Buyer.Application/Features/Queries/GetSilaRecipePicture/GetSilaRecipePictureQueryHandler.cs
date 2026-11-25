using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipePicture
{
    public class GetSilaRecipePictureQueryHandler : IRequestHandler<GetSilaRecipePictureQuery, SilaRecipePictureFileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IConfiguration _configuration;

        public GetSilaRecipePictureQueryHandler(IRepositoryWrapper repository, ILoggerManager logger, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SilaRecipePictureFileDto> Handle(GetSilaRecipePictureQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching recipe picture. RecipeId: {request.RecipeId}, PreferDraft: {request.PreferDraft}");

            string basePath = SilaRecipePictureRules.RequireBasePath(_configuration, _logger);
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Recipe recipe = await SilaRecipeRules.GetTrackedAsync(_repository, _logger, buyer.Id, request.RecipeId);

            bool preferDraft = request.PreferDraft && SilaRecipePictureRules.PreferDraftSlot(recipe);
            string? path = request.PreferDraft
                ? SilaRecipePictureRules.ResolveStudioFile(basePath, buyer.Id, recipe, preferDraft)
                : SilaRecipePictureRules.ResolveCatalogFile(basePath, buyer.Id, recipe);

            if (path == null || !File.Exists(path))
            {
                throw new NotFoundCustomException("Picture not found.", "This recipe has no saved picture.");
            }

            byte[] content = await File.ReadAllBytesAsync(path, cancellationToken);
            string fileName = Path.GetFileName(path);
            string slot = fileName.StartsWith(SilaRecipePictureRules.SLOT_DRAFT, StringComparison.OrdinalIgnoreCase)
                ? SilaRecipePictureRules.SLOT_DRAFT
                : SilaRecipePictureRules.SLOT_ACTIVE;
            string? kind = SilaFileRules.KindOf(fileName) ?? SilaFileRules.DetectBinaryKind(content) ?? SilaFileRules.KIND_JPEG;

            return new SilaRecipePictureFileDto
            {
                FileName = SilaFileRules.SafeDisplayName(fileName, $"recipe-{recipe.Id:N}", kind),
                ContentType = SilaFileRules.ContentTypeOf(kind),
                Content = content,
                Slot = slot
            };
        }
    }
}