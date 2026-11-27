import React, { useCallback, useEffect, useMemo, useState } from "react";
import { listMrpRuns, type SilaCulinaryMrpRunSummary } from "../../../../api/silaMe/silaCulinaryMrpApi";
import {
  allocateIncomingPo,
  analyzeProcurementWithAi,
  cancelProcurementPlan,
  createProcurementItoDraft,
  createProcurementPlan,
  createProcurementPoDraft,
  createStockReservation,
  decideProcurementPlanApproval,
  exportProcurementReport,
  getPhase3AHandover,
  getProcurementDashboard,
  getProcurementPlan,
  getProcurementReportCatalog,
  handleProcurementChangeImpact,
  linkWeeklyBucketToProcurementLine,
  listProcurementPlans,
  optimizeProcurementPlan,
  recalculateProcurementPlan,
  reconcileProcurementGrn,
  reconcileProcurementItoReversal,
  resolveProcurementException,
  simulateProcurementPlan,
  submitProcurementPlanForApproval,
  transitionStockReservation,
  type SilaCulinaryPhase3AHandover,
  type SilaCulinaryProcurementAiAnalyzeResponse,
  type SilaCulinaryProcurementDashboard,
  type SilaCulinaryProcurementLine,
  type SilaCulinaryProcurementOptimizeResult,
  type SilaCulinaryProcurementPlanDetail,
  type SilaCulinaryProcurementPlanSummary,
  type SilaCulinaryProcurementReportCatalogItem,
  type SilaCulinaryProcurementReportResult,
  type SilaCulinarySimulateProcurementResult,
} from "../../../../api/silaMe/silaCulinaryProcurementApi";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface Props {
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

type ProcTab =
  | "coverage"
  | "reservations"
  | "incoming-pos"
  | "transfers"
  | "purchases"
  | "suppliers"
  | "simulator"
  | "delivery"
  | "exceptions"
  | "approvals"
  | "reports"
  | "history";

const PROC_TABS: Array<{ id: ProcTab; label: string }> = [
  { id: "coverage", label: "Coverage Overview" },
  { id: "reservations", label: "Stock Reservations" },
  { id: "incoming-pos", label: "Incoming POs" },
  { id: "transfers", label: "Internal Transfers" },
  { id: "purchases", label: "Purchase Recommendations" },
  { id: "suppliers", label: "Supplier Comparison" },
  { id: "simulator", label: "Procurement Simulator" },
  { id: "delivery", label: "Delivery Tracking" },
  { id: "exceptions", label: "Exceptions" },
  { id: "approvals", label: "Approvals" },
  { id: "reports", label: "Reports" },
  { id: "history", label: "History" },
];

const AI_PROCUREMENT_PROMPTS = [
  "Which materials are not available for tomorrow's event?",
  "Can we avoid purchasing salmon by transferring stock from another outlet?",
  "Which incoming POs can cover this event?",
  "Why is the recommended purchase quantity higher than the shortage?",
  "Which supplier can deliver before preparation starts?",
  "Can we consolidate requirements across three events?",
];

const fmtNum = (v: number | null | undefined, digits = 2) =>
  v === null || v === undefined || Number.isNaN(v) ? "Unknown" : Number(v).toLocaleString(undefined, { maximumFractionDigits: digits });

const fmtDate = (iso: string | null | undefined) => {
  if (!iso) return "—";
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString();
};

const statusBadgeTone = (status: string): string => {
  const s = (status || "").toUpperCase();
  if (s.includes("READY") || s === "ACTIVE" || s === "APPROVED" || s === "FULFILLED" || s === "RESOLVED" || s === "RECEIVED") {
    return "cs2-badge--live";
  }
  if (s.includes("SHORTAGE") || s === "CRITICAL" || s === "REJECTED" || s === "REVERSED" || s === "CANCELLED" || s.includes("RISK")) {
    return "cs2-badge--danger";
  }
  if (s.includes("PENDING") || s.includes("PROPOSED") || s.includes("PARTIAL") || s.includes("WARNING")) {
    return "cs2-badge--warn";
  }
  return "cs2-badge--indigo";
};

const downloadCsv = (filename: string, csvContent: string) => {
  const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
};

const CulinaryProcurementStudioWorkspace: React.FC<Props> = ({ onOpenWorkspace }) => {
  const [tab, setTab] = useState<ProcTab>("coverage");
  const [dashboard, setDashboard] = useState<SilaCulinaryProcurementDashboard | null>(null);
  const [plans, setPlans] = useState<SilaCulinaryProcurementPlanSummary[]>([]);
  const [mrpRuns, setMrpRuns] = useState<SilaCulinaryMrpRunSummary[]>([]);
  const [activePlan, setActivePlan] = useState<SilaCulinaryProcurementPlanDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // Create plan from approved MRP
  const [showNewPlan, setShowNewPlan] = useState(false);
  const [selectedMrpId, setSelectedMrpId] = useState("");
  const [newPlanName, setNewPlanName] = useState("");
  const [newPlanIsSim, setNewPlanIsSim] = useState(false);
  const [includeProposedItoInNet, setIncludeProposedItoInNet] = useState(true);

  // Coverage table controls (§46)
  const [searchText, setSearchText] = useState("");
  const [statusFilter, setStatusFilter] = useState("ALL");
  const [groupBy, setGroupBy] = useState<"NONE" | "CATEGORY" | "READINESS">("NONE");
  const [sortBy, setSortBy] = useState<"material" | "requiredBy" | "shortage" | "value">("shortage");
  const [showPackCols, setShowPackCols] = useState(true);
  const [expandedLineId, setExpandedLineId] = useState<string | null>(null);

  // Reservation creation state (§10-§14)
  const [resLineId, setResLineId] = useState("");
  const [resLocationId, setResLocationId] = useState("");
  const [resQty, setResQty] = useState("10");
  const [resBatch, setResBatch] = useState("");
  const [resActivateNow, setResActivateNow] = useState(true);
  const [resSafetyOverride, setResSafetyOverride] = useState(false);
  const [resSafetyReason, setResSafetyReason] = useState("");

  // Incoming PO & GRN reconciliation state (§17-§19, §42-§43)
  const [poAllocLineId, setPoAllocLineId] = useState("");
  const [poAllocItemId, setPoAllocItemId] = useState("");
  const [poAllocQty, setPoAllocQty] = useState("20");
  const [grnIdInput, setGrnIdInput] = useState("");
  const [grnIsReversal, setGrnIsReversal] = useState(false);
  const [grnRevDoc, setGrnRevDoc] = useState("");

  // ITO draft & SAP 312 reversal state (§20-§23)
  const [itoLineId, setItoLineId] = useState("");
  const [itoFromLocId, setItoFromLocId] = useState("");
  const [itoToLocId, setItoToLocId] = useState("");
  const [itoQty, setItoQty] = useState("15");
  const [itoReason, setItoReason] = useState("Event requirement internal transfer");
  const [itoRevId, setItoRevId] = useState("");
  const [itoRevQty, setItoRevQty] = useState("15");
  const [itoRevDoc, setItoRevDoc] = useState("4900000312");

  // PO draft creation state (§35-§38)
  const [selectedPoLineIds, setSelectedPoLineIds] = useState<string[]>([]);
  const [poSupplierId, setPoSupplierId] = useState("");
  const [poAcctCategory, setPoAcctCategory] = useState("");
  const [poCostCenter, setPoCostCenter] = useState("");
  const [poGlAccount, setPoGlAccount] = useState("");
  const [poContractRef, setPoContractRef] = useState("");
  const [wbLineId, setWbLineId] = useState("");
  const [wbItemId, setWbItemId] = useState("");

  // Simulator state (§47, §50)
  const [simScale, setSimScale] = useState(1.0);
  const [simLocalStock, setSimLocalStock] = useState("");
  const [simSafetyStock, setSimSafetyStock] = useState("");
  const [simCompetingRes, setSimCompetingRes] = useState("");
  const [simIncomingPoQty, setSimIncomingPoQty] = useState("");
  const [simIncomingDelayDays, setSimIncomingDelayDays] = useState("0");
  const [simTransferQty, setSimTransferQty] = useState("");
  const [simSupplierPrice, setSimSupplierPrice] = useState("");
  const [simPackSize, setSimPackSize] = useState("");
  const [simMoq, setSimMoq] = useState("");
  const [simLeadDays, setSimLeadDays] = useState("");
  const [simScenarioType, setSimScenarioType] = useState("CUSTOM");
  const [simScenarioName, setSimScenarioName] = useState("Interactive What-If");
  const [simResult, setSimResult] = useState<SilaCulinarySimulateProcurementResult | null>(null);

  // Optimization & AI state (§31, §48-§49)
  const [optMaxBudget, setOptMaxBudget] = useState("");
  const [optResult, setOptResult] = useState<SilaCulinaryProcurementOptimizeResult | null>(null);
  const [aiQuestion, setAiQuestion] = useState(AI_PROCUREMENT_PROMPTS[0]);
  const [aiResponse, setAiResponse] = useState<SilaCulinaryProcurementAiAnalyzeResponse | null>(null);

  // Governance / Change Impact / Cancellation / Approvals
  const [approvalComment, setApprovalComment] = useState("");
  const [changeImpactMrpId, setChangeImpactMrpId] = useState("");
  const [cancelReason, setCancelReason] = useState("");

  // Reports & Phase 3A Handover (§55, §66)
  const [reportCatalog, setReportCatalog] = useState<SilaCulinaryProcurementReportCatalogItem[]>([]);
  const [selectedReportCode, setSelectedReportCode] = useState("RPT_01_EVENT_COVERAGE");
  const [activeReport, setActiveReport] = useState<SilaCulinaryProcurementReportResult | null>(null);
  const [phase3a, setPhase3a] = useState<SilaCulinaryPhase3AHandover | null>(null);

  const loadWorkspace = useCallback(async (preferredPlanId?: string) => {
    setLoading(true);
    setError(null);
    try {
      const [dash, planList, runsList, catalog] = await Promise.all([
        getProcurementDashboard(),
        listProcurementPlans({ includeSimulations: true }),
        listMrpRuns({ includeSimulations: true }),
        getProcurementReportCatalog(),
      ]);
      setDashboard(dash);
      setPlans(planList);
      setMrpRuns(runsList);
      setReportCatalog(catalog);

      const eligibleMrps = runsList.filter((r) => r.status === "APPROVED" || r.status === "RELEASED");
      if (eligibleMrps.length > 0 && !selectedMrpId) {
        setSelectedMrpId(eligibleMrps[0].id);
      } else if (runsList.length > 0 && !selectedMrpId) {
        setSelectedMrpId(runsList[0].id);
      }

      const targetId = preferredPlanId || activePlan?.summary.id || planList[0]?.id;
      if (targetId) {
        const detail = await getProcurementPlan(targetId);
        setActivePlan(detail);
        if (detail.lines.length > 0) {
          const firstLine = detail.lines[0];
          setResLineId((prev) => prev || firstLine.id);
          setResLocationId((prev) => prev || firstLine.destinationLocationId || "");
          setItoLineId((prev) => prev || firstLine.id);
          setItoToLocId((prev) => prev || firstLine.destinationLocationId || "");
          if (firstLine.itoCandidates.length > 0) {
            setItoFromLocId((prev) => prev || firstLine.itoCandidates[0].sourceLocationId);
          }
          setPoAllocLineId((prev) => prev || firstLine.id);
          if (firstLine.recommendedSupplierId) {
            setPoSupplierId((prev) => prev || firstLine.recommendedSupplierId || "");
          }
          setWbLineId((prev) => prev || firstLine.id);
        }
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load Event Procurement Studio.");
    } finally {
      setLoading(false);
    }
  }, [activePlan?.summary.id, selectedMrpId]);

  useEffect(() => {
    void loadWorkspace();
  }, [loadWorkspace]);

  const selectPlan = async (planId: string) => {
    setBusy(true);
    setError(null);
    setNotice(null);
    setSimResult(null);
    setOptResult(null);
    setAiResponse(null);
    setPhase3a(null);
    try {
      const detail = await getProcurementPlan(planId);
      setActivePlan(detail);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load procurement plan.");
    } finally {
      setBusy(false);
    }
  };

  const handleCreatePlan = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedMrpId) {
      setError("Select an approved/released Phase 2B MRP Run first.");
      return;
    }
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      const detail = await createProcurementPlan({
        mrpRunId: selectedMrpId,
        name: newPlanName.trim() || undefined,
        isSimulation: newPlanIsSim,
        includeProposedItoInNet,
        idempotencyKey: `plan-${selectedMrpId}-${Date.now()}`,
      });
      setActivePlan(detail);
      setShowNewPlan(false);
      setNewPlanName("");
      setNotice(`Procurement plan ${detail.summary.planCode} calculated.`);
      await loadWorkspace(detail.summary.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create procurement plan.");
    } finally {
      setBusy(false);
    }
  };

  const handleRecalculate = async () => {
    if (!activePlan) return;
    setBusy(true);
    setError(null);
    try {
      const detail = await recalculateProcurementPlan(activePlan.summary.id);
      setActivePlan(detail);
      setNotice(`Recalculated ${detail.summary.planCode}.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Recalculation failed.");
    } finally {
      setBusy(false);
    }
  };

  const filteredLines = useMemo(() => {
    if (!activePlan) return [];
    const q = searchText.trim().toLowerCase();
    const list = activePlan.lines.filter((ln) => {
      if (statusFilter !== "ALL" && ln.readinessStatus !== statusFilter && ln.coverageStatus !== statusFilter) {
        return false;
      }
      if (!q) return true;
      return (
        (ln.materialCode || "").toLowerCase().includes(q) ||
        ln.materialDescription.toLowerCase().includes(q) ||
        ln.materialCategory.toLowerCase().includes(q) ||
        (ln.recommendedSupplierName || "").toLowerCase().includes(q)
      );
    });
    return [...list].sort((a, b) => {
      if (sortBy === "material") return a.materialDescription.localeCompare(b.materialDescription);
      if (sortBy === "requiredBy") return a.requiredByUtc.localeCompare(b.requiredByUtc);
      if (sortBy === "value") return (b.estimatedPurchaseValue ?? 0) - (a.estimatedPurchaseValue ?? 0);
      return b.netUncoveredQty - a.netUncoveredQty;
    });
  }, [activePlan, searchText, statusFilter, sortBy]);

  const groupedLines = useMemo(() => {
    if (groupBy === "NONE") return [{ group: "All Materials", items: filteredLines }];
    const map = new Map<string, SilaCulinaryProcurementLine[]>();
    for (const ln of filteredLines) {
      const key = groupBy === "CATEGORY" ? ln.materialCategory || "Uncategorized" : ln.readinessStatus || "UNKNOWN";
      const arr = map.get(key) ?? [];
      arr.push(ln);
      map.set(key, arr);
    }
    return Array.from(map.entries()).map(([group, items]) => ({ group, items }));
  }, [filteredLines, groupBy]);

  const handleRunSimulator = async (saveScenario: boolean) => {
    if (!activePlan) return;
    setBusy(true);
    setError(null);
    try {
      const res = await simulateProcurementPlan(activePlan.summary.id, {
        guestScaleFactor: simScale,
        localStockOverrideQty: simLocalStock ? Number(simLocalStock) : null,
        safetyStockOverrideQty: simSafetyStock ? Number(simSafetyStock) : null,
        competingReservationOverrideQty: simCompetingRes ? Number(simCompetingRes) : null,
        incomingPoQtyOverride: simIncomingPoQty ? Number(simIncomingPoQty) : null,
        incomingPoDelayDays: simIncomingDelayDays ? Number(simIncomingDelayDays) : null,
        transferableStockOverrideQty: simTransferQty ? Number(simTransferQty) : null,
        supplierPriceOverride: simSupplierPrice ? Number(simSupplierPrice) : null,
        packSizeOverride: simPackSize ? Number(simPackSize) : null,
        moqOverride: simMoq ? Number(simMoq) : null,
        supplierLeadTimeDaysOverride: simLeadDays ? Number(simLeadDays) : null,
        scenarioType: simScenarioType,
        scenarioName: simScenarioName,
        saveScenario,
      });
      setSimResult(res);
      if (saveScenario) {
        setNotice(`Scenario "${simScenarioName}" saved to plan.`);
        setActivePlan(await getProcurementPlan(activePlan.summary.id));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Simulation failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleCreateReservation = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activePlan || !resLineId || !resLocationId) return;
    setBusy(true);
    setError(null);
    try {
      await createStockReservation(activePlan.summary.id, {
        procurementLineId: resLineId,
        locationId: resLocationId,
        quantity: Number(resQty),
        batchNumber: resBatch.trim() || undefined,
        activateImmediately: resActivateNow,
        allowSafetyStockOverride: resSafetyOverride,
        safetyOverrideReason: resSafetyReason.trim() || undefined,
        idempotencyKey: `res-${resLineId}-${Date.now()}`,
      });
      setActivePlan(await getProcurementPlan(activePlan.summary.id));
      setNotice("Atomic stock reservation recorded (zero physical inventory ledger deduction).");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Reservation failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleCreateItoDraft = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activePlan || !itoLineId || !itoFromLocId || !itoToLocId) return;
    setBusy(true);
    setError(null);
    try {
      const alloc = await createProcurementItoDraft(activePlan.summary.id, {
        procurementLineId: itoLineId,
        fromLocationId: itoFromLocId,
        toLocationId: itoToLocId,
        quantity: Number(itoQty),
        reason: itoReason.trim() || undefined,
        idempotencyKey: `ito-${itoLineId}-${itoFromLocId}-${Date.now()}`,
      });
      setActivePlan(await getProcurementPlan(activePlan.summary.id));
      setNotice(`ITO draft ${alloc.itoNumber ?? ""} created via existing SILA 311 workflow (awaiting approval, not auto-posted).`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "ITO draft creation failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleCreatePoDraft = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activePlan || selectedPoLineIds.length === 0 || !poSupplierId) {
      setError("Select at least one procurement line and a supplier.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const alloc = await createProcurementPoDraft(activePlan.summary.id, {
        procurementLineIds: selectedPoLineIds,
        supplierId: poSupplierId,
        accountAssignmentCategory: poAcctCategory.trim() || undefined,
        costCenter: poCostCenter.trim() || undefined,
        glAccount: poGlAccount.trim() || undefined,
        contractReference: poContractRef.trim() || undefined,
        idempotencyKey: `po-${activePlan.summary.id}-${selectedPoLineIds.join("_")}-${poSupplierId}`,
      });
      setActivePlan(await getProcurementPlan(activePlan.summary.id));
      setNotice(`PO draft ${alloc.poNumber ?? ""} created with DeferErpUntilApproved=true (never auto-released).`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "PO draft creation failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleLoadReport = async (code: string) => {
    if (!activePlan) return;
    setBusy(true);
    setError(null);
    try {
      const rpt = await exportProcurementReport(activePlan.summary.id, code);
      setActiveReport(rpt);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate report.");
    } finally {
      setBusy(false);
    }
  };

  const handleLoadPhase3A = async () => {
    if (!activePlan) return;
    setBusy(true);
    setError(null);
    try {
      const data = await getPhase3AHandover(activePlan.summary.id);
      setPhase3a(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load Phase 3A handover.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="cs2-shell" data-testid="culinary-procurement-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Event Procurement Intelligence Studio (Phase 2C)</h2>
          <p className="cs2-subtitle">
            Date-aware projected availability, atomic stock reservations, FEFO batches, incoming PO &amp; GRN reconciliation,
            outlet/kitchen ITO (SAP 311/312), supplier &amp; MOQ/pack optimization, and Phase 3A cost handover.
          </p>
        </div>
        <div className="cs2-actions-row">
          <span className="cs2-badge cs2-badge--live">Phase 2C Live</span>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("mrp")}>
            Event MRP (2B)
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--primary"
            onClick={() => setShowNewPlan((v) => !v)}
            data-testid="proc-new-plan-btn"
          >
            {showNewPlan ? "Close New Plan" : "+ New Plan from MRP"}
          </button>
        </div>
      </div>

      <div className="cs2-banner cs2-banner--phase" role="status">
        <div>
          <strong>REGRESSION GUARD ACTIVE — NO LIVE SAP POSTINGS OR AUTO-PO RELEASES</strong>
          <div>
            PO drafts use the existing <code>CreatePurchaseOrderCommand</code> with <code>DeferErpUntilApproved=true</code>.
            Internal transfers reuse the existing <code>FIVE S4 - Transfer Stock</code> (311/312) approval workflow.
          </div>
        </div>
        <div className="cs2-actions-row">
          <select
            className="sila-input"
            aria-label="Select procurement plan"
            value={activePlan?.summary.id ?? ""}
            onChange={(e) => e.target.value && void selectPlan(e.target.value)}
            data-testid="proc-plan-selector"
          >
            <option value="">-- Select Procurement Plan --</option>
            {plans.map((p) => (
              <option key={p.id} value={p.id}>
                {p.planCode} · {p.name} ({p.status})
              </option>
            ))}
          </select>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={!activePlan || busy} onClick={handleRecalculate}>
            Recalculate
          </button>
        </div>
      </div>

      {error && (
        <div className="cs2-banner cs2-banner--warn" role="alert">
          <span>{error}</span>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => setError(null)}>
            Dismiss
          </button>
        </div>
      )}
      {notice && (
        <div className="cs2-banner" role="status">
          <span>{notice}</span>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => setNotice(null)}>
            Dismiss
          </button>
        </div>
      )}

      {showNewPlan && (
        <form className="cs2-card" onSubmit={handleCreatePlan} data-testid="proc-create-plan-form">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Create Procurement Plan from Approved Phase 2B MRP</h3>
          </div>
          <div className="cs2-form-grid">
            <label>
              <span>Phase 2B MRP Run</span>
              <select className="sila-input" value={selectedMrpId} onChange={(e) => setSelectedMrpId(e.target.value)}>
                <option value="">-- Choose MRP Run --</option>
                {mrpRuns.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.mrpCode} v{r.versionNumber} · {r.name} [{r.status}]
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Plan Name (optional)</span>
              <input className="sila-input" value={newPlanName} onChange={(e) => setNewPlanName(e.target.value)} placeholder="e.g., Grand Corporate Gala Procurement" />
            </label>
            <label className="cs2-checkbox-row">
              <input type="checkbox" checked={newPlanIsSim} onChange={(e) => setNewPlanIsSim(e.target.checked)} />
              <span>Simulation only (allow draft MRP without operational allocations)</span>
            </label>
            <label className="cs2-checkbox-row">
              <input type="checkbox" checked={includeProposedItoInNet} onChange={(e) => setIncludeProposedItoInNet(e.target.checked)} />
              <span>Subtract proposed ITOs from recommended net purchase</span>
            </label>
          </div>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="submit" className="sila-btn sila-btn--primary" disabled={busy || !selectedMrpId}>
              Calculate Procurement Plan
            </button>
          </div>
        </form>
      )}

      {/* §45 / §54 Event & Dashboard KPI Cards */}
      {activePlan ? (
        <div className="cs2-card">
          <div className="cs2-card__head">
            <div>
              <h3 className="cs2-card__title">
                {activePlan.summary.primaryEventName || activePlan.summary.name} ({activePlan.summary.planCode})
              </h3>
              <p className="cs2-empty-note">
                Event: <strong>{activePlan.summary.primaryEventCode || "Multi-Event"}</strong> · Property:{" "}
                <strong>{activePlan.summary.propertyName || "—"}</strong> (Plant {activePlan.summary.plantCode || "—"}) ·
                Venue: <strong>{activePlan.summary.venueName || "—"}</strong> · Event Date:{" "}
                <strong>{fmtDate(activePlan.summary.eventDateUtc)}</strong> · Prep Deadline:{" "}
                <strong>{fmtDate(activePlan.summary.preparationDeadlineUtc)}</strong> · Guests:{" "}
                <strong>{activePlan.summary.plannedGuests}</strong> · Chef:{" "}
                <strong>{activePlan.summary.responsibleChef || "—"}</strong> · MRP:{" "}
                <strong>{activePlan.summary.mrpCode} v{activePlan.summary.mrpVersion}</strong>
              </p>
            </div>
            <span className={`cs2-badge ${statusBadgeTone(activePlan.summary.status)}`}>{activePlan.summary.status}</span>
          </div>

          <div className="cs2-kpi-grid" data-testid="proc-kpi-grid">
            <div className="cs2-kpi cs2-kpi--accent">
              <span className="cs2-kpi__label">Required Materials</span>
              <span className="cs2-kpi__value">{activePlan.summary.requiredMaterialsCount}</span>
              <span className="cs2-kpi__hint">Active reservations: {dashboard?.activeReservationsCount ?? activePlan.reservations.length}</span>
            </div>
            <div className="cs2-kpi cs2-kpi--emerald">
              <span className="cs2-kpi__label">Fully Reserved</span>
              <span className="cs2-kpi__value">{activePlan.summary.fullyReservedCount}</span>
              <span className="cs2-kpi__hint">Physical readiness: {activePlan.summary.physicalReadinessPercent}%</span>
            </div>
            <div className="cs2-kpi cs2-kpi--cyan">
              <span className="cs2-kpi__label">Incoming PO Covered</span>
              <span className="cs2-kpi__value">{activePlan.summary.incomingPoCoveredCount}</span>
              <span className="cs2-kpi__hint">Open linked POs: {dashboard?.openLinkedPos ?? 0}</span>
            </div>
            <div className="cs2-kpi cs2-kpi--cyan">
              <span className="cs2-kpi__label">Transfer Covered</span>
              <span className="cs2-kpi__value">{activePlan.summary.transferCoveredCount}</span>
              <span className="cs2-kpi__hint">Projected readiness: {activePlan.summary.projectedReadinessPercent}%</span>
            </div>
            <div className="cs2-kpi cs2-kpi--amber">
              <span className="cs2-kpi__label">Requiring Purchase</span>
              <span className="cs2-kpi__value">{activePlan.summary.purchaseRequiredCount}</span>
              <span className="cs2-kpi__hint">Pending approvals: {activePlan.summary.pendingApprovalCount}</span>
            </div>
            <div className="cs2-kpi cs2-kpi--rose">
              <span className="cs2-kpi__label">Shortages / Delayed</span>
              <span className="cs2-kpi__value">
                {activePlan.summary.criticalShortageCount} / {activePlan.summary.delayedDeliveryCount}
              </span>
              <span className="cs2-kpi__hint">Open exceptions: {activePlan.exceptions.filter((x) => x.resolutionStatus === "OPEN").length}</span>
            </div>
            {activePlan.costsVisible && (
              <div className="cs2-kpi cs2-kpi--accent">
                <span className="cs2-kpi__label">Est. New Purchase ({activePlan.summary.currency})</span>
                <span className="cs2-kpi__value">{fmtNum(activePlan.summary.estimatedNewPurchaseValue)}</span>
                <span className="cs2-kpi__hint">Committed: {fmtNum(activePlan.summary.committedProcurementValue)}</span>
              </div>
            )}
          </div>
        </div>
      ) : (
        <div className="cs2-card">
          <p className="cs2-empty-note">
            {loading ? "Loading procurement plans..." : "No procurement plan selected. Create a plan from an approved Phase 2B MRP run above."}
          </p>
        </div>
      )}

      {/* §45 Contextual Tabs */}
      <div className="cs2-nav-tabs" role="tablist" aria-label="Procurement Studio tabs">
        {PROC_TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            data-testid={`proc-tab-${t.id}`}
            className={`cs2-nav-tab${tab === t.id ? " is-active" : ""}`}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* TAB 1: COVERAGE OVERVIEW (§46) */}
      {tab === "coverage" && activePlan && (
        <div className="cs2-card" data-testid="proc-panel-coverage">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Interactive Material Coverage Table</h3>
            <div className="cs2-actions-row">
              <input
                className="sila-input"
                placeholder="Search material, code, supplier..."
                value={searchText}
                onChange={(e) => setSearchText(e.target.value)}
              />
              <select className="sila-input" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="ALL">All Statuses</option>
                <option value="MATERIAL_READY">Material Ready</option>
                <option value="MATERIAL_SHORTAGE">Material Shortage</option>
                <option value="DELIVERY_RISK">Delivery Risk</option>
                <option value="PURCHASE_RECOMMENDED">Purchase Recommended</option>
              </select>
              <select className="sila-input" value={groupBy} onChange={(e) => setGroupBy(e.target.value as "NONE" | "CATEGORY" | "READINESS")}>
                <option value="NONE">No Grouping</option>
                <option value="CATEGORY">Group by Category</option>
                <option value="READINESS">Group by Readiness</option>
              </select>
              <select className="sila-input" value={sortBy} onChange={(e) => setSortBy(e.target.value as "material" | "requiredBy" | "shortage" | "value")}>
                <option value="shortage">Sort: Net Shortage</option>
                <option value="requiredBy">Sort: Required Date</option>
                <option value="value">Sort: Est. Value</option>
                <option value="material">Sort: Material</option>
              </select>
              <label className="cs2-checkbox-row">
                <input type="checkbox" checked={showPackCols} onChange={(e) => setShowPackCols(e.target.checked)} />
                <span>Pack/MOQ cols</span>
              </label>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                onClick={() => void handleLoadReport("RPT_01_EVENT_COVERAGE").then(() => setTab("reports"))}
              >
                Export CSV/Excel
              </button>
            </div>
          </div>

          {groupedLines.map((grp) => (
            <div key={grp.group} style={{ marginBottom: 14 }}>
              {groupBy !== "NONE" && <h4 style={{ margin: "6px 0" }}>{grp.group} ({grp.items.length})</h4>}
              <div className="cs2-table-wrap">
                <table className="cs2-table">
                  <thead>
                    <tr>
                      <th>Material</th>
                      <th className="cs2-num">Req Qty</th>
                      <th>UOM</th>
                      <th>Req Date</th>
                      <th className="cs2-num">Local On-Hand</th>
                      <th className="cs2-num">Competing Res</th>
                      <th className="cs2-num">Safety</th>
                      <th className="cs2-num">Eligible Local</th>
                      <th className="cs2-num">Active Res</th>
                      <th className="cs2-num">Inc PO</th>
                      <th className="cs2-num">Prop ITO</th>
                      <th className="cs2-num">Confirmed Gap</th>
                      <th className="cs2-num">Net Uncov</th>
                      {showPackCols && <th className="cs2-num">Rec Purchase (Packs)</th>}
                      <th>Supplier</th>
                      {activePlan.costsVisible && <th className="cs2-num">Est Value</th>}
                      <th>Coverage / Readiness</th>
                      <th>Details</th>
                    </tr>
                  </thead>
                  <tbody>
                    {grp.items.map((ln) => {
                      const expanded = expandedLineId === ln.id;
                      return (
                        <React.Fragment key={ln.id}>
                          <tr>
                            <td>
                              <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                                {ln.materialImageUrl && (
                                  <img src={ln.materialImageUrl} alt={ln.materialDescription} style={{ width: 26, height: 26, borderRadius: 6, objectFit: "cover" }} />
                                )}
                                <div>
                                  <strong>{ln.materialDescription}</strong>
                                  <div className="cs2-empty-note">{ln.materialCode || "Unmapped"} · {ln.destinationKitchenName || "Kitchen"}</div>
                                </div>
                              </div>
                            </td>
                            <td className="cs2-num">{fmtNum(ln.grossRequirementQty)}</td>
                            <td>{ln.baseUom}</td>
                            <td>{fmtDate(ln.requiredByUtc)}</td>
                            <td className="cs2-num">{fmtNum(ln.localOnHandQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.competingReservationsQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.safetyStockQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.eligibleLocalStockQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.activeEventReservationQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.incomingPoAllocatedQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.proposedItoQty)}</td>
                            <td className="cs2-num">{fmtNum(ln.remainingConfirmedGapQty)}</td>
                            <td className="cs2-num"><strong>{fmtNum(ln.netUncoveredQty)}</strong></td>
                            {showPackCols && (
                              <td className="cs2-num">
                                {fmtNum(ln.recommendedPurchaseQty)} {ln.purchasingUom || ln.baseUom}
                                {ln.numberOfPacks ? ` (${ln.numberOfPacks}×${ln.packSizeQty})` : ""}
                              </td>
                            )}
                            <td>{ln.recommendedSupplierName || "—"}</td>
                            {activePlan.costsVisible && (
                              <td className="cs2-num">{ln.estimatedPurchaseValue != null ? `${ln.currency} ${fmtNum(ln.estimatedPurchaseValue)}` : "Unknown"}</td>
                            )}
                            <td>
                              <span className={`cs2-badge ${statusBadgeTone(ln.readinessStatus)}`}>{ln.readinessStatus}</span>
                            </td>
                            <td>
                              <button
                                type="button"
                                className="sila-btn sila-btn--sm sila-btn--secondary"
                                onClick={() => setExpandedLineId(expanded ? null : ln.id)}
                              >
                                {expanded ? "Hide" : "Inspect"}
                              </button>
                            </td>
                          </tr>
                          {expanded && (
                            <tr>
                              <td colSpan={activePlan.costsVisible ? 18 : 17} style={{ background: "#f8fafc" }}>
                                <div className="cs2-grid-3">
                                  <div>
                                    <strong>FEFO Eligible Batches</strong>
                                    {ln.fefoBatches.length === 0 ? (
                                      <p className="cs2-empty-note">No batch-tracked lots (or expiry unknown).</p>
                                    ) : (
                                      <ul>
                                        {ln.fefoBatches.map((b, idx) => (
                                          <li key={idx}>
                                            {b.batchNumber || "UNBATCHED"} · Exp: {fmtDate(b.expiryDateUtc)} · Avail {b.availableQty} · Rec {b.recommendedQty} [{b.qualityStatus}]
                                          </li>
                                        ))}
                                      </ul>
                                    )}
                                  </div>
                                  <div>
                                    <strong>ITO Candidates ({ln.itoCandidates.length})</strong>
                                    {ln.itoCandidates.length === 0 ? (
                                      <p className="cs2-empty-note">No transferable stock at other locations.</p>
                                    ) : (
                                      <ul>
                                        {ln.itoCandidates.map((c) => (
                                          <li key={c.sourceLocationId}>
                                            {c.sourceLocationName} ({c.sourceStorageLocation}) · Avail {c.availableQty} · Rec {c.recommendedTransferQty} ·{" "}
                                            {c.samePlant311Eligible ? "SAP 311 Eligible" : `Not 311: ${c.ineligibilityReason}`}
                                          </li>
                                        ))}
                                      </ul>
                                    )}
                                  </div>
                                  <div>
                                    <strong>Delivery &amp; Weekly Bucket</strong>
                                    <p className="cs2-empty-note">
                                      Delivery Feasible: <strong>{ln.isDeliveryFeasible ? "Yes" : "No (Lead-time risk)"}</strong>
                                      <br />
                                      Latest Order Release: {fmtDate(ln.latestOrderReleaseDateUtc)}
                                      <br />
                                      Weekly Bucket Linked: {ln.weeklyBucketLinked ? "Yes (deduplicated)" : "No"}
                                    </p>
                                  </div>
                                </div>
                              </td>
                            </tr>
                          )}
                        </React.Fragment>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* TAB 2: STOCK RESERVATIONS (§10-§14) */}
      {tab === "reservations" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-reservations">
          <form className="cs2-card" onSubmit={handleCreateReservation}>
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Create Atomic Stock Reservation</h3>
            </div>
            <div className="cs2-form-grid">
              <label>
                <span>Material Line</span>
                <select className="sila-input" value={resLineId} onChange={(e) => setResLineId(e.target.value)}>
                  {activePlan.lines.map((l) => (
                    <option key={l.id} value={l.id}>
                      {l.materialDescription} (Req {l.grossRequirementQty} {l.baseUom})
                    </option>
                  ))}
                </select>
              </label>
              <label>
                <span>Location ID</span>
                <input className="sila-input" value={resLocationId} onChange={(e) => setResLocationId(e.target.value)} placeholder="Location GUID" />
              </label>
              <label>
                <span>Quantity</span>
                <input className="sila-input" type="number" step="0.01" value={resQty} onChange={(e) => setResQty(e.target.value)} />
              </label>
              <label>
                <span>Batch (optional FEFO)</span>
                <input className="sila-input" value={resBatch} onChange={(e) => setResBatch(e.target.value)} placeholder="Batch number" />
              </label>
              <label className="cs2-checkbox-row">
                <input type="checkbox" checked={resActivateNow} onChange={(e) => setResActivateNow(e.target.checked)} />
                <span>Activate immediately (atomic over-allocation check)</span>
              </label>
              <label className="cs2-checkbox-row">
                <input type="checkbox" checked={resSafetyOverride} onChange={(e) => setResSafetyOverride(e.target.checked)} />
                <span>Authorized safety stock override</span>
              </label>
              {resSafetyOverride && (
                <label>
                  <span>Override Reason</span>
                  <input className="sila-input" value={resSafetyReason} onChange={(e) => setResSafetyReason(e.target.value)} required />
                </label>
              )}
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 12 }}>
              <button type="submit" className="sila-btn sila-btn--primary" disabled={busy}>Reserve Stock</button>
            </div>
          </form>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Event Reservations ({activePlan.reservations.length})</h3>
            </div>
            <div className="cs2-table-wrap">
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>Code</th>
                    <th>Material</th>
                    <th className="cs2-num">Reserved</th>
                    <th>Batch</th>
                    <th>Status</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlan.reservations.map((r) => (
                    <tr key={r.id}>
                      <td>{r.reservationCode}</td>
                      <td>{r.materialDescription}</td>
                      <td className="cs2-num">{r.reservedQty} {r.baseUom}</td>
                      <td>{r.batchNumber || "—"}</td>
                      <td><span className={`cs2-badge ${statusBadgeTone(r.status)}`}>{r.status}</span></td>
                      <td className="cs2-actions-row">
                        {r.status !== "ACTIVE" && r.status !== "RELEASED" && (
                          <button
                            type="button"
                            className="sila-btn sila-btn--sm sila-btn--primary"
                            onClick={() => void transitionStockReservation(activePlan.summary.id, r.id, "ACTIVATE").then(() => selectPlan(activePlan.summary.id))}
                          >
                            Activate
                          </button>
                        )}
                        {r.status !== "RELEASED" && r.status !== "CANCELLED" && (
                          <button
                            type="button"
                            className="sila-btn sila-btn--sm sila-btn--secondary"
                            onClick={() => void transitionStockReservation(activePlan.summary.id, r.id, "RELEASE", "Manual release").then(() => selectPlan(activePlan.summary.id))}
                          >
                            Release
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* TAB 3: INCOMING POS & GRN RECONCILIATION (§17-§19, §42-§43) */}
      {tab === "incoming-pos" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-incoming-pos">
          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Allocate Incoming PO &amp; Reconcile GRN</h3>
            </div>
            <div className="cs2-form-grid">
              <label>
                <span>Material Line</span>
                <select className="sila-input" value={poAllocLineId} onChange={(e) => setPoAllocLineId(e.target.value)}>
                  {activePlan.lines.map((l) => (
                    <option key={l.id} value={l.id}>{l.materialDescription}</option>
                  ))}
                </select>
              </label>
              <label>
                <span>PO Item ID</span>
                <input className="sila-input" value={poAllocItemId} onChange={(e) => setPoAllocItemId(e.target.value)} placeholder="PurchaseOrderItem GUID" />
              </label>
              <label>
                <span>Quantity</span>
                <input className="sila-input" type="number" step="0.01" value={poAllocQty} onChange={(e) => setPoAllocQty(e.target.value)} />
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 10 }}>
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                disabled={busy || !poAllocLineId || !poAllocItemId}
                onClick={() =>
                  void allocateIncomingPo(activePlan.summary.id, {
                    procurementLineId: poAllocLineId,
                    purchaseOrderItemId: poAllocItemId,
                    quantity: Number(poAllocQty),
                    idempotencyKey: `incpo-${poAllocLineId}-${poAllocItemId}-${Date.now()}`,
                  })
                    .then(() => selectPlan(activePlan.summary.id))
                    .catch((err) => setError(err instanceof Error ? err.message : "Allocation failed."))
                }
              >
                Allocate Incoming PO
              </button>
            </div>

            <hr style={{ margin: "14px 0", borderColor: "#e2e8f0" }} />
            <div className="cs2-form-grid">
              <label>
                <span>Goods Receipt ID (GRN)</span>
                <input className="sila-input" value={grnIdInput} onChange={(e) => setGrnIdInput(e.target.value)} placeholder="GoodsReceipt GUID" />
              </label>
              <label className="cs2-checkbox-row">
                <input type="checkbox" checked={grnIsReversal} onChange={(e) => setGrnIsReversal(e.target.checked)} />
                <span>GRN Reversal Reconciliation</span>
              </label>
              {grnIsReversal && (
                <label>
                  <span>Reversal Material Doc</span>
                  <input className="sila-input" value={grnRevDoc} onChange={(e) => setGrnRevDoc(e.target.value)} placeholder="SAP Reversal Doc #" />
                </label>
              )}
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 10 }}>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy || !grnIdInput}
                onClick={() =>
                  void reconcileProcurementGrn(activePlan.summary.id, {
                    goodsReceiptId: grnIdInput,
                    isReversal: grnIsReversal,
                    reversalMaterialDocument: grnRevDoc || undefined,
                  })
                    .then((d) => {
                      setActivePlan(d);
                      setNotice("GRN receipt/reversal reconciled with incoming allocations & reservations.");
                    })
                    .catch((err) => setError(err instanceof Error ? err.message : "GRN reconciliation failed."))
                }
              >
                Reconcile GRN
              </button>
            </div>
          </div>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Incoming PO Allocations</h3>
            </div>
            <div className="cs2-table-wrap">
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>PO</th>
                    <th>Material</th>
                    <th className="cs2-num">Alloc</th>
                    <th className="cs2-num">Recvd</th>
                    <th className="cs2-num">Rem</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlan.allocations
                    .filter((a) => a.allocationType === "INCOMING_PO")
                    .map((a) => (
                      <tr key={a.id}>
                        <td>{a.poNumber || "PO"} #{a.poItemNumber ?? 1}</td>
                        <td>{a.materialDescription}</td>
                        <td className="cs2-num">{a.allocatedQty} {a.uom}</td>
                        <td className="cs2-num">{a.receivedQty}</td>
                        <td className="cs2-num">{a.remainingQty}</td>
                        <td><span className={`cs2-badge ${statusBadgeTone(a.status)}`}>{a.status}</span></td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* TAB 4: INTERNAL TRANSFERS (ITO / SAP 311 / 312) (§20-§23) */}
      {tab === "transfers" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-transfers">
          <form className="cs2-card" onSubmit={handleCreateItoDraft}>
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Internal Stock Transfer Recommendation &amp; ITO Draft (SAP 311)</h3>
            </div>
            <p className="cs2-empty-note" style={{ marginBottom: 10 }}>
              Same-plant storage location transfers reuse <strong>FIVE S4 - Transfer Stock (311)</strong> via existing ITO approval. Cross-plant transfers are blocked for 311.
            </p>
            <div className="cs2-form-grid">
              <label>
                <span>Material Line</span>
                <select className="sila-input" value={itoLineId} onChange={(e) => setItoLineId(e.target.value)}>
                  {activePlan.lines.map((l) => (
                    <option key={l.id} value={l.id}>{l.materialDescription}</option>
                  ))}
                </select>
              </label>
              <label>
                <span>Source Location ID</span>
                <input className="sila-input" value={itoFromLocId} onChange={(e) => setItoFromLocId(e.target.value)} placeholder="Source Outlet/Store GUID" />
              </label>
              <label>
                <span>Destination Kitchen/Outlet ID</span>
                <input className="sila-input" value={itoToLocId} onChange={(e) => setItoToLocId(e.target.value)} placeholder="Destination GUID" />
              </label>
              <label>
                <span>Transfer Quantity</span>
                <input className="sila-input" type="number" step="0.01" value={itoQty} onChange={(e) => setItoQty(e.target.value)} />
              </label>
              <label>
                <span>Reason</span>
                <input className="sila-input" value={itoReason} onChange={(e) => setItoReason(e.target.value)} />
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 12 }}>
              <button type="submit" className="sila-btn sila-btn--primary" disabled={busy}>Create ITO Draft (Submit for Approval)</button>
            </div>

            <hr style={{ margin: "14px 0", borderColor: "#e2e8f0" }} />
            <h4 style={{ margin: "0 0 8px" }}>SAP 312 Transfer Reversal Reconciliation</h4>
            <div className="cs2-form-grid">
              <label>
                <span>ITO ID</span>
                <input className="sila-input" value={itoRevId} onChange={(e) => setItoRevId(e.target.value)} placeholder="InternalTransferOrder GUID" />
              </label>
              <label>
                <span>Reversed Qty</span>
                <input className="sila-input" type="number" step="0.01" value={itoRevQty} onChange={(e) => setItoRevQty(e.target.value)} />
              </label>
              <label>
                <span>SAP 312 Document</span>
                <input className="sila-input" value={itoRevDoc} onChange={(e) => setItoRevDoc(e.target.value)} />
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 10 }}>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy || !itoRevId}
                onClick={() =>
                  void reconcileProcurementItoReversal(activePlan.summary.id, {
                    internalTransferOrderId: itoRevId,
                    reversedQty: Number(itoRevQty),
                    reversalMaterialDocument: itoRevDoc,
                    reason: "SAP 312 reversal reconciliation",
                  })
                    .then((d) => {
                      setActivePlan(d);
                      setNotice("SAP 312 reversal reconciled; affected event requirements reopened.");
                    })
                    .catch((err) => setError(err instanceof Error ? err.message : "ITO reversal reconciliation failed."))
                }
              >
                Reconcile SAP 312 Reversal
              </button>
            </div>
          </form>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">ITO Allocations &amp; Candidates</h3>
            </div>
            <div className="cs2-table-wrap">
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>ITO #</th>
                    <th>Material</th>
                    <th>Route</th>
                    <th className="cs2-num">Qty</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlan.allocations
                    .filter((a) => a.allocationType === "PROPOSED_ITO")
                    .map((a) => (
                      <tr key={a.id}>
                        <td>{a.itoNumber || "Draft"}</td>
                        <td>{a.materialDescription}</td>
                        <td>{a.sourceLocationName || "Source"} → {a.destinationLocationName || "Dest"}</td>
                        <td className="cs2-num">{a.allocatedQty} {a.uom}</td>
                        <td><span className={`cs2-badge ${statusBadgeTone(a.status)}`}>{a.status}</span></td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* TAB 5: PURCHASE RECOMMENDATIONS & PO DRAFTS (§25, §29, §32-§38) */}
      {tab === "purchases" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-purchases">
          <form className="cs2-card" onSubmit={handleCreatePoDraft}>
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Create PO Draft (Existing Service · Never Auto-Released)</h3>
            </div>
            {activePlan.purchasingProfile && (
              <div className="cs2-banner" style={{ marginBottom: 10 }}>
                <div>
                  <strong>User Purchasing Profile Defaults:</strong> Company {activePlan.purchasingProfile.companyCode || "—"} ·
                  Plant {activePlan.purchasingProfile.plantCode || "—"} · PurchOrg {activePlan.purchasingProfile.purchasingOrganization || "—"} ·
                  Acct Cat {activePlan.purchasingProfile.accountAssignmentCategory || "—"} · Cost Center {activePlan.purchasingProfile.costCenter || "—"}
                </div>
              </div>
            )}
            <div style={{ marginBottom: 10 }}>
              <strong>Select Lines to Convert to PO Draft:</strong>
              {activePlan.lines
                .filter((l) => l.recommendedPurchaseQty > 0)
                .map((l) => (
                  <label key={l.id} className="cs2-checkbox-row" style={{ marginTop: 4 }}>
                    <input
                      type="checkbox"
                      checked={selectedPoLineIds.includes(l.id)}
                      onChange={(e) =>
                        setSelectedPoLineIds((prev) =>
                          e.target.checked ? [...prev, l.id] : prev.filter((x) => x !== l.id)
                        )
                      }
                    />
                    <span>
                      {l.materialDescription} — Rec {l.recommendedPurchaseQty} {l.purchasingUom || l.baseUom} ({l.recommendedSupplierName || "Select supplier"})
                    </span>
                  </label>
                ))}
            </div>
            <div className="cs2-form-grid">
              <label>
                <span>Supplier ID</span>
                <input className="sila-input" value={poSupplierId} onChange={(e) => setPoSupplierId(e.target.value)} placeholder="Supplier GUID" />
              </label>
              <label>
                <span>Account Assignment Override</span>
                <input className="sila-input" value={poAcctCategory} onChange={(e) => setPoAcctCategory(e.target.value)} placeholder="Blank preserves STOCK item rule" />
              </label>
              <label>
                <span>Cost Center (if expense)</span>
                <input className="sila-input" value={poCostCenter} onChange={(e) => setPoCostCenter(e.target.value)} />
              </label>
              <label>
                <span>GL Account (if expense)</span>
                <input className="sila-input" value={poGlAccount} onChange={(e) => setPoGlAccount(e.target.value)} />
              </label>
              <label>
                <span>Contract Reference</span>
                <input className="sila-input" value={poContractRef} onChange={(e) => setPoContractRef(e.target.value)} />
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 12 }}>
              <button type="submit" className="sila-btn sila-btn--primary" disabled={busy}>Create PO Draft</button>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy}
                onClick={() =>
                  void optimizeProcurementPlan(activePlan.summary.id, optMaxBudget ? Number(optMaxBudget) : null)
                    .then(setOptResult)
                    .catch((err) => setError(err instanceof Error ? err.message : "Optimization failed."))
                }
              >
                Run OR-Tools CP-SAT Optimizer
              </button>
              <input
                className="sila-input"
                style={{ width: 140 }}
                placeholder="Max budget"
                value={optMaxBudget}
                onChange={(e) => setOptMaxBudget(e.target.value)}
              />
            </div>

            {optResult && (
              <div className="cs2-banner" style={{ marginTop: 12 }}>
                <div>
                  <strong>{optResult.solverName} ({optResult.solverStatus})</strong> — Feasible: {optResult.feasible ? "Yes" : "No"} ·
                  Total Cost: {fmtNum(optResult.totalPurchaseCost)} · Excess: {fmtNum(optResult.totalExcessQty)}
                  {optResult.infeasibilityReasons.length > 0 && <div>Issues: {optResult.infeasibilityReasons.join("; ")}</div>}
                </div>
              </div>
            )}
          </form>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Multi-Event Consolidation &amp; Weekly Bucket Link</h3>
            </div>
            <div className="cs2-table-wrap">
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>Material</th>
                    <th>Supplier</th>
                    <th className="cs2-num">Combined Net</th>
                    <th className="cs2-num">Rec Purchase</th>
                    <th className="cs2-num">Excess</th>
                    <th>Event Shares</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlan.consolidatedPurchases.map((cp) => (
                    <tr key={cp.consolidationKey}>
                      <td>{cp.materialDescription}</td>
                      <td>{cp.supplierName}</td>
                      <td className="cs2-num">{cp.combinedNetQty} {cp.baseUom}</td>
                      <td className="cs2-num"><strong>{cp.recommendedPurchaseQty}</strong></td>
                      <td className="cs2-num">{cp.expectedExcessQty}</td>
                      <td>{cp.eventShares.map((s) => `${s.eventCode}:${s.allocatedQty}`).join(", ")}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="cs2-form-grid" style={{ marginTop: 12 }}>
              <label>
                <span>Line for Weekly Bucket</span>
                <select className="sila-input" value={wbLineId} onChange={(e) => setWbLineId(e.target.value)}>
                  {activePlan.lines.map((l) => (
                    <option key={l.id} value={l.id}>{l.materialDescription}</option>
                  ))}
                </select>
              </label>
              <label>
                <span>Weekly Bucket Item ID</span>
                <input className="sila-input" value={wbItemId} onChange={(e) => setWbItemId(e.target.value)} placeholder="WeeklyBucketItem GUID" />
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 8 }}>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                disabled={busy || !wbLineId || !wbItemId}
                onClick={() =>
                  void linkWeeklyBucketToProcurementLine(activePlan.summary.id, wbLineId, wbItemId)
                    .then((d) => {
                      setActivePlan(d);
                      setNotice("Linked to Weekly Bucket (duplicate independent event PO demand cleared).");
                    })
                    .catch((err) => setError(err instanceof Error ? err.message : "Weekly Bucket link failed."))
                }
              >
                Consolidate into Weekly Bucket
              </button>
            </div>
          </div>
        </div>
      )}

      {/* TAB 6: SUPPLIER COMPARISON (§26-§30) */}
      {tab === "suppliers" && activePlan && (
        <div className="cs2-card" data-testid="proc-panel-suppliers">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Supplier, Contract &amp; Catalog Price Comparison</h3>
          </div>
          {activePlan.lines.map((ln) => (
            <div key={ln.id} style={{ marginBottom: 16 }}>
              <h4 style={{ margin: "4px 0" }}>
                {ln.materialDescription} ({ln.materialCode}) — Net Uncovered: {ln.netUncoveredQty} {ln.baseUom}
              </h4>
              {ln.supplierComparisons.length === 0 ? (
                <p className="cs2-empty-note">No verified supplier contract/catalog entries found.</p>
              ) : (
                <div className="cs2-table-wrap">
                  <table className="cs2-table">
                    <thead>
                      <tr>
                        <th>Supplier</th>
                        <th>Contract</th>
                        <th>Purch UOM</th>
                        {activePlan.costsVisible && <th className="cs2-num">Base Unit Price</th>}
                        <th className="cs2-num">Pack Size</th>
                        <th className="cs2-num">MOQ</th>
                        <th className="cs2-num">Lead (d)</th>
                        <th>Est Delivery</th>
                        <th className="cs2-num">Rec Qty</th>
                        <th className="cs2-num">Excess</th>
                        {activePlan.costsVisible && <th className="cs2-num">Est Cost</th>}
                        <th>Feasible</th>
                      </tr>
                    </thead>
                    <tbody>
                      {ln.supplierComparisons.map((sc, i) => (
                        <tr key={sc.supplierId ?? i}>
                          <td><strong>{sc.supplierName}</strong> {sc.isSelected ? "★" : ""}</td>
                          <td>{sc.contractReference || "Catalog"} {sc.contractMandatory ? "(Mandatory)" : ""}</td>
                          <td>{sc.purchasingUom}</td>
                          {activePlan.costsVisible && <td className="cs2-num">{sc.currency} {fmtNum(sc.baseUnitPrice)}</td>}
                          <td className="cs2-num">{sc.packSizeQty}</td>
                          <td className="cs2-num">{sc.moqQty}</td>
                          <td className="cs2-num">{sc.leadTimeDays}</td>
                          <td>{fmtDate(sc.estimatedDeliveryDateUtc)}</td>
                          <td className="cs2-num">{sc.recommendedPurchaseQty}</td>
                          <td className="cs2-num">{sc.expectedExcessQty}</td>
                          {activePlan.costsVisible && <td className="cs2-num">{sc.currency} {fmtNum(sc.totalEstimatedCost)}</td>}
                          <td><span className={`cs2-badge ${sc.deliveryFeasible ? "cs2-badge--live" : "cs2-badge--danger"}`}>{sc.deliveryFeasible ? "Feasible" : "Late"}</span></td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* TAB 7: INTERACTIVE PROCUREMENT SIMULATOR & AI AGENT (§47-§50, §63) */}
      {tab === "simulator" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-simulator">
          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Interactive Deterministic Procurement Simulator</h3>
            </div>
            <div className="cs2-form-grid">
              <label className="cs2-slider-row">
                <span>Guest Scale Factor: <strong>{simScale.toFixed(2)}×</strong></span>
                <input type="range" min="0.5" max="2.5" step="0.05" value={simScale} onChange={(e) => setSimScale(Number(e.target.value))} />
              </label>
              <label>
                <span>Scenario Name</span>
                <input className="sila-input" value={simScenarioName} onChange={(e) => setSimScenarioName(e.target.value)} />
              </label>
              <label>
                <span>Scenario Strategy</span>
                <select className="sila-input" value={simScenarioType} onChange={(e) => setSimScenarioType(e.target.value)}>
                  <option value="CUSTOM">Custom What-If</option>
                  <option value="MAX_LOCAL_STOCK">Maximum Local Stock Utilization</option>
                  <option value="MIN_PURCHASE_COST">Minimum Purchase Cost</option>
                  <option value="EARLIEST_AVAILABILITY">Earliest Material Availability</option>
                  <option value="MIN_INTERNAL_TRANSFERS">Minimum Internal Transfers</option>
                  <option value="CONTRACT_PREFERRED">Contract-Preferred Purchasing</option>
                  <option value="SUPPLIER_DELAY">Supplier Delay Scenario</option>
                </select>
              </label>
              <label>
                <span>Local Stock Override</span>
                <input className="sila-input" type="number" value={simLocalStock} onChange={(e) => setSimLocalStock(e.target.value)} placeholder="Auto" />
              </label>
              <label>
                <span>Safety Stock Override</span>
                <input className="sila-input" type="number" value={simSafetyStock} onChange={(e) => setSimSafetyStock(e.target.value)} placeholder="Auto" />
              </label>
              <label>
                <span>Competing Res Override</span>
                <input className="sila-input" type="number" value={simCompetingRes} onChange={(e) => setSimCompetingRes(e.target.value)} placeholder="Auto" />
              </label>
              <label>
                <span>Incoming PO Qty Override</span>
                <input className="sila-input" type="number" value={simIncomingPoQty} onChange={(e) => setSimIncomingPoQty(e.target.value)} placeholder="Auto" />
              </label>
              <label>
                <span>Incoming PO Delay (days)</span>
                <input className="sila-input" type="number" value={simIncomingDelayDays} onChange={(e) => setSimIncomingDelayDays(e.target.value)} />
              </label>
              <label>
                <span>Transferable Stock Override</span>
                <input className="sila-input" type="number" value={simTransferQty} onChange={(e) => setSimTransferQty(e.target.value)} placeholder="Auto" />
              </label>
              <label>
                <span>Pack Size / MOQ / Price</span>
                <div className="cs2-actions-row">
                  <input className="sila-input" style={{ width: 70 }} placeholder="Pack" value={simPackSize} onChange={(e) => setSimPackSize(e.target.value)} />
                  <input className="sila-input" style={{ width: 70 }} placeholder="MOQ" value={simMoq} onChange={(e) => setSimMoq(e.target.value)} />
                  <input className="sila-input" style={{ width: 80 }} placeholder="Price" value={simSupplierPrice} onChange={(e) => setSimSupplierPrice(e.target.value)} />
                </div>
              </label>
              <label>
                <span>Supplier Lead Time (days)</span>
                <input className="sila-input" type="number" value={simLeadDays} onChange={(e) => setSimLeadDays(e.target.value)} placeholder="Auto" />
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 12 }}>
              <button type="button" className="sila-btn sila-btn--primary" disabled={busy} onClick={() => void handleRunSimulator(false)}>
                Run Deterministic Simulation
              </button>
              <button type="button" className="sila-btn sila-btn--secondary" disabled={busy} onClick={() => void handleRunSimulator(true)}>
                Save Scenario Comparison
              </button>
            </div>
            {simResult && (
              <div className="cs2-kpi-grid" style={{ marginTop: 12 }}>
                <div className="cs2-kpi"><span className="cs2-kpi__label">Gross Req</span><span className="cs2-kpi__value">{simResult.grossRequirementQty}</span></div>
                <div className="cs2-kpi"><span className="cs2-kpi__label">Net Purchase</span><span className="cs2-kpi__value">{simResult.netPurchaseRequirementQty}</span></div>
                <div className="cs2-kpi"><span className="cs2-kpi__label">Rec Purchase ({simResult.numberOfPacks} pks)</span><span className="cs2-kpi__value">{simResult.recommendedPurchaseQty}</span></div>
                <div className="cs2-kpi"><span className="cs2-kpi__label">Excess / Readiness</span><span className="cs2-kpi__value">{simResult.expectedExcessQty} / {simResult.readinessImpactPercent}%</span></div>
              </div>
            )}
          </div>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">AI Procurement Intelligence Agent (Advisory Only)</h3>
            </div>
            <div className="cs2-actions-row" style={{ marginBottom: 8 }}>
              {AI_PROCUREMENT_PROMPTS.map((q) => (
                <button key={q} type="button" className="cs2-mode-btn" onClick={() => setAiQuestion(q)}>
                  {q}
                </button>
              ))}
            </div>
            <div className="cs2-actions-row">
              <input
                className="sila-input"
                style={{ flex: 1 }}
                value={aiQuestion}
                onChange={(e) => setAiQuestion(e.target.value)}
                placeholder="Ask about shortages, ITO alternatives, MOQ/pack excess, or supplier lead times..."
              />
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                disabled={busy || !aiQuestion.trim()}
                onClick={() =>
                  void analyzeProcurementWithAi(activePlan.summary.id, aiQuestion)
                    .then(setAiResponse)
                    .catch((err) => setError(err instanceof Error ? err.message : "AI analysis failed."))
                }
              >
                Ask AI Agent
              </button>
            </div>
            {aiResponse && (
              <div className="cs2-banner" style={{ marginTop: 12 }}>
                <div>
                  <strong>Provider: {aiResponse.providerUsed}</strong> (Advisory Only — cannot post SAP or DB transactions)
                  <p style={{ margin: "6px 0" }}>{aiResponse.answer}</p>
                  <div className="cs2-empty-note">
                    Materials: {aiResponse.referencedMaterials.join(", ") || "—"} | Suppliers: {aiResponse.referencedSuppliers.join(", ") || "—"}
                  </div>
                </div>
              </div>
            )}

            {activePlan.scenarios.length > 0 && (
              <div style={{ marginTop: 14 }}>
                <h4>Saved Scenario Comparisons ({activePlan.scenarios.length})</h4>
                <ul>
                  {activePlan.scenarios.map((sc) => (
                    <li key={sc.id}>
                      <strong>{sc.name}</strong> ({sc.scenarioType}) — Readiness: {sc.projectedReadinessPercent}%
                      {activePlan.costsVisible && sc.estimatedPurchaseValue != null ? ` · Est Cost: ${fmtNum(sc.estimatedPurchaseValue)}` : ""}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 8: DELIVERY TRACKING & TIME-PHASED LEDGER (§8, §41) */}
      {tab === "delivery" && activePlan && (
        <div className="cs2-card" data-testid="proc-panel-delivery">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Event-to-PO / ITO Traceability &amp; Time-Phased Supply/Demand Ledger</h3>
          </div>
          <div className="cs2-table-wrap">
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>Timestamp (UTC)</th>
                  <th>Material</th>
                  <th>Event Type</th>
                  <th>Reference</th>
                  <th className="cs2-num">Delta</th>
                  <th className="cs2-num">Running Projected</th>
                  <th>Confirmed</th>
                  <th>Notes</th>
                </tr>
              </thead>
              <tbody>
                {activePlan.supplyDemandLedger.map((le, idx) => (
                  <tr key={idx}>
                    <td>{fmtDate(le.timestampUtc)}</td>
                    <td>{le.materialCode}</td>
                    <td>{le.eventType}</td>
                    <td>{le.sourceReference}</td>
                    <td className="cs2-num">{le.deltaQty}</td>
                    <td className="cs2-num">{le.runningProjectedQty}</td>
                    <td>{le.confirmed ? "Yes" : "Proposed"}</td>
                    <td>{le.notes}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 9: EXCEPTIONS, CHANGE IMPACT & CANCELLATION (§51-§53) */}
      {tab === "exceptions" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-exceptions">
          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Delivery Risks &amp; Procurement Exceptions ({activePlan.exceptions.length})</h3>
            </div>
            <div className="cs2-table-wrap">
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>Severity</th>
                    <th>Type</th>
                    <th>Material</th>
                    <th>Message</th>
                    <th>Status</th>
                    <th>Action</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlan.exceptions.map((ex) => (
                    <tr key={ex.id}>
                      <td><span className={`cs2-badge ${statusBadgeTone(ex.severity)}`}>{ex.severity}</span></td>
                      <td>{ex.exceptionType}</td>
                      <td>{ex.materialDescription || ex.materialCode || "—"}</td>
                      <td>{ex.message}</td>
                      <td>{ex.resolutionStatus}</td>
                      <td>
                        {ex.resolutionStatus === "OPEN" && (
                          <button
                            type="button"
                            className="sila-btn sila-btn--sm sila-btn--secondary"
                            onClick={() =>
                              void resolveProcurementException(activePlan.summary.id, ex.id, "RESOLVED", "Reviewed in Procurement Studio")
                                .then(() => selectPlan(activePlan.summary.id))
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
          </div>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Event Change Impact &amp; Cancellation Control</h3>
            </div>
            <div className="cs2-form-grid">
              <label>
                <span>Revised MRP Run for Change Impact</span>
                <select className="sila-input" value={changeImpactMrpId} onChange={(e) => setChangeImpactMrpId(e.target.value)}>
                  <option value="">-- Select Newer MRP Revision --</option>
                  {mrpRuns.map((r) => (
                    <option key={r.id} value={r.id}>{r.mrpCode} v{r.versionNumber} · {r.name}</option>
                  ))}
                </select>
              </label>
            </div>
            <div className="cs2-actions-row" style={{ marginTop: 8 }}>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy || !changeImpactMrpId}
                onClick={() =>
                  void handleProcurementChangeImpact(activePlan.summary.id, changeImpactMrpId)
                    .then((d) => {
                      setActivePlan(d);
                      setNotice("Controlled change impact amendment calculated (released POs/posted ITOs untouched).");
                    })
                    .catch((err) => setError(err instanceof Error ? err.message : "Change impact failed."))
                }
              >
                Analyze &amp; Apply Change Impact
              </button>
            </div>
            <hr style={{ margin: "14px 0", borderColor: "#e2e8f0" }} />
            <label>
              <span>Event Cancellation Reason</span>
              <input className="sila-input" value={cancelReason} onChange={(e) => setCancelReason(e.target.value)} placeholder="e.g., Client postponed gala" />
            </label>
            <div className="cs2-actions-row" style={{ marginTop: 8 }}>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy || !cancelReason.trim()}
                onClick={() =>
                  void cancelProcurementPlan(activePlan.summary.id, cancelReason)
                    .then((d) => {
                      setActivePlan(d);
                      setNotice("Active reservations & incoming allocations released; released POs/posted ITOs preserved.");
                    })
                    .catch((err) => setError(err instanceof Error ? err.message : "Cancellation failed."))
                }
              >
                Cancel Event Procurement Plan
              </button>
            </div>
          </div>
        </div>
      )}

      {/* TAB 10: APPROVALS (§39-§40) */}
      {tab === "approvals" && activePlan && (
        <div className="cs2-card" data-testid="proc-panel-approvals">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Procurement Approval Package &amp; Workflow</h3>
            <div className="cs2-actions-row">
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                disabled={busy}
                onClick={() =>
                  void submitProcurementPlanForApproval(activePlan.summary.id)
                    .then((d) => {
                      setActivePlan(d);
                      setNotice("Procurement plan submitted for approval.");
                    })
                    .catch((err) => setError(err instanceof Error ? err.message : "Submit approval failed."))
                }
              >
                Submit for Approval
              </button>
            </div>
          </div>
          <div className="cs2-actions-row" style={{ marginBottom: 12 }}>
            <input
              className="sila-input"
              style={{ minWidth: 260 }}
              placeholder="Approval / return / reject comment"
              value={approvalComment}
              onChange={(e) => setApprovalComment(e.target.value)}
            />
            <button
              type="button"
              className="sila-btn sila-btn--primary"
              disabled={busy}
              onClick={() =>
                void decideProcurementPlanApproval(activePlan.summary.id, "APPROVE", approvalComment)
                  .then((d) => {
                    setActivePlan(d);
                    setNotice("Procurement plan approved.");
                  })
                  .catch((err) => setError(err instanceof Error ? err.message : "Approval failed."))
              }
            >
              Approve
            </button>
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              disabled={busy}
              onClick={() =>
                void decideProcurementPlanApproval(activePlan.summary.id, "RETURN", approvalComment)
                  .then((d) => {
                    setActivePlan(d);
                    setNotice("Returned for correction.");
                  })
                  .catch((err) => setError(err instanceof Error ? err.message : "Return failed."))
              }
            >
              Return
            </button>
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              disabled={busy}
              onClick={() =>
                void decideProcurementPlanApproval(activePlan.summary.id, "REJECT", approvalComment)
                  .then((d) => {
                    setActivePlan(d);
                    setNotice("Rejected.");
                  })
                  .catch((err) => setError(err instanceof Error ? err.message : "Reject failed."))
              }
            >
              Reject
            </button>
          </div>
          <div className="cs2-table-wrap">
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>Step</th>
                  <th>Approver ID</th>
                  <th>Status</th>
                  <th>Comment</th>
                  <th>Decided On</th>
                </tr>
              </thead>
              <tbody>
                {activePlan.approvalSteps.map((s) => (
                  <tr key={s.stepOrder}>
                    <td>{s.stepOrder}</td>
                    <td>{s.approverUserId}</td>
                    <td><span className={`cs2-badge ${statusBadgeTone(s.status)}`}>{s.status}</span></td>
                    <td>{s.comment || "—"}</td>
                    <td>{fmtDate(s.decidedOn)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* TAB 11: REPORT STUDIO (§55 — 34 REPORTS) */}
      {tab === "reports" && activePlan && (
        <div className="cs2-card" data-testid="proc-panel-reports">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Procurement Report Studio ({reportCatalog.length} Reports)</h3>
            <div className="cs2-actions-row">
              <select className="sila-input" value={selectedReportCode} onChange={(e) => setSelectedReportCode(e.target.value)}>
                {reportCatalog.map((r) => (
                  <option key={r.reportCode} value={r.reportCode}>
                    [{r.category}] {r.title}
                  </option>
                ))}
              </select>
              <button type="button" className="sila-btn sila-btn--primary" disabled={busy} onClick={() => void handleLoadReport(selectedReportCode)}>
                Preview Report
              </button>
              {activeReport && (
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary"
                  onClick={() => downloadCsv(`${activeReport.reportCode}_${activePlan.summary.planCode}.csv`, activeReport.csvContent)}
                >
                  Download Excel/CSV
                </button>
              )}
            </div>
          </div>
          {activeReport && (
            <div className="cs2-table-wrap">
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
          )}
        </div>
      )}

      {/* TAB 12: HISTORY, TRACKER & PHASE 3A HANDOVER (§57, §66) */}
      {tab === "history" && activePlan && (
        <div className="cs2-grid-2" data-testid="proc-panel-history">
          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Transaction Tracker &amp; Audit History</h3>
            </div>
            <div className="cs2-table-wrap">
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>Action</th>
                    <th>Details</th>
                    <th>Timestamp</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlan.history.map((h, i) => (
                    <tr key={i}>
                      <td><span className="cs2-badge cs2-badge--indigo">{h.action}</span></td>
                      <td>{h.comment}</td>
                      <td>{fmtDate(h.dateCreated)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Phase 3A Event P&amp;L Handover Contract</h3>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={busy} onClick={handleLoadPhase3A}>
                Load Phase 3A Handover
              </button>
            </div>
            {phase3a ? (
              <div>
                <p className="cs2-empty-note">{phase3a.note}</p>
                <div className="cs2-kpi-grid" style={{ marginTop: 10 }}>
                  <div className="cs2-kpi"><span className="cs2-kpi__label">Contract</span><span className="cs2-kpi__value">{phase3a.contractVersion}</span></div>
                  <div className="cs2-kpi"><span className="cs2-kpi__label">Est Req Cost</span><span className="cs2-kpi__value">{fmtNum(phase3a.estimatedMaterialRequirementCost)}</span></div>
                  <div className="cs2-kpi"><span className="cs2-kpi__label">Est New Purchase</span><span className="cs2-kpi__value">{fmtNum(phase3a.estimatedNewPurchaseExpenditure)}</span></div>
                  <div className="cs2-kpi"><span className="cs2-kpi__label">Committed PO</span><span className="cs2-kpi__value">{fmtNum(phase3a.approvedProcurementCommitment)}</span></div>
                  <div className="cs2-kpi"><span className="cs2-kpi__label">Received Value</span><span className="cs2-kpi__value">{fmtNum(phase3a.actualReceivedMaterialValue)}</span></div>
                </div>
              </div>
            ) : (
              <p className="cs2-empty-note">Click &quot;Load Phase 3A Handover&quot; to inspect version-controlled event, MRP, reservation, ITO, PO, GRN, and cost object references.</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default CulinaryProcurementStudioWorkspace;
