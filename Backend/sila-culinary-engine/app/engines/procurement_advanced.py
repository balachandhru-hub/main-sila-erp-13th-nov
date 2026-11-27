from __future__ import annotations

import math
import re
from collections import defaultdict
from datetime import datetime, timedelta, timezone
from decimal import Decimal, ROUND_CEILING, ROUND_HALF_UP
from typing import Any

import numpy as np
from scipy.optimize import Bounds, LinearConstraint, milp

from app.providers.adapters import ClaudeSdkAdapter, GeminiSdkAdapter, LocalOllamaAdapter
from app.providers.router import AiExecutionRequest, AiProviderRouter, ProviderUnavailableError
from app.schemas.procurement_engine import (
    MultiEventConsolidatedPurchaseItem,
    ProcurementAiAnalyzeRequest,
    ProcurementAiAnalyzeResponse,
    ProcurementDemandLineInput,
    ProcurementIncomingItoInput,
    ProcurementIncomingPoInput,
    ProcurementInventorySourceInput,
    ProcurementLineAllocationPlan,
    ProcurementOptimizeRequest,
    ProcurementOptimizeResponse,
    ProcurementPlanRequest,
    ProcurementPlanResponse,
    ProcurementSupplierOfferInput,
    SupplyDemandLedgerEntry,
)

_Q4 = Decimal("0.0001")
_Q2 = Decimal("0.01")
_ZERO = Decimal("0")


def _dec(val: Any) -> Decimal:
    if val is None:
        return _ZERO
    return Decimal(str(val))


def _r4(val: Decimal) -> float:
    return float(val.quantize(_Q4, rounding=ROUND_HALF_UP))


def _r2(val: Decimal) -> float:
    return float(val.quantize(_Q2, rounding=ROUND_HALF_UP))


def _eligible_source_qty(
    src: ProcurementInventorySourceInput,
    required_by: datetime,
    allow_safety_override: bool,
) -> tuple[Decimal, Decimal, Decimal, Decimal, list[dict[str, Any]]]:
    """
    Returns (on_hand, competing_res, safety_stock, eligible_allocatable, source_exceptions).
    Strictly preserves unknown availability (on_hand_qty is None) and excludes expired/blocked/QI/short shelf-life stock.
    """
    exceptions: list[dict[str, Any]] = []
    if src.on_hand_qty is None:
        exceptions.append(
            {
                "exception_type": "UNKNOWN_STOCK",
                "severity": "WARNING",
                "material_code": src.material_code,
                "source_id": src.source_id,
                "message": f"Stock quantity unknown at {src.location_name or src.storage_location_code}.",
            }
        )
        return _ZERO, _ZERO, _ZERO, _ZERO, exceptions

    on_hand = max(_ZERO, _dec(src.on_hand_qty))
    competing_res = _ZERO if src.available_already_excludes_reservations else max(_ZERO, _dec(src.existing_reservations_qty))
    safety = _ZERO if (allow_safety_override or src.available_already_excludes_safety_stock) else max(_ZERO, _dec(src.safety_stock_qty))

    if src.is_stale:
        exceptions.append(
            {
                "exception_type": "STALE_INVENTORY_DATA",
                "severity": "WARNING",
                "material_code": src.material_code,
                "source_id": src.source_id,
                "message": f"Inventory timestamp stale at {src.location_name or src.storage_location_code}.",
            }
        )

    # Check blocked / QI / damaged
    ineligible = max(_ZERO, _dec(src.blocked_qty)) + max(_ZERO, _dec(src.quality_inspection_qty)) + max(_ZERO, _dec(src.damaged_qty))
    if src.quality_status.upper() in {"BLOCKED", "QI", "QUALITY_INSPECTION", "DAMAGED", "EXPIRED"}:
        exceptions.append(
            {
                "exception_type": "BLOCKED_STOCK" if src.quality_status.upper() == "BLOCKED" else "EXPIRED_STOCK",
                "severity": "WARNING",
                "material_code": src.material_code,
                "source_id": src.source_id,
                "message": f"Stock at {src.location_name or src.storage_location_code} excluded due to quality status {src.quality_status}.",
            }
        )
        return on_hand, competing_res, safety, _ZERO, exceptions

    # Batch & expiry food safety gate
    if src.expiry_date_utc is not None:
        cutoff = required_by + timedelta(days=max(0, src.min_remaining_shelf_life_days))
        if src.expiry_date_utc <= cutoff:
            exceptions.append(
                {
                    "exception_type": "EXPIRED_STOCK",
                    "severity": "WARNING",
                    "material_code": src.material_code,
                    "source_id": src.source_id,
                    "message": f"Batch {src.batch_number or 'N/A'} expires {src.expiry_date_utc.date()} before required usage/shelf-life cutoff.",
                }
            )
            return on_hand, competing_res, safety, _ZERO, exceptions

    eligible = max(_ZERO, on_hand - ineligible - competing_res - safety)
    return on_hand, competing_res, safety, eligible, exceptions


def _calculate_earliest_delivery(offer: ProcurementSupplierOfferInput, order_date: datetime) -> datetime:
    """Walk business delivery calendar forward from order_date for lead_time_days + inspection allowance."""
    allowed_days = set(offer.delivery_weekdays or [0, 1, 2, 3, 4, 5, 6])
    cursor = order_date + timedelta(days=offer.lead_time_days)
    guard = 0
    while cursor.weekday() not in allowed_days and guard < 14:
        cursor += timedelta(days=1)
        guard += 1
    return cursor + timedelta(hours=offer.receiving_inspection_hours)


