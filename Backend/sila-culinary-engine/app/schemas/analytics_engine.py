from __future__ import annotations

from datetime import datetime
from typing import Any, Literal
from pydantic import BaseModel, Field


CostMethodology = Literal[
    "CURRENT",
    "HISTORICAL",
    "STANDARD",
    "ACTUAL",
    "FORECAST",
    "WEIGHTED_AVERAGE",
    "LATEST_PURCHASE",
    "CONTRACT_PRICE",
]

QuadrantType = Literal["STAR", "PLOWHORSE", "PUZZLE", "DOG", "INSUFFICIENT_DATA"]


class MenuEngineeringItemInput(BaseModel):
    menu_item_id: str
    menu_item_code: str = ""
    name: str
    category: str = "Food"
    meal_period: str = "All Day"
    outlet_name: str = ""
    property_name: str = ""
    sales_channel: str = "DINE_IN"
    event_type: str | None = None
    quantity_sold: float = 0.0
    previous_quantity_sold: float | None = None
    gross_selling_price: float = 0.0
    discount_amount: float = 0.0
    tax_rate_percent: float = 0.0
    net_revenue: float | None = None
    standard_recipe_cost: float | None = None
    actual_ingredient_cost: float | None = None
    garnish_cost: float = 0.0
    packaging_cost: float = 0.0
    direct_labor_cost: float = 0.0
    allocated_overhead: float = 0.0
    previous_quadrant: QuadrantType | None = None
    has_sufficient_history: bool = True
    cost_source: str = "STANDARD_COST"
    recipe_version: int = 1
    cost_timestamp_utc: str | None = None
    methodology: CostMethodology = "STANDARD"


class MenuEngineeringAnalyzeRequest(BaseModel):
    contract_version: str = "v3b.1"
    currency: str = "AED"
    popularity_threshold_factor: float = 0.70
    contribution_threshold_override: float | None = None
    min_sales_for_classification: float = 5.0
    items: list[MenuEngineeringItemInput] = Field(default_factory=list)


class MenuEngineeringItemResult(BaseModel):
    menu_item_id: str
    menu_item_code: str
    name: str
    category: str
    meal_period: str
    outlet_name: str
    property_name: str
    quantity_sold: float
    net_sales_revenue: float
    average_selling_price: float
    standard_recipe_cost: float | None = None
    actual_ingredient_cost: float | None = None
    food_cost_percent: float | None = None
    contribution_margin_per_item: float | None = None
    total_contribution_margin: float | None = None
    fully_loaded_profit: float | None = None
    profit_per_portion: float | None = None
    popularity_percent: float
    sales_mix_percent: float
    revenue_contribution_percent: float
    profit_contribution_percent: float | None = None
    quadrant: QuadrantType
    previous_quadrant: QuadrantType | None = None
    movement: str = "UNCHANGED"
    cost_source: str
    recipe_version: int
    cost_timestamp_utc: str
    recommended_action: str


class MenuEngineeringAnalyzeResponse(BaseModel):
    contract_version: str = "v3b.1"
    currency: str
    popularity_threshold_percent: float
    contribution_margin_threshold: float
    weighted_avg_contribution_margin: float
    total_quantity_sold: float
    total_net_revenue: float
    total_contribution_margin: float
    counts: dict[str, int] = Field(default_factory=dict)
    items: list[MenuEngineeringItemResult] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


class ConsumptionVarianceInputRow(BaseModel):
    material_id: str
    material_code: str = ""
    material_name: str
    uom: str = "KG"
    unit_cost: float = 0.0
    sales_quantity: float = 0.0
    recipe_qty_per_portion: float = 0.0
    portion_factor: float = 1.0
    yield_factor: float = 1.0
    already_yield_adjusted: bool = False
    recipe_version: int = 1
    gross_stock_issues: float = 0.0
    stock_returns: float = 0.0
    inter_outlet_transfers_out: float = 0.0
    recorded_wastage: float = 0.0
    reconciled_actual_override: float | None = None
    pos_mapped: bool = True
    uom_verified: bool = True


