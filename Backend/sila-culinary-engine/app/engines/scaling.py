from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP
from typing import Literal
from pydantic import BaseModel, Field


ScalingRule = Literal["LINEAR", "SPICE_NONLINEAR", "SEASONING_STEPPED", "LIQUID_EVAPORATION"]
ScalingMethodV2 = Literal[
    "LINEAR",
    "FIXED_QUANTITY",
    "BATCH_BASED",
    "SPICE_NONLINEAR",
    "SEASONING_STEPPED",
    "LIQUID_EVAPORATION",
    "CHEF_FACTOR",
    "MANUAL_REVIEW",
]


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


# --- Phase 1A Deterministic Recipe Scaling Engine v2 (§19 & §20) ---


class ScalingIngredientV2Input(BaseModel):
    temp_ref: str
    description: str
    uom: str
    base_qty: Decimal = Field(ge=Decimal("0"))
    quantity_basis: Literal["RAW", "EDIBLE"] = "RAW"
    yield_percent: Decimal = Field(default=Decimal("100"), gt=Decimal("0"), le=Decimal("100"))
    scaling_method: ScalingMethodV2 = "LINEAR"
    chef_scaling_factor: Decimal | None = Field(default=None, gt=Decimal("0"))
    batch_size_servings: Decimal | None = Field(default=None, gt=Decimal("0"))


class RecipeScaleV2Request(BaseModel):
    recipe_name: str
    base_servings: Decimal = Field(gt=Decimal("0"))
    target_servings: Decimal = Field(gt=Decimal("0"))
    base_serving_size: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    target_serving_size: Decimal = Field(default=Decimal("180"), gt=Decimal("0"))
    prep_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    use_nonlinear_rules: bool = True
    ingredients: list[ScalingIngredientV2Input]


class ScaledIngredientV2Result(BaseModel):
    temp_ref: str
    description: str
    uom: str
    scaling_method: ScalingMethodV2
    quantity_basis: Literal["RAW", "EDIBLE"]
    base_qty: Decimal
    scaled_edible_qty: Decimal
    scaled_raw_qty: Decimal
    yield_percent: Decimal
    requires_manual_review: bool
    note: str = ""


class RecipeScaleV2Result(BaseModel):
    recipe_name: str
    base_servings: Decimal
    target_servings: Decimal
    linear_factor: Decimal
    ingredients: list[ScaledIngredientV2Result]


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


def scale_recipe_v2(req: RecipeScaleV2Request) -> RecipeScaleV2Result:
    """
    Phase 1A deterministic scaling always computing directly from the immutable base_qty
    (preventing cumulative rounding drift when target_servings changes repeatedly).
    Applies raw-from-edible yield expansion ONLY when quantity_basis == "EDIBLE" (preventing double yield).
    """
    portion_ratio = req.target_serving_size / req.base_serving_size
    linear_factor = (req.target_servings / req.base_servings) * portion_ratio
    buffer_mult = Decimal("1") + (req.prep_buffer_percent / Decimal("100"))

    results: list[ScaledIngredientV2Result] = []
    for ing in req.ingredients:
        requires_review = False
        note = ""
        method = ing.scaling_method

        if method == "FIXED_QUANTITY":
            factor = Decimal("1")
        elif method == "BATCH_BASED":
            batch_srv = ing.batch_size_servings or req.base_servings
            # Ceiling batch multiplier relative to base batches
            base_batches = (req.base_servings / batch_srv).to_integral_value(rounding="ROUND_CEILING")
            target_batches = (req.target_servings / batch_srv).to_integral_value(rounding="ROUND_CEILING")
            factor = target_batches / max(base_batches, Decimal("1"))
        elif method == "CHEF_FACTOR":
            factor = ing.chef_scaling_factor if ing.chef_scaling_factor is not None else linear_factor
        elif method == "MANUAL_REVIEW":
            factor = linear_factor
            requires_review = True
            note = "Flagged for manual chef review when scaling."
        elif method in ("SPICE_NONLINEAR", "SEASONING_STEPPED", "LIQUID_EVAPORATION"):
            factor = _effective_factor(linear_factor, method, req.use_nonlinear_rules)
            if linear_factor != Decimal("1"):
                requires_review = True
                note = f"Applied {method} scaling curve; chef verification recommended."
        else:
            factor = linear_factor

        scaled_base = ing.base_qty * factor * buffer_mult
        yield_ratio = ing.yield_percent / Decimal("100")

        if ing.quantity_basis == "EDIBLE":
            scaled_edible = scaled_base
            scaled_raw = scaled_base / yield_ratio
        else:
            # Base quantity is already RAW; derive edible without applying yield a second time
            scaled_raw = scaled_base
            scaled_edible = scaled_base * yield_ratio

        results.append(
            ScaledIngredientV2Result(
                temp_ref=ing.temp_ref,
                description=ing.description,
                uom=ing.uom,
                scaling_method=method,
                quantity_basis=ing.quantity_basis,
                base_qty=_q(ing.base_qty),
                scaled_edible_qty=_q(scaled_edible),
                scaled_raw_qty=_q(scaled_raw),
                yield_percent=ing.yield_percent,
                requires_manual_review=requires_review,
                note=note,
            )
        )

    return RecipeScaleV2Result(
        recipe_name=req.recipe_name,
        base_servings=req.base_servings,
        target_servings=req.target_servings,
        linear_factor=_q(linear_factor),
        ingredients=results,
    )
