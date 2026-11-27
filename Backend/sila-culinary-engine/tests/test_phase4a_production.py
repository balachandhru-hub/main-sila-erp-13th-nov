"""Phase 4A Kitchen Production Scheduling & Central Kitchen Allocation tests."""

from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def test_feasible_milp_production_schedule_respects_dependencies_and_equipment():
    res = client.post(
        "/v1/production/schedule",
        json={
            "objective": "MIN_DELAY",
            "enforce_food_safety": True,
            "equipment": [
                {
                    "equipment_id": "EQ-OVEN-1",
                    "name": "Rational Combi Oven 1",
                    "kitchen_id": "KIT-MAIN",
                    "workstation_type": "COOKING",
                    "batch_capacity": 60.0,
                    "setup_minutes": 10,
                    "cleaning_minutes": 15,
                    "available_from_minute": 420,
                    "available_until_minute": 1140,
                    "is_available": True,
                }
            ],
            "staff": [
                {
                    "user_id": "CHEF-1",
                    "name": "Chef de Partie",
                    "kitchen_id": "KIT-MAIN",
                    "available_from_minute": 420,
                    "available_until_minute": 1140,
                    "max_concurrent_batches": 1,
                    "is_available": True,
                }
            ],
            "batches": [
                {
                    "batch_id": "B-STOCK",
                    "batch_number": "BAT-001",
                    "production_order_id": "PO-1",
                    "recipe_name": "Seafood Veloute Base",
                    "kitchen_id": "KIT-MAIN",
                    "planned_qty": 40.0,
                    "prep_minutes": 15,
                    "cook_minutes": 30,
                    "service_deadline_minute": 720,
                    "earliest_start_minute": 420,
                    "max_holding_minutes": 240,
                    "ingredients_ready": True,
                    "priority": 90,
                },
                {
                    "batch_id": "B-MAIN",
                    "batch_number": "BAT-002",
                    "production_order_id": "PO-1",
                    "recipe_name": "Poached Salmon Gala",
                    "kitchen_id": "KIT-MAIN",
                    "planned_qty": 50.0,
                    "prep_minutes": 15,
                    "cook_minutes": 30,
                    "service_deadline_minute": 780,
                    "earliest_start_minute": 420,
                    "max_holding_minutes": 120,
                    "ingredients_ready": True,
                    "depends_on_batch_ids": ["B-STOCK"],
                    "priority": 80,
                },
            ],
        },
    )
    assert res.status_code == 200
    data = res.json()
    assert data["status"] == "OPTIMAL"
    assert len(data["assignments"]) == 2
    by_id = {a["batch_id"]: a for a in data["assignments"]}
    assert by_id["B-MAIN"]["planned_start_minute"] >= by_id["B-STOCK"]["planned_finish_minute"]
    assert by_id["B-MAIN"]["delay_minutes"] == 0


def test_infeasible_schedule_when_food_safety_or_capacity_violated():
    res = client.post(
        "/v1/production/schedule",
        json={
            "enforce_food_safety": True,
            "equipment": [
                {
                    "equipment_id": "EQ-OVEN-1",
                    "name": "Small Oven",
                    "kitchen_id": "KIT-MAIN",
                    "batch_capacity": 30.0,
                    "available_from_minute": 480,
                    "available_until_minute": 900,
                    "is_available": True,
                }
            ],
            "batches": [
                {
                    "batch_id": "B-BIG",
                    "batch_number": "BAT-999",
                    "production_order_id": "PO-9",
                    "recipe_name": "Banquet Lamb Roast",
                    "kitchen_id": "KIT-MAIN",
                    "planned_qty": 80.0,
                    "prep_minutes": 20,
                    "cook_minutes": 60,
                    "service_deadline_minute": 780,
                    "earliest_start_minute": 480,
                    "max_holding_minutes": 90,
                    "ingredients_ready": False,
                }
            ],
        },
    )
    assert res.status_code == 200
    data = res.json()
    assert data["status"] == "INFEASIBLE"
    assert any("Ingredients are not ready" in c for c in data["conflicting_constraints"])
    assert any("capacity" in c.lower() for c in data["conflicting_constraints"])


def test_central_kitchen_allocation_never_exceeds_batch_output():
    res = client.post(
        "/v1/production/central-kitchen-allocate",
        json={
            "batch_id": "BAT-CK-10",
            "available_batch_output_qty": 100.0,
            "requests": [
                {"outlet_id": "OUT-1", "outlet_name": "Main Dining", "requested_qty": 70.0, "priority": 90},
                {"outlet_id": "OUT-2", "outlet_name": "Pool Bar", "requested_qty": 60.0, "priority": 60},
            ],
        },
    )
    assert res.status_code == 200
    data = res.json()
    assert data["total_allocated_qty"] == 100.0
    assert data["allocations"][0]["allocated_qty"] == 70.0
    assert data["allocations"][1]["allocated_qty"] == 30.0
    assert data["allocations"][1]["remaining_unfilled_qty"] == 30.0
    assert data["shortage_warning"] is not None
