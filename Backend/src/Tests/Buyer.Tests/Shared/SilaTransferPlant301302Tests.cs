using System.Text.Json;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;
using Xunit;

namespace Buyer.Tests.Shared
{
    /// <summary>Contract checks for FIVE S4 - Plant Transfer Stock 301 / 302 (no live SAP).</summary>
    public class SilaTransferPlant301302Tests
    {
        [Fact]
        public void PlantTransfer_Uses_FIVE_S4_Plant_Transfer_Stock_301_Code04()
        {
            Assert.Equal("FIVE S4 - Plant Transfer Stock", SilaInventorySap.PlantTransfer.ApiName);
            Assert.Equal("04", SilaInventorySap.PlantTransfer.GoodsMovementCode);
            Assert.Equal("301", SilaInventorySap.PlantTransfer.GoodsMovementType);
            Assert.True(SilaInventorySap.PlantTransfer.ReceivingPlant);
        }

        [Fact]
        public void PlantTransferReversal_Uses_FIVE_S4_Plant_Transfer_Stock_302_Code06()
        {
            Assert.Equal("FIVE S4 - Plant Transfer Stock", SilaInventorySap.PlantTransferReversal.ApiName);
            Assert.Equal("06", SilaInventorySap.PlantTransferReversal.GoodsMovementCode);
            Assert.Equal("302", SilaInventorySap.PlantTransferReversal.GoodsMovementType);
            Assert.True(SilaInventorySap.PlantTransferReversal.ReceivingPlant);
        }

        [Fact]
        public void TryBuildTransfer_CrossCompany_Uses_301_With_Receiving_Plant()
        {
            ItemBuyerMaster material = Material();
            string? problem = SilaInventorySap.TryBuildTransfer(
                new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                "1060",
                "FL01",
                "1050",
                "FJ01",
                "1060",
                "1050",
                new[] { new SilaInventorySap.TransferLine(material, 10m, "EA") },
                new Dictionary<Guid, List<MaterialUomConversion>>(),
                out string? body,
                out SilaInventorySap.Rule? rule);

            Assert.Null(problem);
            Assert.Equal("301", rule!.GoodsMovementType);
            Assert.Equal(SilaInventorySap.API_PLANT_TRANSFER, rule.ApiName);
            using JsonDocument json = JsonDocument.Parse(body!);
            Assert.Equal("04", json.RootElement.GetProperty("GoodsMovementCode").GetString());
            JsonElement item = json.RootElement.GetProperty("to_MaterialDocumentItem")[0];
            Assert.Equal("301", item.GetProperty("GoodsMovementType").GetString());
            Assert.Equal("1060", item.GetProperty("Plant").GetString());
            Assert.Equal("FL01", item.GetProperty("StorageLocation").GetString());
            Assert.Equal("1050", item.GetProperty("IssuingOrReceivingPlant").GetString());
            Assert.Equal("FJ01", item.GetProperty("IssuingOrReceivingStorageLoc").GetString());
            Assert.Equal("10.000", item.GetProperty("QuantityInEntryUnit").GetString());
        }

        [Fact]
        public void TryBuildTransferReversal_CrossCompany_Uses_302_From_Destination()
        {
            ItemBuyerMaster material = Material();
            string? problem = SilaInventorySap.TryBuildTransferReversal(
                new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc),
                "1060",
                "FL01",
                "1050",
                "FJ01",
                "1060",
                "1050",
                "ITO000099",
                new[] { new SilaInventorySap.TransferLine(material, 2m, "EA") },
                new Dictionary<Guid, List<MaterialUomConversion>>(),
                out string? body,
                out SilaInventorySap.Rule? rule);

            Assert.Null(problem);
            Assert.Equal("302", rule!.GoodsMovementType);
            Assert.Equal("06", rule.GoodsMovementCode);
            Assert.Equal(SilaInventorySap.API_PLANT_TRANSFER, rule.ApiName);
            using JsonDocument json = JsonDocument.Parse(body!);
            Assert.Equal("06", json.RootElement.GetProperty("GoodsMovementCode").GetString());
            JsonElement item = json.RootElement.GetProperty("to_MaterialDocumentItem")[0];
            Assert.Equal("302", item.GetProperty("GoodsMovementType").GetString());
            Assert.Equal("1050", item.GetProperty("Plant").GetString());
            Assert.Equal("FJ01", item.GetProperty("StorageLocation").GetString());
            Assert.Equal("1060", item.GetProperty("IssuingOrReceivingPlant").GetString());
            Assert.Equal("FL01", item.GetProperty("IssuingOrReceivingStorageLoc").GetString());
            Assert.Equal("2.000", item.GetProperty("QuantityInEntryUnit").GetString());
        }

        private static ItemBuyerMaster Material()
        {
            return new ItemBuyerMaster
            {
                Id = Guid.NewGuid(),
                MaterialCode = "1000000",
                Description = "Test",
                BaseUnitOfMeasure = "EA",
                InventoryType = "STOCK",
                IsInventoryItem = true,
                IsActive = true
            };
        }
    }
}
