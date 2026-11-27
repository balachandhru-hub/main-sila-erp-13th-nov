from __future__ import annotations

import hashlib
import json
import os
import re
import threading
import time
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

import faiss
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.preprocessing import normalize as sk_normalize

from app.engines.ingredient_attributes import extract_ingredient_attributes, normalize_for_matching
from app.schemas.material_ai import (
    ExtractedAttributes,
    IndexStatusResponse,
    MaterialAliasRecord,
    MaterialMasterRecord,
    MaterialMatchingConfig,
)

_DEFAULT_INDEX_ROOT = Path(os.getenv("SILA_MATERIAL_INDEX_DIR", str(Path(__file__).resolve().parents[2] / "data" / "indexes")))


class EmbeddingRuntime:
    """
    Lazy singleton manager for ONNX Runtime embeddings (`fastembed`) and CrossEncoder reranker.
    Falls back cleanly to deterministic TF-IDF if model weights cannot be loaded in an offline test environment.
    """

    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._embedders: dict[str, Any] = {}
        self._rerankers: dict[str, Any] = {}
        self._failed_models: dict[str, str] = {}

    def get_embedder(self, model_name: str) -> tuple[Any | None, str]:
        if model_name == "TFIDF_CHAR_TRIGRAM":
            return None, "TFIDF_CHAR_TRIGRAM"
        with self._lock:
            if model_name in self._embedders:
                return self._embedders[model_name], f"FASTEMBED_ONNX:{model_name}"
            if model_name in self._failed_models:
                return None, f"TFIDF_FALLBACK({self._failed_models[model_name]})"
            try:
                from fastembed import TextEmbedding

                emb = TextEmbedding(model_name=model_name)
                self._embedders[model_name] = emb
                return emb, f"FASTEMBED_ONNX:{model_name}"
            except Exception as exc:
                self._failed_models[model_name] = str(exc)[:120]
                return None, f"TFIDF_FALLBACK({self._failed_models[model_name]})"

    def get_reranker(self, model_name: str) -> tuple[Any | None, str]:
        with self._lock:
            if model_name in self._rerankers:
                return self._rerankers[model_name], f"FASTEMBED_CROSS_ENCODER:{model_name}"
            if model_name in self._failed_models:
                return None, f"WEIGHTED_FALLBACK({self._failed_models[model_name]})"
            try:
                from fastembed.rerank.cross_encoder import TextCrossEncoder

                reranker = TextCrossEncoder(model_name=model_name)
                self._rerankers[model_name] = reranker
                return reranker, f"FASTEMBED_CROSS_ENCODER:{model_name}"
            except Exception as exc:
                self._failed_models[model_name] = str(exc)[:120]
                return None, f"WEIGHTED_FALLBACK({self._failed_models[model_name]})"

    def embed_texts(self, texts: list[str], model_name: str, batch_size: int = 32) -> tuple[np.ndarray | None, str]:
        if not texts:
            return None, "EMPTY"
        embedder, backend = self.get_embedder(model_name)
        if embedder is None:
            return None, backend
        vecs = list(embedder.embed(texts, batch_size=batch_size))
        arr = np.asarray(vecs, dtype=np.float32)
        if arr.ndim == 1:
            arr = arr.reshape(1, -1)
        faiss.normalize_L2(arr)
        return arr, backend

    def rerank(self, query: str, documents: list[str], model_name: str) -> tuple[list[float] | None, str]:
        if not documents:
            return [], "EMPTY"
        reranker, backend = self.get_reranker(model_name)
        if reranker is None:
            return None, backend
        scores = [float(s) for s in reranker.rerank(query, documents)]
        return scores, backend


_RUNTIME = EmbeddingRuntime()


def get_embedding_runtime() -> EmbeddingRuntime:
    return _RUNTIME


def _safe_tenant_slug(tenant_id: str) -> str:
    cleaned = re.sub(r"[^A-Za-z0-9_.-]", "_", tenant_id.strip())
    if not cleaned or cleaned in {".", ".."}:
        raise ValueError("Invalid tenant identifier for index isolation.")
    return cleaned


def _material_searchable_text(m: MaterialMasterRecord, aliases: list[str]) -> str:
    parts = [m.description]
    if m.long_description:
        parts.append(m.long_description)
    if m.brand:
        parts.append(m.brand)
    if m.specification:
        parts.append(m.specification)
    if m.material_group:
        parts.append(m.material_group)
    if m.category:
        parts.append(m.category)
    if aliases:
        parts.append(" ".join(aliases))
    return " | ".join(p.strip() for p in parts if p and p.strip())


