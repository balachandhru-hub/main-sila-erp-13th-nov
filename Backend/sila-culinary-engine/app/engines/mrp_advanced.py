from __future__ import annotations

import math
import re
from collections import OrderedDict
from datetime import timedelta
from decimal import Decimal, ROUND_HALF_UP
from typing import Any

import numpy as np
from pint import UnitRegistry
from scipy.optimize import Bounds, LinearConstraint, milp

from app.providers.adapters import ClaudeSdkAdapter, GeminiSdkAdapter, LocalOllamaAdapter
from app.providers.router import AiExecutionRequest, AiProviderRouter, ProviderUnavailableError
from app.schemas.mrp_engine import (
    MrpAiAnalyzeOutput,
    MrpAiAnalyzeRequest,
    MrpAiAnalyzeResponse,
    MrpBatchSummary,
    MrpConsolidatedMaterialLine,
    MrpEventInput,
    MrpEventMenuLineInput,
    MrpEventSessionInput,
    MrpExceptionItem,
    MrpExplodeRequest,
    MrpExplodeResponse,
    MrpExplosionTreeNode,
    MrpIngredientInput,
    MrpOptimizedBatchResult,
    MrpOptimizeRequest,
    MrpOptimizeResponse,
    MrpRecipeNodeInput,
    MrpTimeBucketSummary,
    MrpUomConversionRule,
)

_UREG = UnitRegistry()
_Q4 = Decimal("0.0001")
_Q2 = Decimal("0.01")
_HUNDRED = Decimal("100")

# Tenant- and recipe-version-isolated LRU cache (never shared across tenants or versions)
_RECIPE_GRAPH_CACHE: OrderedDict[tuple[str, str, int], dict[str, Any]] = OrderedDict()
_MAX_CACHE_ENTRIES = 512

_CANONICAL_UOM_MAP = {
    "KILO": "KG",
    "KILOGRAM": "KG",
    "KILOGRAMS": "KG",
    "KG": "KG",
    "GRAM": "G",
    "GRAMS": "G",
    "G": "G",
    "MILLIGRAM": "MG",
    "MILLIGRAMS": "MG",
    "MG": "MG",
    "LITRE": "L",
    "LITER": "L",
    "LITRES": "L",
    "LITERS": "L",
    "LT": "L",
    "L": "L",
    "MILLILITRE": "ML",
    "MILLILITER": "ML",
    "MILLILITERS": "ML",
    "MLS": "ML",
    "ML": "ML",
    "EACH": "EA",
    "UNIT": "EA",
    "UNITS": "EA",
    "EA": "EA",
    "PIECE": "PC",
    "PIECES": "PC",
    "PCS": "PC",
    "PC": "PC",
    "BOTTLE": "BTL",
    "BOTTLES": "BTL",
    "BOT": "BTL",
    "BT": "BTL",
    "BTL": "BTL",
}

_MASS_UOMS = {"KG", "G", "MG"}
_VOL_UOMS = {"L", "ML"}

_PROMPT_INJECTION_PATTERNS = [
    re.compile(r"(?i)ignore\s+(all\s+)?previous\s+instructions"),
    re.compile(r"(?i)system\s*prompt"),
    re.compile(r"(?i)execute\s+(po|purchase\s+order|sap|311|reservation)"),
    re.compile(r"(?i)override\s+approval"),
]


def _q4(val: Decimal) -> Decimal:
    return val.quantize(_Q4, rounding=ROUND_HALF_UP)


def _q2(val: Decimal) -> Decimal:
    return val.quantize(_Q2, rounding=ROUND_HALF_UP)


def _canon_uom(uom: str | None) -> str:
    cleaned = (uom or "EA").strip().upper()
    return _CANONICAL_UOM_MAP.get(cleaned, cleaned or "EA")


def _sanitize_untrusted(text: str) -> str:
    clean = text
    for pat in _PROMPT_INJECTION_PATTERNS:
        clean = pat.sub("[REDACTED_INSTRUCTION]", clean)
    return clean.strip()[:1200]


def convert_uom_deterministic(
    quantity: Decimal,
    from_uom: str,
    to_uom: str,
    material_rules: list[MrpUomConversionRule],
) -> tuple[Decimal | None, str | None]:
    """
    Deterministic UOM conversion honoring Material Master rules first, then Pint SI within same physical dimension.
    Never converts mass <-> volume without an explicit Material Master rule.
    """
    src = _canon_uom(from_uom)
    dst = _canon_uom(to_uom)
    if src == dst:
        return quantity, None

    # 1. Explicit Material Master BFS graph (bidirectional)
    adj: dict[str, list[tuple[str, Decimal]]] = {}
    for rule in material_rules:
        if rule.factor <= 0:
            continue
        f = _canon_uom(rule.from_uom)
        t = _canon_uom(rule.to_uom)
        adj.setdefault(f, []).append((t, rule.factor))
        adj.setdefault(t, []).append((f, Decimal("1") / rule.factor))

    # Include SI mass and volume edges inside same dimension
    si_edges = [
        ("KG", "G", Decimal("1000")),
        ("G", "MG", Decimal("1000")),
        ("L", "ML", Decimal("1000")),
    ]
    for f, t, factor in si_edges:
        adj.setdefault(f, []).append((t, factor))
        adj.setdefault(t, []).append((f, Decimal("1") / factor))

    visited: set[str] = {src}
    queue: list[tuple[str, Decimal]] = [(src, Decimal("1"))]
    while queue:
        cur, mult = queue.pop(0)
        if cur == dst:
            return quantity * mult, None
        for nxt, edge_factor in adj.get(cur, []):
            if nxt not in visited:
                visited.add(nxt)
                queue.append((nxt, mult * edge_factor))

    # Reject cross-dimension mass <-> volume when no material conversion exists
    if (src in _MASS_UOMS and dst in _VOL_UOMS) or (src in _VOL_UOMS and dst in _MASS_UOMS):
        return None, f"Mass-to-volume conversion ({src} -> {dst}) requires Material Master conversion."

    # 2. Pint fallback strictly within matching dimensionality
    pint_aliases = {"KG": "kilogram", "G": "gram", "MG": "milligram", "L": "liter", "ML": "milliliter"}
    if src in pint_aliases and dst in pint_aliases:
        try:
            q_pint = float(quantity) * _UREG(pint_aliases[src])
            converted = q_pint.to(pint_aliases[dst]).magnitude
            return Decimal(str(converted)), None
        except Exception:
            pass

    return None, f"Missing Material Master UOM conversion from {src} to {dst}."


