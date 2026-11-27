"""Deterministic & MILP-backed Kitchen Production Scheduling and Central Kitchen Allocation (Phase 4A)."""

from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP
from typing import Optional

import numpy as np
from scipy.optimize import Bounds, LinearConstraint, milp

from app.schemas.production_engine import (
    CentralKitchenAllocateRequest,
    CentralKitchenAllocateResponse,
    CentralKitchenOutletAllocation,
    EquipmentResourceInput,
    ProductionBatchTaskInput,
    ProductionScheduleRequest,
    ProductionScheduleResponse,
    ScheduledBatchAssignment,
    StaffResourceInput,
)


def _round2(value: float) -> float:
    return float(Decimal(str(value)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP))


def optimize_kitchen_schedule(request: ProductionScheduleRequest) -> ProductionScheduleResponse:
    """
    Constraint-based production batch scheduling using discrete slot MILP (SciPy HiGHS)
    with hard food-safety, holding-time, ingredient-readiness, equipment-capacity, and
    allergen-segregation enforcement. Never relaxes mandatory food safety constraints.
    """
    if not request.batches:
        return ProductionScheduleResponse(
            status="OPTIMAL",
            solver_used="scipy_highs_milp",
            assumptions=["No production batches supplied."],
        )

    conflicts: list[str] = []
    bottlenecks: list[str] = []
    assumptions = [
        "Mandatory food safety holding-time, ingredient readiness, and allergen cleaning constraints are hard constraints and never relaxed.",
        f"Time horizon discretized into 15-minute operational planning slots (objective={request.objective}).",
    ]

    eq_by_id = {e.equipment_id: e for e in request.equipment}

    # Pre-check hard constraints before solver invocation
    for b in request.batches:
        duration = b.prep_minutes + b.cook_minutes + b.cool_rest_minutes
        if duration <= 0:
            duration = 15

        if request.enforce_food_safety and not b.ingredients_ready:
            conflicts.append(
                f"Batch {b.batch_number} ({b.recipe_name}): Ingredients are not ready or failed expiry/quality gate."
            )

        if b.required_equipment_id:
            eq = eq_by_id.get(b.required_equipment_id)
            if eq is None or not eq.is_available:
                conflicts.append(
                    f"Batch {b.batch_number}: Required equipment '{b.required_equipment_id}' is unavailable or under maintenance."
                )
            elif b.planned_qty > eq.batch_capacity:
                conflicts.append(
                    f"Batch {b.batch_number}: Planned qty {b.planned_qty} exceeds equipment '{eq.name}' batch capacity {eq.batch_capacity}. Split batch."
                )

        eligible_eq = [
            e
            for e in request.equipment
            if e.is_available
            and e.kitchen_id == b.kitchen_id
            and e.batch_capacity >= b.planned_qty
            and (b.required_equipment_id is None or e.equipment_id == b.required_equipment_id)
        ]
        if request.equipment and not eligible_eq and not b.required_equipment_id:
            conflicts.append(
                f"Batch {b.batch_number}: No available equipment in kitchen '{b.kitchen_id}' with capacity >= {b.planned_qty}."
            )

        if request.enforce_food_safety and b.max_holding_minutes > 0:
            earliest_finish = b.earliest_start_minute + duration
            if earliest_finish > b.service_deadline_minute:
                conflicts.append(
                    f"Batch {b.batch_number}: Minimum duration ({duration}m) finishes at {earliest_finish}m after service deadline {b.service_deadline_minute}m."
                )
            elif b.service_deadline_minute - earliest_finish > b.max_holding_minutes and b.earliest_start_minute >= b.service_deadline_minute - duration:
                conflicts.append(
                    f"Batch {b.batch_number}: Cannot satisfy max holding limit {b.max_holding_minutes}m."
                )

    if conflicts:
        return ProductionScheduleResponse(
            status="INFEASIBLE",
            solver_used="scipy_highs_milp",
            conflicting_constraints=conflicts,
            bottlenecks=conflicts[:3],
            assumptions=assumptions,
            alternative_recommendations=[
                "Split oversized batches to fit available equipment batch capacity.",
                "Reschedule or reassign batches to another kitchen with idle equipment capacity.",
                "Resolve ingredient shortages or pending quality checks before releasing batches.",
            ],
        )

    # Discretize candidate start slots (15-min increments) for MILP assignment
    slot_step = 15
    batch_list = sorted(request.batches, key=lambda x: (-x.priority, x.service_deadline_minute))

    # Build feasible (batch_idx, eq_id, staff_id, start_min, finish_min) decision variables
    candidates: list[tuple[int, Optional[str], Optional[str], int, int, float]] = []
    batch_candidate_indices: dict[int, list[int]] = {i: [] for i in range(len(batch_list))}

    for bi, b in enumerate(batch_list):
        duration = max(15, b.prep_minutes + b.cook_minutes + b.cool_rest_minutes)
        eligible_eq: list[Optional[EquipmentResourceInput]] = [
            e
            for e in request.equipment
            if e.is_available
            and e.kitchen_id == b.kitchen_id
            and e.batch_capacity >= b.planned_qty
            and (b.required_equipment_id is None or e.equipment_id == b.required_equipment_id)
        ] or [None]
        eligible_staff: list[Optional[StaffResourceInput]] = [
            s for s in request.staff if s.is_available and s.kitchen_id == b.kitchen_id
        ] or [None]

        # Enforce holding-time window if food safety enabled
        min_start = b.earliest_start_minute
        if request.enforce_food_safety and b.max_holding_minutes > 0:
            min_start = max(min_start, b.service_deadline_minute - duration - b.max_holding_minutes)
        max_start = max(min_start, b.service_deadline_minute - duration)

        for eq in eligible_eq:
            setup = eq.setup_minutes if eq else 0
            clean = eq.cleaning_minutes if eq else 0
            if eq and b.allergens and not eq.allergen_segregated:
                clean = max(clean, request.allergen_segregation_cleaning_minutes)

            eq_start = max(min_start, eq.available_from_minute if eq else 0)
            eq_end = eq.available_until_minute if eq else 1440

            for st in eligible_staff:
                st_start = max(eq_start, st.available_from_minute if st else 0)
                st_end = min(eq_end, st.available_until_minute if st else 1440)
                latest_slot = min(max_start, st_end - duration - clean)
                if st_start > latest_slot:
                    continue
                for start_min in range(st_start, latest_slot + 1, slot_step):
                    finish_min = start_min + duration
                    # Objective cost per candidate
                    holding_gap = max(0, b.service_deadline_minute - finish_min)
                    delay = max(0, finish_min - b.service_deadline_minute)
                    if request.objective == "MIN_EARLY_PRODUCTION":
                        cost = 1.0 + delay * 100.0 + holding_gap * 2.0
                    elif request.objective == "MAX_UTILIZATION":
                        cost = 1.0 + delay * 100.0 + start_min * 0.05
                    else:  # MIN_DELAY
                        cost = 1.0 + delay * 100.0 + start_min * 0.01 + holding_gap * 0.25 + setup * 0.05

                    var_idx = len(candidates)
                    candidates.append(
                        (
                            bi,
                            eq.equipment_id if eq else None,
                            st.user_id if st else None,
                            start_min,
                            finish_min + clean,  # occupied_until includes mandatory cleaning
                            cost,
                        )
                    )
                    batch_candidate_indices[bi].append(var_idx)

        if not batch_candidate_indices[bi]:
            conflicts.append(
                f"Batch {b.batch_number} ({b.recipe_name}): No feasible time window within staff/equipment shifts and holding-time limit ({b.max_holding_minutes}m)."
            )

    if conflicts:
        return ProductionScheduleResponse(
            status="INFEASIBLE",
            solver_used="scipy_highs_milp",
            conflicting_constraints=conflicts,
            bottlenecks=conflicts[:3],
            assumptions=assumptions,
            alternative_recommendations=[
                "Extend kitchen shift availability or add parallel equipment.",
                "Adjust planned start time closer to service deadline to honor holding limits.",
            ],
        )

    num_vars = len(candidates)
    c = np.array([cand[5] for cand in candidates], dtype=float)
    rows: list[np.ndarray] = []
    lbs: list[float] = []
    ubs: list[float] = []

    # 1. Exactly one assignment per batch
    for bi in range(len(batch_list)):
        row = np.zeros(num_vars, dtype=float)
        for vi in batch_candidate_indices[bi]:
            row[vi] = 1.0
        rows.append(row)
        lbs.append(1.0)
        ubs.append(1.0)

    # 2. Equipment exclusivity across discrete 15-min slots
    eq_slots: dict[tuple[str, int], list[int]] = {}
    staff_slots: dict[tuple[str, int], list[int]] = {}
    for vi, (_, eq_id, st_id, start_min, occ_until, _) in enumerate(candidates):
        for t in range(start_min, occ_until, slot_step):
            if eq_id is not None:
                eq_slots.setdefault((eq_id, t), []).append(vi)
            if st_id is not None:
                staff_slots.setdefault((st_id, t), []).append(vi)

    for (_eq_id, _t), vis in eq_slots.items():
        if len(vis) > 1:
            row = np.zeros(num_vars, dtype=float)
            for vi in vis:
                row[vi] = 1.0
            rows.append(row)
            lbs.append(0.0)
            ubs.append(1.0)

    staff_cap = {s.user_id: s.max_concurrent_batches for s in request.staff}
    for (st_id, _t), vis in staff_slots.items():
        cap = staff_cap.get(st_id, 1)
        if len(vis) > cap:
            row = np.zeros(num_vars, dtype=float)
            for vi in vis:
                row[vi] = 1.0
            rows.append(row)
            lbs.append(0.0)
            ubs.append(float(cap))

    # 3. Sub-recipe / dependency precedence (A finishes before B starts)
    batch_idx_by_id = {b.batch_id: idx for idx, b in enumerate(batch_list)}
    for bi, b in enumerate(batch_list):
        for dep_id in b.depends_on_batch_ids:
            di = batch_idx_by_id.get(dep_id)
            if di is None:
                continue
            # Expected finish(dep) <= start(b) using big-M linearization per candidate pair
            # Since variables are few in operational plans, pairwise conflict exclusion suffices cleanly
            for vi_dep in batch_candidate_indices[di]:
                dep_finish = candidates[vi_dep][4]
                for vi_cur in batch_candidate_indices[bi]:
                    cur_start = candidates[vi_cur][3]
                    if cur_start < dep_finish:
                        row = np.zeros(num_vars, dtype=float)
                        row[vi_dep] = 1.0
                        row[vi_cur] = 1.0
                        rows.append(row)
                        lbs.append(0.0)
                        ubs.append(1.0)

    A = np.vstack(rows)
    res = milp(
        c=c,
        integrality=np.ones(num_vars, dtype=int),
        bounds=Bounds(np.zeros(num_vars), np.ones(num_vars)),
        constraints=LinearConstraint(A, np.array(lbs), np.array(ubs)),
    )

    if not res.success or res.x is None:
        return ProductionScheduleResponse(
            status="INFEASIBLE",
            solver_used="scipy_highs_milp",
            conflicting_constraints=[
                "Equipment or staff concurrency limits prevent scheduling all batches before their service deadlines."
            ],
            bottlenecks=["Kitchen resource contention across overlapping meal/event deadlines."],
            assumptions=assumptions,
            alternative_recommendations=[
                "Add parallel kitchen/equipment capacity or stagger event service windows.",
            ],
        )

    assignments: list[ScheduledBatchAssignment] = []
    eq_busy: dict[str, int] = {e.equipment_id: 0 for e in request.equipment}
    st_busy: dict[str, int] = {s.user_id: 0 for s in request.staff}

    for vi, val in enumerate(res.x):
        if val > 0.5:
            bi, eq_id, st_id, start_min, occ_until, _ = candidates[vi]
            b = batch_list[bi]
            duration = max(15, b.prep_minutes + b.cook_minutes + b.cool_rest_minutes)
            finish_min = start_min + duration
            if eq_id and eq_id in eq_busy:
                eq_busy[eq_id] += occ_until - start_min
            if st_id and st_id in st_busy:
                st_busy[st_id] += duration
            assignments.append(
                ScheduledBatchAssignment(
                    batch_id=b.batch_id,
                    batch_number=b.batch_number,
                    recipe_name=b.recipe_name,
                    kitchen_id=b.kitchen_id,
                    workstation_type=b.workstation_type,
                    assigned_equipment_id=eq_id,
                    assigned_staff_id=st_id,
                    planned_start_minute=start_min,
                    planned_finish_minute=finish_min,
                    service_deadline_minute=b.service_deadline_minute,
                    delay_minutes=max(0, finish_min - b.service_deadline_minute),
                    holding_buffer_minutes=max(0, b.service_deadline_minute - finish_min),
                    status="SCHEDULED",
                )
            )

    eq_util = {}
    for e in request.equipment:
        avail = max(1, e.available_until_minute - e.available_from_minute)
        pct = _round2(min(100.0, (eq_busy.get(e.equipment_id, 0) / avail) * 100.0))
        eq_util[e.equipment_id] = pct
        if pct >= 85.0:
            bottlenecks.append(f"Equipment '{e.name}' utilization is high ({pct}%).")

    st_util = {}
    for s in request.staff:
        avail = max(1, s.available_until_minute - s.available_from_minute)
        pct = _round2(min(100.0, (st_busy.get(s.user_id, 0) / avail) * 100.0))
        st_util[s.user_id] = pct

    assignments.sort(key=lambda a: a.planned_start_minute)
    return ProductionScheduleResponse(
        status="OPTIMAL",
        solver_used="scipy_highs_milp",
        assignments=assignments,
        equipment_utilization_pct=eq_util,
        staff_utilization_pct=st_util,
        bottlenecks=bottlenecks,
        assumptions=assumptions,
    )


