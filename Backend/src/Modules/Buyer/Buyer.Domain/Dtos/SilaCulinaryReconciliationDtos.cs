namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryReconciliationConfigDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string ConsumptionMethod { get; set; } = "EXPLICIT";
        public bool BackflushRequiresApproval { get; set; } = true;
        public string InventoryValuationMethod { get; set; } = "WEIGHTED_AVERAGE";
        public decimal MaterialReconciliationTolerancePercent { get; set; } = 2.0m;
        public decimal WasteApprovalThresholdAmount { get; set; } = 250.0m;
        public decimal YieldVarianceThresholdPercent { get; set; } = 5.0m;
        public decimal PortionVarianceThresholdPercent { get; set; } = 5.0m;
        public decimal FoodCostVarianceThresholdPercent { get; set; } = 3.0m;
        public string LaborAllocationMethod { get; set; } = "ACTUAL_TIME";
        public string OverheadAllocationMethod { get; set; } = "PERCENT_OF_DIRECT_COST";
        public string CentralKitchenAllocationMethod { get; set; } = "ACTUAL_COST_PROPORTIONAL";
        public bool AllowProvisionalSettlement { get; set; } = true;
        public DateTime? FinancialPeriodLockedBeforeUtc { get; set; }
        public string ClosedPeriodAdjustmentPolicy { get; set; } = "CURRENT_OPEN_PERIOD_ACCRUAL";
        public string ErpPostingPolicy { get; set; } = "OUTBOX_CONFIRMED";
        public string InventorySystemOfRecord { get; set; } = "SILA_LEDGER";
        public int MaxLeftoverHoldingHours { get; set; } = 24;
        public bool RequireDisposalPhotoAboveThreshold { get; set; } = true;
        public string DefaultCurrency { get; set; } = "AED";
        public int ConfigVersion { get; set; } = 1;
        public DateTime EffectiveFromUtc { get; set; }
        public string ApprovalMatrixJson { get; set; } = "{}";
        public string ConfigHistoryJson { get; set; } = "[]";
    }

    public class SilaRecordConsumptionRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? RecipeId { get; set; }
        public Guid? SubRecipeId { get; set; }
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public decimal PlannedQty { get; set; }
        public decimal IssuedQty { get; set; }
        public decimal ActualConsumedQty { get; set; }
        public string ConsumptionUom { get; set; } = "KG";
        public string ConsumptionMethod { get; set; } = "EXPLICIT";
        public bool IncludesWaste { get; set; }
        public bool IsSubstitution { get; set; }
        public string? SubstitutedFromMaterialCode { get; set; }
        public string CaptureMode { get; set; } = "MANUAL";
        public bool BackflushApproved { get; set; }
        public string? DeviationReason { get; set; }
        public string? SupportingEvidence { get; set; }
        public string SourceChannel { get; set; } = "CLOUD";
        public string? IdempotencyKey { get; set; }
    }

    public class SilaProductionConsumptionDto
    {
        public Guid Id { get; set; }
        public string ConsumptionNumber { get; set; } = string.Empty;
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public decimal PlannedQty { get; set; }
        public decimal IssuedQty { get; set; }
        public decimal ActualConsumedQty { get; set; }
        public string ConsumptionUom { get; set; } = "KG";
        public string ConsumptionMethod { get; set; } = "EXPLICIT";
        public bool IncludesWaste { get; set; }
        public string CaptureMode { get; set; } = "MANUAL";
        public bool IsReversal { get; set; }
        public string ApprovalStatus { get; set; } = "APPROVED";
        public string SourceChannel { get; set; } = "CLOUD";
        public DateTime ConsumedAtUtc { get; set; }
    }

    public class SilaAdditionalMaterialRequestDto
    {
        public Guid? Id { get; set; }
        public string? RequestNumber { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequestedQty { get; set; }
        public decimal ApprovedQty { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal AvailableStockSnapshot { get; set; }
        public decimal EstimatedCostImpact { get; set; }
        public string ReasonCode { get; set; } = "UNEXPECTED_DEMAND";
        public string? ReasonNotes { get; set; }
        public string? AlternativeMaterialCode { get; set; }
        public Guid? AlternativeStoreLocationId { get; set; }
        public bool EmergencyAuthorized { get; set; }
        public string ApprovalStatus { get; set; } = "PENDING";
        public bool InventoryIssued { get; set; }
        public string? InventoryIssueReference { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SilaMaterialReturnRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? OriginalMaterialIssueRefId { get; set; }
        public Guid? ReturningKitchenId { get; set; }
        public Guid? ReceivingStoreLocationId { get; set; }
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public decimal ReturnedQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string ReturnClassification { get; set; } = "UNOPENED_USABLE";
        public string IngredientCondition { get; set; } = "GOOD";
        public DateTime? ExpiryUtc { get; set; }
        public string ReturnReason { get; set; } = "EXCESS_ISSUED";
        public string? IdempotencyKey { get; set; }
    }

    public class SilaMaterialReturnDto
    {
        public Guid Id { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public decimal ReturnedQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string ReturnClassification { get; set; } = string.Empty;
        public string QualityInspectionStatus { get; set; } = string.Empty;
        public string RoutedStockBucket { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ReturnedAtUtc { get; set; }
    }

    public class SilaMaterialIssueReversalRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? OriginalMaterialIssueRefId { get; set; }
        public string OriginalIssueNumber { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public string ReversalType { get; set; } = "INCORRECT_ISSUE";
        public decimal ReversalQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string Reason { get; set; } = string.Empty;
        public string? ErpReference { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SilaMaterialIssueReversalDto
    {
        public Guid Id { get; set; }
        public string ReversalNumber { get; set; } = string.Empty;
        public string OriginalIssueNumber { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string ReversalType { get; set; } = string.Empty;
        public decimal ReversalQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErpReference { get; set; }
        public DateTime ReversedAtUtc { get; set; }
    }

    public class SilaRecordWasteEventRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? MaterialId { get; set; }
        public Guid? RecipeId { get; set; }
        public string ItemDomain { get; set; } = "RAW_INGREDIENT";
        public bool IsNormalRecipeYieldLoss { get; set; }
        public bool AlreadyDeductedFromInventory { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public string WasteCategory { get; set; } = "PRODUCTION_WASTE";
        public decimal WasteQty { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal EstimatedWasteCost { get; set; }
        public string WasteReason { get; set; } = string.Empty;
        public string DisposalMethod { get; set; } = "ORGANIC_BIN";
        public string? PhotoEvidenceRef { get; set; }
        public string SourceChannel { get; set; } = "CLOUD";
        public string? IdempotencyKey { get; set; }
    }

    public class SilaWasteEventDto
    {
        public Guid Id { get; set; }
        public string WasteCode { get; set; } = string.Empty;
        public string ItemDomain { get; set; } = string.Empty;
        public bool IsNormalRecipeYieldLoss { get; set; }
        public bool AlreadyDeductedFromInventory { get; set; }
        public bool SecondDeductionPrevented { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string WasteCategory { get; set; } = string.Empty;
        public decimal WasteQty { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal EstimatedWasteCost { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime RecordedAtUtc { get; set; }
    }

    public class SilaFoodLeftoverRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? RecipeId { get; set; }
        public string SourceType { get; set; } = "BUFFET";
        public string FinishedProductName { get; set; } = string.Empty;
        public decimal OriginalProductionQty { get; set; }
        public decimal ServedQty { get; set; }
        public decimal RemainingQty { get; set; }
        public string Uom { get; set; } = "PORTION";
        public string HoldingConditions { get; set; } = "CHILLED_BELOW_5C";
        public DateTime? ProductionTimestampUtc { get; set; }
        public DateTime? ApprovedUseByUtc { get; set; }
        public string StorageConditions { get; set; } = "BLAST_CHILLED_COVERED";
        public string AllergenInfo { get; set; } = string.Empty;
        public string QualityStatus { get; set; } = "SAFE_VERIFIED";
        public bool FoodSafetyApproved { get; set; }
        public string Disposition { get; set; } = "QUALITY_INSPECTION";
    }

    public class SilaFoodLeftoverDto
    {
        public Guid Id { get; set; }
        public string LeftoverNumber { get; set; } = string.Empty;
        public string SourceType { get; set; } = string.Empty;
        public string FinishedProductName { get; set; } = string.Empty;
        public decimal OriginalProductionQty { get; set; }
        public decimal ServedQty { get; set; }
        public decimal RemainingQty { get; set; }
        public string Uom { get; set; } = "PORTION";
        public string QualityStatus { get; set; } = string.Empty;
        public bool FoodSafetyApproved { get; set; }
        public string Disposition { get; set; } = string.Empty;
        public DateTime ApprovedUseByUtc { get; set; }
    }

    public class SilaWasteDisposalRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? WasteEventId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal DisposalQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string DisposalReason { get; set; } = string.Empty;
        public string DisposalMethod { get; set; } = "ORGANIC_COMPOST";
        public string DisposalLocation { get; set; } = "Main Waste Dock";
        public string ResponsibleEmployee { get; set; } = string.Empty;
        public string? SupportingEvidenceRef { get; set; }
        public decimal FinancialWasteCost { get; set; }
    }

    public class SilaWasteDisposalDto
    {
        public Guid Id { get; set; }
        public string DisposalNumber { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal DisposalQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string DisposalMethod { get; set; } = string.Empty;
        public string SupervisorApprovalStatus { get; set; } = string.Empty;
        public decimal FinancialWasteCost { get; set; }
        public DateTime DisposedAtUtc { get; set; }
    }

    public class SilaOutputConfirmationRequestDto
    {
        public Guid ProductionOrderId { get; set; }
        public Guid ProductionBatchId { get; set; }
        public Guid? ParentBatchId { get; set; }
        public string OutputType { get; set; } = "FINISHED";
        public decimal PlannedOutputQty { get; set; }
        public decimal ActualGrossOutputQty { get; set; }
        public decimal AcceptedUsableOutputQty { get; set; }
        public decimal RejectedOutputQty { get; set; }
        public decimal ReworkQty { get; set; }
        public decimal FinishedProductWasteQty { get; set; }
        public string OutputUom { get; set; } = "PORTION";
        public decimal ActualPortionsProduced { get; set; }
        public decimal AveragePortionWeightGrams { get; set; }
        public string QualityApprovalStatus { get; set; } = "PASSED";
        public string ResponsibleChef { get; set; } = string.Empty;
        public string? IdempotencyKey { get; set; }
    }

    public class SilaPortionSampleRequestDto
    {
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? RecipeId { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public decimal StandardPortionWeightGrams { get; set; } = 200m;
        public List<decimal> SampledWeightsGrams { get; set; } = new();
        public decimal PlannedPortions { get; set; }
        public decimal ActualPortions { get; set; }
        public decimal TotalUsableOutputGrams { get; set; }
        public decimal StandardPortionCost { get; set; }
    }

    public class SilaPortionSampleDto
    {
        public Guid Id { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public int SampleCount { get; set; }
        public decimal StandardPortionWeightGrams { get; set; }
        public decimal AverageActualWeightGrams { get; set; }
        public decimal PortionWeightVariancePercent { get; set; }
        public decimal EstimatedPortionCostVariance { get; set; }
        public string SamplingBasisNote { get; set; } = string.Empty;
        public DateTime SampledAtUtc { get; set; }
    }

    public class SilaProductionReconciliationLineDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string InventoryBatchNumber { get; set; } = "DEFAULT";
        public string Uom { get; set; } = "KG";
        public decimal PlannedQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal PickedQty { get; set; }
        public decimal OriginalIssuedQty { get; set; }
        public decimal AdditionalIssuedQty { get; set; }
        public decimal ReversedIssueQty { get; set; }
        public decimal NetIssuedQty { get; set; }
        public decimal NetInboundTransferQty { get; set; }
        public decimal NetOutboundTransferQty { get; set; }
        public decimal UsableConsumptionQty { get; set; }
        public decimal SeparateWasteQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal RemainingWipQty { get; set; }
        public decimal AccountedQty { get; set; }
        public decimal UnexplainedQty { get; set; }
        public decimal TolerancePercent { get; set; }
        public string LineStatus { get; set; } = "BALANCED";
        public bool SettlementBlocked { get; set; }
        public decimal PlanningVolumeVarianceQty { get; set; }
        public decimal UsageVarianceQty { get; set; }
        public decimal UsageCostVariance { get; set; }
        public decimal PriceCostVariance { get; set; }
    }

    public class SilaProductionReconciliationDto
    {
        public Guid Id { get; set; }
        public string ReconciliationNumber { get; set; } = string.Empty;
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public string ReconciliationStatus { get; set; } = string.Empty;
        public string OperationalSettlementStatus { get; set; } = string.Empty;
        public string FinancialSettlementStatus { get; set; } = string.Empty;
        public bool SettlementBlocked { get; set; }
        public decimal PlannedProductionCost { get; set; }
        public decimal ActualDirectMaterialCost { get; set; }
        public decimal ActualDirectLaborCost { get; set; }
        public decimal ActualEquipmentUtilityPackagingCost { get; set; }
        public decimal AllocatedFixedOverhead { get; set; }
        public decimal FullyLoadedProductionCost { get; set; }
        public string CostStatus { get; set; } = "PROVISIONAL";
        public string CostMethodology { get; set; } = "WEIGHTED_AVERAGE";
        public int SettlementVersion { get; set; }
        public string CostVarianceBridgeJson { get; set; } = "{}";
        public string ValidationChecklistJson { get; set; } = "[]";
        public string FinishedProductBalanceJson { get; set; } = "{}";
        public List<SilaProductionReconciliationLineDto> Lines { get; set; } = new();
    }

    public class SilaLaborAllocationRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? ProductionTaskId { get; set; }
        public Guid? EmployeeUserId { get; set; }
        public string EmployeeReference { get; set; } = string.Empty;
        public string StaffClassification { get; set; } = "CHEF_DE_PARTIE";
        public decimal ActualHours { get; set; }
        public decimal ApplicableHourlyRate { get; set; }
        public decimal OvertimeCost { get; set; }
        public string AllocationMethod { get; set; } = "ACTUAL_TIME";
        public bool IsEstimated { get; set; }
    }

    public class SilaCentralKitchenCostAllocationRequestDto
    {
        public Guid SourceKitchenId { get; set; }
        public Guid ProductionBatchId { get; set; }
        public decimal TotalBatchProductionCost { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal RemainingApprovedStockQty { get; set; }
        public string AllocationMethod { get; set; } = "ACTUAL_COST_PROPORTIONAL";
        public List<SilaCkOutletSliceDto> OutletAllocations { get; set; } = new();
    }

    public class SilaCkOutletSliceDto
    {
        public Guid? ReceivingOutletId { get; set; }
        public string ReceivingOutletName { get; set; } = string.Empty;
        public decimal DistributedQty { get; set; }
        public bool ReceivingConfirmed { get; set; }
    }

    public class SilaBuffetReconciliationRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? EventId { get; set; }
        public string MealPeriod { get; set; } = "BREAKFAST";
        public int ExpectedCovers { get; set; }
        public int ActualCovers { get; set; }
        public decimal PlannedBuffetQty { get; set; }
        public decimal InitialProductionQty { get; set; }
        public decimal ReplenishmentQty { get; set; }
        public decimal TotalDispatchedQty { get; set; }
        public decimal ReturnedQty { get; set; }
        public decimal LeftoverQty { get; set; }
        public decimal WasteQty { get; set; }
        public decimal ActualIngredientConsumptionQty { get; set; }
        public decimal PlannedProductionCost { get; set; }
        public decimal ActualProductionCost { get; set; }
        public decimal WasteCost { get; set; }
    }

    public class SilaEventClosureRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public int ExpectedGuestCount { get; set; }
        public int ConfirmedGuestCount { get; set; }
        public int ActualGuestCount { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal OriginalBudgetCost { get; set; }
        public decimal RevisedBudgetCost { get; set; }
        public decimal ForecastCost { get; set; }
        public decimal NetRevenueActual { get; set; }
        public decimal DirectMaterialCostActual { get; set; }
        public decimal DirectLaborCostActual { get; set; }
        public decimal ExternalExpenseActual { get; set; }
        public decimal AllocatedOverheadActual { get; set; }
        public decimal PendingAccruedCost { get; set; }
        public string AuthoritativeRevenueSource { get; set; } = "OPERA_OR_POS_CONFIGURED";
        public bool AllServiceCompleted { get; set; } = true;
        public bool AllMaterialsReconciled { get; set; } = true;
        public bool AllInvoicesConfirmed { get; set; } = true;
        public string? PendingCostDescription { get; set; }
    }

    public class SilaReopenClosureRequestDto
    {
        public string ReasonCode { get; set; } = "LATE_SUPPLIER_INVOICE";
        public string Notes { get; set; } = string.Empty;
        public decimal AdjustmentAmount { get; set; }
        public string? SupportingEvidence { get; set; }
    }

    public class SilaPhase4BDashboardDto
    {
        public string Currency { get; set; } = "AED";
        public decimal PlannedProductionCost { get; set; }
        public decimal ActualProductionCost { get; set; }
        public decimal ProductionCostVariance { get; set; }
        public decimal TotalWasteCost { get; set; }
        public int PendingSettlementCount { get; set; }
        public int PendingEventClosureCount { get; set; }
        public int BalancedReconciliationCount { get; set; }
        public int BlockedReconciliationCount { get; set; }
        public int HistoricalSnapshotsReady { get; set; }
        public List<SilaProductionReconciliationDto> Reconciliations { get; set; } = new();
        public List<SilaProductionConsumptionDto> Consumptions { get; set; } = new();
        public List<SilaAdditionalMaterialRequestDto> AdditionalRequests { get; set; } = new();
        public List<SilaMaterialReturnDto> Returns { get; set; } = new();
        public List<SilaMaterialIssueReversalDto> Reversals { get; set; } = new();
        public List<SilaWasteEventDto> WasteEvents { get; set; } = new();
        public List<SilaFoodLeftoverDto> Leftovers { get; set; } = new();
        public List<SilaWasteDisposalDto> Disposals { get; set; } = new();
        public List<SilaPortionSampleDto> PortionSamples { get; set; } = new();
        public List<SilaCulinaryEventClosureSummaryDto> EventClosures { get; set; } = new();
        public List<SilaErpReconciliationRefDto> ErpReconciliations { get; set; } = new();
        public List<SilaHistoricalLearningSnapshotDto> HistoricalSnapshots { get; set; } = new();
        public SilaCulinaryReconciliationConfigDto Config { get; set; } = new();
    }

    public class SilaCulinaryEventClosureSummaryDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string ClosureNumber { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public int ExpectedGuestCount { get; set; }
        public int ConfirmedGuestCount { get; set; }
        public int ActualGuestCount { get; set; }
        public string Currency { get; set; } = "AED";
        public string OperationalStatus { get; set; } = string.Empty;
        public string FinancialStatus { get; set; } = string.Empty;
        public int SettlementVersion { get; set; }
        public decimal NetRevenueActual { get; set; }
        public decimal TotalVariableCostActual { get; set; }
        public decimal ContributionMarginActual { get; set; }
        public decimal FullyLoadedProfitActual { get; set; }
        public decimal PendingAccruedCost { get; set; }
        public string FourPerspectivePnlJson { get; set; } = "{}";
        public string ProfitabilityBridgeJson { get; set; } = "{}";
    }

    public class SilaErpReconciliationRefDto
    {
        public Guid Id { get; set; }
        public string SilaTransactionRef { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string ErpSystem { get; set; } = string.Empty;
        public string? ErpDocumentRef { get; set; }
        public string PostingStatus { get; set; } = string.Empty;
        public string ReconciliationStatus { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? PostedAtUtc { get; set; }
    }

    public class SilaHistoricalLearningSnapshotDto
    {
        public Guid Id { get; set; }
        public string SnapshotNumber { get; set; } = string.Empty;
        public string DatasetVersion { get; set; } = string.Empty;
        public string DataMaturity { get; set; } = string.Empty;
        public bool ValidatedForTraining { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string MealPeriod { get; set; } = string.Empty;
        public int ExpectedGuestCount { get; set; }
        public int ActualGuestCount { get; set; }
        public decimal PlannedFoodCost { get; set; }
        public decimal ActualFoodCost { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal ActualContributionMargin { get; set; }
        public decimal ActualFullyLoadedProfit { get; set; }
        public decimal TotalWasteCost { get; set; }
        public DateTime CapturedAtUtc { get; set; }
    }

    public class SilaReconciliationCopilotRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public bool CanViewFinancialCost { get; set; } = true;
    }

    public class SilaReconciliationCopilotResponseDto
    {
        public bool AccessDenied { get; set; }
        public string DirectAnswer { get; set; } = string.Empty;
        public string DataSource { get; set; } = "SILA Phase 4B Reconciliation Ledger";
        public string DateRange { get; set; } = string.Empty;
        public string CalculationBasis { get; set; } = "Deterministic Ledger + Variance Equations";
        public decimal? FinancialImpact { get; set; }
        public List<string> SupportingMetrics { get; set; } = new();
        public List<string> IdentifiedDataGaps { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public List<string> SupportingDocuments { get; set; } = new();
    }
}
