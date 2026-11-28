from __future__ import annotations

from typing import Literal, Optional
from pydantic import BaseModel, Field


# ─────────────────────────────────────────────────────────────────────────────
# 1. Digital Twin Dependency Graph & Reverse Impact (NetworkX)
# ─────────────────────────────────────────────────────────────────────────────


class TwinNodeInput(BaseModel):
    node_id: str
    node_type: Literal[
        "INGREDIENT",
        "SUB_RECIPE",
        "RECIPE",
        "MENU_ITEM",
        "MENU",
        "OUTLET",
        "EVENT",
        "PRODUCTION",
        "PURCHASE",
    ]
    label: str
    unit_cost: Optional[float] = None
    selling_price: Optional[float] = None
    uom: Optional[str] = None
    is_approved: bool = True
    planned_covers: float = 0.0
    metadata: dict[str, object] = Field(default_factory=dict)


class TwinEdgeInput(BaseModel):
    source_id: str
    target_id: str
    relation: str = "USED_IN"
    quantity: float = 1.0
    uom: Optional[str] = None
    yield_percent: float = 100.0
    uom_valid: bool = True


class TwinDependencyGraphRequest(BaseModel):
    tenant_id: str
    property_id: str = "DEFAULT"
    nodes: list[TwinNodeInput] = Field(default_factory=list)
    edges: list[TwinEdgeInput] = Field(default_factory=list)
    shock_node_id: Optional[str] = None
    price_change_percent: float = 0.0


class TwinIntegrityIssue(BaseModel):
    issue_type: Literal[
        "CIRCULAR_DEPENDENCY",
        "INVALID_REFERENCE",
        "UNAPPROVED_RECIPE",
        "INVALID_UOM_CONVERSION",
        "MISSING_COST_REFERENCE",
    ]
    severity: Literal["ERROR", "WARNING"]
    node_ids: list[str] = Field(default_factory=list)
    detail: str


class TwinImpactedNode(BaseModel):
    node_id: str
    node_type: str
    label: str
    hop_distance: int
    baseline_cost: float
    shocked_cost: float
    cost_delta: float
    cost_delta_percent: float
    margin_delta: float


class TwinDependencyGraphResponse(BaseModel):
    node_count: int
    edge_count: int
    is_acyclic: bool
    cycles: list[list[str]] = Field(default_factory=list)
    integrity_issues: list[TwinIntegrityIssue] = Field(default_factory=list)
    shock_node_id: Optional[str] = None
    price_change_percent: float = 0.0
    impacted_nodes: list[TwinImpactedNode] = Field(default_factory=list)
    affected_recipes: list[str] = Field(default_factory=list)
    affected_menu_items: list[str] = Field(default_factory=list)
    affected_menus: list[str] = Field(default_factory=list)
    affected_events: list[str] = Field(default_factory=list)
    total_cost_impact: float = 0.0
    total_margin_impact: float = 0.0
    engine_version: str = "me-twin-nx-3.0"


# ─────────────────────────────────────────────────────────────────────────────
# 2. Multi-Objective Optimization Engine (OR-Tools CP-SAT + SciPy MILP)
# ─────────────────────────────────────────────────────────────────────────────


class OptDishCandidate(BaseModel):
    dish_id: str
    name: str
    category: str
    cuisine: str = "INTERNATIONAL"
    selling_price: float = Field(ge=0.0)
    ingredient_cost_per_portion: float = Field(ge=0.0)
    variable_cost_per_portion: float = Field(default=0.0, ge=0.0)
    predicted_popularity: float = Field(default=0.5, ge=0.0, le=1.0)
    predicted_waste_rate: float = Field(default=0.05, ge=0.0, le=1.0)
    prep_minutes_per_batch: float = Field(default=15.0, ge=0.0)
    batch_size_portions: float = Field(default=25.0, gt=0.0)
    complexity_score: float = Field(default=2.0, ge=0.0, le=10.0)
    eligible_inventory_coverage: float = Field(default=1.0, ge=0.0, le=1.0)
    shortage_risk: float = Field(default=0.0, ge=0.0, le=1.0)
    allergens: list[str] = Field(default_factory=list)
    dietary_tags: list[str] = Field(default_factory=list)
    kitchen_station: str = "HOT_KITCHEN"
    food_safety_approved: bool = True
    is_approved_recipe: bool = True
    min_portions: int = 0
    max_portions: int = 2000
    ingredient_requirements: dict[str, float] = Field(default_factory=dict)


