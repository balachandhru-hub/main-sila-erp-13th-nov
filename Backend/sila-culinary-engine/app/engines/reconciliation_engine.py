from __future__ import annotations

import io
from typing import Any

from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill

from app.schemas.reconciliation_engine import (
    CostVarianceBridgeRequest,
    CostVarianceBridgeResponse,
    CostVarianceBridgeStep,
    EventProfitBridgeRequest,
    EventProfitBridgeResponse,
    MaterialReconciliationRequest,
    MaterialReconciliationResponse,
    MaterialReconLineResult,
    PortionSampleRequest,
    PortionSampleResponse,
    ReconWorkbookExportRequest,
    VarianceInvestigationFinding,
    VarianceInvestigationRequest,
    VarianceInvestigationResponse,
    YieldVarianceRequest,
    YieldVarianceResponse,
)

_MASS_UOMS: dict[str, float] = {
    "KG": 1000.0,
    "G": 1.0,
    "GM": 1.0,
    "GRAM": 1.0,
    "GRAMS": 1.0,
    "MG": 0.001,
    "LB": 453.59237,
    "OZ": 28.349523125,
}
_VOL_UOMS: dict[str, float] = {
    "L": 1000.0,
    "LTR": 1000.0,
    "LITER": 1000.0,
    "LITRE": 1000.0,
    "ML": 1.0,
}


def _safe_excel_cell(val: Any) -> Any:
    if isinstance(val, str) and val and val[0] in ("=", "+", "-", "@", "\t", "\r"):
        return f"'{val}"
    return val


def _uom_to_base(qty: float, uom: str) -> tuple[str, float] | None:
    u = (uom or "").strip().upper()
    if u in _MASS_UOMS:
        return ("MASS_G", qty * _MASS_UOMS[u])
    if u in _VOL_UOMS:
        return ("VOL_ML", qty * _VOL_UOMS[u])
    if u in {"PORTION", "PORTIONS", "EA", "EACH", "PC", "PCS", "UNIT"}:
        return ("COUNT", qty)
    return None


