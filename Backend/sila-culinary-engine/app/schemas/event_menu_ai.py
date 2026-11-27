from __future__ import annotations

from decimal import Decimal
from typing import Literal
from pydantic import BaseModel, Field

from app.schemas.recipe_ai import ProviderRuntimeConfig


ServiceStyle = Literal[
    "BUFFET",
    "PLATED",
    "SET_MENU",
    "A_LA_CARTE",
    "LIVE_STATION",
    "COCKTAIL_CANAPES",
    "FAMILY_STYLE",
    "SHARING_PLATTERS",
    "BEVERAGE_RECEPTION",
    "COFFEE_BREAK",
    "PACKED_MEALS",
    "MIXED",
]


class PublishedRecipeCatalogItem(BaseModel):
    recipe_id: str
    recipe_code: str
    recipe_version: int = 1
    name: str
    description: str = ""
    category: str = ""
    cuisine: str = ""
    course_hint: str = "Main Courses"
    base_servings: int = Field(default=10, ge=1)
    serving_size_g: float = Field(default=180.0, gt=0)
    cost_per_portion: float | None = None
    currency: str = "AED"
    dietary_tags: list[str] = Field(default_factory=list)
    verified_allergens: list[str] = Field(default_factory=list)
    unknown_allergens: bool = False
    is_beverage: bool = False
    available_stock_score: float = Field(default=0.5, ge=0.0, le=1.0)
    prep_complexity: float = Field(default=0.5, ge=0.0, le=1.0)
    is_published: bool = True
    property_eligible: bool = True


class SemanticRecipeSearchRequest(BaseModel):
    query: str = ""
    category: str | None = None
    cuisine: str | None = None
    required_dietary_tags: list[str] = Field(default_factory=list)
    excluded_allergens: list[str] = Field(default_factory=list)
    strict_allergen_safety: bool = True
    max_cost_per_portion: float | None = None
    require_published: bool = True
    require_property_eligible: bool = True
    limit: int = Field(default=20, ge=1, le=100)
    catalog: list[PublishedRecipeCatalogItem] = Field(default_factory=list)


class SemanticRecipeSearchMatch(BaseModel):
    recipe_id: str
    recipe_code: str
    recipe_version: int
    name: str
    category: str
    cuisine: str
    course_hint: str
    score: float
    cost_per_portion: float | None = None
    currency: str = "AED"
    dietary_tags: list[str] = Field(default_factory=list)
    verified_allergens: list[str] = Field(default_factory=list)
    unknown_allergens: bool = False
    match_reason: str


class SemanticRecipeSearchResponse(BaseModel):
    results: list[SemanticRecipeSearchMatch]
    filtered_out_due_to_safety: int = 0
    warnings: list[str] = Field(default_factory=list)


class EventMenuLineCalcInput(BaseModel):
    line_id: str
    course_name: str = "Main Courses"
    dish_name: str
    recipe_id: str | None = None
    service_style: ServiceStyle = "BUFFET"
    uptake_percent: float = Field(default=100.0, ge=0.0, le=300.0)
    serving_frequency: float = Field(default=1.0, ge=0.0, le=10.0)
    portion_size_g: float = Field(default=180.0, gt=0.0)
    allocated_portions: int | None = Field(default=None, ge=0)
    manual_portions_override: int | None = Field(default=None, ge=0)
    is_beverage: bool = False
    servings_per_guest: float = Field(default=1.0, ge=0.0, le=20.0)
    serving_volume_ml: float | None = Field(default=None, gt=0.0)
    verified_station_capacity_per_hour: int | None = Field(default=None, ge=1)
    cost_per_portion: float | None = None
    item_role: Literal["MANDATORY", "OPTIONAL", "ALTERNATIVE"] = "MANDATORY"
    session_id: str | None = None
    session_guest_count: int | None = None


class PortionCalculationRequest(BaseModel):
    guaranteed_guests: int = Field(ge=0)
    expected_guests: int = Field(default=0, ge=0)
    additional_allowance_percent: float = Field(default=0.0, ge=0.0, le=100.0)
    billable_policy: Literal["GUARANTEED_ONLY", "EXPECTED_OR_GUARANTEED"] = "GUARANTEED_ONLY"
    net_price_per_guest: float = Field(default=0.0, ge=0.0)
    fixed_package_price: float | None = Field(default=None, ge=0.0)
    target_food_cost_percent: float = Field(default=28.0, gt=0.0, le=100.0)
    menu_budget: float | None = Field(default=None, ge=0.0)
    currency: str = "AED"
    default_service_style: ServiceStyle = "BUFFET"
    round_portions_up: bool = True
    lines: list[EventMenuLineCalcInput] = Field(default_factory=list)


