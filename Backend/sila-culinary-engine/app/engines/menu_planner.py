from __future__ import annotations

import math
import re
from decimal import Decimal, ROUND_CEILING, ROUND_HALF_UP
from typing import Iterable

import numpy as np
from rapidfuzz import fuzz
from scipy.optimize import Bounds, LinearConstraint, milp

from pydantic import BaseModel

from app.providers.adapters import ClaudeSdkAdapter, GeminiSdkAdapter, LocalOllamaAdapter
from app.providers.router import (
    AiExecutionRequest,
    AiProviderRouter,
    ProviderPolicy,
    ProviderUnavailableError,
)
from app.schemas.event_menu_ai import (
    GeneratedMenuLineDto,
    MenuGenerateRequest,
    MenuGenerateResponse,
    MenuOptimizeRequest,
    MenuOptimizeResponse,
    OptimizedMenuSelection,
    PortionCalculationLineResult,
    PortionCalculationRequest,
    PortionCalculationResponse,
    PublishedRecipeCatalogItem,
    SemanticRecipeSearchMatch,
    SemanticRecipeSearchRequest,
    SemanticRecipeSearchResponse,
)


_INJECTION_RE = re.compile(
    r"(ignore\s+(all\s+)?previous\s+instructions|system\s*:|assistant\s*:|<\s*script|drop\s+table)",
    re.IGNORECASE,
)


def sanitize_untrusted_brief(text: str) -> str:
    cleaned = _INJECTION_RE.sub("[FILTERED]", text or "")
    return cleaned.strip()[:2000]


def _norm_tokens(text: str) -> set[str]:
    return {t for t in re.split(r"[^a-z0-9]+", (text or "").lower()) if len(t) >= 2}


def _d(val: float | int | str | Decimal) -> Decimal:
    return Decimal(str(val))


def _r2(val: Decimal) -> float:
    return float(val.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP))


def _ceil_int(val: Decimal) -> int:
    return int(val.to_integral_value(rounding=ROUND_CEILING))


def _is_allergen_safe(
    item: PublishedRecipeCatalogItem,
    excluded_allergens: set[str],
    strict_allergen_safety: bool,
) -> tuple[bool, str | None]:
    if not excluded_allergens:
        return True, None
    if strict_allergen_safety and item.unknown_allergens:
        return False, "Unknown allergen metadata excluded under strict food-safety mode."
    item_allergens = {a.strip().lower() for a in item.verified_allergens if a.strip()}
    hit = excluded_allergens & item_allergens
    if hit:
        return False, f"Contains excluded allergen(s): {', '.join(sorted(hit))}."
    return True, None


def semantic_search_recipes(req: SemanticRecipeSearchRequest) -> SemanticRecipeSearchResponse:
    query = sanitize_untrusted_brief(req.query)
    q_lower = query.lower()
    q_tokens = _norm_tokens(query)
    req_diet = {d.strip().lower() for d in req.required_dietary_tags if d.strip()}
    excl_all = {a.strip().lower() for a in req.excluded_allergens if a.strip()}

    matches: list[SemanticRecipeSearchMatch] = []
    filtered_safety = 0
    warnings: list[str] = []

    for item in req.catalog:
        if req.require_published and not item.is_published:
            continue
        if req.require_property_eligible and not item.property_eligible:
            continue
        if req.category and item.category.strip().lower() != req.category.strip().lower():
            continue
        if req.cuisine and item.cuisine.strip().lower() != req.cuisine.strip().lower():
            continue
        if req.max_cost_per_portion is not None and item.cost_per_portion is not None:
            if item.cost_per_portion > req.max_cost_per_portion:
                continue

        # Explicit dietary constraint (not semantic similarity)
        item_diet = {d.strip().lower() for d in item.dietary_tags if d.strip()}
        if req_diet and not req_diet.issubset(item_diet):
            continue

        safe, _reason = _is_allergen_safe(item, excl_all, req.strict_allergen_safety)
        if not safe:
            filtered_safety += 1
            continue

        # Hybrid exact + fuzzy + token-semantic score
        haystack = f"{item.recipe_code} {item.name} {item.description} {item.category} {item.cuisine} {' '.join(item.dietary_tags)}"
        if not q_lower:
            score = 0.85
            reason = "Filter match"
        elif q_lower == item.recipe_code.lower() or q_lower == item.name.lower():
            score = 1.0
            reason = "Exact Recipe ID / Name match"
        else:
            fuzzy = fuzz.WRatio(q_lower, f"{item.name} {item.cuisine} {item.category}".lower()) / 100.0
            h_tokens = _norm_tokens(haystack)
            overlap = len(q_tokens & h_tokens) / max(1, len(q_tokens))
            score = min(0.99, round(0.55 * fuzzy + 0.45 * overlap, 4))
            if score < 0.20 and q_lower not in haystack.lower():
                continue
            reason = f"Hybrid lexical/semantic ({int(score * 100)}%)"

        matches.append(
            SemanticRecipeSearchMatch(
                recipe_id=item.recipe_id,
                recipe_code=item.recipe_code,
                recipe_version=item.recipe_version,
                name=item.name,
                category=item.category,
                cuisine=item.cuisine,
                course_hint=item.course_hint,
                score=score,
                cost_per_portion=item.cost_per_portion,
                currency=item.currency,
                dietary_tags=item.dietary_tags,
                verified_allergens=item.verified_allergens,
                unknown_allergens=item.unknown_allergens,
                match_reason=reason,
            )
        )

    matches.sort(key=lambda m: (-m.score, m.cost_per_portion if m.cost_per_portion is not None else 1e9, m.name))
    if filtered_safety > 0:
        warnings.append(
            f"{filtered_safety} recipe(s) excluded due to verified or unknown allergen safety constraints."
        )

    return SemanticRecipeSearchResponse(
        results=matches[: req.limit],
        filtered_out_due_to_safety=filtered_safety,
        warnings=warnings,
    )


