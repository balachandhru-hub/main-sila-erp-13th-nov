from __future__ import annotations

from fastapi import FastAPI, File, HTTPException, UploadFile
from pydantic import BaseModel
from app.agents.recipe_agent import RecipeIntelligenceAgent
from app.engines.documents import extract_recipe_document, validate_and_prepare_dish_image
from app.engines.material_eval import run_material_evaluation
from app.engines.material_index import get_index_manager
from app.engines.material_matcher import MaterialIntelligenceEngine
from app.engines.mrp import EventMrpRequest, EventMrpResult, calculate_event_mrp
from app.engines.normalization import convert_physical_quantity, normalize_ingredient_text
from app.engines.pnl import EventPnlRequest, EventPnlResult, calculate_event_pnl
from app.engines.scaling import (
    RecipeScaleRequest,
    RecipeScaleResult,
    RecipeScaleV2Request,
    RecipeScaleV2Result,
    scale_recipe,
    scale_recipe_v2,
)
from app.engines.substitution_engine import SubstitutionEngine
from app.engines.validation import enrich_and_validate_recipe
from app.providers.adapters import test_provider_connection
from app.providers.router import ProviderKind, ProviderPolicy, ProviderUnavailableError
from app.schemas.material_ai import (
    BulkMaterialMatchRequest,
    BulkMaterialMatchResponse,
    EvaluationMetricsResponse,
    IndexBuildRequest,
    IndexStatusResponse,
    IndexSyncRequest,
    IngredientMatchResponse,
    MatchCandidateResult,
    MaterialMatchRequest,
    MaterialMatchingConfig,
    MaterialSearchRequest,
    SubstitutionSuggestRequest,
    SubstitutionSuggestResponse,
)
from app.schemas.recipe_ai import (
    ExtractedDocumentResult,
    GeneratedRecipeResponse,
    GenerateRecipeHttpRequest,
    ProviderRuntimeConfig,
    RefineRecipeHttpRequest,
    RefineRecipeHttpResponse,
    RegenerateSectionHttpRequest,
    StructuredRecipePayload,
)

app = FastAPI(
    title="SILA Culinary Intelligence Engine",
    version="1.0.0-phase1b",
    description="Isolated culinary deterministic math, document/image interpretation, AI recipe generation, and Material Intelligence engine (Phase 1A + 1B).",
)

_agent = RecipeIntelligenceAgent()
_index_mgr = get_index_manager()
_material_engine = MaterialIntelligenceEngine(_index_mgr)
_substitution_engine = SubstitutionEngine(_index_mgr)


class ProviderTestEnvelope(BaseModel):
    provider_config: ProviderRuntimeConfig
    provider: ProviderKind


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok", "phase": "phase1b", "mode": "culinary-recipe-and-material-intelligence"}


@app.get("/v1/providers/capabilities")
def capabilities() -> dict:
    return {
        "operatingModes": ["LOCAL", "GEMINI", "CLAUDE", "HYBRID"],
        "tasks": [
            "RECIPE_GENERATION",
            "RECIPE_REFINEMENT",
            "RECIPE_DOCUMENT_INTERPRETATION",
            "RECIPE_IMAGE_INTERPRETATION",
            "INGREDIENT_SUBSTITUTION",
            "PREPARATION_INSTRUCTIONS",
            "DIETARY_ANALYSIS",
            "CULINARY_EXPLANATION",
            "MENU_GENERATION",
            "EVENT_PLANNING",
            "INGREDIENT_EXTRACTION",
            "MATERIAL_SEMANTIC_MATCHING",
            "FOOD_COST_RECOMMENDATIONS",
            "MANAGEMENT_INSIGHTS",
            "REPORT_SUMMARIES",
            "VOICE_INTERPRETATION",
        ],
        "deterministicEngines": [
            "RECIPE_SCALING",
            "RECIPE_SCALING_V2",
            "EVENT_MRP",
            "EVENT_PNL",
            "UNIT_CONVERSION",
            "MATERIAL_MATCHING",
            "MATERIAL_INDEX",
            "UOM_PACK_VALIDATION",
            "SUBSTITUTION_ENGINE",
        ],
    }