class PortionCalculationLineResult(BaseModel):
    line_id: str
    dish_name: str
    course_name: str
    service_style: str
    effective_guests: int
    planned_portions: int
    edible_quantity_kg: float | None = None
    beverage_liters: float | None = None
    unit_cost_per_portion: float | None = None
    estimated_line_cost: float | None = None
    cost_missing: bool = False
    warnings: list[str] = Field(default_factory=list)


class PortionCalculationResponse(BaseModel):
    guaranteed_guests: int
    planning_guests: int
    billable_guests: int
    currency: str
    estimated_revenue: float
    total_food_cost: float
    total_beverage_cost: float
    total_menu_cost: float
    missing_cost_lines: int
    cost_per_guaranteed_guest: float
    cost_per_planned_guest: float
    menu_cost_percent: float | None = None
    menu_budget: float
    remaining_menu_budget: float
    budget_variance: float
    budget_exceeded: bool
    plated_allocation_valid: bool = True
    warnings: list[str] = Field(default_factory=list)
    lines: list[PortionCalculationLineResult] = Field(default_factory=list)


class MenuOptimizeRequest(BaseModel):
    planning_guests: int = Field(ge=1)
    max_budget: float = Field(gt=0.0)
    required_courses: dict[str, int] = Field(default_factory=dict)
    locked_recipe_ids: list[str] = Field(default_factory=list)
    prohibited_recipe_ids: list[str] = Field(default_factory=list)
    required_dietary_tags: list[str] = Field(default_factory=list)
    excluded_allergens: list[str] = Field(default_factory=list)
    strict_allergen_safety: bool = True
    default_uptake_percent: float = Field(default=100.0, gt=0.0, le=200.0)
    strategy: Literal["LOWEST_COST", "MAX_STOCK", "BALANCED", "PREMIUM_EXPERIENCE", "MIN_COMPLEXITY"] = "BALANCED"
    catalog: list[PublishedRecipeCatalogItem] = Field(default_factory=list)


class OptimizedMenuSelection(BaseModel):
    recipe_id: str
    recipe_code: str
    recipe_version: int
    name: str
    course_name: str
    planned_portions: int
    unit_cost_per_portion: float
    estimated_line_cost: float
    locked: bool = False
    dietary_tags: list[str] = Field(default_factory=list)


class MenuOptimizeResponse(BaseModel):
    feasible: bool
    solver: str = "SCIPY_HIGHS_MILP"
    total_estimated_cost: float = 0.0
    budget: float
    remaining_budget: float = 0.0
    selected_items: list[OptimizedMenuSelection] = Field(default_factory=list)
    conflicting_constraints: list[str] = Field(default_factory=list)
    suggested_adjustments: list[str] = Field(default_factory=list)
    explanation: str = ""


class MenuGenerateRequest(BaseModel):
    event_type: str = "Corporate Gala"
    event_theme: str = ""
    natural_language_brief: str = ""
    guaranteed_guests: int = Field(default=100, ge=1)
    additional_allowance_percent: float = Field(default=0.0, ge=0.0, le=100.0)
    service_style: ServiceStyle = "BUFFET"
    cuisine_preferences: list[str] = Field(default_factory=list)
    required_courses: list[str] = Field(default_factory=lambda: ["Starters", "Main Courses", "Desserts", "Beverages"])
    required_dietary_tags: list[str] = Field(default_factory=list)
    excluded_allergens: list[str] = Field(default_factory=list)
    menu_budget: float | None = None
    target_food_cost_percent: float = 28.0
    strategy: str = "BALANCED"
    allow_missing_recipe_suggestions: bool = True
    locked_recipe_ids: list[str] = Field(default_factory=list)
    catalog: list[PublishedRecipeCatalogItem] = Field(default_factory=list)
    provider_config: ProviderRuntimeConfig | None = None


class GeneratedMenuLineDto(BaseModel):
    course_name: str
    dish_name: str
    recipe_id: str | None = None
    recipe_code: str | None = None
    recipe_version: int | None = None
    line_status: Literal["PUBLISHED", "RECIPE_REQUIRED"] = "PUBLISHED"
    uptake_percent: float = 100.0
    planned_portions: int
    unit_cost_per_portion: float | None = None
    estimated_line_cost: float | None = None
    dietary_tags: list[str] = Field(default_factory=list)
    verified_allergens: list[str] = Field(default_factory=list)
    rationale: str = ""


class MenuGenerateResponse(BaseModel):
    menu_title: str
    menu_description: str
    planning_guests: int
    estimated_total_cost: float
    currency: str = "AED"
    provider_used: str = "LOCAL_DETERMINISTIC"
    model_used: str = "sila-menu-planner-v1"
    clarification_questions: list[str] = Field(default_factory=list)
    budget_warnings: list[str] = Field(default_factory=list)
    dietary_coverage: dict[str, int] = Field(default_factory=dict)
    lines: list[GeneratedMenuLineDto] = Field(default_factory=list)
    missing_recipe_count: int = 0
    explanation: str = ""
