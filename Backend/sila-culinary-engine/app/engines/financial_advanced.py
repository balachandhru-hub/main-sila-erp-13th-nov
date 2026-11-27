from __future__ import annotations

import math
import re
from decimal import Decimal, ROUND_CEILING, ROUND_HALF_UP
from typing import Any, Optional

from app.providers.adapters import ClaudeSdkAdapter, GeminiSdkAdapter, LocalOllamaAdapter
from app.providers.router import AiExecutionRequest, AiProviderRouter, ProviderUnavailableError
from app.schemas.financial_engine import (
    EventFinancialCalculateRequest,
    EventFinancialCalculateResponse,
    FinancialAiAnalyzeRequest,
    FinancialAiAnalyzeResponse,
    FinancialIngredientResult,
    FinancialRecipeCostRequest,
    FinancialRecipeCostResponse,
    FxRateInput,
    MaterialPriceImpactLine,
    MaterialPriceImpactRequest,
    MaterialPriceImpactResponse,
    MenuCostingRequest,
    MenuCostingResponse,
    MenuLineFinancialResult,
    WaterfallStepDto,
)

_ZERO = Decimal("0")
_ONE = Decimal("1")
_HUNDRED = Decimal("100")
_Q2 = Decimal("0.01")
_Q4 = Decimal("0.0001")

_INJECTION_PATTERNS = (
    re.compile(r"ignore\s+previous\s+instructions", re.IGNORECASE),
    re.compile(r"post\s+(sap|gl|journal|invoice|accounting)", re.IGNORECASE),
    re.compile(r"delete\s+from|drop\s+table|update\s+set", re.IGNORECASE),
    re.compile(r"system\s+prompt|override\s+budget", re.IGNORECASE),
)


def _q2(v: Decimal) -> Decimal:
    return v.quantize(_Q2, rounding=ROUND_HALF_UP)


def _q4(v: Decimal) -> Decimal:
    return v.quantize(_Q4, rounding=ROUND_HALF_UP)


def _ceil_int(v: Decimal) -> int:
    return int(v.to_integral_value(rounding=ROUND_CEILING))


def _resolve_fx_rate(
    source_currency: str,
    target_currency: str,
    fx_rates: list[FxRateInput],
    explicit_rate: Optional[Decimal] = None,
) -> Optional[Decimal]:
    src = (source_currency or target_currency).strip().upper()
    tgt = target_currency.strip().upper()
    if src == tgt:
        return _ONE
    if explicit_rate is not None and explicit_rate > _ZERO:
        return explicit_rate
    for r in fx_rates:
        if r.source_currency.strip().upper() == src and r.target_currency.strip().upper() == tgt:
            return r.rate
    for r in fx_rates:
        if r.source_currency.strip().upper() == tgt and r.target_currency.strip().upper() == src and r.rate > _ZERO:
            return _ONE / r.rate
    return None