def compute_material_reconciliation(req: MaterialReconciliationRequest) -> MaterialReconciliationResponse:
    results: list[MaterialReconLineResult] = []
    any_blocked = False
    has_pending = False
    has_mismatch = False
    total_net_issued = 0.0
    total_accounted = 0.0
    total_unexplained = 0.0
    total_cost_var = 0.0

    for line in req.lines:
        net_issued = round(line.original_issued_qty + line.additional_issued_qty - line.reversed_issue_qty, 4)
        # Prevent double counting waste when recorded consumption already includes ingredient waste
        effective_waste = 0.0 if line.consumption_includes_waste else line.separate_ingredient_waste_qty
        accounted = round(
            line.usable_consumption_qty
            + effective_waste
            + line.unused_returned_qty
            + line.net_outbound_transfer_qty
            + line.remaining_wip_qty,
            4,
        )
        unexplained = round(net_issued + line.net_inbound_transfer_qty - accounted, 4)
        denom = max(abs(net_issued + line.net_inbound_transfer_qty), 1e-6)
        unexplained_pct = round(abs(unexplained) / denom * 100.0, 4) if (net_issued + line.net_inbound_transfer_qty) != 0 else (100.0 if unexplained != 0 else 0.0)
        within_tol = unexplained_pct <= line.tolerance_percent or abs(unexplained) <= 1e-6

        if line.duplicate_posting_suspected:
            status = "DUPLICATE_TRANSACTION_SUSPECTED"
            blocked = True
        elif line.pending_erp_confirmation:
            status = "PENDING_ERP_CONFIRMATION"
            blocked = True
            has_pending = True
        elif line.pending_waste_approval_qty > 0:
            status = "PENDING_WASTE_APPROVAL"
            blocked = True
            has_pending = True
        elif line.pending_return_qty > 0:
            status = "PENDING_RETURN"
            blocked = True
            has_pending = True
        elif abs(unexplained) <= 1e-6:
            status = "BALANCED"
            blocked = False
        elif within_tol:
            status = "WITHIN_TOLERANCE"
            blocked = False
        else:
            status = "QUANTITY_MISMATCH"
            blocked = True
            has_mismatch = True

        std_for_actual = line.standard_qty_for_actual_output if line.standard_qty_for_actual_output is not None else line.planned_qty
        planning_vol_var = round(std_for_actual - line.planned_qty, 4)
        actual_consumed_for_var = round(line.usable_consumption_qty + effective_waste, 4)
        usage_var_qty = round(actual_consumed_for_var - std_for_actual, 4)
        usage_cost_var = round(usage_var_qty * line.standard_unit_price, 2)
        price_cost_var = round(actual_consumed_for_var * (line.actual_unit_price - line.standard_unit_price), 2)
        total_mat_var = round(usage_cost_var + price_cost_var, 2)

        if blocked:
            any_blocked = True

        total_net_issued += net_issued
        total_accounted += accounted
        total_unexplained += unexplained
        total_cost_var += total_mat_var

        results.append(
            MaterialReconLineResult(
                material_code=line.material_code,
                material_name=line.material_name,
                inventory_batch=line.inventory_batch,
                uom=line.uom,
                net_issued_qty=net_issued,
                accounted_qty=accounted,
                effective_waste_qty=round(effective_waste, 4),
                unexplained_qty=unexplained,
                unexplained_percent=unexplained_pct,
                tolerance_percent=line.tolerance_percent,
                within_tolerance=within_tol,
                settlement_blocked=blocked,
                status=status,
                planning_volume_variance_qty=planning_vol_var,
                usage_variance_qty=usage_var_qty,
                usage_cost_variance=usage_cost_var,
                price_cost_variance=price_cost_var,
                total_direct_material_variance=total_mat_var,
            )
        )

    if any_blocked and has_mismatch:
        overall_status = "REQUIRES_INVESTIGATION"
    elif any_blocked and has_pending:
        overall_status = "PENDING_CONFIRMATION"
    elif any_blocked:
        overall_status = "QUANTITY_MISMATCH"
    elif any(r.status == "WITHIN_TOLERANCE" for r in results):
        overall_status = "WITHIN_TOLERANCE"
    else:
        overall_status = "BALANCED"

    return MaterialReconciliationResponse(
        production_order_code=req.production_order_code,
        production_batch_number=req.production_batch_number,
        overall_status=overall_status,
        settlement_blocked=any_blocked,
        total_net_issued_qty=round(total_net_issued, 4),
        total_accounted_qty=round(total_accounted, 4),
        total_unexplained_qty=round(total_unexplained, 4),
        total_material_cost_variance=round(total_cost_var, 2),
        lines=results,
    )