class OptObjectiveWeight(BaseModel):
    objective: Literal[
        "MINIMIZE_FOOD_COST",
        "MAXIMIZE_CONTRIBUTION_MARGIN",
        "MAXIMIZE_POPULARITY",
        "MINIMIZE_WASTE",
        "MINIMIZE_PURCHASE_COST",
        "MAXIMIZE_INVENTORY_UTILIZATION",
        "MINIMIZE_COMPLEXITY",
        "MINIMIZE_SHORTAGE_RISK",
        "MAXIMIZE_DIVERSITY",
    ]
    weight: float = Field(default=1.0, ge=0.0)


class MeMultiObjectiveOptimizeRequest(BaseModel):
    mode: Literal[
        "SINGLE_OBJECTIVE",
        "WEIGHTED_MULTI_OBJECTIVE",
        "LEXICOGRAPHIC",
        "PARETO_FRONTIER",
    ] = "WEIGHTED_MULTI_OBJECTIVE"
    menu_format: Literal[
        "BUFFET",
        "BRUNCH",
        "BANQUET",
        "SET_MENU",
        "A_LA_CARTE",
        "WEEKLY",
        "SEASONAL",
    ] = "BUFFET"
    guest_count: int = Field(default=200, ge=1)
    target_selling_price: float = Field(default=150.0, gt=0.0)
    max_food_cost_percent: float = Field(default=30.0, gt=0.0, le=100.0)
    max_budget: Optional[float] = None
    min_total_items: int = Field(default=1, ge=1)
    max_total_items: int = Field(default=25, ge=1)
    category_counts: dict[str, int] = Field(default_factory=dict)
    excluded_allergens: list[str] = Field(default_factory=list)
    required_dietary_tags: list[str] = Field(default_factory=list)
    available_production_minutes: float = Field(default=480.0, gt=0.0)
    station_capacity_minutes: dict[str, float] = Field(default_factory=dict)
    eligible_inventory_stock: dict[str, float] = Field(default_factory=dict)
    soft_diversity_penalty: float = Field(default=0.1, ge=0.0)
    soft_waste_penalty: float = Field(default=0.2, ge=0.0)
    objectives: list[OptObjectiveWeight] = Field(default_factory=list)
    candidates: list[OptDishCandidate] = Field(default_factory=list)
    solver_time_limit_seconds: float = Field(default=5.0, ge=0.2, le=30.0)


class SelectedDishAllocation(BaseModel):
    dish_id: str
    name: str
    category: str
    planned_portions: int
    unit_cost: float
    total_cost: float
    contribution_margin: float
    prep_minutes: float
    inventory_coverage: float


class MeOptimizationSolution(BaseModel):
    scenario_label: str
    pareto_rank: int = 1
    solver_status: Literal["OPTIMAL", "FEASIBLE", "INFEASIBLE", "TIMEOUT"]
    is_feasible: bool
    is_globally_optimal: bool
    optimality_gap: Optional[float] = None
    solver_runtime_ms: float
    selected_dishes: list[SelectedDishAllocation] = Field(default_factory=list)
    total_revenue: float
    total_food_cost: float
    food_cost_percent: float
    total_contribution_margin: float
    expected_waste_cost: float
    inventory_utilization_percent: float
    total_prep_minutes: float
    normalized_objective_score: float
    objective_breakdown: dict[str, float] = Field(default_factory=dict)
    hard_constraints_satisfied: bool
    trade_off_explanation: list[str] = Field(default_factory=list)


class MeMultiObjectiveOptimizeResponse(BaseModel):
    status: Literal["OPTIMAL", "FEASIBLE", "INFEASIBLE", "ERROR"]
    solver_engine: str = "OR-TOOLS_CP_SAT"
    model_version: str = "me-opt-cpsat-3.0"
    solutions: list[MeOptimizationSolution] = Field(default_factory=list)
    infeasibility_reasons: list[str] = Field(default_factory=list)
    constraint_audit: dict[str, object] = Field(default_factory=dict)


