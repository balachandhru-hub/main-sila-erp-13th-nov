import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  listCulinaryRecipeDrafts,
  type CulinaryRecipeDraftSummary,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";
import {
  applyIngredientSubstitution,
  bulkAcceptExactMappings,
  confirmMaterialMapping,
  createMaterialAlias,
  decideMaterialAlias,
  exportMaterialReport,
  getMaterialDashboard,
  getMaterialMappingHistory,
  getMaterialWorkspace,
  getPhase1CMaterialHandover,
  listMaterialAliases,
  matchDraftMaterials,
  matchSingleIngredient,
  prepareMaterialRequestDraft,
  recalculateMaterialRequirements,
  rejectMaterialMapping,
  searchMaterialsForMapping,
  suggestIngredientSubstitutions,
  type SilaCulinaryMaterialAlias,
  type SilaCulinaryMaterialCandidate,
  type SilaCulinaryMaterialDashboard,
  type SilaCulinaryMaterialHistory,
  type SilaCulinaryMaterialMappingLine,
  type SilaCulinaryMaterialSearchItem,
  type SilaCulinaryMaterialWorkspace,
  type SilaCulinaryPhase1CHandover,
  type SilaCulinarySubstitutionComparison,
  type SilaCulinarySubstitutionResponse,
} from "../../../../api/silaMe/silaCulinaryMaterialsApi";
import { triggerBlobDownload } from "./culinaryRecipePlannerHelpers";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import { CulinaryKpis } from "./CulinaryWorkspaceKit";
import "./CulinarySuite.css";

interface CulinaryMaterialIntelligenceWorkspaceProps {
  mode: "materials" | "material-requests";
  selectedDraftId?: string | null;
  onSelectDraftId?: (draftId: string | null) => void;
  onOpenWorkspace?: (target: CulinaryWorkspaceKind) => void;
}

type MaterialSubTab = "table" | "dashboard" | "aliases" | "history";
type FilterKey =
  | "ALL"
  | "EXACT"
  | "SUGGESTED"
  | "CONFIRMED"
  | "PENDING_REVIEW"
  | "MISSING"
  | "UOM_CONFLICT"
  | "SPEC_CONFLICT"
  | "STOCK_SHORTAGE"
  | "MISSING_PRICE";
type SortKey = "REF" | "STATUS" | "SCORE_DESC" | "SHORTAGE_DESC";

const DEMO_MATCHING_ROWS = [
  {
    ingredient: "Atlantic Salmon Fillet (Skin-on, Fresh)",
    suggestedCode: "DEMO-MAT-1001",
    description: "SALMON FILLET FRESH NORWEGIAN KG",
    uom: "KG / Case 5 KG",
    stock: "25.0 kg",
    cost: "AED 85.00 / kg",
    status: "EXACT_MATCH",
    evidence: "Exact normalized description + Seafood category + KG UOM",
  },
  {
    ingredient: "Extra Virgin Olive Oil",
    suggestedCode: "DEMO-MAT-1006",
    description: "OLIVE OIL EXTRA VIRGIN MEDITERRANEAN 5L",
    uom: "L / Tin 5 L",
    stock: "10.0 L",
    cost: "AED 35.00 / L",
    status: "SUGGESTED_MATCH",
    evidence: "Fuzzy 91% + Semantic 0.94 (Requires Chef Confirmation)",
  },
  {
    ingredient: "Fresh Dill (Bunch)",
    suggestedCode: "—",
    description: "No authorized Material Master record found",
    uom: "KG / Pack 1 KG",
    stock: "0",
    cost: "AED 45.00 (Est.)",
    status: "MATERIAL_CREATION_REQUIRED",
    evidence: "Missing in Material Master — auto-drafted Material Creation Request",
  },
];

function statusBadgeClass(status: string): string {
  switch (status) {
    case "CONFIRMED":
    case "EXACT_MATCH":
    case "VERIFIED_ALIAS_MATCH":
      return "cs2-badge--live";
    case "SUGGESTED_MATCH":
    case "MULTIPLE_CANDIDATES":
    case "PENDING_MATERIAL_REQUEST":
      return "cs2-badge--warn";
    case "UOM_CONFLICT":
    case "SPECIFICATION_CONFLICT":
    case "MATERIAL_INELIGIBLE":
    case "REJECTED":
    case "UNMATCHED":
      return "cs2-badge--danger";
    default:
      return "cs2-badge--indigo";
  }
}

function Thumbnail({ url, label }: { url?: string | null; label: string }) {
  if (url) {
    return (
      <span className="cs2-thumb" title={label}>
        <img src={url} alt={label} />
      </span>
    );
  }
  const initials = label
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? "")
    .join("");
  return (
    <span className="cs2-thumb" aria-label={`${label} placeholder`}>
      {initials || "MAT"}
    </span>
  );
}

const REQUEST_TYPES = [
  "NEW_MATERIAL",
  "MATERIAL_EXTENSION",
  "UOM_MAINTENANCE",
  "MATERIAL_REACTIVATION",
  "SPECIFICATION_CORRECTION",
  "MANUAL_MASTER_DATA_REVIEW",
];

const CulinaryMaterialIntelligenceWorkspace: React.FC<
  CulinaryMaterialIntelligenceWorkspaceProps
