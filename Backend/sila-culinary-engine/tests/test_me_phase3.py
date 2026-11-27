from __future__ import annotations

from fastapi.testclient import TestClient
from app.main import app


client = TestClient(app)


def test_dependency_graph_reverse_price_shock_and_cycle_detection() -> None:
    resp = client.post(
        "/v1/me-twin/dependency-graph",
        json={
            "tenant_id": "t1",
            "nodes": [
                {"node_id": "ing_salmon", "node_type": "INGREDIENT", "label": "Atlantic Salmon", "unit_cost": 50.0},
                {"node_id": "rec_salmon", "node_type": "RECIPE", "label": "Grilled Salmon Recipe"},
                {"node_id": "item_salmon", "node_type": "MENU_ITEM", "label": "Grilled Salmon", "planned_covers": 100},
                {"node_id": "menu_dinner", "node_type": "MENU", "label": "Dinner Buffet", "planned_covers": 100},
            ],
            "edges": [
                {"source_id": "ing_salmon", "target_id": "rec_salmon", "quantity": 0.2, "yield_percent": 100.0},
                {"source_id": "rec_salmon", "target_id": "item_salmon", "quantity": 1.0},
                {"source_id": "item_salmon", "target_id": "menu_dinner", "quantity": 1.0},
            ],
            "shock_node_id": "ing_salmon",
            "price_change_percent": 20.0,
        },
    )
    assert resp.status_code == 200
    data = resp.json()
    assert data["is_acyclic"] is True
    assert "rec_salmon" in data["affected_recipes"]
    assert "item_salmon" in data["affected_menu_items"]
    assert "menu_dinner" in data["affected_menus"]
    assert data["total_cost_impact"] == 200.0  # 0.2 * 50 * 20% = 2.0 per portion * 100 covers

    # Circular recipe dependency detection
    cyc_resp = client.post(
        "/v1/me-twin/dependency-graph",
        json={
            "tenant_id": "t1",
            "nodes": [
                {"node_id": "sub_a", "node_type": "SUB_RECIPE", "label": "Stock A"},
                {"node_id": "sub_b", "node_type": "SUB_RECIPE", "label": "Sauce B"},
            ],
            "edges": [
                {"source_id": "sub_a", "target_id": "sub_b", "quantity": 1.0},
                {"source_id": "sub_b", "target_id": "sub_a", "quantity": 1.0},
            ],
        },
    )
    assert cyc_resp.status_code == 200
    cyc = cyc_resp.json()
    assert cyc["is_acyclic"] is False
    assert any(i["issue_type"] == "CIRCULAR_DEPENDENCY" for i in cyc["integrity_issues"])


def test_ortools_cpsat_buffet_and_infeasibility_no_silent_relaxation() -> None:
    candidates = []
    for i in range(5):
        candidates.append(
            {
                "dish_id": f"st_{i}",
                "name": f"Starter {i}",
                "category": "Starter",
                "selling_price": 150.0,
                "ingredient_cost_per_portion": 8.0 + i,
                "predicted_popularity": 0.7,
                "prep_minutes_per_batch": 15.0,
                "batch_size_portions": 50.0,
            }
        )
    for i in range(7):
        candidates.append(
            {
                "dish_id": f"mc_{i}",
                "name": f"Main {i}",
                "category": "Main Course",
                "selling_price": 150.0,
                "ingredient_cost_per_portion": 18.0 + i,
                "predicted_popularity": 0.75,
                "prep_minutes_per_batch": 20.0,
                "batch_size_portions": 50.0,
            }
        )
    for i in range(4):
        candidates.append(
            {
                "dish_id": f"ds_{i}",
                "name": f"Dessert {i}",
                "category": "Dessert",
                "selling_price": 150.0,
                "ingredient_cost_per_portion": 6.0 + i,
                "predicted_popularity": 0.65,
                "prep_minutes_per_batch": 10.0,
                "batch_size_portions": 50.0,
            }
        )

    # Feasible buffet: 500 guests, AED 150, max 30% FC, 4 starters, 6 mains, 3 desserts
    ok_resp = client.post(
        "/v1/me-twin/optimize-multi-objective",
        json={
            "mode": "PARETO_FRONTIER",
            "menu_format": "BUFFET",
            "guest_count": 500,
            "target_selling_price": 150.0,
            "max_food_cost_percent": 30.0,
            "min_total_items": 13,
            "max_total_items": 13,
            "category_counts": {"Starter": 4, "Main Course": 6, "Dessert": 3},
            "available_production_minutes": 1800.0,
            "candidates": candidates,
        },
    )
    assert ok_resp.status_code == 200
    ok_data = ok_resp.json()
    assert ok_data["status"] in ("OPTIMAL", "FEASIBLE")
    assert len(ok_data["solutions"]) >= 1
    for sol in ok_data["solutions"]:
        assert sol["food_cost_percent"] <= 30.0
        assert sol["hard_constraints_satisfied"] is True
        assert len(sol["selected_dishes"]) == 13

    # Infeasible production capacity: 10 mins available -> must NOT relax hard constraint
    bad_resp = client.post(
        "/v1/me-twin/optimize-multi-objective",
        json={
            "mode": "SINGLE_OBJECTIVE",
            "menu_format": "BUFFET",
            "guest_count": 500,
            "target_selling_price": 150.0,
            "max_food_cost_percent": 30.0,
            "min_total_items": 13,
            "max_total_items": 13,
            "category_counts": {"Starter": 4, "Main Course": 6, "Dessert": 3},
            "available_production_minutes": 10.0,
            "candidates": candidates,
        },
    )
    assert bad_resp.status_code == 200
    bad_data = bad_resp.json()
    assert bad_data["status"] == "INFEASIBLE"
    assert len(bad_data["solutions"]) == 0
    assert len(bad_data["infeasibility_reasons"]) >= 1


