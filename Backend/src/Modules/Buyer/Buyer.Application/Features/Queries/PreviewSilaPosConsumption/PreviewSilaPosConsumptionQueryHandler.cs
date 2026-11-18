using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.PreviewSilaPosConsumption
{
    public class PreviewSilaPosConsumptionQueryHandler : IRequestHandler<PreviewSilaPosConsumptionQuery, SilaPosConsumptionPreviewDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public PreviewSilaPosConsumptionQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaPosConsumptionPreviewDto> Handle(PreviewSilaPosConsumptionQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Previewing POS consumption. OrganizationId: {request.OrganizationId}, BatchId: {request.BatchId}, Selected: {request.TransactionIds?.Count}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            PosSalesBatch? batch = await _repository.PosSalesBatch.FindFirstByConditionAsync(
                x => x.Id == request.BatchId && x.BuyerId == buyer.Id && x.IsActive);
            if (batch == null)
            {
                throw new NotFoundCustomException("Sales batch not found.", "Select a sales batch of this organization.");
            }

            return await SilaPosProcessing.PreviewConsumptionAsync(
                _repository, buyer.Id, request.BatchId, request.TransactionIds, cancellationToken);
        }
    }
}
