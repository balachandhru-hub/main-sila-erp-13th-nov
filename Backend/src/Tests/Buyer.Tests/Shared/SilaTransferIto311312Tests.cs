using System.Text.Json;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;
using Xunit;

namespace Buyer.Tests.Shared
{
    /// <summary>Contract checks for FIVE S4 - Transfer Stock 311 create body and 312 reversal body (no live SAP).</summary>
    public class SilaTransferIto311312Tests
    {
        [Fact]
        public void SamePlantTransfer_Uses_FIVE_S4_Transfer_Stock_311_Code04()
        {
            Assert.Equal("FIVE S4 - Transfer Stock", SilaInventorySap.SamePlantTransfer.ApiName);
            Assert.Equal("04", SilaInventorySap.SamePlantTransfer.GoodsMovementCode);
            Assert.Equal("311", SilaInventorySap.SamePlantTransfer.GoodsMovementType);
        }

        [Fact]
        public void SamePlantTransferReversal_Uses_FIVE_S4_Transfer_Stock_312_Code06()
        {
            Assert.Equal("FIVE S4 - Transfer Stock", SilaInventorySap.SamePlantTransferReversal.ApiName);
            Assert.Equal("06", SilaInventorySap.SamePlantTransferReversal.GoodsMovementCode);
            Assert.Equal("312", SilaInventorySap.SamePlantTransferReversal.GoodsMovementType);
        }

        [Fact]
        public void TryBuildTransferReversal_Swaps_Storage_Locations_And_Uses_312()
        {
            ItemBuyerMaster material = new ItemBuyerMaster
            {
                Id = Guid.NewGuid(),
                MaterialCode = "1000000",
                Description = "Test",
                BaseUnitOfMeasure = "EA",
                InventoryType = "STOCK",
                IsInventoryItem = true,
                IsActive = true
            };
            Dictionary<Guid, List<MaterialUomConversion>> conversions = new();
            string? problem = SilaInventorySap.TryBuildTransferReversal(
                new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                "1060",
                "CS01",
                "1060",
                "FL10",
                "1060",
                "1060",
                "ITO000025",
                new[] { new SilaInventorySap.TransferLine(material, 10m, "EA") },
                conversions,
                out string? body,
                out SilaInventorySap.Rule? rule);

            Assert.Null(problem);
            Assert.NotNull(rule);
            Assert.Equal("312", rule!.GoodsMovementType);
            Assert.Equal("06", rule.GoodsMovementCode);
            Assert.False(string.IsNullOrWhiteSpace(body));

            using JsonDocument json = JsonDocument.Parse(body!);
            Assert.Equal("06", json.RootElement.GetProperty("GoodsMovementCode").GetString());
            JsonElement item = json.RootElement.GetProperty("to_MaterialDocumentItem")[0];
            Assert.Equal("312", item.GetProperty("GoodsMovementType").GetString());
            Assert.Equal("FL10", item.GetProperty("StorageLocation").GetString());
            Assert.Equal("CS01", item.GetProperty("IssuingOrReceivingStorageLoc").GetString());
            Assert.Equal("1060", item.GetProperty("Plant").GetString());
        }

        [Fact]
        public void DecideTransfer_CrossPlant_DoesNotSelect_311()
        {
            string? problem = SilaInventorySap.DecideTransfer("1060", "CS01", "1070", "FL10", "1060", "1060", out SilaInventorySap.Rule? rule);
            Assert.Null(problem);
            Assert.NotNull(rule);
            Assert.Equal("301", rule!.GoodsMovementType);
            Assert.NotEqual("311", rule.GoodsMovementType);
        }
    }
}
