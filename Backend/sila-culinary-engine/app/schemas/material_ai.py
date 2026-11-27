from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Any, Literal
from pydantic import BaseModel, Field
from app.schemas.recipe_ai import ProviderRuntimeConfig


class MaterialUomFactor(BaseModel):
    from_uom: str
    to_uom: str
    factor: Decimal = Field(gt=0)
    source: str = "MATERIAL_MASTER"


class MaterialStockSummary(BaseModel):
    property_id: str | None = None
    plant_code: str | None = None
    storage_location: str | None = None
    on_hand_qty: Decimal | None = None
    reserved_qty: Decimal | None = None
    available_qty: Decimal | None = None
    blocked_qty: Decimal | None = None
    inventory_uom: str | None = None
    batch_number: str | None = None
    expiry_date: str | None = None
    stock_status: Literal["KNOWN", "UNKNOWN", "UNAUTHORIZED"] = "UNKNOWN"
    timestamp_utc: datetime | None = None


class MaterialPriceSummary(BaseModel):
    unit_price: Decimal | None = None
    currency: str | None = None
    price_uom: str | None = None
    price_source: str | None = None
    supplier_name: str | None = None
    effective_date: str | None = None
    price_status: Literal["VERIFIED", "MISSING", "UNAUTHORIZED"] = "MISSING"
    timestamp_utc: datetime | None = None


class MaterialMasterRecord(BaseModel):
    material_id: str
    material_code: str = Field(min_length=1)
    description: str = Field(min_length=1)
    long_description: str | None = None
    material_type: str | None = None
    material_group: str | None = None
    category: str | None = None
    product_hierarchy: str | None = None
    brand: str | None = None
    specification: str | None = None
    base_uom: str = "EA"
    order_uom: str | None = None
    alternate_uom: str | None = None
    uom_conversions: list[MaterialUomFactor] = Field(default_factory=list)
    pack_size: Decimal | None = None
    pack_uom: str | None = None
    min_order_qty: Decimal | None = None
    barcode: str | None = None
    aliases: list[str] = Field(default_factory=list)
    image_url: str | None = None
    is_active: bool = True
    plant_eligible: bool = True
    purchasing_eligible: bool = True
    batch_managed: bool = False
    expiry_managed: bool = False
    storage_requirement: str | None = None
    allergen_info: list[str] = Field(default_factory=list)
    dietary_info: list[str] = Field(default_factory=list)
    density_g_per_ml: Decimal | None = None
    verified_recipe_usage_count: int = 0
    revision_stamp: str | None = None
    updated_at: datetime | None = None
    stock_summary: MaterialStockSummary | None = None
    price_summary: MaterialPriceSummary | None = None


class MaterialAliasRecord(BaseModel):
    alias_id: str | None = None
    alias_text: str = Field(min_length=1)
    material_id: str
    material_code: str
    property_id: str | None = None
    language: str = "en"
    source: str = "CHEF_APPROVED"
    approval_status: Literal["PENDING", "APPROVED", "REJECTED", "INACTIVE"] = "APPROVED"
    revision: int = 1


class IngredientQueryLine(BaseModel):
    temp_ref: str = Field(min_length=1)
    description: str = Field(min_length=1)
    normalized_description: str | None = None
    category: str | None = None
    ingredient_form: str | None = None
    brand_preference: str | None = None
    quantity: Decimal = Field(gt=0)
    recipe_uom: str = Field(min_length=1)
    required_allergen_free: list[str] = Field(default_factory=list)
    required_dietary: list[str] = Field(default_factory=list)
    property_id: str | None = None
    plant_code: str | None = None
    location_id: str | None = None
    density_g_per_ml: Decimal | None = None
    explicit_material_code: str | None = None
    explicit_barcode: str | None = None


class MaterialRankingWeights(BaseModel):
    exact_identity: float = 0.45
    alias_identity: float = 0.35
    fuzzy: float = 0.25
    semantic: float = 0.30
    rerank: float = 0.25
    attribute_compatibility: float = 0.15
    uom_compatibility: float = 0.08
    verified_usage: float = 0.04