@app.post("/v1/providers/test")
async def post_provider_test(env: ProviderTestEnvelope) -> dict:
    cfg = env.provider_config
    policy = ProviderPolicy(
        operating_mode=cfg.operating_mode,
        tenant_id=cfg.tenant_id,
        local_enabled=cfg.local_enabled,
        local_endpoint=cfg.local_endpoint,
        local_model=cfg.local_model,
        gemini_enabled=cfg.gemini_enabled,
        gemini_model=cfg.gemini_model,
        gemini_api_key_present=bool(cfg.gemini_api_key),
        gemini_api_key=cfg.gemini_api_key,
        claude_enabled=cfg.claude_enabled,
        claude_model=cfg.claude_model,
        claude_api_key_present=bool(cfg.claude_api_key),
        claude_api_key=cfg.claude_api_key,
        tenant_external_ai_allowed=cfg.tenant_external_ai_allowed,
    )
    return await test_provider_connection(policy, env.provider)


@app.post("/v1/engines/scale", response_model=RecipeScaleResult)
def post_scale(req: RecipeScaleRequest) -> RecipeScaleResult:
    return scale_recipe(req)


@app.post("/v1/engines/scale-v2", response_model=RecipeScaleV2Result)
def post_scale_v2(req: RecipeScaleV2Request) -> RecipeScaleV2Result:
    return scale_recipe_v2(req)


@app.post("/v1/engines/mrp", response_model=EventMrpResult)
def post_mrp(req: EventMrpRequest) -> EventMrpResult:
    return calculate_event_mrp(req)


@app.post("/v1/engines/pnl", response_model=EventPnlResult)
def post_pnl(req: EventPnlRequest) -> EventPnlResult:
    return calculate_event_pnl(req)


@app.post("/v1/recipes/validate")
def post_validate_recipe(payload: StructuredRecipePayload) -> dict:
    validated, warnings = enrich_and_validate_recipe(payload)
    return {"recipe": validated, "warnings": warnings}


@app.post("/v1/recipes/normalize-ingredient")
def post_normalize_ingredient(description: str) -> dict:
    return normalize_ingredient_text(description)


@app.post("/v1/recipes/convert-uom")
def post_convert_uom(quantity: float, from_uom: str, to_uom: str, density_g_per_ml: float | None = None) -> dict:
    from decimal import Decimal

    try:
        res = convert_physical_quantity(
            Decimal(str(quantity)),
            from_uom,
            to_uom,
            Decimal(str(density_g_per_ml)) if density_g_per_ml is not None else None,
        )
    except ValueError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc
    return {"quantity": str(res), "uom": to_uom.upper()}


@app.post("/v1/recipes/generate", response_model=GeneratedRecipeResponse)
async def post_generate_recipe(req: GenerateRecipeHttpRequest, generation_ref: str | None = None) -> GeneratedRecipeResponse:
    try:
        return await _agent.generate_recipe(req, generation_ref=generation_ref)
    except ProviderUnavailableError as exc:
        status = 403 if exc.code == "UNAUTHORIZED_PROVIDER" else 503
        raise HTTPException(status_code=status, detail={"code": exc.code, "message": exc.message}) from exc


@app.post("/v1/recipes/refine", response_model=RefineRecipeHttpResponse)
async def post_refine_recipe(req: RefineRecipeHttpRequest) -> RefineRecipeHttpResponse:
    try:
        return await _agent.refine_recipe(req)
    except ProviderUnavailableError as exc:
        status = 403 if exc.code == "UNAUTHORIZED_PROVIDER" else 503
        raise HTTPException(status_code=status, detail={"code": exc.code, "message": exc.message}) from exc


