from __future__ import annotations

import math
import re
from typing import Any

import numpy as np
from rapidfuzz import fuzz
from scipy.optimize import Bounds, LinearConstraint, milp
from sklearn.ensemble import GradientBoostingRegressor
from sklearn.linear_model import Ridge

from app.schemas.me_ai_native import (
    MeBacktestForecastRequest,
    MeBacktestForecastResponse,
    MeBacktestModelMetric,
    MeCandidateDish,
    MeMultiConstraintOptimizeRequest,
    MeMultiConstraintOptimizeResponse,
    MeNlParseRequest,
    MeNlParseResponse,
    MePosMappingMatch,
    MePosMappingSuggestRequest,
    MePosMappingSuggestResponse,
    MeScenarioResult,
)

_INJECTION_RE = re.compile(
    r"(ignore\s+(all\s+)?previous\s+instructions|system\s*:|assistant\s*:|<\s*script|;\s*drop\s+table|select\s+\*\s+from)",
    re.IGNORECASE,
)

_NUMBER_WORDS = {
    "one": 1,
    "two": 2,
    "three": 3,
    "four": 4,
    "five": 5,
    "six": 6,
    "seven": 7,
    "eight": 8,
    "nine": 9,
    "ten": 10,
}


def sanitize_command(text: str) -> str:
    cleaned = _INJECTION_RE.sub("[FILTERED]", text or "")
    return re.sub(r"\s+", " ", cleaned).strip()[:2000]


