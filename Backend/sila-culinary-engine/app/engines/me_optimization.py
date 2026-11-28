from __future__ import annotations

import time
from ortools.sat.python import cp_model
from app.schemas.me_twin_opt import (
    InventoryMenuOptimizeRequest,
    InventoryMenuOptimizeResponse,
    MeMultiObjectiveOptimizeRequest,
    MeMultiObjectiveOptimizeResponse,
    MeOptimizationSolution,
    OptDishCandidate,
    OptObjectiveWeight,
    PricingOptimizeRequest,
    PricingOptimizeResponse,
    PricingRecommendationItem,
    SelectedDishAllocation,
)


SCALE = 1000  # Fixed integer scaling for currency & continuous metrics in CP-SAT


def _filter_eligible_candidates(
    candidates: list[OptDishCandidate],
    excluded_allergens: list[str],
    required_dietary_tags: list[str],
) -> tuple[list[OptDishCandidate], list[str]]:
    excluded_set = {a.strip().lower() for a in excluded_allergens if a.strip()}
    required_diet = {d.strip().lower() for d in required_dietary_tags if d.strip()}
    eligible: list[OptDishCandidate] = []
    rejected_reasons: list[str] = []

    for c in candidates:
        if not c.food_safety_approved:
            rejected_reasons.append(f"{c.name}: excluded by food safety constraint.")
            continue
        if not c.is_approved_recipe:
            rejected_reasons.append(f"{c.name}: excluded because recipe is not approved.")
            continue
        dish_allergens = {a.strip().lower() for a in c.allergens}
        if excluded_set & dish_allergens:
            rejected_reasons.append(f"{c.name}: excluded due to restricted allergen ({', '.join(sorted(excluded_set & dish_allergens))}).")
            continue
        if required_diet:
            dish_diet = {d.strip().lower() for d in c.dietary_tags}
            if not required_diet.issubset(dish_diet):
                rejected_reasons.append(f"{c.name}: excluded due to missing dietary requirement ({', '.join(sorted(required_diet - dish_diet))}).")
                continue
        eligible.append(c)
    return eligible, rejected_reasons