@app.post("/v1/recipes/regenerate-section", response_model=GeneratedRecipeResponse)
async def post_regenerate_section(req: RegenerateSectionHttpRequest) -> GeneratedRecipeResponse:
    try:
        return await _agent.regenerate_section(req)
    except ProviderUnavailableError as exc:
        status = 403 if exc.code == "UNAUTHORIZED_PROVIDER" else 503
        raise HTTPException(status_code=status, detail={"code": exc.code, "message": exc.message}) from exc


@app.post("/v1/recipes/cancel/{generation_ref}")
def post_cancel_generation(generation_ref: str) -> dict[str, bool | str]:
    cancelled = _agent.cancel_generation(generation_ref)
    return {"generation_reference": generation_ref, "cancelled": cancelled}


@app.post("/v1/recipes/extract-document", response_model=ExtractedDocumentResult)
async def post_extract_document(file: UploadFile = File(...)) -> ExtractedDocumentResult:
    try:
        content = await file.read()
        return extract_recipe_document(file.filename or "recipe.txt", content)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/recipes/prepare-dish-image")
async def post_prepare_dish_image(file: UploadFile = File(...)) -> dict:
    try:
        content = await file.read()
        b64, mime, w, h = validate_and_prepare_dish_image(file.filename or "dish.jpg", content)
        return {"base64": b64, "mime_type": mime, "width": w, "height": h}
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


# ==================== PHASE 1B MATERIAL INTELLIGENCE ENDPOINTS ====================


@app.post("/v1/materials/index/build", response_model=IndexStatusResponse)
def post_build_material_index(req: IndexBuildRequest) -> IndexStatusResponse:
    try:
        return _index_mgr.build_index(
            tenant_id=req.tenant_id,
            materials=req.materials,
            aliases=req.aliases,
            config=req.config,
            force_rebuild=req.force_rebuild,
            persist=True,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/materials/index/sync", response_model=IndexStatusResponse)
def post_sync_material_index(req: IndexSyncRequest) -> IndexStatusResponse:
    try:
        return _index_mgr.sync_index(
            tenant_id=req.tenant_id,
            upserted_materials=req.upserted_materials,
            deactivated_material_ids=req.deactivated_material_ids,
            aliases=req.aliases,
            config=req.config,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.get("/v1/materials/index/status/{tenant_id}", response_model=IndexStatusResponse)
def get_material_index_status(tenant_id: str) -> IndexStatusResponse:
    try:
        return _index_mgr.get_status(tenant_id)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/materials/match", response_model=IngredientMatchResponse)
async def post_match_material(req: MaterialMatchRequest) -> IngredientMatchResponse:
    try:
        return await _material_engine.match_ingredient(
            tenant_id=req.tenant_id,
            ingredient=req.ingredient,
            config=req.config,
            materials=req.materials,
            aliases=req.aliases,
            provider_config=req.provider_config,
        )
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/materials/match-bulk", response_model=BulkMaterialMatchResponse)
async def post_match_materials_bulk(req: BulkMaterialMatchRequest) -> BulkMaterialMatchResponse:
    try:
        return await _material_engine.match_bulk(req)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/materials/search", response_model=list[MatchCandidateResult])
def post_search_materials(req: MaterialSearchRequest) -> list[MatchCandidateResult]:
    try:
        return _material_engine.search_materials(req)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/materials/substitutions", response_model=SubstitutionSuggestResponse)
async def post_suggest_substitutions(req: SubstitutionSuggestRequest) -> SubstitutionSuggestResponse:
    try:
        return await _substitution_engine.suggest_substitutions(req)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/v1/materials/evaluate", response_model=EvaluationMetricsResponse)
async def post_evaluate_material_engine(config: MaterialMatchingConfig | None = None) -> EvaluationMetricsResponse:
    return await run_material_evaluation(config)

