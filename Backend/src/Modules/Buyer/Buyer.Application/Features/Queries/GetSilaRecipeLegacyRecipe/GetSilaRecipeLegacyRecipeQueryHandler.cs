using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyRecipe
{
    public class GetSilaRecipeLegacyRecipeQueryHandler : IRequestHandler<GetSilaRecipeLegacyRecipeQuery, SilaRecipeLegacyRecipeDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaRecipeLegacyRecipeQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaRecipeLegacyRecipeDetailDto> Handle(GetSilaRecipeLegacyRecipeQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            _ = await SilaRecipeLegacyMigration.GetBatchAsync(_repository, _logger, buyer.Id, request.BatchId, cancellationToken);
            return await SilaRecipeLegacyMigration.GetRecipeDetailAsync(
                _repository, _logger, buyer.Id, request.BatchId, request.RecipeId, cancellationToken);
        }
    }
}
