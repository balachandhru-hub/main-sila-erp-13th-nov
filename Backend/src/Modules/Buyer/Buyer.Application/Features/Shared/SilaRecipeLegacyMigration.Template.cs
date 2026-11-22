using Buyer.Domain.Dtos;

namespace Buyer.Application.Features.Shared
{
    public static partial class SilaRecipeLegacyMigration
    {
        public const string SHEET_HEADER = "Recipe Header";
        public const string SHEET_INGREDIENTS = "Recipe Ingredients";
        public const string SHEET_INSTRUCTIONS = "Recipe Instructions";
        public const string SHEET_SUB_RECIPES = "Sub-Recipe References";
        public const string SHEET_VARIANTS = "Recipe Variants";
        public const string SHEET_REFERENCE = "Reference Values";

        public static readonly string[] HeaderColumns =
        {
            "Legacy Recipe ID", "Recipe Name", "Description", "Category", "Subcategory", "Recipe Format", "Product Class",
            "Yield Qty", "Yield UOM", "Portion Qty", "Portion UOM", "Prep Minutes", "Cook Minutes",
            "Serving Instructions", "Outlet Scope", "Active Status", "Legacy Reference", "Notes"
        };

        public static readonly string[] IngredientColumns =
        {
            "Legacy Recipe ID", "Ingredient Line", "Legacy Material Number", "Material Number", "Material Description",
            "Quantity", "UOM", "Notes"
        };

        public static readonly string[] InstructionColumns =
        {
            "Legacy Recipe ID", "Step Number", "Instruction Text"
        };

        public static readonly string[] SubRecipeColumns =
        {
            "Legacy Recipe ID", "Sub Legacy Recipe ID", "Quantity", "UOM"
        };

        public static readonly string[] VariantColumns =
        {
            "Legacy Recipe ID", "Variant Legacy Recipe ID", "Variant Name", "Notes"
        };

        public static readonly string[] ReferenceColumns =
        {
            "Type", "Code", "Description"
        };

        private static readonly string[] RequiredHeaderColumns = { "Legacy Recipe ID", "Recipe Name" };
        private static readonly string[] RequiredIngredientColumns = { "Legacy Recipe ID", "Quantity" };

        public static SilaRecipeFileDto BuildTemplate()
        {
            return SilaRecipeExcel.Build(
                "recipe-legacy-migration-template.xlsx",
                new SilaRecipeExcel.Sheet
                {
                    Name = SHEET_HEADER,
                    Headers = HeaderColumns,
                    Rows = new List<object?[]>
                    {
                        new object?[]
                        {
                            "LEG-001", "Classic Caesar Salad", "House caesar", "Food", "Salad", "STANDARD_RECIPE", "FOOD",
                            10, "EA", 1, "EA", 15, 0, "Toss and plate", "ALL", "ACTIVE", "LEG-001", ""
                        }
                    }
                },
                new SilaRecipeExcel.Sheet
                {
                    Name = SHEET_INGREDIENTS,
                    Headers = IngredientColumns,
                    Rows = new List<object?[]>
                    {
                        new object?[] { "LEG-001", 1, "OLD-ROM", "100001", "Romaine lettuce", 200, "G", "" },
                        new object?[] { "LEG-001", 2, "OLD-DRS", "100002", "Caesar dressing", 40, "ML", "" }
                    }
                },
                new SilaRecipeExcel.Sheet
                {
                    Name = SHEET_INSTRUCTIONS,
                    Headers = InstructionColumns,
                    Rows = new List<object?[]>
                    {
                        new object?[] { "LEG-001", 1, "Wash and chop lettuce." },
                        new object?[] { "LEG-001", 2, "Toss with dressing and plate." }
                    }
                },
                new SilaRecipeExcel.Sheet
                {
                    Name = SHEET_SUB_RECIPES,
                    Headers = SubRecipeColumns,
                    Rows = new List<object?[]>()
                },
                new SilaRecipeExcel.Sheet
                {
                    Name = SHEET_VARIANTS,
                    Headers = VariantColumns,
                    Rows = new List<object?[]>()
                },
                new SilaRecipeExcel.Sheet
                {
                    Name = SHEET_REFERENCE,
                    Headers = ReferenceColumns,
                    Rows = new List<object?[]>
                    {
                        new object?[] { "RecipeFormat", "STANDARD_RECIPE", "Standard plated recipe" },
                        new object?[] { "RecipeFormat", "DIRECT_ITEM", "Direct sell item" },
                        new object?[] { "RecipeFormat", "BATCH_RECIPE", "Batch production" },
                        new object?[] { "RecipeFormat", "SUB_RECIPE", "Sub-recipe / prep" },
                        new object?[] { "RecipeFormat", "COMBO", "Combo" },
                        new object?[] { "RecipeFormat", "VARIANT_RECIPE", "Variant" },
                        new object?[] { "ProductClass", "FOOD", "Food" },
                        new object?[] { "ProductClass", "BEVERAGE", "Beverage" },
                        new object?[] { "ProductClass", "MINIBAR", "Minibar" },
                        new object?[] { "ProductClass", "TOBACCO", "Tobacco" }
                    }
                });
        }
    }
}