class ConsumptionVarianceRequest(BaseModel):
    currency: str = "AED"
    tolerance_percent: float = 5.0
    rows: list[ConsumptionVarianceInputRow] = Field(default_factory=list)


class ConsumptionVarianceResultRow(BaseModel):
    material_id: str
    material_code: str
    material_name: str
    uom: str
    recipe_version: int
    theoretical_quantity: float
    reconciled_actual_quantity: float
    recorded_wastage: float
    quantity_variance: float
    unexplained_variance: float
    variance_percent: float | None = None
    financial_variance: float
    yield_variance_qty: float
    portion_variance_qty: float
    possible_causes: list[str] = Field(default_factory=list)
    exceeds_tolerance: bool = False


class ConsumptionVarianceResponse(BaseModel):
    currency: str
    total_theoretical_qty: float
    total_actual_qty: float
    total_recorded_wastage: float
    total_unexplained_qty: float
    total_financial_variance: float
    rows: list[ConsumptionVarianceResultRow] = Field(default_factory=list)
    note: str = "Recorded wastage is excluded from unexplained variance to prevent double-counting. Causes are candidate hypotheses requiring investigation."


class PriceImpactNodeInput(BaseModel):
    node_type: Literal["SUB_RECIPE", "RECIPE", "MENU_ITEM", "BUFFET", "EVENT"]
    node_id: str
    node_name: str
    property_name: str = ""
    outlet_name: str = ""
    qty_per_unit: float = 1.0
    monthly_sales_volume: float = 0.0
    current_unit_cost: float = 0.0
    selling_price: float = 0.0
    via_sub_recipe: str | None = None


class ReversePriceImpactRequest(BaseModel):
    currency: str = "AED"
    material_code: str
    material_name: str
    previous_price: float
    current_price: float
    alert_threshold_percent: float = 5.0
    nodes: list[PriceImpactNodeInput] = Field(default_factory=list)


class ReversePriceImpactNodeResult(BaseModel):
    node_type: str
    node_id: str
    node_name: str
    property_name: str
    outlet_name: str
    via_sub_recipe: str | None = None
    previous_unit_cost: float
    revised_unit_cost: float
    unit_cost_delta: float
    previous_food_cost_pct: float | None = None
    revised_food_cost_pct: float | None = None
    margin_impact_per_unit: float
    estimated_monthly_impact: float
    estimated_annual_impact: float


class ReversePriceImpactResponse(BaseModel):
    currency: str
    material_code: str
    material_name: str
    price_delta: float
    price_change_percent: float | None = None
    alert_triggered: bool
    affected_counts: dict[str, int] = Field(default_factory=dict)
    total_monthly_impact: float
    total_annual_impact: float
    impacts: list[ReversePriceImpactNodeResult] = Field(default_factory=list)


class WhatIfScenarioParam(BaseModel):
    scenario_name: str
    ingredient_price_delta_pct: float = 0.0
    selling_price_delta_pct: float = 0.0
    portion_size_delta_pct: float = 0.0
    covers: float = 500.0
    occupancy_pct: float = 80.0
    labor_cost_delta_pct: float = 0.0
    wastage_pct: float = 4.0
    yield_pct: float = 88.0
    discount_pct: float = 0.0
    overhead_amount: float = 10000.0
    buffet_replenishment_factor: float = 1.0
    substitution_savings_pct: float = 0.0


class WhatIfSimulationRequest(BaseModel):
    currency: str = "AED"
    base_selling_price_per_cover: float = 180.0
    base_food_cost_per_cover: float = 54.0
    base_labor_cost: float = 15000.0
    base_fixed_expenses: float = 20000.0
    target_food_cost_pct: float = 28.0
    scenarios: list[WhatIfScenarioParam] = Field(default_factory=list)


class WhatIfScenarioOutput(BaseModel):
    scenario_name: str
    covers: float
    net_revenue: float
    revised_food_cost: float
    revised_food_cost_pct: float | None = None
    revised_labor_cost: float
    total_cost: float
    contribution_margin: float
    contribution_margin_pct: float | None = None
    break_even_covers: int | None = None
    target_selling_price_for_food_cost: float | None = None
    expected_savings_vs_baseline: float
    profitability_delta_vs_baseline: float


