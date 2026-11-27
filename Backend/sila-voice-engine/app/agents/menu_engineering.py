"""MENU_ENGINEERING_AGENT — voice assistant for Menu Engineering queries (read/simulate only; mutations require approval)."""

from __future__ import annotations

import re
from typing import Any

from app.tools.registry import ToolRegistry

_COVERS_RE = re.compile(r"(\d+)\s+covers?", re.I)
_MEAL_RE = re.compile(r"\b(breakfast|lunch|dinner|brunch|all[- ]day)\b", re.I)
_ALT_RE = re.compile(r"alternative\s+(?:to|for)\s+([a-z0-9\s-]+)", re.I)


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

        return {"ok": True, "intent": intent, "results": [], "spoken": payload.get("spoken_reply") or "Ready for Menu Engineering commands."}
