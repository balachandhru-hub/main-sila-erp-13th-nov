# 12. SILA Voice Validation Report

**Service:** `Backend/sila-voice-engine` (port `8012`)  
**Health Check Output (Live):** `{"status":"ok","version":"1.0.0","asrProvider":"browser","ttsProvider":"browser","vadProvider":"energy","noiseProvider":"browser","llmProvider":"rules","wakeWordEnabled":false,"allowedAgents":["PURCHASE_ORDER_AGENT","MENU_ENGINEERING_AGENT"],"activeAgents":["PURCHASE_ORDER_AGENT","MENU_ENGINEERING_AGENT"], ... "buyerApiUrl":"http://127.0.0.1:8004"}`

## 1. Test Results
- **`pytest -q` (`Backend/sila-voice-engine`):** **10 passed, 0 failed** in `0.35s`.
- Verified culinary intents (`app/agents/menu_engineering.py`), editable transcript / confirmation flows, and non-bypass of server-side RBAC (voice recognition is strictly an input channel, never authentication or business approval).

## 2. Language & Audio Testing Boundary
- **Automated Deterministic Coverage:** English voice intent routing and slot extraction tests pass (`10/10`).
- **Pending Live Microphone / Multilingual UAT:** Browser WebSpeech ASR/TTS in Arabic, Hindi, Tamil, Malayalam, and mixed-language kitchen commands are configured via browser providers (`asrProvider: browser`, `ttsProvider: browser`) and require interactive human microphone testing on target client devices; live audio accuracy for non-English dialects is **not** claimed without device UAT.
