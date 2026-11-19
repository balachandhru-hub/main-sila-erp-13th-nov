using Buyer.Application.Features.Commands.AlignIntegrationConnection;

namespace Buyer.Tests.Shared
{
    public class AlignIntegrationConnectionTests
    {
        [Fact]
        public void NormalizeResourcePath_AddsMaterialDocumentEntity()
        {
            Assert.Equal(
                "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
                AlignIntegrationConnectionCommandHandler.NormalizeResourcePath(
                    "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV",
                    "POST"));
        }

        [Fact]
        public void NormalizeResourcePath_KeepsExistingEntity()
        {
            string path = "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader";
            Assert.Equal(path, AlignIntegrationConnectionCommandHandler.NormalizeResourcePath(path, "POST"));
        }

        [Fact]
        public void MergeHeaders_AddsCsrfAndContentTypeForPost()
        {
            string? json = AlignIntegrationConnectionCommandHandler.MergeHeaders(
                "{\"Accept\":\"application/json\",\"DataServiceVersion\":\"2.0\"}",
                null,
                "POST");
            Assert.Contains("x-csrf-token", json);
            Assert.Contains("Fetch", json);
            Assert.Contains("Content-Type", json);
            Assert.Contains("DataServiceVersion", json);
        }
    }
}
