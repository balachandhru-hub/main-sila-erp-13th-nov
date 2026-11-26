from __future__ import annotations

import asyncio
import json
import time
from abc import ABC, abstractmethod
from decimal import Decimal
from typing import Literal, Type, TypeVar
from pydantic import BaseModel, Field, ValidationError

OperatingMode = Literal["LOCAL", "GEMINI", "CLAUDE", "HYBRID"]
ProviderKind = Literal["LOCAL", "GEMINI", "CLAUDE"]
FallbackPolicy = Literal["NONE", "LOCAL_ONLY", "CONFIGURED_ORDER"]

TModel = TypeVar("TModel", bound=BaseModel)


class ProviderPolicy(BaseModel):
    operating_mode: OperatingMode = "LOCAL"
    tenant_id: str
    local_enabled: bool = True
    local_endpoint: str | None = None
    local_model: str = "qwen2.5:7b-instruct"
    gemini_enabled: bool = False
    gemini_model: str = "gemini-2.5-flash"
    gemini_api_key_present: bool = False
    claude_enabled: bool = False
    claude_model: str = "claude-sonnet-4-20250514"
    claude_api_key_present: bool = False
    tenant_external_ai_allowed: bool = False
    timeout_seconds: float = Field(default=30.0, gt=0.0, le=120.0)
    max_retries: int = Field(default=1, ge=0, le=3)
    fallback_policy: FallbackPolicy = "NONE"
    task_routing: dict[str, ProviderKind] = Field(default_factory=dict)


class AiExecutionRequest(BaseModel):
    task_name: str
    prompt_version: str = "v1.0"
    system_prompt: str
    user_payload: dict
    max_output_tokens: int = Field(default=2048, ge=64, le=8192)
    temperature: float = Field(default=0.2, ge=0.0, le=1.0)


class AiProviderResponse(BaseModel):
    provider: ProviderKind
    model: str
    prompt_version: str
    raw_json: dict
    input_tokens: int = 0
    output_tokens: int = 0
    estimated_cost_usd: Decimal = Decimal("0")
    latency_ms: int = 0
    used_fallback: bool = False


class ProviderUnavailableError(RuntimeError):
    def __init__(self, code: str, message: str) -> None:
        super().__init__(message)
        self.code = code
        self.message = message


class AiProviderAdapter(ABC):
    kind: ProviderKind

    @abstractmethod
    async def invoke_structured(
        self,
        request: AiExecutionRequest,
        model_name: str,
        timeout_seconds: float,
    ) -> AiProviderResponse:
        raise NotImplementedError


def resolve_candidate_providers(policy: ProviderPolicy, task_name: str) -> list[tuple[ProviderKind, str]]:
    """
    Determine ordered providers respecting tenant privacy and explicit enablement.
    Never routes customer data to an external provider unless explicitly enabled + permitted.
    """
    candidates: list[ProviderKind] = []
    if policy.operating_mode == "LOCAL":
        candidates = ["LOCAL"]
    elif policy.operating_mode == "GEMINI":
        candidates = ["GEMINI"]
    elif policy.operating_mode == "CLAUDE":
        candidates = ["CLAUDE"]
    elif policy.operating_mode == "HYBRID":
        preferred = policy.task_routing.get(task_name, "LOCAL")
        candidates = [preferred]

    if policy.fallback_policy == "LOCAL_ONLY" and "LOCAL" not in candidates:
        candidates.append("LOCAL")
    elif policy.fallback_policy == "CONFIGURED_ORDER":
        for p in ("LOCAL", "GEMINI", "CLAUDE"):
            if p not in candidates:
                candidates.append(p)

    authorized: list[tuple[ProviderKind, str]] = []
    for kind in candidates:
        if kind == "LOCAL":
            if policy.local_enabled and (policy.local_endpoint or "").strip():
                authorized.append(("LOCAL", policy.local_model))
        elif kind == "GEMINI":
            if policy.tenant_external_ai_allowed and policy.gemini_enabled and policy.gemini_api_key_present:
                authorized.append(("GEMINI", policy.gemini_model))
        elif kind == "CLAUDE":
            if policy.tenant_external_ai_allowed and policy.claude_enabled and policy.claude_api_key_present:
                authorized.append(("CLAUDE", policy.claude_model))
    return authorized


class AiProviderRouter:
    def __init__(self, adapters: dict[ProviderKind, AiProviderAdapter]) -> None:
        self._adapters = adapters

    async def execute_structured(
        self,
        policy: ProviderPolicy,
        request: AiExecutionRequest,
        output_schema: Type[TModel],
        cancel_event: asyncio.Event | None = None,
    ) -> tuple[TModel, AiProviderResponse]:
        if cancel_event and cancel_event.is_set():
            raise ProviderUnavailableError("CANCELLED", "AI execution was cancelled before dispatch.")

        targets = resolve_candidate_providers(policy, request.task_name)
        if not targets:
            raise ProviderUnavailableError(
                "NO_AUTHORIZED_PROVIDER",
                "No authorized AI provider is enabled or configured for this tenant and task.",
            )

        last_error: str | None = None
        for idx, (provider_kind, model_name) in enumerate(targets):
            adapter = self._adapters.get(provider_kind)
            if adapter is None:
                last_error = f"Provider adapter {provider_kind} is not registered."
                continue

            for attempt in range(policy.max_retries + 1):
                if cancel_event and cancel_event.is_set():
                    raise ProviderUnavailableError("CANCELLED", "AI execution cancelled.")
                start = time.monotonic()
                try:
                    raw = await asyncio.wait_for(
                        adapter.invoke_structured(request, model_name, policy.timeout_seconds),
                        timeout=policy.timeout_seconds,
                    )
                    validated = output_schema.model_validate(raw.raw_json)
                    raw.latency_ms = int((time.monotonic() - start) * 1000)
                    raw.used_fallback = idx > 0
                    return validated, raw
                except asyncio.TimeoutError:
                    last_error = f"{provider_kind} timed out after {policy.timeout_seconds}s."
                except (ValidationError, json.JSONDecodeError, ValueError) as exc:
                    last_error = f"{provider_kind} returned invalid structured JSON: {exc}"
                except Exception as exc:
                    last_error = f"{provider_kind} invocation failed: {exc}"

        raise ProviderUnavailableError(
            "ALL_PROVIDERS_FAILED",
            last_error or "All authorized AI providers failed. No AI output was fabricated.",
        )
