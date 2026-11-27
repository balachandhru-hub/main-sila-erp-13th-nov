from __future__ import annotations

import asyncio
import time
from decimal import Decimal
from unittest.mock import AsyncMock

import pytest
from fastapi.testclient import TestClient

from app.engines.ingredient_attributes import extract_ingredient_attributes
from app.engines.material_eval import run_material_evaluation
from app.engines.material_index import TenantMaterialIndexManager
from app.engines.material_matcher import MaterialIntelligenceEngine, validate_uom_and_pack
from app.engines.substitution_engine import SubstitutionEngine
from app.main import app
from app.providers.router import AiProviderResponse
from app.schemas.material_ai import (
    BulkMaterialMatchRequest,
    IngredientQueryLine,
    MaterialAliasRecord,
    MaterialMasterRecord,
    MaterialMatchingConfig,
    MaterialPriceSummary,
    MaterialStockSummary,
    MaterialUomFactor,
    SubstitutionSuggestRequest,
)
from app.schemas.recipe_ai import ProviderRuntimeConfig


def _sample_catalog() -> tuple[list[MaterialMasterRecord], list[MaterialAliasRecord]]:
    materials = [
        MaterialMasterRecord(
            material_id="m-001",
            material_code="00001045",
            description="Chicken Breast Boneless Fresh",
            category="PROTEIN_POULTRY",
            base_uom="KG",
            order_uom="CASE",
            pack_size=Decimal("5"),
            pack_uom="KG",
            uom_conversions=[MaterialUomFactor(from_uom="CASE", to_uom="KG", factor=Decimal("5"))],
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
            stock_summary=MaterialStockSummary(on_hand_qty=Decimal("25"), available_qty=Decimal("25"), inventory_uom="KG", stock_status="KNOWN"),
            price_summary=MaterialPriceSummary(unit_price=Decimal("28.50"), currency="AED", price_uom="KG", price_source="APPROVED_CONTRACT", price_status="VERIFIED"),
        ),
        MaterialMasterRecord(
            material_id="m-002",
            material_code="00002010",
            description="Butter Unsalted Dairy 82% Fat",
            brand="Lurpak",
            category="DAIRY",
            base_uom="KG",
            order_uom="CASE",
            pack_size=Decimal("0.5"),
            pack_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
            allergen_info=["DAIRY"],
            price_summary=MaterialPriceSummary(unit_price=Decimal("42.00"), currency="AED", price_uom="KG", price_source="STANDARD_COST", price_status="VERIFIED"),
        ),
        MaterialMasterRecord(
            material_id="m-003",
            material_code="00002011",
            description="Butter Salted Dairy 80% Fat",
            brand="Anchor",
            category="DAIRY",
            base_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
            allergen_info=["DAIRY"],
            price_summary=MaterialPriceSummary(unit_price=Decimal("32.00"), currency="AED", price_uom="KG", price_source="STANDARD_COST", price_status="VERIFIED"),
        ),
        MaterialMasterRecord(
            material_id="m-004",
            material_code="00002020",
            description="Cooking Cream 35% Fat",
            category="DAIRY",
            base_uom="L",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
            allergen_info=["DAIRY"],
        ),
        MaterialMasterRecord(
            material_id="m-005",
            material_code="00002021",
            description="Fresh Dairy Cream 35% Fat",
            category="DAIRY",
            base_uom="L",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
            allergen_info=["DAIRY"],
        ),
        MaterialMasterRecord(
            material_id="m-006",
            material_code="00002035",
            description="Parmesan Cheese Grated",
            category="DAIRY",
            base_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
            allergen_info=["DAIRY"],
        ),
        MaterialMasterRecord(
            material_id="m-007",
            material_code="00003001",
            description="Eggplant Fresh",
            category="PRODUCE",
            base_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
        ),
        MaterialMasterRecord(
            material_id="m-008",
            material_code="00004010",
            description="Extra Virgin Olive Oil",
            category="OILS_FATS",
            base_uom="L",
            order_uom="BTL",
            uom_conversions=[MaterialUomFactor(from_uom="BTL", to_uom="ML", factor=Decimal("750"))],
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
        ),
        MaterialMasterRecord(
            material_id="m-009",
            material_code="00005001",
            description="Fresh Dill Herb",
            category="HERBS_SPICES",
            base_uom="KG",
            is_active=True,
            plant_eligible=False,
            purchasing_eligible=True,
        ),
        MaterialMasterRecord(
            material_id="m-010",
            material_code="00005002",
            description="Saffron Threads Grade A",
            category="HERBS_SPICES",
            base_uom="G",
            is_active=False,
            plant_eligible=True,
            purchasing_eligible=True,
        ),
        MaterialMasterRecord(
            material_id="m-011",
            material_code="00007001",
            description="Sea Salt Fine",
            category="HERBS_SPICES",
            base_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
        ),
        MaterialMasterRecord(
            material_id="m-012",
            material_code="00007002",
            description="Sea Salt Fine",
            category="HERBS_SPICES",
            base_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
        ),
    ]
    aliases = [
        MaterialAliasRecord(
            alias_id="al-1",
            alias_text="Aubergine",
            material_id="m-007",
            material_code="00003001",
            approval_status="APPROVED",
        ),
    ]
    return materials, aliases


