from __future__ import annotations

from datetime import datetime, timezone
from decimal import Decimal
from typing import Literal
from pydantic import BaseModel, Field

GenerationMode = Literal[
    "DISH_NAME",
    "INSTRUCTIONS",
    "EXISTING_RECIPE",
    "AVAILABLE_INGREDIENTS",
    "DOCUMENT",
    "DISH_IMAGE",
    "BUDGET_BASED",
]

RecipeTypeKind = Literal[
    "STANDARD_RECIPE",
    "BATCH_RECIPE",
    "SUB_RECIPE",
    "DIRECT_ITEM",
    "VARIANT_RECIPE",
]

ScalingMethod = Literal[
    "LINEAR",
    "FIXED_QUANTITY",
    "BATCH_BASED",
    "SPICE_NONLINEAR",
    "SEASONING_STEPPED",
    "LIQUID_EVAPORATION",
    "CHEF_FACTOR",
    "MANUAL_REVIEW",
]

QuantityBasis = Literal["RAW", "EDIBLE"]
ProvenanceKind = Literal["AI_GENERATED", "CHEF_EDITED", "LOCKED"]
ProviderKind = Literal["LOCAL", "GEMINI", "CLAUDE"]


class RecipeHeaderContract(BaseModel):
    proposed_name: str = Field(min_length=1, max_length=200)
    description: str = Field(default="", max_length=2000)
    cuisine: str = Field(default="International", max_length=120)
    category: str = Field(default="Main Course", max_length=120)
    recipe_type: RecipeTypeKind = "STANDARD_RECIPE"
    base_servings: Decimal = Field(default=Decimal("10"), gt=Decimal("0"))
    serving_size: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    serving_uom: str = Field(default="G", min_length=1, max_length=20)
    batch_size: Decimal | None = Field(default=None, gt=Decimal("0"))
    expected_yield_qty: Decimal | None = Field(default=None, gt=Decimal("0"))
    expected_yield_uom: str | None = Field(default=None, max_length=20)
    prep_duration_minutes: int = Field(default=20, ge=0, le=2880)
    cook_duration_minutes: int = Field(default=30, ge=0, le=2880)
    total_duration_minutes: int = Field(default=50, ge=0, le=5760)
    cooking_method: str = Field(default="", max_length=160)
    difficulty: Literal["EASY", "MEDIUM", "ADVANCED"] = "MEDIUM"
    dietary_tags: list[str] = Field(default_factory=list)
    potential_allergens: list[str] = Field(default_factory=list)
    cross_contact_warnings: list[str] = Field(default_factory=list)
    storage_guidance: str = Field(default="", max_length=500)
    shelf_life_suggestion: str = Field(default="Requires chef & food safety verification", max_length=300)
    chef_verification_required: bool = True
    nutrition_status: str = "UNAVAILABLE_OR_UNVERIFIED"


class IngredientLineContract(BaseModel):
    temp_ref: str = Field(min_length=1, max_length=40)
    description: str = Field(min_length=1, max_length=250)
    normalized_description: str = Field(default="", max_length=250)
    category: str = Field(default="OTHER", max_length=80)
    ingredient_form: str = Field(default="", max_length=80)
    brand_preference: str | None = Field(default=None, max_length=100)
    quantity: Decimal = Field(ge=Decimal("0"))
    recipe_uom: str = Field(min_length=1, max_length=30)
    normalized_uom: str = Field(default="", max_length=30)
    uom_resolved: bool = True
    quantity_basis: QuantityBasis = "RAW"
    raw_quantity: Decimal | None = Field(default=None, ge=Decimal("0"))
    edible_quantity: Decimal | None = Field(default=None, ge=Decimal("0"))
    yield_percent: Decimal = Field(default=Decimal("100"), gt=Decimal("0"), le=Decimal("100"))
    yield_source: Literal["AI_SUGGESTED", "CHEF_CONFIRMED", "HISTORICAL"] = "AI_SUGGESTED"
    prep_loss_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("95"))
    cook_loss_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("95"))
    scaling_method: ScalingMethod = "LINEAR"
    chef_scaling_factor: Decimal | None = Field(default=None, gt=Decimal("0"))
    optional_indicator: bool = False
    preparation_notes: str = Field(default="", max_length=400)
    suggested_alternatives: list[str] = Field(default_factory=list)
    material_mapping_status: Literal["PENDING_MATERIAL_MAPPING"] = "PENDING_MATERIAL_MAPPING"
    material_reference: str | None = None
    provenance: ProvenanceKind = "AI_GENERATED"
    locked: bool = False


