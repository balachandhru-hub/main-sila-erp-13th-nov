using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public static partial class SilaRecipeLegacyMigration
    {
        /// <summary>
        /// Publish posted DRAFT recipes: readiness check → submit (PENDING_APPROVAL) → if canApprove, decide approve to ActiveVersion.
        /// Mirrors SubmitSilaRecipe + DecideSilaRecipe promote path without changing those handlers.
        /// </summary>
        public static async Task<SilaRecipeLegacyActionResultDto> PublishBatchAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IConfiguration configuration,
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid batchId,
            bool canApprove,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status is not (Common.SILA_RECIPE_LEGACY_POSTED or Common.SILA_RECIPE_LEGACY_PARTIAL_POST
                or Common.SILA_RECIPE_LEGACY_IMAGES_ASSIGNED or Common.SILA_RECIPE_LEGACY_PARTIAL_PUBLISH
                or Common.SILA_RECIPE_LEGACY_PUBLISHED))
            {
                throw new BadRequestCustomException("Batch is not ready to publish.", "Post recipes before publishing.");
            }

            if (!canApprove)
            {
                throw new ForBiddenCustomException("Approval permission required.", "Publishing to ActiveVersion requires APPROVE_SILA_RECIPE.");
            }

            List<RecipeLegacyRecipe> staged = await repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive && x.IsSelected
                    && x.CreatedRecipeId != null
                    && x.PostingStatus == Common.SILA_POSTING_POSTED)
                .ToListAsync(cancellationToken);

            int published = 0, failed = 0, skipped = 0;
            List<object> report = new List<object>();
            InventoryLedger ledger = new InventoryLedger(repository, buyer.Id, userId);
            string? basePath = configuration[Common.BASE_FOLDER_PATH];

            foreach (RecipeLegacyRecipe row in staged)
            {
                try
                {
                    Recipe recipe = await SilaRecipeRules.GetTrackedAsync(repository, logger, buyer.Id, row.CreatedRecipeId!.Value);
                    if (recipe.Status == Common.SILA_RECIPE_APPROVED && recipe.ActiveVersion == recipe.Version)
                    {
                        row.PublishStatus = PUBLISH_PUBLISHED;
                        skipped++;
                        report.Add(new { row.LegacyRecipeId, Status = "SKIPPED", Reason = "Already approved." });
                        repository.RecipeLegacyRecipe.Update(row);
                        continue;
                    }

                    if (recipe.Status == Common.SILA_RECIPE_DRAFT)
                    {
                        SilaRecipeReadiness readiness = await SilaRecipeReadiness.LoadAsync(repository, buyer.Id, new List<Guid> { recipe.Id }, cancellationToken);
                        SilaRecipeReadinessDto check = readiness.Evaluate(recipe, recipe.Version);
                        if (!check.Ready)
                        {
                            // Migration publish: outlet prices may be missing — treat as soft fail with clear message.
                            throw new BadRequestCustomException(
                                "Recipe is not ready for approval.",
                                string.Join("; ", check.Issues.Take(5)));
                        }

                        List<RecipeIngredient> ingredients = readiness.IngredientsOf(recipe.Id, recipe.Version);
                        decimal totalCost = 0;
                        foreach (RecipeIngredient ingredient in ingredients)
                        {
                            ItemBuyerMaster? material = readiness.MaterialOf(ingredient);
                            if (material != null)
                            {
                                ingredient.BaseQuantity = readiness.BaseQuantityOf(ingredient) ?? 0;
                                ingredient.BaseUom = UomConverter.BaseUomOf(material);
                                ingredient.UnitCost = material.UnitCost;
                                ingredient.Cost = Math.Round(ingredient.BaseQuantity * (material.UnitCost ?? 0), 4);
                            }

                            totalCost += ingredient.Cost;
                        }

                        repository.RecipeIngredient.UpdateRange(ingredients);
                        SilaRecipeRules.SetVersionCost(recipe, Math.Round(totalCost, 4));

                        ApprovalScope scope = await SilaRecipeRules.GetApprovalScopeAsync(repository, recipe, cancellationToken);
                        try
                        {
                            await SilaApprovals.StartAsync(
                                repository, logger, buyer.Id, Common.SILA_APPROVAL_TYPE_RECIPE,
                                Common.SILA_REF_RECIPE, recipe.Id, recipe.Version, scope, cancellationToken);
                            recipe.Status = Common.SILA_RECIPE_PENDING_APPROVAL;
                            recipe.SubmittedBy = userId;
                            recipe.SubmittedOn = DateTime.UtcNow;
                            ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_SUBMITTED,
                                $"Submitted from legacy migration publish ({batch.BatchNumber}).");
                        }
                        catch (BadRequestCustomException)
                        {
                            // No RECIPE flow: permission-based promote for migration when caller has APPROVE_SILA_RECIPE.
                            recipe.Status = Common.SILA_RECIPE_APPROVED;
                            recipe.ActiveVersion = recipe.Version;
                            recipe.ApprovedOn = DateTime.UtcNow;
                            SilaRecipeRules.PromoteDraftHeader(recipe);
                            if (!string.IsNullOrWhiteSpace(basePath))
                            {
                                SilaRecipePictureRules.PromoteDraftToActive(basePath, buyer.Id, recipe.Id, logger);
                            }

                            ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_APPROVED,
                                $"Published from legacy migration without MasterApprovalFlow ({batch.BatchNumber}).");
                            row.PublishStatus = PUBLISH_PUBLISHED;
                            published++;
                            report.Add(new { row.LegacyRecipeId, Status = "PUBLISHED", Mode = "PERMISSION" });
                            repository.Recipe.Update(recipe);
                            repository.RecipeLegacyRecipe.Update(row);
                            continue;
                        }
                    }

                    if (recipe.Status == Common.SILA_RECIPE_PENDING_APPROVAL)
                    {
                        try
                        {
                            ApprovalDecision decision = await SilaApprovals.DecideAsync(
                                repository, logger, Common.SILA_REF_RECIPE, recipe.Id, recipe.Version,
                                userId, true, $"Legacy migration publish {batch.BatchNumber}", cancellationToken,
                                SilaAccess.IsAdministrator(roleId));
                            if (decision.Outcome == SilaApprovals.OUTCOME_APPROVED)
                            {
                                recipe.Status = Common.SILA_RECIPE_APPROVED;
                                recipe.ActiveVersion = recipe.Version;
                                SilaRecipeRules.PromoteDraftHeader(recipe);
                                if (!string.IsNullOrWhiteSpace(basePath))
                                {
                                    SilaRecipePictureRules.PromoteDraftToActive(basePath, buyer.Id, recipe.Id, logger);
                                }

                                recipe.ApprovedOn = DateTime.UtcNow;
                                ledger.AddEvent(Common.SILA_REF_RECIPE, recipe.Id, SilaRecipeRules.EVENT_APPROVED,
                                    $"Approved via legacy migration publish ({batch.BatchNumber}).");
                                row.PublishStatus = PUBLISH_PUBLISHED;
                                published++;
                                report.Add(new { row.LegacyRecipeId, Status = "PUBLISHED", Mode = "FLOW" });
                            }
                            else if (decision.Outcome == SilaApprovals.OUTCOME_PENDING)
                            {
                                row.PublishStatus = PUBLISH_PENDING;
                                skipped++;
                                report.Add(new { row.LegacyRecipeId, Status = "PENDING", Reason = $"Waiting level {decision.Level + 1}" });
                            }
                        }
                        catch (BaseCustomException ex)
                        {
                            // Caller has APPROVE_SILA_RECIPE but is not in the flow — leave pending for configured approvers.
                            row.PublishStatus = PUBLISH_PENDING;
                            skipped++;
                            report.Add(new { row.LegacyRecipeId, Status = "PENDING", Reason = ex.Message });
                        }
                    }

                    repository.Recipe.Update(recipe);
                    repository.RecipeLegacyRecipe.Update(row);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    row.PublishStatus = PUBLISH_FAILED;
                    row.PostingError = ex is BaseCustomException bce ? $"{bce.Message} {bce.Description}".Trim() : ex.Message;
                    repository.RecipeLegacyRecipe.Update(row);
                    report.Add(new { row.LegacyRecipeId, Status = "FAILED", Error = row.PostingError });
                }
            }

            batch.PublishedCount = staged.Count(x => x.PublishStatus == PUBLISH_PUBLISHED);
            batch.Status = failed > 0 || staged.Any(x => x.PublishStatus == PUBLISH_PENDING)
                ? Common.SILA_RECIPE_LEGACY_PARTIAL_PUBLISH
                : Common.SILA_RECIPE_LEGACY_PUBLISHED;
            if (published > 0 && failed == 0 && !staged.Any(x => x.PublishStatus == PUBLISH_PENDING || x.PublishStatus == PUBLISH_FAILED))
            {
                batch.Status = Common.SILA_RECIPE_LEGACY_PUBLISHED;
            }

            batch.PublishReportJson = Serialize(new { GeneratedOn = DateTime.UtcNow, Published = published, Failed = failed, Skipped = skipped, Rows = report });
            repository.RecipeLegacyBatch.Update(batch);
            ledger.AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_PUBLISH,
                $"Publish complete: published={published}, failed={failed}, skipped={skipped}.");
            await repository.SaveAsync();

            return new SilaRecipeLegacyActionResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Message = $"Published {published}; failed {failed}; skipped/pending {skipped}.",
                Succeeded = published,
                Failed = failed,
                Skipped = skipped,
                ReportJson = batch.PublishReportJson
            };
        }

        public static async Task<SilaRecipeLegacyRecipePageDto> ListRecipesAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid batchId,
            string? status,
            string? search,
            int index,
            int limit,
            CancellationToken cancellationToken)
        {
            int take = Math.Clamp(limit, 1, 200);
            int skip = Math.Max(0, index);
            IQueryable<RecipeLegacyRecipe> query = repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyerId && x.IsActive);
            if (!string.IsNullOrWhiteSpace(status))
            {
                string s = status.Trim().ToUpperInvariant();
                query = query.Where(x => x.ValidationStatus == s || x.PostingStatus == s || x.PublishStatus == s);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();
                query = query.Where(x => x.LegacyRecipeId.Contains(term) || x.RecipeName.Contains(term)
                    || (x.CreatedRecipeCode != null && x.CreatedRecipeCode.Contains(term)));
            }

            int total = await query.CountAsync(cancellationToken);
            List<RecipeLegacyRecipe> items = await query
                .OrderBy(x => x.SourceRow)
                .ThenBy(x => x.LegacyRecipeId)
                .Skip(skip)
                .Take(take)
                .ToListAsync(cancellationToken);
            return new SilaRecipeLegacyRecipePageDto
            {
                Items = items.Select(ToRecipeListItem).ToList(),
                Total = total,
                Index = skip,
                Limit = take
            };
        }

        public static async Task<SilaRecipeLegacyRecipeDetailDto> GetRecipeDetailAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid batchId,
            Guid recipeId,
            CancellationToken cancellationToken)
        {
            RecipeLegacyRecipe? recipe = await repository.RecipeLegacyRecipe
                .FindByCondition(x => x.Id == recipeId && x.BatchId == batchId && x.BuyerId == buyerId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (recipe == null)
            {
                logger.LogError($"Legacy staged recipe not found. RecipeId: {recipeId}, BatchId: {batchId}");
                throw new NotFoundCustomException("Staged recipe not found.", "Select a recipe of this migration batch.");
            }

            List<RecipeLegacyMaterialMap> maps = await repository.RecipeLegacyMaterialMap
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyerId && x.IsActive
                    && x.LegacyRecipeId == recipe.LegacyRecipeId)
                .OrderBy(x => x.IngredientLine)
                .ToListAsync(cancellationToken);

            SilaRecipeLegacyRecipeListItemDto list = ToRecipeListItem(recipe);
            return new SilaRecipeLegacyRecipeDetailDto
            {
                Id = list.Id,
                LegacyRecipeId = list.LegacyRecipeId,
                RecipeName = list.RecipeName,
                RecipeFormat = list.RecipeFormat,
                ProductClass = list.ProductClass,
                Category = list.Category,
                ValidationStatus = list.ValidationStatus,
                ErrorCount = list.ErrorCount,
                WarningCount = list.WarningCount,
                PostingStatus = list.PostingStatus,
                ImageStatus = list.ImageStatus,
                PublishStatus = list.PublishStatus,
                CreatedRecipeId = list.CreatedRecipeId,
                CreatedRecipeCode = list.CreatedRecipeCode,
                IsSelected = list.IsSelected,
                SourceRow = list.SourceRow,
                BatchId = recipe.BatchId,
                Description = recipe.Description,
                Subcategory = recipe.Subcategory,
                YieldQty = recipe.YieldQty,
                YieldUom = recipe.YieldUom,
                PortionQty = recipe.PortionQty,
                PortionUom = recipe.PortionUom,
                PrepMinutes = recipe.PrepMinutes,
                CookMinutes = recipe.CookMinutes,
                ServingInstructions = recipe.ServingInstructions,
                OutletScope = recipe.OutletScope,
                ActiveStatus = recipe.ActiveStatus,
                LegacyReference = recipe.LegacyReference,
                Notes = recipe.Notes,
                IngredientsJson = recipe.IngredientsJson,
                InstructionsJson = recipe.InstructionsJson,
                SubRecipesJson = recipe.SubRecipesJson,
                VariantsJson = recipe.VariantsJson,
                FindingsJson = recipe.FindingsJson,
                MaterialMappingsJson = recipe.MaterialMappingsJson,
                CreatedVersion = recipe.CreatedVersion,
                PostingError = recipe.PostingError,
                MaterialMaps = maps.Select(x => new SilaRecipeLegacyMaterialMapDto
                {
                    Id = x.Id,
                    LegacyRecipeId = x.LegacyRecipeId,
                    IngredientLine = x.IngredientLine,
                    LegacyMaterialNumber = x.LegacyMaterialNumber,
                    MaterialNumber = x.MaterialNumber,
                    MaterialDescription = x.MaterialDescription,
                    MappedMaterialId = x.MappedMaterialId,
                    Status = x.Status
                }).ToList()
            };
        }
    }
}