class MaterialMatchingConfig(BaseModel):
    exact_matching_enabled: bool = True
    alias_matching_enabled: bool = True
    fuzzy_matching_enabled: bool = True
    semantic_matching_enabled: bool = True
    reranking_enabled: bool = True
    llm_assisted_enabled: bool = False
    candidate_retrieval_limit: int = Field(default=25, ge=5, le=100)
    candidate_display_limit: int = Field(default=5, ge=1, le=20)
    fuzzy_threshold: float = Field(default=70.0, ge=0.0, le=100.0)
    semantic_threshold: float = Field(default=0.66, ge=0.0, le=1.0)
    auto_exact_threshold: float = Field(default=0.95, ge=0.5, le=1.0)
    ambiguity_margin: float = Field(default=0.05, ge=0.0, le=0.5)
    mandatory_chef_review: bool = True
    embedding_model: str = "BAAI/bge-small-en-v1.5"
    reranking_model: str = "Xenova/ms-marco-MiniLM-L-6-v2"
    execution_mode: Literal["CPU", "GPU"] = "CPU"
    batch_size: int = Field(default=32, ge=1, le=256)
    max_ingredients_per_bulk: int = Field(default=150, ge=1, le=500)
    weights: MaterialRankingWeights = Field(default_factory=MaterialRankingWeights)


class ExtractedAttributes(BaseModel):
    original_description: str
    normalized_description: str
    canonical_tokens: list[str] = Field(default_factory=list)
    ingredient_identity: str
    product_category: str | None = None
    ingredient_form: str | None = None
    species: str | None = None
    meat_cut: str | None = None
    brand: str | None = None
    fat_percentage: str | None = None
    concentration: str | None = None
    fresh_or_frozen: Literal["FRESH", "FROZEN", "SMOKED", "DRIED", "CANNED", "UNKNOWN"] = "UNKNOWN"
    raw_or_cooked: Literal["RAW", "COOKED", "UNKNOWN"] = "UNKNOWN"
    whole_or_processed: str = "UNKNOWN"
    salted_or_unsalted: Literal["SALTED", "UNSALTED", "UNKNOWN"] = "UNKNOWN"
    pack_size_expr: str | None = None
    net_weight_expr: str | None = None
    volume_expr: str | None = None
    food_grade: str | None = None
    dietary_flags: list[str] = Field(default_factory=list)
    allergen_flags: list[str] = Field(default_factory=list)
    quality_specification: list[str] = Field(default_factory=list)
    specification_tokens: list[str] = Field(default_factory=list)


class UomValidationResult(BaseModel):
    compatible: bool
    status: Literal["COMPATIBLE", "UOM_CONFLICT", "MISSING_CONVERSION"]
    recipe_qty: Decimal
    recipe_uom: str
    base_uom: str
    converted_base_qty: Decimal | None = None
    conversion_factor: Decimal | None = None
    conversion_source: str | None = None
    purchasing_uom: str | None = None
    purchasing_qty: Decimal | None = None
    pack_size: Decimal | None = None
    pack_uom: str | None = None
    indicative_packs_needed: Decimal | None = None
    excess_after_pack_rounding: Decimal | None = None
    warning: str | None = None


class CandidateScoreBreakdown(BaseModel):
    exact: float = 0.0
    alias: float = 0.0
    fuzzy_ratio: float = 0.0
    fuzzy_partial: float = 0.0
    fuzzy_token_sort: float = 0.0
    fuzzy_token_set: float = 0.0
    fuzzy_wratio: float = 0.0
    semantic: float = 0.0
    lexical_tfidf: float = 0.0
    rerank_raw: float = 0.0
    rerank_norm: float = 0.0
    attribute_score: float = 0.0
    composite_score: float = 0.0


class MatchCandidateResult(BaseModel):
    material_id: str
    material_code: str
    description: str
    long_description: str | None = None
    material_group: str | None = None
    category: str | None = None
    brand: str | None = None
    specification: str | None = None
    base_uom: str
    order_uom: str | None = None
    pack_size: Decimal | None = None
    image_url: str | None = None
    match_method: str
    scores: CandidateScoreBreakdown
    specification_compatible: bool = True
    specification_conflicts: list[str] = Field(default_factory=list)
    uom_validation: UomValidationResult
    eligible: bool = True
    is_active: bool = True
    plant_eligible: bool = True
    purchasing_eligible: bool = True
    ineligibility_reasons: list[str] = Field(default_factory=list)
    evidence: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    stock_summary: MaterialStockSummary | None = None
    price_summary: MaterialPriceSummary | None = None


