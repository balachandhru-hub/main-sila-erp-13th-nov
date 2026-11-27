using System;
using System.Collections.Generic;

namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryProcurementDashboardDto
    {
        public int TotalPlans { get; set; }
        public int UpcomingEventRequirements { get; set; }
        public int ActiveReservationsCount { get; set; }
        public int MaterialsAwaitingTransfer { get; set; }
        public int MaterialsAwaitingPurchase { get; set; }
        public int PendingPoApprovals { get; set; }
        public int OpenLinkedPos { get; set; }
        public int OverdueReceipts { get; set; }
        public decimal AveragePhysicalReadinessPercent { get; set; }
        public decimal AverageProjectedReadinessPercent { get; set; }
        public bool CostsVisible { get; set; }
        public decimal? EstimatedNewPurchaseValue { get; set; }
        public decimal? CommittedProcurementValue { get; set; }
        public int OpenExceptionsCount { get; set; }
        public List<SilaCulinaryProcurementPlanSummaryDto> Plans { get; set; } = new();
    }

    public class SilaCulinaryProcurementPlanSummaryDto
    {
        public Guid Id { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid MrpRunId { get; set; }
        public string MrpCode { get; set; } = string.Empty;
        public int MrpVersion { get; set; }
        public Guid? PrimaryEventId { get; set; }
        public string? PrimaryEventCode { get; set; }
        public string? PrimaryEventName { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string Currency { get; set; } = "AED";
        public string Status { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public bool IsSimulation { get; set; }
        public DateTime? EventDateUtc { get; set; }
        public DateTime? PreparationDeadlineUtc { get; set; }
        public int PlannedGuests { get; set; }
        public string? ResponsibleChef { get; set; }
        public string? VenueName { get; set; }
        public int RequiredMaterialsCount { get; set; }
        public int FullyReservedCount { get; set; }
        public int IncomingPoCoveredCount { get; set; }
        public int TransferCoveredCount { get; set; }
        public int PurchaseRequiredCount { get; set; }
        public int CriticalShortageCount { get; set; }
        public int DelayedDeliveryCount { get; set; }
        public int PendingApprovalCount { get; set; }
        public decimal? EstimatedNewPurchaseValue { get; set; }
        public decimal? CommittedProcurementValue { get; set; }
        public decimal PhysicalReadinessPercent { get; set; }
        public decimal ProjectedReadinessPercent { get; set; }
        public DateTime CalculatedAtUtc { get; set; }
    }

    public class SilaCulinaryProcurementPlanDetailDto
    {
        public SilaCulinaryProcurementPlanSummaryDto Summary { get; set; } = new();
        public bool CostsVisible { get; set; }
        public List<string> CoveragePriority { get; set; } = new();
        public SilaCulinaryProcurementPurchasingProfileDto? PurchasingProfile { get; set; }
        public List<SilaCulinaryProcurementLineDto> Lines { get; set; } = new();
        public List<SilaCulinaryStockReservationDto> Reservations { get; set; } = new();
        public List<SilaCulinaryProcurementAllocationDto> Allocations { get; set; } = new();
        public List<SilaCulinaryProcurementConsolidatedPurchaseDto> ConsolidatedPurchases { get; set; } = new();
        public List<SilaCulinaryProcurementExceptionDto> Exceptions { get; set; } = new();
        public List<SilaCulinaryProcurementScenarioDto> Scenarios { get; set; } = new();
        public List<SilaCulinaryProcurementLedgerEntryDto> SupplyDemandLedger { get; set; } = new();
        public List<SilaCulinaryProcurementApprovalStepDto> ApprovalSteps { get; set; } = new();
        public List<SilaCulinaryProcurementHistoryItemDto> History { get; set; } = new();
    }

    public class SilaCulinaryProcurementPurchasingProfileDto
    {
        public Guid? ProfileId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? ProfitCenter { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public string? DocumentType { get; set; }
        public string? ItemCategory { get; set; }
        public string? Currency { get; set; }
        public bool IsComplete { get; set; }
        public string? ValidationMessage { get; set; }
    }

    public class SilaCulinaryProcurementLineDto
    {
        public Guid Id { get; set; }
        public Guid? EventId { get; set; }
        public string? EventCode { get; set; }
        public string? EventName { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string MaterialDescription { get; set; } = string.Empty;
        public string MaterialCategory { get; set; } = string.Empty;
        public string? MaterialImageUrl { get; set; }
        public string BaseUom { get; set; } = "KG";
        public string? PurchasingUom { get; set; }
        public Guid? DestinationLocationId { get; set; }
        public string? DestinationKitchenName { get; set; }
        public string? DestinationStorageLocation { get; set; }
        public DateTime RequiredByUtc { get; set; }
        public decimal GrossRequirementQty { get; set; }
        public decimal? LocalOnHandQty { get; set; }
        public decimal CompetingReservationsQty { get; set; }
        public decimal SafetyStockQty { get; set; }
        public decimal? EligibleLocalStockQty { get; set; }
        public decimal ActiveEventReservationQty { get; set; }
        public decimal ProposedReservationQty { get; set; }
        public decimal PreparedStockCoverageQty { get; set; }
        public decimal IncomingPoAllocatedQty { get; set; }
        public decimal IncomingTransferAllocatedQty { get; set; }
        public decimal ProposedItoQty { get; set; }
        public decimal ConfirmedCoverageQty { get; set; }
        public decimal RemainingConfirmedGapQty { get; set; }
        public decimal NetUncoveredQty { get; set; }
        public decimal RecommendedPurchaseQty { get; set; }
        public decimal ExpectedExcessQty { get; set; }
        public decimal? PackSizeQty { get; set; }
        public decimal? MoqQty { get; set; }
        public int? NumberOfPacks { get; set; }
        public Guid? RecommendedSupplierId { get; set; }
        public string? RecommendedSupplierCode { get; set; }
        public string? RecommendedSupplierName { get; set; }
        public string? ContractReference { get; set; }
        public decimal? SupplierUnitPrice { get; set; }
        public decimal? EstimatedPurchaseValue { get; set; }
        public string Currency { get; set; } = "AED";
        public DateTime? ExpectedDeliveryDateUtc { get; set; }
        public DateTime? LatestOrderReleaseDateUtc { get; set; }
        public bool IsDeliveryFeasible { get; set; }
        public string CoverageStatus { get; set; } = string.Empty;
        public string ReadinessStatus { get; set; } = string.Empty;
        public bool WeeklyBucketLinked { get; set; }
        public Guid? WeeklyBucketItemId { get; set; }
        public List<SilaCulinaryProcurementSupplierComparisonDto> SupplierComparisons { get; set; } = new();
        public List<SilaCulinaryProcurementFefoBatchDto> FefoBatches { get; set; } = new();
        public List<SilaCulinaryProcurementItoCandidateDto> ItoCandidates { get; set; } = new();
    }

    public class SilaCulinaryProcurementSupplierComparisonDto
    {
        public Guid? SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public string? ContractReference { get; set; }
        public bool ContractMandatory { get; set; }
        public string PurchasingUom { get; set; } = "KG";
        public decimal? UnitPrice { get; set; }
        public decimal? BaseUnitPrice { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal PackSizeQty { get; set; } = 1;
        public decimal MoqQty { get; set; }
        public int LeadTimeDays { get; set; }
        public DateTime? EstimatedDeliveryDateUtc { get; set; }
        public DateTime? LatestOrderReleaseDateUtc { get; set; }
        public decimal RecommendedPurchaseQty { get; set; }
        public decimal ExpectedExcessQty { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public bool DeliveryFeasible { get; set; }
        public bool IsSelected { get; set; }
    }

    public class SilaCulinaryProcurementFefoBatchDto
    {
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDateUtc { get; set; }
        public int? RemainingShelfLifeDays { get; set; }
        public decimal AvailableQty { get; set; }
        public decimal RecommendedQty { get; set; }
        public string QualityStatus { get; set; } = "ELIGIBLE";
        public string? LocationName { get; set; }
        public string? StorageLocationCode { get; set; }
    }

    public class SilaCulinaryProcurementItoCandidateDto
    {
        public Guid SourceLocationId { get; set; }
        public string SourceLocationName { get; set; } = string.Empty;
        public string? SourceStorageLocation { get; set; }
        public string? SourcePlantCode { get; set; }
        public string SourceType { get; set; } = string.Empty;
        public decimal AvailableQty { get; set; }
        public decimal SafetyStockQty { get; set; }
        public decimal CompetingReservationsQty { get; set; }
        public decimal RecommendedTransferQty { get; set; }
        public bool SamePlant311Eligible { get; set; }
        public string? IneligibilityReason { get; set; }
    }

    public class SilaCulinaryStockReservationDto
    {
        public Guid Id { get; set; }
        public string ReservationCode { get; set; } = string.Empty;
        public Guid ProcurementPlanId { get; set; }
        public Guid? ProcurementLineId { get; set; }
        public Guid EventId { get; set; }
        public int EventRevision { get; set; }
        public Guid MrpRunId { get; set; }
        public int MrpVersion { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public Guid LocationId { get; set; }
        public string? StorageLocationCode { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? BatchExpiryUtc { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal FulfilledQty { get; set; }
        public decimal ReleasedQty { get; set; }
        public string BaseUom { get; set; } = "KG";
        public DateTime RequiredByUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsPreparedStock { get; set; }
        public bool SafetyStockOverrideUsed { get; set; }
        public string? SafetyOverrideReason { get; set; }
        public int PriorityRank { get; set; }
        public string? ApprovalReference { get; set; }
    }

    public class SilaCulinaryProcurementAllocationDto
    {
        public Guid Id { get; set; }
        public Guid ProcurementPlanId { get; set; }
        public Guid? ProcurementLineId { get; set; }
        public Guid EventId { get; set; }
        public Guid MrpRunId { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string MaterialDescription { get; set; } = string.Empty;
        public string AllocationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? SourceLocationId { get; set; }
        public string? SourceLocationName { get; set; }
        public Guid? DestinationLocationId { get; set; }
        public string? DestinationLocationName { get; set; }
        public Guid? PurchaseOrderId { get; set; }
        public Guid? PurchaseOrderItemId { get; set; }
        public string? PoNumber { get; set; }
        public int? PoItemNumber { get; set; }
        public Guid? InternalTransferOrderId { get; set; }
        public string? ItoNumber { get; set; }
        public Guid? GoodsReceiptId { get; set; }
        public string? GrnNumber { get; set; }
        public string? SapMaterialDocument { get; set; }
        public string? ReversalMaterialDocument { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? ContractReference { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal ReversedQty { get; set; }
        public decimal RemainingQty { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal? UnitPrice { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string Currency { get; set; } = "AED";
        public DateTime? ExpectedDateUtc { get; set; }
        public string? Notes { get; set; }
    }

    public class SilaCulinaryProcurementConsolidatedPurchaseDto
    {
        public string ConsolidationKey { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public Guid? SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string BaseUom { get; set; } = "KG";
        public decimal CombinedNetQty { get; set; }
        public decimal RecommendedPurchaseQty { get; set; }
        public decimal ExpectedExcessQty { get; set; }
        public decimal? EstimatedValue { get; set; }
        public string Currency { get; set; } = "AED";
        public List<SilaCulinaryConsolidatedEventShareDto> EventShares { get; set; } = new();
    }

    public class SilaCulinaryConsolidatedEventShareDto
    {
        public Guid EventId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public decimal AllocatedQty { get; set; }
    }

    public class SilaCulinaryProcurementExceptionDto
    {
        public Guid Id { get; set; }
        public Guid ProcurementPlanId { get; set; }
        public Guid? ProcurementLineId { get; set; }
        public string? EventCode { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialDescription { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public decimal QuantityImpact { get; set; }
        public DateTime? RequiredDateUtc { get; set; }
        public string? ResponsibleRole { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public string ResolutionStatus { get; set; } = string.Empty;
        public string? ResolutionNote { get; set; }
    }

    public class SilaCulinaryProcurementScenarioDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ScenarioType { get; set; } = string.Empty;
        public decimal? EstimatedPurchaseValue { get; set; }
        public decimal ProjectedReadinessPercent { get; set; }
        public string ParametersJson { get; set; } = "{}";
        public string ResultsJson { get; set; } = "{}";
        public DateTime DateCreated { get; set; }
    }

    public class SilaCulinaryProcurementLedgerEntryDto
    {
        public string TimestampUtc { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string SourceReference { get; set; } = string.Empty;
        public decimal DeltaQty { get; set; }
        public decimal RunningProjectedQty { get; set; }
        public bool Confirmed { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class SilaCulinaryProcurementApprovalStepDto
    {
        public int StepOrder { get; set; }
        public Guid ApproverUserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime? DecidedOn { get; set; }
    }

    public class SilaCulinaryProcurementHistoryItemDto
    {
        public string Action { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
    }

    public class SilaCulinaryCreateProcurementPlanRequestDto
    {
        public Guid MrpRunId { get; set; }
        public string? Name { get; set; }
        public bool IsSimulation { get; set; }
        public bool IncludeProposedItoInNet { get; set; } = true;
        public bool AllowSafetyStockOverride { get; set; }
        public string? IdempotencyKey { get; set; }
        public List<string>? CoveragePriority { get; set; }
    }

    public class SilaCulinaryCreateReservationRequestDto
    {
        public Guid ProcurementLineId { get; set; }
        public Guid LocationId { get; set; }
        public decimal Quantity { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? BatchExpiryUtc { get; set; }
        public bool ActivateImmediately { get; set; } = true;
        public bool AllowSafetyStockOverride { get; set; }
        public string? SafetyOverrideReason { get; set; }
        public int PriorityRank { get; set; } = 100;
        public string? IdempotencyKey { get; set; }
    }

    public class SilaCulinaryAllocateIncomingPoRequestDto
    {
        public Guid ProcurementLineId { get; set; }
        public Guid PurchaseOrderItemId { get; set; }
        public decimal Quantity { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SilaCulinaryCreateItoDraftRequestDto
    {
        public Guid ProcurementLineId { get; set; }
        public Guid FromLocationId { get; set; }
        public Guid ToLocationId { get; set; }
        public decimal Quantity { get; set; }
        public DateTime? RequiredByUtc { get; set; }
        public string? Reason { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SilaCulinaryCreatePoDraftRequestDto
    {
        public List<Guid> ProcurementLineIds { get; set; } = new();
        public Guid SupplierId { get; set; }
        public DateTime? DeliveryDateUtc { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public string? ProfitCenter { get; set; }
        public string? ContractReference { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SilaCulinaryReconcileGrnRequestDto
    {
        public Guid GoodsReceiptId { get; set; }
        public bool IsReversal { get; set; }
        public string? ReversalMaterialDocument { get; set; }
    }

    public class SilaCulinaryReconcileItoReversalRequestDto
    {
        public Guid InternalTransferOrderId { get; set; }
        public decimal ReversedQty { get; set; }
        public string? ReversalMaterialDocument { get; set; }
        public string? Reason { get; set; }
    }

    public class SilaCulinarySimulateProcurementRequestDto
    {
        public decimal GuestScaleFactor { get; set; } = 1.0m;
        public decimal? LocalStockOverrideQty { get; set; }
        public decimal? SafetyStockOverrideQty { get; set; }
        public decimal? CompetingReservationOverrideQty { get; set; }
        public decimal? IncomingPoQtyOverride { get; set; }
        public int? IncomingPoDelayDays { get; set; }
        public decimal? TransferableStockOverrideQty { get; set; }
        public int? TransferLeadTimeDays { get; set; }
        public decimal? SupplierPriceOverride { get; set; }
        public decimal? PackSizeOverride { get; set; }
        public decimal? MoqOverride { get; set; }
        public int? SupplierLeadTimeDaysOverride { get; set; }
        public string? ScenarioType { get; set; }
        public string? ScenarioName { get; set; }
        public bool SaveScenario { get; set; }
    }

    public class SilaCulinarySimulateProcurementResultDto
    {
        public decimal GrossRequirementQty { get; set; }
        public decimal EligibleStockCoverageQty { get; set; }
        public decimal ExpectedReceiptCoverageQty { get; set; }
        public decimal TransferCoverageQty { get; set; }
        public decimal NetPurchaseRequirementQty { get; set; }
        public decimal RecommendedPurchaseQty { get; set; }
        public int NumberOfPacks { get; set; }
        public decimal ExpectedExcessQty { get; set; }
        public decimal? EstimatedPurchaseCost { get; set; }
        public bool DeliveryFeasible { get; set; }
        public decimal ReadinessImpactPercent { get; set; }
        public Guid? SavedScenarioId { get; set; }
    }

    public class SilaCulinaryProcurementOptimizeResultDto
    {
        public bool Feasible { get; set; }
        public string SolverStatus { get; set; } = string.Empty;
        public string SolverName { get; set; } = string.Empty;
        public decimal? TotalPurchaseCost { get; set; }
        public decimal TotalExcessQty { get; set; }
        public List<string> InfeasibilityReasons { get; set; } = new();
        public List<SilaCulinaryProcurementConsolidatedPurchaseDto> SelectedPurchases { get; set; } = new();
    }

    public class SilaCulinaryProcurementAiAnalyzeResponseDto
    {
        public string ProviderUsed { get; set; } = "LOCAL";
        public string Answer { get; set; } = string.Empty;
        public List<string> ReferencedMaterials { get; set; } = new();
        public List<string> ReferencedSuppliers { get; set; } = new();
        public bool AdvisoryOnly { get; set; } = true;
    }

    public class SilaCulinaryProcurementReportCatalogItemDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    public class SilaCulinaryProcurementReportResultDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Currency { get; set; } = "AED";
        public DateTime GeneratedAtUtc { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<List<string>> PreviewRows { get; set; } = new();
        public string CsvContent { get; set; } = string.Empty;
    }

    /// <summary>§66 Phase 3A Food Cost and Event P&L Handover Contract.</summary>
    public class SilaCulinaryPhase3AHandoverDto
    {
        public string ContractVersion { get; set; } = "3A.1.0";
        public Guid ProcurementPlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public Guid MrpRunId { get; set; }
        public string MrpCode { get; set; } = string.Empty;
        public int MrpVersion { get; set; }
        public Guid? EventId { get; set; }
        public string? EventCode { get; set; }
        public string? EventName { get; set; }
        public int EventRevision { get; set; }
        public Guid? PropertyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? KitchenName { get; set; }
        public DateTime? EventDateUtc { get; set; }
        public int PlannedGuests { get; set; }
        public int GuaranteedGuests { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal? EstimatedMaterialRequirementCost { get; set; }
        public decimal? EstimatedNewPurchaseExpenditure { get; set; }
        public decimal? ApprovedProcurementCommitment { get; set; }
        public decimal? ActualReceivedMaterialValue { get; set; }
        public decimal? FutureActualConsumptionCost { get; set; }
        public string Note { get; set; } = "PO expenditure and GRN value are distinct from actual event consumption cost (reconciled in Phase 3A).";
        public List<SilaCulinaryPhase3AMaterialHandoverDto> Materials { get; set; } = new();
    }

    public class SilaCulinaryPhase3AMaterialHandoverDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public decimal GrossRequirement { get; set; }
        public decimal ExpectedConsumptionQty { get; set; }
        public string BaseUom { get; set; } = "KG";
        public decimal? EstimatedMaterialCost { get; set; }
        public List<string> ReservationReferences { get; set; } = new();
        public List<string> IncomingPoAllocations { get; set; } = new();
        public List<string> ItoReferences { get; set; } = new();
        public List<string> PoReferences { get; set; } = new();
        public List<string> GrnReferences { get; set; } = new();
        public decimal OrderedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal OutstandingCommitmentQty { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
    }
}
