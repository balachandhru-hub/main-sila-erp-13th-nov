using System.Globalization;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public static partial class SilaRecipeLegacyMigration
    {
        public static async Task<SilaRecipeLegacyBatchDetailDto> UploadAndStageAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration,
            Guid organizationId,
            Guid userId,
            string? fileName,
            byte[] content,
            CancellationToken cancellationToken)
        {
            SilaFileRules.Validate(logger, "Legacy recipe workbook", fileName, content, new[] { SilaFileRules.KIND_XLSX }, MAX_UPLOAD_BYTES);
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            string basePath = RequireBasePath(configuration, logger);

            if (IsWorkbookFormat(SilaRecipeExcel.SheetNames(content)))
            {
                (List<RecipeLegacyRecipe> workbookStaged, List<RecipeLegacyMaterialMap> workbookMaps) =
                    StageWorkbookFormat(logger, fileName, content, buyer.Id);
                return await SaveBatchAsync(repository, logger, buyer, basePath, userId, fileName, content, workbookStaged, workbookMaps, 0, cancellationToken);
            }

            List<Dictionary<string, string>> headers = SilaRecipeExcel.ReadSheet(logger, fileName, content, SHEET_HEADER, RequiredHeaderColumns);
            if (headers.Count == 0)
            {
                List<string> sheetNames = SilaRecipeExcel.SheetNames(content);
                if (!sheetNames.Any(name => string.Equals(name, SHEET_HEADER, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.LogError($"Legacy workbook has no {SHEET_HEADER} sheet. FileName: {fileName}, Sheets: {string.Join(",", sheetNames)}");
                    throw new BadRequestCustomException(
                        "No recipe sheet found.",
                        $"This file has the sheets: {string.Join(", ", sheetNames)}. Use the recipe template (Download template) with its \"{SilaRecipeImport.SHEET_RECIPES}\" sheet, or the legacy workbook with a \"{SHEET_HEADER}\" sheet.");
                }

                throw new BadRequestCustomException("No recipes found.", $"Add at least one row to the {SHEET_HEADER} sheet.");
            }

            List<Dictionary<string, string>> ingredients = SilaRecipeExcel.ReadSheet(logger, fileName, content, SHEET_INGREDIENTS, RequiredIngredientColumns);
            List<Dictionary<string, string>> instructions = SilaRecipeExcel.ReadSheet(logger, fileName, content, SHEET_INSTRUCTIONS, new[] { "Legacy Recipe ID", "Instruction Text" });
            List<Dictionary<string, string>> subRecipes = SilaRecipeExcel.ReadSheet(logger, fileName, content, SHEET_SUB_RECIPES, new[] { "Legacy Recipe ID", "Sub Legacy Recipe ID" });
            List<Dictionary<string, string>> variants = SilaRecipeExcel.ReadSheet(logger, fileName, content, SHEET_VARIANTS, new[] { "Legacy Recipe ID" });

            Guid batchId = Guid.NewGuid();

            Dictionary<string, List<SilaRecipeLegacyIngredientDto>> ingredientsByLegacy = GroupIngredients(ingredients);
            Dictionary<string, List<string>> instructionsByLegacy = GroupInstructions(instructions);
            Dictionary<string, List<SilaRecipeLegacySubRecipeDto>> subsByLegacy = GroupSubRecipes(subRecipes);
            Dictionary<string, List<SilaRecipeLegacyVariantDto>> variantsByLegacy = GroupVariants(variants);

            HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<RecipeLegacyRecipe> staged = new List<RecipeLegacyRecipe>();
            List<RecipeLegacyMaterialMap> maps = new List<RecipeLegacyMaterialMap>();
            int duplicateCount = 0;

            foreach (Dictionary<string, string> row in headers)
            {
                string? legacyId = SilaRecipeExcel.Value(row, "Legacy Recipe ID");
                string? name = SilaRecipeExcel.Value(row, "Recipe Name");
                int sourceRow = SilaRecipeExcel.RowNumber(row);
                if (string.IsNullOrWhiteSpace(legacyId) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                string normalizedId = legacyId.Trim();
                bool duplicate = !seenIds.Add(normalizedId);
                if (duplicate) duplicateCount++;

                List<SilaRecipeLegacyIngredientDto> recipeIngredients = ingredientsByLegacy.TryGetValue(normalizedId, out List<SilaRecipeLegacyIngredientDto>? foundIng)
                    ? foundIng
                    : new List<SilaRecipeLegacyIngredientDto>();
                List<string> recipeInstructions = instructionsByLegacy.TryGetValue(normalizedId, out List<string>? foundIns)
                    ? foundIns
                    : new List<string>();
                List<SilaRecipeLegacySubRecipeDto> recipeSubs = subsByLegacy.TryGetValue(normalizedId, out List<SilaRecipeLegacySubRecipeDto>? foundSub)
                    ? foundSub
                    : new List<SilaRecipeLegacySubRecipeDto>();
                List<SilaRecipeLegacyVariantDto> recipeVariants = variantsByLegacy.TryGetValue(normalizedId, out List<SilaRecipeLegacyVariantDto>? foundVar)
                    ? foundVar
                    : new List<SilaRecipeLegacyVariantDto>();

                RecipeLegacyRecipe recipe = new RecipeLegacyRecipe
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    BuyerId = buyer.Id,
                    LegacyRecipeId = normalizedId,
                    RecipeName = name.Trim(),
                    Description = SilaRecipeExcel.Value(row, "Description"),
                    Category = SilaRecipeExcel.Value(row, "Category"),
                    Subcategory = SilaRecipeExcel.Value(row, "Subcategory"),
                    RecipeFormat = SilaRecipeExcel.Value(row, "Recipe Format")?.Trim().ToUpperInvariant(),
                    ProductClass = SilaRecipeExcel.Value(row, "Product Class")?.Trim().ToUpperInvariant(),
                    YieldQty = ParseDecimal(SilaRecipeExcel.Value(row, "Yield Qty")),
                    YieldUom = SilaRecipeExcel.Value(row, "Yield UOM")?.Trim().ToUpperInvariant(),
                    PortionQty = ParseDecimal(SilaRecipeExcel.Value(row, "Portion Qty")),
                    PortionUom = SilaRecipeExcel.Value(row, "Portion UOM")?.Trim().ToUpperInvariant(),
                    PrepMinutes = ParseInt(SilaRecipeExcel.Value(row, "Prep Minutes")),
                    CookMinutes = ParseInt(SilaRecipeExcel.Value(row, "Cook Minutes")),
                    ServingInstructions = SilaRecipeExcel.Value(row, "Serving Instructions"),
                    OutletScope = SilaRecipeExcel.Value(row, "Outlet Scope"),
                    ActiveStatus = SilaRecipeExcel.Value(row, "Active Status"),
                    LegacyReference = SilaRecipeExcel.Value(row, "Legacy Reference") ?? normalizedId,
                    Notes = SilaRecipeExcel.Value(row, "Notes"),
                    IngredientsJson = Serialize(recipeIngredients),
                    InstructionsJson = Serialize(recipeInstructions),
                    SubRecipesJson = Serialize(recipeSubs),
                    VariantsJson = Serialize(recipeVariants),
                    ValidationStatus = duplicate ? Common.SILA_RECIPE_LEGACY_DUPLICATE : Common.SILA_RECIPE_LEGACY_UNMAPPED,
                    ErrorCount = 0,
                    WarningCount = 0,
                    PostingStatus = Common.SILA_POSTING_PENDING,
                    SourceRow = sourceRow,
                    IsSelected = !duplicate,
                    IsActive = true
                };
                staged.Add(recipe);

                foreach (SilaRecipeLegacyIngredientDto ingredient in recipeIngredients)
                {
                    maps.Add(new RecipeLegacyMaterialMap
                    {
                        Id = Guid.NewGuid(),
                        BatchId = batchId,
                        BuyerId = buyer.Id,
                        LegacyRecipeId = normalizedId,
                        IngredientLine = ingredient.Line,
                        LegacyMaterialNumber = ingredient.LegacyMaterialNumber,
                        MaterialNumber = ingredient.MaterialNumber,
                        MaterialDescription = ingredient.MaterialDescription,
                        MappedMaterialId = null,
                        Status = Common.SILA_RECIPE_LEGACY_UNMAPPED,
                        IsActive = true
                    });
                }
            }

            if (staged.Count == 0)
            {
                throw new BadRequestCustomException("No valid recipes found.", "Each header row needs a Legacy Recipe ID and Recipe Name.");
            }

            return await SaveBatchAsync(repository, logger, buyer, basePath, userId, fileName, content, staged, maps, duplicateCount, cancellationToken);
        }

        private static async Task<SilaRecipeLegacyBatchDetailDto> SaveBatchAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            BuyerBusinessProfile buyer,
            string basePath,
            Guid userId,
            string? fileName,
            byte[] content,
            List<RecipeLegacyRecipe> staged,
            List<RecipeLegacyMaterialMap> maps,
            int duplicateCount,
            CancellationToken cancellationToken)
        {
            Guid batchId = staged[0].BatchId;
            string batchNumber = await DocumentNumber.NextAsync(repository, buyer.Id, DocumentNumber.RECIPE_LEGACY_BATCH, 8, cancellationToken);
            string kind = SilaFileRules.KIND_XLSX;
            string storedName = SilaFileRules.StoredName(batchId, kind);
            string directory = BatchDirectory(basePath, buyer.Id, batchId);
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, storedName), content, cancellationToken);

            RecipeLegacyBatch batch = new RecipeLegacyBatch
            {
                Id = batchId,
                BuyerId = buyer.Id,
                BatchNumber = batchNumber,
                FileName = SilaFileRules.SafeDisplayName(fileName, "legacy-recipes", kind),
                StoredFileName = storedName,
                Status = Common.SILA_RECIPE_LEGACY_UPLOADED,
                UploadedBy = userId,
                RecipeCount = staged.Count,
                ValidCount = 0,
                WarningCount = 0,
                ErrorCount = 0,
                UnmappedCount = maps.Count,
                DuplicateCount = duplicateCount,
                Revision = 1,
                PostedCount = 0,
                FailedPostCount = 0,
                ImageAssignedCount = 0,
                PublishedCount = 0,
                IsActive = true
            };

            repository.RecipeLegacyBatch.Create(batch);
            repository.RecipeLegacyRecipe.CreateRange(staged);
            if (maps.Count > 0)
            {
                repository.RecipeLegacyMaterialMap.CreateRange(maps);
            }

            new InventoryLedger(repository, buyer.Id, userId)
                .AddEvent(Common.SILA_REF_RECIPE_LEGACY, batchId, Common.SILA_RECIPE_LEGACY_EVENT_UPLOAD,
                    $"Uploaded {staged.Count} recipe(s) as {batchNumber} from {batch.FileName}.");
            await repository.SaveAsync();

            logger.LogInfo($"Legacy recipe batch staged. BatchId: {batchId}, BatchNumber: {batchNumber}, Recipes: {staged.Count}");
            return ToDetail(batch);
        }

        private static Dictionary<string, List<SilaRecipeLegacyIngredientDto>> GroupIngredients(List<Dictionary<string, string>> rows)
        {
            Dictionary<string, List<SilaRecipeLegacyIngredientDto>> map = new Dictionary<string, List<SilaRecipeLegacyIngredientDto>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> autoLine = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, string> row in rows)
            {
                string? legacyId = SilaRecipeExcel.Value(row, "Legacy Recipe ID");
                if (string.IsNullOrWhiteSpace(legacyId)) continue;
                string key = legacyId.Trim();
                if (!autoLine.ContainsKey(key)) autoLine[key] = 0;
                int line = ParseInt(SilaRecipeExcel.Value(row, "Ingredient Line")) ?? (++autoLine[key]);
                if (!map.TryGetValue(key, out List<SilaRecipeLegacyIngredientDto>? list))
                {
                    list = new List<SilaRecipeLegacyIngredientDto>();
                    map[key] = list;
                }

                list.Add(new SilaRecipeLegacyIngredientDto
                {
                    Line = line,
                    LegacyMaterialNumber = SilaRecipeExcel.Value(row, "Legacy Material Number"),
                    MaterialNumber = SilaRecipeExcel.Value(row, "Material Number"),
                    MaterialDescription = SilaRecipeExcel.Value(row, "Material Description"),
                    Quantity = ParseDecimal(SilaRecipeExcel.Value(row, "Quantity")) ?? 0m,
                    Uom = SilaRecipeExcel.Value(row, "UOM")?.Trim().ToUpperInvariant(),
                    Notes = SilaRecipeExcel.Value(row, "Notes")
                });
            }

            return map;
        }

        private static Dictionary<string, List<string>> GroupInstructions(List<Dictionary<string, string>> rows)
        {
            Dictionary<string, List<(int Step, string Text)>> raw = new Dictionary<string, List<(int, string)>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, string> row in rows)
            {
                string? legacyId = SilaRecipeExcel.Value(row, "Legacy Recipe ID");
                string? text = SilaRecipeExcel.Value(row, "Instruction Text");
                if (string.IsNullOrWhiteSpace(legacyId) || string.IsNullOrWhiteSpace(text)) continue;
                string key = legacyId.Trim();
                if (!raw.TryGetValue(key, out List<(int, string)>? list))
                {
                    list = new List<(int, string)>();
                    raw[key] = list;
                }

                list.Add((ParseInt(SilaRecipeExcel.Value(row, "Step Number")) ?? list.Count + 1, text.Trim()));
            }

            return raw.ToDictionary(
                x => x.Key,
                x => x.Value.OrderBy(s => s.Step).Select(s => s.Text).ToList(),
                StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, List<SilaRecipeLegacySubRecipeDto>> GroupSubRecipes(List<Dictionary<string, string>> rows)
        {
            Dictionary<string, List<SilaRecipeLegacySubRecipeDto>> map = new Dictionary<string, List<SilaRecipeLegacySubRecipeDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, string> row in rows)
            {
                string? legacyId = SilaRecipeExcel.Value(row, "Legacy Recipe ID");
                string? subId = SilaRecipeExcel.Value(row, "Sub Legacy Recipe ID");
                if (string.IsNullOrWhiteSpace(legacyId) || string.IsNullOrWhiteSpace(subId)) continue;
                string key = legacyId.Trim();
                if (!map.TryGetValue(key, out List<SilaRecipeLegacySubRecipeDto>? list))
                {
                    list = new List<SilaRecipeLegacySubRecipeDto>();
                    map[key] = list;
                }

                list.Add(new SilaRecipeLegacySubRecipeDto
                {
                    SubLegacyRecipeId = subId.Trim(),
                    Quantity = ParseDecimal(SilaRecipeExcel.Value(row, "Quantity")) ?? 1m,
                    Uom = SilaRecipeExcel.Value(row, "UOM")?.Trim().ToUpperInvariant()
                });
            }

            return map;
        }

        private static Dictionary<string, List<SilaRecipeLegacyVariantDto>> GroupVariants(List<Dictionary<string, string>> rows)
        {
            Dictionary<string, List<SilaRecipeLegacyVariantDto>> map = new Dictionary<string, List<SilaRecipeLegacyVariantDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, string> row in rows)
            {
                string? legacyId = SilaRecipeExcel.Value(row, "Legacy Recipe ID");
                if (string.IsNullOrWhiteSpace(legacyId)) continue;
                string key = legacyId.Trim();
                if (!map.TryGetValue(key, out List<SilaRecipeLegacyVariantDto>? list))
                {
                    list = new List<SilaRecipeLegacyVariantDto>();
                    map[key] = list;
                }

                list.Add(new SilaRecipeLegacyVariantDto
                {
                    VariantLegacyRecipeId = SilaRecipeExcel.Value(row, "Variant Legacy Recipe ID"),
                    VariantName = SilaRecipeExcel.Value(row, "Variant Name"),
                    Notes = SilaRecipeExcel.Value(row, "Notes")
                });
            }

            return map;
        }

        public static decimal? ParseDecimal(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : null;
        }

        public static int? ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;
        }
    }
}
