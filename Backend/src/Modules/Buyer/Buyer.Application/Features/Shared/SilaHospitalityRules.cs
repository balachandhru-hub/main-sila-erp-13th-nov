using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Hospitality product class / recipe format helpers for Recipe Studio.
    /// Extends the existing recipe engine — does not replace ItemMode (DIRECT|RECIPE|BATCH).
    /// </summary>
    public static class SilaHospitalityRules
    {
        public const string FORMAT_STANDARD = "STANDARD_RECIPE";
        public const string FORMAT_DIRECT = "DIRECT_ITEM";
        public const string FORMAT_BATCH = "BATCH_RECIPE";
        public const string FORMAT_SUB = "SUB_RECIPE";
        public const string FORMAT_COMBO = "COMBO";
        public const string FORMAT_VARIANT = "VARIANT_RECIPE";

        private static readonly string[] ProductClasses =
        {
            Common.SILA_PRODUCT_FOOD,
            Common.SILA_PRODUCT_BEVERAGE,
            Common.SILA_PRODUCT_MINIBAR,
            Common.SILA_PRODUCT_TOBACCO
        };

        private static readonly string[] RecipeFormats =
        {
            FORMAT_STANDARD, FORMAT_DIRECT, FORMAT_BATCH, FORMAT_SUB, FORMAT_COMBO, FORMAT_VARIANT
        };

        /// <summary>Null/empty allowed (legacy). Otherwise one of FOOD|BEVERAGE|MINIBAR|TOBACCO.</summary>
        public static string? NormalizeProductClass(ILoggerManager logger, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return SilaInputRules.OneOf(logger, value, ProductClasses, "product class");
        }

        /// <summary>Null/empty allowed. Otherwise a known recipe format label.</summary>
        public static string? NormalizeRecipeFormat(ILoggerManager logger, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return SilaInputRules.OneOf(logger, value, RecipeFormats, "recipe format");
        }

        /// <summary>Infer product class for legacy rows without ProductClass.</summary>
        public static string InferProductClass(Recipe recipe)
        {
            if (!string.IsNullOrWhiteSpace(recipe.ProductClass))
            {
                return recipe.ProductClass.Trim().ToUpperInvariant();
            }

            if (recipe.ItemMode == Common.SILA_RECIPE_DIRECT)
            {
                return Common.SILA_PRODUCT_MINIBAR;
            }

            return Common.SILA_PRODUCT_FOOD;
        }

        /// <summary>Infer display format from RecipeFormat or ItemMode.</summary>
        public static string InferRecipeFormat(Recipe recipe)
        {
            if (!string.IsNullOrWhiteSpace(recipe.RecipeFormat))
            {
                return recipe.RecipeFormat.Trim().ToUpperInvariant();
            }

            return recipe.ItemMode switch
            {
                Common.SILA_RECIPE_DIRECT => FORMAT_DIRECT,
                Common.SILA_RECIPE_BATCH => FORMAT_BATCH,
                _ => FORMAT_STANDARD
            };
        }

        /// <summary>
        /// Default ItemMode for a product class when the client omits a compatible mode.
        /// Minibar/Tobacco default DIRECT; Food/Beverage default RECIPE.
        /// </summary>
        public static string DefaultItemMode(string? productClass)
        {
            string? cls = productClass?.Trim().ToUpperInvariant();
            if (cls == Common.SILA_PRODUCT_MINIBAR || cls == Common.SILA_PRODUCT_TOBACCO)
            {
                return Common.SILA_RECIPE_DIRECT;
            }

            return Common.SILA_RECIPE_RECIPE;
        }

        /// <summary>
        /// Gross→inventory multiplier from trim/cooking/waste percents.
        /// Applied once at the leaf material quantity (not nested again on sub-recipes).
        /// Formula: qty * (1 + trim/100) * (1 + cooking/100) * (1 + waste/100).
        /// Null/zero factors = 1 (legacy behaviour).
        /// </summary>
        public static decimal LossMultiplier(Recipe recipe)
        {
            decimal trim = ClampPercent(recipe.TrimLossPercent);
            decimal cook = ClampPercent(recipe.CookingLossPercent);
            decimal waste = ClampPercent(recipe.WasteFactorPercent);
            return (1m + trim / 100m) * (1m + cook / 100m) * (1m + waste / 100m);
        }

        private static decimal ClampPercent(decimal? value)
        {
            if (value == null || value <= 0m)
            {
                return 0m;
            }

            return value > 100m ? 100m : value.Value;
        }

        /// <summary>
        /// Ensures starter hospitality category codes exist for the buyer (idempotent).
        /// Tenant may rename/deactivate; codes are stable seeds only.
        /// </summary>
        public static async Task EnsureStarterCategoriesAsync(
            IRepositoryWrapper repository, Guid buyerId, CancellationToken cancellationToken)
        {
            (string Code, string Name, string ProductClass)[] seeds =
            {
                ("FOOD_MAIN", "Main Course", Common.SILA_PRODUCT_FOOD),
                ("FOOD_APP", "Appetizers", Common.SILA_PRODUCT_FOOD),
                ("FOOD_DESS", "Desserts", Common.SILA_PRODUCT_FOOD),
                ("FOOD_SAUCE", "Sauces", Common.SILA_PRODUCT_FOOD),
                ("BEV_COCKTAIL", "Cocktail", Common.SILA_PRODUCT_BEVERAGE),
                ("BEV_COFFEE", "Coffee", Common.SILA_PRODUCT_BEVERAGE),
                ("BEV_JUICE", "Juice", Common.SILA_PRODUCT_BEVERAGE),
                ("MINI_SNACK", "Minibar Snacks", Common.SILA_PRODUCT_MINIBAR),
                ("MINI_BOTTLE", "Bottled Drinks", Common.SILA_PRODUCT_MINIBAR),
                ("TOB_CIG", "Cigarettes", Common.SILA_PRODUCT_TOBACCO),
                ("TOB_SHISHA", "Shisha", Common.SILA_PRODUCT_TOBACCO),
                ("COMBO_MEAL", "Combo Meal", Common.SILA_PRODUCT_FOOD)
            };

            List<string> codes = seeds.Select(x => x.Code).ToList();
            HashSet<string> existing = (await repository.RecipeCategory
                .FindByCondition(x => x.BuyerId == buyerId && codes.Contains(x.Code))
                .Select(x => x.Code)
                .ToListAsync(cancellationToken)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach ((string code, string name, string productClass) in seeds)
            {
                if (existing.Contains(code))
                {
                    continue;
                }

                repository.RecipeCategory.Create(new RecipeCategory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    Code = code,
                    Name = name,
                    Description = $"Hospitality starter · {productClass}",
                    ProductClass = productClass,
                    IsActive = true
                });
            }

            await repository.SaveAsync();
        }

        /// <summary>Applies hospitality fields from a write DTO onto the recipe entity.</summary>
        public static void ApplyHospitalityFields(ILoggerManager logger, Recipe recipe, SilaRecipeWriteDto request)
        {
            // Explicit product class wins; leave null so category inheritance can fill it later.
            recipe.ProductClass = string.IsNullOrWhiteSpace(request.ProductClass)
                ? recipe.ProductClass
                : NormalizeProductClass(logger, request.ProductClass);

            recipe.RecipeFormat = string.IsNullOrWhiteSpace(request.RecipeFormat)
                ? (string.IsNullOrWhiteSpace(recipe.RecipeFormat)
                    ? InferRecipeFormat(new Recipe { ItemMode = recipe.ItemMode, RecipeFormat = null })
                    : recipe.RecipeFormat)
                : NormalizeRecipeFormat(logger, request.RecipeFormat);

            recipe.Subcategory = string.IsNullOrWhiteSpace(request.Subcategory) ? null : request.Subcategory.Trim();
            if (recipe.Subcategory != null && recipe.Subcategory.Length > 100)
            {
                logger.LogError($"Subcategory too long. RecipeId: {recipe.Id}");
                throw new BadRequestCustomException("Subcategory is too long.", "Use at most 100 characters.");
            }

            recipe.TrimLossPercent = NormalizeOptionalPercent(logger, request.TrimLossPercent, "trim loss");
            recipe.CookingLossPercent = NormalizeOptionalPercent(logger, request.CookingLossPercent, "cooking loss");
            recipe.WasteFactorPercent = NormalizeOptionalPercent(logger, request.WasteFactorPercent, "waste factor");
            recipe.PrepTimeMinutes = NormalizeOptionalMinutes(logger, request.PrepTimeMinutes, "prep time");
            recipe.CookTimeMinutes = NormalizeOptionalMinutes(logger, request.CookTimeMinutes, "cook time");
            recipe.PreparationSteps = string.IsNullOrWhiteSpace(request.PreparationSteps)
                ? null
                : request.PreparationSteps.Trim();
        }

        private static decimal? NormalizeOptionalPercent(ILoggerManager logger, decimal? value, string label)
        {
            if (value == null)
            {
                return null;
            }

            if (value < 0m || value > 100m)
            {
                logger.LogError($"{label} percent out of range: {value}");
                throw new BadRequestCustomException($"{label} is invalid.", "Enter a percentage between 0 and 100.");
            }

            return value;
        }

        private static int? NormalizeOptionalMinutes(ILoggerManager logger, int? value, string label)
        {
            if (value == null)
            {
                return null;
            }

            if (value < 0 || value > 24 * 60)
            {
                logger.LogError($"{label} minutes out of range: {value}");
                throw new BadRequestCustomException($"{label} is invalid.", "Enter minutes between 0 and 1440.");
            }

            return value;
        }
    }
}
