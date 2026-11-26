from decimal import Decimal
from app.engines.mrp import EventMrpRequest, MrpLineInput, calculate_event_mrp
from app.engines.pnl import EventPnlRequest, calculate_event_pnl
from app.engines.scaling import RecipeScaleRequest, ScalingIngredientInput, scale_recipe


def test_salmon_recipe_scaling_base_and_nonlinear():
    req = RecipeScaleRequest(
        recipe_name="Grilled Salmon with Lemon Butter Sauce",
        base_servings=Decimal("10"),
        target_servings=Decimal("20"),
        use_nonlinear_rules=True,
        ingredients=[
            ScalingIngredientInput(name="Salmon fillet", base_qty=Decimal("1.8"), uom="kg", unit_cost=Decimal("85"), scaling_rule="LINEAR", material_matched=True, material_code="DEMO-SAL-01"),
            ScalingIngredientInput(name="Sea salt", category="SPICE", base_qty=Decimal("0.025"), uom="kg", unit_cost=Decimal("5"), scaling_rule="SEASONING_STEPPED", material_matched=True, material_code="DEMO-SALT-01"),
            ScalingIngredientInput(name="Fresh dill", category="HERB", base_qty=Decimal("0.05"), uom="kg", unit_cost=Decimal("45"), scaling_rule="LINEAR", material_matched=False),
        ],
    )
    res = scale_recipe(req)
    assert res.base_total_cost == Decimal("155.38")
    assert res.missing_material_count == 1
    assert res.ingredients[0].scaled_qty == Decimal("3.6000")
    # Sea salt uses stepped sub-linear scaling (2^0.80 < 2.0)
    assert res.ingredients[1].scaled_qty < Decimal("0.0500")


def test_event_mrp_salmon_spec_example():
    req = EventMrpRequest(
        event_reference="EVT-2027-0001",
        expected_guests=500,
        lines=[
            MrpLineInput(
                material_name="Salmon",
                material_code="DEMO-SAL-01",
                material_found=True,
                uom="kg",
                uptake_percent=Decimal("60"),
                edible_portion_grams=Decimal("180"),
                prep_buffer_percent=Decimal("10"),
                usable_yield_percent=Decimal("90"),
                available_stock=Decimal("25"),
                incoming_po_qty=Decimal("10"),
                pack_size=Decimal("5"),
                unit_cost=Decimal("85"),
            ),
            MrpLineInput(
                material_name="Fresh Dill",
                material_found=False,
                uom="kg",
                fixed_raw_requirement=Decimal("3"),
            ),
        ],
    )
    res = calculate_event_mrp(req)
    salmon = res.lines[0]
    assert salmon.expected_portions == Decimal("300.00")
    assert salmon.edible_requirement == Decimal("54.00")
    assert salmon.buffered_edible_requirement == Decimal("59.40")
    assert salmon.raw_requirement == Decimal("66.00")
    assert salmon.shortage == Decimal("31.00")
    # 31 kg rounded to 5 kg packs => 7 packs = 35 kg, 4 kg excess
    assert salmon.recommended_purchase == Decimal("35.00")
    assert salmon.excess_after_rounding == Decimal("4.00")
    assert res.lines[1].action_status == "MATERIAL_CREATION_REQUIRED"


def test_event_pnl_gala_dinner_spec_example():
    req = EventPnlRequest(
        event_reference="EVT-2027-0001",
        guest_count=500,
        selling_price_per_guest=Decimal("350"),
        food_cost_per_guest=Decimal("90"),
        beverage_cost_per_guest=Decimal("25"),
        fixed_event_expenses=Decimal("35000"),
    )
    res = calculate_event_pnl(req)
    assert res.net_revenue == Decimal("175000.00")
    assert res.food_cost == Decimal("45000.00")
    assert res.beverage_cost == Decimal("12500.00")
    assert res.fixed_cost == Decimal("35000.00")
    assert res.total_event_cost == Decimal("92500.00")
    assert res.event_contribution == Decimal("82500.00")
    assert res.contribution_margin_percent == Decimal("47.14")
    assert res.food_cost_percent == Decimal("25.71")
    assert res.break_even_guests == 149
