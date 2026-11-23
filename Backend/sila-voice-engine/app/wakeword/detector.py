"""Optional foreground wake-word. Prefer on-device/browser; server-side is phrase match + optional openWakeWord."""

from __future__ import annotations

from app.interfaces.protocols import AudioFrame
from app.settings import VoiceSettings


class PhraseWakeWord:
    """Lightweight transcript-side wake check for push-to-talk / ASR text (not ambient streaming)."""

    def __init__(self, phrase: str = "hey sila", sensitivity: float = 0.5) -> None:
        self.phrase = (phrase or "hey sila").strip().lower()
        self.sensitivity = sensitivity

    def detect(self, frame: AudioFrame) -> bool:
        # Server does not continuously ingest ambient audio for wake-word.
        # Audio-frame detect is intentionally false; use match_transcript for ASR text.
        return False

    def match_transcript(self, transcript: str) -> bool:
        text = (transcript or "").strip().lower()
        if not text or not self.phrase:
            return False
        if self.phrase in text:
            return True
        # Loose token overlap for mild ASR drift.
        want = set(self.phrase.split())
        have = set(text.replace(",", " ").split())
        if not want:
            return False
        overlap = len(want & have) / len(want)
        return overlap >= max(0.5, self.sensitivity)


class DisabledWakeWord:
    def detect(self, frame: AudioFrame) -> bool:
        return False

    def match_transcript(self, transcript: str) -> bool:
        return False


def build_wake_word_detector(settings: VoiceSettings):
    if not settings.wake_word_enabled:
        return DisabledWakeWord()
    # openWakeWord custom "Hey SILA" model is not assumed present.
    return PhraseWakeWord(settings.wake_word_phrase, settings.wake_word_sensitivity)
