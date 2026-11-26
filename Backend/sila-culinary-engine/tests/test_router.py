import asyncio
import pytest
from pydantic import BaseModel
from app.providers.router import (
    AiExecutionRequest,
    AiProviderAdapter,
    AiProviderResponse,
    AiProviderRouter,
    ProviderPolicy,
    ProviderUnavailableError,
    resolve_candidate_providers,
)


class SampleSchema(BaseModel):
    dish_name: str
    servings: int


class FakeAdapter(AiProviderAdapter):
    def __init__(self, kind, payload=None, fail=False):
        self.kind = kind
        self._payload = payload or {}
        self._fail = fail

    async def invoke_structured(self, request, model_name, timeout_seconds):
        if self._fail:
            raise RuntimeError("boom")
        return AiProviderResponse(
            provider=self.kind,
            model=model_name,
            prompt_version=request.prompt_version,
            raw_json=self._payload,
        )


def test_privacy_blocks_external_provider_when_not_allowed():
    policy = ProviderPolicy(
        operating_mode="GEMINI",
        tenant_id="org-1",
        gemini_enabled=True,
        gemini_api_key_present=True,
        tenant_external_ai_allowed=False,
        local_enabled=False,
    )
    assert resolve_candidate_providers(policy, "RECIPE_GENERATION") == []


def test_router_validates_structured_json_and_never_fabricates():
    router = AiProviderRouter({"LOCAL": FakeAdapter("LOCAL", {"dish_name": "Soup", "servings": 4})})
    policy = ProviderPolicy(
        operating_mode="LOCAL",
        tenant_id="org-1",
        local_enabled=True,
        local_endpoint="http://127.0.0.1:11434",
    )
    req = AiExecutionRequest(task_name="RECIPE_GENERATION", system_prompt="sys", user_payload={"dish": "Soup"})
    parsed, meta = asyncio.run(router.execute_structured(policy, req, SampleSchema))
    assert parsed.dish_name == "Soup"
    assert meta.provider == "LOCAL"
    assert meta.used_fallback is False


def test_router_raises_actionable_error_when_all_providers_fail():
    router = AiProviderRouter({"LOCAL": FakeAdapter("LOCAL", fail=True)})
    policy = ProviderPolicy(
        operating_mode="LOCAL",
        tenant_id="org-1",
        local_enabled=True,
        local_endpoint="http://127.0.0.1:11434",
    )
    req = AiExecutionRequest(task_name="RECIPE_GENERATION", system_prompt="sys", user_payload={})
    with pytest.raises(ProviderUnavailableError) as exc:
        asyncio.run(router.execute_structured(policy, req, SampleSchema))
    assert exc.value.code == "ALL_PROVIDERS_FAILED"
