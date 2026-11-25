using Buyer.Application.Features.Commands.ProcessSilaVoicePoTurn;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.StartSilaVoicePoSession
{
    public class StartSilaVoicePoSessionCommandHandler : IRequestHandler<StartSilaVoicePoSessionCommand, SilaVoicePoSessionDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public StartSilaVoicePoSessionCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<SilaVoicePoSessionDto> Handle(StartSilaVoicePoSessionCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaVoicePoStartRequestDto input = request.Request;

            if (!string.IsNullOrWhiteSpace(input.IdempotencyKey))
            {
                string key = input.IdempotencyKey.Trim();
                SilaVoicePoSession? existing = await _repository.SilaVoicePoSession
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.UserId == request.UserId
                        && x.IsActive && x.IdempotencyKey == key);
                if (existing != null)
                {
                    List<SilaVoicePoTurn> turns = await _repository.SilaVoicePoTurn
                        .FindByCondition(x => x.SessionId == existing.Id && x.IsActive)
                        .OrderBy(x => x.TurnNumber)
                        .ToListAsync(cancellationToken);
                    return SilaVoicePoSessionMapper.ToDto(existing, turns);
                }
            }

            SilaUserPurchasingDefaultsDto defaults = await SilaUserPurchasingProfileRules.ResolveAsync(
                _repository,
                _logger,
                buyer.Id,
                request.UserId,
                new ResolveSilaUserPurchasingDefaultsRequestDto
                {
                    ProfileId = input.PurchasingProfileId,
                    PropertyId = input.PropertyId,
                    OutletId = input.OutletId,
                    LocationId = input.LocationId
                },
                cancellationToken);

            SilaVoicePoDraftDto draft = new()
            {
                CompanyCode = defaults.CompanyCode,
                PlantCode = defaults.PlantCode,
                StorageLocation = defaults.StorageLocation,
                PurchasingOrganization = defaults.PurchasingOrganization,
                PurchasingGroup = defaults.PurchasingGroup,
                Currency = defaults.DefaultCurrency
            };
            if (defaults.DefaultDeliveryLeadTimeDays != null)
            {
                draft.DeliveryDate = DateTime.UtcNow.Date.AddDays(defaults.DefaultDeliveryLeadTimeDays.Value);
            }

            SilaVoicePoSession session = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                UserId = request.UserId,
                SessionNumber = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.VOICE_PO, 6, cancellationToken),
                Status = SilaVoicePoAgentRules.STATUS_OPEN,
                LocationId = input.LocationId ?? defaults.LocationId,
                PropertyId = input.PropertyId ?? defaults.PropertyId,
                OutletId = input.OutletId ?? defaults.OutletId,
                PurchasingProfileId = defaults.ProfileId,
                PurchasingProfileVersion = defaults.ProfileVersion,
                Language = string.IsNullOrWhiteSpace(input.Language) ? "en" : input.Language.Trim(),
                IdempotencyKey = string.IsNullOrWhiteSpace(input.IdempotencyKey) ? null : input.IdempotencyKey.Trim(),
                SpokenReply = "Voice purchase order started. Tell me what to order, for example 'order 10 kg chicken'.",
                TranscriptRetainUntil = DateTime.UtcNow.AddDays(30),
                IsActive = true
            };
            SilaVoicePoSessionMapper.WriteDraft(session, draft);
            SilaVoicePoSessionMapper.WriteValidation(session, SilaVoicePoAgentRules.ValidateDraft(draft));
            _repository.SilaVoicePoSession.Create(session);

            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_VOICE_PO, session.Id, Common.AUDIT_CREATED, session.SessionNumber);
            await _repository.SaveAsync();
            _logger.LogInfo($"Voice PO session started. SessionId: {session.Id}, Number: {session.SessionNumber}");

            if (!string.IsNullOrWhiteSpace(input.InitialTranscript))
            {
                return await _mediator.Send(new ProcessSilaVoicePoTurnCommand
                {
                    OrganizationId = request.OrganizationId,
                    UserId = request.UserId,
                    SessionId = session.Id,
                    Request = new SilaVoicePoTurnRequestDto
                    {
                        Transcript = input.InitialTranscript,
                        Channel = "TEXT"
                    }
                }, cancellationToken);
            }

            return SilaVoicePoSessionMapper.ToDto(session);
        }
    }
}
