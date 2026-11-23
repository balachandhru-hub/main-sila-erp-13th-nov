"""Maps logical tools to existing SILA Buyer Voice PO / purchasing APIs."""

from __future__ import annotations

from typing import Any

import httpx

from app.settings import VoiceSettings
from app.tools.registry import ToolRegistry, ToolSpec


class SilaBuyerTools:
    def __init__(self, settings: VoiceSettings) -> None:
        self.settings = settings
        self.base = settings.buyer_api_url.rstrip("/")

    def _headers(self, ctx: dict[str, Any]) -> dict[str, str]:
        token = (ctx.get("user_token") or "").strip()
        headers = {"Content-Type": "application/json", "Accept": "application/json"}
        if token:
            if token.lower().startswith("bearer "):
                headers["Authorization"] = token
            else:
                headers["Authorization"] = f"Bearer {token}"
        # Cookie-style Cloud sessions: forward Cookie if provided.
        cookie = ctx.get("cookie")
        if cookie:
            headers["Cookie"] = cookie
        return headers

    async def _request(self, method: str, path: str, ctx: dict[str, Any], json_body: dict | None = None) -> dict[str, Any]:
        url = f"{self.base}{path}"
        async with httpx.AsyncClient(timeout=self.settings.request_timeout_seconds) as client:
            response = await client.request(method, url, headers=self._headers(ctx), json=json_body)
            if response.status_code >= 400:
                return {
                    "ok": False,
                    "status": response.status_code,
                    "error": "BUYER_API_ERROR",
                    "message": response.text[:500],
                }
            if not response.content:
                return {"ok": True, "data": None}
            return {"ok": True, "data": response.json()}

    def bind(self, registry: ToolRegistry) -> None:
        registry.register(
            ToolSpec("GetCurrentUserContext", "Current authenticated purchasing context", "requestPurchase"),
            self.get_current_user_context,
        )
        registry.register(
            ToolSpec("GetUserPurchasingDefaults", "Resolve user PO defaults", "requestPurchase"),
            self.get_user_purchasing_defaults,
        )
        registry.register(
            ToolSpec("SearchMaterials", "Search Material Master via Voice PO turn", "requestPurchase"),
            self.search_materials,
        )
        registry.register(
            ToolSpec("GetMaterialDetails", "Material details via Voice PO session draft", "requestPurchase"),
            self.get_material_details,
        )
        registry.register(
            ToolSpec("GetApprovedSuppliers", "Supplier from Voice PO resolution", "requestPurchase"),
            self.get_approved_suppliers,
        )
        registry.register(
            ToolSpec("GetRecentPurchaseHistory", "Go-with-default recent PO history", "requestPurchase"),
            self.get_recent_purchase_history,
        )
        registry.register(
            ToolSpec("GetCurrentPricing", "Pricing from draft lines", "requestPurchase"),
            self.get_current_pricing,
        )
        registry.register(
            ToolSpec("GetOpenPurchaseOrders", "Open POs not exposed as free SQL — via Voice PO clarifications", "requestPurchase"),
            self.get_open_purchase_orders,
        )
        registry.register(
            ToolSpec("ValidatePurchaseDraft", "Validate Voice PO draft", "requestPurchase"),
            self.validate_purchase_draft,
        )
        registry.register(
            ToolSpec("CreatePurchaseOrderDraft", "Confirm draft via existing Voice PO API", "requestPurchase"),
            self.create_purchase_order_draft,
        )
        registry.register(
            ToolSpec("StartOrContinueVoicePo", "Start/continue existing Voice PO session", "requestPurchase"),
            self.start_or_continue_voice_po,
        )

    async def get_current_user_context(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        return {
            "ok": True,
            "data": {
                "locationId": ctx.get("locationId"),
                "propertyId": ctx.get("propertyId"),
                "outletId": ctx.get("outletId"),
                "language": ctx.get("language"),
            },
        }

    async def get_user_purchasing_defaults(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        body = {
            "locationId": args.get("locationId") or ctx.get("locationId"),
            "propertyId": args.get("propertyId") or ctx.get("propertyId"),
            "outletId": args.get("outletId") or ctx.get("outletId"),
            "plantCode": args.get("plantCode"),
            "profileId": args.get("profileId") or ctx.get("purchasingProfileId"),
        }
        return await self._request("POST", "/api/v1/buyer/sila/user-purchasing-profiles/resolve", ctx, body)

    async def start_or_continue_voice_po(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        session_id = args.get("sessionId") or ctx.get("sila_voice_po_session_id")
        transcript = args.get("transcript") or ""
        if session_id:
            return await self._request(
                "POST",
                f"/api/v1/buyer/sila/voice-po/sessions/{session_id}/turns",
                ctx,
                {"transcript": transcript, "channel": args.get("channel") or "VOICE", "language": ctx.get("language")},
            )
        return await self._request(
            "POST",
            "/api/v1/buyer/sila/voice-po/sessions",
            ctx,
            {
                "locationId": ctx.get("locationId"),
                "propertyId": ctx.get("propertyId"),
                "outletId": ctx.get("outletId"),
                "purchasingProfileId": ctx.get("purchasingProfileId"),
                "language": ctx.get("language") or "en",
                "initialTranscript": transcript or None,
            },
        )

    async def search_materials(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        query = args.get("query") or args.get("transcript") or ""
        return await self.start_or_continue_voice_po({"transcript": query, "channel": "VOICE"}, ctx)

    async def get_material_details(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        session_id = ctx.get("sila_voice_po_session_id")
        if not session_id:
            return {"ok": False, "error": "NO_SESSION", "message": "No Voice PO session."}
        return await self._request("GET", f"/api/v1/buyer/sila/voice-po/sessions/{session_id}", ctx)

    async def get_approved_suppliers(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        return await self.get_material_details(args, ctx)

    async def get_recent_purchase_history(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        return await self.start_or_continue_voice_po({"transcript": "go with default", "channel": "VOICE"}, ctx)

    async def get_current_pricing(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        return await self.get_material_details(args, ctx)

    async def get_open_purchase_orders(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        return {"ok": True, "data": {"note": "Open PO listing remains on existing Buyer APIs; not duplicated here."}}

    async def validate_purchase_draft(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        return await self.get_material_details(args, ctx)

    async def create_purchase_order_draft(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        session_id = ctx.get("sila_voice_po_session_id")
        if not session_id:
            return {"ok": False, "error": "NO_SESSION", "message": "Start a Voice PO session first."}
        if not args.get("confirmation"):
            return {"ok": False, "error": "CONFIRMATION_REQUIRED", "message": "Explicit confirmation required."}
        return await self._request(
            "POST",
            f"/api/v1/buyer/sila/voice-po/sessions/{session_id}/confirm",
            ctx,
            {
                "confirmationPhrase": args.get("confirmationPhrase") or "confirm purchase order",
                "idempotencyKey": args.get("idempotencyKey"),
            },
        )
