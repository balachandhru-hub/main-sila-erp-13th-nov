# Python Stack & Licensing Evaluation

## Phase 1A Active Minimal Stack (Commercial-Safe Permissive Licenses)

| Package | Pinned Version | License | Role in Culinary Engine |
|---|---|---|---|
| `Python` (stdlib `decimal`, `asyncio`) | 3.12 | PSF | Exact financial `Decimal` calculations, async execution |
| `fastapi` | 0.115.6 | MIT | Isolated HTTP API microservice |
| `pydantic` | 2.10.4 | MIT | Strict schema validation for AI structured outputs & deterministic engines |
| `pydantic-settings` | 2.7.0 | MIT | Environment & router configuration |
| `uvicorn` | 0.34.0 | BSD-3-Clause | ASGI runtime |
| `httpx` | 0.28.1 | BSD-3-Clause | Async HTTP client for Local Ollama/OpenAI-compatible health & chat calls |
| `numpy` | 2.2.1 | BSD-3-Clause | Vector/numerical foundation |
| `RapidFuzz` | 3.11.0 | MIT | Fast bounded ingredient text normalization & fuzzy lookup |
| `openpyxl` | 3.1.5 | MIT | Excel `.xlsx` recipe extraction & reporting |
| `python-multipart` | 0.0.20 | Apache-2.0 | Authenticated document & dish image upload parsing |
| `Pint` | 0.26.2 | BSD-3-Clause | Deterministic physical UOM conversions (rejecting mass↔volume without density) |
| `pypdf` | 7.2.0 | BSD-3-Clause | Permissive PDF text extraction (replaces AGPL `PyMuPDF`) |
| `pdfplumber` | 0.11.11 | MIT | Structured PDF table/line extraction |
| `python-docx` | 1.2.0 | MIT | Word `.docx` recipe extraction |
| `pillow` | 13.0.0 | MIT-CMU (HPND) | Dish image validation, EXIF stripping & preprocessing |
| `google-genai` | 2.8.0 | Apache-2.0 | Official Google Gemini structured output & multimodal SDK |
| `anthropic` | 2.14.0 | MIT | Official Anthropic Claude structured output & multimodal SDK |
| `pytest` / `pytest-asyncio` | 8.3.4 / 0.25.0 | MIT / Apache-2.0 | Automated unit & contract testing |

## Local AI Runtime & Model Requirements

- **Supported Local Endpoints:** Ollama (`/api/chat`, `/api/tags`) and OpenAI-compatible local endpoints (`/v1/chat/completions`, `/v1/models`).
- **Recommended Commercial-Permissive Local Models (Apache-2.0 / MIT):**
  - `qwen2.5:7b-instruct` (Apache-2.0, ~4.7 GB Q4_K_M, ~6–8 GB RAM/VRAM) — strong JSON schema adherence.
  - `qwen2.5:14b-instruct` (Apache-2.0, ~9.0 GB Q4_K_M, ~12–16 GB RAM/VRAM) — higher culinary detail.
  - `mistral:7b-instruct` (Apache-2.0, ~4.1 GB Q4_K_M, ~6 GB RAM/VRAM).
- **VM Status:** No local Ollama daemon or model weights are pre-installed on this development VM (`NO_OLLAMA`), so Local AI execution correctly fails with `LOCAL_ENDPOINT_UNREACHABLE` / `PROVIDER_UNAVAILABLE` unless an authorized Ollama endpoint is running or mocked in tests.

## Deferred Candidate Libraries (Evaluated for Phases 1B–5 — Not Installed in Phase 1A)

| Candidate | Target Phase | License | Evaluation & Notes |
|---|---|---|---|
| `langgraph` / `pydantic-ai` | Phase 1B–2A | MIT | Multi-agent planning workflows |
| `llama-cpp-python` | Phase 1A+ (Optional) | MIT | In-process GGUF inference if Ollama daemon is not used |
| `sentence-transformers` / `faiss-cpu` / `spacy` | Phase 1B | Apache-2.0 / MIT | Semantic Material Master matching & NER |
| `ortools` | Phase 2C / 3B / 4A | Apache-2.0 | CP-SAT kitchen scheduling & constrained procurement/menu optimization |
| `statsforecast` / `lightgbm` / `xgboost` | Phase 5 | Apache-2.0 / MIT | Historical demand & wastage forecasting |
| `PyMuPDF` (`fitz`) | Rejected (AGPL-3.0) | AGPL-3.0 | Replaced by `pypdf` + `pdfplumber` to preserve commercial-safe licensing |
