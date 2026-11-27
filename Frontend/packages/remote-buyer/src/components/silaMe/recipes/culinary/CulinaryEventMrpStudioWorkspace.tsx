import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { listCulinaryEvents, type SilaCulinaryEvent } from "../../../../api/silaMe/silaCulinaryEventMenuApi";
import {
  analyzeMrpEventImpact,
  analyzeMrpWithAi,
  createMaterialRequestFromMrpException,
  createMrpRevision,
  createMrpRun,
  decideMrpApproval,
  exportMrpReport,
  getMrpDashboard,
  getMrpGroupedView,
  getMrpPhase2CHandover,
  getMrpReportCatalog,
  getMrpRun,
  listMrpRuns,
  optimizeMrpBatches,
  recalculateMrpRun,
  releaseMrpForPlanning,
  resolveMrpException,
  saveMrpScenario,
  simulateMrpRun,
  submitMrpForApproval,
  suggestMrpSubstitutions,
  type SilaCulinaryMrpAiAnalyzeResponse,
  type SilaCulinaryMrpDashboard,
  type SilaCulinaryMrpGroupView,
  type SilaCulinaryMrpImpactAnalysis,
  type SilaCulinaryMrpOptimizeResult,
  type SilaCulinaryMrpReportCatalogItem,
  type SilaCulinaryMrpReportResult,
  type SilaCulinaryMrpRunDetail,
  type SilaCulinaryMrpRunSummary,
  type SilaCulinaryMrpSimulationResult,
  type SilaCulinaryMrpSubstitutionSuggestion,
  type SilaCulinaryPhase2CHandover,
} from "../../../../api/silaMe/silaCulinaryMrpApi";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import CulinaryMrpSimulatorWorkspace from "./CulinaryMrpSimulatorWorkspace";
import "./CulinarySuite.css";

interface Props {
  onOpenWorkspace: (target: "list" | "legacy-upload" | CulinaryWorkspaceKind) => void;
}

type MrpTab = "overview" | "bom" | "tree" | "timeline" | "simulator" | "ai" | "governance" | "reports" | "demo";

const GROUP_VIEWS: Array<{ id: string; label: string }> = [
  { id: "MATERIAL", label: "By Material" },
  { id: "RECIPE", label: "By Recipe" },
  { id: "CATEGORY", label: "By Category" },
  { id: "SESSION", label: "By Session" },
  { id: "KITCHEN", label: "By Kitchen" },
  { id: "STORAGE_LOCATION", label: "By Storage Location" },
  { id: "REQUIRED_DATE", label: "By Required Date" },
  { id: "EVENT", label: "By Event" },
  { id: "PROPERTY", label: "By Property" },
];

const AI_PRESETS = [
  "Which materials are critical or short for this event?",
  "Which materials are required earliest?",
  "Which ingredients contribute most to event cost?",
  "How can we reduce the material requirement cost?",
];

