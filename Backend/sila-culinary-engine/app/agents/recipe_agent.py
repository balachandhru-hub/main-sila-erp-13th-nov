from __future__ import annotations

import asyncio
import uuid
from datetime import datetime, timezone
from decimal import Decimal
from app.engines.documents import sanitize_untrusted_text
from app.engines.validation import enrich_and_validate_recipe
from app.prompts.recipe_prompts import (
    PROMPT_VERSION,
    build_generation_prompt,
    build_refinement_prompt,
    build_selective_regen_prompt,
)
from app.providers.adapters import ClaudeSdkAdapter, GeminiSdkAdapter, LocalOllamaAdapter
from app.providers.router import (
    AiExecutionRequest,
    AiProviderAdapter,
    AiProviderRouter,
    ProviderKind,
    ProviderPolicy,
    ProviderUnavailableError,
)
from app.schemas.recipe_ai import (
    AiMetadataContract,
    GeneratedRecipeResponse,
    GenerateRecipeHttpRequest,
    ProviderRuntimeConfig,
    RecipeDiffItem,
    RefineRecipeHttpRequest,
    RefineRecipeHttpResponse,
    RegenerateSectionHttpRequest,
    StructuredRecipePayload,
    ValidationWarning,
)


def _to_policy(cfg: ProviderRuntimeConfig, preferred: ProviderKind | None = None) -> ProviderPolicy:
    if preferred == "LOCAL" and not cfg.local_enabled:
        raise ProviderUnavailableError("UNAUTHORIZED_PROVIDER", "Local AI is not enabled for this tenant.")
    if preferred == "GEMINI" and (not cfg.tenant_external_ai_allowed or not cfg.gemini_enabled):
        raise ProviderUnavailableError("UNAUTHORIZED_PROVIDER", "Gemini is not authorized or enabled for this tenant.")
    if preferred == "CLAUDE" and (not cfg.tenant_external_ai_allowed or not cfg.claude_enabled):
        raise ProviderUnavailableError("UNAUTHORIZED_PROVIDER", "Claude is not authorized or enabled for this tenant.")

    routing: dict[str, ProviderKind] = {}
    for k, v in cfg.task_routing.items():
        if v in ("LOCAL", "GEMINI", "CLAUDE"):
            routing[k] = v  # type: ignore[assignment]

    return ProviderPolicy(
        operating_mode=preferred or cfg.operating_mode,
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
        timeout_seconds=cfg.timeout_seconds,
        max_retries=cfg.max_retries,
        fallback_policy=cfg.fallback_policy,
        task_routing=routing,
    )


def _resolve_task_for_mode(mode: str) -> str:
    if mode == "DOCUMENT":
        return "RECIPE_DOCUMENT_INTERPRETATION"
    if mode == "DISH_IMAGE":
        return "RECIPE_IMAGE_INTERPRETATION"
    return "RECIPE_GENERATION"