class ProposedMaterialRequestDraft(BaseModel):
    request_type: Literal[
        "NEW_MATERIAL",
        "MATERIAL_EXTENSION",
        "UOM_MAINTENANCE",
        "MATERIAL_REACTIVATION",
        "SPECIFICATION_CORRECTION",
        "MANUAL_MASTER_DATA_REVIEW",
    ]
    proposed_description: str
    long_description: str | None = None
    ingredient_category: str | None = None
    proposed_material_group: str | None = None
    proposed_base_uom: str
    proposed_purchasing_uom: str | None = None
    proposed_pack_size: str | None = None
    brand_preference: str | None = None
    product_specification: str | None = None
    storage_requirement: str | None = None
    potential_allergens: list[str] = Field(default_factory=list)
    ingredient_temp_ref: str
    required_quantity: Decimal
    property_id: str | None = None
    plant_code: str | None = None
    reason_for_request: str
    existing_material_code: str | None = None
    duplicate_check_summary: str
    ai_suggested_fields: list[str] = Field(default_factory=list)
    verified_fields: list[str] = Field(default_factory=list)


class LlmMatchRecommendation(BaseModel):
    invoked: bool = False
    provider: str | None = None
    model: str | None = None
    recommended_material_code: str | None = None
    validated_against_candidates: bool = False
    rejected_unknown_code: str | None = None
    reasoning: str | None = None
    input_tokens: int = 0
    output_tokens: int = 0
    estimated_cost_usd: Decimal = Decimal("0")
    error: str | None = None


class IngredientMatchResponse(BaseModel):
    temp_ref: str
    original_description: str
    normalized_description: str
    extracted_attributes: ExtractedAttributes
    status: Literal[
        "EXACT_MATCH",
        "VERIFIED_ALIAS_MATCH",
        "SUGGESTED_MATCH",
        "MULTIPLE_CANDIDATES",
        "SPECIFICATION_CONFLICT",
        "UOM_CONFLICT",
        "MATERIAL_INELIGIBLE",
        "UNMATCHED",
    ]
    match_method: str
    confidence_score: float = 0.0
    score_gap_to_second: float | None = None
    review_required: bool = True
    auto_bulk_acceptable: bool = False
    recommended_candidate: MatchCandidateResult | None = None
    candidates: list[MatchCandidateResult] = Field(default_factory=list)
    missing_material_classification: Literal[
        "NONE",
        "NO_MATERIAL_FOUND",
        "SPECIFICATION_INCOMPATIBLE",
        "MATERIAL_INACTIVE",
        "NOT_EXTENDED_TO_PLANT",
        "NOT_AUTHORIZED_FOR_PURCHASING",
        "UOM_CONVERSION_MISSING",
        "INCOMPLETE_MASTER_DATA",
        "REQUIRES_CHEF_REVIEW",
    ] = "NONE"
    duplicate_check_candidates: list[MatchCandidateResult] = Field(default_factory=list)
    proposed_material_request_draft: ProposedMaterialRequestDraft | None = None
    llm_recommendation: LlmMatchRecommendation | None = None
    latency_ms: float = 0.0
    error: str | None = None


class IndexBuildRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    materials: list[MaterialMasterRecord] = Field(default_factory=list)
    aliases: list[MaterialAliasRecord] = Field(default_factory=list)
    config: MaterialMatchingConfig = Field(default_factory=MaterialMatchingConfig)
    force_rebuild: bool = False


class IndexSyncRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    upserted_materials: list[MaterialMasterRecord] = Field(default_factory=list)
    deactivated_material_ids: list[str] = Field(default_factory=list)
    aliases: list[MaterialAliasRecord] | None = None
    config: MaterialMatchingConfig = Field(default_factory=MaterialMatchingConfig)


