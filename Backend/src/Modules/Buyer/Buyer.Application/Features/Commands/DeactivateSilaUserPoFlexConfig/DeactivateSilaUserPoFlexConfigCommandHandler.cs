using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeactivateSilaUserPoFlexConfig
{
    public class DeactivateSilaUserPoFlexConfigCommandHandler : IRequestHandler<DeactivateSilaUserPoFlexConfigCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeactivateSilaUserPoFlexConfigCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeactivateSilaUserPoFlexConfigCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            await SilaUserPoFlexRules.EnsureOwnedAsync(_repository, _logger, buyer.Id, request.Id);
            SilaUserPoFlexConfig row = (await _repository.SilaUserPoFlexConfig
                .FindFirstByConditionAsync(x => x.Id == request.Id && x.BuyerId == buyer.Id && x.IsActive))!;
            row.Status = SilaUserPoFlexRules.STATUS_INACTIVE;
            row.IsActive = false;
            row.IsDefault = false;
            _repository.SilaUserPoFlexConfig.Update(row);
            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_USER_PO_PROFILE, row.Id, Common.AUDIT_CANCELLED, row.Name);
            await _repository.SaveAsync();
            return Unit.Value;
        }
    }
}