class WhatIfSimulationResponse(BaseModel):
    currency: str
    isolated_from_production: bool = True
    results: list[WhatIfScenarioOutput] = Field(default_factory=list)


class MenuOptimizationCandidate(BaseModel):
    item_id: str
    name: str
    category: str = "Main"
    selling_price: float
    unit_cost: float
    wastage_risk_score: float = 0.05
    ingredient_utilization_score: float = 0.80
    min_portions: int = 0
    max_portions: int = 200
    min_expected_demand: int = 0
    prep_minutes_per_portion: float = 2.0
    allergens: list[str] = Field(default_factory=list)
    dietary_tags: list[str] = Field(default_factory=list)
    allergens_verified: bool = True
    approved_ingredients_only: bool = True


class MenuOptimizationRequest(BaseModel):
    currency: str = "AED"
    objective: Literal["MAX_CONTRIBUTION", "MIN_FOOD_COST", "MIN_WASTE", "MAX_UTILIZATION"] = "MAX_CONTRIBUTION"
    total_target_portions: int = 200
    target_food_cost_pct: float | None = 30.0
    max_budget: float | None = None
    max_kitchen_minutes: float | None = None
    excluded_allergens: list[str] = Field(default_factory=list)
    required_dietary_tags: list[str] = Field(default_factory=list)
    strict_allergen_safety: bool = True
    category_minimums: dict[str, int] = Field(default_factory=dict)
    candidates: list[MenuOptimizationCandidate] = Field(default_factory=list)


class MenuOptimizationSelection(BaseModel):
    item_id: str
    name: str
    category: str
    recommended_portions: int
    unit_cost: float
    selling_price: float
    total_cost: float
    total_revenue: float
    total_contribution: float


class MenuOptimizationResponse(BaseModel):
    status: Literal["OPTIMAL", "FEASIBLE", "INFEASIBLE"]
    currency: str
    objective: str
    baseline_cost: float
    baseline_contribution: float
    proposed_cost: float
    proposed_revenue: float
    proposed_contribution: float
    proposed_food_cost_pct: float | None = None
    expected_savings: float
    expected_margin_improvement_pct: float
    constraint_violations: list[str] = Field(default_factory=list)
    infeasibility_reasons: list[str] = Field(default_factory=list)
    assumptions: list[str] = Field(default_factory=list)
    data_quality: str = "VERIFIED"
    explanation: str
    selections: list[MenuOptimizationSelection] = Field(default_factory=list)


class SubstitutionCandidateInput(BaseModel):
    material_id: str
    material_code: str
    name: str
    category: str
    unit_cost: float
    uom: str = "KG"
    yield_percent: float = 100.0
    available_stock: float = 0.0
    allergens: list[str] = Field(default_factory=list)
    allergen_verified: bool = False
    dietary_tags: list[str] = Field(default_factory=list)
    dietary_verified: bool = False
    calories_per_100g: float | None = None
    culinary_compatibility_score: float = 0.8
    chef_approved: bool = False
    restricted: bool = False


class SubstitutionAnalysisRequest(BaseModel):
    currency: str = "AED"
    original_material_code: str
    original_name: str
    original_category: str
    original_unit_cost: float
    original_uom: str = "KG"
    original_yield_percent: float = 100.0
    required_qty: float = 1.0
    recipe_current_cost: float = 100.0
    excluded_allergens: list[str] = Field(default_factory=list)
    required_dietary_tags: list[str] = Field(default_factory=list)
    candidates: list[SubstitutionCandidateInput] = Field(default_factory=list)


class SubstitutionRecommendationResult(BaseModel):
    material_id: str
    material_code: str
    name: str
    classification: Literal["APPROVED_SUBSTITUTE", "PROPOSED_SUBSTITUTE", "RESTRICTED_SUBSTITUTE", "INSUFFICIENT_INFORMATION"]
    original_ingredient_cost: float
    substitute_ingredient_cost: float
    revised_recipe_cost: float
    cost_saving: float
    yield_impact_pct: float
    allergen_impact: str
    nutritional_impact: str
    availability_status: str
    requires_executive_chef_approval: bool = True
    auto_approval_blocked: bool = False
    blocking_reason: str | None = None