def _compute_recipe_diff(original: StructuredRecipePayload, proposed: StructuredRecipePayload) -> list[RecipeDiffItem]:
    diffs: list[RecipeDiffItem] = []
    if original.header.proposed_name != proposed.header.proposed_name:
        diffs.append(RecipeDiffItem(section="header", field="proposed_name", before=original.header.proposed_name, after=proposed.header.proposed_name))
    if original.header.base_servings != proposed.header.base_servings:
        diffs.append(RecipeDiffItem(section="header", field="base_servings", before=str(original.header.base_servings), after=str(proposed.header.base_servings)))
    if original.header.dietary_tags != proposed.header.dietary_tags:
        diffs.append(RecipeDiffItem(section="header", field="dietary_tags", before=", ".join(original.header.dietary_tags), after=", ".join(proposed.header.dietary_tags)))

    orig_by_ref = {i.temp_ref: i for i in original.ingredients}
    prop_by_ref = {i.temp_ref: i for i in proposed.ingredients}

    for ref, orig_line in orig_by_ref.items():
        if ref not in prop_by_ref:
            diffs.append(RecipeDiffItem(section="ingredients", field=ref, before=f"{orig_line.description} ({orig_line.quantity} {orig_line.recipe_uom})", after="[REMOVED]"))
        else:
            p = prop_by_ref[ref]
            if orig_line.description != p.description or orig_line.quantity != p.quantity or orig_line.recipe_uom != p.recipe_uom:
                diffs.append(
                    RecipeDiffItem(
                        section="ingredients",
                        field=ref,
                        before=f"{orig_line.description} ({orig_line.quantity} {orig_line.recipe_uom})",
                        after=f"{p.description} ({p.quantity} {p.recipe_uom})",
                    )
                )
    for ref, prop_line in prop_by_ref.items():
        if ref not in orig_by_ref:
            diffs.append(RecipeDiffItem(section="ingredients", field=ref, before="[NEW]", after=f"{prop_line.description} ({prop_line.quantity} {prop_line.recipe_uom})"))

    if len(original.instructions) != len(proposed.instructions) or any(
        a.instruction != b.instruction for a, b in zip(original.instructions, proposed.instructions)
    ):
        diffs.append(
            RecipeDiffItem(
                section="instructions",
                field="steps",
                before=f"{len(original.instructions)} steps",
                after=f"{len(proposed.instructions)} steps",
            )
        )
    if len(original.sub_recipes) != len(proposed.sub_recipes):
        diffs.append(
            RecipeDiffItem(
                section="sub_recipes",
                field="count",
                before=f"{len(original.sub_recipes)} sub-recipes",
                after=f"{len(proposed.sub_recipes)} sub-recipes",
            )
        )
    return diffs


