from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Protocol


@dataclass
class BusinessAgent:
    agent_id: str
    supported_intents: list[str]
    required_permission: str
    required_fields: list[str] = field(default_factory=list)
    allowed_tools: list[str] = field(default_factory=list)
    confirmation_required: bool = True
    implemented: bool = False


class AgentHandler(Protocol):
    async def handle(self, intent: str, payload: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]: ...


class AgentRegistry:
    def __init__(self) -> None:
        self._agents: dict[str, BusinessAgent] = {}
        self._handlers: dict[str, AgentHandler] = {}

    def register(self, agent: BusinessAgent, handler: AgentHandler | None = None) -> None:
        self._agents[agent.agent_id] = agent
        if handler is not None and agent.implemented:
            self._handlers[agent.agent_id] = handler

    def get(self, agent_id: str) -> BusinessAgent | None:
        return self._agents.get(agent_id)

    def active(self) -> list[BusinessAgent]:
        return [a for a in self._agents.values() if a.implemented]

    def reserved(self) -> list[BusinessAgent]:
        return [a for a in self._agents.values() if not a.implemented]

    async def dispatch(self, agent_id: str, intent: str, payload: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        agent = self._agents.get(agent_id)
        if agent is None or not agent.implemented:
            return {"ok": False, "error": "AGENT_NOT_IMPLEMENTED", "message": f"Agent {agent_id} is not active."}
        handler = self._handlers.get(agent_id)
        if handler is None:
            return {"ok": False, "error": "AGENT_HANDLER_MISSING", "message": f"No handler for {agent_id}."}
        if intent not in agent.supported_intents and intent not in {"PO_UTTERANCE", "ASK_REPEAT"}:
            return {"ok": False, "error": "INTENT_NOT_SUPPORTED", "message": f"Intent {intent} not supported by {agent_id}."}
        return await handler.handle(intent, payload, ctx)
