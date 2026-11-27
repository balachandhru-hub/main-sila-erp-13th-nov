using System.Text.Json;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryReconciliationService
    {
        Task<SilaPhase4BDashboardDto> GetDashboardAsync(Guid organizationId, Guid? propertyId, bool canViewSalaryDetails, CancellationToken cancellationToken);
        Task<SilaCulinaryReconciliationConfigDto> UpsertConfigAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryReconciliationConfigDto request, CancellationToken cancellationToken);
        Task<SilaProductionConsumptionDto> RecordConsumptionAsync(Guid organizationId, Guid userId, string actorName, SilaRecordConsumptionRequestDto request, CancellationToken cancellationToken);
        Task<SilaAdditionalMaterialRequestDto> CreateOrDecideAdditionalRequestAsync(Guid organizationId, Guid userId, string actorName, SilaAdditionalMaterialRequestDto request, bool isDecision, CancellationToken cancellationToken);
        Task<SilaMaterialReturnDto> RecordOrInspectReturnAsync(Guid organizationId, Guid userId, string actorName, SilaMaterialReturnRequestDto request, Guid? inspectReturnId, bool? inspectionPassed, CancellationToken cancellationToken);
        Task<SilaMaterialIssueReversalDto> ReverseMaterialIssueAsync(Guid organizationId, Guid userId, string actorName, SilaMaterialIssueReversalRequestDto request, CancellationToken cancellationToken);
        Task<SilaWasteEventDto> RecordWasteEventAsync(Guid organizationId, Guid userId, string actorName, SilaRecordWasteEventRequestDto request, bool? approveDecision, Guid? wasteId, CancellationToken cancellationToken);
        Task<SilaFoodLeftoverDto> RecordOrDisposeLeftoverAsync(Guid organizationId, Guid userId, string actorName, SilaFoodLeftoverRequestDto request, Guid? leftoverId, CancellationToken cancellationToken);
        Task<SilaWasteDisposalDto> ConfirmDisposalAsync(Guid organizationId, Guid userId, string actorName, SilaWasteDisposalRequestDto request, bool approveSupervisor, Guid? disposalId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionOutputConfirmation> ConfirmProductionOutputAsync(Guid organizationId, Guid userId, string actorName, SilaOutputConfirmationRequestDto request, CancellationToken cancellationToken);
        Task<SilaPortionSampleDto> RecordPortionSampleAsync(Guid organizationId, Guid userId, string actorName, SilaPortionSampleRequestDto request, CancellationToken cancellationToken);
        Task<SilaProductionReconciliationDto> ReconcileProductionOrderAsync(Guid organizationId, Guid userId, string actorName, Guid productionOrderId, Guid? productionBatchId, CancellationToken cancellationToken);
        Task<SilaProductionReconciliationDto> SettleProductionAsync(Guid organizationId, Guid userId, string actorName, Guid reconciliationId, bool financialSettlement, CancellationToken cancellationToken);
        Task<SilaCulinaryLaborAllocation> RecordLaborAllocationAsync(Guid organizationId, Guid userId, string actorName, SilaLaborAllocationRequestDto request, CancellationToken cancellationToken);
        Task<List<SilaCulinaryCentralKitchenAllocation>> AllocateCentralKitchenCostAsync(Guid organizationId, Guid userId, string actorName, SilaCentralKitchenCostAllocationRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryBuffetReconciliation> ReconcileBuffetAsync(Guid organizationId, Guid userId, string actorName, SilaBuffetReconciliationRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventClosureSummaryDto> UpsertEventClosureAsync(Guid organizationId, Guid userId, string actorName, SilaEventClosureRequestDto request, bool closeOperational, bool closeFinancial, CancellationToken cancellationToken);
        Task<SilaCulinaryEventClosureSummaryDto> ReopenEventClosureAsync(Guid organizationId, Guid userId, string actorName, Guid closureId, SilaReopenClosureRequestDto request, CancellationToken cancellationToken);
        Task<SilaErpReconciliationRefDto> UpsertErpReconciliationAsync(Guid organizationId, Guid userId, string actorName, string silaTxRef, string txType, string erpSystem, string? erpDocRef, string postingStatus, string? errorMsg, string idempotencyKey, CancellationToken cancellationToken);
        Task<SilaHistoricalLearningSnapshotDto> CaptureHistoricalLearningSnapshotAsync(Guid organizationId, Guid userId, string actorName, Guid eventClosureId, CancellationToken cancellationToken);
        Task<SilaReconciliationCopilotResponseDto> AskCostControlCopilotAsync(Guid organizationId, SilaReconciliationCopilotRequestDto request, CancellationToken cancellationToken);
        Task<List<object>> Build27ReportsPayloadAsync(Guid organizationId, Guid? propertyId, bool canViewSalaryDetails, CancellationToken cancellationToken);
    }

    public class SilaCulinaryReconciliationService : ISilaCulinaryReconciliationService
    {
        private readonly IRepositoryWrapper _repository;
        private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public SilaCulinaryReconciliationService(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        private async Task<BuyerBusinessProfile> ResolveBuyerAsync(Guid organizationId, CancellationToken ct)
        {
            BuyerBusinessProfile? buyer = await _repository.BuyerBusinessProfile
                .FindFirstByConditionAsync(x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                throw new InvalidOperationException("Buyer business profile not found for tenant.");
            }
            return buyer;
        }

        private async Task EnsureFeatureEnabledAsync(Guid buyerId, CancellationToken ct)
        {
            SilaCulinaryAiConfiguration? cfg = await _repository.SilaCulinaryAiConfiguration
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.IsActive);
            if (cfg != null && !string.IsNullOrWhiteSpace(cfg.FeatureFlagsJson))
            {
                try
                {
                    SilaCulinaryAiFeatureFlagsDto? flags = JsonSerializer.Deserialize<SilaCulinaryAiFeatureFlagsDto>(cfg.FeatureFlagsJson, JsonOpts);
                    if (flags != null && !flags.CulinaryPhase4BEnabled)
                    {
                        throw new InvalidOperationException("Phase 4B Production Reconciliation is disabled for this tenant.");
                    }
                }
                catch (JsonException) { }
            }
        }

        private async Task<SilaCulinaryReconciliationConfig> GetOrCreateConfigEntityAsync(Guid buyerId, Guid? propertyId, CancellationToken ct)
        {
            SilaCulinaryReconciliationConfig? cfg = await _repository.SilaCulinaryReconciliationConfig
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.PropertyId == propertyId && x.IsActive);
            if (cfg == null && propertyId.HasValue)
            {
                cfg = await _repository.SilaCulinaryReconciliationConfig
                    .FindFirstByConditionAsync(x => x.BuyerId == buyerId && x.PropertyId == null && x.IsActive);
            }
            if (cfg == null)
            {
                cfg = new SilaCulinaryReconciliationConfig
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    PropertyId = propertyId,
                    IsActive = true
                };
                _repository.SilaCulinaryReconciliationConfig.Create(cfg);
                await _repository.SaveAsync();
            }
            return cfg;
        }

        private async Task AuditAsync(Guid buyerId, Guid? propertyId, string auditType, Guid actorId, string actorName, string summary, object payload)
        {
            _repository.SilaCulinaryReconciliationAudit.Create(new SilaCulinaryReconciliationAudit
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                PropertyId = propertyId,
                AuditType = auditType,
                ActorUserId = actorId,
                ActorName = actorName,
                Summary = summary.Length > 400 ? summary[..400] : summary,
                PayloadJson = JsonSerializer.Serialize(payload, JsonOpts),
                ExecutedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
            await _repository.SaveAsync();
        }

        public async Task<SilaPhase4BDashboardDto> GetDashboardAsync(Guid organizationId, Guid? propertyId, bool canViewSalaryDetails, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryReconciliationConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, propertyId, cancellationToken);
            List<SilaCulinaryProductionReconciliation> recons = await _repository.SilaCulinaryProductionReconciliation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.ReconciledAtUtc).Take(50).ToListAsync(cancellationToken);

            List<Guid> reconIds = recons.Select(r => r.Id).ToList();
            List<SilaCulinaryProductionReconciliationLine> reconLines = reconIds.Count == 0
                ? new()
                : await _repository.SilaCulinaryProductionReconciliationLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && reconIds.Contains(x.ReconciliationId) && x.IsActive)
                    .ToListAsync(cancellationToken);

            List<SilaCulinaryProductionConsumption> consumptions = await _repository.SilaCulinaryProductionConsumption
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.ConsumedAtUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryAdditionalMaterialRequest> addlReqs = await _repository.SilaCulinaryAdditionalMaterialRequest
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.DateCreated).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryMaterialReturn> returns = await _repository.SilaCulinaryMaterialReturn
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.ReturnedAtUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryMaterialIssueReversal> reversals = await _repository.SilaCulinaryMaterialIssueReversal
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.ReversedAtUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryWasteEvent> wasteEvents = await _repository.SilaCulinaryWasteEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.RecordedAtUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryFoodLeftover> leftovers = await _repository.SilaCulinaryFoodLeftover
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.ProductionTimestampUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryWasteDisposal> disposals = await _repository.SilaCulinaryWasteDisposal
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.DisposedAtUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryPortionSample> portions = await _repository.SilaCulinaryPortionSample
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.SampledAtUtc).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryEventClosure> closures = await _repository.SilaCulinaryEventClosure
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.DateCreated).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryErpReconciliationRef> erpRefs = await _repository.SilaCulinaryErpReconciliationRef
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.DateCreated).Take(50).ToListAsync(cancellationToken);

            List<SilaCulinaryHistoricalLearningSnapshot> snapshots = await _repository.SilaCulinaryHistoricalLearningSnapshot
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!propertyId.HasValue || x.PropertyId == propertyId))
                .OrderByDescending(x => x.CapturedAtUtc).Take(50).ToListAsync(cancellationToken);

            decimal plannedCost = recons.Sum(r => r.PlannedProductionCost);
            decimal actualCost = recons.Sum(r => r.FullyLoadedProductionCost);

            return new SilaPhase4BDashboardDto
            {
                Currency = cfg.DefaultCurrency,
                PlannedProductionCost = plannedCost,
                ActualProductionCost = actualCost,
                ProductionCostVariance = actualCost - plannedCost,
                TotalWasteCost = wasteEvents.Where(w => !w.IsNormalRecipeYieldLoss).Sum(w => w.EstimatedWasteCost),
                PendingSettlementCount = recons.Count(r => r.FinancialSettlementStatus != "FINANCIALLY_SETTLED" && r.FinancialSettlementStatus != "CLOSED"),
                PendingEventClosureCount = closures.Count(c => c.FinancialStatus != "FINANCIALLY_CLOSED"),
                BalancedReconciliationCount = recons.Count(r => r.ReconciliationStatus == "BALANCED" || r.ReconciliationStatus == "WITHIN_TOLERANCE"),
                BlockedReconciliationCount = recons.Count(r => r.SettlementBlocked),
                HistoricalSnapshotsReady = snapshots.Count(s => s.ValidatedForTraining),
                Reconciliations = recons.Select(r => MapReconciliation(r, reconLines.Where(l => l.ReconciliationId == r.Id).ToList())).ToList(),
                Consumptions = consumptions.Select(MapConsumption).ToList(),
                AdditionalRequests = addlReqs.Select(MapAdditionalRequest).ToList(),
                Returns = returns.Select(MapReturn).ToList(),
                Reversals = reversals.Select(MapReversal).ToList(),
                WasteEvents = wasteEvents.Select(MapWasteEvent).ToList(),
                Leftovers = leftovers.Select(MapLeftover).ToList(),
                Disposals = disposals.Select(MapDisposal).ToList(),
                PortionSamples = portions.Select(MapPortionSample).ToList(),
                EventClosures = closures.Select(MapClosure).ToList(),
                ErpReconciliations = erpRefs.Select(MapErpRef).ToList(),
                HistoricalSnapshots = snapshots.Select(MapSnapshot).ToList(),
                Config = MapConfig(cfg)
            };
        }

        public async Task<SilaCulinaryReconciliationConfigDto> UpsertConfigAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryReconciliationConfigDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryReconciliationConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);
            List<object> history = SafeDeserializeList(cfg.ConfigHistoryJson);
            history.Add(new
            {
                version = cfg.ConfigVersion,
                consumptionMethod = cfg.ConsumptionMethod,
                inventoryValuationMethod = cfg.InventoryValuationMethod,
                tolerancePercent = cfg.MaterialReconciliationTolerancePercent,
                updatedAtUtc = DateTime.UtcNow,
                updatedBy = actorName
            });

            cfg.ConsumptionMethod = request.ConsumptionMethod;
            cfg.BackflushRequiresApproval = request.BackflushRequiresApproval;
            cfg.InventoryValuationMethod = request.InventoryValuationMethod;
            cfg.MaterialReconciliationTolerancePercent = request.MaterialReconciliationTolerancePercent;
            cfg.WasteApprovalThresholdAmount = request.WasteApprovalThresholdAmount;
            cfg.YieldVarianceThresholdPercent = request.YieldVarianceThresholdPercent;
            cfg.PortionVarianceThresholdPercent = request.PortionVarianceThresholdPercent;
            cfg.FoodCostVarianceThresholdPercent = request.FoodCostVarianceThresholdPercent;
            cfg.LaborAllocationMethod = request.LaborAllocationMethod;
            cfg.OverheadAllocationMethod = request.OverheadAllocationMethod;
            cfg.CentralKitchenAllocationMethod = request.CentralKitchenAllocationMethod;
            cfg.AllowProvisionalSettlement = request.AllowProvisionalSettlement;
            cfg.FinancialPeriodLockedBeforeUtc = request.FinancialPeriodLockedBeforeUtc;
            cfg.ClosedPeriodAdjustmentPolicy = request.ClosedPeriodAdjustmentPolicy;
            cfg.ErpPostingPolicy = request.ErpPostingPolicy;
            cfg.InventorySystemOfRecord = request.InventorySystemOfRecord;
            cfg.MaxLeftoverHoldingHours = request.MaxLeftoverHoldingHours;
            cfg.RequireDisposalPhotoAboveThreshold = request.RequireDisposalPhotoAboveThreshold;
            cfg.DefaultCurrency = request.DefaultCurrency;
            cfg.EffectiveFromUtc = request.EffectiveFromUtc == default ? DateTime.UtcNow : request.EffectiveFromUtc;
            cfg.ApprovalMatrixJson = string.IsNullOrWhiteSpace(request.ApprovalMatrixJson) ? "{}" : request.ApprovalMatrixJson;
            cfg.ConfigVersion += 1;
            cfg.ConfigHistoryJson = JsonSerializer.Serialize(history, JsonOpts);
            _repository.SilaCulinaryReconciliationConfig.Update(cfg);
            await _repository.SaveAsync();
            await AuditAsync(buyer.Id, cfg.PropertyId, "CONFIG_UPDATED", userId, actorName, $"Updated Phase 4B config v{cfg.ConfigVersion}", new { cfg.ConfigVersion, cfg.ConsumptionMethod });
            return MapConfig(cfg);
        }

        public async Task<SilaProductionConsumptionDto> RecordConsumptionAsync(Guid organizationId, Guid userId, string actorName, SilaRecordConsumptionRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryProductionConsumption? existing = await _repository.SilaCulinaryProductionConsumption
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive);
                if (existing != null)
                {
                    return MapConsumption(existing);
                }
            }

            if (request.ActualConsumedQty < 0)
            {
                throw new InvalidOperationException("Actual consumed quantity cannot be negative.");
            }

            SilaCulinaryReconciliationConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);
            string method = string.IsNullOrWhiteSpace(request.ConsumptionMethod) ? cfg.ConsumptionMethod : request.ConsumptionMethod.ToUpperInvariant();

            if (method == "BACKFLUSH" && cfg.BackflushRequiresApproval && !request.BackflushApproved)
            {
                throw new InvalidOperationException("Backflush consumption requires explicit approval per tenant policy.");
            }

            // Prevent posting both backflush and explicit consumption for the same batch & material
            if (request.ProductionBatchId.HasValue && (method == "BACKFLUSH" || method == "EXPLICIT"))
            {
                string opposite = method == "BACKFLUSH" ? "EXPLICIT" : "BACKFLUSH";
                bool conflict = await _repository.SilaCulinaryProductionConsumption
                    .FindByCondition(x => x.BuyerId == buyer.Id
                        && x.ProductionBatchId == request.ProductionBatchId
                        && x.MaterialCode == request.MaterialCode
                        && x.InventoryBatchNumber == request.InventoryBatchNumber
                        && x.ConsumptionMethod == opposite
                        && !x.IsReversal
                        && x.IsActive)
                    .AnyAsync(cancellationToken);
                if (conflict)
                {
                    throw new InvalidOperationException($"Cannot post both {opposite} and {method} consumption for batch material {request.MaterialCode}.");
                }
            }

            string number = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.CONSUMPTION, 6, cancellationToken);
            SilaCulinaryProductionConsumption entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                KitchenId = request.KitchenId,
                OutletId = request.OutletId,
                EventId = request.EventId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                RecipeId = request.RecipeId,
                SubRecipeId = request.SubRecipeId,
                MaterialId = request.MaterialId,
                ConsumptionNumber = number,
                MaterialCode = request.MaterialCode,
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                InventoryBatchNumber = string.IsNullOrWhiteSpace(request.InventoryBatchNumber) ? "DEFAULT" : request.InventoryBatchNumber,
                PlannedQty = request.PlannedQty,
                IssuedQty = request.IssuedQty,
                ActualConsumedQty = request.ActualConsumedQty,
                ConsumptionUom = string.IsNullOrWhiteSpace(request.ConsumptionUom) ? "KG" : request.ConsumptionUom,
                ConsumptionMethod = method,
                IncludesWaste = request.IncludesWaste,
                IsSubstitution = request.IsSubstitution,
                SubstitutedFromMaterialCode = request.SubstitutedFromMaterialCode,
                CaptureMode = string.IsNullOrWhiteSpace(request.CaptureMode) ? "MANUAL" : request.CaptureMode,
                DeviationReason = request.DeviationReason,
                SupportingEvidence = request.SupportingEvidence,
                ApprovalStatus = method == "BACKFLUSH" && !request.BackflushApproved ? "PENDING_APPROVAL" : "APPROVED",
                SourceChannel = string.IsNullOrWhiteSpace(request.SourceChannel) ? "CLOUD" : request.SourceChannel,
                IdempotencyKey = request.IdempotencyKey,
                RecordedByUserId = userId,
                RecordedByName = actorName,
                ConsumedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryProductionConsumption.Create(entity);
            await _repository.SaveAsync();
            await AuditAsync(buyer.Id, request.PropertyId, "CONSUMPTION_RECORDED", userId, actorName, $"Recorded {entity.ActualConsumedQty} {entity.ConsumptionUom} of {entity.MaterialCode} ({entity.ConsumptionMethod})", new { entity.ConsumptionNumber, entity.MaterialCode, entity.ActualConsumedQty });
            return MapConsumption(entity);
        }

        public async Task<SilaAdditionalMaterialRequestDto> CreateOrDecideAdditionalRequestAsync(Guid organizationId, Guid userId, string actorName, SilaAdditionalMaterialRequestDto request, bool isDecision, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (isDecision && request.Id.HasValue)
            {
                SilaCulinaryAdditionalMaterialRequest existing = await _repository.SilaCulinaryAdditionalMaterialRequest
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == request.Id.Value && x.IsActive)
                    ?? throw new InvalidOperationException("Additional material request not found.");

                if (existing.InventoryIssued)
                {
                    return MapAdditionalRequest(existing); // Prevents duplicate inventory movement
                }

                decimal approvedQty = request.ApprovalStatus == "REJECTED" ? 0m : Math.Min(request.ApprovedQty > 0 ? request.ApprovedQty : existing.RequestedQty, existing.RequestedQty);
                existing.ApprovedQty = approvedQty;
                existing.ApprovalStatus = approvedQty == 0m ? "REJECTED" : (approvedQty < existing.RequestedQty ? "PARTIALLY_APPROVED" : "APPROVED");
                existing.ApprovedByUserId = userId;
                existing.ApprovedAtUtc = DateTime.UtcNow;

                if (approvedQty > 0m)
                {
                    string issueNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.GOODS_ISSUE, 6, cancellationToken);
                    _repository.SilaCulinaryProductionMaterialIssueRef.Create(new SilaCulinaryProductionMaterialIssueRef
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        ProductionOrderId = existing.ProductionOrderId,
                        ProductionBatchId = existing.ProductionBatchId,
                        IssueNumber = issueNum,
                        MaterialCode = existing.MaterialCode,
                        IssuedQty = approvedQty,
                        IssuedUom = existing.Uom,
                        ToKitchenId = existing.KitchenId,
                        IdempotencyKey = $"AMR-{existing.RequestNumber}",
                        IssuedAtUtc = DateTime.UtcNow,
                        IsActive = true
                    });
                    existing.InventoryIssued = true;
                    existing.InventoryIssueReference = issueNum;
                }

                _repository.SilaCulinaryAdditionalMaterialRequest.Update(existing);
                await _repository.SaveAsync();
                await AuditAsync(buyer.Id, existing.PropertyId, "ADDITIONAL_MATERIAL_DECIDED", userId, actorName, $"Additional request {existing.RequestNumber} -> {existing.ApprovalStatus}", new { existing.RequestNumber, existing.ApprovedQty, existing.InventoryIssueReference });
                return MapAdditionalRequest(existing);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryAdditionalMaterialRequest? dup = await _repository.SilaCulinaryAdditionalMaterialRequest
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive);
                if (dup != null) return MapAdditionalRequest(dup);
            }

            string reqNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.ADDITIONAL_MATERIAL_REQUEST, 6, cancellationToken);
            SilaCulinaryAdditionalMaterialRequest created = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                KitchenId = request.KitchenId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                MaterialId = request.MaterialId,
                RequestNumber = reqNum,
                MaterialCode = request.MaterialCode,
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                RequestedQty = request.RequestedQty,
                ApprovedQty = request.EmergencyAuthorized ? request.RequestedQty : 0m,
                Uom = string.IsNullOrWhiteSpace(request.Uom) ? "KG" : request.Uom,
                AvailableStockSnapshot = request.AvailableStockSnapshot,
                EstimatedCostImpact = request.EstimatedCostImpact,
                ReasonCode = string.IsNullOrWhiteSpace(request.ReasonCode) ? "UNEXPECTED_DEMAND" : request.ReasonCode,
                ReasonNotes = request.ReasonNotes,
                AlternativeMaterialCode = request.AlternativeMaterialCode,
                AlternativeStoreLocationId = request.AlternativeStoreLocationId,
                EmergencyAuthorized = request.EmergencyAuthorized,
                ApprovalStatus = request.EmergencyAuthorized ? "APPROVED" : "PENDING",
                IdempotencyKey = request.IdempotencyKey,
                RequestedByUserId = userId,
                RequestedByName = actorName,
                IsActive = true
            };
            _repository.SilaCulinaryAdditionalMaterialRequest.Create(created);
            await _repository.SaveAsync();
            return MapAdditionalRequest(created);
        }

        public async Task<SilaMaterialReturnDto> RecordOrInspectReturnAsync(Guid organizationId, Guid userId, string actorName, SilaMaterialReturnRequestDto request, Guid? inspectReturnId, bool? inspectionPassed, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (inspectReturnId.HasValue && inspectionPassed.HasValue)
            {
                SilaCulinaryMaterialReturn ret = await _repository.SilaCulinaryMaterialReturn
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == inspectReturnId.Value && x.IsActive)
                    ?? throw new InvalidOperationException("Material return not found.");

                bool isUnsafe = ret.ReturnClassification is "DAMAGED" or "EXPIRED" or "NON_RETURNABLE";
                if (inspectionPassed.Value && !isUnsafe)
                {
                    ret.QualityInspectionStatus = "PASSED";
                    ret.RoutedStockBucket = "AVAILABLE";
                    ret.Status = "POSTED";
                    ret.ReceivedByUserId = userId;
                }
                else
                {
                    ret.QualityInspectionStatus = "FAILED";
                    ret.RoutedStockBucket = "BLOCKED";
                    ret.Status = "REJECTED";
                }
                _repository.SilaCulinaryMaterialReturn.Update(ret);
                await _repository.SaveAsync();
                await AuditAsync(buyer.Id, ret.PropertyId, "MATERIAL_RETURN_INSPECTED", userId, actorName, $"Return {ret.ReturnNumber} QC={ret.QualityInspectionStatus}, Bucket={ret.RoutedStockBucket}", new { ret.ReturnNumber, ret.RoutedStockBucket });
                return MapReturn(ret);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryMaterialReturn? dup = await _repository.SilaCulinaryMaterialReturn
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive);
                if (dup != null) return MapReturn(dup);
            }

            string cls = request.ReturnClassification.ToUpperInvariant();
            bool autoBlocked = cls is "DAMAGED" or "EXPIRED" or "NON_RETURNABLE";
            bool autoAvailable = cls == "UNOPENED_USABLE" && (!request.ExpiryUtc.HasValue || request.ExpiryUtc > DateTime.UtcNow);

            string retNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.MATERIAL_RETURN, 6, cancellationToken);
            SilaCulinaryMaterialReturn entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                OriginalMaterialIssueRefId = request.OriginalMaterialIssueRefId,
                ReturningKitchenId = request.ReturningKitchenId,
                ReceivingStoreLocationId = request.ReceivingStoreLocationId,
                MaterialId = request.MaterialId,
                ReturnNumber = retNum,
                MaterialCode = request.MaterialCode,
                MaterialName = string.IsNullOrWhiteSpace(request.MaterialName) ? request.MaterialCode : request.MaterialName,
                InventoryBatchNumber = string.IsNullOrWhiteSpace(request.InventoryBatchNumber) ? "DEFAULT" : request.InventoryBatchNumber,
                ReturnedQty = request.ReturnedQty,
                Uom = string.IsNullOrWhiteSpace(request.Uom) ? "KG" : request.Uom,
                ReturnClassification = cls,
                IngredientCondition = request.IngredientCondition,
                ExpiryUtc = request.ExpiryUtc,
                ReturnReason = request.ReturnReason,
                QualityInspectionStatus = autoBlocked ? "FAILED" : (autoAvailable ? "PASSED" : "PENDING"),
                RoutedStockBucket = autoBlocked ? "BLOCKED" : (autoAvailable ? "AVAILABLE" : "INSPECTION"),
                Status = autoBlocked ? "REJECTED" : (autoAvailable ? "POSTED" : "PENDING_INSPECTION"),
                IdempotencyKey = request.IdempotencyKey,
                ReturnedByUserId = userId,
                ReturnedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryMaterialReturn.Create(entity);
            await _repository.SaveAsync();
            return MapReturn(entity);
        }

        public async Task<SilaMaterialIssueReversalDto> ReverseMaterialIssueAsync(Guid organizationId, Guid userId, string actorName, SilaMaterialIssueReversalRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryMaterialIssueReversal? dup = await _repository.SilaCulinaryMaterialIssueReversal
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive);
                if (dup != null) return MapReversal(dup);
            }

            if (request.ReversalQty <= 0)
            {
                throw new InvalidOperationException("Reversal quantity must be positive.");
            }

            decimal totalIssued = await _repository.SilaCulinaryProductionMaterialIssueRef
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == request.ProductionOrderId && x.MaterialCode == request.MaterialCode && x.IsActive)
                .SumAsync(x => x.IssuedQty, cancellationToken);

            decimal alreadyReturned = await _repository.SilaCulinaryMaterialReturn
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == request.ProductionOrderId && x.MaterialCode == request.MaterialCode && x.Status != "REJECTED" && x.Status != "REVERSED" && x.IsActive)
                .SumAsync(x => x.ReturnedQty, cancellationToken);

            decimal alreadyReversed = await _repository.SilaCulinaryMaterialIssueReversal
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == request.ProductionOrderId && x.MaterialCode == request.MaterialCode && x.IsActive)
                .SumAsync(x => x.ReversalQty, cancellationToken);

            decimal remainingReversible = totalIssued - alreadyReturned - alreadyReversed;
            if (totalIssued > 0 && request.ReversalQty > remainingReversible)
            {
                throw new InvalidOperationException($"Cannot reverse {request.ReversalQty} {request.Uom}; remaining reversible quantity (after returns and prior reversals) is {Math.Max(0, remainingReversible)}.");
            }

            string revNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.ISSUE_REVERSAL, 6, cancellationToken);
            SilaCulinaryMaterialIssueReversal entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                OriginalMaterialIssueRefId = request.OriginalMaterialIssueRefId,
                ReversalNumber = revNum,
                OriginalIssueNumber = request.OriginalIssueNumber,
                MaterialCode = request.MaterialCode,
                InventoryBatchNumber = request.InventoryBatchNumber,
                ReversalType = request.ReversalType,
                ReversalQty = request.ReversalQty,
                Uom = request.Uom,
                Reason = request.Reason,
                ApprovalStatus = "APPROVED",
                ErpReference = request.ErpReference,
                Status = "POSTED",
                IdempotencyKey = request.IdempotencyKey,
                ReversedByUserId = userId,
                ReversedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryMaterialIssueReversal.Create(entity);
            await _repository.SaveAsync();
            await AuditAsync(buyer.Id, request.PropertyId, "ISSUE_REVERSAL_POSTED", userId, actorName, $"Reversed {entity.ReversalQty} {entity.Uom} of {entity.MaterialCode} ({entity.ReversalNumber})", new { entity.ReversalNumber, entity.OriginalIssueNumber, entity.ReversalQty });
            return MapReversal(entity);
        }

        public async Task<SilaWasteEventDto> RecordWasteEventAsync(Guid organizationId, Guid userId, string actorName, SilaRecordWasteEventRequestDto request, bool? approveDecision, Guid? wasteId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (wasteId.HasValue && approveDecision.HasValue)
            {
                SilaCulinaryWasteEvent w = await _repository.SilaCulinaryWasteEvent
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == wasteId.Value && x.IsActive)
                    ?? throw new InvalidOperationException("Waste record not found.");
                w.ApprovalStatus = approveDecision.Value ? "APPROVED" : "REJECTED";
                _repository.SilaCulinaryWasteEvent.Update(w);
                await _repository.SaveAsync();
                return MapWasteEvent(w);
            }

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryWasteEvent? dup = await _repository.SilaCulinaryWasteEvent
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive);
                if (dup != null) return MapWasteEvent(dup);
            }

            SilaCulinaryReconciliationConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);
            bool needsApproval = !request.IsNormalRecipeYieldLoss && request.EstimatedWasteCost >= cfg.WasteApprovalThresholdAmount;
            string wasteCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.PRODUCTION_WASTE, 6, cancellationToken);

            SilaCulinaryWasteEvent entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                KitchenId = request.KitchenId,
                OutletId = request.OutletId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                EventId = request.EventId,
                MaterialId = request.MaterialId,
                RecipeId = request.RecipeId,
                WasteCode = wasteCode,
                ItemDomain = string.IsNullOrWhiteSpace(request.ItemDomain) ? "RAW_INGREDIENT" : request.ItemDomain,
                IsNormalRecipeYieldLoss = request.IsNormalRecipeYieldLoss,
                AlreadyDeductedFromInventory = request.AlreadyDeductedFromInventory,
                SecondDeductionPrevented = true, // Never perform a second stock deduction if already deducted
                ItemCode = request.ItemCode,
                ItemName = string.IsNullOrWhiteSpace(request.ItemName) ? request.ItemCode : request.ItemName,
                InventoryBatchNumber = request.InventoryBatchNumber,
                WasteCategory = request.WasteCategory,
                WasteQty = request.WasteQty,
                Uom = request.Uom,
                EstimatedWasteCost = request.EstimatedWasteCost,
                WasteReason = request.WasteReason,
                DisposalMethod = request.DisposalMethod,
                PhotoEvidenceRef = request.PhotoEvidenceRef,
                ApprovalStatus = needsApproval ? "PENDING" : "APPROVED",
                SourceChannel = request.SourceChannel,
                IdempotencyKey = request.IdempotencyKey,
                RecordedByUserId = userId,
                RecordedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryWasteEvent.Create(entity);
            await _repository.SaveAsync();
            return MapWasteEvent(entity);
        }

        public async Task<SilaFoodLeftoverDto> RecordOrDisposeLeftoverAsync(Guid organizationId, Guid userId, string actorName, SilaFoodLeftoverRequestDto request, Guid? leftoverId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            // Food safety guardrail: unsafe food must never be recommended or approved for reuse/redistribution
            if (request.QualityStatus == "UNSAFE_HOLD" && request.Disposition is "SAFE_STORAGE" or "INTERNAL_TRANSFER" or "REDISTRIBUTION")
            {
                throw new InvalidOperationException("Food safety guardrail: unsafe food cannot be approved for storage, transfer, or redistribution.");
            }

            if (leftoverId.HasValue)
            {
                SilaCulinaryFoodLeftover existing = await _repository.SilaCulinaryFoodLeftover
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == leftoverId.Value && x.IsActive)
                    ?? throw new InvalidOperationException("Leftover record not found.");
                if (existing.QualityStatus == "UNSAFE_HOLD" && request.Disposition is "SAFE_STORAGE" or "INTERNAL_TRANSFER" or "REDISTRIBUTION")
                {
                    throw new InvalidOperationException("Food safety guardrail: unsafe food cannot be routed to reuse.");
                }
                List<object> hist = SafeDeserializeList(existing.DispositionHistoryJson);
                hist.Add(new { from = existing.Disposition, to = request.Disposition, actor = actorName, atUtc = DateTime.UtcNow });
                existing.Disposition = request.Disposition;
                existing.QualityStatus = request.QualityStatus;
                existing.FoodSafetyApproved = request.FoodSafetyApproved && existing.QualityStatus == "SAFE_VERIFIED";
                existing.DispositionHistoryJson = JsonSerializer.Serialize(hist, JsonOpts);
                _repository.SilaCulinaryFoodLeftover.Update(existing);
                await _repository.SaveAsync();
                return MapLeftover(existing);
            }

            string num = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.FOOD_LEFTOVER, 6, cancellationToken);
            DateTime prodTs = request.ProductionTimestampUtc ?? DateTime.UtcNow;
            DateTime useBy = request.ApprovedUseByUtc ?? prodTs.AddHours(24);
            SilaCulinaryFoodLeftover entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                ProductionBatchId = request.ProductionBatchId,
                EventId = request.EventId,
                RecipeId = request.RecipeId,
                LeftoverNumber = num,
                SourceType = request.SourceType,
                FinishedProductName = request.FinishedProductName,
                OriginalProductionQty = request.OriginalProductionQty,
                ServedQty = request.ServedQty,
                RemainingQty = request.RemainingQty,
                Uom = request.Uom,
                HoldingConditions = request.HoldingConditions,
                ProductionTimestampUtc = prodTs,
                ApprovedUseByUtc = useBy,
                StorageConditions = request.StorageConditions,
                AllergenInfo = request.AllergenInfo,
                QualityStatus = request.QualityStatus,
                FoodSafetyApproved = request.FoodSafetyApproved && request.QualityStatus == "SAFE_VERIFIED",
                Disposition = request.Disposition,
                DispositionHistoryJson = JsonSerializer.Serialize(new[] { new { action = "CREATED", disposition = request.Disposition, actor = actorName, atUtc = DateTime.UtcNow } }, JsonOpts),
                IsActive = true
            };
            _repository.SilaCulinaryFoodLeftover.Create(entity);
            await _repository.SaveAsync();
            return MapLeftover(entity);
        }

        public async Task<SilaWasteDisposalDto> ConfirmDisposalAsync(Guid organizationId, Guid userId, string actorName, SilaWasteDisposalRequestDto request, bool approveSupervisor, Guid? disposalId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (disposalId.HasValue && approveSupervisor)
            {
                SilaCulinaryWasteDisposal existing = await _repository.SilaCulinaryWasteDisposal
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == disposalId.Value && x.IsActive)
                    ?? throw new InvalidOperationException("Disposal record not found.");
                existing.SupervisorApprovalStatus = "APPROVED";
                _repository.SilaCulinaryWasteDisposal.Update(existing);
                await _repository.SaveAsync();
                return MapDisposal(existing);
            }

            string num = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.WASTE_DISPOSAL, 6, cancellationToken);
            SilaCulinaryWasteDisposal entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                WasteEventId = request.WasteEventId,
                ProductionBatchId = request.ProductionBatchId,
                DisposalNumber = num,
                ItemName = request.ItemName,
                DisposalQty = request.DisposalQty,
                Uom = request.Uom,
                DisposalReason = request.DisposalReason,
                DisposalMethod = request.DisposalMethod,
                DisposalLocation = request.DisposalLocation,
                DisposedAtUtc = DateTime.UtcNow,
                ResponsibleEmployee = string.IsNullOrWhiteSpace(request.ResponsibleEmployee) ? actorName : request.ResponsibleEmployee,
                SupervisorApprovalStatus = "PENDING",
                SupportingEvidenceRef = request.SupportingEvidenceRef,
                FinancialWasteCost = request.FinancialWasteCost,
                IsActive = true
            };
            _repository.SilaCulinaryWasteDisposal.Create(entity);
            await _repository.SaveAsync();
            return MapDisposal(entity);
        }

        public async Task<SilaCulinaryProductionOutputConfirmation> ConfirmProductionOutputAsync(Guid organizationId, Guid userId, string actorName, SilaOutputConfirmationRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryProductionOutputConfirmation? dup = await _repository.SilaCulinaryProductionOutputConfirmation
                    .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive);
                if (dup != null) return dup;
            }

            string num = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.OUTPUT_CONFIRMATION, 6, cancellationToken);
            SilaCulinaryProductionOutputConfirmation entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                ParentBatchId = request.ParentBatchId,
                ConfirmationNumber = num,
                OutputType = request.OutputType,
                PlannedOutputQty = request.PlannedOutputQty,
                ActualGrossOutputQty = request.ActualGrossOutputQty,
                AcceptedUsableOutputQty = request.AcceptedUsableOutputQty,
                RejectedOutputQty = request.RejectedOutputQty,
                ReworkQty = request.ReworkQty,
                FinishedProductWasteQty = request.FinishedProductWasteQty,
                OutputUom = request.OutputUom,
                ActualPortionsProduced = request.ActualPortionsProduced,
                AveragePortionWeightGrams = request.AveragePortionWeightGrams,
                QualityApprovalStatus = request.QualityApprovalStatus,
                ResponsibleChef = string.IsNullOrWhiteSpace(request.ResponsibleChef) ? actorName : request.ResponsibleChef,
                FinanciallySettled = false, // Separate physical output confirmation from financial settlement
                IdempotencyKey = request.IdempotencyKey,
                CompletedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryProductionOutputConfirmation.Create(entity);
            await _repository.SaveAsync();
            return entity;
        }

        public async Task<SilaPortionSampleDto> RecordPortionSampleAsync(Guid organizationId, Guid userId, string actorName, SilaPortionSampleRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            List<decimal> samples = request.SampledWeightsGrams.Where(w => w > 0).ToList();
            decimal stdW = Math.Max(request.StandardPortionWeightGrams, 0.01m);
            decimal avgW = samples.Count > 0 ? Math.Round(samples.Average(), 2) : stdW;
            decimal varPct = Math.Round((avgW - stdW) / stdW * 100m, 2);
            decimal costVar = Math.Round(((avgW - stdW) / stdW) * request.StandardPortionCost * Math.Max(request.ActualPortions, request.PlannedPortions), 2);
            string basis = samples.Count > 0
                ? $"Computed from {samples.Count} sampled portion(s); sample reflects observed batch tendency rather than every unweighed portion."
                : "No physical sample weights provided; defaulted to standard weight.";

            SilaCulinaryPortionSample entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                RecipeId = request.RecipeId,
                RecipeCode = request.RecipeCode,
                StandardPortionWeightGrams = stdW,
                SampledWeightsJson = JsonSerializer.Serialize(samples, JsonOpts),
                SampleCount = samples.Count,
                AverageActualWeightGrams = avgW,
                PortionWeightVariancePercent = varPct,
                PlannedPortions = request.PlannedPortions,
                ActualPortions = request.ActualPortions,
                TotalUsableOutputGrams = request.TotalUsableOutputGrams,
                EstimatedPortionCostVariance = costVar,
                SamplingBasisNote = basis,
                SampledAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryPortionSample.Create(entity);
            await _repository.SaveAsync();
            return MapPortionSample(entity);
        }

        public async Task<SilaProductionReconciliationDto> ReconcileProductionOrderAsync(Guid organizationId, Guid userId, string actorName, Guid productionOrderId, Guid? productionBatchId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryProductionOrder order = await _repository.SilaCulinaryProductionOrder
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == productionOrderId && x.IsActive)
                ?? throw new InvalidOperationException("Production order not found.");

            SilaCulinaryReconciliationConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, order.PropertyId, cancellationToken);
            List<SilaCulinaryProductionOrderIngredient> ings = await _repository.SilaCulinaryProductionOrderIngredient
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProductionMaterialIssueRef> issueRefs = await _repository.SilaCulinaryProductionMaterialIssueRef
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProductionConsumption> consumptions = await _repository.SilaCulinaryProductionConsumption
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && !x.IsReversal && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryMaterialReturn> returns = await _repository.SilaCulinaryMaterialReturn
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryMaterialIssueReversal> reversals = await _repository.SilaCulinaryMaterialIssueReversal
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryWasteEvent> rawWaste = await _repository.SilaCulinaryWasteEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.ItemDomain == "RAW_INGREDIENT" && x.IsActive)
                .ToListAsync(cancellationToken);

            List<Guid> orderBatchIds = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProductionQualityResult> qcResults = orderBatchIds.Count == 0
                ? new List<SilaCulinaryProductionQualityResult>()
                : await _repository.SilaCulinaryProductionQualityResult
                    .FindByCondition(x => x.BuyerId == buyer.Id && orderBatchIds.Contains(x.ProductionBatchId) && x.IsActive)
                    .ToListAsync(cancellationToken);

            List<SilaCulinaryProductionOutputConfirmation> outputs = await _repository.SilaCulinaryProductionOutputConfirmation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryLaborAllocation> labor = await _repository.SilaCulinaryLaborAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == productionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            bool qcFailed = qcResults.Any(q => !q.Passed && !q.SupervisorApproved);
            decimal acceptedRatio = order.PlannedOutputQty > 0
                ? (outputs.Count > 0 ? outputs.Sum(o => o.AcceptedUsableOutputQty) / order.PlannedOutputQty : 1m)
                : 1m;

            List<SilaCulinaryProductionReconciliationLine> lines = new();
            bool anyBlocked = qcFailed;
            decimal actualMatCost = 0m;
            decimal totalUsageVarCost = 0m;
            decimal totalPriceVarCost = 0m;

            IEnumerable<string> materialCodes = ings.Select(i => i.MaterialCode)
                .Concat(issueRefs.Select(i => i.MaterialCode))
                .Concat(consumptions.Select(c => c.MaterialCode))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (string code in materialCodes)
            {
                SilaCulinaryProductionOrderIngredient? ing = ings.FirstOrDefault(i => i.MaterialCode == code);
                decimal plannedQty = ing?.GrossRequiredQty ?? 0m;
                decimal origIssued = issueRefs.Where(i => i.MaterialCode == code && (i.IdempotencyKey == null || !i.IdempotencyKey.StartsWith("AMR-"))).Sum(i => i.IssuedQty);
                decimal addlIssued = issueRefs.Where(i => i.MaterialCode == code && i.IdempotencyKey != null && i.IdempotencyKey.StartsWith("AMR-")).Sum(i => i.IssuedQty);
                decimal revQty = reversals.Where(r => r.MaterialCode == code).Sum(r => r.ReversalQty);
                decimal netIssued = origIssued + addlIssued - revQty;

                List<SilaCulinaryProductionConsumption> matCons = consumptions.Where(c => c.MaterialCode == code).ToList();
                decimal usableCons = matCons.Sum(c => c.ActualConsumedQty);
                bool consIncludesWaste = matCons.Any(c => c.IncludesWaste);
                decimal sepWaste = consIncludesWaste ? 0m : rawWaste.Where(w => w.ItemCode == code && !w.IsNormalRecipeYieldLoss).Sum(w => w.WasteQty);
                decimal retQty = returns.Where(r => r.MaterialCode == code && r.Status != "REJECTED" && r.Status != "REVERSED").Sum(r => r.ReturnedQty);

                decimal accounted = usableCons + sepWaste + retQty;
                decimal unexplained = netIssued - accounted;
                decimal tol = cfg.MaterialReconciliationTolerancePercent;
                decimal denom = Math.Max(Math.Abs(netIssued), 0.0001m);
                decimal unexpPct = netIssued == 0m ? (unexplained == 0m ? 0m : 100m) : Math.Abs(unexplained) / denom * 100m;
                bool pendingReturn = returns.Any(r => r.MaterialCode == code && r.Status == "PENDING_INSPECTION");
                bool pendingWaste = rawWaste.Any(w => w.ItemCode == code && w.ApprovalStatus == "PENDING");

                string lineStatus = pendingWaste ? "PENDING_WASTE_APPROVAL"
                    : pendingReturn ? "PENDING_RETURN"
                    : unexplained == 0m ? "BALANCED"
                    : unexpPct <= tol ? "WITHIN_TOLERANCE"
                    : "QUANTITY_MISMATCH";

                bool lineBlocked = lineStatus is "QUANTITY_MISMATCH" or "PENDING_WASTE_APPROVAL" or "PENDING_RETURN";
                if (lineBlocked) anyBlocked = true;

                decimal stdForActual = Math.Round(plannedQty * acceptedRatio, 4);
                decimal planVolVar = stdForActual - plannedQty;
                decimal actualConsForVar = usableCons + sepWaste;
                decimal usageVar = actualConsForVar - stdForActual;
                decimal unitPrice = 10m; // fallback if no valuation record
                decimal usageCostVar = Math.Round(usageVar * unitPrice, 2);
                totalUsageVarCost += usageCostVar;
                actualMatCost += Math.Round(actualConsForVar * unitPrice, 2);

                lines.Add(new SilaCulinaryProductionReconciliationLine
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MaterialCode = code,
                    MaterialName = ing?.MaterialName ?? code,
                    InventoryBatchNumber = "DEFAULT",
                    Uom = ing?.InventoryUom ?? "KG",
                    PlannedQty = plannedQty,
                    ReservedQty = plannedQty,
                    PickedQty = origIssued,
                    OriginalIssuedQty = origIssued,
                    AdditionalIssuedQty = addlIssued,
                    ReversedIssueQty = revQty,
                    NetIssuedQty = netIssued,
                    UsableConsumptionQty = usableCons,
                    SeparateWasteQty = sepWaste,
                    ReturnedQty = retQty,
                    AccountedQty = accounted,
                    UnexplainedQty = unexplained,
                    TolerancePercent = tol,
                    LineStatus = lineStatus,
                    SettlementBlocked = lineBlocked,
                    PlanningVolumeVarianceQty = planVolVar,
                    UsageVarianceQty = usageVar,
                    UsageCostVariance = usageCostVar,
                    PriceCostVariance = 0m,
                    IsActive = true
                });
            }

            string recNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.RECONCILIATION, 6, cancellationToken);
            decimal laborCost = labor.Sum(l => l.AllocatedLaborCost);
            decimal overheadCost = Math.Round(actualMatCost * 0.10m, 2);
            decimal fullyLoaded = actualMatCost + laborCost + overheadCost;

            var checklist = new List<object>
            {
                new { code = "QUALITY_CHECKPOINTS", severity = "BLOCKING", passed = !qcFailed, message = qcFailed ? "Mandatory quality checkpoint failed or on hold." : "All quality checkpoints passed." },
                new { code = "MATERIAL_BALANCE", severity = "BLOCKING", passed = !lines.Any(l => l.SettlementBlocked), message = lines.Any(l => l.SettlementBlocked) ? "Unexplained ingredient variance exceeds tolerance or pending return/waste." : "All ingredients reconciled within tolerance." },
                new { code = "OUTPUT_CONFIRMED", severity = "WARNING", passed = outputs.Count > 0, message = outputs.Count > 0 ? "Actual production output confirmed." : "No formal output confirmation recorded yet." }
            };

            var bridge = new
            {
                plannedCost = order.EstimatedProductionCost,
                actualCost = fullyLoaded,
                totalVariance = fullyLoaded - order.EstimatedProductionCost,
                usageCostVariance = totalUsageVarCost,
                priceCostVariance = totalPriceVarCost,
                laborCost,
                overheadCost
            };

            SilaCulinaryProductionReconciliation recon = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = order.PropertyId,
                KitchenId = order.KitchenId,
                OutletId = order.OutletId,
                EventId = order.EventId,
                ProductionOrderId = order.Id,
                ProductionBatchId = productionBatchId,
                ReconciliationNumber = recNum,
                ReconciliationStatus = anyBlocked ? "REQUIRES_INVESTIGATION" : (lines.Any(l => l.LineStatus == "WITHIN_TOLERANCE") ? "WITHIN_TOLERANCE" : "BALANCED"),
                OperationalSettlementStatus = anyBlocked ? "NOT_READY" : "READY_FOR_SETTLEMENT",
                FinancialSettlementStatus = "PENDING_APPROVAL",
                SettlementBlocked = anyBlocked,
                PlannedProductionCost = order.EstimatedProductionCost,
                ActualDirectMaterialCost = actualMatCost,
                ActualDirectLaborCost = laborCost,
                AllocatedFixedOverhead = overheadCost,
                FullyLoadedProductionCost = fullyLoaded,
                CostStatus = "PROVISIONAL",
                CostMethodology = cfg.InventoryValuationMethod,
                CostVarianceBridgeJson = JsonSerializer.Serialize(bridge, JsonOpts),
                ValidationChecklistJson = JsonSerializer.Serialize(checklist, JsonOpts),
                FinishedProductBalanceJson = JsonSerializer.Serialize(new
                {
                    plannedOutput = order.PlannedOutputQty,
                    acceptedOutput = outputs.Sum(o => o.AcceptedUsableOutputQty),
                    rejectedOutput = outputs.Sum(o => o.RejectedOutputQty),
                    finishedWaste = outputs.Sum(o => o.FinishedProductWasteQty)
                }, JsonOpts),
                ReconciledAtUtc = DateTime.UtcNow,
                IsActive = true
            };

            foreach (var l in lines)
            {
                l.ReconciliationId = recon.Id;
                _repository.SilaCulinaryProductionReconciliationLine.Create(l);
            }
            _repository.SilaCulinaryProductionReconciliation.Create(recon);
            await _repository.SaveAsync();
            return MapReconciliation(recon, lines);
        }

        public async Task<SilaProductionReconciliationDto> SettleProductionAsync(Guid organizationId, Guid userId, string actorName, Guid reconciliationId, bool financialSettlement, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryProductionReconciliation recon = await _repository.SilaCulinaryProductionReconciliation
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == reconciliationId && x.IsActive)
                ?? throw new InvalidOperationException("Reconciliation record not found.");

            if (recon.SettlementBlocked)
            {
                throw new InvalidOperationException("Production settlement is blocked by mandatory validation failures (quality hold, unexplained variance above tolerance, or pending approvals).");
            }

            recon.OperationalSettlementStatus = "PROVISIONALLY_SETTLED";
            if (financialSettlement)
            {
                recon.FinancialSettlementStatus = "FINANCIALLY_SETTLED";
                recon.CostStatus = "FINAL";
                recon.SettledAtUtc = DateTime.UtcNow;
            }
            _repository.SilaCulinaryProductionReconciliation.Update(recon);
            await _repository.SaveAsync();
            await AuditAsync(buyer.Id, recon.PropertyId, "PRODUCTION_SETTLED", userId, actorName, $"Settled {recon.ReconciliationNumber} (Financial={financialSettlement})", new { recon.ReconciliationNumber, recon.FinancialSettlementStatus });

            List<SilaCulinaryProductionReconciliationLine> lines = await _repository.SilaCulinaryProductionReconciliationLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ReconciliationId == recon.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            return MapReconciliation(recon, lines);
        }

        public async Task<SilaCulinaryLaborAllocation> RecordLaborAllocationAsync(Guid organizationId, Guid userId, string actorName, SilaLaborAllocationRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            decimal total = Math.Round((request.ActualHours * request.ApplicableHourlyRate) + request.OvertimeCost, 2);
            SilaCulinaryLaborAllocation entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                ProductionTaskId = request.ProductionTaskId,
                EmployeeUserId = request.EmployeeUserId,
                EmployeeReference = request.EmployeeReference,
                StaffClassification = request.StaffClassification,
                ActualHours = request.ActualHours,
                ApplicableHourlyRate = request.ApplicableHourlyRate,
                OvertimeCost = request.OvertimeCost,
                AllocatedLaborCost = total,
                AllocationMethod = request.AllocationMethod,
                IsEstimated = request.IsEstimated,
                IsActive = true
            };
            _repository.SilaCulinaryLaborAllocation.Create(entity);
            await _repository.SaveAsync();
            return entity;
        }

        public async Task<List<SilaCulinaryCentralKitchenAllocation>> AllocateCentralKitchenCostAsync(Guid organizationId, Guid userId, string actorName, SilaCentralKitchenCostAllocationRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            decimal distributedSum = request.OutletAllocations.Sum(o => o.DistributedQty);
            decimal totalBatchQty = distributedSum + request.RemainingApprovedStockQty;
            if (totalBatchQty <= 0m)
            {
                throw new InvalidOperationException("Central kitchen total distributed plus remaining stock quantity must be positive.");
            }

            List<SilaCulinaryCentralKitchenAllocation> created = new();
            decimal allocatedRunning = 0m;
            for (int i = 0; i < request.OutletAllocations.Count; i++)
            {
                SilaCkOutletSliceDto slice = request.OutletAllocations[i];
                decimal cost = Math.Round(request.TotalBatchProductionCost * (slice.DistributedQty / totalBatchQty), 2);
                allocatedRunning += cost;
                SilaCulinaryCentralKitchenAllocation entity = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    SourceKitchenId = request.SourceKitchenId,
                    ProductionBatchId = request.ProductionBatchId,
                    ReceivingOutletId = slice.ReceivingOutletId,
                    ReceivingOutletName = slice.ReceivingOutletName,
                    DistributedQty = slice.DistributedQty,
                    RemainingApprovedStockQty = request.RemainingApprovedStockQty,
                    Uom = request.Uom,
                    TotalBatchProductionCost = request.TotalBatchProductionCost,
                    AllocatedProductionCost = cost,
                    InternalTransferValuation = cost,
                    TreatedAsExternalRevenue = false,
                    ReceivingConfirmed = slice.ReceivingConfirmed,
                    AllocationMethod = request.AllocationMethod,
                    IsActive = true
                };
                _repository.SilaCulinaryCentralKitchenAllocation.Create(entity);
                created.Add(entity);
            }

            await _repository.SaveAsync();
            return created;
        }

        public async Task<SilaCulinaryBuffetReconciliation> ReconcileBuffetAsync(Guid organizationId, Guid userId, string actorName, SilaBuffetReconciliationRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            decimal totalProd = request.InitialProductionQty + request.ReplenishmentQty;
            // Do not assume dispatched food was consumed: estimated served = dispatched - returned - leftover - waste
            decimal estimatedServed = Math.Max(0m, request.TotalDispatchedQty - request.ReturnedQty - request.LeftoverQty - request.WasteQty);
            SilaCulinaryBuffetReconciliation entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                OutletId = request.OutletId,
                KitchenId = request.KitchenId,
                EventId = request.EventId,
                MealPeriod = request.MealPeriod,
                ServiceDateUtc = DateTime.UtcNow,
                ExpectedCovers = request.ExpectedCovers,
                ActualCovers = request.ActualCovers,
                PlannedBuffetQty = request.PlannedBuffetQty,
                InitialProductionQty = request.InitialProductionQty,
                ReplenishmentQty = request.ReplenishmentQty,
                TotalProductionQty = totalProd,
                TotalDispatchedQty = request.TotalDispatchedQty,
                EstimatedServedQty = estimatedServed,
                ReturnedQty = request.ReturnedQty,
                LeftoverQty = request.LeftoverQty,
                WasteQty = request.WasteQty,
                ActualIngredientConsumptionQty = request.ActualIngredientConsumptionQty,
                PlannedProductionCost = request.PlannedProductionCost,
                ActualProductionCost = request.ActualProductionCost,
                WasteCost = request.WasteCost,
                IsActive = true
            };
            _repository.SilaCulinaryBuffetReconciliation.Create(entity);
            await _repository.SaveAsync();
            return entity;
        }

        public async Task<SilaCulinaryEventClosureSummaryDto> UpsertEventClosureAsync(Guid organizationId, Guid userId, string actorName, SilaEventClosureRequestDto request, bool closeOperational, bool closeFinancial, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryEventClosure? closure = await _repository.SilaCulinaryEventClosure
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.EventId == request.EventId && x.IsActive);

            decimal variableCost = request.DirectMaterialCostActual + request.DirectLaborCostActual + request.ExternalExpenseActual;
            decimal contribution = request.NetRevenueActual - variableCost;
            decimal fullyLoadedProfit = contribution - request.AllocatedOverheadActual;

            string opStatus = closeOperational && request.AllServiceCompleted && request.AllMaterialsReconciled
                ? "OPERATIONALLY_CLOSED"
                : (request.AllServiceCompleted ? "OPERATIONAL_RECONCILIATION" : "SERVICE_COMPLETED");

            bool hasPending = request.PendingAccruedCost > 0m || !request.AllInvoicesConfirmed;
            string finStatus = closeFinancial
                ? (hasPending ? "PROVISIONALLY_CLOSED" : "FINANCIALLY_CLOSED")
                : "FINANCIAL_RECONCILIATION";

            var fourPerspective = new
            {
                originalBudget = new { cost = request.OriginalBudgetCost, profit = request.NetRevenueActual - request.OriginalBudgetCost },
                revisedBudget = new { cost = request.RevisedBudgetCost, profit = request.NetRevenueActual - request.RevisedBudgetCost },
                latestForecast = new { cost = request.ForecastCost, profit = request.NetRevenueActual - request.ForecastCost },
                actualResult = new { netRevenue = request.NetRevenueActual, variableCost, contribution, overhead = request.AllocatedOverheadActual, fullyLoadedProfit, isProvisional = hasPending }
            };

            var pendingList = hasPending
                ? new[] { new { description = request.PendingCostDescription ?? "Pending supplier invoice / accrual", amount = request.PendingAccruedCost, status = "ACCRUED_PROVISIONAL" } }
                : Array.Empty<object>();

            if (closure == null)
            {
                string num = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.EVENT_CLOSURE, 6, cancellationToken);
                closure = new SilaCulinaryEventClosure
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = request.PropertyId,
                    EventId = request.EventId,
                    ClosureNumber = num,
                    EventName = request.EventName,
                    ExpectedGuestCount = request.ExpectedGuestCount,
                    ConfirmedGuestCount = request.ConfirmedGuestCount,
                    ActualGuestCount = request.ActualGuestCount,
                    Currency = request.Currency,
                    OperationalStatus = opStatus,
                    FinancialStatus = finStatus,
                    SettlementVersion = 1,
                    NetRevenueActual = request.NetRevenueActual,
                    DirectMaterialCostActual = request.DirectMaterialCostActual,
                    DirectLaborCostActual = request.DirectLaborCostActual,
                    ExternalExpenseActual = request.ExternalExpenseActual,
                    TotalVariableCostActual = variableCost,
                    ContributionMarginActual = contribution,
                    AllocatedOverheadActual = request.AllocatedOverheadActual,
                    FullyLoadedProfitActual = fullyLoadedProfit,
                    PendingAccruedCost = request.PendingAccruedCost,
                    AuthoritativeRevenueSource = request.AuthoritativeRevenueSource,
                    FourPerspectivePnlJson = JsonSerializer.Serialize(fourPerspective, JsonOpts),
                    PendingCostsJson = JsonSerializer.Serialize(pendingList, JsonOpts),
                    ClosureHistoryJson = JsonSerializer.Serialize(new[] { new { version = 1, opStatus, finStatus, actor = actorName, atUtc = DateTime.UtcNow } }, JsonOpts),
                    IsActive = true
                };
                _repository.SilaCulinaryEventClosure.Create(closure);
            }
            else
            {
                List<object> hist = SafeDeserializeList(closure.ClosureHistoryJson);
                hist.Add(new { version = closure.SettlementVersion, opStatus, finStatus, actor = actorName, atUtc = DateTime.UtcNow });
                closure.ActualGuestCount = request.ActualGuestCount;
                closure.OperationalStatus = opStatus;
                closure.FinancialStatus = finStatus;
                closure.NetRevenueActual = request.NetRevenueActual;
                closure.DirectMaterialCostActual = request.DirectMaterialCostActual;
                closure.DirectLaborCostActual = request.DirectLaborCostActual;
                closure.ExternalExpenseActual = request.ExternalExpenseActual;
                closure.TotalVariableCostActual = variableCost;
                closure.ContributionMarginActual = contribution;
                closure.AllocatedOverheadActual = request.AllocatedOverheadActual;
                closure.FullyLoadedProfitActual = fullyLoadedProfit;
                closure.PendingAccruedCost = request.PendingAccruedCost;
                closure.FourPerspectivePnlJson = JsonSerializer.Serialize(fourPerspective, JsonOpts);
                closure.PendingCostsJson = JsonSerializer.Serialize(pendingList, JsonOpts);
                closure.ClosureHistoryJson = JsonSerializer.Serialize(hist, JsonOpts);
                _repository.SilaCulinaryEventClosure.Update(closure);
            }

            await _repository.SaveAsync();
            return MapClosure(closure);
        }

        public async Task<SilaCulinaryEventClosureSummaryDto> ReopenEventClosureAsync(Guid organizationId, Guid userId, string actorName, Guid closureId, SilaReopenClosureRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryEventClosure closure = await _repository.SilaCulinaryEventClosure
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == closureId && x.IsActive)
                ?? throw new InvalidOperationException("Event closure not found.");

            // Retain previous financial snapshot before incrementing settlement version
            List<object> adjustments = SafeDeserializeList(closure.ReopeningAdjustmentsJson);
            adjustments.Add(new
            {
                previousVersion = closure.SettlementVersion,
                previousProfit = closure.FullyLoadedProfitActual,
                reasonCode = request.ReasonCode,
                notes = request.Notes,
                adjustmentAmount = request.AdjustmentAmount,
                supportingEvidence = request.SupportingEvidence,
                approvedBy = actorName,
                reopenedAtUtc = DateTime.UtcNow
            });

            closure.SettlementVersion += 1;
            closure.FinancialStatus = "REOPENED";
            closure.ExternalExpenseActual += request.AdjustmentAmount;
            closure.TotalVariableCostActual += request.AdjustmentAmount;
            closure.ContributionMarginActual = closure.NetRevenueActual - closure.TotalVariableCostActual;
            closure.FullyLoadedProfitActual = closure.ContributionMarginActual - closure.AllocatedOverheadActual;
            closure.ReopeningAdjustmentsJson = JsonSerializer.Serialize(adjustments, JsonOpts);

            _repository.SilaCulinaryEventClosure.Update(closure);
            await _repository.SaveAsync();
            await AuditAsync(buyer.Id, closure.PropertyId, "EVENT_CLOSURE_REOPENED", userId, actorName, $"Reopened {closure.ClosureNumber} -> v{closure.SettlementVersion} ({request.ReasonCode})", new { closure.ClosureNumber, request.AdjustmentAmount });
            return MapClosure(closure);
        }

        public async Task<SilaErpReconciliationRefDto> UpsertErpReconciliationAsync(Guid organizationId, Guid userId, string actorName, string silaTxRef, string txType, string erpSystem, string? erpDocRef, string postingStatus, string? errorMsg, string idempotencyKey, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryErpReconciliationRef? existing = await _repository.SilaCulinaryErpReconciliationRef
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == idempotencyKey && x.IsActive);

            if (existing != null)
            {
                // Idempotent retry: update posting status & increment retry count without creating duplicate posting
                existing.RetryCount += 1;
                existing.PostingStatus = postingStatus;
                existing.ErpDocumentRef = erpDocRef ?? existing.ErpDocumentRef;
                existing.ErrorMessage = errorMsg;
                existing.PostedAtUtc = postingStatus == "CONFIRMED" ? DateTime.UtcNow : existing.PostedAtUtc;
                existing.ReconciliationStatus = postingStatus == "CONFIRMED" ? "MATCHED" : (postingStatus == "FAILED_RETRY" ? "FAILED" : "PENDING_CONFIRMATION");
                _repository.SilaCulinaryErpReconciliationRef.Update(existing);
                await _repository.SaveAsync();
                return MapErpRef(existing);
            }

            SilaCulinaryErpReconciliationRef created = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SilaTransactionRef = silaTxRef,
                TransactionType = txType,
                ErpSystem = erpSystem,
                ErpDocumentRef = erpDocRef,
                PostingStatus = postingStatus,
                PostedAtUtc = postingStatus == "CONFIRMED" ? DateTime.UtcNow : null,
                ErrorMessage = errorMsg,
                ReconciliationStatus = postingStatus == "CONFIRMED" ? "MATCHED" : "PENDING_CONFIRMATION",
                IdempotencyKey = idempotencyKey,
                IsActive = true
            };
            _repository.SilaCulinaryErpReconciliationRef.Create(created);
            await _repository.SaveAsync();
            return MapErpRef(created);
        }

        public async Task<SilaHistoricalLearningSnapshotDto> CaptureHistoricalLearningSnapshotAsync(Guid organizationId, Guid userId, string actorName, Guid eventClosureId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            SilaCulinaryEventClosure closure = await _repository.SilaCulinaryEventClosure
                .FindFirstByConditionAsync(x => x.BuyerId == buyer.Id && x.Id == eventClosureId && x.IsActive)
                ?? throw new InvalidOperationException("Event closure not found.");

            bool isFinal = closure.FinancialStatus == "FINANCIALLY_CLOSED";
            string num = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.HISTORICAL_LEARNING, 6, cancellationToken);

            decimal wasteCost = await _repository.SilaCulinaryWasteEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventId == closure.EventId && !x.IsNormalRecipeYieldLoss && x.IsActive)
                .SumAsync(x => x.EstimatedWasteCost, cancellationToken);

            decimal wasteQty = await _repository.SilaCulinaryWasteEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventId == closure.EventId && !x.IsNormalRecipeYieldLoss && x.IsActive)
                .SumAsync(x => x.WasteQty, cancellationToken);

            SilaCulinaryHistoricalLearningSnapshot snap = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SnapshotNumber = num,
                DatasetVersion = "DS-4B-1.0",
                DataMaturity = isFinal ? "FINAL_RECONCILED" : "PROVISIONAL_ACTUAL",
                ValidatedForTraining = isFinal, // Only finalized reconciled data is approved for training
                PropertyId = closure.PropertyId,
                EventId = closure.EventId,
                ExpectedGuestCount = closure.ExpectedGuestCount,
                ConfirmedGuestCount = closure.ConfirmedGuestCount,
                ActualGuestCount = closure.ActualGuestCount,
                PlannedFoodCost = closure.DirectMaterialCostActual,
                ActualFoodCost = closure.DirectMaterialCostActual,
                PlannedLaborCost = closure.DirectLaborCostActual,
                ActualLaborCost = closure.DirectLaborCostActual,
                ActualRevenue = closure.NetRevenueActual,
                ActualContributionMargin = closure.ContributionMarginActual,
                ActualFullyLoadedProfit = closure.FullyLoadedProfitActual,
                TotalWasteQty = wasteQty,
                TotalWasteCost = wasteCost,
                SnapshotPayloadJson = closure.FourPerspectivePnlJson,
                CapturedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryHistoricalLearningSnapshot.Create(snap);
            await _repository.SaveAsync();
            return MapSnapshot(snap);
        }

        public async Task<SilaReconciliationCopilotResponseDto> AskCostControlCopilotAsync(Guid organizationId, SilaReconciliationCopilotRequestDto request, CancellationToken cancellationToken)
        {
            if (!request.CanViewFinancialCost)
            {
                return new SilaReconciliationCopilotResponseDto
                {
                    AccessDenied = true,
                    DirectAnswer = "Access denied: your role does not permit viewing financial cost or event profitability data.",
                    SupportingMetrics = new(),
                    IdentifiedDataGaps = new(),
                    RecommendedAction = "Request Cost Controller or Finance Controller authorization to inspect financial reconciliation metrics."
                };
            }

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            await EnsureFeatureEnabledAsync(buyer.Id, cancellationToken);

            List<SilaCulinaryProductionReconciliation> recons = await _repository.SilaCulinaryProductionReconciliation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId))
                .ToListAsync(cancellationToken);

            List<SilaCulinaryWasteEvent> wastes = await _repository.SilaCulinaryWasteEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId))
                .ToListAsync(cancellationToken);

            List<SilaCulinaryEventClosure> closures = await _repository.SilaCulinaryEventClosure
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!request.PropertyId.HasValue || x.PropertyId == request.PropertyId))
                .ToListAsync(cancellationToken);

            decimal totalWasteCost = wastes.Where(w => !w.IsNormalRecipeYieldLoss).Sum(w => w.EstimatedWasteCost);
            decimal totalCostVar = recons.Sum(r => r.FullyLoadedProductionCost - r.PlannedProductionCost);
            int openClosures = closures.Count(c => c.FinancialStatus != "FINANCIALLY_CLOSED");
            int blockedRecons = recons.Count(r => r.SettlementBlocked);

            return new SilaReconciliationCopilotResponseDto
            {
                AccessDenied = false,
                DirectAnswer = $"Across {recons.Count} reconciled production order(s), production cost variance is {totalCostVar:N2}, unexpected food waste totals {totalWasteCost:N2}, {blockedRecons} settlement(s) are blocked, and {openClosures} event(s) await financial closure.",
                DataSource = "SILA Phase 4B Reconciliation, Waste & Event Closure Ledger",
                DateRange = $"{DateTime.UtcNow.AddDays(-30):yyyy-MM-dd} to {DateTime.UtcNow:yyyy-MM-dd} UTC",
                CalculationBasis = "Deterministic ledger sums (no LLM arithmetic)",
                FinancialImpact = totalCostVar + totalWasteCost,
                SupportingMetrics = new()
                {
                    $"Production Cost Variance: {totalCostVar:N2}",
                    $"Unexpected Waste Cost: {totalWasteCost:N2}",
                    $"Blocked Settlements: {blockedRecons}",
                    $"Pending Event Closures: {openClosures}"
                },
                IdentifiedDataGaps = recons.Count == 0 ? new() { "No production reconciliations recorded yet for the selected property." } : new(),
                RecommendedAction = blockedRecons > 0
                    ? "Resolve unexplained material variances and pending quality/waste approvals on blocked production settlements."
                    : "Proceed with financial closure and capture finalized historical learning snapshots.",
                SupportingDocuments = recons.Take(5).Select(r => r.ReconciliationNumber).Concat(closures.Take(5).Select(c => c.ClosureNumber)).ToList()
            };
        }

        public async Task<List<object>> Build27ReportsPayloadAsync(Guid organizationId, Guid? propertyId, bool canViewSalaryDetails, CancellationToken cancellationToken)
        {
            SilaPhase4BDashboardDto dash = await GetDashboardAsync(organizationId, propertyId, canViewSalaryDetails, cancellationToken);
            var reports = new List<object>
            {
                new { sheet_name = "01_Actual_Consumption", columns = new[] { "Number", "Material", "Batch", "Method", "Planned", "Actual", "UOM" }, rows = dash.Consumptions.Select(c => new object[] { c.ConsumptionNumber, c.MaterialCode, c.InventoryBatchNumber, c.ConsumptionMethod, c.PlannedQty, c.ActualConsumedQty, c.ConsumptionUom }).ToList() },
                new { sheet_name = "02_Planned_vs_Actual", columns = new[] { "Number", "Material", "Planned", "Actual", "Variance" }, rows = dash.Consumptions.Select(c => new object[] { c.ConsumptionNumber, c.MaterialCode, c.PlannedQty, c.ActualConsumedQty, c.ActualConsumedQty - c.PlannedQty }).ToList() },
                new { sheet_name = "03_Additional_Material", columns = new[] { "Number", "Material", "Requested", "Approved", "Reason", "Status" }, rows = dash.AdditionalRequests.Select(a => new object[] { a.RequestNumber ?? "", a.MaterialCode, a.RequestedQty, a.ApprovedQty, a.ReasonCode, a.ApprovalStatus }).ToList() },
                new { sheet_name = "04_Ingredient_Returns", columns = new[] { "Number", "Material", "Returned", "Classification", "Bucket", "Status" }, rows = dash.Returns.Select(r => new object[] { r.ReturnNumber, r.MaterialCode, r.ReturnedQty, r.ReturnClassification, r.RoutedStockBucket, r.Status }).ToList() },
                new { sheet_name = "05_Issue_Reversals", columns = new[] { "Number", "OrigIssue", "Material", "Reversed", "Type", "Status" }, rows = dash.Reversals.Select(r => new object[] { r.ReversalNumber, r.OriginalIssueNumber, r.MaterialCode, r.ReversalQty, r.ReversalType, r.Status }).ToList() },
                new { sheet_name = "06_Food_Waste", columns = new[] { "Code", "Item", "Domain", "Category", "Qty", "NormalYieldLoss", "Status" }, rows = dash.WasteEvents.Select(w => new object[] { w.WasteCode, w.ItemName, w.ItemDomain, w.WasteCategory, w.WasteQty, w.IsNormalRecipeYieldLoss, w.ApprovalStatus }).ToList() },
                new { sheet_name = "07_Waste_Cost", columns = new[] { "Code", "Item", "Category", "EstimatedCost" }, rows = dash.WasteEvents.Select(w => new object[] { w.WasteCode, w.ItemName, w.WasteCategory, w.EstimatedWasteCost }).ToList() },
                new { sheet_name = "08_Finished_Leftovers", columns = new[] { "Number", "Source", "Product", "Remaining", "Quality", "Disposition" }, rows = dash.Leftovers.Select(l => new object[] { l.LeftoverNumber, l.SourceType, l.FinishedProductName, l.RemainingQty, l.QualityStatus, l.Disposition }).ToList() },
                new { sheet_name = "09_Food_Disposal", columns = new[] { "Number", "Item", "Qty", "Method", "Approval", "Cost" }, rows = dash.Disposals.Select(d => new object[] { d.DisposalNumber, d.ItemName, d.DisposalQty, d.DisposalMethod, d.SupervisorApprovalStatus, d.FinancialWasteCost }).ToList() },
                new { sheet_name = "10_Production_Yield", columns = new[] { "ReconNumber", "Status", "CostStatus" }, rows = dash.Reconciliations.Select(r => new object[] { r.ReconciliationNumber, r.ReconciliationStatus, r.CostStatus }).ToList() },
                new { sheet_name = "11_Portion_Variance", columns = new[] { "Recipe", "Samples", "StdWeightG", "AvgWeightG", "VariancePct", "EstCostVar" }, rows = dash.PortionSamples.Select(p => new object[] { p.RecipeCode, p.SampleCount, p.StandardPortionWeightGrams, p.AverageActualWeightGrams, p.PortionWeightVariancePercent, p.EstimatedPortionCostVariance }).ToList() },
                new { sheet_name = "12_Inventory_Recon", columns = new[] { "ReconNumber", "Material", "NetIssued", "Accounted", "Unexplained", "Status" }, rows = dash.Reconciliations.SelectMany(r => r.Lines.Select(l => new object[] { r.ReconciliationNumber, l.MaterialCode, l.NetIssuedQty, l.AccountedQty, l.UnexplainedQty, l.LineStatus })).ToList() },
                new { sheet_name = "13_Production_Cost", columns = new[] { "ReconNumber", "Planned", "Material", "Labor", "Overhead", "FullyLoaded" }, rows = dash.Reconciliations.Select(r => new object[] { r.ReconciliationNumber, r.PlannedProductionCost, r.ActualDirectMaterialCost, r.ActualDirectLaborCost, r.AllocatedFixedOverhead, r.FullyLoadedProductionCost }).ToList() },
                new { sheet_name = "14_Cost_Variance", columns = new[] { "ReconNumber", "Planned", "Actual", "Variance" }, rows = dash.Reconciliations.Select(r => new object[] { r.ReconciliationNumber, r.PlannedProductionCost, r.FullyLoadedProductionCost, r.FullyLoadedProductionCost - r.PlannedProductionCost }).ToList() },
                new { sheet_name = "15_Cost_Per_Portion", columns = new[] { "Recipe", "Samples", "PortionCostVar" }, rows = dash.PortionSamples.Select(p => new object[] { p.RecipeCode, p.SampleCount, p.EstimatedPortionCostVariance }).ToList() },
                new { sheet_name = "16_Cost_Per_Cover", columns = new[] { "Closure", "Event", "ActualGuests", "NetRevenue", "VariableCost" }, rows = dash.EventClosures.Select(e => new object[] { e.ClosureNumber, e.EventName, e.ActualGuestCount, e.NetRevenueActual, e.TotalVariableCostActual }).ToList() },
                new { sheet_name = "17_Central_Kitchen_Alloc", columns = new[] { "ReconNumber", "Methodology" }, rows = dash.Reconciliations.Select(r => new object[] { r.ReconciliationNumber, r.CostMethodology }).ToList() },
                new { sheet_name = "18_Buffet_Reconciliation", columns = new[] { "Summary", "Value" }, rows = new List<object[]> { new object[] { "BalancedReconciliations", dash.BalancedReconciliationCount } } },
                new { sheet_name = "19_Event_Consumption", columns = new[] { "Closure", "ActualGuests", "MaterialCost" }, rows = dash.EventClosures.Select(e => new object[] { e.ClosureNumber, e.ActualGuestCount, e.TotalVariableCostActual }).ToList() },
                new { sheet_name = "20_Actual_Event_PnL", columns = new[] { "Closure", "Event", "Revenue", "VariableCost", "Contribution", "NetProfit", "FinStatus" }, rows = dash.EventClosures.Select(e => new object[] { e.ClosureNumber, e.EventName, e.NetRevenueActual, e.TotalVariableCostActual, e.ContributionMarginActual, e.FullyLoadedProfitActual, e.FinancialStatus }).ToList() },
                new { sheet_name = "21_Event_Profit_Variance", columns = new[] { "Closure", "Contribution", "FullyLoadedProfit" }, rows = dash.EventClosures.Select(e => new object[] { e.ClosureNumber, e.ContributionMarginActual, e.FullyLoadedProfitActual }).ToList() },
                new { sheet_name = "22_Pending_Cost_Accruals", columns = new[] { "Closure", "Event", "PendingAccruedCost", "FinStatus" }, rows = dash.EventClosures.Select(e => new object[] { e.ClosureNumber, e.EventName, e.PendingAccruedCost, e.FinancialStatus }).ToList() },
                new { sheet_name = "23_Production_Settlement", columns = new[] { "ReconNumber", "OpStatus", "FinStatus", "Blocked" }, rows = dash.Reconciliations.Select(r => new object[] { r.ReconciliationNumber, r.OperationalSettlementStatus, r.FinancialSettlementStatus, r.SettlementBlocked }).ToList() },
                new { sheet_name = "24_Event_Closure", columns = new[] { "Closure", "Event", "OpStatus", "FinStatus", "Version" }, rows = dash.EventClosures.Select(e => new object[] { e.ClosureNumber, e.EventName, e.OperationalStatus, e.FinancialStatus, e.SettlementVersion }).ToList() },
                new { sheet_name = "25_ERP_Reconciliation", columns = new[] { "SilaRef", "Type", "ErpSystem", "ErpDocRef", "PostingStatus", "ReconStatus", "Retries" }, rows = dash.ErpReconciliations.Select(x => new object[] { x.SilaTransactionRef, x.TransactionType, x.ErpSystem, x.ErpDocumentRef ?? "", x.PostingStatus, x.ReconciliationStatus, x.RetryCount }).ToList() },
                new { sheet_name = "26_AI_Variance_Invest", columns = new[] { "ReconNumber", "Blocked", "Status" }, rows = dash.Reconciliations.Where(r => r.SettlementBlocked).Select(r => new object[] { r.ReconciliationNumber, r.SettlementBlocked, r.ReconciliationStatus }).ToList() },
                new { sheet_name = "27_Historical_Learning", columns = new[] { "Snapshot", "DatasetVersion", "Maturity", "Validated", "ActualGuests", "ActualProfit" }, rows = dash.HistoricalSnapshots.Select(s => new object[] { s.SnapshotNumber, s.DatasetVersion, s.DataMaturity, s.ValidatedForTraining, s.ActualGuestCount, s.ActualFullyLoadedProfit }).ToList() }
            };
            return reports;
        }

        private static List<object> SafeDeserializeList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<object>>(json, JsonOpts) ?? new(); }
            catch { return new(); }
        }

        private static SilaCulinaryReconciliationConfigDto MapConfig(SilaCulinaryReconciliationConfig c) => new()
        {
            Id = c.Id,
            PropertyId = c.PropertyId,
            ConsumptionMethod = c.ConsumptionMethod,
            BackflushRequiresApproval = c.BackflushRequiresApproval,
            InventoryValuationMethod = c.InventoryValuationMethod,
            MaterialReconciliationTolerancePercent = c.MaterialReconciliationTolerancePercent,
            WasteApprovalThresholdAmount = c.WasteApprovalThresholdAmount,
            YieldVarianceThresholdPercent = c.YieldVarianceThresholdPercent,
            PortionVarianceThresholdPercent = c.PortionVarianceThresholdPercent,
            FoodCostVarianceThresholdPercent = c.FoodCostVarianceThresholdPercent,
            LaborAllocationMethod = c.LaborAllocationMethod,
            OverheadAllocationMethod = c.OverheadAllocationMethod,
            CentralKitchenAllocationMethod = c.CentralKitchenAllocationMethod,
            AllowProvisionalSettlement = c.AllowProvisionalSettlement,
            FinancialPeriodLockedBeforeUtc = c.FinancialPeriodLockedBeforeUtc,
            ClosedPeriodAdjustmentPolicy = c.ClosedPeriodAdjustmentPolicy,
            ErpPostingPolicy = c.ErpPostingPolicy,
            InventorySystemOfRecord = c.InventorySystemOfRecord,
            MaxLeftoverHoldingHours = c.MaxLeftoverHoldingHours,
            RequireDisposalPhotoAboveThreshold = c.RequireDisposalPhotoAboveThreshold,
            DefaultCurrency = c.DefaultCurrency,
            ConfigVersion = c.ConfigVersion,
            EffectiveFromUtc = c.EffectiveFromUtc,
            ApprovalMatrixJson = c.ApprovalMatrixJson,
            ConfigHistoryJson = c.ConfigHistoryJson
        };

        private static SilaProductionConsumptionDto MapConsumption(SilaCulinaryProductionConsumption c) => new()
        {
            Id = c.Id,
            ConsumptionNumber = c.ConsumptionNumber,
            ProductionOrderId = c.ProductionOrderId,
            ProductionBatchId = c.ProductionBatchId,
            MaterialCode = c.MaterialCode,
            MaterialName = c.MaterialName,
            InventoryBatchNumber = c.InventoryBatchNumber,
            PlannedQty = c.PlannedQty,
            IssuedQty = c.IssuedQty,
            ActualConsumedQty = c.ActualConsumedQty,
            ConsumptionUom = c.ConsumptionUom,
            ConsumptionMethod = c.ConsumptionMethod,
            IncludesWaste = c.IncludesWaste,
            CaptureMode = c.CaptureMode,
            IsReversal = c.IsReversal,
            ApprovalStatus = c.ApprovalStatus,
            SourceChannel = c.SourceChannel,
            ConsumedAtUtc = c.ConsumedAtUtc
        };

        private static SilaAdditionalMaterialRequestDto MapAdditionalRequest(SilaCulinaryAdditionalMaterialRequest r) => new()
        {
            Id = r.Id,
            RequestNumber = r.RequestNumber,
            PropertyId = r.PropertyId,
            KitchenId = r.KitchenId,
            ProductionOrderId = r.ProductionOrderId,
            ProductionBatchId = r.ProductionBatchId,
            MaterialId = r.MaterialId,
            MaterialCode = r.MaterialCode,
            MaterialName = r.MaterialName,
            RequestedQty = r.RequestedQty,
            ApprovedQty = r.ApprovedQty,
            Uom = r.Uom,
            AvailableStockSnapshot = r.AvailableStockSnapshot,
            EstimatedCostImpact = r.EstimatedCostImpact,
            ReasonCode = r.ReasonCode,
            ReasonNotes = r.ReasonNotes,
            AlternativeMaterialCode = r.AlternativeMaterialCode,
            AlternativeStoreLocationId = r.AlternativeStoreLocationId,
            EmergencyAuthorized = r.EmergencyAuthorized,
            ApprovalStatus = r.ApprovalStatus,
            InventoryIssued = r.InventoryIssued,
            InventoryIssueReference = r.InventoryIssueReference
        };

        private static SilaMaterialReturnDto MapReturn(SilaCulinaryMaterialReturn r) => new()
        {
            Id = r.Id,
            ReturnNumber = r.ReturnNumber,
            ProductionOrderId = r.ProductionOrderId,
            ProductionBatchId = r.ProductionBatchId,
            MaterialCode = r.MaterialCode,
            MaterialName = r.MaterialName,
            InventoryBatchNumber = r.InventoryBatchNumber,
            ReturnedQty = r.ReturnedQty,
            Uom = r.Uom,
            ReturnClassification = r.ReturnClassification,
            QualityInspectionStatus = r.QualityInspectionStatus,
            RoutedStockBucket = r.RoutedStockBucket,
            Status = r.Status,
            ReturnedAtUtc = r.ReturnedAtUtc
        };

        private static SilaMaterialIssueReversalDto MapReversal(SilaCulinaryMaterialIssueReversal r) => new()
        {
            Id = r.Id,
            ReversalNumber = r.ReversalNumber,
            OriginalIssueNumber = r.OriginalIssueNumber,
            MaterialCode = r.MaterialCode,
            ReversalType = r.ReversalType,
            ReversalQty = r.ReversalQty,
            Uom = r.Uom,
            Reason = r.Reason,
            Status = r.Status,
            ErpReference = r.ErpReference,
            ReversedAtUtc = r.ReversedAtUtc
        };

        private static SilaWasteEventDto MapWasteEvent(SilaCulinaryWasteEvent w) => new()
        {
            Id = w.Id,
            WasteCode = w.WasteCode,
            ItemDomain = w.ItemDomain,
            IsNormalRecipeYieldLoss = w.IsNormalRecipeYieldLoss,
            AlreadyDeductedFromInventory = w.AlreadyDeductedFromInventory,
            SecondDeductionPrevented = w.SecondDeductionPrevented,
            ItemCode = w.ItemCode,
            ItemName = w.ItemName,
            WasteCategory = w.WasteCategory,
            WasteQty = w.WasteQty,
            Uom = w.Uom,
            EstimatedWasteCost = w.EstimatedWasteCost,
            ApprovalStatus = w.ApprovalStatus,
            RecordedAtUtc = w.RecordedAtUtc
        };

        private static SilaFoodLeftoverDto MapLeftover(SilaCulinaryFoodLeftover l) => new()
        {
            Id = l.Id,
            LeftoverNumber = l.LeftoverNumber,
            SourceType = l.SourceType,
            FinishedProductName = l.FinishedProductName,
            OriginalProductionQty = l.OriginalProductionQty,
            ServedQty = l.ServedQty,
            RemainingQty = l.RemainingQty,
            Uom = l.Uom,
            QualityStatus = l.QualityStatus,
            FoodSafetyApproved = l.FoodSafetyApproved,
            Disposition = l.Disposition,
            ApprovedUseByUtc = l.ApprovedUseByUtc
        };

        private static SilaWasteDisposalDto MapDisposal(SilaCulinaryWasteDisposal d) => new()
        {
            Id = d.Id,
            DisposalNumber = d.DisposalNumber,
            ItemName = d.ItemName,
            DisposalQty = d.DisposalQty,
            Uom = d.Uom,
            DisposalMethod = d.DisposalMethod,
            SupervisorApprovalStatus = d.SupervisorApprovalStatus,
            FinancialWasteCost = d.FinancialWasteCost,
            DisposedAtUtc = d.DisposedAtUtc
        };

        private static SilaPortionSampleDto MapPortionSample(SilaCulinaryPortionSample p) => new()
        {
            Id = p.Id,
            RecipeCode = p.RecipeCode,
            SampleCount = p.SampleCount,
            StandardPortionWeightGrams = p.StandardPortionWeightGrams,
            AverageActualWeightGrams = p.AverageActualWeightGrams,
            PortionWeightVariancePercent = p.PortionWeightVariancePercent,
            EstimatedPortionCostVariance = p.EstimatedPortionCostVariance,
            SamplingBasisNote = p.SamplingBasisNote,
            SampledAtUtc = p.SampledAtUtc
        };

        private static SilaProductionReconciliationDto MapReconciliation(SilaCulinaryProductionReconciliation r, List<SilaCulinaryProductionReconciliationLine> lines) => new()
        {
            Id = r.Id,
            ReconciliationNumber = r.ReconciliationNumber,
            ProductionOrderId = r.ProductionOrderId,
            ProductionBatchId = r.ProductionBatchId,
            ReconciliationStatus = r.ReconciliationStatus,
            OperationalSettlementStatus = r.OperationalSettlementStatus,
            FinancialSettlementStatus = r.FinancialSettlementStatus,
            SettlementBlocked = r.SettlementBlocked,
            PlannedProductionCost = r.PlannedProductionCost,
            ActualDirectMaterialCost = r.ActualDirectMaterialCost,
            ActualDirectLaborCost = r.ActualDirectLaborCost,
            ActualEquipmentUtilityPackagingCost = r.ActualEquipmentUtilityPackagingCost,
            AllocatedFixedOverhead = r.AllocatedFixedOverhead,
            FullyLoadedProductionCost = r.FullyLoadedProductionCost,
            CostStatus = r.CostStatus,
            CostMethodology = r.CostMethodology,
            SettlementVersion = r.SettlementVersion,
            CostVarianceBridgeJson = r.CostVarianceBridgeJson,
            ValidationChecklistJson = r.ValidationChecklistJson,
            FinishedProductBalanceJson = r.FinishedProductBalanceJson,
            Lines = lines.Select(l => new SilaProductionReconciliationLineDto
            {
                MaterialCode = l.MaterialCode,
                MaterialName = l.MaterialName,
                InventoryBatchNumber = l.InventoryBatchNumber,
                Uom = l.Uom,
                PlannedQty = l.PlannedQty,
                ReservedQty = l.ReservedQty,
                PickedQty = l.PickedQty,
                OriginalIssuedQty = l.OriginalIssuedQty,
                AdditionalIssuedQty = l.AdditionalIssuedQty,
                ReversedIssueQty = l.ReversedIssueQty,
                NetIssuedQty = l.NetIssuedQty,
                NetInboundTransferQty = l.NetInboundTransferQty,
                NetOutboundTransferQty = l.NetOutboundTransferQty,
                UsableConsumptionQty = l.UsableConsumptionQty,
                SeparateWasteQty = l.SeparateWasteQty,
                ReturnedQty = l.ReturnedQty,
                RemainingWipQty = l.RemainingWipQty,
                AccountedQty = l.AccountedQty,
                UnexplainedQty = l.UnexplainedQty,
                TolerancePercent = l.TolerancePercent,
                LineStatus = l.LineStatus,
                SettlementBlocked = l.SettlementBlocked,
                PlanningVolumeVarianceQty = l.PlanningVolumeVarianceQty,
                UsageVarianceQty = l.UsageVarianceQty,
                UsageCostVariance = l.UsageCostVariance,
                PriceCostVariance = l.PriceCostVariance
            }).ToList()
        };

        private static SilaCulinaryEventClosureSummaryDto MapClosure(SilaCulinaryEventClosure c) => new()
        {
            Id = c.Id,
            EventId = c.EventId,
            ClosureNumber = c.ClosureNumber,
            EventName = c.EventName,
            ExpectedGuestCount = c.ExpectedGuestCount,
            ConfirmedGuestCount = c.ConfirmedGuestCount,
            ActualGuestCount = c.ActualGuestCount,
            Currency = c.Currency,
            OperationalStatus = c.OperationalStatus,
            FinancialStatus = c.FinancialStatus,
            SettlementVersion = c.SettlementVersion,
            NetRevenueActual = c.NetRevenueActual,
            TotalVariableCostActual = c.TotalVariableCostActual,
            ContributionMarginActual = c.ContributionMarginActual,
            FullyLoadedProfitActual = c.FullyLoadedProfitActual,
            PendingAccruedCost = c.PendingAccruedCost,
            FourPerspectivePnlJson = c.FourPerspectivePnlJson,
            ProfitabilityBridgeJson = c.ProfitabilityBridgeJson
        };

        private static SilaErpReconciliationRefDto MapErpRef(SilaCulinaryErpReconciliationRef e) => new()
        {
            Id = e.Id,
            SilaTransactionRef = e.SilaTransactionRef,
            TransactionType = e.TransactionType,
            ErpSystem = e.ErpSystem,
            ErpDocumentRef = e.ErpDocumentRef,
            PostingStatus = e.PostingStatus,
            ReconciliationStatus = e.ReconciliationStatus,
            RetryCount = e.RetryCount,
            ErrorMessage = e.ErrorMessage,
            PostedAtUtc = e.PostedAtUtc
        };

        private static SilaHistoricalLearningSnapshotDto MapSnapshot(SilaCulinaryHistoricalLearningSnapshot s) => new()
        {
            Id = s.Id,
            SnapshotNumber = s.SnapshotNumber,
            DatasetVersion = s.DatasetVersion,
            DataMaturity = s.DataMaturity,
            ValidatedForTraining = s.ValidatedForTraining,
            EventType = s.EventType,
            MealPeriod = s.MealPeriod,
            ExpectedGuestCount = s.ExpectedGuestCount,
            ActualGuestCount = s.ActualGuestCount,
            PlannedFoodCost = s.PlannedFoodCost,
            ActualFoodCost = s.ActualFoodCost,
            ActualRevenue = s.ActualRevenue,
            ActualContributionMargin = s.ActualContributionMargin,
            ActualFullyLoadedProfit = s.ActualFullyLoadedProfit,
            TotalWasteCost = s.TotalWasteCost,
            CapturedAtUtc = s.CapturedAtUtc
        };
    }
}
