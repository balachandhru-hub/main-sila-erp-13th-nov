using Buyer.Application.Features.Shared;

namespace Buyer.Tests.VoicePo
{
    public class SilaUserPurchasingProfileRulesTests
    {
        [Theory]
        [InlineData("STOCK", false)]
        [InlineData("INVENTORY", false)]
        [InlineData("NON_STOCK", true)]
        [InlineData("SERVICE", true)]
        [InlineData("CONSUMABLE", true)]
        public void Account_assignment_required_only_for_consumables(string inventoryType, bool required)
        {
            Assert.Equal(required, SilaUserPurchasingProfileRules.RequiresAccountAssignment(inventoryType));
        }
    }
}
