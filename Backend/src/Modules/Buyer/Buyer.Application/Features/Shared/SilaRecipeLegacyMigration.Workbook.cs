using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Staging of the recipe workbook format (sheets "Recipes" and "OutletPrices", the same file as the regular recipe
    /// import). Nothing is validated against the master data here: the rows are staged as they are and the Validate step
    /// of the batch reports what is wrong.
    /// </summary>
    public static partial class SilaRecipeLegacyMigration
    {
        private const int MAX_LEGACY_ID = 100;

        public static bool IsWorkbookFormat(IReadOnlyList<string> sheetNames)
        {
            bool hasLegacyHeader = sheetNames.Any(x => string.Equals(x, SHEET_HEADER, StringComparison.OrdinalIgnoreCase));
            bool hasRecipes = sheetNames.Any(x => string.Equals(x, SilaRecipeImport.SHEET_RECIPES, StringComparison.OrdinalIgnoreCase));
            return !hasLegacyHeader && hasRecipes;
        }

        public static string? RecipeFormatOfItemMode(string? itemMode)
        {
            string? mode = itemMode?.Trim().ToUpperInvariant().Replace('-', '_').Replace(' ', '_');
            return mode switch
            {
                null or "" => null,
                Common.SILA_RECIPE_DIRECT => SilaHospitalityRules.FORMAT_DIRECT,
                Common.SILA_RECIPE_RECIPE => SilaHospitalityRules.FORMAT_STANDARD,
                Common.SILA_RECIPE_BATCH => SilaHospitalityRules.FORMAT_BATCH,
                "SUB_RECIPE" or "SUBRECIPE" => SilaHospitalityRules.FORMAT_SUB,
                _ => mode
            };
        }

        public static (List<RecipeLegacyRecipe> Staged, List<RecipeLegacyMaterialMap> Maps) StageWorkbookFormat(
            ILoggerManager logger, string? fileName, byte[] content, Guid buyerId)
        {
            List<Dictionary<string, string>> recipeRows = SilaRecipeExcel.ReadSheet(logger, fileName, content, SilaRecipeImport.SHEET_RECIPES, new[] { "Name" });
            if (recipeRows.Count == 0)
            {
                throw new BadRequestCustomException("No recipes found.", $"Add at least one row to the {SilaRecipeImport.SHEET_RECIPES} sheet.");
            }

            List<Dictionary<string, string>> priceRows = SilaRecipeExcel.ReadSheet(logger, fileName, content, SilaRecipeImport.SHEET_PRICES, new[] { "OutletCode", "MenuPrice" });

            Guid batchId = Guid.NewGuid();
            Dictionary<string, List<Dictionary<string, string>>> rowsByKey = new Dictionary<string, List<Dictionary<string, string>>>(StringComparer.OrdinalIgnoreCase);
            List<string> order = new List<string>();
            foreach (Dictionary<string, string> row in recipeRows)
            {
                string? key = WorkbookKey(row);
                if (key == null) continue;
                if (!rowsByKey.TryGetValue(key, out List<Dictionary<string, string>>? list))
                {
                    list = new List<Dictionary<string, string>>();
                    rowsByKey[key] = list;
                    order.Add(key);
                }

                list.Add(row);
            }

            Dictionary<string, List<SilaRecipeLegacyOutletPriceDto>> pricesByKey = new Dictionary<string, List<SilaRecipeLegacyOutletPriceDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, string> row in priceRows)
            {
                string? key = WorkbookKey(row);
                if (key == null || !rowsByKey.ContainsKey(key)) continue;
                if (!pricesByKey.TryGetValue(key, out List<SilaRecipeLegacyOutletPriceDto>? list))
                {
                    list = new List<SilaRecipeLegacyOutletPriceDto>();
                    pricesByKey[key] = list;
                }

                list.Add(new SilaRecipeLegacyOutletPriceDto
                {
                    OutletCode = SilaRecipeExcel.Value(row, "OutletCode")?.ToUpperInvariant(),
                    MenuPrice = ParseDecimal(SilaRecipeExcel.Value(row, "MenuPrice")),
                    Currency = SilaRecipeExcel.Value(row, "Currency")?.ToUpperInvariant(),
                    Row = SilaRecipeExcel.RowNumber(row)
                });
            }

            List<RecipeLegacyRecipe> staged = new List<RecipeLegacyRecipe>();
            List<RecipeLegacyMaterialMap> maps = new List<RecipeLegacyMaterialMap>();
            foreach (string key in order)
            {
                List<Dictionary<string, string>> rows = rowsByKey[key];
                Dictionary<string, string> first = rows[0];
                string? recipeCode = SilaRecipeExcel.Value(first, "RecipeCode");
                string name = rows.Select(x => SilaRecipeExcel.Value(x, "Name")).FirstOrDefault(x => x != null) ?? recipeCode ?? key;
                string legacyId = Truncate(recipeCode ?? name, MAX_LEGACY_ID)!;

                List<SilaRecipeLegacyIngredientDto> ingredients = new List<SilaRecipeLegacyIngredientDto>();
                List<SilaRecipeLegacySubRecipeDto> subs = new List<SilaRecipeLegacySubRecipeDto>();
                foreach (Dictionary<string, string> row in rows)
                {
                    string? materialCode = SilaRecipeExcel.Value(row, "MaterialCode");
                    string? subCode = SilaRecipeExcel.Value(row, "SubRecipeCode");
                    string? uom = SilaRecipeExcel.Value(row, "Uom")?.ToUpperInvariant();
                    decimal? quantity = ParseDecimal(SilaRecipeExcel.Value(row, "Quantity"));
                    if (materialCode != null)
                    {
                        ingredients.Add(new SilaRecipeLegacyIngredientDto
                        {
                            Line = ingredients.Count + 1,
                            MaterialNumber = materialCode,
                            Quantity = quantity ?? 0m,
                            Uom = uom
                        });
                    }
                    else if (subCode != null)
                    {
                        subs.Add(new SilaRecipeLegacySubRecipeDto { SubLegacyRecipeId = subCode, Quantity = quantity ?? 0m, Uom = uom });
                    }
                }

                string? family = SilaRecipeExcel.Value(first, "Family");
                List<SilaRecipeLegacyOutletPriceDto> prices = pricesByKey.TryGetValue(key, out List<SilaRecipeLegacyOutletPriceDto>? foundPrices)
                    ? foundPrices
                    : new List<SilaRecipeLegacyOutletPriceDto>();
                SilaRecipeLegacyExtrasDto extras = new SilaRecipeLegacyExtrasDto
                {
                    FamilyCode = family?.ToUpperInvariant(),
                    CategoryCode = SilaRecipeExcel.Value(first, "Category")?.ToUpperInvariant(),
                    PosCode = SilaRecipeExcel.Value(first, "PosCode"),
                    SellingUom = SilaRecipeExcel.Value(first, "SellingUom")?.ToUpperInvariant(),
                    Currency = SilaRecipeExcel.Value(first, "Currency")?.ToUpperInvariant(),
                    OutletPrices = prices
                };

                string? productClass = family?.ToUpperInvariant();
                RecipeLegacyRecipe recipe = new RecipeLegacyRecipe
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    BuyerId = buyerId,
                    LegacyRecipeId = legacyId,
                    RecipeName = Truncate(name, 200)!,
                    Description = Truncate(SilaRecipeExcel.Value(first, "Description"), 1000),
                    Category = Truncate(SilaRecipeExcel.Value(first, "Category"), 100),
                    RecipeFormat = Truncate(RecipeFormatOfItemMode(SilaRecipeExcel.Value(first, "ItemMode")), 30),
                    ProductClass = productClass != null && Validation.IsKnownProductClass(productClass) ? productClass : null,
                    PortionQty = ParseDecimal(SilaRecipeExcel.Value(first, "ServingQty")),
                    PortionUom = Truncate(SilaRecipeExcel.Value(first, "ServingUom")?.ToUpperInvariant(), 20),
                    OutletScope = Truncate(string.Join(", ", prices.Select(x => x.OutletCode).Where(x => x != null).Distinct()), 200),
                    ActiveStatus = Truncate(SilaRecipeExcel.Value(first, "Status"), 30),
                    LegacyReference = Truncate(recipeCode ?? legacyId, 100),
                    IngredientsJson = Serialize(ingredients),
                    InstructionsJson = Serialize(new List<string>()),
                    SubRecipesJson = Serialize(subs),
                    VariantsJson = Serialize(new List<SilaRecipeLegacyVariantDto>()),
                    ExtrasJson = Serialize(extras),
                    ValidationStatus = Common.SILA_RECIPE_LEGACY_UNMAPPED,
                    PostingStatus = Common.SILA_POSTING_PENDING,
                    SourceRow = SilaRecipeExcel.RowNumber(first),
                    IsSelected = true,
                    IsActive = true
                };
                staged.Add(recipe);

                foreach (SilaRecipeLegacyIngredientDto ingredient in ingredients)
                {
                    maps.Add(new RecipeLegacyMaterialMap
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batchId,
                        BuyerId = buyerId,
                        LegacyRecipeId = legacyId,
                        IngredientLine = ingredient.Line,
                        MaterialNumber = ingredient.MaterialNumber,
                        Status = Common.SILA_RECIPE_LEGACY_UNMAPPED,
                        IsActive = true
                    });
                }
            }

            if (staged.Count == 0)
            {
                throw new BadRequestCustomException("No valid recipes found.", "Each row of the Recipes sheet needs a RecipeCode or a Name.");
            }

            return (staged, maps);
        }

        public static SilaRecipeLegacyExtrasDto? ExtrasOf(RecipeLegacyRecipe recipe)
        {
            return string.IsNullOrWhiteSpace(recipe.ExtrasJson) ? null : Deserialize<SilaRecipeLegacyExtrasDto>(recipe.ExtrasJson);
        }

        private static string? WorkbookKey(Dictionary<string, string> row)
        {
            string? code = SilaRecipeExcel.Value(row, "RecipeCode");
            if (code != null) return code.ToUpperInvariant();
            string? name = SilaRecipeExcel.Value(row, "Name");
            return name == null ? null : $"NEW:{name.ToUpperInvariant()}";
        }

        private static string? Truncate(string? value, int length)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return value.Length <= length ? value : value[..length];
        }
    }
}