def calculate_advanced_recipe_costing(req: FinancialRecipeCostRequest) -> FinancialRecipeCostResponse:
    """
    Phase 3A Deterministic Decimal Recipe & Sub-Recipe Costing Engine:
    - Price unit & pricing UOM normalization (e.g. AED 250 / 5 KG -> AED 50 / KG).
    - Currency conversion using snapshot FX rates (never assumes missing rate = 1).
    - Single-pass yield adjustment (never double-applies yield if MRP already applied it).
    - Sub-recipe DAG protection (prevents circular dependency and double-counting raw lines).
    - Batch rounding (full batch vs partial batch) separating expected consumption vs remaining output.
    - Never substitutes 0 for missing/expired prices.
    """
    target_ccy = req.reporting_currency.strip().upper()
    planned_servings = req.planned_servings if req.planned_servings and req.planned_servings > _ZERO else req.standard_batch_servings
    raw_batches = planned_servings / req.standard_batch_servings
    planned_batches = raw_batches if req.allow_partial_batch else Decimal(_ceil_int(raw_batches))
    planned_output_servings = planned_batches * req.standard_batch_servings
    excess_servings = max(_ZERO, planned_output_servings - planned_servings)

    scale = _ONE + (req.global_price_change_percent / _HUNDRED)
    exceptions: list[dict[str, Any]] = []
    line_results: list[FinancialIngredientResult] = []

    total_ing_cost = _ZERO
    food_cost = _ZERO
    bev_cost = _ZERO
    sub_cost = _ZERO
    missing_prices = 0
    missing_fx = 0
    uom_conflicts = 0
    circular = False

    for ing in req.ingredients:
        # Guard against double-counting already exploded raw ingredients of a sub-recipe
        if ing.sub_recipe_already_exploded_in_parent:
            exceptions.append(
                {
                    "exception_type": "DUPLICATE_SUB_RECIPE_LINE_GUARDED",
                    "severity": "INFO",
                    "line_ref": ing.line_ref,
                    "message": f"Skipped already-exploded line '{ing.description}' to prevent double counting.",
                }
            )
            continue

        if ing.is_sub_recipe:
            if (ing.sub_recipe_id and ing.sub_recipe_id == req.recipe_id_or_ref) or (req.recipe_id_or_ref in set(ing.sub_recipe_dependencies)):
                circular = True
                exceptions.append(
                    {
                        "exception_type": "CIRCULAR_RECIPE_DEPENDENCY",
                        "severity": "CRITICAL",
                        "line_ref": ing.line_ref,
                        "message": f"Circular dependency detected for sub-recipe {ing.description}.",
                    }
                )
                line_results.append(
                    FinancialIngredientResult(
                        line_ref=ing.line_ref,
                        material_code=ing.sub_recipe_id,
                        description=ing.description,
                        cost_class=ing.cost_class,
                        is_sub_recipe=True,
                        recipe_qty=_q4(ing.quantity),
                        recipe_uom=ing.uom,
                        raw_qty=_q4(ing.quantity),
                        costable_base_qty=_q4(ing.quantity),
                        base_uom=ing.base_uom,
                        effective_yield_percent=ing.yield_percent,
                        status="CIRCULAR_DEPENDENCY",
                        warning="Circular sub-recipe dependency detected.",
                    )
                )
                continue

            if ing.sub_recipe_batch_cost is None or ing.sub_recipe_batch_yield is None or ing.sub_recipe_batch_yield <= _ZERO:
                missing_prices += 1
                exceptions.append(
                    {
                        "exception_type": "MISSING_PRICE",
                        "severity": "CRITICAL",
                        "line_ref": ing.line_ref,
                        "message": f"Sub-recipe cost or yield missing for {ing.description}.",
                    }
                )
                line_results.append(
                    FinancialIngredientResult(
                        line_ref=ing.line_ref,
                        material_code=ing.sub_recipe_id,
                        description=ing.description,
                        cost_class=ing.cost_class,
                        is_sub_recipe=True,
                        recipe_qty=_q4(ing.quantity),
                        recipe_uom=ing.uom,
                        raw_qty=_q4(ing.quantity),
                        costable_base_qty=_q4(ing.quantity),
                        base_uom=ing.base_uom,
                        effective_yield_percent=ing.yield_percent,
                        status="MISSING_PRICE",
                        warning="Sub-recipe cost or yield unavailable.",
                    )
                )
                continue

            unit_sub = (ing.sub_recipe_batch_cost / ing.sub_recipe_batch_yield) * scale
            scaled_qty = ing.quantity * planned_batches
            cost_val = _q4(scaled_qty * unit_sub)
            sub_cost += cost_val
            total_ing_cost += cost_val
            if ing.cost_class in ("ALCOHOLIC_BEVERAGE", "NON_ALCOHOLIC_BEVERAGE"):
                bev_cost += cost_val
            else:
                food_cost += cost_val
            line_results.append(
                FinancialIngredientResult(
                    line_ref=ing.line_ref,
                    material_code=ing.sub_recipe_id,
                    description=ing.description,
                    cost_class=ing.cost_class,
                    is_sub_recipe=True,
                    recipe_qty=_q4(scaled_qty),
                    recipe_uom=ing.uom,
                    raw_qty=_q4(scaled_qty),
                    costable_base_qty=_q4(scaled_qty),
                    base_uom=ing.base_uom,
                    effective_yield_percent=ing.yield_percent,
                    normalized_unit_cost=_q4(unit_sub),
                    price_source="SUB_RECIPE_SNAPSHOT",
                    source_currency=target_ccy,
                    fx_rate_applied=_ONE,
                    ingredient_cost=cost_val,
                    status="VERIFIED",
                )
            )
            continue

        # Yield calculation (single pass; skip if MRP already adjusted)
        batch_qty = ing.quantity * planned_batches
        if ing.quantity_basis == "EDIBLE" and not ing.yield_already_applied_in_mrp and ing.yield_percent < _HUNDRED:
            raw_qty = batch_qty / (ing.yield_percent / _HUNDRED)
        else:
            raw_qty = batch_qty

        if not ing.uom_compatible or ing.uom_to_base_factor is None or ing.uom_to_base_factor <= _ZERO:
            uom_conflicts += 1
            exceptions.append(
                {
                    "exception_type": "MISSING_UOM_CONVERSION",
                    "severity": "CRITICAL",
                    "line_ref": ing.line_ref,
                    "material_code": ing.material_code,
                    "message": f"Missing UOM conversion from {ing.uom} to {ing.base_uom}.",
                }
            )
            line_results.append(
                FinancialIngredientResult(
                    line_ref=ing.line_ref,
                    material_code=ing.material_code,
                    description=ing.description,
                    cost_class=ing.cost_class,
                    recipe_qty=_q4(batch_qty),
                    recipe_uom=ing.uom,
                    raw_qty=_q4(raw_qty),
                    costable_base_qty=_q4(raw_qty),
                    base_uom=ing.base_uom,
                    effective_yield_percent=ing.yield_percent,
                    status="UOM_CONFLICT",
                    warning=f"Missing UOM conversion {ing.uom} -> {ing.base_uom}.",
                )
            )
            continue

        costable_qty = raw_qty * ing.uom_to_base_factor

        # Price resolution by priority
        selected_candidate = None
        if ing.manual_override_price is not None:
            from app.schemas.financial_engine import PriceCandidateInput

            selected_candidate = PriceCandidateInput(
                source_type="MANUAL_ESTIMATE",
                unit_price=ing.manual_override_price,
                currency=ing.manual_override_currency or target_ccy,
                price_unit=_ONE,
                pricing_uom=ing.base_uom,
                price_uom_to_base_factor=_ONE,
                is_verified=False,
            )
        else:
            by_source = {c.source_type: c for c in ing.price_candidates}
            expired_candidate = None
            for src_type in req.cost_source_priority:
                cand = by_source.get(src_type)
                if cand is None:
                    continue
                if cand.valid_until_utc is not None and req.valuation_date_utc is not None and cand.valid_until_utc < req.valuation_date_utc and not ing.allow_expired_price:
                    expired_candidate = cand
                    continue
                selected_candidate = cand
                break
            if selected_candidate is None and expired_candidate is not None:
                missing_prices += 1
                exceptions.append(
                    {
                        "exception_type": "EXPIRED_CONTRACT_PRICE",
                        "severity": "HIGH",
                        "line_ref": ing.line_ref,
                        "material_code": ing.material_code,
                        "message": f"Price expired for {ing.description} ({expired_candidate.source_type}).",
                    }
                )
                line_results.append(
                    FinancialIngredientResult(
                        line_ref=ing.line_ref,
                        material_code=ing.material_code,
                        description=ing.description,
                        cost_class=ing.cost_class,
                        recipe_qty=_q4(batch_qty),
                        recipe_uom=ing.uom,
                        raw_qty=_q4(raw_qty),
                        costable_base_qty=_q4(costable_qty),
                        base_uom=ing.base_uom,
                        effective_yield_percent=ing.yield_percent,
                        price_source=expired_candidate.source_type,
                        source_currency=expired_candidate.currency,
                        status="EXPIRED_PRICE",
                        warning="Price expired; fallback disabled.",
                    )
                )
                continue

        if selected_candidate is None:
            if ing.is_mandatory:
                missing_prices += 1
            exceptions.append(
                {
                    "exception_type": "MISSING_MATERIAL_PRICE",
                    "severity": "CRITICAL" if ing.is_mandatory else "WARNING",
                    "line_ref": ing.line_ref,
                    "material_code": ing.material_code,
                    "message": f"No verified price source available for {ing.description}.",
                }
            )
            line_results.append(
                FinancialIngredientResult(
                    line_ref=ing.line_ref,
                    material_code=ing.material_code,
                    description=ing.description,
                    cost_class=ing.cost_class,
                    recipe_qty=_q4(batch_qty),
                    recipe_uom=ing.uom,
                    raw_qty=_q4(raw_qty),
                    costable_base_qty=_q4(costable_qty),
                    base_uom=ing.base_uom,
                    effective_yield_percent=ing.yield_percent,
                    status="MISSING_PRICE",
                    warning="Verified material price missing.",
                )
            )
            continue

        fx_rate = _resolve_fx_rate(selected_candidate.currency, target_ccy, req.fx_rates)
        if fx_rate is None:
            missing_fx += 1
            exceptions.append(
                {
                    "exception_type": "MISSING_EXCHANGE_RATE",
                    "severity": "CRITICAL",
                    "line_ref": ing.line_ref,
                    "material_code": ing.material_code,
                    "message": f"Missing FX rate from {selected_candidate.currency} to {target_ccy}.",
                }
            )
            line_results.append(
                FinancialIngredientResult(
                    line_ref=ing.line_ref,
                    material_code=ing.material_code,
                    description=ing.description,
                    cost_class=ing.cost_class,
                    recipe_qty=_q4(batch_qty),
                    recipe_uom=ing.uom,
                    raw_qty=_q4(raw_qty),
                    costable_base_qty=_q4(costable_qty),
                    base_uom=ing.base_uom,
                    effective_yield_percent=ing.yield_percent,
                    price_source=selected_candidate.source_type,
                    source_currency=selected_candidate.currency,
                    status="MISSING_FX",
                    warning=f"Missing FX rate {selected_candidate.currency}->{target_ccy}.",
                )
            )
            continue

        # Price Unit & Pricing UOM normalization (Test A: AED 250 / 5 KG = AED 50/KG)
        norm_unit_cost = (selected_candidate.unit_price / (selected_candidate.price_unit * selected_candidate.price_uom_to_base_factor)) * fx_rate * scale
        line_cost = _q4(costable_qty * norm_unit_cost)
        total_ing_cost += line_cost
        if ing.cost_class in ("ALCOHOLIC_BEVERAGE", "NON_ALCOHOLIC_BEVERAGE"):
            bev_cost += line_cost
        else:
            food_cost += line_cost

        line_results.append(
            FinancialIngredientResult(
                line_ref=ing.line_ref,
                material_code=ing.material_code,
                description=ing.description,
                cost_class=ing.cost_class,
                recipe_qty=_q4(batch_qty),
                recipe_uom=ing.uom,
                raw_qty=_q4(raw_qty),
                costable_base_qty=_q4(costable_qty),
                base_uom=ing.base_uom,
                effective_yield_percent=ing.yield_percent,
                normalized_unit_cost=_q4(norm_unit_cost),
                price_source=selected_candidate.source_type,
                source_currency=selected_candidate.currency,
                fx_rate_applied=_q4(fx_rate),
                ingredient_cost=line_cost,
                status="VERIFIED" if selected_candidate.is_verified and selected_candidate.source_type != "MANUAL_ESTIMATE" else "ESTIMATED",
            )
        )

    # Share of total ingredient cost
    if total_ing_cost > _ZERO:
        for lr in line_results:
            if lr.ingredient_cost is not None:
                lr.share_of_ingredient_cost_pct = _q2((lr.ingredient_cost / total_ing_cost) * _HUNDRED)

    direct_prod_cost = _ZERO
    overhead_cost = _ZERO
    for ac in req.additional_costs:
        if ac.basis == "FIXED_BATCH":
            val = ac.amount * planned_batches
        elif ac.basis == "PER_SERVING":
            val = ac.amount * planned_output_servings
        else:
            val = total_ing_cost * (ac.amount / _HUNDRED)
        if ac.layer == "ALLOCATED_OVERHEAD":
            overhead_cost += val
        else:
            direct_prod_cost += val

    fully_allocated = total_ing_cost + direct_prod_cost + overhead_cost
    ing_per_serving = total_ing_cost / planned_output_servings
    direct_per_serving = (total_ing_cost + direct_prod_cost) / planned_output_servings
    full_per_serving = fully_allocated / planned_output_servings
    expected_consumption_cost = direct_per_serving * planned_servings
    remaining_output_val = direct_per_serving * excess_servings

    finished_units = req.finished_output_qty * planned_batches
    cost_per_finished_unit = fully_allocated / finished_units if finished_units > _ZERO else _ZERO

    net_selling: Optional[Decimal] = None
    food_cost_pct: Optional[Decimal] = None
    ing_contrib: Optional[Decimal] = None
    ing_contrib_pct: Optional[Decimal] = None
    direct_contrib: Optional[Decimal] = None
    full_profit: Optional[Decimal] = None

    if req.selling_price_per_serving is not None:
        if req.selling_price_includes_vat and req.vat_percent > _ZERO:
            net_selling = req.selling_price_per_serving / (_ONE + req.vat_percent / _HUNDRED)
        else:
            net_selling = req.selling_price_per_serving
        if net_selling > _ZERO:
            food_cost_pct = _q2((ing_per_serving / net_selling) * _HUNDRED)
            ing_contrib = net_selling - ing_per_serving
            ing_contrib_pct = _q2((ing_contrib / net_selling) * _HUNDRED)
            direct_contrib = net_selling - direct_per_serving
            full_profit = net_selling - full_per_serving

    target_fc_ratio = req.target_food_cost_percent / _HUNDRED
    req_price_fc = ing_per_serving / target_fc_ratio if target_fc_ratio > _ZERO else _ZERO
    margin_denom = _ONE - (req.target_contribution_margin_percent / _HUNDRED)
    req_price_margin_direct = direct_per_serving / margin_denom if margin_denom > _ZERO else _ZERO
    req_price_margin_full = full_per_serving / margin_denom if margin_denom > _ZERO else _ZERO

    completeness = "COMPLETE" if (missing_prices == 0 and missing_fx == 0 and uom_conflicts == 0 and not circular) else "INCOMPLETE"

    return FinancialRecipeCostResponse(
        recipe_id_or_ref=req.recipe_id_or_ref,
        recipe_name=req.recipe_name,
        recipe_version=req.recipe_version,
        reporting_currency=target_ccy,
        standard_batch_servings=req.standard_batch_servings,
        planned_servings=planned_servings,
        planned_batches=_q4(planned_batches),
        planned_output_servings=_q4(planned_output_servings),
        excess_servings=_q4(excess_servings),
        total_ingredient_cost=_q2(total_ing_cost),
        food_ingredient_cost=_q2(food_cost),
        beverage_ingredient_cost=_q2(bev_cost),
        sub_recipe_cost=_q2(sub_cost),
        direct_production_cost=_q2(direct_prod_cost),
        allocated_overhead_cost=_q2(overhead_cost),
        fully_allocated_batch_cost=_q2(fully_allocated),
        expected_event_consumption_cost=_q2(expected_consumption_cost),
        remaining_usable_output_value=_q2(remaining_output_val),
        ingredient_cost_per_serving=_q4(ing_per_serving),
        direct_cost_per_serving=_q4(direct_per_serving),
        fully_allocated_cost_per_serving=_q4(full_per_serving),
        cost_per_finished_unit=_q4(cost_per_finished_unit),
        net_selling_price_per_serving=_q4(net_selling) if net_selling is not None else None,
        food_cost_percent=food_cost_pct,
        ingredient_contribution_per_serving=_q4(ing_contrib) if ing_contrib is not None else None,
        ingredient_contribution_percent=ing_contrib_pct,
        direct_contribution_per_serving=_q4(direct_contrib) if direct_contrib is not None else None,
        fully_allocated_profit_per_serving=_q4(full_profit) if full_profit is not None else None,
        required_selling_price_target_food_cost=_q2(req_price_fc),
        required_selling_price_target_margin_direct=_q2(req_price_margin_direct),
        required_selling_price_target_margin_full=_q2(req_price_margin_full),
        cost_completeness=completeness,
        missing_price_count=missing_prices,
        missing_fx_count=missing_fx,
        uom_conflict_count=uom_conflicts,
        circular_dependency_detected=circular,
        exceptions=exceptions,
        lines=line_results,
    )


