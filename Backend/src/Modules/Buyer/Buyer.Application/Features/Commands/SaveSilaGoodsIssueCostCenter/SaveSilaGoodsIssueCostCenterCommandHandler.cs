using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SaveSilaGoodsIssueCostCenter
{
    public class SaveSilaGoodsIssueCostCenterCommandHandler : IRequestHandler<SaveSilaGoodsIssueCostCenterCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SaveSilaGoodsIssueCostCenterCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SaveSilaGoodsIssueCostCenterCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving Goods Issue cost center. OrganizationId: {request.OrganizationId}, Id: {request.Id}");
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            Guid id = await SilaGoodsIssueCostCenterRules.SaveAsync(
                _repository, _logger, buyer.Id, request.Id, request.Request, cancellationToken);
            _logger.LogInfo($"Goods Issue cost center saved. Id: {id}");
            return id;
        }
    }
}
