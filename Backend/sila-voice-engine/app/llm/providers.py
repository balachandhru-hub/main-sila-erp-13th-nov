"""LLM abstraction. Default rules provider — no DB writes / SAP / unrestricted tools."""

from __future__ import annotations

import re

from app.interfaces.protocols import LlmTurnResult
from app.settings import VoiceSettings


_CANCEL = re.compile(r"\b(cancel|stop|never ?mind|abort)\b", re.I)
_CONFIRM = re.compile(r"\b(confirm purchase order|confirm the (po|order)|create the (po|purchase order))\b", re.I)
_DEFAULT = re.compile(r"\b(go with default|use (my )?default|default order|usual order)\b", re.I)
_WAKE_PREFIX = re.compile(
    r"^\s*(?:hey|hi|hello|okay|ok|good\s+(?:morning|afternoon|evening))\s*,?\s*sila[,!]?\s*",
    re.I,
)
_PURE_WAKE = re.compile(
    r"^\s*(?:hey|hi|hello|okay|ok|good\s+(?:morning|afternoon|evening))\s*,?\s*sila[,!]?\s*$",
    re.I,
)
_AVAIL = re.compile(r"^\s*(?:sila[,!]?\s+)?(?:are you there|can you hear me|you there)\??\s*$", re.I)
_SOCIAL = re.compile(r"^\s*(?:how are you|how'?s it going|what'?s up)\b", re.I)
_GRATITUDE = re.compile(r"^\s*(?:thanks|thank you|thx)\b", re.I)
_FAREWELL = re.compile(
    r"^\s*(?:no[, ]?(?:thank you|thanks)?|that'?s all|nothing else|goodbye|bye(?:\s+sila)?|stop(?:\s+sila)?|end conversation)\b",
    re.I,
)


