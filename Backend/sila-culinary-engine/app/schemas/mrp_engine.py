from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Any, Literal
from pydantic import BaseModel, Field

from app.providers.router import ProviderPolicy


ScalingMethod = Literal[
    "LINEAR",
    "FIXED_QUANTITY",
    "BATCH_BASED",
    "MIN_MAX",
    "NONLINEAR",
    "SPICE_NONLINEAR",
    "SEASONING_STEPPED",
    "LIQUID_EVAPORATION",
    "CHEF_FACTOR",
    "PORTION_DEPENDENT",
    "YIELD_DEPENDENT",
]
QuantityBasis = Literal["RAW", "EDIBLE", "FINISHED"]
BufferPolicy = Literal["PRECEDENCE", "ADDITIVE_EXPLICIT"]
PreparedStockMode = Literal["GROSS_ONLY", "ADJUSTED_SCENARIO"]
MrpSeverity = Literal["BLOCKING", "WARNING", "INFO"]


class MrpUomConversionRule(BaseModel):
    from_uom: str
    to_uom: str
    factor: Decimal = Field(gt=Decimal("0"))


class MrpStockSnapshotInput(BaseModel):
    eligible_qty: Decimal | None = Field(default=None, ge=Decimal("0"))
    on_hand_qty: Decimal | None = Field(default=None, ge=Decimal("0"))
    blocked_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    quality_inspection_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    expired_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    near_expiry_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    damaged_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    incoming_po_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    prepared_stock_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    storage_location_code: str | None = None
    batch_number: str | None = None
    earliest_expiry_utc: datetime | None = None
    inventory_timestamp_utc: datetime | None = None
    is_stale: bool = False
    exclusion_reasons: list[str] = Field(default_factory=list)


class MrpIngredientInput(BaseModel):
    line_ref: str
    item_code: str = ""
    item_name: str
    material_id: str | None = None
    material_code: str | None = None
    material_description: str | None = None
    material_category: str = "Uncategorized"
    material_active: bool = True
    extended_to_plant: bool = True
    is_sub_recipe: bool = False
    sub_recipe_id: str | None = None
    sub_recipe_version: int | None = None
    quantity: Decimal = Field(ge=Decimal("0"))
    uom: str
    base_uom: str | None = None
    quantity_basis: QuantityBasis = "RAW"
    scaling_method: ScalingMethod = "LINEAR"
    chef_scaling_factor: Decimal | None = Field(default=None, gt=Decimal("0"))
    min_quantity: Decimal | None = Field(default=None, ge=Decimal("0"))
    max_quantity: Decimal | None = Field(default=None, ge=Decimal("0"))
    batch_step_size: Decimal | None = Field(default=None, gt=Decimal("0"))
    usable_yield_percent: Decimal = Field(default=Decimal("100"), ge=Decimal("0"), le=Decimal("100"))
    prep_yield_percent: Decimal = Field(default=Decimal("100"), ge=Decimal("0"), le=Decimal("100"))
    cooking_yield_percent: Decimal = Field(default=Decimal("100"), ge=Decimal("0"), le=Decimal("100"))
    ingredient_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("200"))
    prep_lead_hours: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    chef_locked: bool = False
    unit_cost: Decimal | None = Field(default=None, ge=Decimal("0"))
    cost_status: str = "VERIFIED"  # VERIFIED | ESTIMATED | COST_UNAVAILABLE
    currency: str = "AED"
    uom_conversions: list[MrpUomConversionRule] = Field(default_factory=list)
    stock: MrpStockSnapshotInput | None = None


class MrpRecipeNodeInput(BaseModel):
    recipe_id: str
    recipe_code: str = ""
    recipe_name: str
    recipe_version: int = 1
    recipe_type: str = "STANDARD_RECIPE"
    is_published: bool = True
    base_servings: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    batch_yield: Decimal | None = Field(default=None, gt=Decimal("0"))
    batch_yield_uom: str = "KG"
    min_batch_size: Decimal | None = Field(default=None, gt=Decimal("0"))
    max_batch_size: Decimal | None = Field(default=None, gt=Decimal("0"))
    allow_partial_batches: bool = True
    prep_time_minutes: int = 0
    cook_time_minutes: int = 0
    marination_lead_hours: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    recipe_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("200"))
    trim_loss_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("99.9"))
    cooking_loss_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("99.9"))
    waste_factor_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("99.9"))
    prepared_stock_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    chef_locked: bool = False
    ingredients: list[MrpIngredientInput] = Field(default_factory=list)


