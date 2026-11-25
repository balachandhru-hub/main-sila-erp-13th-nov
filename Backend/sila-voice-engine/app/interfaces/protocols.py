"""Provider protocols — swap models without changing business agents."""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Protocol, runtime_checkable


@dataclass
class AudioFrame:
    samples: bytes
    sample_rate: int = 16000
    channels: int = 1
    encoding: str = "pcm_s16le"


@dataclass
class AsrResult:
    original_transcript: str
    normalized_transcript: str
    language: str | None = None
    confidence: float | None = None
    uncertain_words: list[str] = field(default_factory=list)
    provider: str = "unknown"
    quality_notes: list[str] = field(default_factory=list)


@dataclass
class QualityReport:
    clipping: bool = False
    low_volume: bool = False
    excessive_noise: bool = False
    poor_signal: bool = False
    incomplete_speech: bool = False
    competing_speakers: bool = False
    peak_amplitude: float = 0.0
    rms: float = 0.0
    ask_to_repeat: bool = False
    reasons: list[str] = field(default_factory=list)


@dataclass
class TtsResult:
    audio: bytes
    content_type: str = "audio/wav"
    provider: str = "none"
    text: str = ""


@dataclass
class LlmTurnResult:
    intent: str
    spoken_reply: str
    agent_id: str | None = None
    structured: dict = field(default_factory=dict)
    needs_clarification: bool = False
    clarification: str | None = None
    tool_calls: list[dict] = field(default_factory=list)


@runtime_checkable
class WakeWordDetector(Protocol):
    def detect(self, frame: AudioFrame) -> bool: ...


@runtime_checkable
class VadProvider(Protocol):
    def is_speech(self, frame: AudioFrame) -> bool: ...


@runtime_checkable
class NoiseSuppressor(Protocol):
    def suppress(self, frame: AudioFrame) -> AudioFrame: ...


@runtime_checkable
class SpeechEnhancer(Protocol):
    def enhance(self, frame: AudioFrame) -> AudioFrame: ...


@runtime_checkable
class AudioQualityAssessor(Protocol):
    def assess(self, frame: AudioFrame) -> QualityReport: ...


@runtime_checkable
class AsrProvider(Protocol):
    async def transcribe(self, frame: AudioFrame, language: str | None = None) -> AsrResult: ...


@runtime_checkable
class TtsProvider(Protocol):
    async def synthesize(self, text: str, language: str | None = None, voice: str | None = None) -> TtsResult: ...


@runtime_checkable
class LlmProvider(Protocol):
    async def interpret(
        self,
        *,
        transcript: str,
        session_state: dict,
        allowed_intents: list[str],
        language: str | None = None,
    ) -> LlmTurnResult: ...
