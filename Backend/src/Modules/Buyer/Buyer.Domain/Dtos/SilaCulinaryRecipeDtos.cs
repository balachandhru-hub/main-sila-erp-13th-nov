using System.Text.Json.Serialization;

namespace Buyer.Domain.Dtos
{
    public class CulinaryLocationContextDto
    {
        public Guid LocationId { get; set; }
        public Guid PropertyId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public Guid? OutletId { get; set; }
        public string? OutletName { get; set; }
    }

    public class CulinaryExistingRecipeRefDto
    {
        public Guid RecipeId { get; set; }
        public string RecipeCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public decimal ServingQty { get; set; }
        public string ServingUom { get; set; } = "EA";
        public string Status { get; set; } = string.Empty;
        public int Version { get; set; }
    }

    public class CulinaryAvailableStockItemDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal OnHandQty { get; set; }
        public string BaseUom { get; set; } = string.Empty;
    }

    public class CulinaryPlannerContextDto
    {
        public bool CanSelectProvider { get; set; }
        public string OperatingMode { get; set; } = "LOCAL";
        public List<string> EnabledProviders { get; set; } = new();
        public bool AllowExternalAi { get; set; }
        public string PromptVersion { get; set; } = "culinary-recipe-v1.0";
        public string Currency { get; set; } = "AED";
        public List<CulinaryLocationContextDto> Locations { get; set; } = new();
        public List<CulinaryExistingRecipeRefDto> ExistingRecipes { get; set; } = new();
        public List<CulinaryAvailableStockItemDto> AvailableStock { get; set; } = new();
    }

    public class CulinaryRecipeHeaderDto
    {
        [JsonPropertyName("proposed_name")]
        public string ProposedName { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("cuisine")]
        public string Cuisine { get; set; } = "International";
        [JsonPropertyName("category")]
        public string Category { get; set; } = "Main Course";
        [JsonPropertyName("recipe_type")]
        public string RecipeType { get; set; } = "STANDARD_RECIPE";
        [JsonPropertyName("base_servings")]
        public decimal BaseServings { get; set; } = 10m;
        [JsonPropertyName("serving_size")]
        public decimal ServingSize { get; set; } = 180m;
        [JsonPropertyName("serving_uom")]
        public string ServingUom { get; set; } = "G";
        [JsonPropertyName("batch_size")]
        public decimal? BatchSize { get; set; }
        [JsonPropertyName("expected_yield_qty")]
        public decimal? ExpectedYieldQty { get; set; }
        [JsonPropertyName("expected_yield_uom")]
        public string? ExpectedYieldUom { get; set; }
        [JsonPropertyName("prep_duration_minutes")]
        public int PrepDurationMinutes { get; set; } = 20;
        [JsonPropertyName("cook_duration_minutes")]
        public int CookDurationMinutes { get; set; } = 30;
        [JsonPropertyName("total_duration_minutes")]
        public int TotalDurationMinutes { get; set; } = 50;
        [JsonPropertyName("cooking_method")]
        public string CookingMethod { get; set; } = string.Empty;
        [JsonPropertyName("difficulty")]
        public string Difficulty { get; set; } = "MEDIUM";
        [JsonPropertyName("dietary_tags")]
        public List<string> DietaryTags { get; set; } = new();
        [JsonPropertyName("potential_allergens")]
        public List<string> PotentialAllergens { get; set; } = new();
        [JsonPropertyName("cross_contact_warnings")]
        public List<string> CrossContactWarnings { get; set; } = new();
        [JsonPropertyName("storage_guidance")]
        public string StorageGuidance { get; set; } = string.Empty;
        [JsonPropertyName("shelf_life_suggestion")]
        public string ShelfLifeSuggestion { get; set; } = string.Empty;
        [JsonPropertyName("chef_verification_required")]
        public bool ChefVerificationRequired { get; set; } = true;
        [JsonPropertyName("nutrition_status")]
        public string NutritionStatus { get; set; } = "UNAVAILABLE_OR_UNVERIFIED";
    }

    public class CulinaryIngredientLineDto
    {
        [JsonPropertyName("temp_ref")]
        public string TempRef { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("normalized_description")]
        public string NormalizedDescription { get; set; } = string.Empty;
        [JsonPropertyName("category")]
        public string Category { get; set; } = "OTHER";
        [JsonPropertyName("ingredient_form")]
        public string IngredientForm { get; set; } = string.Empty;
        [JsonPropertyName("brand_preference")]
        public string? BrandPreference { get; set; }
        [JsonPropertyName("quantity")]
        public decimal Quantity { get; set; }
        [JsonPropertyName("recipe_uom")]
        public string RecipeUom { get; set; } = "KG";
        [JsonPropertyName("normalized_uom")]
        public string NormalizedUom { get; set; } = string.Empty;
        [JsonPropertyName("uom_resolved")]
        public bool UomResolved { get; set; } = true;
        [JsonPropertyName("quantity_basis")]
        public string QuantityBasis { get; set; } = "RAW";
        [JsonPropertyName("raw_quantity")]
        public decimal? RawQuantity { get; set; }
        [JsonPropertyName("edible_quantity")]
        public decimal? EdibleQuantity { get; set; }
        [JsonPropertyName("yield_percent")]
        public decimal YieldPercent { get; set; } = 100m;
        [JsonPropertyName("yield_source")]
        public string YieldSource { get; set; } = "AI_SUGGESTED";
        [JsonPropertyName("prep_loss_percent")]
        public decimal PrepLossPercent { get; set; }
        [JsonPropertyName("cook_loss_percent")]
        public decimal CookLossPercent { get; set; }
        [JsonPropertyName("scaling_method")]
        public string ScalingMethod { get; set; } = "LINEAR";
        [JsonPropertyName("chef_scaling_factor")]
        public decimal? ChefScalingFactor { get; set; }
        [JsonPropertyName("optional_indicator")]
        public bool OptionalIndicator { get; set; }
        [JsonPropertyName("preparation_notes")]
        public string PreparationNotes { get; set; } = string.Empty;
        [JsonPropertyName("suggested_alternatives")]
        public List<string> SuggestedAlternatives { get; set; } = new();
        [JsonPropertyName("material_mapping_status")]
        public string MaterialMappingStatus { get; set; } = "PENDING_MATERIAL_MAPPING";
        [JsonPropertyName("material_reference")]
        public string? MaterialReference { get; set; }
        [JsonPropertyName("provenance")]
        public string Provenance { get; set; } = "AI_GENERATED";
        [JsonPropertyName("locked")]
        public bool Locked { get; set; }
    }

    public class CulinaryInstructionStepDto
    {
        [JsonPropertyName("step_number")]
        public int StepNumber { get; set; }
        [JsonPropertyName("step_title")]
        public string StepTitle { get; set; } = string.Empty;
        [JsonPropertyName("instruction")]
        public string Instruction { get; set; } = string.Empty;
        [JsonPropertyName("estimated_duration_minutes")]
        public int EstimatedDurationMinutes { get; set; }
        [JsonPropertyName("equipment_suggestion")]
        public string EquipmentSuggestion { get; set; } = string.Empty;
        [JsonPropertyName("dependency_step")]
        public int? DependencyStep { get; set; }
        [JsonPropertyName("quality_checkpoint")]
        public string QualityCheckpoint { get; set; } = string.Empty;
        [JsonPropertyName("food_safety_verification")]
        public string FoodSafetyVerification { get; set; } = string.Empty;
        [JsonPropertyName("provenance")]
        public string Provenance { get; set; } = "AI_GENERATED";
        [JsonPropertyName("locked")]
        public bool Locked { get; set; }
    }

    public class CulinarySubRecipeDto
    {
        [JsonPropertyName("sub_recipe_ref")]
        public string SubRecipeRef { get; set; } = string.Empty;
        [JsonPropertyName("sub_recipe_name")]
        public string SubRecipeName { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("batch_yield_qty")]
        public decimal BatchYieldQty { get; set; } = 1m;
        [JsonPropertyName("batch_yield_uom")]
        public string BatchYieldUom { get; set; } = "KG";
        [JsonPropertyName("required_qty_in_parent")]
        public decimal RequiredQtyInParent { get; set; } = 1m;
        [JsonPropertyName("required_uom_in_parent")]
        public string RequiredUomInParent { get; set; } = "KG";
        [JsonPropertyName("ingredients")]
        public List<CulinaryIngredientLineDto> Ingredients { get; set; } = new();
        [JsonPropertyName("instructions")]
        public List<CulinaryInstructionStepDto> Instructions { get; set; } = new();
        [JsonPropertyName("existing_recipe_reference")]
        public string? ExistingRecipeReference { get; set; }
    }

    public class CulinaryStructuredRecipePayloadDto
    {
        [JsonPropertyName("header")]
        public CulinaryRecipeHeaderDto Header { get; set; } = new();
        [JsonPropertyName("ingredients")]
        public List<CulinaryIngredientLineDto> Ingredients { get; set; } = new();
        [JsonPropertyName("instructions")]
        public List<CulinaryInstructionStepDto> Instructions { get; set; } = new();
        [JsonPropertyName("sub_recipes")]
        public List<CulinarySubRecipeDto> SubRecipes { get; set; } = new();
        [JsonPropertyName("assumptions")]
        public List<string> Assumptions { get; set; } = new();
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();
        [JsonPropertyName("chef_notes")]
        public string ChefNotes { get; set; } = string.Empty;
        [JsonPropertyName("visually_observed_characteristics")]
        public List<string> VisuallyObservedCharacteristics { get; set; } = new();
        [JsonPropertyName("inferred_uncertain_items")]
        public List<string> InferredUncertainItems { get; set; } = new();
        [JsonPropertyName("cost_validation_note")]
        public string CostValidationNote { get; set; } = "Cost validation pending material matching.";
    }

    public class CulinaryValidationWarningDto
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;
        [JsonPropertyName("severity")]
        public string Severity { get; set; } = "WARNING";
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    public class CulinaryConversationTurnDto
    {
        public string Role { get; set; } = "CHEF";
        public string Message { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? Model { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    }

    public class CulinaryGenerateRecipeRequestDto
    {
        public Guid? DraftId { get; set; }
        public string GenerationMode { get; set; } = "DISH_NAME";
        public string RecipeName { get; set; } = string.Empty;
        public string RecipeDescription { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string RecipeType { get; set; } = "STANDARD_RECIPE";
        public decimal Servings { get; set; } = 10m;
        public decimal ServingSize { get; set; } = 180m;
        public string ServingUom { get; set; } = "G";
        public string PreparationStyle { get; set; } = string.Empty;
        public string CookingMethod { get; set; } = string.Empty;
        public string SpiceLevel { get; set; } = string.Empty;
        public List<string> DietaryPreferences { get; set; } = new();
        public List<string> IngredientsToInclude { get; set; } = new();
        public List<string> IngredientsToExclude { get; set; } = new();
        public List<string> AllergensToAvoid { get; set; } = new();
        public List<string> PreferredIngredients { get; set; } = new();
        public decimal? TargetIngredientBudget { get; set; }
        public decimal? TargetCostPerServing { get; set; }
        public decimal? TargetSellingPrice { get; set; }
        public decimal? TargetFoodCostPercent { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? SourceRecipeId { get; set; }
        public string? SourceDocumentName { get; set; }
        public string? ExtractedDocumentText { get; set; }
        public string? ImageBase64 { get; set; }
        public string? ImageMimeType { get; set; }
        public string AdditionalInstructions { get; set; } = string.Empty;
        public string? PreferredProvider { get; set; }
        public string? GenerationReference { get; set; }
    }

    public class CulinaryRefineRequestDto
    {
        public string Instruction { get; set; } = string.Empty;
        public CulinaryStructuredRecipePayloadDto? CurrentPayload { get; set; }
        public string? PreferredProvider { get; set; }
    }

    public class CulinaryRecipeDiffItemDto
    {
        [JsonPropertyName("section")]
        public string Section { get; set; } = string.Empty;
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;
        [JsonPropertyName("before")]
        public string Before { get; set; } = string.Empty;
        [JsonPropertyName("after")]
        public string After { get; set; } = string.Empty;
    }

    public class CulinaryRefineResponseDto
    {
        public CulinaryStructuredRecipePayloadDto ProposedPayload { get; set; } = new();
        public List<CulinaryRecipeDiffItemDto> DiffSummary { get; set; } = new();
        public List<CulinaryValidationWarningDto> ValidationWarnings { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public bool UsedFallback { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public int LatencyMs { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
    }

    public class CulinaryRegenerateSectionRequestDto
    {
        public CulinaryStructuredRecipePayloadDto? CurrentPayload { get; set; }
        public List<string> Sections { get; set; } = new();
        public List<string> LockedFields { get; set; } = new();
        public List<string> LockedIngredientRefs { get; set; } = new();
        public string ChefGuidance { get; set; } = string.Empty;
        public string? PreferredProvider { get; set; }
    }

    public class CulinaryScaleV2RequestDto
    {
        public string RecipeName { get; set; } = string.Empty;
        public decimal BaseServings { get; set; } = 10m;
        public decimal TargetServings { get; set; } = 10m;
        public decimal BaseServingSize { get; set; } = 180m;
        public decimal TargetServingSize { get; set; } = 180m;
        public decimal PrepBufferPercent { get; set; }
        public bool UseNonlinearRules { get; set; } = true;
        public List<CulinaryScaleIngredientV2Dto> Ingredients { get; set; } = new();
    }

    public class CulinaryScaleIngredientV2Dto
    {
        [JsonPropertyName("temp_ref")]
        public string TempRef { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("uom")]
        public string Uom { get; set; } = "KG";
        [JsonPropertyName("base_qty")]
        public decimal BaseQty { get; set; }
        [JsonPropertyName("quantity_basis")]
        public string QuantityBasis { get; set; } = "RAW";
        [JsonPropertyName("yield_percent")]
        public decimal YieldPercent { get; set; } = 100m;
        [JsonPropertyName("scaling_method")]
        public string ScalingMethod { get; set; } = "LINEAR";
        [JsonPropertyName("chef_scaling_factor")]
        public decimal? ChefScalingFactor { get; set; }
    }

    public class CulinaryScaleV2ResultItemDto
    {
        [JsonPropertyName("temp_ref")]
        public string TempRef { get; set; } = string.Empty;
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;
        [JsonPropertyName("uom")]
        public string Uom { get; set; } = string.Empty;
        [JsonPropertyName("scaling_method")]
        public string ScalingMethod { get; set; } = "LINEAR";
        [JsonPropertyName("quantity_basis")]
        public string QuantityBasis { get; set; } = "RAW";
        [JsonPropertyName("base_qty")]
        public decimal BaseQty { get; set; }
        [JsonPropertyName("scaled_edible_qty")]
        public decimal ScaledEdibleQty { get; set; }
        [JsonPropertyName("scaled_raw_qty")]
        public decimal ScaledRawQty { get; set; }
        [JsonPropertyName("yield_percent")]
        public decimal YieldPercent { get; set; }
        [JsonPropertyName("requires_manual_review")]
        public bool RequiresManualReview { get; set; }
        [JsonPropertyName("note")]
        public string Note { get; set; } = string.Empty;
    }

    public class CulinaryScaleV2ResultDto
    {
        [JsonPropertyName("recipe_name")]
        public string RecipeName { get; set; } = string.Empty;
        [JsonPropertyName("base_servings")]
        public decimal BaseServings { get; set; }
        [JsonPropertyName("target_servings")]
        public decimal TargetServings { get; set; }
        [JsonPropertyName("linear_factor")]
        public decimal LinearFactor { get; set; }
        [JsonPropertyName("ingredients")]
        public List<CulinaryScaleV2ResultItemDto> Ingredients { get; set; } = new();
    }

    public class CulinarySaveDraftRequestDto
    {
        public int ExpectedRevisionNumber { get; set; }
        public string? Status { get; set; }
        public string ChangeType { get; set; } = "CHEF_EDIT";
        public string ChangeSummary { get; set; } = "Chef updated recipe draft.";
        public CulinaryStructuredRecipePayloadDto Payload { get; set; } = new();
        public List<CulinaryConversationTurnDto>? Conversation { get; set; }
        public List<CulinaryValidationWarningDto>? ValidationWarnings { get; set; }
    }

    public class CulinaryRecipeRevisionDto
    {
        public Guid Id { get; set; }
        public int RevisionNumber { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string ChangeSummary { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? Model { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; }
        public CulinaryStructuredRecipePayloadDto Payload { get; set; } = new();
    }

    public class CulinaryRecipeDraftSummaryDto
    {
        public Guid Id { get; set; }
        public string DraftReference { get; set; } = string.Empty;
        public string RecipeName { get; set; } = string.Empty;
        public string GenerationMode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string? KitchenName { get; set; }
        public decimal BaseServings { get; set; }
        public int IngredientCount { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string ValidationStatus { get; set; } = string.Empty;
        public int RevisionNumber { get; set; }
        public long TotalTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
    }

    public class CulinaryRecipeDraftDetailDto : CulinaryRecipeDraftSummaryDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? LocationId { get; set; }
        public Guid? OutletId { get; set; }
        public string? OutletName { get; set; }
        public Guid? SourceRecipeId { get; set; }
        public string? SourceDocumentName { get; set; }
        public string PromptVersion { get; set; } = string.Empty;
        public string GenerationReference { get; set; } = string.Empty;
        public long TotalInputTokens { get; set; }
        public long TotalOutputTokens { get; set; }
        public CulinaryStructuredRecipePayloadDto Payload { get; set; } = new();
        public List<CulinaryValidationWarningDto> ValidationWarnings { get; set; } = new();
        public List<CulinaryConversationTurnDto> Conversation { get; set; } = new();
        public List<CulinaryRecipeRevisionDto> Revisions { get; set; } = new();
    }

    public class CulinaryDocumentExtractDto
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; } = string.Empty;
        [JsonPropertyName("file_type")]
        public string FileType { get; set; } = string.Empty;
        [JsonPropertyName("extracted_text")]
        public string ExtractedText { get; set; } = string.Empty;
        [JsonPropertyName("line_count")]
        public int LineCount { get; set; }
        [JsonPropertyName("detected_title")]
        public string? DetectedTitle { get; set; }
        [JsonPropertyName("detected_servings")]
        public decimal? DetectedServings { get; set; }
        [JsonPropertyName("missing_quantities_detected")]
        public List<string> MissingQuantitiesDetected { get; set; } = new();
        [JsonPropertyName("unclear_units_detected")]
        public List<string> UnclearUnitsDetected { get; set; } = new();
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();
    }

    public class CulinaryDishImagePreparedDto
    {
        [JsonPropertyName("base64")]
        public string Base64 { get; set; } = string.Empty;
        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;
        [JsonPropertyName("width")]
        public int Width { get; set; }
        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}
