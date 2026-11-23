"""SILA Voice Engine entrypoint. Run: uvicorn app.main:app --host 127.0.0.1 --port 8012"""

from __future__ import annotations

import logging

from fastapi import FastAPI
from fastapi.responses import JSONResponse

from app import __version__
from app.api.routes import router
from app.engine import get_engine
from app.settings import get_settings

logger = logging.getLogger("sila-voice")
logging.basicConfig(level=logging.INFO)

settings = get_settings()
app = FastAPI(
    title="SILA Voice Engine",
    version=__version__,
    description="Isolated enterprise conversational AI engine. Business writes go through existing SILA Buyer APIs only.",
)
app.include_router(router)


@app.get("/health")
def root_health() -> dict:
    return get_engine().health()


@app.exception_handler(Exception)
async def unexpected(request, exc: Exception):  # type: ignore[no-untyped-def]
    logger.error("Unhandled error on %s: %s", request.url.path, type(exc).__name__, exc_info=exc)
    return JSONResponse(status_code=500, content={"error": "INTERNAL_ERROR", "message": "Voice engine could not handle the request."})


@app.on_event("startup")
async def startup() -> None:
    engine = get_engine()
    logger.info(
        "SILA Voice Engine started enabled=%s asr=%s tts=%s agents=%s",
        engine.settings.enabled,
        engine.settings.asr_provider,
        engine.settings.tts_provider,
        engine.settings.allowed_agents,
    )