class InstructionStepContract(BaseModel):
    step_number: int = Field(ge=1)
    step_title: str = Field(default="", max_length=160)
    instruction: str = Field(min_length=1, max_length=2000)
    estimated_duration_minutes: int = Field(default=5, ge=0, le=1440)
    equipment_suggestion: str = Field(default="", max_length=200)
    dependency_step: int | None = Field(default=None, ge=1)
    quality_checkpoint: str = Field(default="", max_length=400)
    food_safety_verification: str = Field(default="", max_length=400)
    provenance: ProvenanceKind = "AI_GENERATED"
    locked: bool = False


class SubRecipeContract(BaseModel):
    sub_recipe_ref: str = Field(min_length=1, max_length=40)
    sub_recipe_name: str = Field(min_length=1, max_length=200)
    description: str = Field(default="", max_length=1000)
    batch_yield_qty: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    batch_yield_uom: str = Field(default="KG", min_length=1, max_length=20)
    required_qty_in_parent: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    required_uom_in_parent: str = Field(default="KG", min_length=1, max_length=20)
    ingredients: list[IngredientLineContract] = Field(default_factory=list)
    instructions: list[InstructionStepContract] = Field(default_factory=list)
    existing_recipe_reference: str | None = None


class AiMetadataContract(BaseModel):
    provider: ProviderKind
    model: str
    prompt_version: str
    generation_timestamp: str = Field(
        default_factory=lambda: datetime.now(timezone.utc).isoformat()
    )
    generation_reference: str
    assumptions: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    validation_status: Literal["VALID", "VALIDATION_WARNINGS", "REPAIRED"] = "VALID"
    input_tokens: int = 0
    output_tokens: int = 0
    total_tokens: int = 0
    estimated_cost_usd: Decimal = Decimal("0")
    latency_ms: int = 0
    used_fallback: bool = False
    validation_retries: int = 0


class ValidationWarning(BaseModel):
    code: str
    severity: Literal["INFO", "WARNING", "ERROR"] = "WARNING"
    field: str = ""
    message: str


class StructuredRecipePayload(BaseModel):
    header: RecipeHeaderContract
    ingredients: list[IngredientLineContract] = Field(min_length=1)
    instructions: list[InstructionStepContract] = Field(min_length=1)
    sub_recipes: list[SubRecipeContract] = Field(default_factory=list)
    assumptions: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    chef_notes: str = ""
    visually_observed_characteristics: list[str] = Field(default_factory=list)
    inferred_uncertain_items: list[str] = Field(default_factory=list)
    cost_validation_note: str = "Cost validation pending material matching."


class GeneratedRecipeResponse(BaseModel):
    payload: StructuredRecipePayload
    ai_metadata: AiMetadataContract
    validation_warnings: list[ValidationWarning] = Field(default_factory=list)


