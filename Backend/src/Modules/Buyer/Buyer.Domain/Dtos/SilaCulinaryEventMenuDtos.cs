namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryDietaryAllocationDto
    {
        public string RequirementType { get; set; } = string.Empty;
        public int GuestCount { get; set; }
        public Guid? SessionId { get; set; }
        public List<string> RequiredMenuItems { get; set; } = new();
        public string? SpecialPrepNotes { get; set; }
        public bool OverlapsOtherGroups { get; set; }
        public string VerificationStatus { get; set; } = "PENDING"; // PENDING, VERIFIED, FLAGGED
        public string? ReviewerName { get; set; }
    }

    public class SilaCulinaryEventSessionDto
    {
        public Guid Id { get; set; }
        public Guid EventId { get; set; }
        public string SessionCode { get; set; } = string.Empty;
        public int DayNumber { get; set; } = 1;
        public DateTime SessionDateUtc { get; set; }
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "12:00";
        public string SessionName { get; set; } = string.Empty;
        public string? VenueName { get; set; }
        public Guid? KitchenLocationId { get; set; }
        public string ServiceStyle { get; set; } = "BUFFET";
        public int GuestCount { get; set; }
        public Guid? MenuId { get; set; }
        public decimal? BudgetAllocation { get; set; }
        public DateTime? PrepDeadlineUtc { get; set; }
        public string? DietaryNotesJson { get; set; }
        public int SortOrder { get; set; }
        public decimal? EstimatedSessionCost { get; set; }
    }

    public class SilaCulinaryUpsertSessionRequestDto
    {
        public Guid? SessionId { get; set; }
        public int DayNumber { get; set; } = 1;
        public DateTime SessionDateUtc { get; set; }
        public string StartTime { get; set; } = "09:00";
        public string EndTime { get; set; } = "12:00";
        public string SessionName { get; set; } = string.Empty;
        public string? VenueName { get; set; }
        public Guid? KitchenLocationId { get; set; }
        public string ServiceStyle { get; set; } = "BUFFET";
        public int GuestCount { get; set; }
        public Guid? MenuId { get; set; }
        public decimal? BudgetAllocation { get; set; }
        public DateTime? PrepDeadlineUtc { get; set; }
        public string? DietaryNotesJson { get; set; }
        public int SortOrder { get; set; }
    }

    public class SilaCulinaryMenuLineDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string CourseName { get; set; } = "Main Courses";
        public int SortOrder { get; set; }
        public string ItemRole { get; set; } = "MANDATORY";
        public bool IsLocked { get; set; }
        public Guid? RecipeId { get; set; }
        public string? RecipeCode { get; set; }
        public int? RecipeVersion { get; set; }
        public int? LatestPublishedRecipeVersion { get; set; }
        public bool RecipeVersionOutdated { get; set; }
        public Guid? RecipeDraftId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string? CustomerDescription { get; set; }
        public string? DishImageUrl { get; set; }
        public string LineStatus { get; set; } = "PUBLISHED";
        public string ServiceStyle { get; set; } = "BUFFET";
        public decimal UptakePercent { get; set; } = 100m;
        public decimal ServingFrequency { get; set; } = 1m;
        public decimal PortionSize { get; set; } = 180m;
        public string PortionUom { get; set; } = "G";
        public int? AllocatedPortions { get; set; }
        public int? ManualPortionsOverride { get; set; }
        public int PlannedPortions { get; set; }
        public Guid? CostingSnapshotId { get; set; }
        public decimal? UnitCostPerPortion { get; set; }
        public decimal? EstimatedLineCost { get; set; }
        public bool CostVisible { get; set; } = true;
        public List<string> DietaryTags { get; set; } = new();
        public List<string> VerifiedAllergens { get; set; } = new();
        public bool AllergenUnknown { get; set; }
        public bool AllergenVerified { get; set; }
        public bool IsBeverage { get; set; }
        public decimal ServingsPerGuest { get; set; } = 1m;
        public decimal? ServingVolumeMl { get; set; }
        public decimal? ContainerSizeMl { get; set; }
        public decimal? EstimatedBeverageLiters { get; set; }
        public string? LiveStationName { get; set; }
        public int? VerifiedStationCapacityPerHour { get; set; }
        public string? Notes { get; set; }
    }

    public class SilaCulinaryMenuDto
    {
        public Guid Id { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public Guid? EventId { get; set; }
        public Guid? SessionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MenuCategory { get; set; } = "Event Banquet";
        public string? Cuisine { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public string ServiceStyle { get; set; } = "BUFFET";
        public int VersionNumber { get; set; } = 1;
        public DateTime EffectiveDateUtc { get; set; }
        public string? MenuImageUrl { get; set; }
        public string Status { get; set; } = "DRAFT";
        public bool IsReusableTemplate { get; set; }
        public bool IsScenario { get; set; }
        public string? ScenarioName { get; set; }
        public bool IsSelectedScenario { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal? TargetSellingPrice { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 28m;
        public decimal? TotalFoodCost { get; set; }
        public decimal? TotalBeverageCost { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public decimal? CostPerGuest { get; set; }
        public bool CostVisible { get; set; } = true;
        public string? AiRecommendationNotes { get; set; }
        public List<SilaCulinaryMenuLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryEventDto
    {
        public Guid Id { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventType { get; set; } = "Corporate Gala";
        public string? EventCategory { get; set; }
        public string? CustomerReference { get; set; }
        public string? CustomerName { get; set; }
        public string? BookingReference { get; set; }
        public string? ExternalEventReference { get; set; }
        public string? EventCoordinator { get; set; }
        public string? ResponsibleChef { get; set; }
        public string? ExecutiveChef { get; set; }
        public string? BanquetManager { get; set; }
        public string Status { get; set; } = "DRAFT";
        public int RevisionNumber { get; set; } = 1;
        public int? ApprovedRevisionNumber { get; set; }
        public Guid? PropertyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? VenueName { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? KitchenLocationId { get; set; }
        public List<string> StorageLocationCodes { get; set; } = new();
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public string StartTime { get; set; } = "18:00";
        public string EndTime { get; set; } = "23:00";
        public DateTime? PrepStartUtc { get; set; }
        public DateTime? MenuConfirmationDeadlineUtc { get; set; }
        public DateTime? GuestConfirmationDeadlineUtc { get; set; }
        public DateTime? MrpDeadlineUtc { get; set; }
        public string TimeZoneId { get; set; } = "Asia/Dubai";
        public int GuaranteedGuests { get; set; }
        public int ExpectedGuests { get; set; }
        public int? MaxCapacity { get; set; }
        public decimal AdditionalAllowancePercent { get; set; }
        public int PlanningGuests { get; set; }
        public int BillableGuests { get; set; }
        public string BillablePolicy { get; set; } = "GUARANTEED_ONLY";
        public int AdultsCount { get; set; }
        public int ChildrenCount { get; set; }
        public int VipCount { get; set; }
        public int StaffMealsCount { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal? NetPricePerGuest { get; set; }
        public decimal? FixedPackagePrice { get; set; }
        public decimal? EstimatedFoodRevenue { get; set; }
        public decimal? EstimatedBeverageRevenue { get; set; }
        public decimal? OtherPlannedRevenue { get; set; }
        public decimal? TotalEstimatedRevenue { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 28m;
        public decimal TargetBeverageCostPercent { get; set; } = 22m;
        public decimal? CombinedMenuBudget { get; set; }
        public decimal? ApprovedBudget { get; set; }
        public decimal? TotalEstimatedMenuCost { get; set; }
        public decimal? MenuCostPercent { get; set; }
        public decimal? RemainingMenuBudget { get; set; }
        public bool CostVisible { get; set; } = true;
        public string ServiceStyle { get; set; } = "BUFFET";
        public Guid? PrimaryMenuId { get; set; }
        public Guid? SourceTemplateId { get; set; }
        public int WizardStepCompleted { get; set; } = 1;
        public List<SilaCulinaryDietaryAllocationDto> DietaryRequirements { get; set; } = new();
        public bool DietaryReviewVerified { get; set; }
        public string? DietaryReviewerName { get; set; }
        public DateTime? DietaryVerifiedOnUtc { get; set; }
        public bool HasVenueConflict { get; set; }
        public List<string> ConflictingEventCodes { get; set; } = new();
        public string? ApprovalReference { get; set; }
        public DateTime? SubmittedOnUtc { get; set; }
        public DateTime? ApprovedOnUtc { get; set; }
        public string? ApprovalDecisionComment { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public SilaCulinaryMenuDto? PrimaryMenu { get; set; }
        public List<SilaCulinaryEventSessionDto> Sessions { get; set; } = new();
        public List<SilaCulinaryMenuDto> Scenarios { get; set; } = new();
    }

    public class SilaCulinaryUpsertEventRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventType { get; set; } = "Corporate Gala";
        public string? EventCategory { get; set; }
        public string? CustomerReference { get; set; }
        public string? CustomerName { get; set; }
        public string? BookingReference { get; set; }
        public string? ExternalEventReference { get; set; }
        public string? EventCoordinator { get; set; }
        public string? ResponsibleChef { get; set; }
        public string? ExecutiveChef { get; set; }
        public string? BanquetManager { get; set; }
        public Guid? PropertyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? VenueName { get; set; }
        public Guid? OutletLocationId { get; set; }
        public Guid? KitchenLocationId { get; set; }
        public List<string>? StorageLocationCodes { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public string StartTime { get; set; } = "18:00";
        public string EndTime { get; set; } = "23:00";
        public DateTime? PrepStartUtc { get; set; }
        public DateTime? MenuConfirmationDeadlineUtc { get; set; }
        public DateTime? GuestConfirmationDeadlineUtc { get; set; }
        public DateTime? MrpDeadlineUtc { get; set; }
        public string TimeZoneId { get; set; } = "Asia/Dubai";
        public int GuaranteedGuests { get; set; }
        public int ExpectedGuests { get; set; }
        public int? MaxCapacity { get; set; }
        public decimal AdditionalAllowancePercent { get; set; }
        public string BillablePolicy { get; set; } = "GUARANTEED_ONLY";
        public int AdultsCount { get; set; }
        public int ChildrenCount { get; set; }
        public int VipCount { get; set; }
        public int StaffMealsCount { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal NetPricePerGuest { get; set; }
        public decimal? FixedPackagePrice { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 28m;
        public decimal TargetBeverageCostPercent { get; set; } = 22m;
        public decimal? CombinedMenuBudget { get; set; }
        public decimal? ApprovedBudget { get; set; }
        public string ServiceStyle { get; set; } = "BUFFET";
        public int WizardStepCompleted { get; set; } = 1;
        public List<SilaCulinaryDietaryAllocationDto>? DietaryRequirements { get; set; }
        public string? ChangeReason { get; set; }
    }

    public class SilaCulinaryCreateMenuRequestDto
    {
        public Guid? EventId { get; set; }
        public Guid? SessionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string MenuCategory { get; set; } = "Event Banquet";
        public string? Cuisine { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletLocationId { get; set; }
        public string ServiceStyle { get; set; } = "BUFFET";
        public bool IsReusableTemplate { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal? TargetSellingPrice { get; set; }
        public decimal TargetFoodCostPercent { get; set; } = 28m;
    }

    public class SilaCulinaryUpsertMenuLineRequestDto
    {
        public Guid? LineId { get; set; }
        public string CourseName { get; set; } = "Main Courses";
        public int? SortOrder { get; set; }
        public string ItemRole { get; set; } = "MANDATORY";
        public bool IsLocked { get; set; }
        public Guid? RecipeId { get; set; }
        public int? RecipeVersion { get; set; }
        public string? DishName { get; set; }
        public string? CustomerDescription { get; set; }
        public string ServiceStyle { get; set; } = "BUFFET";
        public decimal UptakePercent { get; set; } = 100m;
        public decimal ServingFrequency { get; set; } = 1m;
        public decimal PortionSize { get; set; } = 180m;
        public string PortionUom { get; set; } = "G";
        public int? AllocatedPortions { get; set; }
        public int? ManualPortionsOverride { get; set; }
        public bool IsBeverage { get; set; }
        public decimal ServingsPerGuest { get; set; } = 1m;
        public decimal? ServingVolumeMl { get; set; }
        public decimal? ContainerSizeMl { get; set; }
        public string? LiveStationName { get; set; }
        public int? VerifiedStationCapacityPerHour { get; set; }
        public string? Notes { get; set; }
        public decimal? UnitCostOverride { get; set; }
    }

    public class SilaCulinaryPublishedRecipeSearchItemDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public int RecipeVersion { get; set; } = 1;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Cuisine { get; set; }
        public string? ImageUrl { get; set; }
        public decimal ServingQty { get; set; }
        public string ServingUom { get; set; } = "PORTION";
        public decimal? CostPerPortion { get; set; }
        public string Currency { get; set; } = "AED";
        public int? PrepTimeMinutes { get; set; }
        public int? CookTimeMinutes { get; set; }
        public List<string> DietaryTags { get; set; } = new();
        public List<string> VerifiedAllergens { get; set; } = new();
        public bool UnknownAllergens { get; set; }
        public bool PropertyEligible { get; set; } = true;
        public decimal MatchScore { get; set; } = 1m;
        public string MatchReason { get; set; } = "Published Recipe Master";
    }

    public class SilaCulinaryAiMenuGenerateRequestDto
    {
        public string? NaturalLanguageBrief { get; set; }
        public string? EventTheme { get; set; }
        public List<string> CuisinePreferences { get; set; } = new();
        public List<string> RequiredCourses { get; set; } = new() { "Starters", "Main Courses", "Desserts", "Cold Beverages" };
        public List<string> RequiredDietaryTags { get; set; } = new();
        public List<string> ExcludedAllergens { get; set; } = new();
        public string Strategy { get; set; } = "BALANCED";
        public bool AllowMissingRecipeSuggestions { get; set; } = true;
        public bool ApplyToMenu { get; set; } = false;
    }

    public class SilaCulinaryAiMenuGenerateResultDto
    {
        public string MenuTitle { get; set; } = string.Empty;
        public string MenuDescription { get; set; } = string.Empty;
        public int PlanningGuests { get; set; }
        public decimal? EstimatedTotalCost { get; set; }
        public string Currency { get; set; } = "AED";
        public string ProviderUsed { get; set; } = "LOCAL";
        public string ModelUsed { get; set; } = string.Empty;
        public List<string> ClarificationQuestions { get; set; } = new();
        public List<string> BudgetWarnings { get; set; } = new();
        public Dictionary<string, int> DietaryCoverage { get; set; } = new();
        public int MissingRecipeCount { get; set; }
        public string Explanation { get; set; } = string.Empty;
        public List<SilaCulinaryMenuLineDto> ProposedLines { get; set; } = new();
    }

    public class SilaCulinaryMenuOptimizeRequestDto
    {
        public decimal? MaxBudget { get; set; }
        public Dictionary<string, int>? RequiredCourses { get; set; }
        public List<string> RequiredDietaryTags { get; set; } = new();
        public List<string> ExcludedAllergens { get; set; } = new();
        public string Strategy { get; set; } = "LOWEST_COST";
        public bool ApplyChanges { get; set; } = false;
    }

    public class SilaCulinaryMenuOptimizeResultDto
    {
        public bool Feasible { get; set; }
        public string Solver { get; set; } = "SCIPY_HIGHS_MILP";
        public decimal? BeforeCost { get; set; }
        public decimal? AfterCost { get; set; }
        public decimal? Budget { get; set; }
        public decimal? Savings { get; set; }
        public List<string> ConflictingConstraints { get; set; } = new();
        public List<string> SuggestedAdjustments { get; set; } = new();
        public string Explanation { get; set; } = string.Empty;
        public List<SilaCulinaryMenuLineDto> OptimizedLines { get; set; } = new();
    }

    public class SilaCulinaryAllergenMatrixRowDto
    {
        public Guid MenuLineId { get; set; }
        public string DishName { get; set; } = string.Empty;
        public string? RecipeCode { get; set; }
        public int? RecipeVersion { get; set; }
        public List<string> VerifiedAllergens { get; set; } = new();
        public List<string> SubRecipeAllergens { get; set; } = new();
        public bool AllergenUnknown { get; set; }
        public string CrossContactRisk { get; set; } = "UNKNOWN";
        public string VerificationStatus { get; set; } = "PENDING";
    }

    public class SilaCulinaryMaterialPreviewLineDto
    {
        public string DishName { get; set; } = string.Empty;
        public string? RecipeCode { get; set; }
        public int? RecipeVersion { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialDescription { get; set; }
        public decimal RequiredQty { get; set; }
        public string RecipeUom { get; set; } = string.Empty;
        public decimal? BaseQty { get; set; }
        public string? BaseUom { get; set; }
        public decimal? OnHandStock { get; set; }
        public string StockIndicator { get; set; } = "UNKNOWN"; // SUFFICIENT, POTENTIAL_SHORTAGE, UNAVAILABLE, UNKNOWN, REVIEW_REQUIRED
        public DateTime? StockTimestampUtc { get; set; }
        public bool ScalingIncomplete { get; set; }
    }

    public class SilaCulinaryEventSimulationRequestDto
    {
        public int? GuaranteedGuests { get; set; }
        public int? ExpectedGuests { get; set; }
        public decimal? AdditionalAllowancePercent { get; set; }
        public decimal? NetPricePerGuest { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public decimal? MenuBudget { get; set; }
        public string? ServiceStyle { get; set; }
        public List<SilaCulinaryMenuLineSimulationOverrideDto> LineOverrides { get; set; } = new();
    }

    public class SilaCulinaryMenuLineSimulationOverrideDto
    {
        public Guid LineId { get; set; }
        public decimal? UptakePercent { get; set; }
        public decimal? PortionSize { get; set; }
        public decimal? UnitCostOverride { get; set; }
    }

    public class SilaCulinaryEventSimulationResultDto
    {
        public int GuaranteedGuests { get; set; }
        public int PlanningGuests { get; set; }
        public int BillableGuests { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal? EstimatedRevenue { get; set; }
        public decimal? TotalFoodCost { get; set; }
        public decimal? TotalBeverageCost { get; set; }
        public decimal? TotalMenuCost { get; set; }
        public decimal? CostPerGuaranteedGuest { get; set; }
        public decimal? CostPerPlannedGuest { get; set; }
        public decimal? MenuCostPercent { get; set; }
        public decimal? MenuBudget { get; set; }
        public decimal? RemainingMenuBudget { get; set; }
        public bool BudgetExceeded { get; set; }
        public bool PlatedAllocationValid { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
        public List<SilaCulinaryMenuLineDto> SimulatedLines { get; set; } = new();
    }

    public class SilaCulinaryEventReadinessItemDto
    {
        public string CheckCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Status { get; set; } = "PASSED"; // PASSED, WARNING, BLOCKED
        public string Detail { get; set; } = string.Empty;
        public string? RecommendedAction { get; set; }
    }

    public class SilaCulinaryEventReadinessDto
    {
        public Guid EventId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public bool ReadyForApproval { get; set; }
        public bool ReadyForMrp { get; set; }
        public int PassedCount { get; set; }
        public int WarningCount { get; set; }
        public int BlockedCount { get; set; }
        public List<SilaCulinaryEventReadinessItemDto> Checks { get; set; } = new();
    }

    public class SilaCulinaryCustomerMenuCourseDto
    {
        public string CourseName { get; set; } = string.Empty;
        public List<SilaCulinaryCustomerMenuDishDto> Dishes { get; set; } = new();
    }

    public class SilaCulinaryCustomerMenuDishDto
    {
        public string DishName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<string> DietaryTags { get; set; } = new();
        public List<string> AllergenDisclosures { get; set; } = new();
        public bool AllergenPendingVerification { get; set; }
    }

    public class SilaCulinaryCustomerMenuPreviewDto
    {
        public string EventName { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string? VenueName { get; set; }
        public DateTime StartDateUtc { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public string ServiceStyle { get; set; } = string.Empty;
        public bool IsProposedOnly { get; set; }
        public List<SilaCulinaryCustomerMenuCourseDto> Courses { get; set; } = new();
    }

    public class SilaCulinaryEventTemplateDto
    {
        public Guid Id { get; set; }
        public string TemplateCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string DefaultServiceStyle { get; set; } = "BUFFET";
        public int DefaultDurationDays { get; set; } = 1;
        public decimal DefaultAllowancePercent { get; set; } = 5m;
        public decimal DefaultTargetFoodCostPercent { get; set; } = 28m;
        public DateTime CreatedOn { get; set; }
    }

    public class SilaCulinaryEventDashboardDto
    {
        public int UpcomingEvents { get; set; }
        public int EventsThisWeek { get; set; }
        public int EventsThisMonth { get; set; }
        public int TotalPlannedGuests { get; set; }
        public int DraftEvents { get; set; }
        public int AwaitingApprovalEvents { get; set; }
        public int ApprovedEvents { get; set; }
        public int IncompleteMenuEvents { get; set; }
        public int AboveBudgetEvents { get; set; }
        public int DietaryReviewPendingEvents { get; set; }
        public int ReadyForMrpEvents { get; set; }
        public Dictionary<string, int> EventsByType { get; set; } = new();
        public Dictionary<string, int> EventsByStatus { get; set; } = new();
        public List<SilaCulinaryEventDashboardChartPointDto> BudgetVsCost { get; set; } = new();
    }

    public class SilaCulinaryEventDashboardChartPointDto
    {
        public string EventCode { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public decimal? Budget { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int PlanningGuests { get; set; }
    }

    public class SilaCulinaryEventRevisionDto
    {
        public Guid Id { get; set; }
        public int RevisionNumber { get; set; }
        public string StatusAtRevision { get; set; } = string.Empty;
        public bool IsApprovedBaseline { get; set; }
        public string? ChangeReason { get; set; }
        public int GuaranteedGuestsAtRevision { get; set; }
        public int PlanningGuestsAtRevision { get; set; }
        public decimal? MenuCostAtRevision { get; set; }
        public string? ApprovalReference { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class SilaCulinaryEventReportResultDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public string Base64Content { get; set; } = string.Empty;
        public int RowCount { get; set; }
    }

    /// <summary>Phase 2B Event MRP Handover Contract (§57).</summary>
    public class SilaCulinaryPhase2BHandoverDto
    {
        public Guid EventId { get; set; }
        public string EventCode { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public int EventRevision { get; set; }
        public int? ApprovedRevision { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PlantCode { get; set; }
        public string? VenueName { get; set; }
        public Guid? KitchenLocationId { get; set; }
        public DateTime StartDateUtc { get; set; }
        public DateTime EndDateUtc { get; set; }
        public DateTime? MrpDeadlineUtc { get; set; }
        public int GuaranteedGuests { get; set; }
        public int PlanningGuests { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal? EventMenuBudget { get; set; }
        public decimal? TotalEstimatedMenuCost { get; set; }
        public string? ApprovalReference { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public DateTime? BaselineTimestampUtc { get; set; }
        public bool DietaryVerified { get; set; }
        public bool MrpReady { get; set; }
        public List<string> OutstandingWarnings { get; set; } = new();
        public List<SilaCulinaryEventSessionDto> Sessions { get; set; } = new();
        public List<SilaCulinaryMenuLineDto> PinnedMenuLines { get; set; } = new();
        public List<SilaCulinaryMaterialPreviewLineDto> PreliminaryMaterialRequirements { get; set; } = new();
    }
}