def calculate_menu_costing(req: MenuCostingRequest) -> MenuCostingResponse:
    guests = Decimal(req.planned_guests)
    food_total = _ZERO
    bev_total = _ZERO
    results: list[MenuLineFinancialResult] = []

    for line in req.lines:
        portions = guests * (line.guest_uptake_percent / _HUNDRED) * line.servings_per_guest
        line_cost = portions * line.cost_per_serving
        per_guest = line_cost / guests
        if line.cost_class == "BEVERAGE":
            bev_total += line_cost
        else:
            food_total += line_cost
        results.append(
            MenuLineFinancialResult(
                line_ref=line.line_ref,
                name=line.name,
                course=line.course,
                cost_class=line.cost_class,
                expected_portions=_q4(portions),
                cost_per_serving=_q4(line.cost_per_serving),
                expected_line_cost=_q2(line_cost),
                cost_per_guest=_q4(per_guest),
            )
        )

    total_cost = food_total + bev_total
    pkg_rev = req.package_selling_price_per_guest * guests if req.package_selling_price_per_guest is not None else None
    contrib = pkg_rev - total_cost if pkg_rev is not None else None

    if req.allocated_food_revenue_per_guest and req.allocated_food_revenue_per_guest > _ZERO:
        food_pct = _q2(((food_total / guests) / req.allocated_food_revenue_per_guest) * _HUNDRED)
        food_label = "Food Cost % of Allocated Food Revenue"
    elif pkg_rev and pkg_rev > _ZERO:
        food_pct = _q2((food_total / pkg_rev) * _HUNDRED)
        food_label = "Food Cost % against Total Package Revenue"
    else:
        food_pct = None
        food_label = "Food Revenue Unknown"

    if req.allocated_beverage_revenue_per_guest and req.allocated_beverage_revenue_per_guest > _ZERO:
        bev_pct = _q2(((bev_total / guests) / req.allocated_beverage_revenue_per_guest) * _HUNDRED)
        bev_label = "Beverage Cost % of Allocated Beverage Revenue"
    elif pkg_rev and pkg_rev > _ZERO:
        bev_pct = _q2((bev_total / pkg_rev) * _HUNDRED)
        bev_label = "Beverage Cost % against Total Package Revenue"
    else:
        bev_pct = None
        bev_label = "Beverage Revenue Unknown"

    return MenuCostingResponse(
        menu_id_or_ref=req.menu_id_or_ref,
        menu_name=req.menu_name,
        service_style=req.service_style,
        planned_guests=req.planned_guests,
        reporting_currency=req.reporting_currency.upper(),
        total_food_cost=_q2(food_total),
        total_beverage_cost=_q2(bev_total),
        total_menu_cost=_q2(total_cost),
        food_cost_per_guest=_q4(food_total / guests),
        beverage_cost_per_guest=_q4(bev_total / guests),
        expected_cost_per_guest=_q4(total_cost / guests),
        total_package_revenue=_q2(pkg_rev) if pkg_rev is not None else None,
        expected_contribution=_q2(contrib) if contrib is not None else None,
        food_cost_percent=food_pct,
        food_cost_basis_label=food_label,
        beverage_cost_percent=bev_pct,
        beverage_cost_basis_label=bev_label,
        lines=results,
    )


