using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryReconciliationConfig : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [MaxLength(40)]
        public string ConsumptionMethod { get; set; } = "EXPLICIT"; // EXPLICIT | DERIVED | BACKFLUSH | ERP_CONFIRMED

        public bool BackflushRequiresApproval { get; set; } = true;

        [MaxLength(40)]
        public string InventoryValuationMethod { get; set; } = "WEIGHTED_AVERAGE"; // STANDARD | WEIGHTED_AVERAGE | ACTUAL_BATCH | ERP_CONFIRMED

        [Precision(6, 2)]
        public decimal MaterialReconciliationTolerancePercent { get; set; } = 2.0m;

        [Precision(18, 2)]
        public decimal WasteApprovalThresholdAmount { get; set; } = 250.0m;

        [Precision(6, 2)]
        public decimal YieldVarianceThresholdPercent { get; set; } = 5.0m;

        [Precision(6, 2)]
        public decimal PortionVarianceThresholdPercent { get; set; } = 5.0m;

        [Precision(6, 2)]
        public decimal FoodCostVarianceThresholdPercent { get; set; } = 3.0m;

        [MaxLength(40)]
        public string LaborAllocationMethod { get; set; } = "ACTUAL_TIME"; // ACTUAL_TIME | STANDARD_TIME | PRODUCTION_QTY | PORTIONS | GUEST_COUNT

        [MaxLength(40)]
        public string OverheadAllocationMethod { get; set; } = "PERCENT_OF_DIRECT_COST";

        [MaxLength(40)]
        public string CentralKitchenAllocationMethod { get; set; } = "ACTUAL_COST_PROPORTIONAL";

        public bool AllowProvisionalSettlement { get; set; } = true;

        public DateTime? FinancialPeriodLockedBeforeUtc { get; set; }

        [MaxLength(40)]
        public string ClosedPeriodAdjustmentPolicy { get; set; } = "CURRENT_OPEN_PERIOD_ACCRUAL";

        [MaxLength(40)]
        public string ErpPostingPolicy { get; set; } = "OUTBOX_CONFIRMED";

        [MaxLength(40)]
        public string InventorySystemOfRecord { get; set; } = "SILA_LEDGER";

        public int MaxLeftoverHoldingHours { get; set; } = 24;

        public bool RequireDisposalPhotoAboveThreshold { get; set; } = true;

        [MaxLength(10)]
        public string DefaultCurrency { get; set; } = "AED";

        public int ConfigVersion { get; set; } = 1;

        public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;

        public string ApprovalMatrixJson { get; set; } = "{}";

        public string ConfigHistoryJson { get; set; } = "[]";
    }

    public class SilaCulinaryProductionConsumption : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? RecipeId { get; set; }
        public Guid? SubRecipeId { get; set; }
        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ConsumptionNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [MaxLength(80)]
        public string InventoryBatchNumber { get; set; } = "DEFAULT";

        [Precision(18, 4)]
        public decimal PlannedQty { get; set; }

        [Precision(18, 4)]
        public decimal IssuedQty { get; set; }

        [Precision(18, 4)]
        public decimal ActualConsumedQty { get; set; }

        [MaxLength(20)]
        public string ConsumptionUom { get; set; } = "KG";

        [Required]
        [MaxLength(30)]
        public string ConsumptionMethod { get; set; } = "EXPLICIT"; // EXPLICIT | DERIVED | BACKFLUSH | ERP_CONFIRMED

        public bool IncludesWaste { get; set; }

        public bool IsSubstitution { get; set; }

        [MaxLength(60)]
        public string? SubstitutedFromMaterialCode { get; set; }

        [MaxLength(30)]
        public string CaptureMode { get; set; } = "MANUAL"; // MANUAL | BARCODE | QR | OFFLINE_SYNC | BACKFLUSH

        public bool IsReversal { get; set; }

        public Guid? ReversedConsumptionId { get; set; }

        [MaxLength(300)]
        public string? DeviationReason { get; set; }

        [MaxLength(300)]
        public string? SupportingEvidence { get; set; }

        [MaxLength(40)]
        public string ApprovalStatus { get; set; } = "APPROVED";

        [MaxLength(40)]
        public string SourceChannel { get; set; } = "CLOUD"; // CLOUD | MOBILE

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public Guid RecordedByUserId { get; set; }

        [MaxLength(160)]
        public string RecordedByName { get; set; } = string.Empty;

        public DateTime ConsumedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryAdditionalMaterialRequest : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(60)]
        public string RequestNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal RequestedQty { get; set; }

        [Precision(18, 4)]
        public decimal ApprovedQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Precision(18, 4)]
        public decimal AvailableStockSnapshot { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedCostImpact { get; set; }

        [Required]
        [MaxLength(60)]
        public string ReasonCode { get; set; } = "UNEXPECTED_DEMAND";

        [MaxLength(400)]
        public string? ReasonNotes { get; set; }

        [MaxLength(60)]
        public string? AlternativeMaterialCode { get; set; }

        public Guid? AlternativeStoreLocationId { get; set; }

        public bool EmergencyAuthorized { get; set; }

        [Required]
        [MaxLength(40)]
        public string ApprovalStatus { get; set; } = "PENDING"; // PENDING | APPROVED | PARTIALLY_APPROVED | REJECTED

        public bool InventoryIssued { get; set; }

        [MaxLength(80)]
        public string? InventoryIssueReference { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public Guid RequestedByUserId { get; set; }

        [MaxLength(160)]
        public string RequestedByName { get; set; } = string.Empty;

        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
    }

    public class SilaCulinaryMaterialReturn : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? OriginalMaterialIssueRefId { get; set; }
        public Guid? ReturningKitchenId { get; set; }
        public Guid? ReceivingStoreLocationId { get; set; }
        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ReturnNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [MaxLength(80)]
        public string InventoryBatchNumber { get; set; } = "DEFAULT";

        [Precision(18, 4)]
        public decimal ReturnedQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Required]
        [MaxLength(40)]
        public string ReturnClassification { get; set; } = "UNOPENED_USABLE"; // UNOPENED_USABLE | OPENED_USABLE | INSPECTION_REQUIRED | DAMAGED | EXPIRED | NON_RETURNABLE

        [MaxLength(120)]
        public string IngredientCondition { get; set; } = "GOOD";

        public DateTime? ExpiryUtc { get; set; }

        [MaxLength(60)]
        public string ReturnReason { get; set; } = "EXCESS_ISSUED";

        [MaxLength(40)]
        public string QualityInspectionStatus { get; set; } = "PENDING"; // PENDING | PASSED | FAILED

        [MaxLength(40)]
        public string RoutedStockBucket { get; set; } = "INSPECTION"; // AVAILABLE | INSPECTION | BLOCKED

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "PENDING_INSPECTION"; // DRAFT | PENDING_INSPECTION | PENDING_APPROVAL | APPROVED | POSTED | REJECTED | REVERSED

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public Guid ReturnedByUserId { get; set; }
        public Guid? ReceivedByUserId { get; set; }
        public DateTime ReturnedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryMaterialIssueReversal : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? OriginalMaterialIssueRefId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ReversalNumber { get; set; } = string.Empty;

        [MaxLength(60)]
        public string OriginalIssueNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [MaxLength(80)]
        public string InventoryBatchNumber { get; set; } = "DEFAULT";

        [Required]
        [MaxLength(40)]
        public string ReversalType { get; set; } = "INCORRECT_ISSUE"; // INCORRECT_ISSUE | DUPLICATE_ISSUE | INCORRECT_POSTING

        [Precision(18, 4)]
        public decimal ReversalQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [MaxLength(300)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(40)]
        public string ApprovalStatus { get; set; } = "APPROVED";

        [MaxLength(80)]
        public string? ErpReference { get; set; }

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "POSTED";

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public Guid ReversedByUserId { get; set; }
        public DateTime ReversedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryWasteEvent : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? MaterialId { get; set; }
        public Guid? RecipeId { get; set; }

        [Required]
        [MaxLength(60)]
        public string WasteCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string ItemDomain { get; set; } = "RAW_INGREDIENT"; // RAW_INGREDIENT | FINISHED_PRODUCT | PLATE_WASTE | PACKAGING

        public bool IsNormalRecipeYieldLoss { get; set; }

        public bool AlreadyDeductedFromInventory { get; set; }

        [Required]
        [MaxLength(60)]
        public string ItemCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [MaxLength(80)]
        public string InventoryBatchNumber { get; set; } = "DEFAULT";

        [Required]
        [MaxLength(60)]
        public string WasteCategory { get; set; } = "PRODUCTION_WASTE";

        [Precision(18, 4)]
        public decimal WasteQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Precision(18, 2)]
        public decimal EstimatedWasteCost { get; set; }

        [MaxLength(300)]
        public string WasteReason { get; set; } = string.Empty;

        [MaxLength(60)]
        public string DisposalMethod { get; set; } = "ORGANIC_BIN";

        [MaxLength(400)]
        public string? PhotoEvidenceRef { get; set; }

        [Required]
        [MaxLength(40)]
        public string ApprovalStatus { get; set; } = "APPROVED"; // PENDING | APPROVED | REJECTED

        public bool SecondDeductionPrevented { get; set; } = true;

        [MaxLength(40)]
        public string SourceChannel { get; set; } = "CLOUD";

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public Guid RecordedByUserId { get; set; }
        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryFoodLeftover : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? RecipeId { get; set; }

        [Required]
        [MaxLength(60)]
        public string LeftoverNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string SourceType { get; set; } = "BUFFET"; // BUFFET | EVENT | BANQUET | CENTRAL_KITCHEN | RESTAURANT | CATERING | PRODUCTION_SURPLUS

        [Required]
        [MaxLength(200)]
        public string FinishedProductName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal OriginalProductionQty { get; set; }

        [Precision(18, 4)]
        public decimal ServedQty { get; set; }

        [Precision(18, 4)]
        public decimal RemainingQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "PORTION";

        [MaxLength(160)]
        public string HoldingConditions { get; set; } = "CHILLED_BELOW_5C";

        public DateTime ProductionTimestampUtc { get; set; } = DateTime.UtcNow;

        public DateTime ApprovedUseByUtc { get; set; } = DateTime.UtcNow.AddHours(24);

        [MaxLength(160)]
        public string StorageConditions { get; set; } = "BLAST_CHILLED_COVERED";

        [MaxLength(200)]
        public string AllergenInfo { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string QualityStatus { get; set; } = "SAFE_VERIFIED"; // SAFE_VERIFIED | INSPECTION_REQUIRED | UNSAFE_HOLD

        public bool FoodSafetyApproved { get; set; }

        [Required]
        [MaxLength(40)]
        public string Disposition { get; set; } = "QUALITY_INSPECTION"; // SAFE_STORAGE | INTERNAL_TRANSFER | REDISTRIBUTION | QUALITY_INSPECTION | DISPOSAL | WASTE_RECORDING

        public string DispositionHistoryJson { get; set; } = "[]";
    }

    public class SilaCulinaryWasteDisposal : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? WasteEventId { get; set; }
        public Guid? ProductionBatchId { get; set; }

        [Required]
        [MaxLength(60)]
        public string DisposalNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal DisposalQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [MaxLength(200)]
        public string DisposalReason { get; set; } = string.Empty;

        [MaxLength(60)]
        public string DisposalMethod { get; set; } = "ORGANIC_COMPOST";

        [MaxLength(160)]
        public string DisposalLocation { get; set; } = "Main Waste Dock";

        public DateTime DisposedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(160)]
        public string ResponsibleEmployee { get; set; } = string.Empty;

        [MaxLength(40)]
        public string SupervisorApprovalStatus { get; set; } = "PENDING";

        [MaxLength(400)]
        public string? SupportingEvidenceRef { get; set; }

        [MaxLength(80)]
        public string? InventoryAdjustmentRef { get; set; }

        [Precision(18, 2)]
        public decimal FinancialWasteCost { get; set; }
    }

    public class SilaCulinaryProductionOutputConfirmation : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid ProductionOrderId { get; set; }
        public Guid ProductionBatchId { get; set; }
        public Guid? ParentBatchId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ConfirmationNumber { get; set; } = string.Empty;

        [MaxLength(40)]
        public string OutputType { get; set; } = "FINISHED"; // FINISHED | SUB_RECIPE | CO_PRODUCT | BY_PRODUCT | REWORK

        [Precision(18, 4)]
        public decimal PlannedOutputQty { get; set; }

        [Precision(18, 4)]
        public decimal ActualGrossOutputQty { get; set; }

        [Precision(18, 4)]
        public decimal AcceptedUsableOutputQty { get; set; }

        [Precision(18, 4)]
        public decimal RejectedOutputQty { get; set; }

        [Precision(18, 4)]
        public decimal ReworkQty { get; set; }

        [Precision(18, 4)]
        public decimal FinishedProductWasteQty { get; set; }

        [MaxLength(20)]
        public string OutputUom { get; set; } = "PORTION";

        [Precision(18, 2)]
        public decimal ActualPortionsProduced { get; set; }

        [Precision(18, 2)]
        public decimal AveragePortionWeightGrams { get; set; }

        [MaxLength(40)]
        public string QualityApprovalStatus { get; set; } = "PASSED";

        [MaxLength(160)]
        public string ResponsibleChef { get; set; } = string.Empty;

        public bool FinanciallySettled { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryPortionSample : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? RecipeId { get; set; }

        [MaxLength(60)]
        public string RecipeCode { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal StandardPortionWeightGrams { get; set; }

        public string SampledWeightsJson { get; set; } = "[]";

        public int SampleCount { get; set; }

        [Precision(18, 2)]
        public decimal AverageActualWeightGrams { get; set; }

        [Precision(8, 2)]
        public decimal PortionWeightVariancePercent { get; set; }

        [Precision(18, 2)]
        public decimal PlannedPortions { get; set; }

        [Precision(18, 2)]
        public decimal ActualPortions { get; set; }

        [Precision(18, 4)]
        public decimal TotalUsableOutputGrams { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedPortionCostVariance { get; set; }

        [MaxLength(300)]
        public string SamplingBasisNote { get; set; } = string.Empty;

        public DateTime SampledAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryProductionReconciliation : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? EventId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ReconciliationNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string ReconciliationStatus { get; set; } = "RECONCILIATION_IN_PROGRESS"; // BALANCED | WITHIN_TOLERANCE | QUANTITY_MISMATCH | MISSING_TRANSACTION | DUPLICATE_TRANSACTION_SUSPECTED | PENDING_RETURN | PENDING_WASTE_APPROVAL | PENDING_ERP_CONFIRMATION | REQUIRES_INVESTIGATION

        [MaxLength(40)]
        public string OperationalSettlementStatus { get; set; } = "NOT_READY";

        [MaxLength(40)]
        public string FinancialSettlementStatus { get; set; } = "NOT_READY";

        public bool SettlementBlocked { get; set; }

        [Precision(18, 2)]
        public decimal PlannedProductionCost { get; set; }

        [Precision(18, 2)]
        public decimal ActualDirectMaterialCost { get; set; }

        [Precision(18, 2)]
        public decimal ActualDirectLaborCost { get; set; }

        [Precision(18, 2)]
        public decimal ActualEquipmentUtilityPackagingCost { get; set; }

        [Precision(18, 2)]
        public decimal AllocatedFixedOverhead { get; set; }

        [Precision(18, 2)]
        public decimal FullyLoadedProductionCost { get; set; }

        [MaxLength(30)]
        public string CostStatus { get; set; } = "PROVISIONAL"; // PROVISIONAL | FINAL

        [MaxLength(40)]
        public string CostMethodology { get; set; } = "WEIGHTED_AVERAGE";

        public int SettlementVersion { get; set; } = 1;

        public string CostVarianceBridgeJson { get; set; } = "{}";
        public string ValidationChecklistJson { get; set; } = "[]";
        public string FinishedProductBalanceJson { get; set; } = "{}";

        public DateTime ReconciledAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? SettledAtUtc { get; set; }
    }

    public class SilaCulinaryProductionReconciliationLine : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid ReconciliationId { get; set; }
        public Guid BuyerId { get; set; }

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string MaterialName { get; set; } = string.Empty;

        [MaxLength(80)]
        public string InventoryBatchNumber { get; set; } = "DEFAULT";

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Precision(18, 4)] public decimal PlannedQty { get; set; }
        [Precision(18, 4)] public decimal ReservedQty { get; set; }
        [Precision(18, 4)] public decimal PickedQty { get; set; }
        [Precision(18, 4)] public decimal OriginalIssuedQty { get; set; }
        [Precision(18, 4)] public decimal AdditionalIssuedQty { get; set; }
        [Precision(18, 4)] public decimal ReversedIssueQty { get; set; }
        [Precision(18, 4)] public decimal NetIssuedQty { get; set; }
        [Precision(18, 4)] public decimal NetInboundTransferQty { get; set; }
        [Precision(18, 4)] public decimal NetOutboundTransferQty { get; set; }
        [Precision(18, 4)] public decimal UsableConsumptionQty { get; set; }
        [Precision(18, 4)] public decimal SeparateWasteQty { get; set; }
        [Precision(18, 4)] public decimal ReturnedQty { get; set; }
        [Precision(18, 4)] public decimal RemainingWipQty { get; set; }
        [Precision(18, 4)] public decimal AccountedQty { get; set; }
        [Precision(18, 4)] public decimal UnexplainedQty { get; set; }
        [Precision(8, 4)] public decimal TolerancePercent { get; set; } = 2.0m;

        [MaxLength(50)]
        public string LineStatus { get; set; } = "BALANCED";

        public bool SettlementBlocked { get; set; }

        [Precision(18, 4)] public decimal PlanningVolumeVarianceQty { get; set; }
        [Precision(18, 4)] public decimal UsageVarianceQty { get; set; }
        [Precision(18, 2)] public decimal UsageCostVariance { get; set; }
        [Precision(18, 2)] public decimal PriceCostVariance { get; set; }

        public string SourceTransactionsJson { get; set; } = "[]";
    }

    public class SilaCulinaryLaborAllocation : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? ProductionTaskId { get; set; }
        public Guid? EmployeeUserId { get; set; }

        [MaxLength(120)]
        public string EmployeeReference { get; set; } = string.Empty;

        [MaxLength(60)]
        public string StaffClassification { get; set; } = "CHEF_DE_PARTIE";

        [Precision(10, 2)]
        public decimal ActualHours { get; set; }

        [Precision(18, 2)]
        public decimal ApplicableHourlyRate { get; set; }

        [Precision(18, 2)]
        public decimal OvertimeCost { get; set; }

        [Precision(18, 2)]
        public decimal AllocatedLaborCost { get; set; }

        [MaxLength(40)]
        public string AllocationMethod { get; set; } = "ACTUAL_TIME";

        public bool IsEstimated { get; set; }
    }

    public class SilaCulinaryCentralKitchenAllocation : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid SourceKitchenId { get; set; }
        public Guid ProductionBatchId { get; set; }
        public Guid? ReceivingOutletId { get; set; }

        [MaxLength(160)]
        public string ReceivingOutletName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal DistributedQty { get; set; }

        [Precision(18, 4)]
        public decimal RemainingApprovedStockQty { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Precision(18, 2)]
        public decimal TotalBatchProductionCost { get; set; }

        [Precision(18, 2)]
        public decimal AllocatedProductionCost { get; set; }

        [Precision(18, 2)]
        public decimal InternalTransferValuation { get; set; }

        public bool TreatedAsExternalRevenue { get; set; } = false;

        public bool ReceivingConfirmed { get; set; }

        [MaxLength(40)]
        public string AllocationMethod { get; set; } = "ACTUAL_COST_PROPORTIONAL";
    }

    public class SilaCulinaryBuffetReconciliation : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? EventId { get; set; }

        [Required]
        [MaxLength(40)]
        public string MealPeriod { get; set; } = "BREAKFAST"; // BREAKFAST | LUNCH | DINNER | BRUNCH | SPECIAL_BUFFET | EVENT_BUFFET

        public DateTime ServiceDateUtc { get; set; } = DateTime.UtcNow;

        public int ExpectedCovers { get; set; }
        public int ActualCovers { get; set; }

        [Precision(18, 4)] public decimal PlannedBuffetQty { get; set; }
        [Precision(18, 4)] public decimal InitialProductionQty { get; set; }
        [Precision(18, 4)] public decimal ReplenishmentQty { get; set; }
        [Precision(18, 4)] public decimal TotalProductionQty { get; set; }
        [Precision(18, 4)] public decimal TotalDispatchedQty { get; set; }
        [Precision(18, 4)] public decimal EstimatedServedQty { get; set; }
        [Precision(18, 4)] public decimal ReturnedQty { get; set; }
        [Precision(18, 4)] public decimal LeftoverQty { get; set; }
        [Precision(18, 4)] public decimal WasteQty { get; set; }
        [Precision(18, 4)] public decimal ActualIngredientConsumptionQty { get; set; }
        [Precision(18, 2)] public decimal PlannedProductionCost { get; set; }
        [Precision(18, 2)] public decimal ActualProductionCost { get; set; }
        [Precision(18, 2)] public decimal WasteCost { get; set; }
    }

    public class SilaCulinaryEventClosure : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }
        public Guid EventId { get; set; }

        [Required]
        [MaxLength(60)]
        public string ClosureNumber { get; set; } = string.Empty;

        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        public int ExpectedGuestCount { get; set; }
        public int ConfirmedGuestCount { get; set; }
        public int ActualGuestCount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Required]
        [MaxLength(40)]
        public string OperationalStatus { get; set; } = "IN_EXECUTION"; // PLANNED | CONFIRMED | IN_PREPARATION | IN_EXECUTION | SERVICE_COMPLETED | OPERATIONAL_RECONCILIATION | OPERATIONALLY_CLOSED | REOPENED

        [Required]
        [MaxLength(40)]
        public string FinancialStatus { get; set; } = "NOT_STARTED"; // NOT_STARTED | FINANCIAL_RECONCILIATION | PROVISIONALLY_CLOSED | FINANCIALLY_CLOSED | REOPENED

        public int SettlementVersion { get; set; } = 1;

        [Precision(18, 2)] public decimal NetRevenueActual { get; set; }
        [Precision(18, 2)] public decimal DirectMaterialCostActual { get; set; }
        [Precision(18, 2)] public decimal DirectLaborCostActual { get; set; }
        [Precision(18, 2)] public decimal ExternalExpenseActual { get; set; }
        [Precision(18, 2)] public decimal TotalVariableCostActual { get; set; }
        [Precision(18, 2)] public decimal ContributionMarginActual { get; set; }
        [Precision(18, 2)] public decimal AllocatedOverheadActual { get; set; }
        [Precision(18, 2)] public decimal FullyLoadedProfitActual { get; set; }
        [Precision(18, 2)] public decimal PendingAccruedCost { get; set; }

        [MaxLength(40)]
        public string AuthoritativeRevenueSource { get; set; } = "OPERA_OR_POS_CONFIGURED";

        public string FourPerspectivePnlJson { get; set; } = "{}";
        public string ProfitabilityBridgeJson { get; set; } = "{}";
        public string PendingCostsJson { get; set; } = "[]";
        public string OperationalChecklistJson { get; set; } = "[]";
        public string FinancialChecklistJson { get; set; } = "[]";
        public string ClosureHistoryJson { get; set; } = "[]";
        public string ReopeningAdjustmentsJson { get; set; } = "[]";
    }

    public class SilaCulinaryErpReconciliationRef : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(80)]
        public string SilaTransactionRef { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string TransactionType { get; set; } = "MATERIAL_ISSUE"; // MATERIAL_ISSUE | ISSUE_REVERSAL | MATERIAL_RETURN | STOCK_TRANSFER | PRODUCTION_CONSUMPTION | FINISHED_OUTPUT | WASTE_ADJUSTMENT | SUPPLIER_INVOICE | CREDIT_MEMO | EVENT_REVENUE | ALLOCATION

        [MaxLength(40)]
        public string ErpSystem { get; set; } = "SAP_S4HANA";

        [MaxLength(80)]
        public string? ErpDocumentRef { get; set; }

        [MaxLength(100)]
        public string? IntegrationMessageId { get; set; }

        [Required]
        [MaxLength(40)]
        public string PostingStatus { get; set; } = "PENDING"; // PENDING | CONFIRMED | FAILED_RETRY | REJECTED

        public DateTime? PostedAtUtc { get; set; }
        public int RetryCount { get; set; }

        [MaxLength(400)]
        public string? ErrorMessage { get; set; }

        [MaxLength(40)]
        public string ReconciliationStatus { get; set; } = "PENDING_CONFIRMATION"; // MATCHED | PENDING_CONFIRMATION | MISMATCH | FAILED

        [Required]
        [MaxLength(120)]
        public string IdempotencyKey { get; set; } = string.Empty;
    }

    public class SilaCulinaryHistoricalLearningSnapshot : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string SnapshotNumber { get; set; } = string.Empty;

        [MaxLength(40)]
        public string DatasetVersion { get; set; } = "DS-4B-1.0";

        [Required]
        [MaxLength(30)]
        public string DataMaturity { get; set; } = "FINAL_RECONCILED"; // FORECAST | PROVISIONAL_ACTUAL | FINAL_RECONCILED

        public bool ValidatedForTraining { get; set; }

        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? EventId { get; set; }

        [MaxLength(60)]
        public string EventType { get; set; } = "BANQUET";

        [MaxLength(30)]
        public string Season { get; set; } = "WINTER";

        [MaxLength(20)]
        public string DayOfWeekName { get; set; } = "FRIDAY";

        [MaxLength(40)]
        public string MealPeriod { get; set; } = "DINNER";

        public int ExpectedGuestCount { get; set; }
        public int ConfirmedGuestCount { get; set; }
        public int ActualGuestCount { get; set; }

        [Precision(18, 2)] public decimal PlannedFoodCost { get; set; }
        [Precision(18, 2)] public decimal ActualFoodCost { get; set; }
        [Precision(18, 2)] public decimal PlannedLaborCost { get; set; }
        [Precision(18, 2)] public decimal ActualLaborCost { get; set; }
        [Precision(18, 2)] public decimal ActualRevenue { get; set; }
        [Precision(18, 2)] public decimal ActualContributionMargin { get; set; }
        [Precision(18, 2)] public decimal ActualFullyLoadedProfit { get; set; }
        [Precision(18, 4)] public decimal TotalWasteQty { get; set; }
        [Precision(18, 2)] public decimal TotalWasteCost { get; set; }
        [Precision(8, 2)] public decimal ActualYieldPercent { get; set; }
        [Precision(8, 2)] public decimal PortionVariancePercent { get; set; }

        public string SnapshotPayloadJson { get; set; } = "{}";

        public DateTime CapturedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryReconciliationAudit : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(60)]
        public string AuditType { get; set; } = string.Empty;

        public Guid ActorUserId { get; set; }

        [MaxLength(160)]
        public string ActorName { get; set; } = string.Empty;

        [MaxLength(400)]
        public string Summary { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = "{}";

        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