def calculate_event_menu_portions_and_cost(req: PortionCalculationRequest) -> PortionCalculationResponse:
    guaranteed = Decimal(req.guaranteed_guests)
    allowance_factor = Decimal("1") + (_d(req.additional_allowance_percent) / Decimal("100"))
    planning = _ceil_int(guaranteed * allowance_factor)
    if req.expected_guests > planning and req.additional_allowance_percent == 0:
        planning = req.expected_guests

    billable = int(guaranteed) if req.billable_policy == "GUARANTEED_ONLY" else max(int(guaranteed), planning)

    if req.fixed_package_price is not None and req.fixed_package_price > 0:
        est_revenue = _d(req.fixed_package_price)
    else:
        est_revenue = Decimal(billable) * _d(req.net_price_per_guest)

    target_fc_ratio = _d(req.target_food_cost_percent) / Decimal("100")
    menu_budget = _d(req.menu_budget) if req.menu_budget is not None else (est_revenue * target_fc_ratio)

    food_cost = Decimal("0")
    bev_cost = Decimal("0")
    missing_cost_lines = 0
    warnings: list[str] = []
    out_lines: list[PortionCalculationLineResult] = []

    # Validate mutually exclusive plated allocations by session
    plated_groups: dict[str, int] = {}

    for line in req.lines:
        eff_guests = line.session_guest_count if line.session_guest_count is not None else planning
        line_warnings: list[str] = []
        bev_liters: float | None = None
        edible_kg: float | None = None

        if line.manual_portions_override is not None:
            portions = int(line.manual_portions_override)
        elif line.service_style == "PLATED" and line.allocated_portions is not None:
            portions = int(line.allocated_portions)
            grp_key = f"{line.session_id or 'MAIN'}:{line.course_name}"
            plated_groups[grp_key] = plated_groups.get(grp_key, 0) + portions
        elif line.is_beverage or line.service_style in ("BEVERAGE_RECEPTION", "COFFEE_BREAK"):
            raw_servings = Decimal(eff_guests) * _d(line.servings_per_guest) * (_d(line.uptake_percent) / Decimal("100"))
            portions = _ceil_int(raw_servings) if req.round_portions_up else int(raw_servings.to_integral_value(rounding=ROUND_HALF_UP))
            if line.serving_volume_ml:
                liters = Decimal(portions) * _d(line.serving_volume_ml) / Decimal("1000")
                bev_liters = float(liters.quantize(Decimal("0.001"), rounding=ROUND_HALF_UP))
        else:
            raw_portions = Decimal(eff_guests) * (_d(line.uptake_percent) / Decimal("100")) * _d(line.serving_frequency)
            portions = _ceil_int(raw_portions) if req.round_portions_up else int(raw_portions.to_integral_value(rounding=ROUND_HALF_UP))

        if line.service_style == "LIVE_STATION" and line.verified_station_capacity_per_hour is None:
            line_warnings.append("Live station capacity unverified; detailed equipment/staffing belongs to Phase 4A.")

        if not line.is_beverage and line.portion_size_g > 0:
            edible_kg = float((Decimal(portions) * _d(line.portion_size_g) / Decimal("1000")).quantize(Decimal("0.001"), rounding=ROUND_HALF_UP))

        line_cost_val: float | None = None
        cost_missing = line.cost_per_portion is None
        if cost_missing:
            missing_cost_lines += 1
            line_warnings.append("Missing approved recipe cost per portion; not defaulted to zero.")
        else:
            lc = Decimal(portions) * _d(line.cost_per_portion)
            line_cost_val = _r2(lc)
            if line.is_beverage:
                bev_cost += lc
            else:
                food_cost += lc

        out_lines.append(
            PortionCalculationLineResult(
                line_id=line.line_id,
                dish_name=line.dish_name,
                course_name=line.course_name,
                service_style=line.service_style,
                effective_guests=eff_guests,
                planned_portions=portions,
                edible_quantity_kg=edible_kg,
                beverage_liters=bev_liters,
                unit_cost_per_portion=line.cost_per_portion,
                estimated_line_cost=line_cost_val,
                cost_missing=cost_missing,
                warnings=line_warnings,
            )
        )

    plated_valid = True
    for grp, total_alloc in plated_groups.items():
        if total_alloc != planning and total_alloc != int(guaranteed):
            plated_valid = False
            warnings.append(
                f"Plated course {grp} allocated {total_alloc} meal(s) vs {planning} planning / {int(guaranteed)} guaranteed guest(s)."
            )

    total_cost = food_cost + bev_cost
    cost_per_guaranteed = _r2(total_cost / guaranteed) if guaranteed > 0 else 0.0
    cost_per_planned = _r2(total_cost / Decimal(planning)) if planning > 0 else 0.0
    menu_cost_pct = float(((total_cost / est_revenue) * Decimal("100")).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)) if est_revenue > 0 else None
    remaining_budget = menu_budget - total_cost
    budget_exceeded = total_cost > menu_budget

    if missing_cost_lines > 0:
        warnings.append(f"{missing_cost_lines} menu line(s) are missing verified recipe cost.")

    return PortionCalculationResponse(
        guaranteed_guests=int(guaranteed),
        planning_guests=planning,
        billable_guests=billable,
        currency=req.currency,
        estimated_revenue=_r2(est_revenue),
        total_food_cost=_r2(food_cost),
        total_beverage_cost=_r2(bev_cost),
        total_menu_cost=_r2(total_cost),
        missing_cost_lines=missing_cost_lines,
        cost_per_guaranteed_guest=cost_per_guaranteed,
        cost_per_planned_guest=cost_per_planned,
        menu_cost_percent=menu_cost_pct,
        menu_budget=_r2(menu_budget),
        remaining_menu_budget=_r2(remaining_budget),
        budget_variance=_r2(remaining_budget),
        budget_exceeded=budget_exceeded,
        plated_allocation_valid=plated_valid,
        warnings=warnings,
        lines=out_lines,
    )