def allocate_central_kitchen_batch(request: CentralKitchenAllocateRequest) -> CentralKitchenAllocateResponse:
    """
    Priority-ordered deterministic central kitchen allocation.
    Strictly prevents allocating more than `available_batch_output_qty`.
    """
    remaining = Decimal(str(request.available_batch_output_qty))
    total_req = sum((Decimal(str(r.requested_qty)) for r in request.requests), Decimal("0"))
    sorted_reqs = sorted(request.requests, key=lambda r: (-r.priority, r.outlet_name))
    allocations: list[CentralKitchenOutletAllocation] = []
    total_alloc = Decimal("0")

    for r in sorted_reqs:
        req_qty = Decimal(str(r.requested_qty))
        alloc = min(req_qty, remaining)
        remaining -= alloc
        total_alloc += alloc
        allocations.append(
            CentralKitchenOutletAllocation(
                outlet_id=r.outlet_id,
                outlet_name=r.outlet_name,
                requested_qty=float(req_qty),
                allocated_qty=float(alloc),
                remaining_unfilled_qty=float(req_qty - alloc),
            )
        )

    shortage_warning: Optional[str] = None
    if total_req > Decimal(str(request.available_batch_output_qty)):
        shortage_warning = (
            f"Requested {float(total_req)} exceeds available batch output {request.available_batch_output_qty}. "
            "Trigger replenishment batch or review outlet allocations."
        )

    return CentralKitchenAllocateResponse(
        batch_id=request.batch_id,
        total_available_qty=request.available_batch_output_qty,
        total_requested_qty=float(total_req),
        total_allocated_qty=float(total_alloc),
        allocations=allocations,
        shortage_warning=shortage_warning,
    )
