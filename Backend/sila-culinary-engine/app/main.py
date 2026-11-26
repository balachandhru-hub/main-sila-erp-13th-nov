from __future__ import annotations

from fastapi import FastAPI
from app.engines.mrp import EventMrpRequest, EventMrpResult, calculate_event_mrp
from app.engines.pnl import EventPnlRequest, EventPnlResult, calculate_event_pnl
from app.engines.scaling import RecipeScaleRequest, RecipeScaleResult, scale_recipe

app = FastAPI(
    title="SILA Culinary Intelligence Engine",
    version="0.1.0-phase0",
    description="Isolated deterministic culinary math and AI provider router foundation (Phase 0).",
)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "phase": "phase0", "mode": "deterministic-foundation"}


@app.get("/v1/providers/capabilities")
def capabilities() -> dict:
    return {
        "operatingModes": ["LOCAL", "GEMINI", "CLAUDE", "HYBRID"],
        "tasks": [
            "RECIPE_GENERATION",
            "MENU_GENERATION",
            "EVENT_PLANNING",
            "INGREDIENT_EXTRACTION",
            "RECIPE_DOCUMENT_INTERPRETATION",
            "MATERIAL_SEMANTIC_MATCHING",
            "INGREDIENT_SUBSTITUTION",
            "FOOD_COST_RECOMMENDATIONS",
            "MANAGEMENT_INSIGHTS",
            "REPORT_SUMMARIES",
            "VOICE_INTERPRETATION",
        ],
        "deterministicEngines": ["RECIPE_SCALING", "EVENT_MRP", "EVENT_PNL"],
    }


@app.post("/v1/engines/scale", response_model=RecipeScaleResult)
def post_scale(req: RecipeScaleRequest) -> RecipeScaleResult:
    return scale_recipe(req)


@app.post("/v1/engines/mrp", response_model=EventMrpResult)
def post_mrp(req: EventMrpRequest) -> EventMrpResult:
    return calculate_event_mrp(req)


@app.post("/v1/engines/pnl", response_model=EventPnlResult)
def post_pnl(req: EventPnlRequest) -> EventPnlResult:
    return calculate_event_pnl(req)