def _solve_cpsat_once(
    req: MeMultiObjectiveOptimizeRequest,
    eligible: list[OptDishCandidate],
    weights: dict[str, float],
    scenario_label: str,
    pareto_rank: int = 1,
) -> tuple[MeOptimizationSolution | None, list[str]]:
    t0 = time.perf_counter()
    model = cp_model.CpModel()
    n = len(eligible)
    if n == 0:
        return None, ["No eligible candidates remain after applying allergen, dietary, recipe-approval, and food-safety hard constraints."]

    x = [model.NewBoolVar(f"select_{i}") for i in range(n)]
    # Portions per dish: for buffet/banquet/set menus, allocate realistic planned portions within bounds
    # If menu_format is BUFFET/BANQUET/BRUNCH, revenue is guest_count * target_selling_price
    is_package = req.menu_format in {"BUFFET", "BRUNCH", "BANQUET", "SET_MENU"}
    default_dish_portions = max(1, int(round(req.guest_count * 0.45)))

    portions = []
    target_dishes = max(req.min_total_items, sum(req.category_counts.values()) if req.category_counts else req.min_total_items)
    for i, c in enumerate(eligible):
        upper = max(c.min_portions, min(c.max_portions, req.guest_count * 2))
        p_var = model.NewIntVar(0, upper, f"portions_{i}")
        if is_package:
            # Buffet/Banquet participation share: scales with popularity and dish count on the buffet line
            share_min = max(0.08, (c.predicted_popularity / max(1.0, target_dishes)) * 1.15)
            lower_if_selected = max(1, c.min_portions, min(upper, int(round(req.guest_count * share_min))))
        else:
            lower_if_selected = max(1, c.min_portions)
        model.Add(p_var >= lower_if_selected * x[i])
        model.Add(p_var <= upper * x[i])
        if is_package:
            # Keep total portions proportional to guest participation
            model.Add(p_var <= max(lower_if_selected, int(round(req.guest_count * 1.1))) * x[i])
        portions.append(p_var)

    # Hard constraints: total items
    model.Add(sum(x) >= req.min_total_items)
    model.Add(sum(x) <= req.max_total_items)

    # Hard constraints: exact or minimum category counts (e.g., 4 starters, 6 mains, 3 desserts)
    infeasibility: list[str] = []
    for cat, req_count in req.category_counts.items():
        cat_indices = [i for i, c in enumerate(eligible) if c.category.strip().lower() == cat.strip().lower()]
        if len(cat_indices) < req_count:
            infeasibility.append(
                f"Category '{cat}' requires {req_count} dishes, but only {len(cat_indices)} eligible approved candidates exist."
            )
        model.Add(sum(x[i] for i in cat_indices) == req_count)

    if infeasibility:
        return None, infeasibility

    # Total food cost & revenue in scaled integers
    total_cost_expr = sum(
        portions[i] * int(round((c.ingredient_cost_per_portion + c.variable_cost_per_portion) * SCALE))
        for i, c in enumerate(eligible)
    )
    if is_package:
        package_revenue = req.guest_count * req.target_selling_price
        max_cost_scaled = int(round(package_revenue * (req.max_food_cost_percent / 100.0) * SCALE))
        if req.max_budget is not None:
            max_cost_scaled = min(max_cost_scaled, int(round(req.max_budget * SCALE)))
        model.Add(total_cost_expr <= max_cost_scaled)
        # Ensure meaningful total dish portions cover guests (at least 1.0 portion per guest for multi-dish buffet)
        required_total_cat_dishes = sum(req.category_counts.values()) if req.category_counts else req.min_total_items
        if required_total_cat_dishes > 0:
            model.Add(sum(portions) >= req.guest_count)
    else:
        # A la carte / Weekly: sum(cost) <= (max_fc% / 100) * sum(price * portions)
        total_rev_expr = sum(
            portions[i] * int(round(max(c.selling_price, req.target_selling_price) * SCALE))
            for i, c in enumerate(eligible)
        )
        # 100 * cost <= max_fc * rev
        model.Add(total_cost_expr * 100 <= int(round(req.max_food_cost_percent)) * total_rev_expr)
        if req.max_budget is not None:
            model.Add(total_cost_expr <= int(round(req.max_budget * SCALE)))

    # Hard production capacity constraint (total & per-station)
    prep_terms = []
    for i, c in enumerate(eligible):
        # Approximate minutes scaled by 100
        mins_per_portion = c.prep_minutes_per_batch / max(1.0, c.batch_size_portions)
        prep_terms.append(portions[i] * int(round(mins_per_portion * 100)))
    model.Add(sum(prep_terms) <= int(round(req.available_production_minutes * 100)))

    for station, cap_mins in req.station_capacity_minutes.items():
        st_terms = [
            portions[i] * int(round((eligible[i].prep_minutes_per_batch / max(1.0, eligible[i].batch_size_portions)) * 100))
            for i in range(n)
            if eligible[i].kitchen_station.strip().lower() == station.strip().lower()
        ]
        if st_terms:
            model.Add(sum(st_terms) <= int(round(cap_mins * 100)))

    # Hard inventory stock constraints where explicitly specified
    for ing_id, avail_qty in req.eligible_inventory_stock.items():
        ing_terms = [
            portions[i] * int(round(eligible[i].ingredient_requirements[ing_id] * 1000))
            for i in range(n)
            if ing_id in eligible[i].ingredient_requirements
        ]
        if ing_terms:
            model.Add(sum(ing_terms) <= int(round(avail_qty * 1000)))

    # Normalize multi-objective scales (0..10000 per objective) before combining
    cost_norm_denom = max(1.0, (req.guest_count * req.target_selling_price) * (req.max_food_cost_percent / 100.0))
    pop_norm_denom = max(1.0, float(req.max_total_items))
    waste_norm_denom = max(1.0, cost_norm_denom * 0.25)
    comp_norm_denom = max(1.0, float(req.max_total_items * 10.0))

    w_cost = weights.get("MINIMIZE_FOOD_COST", 0.0) + weights.get("MINIMIZE_PURCHASE_COST", 0.0)
    w_margin = weights.get("MAXIMIZE_CONTRIBUTION_MARGIN", 0.0)
    w_pop = weights.get("MAXIMIZE_POPULARITY", 0.0)
    w_waste = weights.get("MINIMIZE_WASTE", 0.0) + req.soft_waste_penalty
    w_inv = weights.get("MAXIMIZE_INVENTORY_UTILIZATION", 0.0)
    w_comp = weights.get("MINIMIZE_COMPLEXITY", 0.0)
    w_short = weights.get("MINIMIZE_SHORTAGE_RISK", 0.0)
    w_div = weights.get("MAXIMIZE_DIVERSITY", 0.0)

    obj_terms = []
    # We minimize a normalized scalar objective
    for i, c in enumerate(eligible):
        unit_cost = c.ingredient_cost_per_portion + c.variable_cost_per_portion
        unit_price = req.target_selling_price if is_package else max(c.selling_price, req.target_selling_price)
        unit_margin = unit_price - unit_cost

        # Portion-weighted terms
        cost_coeff = (w_cost + w_margin * 0.5) * (unit_cost / cost_norm_denom) * 10000.0
        margin_coeff = -w_margin * (unit_margin / cost_norm_denom) * 10000.0
        waste_coeff = w_waste * ((unit_cost * c.predicted_waste_rate) / waste_norm_denom) * 10000.0
        inv_coeff = -w_inv * (c.eligible_inventory_coverage / max(1, req.guest_count)) * 10000.0
        short_coeff = w_short * (c.shortage_risk / max(1, req.guest_count)) * 10000.0
        p_coeff = int(round(cost_coeff + margin_coeff + waste_coeff + inv_coeff + short_coeff))
        if p_coeff != 0:
            obj_terms.append(portions[i] * p_coeff)

        # Selection-weighted terms
        pop_coeff = -w_pop * (c.predicted_popularity / pop_norm_denom) * 10000.0
        comp_coeff = w_comp * (c.complexity_score / comp_norm_denom) * 10000.0
        div_coeff = -w_div * (1.0 / pop_norm_denom) * 10000.0
        x_coeff = int(round(pop_coeff + comp_coeff + div_coeff))
        if x_coeff != 0:
            obj_terms.append(x[i] * x_coeff)

    if obj_terms:
        model.Minimize(sum(obj_terms))

    solver = cp_model.CpSolver()
    solver.parameters.max_time_in_seconds = req.solver_time_limit_seconds
    solver.parameters.num_search_workers = 4
    status_code = solver.Solve(model)
    runtime_ms = (time.perf_counter() - t0) * 1000.0

    if status_code not in (cp_model.OPTIMAL, cp_model.FEASIBLE):
        reasons = [
            f"CP-SAT solver returned {solver.StatusName(status_code)}: constraints could not be satisfied simultaneously.",
            f"Check max_food_cost_percent ({req.max_food_cost_percent}%), available_production_minutes ({req.available_production_minutes}), and category_counts ({req.category_counts}).",
        ]
        return None, reasons

    selected: list[SelectedDishAllocation] = []
    tot_cost = 0.0
    tot_waste_cost = 0.0
    tot_prep = 0.0
    inv_cov_weighted = 0.0
    tot_portions = 0
    tot_ala_carte_rev = 0.0
    avg_pop = 0.0

    for i, c in enumerate(eligible):
        if solver.Value(x[i]) == 1:
            p = int(solver.Value(portions[i]))
            u_cost = c.ingredient_cost_per_portion + c.variable_cost_per_portion
            line_cost = p * u_cost
            u_price = req.target_selling_price if is_package else max(c.selling_price, req.target_selling_price)
            line_rev = p * u_price
            prep_m = p * (c.prep_minutes_per_batch / max(1.0, c.batch_size_portions))
            tot_cost += line_cost
            tot_waste_cost += line_cost * c.predicted_waste_rate
            tot_prep += prep_m
            inv_cov_weighted += p * c.eligible_inventory_coverage
            tot_portions += p
            tot_ala_carte_rev += line_rev
            avg_pop += c.predicted_popularity
            selected.append(
                SelectedDishAllocation(
                    dish_id=c.dish_id,
                    name=c.name,
                    category=c.category,
                    planned_portions=p,
                    unit_cost=round(u_cost, 4),
                    total_cost=round(line_cost, 2),
                    contribution_margin=round((line_rev if not is_package else (req.guest_count * req.target_selling_price / max(1, len(eligible)))) - line_cost, 2),
                    prep_minutes=round(prep_m, 2),
                    inventory_coverage=round(c.eligible_inventory_coverage, 3),
                )
            )

    tot_rev = (req.guest_count * req.target_selling_price) if is_package else tot_ala_carte_rev
    fc_pct = (tot_cost / tot_rev * 100.0) if tot_rev > 0 else 0.0
    tot_margin = tot_rev - tot_cost
    inv_util = (inv_cov_weighted / max(1, tot_portions)) * 100.0
    is_opt = status_code == cp_model.OPTIMAL

    gap = 0.0 if is_opt else None
    if not is_opt and abs(solver.ObjectiveValue()) > 1e-6:
        gap = abs(solver.ObjectiveValue() - solver.BestObjectiveBound()) / max(1.0, abs(solver.ObjectiveValue()))

    cuisines = len({eligible[i].cuisine for i in range(n) if solver.Value(x[i]) == 1})
    trade_offs = [
        f"Food cost {fc_pct:.2f}% vs hard ceiling {req.max_food_cost_percent:.2f}%.",
        f"Kitchen load {tot_prep:.1f} min / {req.available_production_minutes:.1f} min capacity.",
        f"Inventory coverage {inv_util:.1f}% across {len(selected)} selected dishes ({cuisines} cuisine profiles).",
    ]

    return (
        MeOptimizationSolution(
            scenario_label=scenario_label,
            pareto_rank=pareto_rank,
            solver_status="OPTIMAL" if is_opt else "FEASIBLE",
            is_feasible=True,
            is_globally_optimal=is_opt,
            optimality_gap=round(gap, 4) if gap is not None else None,
            solver_runtime_ms=round(runtime_ms, 2),
            selected_dishes=selected,
            total_revenue=round(tot_rev, 2),
            total_food_cost=round(tot_cost, 2),
            food_cost_percent=round(fc_pct, 2),
            total_contribution_margin=round(tot_margin, 2),
            expected_waste_cost=round(tot_waste_cost, 2),
            inventory_utilization_percent=round(inv_util, 2),
            total_prep_minutes=round(tot_prep, 2),
            normalized_objective_score=round(float(solver.ObjectiveValue()), 2),
            objective_breakdown={
                "food_cost_percent": round(fc_pct, 2),
                "contribution_margin": round(tot_margin, 2),
                "avg_popularity": round(avg_pop / max(1, len(selected)), 3),
                "expected_waste_cost": round(tot_waste_cost, 2),
                "inventory_utilization_percent": round(inv_util, 2),
            },
            hard_constraints_satisfied=True,
            trade_off_explanation=trade_offs,
        ),
        [],
    )


