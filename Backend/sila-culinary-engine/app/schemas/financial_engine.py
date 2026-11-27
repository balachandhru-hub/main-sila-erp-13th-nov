from __future__ import annotations

from datetime import datetime
from decimal import Decimal
from typing import Literal, Optional
from pydantic import BaseModel, Field


PriceSourceType = Literal[
    "SUPPLIER_CONTRACT",
    "SUPPLIER_CATALOG",
    "INVENTORY_VALUATION",
    "MOVING_AVERAGE",
    "STANDARD_COST",
    "LATEST_PURCHASE",
    "WEIGHTED_HISTORICAL",
    "MANUAL_ESTIMATE",
    "SUB_RECIPE_SNAPSHOT",
    "MISSING",
]

CostClassType = Literal[
    "FOOD",
    "ALCOHOLIC_BEVERAGE",
    "NON_ALCOHOLIC_BEVERAGE",
    "TOBACCO",
    "PACKAGING",
    "CONSUMABLES",
    "DIRECT_PRODUCTION",
]

CostBehaviorType = Literal[
    "FIXED",
    "VARIABLE_PER_GUEST",
    "VARIABLE_PER_SERVING",
    "VARIABLE_PER_HOUR",
    "VARIABLE_PER_BATCH",
    "MIXED",
    "STEP_FIXED",
]

ExpenseCategoryType = Literal[
    "FOOD",
    "BEVERAGE",
    "CONSUMABLES",
    "PACKAGING",
    "MANPOWER",
    "EQUIPMENT",
    "DECORATION",
    "ENTERTAINMENT",
    "EXTERNAL_SERVICE",
    "UTILITIES",
    "OTHER",
    "OVERHEAD",
]

RevenueStatusType = Literal["PLANNED", "CONTRACTED", "INVOICED", "RECOGNIZED"]
BillablePolicyType = Literal["GUARANTEED_ONLY", "GUARANTEED_PLUS_ADDITIONAL", "ACTUAL_ATTENDANCE"]


class FxRateInput(BaseModel):
    source_currency: str
    target_currency: str
    rate: Decimal = Field(gt=Decimal("0"))
    effective_date_utc: Optional[datetime] = None
    rate_type: str = "CORPORATE"
    source: str = "TENANT_CONFIG"


class PriceCandidateInput(BaseModel):
    source_type: PriceSourceType
    unit_price: Decimal = Field(ge=Decimal("0"))
    currency: str = "AED"
    price_unit: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    pricing_uom: str = "KG"
    price_uom_to_base_factor: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    supplier_code: Optional[str] = None
    supplier_name: Optional[str] = None
    source_document: Optional[str] = None
    effective_date_utc: Optional[datetime] = None
    valid_until_utc: Optional[datetime] = None
    is_verified: bool = True


class FinancialIngredientInput(BaseModel):
    line_ref: str
    material_code: Optional[str] = None
    description: str
    cost_class: CostClassType = "FOOD"
    is_sub_recipe: bool = False
    sub_recipe_id: Optional[str] = None
    sub_recipe_version: Optional[int] = None
    sub_recipe_dependencies: list[str] = Field(default_factory=list)
    sub_recipe_batch_cost: Optional[Decimal] = None
    sub_recipe_batch_yield: Optional[Decimal] = None
    sub_recipe_already_exploded_in_parent: bool = False
    quantity: Decimal = Field(ge=Decimal("0"))
    uom: str = "KG"
    base_uom: str = "KG"
    uom_to_base_factor: Optional[Decimal] = Decimal("1")
    uom_compatible: bool = True
    quantity_basis: Literal["RAW", "EDIBLE"] = "RAW"
    yield_percent: Decimal = Field(default=Decimal("100"), gt=Decimal("0"), le=Decimal("100"))
    yield_already_applied_in_mrp: bool = False
    is_mandatory: bool = True
    price_candidates: list[PriceCandidateInput] = Field(default_factory=list)
    manual_override_price: Optional[Decimal] = None
    manual_override_currency: Optional[str] = None
    allow_expired_price: bool = False


