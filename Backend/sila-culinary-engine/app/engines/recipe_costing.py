from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP
from typing import Optional

from app.engines.ingredient_attributes import extract_ingredient_attributes
from app.schemas.costing_ai import (
    CostedIngredientLine,
    CostOptimizationRequest,
    CostOptimizationResponse,
    CostOptimizationSuggestion,
    RecipeCostingRequest,
    RecipeCostingResponse,
)

Q4 = Decimal("0.0001")
Q2 = Decimal("0.01")
HUNDRED = Decimal("100")


def _q4(val: Decimal) -> Decimal:
    return val.quantize(Q4, rounding=ROUND_HALF_UP)


def _q2(val: Decimal) -> Decimal:
    return val.quantize(Q2, rounding=ROUND_HALF_UP)


def _has_circular_dependency(root_ref: str, deps: list[str], sub_id: Optional[str]) -> bool:
    if sub_id and sub_id == root_ref:
        return True
    return root_ref in set(deps)


def calculate_recipe_costing(req: RecipeCostingRequest) -> RecipeCostingResponse:
    """
    Deterministic Decimal recipe costing engine (§19–§33 & §57):
    - Applies raw/edible yield without double-counting.
    - Applies separate wastage & production buffer layers cleanly.
    - Enforces currency compatibility (requires exchange rate when currency differs).
    - Keeps missing / expired prices explicit rather than substituting zero.
    - Separates Gross Margin vs Markup and Direct Cost vs Allocated Overhead.
    """
    costed_lines: list[CostedIngredientLine] = []
    warnings: list[str] = []

    total_ingredient_cost = Decimal("0")
    verified_ingredient_cost = Decimal("0")
    estimated_ingredient_cost = Decimal("0")
    sub_recipe_cost = Decimal("0")

    missing_count = 0
    expired_count = 0
    currency_mismatch_count = 0
    uom_conflict_count = 0
    circular_detected = False

    price_scale = Decimal("1") + (req.global_price_increase_percent / HUNDRED)

    for item in req.ingredients:
        line_warnings: list[str] = []

        # 1. Sub-recipe costing (§27)
        if item.is_sub_recipe:
            if _has_circular_dependency(req.recipe_id_or_ref, item.sub_recipe_dependencies, item.sub_recipe_id):
                circular_detected = True
                line_warnings.append("Circular sub-recipe dependency detected.")
                costed_lines.append(
                    CostedIngredientLine(
                        temp_ref=item.temp_ref,
                        description=item.description,
                        material_code=item.sub_recipe_id,
                        is_sub_recipe=True,
                        recipe_qty=_q4(item.recipe_qty),
                        recipe_uom=item.recipe_uom,
                        raw_qty=_q4(item.recipe_qty),
                        costing_qty=_q4(item.recipe_qty),
                        costing_uom=item.recipe_uom,
                        price_status="CIRCULAR_SUB_RECIPE",
                        warnings=line_warnings,
                    )
                )
                continue

            if item.sub_recipe_total_cost is None or item.sub_recipe_batch_yield is None or item.sub_recipe_batch_yield <= 0:
                missing_count += 1
                line_warnings.append("Sub-recipe batch cost or yield is unavailable.")
                costed_lines.append(
                    CostedIngredientLine(
                        temp_ref=item.temp_ref,
                        description=item.description,
                        material_code=item.sub_recipe_id,
                        is_sub_recipe=True,
                        recipe_qty=_q4(item.recipe_qty),
                        recipe_uom=item.recipe_uom,
                        raw_qty=_q4(item.recipe_qty),
                        costing_qty=_q4(item.recipe_qty),
                        costing_uom=item.recipe_uom,
                        price_status="MISSING",
                        warnings=line_warnings,
                    )
                )
                continue

            unit_sub_cost = (item.sub_recipe_total_cost / item.sub_recipe_batch_yield) * price_scale
            effective_qty = item.recipe_qty * (Decimal("1") + (req.global_buffer_percent / HUNDRED))
            line_cost = _q4(effective_qty * unit_sub_cost)
            sub_recipe_cost += line_cost
            total_ingredient_cost += line_cost
            verified_ingredient_cost += line_cost
            costed_lines.append(
                CostedIngredientLine(
                    temp_ref=item.temp_ref,
                    description=item.description,
                    material_code=item.sub_recipe_id,
                    is_sub_recipe=True,
                    recipe_qty=_q4(item.recipe_qty),
                    recipe_uom=item.recipe_uom,
                    raw_qty=_q4(effective_qty),
                    costing_qty=_q4(effective_qty),
                    costing_uom=item.recipe_uom,
                    effective_unit_cost=_q4(unit_sub_cost),
                    currency=req.reporting_currency,
                    ingredient_cost=line_cost,
                    price_source="SUB_RECIPE_SNAPSHOT",
                    price_status="VERIFIED",
                )
            )
            continue

        # 2. Yield conversion (EDIBLE -> RAW once only, §25)
        raw_qty = item.recipe_qty
        if item.quantity_basis == "EDIBLE":
            raw_qty = item.recipe_qty / (item.yield_percent / HUNDRED)

        # 3. Separate wastage & buffer layers (§26) — avoid overlapping double loss if EDIBLE yield already applied
        extra_loss_factor = Decimal("1")
        if item.prep_loss_percent > 0 and item.quantity_basis == "RAW":
            extra_loss_factor = extra_loss_factor / (Decimal("1") - (item.prep_loss_percent / HUNDRED))
        if item.cooking_loss_percent > 0:
            extra_loss_factor = extra_loss_factor / (Decimal("1") - (item.cooking_loss_percent / HUNDRED))
        if item.spoilage_percent > 0:
            extra_loss_factor = extra_loss_factor / (Decimal("1") - (item.spoilage_percent / HUNDRED))
        if req.wastage_multiplier_percent > 0:
            extra_loss_factor = extra_loss_factor * (Decimal("1") + (req.wastage_multiplier_percent / HUNDRED))

        total_buffer_pct = item.production_buffer_percent + req.global_buffer_percent
        buffer_factor = Decimal("1") + (total_buffer_pct / HUNDRED)
        adjusted_recipe_qty = raw_qty * extra_loss_factor * buffer_factor

        # 4. UOM conversion to costing base UOM
        costing_uom = (item.costing_uom or item.recipe_uom).upper()
        if not item.uom_compatible:
            uom_conflict_count += 1
            line_warnings.append(f"UOM conversion missing between {item.recipe_uom} and {costing_uom}.")
            costed_lines.append(
                CostedIngredientLine(
                    temp_ref=item.temp_ref,
                    description=item.description,
                    material_code=item.material_code,
                    recipe_qty=_q4(item.recipe_qty),
                    recipe_uom=item.recipe_uom,
                    raw_qty=_q4(raw_qty),
                    costing_qty=_q4(adjusted_recipe_qty),
                    costing_uom=costing_uom,
                    price_status="UOM_CONFLICT",
                    warnings=line_warnings,
                )
            )
            continue

        conv_factor = item.uom_conversion_factor if item.uom_conversion_factor is not None else Decimal("1")
        costing_qty = adjusted_recipe_qty * conv_factor

        # 5. Price resolution & unit normalization (§21–§23)
        raw_price = item.price_override if item.price_override is not None else item.unit_price
        if raw_price is None:
            missing_count += 1
            line_warnings.append("Authorized unit price is missing.")
            costed_lines.append(
                CostedIngredientLine(
                    temp_ref=item.temp_ref,
                    description=item.description,
                    material_code=item.material_code,
                    recipe_qty=_q4(item.recipe_qty),
                    recipe_uom=item.recipe_uom,
                    raw_qty=_q4(raw_qty),
                    costing_qty=_q4(costing_qty),
                    costing_uom=costing_uom,
                    price_source=item.price_source,
                    price_status="MISSING",
                    warnings=line_warnings,
                )
            )
            continue

        if item.is_price_expired and not item.allow_expired_fallback and item.price_override is None:
            expired_count += 1
            missing_count += 1
            line_warnings.append("Material price is expired and fallback is disabled.")
            costed_lines.append(
                CostedIngredientLine(
                    temp_ref=item.temp_ref,
                    description=item.description,
                    material_code=item.material_code,
                    recipe_qty=_q4(item.recipe_qty),
                    recipe_uom=item.recipe_uom,
                    raw_qty=_q4(raw_qty),
                    costing_qty=_q4(costing_qty),
                    costing_uom=costing_uom,
                    price_source=item.price_source,
                    price_status="MISSING",
                    warnings=line_warnings,
                )
            )
            continue

        price_currency = (item.price_currency or req.reporting_currency).upper()
        fx_rate = Decimal("1")
        if price_currency != req.reporting_currency.upper():
            if item.exchange_rate_to_reporting is None:
                currency_mismatch_count += 1
                line_warnings.append(f"Missing exchange rate from {price_currency} to {req.reporting_currency}.")
                costed_lines.append(
                    CostedIngredientLine(
                        temp_ref=item.temp_ref,
                        description=item.description,
                        material_code=item.material_code,
                        recipe_qty=_q4(item.recipe_qty),
                        recipe_uom=item.recipe_uom,
                        raw_qty=_q4(raw_qty),
                        costing_qty=_q4(costing_qty),
                        costing_uom=costing_uom,
                        currency=price_currency,
                        price_source=item.price_source,
                        price_status="CURRENCY_MISMATCH",
                        warnings=line_warnings,
                    )
                )
                continue
            fx_rate = item.exchange_rate_to_reporting

        # Normalize price per 1 unit of costing_uom:
        # e.g., AED 120 per 10 KG -> 12 AED/KG; or AED 120 per 1 CASE (10 KG) -> price_uom_factor=10 -> 12 AED/KG
        price_uom_factor = item.price_uom_factor if item.price_uom_factor and item.price_uom_factor > 0 else Decimal("1")
        unit_cost_reporting = (raw_price / (item.price_unit * price_uom_factor)) * fx_rate * price_scale
        line_cost = _q4(costing_qty * unit_cost_reporting)

        status: str = "VERIFIED"
        if item.is_manual_estimate or item.price_override is not None:
            status = "ESTIMATED"
            estimated_ingredient_cost += line_cost
        elif item.is_price_expired and item.allow_expired_fallback:
            status = "EXPIRED_FALLBACK"
            expired_count += 1
            estimated_ingredient_cost += line_cost
            line_warnings.append("Using expired price under tenant fallback policy.")
        else:
            verified_ingredient_cost += line_cost

        total_ingredient_cost += line_cost
        costed_lines.append(
            CostedIngredientLine(
                temp_ref=item.temp_ref,
                description=item.description,
                material_code=item.material_code,
                recipe_qty=_q4(item.recipe_qty),
                recipe_uom=item.recipe_uom,
                raw_qty=_q4(raw_qty),
                costing_qty=_q4(costing_qty),
                costing_uom=costing_uom,
                effective_unit_cost=_q4(unit_cost_reporting),
                currency=req.reporting_currency.upper(),
                ingredient_cost=line_cost,
                price_source=item.price_source or ("MANUAL_OVERRIDE" if item.price_override is not None else "MATERIAL_MASTER"),
                price_status=status,  # type: ignore[arg-type]
                warnings=line_warnings,
            )
        )

    # Populate percentage of total ingredient cost
    if total_ingredient_cost > 0:
        for line in costed_lines:
            if line.ingredient_cost is not None:
                line.percentage_of_total = _q2((line.ingredient_cost / total_ingredient_cost) * HUNDRED)

    # Additional cost components (§29)
    direct_operating_cost = Decimal("0")
    allocated_overhead_cost = Decimal("0")
    for comp in req.additional_costs:
        if comp.basis == "FIXED_BATCH":
            val = comp.amount
        elif comp.basis == "PER_PORTION":
            val = comp.amount * req.servings
        elif comp.basis == "PCT_OF_INGREDIENT":
            val = total_ingredient_cost * (comp.amount / HUNDRED)
        else:  # HOURLY
            val = comp.amount * (comp.hours if comp.hours is not None else Decimal("1"))

        if comp.category == "ALLOCATED_OVERHEAD":
            allocated_overhead_cost += val
        else:
            direct_operating_cost += val

    direct_operating_cost = _q4(direct_operating_cost)
    allocated_overhead_cost = _q4(allocated_overhead_cost)
    total_ingredient_cost = _q4(total_ingredient_cost)
    total_batch_cost = _q4(total_ingredient_cost + direct_operating_cost + allocated_overhead_cost)

    ing_per_portion = _q4(total_ingredient_cost / req.servings)
    total_per_portion = _q4(total_batch_cost / req.servings)

    # Tax & Selling Price normalization (§23, §30–§32)
    selling_net: Optional[Decimal] = None
    selling_gross: Optional[Decimal] = None
    estimated_revenue: Optional[Decimal] = None
    food_cost_pct: Optional[Decimal] = None
    target_variance_pct: Optional[Decimal] = None
    gross_profit: Optional[Decimal] = None
    gross_margin_pct: Optional[Decimal] = None
    markup_pct: Optional[Decimal] = None
    contribution_amt: Optional[Decimal] = None
    contribution_margin_pct: Optional[Decimal] = None
    est_net_profit: Optional[Decimal] = None

    tax_factor = Decimal("1") + (req.tax_rate_percent / HUNDRED)
    if req.selling_price_per_portion is not None and req.selling_price_per_portion > 0:
        if req.tax_basis == "TAX_INCLUSIVE" and req.tax_rate_percent > 0:
            selling_gross = _q4(req.selling_price_per_portion)
            selling_net = _q4(req.selling_price_per_portion / tax_factor)
        else:
            selling_net = _q4(req.selling_price_per_portion)
            selling_gross = _q4(req.selling_price_per_portion * tax_factor)

        estimated_revenue = _q4(selling_net * req.servings)
        food_cost_pct = _q2((ing_per_portion / selling_net) * HUNDRED)
        target_variance_pct = _q2(food_cost_pct - req.target_food_cost_percent)
        gross_profit = _q4(estimated_revenue - total_ingredient_cost)
        if estimated_revenue > 0:
            gross_margin_pct = _q2((gross_profit / estimated_revenue) * HUNDRED)
        if ing_per_portion > 0:
            markup_pct = _q2(((selling_net - ing_per_portion) / ing_per_portion) * HUNDRED)
        contribution_amt = _q4(estimated_revenue - total_ingredient_cost - direct_operating_cost)
        if estimated_revenue > 0:
            contribution_margin_pct = _q2((contribution_amt / estimated_revenue) * HUNDRED)
        est_net_profit = _q4(contribution_amt - allocated_overhead_cost)
    elif req.selling_price_per_portion == 0:
        warnings.append("Selling price is 0; food cost % and margin ratios omitted to prevent division by zero.")

    recommended_selling_net = _q4(ing_per_portion / (req.target_food_cost_percent / HUNDRED)) if ing_per_portion > 0 else Decimal("0")
    recommended_by_margin: Optional[Decimal] = None
    if req.target_gross_margin_percent is not None and ing_per_portion > 0:
        margin_denom = Decimal("1") - (req.target_gross_margin_percent / HUNDRED)
        if margin_denom > 0:
            recommended_by_margin = _q4(ing_per_portion / margin_denom)

    completeness: str = "COMPLETE"
    if len(req.ingredients) == 0 or (missing_count == len(req.ingredients) and total_ingredient_cost == 0):
        completeness = "MISSING"
    elif missing_count > 0 or currency_mismatch_count > 0 or uom_conflict_count > 0 or circular_detected:
        completeness = "PARTIAL"
        warnings.append("Recipe costing is partial due to unresolved prices, currency rates, or UOM conversions.")

    if estimated_ingredient_cost > 0:
        warnings.append("Includes estimated/override ingredient costs; distinguish from verified ERP/contract costs.")

    return RecipeCostingResponse(
        recipe_id_or_ref=req.recipe_id_or_ref,
        reporting_currency=req.reporting_currency.upper(),
        tax_basis=req.tax_basis,
        servings=req.servings,
        total_ingredient_cost=total_ingredient_cost,
        verified_ingredient_cost=_q4(verified_ingredient_cost),
        estimated_ingredient_cost=_q4(estimated_ingredient_cost),
        sub_recipe_cost=_q4(sub_recipe_cost),
        direct_operating_cost=direct_operating_cost,
        allocated_overhead_cost=allocated_overhead_cost,
        total_batch_cost=total_batch_cost,
        ingredient_cost_per_portion=ing_per_portion,
        total_cost_per_portion=total_per_portion,
        selling_price_net=selling_net,
        selling_price_gross=selling_gross,
        estimated_revenue=estimated_revenue,
        food_cost_percent=food_cost_pct,
        target_food_cost_percent=req.target_food_cost_percent,
        target_variance_percent=target_variance_pct,
        gross_profit=gross_profit,
        gross_margin_percent=gross_margin_pct,
        markup_percent=markup_pct,
        contribution_amount=contribution_amt,
        contribution_margin_percent=contribution_margin_pct,
        estimated_net_profit_after_overhead=est_net_profit,
        recommended_selling_price_net=recommended_selling_net,
        recommended_selling_price_by_margin=recommended_by_margin,
        cost_completeness=completeness,  # type: ignore[arg-type]
        missing_price_count=missing_count,
        expired_price_count=expired_count,
        currency_mismatch_count=currency_mismatch_count,
        uom_conflict_count=uom_conflict_count,
        circular_dependency_detected=circular_detected,
        warnings=warnings,
        lines=costed_lines,
    )


