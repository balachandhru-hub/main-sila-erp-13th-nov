using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaGoodsIssueCostCenterRulesTests
    {
        [Fact]
        public void ScopeConstants_ArePlantAndStorage()
        {
            Assert.Equal("PLANT", GoodsIssueCostCenter.SCOPE_PLANT);
            Assert.Equal("STORAGE", GoodsIssueCostCenter.SCOPE_STORAGE);
        }

        [Fact]
        public void MappingShape_SupportsPropertyPlantStorageCostCenter()
        {
            // User example: Property 1060, Plant 1060, Storage FL10, Cost center 1060050124
            GoodsIssueCostCenter row = new GoodsIssueCostCenter
            {
                Id = Guid.NewGuid(),
                BuyerId = Guid.NewGuid(),
                PropertyId = Guid.NewGuid(),
                PlantCode = "1060",
                Scope = GoodsIssueCostCenter.SCOPE_STORAGE,
                StorageLocationCode = "FL10",
                CostCenter = "1060050124",
                IsActive = true
            };

            Assert.Equal("1060", row.PlantCode);
            Assert.Equal("FL10", row.StorageLocationCode);
            Assert.Equal("1060050124", row.CostCenter);
            Assert.Equal(GoodsIssueCostCenter.SCOPE_STORAGE, row.Scope);
        }

        [Fact]
        public void PlantScope_UsesEmptyStorageLocationCode()
        {
            GoodsIssueCostCenter row = new GoodsIssueCostCenter
            {
                PlantCode = "1060",
                Scope = GoodsIssueCostCenter.SCOPE_PLANT,
                StorageLocationCode = string.Empty,
                CostCenter = "1060050124"
            };

            Assert.Equal(string.Empty, row.StorageLocationCode);
            Assert.Equal(GoodsIssueCostCenter.SCOPE_PLANT, row.Scope);
        }
    }
}