def _compute_scaling_multiplier(
    ing: MrpIngredientInput,
    raw_factor: Decimal,
    planned_portions: Decimal,
) -> Decimal:
    method = ing.scaling_method
    if raw_factor <= 0:
        return Decimal("0")
    if method == "FIXED_QUANTITY":
        return Decimal("1")
    if method == "CHEF_FACTOR" and ing.chef_scaling_factor is not None:
        return ing.chef_scaling_factor
    if method == "BATCH_BASED" and ing.batch_step_size and ing.batch_step_size > 0:
        batches = Decimal(math.ceil(float(planned_portions / ing.batch_step_size)))
        return batches
    if method == "SPICE_NONLINEAR":
        exp = 0.85 if raw_factor > 1 else 0.90
        return Decimal(str(math.pow(float(raw_factor), exp)))
    if method == "SEASONING_STEPPED":
        exp = 0.80 if raw_factor > 1 else 0.90
        return Decimal(str(math.pow(float(raw_factor), exp)))
    if method == "LIQUID_EVAPORATION":
        exp = 0.92 if raw_factor > 1 else 0.95
        return Decimal(str(math.pow(float(raw_factor), exp)))
    if method == "NONLINEAR":
        exp = 0.88 if raw_factor > 1 else 0.92
        return Decimal(str(math.pow(float(raw_factor), exp)))
    return raw_factor


def _resolve_effective_buffer_percent(
    event_buf: Decimal,
    session_buf: Decimal,
    recipe_buf: Decimal,
    ing_buf: Decimal,
    baseline_includes_allowance: bool,
    policy: str,
) -> Decimal:
    if policy == "ADDITIVE_EXPLICIT":
        ev_part = Decimal("0") if baseline_includes_allowance else event_buf
        return ev_part + session_buf + recipe_buf + ing_buf

    # PRECEDENCE: most specific non-zero buffer wins so buffers never double-count silently
    if ing_buf > 0:
        return ing_buf
    if recipe_buf > 0:
        return recipe_buf
    if session_buf > 0:
        return session_buf
    if event_buf > 0 and not baseline_includes_allowance:
        return event_buf
    return Decimal("0")


def _compute_combined_yield_fraction(
    ing: MrpIngredientInput,
    recipe: MrpRecipeNodeInput,
) -> tuple[Decimal | None, str | None]:
    """
    Sequential yield calculation (prep yield * cooking yield * usable yield).
    Never divides by zero and never double-applies yield when quantity_basis == RAW.
    """
    if ing.quantity_basis == "RAW":
        return Decimal("1"), None

    usable = ing.usable_yield_percent
    prep = ing.prep_yield_percent
    cook = ing.cooking_yield_percent

    # Fall back to recipe-level losses only if ingredient-level prep/cook are 100%
    if prep == Decimal("100") and recipe.trim_loss_percent > 0:
        prep = Decimal("100") - recipe.trim_loss_percent
    if cook == Decimal("100") and recipe.cooking_loss_percent > 0:
        cook = Decimal("100") - recipe.cooking_loss_percent

    if usable <= 0 or prep <= 0 or cook <= 0:
        return None, f"Invalid zero/negative yield on ingredient '{ing.item_name}'."

    combined = (usable / _HUNDRED) * (prep / _HUNDRED) * (cook / _HUNDRED)
    if combined <= 0:
        return None, f"Combined sequential yield is zero on ingredient '{ing.item_name}'."
    return combined, None


def _compute_planned_guests(ev: MrpEventInput) -> int:
    # Prevent double counting allowance if baseline already includes allowance
    if ev.baseline_includes_allowance:
        return max(ev.guaranteed_guests, ev.expected_guests)
    allowed = math.ceil(float(Decimal(ev.guaranteed_guests) * (Decimal("1") + ev.additional_allowance_percent / _HUNDRED)))
    return max(allowed, ev.expected_guests)


def _compute_line_portions(ev: MrpEventInput, session: MrpEventSessionInput | None, line: MrpEventMenuLineInput) -> Decimal:
    if line.manual_portion_override is not None:
        return line.manual_portion_override
    guests = Decimal(session.planned_guests if session and session.planned_guests is not None else _compute_planned_guests(ev))
    uptake = line.uptake_percent / _HUNDRED
    replenish = Decimal("1") + (line.buffet_replenishment_percent / _HUNDRED)
    return guests * uptake * line.serving_frequency * replenish


def _cache_key(tenant_id: str, recipe_id: str, version: int) -> tuple[str, str, int]:
    return (tenant_id, recipe_id, version)


def _get_cached_recipe_meta(tenant_id: str, recipe: MrpRecipeNodeInput) -> dict[str, Any]:
    key = _cache_key(tenant_id, recipe.recipe_id, recipe.recipe_version)
    if key in _RECIPE_GRAPH_CACHE:
        _RECIPE_GRAPH_CACHE.move_to_end(key)
        return {"hit": True, "data": _RECIPE_GRAPH_CACHE[key]}

    meta = {
        "recipe_id": recipe.recipe_id,
        "recipe_version": recipe.recipe_version,
        "ingredient_count": len(recipe.ingredients),
        "sub_recipe_ids": [i.sub_recipe_id for i in recipe.ingredients if i.is_sub_recipe and i.sub_recipe_id],
    }
    _RECIPE_GRAPH_CACHE[key] = meta
    if len(_RECIPE_GRAPH_CACHE) > _MAX_CACHE_ENTRIES:
        _RECIPE_GRAPH_CACHE.popitem(last=False)
    return {"hit": False, "data": meta}


