using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public static partial class SilaRecipeLegacyMigration
    {
        private static readonly HashSet<string> KnownFormats = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            SilaHospitalityRules.FORMAT_STANDARD,
            SilaHospitalityRules.FORMAT_DIRECT,
            SilaHospitalityRules.FORMAT_BATCH,
            SilaHospitalityRules.FORMAT_SUB,
            SilaHospitalityRules.FORMAT_COMBO,
            SilaHospitalityRules.FORMAT_VARIANT
        };

        private static readonly HashSet<string> KnownProductClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Common.SILA_PRODUCT_FOOD,
            Common.SILA_PRODUCT_BEVERAGE,
            Common.SILA_PRODUCT_MINIBAR,
            Common.SILA_PRODUCT_TOBACCO
        };

        /// <summary>Pure validation helpers (unit-testable without DB).</summary>
        public static class Validation
        {
            public static bool IsKnownFormat(string? format) =>
                !string.IsNullOrWhiteSpace(format) && KnownFormats.Contains(format.Trim());

            public static bool IsKnownProductClass(string? productClass) =>
                !string.IsNullOrWhiteSpace(productClass) && KnownProductClasses.Contains(productClass.Trim());

            public static bool HasCircularSubRecipe(
                string legacyRecipeId,
                IReadOnlyDictionary<string, IReadOnlyList<string>> adjacency)
            {
                HashSet<string> visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                return Dfs(legacyRecipeId, adjacency, visiting, visited);
            }

            public static List<string> DetectCircularRecipes(IReadOnlyDictionary<string, IReadOnlyList<string>> adjacency)
            {
                List<string> cycles = new List<string>();
                foreach (string node in adjacency.Keys)
                {
                    if (HasCircularSubRecipe(node, adjacency))
                    {
                        cycles.Add(node);
                    }
                }

                return cycles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }

            public static string AggregateStatus(int errorCount, int warningCount, bool duplicate, bool unmappedMaterial)
            {
                if (duplicate) return Common.SILA_RECIPE_LEGACY_DUPLICATE;
                if (errorCount > 0) return Common.SILA_RECIPE_LEGACY_INVALID;
                if (unmappedMaterial) return Common.SILA_RECIPE_LEGACY_UNMAPPED;
                if (warningCount > 0) return Common.SILA_RECIPE_LEGACY_WARNING;
                return Common.SILA_RECIPE_LEGACY_VALID;
            }

            private static bool Dfs(
                string node,
                IReadOnlyDictionary<string, IReadOnlyList<string>> adjacency,
                HashSet<string> visiting,
                HashSet<string> visited)
            {
                if (visiting.Contains(node)) return true;
                if (visited.Contains(node)) return false;
                visiting.Add(node);
                if (adjacency.TryGetValue(node, out IReadOnlyList<string>? children))
                {
                    foreach (string child in children)
                    {
                        if (Dfs(child, adjacency, visiting, visited)) return true;
                    }
                }

                visiting.Remove(node);
                visited.Add(node);
                return false;
            }
        }

        public static async Task<SilaRecipeLegacyBatchDetailDto> ValidateBatchAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid organizationId,
            Guid userId,
            Guid batchId,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status is Common.SILA_RECIPE_LEGACY_POSTING or Common.SILA_RECIPE_LEGACY_POSTED
                or Common.SILA_RECIPE_LEGACY_PARTIAL_POST or Common.SILA_RECIPE_LEGACY_PUBLISHED
                or Common.SILA_RECIPE_LEGACY_PARTIAL_PUBLISH or Common.SILA_RECIPE_LEGACY_IMAGES_ASSIGNED)
            {
                throw new BadRequestCustomException("Batch cannot be revalidated.", "This batch has already been posted or published.");
            }

            List<RecipeLegacyRecipe> recipes = await repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<RecipeLegacyMaterialMap> maps = await repository.RecipeLegacyMaterialMap
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            HashSet<string> materialCodes = maps
                .Select(x => x.MaterialNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ItemBuyerMaster> materialsByCode = materialCodes.Count == 0
                ? new Dictionary<string, ItemBuyerMaster>(StringComparer.OrdinalIgnoreCase)
                : await repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.MaterialCode != null && materialCodes.Contains(x.MaterialCode))
                    .ToDictionaryAsync(x => x.MaterialCode!, StringComparer.OrdinalIgnoreCase, cancellationToken);

            Dictionary<string, IReadOnlyList<string>> adjacency = BuildSubRecipeAdjacency(recipes);
            HashSet<string> circular = Validation.DetectCircularRecipes(adjacency).ToHashSet(StringComparer.OrdinalIgnoreCase);
            HashSet<string> nameDupes = recipes
                .GroupBy(x => x.RecipeName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<SilaRecipeLegacyExtrasDto?> allExtras = recipes.Select(ExtrasOf).ToList();
            HashSet<string> batchIds = recipes.Select(x => x.LegacyRecipeId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<string> outsideSubCodes = recipes
                .SelectMany(x => Deserialize<List<SilaRecipeLegacySubRecipeDto>>(x.SubRecipesJson) ?? new())
                .Select(x => x.SubLegacyRecipeId)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !batchIds.Contains(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            Dictionary<string, Recipe> existingRecipes = outsideSubCodes.Count == 0
                ? new Dictionary<string, Recipe>(StringComparer.OrdinalIgnoreCase)
                : await repository.Recipe
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && outsideSubCodes.Contains(x.RecipeCode))
                    .ToDictionaryAsync(x => x.RecipeCode, StringComparer.OrdinalIgnoreCase, cancellationToken);
            HashSet<string> familyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> categoryCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> outletCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (allExtras.Any(x => x != null))
            {
                familyCodes = (await repository.RecipeFamily.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive).Select(x => x.Code).ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                categoryCodes = (await repository.RecipeCategory.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive).Select(x => x.Code).ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                outletCodes = (await repository.InventoryLocation
                        .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.LocationType == Common.SILA_LOCATION_OUTLET)
                        .Select(x => x.LocationCode).ToListAsync(cancellationToken))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }

            int valid = 0, warning = 0, error = 0, unmapped = 0, duplicate = 0;
            List<object> reportRows = new List<object>();

            foreach (RecipeLegacyRecipe recipe in recipes)
            {
                List<SilaRecipeLegacyFindingDto> findings = new List<SilaRecipeLegacyFindingDto>();
                SilaRecipeLegacyExtrasDto? extras = ExtrasOf(recipe);
                string headerSheet = extras != null ? SilaRecipeImport.SHEET_RECIPES : SHEET_HEADER;
                string ingredientsSheet = extras != null ? SilaRecipeImport.SHEET_RECIPES : SHEET_INGREDIENTS;
                string subsSheet = extras != null ? SilaRecipeImport.SHEET_RECIPES : SHEET_SUB_RECIPES;
                bool isDuplicate = recipe.ValidationStatus == Common.SILA_RECIPE_LEGACY_DUPLICATE
                    || recipes.Count(x => string.Equals(x.LegacyRecipeId, recipe.LegacyRecipeId, StringComparison.OrdinalIgnoreCase)) > 1;

                if (string.IsNullOrWhiteSpace(recipe.RecipeName))
                {
                    findings.Add(Finding(SEVERITY_ERROR, "NAME_REQUIRED", "Recipe name is required.", "RecipeName", recipe.SourceRow, headerSheet));
                }

                if (!string.IsNullOrWhiteSpace(recipe.RecipeFormat) && !Validation.IsKnownFormat(recipe.RecipeFormat))
                {
                    findings.Add(Finding(SEVERITY_ERROR, "FORMAT_INVALID", $"Unknown recipe format '{recipe.RecipeFormat}'.", "RecipeFormat", recipe.SourceRow, headerSheet));
                }

                if (!string.IsNullOrWhiteSpace(recipe.ProductClass) && !Validation.IsKnownProductClass(recipe.ProductClass))
                {
                    findings.Add(Finding(SEVERITY_ERROR, "PRODUCT_CLASS_INVALID", $"Unknown product class '{recipe.ProductClass}'.", "ProductClass", recipe.SourceRow, headerSheet));
                }

                if (ServingQtyOf(recipe) <= 0)
                {
                    findings.Add(Finding(SEVERITY_ERROR, "YIELD_REQUIRED", "Yield or portion quantity must be greater than zero.", "YieldQty", recipe.SourceRow, headerSheet));
                }

                if (circular.Contains(recipe.LegacyRecipeId))
                {
                    findings.Add(Finding(SEVERITY_ERROR, "CIRCULAR_SUB_RECIPE", "Sub-recipe references form a cycle.", "SubRecipes", recipe.SourceRow, subsSheet));
                }

                if (nameDupes.Contains(recipe.RecipeName.Trim()))
                {
                    findings.Add(Finding(SEVERITY_WARNING, "NAME_DUPLICATE", "Another staged recipe shares this name.", "RecipeName", recipe.SourceRow, headerSheet));
                }

                List<SilaRecipeLegacyIngredientDto> ingredients = Deserialize<List<SilaRecipeLegacyIngredientDto>>(recipe.IngredientsJson) ?? new();
                List<SilaRecipeLegacySubRecipeDto> subs = Deserialize<List<SilaRecipeLegacySubRecipeDto>>(recipe.SubRecipesJson) ?? new();
                string itemMode = ItemModeFromFormat(recipe.RecipeFormat, recipe.ProductClass);

                if (itemMode != Common.SILA_RECIPE_DIRECT && ingredients.Count == 0 && subs.Count == 0)
                {
                    findings.Add(Finding(SEVERITY_ERROR, "INGREDIENTS_REQUIRED", "At least one ingredient or sub-recipe is required.", "Ingredients", recipe.SourceRow, ingredientsSheet));
                }

                foreach (SilaRecipeLegacySubRecipeDto sub in subs)
                {
                    if (!batchIds.Contains(sub.SubLegacyRecipeId)
                        && (!existingRecipes.TryGetValue(sub.SubLegacyRecipeId.Trim(), out Recipe? existingSub)
                            || existingSub.ActiveVersion <= 0
                            || existingSub.Status == Common.SILA_RECIPE_INACTIVE))
                    {
                        findings.Add(Finding(SEVERITY_ERROR, "SUB_RECIPE_MISSING", $"Sub-recipe '{sub.SubLegacyRecipeId}' is not in this batch and is not an approved recipe.", "SubRecipes", recipe.SourceRow, subsSheet));
                    }

                    if (sub.Quantity <= 0)
                    {
                        findings.Add(Finding(SEVERITY_ERROR, "SUB_QTY_INVALID", $"Sub-recipe '{sub.SubLegacyRecipeId}' quantity must be greater than zero.", "SubRecipes", recipe.SourceRow, subsSheet));
                    }
                }

                if (subs.Count > 0 && itemMode != Common.SILA_RECIPE_BATCH)
                {
                    findings.Add(Finding(SEVERITY_ERROR, "SUB_NEEDS_BATCH", "Only a BATCH recipe can contain a sub-recipe. Set ItemMode to BATCH.", "ItemMode", recipe.SourceRow, headerSheet));
                }

                if (extras != null)
                {
                    AddWorkbookFindings(findings, recipe, extras, familyCodes, categoryCodes, outletCodes, headerSheet);
                }

                bool hasUnmapped = false;
                List<RecipeLegacyMaterialMap> recipeMaps = maps
                    .Where(x => string.Equals(x.LegacyRecipeId, recipe.LegacyRecipeId, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (SilaRecipeLegacyIngredientDto ingredient in ingredients)
                {
                    if (ingredient.Quantity <= 0)
                    {
                        findings.Add(Finding(SEVERITY_ERROR, "QTY_INVALID", $"Ingredient line {ingredient.Line} quantity must be greater than zero.", "Quantity", recipe.SourceRow, ingredientsSheet));
                    }

                    if (string.IsNullOrWhiteSpace(ingredient.Uom))
                    {
                        findings.Add(Finding(SEVERITY_WARNING, "UOM_MISSING", $"Ingredient line {ingredient.Line} has no UOM.", "UOM", recipe.SourceRow, ingredientsSheet));
                    }

                    RecipeLegacyMaterialMap? map = recipeMaps.FirstOrDefault(x => x.IngredientLine == ingredient.Line);
                    if (map == null) continue;

                    if (map.Status == Common.SILA_RECIPE_LEGACY_IGNORED)
                    {
                        continue;
                    }

                    string? code = map.MaterialNumber ?? ingredient.MaterialNumber;
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        hasUnmapped = true;
                        map.Status = Common.SILA_RECIPE_LEGACY_UNMAPPED;
                        map.MappedMaterialId = null;
                        findings.Add(Finding(SEVERITY_ERROR, "MATERIAL_UNMAPPED", $"Ingredient line {ingredient.Line} has no material number.", "MaterialNumber", recipe.SourceRow, ingredientsSheet));
                        continue;
                    }

                    if (materialsByCode.TryGetValue(code.Trim(), out ItemBuyerMaster? material))
                    {
                        map.MappedMaterialId = material.Id;
                        map.MaterialNumber = material.MaterialCode;
                        map.MaterialDescription ??= material.Description;
                        map.Status = Common.SILA_RECIPE_LEGACY_MAPPED;
                    }
                    else if (map.MappedMaterialId != null)
                    {
                        map.Status = Common.SILA_RECIPE_LEGACY_MAPPED;
                    }
                    else
                    {
                        hasUnmapped = true;
                        map.Status = Common.SILA_RECIPE_LEGACY_UNMAPPED;
                        map.MappedMaterialId = null;
                        findings.Add(Finding(SEVERITY_ERROR, "MATERIAL_NOT_FOUND", $"Material '{code}' was not found in Item Master.", "MaterialNumber", recipe.SourceRow, ingredientsSheet));
                    }
                }

                int errors = findings.Count(x => x.Severity == SEVERITY_ERROR);
                int warnings = findings.Count(x => x.Severity == SEVERITY_WARNING);
                string status = Validation.AggregateStatus(errors, warnings, isDuplicate, hasUnmapped && errors == 0);
                // Unmapped materials already counted as errors above; keep UNMAPPED when only mapping gaps remain without other errors.
                if (!isDuplicate && hasUnmapped && errors > 0 && findings.All(x => x.Severity != SEVERITY_ERROR || x.Code.StartsWith("MATERIAL_", StringComparison.Ordinal)))
                {
                    status = Common.SILA_RECIPE_LEGACY_UNMAPPED;
                }

                recipe.ValidationStatus = status;
                recipe.ErrorCount = errors;
                recipe.WarningCount = warnings;
                recipe.FindingsJson = Serialize(findings);
                recipe.MaterialMappingsJson = Serialize(recipeMaps.Select(x => new SilaRecipeLegacyMaterialMapDto
                {
                    Id = x.Id,
                    LegacyRecipeId = x.LegacyRecipeId,
                    IngredientLine = x.IngredientLine,
                    LegacyMaterialNumber = x.LegacyMaterialNumber,
                    MaterialNumber = x.MaterialNumber,
                    MaterialDescription = x.MaterialDescription,
                    MappedMaterialId = x.MappedMaterialId,
                    Status = x.Status
                }));

                switch (status)
                {
                    case Common.SILA_RECIPE_LEGACY_VALID: valid++; break;
                    case Common.SILA_RECIPE_LEGACY_WARNING: warning++; break;
                    case Common.SILA_RECIPE_LEGACY_DUPLICATE: duplicate++; break;
                    case Common.SILA_RECIPE_LEGACY_UNMAPPED: unmapped++; error++; break;
                    default: error++; break;
                }

                reportRows.Add(new
                {
                    recipe.LegacyRecipeId,
                    recipe.RecipeName,
                    Status = status,
                    Errors = errors,
                    Warnings = warnings,
                    Findings = findings
                });
            }

            if (maps.Count > 0)
            {
                repository.RecipeLegacyMaterialMap.UpdateRange(maps);
            }

            repository.RecipeLegacyRecipe.UpdateRange(recipes);
            batch.ValidCount = valid;
            batch.WarningCount = warning;
            batch.ErrorCount = error;
            batch.UnmappedCount = unmapped;
            batch.DuplicateCount = duplicate;
            batch.Revision++;
            batch.Status = Common.SILA_RECIPE_LEGACY_VALIDATED;
            batch.ValidationReportJson = Serialize(new { GeneratedOn = DateTime.UtcNow, Recipes = reportRows });
            repository.RecipeLegacyBatch.Update(batch);

            new InventoryLedger(repository, buyer.Id, userId)
                .AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_VALIDATION,
                    $"Validated: valid={valid}, warning={warning}, error={error}, unmapped={unmapped}, duplicate={duplicate}.");
            await repository.SaveAsync();

            logger.LogInfo($"Legacy recipe batch validated. BatchId: {batch.Id}, Valid: {valid}, Error: {error}");
            return ToDetail(batch);
        }

        public static SilaRecipeFileDto BuildValidationReport(RecipeLegacyBatch batch, List<RecipeLegacyRecipe> recipes)
        {
            SilaRecipeExcel.Sheet sheet = new SilaRecipeExcel.Sheet
            {
                Name = "Validation",
                Headers = new[] { "Legacy Recipe ID", "Recipe Name", "Status", "Errors", "Warnings", "Findings" },
                Rows = recipes.Select(r =>
                {
                    List<SilaRecipeLegacyFindingDto> findings = Deserialize<List<SilaRecipeLegacyFindingDto>>(r.FindingsJson) ?? new();
                    string text = string.Join(" | ", findings.Select(f => $"{f.Severity}:{f.Code}:{f.Message}"));
                    return new object?[] { r.LegacyRecipeId, r.RecipeName, r.ValidationStatus, r.ErrorCount, r.WarningCount, text };
                }).ToList()
            };
            return SilaRecipeExcel.Build($"recipe-legacy-validation-{batch.BatchNumber}.xlsx", sheet);
        }

        private static void AddWorkbookFindings(
            List<SilaRecipeLegacyFindingDto> findings,
            RecipeLegacyRecipe recipe,
            SilaRecipeLegacyExtrasDto extras,
            HashSet<string> familyCodes,
            HashSet<string> categoryCodes,
            HashSet<string> outletCodes,
            string headerSheet)
        {
            if (string.IsNullOrWhiteSpace(recipe.RecipeFormat))
            {
                findings.Add(Finding(SEVERITY_ERROR, "ITEM_MODE_REQUIRED", "ItemMode is required (DIRECT, RECIPE or BATCH).", "ItemMode", recipe.SourceRow, headerSheet));
            }

            if (recipe.PortionQty is not > 0 || string.IsNullOrWhiteSpace(recipe.PortionUom))
            {
                findings.Add(Finding(SEVERITY_ERROR, "SERVING_REQUIRED", "ServingQty must be greater than zero and ServingUom is required.", "ServingQty", recipe.SourceRow, headerSheet));
            }

            if (!string.IsNullOrWhiteSpace(extras.FamilyCode) && !familyCodes.Contains(extras.FamilyCode))
            {
                findings.Add(Finding(SEVERITY_ERROR, "FAMILY_NOT_FOUND", $"Family '{extras.FamilyCode}' does not exist. Use a family code from Recipe master data.", "Family", recipe.SourceRow, headerSheet));
            }

            if (!string.IsNullOrWhiteSpace(extras.CategoryCode) && !categoryCodes.Contains(extras.CategoryCode))
            {
                findings.Add(Finding(SEVERITY_ERROR, "CATEGORY_NOT_FOUND", $"Category '{extras.CategoryCode}' does not exist. Use a category code from Recipe master data.", "Category", recipe.SourceRow, headerSheet));
            }

            HashSet<string> priced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (SilaRecipeLegacyOutletPriceDto price in extras.OutletPrices)
            {
                if (string.IsNullOrWhiteSpace(price.OutletCode) || !outletCodes.Contains(price.OutletCode))
                {
                    findings.Add(Finding(SEVERITY_ERROR, "OUTLET_NOT_FOUND", $"Outlet '{price.OutletCode}' is not an active outlet location.", "OutletCode", price.Row, SilaRecipeImport.SHEET_PRICES));
                }
                else if (!priced.Add(price.OutletCode))
                {
                    findings.Add(Finding(SEVERITY_ERROR, "OUTLET_DUPLICATE", $"Outlet '{price.OutletCode}' is priced twice for this recipe.", "OutletCode", price.Row, SilaRecipeImport.SHEET_PRICES));
                }

                if (price.MenuPrice is not > 0)
                {
                    findings.Add(Finding(SEVERITY_ERROR, "MENU_PRICE_INVALID", "MenuPrice must be a number greater than zero.", "MenuPrice", price.Row, SilaRecipeImport.SHEET_PRICES));
                }
            }
        }

        private static Dictionary<string, IReadOnlyList<string>> BuildSubRecipeAdjacency(List<RecipeLegacyRecipe> recipes)
        {
            Dictionary<string, IReadOnlyList<string>> adjacency = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (RecipeLegacyRecipe recipe in recipes)
            {
                List<SilaRecipeLegacySubRecipeDto> subs = Deserialize<List<SilaRecipeLegacySubRecipeDto>>(recipe.SubRecipesJson) ?? new();
                adjacency[recipe.LegacyRecipeId] = subs.Select(x => x.SubLegacyRecipeId).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            }

            return adjacency;
        }

        private static SilaRecipeLegacyFindingDto Finding(string severity, string code, string message, string? field, int? row, string? sheet)
        {
            return new SilaRecipeLegacyFindingDto
            {
                Severity = severity,
                Code = code,
                Message = message,
                Field = field,
                Row = row,
                Sheet = sheet
            };
        }
    }
}