def _round_purchase_qty(net_qty: Decimal, offer: ProcurementSupplierOfferInput) -> tuple[Decimal, int, Decimal]:
    """Compute recommended purchasing qty respecting pack size & MOQ without altering consumption requirement."""
    if net_qty <= _ZERO:
        return _ZERO, 0, _ZERO
    pack = max(_dec(offer.pack_size_base_qty), Decimal("0.0001"))
    moq = max(_ZERO, _dec(offer.moq_base_qty))
    target = max(net_qty, moq)
    packs = int((target / pack).to_integral_value(rounding=ROUND_CEILING))
    rec_qty = Decimal(packs) * pack
    excess = max(_ZERO, rec_qty - net_qty)
    return rec_qty, packs, excess


def compute_procurement_plan(req: ProcurementPlanRequest) -> ProcurementPlanResponse:
    now_utc = req.reference_now_utc or datetime.now(timezone.utc)
    if now_utc.tzinfo is None:
        now_utc = now_utc.replace(tzinfo=timezone.utc)

    # Sort demand lines deterministically by confirmed status, priority_rank, and required_by_utc
    ordered_demand = sorted(
        req.demand_lines,
        key=lambda d: (0 if d.is_event_confirmed else 1, d.priority_rank, d.required_by_utc, d.line_id),
    )

    # Mutable remaining pools keyed cleanly so concurrent events never over-allocate
    remaining_source: dict[str, Decimal] = {}
    source_meta: dict[str, tuple[Decimal, Decimal, Decimal, list[dict[str, Any]]]] = {}
    for src in req.inventory_sources:
        # Pick earliest demand date for expiry check baseline if multiple exist
        mat_dates = [d.required_by_utc for d in ordered_demand if d.material_code == src.material_code]
        ref_date = min(mat_dates) if mat_dates else now_utc
        on_hand, comp_res, safety, eligible, exs = _eligible_source_qty(src, ref_date, req.allow_safety_stock_override)
        remaining_source[src.source_id] = eligible
        source_meta[src.source_id] = (on_hand, comp_res, safety, exs)

    remaining_po: dict[str, Decimal] = {}
    for po in req.incoming_pos:
        if po.is_cancelled or po.delivery_completed or po.po_approval_status.upper() in {"CANCELLED", "REJECTED"}:
            remaining_po[po.po_item_id] = _ZERO
        else:
            net_received = max(_ZERO, _dec(po.received_qty) - _dec(po.reversed_receipt_qty))
            open_qty = max(_ZERO, _dec(po.ordered_qty) - net_received - _dec(po.cancelled_qty) - _dec(po.already_allocated_qty))
            remaining_po[po.po_item_id] = open_qty

    remaining_ito: dict[str, Decimal] = {}
    for ito in req.incoming_itos:
        if ito.status.upper() in {"CANCELLED", "REJECTED", "FULLY_REVERSED"}:
            remaining_ito[ito.ito_item_id] = _ZERO
        else:
            remaining_ito[ito.ito_item_id] = max(_ZERO, _dec(ito.approved_qty) - _dec(ito.received_qty) - _dec(ito.already_allocated_qty))

    all_exceptions: list[dict[str, Any]] = []
    line_plans: list[ProcurementLineAllocationPlan] = []
    consolidation_buckets: dict[tuple[str, str, str, str, str], dict[str, Any]] = {}

    for d in ordered_demand:
        line_exs: list[dict[str, Any]] = []
        req_by = d.required_by_utc if d.required_by_utc.tzinfo else d.required_by_utc.replace(tzinfo=timezone.utc)
        gross = max(_ZERO, _dec(d.gross_requirement_qty))

        if not d.is_resolved_material or not d.material_code:
            line_exs.append(
                {
                    "exception_type": "MISSING_MATERIAL",
                    "severity": "BLOCKING",
                    "event_id": d.event_id,
                    "material_code": d.material_code,
                    "message": f"Material mapping unresolved for {d.material_description}.",
                }
            )
            all_exceptions.extend(line_exs)
            line_plans.append(
                ProcurementLineAllocationPlan(
                    line_id=d.line_id,
                    event_id=d.event_id,
                    event_code=d.event_code,
                    material_code=d.material_code,
                    material_description=d.material_description,
                    base_uom=d.base_uom,
                    required_by_utc=req_by,
                    gross_requirement_qty=_r4(gross),
                    remaining_confirmed_gap_qty=_r4(gross),
                    net_purchase_requirement_qty=_r4(gross),
                    coverage_status="MISSING_MATERIAL",
                    exceptions=line_exs,
                )
            )
            continue

        mat_code = d.material_code
        # Collect matching inventory sources sorted FEFO (earliest valid expiry first, None last)
        matching_sources = [
            s
            for s in req.inventory_sources
            if s.material_code == mat_code and (not d.property_id or not s.property_id or s.property_id == d.property_id)
        ]

        local_types = {"LOCAL_KITCHEN", "LOCAL_OUTLET"}
        local_sources = [
            s
            for s in matching_sources
            if s.source_type in local_types
            and (
                (d.destination_location_id and s.location_id == d.destination_location_id)
                or (d.destination_storage_location and s.storage_location_code == d.destination_storage_location)
                or (not d.destination_location_id and not d.destination_storage_location)
            )
        ]
        # Fallback if property has only local sources matching plant
        if not local_sources:
            local_sources = [s for s in matching_sources if s.source_type in local_types]

        local_sources_known = [s for s in local_sources if s.on_hand_qty is not None]
        local_on_hand = sum((source_meta[s.source_id][0] for s in local_sources_known), _ZERO) if local_sources_known else None
        competing_res = sum((source_meta[s.source_id][1] for s in local_sources_known), _ZERO)
        safety_stock = sum((source_meta[s.source_id][2] for s in local_sources_known), _ZERO)
        for s in local_sources:
            line_exs.extend(source_meta[s.source_id][3])

        # FEFO sort key
        def _fefo_key(src: ProcurementInventorySourceInput) -> tuple[int, datetime]:
            if src.expiry_date_utc is None:
                return (1, datetime.max.replace(tzinfo=timezone.utc))
            exp = src.expiry_date_utc if src.expiry_date_utc.tzinfo else src.expiry_date_utc.replace(tzinfo=timezone.utc)
            return (0, exp)

        local_sources_fefo = sorted(local_sources_known, key=_fefo_key)
        eligible_local_avail = sum((remaining_source[s.source_id] for s in local_sources_fefo), _ZERO) if local_sources_known else None

        remaining_need = gross
        rec_local_res = _ZERO
        fefo_allocs: list[dict[str, Any]] = []
        ledger: list[SupplyDemandLedgerEntry] = []
        running_avail = eligible_local_avail if eligible_local_avail is not None else _ZERO

        if local_on_hand is not None:
            ledger.append(
                SupplyDemandLedgerEntry(
                    timestamp_utc=now_utc,
                    entry_type="ON_HAND_ELIGIBLE",
                    source_reference="LOCAL_STOCK",
                    material_code=mat_code,
                    delta_qty=_r4(running_avail),
                    running_projected_available=_r4(running_avail),
                    notes=f"On-hand {_r4(local_on_hand)} - competing {_r4(competing_res)} - safety {_r4(safety_stock)}",
                )
            )

        # 1. Prepared stock (if eligible)
        prep_alloc = _ZERO
        if d.prepared_stock_eligible_qty > 0 and remaining_need > _ZERO:
            prep_alloc = min(remaining_need, _dec(d.prepared_stock_eligible_qty))
            remaining_need -= prep_alloc

        # 2. Local stock FEFO reservation
        for src in local_sources_fefo:
            if remaining_need <= _ZERO:
                break
            avail = remaining_source[src.source_id]
            if avail <= _ZERO:
                continue
            # Check expiry against this line's exact required_by_utc
            if src.expiry_date_utc is not None:
                exp = src.expiry_date_utc if src.expiry_date_utc.tzinfo else src.expiry_date_utc.replace(tzinfo=timezone.utc)
                if exp <= req_by + timedelta(days=max(0, src.min_remaining_shelf_life_days)):
                    continue
            take = min(remaining_need, avail)
            remaining_source[src.source_id] -= take
            remaining_need -= take
            rec_local_res += take
            fefo_allocs.append(
                {
                    "source_id": src.source_id,
                    "location_id": src.location_id,
                    "storage_location_code": src.storage_location_code,
                    "batch_number": src.batch_number,
                    "expiry_date_utc": src.expiry_date_utc.isoformat() if src.expiry_date_utc else None,
                    "allocated_qty": _r4(take),
                }
            )

        # 3. Incoming PO allocation (only timely POs arriving <= required_by_utc)
        po_alloc_total = _ZERO
        po_allocs: list[dict[str, Any]] = []
        matching_pos = sorted(
            [p for p in req.incoming_pos if p.material_code == mat_code and (not d.plant_code or not p.plant_code or p.plant_code == d.plant_code)],
            key=lambda p: p.delivery_date_utc or datetime.max.replace(tzinfo=timezone.utc),
        )
        for po in matching_pos:
            avail_po = remaining_po.get(po.po_item_id, _ZERO)
            if avail_po <= _ZERO:
                continue
            del_dt = po.delivery_date_utc
            if del_dt is not None and del_dt.tzinfo is None:
                del_dt = del_dt.replace(tzinfo=timezone.utc)
            if del_dt is not None and del_dt > req_by:
                line_exs.append(
                    {
                        "exception_type": "DELIVERY_DEADLINE_INFEASIBLE",
                        "severity": "WARNING",
                        "event_id": d.event_id,
                        "material_code": mat_code,
                        "source_reference": po.po_number,
                        "message": f"PO {po.po_number} arrives {del_dt.isoformat()} after required deadline {req_by.isoformat()}; excluded from timely coverage.",
                    }
                )
                ledger.append(
                    SupplyDemandLedgerEntry(
                        timestamp_utc=del_dt,
                        entry_type="LATE_INCOMING_PO_EXCLUDED",
                        source_reference=po.po_number,
                        material_code=mat_code,
                        delta_qty=0.0,
                        running_projected_available=_r4(running_avail),
                        timely_for_deadline=False,
                        notes="Delivery after preparation deadline; excluded.",
                    )
                )
                continue
            if remaining_need <= _ZERO:
                break
            take = min(remaining_need, avail_po)
            remaining_po[po.po_item_id] -= take
            remaining_need -= take
            po_alloc_total += take
            running_avail += take
            po_allocs.append(
                {
                    "po_id": po.po_id,
                    "po_number": po.po_number,
                    "po_item_id": po.po_item_id,
                    "supplier_code": po.supplier_code,
                    "supplier_name": po.supplier_name,
                    "allocated_qty": _r4(take),
                    "delivery_date_utc": del_dt.isoformat() if del_dt else None,
                    "supplier_confirmed": po.supplier_confirmed,
                }
            )
            ledger.append(
                SupplyDemandLedgerEntry(
                    timestamp_utc=del_dt or now_utc,
                    entry_type="INCOMING_PO_ALLOCATED",
                    source_reference=po.po_number,
                    material_code=mat_code,
                    delta_qty=_r4(take),
                    running_projected_available=_r4(running_avail),
                    timely_for_deadline=True,
                )
            )

        # 4. Confirmed incoming ITOs
        ito_alloc_total = _ZERO
        for ito in req.incoming_itos:
            if ito.material_code != mat_code or remaining_need <= _ZERO:
                continue
            if d.destination_location_id and ito.to_location_id and ito.to_location_id != d.destination_location_id:
                continue
            avail_ito = remaining_ito.get(ito.ito_item_id, _ZERO)
            if avail_ito <= _ZERO:
                continue
            arr_dt = ito.expected_arrival_utc
            if arr_dt is not None and arr_dt.tzinfo is None:
                arr_dt = arr_dt.replace(tzinfo=timezone.utc)
            if arr_dt is not None and arr_dt > req_by:
                continue
            take = min(remaining_need, avail_ito)
            remaining_ito[ito.ito_item_id] -= take
            remaining_need -= take
            ito_alloc_total += take

        # 5. Proposed Internal Transfers (Central Store / Other Kitchen / Other Outlet) — same-plant 311 priority
        proposed_ito_total = _ZERO
        ito_recs: list[dict[str, Any]] = []
        transfer_sources = [
            s
            for s in matching_sources
            if s.source_type in {"CENTRAL_STORE", "SUB_STORE", "OTHER_KITCHEN", "OTHER_OUTLET"}
            and s.transfer_enabled
            and s.on_hand_qty is not None
            and (s.location_id != d.destination_location_id or not d.destination_location_id)
            and (s.storage_location_code != d.destination_storage_location or not d.destination_storage_location)
        ]
        # Same-plant 311 first, central store before other outlets
        transfer_sources.sort(
            key=lambda s: (
                0 if (d.plant_code and s.plant_code == d.plant_code) else 1,
                0 if s.source_type == "CENTRAL_STORE" else 1,
                _fefo_key(s),
            )
        )
        for src in transfer_sources:
            if remaining_need <= _ZERO:
                break
            avail = remaining_source[src.source_id]
            if avail <= _ZERO:
                continue
            same_plant = (not d.plant_code or not src.plant_code or d.plant_code == src.plant_code)
            if not same_plant:
                # 311 only supports same-plant; skip for 311 proposals
                continue
            expected_complete = now_utc + timedelta(hours=src.transfer_lead_time_hours)
            if expected_complete > req_by:
                continue
            take = min(remaining_need, avail)
            remaining_source[src.source_id] -= take
            if req.include_proposed_itos_in_net:
                remaining_need -= take
            proposed_ito_total += take
            ito_recs.append(
                {
                    "source_id": src.source_id,
                    "source_type": src.source_type,
                    "from_location_id": src.location_id,
                    "from_location_name": src.location_name,
                    "from_storage_location": src.storage_location_code,
                    "to_location_id": d.destination_location_id,
                    "to_storage_location": d.destination_storage_location,
                    "plant_code": src.plant_code or d.plant_code,
                    "sap_movement_type": "311",
                    "recommended_qty": _r4(take),
                    "expected_completion_utc": expected_complete.isoformat(),
                    "status": "PROPOSED",
                }
            )

        confirmed_coverage = rec_local_res + prep_alloc + po_alloc_total + ito_alloc_total
        proposed_coverage = confirmed_coverage + proposed_ito_total
        remaining_confirmed_gap = max(_ZERO, gross - confirmed_coverage)
        net_purchase_req = max(_ZERO, gross - proposed_coverage) if req.include_proposed_itos_in_net else remaining_confirmed_gap

        # Record event consumption on ledger
        ledger.append(
            SupplyDemandLedgerEntry(
                timestamp_utc=req_by,
                entry_type="EVENT_REQUIREMENT",
                source_reference=d.event_code or d.event_id,
                material_code=mat_code,
                delta_qty=-_r4(gross),
                running_projected_available=_r4(confirmed_coverage - gross),
                notes=f"Confirmed coverage {_r4(confirmed_coverage)}; proposed ITO {_r4(proposed_ito_total)}; net purchase {_r4(net_purchase_req)}",
            )
        )

        # 6. Supplier evaluation, lead time, pack size & MOQ
        offers = [
            o
            for o in req.supplier_offers
            if o.material_code == mat_code
            and o.is_approved_supplier
            and (not d.company_code or not o.company_code or d.company_code == o.company_code)
            and (not d.plant_code or not o.plant_code or d.plant_code == o.plant_code)
        ]

        supplier_comparisons: list[dict[str, Any]] = []
        best_offer: ProcurementSupplierOfferInput | None = None
        best_rec_qty = _ZERO
        best_packs = 0
        best_excess = _ZERO
        best_est_val: Decimal | None = None
        best_del_dt: datetime | None = None
        best_feasible = True

        # Mandatory active contracts take policy priority over pure lowest price (§27)
        def _offer_rank(o: ProcurementSupplierOfferInput) -> tuple[int, int, int, float, float]:
            del_dt = _calculate_earliest_delivery(o, now_utc)
            feasible = del_dt <= req_by
            rec_q, _, _ = _round_purchase_qty(net_purchase_req, o)
            price = o.unit_price if o.unit_price is not None else 9999999.0
            cost = float(rec_q) * price + o.freight_charge
            return (
                0 if o.is_contract_mandatory and o.is_contract_active else (1 if o.is_contract_active else 2),
                0 if feasible else 1,
                0 if o.unit_price is not None else 1,
                cost,
                -o.reliability_score,
            )

        ranked_offers = sorted(offers, key=_offer_rank)
        for o in ranked_offers:
            del_dt = _calculate_earliest_delivery(o, now_utc)
            feasible = del_dt <= req_by
            rec_q, packs, excess = _round_purchase_qty(net_purchase_req, o)
            est_cost = _r2(rec_q * _dec(o.unit_price) + _dec(o.freight_charge)) if o.unit_price is not None else None
            supplier_comparisons.append(
                {
                    "supplier_id": o.supplier_id,
                    "supplier_code": o.supplier_code,
                    "supplier_name": o.supplier_name,
                    "contract_id": o.contract_id,
                    "contract_number": o.contract_number,
                    "is_contract_active": o.is_contract_active,
                    "unit_price": o.unit_price,
                    "currency": o.currency,
                    "purchasing_uom": o.purchasing_uom,
                    "pack_size_base_qty": o.pack_size_base_qty,
                    "moq_base_qty": o.moq_base_qty,
                    "recommended_purchase_qty": _r4(rec_q),
                    "pack_count": packs,
                    "excess_qty": _r4(excess),
                    "estimated_cost": est_cost,
                    "expected_delivery_utc": del_dt.isoformat(),
                    "delivery_feasible": feasible,
                }
            )

        if net_purchase_req > _ZERO:
            if ranked_offers:
                best_offer = ranked_offers[0]
                best_del_dt = _calculate_earliest_delivery(best_offer, now_utc)
                best_feasible = best_del_dt <= req_by
                best_rec_qty, best_packs, best_excess = _round_purchase_qty(net_purchase_req, best_offer)
                if best_offer.unit_price is not None:
                    best_est_val = best_rec_qty * _dec(best_offer.unit_price) + _dec(best_offer.freight_charge)
                else:
                    line_exs.append(
                        {
                            "exception_type": "INVALID_CATALOG_PRICE",
                            "severity": "WARNING",
                            "event_id": d.event_id,
                            "material_code": mat_code,
                            "message": f"Missing catalog/contract unit price for supplier {best_offer.supplier_name}.",
                        }
                    )
                if not best_feasible:
                    line_exs.append(
                        {
                            "exception_type": "DELIVERY_DEADLINE_INFEASIBLE",
                            "severity": "BLOCKING",
                            "event_id": d.event_id,
                            "material_code": mat_code,
                            "message": f"Earliest delivery ({best_del_dt.isoformat()}) from {best_offer.supplier_name} exceeds deadline ({req_by.isoformat()}).",
                        }
                    )
                if _dec(best_offer.moq_base_qty) > net_purchase_req:
                    line_exs.append(
                        {
                            "exception_type": "SUPPLIER_MOQ_CONFLICT",
                            "severity": "INFO",
                            "event_id": d.event_id,
                            "material_code": mat_code,
                            "message": f"Supplier MOQ ({best_offer.moq_base_qty} {d.base_uom}) exceeds net requirement ({_r4(net_purchase_req)} {d.base_uom}); excess {_r4(best_excess)} {d.base_uom}.",
                        }
                    )
                # Multi-event consolidation bucket (compatible purchasing context)
                ckey = (
                    mat_code,
                    best_offer.supplier_code,
                    d.company_code or "",
                    d.plant_code or "",
                    best_offer.currency,
                )
                if ckey not in consolidation_buckets:
                    consolidation_buckets[ckey] = {
                        "offer": best_offer,
                        "company_code": d.company_code,
                        "plant_code": d.plant_code,
                        "combined_net": _ZERO,
                        "earliest_req": req_by,
                        "events": [],
                    }
                b = consolidation_buckets[ckey]
                b["combined_net"] += net_purchase_req
                if req_by < b["earliest_req"]:
                    b["earliest_req"] = req_by
                b["events"].append({"event_id": d.event_id, "event_code": d.event_code, "allocated_net_qty": _r4(net_purchase_req)})
            else:
                best_rec_qty = net_purchase_req
                line_exs.append(
                    {
                        "exception_type": "MISSING_SUPPLIER",
                        "severity": "BLOCKING",
                        "event_id": d.event_id,
                        "material_code": mat_code,
                        "message": f"No approved supplier/contract/catalog offer found for {mat_code}.",
                    }
                )

        # Coverage status determination
        if net_purchase_req <= _ZERO and confirmed_coverage >= gross:
            cov_status = "STOCK_RESERVED" if rec_local_res >= gross else ("INCOMING_PO_ALLOCATED" if po_alloc_total > _ZERO else "MATERIAL_READY")
        elif net_purchase_req <= _ZERO and proposed_ito_total > _ZERO:
            cov_status = "ITO_PROPOSED"
        elif best_offer is not None and not best_feasible:
            cov_status = "DELIVERY_RISK"
        elif net_purchase_req > _ZERO:
            cov_status = "PURCHASE_RECOMMENDED"
        else:
            cov_status = "REQUIREMENT_VALIDATED"

        all_exceptions.extend(line_exs)
        line_plans.append(
            ProcurementLineAllocationPlan(
                line_id=d.line_id,
                event_id=d.event_id,
                event_code=d.event_code,
                material_code=mat_code,
                material_description=d.material_description,
                base_uom=d.base_uom,
                required_by_utc=req_by,
                gross_requirement_qty=_r4(gross),
                local_on_hand_qty=_r4(local_on_hand) if local_on_hand is not None else None,
                competing_reservations_qty=_r4(competing_res),
                safety_stock_qty=_r4(safety_stock),
                eligible_local_stock_qty=_r4(eligible_local_avail) if eligible_local_avail is not None else None,
                recommended_local_reservation_qty=_r4(rec_local_res),
                prepared_stock_allocated_qty=_r4(prep_alloc),
                incoming_po_allocated_qty=_r4(po_alloc_total),
                incoming_ito_allocated_qty=_r4(ito_alloc_total),
                proposed_ito_qty=_r4(proposed_ito_total),
                confirmed_coverage_qty=_r4(confirmed_coverage),
                proposed_total_coverage_qty=_r4(proposed_coverage),
                remaining_confirmed_gap_qty=_r4(remaining_confirmed_gap),
                net_purchase_requirement_qty=_r4(net_purchase_req),
                recommended_purchase_qty=_r4(best_rec_qty),
                purchasing_uom=best_offer.purchasing_uom if best_offer else d.base_uom,
                pack_size_base_qty=best_offer.pack_size_base_qty if best_offer else 1.0,
                pack_count=best_packs,
                moq_base_qty=best_offer.moq_base_qty if best_offer else 0.0,
                expected_excess_qty=_r4(best_excess),
                recommended_supplier_id=best_offer.supplier_id if best_offer else None,
                recommended_supplier_code=best_offer.supplier_code if best_offer else None,
                recommended_supplier_name=best_offer.supplier_name if best_offer else None,
                contract_id=best_offer.contract_id if best_offer else None,
                contract_number=best_offer.contract_number if best_offer else None,
                unit_price=best_offer.unit_price if best_offer else None,
                currency=best_offer.currency if best_offer else "AED",
                estimated_purchase_value=_r2(best_est_val) if best_est_val is not None else None,
                expected_delivery_utc=best_del_dt,
                delivery_feasible=best_feasible,
                coverage_status=cov_status,
                fefo_batch_allocations=fefo_allocs,
                incoming_po_allocations=po_allocs,
                ito_recommendations=ito_recs,
                supplier_comparisons=supplier_comparisons,
                ledger=ledger,
                exceptions=line_exs,
            )
        )

    consolidated: list[MultiEventConsolidatedPurchaseItem] = []
    for (mat, sup, comp, plant, curr), b in consolidation_buckets.items():
        offer: ProcurementSupplierOfferInput = b["offer"]
        comb_net: Decimal = b["combined_net"]
        rec_q, _, excess = _round_purchase_qty(comb_net, offer)
        del_dt = _calculate_earliest_delivery(offer, now_utc)
        est_total = _r2(rec_q * _dec(offer.unit_price) + _dec(offer.freight_charge)) if offer.unit_price is not None else None
        consolidated.append(
            MultiEventConsolidatedPurchaseItem(
                consolidation_key=f"{mat}|{sup}|{comp}|{plant}|{curr}",
                material_code=mat,
                supplier_code=sup,
                supplier_name=offer.supplier_name,
                company_code=b["company_code"],
                plant_code=b["plant_code"],
                purchasing_org=offer.purchasing_org,
                currency=curr,
                combined_net_qty=_r4(comb_net),
                pack_size_base_qty=offer.pack_size_base_qty,
                moq_base_qty=offer.moq_base_qty,
                recommended_purchase_qty=_r4(rec_q),
                expected_excess_qty=_r4(excess),
                estimated_total_cost=est_total,
                earliest_required_by_utc=b["earliest_req"],
                delivery_feasible=del_dt <= b["earliest_req"],
                event_allocations=b["events"],
            )
        )

    total_est = None
    valued_lines = [l.estimated_purchase_value for l in line_plans if l.estimated_purchase_value is not None]
    if valued_lines:
        total_est = _r2(sum((_dec(v) for v in valued_lines), _ZERO))

    total_gross = sum((_dec(l.gross_requirement_qty) for l in line_plans), _ZERO)
    total_phys = sum((_dec(l.recommended_local_reservation_qty) + _dec(l.prepared_stock_allocated_qty) for l in line_plans), _ZERO)
    total_proj = sum((_dec(l.proposed_total_coverage_qty) + _dec(l.recommended_purchase_qty) for l in line_plans), _ZERO)
    phys_pct = _r2((total_phys / total_gross * Decimal("100")) if total_gross > _ZERO else _ZERO)
    proj_pct = _r2((min(total_gross, total_proj) / total_gross * Decimal("100")) if total_gross > _ZERO else _ZERO)

    return ProcurementPlanResponse(
        tenant_id=req.tenant_id,
        calculated_at_utc=now_utc,
        lines=line_plans,
        consolidated_purchases=consolidated,
        exceptions=all_exceptions,
        total_estimated_purchase_value=total_est,
        physical_readiness_percent=phys_pct,
        projected_readiness_percent=proj_pct,
    )


