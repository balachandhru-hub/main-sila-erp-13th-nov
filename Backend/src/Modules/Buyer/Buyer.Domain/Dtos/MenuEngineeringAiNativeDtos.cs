namespace Buyer.Domain.Dtos
{
    public class MenuEngineeringAgentDescriptorDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string EquivalentExistingAgent { get; set; } = string.Empty;
        public List<string> SupportedIntents { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
        public List<string> AuthorizedTools { get; set; } = new();
        public List<string> DataDependencies { get; set; } = new();
        public int MaxSteps { get; set; } = 15;
        public int TokenBudget { get; set; } = 4000;
        public int TimeoutSeconds { get; set; } = 30;
        public string ModelTier { get; set; } = "LIGHTWEIGHT"; // LIGHTWEIGHT | ADVANCED
        public string RiskLevel { get; set; } = "LOW"; // LOW | MEDIUM | HIGH
        public bool RequiresApprovalForMutations { get; set; } = true;
        public string OutputSchema { get; set; } = string.Empty;
        public string FeedbackEndpoint { get; set; } = "/api/v1/buyer/sila/me-studio/ai/feedback";
    }

    public class MenuEngineeringBusinessToolDescriptorDto
    {
        public string ToolId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RequiredPermission { get; set; } = string.Empty;
        public bool MutatesDraftState { get; set; }
        public bool RequiresApproval { get; set; }
        public string BackingService { get; set; } = string.Empty;
    }

    public class AiOperatingModeStatusDto
    {
        public string OperatingMode { get; set; } = "AI_ASSISTED"; // MANUAL | AI_ASSISTED | AGENTIC
        public bool CanUseAiAssisted { get; set; }
        public bool CanUseAgentic { get; set; }
        public bool AiProviderAvailable { get; set; }
        public string ProviderMode { get; set; } = "DETERMINISTIC_OFFLINE";
        public int DailyUsageCount { get; set; }
        public int DailyUsageLimit { get; set; }
        public int DailyTokensUsed { get; set; }
        public int DailyTokenBudget { get; set; }
        public decimal MonthlyEstimatedCostUsd { get; set; }
        public decimal MonthlyCostBudgetUsd { get; set; }
        public string? FallbackNotice { get; set; }
        public List<MenuEngineeringAgentDescriptorDto> Agents { get; set; } = new();
        public List<MenuEngineeringBusinessToolDescriptorDto> Tools { get; set; } = new();
    }

    public class AiContextualRequestDto
    {
        public string ScreenKey { get; set; } = "silaMenuStudio";
        public string Action { get; set; } = "AnalyzeThis"; // AnalyzeThis | ExplainVariance | RecommendImprovements | FindAlternatives | Optimize | PredictNextPeriod | CompareWithHistory | SimulateChange | GenerateDraft | ShowSupportingEvidence
        public string? UserPrompt { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? MenuPlanId { get; set; }
        public Guid? MenuItemId { get; set; }
        public int? RecipeVersion { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? MealPeriod { get; set; }
        public string? OperatingMode { get; set; }
        public Guid? ScenarioId { get; set; }
        public Dictionary<string, string>? ActiveFilters { get; set; }
    }

    public class AiContextualActionProposalDto
    {
        public string ActionType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; } = true;
        public string TargetEndpoint { get; set; } = string.Empty;
        public Dictionary<string, object?> Payload { get; set; } = new();
    }

    public class AiContextualResponseDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string ScreenKey { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string OperatingMode { get; set; } = "AI_ASSISTED";
        public string ValueTag { get; set; } = "AI_RECOMMENDATION"; // ACTUAL | FORECAST | AI_RECOMMENDATION | SIMULATION
        public string Summary { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public List<string> Evidence { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public string DataQuality { get; set; } = "HIGH";
        public string DataFreshness { get; set; } = string.Empty;
        public decimal Confidence { get; set; } = 0.80m;
        public bool FinancialRedacted { get; set; }
        public bool AiProviderUsed { get; set; }
        public List<AiContextualActionProposalDto> ProposedActions { get; set; } = new();
        public List<string> InvokedTools { get; set; } = new();
    }

    public class MeStudioMultiAgentCommandRequestDto
    {
        public string Objective { get; set; } = string.Empty;
        public string? IdempotencyKey { get; set; }
        public string OperatingMode { get; set; } = "AGENTIC";
        public string OptimizationMode { get; set; } = "BALANCED";
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public DateTime? ServiceDate { get; set; }
        public decimal? ExpectedCovers { get; set; }
        public decimal? SellingPriceTarget { get; set; }
        public decimal? MaxFoodCostPercent { get; set; }
        public List<string>? RequiredDietary { get; set; }
        public List<string>? ExcludedAllergens { get; set; }
        public Dictionary<string, int>? RequiredCategories { get; set; }
        public decimal WeightFoodCost { get; set; } = 0.25m;
        public decimal WeightMargin { get; set; } = 0.25m;
        public decimal WeightPopularity { get; set; } = 0.15m;
        public decimal WeightAvailability { get; set; } = 0.15m;
        public decimal WeightWaste { get; set; } = 0.10m;
        public decimal WeightProduction { get; set; } = 0.05m;
        public decimal WeightDiversity { get; set; } = 0.05m;
        public bool SaveSelectedProposalAsDraft { get; set; } = true;
    }

    public class MultiAgentStepStatusDto
    {
        public int StepNumber { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string ToolId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "COMPLETED"; // PENDING | RUNNING | COMPLETED | SKIPPED | FAILED
        public string OutputSummary { get; set; } = string.Empty;
        public long DurationMs { get; set; }
    }

    public class OptimizedScenarioOptionDto
    {
        public string ScenarioCode { get; set; } = "A";
        public string Label { get; set; } = string.Empty;
        public string Strategy { get; set; } = string.Empty;
        public bool Feasible { get; set; } = true;
        public string? ConflictReason { get; set; }
        public List<Guid> SelectedMenuItemIds { get; set; } = new();
        public List<string> SelectedDishNames { get; set; } = new();
        public decimal FoodCostPerGuest { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal FoodCostPercent { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal PopularityIndex { get; set; }
        public decimal InventoryUtilizationPercent { get; set; }
        public decimal EstimatedWastePercent { get; set; }
        public bool ParetoOptimal { get; set; } = true;
        public string TradeOffSummary { get; set; } = string.Empty;
    }

    public class MeStudioMultiAgentRunResultDto
    {
        public Guid RunId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string Status { get; set; } = "COMPLETED";
        public string Objective { get; set; } = string.Empty;
        public string ServiceFormat { get; set; } = "Buffet";
        public string MealPeriod { get; set; } = "Dinner";
        public decimal ExpectedCovers { get; set; }
        public decimal SellingPricePerGuest { get; set; }
        public decimal MaxFoodCostPercent { get; set; }
        public bool Feasible { get; set; } = true;
        public string? ConflictExplanation { get; set; }
        public string RecommendedScenarioCode { get; set; } = "G";
        public Guid? CreatedDraftMenuId { get; set; }
        public bool AutoPublished { get; set; } = false;
        public bool RequiresApprovalToPublish { get; set; } = true;
        public List<MultiAgentStepStatusDto> Steps { get; set; } = new();
        public List<OptimizedScenarioOptionDto> Scenarios { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public List<string> ProcurementShortages { get; set; } = new();
        public List<string> ProductionConstraints { get; set; } = new();
        public DateTime StartedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public class AiMenuItemAssistRequestDto
    {
        public string DishName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string>? Ingredients { get; set; }
        public Guid? ExistingRecipeId { get; set; }
        public string? PhotographUrl { get; set; }
        public bool PhotographIsAiGenerated { get; set; }
        public string? SupplierProductRef { get; set; }
        public decimal? TargetFoodCostPercent { get; set; } = 30m;
        public string? Language { get; set; } = "en";
    }

    public class AiMenuItemAssistResultDto
    {
        public string SuggestedName { get; set; } = string.Empty;
        public string GeneratedDescription { get; set; } = string.Empty;
        public Dictionary<string, string> MultilingualDescriptions { get; set; } = new();
        public string SuggestedCategory { get; set; } = "Main";
        public string SuggestedCuisine { get; set; } = "International";
        public Guid? MatchedRecipeId { get; set; }
        public string? MatchedRecipeName { get; set; }
        public bool SuggestDraftRecipeConcept { get; set; }
        public string? DraftRecipeConceptStatus { get; set; } // "DRAFT_UNAPPROVED" when no approved recipe exists
        public List<string> DuplicateOrSimilarItems { get; set; } = new();
        public List<string> SuggestedIngredients { get; set; } = new();
        public List<string> ApprovedSubstituteSuggestions { get; set; } = new();
        public List<string> ValidatedAllergens { get; set; } = new();
        public List<string> ValidatedDietaryTags { get; set; } = new();
        public string? AllergenValidationSource { get; set; }
        public decimal RecommendedPortionSize { get; set; } = 1m;
        public string RecommendedPortionUom { get; set; } = "PORTION";
        public decimal? EstimatedFoodCost { get; set; }
        public decimal? RecommendedSellingPrice { get; set; }
        public decimal PredictedPopularityScore { get; set; }
        public decimal? PredictedMargin { get; set; }
        public string SeasonalSuitability { get; set; } = "ALL_SEASON";
        public bool ImageIsIllustrativeOnly { get; set; }
    }

    public class AiMenuCalendarGenerateRequestDto
    {
        public string Horizon { get; set; } = "WEEKLY"; // WEEKLY | MONTHLY
        public DateTime StartDate { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public string MealPeriod { get; set; } = "Dinner";
        public string ServiceFormat { get; set; } = "Buffet";
        public decimal ExpectedCoversPerDay { get; set; } = 200m;
        public bool PrioritizeAvailableInventory { get; set; } = true;
        public bool CreateDraftPlans { get; set; } = false;
    }

    public class AiMenuCalendarDayDto
    {
        public DateTime ServiceDate { get; set; }
        public string MealPeriod { get; set; } = string.Empty;
        public List<Guid> RecommendedMenuItemIds { get; set; } = new();
        public List<string> RecommendedDishNames { get; set; } = new();
        public decimal InventoryCoveragePercent { get; set; }
        public decimal EstimatedFoodCostPercent { get; set; }
        public List<string> Warnings { get; set; } = new();
        public Guid? CreatedDraftPlanId { get; set; }
    }

    public class AiMenuCalendarGenerateResultDto
    {
        public string Horizon { get; set; } = "WEEKLY";
        public List<AiMenuCalendarDayDto> Days { get; set; } = new();
        public List<string> DetectedRepetitionIssues { get; set; } = new();
        public List<string> ExpiryUtilizationOpportunities { get; set; } = new();
        public List<string> CapacityConflicts { get; set; } = new();
        public List<string> LowPerformerWarnings { get; set; } = new();
        public bool RequiresUserApproval { get; set; } = true;
    }

    public class AiPortionOptimizeResultDto
    {
        public List<AiPortionRecommendationRowDto> Rows { get; set; } = new();
        public decimal TotalProjectedFoodCostDelta { get; set; }
        public decimal TotalProjectedWasteReductionKg { get; set; }
        public string SafetyGuardNotice { get; set; } = "Approved master recipe portions remain unchanged; recommendations apply to predictive planning buffers only and enforce food-safety minimums.";
    }

    public class AiPortionRecommendationRowDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public decimal StandardPortion { get; set; }
        public decimal CurrentPlannedPortion { get; set; }
        public decimal? AvgActualSampledPortion { get; set; }
        public decimal RecommendedPlannedPortion { get; set; }
        public decimal RecommendedParticipationRate { get; set; }
        public decimal RecommendedServingsPerGuest { get; set; }
        public decimal RecommendedBufferPercent { get; set; }
        public decimal RecommendedBatchSize { get; set; }
        public decimal RecommendedReplenishmentQty { get; set; }
        public string VariancePattern { get; set; } = "BALANCED"; // OVER_PORTIONING | UNDER_PORTIONING | BALANCED
        public decimal ExpectedCostImpact { get; set; }
        public decimal ExpectedWasteImpactPercent { get; set; }
        public decimal Confidence { get; set; }
    }

    public class AiBacktestDemandResultDto
    {
        public string SelectedModel { get; set; } = string.Empty;
        public string SelectedModelVersion { get; set; } = string.Empty;
        public string DataQuality { get; set; } = "MODERATE";
        public bool ColdStart { get; set; }
        public string DataFreshness { get; set; } = string.Empty;
        public List<AiDemandForecastDayDto> HorizonDays { get; set; } = new();
        public List<AiDemandModelMetricDto> ModelEvaluations { get; set; } = new();
        public List<string> Drivers { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class AiDemandForecastDayDto
    {
        public DateTime BusinessDate { get; set; }
        public decimal HistoricalBaseline { get; set; }
        public decimal ForecastQuantity { get; set; }
        public decimal LowerBound { get; set; }
        public decimal UpperBound { get; set; }
        public decimal ConfirmedEventsQty { get; set; }
        public decimal RecommendedDemand { get; set; }
    }

    public class AiDemandModelMetricDto
    {
        public string ModelName { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public decimal Mae { get; set; }
        public decimal Wape { get; set; }
        public decimal Bias { get; set; }
        public decimal IntervalCoveragePercent { get; set; }
        public bool Selected { get; set; }
    }

    public class AiPosSalesIntelligenceDto
    {
        public string DailySummary { get; set; } = string.Empty;
        public string WeeklySummary { get; set; } = string.Empty;
        public List<AiPosAnomalyItemDto> Anomalies { get; set; } = new();
        public List<string> RevenueOpportunities { get; set; } = new();
        public string Disclaimer { get; set; } = "Statistical anomalies and correlations are surfaced for investigation; they are not confirmed causes or fraud determinations without verified audit evidence.";
    }

    public class AiPosAnomalyItemDto
    {
        public string AnomalyType { get; set; } = string.Empty; // SALES_DROP | SALES_SPIKE | ABNORMAL_DISCOUNT | EXCESS_COMP | REFUND_PATTERN | VOID_PATTERN | MISSING_SEQUENCE | UNEXPECTED_PRICE | UNMAPPED_HIGH_VOLUME
        public string Severity { get; set; } = "WARNING";
        public string PosCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public decimal ActualValue { get; set; }
        public decimal ExpectedValue { get; set; }
        public decimal VariancePercent { get; set; }
        public string ComparisonPeriod { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public List<string> PossibleContributingFactors { get; set; } = new();
    }

    public class AiPosMappingSuggestionBatchDto
    {
        public List<AiPosMappingCandidateRowDto> Queue { get; set; } = new();
        public List<string> DuplicateConflicts { get; set; } = new();
        public int AutoMappableDeterministicCount { get; set; }
        public int HumanReviewRequiredCount { get; set; }
    }

    public class AiPosMappingCandidateRowDto
    {
        public string PosCode { get; set; } = string.Empty;
        public string PosDescription { get; set; } = string.Empty;
        public decimal ObservedQuantity { get; set; }
        public Guid? CandidateMenuItemId { get; set; }
        public string? CandidateMenuItemName { get; set; }
        public Guid? CandidateRecipeId { get; set; }
        public decimal Confidence { get; set; }
        public decimal SuggestedPortionMultiplier { get; set; } = 1m;
        public string? SuggestedSizeVariant { get; set; }
        public string? SuggestedModifier { get; set; }
        public string? AdditionalRecipeHint { get; set; }
        public string MatchingEvidence { get; set; } = string.Empty;
        public bool RequiresHumanReview { get; set; } = true;
        public List<string> Alternatives { get; set; } = new();
    }

    public class AiConsumptionRootCauseDto
    {
        public List<AiConsumptionAnomalyCauseDto> Anomalies { get; set; } = new();
        public string LedgerGuardNotice { get; set; } = "AI Consumption Intelligence prepares investigation worklists only; inventory ledger balances are never auto-adjusted.";
    }

    public class AiConsumptionAnomalyCauseDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal TheoreticalQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal VarianceQty { get; set; }
        public decimal VarianceValue { get; set; }
        public List<AiRankedCauseDto> RankedCauses { get; set; } = new();
    }

    public class AiRankedCauseDto
    {
        public string CauseCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ProbabilityScore { get; set; }
        public string SupportingEvidence { get; set; } = string.Empty;
    }

    public class AiPriceShockRequestDto
    {
        public string IngredientQuery { get; set; } = string.Empty;
        public decimal PriceChangePercent { get; set; }
        public Guid? PropertyId { get; set; }
    }

    public class AiPriceShockResultDto
    {
        public string IngredientName { get; set; } = string.Empty;
        public decimal PriceChangePercent { get; set; }
        public List<AiPriceShockAffectedRecipeDto> AffectedRecipes { get; set; } = new();
        public List<AiPriceShockAffectedMenuItemDto> AffectedMenuItems { get; set; } = new();
        public List<AiPriceShockAffectedMenuDto> AffectedMenus { get; set; } = new();
        public decimal TotalProjectedCostDelta { get; set; }
        public string RecommendedCorrectiveAction { get; set; } = string.Empty;
    }

    public class AiPriceShockAffectedRecipeDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal CurrentBatchCost { get; set; }
        public decimal PredictedBatchCost { get; set; }
        public decimal CostDelta { get; set; }
    }

    public class AiPriceShockAffectedMenuItemDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public decimal CurrentUnitCost { get; set; }
        public decimal PredictedUnitCost { get; set; }
        public decimal CurrentMargin { get; set; }
        public decimal PredictedMargin { get; set; }
        public decimal? PredictedFoodCostPercent { get; set; }
    }

    public class AiPriceShockAffectedMenuDto
    {
        public Guid MenuPlanId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public string LifecycleStatus { get; set; } = string.Empty;
        public DateTime ServiceDate { get; set; }
        public decimal ProjectedMenuCostDelta { get; set; }
    }

    public class AiPricingRecommendationsResultDto
    {
        public List<AiPricingRecommendationRowDto> Items { get; set; } = new();
        public string ElasticityNotice { get; set; } = "Sales quantity is held constant unless validated historical price-elasticity data exceeds minimum confidence thresholds. Published selling prices are never updated without approval.";
    }

    public class AiPricingRecommendationRowDto
    {
        public Guid MenuItemId { get; set; }
        public string MenuItemName { get; set; } = string.Empty;
        public decimal CurrentNetPrice { get; set; }
        public decimal CurrentUnitCost { get; set; }
        public decimal MinimumPriceForTargetMargin { get; set; }
        public decimal TargetFoodCostPrice { get; set; }
        public decimal RecommendedPriceLow { get; set; }
        public decimal RecommendedPriceHigh { get; set; }
        public decimal ExpectedContributionDelta { get; set; }
        public bool ElasticityValidated { get; set; }
        public string AssumptionNote { get; set; } = string.Empty;
    }

    public class AiNlSimulationRequestDto
    {
        public string Command { get; set; } = string.Empty;
        public Guid? MenuPlanId { get; set; }
        public Guid? PropertyId { get; set; }
        public bool SaveScenario { get; set; } = false;
    }

    public class AiNlSimulationResultDto
    {
        public string ParsedIntent { get; set; } = string.Empty;
        public string SanitizedCommand { get; set; } = string.Empty;
        public WhatIfSimulationRequestDto TranslatedInputs { get; set; } = new();
        public WhatIfSimulationResultDto Simulation { get; set; } = new();
        public AiPriceShockResultDto? PriceShock { get; set; }
        public List<string> Constraints { get; set; } = new();
        public List<string> DataLimitations { get; set; } = new();
        public bool LiveDataMutated { get; set; } = false;
    }

    public class MeRecommendationCockpitItemDto
    {
        public Guid Id { get; set; }
        public string RecommendationCode { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public string? TargetObjectType { get; set; }
        public Guid? TargetObjectId { get; set; }
        public string? TargetObjectLabel { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CurrentBaseline { get; set; } = string.Empty;
        public string ProposedChange { get; set; } = string.Empty;
        public string SupportingEvidence { get; set; } = string.Empty;
        public decimal? EstimatedCostImpact { get; set; }
        public decimal? EstimatedMarginImpact { get; set; }
        public decimal? RealizedFinancialImpact { get; set; }
        public string? OperationalImpact { get; set; }
        public string DataQuality { get; set; } = "HIGH";
        public decimal Confidence { get; set; }
        public string Priority { get; set; } = "MEDIUM";
        public Guid? ResponsibleUserId { get; set; }
        public bool RequiresApproval { get; set; }
        public string Status { get; set; } = "NEW";
        public int? FeedbackRating { get; set; }
        public string? FeedbackNotes { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class MeRecommendationLifecycleActionDto
    {
        public string Action { get; set; } = "ACCEPT"; // REVIEW | ACCEPT | REJECT | APPROVE | IMPLEMENT | VALIDATE_OUTCOME | CLOSE | ASSIGN | EDIT_PROPOSAL
        public string? Notes { get; set; }
        public string? EditedProposalJson { get; set; }
        public Guid? ResponsibleUserId { get; set; }
        public decimal? RealizedFinancialImpact { get; set; }
        public decimal? ActualFoodCostPercent { get; set; }
        public decimal? ActualMargin { get; set; }
        public decimal? ActualCovers { get; set; }
        public decimal? ActualWastePercent { get; set; }
        public int? FeedbackRating { get; set; }
    }

    public class MeProactiveAlertDto
    {
        public Guid Id { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public string Severity { get; set; } = "WARNING";
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? AffectedObjectType { get; set; }
        public Guid? AffectedObjectId { get; set; }
        public string? RecommendedAction { get; set; }
        public string Status { get; set; } = "OPEN";
        public DateTime CreatedAtUtc { get; set; }
        public DateTime CooldownUntilUtc { get; set; }
    }

    public class MeSetupWizardReadinessDto
    {
        public decimal OverallReadinessScore { get; set; }
        public int TotalSteps { get; set; }
        public int CompletedCount { get; set; }
        public int PendingCount { get; set; }
        public int BlockedCount { get; set; }
        public List<MeSetupWizardStepDto> Steps { get; set; } = new();
        public List<MeAiSuggestedThresholdDto> SuggestedThresholds { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> RequiredActions { get; set; } = new();
        public bool AdminApprovalRequiredToApply { get; set; } = true;
    }

    public class MeSetupWizardStepDto
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = string.Empty;
        public string Category { get; set; } = "CORE";
        public bool IsOptional { get; set; }
        public decimal ReadinessPercent { get; set; }
        public string Status { get; set; } = "READY"; // READY | NEEDS_ATTENTION | BLOCKED
        public string Detail { get; set; } = string.Empty;
        public string? RequiredAction { get; set; }
    }

    public class MeAiSuggestedThresholdDto
    {
        public string SettingKey { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = string.Empty;
        public string SuggestedValue { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
    }

    public class MeLearningSummaryDto
    {
        public string ActiveDatasetVersion { get; set; } = "ds-v1";
        public string ActiveModelVersion { get; set; } = string.Empty;
        public int TotalValidatedOutcomes { get; set; }
        public decimal? AvgForecastErrorPercent { get; set; }
        public decimal? AvgRealizedMarginDelta { get; set; }
        public List<AiDemandModelMetricDto> ModelEvaluations { get; set; } = new();
        public List<MeLearningOutcomeRowDto> RecentOutcomes { get; set; } = new();
    }

    public class MeLearningOutcomeRowDto
    {
        public Guid Id { get; set; }
        public string DatasetVersion { get; set; } = string.Empty;
        public string ModelVersion { get; set; } = string.Empty;
        public Guid? MenuPlanId { get; set; }
        public Guid? RecommendationId { get; set; }
        public decimal? PredictedFoodCostPercent { get; set; }
        public decimal? ActualFoodCostPercent { get; set; }
        public decimal? PredictedMargin { get; set; }
        public decimal? ActualMargin { get; set; }
        public bool OutcomeValidated { get; set; }
        public string OutcomeSource { get; set; } = string.Empty;
        public DateTime CapturedAtUtc { get; set; }
    }

    public class MeAiFeedbackSubmitDto
    {
        public string AgentId { get; set; } = string.Empty;
        public string? ContextScreen { get; set; }
        public Guid? TargetEntityId { get; set; }
        public int Rating { get; set; } = 5;
        public bool WasAccepted { get; set; } = true;
        public string? CorrectionNotes { get; set; }
    }

    public class SilaStoreRoleIntelligenceDto
    {
        public string RolePersona { get; set; } = "EXECUTIVE_CHEF"; // EXECUTIVE_CHEF | STOREKEEPER | COST_CONTROLLER | FB_MANAGER
        public List<MeProactiveAlertDto> RoleAlerts { get; set; } = new();
        public List<MeRecommendationCockpitItemDto> RoleRecommendations { get; set; } = new();
        public List<string> Highlights { get; set; } = new();
        public List<string> QuickActions { get; set; } = new();
        public ExecutiveBriefingDto? DailyBriefing { get; set; }
        public List<ActionItemDto> PendingApprovals { get; set; } = new();
        public List<string> AssignedTasks { get; set; } = new();
    }
}
