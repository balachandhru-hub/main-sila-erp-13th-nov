# Python Stack & Licensing Evaluation

## Phase 0 Active Minimal Stack (Commercial-Safe Permissive Licenses)

| Package | Pinned Version | License | Role in Culinary Engine |
|---|---|---|---|
| `Python` (stdlib `decimal`, `asyncio`) | 3.12 | PSF | Exact financial `Decimal` calculations, async execution |
| `fastapi` | 0.115.6 | MIT | Isolated HTTP API microservice |
| `pydantic` | 2.10.4 | MIT | Strict schema validation for AI structured outputs & deterministic engines |
| `pydantic-settings` | 2.7.0 | MIT | Environment & router configuration |
| `uvicorn` | 0.34.0 | BSD-3-Clause | ASGI runtime |
| `httpx` | 0.28.1 | BSD-3-Clause | Async HTTP client for Local/Gemini/Claude provider calls with timeout & cancellation |
| `numpy` | 2.2.1 | BSD-3-Clause | Vector/numerical foundation |
| `RapidFuzz` | 3.11.0 | MIT | Fast fuzzy ingredient-to-material matching (Phase 1B ready) |
| `openpyxl` | 3.1.5 | MIT | Excel workbook parsing/reporting foundation |
| `pytest` / `pytest-asyncio` | 8.3.4 / 0.25.0 | MIT / Apache-2.0 | Automated unit & contract testing |

## Deferred Candidate Libraries (Evaluated for Phases 1–5 — Not Installed in Phase 0)

| Candidate | Target Phase | License | Evaluation & Notes |
|---|---|---|---|
| `google-genai` | Phase 1A | Apache-2.0 | Official Google Gemini SDK (or direct REST via `httpx`) |
| `anthropic` | Phase 1A | MIT | Official Anthropic Claude SDK (or direct REST via `httpx`) |
| `langgraph` / `pydantic-ai` | Phase 1A–2A | MIT | Multi-step agent orchestration with schema validation |
| `ollama` / `llama-cpp-python` | Phase 1A | MIT | Local LLM inference (`llama-cpp-python` requires native C++ build / GGUF weights) |
| `sentence-transformers` / `faiss-cpu` / `spacy` | Phase 1B | Apache-2.0 / MIT | Semantic ingredient matching & NER |
| `Pint` | Phase 1B–2B | BSD-3-Clause | Culinary unit conversions aligned with `UomConverter` |
| `ortools` | Phase 2C / 3B / 4A | Apache-2.0 | CP-SAT kitchen scheduling & constrained procurement/menu optimization |
| `statsforecast` / `lightgbm` / `xgboost` | Phase 5 | Apache-2.0 / MIT | Historical demand & wastage forecasting |
| `PyMuPDF` (`fitz`) | Phase 1A / 3B | AGPL-3.0 (commercial license required if closed-source) | **License flag:** Evaluate `pypdf` (BSD-3-Clause) or `pdfplumber` (MIT) before commercial distribution |