# ─────────────────────────────────────────────────────────────────────────────
# 3. Inventory-Driven FEFO & Pricing Optimization
# ─────────────────────────────────────────────────────────────────────────────


class EligibleInventoryLot(BaseModel):
    ingredient_id: str
    ingredient_name: str
    eligible_qty: float = Field(ge=0.0)
    reserved_qty: float = Field(default=0.0, ge=0.0)
    blocked_qty: float = Field(default=0.0, ge=0.0)
    expired_qty: float = Field(default=0.0, ge=0.0)
    days_to_expiry: Optional[int] = None
    unit_cost: float = Field(ge=0.0)


class InventoryMenuOptimizeRequest(BaseModel):
    guest_count: int = Field(default=250, ge=1)
    min_items: int = Field(default=4, ge=1)
    max_items: int = Field(default=12, ge=1)
    max_food_cost_percent: float = Field(default=32.0, gt=0.0)
    inventory_lots: list[EligibleInventoryLot] = Field(default_factory=list)
    candidates: list[OptDishCandidate] = Field(default_factory=list)


class InventoryMenuOptimizeResponse(BaseModel):
    status: Literal["OPTIMAL", "FEASIBLE", "INFEASIBLE"]
    selected_dishes: list[SelectedDishAllocation] = Field(default_factory=list)
    inventory_utilization_percent: float
    avoidable_purchase_cost: float
    expiry_avoidance_value: float
    remaining_shortages: dict[str, float] = Field(default_factory=dict)
    excluded_stock_summary: dict[str, float] = Field(default_factory=dict)
    food_cost_percent: float
    expected_contribution_margin: float


class PricingCandidateInput(BaseModel):
    menu_item_id: str
    name: str
    current_price: float = Field(gt=0.0)
    ingredient_cost: float = Field(ge=0.0)
    variable_cost: float = Field(default=0.0, ge=0.0)
    baseline_daily_units: float = Field(ge=0.0)
    price_elasticity: Optional[float] = None
    min_price: float = Field(gt=0.0)
    max_price: float = Field(gt=0.0)
    max_change_percent: float = Field(default=15.0, gt=0.0)
    price_step: float = Field(default=1.0, gt=0.0)
    target_food_cost_percent: float = Field(default=28.0, gt=0.0)


class PricingOptimizeRequest(BaseModel):
    items: list[PricingCandidateInput] = Field(default_factory=list)


class PricingRecommendationItem(BaseModel):
    menu_item_id: str
    name: str
    current_price: float
    recommended_price: float
    price_delta_percent: float
    expected_units: float
    current_contribution: float
    recommended_contribution: float
    recommended_food_cost_percent: float
    elasticity_mode: Literal["VALIDATED_ELASTICITY", "FIXED_DEMAND_ASSUMPTION"]
    assumption_disclosure: str


class PricingOptimizeResponse(BaseModel):
    recommendations: list[PricingRecommendationItem] = Field(default_factory=list)
    total_current_contribution: float
    total_recommended_contribution: float
    requires_manual_publish_approval: bool = True


# ─────────────────────────────────────────────────────────────────────────────
# 4. Hierarchical Demand, Cold-Start, Food Waste & Price Prediction
# ─────────────────────────────────────────────────────────────────────────────


class HierarchicalSeriesInput(BaseModel):
    node_key: str
    level: Literal["PROPERTY", "OUTLET", "MEAL_PERIOD", "MENU", "MENU_ITEM", "RECIPE", "INGREDIENT"]
    parent_key: Optional[str] = None
    history: list[float] = Field(default_factory=list)
    confirmed_events: list[float] = Field(default_factory=list)


class HierarchicalForecastRequest(BaseModel):
    horizon_days: int = Field(default=7, ge=1, le=60)
    series: list[HierarchicalSeriesInput] = Field(default_factory=list)


class HierarchicalNodeForecast(BaseModel):
    node_key: str
    level: str
    parent_key: Optional[str] = None
    selected_model: str
    raw_forecast: list[float]
    reconciled_forecast: list[float]
    lower_p10: list[float]
    upper_p90: list[float]
    mae: float
    wape: float
    bias: float
    coverage_percent: float


