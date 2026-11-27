# Python Stack & Licensing Evaluation

## Phase 1A & Phase 1B Active Minimal Stack (Commercial-Safe Permissive Licenses)

| Package | Pinned Version | License | Role in Culinary Engine |
|---|---|---|---|
| `Python` (stdlib `decimal`, `asyncio`, `unicodedata`) | 3.12 | PSF | Exact financial `Decimal` calculations, Unicode NFKC normalization, async execution |
| `fastapi` | 0.115.6 | MIT | Isolated HTTP API microservice |
| `pydantic` | 2.10.4 | MIT | Strict schema validation for AI structured outputs, material matching & deterministic engines |
| `pydantic-settings` | 2.7.0 | MIT | Environment & router configuration |
| `uvicorn` | 0.34.0 | BSD-3-Clause | ASGI runtime |
| `httpx` | 0.28.1 | BSD-3-Clause | Async HTTP client for Local Ollama/OpenAI-compatible health & chat calls |
| `numpy` | 2.2.1 | BSD-3-Clause | Vector/numerical foundation |
| `RapidFuzz` | 3.11.0 | MIT | Fast bounded ingredient fuzzy matching (`ratio`, `partial_ratio`, `token_sort_ratio`, `token_set_ratio`, `WRatio`) |
| `faiss-cpu` | 1.16.0 | MIT | Tenant-isolated dense vector similarity index (`IndexFlatIP` on L2-normalized embeddings) |
| `fastembed` | 0.9.1 | Apache-2.0 | Lightweight ONNX Runtime embedding (`BAAI/bge-small-en-v1.5`) and CrossEncoder reranking (`Xenova/ms-marco-MiniLM-L-6-v2`) without PyTorch/CUDA bloat |
| `onnxruntime` | 1.32.0 | MIT | CPU/GPU ONNX inference runtime pulled by `fastembed` |
| `scikit-learn` | 1.9.1 | BSD-3-Clause | Deterministic TF-IDF character/word n-gram lexical hybrid retriever & fallback |
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

## Phase 1B Embedding & Reranking Model Selection & Licenses

| Model | Role | Dimensions / Footprint | License | Measured CPU Cold-Load + Inference (4 vCPU Xeon) |
|---|---|---|---|---|
| `BAAI/bge-small-en-v1.5` | Primary Semantic Material Embedding (ONNX via `fastembed`) | 384-dim, ~127 MB ONNX | MIT | ~0.85s cold load + first batch; <10ms per query |
| `sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2` | Optional Multilingual Embedding (ONNX via `fastembed`) | 384-dim, ~117 MB ONNX | Apache-2.0 | ~1.4s cold load; <15ms per query |
| `Xenova/ms-marco-MiniLM-L-6-v2` | CrossEncoder Candidate Reranker (ONNX via `fastembed`) | ~85 MB ONNX | Apache-2.0 | ~0.76s cold load; ~15–40ms per candidate shortlist |
| `TFIDF_CHAR_TRIGRAM` | Offline / Zero-Download Deterministic Lexical Vectorizer (`scikit-learn`) | Sparse matrix | BSD-3-Clause | <5ms fit/query; zero external model weights |

## Hardware Requirements (Phase 1B Local-First Engine)

- **Minimum CPU Node:** 2 vCPU, 2 GiB RAM (runs `fastembed` ONNX `BAAI/bge-small-en-v1.5` + `Xenova/ms-marco-MiniLM-L-6-v2` + `faiss-cpu` comfortably up to 50k–100k materials per tenant).
- **Recommended Production Node:** 4 vCPU, 4–8 GiB RAM, SSD storage for persisted per-tenant FAISS indexes (`~1.5 KB` float32 vector per material = ~150 MB RAM for 100,000 materials).
- **GPU:** Optional (`onnxruntime-gpu` / CUDAExecutionProvider); not required for Phase 1B candidate retrieval & shortlist reranking.

## Local AI Runtime & Model Requirements (LLM-Assisted Complex Matching)

- **Supported Local Endpoints:** Ollama (`/api/chat`, `/api/tags`) and OpenAI-compatible local endpoints (`/v1/chat/completions`, `/v1/models`).
- **Recommended Commercial-Permissive Local Models (Apache-2.0 / MIT):**
  - `qwen2.5:7b-instruct` (Apache-2.0, ~4.7 GB Q4_K_M, ~6–8 GB RAM/VRAM) — strong JSON schema adherence.
  - `qwen2.5:14b-instruct` (Apache-2.0, ~9.0 GB Q4_K_M, ~12–16 GB RAM/VRAM) — higher culinary detail.
  - `mistral:7b-instruct` (Apache-2.0, ~4.1 GB Q4_K_M, ~6 GB RAM/VRAM).
- **VM Status:** No local Ollama daemon or LLM weights are pre-installed on this development VM (`NO_OLLAMA`), so Local LLM execution cleanly reports `LOCAL_ENDPOINT_UNREACHABLE` / `PROVIDER_UNAVAILABLE` unless configured. Local ONNX embedding + CrossEncoder + FAISS + RapidFuzz run fully in-process on CPU.

## Deferred Candidate Libraries (Evaluated for Phases 2–5 — Not Installed in Phase 1B)

| Candidate | Target Phase | License | Evaluation & Notes |
|---|---|---|---|
| `ortools` | Phase 2C / 3B / 4A | Apache-2.0 | CP-SAT kitchen scheduling & constrained procurement/menu optimization |
| `statsforecast` / `lightgbm` / `xgboost` | Phase 5 | Apache-2.0 / MIT | Historical demand & wastage forecasting |
| `PyMuPDF` (`fitz`) | Rejected (AGPL-3.0) | AGPL-3.0 | Replaced by `pypdf` + `pdfplumber` to preserve commercial-safe licensing |
