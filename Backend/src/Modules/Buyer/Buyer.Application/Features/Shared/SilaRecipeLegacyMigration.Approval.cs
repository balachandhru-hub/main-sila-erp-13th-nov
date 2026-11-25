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
        public static async Task<SilaRecipeLegacyActionResultDto> SubmitForApprovalAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid batchId,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status is not (Common.SILA_RECIPE_LEGACY_VALIDATED or Common.SILA_RECIPE_LEGACY_RETURNED
                or Common.SILA_RECIPE_LEGACY_REJECTED))
            {
                throw new BadRequestCustomException("Batch is not ready for approval.", "Validate the batch first, then submit it.");
            }

            if (batch.ErrorCount > 0 || batch.UnmappedCount > 0)
            {
                throw new BadRequestCustomException("Batch still has blocking validation issues.", "Fix INVALID and UNMAPPED recipes before submitting.");
            }

            List<RecipeLegacyRecipe> eligible = await repository.RecipeLegacyRecipe
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive && x.IsSelected
                    && (x.ValidationStatus == Common.SILA_RECIPE_LEGACY_VALID || x.ValidationStatus == Common.SILA_RECIPE_LEGACY_WARNING))
                .ToListAsync(cancellationToken);
            if (eligible.Count == 0)
            {
                throw new BadRequestCustomException("No eligible recipes.", "Select at least one VALID or WARNING recipe to submit.");
            }

            // Prefer existing RECIPE MasterApprovalFlow so approval screens keep working.
            // Reference type is RECIPE_LEGACY so tracker events stay on the batch.
            // If ResolveFlow fails with BadRequest (no flow), fall back to single-step permission-based approval
            // recorded on the batch (controller still requires APPROVE_SILA_RECIPE for Decide).
            bool usedFlow = false;
            try
            {
                ApprovalScope scope = new ApprovalScope();
                await SilaApprovals.StartAsync(
                    repository, logger, buyer.Id, Common.SILA_APPROVAL_TYPE_RECIPE,
                    Common.SILA_REF_RECIPE_LEGACY, batch.Id, batch.Revision, scope, cancellationToken);
                usedFlow = true;
            }
            catch (BadRequestCustomException ex)
            {
                logger.LogInfo($"No RECIPE approval flow for legacy batch; permission-based decide will be used. BatchId: {batch.Id}, Detail: {ex.Message}");
                usedFlow = false;
            }

            batch.Status = Common.SILA_RECIPE_LEGACY_PENDING_APPROVAL;
            batch.ApprovalSnapshotJson = Serialize(new
            {
                SubmittedOn = DateTime.UtcNow,
                SubmittedBy = userId,
                Eligible = eligible.Count,
                UsedApprovalFlow = usedFlow,
                RecipeIds = eligible.Select(x => x.LegacyRecipeId).ToList()
            });
            batch.RejectedBy = null;
            batch.RejectedOn = null;
            repository.RecipeLegacyBatch.Update(batch);

            new InventoryLedger(repository, buyer.Id, userId)
                .AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_APPROVAL,
                    $"Submitted for approval ({eligible.Count} recipes). Flow={(usedFlow ? "RECIPE" : "PERMISSION")}.");
            await repository.SaveAsync();

            return new SilaRecipeLegacyActionResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Message = usedFlow
                    ? "Batch submitted for RECIPE approval flow."
                    : "Batch submitted. Approvers with APPROVE_SILA_RECIPE can decide without a MasterApprovalFlow.",
                Succeeded = eligible.Count
            };
        }

        public static async Task<SilaRecipeLegacyActionResultDto> DecideBatchAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid batchId,
            SilaRecipeLegacyDecideDto request,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status != Common.SILA_RECIPE_LEGACY_PENDING_APPROVAL)
            {
                throw new BadRequestCustomException("Batch is not pending approval.", "Only a submitted batch can be approved, rejected or returned.");
            }

            string decision = (request.Decision ?? string.Empty).Trim().ToLowerInvariant();
            if (decision is not ("approve" or "reject" or "return"))
            {
                throw new BadRequestCustomException("Decision is invalid.", "Use approve, reject or return.");
            }

            bool needsComment = decision is "reject" or "return";
            EnsureComment(logger, request.Comment, needsComment);
            string? comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

            // Try multi-level RECIPE flow first (reference RECIPE_LEGACY). If no pending steps / ResolveFlow
            // path fails, fall back to permission-gated single-step decide (caller must have APPROVE_SILA_RECIPE).
            bool flowHandled = false;
            if (decision is "approve" or "reject")
            {
                try
                {
                    ApprovalDecision outcome = await SilaApprovals.DecideAsync(
                        repository, logger, Common.SILA_REF_RECIPE_LEGACY, batch.Id, batch.Revision,
                        userId, decision == "approve", comment, cancellationToken, SilaAccess.IsAdministrator(roleId));
                    if (outcome.Outcome == SilaApprovals.OUTCOME_PENDING)
                    {
                        batch.ApprovalComment = comment;
                        repository.RecipeLegacyBatch.Update(batch);
                        new InventoryLedger(repository, buyer.Id, userId)
                            .AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_APPROVAL,
                                $"Level {outcome.Level} approved; waiting for next approver.");
                        await repository.SaveAsync();
                        return new SilaRecipeLegacyActionResultDto
                        {
                            BatchId = batch.Id,
                            BatchNumber = batch.BatchNumber,
                            Status = batch.Status,
                            Message = $"Approval level {outcome.Level} recorded; more levels pending.",
                            Succeeded = 1
                        };
                    }

                    flowHandled = true;
                    if (outcome.Outcome == SilaApprovals.OUTCOME_REJECTED)
                    {
                        decision = "reject";
                    }
                    else if (outcome.Outcome == SilaApprovals.OUTCOME_APPROVED)
                    {
                        decision = "approve";
                    }
                }
                catch (BadRequestCustomException ex)
                {
                    logger.LogInfo($"Permission-based batch decide fallback. BatchId: {batch.Id}, Detail: {ex.Message}");
                    flowHandled = false;
                }
            }

            if (decision == "approve")
            {
                batch.Status = Common.SILA_RECIPE_LEGACY_APPROVED;
                batch.ApprovedBy = userId;
                batch.ApprovedOn = DateTime.UtcNow;
                batch.ApprovalComment = comment;
                batch.RejectedBy = null;
                batch.RejectedOn = null;
            }
            else if (decision == "reject")
            {
                batch.Status = Common.SILA_RECIPE_LEGACY_REJECTED;
                batch.RejectedBy = userId;
                batch.RejectedOn = DateTime.UtcNow;
                batch.ApprovalComment = comment;
            }
            else
            {
                batch.Status = Common.SILA_RECIPE_LEGACY_RETURNED;
                batch.RejectedBy = userId;
                batch.RejectedOn = DateTime.UtcNow;
                batch.ApprovalComment = comment;
            }

            repository.RecipeLegacyBatch.Update(batch);
            new InventoryLedger(repository, buyer.Id, userId)
                .AddEvent(Common.SILA_REF_RECIPE_LEGACY, batch.Id, Common.SILA_RECIPE_LEGACY_EVENT_APPROVAL,
                    $"{decision.ToUpperInvariant()} (flowHandled={flowHandled}): {comment}");
            await repository.SaveAsync();

            return new SilaRecipeLegacyActionResultDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                Status = batch.Status,
                Message = $"Batch {decision}d.",
                Succeeded = 1
            };
        }

        public static async Task UpdateMaterialMapsAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid organizationId,
            Guid userId,
            Guid batchId,
            List<SilaRecipeLegacyMaterialMapWriteDto> mappings,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(repository, logger, organizationId);
            RecipeLegacyBatch batch = await GetBatchAsync(repository, logger, buyer.Id, batchId, cancellationToken);
            if (batch.Status is Common.SILA_RECIPE_LEGACY_POSTING or Common.SILA_RECIPE_LEGACY_POSTED
                or Common.SILA_RECIPE_LEGACY_PARTIAL_POST or Common.SILA_RECIPE_LEGACY_PUBLISHED
                or Common.SILA_RECIPE_LEGACY_PARTIAL_PUBLISH)
            {
                throw new BadRequestCustomException("Mappings are locked.", "Material maps cannot change after posting.");
            }

            if (mappings == null || mappings.Count == 0)
            {
                throw new BadRequestCustomException("Mappings are required.", "Send at least one material mapping.");
            }

            List<RecipeLegacyMaterialMap> existing = await repository.RecipeLegacyMaterialMap
                .FindByCondition(x => x.BatchId == batchId && x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            HashSet<string> codes = mappings
                .Select(x => x.MaterialNumber)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, ItemBuyerMaster> byCode = codes.Count == 0
                ? new Dictionary<string, ItemBuyerMaster>(StringComparer.OrdinalIgnoreCase)
                : await repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.MaterialCode != null && codes.Contains(x.MaterialCode))
                    .ToDictionaryAsync(x => x.MaterialCode!, StringComparer.OrdinalIgnoreCase, cancellationToken);

            List<Guid> ids = mappings.Where(x => x.MappedMaterialId != null).Select(x => x.MappedMaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> byId = ids.Count == 0
                ? new Dictionary<Guid, ItemBuyerMaster>()
                : await repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && ids.Contains(x.Id))
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

            foreach (SilaRecipeLegacyMaterialMapWriteDto write in mappings)
            {
                RecipeLegacyMaterialMap? map = write.Id != null
                    ? existing.FirstOrDefault(x => x.Id == write.Id.Value)
                    : existing.FirstOrDefault(x =>
                        string.Equals(x.LegacyRecipeId, write.LegacyRecipeId, StringComparison.OrdinalIgnoreCase)
                        && x.IngredientLine == write.IngredientLine);
                if (map == null)
                {
                    throw new NotFoundCustomException("Material map not found.", $"No map for {write.LegacyRecipeId} line {write.IngredientLine}.");
                }

                string status = (write.Status ?? Common.SILA_RECIPE_LEGACY_MAPPED).Trim().ToUpperInvariant();
                if (status is not (Common.SILA_RECIPE_LEGACY_MAPPED or Common.SILA_RECIPE_LEGACY_UNMAPPED or Common.SILA_RECIPE_LEGACY_IGNORED))
                {
                    throw new BadRequestCustomException("Map status is invalid.", "Use MAPPED, UNMAPPED or IGNORED.");
                }

                if (status == Common.SILA_RECIPE_LEGACY_IGNORED)
                {
                    map.Status = Common.SILA_RECIPE_LEGACY_IGNORED;
                    map.MappedMaterialId = null;
                    continue;
                }

                if (status == Common.SILA_RECIPE_LEGACY_UNMAPPED)
                {
                    map.Status = Common.SILA_RECIPE_LEGACY_UNMAPPED;
                    map.MappedMaterialId = null;
                    map.MaterialNumber = write.MaterialNumber?.Trim();
                    continue;
                }

                ItemBuyerMaster? material = null;
                if (write.MappedMaterialId != null && byId.TryGetValue(write.MappedMaterialId.Value, out ItemBuyerMaster? byIdHit))
                {
                    material = byIdHit;
                }
                else if (!string.IsNullOrWhiteSpace(write.MaterialNumber) && byCode.TryGetValue(write.MaterialNumber.Trim(), out ItemBuyerMaster? byCodeHit))
                {
                    material = byCodeHit;
                }

                if (material == null)
                {
                    throw new BadRequestCustomException("Material not found.", $"Map line {write.IngredientLine} to an active Item Master material.");
                }

                map.MappedMaterialId = material.Id;
                map.MaterialNumber = material.MaterialCode;
                map.MaterialDescription = material.Description;
                map.Status = Common.SILA_RECIPE_LEGACY_MAPPED;
            }

            repository.RecipeLegacyMaterialMap.UpdateRange(existing);
            await repository.SaveAsync();
            logger.LogInfo($"Legacy material maps updated. BatchId: {batchId}, Count: {mappings.Count}, UserId: {userId}");
        }
    }
}
