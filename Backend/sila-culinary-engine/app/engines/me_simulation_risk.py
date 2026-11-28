from __future__ import annotations

import time
import numpy as np
from app.schemas.me_twin_opt import (
    MonteCarloInput,
    MonteCarloResponse,
    SensitivityRequest,
    SensitivityResponse,
    SensitivityTornadoItem,
    StressTestOutcome,
    StressTestRequest,
    StressTestResponse,
    SubstitutionCascadeRequest,
    SubstitutionCascadeResponse,
    SubstitutionCascadeResult,
)


def run_monte_carlo_simulation(req: MonteCarloInput) -> MonteCarloResponse:
    t0 = time.perf_counter()
    rng = np.random.default_rng(req.seed)
    n = req.runs

    # Correlated standard normals for guest count & ingredient price pressure
    z1 = rng.standard_normal(n)
    z2_ind = rng.standard_normal(n)
    rho = req.guest_price_correlation
    z2 = rho * z1 + np.sqrt(max(0.0, 1.0 - rho * rho)) * z2_ind

    guests = np.clip(req.guest_count_mean + req.guest_count_std * z1, 1.0, req.guest_count_mean * 3.0)
    participation = np.clip(rng.normal(req.participation_mean, req.participation_std, n), 0.1, 1.5)
    servings = guests * participation

    price_factor = np.clip(1.0 + (req.ingredient_price_vol_percent / 100.0) * z2, 0.5, 2.5)
    yield_factor = np.clip(rng.normal(req.yield_mean_factor, req.yield_std, n), 0.4, 1.05)
    waste_rate = np.clip(rng.normal(req.waste_rate_mean, req.waste_rate_std, n), 0.0, 0.5)

    revenues = guests * req.selling_price_per_guest
    unit_cost = (req.base_food_cost_per_serving * price_factor) / yield_factor
    costs = servings * unit_cost * (1.0 + waste_rate)
    margins = revenues - costs
    fc_pcts = (costs / np.maximum(1.0, revenues)) * 100.0

    prob_exceed_budget = float(np.mean(costs > req.event_budget) * 100.0)
    prob_exceed_fc = float(np.mean(fc_pcts > req.target_food_cost_percent) * 100.0)
    prob_shortage = float(np.mean(servings > req.available_stock_servings) * 100.0)
    var_95_margin = float(np.percentile(margins, 5))

    runtime_ms = (time.perf_counter() - t0) * 1000.0
    return MonteCarloResponse(
        seed=req.seed,
        runs=req.runs,
        expected_revenue=round(float(np.mean(revenues)), 2),
        expected_cost=round(float(np.mean(costs)), 2),
        median_cost=round(float(np.median(costs)), 2),
        cost_p10=round(float(np.percentile(costs, 10)), 2),
        cost_p90=round(float(np.percentile(costs, 90)), 2),
        expected_food_cost_percent=round(float(np.mean(fc_pcts)), 2),
        expected_contribution_margin=round(float(np.mean(margins)), 2),
        margin_p10=round(float(np.percentile(margins, 10)), 2),
        margin_p90=round(float(np.percentile(margins, 90)), 2),
        downside_var_95_margin=round(var_95_margin, 2),
        prob_exceed_budget=round(prob_exceed_budget, 2),
        prob_exceed_food_cost_target=round(prob_exceed_fc, 2),
        prob_ingredient_shortage=round(prob_shortage, 2),
        assumptions={
            "guest_price_correlation": req.guest_price_correlation,
            "guest_count_mean": req.guest_count_mean,
            "guest_count_std": req.guest_count_std,
            "ingredient_price_vol_percent": req.ingredient_price_vol_percent,
            "yield_mean_factor": req.yield_mean_factor,
            "waste_rate_mean": req.waste_rate_mean,
        },
        runtime_ms=round(runtime_ms, 2),
    )


