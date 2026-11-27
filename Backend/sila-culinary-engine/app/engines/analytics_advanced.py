from __future__ import annotations

from datetime import datetime, timedelta, timezone
from decimal import Decimal, ROUND_HALF_UP
import math
import re
from typing import Any

import numpy as np
from scipy.optimize import LinearConstraint, milp, Bounds
from sklearn.linear_model import Ridge

from app.schemas.analytics_engine import (
    BuffetDishRecommendation,
    BuffetOptimizeRequest,
    BuffetOptimizeResponse,
    ConsumptionVarianceRequest,
    ConsumptionVarianceResponse,
    ConsumptionVarianceResultRow,
    CopilotQueryRequest,
    CopilotQueryResponse,
    DemandForecastRequest,
    DemandForecastResponse,
    EventPackageOption,
    EventPackageOptimizeRequest,
    EventPackageOptimizeResponse,
    ForecastPoint,
    MenuEngineeringAnalyzeRequest,
    MenuEngineeringAnalyzeResponse,
    MenuEngineeringItemResult,
    MenuOptimizationRequest,
    MenuOptimizationResponse,
    MenuOptimizationSelection,
    PropertyBenchmarkRequest,
    PropertyBenchmarkResponse,
    PropertyBenchmarkRow,
    ReversePriceImpactNodeResult,
    ReversePriceImpactRequest,
    ReversePriceImpactResponse,
    SubstitutionAnalysisRequest,
    SubstitutionAnalysisResponse,
    SubstitutionRecommendationResult,
    WasteAnalyzeRequest,
    WasteAnalyzeResponse,
    WhatIfScenarioOutput,
    WhatIfSimulationRequest,
    WhatIfSimulationResponse,
)

MONEY_Q = Decimal("0.01")
QTY_Q = Decimal("0.0001")
PCT_Q = Decimal("0.01")


def _d(val: float | int | Decimal | None) -> Decimal:
    if val is None:
        return Decimal("0")
    return Decimal(str(val))


def _r2(val: Decimal) -> float:
    return float(val.quantize(MONEY_Q, rounding=ROUND_HALF_UP))


def _r4(val: Decimal) -> float:
    return float(val.quantize(QTY_Q, rounding=ROUND_HALF_UP))


def _now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


# ---------------------------------------------------------------------------
# 1. Menu Engineering & Item Profitability
# ---------------------------------------------------------------------------

def analyze_menu_engineering(req: MenuEngineeringAnalyzeRequest) -> MenuEngineeringAnalyzeResponse:
    warnings: list[str] = []
    if not req.items:
        return MenuEngineeringAnalyzeResponse(
            currency=req.currency,
            popularity_threshold_percent=0.0,
            contribution_margin_threshold=0.0,
            weighted_avg_contribution_margin=0.0,
            total_quantity_sold=0.0,
            total_net_revenue=0.0,
            total_contribution_margin=0.0,
            counts={"STAR": 0, "PLOWHORSE": 0, "PUZZLE": 0, "DOG": 0, "INSUFFICIENT_DATA": 0},
            items=[],
            warnings=["No menu items with POS sales or recipe records matched the selected filter."],
        )

    total_qty = sum((_d(i.quantity_sold) for i in req.items), Decimal("0"))

    # Compute per-item financials first using exact Decimal arithmetic (no fixed overhead inside CM)
    prepared: list[dict[str, Any]] = []
    sufficient_items: list[dict[str, Any]] = []

    for item in req.items:
        qty = _d(item.quantity_sold)
        gross = _d(item.gross_selling_price)
        disc = _d(item.discount_amount)
        tax_rate = _d(item.tax_rate_percent) / Decimal("100")
        net_unit_price = (gross - disc) / (Decimal("1") + tax_rate) if (Decimal("1") + tax_rate) > 0 else (gross - disc)
        if net_unit_price < 0:
            net_unit_price = Decimal("0")

        net_rev = _d(item.net_revenue) if item.net_revenue is not None else net_unit_price * qty
        avg_price = (net_rev / qty) if qty > 0 else net_unit_price

        # Choose variable recipe/food cost according to methodology without fabricating missing cost
        raw_cost: Decimal | None = None
        if item.methodology == "ACTUAL" and item.actual_ingredient_cost is not None:
            raw_cost = _d(item.actual_ingredient_cost)
        elif item.standard_recipe_cost is not None:
            raw_cost = _d(item.standard_recipe_cost)
        elif item.actual_ingredient_cost is not None:
            raw_cost = _d(item.actual_ingredient_cost)

        variable_extra = _d(item.garnish_cost) + _d(item.packaging_cost) + _d(item.direct_labor_cost)
        overhead = _d(item.allocated_overhead)

        if raw_cost is not None:
            unit_var_cost = raw_cost + variable_extra
            cm_unit = avg_price - unit_var_cost
            total_cm = cm_unit * qty
            food_cost_pct = ((raw_cost / avg_price) * Decimal("100")) if avg_price > 0 else None
            fully_loaded_unit = avg_price - unit_var_cost - overhead
            fully_loaded_total = fully_loaded_unit * qty
        else:
            cm_unit = None
            total_cm = None
            food_cost_pct = None
            fully_loaded_unit = None
            fully_loaded_total = None

        has_enough_sales = (
            item.has_sufficient_history
            and qty >= _d(req.min_sales_for_classification)
            and raw_cost is not None
        )

        row = {
            "item": item,
            "qty": qty,
            "net_rev": net_rev,
            "avg_price": avg_price,
            "raw_cost": raw_cost,
            "food_cost_pct": food_cost_pct,
            "cm_unit": cm_unit,
            "total_cm": total_cm,
            "fully_loaded_unit": fully_loaded_unit,
            "fully_loaded_total": fully_loaded_total,
            "has_enough_sales": has_enough_sales,
        }
        prepared.append(row)
        if has_enough_sales and cm_unit is not None:
            sufficient_items.append(row)

    comparable_count = len(sufficient_items) if sufficient_items else len(prepared)
    comparable_qty = sum((r["qty"] for r in sufficient_items), Decimal("0"))
    comparable_cm_sum = sum((r["total_cm"] for r in sufficient_items if r["total_cm"] is not None), Decimal("0"))

    weighted_avg_cm = (comparable_cm_sum / comparable_qty) if comparable_qty > 0 else Decimal("0")
    cm_threshold = _d(req.contribution_threshold_override) if req.contribution_threshold_override is not None else weighted_avg_cm

    pop_threshold_pct = (
        (Decimal("100") / Decimal(str(comparable_count))) * _d(req.popularity_threshold_factor)
        if comparable_count > 0
        else Decimal("0")
    )

    total_rev = sum((r["net_rev"] for r in prepared), Decimal("0"))
    total_cm_all = sum((r["total_cm"] for r in prepared if r["total_cm"] is not None), Decimal("0"))

    counts = {"STAR": 0, "PLOWHORSE": 0, "PUZZLE": 0, "DOG": 0, "INSUFFICIENT_DATA": 0}
    results: list[MenuEngineeringItemResult] = []
    ts_default = _now_iso()

    for r in prepared:
        item = r["item"]
        qty = r["qty"]
        pop_pct = ((qty / total_qty) * Decimal("100")) if total_qty > 0 else Decimal("0")
        rev_contrib_pct = ((r["net_rev"] / total_rev) * Decimal("100")) if total_rev > 0 else Decimal("0")
        profit_contrib_pct = (
            ((r["total_cm"] / total_cm_all) * Decimal("100"))
            if (r["total_cm"] is not None and total_cm_all > 0)
            else None
        )

        if not r["has_enough_sales"] or r["cm_unit"] is None:
            quadrant = "INSUFFICIENT_DATA"
            action = (
                "Insufficient POS sales history or missing recipe cost; monitor before classifying."
                if r["raw_cost"] is not None
                else "Link an effective-dated recipe version to compute accurate cost and margin."
            )
        else:
            high_pop = pop_pct >= pop_threshold_pct
            high_cm = r["cm_unit"] >= cm_threshold
            if high_pop and high_cm:
                quadrant = "STAR"
                action = "Maintain quality, feature prominently on menu, and protect portion consistency."
            elif high_pop and not high_cm:
                quadrant = "PLOWHORSE"
                action = "Review selling price upward slightly, optimize recipe cost, or pair with high-margin sides."
            elif not high_pop and high_cm:
                quadrant = "PUZZLE"
                action = "Promote visibility via server upsell, repositioning, or targeted combo offers."
            else:
                quadrant = "DOG"
                action = "Review recipe viability, re-engineer ingredient cost, or consider seasonal removal."

        counts[quadrant] += 1
        movement = "NEW" if item.previous_quadrant is None else (
            "UNCHANGED" if item.previous_quadrant == quadrant else f"{item.previous_quadrant}->{quadrant}"
        )

        results.append(
            MenuEngineeringItemResult(
                menu_item_id=item.menu_item_id,
                menu_item_code=item.menu_item_code,
                name=item.name,
                category=item.category,
                meal_period=item.meal_period,
                outlet_name=item.outlet_name,
                property_name=item.property_name,
                quantity_sold=_r4(qty),
                net_sales_revenue=_r2(r["net_rev"]),
                average_selling_price=_r2(r["avg_price"]),
                standard_recipe_cost=_r2(_d(item.standard_recipe_cost)) if item.standard_recipe_cost is not None else None,
                actual_ingredient_cost=_r2(_d(item.actual_ingredient_cost)) if item.actual_ingredient_cost is not None else None,
                food_cost_percent=_r2(r["food_cost_pct"]) if r["food_cost_pct"] is not None else None,
                contribution_margin_per_item=_r2(r["cm_unit"]) if r["cm_unit"] is not None else None,
                total_contribution_margin=_r2(r["total_cm"]) if r["total_cm"] is not None else None,
                fully_loaded_profit=_r2(r["fully_loaded_total"]) if r["fully_loaded_total"] is not None else None,
                profit_per_portion=_r2(r["fully_loaded_unit"]) if r["fully_loaded_unit"] is not None else None,
                popularity_percent=_r2(pop_pct),
                sales_mix_percent=_r2(pop_pct),
                revenue_contribution_percent=_r2(rev_contrib_pct),
                profit_contribution_percent=_r2(profit_contrib_pct) if profit_contrib_pct is not None else None,
                quadrant=quadrant,  # type: ignore[arg-type]
                previous_quadrant=item.previous_quadrant,
                movement=movement,
                cost_source=item.cost_source,
                recipe_version=item.recipe_version,
                cost_timestamp_utc=item.cost_timestamp_utc or ts_default,
                recommended_action=action,
            )
        )

    if counts["INSUFFICIENT_DATA"] > 0:
        warnings.append(
            f"{counts['INSUFFICIENT_DATA']} menu item(s) marked INSUFFICIENT_DATA instead of DOG due to low sales history or missing recipe cost."
        )

    return MenuEngineeringAnalyzeResponse(
        currency=req.currency,
        popularity_threshold_percent=_r2(pop_threshold_pct),
        contribution_margin_threshold=_r2(cm_threshold),
        weighted_avg_contribution_margin=_r2(weighted_avg_cm),
        total_quantity_sold=_r4(total_qty),
        total_net_revenue=_r2(total_rev),
        total_contribution_margin=_r2(total_cm_all),
        counts=counts,
        items=results,
        warnings=warnings,
    )


