"""Stateful multi-turn conversation manager with explicit transitions."""

from __future__ import annotations

import threading
import time
import uuid
from dataclasses import dataclass, field
from enum import Enum
from typing import Any


class ConversationState(str, Enum):
    IDLE = "IDLE"
    LISTENING = "LISTENING"
    TRANSCRIBING = "TRANSCRIBING"
    UNDERSTANDING = "UNDERSTANDING"
    RESOLVING = "RESOLVING"
    ASKING = "ASKING"
    SPEAKING = "SPEAKING"
    AWAITING_CONFIRMATION = "AWAITING_CONFIRMATION"
    EXECUTING = "EXECUTING"
    COMPLETED = "COMPLETED"
    CANCELLED = "CANCELLED"
    FAILED = "FAILED"


_ALLOWED: dict[ConversationState, set[ConversationState]] = {
    ConversationState.IDLE: {ConversationState.LISTENING, ConversationState.TRANSCRIBING, ConversationState.CANCELLED},
    ConversationState.LISTENING: {
        ConversationState.TRANSCRIBING,
        ConversationState.IDLE,
        ConversationState.CANCELLED,
        ConversationState.FAILED,
    },
    ConversationState.TRANSCRIBING: {
        ConversationState.UNDERSTANDING,
        ConversationState.ASKING,
        ConversationState.FAILED,
        ConversationState.CANCELLED,
    },
    ConversationState.UNDERSTANDING: {
        ConversationState.RESOLVING,
        ConversationState.ASKING,
        ConversationState.SPEAKING,
        ConversationState.CANCELLED,
        ConversationState.FAILED,
    },
    ConversationState.RESOLVING: {
        ConversationState.ASKING,
        ConversationState.AWAITING_CONFIRMATION,
        ConversationState.SPEAKING,
        ConversationState.FAILED,
        ConversationState.CANCELLED,
    },
    ConversationState.ASKING: {
        ConversationState.LISTENING,
        ConversationState.TRANSCRIBING,
        ConversationState.SPEAKING,
        ConversationState.CANCELLED,
    },
    ConversationState.SPEAKING: {
        ConversationState.LISTENING,
        ConversationState.AWAITING_CONFIRMATION,
        ConversationState.IDLE,
        ConversationState.CANCELLED,
        ConversationState.TRANSCRIBING,  # barge-in
    },
    ConversationState.AWAITING_CONFIRMATION: {
        ConversationState.EXECUTING,
        ConversationState.TRANSCRIBING,
        ConversationState.LISTENING,
        ConversationState.CANCELLED,
        ConversationState.FAILED,
    },
    ConversationState.EXECUTING: {
        ConversationState.COMPLETED,
        ConversationState.FAILED,
        ConversationState.SPEAKING,
    },
    ConversationState.COMPLETED: {ConversationState.IDLE},
    ConversationState.CANCELLED: {ConversationState.IDLE},
    ConversationState.FAILED: {ConversationState.IDLE, ConversationState.LISTENING},
}


@dataclass
class VoiceSession:
    id: str
    user_token: str
    tenant_hint: str | None = None
    state: ConversationState = ConversationState.IDLE
    language: str | None = "en"
    agent_id: str | None = "PURCHASE_ORDER_AGENT"
    sila_voice_po_session_id: str | None = None
    draft: dict[str, Any] = field(default_factory=dict)
    turns: list[dict[str, Any]] = field(default_factory=list)
    context: dict[str, Any] = field(default_factory=dict)
    spoken_reply: str | None = None
    created_at: float = field(default_factory=time.time)
    updated_at: float = field(default_factory=time.time)
    interrupted: bool = False
    lock: threading.RLock = field(default_factory=threading.RLock)

    def transition(self, new_state: ConversationState) -> None:
        allowed = _ALLOWED.get(self.state, set())
        if new_state not in allowed and new_state != self.state:
            raise ValueError(f"Illegal transition {self.state} → {new_state}")
        self.state = new_state
        self.updated_at = time.time()


class ConversationManager:
    def __init__(self, session_timeout_seconds: int = 900) -> None:
        self.session_timeout_seconds = session_timeout_seconds
        self._sessions: dict[str, VoiceSession] = {}
        self._guard = threading.RLock()

    def create(self, user_token: str, *, language: str | None = "en", context: dict | None = None) -> VoiceSession:
        self.purge_expired()
        session = VoiceSession(
            id=str(uuid.uuid4()),
            user_token=user_token,
            language=language,
            context=dict(context or {}),
        )
        with self._guard:
            self._sessions[session.id] = session
        return session

    def get(self, session_id: str) -> VoiceSession | None:
        self.purge_expired()
        with self._guard:
            return self._sessions.get(session_id)

    def cancel(self, session_id: str) -> VoiceSession | None:
        session = self.get(session_id)
        if not session:
            return None
        with session.lock:
            try:
                session.transition(ConversationState.CANCELLED)
            except ValueError:
                session.state = ConversationState.CANCELLED
                session.updated_at = time.time()
            session.spoken_reply = "Conversation cancelled."
        return session

    def interrupt(self, session: VoiceSession) -> None:
        with session.lock:
            session.interrupted = True
            if session.state == ConversationState.SPEAKING:
                session.transition(ConversationState.TRANSCRIBING)

    def purge_expired(self) -> None:
        now = time.time()
        with self._guard:
            dead = [
                sid
                for sid, session in self._sessions.items()
                if now - session.updated_at > self.session_timeout_seconds
            ]
            for sid in dead:
                self._sessions.pop(sid, None)
