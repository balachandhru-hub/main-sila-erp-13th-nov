"""HTTP + WebSocket surface for the isolated voice engine."""

from __future__ import annotations

from typing import Any

from fastapi import APIRouter, Header, HTTPException, WebSocket, WebSocketDisconnect
from fastapi.responses import Response
from pydantic import BaseModel, Field

from app.engine import get_engine
from app.settings import get_settings

router = APIRouter(prefix="/api/v1/sila-voice", tags=["sila-voice"])


class StartRequest(BaseModel):
    language: str | None = "en"
    locationId: str | None = None
    propertyId: str | None = None
    outletId: str | None = None
    purchasingProfileId: str | None = None


class TurnRequest(BaseModel):
    transcript: str | None = None
    audioBase64: str | None = None
    sampleRate: int = 16000
    channel: str = "VOICE"
    bargeIn: bool = False
    confirm: bool = False
    idempotencyKey: str | None = None


class TtsRequest(BaseModel):
    text: str = Field(..., min_length=1, max_length=500)
    language: str | None = "en"
    voice: str | None = None


def _require_auth(authorization: str | None, cookie: str | None, x_voice_key: str | None) -> str:
    settings = get_settings()
    if settings.internal_service_key and x_voice_key == settings.internal_service_key:
        return authorization or "service"
    if authorization or cookie:
        return authorization or "cookie"
    raise HTTPException(status_code=401, detail="Authenticated SILA session required. Voice is not an authenticator.")


@router.get("/health")
def health() -> dict[str, Any]:
    return get_engine().health()


@router.get("/ready")
def ready() -> dict[str, Any]:
    engine = get_engine()
    if not engine.settings.enabled:
        raise HTTPException(status_code=503, detail="Voice engine disabled")
    return {"ready": True, "buyerApiUrl": engine.settings.buyer_api_url}


@router.get("/agents")
def agents() -> dict[str, Any]:
    engine = get_engine()
    return {
        "active": [
            {
                "agentId": a.agent_id,
                "intents": a.supported_intents,
                "confirmationRequired": a.confirmation_required,
            }
            for a in engine.agents.active()
        ],
        "reserved": [a.agent_id for a in engine.agents.reserved()],
    }


@router.post("/sessions")
async def start_session(
    body: StartRequest,
    authorization: str | None = Header(default=None),
    cookie: str | None = Header(default=None),
    x_voice_internal_key: str | None = Header(default=None, alias="X-Voice-Internal-Key"),
) -> dict[str, Any]:
    token = _require_auth(authorization, cookie, x_voice_internal_key)
    engine = get_engine()
    return await engine.start_session(
        user_token=authorization or token,
        language=body.language,
        cookie=cookie,
        context={
            "locationId": body.locationId,
            "propertyId": body.propertyId,
            "outletId": body.outletId,
            "purchasingProfileId": body.purchasingProfileId,
            "language": body.language,
        },
    )


@router.post("/sessions/{session_id}/turns")
async def process_turn(
    session_id: str,
    body: TurnRequest,
    authorization: str | None = Header(default=None),
    cookie: str | None = Header(default=None),
    x_voice_internal_key: str | None = Header(default=None, alias="X-Voice-Internal-Key"),
) -> dict[str, Any]:
    _require_auth(authorization, cookie, x_voice_internal_key)
    engine = get_engine()
    session = engine.conversations.get(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Voice session not found")
    # Refresh auth material for tool calls.
    if authorization:
        session.user_token = authorization
        session.context["user_token"] = authorization
    if cookie:
        session.context["cookie"] = cookie
    return await engine.process_turn(
        session_id=session_id,
        transcript=body.transcript,
        audio_base64=body.audioBase64,
        sample_rate=body.sampleRate,
        channel=body.channel,
        barge_in=body.bargeIn,
        confirm=body.confirm,
        idempotency_key=body.idempotencyKey,
    )


@router.post("/tts")
async def synthesize_speech(
    body: TtsRequest,
    authorization: str | None = Header(default=None),
    cookie: str | None = Header(default=None),
    x_voice_internal_key: str | None = Header(default=None, alias="X-Voice-Internal-Key"),
) -> Response:
    """Return audible WAV for SILA replies when browser speechSynthesis has no voices."""
    _require_auth(authorization, cookie, x_voice_internal_key)
    engine = get_engine()
    result = await engine.tts.synthesize(body.text, body.language, body.voice)
    if result.audio:
        return Response(
            content=result.audio,
            media_type=result.content_type or "audio/wav",
            headers={
                "X-Sila-Tts-Provider": result.provider,
                "Cache-Control": "no-store",
            },
        )
    raise HTTPException(
        status_code=503,
        detail=f"Audible TTS unavailable (provider={result.provider}). Install espeak-ng or enable browser voices.",
    )


@router.post("/sessions/{session_id}/cancel")
async def cancel_session(
    session_id: str,
    authorization: str | None = Header(default=None),
    cookie: str | None = Header(default=None),
    x_voice_internal_key: str | None = Header(default=None, alias="X-Voice-Internal-Key"),
) -> dict[str, Any]:
    _require_auth(authorization, cookie, x_voice_internal_key)
    engine = get_engine()
    session = engine.conversations.cancel(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Voice session not found")
    # Also cancel SILA Voice PO if linked.
    if session.sila_voice_po_session_id:
        await engine.buyer_tools._request(
            "POST",
            f"/api/v1/buyer/sila/voice-po/sessions/{session.sila_voice_po_session_id}/cancel",
            {"user_token": authorization or session.user_token, "cookie": cookie or session.context.get("cookie")},
            {},
        )
    engine.audit.record("SESSION_CANCELLED", voiceSessionId=session.id, status="CANCELLED")
    return {"ok": True, "sessionId": session.id, "state": session.state.value, "spokenReply": session.spoken_reply}


@router.websocket("/ws/{session_id}")
async def voice_ws(websocket: WebSocket, session_id: str) -> None:
    """Streaming control channel — barge-in signals + turn JSON. No ambient audio continuous upload by default."""
    await websocket.accept()
    engine = get_engine()
    try:
        while True:
            message = await websocket.receive_json()
            kind = (message.get("type") or "turn").lower()
            if kind == "ping":
                await websocket.send_json({"type": "pong"})
                continue
            if kind == "barge_in":
                session = engine.conversations.get(session_id)
                if session:
                    engine.conversations.interrupt(session)
                await websocket.send_json({"type": "interrupted", "sessionId": session_id})
                continue
            result = await engine.process_turn(
                session_id=session_id,
                transcript=message.get("transcript"),
                audio_base64=message.get("audioBase64"),
                sample_rate=int(message.get("sampleRate") or 16000),
                channel=message.get("channel") or "VOICE",
                barge_in=bool(message.get("bargeIn")),
                confirm=bool(message.get("confirm")),
                idempotency_key=message.get("idempotencyKey"),
            )
            await websocket.send_json({"type": "turn_result", **result})
    except WebSocketDisconnect:
        return
