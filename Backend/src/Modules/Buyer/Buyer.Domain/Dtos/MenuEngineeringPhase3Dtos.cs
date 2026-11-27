namespace Buyer.Domain.Dtos
{
    // ─────────────────────────────────────────────────────────────────────────
    // 1. Digital Twin Snapshot Engine (9-Component Hospitality Twin)
    // ─────────────────────────────────────────────────────────────────────────

    public class CreateTwinSnapshotRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? EventId { get; set; }
        public string SnapshotType { get; set; } = "LIVE"; // LIVE | SCHEDULED | HISTORICAL | SCENARIO | EVENT | MENU
        public DateTime? BusinessEffectiveDateUtc { get; set; }
        public int? ExpirationHours { get; set; } = 72;
    }

    public class TwinComponentSummaryDto
    {
        public string ComponentKey { get; set; } = string.Empty; // MENU | RECIPE | DEMAND | INVENTORY | PRODUCTION | FINANCIAL | SUPPLIER | WASTE | EVENT
        public string DisplayName { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public string SummaryMetric { get; set; } = string.Empty;
        public string DataQuality { get; set; } = "HIGH";
        public DateTime? LastSourceUpdateUtc { get; set; }
    }

    public class TwinSnapshotDto
    {
        public Guid SnapshotId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? EventId { get; set; }
        public string SnapshotType { get; set; } = "LIVE";
        public int VersionNumber { get; set; }
        public string SnapshotHash { get; set; } = string.Empty;
        public string CalculationVersion { get; set; } = "me-calc-v3.0";
        public string ModelVersion { get; set; } = "me-twin-v3.0";
        public decimal DataQualityScore { get; set; }
        public string DataQualityLabel { get; set; } = "HIGH";
        public DateTime BusinessEffectiveDateUtc { get; set; }
        public DateTime SourceMaxUpdatedAtUtc { get; set; }
        public DateTime DataFreshnessUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public bool IsStale { get; set; }
        public string? StalenessReason { get; set; }
        public DateTime CapturedAtUtc { get; set; }
        public List<TwinComponentSummaryDto> Components { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<string> SourceLineage { get; set; } = new();
    }

    public class TwinSnapshotComparisonDto
    {
        public Guid LeftSnapshotId { get; set; }
        public Guid RightSnapshotId { get; set; }
        public int LeftVersion { get; set; }
        public int RightVersion { get; set; }
        public List<TwinDeltaMetricDto> Deltas { get; set; } = new();
    }

    public class TwinDeltaMetricDto
    {
        public string MetricKey { get; set; } = string.Empty;
        public decimal LeftValue { get; set; }
        public decimal RightValue { get; set; }
        public decimal Delta { get; set; }
        public decimal DeltaPercent { get; set; }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Digital Twin Dependency Graph & Reverse Price Impact
    // ─────────────────────────────────────────────────────────────────────────

    public class TwinDependencyAnalysisRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? ShockMaterialId { get; set; }
        public string? ShockIngredientName { get; set; }
        public decimal PriceChangePercent { get; set; }
    }

    public class TwinIntegrityIssueDto
    {
        public string IssueType { get; set; } = string.Empty;
        public string Severity { get; set; } = "WARNING";
        public List<string> NodeIds { get; set; } = new();
        public string Detail { get; set; } = string.Empty;
    }

    public class TwinImpactedObjectDto
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int HopDistance { get; set; }
        public decimal BaselineCost { get; set; }
        public decimal ShockedCost { get; set; }
        public decimal CostDelta { get; set; }
        public decimal CostDeltaPercent { get; set; }
        public decimal MarginDelta { get; set; }
    }

    public class TwinDependencyAnalysisResultDto
    {
        public int NodeCount { get; set; }
        public int EdgeCount { get; set; }
        public bool IsAcyclic { get; set; }
        public List<List<string>> Cycles { get; set; } = new();
        public List<TwinIntegrityIssueDto> IntegrityIssues { get; set; } = new();
        public string? ShockNodeId { get; set; }
        public decimal PriceChangePercent { get; set; }
        public List<TwinImpactedObjectDto> ImpactedObjects { get; set; } = new();
        public List<string> AffectedRecipes { get; set; } = new();
        public List<string> AffectedMenuItems { get; set; } = new();
        public List<string> AffectedMenus { get; set; } = new();
        public List<string> AffectedEvents { get; set; } = new();
        public decimal TotalCostImpact { get; set; }
        public decimal TotalMarginImpact { get; set; }
        public string EngineVersion { get; set; } = "me-twin-nx-3.0";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Multi-Objective Optimization, Production Feasibility & Async Jobs
    // ─────────────────────────────────────────────────────────────────────────

    public class MeObjectiveWeightDto
    {
        public string Objective { get; set; } = "MINIMIZE_FOOD_COST";
        public decimal Weight { get; set; } = 1.0m;
    }

    public class MePhase3OptimizeRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? BaselineSnapshotId { get; set; }
        public string Mode { get; set; } = "PARETO_FRONTIER"; // SINGLE_OBJECTIVE | WEIGHTED_MULTI_OBJECTIVE | LEXICOGRAPHIC | PARETO_FRONTIER
        public string MenuFormat { get; set; } = "BUFFET";
        public int GuestCount { get; set; } = 300;
        public decimal TargetSellingPrice { get; set; } = 150m;
        public decimal MaxFoodCostPercent { get; set; } = 30m;
        public decimal? MaxBudget { get; set; }
        public int MinTotalItems { get; set; } = 4;
        public int MaxTotalItems { get; set; } = 18;
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
        public List<string> ExcludedAllergens { get; set; } = new();
        public List<string> RequiredDietaryTags { get; set; } = new();
        public decimal? OverrideProductionMinutes { get; set; }
        public List<MeObjectiveWeightDto> Objectives { get; set; } = new();
        public bool RunAsynchronously { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class MeSelectedDishAllocationDto
    {
        public string DishId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int PlannedPortions { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal PrepMinutes { get; set; }
        public decimal InventoryCoverage { get; set; }
    }

    public class MePhase3OptimizationSolutionDto
    {
        public string ScenarioLabel { get; set; } = string.Empty;
        public int ParetoRank { get; set; } = 1;
        public string SolverStatus { get; set; } = "OPTIMAL";
        public bool IsFeasible { get; set; }
        public bool IsGloballyOptimal { get; set; }
        public decimal? OptimalityGap { get; set; }
        public decimal SolverRuntimeMs { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal FoodCostPercent { get; set; }
        public decimal TotalContributionMargin { get; set; }
        public decimal ExpectedWasteCost { get; set; }
        public decimal InventoryUtilizationPercent { get; set; }
        public decimal TotalPrepMinutes { get; set; }
        public bool HardConstraintsSatisfied { get; set; }
        public List<string> TradeOffExplanation { get; set; } = new();
        public List<MeSelectedDishAllocationDto> SelectedDishes { get; set; } = new();
    }

    public class MePhase3OptimizationJobDto
    {
        public Guid JobId { get; set; }
        public string RunType { get; set; } = "OPTIMIZATION";
        public string Status { get; set; } = "COMPLETED"; // QUEUED | RUNNING | COMPLETED | CANCELLED | FAILED | INFEASIBLE
        public Guid? BaselineSnapshotId { get; set; }
        public string SolverUsed { get; set; } = "OR-TOOLS_CP_SAT";
        public decimal RuntimeMs { get; set; }
        public decimal? OptimalityGap { get; set; }
        public List<MePhase3OptimizationSolutionDto> Solutions { get; set; } = new();
        public List<string> InfeasibilityReasons { get; set; } = new();
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
        public bool IsStale { get; set; }
    }

    public class MePhase3InventoryMenuRequestDto
    {
        public Guid? PropertyId { get; set; }
        public int GuestCount { get; set; } = 150;
        public int MinItems { get; set; } = 3;
        public int MaxItems { get; set; } = 10;
        public decimal MaxFoodCostPercent { get; set; } = 32m;
    }

    public class MePhase3InventoryMenuResultDto
    {
        public string Status { get; set; } = "OPTIMAL";
        public decimal InventoryUtilizationPercent { get; set; }
        public decimal AvoidablePurchaseCost { get; set; }
        public decimal ExpiryAvoidanceValue { get; set; }
        public decimal FoodCostPercent { get; set; }
        public decimal ExpectedContributionMargin { get; set; }
        public Dictionary<string, decimal> RemainingShortages { get; set; } = new();
        public Dictionary<string, decimal> ExcludedStockSummary { get; set; } = new(); // reserved, blocked, expired
        public List<MeSelectedDishAllocationDto> SelectedDishes { get; set; } = new();
    }

    public class MePhase3ProductionFeasibilityDto
    {
        public Guid? MenuPlanId { get; set; }
        public bool IsFeasible { get; set; }
        public int ActiveKitchenCount { get; set; }
        public int ActiveEquipmentCount { get; set; }
        public int ActiveStaffCount { get; set; }
        public decimal TotalRequiredPrepMinutes { get; set; }
        public decimal AvailableKitchenMinutes { get; set; }
        public decimal UtilizationPercent { get; set; }
        public List<string> Bottlenecks { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class MePricingOptimizeItemDto
    {
        public Guid MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public decimal PriceDeltaPercent { get; set; }
        public decimal ExpectedUnits { get; set; }
        public decimal CurrentContribution { get; set; }
        public decimal RecommendedContribution { get; set; }
        public decimal RecommendedFoodCostPercent { get; set; }
        public string ElasticityMode { get; set; } = "FIXED_DEMAND_ASSUMPTION";
        public string AssumptionDisclosure { get; set; } = string.Empty;
    }

    public class MePhase3PricingOptimizeResultDto
    {
        public decimal TotalCurrentContribution { get; set; }
        public decimal TotalRecommendedContribution { get; set; }
        public bool RequiresManualPublishApproval { get; set; } = true;
        public List<MePricingOptimizeItemDto> Recommendations { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Hierarchical Forecast, Cold-Start, Waste, Price Prediction, Substitution
    // ─────────────────────────────────────────────────────────────────────────

    public class MeHierarchicalNodeForecastDto
    {
        public string NodeKey { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string? ParentKey { get; set; }
        public string SelectedModel { get; set; } = string.Empty;
        public List<decimal> ReconciledForecast { get; set; } = new();
        public List<decimal> LowerP10 { get; set; } = new();
        public List<decimal> UpperP90 { get; set; } = new();
        public decimal Mae { get; set; }
        public decimal Wape { get; set; }
        public decimal Bias { get; set; }
        public decimal CoveragePercent { get; set; }
    }

    public class MeHierarchicalForecastResultDto
    {
        public int HorizonDays { get; set; }
        public string ReconciliationMethod { get; set; } = "TOP_DOWN_PROPORTIONAL";
        public bool LeakageChecked { get; set; } = true;
        public decimal ConsistencyMaxGap { get; set; }
        public List<MeHierarchicalNodeForecastDto> Nodes { get; set; } = new();
    }

    public class MeColdStartPredictRequestDto
    {
        public string DishName { get; set; } = string.Empty;
        public string Cuisine { get; set; } = "INTERNATIONAL";
        public string Category { get; set; } = "Main Course";
        public decimal SellingPrice { get; set; } = 85m;
        public decimal FoodCostPercent { get; set; } = 28m;
        public List<string> Ingredients { get; set; } = new();
        public List<string> DietaryTags { get; set; } = new();
    }

    public class MeColdStartComparableDto
    {
        public string DishId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal SimilarityScore { get; set; }
        public decimal HistoricalDailyCovers { get; set; }
        public decimal HistoricalMarginPercent { get; set; }
    }

    public class MeColdStartPredictResultDto
    {
        public bool IsColdStartEstimate { get; set; } = true;
        public string Disclosure { get; set; } = string.Empty;
        public decimal ExpectedDailyDemand { get; set; }
        public decimal DemandLowerP10 { get; set; }
        public decimal DemandUpperP90 { get; set; }
        public string UncertaintyIndicator { get; set; } = "MEDIUM";
        public List<MeColdStartComparableDto> Comparables { get; set; } = new();
    }

    public class MeWastePredictItemDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal PrepWasteQty { get; set; }
        public decimal CookingYieldLossQty { get; set; }
        public decimal OverproductionQty { get; set; }
        public decimal BuffetLeftoverQty { get; set; }
        public decimal ExpiryRiskQty { get; set; }
        public decimal TotalPredictedWasteQty { get; set; }
        public decimal PredictedWasteCost { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    public class MeWastePredictResultDto
    {
        public bool YieldAndWasteSeparated { get; set; } = true;
        public bool FoodSafetyPriorityEnforced { get; set; } = true;
        public decimal TotalPredictedWasteCost { get; set; }
        public List<MeWastePredictItemDto> Items { get; set; } = new();
    }

    public class MePricePredictItemDto
    {
        public string IngredientId { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public decimal CurrentApprovedPrice { get; set; }
        public bool SufficientHistory { get; set; }
        public decimal? PredictedPriceMid { get; set; }
        public decimal? PredictedPriceLow { get; set; }
        public decimal? PredictedPriceHigh { get; set; }
        public decimal? ExpectedMovementPercent { get; set; }
        public decimal? HistoricalVolatilityPercent { get; set; }
        public string DataQuality { get; set; } = "INSUFFICIENT_HISTORY";
        public string Disclosure { get; set; } = string.Empty;
    }

    public class MePricePredictResultDto
    {
        public List<MePricePredictItemDto> Results { get; set; } = new();
    }

    public class MeSubstitutionCandidateRequestDto
    {
        public string SubstituteId { get; set; } = string.Empty;
        public string SubstituteName { get; set; } = string.Empty;
        public bool IsApprovedRelationship { get; set; }
        public decimal YieldFactor { get; set; } = 1.0m;
        public decimal UnitCost { get; set; }
        public bool SupplierAvailable { get; set; } = true;
        public List<string> Allergens { get; set; } = new();
        public List<string> DietaryTags { get; set; } = new();
    }

    public class MeSubstitutionCascadeRequestDto
    {
        public string OriginalIngredient { get; set; } = string.Empty;
        public decimal OriginalQty { get; set; } = 1m;
        public decimal OriginalUnitCost { get; set; } = 20m;
        public decimal RecipeSellingPrice { get; set; } = 100m;
        public decimal OtherIngredientsCost { get; set; } = 10m;
        public List<string> ExcludedAllergens { get; set; } = new();
        public List<string> RequiredDietaryTags { get; set; } = new();
        public List<MeSubstitutionCandidateRequestDto> Candidates { get; set; } = new();
    }

    public class MeSubstitutionCascadeOptionDto
    {
        public string SubstituteId { get; set; } = string.Empty;
        public string SubstituteName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // APPROVED_SUBSTITUTION | PROPOSED_SUBSTITUTION | REJECTED_UNSAFE
        public bool IsSafe { get; set; }
        public string? RejectionReason { get; set; }
        public decimal RecalculatedIngredientQty { get; set; }
        public decimal RecalculatedRecipeCost { get; set; }
        public decimal RecalculatedFoodCostPercent { get; set; }
        public decimal RecalculatedMargin { get; set; }
        public decimal CostDelta { get; set; }
    }

    public class MeSubstitutionCascadeResultDto
    {
        public decimal BaselineRecipeCost { get; set; }
        public decimal BaselineFoodCostPercent { get; set; }
        public List<MeSubstitutionCascadeOptionDto> Results { get; set; } = new();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Monte Carlo, Tornado Sensitivity, Stress Tests, Scenario Lab & Learning
    // ─────────────────────────────────────────────────────────────────────────

    public class MeMonteCarloRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? BaselineSnapshotId { get; set; }
        public int Seed { get; set; } = 42;
        public int Runs { get; set; } = 1000;
        public decimal GuestCountMean { get; set; } = 300m;
        public decimal GuestCountStd { get; set; } = 35m;
        public decimal SellingPricePerGuest { get; set; } = 150m;
        public decimal BaseFoodCostPerServing { get; set; } = 42m;
        public decimal IngredientPriceVolPercent { get; set; } = 8m;
        public decimal EventBudget { get; set; } = 14000m;
        public decimal TargetFoodCostPercent { get; set; } = 30m;
        public decimal AvailableStockServings { get; set; } = 280m;
        public string? IdempotencyKey { get; set; }
    }

    public class MeMonteCarloResultDto
    {
        public Guid SimulationRunId { get; set; }
        public int Seed { get; set; }
        public int Runs { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal ExpectedCost { get; set; }
        public decimal MedianCost { get; set; }
        public decimal CostP10 { get; set; }
        public decimal CostP90 { get; set; }
        public decimal ExpectedFoodCostPercent { get; set; }
        public decimal ExpectedContributionMargin { get; set; }
        public decimal MarginP10 { get; set; }
        public decimal MarginP90 { get; set; }
        public decimal DownsideVar95Margin { get; set; }
        public decimal ProbExceedBudget { get; set; }
        public decimal ProbExceedFoodCostTarget { get; set; }
        public decimal ProbIngredientShortage { get; set; }
        public decimal RuntimeMs { get; set; }
        public Dictionary<string, object> Assumptions { get; set; } = new();
    }

    public class MeSensitivityRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 30m;
    }

    public class MeSensitivityTornadoDriverDto
    {
        public string VariableName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal LowMargin { get; set; }
        public decimal HighMargin { get; set; }
        public decimal MarginSwing { get; set; }
        public decimal LowFoodCostPercent { get; set; }
        public decimal HighFoodCostPercent { get; set; }
        public decimal FoodCostSwingPercent { get; set; }
    }

    public class MeSensitivityResultDto
    {
        public decimal BaselineMargin { get; set; }
        public decimal BaselineFoodCostPercent { get; set; }
        public decimal BreakEvenFoodCostCeiling { get; set; }
        public List<string> TopRiskDrivers { get; set; } = new();
        public List<MeSensitivityTornadoDriverDto> Tornado { get; set; } = new();
    }

    public class MeStressScenarioInputDto
    {
        public string StressType { get; set; } = "CUSTOM";
        public string Label { get; set; } = string.Empty;
        public decimal GuestMultiplier { get; set; } = 1.0m;
        public decimal IngredientPriceMultiplier { get; set; } = 1.0m;
        public decimal YieldMultiplier { get; set; } = 1.0m;
        public int SupplierDelayDays { get; set; }
        public string? UnavailableIngredient { get; set; }
        public decimal UnavailableEquipmentMinutes { get; set; }
    }

    public class MeStressTestRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public int BaselineGuests { get; set; } = 300;
        public int IngredientBufferDays { get; set; } = 2;
        public List<MeStressScenarioInputDto> Scenarios { get; set; } = new();
    }

    public class MeStressTestOutcomeDto
    {
        public string StressType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int RevisedGuests { get; set; }
        public decimal RevisedRevenue { get; set; }
        public decimal RevisedFoodCost { get; set; }
        public decimal RevisedFoodCostPercent { get; set; }
        public decimal RevisedMargin { get; set; }
        public decimal RevisedPrepMinutes { get; set; }
        public bool IsOperationallyFeasible { get; set; }
        public string ShortageRisk { get; set; } = "LOW";
        public List<string> CorrectiveActions { get; set; } = new();
    }

    public class MeStressTestResultDto
    {
        public decimal BaselineMargin { get; set; }
        public List<MeStressTestOutcomeDto> Outcomes { get; set; } = new();
    }

    public class MeScenarioCreateRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? BaselineSnapshotId { get; set; }
        public Guid? ParentScenarioId { get; set; }
        public string ScenarioName { get; set; } = string.Empty;
        public string ScenarioType { get; set; } = "OPTIMIZED"; // BASELINE | MANUAL | AI_GENERATED | OPTIMIZED | STRESS | HISTORICAL | PROBABILISTIC
        public string? Notes { get; set; }
        public Dictionary<string, object> DeltaOverrides { get; set; } = new();
    }

    public class MeScenarioLabItemDto
    {
        public Guid ScenarioId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? BaselineSnapshotId { get; set; }
        public int? BaselineSnapshotVersion { get; set; }
        public string ScenarioName { get; set; } = string.Empty;
        public string ScenarioType { get; set; } = string.Empty;
        public string Status { get; set; } = "ACTIVE";
        public string SolverStatus { get; set; } = "FEASIBLE";
        public int? ParetoRank { get; set; }
        public bool IsPreferred { get; set; }
        public bool IsBaselineStale { get; set; }
        public string CalculationVersion { get; set; } = "me-calc-v3.0";
        public string ModelVersion { get; set; } = "me-twin-v3.0";
        public decimal ExpectedRevenue { get; set; }
        public decimal ExpectedFoodCostPercent { get; set; }
        public decimal ExpectedContributionMargin { get; set; }
        public decimal ExpectedWastePercent { get; set; }
        public decimal InventoryUtilizationPercent { get; set; }
        public decimal CompositeRankScore { get; set; }
        public bool IsFeasible { get; set; }
        public string TradeOffExplanation { get; set; } = string.Empty;
        public DateTime DateCreated { get; set; }
    }

    public class MeScenarioComparisonDto
    {
        public string CalculationBasis { get; set; } = "me-calc-v3.0";
        public List<MeScenarioLabItemDto> FeasibleScenarios { get; set; } = new();
        public List<MeScenarioLabItemDto> InfeasibleScenarios { get; set; } = new();
    }

    public class MeHistoricalSimilarityResultDto
    {
        public string TargetTitle { get; set; } = string.Empty;
        public List<MeHistoricalSimilarityMatchDto> Matches { get; set; } = new();
    }

    public class MeHistoricalSimilarityMatchDto
    {
        public string RecordId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal SimilarityScore { get; set; }
        public List<string> MatchingFactors { get; set; } = new();
        public List<string> ImportantDifferences { get; set; } = new();
        public decimal ActualFoodCostPercent { get; set; }
        public decimal ActualWastePercent { get; set; }
        public decimal ActualMargin { get; set; }
    }

    public class MeEventOutcomeComparisonDto
    {
        public Guid MenuPlanId { get; set; }
        public string MenuPlanName { get; set; } = string.Empty;
        public string DatasetVersion { get; set; } = "ds-v3";
        public string ModelVersion { get; set; } = "me-model-v3.0";
        public decimal PredictedCovers { get; set; }
        public decimal ActualCovers { get; set; }
        public decimal PredictedFoodCostPercent { get; set; }
        public decimal ActualFoodCostPercent { get; set; }
        public decimal PredictedMargin { get; set; }
        public decimal ActualMargin { get; set; }
        public decimal ActualWastePercent { get; set; }
        public decimal ForecastBiasPercent { get; set; }
        public bool DriftDetected { get; set; }
        public DateTime CapturedAtUtc { get; set; }
    }

    public class MeDriftEventDto
    {
        public Guid DriftEventId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public string DriftType { get; set; } = string.Empty;
        public decimal BaselineMetric { get; set; }
        public decimal ObservedMetric { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ChampionModelVersion { get; set; }
        public string? ChallengerModelVersion { get; set; }
        public DateTime DetectedAtUtc { get; set; }
    }

    public class MeModelGovernanceStatusDto
    {
        public string ActiveChampionModel { get; set; } = "me-model-v3.0";
        public string? CandidateChallengerModel { get; set; }
        public List<AiDemandModelMetricDto> Evaluations { get; set; } = new();
        public List<MeDriftEventDto> DriftEvents { get; set; } = new();
        public List<MeEventOutcomeComparisonDto> RecentOutcomeLineage { get; set; } = new();
    }

    public class MeDriftActionRequestDto
    {
        public string Action { get; set; } = "SCHEDULE_RETRAIN"; // SCHEDULE_RETRAIN | DEPLOY_CHALLENGER | ROLLBACK_CHAMPION
        public string? ChallengerModelVersion { get; set; }
        public string? Notes { get; set; }
    }
}