# ---------------------------------------------------------------------------
# 2. Theoretical vs Actual Consumption Variance (No double-counting wastage)
# ---------------------------------------------------------------------------

def analyze_consumption_variance(req: ConsumptionVarianceRequest) -> ConsumptionVarianceResponse:
    rows_out: list[ConsumptionVarianceResultRow] = []
    tot_theo = Decimal("0")
    tot_act = Decimal("0")
    tot_waste = Decimal("0")
    tot_unexp = Decimal("0")
    tot_fin = Decimal("0")

    for r in req.rows:
        sales = _d(r.sales_quantity)
        qty_per_portion = _d(r.recipe_qty_per_portion)
        portion_factor = _d(r.portion_factor)
        # Apply yield factor only once
        yield_factor = Decimal("1") if r.already_yield_adjusted else (_d(r.yield_factor) if r.yield_factor > 0 else Decimal("1"))
        theoretical = sales * qty_per_portion * portion_factor * yield_factor

        if r.reconciled_actual_override is not None:
            reconciled_actual = _d(r.reconciled_actual_override)
        else:
            # Reconcile gross issues by removing returns and inter-outlet transfers
            reconciled_actual = max(
                Decimal("0"),
                _d(r.gross_stock_issues) - _d(r.stock_returns) - _d(r.inter_outlet_transfers_out),
            )

        recorded_waste = max(Decimal("0"), _d(r.recorded_wastage))
        total_variance = reconciled_actual - theoretical
        # Prevent double-counting: subtract recorded wastage from total variance to isolate unexplained variance
        unexplained = total_variance - recorded_waste

        var_pct = ((total_variance / theoretical) * Decimal("100")) if theoretical > 0 else None
        unit_cost = _d(r.unit_cost)
        financial_var = total_variance * unit_cost

        # Decompose yield vs portion candidates cleanly
        base_unadj = sales * qty_per_portion
        yield_var_qty = theoretical - (base_unadj * portion_factor)
        portion_var_qty = theoretical - (base_unadj * yield_factor)

        causes: list[str] = []
        if not r.pos_mapped:
            causes.append("Possible POS mapping error")
        if not r.uom_verified:
            causes.append("Possible incorrect UOM conversion")
        if recorded_waste > 0:
            causes.append("Recorded production/spoilage wastage accounted separately")
        if unexplained > Decimal("0.001"):
            causes.extend([
                "Possible over-portioning",
                "Possible recipe deviation",
                "Possible unrecorded consumption or stock discrepancy",
            ])
        elif unexplained < Decimal("-0.001"):
            causes.extend([
                "Possible under-portioning",
                "Possible unposted stock issue or recipe mapping mismatch",
            ])

        exceeds = var_pct is not None and abs(var_pct) > _d(req.tolerance_percent)

        tot_theo += theoretical
        tot_act += reconciled_actual
        tot_waste += recorded_waste
        tot_unexp += unexplained
        tot_fin += financial_var

        rows_out.append(
            ConsumptionVarianceResultRow(
                material_id=r.material_id,
                material_code=r.material_code,
                material_name=r.material_name,
                uom=r.uom,
                recipe_version=r.recipe_version,
                theoretical_quantity=_r4(theoretical),
                reconciled_actual_quantity=_r4(reconciled_actual),
                recorded_wastage=_r4(recorded_waste),
                quantity_variance=_r4(total_variance),
                unexplained_variance=_r4(unexplained),
                variance_percent=_r2(var_pct) if var_pct is not None else None,
                financial_variance=_r2(financial_var),
                yield_variance_qty=_r4(yield_var_qty),
                portion_variance_qty=_r4(portion_var_qty),
                possible_causes=causes,
                exceeds_tolerance=exceeds,
            )
        )

    return ConsumptionVarianceResponse(
        currency=req.currency,
        total_theoretical_qty=_r4(tot_theo),
        total_actual_qty=_r4(tot_act),
        total_recorded_wastage=_r4(tot_waste),
        total_unexplained_qty=_r4(tot_unexp),
        total_financial_variance=_r2(tot_fin),
        rows=rows_out,
    )


