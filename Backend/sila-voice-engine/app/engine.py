"""Voice engine orchestrator — wires providers, conversation, agents, tools."""

from __future__ import annotations

import asyncio
import base64
from typing import Any

from app.agents.purchase_order import register_default_agents
from app.agents.registry import AgentRegistry
from app.asr.providers import build_asr_provider
from app.audio.pipeline import AudioPipeline
from app.audit.adapter import AuditAdapter
from app.conversation.manager import ConversationManager, ConversationState
from app.interfaces.protocols import AudioFrame
from app.llm.providers import build_llm_provider
from app.memory.scopes import MemoryStore
from app.settings import VoiceSettings, get_settings
from app.tools.registry import ToolRegistry
from app.tools.sila_buyer import SilaBuyerTools
from app.tts.providers import build_tts_provider
from app.wakeword.detector import build_wake_word_detector


class VoiceEngine:
    def __init__(self, settings: VoiceSettings | None = None) -> None:
        self.settings = settings or get_settings()
        self.conversations = ConversationManager(self.settings.session_timeout_seconds)
        self.memory = MemoryStore(self.settings.transcript_retention_seconds)
        self.audit = AuditAdapter(store_transcripts=False)
        self.audio = AudioPipeline()
        self.asr = build_asr_provider(self.settings)
        self.tts = build_tts_provider(self.settings)
        self.llm = build_llm_provider(self.settings)
        self.wake = build_wake_word_detector(self.settings)
        self.tools = ToolRegistry()
        self.buyer_tools = SilaBuyerTools(self.settings)
        self.buyer_tools.bind(self.tools)
        self.agents = AgentRegistry()
        register_default_agents(self.agents, self.tools, self.settings.allowed_agents)
        self._sem = asyncio.Semaphore(self.settings.max_concurrency)

    def health(self) -> dict[str, Any]:
        return {
            "status": "ok" if self.settings.enabled else "disabled",
            "version": "1.0.0",
            "asrProvider": self.settings.asr_provider,
            "ttsProvider": self.settings.tts_provider,
            "vadProvider": self.settings.vad_provider,
            "noiseProvider": self.settings.noise_provider,
            "llmProvider": self.settings.llm_provider,
            "wakeWordEnabled": self.settings.wake_word_enabled,
            "allowedAgents": self.settings.allowed_agents,
            "activeAgents": [a.agent_id for a in self.agents.active()],
            "reservedAgents": [a.agent_id for a in self.agents.reserved()],
            "buyerApiUrl": self.settings.buyer_api_url,
        }

    async def start_session(
        self,
        *,
        user_token: str,
        language: str | None = "en",
        context: dict[str, Any] | None = None,
        cookie: str | None = None,
    ) -> dict[str, Any]:
        if not self.settings.enabled:
            return {"ok": False, "error": "VOICE_DISABLED"}
        session = self.conversations.create(user_token, language=language, context=context)
        session.context["cookie"] = cookie
        session.context["user_token"] = user_token
        session.context["language"] = language
        if context:
            session.context.update(context)
        session.transition(ConversationState.LISTENING)
        self.audit.record(
            "SESSION_STARTED",
            voiceSessionId=session.id,
            user="authenticated",
            businessIntent="PURCHASE_ORDER_AGENT",
            status="LISTENING",
        )
        return {
            "ok": True,
            "sessionId": session.id,
            "state": session.state.value,
            "wakeWordEnabled": self.settings.wake_word_enabled,
            "pushToTalk": True,
            "spokenReply": "Listening. Say what you want to order, or press and talk.",
        }

    async def process_turn(
        self,
        *,
        session_id: str,
        transcript: str | None = None,
        audio_base64: str | None = None,
        sample_rate: int = 16000,
        channel: str = "VOICE",
        barge_in: bool = False,
        confirm: bool = False,
        idempotency_key: str | None = None,
    ) -> dict[str, Any]:
        async with self._sem:
            session = self.conversations.get(session_id)
            if session is None:
                return {"ok": False, "error": "SESSION_NOT_FOUND", "message": "Start a new Voice session."}

            with session.lock:
                if barge_in:
                    self.conversations.interrupt(session)
                if session.state in {ConversationState.COMPLETED, ConversationState.CANCELLED}:
                    return {"ok": False, "error": "SESSION_CLOSED", "state": session.state.value}

                started = time_monotonic()
                try:
                    session.transition(ConversationState.TRANSCRIBING)
                except ValueError:
                    session.state = ConversationState.TRANSCRIBING

                quality_notes: list[str] = []
                asr_status = "CLIENT_TEXT"
                text = (transcript or "").strip()

                if audio_base64 and not text:
                    raw = base64.b64decode(audio_base64)
                    frame = AudioFrame(samples=raw, sample_rate=sample_rate)
                    pipe = self.audio.process(frame)
                    quality_notes = list(pipe.quality.reasons)
                    if pipe.quality.ask_to_repeat and not pipe.speech_detected:
                        session.transition(ConversationState.ASKING)
                        reply = "I could not hear you clearly. Please repeat."
                        tts = await self.tts.synthesize(reply, session.language)
                        return {
                            "ok": True,
                            "sessionId": session.id,
                            "state": session.state.value,
                            "askToRepeat": True,
                            "quality": pipe.quality.__dict__,
                            "spokenReply": reply,
                            "ttsProvider": tts.provider,
                            "asrStatus": "QUALITY_REJECT",
                        }
                    asr = await self.asr.transcribe(pipe.frame, session.language)
                    text = asr.normalized_transcript or asr.original_transcript
                    asr_status = asr.provider
                    quality_notes.extend(asr.quality_notes)
                elif text:
                    # Optional transcript-side wake phrase strip / detect (no ambient streaming).
                    if self.settings.wake_word_enabled and self.wake.match_transcript(text):
                        quality_notes.append("Wake phrase detected in transcript.")

                if not text and not confirm:
                    session.transition(ConversationState.ASKING)
                    reply = "Please say the material and quantity, or type your request."
                    return {
                        "ok": True,
                        "sessionId": session.id,
                        "state": session.state.value,
                        "spokenReply": reply,
                        "asrStatus": asr_status,
                        "qualityNotes": quality_notes,
                    }

                session.transition(ConversationState.UNDERSTANDING)
                llm = await self.llm.interpret(
                    transcript="confirm purchase order" if confirm else text,
                    session_state={"draft": session.draft, "context": session.context},
                    allowed_intents=[
                        "ADD_PO_LINE",
                        "GO_WITH_DEFAULT",
                        "CONFIRM_PO",
                        "CANCEL",
                        "PO_UTTERANCE",
                        "ASK_REPEAT",
                        "WAKE_ACK",
                        "GREETING",
                        "AVAILABILITY",
                        "SOCIAL",
                        "GRATITUDE",
                        "FAREWELL",
                        "MENU_TODAY_MEAL",
                        "MENU_CALC_COVERS",
                        "MENU_LOW_MARGIN",
                        "MENU_ALTERNATIVE",
                    ],
                    language=session.language,
                )
                if confirm:
                    llm.intent = "CONFIRM_PO"

                # Conversational intents: speak locally; do not invoke the PO agent.
                if llm.intent in {
                    "WAKE_ACK",
                    "GREETING",
                    "AVAILABILITY",
                    "SOCIAL",
                    "GRATITUDE",
                    "FAREWELL",
                }:
                    spoken = llm.spoken_reply or "Hi! I'm here."
                    session.spoken_reply = spoken
                    session.turns.append(
                        {
                            "role": "user",
                            "transcript": text if self.settings.transcript_retention_seconds else None,
                            "intent": llm.intent,
                        }
                    )
                    if llm.intent == "FAREWELL":
                        try:
                            session.transition(ConversationState.COMPLETED)
                        except ValueError:
                            session.state = ConversationState.COMPLETED
                    else:
                        try:
                            session.transition(ConversationState.SPEAKING)
                            session.transition(ConversationState.LISTENING)
                        except ValueError:
                            session.state = ConversationState.LISTENING
                    tts = await self.tts.synthesize(spoken, session.language)
                    return {
                        "ok": True,
                        "sessionId": session.id,
                        "state": session.state.value,
                        "intent": llm.intent,
                        "spokenReply": spoken,
                        "ttsProvider": tts.provider,
                        "asrStatus": asr_status,
                        "qualityNotes": quality_notes,
                        "localDialogue": True,
                    }

                session.transition(ConversationState.RESOLVING)
                ctx = {
                    **session.context,
                    "user_token": session.user_token,
                    "sila_voice_po_session_id": session.sila_voice_po_session_id,
                    "_buyer_tools": self.buyer_tools,
                }
                agent_result = await self.agents.dispatch(
                    llm.agent_id or "PURCHASE_ORDER_AGENT",
                    llm.intent,
                    {
                        "transcript": text,
                        "spoken_reply": llm.spoken_reply,
                        "channel": channel,
                        "idempotencyKey": idempotency_key,
                    },
                    ctx,
                )

                sila_session = agent_result.get("session") or {}
                sid = sila_session.get("id") or sila_session.get("Id")
                if sid:
                    session.sila_voice_po_session_id = sid
                session.draft = sila_session.get("draft") or sila_session.get("Draft") or session.draft
                spoken = agent_result.get("spoken") or llm.spoken_reply
                session.spoken_reply = spoken
                session.turns.append(
                    {
                        "role": "user",
                        "transcript": text if self.settings.transcript_retention_seconds else None,
                        "intent": llm.intent,
                    }
                )
                self.memory.set_session(
                    session.id,
                    {
                        "silaVoicePoSessionId": session.sila_voice_po_session_id,
                        "lastIntent": llm.intent,
                        "draftLineCount": len((session.draft or {}).get("lines") or []),
                    },
                )

                validation = (sila_session.get("validationStatus") or sila_session.get("ValidationStatus") or "").upper()
                if llm.intent == "CONFIRM_PO" and agent_result.get("ok"):
                    session.transition(ConversationState.EXECUTING)
                    session.transition(ConversationState.COMPLETED)
                elif validation == "OK" or (session.draft and (session.draft.get("lines") or [])):
                    session.transition(ConversationState.AWAITING_CONFIRMATION)
                elif llm.needs_clarification or not agent_result.get("ok"):
                    session.transition(ConversationState.ASKING)
                else:
                    session.transition(ConversationState.SPEAKING)

                tts = await self.tts.synthesize(spoken or "", session.language)
                duration_ms = int((time_monotonic() - started) * 1000)
                self.audit.record(
                    "TURN_PROCESSED",
                    voiceSessionId=session.id,
                    businessIntent=llm.intent,
                    speechRecognitionStatus=asr_status,
                    silaVoicePoSessionId=session.sila_voice_po_session_id,
                    status=session.state.value,
                    processingDurationMs=duration_ms,
                    toolExecutionStatus="OK" if agent_result.get("ok") else "FAILED",
                )
                return {
                    "ok": bool(agent_result.get("ok", True)),
                    "sessionId": session.id,
                    "state": session.state.value,
                    "intent": llm.intent,
                    "agentId": llm.agent_id,
                    "spokenReply": spoken,
                    "ttsProvider": tts.provider,
                    "ttsText": tts.text,
                    "asrStatus": asr_status,
                    "qualityNotes": quality_notes,
                    "silaVoicePoSessionId": session.sila_voice_po_session_id,
                    "draft": session.draft,
                    "silaSession": sila_session,
                    "defaults": agent_result.get("defaults"),
                    "processingDurationMs": duration_ms,
                }


def time_monotonic() -> float:
    import time

    return time.monotonic()


_engine: VoiceEngine | None = None


def get_engine() -> VoiceEngine:
    global _engine
    if _engine is None:
        _engine = VoiceEngine()
    return _engine