class FinancialAdditionalCostInput(BaseModel):
    name: str
    layer: Literal["DIRECT_PRODUCTION", "ALLOCATED_OVERHEAD"] = "DIRECT_PRODUCTION"
    basis: Literal["FIXED_BATCH", "PER_SERVING", "PCT_INGREDIENT"] = "FIXED_BATCH"
    amount: Decimal = Field(ge=Decimal("0"))


class FinancialRecipeCostRequest(BaseModel):
    recipe_id_or_ref: str
    recipe_name: str
    recipe_version: int = 1
    reporting_currency: str = "AED"
    valuation_date_utc: Optional[datetime] = None
    cost_source_priority: list[PriceSourceType] = Field(
        default_factory=lambda: [
            "SUPPLIER_CONTRACT",
            "SUPPLIER_CATALOG",
            "INVENTORY_VALUATION",
            "MOVING_AVERAGE",
            "LATEST_PURCHASE",
            "WEIGHTED_HISTORICAL",
            "STANDARD_COST",
            "MANUAL_ESTIMATE",
        ]
    )
    standard_batch_servings: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    planned_servings: Optional[Decimal] = None
    allow_partial_batch: bool = True
    finished_output_qty: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    finished_output_uom: str = "PORTION"
    selling_price_per_serving: Optional[Decimal] = Field(default=None, ge=Decimal("0"))
    vat_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    selling_price_includes_vat: bool = False
    target_food_cost_percent: Decimal = Field(default=Decimal("28"), gt=Decimal("0"), lt=Decimal("100"))
    target_contribution_margin_percent: Decimal = Field(default=Decimal("40"), gt=Decimal("0"), lt=Decimal("100"))
    global_price_change_percent: Decimal = Decimal("0")
    fx_rates: list[FxRateInput] = Field(default_factory=list)
    ingredients: list[FinancialIngredientInput] = Field(default_factory=list)
    additional_costs: list[FinancialAdditionalCostInput] = Field(default_factory=list)


class FinancialIngredientResult(BaseModel):
    line_ref: str
    material_code: Optional[str] = None
    description: str
    cost_class: str
    is_sub_recipe: bool = False
    recipe_qty: Decimal
    recipe_uom: str
    raw_qty: Decimal
    costable_base_qty: Decimal
    base_uom: str
    effective_yield_percent: Decimal
    normalized_unit_cost: Optional[Decimal] = None
    price_source: PriceSourceType = "MISSING"
    source_currency: Optional[str] = None
    fx_rate_applied: Decimal = Decimal("1")
    ingredient_cost: Optional[Decimal] = None
    share_of_ingredient_cost_pct: Optional[Decimal] = None
    status: Literal["VERIFIED", "ESTIMATED", "MISSING_PRICE", "EXPIRED_PRICE", "MISSING_FX", "UOM_CONFLICT", "CIRCULAR_DEPENDENCY"] = "VERIFIED"
    warning: Optional[str] = None


class FinancialRecipeCostResponse(BaseModel):
    recipe_id_or_ref: str
    recipe_name: str
    recipe_version: int
    reporting_currency: str
    standard_batch_servings: Decimal
    planned_servings: Decimal
    planned_batches: Decimal
    planned_output_servings: Decimal
    excess_servings: Decimal
    total_ingredient_cost: Decimal
    food_ingredient_cost: Decimal
    beverage_ingredient_cost: Decimal
    sub_recipe_cost: Decimal
    direct_production_cost: Decimal
    allocated_overhead_cost: Decimal
    fully_allocated_batch_cost: Decimal
    expected_event_consumption_cost: Decimal
    remaining_usable_output_value: Decimal
    ingredient_cost_per_serving: Decimal
    direct_cost_per_serving: Decimal
    fully_allocated_cost_per_serving: Decimal
    cost_per_finished_unit: Decimal
    net_selling_price_per_serving: Optional[Decimal] = None
    food_cost_percent: Optional[Decimal] = None
    ingredient_contribution_per_serving: Optional[Decimal] = None
    ingredient_contribution_percent: Optional[Decimal] = None
    direct_contribution_per_serving: Optional[Decimal] = None
    fully_allocated_profit_per_serving: Optional[Decimal] = None
    required_selling_price_target_food_cost: Decimal
    required_selling_price_target_margin_direct: Decimal
    required_selling_price_target_margin_full: Decimal
    cost_completeness: Literal["COMPLETE", "INCOMPLETE"] = "COMPLETE"
    missing_price_count: int = 0
    missing_fx_count: int = 0
    uom_conflict_count: int = 0
    circular_dependency_detected: bool = False
    exceptions: list[dict] = Field(default_factory=list)
    lines: list[FinancialIngredientResult] = Field(default_factory=list)


