using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaRecipeLegacyBatch
{
    public class GetSilaRecipeLegacyBatchQueryHandler : IRequestHandler<GetSilaRecipeLegacyBatchQuery, SilaRecipeLegacyBatchDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaRecipeLegacyBatchQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaRecipeLegacyBatchDetailDto> Handle(GetSilaRecipeLegacyBatchQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            RecipeLegacyBatch batch = await SilaRecipeLegacyMigration.GetBatchAsync(_repository, _logger, buyer.Id, request.BatchId, cancellationToken);
            return SilaRecipeLegacyMigration.ToDetail(batch);
        }
    }
}