> = ({ mode, selectedDraftId, onSelectDraftId, onOpenWorkspace }) => {
  const [previewMode, setPreviewMode] = useState(false);
  const [drafts, setDrafts] = useState<CulinaryRecipeDraftSummary[]>([]);
  const [activeDraftId, setActiveDraftId] = useState<string>(selectedDraftId ?? "");
  const [loadingDrafts, setLoadingDrafts] = useState(true);
  const [loadingWorkspace, setLoadingWorkspace] = useState(false);
  const [workspace, setWorkspace] = useState<SilaCulinaryMaterialWorkspace | null>(null);
  const [handover, setHandover] = useState<SilaCulinaryPhase1CHandover | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [subTab, setSubTab] = useState<MaterialSubTab>("table");
  const [filterKey, setFilterKey] = useState<FilterKey>("ALL");
  const [searchText, setSearchText] = useState("");
  const openMappingRows = (key: FilterKey) => {
    setFilterKey(key);
    setSearchText("");
    setSubTab("table");
  };
  const [sortKey, setSortKey] = useState<SortKey>("REF");
  const [groupByStatus, setGroupByStatus] = useState(false);

  // Interactive scaling inputs (§30)
  const [scaleServings, setScaleServings] = useState<number>(10);
  const [scaleServingSize, setScaleServingSize] = useState<string>("");
  const [recalcBusy, setRecalcBusy] = useState(false);

  // Matching & bulk actions (§31)
  const [matchingBusy, setMatchingBusy] = useState(false);
  const [useAiDisambig, setUseAiDisambig] = useState(false);
  const matchAbortRef = useRef<AbortController | null>(null);

  // Side drawer state (§29)
  const [drawerRef, setDrawerRef] = useState<string | null>(null);
  const [drawerBusy, setDrawerBusy] = useState(false);
  const [suggestAliasOnConfirm, setSuggestAliasOnConfirm] = useState(false);
  const [rejectReason, setRejectReason] = useState("");
  const [matSearchQuery, setMatSearchQuery] = useState("");
  const [matSearchResults, setMatSearchResults] = useState<SilaCulinaryMaterialSearchItem[]>([]);
  const [matSearchBusy, setMatSearchBusy] = useState(false);

  // Substitution state (§26-27)
  const [subsResponse, setSubsResponse] = useState<SilaCulinarySubstitutionResponse | null>(null);
  const [subsBusy, setSubsBusy] = useState(false);
  const [subRatioOverride, setSubRatioOverride] = useState<Record<string, string>>({});

  // Request draft editor state (§33)
  const [reqType, setReqType] = useState("NEW_MATERIAL");
  const [reqDesc, setReqDesc] = useState("");
  const [reqGroup, setReqGroup] = useState("");
  const [reqBaseUom, setReqBaseUom] = useState("KG");
  const [reqPurchUom, setReqPurchUom] = useState("");
  const [reqPackSize, setReqPackSize] = useState("");
  const [reqStorage, setReqStorage] = useState("");
  const [reqReason, setReqReason] = useState("");

  // Dashboard, Aliases, History sub-tabs (§13, §35, §39)
  const [dashboard, setDashboard] = useState<SilaCulinaryMaterialDashboard | null>(null);
  const [aliases, setAliases] = useState<SilaCulinaryMaterialAlias[]>([]);
  const [aliasFilterStatus, setAliasFilterStatus] = useState("");
  const [aliasSearchText, setAliasSearchText] = useState("");
  const [newAliasText, setNewAliasText] = useState("");
  const [newAliasMatQuery, setNewAliasMatQuery] = useState("");
  const [newAliasMatOptions, setNewAliasMatOptions] = useState<SilaCulinaryMaterialSearchItem[]>([]);
  const [newAliasMatId, setNewAliasMatId] = useState("");
  const [historyRows, setHistoryRows] = useState<SilaCulinaryMaterialHistory[]>([]);

  useEffect(() => {
    if (selectedDraftId && selectedDraftId !== activeDraftId) {
      setActiveDraftId(selectedDraftId);
    }
  }, [selectedDraftId, activeDraftId]);

  const loadDraftList = useCallback(async () => {
    setLoadingDrafts(true);
    try {
      const list = await listCulinaryRecipeDrafts();
      setDrafts(list);
      setActiveDraftId((prev) => {
        if (prev && list.some((d) => d.id === prev)) return prev;
        if (selectedDraftId && list.some((d) => d.id === selectedDraftId)) return selectedDraftId;
        return list[0]?.id ?? "";
      });
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to load AI recipe drafts.");
    } finally {
      setLoadingDrafts(false);
    }
  }, [selectedDraftId]);

  useEffect(() => {
    loadDraftList();
  }, [loadDraftList]);

  const loadActiveWorkspace = useCallback(
    async (draftId: string) => {
      if (!draftId) {
        setWorkspace(null);
        setHandover(null);
        return;
      }
      setLoadingWorkspace(true);
      setError(null);
      try {
        if (mode === "material-requests") {
          const [ws, ho] = await Promise.all([
            getMaterialWorkspace(draftId),
            getPhase1CMaterialHandover(draftId),
          ]);
          setWorkspace(ws);
          setHandover(ho);
          setScaleServings(ws.servings);
          setScaleServingSize(ws.servingSize ?? "");
        } else {
          const ws = await getMaterialWorkspace(draftId);
          setWorkspace(ws);
          setScaleServings(ws.servings);
          setScaleServingSize(ws.servingSize ?? "");
        }
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : "Unable to load Material Intelligence workspace.");
      } finally {
        setLoadingWorkspace(false);
      }
    },
    [mode]
  );

  useEffect(() => {
    if (activeDraftId && !previewMode) {
      loadActiveWorkspace(activeDraftId);
    }
  }, [activeDraftId, previewMode, loadActiveWorkspace]);

  const refreshSubTabData = useCallback(
    async (tab: MaterialSubTab) => {
      try {
        if (tab === "dashboard") {
          const d = await getMaterialDashboard();
          setDashboard(d);
        } else if (tab === "aliases") {
          const a = await listMaterialAliases(aliasFilterStatus || undefined, aliasSearchText || undefined);
          setAliases(a);
        } else if (tab === "history" && activeDraftId) {
          const h = await getMaterialMappingHistory(activeDraftId);
          setHistoryRows(h);
        }
      } catch (err: unknown) {
        toastService.error(err instanceof Error ? err.message : "Failed to load tab data.");
      }
    },
    [activeDraftId, aliasFilterStatus, aliasSearchText]
  );

  useEffect(() => {
    if (!previewMode && subTab !== "table") {
      refreshSubTabData(subTab);
    }
  }, [subTab, previewMode, refreshSubTabData]);

  const drawerLine: SilaCulinaryMaterialMappingLine | null = useMemo(() => {
    if (!drawerRef || !workspace) return null;
    return workspace.lines.find((l) => l.ingredientTempRef === drawerRef) ?? null;
  }, [drawerRef, workspace]);

  const openDrawerForLine = async (line: SilaCulinaryMaterialMappingLine) => {
    setDrawerRef(line.ingredientTempRef);
    setRejectReason("");
    setSuggestAliasOnConfirm(false);
    setSubsResponse(null);
    setMatSearchQuery(line.originalDescription);
    setMatSearchResults([]);
    setReqType(line.requestDraft?.requestType ?? line.recommendedRequestType ?? "NEW_MATERIAL");
    setReqDesc(line.requestDraft?.proposedDescription ?? line.originalDescription.toUpperCase());
    setReqGroup(line.requestDraft?.proposedMaterialGroup ?? "");
    setReqBaseUom(line.requestDraft?.proposedBaseUom ?? line.recipeUom ?? "KG");
    setReqPurchUom(line.requestDraft?.proposedPurchasingUom ?? "");
    setReqPackSize(line.requestDraft?.proposedPackSize ?? "");
    setReqStorage(line.requestDraft?.storageRequirement ?? "");
    setReqReason(line.requestDraft?.reason ?? line.missingReason ?? "");
    if (activeDraftId) {
      getMaterialMappingHistory(activeDraftId, line.ingredientTempRef)
        .then(setHistoryRows)
        .catch(() => {});
    }
  };

  const replaceLineInWorkspace = (updated: SilaCulinaryMaterialMappingLine) => {
    setWorkspace((prev) => {
      if (!prev) return prev;
      return {
        ...prev,
        lines: prev.lines.map((l) =>
          l.ingredientTempRef === updated.ingredientTempRef ? updated : l
        ),
      };
    });
    if (activeDraftId) {
      // Refresh authoritative KPIs and readiness status
      getMaterialWorkspace(activeDraftId).then(setWorkspace).catch(() => {});
    }
  };

  const handleSelectDraft = (draftId: string) => {
    setActiveDraftId(draftId);
    onSelectDraftId?.(draftId || null);
    setDrawerRef(null);
  };

  const handleRunMatch = async (unresolvedOnly: boolean) => {
    if (!activeDraftId) return;
    const ctrl = new AbortController();
    matchAbortRef.current = ctrl;
    setMatchingBusy(true);
    try {
      const ws = await matchDraftMaterials(
        activeDraftId,
        { unresolvedOnly, overwriteConfirmed: false, useAiDisambiguation: useAiDisambig },
        ctrl.signal
      );
      setWorkspace(ws);
      toastService.success(
        `Matching complete: ${ws.kpis.exactMatches} exact, ${ws.kpis.suggestedMatches} suggested, ${ws.kpis.missingMaterials} missing.`
      );
    } catch (err: unknown) {
      if (ctrl.signal.aborted) {
        toastService.info("Material matching request cancelled.");
      } else {
        toastService.error(err instanceof Error ? err.message : "Material matching failed.");
      }
    } finally {
      setMatchingBusy(false);
      matchAbortRef.current = null;
    }
  };

  const handleBulkAcceptExact = async () => {
    if (!activeDraftId) return;
    setMatchingBusy(true);
    try {
      const ws = await bulkAcceptExactMappings(activeDraftId);
      setWorkspace(ws);
      toastService.success(`Confirmed mappings updated (${ws.kpis.confirmedMappings} confirmed).`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Bulk accept failed.");
    } finally {
      setMatchingBusy(false);
    }
  };

  const handleRecalculate = async () => {
    if (!activeDraftId || scaleServings <= 0) return;
    setRecalcBusy(true);
    try {
      const ws = await recalculateMaterialRequirements(activeDraftId, {
        servings: scaleServings,
        servingSize: scaleServingSize || null,
      });
      setWorkspace(ws);
      toastService.success(`Scaled requirements to ${ws.servings} servings without re-running AI matching.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Recalculation failed.");
    } finally {
      setRecalcBusy(false);
    }
  };

  const handleConfirmCandidate = async (
    line: SilaCulinaryMaterialMappingLine,
    materialId: string | null,
    materialCode: string | null
  ) => {
    if (!activeDraftId) return;
    setDrawerBusy(true);
    try {
      const updated = await confirmMaterialMapping(activeDraftId, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        materialId,
        materialCode,
        suggestAlias: suggestAliasOnConfirm,
      });
      replaceLineInWorkspace(updated);
      toastService.success(`Confirmed ${updated.materialCode ?? "material"} for ${line.originalDescription}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Confirmation failed.");
    } finally {
      setDrawerBusy(false);
    }
  };

  const handleRejectLine = async (line: SilaCulinaryMaterialMappingLine) => {
    if (!activeDraftId) return;
    setDrawerBusy(true);
    try {
      const updated = await rejectMaterialMapping(activeDraftId, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        reason: rejectReason.trim() || "Rejected by chef during review",
        feedbackType: "CHEF_REJECTED",
      });
      replaceLineInWorkspace(updated);
      toastService.info(`Rejected candidate for ${line.originalDescription}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Reject failed.");
    } finally {
      setDrawerBusy(false);
    }
  };

  const handlePrepareRequestDraft = async (line: SilaCulinaryMaterialMappingLine) => {
    if (!activeDraftId || !reqDesc.trim()) return;
    setDrawerBusy(true);
    try {
      const updated = await prepareMaterialRequestDraft(activeDraftId, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        requestType: reqType,
        proposedDescription: reqDesc.trim(),
        proposedMaterialGroup: reqGroup.trim() || null,
        proposedBaseUom: reqBaseUom.trim() || "KG",
        proposedPurchasingUom: reqPurchUom.trim() || null,
        proposedPackSize: reqPackSize.trim() || null,
        storageRequirement: reqStorage.trim() || null,
        reason: reqReason.trim() || "Prepared for Phase 1C Master Data Review",
      });
      replaceLineInWorkspace(updated);
      toastService.success("Prepared Phase 1C Material Request Draft (no production material created).");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to save request draft.");
    } finally {
      setDrawerBusy(false);
    }
  };

  const handleRetrySingle = async (line: SilaCulinaryMaterialMappingLine) => {
    if (!activeDraftId) return;
    setDrawerBusy(true);
    try {
      const updated = await matchSingleIngredient(activeDraftId, line.ingredientTempRef, {
        overwriteConfirmed: true,
        useAiDisambiguation: useAiDisambig,
      });
      replaceLineInWorkspace(updated);
      toastService.success(`Re-matched ${line.originalDescription}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Single ingredient match failed.");
    } finally {
      setDrawerBusy(false);
    }
  };

  const handleSearchMaterials = async () => {
    setMatSearchBusy(true);
    try {
      const items = await searchMaterialsForMapping(
        matSearchQuery,
        workspace?.propertyId ?? null,
        20
      );
      setMatSearchResults(items);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Search failed.");
    } finally {
      setMatSearchBusy(false);
    }
  };

  const handleSuggestSubstitutions = async (line: SilaCulinaryMaterialMappingLine) => {
    if (!activeDraftId) return;
    setSubsBusy(true);
    try {
      const res = await suggestIngredientSubstitutions(activeDraftId, line.ingredientTempRef);
      setSubsResponse(res);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Substitution analysis failed.");
    } finally {
      setSubsBusy(false);
    }
  };

  const handleApplySubstitution = async (
    line: SilaCulinaryMaterialMappingLine,
    sub: SilaCulinarySubstitutionComparison
  ) => {
    if (!activeDraftId) return;
    const rawRatio = subRatioOverride[sub.proposed_ingredient];
    const parsedRatio = rawRatio
      ? Number(rawRatio)
      : sub.suggested_replacement_ratio ?? 1;
    setDrawerBusy(true);
    try {
      const updated = await applyIngredientSubstitution(activeDraftId, line.ingredientTempRef, {
        expectedMappingRevision: line.mappingRevision,
        proposedIngredient: sub.proposed_ingredient,
        proposedMaterialId: sub.proposed_material_id ?? null,
        proposedMaterialCode: sub.proposed_material_code ?? null,
        replacementRatio: Number.isFinite(parsedRatio) && parsedRatio > 0 ? parsedRatio : 1,
        reason: `Chef accepted ${sub.substitution_kind}: ${sub.proposed_ingredient}`,
      });
      replaceLineInWorkspace(updated);
      toastService.success("Substitution applied to draft revision only.");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to apply substitution.");
    } finally {
      setDrawerBusy(false);
    }
  };

  const handleExport = async (format: "CSV" | "XLSX" | "PDF") => {
    if (!activeDraftId) return;
    try {
      const { blob, filename } = await exportMaterialReport(activeDraftId, format);
      triggerBlobDownload(blob, filename);
      toastService.success(`Exported ${filename}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Export failed.");
    }
  };

  const filteredLines = useMemo(() => {
    const base = workspace?.lines ?? [];
    const q = searchText.trim().toLowerCase();
    const filtered = base.filter((l) => {
      if (q) {
        const hay = `${l.ingredientTempRef} ${l.originalDescription} ${l.normalizedDescription} ${l.materialCode ?? ""} ${l.materialDescription ?? ""}`.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      switch (filterKey) {
        case "EXACT":
          return (
            l.matchMethod === "EXACT_CODE" ||
            l.matchMethod === "EXACT_DESCRIPTION" ||
            l.matchMethod === "BARCODE" ||
            l.mappingStatus === "EXACT_MATCH"
          );
        case "SUGGESTED":
          return l.mappingStatus === "SUGGESTED_MATCH" || l.mappingStatus === "MULTIPLE_CANDIDATES";
        case "CONFIRMED":
          return l.mappingStatus === "CONFIRMED";
        case "PENDING_REVIEW":
          return l.requiresReview && l.mappingStatus !== "UNMATCHED" && l.mappingStatus !== "PENDING_MATERIAL_REQUEST";
        case "MISSING":
          return l.mappingStatus === "UNMATCHED" || l.mappingStatus === "PENDING_MATERIAL_REQUEST";
        case "UOM_CONFLICT":
          return !l.uomCompatible || l.mappingStatus === "UOM_CONFLICT";
        case "SPEC_CONFLICT":
          return !l.specificationCompatible || l.mappingStatus === "SPECIFICATION_CONFLICT";
        case "STOCK_SHORTAGE":
          return (l.indicativeShortage ?? 0) > 0;
        case "MISSING_PRICE":
          return workspace?.canViewCost ? l.priceStatus === "MISSING" : false;
        default:
          return true;
      }
    });

    const sorted = [...filtered];
    sorted.sort((a, b) => {
      if (sortKey === "SCORE_DESC") return b.matchScore - a.matchScore;
      if (sortKey === "SHORTAGE_DESC") return (b.indicativeShortage ?? 0) - (a.indicativeShortage ?? 0);
      if (sortKey === "STATUS") return a.mappingStatus.localeCompare(b.mappingStatus);
      return a.ingredientTempRef.localeCompare(b.ingredientTempRef);
    });
    if (groupByStatus) {
      sorted.sort((a, b) => a.mappingStatus.localeCompare(b.mappingStatus));
    }
    return sorted;
  }, [workspace, searchText, filterKey, sortKey, groupByStatus]);

  if (previewMode) {
    return (
      <div className="cs2-shell" data-testid={`culinary-${mode}-workspace`}>
        <div className="cs2-header">
          <div>
            <h2 className="cs2-title">
              {mode === "materials" ? "Material Intelligence Engine" : "Material Creation Requests"}
            </h2>
            <p className="cs2-subtitle">Illustrative demonstration mode (sample data only).</p>
          </div>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--primary"
            onClick={() => setPreviewMode(false)}
          >
            Return to Live Material Intelligence
          </button>
        </div>
        <div className="cs2-banner cs2-banner--phase" role="status">
          <div>
            <strong>ILLUSTRATIVE PREVIEW MODE — NON-PRODUCTION SAMPLE</strong>
            <div>Zero production writes. Toggle back to Live Material Intelligence to match real drafts.</div>
          </div>
          <span className="cs2-badge cs2-badge--demo">Demo Only</span>
        </div>
        <section className="cs2-card">
          <div className="cs2-table-wrap">
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>AI Ingredient</th>
                  <th>Suggested Material (Demo)</th>
                  <th>UOM</th>
                  <th>Stock</th>
                  <th>Cost</th>
                  <th>Status &amp; Evidence</th>
                </tr>
              </thead>
              <tbody>
                {DEMO_MATCHING_ROWS.map((row) => (
                  <tr key={row.ingredient}>
                    <td><strong>{row.ingredient}</strong></td>
                    <td>
                      <code>{row.suggestedCode}</code>
                      <div className="cs2-empty-note">{row.description}</div>
                    </td>
                    <td>{row.uom}</td>
                    <td>{row.stock}</td>
                    <td>{row.cost}</td>
                    <td>
                      <span className={`cs2-badge ${statusBadgeClass(row.status)}`}>{row.status}</span>
                      <div className="cs2-empty-note">{row.evidence}</div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      </div>
    );
  }

  return (
    <div className="cs2-shell" data-testid={`culinary-${mode}-workspace`}>
      {/* Header */}
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">
            {mode === "materials"
              ? "Material Intelligence Engine"
              : "Material Request Drafts & Master Data Handover"}
          </h2>
          <p className="cs2-subtitle">
            {mode === "materials"
              ? "Local-first multi-stage ingredient matching against authorized SILA Material Master, UOM conversions, Live Inventory, and authorized prices."
              : "Prepared missing material, plant extension, and UOM maintenance drafts ready for Master Data approval."}
          </p>
        </div>
        <div className="cs2-actions-row">
          <select
            aria-label="Select AI Recipe Draft"
            className="sila-select"
            value={activeDraftId}
            onChange={(e) => handleSelectDraft(e.target.value)}
            disabled={loadingDrafts || drafts.length === 0}
          >
            {drafts.length === 0 ? (
              <option value="">No AI recipe drafts found</option>
            ) : (
              drafts.map((d) => (
                <option key={d.id} value={d.id}>
                  {d.recipeName} ({d.draftReference} · Rev {d.revisionNumber})
                </option>
              ))
            )}
          </select>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--secondary"
            onClick={() => setPreviewMode(true)}
          >
            Illustrative Preview
          </button>
        </div>
      </div>

      {loadingDrafts || loadingWorkspace ? (
        <Loader size={24} message="Loading Material Intelligence workspace..." />
      ) : error ? (
        <EmptyState
          variant="error"
          title="Unable to load Material Intelligence"
          description={error}
          action={
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              onClick={() => activeDraftId && loadActiveWorkspace(activeDraftId)}
            >
              Retry
            </button>
          }
        />
      ) : !workspace ? (
        <EmptyState
          title="No AI Recipe Draft Selected"
          description="Generate or open an AI recipe draft in AI Recipe Planner, then continue to Material Mapping."
          action={
            onOpenWorkspace && (
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                onClick={() => onOpenWorkspace("ai-planner")}
              >
                Open AI Recipe Planner
              </button>
            )
          }
        />
      ) : mode === "material-requests" ? (
        /* ==================== PHASE 1C HANDOVER & MATERIAL REQUESTS VIEW ==================== */
        <>
          <div className="cs2-banner cs2-banner--phase" role="status">
            <div>
              <strong>PHASE 1B BOUNDARY — NO PRODUCTION MATERIAL MASTER CREATION OR SAP POSTINGS</strong>
              <div>
                Readiness: <strong>{handover?.readinessStatus ?? workspace.readinessStatus}</strong> · Draft{" "}
                <code>{workspace.draftReference}</code> (Rev {workspace.draftRevision})
              </div>
            </div>
            <span
              className={`cs2-badge ${
                handover?.readyForPhase1C ? "cs2-badge--live" : "cs2-badge--warn"
              }`}
            >
              {handover?.readyForPhase1C ? "Ready for Costing & Publishing" : "Review / Requests Required"}
            </span>
          </div>

          {handover && handover.outstandingValidationIssues.length > 0 && (
            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Outstanding Validation Issues Blocking Next Step</h3>
                <span className="cs2-badge cs2-badge--danger">
                  {handover.outstandingValidationIssues.length} Issues
                </span>
              </div>
              <ul style={{ margin: 0, paddingLeft: 18 }}>
                {handover.outstandingValidationIssues.map((issue, idx) => (
                  <li key={idx} className="cs2-empty-note" style={{ color: "#991b1b", marginBottom: 4 }}>
                    {issue}
                  </li>
                ))}
              </ul>
            </section>
          )}

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">
                Prepared Material Request Drafts ({handover?.materialRequestDrafts.length ?? 0})
              </h3>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                onClick={() => onOpenWorkspace?.("materials")}
              >
                Back to Material Matching Table
              </button>
            </div>

            {(handover?.materialRequestDrafts.length ?? 0) === 0 ? (
              <p className="cs2-empty-note">
                No missing material request drafts have been prepared for this recipe yet. Open an unmatched or
                extension-needed ingredient in the Material Mapping table to prepare a request draft.
              </p>
            ) : (
              <div className="cs2-shell">
                {handover!.materialRequestDrafts.map((req) => (
                  <div key={req.ingredientTempRef} className="cs2-step-card">
                    <div style={{ display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: 8 }}>
                      <div>
                        <strong>{req.proposedDescription}</strong>{" "}
                        <span className="cs2-badge cs2-badge--indigo">{req.requestType}</span>{" "}
                        {req.isAiSuggested && <span className="cs2-badge cs2-badge--demo">AI-Suggested Fields</span>}
                      </div>
                      <span className="cs2-empty-note">
                        {req.draftReference} · {req.ingredientTempRef} · {req.requiredQuantity} {req.recipeUom}
                      </span>
                    </div>
                    <div className="cs2-form-grid">
                      <div>
                        <span className="sila-label">Base / Purchasing UOM</span>
                        <div>{req.proposedBaseUom ?? "—"} / {req.proposedPurchasingUom ?? "—"}</div>
                      </div>
                      <div>
                        <span className="sila-label">Plant / Property / Requested By</span>
                        <div>{req.plantCode ?? "—"} · {req.requestedBy ?? "—"}</div>
                      </div>
                      <div>
                        <span className="sila-label">Reason</span>
                        <div>{req.reason}</div>
                      </div>
                      <div>
                        <span className="sila-label">Duplicate Check Evidence</span>
                        <div>
                          {req.candidateMaterialCodesChecked.length > 0
                            ? `Checked: ${req.candidateMaterialCodesChecked.join(", ")}`
                            : "No close duplicates found"}
                        </div>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}

            <div className="cs2-actions-row" style={{ marginTop: 14 }}>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled>
                Submit Material Request
              </button>
              <span className="cs2-empty-note">
                Approval routing, production Material Master creation, and SAP material sync happen via authorized Material Requests.
              </span>
            </div>
          </section>
        </>
      ) : (
        /* ==================== PHASE 1B MATERIAL INTELLIGENCE WORKSPACE ==================== */
        <>
          {/* Context Metadata Strip (§28) */}
          <section className="cs2-card">
            <div style={{ display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: 12 }}>
              <div>
                <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
                  <strong style={{ fontSize: 16 }}>{workspace.recipeName}</strong>
                  <span className="cs2-badge cs2-badge--indigo">
                    {workspace.draftReference} · Rev {workspace.draftRevision}
                  </span>
                  <span className={`cs2-badge ${statusBadgeClass(workspace.readinessStatus)}`}>
                    {workspace.readinessStatus}
                  </span>
                </div>
                <div className="cs2-empty-note" style={{ marginTop: 4 }}>
                  Property: <strong>{workspace.propertyName ?? "Unassigned"}</strong> · Kitchen:{" "}
                  <strong>{workspace.kitchenName ?? "—"}</strong> · Plant:{" "}
                  <strong>{workspace.plantCode ?? "—"}</strong> · Engine:{" "}
                  <strong>{workspace.matchingEngine}</strong> · AI Mode:{" "}
                  <strong>{workspace.operatingMode}</strong>
                  {workspace.lastMatchedOnUtc && (
                    <> · Last Matched: {new Date(workspace.lastMatchedOnUtc).toLocaleString()}</>
                  )}
                </div>
              </div>

              {/* Interactive Requirement Preview (§30) */}
              <div className="cs2-actions-row" data-testid="p1b-scaling-preview">
                <label className="sila-label" htmlFor="p1b-servings" style={{ margin: 0 }}>
                  Servings
                </label>
                <input
                  id="p1b-servings"
                  type="number"
                  min={1}
                  step="1"
                  className="sila-input"
                  style={{ width: 84 }}
                  value={scaleServings}
                  onChange={(e) => setScaleServings(Number(e.target.value) || 1)}
                />
                <input
                  aria-label="Serving size"
                  className="sila-input"
                  style={{ width: 110 }}
                  placeholder="e.g. 200 G"
                  value={scaleServingSize}
                  onChange={(e) => setScaleServingSize(e.target.value)}
                />
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  disabled={recalcBusy}
                  onClick={handleRecalculate}
                >
                  {recalcBusy ? "Scaling..." : "Recalculate Qty"}
                </button>
              </div>
            </div>
          </section>

          {/* 10 Live KPI Cards (§28) */}
          <CulinaryKpis
            ariaLabel="Material intelligence metrics"
            testId="p1b-kpi-grid"
            columns={5}
            items={[
            { id: "mi-total", label: "Total Ingredients", value: workspace.kpis.totalIngredients, tone: "blue", drillLabel: "Show all ingredients", selected: subTab === "table" && filterKey === "ALL", onClick: () => openMappingRows("ALL") },
            { id: "mi-exact", label: "Exact Matches", value: workspace.kpis.exactMatches, tone: "green", drillLabel: "Show exact matches", selected: subTab === "table" && filterKey === "EXACT", onClick: () => openMappingRows("EXACT") },
            { id: "mi-suggested", label: "Suggested Matches", value: workspace.kpis.suggestedMatches, tone: "sky", drillLabel: "Show suggested matches", selected: subTab === "table" && filterKey === "SUGGESTED", onClick: () => openMappingRows("SUGGESTED") },
            { id: "mi-confirmed", label: "Confirmed Mappings", value: workspace.kpis.confirmedMappings, tone: "green", drillLabel: "Show confirmed mappings", selected: subTab === "table" && filterKey === "CONFIRMED", onClick: () => openMappingRows("CONFIRMED") },
            { id: "mi-pending", label: "Pending Review", value: workspace.kpis.pendingReview, tone: "amber", drillLabel: "Show ingredients pending review", selected: subTab === "table" && filterKey === "PENDING_REVIEW", onClick: () => openMappingRows("PENDING_REVIEW") },
            { id: "mi-missing", label: "Missing Materials", value: workspace.kpis.missingMaterials, tone: "rose", drillLabel: "Show ingredients with no material", selected: subTab === "table" && filterKey === "MISSING", onClick: () => openMappingRows("MISSING") },
            { id: "mi-uom", label: "UOM Conflicts", value: workspace.kpis.uomConflicts, tone: "amber", drillLabel: "Show UOM conflicts", selected: subTab === "table" && filterKey === "UOM_CONFLICT", onClick: () => openMappingRows("UOM_CONFLICT") },
            { id: "mi-spec", label: "Spec Conflicts", value: workspace.kpis.specificationConflicts, tone: "lavender", drillLabel: "Show specification conflicts", selected: subTab === "table" && filterKey === "SPEC_CONFLICT", onClick: () => openMappingRows("SPEC_CONFLICT") },
            { id: "mi-shortage", label: "Stock Shortages", value: workspace.kpis.stockShortages, tone: "rose", drillLabel: "Show stock shortages", selected: subTab === "table" && filterKey === "STOCK_SHORTAGE", onClick: () => openMappingRows("STOCK_SHORTAGE") },
            { id: "mi-price", label: "Missing Prices", value: workspace.canViewCost ? workspace.kpis.missingPrices : "Restricted", tone: "indigo", drillLabel: "Show ingredients with missing prices", selected: subTab === "table" && filterKey === "MISSING_PRICE", onClick: () => openMappingRows("MISSING_PRICE") },
            ]}
          />

          {/* Sub-Tabs & Bulk Action Toolbar (§31, §39, §40) */}
          <div className="cs2-card">
            <div style={{ display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: 10 }}>
              <div className="cs2-actions-row">
                {(
                  [
                    ["table", "Material Mapping Table"],
                    ["dashboard", "Intelligence Dashboard"],
                    ["aliases", "Alias Governance"],
                    ["history", "Audit History"],
                  ] as [MaterialSubTab, string][]
                ).map(([key, label]) => (
                  <button
                    key={key}
                    type="button"
                    className={`cs2-nav-tab ${subTab === key ? "is-active" : ""}`}
                    onClick={() => setSubTab(key)}
                  >
                    {label}
                  </button>
                ))}
              </div>

              <div className="cs2-actions-row">
                <label className="cs2-checkbox-row" style={{ fontSize: 12 }}>
                  <input
                    type="checkbox"
                    checked={useAiDisambig}
                    onChange={(e) => setUseAiDisambig(e.target.checked)}
                  />
                  AI Disambiguation
                </label>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  disabled={matchingBusy}
                  onClick={() => handleRunMatch(false)}
                >
                  {matchingBusy ? "Matching..." : "Match All Ingredients"}
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  disabled={matchingBusy}
                  onClick={() => handleRunMatch(true)}
                >
                  Rematch Unresolved
                </button>
                {matchingBusy && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    onClick={() => matchAbortRef.current?.abort()}
                  >
                    Cancel
                  </button>
                )}
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  disabled={matchingBusy}
                  onClick={handleBulkAcceptExact}
                >
                  Bulk Accept Exact
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={() => handleExport("XLSX")}
                >
                  Export XLSX
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={() => handleExport("PDF")}
                >
                  PDF
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={() => handleExport("CSV")}
                >
                  CSV
                </button>
                {onOpenWorkspace && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    onClick={() => onOpenWorkspace("material-requests")}
                  >
                    Material Requests Handover ({workspace.kpis.missingMaterials})
                  </button>
                )}
              </div>
            </div>
          </div>

          {subTab === "table" && (
            <section className="cs2-card">
              {/* Filter Pills, Search, Sort & Group (§28) */}
              <div style={{ display: "flex", justifyContent: "space-between", flexWrap: "wrap", gap: 10, marginBottom: 12 }}>
                <div className="cs2-chip-row">
                  {(
                    [
                      ["ALL", "All"],
                      ["EXACT", "Exact"],
                      ["SUGGESTED", "Suggested"],
                      ["CONFIRMED", "Confirmed"],
                      ["PENDING_REVIEW", "Pending Review"],
                      ["MISSING", "Missing"],
                      ["UOM_CONFLICT", "UOM Conflicts"],
                      ["SPEC_CONFLICT", "Spec Conflicts"],
                      ["STOCK_SHORTAGE", "Shortages"],
                      ["MISSING_PRICE", "Missing Price"],
                    ] as [FilterKey, string][]
                  ).map(([k, lbl]) => (
                    <button
                      key={k}
                      type="button"
                      className={`cs2-filter-pill ${filterKey === k ? "is-active" : ""}`}
                      onClick={() => setFilterKey(k)}
                    >
                      {lbl}
                    </button>
                  ))}
                </div>
                <div className="cs2-actions-row">
                  <input
                    className="sila-input"
                    style={{ width: 210 }}
                    placeholder="Filter ingredient or material code..."
                    value={searchText}
                    onChange={(e) => setSearchText(e.target.value)}
                  />
                  <select
                    aria-label="Sort mappings"
                    className="sila-select"
                    value={sortKey}
                    onChange={(e) => setSortKey(e.target.value as SortKey)}
                  >
                    <option value="REF">Sort: Ingredient Ref</option>
                    <option value="STATUS">Sort: Mapping Status</option>
                    <option value="SCORE_DESC">Sort: Highest Score</option>
                    <option value="SHORTAGE_DESC">Sort: Largest Shortage</option>
                  </select>
                  <label className="cs2-checkbox-row" style={{ fontSize: 12 }}>
                    <input
                      type="checkbox"
                      checked={groupByStatus}
                      onChange={(e) => setGroupByStatus(e.target.checked)}
                    />
                    Group by Status
                  </label>
                </div>
              </div>

              <div className="cs2-table-wrap">
                <table className="cs2-table">
                  <thead>
                    <tr>
                      <th>Ingredient</th>
                      <th>Required Qty</th>
                      <th>Suggested Material &amp; Code</th>
                      <th>Base / Purchase UOM</th>
                      <th>Converted &amp; Packs</th>
                      <th>Available Stock</th>
                      <th>Price</th>
                      <th>Method / Score</th>
                      <th>Status</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredLines.length === 0 ? (
                      <tr>
                        <td colSpan={10} className="cs2-empty-note">
                          No ingredient rows match the active filter.
                        </td>
                      </tr>
                    ) : (
                      filteredLines.map((line) => (
                        <tr key={line.ingredientTempRef}>
                          <td>
                            <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                              <Thumbnail url={line.ingredientImageUrl} label={line.originalDescription} />
                              <div>
                                <div>
                                  <strong>{line.originalDescription}</strong>{" "}
                                  <span className="cs2-empty-note">({line.ingredientTempRef})</span>
                                </div>
                                <div className="cs2-empty-note">norm: {line.normalizedDescription}</div>
                              </div>
                            </div>
                          </td>
                          <td>
                            <strong>{line.requiredQuantity}</strong> {line.recipeUom}
                          </td>
                          <td>
                            {line.materialCode ? (
                              <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                <Thumbnail
                                  url={line.materialImageUrl}
                                  label={line.materialDescription ?? line.materialCode}
                                />
                                <div>
                                  <code>{line.materialCode}</code>
                                  <div className="cs2-empty-note">{line.materialDescription}</div>
                                </div>
                              </div>
                            ) : (
                              <span className="cs2-empty-note">— Unmatched —</span>
                            )}
                          </td>
                          <td>
                            {line.baseUom ? (
                              <>
                                <div>{line.baseUom}</div>
                                {line.purchasingUom && (
                                  <div className="cs2-empty-note">Order: {line.purchasingUom}</div>
                                )}
                              </>
                            ) : (
                              "—"
                            )}
                          </td>
                          <td>
                            {line.convertedQuantity != null ? (
                              <>
                                <div>
                                  <strong>{line.convertedQuantity}</strong> {line.baseUom}
                                </div>
                                {line.indicativeWholePacks != null && (
                                  <div className="cs2-empty-note">
                                    ~{line.indicativeWholePacks} pack(s)
                                    {line.excessAfterPackRounding != null
                                      ? ` (+${line.excessAfterPackRounding} excess)`
                                      : ""}
                                  </div>
                                )}
                              </>
                            ) : (
                              <span className="cs2-empty-note">
                                {line.uomCompatible ? "—" : "UOM Conflict"}
                              </span>
                            )}
                          </td>
                          <td>
                            {line.stockStatus === "UNKNOWN" || line.availableStock == null ? (
                              <span className="cs2-badge cs2-badge--planned">Unknown</span>
                            ) : (
                              <div>
                                <strong>{line.availableStock}</strong> {line.stockUom}
                                {(line.indicativeShortage ?? 0) > 0 && (
                                  <div className="cs2-empty-note" style={{ color: "#b91c1c" }}>
                                    Short: {line.indicativeShortage} {line.baseUom}
                                  </div>
                                )}
                              </div>
                            )}
                          </td>
                          <td>
                            {!workspace.canViewCost ? (
                              <span className="cs2-badge cs2-badge--planned">Restricted</span>
                            ) : line.priceStatus === "MISSING" || line.unitPrice == null ? (
                              <span className="cs2-badge cs2-badge--warn">Missing</span>
                            ) : (
                              <div>
                                <strong>
                                  {line.currency} {line.unitPrice.toFixed(2)}
                                </strong>{" "}
                                / {line.priceUom}
                                <div className="cs2-empty-note">{line.priceSource}</div>
                              </div>
                            )}
                          </td>
                          <td>
                            <div>{line.matchMethod}</div>
                            <div className="cs2-empty-note">
                              Score: {(line.matchScore * 100).toFixed(0)}%
                            </div>
                          </td>
                          <td>
                            <span className={`cs2-badge ${statusBadgeClass(line.mappingStatus)}`}>
                              {line.mappingStatus}
                            </span>
                            {line.isSubstitution && (
                              <div className="cs2-empty-note">
                                Sub from <code>{line.substitutedFromMaterialCode}</code>
                              </div>
                            )}
                          </td>
                          <td>
                            <div className="cs2-actions-row">
                              <button
                                type="button"
                                className="sila-btn sila-btn--sm sila-btn--secondary"
                                onClick={() => openDrawerForLine(line)}
                              >
                                Inspect
                              </button>
                              {line.materialId && line.mappingStatus !== "CONFIRMED" && line.uomCompatible && line.specificationCompatible && (
                                <button
                                  type="button"
                                  className="sila-btn sila-btn--sm sila-btn--primary"
                                  onClick={() =>
                                    handleConfirmCandidate(
                                      line,
                                      line.materialId ?? null,
                                      line.materialCode ?? null
                                    )
                                  }
                                >
                                  Confirm
                                </button>
                              )}
                            </div>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {subTab === "dashboard" && (
            <div className="cs2-grid-2" data-testid="p1b-dashboard">
              <section className="cs2-card">
                <div className="cs2-card__head">
                  <h3 className="cs2-card__title">Matching Method Distribution</h3>
                  <span className="cs2-badge cs2-badge--live">
                    {dashboard?.isRealTelemetry ? "Real Audited Telemetry" : "Live"}
                  </span>
                </div>
                {!dashboard ? (
                  <p className="cs2-empty-note">Loading metrics...</p>
                ) : (
                  <div className="cs2-shell">
                    <div className="cs2-empty-note">
                      Total Processed: <strong>{dashboard.totalIngredientsProcessed}</strong> · Local:{" "}
                      <strong>{dashboard.localMatchingUtilizationPct.toFixed(1)}%</strong> · External AI:{" "}
                      <strong>{dashboard.externalAiUtilizationPct.toFixed(1)}%</strong>
                    </div>
                    {dashboard.methodDistribution.map((pt) => {
                      const pct =
                        dashboard.totalIngredientsProcessed > 0
                          ? Math.round((pt.count / dashboard.totalIngredientsProcessed) * 100)
                          : 0;
                      return (
                        <div key={pt.label} className="cs2-bar-row">
                          <span>{pt.label}</span>
                          <div className="cs2-bar-track">
                            <div className="cs2-bar-fill" style={{ width: `${pct}%` }} />
                          </div>
                          <strong>{pt.count}</strong>
                        </div>
                      );
                    })}
                  </div>
                )}
              </section>

              <section className="cs2-card">
                <div className="cs2-card__head">
                  <h3 className="cs2-card__title">Mapping Status &amp; Index Health</h3>
                  <span className="cs2-badge cs2-badge--indigo">
                    {dashboard?.indexHealth.status ?? "READY"}
                  </span>
                </div>
                {dashboard && (
                  <div className="cs2-shell">
                    {dashboard.statusDistribution.map((pt) => {
                      const pct =
                        dashboard.totalIngredientsProcessed > 0
                          ? Math.round((pt.count / dashboard.totalIngredientsProcessed) * 100)
                          : 0;
                      return (
                        <div key={pt.label} className="cs2-bar-row">
                          <span>{pt.label}</span>
                          <div className="cs2-bar-track">
                            <div className="cs2-bar-fill" style={{ width: `${pct}%` }} />
                          </div>
                          <strong>{pt.count}</strong>
                        </div>
                      );
                    })}
                    <div className="cs2-empty-note">
                      Indexed Materials: <strong>{dashboard.indexHealth.indexedMaterialCount}</strong> /{" "}
                      {dashboard.indexHealth.activeMaterialCount} · Model:{" "}
                      <code>{dashboard.indexHealth.embeddingModelVersion}</code> · Active Aliases:{" "}
                      <strong>{dashboard.activeAliases}</strong>
                    </div>
                  </div>
                )}
              </section>
            </div>
          )}

          {subTab === "aliases" && (
            <section className="cs2-card" data-testid="p1b-aliases">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Tenant Material Alias Governance (§13)</h3>
                <div className="cs2-actions-row">
                  <select
                    aria-label="Filter alias status"
                    className="sila-select"
                    value={aliasFilterStatus}
                    onChange={(e) => setAliasFilterStatus(e.target.value)}
                  >
                    <option value="">All Statuses</option>
                    <option value="APPROVED">Approved</option>
                    <option value="PENDING">Pending Approval</option>
                    <option value="INACTIVE">Inactive</option>
                  </select>
                  <input
                    className="sila-input"
                    placeholder="Search alias or code..."
                    value={aliasSearchText}
                    onChange={(e) => setAliasSearchText(e.target.value)}
                  />
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    onClick={() => refreshSubTabData("aliases")}
                  >
                    Refresh
                  </button>
                </div>
              </div>

              <div className="cs2-step-card" style={{ marginBottom: 12 }}>
                <strong>Propose New Culinary Alias</strong>
                <div className="cs2-actions-row">
                  <input
                    className="sila-input"
                    placeholder="Alias phrase (e.g. Aubergine)"
                    value={newAliasText}
                    onChange={(e) => setNewAliasText(e.target.value)}
                  />
                  <input
                    className="sila-input"
                    placeholder="Find Material Master item..."
                    value={newAliasMatQuery}
                    onChange={(e) => setNewAliasMatQuery(e.target.value)}
                  />
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    onClick={async () => {
                      const items = await searchMaterialsForMapping(newAliasMatQuery, workspace.propertyId, 10);
                      setNewAliasMatOptions(items);
                      if (items[0]) setNewAliasMatId(items[0].materialId);
                    }}
                  >
                    Find Material
                  </button>
                  {newAliasMatOptions.length > 0 && (
                    <select
                      aria-label="Target material for alias"
                      className="sila-select"
                      value={newAliasMatId}
                      onChange={(e) => setNewAliasMatId(e.target.value)}
                    >
                      {newAliasMatOptions.map((m) => (
                        <option key={m.materialId} value={m.materialId}>
                          {m.materialCode} — {m.description}
                        </option>
                      ))}
                    </select>
                  )}
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    disabled={!newAliasText.trim() || !newAliasMatId}
                    onClick={async () => {
                      try {
                        await createMaterialAlias({
                          aliasText: newAliasText.trim(),
                          materialId: newAliasMatId,
                          propertyId: workspace.propertyId ?? null,
                        });
                        setNewAliasText("");
                        toastService.success("Alias submitted.");
                        await refreshSubTabData("aliases");
                      } catch (err: unknown) {
                        toastService.error(err instanceof Error ? err.message : "Failed to save alias.");
                      }
                    }}
                  >
                    Save Alias
                  </button>
                </div>
              </div>

              <div className="cs2-table-wrap">
                <table className="cs2-table">
                  <thead>
                    <tr>
                      <th>Alias</th>
                      <th>Material Code</th>
                      <th>Material Description</th>
                      <th>Status</th>
                      <th>Source / Author</th>
                      <th>Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {aliases.map((a) => (
                      <tr key={a.id}>
                        <td><strong>{a.aliasText}</strong></td>
                        <td><code>{a.materialCode}</code></td>
                        <td>{a.materialDescription ?? "—"}</td>
                        <td><span className={`cs2-badge ${statusBadgeClass(a.approvalStatus)}`}>{a.approvalStatus}</span></td>
                        <td className="cs2-empty-note">{a.source} · {a.createdByName}</td>
                        <td>
                          {workspace.canApproveAliases && (
                            <div className="cs2-actions-row">
                              {a.approvalStatus !== "APPROVED" && (
                                <button
                                  type="button"
                                  className="sila-btn sila-btn--sm sila-btn--primary"
                                  onClick={async () => {
                                    await decideMaterialAlias(a.id, true);
                                    await refreshSubTabData("aliases");
                                  }}
                                >
                                  Approve
                                </button>
                              )}
                              {a.approvalStatus !== "INACTIVE" && (
                                <button
                                  type="button"
                                  className="sila-btn sila-btn--sm sila-btn--secondary"
                                  onClick={async () => {
                                    await decideMaterialAlias(a.id, false);
                                    await refreshSubTabData("aliases");
                                  }}
                                >
                                  Deactivate
                                </button>
                              )}
                            </div>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          {subTab === "history" && (
            <section className="cs2-card" data-testid="p1b-history">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Material Mapping Audit History (§35–§36)</h3>
              </div>
              <div className="cs2-table-wrap">
                <table className="cs2-table">
                  <thead>
                    <tr>
                      <th>Timestamp</th>
                      <th>Ref</th>
                      <th>Rev</th>
                      <th>Action</th>
                      <th>Status Change</th>
                      <th>Material Change</th>
                      <th>Actor / Reason</th>
                    </tr>
                  </thead>
                  <tbody>
                    {historyRows.map((h) => (
                      <tr key={h.id}>
                        <td className="cs2-empty-note">{new Date(h.createdOn).toLocaleString()}</td>
                        <td><code>{h.ingredientTempRef}</code></td>
                        <td>v{h.mappingRevision}</td>
                        <td><strong>{h.action}</strong></td>
                        <td>{h.previousStatus ?? "—"} → {h.newStatus}</td>
                        <td><code>{h.previousMaterialCode ?? "—"}</code> → <code>{h.newMaterialCode ?? "—"}</code></td>
                        <td className="cs2-empty-note">{h.actorName}{h.reason ? ` — ${h.reason}` : ""}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}
        </>
      )}

      {/* ==================== MATERIAL CANDIDATE SIDE DRAWER (§29) ==================== */}
      {drawerLine && workspace && (
        <div
          className="cs2-drawer-backdrop"
          role="dialog"
          aria-modal="true"
          aria-label={`Inspect material mapping for ${drawerLine.originalDescription}`}
          data-testid="p1b-candidate-drawer"
        >
          <div className="cs2-drawer">
            <div className="cs2-drawer__head">
              <div>
                <h3 style={{ margin: 0 }}>{drawerLine.originalDescription}</h3>
                <div className="cs2-empty-note">
                  {drawerLine.ingredientTempRef} · Required:{" "}
                  <strong>
                    {drawerLine.requiredQuantity} {drawerLine.recipeUom}
                  </strong>{" "}
                  · Rev {drawerLine.mappingRevision}
                </div>
              </div>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                onClick={() => setDrawerRef(null)}
              >
                Close
              </button>
            </div>

            <div className="cs2-drawer__body">
              {/* Current / Selected Material Summary */}
              <section className="cs2-step-card">
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <strong>Selected Material &amp; Compatibility</strong>
                  <span className={`cs2-badge ${statusBadgeClass(drawerLine.mappingStatus)}`}>
                    {drawerLine.mappingStatus}
                  </span>
                </div>
                {drawerLine.materialCode ? (
                  <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
                    <Thumbnail
                      url={drawerLine.materialImageUrl}
                      label={drawerLine.materialDescription ?? drawerLine.materialCode}
                    />
                    <div>
                      <div>
                        <code>{drawerLine.materialCode}</code> — <strong>{drawerLine.materialDescription}</strong>
                      </div>
                      <div className="cs2-empty-note">
                        Base UOM: {drawerLine.baseUom} · Converted:{" "}
                        {drawerLine.convertedQuantity != null
                          ? `${drawerLine.convertedQuantity} ${drawerLine.baseUom}`
                          : "UOM Conflict"}{" "}
                        · Stock:{" "}
                        {drawerLine.stockStatus === "UNKNOWN" || drawerLine.availableStock == null
                          ? "Unknown"
                          : `${drawerLine.availableStock} ${drawerLine.stockUom} (On-hand ${drawerLine.onHandStock}, Reserved ${drawerLine.reservedStock})`}
                      </div>
                      <div className="cs2-empty-note">
                        Price:{" "}
                        {!workspace.canViewCost
                          ? "Restricted"
                          : drawerLine.unitPrice != null
                            ? `${drawerLine.currency} ${drawerLine.unitPrice.toFixed(2)} / ${drawerLine.priceUom} (${drawerLine.priceSource})`
                            : "Missing"}
                      </div>
                    </div>
                  </div>
                ) : (
                  <p className="cs2-empty-note">No eligible material currently selected.</p>
                )}

                {drawerLine.evidence.length > 0 && (
                  <div className="cs2-empty-note">
                    <strong>Evidence:</strong> {drawerLine.evidence.join(" · ")}
                  </div>
                )}
                {drawerLine.warnings.length > 0 && (
                  <div className="cs2-empty-note" style={{ color: "#b45309" }}>
                    <strong>Warnings:</strong> {drawerLine.warnings.join(" · ")}
                  </div>
                )}

                <div className="cs2-actions-row">
                  <label className="cs2-checkbox-row" style={{ fontSize: 12 }}>
                    <input
                      type="checkbox"
                      checked={suggestAliasOnConfirm}
                      onChange={(e) => setSuggestAliasOnConfirm(e.target.checked)}
                    />
                    Propose description as tenant alias on confirm
                  </label>
                </div>

                <div className="cs2-actions-row">
                  {drawerLine.materialId && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--primary"
                      disabled={drawerBusy || !drawerLine.uomCompatible || !drawerLine.specificationCompatible}
                      onClick={() =>
                        handleConfirmCandidate(
                          drawerLine,
                          drawerLine.materialId ?? null,
                          drawerLine.materialCode ?? null
                        )
                      }
                    >
                      Confirm Current Material
                    </button>
                  )}
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    disabled={drawerBusy}
                    onClick={() => handleRetrySingle(drawerLine)}
                  >
                    Refresh / Rematch
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    disabled={subsBusy}
                    onClick={() => handleSuggestSubstitutions(drawerLine)}
                  >
                    {subsBusy ? "Analyzing..." : "Review Substitutions"}
                  </button>
                </div>
              </section>

              {/* Alternative Ranked Candidates */}
              <section className="cs2-step-card">
                <strong>Ranked Candidates ({drawerLine.candidates.length})</strong>
                {drawerLine.candidates.length === 0 ? (
                  <p className="cs2-empty-note">No ranked candidates available for this ingredient.</p>
                ) : (
                  drawerLine.candidates.map((cand: SilaCulinaryMaterialCandidate) => (
                    <div
                      key={cand.materialId + cand.materialCode}
                      style={{
                        borderTop: "1px solid #e2e8f0",
                        paddingTop: 8,
                        display: "flex",
                        justifyContent: "space-between",
                        gap: 8,
                        alignItems: "center",
                      }}
                    >
                      <div>
                        <div>
                          <code>{cand.materialCode}</code> — <strong>{cand.description}</strong>
                        </div>
                        <div className="cs2-empty-note">
                          {cand.matchMethod} · Score {(cand.finalScore * 100).toFixed(0)}% · Base {cand.baseUom} ·
                          {cand.uomCompatible ? " UOM OK" : " UOM Conflict"} ·
                          {cand.specificationCompatible ? " Spec OK" : " Spec Conflict"} ·
                          {cand.plantEligible ? " Plant Eligible" : " Not Extended"}
                        </div>
                      </div>
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--secondary"
                        disabled={drawerBusy || !cand.materialActive || !cand.plantEligible}
                        onClick={() =>
                          handleConfirmCandidate(drawerLine, cand.materialId, cand.materialCode)
                        }
                      >
                        Select &amp; Confirm
                      </button>
                    </div>
                  ))
                )}
              </section>

              {/* Live Material Master Search */}
              <section className="cs2-step-card">
                <strong>Search Tenant Material Master</strong>
                <div className="cs2-actions-row">
                  <input
                    className="sila-input"
                    style={{ flex: 1 }}
                    placeholder="Search by code, description, barcode, or group..."
                    value={matSearchQuery}
                    onChange={(e) => setMatSearchQuery(e.target.value)}
                  />
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    disabled={matSearchBusy}
                    onClick={handleSearchMaterials}
                  >
                    {matSearchBusy ? "Searching..." : "Search"}
                  </button>
                </div>
                {matSearchResults.map((item) => (
                  <div
                    key={item.materialId}
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      alignItems: "center",
                      borderTop: "1px solid #f1f5f9",
                      paddingTop: 6,
                      gap: 8,
                    }}
                  >
                    <div>
                      <code>{item.materialCode}</code> — <strong>{item.description}</strong>
                      <div className="cs2-empty-note">
                        {item.baseUom} · {item.plantEligible ? "Plant Eligible" : "Not Extended"} · Stock:{" "}
                        {item.stock?.availableQty != null ? `${item.stock.availableQty} ${item.stock.inventoryUom}` : "Unknown"}
                      </div>
                    </div>
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--primary"
                      disabled={drawerBusy || !item.plantEligible}
                      onClick={() =>
                        handleConfirmCandidate(drawerLine, item.materialId, item.materialCode)
                      }
                    >
                      Assign
                    </button>
                  </div>
                ))}
              </section>

              {/* Substitution Comparison (§26-27) */}
              {subsResponse && (
                <section className="cs2-step-card" data-testid="p1b-substitution-panel">
                  <strong>Substitution &amp; Alternative Material Comparison</strong>
                  {subsResponse.suggestions.length === 0 ? (
                    <p className="cs2-empty-note">No substitution options proposed.</p>
                  ) : (
                    subsResponse.suggestions.map((sub) => (
                      <div key={sub.proposed_ingredient} style={{ borderTop: "1px solid #e2e8f0", paddingTop: 8 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", gap: 8 }}>
                          <strong>{sub.proposed_ingredient}</strong>
                          <span className="cs2-badge cs2-badge--indigo">{sub.substitution_kind}</span>
                        </div>
                        <div className="cs2-empty-note">
                          Material: <code>{sub.proposed_material_code ?? "Unlinked"}</code> · Ratio:{" "}
                          {sub.suggested_replacement_ratio ?? "Chef input"} · Taste: {sub.taste_impact} ·
                          Texture: {sub.texture_impact} · Dietary: {sub.dietary_impact} · Allergen:{" "}
                          {sub.allergen_impact}
                        </div>
                        <div className="cs2-actions-row" style={{ marginTop: 6 }}>
                          <input
                            className="sila-input"
                            style={{ width: 95 }}
                            placeholder="Ratio"
                            value={
                              subRatioOverride[sub.proposed_ingredient] ??
                              (sub.suggested_replacement_ratio != null
                                ? String(sub.suggested_replacement_ratio)
                                : "1")
                            }
                            onChange={(e) =>
                              setSubRatioOverride((p) => ({
                                ...p,
                                [sub.proposed_ingredient]: e.target.value,
                              }))
                            }
                          />
                          <button
                            type="button"
                            className="sila-btn sila-btn--sm sila-btn--primary"
                            disabled={drawerBusy}
                            onClick={() => handleApplySubstitution(drawerLine, sub)}
                          >
                            Apply to Draft Only
                          </button>
                        </div>
                      </div>
                    ))
                  )}
                </section>
              )}

              {/* Reject & Prepare Phase 1C Request Draft (§32-34) */}
              <section className="cs2-step-card">
                <strong>Reject Candidate or Prepare Missing Material Request</strong>
                <div className="cs2-actions-row">
                  <input
                    className="sila-input"
                    style={{ flex: 1 }}
                    placeholder="Rejection reason (optional)"
                    value={rejectReason}
                    onChange={(e) => setRejectReason(e.target.value)}
                  />
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    disabled={drawerBusy}
                    onClick={() => handleRejectLine(drawerLine)}
                  >
                    Reject Candidate
                  </button>
                </div>

                <div className="cs2-form-grid" style={{ marginTop: 8 }}>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1b-req-type">Request Type</label>
                    <select
                      id="p1b-req-type"
                      className="sila-select"
                      value={reqType}
                      onChange={(e) => setReqType(e.target.value)}
                    >
                      {REQUEST_TYPES.map((rt) => (
                        <option key={rt} value={rt}>{rt}</option>
                      ))}
                    </select>
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1b-req-desc">Proposed Description</label>
                    <input
                      id="p1b-req-desc"
                      className="sila-input"
                      value={reqDesc}
                      onChange={(e) => setReqDesc(e.target.value)}
                    />
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1b-req-uom">Proposed Base / Order UOM</label>
                    <div style={{ display: "flex", gap: 6 }}>
                      <input
                        id="p1b-req-uom"
                        className="sila-input"
                        value={reqBaseUom}
                        onChange={(e) => setReqBaseUom(e.target.value)}
                      />
                      <input
                        aria-label="Purchasing UOM"
                        className="sila-input"
                        placeholder="Order UOM"
                        value={reqPurchUom}
                        onChange={(e) => setReqPurchUom(e.target.value)}
                      />
                    </div>
                  </div>
                  <div className="sila-field">
                    <label className="sila-label" htmlFor="p1b-req-reason">Justification</label>
                    <input
                      id="p1b-req-reason"
                      className="sila-input"
                      value={reqReason}
                      onChange={(e) => setReqReason(e.target.value)}
                    />
                  </div>
                </div>
                <div>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    disabled={drawerBusy || !reqDesc.trim()}
                    onClick={() => handlePrepareRequestDraft(drawerLine)}
                  >
                    Save Material Request Draft
                  </button>
                </div>
              </section>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CulinaryMaterialIntelligenceWorkspace;
