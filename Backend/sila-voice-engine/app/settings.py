"""Isolated voice-engine settings. Reads appsettings*.json then environment (same pattern as python-backend)."""

from __future__ import annotations

import json
import os
from functools import lru_cache
from pathlib import Path
from typing import Any

from pydantic import BaseModel, Field


def _load_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        return {}
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}


def _merged_appsettings() -> dict[str, Any]:
    root = Path(__file__).resolve().parent.parent
    data: dict[str, Any] = {}
    data.update(_load_json(root / "appsettings.json"))
    env = os.getenv("ASPNETCORE_ENVIRONMENT") or os.getenv("VOICE_ENVIRONMENT") or "Development"
    data.update(_load_json(root / f"appsettings.{env}.json"))
    data.update(_load_json(root / "appsettings.Local.json"))
    return data


def _section(data: dict[str, Any], name: str) -> dict[str, Any]:
    value = data.get(name)
    return value if isinstance(value, dict) else {}


def _env(name: str, default: str = "") -> str:
    return (os.getenv(name) or default).strip()


def _env_bool(name: str, default: bool) -> bool:
    raw = os.getenv(name)
    if raw is None:
        return default
    return raw.strip().lower() in {"1", "true", "yes", "on"}


def _env_float(name: str, default: float) -> float:
    raw = os.getenv(name)
    if raw is None or not raw.strip():
        return default
    try:
        return float(raw)
    except ValueError:
        return default


def _env_int(name: str, default: int) -> int:
    raw = os.getenv(name)
    if raw is None or not raw.strip():
        return default
    try:
        return int(raw)
    except ValueError:
        return default


class VoiceSettings(BaseModel):
    enabled: bool = True
    wake_word_enabled: bool = False
    wake_word_phrase: str = "hey sila"
    wake_word_sensitivity: float = 0.5
    asr_provider: str = "browser"  # browser | faster-whisper | sherpa-onnx
    asr_model: str = "base"
    vad_provider: str = "energy"  # energy | silero
    noise_provider: str = "browser"  # browser | rnnoise | deepfilternet | none
    tts_provider: str = "browser"  # browser | piper | kokoro | none
    tts_voice: str = "default"
    llm_provider: str = "rules"  # rules | ollama | openai-compatible
    llm_endpoint: str = ""
    llm_model: str = ""
    supported_languages: list[str] = Field(default_factory=lambda: ["en", "ar", "hi", "ta", "ms"])
    session_timeout_seconds: int = 900
    audio_retention_seconds: int = 0
    transcript_retention_seconds: int = 86400
    confidence_threshold: float = 0.0
    max_recording_seconds: int = 60
    listening_timeout_seconds: int = 12
    allowed_agents: list[str] = Field(default_factory=lambda: ["PURCHASE_ORDER_AGENT"])
    internal_service_key: str = ""
    buyer_api_url: str = "http://127.0.0.1:8004"
    identity_url: str = "http://127.0.0.1:8001"
    host: str = "127.0.0.1"
    port: int = 8012
    request_timeout_seconds: float = 30.0
    max_concurrency: int = 4

    # Voice 2.0 continuous conversation (client-driven; server-aware flags)
    continuous_conversation: bool = True
    auto_end_of_turn: bool = True
    barge_in_enabled: bool = True
    end_of_speech_silence_ms: int = 700



@lru_cache(maxsize=1)
def get_settings() -> VoiceSettings:
    data = _merged_appsettings()
    voice = _section(data, "Voice")
    inter = _section(data, "InterCallService")

    languages = voice.get("SupportedLanguages") or ["en", "ar", "hi", "ta", "ms"]
    if isinstance(languages, str):
        languages = [part.strip() for part in languages.split(",") if part.strip()]

    agents = voice.get("AllowedAgents") or ["PURCHASE_ORDER_AGENT"]
    if isinstance(agents, str):
        agents = [part.strip() for part in agents.split(",") if part.strip()]

    return VoiceSettings(
        enabled=_env_bool("VOICE_ENABLED", bool(voice.get("Enabled", True))),
        wake_word_enabled=_env_bool("VOICE_WAKE_WORD_ENABLED", bool(voice.get("WakeWordEnabled", False))),
        wake_word_phrase=_env("VOICE_WAKE_WORD_PHRASE", str(voice.get("WakeWordPhrase", "hey sila"))),
        wake_word_sensitivity=_env_float("VOICE_WAKE_WORD_SENSITIVITY", float(voice.get("WakeWordSensitivity", 0.5))),
        asr_provider=_env("VOICE_ASR_PROVIDER", str(voice.get("AsrProvider", "browser"))).lower(),
        asr_model=_env("VOICE_ASR_MODEL", str(voice.get("AsrModel", "base"))),
        vad_provider=_env("VOICE_VAD_PROVIDER", str(voice.get("VadProvider", "energy"))).lower(),
        noise_provider=_env("VOICE_NOISE_PROVIDER", str(voice.get("NoiseProvider", "browser"))).lower(),
        tts_provider=_env("VOICE_TTS_PROVIDER", str(voice.get("TtsProvider", "browser"))).lower(),
        tts_voice=_env("VOICE_TTS_VOICE", str(voice.get("TtsVoice", "default"))),
        llm_provider=_env("VOICE_LLM_PROVIDER", str(voice.get("LlmProvider", "rules"))).lower(),
        llm_endpoint=_env("VOICE_LLM_ENDPOINT", str(voice.get("LlmEndpoint", "") or inter.get("LlmUrl", ""))),
        llm_model=_env("VOICE_LLM_MODEL", str(voice.get("LlmModel", ""))),
        supported_languages=list(languages),
        session_timeout_seconds=_env_int("VOICE_SESSION_TIMEOUT", int(voice.get("SessionTimeoutSeconds", 900))),
        audio_retention_seconds=_env_int("VOICE_AUDIO_RETENTION", int(voice.get("AudioRetentionSeconds", 0))),
        transcript_retention_seconds=_env_int("VOICE_TRANSCRIPT_RETENTION", int(voice.get("TranscriptRetentionSeconds", 86400))),
        confidence_threshold=_env_float("VOICE_CONFIDENCE_THRESHOLD", float(voice.get("ConfidenceThreshold", 0.0))),
        max_recording_seconds=_env_int("VOICE_MAX_RECORDING", int(voice.get("MaxRecordingSeconds", 60))),
        listening_timeout_seconds=_env_int("VOICE_LISTENING_TIMEOUT", int(voice.get("ListeningTimeoutSeconds", 12))),
        allowed_agents=list(agents),
        internal_service_key=_env("VOICE_INTERNAL_SERVICE_KEY", str(voice.get("InternalServiceKey", ""))),
        buyer_api_url=_env("VOICE_BUYER_API_URL", str(inter.get("BuyerUrl", "http://127.0.0.1:8004"))),
        identity_url=_env("VOICE_IDENTITY_URL", str(inter.get("IdentityUrl", "http://127.0.0.1:8001"))),
        host=_env("VOICE_HOST", str(voice.get("Host", "127.0.0.1"))),
        port=_env_int("VOICE_PORT", int(voice.get("Port", 8012))),
        request_timeout_seconds=_env_float("VOICE_REQUEST_TIMEOUT", float(voice.get("RequestTimeoutSeconds", 30))),
        max_concurrency=_env_int("VOICE_MAX_CONCURRENCY", int(voice.get("MaxConcurrency", 4))),
    )