def optimize_recipe_costing(req: CostOptimizationRequest) -> CostOptimizationResponse:
    """
    Deterministic cost optimization recommender (§35):
    Identifies top cost drivers, buffer trim opportunities, and portion/yield optimizations,
    recalculating every suggestion through the verified Decimal costing engine.
    """
    baseline = calculate_recipe_costing(req.costing_request)
    suggestions: list[CostOptimizationSuggestion] = []

    ranked_lines = sorted(
        [ln for ln in baseline.lines if (ln.ingredient_cost or Decimal("0")) > 0],
        key=lambda x: x.ingredient_cost or Decimal("0"),
        reverse=True,
    )

    if ranked_lines:
        top = ranked_lines[0]
        top_cost = top.ingredient_cost or Decimal("0")
        # Evaluate 10% quantity/yield trim on the top cost driver
        sim_req = req.costing_request.model_copy(deep=True)
        for ing in sim_req.ingredients:
            if ing.temp_ref == top.temp_ref:
                ing.recipe_qty = _q4(ing.recipe_qty * Decimal("0.90"))
        sim_res = calculate_recipe_costing(sim_req)
        savings = _q4(baseline.total_ingredient_cost - sim_res.total_ingredient_cost)
        attrs = extract_ingredient_attributes(top.description)
        cat_upper = (attrs.product_category or "").upper()
        allergen_note = (
            "Review allergen/dietary impact and chef flavor balance before reducing primary protein/dairy ingredient."
            if cat_upper in ("PROTEIN", "MEAT", "DAIRY", "SEAFOOD")
            else "Requires chef verification for flavor/texture parity."
        )
        suggestions.append(
            CostOptimizationSuggestion(
                suggestion_id="OPT-TOP-DRIVER-10PCT",
                category="TOP_COST_DRIVER",
                title=f"Reduce {top.description} by 10% or review alternate approved grade",
                rationale=(
                    f"{top.description} accounts for {top.percentage_of_total or Decimal('0')}% "
                    f"({baseline.reporting_currency} {top_cost}) of ingredient cost."
                ),
                target_temp_ref=top.temp_ref,
                projected_ingredient_cost=sim_res.total_ingredient_cost,
                projected_cost_savings=savings,
                projected_food_cost_percent=sim_res.food_cost_percent,
                requires_chef_confirmation=True,
                allergen_or_safety_warning=allergen_note,
            )
        )

    # Check if any buffer/wastage exceeds 0
    if req.costing_request.global_buffer_percent > 0 or any(i.production_buffer_percent > 0 for i in req.costing_request.ingredients):
        sim_buf = req.costing_request.model_copy(deep=True)
        sim_buf.global_buffer_percent = Decimal("0")
        for ing in sim_buf.ingredients:
            ing.production_buffer_percent = Decimal("0")
        sim_res = calculate_recipe_costing(sim_buf)
        savings = _q4(baseline.total_ingredient_cost - sim_res.total_ingredient_cost)
        if savings > 0:
            suggestions.append(
                CostOptimizationSuggestion(
                    suggestion_id="OPT-BUFFER-ZERO",
                    category="BUFFER_OPTIMIZATION",
                    title="Eliminate excess production buffer via exact batch scaling",
                    rationale="Trimming unallocated safety buffers reduces batch cost without changing plated recipe yield.",
                    projected_ingredient_cost=sim_res.total_ingredient_cost,
                    projected_cost_savings=savings,
                    projected_food_cost_percent=sim_res.food_cost_percent,
                    requires_chef_confirmation=True,
                )
            )

    # Portion / serving target option (+5% yield portions from same batch)
    sim_srv = req.costing_request.model_copy(deep=True)
    sim_srv.servings = _q4(req.costing_request.servings * Decimal("1.05"))
    sim_srv_res = calculate_recipe_costing(sim_srv)
    suggestions.append(
        CostOptimizationSuggestion(
            suggestion_id="OPT-PORTION-YIELD-5PCT",
            category="PORTION_OR_YIELD",
            title="Optimize portioning yield (+5% portions per batch)",
            rationale=(
                f"Lowers ingredient cost per portion from {baseline.reporting_currency} {baseline.ingredient_cost_per_portion} "
                f"to {baseline.reporting_currency} {sim_srv_res.ingredient_cost_per_portion}."
            ),
            projected_ingredient_cost=sim_srv_res.total_ingredient_cost,
            projected_cost_savings=_q4((baseline.ingredient_cost_per_portion - sim_srv_res.ingredient_cost_per_portion) * baseline.servings),
            projected_food_cost_percent=sim_srv_res.food_cost_percent,
            requires_chef_confirmation=True,
            allergen_or_safety_warning="Verify portion weight meets menu specification before applying.",
        )
    )

    return CostOptimizationResponse(
        baseline_ingredient_cost=baseline.total_ingredient_cost,
        baseline_food_cost_percent=baseline.food_cost_percent,
        goal=req.goal,
        provider_used="LOCAL_DETERMINISTIC",
        suggestions=suggestions,
    )