def optimize_menu_multi_objective(req: MeMultiObjectiveOptimizeRequest) -> MeMultiObjectiveOptimizeResponse:
    eligible, rejected = _filter_eligible_candidates(
        req.candidates, req.excluded_allergens, req.required_dietary_tags
    )
    base_weights = {o.objective: o.weight for o in req.objectives}
    if not base_weights:
        base_weights = {
            "MINIMIZE_FOOD_COST": 0.4,
            "MAXIMIZE_CONTRIBUTION_MARGIN": 0.35,
            "MAXIMIZE_POPULARITY": 0.15,
            "MINIMIZE_WASTE": 0.10,
        }

    solutions: list[MeOptimizationSolution] = []
    all_reasons: list[str] = []

    if req.mode == "PARETO_FRONTIER":
        frontier_configs = [
            ("Cost-Minimized Pareto", {"MINIMIZE_FOOD_COST": 0.75, "MINIMIZE_WASTE": 0.15, "MAXIMIZE_POPULARITY": 0.10}),
            ("Balanced Margin & Popularity", {"MINIMIZE_FOOD_COST": 0.35, "MAXIMIZE_CONTRIBUTION_MARGIN": 0.35, "MAXIMIZE_POPULARITY": 0.20, "MINIMIZE_WASTE": 0.10}),
            ("Inventory & Waste-Minimized", {"MAXIMIZE_INVENTORY_UTILIZATION": 0.50, "MINIMIZE_WASTE": 0.30, "MINIMIZE_FOOD_COST": 0.20}),
        ]
        for rank, (label, w_cfg) in enumerate(frontier_configs, start=1):
            sol, reasons = _solve_cpsat_once(req, eligible, w_cfg, label, pareto_rank=rank)
            if sol is not None:
                solutions.append(sol)
            else:
                all_reasons.extend(reasons)
    elif req.mode == "LEXICOGRAPHIC":
        # Priority 1: strict primary objective dominant weight, secondary tie-breakers
        ordered = req.objectives if req.objectives else [
            OptObjectiveWeight(objective="MINIMIZE_FOOD_COST", weight=1.0),
            OptObjectiveWeight(objective="MAXIMIZE_POPULARITY", weight=0.1),
        ]
        lex_weights: dict[str, float] = {}
        factor = 100.0
        for item in ordered:
            lex_weights[item.objective] = factor
            factor *= 0.1
        sol, reasons = _solve_cpsat_once(req, eligible, lex_weights, "Lexicographic Optimal", pareto_rank=1)
        if sol is not None:
            solutions.append(sol)
        else:
            all_reasons.extend(reasons)
    else:
        label = "Single-Objective Optimal" if req.mode == "SINGLE_OBJECTIVE" else "Weighted Multi-Objective Optimal"
        sol, reasons = _solve_cpsat_once(req, eligible, base_weights, label, pareto_rank=1)
        if sol is not None:
            solutions.append(sol)
        else:
            all_reasons.extend(reasons)

    if not solutions:
        return MeMultiObjectiveOptimizeResponse(
            status="INFEASIBLE",
            solutions=[],
            infeasibility_reasons=list(dict.fromkeys(all_reasons or ["No feasible solution found under hard constraints."])),
            constraint_audit={
                "eligible_candidates": len(eligible),
                "rejected_candidates": len(rejected),
                "rejected_reasons": rejected[:20],
            },
        )

    best_status = "OPTIMAL" if all(s.is_globally_optimal for s in solutions) else "FEASIBLE"
    return MeMultiObjectiveOptimizeResponse(
        status=best_status,
        solutions=solutions,
        infeasibility_reasons=[],
        constraint_audit={
            "eligible_candidates": len(eligible),
            "rejected_candidates": len(rejected),
            "rejected_reasons": rejected[:20],
            "hard_constraints_never_relaxed": True,
        },
    )


