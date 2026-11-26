from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP
from typing import Literal
from pydantic import BaseModel, Field


ScalingRule = Literal["LINEAR", "SPICE_NONLINEAR", "SEASONING_STEPPED", "LIQUID_EVAPORATION"]


class ScalingIngredientInput(BaseModel):
    name: str
    category: str = "CORE"
    base_qty: Decimal = Field(ge=Decimal("0"))
    uom: str
    unit_cost: Decimal = Field(ge=Decimal("0"))
    yield_percent: Decimal = Field(default=Decimal("100"), gt=Decimal("0"), le=Decimal("100"))
    scaling_rule: ScalingRule = "LINEAR"
    material_matched: bool = True
    material_code: str | None = None


class RecipeScaleRequest(BaseModel):
    recipe_name: str
    base_servings: Decimal = Field(gt=Decimal("0"))
    target_servings: Decimal = Field(gt=Decimal("0"))
    base_serving_size_grams: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    target_serving_size_grams: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    prep_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    waste_allowance_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    use_nonlinear_rules: bool = True
    ingredients: list[ScalingIngredientInput]


class ScaledIngredientResult(BaseModel):
    name: str
    category: str
    uom: str
    scaling_rule: ScalingRule
    base_qty: Decimal
    scaled_qty: Decimal
    yield_adjusted_qty: Decimal
    unit_cost: Decimal
    base_extended_cost: Decimal
    scaled_extended_cost: Decimal
    material_matched: bool
    material_code: str | None = None


class RecipeScaleResult(BaseModel):
    recipe_name: str
    base_servings: Decimal
    target_servings: Decimal
    linear_factor: Decimal
    base_total_cost: Decimal
    scaled_total_cost: Decimal
    base_cost_per_serving: Decimal
    scaled_cost_per_serving: Decimal
    missing_material_count: int
    ingredients: list[ScaledIngredientResult]


def _q(value: Decimal, places: str = "0.0001") -> Decimal:
    return value.quantize(Decimal(places), rounding=ROUND_HALF_UP)


def _money(value: Decimal) -> Decimal:
    return value.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def _effective_factor(linear_factor: Decimal, rule: ScalingRule, enabled: bool) -> Decimal:
    if not enabled or rule == "LINEAR" or linear_factor == Decimal("1"):
        return linear_factor
    if linear_factor > Decimal("1"):
        # Spices and seasonings scale sub-linearly on batch expansion (power ~0.82); liquids retain ~0.92.
        if rule == "SPICE_NONLINEAR":
            return Decimal(str(round(float(linear_factor) ** 0.82, 6)))
        if rule == "SEASONING_STEPPED":
            return Decimal(str(round(float(linear_factor) ** 0.80, 6)))
        if rule == "LIQUID_EVAPORATION":
            return Decimal(str(round(float(linear_factor) ** 0.92, 6)))
    return linear_factor


def scale_recipe(req: RecipeScaleRequest) -> RecipeScaleResult:
    portion_ratio = req.target_serving_size_grams / req.base_serving_size_grams
    linear_factor = (req.target_servings / req.base_servings) * portion_ratio
    buffer_mult = (Decimal("1") + req.prep_buffer_percent / Decimal("100")) * (
        Decimal("1") + req.waste_allowance_percent / Decimal("100")
    )

    items: list[ScaledIngredientResult] = []
    base_total = Decimal("0")
    scaled_total = Decimal("0")
    missing_count = 0

    for ing in req.ingredients:
        if not ing.material_matched:
            missing_count += 1
        factor = _effective_factor(linear_factor, ing.scaling_rule, req.use_nonlinear_rules)
        raw_scaled = ing.base_qty * factor * buffer_mult
        yield_ratio = ing.yield_percent / Decimal("100")
        # Only apply additional yield expansion if yield_percent < 100 and user requests yield adjustment;
        # base_qty at 100% yield stays equal to scaled_qty so standard recipe quantities are never double-counted.
        yield_adjusted = raw_scaled / yield_ratio
        base_ext = _money(ing.base_qty * ing.unit_cost)
        scaled_ext = _money(yield_adjusted * ing.unit_cost)
        base_total += base_ext
        scaled_total += scaled_ext
        items.append(
            ScaledIngredientResult(
                name=ing.name,
                category=ing.category,
                uom=ing.uom,
                scaling_rule=ing.scaling_rule,
                base_qty=_q(ing.base_qty),
                scaled_qty=_q(raw_scaled),
                yield_adjusted_qty=_q(yield_adjusted),
                unit_cost=_money(ing.unit_cost),
                base_extended_cost=base_ext,
                scaled_extended_cost=scaled_ext,
                material_matched=ing.material_matched,
                material_code=ing.material_code,
            )
        )

    return RecipeScaleResult(
        recipe_name=req.recipe_name,
        base_servings=req.base_servings,
        target_servings=req.target_servings,
        linear_factor=_q(linear_factor),
        base_total_cost=_money(base_total),
        scaled_total_cost=_money(scaled_total),
        base_cost_per_serving=_money(base_total / req.base_servings),
        scaled_cost_per_serving=_money(scaled_total / req.target_servings),
        missing_material_count=missing_count,
        ingredients=items,
    )