def optimize_menu_milp(req: MenuOptimizeRequest) -> MenuOptimizeResponse:
    """
    Deterministic constraint-based menu optimization via SciPy HiGHS MILP.
    Preserves locked dishes, enforces dietary and allergen safety hard constraints,
    and explicitly explains conflicting constraints when infeasible.
    """
    locked_set = set(req.locked_recipe_ids)
    prohibited_set = set(req.prohibited_recipe_ids)
    excl_allergens = {a.strip().lower() for a in req.excluded_allergens if a.strip()}
    req_diet = [d.strip().lower() for d in req.required_dietary_tags if d.strip()]

    candidates: list[PublishedRecipeCatalogItem] = []
    conflicts: list[str] = []

    for item in req.catalog:
        if not item.is_published or not item.property_eligible:
            continue
        if item.recipe_id in prohibited_set and item.recipe_id not in locked_set:
            continue
        safe, reason = _is_allergen_safe(item, excl_allergens, req.strict_allergen_safety)
        if not safe:
            if item.recipe_id in locked_set:
                conflicts.append(f"Locked recipe '{item.name}' violates allergen safety: {reason}")
            continue
        if item.cost_per_portion is None:
            if item.recipe_id in locked_set:
                conflicts.append(f"Locked recipe '{item.name}' is missing verified cost per portion.")
            continue
        candidates.append(item)

    for lock_id in locked_set:
        if not any(c.recipe_id == lock_id for c in candidates) and not any(lock_id in c for c in conflicts):
            conflicts.append(f"Locked recipe ID '{lock_id}' is not an eligible published recipe with verified cost.")

    if conflicts:
        return MenuOptimizeResponse(
            feasible=False,
            budget=req.max_budget,
            conflicting_constraints=conflicts,
            suggested_adjustments=["Unlock conflicting recipes or update verified allergen/cost metadata."],
            explanation="Optimization blocked by incompatible locked recipe constraints.",
        )

    n = len(candidates)
    if n == 0:
        return MenuOptimizeResponse(
            feasible=False,
            budget=req.max_budget,
            conflicting_constraints=["No eligible published recipes with verified cost and safe allergen metadata found."],
            suggested_adjustments=["Publish eligible recipes or relax allergen/property filters."],
            explanation="No feasible candidate set.",
        )

    portions_per_item = [
        _ceil_int(Decimal(req.planning_guests) * (_d(req.default_uptake_percent) / Decimal("100")))
        for _ in candidates
    ]
    line_costs = np.array(
        [float(_d(candidates[i].cost_per_portion or 0) * Decimal(portions_per_item[i])) for i in range(n)],
        dtype=float,
    )

    # Objective weights based on strategy
    obj = np.zeros(n, dtype=float)
    for i, c in enumerate(candidates):
        if req.strategy == "LOWEST_COST":
            obj[i] = line_costs[i]
        elif req.strategy == "MAX_STOCK":
            obj[i] = line_costs[i] * 0.1 - c.available_stock_score * 1000.0
        elif req.strategy == "MIN_COMPLEXITY":
            obj[i] = c.prep_complexity * 1000.0 + line_costs[i] * 0.05
        elif req.strategy == "PREMIUM_EXPERIENCE":
            obj[i] = -line_costs[i] * 0.25 - c.available_stock_score * 100.0
        else:  # BALANCED
            obj[i] = line_costs[i] - c.available_stock_score * 250.0 + c.prep_complexity * 100.0

    lb = np.zeros(n, dtype=float)
    ub = np.ones(n, dtype=float)
    for i, c in enumerate(candidates):
        if c.recipe_id in locked_set:
            lb[i] = 1.0

    rows: list[np.ndarray] = []
    row_lb: list[float] = []
    row_ub: list[float] = []

    # 1. Hard budget constraint
    rows.append(line_costs)
    row_lb.append(-np.inf)
    row_ub.append(float(req.max_budget))

    # 2. Course exact/min count constraints
    for course, count in req.required_courses.items():
        if count <= 0:
            continue
        vec = np.array([1.0 if c.course_hint.strip().lower() == course.strip().lower() else 0.0 for c in candidates])
        if np.sum(vec) < count:
            conflicts.append(f"Course '{course}' requires {count} recipe(s), but only {int(np.sum(vec))} safe published candidate(s) exist.")
        rows.append(vec)
        row_lb.append(float(count))
        row_ub.append(float(count))

    # 3. Required dietary coverage constraints (at least 1 item per required dietary tag)
    for tag in req_diet:
        vec = np.array([1.0 if tag in {t.strip().lower() for t in c.dietary_tags} else 0.0 for c in candidates])
        if np.sum(vec) < 1:
            conflicts.append(f"No safe published recipe satisfies required dietary tag '{tag}'.")
        rows.append(vec)
        row_lb.append(1.0)
        row_ub.append(np.inf)

    if conflicts:
        return MenuOptimizeResponse(
            feasible=False,
            budget=req.max_budget,
            conflicting_constraints=conflicts,
            suggested_adjustments=[
                "Add published recipes for the missing course/dietary category or create an AI Recipe Planner draft (Phase 1A)."
            ],
            explanation="Optimization infeasible due to missing candidate coverage.",
        )

    constraints = LinearConstraint(np.vstack(rows), row_lb, row_ub)
    res = milp(c=obj, integrality=np.ones(n), bounds=Bounds(lb, ub), constraints=constraints)

    if not res.success:
        # Compute unconstrained minimum cost for courses to explain budget gap clearly
        locked_cost = sum(line_costs[i] for i, c in enumerate(candidates) if c.recipe_id in locked_set)
        min_course_cost = 0.0
        for course, count in req.required_courses.items():
            if count <= 0:
                continue
            cc = sorted(line_costs[i] for i, c in enumerate(candidates) if c.course_hint.strip().lower() == course.strip().lower())
            min_course_cost += sum(cc[:count])
        detail = [
            f"Hard budget {req.max_budget:.2f} is incompatible with required courses / dietary / locked items (minimum course cost approx. {max(locked_cost, min_course_cost):.2f})."
        ]
        return MenuOptimizeResponse(
            feasible=False,
            budget=req.max_budget,
            conflicting_constraints=detail,
            suggested_adjustments=[
                f"Increase menu budget to at least {max(locked_cost, min_course_cost):.2f}",
                "Reduce required course count or unlock high-cost mandatory dishes.",
            ],
            explanation="HiGHS MILP solver proved no feasible menu exists within hard budget and course constraints.",
        )

    selected: list[OptimizedMenuSelection] = []
    total_cost = Decimal("0")
    for i, val in enumerate(res.x):
        if val > 0.5:
            c = candidates[i]
            qty = portions_per_item[i]
            lc = _d(c.cost_per_portion or 0) * Decimal(qty)
            total_cost += lc
            selected.append(
                OptimizedMenuSelection(
                    recipe_id=c.recipe_id,
                    recipe_code=c.recipe_code,
                    recipe_version=c.recipe_version,
                    name=c.name,
                    course_name=c.course_hint,
                    planned_portions=qty,
                    unit_cost_per_portion=float(c.cost_per_portion or 0),
                    estimated_line_cost=_r2(lc),
                    locked=c.recipe_id in locked_set,
                    dietary_tags=c.dietary_tags,
                )
            )

    tot_f = _r2(total_cost)
    return MenuOptimizeResponse(
        feasible=True,
        total_estimated_cost=tot_f,
        budget=req.max_budget,
        remaining_budget=round(req.max_budget - tot_f, 2),
        selected_items=selected,
        explanation=f"Solved optimal menu using {req.strategy} strategy within budget {req.max_budget:.2f}.",
    )


