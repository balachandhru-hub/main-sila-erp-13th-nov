using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyRecipes
{
    public class GetSilaRecipeLegacyRecipesQueryHandler : IRequestHandler<GetSilaRecipeLegacyRecipesQuery, SilaRecipeLegacyRecipePageDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaRecipeLegacyRecipesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaRecipeLegacyRecipePageDto> Handle(GetSilaRecipeLegacyRecipesQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            _ = await SilaRecipeLegacyMigration.GetBatchAsync(_repository, _logger, buyer.Id, request.BatchId, cancellationToken);
            return await SilaRecipeLegacyMigration.ListRecipesAsync(
                _repository, buyer.Id, request.BatchId, request.Status, request.Search, request.Index, request.Limit, cancellationToken);
        }
    }
}