class RecipeGenerationInput(BaseModel):
    generation_mode: GenerationMode = "DISH_NAME"
    recipe_name: str = Field(default="", max_length=200)
    recipe_description: str = Field(default="", max_length=1000)
    cuisine: str = Field(default="", max_length=120)
    category: str = Field(default="", max_length=120)
    recipe_type: RecipeTypeKind = "STANDARD_RECIPE"
    servings: Decimal = Field(default=Decimal("10"), gt=Decimal("0"))
    serving_size: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    serving_uom: str = Field(default="G", max_length=20)
    preparation_style: str = Field(default="", max_length=120)
    cooking_method: str = Field(default="", max_length=120)
    spice_level: str = Field(default="", max_length=60)
    dietary_preferences: list[str] = Field(default_factory=list)
    ingredients_to_include: list[str] = Field(default_factory=list)
    ingredients_to_exclude: list[str] = Field(default_factory=list)
    allergens_to_avoid: list[str] = Field(default_factory=list)
    preferred_ingredients: list[str] = Field(default_factory=list)
    target_ingredient_budget: Decimal | None = Field(default=None, ge=Decimal("0"))
    target_cost_per_serving: Decimal | None = Field(default=None, ge=Decimal("0"))
    target_selling_price: Decimal | None = Field(default=None, ge=Decimal("0"))
    target_food_cost_percent: Decimal | None = Field(default=None, ge=Decimal("0"), le=Decimal("100"))
    currency: str = Field(default="AED", max_length=10)
    property_name: str | None = Field(default=None, max_length=160)
    kitchen_name: str | None = Field(default=None, max_length=160)
    outlet_name: str | None = Field(default=None, max_length=160)
    additional_instructions: str = Field(default="", max_length=3000)
    existing_recipe_context: dict | None = None
    available_ingredients_context: list[dict] = Field(default_factory=list)
    extracted_document_text: str | None = Field(default=None, max_length=12000)
    image_base64: str | None = None
    image_mime_type: str | None = None
    preferred_provider: ProviderKind | None = None


class ProviderRuntimeConfig(BaseModel):
    tenant_id: str
    operating_mode: Literal["LOCAL", "GEMINI", "CLAUDE", "HYBRID"] = "LOCAL"
    tenant_external_ai_allowed: bool = False
    local_enabled: bool = True
    local_endpoint: str | None = None
    local_model: str = "qwen2.5:7b-instruct"
    gemini_enabled: bool = False
    gemini_model: str = "gemini-2.5-flash"
    gemini_api_key: str | None = None
    claude_enabled: bool = False
    claude_model: str = "claude-sonnet-4-20250514"
    claude_api_key: str | None = None
    timeout_seconds: float = Field(default=45.0, gt=0.0, le=120.0)
    max_output_tokens: int = Field(default=3000, ge=256, le=16384)
    temperature: float = Field(default=0.2, ge=0.0, le=1.0)
    max_retries: int = Field(default=1, ge=0, le=3)
    fallback_policy: Literal["NONE", "LOCAL_ONLY", "CONFIGURED_ORDER"] = "NONE"
    task_routing: dict[str, str] = Field(default_factory=dict)


class GenerateRecipeHttpRequest(BaseModel):
    input: RecipeGenerationInput
    provider_config: ProviderRuntimeConfig


class RefineRecipeHttpRequest(BaseModel):
    current_payload: StructuredRecipePayload
    instruction: str = Field(min_length=1, max_length=2000)
    conversation_history: list[dict[str, str]] = Field(default_factory=list)
    available_ingredients_context: list[dict] = Field(default_factory=list)
    provider_config: ProviderRuntimeConfig
    preferred_provider: ProviderKind | None = None


class RecipeDiffItem(BaseModel):
    section: str
    field: str
    before: str
    after: str


class RefineRecipeHttpResponse(BaseModel):
    proposed_payload: StructuredRecipePayload
    ai_metadata: AiMetadataContract
    diff_summary: list[RecipeDiffItem] = Field(default_factory=list)
    validation_warnings: list[ValidationWarning] = Field(default_factory=list)
    summary_message: str


RegenerableSection = Literal[
    "description",
    "ingredients",
    "instructions",
    "cooking_method",
    "sub_recipes",
    "plating",
    "alternatives",
    "dietary",
]


class RegenerateSectionHttpRequest(BaseModel):
    current_payload: StructuredRecipePayload
    sections: list[RegenerableSection] = Field(min_length=1)
    locked_fields: list[str] = Field(default_factory=list)
    locked_ingredient_refs: list[str] = Field(default_factory=list)
    chef_guidance: str = Field(default="", max_length=1500)
    provider_config: ProviderRuntimeConfig
    preferred_provider: ProviderKind | None = None


class ExtractedDocumentResult(BaseModel):
    filename: str
    file_type: str
    extracted_text: str
    line_count: int
    detected_title: str | None = None
    detected_servings: Decimal | None = None
    missing_quantities_detected: list[str] = Field(default_factory=list)
    unclear_units_detected: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    structured_draft: GeneratedRecipeResponse | None = None
