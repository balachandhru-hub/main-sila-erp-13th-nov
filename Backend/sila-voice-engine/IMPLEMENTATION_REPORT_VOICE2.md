# SILA Voice 2.0 — Continuous Conversation Implementation Report

Date: 2026-11-24 (Asia/Dubai)

## 1. Root cause of repeated Start and Send
The mobile Voice PO panel was a **manual record-and-submit** loop:
- Recognition stopped after each capture (`onend` → idle).
- No end-of-speech / VAD turn finalization.
- Turn submission only via explicit **Send**.
- TTS used default robotic `speechSynthesis` without voice selection or playback-complete → listen resume.
- Overlay showed “Voice Engine Ready” with **no activation action**.

## 2. Existing Voice Agent architecture inspected
- Mobile `VoicePoScreen` + `VoiceAgentOverlay`
- Buyer Voice PO APIs (start/turn/confirm/cancel) — unchanged
- Isolated Python `Backend/sila-voice-engine` (ASR/TTS stubs, energy VAD, PO agent tools)
- Buyer proxy `/api/v1/buyer/sila/voice-engine/*`

## 3. Components reused
- Existing Voice PO session/turn/confirm APIs and business rules
- Browser Web Speech ASR + getUserMedia AEC/NS/AGC
- Existing Voice Engine health check
- Explicit Confirm for draft PO creation

## 4. Python libraries selected
No new heavy ML packages installed. Retained existing FastAPI stack. Optional Silero/faster-whisper/Kokoro remain optional extras (not blindly installed).

## 5. Pipecat integration status
**Not integrated.** Justification: existing Voice Engine + browser transport already cover the required loop; Pipecat would add WebRTC/pipeline deps without improving the primary failure mode (frontend conversation lifecycle). Compatible later if streaming PCM transport is required.

## 6. Audio transport
Authenticated continuous microphone stream for the active session (not open/close per turn). Turns still use existing text Voice PO turn API after ASR (same business path).

## 7. VAD
Client energy VAD from AnalyserNode + SpeechRecognition final/interim results. Server EnergyVad retained. Silero remains optional.

## 8. Automatic end-of-turn
Silence timer (`700ms`, `1400ms` if incomplete tail words). Incomplete-utterance heuristic avoids cutting “Order ten bottles of…”.

## 9. Noise suppression
Browser `echoCancellation`, `noiseSuppression`, `autoGainControl` (no stacked RNNoise/DeepFilterNet).

## 10. Speech recognition
Browser continuous Web Speech API; recognition auto-restarts while session active.

## 11. Neural TTS / selected voice
Primary: browser neural/natural voices via preference list (Google/Microsoft Natural/Aria/Jenny, Samantha, etc.) with warmer rate/pitch. Kokoro/Piper remain optional server providers (not enabled without verified matrix). Substantially less robotic than default system voice when a neural browser voice is present.

## 12. Automatic playback
TTS starts automatically after agent reply; unlocked on initial user gesture.

## 13. Automatic listening resume
On TTS `onend` → `LISTENING` (clears turn transcript for next utterance).

## 14. Barge-in
During `SPEAKING`, elevated mic energy or recognition results cancel TTS and capture the new utterance.

## 15. Conversation state-machine corrections
UI states: idle → activating → listening → userSpeaking → transcribing → processing → speaking → listening (loop). Awaiting confirmation / error / End Session supported. Does **not** return to idle after every reply.

## 16. Files added
- `voiceAgent/naturalTts.ts`
- `voiceAgent/conversationConfig.ts`
- `Backend/sila-voice-engine/IMPLEMENTATION_REPORT_VOICE2.md`

## 17. Files modified
- `VoicePoScreen.tsx` / `.css` — continuous conversation lifecycle; remove mandatory Start/Send recording controls
- `VoiceAgentOverlay.tsx` / `.css` — SILA Voice 2.0 frozen orb identity
- `silaVoicePoFrontend.test.ts` — contract assertions
- `app/settings.py` — additive continuous conversation flags

## 18. Measured conversational latency
Not claimed as production SLA. Turn latency = end-of-speech wait (~0.7–1.4s) + Voice PO turn API + TTS start. No fabricated numbers.

## 19. CPU/GPU/memory
No new server models loaded. Client Web Speech + speechSynthesis only.

## 20. Continuous conversation tests
Automated: frontend contract + voice-engine pytest (8 passed). Manual GUI activation path verified via Voice 2.0 orb controls presence. Full 10-turn live speech requires device mic (environment-dependent).

## 21. Mobile browser compatibility
Requires foreground mic permission; iOS Safari Web Speech constraints apply; no locked-screen wake word.

## 22. Remaining limitations
- Server Kokoro/Piper neural audio bytes not enabled in this environment
- Pipecat/WebRTC bidirectional audio not adopted
- Semantic Smart Turn model not installed — heuristic incomplete-utterance fallback used

## 23. PO API configuration
**Unchanged.** Confirm still uses existing Voice PO confirm → CreatePurchaseOrder.

## 24. Live SAP postings
**None.** Schedulers remain PARKED.

## 25. Unrelated changes
None intended beyond the existing Voice Agent panel / voice-engine settings flags.
