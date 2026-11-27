from __future__ import annotations

from decimal import Decimal
from typing import Literal, Optional

from pydantic import BaseModel, Field

from app.schemas.material_ai import MaterialMasterRecord
from app.schemas.recipe_ai import ProviderRuntimeConfig


CostCompleteness = Literal["COMPLETE", "PARTIAL", "MISSING"]
TaxBasis = Literal["TAX_EXCLUSIVE", "TAX_INCLUSIVE"]
AdditionalCostBasis = Literal["FIXED_BATCH", "PER_PORTION", "PCT_OF_INGREDIENT", "HOURLY"]


class CostingIngredientInput(BaseModel):
    temp_ref: str = Field(min_length=1, max_length=60)
    description: str = Field(min_length=1, max_length=300)
    is_sub_recipe: bool = False
    material_id: Optional[str] = None
    material_code: Optional[str] = None
    sub_recipe_id: Optional[str] = None
    sub_recipe_name: Optional[str] = None
    sub_recipe_version: Optional[int] = None
    sub_recipe_batch_yield: Optional[Decimal] = None
    sub_recipe_total_cost: Optional[Decimal] = None
    sub_recipe_dependencies: list[str] = Field(default_factory=list)

    recipe_qty: Decimal = Field(gt=0)
    recipe_uom: str = Field(min_length=1, max_length=20)
    quantity_basis: Literal["RAW", "EDIBLE"] = "RAW"
    yield_percent: Decimal = Field(default=Decimal("100"), gt=0, le=100)
    prep_loss_percent: Decimal = Field(default=Decimal("0"), ge=0, lt=100)
    cooking_loss_percent: Decimal = Field(default=Decimal("0"), ge=0, lt=100)
    spoilage_percent: Decimal = Field(default=Decimal("0"), ge=0, lt=100)
    production_buffer_percent: Decimal = Field(default=Decimal("0"), ge=0, le=200)

    # Validated UOM conversion factor: recipe_uom -> costing_uom (base_uom)
    costing_uom: Optional[str] = None
    uom_conversion_factor: Optional[Decimal] = None
    uom_compatible: bool = True

    # Authorized unit price in price_currency per price_unit of price_uom
    unit_price: Optional[Decimal] = Field(default=None, ge=0)
    price_unit: Decimal = Field(default=Decimal("1"), gt=0)
    price_uom: Optional[str] = None
    price_uom_factor: Optional[Decimal] = None
    price_currency: Optional[str] = None
    exchange_rate_to_reporting: Optional[Decimal] = Field(default=None, gt=0)
    price_source: Optional[str] = None
    price_date: Optional[str] = None
    is_price_expired: bool = False
    allow_expired_fallback: bool = False
    is_manual_estimate: bool = False
    price_override: Optional[Decimal] = Field(default=None, ge=0)


class AdditionalCostComponentInput(BaseModel):
    code: str = Field(min_length=1, max_length=60)
    name: str = Field(min_length=1, max_length=140)
    category: Literal["DIRECT_OPERATING", "ALLOCATED_OVERHEAD"] = "DIRECT_OPERATING"
    basis: AdditionalCostBasis = "PER_PORTION"
    amount: Decimal = Field(ge=0)
    hours: Optional[Decimal] = Field(default=None, ge=0)
    source: str = "TENANT_POLICY"


class RecipeCostingRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    recipe_id_or_ref: str = Field(min_length=1, max_length=100)
    servings: Decimal = Field(gt=0)
    serving_size: Optional[Decimal] = Field(default=None, gt=0)
    reporting_currency: str = Field(default="AED", min_length=3, max_length=3)
    tax_basis: TaxBasis = "TAX_EXCLUSIVE"
    tax_rate_percent: Decimal = Field(default=Decimal("0"), ge=0, lt=100)
    selling_price_per_portion: Optional[Decimal] = Field(default=None, ge=0)
    target_food_cost_percent: Decimal = Field(default=Decimal("28"), gt=0, lt=100)
    target_gross_margin_percent: Optional[Decimal] = Field(default=None, gt=0, lt=100)
    global_price_increase_percent: Decimal = Field(default=Decimal("0"), ge=-99, le=500)
    global_buffer_percent: Decimal = Field(default=Decimal("0"), ge=0, le=200)
    wastage_multiplier_percent: Decimal = Field(default=Decimal("0"), ge=0, le=100)
    ingredients: list[CostingIngredientInput] = Field(default_factory=list)
    additional_costs: list[AdditionalCostComponentInput] = Field(default_factory=list)