class MenuLineFinancialInput(BaseModel):
    line_ref: str
    recipe_id: Optional[str] = None
    name: str
    course: str = "Main Course"
    cost_class: Literal["FOOD", "BEVERAGE"] = "FOOD"
    cost_per_serving: Decimal = Field(ge=Decimal("0"))
    guest_uptake_percent: Decimal = Field(default=Decimal("100"), ge=Decimal("0"), le=Decimal("200"))
    servings_per_guest: Decimal = Field(default=Decimal("1"), ge=Decimal("0"))
    allocated_selling_price: Optional[Decimal] = None


class MenuCostingRequest(BaseModel):
    menu_id_or_ref: str
    menu_name: str
    service_style: str = "BUFFET"
    planned_guests: int = Field(gt=0)
    reporting_currency: str = "AED"
    package_selling_price_per_guest: Optional[Decimal] = None
    allocated_food_revenue_per_guest: Optional[Decimal] = None
    allocated_beverage_revenue_per_guest: Optional[Decimal] = None
    lines: list[MenuLineFinancialInput] = Field(default_factory=list)


class MenuLineFinancialResult(BaseModel):
    line_ref: str
    name: str
    course: str
    cost_class: str
    expected_portions: Decimal
    cost_per_serving: Decimal
    expected_line_cost: Decimal
    cost_per_guest: Decimal


class MenuCostingResponse(BaseModel):
    menu_id_or_ref: str
    menu_name: str
    service_style: str
    planned_guests: int
    reporting_currency: str
    total_food_cost: Decimal
    total_beverage_cost: Decimal
    total_menu_cost: Decimal
    food_cost_per_guest: Decimal
    beverage_cost_per_guest: Decimal
    expected_cost_per_guest: Decimal
    total_package_revenue: Optional[Decimal] = None
    expected_contribution: Optional[Decimal] = None
    food_cost_percent: Optional[Decimal] = None
    food_cost_basis_label: str
    beverage_cost_percent: Optional[Decimal] = None
    beverage_cost_basis_label: str
    lines: list[MenuLineFinancialResult] = Field(default_factory=list)


class EventRevenueLineInput(BaseModel):
    line_ref: str
    category: str = "PACKAGE"
    description: str = ""
    revenue_status: RevenueStatusType = "PLANNED"
    billing_basis: Literal["PER_GUEST", "FIXED", "GUARANTEED_RULE"] = "PER_GUEST"
    quantity: Decimal = Field(default=Decimal("1"), ge=Decimal("0"))
    unit_price: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    discount_amount: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    discount_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    tax_amount: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    currency: str = "AED"
    exchange_rate_to_reporting: Optional[Decimal] = None


class EventExpenseLineInput(BaseModel):
    expense_ref: str
    category: ExpenseCategoryType = "OTHER"
    description: str = ""
    cost_behavior: CostBehaviorType = "FIXED"
    step_threshold_guests: Optional[int] = None
    step_increment_cost: Optional[Decimal] = None
    staff_count: Optional[int] = None
    planned_hours: Optional[Decimal] = None
    hourly_rate: Optional[Decimal] = None
    overtime_hours: Optional[Decimal] = None
    overtime_rate: Optional[Decimal] = None
    fixed_shift_cost: Optional[Decimal] = None
    external_agency_charge: Optional[Decimal] = None
    budget_amount: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    estimated_amount: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    committed_amount: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    actual_verified_amount: Optional[Decimal] = None
    variable_rate_per_guest: Optional[Decimal] = None
    currency: str = "AED"
    exchange_rate_to_reporting: Optional[Decimal] = None
    is_duplicate_of: Optional[str] = None