def explode_mrp(req: MrpExplodeRequest) -> MrpExplodeResponse:
    exceptions: list[MrpExceptionItem] = []
    seen_event_revs: set[tuple[str, int]] = set()
    recipe_map: dict[tuple[str, int], MrpRecipeNodeInput] = {}
    recipe_latest_fallback: dict[str, MrpRecipeNodeInput] = {}
    for r in req.recipes:
        recipe_map[(r.recipe_id, r.recipe_version)] = r
        recipe_latest_fallback[r.recipe_id] = r

    cache_hits = 0
    cache_misses = 0
    circular_detected = False
    nodes_visited = 0

    consolidated: dict[str, MrpConsolidatedMaterialLine] = {}
    batch_tracker: dict[tuple[str, int], MrpBatchSummary] = {}
    explosion_tree: list[MrpExplosionTreeNode] = []
    unique_recipes_used: set[tuple[str, int]] = set()

    def record_exception(
        code: str,
        exc_type: str,
        severity: str,
        impact: str,
        action: str,
        event_id: str | None = None,
        event_code: str | None = None,
        recipe_id: str | None = None,
        recipe_code: str | None = None,
        material_code: str | None = None,
        material_name: str | None = None,
        cycle_path: list[str] | None = None,
    ) -> None:
        exceptions.append(
            MrpExceptionItem(
                exception_code=code,
                exception_type=exc_type,
                severity=severity,  # type: ignore[arg-type]
                event_id=event_id,
                event_code=event_code,
                recipe_id=recipe_id,
                recipe_code=recipe_code,
                material_code=material_code,
                material_name=material_name,
                calculation_impact=impact,
                recommended_action=action,
                cycle_path=cycle_path or [],
            )
        )

    def explode_recipe_node(
        ev: MrpEventInput,
        session: MrpEventSessionInput | None,
        line: MrpEventMenuLineInput,
        recipe_id: str,
        recipe_version: int,
        required_portions_or_qty: Decimal,
        is_sub_recipe_call: bool,
        sub_qty_uom: str | None,
        ancestry: list[tuple[str, int, str]],
        depth: int,
        kitchen_id: str | None,
        kitchen_name: str,
        required_by_utc: datetime,
    ) -> MrpExplosionTreeNode | None:
        nonlocal cache_hits, cache_misses, circular_detected, nodes_visited

        nodes_visited += 1
        if nodes_visited > req.max_nodes:
            record_exception(
                "MRP-NODE-LIMIT",
                "RECIPE_DEPENDENCY_LIMIT",
                "BLOCKING",
                f"Exceeded maximum explosion node limit ({req.max_nodes}).",
                "Simplify nested sub-recipe graph or increase configured limit.",
                event_id=ev.event_id,
                event_code=ev.event_code,
                recipe_id=recipe_id,
            )
            return None

        if depth > req.max_depth:
            record_exception(
                "MRP-DEPTH-LIMIT",
                "RECIPE_DEPENDENCY_LIMIT",
                "BLOCKING",
                f"Exceeded maximum recipe nesting depth ({req.max_depth}).",
                "Flatten sub-recipe nesting below maximum depth.",
                event_id=ev.event_id,
                event_code=ev.event_code,
                recipe_id=recipe_id,
            )
            return None

        # Cycle check
        for a_id, a_ver, a_label in ancestry:
            if a_id == recipe_id:
                circular_detected = True
                cycle_labels = [x[2] for x in ancestry] + [f"{a_label}"]
                record_exception(
                    "MRP-CYCLE",
                    "CIRCULAR_RECIPE_DEPENDENCY",
                    "BLOCKING",
                    f"Circular recipe dependency detected: {' -> '.join(cycle_labels)}.",
                    "Break circular sub-recipe reference in Recipe Master.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe_id,
                    cycle_path=cycle_labels,
                )
                return MrpExplosionTreeNode(
                    node_id=f"cycle-{recipe_id}-{depth}",
                    node_type="SUB_RECIPE" if is_sub_recipe_call else "RECIPE",
                    reference_code=recipe_id,
                    description=f"CIRCULAR DEPENDENCY: {' -> '.join(cycle_labels)}",
                    recipe_id=recipe_id,
                    recipe_version=recipe_version,
                    required_qty=_q4(required_portions_or_qty),
                    uom=sub_qty_uom or "PORTION",
                    dependency_level=depth,
                    validation_status="CIRCULAR_DEPENDENCY",
                )

        # Exact version resolution — NEVER auto-upgrade to latest when pinned version missing
        recipe = recipe_map.get((recipe_id, recipe_version))
        if recipe is None:
            record_exception(
                "MRP-MISSING-REC-VER",
                "MISSING_RECIPE_VERSION",
                "BLOCKING",
                f"Pinned recipe {recipe_id} v{recipe_version} was not found.",
                "Publish or restore the pinned recipe version or update the event menu baseline.",
                event_id=ev.event_id,
                event_code=ev.event_code,
                recipe_id=recipe_id,
            )
            return None

        if not recipe.is_published:
            record_exception(
                "MRP-UNPUBLISHED-REC",
                "UNPUBLISHED_RECIPE",
                "BLOCKING",
                f"Recipe {recipe.recipe_code or recipe.recipe_name} v{recipe.recipe_version} is not published.",
                "Publish recipe before releasing operational MRP.",
                event_id=ev.event_id,
                event_code=ev.event_code,
                recipe_id=recipe.recipe_id,
                recipe_code=recipe.recipe_code,
            )

        c_res = _get_cached_recipe_meta(req.tenant_id, recipe)
        if c_res["hit"]:
            cache_hits += 1
        else:
            cache_misses += 1

        unique_recipes_used.add((recipe.recipe_id, recipe.recipe_version))
        rec_label = f"{recipe.recipe_code or recipe.recipe_name} v{recipe.recipe_version}"

        # Batch calculation for sub-recipes / batch recipes
        scale_factor = Decimal("1")
        planned_production_qty = required_portions_or_qty
        if is_sub_recipe_call:
            if recipe.batch_yield is None or recipe.batch_yield <= 0:
                record_exception(
                    "MRP-INVALID-BATCH-YIELD",
                    "MISSING_BATCH_YIELD",
                    "BLOCKING",
                    f"Sub-recipe {rec_label} has missing or zero batch yield.",
                    "Maintain valid batch yield on sub-recipe.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                )
                return None

            converted_sub_req, conv_err = convert_uom_deterministic(
                required_portions_or_qty,
                sub_qty_uom or recipe.batch_yield_uom,
                recipe.batch_yield_uom,
                [],
            )
            if converted_sub_req is None:
                record_exception(
                    "MRP-SUB-UOM",
                    "INVALID_UOM_CONVERSION",
                    "BLOCKING",
                    conv_err or f"Cannot convert sub-recipe demand to {recipe.batch_yield_uom}.",
                    "Align sub-recipe demand UOM with batch yield UOM.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                )
                return None

            gross_sub_qty = converted_sub_req
            prepared_avail = recipe.prepared_stock_qty if req.prepared_stock_mode == "ADJUSTED_SCENARIO" else Decimal("0")
            new_prod_qty = max(Decimal("0"), gross_sub_qty - prepared_avail)

            raw_batches = new_prod_qty / recipe.batch_yield
            req_batches = raw_batches if recipe.allow_partial_batches else Decimal(math.ceil(float(raw_batches)))
            if recipe.min_batch_size and req_batches > 0 and (req_batches * recipe.batch_yield) < recipe.min_batch_size:
                req_batches = recipe.min_batch_size / recipe.batch_yield
                if not recipe.allow_partial_batches:
                    req_batches = Decimal(math.ceil(float(req_batches)))

            planned_output = req_batches * recipe.batch_yield
            excess_output = max(Decimal("0"), planned_output - new_prod_qty)
            scale_factor = req_batches
            planned_production_qty = new_prod_qty

            b_key = (recipe.recipe_id, recipe.recipe_version)
            if b_key not in batch_tracker:
                batch_tracker[b_key] = MrpBatchSummary(
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                    recipe_name=recipe.recipe_name,
                    recipe_version=recipe.recipe_version,
                    required_qty=_q4(gross_sub_qty),
                    batch_yield=_q4(recipe.batch_yield),
                    batch_yield_uom=_canon_uom(recipe.batch_yield_uom),
                    allow_partial_batches=recipe.allow_partial_batches,
                    required_batches=_q4(req_batches),
                    planned_output_qty=_q4(planned_output),
                    excess_output_qty=_q4(excess_output),
                    prepared_stock_qty=_q4(prepared_avail),
                    indicative_new_production_qty=_q4(new_prod_qty),
                )
            else:
                prev = batch_tracker[b_key]
                tot_gross = prev.required_qty + gross_sub_qty
                tot_new = max(Decimal("0"), tot_gross - prev.prepared_stock_qty)
                tot_b = (tot_new / recipe.batch_yield) if recipe.allow_partial_batches else Decimal(math.ceil(float(tot_new / recipe.batch_yield)))
                tot_out = tot_b * recipe.batch_yield
                prev.required_qty = _q4(tot_gross)
                prev.indicative_new_production_qty = _q4(tot_new)
                prev.required_batches = _q4(tot_b)
                prev.planned_output_qty = _q4(tot_out)
                prev.excess_output_qty = _q4(max(Decimal("0"), tot_out - tot_new))
        else:
            # Top-level menu recipe scaling (portions * serving size adjustment)
            serving_ratio = Decimal("1")
            if line.event_serving_size_grams is not None and line.standard_serving_size_grams > 0:
                serving_ratio = line.event_serving_size_grams / line.standard_serving_size_grams
            scale_factor = (required_portions_or_qty / recipe.base_servings) * serving_ratio

        # Time-phased required-by timestamp for this recipe's ingredients
        lead_hours = recipe.marination_lead_hours + Decimal(recipe.prep_time_minutes + recipe.cook_time_minutes) / Decimal("60")
        recipe_req_by = required_by_utc - timedelta(hours=float(lead_hours))

        node = MrpExplosionTreeNode(
            node_id=f"rec-{recipe.recipe_id}-v{recipe.recipe_version}-d{depth}-{nodes_visited}",
            node_type="SUB_RECIPE" if is_sub_recipe_call else "RECIPE",
            reference_code=recipe.recipe_code or recipe.recipe_id,
            description=f"{recipe.recipe_name} (v{recipe.recipe_version})",
            recipe_id=recipe.recipe_id,
            recipe_version=recipe.recipe_version,
            required_qty=_q4(required_portions_or_qty),
            uom=sub_qty_uom or "PORTION",
            scaling_factor=_q4(scale_factor),
            dependency_level=depth,
            validation_status="VALID" if recipe.is_published else "UNPUBLISHED_RECIPE",
        )

        new_ancestry = ancestry + [(recipe.recipe_id, recipe.recipe_version, rec_label)]
        for ing in recipe.ingredients:
            ing_scale = _compute_scaling_multiplier(ing, scale_factor, required_portions_or_qty)
            scaled_qty = ing.quantity * ing_scale
            if ing.min_quantity is not None and scaled_qty < ing.min_quantity:
                scaled_qty = ing.min_quantity
            if ing.max_quantity is not None and scaled_qty > ing.max_quantity:
                scaled_qty = ing.max_quantity

            eff_buf = _resolve_effective_buffer_percent(
                ev.event_buffer_percent,
                session.session_buffer_percent if session else Decimal("0"),
                recipe.recipe_buffer_percent,
                ing.ingredient_buffer_percent,
                ev.baseline_includes_allowance,
                req.buffer_policy,
            )
            buffered_qty = scaled_qty * (Decimal("1") + eff_buf / _HUNDRED)

            # Nested sub-recipe recursion
            if ing.is_sub_recipe and ing.sub_recipe_id:
                sub_ver = ing.sub_recipe_version or recipe_latest_fallback.get(ing.sub_recipe_id, MrpRecipeNodeInput(
                    recipe_id=ing.sub_recipe_id, recipe_name=ing.item_name
                )).recipe_version
                child_node = explode_recipe_node(
                    ev=ev,
                    session=session,
                    line=line,
                    recipe_id=ing.sub_recipe_id,
                    recipe_version=sub_ver,
                    required_portions_or_qty=buffered_qty,
                    is_sub_recipe_call=True,
                    sub_qty_uom=ing.uom,
                    ancestry=new_ancestry,
                    depth=depth + 1,
                    kitchen_id=kitchen_id,
                    kitchen_name=kitchen_name,
                    required_by_utc=recipe_req_by,
                )
                if child_node is not None:
                    node.children.append(child_node)
                continue

            # Raw/Edible Yield conversion
            yield_frac, yield_err = _compute_combined_yield_fraction(ing, recipe)
            if yield_frac is None:
                record_exception(
                    "MRP-INVALID-YIELD",
                    "INVALID_YIELD",
                    "BLOCKING",
                    yield_err or "Invalid yield percentage.",
                    "Set usable, prep, and cooking yields > 0% and <= 100%.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )
                continue

            raw_qty_in_ing_uom = buffered_qty / yield_frac

            # UOM conversion to Material base UOM
            target_uom = _canon_uom(ing.base_uom or ing.uom)
            converted_gross, uom_err = convert_uom_deterministic(raw_qty_in_ing_uom, ing.uom, target_uom, ing.uom_conversions)
            converted_edible, _ = convert_uom_deterministic(scaled_qty, ing.uom, target_uom, ing.uom_conversions)

            is_blocking = False
            val_status = "REQUIREMENT_CALCULATED"

            if converted_gross is None:
                is_blocking = True
                val_status = "UOM_CONVERSION_MISSING"
                record_exception(
                    "MRP-UOM-MISSING",
                    "MISSING_UOM_CONVERSION",
                    "BLOCKING",
                    uom_err or f"No UOM conversion from {ing.uom} to {target_uom} for {ing.item_name}.",
                    "Raise Phase 1C UOM Maintenance Request in Material Intelligence.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )
                converted_gross = raw_qty_in_ing_uom
                converted_edible = scaled_qty
                target_uom = _canon_uom(ing.uom)

            resolved = bool(ing.material_id or ing.material_code)
            if not resolved:
                is_blocking = True
                val_status = "MATERIAL_NOT_MAPPED"
                record_exception(
                    "MRP-MAT-MISSING",
                    "MISSING_MATERIAL",
                    "BLOCKING",
                    f"Ingredient '{ing.item_name}' in {rec_label} has no Material Master mapping.",
                    "Raise Phase 1C Material Creation Request or link Material Master in Phase 1B.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                    material_name=ing.item_name,
                )
            elif not ing.material_active:
                is_blocking = True
                val_status = "MATERIAL_INACTIVE"
                record_exception(
                    "MRP-MAT-INACTIVE",
                    "INACTIVE_MATERIAL",
                    "BLOCKING",
                    f"Material {ing.material_code} ({ing.item_name}) is inactive.",
                    "Reactivate material or replace with approved substitute.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )
            elif not ing.extended_to_plant:
                is_blocking = True
                val_status = "MATERIAL_NOT_EXTENDED"
                record_exception(
                    "MRP-PLANT-EXT",
                    "MATERIAL_NOT_EXTENDED",
                    "BLOCKING",
                    f"Material {ing.material_code} is not extended to plant {ev.plant_code or 'default'}.",
                    "Raise Material Extension request in Phase 1C.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    recipe_id=recipe.recipe_id,
                    recipe_code=recipe.recipe_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )

            ing_req_by = recipe_req_by - timedelta(hours=float(ing.prep_lead_hours))
            bucket_str = ing_req_by.strftime("%Y-%m-%d")
            storage_loc = (ing.stock.storage_location_code if ing.stock else None) or ev.storage_location_code or "DEFAULT"
            mat_key = ing.material_id or (f"CODE:{ing.material_code}" if ing.material_code else f"UNMAPPED:{ing.item_name.upper()}")
            c_key = f"{mat_key}|{ev.plant_code or ''}|{storage_loc}|{bucket_str}|{target_uom}"

            sub_path_str = " -> ".join([x[2] for x in new_ancestry])
            node.children.append(
                MrpExplosionTreeNode(
                    node_id=f"mat-{ing.line_ref}-{nodes_visited}",
                    node_type="MATERIAL",
                    reference_code=ing.material_code or "UNMAPPED",
                    description=ing.material_description or ing.item_name,
                    material_code=ing.material_code,
                    required_qty=_q4(converted_gross),
                    uom=target_uom,
                    yield_percent=_q2(yield_frac * _HUNDRED),
                    scaling_factor=_q4(ing_scale),
                    dependency_level=depth + 1,
                    validation_status=val_status,
                )
            )

            stock = ing.stock
            prep_stock = stock.prepared_stock_qty if (stock and req.prepared_stock_mode == "ADJUSTED_SCENARIO") else Decimal("0")
            if c_key not in consolidated:
                consolidated[c_key] = MrpConsolidatedMaterialLine(
                    consolidation_key=c_key,
                    material_id=ing.material_id,
                    material_code=ing.material_code,
                    material_description=ing.material_description or ing.item_name,
                    material_category=ing.material_category,
                    is_resolved_material=resolved,
                    plant_code=ev.plant_code,
                    kitchen_location_id=kitchen_id,
                    kitchen_name=kitchen_name,
                    storage_location_code=storage_loc,
                    required_date_utc=ing_req_by,
                    required_date_bucket=bucket_str,
                    base_uom=target_uom,
                    edible_qty=_q4(converted_edible or Decimal("0")),
                    raw_qty=_q4(converted_gross),
                    gross_qty=_q4(converted_gross),
                    prepared_stock_qty=_q4(prep_stock),
                    indicative_new_production_qty=_q4(max(Decimal("0"), converted_gross - prep_stock)),
                    eligible_stock_qty=stock.eligible_qty if stock else None,
                    incoming_po_qty=stock.incoming_po_qty if stock else Decimal("0"),
                    unit_cost=ing.unit_cost,
                    currency=ing.currency or ev.currency,
                    cost_status="VERIFIED" if ing.unit_cost is not None else "COST_UNAVAILABLE",
                    validation_status=val_status,
                    is_blocking=is_blocking,
                    contributing_recipes=[rec_label],
                    contributing_events=[ev.event_code],
                    contributing_sessions=[session.session_name] if session else ["Main"],
                    sub_recipe_paths=[sub_path_str],
                    exclusion_reasons=list(stock.exclusion_reasons) if stock else [],
                )
            else:
                existing = consolidated[c_key]
                existing.edible_qty = _q4(existing.edible_qty + (converted_edible or Decimal("0")))
                existing.raw_qty = _q4(existing.raw_qty + converted_gross)
                existing.gross_qty = _q4(existing.gross_qty + converted_gross)
                existing.indicative_new_production_qty = _q4(max(Decimal("0"), existing.gross_qty - existing.prepared_stock_qty))
                existing.is_blocking = existing.is_blocking or is_blocking
                if rec_label not in existing.contributing_recipes:
                    existing.contributing_recipes.append(rec_label)
                if ev.event_code not in existing.contributing_events:
                    existing.contributing_events.append(ev.event_code)
                if session and session.session_name not in existing.contributing_sessions:
                    existing.contributing_sessions.append(session.session_name)
                if sub_path_str not in existing.sub_recipe_paths:
                    existing.sub_recipe_paths.append(sub_path_str)

            # Stock/Price governance exceptions (warn only; shortages do not block procurement planning)
            if stock and stock.expired_qty > 0:
                record_exception(
                    "MRP-EXPIRED-STOCK",
                    "EXPIRED_STOCK_EXCLUDED",
                    "WARNING",
                    f"Excluded {stock.expired_qty} {target_uom} expired stock for {ing.item_name}.",
                    "Inspect physical stock and exclude expired batches.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )
            if stock and stock.is_stale:
                record_exception(
                    "MRP-STALE-STOCK",
                    "STALE_INVENTORY_DATA",
                    "WARNING",
                    f"Inventory timestamp for {ing.item_name} is stale.",
                    "Run stock count or sync live inventory before procurement.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )
            if resolved and ing.unit_cost is None:
                record_exception(
                    "MRP-MISSING-COST",
                    "MISSING_PRICE",
                    "WARNING",
                    f"Material {ing.material_code or ing.item_name} has no verified unit cost.",
                    "Maintain verified price in Material Master / Phase 1C Costing.",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                    material_code=ing.material_code,
                    material_name=ing.item_name,
                )

        return node

    # Traverse events, preventing duplicate event+revision inclusion
    for ev in req.events:
        ev_key = (ev.event_id, ev.event_revision)
        if ev_key in seen_event_revs:
            record_exception(
                "MRP-DUP-EVENT",
                "DUPLICATE_EVENT_REVISION",
                "WARNING",
                f"Duplicate inclusion of {ev.event_code} Rev {ev.event_revision} ignored.",
                "Select distinct events/revisions for multi-event consolidation.",
                event_id=ev.event_id,
                event_code=ev.event_code,
            )
            continue
        seen_event_revs.add(ev_key)

        ev_node = MrpExplosionTreeNode(
            node_id=f"ev-{ev.event_id}",
            node_type="EVENT",
            reference_code=ev.event_code,
            description=f"{ev.event_name} (Rev {ev.event_revision})",
            required_qty=Decimal(_compute_planned_guests(ev)),
            uom="GUEST",
            dependency_level=0,
            validation_status=ev.approval_status,
        )

        sessions_by_id = {s.session_id: s for s in ev.sessions}
        session_buckets: dict[str, MrpExplosionTreeNode] = {}

        # Validate mutually exclusive plated allocations if applicable
        plated_lines = [l for l in ev.menu_lines if l.service_style.upper() == "PLATED"]
        if plated_lines:
            by_course_uptake: dict[str, Decimal] = {}
            for pl in plated_lines:
                by_course_uptake[pl.course_name] = by_course_uptake.get(pl.course_name, Decimal("0")) + pl.uptake_percent
            for c_name, sum_up in by_course_uptake.items():
                if sum_up > Decimal("100.01"):
                    record_exception(
                        "MRP-PLATED-UPTAKE",
                        "INVALID_PLATED_ALLOCATION",
                        "WARNING",
                        f"Plated course '{c_name}' total uptake ({sum_up}%) exceeds 100%.",
                        "Adjust plated uptake percentages so mutually exclusive choices total <= 100%.",
                        event_id=ev.event_id,
                        event_code=ev.event_code,
                    )

        for ml in ev.menu_lines:
            sess = sessions_by_id.get(ml.session_id) if ml.session_id else None
            sess_key = sess.session_id if sess else "__DEFAULT__"
            if sess_key not in session_buckets:
                s_node = MrpExplosionTreeNode(
                    node_id=f"sess-{ev.event_id}-{sess_key}",
                    node_type="SESSION",
                    reference_code=sess.session_name if sess else "Main Service",
                    description=sess.session_name if sess else "Event Service",
                    required_qty=Decimal(sess.planned_guests if sess and sess.planned_guests else _compute_planned_guests(ev)),
                    uom="GUEST",
                    dependency_level=1,
                )
                session_buckets[sess_key] = s_node
                ev_node.children.append(s_node)

            portions = _compute_line_portions(ev, sess, ml)
            if not ml.recipe_id or ml.recipe_version is None:
                record_exception(
                    "MRP-MISSING-RECIPE-REF",
                    "MISSING_RECIPE",
                    "BLOCKING",
                    f"Menu line '{ml.dish_name}' is not linked to a published recipe version.",
                    "Link published recipe version in Menu Studio (Phase 2A).",
                    event_id=ev.event_id,
                    event_code=ev.event_code,
                )
                continue

            req_dt = sess.session_date_utc if sess else ev.event_date_utc
            k_id = (sess.kitchen_location_id if sess else None) or ev.kitchen_location_id
            k_name = (sess.kitchen_name if sess else None) or ev.kitchen_name
            r_node = explode_recipe_node(
                ev=ev,
                session=sess,
                line=ml,
                recipe_id=ml.recipe_id,
                recipe_version=ml.recipe_version,
                required_portions_or_qty=portions,
                is_sub_recipe_call=False,
                sub_qty_uom=None,
                ancestry=[],
                depth=2,
                kitchen_id=k_id,
                kitchen_name=k_name,
                required_by_utc=req_dt,
            )
            if r_node is not None:
                session_buckets[sess_key].children.append(r_node)

        explosion_tree.append(ev_node)

    # Finalize shortage, costing & status per consolidated line
    lines_out: list[MrpConsolidatedMaterialLine] = []
    sufficient_cnt = 0
    shortage_cnt = 0
    unknown_cnt = 0
    missing_map_cnt = 0
    total_req_val = Decimal("0")
    total_short_val = Decimal("0")
    has_any_cost = False
    has_any_shortage_cost = False

    time_buckets: dict[tuple[str, str], MrpTimeBucketSummary] = {}

    for line in consolidated.values():
        demand_basis = line.indicative_new_production_qty if req.prepared_stock_mode == "ADJUSTED_SCENARIO" else line.gross_qty
        if line.eligible_stock_qty is not None:
            shortage = _q4(max(Decimal("0"), demand_basis - line.eligible_stock_qty))
            rem_shortage = _q4(max(Decimal("0"), demand_basis - line.eligible_stock_qty - line.incoming_po_qty))
            line.indicative_current_shortage_qty = shortage
            line.indicative_remaining_shortage_qty = rem_shortage

            if not line.is_blocking:
                if shortage == 0:
                    line.validation_status = "CURRENT_STOCK_SUFFICIENT"
                    sufficient_cnt += 1
                elif line.eligible_stock_qty == 0:
                    line.validation_status = "NO_STOCK"
                    shortage_cnt += 1
                else:
                    line.validation_status = "POTENTIAL_SHORTAGE"
                    shortage_cnt += 1
        else:
            unknown_cnt += 1
            if not line.is_blocking:
                line.validation_status = "AVAILABILITY_UNKNOWN"

        if not line.is_resolved_material:
            missing_map_cnt += 1

        if line.unit_cost is not None:
            req_val = _q2(demand_basis * line.unit_cost)
            line.requirement_value = req_val
            total_req_val += req_val
            has_any_cost = True
            if line.indicative_current_shortage_qty is not None and line.indicative_current_shortage_qty > 0:
                sh_val = _q2(line.indicative_current_shortage_qty * line.unit_cost)
                line.indicative_shortage_value = sh_val
                total_short_val += sh_val
                has_any_shortage_cost = True

        lines_out.append(line)

        tb_key = (line.required_date_bucket, line.kitchen_name)
        if tb_key not in time_buckets:
            time_buckets[tb_key] = MrpTimeBucketSummary(
                bucket_date=line.required_date_bucket,
                kitchen_name=line.kitchen_name,
                material_count=1,
                total_requirement_value=line.requirement_value,
                shortage_material_count=1 if (line.indicative_current_shortage_qty or 0) > 0 else 0,
                earliest_required_by_utc=line.required_date_utc,
            )
        else:
            tb = time_buckets[tb_key]
            tb.material_count += 1
            if line.requirement_value is not None:
                tb.total_requirement_value = _q2((tb.total_requirement_value or Decimal("0")) + line.requirement_value)
            if (line.indicative_current_shortage_qty or 0) > 0:
                tb.shortage_material_count += 1
            if line.required_date_utc < tb.earliest_required_by_utc:
                tb.earliest_required_by_utc = line.required_date_utc

    blocking_cnt = sum(1 for e in exceptions if e.severity == "BLOCKING")
    warning_cnt = sum(1 for e in exceptions if e.severity == "WARNING")
    currency = req.events[0].currency if req.events else "AED"

    return MrpExplodeResponse(
        mrp_run_id=req.mrp_run_id,
        total_events=len(seen_event_revs),
        total_recipes=len(unique_recipes_used),
        total_unique_materials=len(lines_out),
        sufficient_stock_count=sufficient_cnt,
        shortage_count=shortage_cnt,
        unknown_stock_count=unknown_cnt,
        missing_mapping_count=missing_map_cnt,
        blocking_exception_count=blocking_cnt,
        warning_exception_count=warning_cnt,
        estimated_requirement_value=_q2(total_req_val) if has_any_cost else None,
        indicative_shortage_value=_q2(total_short_val) if has_any_shortage_cost else None,
        currency=currency,
        circular_dependency_detected=circular_detected,
        cache_hits=cache_hits,
        cache_misses=cache_misses,
        explosion_tree=explosion_tree,
        consolidated_materials=sorted(lines_out, key=lambda x: (x.required_date_bucket, x.material_description)),
        batch_summaries=list(batch_tracker.values()),
        time_phased_buckets=sorted(time_buckets.values(), key=lambda x: x.bucket_date),
        exceptions=exceptions,
    )


def optimize_mrp_batches(req: MrpOptimizeRequest) -> MrpOptimizeResponse:
    """
    HiGHS MILP batch optimization (scipy.optimize.milp) minimizing total batch cost + excess production
    subject to required output, min/max batch limits, and optional budget cap.
    """
    if not req.items:
        return MrpOptimizeResponse(feasible=True, assumptions=["No batch items provided."])

    n = len(req.items)
    for it in req.items:
        if it.batch_yield <= 0:
            return MrpOptimizeResponse(
                feasible=False,
                infeasibility_reason=f"Batch item {it.recipe_name} has invalid batch yield <= 0.",
            )
        if Decimal(it.max_batches) * it.batch_yield < it.required_qty:
            return MrpOptimizeResponse(
                feasible=False,
                infeasibility_reason=(
                    f"Required output {it.required_qty} for '{it.recipe_name}' exceeds max batch capacity "
                    f"({it.max_batches} x {it.batch_yield} = {Decimal(it.max_batches) * it.batch_yield})."
                ),
            )

    # Decision variables: batch count per item (integer if full batches only, continuous if partial allowed)
    integrality = np.array([0 if it.allow_partial_batches else 1 for it in req.items], dtype=float)
    lb = np.array([float(it.min_batches) for it in req.items], dtype=float)
    ub = np.array([float(it.max_batches) for it in req.items], dtype=float)

    # Objective: minimize cost + small penalty on excess output
    c = np.array(
        [float(it.cost_per_batch) + float(it.batch_yield) * 0.01 for it in req.items],
        dtype=float,
    )

    rows: list[np.ndarray] = []
    row_lb: list[float] = []
    row_ub: list[float] = []

    for idx, it in enumerate(req.items):
        vec = np.zeros(n, dtype=float)
        vec[idx] = float(it.batch_yield)
        rows.append(vec)
        row_lb.append(float(it.required_qty))
        max_allowed_out = float(it.required_qty * (Decimal("1") + req.max_excess_ratio))
        if not it.allow_partial_batches:
            # Ensure at least ceiling batch is allowed if excess cap is tight
            ceil_out = math.ceil(float(it.required_qty / it.batch_yield)) * float(it.batch_yield)
            max_allowed_out = max(max_allowed_out, ceil_out)
        row_ub.append(max_allowed_out)

    if req.max_total_cost is not None:
        cost_vec = np.array([float(it.cost_per_batch) for it in req.items], dtype=float)
        rows.append(cost_vec)
        row_lb.append(-np.inf)
        row_ub.append(float(req.max_total_cost))

    res = milp(c=c, integrality=integrality, bounds=Bounds(lb, ub), constraints=LinearConstraint(np.vstack(rows), row_lb, row_ub))
    if not res.success or res.x is None:
        return MrpOptimizeResponse(
            feasible=False,
            infeasibility_reason="HiGHS MILP solver found no feasible batch plan within budget and excess constraints.",
            assumptions=["Relax max_total_cost or max_excess_ratio constraints."],
        )

    out_rows: list[MrpOptimizedBatchResult] = []
    tot_cost = Decimal("0")
    tot_excess = Decimal("0")
    for idx, it in enumerate(req.items):
        batches = Decimal(str(round(float(res.x[idx]), 4)))
        if not it.allow_partial_batches:
            batches = Decimal(int(round(float(res.x[idx]))))
        planned_out = _q4(batches * it.batch_yield)
        excess = _q4(max(Decimal("0"), planned_out - it.required_qty))
        est_cost = _q2(batches * it.cost_per_batch)
        tot_cost += est_cost
        tot_excess += excess
        out_rows.append(
            MrpOptimizedBatchResult(
                recipe_id=it.recipe_id,
                recipe_code=it.recipe_code,
                recipe_name=it.recipe_name,
                selected_batches=batches,
                planned_output=planned_out,
                excess_output=excess,
                estimated_cost=est_cost,
            )
        )

    return MrpOptimizeResponse(
        feasible=True,
        total_cost=_q2(tot_cost),
        total_excess_output=_q4(tot_excess),
        assumptions=[
            "Deterministic HiGHS MILP solver; respects chef-locked recipes, batch yields, and required demand.",
            "Does not reserve stock, create POs, or schedule kitchen equipment.",
        ],
        results=out_rows,
    )


async def analyze_mrp_with_ai(req: MrpAiAnalyzeRequest) -> MrpAiAnalyzeResponse:
    """
    Grounded MRP AI analysis reusing AiProviderRouter.
    Never fabricates codes, quantities, stock levels, or prices — strictly summarizes authorized MRP lines.
    """
    clean_q = _sanitize_untrusted(req.question)
    mrp = req.mrp_result
    top_shortages = sorted(
        [m for m in mrp.consolidated_materials if (m.indicative_current_shortage_qty or Decimal("0")) > 0],
        key=lambda x: x.indicative_shortage_value or x.indicative_current_shortage_qty or Decimal("0"),
        reverse=True,
    )[:5]
    top_cost = sorted(
        [m for m in mrp.consolidated_materials if m.requirement_value is not None],
        key=lambda x: x.requirement_value or Decimal("0"),
        reverse=True,
    )[:5]
    earliest = sorted(mrp.consolidated_materials, key=lambda x: x.required_date_utc)[:5]
    shared = [m for m in mrp.consolidated_materials if len(m.contributing_recipes) > 1][:5]

    if req.provider_policy is not None:
        router = AiProviderRouter(
            {
                "LOCAL": LocalOllamaAdapter(),
                "GEMINI": GeminiSdkAdapter(),
                "CLAUDE": ClaudeSdkAdapter(),
            }
        )
        try:
            ai_req = AiExecutionRequest(
                task_name="mrp_analysis",
                prompt_version="culinary-mrp-2b-v1",
                system_prompt=(
                    "You are SILA Culinary MRP Analyst. Answer ONLY using the provided verified MRP figures. "
                    "Never invent material codes, quantities, prices, or stock. Never issue POs or stock reservations."
                ),
                user_payload={
                    "question": clean_q,
                    "summary": {
                        "total_unique_materials": mrp.total_unique_materials,
                        "shortage_count": mrp.shortage_count,
                        "missing_mapping_count": mrp.missing_mapping_count,
                        "estimated_requirement_value": str(mrp.estimated_requirement_value),
                        "currency": mrp.currency,
                    },
                    "top_shortages": [
                        {
                            "code": m.material_code,
                            "name": m.material_description,
                            "gross": str(m.gross_qty),
                            "shortage": str(m.indicative_current_shortage_qty),
                            "uom": m.base_uom,
                        }
                        for m in top_shortages
                    ],
                    "top_cost": [
                        {
                            "code": m.material_code,
                            "name": m.material_description,
                            "value": str(m.requirement_value),
                            "recipes": m.contributing_recipes,
                        }
                        for m in top_cost
                    ],
                },
                output_json_schema=MrpAiAnalyzeOutput.model_json_schema(),
            )
            parsed, resp = await router.execute_structured(req.provider_policy, ai_req, MrpAiAnalyzeOutput)
            return MrpAiAnalyzeResponse(
                provider_used=resp.provider,
                model_used=resp.model,
                output=parsed,
                metadata={"latency_ms": resp.latency_ms, "used_fallback": resp.used_fallback},
            )
        except ProviderUnavailableError:
            pass

    # Deterministic grounded explanation fallback using verified MRP lines only
    q_lower = clean_q.lower()
    parts: list[str] = []
    recs: list[str] = []
    ref_mats: list[str] = []
    ref_recs: list[str] = []

    if "short" in q_lower or "critical" in q_lower:
        if top_shortages:
            desc = "; ".join(
                f"{m.material_description} ({m.material_code or 'UNMAPPED'}): need {m.gross_qty} {m.base_uom}, indicative current shortage {m.indicative_current_shortage_qty} {m.base_uom}"
                for m in top_shortages
            )
            parts.append(f"Top indicative shortages based on current eligible stock: {desc}.")
            ref_mats.extend([m.material_code or m.material_description for m in top_shortages])
        else:
            parts.append("No verified current-stock shortages detected across resolved materials.")
    elif "earliest" in q_lower or "when" in q_lower or "deadline" in q_lower:
        if earliest:
            desc = "; ".join(
                f"{m.material_description} required by {m.required_date_utc.strftime('%Y-%m-%d %H:%M UTC')} ({m.gross_qty} {m.base_uom})"
                for m in earliest
            )
            parts.append(f"Earliest required materials: {desc}.")
            ref_mats.extend([m.material_code or m.material_description for m in earliest])
    elif "shared" in q_lower or "multiple" in q_lower:
        if shared:
            desc = "; ".join(
                f"{m.material_description} ({m.gross_qty} {m.base_uom}) shared across {', '.join(m.contributing_recipes)}"
                for m in shared
            )
            parts.append(f"Ingredients shared across multiple recipes: {desc}.")
            ref_mats.extend([m.material_code or m.material_description for m in shared])
        else:
            parts.append("No ingredients are shared across multiple recipes in this MRP run.")
    elif "cost" in q_lower or "reduce" in q_lower or "expensive" in q_lower:
        if top_cost:
            desc = "; ".join(
                f"{m.material_description}: {mrp.currency} {m.requirement_value} ({m.gross_qty} {m.base_uom})"
                for m in top_cost
            )
            parts.append(f"Highest requirement value drivers: {desc}.")
            ref_mats.extend([m.material_code or m.material_description for m in top_cost])
            recs.append("Review yield losses, batch rounding excess, or chef-approved seasonal substitutes in Phase 1B.")
    else:
        parts.append(
            f"MRP Run {mrp.mrp_run_id} covers {mrp.total_events} event(s), {mrp.total_recipes} recipe(s), and "
            f"{mrp.total_unique_materials} unique materials ({mrp.shortage_count} potential shortages, "
            f"{mrp.missing_mapping_count} missing mappings)."
        )
        if top_shortages:
            parts.append(
                f"Primary shortage item: {top_shortages[0].material_description} "
                f"({top_shortages[0].indicative_current_shortage_qty} {top_shortages[0].base_uom})."
            )
            ref_mats.append(top_shortages[0].material_code or top_shortages[0].material_description)

    for m in top_shortages[:3]:
        for r in m.contributing_recipes:
            if r not in ref_recs:
                ref_recs.append(r)

    if mrp.missing_mapping_count > 0:
        recs.append(f"Resolve {mrp.missing_mapping_count} unmapped material(s) via Phase 1C Material Creation Request.")
    if not recs:
        recs.append("Review exceptions and submit MRP baseline for authorized approval before Phase 2C procurement handover.")

    return MrpAiAnalyzeResponse(
        provider_used="LOCAL_DETERMINISTIC",
        model_used="mrp-grounded-analyzer-2b",
        output=MrpAiAnalyzeOutput(
            answer=" ".join(parts),
            referenced_materials=ref_mats,
            referenced_recipes=ref_recs,
            recommendations=recs,
            data_grounding_notes=[
                "All quantities, shortages, and costs are derived strictly from deterministic Phase 2B MRP calculations.",
                "Shortages are preliminary against current eligible stock; actual reservations belong to Phase 2C.",
            ],
        ),
    )