class RecipeIntelligenceAgent:
    def __init__(self, adapters: dict[ProviderKind, AiProviderAdapter] | None = None) -> None:
        self._adapters: dict[ProviderKind, AiProviderAdapter] = adapters or {
            "LOCAL": LocalOllamaAdapter(),
            "GEMINI": GeminiSdkAdapter(),
            "CLAUDE": ClaudeSdkAdapter(),
        }
        self._router = AiProviderRouter(self._adapters)
        self._cancel_events: dict[str, asyncio.Event] = {}

    def register_cancel(self, generation_ref: str) -> asyncio.Event:
        ev = asyncio.Event()
        self._cancel_events[generation_ref] = ev
        return ev

    def cancel_generation(self, generation_ref: str) -> bool:
        ev = self._cancel_events.get(generation_ref)
        if ev is not None:
            ev.set()
            return True
        return False

    def clear_cancel(self, generation_ref: str) -> None:
        self._cancel_events.pop(generation_ref, None)

    async def generate_recipe(
        self,
        req: GenerateRecipeHttpRequest,
        generation_ref: str | None = None,
    ) -> GeneratedRecipeResponse:
        gen_ref = generation_ref or f"GEN-{uuid.uuid4().hex[:12].upper()}"
        cancel_ev = self.register_cancel(gen_ref)
        try:
            policy = _to_policy(req.provider_config, req.input.preferred_provider)
            task_name = _resolve_task_for_mode(req.input.generation_mode)

            clean_doc: str | None = None
            if req.input.extracted_document_text:
                clean_doc, _ = sanitize_untrusted_text(req.input.extracted_document_text)

            sys_prompt, user_payload = build_generation_prompt(req.input, sanitized_doc_text=clean_doc)

            ai_req = AiExecutionRequest(
                task_name=task_name,
                prompt_version=PROMPT_VERSION,
                system_prompt=sys_prompt,
                user_payload=user_payload,
                max_output_tokens=req.provider_config.max_output_tokens,
                temperature=req.provider_config.temperature,
                image_base64=req.input.image_base64,
                image_mime_type=req.input.image_mime_type,
                output_json_schema=StructuredRecipePayload.model_json_schema(),
            )
            parsed, meta = await self._router.execute_structured(
                policy,
                ai_req,
                StructuredRecipePayload,
                cancel_event=cancel_ev,
            )

            validated, warnings = enrich_and_validate_recipe(parsed)
            if req.input.generation_mode == "DISH_IMAGE":
                warnings.append(
                    ValidationWarning(
                        code="IMAGE_INFERENCE_NOTICE",
                        severity="INFO",
                        field="header",
                        message="Dish photograph interpretation provides AI-inferred suggestions only; visible characteristics cannot guarantee exact ingredients or allergens.",
                    )
                )
            if (
                req.input.generation_mode == "BUDGET_BASED"
                or req.input.target_ingredient_budget is not None
                or req.input.target_cost_per_serving is not None
            ):
                validated.cost_validation_note = "Cost validation pending material matching."
                warnings.append(
                    ValidationWarning(
                        code="COST_VALIDATION_PENDING",
                        severity="INFO",
                        field="header",
                        message="Cost validation pending material matching.",
                    )
                )
            if req.input.generation_mode == "AVAILABLE_INGREDIENTS" and req.input.available_ingredients_context:
                supplied_names = {
                    str(item.get("name") or item.get("description") or "").strip().lower()
                    for item in req.input.available_ingredients_context
                }
                for ing in validated.ingredients:
                    if ing.description.strip().lower() not in supplied_names:
                        ing.preparation_notes = (
                            ing.preparation_notes + " [Additional suggested ingredient; stock availability not validated]"
                        ).strip()

            val_status = "REPAIRED" if meta.validation_retries > 0 else ("VALIDATION_WARNINGS" if warnings else "VALID")
            ai_meta = AiMetadataContract(
                provider=meta.provider,
                model=meta.model,
                prompt_version=meta.prompt_version,
                generation_timestamp=datetime.now(timezone.utc).isoformat(),
                generation_reference=gen_ref,
                assumptions=validated.assumptions,
                warnings=[w.message for w in warnings],
                validation_status=val_status,
                input_tokens=meta.input_tokens,
                output_tokens=meta.output_tokens,
                total_tokens=meta.input_tokens + meta.output_tokens,
                estimated_cost_usd=meta.estimated_cost_usd,
                latency_ms=meta.latency_ms,
                used_fallback=meta.used_fallback,
                validation_retries=meta.validation_retries,
            )

            return GeneratedRecipeResponse(
                payload=validated,
                ai_metadata=ai_meta,
                validation_warnings=warnings,
            )
        finally:
            self.clear_cancel(gen_ref)

    async def refine_recipe(self, req: RefineRecipeHttpRequest) -> RefineRecipeHttpResponse:
        gen_ref = f"REF-{uuid.uuid4().hex[:12].upper()}"
        policy = _to_policy(req.provider_config, req.preferred_provider)
        sys_prompt, user_payload = build_refinement_prompt(req)
        ai_req = AiExecutionRequest(
            task_name="RECIPE_REFINEMENT",
            prompt_version=PROMPT_VERSION,
            system_prompt=sys_prompt,
            user_payload=user_payload,
            max_output_tokens=req.provider_config.max_output_tokens,
            temperature=req.provider_config.temperature,
            output_json_schema=StructuredRecipePayload.model_json_schema(),
        )
        parsed, meta = await self._router.execute_structured(policy, ai_req, StructuredRecipePayload)
        validated, warnings = enrich_and_validate_recipe(parsed)
        diffs = _compute_recipe_diff(req.current_payload, validated)

        ai_meta = AiMetadataContract(
            provider=meta.provider,
            model=meta.model,
            prompt_version=meta.prompt_version,
            generation_timestamp=datetime.now(timezone.utc).isoformat(),
            generation_reference=gen_ref,
            assumptions=validated.assumptions,
            warnings=[w.message for w in warnings],
            validation_status="VALIDATION_WARNINGS" if warnings else "VALID",
            input_tokens=meta.input_tokens,
            output_tokens=meta.output_tokens,
            total_tokens=meta.input_tokens + meta.output_tokens,
            estimated_cost_usd=meta.estimated_cost_usd,
            latency_ms=meta.latency_ms,
            used_fallback=meta.used_fallback,
            validation_retries=meta.validation_retries,
        )

        return RefineRecipeHttpResponse(
            proposed_payload=validated,
            ai_metadata=ai_meta,
            diff_summary=diffs,
            validation_warnings=warnings,
            summary_message=f"Prepared {len(diffs)} proposed change(s) for chef review.",
        )

    async def regenerate_section(self, req: RegenerateSectionHttpRequest) -> GeneratedRecipeResponse:
        gen_ref = f"SEC-{uuid.uuid4().hex[:12].upper()}"
        policy = _to_policy(req.provider_config, req.preferred_provider)
        sys_prompt, user_payload = build_selective_regen_prompt(req)
        task_name = "PREPARATION_INSTRUCTIONS" if any(s in ("instructions", "plating") for s in req.sections) else "RECIPE_GENERATION"
        ai_req = AiExecutionRequest(
            task_name=task_name,
            prompt_version=PROMPT_VERSION,
            system_prompt=sys_prompt,
            user_payload=user_payload,
            max_output_tokens=req.provider_config.max_output_tokens,
            temperature=req.provider_config.temperature,
            output_json_schema=StructuredRecipePayload.model_json_schema(),
        )
        parsed, meta = await self._router.execute_structured(policy, ai_req, StructuredRecipePayload)

        merged = req.current_payload.model_copy(deep=True)
        locked_fields = set(req.locked_fields)
        locked_ing_refs = set(req.locked_ingredient_refs) | {
            line.temp_ref for line in req.current_payload.ingredients if line.locked
        }

        for section in req.sections:
            if section == "description" and "description" not in locked_fields:
                merged.header.description = parsed.header.description
            elif section == "cooking_method" and "cooking_method" not in locked_fields:
                merged.header.cooking_method = parsed.header.cooking_method
            elif section == "dietary":
                if "dietary_tags" not in locked_fields:
                    merged.header.dietary_tags = parsed.header.dietary_tags
                if "potential_allergens" not in locked_fields:
                    merged.header.potential_allergens = parsed.header.potential_allergens
            elif section == "instructions":
                merged.instructions = parsed.instructions
            elif section == "plating":
                merged.header.storage_guidance = parsed.header.storage_guidance
                merged.instructions = parsed.instructions
            elif section == "sub_recipes":
                merged.sub_recipes = parsed.sub_recipes
            elif section in ("ingredients", "alternatives"):
                orig_locked = {
                    line.temp_ref: line
                    for line in req.current_payload.ingredients
                    if line.temp_ref in locked_ing_refs
                }
                new_lines = []
                for p_line in parsed.ingredients:
                    if p_line.temp_ref in orig_locked:
                        new_lines.append(orig_locked[p_line.temp_ref])
                    else:
                        new_lines.append(p_line)
                for ref, locked_line in orig_locked.items():
                    if not any(l.temp_ref == ref for l in new_lines):
                        new_lines.insert(0, locked_line)
                merged.ingredients = new_lines

        validated, warnings = enrich_and_validate_recipe(merged)
        ai_meta = AiMetadataContract(
            provider=meta.provider,
            model=meta.model,
            prompt_version=meta.prompt_version,
            generation_timestamp=datetime.now(timezone.utc).isoformat(),
            generation_reference=gen_ref,
            assumptions=parsed.assumptions,
            warnings=[w.message for w in warnings],
            validation_status="VALIDATION_WARNINGS" if warnings else "VALID",
            input_tokens=meta.input_tokens,
            output_tokens=meta.output_tokens,
            total_tokens=meta.input_tokens + meta.output_tokens,
            estimated_cost_usd=meta.estimated_cost_usd,
            latency_ms=meta.latency_ms,
            used_fallback=meta.used_fallback,
            validation_retries=meta.validation_retries,
        )

        return GeneratedRecipeResponse(
            payload=validated,
            ai_metadata=ai_meta,
            validation_warnings=warnings,
        )