class HierarchicalForecastResponse(BaseModel):
    horizon_days: int
    reconciliation_method: str = "TOP_DOWN_PROPORTIONAL"
    leakage_checked: bool = True
    node_forecasts: list[HierarchicalNodeForecast] = Field(default_factory=list)
    consistency_max_gap: float = 0.0


class ComparableDishFeature(BaseModel):
    dish_id: str
    name: str
    cuisine: str
    category: str
    outlet: str = "MAIN"
    service_format: str = "A_LA_CARTE"
    selling_price: float
    portion_grams: float = 200.0
    food_cost_percent: float = 28.0
    ingredients: list[str] = Field(default_factory=list)
    dietary_tags: list[str] = Field(default_factory=list)
    historical_daily_covers: float = 0.0
    historical_margin_percent: float = 70.0


class ColdStartPredictRequest(BaseModel):
    candidate: ComparableDishFeature
    historical_catalog: list[ComparableDishFeature] = Field(default_factory=list)
    top_k: int = Field(default=5, ge=1, le=20)


class ColdStartComparableMatch(BaseModel):
    dish_id: str
    name: str
    similarity_score: float
    historical_daily_covers: float
    historical_margin_percent: float


class ColdStartPredictResponse(BaseModel):
    is_cold_start_estimate: bool = True
    disclosure: str = "Cold-start estimate inferred from comparable approved dishes; not actual historical performance."
    expected_daily_demand: float
    demand_lower_p10: float
    demand_upper_p90: float
    expected_popularity_range: tuple[float, float]
    expected_margin_percent_range: tuple[float, float]
    uncertainty_indicator: Literal["HIGH", "MEDIUM", "LOW"]
    comparables: list[ColdStartComparableMatch] = Field(default_factory=list)


class WastePredictItem(BaseModel):
    item_id: str
    name: str
    planned_production_qty: float = Field(ge=0.0)
    expected_consumption_qty: float = Field(ge=0.0)
    prep_trim_loss_percent: float = Field(default=4.0, ge=0.0, le=60.0)
    cooking_yield_loss_percent: float = Field(default=8.0, ge=0.0, le=60.0)
    historical_overproduction_rate: float = Field(default=0.06, ge=0.0, le=1.0)
    historical_buffet_leftover_rate: float = Field(default=0.05, ge=0.0, le=1.0)
    ingredient_days_to_expiry: Optional[int] = None
    unit_cost: float = Field(default=10.0, ge=0.0)
    food_safety_reusable: bool = False


class WastePredictRequest(BaseModel):
    items: list[WastePredictItem] = Field(default_factory=list)


class WastePredictItemResult(BaseModel):
    item_id: str
    name: str
    prep_waste_qty: float
    cooking_yield_loss_qty: float
    overproduction_qty: float
    buffet_leftover_qty: float
    expiry_risk_qty: float
    total_predicted_waste_qty: float
    predicted_waste_cost: float
    recommendations: list[str] = Field(default_factory=list)


class WastePredictResponse(BaseModel):
    yield_and_waste_separated: bool = True
    food_safety_priority_enforced: bool = True
    total_predicted_waste_cost: float
    items: list[WastePredictItemResult] = Field(default_factory=list)


class PriceHistoryItem(BaseModel):
    ingredient_id: str
    ingredient_name: str
    current_approved_price: float = Field(gt=0.0)
    historical_prices: list[float] = Field(default_factory=list)
    horizon_days: int = Field(default=30, ge=1, le=180)


class PricePredictRequest(BaseModel):
    items: list[PriceHistoryItem] = Field(default_factory=list)


class PricePredictItemResult(BaseModel):
    ingredient_id: str
    ingredient_name: str
    current_approved_price: float
    sufficient_history: bool
    predicted_price_mid: Optional[float] = None
    predicted_price_low: Optional[float] = None
    predicted_price_high: Optional[float] = None
    expected_movement_percent: Optional[float] = None
    historical_volatility_percent: Optional[float] = None
    data_quality: Literal["HIGH", "MEDIUM", "INSUFFICIENT_HISTORY"]
    disclosure: str


