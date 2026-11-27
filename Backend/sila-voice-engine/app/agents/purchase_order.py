"""PURCHASE_ORDER_AGENT — extends existing Voice PO; does not create a parallel PO engine."""

from __future__ import annotations

from typing import Any

from app.tools.registry import ToolRegistry


class PurchaseOrderAgent:
    def __init__(self, tools: ToolRegistry) -> None:
        self.tools = tools

    async def handle(self, intent: str, payload: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        transcript = (payload.get("transcript") or "").strip()
        results: list[dict[str, Any]] = []

        if intent == "CANCEL":
            session_id = ctx.get("sila_voice_po_session_id")
            if session_id:
                # Cancel via existing Voice PO API through StartOrContinue path's sibling — call cancel endpoint via tool registry extension
                from app.tools.sila_buyer import SilaBuyerTools

                buyer: SilaBuyerTools | None = ctx.get("_buyer_tools")
                if buyer:
                    cancel = await buyer._request(
                        "POST",
                        f"/api/v1/buyer/sila/voice-po/sessions/{session_id}/cancel",
                        ctx,
                        {},
                    )
                    results.append(cancel)
            return {"ok": True, "intent": intent, "results": results, "spoken": "Conversation cancelled."}

        if intent == "CONFIRM_PO":
            result = await self.tools.invoke(
                "CreatePurchaseOrderDraft",
                {
                    "confirmation": True,
                    "confirmationPhrase": "confirm purchase order",
                    "idempotencyKey": payload.get("idempotencyKey"),
                },
                ctx,
            )
            results.append(result)
            data = (result.get("data") or {}) if result.get("ok") else {}
            po = data.get("createdPoNumber") or data.get("CreatedPoNumber")
            spoken = data.get("spokenReply") or (
                f"Purchase order {po} created." if po else "Purchase order draft created." if result.get("ok") else result.get("message") or "Confirmation failed."
            )
            return {"ok": bool(result.get("ok")), "intent": intent, "results": results, "spoken": spoken, "session": data}

        if intent in {"GO_WITH_DEFAULT", "ADD_PO_LINE", "PO_UTTERANCE"}:
            defaults = await self.tools.invoke("GetUserPurchasingDefaults", {}, ctx)
            results.append(defaults)
            # Accounting values never invented — only from resolve profile.
            turn = await self.tools.invoke(
                "StartOrContinueVoicePo",
                {"transcript": "go with default" if intent == "GO_WITH_DEFAULT" else transcript, "channel": payload.get("channel") or "VOICE"},
                ctx,
            )
            results.append(turn)
            data = (turn.get("data") or {}) if turn.get("ok") else {}
            if data.get("id") or data.get("Id"):
                ctx["sila_voice_po_session_id"] = data.get("id") or data.get("Id")
            spoken = data.get("spokenReply") or data.get("SpokenReply") or payload.get("spoken_reply") or "Draft updated."
            return {
                "ok": bool(turn.get("ok")),
                "intent": intent,
                "results": results,
                "spoken": spoken,
                "session": data,
                "defaults": defaults.get("data"),
            }

        return {
            "ok": True,
            "intent": intent,
            "results": results,
            "spoken": payload.get("spoken_reply") or "Please say the material and quantity.",
        }


def register_default_agents(registry, tools: ToolRegistry, allowed: list[str]) -> PurchaseOrderAgent:
    from app.agents.registry import BusinessAgent

    po = PurchaseOrderAgent(tools)
    if "PURCHASE_ORDER_AGENT" in allowed:
        registry.register(
            BusinessAgent(
                agent_id="PURCHASE_ORDER_AGENT",
                supported_intents=[
                    "ADD_PO_LINE",
                    "GO_WITH_DEFAULT",
                    "CONFIRM_PO",
                    "CANCEL",
                    "PO_UTTERANCE",
                    "ASK_REPEAT",
                ],
                required_permission="requestPurchase",
                required_fields=["material", "quantity", "supplier"],
                allowed_tools=tools.allowed_names(),
                confirmation_required=True,
                implemented=True,
            ),
            po,
        )
    if "MENU_ENGINEERING_AGENT" in allowed:
        from app.agents.menu_engineering import MenuEngineeringAgent

        me = MenuEngineeringAgent(tools)
        registry.register(
            BusinessAgent(
                agent_id="MENU_ENGINEERING_AGENT",
                supported_intents=[
                    "MENU_TODAY_MEAL",
                    "MENU_CALC_COVERS",
                    "MENU_LOW_MARGIN",
                    "MENU_ALTERNATIVE",
                ],
                required_permission="ViewMeStudio",
                required_fields=[],
                allowed_tools=["GetMenuMobileSummary", "SimulateMenuCovers", "GetMenuCostMargin", "GetMenuAiIntelligence"],
                confirmation_required=True,
                implemented=True,
            ),
            me,
        )
    # Reserved future agents — registered but not implemented / not exposed as working.
    for reserved_id in (
        "INVENTORY_QUERY_AGENT",
        "STOCK_COUNT_AGENT",
        "STOCK_TRANSFER_AGENT",
        "GRN_AGENT",
        "APPROVAL_AGENT",
        "RECIPE_AGENT",
        "ANALYTICS_AGENT",
    ):
        registry.register(
            BusinessAgent(
                agent_id=reserved_id,
                supported_intents=[],
                required_permission="",
                implemented=False,
            )
        )
    return po