def test_inventory_driven_menu_excludes_reserved_and_blocked_stock() -> None:
    resp = client.post(
        "/v1/me-twin/optimize-inventory-menu",
        json={
            "guest_count": 100,
            "min_items": 2,
            "max_items": 2,
            "inventory_lots": [
                {
                    "ingredient_id": "ing_chicken",
                    "ingredient_name": "Chicken Breast",
                    "eligible_qty": 40.0,
                    "reserved_qty": 25.0,
                    "blocked_qty": 10.0,
                    "expired_qty": 5.0,
                    "days_to_expiry": 2,
                    "unit_cost": 22.0,
                },
                {
                    "ingredient_id": "ing_rice",
                    "ingredient_name": "Basmati Rice",
                    "eligible_qty": 30.0,
                    "reserved_qty": 0.0,
                    "blocked_qty": 0.0,
                    "unit_cost": 8.0,
                },
            ],
            "candidates": [
                {
                    "dish_id": "d1",
                    "name": "Chicken Biryani",
                    "category": "Main Course",
                    "selling_price": 65.0,
                    "ingredient_cost_per_portion": 14.0,
                    "ingredient_requirements": {"ing_chicken": 0.25, "ing_rice": 0.15},
                },
                {
                    "dish_id": "d2",
                    "name": "Herb Roasted Chicken",
                    "category": "Main Course",
                    "selling_price": 70.0,
                    "ingredient_cost_per_portion": 16.0,
                    "ingredient_requirements": {"ing_chicken": 0.30},
                },
            ],
        },
    )
    assert resp.status_code == 200
    data = resp.json()
    assert data["excluded_stock_summary"]["reserved_qty"] == 25.0
    assert data["excluded_stock_summary"]["blocked_qty"] == 10.0
    assert data["excluded_stock_summary"]["expired_qty"] == 5.0
    assert data["inventory_utilization_percent"] > 0.0
    assert data["expiry_avoidance_value"] > 0.0


