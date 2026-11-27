from __future__ import annotations

import json
import os
import statistics
import time
from pathlib import Path

from app.engines.material_index import TenantMaterialIndexManager
from app.engines.material_matcher import MaterialIntelligenceEngine
from app.schemas.material_ai import (
    EvaluationMetricsResponse,
    IngredientQueryLine,
    MaterialAliasRecord,
    MaterialMasterRecord,
    MaterialMatchingConfig,
)

_EVAL_PATH = Path(__file__).resolve().parents[2] / "data" / "eval_material_matching.json"


def _current_rss_mb() -> float:
    try:
        with open("/proc/self/status", "r", encoding="utf-8") as f:
            for line in f:
                if line.startswith("VmRSS:"):
                    kb = int(line.split()[1])
                    return round(kb / 1024.0, 2)
    except Exception:
        pass
    return 0.0


async def run_material_evaluation(config: MaterialMatchingConfig | None = None) -> EvaluationMetricsResponse:
    cfg = config or MaterialMatchingConfig()
    raw = json.loads(_EVAL_PATH.read_text(encoding="utf-8"))
    materials = [MaterialMasterRecord.model_validate(m) for m in raw["materials"]]
    aliases = [MaterialAliasRecord.model_validate(a) for a in raw["aliases"]]
    cases = raw["cases"]

    mgr = TenantMaterialIndexManager()
    engine = MaterialIntelligenceEngine(mgr)
    tenant_id = "eval-isolated-tenant"

    idx_status = mgr.build_index(tenant_id, materials, aliases, cfg, force_rebuild=True, persist=False)

    top1_hits = 0
    top3_hits = 0
    top5_hits = 0
    retrieval_total = 0

    exact_pred = 0
    exact_tp = 0
    false_positives = 0

    missing_true = 0
    missing_pred = 0
    missing_tp = 0

    spec_cases = 0
    spec_detected = 0
    uom_cases = 0
    uom_detected = 0

    latencies: list[float] = []
    case_results: list[dict] = []

    for c in cases:
        ing = IngredientQueryLine.model_validate(c["ingredient"])
        res = await engine.match_ingredient(tenant_id, ing, cfg)
        latencies.append(res.latency_ms)

        cand_codes = [cand.material_code for cand in res.candidates]
        top_code = res.recommended_candidate.material_code if res.recommended_candidate else None
        passed = True

        if "expected_code" in c:
            retrieval_total += 1
            exp_code = c["expected_code"]
            if top_code == exp_code:
                top1_hits += 1
            if exp_code in cand_codes[:3]:
                top3_hits += 1
            if exp_code in cand_codes[:5]:
                top5_hits += 1
            if c.get("expected_top_k"):
                passed = exp_code in cand_codes[:5]
            elif "expected_status" in c:
                passed = top_code == exp_code and res.status == c["expected_status"]
            else:
                passed = top_code == exp_code

        if "forbidden_top_code" in c:
            spec_cases += 1
            if top_code != c["forbidden_top_code"]:
                spec_detected += 1
            else:
                passed = False
                false_positives += 1

        if c.get("expected_status") == "EXACT_MATCH":
            pass
        if res.status == "EXACT_MATCH":
            exact_pred += 1
            if "expected_code" in c and top_code == c["expected_code"]:
                exact_tp += 1
            else:
                false_positives += 1

        if c.get("expected_status") == "UOM_CONFLICT":
            uom_cases += 1
            if res.status == "UOM_CONFLICT":
                uom_detected += 1
            else:
                passed = False

        if c.get("expected_missing"):
            missing_true += 1
            if res.status in {"UNMATCHED", "MATERIAL_INELIGIBLE"}:
                missing_tp += 1
            else:
                passed = False

        if res.status == "UNMATCHED":
            missing_pred += 1

        if "expected_classification" in c and res.missing_material_classification != c["expected_classification"]:
            passed = False

        if "expected_status" in c and c["expected_status"] in {"MULTIPLE_CANDIDATES", "MATERIAL_INELIGIBLE", "UOM_CONFLICT"}:
            if res.status != c["expected_status"]:
                passed = False

        case_results.append(
            {
                "name": c["name"],
                "status": res.status,
                "top_code": top_code,
                "candidate_codes": cand_codes,
                "missing_classification": res.missing_material_classification,
                "latency_ms": res.latency_ms,
                "passed": passed,
            }
        )

    lat_sorted = sorted(latencies)
    p50 = statistics.median(lat_sorted) if lat_sorted else 0.0
    p95 = lat_sorted[min(len(lat_sorted) - 1, int(len(lat_sorted) * 0.95))] if lat_sorted else 0.0

    return EvaluationMetricsResponse(
        dataset_name=raw["dataset_name"],
        dataset_size=len(cases),
        top1_accuracy=round(top1_hits / max(1, retrieval_total), 4),
        top3_accuracy=round(top3_hits / max(1, retrieval_total), 4),
        top5_accuracy=round(top5_hits / max(1, retrieval_total), 4),
        exact_match_precision=round(exact_tp / max(1, exact_pred), 4) if exact_pred else 1.0,
        false_positive_rate=round(false_positives / max(1, len(cases)), 4),
        missing_material_recall=round(missing_tp / max(1, missing_true), 4) if missing_true else 1.0,
        missing_material_precision=round(missing_tp / max(1, missing_pred), 4) if missing_pred else 1.0,
        spec_conflict_detection_rate=round(spec_detected / max(1, spec_cases), 4) if spec_cases else 1.0,
        uom_conflict_detection_rate=round(uom_detected / max(1, uom_cases), 4) if uom_cases else 1.0,
        p50_latency_ms=round(p50, 2),
        p95_latency_ms=round(p95, 2),
        index_build_ms=idx_status.build_duration_ms,
        process_rss_mb=_current_rss_mb(),
        semantic_backend=idx_status.semantic_backend,
        reranker_backend=cfg.reranking_model if cfg.reranking_enabled else "DISABLED",
        limitations=[
            f"Evaluated on curated isolated test dataset of {len(cases)} labeled culinary matching cases across 21 representative materials.",
            "Production accuracy on tenant catalogs depends on actual Material Master description quality, alias coverage, and verified UOM conversions.",
        ],
        case_results=case_results,
    )
