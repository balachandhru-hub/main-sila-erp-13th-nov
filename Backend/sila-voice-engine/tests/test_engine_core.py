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
    assert wake.match_transcript("Hi SILA") is True
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
    hey = await llm.interpret(transcript="Hey SILA", session_state={}, allowed_intents=["WAKE_ACK"])
    assert hey.intent == "WAKE_ACK"
    assert "here" in (hey.spoken_reply or "").lower()
    hi = await llm.interpret(transcript="Hi SILA", session_state={}, allowed_intents=["GREETING"])
    assert hi.intent == "GREETING"
    avail = await llm.interpret(
        transcript="SILA, are you there?",
        session_state={},
        allowed_intents=["AVAILABILITY"],
    )
    assert avail.intent == "AVAILABILITY"
    combined = await llm.interpret(
        transcript="Hey SILA order ten bottles",
        session_state={},
        allowed_intents=["ADD_PO_LINE", "PO_UTTERANCE"],
    )
    assert combined.intent in {"ADD_PO_LINE", "PO_UTTERANCE"}
    assert "ten bottles" in (combined.structured or {}).get("transcript", "").lower() or "order" in (
        combined.structured or {}
    ).get("transcript", "").lower()


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


def test_tts_requires_auth_and_returns_wav_when_espeak_present():
    import shutil

    client = TestClient(app)
    denied = client.post("/api/v1/sila-voice/tts", json={"text": "Hi! I'm here."})
    assert denied.status_code == 401
    if not shutil.which("espeak-ng"):
        pytest.skip("espeak-ng not installed")
    ok = client.post(
        "/api/v1/sila-voice/tts",
        json={"text": "Hi! I'm here. How can I help you today?"},
        headers={"Authorization": "Bearer test"},
    )
    assert ok.status_code == 200
    assert ok.headers.get("content-type", "").startswith("audio/")
    assert len(ok.content) > 1000
    assert ok.content[:4] == b"RIFF"


def test_session_requires_auth():
    client = TestClient(app)
    denied = client.post("/api/v1/sila-voice/sessions", json={"language": "en"})
    assert denied.status_code == 401