class MrpEventSessionInput(BaseModel):
    session_id: str
    session_name: str
    session_date_utc: datetime
    kitchen_location_id: str | None = None
    kitchen_name: str = "Main Banquet Kitchen"
    service_style: str = "BUFFET"
    planned_guests: int | None = None
    session_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("200"))


class MrpEventMenuLineInput(BaseModel):
    menu_line_id: str
    session_id: str | None = None
    course_name: str = "Main Courses"
    dish_name: str
    recipe_id: str | None = None
    recipe_code: str | None = None
    recipe_version: int | None = None
    service_style: str = "BUFFET"
    uptake_percent: Decimal = Field(default=Decimal("100"), ge=Decimal("0"), le=Decimal("300"))
    serving_frequency: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    buffet_replenishment_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("200"))
    manual_portion_override: Decimal | None = Field(default=None, ge=Decimal("0"))
    standard_serving_size_grams: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    event_serving_size_grams: Decimal | None = Field(default=None, gt=Decimal("0"))
    portion_uom: str = "G"
    is_beverage: bool = False
    serving_volume_ml: Decimal | None = Field(default=None, gt=Decimal("0"))
    dietary_verified: bool = True


class MrpEventInput(BaseModel):
    event_id: str
    event_code: str
    event_name: str
    event_revision: int = 1
    approval_status: str = "APPROVED"
    property_id: str | None = None
    property_name: str = "Main Property"
    company_code: str | None = None
    plant_code: str | None = None
    venue_name: str | None = None
    kitchen_location_id: str | None = None
    kitchen_name: str = "Main Banquet Kitchen"
    storage_location_code: str | None = None
    event_date_utc: datetime
    prep_deadline_utc: datetime | None = None
    timezone_id: str = "Asia/Dubai"
    guaranteed_guests: int = Field(ge=0)
    expected_guests: int = Field(default=0, ge=0)
    additional_allowance_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("200"))
    baseline_includes_allowance: bool = False
    adults_count: int = 0
    children_count: int = 0
    vip_count: int = 0
    staff_meals_count: int = 0
    event_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("200"))
    currency: str = "AED"
    sessions: list[MrpEventSessionInput] = Field(default_factory=list)
    menu_lines: list[MrpEventMenuLineInput] = Field(default_factory=list)


class MrpExplodeRequest(BaseModel):
    tenant_id: str
    mrp_run_id: str = "MRP-PREVIEW"
    events: list[MrpEventInput]
    recipes: list[MrpRecipeNodeInput]
    buffer_policy: BufferPolicy = "PRECEDENCE"
    prepared_stock_mode: PreparedStockMode = "ADJUSTED_SCENARIO"
    max_depth: int = Field(default=10, ge=1, le=25)
    max_nodes: int = Field(default=500, ge=10, le=5000)


class MrpExceptionItem(BaseModel):
    exception_code: str
    exception_type: str
    severity: MrpSeverity
    event_id: str | None = None
    event_code: str | None = None
    recipe_id: str | None = None
    recipe_code: str | None = None
    material_code: str | None = None
    material_name: str | None = None
    calculation_impact: str
    recommended_action: str
    cycle_path: list[str] = Field(default_factory=list)


class MrpExplosionTreeNode(BaseModel):
    node_id: str
    node_type: Literal["EVENT", "SESSION", "COURSE", "RECIPE", "SUB_RECIPE", "MATERIAL"]
    reference_code: str
    description: str
    recipe_id: str | None = None
    recipe_version: int | None = None
    material_code: str | None = None
    required_qty: Decimal
    uom: str
    yield_percent: Decimal = Decimal("100")
    scaling_factor: Decimal = Decimal("1")
    dependency_level: int = 0
    validation_status: str = "VALID"
    children: list[MrpExplosionTreeNode] = Field(default_factory=list)


class MrpConsolidatedMaterialLine(BaseModel):
    consolidation_key: str
    material_id: str | None = None
    material_code: str | None = None
    material_description: str
    material_category: str = "Uncategorized"
    is_resolved_material: bool = True
    plant_code: str | None = None
    kitchen_location_id: str | None = None
    kitchen_name: str = "Main Banquet Kitchen"
    storage_location_code: str | None = None
    required_date_utc: datetime
    required_date_bucket: str
    base_uom: str
    edible_qty: Decimal
    raw_qty: Decimal
    gross_qty: Decimal
    prepared_stock_qty: Decimal
    indicative_new_production_qty: Decimal
    eligible_stock_qty: Decimal | None = None
    incoming_po_qty: Decimal = Decimal("0")
    indicative_current_shortage_qty: Decimal | None = None
    indicative_remaining_shortage_qty: Decimal | None = None
    unit_cost: Decimal | None = None
    currency: str = "AED"
    cost_status: str = "COST_UNAVAILABLE"
    requirement_value: Decimal | None = None
    indicative_shortage_value: Decimal | None = None
    validation_status: str = "REQUIREMENT_CALCULATED"
    is_blocking: bool = False
    contributing_recipes: list[str] = Field(default_factory=list)
    contributing_events: list[str] = Field(default_factory=list)
    contributing_sessions: list[str] = Field(default_factory=list)
    sub_recipe_paths: list[str] = Field(default_factory=list)
    exclusion_reasons: list[str] = Field(default_factory=list)


