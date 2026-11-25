# SILA Voice Engine — Final Implementation Report

Date: 2026-11-23 (UTC) / 2026-11-24 Asia/Dubai

## 1. Existing services reused
- Buyer Voice PO APIs (`/api/v1/buyer/sila/voice-po/sessions` start/turn/confirm/cancel)
- User purchasing profile resolve (`/api/v1/buyer/sila/user-purchasing-profiles/resolve`)
- Existing `CreatePurchaseOrder` path (via Voice PO confirm — unchanged)
- Mobile Voice PO screen (extended with isolated overlay only)
- Cloud cookie / Mobile bearer auth forwarded through Buyer proxy
- Transaction Tracker pattern via Voice PO ledger events (engine audit adapter emits structured events; PO confirm still logs `SILA_REF_VOICE_PO`)

## 2. Python libraries selected (pinned)
fastapi, uvicorn, pydantic, pydantic-settings, httpx, numpy, rapidfuzz, websockets, python-multipart, pytest, pytest-asyncio

## 3. Model names and versions
- Default ASR: **browser** (Web Speech) — no server model download
- Optional ASR: faster-whisper (e.g. `base`) via `requirements-optional.txt`
- Default TTS: **browser** speechSynthesis
- Optional TTS: Piper / Kokoro reserved (not enabled without verified language matrix)
- Wake word: phrase match on transcript; openWakeWord optional — **no pretrained “Hey SILA” model assumed**
- VAD: energy-based default; Silero optional
- LLM: deterministic **rules** provider; Ollama/openai-compatible optional

## 4. Licensing considerations
- Core stack: OSI-friendly (MIT/BSD/Apache as applicable per package)
- Optional faster-whisper / Whisper weights: check OpenAI model license before production enablement
- openWakeWord / custom wake models: train or license a Hey SILA model separately
- Piper/Kokoro voices: verify per-voice licenses; do not clone real persons

## 5. Voice engine architecture
Microphone (browser AEC/NS) → optional server PCM normalize → Energy VAD → quality assess → ASR provider → Rules/LLM intent → Agent Registry (`PURCHASE_ORDER_AGENT`) → Tool Registry → existing Buyer Voice PO APIs → TTS text for browser playback. Barge-in stops TTS client-side and interrupts SPEAKING→TRANSCRIBING.

## 6. Files added
- `Backend/sila-voice-engine/**` (engine, agents, tools, audio, asr, tts, llm, tests, Docker, README)
- `Backend/src/Modules/Buyer/Buyer.API/Controller/SilaVoiceEngineController.cs`
- `Frontend/packages/remote-buyer/src/api/silaMe/silaVoiceEngineApi.ts`
- `Frontend/packages/remote-mobile/src/screens/purchasing/voiceAgent/*`

## 7. Files modified and justification
- `Common.cs` — additive `VoiceEngineUrl` / HTTP client name
- `appsettings.json` / `appsettings.UAT.json` — additive `InterCallService:VoiceEngineUrl` only
- `ServiceExtention.cs` — register voice-engine HttpClient
- `VoicePoScreen.tsx` / frontend test — mount isolated overlay; existing PO flow unchanged
- `scripts/cloud-agent-start.sh` — start voice engine on :8012

## 8. Wake-word
Optional, off by default. Transcript-side “hey sila” match. No continuous ambient upload. Push-to-talk remains primary for Mobile PWA.

## 9. Noise suppression
Browser `echoCancellation` / `noiseSuppression` / `autoGainControl` (existing Voice PO capture). Server default does **not** stack RNNoise/DeepFilterNet.

## 10. Speech recognition
Browser passthrough by default; faster-whisper optional when installed and configured.

## 11. Natural voice
Browser TTS text returned for existing speechSynthesis path; Piper/Kokoro pluggable.

## 12. Conversation state management
Explicit states IDLE→…→COMPLETED/CANCELLED/FAILED with transition guards and barge-in.

## 13. Business agent registry
Active: `PURCHASE_ORDER_AGENT`. Reserved (not implemented / not exposed as working): inventory, stock count, transfer, GRN, approval, recipe, analytics.

## 14. User purchasing defaults
Tool `GetUserPurchasingDefaults` → existing resolve API. LLM cannot invent CC/GL.

## 15. Existing PO API integration
Confirm tool → existing Voice PO confirm → existing `CreatePurchaseOrder`. No parallel PO engine. No auto-approve / SAP transmit.

## 16. Deployment
Port **8012**, Docker image, health/ready endpoints, concurrency semaphore, graceful uvicorn shutdown. Buyer proxy: `/api/v1/buyer/sila/voice-engine/*`.

## 17. CPU/GPU resource measurements
Not benchmarked with production Whisper models in this environment (default browser providers). Enabling faster-whisper `base` on CPU is expected to require additional RAM/disk for model cache; GPU optional and not required.

## 18. Speech recognition evaluation results
No fabricated WER numbers. Unit tests cover VAD energy, quality flags, wake phrase, material ambiguity ranking, HTTP health/auth.

## 19. Voice response latency
Not claimed. Turn path latency depends on Buyer Voice PO + network; engine records `processingDurationMs` per turn when used.

## 20. Automated test results
- `Backend/sila-voice-engine`: **8 passed** (pytest)
- `silaVoicePoFrontend.test.ts`: **ok** (overlay + voice-engine API path assertions)
- Buyer.API build: **0 errors** (proxy controller compiles)

## 21. Browser / PWA limitations
No background/locked-screen wake word. Mic requires foreground permission. iOS Safari Web Speech constraints apply.

## 22. Remaining configuration dependencies
- Start Buyer.API with Voice PO routes
- Start `sila-voice-engine` on 8012
- Seed user PO Defaults for SILA Admin values
- Optional: install `requirements-optional.txt` + set `Voice:AsrProvider=faster-whisper`

## 23. Live SAP postings
None executed. Schedulers remain PARKED. Voice confirm creates SILA draft PO only through existing workflow.

## 24. Existing functionality intact
OCR python-backend, Live Inventory, Recipe, Integration, existing Voice PO APIs preserved. No unrelated GUI redesign.

## 25. Unrelated changes
None intended. Diff limited to voice engine + thin adapters + overlay.
