using System.Text.Json;
using Buyer.Application.Features.Commands.CreatePurchaseOrder;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ConfirmSilaVoicePoDraft
{
    public class ConfirmSilaVoicePoDraftCommandHandler : IRequestHandler<ConfirmSilaVoicePoDraftCommand, SilaVoicePoSessionDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public ConfirmSilaVoicePoDraftCommandHandler(IRepositoryWrapper repository, ILoggerManager logger, IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<SilaVoicePoSessionDto> Handle(ConfirmSilaVoicePoDraftCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaVoicePoSession? session = await _repository.SilaVoicePoSession
                .FindFirstByConditionAsync(x => x.Id == request.SessionId && x.BuyerId == buyer.Id && x.IsActive);
            if (session == null || session.UserId != request.UserId)
            {
                throw new NotFoundCustomException("Voice PO session not found.", "Start a new Voice PO session.");
            }

            // Idempotent replay when the session already created POs.
            if (session.Status == SilaVoicePoAgentRules.STATUS_CREATED && session.CreatedPurchaseOrderId != null)
            {
                return await LoadDtoAsync(session, cancellationToken);
            }

            if (session.Status is SilaVoicePoAgentRules.STATUS_CANCELLED or SilaVoicePoAgentRules.STATUS_FAILED)
            {
                throw new ConflictCustomException("Voice PO session is closed.", $"Session {session.SessionNumber} is {session.Status}.");
            }

            string? phrase = request.Request.ConfirmationPhrase ?? session.LatestTranscript;
            if (SilaVoicePoAgentRules.RejectsConfirmation(phrase))
            {
                throw new BadRequestCustomException(
                    "Explicit confirmation required.",
                    "Say or send an explicit phrase such as 'confirm purchase order'. Yes, OK, or silence is not accepted.");
            }

            SilaVoicePoDraftDto draft = SilaVoicePoAgentRules.DeserializeDraft(session.DraftJson) ?? new SilaVoicePoDraftDto();
            SilaVoicePoDraftValidation validation = SilaVoicePoAgentRules.ValidateDraft(draft);
            SilaVoicePoSessionMapper.WriteValidation(session, validation);
            if (validation.Status != SilaVoicePoAgentRules.VALIDATION_OK)
            {
                session.SpokenReply = validation.Clarifications.FirstOrDefault()?.Message
                    ?? validation.Errors.FirstOrDefault()
                    ?? "Draft is not ready to confirm.";
                await _repository.SaveAsync();
                throw new BadRequestCustomException(
                    "Voice PO draft is not valid.",
                    string.Join(" ", validation.Errors));
            }

            if (!string.IsNullOrWhiteSpace(request.Request.IdempotencyKey))
            {
                session.IdempotencyKey = request.Request.IdempotencyKey.Trim();
            }

            session.Status = SilaVoicePoAgentRules.STATUS_CONFIRMED;
            await _repository.SaveAsync();

            List<SilaVoicePoCreatedOrderDto> created = new();
            try
            {
                // Refresh line text from ItemBuyerMaster so PO item text is exactly the system description.
                List<string> materialCodes = draft.Lines
                    .Select(l => l.MaterialCode?.Trim())
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Dictionary<string, ItemBuyerMaster> materialsByCode = new(StringComparer.OrdinalIgnoreCase);
                if (materialCodes.Count > 0)
                {
                    List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                        .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && materialCodes.Contains(x.MaterialCode))
                        .ToListAsync(cancellationToken);
                    foreach (ItemBuyerMaster material in materials)
                    {
                        if (!string.IsNullOrWhiteSpace(material.MaterialCode))
                        {
                            materialsByCode[material.MaterialCode.Trim()] = material;
                        }
                    }
                }

                foreach (IGrouping<Guid, SilaVoicePoDraftLineDto> group in draft.Lines.GroupBy(l => l.SupplierId!.Value))
                {
                    SilaVoicePoDraftLineDto first = group.First();
                    CreatePurchaseOrderRequestDto orderRequest = new()
                    {
                        SupplierId = group.Key,
                        CompanyCode = draft.CompanyCode,
                        PlantCode = draft.PlantCode,
                        Currency = draft.Currency,
                        DeliveryDate = draft.DeliveryDate,
                        PurchaseOrderType = null,
                        PurchasingOrganization = draft.PurchasingOrganization,
                        PurchasingGroup = draft.PurchasingGroup,
                        SupplierCode = first.SupplierCode,
                        StorageLocation = draft.StorageLocation,
                        AccountAssignmentCategory = first.AccountAssignmentCategory ?? draft.AccountAssignmentCategory,
                        GlAccount = first.GlAccount ?? draft.GlAccount,
                        Lines = group.Select(l =>
                        {
                            ItemBuyerMaster? master = null;
                            if (!string.IsNullOrWhiteSpace(l.MaterialCode))
                            {
                                materialsByCode.TryGetValue(l.MaterialCode.Trim(), out master);
                            }

                            // Prefer live system description for PurchaseOrderItemText; keep draft text as fallback.
                            string? itemText = !string.IsNullOrWhiteSpace(master?.Description)
                                ? master!.Description
                                : l.Description;
                            if (!string.IsNullOrWhiteSpace(itemText))
                            {
                                l.Description = itemText;
                            }

                            string? materialGroup = l.MaterialGroup
                                ?? draft.MaterialGroup
                                ?? master?.MaterialGroup;

                            return new CreatePurchaseOrderLineDto
                            {
                                MaterialCode = l.MaterialCode,
                                Description = itemText,
                                Quantity = l.Quantity,
                                UnitOfMeasure = l.UnitOfMeasure,
                                UnitPrice = l.UnitPrice,
                                StorageLocation = l.StorageLocation ?? draft.StorageLocation,
                                CostCenter = l.CostCenter ?? draft.CostCenter,
                                MaterialGroup = materialGroup
                            };
                        }).ToList()
                    };

                    PurchaseOrderProcessResultDto result = await _mediator.Send(new CreatePurchaseOrderCommand
                    {
                        OrganizationId = request.OrganizationId,
                        UserId = request.UserId,
                        Request = orderRequest,
                        SupplierName = first.SupplierName
                    }, cancellationToken);

                    created.Add(new SilaVoicePoCreatedOrderDto
                    {
                        PurchaseOrderId = result.Id,
                        PoNumber = result.PoNumber,
                        SupplierId = group.Key,
                        SupplierName = first.SupplierName
                    });
                }

                session.Status = SilaVoicePoAgentRules.STATUS_CREATED;
                session.CreatedPurchaseOrderId = created.FirstOrDefault()?.PurchaseOrderId;
                session.CreatedPoNumber = created.Count == 1
                    ? created[0].PoNumber
                    : string.Join(",", created.Select(c => c.PoNumber).Where(n => !string.IsNullOrWhiteSpace(n)));
                session.SpokenReply = created.Count == 1
                    ? $"Purchase order {session.CreatedPoNumber} created."
                    : $"Created {created.Count} purchase orders: {session.CreatedPoNumber}.";
                session.DraftJson = JsonSerializer.Serialize(new
                {
                    draft,
                    createdOrders = created
                });

                InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
                ledger.AddEvent(
                    Common.SILA_REF_VOICE_PO,
                    session.Id,
                    Common.AUDIT_SUBMITTED,
                    session.CreatedPoNumber);
                await _repository.SaveAsync();
            }
            catch (Exception exception) when (exception is not OperationCanceledException and not BadRequestCustomException)
            {
                session.Status = SilaVoicePoAgentRules.STATUS_FAILED;
                session.SpokenReply = "Purchase order creation failed.";
                session.ValidationStatus = SilaVoicePoAgentRules.VALIDATION_INVALID;
                session.ValidationErrors = SilaVoicePoAgentRules.SerializeErrors(new List<string> { exception.Message });
                InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
                ledger.AddEvent(Common.SILA_REF_VOICE_PO, session.Id, Common.AUDIT_ERP_FAILED, exception.Message);
                await _repository.SaveAsync();
                _logger.LogError($"Voice PO confirm failed. SessionId: {session.Id}, Error: {exception.Message}");
                throw;
            }

            SilaVoicePoSessionDto dto = await LoadDtoAsync(session, cancellationToken);
            dto.CreatedOrders = created;
            return dto;
        }

        private async Task<SilaVoicePoSessionDto> LoadDtoAsync(SilaVoicePoSession session, CancellationToken cancellationToken)
        {
            List<SilaVoicePoTurn> turns = await _repository.SilaVoicePoTurn
                .FindByCondition(x => x.SessionId == session.Id && x.IsActive)
                .OrderBy(x => x.TurnNumber)
                .ToListAsync(cancellationToken);
            return SilaVoicePoSessionMapper.ToDto(session, turns);
        }
    }
}
