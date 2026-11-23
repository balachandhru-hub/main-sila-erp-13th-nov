using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpsertSilaUserPurchasingProfile
{
    public class UpsertSilaUserPurchasingProfileCommandHandler
        : IRequestHandler<UpsertSilaUserPurchasingProfileCommand, SilaUserPurchasingProfileDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpsertSilaUserPurchasingProfileCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaUserPurchasingProfileDto> Handle(
            UpsertSilaUserPurchasingProfileCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            List<string> errors = SilaUserPurchasingProfileRules.ValidateWrite(request.Request);
            if (errors.Count > 0)
            {
                throw new BadRequestCustomException("Purchasing profile is invalid.", string.Join(" ", errors));
            }

            Guid targetUserId = request.AdminScope
                ? (request.Request.UserId ?? request.UserId)
                : request.UserId;

            SilaUserPurchasingProfile? existing = null;
            if (request.Id != null)
            {
                existing = await _repository.SilaUserPurchasingProfile
                    .FindFirstByConditionAsync(x => x.Id == request.Id.Value && x.BuyerId == buyer.Id && x.IsActive);
                if (existing == null || (!request.AdminScope && existing.UserId != request.UserId))
                {
                    throw new NotFoundCustomException("Purchasing profile not found.", "Select an active user purchasing profile.");
                }
            }

            SilaUserPurchasingProfile row = existing ?? new SilaUserPurchasingProfile
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProfileVersion = 1,
                IsActive = true,
                Status = SilaUserPurchasingProfileRules.STATUS_ACTIVE
            };

            if (existing != null)
            {
                row.ProfileVersion = existing.ProfileVersion + 1;
            }
            else
            {
                _repository.SilaUserPurchasingProfile.Create(row);
            }

            SilaUserPurchasingProfileWriteDto write = request.Request;
            write.UserId = targetUserId;
            SilaUserPurchasingProfileRules.Apply(row, write, targetUserId);

            if (row.IsDefault)
            {
                await SilaUserPurchasingProfileRules.ClearOtherDefaultsAsync(
                    _repository, buyer.Id, targetUserId, row.Id, cancellationToken);
            }

            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(
                Common.SILA_REF_USER_PO_PROFILE,
                row.Id,
                existing == null ? Common.AUDIT_CREATED : Common.AUDIT_MODIFIED,
                row.IsDefault ? "Default purchasing profile" : "Purchasing profile");
            await _repository.SaveAsync();
            _logger.LogInfo($"Purchasing profile upserted. Id: {row.Id}, UserId: {row.UserId}, Version: {row.ProfileVersion}");
            return SilaUserPurchasingProfileRules.ToDto(row);
        }
    }
}