class RulesLlm:
    """Deterministic intent layer for PURCHASE_ORDER_AGENT until an approved LLM is configured."""

    async def interpret(
        self,
        *,
        transcript: str,
        session_state: dict,
        allowed_intents: list[str],
        language: str | None = None,
    ) -> LlmTurnResult:
        raw = (transcript or "").strip()
        agent = "PURCHASE_ORDER_AGENT" if "PURCHASE_ORDER_AGENT" in allowed_intents else None

        if _AVAIL.search(raw):
            return LlmTurnResult(
                intent="AVAILABILITY",
                spoken_reply="Yes, I'm here. Go ahead.",
                agent_id=None,
            )
        if _PURE_WAKE.search(raw):
            hey = bool(re.match(r"^\s*hey\b", raw, re.I))
            return LlmTurnResult(
                intent="WAKE_ACK" if hey else "GREETING",
                spoken_reply=(
                    "Hi! I'm here. How can I help you today?"
                    if hey
                    else "Hello! What can I help you with?"
                ),
                agent_id=None,
            )
        if _SOCIAL.search(raw):
            return LlmTurnResult(
                intent="SOCIAL",
                spoken_reply="I'm doing well, thank you. How can I help with purchasing?",
                agent_id=None,
            )
        if _FAREWELL.search(raw):
            return LlmTurnResult(
                intent="FAREWELL",
                spoken_reply="You're welcome. Have a great day!",
                agent_id=None,
            )
        if _GRATITUDE.search(raw):
            return LlmTurnResult(
                intent="GRATITUDE",
                spoken_reply="You're welcome.",
                agent_id=None,
            )

        text = _WAKE_PREFIX.sub("", raw).strip()
        lower = text.lower()

        if not text:
            return LlmTurnResult(
                intent="ASK_REPEAT",
                spoken_reply="I did not catch that. Please say the material and quantity again.",
                agent_id=agent,
                needs_clarification=True,
                clarification="Please repeat your request.",
            )
        if _CANCEL.search(lower):
            return LlmTurnResult(intent="CANCEL", spoken_reply="Cancelling this conversation.", agent_id=agent)
        if re.search(r"\b(create|plan|optimize)\b.*\b(menu|buffet|breakfast|lunch|dinner|food cost)\b|\breduce\s+food\s+cost\b", lower):
            return LlmTurnResult(
                intent="MENU_CREATE_OBJECTIVE",
                spoken_reply="Running ME Studio intelligent optimization.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\b(consum(ing|ption)|more ingredients than expected|variance)\b", lower):
            return LlmTurnResult(
                intent="MENU_CONSUMPTION_ANOMALY",
                spoken_reply="Analyzing ingredient consumption anomalies.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\b(purchase|buy)\b.*\b(next week|ingredients?)\b", lower):
            return LlmTurnResult(
                intent="MENU_PURCHASE_NEXT_WEEK",
                spoken_reply="Checking time-phased ingredient purchase requirements.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\bcompare\b.*\b(last month|historical|best[- ]performing)\b", lower):
            return LlmTurnResult(
                intent="MENU_COMPARE_HISTORY",
                spoken_reply="Comparing with best-performing historical menus.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\b(today'?s|show)\b.*\b(breakfast|lunch|dinner|brunch|menu)\b", lower):
            return LlmTurnResult(
                intent="MENU_TODAY_MEAL",
                spoken_reply="Fetching today's menu schedule.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\b(ingredient requirements?|calculate)\b.*\bcovers?\b", lower):
            return LlmTurnResult(
                intent="MENU_CALC_COVERS",
                spoken_reply="Calculating simulated ingredient requirements.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\b(low margin|low margins|high food cost)\b", lower):
            return LlmTurnResult(
                intent="MENU_LOW_MARGIN",
                spoken_reply="Checking menu item margins.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if re.search(r"\b(alternative|substitute)\b", lower):
            return LlmTurnResult(
                intent="MENU_ALTERNATIVE",
                spoken_reply="Checking authorized substitution recommendations.",
                agent_id="MENU_ENGINEERING_AGENT",
                structured={"transcript": text},
            )
        if _CONFIRM.search(lower):
            return LlmTurnResult(
                intent="CONFIRM_PO",
                spoken_reply="Confirming the purchase order draft.",
                agent_id=agent,
                tool_calls=[{"name": "CreatePurchaseOrderDraft", "args": {"confirmation": True}}],
            )
        if _DEFAULT.search(lower):
            return LlmTurnResult(
                intent="GO_WITH_DEFAULT",
                spoken_reply="Applying your purchasing defaults and recent eligible order history.",
                agent_id=agent,
                tool_calls=[{"name": "GetRecentPurchaseHistory", "args": {}}, {"name": "ValidatePurchaseDraft", "args": {}}],
                structured={"go_with_default": True, "transcript": text},
            )
        if any(k in lower for k in ("add ", "order ", "buy ", "need ", "want ", "get ")):
            return LlmTurnResult(
                intent="ADD_PO_LINE",
                spoken_reply="Resolving materials against the Material Master.",
                agent_id=agent,
                tool_calls=[{"name": "SearchMaterials", "args": {"query": text}}],
                structured={"transcript": text},
            )
        return LlmTurnResult(
            intent="PO_UTTERANCE",
            spoken_reply="Working on that purchasing request.",
            agent_id=agent,
            tool_calls=[{"name": "SearchMaterials", "args": {"query": text}}],
            structured={"transcript": text},
        )


class OllamaLlm:
    def __init__(self, endpoint: str, model: str) -> None:
        self.endpoint = endpoint.rstrip("/")
        self.model = model

    async def interpret(
        self,
        *,
        transcript: str,
        session_state: dict,
        allowed_intents: list[str],
        language: str | None = None,
    ) -> LlmTurnResult:
        # Structured tool calls only — never direct DB/SAP. Fall back to rules on failure.
        fallback = RulesLlm()
        try:
            import httpx

            prompt = (
                "You are SILA Voice for enterprise purchasing. "
                "Return a short spoken reply. Do not invent materials, quantities, or accounting values. "
                f"Allowed intents: {', '.join(allowed_intents)}. User said: {transcript}"
            )
            async with httpx.AsyncClient(timeout=20.0) as client:
                response = await client.post(
                    f"{self.endpoint}/api/generate",
                    json={"model": self.model, "prompt": prompt, "stream": False},
                )
                response.raise_for_status()
                body = response.json()
                spoken = (body.get("response") or "").strip() or "Working on that."
            base = await fallback.interpret(
                transcript=transcript,
                session_state=session_state,
                allowed_intents=allowed_intents,
                language=language,
            )
            base.spoken_reply = spoken[:400]
            return base
        except Exception:
            return await fallback.interpret(
                transcript=transcript,
                session_state=session_state,
                allowed_intents=allowed_intents,
                language=language,
            )


def build_llm_provider(settings: VoiceSettings):
    if settings.llm_provider in {"ollama", "llama-cpp", "openai-compatible"} and settings.llm_endpoint:
        return OllamaLlm(settings.llm_endpoint, settings.llm_model or "llama3.2")
    return RulesLlm()