def test_attribute_extraction_and_preservation():
    attrs = extract_ingredient_attributes("Fresh unsalted dairy butter, 82% fat")
    assert attrs.salted_or_unsalted == "UNSALTED"
    assert attrs.fresh_or_frozen == "FRESH"
    assert attrs.fat_percentage == "82%"
    assert "DAIRY" in attrs.allergen_flags
    assert "unsalted" in attrs.specification_tokens


@pytest.mark.asyncio
async def test_exact_code_preserves_leading_zeros_and_short_circuits():
    materials, aliases = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False)

    res = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(
            temp_ref="i1",
            description="Boneless chicken breast",
            explicit_material_code="00001045",
            quantity=Decimal("1500"),
            recipe_uom="G",
        ),
        cfg,
        materials=materials,
        aliases=aliases,
    )
    assert res.status == "EXACT_MATCH"
    assert res.recommended_candidate is not None
    assert res.recommended_candidate.material_code == "00001045"
    assert res.recommended_candidate.uom_validation.converted_base_qty == Decimal("1.5000")
    assert res.auto_bulk_acceptable is True


@pytest.mark.asyncio
async def test_duplicate_exact_descriptions_require_selection():
    materials, aliases = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False)

    res = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="i2", description="Sea Salt Fine", quantity=Decimal("0.2"), recipe_uom="KG"),
        cfg,
        materials=materials,
        aliases=aliases,
    )
    assert res.status == "MULTIPLE_CANDIDATES"
    assert res.auto_bulk_acceptable is False
    assert len(res.candidates) >= 2


@pytest.mark.asyncio
async def test_salted_vs_unsalted_conflict_never_wins():
    materials, aliases = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=True, reranking_enabled=True)

    # Unsalted butter query must match 00002010 (Unsalted), never 00002011 (Salted)
    res = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="i3", description="Fresh unsalted dairy butter, 82% fat", quantity=Decimal("1.25"), recipe_uom="KG"),
        cfg,
        materials=materials,
        aliases=aliases,
    )
    assert res.recommended_candidate is not None
    assert res.recommended_candidate.material_code == "00002010"
    assert res.recommended_candidate.uom_validation.indicative_packs_needed == Decimal("3")
    assert res.recommended_candidate.uom_validation.excess_after_pack_rounding == Decimal("0.2500")

    # Even if ONLY Salted butter exists in catalog, requesting Unsalted must report SPECIFICATION_CONFLICT
    only_salted = [m for m in materials if m.material_code == "00002011"]
    res_conflict = await engine.match_ingredient(
        "tenant-only-salted",
        IngredientQueryLine(temp_ref="i3b", description="Unsalted butter", quantity=Decimal("1"), recipe_uom="KG"),
        MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False),
        materials=only_salted,
    )
    assert res_conflict.status == "SPECIFICATION_CONFLICT"


@pytest.mark.asyncio
async def test_alias_fuzzy_and_semantic_matching():
    materials, aliases = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=True, reranking_enabled=True)

    mgr.build_index("tenant-a", materials, aliases, cfg, force_rebuild=True, persist=False)

    res_alias = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="a1", description="Aubergine", quantity=Decimal("1"), recipe_uom="KG"),
        cfg,
    )
    assert res_alias.status == "VERIFIED_ALIAS_MATCH"
    assert res_alias.recommended_candidate is not None
    assert res_alias.recommended_candidate.material_code == "00003001"

    res_fuzzy = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="f1", description="Parmesan Chese", quantity=Decimal("250"), recipe_uom="G"),
        cfg,
    )
    assert res_fuzzy.recommended_candidate is not None
    assert res_fuzzy.recommended_candidate.material_code == "00002035"
    assert res_fuzzy.recommended_candidate.uom_validation.converted_base_qty == Decimal("0.2500")