def parse_nl_command(req: MeNlParseRequest) -> MeNlParseResponse:
    cleaned = sanitize_command(req.command)
    lower = cleaned.lower()

    intent = "GENERAL_QUERY"
    if any(k in lower for k in ["what happens if", "simulate", "increase by", "decreas", "reduce food cost", "contribution margin by"]):
        intent = "WHAT_IF_SIMULATE"
    elif any(k in lower for k in ["price shock", "if chicken", "if salmon", "affected menus", "affected recipes"]):
        intent = "PRICE_SHOCK"
    elif any(k in lower for k in ["weekly menu", "next week", "monthly menu", "plan a", "create ", "generate a", "build a"]):
        intent = "WEEKLY_PLAN" if ("weekly" in lower or "next week" in lower or "monthly" in lower) else "CREATE_MENU"
    elif any(k in lower for k in ["optimize", "optimise"]):
        intent = "OPTIMIZE_MENU"
    elif any(k in lower for k in ["alternative to", "replace ", "substitute"]):
        intent = "SUBSTITUTION"

    service_format: str | None = None
    for fmt, kw in [
        ("Buffet", ["buffet"]),
        ("Brunch", ["brunch"]),
        ("Banquet", ["banquet", "gala"]),
        ("Corporate catering", ["corporate", "catering"]),
        ("Set menu", ["set menu"]),
        ("Tasting menu", ["tasting"]),
        ("Room service", ["room service"]),
        ("A la carte", ["a la carte", "à la carte"]),
    ]:
        if any(k in lower for k in kw):
            service_format = fmt
            break

    meal_period: str | None = None
    for mp in ["Breakfast", "Lunch", "Dinner", "Brunch", "All-Day"]:
        if mp.lower() in lower:
            meal_period = mp
            break

    cuisine: str | None = None
    for cu in ["International", "Arabic", "Mediterranean", "Italian", "Asian", "Indian", "French", "Seafood", "Grill"]:
        if cu.lower() in lower:
            cuisine = cu
            break

    covers: int | None = None
    m_cov = re.search(r"(\d{1,5})\s*(?:guests?|covers?|pax|people)", lower)
    if m_cov:
        covers = int(m_cov.group(1))
    else:
        m_to = re.search(r"from\s+\d+\s+to\s+(\d{1,5})", lower)
        if m_to and "guest" in lower or (m_to and "cover" in lower):
            covers = int(m_to.group(1))

    selling_price: float | None = None
    m_price = re.search(r"(?:aed|usd|eur|sar|\$|price\s*(?:of|at)?)\s*(\d+(?:\.\d+)?)", lower)
    if not m_price:
        m_price = re.search(r"(\d+(?:\.\d+)?)\s*(?:aed|usd|eur|sar)\s*(?:selling|price)?", lower)
    if m_price:
        selling_price = float(m_price.group(1))

    max_fc: float | None = None
    m_fc = re.search(r"(?:max(?:imum)?|under|below|reduce(?:\s+food\s+cost)?\s+to|target)\s*(\d+(?:\.\d+)?)\s*%?\s*(?:food\s*cost)?", lower)
    if m_fc and ("food cost" in lower or "cost" in lower):
        max_fc = float(m_fc.group(1))

    min_cm_imp: float | None = None
    m_cm = re.search(r"(?:margin|contribution\s*margin).*?(\d+(?:\.\d+)?)\s*%", lower)
    if m_cm:
        min_cm_imp = float(m_cm.group(1))

    dietary: list[str] = []
    for d_tag, d_kw in [("Vegetarian", "vegetarian"), ("Vegan", "vegan"), ("Gluten-Free", "gluten"), ("Halal", "halal"), ("Dairy-Free", "dairy-free")]:
        if d_kw in lower:
            dietary.append(d_tag)

    req_cats: dict[str, int] = {}
    for cat_label, cat_kw in [("Dessert", "dessert"), ("Starter", "starter"), ("Appetizer", "appetizer"), ("Main", "main"), ("Salad", "salad"), ("Soup", "soup")]:
        m_cat = re.search(rf"(\d+|{'|'.join(_NUMBER_WORDS.keys())})\s+{cat_kw}s?", lower)
        if m_cat:
            raw_n = m_cat.group(1)
            req_cats[cat_label] = int(raw_n) if raw_n.isdigit() else _NUMBER_WORDS.get(raw_n, 1)

    max_inv = any(k in lower for k in ["available inventory", "available stock", "inventory utilization", "existing stock", "expiring"])
    preserve_cats = "without changing the number of menu categories" in lower or "keep categories" in lower

    opt_mode = "BALANCED"
    if max_inv:
        opt_mode = "INVENTORY_OPTIMIZED"
    elif "waste" in lower:
        opt_mode = "WASTE_OPTIMIZED"
    elif "budget" in lower or max_fc is not None:
        opt_mode = "BUDGET_OPTIMIZED"
    elif "margin" in lower or "profit" in lower:
        opt_mode = "MARGIN_OPTIMIZED"
    elif "premium" in lower:
        opt_mode = "PREMIUM_EXPERIENCE"
    elif "popular" in lower:
        opt_mode = "POPULARITY_OPTIMIZED"
    elif "healthy" in lower:
        opt_mode = "HEALTHY_MENU"
    elif "seasonal" in lower:
        opt_mode = "SEASONAL_MENU"
    elif "historical" in lower or "last month" in lower:
        opt_mode = "HISTORICAL_BEST"

    target_ing: str | None = None
    ing_pct: float | None = None
    m_ing = re.search(r"(?:if|alternative\s+(?:to|for)|replace)\s+([a-z][a-z\s-]{1,30}?)(?:\s+cost|\s+price|\s+increases|\s+decreases|\s+by|\?|$)", lower)
    if m_ing:
        cand = m_ing.group(1).strip()
        if cand not in {"guest count", "selling price", "food cost"}:
            target_ing = cand
    m_ipct = re.search(r"(?:increases?|up|rises?|by)\s*(\d+(?:\.\d+)?)\s*%", lower)
    if m_ipct and target_ing:
        ing_pct = float(m_ipct.group(1))

    return MeNlParseResponse(
        intent=intent,
        service_format=service_format,
        meal_period=meal_period,
        cuisine=cuisine,
        expected_covers=covers or req.default_covers,
        selling_price_target=selling_price,
        max_food_cost_percent=max_fc,
        min_margin_improvement_percent=min_cm_imp,
        required_dietary=dietary,
        required_categories=req_cats,
        optimization_mode=opt_mode,
        target_ingredient=target_ing,
        ingredient_price_change_percent=ing_pct,
        maximize_inventory_utilization=max_inv,
        preserve_categories=preserve_cats,
        sanitized_command=cleaned,
        confidence=0.91 if (covers or max_fc or selling_price or req_cats) else 0.78,
    )


