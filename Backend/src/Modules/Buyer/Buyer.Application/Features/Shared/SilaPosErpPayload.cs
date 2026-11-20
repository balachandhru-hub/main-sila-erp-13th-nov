using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// POS → FIVE_POS_UPDATE exchange persistence for the Transaction Tracker.
    /// Stores only the final HTTP JSON body (or an explicit not-captured message) — never SILA sale metadata as a fake SAP payload.
    /// </summary>
    public static class SilaPosErpPayload
    {
        /// <summary>Shown when ErpRequestPayload was never captured at the HTTP boundary.</summary>
        public const string NOT_CAPTURED = "Actual SAP request was not captured for this attempt.";

        /// <summary>
        /// Returns the captured SAP request body for the tracker, or <see cref="NOT_CAPTURED"/>.
        /// Does not invent SILA metadata JSON and does not overwrite historical rows with a rebuilt guess.
        /// </summary>
        public static Task<string?> EnsureRequestAsync(
            IRepositoryWrapper? repository,
            Guid buyerId,
            PosSalesTransaction sale,
            InventoryErpPosting? posting,
            CancellationToken cancellationToken)
        {
            _ = repository;
            _ = buyerId;
            _ = sale;
            _ = cancellationToken;
            return Task.FromResult<string?>(ReadCapturedRequest(posting));
        }

        /// <summary>Captured SAP body, or the not-captured message. Never a fabricated sale metadata object.</summary>
        public static string ReadCapturedRequest(InventoryErpPosting? posting)
        {
            string? stored = posting?.ErpRequestPayload;
            if (IsCapturedSapBody(stored))
            {
                return Store(stored!) ?? stored!.Trim();
            }

            return NOT_CAPTURED;
        }

        /// <summary>
        /// True when the text is the real OData material-document JSON (or a partial SAP-shaped body),
        /// not a SILA diagnostic / UI-synthesized object.
        /// </summary>
        public static bool IsCapturedSapBody(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return false;
            }

            string text = body.Trim();
            if (string.Equals(text, NOT_CAPTURED, StringComparison.Ordinal))
            {
                return false;
            }

            // Reject prior fabricated diagnostics and UI-synthesized objects.
            if (text.Contains("SILA built this request", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SILA UI built this request", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SILA rebuilt this request", StringComparison.OrdinalIgnoreCase)
                || text.Contains("SILA recorded a diagnostic", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return text.Contains("to_MaterialDocumentItem", StringComparison.Ordinal)
                && text.Contains("GoodsMovementCode", StringComparison.Ordinal);
        }

        /// <summary>
        /// Persists the exact body that is about to be (or was) sent to SAP.
        /// Empty bodies become <see cref="NOT_CAPTURED"/> — never a SILA metadata JSON object.
        /// </summary>
        public static string RememberRequest(InventoryErpPosting posting, string? requestBody)
        {
            if (IsCapturedSapBody(requestBody))
            {
                string stored = Store(requestBody!) ?? requestBody!.Trim();
                posting.ErpRequestPayload = stored;
                return stored;
            }

            posting.ErpRequestPayload = NOT_CAPTURED;
            return NOT_CAPTURED;
        }

        /// <summary>Mask secrets; keep large SAP bodies (do not truncate the material-document JSON for tracker download).</summary>
        public static string? Store(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            return SilaErpPayload.SanitiseForPersist(body) ?? body.Trim();
        }

        /// <summary>Sanitise response for persistence without the short display truncate.</summary>
        public static string? StoreResponse(string? body)
        {
            return Store(body);
        }
    }
}
