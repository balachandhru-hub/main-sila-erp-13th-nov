"""Material matching helpers — ranking only; authoritative resolution stays in Buyer Material Master / Voice PO."""

from __future__ import annotations

from dataclasses import dataclass

from rapidfuzz import fuzz


@dataclass
class MaterialCandidate:
    material_code: str
    description: str
    score: float
    match_type: str


class MaterialMatcher:
    """Exact → fuzzy ranking. Never auto-picks ambiguous SKUs."""

    def rank(self, query: str, catalog: list[dict], limit: int = 5) -> list[MaterialCandidate]:
        q = (query or "").strip().lower()
        if not q or not catalog:
            return []
        exact: list[MaterialCandidate] = []
        fuzzy: list[MaterialCandidate] = []
        for row in catalog:
            code = str(row.get("materialCode") or row.get("MaterialCode") or "").strip()
            desc = str(row.get("description") or row.get("Description") or "").strip()
            if not code and not desc:
                continue
            if code.lower() == q:
                exact.append(MaterialCandidate(code, desc, 100.0, "exact_code"))
                continue
            if desc.lower() == q:
                exact.append(MaterialCandidate(code, desc, 99.0, "exact_description"))
                continue
            score = max(
                fuzz.token_set_ratio(q, desc.lower()) if desc else 0,
                fuzz.partial_ratio(q, code.lower()) if code else 0,
            )
            if score >= 70:
                fuzzy.append(MaterialCandidate(code, desc, float(score), "fuzzy"))
        fuzzy.sort(key=lambda c: c.score, reverse=True)
        combined = exact + fuzzy
        # Ambiguity: if top two fuzzy within 3 points and no exact, caller must confirm.
        return combined[:limit]

    def is_ambiguous(self, candidates: list[MaterialCandidate]) -> bool:
        if len(candidates) < 2:
            return False
        if candidates[0].match_type.startswith("exact"):
            return False
        return abs(candidates[0].score - candidates[1].score) <= 3
