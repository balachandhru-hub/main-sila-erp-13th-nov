using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    /// <summary>ERP payloads are stored for the tracker with credentials masked and truncated.</summary>
    public class SilaErpPayloadTests
    {
        [Fact]
        public void Sanitise_MasksJsonFormAndXmlSecrets()
        {
            string json = SilaErpPayload.Sanitise("{\"user\":\"sap\",\"password\":\"p@ss\\\"word\",\"api_key\": 12345,\"qty\":2}")!;
            Assert.Contains("\"password\":\"***\"", json);
            Assert.Contains("\"api_key\": \"***\"", json);
            Assert.Contains("\"user\":\"sap\"", json);
            Assert.Contains("\"qty\":2", json);
            Assert.DoesNotContain("p@ss", json);

            Assert.Equal("grant=client&client_secret=***&scope=x", SilaErpPayload.Sanitise("grant=client&client_secret=abc&scope=x"));
            Assert.Equal("<a><Token>***</Token><b>1</b></a>", SilaErpPayload.Sanitise("<a><Token>xyz</Token><b>1</b></a>"));
        }

        [Fact]
        public void Sanitise_TruncatesAndKeepsEmptyAsNull()
        {
            Assert.Null(SilaErpPayload.Sanitise(null));
            Assert.Null(SilaErpPayload.Sanitise(string.Empty));
            Assert.Equal(SilaErpPayload.MAX_LENGTH, SilaErpPayload.Sanitise(new string('x', 10000))!.Length);
        }

        [Fact]
        public void HttpBodyFromError_ExtractsJsonAfterHttpStatus()
        {
            string error = "The ERP returned HTTP 400. {\"error\":{\"code\":\"MM_IM_ODATA_API_MDOC/014\",\"message\":{\"value\":\"Material Document processing failed\"}}}";
            string? body = SilaErpPayload.HttpBodyFromError(error);
            Assert.NotNull(body);
            Assert.StartsWith("{\"error\"", body);
            Assert.Contains("MM_IM_ODATA_API_MDOC/014", body);
            Assert.Null(SilaErpPayload.HttpBodyFromError("The ERP could not be reached."));
            Assert.Null(SilaErpPayload.HttpBodyFromError(null));
        }

        [Fact]
        public void RememberRequest_NeverLeavesPosPostingBlank()
        {
            InventoryErpPosting posting = new InventoryErpPosting
            {
                Id = Guid.Parse("a2e2e805-f94a-4852-86c5-451fa8032536"),
                ReferenceNumber = "100046",
                MovementType = "Z01"
            };
            string stored = SilaPosErpPayload.RememberRequest(posting, null);
            Assert.False(string.IsNullOrWhiteSpace(stored));
            Assert.Equal(stored, posting.ErpRequestPayload);
            Assert.Contains("a2e2e805-f94a-4852-86c5-451fa8032536", stored, StringComparison.OrdinalIgnoreCase);

            string sapBody = "{\"PostingDate\":\"2026-11-20T00:00:00\",\"GoodsMovementCode\":\"03\",\"to_MaterialDocumentItem\":{\"results\":[{\"Material\":\"000000000000100000\",\"QuantityInEntryUnit\":\"60.000\",\"EntryUnit\":\"ML\",\"GoodsMovementType\":\"Z01\"}]}}";
            string kept = SilaPosErpPayload.RememberRequest(posting, sapBody);
            Assert.Contains("QuantityInEntryUnit", kept);
            Assert.Contains("ML", kept);
            Assert.Equal(kept, posting.ErpRequestPayload);
        }
    }
}
