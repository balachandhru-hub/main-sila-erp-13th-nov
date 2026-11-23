"""Audit adapter — structured events for Transaction Tracker integration (no raw audio by default)."""

from __future__ import annotations

import logging
import time
from typing import Any

logger = logging.getLogger("sila-voice.audit")


class AuditAdapter:
    def __init__(self, store_transcripts: bool = False) -> None:
        self.store_transcripts = store_transcripts
        self.events: list[dict[str, Any]] = []

    def record(self, event_type: str, **fields: Any) -> dict[str, Any]:
        payload = {
            "eventType": event_type,
            "at": time.time(),
            **fields,
        }
        if not self.store_transcripts:
            payload.pop("transcript", None)
            payload.pop("rawAudioRef", None)
        self.events.append(payload)
        logger.info(
            "voice_audit type=%s session=%s intent=%s status=%s",
            event_type,
            payload.get("voiceSessionId"),
            payload.get("businessIntent"),
            payload.get("status"),
        )
        return payload