class SubstitutionAnalysisResponse(BaseModel):
    currency: str
    original_ingredient_cost: float
    recommendations: list[SubstitutionRecommendationResult] = Field(default_factory=list)


class BuffetDishInput(BaseModel):
    dish_id: str
    name: str
    meal_period: str = "Dinner"
    popularity_weight: float = 0.25
    portion_grams: float = 150.0
    unit_cost_per_portion: float = 12.0
    batch_size_portions: int = 20
    min_display_portions: int = 10
    max_holding_minutes: int = 120
    service_duration_minutes: int = 180
    historical_waste_pct: float = 8.0
    ingredient_available_portions: float = 500.0


class BuffetOptimizeRequest(BaseModel):
    currency: str = "AED"
    meal_period: Literal["BREAKFAST", "LUNCH", "DINNER", "BRUNCH", "BANQUET", "SPECIAL_EVENT"] = "DINNER"
    expected_covers: int = 200
    occupancy_percent: float = 85.0
    dishes: list[BuffetDishInput] = Field(default_factory=list)


class BuffetDishRecommendation(BaseModel):
    dish_id: str
    name: str
    initial_production_portions: int
    replenishment_batches: int
    replenishment_qty_per_batch: int
    replenishment_interval_minutes: int
    expected_consumption_portions: float
    expected_leftover_portions: float
    production_cost: float
    food_cost_per_guest: float
    potential_waste_reduction_portions: float
    food_safety_note: str


class BuffetOptimizeResponse(BaseModel):
    currency: str
    meal_period: str
    expected_covers: int
    total_production_cost: float
    food_cost_per_guest: float
    total_potential_waste_reduction_value: float
    food_safety_policy: str = "Holding-time limits enforced; leftover buffet food is never recommended for unsafe reuse."
    recommendations: list[BuffetDishRecommendation] = Field(default_factory=list)


class EventPackageOptimizeRequest(BaseModel):
    currency: str = "AED"
    guest_count: int = 500
    event_budget: float = 105000.0
    target_revenue: float = 175000.0
    target_margin_percent: float = 40.0
    base_food_cost_per_guest: float = 90.0
    base_beverage_cost_per_guest: float = 25.0
    manpower_cost: float = 15000.0
    fixed_expenses: float = 20000.0
    excluded_allergens: list[str] = Field(default_factory=list)
    required_dietary_tags: list[str] = Field(default_factory=list)


class EventPackageOption(BaseModel):
    package_code: Literal["BUDGET_OPTIMIZED", "MARGIN_OPTIMIZED", "PREMIUM_EXPERIENCE", "WASTE_OPTIMIZED", "BALANCED"]
    package_name: str
    menu_composition_summary: str
    estimated_food_cost: float
    estimated_beverage_cost: float
    estimated_manpower_cost: float
    estimated_event_cost: float
    cost_per_guest: float
    selling_price_per_guest: float
    expected_revenue: float
    expected_contribution: float
    expected_contribution_margin_pct: float
    estimated_fully_loaded_profit: float
    ingredient_requirement_kg: float
    procurement_requirement_value: float


class EventPackageOptimizeResponse(BaseModel):
    currency: str
    guest_count: int
    packages: list[EventPackageOption] = Field(default_factory=list)


class HistoricalDemandPoint(BaseModel):
    date: str
    covers: float
    occupancy_pct: float = 75.0
    event_guests: float = 0.0
    is_holiday: bool = False


class DemandForecastRequest(BaseModel):
    horizon_days: int = 7
    occupancy_assumption_pct: float = 78.0
    upcoming_event_guests: list[float] = Field(default_factory=list)
    historical: list[HistoricalDemandPoint] = Field(default_factory=list)
    avg_food_cost_per_cover: float = 45.0


class ForecastPoint(BaseModel):
    day_index: int
    date: str
    forecast_covers: float
    lower_bound_80: float
    upper_bound_80: float
    lower_bound_95: float
    upper_bound_95: float
    expected_food_cost: float
    is_forecast: bool = True