class PricePredictResponse(BaseModel):
    results: list[PricePredictItemResult] = Field(default_factory=list)


# ─────────────────────────────────────────────────────────────────────────────
# 5. Monte Carlo, Sensitivity Tornado, Stress Testing, Substitution, Similarity
# ─────────────────────────────────────────────────────────────────────────────


class MonteCarloInput(BaseModel):
    seed: int = 42
    runs: int = Field(default=1000, ge=100, le=10000)
    guest_count_mean: float = Field(default=300.0, gt=0.0)
    guest_count_std: float = Field(default=30.0, ge=0.0)
    participation_mean: float = Field(default=0.75, gt=0.0, le=1.5)
    participation_std: float = Field(default=0.06, ge=0.0)
    selling_price_per_guest: float = Field(default=150.0, gt=0.0)
    base_food_cost_per_serving: float = Field(default=42.0, gt=0.0)
    ingredient_price_vol_percent: float = Field(default=8.0, ge=0.0, le=80.0)
    yield_mean_factor: float = Field(default=0.95, gt=0.3, le=1.05)
    yield_std: float = Field(default=0.03, ge=0.0)
    waste_rate_mean: float = Field(default=0.06, ge=0.0, le=0.5)
    waste_rate_std: float = Field(default=0.02, ge=0.0)
    event_budget: float = Field(default=15000.0, gt=0.0)
    target_food_cost_percent: float = Field(default=30.0, gt=0.0)
    available_stock_servings: float = Field(default=280.0, ge=0.0)
    guest_price_correlation: float = Field(default=0.25, ge=-0.9, le=0.9)


class MonteCarloResponse(BaseModel):
    seed: int
    runs: int
    expected_revenue: float
    expected_cost: float
    median_cost: float
    cost_p10: float
    cost_p90: float
    expected_food_cost_percent: float
    expected_contribution_margin: float
    margin_p10: float
    margin_p90: float
    downside_var_95_margin: float
    prob_exceed_budget: float
    prob_exceed_food_cost_target: float
    prob_ingredient_shortage: float
    assumptions: dict[str, object] = Field(default_factory=dict)
    runtime_ms: float


class SensitivityDriverInput(BaseModel):
    variable_name: str
    category: Literal["GUEST_COUNT", "SELLING_PRICE", "INGREDIENT_PRICE", "PORTION_SIZE", "YIELD", "WASTE", "PARTICIPATION", "LEAD_TIME"]
    baseline_value: float
    low_value: float
    high_value: float
    baseline_cost_impact_per_unit: float = 0.0
    baseline_revenue_impact_per_unit: float = 0.0


class SensitivityRequest(BaseModel):
    baseline_revenue: float = Field(gt=0.0)
    baseline_cost: float = Field(ge=0.0)
    target_food_cost_percent: float = Field(default=30.0, gt=0.0)
    drivers: list[SensitivityDriverInput] = Field(default_factory=list)


class SensitivityTornadoItem(BaseModel):
    variable_name: str
    category: str
    low_margin: float
    high_margin: float
    margin_swing: float
    low_food_cost_percent: float
    high_food_cost_percent: float
    food_cost_swing_percent: float


class SensitivityResponse(BaseModel):
    baseline_margin: float
    baseline_food_cost_percent: float
    tornado: list[SensitivityTornadoItem] = Field(default_factory=list)
    top_risk_drivers: list[str] = Field(default_factory=list)
    break_even_food_cost_ceiling: float


class StressScenarioSpec(BaseModel):
    stress_type: Literal[
        "GUEST_SURGE_30",
        "GUEST_DROP_20",
        "SEAFOOD_PRICE_25",
        "SUPPLIER_DELAY",
        "INGREDIENT_UNAVAILABLE",
        "EQUIPMENT_UNAVAILABLE",
        "LOW_YIELD",
        "CUSTOM",
    ]
    label: str
    guest_multiplier: float = 1.0
    ingredient_price_multiplier: float = 1.0
    yield_multiplier: float = 1.0
    supplier_delay_days: int = 0
    unavailable_ingredient: Optional[str] = None
    unavailable_equipment_minutes: float = 0.0