class IndexStatusResponse(BaseModel):
    tenant_id: str
    status: Literal["EMPTY", "BUILDING", "READY", "FAILED"]
    indexed_material_count: int = 0
    active_material_count: int = 0
    alias_count: int = 0
    embedding_model: str
    reranking_model: str
    semantic_backend: str
    faiss_enabled: bool = False
    last_build_utc: datetime | None = None
    last_sync_utc: datetime | None = None
    build_duration_ms: float = 0.0
    errors: list[str] = Field(default_factory=list)


class MaterialMatchRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    ingredient: IngredientQueryLine
    materials: list[MaterialMasterRecord] | None = None
    aliases: list[MaterialAliasRecord] | None = None
    config: MaterialMatchingConfig = Field(default_factory=MaterialMatchingConfig)
    provider_config: ProviderRuntimeConfig | None = None


class BulkMaterialMatchRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    ingredients: list[IngredientQueryLine] = Field(default_factory=list)
    materials: list[MaterialMasterRecord] | None = None
    aliases: list[MaterialAliasRecord] | None = None
    config: MaterialMatchingConfig = Field(default_factory=MaterialMatchingConfig)
    provider_config: ProviderRuntimeConfig | None = None


class BulkMaterialMatchResponse(BaseModel):
    tenant_id: str
    total_ingredients: int
    processed_count: int
    failed_count: int
    results: list[IngredientMatchResponse]
    index_status: IndexStatusResponse
    total_latency_ms: float


class MaterialSearchRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    query: str = Field(min_length=1)
    property_id: str | None = None
    include_inactive: bool = False
    limit: int = Field(default=20, ge=1, le=50)
    materials: list[MaterialMasterRecord] | None = None
    aliases: list[MaterialAliasRecord] | None = None
    config: MaterialMatchingConfig = Field(default_factory=MaterialMatchingConfig)


class SubstitutionSuggestRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    ingredient: IngredientQueryLine
    current_material: MaterialMasterRecord | None = None
    materials: list[MaterialMasterRecord] | None = None
    aliases: list[MaterialAliasRecord] | None = None
    dietary_constraints: list[str] = Field(default_factory=list)
    allergen_exclusions: list[str] = Field(default_factory=list)
    config: MaterialMatchingConfig = Field(default_factory=MaterialMatchingConfig)
    provider_config: ProviderRuntimeConfig | None = None


class SubstitutionComparisonItem(BaseModel):
    substitution_kind: Literal["ALTERNATIVE_MATERIAL_MATCH", "CULINARY_SUBSTITUTION"]
    original_ingredient: str
    original_material_code: str | None = None
    proposed_ingredient: str
    proposed_material_id: str | None = None
    proposed_material_code: str | None = None
    proposed_material_description: str | None = None
    suggested_replacement_ratio: Decimal | None = None
    ratio_requires_chef_input: bool = True
    converted_quantity: Decimal | None = None
    target_uom: str | None = None
    uom_compatible: bool = False
    stock_available: Decimal | None = None
    stock_status: str = "UNKNOWN"
    estimated_cost_impact: Decimal | None = None
    currency: str | None = None
    taste_impact: str
    texture_impact: str
    preparation_changes: str
    dietary_impact: str
    allergen_impact: str
    chef_verification_requirements: list[str] = Field(default_factory=list)
    confidence_note: str


class SubstitutionSuggestResponse(BaseModel):
    temp_ref: str
    original_ingredient: str
    suggestions: list[SubstitutionComparisonItem] = Field(default_factory=list)
    llm_used: bool = False
    provider: str | None = None
    model: str | None = None
    warnings: list[str] = Field(default_factory=list)


class EvaluationMetricsResponse(BaseModel):
    dataset_name: str
    dataset_size: int
    top1_accuracy: float
    top3_accuracy: float
    top5_accuracy: float
    exact_match_precision: float
    false_positive_rate: float
    missing_material_recall: float
    missing_material_precision: float
    spec_conflict_detection_rate: float
    uom_conflict_detection_rate: float
    p50_latency_ms: float
    p95_latency_ms: float
    index_build_ms: float
    process_rss_mb: float
    semantic_backend: str
    reranker_backend: str
    limitations: list[str] = Field(default_factory=list)
    case_results: list[dict[str, Any]] = Field(default_factory=list)
