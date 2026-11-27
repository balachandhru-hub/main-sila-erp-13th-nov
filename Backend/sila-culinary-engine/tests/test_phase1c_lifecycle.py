from __future__ import annotations

from decimal import Decimal
from fastapi.testclient import TestClient

from app.main import app


client = TestClient(app)


def _butter_chicken_50_payload() -> dict:
    return {
        "tenant_id": "test-tenant-1c",
        "recipe_id_or_ref": "RCP-BUTTER-CHICKEN",
        "servings": "50",
        "reporting_currency": "AED",
        "tax_basis": "TAX_EXCLUSIVE",
        "selling_price_per_portion": "65",
        "target_food_cost_percent": "28",
        "ingredients": [
            {"temp_ref": "I-1", "description": "Chicken breast", "material_code": "0001001", "recipe_qty": "9", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "24"},
            {"temp_ref": "I-2", "description": "Tomato puree", "material_code": "0001002", "recipe_qty": "6", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "12"},
            {"temp_ref": "I-3", "description": "Unsalted butter", "material_code": "0001003", "recipe_qty": "1.25", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "32"},
            {"temp_ref": "I-4", "description": "Fresh cream", "material_code": "0001004", "recipe_qty": "2", "recipe_uom": "L", "costing_uom": "L", "unit_price": "18"},
            {"temp_ref": "I-5", "description": "Ginger garlic paste", "material_code": "0001005", "recipe_qty": "0.6", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "18"},
            {"temp_ref": "I-6", "description": "Yogurt", "material_code": "0001006", "recipe_qty": "1.5", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "8"},
            {"temp_ref": "I-7", "description": "Garam masala", "material_code": "0001007", "recipe_qty": "0.125", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "45"},
            {"temp_ref": "I-8", "description": "Chilli powder", "material_code": "0001008", "recipe_qty": "0.1", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "40"},
            {"temp_ref": "I-9", "description": "Kasuri methi", "material_code": "0001009", "recipe_qty": "0.05", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "60"},
            {"temp_ref": "I-10", "description": "Salt", "material_code": "0001010", "recipe_qty": "0.125", "recipe_uom": "KG", "costing_uom": "KG", "unit_price": "3"},
        ],
        "additional_costs": [
            {"code": "OPEX", "name": "Direct Operating Cost", "category": "DIRECT_OPERATING", "basis": "PER_PORTION", "amount": "8"}
        ],
    }


def test_butter_chicken_50_servings_exact_spec_values():
    res = client.post("/v1/costing/calculate", json=_butter_chicken_50_payload())
    assert res.status_code == 200, res.text
    data = res.json()
    assert Decimal(data["total_ingredient_cost"]) == Decimal("399.8000")
    assert Decimal(data["ingredient_cost_per_portion"]) == Decimal("7.9960")
    assert Decimal(data["estimated_revenue"]) == Decimal("3250.0000")
    assert Decimal(data["food_cost_percent"]) == Decimal("12.30")
    assert Decimal(data["contribution_amount"]) == Decimal("2450.2000")
    assert data["cost_completeness"] == "COMPLETE"


def test_price_unit_conversion_and_edible_yield_no_double_loss():
    payload = {
        "tenant_id": "test-tenant-1c",
        "recipe_id_or_ref": "RCP-YIELD-PU",
        "servings": "10",
        "reporting_currency": "AED",
        "selling_price_per_portion": "40",
        "target_food_cost_percent": "30",
        "ingredients": [
            {
                "temp_ref": "I-1",
                "description": "Edible chicken",
                "recipe_qty": "10",
                "recipe_uom": "KG",
                "quantity_basis": "EDIBLE",
                "yield_percent": "80",
                "prep_loss_percent": "20",  # Ignored when EDIBLE so yield is not double-counted (§25)
                "costing_uom": "KG",
                "unit_price": "240",
                "price_unit": "10",  # AED 240 per 10 KG = AED 24 / KG (§21)
            }
        ],
    }
    res = client.post("/v1/costing/calculate", json=payload)
    assert res.status_code == 200
    data = res.json()
    # 10 KG edible @ 80% yield = 12.5 KG raw * 24 AED/KG = 300 AED
    assert Decimal(data["total_ingredient_cost"]) == Decimal("300.0000")
    assert Decimal(data["ingredient_cost_per_portion"]) == Decimal("30.0000")
    # Recommended selling price @ 30% target = 30 / 0.30 = 100 AED
    assert Decimal(data["recommended_selling_price_net"]) == Decimal("100.0000")


