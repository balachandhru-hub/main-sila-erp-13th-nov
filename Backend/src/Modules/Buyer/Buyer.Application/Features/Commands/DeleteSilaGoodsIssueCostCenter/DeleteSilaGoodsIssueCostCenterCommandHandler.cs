using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeleteSilaGoodsIssueCostCenter
{
    public class DeleteSilaGoodsIssueCostCenterCommandHandler : IRequestHandler<DeleteSilaGoodsIssueCostCenterCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteSilaGoodsIssueCostCenterCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeleteSilaGoodsIssueCostCenterCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Deleting Goods Issue cost center. OrganizationId: {request.OrganizationId}, Id: {request.Id}");
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            await SilaGoodsIssueCostCenterRules.DeleteAsync(_repository, _logger, buyer.Id, request.Id, cancellationToken);
            _logger.LogInfo($"Goods Issue cost center deleted. Id: {request.Id}");
            return Unit.Value;
        }
    }
}
