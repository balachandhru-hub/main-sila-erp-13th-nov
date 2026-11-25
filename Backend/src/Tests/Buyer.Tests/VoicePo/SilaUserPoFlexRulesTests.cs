using Buyer.Application.Features.Shared;
using Buyer.Domain.Entities;

namespace Buyer.Tests.VoicePo
{
    public class SilaUserPoFlexRulesTests
    {
        [Fact]
        public void Condition_EQ_sets_then_param()
        {
            List<SilaUserPoFlexParam> parameters =
            [
                new() { ParamKey = "AccountAssignmentCategory", ParamValue = "K", SortOrder = 1, IsActive = true },
                new() { ParamKey = "CompanyCode", ParamValue = "1060", SortOrder = 0, IsActive = true }
            ];
            List<SilaUserPoFlexCondition> conditions =
            [
                new()
                {
                    WhenParam = "AccountAssignmentCategory",
                    WhenOperator = "EQ",
                    WhenValue = "K",
                    ThenParam = "CostCenter",
                    ThenValue = "1060010108",
                    SortOrder = 0,
                    IsActive = true
                },
                new()
                {
                    WhenParam = "AccountAssignmentCategory",
                    WhenOperator = "EQ",
                    WhenValue = "K",
                    ThenParam = "GlAccount",
                    ThenValue = "3250000109",
                    SortOrder = 1,
                    IsActive = true
                }
            ];

            Dictionary<string, string?> bag = SilaUserPoFlexRules.BuildParameterBag(parameters, conditions);
            Assert.Equal("1060", bag["CompanyCode"]);
            Assert.Equal("K", bag["AccountAssignmentCategory"]);
            Assert.Equal("1060010108", bag["CostCenter"]);
            Assert.Equal("3250000109", bag["GlAccount"]);
        }

        [Fact]
        public void Canonical_key_aliases_normalize()
        {
            Assert.Equal("CompanyCode", SilaUserPoFlexRules.CanonicalKey("company code"));
            Assert.Equal("StorageLocation", SilaUserPoFlexRules.CanonicalKey("SLoc"));
            Assert.Equal("PurchasingOrganization", SilaUserPoFlexRules.CanonicalKey("POrg"));
        }
    }
}
