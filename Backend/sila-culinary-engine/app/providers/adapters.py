from __future__ import annotations

import json
import os
import re
from decimal import Decimal
import anthropic
import httpx
from google import genai
from google.genai import types as genai_types
from app.providers.router import (
    AiExecutionRequest,
    AiProviderAdapter,
    AiProviderResponse,
    ProviderKind,
    ProviderPolicy,
    ProviderUnavailableError,
)


def _extract_json_object(raw_text: str) -> dict:
    cleaned = raw_text.strip()
    if cleaned.startswith("```"):
        cleaned = re.sub(r"^```(?:json)?\s*", "", cleaned)
        cleaned = re.sub(r"\s*```$", "", cleaned)
    try:
        parsed = json.loads(cleaned)
        if isinstance(parsed, dict):
            return parsed
    except json.JSONDecodeError:
        pass
    start = cleaned.find("{")
    end = cleaned.rfind("}")
    if start != -1 and end > start:
        parsed = json.loads(cleaned[start : end + 1])
        if isinstance(parsed, dict):
            return parsed
    raise ValueError("Model response did not contain a valid JSON object.")


def estimate_cost_usd(
    policy: ProviderPolicy,
    provider: ProviderKind,
    model: str,
    input_tokens: int,
    output_tokens: int,
) -> Decimal | None:
    """
    Uses configurable pricing from policy.pricing_overrides when supplied.
    Returns None when no pricing rate is configured so UI displays 'Unavailable' rather than stale hardcoded pricing.
    """
    rates = policy.pricing_overrides.get(model) or policy.pricing_overrides.get(provider)
    if not rates:
        return None
    in_per_1m = Decimal(str(rates.get("input_per_1m", "0")))
    out_per_1m = Decimal(str(rates.get("output_per_1m", "0")))
    cost = (Decimal(input_tokens) * in_per_1m + Decimal(output_tokens) * out_per_1m) / Decimal("1000000")
    return cost.quantize(Decimal("0.000001"))


class LocalOllamaAdapter(AiProviderAdapter):
    kind: ProviderKind = "LOCAL"

    async def invoke_structured(
        self,
        request: AiExecutionRequest,
        model_name: str,
        timeout_seconds: float,
        policy: ProviderPolicy | None = None,
    ) -> AiProviderResponse:
        endpoint = (policy.local_endpoint if policy else None) or os.getenv("SILA_CULINARY_LOCAL_ENDPOINT", "")
        endpoint = endpoint.strip().rstrip("/")
        if not endpoint:
            raise ProviderUnavailableError("LOCAL_NOT_CONFIGURED", "Local AI endpoint is not configured.")

        messages: list[dict] = [
            {"role": "system", "content": request.system_prompt},
            {
                "role": "user",
                "content": json.dumps(request.user_payload, ensure_ascii=False, default=str),
            },
        ]
        if request.repair_hint:
            messages.append({"role": "user", "content": f"Repair JSON strictly matching schema: {request.repair_hint}"})

        # Supports Ollama native /api/chat and OpenAI-compatible /v1/chat/completions endpoints
        is_openai_compat = endpoint.endswith("/v1") or "/v1/" in endpoint
        async with httpx.AsyncClient(timeout=httpx.Timeout(timeout_seconds)) as client:
            if is_openai_compat:
                url = f"{endpoint}/chat/completions" if endpoint.endswith("/v1") else endpoint
                body: dict = {
                    "model": model_name,
                    "messages": messages,
                    "temperature": request.temperature,
                    "max_tokens": request.max_output_tokens,
                    "response_format": {"type": "json_object"},
                }
                if request.image_base64 and request.image_mime_type:
                    messages[1] = {
                        "role": "user",
                        "content": [
                            {"type": "text", "text": json.dumps(request.user_payload, ensure_ascii=False, default=str)},
                            {
                                "type": "image_url",
                                "image_url": {"url": f"data:{request.image_mime_type};base64,{request.image_base64}"},
                            },
                        ],
                    }
                resp = await client.post(url, json=body)
                if resp.status_code >= 400:
                    raise ProviderUnavailableError("LOCAL_HTTP_ERROR", f"Local endpoint returned HTTP {resp.status_code}.")
                data = resp.json()
                text = data["choices"][0]["message"]["content"]
                usage = data.get("usage", {})
                in_tok = int(usage.get("prompt_tokens", 0))
                out_tok = int(usage.get("completion_tokens", 0))
            else:
                url = f"{endpoint}/api/chat"
                ollama_messages = list(messages)
                if request.image_base64:
                    ollama_messages[1] = {
                        "role": "user",
                        "content": json.dumps(request.user_payload, ensure_ascii=False, default=str),
                        "images": [request.image_base64],
                    }
                body = {
                    "model": model_name,
                    "messages": ollama_messages,
                    "stream": False,
                    "format": request.output_json_schema or "json",
                    "options": {
                        "temperature": request.temperature,
                        "num_predict": request.max_output_tokens,
                    },
                }
                resp = await client.post(url, json=body)
                if resp.status_code >= 400:
                    raise ProviderUnavailableError("LOCAL_HTTP_ERROR", f"Local Ollama endpoint returned HTTP {resp.status_code}.")
                data = resp.json()
                text = (data.get("message") or {}).get("content", "")
                in_tok = int(data.get("prompt_eval_count", 0) or 0)
                out_tok = int(data.get("eval_count", 0) or 0)

        parsed = _extract_json_object(text)
        cost = estimate_cost_usd(policy, "LOCAL", model_name, in_tok, out_tok) if policy else None
        return AiProviderResponse(
            provider="LOCAL",
            model=model_name,
            prompt_version=request.prompt_version,
            raw_json=parsed,
            input_tokens=in_tok,
            output_tokens=out_tok,
            estimated_cost_usd=cost if cost is not None else Decimal("0"),
            cost_available=cost is not None,
        )