def optimize_inventory_driven_menu(req: InventoryMenuOptimizeRequest) -> InventoryMenuOptimizeResponse:
    # Strictly exclude reserved, blocked, and expired stock (only eligible_qty is usable; FEFO prioritized)
    eligible_map: dict[str, float] = {}
    excluded_summary = {"reserved_qty": 0.0, "blocked_qty": 0.0, "expired_qty": 0.0}
    expiring_bonus_ingredients: set[str] = set()
    lot_unit_cost: dict[str, float] = {}

    for lot in req.inventory_lots:
        excluded_summary["reserved_qty"] += lot.reserved_qty
        excluded_summary["blocked_qty"] += lot.blocked_qty
        excluded_summary["expired_qty"] += lot.expired_qty
        if lot.eligible_qty > 0:
            eligible_map[lot.ingredient_id] = eligible_map.get(lot.ingredient_id, 0.0) + lot.eligible_qty
            lot_unit_cost[lot.ingredient_id] = lot.unit_cost
            if lot.days_to_expiry is not None and lot.days_to_expiry <= 5:
                expiring_bonus_ingredients.add(lot.ingredient_id)

    # Score each candidate by eligible stock utilization and expiry avoidance
    scored_candidates: list[tuple[float, float, OptDishCandidate]] = []
    for c in req.candidates:
        if not c.food_safety_approved or not c.is_approved_recipe:
            continue
        reqs = c.ingredient_requirements
        if not reqs:
            scored_candidates.append((c.eligible_inventory_coverage, 0.0, c))
            continue
        covered_val = 0.0
        total_val = 0.0
        expiring_val = 0.0
        for ing_id, qty_per_portion in reqs.items():
            needed = qty_per_portion * req.guest_count * 0.4
            avail = eligible_map.get(ing_id, 0.0)
            u_cost = lot_unit_cost.get(ing_id, 10.0)
            used = min(needed, avail)
            covered_val += used * u_cost
            total_val += max(1e-6, needed * u_cost)
            if ing_id in expiring_bonus_ingredients:
                expiring_val += used * u_cost
        ratio = covered_val / max(1e-6, total_val)
        scored_candidates.append((ratio, expiring_val, c))

    scored_candidates.sort(key=lambda t: (t[0], t[1], -t[2].ingredient_cost_per_portion), reverse=True)
    chosen = scored_candidates[: min(req.max_items, len(scored_candidates))]
    if len(chosen) < req.min_items:
        return InventoryMenuOptimizeResponse(
            status="INFEASIBLE",
            inventory_utilization_percent=0.0,
            avoidable_purchase_cost=0.0,
            expiry_avoidance_value=0.0,
            excluded_stock_summary={k: round(v, 3) for k, v in excluded_summary.items()},
            food_cost_percent=0.0,
            expected_contribution_margin=0.0,
        )

    portions_each = max(1, int(round(req.guest_count / max(1, len(chosen)) * 1.1)))
    remaining_eligible = dict(eligible_map)
    selected: list[SelectedDishAllocation] = []
    avoidable_purchase = 0.0
    expiry_saved = 0.0
    shortages: dict[str, float] = {}
    tot_cost = 0.0
    tot_rev = 0.0
    tot_needed_value = 0.0

    for ratio, _, c in chosen:
        u_cost = c.ingredient_cost_per_portion + c.variable_cost_per_portion
        line_cost = portions_each * u_cost
        line_rev = portions_each * max(c.selling_price, 1.0)
        tot_cost += line_cost
        tot_rev += line_rev

        for ing_id, qty_per_p in c.ingredient_requirements.items():
            need = qty_per_p * portions_each
            avail = remaining_eligible.get(ing_id, 0.0)
            used = min(need, avail)
            remaining_eligible[ing_id] = max(0.0, avail - used)
            u_c = lot_unit_cost.get(ing_id, 5.0)
            tot_needed_value += need * u_c
            avoidable_purchase += used * u_c
            if ing_id in expiring_bonus_ingredients:
                expiry_saved += used * u_c
            if need > used + 1e-6:
                shortages[ing_id] = round(shortages.get(ing_id, 0.0) + (need - used), 3)

        selected.append(
            SelectedDishAllocation(
                dish_id=c.dish_id,
                name=c.name,
                category=c.category,
                planned_portions=portions_each,
                unit_cost=round(u_cost, 4),
                total_cost=round(line_cost, 2),
                contribution_margin=round(line_rev - line_cost, 2),
                prep_minutes=round(portions_each * (c.prep_minutes_per_batch / max(1.0, c.batch_size_portions)), 2),
                inventory_coverage=round(ratio, 3),
            )
        )

    inv_util = (avoidable_purchase / max(1e-6, tot_needed_value) * 100.0) if tot_needed_value > 0 else (
        sum(s.inventory_coverage for s in selected) / max(1, len(selected)) * 100.0
    )
    fc_pct = (tot_cost / tot_rev * 100.0) if tot_rev > 0 else 0.0

    return InventoryMenuOptimizeResponse(
        status="OPTIMAL" if fc_pct <= req.max_food_cost_percent else "FEASIBLE",
        selected_dishes=selected,
        inventory_utilization_percent=round(min(100.0, inv_util), 2),
        avoidable_purchase_cost=round(avoidable_purchase, 2),
        expiry_avoidance_value=round(expiry_saved, 2),
        remaining_shortages=shortages,
        excluded_stock_summary={k: round(v, 3) for k, v in excluded_summary.items()},
        food_cost_percent=round(fc_pct, 2),
        expected_contribution_margin=round(tot_rev - tot_cost, 2),
    )