def _compute_record_revision(m: MaterialMasterRecord, aliases: list[str]) -> str:
    if m.revision_stamp:
        base = f"{m.revision_stamp}|{'|'.join(sorted(aliases))}"
    else:
        payload = f"{m.material_code}|{m.description}|{m.long_description}|{m.brand}|{m.specification}|{m.base_uom}|{m.is_active}|{m.plant_eligible}|{m.purchasing_eligible}|{'|'.join(sorted(aliases))}"
        base = payload
    return hashlib.sha256(base.encode("utf-8")).hexdigest()[:16]


@dataclass
class TenantMaterialIndex:
    tenant_id: str
    status: str = "EMPTY"
    embedding_model: str = "BAAI/bge-small-en-v1.5"
    reranking_model: str = "Xenova/ms-marco-MiniLM-L-6-v2"
    semantic_backend: str = "UNINITIALIZED"
    last_build_utc: datetime | None = None
    last_sync_utc: datetime | None = None
    build_duration_ms: float = 0.0
    errors: list[str] = field(default_factory=list)

    records_by_id: dict[str, MaterialMasterRecord] = field(default_factory=dict)
    ordered_ids: list[str] = field(default_factory=list)
    normalized_texts: list[str] = field(default_factory=list)
    searchable_texts: list[str] = field(default_factory=list)
    extracted_attrs_by_id: dict[str, ExtractedAttributes] = field(default_factory=dict)
    revision_by_id: dict[str, str] = field(default_factory=dict)

    exact_code_map: dict[str, list[str]] = field(default_factory=dict)
    barcode_map: dict[str, list[str]] = field(default_factory=dict)
    normalized_desc_map: dict[str, list[str]] = field(default_factory=dict)
    alias_entries: list[MaterialAliasRecord] = field(default_factory=list)
    alias_map: dict[str, list[tuple[MaterialAliasRecord, str]]] = field(default_factory=dict)
    attribute_inverted_index: dict[str, set[str]] = field(default_factory=dict)

    # Cached embedding vectors keyed by (material_id, revision_hash, model_name)
    vector_cache: dict[tuple[str, str, str], np.ndarray] = field(default_factory=dict)
    faiss_index: faiss.IndexFlatIP | None = None
    vector_dim: int = 0

    tfidf_vectorizer: TfidfVectorizer | None = None
    tfidf_matrix: Any = None


