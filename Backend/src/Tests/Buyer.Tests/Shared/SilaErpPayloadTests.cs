using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    /// <summary>ERP payloads are stored for the tracker with credentials masked; POS never fabricates SILA metadata as SAP JSON.</summary>
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
        public void SanitiseForPersist_KeepsLargeSapBodies()
        {
            string large = "{\"GoodsMovementCode\":\"03\",\"to_MaterialDocumentItem\":{\"results\":["
                + new string('a', 20_000)
                + "]}}";
            string? stored = SilaErpPayload.SanitiseForPersist(large);
            Assert.NotNull(stored);
            Assert.True(stored!.Length > SilaErpPayload.MAX_LENGTH);
            Assert.Contains("GoodsMovementCode", stored);
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
        public void RememberRequest_StoresRealSapBody_AndRejectsFabrication()
        {
            InventoryErpPosting posting = new InventoryErpPosting
            {
                Id = Guid.Parse("84052e1e-5474-4722-8f38-e02d75638da5"),
                ReferenceNumber = "100052",
                MovementType = "Z01"
            };

            string empty = SilaPosErpPayload.RememberRequest(posting, null);
            Assert.Equal(SilaPosErpPayload.NOT_CAPTURED, empty);
            Assert.Equal(SilaPosErpPayload.NOT_CAPTURED, posting.ErpRequestPayload);

            string fabricated = "{\"note\":\"SILA UI built this request view because technicalDetail was blank.\",\"saleId\":\"2d51e37d-8c1a-4c19-adfe-3a01f4dd65f4\",\"posCode\":\"34567\"}";
            Assert.False(SilaPosErpPayload.IsCapturedSapBody(fabricated));
            Assert.Equal(SilaPosErpPayload.NOT_CAPTURED, SilaPosErpPayload.RememberRequest(posting, fabricated));

            string sapBody = "{\"DocumentDate\":\"2026-11-20T00:00:00\",\"PostingDate\":\"2026-11-20T00:00:00\",\"GoodsMovementCode\":\"03\",\"MaterialDocumentHeaderText\":\"POSSALES2\",\"ReferenceDocument\":\"POS-FL10-100052\",\"to_MaterialDocumentItem\":{\"results\":[{\"Material\":\"000000000000100001\",\"Plant\":\"1060\",\"StorageLocation\":\"FL10\",\"GoodsMovementType\":\"Z01\",\"QuantityInEntryUnit\":\"30.000\",\"EntryUnit\":\"ML\",\"CostCenter\":\"1060050124\"},{\"Material\":\"000000000000100000\",\"Plant\":\"1060\",\"StorageLocation\":\"FL10\",\"GoodsMovementType\":\"Z01\",\"QuantityInEntryUnit\":\"60.000\",\"EntryUnit\":\"ML\",\"CostCenter\":\"1060050124\"}]}}";
            string kept = SilaPosErpPayload.RememberRequest(posting, sapBody);
            Assert.True(SilaPosErpPayload.IsCapturedSapBody(kept));
            Assert.Contains("QuantityInEntryUnit", kept);
            Assert.Contains("Z01", kept);
            Assert.DoesNotContain("saleId", kept);
            Assert.DoesNotContain("posCode", kept);
            Assert.Equal(kept, posting.ErpRequestPayload);
            Assert.Equal(SilaPosErpPayload.NOT_CAPTURED, SilaPosErpPayload.ReadCapturedRequest(new InventoryErpPosting { ErpRequestPayload = fabricated }));
        }

        [Fact]
        public void PosMovement_AlwaysZ01_NeverPreservesLegacyZ02()
        {
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaInventorySap.MovementForSale(SilaInventorySap.TYPE_Z02, 2m));
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaInventorySap.MovementForSale(null, 2m));
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaPosReversal.MovementForSale(SilaInventorySap.TYPE_Z02, null));
            Assert.Equal(SilaStockCountSap.API_CONFIG_NAME, SilaInventorySap.PosConsumption.ApiName);
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaInventorySap.PosConsumption.GoodsMovementType);
            Assert.Equal(SilaInventorySap.CODE_ISSUE, SilaInventorySap.PosConsumption.GoodsMovementCode);
        }
    }
}
