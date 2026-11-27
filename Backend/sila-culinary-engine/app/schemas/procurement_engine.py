from __future__ import annotations

from datetime import datetime
from typing import Any, Literal, Optional
from pydantic import BaseModel, Field


class ProcurementDemandLineInput(BaseModel):
    line_id: str
    event_id: str
    event_code: str = ""
    event_name: str = ""
    event_revision: int = 1
    mrp_run_id: str = ""
    mrp_code: str = ""
    mrp_version: int = 1
    property_id: Optional[str] = None
    company_code: Optional[str] = None
    plant_code: Optional[str] = None
    kitchen_name: str = "Main Kitchen"
    destination_location_id: Optional[str] = None
    destination_storage_location: str = ""
    material_id: Optional[str] = None
    material_code: Optional[str] = None
    material_description: str
    material_category: str = "GENERAL"
    base_uom: str = "KG"
    gross_requirement_qty: float = Field(ge=0)
    prepared_stock_eligible_qty: float = Field(default=0.0, ge=0)
    required_by_utc: datetime
    priority_rank: int = 100
    is_event_confirmed: bool = True
    is_resolved_material: bool = True
    approved_substitute_codes: list[str] = Field(default_factory=list)
    weekly_bucket_demand_qty: float = Field(default=0.0, ge=0)
    already_linked_to_weekly_bucket: bool = False


class ProcurementInventorySourceInput(BaseModel):
    source_id: str
    source_type: Literal[
        "LOCAL_KITCHEN",
        "LOCAL_OUTLET",
        "CENTRAL_STORE",
        "SUB_STORE",
        "OTHER_KITCHEN",
        "OTHER_OUTLET",
        "PREPARED_STOCK",
    ]
    property_id: Optional[str] = None
    company_code: Optional[str] = None
    plant_code: Optional[str] = None
    location_id: Optional[str] = None
    location_name: str = ""
    storage_location_code: str = ""
    material_id: Optional[str] = None
    material_code: str
    batch_number: Optional[str] = None
    expiry_date_utc: Optional[datetime] = None
    min_remaining_shelf_life_days: int = 0
    on_hand_qty: Optional[float] = None
    existing_reservations_qty: float = Field(default=0.0, ge=0)
    safety_stock_qty: float = Field(default=0.0, ge=0)
    available_already_excludes_reservations: bool = False
    available_already_excludes_safety_stock: bool = False
    blocked_qty: float = Field(default=0.0, ge=0)
    quality_inspection_qty: float = Field(default=0.0, ge=0)
    damaged_qty: float = Field(default=0.0, ge=0)
    quality_status: str = "UNRESTRICTED"
    transfer_enabled: bool = True
    transfer_lead_time_hours: float = Field(default=2.0, ge=0)
    freshness_timestamp_utc: Optional[datetime] = None
    is_stale: bool = False


class ProcurementIncomingPoInput(BaseModel):
    po_id: str
    po_number: str
    po_item_id: str
    line_number: int = 10
    supplier_id: Optional[str] = None
    supplier_code: str = ""
    supplier_name: str = ""
    material_id: Optional[str] = None
    material_code: str
    plant_code: Optional[str] = None
    storage_location_code: str = ""
    ordered_qty: float = Field(ge=0)
    received_qty: float = Field(default=0.0, ge=0)
    reversed_receipt_qty: float = Field(default=0.0, ge=0)
    cancelled_qty: float = Field(default=0.0, ge=0)
    already_allocated_qty: float = Field(default=0.0, ge=0)
    delivery_date_utc: Optional[datetime] = None
    supplier_confirmed: bool = True
    delivery_completed: bool = False
    is_cancelled: bool = False
    po_approval_status: str = "APPROVED"
    unit_price: Optional[float] = None
    currency: str = "AED"


class ProcurementIncomingItoInput(BaseModel):
    ito_id: str
    ito_number: str
    ito_item_id: str
    material_code: str
    from_location_id: Optional[str] = None
    to_location_id: Optional[str] = None
    plant_code: Optional[str] = None
    to_storage_location: str = ""
    approved_qty: float = Field(ge=0)
    received_qty: float = Field(default=0.0, ge=0)
    already_allocated_qty: float = Field(default=0.0, ge=0)
    expected_arrival_utc: Optional[datetime] = None
    status: str = "APPROVED"


class ProcurementSupplierOfferInput(BaseModel):
    supplier_id: str
    supplier_code: str
    supplier_name: str
    material_code: str
    purchasing_org: Optional[str] = None
    company_code: Optional[str] = None
    plant_code: Optional[str] = None
    contract_id: Optional[str] = None
    contract_number: Optional[str] = None
    is_contract_active: bool = False
    is_contract_mandatory: bool = False
    is_approved_supplier: bool = True
    catalog_id: Optional[str] = None
    unit_price: Optional[float] = None
    currency: str = "AED"
    purchasing_uom: str = "KG"
    uom_to_base_factor: float = Field(default=1.0, gt=0)
    pack_size_base_qty: float = Field(default=1.0, gt=0)
    moq_base_qty: float = Field(default=0.0, ge=0)
    lead_time_days: int = Field(default=1, ge=0)
    delivery_weekdays: list[int] = Field(default_factory=lambda: [0, 1, 2, 3, 4, 5, 6])
    receiving_inspection_hours: float = Field(default=2.0, ge=0)
    reliability_score: float = Field(default=0.95, ge=0, le=1.0)
    freight_charge: float = Field(default=0.0, ge=0)


