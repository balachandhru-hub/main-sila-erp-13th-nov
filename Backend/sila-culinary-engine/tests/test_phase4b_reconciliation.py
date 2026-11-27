from __future__ import annotations

import io
from openpyxl import load_workbook
from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def test_scenario_1_2_3_material_reconciliation_equations():
    # Scenario 1: 100 issued, 80 usable, 5 waste, 15 return -> Balanced
    # Scenario 2: 100 + 10 addl, 85 usable, 10 waste, 15 return -> Balanced
    # Scenario 3: 100 issued, 80 usable, 5 waste, 10 return -> 5 unexplained -> Blocked
    res = client.post(
        "/v1/reconciliation/materials",
        json={
            "production_order_code": "PRD-ORD000010",
            "production_batch_number": "PRD-ORD000010-B01",
            "lines": [
                {
                    "material_code": "MAT-S1",
                    "material_name": "Beef Tenderloin",
                    "inventory_batch": "LOT-A1",
                    "uom": "KG",
                    "planned_qty": 100,
                    "original_issued_qty": 100,
                    "usable_consumption_qty": 80,
                    "separate_ingredient_waste_qty": 5,
                    "unused_returned_qty": 15,
                    "tolerance_percent": 2.0,
                },
                {
                    "material_code": "MAT-S2",
                    "material_name": "Basmati Rice",
                    "inventory_batch": "LOT-B1",
                    "uom": "KG",
                    "planned_qty": 100,
                    "original_issued_qty": 100,
                    "additional_issued_qty": 10,
                    "usable_consumption_qty": 85,
                    "separate_ingredient_waste_qty": 10,
                    "unused_returned_qty": 15,
                    "tolerance_percent": 2.0,
                },
                {
                    "material_code": "MAT-S3",
                    "material_name": "Atlantic Salmon",
                    "inventory_batch": "LOT-C1",
                    "uom": "KG",
                    "planned_qty": 100,
                    "original_issued_qty": 100,
                    "usable_consumption_qty": 80,
                    "separate_ingredient_waste_qty": 5,
                    "unused_returned_qty": 10,
                    "tolerance_percent": 2.0,
                },
            ],
        },
    )
    assert res.status_code == 200
    body = res.json()
    assert body["settlement_blocked"] is True
    assert body["overall_status"] == "REQUIRES_INVESTIGATION"
    assert body["lines"][0]["status"] == "BALANCED"
    assert body["lines"][0]["unexplained_qty"] == 0.0
    assert body["lines"][1]["net_issued_qty"] == 110.0
    assert body["lines"][1]["status"] == "BALANCED"
    assert body["lines"][2]["unexplained_qty"] == 5.0
    assert body["lines"][2]["settlement_blocked"] is True


def test_consumption_includes_waste_not_double_counted_and_reversal():
    res = client.post(
        "/v1/reconciliation/materials",
        json={
            "lines": [
                {
                    "material_code": "MAT-NO-DBL",
                    "uom": "KG",
                    "original_issued_qty": 100,
                    "reversed_issue_qty": 10,
                    "usable_consumption_qty": 90,
                    "consumption_includes_waste": True,
                    "separate_ingredient_waste_qty": 5,
                    "tolerance_percent": 2.0,
                }
            ]
        },
    )
    assert res.status_code == 200
    line = res.json()["lines"][0]
    assert line["net_issued_qty"] == 90.0
    assert line["effective_waste_qty"] == 0.0
    assert line["accounted_qty"] == 90.0
    assert line["unexplained_qty"] == 0.0
    assert line["status"] == "BALANCED"


