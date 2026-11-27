from __future__ import annotations

from typing import Literal
from pydantic import BaseModel, Field


class MaterialReconLineInput(BaseModel):
    material_code: str
    material_name: str = ""
    inventory_batch: str = "DEFAULT"
    uom: str = "KG"
    planned_qty: float = 0.0
    reserved_qty: float = 0.0
    picked_qty: float = 0.0
    original_issued_qty: float = 0.0
    additional_issued_qty: float = 0.0
    reversed_issue_qty: float = 0.0
    net_inbound_transfer_qty: float = 0.0
    net_outbound_transfer_qty: float = 0.0
    usable_consumption_qty: float = 0.0
    consumption_includes_waste: bool = False
    separate_ingredient_waste_qty: float = 0.0
    unused_returned_qty: float = 0.0
    remaining_wip_qty: float = 0.0
    standard_unit_price: float = 0.0
    actual_unit_price: float = 0.0
    standard_qty_for_actual_output: float | None = None
    tolerance_percent: float = 2.0
    pending_return_qty: float = 0.0
    pending_waste_approval_qty: float = 0.0
    duplicate_posting_suspected: bool = False
    pending_erp_confirmation: bool = False


class MaterialReconLineResult(BaseModel):
    material_code: str
    material_name: str
    inventory_batch: str
    uom: str
    net_issued_qty: float
    accounted_qty: float
    effective_waste_qty: float
    unexplained_qty: float
    unexplained_percent: float
    tolerance_percent: float
    within_tolerance: bool
    settlement_blocked: bool
    status: str
    planning_volume_variance_qty: float
    usage_variance_qty: float
    usage_cost_variance: float
    price_cost_variance: float
    total_direct_material_variance: float


class MaterialReconciliationRequest(BaseModel):
    production_order_code: str = ""
    production_batch_number: str = ""
    lines: list[MaterialReconLineInput] = Field(default_factory=list)


class MaterialReconciliationResponse(BaseModel):
    production_order_code: str
    production_batch_number: str
    overall_status: str
    settlement_blocked: bool
    total_net_issued_qty: float
    total_accounted_qty: float
    total_unexplained_qty: float
    total_material_cost_variance: float
    lines: list[MaterialReconLineResult] = Field(default_factory=list)


class YieldVarianceRequest(BaseModel):
    recipe_code: str
    recipe_name: str = ""
    gross_input_qty: float
    input_uom: str = "KG"
    accepted_usable_output_qty: float
    rejected_output_qty: float = 0.0
    output_uom: str = "KG"
    standard_yield_percent: float = 85.0
    unit_cost: float = 0.0


class YieldVarianceResponse(BaseModel):
    recipe_code: str
    comparable_uom: bool
    uom_message: str
    actual_yield_percent: float | None
    standard_yield_percent: float
    yield_variance_percent: float | None
    expected_output_qty: float
    output_variance_qty: float
    estimated_financial_impact: float
    allows_over_100_percent: bool = True
    root_cause_hints: list[str] = Field(default_factory=list)


class PortionSampleRequest(BaseModel):
    recipe_code: str
    standard_portion_weight_g: float
    sampled_weights_g: list[float] = Field(default_factory=list)
    planned_portions: float = 0.0
    actual_portions: float = 0.0
    total_usable_output_g: float = 0.0
    standard_portion_cost: float = 0.0


class PortionSampleResponse(BaseModel):
    recipe_code: str
    sample_count: int
    sampling_basis_note: str
    standard_portion_weight_g: float
    average_actual_weight_g: float
    portion_weight_variance_g: float
    over_portioning_percent: float
    under_portioning_percent: float
    expected_portions_from_actual_output: float
    actual_portions: float
    estimated_portion_cost_variance: float
    recommendations: list[str] = Field(default_factory=list)


class CostVarianceBridgeRequest(BaseModel):
    planned_cost: float
    actual_cost: float
    planning_volume_variance: float = 0.0
    ingredient_usage_variance: float = 0.0
    ingredient_price_variance: float = 0.0
    substitution_impact: float = 0.0
    recipe_mix_variance: float = 0.0
    labor_rate_variance: float = 0.0
    labor_efficiency_variance: float = 0.0
    equipment_cost_variance: float = 0.0
    overhead_variance: float = 0.0
    explanatory_yield_impact: float = 0.0
    explanatory_portion_impact: float = 0.0
    explanatory_waste_impact: float = 0.0


class CostVarianceBridgeStep(BaseModel):
    component: str
    amount: float
    category: Literal["ADDITIVE", "EXPLANATORY"]


class CostVarianceBridgeResponse(BaseModel):
    planned_cost: float
    actual_cost: float
    total_cost_variance: float
    unallocated_residual: float
    reconciled_exactly: bool
    additive_steps: list[CostVarianceBridgeStep] = Field(default_factory=list)
    explanatory_drivers: list[CostVarianceBridgeStep] = Field(default_factory=list)


class EventProfitBridgeRequest(BaseModel):
    baseline_profit: float
    actual_profit: float
    guest_count_impact: float = 0.0
    selling_price_impact: float = 0.0
    discount_impact: float = 0.0
    menu_mix_impact: float = 0.0
    ingredient_price_impact: float = 0.0
    ingredient_usage_impact: float = 0.0
    additional_labor_impact: float = 0.0
    external_expense_impact: float = 0.0
    other_cost_impact: float = 0.0
    explanatory_waste_driver: float = 0.0
    explanatory_yield_driver: float = 0.0


class EventProfitBridgeResponse(BaseModel):
    baseline_profit: float
    actual_profit: float
    total_profit_delta: float
    residual_other_impact: float
    reconciled_exactly: bool
    steps: list[CostVarianceBridgeStep] = Field(default_factory=list)
    explanatory_drivers: list[CostVarianceBridgeStep] = Field(default_factory=list)


class VarianceInvestigationItem(BaseModel):
    reference_code: str
    category: str
    metric_name: str
    expected_value: float
    actual_value: float
    historical_baseline: float | None = None
    financial_impact: float = 0.0
    data_quality: str = "VERIFIED"


class VarianceInvestigationRequest(BaseModel):
    property_name: str = "All Properties"
    currency: str = "AED"
    items: list[VarianceInvestigationItem] = Field(default_factory=list)


class VarianceInvestigationFinding(BaseModel):
    reference_code: str
    category: str
    identified_variance: str
    supporting_evidence: str
    historical_comparison: str
    possible_contributing_factors: list[str]
    estimated_financial_impact: float
    data_quality: str
    confidence_percent: float
    recommended_investigation: str
    suggested_responsible_role: str


class VarianceInvestigationResponse(BaseModel):
    property_name: str
    currency: str
    findings: list[VarianceInvestigationFinding] = Field(default_factory=list)
    summary: str = ""


class ReconWorkbookSheet(BaseModel):
    sheet_name: str
    columns: list[str]
    rows: list[list[str | int | float | None]] = Field(default_factory=list)


class ReconWorkbookExportRequest(BaseModel):
    title: str = "SILA Phase 4B Production Reconciliation & Event Closure Reports"
    currency: str = "AED"
    sheets: list[ReconWorkbookSheet] = Field(default_factory=list)
