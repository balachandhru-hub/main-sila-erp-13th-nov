namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryKitchenDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string KitchenCode { get; set; } = string.Empty;
        public string KitchenName { get; set; } = string.Empty;
        public string KitchenType { get; set; } = "MAIN_KITCHEN";
        public Guid? AssociatedStoreLocationId { get; set; }
        public string? ResponsibleExecutiveChef { get; set; }
        public string? KitchenSupervisor { get; set; }
        public decimal DailyPortionCapacity { get; set; }
        public List<Guid> OutletIds { get; set; } = new();
        public List<string> SupportedMealPeriods { get; set; } = new();
        public List<string> SupportedCategories { get; set; } = new();
        public DateTime EffectiveFromUtc { get; set; }
        public DateTime? EffectiveToUtc { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SilaCulinaryUpsertKitchenRequestDto
    {
        public Guid? Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string KitchenCode { get; set; } = string.Empty;
        public string KitchenName { get; set; } = string.Empty;
        public string KitchenType { get; set; } = "MAIN_KITCHEN";
        public Guid? AssociatedStoreLocationId { get; set; }
        public string? ResponsibleExecutiveChef { get; set; }
        public string? KitchenSupervisor { get; set; }
        public decimal DailyPortionCapacity { get; set; } = 1000m;
        public List<Guid> OutletIds { get; set; } = new();
        public List<string> SupportedMealPeriods { get; set; } = new() { "BREAKFAST", "LUNCH", "DINNER" };
        public List<string> SupportedCategories { get; set; } = new();
    }

    public class SilaCulinaryWorkstationDto
    {
        public Guid Id { get; set; }
        public Guid KitchenId { get; set; }
        public string WorkstationCode { get; set; } = string.Empty;
        public string WorkstationName { get; set; } = string.Empty;
        public string WorkstationType { get; set; } = "COOKING";
        public int ConcurrentCapacity { get; set; }
    }

    public class SilaCulinaryEquipmentDto
    {
        public Guid Id { get; set; }
        public Guid KitchenId { get; set; }
        public Guid? WorkstationId { get; set; }
        public string EquipmentCode { get; set; } = string.Empty;
        public string EquipmentName { get; set; } = string.Empty;
        public string EquipmentType { get; set; } = "COMBI_OVEN";
        public decimal BatchCapacity { get; set; }
        public string CapacityUom { get; set; } = "PORTION";
        public int SetupMinutes { get; set; }
        public int CleaningMinutes { get; set; }
        public int AvailableFromMinute { get; set; }
        public int AvailableUntilMinute { get; set; }
        public bool UnderMaintenance { get; set; }
        public string? TemperatureCapability { get; set; }
        public bool AllergenSegregated { get; set; }
    }

    public class SilaCulinaryStaffAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid UserId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string CulinaryRole { get; set; } = "CHEF_DE_PARTIE";
        public Guid KitchenId { get; set; }
        public Guid? WorkstationId { get; set; }
        public string ShiftCode { get; set; } = "AM";
        public string SkillClassification { get; set; } = "HOT_PRODUCTION";
        public string? SupervisorName { get; set; }
        public int AvailableFromMinute { get; set; }
        public int AvailableUntilMinute { get; set; }
        public bool IsTemporary { get; set; }
        public bool IsCrossKitchen { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class SilaCulinaryProductionDemandDto
    {
        public Guid Id { get; set; }
        public string SourceDocumentKey { get; set; } = string.Empty;
        public string DemandSource { get; set; } = "EVENT_BOOKING";
        public string DemandNature { get; set; } = "CONFIRMED";
        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? RecipeId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public string MealPeriod { get; set; } = "DINNER";
        public DateTime ProductionDateUtc { get; set; }
        public DateTime RequiredCompletionUtc { get; set; }
        public decimal ForecastPortions { get; set; }
        public decimal AdjustedPortions { get; set; }
        public int Priority { get; set; }
        public string ConversionStatus { get; set; } = "OPEN";
    }

    public class SilaCulinaryProductionPlanLineDto
    {
        public Guid Id { get; set; }
        public Guid? RecipeId { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public string MenuItemName { get; set; } = string.Empty;
        public int RecipeVersion { get; set; }
        public decimal RequiredPortions { get; set; }
        public decimal PlannedProductionQty { get; set; }
        public string ProductionUom { get; set; } = "PORTION";
        public Guid? KitchenId { get; set; }
        public Guid? WorkstationId { get; set; }
        public DateTime PlannedStartUtc { get; set; }
        public DateTime RequiredCompletionUtc { get; set; }
        public int Priority { get; set; }
        public string IngredientReadiness { get; set; } = "AVAILABLE";
        public string EquipmentReadiness { get; set; } = "READY";
        public string StaffReadiness { get; set; } = "READY";
        public decimal? EstimatedFoodCost { get; set; }
        public string LineStatus { get; set; } = "PLANNED";
    }

    public class SilaCulinaryProductionPlanDto
    {
        public Guid Id { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public DateTime ProductionDateUtc { get; set; }
        public string PlanningPeriod { get; set; } = "DAILY";
        public string MealPeriod { get; set; } = "ALL_DAY";
        public Guid? EventId { get; set; }
        public string? EventReference { get; set; }
        public string ResponsibleChef { get; set; } = string.Empty;
        public string SourceDemandType { get; set; } = "MIXED";
        public string Status { get; set; } = "DRAFT";
        public int VersionNumber { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public List<SilaCulinaryProductionPlanLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryCreateProductionPlanRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public DateTime? ProductionDateUtc { get; set; }
        public string PlanningPeriod { get; set; } = "DAILY";
        public string MealPeriod { get; set; } = "DINNER";
        public Guid? EventId { get; set; }
        public string? EventReference { get; set; }
        public string ResponsibleChef { get; set; } = "Executive Chef";
        public List<Guid> DemandIds { get; set; } = new();
        public List<SilaCulinaryCreatePlanLineInputDto> ManualLines { get; set; } = new();
    }

    public class SilaCulinaryCreatePlanLineInputDto
    {
        public Guid? RecipeId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public decimal RequiredPortions { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? WorkstationId { get; set; }
        public DateTime? RequiredCompletionUtc { get; set; }
        public int Priority { get; set; } = 50;
    }

    public class SilaCulinaryProductionOrderIngredientDto
    {
        public Guid Id { get; set; }
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal GrossRequiredQty { get; set; }
        public decimal NetUsableQty { get; set; }
        public decimal YieldAdjustedQty { get; set; }
        public string InventoryUom { get; set; } = "KG";
        public decimal AvailableEligibleStock { get; set; }
        public decimal IncomingBeforeProductionQty { get; set; }
        public decimal ExistingCommitmentsQty { get; set; }
        public decimal ShortageQty { get; set; }
        public string ReadinessStatus { get; set; } = "AVAILABLE";
    }

    public class SilaCulinaryProductionBatchDto
    {
        public Guid Id { get; set; }
        public Guid ProductionOrderId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string BatchType { get; set; } = "EQUIPMENT_BASED";
        public decimal PlannedBatchQty { get; set; }
        public decimal ActualOutputQty { get; set; }
        public string ProductionUom { get; set; } = "PORTION";
        public Guid? KitchenId { get; set; }
        public Guid? WorkstationId { get; set; }
        public Guid? EquipmentId { get; set; }
        public Guid? AssignedStaffUserId { get; set; }
        public string? AssignedChefName { get; set; }
        public DateTime PlannedStartUtc { get; set; }
        public DateTime PlannedFinishUtc { get; set; }
        public DateTime? ActualStartUtc { get; set; }
        public DateTime? ActualFinishUtc { get; set; }
        public int BatchPriority { get; set; }
        public string QualityStatus { get; set; } = "PENDING";
        public string BatchStatus { get; set; } = "CREATED";
        public Guid? ParentBatchId { get; set; }
        public DateTime? UseByUtc { get; set; }
        public string? AllergenSummary { get; set; }
        public string QrPayloadJson { get; set; } = "{}";
    }

    public class SilaCulinaryProductionOrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? ProductionPlanId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? KitchenId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? EventId { get; set; }
        public Guid? RecipeId { get; set; }
        public int RecipeVersion { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string OrderType { get; set; } = "INDIVIDUAL";
        public decimal PlannedOutputQty { get; set; }
        public decimal ActualOutputQty { get; set; }
        public string OutputUom { get; set; } = "PORTION";
        public DateTime ScheduledStartUtc { get; set; }
        public DateTime RequiredCompletionUtc { get; set; }
        public string ResponsibleChef { get; set; } = string.Empty;
        public int Priority { get; set; }
        public decimal? EstimatedProductionCost { get; set; }
        public string ApprovalStatus { get; set; } = "APPROVED";
        public string ExecutionStatus { get; set; } = "CREATED";
        public List<SilaCulinaryProductionOrderIngredientDto> Ingredients { get; set; } = new();
        public List<SilaCulinaryProductionBatchDto> Batches { get; set; } = new();
    }

    public class SilaCulinaryProductionTaskDto
    {
        public Guid Id { get; set; }
        public Guid ProductionBatchId { get; set; }
        public int SequenceOrder { get; set; }
        public Guid? DependsOnTaskId { get; set; }
        public string TaskType { get; set; } = "PREPARATION";
        public string Instructions { get; set; } = string.Empty;
        public Guid? WorkstationId { get; set; }
        public Guid? EquipmentId { get; set; }
        public int PlannedDurationMinutes { get; set; }
        public int? ActualDurationMinutes { get; set; }
        public string Status { get; set; } = "PENDING";
        public string? ExceptionReason { get; set; }
    }

    public class SilaCulinaryProductionPickingLineDto
    {
        public Guid Id { get; set; }
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; }
        public decimal ReservedQty { get; set; }
        public decimal PickedQty { get; set; }
        public string InventoryUom { get; set; } = "KG";
        public Guid? StoreLocationId { get; set; }
        public string? InventoryBatchNumber { get; set; }
        public DateTime? ExpiryUtc { get; set; }
        public string PickingStrategy { get; set; } = "FEFO";
        public string Status { get; set; } = "OPEN";
    }

    public class SilaCulinaryIssueMaterialRequestDto
    {
        public Guid ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public Guid FromStoreLocationId { get; set; }
        public Guid ToKitchenLocationId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public decimal IssueQty { get; set; }
        public string Uom { get; set; } = "KG";
        public string IdempotencyKey { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public class SilaCulinaryQualityRecordRequestDto
    {
        public Guid ProductionBatchId { get; set; }
        public Guid? CheckpointId { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = ">= 75";
        public string ActualValue { get; set; } = string.Empty;
        public string MeasurementUom { get; set; } = "C";
        public bool Passed { get; set; }
        public string? Comments { get; set; }
        public string? CorrectiveAction { get; set; }
    }

    public class SilaCulinaryQualityResultDto
    {
        public Guid Id { get; set; }
        public Guid ProductionBatchId { get; set; }
        public string CheckpointName { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public string MeasurementUom { get; set; } = "C";
        public bool Passed { get; set; }
        public string RecordedByName { get; set; } = string.Empty;
        public DateTime RecordedAtUtc { get; set; }
        public string? Comments { get; set; }
        public string? CorrectiveAction { get; set; }
        public bool SupervisorApproved { get; set; }
    }

    public class SilaCulinaryProductionExceptionDto
    {
        public Guid Id { get; set; }
        public string ExceptionCode { get; set; } = string.Empty;
        public Guid? ProductionOrderId { get; set; }
        public Guid? ProductionBatchId { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public string Severity { get; set; } = "HIGH";
        public string Description { get; set; } = string.Empty;
        public string ReportedByName { get; set; } = string.Empty;
        public string? AssignedOwner { get; set; }
        public string? RecommendedAction { get; set; }
        public string ResolutionStatus { get; set; } = "OPEN";
        public string? ResolutionNote { get; set; }
    }

    public class SilaCulinaryProductionDispatchDto
    {
        public Guid Id { get; set; }
        public string DispatchNumber { get; set; } = string.Empty;
        public Guid CentralKitchenId { get; set; }
        public Guid ProductionBatchId { get; set; }
        public Guid DestinationOutletId { get; set; }
        public string DestinationOutletName { get; set; } = string.Empty;
        public decimal RequestedQty { get; set; }
        public decimal AllocatedQty { get; set; }
        public decimal DispatchedQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public DateTime RequiredDeliveryUtc { get; set; }
        public string Status { get; set; } = "ALLOCATED";
    }

    public class SilaCulinaryScheduleOptimizeRequestDto
    {
        public Guid? ProductionPlanId { get; set; }
        public string Objective { get; set; } = "MIN_DELAY";
        public bool EnforceFoodSafety { get; set; } = true;
    }

    public class SilaCulinaryScheduleOptimizeResponseDto
    {
        public string Status { get; set; } = "OPTIMAL";
        public string SolverUsed { get; set; } = "scipy_highs_milp";
        public List<SilaCulinaryScheduledBatchDto> Assignments { get; set; } = new();
        public Dictionary<string, decimal> EquipmentUtilizationPct { get; set; } = new();
        public Dictionary<string, decimal> StaffUtilizationPct { get; set; } = new();
        public List<string> Bottlenecks { get; set; } = new();
        public List<string> ConflictingConstraints { get; set; } = new();
        public List<string> Assumptions { get; set; } = new();
        public List<string> AlternativeRecommendations { get; set; } = new();
    }

    public class SilaCulinaryScheduledBatchDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string? AssignedEquipmentId { get; set; }
        public string? AssignedStaffId { get; set; }
        public DateTime PlannedStartUtc { get; set; }
        public DateTime PlannedFinishUtc { get; set; }
        public int DelayMinutes { get; set; }
        public int HoldingBufferMinutes { get; set; }
    }

    public class SilaCulinaryProductionDashboardDto
    {
        public int PlannedOrdersCount { get; set; }
        public int ReleasedOrdersCount { get; set; }
        public int ActiveBatchesCount { get; set; }
        public int CompletedBatchesCount { get; set; }
        public int DelayedBatchesCount { get; set; }
        public int IngredientShortagesCount { get; set; }
        public int PendingQualityChecksCount { get; set; }
        public decimal EquipmentUtilizationPct { get; set; }
        public decimal StaffUtilizationPct { get; set; }
        public decimal OnTimeProductionRatePct { get; set; }
        public decimal PlannedOutputTotal { get; set; }
        public decimal ActualOutputTotal { get; set; }
        public List<SilaCulinaryProductionOrderDto> Orders { get; set; } = new();
        public List<SilaCulinaryProductionBatchDto> KdsBatches { get; set; } = new();
        public List<SilaCulinaryProductionExceptionDto> OpenExceptions { get; set; } = new();
    }

    public class SilaCulinaryProductionBatchTraceabilityDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int RecipeVersion { get; set; }
        public string QrPayloadJson { get; set; } = "{}";
        public string BackwardTraceSummary { get; set; } = string.Empty;
        public string ForwardTraceSummary { get; set; } = string.Empty;
        public List<string> IngredientBatches { get; set; } = new();
        public List<SilaCulinaryProductionDispatchDto> OutletDispatches { get; set; } = new();
        public List<SilaCulinaryQualityResultDto> QualityResults { get; set; } = new();
    }

    public class SilaCulinaryProductionCopilotResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public DateTime DataTimestampUtc { get; set; } = DateTime.UtcNow;
        public List<string> SupportingMetrics { get; set; } = new();
        public List<string> IdentifiedRisks { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public string ExpectedOperationalImpact { get; set; } = string.Empty;
        public bool ActionBlockedNotice { get; set; } = true;
    }

    public class SilaCulinaryProductionReportCatalogItemDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Formats { get; set; } = new() { "EXCEL", "CSV", "PDF" };
    }

    public class SilaCulinaryMobileProductionSummaryDto
    {
        public bool CanViewCosts { get; set; }
        public int MyTasksCount { get; set; }
        public int ActiveBatchesCount { get; set; }
        public int PendingPickingCount { get; set; }
        public int PendingQualityChecksCount { get; set; }
        public int OpenExceptionsCount { get; set; }
        public List<SilaCulinaryProductionTaskDto> MyTasks { get; set; } = new();
        public List<SilaCulinaryProductionBatchDto> ActiveBatches { get; set; } = new();
        public List<SilaCulinaryProductionPickingLineDto> PickingLines { get; set; } = new();
        public List<SilaCulinaryProductionExceptionDto> Exceptions { get; set; } = new();
    }

    public class SilaCulinaryProductionPhase4BHandoverDto
    {
        public string ContractVersion { get; set; } = "v4a-to-v4b.1";
        public int CompletedBatchesCount { get; set; }
        public int MaterialIssueReferencesCount { get; set; }
        public decimal TotalPlannedOutputQty { get; set; }
        public decimal TotalActualOutputQty { get; set; }
        public string ReadinessNote { get; set; } = "Operational production execution & issue references preserved separately from Phase 4B financial settlement.";
    }
}
