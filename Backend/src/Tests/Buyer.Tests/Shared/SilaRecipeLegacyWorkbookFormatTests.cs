using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Tests.Support;

namespace Buyer.Tests.Shared
{
    public class SilaRecipeLegacyWorkbookFormatTests
    {
        private static readonly string[] RecipeHeaders =
        {
            "RecipeCode", "Name", "Family", "Category", "ItemMode", "ServingQty", "ServingUom", "SellingUom", "PosCode", "Currency",
            "Description", "MaterialCode", "SubRecipeCode", "Quantity", "Uom", "Version", "Status"
        };

        [Theory]
        [InlineData(new[] { "Recipes", "OutletPrices" }, true)]
        [InlineData(new[] { "recipes" }, true)]
        [InlineData(new[] { "Recipe Header", "Recipes" }, false)]
        [InlineData(new[] { "Recipe Header", "Recipe Ingredients" }, false)]
        [InlineData(new[] { "Foo" }, false)]
        public void IsWorkbookFormat_NeedsRecipesSheetWithoutLegacyHeader(string[] sheets, bool expected)
        {
            Assert.Equal(expected, SilaRecipeLegacyMigration.IsWorkbookFormat(sheets));
        }

        [Theory]
        [InlineData("DIRECT", "DIRECT_ITEM")]
        [InlineData("recipe", "STANDARD_RECIPE")]
        [InlineData("BATCH", "BATCH_RECIPE")]
        [InlineData("SUB-RECIPE", "SUB_RECIPE")]
        [InlineData("Sub Recipe", "SUB_RECIPE")]
        [InlineData("WHATEVER", "WHATEVER")]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void RecipeFormatOfItemMode_MapsItemModeToFormat(string? itemMode, string? expected)
        {
            Assert.Equal(expected, SilaRecipeLegacyMigration.RecipeFormatOfItemMode(itemMode));
        }

        [Fact]
        public void StageWorkbookFormat_GroupsRowsPerRecipeAndKeepsExtras()
        {
            SilaRecipeExcel.Sheet recipes = new SilaRecipeExcel.Sheet { Name = "Recipes", Headers = RecipeHeaders };
            recipes.Rows.Add(new object?[] { "SUB-A", "Prep A", "FOOD", "PREP", "SUB-RECIPE", 1000m, "G", "G", null, "AED", "desc", "1001", null, 750m, "G", null, null });
            recipes.Rows.Add(new object?[] { "SUB-A", "Prep A", "FOOD", "PREP", "SUB-RECIPE", 1000m, "G", "G", null, "AED", "desc", "1002", null, 250m, "G", null, null });
            recipes.Rows.Add(new object?[] { null, "Main dish", "FOOD", "MAIN", "BATCH", 1m, "EA", "EA", "POS-9", "AED", null, "1003", null, 2m, "PC", null, null });
            recipes.Rows.Add(new object?[] { null, "Main dish", "FOOD", "MAIN", "BATCH", 1m, "EA", "EA", "POS-9", "AED", null, null, "SUB-A", 100m, "G", null, null });
            SilaRecipeExcel.Sheet prices = new SilaRecipeExcel.Sheet
            {
                Name = "OutletPrices",
                Headers = new[] { "RecipeCode", "Name", "OutletCode", "MenuPrice", "Currency" }
            };
            prices.Rows.Add(new object?[] { null, "Main dish", "fl17", 49m, "aed" });
            SilaRecipeFileDto file = SilaRecipeExcel.Build("t.xlsx", recipes, prices);

            (List<RecipeLegacyRecipe> staged, List<RecipeLegacyMaterialMap> maps) =
                SilaRecipeLegacyMigration.StageWorkbookFormat(new FakeLogger(), "t.xlsx", file.Content, Guid.NewGuid());

            Assert.Equal(2, staged.Count);
            Assert.Equal(3, maps.Count);

            RecipeLegacyRecipe prep = staged.Single(x => x.LegacyRecipeId == "SUB-A");
            Assert.Equal("SUB_RECIPE", prep.RecipeFormat);
            Assert.Equal("FOOD", prep.ProductClass);
            Assert.Equal(1000m, prep.PortionQty);
            Assert.Equal("G", prep.PortionUom);
            List<SilaRecipeLegacyIngredientDto> prepLines = SilaRecipeLegacyMigration.Deserialize<List<SilaRecipeLegacyIngredientDto>>(prep.IngredientsJson)!;
            Assert.Equal(new[] { "1001", "1002" }, prepLines.Select(x => x.MaterialNumber).ToArray());

            RecipeLegacyRecipe main = staged.Single(x => x.LegacyRecipeId == "Main dish");
            Assert.Equal("BATCH_RECIPE", main.RecipeFormat);
            List<SilaRecipeLegacySubRecipeDto> subs = SilaRecipeLegacyMigration.Deserialize<List<SilaRecipeLegacySubRecipeDto>>(main.SubRecipesJson)!;
            Assert.Equal("SUB-A", Assert.Single(subs).SubLegacyRecipeId);
            Assert.Equal(100m, subs[0].Quantity);

            SilaRecipeLegacyExtrasDto extras = SilaRecipeLegacyMigration.ExtrasOf(main)!;
            Assert.Equal("POS-9", extras.PosCode);
            Assert.Equal("MAIN", extras.CategoryCode);
            SilaRecipeLegacyOutletPriceDto price = Assert.Single(extras.OutletPrices);
            Assert.Equal("FL17", price.OutletCode);
            Assert.Equal(49m, price.MenuPrice);
            Assert.Equal("FL17", main.OutletScope);
        }

        [Fact]
        public void StageWorkbookFormat_StagesInvalidValuesWithoutRejecting()
        {
            SilaRecipeExcel.Sheet recipes = new SilaRecipeExcel.Sheet { Name = "Recipes", Headers = RecipeHeaders };
            recipes.Rows.Add(new object?[] { null, "Odd one", "NOPE", null, "SOMETHING", null, null, null, null, null, null, "1001", null, null, null, null, null });
            SilaRecipeFileDto file = SilaRecipeExcel.Build("t.xlsx", recipes);

            (List<RecipeLegacyRecipe> staged, _) =
                SilaRecipeLegacyMigration.StageWorkbookFormat(new FakeLogger(), "t.xlsx", file.Content, Guid.NewGuid());

            RecipeLegacyRecipe recipe = Assert.Single(staged);
            Assert.Equal("SOMETHING", recipe.RecipeFormat);
            Assert.Null(recipe.ProductClass);
            Assert.Null(recipe.PortionQty);
            Assert.Equal(0m, SilaRecipeLegacyMigration.Deserialize<List<SilaRecipeLegacyIngredientDto>>(recipe.IngredientsJson)![0].Quantity);
        }
    }
}
