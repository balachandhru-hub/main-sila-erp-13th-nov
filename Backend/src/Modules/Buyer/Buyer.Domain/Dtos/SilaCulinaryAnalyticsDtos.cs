namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryAnalyticsConfigDto
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public Guid? PropertyId { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 28.0m;
        public decimal TargetContributionMargin { get; set; } = 50.0m;
        public decimal PopularityThresholdFactor { get; set; } = 0.70m;
        public decimal? MenuProfitabilityThresholdOverride { get; set; }
        public decimal IngredientPriceAlertThresholdPercent { get; set; } = 5.0m;
        public decimal WasteTolerancePercent { get; set; } = 4.0m;
        public decimal ConsumptionVarianceTolerancePercent { get; set; } = 5.0m;
        public int DefaultForecastHorizonDays { get; set; } = 14;
        public int MinimumHistoricalDataDays { get; set; } = 7;
        public int MinSalesForClassification { get; set; } = 5;
        public string CostMethodology { get; set; } = "STANDARD";
        public string DefaultCurrency { get; set; } = "AED";
        public string PreferredAiProvider { get; set; } = "LOCAL";
        public string PreferredAiModel { get; set; } = "local-deterministic";
        public int DailyAiQueryLimit { get; set; } = 200;
        public string RecommendationApprovalMode { get; set; } = "EXECUTIVE_CHEF_AND_FINANCE";
        public string ScheduledRefreshFrequency { get; set; } = "DAILY";
        public List<string> AllergenRestrictions { get; set; } = new();
        public List<string> OptimizationObjectives { get; set; } = new() { "MAX_CONTRIBUTION" };
        public int ConfigVersion { get; set; } = 1;
        public List<string> HistoryNotes { get; set; } = new();
        public bool CanEdit { get; set; }
    }

    public class SilaCulinarySaveAnalyticsConfigRequestDto
    {
        public Guid? PropertyId { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 28.0m;
        public decimal TargetContributionMargin { get; set; } = 50.0m;
        public decimal PopularityThresholdFactor { get; set; } = 0.70m;
        public decimal? MenuProfitabilityThresholdOverride { get; set; }
        public decimal IngredientPriceAlertThresholdPercent { get; set; } = 5.0m;
        public decimal WasteTolerancePercent { get; set; } = 4.0m;
        public decimal ConsumptionVarianceTolerancePercent { get; set; } = 5.0m;
        public int DefaultForecastHorizonDays { get; set; } = 14;
        public int MinimumHistoricalDataDays { get; set; } = 7;
        public int MinSalesForClassification { get; set; } = 5;
        public string CostMethodology { get; set; } = "STANDARD";
        public string DefaultCurrency { get; set; } = "AED";
        public string PreferredAiProvider { get; set; } = "LOCAL";
        public string PreferredAiModel { get; set; } = "local-deterministic";
        public int DailyAiQueryLimit { get; set; } = 200;
        public string RecommendationApprovalMode { get; set; } = "EXECUTIVE_CHEF_AND_FINANCE";
        public string ScheduledRefreshFrequency { get; set; } = "DAILY";
        public List<string>? AllergenRestrictions { get; set; }
        public List<string>? OptimizationObjectives { get; set; }
    }

    public class SilaCulinaryExecutiveFilterDto
    {
        public Guid? PropertyId { get; set; }
        public string? OutletName { get; set; }
        public string? MenuCategory { get; set; }
        public string? MealPeriod { get; set; }
        public string? EventType { get; set; }
        public string? SalesChannel { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public string? Currency { get; set; }
        public string? CostMethodology { get; set; }
    }

    public class SilaCulinaryMenuEngineeringItemDto
    {
        public string MenuItemId { get; set; } = string.Empty;
        public string MenuItemCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Food";
        public string MealPeriod { get; set; } = "All Day";
        public string OutletName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public decimal GrossSellingPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetSellingPrice { get; set; }
        public decimal NetSalesRevenue { get; set; }
        public decimal? StandardRecipeCost { get; set; }
        public decimal? ActualIngredientCost { get; set; }
        public decimal GarnishCost { get; set; }
        public decimal PackagingCost { get; set; }
        public decimal DirectLaborCost { get; set; }
        public decimal AllocatedOverhead { get; set; }
        public decimal? TotalProductionCost { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? ContributionMarginPerItem { get; set; }
        public decimal? ContributionMarginPercent { get; set; }
        public decimal? TotalContributionMargin { get; set; }
        public decimal? EstimatedFullyLoadedProfit { get; set; }
        public decimal? ProfitPerPortion { get; set; }
        public decimal PopularityPercent { get; set; }
        public decimal SalesMixPercent { get; set; }
        public decimal RevenueContributionPercent { get; set; }
        public decimal? ProfitContributionPercent { get; set; }
        public string Quadrant { get; set; } = "INSUFFICIENT_DATA";
        public string Movement { get; set; } = "UNCHANGED";
        public string CostSource { get; set; } = "STANDARD_COST";
        public int RecipeVersion { get; set; } = 1;
        public DateTime CostTimestampUtc { get; set; } = DateTime.UtcNow;
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public class SilaCulinaryMenuEngineeringResponseDto
    {
        public string Currency { get; set; } = "AED";
        public decimal PopularityThresholdPercent { get; set; }
        public decimal ContributionMarginThreshold { get; set; }
        public decimal WeightedAverageContributionMargin { get; set; }
        public decimal TotalQuantitySold { get; set; }
        public decimal TotalNetRevenue { get; set; }
        public decimal TotalContributionMargin { get; set; }
        public Dictionary<string, int> Counts { get; set; } = new();
        public List<SilaCulinaryMenuEngineeringItemDto> Items { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class SilaCulinaryConsumptionVarianceRowDto
    {
        public string MaterialId { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Uom { get; set; } = "KG";
        public int RecipeVersion { get; set; } = 1;
        public decimal TheoreticalQuantity { get; set; }
        public decimal ReconciledActualQuantity { get; set; }
        public decimal RecordedWastage { get; set; }
        public decimal QuantityVariance { get; set; }
        public decimal UnexplainedVariance { get; set; }
        public decimal? VariancePercent { get; set; }
        public decimal FinancialVariance { get; set; }
        public decimal YieldVarianceQty { get; set; }
        public decimal PortionVarianceQty { get; set; }
        public List<string> PossibleCauses { get; set; } = new();
        public bool ExceedsTolerance { get; set; }
    }

    public class SilaCulinaryConsumptionVarianceResponseDto
    {
        public string Currency { get; set; } = "AED";
        public decimal TotalTheoreticalQty { get; set; }
        public decimal TotalActualQty { get; set; }
        public decimal TotalRecordedWastage { get; set; }
        public decimal TotalUnexplainedQty { get; set; }
        public decimal TotalFinancialVariance { get; set; }
        public List<SilaCulinaryConsumptionVarianceRowDto> Rows { get; set; } = new();
        public string Note { get; set; } = "Recorded wastage is subtracted from total variance to avoid double-counting. Causes require chef/controller verification.";
    }

    public class SilaCulinaryCreateVarianceTaskRequestDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal UnexplainedVarianceQty { get; set; }
        public decimal FinancialImpact { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public class SilaCulinaryReversePriceImpactRequestDto
    {
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public decimal? SimulatedNewPrice { get; set; }
    }

    public class SilaCulinaryPriceImpactNodeDto
    {
        public string NodeType { get; set; } = "RECIPE";
        public string NodeId { get; set; } = string.Empty;
        public string NodeName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string OutletName { get; set; } = string.Empty;
        public string? ViaSubRecipe { get; set; }
        public decimal PreviousUnitCost { get; set; }
        public decimal RevisedUnitCost { get; set; }
        public decimal UnitCostDelta { get; set; }
        public decimal? PreviousFoodCostPct { get; set; }
        public decimal? RevisedFoodCostPct { get; set; }
        public decimal MarginImpactPerUnit { get; set; }
        public decimal EstimatedMonthlyImpact { get; set; }
        public decimal EstimatedAnnualImpact { get; set; }
    }

    public class SilaCulinaryReversePriceImpactResponseDto
    {
        public string Currency { get; set; } = "AED";
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal PreviousPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal PriceDelta { get; set; }
        public decimal? PriceChangePercent { get; set; }
        public bool AlertTriggered { get; set; }
        public Dictionary<string, int> AffectedCounts { get; set; } = new();
        public decimal TotalMonthlyImpact { get; set; }
        public decimal TotalAnnualImpact { get; set; }
        public List<SilaCulinaryPriceImpactNodeDto> Impacts { get; set; } = new();
    }

    public class SilaCulinarySimulationScenarioInputDto
    {
        public string ScenarioName { get; set; } = "Scenario A";
        public decimal IngredientPriceDeltaPct { get; set; }
        public decimal SellingPriceDeltaPct { get; set; }
        public decimal PortionSizeDeltaPct { get; set; }
        public decimal Covers { get; set; } = 500;
        public decimal OccupancyPct { get; set; } = 80;
        public decimal LaborCostDeltaPct { get; set; }
        public decimal WastagePct { get; set; } = 4;
        public decimal YieldPct { get; set; } = 88;
        public decimal DiscountPct { get; set; }
        public decimal OverheadAmount { get; set; } = 10000;
        public decimal BuffetReplenishmentFactor { get; set; } = 1;
        public decimal SubstitutionSavingsPct { get; set; }
    }

    public class SilaCulinaryRunSimulationRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string Name { get; set; } = "Food Cost & Price Shock Simulation";
        public string Currency { get; set; } = "AED";
        public decimal BaseSellingPricePerCover { get; set; } = 180;
        public decimal BaseFoodCostPerCover { get; set; } = 54;
        public decimal BaseLaborCost { get; set; } = 15000;
        public decimal BaseFixedExpenses { get; set; } = 20000;
        public decimal TargetFoodCostPct { get; set; } = 28;
        public bool SaveSnapshot { get; set; } = true;
        public List<SilaCulinarySimulationScenarioInputDto> Scenarios { get; set; } = new();
    }

    public class SilaCulinarySimulationScenarioResultDto
    {
        public string ScenarioName { get; set; } = string.Empty;
        public decimal Covers { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal RevisedFoodCost { get; set; }
        public decimal? RevisedFoodCostPct { get; set; }
        public decimal RevisedLaborCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal? ContributionMarginPct { get; set; }
        public int? BreakEvenCovers { get; set; }
        public decimal? TargetSellingPriceForFoodCost { get; set; }
        public decimal ExpectedSavingsVsBaseline { get; set; }
        public decimal ProfitabilityDeltaVsBaseline { get; set; }
    }

    public class SilaCulinarySimulationRecordDto
    {
        public Guid Id { get; set; }
        public string SimulationCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = "AED";
        public bool IsolatedFromProduction { get; set; } = true;
        public bool IsShared { get; set; }
        public decimal BaselineContribution { get; set; }
        public decimal SimulatedContribution { get; set; }
        public decimal ExpectedSavings { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<SilaCulinarySimulationScenarioResultDto> Results { get; set; } = new();
    }

    public class SilaCulinaryOptimizationRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string Objective { get; set; } = "MAX_CONTRIBUTION";
        public int TotalTargetPortions { get; set; } = 200;
        public decimal? TargetFoodCostPct { get; set; } = 30.0m;
        public decimal? MaxBudget { get; set; }
        public decimal? MaxKitchenMinutes { get; set; }
        public List<string> ExcludedAllergens { get; set; } = new();
        public List<string> RequiredDietaryTags { get; set; } = new();
        public bool StrictAllergenSafety { get; set; } = true;
    }

    public class SilaCulinaryOptimizationSelectionDto
    {
        public string ItemId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int RecommendedPortions { get; set; }
        public decimal UnitCost { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal TotalCost { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalContribution { get; set; }
    }

    public class SilaCulinaryOptimizationResultDto
    {
        public Guid JobId { get; set; }
        public string JobCode { get; set; } = string.Empty;
        public string Status { get; set; } = "OPTIMAL";
        public string Currency { get; set; } = "AED";
        public string Objective { get; set; } = "MAX_CONTRIBUTION";
        public decimal BaselineCost { get; set; }
        public decimal BaselineContribution { get; set; }
        public decimal ProposedCost { get; set; }
        public decimal ProposedRevenue { get; set; }
        public decimal ProposedContribution { get; set; }
        public decimal? ProposedFoodCostPct { get; set; }
        public decimal ExpectedSavings { get; set; }
        public decimal ExpectedMarginImprovementPct { get; set; }
        public List<string> ConstraintViolations { get; set; } = new();
        public List<string> InfeasibilityReasons { get; set; } = new();
        public List<string> Assumptions { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public List<SilaCulinaryOptimizationSelectionDto> Selections { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
    }

    public class SilaCulinarySubstitutionRequestDto
    {
        public string OriginalMaterialCode { get; set; } = string.Empty;
        public decimal RequiredQty { get; set; } = 1;
        public List<string> ExcludedAllergens { get; set; } = new();
        public List<string> RequiredDietaryTags { get; set; } = new();
    }

    public class SilaCulinarySubstitutionOptionDto
    {
        public string MaterialId { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Classification { get; set; } = "PROPOSED_SUBSTITUTE";
        public decimal OriginalIngredientCost { get; set; }
        public decimal SubstituteIngredientCost { get; set; }
        public decimal RevisedRecipeCost { get; set; }
        public decimal CostSaving { get; set; }
        public decimal YieldImpactPct { get; set; }
        public string AllergenImpact { get; set; } = string.Empty;
        public string NutritionalImpact { get; set; } = string.Empty;
        public string AvailabilityStatus { get; set; } = string.Empty;
        public bool RequiresExecutiveChefApproval { get; set; } = true;
        public bool AutoApprovalBlocked { get; set; }
        public string? BlockingReason { get; set; }
    }

    public class SilaCulinarySubstitutionResponseDto
    {
        public string Currency { get; set; } = "AED";
        public string OriginalMaterialName { get; set; } = string.Empty;
        public decimal OriginalIngredientCost { get; set; }
        public List<SilaCulinarySubstitutionOptionDto> Recommendations { get; set; } = new();
    }

    public class SilaCulinaryBuffetOptimizeRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string MealPeriod { get; set; } = "DINNER";
        public int ExpectedCovers { get; set; } = 200;
        public decimal OccupancyPercent { get; set; } = 85;
    }

    public class SilaCulinaryBuffetDishRecDto
    {
        public string DishId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int InitialProductionPortions { get; set; }
        public int ReplenishmentBatches { get; set; }
        public int ReplenishmentQtyPerBatch { get; set; }
        public int ReplenishmentIntervalMinutes { get; set; }
        public decimal ExpectedConsumptionPortions { get; set; }
        public decimal ExpectedLeftoverPortions { get; set; }
        public decimal ProductionCost { get; set; }
        public decimal FoodCostPerGuest { get; set; }
        public decimal PotentialWasteReductionPortions { get; set; }
        public string FoodSafetyNote { get; set; } = string.Empty;
    }

    public class SilaCulinaryBuffetOptimizeResponseDto
    {
        public string Currency { get; set; } = "AED";
        public string MealPeriod { get; set; } = "DINNER";
        public int ExpectedCovers { get; set; }
        public decimal TotalProductionCost { get; set; }
        public decimal FoodCostPerGuest { get; set; }
        public decimal TotalPotentialWasteReductionValue { get; set; }
        public string FoodSafetyPolicy { get; set; } = "Holding-time limits enforced; leftover buffet food is never recommended for unsafe reuse.";
        public List<SilaCulinaryBuffetDishRecDto> Recommendations { get; set; } = new();
    }

    public class SilaCulinaryEventOptimizeRequestDto
    {
        public Guid? EventFinancialPlanId { get; set; }
        public int? GuestCount { get; set; }
        public decimal? EventBudget { get; set; }
        public decimal? TargetRevenue { get; set; }
        public List<string> ExcludedAllergens { get; set; } = new();
        public List<string> RequiredDietaryTags { get; set; } = new();
    }

    public class SilaCulinaryEventPackageDto
    {
        public string PackageCode { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string MenuCompositionSummary { get; set; } = string.Empty;
        public decimal EstimatedFoodCost { get; set; }
        public decimal EstimatedBeverageCost { get; set; }
        public decimal EstimatedManpowerCost { get; set; }
        public decimal EstimatedEventCost { get; set; }
        public decimal CostPerGuest { get; set; }
        public decimal SellingPricePerGuest { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal ExpectedContribution { get; set; }
        public decimal ExpectedContributionMarginPct { get; set; }
        public decimal EstimatedFullyLoadedProfit { get; set; }
        public decimal IngredientRequirementKg { get; set; }
        public decimal ProcurementRequirementValue { get; set; }
    }

    public class SilaCulinaryEventOptimizeResponseDto
    {
        public string Currency { get; set; } = "AED";
        public int GuestCount { get; set; }
        public List<SilaCulinaryEventPackageDto> Packages { get; set; } = new();
    }

    public class SilaCulinaryForecastRequestDto
    {
        public Guid? PropertyId { get; set; }
        public int HorizonDays { get; set; } = 14;
        public decimal OccupancyAssumptionPct { get; set; } = 80;
    }

    public class SilaCulinaryForecastPointDto
    {
        public int DayIndex { get; set; }
        public string Date { get; set; } = string.Empty;
        public decimal ForecastCovers { get; set; }
        public decimal LowerBound80 { get; set; }
        public decimal UpperBound80 { get; set; }
        public decimal LowerBound95 { get; set; }
        public decimal UpperBound95 { get; set; }
        public decimal ExpectedFoodCost { get; set; }
        public bool IsForecast { get; set; } = true;
    }

    public class SilaCulinaryForecastResponseDto
    {
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = "v3b.1";
        public int HorizonDays { get; set; }
        public bool ColdStartUsed { get; set; }
        public decimal? Mae { get; set; }
        public decimal? WapePercent { get; set; }
        public decimal? ForecastBias { get; set; }
        public string DataQuality { get; set; } = "MODERATE";
        public DateTime LastTrainedUtc { get; set; } = DateTime.UtcNow;
        public List<SilaCulinaryForecastPointDto> ForecastPoints { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class SilaCulinaryWasteRecordCreateDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? LocationId { get; set; }
        public string? OutletName { get; set; }
        public string MealPeriod { get; set; } = "Dinner";
        public Guid? EventId { get; set; }
        public string WasteCategory { get; set; } = "PRODUCTION_WASTE";
        public string IngredientName { get; set; } = string.Empty;
        public Guid? MaterialId { get; set; }
        public Guid? RecipeId { get; set; }
        public Guid? MenuItemId { get; set; }
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal EstimatedCost { get; set; }
        public int CoversServed { get; set; }
        public string SourceChannel { get; set; } = "CLOUD";
        public string? IdempotencyKey { get; set; }
        public string? Notes { get; set; }
    }

    public class SilaCulinaryWasteItemDto
    {
        public Guid Id { get; set; }
        public string WasteNumber { get; set; } = string.Empty;
        public string WasteCategory { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public string OutletName { get; set; } = string.Empty;
        public string MealPeriod { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal EstimatedCost { get; set; }
        public string SourceChannel { get; set; } = "CLOUD";
        public string Status { get; set; } = "RECORDED";
        public DateTime RecordedAtUtc { get; set; }
    }

    public class SilaCulinaryWasteIntelligenceDto
    {
        public string Currency { get; set; } = "AED";
        public decimal TotalWasteCost { get; set; }
        public decimal? WastePercentOfFoodCost { get; set; }
        public decimal? WastePerCover { get; set; }
        public Dictionary<string, decimal> ByCategory { get; set; } = new();
        public Dictionary<string, decimal> ByProperty { get; set; } = new();
        public Dictionary<string, decimal> ByMealPeriod { get; set; } = new();
        public List<string> RecurringPatterns { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<SilaCulinaryWasteItemDto> RecentRecords { get; set; } = new();
    }

    public class SilaCulinaryPropertyBenchmarkRowDto
    {
        public string PropertyId { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string OutletType { get; set; } = "ALL_DAY_DINING";
        public string LocalCurrency { get; set; } = "AED";
        public decimal? FxRateApplied { get; set; }
        public decimal? RevenueReporting { get; set; }
        public decimal? FoodCostReporting { get; set; }
        public decimal? FoodCostPct { get; set; }
        public decimal? CostPerCoverReporting { get; set; }
        public decimal? RevenuePerCoverReporting { get; set; }
        public decimal? ContributionMarginPct { get; set; }
        public decimal? WastePct { get; set; }
        public decimal? WastePerCoverReporting { get; set; }
        public decimal? EventMarginPct { get; set; }
        public string? ComparisonWarning { get; set; }
    }

    public class SilaCulinaryBenchmarkResponseDto
    {
        public string ReportingCurrency { get; set; } = "AED";
        public List<string> Warnings { get; set; } = new();
        public List<SilaCulinaryPropertyBenchmarkRowDto> Rows { get; set; } = new();
    }

    public class SilaCulinaryRecommendationDto
    {
        public Guid Id { get; set; }
        public string RecommendationCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SourceEngine { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CurrentSituation { get; set; } = string.Empty;
        public string ProposedAction { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string? OutletName { get; set; }
        public decimal ExpectedFinancialImpact { get; set; }
        public decimal? RealizedFinancialImpact { get; set; }
        public string Priority { get; set; } = "HIGH";
        public string DataQuality { get; set; } = "VERIFIED";
        public string RiskLevel { get; set; } = "LOW";
        public string ResponsibleRole { get; set; } = "EXECUTIVE_CHEF";
        public string Status { get; set; } = "NEW";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ValidToUtc { get; set; }
    }

    public class SilaCulinaryRecommendationDecisionDto
    {
        public string Status { get; set; } = "APPROVED";
        public string? Comment { get; set; }
        public decimal? RealizedFinancialImpact { get; set; }
    }

    public class SilaCulinaryCopilotRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public string ContextModule { get; set; } = "EXECUTIVE_DASHBOARD";
        public Guid? PropertyId { get; set; }
    }

    public class SilaCulinaryCopilotResponseDto
    {
        public string DirectAnswer { get; set; } = string.Empty;
        public List<string> SupportingMetrics { get; set; } = new();
        public string DataSource { get; set; } = string.Empty;
        public string DateRange { get; set; } = string.Empty;
        public List<string> Assumptions { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public string EstimatedFinancialImpact { get; set; } = string.Empty;
        public string Confidence { get; set; } = "HIGH";
        public string SupportingRoute { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = "LOCAL";
        public bool PromptInjectionBlocked { get; set; }
    }

    public class SilaCulinaryExecutiveDashboardDto
    {
        public string Currency { get; set; } = "AED";
        public bool CanViewFinancials { get; set; }
        public bool CanViewCrossProperty { get; set; }
        public decimal TotalFbRevenue { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal TargetFoodCostPercent { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal? CostPerCover { get; set; }
        public decimal? RevenuePerCover { get; set; }
        public decimal WasteCost { get; set; }
        public decimal? WastePercent { get; set; }
        public decimal EventProfitability { get; set; }
        public decimal ForecastDemandCovers { get; set; }
        public decimal PotentialSavingsPipeline { get; set; }
        public decimal RealizedSavings { get; set; }
        public Dictionary<string, int> QuadrantCounts { get; set; } = new();
        public List<SilaCulinaryMenuEngineeringItemDto> TopProfitableItems { get; set; } = new();
        public List<SilaCulinaryMenuEngineeringItemDto> LowMarginItems { get; set; } = new();
        public List<SilaCulinaryRecommendationDto> TopRecommendations { get; set; } = new();
        public List<string> DataAvailabilityNotes { get; set; } = new();
    }

    public class SilaCulinaryAnalyticsReportCatalogItemDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Formats { get; set; } = new() { "CSV", "EXCEL", "PDF" };
    }

    public class SilaCulinaryAnalyticsReportExportDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Format { get; set; } = "CSV";
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public string Content { get; set; } = string.Empty;
    }

    public class SilaCulinaryMobileOpsSummaryDto
    {
        public string Currency { get; set; } = "AED";
        public bool CanViewCosts { get; set; }
        public decimal DailyExpectedCovers { get; set; }
        public int ActiveShortageAlerts { get; set; }
        public int ActiveWasteAlerts { get; set; }
        public int PendingApprovalTasks { get; set; }
        public decimal? DailyFoodCostPercent { get; set; }
        public decimal TargetFoodCostPercent { get; set; }
        public List<string> ProductionRecommendations { get; set; } = new();
        public List<string> BuffetReplenishmentNotes { get; set; } = new();
        public List<string> EventProductionSummaries { get; set; } = new();
        public List<SilaCulinaryRecommendationDto> ChefRecommendations { get; set; } = new();
    }
}
