"""Allowlisted enterprise tools only — no SQL/shell/arbitrary HTTP."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any, Awaitable, Callable


ToolHandler = Callable[[dict[str, Any], dict[str, Any]], Awaitable[dict[str, Any]]]


@dataclass(frozen=True)
class ToolSpec:
    name: str
    description: str
    required_permission: str | None = None


class ToolRegistry:
    def __init__(self) -> None:
        self._handlers: dict[str, ToolHandler] = {}
        self._specs: dict[str, ToolSpec] = {}

    def register(self, spec: ToolSpec, handler: ToolHandler) -> None:
        self._specs[spec.name] = spec
        self._handlers[spec.name] = handler

    def allowed_names(self) -> list[str]:
        return sorted(self._handlers)

    async def invoke(self, name: str, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        handler = self._handlers.get(name)
        if handler is None:
            return {"ok": False, "error": "TOOL_NOT_ALLOWED", "message": f"Tool {name} is not registered."}
        try:
            return await handler(args or {}, ctx)
        except Exception as exc:  # noqa: BLE001
            return {"ok": False, "error": "TOOL_FAILED", "message": str(exc)[:400]}