def test_missing_price_currency_mismatch_zero_selling_and_circular_sub_recipe():
    payload = {
        "tenant_id": "test-tenant-1c",
        "recipe_id_or_ref": "RCP-PARENT",
        "servings": "20",
        "reporting_currency": "AED",
        "selling_price_per_portion": "0",
        "target_food_cost_percent": "28",
        "ingredients": [
            {"temp_ref": "I-MISS", "description": "Saffron", "recipe_qty": "1", "recipe_uom": "G", "unit_price": None},
            {"temp_ref": "I-FX", "description": "Truffle Oil", "recipe_qty": "1", "recipe_uom": "L", "unit_price": "50", "price_currency": "EUR"},
            {"temp_ref": "I-CIRC", "description": "Circular Sauce", "is_sub_recipe": True, "sub_recipe_id": "RCP-PARENT", "recipe_qty": "2", "recipe_uom": "KG"},
        ],
    }
    res = client.post("/v1/costing/calculate", json=payload)
    assert res.status_code == 200
    data = res.json()
    assert data["missing_price_count"] == 1
    assert data["currency_mismatch_count"] == 1
    assert data["circular_dependency_detected"] is True
    assert data["food_cost_percent"] is None
    assert data["cost_completeness"] == "PARTIAL"


def test_sub_recipe_costing_and_scenarios_plus_optimization():
    base = _butter_chicken_50_payload()
    base["ingredients"].append({
        "temp_ref": "SUB-1",
        "description": "Tomato Butter Sauce",
        "is_sub_recipe": True,
        "sub_recipe_id": "RCP-SAUCE-1",
        "sub_recipe_batch_yield": "10",
        "sub_recipe_total_cost": "150",
        "recipe_qty": "3",
        "recipe_uom": "KG",
    })
    res = client.post("/v1/costing/calculate", json=base)
    assert res.status_code == 200
    data = res.json()
    assert Decimal(data["sub_recipe_cost"]) == Decimal("45.0000")
    assert Decimal(data["total_ingredient_cost"]) == Decimal("444.8000")

    opt = client.post("/v1/costing/optimize", json={"tenant_id": "test-tenant-1c", "goal": "Reduce food cost by 10%", "costing_request": base})
    assert opt.status_code == 200
    opt_data = opt.json()
    assert len(opt_data["suggestions"]) >= 1
    assert all(s["requires_chef_confirmation"] is True for s in opt_data["suggestions"])


def test_material_request_assist_detects_duplicates_and_plant_extensions():
    res = client.post(
        "/v1/material-requests/assist",
        json={
            "tenant_id": "test-tenant-1c",
            "ingredient_description": "Fresh Dill",
            "required_qty": "0.5",
            "recipe_uom": "KG",
            "recipe_name": "Grilled Salmon",
            "existing_materials": [
                {
                    "material_id": "mat-dill-1",
                    "material_code": "00004412",
                    "description": "FRESH DILL HERB",
                    "base_uom": "KG",
                    "is_active": True,
                    "plant_eligible": False,
                }
            ],
        },
    )
    assert res.status_code == 200
    data = res.json()
    assert data["ai_suggested"] is True
    assert data["recommended_request_type"] == "MATERIAL_EXTENSION"
    assert len(data["potential_duplicates"]) == 1
    assert data["potential_duplicates"][0]["material_code"] == "00004412"
