from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP

from app.engines.documents import sanitize_untrusted_text
from app.engines.ingredient_attributes import extract_ingredient_attributes
from app.engines.material_index import TenantMaterialIndexManager, get_index_manager
from app.engines.material_matcher import MaterialIntelligenceEngine, validate_uom_and_pack, _build_llm_policy
from app.providers.adapters import ClaudeSdkAdapter, GeminiSdkAdapter, LocalOllamaAdapter
from app.providers.router import AiExecutionRequest, AiProviderRouter, ProviderUnavailableError
from app.schemas.material_ai import (
    IngredientQueryLine,
    MaterialSearchRequest,
    SubstitutionComparisonItem,
    SubstitutionSuggestRequest,
    SubstitutionSuggestResponse,
)

# Deterministic culinary substitution knowledge base (§26, §27)
_CULINARY_SUB_RULES: list[tuple[str, dict]] = [
    (
        "butter",
        {
            "proposed_ingredient": "Extra Virgin Olive Oil",
            "search_terms": ["olive oil", "vegetable oil"],
            "ratio": Decimal("0.75"),
            "ratio_verified": False,
            "taste_impact": "Fruity, savory olive notes replace rich dairy butter flavor.",
            "texture_impact": "Lighter mouthfeel; less aeration/lamination in pastry or sauces.",
            "preparation_changes": "Lower heat slightly when sauteing; adjust emulsification in pan sauces.",
            "dietary_impact": "Removes dairy; suitable for dairy-free / vegan menus.",
            "allergen_impact": "Eliminates DAIRY allergen.",
        },
    ),
    (
        "heavy cream",
        {
            "proposed_ingredient": "Coconut Cream",
            "search_terms": ["coconut cream", "cooking cream"],
            "ratio": Decimal("1.0"),
            "ratio_verified": False,
            "taste_impact": "Mild sweet coconut aroma vs neutral dairy cream.",
            "texture_impact": "Comparable richness and body for curries and soups.",
            "preparation_changes": "Simmer gently; avoid prolonged boiling to prevent separation.",
            "dietary_impact": "Plant-based / dairy-free alternative.",
            "allergen_impact": "Removes DAIRY; verify coconut sensitivity.",
        },
    ),
    (
        "cream",
        {
            "proposed_ingredient": "Greek Yogurt",
            "search_terms": ["yogurt", "cooking cream", "sour cream"],
            "ratio": Decimal("1.0"),
            "ratio_verified": False,
            "taste_impact": "Tangier finish than fresh dairy cream.",
            "texture_impact": "Thicker; temper off-heat to avoid curdling.",
            "preparation_changes": "Fold in off direct heat at finishing stage.",
            "dietary_impact": "Lower fat content.",
            "allergen_impact": "Retains DAIRY allergen.",
        },
    ),
    (
        "lemon juice",
        {
            "proposed_ingredient": "Lime Juice",
            "search_terms": ["lime juice", "white wine vinegar"],
            "ratio": Decimal("1.0"),
            "ratio_verified": False,
            "taste_impact": "Brighter floral acidity compared to lemon.",
            "texture_impact": "Minimal impact.",
            "preparation_changes": "Taste and adjust acidity before service.",
            "dietary_impact": "Neutral dietary impact.",
            "allergen_impact": "No major allergen change.",
        },
    ),
    (
        "coriander",
        {
            "proposed_ingredient": "Flat-Leaf Parsley",
            "search_terms": ["parsley", "cilantro"],
            "ratio": Decimal("1.0"),
            "ratio_verified": False,
            "taste_impact": "Herbaceous, milder finish without citrusy coriander notes.",
            "texture_impact": "Equivalent garnish/chop behavior.",
            "preparation_changes": "Add fresh at finish.",
            "dietary_impact": "Neutral dietary impact.",
            "allergen_impact": "No major allergen change.",
        },
    ),
]


