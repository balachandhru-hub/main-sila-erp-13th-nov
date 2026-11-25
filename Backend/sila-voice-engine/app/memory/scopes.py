"""Three memory scopes — session / user preference / business context. Not authoritative ERP data."""

from __future__ import annotations

import threading
import time
from typing import Any


class MemoryStore:
    def __init__(self, transcript_retention_seconds: int = 86400) -> None:
        self.transcript_retention_seconds = transcript_retention_seconds
        self._session: dict[str, dict[str, Any]] = {}
        self._user_prefs: dict[str, dict[str, Any]] = {}
        self._lock = threading.RLock()

    def set_session(self, session_id: str, data: dict[str, Any]) -> None:
        with self._lock:
            self._session[session_id] = {**data, "_ts": time.time()}

    def get_session(self, session_id: str) -> dict[str, Any]:
        with self._lock:
            return dict(self._session.get(session_id) or {})

    def set_user_prefs(self, user_key: str, prefs: dict[str, Any]) -> None:
        # Only explicitly saved preferences — no automatic sensitive transcript persistence.
        allowed = {k: prefs[k] for k in ("language", "ttsMuted", "voiceSpeed", "wakeWordEnabled") if k in prefs}
        with self._lock:
            self._user_prefs[user_key] = allowed

    def get_user_prefs(self, user_key: str) -> dict[str, Any]:
        with self._lock:
            return dict(self._user_prefs.get(user_key) or {})

    def business_context_note(self) -> str:
        return "Business context is retrieved live from SILA Material Master / Supplier / PO history — not conversational memory."

    def purge(self) -> None:
        now = time.time()
        with self._lock:
            dead = [k for k, v in self._session.items() if now - float(v.get("_ts", 0)) > self.transcript_retention_seconds]
            for k in dead:
                self._session.pop(k, None)
