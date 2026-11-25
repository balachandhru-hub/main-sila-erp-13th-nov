using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;

namespace Buyer.Tests.Shared
{
    public class SilaRecipeLegacyMigrationValidationTests
    {
        [Theory]
        [InlineData("STANDARD_RECIPE", true)]
        [InlineData("DIRECT_ITEM", true)]
        [InlineData("BATCH_RECIPE", true)]
        [InlineData("SUB_RECIPE", true)]
        [InlineData("COMBO", true)]
        [InlineData("VARIANT_RECIPE", true)]
        [InlineData("UNKNOWN", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsKnownFormat_AcceptsHospitalityFormats(string? format, bool expected)
        {
            Assert.Equal(expected, SilaRecipeLegacyMigration.Validation.IsKnownFormat(format));
        }

        [Theory]
        [InlineData("FOOD", true)]
        [InlineData("BEVERAGE", true)]
        [InlineData("MINIBAR", true)]
        [InlineData("TOBACCO", true)]
        [InlineData("OTHER", false)]
        public void IsKnownProductClass_AcceptsProductClasses(string? productClass, bool expected)
        {
            Assert.Equal(expected, SilaRecipeLegacyMigration.Validation.IsKnownProductClass(productClass));
        }

        [Fact]
        public void HasCircularSubRecipe_DetectsSimpleCycle()
        {
            Dictionary<string, IReadOnlyList<string>> adjacency = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["A"] = new List<string> { "B" },
                ["B"] = new List<string> { "A" }
            };

            Assert.True(SilaRecipeLegacyMigration.Validation.HasCircularSubRecipe("A", adjacency));
            Assert.Contains("A", SilaRecipeLegacyMigration.Validation.DetectCircularRecipes(adjacency));
        }

        [Fact]
        public void HasCircularSubRecipe_AllowsTree()
        {
            Dictionary<string, IReadOnlyList<string>> adjacency = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Parent"] = new List<string> { "Child" },
                ["Child"] = new List<string>()
            };

            Assert.False(SilaRecipeLegacyMigration.Validation.HasCircularSubRecipe("Parent", adjacency));
            Assert.Empty(SilaRecipeLegacyMigration.Validation.DetectCircularRecipes(adjacency));
        }

        [Theory]
        [InlineData(0, 0, false, false, Common.SILA_RECIPE_LEGACY_VALID)]
        [InlineData(0, 2, false, false, Common.SILA_RECIPE_LEGACY_WARNING)]
        [InlineData(1, 0, false, false, Common.SILA_RECIPE_LEGACY_INVALID)]
        [InlineData(0, 0, true, false, Common.SILA_RECIPE_LEGACY_DUPLICATE)]
        [InlineData(0, 0, false, true, Common.SILA_RECIPE_LEGACY_UNMAPPED)]
        public void AggregateStatus_MapsSeverity(int errors, int warnings, bool duplicate, bool unmapped, string expected)
        {
            Assert.Equal(expected, SilaRecipeLegacyMigration.Validation.AggregateStatus(errors, warnings, duplicate, unmapped));
        }

        [Theory]
        [InlineData("DIRECT_ITEM", null, Common.SILA_RECIPE_DIRECT)]
        [InlineData("BATCH_RECIPE", null, Common.SILA_RECIPE_BATCH)]
        [InlineData("SUB_RECIPE", null, Common.SILA_RECIPE_BATCH)]
        [InlineData("STANDARD_RECIPE", null, Common.SILA_RECIPE_RECIPE)]
        [InlineData(null, "MINIBAR", Common.SILA_RECIPE_DIRECT)]
        [InlineData(null, "FOOD", Common.SILA_RECIPE_RECIPE)]
        public void ItemModeFromFormat_MapsExpectedModes(string? format, string? productClass, string expected)
        {
            Assert.Equal(expected, SilaRecipeLegacyMigration.ItemModeFromFormat(format, productClass));
        }
    }
}