const CulinaryEventMrpStudioWorkspace: React.FC<Props> = ({ onOpenWorkspace }) => {
  const [tab, setTab] = useState<MrpTab>("overview");
  const [dashboard, setDashboard] = useState<SilaCulinaryMrpDashboard | null>(null);
  const [runs, setRuns] = useState<SilaCulinaryMrpRunSummary[]>([]);
  const [events, setEvents] = useState<SilaCulinaryEvent[]>([]);
  const [activeRun, setActiveRun] = useState<SilaCulinaryMrpRunDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // New MRP run creator state
  const [showNewRun, setShowNewRun] = useState(false);
  const [newName, setNewName] = useState("");
  const [newScope, setNewScope] = useState("SINGLE_EVENT");
  const [selectedEventIds, setSelectedEventIds] = useState<string[]>([]);
  const [newIsSim, setNewIsSim] = useState(false);

  // BOM / Consolidated View state
  const [groupBy, setGroupBy] = useState("MATERIAL");
  const [groupedView, setGroupedView] = useState<SilaCulinaryMrpGroupView | null>(null);
  const [bomSearch, setBomSearch] = useState("");
  const [bomStatusFilter, setBomStatusFilter] = useState("ALL");
  const [expandedLineId, setExpandedLineId] = useState<string | null>(null);

  // Tree state
  const [expandedNodes, setExpandedNodes] = useState<Record<string, boolean>>({});
  const [treeSearch, setTreeSearch] = useState("");

  // Simulator state
  const [guestScale, setGuestScale] = useState(100);
  const [allowancePct, setAllowancePct] = useState(0);
  const [bufferPct, setBufferPct] = useState(0);
  const [yieldPct, setYieldPct] = useState(0);
  const [pricePct, setPricePct] = useState(0);
  const [prepMode, setPrepMode] = useState<"ADJUSTED_SCENARIO" | "GROSS_ONLY">("ADJUSTED_SCENARIO");
  const [simResult, setSimResult] = useState<SilaCulinaryMrpSimulationResult | null>(null);
  const [scenarioName, setScenarioName] = useState("");
  const simDebounceRef = useRef<number | null>(null);

  // AI & Optimization state
  const [aiQuestion, setAiQuestion] = useState(AI_PRESETS[0]);
  const [aiAnswer, setAiAnswer] = useState<SilaCulinaryMrpAiAnalyzeResponse | null>(null);
  const [substitutions, setSubstitutions] = useState<SilaCulinaryMrpSubstitutionSuggestion[]>([]);
  const [optResult, setOptResult] = useState<SilaCulinaryMrpOptimizeResult | null>(null);

  // Governance & Handover state
  const [impact, setImpact] = useState<SilaCulinaryMrpImpactAnalysis | null>(null);
  const [handover, setHandover] = useState<SilaCulinaryPhase2CHandover | null>(null);
  const [approvalComment, setApprovalComment] = useState("");

  // Report Studio state
  const [reportCatalog, setReportCatalog] = useState<SilaCulinaryMrpReportCatalogItem[]>([]);
  const [reportCategory, setReportCategory] = useState("ALL");
  const [activeReport, setActiveReport] = useState<SilaCulinaryMrpReportResult | null>(null);

  const loadAll = useCallback(async (selectRunId?: string) => {
    setLoading(true);
    setError(null);
    try {
      const [dash, runList, evList, catalog] = await Promise.all([
        getMrpDashboard(),
        listMrpRuns({ includeSimulations: true }),
        listCulinaryEvents(),
        getMrpReportCatalog(),
      ]);
      setDashboard(dash);
      setRuns(runList);
      setEvents(evList);
      setReportCatalog(catalog);
      if (evList.length > 0 && selectedEventIds.length === 0) {
        setSelectedEventIds([evList[0].id]);
      }
      const targetId = selectRunId ?? activeRun?.id ?? runList[0]?.id;
      if (targetId) {
        const detail = await getMrpRun(targetId);
        setActiveRun(detail);
      }
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load Event MRP Studio");
    } finally {
      setLoading(false);
    }
  }, [activeRun?.id, selectedEventIds.length]);

  useEffect(() => {
    void loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (!activeRun || groupBy === "MATERIAL") {
      setGroupedView(null);
      return;
    }
    let cancelled = false;
    void getMrpGroupedView(activeRun.id, groupBy)
      .then((res) => {
        if (!cancelled) setGroupedView(res);
      })
      .catch(() => {});
    return () => {
      cancelled = true;
    };
  }, [activeRun, groupBy]);

  // Debounced deterministic MRP simulation
  useEffect(() => {
    if (!activeRun || tab !== "simulator") return;
    if (simDebounceRef.current) window.clearTimeout(simDebounceRef.current);
    simDebounceRef.current = window.setTimeout(() => {
      void simulateMrpRun(activeRun.id, {
        guestScaleFactor: guestScale / 100,
        additionalAllowancePercent: allowancePct,
        bufferPercent: bufferPct,
        yieldOverridePercent: yieldPct,
        priceIncreasePercent: pricePct,
        preparedStockMode: prepMode,
        lineOverrides: [],
      })
        .then(setSimResult)
        .catch(() => {});
    }, 180);
    return () => {
      if (simDebounceRef.current) window.clearTimeout(simDebounceRef.current);
    };
  }, [activeRun, tab, guestScale, allowancePct, bufferPct, yieldPct, pricePct, prepMode]);

  const selectRun = async (runId: string) => {
    setBusy(true);
    setError(null);
    try {
      const detail = await getMrpRun(runId);
      setActiveRun(detail);
      setSimResult(null);
      setImpact(null);
      setHandover(null);
      setAiAnswer(null);
      setOptResult(null);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to load MRP run");
    } finally {
      setBusy(false);
    }
  };

  const handleCreateRun = async () => {
    if (selectedEventIds.length === 0) {
      setError("Select at least one event.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const created = await createMrpRun({
        name: newName.trim() || undefined,
        runScope: selectedEventIds.length > 1 ? "MULTI_EVENT" : newScope,
        eventIds: selectedEventIds,
        isSimulation: newIsSim,
        allowUnapprovedEvents: true,
      });
      setShowNewRun(false);
      setNewName("");
      setNotice(`MRP run ${created.mrpCode} calculated (${created.totalUniqueMaterials} materials).`);
      await loadAll(created.id);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Failed to calculate MRP run");
    } finally {
      setBusy(false);
    }
  };

  const handleAction = async (fn: () => Promise<void>, successMsg: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      await fn();
      setNotice(successMsg);
    } catch (e: unknown) {
      setError(e instanceof Error ? e.message : "Operation failed");
    } finally {
      setBusy(false);
    }
  };

  const filteredLines = useMemo(() => {
    if (!activeRun) return [];
    const q = bomSearch.trim().toLowerCase();
    return activeRun.lines.filter((l) => {
      if (bomStatusFilter !== "ALL" && l.validationStatus !== bomStatusFilter) return false;
      if (!q) return true;
      return (
        l.materialDescription.toLowerCase().includes(q) ||
        (l.materialCode ?? "").toLowerCase().includes(q) ||
        l.materialCategory.toLowerCase().includes(q) ||
        l.contributingRecipes.some((r) => r.toLowerCase().includes(q))
      );
    });
  }, [activeRun, bomSearch, bomStatusFilter]);

  const setAllTreeExpanded = (expanded: boolean) => {
    if (!activeRun) return;
    const map: Record<string, boolean> = {};
    const walk = (nodes: SilaCulinaryMrpRunDetail["explosionTree"]) => {
      for (const n of nodes) {
        map[n.nodeId] = expanded;
        if (n.children?.length) walk(n.children);
      }
    };
    walk(activeRun.explosionTree);
    setExpandedNodes(map);
  };

  const downloadCsvReport = (r: SilaCulinaryMrpReportResult) => {
    const bytes = Uint8Array.from(atob(r.base64Content), (c) => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = r.fileName;
    a.click();
    URL.revokeObjectURL(url);
  };

  const renderTreeNode = (node: SilaCulinaryMrpRunDetail["explosionTree"][number]): React.ReactNode => {
    const isExp = expandedNodes[node.nodeId] ?? node.dependencyLevel < 2;
    const matches =
      treeSearch.trim().length > 0 &&
      (node.description.toLowerCase().includes(treeSearch.toLowerCase()) ||
        node.referenceCode.toLowerCase().includes(treeSearch.toLowerCase()));
    return (
      <div key={node.nodeId} style={{ marginLeft: node.dependencyLevel * 18, borderLeft: "2px solid #e2e8f0", paddingLeft: 10, marginTop: 6 }}>
        <div
          className="cs2-row-between"
          style={{
            background: matches ? "#fef9c3" : node.validationStatus.includes("CIRCULAR") ? "#fef2f2" : "#f8fafc",
            border: "1px solid #e2e8f0",
            borderRadius: 8,
            padding: "6px 10px",
          }}
        >
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            {node.children?.length > 0 && (
              <button
                type="button"
                className="sila-btn sila-btn--xs sila-btn--secondary"
                onClick={() => setExpandedNodes((p) => ({ ...p, [node.nodeId]: !isExp }))}
              >
                {isExp ? "−" : "+"}
              </button>
            )}
            <span className="cs2-pill">{node.nodeType}</span>
            <strong>{node.description}</strong>
            <code style={{ fontSize: 11 }}>{node.referenceCode}</code>
            {node.recipeVersion ? <span style={{ fontSize: 11, color: "#475569" }}>v{node.recipeVersion}</span> : null}
          </div>
          <div style={{ display: "flex", gap: 10, fontSize: 12, alignItems: "center" }}>
            <span>
              <strong>{node.requiredQty.toLocaleString(undefined, { maximumFractionDigits: 3 })}</strong> {node.uom}
            </span>
            {node.yieldPercent ? <span style={{ color: "#64748b" }}>Yield {node.yieldPercent}%</span> : null}
            <span style={{ color: "#64748b" }}>×{node.scalingFactor}</span>
            <span className={`cs2-pill ${node.validationStatus.includes("MISSING") || node.validationStatus.includes("CIRCULAR") ? "cs2-pill--danger" : ""}`}>
              {node.validationStatus}
            </span>
          </div>
        </div>
        {isExp && node.children?.map((c) => renderTreeNode(c))}
      </div>
    );
  };

  return (
    <div className="cs2-shell" data-testid="culinary-event-mrp-studio">
      {/* Header */}
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Event MRP Studio — Material Requirement Planning Engine</h2>
          <p className="cs2-subtitle">
            Deterministic multi-level recipe explosion, yield &amp; buffer planning, time-phased demand, preliminary stock comparison, and procurement handover.
            <strong style={{ marginLeft: 6, color: "#0f766e" }}>Zero stock reservations, POs, ITOs, or SAP postings.</strong>
          </p>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => setShowNewRun((v) => !v)} disabled={busy}>
            + New MRP Run
          </button>
          {activeRun && (
            <>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                disabled={busy || activeRun.isApprovedBaseline}
                onClick={() =>
                  void handleAction(async () => {
                    const updated = await recalculateMrpRun(activeRun.id);
                    setActiveRun(updated);
                  }, `Recalculated ${activeRun.mrpCode}.`)
                }
              >
                Recalculate
              </button>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                disabled={busy || !activeRun.readiness?.canSubmitForApproval || activeRun.status !== "CALCULATED"}
                onClick={() =>
                  void handleAction(async () => {
                    const updated = await submitMrpForApproval(activeRun.id);
                    setActiveRun(updated);
                  }, `Submitted ${activeRun.mrpCode} for approval.`)
                }
              >
                Submit Approval
              </button>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                disabled={busy || activeRun.status !== "APPROVED"}
                onClick={() =>
                  void handleAction(async () => {
                    const updated = await releaseMrpForPlanning(activeRun.id);
                    setActiveRun(updated);
                  }, `Released ${activeRun.mrpCode} for procurement planning (0 POs/ITOs/reservations).`)
                }
              >
                Release to Procurement
              </button>
            </>
          )}
        </div>
      </div>

      {error && <div className="cs2-alert cs2-alert--danger" style={{ marginTop: 10 }}>{error}</div>}
      {notice && <div className="cs2-alert cs2-alert--info" style={{ marginTop: 10 }}>{notice}</div>}

      {/* New MRP Run Drawer */}
      {showNewRun && (
        <div className="cs2-card" style={{ marginTop: 12 }}>
          <h3 className="cs2-card-title">Create New Event MRP Run</h3>
          <div className="cs2-form-grid" style={{ marginTop: 8 }}>
            <label className="cs2-field">
              <span>Run Name</span>
              <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="e.g., Weekend Gala Multi-Event MRP" />
            </label>
            <label className="cs2-field">
              <span>Scope</span>
              <select value={newScope} onChange={(e) => setNewScope(e.target.value)}>
                <option value="SINGLE_EVENT">Single Event</option>
                <option value="MULTI_EVENT">Multi-Event Consolidation</option>
                <option value="DATE_RANGE">Date Range</option>
                <option value="PROPERTY">Property Overview</option>
              </select>
            </label>
            <label className="cs2-field" style={{ flexDirection: "row", alignItems: "center", gap: 8 }}>
              <input type="checkbox" checked={newIsSim} onChange={(e) => setNewIsSim(e.target.checked)} />
              <span>Run as Draft Simulation</span>
            </label>
          </div>
          <div style={{ marginTop: 10 }}>
            <strong style={{ fontSize: 12 }}>Select Approved / Draft Events ({selectedEventIds.length} selected):</strong>
            <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginTop: 6 }}>
              {events.map((ev) => {
                const checked = selectedEventIds.includes(ev.id);
                return (
                  <label key={ev.id} className="cs2-pill" style={{ cursor: "pointer", background: checked ? "#dbeafe" : undefined }}>
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() =>
                        setSelectedEventIds((p) => (p.includes(ev.id) ? p.filter((x) => x !== ev.id) : [...p, ev.id]))
                      }
                      style={{ marginRight: 6 }}
                    />
                    {ev.eventCode} — {ev.name} ({ev.planningGuests} guests, {ev.status})
                  </label>
                );
              })}
              {events.length === 0 && <span style={{ fontSize: 12, color: "#64748b" }}>No culinary events found. Create an event in Event Planner or use the §32 Gala Demo tab.</span>}
            </div>
          </div>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => void handleCreateRun()} disabled={busy}>
              Run MRP Calculation
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => setShowNewRun(false)}>
              Cancel
            </button>
          </div>
        </div>
      )}

      {/* Active MRP Run Selector & Context Banner */}
      <div className="cs2-card" style={{ marginTop: 12, padding: "10px 14px" }}>
        <div className="cs2-row-between" style={{ flexWrap: "wrap", gap: 10 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
            <strong style={{ fontSize: 12 }}>Active MRP Run:</strong>
            <select
              value={activeRun?.id ?? ""}
              onChange={(e) => e.target.value && void selectRun(e.target.value)}
              style={{ minWidth: 260 }}
            >
              {runs.length === 0 && <option value="">No MRP runs calculated yet</option>}
              {runs.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.mrpCode} v{r.versionNumber} — {r.name} [{r.status}]
                </option>
              ))}
            </select>
            {activeRun && (
              <>
                <span className="cs2-pill">{activeRun.status}</span>
                <span className="cs2-pill">v{activeRun.versionNumber}</span>
                {activeRun.isSimulation && <span className="cs2-pill cs2-pill--warn">SIMULATION</span>}
                {activeRun.isApprovedBaseline && <span className="cs2-pill">APPROVED BASELINE</span>}
                <span style={{ fontSize: 12, color: "#475569" }}>
                  Property: <strong>{activeRun.propertyName ?? "Default"}</strong> · Events: <strong>{activeRun.eventReferences.join(", ") || "—"}</strong>
                </span>
              </>
            )}
          </div>
          {activeRun?.nextRequiredByUtc && (
            <span style={{ fontSize: 12, color: "#b45309", fontWeight: 600 }}>
              Earliest Deadline: {new Date(activeRun.nextRequiredByUtc).toLocaleString()}
            </span>
          )}
        </div>
      </div>

      {/* KPI Strip */}
      <div className="cs2-kpi-grid" style={{ marginTop: 12 }}>
        <div className="cs2-kpi-card">
          <span>Active MRP Runs</span>
          <strong>{dashboard?.activeMrpRuns ?? runs.length}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Total Recipes</span>
          <strong>{activeRun?.totalRecipes ?? 0}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Unique Materials</span>
          <strong>{activeRun?.totalUniqueMaterials ?? dashboard?.uniqueRequiredMaterials ?? 0}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Sufficient Stock</span>
          <strong style={{ color: "#0f766e" }}>{activeRun?.sufficientStockCount ?? 0}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Potential Shortages</span>
          <strong style={{ color: "#b91c1c" }}>{activeRun?.shortageCount ?? dashboard?.materialsWithShortages ?? 0}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Unknown Availability</span>
          <strong>{activeRun?.unknownStockCount ?? 0}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Missing Mappings</span>
          <strong style={{ color: "#b45309" }}>{activeRun?.missingMappingCount ?? dashboard?.materialsWithUnresolvedMappings ?? 0}</strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Est. Requirement Value</span>
          <strong>
            {activeRun?.costVisible && activeRun.estimatedRequirementValue != null
              ? `${activeRun.currency} ${activeRun.estimatedRequirementValue.toLocaleString(undefined, { maximumFractionDigits: 0 })}`
              : activeRun?.costVisible === false
                ? "Restricted"
                : "Unknown"}
          </strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Indicative Shortage Value</span>
          <strong style={{ color: "#b91c1c" }}>
            {activeRun?.costVisible && activeRun.indicativeShortageValue != null
              ? `${activeRun.currency} ${activeRun.indicativeShortageValue.toLocaleString(undefined, { maximumFractionDigits: 0 })}`
              : activeRun?.costVisible === false
                ? "Restricted"
                : "Unknown"}
          </strong>
        </div>
        <div className="cs2-kpi-card">
          <span>Due ≤24h / ≤48h</span>
          <strong>{dashboard?.materialsRequiredWithin24Hours ?? 0} / {dashboard?.materialsRequiredWithin48Hours ?? 0}</strong>
        </div>
      </div>

      {/* Studio Sub-Navigation */}
      <div className="cs2-tabs" style={{ marginTop: 14, flexWrap: "wrap" }}>
        {[
          { id: "overview", label: "Dashboard & Overview" },
          { id: "bom", label: "Consolidated BOM (10 Views)" },
          { id: "tree", label: "Recipe Explosion Tree" },
          { id: "timeline", label: "Time-Phased & Stock" },
          { id: "simulator", label: "MRP Simulator & Scenarios" },
          { id: "ai", label: "AI Analysis & Optimization" },
          { id: "governance", label: `Exceptions, Approval & Procurement Handover (${activeRun?.blockingExceptionCount ?? 0})` },
          { id: "reports", label: `Report Studio (${reportCatalog.length || 36})` },
          { id: "demo", label: "§32 Grand Gala Fixture" },
        ].map((t) => (
          <button
            key={t.id}
            type="button"
            className={`cs2-tab ${tab === t.id ? "cs2-tab--active" : ""}`}
            onClick={() => setTab(t.id as MrpTab)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {loading && <p style={{ padding: 16 }}>Loading MRP Studio...</p>}

      {/* TAB 1: DASHBOARD & OVERVIEW */}
      {tab === "overview" && (
        <div className="cs2-grid-2" style={{ marginTop: 12 }}>
          <div className="cs2-card">
            <h3 className="cs2-card-title">Requirements by Material Category</h3>
            <div style={{ marginTop: 10, display: "flex", flexDirection: "column", gap: 8 }}>
              {Object.entries(dashboard?.requirementsByCategory ?? {}).map(([cat, qty]) => (
                <div key={cat} className="cs2-row-between">
                  <span>{cat}</span>
                  <strong>{qty.toLocaleString(undefined, { maximumFractionDigits: 2 })}</strong>
                </div>
              ))}
              {Object.keys(dashboard?.requirementsByCategory ?? {}).length === 0 && (
                <span style={{ color: "#64748b", fontSize: 13 }}>Run an MRP calculation to inspect live category demand.</span>
              )}
            </div>
          </div>
          <div className="cs2-card">
            <h3 className="cs2-card-title">Top Potential Shortages (Preliminary)</h3>
            <div style={{ marginTop: 10, display: "flex", flexDirection: "column", gap: 8 }}>
              {(dashboard?.shortageByMaterial ?? []).map((pt) => (
                <div key={pt.label} className="cs2-row-between">
                  <span>{pt.label}</span>
                  <strong style={{ color: "#b91c1c" }}>
                    {pt.qty.toLocaleString(undefined, { maximumFractionDigits: 2 })}
                    {pt.value != null ? ` (${dashboard?.currency ?? "AED"} ${pt.value.toLocaleString()})` : ""}
                  </strong>
                </div>
              ))}
              {(dashboard?.shortageByMaterial?.length ?? 0) === 0 && (
                <span style={{ color: "#64748b", fontSize: 13 }}>No preliminary shortages detected across active runs.</span>
              )}
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: CONSOLIDATED BOM & 10 VIEWS */}
      {tab === "bom" && activeRun && (
        <div className="cs2-card" style={{ marginTop: 12 }}>
          <div className="cs2-row-between" style={{ flexWrap: "wrap", gap: 10 }}>
            <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
              {GROUP_VIEWS.map((gv) => (
                <button
                  key={gv.id}
                  type="button"
                  className={`sila-btn sila-btn--xs ${groupBy === gv.id ? "sila-btn--primary" : "sila-btn--secondary"}`}
                  onClick={() => setGroupBy(gv.id)}
                >
                  {gv.label}
                </button>
              ))}
            </div>
            <div style={{ display: "flex", gap: 8 }}>
              <input value={bomSearch} onChange={(e) => setBomSearch(e.target.value)} placeholder="Search material or recipe..." />
              <select value={bomStatusFilter} onChange={(e) => setBomStatusFilter(e.target.value)}>
                <option value="ALL">All Statuses</option>
                <option value="CURRENT_STOCK_SUFFICIENT">Sufficient</option>
                <option value="POTENTIAL_SHORTAGE">Potential Shortage</option>
                <option value="NO_STOCK">No Stock</option>
                <option value="AVAILABILITY_UNKNOWN">Unknown Availability</option>
                <option value="MATERIAL_NOT_MAPPED">Material Not Mapped</option>
                <option value="UOM_CONVERSION_MISSING">Missing UOM Conversion</option>
              </select>
            </div>
          </div>

          {groupBy !== "MATERIAL" && groupedView ? (
            <div style={{ marginTop: 12, display: "flex", flexDirection: "column", gap: 10 }}>
              {groupedView.groups.map((g) => (
                <div key={g.groupKey} style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: 10 }}>
                  <div className="cs2-row-between">
                    <strong>{g.groupLabel}</strong>
                    <div style={{ display: "flex", gap: 12, fontSize: 12 }}>
                      <span>{g.lineCount} lines</span>
                      {g.totalQtyIfSameUom != null && <strong>{g.totalQtyIfSameUom} {g.commonUom}</strong>}
                      {g.totalRequirementValue != null && <span>{activeRun.currency} {g.totalRequirementValue.toLocaleString()}</span>}
                      <span style={{ color: g.shortageCount > 0 ? "#b91c1c" : "#0f766e" }}>{g.shortageCount} shortages</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ overflowX: "auto", marginTop: 12 }}>
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>Material</th>
                    <th>Category</th>
                    <th>Recipes</th>
                    <th>Edible</th>
                    <th>Raw / Gross</th>
                    <th>Eligible Stock</th>
                    <th>Incoming PO</th>
                    <th>Shortage</th>
                    <th>Required By</th>
                    <th>Req. Value</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredLines.map((l) => (
                    <React.Fragment key={l.id}>
                      <tr onClick={() => setExpandedLineId((p) => (p === l.id ? null : l.id))} style={{ cursor: "pointer" }}>
                        <td>
                          <strong>{l.materialDescription}</strong>
                          <div style={{ fontSize: 11, color: "#64748b" }}>{l.materialCode ?? "UNMAPPED"}</div>
                        </td>
                        <td>{l.materialCategory}</td>
                        <td style={{ fontSize: 11 }}>{l.contributingRecipes.join(", ")}</td>
                        <td>{l.edibleQty} {l.baseUom}</td>
                        <td><strong>{l.grossQty} {l.baseUom}</strong></td>
                        <td>{l.eligibleStockQty != null ? `${l.eligibleStockQty} ${l.baseUom}` : "Unknown"}</td>
                        <td>{l.incomingPoQty > 0 ? `${l.incomingPoQty} ${l.baseUom}` : "—"}</td>
                        <td style={{ color: (l.indicativeCurrentShortageQty ?? 0) > 0 ? "#b91c1c" : undefined, fontWeight: 600 }}>
                          {l.indicativeCurrentShortageQty != null ? `${l.indicativeCurrentShortageQty} ${l.baseUom}` : "Unknown"}
                        </td>
                        <td style={{ fontSize: 11 }}>{new Date(l.requiredDateUtc).toLocaleDateString()}</td>
                        <td>{l.costVisible ? (l.requirementValue != null ? `${l.currency} ${l.requirementValue.toLocaleString()}` : "Unknown") : "Restricted"}</td>
                        <td><span className="cs2-pill">{l.validationStatus}</span></td>
                      </tr>
                      {expandedLineId === l.id && (
                        <tr>
                          <td colSpan={11} style={{ background: "#f8fafc", fontSize: 12 }}>
                            <div><strong>Lineage Paths:</strong> {l.subRecipePaths.join(" | ")}</div>
                            <div><strong>Events / Sessions:</strong> {l.contributingEvents.join(", ")} · {l.contributingSessions.join(", ")}</div>
                            {l.exclusionReasons.length > 0 && <div style={{ color: "#b45309" }}><strong>Exclusions:</strong> {l.exclusionReasons.join("; ")}</div>}
                            {!l.isResolvedMaterial && (
                              <button
                                type="button"
                                className="sila-btn sila-btn--xs sila-btn--primary"
                                style={{ marginTop: 6 }}
                                onClick={() => onOpenWorkspace("material-requests")}
                              >
                                Open Material Creation Request Workflow
                              </button>
                            )}
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* TAB 3: INTERACTIVE RECIPE EXPLOSION TREE */}
      {tab === "tree" && activeRun && (
        <div className="cs2-card" style={{ marginTop: 12 }}>
          <div className="cs2-row-between">
            <h3 className="cs2-card-title">Multi-Level Recipe Explosion Tree</h3>
            <div style={{ display: "flex", gap: 8 }}>
              <input value={treeSearch} onChange={(e) => setTreeSearch(e.target.value)} placeholder="Highlight material or recipe..." />
              <button type="button" className="sila-btn sila-btn--xs sila-btn--secondary" onClick={() => setAllTreeExpanded(true)}>Expand All</button>
              <button type="button" className="sila-btn sila-btn--xs sila-btn--secondary" onClick={() => setAllTreeExpanded(false)}>Collapse All</button>
            </div>
          </div>
          <div style={{ marginTop: 12 }}>
            {activeRun.explosionTree.map((node) => renderTreeNode(node))}
          </div>
        </div>
      )}

      {/* TAB 4: TIME-PHASED DEMAND & STOCK COMPARISON */}
      {tab === "timeline" && activeRun && (
        <div className="cs2-grid-2" style={{ marginTop: 12 }}>
          <div className="cs2-card">
            <h3 className="cs2-card-title">Time-Phased Daily &amp; Kitchen Demand Buckets</h3>
            <table className="cs2-table" style={{ marginTop: 10 }}>
              <thead>
                <tr><th>Required Date</th><th>Kitchen</th><th>Materials</th><th>Shortages</th><th>Est. Value</th></tr>
              </thead>
              <tbody>
                {activeRun.timePhasedBuckets.map((tb) => (
                  <tr key={`${tb.bucketDate}-${tb.kitchenName}`}>
                    <td><strong>{tb.bucketDate}</strong></td>
                    <td>{tb.kitchenName}</td>
                    <td>{tb.materialCount}</td>
                    <td style={{ color: tb.shortageMaterialCount > 0 ? "#b91c1c" : "#0f766e" }}>{tb.shortageMaterialCount}</td>
                    <td>{activeRun.costVisible && tb.totalRequirementValue != null ? `${activeRun.currency} ${tb.totalRequirementValue.toLocaleString()}` : "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="cs2-card">
            <h3 className="cs2-card-title">Sub-Recipe Batch Summary</h3>
            <table className="cs2-table" style={{ marginTop: 10 }}>
              <thead>
                <tr><th>Sub-Recipe</th><th>Required</th><th>Batch Yield</th><th>Batches</th><th>Planned Out</th><th>Excess</th></tr>
              </thead>
              <tbody>
                {activeRun.batchSummaries.map((b) => (
                  <tr key={`${b.recipeId}-${b.recipeVersion}`}>
                    <td>{b.recipeName} (v{b.recipeVersion})</td>
                    <td>{b.requiredQty} {b.batchYieldUom}</td>
                    <td>{b.batchYield} {b.batchYieldUom}</td>
                    <td><strong>{b.requiredBatches}</strong></td>
                    <td>{b.plannedOutputQty} {b.batchYieldUom}</td>
                    <td style={{ color: b.excessOutputQty > 0 ? "#b45309" : undefined }}>{b.excessOutputQty} {b.batchYieldUom}</td>
                  </tr>
                ))}
                {activeRun.batchSummaries.length === 0 && (
                  <tr><td colSpan={6} style={{ color: "#64748b" }}>No nested sub-recipe batches in this MRP run.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 5: INTERACTIVE MRP SIMULATOR & SCENARIO COMPARISON */}
      {tab === "simulator" && activeRun && (
        <div className="cs2-card" style={{ marginTop: 12 }}>
          <div className="cs2-row-between">
            <h3 className="cs2-card-title">Deterministic Interactive MRP Simulator</h3>
            <button
              type="button"
              className="sila-btn sila-btn--xs sila-btn--secondary"
              onClick={() => {
                setGuestScale(100);
                setAllowancePct(0);
                setBufferPct(0);
                setYieldPct(0);
                setPricePct(0);
                setPrepMode("ADJUSTED_SCENARIO");
              }}
            >
              Reset to Baseline
            </button>
          </div>
          <div className="cs2-form-grid" style={{ marginTop: 12 }}>
            <label className="cs2-field">
              <span>Guest Scale ({guestScale}%)</span>
              <input type="range" min={50} max={200} value={guestScale} onChange={(e) => setGuestScale(Number(e.target.value))} />
            </label>
            <label className="cs2-field">
              <span>Additional Allowance ({allowancePct}%)</span>
              <input type="range" min={0} max={25} value={allowancePct} onChange={(e) => setAllowancePct(Number(e.target.value))} />
            </label>
            <label className="cs2-field">
              <span>Production Buffer ({bufferPct}%)</span>
              <input type="range" min={0} max={25} value={bufferPct} onChange={(e) => setBufferPct(Number(e.target.value))} />
            </label>
            <label className="cs2-field">
              <span>Yield Override ({yieldPct === 0 ? "Recipe Default" : `${yieldPct}%`})</span>
              <input type="range" min={0} max={100} value={yieldPct} onChange={(e) => setYieldPct(Number(e.target.value))} />
            </label>
            <label className="cs2-field">
              <span>Price Increase Scenario ({pricePct}%)</span>
              <input type="range" min={-20} max={40} value={pricePct} onChange={(e) => setPricePct(Number(e.target.value))} />
            </label>
            <label className="cs2-field">
              <span>Prepared Stock Scenario</span>
              <select value={prepMode} onChange={(e) => setPrepMode(e.target.value as "ADJUSTED_SCENARIO" | "GROSS_ONLY")}>
                <option value="ADJUSTED_SCENARIO">Subtract Eligible Prepared Stock</option>
                <option value="GROSS_ONLY">Gross Requirement Only</option>
              </select>
            </label>
          </div>

          {simResult && (
            <div style={{ marginTop: 12, padding: 12, background: "#f8fafc", borderRadius: 8, border: "1px solid #e2e8f0" }}>
              <div className="cs2-row-between">
                <span>Simulated Planned Guests: <strong>{simResult.plannedGuests}</strong></span>
                <span>Shortages: <strong style={{ color: "#b91c1c" }}>{simResult.shortageCount}</strong></span>
                <span>
                  Simulated Req. Value:{" "}
                  <strong>{simResult.costVisible && simResult.totalRequirementValue != null ? `${simResult.currency} ${simResult.totalRequirementValue.toLocaleString()}` : "Restricted"}</strong>
                </span>
                <span>
                  Simulated Shortage Value:{" "}
                  <strong style={{ color: "#b91c1c" }}>{simResult.costVisible && simResult.totalShortageValue != null ? `${simResult.currency} ${simResult.totalShortageValue.toLocaleString()}` : "Restricted"}</strong>
                </span>
              </div>
              <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
                <input value={scenarioName} onChange={(e) => setScenarioName(e.target.value)} placeholder="Scenario label (e.g., +20% Guests & 10% Buffer)" style={{ flex: 1 }} />
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={() =>
                    void handleAction(async () => {
                      await saveMrpScenario(activeRun.id, scenarioName || `Sim ${guestScale}%`, "CUSTOM", {
                        guestScaleFactor: guestScale / 100,
                        additionalAllowancePercent: allowancePct,
                        bufferPercent: bufferPct,
                        yieldOverridePercent: yieldPct,
                        priceIncreasePercent: pricePct,
                        preparedStockMode: prepMode,
                        lineOverrides: [],
                      });
                      setScenarioName("");
                      await selectRun(activeRun.id);
                    }, "Saved simulation scenario.")
                  }
                >
                  Save Scenario
                </button>
              </div>
            </div>
          )}

          <h4 style={{ marginTop: 16 }}>Saved Scenario Comparison</h4>
          <table className="cs2-table" style={{ marginTop: 6 }}>
            <thead>
              <tr><th>Code</th><th>Name</th><th>Type</th><th>Planned Guests</th><th>Shortages</th><th>Req. Value</th><th>Shortage Value</th></tr>
            </thead>
            <tbody>
              {activeRun.scenarios.map((s) => (
                <tr key={s.id}>
                  <td><code>{s.scenarioCode}</code></td>
                  <td>{s.name}</td>
                  <td>{s.scenarioType}</td>
                  <td>{s.plannedGuests}</td>
                  <td>{s.shortageCount}</td>
                  <td>{activeRun.costVisible && s.totalRequirementValue != null ? `${activeRun.currency} ${s.totalRequirementValue.toLocaleString()}` : "—"}</td>
                  <td>{activeRun.costVisible && s.totalShortageValue != null ? `${activeRun.currency} ${s.totalShortageValue.toLocaleString()}` : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* TAB 6: AI ANALYSIS, SUBSTITUTIONS & BATCH OPTIMIZATION */}
      {tab === "ai" && activeRun && (
        <div className="cs2-grid-2" style={{ marginTop: 12 }}>
          <div className="cs2-card">
            <h3 className="cs2-card-title">AI Material Requirement Analysis Agent</h3>
            <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginTop: 8 }}>
              {AI_PRESETS.map((p) => (
                <button key={p} type="button" className="sila-btn sila-btn--xs sila-btn--secondary" onClick={() => setAiQuestion(p)}>
                  {p}
                </button>
              ))}
            </div>
            <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
              <input value={aiQuestion} onChange={(e) => setAiQuestion(e.target.value)} style={{ flex: 1 }} />
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--primary"
                disabled={busy}
                onClick={() =>
                  void handleAction(async () => {
                    setAiAnswer(await analyzeMrpWithAi(activeRun.id, aiQuestion));
                  }, "AI requirement analysis completed.")
                }
              >
                Ask AI
              </button>
            </div>
            {aiAnswer && (
              <div style={{ marginTop: 12, padding: 12, background: "#f8fafc", borderRadius: 8, border: "1px solid #cbd5e1" }}>
                <div style={{ fontSize: 11, color: "#475569" }}>Provider: <strong>{aiAnswer.providerUsed}</strong> ({aiAnswer.modelUsed})</div>
                <p style={{ marginTop: 6 }}>{aiAnswer.answer}</p>
                {aiAnswer.recommendations.length > 0 && (
                  <ul style={{ marginTop: 6, paddingLeft: 18, fontSize: 12 }}>
                    {aiAnswer.recommendations.map((r) => <li key={r}>{r}</li>)}
                  </ul>
                )}
              </div>
            )}
          </div>

          <div className="cs2-card">
            <div className="cs2-row-between">
              <h3 className="cs2-card-title">Batch Optimization &amp; Substitutions</h3>
              <div style={{ display: "flex", gap: 6 }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--xs sila-btn--secondary"
                  onClick={() => void handleAction(async () => setOptResult(await optimizeMrpBatches(activeRun.id)), "Batch optimization evaluated.")}
                >
                  Run HiGHS Optimizer
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--xs sila-btn--secondary"
                  onClick={() => void handleAction(async () => setSubstitutions(await suggestMrpSubstitutions(activeRun.id)), "Loaded eligible substitutions.")}
                >
                  Suggest Substitutions
                </button>
              </div>
            </div>
            {optResult && (
              <div style={{ marginTop: 10, padding: 10, background: optResult.feasible ? "#ecfdf5" : "#fef2f2", borderRadius: 8, fontSize: 12 }}>
                <strong>Solver: {optResult.solver}</strong> · Feasible: <strong>{optResult.feasible ? "YES" : "NO"}</strong> · Excess Output: {optResult.totalExcessOutput}
                {optResult.infeasibilityReason && <div style={{ color: "#b91c1c" }}>{optResult.infeasibilityReason}</div>}
              </div>
            )}
            {substitutions.length > 0 && (
              <table className="cs2-table" style={{ marginTop: 10 }}>
                <thead><tr><th>Short Material</th><th>Proposed Substitute</th><th>Stock</th><th>Status</th></tr></thead>
                <tbody>
                  {substitutions.map((sub) => (
                    <tr key={`${sub.originalMaterialCode}-${sub.proposedMaterialCode}`}>
                      <td>{sub.originalMaterialName}</td>
                      <td>{sub.proposedMaterialName} ({sub.proposedMaterialCode})</td>
                      <td>{sub.proposedEligibleStock ?? "—"}</td>
                      <td><span className="cs2-pill">{sub.chefReviewStatus}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      )}

      {/* TAB 7: EXCEPTIONS, READINESS, GOVERNANCE & PHASE 2C HANDOVER */}
      {tab === "governance" && activeRun && (
        <div style={{ marginTop: 12, display: "flex", flexDirection: "column", gap: 12 }}>
          <div className="cs2-card">
            <h3 className="cs2-card-title">4-Dimension MRP Readiness</h3>
            <div className="cs2-kpi-grid" style={{ marginTop: 8 }}>
              {activeRun.readiness?.checks?.map((c) => (
                <div key={c.dimension} className="cs2-kpi-card">
                  <span>{c.label}</span>
                  <strong style={{ color: c.status === "READY" ? "#0f766e" : c.status === "BLOCKED" ? "#b91c1c" : "#b45309" }}>{c.status}</strong>
                  <small style={{ fontSize: 11, color: "#64748b" }}>{c.detail}</small>
                </div>
              ))}
            </div>
          </div>

          <div className="cs2-card">
            <h3 className="cs2-card-title">MRP Exceptions ({activeRun.exceptions.length})</h3>
            <table className="cs2-table" style={{ marginTop: 8 }}>
              <thead>
                <tr><th>Code</th><th>Type</th><th>Severity</th><th>Impact</th><th>Status</th><th>Actions</th></tr>
              </thead>
              <tbody>
                {activeRun.exceptions.map((ex) => (
                  <tr key={ex.id}>
                    <td><code>{ex.exceptionCode}</code></td>
                    <td>{ex.exceptionType}</td>
                    <td><span className={`cs2-pill ${ex.severity === "BLOCKING" ? "cs2-pill--danger" : ""}`}>{ex.severity}</span></td>
                    <td style={{ fontSize: 12 }}>{ex.calculationImpact}</td>
                    <td><span className="cs2-pill">{ex.resolutionStatus}</span></td>
                    <td style={{ display: "flex", gap: 6 }}>
                      {(ex.exceptionType.includes("MATERIAL") || ex.exceptionType.includes("UOM")) && ex.resolutionStatus === "OPEN" && (
                        <button
                          type="button"
                          className="sila-btn sila-btn--xs sila-btn--primary"
                          onClick={() =>
                            void handleAction(async () => {
                              await createMaterialRequestFromMrpException(activeRun.id, ex.id);
                              await selectRun(activeRun.id);
                            }, `Prepared Phase 1C Material Request for ${ex.exceptionCode}.`)
                          }
                        >
                          Create Material Request
                        </button>
                      )}
                      {ex.resolutionStatus === "OPEN" && (
                        <button
                          type="button"
                          className="sila-btn sila-btn--xs sila-btn--secondary"
                          onClick={() =>
                            void handleAction(async () => {
                              await resolveMrpException(activeRun.id, ex.id, "RESOLVED", "Resolved by planner");
                              await selectRun(activeRun.id);
                            }, `Resolved ${ex.exceptionCode}.`)
                          }
                        >
                          Resolve
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="cs2-card">
            <div className="cs2-row-between">
              <h3 className="cs2-card-title">Approval, Impact Analysis &amp; Procurement Handover</h3>
              <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--xs sila-btn--secondary"
                  onClick={() => void handleAction(async () => setImpact(await analyzeMrpEventImpact(activeRun.id)), "Event change impact calculated.")}
                >
                  Check Event Impact
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--xs sila-btn--secondary"
                  onClick={() =>
                    void handleAction(async () => {
                      const rev = await createMrpRevision(activeRun.id, "Controlled MRP revision");
                      await loadAll(rev.id);
                    }, "Created new controlled MRP revision.")
                  }
                >
                  Create Revision
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--xs sila-btn--primary"
                  onClick={() => void handleAction(async () => setHandover(await getMrpPhase2CHandover(activeRun.id)), "Procurement handover contract generated.")}
                >
                  Inspect Procurement Handover
                </button>
              </div>
            </div>

            {activeRun.status === "PENDING_APPROVAL" && (
              <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
                <input value={approvalComment} onChange={(e) => setApprovalComment(e.target.value)} placeholder="Approval decision comment..." style={{ flex: 1 }} />
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={() => void handleAction(async () => setActiveRun(await decideMrpApproval(activeRun.id, "APPROVE", approvalComment)), "MRP baseline approved.")}
                >
                  Approve
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={() => void handleAction(async () => setActiveRun(await decideMrpApproval(activeRun.id, "RETURN", approvalComment)), "MRP returned for correction.")}
                >
                  Return
                </button>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={() => void handleAction(async () => setActiveRun(await decideMrpApproval(activeRun.id, "REJECT", approvalComment)), "MRP rejected.")}
                >
                  Reject
                </button>
              </div>
            )}

            {impact && (
              <div style={{ marginTop: 12, padding: 10, background: "#fffbeb", borderRadius: 8, fontSize: 12 }}>
                <strong>Event Change Impact:</strong> Approval Invalidated: <strong>{impact.approvalInvalidated ? "YES" : "NO"}</strong> · New Materials: {impact.newlyRequiredCount} · Removed: {impact.removedCount}
              </div>
            )}

            {handover && (
              <div style={{ marginTop: 12, padding: 10, background: "#ecfdf5", borderRadius: 8, fontSize: 12 }}>
                <strong>Procurement Handover ({handover.mrpCode} v{handover.mrpVersion})</strong> —{" "}
                Zero Reservations: {String(handover.zeroReservationsCreated)} · Zero POs: {String(handover.zeroPosCreated)} · Zero ITOs: {String(handover.zeroItosCreated)} · Zero SAP Postings: {String(handover.zeroSapPostingsExecuted)} · {handover.materials.length} material line(s)
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 8: REPORT STUDIO (36 REPORTS) */}
      {tab === "reports" && activeRun && (
        <div className="cs2-card" style={{ marginTop: 12 }}>
          <div className="cs2-row-between">
            <h3 className="cs2-card-title">MRP Report Studio (36 Reports — Formula-Safe CSV &amp; Print)</h3>
            <select value={reportCategory} onChange={(e) => setReportCategory(e.target.value)}>
              <option value="ALL">All Categories (36)</option>
              <option value="MRP">MRP Reports (1–12)</option>
              <option value="QUANTITY_YIELD">Quantity &amp; Yield (13–20)</option>
              <option value="STOCK_COST">Stock &amp; Cost (21–26)</option>
              <option value="GOVERNANCE">Governance (27–36)</option>
            </select>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))", gap: 10, marginTop: 12 }}>
            {reportCatalog
              .filter((r) => reportCategory === "ALL" || r.category === reportCategory)
              .map((r) => (
                <div key={r.reportCode} style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: 10 }}>
                  <strong style={{ fontSize: 13 }}>{r.title}</strong>
                  <p style={{ fontSize: 11, color: "#64748b", marginTop: 4 }}>{r.description}</p>
                  <button
                    type="button"
                    className="sila-btn sila-btn--xs sila-btn--secondary"
                    style={{ marginTop: 8 }}
                    onClick={() => void handleAction(async () => setActiveReport(await exportMrpReport(activeRun.id, r.reportCode)), `Generated preview for ${r.title}.`)}
                  >
                    Preview &amp; Export
                  </button>
                </div>
              ))}
          </div>

          {activeReport && (
            <div style={{ marginTop: 16, borderTop: "1px solid #e2e8f0", paddingTop: 12 }}>
              <div className="cs2-row-between">
                <strong>{activeReport.title} ({activeReport.rowCount} rows)</strong>
                <div style={{ display: "flex", gap: 8 }}>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => downloadCsvReport(activeReport)}>
                    Download CSV / Excel
                  </button>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => window.print()}>
                    Print / PDF
                  </button>
                </div>
              </div>
              <div style={{ overflowX: "auto", marginTop: 8 }}>
                <table className="cs2-table">
                  <thead>
                    <tr>{activeReport.headers.map((h) => <th key={h}>{h}</th>)}</tr>
                  </thead>
                  <tbody>
                    {activeReport.previewRows.map((row, idx) => (
                      <tr key={idx}>{row.map((cell, ci) => <td key={ci}>{cell}</td>)}</tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </div>
      )}

      {/* TAB 9: ISOLATED §32 GRAND CORPORATE GALA DEMO */}
      {tab === "demo" && (
        <div style={{ marginTop: 12 }}>
          <CulinaryMrpSimulatorWorkspace />
        </div>
      )}
    </div>
  );
};

export default CulinaryEventMrpStudioWorkspace;
