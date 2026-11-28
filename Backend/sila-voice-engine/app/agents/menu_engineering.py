"""MENU_ENGINEERING_AGENT — voice assistant for Menu Engineering queries & controlled draft orchestration."""

from __future__ import annotations

import re
from typing import Any

from app.tools.registry import ToolRegistry

_COVERS_RE = re.compile(r"(\d+)\s+(?:covers?|guests?)", re.I)
_MEAL_RE = re.compile(r"\b(breakfast|lunch|dinner|brunch|buffet|all[- ]day)\b", re.I)
_ALT_RE = re.compile(r"alternative[s]?\s+(?:to|for)\s+([a-z0-9\s-]+)", re.I)
_CONFIRM_WORDS_RE = re.compile(r"\b(confirm|save draft|proceed|approve draft)\b", re.I)


class MenuEngineeringAgent:
    def __init__(self, tools: ToolRegistry) -> None:
        self.tools = tools

    async def handle(self, intent: str, payload: dict[str, Any], ctx: dict[str, Any]) -> dict[str, Any]:
        transcript = (payload.get("transcript") or "").strip()

        if intent == "MENU_TODAY_MEAL":
            m = _MEAL_RE.search(transcript)
            meal = m.group(1).title() if m else ""
            res = await self.tools.invoke("GetMenuMobileSummary", {"mealPeriod": meal}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not load today's menu. Check permissions."}
            data = res.get("data") or {}
            plans = data.get("todayMenus") or data.get("publishedMenus") or []
            if meal:
                filtered = [p for p in plans if (p.get("mealPeriod") or "").lower() == meal.lower()]
                if filtered:
                    plans = filtered
            if not plans:
                spoken = f"No {meal.lower() + ' ' if meal else ''}menu is scheduled for today."
            else:
                names = ", ".join(p.get("name", "Menu") for p in plans[:3])
                spoken = f"Today's {meal.lower() + ' ' if meal else ''}menus: {names}."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_CALC_COVERS":
            m = _COVERS_RE.search(transcript)
            covers = int(m.group(1)) if m else 300
            res = await self.tools.invoke("SimulateMenuCovers", {"expectedCovers": covers}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not simulate ingredient requirements."}
            data = res.get("data") or {}
            req = data.get("ingredientRequirement") or 0
            shortage = data.get("shortage") or 0
            spoken = f"For {covers} covers, total ingredient requirement is {req} units with projected shortage {shortage}. Live data is unchanged."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_LOW_MARGIN":
            res = await self.tools.invoke("GetMenuCostMargin", {}, ctx)
            if not res.get("ok"):
                status = res.get("status")
                spoken = "Access denied to menu margins." if status == 403 else "Could not retrieve margin data."
                return {"ok": False, "intent": intent, "results": [res], "spoken": spoken}
            data = res.get("data") or {}
            alerts = data.get("marginAlerts") or []
            if not alerts:
                spoken = "No menu items currently breach margin alert thresholds."
            else:
                items = ", ".join(a.get("menuItemName", a.get("title", "Item")) for a in alerts[:4])
                spoken = f"Low margin items: {items}."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_ALTERNATIVE":
            m = _ALT_RE.search(transcript)
            target = (m.group(1).strip().rstrip("?.!") if m else "ingredient")
            res = await self.tools.invoke("GetMenuAiIntelligence", {"target": target}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not fetch substitution recommendations."}
            data = res.get("data") or {}
            recs = data.get("recommendations") or []
            first = recs[0] if recs else None
            if first:
                spoken = f"Suggested alternative for {target}: {first.get('proposedChange') or first.get('recommendation')}. Approval is required before changing any approved recipe."
            else:
                spoken = f"No approved substitute recommendation is available for {target}; consult Culinary Management."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_CONSUMPTION_ANOMALY":
            res = await self.tools.invoke("GetMenuConsumptionAnomalies", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not retrieve consumption variance anomalies."}
            data = res.get("data") or {}
            anomalies = data.get("anomalies") or []
            if not anomalies:
                spoken = "No abnormal ingredient consumption variances detected."
            else:
                top = anomalies[0]
                cause = (top.get("rankedCauses") or [{}])[0].get("description", "variance under investigation")
                spoken = f"Top variance: {top.get('materialName')} ({top.get('varianceQty')} units). Likely cause: {cause}. Inventory balances remain unchanged."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_PURCHASE_NEXT_WEEK":
            res = await self.tools.invoke("GetMenuPurchasePlan", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not retrieve next week's ingredient purchase plan."}
            data = res.get("data") or {}
            rows = [r for r in (data.get("rows") or []) if (r.get("shortage") or 0) > 0]
            if not rows:
                spoken = "All ingredients for upcoming menus are currently covered by on-hand stock or open purchase orders."
            else:
                top_names = ", ".join(r.get("materialName", "Ingredient") for r in rows[:4])
                spoken = f"{len(rows)} ingredients require purchasing for upcoming menus, led by {top_names}."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_COMPARE_HISTORY":
            res = await self.tools.invoke("GetHistoricalMenus", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not load historical menus for comparison."}
            menus = res.get("data") or []
            if not menus:
                spoken = "No comparable historical menus are available yet."
            else:
                best = sorted(menus, key=lambda m: float(m.get("actualSales") or 0), reverse=True)[0]
                spoken = f"Top historical performer is {best.get('name')} with sales {best.get('actualSales')} and food cost {best.get('historicalFoodCostPercent')}%."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": menus}

        if intent == "MENU_CREATE_OBJECTIVE":
            confirmed = bool(payload.get("confirmed") or _CONFIRM_WORDS_RE.search(transcript))
            res = await self.tools.invoke("RunMeStudioObjective", {"objective": transcript, "confirmation": confirmed}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not run ME Studio optimization. Check permissions."}
            data = res.get("data") or {}
            spoken = data.get("explanation") or "Menu optimization complete."
            if not confirmed:
                spoken += " Please confirm if you want me to save this proposal as a draft menu."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_INGREDIENT_DEMAND_TOMORROW":
            m = re.search(r"how much\s+([a-z0-9\s-]+?)\s+(?:do we need|is needed|required)", transcript, re.I)
            ing_query = (m.group(1).strip() if m else "").lower()
            res = await self.tools.invoke("GetMenuPurchasePlan", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not retrieve tomorrow's ingredient demand."}
            data = res.get("data") or {}
            rows = data.get("rows") or []
            matched = [r for r in rows if not ing_query or ing_query in (r.get("materialName") or "").lower()]
            if matched:
                top = matched[0]
                spoken = f"Tomorrow's predicted requirement for {top.get('materialName')}: {top.get('grossRequirement') or top.get('requiredQty') or 0} {top.get('uom', 'units')} (shortage {top.get('shortage', 0)})."
            else:
                spoken = f"No upcoming menu demand found for {ing_query or 'that ingredient'}."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_EXPECTED_DEMAND":
            m = _MEAL_RE.search(transcript)
            meal = m.group(1).title() if m else "Breakfast"
            res = await self.tools.invoke("GetTwinDemandForecast", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not retrieve expected demand forecast."}
            data = res.get("data") or {}
            nodes = data.get("nodes") or []
            match_node = next((n for n in nodes if meal.lower() in (n.get("nodeName") or n.get("nodeKey") or "").lower()), None)
            if not match_node:
                match_node = next((n for n in nodes if (n.get("level") or "").upper() == "PROPERTY"), None)
            if match_node:
                reconciled = match_node.get("reconciledForecast") or []
                fc_val = match_node.get("tomorrowForecast") if match_node.get("tomorrowForecast") is not None else (reconciled[0] if reconciled else 0)
                model_name = data.get("selectedModel") or match_node.get("selectedModel") or "Digital Twin"
                spoken = f"Expected {meal.lower()} demand tomorrow is {fc_val} covers (selected model: {model_name})."
            else:
                spoken = f"No demand forecast nodes are currently available for {meal.lower()}."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_SHORTAGE_RISK":
            res = await self.tools.invoke("GetMenuPurchasePlan", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not evaluate ingredient shortage risks."}
            data = res.get("data") or {}
            shortages = [r for r in (data.get("rows") or []) if float(r.get("shortage") or 0) > 0]
            if shortages:
                names = ", ".join(r.get("materialName", "Ingredient") for r in shortages[:4])
                spoken = f"{len(shortages)} ingredients may run out based on upcoming demand: {names}."
            else:
                spoken = "No immediate ingredient stockouts are predicted against eligible on-hand stock."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_SMALLER_BATCH_PREP":
            res = await self.tools.invoke("GetTwinWasteRecommendations", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not evaluate smaller-batch prep recommendations."}
            data = res.get("data") or {}
            recs = list(data.get("recommendations") or [])
            if not recs:
                for item in data.get("items") or []:
                    for r in item.get("recommendations") or []:
                        recs.append(f"{item.get('name', 'Dish')}: {r}")
            if recs:
                spoken = f"Smaller-batch prep guidance: {'; '.join(recs[:2])}."
            else:
                spoken = "No high-leftover dishes require smaller-batch splitting right now."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_TODAY_RISKS":
            res = await self.tools.invoke("GetAutonomousControlTower", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not retrieve today's culinary risks."}
            data = res.get("data") or {}
            risks = data.get("criticalRisks") or []
            exposure = float(data.get("estimatedFinancialExposure") or 0)
            if risks:
                top_titles = ", ".join(r.get("title") or r.get("code") or "Risk" for r in risks[:3])
                spoken = f"Found {len(risks)} major culinary risks (estimated exposure {exposure:.2f}): {top_titles}."
            else:
                spoken = "No critical culinary risks detected right now."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_ATTENTION_REQUIRED":
            res = await self.tools.invoke("GetMenuCostMargin", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not evaluate menu attention status."}
            data = res.get("data") or {}
            items = [i for i in (data.get("items") or []) if float(i.get("foodCostPercentage") or 0) > 35]
            if items:
                names = ", ".join(i.get("itemName", "Menu Item") for i in items[:4])
                spoken = f"{len(items)} items require attention due to elevated food cost: {names}."
            else:
                spoken = "All active menu items are currently within configured food cost thresholds."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_FOOD_COST_DRIVER":
            ct = await self.tools.invoke("GetAutonomousControlTower", {}, ctx)
            anom = await self.tools.invoke("GetMenuConsumptionAnomalies", {}, ctx)
            ct_data = (ct.get("data") or {}) if ct.get("ok") else {}
            anom_data = (anom.get("data") or {}) if anom.get("ok") else {}
            risks = ct_data.get("criticalRisks") or []
            causes = anom_data.get("topRootCauses") or anom_data.get("anomalies") or []
            parts = []
            if risks:
                parts.append(risks[0].get("summary") or risks[0].get("title") or "Price variance risk detected")
            if causes:
                first_cause = causes[0] if isinstance(causes[0], str) else (causes[0].get("rootCause") or causes[0].get("materialName") or "consumption variance")
                parts.append(f"Consumption driver: {first_cause}")
            spoken = "; ".join(parts) if parts else "Food cost changes are driven by ingredient price updates and portion consumption variance."
            return {"ok": True, "intent": intent, "results": [ct, anom], "spoken": spoken, "data": {"controlTower": ct_data, "anomalies": anom_data}}

        if intent == "MENU_PENDING_APPROVALS":
            res = await self.tools.invoke("GetAutonomousControlTower", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not check pending approvals."}
            data = res.get("data") or {}
            pending = data.get("pendingApprovalsCount") or len(data.get("pendingApprovals") or [])
            spoken = f"There are {pending} recommendations or action items awaiting authorized approval. Voice commands cannot auto-approve financial changes."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_PREPARE_ACTION_PLAN":
            confirmed = bool(payload.get("confirmed") or _CONFIRM_WORDS_RE.search(transcript))
            if not confirmed:
                return {
                    "ok": True,
                    "intent": intent,
                    "results": [],
                    "requiresConfirmation": True,
                    "spoken": "Preparing a corrective business action plan creates a controlled draft for review and approval. Please confirm if you want me to generate it now.",
                }
            res = await self.tools.invoke("PublishAutonomousEvent", {"eventType": "ForecastDeviationDetected", "objectLabel": "Voice Corrective Action Plan"}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not generate corrective action plan. Check RBAC permissions."}
            data = res.get("data") or {}
            spoken = "Generated consolidated corrective business action plan. High-risk steps still require explicit RBAC approval before execution."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_OPTIMIZE_BUFFET":
            confirmed = bool(payload.get("confirmed") or _CONFIRM_WORDS_RE.search(transcript))
            res = await self.tools.invoke("RunMeStudioObjective", {"objective": transcript, "confirmation": confirmed}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not optimize tomorrow's buffet. Check permissions."}
            data = res.get("data") or {}
            spoken = data.get("explanation") or "Tomorrow's buffet optimization scenario generated."
            if not confirmed:
                spoken += " Draft changes require explicit confirmation and approval before publishing."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_REALIZED_SAVINGS":
            res = await self.tools.invoke("GetAutonomousControlTower", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not retrieve realized savings."}
            data = res.get("data") or {}
            realized = float(data.get("validatedRealizedSavings") or 0)
            potential = float(data.get("potentialSavings") or 0)
            spoken = f"Validated realized savings stand at {realized:.2f}, with {potential:.2f} in additional potential savings under evaluation."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        if intent == "MENU_SUPPLIER_DELIVERY_RISK":
            res = await self.tools.invoke("GetAutonomousControlTower", {}, ctx)
            if not res.get("ok"):
                return {"ok": False, "intent": intent, "results": [res], "spoken": "Could not check supplier delivery risks."}
            data = res.get("data") or {}
            integrations = data.get("integrations") or []
            degraded = [i for i in integrations if (i.get("status") or "").upper() in ("DEGRADED", "UNHEALTHY", "DELAYED")]
            risks = [r for r in (data.get("criticalRisks") or []) if "supplier" in (r.get("title") or r.get("summary") or "").lower() or "delivery" in (r.get("title") or "").lower()]
            if risks or degraded:
                spoken = f"Detected {len(risks)} supplier risk items and {len(degraded)} degraded integration feeds requiring procurement review."
            else:
                spoken = "No active supplier delivery delay risks are currently flagged in the Control Tower."
            return {"ok": True, "intent": intent, "results": [res], "spoken": spoken, "data": data}

        return {"ok": True, "intent": intent, "results": [], "spoken": payload.get("spoken_reply") or "Ready for Menu Engineering commands."}
