using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SetDefaultSilaUserPurchasingProfile
{
    public class SetDefaultSilaUserPurchasingProfileCommandHandler
        : IRequestHandler<SetDefaultSilaUserPurchasingProfileCommand, SilaUserPurchasingProfileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SetDefaultSilaUserPurchasingProfileCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaUserPurchasingProfileDto> Handle(
            SetDefaultSilaUserPurchasingProfileCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaUserPurchasingProfile? row = await _repository.SilaUserPurchasingProfile
                .FindFirstByConditionAsync(x => x.Id == request.Id && x.BuyerId == buyer.Id && x.IsActive
                    && x.Status == SilaUserPurchasingProfileRules.STATUS_ACTIVE);
            if (row == null || (!request.AdminScope && row.UserId != request.UserId))
            {
                throw new NotFoundCustomException("Purchasing profile not found.", "Select an active user purchasing profile.");
            }

            await SilaUserPurchasingProfileRules.ClearOtherDefaultsAsync(
                _repository, buyer.Id, row.UserId, row.Id, cancellationToken);
            row.IsDefault = true;
            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_USER_PO_PROFILE, row.Id, Common.AUDIT_MODIFIED, "Set as default purchasing profile");
            await _repository.SaveAsync();
            return SilaUserPurchasingProfileRules.ToDto(row);
        }
    }
}