def compute_yield_variance(req: YieldVarianceRequest) -> YieldVarianceResponse:
    in_base = _uom_to_base(req.gross_input_qty, req.input_uom)
    out_base = _uom_to_base(req.accepted_usable_output_qty, req.output_uom)

    if in_base is None or out_base is None or in_base[0] != out_base[0]:
        return YieldVarianceResponse(
            recipe_code=req.recipe_code,
            comparable_uom=False,
            uom_message=f"Incompatible UOMs ({req.input_uom} vs {req.output_uom}); mass/volume yield % cannot be compared directly.",
            actual_yield_percent=None,
            standard_yield_percent=req.standard_yield_percent,
            yield_variance_percent=None,
            expected_output_qty=round(req.gross_input_qty * (req.standard_yield_percent / 100.0), 4),
            output_variance_qty=0.0,
            estimated_financial_impact=0.0,
            root_cause_hints=["Verify UOM conversion factor or portion weight specification before computing mass yield."],
        )

    gross_base_qty = in_base[1]
    accepted_base_qty = out_base[1]
    actual_yield_pct = round((accepted_base_qty / gross_base_qty) * 100.0, 2) if gross_base_qty > 0 else 0.0
    yield_var_pct = round(actual_yield_pct - req.standard_yield_percent, 2)

    # Express expected output in output_uom
    scale = (accepted_base_qty / req.accepted_usable_output_qty) if req.accepted_usable_output_qty else 1.0
    expected_out_in_output_uom = round((gross_base_qty * (req.standard_yield_percent / 100.0)) / scale, 4) if scale else 0.0
    output_var = round(req.accepted_usable_output_qty - expected_out_in_output_uom, 4)
    fin_impact = round(output_var * req.unit_cost, 2)

    hints: list[str] = []
    if yield_var_pct < -3.0:
        hints.append("Actual yield below standard; review trimming loss, cooking shrinkage, or rejected output.")
    elif actual_yield_pct > 100.0:
        hints.append("Yield exceeds 100% (expected for hydration/moisture-gain recipes such as grains, pasta, or reductions).")
    if req.rejected_output_qty > 0:
        hints.append(f"{req.rejected_output_qty:g} {req.output_uom} rejected output recorded; inspect quality checkpoint logs.")

    return YieldVarianceResponse(
        recipe_code=req.recipe_code,
        comparable_uom=True,
        uom_message="UOMs compatible; mass/volume yield computed accurately.",
        actual_yield_percent=actual_yield_pct,
        standard_yield_percent=req.standard_yield_percent,
        yield_variance_percent=yield_var_pct,
        expected_output_qty=expected_out_in_output_uom,
        output_variance_qty=output_var,
        estimated_financial_impact=fin_impact,
        root_cause_hints=hints or ["Yield within expected recipe tolerance."],
    )


def compute_portion_variance(req: PortionSampleRequest) -> PortionSampleResponse:
    samples = [w for w in req.sampled_weights_g if w > 0]
    count = len(samples)
    avg_weight = round(sum(samples) / count, 2) if count > 0 else req.standard_portion_weight_g
    std_w = max(req.standard_portion_weight_g, 0.0001)
    weight_var = round(avg_weight - std_w, 2)
    pct_diff = round((weight_var / std_w) * 100.0, 2)
    over_pct = pct_diff if pct_diff > 0 else 0.0
    under_pct = abs(pct_diff) if pct_diff < 0 else 0.0

    expected_portions = round(req.total_usable_output_g / std_w, 2) if req.total_usable_output_g > 0 else req.planned_portions
    portion_delta = round(req.actual_portions - expected_portions, 2) if req.actual_portions > 0 else 0.0
    est_cost_var = round(((avg_weight - std_w) / std_w) * req.standard_portion_cost * max(req.actual_portions, expected_portions), 2)

    coverage_ratio = (count / req.actual_portions * 100.0) if req.actual_portions > 0 else 0.0
    basis_note = (
        f"Based on {count} sampled portions ({coverage_ratio:.1f}% of {req.actual_portions:g} actual portions). "
        "Sampled portions indicate observed plating tendency and may not represent every unmeasured portion."
        if count > 0
        else "No physical portion samples recorded; defaulting to standard portion weight."
    )

    recs: list[str] = []
    if over_pct > 5.0:
        recs.append(f"Over-portioning of +{over_pct:.1f}% detected; calibrate ladles/scales at plating station.")
    elif under_pct > 5.0:
        recs.append(f"Under-portioning of -{under_pct:.1f}% detected; verify guest spec compliance and portioning tools.")
    if count < 5 and req.actual_portions >= 50:
        recs.append("Increase sample size (minimum 5–10 portions per batch) for stronger statistical confidence.")

    return PortionSampleResponse(
        recipe_code=req.recipe_code,
        sample_count=count,
        sampling_basis_note=basis_note,
        standard_portion_weight_g=round(std_w, 2),
        average_actual_weight_g=avg_weight,
        portion_weight_variance_g=weight_var,
        over_portioning_percent=over_pct,
        under_portioning_percent=under_pct,
        expected_portions_from_actual_output=expected_portions,
        actual_portions=req.actual_portions,
        estimated_portion_cost_variance=est_cost_var,
        recommendations=recs or ["Portion control within target specification."],
    )


