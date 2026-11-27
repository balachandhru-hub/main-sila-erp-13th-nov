namespace Buyer.Domain.Dtos
{
    // ==================== MATERIAL REQUESTS (§5 - §17, §39) ====================

    public class SilaCulinaryMaterialRequestLinkDto
    {
        public Guid Id { get; set; }
        public Guid DraftId { get; set; }
        public string? DraftReference { get; set; }
        public string? RecipeName { get; set; }
        public string IngredientTempRef { get; set; } = string.Empty;
        public string IngredientDescription { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string RecipeUom { get; set; } = "KG";
        public bool Reconnected { get; set; }
        public bool ChefConfirmed { get; set; }
        public DateTime? ReconnectedOnUtc { get; set; }
    }

    public class SilaCulinaryMaterialRequestHistoryDto
    {
        public Guid Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public Guid ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public string? Comment { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class SilaCulinaryDuplicateCandidateDto
    {
        public Guid? MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Similarity { get; set; }
        public bool IsActive { get; set; }
        public bool PlantEligible { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class SilaCulinaryMaterialRequestDto
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public string RequestReference { get; set; } = string.Empty;
        public string RequestType { get; set; } = "NEW_MATERIAL";
        public string Status { get; set; } = "DRAFT";
        public int RevisionNumber { get; set; }
        public int? ApprovedRevisionNumber { get; set; }
        public string ProposedDescription { get; set; } = string.Empty;
        public string? LongDescription { get; set; }
        public string? IngredientCategory { get; set; }
        public string? MaterialType { get; set; }
        public string? MaterialGroup { get; set; }
        public string? ProductHierarchy { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public string? ProductSpecification { get; set; }
        public string? Barcode { get; set; }
        public string? CountryOfOrigin { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string BaseUom { get; set; } = "KG";
        public string? PurchasingUom { get; set; }
        public string? AlternateUom { get; set; }
        public decimal? PackSize { get; set; }
        public decimal? ConversionNumerator { get; set; }
        public decimal? ConversionDenominator { get; set; }
        public decimal? NetWeight { get; set; }
        public decimal? GrossWeight { get; set; }
        public decimal? Volume { get; set; }
        public string? IngredientForm { get; set; }
        public string? StorageRequirements { get; set; }
        public int? ShelfLifeDays { get; set; }
        public bool BatchManaged { get; set; }
        public string? PotentialAllergens { get; set; }
        public string? DietaryClassification { get; set; }
        public string? QualitySpecifications { get; set; }
        public string? FoodSafetyRequirements { get; set; }
        public decimal? RequiredQuantity { get; set; }
        public DateTime? RequiredDateUtc { get; set; }
        public Guid? RequestedByUserId { get; set; }
        public string? RequestedByName { get; set; }
        public string? BusinessJustification { get; set; }
        public string? SuggestedSupplier { get; set; }
        public Guid? TargetMaterialId { get; set; }
        public string? TargetMaterialCode { get; set; }
        public string DataOrigin { get; set; } = "CHEF_ENTERED";
        public string? IdempotencyKey { get; set; }
        public string? CorrelationId { get; set; }
        public string? ExecutionReference { get; set; }
        public string ExecutionStatus { get; set; } = "NOT_STARTED";
        public Guid? ResultingMaterialId { get; set; }
        public string? ResultingMaterialCode { get; set; }
        public string? LastError { get; set; }
        public string? DecisionComment { get; set; }
        public bool IsErpIntegratedTenant { get; set; }
        public bool HasErpCreationApi { get; set; }
        public List<SilaCulinaryDuplicateCandidateDto> DuplicateCandidates { get; set; } = new();
        public List<string> Attachments { get; set; } = new();
        public List<SilaCulinaryMaterialRequestLinkDto> Links { get; set; } = new();
        public List<SilaCulinaryMaterialRequestHistoryDto> History { get; set; } = new();
        public DateTime? SubmittedOnUtc { get; set; }
        public DateTime? ApprovedOnUtc { get; set; }
        public DateTime? MaterialAvailableOnUtc { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
    }

    public class SilaCulinaryMaterialRequestLinkInputDto
    {
        public Guid DraftId { get; set; }
        public string IngredientTempRef { get; set; } = string.Empty;
        public string? IngredientDescription { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string? RecipeUom { get; set; }
    }

    public class SilaCulinaryMaterialRequestUpsertDto
    {
        public string? RequestType { get; set; }
        public string ProposedDescription { get; set; } = string.Empty;
        public string? LongDescription { get; set; }
        public string? IngredientCategory { get; set; }
        public string? MaterialType { get; set; }
        public string? MaterialGroup { get; set; }
        public string? ProductHierarchy { get; set; }
        public string? Brand { get; set; }
        public string? Manufacturer { get; set; }
        public string? ProductSpecification { get; set; }
        public string? Barcode { get; set; }
        public string? CountryOfOrigin { get; set; }
        public Guid? PropertyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? BaseUom { get; set; }
        public string? PurchasingUom { get; set; }
        public string? AlternateUom { get; set; }
        public decimal? PackSize { get; set; }
        public decimal? ConversionNumerator { get; set; }
        public decimal? ConversionDenominator { get; set; }
        public decimal? NetWeight { get; set; }
        public decimal? GrossWeight { get; set; }
        public decimal? Volume { get; set; }
        public string? IngredientForm { get; set; }
        public string? StorageRequirements { get; set; }
        public int? ShelfLifeDays { get; set; }
        public bool BatchManaged { get; set; }
        public string? PotentialAllergens { get; set; }
        public string? DietaryClassification { get; set; }
        public string? QualitySpecifications { get; set; }
        public string? FoodSafetyRequirements { get; set; }
        public decimal? RequiredQuantity { get; set; }
        public DateTime? RequiredDateUtc { get; set; }
        public string? BusinessJustification { get; set; }
        public string? SuggestedSupplier { get; set; }
        public Guid? TargetMaterialId { get; set; }
        public string? TargetMaterialCode { get; set; }
        public string? DataOrigin { get; set; }
        public string? IdempotencyKey { get; set; }
        public bool AllowDuplicateOverride { get; set; }
        public List<string>? Attachments { get; set; }
        public List<SilaCulinaryMaterialRequestLinkInputDto> Links { get; set; } = new();
    }

    public class SilaCulinaryBulkMaterialRequestDto
    {
        public Guid? PropertyId { get; set; }
        public bool ConsolidateEquivalent { get; set; } = true;
        public string? BusinessJustification { get; set; }
        public List<SilaCulinaryMaterialRequestLinkInputDto> Items { get; set; } = new();
    }

    public class SilaCulinaryMaterialRequestDecisionDto
    {
        /// <summary>APPROVE, REJECT, RETURN, CLARIFY, CANCEL.</summary>
        public string Decision { get; set; } = "APPROVE";
        public string? Comment { get; set; }
        public string? TargetMaterialCode { get; set; }
    }

    public class SilaCulinaryManualErpVerifyDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public bool AutoReconnectDrafts { get; set; } = true;
    }

    public class SilaCulinaryMaterialRequestAssistInputDto
    {
        public string IngredientDescription { get; set; } = string.Empty;
        public decimal? RequiredQty { get; set; }
        public string? RecipeUom { get; set; }
        public Guid? DraftId { get; set; }
        public Guid? PropertyId { get; set; }
    }

    public class SilaCulinaryMaterialRequestAssistResultDto
    {
        public string RecommendedRequestType { get; set; } = "NEW_MATERIAL";
        public string ProposedDescription { get; set; } = string.Empty;
        public string ProposedLongDescription { get; set; } = string.Empty;
        public string IngredientCategory { get; set; } = string.Empty;
        public string ProposedMaterialGroup { get; set; } = string.Empty;
        public string ProposedBaseUom { get; set; } = "KG";
        public string ProductSpecification { get; set; } = string.Empty;
        public string StorageRequirement { get; set; } = string.Empty;
        public List<string> PotentialAllergens { get; set; } = new();
        public string BusinessJustification { get; set; } = string.Empty;
        public List<SilaCulinaryDuplicateCandidateDto> PotentialDuplicates { get; set; } = new();
        public string ProviderUsed { get; set; } = "LOCAL_DETERMINISTIC";
        public bool AiSuggested { get; set; } = true;
    }

    public class SilaCulinaryMaterialRequestDashboardDto
    {
        public int TotalRequests { get; set; }
        public int DraftRequests { get; set; }
        public int PendingApprovalRequests { get; set; }
        public int ReturnedRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int CreationPendingRequests { get; set; }
        public int CreationFailedRequests { get; set; }
        public int SynchronizationPendingRequests { get; set; }
        public int ResolvedRequests { get; set; }
        public int RecipesBlockedByMissingMaterials { get; set; }
        public double? AverageResolutionHours { get; set; }
        public bool IsErpIntegratedTenant { get; set; }
        public bool HasErpCreationApi { get; set; }
        public Dictionary<string, int> ByType { get; set; } = new();
        public Dictionary<string, int> AgingByStage { get; set; } = new();
    }

    // ==================== RECIPE COSTING & SCENARIOS (§19 - §38) ====================

    public class SilaCulinaryCostingOverrideDto
    {
        public string TempRef { get; set; } = string.Empty;
        public decimal? Quantity { get; set; }
        public string? RecipeUom { get; set; }
        public string? QuantityBasis { get; set; }
        public decimal? YieldPercent { get; set; }
        public decimal? PrepLossPercent { get; set; }
        public decimal? CookLossPercent { get; set; }
        public decimal? WastagePercent { get; set; }
        public decimal? BufferPercent { get; set; }
        public decimal? ManualUnitPrice { get; set; }
        public string? ManualPriceUom { get; set; }
        public decimal? ManualPriceUnit { get; set; }
        public string? ManualPriceSource { get; set; }
        public string? ManualPriceJustification { get; set; }
    }

    public class SilaCulinaryAdditionalCostInputDto
    {
        public string Code { get; set; } = "LABOR";
        public string Name { get; set; } = "Kitchen Labor";
        /// <summary>DIRECT_OPERATING or ALLOCATED_OVERHEAD.</summary>
        public string Category { get; set; } = "DIRECT_OPERATING";
        /// <summary>FIXED_BATCH, PER_PORTION, PERCENT_OF_INGREDIENT, HOURLY, QUANTITY.</summary>
        public string Basis { get; set; } = "PER_PORTION";
        public decimal Amount { get; set; }
        public decimal Units { get; set; } = 1m;
        public string? Source { get; set; }
    }

    public class SilaCulinaryCostingCalculateRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public decimal? Servings { get; set; }
        public decimal? PortionSize { get; set; }
        public string? PortionUom { get; set; }
        public decimal? SellingPricePerPortion { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public string? CostingBasis { get; set; }
        public string? TaxBasis { get; set; }
        public decimal GlobalPriceIncreasePercent { get; set; }
        public decimal GlobalBufferPercent { get; set; }
        public string? ScenarioName { get; set; }
        public bool SaveSnapshot { get; set; }
        public bool IsBaseline { get; set; } = true;
        public List<SilaCulinaryCostingOverrideDto> Overrides { get; set; } = new();
        public List<SilaCulinaryAdditionalCostInputDto> AdditionalCosts { get; set; } = new();
    }

    public class SilaCulinaryCostedLineDto
    {
        public string TempRef { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSubRecipe { get; set; }
        public Guid? SubRecipeId { get; set; }
        public decimal RecipeQty { get; set; }
        public string RecipeUom { get; set; } = "KG";
        public decimal YieldPercent { get; set; } = 100m;
        public decimal EffectiveGrossQty { get; set; }
        public decimal CostingQty { get; set; }
        public string CostingUom { get; set; } = "KG";
        public decimal? UnitPrice { get; set; }
        public decimal PriceUnit { get; set; } = 1m;
        public string PriceCurrency { get; set; } = "AED";
        public string PriceSource { get; set; } = "MISSING";
        public bool IsEstimatedCost { get; set; }
        public DateTime? PriceTimestampUtc { get; set; }
        public decimal? IngredientCost { get; set; }
        public decimal? PercentageOfTotal { get; set; }
        public string ValidationStatus { get; set; } = "OK";
        public string? IssueReason { get; set; }
    }

    public class SilaCulinaryCostingResultDto
    {
        public Guid? SnapshotId { get; set; }
        public string? CostingReference { get; set; }
        public Guid DraftId { get; set; }
        public int DraftRevision { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string ScenarioName { get; set; } = "Baseline";
        public bool IsBaseline { get; set; } = true;
        public bool IsApprovedSnapshot { get; set; }
        public string ApprovalStatus { get; set; } = "DRAFT";
        public string CostingBasis { get; set; } = "STANDARD_RECIPE_COST";
        public string TaxBasis { get; set; } = "TAX_EXCLUSIVE";
        public string Currency { get; set; } = "AED";
        public decimal Servings { get; set; }
        public decimal TotalIngredientCost { get; set; }
        public decimal SubRecipeCost { get; set; }
        public decimal AdditionalDirectCost { get; set; }
        public decimal AllocatedOverheadCost { get; set; }
        public decimal TotalRecipeCost { get; set; }
        public decimal IngredientCostPerPortion { get; set; }
        public decimal TotalCostPerPortion { get; set; }
        public decimal? SellingPricePerPortion { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public decimal? TargetVariancePercent { get; set; }
        public decimal? RecommendedSellingPriceNet { get; set; }
        public decimal? RecommendedSellingPriceMarkup { get; set; }
        public decimal? EstimatedRevenue { get; set; }
        public decimal? GrossProfit { get; set; }
        public decimal? ContributionAmount { get; set; }
        public decimal? ContributionMarginPercent { get; set; }
        public decimal? EstimatedNetProfitAfterOverhead { get; set; }
        public string CostCompleteness { get; set; } = "COMPLETE";
        public int MissingPriceCount { get; set; }
        public int ExpiredPriceCount { get; set; }
        public int CurrencyMismatchCount { get; set; }
        public bool CircularDependencyDetected { get; set; }
        public bool CanViewCostDetail { get; set; } = true;
        public List<SilaCulinaryCostedLineDto> Lines { get; set; } = new();
        public List<SilaCulinaryAdditionalCostInputDto> AdditionalCosts { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime CalculatedAtUtc { get; set; }
    }

    public class SilaCulinaryCostOptimizationRequestDto
    {
        public string Goal { get; set; } = "Reduce food cost by 10% without compromising recipe quality";
        public SilaCulinaryCostingCalculateRequestDto? CostingRequest { get; set; }
    }

    public class SilaCulinaryCostOptimizationSuggestionDto
    {
        public string SuggestionId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Rationale { get; set; } = string.Empty;
        public string? TargetTempRef { get; set; }
        public decimal ProjectedIngredientCost { get; set; }
        public decimal ProjectedCostSavings { get; set; }
        public decimal? ProjectedFoodCostPercent { get; set; }
        public bool RequiresChefConfirmation { get; set; } = true;
        public string? AllergenOrSafetyWarning { get; set; }
    }

    public class SilaCulinaryCostOptimizationResultDto
    {
        public Guid DraftId { get; set; }
        public string ProviderUsed { get; set; } = "LOCAL_DETERMINISTIC";
        public SilaCulinaryCostingResultDto Baseline { get; set; } = new();
        public List<SilaCulinaryCostOptimizationSuggestionDto> Suggestions { get; set; } = new();
        public List<string> Notes { get; set; } = new();
    }

    public class SilaCulinaryBulkRecostRequestDto
    {
        public List<Guid> DraftIds { get; set; } = new();
        public Guid? PropertyId { get; set; }
        public bool SaveNewSnapshot { get; set; } = true;
    }

    public class SilaCulinaryBulkRecostResultDto
    {
        public int ProcessedCount { get; set; }
        public int ExceedingTargetCount { get; set; }
        public int MissingCostCount { get; set; }
        public List<SilaCulinaryCostingResultDto> Results { get; set; } = new();
    }

    // ==================== READINESS, APPROVAL, RECIPE ID, IMAGE, PUBLISHING, VERSIONING, HANDOVER ====================

    public class SilaCulinaryReadinessCheckItemDto
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Status { get; set; } = "PASSED"; // PASSED, BLOCKING, WARNING
        public string ResponsibleTeam { get; set; } = "Chef";
        public string? Detail { get; set; }
        public string? RecommendedAction { get; set; }
    }

    public class SilaCulinaryRecipeReadinessDto
    {
        public Guid DraftId { get; set; }
        public string DraftReference { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public int DraftRevision { get; set; }
        public string LifecycleStatus { get; set; } = "DRAFT";
        public bool ReadyForApproval { get; set; }
        public bool ReadyForProductionId { get; set; }
        public bool ReadyForPublishing { get; set; }
        public int PassedCount { get; set; }
        public int BlockingCount { get; set; }
        public int WarningCount { get; set; }
        public List<SilaCulinaryReadinessCheckItemDto> Checks { get; set; } = new();
        public Guid? ProductionRecipeId { get; set; }
        public string? ProductionRecipeCode { get; set; }
        public int? ProductionRecipeVersion { get; set; }
    }

    public class SilaCulinaryApprovalPackageDto
    {
        public Guid DraftId { get; set; }
        public string DraftReference { get; set; } = string.Empty;
        public int DraftRevision { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Cuisine { get; set; }
        public decimal BaseServings { get; set; }
        public string ServingUom { get; set; } = "PORTION";
        public List<string> Allergens { get; set; } = new();
        public List<string> DietaryFlags { get; set; } = new();
        public List<string> PreparationSteps { get; set; } = new();
        public SilaCulinaryCostingResultDto? CostingSummary { get; set; }
        public SilaCulinaryRecipeReadinessDto Readiness { get; set; } = new();
        public List<SilaCulinaryMaterialRequestDto> MaterialRequests { get; set; } = new();
        public SilaCulinaryRecipeLifecycleSummaryDto Lifecycle { get; set; } = new();
    }

    public class SilaCulinaryRecipeLifecycleSummaryDto
    {
        public Guid Id { get; set; }
        public Guid DraftId { get; set; }
        public int ApprovedDraftRevision { get; set; }
        public Guid? CostingSnapshotId { get; set; }
        public Guid? ProductionRecipeId { get; set; }
        public string? ProductionRecipeCode { get; set; }
        public int? ProductionRecipeVersion { get; set; }
        public int? ProductionActiveVersion { get; set; }
        public string? ProductionStatus { get; set; }
        public string LifecycleStatus { get; set; } = "DRAFT";
        public bool ImageAssigned { get; set; }
        public string? ImageOrigin { get; set; }
        public string? ApprovalComment { get; set; }
        public string? RejectionReason { get; set; }
        public string? ChangeReason { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedOnUtc { get; set; }
        public DateTime? PublishedOnUtc { get; set; }
        public DateTime? EffectiveDateUtc { get; set; }
    }

    public class SilaCulinaryRecipeApproveActionDto
    {
        public string? Comment { get; set; }
        public string? IdempotencyKey { get; set; }
        public string? ItemMode { get; set; }
        public string? ProductClass { get; set; }
        public string? RecipeFormat { get; set; }
        public Guid? CategoryId { get; set; }
        public DateTime? EffectiveDateUtc { get; set; }
        public string? ChangeReason { get; set; }
    }

    public class SilaCulinaryRecipeVersionHistoryItemDto
    {
        public int Version { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActiveSelling { get; set; }
        public decimal TotalCost { get; set; }
        public string? Currency { get; set; }
        public string? ChangeReason { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime? ApprovedOn { get; set; }
    }

    public class SilaCulinaryLifecycleDashboardDto
    {
        public int AiRecipeDrafts { get; set; }
        public int MaterialMappingPending { get; set; }
        public int MissingMaterialRequests { get; set; }
        public int AwaitingMaterialSync { get; set; }
        public int ReadyForCosting { get; set; }
        public int CostingIncomplete { get; set; }
        public int AboveFoodCostTarget { get; set; }
        public int PendingApproval { get; set; }
        public int ApprovedRecipes { get; set; }
        public int ReadyForPublishing { get; set; }
        public int PublishedRecipes { get; set; }
        public int RequiringRecosting { get; set; }
        public int RecipesWithNewVersions { get; set; }
        public Dictionary<string, int> LifecycleFunnel { get; set; } = new();
        public Dictionary<string, int> RecipesByCategory { get; set; } = new();
        public List<SilaCulinaryCostingResultDto> TopCostDrivers { get; set; } = new();
    }

    public class SilaCulinaryPhase2AHandoverIngredientDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Uom { get; set; } = string.Empty;
        public decimal ConversionFactor { get; set; } = 1m;
        public decimal YieldPercent { get; set; } = 100m;
        public bool Scalable { get; set; } = true;
    }

    public class SilaCulinaryPhase2AHandoverDto
    {
        public Guid ProductionRecipeId { get; set; }
        public string ProductionRecipeCode { get; set; } = string.Empty;
        public int RecipeVersion { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public string? RecipeCategory { get; set; }
        public string? Cuisine { get; set; }
        public decimal BaseServings { get; set; }
        public string ServingUom { get; set; } = "PORTION";
        public string SellingUom { get; set; } = "EA";
        public int? PrepDurationMinutes { get; set; }
        public int? CookDurationMinutes { get; set; }
        public bool ImageAssigned { get; set; }
        public string? PictureEndpoint { get; set; }
        public List<string> Allergens { get; set; } = new();
        public List<string> DietaryClassifications { get; set; } = new();
        public Guid? CostingSnapshotId { get; set; }
        public string? CostingReference { get; set; }
        public decimal CostPerPortion { get; set; }
        public decimal? AuthorizedSellingPrice { get; set; }
        public string Currency { get; set; } = "AED";
        public string PublishingStatus { get; set; } = "APPROVED";
        public DateTime? EffectiveDateUtc { get; set; }
        public List<SilaCulinaryPhase2AHandoverIngredientDto> Ingredients { get; set; } = new();
    }
}