class MrpBatchSummary(BaseModel):
    recipe_id: str
    recipe_code: str
    recipe_name: str
    recipe_version: int
    required_qty: Decimal
    batch_yield: Decimal
    batch_yield_uom: str
    allow_partial_batches: bool
    required_batches: Decimal
    planned_output_qty: Decimal
    excess_output_qty: Decimal
    prepared_stock_qty: Decimal = Decimal("0")
    indicative_new_production_qty: Decimal = Decimal("0")


class MrpTimeBucketSummary(BaseModel):
    bucket_date: str
    kitchen_name: str
    material_count: int
    total_requirement_value: Decimal | None = None
    shortage_material_count: int = 0
    earliest_required_by_utc: datetime


class MrpExplodeResponse(BaseModel):
    calculation_engine_version: str = "2B.1.0"
    mrp_run_id: str
    total_events: int
    total_recipes: int
    total_unique_materials: int
    sufficient_stock_count: int
    shortage_count: int
    unknown_stock_count: int
    missing_mapping_count: int
    blocking_exception_count: int
    warning_exception_count: int
    estimated_requirement_value: Decimal | None = None
    indicative_shortage_value: Decimal | None = None
    currency: str = "AED"
    circular_dependency_detected: bool = False
    cache_hits: int = 0
    cache_misses: int = 0
    explosion_tree: list[MrpExplosionTreeNode] = Field(default_factory=list)
    consolidated_materials: list[MrpConsolidatedMaterialLine] = Field(default_factory=list)
    batch_summaries: list[MrpBatchSummary] = Field(default_factory=list)
    time_phased_buckets: list[MrpTimeBucketSummary] = Field(default_factory=list)
    exceptions: list[MrpExceptionItem] = Field(default_factory=list)


class MrpOptimizeBatchItem(BaseModel):
    recipe_id: str
    recipe_code: str
    recipe_name: str
    required_qty: Decimal
    batch_yield: Decimal
    min_batches: int = Field(default=1, ge=0)
    max_batches: int = Field(default=100, ge=1)
    cost_per_batch: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    allow_partial_batches: bool = False
    chef_locked: bool = False


class MrpOptimizeRequest(BaseModel):
    tenant_id: str
    max_excess_ratio: Decimal = Field(default=Decimal("0.50"), ge=Decimal("0"))
    max_total_cost: Decimal | None = Field(default=None, ge=Decimal("0"))
    items: list[MrpOptimizeBatchItem] = Field(default_factory=list)


class MrpOptimizedBatchResult(BaseModel):
    recipe_id: str
    recipe_code: str
    recipe_name: str
    selected_batches: Decimal
    planned_output: Decimal
    excess_output: Decimal
    estimated_cost: Decimal


class MrpOptimizeResponse(BaseModel):
    feasible: bool
    solver: str = "SCIPY_HIGHS_MILP"
    total_cost: Decimal = Decimal("0")
    total_excess_output: Decimal = Decimal("0")
    assumptions: list[str] = Field(default_factory=list)
    infeasibility_reason: str | None = None
    results: list[MrpOptimizedBatchResult] = Field(default_factory=list)


class MrpAiAnalyzeRequest(BaseModel):
    tenant_id: str
    question: str
    mrp_result: MrpExplodeResponse
    provider_policy: ProviderPolicy | None = None


class MrpAiAnalyzeOutput(BaseModel):
    answer: str
    referenced_materials: list[str] = Field(default_factory=list)
    referenced_recipes: list[str] = Field(default_factory=list)
    recommendations: list[str] = Field(default_factory=list)
    data_grounding_notes: list[str] = Field(default_factory=list)


class MrpAiAnalyzeResponse(BaseModel):
    provider_used: str = "LOCAL_DETERMINISTIC"
    model_used: str = "mrp-grounded-analyzer-2b"
    output: MrpAiAnalyzeOutput
    metadata: dict[str, Any] = Field(default_factory=dict)
