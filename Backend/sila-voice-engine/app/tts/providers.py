"""TTS providers. Default returns text-only for browser speechSynthesis playback."""

from __future__ import annotations

import logging

from app.interfaces.protocols import TtsResult
from app.settings import VoiceSettings

logger = logging.getLogger("sila-voice.tts")


class BrowserTtsStub:
    """Mobile/Cloud play spoken text with Web Speech API — no server audio bytes."""

    async def synthesize(self, text: str, language: str | None = None, voice: str | None = None) -> TtsResult:
        return TtsResult(audio=b"", content_type="text/plain", provider="browser", text=text or "")


class PiperTts:
    async def synthesize(self, text: str, language: str | None = None, voice: str | None = None) -> TtsResult:
        try:
            # Optional: import piper when installed and Voice:TtsProvider=piper.
            raise RuntimeError("Piper not configured in this environment")
        except Exception as exc:  # noqa: BLE001
            logger.warning("Piper TTS unavailable: %s", type(exc).__name__)
            return TtsResult(audio=b"", content_type="text/plain", provider="piper-fallback-browser", text=text or "")


def build_tts_provider(settings: VoiceSettings):
    if settings.tts_provider == "piper":
        return PiperTts()
    if settings.tts_provider == "kokoro":
        # Kokoro reserved — verified language/voice matrix required before enablement.
        return BrowserTtsStub()
    return BrowserTtsStub()