@pytest.mark.asyncio
async def test_menu_engineering_voice_intents_and_agent():
    from app.agents.menu_engineering import MenuEngineeringAgent
    from app.tools.registry import ToolRegistry, ToolSpec

    llm = RulesLlm()
    for phrase, expected in [
        ("Show today's breakfast menu.", "MENU_TODAY_MEAL"),
        ("Calculate ingredient requirements for 300 covers.", "MENU_CALC_COVERS"),
        ("Which menu items have low margins?", "MENU_LOW_MARGIN"),
        ("Suggest an alternative to salmon.", "MENU_ALTERNATIVE"),
    ]:
        res = await llm.interpret(transcript=phrase, session_state={}, allowed_intents=[expected])
        assert res.intent == expected
        assert res.agent_id == "MENU_ENGINEERING_AGENT"

    reg = ToolRegistry()
    reg.register(
        ToolSpec("GetMenuMobileSummary", "summary"),
        lambda a, c: _async_val({"ok": True, "data": {"todayMenus": [{"name": "Sunrise Buffet", "mealPeriod": "Breakfast"}]}}),
    )
    reg.register(
        ToolSpec("SimulateMenuCovers", "sim"),
        lambda a, c: _async_val({"ok": True, "data": {"ingredientRequirement": 420.0, "shortage": 12.0}}),
    )
    reg.register(
        ToolSpec("GetMenuCostMargin", "margin"),
        lambda a, c: _async_val({"ok": False, "status": 403}),
    )
    reg.register(
        ToolSpec("GetMenuAiIntelligence", "ai"),
        lambda a, c: _async_val({"ok": True, "data": {"recommendations": [{"proposedChange": "Arctic Char Fillet"}]}}),
    )

    agent = MenuEngineeringAgent(reg)
    r1 = await agent.handle("MENU_TODAY_MEAL", {"transcript": "Show today's breakfast menu."}, {})
    assert r1["ok"] and "Sunrise Buffet" in r1["spoken"]
    r2 = await agent.handle("MENU_CALC_COVERS", {"transcript": "Calculate ingredient requirements for 300 covers."}, {})
    assert r2["ok"] and "300 covers" in r2["spoken"] and "420" in r2["spoken"]
    r3 = await agent.handle("MENU_LOW_MARGIN", {"transcript": "Which menu items have low margins?"}, {})
    assert not r3["ok"] and "denied" in r3["spoken"].lower()
    r4 = await agent.handle("MENU_ALTERNATIVE", {"transcript": "Suggest an alternative to salmon."}, {})
    assert r4["ok"] and "Arctic Char" in r4["spoken"] and "approval" in r4["spoken"].lower()

    for phrase, expected in [
        ("Create a breakfast menu for 300 guests.", "MENU_CREATE_OBJECTIVE"),
        ("Which dishes are consuming more ingredients than expected?", "MENU_CONSUMPTION_ANOMALY"),
        ("Show all ingredients we need to purchase for next week.", "MENU_PURCHASE_NEXT_WEEK"),
        ("Compare this menu with last month's best-performing menu.", "MENU_COMPARE_HISTORY"),
        ("Reduce food cost to 28%.", "MENU_CREATE_OBJECTIVE"),
    ]:
        res = await llm.interpret(transcript=phrase, session_state={}, allowed_intents=[expected])
        assert res.intent == expected
        assert res.agent_id == "MENU_ENGINEERING_AGENT"

    reg.register(
        ToolSpec("GetMenuConsumptionAnomalies", "anomalies"),
        lambda a, c: _async_val({"ok": True, "data": {"anomalies": [{"materialName": "Salmon", "varianceQty": 8.5, "rankedCauses": [{"description": "Over-portioning"}]}]}}),
    )
    reg.register(
        ToolSpec("GetMenuPurchasePlan", "purchase"),
        lambda a, c: _async_val({"ok": True, "data": {"rows": [{"materialName": "Basmati Rice", "shortage": 25.0}]}}),
    )
    reg.register(
        ToolSpec("GetHistoricalMenus", "history"),
        lambda a, c: _async_val({"ok": True, "data": [{"name": "March Gala", "actualSales": 42000, "historicalFoodCostPercent": 27.5}]}),
    )
    reg.register(
        ToolSpec("RunMeStudioObjective", "objective"),
        lambda a, c: _async_val({"ok": True, "data": {"explanation": "Generated 300-guest breakfast at 28.0% food cost."}}),
    )

    r5 = await agent.handle("MENU_CONSUMPTION_ANOMALY", {"transcript": "Which dishes are consuming more ingredients than expected?"}, {})
    assert r5["ok"] and "Salmon" in r5["spoken"]
    r6 = await agent.handle("MENU_PURCHASE_NEXT_WEEK", {"transcript": "Show all ingredients we need to purchase for next week."}, {})
    assert r6["ok"] and "Basmati Rice" in r6["spoken"]
    r7 = await agent.handle("MENU_COMPARE_HISTORY", {"transcript": "Compare this menu with last month's best-performing menu."}, {})
    assert r7["ok"] and "March Gala" in r7["spoken"]
    r8 = await agent.handle("MENU_CREATE_OBJECTIVE", {"transcript": "Create a breakfast menu for 300 guests."}, {})
    assert r8["ok"] and "confirm" in r8["spoken"].lower()

    # Phase ME.3 Section 38 SILA Voice questions
    for phrase, expected in [
        ("How much chicken do we need tomorrow?", "MENU_INGREDIENT_DEMAND_TOMORROW"),
        ("What is the expected breakfast demand?", "MENU_EXPECTED_DEMAND"),
        ("Which ingredients may run out?", "MENU_SHORTAGE_RISK"),
        ("Which dishes should we prepare in smaller batches?", "MENU_SMALLER_BATCH_PREP"),
    ]:
        res = await llm.interpret(transcript=phrase, session_state={}, allowed_intents=[expected])
        assert res.intent == expected
        assert res.agent_id == "MENU_ENGINEERING_AGENT"

    reg.register(
        ToolSpec("GetMenuPurchasePlan", "purchase2"),
        lambda a, c: _async_val({"ok": True, "data": {"rows": [{"materialName": "Chicken Breast", "grossRequirement": 45.0, "uom": "KG", "shortage": 12.0}]}}),
    )
    reg.register(
        ToolSpec("GetTwinDemandForecast", "demand"),
        lambda a, c: _async_val({"ok": True, "data": {"selectedModel": "EXP_SMOOTHING", "nodes": [{"level": "MEAL_PERIOD", "nodeName": "Breakfast", "tomorrowForecast": 310}]}}),
    )
    reg.register(
        ToolSpec("GetTwinWasteRecommendations", "waste"),
        lambda a, c: _async_val({"ok": True, "data": {"recommendations": ["Prepare Creamy Pasta in smaller batches"]}}),
    )

    v1 = await agent.handle("MENU_INGREDIENT_DEMAND_TOMORROW", {"transcript": "How much chicken do we need tomorrow?"}, {})
    assert v1["ok"] and "Chicken Breast" in v1["spoken"] and "45.0" in v1["spoken"]
    v2 = await agent.handle("MENU_EXPECTED_DEMAND", {"transcript": "What is the expected breakfast demand?"}, {})
    assert v2["ok"] and "310" in v2["spoken"]
    v3 = await agent.handle("MENU_SHORTAGE_RISK", {"transcript": "Which ingredients may run out?"}, {})
    assert v3["ok"] and "Chicken Breast" in v3["spoken"]
    v4 = await agent.handle("MENU_SMALLER_BATCH_PREP", {"transcript": "Which dishes should we prepare in smaller batches?"}, {})
    assert v4["ok"] and "smaller batches" in v4["spoken"].lower()


async def _async_val(val):
    return val
