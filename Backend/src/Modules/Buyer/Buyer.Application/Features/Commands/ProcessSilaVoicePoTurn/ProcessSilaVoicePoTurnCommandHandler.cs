using System.Text.Json;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ProcessSilaVoicePoTurn
{
    public class ProcessSilaVoicePoTurnCommandHandler : IRequestHandler<ProcessSilaVoicePoTurnCommand, SilaVoicePoSessionDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ProcessSilaVoicePoTurnCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaVoicePoSessionDto> Handle(ProcessSilaVoicePoTurnCommand request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaVoicePoSession session = await GetOpenSessionAsync(buyer.Id, request.UserId, request.SessionId);
            SilaVoicePoTurnRequestDto input = request.Request;
            string? transcript = input.Transcript?.Trim();

            int nextTurn = await _repository.SilaVoicePoTurn
                .FindByCondition(x => x.SessionId == session.Id)
                .Select(x => (int?)x.TurnNumber)
                .MaxAsync(cancellationToken) ?? 0;
            nextTurn += 1;

            SilaVoicePoTurn userTurn = new()
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                TurnNumber = nextTurn,
                Role = SilaVoicePoAgentRules.ROLE_USER,
                Channel = string.IsNullOrWhiteSpace(input.Channel) ? "TEXT" : input.Channel.Trim().ToUpperInvariant(),
                Transcript = transcript,
                TranscriptionStatus = input.TranscriptionStatus,
                Confidence = input.Confidence,
                IsActive = true
            };
            _repository.SilaVoicePoTurn.Create(userTurn);
            session.LatestTranscript = transcript;

            SilaVoicePoDraftDto draft = SilaVoicePoAgentRules.DeserializeDraft(session.DraftJson) ?? new SilaVoicePoDraftDto();
            string spoken;
            VoicePoParseResult parse;

            if (SilaVoicePoAgentRules.IsUncertainTranscript(transcript, input.Confidence, input.TranscriptionStatus))
            {
                parse = new VoicePoParseResult
                {
                    Intent = VoicePoIntent.Unknown,
                    NeedsClarification = true,
                    ClarificationMessage = "I could not hear that clearly. Please repeat the material and quantity."
                };
                spoken = parse.ClarificationMessage!;
            }
            else
            {
                parse = SilaVoicePoAgentRules.ParseOrderIntent(transcript);
                spoken = await ApplyIntentAsync(buyer.Id, request.UserId, session, draft, parse, cancellationToken);
            }

            SilaVoicePoDraftValidation validation = SilaVoicePoAgentRules.ValidateDraft(draft);
            // Always prefer a mandatory-field ask over any "added / confirm" success copy.
            if (validation.Status != SilaVoicePoAgentRules.VALIDATION_OK)
            {
                string? ask = SilaVoicePoAgentRules.PreferMandatoryAsk(validation)
                    ?? SilaVoicePoAgentRules.BuildSpokenReply(parse.Intent, draft, validation);
                if (!string.IsNullOrWhiteSpace(ask))
                {
                    spoken = ask;
                }
            }

            SilaVoicePoSessionMapper.WriteDraft(session, draft);
            SilaVoicePoSessionMapper.WriteValidation(session, validation);
            session.SpokenReply = spoken;
            if (!string.IsNullOrWhiteSpace(input.Language))
            {
                session.Language = input.Language.Trim();
            }

            nextTurn += 1;
            SilaVoicePoTurn agentTurn = new()
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                TurnNumber = nextTurn,
                Role = SilaVoicePoAgentRules.ROLE_AGENT,
                Channel = "TEXT",
                SpokenText = spoken,
                Transcript = spoken,
                StructuredJson = JsonSerializer.Serialize(new
                {
                    intent = parse.Intent.ToString(),
                    validation = validation.Status,
                    llmConfigured = !string.IsNullOrWhiteSpace(SilaVoicePoAgentRules.LlmUrl),
                    sttConfigured = !string.IsNullOrWhiteSpace(SilaVoicePoAgentRules.SttUrl),
                    ttsConfigured = !string.IsNullOrWhiteSpace(SilaVoicePoAgentRules.TtsUrl)
                }),
                IsActive = true
            };
            _repository.SilaVoicePoTurn.Create(agentTurn);

            InventoryLedger ledger = new(_repository, buyer.Id, request.UserId);
            ledger.AddEvent(Common.SILA_REF_VOICE_PO, session.Id, Common.AUDIT_MODIFIED, $"Turn {userTurn.TurnNumber}: {parse.Intent}");
            await _repository.SaveAsync();

            List<SilaVoicePoTurn> turns = await _repository.SilaVoicePoTurn
                .FindByCondition(x => x.SessionId == session.Id && x.IsActive)
                .OrderBy(x => x.TurnNumber)
                .ToListAsync(cancellationToken);
            return SilaVoicePoSessionMapper.ToDto(session, turns);
        }

        private async Task<string> ApplyIntentAsync(
            Guid buyerId,
            Guid userId,
            SilaVoicePoSession session,
            SilaVoicePoDraftDto draft,
            VoicePoParseResult parse,
            CancellationToken cancellationToken)
        {
            switch (parse.Intent)
            {
                case VoicePoIntent.GoWithDefault:
                {
                    SilaUserPurchasingDefaultsDto defaults = await SilaUserPurchasingProfileRules.ResolveAsync(
                        _repository,
                        _logger,
                        buyerId,
                        userId,
                        new ResolveSilaUserPurchasingDefaultsRequestDto
                        {
                            ProfileId = session.PurchasingProfileId,
                            PropertyId = session.PropertyId,
                            OutletId = session.OutletId,
                            LocationId = session.LocationId,
                            PlantCode = draft.PlantCode
                        },
                        cancellationToken);
                    SilaVoicePoAgentRules.ApplyGoWithDefault(draft, defaults);
                    session.PurchasingProfileId = defaults.ProfileId ?? session.PurchasingProfileId;
                    session.PurchasingProfileVersion = defaults.ProfileVersion ?? session.PurchasingProfileVersion;
                    return SilaVoicePoAgentRules.BuildSpokenReply(parse.Intent, draft, SilaVoicePoAgentRules.ValidateDraft(draft));
                }
                case VoicePoIntent.DoubleQuantity:
                    SilaVoicePoAgentRules.DoubleQuantities(draft);
                    return SilaVoicePoAgentRules.BuildSpokenReply(parse.Intent, draft, SilaVoicePoAgentRules.ValidateDraft(draft));
                case VoicePoIntent.SetUnitPrice:
                {
                    SilaVoicePoDraftLineDto? target = draft.Lines.LastOrDefault(l => l.UnitPrice == null || l.UnitPrice <= 0)
                        ?? draft.Lines.LastOrDefault();
                    if (target == null)
                    {
                        return "There is no draft line to price. Add a material first.";
                    }

                    decimal price = parse.UnitPrice is > 0 ? parse.UnitPrice.Value : 1m;
                    target.UnitPrice = price;
                    target.ResolutionNote = string.IsNullOrWhiteSpace(target.ResolutionNote)
                        ? $"Unit price set to {price}."
                        : $"{target.ResolutionNote} Unit price set to {price}.";
                    return SilaVoicePoAgentRules.BuildSpokenReply(parse.Intent, draft, SilaVoicePoAgentRules.ValidateDraft(draft));
                }
                case VoicePoIntent.RemoveLine:
                {
                    bool removed = SilaVoicePoAgentRules.RemoveLine(draft, parse.MaterialHint);
                    return removed
                        ? SilaVoicePoAgentRules.BuildSpokenReply(parse.Intent, draft, SilaVoicePoAgentRules.ValidateDraft(draft))
                        : "I could not find that line to remove.";
                }
                case VoicePoIntent.Review:
                    return SilaVoicePoAgentRules.SummarizeDraft(draft);
                case VoicePoIntent.Confirm:
                    return SilaVoicePoAgentRules.IsExplicitConfirmation(session.LatestTranscript)
                        ? "To create the order, use Confirm with an explicit phrase such as 'confirm purchase order'."
                        : "Say 'confirm purchase order' to create the order. Yes or OK is not enough.";
                case VoicePoIntent.Cancel:
                    session.Status = SilaVoicePoAgentRules.STATUS_CANCELLED;
                    return "Session cancelled.";
                case VoicePoIntent.AddLine:
                    return await AddLineAsync(buyerId, userId, session, draft, parse, cancellationToken);
                default:
                    return parse.ClarificationMessage
                        ?? "Please say what to order, or 'confirm purchase order' when the draft is ready.";
            }
        }

        private async Task<string> AddLineAsync(
            Guid buyerId,
            Guid userId,
            SilaVoicePoSession session,
            SilaVoicePoDraftDto draft,
            VoicePoParseResult parse,
            CancellationToken cancellationToken)
        {
            if (parse.NeedsClarification || string.IsNullOrWhiteSpace(parse.MaterialHint))
            {
                return parse.ClarificationMessage ?? "Which material should I add?";
            }

            MaterialResolution material = await SilaVoicePoAgentRules.ResolveMaterialAsync(
                _repository, buyerId, parse.MaterialHint, cancellationToken);
            if (material.Candidates.Count > 1)
            {
                string options = string.Join(", ", material.Candidates.Select(c => $"{c.MaterialCode} ({c.Description})"));
                return $"Several materials match. Please say the code: {options}";
            }

            // Unknown material → text PO line (no invented material code). Uses defaults: material group, AA K, GL, etc.
            bool textPo = !material.Resolved || material.Material == null;
            ItemBuyerMaster? item = material.Material;
            string inventoryType = textPo
                ? SilaUserPurchasingProfileRules.INVENTORY_NON_STOCK
                : SilaUserPurchasingProfileRules.NormalizeInventoryType(
                    item!.InventoryType ?? (item.IsInventoryItem ? SilaUserPurchasingProfileRules.INVENTORY_STOCK : SilaUserPurchasingProfileRules.INVENTORY_NON_STOCK));

            SilaUserPurchasingDefaultsDto defaults = await SilaUserPurchasingProfileRules.ResolveAsync(
                _repository,
                _logger,
                buyerId,
                userId,
                new ResolveSilaUserPurchasingDefaultsRequestDto
                {
                    ProfileId = session.PurchasingProfileId,
                    PropertyId = session.PropertyId,
                    OutletId = session.OutletId,
                    LocationId = session.LocationId,
                    PlantCode = draft.PlantCode,
                    MaterialCode = item?.MaterialCode,
                    InventoryType = inventoryType
                },
                cancellationToken);

            // Text / consumable POs always carry account assignment from defaults (category K).
            if (textPo)
            {
                defaults.RequiresAccountAssignment = true;
                defaults.AccountAssignmentCategory ??= "K";
                draft.AccountAssignmentCategory ??= defaults.AccountAssignmentCategory;
                draft.CostCenter ??= defaults.CostCenter;
                draft.GlAccount ??= defaults.GlAccount;
                draft.MaterialGroup ??= defaults.MaterialGroup;
            }

            if (!string.IsNullOrWhiteSpace(parse.Currency))
            {
                draft.Currency = parse.Currency.Trim().ToUpperInvariant();
            }
            else
            {
                draft.Currency ??= defaults.DefaultCurrency ?? "AED";
            }

            SupplierResolution supplier = await SilaVoicePoAgentRules.ResolveSupplierAsync(
                _repository,
                buyerId,
                defaults.PreferredSupplierId,
                defaults.PreferredSupplierCode,
                item?.MaterialCode,
                cancellationToken);

            if (textPo && !supplier.Resolved && !string.IsNullOrWhiteSpace(defaults.PreferredSupplierCode))
            {
                // Prefer the SAP vendor code from PO defaults even when not in local mapping.
                supplier = new SupplierResolution
                {
                    Resolved = true,
                    SupplierId = defaults.PreferredSupplierId ?? Guid.NewGuid(),
                    SupplierCode = defaults.PreferredSupplierCode,
                    SupplierName = defaults.PreferredSupplierCode,
                    Note = "Preferred supplier code from PO defaults (text PO)."
                };
            }

            string description = textPo
                ? (parse.MaterialHint ?? "Text item").Trim()
                : (item!.Description ?? item.MaterialCode ?? parse.MaterialHint ?? "Item");

            SilaVoicePoDraftLineDto? existing = textPo
                ? draft.Lines.FirstOrDefault(l =>
                    string.IsNullOrWhiteSpace(l.MaterialCode)
                    && string.Equals(l.Description, description, StringComparison.OrdinalIgnoreCase))
                : draft.Lines.FirstOrDefault(l =>
                    string.Equals(l.MaterialCode, item!.MaterialCode, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Quantity += parse.Quantity ?? 1;
                if (!string.IsNullOrWhiteSpace(parse.UnitOfMeasure))
                {
                    existing.UnitOfMeasure = parse.UnitOfMeasure;
                }

                if (parse.UnitPrice != null)
                {
                    existing.UnitPrice = parse.UnitPrice;
                }
            }
            else
            {
                // Never invent UOM or price from the item master — ask the user (or "use one AED").
                string? resolvedUom = parse.UnitOfMeasure;
                decimal? resolvedPrice = parse.UnitPrice is > 0 ? parse.UnitPrice : null;

                SilaVoicePoDraftLineDto line = new()
                {
                    LineNumber = draft.Lines.Count + 1,
                    MaterialId = item?.Id,
                    MaterialCode = textPo ? null : item!.MaterialCode,
                    Description = description,
                    Quantity = parse.Quantity ?? 1,
                    UnitOfMeasure = resolvedUom,
                    UnitPrice = resolvedPrice,
                    SupplierId = supplier.SupplierId,
                    SupplierCode = supplier.SupplierCode,
                    SupplierName = supplier.SupplierName,
                    StorageLocation = draft.StorageLocation ?? defaults.StorageLocation,
                    MaterialGroup = defaults.MaterialGroup ?? item?.MaterialGroup,
                    InventoryType = inventoryType,
                    Resolved = true,
                    ResolutionNote = textPo
                        ? $"Text PO (no item-master match). {supplier.Note}".Trim()
                        : $"{material.Note} {supplier.Note}".Trim()
                };

                if (defaults.RequiresAccountAssignment
                    || textPo
                    || SilaUserPurchasingProfileRules.RequiresAccountAssignment(inventoryType))
                {
                    line.AccountAssignmentCategory = defaults.AccountAssignmentCategory ?? "K";
                    line.CostCenter = defaults.CostCenter;
                    line.GlAccount = defaults.GlAccount;
                }

                draft.Lines.Add(line);
            }

            SilaVoicePoAgentRules.Renumber(draft);
            SilaVoicePoDraftValidation validation = SilaVoicePoAgentRules.ValidateDraft(draft);
            string spokenName = description;
            // Never claim "added" while price/UOM (or other mandatory fields) are still missing.
            if (validation.Status != SilaVoicePoAgentRules.VALIDATION_OK)
            {
                return SilaVoicePoAgentRules.BuildSpokenReply(VoicePoIntent.AddLine, draft, validation);
            }

            return SilaVoicePoAgentRules.BuildSpokenReply(
                VoicePoIntent.AddLine,
                draft,
                validation,
                textPo
                    ? $"No item-master match — added text line '{spokenName}' with your defaults (material group {defaults.MaterialGroup}, account assignment {defaults.AccountAssignmentCategory ?? "K"}). {SilaVoicePoAgentRules.SummarizeDraft(draft)}"
                    : $"Added {spokenName}. {SilaVoicePoAgentRules.SummarizeDraft(draft)}");
        }

        private async Task<SilaVoicePoSession> GetOpenSessionAsync(Guid buyerId, Guid userId, Guid sessionId)
        {
            SilaVoicePoSession? session = await _repository.SilaVoicePoSession
                .FindFirstByConditionAsync(x => x.Id == sessionId && x.BuyerId == buyerId && x.IsActive);
            if (session == null || session.UserId != userId)
            {
                throw new NotFoundCustomException("Voice PO session not found.", "Start a new Voice PO session.");
            }

            if (session.Status is SilaVoicePoAgentRules.STATUS_CREATED
                or SilaVoicePoAgentRules.STATUS_CANCELLED
                or SilaVoicePoAgentRules.STATUS_FAILED
                or SilaVoicePoAgentRules.STATUS_CONFIRMED)
            {
                throw new ConflictCustomException(
                    "Voice PO session is closed.",
                    $"Session {session.SessionNumber} is {session.Status} and cannot accept new turns.");
            }

            return session;
        }
    }
}