def test_yield_variance_allows_hydration_over_100_and_rejects_incompatible_uom():
    ok_res = client.post(
        "/v1/reconciliation/yield-variance",
        json={
            "recipe_code": "RI00001",
            "gross_input_qty": 10.0,
            "input_uom": "KG",
            "accepted_usable_output_qty": 14.5,
            "output_uom": "KG",
            "standard_yield_percent": 140.0,
            "unit_cost": 12.0,
        },
    )
    assert ok_res.status_code == 200
    ok_body = ok_res.json()
    assert ok_body["comparable_uom"] is True
    assert ok_body["actual_yield_percent"] == 145.0
    assert ok_body["yield_variance_percent"] == 5.0

    bad_res = client.post(
        "/v1/reconciliation/yield-variance",
        json={
            "recipe_code": "RI00002",
            "gross_input_qty": 10.0,
            "input_uom": "KG",
            "accepted_usable_output_qty": 20.0,
            "output_uom": "L",
            "standard_yield_percent": 85.0,
        },
    )
    assert bad_res.status_code == 200
    assert bad_res.json()["comparable_uom"] is False
    assert bad_res.json()["actual_yield_percent"] is None


def test_scenario_6_portion_variance_and_bridges():
    portion = client.post(
        "/v1/reconciliation/portion-variance",
        json={
            "recipe_code": "RI00010",
            "standard_portion_weight_g": 200.0,
            "sampled_weights_g": [218.0, 222.0, 220.0],
            "planned_portions": 500,
            "actual_portions": 450,
            "total_usable_output_g": 99000,
            "standard_portion_cost": 15.0,
        },
    )
    assert portion.status_code == 200
    p_body = portion.json()
    assert p_body["average_actual_weight_g"] == 220.0
    assert p_body["over_portioning_percent"] == 10.0
    assert "3 sampled portions" in p_body["sampling_basis_note"]

    bridge = client.post(
        "/v1/reconciliation/cost-bridge",
        json={
            "planned_cost": 10000.0,
            "actual_cost": 10850.0,
            "planning_volume_variance": -400.0,
            "ingredient_usage_variance": 650.0,
            "ingredient_price_variance": 300.0,
            "labor_efficiency_variance": 300.0,
            "explanatory_yield_impact": 400.0,
            "explanatory_waste_impact": 250.0,
        },
    )
    assert bridge.status_code == 200
    b_body = bridge.json()
    assert b_body["reconciled_exactly"] is True
    assert b_body["total_cost_variance"] == 850.0

    evt_bridge = client.post(
        "/v1/reconciliation/event-profit-bridge",
        json={
            "baseline_profit": 25000.0,
            "actual_profit": 23400.0,
            "guest_count_impact": -2000.0,
            "ingredient_price_impact": -500.0,
            "ingredient_usage_impact": 900.0,
            "explanatory_waste_driver": 450.0,
        },
    )
    assert evt_bridge.status_code == 200
    assert evt_bridge.json()["reconciled_exactly"] is True


def test_investigation_and_formula_safe_workbook_export():
    inv = client.post(
        "/v1/reconciliation/investigate",
        json={
            "property_name": "Grand Hotel",
            "currency": "AED",
            "items": [
                {
                    "reference_code": "PRD-B01",
                    "category": "PORTION_VARIANCE",
                    "metric_name": "Portion Weight",
                    "expected_value": 200.0,
                    "actual_value": 225.0,
                    "historical_baseline": 202.0,
                    "financial_impact": 850.0,
                }
            ],
        },
    )
    assert inv.status_code == 200
    assert len(inv.json()["findings"]) == 1

    wb_res = client.post(
        "/v1/reconciliation/export-workbook",
        json={
            "title": "Phase 4B Reports",
            "currency": "AED",
            "sheets": [
                {
                    "sheet_name": "01_Actual_Consumption",
                    "columns": ["Batch", "Ingredient", "FormulaCheck"],
                    "rows": [["B01", "Beef", "=HYPERLINK(\"http://evil\")"]],
                }
            ],
        },
    )
    assert wb_res.status_code == 200
    wb = load_workbook(io.BytesIO(wb_res.content))
    assert wb["01_Actual_Consumption"]["C2"].value == "'=HYPERLINK(\"http://evil\")"
