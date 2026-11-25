using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyBatches
{
    public class GetSilaRecipeLegacyBatchesQueryHandler : IRequestHandler<GetSilaRecipeLegacyBatchesQuery, List<SilaRecipeLegacyBatchListItemDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaRecipeLegacyBatchesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SilaRecipeLegacyBatchListItemDto>> Handle(GetSilaRecipeLegacyBatchesQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            return await SilaRecipeLegacyMigration.ListBatchesAsync(_repository, buyer.Id, request.Index, request.Limit, cancellationToken);
        }
    }
}