def compute_cost_variance_bridge(req: CostVarianceBridgeRequest) -> CostVarianceBridgeResponse:
    total_var = round(req.actual_cost - req.planned_cost, 2)
    additive = [
        CostVarianceBridgeStep(component="Planning Volume Variance", amount=round(req.planning_volume_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Ingredient Usage Variance", amount=round(req.ingredient_usage_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Ingredient Price Variance", amount=round(req.ingredient_price_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Ingredient Substitution Impact", amount=round(req.substitution_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Recipe Mix Variance", amount=round(req.recipe_mix_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Labor Rate Variance", amount=round(req.labor_rate_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Labor Efficiency Variance", amount=round(req.labor_efficiency_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Equipment Cost Variance", amount=round(req.equipment_cost_variance, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Overhead Allocation Variance", amount=round(req.overhead_variance, 2), category="ADDITIVE"),
    ]
    additive_sum = round(sum(s.amount for s in additive), 2)
    residual = round(total_var - additive_sum, 2)
    if abs(residual) > 0.005:
        additive.append(CostVarianceBridgeStep(component="Unallocated / Rounding Variance", amount=residual, category="ADDITIVE"))

    explanatory = [
        CostVarianceBridgeStep(component="Yield Variance (Explanatory - Included in Usage)", amount=round(req.explanatory_yield_impact, 2), category="EXPLANATORY"),
        CostVarianceBridgeStep(component="Portion Variance (Explanatory - Included in Usage)", amount=round(req.explanatory_portion_impact, 2), category="EXPLANATORY"),
        CostVarianceBridgeStep(component="Recorded Waste (Explanatory - Included in Material Cost)", amount=round(req.explanatory_waste_impact, 2), category="EXPLANATORY"),
    ]
    reconciled = abs(round(sum(s.amount for s in additive), 2) - total_var) <= 0.01

    return CostVarianceBridgeResponse(
        planned_cost=round(req.planned_cost, 2),
        actual_cost=round(req.actual_cost, 2),
        total_cost_variance=total_var,
        unallocated_residual=residual,
        reconciled_exactly=reconciled,
        additive_steps=additive,
        explanatory_drivers=explanatory,
    )


def compute_event_profit_bridge(req: EventProfitBridgeRequest) -> EventProfitBridgeResponse:
    total_delta = round(req.actual_profit - req.baseline_profit, 2)
    steps = [
        CostVarianceBridgeStep(component="Guest Count Impact", amount=round(req.guest_count_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Selling Price Impact", amount=round(req.selling_price_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Discount & Credit Impact", amount=round(req.discount_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Menu Mix Impact", amount=round(req.menu_mix_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Ingredient Price Impact", amount=round(req.ingredient_price_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Ingredient Usage Impact", amount=round(req.ingredient_usage_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Additional Labor Impact", amount=round(req.additional_labor_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="External Expense Impact", amount=round(req.external_expense_impact, 2), category="ADDITIVE"),
        CostVarianceBridgeStep(component="Other Direct / Overhead Impact", amount=round(req.other_cost_impact, 2), category="ADDITIVE"),
    ]
    residual = round(total_delta - sum(s.amount for s in steps), 2)
    if abs(residual) > 0.005:
        steps.append(CostVarianceBridgeStep(component="Residual Variance", amount=residual, category="ADDITIVE"))

    explanatory = [
        CostVarianceBridgeStep(component="Food Waste Driver (Explanatory)", amount=round(req.explanatory_waste_driver, 2), category="EXPLANATORY"),
        CostVarianceBridgeStep(component="Yield Driver (Explanatory)", amount=round(req.explanatory_yield_driver, 2), category="EXPLANATORY"),
    ]
    return EventProfitBridgeResponse(
        baseline_profit=round(req.baseline_profit, 2),
        actual_profit=round(req.actual_profit, 2),
        total_profit_delta=total_delta,
        residual_other_impact=residual,
        reconciled_exactly=abs(round(sum(s.amount for s in steps), 2) - total_delta) <= 0.01,
        steps=steps,
        explanatory_drivers=explanatory,
    )


def investigate_variances(req: VarianceInvestigationRequest) -> VarianceInvestigationResponse:
    findings: list[VarianceInvestigationFinding] = []
    for item in req.items:
        delta = round(item.actual_value - item.expected_value, 4)
        pct = round((delta / item.expected_value) * 100.0, 2) if abs(item.expected_value) > 1e-6 else 0.0
        if abs(pct) < 3.0 and abs(item.financial_impact) < 100.0:
            continue

        cat = item.category.upper()
        if "PORTION" in cat:
            factors = ["Plating utensil sizing or manual scooping drift", "Unupdated recipe standard weight"]
            role = "CHEF_DE_PARTIE"
            action = "Audit plating scale calibration and run 10-portion spot verification."
        elif "YIELD" in cat:
            factors = ["Trimming loss variation or raw material grade variance", "Cooking duration/temperature variance"]
            role = "EXECUTIVE_CHEF"
            action = "Review supplier batch spec and cooking equipment calibration log."
        elif "WASTE" in cat:
            factors = ["Over-production relative to actual covers", "Storage/cold-chain handling or prep spoilage"]
            role = "EXECUTIVE_CHEF"
            action = "Review batch sizing against confirmed covers and verify FEFO store rotation."
        elif "RECON" in cat or "INVENTORY" in cat:
            factors = ["Unposted material return or pending issue reversal", "UOM conversion mismatch between store issue and kitchen log"]
            role = "STOREKEEPER"
            action = "Reconcile physical store bin card against posted GI/return/transfer transactions."
        else:
            factors = ["Supplier unit cost change or unplanned additional issue", "Event guest count or menu mix shift"]
            role = "COST_CONTROLLER"
            action = "Inspect supporting GRN valuation, additional material requests, and batch settlement checklist."

        hist_comp = (
            f"Historical baseline {item.historical_baseline:g} vs actual {item.actual_value:g}"
            if item.historical_baseline is not None
            else "Insufficient historical cycles for multi-period comparison"
        )
        conf = 88.0 if item.data_quality == "VERIFIED" else 62.0

        findings.append(
            VarianceInvestigationFinding(
                reference_code=item.reference_code,
                category=item.category,
                identified_variance=f"{item.metric_name}: {delta:+g} ({pct:+.1f}% vs expected)",
                supporting_evidence=f"Expected {item.expected_value:g}, Actual {item.actual_value:g}; Quality={item.data_quality}",
                historical_comparison=hist_comp,
                possible_contributing_factors=factors,
                estimated_financial_impact=round(item.financial_impact, 2),
                data_quality=item.data_quality,
                confidence_percent=conf,
                recommended_investigation=action,
                suggested_responsible_role=role,
            )
        )

    summary = (
        f"Identified {len(findings)} material variance(s) requiring operational or cost review for {req.property_name}."
        if findings
        else f"No abnormal variance patterns detected above thresholds for {req.property_name}."
    )
    return VarianceInvestigationResponse(
        property_name=req.property_name,
        currency=req.currency,
        findings=findings,
        summary=summary,
    )


def export_reconciliation_workbook(req: ReconWorkbookExportRequest) -> bytes:
    wb = Workbook()
    default_ws = wb.active
    wb.remove(default_ws)

    header_fill = PatternFill(start_color="1E293B", end_color="1E293B", fill_type="solid")
    header_font = Font(color="FFFFFF", bold=True)

    sheets = req.sheets or []
    if not sheets:
        ws = wb.create_sheet(title="Summary")
        ws.append([_safe_excel_cell(req.title), _safe_excel_cell(req.currency)])
    else:
        for sheet in sheets:
            safe_name = (sheet.sheet_name or "Report")[:31]
            ws = wb.create_sheet(title=safe_name)
            ws.append([_safe_excel_cell(c) for c in sheet.columns])
            for cell in ws[1]:
                cell.fill = header_fill
                cell.font = header_font
            for row in sheet.rows:
                ws.append([_safe_excel_cell(v) for v in row])

    buf = io.BytesIO()
    wb.save(buf)
    return buf.getvalue()