def optimize_procurement_milp(req: ProcurementOptimizeRequest) -> ProcurementOptimizeResponse:
    """
    Use scipy.optimize.milp (HiGHS) to pick feasible supplier/pack counts minimizing total purchase cost + excess penalty
    across net uncovered material lines subject to MOQ, deadline feasibility, mandatory contracts, and optional max_budget.
    """
    base_plan = compute_procurement_plan(req.plan_request)
    now_utc = req.plan_request.reference_now_utc or datetime.now(timezone.utc)
    if now_utc.tzinfo is None:
        now_utc = now_utc.replace(tzinfo=timezone.utc)

    purchase_lines = [l for l in base_plan.lines if l.net_purchase_requirement_qty > 0]
    if not purchase_lines:
        return ProcurementOptimizeResponse(
            feasible=True,
            solver="SCIPY_HIGHS_MILP",
            objective_value=0.0,
            total_purchase_cost=0.0,
            total_excess_qty=0.0,
            assumptions=["All requirements are covered by local/prepared/incoming stock; zero new purchases needed."],
            optimized_plan=base_plan,
        )

    # Decision variables: for each line i and feasible offer j:
    # y_{i,j} in {0, 1} (supplier selected), n_{i,j} in Z_+ (pack count)
    pair_meta: list[tuple[int, ProcurementSupplierOfferInput, float, float, float]] = []
    line_to_pairs: dict[int, list[int]] = defaultdict(list)

    for idx, line in enumerate(purchase_lines):
        req_by = line.required_by_utc if line.required_by_utc.tzinfo else line.required_by_utc.replace(tzinfo=timezone.utc)
        offers = [
            o
            for o in req.plan_request.supplier_offers
            if o.material_code == line.material_code
            and o.is_approved_supplier
            and o.unit_price is not None
            and _calculate_earliest_delivery(o, now_utc) <= req_by
        ]
        # Respect mandatory active contracts when present
        mandatory = [o for o in offers if o.is_contract_mandatory and o.is_contract_active]
        if mandatory:
            offers = mandatory
        if not offers:
            return ProcurementOptimizeResponse(
                feasible=False,
                solver="SCIPY_HIGHS_MILP",
                infeasibility_reason=f"No timely priced approved supplier available for {line.material_code} ({line.material_description}) before {req_by.isoformat()}.",
                assumptions=["Hard constraint: approved supplier with valid price and delivery <= required_by_utc."],
                optimized_plan=base_plan,
            )
        for o in offers:
            var_idx = len(pair_meta)
            pair_meta.append((idx, o, line.net_purchase_requirement_qty, o.pack_size_base_qty, float(o.unit_price or 0.0)))
            line_to_pairs[idx].append(var_idx)

    k = len(pair_meta)
    num_vars = 2 * k  # [y_0..y_{k-1}, n_0..n_{k-1}]
    c = np.zeros(num_vars, dtype=float)
    lb = np.zeros(num_vars, dtype=float)
    ub = np.zeros(num_vars, dtype=float)
    integrality = np.ones(num_vars, dtype=int)

    rows: list[np.ndarray] = []
    con_lb: list[float] = []
    con_ub: list[float] = []

    for j, (line_idx, offer, net_qty, pack, price) in enumerate(pair_meta):
        # y_j in [0, 1], n_j in [0, upper_packs]
        ub[j] = 1.0
        max_packs = max(1, math.ceil(max(net_qty, offer.moq_base_qty) / max(pack, 0.0001)) + 5)
        ub[k + j] = float(max_packs)
        # Cost objective: freight * y_j + (pack * price + 0.01 * pack excess weight) * n_j
        c[j] = float(offer.freight_charge)
        c[k + j] = pack * price + 0.01 * pack

        # Linking: pack * n_j >= max(net_qty, moq) * y_j  =>  pack * n_j - req_min * y_j >= 0
        req_min = max(net_qty, offer.moq_base_qty)
        r_min = np.zeros(num_vars, dtype=float)
        r_min[k + j] = pack
        r_min[j] = -req_min
        rows.append(r_min)
        con_lb.append(0.0)
        con_ub.append(math.inf)

        # Linking upper bound: n_j - max_packs * y_j <= 0
        r_max = np.zeros(num_vars, dtype=float)
        r_max[k + j] = 1.0
        r_max[j] = -float(max_packs)
        rows.append(r_max)
        con_lb.append(-math.inf)
        con_ub.append(0.0)

    # Exactly 1 supplier per line: sum_{j in line} y_j == 1
    for idx in range(len(purchase_lines)):
        r_sel = np.zeros(num_vars, dtype=float)
        for j in line_to_pairs[idx]:
            r_sel[j] = 1.0
        rows.append(r_sel)
        con_lb.append(1.0)
        con_ub.append(1.0)

    # Optional budget constraint
    if req.max_budget is not None:
        r_bud = np.zeros(num_vars, dtype=float)
        for j, (_, offer, _, pack, price) in enumerate(pair_meta):
            r_bud[j] = float(offer.freight_charge)
            r_bud[k + j] = pack * price
        rows.append(r_bud)
        con_lb.append(-math.inf)
        con_ub.append(float(req.max_budget))

    res = milp(
        c=c,
        integrality=integrality,
        bounds=Bounds(lb, ub),
        constraints=LinearConstraint(np.vstack(rows), con_lb, con_ub),
    )

    if not res.success:
        return ProcurementOptimizeResponse(
            feasible=False,
            solver="SCIPY_HIGHS_MILP",
            infeasibility_reason=f"MILP infeasible: {res.message}",
            assumptions=["Budget, MOQ, pack multiples, and delivery deadlines enforced as hard constraints."],
            optimized_plan=base_plan,
        )

    total_cost = Decimal("0")
    total_excess = Decimal("0")
    for j, (line_idx, offer, net_qty, pack, price) in enumerate(pair_meta):
        if res.x[j] > 0.5:
            packs = int(round(res.x[k + j]))
            qty = _dec(packs) * _dec(pack)
            excess = max(_ZERO, qty - _dec(net_qty))
            cost = qty * _dec(price) + _dec(offer.freight_charge)
            total_cost += cost
            total_excess += excess
            line = purchase_lines[line_idx]
            line.recommended_supplier_id = offer.supplier_id
            line.recommended_supplier_code = offer.supplier_code
            line.recommended_supplier_name = offer.supplier_name
            line.contract_id = offer.contract_id
            line.contract_number = offer.contract_number
            line.pack_size_base_qty = pack
            line.pack_count = packs
            line.moq_base_qty = offer.moq_base_qty
            line.recommended_purchase_qty = _r4(qty)
            line.expected_excess_qty = _r4(excess)
            line.unit_price = price
            line.estimated_purchase_value = _r2(cost)

    base_plan.total_estimated_purchase_value = _r2(total_cost)
    return ProcurementOptimizeResponse(
        feasible=True,
        solver="SCIPY_HIGHS_MILP",
        objective_value=round(float(res.fun), 4),
        total_purchase_cost=_r2(total_cost),
        total_excess_qty=_r4(total_excess),
        assumptions=[
            "HiGHS MILP exact integer pack & supplier selection",
            "Mandatory active contracts & lead-time feasibility enforced",
            "No stock reservation, ITO, or PO created by optimizer",
        ],
        optimized_plan=base_plan,
    )


