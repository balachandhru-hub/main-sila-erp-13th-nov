using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    public class SilaProductClassifierTests
    {
        private static ItemBuyerMaster Item(string description, string? group = null) =>
            new ItemBuyerMaster { Description = description, MaterialGroup = group, ProductType = null, ValuationClass = null };

        [Theory]
        [InlineData("GIN BOMBAY SAPPHIRE 750ML", null, null, "BEVERAGE")]
        [InlineData("RED WINE MALBEC", "WINE", null, "BEVERAGE")]
        [InlineData("COCA COLA CAN 330ML", null, null, "BEVERAGE")]
        [InlineData("CHICKEN BREAST FROZEN", null, null, "FOOD")]
        [InlineData("BASMATI RICE 5KG", null, null, "FOOD")]
        [InlineData("MARLBORO RED CIGARETTES", null, null, "TOBACCO")]
        [InlineData("COHIBA CIGAR ROBUSTO", null, null, "TOBACCO")]
        [InlineData("JOHNNIE WALKER BLACK 50ML", null, null, "MINIBAR")]
        [InlineData("MINIBAR SNACK PACK", null, null, "MINIBAR")]
        [InlineData("HOUSE ITEM", null, "BEVERAGE", "BEVERAGE")]
        [InlineData("HOUSE ITEM", null, null, "GENERAL")]
        public void Infer_ClassifiesFromItemText(string description, string? group, string? storeCategory, string expected)
        {
            Assert.Equal(expected, SilaProductClassifier.Infer(Item(description, group), storeCategory, null));
        }

        [Fact]
        public void Infer_UsesMinibarLocationNameWhenTextIsNeutral()
        {
            Assert.Equal("MINIBAR", SilaProductClassifier.Infer(Item("HOUSE ITEM"), null, "Room 101 Mini Bar"));
        }

        [Theory]
        [InlineData("food", "FOOD")]
        [InlineData(" Minibar ", "MINIBAR")]
        [InlineData("ALL", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void Normalize_AcceptsOnlyKnownClasses(string? value, string? expected)
        {
            Assert.Equal(expected, SilaProductClassifier.Normalize(value));
        }
    }
}
