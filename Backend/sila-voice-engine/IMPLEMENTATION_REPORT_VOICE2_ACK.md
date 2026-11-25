# SILA Voice 2.0 — Wake-Word Acknowledgement & Continuous Conversation Correction

Date: 2026-11-24 (Asia/Dubai)

## 1. Root cause of missing voice acknowledgement
The live mobile path was browser STT → Voice PO turn API → browser TTS. There was **no local wake/greeting intent handler**. Short phrases such as “Hey SILA” / “Hi SILA” were either:
- dropped by `minUtteranceMs` / incomplete-utterance gates (transcript shown, no TTS), or
- submitted to the PO agent as `Unknown` clarification text (not an audible wake ack).

## 2. Root cause of transcript-only behavior
Transcript finalization did not always invoke a spoken response path. When recognition ended during `speaking`/`processing`, `onend` returned without restart and `resumeListening` did not re-arm SpeechRecognition — so the loop stalled after the first reply.

## 3. Existing components reused
- Mobile `VoicePoScreen` + `VoiceAgentOverlay` (existing Voice Agent)
- Browser Web Speech ASR + `speechSynthesis` neural voice preference (`naturalTts.ts`)
- Existing Voice PO session/turn/confirm APIs (unchanged business path)
- Isolated Python `Backend/sila-voice-engine` (RulesLlm, PhraseWakeWord, ConversationManager)
- Buyer voice-engine health proxy (overlay only)

## 4. Python frameworks / libraries selected
No new heavy ML packages installed. Retained FastAPI + existing optional extras matrix. Pipecat / LiveKit Agents **not** adopted for this correction — the primary defect was frontend dialogue lifecycle + missing greeting intents, not transport.

## 5. Pipecat / LiveKit Agents integration status
**Not integrated.** Compatible later if bidirectional PCM/WebRTC streaming is required. Existing browser continuous recognition + Voice PO APIs remain the production turn path.

## 6. Wake-word implementation
- Client transcript matching via `dialogueIntents.ts` (Hey/Hi/Hello/Okay/Good morning SILA, availability checks).
- Server `PhraseWakeWord` expanded for the same phrases (transcript-side; ambient acoustic model not assumed present).
- openWakeWord custom “Hey SILA” model is **not** assumed shipped.

## 7. Greeting recognition
Deterministic local dialogue intents: `WAKE_ACK`, `GREETING`, `AVAILABILITY`, `SOCIAL`, `GRATITUDE`, `FAREWELL`. Combined wake + business command strips the wake prefix and preserves the purchasing remainder.

## 8. Dialogue manager
Frontend `resolveDialogueIntent` runs **before** `sendVoicePoTurn`. Local-only intents speak immediately and resume listening. Business intents keep the existing PO agent. Python `RulesLlm` + `engine.process_turn` short-circuit the same conversational intents when the voice-engine turn API is used.

## 9. Neural TTS model / voice
Primary: browser neural/natural voices (`pickNaturalVoice` preference list). Unlock hardened (gesture-tied utterance is no longer cancelled immediately). Server Kokoro/Piper remain optional and were not enabled in this environment.

## 10. Automatic playback correction
Every acknowledgement and agent reply uses `speakNaturally` automatically after intent resolution. Activation still unlocks playback on the user gesture.

## 11. Automatic listening-resume correction
`resumeListening` re-arms recognition when the recognizer stopped during speak/process. Barge-in mute window (`bargeInMuteMs`) reduces self-echo cancelling TTS.

## 12. Conversation closure
Farewell intents speak a natural goodbye, stop mic/recognition, and return to idle / standby. Manual End Session unchanged.

## 13. Files added
- `Frontend/.../voiceAgent/dialogueIntents.ts`
- `Frontend/.../voiceAgent/dialogueIntents.test.ts`
- `Backend/sila-voice-engine/IMPLEMENTATION_REPORT_VOICE2_ACK.md`

## 14. Files modified (justification)
- `VoicePoScreen.tsx` — wake intercept, speak ack, resume recognition, barge-in mute
- `conversationConfig.ts` — acknowledging/closing states, greeting min duration, barge-in mute
- `naturalTts.ts` — unlock without immediate cancel
- `VoiceAgentOverlay.tsx` / `.css` — status copy + acknowledging/closing animation hooks (orb palette unchanged)
- `silaVoicePoFrontend.test.ts` — contract assertions
- `sila-voice-engine` `providers.py`, `detector.py`, `engine.py`, `tests/test_engine_core.py` — greeting intents

## 15. End-to-end test results
- `dialogueIntents.test.ts`: pass (Hey/Hi/availability/combined/farewell)
- `silaVoicePoFrontend.test.ts`: pass (contracts)
- `sila-voice-engine` pytest: pass (including new greeting intents)
- Live device mic + audible playback is environment-dependent (browser autoplay / mic permission)

## 16. Response latency
Not fabricated. Greeting path is local (no PO API) → TTS start after end-of-speech silence (~0.2–0.7s for short phrases) + browser speechSynthesis start.

## 17. Mobile browser compatibility
Requires foreground mic permission. iOS Safari Web Speech constraints apply. No locked-screen ambient wake word.

## 18. Remaining limitations
- No custom acoustic “Hey SILA” openWakeWord model shipped
- Pipecat/WebRTC not adopted
- Server neural TTS bytes not enabled in this environment
- Full 10-turn live speech requires a device with mic + unmuted speakers

## 19. SILA Voice 2.0 GUI
Approved orb colors / gradient / rings / End Session red **unchanged**. Only status strings and state animation hooks added.

## 20. PO API configuration
**Unchanged.** No FIVE Create PO / API mapping edits in this task.

## 21. Live SAP postings
**None.** Schedulers remain PARKED. No confirm/posting exercised for this acknowledgement task.

## 22. Unrelated changes
No Material Master, Supplier Master, Inventory, GRN, ITO, approvals, auth, tenant, or unrelated GUI/workflow changes in this task’s voice-ack diff.
