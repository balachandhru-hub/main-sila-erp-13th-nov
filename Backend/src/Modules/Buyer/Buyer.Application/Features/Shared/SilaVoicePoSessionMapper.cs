using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;

namespace Buyer.Application.Features.Shared
{
    public static class SilaVoicePoSessionMapper
    {
        public static SilaVoicePoSessionDto ToDto(SilaVoicePoSession session, IEnumerable<SilaVoicePoTurn>? turns = null)
        {
            SilaVoicePoDraftDto? draft = SilaVoicePoAgentRules.DeserializeDraft(session.DraftJson);
            return new SilaVoicePoSessionDto
            {
                Id = session.Id,
                SessionNumber = session.SessionNumber,
                Status = session.Status,
                UserId = session.UserId,
                LocationId = session.LocationId,
                PropertyId = session.PropertyId,
                OutletId = session.OutletId,
                PurchasingProfileId = session.PurchasingProfileId,
                PurchasingProfileVersion = session.PurchasingProfileVersion,
                LatestTranscript = session.LatestTranscript,
                Draft = draft,
                Clarifications = SilaVoicePoAgentRules.DeserializeClarifications(session.ClarificationJson),
                SpokenReply = session.SpokenReply,
                ValidationStatus = session.ValidationStatus,
                ValidationErrors = SilaVoicePoAgentRules.DeserializeErrors(session.ValidationErrors),
                IdempotencyKey = session.IdempotencyKey,
                CreatedPurchaseOrderId = session.CreatedPurchaseOrderId,
                CreatedPoNumber = session.CreatedPoNumber,
                CreatedOrders = ReadCreatedOrders(session),
                Language = session.Language,
                Turns = (turns ?? Array.Empty<SilaVoicePoTurn>())
                    .OrderBy(t => t.TurnNumber)
                    .Select(ToTurnDto)
                    .ToList(),
                DateUpdated = session.DateUpdated
            };
        }

        public static SilaVoicePoTurnDto ToTurnDto(SilaVoicePoTurn turn)
        {
            return new SilaVoicePoTurnDto
            {
                Id = turn.Id,
                TurnNumber = turn.TurnNumber,
                Role = turn.Role,
                Channel = turn.Channel,
                Transcript = turn.Transcript,
                SpokenText = turn.SpokenText,
                StructuredJson = turn.StructuredJson,
                TranscriptionStatus = turn.TranscriptionStatus,
                Confidence = turn.Confidence,
                DateCreated = turn.DateCreated
            };
        }

        public static void WriteDraft(SilaVoicePoSession session, SilaVoicePoDraftDto draft)
        {
            session.DraftJson = SilaVoicePoAgentRules.SerializeDraft(draft);
        }

        public static void WriteValidation(SilaVoicePoSession session, SilaVoicePoDraftValidation validation)
        {
            session.ValidationStatus = validation.Status;
            session.ValidationErrors = SilaVoicePoAgentRules.SerializeErrors(validation.Errors);
            session.ClarificationJson = SilaVoicePoAgentRules.SerializeClarifications(validation.Clarifications);
            if (validation.Status == SilaVoicePoAgentRules.VALIDATION_OK && session.Status is SilaVoicePoAgentRules.STATUS_OPEN or SilaVoicePoAgentRules.STATUS_REVIEW)
            {
                session.Status = SilaVoicePoAgentRules.STATUS_REVIEW;
            }
        }

        private static List<SilaVoicePoCreatedOrderDto> ReadCreatedOrders(SilaVoicePoSession session)
        {
            if (session.CreatedPurchaseOrderId == null)
            {
                return new();
            }

            return new List<SilaVoicePoCreatedOrderDto>
            {
                new()
                {
                    PurchaseOrderId = session.CreatedPurchaseOrderId.Value,
                    PoNumber = session.CreatedPoNumber
                }
            };
        }
    }
}