class ProcurementPlanRequest(BaseModel):
    tenant_id: str
    reference_now_utc: Optional[datetime] = None
    coverage_priority: list[str] = Field(
        default_factory=lambda: [
            "LOCAL_STOCK",
            "PREPARED_STOCK",
            "INCOMING_PO",
            "INCOMING_ITO",
            "CENTRAL_STORE_ITO",
            "OTHER_OUTLET_ITO",
            "CONTRACT_SUPPLIER",
            "CATALOG_SUPPLIER",
        ]
    )
    demand_lines: list[ProcurementDemandLineInput] = Field(default_factory=list)
    inventory_sources: list[ProcurementInventorySourceInput] = Field(default_factory=list)
    incoming_pos: list[ProcurementIncomingPoInput] = Field(default_factory=list)
    incoming_itos: list[ProcurementIncomingItoInput] = Field(default_factory=list)
    supplier_offers: list[ProcurementSupplierOfferInput] = Field(default_factory=list)
    include_proposed_itos_in_net: bool = True
    allow_safety_stock_override: bool = False


class SupplyDemandLedgerEntry(BaseModel):
    timestamp_utc: datetime
    entry_type: str
    source_reference: str
    material_code: str
    delta_qty: float
    running_projected_available: float
    timely_for_deadline: bool = True
    notes: str = ""


class ProcurementLineAllocationPlan(BaseModel):
    line_id: str
    event_id: str
    event_code: str
    material_code: Optional[str] = None
    material_description: str
    base_uom: str
    required_by_utc: datetime
    gross_requirement_qty: float
    local_on_hand_qty: Optional[float] = None
    competing_reservations_qty: float = 0.0
    safety_stock_qty: float = 0.0
    eligible_local_stock_qty: Optional[float] = None
    recommended_local_reservation_qty: float = 0.0
    prepared_stock_allocated_qty: float = 0.0
    incoming_po_allocated_qty: float = 0.0
    incoming_ito_allocated_qty: float = 0.0
    proposed_ito_qty: float = 0.0
    confirmed_coverage_qty: float = 0.0
    proposed_total_coverage_qty: float = 0.0
    remaining_confirmed_gap_qty: float = 0.0
    net_purchase_requirement_qty: float = 0.0
    recommended_purchase_qty: float = 0.0
    purchasing_uom: str = "KG"
    pack_size_base_qty: float = 1.0
    pack_count: int = 0
    moq_base_qty: float = 0.0
    expected_excess_qty: float = 0.0
    recommended_supplier_id: Optional[str] = None
    recommended_supplier_code: Optional[str] = None
    recommended_supplier_name: Optional[str] = None
    contract_id: Optional[str] = None
    contract_number: Optional[str] = None
    unit_price: Optional[float] = None
    currency: str = "AED"
    estimated_purchase_value: Optional[float] = None
    expected_delivery_utc: Optional[datetime] = None
    delivery_feasible: bool = True
    coverage_status: str = "REQUIREMENT_VALIDATED"
    fefo_batch_allocations: list[dict[str, Any]] = Field(default_factory=list)
    incoming_po_allocations: list[dict[str, Any]] = Field(default_factory=list)
    ito_recommendations: list[dict[str, Any]] = Field(default_factory=list)
    supplier_comparisons: list[dict[str, Any]] = Field(default_factory=list)
    ledger: list[SupplyDemandLedgerEntry] = Field(default_factory=list)
    exceptions: list[dict[str, Any]] = Field(default_factory=list)


class MultiEventConsolidatedPurchaseItem(BaseModel):
    consolidation_key: str
    material_code: str
    supplier_code: str
    supplier_name: str
    company_code: Optional[str] = None
    plant_code: Optional[str] = None
    purchasing_org: Optional[str] = None
    currency: str = "AED"
    combined_net_qty: float
    pack_size_base_qty: float
    moq_base_qty: float
    recommended_purchase_qty: float
    expected_excess_qty: float
    estimated_total_cost: Optional[float] = None
    earliest_required_by_utc: datetime
    delivery_feasible: bool
    event_allocations: list[dict[str, Any]] = Field(default_factory=list)


class ProcurementPlanResponse(BaseModel):
    tenant_id: str
    calculated_at_utc: datetime
    lines: list[ProcurementLineAllocationPlan] = Field(default_factory=list)
    consolidated_purchases: list[MultiEventConsolidatedPurchaseItem] = Field(default_factory=list)
    exceptions: list[dict[str, Any]] = Field(default_factory=list)
    total_estimated_purchase_value: Optional[float] = None
    physical_readiness_percent: float = 0.0
    projected_readiness_percent: float = 0.0


class ProcurementOptimizeRequest(BaseModel):
    tenant_id: str
    plan_request: ProcurementPlanRequest
    max_budget: Optional[float] = None
    prefer_local_and_transfers: bool = True


class ProcurementOptimizeResponse(BaseModel):
    feasible: bool
    solver: str = "SCIPY_HIGHS_MILP"
    objective_value: float = 0.0
    total_purchase_cost: float = 0.0
    total_excess_qty: float = 0.0
    infeasibility_reason: Optional[str] = None
    assumptions: list[str] = Field(default_factory=list)
    optimized_plan: Optional[ProcurementPlanResponse] = None


class ProcurementAiAnalyzeRequest(BaseModel):
    tenant_id: str
    question: str
    provider_mode: str = "LOCAL"
    preferred_provider: Optional[str] = None
    model_name: Optional[str] = None
    api_key: Optional[str] = None
    ollama_base_url: Optional[str] = None
    plan_context: dict[str, Any] = Field(default_factory=dict)


class ProcurementAiAnalyzeResponse(BaseModel):
    provider_used: str = "LOCAL_DETERMINISTIC"
    model_used: str = "deterministic-grounding-v1"
    answer: str
    referenced_materials: list[str] = Field(default_factory=list)
    referenced_suppliers: list[str] = Field(default_factory=list)
    recommendations: list[str] = Field(default_factory=list)
    data_grounding_notes: list[str] = Field(default_factory=list)