def test_hierarchical_forecast_cold_start_and_waste() -> None:
    hf = client.post(
        "/v1/me-twin/forecast-hierarchical",
        json={
            "horizon_days": 5,
            "series": [
                {"node_key": "prop_1", "level": "PROPERTY", "history": [100, 105, 110, 108, 112, 115, 118, 120, 122, 125]},
                {"node_key": "out_1", "level": "OUTLET", "parent_key": "prop_1", "history": [60, 63, 66, 65, 67, 69, 71, 72, 73, 75]},
                {"node_key": "out_2", "level": "OUTLET", "parent_key": "prop_1", "history": [40, 42, 44, 43, 45, 46, 47, 48, 49, 50]},
                {"node_key": "sparse_item", "level": "MENU_ITEM", "history": [0, 0, 5, 0, 0, 4, 0, 0, 6, 0]},
            ],
        },
    )
    assert hf.status_code == 200
    hf_data = hf.json()
    assert hf_data["leakage_checked"] is True
    assert hf_data["consistency_max_gap"] <= 0.05

    cs = client.post(
        "/v1/me-twin/predict-cold-start",
        json={
            "candidate": {
                "dish_id": "new_1",
                "name": "Truffle Risotto",
                "cuisine": "ITALIAN",
                "category": "Main Course",
                "selling_price": 95.0,
                "ingredients": ["arborio rice", "truffle", "parmesan"],
            },
            "historical_catalog": [
                {
                    "dish_id": "old_1",
                    "name": "Mushroom Risotto",
                    "cuisine": "ITALIAN",
                    "category": "Main Course",
                    "selling_price": 85.0,
                    "ingredients": ["arborio rice", "mushroom", "parmesan"],
                    "historical_daily_covers": 34.0,
                    "historical_margin_percent": 74.0,
                }
            ],
        },
    )
    assert cs.status_code == 200
    assert cs.json()["is_cold_start_estimate"] is True
    assert cs.json()["expected_daily_demand"] > 0

    w = client.post(
        "/v1/me-twin/predict-waste",
        json={
            "items": [
                {
                    "item_id": "w1",
                    "name": "Buffet Salmon",
                    "planned_production_qty": 100.0,
                    "expected_consumption_qty": 80.0,
                    "unit_cost": 40.0,
                    "food_safety_reusable": False,
                }
            ]
        },
    )
    assert w.status_code == 200
    assert w.json()["yield_and_waste_separated"] is True
    assert w.json()["food_safety_priority_enforced"] is True


def test_monte_carlo_reproducibility_tornado_stress_and_unsafe_substitution() -> None:
    mc1 = client.post("/v1/me-twin/simulate-monte-carlo", json={"seed": 777, "runs": 500, "guest_count_mean": 300})
    mc2 = client.post("/v1/me-twin/simulate-monte-carlo", json={"seed": 777, "runs": 500, "guest_count_mean": 300})
    assert mc1.status_code == 200 and mc2.status_code == 200
    assert mc1.json()["expected_cost"] == mc2.json()["expected_cost"]
    assert mc1.json()["margin_p10"] == mc2.json()["margin_p10"]

    sens = client.post(
        "/v1/me-twin/sensitivity",
        json={
            "baseline_revenue": 45000.0,
            "baseline_cost": 13000.0,
            "drivers": [
                {"variable_name": "Salmon Price", "category": "INGREDIENT_PRICE", "baseline_value": 50.0, "low_value": 42.0, "high_value": 65.0, "baseline_cost_impact_per_unit": 150.0},
                {"variable_name": "Guest Count", "category": "GUEST_COUNT", "baseline_value": 300.0, "low_value": 240.0, "high_value": 360.0, "baseline_revenue_impact_per_unit": 150.0, "baseline_cost_impact_per_unit": 42.0},
            ],
        },
    )
    assert sens.status_code == 200
    assert sens.json()["tornado"][0]["variable_name"] == "Guest Count"

    st = client.post(
        "/v1/me-twin/stress-test",
        json={
            "baseline_guests": 300,
            "ingredient_buffer_days": 2,
            "scenarios": [
                {"stress_type": "SUPPLIER_DELAY", "label": "4-Day Port Delay", "supplier_delay_days": 4}
            ],
        },
    )
    assert st.status_code == 200
    assert st.json()["outcomes"][0]["shortage_risk"] == "CRITICAL"
    assert st.json()["outcomes"][0]["is_operationally_feasible"] is False

    sub = client.post(
        "/v1/me-twin/substitution-cascade",
        json={
            "original_ingredient": "Olive Oil",
            "original_qty": 2.0,
            "original_unit_cost": 25.0,
            "recipe_selling_price": 120.0,
            "excluded_allergens": ["PEANUT"],
            "candidates": [
                {"substitute_id": "s_bad", "substitute_name": "Peanut Oil", "is_approved_relationship": False, "unit_cost": 14.0, "allergens": ["PEANUT"]},
                {"substitute_id": "s_ok", "substitute_name": "Sunflower Oil", "is_approved_relationship": True, "unit_cost": 15.0},
            ],
        },
    )
    assert sub.status_code == 200
    res = sub.json()["results"]
    bad = next(r for r in res if r["substitute_id"] == "s_bad")
    ok = next(r for r in res if r["substitute_id"] == "s_ok")
    assert bad["category"] == "REJECTED_UNSAFE"
    assert bad["is_safe"] is False
    assert ok["category"] == "APPROVED_SUBSTITUTION"
    assert ok["is_safe"] is True