class GeminiSdkAdapter(AiProviderAdapter):
    kind: ProviderKind = "GEMINI"

    async def invoke_structured(
        self,
        request: AiExecutionRequest,
        model_name: str,
        timeout_seconds: float,
        policy: ProviderPolicy | None = None,
    ) -> AiProviderResponse:
        api_key = (policy.gemini_api_key if policy else None) or os.getenv("GEMINI_API_KEY") or os.getenv("GOOGLE_API_KEY")
        if not api_key:
            raise ProviderUnavailableError("GEMINI_NO_KEY", "Gemini API key is not configured.")

        client = genai.Client(api_key=api_key)
        contents: list = [json.dumps(request.user_payload, ensure_ascii=False, default=str)]
        if request.repair_hint:
            contents.append(f"Ensure valid JSON strictly satisfying schema: {request.repair_hint}")
        if request.image_base64 and request.image_mime_type:
            import base64

            contents.append(
                genai_types.Part.from_bytes(
                    data=base64.b64decode(request.image_base64),
                    mime_type=request.image_mime_type,
                )
            )

        config = genai_types.GenerateContentConfig(
            system_instruction=request.system_prompt,
            temperature=request.temperature,
            max_output_tokens=request.max_output_tokens,
            response_mime_type="application/json",
        )
        if request.output_json_schema:
            config.response_json_schema = request.output_json_schema

        resp = await client.aio.models.generate_content(
            model=model_name,
            contents=contents,
            config=config,
        )
        parsed = _extract_json_object(resp.text or "")
        um = getattr(resp, "usage_metadata", None)
        in_tok = int(getattr(um, "prompt_token_count", 0) or 0)
        out_tok = int(getattr(um, "candidates_token_count", 0) or 0)
        cost = estimate_cost_usd(policy, "GEMINI", model_name, in_tok, out_tok) if policy else None
        return AiProviderResponse(
            provider="GEMINI",
            model=model_name,
            prompt_version=request.prompt_version,
            raw_json=parsed,
            input_tokens=in_tok,
            output_tokens=out_tok,
            estimated_cost_usd=cost if cost is not None else Decimal("0"),
            cost_available=cost is not None,
        )


class ClaudeSdkAdapter(AiProviderAdapter):
    kind: ProviderKind = "CLAUDE"

    async def invoke_structured(
        self,
        request: AiExecutionRequest,
        model_name: str,
        timeout_seconds: float,
        policy: ProviderPolicy | None = None,
    ) -> AiProviderResponse:
        api_key = (policy.claude_api_key if policy else None) or os.getenv("ANTHROPIC_API_KEY")
        if not api_key:
            raise ProviderUnavailableError("CLAUDE_NO_KEY", "Anthropic Claude API key is not configured.")

        client = anthropic.AsyncAnthropic(api_key=api_key, timeout=timeout_seconds)
        content_blocks: list[dict] = []
        if request.image_base64 and request.image_mime_type:
            content_blocks.append(
                {
                    "type": "image",
                    "source": {
                        "type": "base64",
                        "media_type": request.image_mime_type,
                        "data": request.image_base64,
                    },
                }
            )
        user_text = json.dumps(request.user_payload, ensure_ascii=False, default=str)
        if request.repair_hint:
            user_text += f"\n\nRepair strictly: {request.repair_hint}"
        content_blocks.append({"type": "text", "text": user_text})

        tool_schema = request.output_json_schema or {"type": "object"}
        resp = await client.messages.create(
            model=model_name,
            max_tokens=request.max_output_tokens,
            temperature=request.temperature,
            system=request.system_prompt,
            tools=[
                {
                    "name": "emit_structured_culinary_output",
                    "description": "Return the validated structured culinary JSON payload.",
                    "input_schema": tool_schema,
                }
            ],
            tool_choice={"type": "tool", "name": "emit_structured_culinary_output"},
            messages=[{"role": "user", "content": content_blocks}],
        )

        parsed: dict | None = None
        for block in resp.content:
            if getattr(block, "type", None) == "tool_use" and isinstance(getattr(block, "input", None), dict):
                parsed = block.input
                break
        if parsed is None:
            text_parts = [getattr(b, "text", "") for b in resp.content if getattr(b, "type", None) == "text"]
            parsed = _extract_json_object("\n".join(text_parts))

        usage = getattr(resp, "usage", None)
        in_tok = int(getattr(usage, "input_tokens", 0) or 0)
        out_tok = int(getattr(usage, "output_tokens", 0) or 0)
        cost = estimate_cost_usd(policy, "CLAUDE", model_name, in_tok, out_tok) if policy else None
        return AiProviderResponse(
            provider="CLAUDE",
            model=model_name,
            prompt_version=request.prompt_version,
            raw_json=parsed,
            input_tokens=in_tok,
            output_tokens=out_tok,
            estimated_cost_usd=cost if cost is not None else Decimal("0"),
            cost_available=cost is not None,
        )


