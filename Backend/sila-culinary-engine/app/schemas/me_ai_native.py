from __future__ import annotations

from typing import Any, Literal
from pydantic import BaseModel, Field


class MeNlParseRequest(BaseModel):
    command: str = Field(..., min_length=1, max_length=2000)
    currency: str = "AED"
    default_covers: int = Field(default=100, ge=1, le=50000)


class MeNlParseResponse(BaseModel):
    intent: str  # CREATE_MENU | WHAT_IF_SIMULATE | OPTIMIZE_MENU | WEEKLY_PLAN | PRICE_SHOCK | SUBSTITUTION | GENERAL_QUERY
    service_format: str | None = None
    meal_period: str | None = None
    cuisine: str | None = None
    expected_covers: int | None = None
    selling_price_target: float | None = None
    max_food_cost_percent: float | None = None
    min_margin_improvement_percent: float | None = None
    required_dietary: list[str] = Field(default_factory=list)
    required_categories: dict[str, int] = Field(default_factory=dict)
    optimization_mode: str = "BALANCED"
    target_ingredient: str | None = None
    ingredient_price_change_percent: float | None = None
    maximize_inventory_utilization: bool = False
    preserve_categories: bool = False
    sanitized_command: str
    confidence: float = 0.85


class MeCandidateDish(BaseModel):
    menu_item_id: str
    name: str
    category: str = "Main"
    cuisine: str = "International"
    dietary_tags: list[str] = Field(default_factory=list)
    allergens: list[str] = Field(default_factory=list)
    food_cost_per_portion: float = 0.0
    selling_price: float = 0.0
    contribution_margin: float = 0.0
    popularity_score: float = 50.0
    available_portions: float = 1000.0
    estimated_waste_percent: float = 4.0
    prep_time_minutes: int = 15
    is_healthy: bool = False
    is_seasonal: bool = False
    is_premium: bool = False


class MeOptimizationWeights(BaseModel):
    food_cost: float = 0.25
    contribution_margin: float = 0.25
    popularity: float = 0.15
    availability: float = 0.15
    waste: float = 0.10
    production_feasibility: float = 0.05
    diversity: float = 0.05


class MeMultiConstraintOptimizeRequest(BaseModel):
    covers: int = Field(default=100, ge=1, le=50000)
    selling_price_per_guest: float | None = None
    max_food_cost_percent: float | None = 30.0
    max_budget_per_guest: float | None = None
    required_dietary: list[str] = Field(default_factory=list)
    excluded_allergens: list[str] = Field(default_factory=list)
    required_categories: dict[str, int] = Field(default_factory=dict)
    max_prep_capacity_minutes: int | None = None
    optimization_mode: str = "BALANCED"
    weights: MeOptimizationWeights = Field(default_factory=MeOptimizationWeights)
    candidates: list[MeCandidateDish] = Field(default_factory=list)


class MeScenarioResult(BaseModel):
    scenario_code: str
    label: str
    strategy: str
    feasible: bool = True
    conflict_reason: str | None = None
    selected_item_ids: list[str] = Field(default_factory=list)
    selected_item_names: list[str] = Field(default_factory=list)
    food_cost_per_guest: float = 0.0
    total_food_cost: float = 0.0
    expected_revenue: float = 0.0
    food_cost_percent: float = 0.0
    contribution_margin: float = 0.0
    popularity_index: float = 0.0
    inventory_utilization_percent: float = 100.0
    estimated_waste_percent: float = 0.0
    pareto_optimal: bool = True
    trade_off_summary: str = ""


class MeMultiConstraintOptimizeResponse(BaseModel):
    feasible: bool
    conflict_explanation: str | None = None
    recommended_scenario_code: str
    scenarios: list[MeScenarioResult] = Field(default_factory=list)


class MeBacktestForecastRequest(BaseModel):
    series: list[float] = Field(default_factory=list)
    horizon: int = Field(default=7, ge=1, le=90)
    confirmed_events_by_day: list[float] = Field(default_factory=list)
    occupancy_by_day: list[float] = Field(default_factory=list)


class MeBacktestModelMetric(BaseModel):
    model_name: str
    model_version: str
    mae: float
    wape: float
    bias: float
    interval_coverage_percent: float
    selected: bool = False


class MeBacktestForecastResponse(BaseModel):
    selected_model: str
    selected_model_version: str
    data_quality: str
    cold_start: bool
    forecast: list[float] = Field(default_factory=list)
    lower_bound: list[float] = Field(default_factory=list)
    upper_bound: list[float] = Field(default_factory=list)
    model_metrics: list[MeBacktestModelMetric] = Field(default_factory=list)
    drivers: list[str] = Field(default_factory=list)
    warnings: list[str] = Field(default_factory=list)


class MePosMappingCandidateItem(BaseModel):
    menu_item_id: str
    name: str
    category: str = ""
    recipe_ingredients: list[str] = Field(default_factory=list)


class MePosMappingSuggestRequest(BaseModel):
    pos_code: str
    pos_description: str
    outlet_context: str | None = None
    catalog: list[MePosMappingCandidateItem] = Field(default_factory=list)
    available_modifiers: list[str] = Field(default_factory=list)


class MePosMappingMatch(BaseModel):
    menu_item_id: str
    menu_item_name: str
    confidence: float
    portion_multiplier: float = 1.0
    size_variant: str | None = None
    detected_modifier: str | None = None
    evidence: str


class MePosMappingSuggestResponse(BaseModel):
    pos_code: str
    pos_description: str
    requires_human_review: bool = True
    best_match: MePosMappingMatch | None = None
    alternatives: list[MePosMappingMatch] = Field(default_factory=list)