_INJECTION_RE = re.compile(r"(ignore\s+previous|system\s+prompt|drop\s+table|post\s+sap|release\s+po|execute\s+sql)", re.IGNORECASE)


def _deterministic_procurement_answer(req: ProcurementAiAnalyzeRequest) -> ProcurementAiAnalyzeResponse:
    q = (req.question or "").strip()
    if _INJECTION_RE.search(q):
        return ProcurementAiAnalyzeResponse(
            provider_used="LOCAL_DETERMINISTIC",
            model_used="policy-guard-v1",
            answer="Refused: prompt-injection or unauthorized transaction instruction detected. AI Procurement Agent is read-only and cannot post SAP transfers, reserve stock, or release POs.",
            data_grounding_notes=["Prompt-injection guardrail triggered; zero mutations executed."],
        )

    lines: list[dict[str, Any]] = req.plan_context.get("lines", []) or []
    shortages = [l for l in lines if float(l.get("net_uncovered_qty") or l.get("net_purchase_requirement_qty") or 0) > 0]
    shortages.sort(key=lambda x: float(x.get("net_uncovered_qty") or x.get("net_purchase_requirement_qty") or 0), reverse=True)
    itos = [l for l in lines if float(l.get("proposed_ito_qty") or 0) > 0]
    incoming = [l for l in lines if float(l.get("incoming_po_allocated_qty") or 0) > 0]

    ref_mats = [str(l.get("material_code") or l.get("material_description")) for l in shortages[:5]]
    ref_sups = sorted({str(l.get("recommended_supplier_name")) for l in shortages if l.get("recommended_supplier_name")})

    parts: list[str] = []
    recs: list[str] = []
    if shortages:
        top = ", ".join(
            f"{l.get('material_description')} (net {l.get('net_uncovered_qty') or l.get('net_purchase_requirement_qty')} {l.get('base_uom', 'KG')}, rec purchase {l.get('recommended_purchase_qty')} via {l.get('recommended_supplier_name') or 'unassigned'})"
            for l in shortages[:5]
        )
        parts.append(f"Materials requiring procurement: {top}.")
    else:
        parts.append("All evaluated materials are covered by local stock, prepared stock, incoming POs, or transfers.")

    if itos:
        parts.append(f"{len(itos)} material(s) have same-plant 311 ITO recommendations reducing external purchase volume.")
        recs.append("Submit proposed 311 ITO drafts through existing ITO approval before converting remaining gaps to POs.")
    if incoming:
        parts.append(f"{len(incoming)} material(s) rely on timely incoming PO allocations.")
        recs.append("Monitor supplier confirmations and GRN receipts before preparation deadlines.")
    for l in shortages[:3]:
        ex = float(l.get("expected_excess_qty") or 0)
        if ex > 0:
            recs.append(
                f"{l.get('material_description')}: recommended purchase ({l.get('recommended_purchase_qty')} {l.get('base_uom')}) exceeds net gap due to pack/MOQ rules (excess {ex})."
            )

    return ProcurementAiAnalyzeResponse(
        provider_used="LOCAL_DETERMINISTIC",
        model_used="deterministic-grounding-v1",
        answer=" ".join(parts),
        referenced_materials=ref_mats,
        referenced_suppliers=ref_sups,
        recommendations=recs,
        data_grounding_notes=[
            "All quantities, stock, suppliers, and prices sourced directly from validated Phase 2C coverage data.",
            "AI recommendations never automatically reserve stock, post ITOs, or release POs.",
        ],
    )


