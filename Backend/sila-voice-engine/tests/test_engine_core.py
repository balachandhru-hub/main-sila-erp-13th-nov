"""Unit tests for the isolated voice engine (no live SAP, mocked Buyer tools)."""

from __future__ import annotations

import array
import math

import pytest
from fastapi.testclient import TestClient

from app.audio.pipeline import AudioPipeline, EnergyVad, normalize_pcm16
from app.conversation.manager import ConversationManager, ConversationState
from app.llm.providers import RulesLlm
from app.main import app
from app.matching.materials import MaterialMatcher
from app.wakeword.detector import PhraseWakeWord


def _pcm_sine(seconds: float = 0.5, rate: int = 16000, amp: int = 4000) -> bytes:
    samples = array.array("h")
    for i in range(int(rate * seconds)):
        samples.append(int(amp * math.sin(2 * math.pi * 440 * i / rate)))
    return samples.tobytes()


def test_energy_vad_and_quality():
    pipe = AudioPipeline(vad=EnergyVad(threshold=200))
    frame = normalize_pcm16(_pcm_sine())
    result = pipe.process(frame)
    assert result.speech_detected is True
    assert result.quality.ask_to_repeat is False


def test_low_volume_asks_repeat():
    pipe = AudioPipeline(vad=EnergyVad(threshold=50))
    quiet = normalize_pcm16(_pcm_sine(amp=50))
    result = pipe.process(quiet)
    assert result.quality.low_volume or result.quality.ask_to_repeat


def test_wake_word_transcript_only():
    wake = PhraseWakeWord("hey sila", 0.5)
    assert wake.detect(normalize_pcm16(b"\x00\x00")) is False
    assert wake.match_transcript("Hey SILA order rice") is True
    assert wake.match_transcript("order rice") is False


@pytest.mark.asyncio
async def test_rules_llm_intents():
    llm = RulesLlm()
    cancel = await llm.interpret(transcript="cancel please", session_state={}, allowed_intents=["CANCEL"])
    assert cancel.intent == "CANCEL"
    confirm = await llm.interpret(
        transcript="confirm purchase order",
        session_state={},
        allowed_intents=["CONFIRM_PO"],
    )
    assert confirm.intent == "CONFIRM_PO"
    default = await llm.interpret(
        transcript="go with default",
        session_state={},
        allowed_intents=["GO_WITH_DEFAULT"],
    )
    assert default.intent == "GO_WITH_DEFAULT"


def test_material_matcher_ambiguity():
    matcher = MaterialMatcher()
    catalog = [
        {"materialCode": "1001", "description": "Coca Cola 330ml"},
        {"materialCode": "1002", "description": "Coca Cola 500ml"},
        {"materialCode": "2001", "description": "Basmati Rice 5kg"},
    ]
    cokes = matcher.rank("coca cola", catalog)
    assert len(cokes) >= 2
    assert matcher.is_ambiguous(cokes) is True
    rice = matcher.rank("2001", catalog)
    assert rice[0].match_type == "exact_code"


def test_conversation_transitions():
    mgr = ConversationManager(60)
    session = mgr.create("Bearer test")
    session.transition(ConversationState.LISTENING)
    session.transition(ConversationState.TRANSCRIBING)
    with pytest.raises(ValueError):
        session.transition(ConversationState.EXECUTING)


def test_health_and_agents_http():
    client = TestClient(app)
    health = client.get("/health")
    assert health.status_code == 200
    body = health.json()
    assert body["status"] in {"ok", "disabled"}
    assert "PURCHASE_ORDER_AGENT" in body["allowedAgents"]
    agents = client.get("/api/v1/sila-voice/agents")
    assert agents.status_code == 200
    assert any(a["agentId"] == "PURCHASE_ORDER_AGENT" for a in agents.json()["active"])
    assert "INVENTORY_QUERY_AGENT" in agents.json()["reserved"]


def test_session_requires_auth():
    client = TestClient(app)
    denied = client.post("/api/v1/sila-voice/sessions", json={"language": "en"})
    assert denied.status_code == 401