def calculate_event_financials(req: EventFinancialCalculateRequest) -> EventFinancialCalculateResponse:
    """
    Phase 3A Event P&L, Commitment, Forecast-at-Completion, Break-Even & Variance Engine.
    Strictly separates Procurement Expenditure vs Expected Consumption Cost vs Actual Consumption.
    """
    exceptions: list[dict[str, Any]] = []
    # 1. Billable guest count per contractual rule
    actual_g = req.actual_guests
    if req.billable_policy == "GUARANTEED_ONLY":
        billable_guests = req.guaranteed_guests if req.guaranteed_guests > 0 else req.planned_guests
    elif req.billable_policy == "ACTUAL_ATTENDANCE":
        billable_guests = actual_g if actual_g is not None else req.planned_guests
    else:  # GUARANTEED_PLUS_ADDITIONAL
        base_g = req.guaranteed_guests if req.guaranteed_guests > 0 else req.planned_guests
        billable_guests = max(base_g, actual_g if actual_g is not None else req.planned_guests)

    # 2. Revenue calculation (explicit lines or per-guest baseline)
    gross_rev = _ZERO
    discount_tot = _ZERO
    net_rev = _ZERO
    planned_rev = _ZERO
    contracted_rev = _ZERO
    invoiced_rev = _ZERO
    recognized_rev = _ZERO

    if req.revenue_lines:
        for rl in req.revenue_lines:
            fx = rl.exchange_rate_to_reporting if rl.exchange_rate_to_reporting else (_ONE if rl.currency.upper() == req.reporting_currency.upper() else None)
            if fx is None:
                exceptions.append({"exception_type": "MISSING_EXCHANGE_RATE", "severity": "CRITICAL", "message": f"Missing FX rate for revenue line {rl.line_ref} ({rl.currency})."})
                continue
            qty = Decimal(billable_guests) if rl.billing_basis == "GUARANTEED_RULE" else rl.quantity
            line_gross = qty * rl.unit_price * fx
            line_disc = (rl.discount_amount * fx) + (line_gross * rl.discount_percent / _HUNDRED)
            line_net = max(_ZERO, line_gross - line_disc)
            gross_rev += line_gross
            discount_tot += line_disc
            net_rev += line_net
            if rl.revenue_status == "PLANNED":
                planned_rev += line_net
            elif rl.revenue_status == "CONTRACTED":
                contracted_rev += line_net
            elif rl.revenue_status == "INVOICED":
                invoiced_rev += line_net
            else:
                recognized_rev += line_net
    else:
        gross_rev = Decimal(billable_guests) * req.selling_price_per_guest
        discount_tot = gross_rev * (req.discount_percent / _HUNDRED)
        net_rev = max(_ZERO, gross_rev - discount_tot)
        planned_rev = net_rev

    # 3. Expenses, manpower, fixed/variable/step-fixed classification
    est_food = _ZERO
    est_bev = _ZERO
    est_manpower = _ZERO
    est_equip = _ZERO
    est_decor = _ZERO
    est_ext_svc = _ZERO
    est_other = _ZERO
    fixed_total = _ZERO
    var_per_guest_from_lines = _ZERO
    step_fixed_rules: list[tuple[int, Decimal]] = []

    committed_from_expenses = _ZERO
    actual_from_expenses = _ZERO
    has_verified_actual_expenses = False
    seen_refs: set[str] = set()
    total_labor_hours = _ZERO

    for ex in req.expense_lines:
        if ex.is_duplicate_of or ex.expense_ref in seen_refs:
            exceptions.append({"exception_type": "DUPLICATE_EXPENSE", "severity": "HIGH", "message": f"Duplicate expense reference {ex.expense_ref} excluded."})
            continue
        seen_refs.add(ex.expense_ref)

        fx = ex.exchange_rate_to_reporting if ex.exchange_rate_to_reporting else (_ONE if ex.currency.upper() == req.reporting_currency.upper() else None)
        if fx is None:
            exceptions.append({"exception_type": "MISSING_EXCHANGE_RATE", "severity": "CRITICAL", "message": f"Missing FX rate for expense {ex.expense_ref}."})
            continue

        # Manpower calculation if detailed fields provided
        amt = ex.estimated_amount * fx
        if ex.category == "MANPOWER" and amt == _ZERO and ex.staff_count is not None and ex.planned_hours is not None and ex.hourly_rate is not None:
            base_mp = Decimal(ex.staff_count) * ex.planned_hours * ex.hourly_rate
            ot_mp = Decimal(ex.staff_count) * (ex.overtime_hours or _ZERO) * (ex.overtime_rate or _ZERO)
            amt = (base_mp + ot_mp + (ex.fixed_shift_cost or _ZERO) + (ex.external_agency_charge or _ZERO)) * fx
            total_labor_hours += Decimal(ex.staff_count) * (ex.planned_hours + (ex.overtime_hours or _ZERO))
        elif ex.planned_hours:
            total_labor_hours += ex.planned_hours

        if ex.cost_behavior == "VARIABLE_PER_GUEST" and ex.variable_rate_per_guest is not None:
            rate = ex.variable_rate_per_guest * fx
            var_per_guest_from_lines += rate
            if amt == _ZERO:
                amt = rate * Decimal(req.planned_guests)
        elif ex.cost_behavior == "STEP_FIXED" and ex.step_threshold_guests and ex.step_increment_cost:
            step_fixed_rules.append((ex.step_threshold_guests, ex.step_increment_cost * fx))
            steps = math.ceil(req.planned_guests / ex.step_threshold_guests)
            if amt == _ZERO:
                amt = Decimal(steps) * ex.step_increment_cost * fx
        elif ex.cost_behavior in ("FIXED", "MIXED"):
            fixed_total += amt

        if ex.category == "FOOD":
            est_food += amt
        elif ex.category == "BEVERAGE":
            est_bev += amt
        elif ex.category == "MANPOWER":
            est_manpower += amt
        elif ex.category == "EQUIPMENT":
            est_equip += amt
        elif ex.category == "DECORATION":
            est_decor += amt
        elif ex.category == "EXTERNAL_SERVICE":
            est_ext_svc += amt
        else:
            est_other += amt

        committed_from_expenses += ex.committed_amount * fx
        if ex.actual_verified_amount is not None:
            has_verified_actual_expenses = True
            actual_from_expenses += ex.actual_verified_amount * fx

    # Apply per-guest baseline F&B if not passed in expense lines
    if est_food == _ZERO and req.food_cost_per_guest > _ZERO:
        est_food = Decimal(req.planned_guests) * req.food_cost_per_guest
    if est_bev == _ZERO and req.beverage_cost_per_guest > _ZERO:
        est_bev = Decimal(req.planned_guests) * req.beverage_cost_per_guest

    var_per_guest = (est_food + est_bev) / Decimal(req.planned_guests) + var_per_guest_from_lines
    est_direct = est_food + est_bev + est_manpower + est_equip + est_decor + est_ext_svc + est_other

    # 4. Overhead allocation (duplicate-pool guarded)
    est_overhead = _ZERO
    seen_pools: set[str] = set()
    for oh in req.overhead_rules:
        if not oh.is_approved:
            continue
        pool_key = oh.source_pool_id or oh.rule_id
        if pool_key in seen_pools:
            exceptions.append({"exception_type": "DUPLICATE_OVERHEAD_ALLOCATION", "severity": "HIGH", "message": f"Duplicate overhead pool {pool_key} prevented."})
            continue
        seen_pools.add(pool_key)
        if oh.method == "FIXED_PER_EVENT" or oh.method == "MANUAL_APPROVED":
            est_overhead += oh.rate_or_amount
        elif oh.method == "PCT_NET_REVENUE":
            est_overhead += net_rev * (oh.rate_or_amount / _HUNDRED)
        elif oh.method == "PCT_DIRECT_COST":
            est_overhead += est_direct * (oh.rate_or_amount / _HUNDRED)
        elif oh.method == "PER_GUEST":
            est_overhead += Decimal(req.planned_guests) * oh.rate_or_amount
        elif oh.method in ("PER_LABOR_HOUR", "PER_KITCHEN_HOUR", "PER_VENUE_HOUR"):
            hrs = oh.driver_hours if oh.driver_hours > _ZERO else total_labor_hours
            est_overhead += hrs * oh.rate_or_amount

    fixed_total += est_overhead
    est_total = est_direct + est_overhead

    # 5. Procurement Commitments & Purchase-vs-Consumption Separation (Tests I & J)
    proposed_po_val = _ZERO
    draft_po_val = _ZERO
    approved_po_commit = _ZERO
    attributable_po_commit = _ZERO
    expected_consumption_po = _ZERO
    remaining_inv_val = _ZERO
    received_mat_val = _ZERO
    invoiced_mat_val = _ZERO
    actual_mat_consumption = _ZERO
    has_actual_mat_consumption = False

    for po in req.procurement_items:
        fx = po.exchange_rate_to_reporting if po.exchange_rate_to_reporting else (_ONE if po.currency.upper() == req.reporting_currency.upper() else None)
        if fx is None:
            exceptions.append({"exception_type": "MISSING_EXCHANGE_RATE", "severity": "CRITICAL", "message": f"Missing FX rate for PO item {po.material_code}."})
            continue
        gross_purchase = po.purchased_qty * po.unit_cost * fx
        alloc_val = po.event_allocated_qty * po.unit_cost * fx
        unalloc_qty = max(_ZERO, po.purchased_qty - po.event_allocated_qty - po.other_events_allocated_qty)
        unalloc_val = unalloc_qty * po.unit_cost * fx

        if po.po_status == "PROPOSED":
            proposed_po_val += alloc_val
        elif po.po_status == "DRAFT":
            draft_po_val += alloc_val
        else:
            approved_po_commit += gross_purchase
            attributable_po_commit += alloc_val

        expected_consumption_po += alloc_val
        remaining_inv_val += unalloc_val
        received_mat_val += po.received_qty * po.unit_cost * fx
        invoiced_mat_val += po.invoiced_value * fx
        if po.actual_consumed_valuation is not None:
            has_actual_mat_consumption = True
            actual_mat_consumption += po.actual_consumed_valuation * fx

    total_approved_commit = approved_po_commit + committed_from_expenses
    total_attributable_commit = attributable_po_commit + committed_from_expenses

    # 6. Forecast-at-Completion (Test L: Actual Incurred + Remaining Attributable Commitments + Remaining Uncommitted Estimates)
    actual_verified = req.actual_incurred_override if req.actual_incurred_override is not None else (actual_mat_consumption + actual_from_expenses)
    actual_provisional = req.actual_incurred_override is None and not (has_actual_mat_consumption and has_verified_actual_expenses)
    if req.actual_incurred_override is not None or has_actual_mat_consumption or has_verified_actual_expenses:
        remaining_commit = max(_ZERO, total_attributable_commit - actual_verified) if req.actual_incurred_override is None else total_attributable_commit
        remaining_uncommitted = (
            req.remaining_uncommitted_estimate_override
            if req.remaining_uncommitted_estimate_override is not None
            else max(_ZERO, est_total - actual_verified - remaining_commit)
        )
        fac_cost = actual_verified + remaining_commit + remaining_uncommitted
    else:
        fac_cost = est_total

    # 7. Contribution, Margins, Break-Even & Target Margin (Tests E, F, G, H, Q)
    direct_contrib = net_rev - est_direct
    contrib_after_oh = net_rev - est_total
    guests_dec = Decimal(req.planned_guests)

    margin_pct: Optional[Decimal] = None
    food_pct: Optional[Decimal] = None
    bev_pct: Optional[Decimal] = None
    labor_pct: Optional[Decimal] = None
    if net_rev > _ZERO:
        margin_pct = _q2((contrib_after_oh / net_rev) * _HUNDRED)
        food_pct = _q2((est_food / net_rev) * _HUNDRED)
        bev_pct = _q2((est_bev / net_rev) * _HUNDRED)
        labor_pct = _q2((est_manpower / net_rev) * _HUNDRED)

    rev_per_guest = _q2(net_rev / guests_dec) if guests_dec > _ZERO else None
    cost_per_guest = _q2(est_total / guests_dec) if guests_dec > _ZERO else None
    contrib_per_guest = _q2(contrib_after_oh / guests_dec) if guests_dec > _ZERO else None

    effective_price_per_guest = net_rev / Decimal(billable_guests) if billable_guests > 0 else req.selling_price_per_guest
    unit_contrib_before_fixed = effective_price_per_guest - var_per_guest
    break_even_guests: Optional[int] = None
    break_even_rev: Optional[Decimal] = None

    if step_fixed_rules:
        # Deterministic scan for step-fixed break-even
        non_step_fixed = max(_ZERO, fixed_total - sum(Decimal(math.ceil(req.planned_guests / thr)) * inc for thr, inc in step_fixed_rules))
        for g in range(1, max(req.planned_guests * 4, 2000) + 1):
            step_cost = sum(Decimal(math.ceil(g / thr)) * inc for thr, inc in step_fixed_rules)
            rev_g = Decimal(g) * effective_price_per_guest
            cost_g = non_step_fixed + step_cost + (Decimal(g) * var_per_guest)
            if rev_g >= cost_g:
                break_even_guests = g
                break_even_rev = _q2(rev_g)
                break
    elif unit_contrib_before_fixed > _ZERO:
        be = _ceil_int(fixed_total / unit_contrib_before_fixed)
        break_even_guests = be
        break_even_rev = _q2(Decimal(be) * effective_price_per_guest)

    target_ratio = req.target_contribution_margin_percent / _HUNDRED
    denom = _ONE - target_ratio
    req_net_rev = est_total / denom if denom > _ZERO else _ZERO
    req_price_per_guest = req_net_rev / guests_dec if guests_dec > _ZERO else _ZERO
    max_allowable_cost = net_rev * denom
    remaining_headroom = max_allowable_cost - est_total

    budget_variance = (est_total - req.approved_budget) if req.approved_budget is not None else None
    target_margin_var = (margin_pct - req.target_contribution_margin_percent) if margin_pct is not None else None

    if contrib_after_oh < _ZERO:
        exceptions.append({"exception_type": "NEGATIVE_CONTRIBUTION", "severity": "CRITICAL", "message": f"Negative event contribution of {req.reporting_currency} {_q2(contrib_after_oh)}."})
    if budget_variance is not None and budget_variance > _ZERO:
        exceptions.append({"exception_type": "BUDGET_OVERRUN", "severity": "HIGH", "message": f"Estimated cost exceeds approved budget by {req.reporting_currency} {_q2(budget_variance)}."})
    if target_margin_var is not None and target_margin_var < _ZERO:
        exceptions.append({"exception_type": "MARGIN_BELOW_TARGET", "severity": "WARNING", "message": f"Contribution margin {margin_pct}% is below target {req.target_contribution_margin_percent}%."})

    # 8. Price & Quantity Variance (Test K) and Guest Volume Flexible Budget
    price_var_tot = _ZERO
    qty_var_tot = _ZERO
    for vl in req.variance_lines:
        price_var_tot += vl.actual_qty * (vl.actual_unit_price - vl.standard_unit_price)
        qty_var_tot += vl.standard_unit_price * (vl.actual_qty - vl.standard_qty)
    total_mat_var = price_var_tot + qty_var_tot

    flex_food: Optional[Decimal] = None
    guest_vol_var: Optional[Decimal] = None
    if actual_g is not None and req.planned_guests > 0:
        std_food_per_guest = est_food / Decimal(req.planned_guests)
        flex_food = _q2(std_food_per_guest * Decimal(actual_g))
        guest_vol_var = _q2(flex_food - est_food)

    # 9. Revenue-to-Contribution Waterfall (§59)
    waterfall = [
        WaterfallStepDto(label="Net Event Revenue", amount=_q2(net_rev), kind="REVENUE"),
        WaterfallStepDto(label="Food Cost", amount=_q2(-est_food), kind="COST"),
        WaterfallStepDto(label="Beverage Cost", amount=_q2(-est_bev), kind="COST"),
        WaterfallStepDto(label="Manpower", amount=_q2(-est_manpower), kind="COST"),
        WaterfallStepDto(label="Decoration", amount=_q2(-est_decor), kind="COST"),
        WaterfallStepDto(label="Equipment", amount=_q2(-est_equip), kind="COST"),
        WaterfallStepDto(label="External Services", amount=_q2(-est_ext_svc), kind="COST"),
        WaterfallStepDto(label="Other Direct Expenses", amount=_q2(-est_other), kind="COST"),
        WaterfallStepDto(label="Direct Contribution", amount=_q2(direct_contrib), kind="SUBTOTAL"),
        WaterfallStepDto(label="Allocated Overhead", amount=_q2(-est_overhead), kind="OVERHEAD"),
        WaterfallStepDto(label="Event Contribution", amount=_q2(contrib_after_oh), kind="CONTRIBUTION"),
    ]

    return EventFinancialCalculateResponse(
        event_id=req.event_id,
        event_name=req.event_name,
        reporting_currency=req.reporting_currency.upper(),
        planned_guests=req.planned_guests,
        guaranteed_guests=req.guaranteed_guests,
        actual_guests=actual_g,
        billable_guests=billable_guests,
        gross_revenue=_q2(gross_rev),
        discount_total=_q2(discount_tot),
        net_revenue=_q2(net_rev),
        planned_revenue=_q2(planned_rev),
        contracted_revenue=_q2(contracted_rev),
        invoiced_revenue=_q2(invoiced_rev),
        recognized_revenue=_q2(recognized_rev),
        estimated_food_cost=_q2(est_food),
        estimated_beverage_cost=_q2(est_bev),
        estimated_manpower_cost=_q2(est_manpower),
        estimated_equipment_cost=_q2(est_equip),
        estimated_decoration_cost=_q2(est_decor),
        estimated_external_service_cost=_q2(est_ext_svc),
        estimated_other_direct_cost=_q2(est_other),
        estimated_direct_cost=_q2(est_direct),
        estimated_allocated_overhead=_q2(est_overhead),
        estimated_total_cost=_q2(est_total),
        proposed_purchase_value=_q2(proposed_po_val),
        draft_po_value=_q2(draft_po_val),
        approved_procurement_commitment=_q2(total_approved_commit),
        attributable_procurement_commitment=_q2(total_attributable_commit),
        expected_consumption_value_from_po=_q2(expected_consumption_po),
        remaining_unallocated_inventory_value=_q2(remaining_inv_val),
        received_material_value=_q2(received_mat_val),
        invoiced_material_value=_q2(invoiced_mat_val),
        actual_verified_cost=_q2(actual_verified),
        actual_is_provisional=actual_provisional,
        forecast_at_completion_cost=_q2(fac_cost),
        direct_event_contribution=_q2(direct_contrib),
        event_contribution_after_overhead=_q2(contrib_after_oh),
        contribution_margin_percent=margin_pct,
        food_cost_percent=food_pct,
        beverage_cost_percent=bev_pct,
        labor_cost_percent=labor_pct,
        revenue_per_guest=rev_per_guest,
        cost_per_guest=cost_per_guest,
        contribution_per_guest=contrib_per_guest,
        fixed_cost_total=_q2(fixed_total),
        variable_cost_per_guest=_q4(var_per_guest),
        break_even_guests=break_even_guests,
        break_even_revenue=break_even_rev,
        required_net_revenue_target_margin=_q2(req_net_rev),
        required_price_per_guest_target_margin=_q2(req_price_per_guest),
        max_allowable_total_cost=_q2(max_allowable_cost),
        remaining_cost_headroom=_q2(remaining_headroom),
        budget_variance=_q2(budget_variance) if budget_variance is not None else None,
        target_margin_variance_percent=target_margin_var,
        price_variance_total=_q2(price_var_tot),
        quantity_variance_total=_q2(qty_var_tot),
        total_material_variance=_q2(total_mat_var),
        flexible_budget_food_cost=flex_food,
        guest_volume_variance_amount=guest_vol_var,
        waterfall=waterfall,
        exceptions=exceptions,
    )