def optimize_menu_pricing(req: PricingOptimizeRequest) -> PricingOptimizeResponse:
    recs: list[PricingRecommendationItem] = []
    tot_curr = 0.0
    tot_rec = 0.0

    for item in req.items:
        unit_total_cost = item.ingredient_cost + item.variable_cost
        curr_contrib = (item.current_price - unit_total_cost) * item.baseline_daily_units
        tot_curr += curr_contrib

        lo = max(item.min_price, item.current_price * (1.0 - item.max_change_percent / 100.0))
        hi = min(item.max_price, item.current_price * (1.0 + item.max_change_percent / 100.0))
        if hi < lo:
            lo, hi = item.current_price, item.current_price

        best_p = item.current_price
        best_contrib = curr_contrib
        best_units = item.baseline_daily_units

        elasticity_mode = "VALIDATED_ELASTICITY" if item.price_elasticity is not None else "FIXED_DEMAND_ASSUMPTION"
        disclosure = (
            f"Demand adjusted using validated price elasticity ({item.price_elasticity:.2f})."
            if item.price_elasticity is not None
            else "Elasticity not validated for this item; simulation assumes fixed daily demand."
        )

        # Grid search along approved step increment
        steps = max(1, int(round((hi - lo) / max(0.1, item.price_step))))
        candidates_p = sorted({round(lo + k * item.price_step, 2) for k in range(steps + 1)} | {round(hi, 2), round(item.current_price, 2)})
        for p in candidates_p:
            if p < lo - 1e-6 or p > hi + 1e-6:
                continue
            fc_pct = (item.ingredient_cost / p * 100.0) if p > 0 else 100.0
            if fc_pct > item.target_food_cost_percent + 5.0 and p < item.current_price:
                continue
            if item.price_elasticity is not None:
                pct_change = (p - item.current_price) / item.current_price
                exp_units = max(0.0, item.baseline_daily_units * (1.0 + item.price_elasticity * pct_change))
            else:
                exp_units = item.baseline_daily_units
            contrib = (p - unit_total_cost) * exp_units
            if contrib > best_contrib + 1e-6:
                best_contrib = contrib
                best_p = p
                best_units = exp_units

        tot_rec += best_contrib
        recs.append(
            PricingRecommendationItem(
                menu_item_id=item.menu_item_id,
                name=item.name,
                current_price=round(item.current_price, 2),
                recommended_price=round(best_p, 2),
                price_delta_percent=round(((best_p - item.current_price) / item.current_price) * 100.0, 2),
                expected_units=round(best_units, 2),
                current_contribution=round(curr_contrib, 2),
                recommended_contribution=round(best_contrib, 2),
                recommended_food_cost_percent=round((item.ingredient_cost / best_p * 100.0) if best_p > 0 else 0.0, 2),
                elasticity_mode=elasticity_mode,
                assumption_disclosure=disclosure,
            )
        )

    return PricingOptimizeResponse(
        recommendations=recs,
        total_current_contribution=round(tot_curr, 2),
        total_recommended_contribution=round(tot_rec, 2),
        requires_manual_publish_approval=True,
    )
