from __future__ import annotations

import math
from decimal import Decimal, ROUND_HALF_UP
from pydantic import BaseModel, Field


class EventPnlRequest(BaseModel):
    event_reference: str
    guest_count: int = Field(gt=0)
    selling_price_per_guest: Decimal = Field(ge=Decimal("0"))
    fixed_revenue: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    discount_amount: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    food_cost_per_guest: Decimal = Field(ge=Decimal("0"))
    beverage_cost_per_guest: Decimal = Field(ge=Decimal("0"))
    variable_other_cost_per_guest: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    manpower_cost: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    fixed_event_expenses: Decimal = Field(default=Decimal("0"), ge=Decimal("0"))
    target_margin_percent: Decimal = Field(default=Decimal("45"), ge=Decimal("0"), le=Decimal("99"))


class EventPnlResult(BaseModel):
    event_reference: str
    guest_count: int
    net_revenue: Decimal
    food_cost: Decimal
    beverage_cost: Decimal
    variable_other_cost: Decimal
    fixed_cost: Decimal
    total_event_cost: Decimal
    event_contribution: Decimal
    contribution_margin_percent: Decimal
    food_cost_percent: Decimal
    beverage_cost_percent: Decimal
    revenue_per_guest: Decimal
    cost_per_guest: Decimal
    contribution_per_guest: Decimal
    break_even_guests: int | None
    required_selling_price_for_target_margin: Decimal
    max_allowable_food_cost: Decimal


def _money(value: Decimal) -> Decimal:
    return value.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def calculate_event_pnl(req: EventPnlRequest) -> EventPnlResult:
    guests = Decimal(req.guest_count)
    gross_revenue = guests * req.selling_price_per_guest + req.fixed_revenue
    net_revenue = max(Decimal("0"), gross_revenue - req.discount_amount)

    food_cost = guests * req.food_cost_per_guest
    beverage_cost = guests * req.beverage_cost_per_guest
    var_other = guests * req.variable_other_cost_per_guest
    fixed_cost = req.manpower_cost + req.fixed_event_expenses
    total_cost = food_cost + beverage_cost + var_other + fixed_cost
    contribution = net_revenue - total_cost

    if net_revenue > Decimal("0"):
        margin_pct = (contribution / net_revenue) * Decimal("100")
        food_pct = (food_cost / net_revenue) * Decimal("100")
        bev_pct = (beverage_cost / net_revenue) * Decimal("100")
    else:
        margin_pct = Decimal("0")
        food_pct = Decimal("0")
        bev_pct = Decimal("0")

    unit_var_cost = req.food_cost_per_guest + req.beverage_cost_per_guest + req.variable_other_cost_per_guest
    unit_contribution = req.selling_price_per_guest - unit_var_cost
    net_fixed = max(Decimal("0"), fixed_cost + req.discount_amount - req.fixed_revenue)
    break_even: int | None = None
    if unit_contribution > Decimal("0"):
        break_even = math.ceil(float(net_fixed / unit_contribution))

    target_ratio = req.target_margin_percent / Decimal("100")
    denom = Decimal("1") - target_ratio
    required_price = (total_cost / guests) / denom if denom > Decimal("0") else Decimal("0")

    target_total_allowable = net_revenue * (Decimal("1") - target_ratio)
    max_food = max(Decimal("0"), target_total_allowable - beverage_cost - var_other - fixed_cost)

    return EventPnlResult(
        event_reference=req.event_reference,
        guest_count=req.guest_count,
        net_revenue=_money(net_revenue),
        food_cost=_money(food_cost),
        beverage_cost=_money(beverage_cost),
        variable_other_cost=_money(var_other),
        fixed_cost=_money(fixed_cost),
        total_event_cost=_money(total_cost),
        event_contribution=_money(contribution),
        contribution_margin_percent=_money(margin_pct),
        food_cost_percent=_money(food_pct),
        beverage_cost_percent=_money(bev_pct),
        revenue_per_guest=_money(net_revenue / guests),
        cost_per_guest=_money(total_cost / guests),
        contribution_per_guest=_money(contribution / guests),
        break_even_guests=break_even,
        required_selling_price_for_target_margin=_money(required_price),
        max_allowable_food_cost=_money(max_food),
    )
