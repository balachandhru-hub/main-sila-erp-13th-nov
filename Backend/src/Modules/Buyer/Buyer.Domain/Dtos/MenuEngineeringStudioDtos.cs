namespace Buyer.Domain.Dtos
{
    public class MenuEngineeringPermissionSetDto
    {
        public bool MenuEngineeringEnabled { get; set; } = true;
        public bool MeStudioEnabled { get; set; } = true;
        public bool ViewMeStudio { get; set; }
        public bool CreateMenu { get; set; }
        public bool EditDraftMenu { get; set; }
        public bool ManageMenuItems { get; set; }
        public bool ManagePortionPlans { get; set; }
        public bool ManageDemandPlans { get; set; }
        public bool ViewPosSales { get; set; }
        public bool ManagePosMapping { get; set; }
        public bool ViewMenuPerformance { get; set; }
        public bool ViewMenuCost { get; set; }
        public bool ViewMenuMargin { get; set; }
        public bool GeneratePurchaseRecommendation { get; set; }
        public bool UseAiMenuIntelligence { get; set; }
        public bool RunMenuSimulation { get; set; }
        public bool ApproveMenu { get; set; }
        public bool PublishMenu { get; set; }
        public bool ManageMenuSetup { get; set; }
    }

    public class MenuItemEnhancedWriteDto : SilaMenuItemWriteDto
    {
        public string? AlternativeName { get; set; }
        public string? ServiceFormat { get; set; }
        public int? ApprovedRecipeVersion { get; set; }
        public string? RecipeCombinationJson { get; set; }
        public decimal? TaxRatePercent { get; set; }
        public bool TaxInclusive { get; set; }
        public string? PosItemReference { get; set; }
        public int? PrepTimeMinutes { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string? SeasonalAvailability { get; set; }
        public string? ApprovalStatus { get; set; }
    }

    public class MenuItemEnhancedDto : MenuItemEnhancedWriteDto
    {
        public string MenuItemCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string RecipeStatus { get; set; } = string.Empty;
        public int VersionNumber { get; set; } = 1;
        public decimal? IngredientCost { get; set; }
        public decimal? CostPerPortion { get; set; }
        public decimal? NetSellingPrice { get; set; }
        public decimal? TaxInclusiveSellingPrice { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? ContributionMargin { get; set; }
        public decimal HistoricalSalesQty { get; set; }
        public decimal PopularityIndex { get; set; }
        public string MatrixClassification { get; set; } = "UNCLASSIFIED";
        public string CostState { get; set; } = "COST_PENDING";
        public string IngredientAvailabilityStatus { get; set; } = "UNKNOWN";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MenuItemVersionDto
    {
        public Guid Id { get; set; }
        public Guid MenuItemId { get; set; }
        public int VersionNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal? SellingPrice { get; set; }
        public decimal? IngredientCost { get; set; }
        public Guid? LinkedRecipeId { get; set; }
        public int? ApprovedRecipeVersion { get; set; }
        public string? ChangeSummary { get; set; }
        public DateTime ActionAtUtc { get; set; }
    }

    public class MenuItemBulkPriceUpdateDto
    {
        public List<Guid> MenuItemIds { get; set; } = new();
        public decimal? PercentageChange { get; set; }
        public decimal? FixedAmountChange { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public string? Reason { get; set; }
    }

    public class MenuItemBulkImportDto
    {
        public string Format { get; set; } = "CSV";
        public string Payload { get; set; } = string.Empty;
    }

    public class MenuPlanLineEnhancedWriteDto : SilaMenuPlanLineWriteDto
    {
        public string? CategoryName { get; set; }
        public int SequenceOrder { get; set; }
        public Guid? RecipeId { get; set; }
        public int? RecipeVersion { get; set; }
        public decimal? PortionSize { get; set; }
        public string? PortionUom { get; set; }
        public decimal? ExpectedServingsPerGuest { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? ExpectedCovers { get; set; }
        public decimal? SampledActualPortion { get; set; }
        public string? LineNotes { get; set; }
        public string? AlternativesJson { get; set; }
        public string? DietaryTags { get; set; }
    }

    public class MenuPlanLineEnhancedDto : MenuPlanLineEnhancedWriteDto
    {
        public string MenuItemName { get; set; } = string.Empty;
        public string MenuItemCode { get; set; } = string.Empty;
        public string RecipeLinkStatus { get; set; } = string.Empty;
        public string? RecipeName { get; set; }
        public string? ImagePath { get; set; }
        public string? AllergenNotes { get; set; }
        public decimal RecommendedPortions { get; set; }
        public decimal RequiredUsableOutput { get; set; }
        public bool PortionsConfirmed { get; set; }
        public decimal? ActualPortions { get; set; }
        public decimal? Variance { get; set; }
        public decimal? PortionCost { get; set; }
        public decimal? TotalLineCost { get; set; }
        public decimal? ExpectedLineRevenue { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? ContributionMargin { get; set; }
        public string AvailabilityStatus { get; set; } = "READY";
        public string? Warning { get; set; }
    }

    public class MenuPlanEnhancedWriteDto
    {
        public Guid? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public DateTime BusinessDate { get; set; } = DateTime.UtcNow.Date;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? MealPeriod { get; set; }
        public string? EventName { get; set; }
        public string? MenuType { get; set; }
        public string? ServiceFormat { get; set; }
        public string? VenueName { get; set; }
        public string? RotationRule { get; set; }
        public bool IsTemplate { get; set; }
        public string? TemplateCategory { get; set; }
        public decimal? ExpectedGuests { get; set; }
        public decimal? ExpectedCovers { get; set; }
        public decimal? MealParticipation { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public decimal? TargetContributionMargin { get; set; }
        public string? DietaryRequirements { get; set; }
        public string? Notes { get; set; }
        public Guid? CopyFromPlanId { get; set; }
        public string? SourceType { get; set; }
        public Guid? SourceReferenceId { get; set; }
        public bool PreventConflicts { get; set; } = true;
        public List<string> CategoriesOrder { get; set; } = new();
        public List<MenuPlanLineEnhancedWriteDto> Lines { get; set; } = new();
    }

    public class MenuPlanEnhancedDto
    {
        public Guid Id { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public Guid? OutletLocationId { get; set; }
        public string? OutletName { get; set; }
        public DateTime BusinessDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? MealPeriod { get; set; }
        public string? EventName { get; set; }
        public string? MenuType { get; set; }
        public string? ServiceFormat { get; set; }
        public string? VenueName { get; set; }
        public string? RotationRule { get; set; }
        public bool IsTemplate { get; set; }
        public string? TemplateCategory { get; set; }
        public decimal? ExpectedGuests { get; set; }
        public decimal? ExpectedCovers { get; set; }
        public decimal? MealParticipation { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public decimal? TargetContributionMargin { get; set; }
        public string? DietaryRequirements { get; set; }
        public string Status { get; set; } = "DRAFT";
        public int VersionNumber { get; set; } = 1;
        public Guid? ParentPublishedPlanId { get; set; }
        public string? SourceType { get; set; }
        public Guid? SourceReferenceId { get; set; }
        public string? Notes { get; set; }
        public string? ApprovalComment { get; set; }
        public DateTime? SubmittedAtUtc { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public DateTime? PublishedAtUtc { get; set; }
        public bool PreventConflicts { get; set; }
        public List<string> CategoriesOrder { get; set; } = new();
        public List<MenuPlanLineEnhancedDto> Lines { get; set; } = new();
    }

    public class MenuPlanTransitionDto
    {
        public Guid MenuPlanId { get; set; }
        public string TargetStatus { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime? ScheduledDateUtc { get; set; }
        public bool TriggerPosPublish { get; set; }
        public bool TriggerProductionHandoff { get; set; }
    }

    public class MeStudioLiveIntelligenceDto
    {
        public decimal? TotalMenuCost { get; set; }
        public decimal? CostPerGuest { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal? ExpectedFoodCostPercent { get; set; }
        public decimal? ExpectedContributionMargin { get; set; }
        public decimal IngredientReadinessPercent { get; set; }
        public int AvailableIngredients { get; set; }
        public int ShortIngredients { get; set; }
        public decimal TotalPurchaseRequirementValue { get; set; }
        public decimal ExpectedWasteQty { get; set; }
        public decimal? HistoricalAvgMarginPercent { get; set; }
        public List<string> RiskIndicators { get; set; } = new();
        public List<AiMenuRecommendationCardDto> AiRecommendations { get; set; } = new();
    }

    public class MeStudioWorkspaceDto
    {
        public MenuEngineeringPermissionSetDto Permissions { get; set; } = new();
        public SilaMenuLookupsDto Lookups { get; set; } = new();
        public List<MenuItemEnhancedDto> LibraryItems { get; set; } = new();
        public List<MenuPlanEnhancedDto> Menus { get; set; } = new();
        public MenuPlanEnhancedDto? ActiveMenu { get; set; }
        public MeStudioLiveIntelligenceDto LiveIntelligence { get; set; } = new();
        public List<SilaMenuRequirementRowDto> IngredientRequirements { get; set; } = new();
        public MenuPerformanceMatrixDto? PerformanceMatrix { get; set; }
        public List<HistoricalMenuSummaryDto> HistoricalMenus { get; set; } = new();
        public List<MenuEngineeringScenarioDto> Scenarios { get; set; } = new();
        public List<MenuEngineeringApprovalHistoryDto> ApprovalHistory { get; set; } = new();
        public MenuEngineeringSetupDto Setup { get; set; } = new();
    }

    public class MenuEngineeringApprovalHistoryDto
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public int VersionNumber { get; set; }
        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;
        public Guid? ActionByUserId { get; set; }
        public string? ApproverRole { get; set; }
        public string? Reason { get; set; }
        public DateTime ActionAtUtc { get; set; }
    }

    public class PortionSampleWriteDto
    {
        public Guid MenuPlanLineId { get; set; }
        public decimal SampledActualPortion { get; set; }
        public string? Note { get; set; }
    }

    public class PortionPlanRowDto
    {
        public Guid MenuPlanLineId { get; set; }
        public Guid MenuPlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string MenuItemName { get; set; } = string.Empty;
        public string? ServiceFormat { get; set; }
        public string? MealPeriod { get; set; }
        public decimal ExpectedCovers { get; set; }
        public decimal ParticipationRate { get; set; }
        public decimal ExpectedServingsPerGuest { get; set; }
        public decimal StandardPortion { get; set; }
        public decimal PlannedPortion { get; set; }
        public decimal? SampledActualPortion { get; set; }
        public string PortionUom { get; set; } = "PORTION";
        public decimal ExpectedPortions { get; set; }
        public decimal RequiredUsableOutput { get; set; }
        public decimal ProductionBufferPercent { get; set; }
        public decimal RecipeYieldPercent { get; set; } = 100m;
        public decimal GrossIngredientNeedFactor { get; set; }
        public decimal? HistoricalPortionVariancePercent { get; set; }
        public string VarianceStatus { get; set; } = "ON_TARGET";
        public decimal? PortionCost { get; set; }
    }

    public class DemandForecastRowDto
    {
        public DateTime BusinessDate { get; set; }
        public string MealPeriod { get; set; } = "All";
        public string? OutletName { get; set; }
        public decimal? OccupancyPercent { get; set; }
        public decimal HistoricalActualDemand { get; set; }
        public decimal BaselineForecast { get; set; }
        public decimal AiAdjustedForecast { get; set; }
        public decimal ConfirmedReservations { get; set; }
        public decimal ConfirmedEvents { get; set; }
        public decimal ConfirmedDemandDeduped { get; set; }
        public decimal RecommendedDemand { get; set; }
        public decimal LowerRange { get; set; }
        public decimal UpperRange { get; set; }
        public decimal? ForecastAccuracyPercent { get; set; }
        public string ForecastModelVersion { get; set; } = "SILA-HYBRID-V2";
        public bool ColdStartLimited { get; set; }
        public string? ColdStartNote { get; set; }
        public decimal? ManualOverrideQty { get; set; }
        public string? OverrideReason { get; set; }
    }

    public class DemandForecastBoardDto
    {
        public SilaPmsDemandDto? LatestPms { get; set; }
        public List<DemandForecastRowDto> Rows { get; set; } = new();
        public List<MenuDemandOverrideDto> Overrides { get; set; } = new();
        public bool ColdStartLimited { get; set; }
        public string? ColdStartMessage { get; set; }
    }

    public class DemandOverrideWriteDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? MenuItemId { get; set; }
        public DateTime BusinessDate { get; set; }
        public string? MealPeriod { get; set; }
        public decimal OverrideQty { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class MenuDemandOverrideDto
    {
        public Guid Id { get; set; }
        public DateTime BusinessDate { get; set; }
        public string? MealPeriod { get; set; }
        public decimal BaselineForecast { get; set; }
        public decimal AiAdjustedForecast { get; set; }
        public decimal ConfirmedDemand { get; set; }
        public decimal OverrideQty { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime OverriddenAtUtc { get; set; }
    }

    public class PosSaleExtendedRowDto : SilaMenuPosSaleRowDto
    {
        public string? PosLineId { get; set; }
        public DateTime? TransactionTimestampUtc { get; set; }
        public decimal? GrossSellingPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? NetRevenue { get; set; }
        public bool IsVoid { get; set; }
        public bool IsRefund { get; set; }
        public bool IsComplimentary { get; set; }
        public string? ModifierCode { get; set; }
        public string? ComboReference { get; set; }
        public string? MealPeriod { get; set; }
    }

    public class PosSalesExtendedDto
    {
        public decimal QtySold { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal NetRevenue { get; set; }
        public int TransactionCount { get; set; }
        public int UnmappedCount { get; set; }
        public int VoidOrRefundCount { get; set; }
        public DateTime? LastSync { get; set; }
        public string SyncStatus { get; set; } = "READY";
        public string ConnectorNote { get; set; } = "Preserves source POS transactions; file-based CSV/JSON and configured SAP/MICROS adapters supported.";
        public List<PosSaleExtendedRowDto> Rows { get; set; } = new();
    }

    public class PosMappingExtendedWriteDto : SilaMenuPosMappingWriteDto
    {
        public Guid? RecipeId { get; set; }
        public int? ApprovedRecipeVersion { get; set; }
        public string MappingType { get; set; } = "ONE_TO_ONE";
        public decimal PortionMultiplier { get; set; } = 1m;
        public string? UomCode { get; set; }
        public decimal UomConversionFactor { get; set; } = 1m;
        public string? ModifierCode { get; set; }
        public Guid? ModifierRecipeId { get; set; }
        public decimal? ModifierPortionMultiplier { get; set; }
        public string? ComboParentCode { get; set; }
        public bool IsComboComponent { get; set; }
        public bool ExcludeComponentDoubleCount { get; set; } = true;
        public bool ApproveMapping { get; set; }
        public string? MappingNotes { get; set; }
    }

    public class PosMappingExtendedDto : SilaMenuPosMappingDto
    {
        public Guid? RecipeId { get; set; }
        public string? RecipeName { get; set; }
        public int? ApprovedRecipeVersion { get; set; }
        public string MappingType { get; set; } = "ONE_TO_ONE";
        public decimal PortionMultiplier { get; set; } = 1m;
        public string? UomCode { get; set; }
        public decimal UomConversionFactor { get; set; } = 1m;
        public string? ModifierCode { get; set; }
        public Guid? ModifierRecipeId { get; set; }
        public decimal? ModifierPortionMultiplier { get; set; }
        public string? ComboParentCode { get; set; }
        public bool IsComboComponent { get; set; }
        public bool ExcludeComponentDoubleCount { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public string? MappingNotes { get; set; }
        public string ValidationWarning { get; set; } = string.Empty;
    }

    public class PosMappingWorkbenchDto
    {
        public List<PosMappingExtendedDto> Mappings { get; set; } = new();
        public List<PosMappingExtendedDto> UnmappedQueue { get; set; } = new();
        public int MappedCount { get; set; }
        public int UnmappedCount { get; set; }
        public int ComboCount { get; set; }
        public List<string> ReprocessWarnings { get; set; } = new();
    }

    public class MenuPerformanceMatrixRowDto : SilaMenuPerformanceRowDto
    {
        public string? Cuisine { get; set; }
        public string? MealPeriod { get; set; }
        public decimal? AverageNetSellingPrice { get; set; }
        public decimal? TotalIngredientCost { get; set; }
        public decimal? TotalContributionMargin { get; set; }
        public decimal ExpectedPopularityPercent { get; set; }
        public decimal CategoryPopularityThresholdPercent { get; set; }
        public decimal? CategoryWeightedAvgMargin { get; set; }
        public decimal? SalesGrowthPercent { get; set; }
        public decimal? MarginTrendPercent { get; set; }
        public decimal? WasteImpactCost { get; set; }
        public string CostBasis { get; set; } = "FOOD_COST_ONLY";
        public string CostSourceType { get; set; } = "CURRENT_RECIPE_ESTIMATE";
        public string? ExclusionReason { get; set; }
        public string? PriorClassification { get; set; }
        public string ClassificationMovement { get; set; } = "UNCHANGED";
        public string RecommendedAction { get; set; } = string.Empty;
        public decimal? EstimatedFinancialImpact { get; set; }
    }

    public class MenuMatrixCategorySummaryDto
    {
        public string Category { get; set; } = "Uncategorized";
        public int EligibleItems { get; set; }
        public int ExcludedItems { get; set; }
        public decimal TotalSalesQty { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal ExpectedPopularityPercent { get; set; }
        public decimal PopularityThresholdPercent { get; set; }
        public decimal? WeightedAvgContributionMargin { get; set; }
    }

    public class MenuPerformanceMatrixDto
    {
        public string CostBasis { get; set; } = "FOOD_COST_ONLY";
        public decimal PopularityThresholdFactor { get; set; } = 0.70m;
        public List<MenuPerformanceMatrixRowDto> Rows { get; set; } = new();
        public List<MenuPerformanceMatrixRowDto> ExcludedRows { get; set; } = new();
        public List<MenuMatrixCategorySummaryDto> CategorySummaries { get; set; } = new();
        public int Stars { get; set; }
        public int Plowhorses { get; set; }
        public int Puzzles { get; set; }
        public int Dogs { get; set; }
        public int ExcludedCount { get; set; }
    }

    public class ConsumptionVarianceRowDto : SilaMenuConsumptionRowDto
    {
        public decimal ExpectedYieldLossQty { get; set; }
        public decimal RecordedWasteQty { get; set; }
        public decimal UnexplainedVarianceQty { get; set; }
        public string SourceNotes { get; set; } = string.Empty;
    }

    public class ConsumptionVarianceDetailDto
    {
        public decimal Theoretical { get; set; }
        public decimal Actual { get; set; }
        public decimal ExpectedYieldLoss { get; set; }
        public decimal RecordedWaste { get; set; }
        public decimal TotalVariance { get; set; }
        public decimal UnexplainedVariance { get; set; }
        public decimal? VarianceValue { get; set; }
        public bool DoubleCountingPrevented { get; set; } = true;
        public List<ConsumptionVarianceRowDto> Rows { get; set; } = new();
    }

    public class SupplierPriceCompareDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal? UnitPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsPreferred { get; set; }
    }

    public class CostMarginAnalysisDto
    {
        public List<MenuPerformanceMatrixRowDto> ItemRows { get; set; } = new();
        public List<SilaMenuInsightDto> MarginAlerts { get; set; } = new();
        public List<SupplierPriceCompareDto> SupplierComparisons { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public decimal? TotalFoodCost { get; set; }
        public decimal? OverallFoodCostPercent { get; set; }
        public decimal? TotalContributionMargin { get; set; }
    }

    public class MenuPurchasePlanRowDto : SilaMenuRequirementRowDto
    {
        public decimal ReservedStock { get; set; }
        public decimal BlockedStock { get; set; }
        public decimal ExpiringStock { get; set; }
        public decimal EligibleAvailable { get; set; }
        public decimal EligibleIncomingOnTime { get; set; }
        public decimal LateOpenPoExcluded { get; set; }
        public decimal? MinimumOrderQty { get; set; }
        public decimal? PackSize { get; set; }
        public decimal RecommendedPurchaseQty { get; set; }
        public decimal? ContractPrice { get; set; }
        public decimal? EstimatedPurchaseCost { get; set; }
        public string? RecommendedSupplier { get; set; }
        public string? SubstituteSuggestion { get; set; }
        public string? StockTransferSuggestion { get; set; }
        public bool TimePhasedValid { get; set; } = true;
    }

    public class MenuPurchasePlanDto
    {
        public List<MenuPurchasePlanRowDto> Rows { get; set; } = new();
        public SilaPurchaseRecommendationListDto ExistingRecommendations { get; set; } = new();
        public decimal TotalShortageQty { get; set; }
        public decimal TotalEstimatedPurchaseCost { get; set; }
        public bool AutoPoTransmissionDisabled { get; set; } = true;
    }

    public class AiMenuRecommendationCardDto
    {
        public string Strategy { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public string SupportingEvidence { get; set; } = string.Empty;
        public string CurrentBaseline { get; set; } = string.Empty;
        public string ProposedChange { get; set; } = string.Empty;
        public decimal? EstimatedCostImpact { get; set; }
        public decimal? EstimatedMarginImpact { get; set; }
        public string OperationalImpact { get; set; } = string.Empty;
        public string DataQuality { get; set; } = "HIGH";
        public decimal Confidence { get; set; }
        public bool RequiresApproval { get; set; } = true;
        public Guid? TargetMenuItemId { get; set; }
        public Guid? SuggestedRecipeId { get; set; }
        public string? AllergenImpact { get; set; }
    }

    public class AiMenuStrategyOptionDto
    {
        public string StrategyKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<Guid> RecommendedMenuItemIds { get; set; } = new();
        public decimal? ProjectedFoodCostPercent { get; set; }
        public decimal? ProjectedMargin { get; set; }
        public decimal Confidence { get; set; }
    }

    public class AiMenuIntelligenceDto
    {
        public string ProviderMode { get; set; } = "LOCAL_DETERMINISTIC";
        public bool AiProviderAvailable { get; set; }
        public string? FallbackNotice { get; set; }
        public List<AiMenuRecommendationCardDto> Recommendations { get; set; } = new();
        public List<AiMenuStrategyOptionDto> AlternativeMenus { get; set; } = new();
    }

    public class HistoricalMenuSummaryDto
    {
        public Guid SourceId { get; set; }
        public string SourceKind { get; set; } = "MENU_PLAN";
        public string Name { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public DateTime BusinessDate { get; set; }
        public int VersionNumber { get; set; }
        public decimal GuestCount { get; set; }
        public decimal ActualSales { get; set; }
        public decimal? HistoricalFoodCostPercent { get; set; }
        public decimal? HistoricalMargin { get; set; }
        public decimal? ActualWasteQty { get; set; }
        public decimal? RecalculatedCurrentCost { get; set; }
        public decimal? RecalculatedCurrentFoodCostPercent { get; set; }
        public List<Guid> MenuItemIds { get; set; } = new();
    }

    public class WhatIfSimulationRequestDto : SilaMenuWhatIfRequestDto
    {
        public string? ScenarioName { get; set; }
        public string? ScenarioType { get; set; }
        public decimal? SellingPriceChangePercent { get; set; }
        public decimal? IngredientPriceChangePercent { get; set; }
        public decimal? PortionSizeChangePercent { get; set; }
        public decimal? WastePercentOverride { get; set; }
        public decimal? YieldPercentOverride { get; set; }
        public decimal? BufferPercentOverride { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? ParticipationRateOverride { get; set; }
        public Guid? SubstituteOutMenuItemId { get; set; }
        public Guid? SubstituteInMenuItemId { get; set; }
        public bool SaveScenario { get; set; }
    }

    public class WhatIfSimulationResultDto : SilaMenuWhatIfResultDto
    {
        public Guid? ScenarioId { get; set; }
        public string ScenarioName { get; set; } = "Ad-hoc Simulation";
        public decimal BaselineCost { get; set; }
        public decimal BaselineRevenue { get; set; }
        public decimal BaselineMargin { get; set; }
        public decimal BaselineRequirement { get; set; }
        public decimal BaselineShortage { get; set; }
        public decimal CostVarianceAbs { get; set; }
        public decimal CostVariancePct { get; set; }
        public decimal RevenueVarianceAbs { get; set; }
        public decimal RevenueVariancePct { get; set; }
        public decimal MarginVarianceAbs { get; set; }
        public decimal MarginVariancePct { get; set; }
        public decimal ExpectedWasteQty { get; set; }
        public bool FixedDemandAssumed { get; set; } = true;
        public string DemandAssumptionDisclosure { get; set; } = "Simulation assumes fixed demand unless explicit elasticity/cover overrides are supplied. Live data is untouched.";
    }

    public class MenuEngineeringScenarioDto
    {
        public Guid Id { get; set; }
        public Guid? MenuPlanId { get; set; }
        public string ScenarioName { get; set; } = string.Empty;
        public string ScenarioType { get; set; } = "CUSTOM";
        public int ItemCount { get; set; }
        public decimal ExpectedCovers { get; set; }
        public decimal SellingPricePerGuest { get; set; }
        public decimal IngredientCostPerGuest { get; set; }
        public decimal TotalFoodCost { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal IngredientAvailabilityPercent { get; set; }
        public decimal PurchaseRequirement { get; set; }
        public decimal EstimatedWaste { get; set; }
        public string ProductionCapacityImpact { get; set; } = "NORMAL";
        public string DietaryCoverage { get; set; } = "STANDARD";
        public bool IsPreferred { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ScenarioCompareResultDto
    {
        public List<MenuEngineeringScenarioDto> Scenarios { get; set; } = new();
        public Guid? LowestFoodCostScenarioId { get; set; }
        public Guid? HighestMarginScenarioId { get; set; }
        public Guid? HighestReadinessScenarioId { get; set; }
        public string GovernanceNote { get; set; } = "Side-by-side comparison uses identical costing & readiness basis. User selection must respect dietary, allergen, and quality constraints.";
    }

    public class MenuEngineeringSetupDto : SilaMenuEngineeringSettingsDto
    {
        public Guid? PropertyId { get; set; }
        public bool MenuEngineeringEnabled { get; set; } = true;
        public bool MeStudioEnabled { get; set; } = true;
        public List<string> ServiceFormats { get; set; } = new();
        public List<string> Cuisines { get; set; } = new();
        public List<string> DietaryTags { get; set; } = new();
        public string CostingMethod { get; set; } = "CURRENT_COST";
        public decimal TargetFoodCostPercent { get; set; } = 0.30m;
        public decimal? TargetContributionMargin { get; set; }
        public string CostRefreshFrequency { get; set; } = "REALTIME";
        public string DefaultCurrency { get; set; } = "USD";
        public string TaxTreatment { get; set; } = "EXCLUSIVE";
        public string DefaultPortionUom { get; set; } = "PORTION";
        public decimal PortionVarianceThresholdPercent { get; set; } = 10m;
        public int ForecastHorizonDays { get; set; } = 14;
        public string ForecastMethod { get; set; } = "HYBRID_RIDGE";
        public int HistoricalLookbackDays { get; set; } = 90;
        public decimal ForecastAccuracyThresholdPercent { get; set; } = 80m;
        public string ProfitabilityBasis { get; set; } = "FOOD_COST_ONLY";
        public decimal? ProfitabilityThresholdPercent { get; set; }
        public int MinimumSalesHistoryDays { get; set; } = 14;
        public decimal MinimumSalesQuantity { get; set; } = 10m;
        public string ComparableGrouping { get; set; } = "CATEGORY";
        public string PosSyncScheduleCron { get; set; } = "0 */4 * * *";
        public int PosRetryMaxAttempts { get; set; } = 3;
        public List<string> AiEnabledAgents { get; set; } = new();
        public decimal AiConfidenceThreshold { get; set; } = 0.70m;
        public int AiUsageLimitPerDay { get; set; } = 100;
        public decimal ApprovalFoodCostThresholdPercent { get; set; } = 35m;
        public decimal ApprovalPriceChangePercent { get; set; } = 10m;
        public bool RequireApprovalOnSubstitution { get; set; } = true;
        public bool RequireApprovalOnPublish { get; set; } = true;
        public List<string> ApprovalRoles { get; set; } = new();
        public int ConfigVersion { get; set; } = 1;
        public string? ChangeSummary { get; set; }
        public List<MenuEngineeringConfigHistoryDto> History { get; set; } = new();
    }

    public class MenuEngineeringConfigHistoryDto
    {
        public Guid Id { get; set; }
        public int ConfigVersion { get; set; }
        public string? ChangeSummary { get; set; }
        public DateTime ChangedAtUtc { get; set; }
    }

    public class MenuEngineeringReportItemDto
    {
        public string ReportKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> SupportedFormats { get; set; } = ["CSV", "EXCEL", "JSON", "PDF"];
    }

    public class MenuEngineeringReportExportDto
    {
        public string ReportKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Format { get; set; } = "JSON";
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = "application/json";
        public List<string> Columns { get; set; } = new();
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
        public string? TextContent { get; set; }
    }

    public class SilaStoreMenuMobileDto
    {
        public DateTime BusinessDate { get; set; }
        public MenuEngineeringPermissionSetDto Permissions { get; set; } = new();
        public List<MenuPlanEnhancedDto> TodayMenus { get; set; } = new();
        public List<MenuPlanEnhancedDto> UpcomingMenus { get; set; } = new();
        public List<PortionPlanRowDto> PortionInstructions { get; set; } = new();
        public List<SilaMenuRequirementRowDto> IngredientReadiness { get; set; } = new();
        public List<MenuPlanEnhancedDto> PendingApprovals { get; set; } = new();
    }
}