def calculate_material_price_impact(req: MaterialPriceImpactRequest) -> MaterialPriceImpactResponse:
    lines: list[MaterialPriceImpactLine] = []
    tot_cur = _ZERO
    tot_proj = _ZERO
    for item in req.affected_items:
        cur = item.required_qty * item.current_unit_price
        proj = item.required_qty * item.new_unit_price
        delta = proj - cur
        tot_cur += cur
        tot_proj += proj
        lines.append(
            MaterialPriceImpactLine(
                recipe_or_event_ref=item.recipe_or_event_ref,
                name=item.name,
                kind=item.kind,
                required_qty=_q4(item.required_qty),
                uom=item.uom,
                current_cost=_q2(cur),
                projected_cost=_q2(proj),
                cost_delta=_q2(delta),
            )
        )
    return MaterialPriceImpactResponse(
        material_code=req.material_code,
        material_description=req.material_description,
        reporting_currency=req.reporting_currency.upper(),
        total_current_cost=_q2(tot_cur),
        total_projected_cost=_q2(tot_proj),
        total_cost_delta=_q2(tot_proj - tot_cur),
        lines=lines,
    )


async def analyze_financial_with_ai(req: FinancialAiAnalyzeRequest) -> FinancialAiAnalyzeResponse:
    if any(p.search(req.question) for p in _INJECTION_PATTERNS):
        return FinancialAiAnalyzeResponse(
            answer="Blocked potential prompt-injection or unauthorized financial posting instruction. Financial postings and budget edits require authorized workflow actions.",
            provider_used="DETERMINISTIC_GUARD",
            warnings=["Prompt injection or unauthorized financial action rejected."],
            prompt_injection_blocked=True,
        )

    ctx = req.event_context or {}
    ccy = str(ctx.get("currency") or "AED")
    net_rev = ctx.get("net_revenue")
    tot_cost = ctx.get("estimated_total_cost")
    contrib = ctx.get("event_contribution")
    margin = ctx.get("contribution_margin_percent")
    be = ctx.get("break_even_guests")
    req_price = ctx.get("required_selling_price_target_margin")
    headroom = ctx.get("remaining_cost_headroom")
    po_commit = ctx.get("approved_procurement_commitment")
    fac = ctx.get("forecast_at_completion_cost")
    top_drivers = ctx.get("top_cost_drivers") or []

    facts = [
        f"Net Revenue: {ccy} {net_rev}" if net_rev is not None else "Net Revenue: unavailable",
        f"Estimated Total Cost: {ccy} {tot_cost}" if tot_cost is not None else "Estimated Total Cost: unavailable",
        f"Event Contribution: {ccy} {contrib} ({margin}% margin)" if contrib is not None else "Event Contribution: unavailable",
        f"Break-Even Guests: {be}" if be is not None else "Break-Even Guests: N/A",
        f"Required Price for Target Margin: {ccy} {req_price}" if req_price is not None else "Required Target Price: unavailable",
        f"Remaining Cost Headroom: {ccy} {headroom}" if headroom is not None else "Cost Headroom: unavailable",
        f"Procurement Commitment: {ccy} {po_commit}; Forecast-at-Completion: {ccy} {fac}" if fac is not None else "Forecast-at-Completion: unavailable",
    ]
    if top_drivers:
        facts.append("Top Cost Drivers: " + ", ".join(str(d) for d in top_drivers[:5]))

    recs: list[dict[str, Any]] = []
    if margin is not None and ctx.get("target_margin_percent") is not None:
        try:
            if Decimal(str(margin)) < Decimal(str(ctx["target_margin_percent"])):
                recs.append(
                    {
                        "recommendation": "Contribution margin is below target; review high-cost recipe ingredients or selling price.",
                        "current_value": f"{margin}%",
                        "target_value": f"{ctx['target_margin_percent']}%",
                        "requires_approval": True,
                    }
                )
        except Exception:
            pass
    if ctx.get("excess_purchase_value") is not None:
        recs.append(
            {
                "recommendation": "Procurement expenditure includes pack/MOQ stock retained in inventory rather than consumed by this event.",
                "current_value": f"{ccy} {ctx['excess_purchase_value']}",
                "requires_approval": False,
            }
        )

    deterministic_summary = (
        "Grounded Financial Analysis (Deterministic Verified Context): "
        + " | ".join(facts)
        + ". Note: AI cannot post accounting entries or modify approved budgets."
    )

    if req.mode == "LOCAL" and not req.base_url:
        return FinancialAiAnalyzeResponse(
            answer=deterministic_summary,
            provider_used="DETERMINISTIC_LOCAL",
            grounded_facts=facts,
            recommendations=recs,
        )

    if req.mode in ("GEMINI", "CLAUDE") and not req.allow_external_ai:
        return FinancialAiAnalyzeResponse(
            answer=deterministic_summary,
            provider_used="DETERMINISTIC_LOCAL",
            grounded_facts=facts,
            recommendations=recs,
            warnings=["External AI disabled by tenant policy; returned deterministic verified explanation."],
        )

    router = AiProviderRouter(
        local=LocalOllamaAdapter(base_url=req.base_url or "http://127.0.0.1:11434"),
        gemini=GeminiSdkAdapter(api_key=req.api_key or "") if req.api_key else None,
        claude=ClaudeSdkAdapter(api_key=req.api_key or "") if req.api_key else None,
    )
    prompt = (
        "You are SILA Culinary Financial Intelligence Assistant. "
        "Explain ONLY using these verified facts without inventing numbers or recommending SAP/GL postings:\n"
        + "\n".join(f"- {f}" for f in facts)
        + f"\nQuestion: {req.question}"
    )
    try:
        res = await router.execute(
            AiExecutionRequest(
                task_name=req.task_name,
                mode=req.mode,
                prompt=prompt,
                allow_external=req.allow_external_ai,
                model_name=req.model_name,
                max_output_tokens=600,
                temperature=0.1,
            )
        )
        return FinancialAiAnalyzeResponse(
            answer=res.text or deterministic_summary,
            provider_used=res.provider,
            grounded_facts=facts,
            recommendations=recs,
        )
    except (ProviderUnavailableError, Exception) as ex:
        return FinancialAiAnalyzeResponse(
            answer=deterministic_summary,
            provider_used="DETERMINISTIC_FALLBACK",
            grounded_facts=facts,
            recommendations=recs,
            warnings=[f"AI provider unavailable ({type(ex).__name__}); deterministic summary returned."],
        )
