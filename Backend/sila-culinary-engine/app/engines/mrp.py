from __future__ import annotations

import math
from decimal import Decimal, ROUND_HALF_UP
from pydantic import BaseModel, Field


class MrpLineInput(BaseModel):
    material_name: str
    material_code: str | None = None
    material_found: bool = True
    uom: str = "kg"
    uptake_percent: Decimal = Field(default=Decimal("100"), ge=Decimal("0"), le=Decimal("200"))
    edible_portion_grams: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    prep_buffer_percent: Decimal = Field(default=Decimal("0"), ge=Decimal("0"), le=Decimal("100"))
    usable_yield_percent: Decimal = Field(default=Decimal("100"), gt=Decimal("0"), le=Decimal("100"))
    fixed_raw_requirement: Decimal | None = Field(default=None, ge=Decimal("0"))
    base_guest_reference: Decimal = Field(default=Decimal("500"), gt=Decimal("0"))
    prepared_stock_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    available_stock: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    existing_reservations: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    safety_stock: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    incoming_po_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    incoming_transfer_qty: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    alt_store_available: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    pack_size: Decimal = Field(default=Decimal("1"), gt=Decimal("0"))
    unit_cost: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))


class EventMrpRequest(BaseModel):
    event_reference: str
    expected_guests: int = Field(gt=0)
    lines: list[MrpLineInput]


class MrpLineResult(BaseModel):
    material_name: str
    material_code: str | None
    material_found: bool
    uom: str
    expected_portions: Decimal
    edible_requirement: Decimal
    buffered_edible_requirement: Decimal
    raw_requirement: Decimal
    net_available_stock: Decimal
    projected_stock: Decimal
    shortage: Decimal
    recommended_transfer: Decimal
    recommended_purchase: Decimal
    recommended_packs: int
    excess_after_rounding: Decimal
    estimated_procurement_cost: Decimal
    action_status: str


class EventMrpResult(BaseModel):
    event_reference: str
    expected_guests: int
    total_shortage_lines: int
    missing_material_lines: int
    total_estimated_procurement_cost: Decimal
    lines: list[MrpLineResult]


def _q(value: Decimal) -> Decimal:
    return value.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def calculate_event_mrp(req: EventMrpRequest) -> EventMrpResult:
    guests = Decimal(req.expected_guests)
    results: list[MrpLineResult] = []
    shortage_lines = 0
    missing_lines = 0
    total_cost = Decimal("0")

    for line in req.lines:
        expected_portions = _q(guests * (line.uptake_percent / Decimal("100")))
        if line.fixed_raw_requirement is not None and line.edible_portion_grams == Decimal("0"):
            scale = guests / line.base_guest_reference
            raw_req = _q(line.fixed_raw_requirement * scale)
            edible_req = raw_req
            buffered_edible = raw_req
        else:
            edible_req = (expected_portions * line.edible_portion_grams) / Decimal("1000")
            buffered_edible = edible_req * (Decimal("1") + line.prep_buffer_percent / Decimal("100"))
            raw_req = buffered_edible / (line.usable_yield_percent / Decimal("100"))

        net_requirement = max(Decimal("0"), raw_req - line.prepared_stock_qty)
        net_available = max(Decimal("0"), line.available_stock - line.existing_reservations - line.safety_stock)
        projected = net_available + line.incoming_po_qty + line.incoming_transfer_qty
        shortage = max(Decimal("0"), net_requirement - projected)

        if not line.material_found:
            missing_lines += 1
            rec_transfer = Decimal("0")
            rec_purchase = Decimal("0")
            packs = 0
            excess = Decimal("0")
            est_cost = Decimal("0")
            status = "MATERIAL_CREATION_REQUIRED"
        else:
            rec_transfer = min(shortage, line.alt_store_available)
            remaining_after_transfer = max(Decimal("0"), shortage - rec_transfer)
            if remaining_after_transfer > Decimal("0"):
                shortage_lines += 1
                packs = math.ceil(float(remaining_after_transfer / line.pack_size))
                rec_purchase = Decimal(packs) * line.pack_size
                excess = rec_purchase - remaining_after_transfer
                est_cost = _q(rec_purchase * line.unit_cost)
                status = "PROCUREMENT_RECOMMENDED"
            elif rec_transfer > Decimal("0"):
                shortage_lines += 1
                packs = 0
                rec_purchase = Decimal("0")
                excess = Decimal("0")
                est_cost = Decimal("0")
                status = "TRANSFER_RECOMMENDED"
            else:
                packs = 0
                rec_purchase = Decimal("0")
                excess = Decimal("0")
                est_cost = Decimal("0")
                status = "COVERED"

        total_cost += est_cost
        results.append(
            MrpLineResult(
                material_name=line.material_name,
                material_code=line.material_code,
                material_found=line.material_found,
                uom=line.uom,
                expected_portions=expected_portions,
                edible_requirement=_q(edible_req),
                buffered_edible_requirement=_q(buffered_edible),
                raw_requirement=_q(raw_req),
                net_available_stock=_q(net_available),
                projected_stock=_q(projected),
                shortage=_q(shortage),
                recommended_transfer=_q(rec_transfer),
                recommended_purchase=_q(rec_purchase),
                recommended_packs=packs,
                excess_after_rounding=_q(excess),
                estimated_procurement_cost=est_cost,
                action_status=status,
            )
        )

    return EventMrpResult(
        event_reference=req.event_reference,
        expected_guests=req.expected_guests,
        total_shortage_lines=shortage_lines,
        missing_material_lines=missing_lines,
        total_estimated_procurement_cost=_q(total_cost),
        lines=results,
    )