class OverheadAllocationRuleInput(BaseModel):
    rule_id: str
    rule_name: str
    method: Literal[
        "FIXED_PER_EVENT",
        "PCT_NET_REVENUE",
        "PCT_DIRECT_COST",
        "PER_GUEST",
        "PER_LABOR_HOUR",
        "PER_KITCHEN_HOUR",
        "PER_VENUE_HOUR",
        "MANUAL_APPROVED",
    ] = "FIXED_PER_EVENT"
    rate_or_amount: Decimal = Field(ge=Decimal("0"))
    source_pool_id: Optional[str] = None
    driver_hours: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    is_approved: bool = True


class ProcurementCommitmentItemInput(BaseModel):
    material_code: str
    po_number: Optional[str] = None
    po_status: Literal["PROPOSED", "DRAFT", "APPROVED", "RELEASED", "RECEIVED"] = "DRAFT"
    purchased_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    event_allocated_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    other_events_allocated_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    unit_cost: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    received_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    invoiced_value: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    actual_consumed_qty: Optional[Decimal] = None
    actual_consumed_valuation: Optional[Decimal] = None
    currency: str = "AED"
    exchange_rate_to_reporting: Optional[Decimal] = None


class VarianceLineInput(BaseModel):
    material_code: str
    standard_qty: Decimal = Field(ge=Decimal("0"))
    standard_unit_price: Decimal = Field(ge=Decimal("0"))
    actual_qty: Decimal = Field(ge=Decimal("0"))
    actual_unit_price: Decimal = Field(ge=Decimal("0"))


class EventFinancialCalculateRequest(BaseModel):
    event_id: str
    event_name: str
    reporting_currency: str = "AED"
    planned_guests: int = Field(gt=0)
    guaranteed_guests: int = Field(default=0, ge=0)
    actual_guests: Optional[int] = Field(default=None, ge=0)
    billable_policy: BillablePolicyType = "GUARANTEED_PLUS_ADDITIONAL"
    selling_price_per_guest: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    food_cost_per_guest: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    beverage_cost_per_guest: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    target_food_cost_percent: Decimal = Field(default=Decimal("28"), gt=Decimal("0"), lt=Decimal("100"))
    target_beverage_cost_percent: Decimal = Field(default=Decimal("22"), gt=Decimal("0"), lt=Decimal("100"))
    target_contribution_margin_percent: Decimal = Field(default=Decimal("40"), gt=Decimal("0"), lt=Decimal("100"))
    approved_budget: Optional[Decimal] = None
    discount_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    revenue_lines: list[EventRevenueLineInput] = Field(default_factory=list)
    expense_lines: list[EventExpenseLineInput] = Field(default_factory=list)
    overhead_rules: list[OverheadAllocationRuleInput] = Field(default_factory=list)
    procurement_items: list[ProcurementCommitmentItemInput] = Field(default_factory=list)
    variance_lines: list[VarianceLineInput] = Field(default_factory=list)
    actual_incurred_override: Optional[Decimal] = None
    remaining_uncommitted_estimate_override: Optional[Decimal] = None


class WaterfallStepDto(BaseModel):
    label: str
    amount: Decimal
    kind: Literal["REVENUE", "COST", "SUBTOTAL", "OVERHEAD", "CONTRIBUTION"]