class DemandForecastResponse(BaseModel):
    model_name: str
    model_version: str = "v3b.1-seasonal-ols"
    horizon_days: int
    cold_start_used: bool
    mae: float | None = None
    wape_percent: float | None = None
    forecast_bias: float | None = None
    data_quality: str
    last_trained_utc: str
    forecast_points: list[ForecastPoint] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


class WasteEntryInput(BaseModel):
    waste_category: Literal[
        "PREPARATION_WASTE",
        "PRODUCTION_WASTE",
        "OVERPRODUCTION",
        "BUFFET_LEFTOVERS",
        "PLATE_WASTE",
        "SPOILAGE",
        "EXPIRED_INGREDIENTS",
        "STORAGE_DAMAGE",
        "YIELD_LOSS",
        "UNCLASSIFIED_WASTE",
    ]
    ingredient_name: str
    recipe_name: str = ""
    menu_item_name: str = ""
    kitchen_name: str = ""
    outlet_name: str = ""
    property_name: str = ""
    meal_period: str = "All Day"
    event_code: str = ""
    quantity: float = 0.0
    uom: str = "KG"
    cost: float = 0.0
    covers_served: int = 0


class WasteAnalyzeRequest(BaseModel):
    currency: str = "AED"
    total_food_cost: float = 10000.0
    total_covers: int = 500
    entries: list[WasteEntryInput] = Field(default_factory=list)


class WasteAnalyzeResponse(BaseModel):
    currency: str
    total_waste_cost: float
    waste_percent_of_food_cost: float | None = None
    waste_per_cover: float | None = None
    by_category: dict[str, float] = Field(default_factory=dict)
    by_property: dict[str, float] = Field(default_factory=dict)
    by_meal_period: dict[str, float] = Field(default_factory=dict)
    recurring_patterns: list[str] = Field(default_factory=list)
    recommendations: list[str] = Field(default_factory=list)


class PropertyBenchmarkInput(BaseModel):
    property_id: str
    property_name: str
    outlet_type: str = "ALL_DAY_DINING"
    local_currency: str = "AED"
    fx_to_reporting_rate: float | None = 1.0
    food_revenue_local: float = 0.0
    food_cost_local: float = 0.0
    waste_cost_local: float = 0.0
    covers: int = 0
    occupancy_pct: float = 0.0
    event_margin_pct: float | None = None
    forecast_accuracy_pct: float | None = None


class PropertyBenchmarkRequest(BaseModel):
    reporting_currency: str = "AED"
    properties: list[PropertyBenchmarkInput] = Field(default_factory=list)


class PropertyBenchmarkRow(BaseModel):
    property_id: str
    property_name: str
    outlet_type: str
    local_currency: str
    fx_rate_applied: float | None = None
    revenue_reporting: float | None = None
    food_cost_reporting: float | None = None
    food_cost_pct: float | None = None
    cost_per_cover_reporting: float | None = None
    revenue_per_cover_reporting: float | None = None
    contribution_margin_pct: float | None = None
    waste_pct: float | None = None
    waste_per_cover_reporting: float | None = None
    event_margin_pct: float | None = None
    forecast_accuracy_pct: float | None = None
    comparison_warning: str | None = None


class PropertyBenchmarkResponse(BaseModel):
    reporting_currency: str
    warnings: list[str] = Field(default_factory=list)
    rows: list[PropertyBenchmarkRow] = Field(default_factory=list)


class CopilotQueryRequest(BaseModel):
    question: str
    context_module: str = "EXECUTIVE_DASHBOARD"
    mode: str = "LOCAL"
    allow_external_ai: bool = False
    date_range: str = "Last 30 days"
    currency: str = "AED"
    grounded_metrics: dict[str, Any] = Field(default_factory=dict)


class CopilotQueryResponse(BaseModel):
    direct_answer: str
    supporting_metrics: list[str] = Field(default_factory=list)
    data_source: str
    date_range: str
    assumptions: list[str] = Field(default_factory=list)
    recommended_action: str
    estimated_financial_impact: str
    confidence: str
    supporting_route: str
    provider_used: str
    prompt_injection_blocked: bool = False
