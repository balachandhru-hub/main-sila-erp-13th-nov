using Buyer.Application.Features.Shared;
using Xunit;

namespace Buyer.Tests.Shared
{
    /// <summary>Contract checks for Live Inventory stock-count variance Z01/Z02 and legacy 701/702 (no live SAP).</summary>
    public class SilaLiveStockCount701702Tests
    {
        [Fact]
        public void LiveStockCount_Uses_Z01_Z02_Material_Documents()
        {
            Assert.Equal("FIVE S4 - Positive Stock Adjustment", SilaInventorySap.ForDirection("IN").ApiName);
            Assert.Equal("Z01", SilaInventorySap.ForDirection("IN").GoodsMovementType);
            Assert.True(SilaInventorySap.ForDirection("IN").IncreasesStock);

            Assert.Equal("FIVE S4 - Goods Issue", SilaInventorySap.ForDirection("OUT").ApiName);
            Assert.Equal("Z02", SilaInventorySap.ForDirection("OUT").GoodsMovementType);
            Assert.False(SilaInventorySap.ForDirection("OUT").IncreasesStock);
        }

        [Fact]
        public void Waste_Uses_FIVE_STOCK_WASTE_UPDATE_Z01_And_Z02_Reverse()
        {
            Assert.Equal("FIVE_STOCK_WASTE_UPDATE", SilaInventorySap.WasteIssue.ApiName);
            Assert.Equal("Z01", SilaInventorySap.WasteIssue.GoodsMovementType);
            Assert.False(SilaInventorySap.WasteIssue.IncreasesStock);
            Assert.Equal(Buyer.Domain.Common.Common.SILA_DIRECTION_OUT, SilaInventorySap.DirectionForRule(SilaInventorySap.WasteIssue));

            Assert.Equal("Z02", SilaInventorySap.WasteReversal.GoodsMovementType);
            Assert.True(SilaInventorySap.WasteReversal.IncreasesStock);
            Assert.Equal(Buyer.Domain.Common.Common.SILA_DIRECTION_IN, SilaInventorySap.DirectionForRule(SilaInventorySap.WasteReversal));
        }

        [Fact]
        public void InventoryDifference_Uses_Positive_Adjustment_Api_With_701_And_702()
        {
            Assert.Equal("FIVE S4 - Positive Stock Adjustment", SilaInventorySap.InventoryDifferenceSurplus.ApiName);
            Assert.Equal("701", SilaInventorySap.InventoryDifferenceSurplus.GoodsMovementType);
            Assert.True(SilaInventorySap.InventoryDifferenceSurplus.IncreasesStock);

            Assert.Equal("FIVE S4 - Positive Stock Adjustment", SilaInventorySap.InventoryDifferenceShortage.ApiName);
            Assert.Equal("702", SilaInventorySap.InventoryDifferenceShortage.GoodsMovementType);
            Assert.False(SilaInventorySap.InventoryDifferenceShortage.IncreasesStock);
        }

        [Fact]
        public void ForInventoryDifference_Maps_Direction()
        {
            Assert.Equal("701", SilaInventorySap.ForInventoryDifference("IN").GoodsMovementType);
            Assert.Equal("702", SilaInventorySap.ForInventoryDifference("OUT").GoodsMovementType);
        }

        [Fact]
        public void PhysicalInventory_CreateBody_Includes_Material_Item()
        {
            string body = SilaInventoryDifferenceSap.BuildCreateBody(
                "1060", "FL01", "000000000001000000", "ADJ000001", new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));
            Assert.Contains("\"Plant\":\"1060\"", body, StringComparison.Ordinal);
            Assert.Contains("\"StorageLocation\":\"FL01\"", body, StringComparison.Ordinal);
            Assert.Contains("to_PhysicalInventoryDocumentItem", body, StringComparison.Ordinal);
            Assert.Contains("\"Material\":\"000000000001000000\"", body, StringComparison.Ordinal);
            Assert.DoesNotContain("PhysicalInventoryCountQuantity", body, StringComparison.Ordinal);
        }

        [Fact]
        public void PhysicalInventory_ItemBody_Links_Material()
        {
            string body = SilaInventoryDifferenceSap.BuildItemBody("2026", "100000191", "000000000001000000");
            Assert.Contains("\"FiscalYear\":\"2026\"", body, StringComparison.Ordinal);
            Assert.Contains("\"PhysicalInventoryDocument\":\"100000191\"", body, StringComparison.Ordinal);
            Assert.Contains("\"Material\":\"000000000001000000\"", body, StringComparison.Ordinal);
            Assert.Contains(SilaInventoryDifferenceSap.STOCK_TYPE_UNRESTRICTED, body, StringComparison.Ordinal);
        }

        [Fact]
        public void PhysicalInventory_CountPatch_Sets_Physical_Qty()
        {
            string body = SilaInventoryDifferenceSap.BuildCountPatchBody(
                "000000000001000000", 120m, "EA", new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));
            Assert.Contains("\"QuantityInUnitOfEntry\":\"120.000\"", body, StringComparison.Ordinal);
            Assert.Contains("\"UnitOfEntry\":\"EA\"", body, StringComparison.Ordinal);
            Assert.Contains("\"Material\":\"000000000001000000\"", body, StringComparison.Ordinal);
            Assert.Contains("PhysicalInventoryLastCountDate", body, StringComparison.Ordinal);
        }

        [Fact]
        public void PhysicalInventory_PostDifferencesPath_Quotes_Keys()
        {
            string path = SilaInventoryDifferenceSap.PostDifferencesPath(
                "2026", "100000123", "1", "000000000001000000", new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc));
            Assert.Contains("API_PHYSICAL_INVENTORY_DOC_SRV/PostDifferencesOnItem", path, StringComparison.Ordinal);
            Assert.Contains("FiscalYear='2026'", path, StringComparison.Ordinal);
            Assert.Contains("PhysicalInventoryDocument='100000123'", path, StringComparison.Ordinal);
            Assert.Contains("PhysicalInventoryDocumentItem='001'", path, StringComparison.Ordinal);
            Assert.Contains("Material='000000000001000000'", path, StringComparison.Ordinal);
            Assert.Contains("PostingDate=datetime'2026-11-21T00:00:00'", path, StringComparison.Ordinal);
        }

        [Fact]
        public void PhysicalInventory_TryReadDocument_From_OData_Envelope()
        {
            const string payload = "{\"d\":{\"FiscalYear\":\"2026\",\"PhysicalInventoryDocument\":\"100000999\"}}";
            Assert.True(SilaInventoryDifferenceSap.TryReadDocument(payload, out string year, out string doc));
            Assert.Equal("2026", year);
            Assert.Equal("100000999", doc);
        }

        [Fact]
        public void PhysicalInventory_TryReadActiveDocument_From_M7495()
        {
            const string payload = "Physical inventory for 1000000//1060/FL01 is already active in 100000191/2026/001";
            Assert.True(SilaInventoryDifferenceSap.TryReadActiveDocument(payload, out string year, out string doc, out string item));
            Assert.Equal("2026", year);
            Assert.Equal("100000191", doc);
            Assert.Equal("001", item);
        }
    }
}
