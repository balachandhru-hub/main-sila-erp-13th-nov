using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaGoodsIssueCostCenters
{
    public class GetSilaGoodsIssueCostCentersQueryHandler
        : IRequestHandler<GetSilaGoodsIssueCostCentersQuery, List<SilaGoodsIssueCostCenterDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaGoodsIssueCostCentersQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SilaGoodsIssueCostCenterDto>> Handle(
            GetSilaGoodsIssueCostCentersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Goods Issue cost centers. OrganizationId: {request.OrganizationId}, Scope: {request.Scope}");
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            List<SilaGoodsIssueCostCenterDto> result = await SilaGoodsIssueCostCenterRules.ListAsync(
                _repository, buyer.Id, request.Scope, cancellationToken);
            _logger.LogInfo($"Goods Issue cost centers fetched. Count: {result.Count}");
            return result;
        }
    }
}