def test_uom_validation_rules():
    materials, _ = _sample_catalog()
    oil = next(m for m in materials if m.material_code == "00004010")

    # 1.5 L -> 1.5 L base and 2 BTL purchasing via material-specific 1 BTL = 750 ML
    ok = validate_uom_and_pack(
        IngredientQueryLine(temp_ref="u1", description="Extra Virgin Olive Oil", quantity=Decimal("1500"), recipe_uom="ML"),
        oil,
    )
    assert ok.compatible is True
    assert ok.converted_base_qty == Decimal("1.5000")
    assert ok.purchasing_qty == Decimal("2.0000")

    # Mass KG -> Volume L without verified density MUST fail
    bad = validate_uom_and_pack(
        IngredientQueryLine(temp_ref="u2", description="Extra Virgin Olive Oil", quantity=Decimal("2"), recipe_uom="KG"),
        oil,
    )
    assert bad.compatible is False
    assert bad.status in {"UOM_CONFLICT", "MISSING_CONVERSION"}


@pytest.mark.asyncio
async def test_missing_plant_extension_and_inactive_classification():
    materials, aliases = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False)
    mgr.build_index("tenant-a", materials, aliases, cfg, force_rebuild=True, persist=False)

    dill = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="m1", description="Fresh Dill Herb", quantity=Decimal("0.2"), recipe_uom="KG"),
        cfg,
    )
    assert dill.status == "MATERIAL_INELIGIBLE"
    assert dill.missing_material_classification == "NOT_EXTENDED_TO_PLANT"
    assert dill.proposed_material_request_draft is not None
    assert dill.proposed_material_request_draft.request_type == "MATERIAL_EXTENSION"

    saffron = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="m2", description="Saffron Threads Grade A", quantity=Decimal("5"), recipe_uom="G"),
        cfg,
    )
    assert saffron.status == "MATERIAL_INELIGIBLE"
    assert saffron.missing_material_classification == "MATERIAL_INACTIVE"
    assert saffron.proposed_material_request_draft is not None
    assert saffron.proposed_material_request_draft.request_type == "MATERIAL_REACTIVATION"

    kasuri = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="m3", description="Kasuri Methi Dried Leaves", quantity=Decimal("50"), recipe_uom="G"),
        cfg,
    )
    assert kasuri.status == "UNMATCHED"
    assert kasuri.missing_material_classification == "NO_MATERIAL_FOUND"
    assert kasuri.proposed_material_request_draft is not None
    assert kasuri.proposed_material_request_draft.request_type == "NEW_MATERIAL"


@pytest.mark.asyncio
async def test_tenant_index_isolation_and_incremental_sync():
    materials, _ = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False)

    mgr.build_index("tenant-1", materials[:3], [], cfg, force_rebuild=True, persist=False)
    mgr.build_index("tenant-2", materials[3:5], [], cfg, force_rebuild=True, persist=False)

    # Tenant 2 must NEVER see tenant 1's chicken breast
    res_t2 = await engine.match_ingredient(
        "tenant-2",
        IngredientQueryLine(temp_ref="x1", description="Chicken Breast Boneless Fresh", quantity=Decimal("1"), recipe_uom="KG"),
        cfg,
    )
    assert res_t2.status == "UNMATCHED"

    # Deactivate m-001 in tenant 1 via incremental sync
    mgr.sync_index("tenant-1", upserted_materials=[], deactivated_material_ids=["m-001"], aliases=None, config=cfg)
    res_t1 = await engine.match_ingredient(
        "tenant-1",
        IngredientQueryLine(temp_ref="x2", description="Chicken Breast Boneless Fresh", quantity=Decimal("1"), recipe_uom="KG"),
        cfg,
    )
    assert res_t1.status == "MATERIAL_INELIGIBLE"


@pytest.mark.asyncio
async def test_llm_unknown_material_code_rejected_and_unauthorized_blocked():
    materials, _ = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False, llm_assisted_enabled=True)

    # Mock LLM returning a fabricated material code NOT in candidates
    engine._ai_router.execute = AsyncMock(  # type: ignore[method-assign]
        return_value=(
            AiProviderResponse(
                provider="GEMINI",
                model="gemini-2.5-flash",
                prompt_version="phase1b-mat-v1",
                raw_json={"recommended_material_code": "FAKE-99999", "reasoning": "hallucinated"},
                input_tokens=100,
                output_tokens=25,
            ),
            [],
        )
    )
    provider_cfg = ProviderRuntimeConfig(
        operating_mode="GEMINI",
        tenant_id="tenant-a",
        tenant_external_ai_allowed=True,
        gemini_enabled=True,
        gemini_api_key="test-key",
    )
    res = await engine.match_ingredient(
        "tenant-a",
        IngredientQueryLine(temp_ref="l1", description="Fresh Cream 35%", quantity=Decimal("1"), recipe_uom="L"),
        cfg,
        materials=materials,
        provider_config=provider_cfg,
    )
    assert res.llm_recommendation is not None
    assert res.llm_recommendation.validated_against_candidates is False
    assert res.llm_recommendation.rejected_unknown_code == "FAKE-99999"
    assert res.llm_recommendation.recommended_material_code is None


