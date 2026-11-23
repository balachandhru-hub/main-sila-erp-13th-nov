# SILA Voice Engine

Isolated Python FastAPI service for enterprise conversational voice (Alexa/Siri-like interaction for SILA business ops).

- Does **not** replace Buyer.API, existing Voice PO, or `python-backend` OCR/chat.
- Purchase Order Creation is the first registered business agent (`PURCHASE_ORDER_AGENT`).
- All PO writes go through existing Buyer Voice PO APIs (`/api/v1/buyer/sila/voice-po/...`).
- ERP schedulers stay PARKED; no live SAP postings from this service.

## Run (local)

```bash
cd Backend/sila-voice-engine
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --host 127.0.0.1 --port 8012
```

Health: `GET http://127.0.0.1:8012/health`

## Configuration

`appsettings.json` + `Voice__*` / `InterCallService__BuyerUrl` environment overrides.

Optional ML extras: `requirements-optional.txt` (faster-whisper, openwakeword, onnxruntime).  
Default ASR/TTS providers are **browser** (Web Speech) so CPU hosts boot without multi-GB models.

## Auth

Requires existing SILA `Authorization: Bearer` or Cookie. Voice recognition is **not** authentication.

## Docker

```bash
docker build -t sila-voice-engine .
docker run --rm -p 8012:8012 -e InterCallService__BuyerUrl=http://host.docker.internal:8004 sila-voice-engine
```