def _passes_hard_constraints(
    dish: MeCandidateDish,
    excluded_allergens: set[str],
    covers: int,
) -> tuple[bool, str | None]:
    dish_allergens = {a.strip().lower() for a in dish.allergens if a.strip()}
    hit = excluded_allergens & dish_allergens
    if hit:
        return False, f"allergen:{','.join(sorted(hit))}"
    if dish.available_portions < 1:
        return False, "unavailable"
    return True, None


def _solve_milp_subset(
    eligible: list[MeCandidateDish],
    objective_scores: np.ndarray,
    covers: int,
    max_cost_per_guest: float | None,
    required_categories: dict[str, int],
    required_dietary: list[str],
    max_prep_minutes: int | None,
    min_items: int,
    max_items: int,
) -> tuple[list[int], str | None]:
    n = len(eligible)
    if n == 0:
        return [], "No eligible approved dishes passed allergen and availability constraints."

    target_count = max(min_items, min(n, sum(required_categories.values()) or min(max_items, max(4, n // 2))))
    if n < min_items:
        target_count = n

    constraints: list[LinearConstraint] = []

    # 1. Total dishes bounds
    row_all = np.ones((1, n))
    constraints.append(LinearConstraint(row_all, lb=min(min_items, n), ub=min(max_items, n)))

    # 2. Required category counts (hard constraint — never silently relaxed)
    for cat, req_cnt in required_categories.items():
        cat_vec = np.array([[1.0 if d.category.strip().lower() == cat.strip().lower() else 0.0 for d in eligible]])
        avail_cnt = int(cat_vec.sum())
        if avail_cnt < req_cnt:
            return [], f"Hard constraint conflict: required {req_cnt} '{cat}' dishes, but only {avail_cnt} eligible approved dishes exist."
        constraints.append(LinearConstraint(cat_vec, lb=req_cnt, ub=max(req_cnt, avail_cnt)))

    # 3. Required dietary coverage (at least 1 dish per required dietary tag)
    for dtag in required_dietary:
        d_vec = np.array([[1.0 if any(t.strip().lower() == dtag.strip().lower() for t in d.dietary_tags) else 0.0 for d in eligible]])
        if int(d_vec.sum()) == 0:
            return [], f"Hard constraint conflict: no approved allergen-safe dish satisfies dietary requirement '{dtag}'."
        constraints.append(LinearConstraint(d_vec, lb=1.0, ub=float(n)))

    # 4. Prep capacity cap
    if max_prep_minutes is not None and max_prep_minutes > 0:
        prep_vec = np.array([[float(d.prep_time_minutes) for d in eligible]])
        constraints.append(LinearConstraint(prep_vec, lb=0.0, ub=float(max_prep_minutes)))

    # 5. Average food cost per guest cap when target_count dishes are served across covers
    # For buffet/banquet service, average portion cost across selected dishes approximates guest food cost
    if max_cost_per_guest is not None and max_cost_per_guest > 0:
        # sum((cost_i - max_cost_per_guest) * x_i) <= 0 ensures mean dish cost <= max_cost_per_guest
        cost_diff = np.array([[float(d.food_cost_per_portion) - float(max_cost_per_guest) for d in eligible]])
        constraints.append(LinearConstraint(cost_diff, lb=-np.inf, ub=0.0))

    integrality = np.ones(n)
    bounds = Bounds(lb=np.zeros(n), ub=np.ones(n))
    res = milp(c=-objective_scores, integrality=integrality, bounds=bounds, constraints=constraints)

    if not res.success or res.x is None:
        return [], "Hard constraints cannot be simultaneously satisfied (food cost ceiling vs required categories/dietary coverage)."

    chosen = [idx for idx, val in enumerate(res.x) if val > 0.5]
    if not chosen:
        return [], "Optimization produced an empty menu selection under current constraints."
    return chosen, None


def optimize_menu_multi_constraint(req: MeMultiConstraintOptimizeRequest) -> MeMultiConstraintOptimizeResponse:
    excl = {a.strip().lower() for a in req.excluded_allergens if a.strip()}
    eligible: list[MeCandidateDish] = []
    for d in req.candidates:
        ok, _ = _passes_hard_constraints(d, excl, req.covers)
        if ok:
            eligible.append(d)

    if not eligible:
        return MeMultiConstraintOptimizeResponse(
            feasible=False,
            conflict_explanation="All candidate dishes were excluded by hard allergen or availability constraints.",
            recommended_scenario_code="A",
            scenarios=[],
        )

    selling_guest = req.selling_price_per_guest
    if selling_guest is None or selling_guest <= 0:
        avg_sp = float(np.mean([max(1.0, d.selling_price) for d in eligible]))
        selling_guest = max(1.0, avg_sp)

    max_cost_guest: float | None = req.max_budget_per_guest
    if req.max_food_cost_percent is not None and req.max_food_cost_percent > 0:
        fc_cap = selling_guest * (req.max_food_cost_percent / 100.0)
        max_cost_guest = fc_cap if max_cost_guest is None else min(max_cost_guest, fc_cap)

    costs = np.array([max(0.01, d.food_cost_per_portion) for d in eligible])
    cms = np.array([d.contribution_margin if d.contribution_margin > 0 else max(0.0, (d.selling_price or selling_guest) - d.food_cost_per_portion) for d in eligible])
    pops = np.array([max(1.0, d.popularity_score) for d in eligible])
    avails = np.array([min(1.0, d.available_portions / max(1.0, float(req.covers))) for d in eligible])
    wastes = np.array([max(0.1, d.estimated_waste_percent) for d in eligible])
    preps = np.array([1.0 / max(1.0, float(d.prep_time_minutes)) for d in eligible])

    def _norm(arr: np.ndarray) -> np.ndarray:
        span = float(arr.max() - arr.min())
        if span <= 1e-9:
            return np.ones_like(arr)
        return (arr - arr.min()) / span

    n_cost_inv = 1.0 - _norm(costs)
    n_cm = _norm(cms)
    n_pop = _norm(pops)
    n_avail = _norm(avails)
    n_waste_inv = 1.0 - _norm(wastes)
    n_prep = _norm(preps)

    w = req.weights
    balanced_score = (
        w.food_cost * n_cost_inv
        + w.contribution_margin * n_cm
        + w.popularity * n_pop
        + w.availability * n_avail
        + w.waste * n_waste_inv
        + w.production_feasibility * n_prep
    )

    strategy_defs: list[tuple[str, str, str, np.ndarray, float | None, str]] = [
        ("A", "Current Baseline", "BASELINE", np.ones(len(eligible)) * 0.5 + 0.05 * n_pop, None, "Unconstrained baseline across eligible items."),
        ("B", "Lowest Feasible Food Cost", "LOWEST_FOOD_COST", n_cost_inv + 0.05 * n_avail, max_cost_guest, "Minimizes food cost per guest while honoring hard constraints."),
        ("C", "Highest Contribution Margin", "HIGHEST_MARGIN", n_cm + 0.05 * n_pop, max_cost_guest, "Maximizes contribution margin per guest."),
        ("D", "Highest Predicted Popularity", "HIGHEST_POPULARITY", n_pop + 0.05 * n_cm, max_cost_guest, "Prioritizes proven guest-favorite dishes."),
        ("E", "Lowest Expected Waste", "LOWEST_WASTE", n_waste_inv + 0.1 * n_avail, max_cost_guest, "Prioritizes low-spoilage & high-yield dishes."),
        ("F", "Maximum Inventory Utilization", "MAX_INVENTORY_UTILIZATION", n_avail + 0.1 * n_cost_inv, max_cost_guest, "Prioritizes dishes backed by on-hand stock."),
        ("G", "Balanced Recommendation", "BALANCED", balanced_score, max_cost_guest, "Multi-criteria weighted balance across cost, margin, popularity, stock & waste."),
    ]

    min_items = max(1, min(len(eligible), sum(req.required_categories.values()) or 3))
    max_items = max(min_items, min(len(eligible), max(6, min_items + 2)))

    scenarios: list[MeScenarioResult] = []
    any_feasible = False
    first_conflict: str | None = None

    for code, label, strat, obj, cap, trade_note in strategy_defs:
        chosen_idx, conflict = _solve_milp_subset(
            eligible=eligible,
            objective_scores=obj,
            covers=req.covers,
            max_cost_per_guest=cap,
            required_categories=req.required_categories,
            required_dietary=req.required_dietary,
            max_prep_minutes=req.max_prep_capacity_minutes,
            min_items=min_items,
            max_items=max_items,
        )
        if conflict or not chosen_idx:
            if not first_conflict and code != "A":
                first_conflict = conflict
            scenarios.append(
                MeScenarioResult(
                    scenario_code=code,
                    label=label,
                    strategy=strat,
                    feasible=False,
                    conflict_reason=conflict,
                    pareto_optimal=False,
                    trade_off_summary=conflict or "Infeasible",
                )
            )
            continue

        any_feasible = True
        picked = [eligible[i] for i in chosen_idx]
        avg_food_cost = float(np.mean([d.food_cost_per_portion for d in picked]))
        total_food_cost = round(avg_food_cost * req.covers, 2)
        expected_rev = round(selling_guest * req.covers, 2)
        fc_pct = round((total_food_cost / expected_rev) * 100.0, 2) if expected_rev > 0 else 0.0
        cm_total = round(expected_rev - total_food_cost, 2)
        pop_idx = round(float(np.mean([d.popularity_score for d in picked])), 2)
        inv_util = round(float(np.mean([min(100.0, (d.available_portions / max(1.0, req.covers)) * 100.0) for d in picked])), 2)
        waste_pct = round(float(np.mean([d.estimated_waste_percent for d in picked])), 2)

        scenarios.append(
            MeScenarioResult(
                scenario_code=code,
                label=label,
                strategy=strat,
                feasible=True,
                selected_item_ids=[d.menu_item_id for d in picked],
                selected_item_names=[d.name for d in picked],
                food_cost_per_guest=round(avg_food_cost, 2),
                total_food_cost=total_food_cost,
                expected_revenue=expected_rev,
                food_cost_percent=fc_pct,
                contribution_margin=cm_total,
                popularity_index=pop_idx,
                inventory_utilization_percent=inv_util,
                estimated_waste_percent=waste_pct,
                pareto_optimal=True,
                trade_off_summary=trade_note,
            )
        )

    # Mark Pareto-dominated feasible scenarios (minimize food_cost_percent, maximize contribution_margin & popularity_index)
    feasible_list = [s for s in scenarios if s.feasible]
    for s in feasible_list:
        dominated = any(
            other.scenario_code != s.scenario_code
            and other.food_cost_percent <= s.food_cost_percent
            and other.contribution_margin >= s.contribution_margin
            and other.popularity_index >= s.popularity_index
            and (
                other.food_cost_percent < s.food_cost_percent
                or other.contribution_margin > s.contribution_margin
                or other.popularity_index > s.popularity_index
            )
            for other in feasible_list
        )
        s.pareto_optimal = not dominated

    rec_code = "G" if any(s.scenario_code == "G" and s.feasible for s in scenarios) else (feasible_list[0].scenario_code if feasible_list else "A")

    return MeMultiConstraintOptimizeResponse(
        feasible=any_feasible and (first_conflict is None or any(s.feasible and s.scenario_code != "A" for s in scenarios)),
        conflict_explanation=first_conflict if not any(s.feasible and s.scenario_code != "A" for s in scenarios) else None,
        recommended_scenario_code=rec_code,
        scenarios=scenarios,
    )


def _compute_metrics(actual: np.ndarray, pred: np.ndarray, lower: np.ndarray, upper: np.ndarray) -> tuple[float, float, float, float]:
    mae = float(np.mean(np.abs(actual - pred)))
    denom = float(np.sum(np.abs(actual)))
    wape = float(np.sum(np.abs(actual - pred)) / denom * 100.0) if denom > 1e-6 else 0.0
    bias = float(np.mean(pred - actual))
    cov = float(np.mean((actual >= lower) & (actual <= upper)) * 100.0)
    return round(mae, 3), round(wape, 2), round(bias, 3), round(cov, 2)


def evaluate_and_forecast_demand(req: MeBacktestForecastRequest) -> MeBacktestForecastResponse:
    series = [max(0.0, float(v)) for v in req.series]
    n = len(series)
    if n < 5:
        avg = float(np.mean(series)) if series else 0.0
        fc = [round(max(0.0, avg + (req.confirmed_events_by_day[i] if i < len(req.confirmed_events_by_day) else 0.0)), 2) for i in range(req.horizon)]
        lo = [round(max(0.0, v * 0.75), 2) for v in fc]
        hi = [round(v * 1.30, 2) for v in fc]
        return MeBacktestForecastResponse(
            selected_model="COLD_START_BASELINE",
            selected_model_version="cs-v1",
            data_quality="INSUFFICIENT_COLD_START",
            cold_start=True,
            forecast=fc,
            lower_bound=lo,
            upper_bound=hi,
            model_metrics=[MeBacktestModelMetric(model_name="COLD_START_BASELINE", model_version="cs-v1", mae=0.0, wape=0.0, bias=0.0, interval_coverage_percent=100.0, selected=True)],
            drivers=["Insufficient historical observations (<5 points); cold-start baseline + confirmed events applied."],
            warnings=["Cold-start limitation: fewer than 5 historical observations available."],
        )

    arr = np.array(series, dtype=float)
    zero_ratio = float(np.mean(arr <= 0.0))
    split = max(3, int(n * 0.75))
    if n - split < 2:
        split = n - 2
    train, test = arr[:split], arr[split:]

    def eval_naive(tr: np.ndarray, steps: int) -> np.ndarray:
        season = 7 if len(tr) >= 7 else len(tr)
        return np.array([tr[-(season - (i % season))] for i in range(steps)], dtype=float)

    def eval_ma(tr: np.ndarray, steps: int) -> np.ndarray:
        w = min(7, len(tr))
        val = float(np.mean(tr[-w:]))
        return np.full(steps, val, dtype=float)

    def eval_es(tr: np.ndarray, steps: int) -> np.ndarray:
        alpha = 0.35
        level = float(tr[0])
        for v in tr[1:]:
            level = alpha * float(v) + (1.0 - alpha) * level
        return np.full(steps, level, dtype=float)

    def _build_features(idx: int, vals: np.ndarray) -> list[float]:
        dow = idx % 7
        lag1 = float(vals[idx - 1]) if idx >= 1 else float(vals[0])
        lag7 = float(vals[idx - 7]) if idx >= 7 else lag1
        roll7 = float(np.mean(vals[max(0, idx - 7):idx])) if idx > 0 else lag1
        return [float(idx), math.sin(2 * math.pi * dow / 7.0), math.cos(2 * math.pi * dow / 7.0), lag1, lag7, roll7]

    def eval_ml(tr: np.ndarray, steps: int, kind: str) -> np.ndarray:
        if len(tr) < 6:
            return eval_ma(tr, steps)
        xs = np.array([_build_features(i, tr) for i in range(1, len(tr))], dtype=float)
        ys = tr[1:]
        model = Ridge(alpha=1.0) if kind == "RIDGE" else GradientBoostingRegressor(max_depth=2, n_estimators=40, random_state=42)
        model.fit(xs, ys)
        hist = list(tr)
        preds: list[float] = []
        for s_i in range(steps):
            feat = np.array([_build_features(len(hist), np.array(hist))], dtype=float)
            p = max(0.0, float(model.predict(feat)[0]))
            preds.append(p)
            hist.append(p)
        return np.array(preds, dtype=float)

    candidates = [
        ("SEASONAL_NAIVE", "sn-v1", eval_naive),
        ("MOVING_AVERAGE", "ma-v1", eval_ma),
        ("EXPONENTIAL_SMOOTHING", "es-v1", eval_es),
        ("TIME_SERIES_RIDGE", "ridge-v2", lambda tr, st: eval_ml(tr, st, "RIDGE")),
        ("GRADIENT_BOOSTING", "gbr-v2", lambda tr, st: eval_ml(tr, st, "GBR")),
    ]

    metrics: list[MeBacktestModelMetric] = []
    best_fn = eval_ma
    best_name = "MOVING_AVERAGE"
    best_ver = "ma-v1"
    best_wape = float("inf")
    best_std = float(np.std(arr))

    for name, ver, fn in candidates:
        pred = fn(train, len(test))
        res_std = max(1.0, float(np.std(test - pred)) if len(test) > 1 else float(np.std(arr) * 0.25))
        lo = np.maximum(0.0, pred - 1.28 * res_std)
        hi = pred + 1.28 * res_std
        mae, wape, bias, cov = _compute_metrics(test, pred, lo, hi)
        metrics.append(MeBacktestModelMetric(model_name=name, model_version=ver, mae=mae, wape=wape, bias=bias, interval_coverage_percent=cov, selected=False))
        if wape < best_wape:
            best_wape = wape
            best_name = name
            best_ver = ver
            best_fn = fn
            best_std = res_std

    for m in metrics:
        if m.model_name == best_name:
            m.selected = True

    future_raw = best_fn(arr, req.horizon)
    fc_out: list[float] = []
    lo_out: list[float] = []
    hi_out: list[float] = []
    for i in range(req.horizon):
        base = max(0.0, float(future_raw[i]))
        # Confirmed events added only above baseline (never double-counted inside historical series)
        ev = float(req.confirmed_events_by_day[i]) if i < len(req.confirmed_events_by_day) else 0.0
        val = round(base + max(0.0, ev), 2)
        band = max(1.0, 1.28 * best_std)
        fc_out.append(val)
        lo_out.append(round(max(0.0, val - band), 2))
        hi_out.append(round(val + band, 2))

    drivers = [
        f"Backtested {len(candidates)} models on {split}-point train / {len(test)}-point holdout (zero future leakage).",
        f"Selected {best_name} ({best_ver}) with lowest WAPE {best_wape:.2f}%.",
    ]
    if zero_ratio > 0.2:
        drivers.append(f"Intermittent demand detected ({zero_ratio*100:.0f}% zero-sale intervals); non-negative clipping applied.")
    if any(v > 0 for v in req.confirmed_events_by_day):
        drivers.append("Confirmed events blended cleanly atop baseline forecast without double-counting.")

    return MeBacktestForecastResponse(
        selected_model=best_name,
        selected_model_version=best_ver,
        data_quality="HIGH" if n >= 14 else "MODERATE",
        cold_start=False,
        forecast=fc_out,
        lower_bound=lo_out,
        upper_bound=hi_out,
        model_metrics=metrics,
        drivers=drivers,
        warnings=[],
    )


def suggest_pos_mappings_ai(req: MePosMappingSuggestRequest) -> MePosMappingSuggestResponse:
    raw = f"{req.pos_code} {req.pos_description}".strip()
    upper = raw.upper()

    # Extract portion size multiplier
    mult = 1.0
    size_var: str | None = None
    for tok, m_val, lbl in [
        (r"\b(LRG|LARGE|XL|JUMBO)\b", 1.5, "Large"),
        (r"\b(DBL|DOUBLE)\b", 2.0, "Double"),
        (r"\b(SML|SMALL|HALF|KID|MINI)\b", 0.5, "Small/Half"),
        (r"\b(REG|REGULAR|STD|STANDARD)\b", 1.0, "Standard"),
    ]:
        if re.search(tok, upper):
            mult = m_val
            size_var = lbl
            break

    # Extract modifier after '+' or 'EX'/'EXTRA'/'WITH'
    modifier: str | None = None
    m_mod = re.search(r"(?:\+|WITH|EXTRA|EX)\s+([A-Z0-9\s]{2,25})$", upper)
    if m_mod:
        mod_txt = m_mod.group(1).strip()
        if req.available_modifiers:
            best_m = max(req.available_modifiers, key=lambda x: fuzz.token_set_ratio(mod_txt, x.upper()))
            modifier = best_m if fuzz.token_set_ratio(mod_txt, best_m.upper()) >= 60 else mod_txt.title()
        else:
            modifier = mod_txt.title()

    base_desc = re.sub(r"(\+|WITH|EXTRA|EX)\s+[A-Z0-9\s]+$", "", upper).strip()
    base_desc = re.sub(r"\b(LRG|LARGE|XL|SML|SMALL|HALF|REG|REGULAR|DBL|DOUBLE)\b", "", base_desc).strip()

    # Expand common hospitality shorthand
    abbrevs = {
        "CHKN": "CHICKEN",
        "GRL": "GRILLED",
        "SLM": "SALMON",
        "BF": "BEEF",
        "PASTA": "PASTA",
        "BVRG": "BEVERAGE",
        "BRG": "BURGER",
        "SLD": "SALAD",
        "DST": "DESSERT",
    }
    tokens = [abbrevs.get(t, t) for t in re.split(r"[^A-Z0-9]+", base_desc) if t]
    expanded = " ".join(tokens)

    scored: list[MePosMappingMatch] = []
    for item in req.catalog:
        target = f"{item.name} {item.category} {' '.join(item.recipe_ingredients)}".upper()
        name_score = fuzz.token_set_ratio(expanded, item.name.upper())
        full_score = fuzz.token_set_ratio(expanded, target)
        score = 0.72 * name_score + 0.28 * full_score
        if req.pos_code.strip().lower() == item.name.strip().lower():
            score = 99.0
        conf = round(min(0.99, max(0.05, score / 100.0)), 2)
        ev_parts = [f"Normalized '{expanded}' vs '{item.name}' (token score {name_score:.0f}%)"]
        if size_var:
            ev_parts.append(f"size={size_var} ({mult}x)")
        if modifier:
            ev_parts.append(f"modifier='{modifier}'")
        scored.append(
            MePosMappingMatch(
                menu_item_id=item.menu_item_id,
                menu_item_name=item.name,
                confidence=conf,
                portion_multiplier=mult,
                size_variant=size_var,
                detected_modifier=modifier,
                evidence="; ".join(ev_parts),
            )
        )

    scored.sort(key=lambda x: x.confidence, reverse=True)
    best = scored[0] if scored else None
    alts = scored[1:4] if len(scored) > 1 else []
    requires_review = best is None or best.confidence < 0.92 or modifier is not None

    return MePosMappingSuggestResponse(
        pos_code=req.pos_code,
        pos_description=req.pos_description,
        requires_human_review=requires_review,
        best_match=best,
        alternatives=alts,
    )
