namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryMrpDashboardDto
    {
        public int ActiveMrpRuns { get; set; }
        public int UpcomingEventRequirements { get; set; }
        public int ApprovedMrpBaselines { get; set; }
        public int PendingMrpApprovals { get; set; }
        public int UniqueRequiredMaterials { get; set; }
        public int MaterialsWithShortages { get; set; }
        public int MaterialsWithUnresolvedMappings { get; set; }
        public int MaterialsRequiredWithin24Hours { get; set; }
        public int MaterialsRequiredWithin48Hours { get; set; }
        public decimal? EstimatedRequirementValue { get; set; }
        public decimal? IndicativeShortageValue { get; set; }
        public string Currency { get; set; } = "AED";
        public bool CostVisible { get; set; } = true;
        public Dictionary<string, decimal> RequirementsByCategory { get; set; } = new();
        public Dictionary<string, decimal> ValueByCategory { get; set; } = new();
        public Dictionary<string, int> ExceptionsByType { get; set; } = new();
        public Dictionary<string, int> DemandByKitchen { get; set; } = new();
        public List<SilaCulinaryMrpChartPointDto> ShortageByMaterial { get; set; } = new();
        public List<SilaCulinaryMrpChartPointDto> DemandByDate { get; set; } = new();
    }

    public class SilaCulinaryMrpChartPointDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Qty { get; set; }
        public decimal? Value { get; set; }
    }

    public class SilaCulinaryMrpCreateRunRequestDto
    {
        public string? Name { get; set; }
        public string RunScope { get; set; } = "SINGLE_EVENT"; // SINGLE_EVENT | SELECTED_SESSIONS | SELECTED_KITCHENS | MULTI_EVENT | DATE_RANGE | PROPERTY
        public List<Guid> EventIds { get; set; } = new();
        public List<Guid> SessionIds { get; set; } = new();
        public List<Guid> KitchenLocationIds { get; set; } = new();
        public Guid? PropertyId { get; set; }
        public DateTime? HorizonStartUtc { get; set; }
        public DateTime? HorizonEndUtc { get; set; }
        public string BufferPolicy { get; set; } = "PRECEDENCE";
        public string PreparedStockMode { get; set; } = "ADJUSTED_SCENARIO";
        public bool IsSimulation { get; set; }
        public bool AllowUnapprovedEvents { get; set; } = true;
    }

    public class SilaCulinaryMrpRunSummaryDto
    {
        public Guid Id { get; set; }
        public string MrpCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string RunScope { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public bool IsApprovedBaseline { get; set; }
        public bool IsSimulation { get; set; }
        public string EngineVersion { get; set; } = "2B.1.0";
        public string CorrelationId { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? PlantCode { get; set; }
        public DateTime? PlanningHorizonStartUtc { get; set; }
        public DateTime? PlanningHorizonEndUtc { get; set; }
        public DateTime CalculatedAtUtc { get; set; }
        public string BufferPolicy { get; set; } = "PRECEDENCE";
        public string PreparedStockMode { get; set; } = "ADJUSTED_SCENARIO";
        public int TotalEvents { get; set; }
        public int TotalRecipes { get; set; }
        public int TotalUniqueMaterials { get; set; }
        public int SufficientStockCount { get; set; }
        public int ShortageCount { get; set; }
        public int UnknownStockCount { get; set; }
        public int MissingMappingCount { get; set; }
        public int BlockingExceptionCount { get; set; }
        public int WarningExceptionCount { get; set; }
        public decimal? EstimatedRequirementValue { get; set; }
        public decimal? IndicativeShortageValue { get; set; }
        public string Currency { get; set; } = "AED";
        public bool CostVisible { get; set; } = true;
        public DateTime? InventoryTimestampUtc { get; set; }
        public DateTime? NextRequiredByUtc { get; set; }
        public string? ApprovalReference { get; set; }
        public string? CreatedByName { get; set; }
        public List<string> EventReferences { get; set; } = new();
    }

    public class SilaCulinaryMrpRunDetailDto : SilaCulinaryMrpRunSummaryDto
    {
        public List<SilaCulinaryMrpLineDto> Lines { get; set; } = new();
        public List<SilaCulinaryMrpExceptionDto> Exceptions { get; set; } = new();
        public List<SilaCulinaryMrpExplosionNodeDto> ExplosionTree { get; set; } = new();
        public List<SilaCulinaryMrpTimeBucketDto> TimePhasedBuckets { get; set; } = new();
        public List<SilaCulinaryMrpBatchSummaryDto> BatchSummaries { get; set; } = new();
        public SilaCulinaryMrpReadinessDto Readiness { get; set; } = new();
        public List<SilaCulinaryMrpScenarioDto> Scenarios { get; set; } = new();
        public List<SilaCulinaryMrpSnapshotDto> Snapshots { get; set; } = new();
    }

    public class SilaCulinaryMrpLineDto
    {
        public Guid Id { get; set; }
        public string ConsolidationKey { get; set; } = string.Empty;
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string MaterialDescription { get; set; } = string.Empty;
        public string MaterialCategory { get; set; } = "Uncategorized";
        public bool IsResolvedMaterial { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PlantCode { get; set; }
        public Guid? KitchenLocationId { get; set; }
        public string KitchenName { get; set; } = string.Empty;
        public string StorageLocationCode { get; set; } = string.Empty;
        public DateTime RequiredDateUtc { get; set; }
        public string RequiredDateBucket { get; set; } = string.Empty;
        public string BaseUom { get; set; } = "KG";
        public decimal EdibleQty { get; set; }
        public decimal RawQty { get; set; }
        public decimal GrossQty { get; set; }
        public decimal PreparedStockQty { get; set; }
        public decimal IndicativeNewProductionQty { get; set; }
        public decimal? EligibleStockQty { get; set; }
        public decimal IncomingPoQty { get; set; }
        public decimal? IndicativeCurrentShortageQty { get; set; }
        public decimal? IndicativeRemainingShortageQty { get; set; }
        public decimal? UnitCost { get; set; }
        public string Currency { get; set; } = "AED";
        public string CostStatus { get; set; } = "COST_UNAVAILABLE";
        public decimal? RequirementValue { get; set; }
        public decimal? IndicativeShortageValue { get; set; }
        public bool CostVisible { get; set; } = true;
        public string ValidationStatus { get; set; } = "REQUIREMENT_CALCULATED";
        public bool IsBlocking { get; set; }
        public DateTime? InventoryTimestampUtc { get; set; }
        public List<string> ContributingRecipes { get; set; } = new();
        public List<string> ContributingEvents { get; set; } = new();
        public List<string> ContributingSessions { get; set; } = new();
        public List<string> SubRecipePaths { get; set; } = new();
        public List<string> ExclusionReasons { get; set; } = new();
    }

    public class SilaCulinaryMrpGroupViewDto
    {
        public string GroupBy { get; set; } = "MATERIAL";
        public List<SilaCulinaryMrpGroupRowDto> Groups { get; set; } = new();
    }

    public class SilaCulinaryMrpGroupRowDto
    {
        public string GroupKey { get; set; } = string.Empty;
        public string GroupLabel { get; set; } = string.Empty;
        public int LineCount { get; set; }
        public int ShortageCount { get; set; }
        public decimal? TotalQtyIfSameUom { get; set; }
        public string? CommonUom { get; set; }
        public decimal? TotalRequirementValue { get; set; }
        public decimal? TotalShortageValue { get; set; }
        public List<SilaCulinaryMrpLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryMrpExplosionNodeDto
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string ReferenceCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? RecipeId { get; set; }
        public int? RecipeVersion { get; set; }
        public string? MaterialCode { get; set; }
        public decimal RequiredQty { get; set; }
        public string Uom { get; set; } = string.Empty;
        public decimal YieldPercent { get; set; } = 100m;
        public decimal ScalingFactor { get; set; } = 1m;
        public int DependencyLevel { get; set; }
        public string ValidationStatus { get; set; } = "VALID";
        public List<SilaCulinaryMrpExplosionNodeDto> Children { get; set; } = new();
    }

    public class SilaCulinaryMrpTimeBucketDto
    {
        public string BucketDate { get; set; } = string.Empty;
        public string KitchenName { get; set; } = string.Empty;
        public int MaterialCount { get; set; }
        public decimal? TotalRequirementValue { get; set; }
        public int ShortageMaterialCount { get; set; }
        public DateTime EarliestRequiredByUtc { get; set; }
    }

    public class SilaCulinaryMrpBatchSummaryDto
    {
        public string RecipeId { get; set; } = string.Empty;
        public string RecipeCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int RecipeVersion { get; set; }
        public decimal RequiredQty { get; set; }
        public decimal BatchYield { get; set; }
        public string BatchYieldUom { get; set; } = "KG";
        public bool AllowPartialBatches { get; set; }
        public decimal RequiredBatches { get; set; }
        public decimal PlannedOutputQty { get; set; }
        public decimal ExcessOutputQty { get; set; }
        public decimal PreparedStockQty { get; set; }
        public decimal IndicativeNewProductionQty { get; set; }
    }

    public class SilaCulinaryMrpExceptionDto
    {
        public Guid Id { get; set; }
        public string ExceptionCode { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string Severity { get; set; } = "WARNING";
        public Guid? EventId { get; set; }
        public string? EventCode { get; set; }
        public Guid? RecipeId { get; set; }
        public string? RecipeCode { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialName { get; set; }
        public string CalculationImpact { get; set; } = string.Empty;
        public string ResponsibleTeam { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
        public string? LinkedWorkflow { get; set; }
        public string ResolutionStatus { get; set; } = "OPEN";
        public string? ResolutionNote { get; set; }
        public string? ResolvedByName { get; set; }
        public DateTime? ResolvedOnUtc { get; set; }
        public List<string> CyclePath { get; set; } = new();
    }

    public class SilaCulinaryMrpReadinessCheckDto
    {
        public string Dimension { get; set; } = string.Empty; // CALCULATION | MATERIAL_DATA | COSTING | PROCUREMENT_PLANNING
        public string Label { get; set; } = string.Empty;
        public string Status { get; set; } = "READY"; // READY | WARNING | BLOCKED
        public string Detail { get; set; } = string.Empty;
    }

    public class SilaCulinaryMrpReadinessDto
    {
        public bool CalculationReady { get; set; }
        public bool MaterialDataReady { get; set; }
        public bool CostingReady { get; set; }
        public bool ProcurementPlanningReady { get; set; }
        public bool CanSubmitForApproval { get; set; }
        public bool CanReleaseForPlanning { get; set; }
        public List<SilaCulinaryMrpReadinessCheckDto> Checks { get; set; } = new();
    }

    public class SilaCulinaryMrpSimulateRequestDto
    {
        public decimal GuestScaleFactor { get; set; } = 1m;
        public int? OverrideGuests { get; set; }
        public decimal AdditionalAllowancePercent { get; set; }
        public decimal BufferPercent { get; set; }
        public decimal YieldOverridePercent { get; set; }
        public decimal PriceIncreasePercent { get; set; }
        public string PreparedStockMode { get; set; } = "ADJUSTED_SCENARIO";
        public List<SilaCulinaryMrpLineSimulationOverrideDto> LineOverrides { get; set; } = new();
    }

    public class SilaCulinaryMrpLineSimulationOverrideDto
    {
        public Guid LineId { get; set; }
        public decimal? UptakeFactor { get; set; }
        public decimal? BufferPercent { get; set; }
        public decimal? PreparedStockQty { get; set; }
        public decimal? EligibleStockQty { get; set; }
        public decimal? UnitCost { get; set; }
    }

    public class SilaCulinaryMrpSimulationResultDto
    {
        public int PlannedGuests { get; set; }
        public decimal? TotalRequirementValue { get; set; }
        public decimal? TotalShortageValue { get; set; }
        public int ShortageCount { get; set; }
        public int MissingMappingCount { get; set; }
        public string Currency { get; set; } = "AED";
        public bool CostVisible { get; set; } = true;
        public List<SilaCulinaryMrpLineDto> SimulatedLines { get; set; } = new();
    }

    public class SilaCulinaryMrpScenarioDto
    {
        public Guid Id { get; set; }
        public string ScenarioCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ScenarioType { get; set; } = string.Empty;
        public bool IsBaseline { get; set; }
        public int PlannedGuests { get; set; }
        public decimal? TotalRequirementValue { get; set; }
        public decimal? TotalShortageValue { get; set; }
        public int ShortageCount { get; set; }
        public int ExceptionCount { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class SilaCulinaryMrpSnapshotDto
    {
        public Guid Id { get; set; }
        public int VersionNumber { get; set; }
        public string StatusAtSnapshot { get; set; } = string.Empty;
        public bool IsApprovedBaseline { get; set; }
        public string? ChangeReason { get; set; }
        public string? ApprovalReference { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class SilaCulinaryMrpImpactLineDto
    {
        public string MaterialKey { get; set; } = string.Empty;
        public string? MaterialCode { get; set; }
        public string MaterialDescription { get; set; } = string.Empty;
        public string BaseUom { get; set; } = "KG";
        public decimal PreviousQty { get; set; }
        public decimal ProposedQty { get; set; }
        public decimal DeltaQty { get; set; }
        public string ImpactType { get; set; } = "UNCHANGED"; // INCREASE | DECREASE | NEW_MATERIAL | REMOVED_MATERIAL | UNCHANGED
        public decimal? CostImpact { get; set; }
    }

    public class SilaCulinaryMrpImpactAnalysisDto
    {
        public Guid BaseMrpRunId { get; set; }
        public string BaseMrpCode { get; set; } = string.Empty;
        public decimal? PreviousTotalValue { get; set; }
        public decimal? ProposedTotalValue { get; set; }
        public decimal? DeltaTotalValue { get; set; }
        public int NewlyRequiredCount { get; set; }
        public int RemovedCount { get; set; }
        public bool ApprovalInvalidated { get; set; }
        public string Currency { get; set; } = "AED";
        public List<SilaCulinaryMrpImpactLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryMrpAiAnalyzeRequestDto
    {
        public string Question { get; set; } = string.Empty;
    }

    public class SilaCulinaryMrpAiAnalyzeResponseDto
    {
        public string ProviderUsed { get; set; } = "LOCAL_DETERMINISTIC";
        public string ModelUsed { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public List<string> ReferencedMaterials { get; set; } = new();
        public List<string> ReferencedRecipes { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<string> DataGroundingNotes { get; set; } = new();
    }

    public class SilaCulinaryMrpOptimizeResultDto
    {
        public bool Feasible { get; set; }
        public string Solver { get; set; } = "SCIPY_HIGHS_MILP";
        public decimal TotalCost { get; set; }
        public decimal TotalExcessOutput { get; set; }
        public List<string> Assumptions { get; set; } = new();
        public string? InfeasibilityReason { get; set; }
        public List<SilaCulinaryMrpBatchSummaryDto> OptimizedBatches { get; set; } = new();
    }

    public class SilaCulinaryMrpSubstitutionSuggestionDto
    {
        public string OriginalMaterialCode { get; set; } = string.Empty;
        public string OriginalMaterialName { get; set; } = string.Empty;
        public string ProposedMaterialCode { get; set; } = string.Empty;
        public string ProposedMaterialName { get; set; } = string.Empty;
        public decimal QuantityImpact { get; set; }
        public decimal? CostImpact { get; set; }
        public string DietaryImpact { get; set; } = "VERIFY_CHEF";
        public decimal? ProposedEligibleStock { get; set; }
        public string ChefReviewStatus { get; set; } = "CHEF_APPROVAL_REQUIRED";
    }

    public class SilaCulinaryMrpReportCatalogItemDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // MRP | QUANTITY_YIELD | STOCK_COST | GOVERNANCE
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SilaCulinaryMrpReportResultDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public string Base64Content { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public List<string> Headers { get; set; } = new();
        public List<List<string>> PreviewRows { get; set; } = new();
    }

    /// <summary>Phase 2C Procurement Planning Handover Contract (§56).</summary>
    public class SilaCulinaryPhase2CHandoverDto
    {
        public Guid MrpId { get; set; }
        public string MrpCode { get; set; } = string.Empty;
        public int MrpVersion { get; set; }
        public string MrpApprovalStatus { get; set; } = string.Empty;
        public bool ReleasedForPlanning { get; set; }
        public DateTime CalculationTimestampUtc { get; set; }
        public Guid? PropertyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string Currency { get; set; } = "AED";
        public bool ZeroReservationsCreated { get; set; } = true;
        public bool ZeroPosCreated { get; set; } = true;
        public bool ZeroItosCreated { get; set; } = true;
        public bool ZeroSapPostingsExecuted { get; set; } = true;
        public List<string> EventReferences { get; set; } = new();
        public List<SilaCulinaryPhase2CMaterialHandoverDto> Materials { get; set; } = new();
        public List<SilaCulinaryMrpExceptionDto> OutstandingExceptions { get; set; } = new();
    }

    public class SilaCulinaryPhase2CMaterialHandoverDto
    {
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string MaterialDescription { get; set; } = string.Empty;
        public string MaterialCategory { get; set; } = string.Empty;
        public string BaseUom { get; set; } = "KG";
        public string? PurchasingUom { get; set; }
        public decimal GrossRequirement { get; set; }
        public decimal PreparedStockScenario { get; set; }
        public decimal IndicativeNewProductionQty { get; set; }
        public decimal? CurrentEligibleStock { get; set; }
        public decimal IncomingPoQtySeparate { get; set; }
        public decimal? IndicativeShortage { get; set; }
        public DateTime RequiredByTimestampUtc { get; set; }
        public string KitchenName { get; set; } = string.Empty;
        public string StorageLocationCode { get; set; } = string.Empty;
        public List<string> EventReferences { get; set; } = new();
        public List<string> SessionReferences { get; set; } = new();
        public List<string> RecipeReferences { get; set; } = new();
        public List<string> SubRecipePaths { get; set; } = new();
    }
}
