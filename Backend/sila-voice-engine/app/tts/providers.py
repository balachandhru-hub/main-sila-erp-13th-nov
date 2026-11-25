"""TTS providers. Prefer audible WAV (espeak-ng) when browser speechSynthesis has no voices."""

from __future__ import annotations

import logging
import os
import shutil
import subprocess
import tempfile

from app.interfaces.protocols import TtsResult
from app.settings import VoiceSettings

logger = logging.getLogger("sila-voice.tts")

_MAX_CHARS = 500


class BrowserTtsStub:
    """Returns text only — client plays with Web Speech API when voices exist."""

    async def synthesize(self, text: str, language: str | None = None, voice: str | None = None) -> TtsResult:
        return TtsResult(audio=b"", content_type="text/plain", provider="browser", text=text or "")


class EspeakNgTts:
    """Local audible WAV via espeak-ng (works when Chromium has zero speechSynthesis voices)."""

    def __init__(self, voice: str = "en-us+f3", speed: int = 145) -> None:
        self.voice = voice or "en-us+f3"
        self.speed = speed

    async def synthesize(self, text: str, language: str | None = None, voice: str | None = None) -> TtsResult:
        spoken = (text or "").strip()
        if not spoken:
            return TtsResult(audio=b"", content_type="audio/wav", provider="espeak-ng", text="")
        if len(spoken) > _MAX_CHARS:
            spoken = spoken[:_MAX_CHARS].rstrip() + "."
        if not shutil.which("espeak-ng"):
            logger.warning("espeak-ng not found on PATH")
            return TtsResult(audio=b"", content_type="text/plain", provider="espeak-missing", text=spoken)

        voice_id = voice or self.voice
        fd, path = tempfile.mkstemp(suffix=".wav")
        os.close(fd)
        try:
            completed = subprocess.run(
                [
                    "espeak-ng",
                    "-v",
                    voice_id,
                    "-s",
                    str(self.speed),
                    "-w",
                    path,
                    spoken,
                ],
                check=False,
                capture_output=True,
                timeout=20,
            )
            if completed.returncode != 0 or not os.path.exists(path) or os.path.getsize(path) < 44:
                logger.warning(
                    "espeak-ng failed code=%s stderr=%s",
                    completed.returncode,
                    (completed.stderr or b"")[:200],
                )
                return TtsResult(audio=b"", content_type="text/plain", provider="espeak-failed", text=spoken)
            with open(path, "rb") as handle:
                audio = handle.read()
            return TtsResult(audio=audio, content_type="audio/wav", provider="espeak-ng", text=spoken)
        except Exception as exc:  # noqa: BLE001
            logger.warning("espeak-ng TTS unavailable: %s", type(exc).__name__)
            return TtsResult(audio=b"", content_type="text/plain", provider="espeak-error", text=spoken)
        finally:
            try:
                os.remove(path)
            except OSError:
                pass


class PiperTts:
    async def synthesize(self, text: str, language: str | None = None, voice: str | None = None) -> TtsResult:
        try:
            raise RuntimeError("Piper not configured in this environment")
        except Exception as exc:  # noqa: BLE001
            logger.warning("Piper TTS unavailable: %s", type(exc).__name__)
            # Fall back to espeak when present.
            if shutil.which("espeak-ng"):
                return await EspeakNgTts().synthesize(text, language, voice)
            return TtsResult(audio=b"", content_type="text/plain", provider="piper-fallback-browser", text=text or "")


def build_tts_provider(settings: VoiceSettings):
    provider = (settings.tts_provider or "auto").lower()
    if provider == "piper":
        return PiperTts()
    if provider == "kokoro":
        # Kokoro reserved — verified language/voice matrix required before enablement.
        if shutil.which("espeak-ng"):
            return EspeakNgTts(settings.tts_voice if settings.tts_voice != "default" else "en-us+f3")
        return BrowserTtsStub()
    if provider == "espeak" or provider == "espeak-ng":
        return EspeakNgTts(settings.tts_voice if settings.tts_voice != "default" else "en-us+f3")
    if provider == "browser":
        # Prefer audible server WAV when espeak is available — browser voices are often missing.
        if shutil.which("espeak-ng"):
            return EspeakNgTts(settings.tts_voice if settings.tts_voice != "default" else "en-us+f3")
        return BrowserTtsStub()
    # auto
    if shutil.which("espeak-ng"):
        return EspeakNgTts(settings.tts_voice if settings.tts_voice != "default" else "en-us+f3")
    return BrowserTtsStub()