async def analyze_procurement_with_ai(req: ProcurementAiAnalyzeRequest) -> ProcurementAiAnalyzeResponse:
    q = (req.question or "").strip()
    if _INJECTION_RE.search(q):
        return _deterministic_procurement_answer(req)

    mode = (req.provider_mode or "LOCAL").upper()
    # Grounded deterministic fallback is always used if external keys / Ollama daemon are unavailable
    if mode == "LOCAL" and not req.ollama_base_url:
        return _deterministic_procurement_answer(req)

    router = AiProviderRouter(
        local=LocalOllamaAdapter(base_url=req.ollama_base_url or "http://127.0.0.1:11434"),
        gemini=GeminiSdkAdapter(api_key=req.api_key) if req.api_key and mode in {"GEMINI", "HYBRID"} else None,
        claude=ClaudeSdkAdapter(api_key=req.api_key) if req.api_key and mode in {"CLAUDE", "HYBRID"} else None,
    )
    try:
        exec_req = AiExecutionRequest(
            tenant_id=req.tenant_id,
            task="procurement_analysis",
            prompt=(
                "Answer the user's procurement question strictly using the provided JSON context. "
                "Never invent material codes, supplier names, stock quantities, or prices.\n"
                f"Question: {q}\nContext: {req.plan_context}"
            ),
            mode=mode,
            preferred_provider=req.preferred_provider,
            model_name=req.model_name,
            response_schema=ProcurementAiAnalyzeResponse,
        )
        res = await router.execute_structured(exec_req)
        if isinstance(res.parsed, ProcurementAiAnalyzeResponse):
            return res.parsed
    except (ProviderUnavailableError, Exception):
        pass

    return _deterministic_procurement_answer(req)