@pytest.mark.asyncio
async def test_substitution_engine_distinguishes_alternative_vs_culinary():
    materials, aliases = _sample_catalog()
    mgr = TenantMaterialIndexManager()
    sub_engine = SubstitutionEngine(mgr)
    cfg = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False)

    res = await sub_engine.suggest_substitutions(
        SubstitutionSuggestRequest(
            tenant_id="tenant-sub",
            ingredient=IngredientQueryLine(temp_ref="s1", description="Unsalted Butter", quantity=Decimal("1"), recipe_uom="KG"),
            current_material=materials[1],
            materials=materials,
            aliases=aliases,
            config=cfg,
        )
    )
    kinds = {s.substitution_kind for s in res.suggestions}
    assert "CULINARY_SUBSTITUTION" in kinds
    culinary = [s for s in res.suggestions if s.substitution_kind == "CULINARY_SUBSTITUTION"][0]
    assert culinary.ratio_requires_chef_input is True
    assert "DAIRY" in culinary.allergen_impact.upper()


@pytest.mark.asyncio
async def test_evaluation_dataset_metrics_and_1k_benchmark():
    metrics = await run_material_evaluation(MaterialMatchingConfig(semantic_matching_enabled=True, reranking_enabled=True))
    assert metrics.dataset_size >= 18
    assert metrics.top5_accuracy >= 0.90
    assert metrics.spec_conflict_detection_rate == 1.0
    assert metrics.uom_conflict_detection_rate == 1.0
    assert metrics.false_positive_rate == 0.0

    # Measure 1,000-material index build & query latency
    bulk_mats = [
        MaterialMasterRecord(
            material_id=f"bm-{i}",
            material_code=f"{i:08d}",
            description=f"Culinary Material Item {i} Grade A",
            category="DRY_GOODS",
            base_uom="KG",
            is_active=True,
            plant_eligible=True,
            purchasing_eligible=True,
        )
        for i in range(1000)
    ]
    mgr = TenantMaterialIndexManager()
    cfg_lex = MaterialMatchingConfig(semantic_matching_enabled=False, reranking_enabled=False)
    t0 = time.perf_counter()
    st = mgr.build_index("bench-1k", bulk_mats, [], cfg_lex, force_rebuild=True, persist=False)
    assert st.indexed_material_count == 1000
    engine = MaterialIntelligenceEngine(mgr)
    res = await engine.match_ingredient(
        "bench-1k",
        IngredientQueryLine(temp_ref="b1", description="Culinary Material Item 425", quantity=Decimal("2"), recipe_uom="KG"),
        cfg_lex,
    )
    elapsed_ms = (time.perf_counter() - t0) * 1000.0
    assert res.recommended_candidate is not None
    assert res.recommended_candidate.material_code == "00000425"
    assert elapsed_ms < 5000.0


def test_fastapi_material_routes_smoke():
    client = TestClient(app)
    materials, aliases = _sample_catalog()
    build_resp = client.post(
        "/v1/materials/index/build",
        json={
            "tenant_id": "api-test-tenant",
            "materials": [m.model_dump(mode="json") for m in materials],
            "aliases": [a.model_dump(mode="json") for a in aliases],
            "config": {"semantic_matching_enabled": False, "reranking_enabled": False},
            "force_rebuild": True,
        },
    )
    assert build_resp.status_code == 200
    assert build_resp.json()["indexed_material_count"] == len(materials)

    bulk_resp = client.post(
        "/v1/materials/match-bulk",
        json={
            "tenant_id": "api-test-tenant",
            "ingredients": [
                {"temp_ref": "r1", "description": "Chicken Breast Boneless Fresh", "quantity": 2, "recipe_uom": "KG"},
                {"temp_ref": "r2", "description": "Aubergine", "quantity": 1, "recipe_uom": "KG"},
            ],
            "config": {"semantic_matching_enabled": False, "reranking_enabled": False},
        },
    )
    assert bulk_resp.status_code == 200
    body = bulk_resp.json()
    assert body["processed_count"] == 2
    assert body["results"][0]["status"] == "EXACT_MATCH"
    assert body["results"][1]["status"] == "VERIFIED_ALIAS_MATCH"
