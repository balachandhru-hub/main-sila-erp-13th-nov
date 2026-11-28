from __future__ import annotations

from fastapi import FastAPI, File, HTTPException, Response, UploadFile
from pydantic import BaseModel
from app.agents.recipe_agent import RecipeIntelligenceAgent
from app.engines.bottle_vision import BottleVisionResult, analyze_bottle
from app.engines.documents import extract_recipe_document, validate_and_prepare_dish_image
from app.engines.material_eval import run_material_evaluation
from app.engines.material_index import get_index_manager
from app.engines.material_matcher import MaterialIntelligenceEngine
from app.engines.material_request_assist import assist_material_request
from app.engines.menu_planner import (
    calculate_event_menu_portions_and_cost,
    generate_ai_menu,
    optimize_menu_milp,
    semantic_search_recipes,
)
from app.engines.mrp import EventMrpRequest, EventMrpResult, calculate_event_mrp
from app.engines.mrp_advanced import analyze_mrp_with_ai, explode_mrp, optimize_mrp_batches
from app.engines.procurement_advanced import (
    analyze_procurement_with_ai,
    compute_procurement_plan,
    optimize_procurement_milp,
)
from app.engines.financial_advanced import (
    analyze_financial_with_ai,
    calculate_advanced_recipe_costing,
    calculate_event_financials,
    calculate_material_price_impact,
    calculate_menu_costing,
)
from app.engines.normalization import convert_physical_quantity, normalize_ingredient_text
from app.engines.pnl import EventPnlRequest, EventPnlResult, calculate_event_pnl
from app.engines.recipe_costing import calculate_recipe_costing, optimize_recipe_costing
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
from app.schemas.costing_ai import (
    CostOptimizationRequest,
    CostOptimizationResponse,
    MaterialRequestAssistRequest,
    MaterialRequestAssistResponse,
    RecipeCostingRequest,
    RecipeCostingResponse,
)
from app.schemas.event_menu_ai import (
    MenuGenerateRequest,
    MenuGenerateResponse,
    MenuOptimizeRequest,
    MenuOptimizeResponse,
    PortionCalculationRequest,
    PortionCalculationResponse,
    SemanticRecipeSearchRequest,
    SemanticRecipeSearchResponse,
)
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
from app.schemas.mrp_engine import (
    MrpAiAnalyzeRequest,
    MrpAiAnalyzeResponse,
    MrpExplodeRequest,
    MrpExplodeResponse,
    MrpOptimizeRequest,
    MrpOptimizeResponse,
)
from app.schemas.procurement_engine import (
    ProcurementAiAnalyzeRequest,
    ProcurementAiAnalyzeResponse,
    ProcurementOptimizeRequest,
    ProcurementOptimizeResponse,
    ProcurementPlanRequest,
    ProcurementPlanResponse,
)
from app.schemas.financial_engine import (
    EventFinancialCalculateRequest,
    EventFinancialCalculateResponse,
    FinancialAiAnalyzeRequest,
    FinancialAiAnalyzeResponse,
    FinancialRecipeCostRequest,
    FinancialRecipeCostResponse,
    MaterialPriceImpactRequest,
    MaterialPriceImpactResponse,
    MenuCostingRequest,
    MenuCostingResponse,
)
from app.engines.analytics_advanced import (
    analyze_consumption_variance,
    analyze_menu_engineering,
    analyze_reverse_price_impact,
    analyze_substitutions,
    analyze_waste,
    answer_culinary_copilot,
    benchmark_properties,
    forecast_demand,
    optimize_buffet,
    optimize_event_packages,
    optimize_menu_milp as optimize_menu_phase3b_milp,
    simulate_what_if,
)
from app.engines.me_ai_native import (
    evaluate_and_forecast_demand,
    optimize_menu_multi_constraint,
    parse_nl_command,
    suggest_pos_mappings_ai,
)
from app.schemas.me_ai_native import (
    MeBacktestForecastRequest,
    MeBacktestForecastResponse,
    MeMultiConstraintOptimizeRequest,
    MeMultiConstraintOptimizeResponse,
    MeNlParseRequest,
    MeNlParseResponse,
    MePosMappingSuggestRequest,
    MePosMappingSuggestResponse,
)
from app.engines.production_scheduler import (
    allocate_central_kitchen_batch,
    optimize_kitchen_schedule,
)
from app.schemas.production_engine import (
    CentralKitchenAllocateRequest,
    CentralKitchenAllocateResponse,
    ProductionScheduleRequest,
    ProductionScheduleResponse,
)
from app.engines.reconciliation_engine import (
    compute_cost_variance_bridge,
    compute_event_profit_bridge,
    compute_material_reconciliation,
    compute_portion_variance,
    compute_yield_variance,
    export_reconciliation_workbook,
    investigate_variances,
)
from app.schemas.reconciliation_engine import (
    CostVarianceBridgeRequest,
    CostVarianceBridgeResponse,
    EventProfitBridgeRequest,
    EventProfitBridgeResponse,
    MaterialReconciliationRequest,
    MaterialReconciliationResponse,
    PortionSampleRequest,
    PortionSampleResponse,
    ReconWorkbookExportRequest,
    VarianceInvestigationRequest,
    VarianceInvestigationResponse,
    YieldVarianceRequest,
    YieldVarianceResponse,
)
from app.schemas.analytics_engine import (
    BuffetOptimizeRequest,
    BuffetOptimizeResponse,
    ConsumptionVarianceRequest,
    ConsumptionVarianceResponse,
    CopilotQueryRequest,
    CopilotQueryResponse,
    DemandForecastRequest,
    DemandForecastResponse,
    EventPackageOptimizeRequest,
    EventPackageOptimizeResponse,
    MenuEngineeringAnalyzeRequest,
    MenuEngineeringAnalyzeResponse,
    MenuOptimizationRequest,
    MenuOptimizationResponse,
    PropertyBenchmarkRequest,
    PropertyBenchmarkResponse,
    ReversePriceImpactRequest,
    ReversePriceImpactResponse,
    SubstitutionAnalysisRequest,
    SubstitutionAnalysisResponse,
    WasteAnalyzeRequest,
    WasteAnalyzeResponse,
    WhatIfSimulationRequest,
    WhatIfSimulationResponse,
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
from app.engines.me_digital_twin import analyze_twin_dependency_graph, compute_historical_similarity
from app.engines.me_optimization import (
    optimize_inventory_driven_menu,
    optimize_menu_multi_objective,
    optimize_menu_pricing,
)
from app.engines.me_forecast_waste import (
    forecast_hierarchical_demand,
    predict_cold_start_item,
    predict_food_waste,
    predict_ingredient_prices,
)
from app.engines.me_simulation_risk import (
    evaluate_substitution_cascade,
    run_monte_carlo_simulation,
    run_sensitivity_analysis,
    run_stress_tests,
)
from app.schemas.me_twin_opt import (
    ColdStartPredictRequest,
    ColdStartPredictResponse,
    HierarchicalForecastRequest,
    HierarchicalForecastResponse,
    HistoricalSimilarityRequest,
    HistoricalSimilarityResponse,
    InventoryMenuOptimizeRequest,
    InventoryMenuOptimizeResponse,
    MeMultiObjectiveOptimizeRequest,
    MeMultiObjectiveOptimizeResponse,
    MonteCarloInput,
    MonteCarloResponse,
    PricePredictRequest,
    PricePredictResponse,
    PricingOptimizeRequest,
    PricingOptimizeResponse,
    SensitivityRequest,
    SensitivityResponse,
    StressTestRequest,
    StressTestResponse,
    SubstitutionCascadeRequest,
    SubstitutionCascadeResponse,
    TwinDependencyGraphRequest,
    TwinDependencyGraphResponse,
    WastePredictRequest,
    WastePredictResponse,
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


class BottleLevelHttpRequest(BaseModel):
    image_base64: str
    mime_type: str | None = None
    nominal_ml: float | None = None
    description: str | None = None
    provider_config: ProviderRuntimeConfig | None = None


@app.post("/v1/inventory/bottle-level", response_model=BottleVisionResult)
async def post_bottle_level(req: BottleLevelHttpRequest) -> BottleVisionResult:
    import base64
    import binascii

    try:
        content = base64.b64decode(req.image_base64, validate=True)
    except (binascii.Error, ValueError) as exc:
        raise HTTPException(status_code=400, detail="The bottle photo is not valid base64.") from exc
    try:
        return await analyze_bottle(content, req.nominal_ml, req.description, req.provider_config, req.mime_type)
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


# ==================== PHASE 1C MATERIAL REQUESTS & COSTING ENDPOINTS ====================


@app.post("/v1/material-requests/assist", response_model=MaterialRequestAssistResponse)
def post_assist_material_request(req: MaterialRequestAssistRequest) -> MaterialRequestAssistResponse:
    return assist_material_request(req)


@app.post("/v1/costing/calculate", response_model=RecipeCostingResponse)
def post_calculate_recipe_costing(req: RecipeCostingRequest) -> RecipeCostingResponse:
    return calculate_recipe_costing(req)


@app.post("/v1/costing/optimize", response_model=CostOptimizationResponse)
def post_optimize_recipe_costing(req: CostOptimizationRequest) -> CostOptimizationResponse:
    return optimize_recipe_costing(req)


# ==================== PHASE 2A MENU STUDIO & EVENT PLANNER ENDPOINTS ====================


@app.post("/v1/menus/search", response_model=SemanticRecipeSearchResponse)
def post_semantic_search_recipes(req: SemanticRecipeSearchRequest) -> SemanticRecipeSearchResponse:
    return semantic_search_recipes(req)


@app.post("/v1/menus/calculate", response_model=PortionCalculationResponse)
def post_calculate_event_menu(req: PortionCalculationRequest) -> PortionCalculationResponse:
    return calculate_event_menu_portions_and_cost(req)


@app.post("/v1/menus/optimize", response_model=MenuOptimizeResponse)
def post_optimize_event_menu(req: MenuOptimizeRequest) -> MenuOptimizeResponse:
    return optimize_menu_milp(req)


@app.post("/v1/menus/generate", response_model=MenuGenerateResponse)
async def post_generate_event_menu(req: MenuGenerateRequest) -> MenuGenerateResponse:
    return await generate_ai_menu(req)


# ==================== PHASE 2B EVENT MRP ENGINE ENDPOINTS ====================


@app.post("/v1/mrp/explode", response_model=MrpExplodeResponse)
def post_explode_mrp(req: MrpExplodeRequest) -> MrpExplodeResponse:
    return explode_mrp(req)


@app.post("/v1/mrp/optimize", response_model=MrpOptimizeResponse)
def post_optimize_mrp(req: MrpOptimizeRequest) -> MrpOptimizeResponse:
    return optimize_mrp_batches(req)


@app.post("/v1/mrp/analyze", response_model=MrpAiAnalyzeResponse)
async def post_analyze_mrp(req: MrpAiAnalyzeRequest) -> MrpAiAnalyzeResponse:
    return await analyze_mrp_with_ai(req)


# ==================== PHASE 2C PROCUREMENT INTELLIGENCE ENDPOINTS ====================


@app.post("/v1/procurement/plan-coverage", response_model=ProcurementPlanResponse)
def post_plan_procurement_coverage(req: ProcurementPlanRequest) -> ProcurementPlanResponse:
    return compute_procurement_plan(req)


@app.post("/v1/procurement/optimize", response_model=ProcurementOptimizeResponse)
def post_optimize_procurement(req: ProcurementOptimizeRequest) -> ProcurementOptimizeResponse:
    return optimize_procurement_milp(req)


@app.post("/v1/procurement/analyze", response_model=ProcurementAiAnalyzeResponse)
async def post_analyze_procurement(req: ProcurementAiAnalyzeRequest) -> ProcurementAiAnalyzeResponse:
    return await analyze_procurement_with_ai(req)


# ==================== PHASE 3A FINANCIAL INTELLIGENCE ENDPOINTS ====================


@app.post("/v1/financial/recipe-cost", response_model=FinancialRecipeCostResponse)
def post_financial_recipe_cost(req: FinancialRecipeCostRequest) -> FinancialRecipeCostResponse:
    return calculate_advanced_recipe_costing(req)


@app.post("/v1/financial/menu-cost", response_model=MenuCostingResponse)
def post_financial_menu_cost(req: MenuCostingRequest) -> MenuCostingResponse:
    return calculate_menu_costing(req)


@app.post("/v1/financial/event-pnl", response_model=EventFinancialCalculateResponse)
def post_financial_event_pnl(req: EventFinancialCalculateRequest) -> EventFinancialCalculateResponse:
    return calculate_event_financials(req)


@app.post("/v1/financial/price-impact", response_model=MaterialPriceImpactResponse)
def post_financial_price_impact(req: MaterialPriceImpactRequest) -> MaterialPriceImpactResponse:
    return calculate_material_price_impact(req)


@app.post("/v1/financial/analyze", response_model=FinancialAiAnalyzeResponse)
async def post_financial_analyze(req: FinancialAiAnalyzeRequest) -> FinancialAiAnalyzeResponse:
    return await analyze_financial_with_ai(req)


# ==================== PHASE 3B ADVANCED CULINARY ANALYTICS & OPTIMIZATION ENDPOINTS ====================


@app.post("/v1/analytics/menu-engineering", response_model=MenuEngineeringAnalyzeResponse)
def post_analytics_menu_engineering(req: MenuEngineeringAnalyzeRequest) -> MenuEngineeringAnalyzeResponse:
    return analyze_menu_engineering(req)


@app.post("/v1/analytics/consumption-variance", response_model=ConsumptionVarianceResponse)
def post_analytics_consumption_variance(req: ConsumptionVarianceRequest) -> ConsumptionVarianceResponse:
    return analyze_consumption_variance(req)


@app.post("/v1/analytics/price-impact", response_model=ReversePriceImpactResponse)
def post_analytics_price_impact(req: ReversePriceImpactRequest) -> ReversePriceImpactResponse:
    return analyze_reverse_price_impact(req)


@app.post("/v1/analytics/simulate", response_model=WhatIfSimulationResponse)
def post_analytics_simulate(req: WhatIfSimulationRequest) -> WhatIfSimulationResponse:
    return simulate_what_if(req)


@app.post("/v1/analytics/optimize-menu", response_model=MenuOptimizationResponse)
def post_analytics_optimize_menu(req: MenuOptimizationRequest) -> MenuOptimizationResponse:
    return optimize_menu_phase3b_milp(req)


@app.post("/v1/analytics/substitutions", response_model=SubstitutionAnalysisResponse)
def post_analytics_substitutions(req: SubstitutionAnalysisRequest) -> SubstitutionAnalysisResponse:
    return analyze_substitutions(req)


@app.post("/v1/analytics/buffet-optimize", response_model=BuffetOptimizeResponse)
def post_analytics_buffet_optimize(req: BuffetOptimizeRequest) -> BuffetOptimizeResponse:
    return optimize_buffet(req)


@app.post("/v1/analytics/event-optimize", response_model=EventPackageOptimizeResponse)
def post_analytics_event_optimize(req: EventPackageOptimizeRequest) -> EventPackageOptimizeResponse:
    return optimize_event_packages(req)


@app.post("/v1/analytics/forecast", response_model=DemandForecastResponse)
def post_analytics_forecast(req: DemandForecastRequest) -> DemandForecastResponse:
    return forecast_demand(req)


@app.post("/v1/analytics/waste", response_model=WasteAnalyzeResponse)
def post_analytics_waste(req: WasteAnalyzeRequest) -> WasteAnalyzeResponse:
    return analyze_waste(req)


@app.post("/v1/analytics/benchmark", response_model=PropertyBenchmarkResponse)
def post_analytics_benchmark(req: PropertyBenchmarkRequest) -> PropertyBenchmarkResponse:
    return benchmark_properties(req)


@app.post("/v1/analytics/copilot", response_model=CopilotQueryResponse)
def post_analytics_copilot(req: CopilotQueryRequest) -> CopilotQueryResponse:
    return answer_culinary_copilot(req)


# ─────────────────────────────────────────────────────────────────────────────
# Phase 4A — Kitchen Production Scheduling & Central Kitchen Allocation
# ─────────────────────────────────────────────────────────────────────────────


@app.post("/v1/production/schedule", response_model=ProductionScheduleResponse)
def post_production_schedule(req: ProductionScheduleRequest) -> ProductionScheduleResponse:
    return optimize_kitchen_schedule(req)


@app.post("/v1/production/central-kitchen-allocate", response_model=CentralKitchenAllocateResponse)
def post_production_central_kitchen_allocate(req: CentralKitchenAllocateRequest) -> CentralKitchenAllocateResponse:
    return allocate_central_kitchen_batch(req)


# ─────────────────────────────────────────────────────────────────────────────
# Phase 4B — Actual Consumption, Reconciliation, Cost Variance, Bridges & AI Investigation
# ─────────────────────────────────────────────────────────────────────────────


@app.post("/v1/reconciliation/materials", response_model=MaterialReconciliationResponse)
def post_reconciliation_materials(req: MaterialReconciliationRequest) -> MaterialReconciliationResponse:
    return compute_material_reconciliation(req)


@app.post("/v1/reconciliation/yield-variance", response_model=YieldVarianceResponse)
def post_reconciliation_yield_variance(req: YieldVarianceRequest) -> YieldVarianceResponse:
    return compute_yield_variance(req)


@app.post("/v1/reconciliation/portion-variance", response_model=PortionSampleResponse)
def post_reconciliation_portion_variance(req: PortionSampleRequest) -> PortionSampleResponse:
    return compute_portion_variance(req)


@app.post("/v1/reconciliation/cost-bridge", response_model=CostVarianceBridgeResponse)
def post_reconciliation_cost_bridge(req: CostVarianceBridgeRequest) -> CostVarianceBridgeResponse:
    return compute_cost_variance_bridge(req)


@app.post("/v1/reconciliation/event-profit-bridge", response_model=EventProfitBridgeResponse)
def post_reconciliation_event_profit_bridge(req: EventProfitBridgeRequest) -> EventProfitBridgeResponse:
    return compute_event_profit_bridge(req)


@app.post("/v1/reconciliation/investigate", response_model=VarianceInvestigationResponse)
def post_reconciliation_investigate(req: VarianceInvestigationRequest) -> VarianceInvestigationResponse:
    return investigate_variances(req)


@app.post("/v1/reconciliation/export-workbook")
def post_reconciliation_export_workbook(req: ReconWorkbookExportRequest) -> Response:
    content = export_reconciliation_workbook(req)
    return Response(
        content=content,
        media_type="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        headers={"Content-Disposition": 'attachment; filename="sila-phase4b-reconciliation-reports.xlsx"'},
    )


# ─────────────────────────────────────────────────────────────────────────────
# Phase ME.2 — AI-Native Menu Engineering Optimization, Forecasting & Mapping
# ─────────────────────────────────────────────────────────────────────────────


@app.post("/v1/me-ai/parse-command", response_model=MeNlParseResponse)
def post_me_ai_parse_command(req: MeNlParseRequest) -> MeNlParseResponse:
    return parse_nl_command(req)


@app.post("/v1/me-ai/optimize-scenarios", response_model=MeMultiConstraintOptimizeResponse)
def post_me_ai_optimize_scenarios(req: MeMultiConstraintOptimizeRequest) -> MeMultiConstraintOptimizeResponse:
    return optimize_menu_multi_constraint(req)


@app.post("/v1/me-ai/backtest-forecast", response_model=MeBacktestForecastResponse)
def post_me_ai_backtest_forecast(req: MeBacktestForecastRequest) -> MeBacktestForecastResponse:
    return evaluate_and_forecast_demand(req)


@app.post("/v1/me-ai/suggest-pos-mapping", response_model=MePosMappingSuggestResponse)
def post_me_ai_suggest_pos_mapping(req: MePosMappingSuggestRequest) -> MePosMappingSuggestResponse:
    return suggest_pos_mappings_ai(req)


# ─────────────────────────────────────────────────────────────────────────────
# Phase ME.3 — Digital Twin, Multi-Objective CP-SAT, Hierarchical Forecast & Risk
# ─────────────────────────────────────────────────────────────────────────────


@app.post("/v1/me-twin/dependency-graph", response_model=TwinDependencyGraphResponse)
def post_me_twin_dependency_graph(req: TwinDependencyGraphRequest) -> TwinDependencyGraphResponse:
    return analyze_twin_dependency_graph(req)


@app.post("/v1/me-twin/optimize-multi-objective", response_model=MeMultiObjectiveOptimizeResponse)
def post_me_twin_optimize_multi_objective(req: MeMultiObjectiveOptimizeRequest) -> MeMultiObjectiveOptimizeResponse:
    return optimize_menu_multi_objective(req)


@app.post("/v1/me-twin/optimize-inventory-menu", response_model=InventoryMenuOptimizeResponse)
def post_me_twin_optimize_inventory_menu(req: InventoryMenuOptimizeRequest) -> InventoryMenuOptimizeResponse:
    return optimize_inventory_driven_menu(req)


@app.post("/v1/me-twin/optimize-pricing", response_model=PricingOptimizeResponse)
def post_me_twin_optimize_pricing(req: PricingOptimizeRequest) -> PricingOptimizeResponse:
    return optimize_menu_pricing(req)


@app.post("/v1/me-twin/forecast-hierarchical", response_model=HierarchicalForecastResponse)
def post_me_twin_forecast_hierarchical(req: HierarchicalForecastRequest) -> HierarchicalForecastResponse:
    return forecast_hierarchical_demand(req)


@app.post("/v1/me-twin/predict-cold-start", response_model=ColdStartPredictResponse)
def post_me_twin_predict_cold_start(req: ColdStartPredictRequest) -> ColdStartPredictResponse:
    return predict_cold_start_item(req)


@app.post("/v1/me-twin/predict-waste", response_model=WastePredictResponse)
def post_me_twin_predict_waste(req: WastePredictRequest) -> WastePredictResponse:
    return predict_food_waste(req)


@app.post("/v1/me-twin/predict-prices", response_model=PricePredictResponse)
def post_me_twin_predict_prices(req: PricePredictRequest) -> PricePredictResponse:
    return predict_ingredient_prices(req)


@app.post("/v1/me-twin/simulate-monte-carlo", response_model=MonteCarloResponse)
def post_me_twin_simulate_monte_carlo(req: MonteCarloInput) -> MonteCarloResponse:
    return run_monte_carlo_simulation(req)


@app.post("/v1/me-twin/sensitivity", response_model=SensitivityResponse)
def post_me_twin_sensitivity(req: SensitivityRequest) -> SensitivityResponse:
    return run_sensitivity_analysis(req)


@app.post("/v1/me-twin/stress-test", response_model=StressTestResponse)
def post_me_twin_stress_test(req: StressTestRequest) -> StressTestResponse:
    return run_stress_tests(req)


@app.post("/v1/me-twin/substitution-cascade", response_model=SubstitutionCascadeResponse)
def post_me_twin_substitution_cascade(req: SubstitutionCascadeRequest) -> SubstitutionCascadeResponse:
    return evaluate_substitution_cascade(req)


@app.post("/v1/me-twin/historical-similarity", response_model=HistoricalSimilarityResponse)
def post_me_twin_historical_similarity(req: HistoricalSimilarityRequest) -> HistoricalSimilarityResponse:
    return compute_historical_similarity(req)







