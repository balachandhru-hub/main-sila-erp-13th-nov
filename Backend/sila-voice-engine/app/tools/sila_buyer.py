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
        registry.register(
            ToolSpec("GetMenuMobileSummary", "Today's and published menus summary", "ViewMeStudio"),
            self.get_menu_mobile_summary,
        )
        registry.register(
            ToolSpec("SimulateMenuCovers", "Simulate ingredient requirements for cover counts without mutating live data", "RunMenuSimulation"),
            self.simulate_menu_covers,
        )
        registry.register(
            ToolSpec("GetMenuCostMargin", "Identify low-margin or high-food-cost menu items", "ViewMenuMargin"),
            self.get_menu_cost_margin,
        )
        registry.register(
            ToolSpec("GetMenuAiIntelligence", "Get recipe or ingredient substitution recommendations", "UseAiMenuIntelligence"),
            self.get_menu_ai_intelligence,
        )
        registry.register(
            ToolSpec("GetMenuConsumptionAnomalies", "Get top ingredient consumption variance root causes", "ViewMeStudio"),
            self.get_menu_consumption_anomalies,
        )
        registry.register(
            ToolSpec("GetMenuPurchasePlan", "Get next week ingredient purchase requirements", "GeneratePurchaseRecommendation"),
            self.get_menu_purchase_plan,
        )
        registry.register(
            ToolSpec("GetHistoricalMenus", "Get historical best-performing menus for comparison", "ViewMeStudio"),
            self.get_historical_menus,
        )
        registry.register(
            ToolSpec("RunMeStudioObjective", "Run ME Studio multi-agent menu generation or optimization (draft requires confirmation)", "UseAiMenuIntelligence"),
            self.run_me_studio_objective,
        )
        registry.register(
            ToolSpec("GetTwinDemandForecast", "Retrieve hierarchical Digital Twin demand forecast by meal period", "ViewMeStudio"),
            self.get_twin_demand_forecast,
        )
        registry.register(
            ToolSpec("GetTwinWasteRecommendations", "Retrieve Digital Twin food waste and smaller-batch prep recommendations", "ViewMeStudio"),
            self.get_twin_waste_recommendations,
        )
        registry.register(
            ToolSpec("GetAutonomousControlTower", "Retrieve autonomous control tower risks, action plans, pending approvals, and realized savings", "ViewMeStudio"),
            self.get_autonomous_control_tower,
        )
        registry.register(
            ToolSpec("PublishAutonomousEvent", "Publish a normalized business event to generate or inspect a corrective action plan", "UseAiMenuIntelligence"),
            self.publish_autonomous_event,
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

    async def get_menu_mobile_summary(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        meal = (args.get("mealPeriod") or "").strip()
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        res = await self._request("GET", f"/api/v1/buyer/sila/me-studio/mobile-summary{qs}", ctx)
        if res.get("ok") and meal and isinstance(res.get("data"), dict):
            res["requestedMealPeriod"] = meal
        return res

    async def simulate_menu_covers(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        covers = int(args.get("expectedCovers") or 300)
        return await self._request(
            "POST",
            "/api/v1/buyer/sila/me-studio/simulate",
            ctx,
            {
                "scenarioName": f"Voice {covers} covers",
                "expectedCovers": covers,
                "saveScenario": False,
            },
        )

    async def get_menu_cost_margin(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/cost-margin{qs}", ctx)

    async def get_menu_ai_intelligence(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/ai-intelligence{qs}", ctx)

    async def get_menu_consumption_anomalies(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/ai/consumption/root-causes{qs}", ctx)

    async def get_menu_purchase_plan(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/purchase-plan{qs}", ctx)

    async def get_historical_menus(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/historical-menus{qs}", ctx)

    async def run_me_studio_objective(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        objective = (args.get("objective") or "").strip()
        save_draft = bool(args.get("confirmation"))
        return await self._request(
            "POST",
            "/api/v1/buyer/sila/me-studio/ai/multi-agent-run",
            ctx,
            {
                "objective": objective,
                "operatingMode": "AI_ASSISTED",
                "propertyId": ctx.get("propertyId"),
                "saveSelectedProposalAsDraft": save_draft,
            },
        )

    async def get_twin_demand_forecast(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}&horizonDays=3" if ctx.get("propertyId") else "?horizonDays=3"
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/twin/forecast/hierarchical{qs}", ctx)

    async def get_twin_waste_recommendations(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/twin/predict/waste{qs}", ctx)

    async def get_autonomous_control_tower(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("GET", f"/api/v1/buyer/sila/me-studio/autonomous/control-tower{qs}", ctx)

    async def publish_autonomous_event(self, args: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        payload = {
            "propertyId": ctx.get("propertyId"),
            "eventType": args.get("eventType") or "ForecastDeviationDetected",
            "sourceSystem": "SILA_VOICE",
            "objectLabel": args.get("objectLabel") or "Voice Corrective Action Plan Request",
            "materialityScore": float(args.get("materialityScore") or 15.0),
            "payload": args.get("payload") or {},
        }
        pub = await self._request("POST", "/api/v1/buyer/sila/me-studio/autonomous/events", ctx, payload)
        if not pub.get("ok"):
            return pub
        qs = f"?propertyId={ctx['propertyId']}" if ctx.get("propertyId") else ""
        return await self._request("POST", f"/api/v1/buyer/sila/me-studio/autonomous/events/process{qs}", ctx, {})
