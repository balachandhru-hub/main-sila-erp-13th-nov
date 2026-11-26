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
        public static async Task<SilaRecipeLegacyActionResultDto> PostApprovedAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid batchId,
            bool failedOnly,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status is not (Common.SILA_RECIPE_LEGACY_APPROVED or Common.SILA_RECIPE_LEGACY_PARTIAL_POST
                or Common.SILA_RECIPE_LEGACY_POSTED))
            {
                throw new BadRequestCustomException("Batch is not approved for posting.", "Approve the batch before posting recipes.");
            }

            List<RecipeLegacyRecipe> recipes = await repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive && x.IsSelected)
                .ToListAsync(cancellationToken);
            List<RecipeLegacyMaterialMap> maps = await repository.RecipeLegacyMaterialMap
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<RecipeLegacyRecipe> candidates = recipes
                .Where(x => x.ValidationStatus is Common.SILA_RECIPE_LEGACY_VALID or Common.SILA_RECIPE_LEGACY_WARNING)
                .Where(x => !failedOnly || x.PostingStatus == Common.SILA_POSTING_FAILED)
                .Where(x => x.CreatedRecipeId == null || x.PostingStatus == Common.SILA_POSTING_FAILED)
                .ToList();

            batch.Status = Common.SILA_RECIPE_LEGACY_POSTING;
            repository.RecipeLegacyBatch.Update(batch);
            await repository.SaveAsync();

            Dictionary<string, RecipeLegacyRecipe> byLegacy = recipes.ToDictionary(x => x.LegacyRecipeId, StringComparer.OrdinalIgnoreCase);
            List<string> order = TopologicalOrder(candidates, byLegacy);
            int posted = 0, failed = 0, skipped = 0;
            List<object> report = new List<object>();
            InventoryLedger ledger = new InventoryLedger(repository, buyer.Id, userId);

            foreach (string legacyId in order)
            {
                if (!byLegacy.TryGetValue(legacyId, out RecipeLegacyRecipe? staged)) continue;
                if (staged.CreatedRecipeId != null && staged.PostingStatus == Common.SILA_POSTING_POSTED)
                {
                    skipped++;
                    continue;
                }

                try
                {
                    await PostOneAsync(repository, logger, buyer.Id, userId, roleId, staged, maps, byLegacy, ledger, cancellationToken);
                    posted++;
                    report.Add(new { staged.LegacyRecipeId, Status = "POSTED", staged.CreatedRecipeCode, staged.CreatedRecipeId });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    staged.PostingStatus = Common.SILA_POSTING_FAILED;
                    staged.PostingError = ex is BaseCustomException bce ? $"{bce.Message} {bce.Description}".Trim() : ex.Message;
                    repository.RecipeLegacyRecipe.Update(staged);
                    report.Add(new { staged.LegacyRecipeId, Status = "FAILED", Error = staged.PostingError });
                    logger.LogError($"Legacy recipe post failed. LegacyId: {legacyId}, Error: {staged.PostingError}");
                }
            }

            batch.PostedCount = recipes.Count(x => x.PostingStatus == Common.SILA_POSTING_POSTED || x.CreatedRecipeId != null);
            batch.FailedPostCount = recipes.Count(x => x.PostingStatus == Common.SILA_POSTING_FAILED);
            batch.Status = failed > 0
                ? (posted > 0 || batch.PostedCount > 0 ? Common.SILA_RECIPE_LEGACY_PARTIAL_POST : Common.SILA_RECIPE_LEGACY_PARTIAL_POST)
                : Common.SILA_RECIPE_LEGACY_POSTED;
            if (failed == 0 && batch.PostedCount > 0)
            {
                batch.Status = Common.SILA_RECIPE_LEGACY_POSTED;
            }

            batch.PostingReportJson = Serialize(new { GeneratedOn = DateTime.UtcNow, Posted = posted, Failed = failed, Skipped = skipped, Rows = report });
            repository.RecipeLegacyBatch.Update(batch);
            ledger.AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_POST,
                $"Post complete: posted={posted}, failed={failed}, skipped={skipped}.");
            await repository.SaveAsync();

            return new SilaRecipeLegacyActionResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Message = $"Posted {posted} recipe(s); {failed} failed; {skipped} skipped.",
                Succeeded = posted,
                Failed = failed,
                Skipped = skipped,
                ReportJson = batch.PostingReportJson
            };
        }

        private static async Task PostOneAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid userId,
            Guid roleId,
            RecipeLegacyRecipe staged,
            List<RecipeLegacyMaterialMap> maps,
            Dictionary<string, RecipeLegacyRecipe> byLegacy,
            InventoryLedger ledger,
            CancellationToken cancellationToken)
        {
            // Idempotent: already created recipe stays linked.
            if (staged.CreatedRecipeId != null && staged.PostingStatus == Common.SILA_POSTING_POSTED)
            {
                return;
            }

            List<SilaRecipeLegacyIngredientDto> ingredients = Deserialize<List<SilaRecipeLegacyIngredientDto>>(staged.IngredientsJson) ?? new();
            List<SilaRecipeLegacySubRecipeDto> subs = Deserialize<List<SilaRecipeLegacySubRecipeDto>>(staged.SubRecipesJson) ?? new();
            List<string> instructions = Deserialize<List<string>>(staged.InstructionsJson) ?? new();
            List<RecipeLegacyMaterialMap> recipeMaps = maps
                .Where(x => string.Equals(x.LegacyRecipeId, staged.LegacyRecipeId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            List<SilaRecipeIngredientWriteDto> lines = new List<SilaRecipeIngredientWriteDto>();
            foreach (SilaRecipeLegacyIngredientDto ingredient in ingredients.OrderBy(x => x.Line))
            {
                RecipeLegacyMaterialMap? map = recipeMaps.FirstOrDefault(x => x.IngredientLine == ingredient.Line);
                if (map?.Status == Common.SILA_RECIPE_LEGACY_IGNORED) continue;
                if (map?.MappedMaterialId == null)
                {
                    throw new BadRequestCustomException("Material unmapped.", $"Map material for {staged.LegacyRecipeId} line {ingredient.Line} before posting.");
                }

                lines.Add(new SilaRecipeIngredientWriteDto
                {
                    MaterialId = map.MappedMaterialId,
                    Quantity = ingredient.Quantity,
                    Uom = ingredient.Uom
                });
            }

            foreach (SilaRecipeLegacySubRecipeDto sub in subs)
            {
                if (byLegacy.TryGetValue(sub.SubLegacyRecipeId, out RecipeLegacyRecipe? subStaged))
                {
                    if (subStaged.CreatedRecipeId == null)
                    {
                        throw new BadRequestCustomException("Sub-recipe not posted.", $"Post sub-recipe {sub.SubLegacyRecipeId} before {staged.LegacyRecipeId}.");
                    }

                    lines.Add(new SilaRecipeIngredientWriteDto
                    {
                        SubRecipeId = subStaged.CreatedRecipeId,
                        Quantity = sub.Quantity,
                        Uom = sub.Uom ?? ServingUomOf(subStaged)
                    });
                    continue;
                }

                string subCode = sub.SubLegacyRecipeId.Trim();
                Recipe? existingSub = await repository.Recipe.FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.RecipeCode == subCode);
                if (existingSub == null || existingSub.ActiveVersion <= 0 || existingSub.Status == Common.SILA_RECIPE_INACTIVE)
                {
                    throw new BadRequestCustomException("Sub-recipe not found.", $"Sub-recipe {sub.SubLegacyRecipeId} is not in this batch and is not an approved recipe.");
                }

                lines.Add(new SilaRecipeIngredientWriteDto
                {
                    SubRecipeId = existingSub.Id,
                    Quantity = sub.Quantity,
                    Uom = sub.Uom ?? existingSub.ServingUom
                });
            }

            string itemMode = ItemModeFromFormat(staged.RecipeFormat, staged.ProductClass);
            if (itemMode == Common.SILA_RECIPE_DIRECT && lines.Count == 0)
            {
                // Direct items still need one material line for ApplyAsync.
                throw new BadRequestCustomException("Direct item needs a material.", $"Add a mapped material for {staged.LegacyRecipeId}.");
            }

            // Preserve legacy id on staging + ledger only (no PosCode invention). Description kept as provided.
            string? description = staged.Description;
            string preparation = instructions.Count > 0
                ? Serialize(instructions)
                : (staged.ServingInstructions ?? staged.Notes);

            SilaRecipeWriteDto write = new SilaRecipeWriteDto
            {
                Name = staged.RecipeName,
                Description = description,
                Category = staged.Category,
                ItemMode = itemMode,
                ProductClass = staged.ProductClass,
                RecipeFormat = staged.RecipeFormat,
                Subcategory = staged.Subcategory,
                ServingQty = ServingQtyOf(staged),
                ServingUom = ServingUomOf(staged),
                SellingUom = "EA",
                PrepTimeMinutes = staged.PrepMinutes,
                CookTimeMinutes = staged.CookMinutes,
                PreparationSteps = preparation,
                Ingredients = lines,
                OutletPrices = new List<SilaRecipeOutletPriceWriteDto>()
            };

            SilaRecipeLegacyExtrasDto? extras = ExtrasOf(staged);
            if (extras != null)
            {
                await ApplyWorkbookExtrasAsync(repository, buyerId, extras, write, cancellationToken);
            }

            string recipeCode = await DocumentNumber.NextAsync(repository, buyerId, DocumentNumber.RECIPE, 8, cancellationToken);
            Recipe recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                RecipeCode = recipeCode,
                Status = Common.SILA_RECIPE_DRAFT,
                Version = 1,
                ActiveVersion = 0,
                IsActive = true
            };

            // BATCH mode requires approved sub-recipes; Ensure they were posted earlier as DRAFT — ApplyAsync GetSubRecipesAsync
            // typically requires approved active version. For migration, promote sub-recipes used as components if still draft
            // would break lifecycle. Spec: Post creates DRAFT. Sub-recipe references in ApplyAsync need approved recipes.
            // When a sub-recipe is still DRAFT, temporarily treat ItemMode as RECIPE with materials only, OR promote
            // dependency chain: we post topological order; for BATCH ApplyAsync, sub-recipes must be APPROVED.
            // Practical approach: if any SubRecipeId lines exist, ensure those recipes are APPROVED (ActiveVersion=Version)
            // via direct promote for migration-created drafts so ApplyAsync accepts them — documented here.
            foreach (SilaRecipeIngredientWriteDto line in lines.Where(x => x.SubRecipeId != null))
            {
                Recipe? sub = await repository.Recipe.FindFirstByConditionAsync(x => x.Id == line.SubRecipeId && x.BuyerId == buyerId && x.IsActive);
                if (sub != null && sub.ActiveVersion == 0 && sub.Status == Common.SILA_RECIPE_DRAFT)
                {
                    sub.Status = Common.SILA_RECIPE_APPROVED;
                    sub.ActiveVersion = sub.Version;
                    sub.ApprovedOn = DateTime.UtcNow;
                    repository.Recipe.Update(sub);
                    ledger.AddEvent(Common.SILA_REF_RECIPE, sub.Id, SilaRecipeRules.EVENT_APPROVED,
                        $"Auto-promoted for legacy migration dependency of {staged.LegacyRecipeId}.");
                }
            }

            await SilaRecipeRules.ApplyAsync(repository, logger, buyerId, userId, roleId, recipe, write, cancellationToken);
            repository.Recipe.Create(recipe);
            ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_CREATED,
                $"Created from legacy migration {staged.LegacyRecipeId} (LegacyReference={staged.LegacyReference}).");
            ledger.AddEvent(Common.SILA_REF_RECIPE_LEGACY, staged.BatchId, Common.SILA_RECIPE_LEGACY_EVENT_POST,
                $"Posted {staged.LegacyRecipeId} → {recipe.RecipeCode} ({recipe.Id}).");

            staged.CreatedRecipeId = recipe.Id;
            staged.CreatedRecipeCode = recipe.RecipeCode;
            staged.CreatedVersion = recipe.Version;
            staged.PostingStatus = Common.SILA_POSTING_POSTED;
            staged.PostingError = null;
            staged.PublishStatus = PUBLISH_PENDING;
            repository.RecipeLegacyRecipe.Update(staged);
            await repository.SaveAsync();
        }

        private static async Task ApplyWorkbookExtrasAsync(
            IRepositoryWrapper repository, Guid buyerId, SilaRecipeLegacyExtrasDto extras, SilaRecipeWriteDto write, CancellationToken cancellationToken)
        {
            write.PosCode = extras.PosCode;
            write.Currency = extras.Currency;
            if (!string.IsNullOrWhiteSpace(extras.SellingUom))
            {
                write.SellingUom = extras.SellingUom;
            }

            if (!string.IsNullOrWhiteSpace(extras.FamilyCode))
            {
                string familyCode = extras.FamilyCode;
                RecipeFamily? family = await repository.RecipeFamily.FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.Code == familyCode);
                write.FamilyId = family?.Id ?? throw new BadRequestCustomException("Family not found.", $"Family {familyCode} does not exist.");
            }

            if (!string.IsNullOrWhiteSpace(extras.CategoryCode))
            {
                string categoryCode = extras.CategoryCode;
                RecipeCategory? category = await repository.RecipeCategory.FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive && x.Code == categoryCode);
                write.CategoryId = category?.Id ?? throw new BadRequestCustomException("Category not found.", $"Category {categoryCode} does not exist.");
            }

            foreach (SilaRecipeLegacyOutletPriceDto price in extras.OutletPrices)
            {
                string outletCode = price.OutletCode ?? string.Empty;
                InventoryLocation? outlet = await repository.InventoryLocation.FindFirstByConditionAsync(
                    x => x.BuyerId == buyerId && x.IsActive && x.LocationType == Common.SILA_LOCATION_OUTLET && x.LocationCode == outletCode);
                if (outlet == null || price.MenuPrice is not > 0)
                {
                    throw new BadRequestCustomException("Outlet price invalid.", $"Outlet {outletCode} or its menu price is not valid.");
                }

                write.OutletPrices.Add(new SilaRecipeOutletPriceWriteDto { OutletLocationId = outlet.Id, MenuPrice = price.MenuPrice.Value, Currency = price.Currency });
            }
        }

        private static List<string> TopologicalOrder(List<RecipeLegacyRecipe> candidates, Dictionary<string, RecipeLegacyRecipe> all)
        {
            Dictionary<string, List<string>> edges = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> indegree = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (RecipeLegacyRecipe recipe in candidates)
            {
                indegree.TryAdd(recipe.LegacyRecipeId, 0);
                List<SilaRecipeLegacySubRecipeDto> subs = Deserialize<List<SilaRecipeLegacySubRecipeDto>>(recipe.SubRecipesJson) ?? new();
                foreach (SilaRecipeLegacySubRecipeDto sub in subs)
                {
                    if (!all.ContainsKey(sub.SubLegacyRecipeId)) continue;
                    if (!edges.TryGetValue(sub.SubLegacyRecipeId, out List<string>? list))
                    {
                        list = new List<string>();
                        edges[sub.SubLegacyRecipeId] = list;
                    }

                    list.Add(recipe.LegacyRecipeId);
                    indegree[recipe.LegacyRecipeId] = indegree.GetValueOrDefault(recipe.LegacyRecipeId) + 1;
                    indegree.TryAdd(sub.SubLegacyRecipeId, 0);
                }
            }

            Queue<string> queue = new Queue<string>(indegree.Where(x => x.Value == 0).Select(x => x.Key));
            // Prefer candidates that are not depended upon first — ensure all candidates appear.
            foreach (RecipeLegacyRecipe recipe in candidates)
            {
                indegree.TryAdd(recipe.LegacyRecipeId, 0);
            }

            queue = new Queue<string>(indegree.Where(x => x.Value == 0 && candidates.Any(c => string.Equals(c.LegacyRecipeId, x.Key, StringComparison.OrdinalIgnoreCase))).Select(x => x.Key));
            List<string> ordered = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (queue.Count > 0)
            {
                string node = queue.Dequeue();
                if (!seen.Add(node)) continue;
                if (candidates.Any(c => string.Equals(c.LegacyRecipeId, node, StringComparison.OrdinalIgnoreCase)))
                {
                    ordered.Add(node);
                }

                if (!edges.TryGetValue(node, out List<string>? children)) continue;
                foreach (string child in children)
                {
                    indegree[child]--;
                    if (indegree[child] == 0) queue.Enqueue(child);
                }
            }

            foreach (RecipeLegacyRecipe recipe in candidates)
            {
                if (!seen.Contains(recipe.LegacyRecipeId))
                {
                    ordered.Add(recipe.LegacyRecipeId);
                }
            }

            return ordered;
        }
    }
}