class SubstitutionEngine:
    def __init__(self, index_manager: TenantMaterialIndexManager | None = None) -> None:
        self._index_mgr = index_manager or get_index_manager()
        self._matcher = MaterialIntelligenceEngine(self._index_mgr)
        self._ai_router = AiProviderRouter(
            {
                "LOCAL": LocalOllamaAdapter(),
                "GEMINI": GeminiSdkAdapter(),
                "CLAUDE": ClaudeSdkAdapter(),
            }
        )

    async def suggest_substitutions(self, req: SubstitutionSuggestRequest) -> SubstitutionSuggestResponse:
        if req.materials is not None:
            self._index_mgr.build_index(
                req.tenant_id,
                req.materials,
                req.aliases or [],
                req.config,
                force_rebuild=False,
                persist=False,
            )

        idx = self._index_mgr.get_index(req.tenant_id)
        q_attrs = extract_ingredient_attributes(req.ingredient.description)
        suggestions: list[SubstitutionComparisonItem] = []
        warnings: list[str] = []

        # 1. Alternative Material Matches (same ingredient, different brand/pack/eligible plant item)
        alt_candidates = self._matcher.search_materials(
            MaterialSearchRequest(
                tenant_id=req.tenant_id,
                query=req.ingredient.description,
                property_id=req.ingredient.property_id,
                include_inactive=False,
                limit=6,
                config=req.config,
            )
        )
        current_code = req.current_material.material_code if req.current_material else None
        for cand in alt_candidates:
            if current_code and cand.material_code == current_code:
                continue
            if not cand.eligible:
                continue
            mat_rec = idx.records_by_id.get(cand.material_id)
            if mat_rec is None:
                continue
            uom_val = validate_uom_and_pack(req.ingredient, mat_rec)
            stock_avail = mat_rec.stock_summary.available_qty if (mat_rec.stock_summary and mat_rec.stock_summary.stock_status == "KNOWN") else None
            stock_status = mat_rec.stock_summary.stock_status if mat_rec.stock_summary else "UNKNOWN"
            cost_delta: Decimal | None = None
            currency = mat_rec.price_summary.currency if mat_rec.price_summary else None
            if (
                req.current_material
                and req.current_material.price_summary
                and req.current_material.price_summary.unit_price is not None
                and mat_rec.price_summary
                and mat_rec.price_summary.unit_price is not None
                and uom_val.converted_base_qty is not None
                and req.current_material.price_summary.currency == mat_rec.price_summary.currency
            ):
                cost_delta = ((mat_rec.price_summary.unit_price - req.current_material.price_summary.unit_price) * uom_val.converted_base_qty).quantize(
                    Decimal("0.01"), rounding=ROUND_HALF_UP
                )

            suggestions.append(
                SubstitutionComparisonItem(
                    substitution_kind="ALTERNATIVE_MATERIAL_MATCH",
                    original_ingredient=req.ingredient.description,
                    original_material_code=current_code,
                    proposed_ingredient=mat_rec.description,
                    proposed_material_id=mat_rec.material_id,
                    proposed_material_code=mat_rec.material_code,
                    proposed_material_description=mat_rec.description,
                    suggested_replacement_ratio=Decimal("1.0"),
                    ratio_requires_chef_input=not cand.specification_compatible,
                    converted_quantity=uom_val.converted_base_qty,
                    target_uom=mat_rec.base_uom,
                    uom_compatible=uom_val.compatible,
                    stock_available=stock_avail,
                    stock_status=stock_status,
                    estimated_cost_impact=cost_delta,
                    currency=currency,
                    taste_impact="Equivalent core ingredient; verify brand/fat/pack nuances." if cand.specification_compatible else "; ".join(cand.specification_conflicts),
                    texture_impact="Comparable culinary texture expected." if cand.specification_compatible else "Specification differences may alter texture.",
                    preparation_changes="No recipe method change expected." if cand.specification_compatible else "Adjust seasoning/hydration if specification differs.",
                    dietary_impact="Check packaging dietary certifications.",
                    allergen_impact=", ".join(mat_rec.allergen_info) if mat_rec.allergen_info else "Verify label allergens.",
                    chef_verification_requirements=["Confirm brand/pack compatibility", "Verify UOM conversion" if not uom_val.compatible else "Confirm recipe draft update"],
                    confidence_note="Same-ingredient alternative material in tenant Material Master.",
                )
            )
            if len(suggestions) >= 3:
                break

        # 2. Culinary Substitutions (different ingredient fulfilling same culinary role §26)
        norm_lower = q_attrs.normalized_description.lower()
        for trigger, rule in _CULINARY_SUB_RULES:
            if trigger in norm_lower:
                matched_rec = None
                for term in rule["search_terms"]:
                    hits = self._matcher.search_materials(
                        MaterialSearchRequest(
                            tenant_id=req.tenant_id,
                            query=term,
                            property_id=req.ingredient.property_id,
                            include_inactive=False,
                            limit=3,
                            config=req.config,
                        )
                    )
                    eligible_hits = [h for h in hits if h.eligible]
                    if eligible_hits:
                        matched_rec = idx.records_by_id.get(eligible_hits[0].material_id)
                        break

                ratio: Decimal = rule["ratio"]
                scaled_qty = (req.ingredient.quantity * ratio).quantize(Decimal("0.0001"), rounding=ROUND_HALF_UP)
                converted_qty = scaled_qty
                uom_ok = True
                target_uom = req.ingredient.recipe_uom.upper()
                if matched_rec is not None:
                    sub_line = req.ingredient.model_copy(update={"quantity": scaled_qty})
                    uom_val = validate_uom_and_pack(sub_line, matched_rec)
                    uom_ok = uom_val.compatible
                    converted_qty = uom_val.converted_base_qty
                    target_uom = matched_rec.base_uom

                suggestions.append(
                    SubstitutionComparisonItem(
                        substitution_kind="CULINARY_SUBSTITUTION",
                        original_ingredient=req.ingredient.description,
                        original_material_code=current_code,
                        proposed_ingredient=rule["proposed_ingredient"],
                        proposed_material_id=matched_rec.material_id if matched_rec else None,
                        proposed_material_code=matched_rec.material_code if matched_rec else None,
                        proposed_material_description=matched_rec.description if matched_rec else None,
                        suggested_replacement_ratio=ratio,
                        ratio_requires_chef_input=True,  # §27: uncertain culinary ratios require chef confirmation
                        converted_quantity=converted_qty,
                        target_uom=target_uom,
                        uom_compatible=uom_ok,
                        stock_available=(
                            matched_rec.stock_summary.available_qty
                            if (matched_rec and matched_rec.stock_summary and matched_rec.stock_summary.stock_status == "KNOWN")
                            else None
                        ),
                        stock_status=matched_rec.stock_summary.stock_status if (matched_rec and matched_rec.stock_summary) else "UNKNOWN",
                        estimated_cost_impact=None,
                        currency=matched_rec.price_summary.currency if (matched_rec and matched_rec.price_summary) else None,
                        taste_impact=rule["taste_impact"],
                        texture_impact=rule["texture_impact"],
                        preparation_changes=rule["preparation_changes"],
                        dietary_impact=rule["dietary_impact"],
                        allergen_impact=rule["allergen_impact"],
                        chef_verification_requirements=[
                            "Chef confirmation mandatory before applying culinary substitution to draft revision",
                            "Verify replacement ratio for batch yield",
                        ],
                        confidence_note="Culinary substitution rule; ratio marked for chef verification.",
                    )
                )

        # 3. Optional bounded LLM substitution reasoning when configured
        llm_used = False
        used_provider: str | None = None
        used_model: str | None = None
        if req.config.llm_assisted_enabled and req.provider_config is not None:
            try:
                policy = _build_llm_policy(req.provider_config)
                bounded_mats = [
                    {
                        "material_code": m.material_code,
                        "description": sanitize_untrusted_text(m.description),
                        "base_uom": m.base_uom,
                    }
                    for m in list(idx.records_by_id.values())[:25]
                    if m.is_active and m.plant_eligible
                ]
                schema = {
                    "type": "object",
                    "properties": {
                        "proposed_ingredient": {"type": "string"},
                        "proposed_material_code": {"type": ["string", "null"]},
                        "taste_impact": {"type": "string"},
                        "texture_impact": {"type": "string"},
                        "preparation_changes": {"type": "string"},
                        "dietary_impact": {"type": "string"},
                        "allergen_impact": {"type": "string"},
                    },
                    "required": ["proposed_ingredient", "taste_impact", "texture_impact", "preparation_changes"],
                }
                ai_req = AiExecutionRequest(
                    task_name="INGREDIENT_SUBSTITUTION",
                    prompt_version="phase1b-sub-v1",
                    system_prompt="Suggest one culinary substitution. Only cite a material_code from the supplied candidate list or null.",
                    user_payload={
                        "ingredient": sanitize_untrusted_text(req.ingredient.description),
                        "dietary_constraints": req.dietary_constraints,
                        "allergen_exclusions": req.allergen_exclusions,
                        "candidates": bounded_mats,
                    },
                    output_json_schema=schema,
                    temperature=0.1,
                    max_output_tokens=350,
                )
                resp, _ = await self._ai_router.execute("INGREDIENT_SUBSTITUTION", policy, ai_req)
                llm_used = True
                used_provider = resp.provider
                used_model = resp.model
                raw_code = resp.raw_json.get("proposed_material_code")
                valid_codes = {m["material_code"]: m for m in bounded_mats}
                if raw_code and str(raw_code) not in valid_codes:
                    warnings.append(f"Rejected unverified LLM substitution material code '{raw_code}'.")
                    raw_code = None
                suggestions.append(
                    SubstitutionComparisonItem(
                        substitution_kind="CULINARY_SUBSTITUTION",
                        original_ingredient=req.ingredient.description,
                        original_material_code=current_code,
                        proposed_ingredient=str(resp.raw_json.get("proposed_ingredient") or "Chef-selected alternative"),
                        proposed_material_code=str(raw_code) if raw_code else None,
                        proposed_material_description=valid_codes[str(raw_code)]["description"] if raw_code else None,
                        suggested_replacement_ratio=None,
                        ratio_requires_chef_input=True,
                        uom_compatible=False,
                        taste_impact=str(resp.raw_json.get("taste_impact") or ""),
                        texture_impact=str(resp.raw_json.get("texture_impact") or ""),
                        preparation_changes=str(resp.raw_json.get("preparation_changes") or ""),
                        dietary_impact=str(resp.raw_json.get("dietary_impact") or "Verify dietary compatibility."),
                        allergen_impact=str(resp.raw_json.get("allergen_impact") or "Verify allergen labels."),
                        chef_verification_requirements=["LLM suggestion requires chef confirmation and ratio validation"],
                        confidence_note=f"AI-assisted suggestion ({resp.provider}/{resp.model}); ratio requires chef input.",
                    )
                )
            except ProviderUnavailableError as exc:
                warnings.append(f"AI substitution unavailable: {exc.message}")
            except Exception as exc:
                warnings.append(f"AI substitution fallback notice: {exc}")

        return SubstitutionSuggestResponse(
            temp_ref=req.ingredient.temp_ref,
            original_ingredient=req.ingredient.description,
            suggestions=suggestions,
            llm_used=llm_used,
            provider=used_provider,
            model=used_model,
            warnings=warnings,
        )