async def test_provider_connection(policy: ProviderPolicy, target: ProviderKind) -> dict:
    """
    Tests provider availability / configuration without running full recipe generation (§7, §11).
    """
    if target == "LOCAL":
        endpoint = (policy.local_endpoint or "").strip().rstrip("/")
        if not endpoint:
            return {"provider": "LOCAL", "reachable": False, "modelAvailable": False, "detail": "Local AI endpoint is not configured."}
        try:
            async with httpx.AsyncClient(timeout=httpx.Timeout(5.0)) as client:
                tags_url = f"{endpoint[:-3]}/api/tags" if endpoint.endswith("/v1") else f"{endpoint}/api/tags"
                resp = await client.get(tags_url)
                if resp.status_code == 200:
                    models = [m.get("name", "") for m in (resp.json().get("models") or [])]
                    avail = any(policy.local_model in m or m.startswith(policy.local_model.split(":")[0]) for m in models)
                    return {
                        "provider": "LOCAL",
                        "reachable": True,
                        "modelAvailable": avail,
                        "installedModels": models,
                        "detail": (
                            f"Connected to local runtime; model '{policy.local_model}' is available."
                            if avail
                            else f"Connected to local runtime, but configured model '{policy.local_model}' is not installed."
                        ),
                    }
                # Fallback check for OpenAI-compatible /v1/models
                v1_url = f"{endpoint}/models" if endpoint.endswith("/v1") else f"{endpoint}/v1/models"
                resp2 = await client.get(v1_url)
                if resp2.status_code == 200:
                    models = [m.get("id", "") for m in (resp2.json().get("data") or [])]
                    avail = policy.local_model in models
                    return {
                        "provider": "LOCAL",
                        "reachable": True,
                        "modelAvailable": avail,
                        "installedModels": models,
                        "detail": f"Connected to OpenAI-compatible local runtime ({len(models)} models listed).",
                    }
                return {"provider": "LOCAL", "reachable": False, "modelAvailable": False, "detail": f"Local endpoint returned HTTP {resp.status_code}."}
        except Exception as exc:
            return {"provider": "LOCAL", "reachable": False, "modelAvailable": False, "detail": f"Local AI runtime unreachable: {exc}"}

    if target == "GEMINI":
        if not policy.tenant_external_ai_allowed:
            return {"provider": "GEMINI", "reachable": False, "modelAvailable": False, "detail": "External AI is disabled for this tenant."}
        key = policy.gemini_api_key or os.getenv("GEMINI_API_KEY") or os.getenv("GOOGLE_API_KEY")
        if not key and not policy.gemini_api_key_present:
            return {"provider": "GEMINI", "reachable": False, "modelAvailable": False, "detail": "Gemini API key is not configured."}
        if not key:
            return {"provider": "GEMINI", "reachable": False, "modelAvailable": False, "detail": "Gemini credential reference has no runtime key."}
        try:
            client = genai.Client(api_key=key)
            model_info = await client.aio.models.get(model=policy.gemini_model)
            return {
                "provider": "GEMINI",
                "reachable": True,
                "modelAvailable": True,
                "detail": f"Gemini model '{getattr(model_info, 'name', policy.gemini_model)}' verified.",
            }
        except Exception as exc:
            return {"provider": "GEMINI", "reachable": False, "modelAvailable": False, "detail": f"Gemini check failed: {exc}"}

    if target == "CLAUDE":
        if not policy.tenant_external_ai_allowed:
            return {"provider": "CLAUDE", "reachable": False, "modelAvailable": False, "detail": "External AI is disabled for this tenant."}
        key = policy.claude_api_key or os.getenv("ANTHROPIC_API_KEY")
        if not key and not policy.claude_api_key_present:
            return {"provider": "CLAUDE", "reachable": False, "modelAvailable": False, "detail": "Claude API key is not configured."}
        if not key:
            return {"provider": "CLAUDE", "reachable": False, "modelAvailable": False, "detail": "Claude credential reference has no runtime key."}
        try:
            client = anthropic.AsyncAnthropic(api_key=key, timeout=8.0)
            model_info = await client.models.retrieve(policy.claude_model)
            return {
                "provider": "CLAUDE",
                "reachable": True,
                "modelAvailable": True,
                "detail": f"Claude model '{getattr(model_info, 'id', policy.claude_model)}' verified.",
            }
        except Exception as exc:
            return {"provider": "CLAUDE", "reachable": False, "modelAvailable": False, "detail": f"Claude check failed: {exc}"}

    return {"provider": target, "reachable": False, "modelAvailable": False, "detail": "Unsupported provider."}