class CostedIngredientLine(BaseModel):
    temp_ref: str
    description: str
    material_code: Optional[str] = None
    is_sub_recipe: bool = False
    recipe_qty: Decimal
    recipe_uom: str
    raw_qty: Decimal
    costing_qty: Decimal
    costing_uom: str
    effective_unit_cost: Optional[Decimal] = None
    currency: Optional[str] = None
    ingredient_cost: Optional[Decimal] = None
    percentage_of_total: Optional[Decimal] = None
    price_source: Optional[str] = None
    price_status: Literal[
        "VERIFIED",
        "ESTIMATED",
        "EXPIRED_FALLBACK",
        "MISSING",
        "CURRENCY_MISMATCH",
        "UOM_CONFLICT",
        "CIRCULAR_SUB_RECIPE",
    ]
    warnings: list[str] = Field(default_factory=list)


class RecipeCostingResponse(BaseModel):
    recipe_id_or_ref: str
    reporting_currency: str
    tax_basis: TaxBasis
    servings: Decimal
    total_ingredient_cost: Decimal
    verified_ingredient_cost: Decimal
    estimated_ingredient_cost: Decimal
    sub_recipe_cost: Decimal
    direct_operating_cost: Decimal
    allocated_overhead_cost: Decimal
    total_batch_cost: Decimal
    ingredient_cost_per_portion: Decimal
    total_cost_per_portion: Decimal
    selling_price_net: Optional[Decimal] = None
    selling_price_gross: Optional[Decimal] = None
    estimated_revenue: Optional[Decimal] = None
    food_cost_percent: Optional[Decimal] = None
    target_food_cost_percent: Decimal
    target_variance_percent: Optional[Decimal] = None
    gross_profit: Optional[Decimal] = None
    gross_margin_percent: Optional[Decimal] = None
    markup_percent: Optional[Decimal] = None
    contribution_amount: Optional[Decimal] = None
    contribution_margin_percent: Optional[Decimal] = None
    estimated_net_profit_after_overhead: Optional[Decimal] = None
    recommended_selling_price_net: Decimal
    recommended_selling_price_by_margin: Optional[Decimal] = None
    cost_completeness: CostCompleteness
    missing_price_count: int
    expired_price_count: int
    currency_mismatch_count: int
    uom_conflict_count: int
    circular_dependency_detected: bool
    warnings: list[str] = Field(default_factory=list)
    lines: list[CostedIngredientLine] = Field(default_factory=list)


class CostOptimizationRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    goal: str = Field(min_length=3, max_length=500)
    costing_request: RecipeCostingRequest
    provider_config: Optional[ProviderRuntimeConfig] = None


class CostOptimizationSuggestion(BaseModel):
    suggestion_id: str
    category: Literal[
        "TOP_COST_DRIVER",
        "PORTION_OR_YIELD",
        "BUFFER_OPTIMIZATION",
        "SUBSTITUTION_REVIEW",
    ]
    title: str
    rationale: str
    target_temp_ref: Optional[str] = None
    projected_ingredient_cost: Decimal
    projected_cost_savings: Decimal
    projected_food_cost_percent: Optional[Decimal] = None
    requires_chef_confirmation: bool = True
    allergen_or_safety_warning: Optional[str] = None


class CostOptimizationResponse(BaseModel):
    baseline_ingredient_cost: Decimal
    baseline_food_cost_percent: Optional[Decimal] = None
    goal: str
    provider_used: str = "LOCAL_DETERMINISTIC"
    suggestions: list[CostOptimizationSuggestion] = Field(default_factory=list)


class MaterialRequestAssistRequest(BaseModel):
    tenant_id: str = Field(min_length=1)
    ingredient_description: str = Field(min_length=1, max_length=300)
    required_qty: Optional[Decimal] = None
    recipe_uom: Optional[str] = None
    recipe_name: Optional[str] = None
    existing_materials: list[MaterialMasterRecord] = Field(default_factory=list)
    provider_config: Optional[ProviderRuntimeConfig] = None


class MaterialRequestAssistResponse(BaseModel):
    recommended_request_type: str
    proposed_description: str
    proposed_long_description: str
    ingredient_category: str
    proposed_material_group: str
    proposed_base_uom: str
    product_specification: str
    storage_requirement: str
    potential_allergens: list[str] = Field(default_factory=list)
    business_justification: str
    potential_duplicates: list[dict] = Field(default_factory=list)
    provider_used: str = "LOCAL_DETERMINISTIC"
    ai_suggested: bool = True
