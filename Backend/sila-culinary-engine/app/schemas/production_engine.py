"""Versioned Pydantic v2 contracts for SILA Culinary Phase 4A Production Planning & Scheduling."""

from __future__ import annotations

from typing import Literal, Optional

from pydantic import BaseModel, Field


class EquipmentResourceInput(BaseModel):
    equipment_id: str
    name: str
    kitchen_id: str
    workstation_type: str = "COOKING"
    batch_capacity: float = Field(default=50.0, gt=0)
    setup_minutes: int = Field(default=10, ge=0)
    cleaning_minutes: int = Field(default=15, ge=0)
    available_from_minute: int = Field(default=0, ge=0)
    available_until_minute: int = Field(default=960, gt=0)
    is_available: bool = True
    allergen_segregated: bool = False


class StaffResourceInput(BaseModel):
    user_id: str
    name: str
    role: str = "CHEF_DE_PARTIE"
    kitchen_id: str
    workstation_type: str = "COOKING"
    available_from_minute: int = Field(default=0, ge=0)
    available_until_minute: int = Field(default=960, gt=0)
    max_concurrent_batches: int = Field(default=2, ge=1)
    is_available: bool = True


class ProductionBatchTaskInput(BaseModel):
    batch_id: str
    batch_number: str
    production_order_id: str
    recipe_name: str
    kitchen_id: str
    workstation_type: str = "COOKING"
    required_equipment_id: Optional[str] = None
    planned_qty: float = Field(gt=0)
    prep_minutes: int = Field(default=20, ge=0)
    cook_minutes: int = Field(default=30, ge=0)
    cool_rest_minutes: int = Field(default=0, ge=0)
    service_deadline_minute: int = Field(default=720, gt=0)
    earliest_start_minute: int = Field(default=0, ge=0)
    max_holding_minutes: int = Field(default=120, ge=0)
    ingredients_ready: bool = True
    allergens: list[str] = Field(default_factory=list)
    depends_on_batch_ids: list[str] = Field(default_factory=list)
    priority: int = Field(default=50, ge=1, le=100)


class ProductionScheduleRequest(BaseModel):
    contract_version: str = "v4a.1"
    objective: Literal["MIN_DELAY", "MAX_UTILIZATION", "MIN_EARLY_PRODUCTION"] = "MIN_DELAY"
    enforce_food_safety: bool = True
    allergen_segregation_cleaning_minutes: int = Field(default=20, ge=0)
    equipment: list[EquipmentResourceInput] = Field(default_factory=list)
    staff: list[StaffResourceInput] = Field(default_factory=list)
    batches: list[ProductionBatchTaskInput] = Field(default_factory=list)


class ScheduledBatchAssignment(BaseModel):
    batch_id: str
    batch_number: str
    recipe_name: str
    kitchen_id: str
    workstation_type: str
    assigned_equipment_id: Optional[str] = None
    assigned_staff_id: Optional[str] = None
    planned_start_minute: int
    planned_finish_minute: int
    service_deadline_minute: int
    delay_minutes: int = 0
    holding_buffer_minutes: int = 0
    status: str = "SCHEDULED"


class ProductionScheduleResponse(BaseModel):
    contract_version: str = "v4a.1"
    status: Literal["OPTIMAL", "FEASIBLE", "INFEASIBLE"]
    solver_used: str = "scipy_highs_milp"
    assignments: list[ScheduledBatchAssignment] = Field(default_factory=list)
    equipment_utilization_pct: dict[str, float] = Field(default_factory=dict)
    staff_utilization_pct: dict[str, float] = Field(default_factory=dict)
    bottlenecks: list[str] = Field(default_factory=list)
    conflicting_constraints: list[str] = Field(default_factory=list)
    assumptions: list[str] = Field(default_factory=list)
    alternative_recommendations: list[str] = Field(default_factory=list)


class CentralKitchenOutletRequest(BaseModel):
    outlet_id: str
    outlet_name: str
    requested_qty: float = Field(gt=0)
    priority: int = Field(default=50, ge=1, le=100)


class CentralKitchenAllocateRequest(BaseModel):
    contract_version: str = "v4a.1"
    batch_id: str
    available_batch_output_qty: float = Field(ge=0)
    requests: list[CentralKitchenOutletRequest] = Field(default_factory=list)


class CentralKitchenOutletAllocation(BaseModel):
    outlet_id: str
    outlet_name: str
    requested_qty: float
    allocated_qty: float
    remaining_unfilled_qty: float


class CentralKitchenAllocateResponse(BaseModel):
    contract_version: str = "v4a.1"
    batch_id: str
    total_available_qty: float
    total_requested_qty: float
    total_allocated_qty: float
    allocations: list[CentralKitchenOutletAllocation] = Field(default_factory=list)
    shortage_warning: Optional[str] = None