class StressTestRequest(BaseModel):
    baseline_guests: int = Field(default=300, ge=1)
    selling_price_per_guest: float = Field(default=150.0, gt=0.0)
    baseline_food_cost_per_guest: float = Field(default=42.0, ge=0.0)
    baseline_prep_minutes: float = Field(default=360.0, ge=0.0)
    kitchen_capacity_minutes: float = Field(default=450.0, gt=0.0)
    max_food_cost_percent: float = Field(default=32.0, gt=0.0)
    ingredient_buffer_days: int = Field(default=2, ge=0)
    critical_ingredients: list[str] = Field(default_factory=list)
    scenarios: list[StressScenarioSpec] = Field(default_factory=list)


class StressTestOutcome(BaseModel):
    stress_type: str
    label: str
    revised_guests: int
    revised_revenue: float
    revised_food_cost: float
    revised_food_cost_percent: float
    revised_margin: float
    revised_prep_minutes: float
    is_operationally_feasible: bool
    shortage_risk: Literal["LOW", "MEDIUM", "HIGH", "CRITICAL"]
    corrective_actions: list[str] = Field(default_factory=list)


class StressTestResponse(BaseModel):
    baseline_margin: float
    outcomes: list[StressTestOutcome] = Field(default_factory=list)


class SubstitutionCandidateInput(BaseModel):
    substitute_id: str
    substitute_name: str
    is_approved_relationship: bool = False
    yield_factor: float = Field(default=1.0, gt=0.0)
    unit_cost: float = Field(ge=0.0)
    supplier_available: bool = True
    allergens: list[str] = Field(default_factory=list)
    dietary_tags: list[str] = Field(default_factory=list)


class SubstitutionCascadeRequest(BaseModel):
    original_ingredient: str
    original_qty: float = Field(gt=0.0)
    original_unit_cost: float = Field(ge=0.0)
    recipe_selling_price: float = Field(gt=0.0)
    other_ingredients_cost: float = Field(default=0.0, ge=0.0)
    excluded_allergens: list[str] = Field(default_factory=list)
    required_dietary_tags: list[str] = Field(default_factory=list)
    candidates: list[SubstitutionCandidateInput] = Field(default_factory=list)


class SubstitutionCascadeResult(BaseModel):
    substitute_id: str
    substitute_name: str
    category: Literal["APPROVED_SUBSTITUTION", "PROPOSED_SUBSTITUTION", "REJECTED_UNSAFE"]
    is_safe: bool
    rejection_reason: Optional[str] = None
    recalculated_ingredient_qty: float
    recalculated_recipe_cost: float
    recalculated_food_cost_percent: float
    recalculated_margin: float
    cost_delta: float


class SubstitutionCascadeResponse(BaseModel):
    baseline_recipe_cost: float
    baseline_food_cost_percent: float
    results: list[SubstitutionCascadeResult] = Field(default_factory=list)


class HistoricalRecordInput(BaseModel):
    record_id: str
    title: str
    event_type: str = "BANQUET"
    service_format: str = "BUFFET"
    cuisine: str = "INTERNATIONAL"
    outlet: str = "MAIN"
    season: str = "Q4"
    guest_count: float = 100.0
    selling_price: float = 150.0
    food_cost_target: float = 30.0
    dishes: list[str] = Field(default_factory=list)
    actual_food_cost_percent: float = 29.0
    actual_waste_percent: float = 5.0
    actual_margin: float = 10000.0


class HistoricalSimilarityRequest(BaseModel):
    target: HistoricalRecordInput
    candidates: list[HistoricalRecordInput] = Field(default_factory=list)
    top_k: int = Field(default=5, ge=1, le=20)


class HistoricalSimilarityMatch(BaseModel):
    record_id: str
    title: str
    similarity_score: float
    matching_factors: list[str] = Field(default_factory=list)
    important_differences: list[str] = Field(default_factory=list)
    actual_food_cost_percent: float
    actual_waste_percent: float
    actual_margin: float


class HistoricalSimilarityResponse(BaseModel):
    matches: list[HistoricalSimilarityMatch] = Field(default_factory=list)