def run_sensitivity_analysis(req: SensitivityRequest) -> SensitivityResponse:
    base_margin = req.baseline_revenue - req.baseline_cost
    base_fc = (req.baseline_cost / req.baseline_revenue * 100.0) if req.baseline_revenue > 0 else 0.0
    tornado: list[SensitivityTornadoItem] = []

    for d in req.drivers:
        delta_lo = d.low_value - d.baseline_value
        delta_hi = d.high_value - d.baseline_value

        rev_lo = max(1.0, req.baseline_revenue + delta_lo * d.baseline_revenue_impact_per_unit)
        rev_hi = max(1.0, req.baseline_revenue + delta_hi * d.baseline_revenue_impact_per_unit)
        cost_lo = max(0.0, req.baseline_cost + delta_lo * d.baseline_cost_impact_per_unit)
        cost_hi = max(0.0, req.baseline_cost + delta_hi * d.baseline_cost_impact_per_unit)

        m_lo = rev_lo - cost_lo
        m_hi = rev_hi - cost_hi
        fc_lo = cost_lo / rev_lo * 100.0
        fc_hi = cost_hi / rev_hi * 100.0

        tornado.append(
            SensitivityTornadoItem(
                variable_name=d.variable_name,
                category=d.category,
                low_margin=round(min(m_lo, m_hi), 2),
                high_margin=round(max(m_lo, m_hi), 2),
                margin_swing=round(abs(m_hi - m_lo), 2),
                low_food_cost_percent=round(min(fc_lo, fc_hi), 2),
                high_food_cost_percent=round(max(fc_lo, fc_hi), 2),
                food_cost_swing_percent=round(abs(fc_hi - fc_lo), 2),
            )
        )

    tornado.sort(key=lambda t: t.margin_swing, reverse=True)
    top_drivers = [t.variable_name for t in tornado[:5]]
    break_even_ceiling = req.baseline_revenue * (req.target_food_cost_percent / 100.0)
    return SensitivityResponse(
        baseline_margin=round(base_margin, 2),
        baseline_food_cost_percent=round(base_fc, 2),
        tornado=tornado,
        top_risk_drivers=top_drivers,
        break_even_food_cost_ceiling=round(break_even_ceiling, 2),
    )


def run_stress_tests(req: StressTestRequest) -> StressTestResponse:
    base_rev = req.baseline_guests * req.selling_price_per_guest
    base_cost = req.baseline_guests * req.baseline_food_cost_per_guest
    base_margin = base_rev - base_cost
    outcomes: list[StressTestOutcome] = []

    for sc in req.scenarios:
        rev_guests = max(1, int(round(req.baseline_guests * sc.guest_multiplier)))
        rev_revenue = rev_guests * req.selling_price_per_guest
        rev_cost = rev_guests * req.baseline_food_cost_per_guest * sc.ingredient_price_multiplier / max(0.3, sc.yield_multiplier)
        rev_fc_pct = (rev_cost / rev_revenue * 100.0) if rev_revenue > 0 else 0.0
        rev_margin = rev_revenue - rev_cost
        rev_prep = req.baseline_prep_minutes * sc.guest_multiplier / max(0.4, sc.yield_multiplier)
        eff_cap = max(0.0, req.kitchen_capacity_minutes - sc.unavailable_equipment_minutes)

        shortage = "LOW"
        actions: list[str] = []
        feasible = True

        if sc.supplier_delay_days > req.ingredient_buffer_days:
            shortage = "CRITICAL"
            feasible = False
            actions.append(
                f"Supplier delayed {sc.supplier_delay_days}d exceeds {req.ingredient_buffer_days}d safety stock; expedite alternate supplier or ITO transfer."
            )
        elif sc.unavailable_ingredient:
            shortage = "CRITICAL"
            feasible = False
            actions.append(
                f"Ingredient '{sc.unavailable_ingredient}' unavailable; activate approved culinary substitute or remove affected dishes."
            )
        elif sc.guest_multiplier >= 1.2:
            shortage = "HIGH"
            actions.append("Issue emergency incremental PO / central kitchen batch dispatch.")

        if rev_prep > eff_cap:
            feasible = False
            actions.append(
                f"Prep load ({rev_prep:.0f} min) exceeds effective capacity ({eff_cap:.0f} min); shift dishes to low-prep or secondary kitchen."
            )
        if rev_fc_pct > req.max_food_cost_percent:
            actions.append(
                f"Food cost ({rev_fc_pct:.2f}%) breaches target ({req.max_food_cost_percent:.2f}%); adjust protein portion mix or selling price."
            )
        if not actions:
            actions.append("Scenario absorbs shock within approved operational thresholds.")

        outcomes.append(
            StressTestOutcome(
                stress_type=sc.stress_type,
                label=sc.label,
                revised_guests=rev_guests,
                revised_revenue=round(rev_revenue, 2),
                revised_food_cost=round(rev_cost, 2),
                revised_food_cost_percent=round(rev_fc_pct, 2),
                revised_margin=round(rev_margin, 2),
                revised_prep_minutes=round(rev_prep, 1),
                is_operationally_feasible=feasible,
                shortage_risk=shortage,
                corrective_actions=actions,
            )
        )

    return StressTestResponse(baseline_margin=round(base_margin, 2), outcomes=outcomes)


