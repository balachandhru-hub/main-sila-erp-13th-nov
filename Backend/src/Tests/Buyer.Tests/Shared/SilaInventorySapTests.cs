using System.Text.Json;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    /// <summary>
    /// The one stock-posting decision: 311, 301, Z02 and Z01, with positive quantities and no invented units.
    /// </summary>
    public class SilaInventorySapTests
    {
        [Fact]
        public void DecideTransfer_Uses311ForTheSamePlantAnd301ForAnotherPlantInTheCompany()
        {
            string? same = SilaInventorySap.DecideTransfer("PL-A", "FROM", "PL-A", "TO", "1000", "1000", out SilaInventorySap.Rule? transfer);
            Assert.Null(same);
            Assert.Equal(SilaInventorySap.API_TRANSFER, transfer!.ApiName);
            Assert.Equal("04", transfer.GoodsMovementCode);
            Assert.Equal("311", transfer.GoodsMovementType);

            string? plant = SilaInventorySap.DecideTransfer("PL-A", "FROM", "PL-B", "FROM", "1000", "1000", out SilaInventorySap.Rule? other);
            Assert.Null(plant);
            Assert.Equal(SilaInventorySap.API_PLANT_TRANSFER, other!.ApiName);
            Assert.Equal("301", other.GoodsMovementType);
            Assert.True(other.ReceivingPlant);

            Assert.Equal(
                SilaInventorySap.CROSS_COMPANY_TRANSFER_NOT_CONFIGURED,
                SilaInventorySap.DecideTransfer("PL-A", "FROM", "PL-B", "TO", "1000", "2000", out _));
        }

        [Fact]
        public void Effect_AggregatesARecipeConsumptionAndKeepsTheFraction()
        {
            ItemBuyerMaster material = Stock("MAT-1", "BTL");
            List<SilaInventorySap.TransferLine> lines = new List<SilaInventorySap.TransferLine>
            {
                new SilaInventorySap.TransferLine(material, 0.100m, "BTL"),
                new SilaInventorySap.TransferLine(material, 0.150m, "BTL"),
                new SilaInventorySap.TransferLine(material, 0.150m, "BTL")
            };

            string? problem = SilaInventorySap.TryBuildEffect(
                new DateTime(2026, 10, 16),
                "PL-A",
                "BAR",
                SilaInventorySap.GoodsIssue,
                lines,
                new Dictionary<Guid, List<MaterialUomConversion>>(),
                out string? body,
                headerText: "POS~0833B2E7",
                costCenter: "CC-FL10");

            Assert.Null(problem);
            Assert.NotNull(body);
            using JsonDocument json = JsonDocument.Parse(body);
            JsonElement items = json.RootElement.GetProperty("to_MaterialDocumentItem").GetProperty("results");
            JsonElement line = items[0];
            Assert.Equal("03", json.RootElement.GetProperty("GoodsMovementCode").GetString());
            Assert.Equal("POS~0833B2E7", json.RootElement.GetProperty("MaterialDocumentHeaderText").GetString());
            Assert.Equal("S4 - Goods Issue", SilaInventorySap.API_GOODS_ISSUE);
            Assert.Equal(SilaInventorySap.API_GOODS_ISSUE, SilaInventorySap.GoodsIssue.ApiName);
            Assert.Contains(SilaInventorySap.API_GOODS_ISSUE, SilaInventorySap.GoodsIssueApiNames);
            Assert.Contains(SilaStockCountSap.API_CONFIG_NAME, SilaInventorySap.PosUpdateApiNames);
            Assert.Equal("FIVE_POS_UPDATE", SilaStockCountSap.API_CONFIG_NAME);
            Assert.Equal("Z02", line.GetProperty("GoodsMovementType").GetString());
            Assert.Equal("0.400", line.GetProperty("QuantityInEntryUnit").GetString());
            Assert.Equal("BTL", line.GetProperty("EntryUnit").GetString());
            Assert.Equal("PL-A", line.GetProperty("Plant").GetString());
            Assert.Equal("BAR", line.GetProperty("StorageLocation").GetString());
            Assert.Equal("CC-FL10", line.GetProperty("CostCenter").GetString());
            Assert.Equal(1, items.GetArrayLength());
            Assert.Equal(
                "sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader",
                SilaInventorySap.EnsureMaterialDocumentPath("sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV"));
            Assert.Equal("000000000000100001", SilaInventorySap.FormatSapMaterial("100001"));
        }

        [Fact]
        public void PosConsumption_UsesZ01ViaFivePosUpdate_WithoutHardcodedCostCenter()
        {
            ItemBuyerMaster materialA = Stock("1000001", "ML");
            ItemBuyerMaster materialB = Stock("1000000", "ML");
            string? problem = SilaInventorySap.TryBuildEffect(
                new DateTime(2026, 11, 20),
                "1060",
                "FL10",
                SilaInventorySap.PosConsumption,
                new[]
                {
                    new SilaInventorySap.TransferLine(materialA, 30m, "ML"),
                    new SilaInventorySap.TransferLine(materialB, 60m, "ML")
                },
                new Dictionary<Guid, List<MaterialUomConversion>>(),
                out string? body,
                headerText: SilaInventorySap.BuildPosHeaderText("2"),
                costCenter: "1060050124",
                referenceDocument: SilaInventorySap.BuildPosReferenceDocument("FL10", "100046"),
                postingDate: new DateTime(2026, 11, 21),
                keepEntryUnit: true);

            Assert.Null(problem);
            Assert.NotNull(body);
            using JsonDocument json = JsonDocument.Parse(body!);
            Assert.Equal("03", json.RootElement.GetProperty("GoodsMovementCode").GetString());
            Assert.Equal("2026-11-21T00:00:00", json.RootElement.GetProperty("PostingDate").GetString());
            Assert.Equal("2026-11-20T00:00:00", json.RootElement.GetProperty("DocumentDate").GetString());
            Assert.Equal("POSSALES2", json.RootElement.GetProperty("MaterialDocumentHeaderText").GetString());
            Assert.Equal("POS-FL10-100046", json.RootElement.GetProperty("ReferenceDocument").GetString());
            Assert.Equal(SilaStockCountSap.API_CONFIG_NAME, SilaInventorySap.PosConsumption.ApiName);
            Assert.Equal(SilaStockCountSap.API_CONFIG_NAME, SilaInventorySap.PosReversal.ApiName);
            Assert.False(SilaInventorySap.PosConsumption.IncreasesStock);
            Assert.True(SilaInventorySap.PosReversal.IncreasesStock);
            JsonElement items = json.RootElement.GetProperty("to_MaterialDocumentItem").GetProperty("results");
            Assert.Equal(2, items.GetArrayLength());
            Dictionary<string, JsonElement> byMaterial = items.EnumerateArray()
                .ToDictionary(x => x.GetProperty("Material").GetString()!, x => x);
            Assert.Equal("30.000", byMaterial[SilaInventorySap.FormatSapMaterial("1000001")].GetProperty("QuantityInEntryUnit").GetString());
            Assert.Equal("ML", byMaterial[SilaInventorySap.FormatSapMaterial("1000001")].GetProperty("EntryUnit").GetString());
            Assert.Equal("60.000", byMaterial[SilaInventorySap.FormatSapMaterial("1000000")].GetProperty("QuantityInEntryUnit").GetString());
            Assert.Equal("ML", byMaterial[SilaInventorySap.FormatSapMaterial("1000000")].GetProperty("EntryUnit").GetString());
            foreach (JsonElement line in items.EnumerateArray())
            {
                Assert.Equal("Z01", line.GetProperty("GoodsMovementType").GetString());
                Assert.Equal("1060", line.GetProperty("Plant").GetString());
                Assert.Equal("FL10", line.GetProperty("StorageLocation").GetString());
                Assert.Equal("1060050124", line.GetProperty("CostCenter").GetString());
            }

            Assert.Contains(
                "Storage Location Setup",
                SilaInventorySap.CostCenterMissingMessage("1060", "FL10"));
            Assert.DoesNotContain("1060050124", SilaInventorySap.CostCenterMissingMessage("1060", "FL10"));
        }

        [Fact]
        public void PreferRecipeEntryUnits_DropsBaseEaWhenMlPresent()
        {
            ItemBuyerMaster vodka = Stock("1000000", "EA");
            ItemBuyerMaster mixer = Stock("1000001", "EA");
            List<SilaInventorySap.TransferLine> mixed = new List<SilaInventorySap.TransferLine>
            {
                new SilaInventorySap.TransferLine(vodka, 0.320m, "EA"),
                new SilaInventorySap.TransferLine(vodka, 120m, "ML"),
                new SilaInventorySap.TransferLine(mixer, 0.080m, "EA"),
                new SilaInventorySap.TransferLine(mixer, 60m, "ML"),
            };

            List<SilaInventorySap.TransferLine> kept = SilaInventorySap.PreferRecipeEntryUnits(mixed);
            Assert.Equal(2, kept.Count);
            Assert.All(kept, x => Assert.Equal("ML", x.QuantityUom));
            Assert.Contains(kept, x => x.Material.Id == vodka.Id && x.Quantity == 120m);
            Assert.Contains(kept, x => x.Material.Id == mixer.Id && x.Quantity == 60m);

            string? problem = SilaInventorySap.TryBuildEffect(
                new DateTime(2026, 11, 20),
                "1060",
                "FL10",
                SilaInventorySap.PosConsumption,
                mixed,
                new Dictionary<Guid, List<MaterialUomConversion>>(),
                out string? body,
                headerText: SilaInventorySap.BuildPosHeaderText("1000533"),
                costCenter: "1060050124",
                referenceDocument: SilaInventorySap.BuildPosReferenceDocument("FL10", "1000533"),
                keepEntryUnit: true,
                allowMissingConversion: true);

            Assert.Null(problem);
            using JsonDocument json = JsonDocument.Parse(body!);
            Assert.Equal("POSSALES1000533", json.RootElement.GetProperty("MaterialDocumentHeaderText").GetString());
            Assert.Equal("POS-FL10-1000533", json.RootElement.GetProperty("ReferenceDocument").GetString());
            JsonElement items = json.RootElement.GetProperty("to_MaterialDocumentItem").GetProperty("results");
            Assert.Equal(2, items.GetArrayLength());
            foreach (JsonElement item in items.EnumerateArray())
            {
                Assert.Equal("ML", item.GetProperty("EntryUnit").GetString());
                Assert.Equal("Z01", item.GetProperty("GoodsMovementType").GetString());
            }
        }

        [Fact]
        public void PosConsumption_KeepsRecipeEntryUomInsteadOfMaterialBase()
        {
            // Material base is BTL; recipe ingredient is 30 ML. SAP must receive 60 ML for 2 EA, not 0.08 BTL.
            ItemBuyerMaster material = Stock("100000", "BTL");
            Dictionary<Guid, List<MaterialUomConversion>> conversions = new Dictionary<Guid, List<MaterialUomConversion>>
            {
                [material.Id] = new List<MaterialUomConversion>
                {
                    new MaterialUomConversion { MaterialId = material.Id, FromUom = "ML", ToUom = "BTL", Factor = 1m / 750m, IsActive = true }
                }
            };

            string? problem = SilaInventorySap.TryBuildEffect(
                new DateTime(2026, 11, 15),
                "1060",
                "FL10",
                SilaInventorySap.PosConsumption,
                new[] { new SilaInventorySap.TransferLine(material, 60m, "ML") },
                conversions,
                out string? body,
                costCenter: "1060050124",
                postingDate: new DateTime(2026, 11, 20),
                keepEntryUnit: true);

            Assert.Null(problem);
            using JsonDocument json = JsonDocument.Parse(body!);
            JsonElement line = json.RootElement.GetProperty("to_MaterialDocumentItem").GetProperty("results")[0];
            Assert.Equal("60.000", line.GetProperty("QuantityInEntryUnit").GetString());
            Assert.Equal("ML", line.GetProperty("EntryUnit").GetString());
            Assert.Equal("Z01", line.GetProperty("GoodsMovementType").GetString());
            Assert.Equal("2026-11-20T00:00:00", json.RootElement.GetProperty("PostingDate").GetString());
            Assert.Equal("2026-11-15T00:00:00", json.RootElement.GetProperty("DocumentDate").GetString());
        }

        [Fact]
        public void Effect_RoutesAPositiveCorrectionToZ01AndRefusesAMissingUnit()
        {
            // POS consumption (and returns) queue as Z01; API selection uses transaction type + IncreasesStock.
            Assert.Equal("Z01", SilaInventorySap.MovementForSale(null, 10m));
            Assert.Equal("Z01", SilaInventorySap.MovementForSale(null, -2m));
            Assert.Equal(SilaInventorySap.API_POSITIVE_ADJUSTMENT, SilaInventorySap.ForDirection(Common.SILA_DIRECTION_IN).ApiName);
            Assert.Equal(SilaInventorySap.API_GOODS_ISSUE, SilaInventorySap.ForDirection(Common.SILA_DIRECTION_OUT).ApiName);
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaInventorySap.PosConsumption.GoodsMovementType);
            Assert.Equal("03", SilaInventorySap.PosConsumption.GoodsMovementCode);

            ItemBuyerMaster material = Stock("MAT-1", "BTL");
            material.BaseUnitOfMeasure = "";
            Assert.Equal(
                SilaInventorySap.COST_CENTER_MISSING,
                SilaInventorySap.TryBuildEffect(
                    DateTime.UtcNow,
                    "PL-A",
                    "BAR",
                    SilaInventorySap.PositiveAdjustment,
                    new[] { new SilaInventorySap.TransferLine(Stock("MAT-2", "BTL"), 2m, "BTL") },
                    new Dictionary<Guid, List<MaterialUomConversion>>(),
                    out _));

            string? missing = SilaInventorySap.TryBuildEffect(
                DateTime.UtcNow,
                "PL-A",
                "BAR",
                SilaInventorySap.PositiveAdjustment,
                new[] { new SilaInventorySap.TransferLine(material, 2m, "BTL") },
                new Dictionary<Guid, List<MaterialUomConversion>>(),
                out string? body,
                costCenter: "CC-1");
            Assert.Equal(SilaInventorySap.UOM_CONVERSION_MISSING, missing);
            Assert.Null(body);
            Assert.Throws<ArgumentOutOfRangeException>(() => SilaInventorySap.FormatQuantity(-5m));
        }

        [Fact]
        public void Effect_ConvertsWithTheMaterialFactorAndDoesNotInventOne()
        {
            ItemBuyerMaster material = Stock("MAT-1", "BTL");
            Dictionary<Guid, List<MaterialUomConversion>> missing = new Dictionary<Guid, List<MaterialUomConversion>>();
            Assert.Equal(SilaInventorySap.UOM_CONVERSION_MISSING, SilaInventorySap.TryBuildEffect(
                DateTime.UtcNow, "PL-A", "BAR", SilaInventorySap.GoodsIssue,
                new[] { new SilaInventorySap.TransferLine(material, 300m, "ML") }, missing, out _, costCenter: "CC-1"));

            Dictionary<Guid, List<MaterialUomConversion>> conversions = new Dictionary<Guid, List<MaterialUomConversion>>
            {
                [material.Id] = new List<MaterialUomConversion>
                {
                    new MaterialUomConversion { MaterialId = material.Id, FromUom = "ML", ToUom = "BTL", Factor = 1m / 750m, IsActive = true }
                }
            };
            string? problem = SilaInventorySap.TryBuildEffect(
                new DateTime(2026, 10, 16), "PL-A", "BAR", SilaInventorySap.GoodsIssue,
                new[] { new SilaInventorySap.TransferLine(material, 300m, "ML") }, conversions, out string? body, costCenter: "CC-1");
            Assert.Null(problem);
            Assert.NotNull(body);
            using JsonDocument json = JsonDocument.Parse(body);
            JsonElement line = json.RootElement.GetProperty("to_MaterialDocumentItem").GetProperty("results")[0];
            Assert.Equal("0.400", line.GetProperty("QuantityInEntryUnit").GetString());
            Assert.Equal("BTL", line.GetProperty("EntryUnit").GetString());
        }

        private static ItemBuyerMaster Stock(string code, string unit)
        {
            return new ItemBuyerMaster
            {
                Id = Guid.NewGuid(),
                MaterialCode = code,
                BaseUnitOfMeasure = unit,
                IsInventoryItem = true,
                InventoryType = "STOCK",
                IsActive = true
            };
        }
    }
}