class TenantMaterialIndexManager:
    """
    Strictly tenant-isolated derived search index (§8, §9).
    One tenant's index never shares vectors, aliases, or lookup tables with another tenant.
    """

    def __init__(self, index_root: Path | None = None) -> None:
        self._lock = threading.RLock()
        self._indexes: dict[str, TenantMaterialIndex] = {}
        self._root = index_root or _DEFAULT_INDEX_ROOT

    def _get_or_init(self, tenant_id: str) -> TenantMaterialIndex:
        key = _safe_tenant_slug(tenant_id)
        if key not in self._indexes:
            self._indexes[key] = TenantMaterialIndex(tenant_id=tenant_id)
        return self._indexes[key]

    def get_index(self, tenant_id: str) -> TenantMaterialIndex:
        with self._lock:
            return self._get_or_init(tenant_id)

    def get_status(self, tenant_id: str) -> IndexStatusResponse:
        with self._lock:
            idx = self._get_or_init(tenant_id)
            active_count = sum(1 for r in idx.records_by_id.values() if r.is_active)
            return IndexStatusResponse(
                tenant_id=tenant_id,
                status=idx.status,  # type: ignore[arg-type]
                indexed_material_count=len(idx.records_by_id),
                active_material_count=active_count,
                alias_count=len([a for a in idx.alias_entries if a.approval_status == "APPROVED"]),
                embedding_model=idx.embedding_model,
                reranking_model=idx.reranking_model,
                semantic_backend=idx.semantic_backend,
                faiss_enabled=idx.faiss_index is not None,
                last_build_utc=idx.last_build_utc,
                last_sync_utc=idx.last_sync_utc,
                build_duration_ms=idx.build_duration_ms,
                errors=list(idx.errors),
            )

    def build_index(
        self,
        tenant_id: str,
        materials: list[MaterialMasterRecord],
        aliases: list[MaterialAliasRecord],
        config: MaterialMatchingConfig,
        force_rebuild: bool = False,
        persist: bool = True,
    ) -> IndexStatusResponse:
        t0 = time.perf_counter()
        with self._lock:
            prev = self._indexes.get(_safe_tenant_slug(tenant_id))
            old_cache = {} if (force_rebuild or prev is None) else dict(prev.vector_cache)
            idx = TenantMaterialIndex(
                tenant_id=tenant_id,
                status="BUILDING",
                embedding_model=config.embedding_model,
                reranking_model=config.reranking_model,
                vector_cache=old_cache,
            )

            # Group approved aliases per material_id
            aliases_by_mat: dict[str, list[str]] = {}
            for a in aliases:
                if a.approval_status == "APPROVED":
                    aliases_by_mat.setdefault(a.material_id, []).append(a.alias_text)

            for m in materials:
                idx.records_by_id[m.material_id] = m
                idx.ordered_ids.append(m.material_id)
                merged_aliases = list(dict.fromkeys([*(m.aliases or []), *aliases_by_mat.get(m.material_id, [])]))
                rev = _compute_record_revision(m, merged_aliases)
                idx.revision_by_id[m.material_id] = rev

                norm_desc = normalize_for_matching(m.description)
                idx.normalized_texts.append(norm_desc)
                search_txt = _material_searchable_text(m, merged_aliases)
                idx.searchable_texts.append(search_txt)

                attrs = extract_ingredient_attributes(
                    m.description,
                    category_hint=m.category or m.material_group,
                    brand_hint=m.brand,
                )
                idx.extracted_attrs_by_id[m.material_id] = attrs

                # Exact code map (preserve leading zeros!)
                code_key = m.material_code.strip().upper()
                idx.exact_code_map.setdefault(code_key, []).append(m.material_id)

                if m.barcode and m.barcode.strip():
                    idx.barcode_map.setdefault(m.barcode.strip().upper(), []).append(m.material_id)

                idx.normalized_desc_map.setdefault(norm_desc, []).append(m.material_id)
                if m.long_description:
                    norm_long = normalize_for_matching(m.long_description)
                    if norm_long and norm_long != norm_desc:
                        idx.normalized_desc_map.setdefault(norm_long, []).append(m.material_id)

                # Attribute inverted index
                for tok in attrs.canonical_tokens:
                    idx.attribute_inverted_index.setdefault(tok, set()).add(m.material_id)

            # Build approved alias lookup
            idx.alias_entries = list(aliases)
            for a in aliases:
                if a.approval_status == "APPROVED" and a.material_id in idx.records_by_id:
                    na = normalize_for_matching(a.alias_text)
                    if na:
                        idx.alias_map.setdefault(na, []).append((a, a.material_id))
            # Also include material inline aliases if provided on record
            for m in materials:
                for inline_alias in m.aliases or []:
                    na = normalize_for_matching(inline_alias)
                    if na:
                        syn_rec = MaterialAliasRecord(
                            alias_text=inline_alias,
                            material_id=m.material_id,
                            material_code=m.material_code,
                            source="MATERIAL_MASTER_INLINE",
                            approval_status="APPROVED",
                        )
                        idx.alias_map.setdefault(na, []).append((syn_rec, m.material_id))

            # Build lexical TF-IDF matrix (char_wb + word ngrams)
            if idx.searchable_texts:
                vec = TfidfVectorizer(analyzer="char_wb", ngram_range=(3, 4), lowercase=True, max_features=25000)
                mat = vec.fit_transform([normalize_for_matching(t) or t.lower() for t in idx.searchable_texts])
                idx.tfidf_vectorizer = vec
                idx.tfidf_matrix = sk_normalize(mat)

            # Build FAISS dense vector index using cached embeddings where unchanged
            if config.semantic_matching_enabled and idx.ordered_ids:
                missing_positions: list[int] = []
                missing_texts: list[str] = []
                vectors: list[np.ndarray | None] = [None] * len(idx.ordered_ids)
                for pos, mid in enumerate(idx.ordered_ids):
                    cache_key = (mid, idx.revision_by_id[mid], config.embedding_model)
                    cached_vec = idx.vector_cache.get(cache_key)
                    if cached_vec is not None:
                        vectors[pos] = cached_vec
                    else:
                        missing_positions.append(pos)
                        missing_texts.append(idx.searchable_texts[pos])

                backend_label = "CACHED"
                if missing_texts:
                    computed, backend_label = _RUNTIME.embed_texts(missing_texts, config.embedding_model, config.batch_size)
                    if computed is not None:
                        for i, pos in enumerate(missing_positions):
                            mid = idx.ordered_ids[pos]
                            v = computed[i]
                            vectors[pos] = v
                            idx.vector_cache[(mid, idx.revision_by_id[mid], config.embedding_model)] = v
                else:
                    backend_label = f"FASTEMBED_ONNX:{config.embedding_model}"

                valid_vecs = [v for v in vectors if v is not None]
                if len(valid_vecs) == len(idx.ordered_ids) and valid_vecs:
                    matrix = np.vstack(valid_vecs).astype(np.float32)
                    dim = matrix.shape[1]
                    faiss_idx = faiss.IndexFlatIP(dim)
                    faiss_idx.add(matrix)
                    idx.faiss_index = faiss_idx
                    idx.vector_dim = dim
                    idx.semantic_backend = backend_label
                else:
                    idx.semantic_backend = backend_label if "FALLBACK" in backend_label else "TFIDF_CHAR_TRIGRAM"
            else:
                idx.semantic_backend = "TFIDF_CHAR_TRIGRAM"

            now = datetime.now(timezone.utc)
            idx.last_build_utc = now
            idx.last_sync_utc = now
            idx.build_duration_ms = round((time.perf_counter() - t0) * 1000.0, 2)
            idx.status = "READY"

            # Atomic activation in memory
            self._indexes[_safe_tenant_slug(tenant_id)] = idx

            if persist:
                try:
                    self._persist_atomic(idx)
                except Exception as exc:
                    idx.errors.append(f"Persistence warning: {exc}")

        return self.get_status(tenant_id)

    def sync_index(
        self,
        tenant_id: str,
        upserted_materials: list[MaterialMasterRecord],
        deactivated_material_ids: list[str],
        aliases: list[MaterialAliasRecord] | None,
        config: MaterialMatchingConfig,
    ) -> IndexStatusResponse:
        with self._lock:
            existing = self._get_or_init(tenant_id)
            merged_records = dict(existing.records_by_id)
            for mid in deactivated_material_ids:
                if mid in merged_records:
                    rec = merged_records[mid].model_copy(update={"is_active": False})
                    merged_records[mid] = rec
            for m in upserted_materials:
                merged_records[m.material_id] = m
            merged_aliases = aliases if aliases is not None else existing.alias_entries
            return self.build_index(
                tenant_id=tenant_id,
                materials=list(merged_records.values()),
                aliases=merged_aliases,
                config=config,
                force_rebuild=False,
                persist=True,
            )

    def _persist_atomic(self, idx: TenantMaterialIndex) -> None:
        slug = _safe_tenant_slug(idx.tenant_id)
        target_dir = self._root / slug
        tmp_dir = self._root / f".tmp_{slug}_{int(time.time() * 1000)}"
        target_dir.parent.mkdir(parents=True, exist_ok=True)
        tmp_dir.mkdir(parents=True, exist_ok=True)

        manifest = {
            "tenant_id": idx.tenant_id,
            "status": idx.status,
            "indexed_material_count": len(idx.records_by_id),
            "active_material_count": sum(1 for r in idx.records_by_id.values() if r.is_active),
            "embedding_model": idx.embedding_model,
            "reranking_model": idx.reranking_model,
            "semantic_backend": idx.semantic_backend,
            "last_build_utc": idx.last_build_utc.isoformat() if idx.last_build_utc else None,
            "build_duration_ms": idx.build_duration_ms,
        }
        (tmp_dir / "manifest.json").write_text(json.dumps(manifest, indent=2), encoding="utf-8")
        if idx.faiss_index is not None:
            faiss.write_index(idx.faiss_index, str(tmp_dir / "vectors.faiss"))

        # Atomic replacement of manifest & faiss index files
        target_dir.mkdir(parents=True, exist_ok=True)
        os.replace(tmp_dir / "manifest.json", target_dir / "manifest.json")
        if (tmp_dir / "vectors.faiss").exists():
            os.replace(tmp_dir / "vectors.faiss", target_dir / "vectors.faiss")
        try:
            tmp_dir.rmdir()
        except OSError:
            pass


_DEFAULT_INDEX_MANAGER = TenantMaterialIndexManager()


def get_index_manager() -> TenantMaterialIndexManager:
    return _DEFAULT_INDEX_MANAGER