def evaluate_substitution_cascade(req: SubstitutionCascadeRequest) -> SubstitutionCascadeResponse:
    base_cost = req.original_qty * req.original_unit_cost + req.other_ingredients_cost
    base_fc = (base_cost / req.recipe_selling_price * 100.0) if req.recipe_selling_price > 0 else 0.0
    excl_allergens = {a.strip().lower() for a in req.excluded_allergens if a.strip()}
    req_diet = {d.strip().lower() for d in req.required_dietary_tags if d.strip()}

    results: list[SubstitutionCascadeResult] = []
    for c in req.candidates:
        cand_allergens = {a.strip().lower() for a in c.allergens}
        cand_diet = {d.strip().lower() for d in c.dietary_tags}
        allergen_hit = excl_allergens & cand_allergens
        diet_missing = req_diet - cand_diet

        new_qty = req.original_qty / max(0.1, c.yield_factor)
        new_cost = new_qty * c.unit_cost + req.other_ingredients_cost
        new_fc = (new_cost / req.recipe_selling_price * 100.0) if req.recipe_selling_price > 0 else 0.0
        new_margin = req.recipe_selling_price - new_cost

        if allergen_hit:
            results.append(
                SubstitutionCascadeResult(
                    substitute_id=c.substitute_id,
                    substitute_name=c.substitute_name,
                    category="REJECTED_UNSAFE",
                    is_safe=False,
                    rejection_reason=f"Unsafe allergen conflict: {', '.join(sorted(allergen_hit))}.",
                    recalculated_ingredient_qty=round(new_qty, 4),
                    recalculated_recipe_cost=round(new_cost, 2),
                    recalculated_food_cost_percent=round(new_fc, 2),
                    recalculated_margin=round(new_margin, 2),
                    cost_delta=round(new_cost - base_cost, 2),
                )
            )
            continue
        if diet_missing:
            results.append(
                SubstitutionCascadeResult(
                    substitute_id=c.substitute_id,
                    substitute_name=c.substitute_name,
                    category="REJECTED_UNSAFE",
                    is_safe=False,
                    rejection_reason=f"Violates dietary restriction: missing {', '.join(sorted(diet_missing))}.",
                    recalculated_ingredient_qty=round(new_qty, 4),
                    recalculated_recipe_cost=round(new_cost, 2),
                    recalculated_food_cost_percent=round(new_fc, 2),
                    recalculated_margin=round(new_margin, 2),
                    cost_delta=round(new_cost - base_cost, 2),
                )
            )
            continue

        cat = "APPROVED_SUBSTITUTION" if c.is_approved_relationship else "PROPOSED_SUBSTITUTION"
        results.append(
            SubstitutionCascadeResult(
                substitute_id=c.substitute_id,
                substitute_name=c.substitute_name,
                category=cat,
                is_safe=True,
                rejection_reason=None if c.supplier_available else "Requires culinary & supplier lead-time verification.",
                recalculated_ingredient_qty=round(new_qty, 4),
                recalculated_recipe_cost=round(new_cost, 2),
                recalculated_food_cost_percent=round(new_fc, 2),
                recalculated_margin=round(new_margin, 2),
                cost_delta=round(new_cost - base_cost, 2),
            )
        )

    results.sort(key=lambda r: (0 if r.category == "APPROVED_SUBSTITUTION" else (1 if r.category == "PROPOSED_SUBSTITUTION" else 2), r.recalculated_recipe_cost))
    return SubstitutionCascadeResponse(
        baseline_recipe_cost=round(base_cost, 2),
        baseline_food_cost_percent=round(base_fc, 2),
        results=results,
    )
