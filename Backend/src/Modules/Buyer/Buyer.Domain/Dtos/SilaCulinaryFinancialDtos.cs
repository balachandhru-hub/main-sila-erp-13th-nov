namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryFxRateDto
    {
        public string SourceCurrency { get; set; } = "USD";
        public string TargetCurrency { get; set; } = "AED";
        public decimal Rate { get; set; } = 3.6725m;
        public string RateType { get; set; } = "CORPORATE";
        public string Source { get; set; } = "TENANT_FX_TABLE";
        public DateTime? EffectiveDateUtc { get; set; }
    }

    public class SilaCulinaryOverheadRuleDto
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Method { get; set; } = "FIXED_PER_EVENT"; // FIXED_PER_EVENT | PCT_NET_REVENUE | PCT_DIRECT_COST | PER_GUEST | PER_LABOR_HOUR | PER_KITCHEN_HOUR | PER_VENUE_HOUR | MANUAL_APPROVED
        public decimal RateOrAmount { get; set; }
        public string? SourcePoolId { get; set; }
        public decimal DriverHours { get; set; }
        public bool IsApproved { get; set; } = true;
    }

    public class SilaCulinaryFinancialConfigDto
    {
        public Guid Id { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public string PropertyCurrency { get; set; } = "AED";
        public int FinancialPrecision { get; set; } = 2;
        public int QuantityPrecision { get; set; } = 4;
        public string MonetaryRoundingPolicy { get; set; } = "ROUND_HALF_UP";
        public List<string> CostSourcePriority { get; set; } = new();
        public string CostValuationPolicy { get; set; } = "MOVING_AVERAGE_PREFERRED";
        public decimal TargetFoodCostPercent { get; set; } = 28m;
        public decimal TargetBeverageCostPercent { get; set; } = 22m;
        public decimal TargetEventContributionMarginPercent { get; set; } = 40m;
        public decimal MaterialCostAlertThresholdPercent { get; set; } = 5m;
        public decimal BudgetApprovalThresholdAmount { get; set; } = 10000m;
        public bool AllowManualCostOverrides { get; set; } = true;
        public bool ExcludeVatFromRevenue { get; set; } = true;
        public decimal DefaultVatPercent { get; set; } = 5m;
        public string ExchangeRateSource { get; set; } = "TENANT_FX_TABLE";
        public List<SilaCulinaryFxRateDto> ExchangeRates { get; set; } = new();
        public List<SilaCulinaryOverheadRuleDto> OverheadRules { get; set; } = new();
        public int FiscalYearStartMonth { get; set; } = 1;
        public string CostSnapshotPolicy { get; set; } = "IMMUTABLE_VERSIONED";
    }

    public class SilaCulinarySaveFinancialConfigRequestDto
    {
        public string ReportingCurrency { get; set; } = "AED";
        public string PropertyCurrency { get; set; } = "AED";
        public int FinancialPrecision { get; set; } = 2;
        public int QuantityPrecision { get; set; } = 4;
        public string MonetaryRoundingPolicy { get; set; } = "ROUND_HALF_UP";
        public List<string>? CostSourcePriority { get; set; }
        public string CostValuationPolicy { get; set; } = "MOVING_AVERAGE_PREFERRED";
        public decimal TargetFoodCostPercent { get; set; } = 28m;
        public decimal TargetBeverageCostPercent { get; set; } = 22m;
        public decimal TargetEventContributionMarginPercent { get; set; } = 40m;
        public decimal MaterialCostAlertThresholdPercent { get; set; } = 5m;
        public decimal BudgetApprovalThresholdAmount { get; set; } = 10000m;
        public bool AllowManualCostOverrides { get; set; } = true;
        public bool ExcludeVatFromRevenue { get; set; } = true;
        public decimal DefaultVatPercent { get; set; } = 5m;
        public string ExchangeRateSource { get; set; } = "TENANT_FX_TABLE";
        public List<SilaCulinaryFxRateDto>? ExchangeRates { get; set; }
        public List<SilaCulinaryOverheadRuleDto>? OverheadRules { get; set; }
        public int FiscalYearStartMonth { get; set; } = 1;
    }

    public class SilaCulinaryFinancialRecipeLineDto
    {
        public string LineRef { get; set; } = string.Empty;
        public string? MaterialCode { get; set; }
        public string Description { get; set; } = string.Empty;
        public string CostClass { get; set; } = "FOOD";
        public bool IsSubRecipe { get; set; }
        public decimal RecipeQty { get; set; }
        public string RecipeUom { get; set; } = "KG";
        public decimal RawQty { get; set; }
        public decimal CostableBaseQty { get; set; }
        public string BaseUom { get; set; } = "KG";
        public decimal EffectiveYieldPercent { get; set; } = 100m;
        public decimal? NormalizedUnitCost { get; set; }
        public string PriceSource { get; set; } = "MISSING";
        public string? SourceCurrency { get; set; }
        public decimal FxRateApplied { get; set; } = 1m;
        public decimal? IngredientCost { get; set; }
        public decimal? ShareOfIngredientCostPct { get; set; }
        public string Status { get; set; } = "VERIFIED";
        public string? Warning { get; set; }
    }

    public class SilaCulinaryFinancialRecipeCostDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int RecipeVersion { get; set; } = 1;
        public string Category { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public decimal StandardBatchServings { get; set; }
        public decimal PlannedServings { get; set; }
        public decimal PlannedBatches { get; set; }
        public decimal PlannedOutputServings { get; set; }
        public decimal ExcessServings { get; set; }
        public decimal TotalIngredientCost { get; set; }
        public decimal FoodIngredientCost { get; set; }
        public decimal BeverageIngredientCost { get; set; }
        public decimal SubRecipeCost { get; set; }
        public decimal DirectProductionCost { get; set; }
        public decimal AllocatedOverheadCost { get; set; }
        public decimal FullyAllocatedBatchCost { get; set; }
        public decimal ExpectedEventConsumptionCost { get; set; }
        public decimal RemainingUsableOutputValue { get; set; }
        public decimal IngredientCostPerServing { get; set; }
        public decimal DirectCostPerServing { get; set; }
        public decimal FullyAllocatedCostPerServing { get; set; }
        public decimal? NetSellingPricePerServing { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? IngredientContributionPerServing { get; set; }
        public decimal? IngredientContributionPercent { get; set; }
        public decimal? DirectContributionPerServing { get; set; }
        public decimal? FullyAllocatedProfitPerServing { get; set; }
        public decimal RequiredSellingPriceTargetFoodCost { get; set; }
        public decimal RequiredSellingPriceTargetMarginDirect { get; set; }
        public decimal RequiredSellingPriceTargetMarginFull { get; set; }
        public string CostCompleteness { get; set; } = "COMPLETE";
        public int MissingPriceCount { get; set; }
        public int MissingFxCount { get; set; }
        public int UomConflictCount { get; set; }
        public bool CircularDependencyDetected { get; set; }
        public List<SilaCulinaryFinancialRecipeLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryFinancialRecipeCostRequestDto
    {
        public Guid? OutletId { get; set; }
        public decimal? PlannedServings { get; set; }
        public bool AllowPartialBatch { get; set; } = true;
        public decimal? TargetFoodCostPercent { get; set; }
        public decimal? TargetContributionMarginPercent { get; set; }
        public decimal GlobalPriceChangePercent { get; set; }
        public decimal DirectProductionFixedCost { get; set; }
        public decimal AllocatedOverheadFixedCost { get; set; }
        public string? CostingPolicy { get; set; } // STANDARD | REPLACEMENT | EVENT_PLANNING | PROCUREMENT_FORECAST | ACTUAL_CONSUMPTION
        public Dictionary<string, decimal>? ManualPriceOverrides { get; set; }
    }

    public class SilaCulinaryMenuCostLineDto
    {
        public string LineRef { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Course { get; set; } = string.Empty;
        public string CostClass { get; set; } = "FOOD";
        public decimal ExpectedPortions { get; set; }
        public decimal CostPerServing { get; set; }
        public decimal ExpectedLineCost { get; set; }
        public decimal CostPerGuest { get; set; }
    }

    public class SilaCulinaryMenuCostDto
    {
        public Guid MenuId { get; set; }
        public string MenuName { get; set; } = string.Empty;
        public string ServiceStyle { get; set; } = "BUFFET";
        public int PlannedGuests { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public decimal TotalFoodCost { get; set; }
        public decimal TotalBeverageCost { get; set; }
        public decimal TotalMenuCost { get; set; }
        public decimal FoodCostPerGuest { get; set; }
        public decimal BeverageCostPerGuest { get; set; }
        public decimal ExpectedCostPerGuest { get; set; }
        public decimal? TotalPackageRevenue { get; set; }
        public decimal? ExpectedContribution { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public string FoodCostBasisLabel { get; set; } = string.Empty;
        public decimal? BeverageCostPercent { get; set; }
        public string BeverageCostBasisLabel { get; set; } = string.Empty;
        public List<SilaCulinaryMenuCostLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryMaterialPriceImpactRequestDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public decimal NewUnitPrice { get; set; }
        public string? Currency { get; set; }
    }

    public class SilaCulinaryMaterialPriceImpactLineDto
    {
        public string RecipeOrEventRef { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = "RECIPE";
        public decimal RequiredQty { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal CurrentCost { get; set; }
        public decimal ProjectedCost { get; set; }
        public decimal CostDelta { get; set; }
    }

    public class SilaCulinaryMaterialPriceImpactDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string ReportingCurrency { get; set; } = "AED";
        public decimal CurrentUnitPrice { get; set; }
        public decimal NewUnitPrice { get; set; }
        public decimal TotalCurrentCost { get; set; }
        public decimal TotalProjectedCost { get; set; }
        public decimal TotalCostDelta { get; set; }
        public List<SilaCulinaryMaterialPriceImpactLineDto> AffectedItems { get; set; } = new();
    }

    public class SilaCulinaryEventRevenueLineDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = "FOOD_PACKAGE";
        public string Description { get; set; } = string.Empty;
        public string RevenueStatus { get; set; } = "PLANNED";
        public string BillingBasis { get; set; } = "PER_GUEST";
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal GrossAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetRevenue { get; set; }
        public string? SourceReference { get; set; }
    }

    public class SilaCulinaryAddRevenueLineRequestDto
    {
        public string Category { get; set; } = "FOOD_PACKAGE";
        public string Description { get; set; } = string.Empty;
        public string RevenueStatus { get; set; } = "PLANNED";
        public string BillingBasis { get; set; } = "PER_GUEST";
        public decimal Quantity { get; set; } = 1m;
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string Currency { get; set; } = "AED";
        public string? SourceReference { get; set; }
    }

    public class SilaCulinaryEventExpenseLineDto
    {
        public Guid Id { get; set; }
        public string ExpenseCode { get; set; } = string.Empty;
        public string Category { get; set; } = "OTHER";
        public string Description { get; set; } = string.Empty;
        public string CostBehavior { get; set; } = "FIXED";
        public int? StepThresholdGuests { get; set; }
        public decimal? StepIncrementCost { get; set; }
        public string? SupplierOrEmployeeRef { get; set; }
        public string? EmployeeRole { get; set; }
        public int? StaffCount { get; set; }
        public decimal? PlannedHours { get; set; }
        public decimal? ActualHours { get; set; }
        public decimal? HourlyRate { get; set; } // Masked when user lacks salary/rate permission
        public decimal? OvertimeHours { get; set; }
        public decimal? OvertimeRate { get; set; } // Masked when user lacks salary/rate permission
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = "EA";
        public decimal UnitRate { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal ExchangeRate { get; set; } = 1m;
        public decimal BudgetAmount { get; set; }
        public decimal EstimatedAmount { get; set; }
        public decimal CommittedAmount { get; set; }
        public decimal? ActualVerifiedAmount { get; set; }
        public string TaxTreatment { get; set; } = "RECOVERABLE_VAT_EXCLUDED";
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public string? SupportingDocumentRef { get; set; }
        public DateTime ExpenseDateUtc { get; set; }
        public string? ApprovalReference { get; set; }
        public string? SourceTransaction { get; set; }
        public string? SourceCostPoolId { get; set; }
        public string Status { get; set; } = "ESTIMATED";
        public bool SalaryMasked { get; set; }
    }

    public class SilaCulinaryAddExpenseLineRequestDto
    {
        public string Category { get; set; } = "OTHER";
        public string Description { get; set; } = string.Empty;
        public string CostBehavior { get; set; } = "FIXED";
        public int? StepThresholdGuests { get; set; }
        public decimal? StepIncrementCost { get; set; }
        public string? SupplierOrEmployeeRef { get; set; }
        public string? EmployeeRole { get; set; }
        public int? StaffCount { get; set; }
        public decimal? PlannedHours { get; set; }
        public decimal? HourlyRate { get; set; }
        public decimal? OvertimeHours { get; set; }
        public decimal? OvertimeRate { get; set; }
        public decimal? FixedShiftCost { get; set; }
        public decimal? ExternalAgencyCharge { get; set; }
        public decimal Quantity { get; set; } = 1m;
        public string Uom { get; set; } = "EA";
        public decimal UnitRate { get; set; }
        public string Currency { get; set; } = "AED";
        public decimal BudgetAmount { get; set; }
        public decimal? EstimatedAmount { get; set; }
        public decimal CommittedAmount { get; set; }
        public decimal? ActualVerifiedAmount { get; set; }
        public string TaxTreatment { get; set; } = "RECOVERABLE_VAT_EXCLUDED";
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public string? SupportingDocumentRef { get; set; }
        public string? SourceCostPoolId { get; set; }
        public bool MarkAsActualVerified { get; set; }
    }

    public class SilaCulinaryWaterfallStepDto
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Kind { get; set; } = "COST";
    }

    public class SilaCulinaryPnlStatementRowDto
    {
        public string Section { get; set; } = "REVENUE"; // REVENUE | DIRECT_COST | RESULT
        public string RowCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public decimal OriginalBudget { get; set; }
        public decimal RevisedBudget { get; set; }
        public decimal Estimated { get; set; }
        public decimal AttributableCommitments { get; set; }
        public decimal ForecastAtCompletion { get; set; }
        public decimal? ActualVerified { get; set; }
        public decimal VarianceAmount { get; set; }
        public decimal? VariancePercent { get; set; }
        public string VerificationStatus { get; set; } = "ESTIMATED";
        public string? SourceReference { get; set; }
    }

    public class SilaCulinaryProcurementFinancialLineDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialDescription { get; set; } = string.Empty;
        public string? PoNumber { get; set; }
        public string PoStatus { get; set; } = "DRAFT";
        public decimal PurchasedQty { get; set; }
        public decimal EventAllocatedQty { get; set; }
        public decimal OtherEventsAllocatedQty { get; set; }
        public decimal UnallocatedInventoryQty { get; set; }
        public string Uom { get; set; } = "KG";
        public decimal UnitCost { get; set; }
        public decimal PurchaseValue { get; set; }
        public decimal AttributableEventCommitmentValue { get; set; }
        public decimal RemainingInventoryValue { get; set; }
        public decimal ReceivedValue { get; set; }
        public decimal InvoicedValue { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
    }

    public class SilaCulinaryFinancialExceptionDto
    {
        public Guid Id { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public string Severity { get; set; } = "WARNING";
        public string? MaterialOrRecipeRef { get; set; }
        public decimal? FinancialImpact { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RecommendedAction { get; set; }
        public string? ResponsibleUser { get; set; }
        public string ResolutionStatus { get; set; } = "OPEN";
        public string? ResolutionNote { get; set; }
    }

    public class SilaCulinaryFinancialScenarioDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ScenarioType { get; set; } = "CUSTOM";
        public int SimulatedGuests { get; set; }
        public decimal SimulatedRevenue { get; set; }
        public decimal SimulatedTotalCost { get; set; }
        public decimal SimulatedContribution { get; set; }
        public decimal? SimulatedMarginPercent { get; set; }
        public int? SimulatedBreakEvenGuests { get; set; }
        public bool PromotedToRevision { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class SilaCulinaryEventFinancialPlanSummaryDto
    {
        public Guid Id { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
        public Guid? EventId { get; set; }
        public string? EventCode { get; set; }
        public string? EventName { get; set; }
        public int EventRevision { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? CompanyCode { get; set; }
        public string? VenueName { get; set; }
        public string? CustomerReference { get; set; }
        public DateTime? EventDateUtc { get; set; }
        public int PlannedGuests { get; set; }
        public int GuaranteedGuests { get; set; }
        public int? ActualGuests { get; set; }
        public int BillableGuests { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public string Status { get; set; } = "DRAFT";
        public bool IsSimulation { get; set; }
        public bool IsDemo { get; set; }
        public decimal OriginalBudget { get; set; }
        public decimal RevisedBudget { get; set; }
        public decimal? NetRevenue { get; set; }
        public decimal? EstimatedFoodCost { get; set; }
        public decimal? EstimatedBeverageCost { get; set; }
        public decimal? EstimatedManpowerCost { get; set; }
        public decimal? EstimatedTotalCost { get; set; }
        public decimal? ApprovedProcurementCommitment { get; set; }
        public decimal? AttributableProcurementCommitment { get; set; }
        public decimal? ActualVerifiedCost { get; set; }
        public bool ActualIsProvisional { get; set; }
        public decimal? ForecastAtCompletionCost { get; set; }
        public decimal? ContributionAfterOverhead { get; set; }
        public decimal? ContributionMarginPercent { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? BeverageCostPercent { get; set; }
        public decimal? CostPerGuest { get; set; }
        public decimal? RevenuePerGuest { get; set; }
        public decimal? RemainingBudget { get; set; }
        public decimal? TargetMarginVariancePercent { get; set; }
        public int? BreakEvenGuests { get; set; }
        public decimal? MaxAllowableCost { get; set; }
        public decimal? RemainingCostHeadroom { get; set; }
        public string? ApprovalReference { get; set; }
        public DateTime CalculatedAtUtc { get; set; }
    }

    public class SilaCulinaryEventFinancialPlanDetailDto
    {
        public SilaCulinaryEventFinancialPlanSummaryDto Summary { get; set; } = new();
        public List<SilaCulinaryEventRevenueLineDto> RevenueLines { get; set; } = new();
        public List<SilaCulinaryEventExpenseLineDto> ExpenseLines { get; set; } = new();
        public List<SilaCulinaryProcurementFinancialLineDto> ProcurementLines { get; set; } = new();
        public List<SilaCulinaryPnlStatementRowDto> StatementRows { get; set; } = new();
        public List<SilaCulinaryWaterfallStepDto> Waterfall { get; set; } = new();
        public List<SilaCulinaryFinancialExceptionDto> Exceptions { get; set; } = new();
        public List<SilaCulinaryFinancialScenarioDto> Scenarios { get; set; } = new();
        public List<SilaCulinaryAuditEntryDto> History { get; set; } = new();
        public bool CostsVisible { get; set; }
        public bool SalariesVisible { get; set; }
        public bool FinancialStatementsVisible { get; set; }
        public SilaCulinaryPhase3BHandoverDto? Phase3BHandover { get; set; }
        public SilaCulinaryPhase4BHandoverDto? Phase4BHandover { get; set; }
    }

    public class SilaCulinaryCreateEventFinancialPlanRequestDto
    {
        public Guid? EventId { get; set; }
        public Guid? ProcurementPlanId { get; set; }
        public string? Name { get; set; }
        public int PlannedGuests { get; set; } = 500;
        public int GuaranteedGuests { get; set; } = 500;
        public int? ActualGuests { get; set; }
        public string BillablePolicy { get; set; } = "GUARANTEED_PLUS_ADDITIONAL";
        public string? ReportingCurrency { get; set; }
        public decimal SellingPricePerGuest { get; set; } = 350m;
        public decimal DiscountPercent { get; set; }
        public decimal? FoodCostPerGuest { get; set; }
        public decimal? BeverageCostPerGuest { get; set; }
        public decimal? OriginalBudget { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public decimal? TargetBeverageCostPercent { get; set; }
        public decimal? TargetContributionMarginPercent { get; set; }
        public string? CostCenter { get; set; }
        public string? ProfitCenter { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public bool IsSimulation { get; set; }
        public bool IsDemo { get; set; }
        public int? VersionNumber { get; set; }
        public string? IdempotencyKey { get; set; }
    }

    public class SilaCulinarySimulateFinancialRequestDto
    {
        public int PlannedGuests { get; set; } = 500;
        public int GuaranteedGuests { get; set; } = 500;
        public decimal SellingPricePerGuest { get; set; } = 350m;
        public decimal FixedEventRevenue { get; set; }
        public decimal AdditionalGuestRevenue { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal FoodCostPerGuest { get; set; } = 90m;
        public decimal BeverageCostPerGuest { get; set; } = 25m;
        public decimal IngredientPriceChangePercent { get; set; }
        public decimal ManpowerCost { get; set; } = 15000m;
        public decimal EquipmentCost { get; set; } = 5000m;
        public decimal DecorationCost { get; set; } = 10000m;
        public decimal ExternalServiceCost { get; set; }
        public decimal OtherDirectCost { get; set; } = 5000m;
        public decimal AllocatedOverhead { get; set; }
        public decimal TargetContributionMarginPercent { get; set; } = 40m;
        public bool SaveScenario { get; set; }
        public string? ScenarioName { get; set; }
        public string ScenarioType { get; set; } = "CUSTOM";
    }

    public class SilaCulinarySimulateFinancialResultDto
    {
        public Guid? SavedScenarioId { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public int PlannedGuests { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal VariableCosts { get; set; }
        public decimal FixedCosts { get; set; }
        public decimal TotalEventCost { get; set; }
        public decimal EventContribution { get; set; }
        public decimal ContributionMarginPercent { get; set; }
        public decimal FoodCostPercent { get; set; }
        public decimal BeverageCostPercent { get; set; }
        public decimal CostPerGuest { get; set; }
        public decimal ContributionPerGuest { get; set; }
        public int? BreakEvenGuests { get; set; }
        public decimal? BreakEvenRevenue { get; set; }
        public decimal RequiredSellingPricePerGuest { get; set; }
        public decimal MaxAllowableCost { get; set; }
        public decimal BudgetHeadroom { get; set; }
        public List<SilaCulinaryWaterfallStepDto> Waterfall { get; set; } = new();
    }

    public class SilaCulinaryFinancialDashboardDto
    {
        public string ReportingCurrency { get; set; } = "AED";
        public decimal? TotalPlannedEventRevenue { get; set; }
        public decimal? TotalEstimatedEventCost { get; set; }
        public decimal? TotalAttributableCommitments { get; set; }
        public decimal? TotalForecastEventContribution { get; set; }
        public decimal? VerifiedActualContribution { get; set; }
        public decimal? WeightedAverageEventMarginPercent { get; set; }
        public decimal? WeightedAverageFoodCostPercent { get; set; }
        public decimal? WeightedAverageBeverageCostPercent { get; set; }
        public int EventsBelowTargetMarginCount { get; set; }
        public int EventsExceedingBudgetCount { get; set; }
        public int PendingFinancialApprovalsCount { get; set; }
        public int UnresolvedFinancialExceptionsCount { get; set; }
        public List<SilaCulinaryEventFinancialPlanSummaryDto> Plans { get; set; } = new();
    }

    public class SilaCulinaryFinancialAiRequestDto
    {
        public string Question { get; set; } = string.Empty;
        public string TaskName { get; set; } = "FOOD_COST_RECOMMENDATIONS";
    }

    public class SilaCulinaryFinancialAiResponseDto
    {
        public string Answer { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = "DETERMINISTIC_LOCAL";
        public List<string> GroundedFacts { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool PromptInjectionBlocked { get; set; }
    }

    public class SilaCulinaryFinancialReportCatalogItemDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "EVENT_FINANCIAL";
        public string Description { get; set; } = string.Empty;
    }

    public class SilaCulinaryFinancialReportResultDto
    {
        public string ReportCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "text/csv";
        public string CsvContent { get; set; } = string.Empty;
        public List<Dictionary<string, string>> PreviewRows { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryEventCancellationExposureDto
    {
        public Guid FinancialPlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string ReportingCurrency { get; set; } = "AED";
        public decimal CustomerDepositsOrInvoiced { get; set; }
        public decimal NonRefundableSupplierCommitments { get; set; }
        public decimal ReleasedPoExposure { get; set; }
        public decimal ReceivedInventoryValue { get; set; }
        public decimal RecoverableInventoryValue { get; set; }
        public decimal ActualIncurredExpenses { get; set; }
        public decimal NetCancellationExposure { get; set; }
        public List<string> PolicyNotes { get; set; } = new();
    }

    public class SilaCulinaryPhase3BHandoverDto
    {
        public string ContractVersion { get; set; } = "PHASE3B-HANDOVER-1.0";
        public Guid FinancialPlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public Guid? EventId { get; set; }
        public string? EventCode { get; set; }
        public Guid? MenuId { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public decimal EventRevenue { get; set; }
        public decimal EventCost { get; set; }
        public decimal EventContribution { get; set; }
        public decimal? EventMarginPercent { get; set; }
        public decimal BudgetVariance { get; set; }
        public decimal PriceVariance { get; set; }
        public decimal QuantityVariance { get; set; }
        public decimal GuestVolumeVariance { get; set; }
        public string VerificationStatus { get; set; } = "ESTIMATED";
        public List<SilaCulinaryPhase3BRecipeSummaryDto> Recipes { get; set; } = new();
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryPhase3BRecipeSummaryDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public int RecipeVersion { get; set; }
        public decimal IngredientCost { get; set; }
        public decimal CostPerServing { get; set; }
        public decimal? NetSellingPrice { get; set; }
        public decimal? RecipeContribution { get; set; }
        public decimal? RecipeContributionMarginPercent { get; set; }
        public decimal PosSalesQuantity { get; set; }
    }

    public class SilaCulinaryPhase4BHandoverDto
    {
        public string ContractVersion { get; set; } = "PHASE4B-HANDOVER-1.0";
        public Guid FinancialPlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public int FinancialPlanVersion { get; set; }
        public Guid? EventId { get; set; }
        public int EventRevision { get; set; }
        public Guid? MrpRunId { get; set; }
        public Guid? ProcurementPlanId { get; set; }
        public string ReportingCurrency { get; set; } = "AED";
        public decimal ApprovedFinancialBaselineCost { get; set; }
        public decimal ExpectedConsumptionCost { get; set; }
        public decimal ActualConsumptionCost { get; set; }
        public bool ActualIsProvisional { get; set; } = true;
        public bool EventFinanciallyClosed { get; set; } = false; // Always false in Phase 3A (§34, §76)
        public int PlannedGuests { get; set; }
        public int? ActualGuests { get; set; }
        public decimal ActualRecognizedRevenue { get; set; }
        public decimal OutstandingCommitments { get; set; }
        public List<string> WastageCategories { get; set; } = new()
        {
            "PREPARATION_WASTE", "COOKING_WASTE", "SPOILAGE", "EXPIRY",
            "BREAKAGE", "BUFFET_WASTE", "OVERPRODUCTION", "SERVICE_WASTE"
        };
        public List<string> PoReferences { get; set; } = new();
        public List<string> GrnReferences { get; set; } = new();
        public string? ApprovalReference { get; set; }
        public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryAuditEntryDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public Guid? ActorUserId { get; set; }
        public DateTime TimestampUtc { get; set; }
    }

    public class SilaCulinaryMobileFinancialSummaryDto
    {
        public Guid? FinancialPlanId { get; set; }
        public string PlanCode { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string ReportingCurrency { get; set; } = "AED";
        public int PlannedGuests { get; set; }
        public decimal FoodCost { get; set; }
        public decimal BeverageCost { get; set; }
        public decimal ApprovedBudget { get; set; }
        public decimal BudgetUtilizationPercent { get; set; }
        public decimal? EventContribution { get; set; }
        public decimal? ContributionMarginPercent { get; set; }
        public int OpenExceptionsCount { get; set; }
        public string FinancialReadiness { get; set; } = "READY";
        public string Status { get; set; } = "DRAFT";
    }
}