# ---------------------------------------------------------------------------
# 3. Reverse Ingredient Price Impact Engine
# ---------------------------------------------------------------------------

def analyze_reverse_price_impact(req: ReversePriceImpactRequest) -> ReversePriceImpactResponse:
    prev = _d(req.previous_price)
    curr = _d(req.current_price)
    price_delta = curr - prev
    pct_change = ((price_delta / prev) * Decimal("100")) if prev > 0 else None
    alert_triggered = pct_change is not None and pct_change >= _d(req.alert_threshold_percent)

    impacts: list[ReversePriceImpactNodeResult] = []
    counts: dict[str, int] = {"SUB_RECIPE": 0, "RECIPE": 0, "MENU_ITEM": 0, "BUFFET": 0, "EVENT": 0}
    tot_monthly = Decimal("0")

    for n in req.nodes:
        counts[n.node_type] = counts.get(n.node_type, 0) + 1
        qty_per_unit = _d(n.qty_per_unit)
        prev_unit = _d(n.current_unit_cost)
        unit_delta = price_delta * qty_per_unit
        rev_unit = max(Decimal("0"), prev_unit + unit_delta)
        selling = _d(n.selling_price)
        prev_fc = ((prev_unit / selling) * Decimal("100")) if selling > 0 else None
        rev_fc = ((rev_unit / selling) * Decimal("100")) if selling > 0 else None
        monthly_impact = unit_delta * _d(n.monthly_sales_volume)
        annual_impact = monthly_impact * Decimal("12")

        # Count menu-facing nodes in aggregate financial impact to avoid double-counting sub-recipes + recipes + menu items
        if n.node_type in ("MENU_ITEM", "BUFFET", "EVENT"):
            tot_monthly += monthly_impact

        impacts.append(
            ReversePriceImpactNodeResult(
                node_type=n.node_type,
                node_id=n.node_id,
                node_name=n.node_name,
                property_name=n.property_name,
                outlet_name=n.outlet_name,
                via_sub_recipe=n.via_sub_recipe,
                previous_unit_cost=_r2(prev_unit),
                revised_unit_cost=_r2(rev_unit),
                unit_cost_delta=_r2(unit_delta),
                previous_food_cost_pct=_r2(prev_fc) if prev_fc is not None else None,
                revised_food_cost_pct=_r2(rev_fc) if rev_fc is not None else None,
                margin_impact_per_unit=_r2(-unit_delta),
                estimated_monthly_impact=_r2(monthly_impact),
                estimated_annual_impact=_r2(annual_impact),
            )
        )

    if tot_monthly == 0 and impacts:
        tot_monthly = sum((_d(i.estimated_monthly_impact) for i in impacts), Decimal("0"))

    return ReversePriceImpactResponse(
        currency=req.currency,
        material_code=req.material_code,
        material_name=req.material_name,
        price_delta=_r2(price_delta),
        price_change_percent=_r2(pct_change) if pct_change is not None else None,
        alert_triggered=alert_triggered,
        affected_counts=counts,
        total_monthly_impact=_r2(tot_monthly),
        total_annual_impact=_r2(tot_monthly * Decimal("12")),
        impacts=impacts,
    )


# ---------------------------------------------------------------------------
# 4. Isolated What-If Simulation Engine
# ---------------------------------------------------------------------------

def simulate_what_if(req: WhatIfSimulationRequest) -> WhatIfSimulationResponse:
    results: list[WhatIfScenarioOutput] = []
    baseline_profit: Decimal | None = None
    baseline_cost: Decimal | None = None

    for idx, sc in enumerate(req.scenarios):
        covers = _d(sc.covers)
        price_per_cover = _d(req.base_selling_price_per_cover) * (Decimal("1") + _d(sc.selling_price_delta_pct) / Decimal("100"))
        net_price_per_cover = price_per_cover * (Decimal("1") - _d(sc.discount_pct) / Decimal("100"))
        net_revenue = net_price_per_cover * covers

        # Ingredient price, portion, substitution, wastage & yield effects
        yield_ratio = Decimal("88") / max(Decimal("20"), _d(sc.yield_pct))
        waste_multiplier = Decimal("1") + (_d(sc.wastage_pct) - Decimal("4")) / Decimal("100")
        unit_food = (
            _d(req.base_food_cost_per_cover)
            * (Decimal("1") + _d(sc.ingredient_price_delta_pct) / Decimal("100"))
            * (Decimal("1") + _d(sc.portion_size_delta_pct) / Decimal("100"))
            * (Decimal("1") - _d(sc.substitution_savings_pct) / Decimal("100"))
            * yield_ratio
            * waste_multiplier
            * _d(sc.buffet_replenishment_factor)
        )
        revised_food_cost = max(Decimal("0"), unit_food * covers)
        revised_labor = _d(req.base_labor_cost) * (Decimal("1") + _d(sc.labor_cost_delta_pct) / Decimal("100"))
        total_cost = revised_food_cost + revised_labor + _d(req.base_fixed_expenses) + _d(sc.overhead_amount)

        # Contribution margin = Revenue - Variable costs (Food + Direct Labor)
        cm = net_revenue - (revised_food_cost + revised_labor)
        cm_pct = ((cm / net_revenue) * Decimal("100")) if net_revenue > 0 else None
        fc_pct = ((revised_food_cost / net_revenue) * Decimal("100")) if net_revenue > 0 else None

        unit_cm = (cm / covers) if covers > 0 else Decimal("0")
        fixed_total = _d(req.base_fixed_expenses) + _d(sc.overhead_amount)
        be_covers = math.ceil(float(fixed_total / unit_cm)) if unit_cm > 0 else None

        target_fc_ratio = _d(req.target_food_cost_pct) / Decimal("100")
        target_price = (unit_food / target_fc_ratio) if target_fc_ratio > 0 else None

        profit = net_revenue - total_cost
        if idx == 0:
            baseline_profit = profit
            baseline_cost = total_cost

        results.append(
            WhatIfScenarioOutput(
                scenario_name=sc.scenario_name,
                covers=float(covers),
                net_revenue=_r2(net_revenue),
                revised_food_cost=_r2(revised_food_cost),
                revised_food_cost_pct=_r2(fc_pct) if fc_pct is not None else None,
                revised_labor_cost=_r2(revised_labor),
                total_cost=_r2(total_cost),
                contribution_margin=_r2(cm),
                contribution_margin_pct=_r2(cm_pct) if cm_pct is not None else None,
                break_even_covers=be_covers,
                target_selling_price_for_food_cost=_r2(target_price) if target_price is not None else None,
                expected_savings_vs_baseline=_r2((baseline_cost - total_cost) if baseline_cost is not None else Decimal("0")),
                profitability_delta_vs_baseline=_r2((profit - baseline_profit) if baseline_profit is not None else Decimal("0")),
            )
        )

    return WhatIfSimulationResponse(currency=req.currency, isolated_from_production=True, results=results)


