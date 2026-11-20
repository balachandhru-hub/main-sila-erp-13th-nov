using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using SharedKernel.LoggerServices;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaHospitalityRulesTests
    {
        private readonly ILoggerManager _logger = new NullLoggerManager();

        [Fact]
        public void InferProductClass_LegacyDirect_IsMinibar()
        {
            Recipe recipe = new Recipe { ItemMode = Common.SILA_RECIPE_DIRECT };
            Assert.Equal(Common.SILA_PRODUCT_MINIBAR, SilaHospitalityRules.InferProductClass(recipe));
        }

        [Fact]
        public void InferProductClass_LegacyRecipe_IsFood()
        {
            Recipe recipe = new Recipe { ItemMode = Common.SILA_RECIPE_RECIPE };
            Assert.Equal(Common.SILA_PRODUCT_FOOD, SilaHospitalityRules.InferProductClass(recipe));
        }

        [Fact]
        public void InferProductClass_ExplicitBeverage_Wins()
        {
            Recipe recipe = new Recipe
            {
                ItemMode = Common.SILA_RECIPE_RECIPE,
                ProductClass = Common.SILA_PRODUCT_BEVERAGE
            };
            Assert.Equal(Common.SILA_PRODUCT_BEVERAGE, SilaHospitalityRules.InferProductClass(recipe));
        }

        [Theory]
        [InlineData(Common.SILA_RECIPE_DIRECT, "DIRECT_ITEM")]
        [InlineData(Common.SILA_RECIPE_BATCH, "BATCH_RECIPE")]
        [InlineData(Common.SILA_RECIPE_RECIPE, "STANDARD_RECIPE")]
        public void InferRecipeFormat_FromItemMode(string mode, string expected)
        {
            Assert.Equal(expected, SilaHospitalityRules.InferRecipeFormat(new Recipe { ItemMode = mode }));
        }

        [Fact]
        public void LossMultiplier_NoFactors_IsOne()
        {
            Assert.Equal(1m, SilaHospitalityRules.LossMultiplier(new Recipe()));
        }

        [Fact]
        public void LossMultiplier_CombinesTrimCookingWaste_Once()
        {
            // 10% trim, 5% cooking, 2% waste → 1.1 * 1.05 * 1.02 = 1.1781
            Recipe recipe = new Recipe
            {
                TrimLossPercent = 10m,
                CookingLossPercent = 5m,
                WasteFactorPercent = 2m
            };
            Assert.Equal(1.1781m, SilaHospitalityRules.LossMultiplier(recipe));
        }

        [Fact]
        public void DefaultItemMode_MinibarAndTobacco_AreDirect()
        {
            Assert.Equal(Common.SILA_RECIPE_DIRECT, SilaHospitalityRules.DefaultItemMode(Common.SILA_PRODUCT_MINIBAR));
            Assert.Equal(Common.SILA_RECIPE_DIRECT, SilaHospitalityRules.DefaultItemMode(Common.SILA_PRODUCT_TOBACCO));
            Assert.Equal(Common.SILA_RECIPE_RECIPE, SilaHospitalityRules.DefaultItemMode(Common.SILA_PRODUCT_FOOD));
            Assert.Equal(Common.SILA_RECIPE_RECIPE, SilaHospitalityRules.DefaultItemMode(Common.SILA_PRODUCT_BEVERAGE));
        }

        [Fact]
        public void NormalizeProductClass_AcceptsKnownValues()
        {
            Assert.Equal(Common.SILA_PRODUCT_FOOD, SilaHospitalityRules.NormalizeProductClass(_logger, "food"));
            Assert.Null(SilaHospitalityRules.NormalizeProductClass(_logger, null));
        }

        private sealed class NullLoggerManager : ILoggerManager
        {
            public void LogDebug(string message) { }
            public void LogError(string message) { }
            public void LogInfo(string message) { }
        }
    }
}
