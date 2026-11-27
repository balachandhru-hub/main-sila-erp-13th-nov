namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryMaterialWorkspaceDto
    {
        public Guid DraftId { get; set; }
        public string DraftReference { get; set; } = string.Empty;
        public int DraftRevision { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal Servings { get; set; }
        public string? ServingSize { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? KitchenName { get; set; }
        public string? PlantCode { get; set; }
        public string OperatingMode { get; set; } = "LOCAL";
        public string MatchingEngine { get; set; } = "SILA Local Material Intelligence v1";
        public bool CanViewCost { get; set; }
        public bool CanApproveAliases { get; set; }
        public DateTime? LastMatchedOnUtc { get; set; }
        public string ReadinessStatus { get; set; } = "MATERIAL_MAPPING_PENDING";
        public SilaCulinaryMaterialKpisDto Kpis { get; set; } = new();
        public List<SilaCulinaryMaterialMappingLineDto> Lines { get; set; } = new();
    }

    public class SilaCulinaryMaterialKpisDto
    {
        public int TotalIngredients { get; set; }
        public int ExactMatches { get; set; }
        public int AliasMatches { get; set; }
        public int SuggestedMatches { get; set; }
        public int ConfirmedMappings { get; set; }
        public int PendingReview { get; set; }
        public int MissingMaterials { get; set; }
        public int UomConflicts { get; set; }
        public int SpecificationConflicts { get; set; }
        public int StockShortages { get; set; }
        public int MissingPrices { get; set; }
    }

    public class SilaCulinaryMaterialMappingLineDto
    {
        public Guid MappingId { get; set; }
        public int MappingRevision { get; set; }
        public string IngredientTempRef { get; set; } = string.Empty;
        public string OriginalDescription { get; set; } = string.Empty;
        public string NormalizedDescription { get; set; } = string.Empty;
        public string? IngredientImageUrl { get; set; }
        public decimal RequiredQuantity { get; set; }
        public string RecipeUom { get; set; } = "EA";
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialDescription { get; set; }
        public string? MaterialImageUrl { get; set; }
        public string? BaseUom { get; set; }
        public string? PurchasingUom { get; set; }
        public decimal? ConvertedQuantity { get; set; }
        public decimal? ConversionFactor { get; set; }
        public decimal? IndicativePacksNeeded { get; set; }
        public int? IndicativeWholePacks { get; set; }
        public decimal? ExcessAfterPackRounding { get; set; }
        public decimal? AvailableStock { get; set; }
        public decimal? OnHandStock { get; set; }
        public decimal? ReservedStock { get; set; }
        public string? StockUom { get; set; }
        public string StockStatus { get; set; } = "UNKNOWN";
        public decimal? IndicativeShortage { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public string? PriceUom { get; set; }
        public string? PriceSource { get; set; }
        public decimal? ExtendedLineCost { get; set; }
        public string PriceStatus { get; set; } = "MISSING";
        public string MatchMethod { get; set; } = "NONE";
        public decimal MatchScore { get; set; }
        public string MappingStatus { get; set; } = "UNMATCHED";
        public string MissingClassification { get; set; } = "NONE";
        public string? RecommendedRequestType { get; set; }
        public bool SpecificationCompatible { get; set; } = true;
        public bool UomCompatible { get; set; } = true;
        public bool RequiresReview { get; set; } = true;
        public bool IsSubstitution { get; set; }
        public string? SubstitutedFromMaterialCode { get; set; }
        public decimal? ReplacementRatio { get; set; }
        public string? OverrideReason { get; set; }
        public string? MissingReason { get; set; }
        public string? ConfirmedByName { get; set; }
        public DateTime? ConfirmedOnUtc { get; set; }
        public DateTime? LastMatchedOnUtc { get; set; }
        public object? ExtractedAttributes { get; set; }
        public List<string> Evidence { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<SilaCulinaryMaterialCandidateDto> Candidates { get; set; } = new();
        public SilaCulinaryMaterialRequestDraftDto? RequestDraft { get; set; }
    }

    public class SilaCulinaryMaterialCandidateDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? MaterialGroup { get; set; }
        public string? ProductType { get; set; }
        public string BaseUom { get; set; } = "EA";
        public string? OrderUom { get; set; }
        public string? AlternateUom { get; set; }
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public string MatchMethod { get; set; } = "NONE";
        public double FinalScore { get; set; }
        public double ExactScore { get; set; }
        public double AliasScore { get; set; }
        public double FuzzyScore { get; set; }
        public double SemanticScore { get; set; }
        public double RerankScore { get; set; }
        public bool SpecificationCompatible { get; set; } = true;
        public bool UomCompatible { get; set; } = true;
        public bool MaterialActive { get; set; } = true;
        public bool PlantEligible { get; set; } = true;
        public decimal? ConvertedBaseQuantity { get; set; }
        public decimal? ConversionFactor { get; set; }
        public decimal? IndicativePacksNeeded { get; set; }
        public int? IndicativeWholePacks { get; set; }
        public decimal? ExcessAfterPackRounding { get; set; }
        public string? UomReason { get; set; }
        public SilaCulinaryMaterialStockDto? Stock { get; set; }
        public SilaCulinaryMaterialPriceDto? Price { get; set; }
        public List<string> Evidence { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    public class SilaCulinaryMaterialStockDto
    {
        public string Status { get; set; } = "UNKNOWN";
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? StorageLocation { get; set; }
        public decimal? OnHandQty { get; set; }
        public decimal? AvailableQty { get; set; }
        public decimal? ReservedQty { get; set; }
        public string? InventoryUom { get; set; }
        public DateTime? StockTimestampUtc { get; set; }
    }

    public class SilaCulinaryMaterialPriceDto
    {
        public string Status { get; set; } = "MISSING";
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public string? PriceUom { get; set; }
        public string? PriceSource { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? EffectiveDateUtc { get; set; }
        public DateTime? DataTimestampUtc { get; set; }
    }

    public class SilaCulinaryMaterialRequestDraftDto
    {
        public string RequestType { get; set; } = "NEW_MATERIAL";
        public string ProposedDescription { get; set; } = string.Empty;
        public string? ProposedLongDescription { get; set; }
        public string? IngredientCategory { get; set; }
        public string? ProposedMaterialGroup { get; set; }
        public string? ProposedBaseUom { get; set; }
        public string? ProposedPurchasingUom { get; set; }
        public string? ProposedPackSize { get; set; }
        public string? BrandPreference { get; set; }
        public string? ProductSpecification { get; set; }
        public string? StorageRequirement { get; set; }
        public List<string> PotentialAllergens { get; set; } = new();
        public Guid DraftId { get; set; }
        public string DraftReference { get; set; } = string.Empty;
        public string IngredientTempRef { get; set; } = string.Empty;
        public decimal RequiredQuantity { get; set; }
        public string RecipeUom { get; set; } = "EA";
        public Guid? PropertyId { get; set; }
        public string? PlantCode { get; set; }
        public string? RequestedBy { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> DuplicateCheckEvidence { get; set; } = new();
        public List<string> CandidateMaterialCodesChecked { get; set; } = new();
        public bool IsAiSuggested { get; set; } = true;
    }

    public class SilaCulinaryMaterialMatchRequestDto
    {
        public List<string>? IngredientRefs { get; set; }
        public bool UnresolvedOnly { get; set; }
        public bool OverwriteConfirmed { get; set; }
        public bool UseAiDisambiguation { get; set; }
        public Guid? PropertyId { get; set; }
    }

    public class SilaCulinaryMaterialActionDto
    {
        public int? ExpectedMappingRevision { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? Reason { get; set; }
        public string? FeedbackType { get; set; }
        public bool SuggestAlias { get; set; }
    }

    public class SilaCulinaryMaterialBulkAcceptDto
    {
        public List<string>? IngredientRefs { get; set; }
    }

    public class SilaCulinaryMaterialRecalcDto
    {
        public decimal Servings { get; set; }
        public string? ServingSize { get; set; }
        public Guid? PropertyId { get; set; }
    }

    public class SilaCulinaryMaterialSearchItemDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? MaterialGroup { get; set; }
        public string? ProductType { get; set; }
        public string BaseUom { get; set; } = "EA";
        public string? OrderUom { get; set; }
        public string? AlternateUom { get; set; }
        public string? Barcode { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool PlantEligible { get; set; }
        public SilaCulinaryMaterialStockDto? Stock { get; set; }
        public SilaCulinaryMaterialPriceDto? Price { get; set; }
    }

    public class SilaCulinaryMaterialAliasDto
    {
        public Guid Id { get; set; }
        public string AliasText { get; set; } = string.Empty;
        public string NormalizedAlias { get; set; } = string.Empty;
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string? MaterialDescription { get; set; }
        public Guid? PropertyId { get; set; }
        public string Language { get; set; } = "en";
        public string Source { get; set; } = "CHEF_SUBMITTED";
        public string ApprovalStatus { get; set; } = "PENDING";
        public int Revision { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedOnUtc { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class SilaCulinaryMaterialAliasCreateDto
    {
        public string AliasText { get; set; } = string.Empty;
        public Guid MaterialId { get; set; }
        public Guid? PropertyId { get; set; }
        public string Language { get; set; } = "en";
    }

    public class SilaCulinarySubstitutionApplyDto
    {
        public int? ExpectedMappingRevision { get; set; }
        public string ProposedIngredient { get; set; } = string.Empty;
        public Guid? ProposedMaterialId { get; set; }
        public string? ProposedMaterialCode { get; set; }
        public decimal? ReplacementRatio { get; set; }
        public string? Reason { get; set; }
    }

    public class SilaCulinaryMaterialRequestDraftCreateDto
    {
        public int? ExpectedMappingRevision { get; set; }
        public string RequestType { get; set; } = "NEW_MATERIAL";
        public string ProposedDescription { get; set; } = string.Empty;
        public string? ProposedLongDescription { get; set; }
        public string? ProposedMaterialGroup { get; set; }
        public string? ProposedBaseUom { get; set; }
        public string? ProposedPurchasingUom { get; set; }
        public string? ProposedPackSize { get; set; }
        public string? BrandPreference { get; set; }
        public string? ProductSpecification { get; set; }
        public string? StorageRequirement { get; set; }
        public List<string>? PotentialAllergens { get; set; }
        public string? Reason { get; set; }
    }

    public class SilaCulinaryMaterialIndexStatusDto
    {
        public Guid BuyerId { get; set; }
        public string Status { get; set; } = "EMPTY";
        public int IndexedMaterialCount { get; set; }
        public int ActiveMaterialCount { get; set; }
        public int PendingUpdatesCount { get; set; }
        public string EmbeddingModelVersion { get; set; } = "BAAI/bge-small-en-v1.5";
        public string RerankingModelVersion { get; set; } = "Xenova/ms-marco-MiniLM-L-6-v2";
        public string SemanticBackend { get; set; } = "FASTEMBED_ONNX";
        public double BuildDurationMs { get; set; }
        public DateTime? LastBuildCompletedUtc { get; set; }
        public DateTime? LastIncrementalSyncUtc { get; set; }
        public string? LastError { get; set; }
    }

    public class SilaCulinaryMaterialDashboardDto
    {
        public bool IsRealTelemetry { get; set; } = true;
        public int TotalIngredientsProcessed { get; set; }
        public int ExactMatches { get; set; }
        public int AliasMatches { get; set; }
        public int FuzzyMatches { get; set; }
        public int SemanticMatches { get; set; }
        public int AiAssistedMatches { get; set; }
        public int ConfirmedMappings { get; set; }
        public int RejectedCandidates { get; set; }
        public int PendingReview { get; set; }
        public int MissingMaterials { get; set; }
        public int UomConflicts { get; set; }
        public int ActiveAliases { get; set; }
        public double LocalMatchingUtilizationPct { get; set; }
        public double ExternalAiUtilizationPct { get; set; }
        public SilaCulinaryMaterialIndexStatusDto IndexHealth { get; set; } = new();
        public List<SilaCulinaryChartPointDto> MethodDistribution { get; set; } = new();
        public List<SilaCulinaryChartPointDto> StatusDistribution { get; set; } = new();
        public List<SilaCulinaryChartPointDto> UnresolvedIngredients { get; set; } = new();
    }

    public class SilaCulinaryChartPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class SilaCulinaryPhase1CHandoverDto
    {
        public Guid DraftId { get; set; }
        public string DraftReference { get; set; } = string.Empty;
        public int DraftRevision { get; set; }
        public string RecipeName { get; set; } = string.Empty;
        public decimal Servings { get; set; }
        public string? ServingSize { get; set; }
        public string ReadinessStatus { get; set; } = "MATERIAL_MAPPING_PENDING";
        public bool ReadyForPhase1C { get; set; }
        public List<string> OutstandingValidationIssues { get; set; } = new();
        public List<SilaCulinaryMaterialMappingLineDto> IngredientLines { get; set; } = new();
        public List<SilaCulinaryMaterialRequestDraftDto> MaterialRequestDrafts { get; set; } = new();
    }

    public class SilaCulinaryMaterialHistoryDto
    {
        public Guid Id { get; set; }
        public string IngredientTempRef { get; set; } = string.Empty;
        public int MappingRevision { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? PreviousStatus { get; set; }
        public string NewStatus { get; set; } = string.Empty;
        public string? PreviousMaterialCode { get; set; }
        public string? NewMaterialCode { get; set; }
        public string? Reason { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }
}