class _AiMenuPickItem(BaseModel):
    course_name: str = "Main Courses"
    recipe_id: str = ""
    dish_name: str = ""
    uptake_percent: float = 60.0
    rationale: str = ""


class _AiMenuPickResponse(BaseModel):
    title: str = ""
    description: str = ""
    items: list[_AiMenuPickItem] = []


async def generate_ai_menu(req: MenuGenerateRequest) -> MenuGenerateResponse:
    brief = sanitize_untrusted_brief(req.natural_language_brief)
    planning = _ceil_int(Decimal(req.guaranteed_guests) * (Decimal("1") + (_d(req.additional_allowance_percent) / Decimal("100"))))
    clarifications: list[str] = []
    if req.guaranteed_guests <= 0:
        clarifications.append("Confirm guaranteed guest count.")
    if not req.required_courses:
        clarifications.append("Specify required meal courses.")

    excl_allergens = {a.strip().lower() for a in req.excluded_allergens if a.strip()}
    req_diet = {d.strip().lower() for d in req.required_dietary_tags if d.strip()}
    cuisine_pref = {c.strip().lower() for c in req.cuisine_preferences if c.strip()}
    locked_set = set(req.locked_recipe_ids)

    catalog_by_id = {c.recipe_id: c for c in req.catalog if c.is_published and c.property_eligible}
    provider_used = "LOCAL_DETERMINISTIC"
    model_used = "sila-menu-planner-v1"
    ai_picks: list[dict] = []

    if req.provider_config is not None and (
        req.provider_config.operating_mode != "LOCAL" or req.provider_config.local_endpoint
    ):
        try:
            policy = ProviderPolicy(
                operating_mode=req.provider_config.operating_mode,
                tenant_id=req.provider_config.tenant_id,
                local_enabled=req.provider_config.local_enabled,
                local_endpoint=req.provider_config.local_endpoint,
                local_model=req.provider_config.local_model,
                gemini_enabled=req.provider_config.gemini_enabled,
                gemini_model=req.provider_config.gemini_model,
                gemini_api_key_present=bool(req.provider_config.gemini_api_key),
                gemini_api_key=req.provider_config.gemini_api_key,
                claude_enabled=req.provider_config.claude_enabled,
                claude_model=req.provider_config.claude_model,
                claude_api_key_present=bool(req.provider_config.claude_api_key),
                claude_api_key=req.provider_config.claude_api_key,
                tenant_external_ai_allowed=req.provider_config.tenant_external_ai_allowed,
                timeout_seconds=req.provider_config.timeout_seconds,
                max_retries=req.provider_config.max_retries,
                fallback_policy=req.provider_config.fallback_policy,
                task_routing=req.provider_config.task_routing,
            )
            router = AiProviderRouter(
                {
                    "LOCAL": LocalOllamaAdapter(),
                    "GEMINI": GeminiSdkAdapter(),
                    "CLAUDE": ClaudeSdkAdapter(),
                }
            )
            compact_catalog = [
                {
                    "recipe_id": c.recipe_id,
                    "name": c.name,
                    "course": c.course_hint,
                    "cuisine": c.cuisine,
                    "cost": c.cost_per_portion,
                    "dietary": c.dietary_tags,
                    "allergens": c.verified_allergens,
                }
                for c in list(catalog_by_id.values())[:60]
            ]
            ai_req = AiExecutionRequest(
                task_name="MENU_GENERATION",
                system_prompt="You are a hospitality culinary menu planner. Return JSON {title, description, items:[{course_name, recipe_id, dish_name, uptake_percent, rationale}]}. Only pick recipe_id values from provided catalog.",
                user_payload={
                    "event_type": req.event_type,
                    "theme": req.event_theme,
                    "brief": brief,
                    "guests": planning,
                    "service_style": req.service_style,
                    "courses": req.required_courses,
                    "dietary": req.required_dietary_tags,
                    "exclude_allergens": req.excluded_allergens,
                    "budget": req.menu_budget,
                    "catalog": compact_catalog,
                },
            )
            parsed, resp = await router.execute_structured(policy, ai_req, _AiMenuPickResponse)
            provider_used = resp.provider
            model_used = resp.model
            ai_picks = [item.model_dump() for item in parsed.items]
        except ProviderUnavailableError:
            pass

    selected_lines: list[GeneratedMenuLineDto] = []
    used_ids: set[str] = set()

    # Always preserve locked recipes first
    for rid in req.locked_recipe_ids:
        item = catalog_by_id.get(rid)
        if not item:
            continue
        safe, _ = _is_allergen_safe(item, excl_allergens, True)
        if not safe:
            continue
        used_ids.add(rid)
        uptake = 100.0 if item.is_beverage or req.service_style != "BUFFET" else 65.0
        portions = _ceil_int(Decimal(planning) * (_d(uptake) / Decimal("100")))
        line_cost = _r2(_d(item.cost_per_portion) * Decimal(portions)) if item.cost_per_portion is not None else None
        selected_lines.append(
            GeneratedMenuLineDto(
                course_name=item.course_hint,
                dish_name=item.name,
                recipe_id=item.recipe_id,
                recipe_code=item.recipe_code,
                recipe_version=item.recipe_version,
                line_status="PUBLISHED",
                uptake_percent=uptake,
                planned_portions=portions,
                unit_cost_per_portion=item.cost_per_portion,
                estimated_line_cost=line_cost,
                dietary_tags=item.dietary_tags,
                verified_allergens=item.verified_allergens,
                rationale="Locked chef recipe preserved.",
            )
        )

    # Validate AI picks strictly against actual authorized Recipe Master catalog
    for pick in ai_picks:
        if not isinstance(pick, dict):
            continue
        rid = str(pick.get("recipe_id") or "").strip()
        item = catalog_by_id.get(rid)
        if item is not None and rid not in used_ids:
            safe, _ = _is_allergen_safe(item, excl_allergens, True)
            if safe:
                used_ids.add(rid)
                uptake = float(pick.get("uptake_percent") or (100.0 if item.is_beverage else 60.0))
                portions = _ceil_int(Decimal(planning) * (_d(uptake) / Decimal("100")))
                line_cost = _r2(_d(item.cost_per_portion) * Decimal(portions)) if item.cost_per_portion is not None else None
                selected_lines.append(
                    GeneratedMenuLineDto(
                        course_name=str(pick.get("course_name") or item.course_hint),
                        dish_name=item.name,
                        recipe_id=item.recipe_id,
                        recipe_code=item.recipe_code,
                        recipe_version=item.recipe_version,
                        line_status="PUBLISHED",
                        uptake_percent=uptake,
                        planned_portions=portions,
                        unit_cost_per_portion=item.cost_per_portion,
                        estimated_line_cost=line_cost,
                        dietary_tags=item.dietary_tags,
                        verified_allergens=item.verified_allergens,
                        rationale=str(pick.get("rationale") or "AI selected from published Recipe Master."),
                    )
                )

    # Fill remaining required courses deterministically from published catalog
    courses_covered = {l.course_name.strip().lower() for l in selected_lines}
    for course in req.required_courses:
        if course.strip().lower() in courses_covered:
            continue
        course_candidates: list[tuple[float, PublishedRecipeCatalogItem]] = []
        for item in catalog_by_id.values():
            if item.recipe_id in used_ids:
                continue
            if item.course_hint.strip().lower() != course.strip().lower():
                continue
            safe, _ = _is_allergen_safe(item, excl_allergens, True)
            if not safe:
                continue
            score = 1.0
            if cuisine_pref and item.cuisine.strip().lower() in cuisine_pref:
                score += 2.0
            if req_diet and req_diet.intersection({t.strip().lower() for t in item.dietary_tags}):
                score += 1.5
            score += item.available_stock_score
            course_candidates.append((score, item))

        course_candidates.sort(key=lambda pair: (-pair[0], pair[1].cost_per_portion or 9999.0))
        if course_candidates:
            item = course_candidates[0][1]
            used_ids.add(item.recipe_id)
            uptake = 100.0 if item.is_beverage or req.service_style != "BUFFET" else 60.0
            portions = _ceil_int(Decimal(planning) * (_d(uptake) / Decimal("100")))
            line_cost = _r2(_d(item.cost_per_portion) * Decimal(portions)) if item.cost_per_portion is not None else None
            selected_lines.append(
                GeneratedMenuLineDto(
                    course_name=course,
                    dish_name=item.name,
                    recipe_id=item.recipe_id,
                    recipe_code=item.recipe_code,
                    recipe_version=item.recipe_version,
                    line_status="PUBLISHED",
                    uptake_percent=uptake,
                    planned_portions=portions,
                    unit_cost_per_portion=item.cost_per_portion,
                    estimated_line_cost=line_cost,
                    dietary_tags=item.dietary_tags,
                    verified_allergens=item.verified_allergens,
                    rationale="Matched published recipe for required course.",
                )
            )
        elif req.allow_missing_recipe_suggestions:
            portions = planning
            selected_lines.append(
                GeneratedMenuLineDto(
                    course_name=course,
                    dish_name=f"Chef {course} Creation ({req.cuisine_preferences[0] if req.cuisine_preferences else req.event_type})",
                    recipe_id=None,
                    recipe_code=None,
                    recipe_version=None,
                    line_status="RECIPE_REQUIRED",
                    uptake_percent=100.0,
                    planned_portions=portions,
                    unit_cost_per_portion=None,
                    estimated_line_cost=None,
                    dietary_tags=req.required_dietary_tags,
                    verified_allergens=[],
                    rationale="No eligible published recipe found; hand over to Phase 1A AI Recipe Planner.",
                )
            )

    total_cost = Decimal("0")
    diet_cov: dict[str, int] = {}
    missing_cnt = 0
    for ln in selected_lines:
        if ln.estimated_line_cost is not None:
            total_cost += _d(ln.estimated_line_cost)
        if ln.line_status == "RECIPE_REQUIRED":
            missing_cnt += 1
        for tag in ln.dietary_tags:
            diet_cov[tag] = diet_cov.get(tag, 0) + 1

    budget_warnings: list[str] = []
    tot_f = _r2(total_cost)
    if req.menu_budget is not None and tot_f > req.menu_budget:
        budget_warnings.append(f"Estimated menu cost {tot_f:.2f} exceeds menu budget {req.menu_budget:.2f}.")

    return MenuGenerateResponse(
        menu_title=f"{req.event_type} — {req.service_style.title()} Menu",
        menu_description=f"Curated menu for {planning} planning guests ({req.event_theme or req.event_type}).",
        planning_guests=planning,
        estimated_total_cost=tot_f,
        provider_used=provider_used,
        model_used=model_used,
        clarification_questions=clarifications,
        budget_warnings=budget_warnings,
        dietary_coverage=diet_cov,
        lines=selected_lines,
        missing_recipe_count=missing_cnt,
        explanation=f"Generated {len(selected_lines)} menu line(s) ({missing_cnt} requiring Phase 1A recipe creation).",
    )