class EventFinancialCalculateResponse(BaseModel):
    event_id: str
    event_name: str
    reporting_currency: str
    planned_guests: int
    guaranteed_guests: int
    actual_guests: Optional[int] = None
    billable_guests: int
    gross_revenue: Decimal
    discount_total: Decimal
    net_revenue: Decimal
    planned_revenue: Decimal
    contracted_revenue: Decimal
    invoiced_revenue: Decimal
    recognized_revenue: Decimal
    estimated_food_cost: Decimal
    estimated_beverage_cost: Decimal
    estimated_manpower_cost: Decimal
    estimated_equipment_cost: Decimal
    estimated_decoration_cost: Decimal
    estimated_external_service_cost: Decimal
    estimated_other_direct_cost: Decimal
    estimated_direct_cost: Decimal
    estimated_allocated_overhead: Decimal
    estimated_total_cost: Decimal
    proposed_purchase_value: Decimal
    draft_po_value: Decimal
    approved_procurement_commitment: Decimal
    attributable_procurement_commitment: Decimal
    expected_consumption_value_from_po: Decimal
    remaining_unallocated_inventory_value: Decimal
    received_material_value: Decimal
    invoiced_material_value: Decimal
    actual_verified_cost: Decimal
    actual_is_provisional: bool = True
    forecast_at_completion_cost: Decimal
    direct_event_contribution: Decimal
    event_contribution_after_overhead: Decimal
    contribution_margin_percent: Optional[Decimal] = None
    food_cost_percent: Optional[Decimal] = None
    beverage_cost_percent: Optional[Decimal] = None
    labor_cost_percent: Optional[Decimal] = None
    revenue_per_guest: Optional[Decimal] = None
    cost_per_guest: Optional[Decimal] = None
    contribution_per_guest: Optional[Decimal] = None
    fixed_cost_total: Decimal
    variable_cost_per_guest: Decimal
    break_even_guests: Optional[int] = None
    break_even_revenue: Optional[Decimal] = None
    required_net_revenue_target_margin: Decimal
    required_price_per_guest_target_margin: Decimal
    max_allowable_total_cost: Decimal
    remaining_cost_headroom: Decimal
    budget_variance: Optional[Decimal] = None
    target_margin_variance_percent: Optional[Decimal] = None
    price_variance_total: Decimal = Decimal("0")
    quantity_variance_total: Decimal = Decimal("0")
    total_material_variance: Decimal = Decimal("0")
    flexible_budget_food_cost: Optional[Decimal] = None
    guest_volume_variance_amount: Optional[Decimal] = None
    waterfall: list[WaterfallStepDto] = Field(default_factory=list)
    exceptions: list[dict] = Field(default_factory=list)


class MaterialPriceImpactItem(BaseModel):
    recipe_or_event_ref: str
    name: str
    kind: Literal["RECIPE", "SUB_RECIPE", "MENU", "EVENT"]
    required_qty: Decimal
    uom: str
    current_unit_price: Decimal
    new_unit_price: Decimal


class MaterialPriceImpactRequest(BaseModel):
    material_code: str
    material_description: str
    reporting_currency: str = "AED"
    affected_items: list[MaterialPriceImpactItem] = Field(default_factory=list)


class MaterialPriceImpactLine(BaseModel):
    recipe_or_event_ref: str
    name: str
    kind: str
    required_qty: Decimal
    uom: str
    current_cost: Decimal
    projected_cost: Decimal
    cost_delta: Decimal


class MaterialPriceImpactResponse(BaseModel):
    material_code: str
    material_description: str
    reporting_currency: str
    total_current_cost: Decimal
    total_projected_cost: Decimal
    total_cost_delta: Decimal
    lines: list[MaterialPriceImpactLine] = Field(default_factory=list)


class FinancialAiAnalyzeRequest(BaseModel):
    question: str
    mode: Literal["LOCAL", "GEMINI", "CLAUDE", "HYBRID"] = "LOCAL"
    task_name: str = "FOOD_COST_RECOMMENDATIONS"
    allow_external_ai: bool = False
    model_name: Optional[str] = None
    api_key: Optional[str] = None
    base_url: Optional[str] = None
    event_context: dict = Field(default_factory=dict)


class FinancialAiAnalyzeResponse(BaseModel):
    answer: str
    provider_used: str
    grounded_facts: list[str] = Field(default_factory=list)
    recommendations: list[dict] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)
    prompt_injection_blocked: bool = False
