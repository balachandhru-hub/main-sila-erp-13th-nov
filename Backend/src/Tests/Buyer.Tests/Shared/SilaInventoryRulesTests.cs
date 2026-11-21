using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    public class SilaInventoryRulesTests
    {
        [Fact]
        public void SaleFactor_DividesAbsoluteQuantityByServingSize()
        {
            Assert.Equal(0.04m, SilaPosProcessing.SaleFactor(1, 0.04m));
            Assert.Equal(2m, SilaPosProcessing.SaleFactor(1, -2m));
            Assert.Equal(0.25m, SilaPosProcessing.SaleFactor(4, 1m));
        }

        [Fact]
        public void ScaleIngredient_KeepsRecipeUomForPosEaExplosion()
        {
            // Template: 2 EA of PosCode → recipe 1 EA = 30 ML of 100000 and 60 ML of 100001.
            decimal factor = SilaPosProcessing.SaleFactor(1, 2m);
            (decimal enteredA, string uomA, decimal baseA) = SilaPosProcessing.ScaleIngredient(30m, "ML", 0.03m, factor);
            (decimal enteredB, string uomB, decimal baseB) = SilaPosProcessing.ScaleIngredient(60m, "ml", 0.06m, factor);
            Assert.Equal(60m, enteredA);
            Assert.Equal("ML", uomA);
            Assert.Equal(0.06m, baseA);
            Assert.Equal(120m, enteredB);
            Assert.Equal("ML", uomB);
            Assert.Equal(0.12m, baseB);
        }

        [Theory]
        [InlineData(true, "STOCK", true)]
        [InlineData(true, "SERVICE", false)]
        [InlineData(true, "NON_STOCK", false)]
        [InlineData(false, "STOCK", false)]
        public void HoldsStock_RequiresInventoryItemAndStockType(bool inventoryItem, string inventoryType, bool expected)
        {
            ItemBuyerMaster material = new ItemBuyerMaster { IsInventoryItem = inventoryItem, InventoryType = inventoryType };
            Assert.Equal(expected, SilaMaterialRules.HoldsStock(material));
        }

        [Fact]
        public void Transferable_SubtractsSafetyStockWhenTransfersAreEnabled()
        {
            InventoryLocation open = new InventoryLocation { TransferEnabled = true };
            InventoryLocation closed = new InventoryLocation { TransferEnabled = false };
            Assert.Equal(7m, SilaStockLevels.Transferable(open, 10m, 3m));
            Assert.Equal(0m, SilaStockLevels.Transferable(open, 2m, 3m));
            Assert.Equal(0m, SilaStockLevels.Transferable(closed, 10m, 3m));
        }

        [Fact]
        public void UpdateStock_CoversPosConsumptionAndStockCount()
        {
            Assert.True(SilaStockCountSap.UsesUpdateStock("CONSUMPTION"));
            Assert.True(SilaStockCountSap.UsesUpdateStock("STOCK_COUNT"));
            Assert.Equal("Z02", SilaStockCountSap.GoodsMovementType("OUT"));
            Assert.Equal("Z01", SilaStockCountSap.GoodsMovementType("IN"));
            Assert.Equal("EH01", SilaPosMatching.Normalize("eh-01"));
        }
    }
}
