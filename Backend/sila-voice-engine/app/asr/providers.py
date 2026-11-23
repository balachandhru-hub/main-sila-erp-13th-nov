"""Speech recognition providers. Local-first faster-whisper when installed; otherwise browser transcript passthrough."""

from __future__ import annotations

import logging
import re

from app.interfaces.protocols import AsrResult, AudioFrame
from app.settings import VoiceSettings

logger = logging.getLogger("sila-voice.asr")


def _normalize_transcript(text: str) -> str:
    cleaned = re.sub(r"\s+", " ", (text or "").strip())
    return cleaned


class BrowserPassthroughAsr:
    """Accepts client-side Web Speech / MediaRecorder transcript; does not invent ASR confidence."""

    provider_name = "browser"

    async def transcribe(self, frame: AudioFrame, language: str | None = None) -> AsrResult:
        # Frame may carry UTF-8 transcript in samples when encoding=transcript.
        if frame.encoding == "transcript":
            original = frame.samples.decode("utf-8", errors="ignore")
        else:
            original = ""
        return AsrResult(
            original_transcript=original,
            normalized_transcript=_normalize_transcript(original),
            language=language,
            confidence=None,
            provider=self.provider_name,
            quality_notes=["Browser / client ASR passthrough — no server model score."],
        )


class FasterWhisperAsr:
    def __init__(self, model_name: str = "base") -> None:
        self.model_name = model_name
        self._model = None

    def _ensure(self):
        if self._model is not None:
            return
        from faster_whisper import WhisperModel  # type: ignore

        self._model = WhisperModel(self.model_name, device="cpu", compute_type="int8")

    async def transcribe(self, frame: AudioFrame, language: str | None = None) -> AsrResult:
        if frame.encoding == "transcript" and frame.samples:
            # Prefer explicit client correction / browser transcript when provided with audio.
            text = frame.samples.decode("utf-8", errors="ignore")
            if text.strip():
                return AsrResult(
                    original_transcript=text,
                    normalized_transcript=_normalize_transcript(text),
                    language=language,
                    confidence=None,
                    provider="faster-whisper+client",
                )
        try:
            self._ensure()
        except Exception as exc:  # noqa: BLE001
            logger.warning("faster-whisper unavailable: %s", type(exc).__name__)
            return AsrResult(
                original_transcript="",
                normalized_transcript="",
                language=language,
                confidence=None,
                provider="faster-whisper",
                quality_notes=[f"Model unavailable: {type(exc).__name__}"],
            )
        import io
        import wave

        buf = io.BytesIO()
        with wave.open(buf, "wb") as wav:
            wav.setnchannels(1)
            wav.setsampwidth(2)
            wav.setframerate(frame.sample_rate or 16000)
            wav.writeframes(frame.samples)
        buf.seek(0)
        segments, info = self._model.transcribe(buf, language=language, vad_filter=True)
        parts: list[str] = []
        for segment in segments:
            parts.append(segment.text)
        original = " ".join(parts).strip()
        # faster-whisper may expose avg_log_prob; do not invent calibrated confidence.
        return AsrResult(
            original_transcript=original,
            normalized_transcript=_normalize_transcript(original),
            language=getattr(info, "language", language),
            confidence=None,
            provider="faster-whisper",
            quality_notes=[f"model={self.model_name}"],
        )


def build_asr_provider(settings: VoiceSettings):
    if settings.asr_provider in {"faster-whisper", "whisper"}:
        return FasterWhisperAsr(settings.asr_model)
    # sherpa-onnx reserved — fall back to browser until package/model configured
    return BrowserPassthroughAsr()
