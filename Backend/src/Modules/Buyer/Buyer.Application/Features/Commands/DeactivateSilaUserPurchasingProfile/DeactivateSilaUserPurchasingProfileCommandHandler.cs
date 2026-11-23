using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.DeactivateSilaUserPurchasingProfile
{
    public class DeactivateSilaUserPurchasingProfileCommandHandler
        : IRequestHandler<DeactivateSilaUserPurchasingProfileCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeactivateSilaUserPurchasingProfileCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(DeactivateSilaUserPurchasingProfileCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaUserPurchasingProfile? row = await _repository.SilaUserPurchasingProfile
                .FindFirstByConditionAsync(x => x.Id == request.Id && x.BuyerId == buyer.Id && x.IsActive);
            if (row == null || (!request.AdminScope && row.UserId != request.UserId))
            {
                throw new NotFoundCustomException("Purchasing profile not found.", "Select an active user purchasing profile.");
            }

            row.Status = SilaUserPurchasingProfileRules.STATUS_INACTIVE;
            row.IsActive = false;
            row.IsDefault = false;
            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_USER_PO_PROFILE, row.Id, Common.AUDIT_CANCELLED, "Purchasing profile deactivated");
            await _repository.SaveAsync();
            _logger.LogInfo($"Purchasing profile deactivated. Id: {row.Id}");
            return Unit.Value;
        }
    }
}