# ---------------------------------------------------------------------------
# 5. Python MILP Menu Optimization Engine (Hard Allergen & Dietary Constraints)
# ---------------------------------------------------------------------------

def optimize_menu_milp(req: MenuOptimizationRequest) -> MenuOptimizationResponse:
    if not req.candidates:
        return MenuOptimizationResponse(
            status="INFEASIBLE",
            currency=req.currency,
            objective=req.objective,
            baseline_cost=0.0,
            baseline_contribution=0.0,
            proposed_cost=0.0,
            proposed_revenue=0.0,
            proposed_contribution=0.0,
            expected_savings=0.0,
            expected_margin_improvement_pct=0.0,
            infeasibility_reasons=["No candidate dishes were supplied."],
            explanation="Optimization cannot run without candidate menu items.",
        )

    excluded = {a.strip().lower() for a in req.excluded_allergens if a.strip()}
    required_diet = {d.strip().lower() for d in req.required_dietary_tags if d.strip()}

    feasible_candidates = []
    blocked_reasons: list[str] = []
    for c in req.candidates:
        c_allergens = {a.strip().lower() for a in c.allergens}
        c_diet = {d.strip().lower() for d in c.dietary_tags}
        if req.strict_allergen_safety and not c.allergens_verified:
            blocked_reasons.append(f"{c.name}: unverified allergen profile blocked under strict safety policy.")
            continue
        if excluded and (excluded & c_allergens):
            blocked_reasons.append(f"{c.name}: contains excluded allergen(s) {', '.join(sorted(excluded & c_allergens))}.")
            continue
        if required_diet and not required_diet.issubset(c_diet):
            blocked_reasons.append(f"{c.name}: missing required dietary tag(s) {', '.join(sorted(required_diet - c_diet))}.")
            continue
        if not c.approved_ingredients_only:
            blocked_reasons.append(f"{c.name}: contains unapproved ingredients.")
            continue
        feasible_candidates.append(c)

    # Baseline calculation across all supplied candidates at midpoint/min
    base_portions = [max(c.min_portions, min(c.max_portions, max(c.min_expected_demand, req.total_target_portions // len(req.candidates)))) for c in req.candidates]
    base_cost = sum(p * c.unit_cost for p, c in zip(base_portions, req.candidates))
    base_cm = sum(p * (c.selling_price - c.unit_cost) for p, c in zip(base_portions, req.candidates))

    if not feasible_candidates:
        return MenuOptimizationResponse(
            status="INFEASIBLE",
            currency=req.currency,
            objective=req.objective,
            baseline_cost=round(base_cost, 2),
            baseline_contribution=round(base_cm, 2),
            proposed_cost=0.0,
            proposed_revenue=0.0,
            proposed_contribution=0.0,
            expected_savings=0.0,
            expected_margin_improvement_pct=0.0,
            constraint_violations=blocked_reasons,
            infeasibility_reasons=[
                "All candidate dishes were filtered out by mandatory allergen, dietary, or approved-ingredient safety constraints.",
                *blocked_reasons,
            ],
            explanation="Safety constraints are hard constraints and were not relaxed.",
        )

    n = len(feasible_candidates)
    lb = np.array([max(c.min_portions, c.min_expected_demand) for c in feasible_candidates], dtype=float)
    ub = np.array([max(c.min_portions, c.max_portions) for c in feasible_candidates], dtype=float)

    if np.sum(ub) < req.total_target_portions:
        return MenuOptimizationResponse(
            status="INFEASIBLE",
            currency=req.currency,
            objective=req.objective,
            baseline_cost=round(base_cost, 2),
            baseline_contribution=round(base_cm, 2),
            proposed_cost=0.0,
            proposed_revenue=0.0,
            proposed_contribution=0.0,
            expected_savings=0.0,
            expected_margin_improvement_pct=0.0,
            constraint_violations=blocked_reasons,
            infeasibility_reasons=[
                f"Maximum safe capacity across eligible dishes ({int(np.sum(ub))} portions) is below required target ({req.total_target_portions} portions)."
            ],
            explanation="Increase eligible menu items or portion upper bounds without relaxing allergen/dietary safety.",
        )

    # Objective vector (scipy milp minimizes c^T x)
    if req.objective == "MAX_CONTRIBUTION":
        obj = np.array([-(c.selling_price - c.unit_cost) for c in feasible_candidates], dtype=float)
    elif req.objective == "MIN_FOOD_COST":
        obj = np.array([c.unit_cost for c in feasible_candidates], dtype=float)
    elif req.objective == "MIN_WASTE":
        obj = np.array([c.wastage_risk_score * c.unit_cost for c in feasible_candidates], dtype=float)
    else:  # MAX_UTILIZATION
        obj = np.array([-c.ingredient_utilization_score for c in feasible_candidates], dtype=float)

    constraints: list[LinearConstraint] = []
    # 1. Total target portions == total_target_portions
    constraints.append(LinearConstraint(np.ones((1, n)), req.total_target_portions, req.total_target_portions))

    # 2. Target food cost % constraint: sum((unit_cost - target_ratio * selling_price) * x) <= 0
    if req.target_food_cost_pct is not None and req.target_food_cost_pct > 0:
        ratio = req.target_food_cost_pct / 100.0
        fc_row = np.array([[c.unit_cost - ratio * c.selling_price for c in feasible_candidates]], dtype=float)
        constraints.append(LinearConstraint(fc_row, -np.inf, 0.0))

    # 3. Budget limit
    if req.max_budget is not None and req.max_budget > 0:
        b_row = np.array([[c.unit_cost for c in feasible_candidates]], dtype=float)
        constraints.append(LinearConstraint(b_row, -np.inf, req.max_budget))

    # 4. Kitchen capacity limit
    if req.max_kitchen_minutes is not None and req.max_kitchen_minutes > 0:
        k_row = np.array([[c.prep_minutes_per_portion for c in feasible_candidates]], dtype=float)
        constraints.append(LinearConstraint(k_row, -np.inf, req.max_kitchen_minutes))

    # 5. Category balance minimums
    for cat, min_qty in req.category_minimums.items():
        if min_qty > 0:
            c_row = np.array([[1.0 if c.category.lower() == cat.lower() else 0.0 for c in feasible_candidates]], dtype=float)
            constraints.append(LinearConstraint(c_row, float(min_qty), np.inf))

    res = milp(
        c=obj,
        integrality=np.ones(n, dtype=int),
        bounds=Bounds(lb, ub),
        constraints=constraints,
    )

    if not res.success or res.x is None:
        reasons = [res.message]
        if req.target_food_cost_pct is not None:
            min_possible_fc = min((c.unit_cost / c.selling_price * 100.0) if c.selling_price > 0 else 100.0 for c in feasible_candidates)
            if min_possible_fc > req.target_food_cost_pct:
                reasons.append(
                    f"Lowest achievable safe dish food cost is {min_possible_fc:.2f}%, exceeding target {req.target_food_cost_pct:.2f}%."
                )
        if req.max_budget is not None:
            min_budget = float(np.sum(np.sort([c.unit_cost for c in feasible_candidates])[:1]) * req.total_target_portions)
            if min_budget > req.max_budget:
                reasons.append(f"Minimum cost for {req.total_target_portions} safe portions exceeds budget {req.max_budget:.2f}.")
        return MenuOptimizationResponse(
            status="INFEASIBLE",
            currency=req.currency,
            objective=req.objective,
            baseline_cost=round(base_cost, 2),
            baseline_contribution=round(base_cm, 2),
            proposed_cost=0.0,
            proposed_revenue=0.0,
            proposed_contribution=0.0,
            expected_savings=0.0,
            expected_margin_improvement_pct=0.0,
            constraint_violations=blocked_reasons,
            infeasibility_reasons=reasons,
            explanation="MILP solver found no feasible solution under current safety, budget, kitchen, and food-cost constraints.",
        )

    selections: list[MenuOptimizationSelection] = []
    prop_cost = Decimal("0")
    prop_rev = Decimal("0")
    for c, qty_f in zip(feasible_candidates, res.x):
        qty = int(round(float(qty_f)))
        if qty <= 0:
            continue
        t_cost = _d(qty) * _d(c.unit_cost)
        t_rev = _d(qty) * _d(c.selling_price)
        t_cm = t_rev - t_cost
        prop_cost += t_cost
        prop_rev += t_rev
        selections.append(
            MenuOptimizationSelection(
                item_id=c.item_id,
                name=c.name,
                category=c.category,
                recommended_portions=qty,
                unit_cost=_r2(_d(c.unit_cost)),
                selling_price=_r2(_d(c.selling_price)),
                total_cost=_r2(t_cost),
                total_revenue=_r2(t_rev),
                total_contribution=_r2(t_cm),
            )
        )

    prop_cm = prop_rev - prop_cost
    prop_fc_pct = ((prop_cost / prop_rev) * Decimal("100")) if prop_rev > 0 else None
    savings = _d(base_cost) - prop_cost
    margin_imp = (((prop_cm - _d(base_cm)) / abs(_d(base_cm))) * Decimal("100")) if base_cm != 0 else Decimal("0")

    return MenuOptimizationResponse(
        status="OPTIMAL",
        currency=req.currency,
        objective=req.objective,
        baseline_cost=_r2(_d(base_cost)),
        baseline_contribution=_r2(_d(base_cm)),
        proposed_cost=_r2(prop_cost),
        proposed_revenue=_r2(prop_rev),
        proposed_contribution=_r2(prop_cm),
        proposed_food_cost_pct=_r2(prop_fc_pct) if prop_fc_pct is not None else None,
        expected_savings=_r2(savings),
        expected_margin_improvement_pct=_r2(margin_imp),
        constraint_violations=blocked_reasons,
        assumptions=[
            "Allergen and dietary constraints enforced as hard MILP filters.",
            "Integer portion counts solved via SciPy HiGHS MILP.",
        ],
        explanation=(
            f"Selected {len(selections)} safe dish(es) totaling {req.total_target_portions} portions under {req.objective}."
        ),
        selections=selections,
    )


# ---------------------------------------------------------------------------
# 6. Controlled Ingredient Substitution Intelligence
# ---------------------------------------------------------------------------

def analyze_substitutions(req: SubstitutionAnalysisRequest) -> SubstitutionAnalysisResponse:
    orig_yield = max(Decimal("10"), _d(req.original_yield_percent)) / Decimal("100")
    req_qty = _d(req.required_qty)
    orig_cost = (_d(req.original_unit_cost) * req_qty) / orig_yield
    excluded = {a.strip().lower() for a in req.excluded_allergens if a.strip()}
    required_diet = {d.strip().lower() for d in req.required_dietary_tags if d.strip()}

    recs: list[SubstitutionRecommendationResult] = []
    for cand in req.candidates:
        c_yield = max(Decimal("10"), _d(cand.yield_percent)) / Decimal("100")
        sub_cost = (_d(cand.unit_cost) * req_qty) / c_yield
        saving = orig_cost - sub_cost
        revised_recipe = max(Decimal("0"), _d(req.recipe_current_cost) - saving)
        yield_impact = _d(cand.yield_percent) - _d(req.original_yield_percent)

        c_allergens = {a.strip().lower() for a in cand.allergens}
        c_diet = {d.strip().lower() for d in cand.dietary_tags}

        auto_blocked = False
        blocking_reason: str | None = None

        if cand.restricted:
            classification = "RESTRICTED_SUBSTITUTE"
            auto_blocked = True
            blocking_reason = "Ingredient is restricted by culinary policy."
        elif not cand.allergen_verified or not cand.dietary_verified:
            classification = "INSUFFICIENT_INFORMATION"
            auto_blocked = True
            blocking_reason = "Unknown allergen or dietary metadata blocks automatic approval."
        elif excluded and (excluded & c_allergens):
            classification = "RESTRICTED_SUBSTITUTE"
            auto_blocked = True
            blocking_reason = f"Contains excluded allergen(s): {', '.join(sorted(excluded & c_allergens))}."
        elif required_diet and not required_diet.issubset(c_diet):
            classification = "RESTRICTED_SUBSTITUTE"
            auto_blocked = True
            blocking_reason = f"Missing required dietary tags: {', '.join(sorted(required_diet - c_diet))}."
        elif cand.chef_approved:
            classification = "APPROVED_SUBSTITUTE"
        else:
            classification = "PROPOSED_SUBSTITUTE"

        allergen_note = (
            "Verified allergen profile: " + (", ".join(cand.allergens) if cand.allergens else "None declared")
            if cand.allergen_verified
            else "UNVERIFIED allergen profile (never assumed safe)"
        )
        nutr_note = (
            f"{cand.calories_per_100g:.0f} kcal/100g" if cand.calories_per_100g is not None else "Nutritional profile not provided"
        )
        avail_note = "In Stock" if _d(cand.available_stock) >= req_qty else "Low/Order Required"

        recs.append(
            SubstitutionRecommendationResult(
                material_id=cand.material_id,
                material_code=cand.material_code,
                name=cand.name,
                classification=classification,  # type: ignore[arg-type]
                original_ingredient_cost=_r2(orig_cost),
                substitute_ingredient_cost=_r2(sub_cost),
                revised_recipe_cost=_r2(revised_recipe),
                cost_saving=_r2(saving),
                yield_impact_pct=_r2(yield_impact),
                allergen_impact=allergen_note,
                nutritional_impact=nutr_note,
                availability_status=avail_note,
                requires_executive_chef_approval=True,
                auto_approval_blocked=auto_blocked,
                blocking_reason=blocking_reason,
            )
        )

    recs.sort(key=lambda x: x.cost_saving, reverse=True)
    return SubstitutionAnalysisResponse(
        currency=req.currency,
        original_ingredient_cost=_r2(orig_cost),
        recommendations=recs,
    )


# ---------------------------------------------------------------------------
# 7. Buffet Optimization Engine (Holding-Time & Safety Enforced)
# ---------------------------------------------------------------------------

def optimize_buffet(req: BuffetOptimizeRequest) -> BuffetOptimizeResponse:
    covers = Decimal(str(max(1, req.expected_covers)))
    occ_factor = max(Decimal("0.5"), min(Decimal("1.2"), _d(req.occupancy_percent) / Decimal("85")))
    weight_sum = sum((_d(d.popularity_weight) for d in req.dishes), Decimal("0"))
    norm = weight_sum if weight_sum > 0 else Decimal("1")

    recs: list[BuffetDishRecommendation] = []
    tot_prod_cost = Decimal("0")
    tot_waste_save_val = Decimal("0")

    for d in req.dishes:
        share = _d(d.popularity_weight) / norm
        expected_cons = min(_d(d.ingredient_available_portions), covers * share * occ_factor)
        service_mins = max(30, d.service_duration_minutes)
        hold_mins = max(30, min(180, d.max_holding_minutes))
        # Schedule replenishment intervals inside safe holding time
        safe_interval = max(20, min(hold_mins // 2, 45))
        batches = max(1, math.ceil(float(expected_cons) / max(1, d.batch_size_portions)))
        initial_qty = max(d.min_display_portions, min(int(math.ceil(float(expected_cons) * 0.45)), d.batch_size_portions * 2))
        rem_needed = max(0.0, float(expected_cons) - initial_qty)
        rep_batches = max(0, batches - 1)
        rep_qty = int(math.ceil(rem_needed / max(1, rep_batches))) if rep_batches > 0 else 0

        total_planned = initial_qty + (rep_batches * rep_qty)
        expected_leftover = max(Decimal("0"), _d(total_planned) - expected_cons)
        baseline_waste = expected_cons * (_d(d.historical_waste_pct) / Decimal("100"))
        waste_reduction = max(Decimal("0"), baseline_waste - expected_leftover)

        prod_cost = _d(total_planned) * _d(d.unit_cost_per_portion)
        tot_prod_cost += prod_cost
        tot_waste_save_val += waste_reduction * _d(d.unit_cost_per_portion)

        recs.append(
            BuffetDishRecommendation(
                dish_id=d.dish_id,
                name=d.name,
                initial_production_portions=initial_qty,
                replenishment_batches=rep_batches,
                replenishment_qty_per_batch=rep_qty,
                replenishment_interval_minutes=safe_interval,
                expected_consumption_portions=_r2(expected_cons),
                expected_leftover_portions=_r2(expected_leftover),
                production_cost=_r2(prod_cost),
                food_cost_per_guest=_r2(prod_cost / covers),
                potential_waste_reduction_portions=_r2(waste_reduction),
                food_safety_note=(
                    f"Hold max {hold_mins} min; replenish every {safe_interval} min in {rep_qty}-portion batches. Discard unserved hot/chilled leftovers safely."
                ),
            )
        )

    return BuffetOptimizeResponse(
        currency=req.currency,
        meal_period=req.meal_period,
        expected_covers=req.expected_covers,
        total_production_cost=_r2(tot_prod_cost),
        food_cost_per_guest=_r2(tot_prod_cost / covers),
        total_potential_waste_reduction_value=_r2(tot_waste_save_val),
        recommendations=recs,
    )


# ---------------------------------------------------------------------------
# 8. Event Menu Optimization (5 Packages Reusing Phase 3A P&L Math)
# ---------------------------------------------------------------------------

def optimize_event_packages(req: EventPackageOptimizeRequest) -> EventPackageOptimizeResponse:
    guests = Decimal(str(max(1, req.guest_count)))
    base_fc = _d(req.base_food_cost_per_guest)
    base_bc = _d(req.base_beverage_cost_per_guest)
    manpower = _d(req.manpower_cost)
    fixed = _d(req.fixed_expenses)
    target_rev_per_guest = _d(req.target_revenue) / guests

    specs = [
        ("BUDGET_OPTIMIZED", "Budget Optimized Package", "Seasonal high-yield mains + controlled garnish & batch sides", Decimal("0.82"), Decimal("0.85"), Decimal("0.92"), Decimal("0.42")),
        ("MARGIN_OPTIMIZED", "Margin Optimized Package", "High-contribution signature dishes + premium beverage pairings", Decimal("0.88"), Decimal("0.90"), Decimal("1.08"), Decimal("0.45")),
        ("PREMIUM_EXPERIENCE", "Premium Experience Package", "Luxury live stations, chef-carved proteins & artisanal desserts", Decimal("1.18"), Decimal("1.15"), Decimal("1.24"), Decimal("0.55")),
        ("WASTE_OPTIMIZED", "Waste Optimized Package", "Cross-utilized prep ingredients + just-in-time banquet batching", Decimal("0.86"), Decimal("0.92"), Decimal("1.00"), Decimal("0.38")),
        ("BALANCED", "Balanced Recommendation", "Balanced guest appeal, safe dietary coverage & target contribution", Decimal("0.93"), Decimal("0.95"), Decimal("1.02"), Decimal("0.44")),
    ]

    packages: list[EventPackageOption] = []
    for code, title, summary, fc_mult, bc_mult, price_mult, kg_per_guest in specs:
        est_food = base_fc * fc_mult * guests
        est_bev = base_bc * bc_mult * guests
        est_event_cost = est_food + est_bev + manpower + fixed
        sell_guest = target_rev_per_guest * price_mult
        exp_rev = sell_guest * guests
        exp_cm = exp_rev - (est_food + est_bev + manpower)
        exp_cm_pct = ((exp_cm / exp_rev) * Decimal("100")) if exp_rev > 0 else Decimal("0")
        fully_loaded = exp_rev - est_event_cost
        req_kg = kg_per_guest * guests

        packages.append(
            EventPackageOption(
                package_code=code,  # type: ignore[arg-type]
                package_name=title,
                menu_composition_summary=summary,
                estimated_food_cost=_r2(est_food),
                estimated_beverage_cost=_r2(est_bev),
                estimated_manpower_cost=_r2(manpower),
                estimated_event_cost=_r2(est_event_cost),
                cost_per_guest=_r2(est_event_cost / guests),
                selling_price_per_guest=_r2(sell_guest),
                expected_revenue=_r2(exp_rev),
                expected_contribution=_r2(exp_cm),
                expected_contribution_margin_pct=_r2(exp_cm_pct),
                estimated_fully_loaded_profit=_r2(fully_loaded),
                ingredient_requirement_kg=_r2(req_kg),
                procurement_requirement_value=_r2(est_food + est_bev),
            )
        )

    return EventPackageOptimizeResponse(
        currency=req.currency,
        guest_count=req.guest_count,
        packages=packages,
    )


# ---------------------------------------------------------------------------
# 9. Demand Forecasting & Historical Backtesting Engine
# ---------------------------------------------------------------------------

def forecast_demand(req: DemandForecastRequest) -> DemandForecastResponse:
    horizon = max(1, min(90, req.horizon_days))
    hist = req.historical
    warnings: list[str] = []

    if len(hist) < 7:
        # Cold-start baseline using occupancy assumption + any available average
        avg_hist = float(np.mean([p.covers for p in hist])) if hist else (req.occupancy_assumption_pct * 2.2)
        sigma = max(10.0, avg_hist * 0.15)
        start_dt = datetime.now(timezone.utc).date() + timedelta(days=1)
        pts = []
        for idx in range(horizon):
            ev_boost = req.upcoming_event_guests[idx] if idx < len(req.upcoming_event_guests) else 0.0
            pred = max(0.0, avg_hist + ev_boost)
            pts.append(
                ForecastPoint(
                    day_index=idx + 1,
                    date=(start_dt + timedelta(days=idx)).isoformat(),
                    forecast_covers=round(pred, 1),
                    lower_bound_80=round(max(0.0, pred - 1.28 * sigma), 1),
                    upper_bound_80=round(pred + 1.28 * sigma, 1),
                    lower_bound_95=round(max(0.0, pred - 1.96 * sigma), 1),
                    upper_bound_95=round(pred + 1.96 * sigma, 1),
                    expected_food_cost=round(pred * req.avg_food_cost_per_cover, 2),
                )
            )
        warnings.append("Fewer than 7 historical observations available; cold-start baseline applied without fabricated backtest accuracy.")
        return DemandForecastResponse(
            model_name="Cold-Start Occupancy Baseline",
            horizon_days=horizon,
            cold_start_used=True,
            mae=None,
            wape_percent=None,
            forecast_bias=None,
            data_quality="COLD_START",
            last_trained_utc=_now_iso(),
            forecast_points=pts,
            warnings=warnings,
        )

    # Build feature matrix: occupancy, event_guests, weekend flag, holiday flag, time trend
    def _features(idx_val: int, occ: float, ev: float, is_hol: bool, dt_str: str) -> list[float]:
        try:
            dow = datetime.fromisoformat(dt_str[:10]).weekday()
        except ValueError:
            dow = idx_val % 7
        is_wknd = 1.0 if dow in (4, 5, 6) else 0.0
        return [occ, ev, is_wknd, 1.0 if is_hol else 0.0, float(idx_val) / max(1, len(hist))]

    X = np.array([_features(i, p.occupancy_pct, p.event_guests, p.is_holiday, p.date) for i, p in enumerate(hist)], dtype=float)
    y = np.array([p.covers for p in hist], dtype=float)

    # Rolling historical backtest on final holdout window
    split = max(4, int(len(hist) * 0.75))
    bt_model = Ridge(alpha=1.0)
    bt_model.fit(X[:split], y[:split])
    bt_preds = np.maximum(0.0, bt_model.predict(X[split:]))
    bt_actual = y[split:]
    errors = bt_preds - bt_actual
    mae = float(np.mean(np.abs(errors))) if len(errors) > 0 else 0.0
    denom = float(np.sum(np.abs(bt_actual)))
    wape = (float(np.sum(np.abs(errors))) / denom * 100.0) if denom > 0 else None
    bias = float(np.mean(errors)) if len(errors) > 0 else 0.0
    sigma = float(np.std(errors, ddof=1)) if len(errors) > 1 else max(8.0, mae * 1.25)

    # Fit full model for forward predictions
    model = Ridge(alpha=1.0)
    model.fit(X, y)

    try:
        last_dt = datetime.fromisoformat(hist[-1].date[:10]).date()
    except ValueError:
        last_dt = datetime.now(timezone.utc).date()

    pts = []
    for idx in range(horizon):
        f_date = last_dt + timedelta(days=idx + 1)
        ev = req.upcoming_event_guests[idx] if idx < len(req.upcoming_event_guests) else 0.0
        fx = np.array([_features(len(hist) + idx, req.occupancy_assumption_pct, ev, False, f_date.isoformat())], dtype=float)
        pred = max(0.0, float(model.predict(fx)[0]))
        pts.append(
            ForecastPoint(
                day_index=idx + 1,
                date=f_date.isoformat(),
                forecast_covers=round(pred, 1),
                lower_bound_80=round(max(0.0, pred - 1.28 * sigma), 1),
                upper_bound_80=round(pred + 1.28 * sigma, 1),
                lower_bound_95=round(max(0.0, pred - 1.96 * sigma), 1),
                upper_bound_95=round(pred + 1.96 * sigma, 1),
                expected_food_cost=round(pred * req.avg_food_cost_per_cover, 2),
            )
        )

    return DemandForecastResponse(
        model_name="Seasonal Occupancy Ridge Regression",
        horizon_days=horizon,
        cold_start_used=False,
        mae=round(mae, 2),
        wape_percent=round(wape, 2) if wape is not None else None,
        forecast_bias=round(bias, 2),
        data_quality="HIGH" if len(hist) >= 28 else "MODERATE",
        last_trained_utc=_now_iso(),
        forecast_points=pts,
        warnings=warnings,
    )


# ---------------------------------------------------------------------------
# 10. Food Waste Intelligence
# ---------------------------------------------------------------------------

def analyze_waste(req: WasteAnalyzeRequest) -> WasteAnalyzeResponse:
    by_cat: dict[str, Decimal] = {}
    by_prop: dict[str, Decimal] = {}
    by_meal: dict[str, Decimal] = {}
    tot_waste = Decimal("0")

    for e in req.entries:
        c = _d(e.cost)
        tot_waste += c
        by_cat[e.waste_category] = by_cat.get(e.waste_category, Decimal("0")) + c
        prop_k = e.property_name or "Default Property"
        by_prop[prop_k] = by_prop.get(prop_k, Decimal("0")) + c
        by_meal[e.meal_period] = by_meal.get(e.meal_period, Decimal("0")) + c

    waste_pct = ((tot_waste / _d(req.total_food_cost)) * Decimal("100")) if req.total_food_cost > 0 else None
    waste_per_cover = (tot_waste / _d(req.total_covers)) if req.total_covers > 0 else None

    patterns: list[str] = []
    recs: list[str] = []
    if by_cat:
        top_cat = max(by_cat.items(), key=lambda kv: kv[1])[0]
        patterns.append(f"Primary waste driver: {top_cat.replace('_', ' ').title()} ({req.currency} {_r2(by_cat[top_cat]):.2f}).")
        if top_cat in ("OVERPRODUCTION", "BUFFET_LEFTOVERS"):
            recs.extend([
                "Reduce initial buffet batch production and increase replenishment cadence.",
                "Review meal-period cover forecast before prep batching.",
            ])
        elif top_cat in ("SPOILAGE", "EXPIRED_INGREDIENTS"):
            recs.extend([
                "Enforce FEFO stock rotation and reduce purchasing pack size.",
                "Trigger Purchase Recommendation lead-time review for perishables.",
            ])
        elif top_cat in ("PREPARATION_WASTE", "YIELD_LOSS"):
            recs.append("Audit butcher/trimming yield factors and chef prep SOPs.")
        else:
            recs.append("Review portion sizes and plate waste feedback with outlet chefs.")

    return WasteAnalyzeResponse(
        currency=req.currency,
        total_waste_cost=_r2(tot_waste),
        waste_percent_of_food_cost=_r2(waste_pct) if waste_pct is not None else None,
        waste_per_cover=_r2(waste_per_cover) if waste_per_cover is not None else None,
        by_category={k: _r2(v) for k, v in by_cat.items()},
        by_property={k: _r2(v) for k, v in by_prop.items()},
        by_meal_period={k: _r2(v) for k, v in by_meal.items()},
        recurring_patterns=patterns,
        recommendations=recs,
    )


# ---------------------------------------------------------------------------
# 11. Cross-Property FX-Normalized Benchmarking
# ---------------------------------------------------------------------------

def benchmark_properties(req: PropertyBenchmarkRequest) -> PropertyBenchmarkResponse:
    warnings: list[str] = []
    outlet_types = {p.outlet_type for p in req.properties if p.outlet_type}
    if len(outlet_types) > 1:
        warnings.append(
            f"Comparing mixed outlet formats ({', '.join(sorted(outlet_types))}); interpret cost-per-cover benchmarks with outlet normalization."
        )

    rows: list[PropertyBenchmarkRow] = []
    for p in req.properties:
        fx = _d(p.fx_to_reporting_rate) if p.fx_to_reporting_rate is not None else None
        warn: str | None = None
        if p.local_currency != req.reporting_currency and (fx is None or fx <= 0):
            warn = f"Missing effective-dated FX rate for {p.local_currency} -> {req.reporting_currency}; monetary KPIs omitted."
            rev_rep = None
            cost_rep = None
            waste_rep = None
        else:
            rate = fx if fx is not None and fx > 0 else Decimal("1")
            rev_rep = _d(p.food_revenue_local) * rate
            cost_rep = _d(p.food_cost_local) * rate
            waste_rep = _d(p.waste_cost_local) * rate

        rev_local = _d(p.food_revenue_local)
        cost_local = _d(p.food_cost_local)
        waste_local = _d(p.waste_cost_local)
        covers = Decimal(str(p.covers)) if p.covers > 0 else Decimal("0")

        fc_pct = ((cost_local / rev_local) * Decimal("100")) if rev_local > 0 else None
        cm_pct = (((rev_local - cost_local) / rev_local) * Decimal("100")) if rev_local > 0 else None
        waste_pct = ((waste_local / cost_local) * Decimal("100")) if cost_local > 0 else None

        rows.append(
            PropertyBenchmarkRow(
                property_id=p.property_id,
                property_name=p.property_name,
                outlet_type=p.outlet_type,
                local_currency=p.local_currency,
                fx_rate_applied=float(fx) if fx is not None else None,
                revenue_reporting=_r2(rev_rep) if rev_rep is not None else None,
                food_cost_reporting=_r2(cost_rep) if cost_rep is not None else None,
                food_cost_pct=_r2(fc_pct) if fc_pct is not None else None,
                cost_per_cover_reporting=_r2(cost_rep / covers) if (cost_rep is not None and covers > 0) else None,
                revenue_per_cover_reporting=_r2(rev_rep / covers) if (rev_rep is not None and covers > 0) else None,
                contribution_margin_pct=_r2(cm_pct) if cm_pct is not None else None,
                waste_pct=_r2(waste_pct) if waste_pct is not None else None,
                waste_per_cover_reporting=_r2(waste_rep / covers) if (waste_rep is not None and covers > 0) else None,
                event_margin_pct=p.event_margin_pct,
                forecast_accuracy_pct=p.forecast_accuracy_pct,
                comparison_warning=warn,
            )
        )

    return PropertyBenchmarkResponse(reporting_currency=req.reporting_currency, warnings=warnings, rows=rows)


# ---------------------------------------------------------------------------
# 12. Grounded Culinary Copilot (Prompt-Injection Defended, No Fabricated Math)
# ---------------------------------------------------------------------------

_INJECTION_PATTERNS = re.compile(
    r"(ignore\s+previous\s+instructions|system\s+prompt|drop\s+table|select\s+\*\s+from|execute\s+sql|reveal\s+api\s+key|bypass\s+tenant)",
    re.IGNORECASE,
)


def answer_culinary_copilot(req: CopilotQueryRequest) -> CopilotQueryResponse:
    if _INJECTION_PATTERNS.search(req.question):
        return CopilotQueryResponse(
            direct_answer="Request blocked by security policy: Culinary Copilot only answers authorized culinary analytics questions using whitelisted tenant metrics.",
            supporting_metrics=[],
            data_source="SILA Security Guardrail",
            date_range=req.date_range,
            assumptions=["Unrestricted SQL execution, prompt-override instructions, and credential access are prohibited."],
            recommended_action="Rephrase your question around menu engineering, food cost, waste, forecasts, or event margins.",
            estimated_financial_impact="N/A",
            confidence="HIGH",
            supporting_route="/sila-recipes/culinary/analytics",
            provider_used="LOCAL_POLICY_GUARD",
            prompt_injection_blocked=True,
        )

    m = req.grounded_metrics or {}
    q = req.question.lower()
    curr = req.currency
    fc_pct = m.get("foodCostPercent")
    target_fc = m.get("targetFoodCostPercent", 28)
    waste_cost = m.get("wasteCost")
    dogs = m.get("dogItems", [])
    puzzles = m.get("puzzleItems", [])
    price_alerts = m.get("priceAlerts", [])
    shortages = m.get("shortageAlerts", [])

    if "shortage" in q or "stock" in q:
        direct = (
            f"Detected {len(shortages)} active ingredient shortage risk(s) across the current demand horizon."
            if shortages
            else "No critical ingredient shortages are currently flagged against on-hand inventory."
        )
        route = "/sila-recipes/culinary/mrp"
        action = "Review MRP & Purchase Recommendations before approving replenishment POs."
        impact = "Prevents outlet stockouts and emergency spot purchases."
    elif "waste" in q or "buffet" in q:
        direct = f"Recorded waste stands at {curr} {waste_cost or 0:.2f} for {req.date_range}. Targeted buffet batch replenishment typically lowers leftovers by 10–18%."
        route = "/sila-recipes/culinary/analytics"
        action = "Reduce initial buffet display batch size and enforce scheduled replenishment intervals."
        impact = f"Estimated savings: {curr} {round(float(waste_cost or 0) * 0.15, 2):.2f} per period."
    elif "food cost" in q:
        direct = (
            f"Current food cost is {fc_pct if fc_pct is not None else 'N/A'}% vs target {target_fc}% for {req.date_range} "
            f"(recorded waste {curr} {waste_cost or 0:.2f}). All figures are computed deterministically by the SILA .NET & Python engines."
        )
        route = "/sila-recipes/culinary/analytics"
        action = "Inspect high-variance recipes, consumption variance, and ingredient price alerts in the Recommendation Cockpit."
        impact = f"Potential margin recovery aligned to {target_fc}% food cost target."
    elif "price" in q or "salmon" in q or "increase" in q:
        top_alert = price_alerts[0] if price_alerts else "key protein & dairy inputs"
        direct = f"Price variance monitoring currently highlights {top_alert}. Use the isolated What-If Simulation tab to test exact +15% supplier price pass-through without altering live recipes."
        route = "/sila-recipes/culinary/analytics"
        action = "Run a +15% ingredient shock scenario and evaluate Executive-Chef-approved substitutes."
        impact = "Protects menu contribution margin before supplier contract renewal."
    elif "promote" in q or "puzzle" in q or "profit" in q or "dog" in q:
        direct = (
            f"Puzzle items ({len(puzzles)} high-margin, low-volume dishes) are top candidates for promotion; "
            f"{len(dogs)} Dog item(s) require recipe re-engineering or menu review."
        )
        route = "/sila-recipes/culinary/analytics"
        action = "Promote top Puzzle dishes in outlet specials and review Dog items in the Recommendation Cockpit."
        impact = "Raises weighted contribution margin without increasing fixed kitchen overhead."
    else:
        direct = (
            f"Current food cost is {fc_pct if fc_pct is not None else 'N/A'}% vs target {target_fc}% for {req.date_range}. "
            f"All figures are computed deterministically by the SILA .NET & Python engines."
        )
        route = "/sila-recipes/culinary/analytics"
        action = "Inspect high-variance recipes and pending recommendations in the Culinary Recommendation Cockpit."
        impact = f"Potential margin recovery aligned to {target_fc}% food cost target."

    supporting = []
    if fc_pct is not None:
        supporting.append(f"Food Cost %: {fc_pct}% (Target {target_fc}%)")
    if waste_cost is not None:
        supporting.append(f"Waste Cost: {curr} {waste_cost}")
    if m.get("totalRevenue") is not None:
        supporting.append(f"F&B Revenue: {curr} {m.get('totalRevenue')}")

    return CopilotQueryResponse(
        direct_answer=direct,
        supporting_metrics=supporting,
        data_source="Whitelisted SILA Tenant Culinary & Financial Snapshot",
        date_range=req.date_range,
        assumptions=[
            "Calculations derived strictly from .NET/Python deterministic engines, never LLM arithmetic.",
            "External AI calls remain disabled unless tenant AllowExternalAi is enabled.",
        ],
        recommended_action=action,
        estimated_financial_impact=impact,
        confidence="HIGH" if supporting else "MODERATE",
        supporting_route=route,
        provider_used=req.mode or "LOCAL_DETERMINISTIC",
        prompt_injection_blocked=False,
    )
