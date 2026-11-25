"""Microphone audio → normalize → VAD → optional denoise → quality assess."""

from __future__ import annotations

import array
import audioop
import math
from dataclasses import dataclass

from app.interfaces.protocols import (
    AudioFrame,
    AudioQualityAssessor,
    NoiseSuppressor,
    QualityReport,
    SpeechEnhancer,
    VadProvider,
)


def normalize_pcm16(raw: bytes, sample_rate: int = 16000, channels: int = 1) -> AudioFrame:
    """Normalize uploaded audio to mono PCM s16le @ sample_rate when possible."""
    data = raw
    # If client sent float32 WAV-less PCM we cannot detect; assume s16le.
    if channels > 1 and len(data) >= 4:
        try:
            data = audioop.tomono(data, 2, 0.5, 0.5)
            channels = 1
        except audioop.error:
            pass
    return AudioFrame(samples=data, sample_rate=sample_rate, channels=channels, encoding="pcm_s16le")


class EnergyVad:
    def __init__(self, threshold: float = 500.0) -> None:
        self.threshold = threshold

    def is_speech(self, frame: AudioFrame) -> bool:
        if not frame.samples:
            return False
        try:
            rms = audioop.rms(frame.samples, 2)
        except audioop.error:
            return False
        return rms >= self.threshold


class PassthroughNoise:
    def suppress(self, frame: AudioFrame) -> AudioFrame:
        return frame


class PassthroughEnhance:
    def enhance(self, frame: AudioFrame) -> AudioFrame:
        return frame


class BasicQualityAssessor:
    def assess(self, frame: AudioFrame) -> QualityReport:
        report = QualityReport()
        if not frame.samples or len(frame.samples) < 4:
            report.incomplete_speech = True
            report.ask_to_repeat = True
            report.reasons.append("Audio too short or empty.")
            return report
        try:
            samples = array.array("h")
            samples.frombytes(frame.samples[: len(frame.samples) - (len(frame.samples) % 2)])
        except (ValueError, OverflowError):
            report.poor_signal = True
            report.ask_to_repeat = True
            report.reasons.append("Unable to decode PCM samples.")
            return report
        if not samples:
            report.incomplete_speech = True
            report.ask_to_repeat = True
            report.reasons.append("No PCM samples.")
            return report
        peak = max(abs(s) for s in samples)
        rms = math.sqrt(sum(s * s for s in samples) / len(samples))
        report.peak_amplitude = peak / 32768.0
        report.rms = rms / 32768.0
        if peak >= 32000:
            report.clipping = True
            report.reasons.append("Clipping detected.")
        if rms < 200:
            report.low_volume = True
            report.reasons.append("Low volume.")
        if rms > 9000 and peak < 20000:
            report.excessive_noise = True
            report.reasons.append("High sustained energy — possible noise.")
        duration = len(samples) / max(frame.sample_rate, 1)
        if duration < 0.25:
            report.incomplete_speech = True
            report.reasons.append("Utterance too short.")
        report.ask_to_repeat = report.clipping or report.low_volume or report.incomplete_speech or report.poor_signal
        return report


@dataclass
class PipelineResult:
    frame: AudioFrame
    speech_detected: bool
    quality: QualityReport


class AudioPipeline:
    def __init__(
        self,
        vad: VadProvider | None = None,
        noise: NoiseSuppressor | None = None,
        enhancer: SpeechEnhancer | None = None,
        quality: AudioQualityAssessor | None = None,
    ) -> None:
        self.vad = vad or EnergyVad()
        self.noise = noise or PassthroughNoise()
        self.enhancer = enhancer or PassthroughEnhance()
        self.quality = quality or BasicQualityAssessor()

    def process(self, frame: AudioFrame) -> PipelineResult:
        normalized = normalize_pcm16(frame.samples, frame.sample_rate, frame.channels)
        # Browser already applies AEC/NS when available; avoid stacking aggressive denoise by default.
        cleaned = self.noise.suppress(normalized)
        enhanced = self.enhancer.enhance(cleaned)
        speech = self.vad.is_speech(enhanced)
        quality = self.quality.assess(enhanced)
        if not speech and not quality.ask_to_repeat:
            quality.incomplete_speech = True
            quality.ask_to_repeat = True
            quality.reasons.append("No voice activity detected.")
        return PipelineResult(frame=enhanced, speech_detected=speech, quality=quality)
