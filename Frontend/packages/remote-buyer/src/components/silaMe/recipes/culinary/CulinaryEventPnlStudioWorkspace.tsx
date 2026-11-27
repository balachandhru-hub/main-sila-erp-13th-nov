import React, { useCallback, useEffect, useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  addEventExpenseLine,
  addEventRevenueLine,
  analyzeFinancialPlanWithAi,
  analyzeMaterialPriceImpact,
  approveEventExpenseLine,
  createEventFinancialPlan,
  decideFinancialPlanApproval,
  exportFinancialReport,
  getEventFinancialPlan,
  getFinancialCancellationExposure,
  getFinancialConfig,
  getFinancialDashboard,
  getFinancialReportCatalog,
  listEventFinancialPlans,
  promoteFinancialScenario,
  recalculateEventFinancialPlan,
  resolveFinancialException,
  saveFinancialConfig,
  simulateEventFinancials,
  submitFinancialPlanForApproval,
  triggerFinancialChangeImpact,
  type SilaCulinaryEventFinancialPlanDetail,
  type SilaCulinaryEventFinancialPlanSummary,
  type SilaCulinaryFinancialConfig,
  type SilaCulinaryFinancialDashboard,
  type SilaCulinaryMaterialPriceImpact,
  type SilaCulinarySimulateFinancialRequest,
  type SilaCulinarySimulateFinancialResult,
} from "../../../../api/silaMe/silaCulinaryFinancialApi";
import { listCulinaryEvents, type SilaCulinaryEvent } from "../../../../api/silaMe/silaCulinaryEventMenuApi";
import {
  calculateEventPnl,
  DEMO_EVENT_PNL_BASE,
  type EventPnlInput,
} from "./culinarySimulatorsMath";
import "./CulinarySuite.css";

type PnlTab =
  | "overview"
  | "revenue"
  | "food-cost"
  | "beverage-cost"
  | "manpower"
  | "expenses"
  | "procurement"
  | "allocation"
  | "estimated-pnl"
  | "forecast-pnl"
  | "actual-pnl"
  | "variance"
  | "simulator"
  | "ai"
  | "approvals"
  | "reports"
  | "history";

const TAB_ITEMS: Array<{ id: PnlTab; label: string }> = [
  { id: "overview", label: "Financial Overview" },
  { id: "revenue", label: "Revenue" },
  { id: "food-cost", label: "Food Cost" },
  { id: "beverage-cost", label: "Beverage Cost" },
  { id: "manpower", label: "Manpower" },
  { id: "expenses", label: "Expenses" },
  { id: "procurement", label: "Procurement Commitments" },
  { id: "allocation", label: "Cost Allocation" },
  { id: "estimated-pnl", label: "Estimated P&L" },
  { id: "forecast-pnl", label: "Forecast P&L" },
  { id: "actual-pnl", label: "Actual P&L" },
  { id: "variance", label: "Variance Analysis" },
  { id: "simulator", label: "Financial Simulator" },
  { id: "ai", label: "AI Recommendations" },
  { id: "approvals", label: "Approvals" },
  { id: "reports", label: "Reports" },
  { id: "history", label: "History" },
];

const fmtNum = (val: number | null | undefined, ccy?: string) => {
  if (val === null || val === undefined || Number.isNaN(val)) return "N/A";
  const formatted = val.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
  return ccy ? `${ccy} ${formatted}` : formatted;
};

const fmtPct = (val: number | null | undefined) =>
  val === null || val === undefined || Number.isNaN(val) ? "N/A" : `${val.toFixed(2)}%`;

const CulinaryEventPnlStudioWorkspace: React.FC = () => {
  const [tab, setTab] = useState<PnlTab>("overview");
  const [dashboard, setDashboard] = useState<SilaCulinaryFinancialDashboard | null>(null);
  const [plans, setPlans] = useState<SilaCulinaryEventFinancialPlanSummary[]>([]);
  const [events, setEvents] = useState<SilaCulinaryEvent[]>([]);
  const [selectedPlanId, setSelectedPlanId] = useState<string>("");
  const [detail, setDetail] = useState<SilaCulinaryEventFinancialPlanDetail | null>(null);
  const [config, setConfig] = useState<SilaCulinaryFinancialConfig | null>(null);
  const [showConfigModal, setShowConfigModal] = useState(false);

  const [loading, setLoading] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // Create plan inputs
  const [createEventId, setCreateEventId] = useState<string>("");
  const [createName, setCreateName] = useState<string>("Grand Corporate Gala");
  const [createGuests, setCreateGuests] = useState<number>(500);
  const [createPrice, setCreatePrice] = useState<number>(350);

  // Revenue line form
  const [revCat, setRevCat] = useState("ADDITIONAL_GUEST");
  const [revDesc, setRevDesc] = useState("");
  const [revQty, setRevQty] = useState(20);
  const [revPrice, setRevPrice] = useState(350);
  const [revDisc, setRevDisc] = useState(0);

  // Expense line form
  const [expCat, setExpCat] = useState("MANPOWER");
  const [expDesc, setExpDesc] = useState("");
  const [expBehavior, setExpBehavior] = useState("FIXED");
  const [expQty, setExpQty] = useState(1);
  const [expRate, setExpRate] = useState(1500);
  const [expStaff, setExpStaff] = useState(10);
  const [expHours, setExpHours] = useState(8);
  const [expHourlyRate, setExpHourlyRate] = useState(35);
  const [expPoolId, setExpPoolId] = useState("");
  const [expActual, setExpActual] = useState(false);

  // Simulator state (deterministic offline fallback + server persist)
  const [simReq, setSimReq] = useState<SilaCulinarySimulateFinancialRequest>({
    plannedGuests: 500,
    guaranteedGuests: 500,
    sellingPricePerGuest: 350,
    fixedEventRevenue: 0,
    additionalGuestRevenue: 0,
    discountPercent: 0,
    foodCostPerGuest: 90,
    beverageCostPerGuest: 25,
    ingredientPriceChangePercent: 0,
    manpowerCost: 15000,
    equipmentCost: 5000,
    decorationCost: 10000,
    externalServiceCost: 0,
    otherDirectCost: 5000,
    allocatedOverhead: 0,
    targetContributionMarginPercent: 40,
    saveScenario: false,
    scenarioName: "Custom Simulation",
    scenarioType: "CUSTOM",
  });
  const [simServerResult, setSimServerResult] = useState<SilaCulinarySimulateFinancialResult | null>(null);

  // Local deterministic preview for instant slider responsiveness (§52: no LLM per slider change)
  const instantSim = useMemo(() => {
    const input: EventPnlInput = {
      ...DEMO_EVENT_PNL_BASE,
      guestCount: simReq.plannedGuests,
      sellingPricePerGuestAed: simReq.sellingPricePerGuest,
      discountPercent: simReq.discountPercent,
      foodCostPerGuestAed: simReq.foodCostPerGuest * (1 + simReq.ingredientPriceChangePercent / 100),
      beverageCostPerGuestAed: simReq.beverageCostPerGuest,
      manpowerCostAed: simReq.manpowerCost,
      decorationCostAed: simReq.decorationCost,
      equipmentCostAed: simReq.equipmentCost,
      otherExpensesAed: simReq.otherDirectCost + simReq.externalServiceCost + simReq.allocatedOverhead,
      targetMarginPercent: simReq.targetContributionMarginPercent,
    };
    return calculateEventPnl(input);
  }, [simReq]);

  // Material price impact state
  const [impactMatCode, setImpactMatCode] = useState("MAT-SALMON");
  const [impactNewPrice, setImpactNewPrice] = useState(100);
  const [impactResult, setImpactResult] = useState<SilaCulinaryMaterialPriceImpact | null>(null);

  // AI Agent state
  const [aiQuestion, setAiQuestion] = useState("How profitable is this event and what drives the margin?");
  const [aiResponse, setAiResponse] = useState<{
    answer: string;
    providerUsed: string;
    groundedFacts: string[];
    warnings: string[];
    promptInjectionBlocked: boolean;
  } | null>(null);

  // Approval / Change / Cancellation state
  const [approvalComment, setApprovalComment] = useState("");
  const [changeGuests, setChangeGuests] = useState(550);
  const [cancelExposure, setCancelExposure] = useState<Record<string, unknown> | null>(null);

  // Reports state
  const [catalog, setCatalog] = useState<Array<{ reportCode: string; title: string; category: string; description: string }>>([]);
  const [reportFilter, setReportFilter] = useState("");
  const [reportPreview, setReportPreview] = useState<{
    reportCode: string;
    title: string;
    fileName: string;
    csvContent: string;
    previewRows: Array<Record<string, string>>;
  } | null>(null);

  const loadAll = useCallback(async (preferPlanId?: string) => {
    setLoading(true);
    setError(null);
    try {
      const [dash, list, evtList, cfg, cat] = await Promise.all([
        getFinancialDashboard().catch(() => null),
        listEventFinancialPlans({ includeSimulations: false }).catch(() => []),
        listCulinaryEvents().catch(() => []),
        getFinancialConfig().catch(() => null),
        getFinancialReportCatalog().catch(() => []),
      ]);
      setDashboard(dash);
      setPlans(list);
      setEvents(evtList);
      setConfig(cfg);
      setCatalog(cat);

      const targetId = preferPlanId || selectedPlanId || list[0]?.id || "";
      if (targetId) {
        setSelectedPlanId(targetId);
        const d = await getEventFinancialPlan(targetId);
        setDetail(d);
      } else {
        setDetail(null);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setLoading(false);
    }
  }, [selectedPlanId]);

  useEffect(() => {
    void loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSelectPlan = async (planId: string) => {
    setSelectedPlanId(planId);
    if (!planId) return;
    setBusy(true);
    setError(null);
    try {
      const d = await getEventFinancialPlan(planId);
      setDetail(d);
      setSimServerResult(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleCreateDemoGala = async () => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      const created = await createEventFinancialPlan({
        name: "Grand Corporate Gala",
        plannedGuests: 500,
        guaranteedGuests: 500,
        sellingPricePerGuest: 350,
        foodCostPerGuest: 90,
        beverageCostPerGuest: 25,
        originalBudget: 105000,
        isDemo: true,
      });
      setNotice(`Created isolated demonstration baseline ${created.summary.planCode} (Grand Corporate Gala).`);
      await loadAll(created.summary.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleCreatePlan = async () => {
    setBusy(true);
    setError(null);
    setNotice(null);
    try {
      const created = await createEventFinancialPlan({
        eventId: createEventId || undefined,
        name: createName || undefined,
        plannedGuests: createGuests,
        guaranteedGuests: createGuests,
        sellingPricePerGuest: createPrice,
      });
      setNotice(`Created Event Financial Plan ${created.summary.planCode} v${created.summary.versionNumber}.`);
      await loadAll(created.summary.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleRecalculate = async () => {
    if (!detail) return;
    setBusy(true);
    setError(null);
    try {
      const updated = await recalculateEventFinancialPlan(detail.summary.id);
      setDetail(updated);
      setNotice(`Recalculated ${updated.summary.planCode} deterministic snapshot.`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleAddRevenue = async () => {
    if (!detail || !revDesc.trim()) return;
    setBusy(true);
    setError(null);
    try {
      const updated = await addEventRevenueLine(detail.summary.id, {
        category: revCat,
        description: revDesc.trim(),
        quantity: revQty,
        unitPrice: revPrice,
        discountAmount: revDisc,
        currency: detail.summary.reportingCurrency,
      });
      setDetail(updated);
      setRevDesc("");
      setNotice("Revenue line added and P&L recalculated.");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleAddExpense = async () => {
    if (!detail || !expDesc.trim()) return;
    setBusy(true);
    setError(null);
    try {
      const updated = await addEventExpenseLine(detail.summary.id, {
        category: expCat,
        description: expDesc.trim(),
        costBehavior: expBehavior,
        quantity: expQty,
        unitRate: expRate,
        currency: detail.summary.reportingCurrency,
        staffCount: expCat === "MANPOWER" ? expStaff : undefined,
        plannedHours: expCat === "MANPOWER" ? expHours : undefined,
        hourlyRate: expCat === "MANPOWER" ? expHourlyRate : undefined,
        sourceCostPoolId: expPoolId.trim() || undefined,
        markAsActualVerified: expActual,
      });
      setDetail(updated);
      setExpDesc("");
      setExpPoolId("");
      setNotice("Expense line added and P&L recalculated.");
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleRunServerSimulation = async (saveScenario: boolean) => {
    if (!detail) return;
    setBusy(true);
    setError(null);
    try {
      const res = await simulateEventFinancials(detail.summary.id, { ...simReq, saveScenario });
      setSimServerResult(res);
      if (saveScenario) {
        const refreshed = await getEventFinancialPlan(detail.summary.id);
        setDetail(refreshed);
        setNotice(`Saved financial scenario "${simReq.scenarioName || "Custom"}" without altering approved baseline.`);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handlePromoteScenario = async (scenarioId: string) => {
    if (!detail) return;
    setBusy(true);
    setError(null);
    try {
      const rev = await promoteFinancialScenario(detail.summary.id, scenarioId);
      setNotice(`Promoted scenario into new financial revision ${rev.summary.planCode} v${rev.summary.versionNumber}.`);
      await loadAll(rev.summary.id);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const handleExportReport = async (reportCode: string) => {
    if (!detail) return;
    setBusy(true);
    setError(null);
    try {
      const res = await exportFinancialReport(detail.summary.id, reportCode);
      setReportPreview(res);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setBusy(false);
    }
  };

  const downloadCsv = (fileName: string, csvContent: string) => {
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
  };

  const summary = detail?.summary;
  const ccy = summary?.reportingCurrency || config?.reportingCurrency || "AED";

  const waterfallChartData = useMemo(() => {
    if (detail?.waterfall?.length) {
      return detail.waterfall.map((w) => ({
        name: w.label,
        value: Math.abs(w.amount),
        raw: w.amount,
        kind: w.kind,
      }));
    }
    return [
      { name: "Net Revenue", value: instantSim.netRevenueAed, raw: instantSim.netRevenueAed, kind: "REVENUE" },
      { name: "Food", value: instantSim.foodCostAed, raw: -instantSim.foodCostAed, kind: "COST" },
      { name: "Beverage", value: instantSim.beverageCostAed, raw: -instantSim.beverageCostAed, kind: "COST" },
      { name: "Fixed & Staff", value: instantSim.totalFixedCostAed, raw: -instantSim.totalFixedCostAed, kind: "COST" },
      { name: "Contribution", value: Math.abs(instantSim.eventContributionAed), raw: instantSim.eventContributionAed, kind: "CONTRIBUTION" },
    ];
  }, [detail, instantSim]);

  const filteredCatalog = useMemo(() => {
    const q = reportFilter.trim().toLowerCase();
    if (!q) return catalog;
    return catalog.filter(
      (r) =>
        r.title.toLowerCase().includes(q) ||
        r.reportCode.toLowerCase().includes(q) ||
        r.category.toLowerCase().includes(q)
    );
  }, [catalog, reportFilter]);

  return (
    <div className="cs2-shell" data-testid="culinary-pnl-workspace">
      {/* Top Studio Header */}
      <div className="cs2-card" style={{ padding: 18 }}>
        <div style={{ display: "flex", flexWrap: "wrap", justifyContent: "space-between", alignItems: "flex-start", gap: 14 }}>
          <div>
            <div style={{ display: "flex", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
              <span className="cs2-brand-pill">Phase 3A Live</span>
              <span style={{ fontSize: 12, fontWeight: 600, color: "#475569" }}>
                Zero Live SAP Postings &middot; Zero GL/Invoice Auto-Creation &middot; Deterministic Decimal Engine
              </span>
            </div>
            <h2 style={{ margin: "6px 0 4px", fontSize: 20, fontWeight: 700, color: "#0f172a" }}>
              {summary ? `${summary.eventName || summary.name} — Event P&L & Financial Intelligence` : "Event P&L & Financial Intelligence Studio"}
            </h2>
            {summary ? (
              <div style={{ fontSize: 12.5, color: "#475569", display: "flex", flexWrap: "wrap", gap: 12 }}>
                <span>Plan: <strong>{summary.planCode}</strong> (v{summary.versionNumber})</span>
                <span>Property: <strong>{summary.propertyName || "Main Property"}</strong></span>
                <span>Venue: <strong>{summary.venueName || "Grand Ballroom"}</strong></span>
                <span>Guests: <strong>{summary.plannedGuests}</strong> planned / <strong>{summary.billableGuests}</strong> billable</span>
                <span>Status: <strong>{summary.status}</strong></span>
                <span>Actuals: <strong>{summary.actualIsProvisional ? "PROVISIONAL (Phase 3A)" : "VERIFIED"}</strong></span>
              </div>
            ) : (
              <div style={{ fontSize: 13, color: "#64748b" }}>
                Create an Event Financial Plan from an existing Event or launch the isolated Grand Corporate Gala demonstration baseline.
              </div>
            )}
          </div>

          <div style={{ display: "flex", flexWrap: "wrap", gap: 8, alignItems: "center" }}>
            <select
              className="cs2-input"
              style={{ minWidth: 240 }}
              value={selectedPlanId}
              onChange={(e) => void handleSelectPlan(e.target.value)}
            >
              <option value="">Select Event Financial Plan...</option>
              {plans.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.planCode} &middot; {p.name} (v{p.versionNumber}, {p.status})
                </option>
              ))}
            </select>
            <button type="button" className="cs2-btn cs2-btn-secondary" disabled={busy || loading} onClick={() => void loadAll()}>
              Refresh
            </button>
            {summary && (
              <button type="button" className="cs2-btn cs2-btn-secondary" disabled={busy} onClick={() => void handleRecalculate()}>
                Recalculate
              </button>
            )}
            <button type="button" className="cs2-btn cs2-btn-secondary" onClick={() => setShowConfigModal((v) => !v)}>
              Financial Config &amp; FX
            </button>
            <button type="button" className="cs2-btn cs2-btn-primary" disabled={busy} onClick={() => void handleCreateDemoGala()}>
              Launch Grand Corporate Gala Demo
            </button>
          </div>
        </div>

        {/* Create new financial plan bar */}
        <div style={{ marginTop: 14, paddingTop: 12, borderTop: "1px solid #e2e8f0", display: "flex", flexWrap: "wrap", gap: 8, alignItems: "center" }}>
          <span style={{ fontSize: 12, fontWeight: 700, color: "#334155" }}>New Plan:</span>
          <select className="cs2-input" style={{ minWidth: 200 }} value={createEventId} onChange={(e) => setCreateEventId(e.target.value)}>
            <option value="">Standalone / Custom Event</option>
            {events.map((ev) => (
              <option key={ev.id} value={ev.id}>
                {ev.eventCode} &middot; {ev.name} ({ev.expectedGuests} guests)
              </option>
            ))}
          </select>
          <input className="cs2-input" placeholder="Plan Name" value={createName} onChange={(e) => setCreateName(e.target.value)} style={{ width: 190 }} />
          <input className="cs2-input" type="number" min={1} placeholder="Guests" value={createGuests} onChange={(e) => setCreateGuests(Number(e.target.value) || 1)} style={{ width: 95 }} />
          <input className="cs2-input" type="number" min={0} placeholder="Price/Guest" value={createPrice} onChange={(e) => setCreatePrice(Number(e.target.value) || 0)} style={{ width: 110 }} />
          <button type="button" className="cs2-btn cs2-btn-primary" disabled={busy} onClick={() => void handleCreatePlan()}>
            Create Financial Plan
          </button>
        </div>

        {error && (
          <div style={{ marginTop: 10, padding: "10px 12px", borderRadius: 8, background: "#fef2f2", border: "1px solid #fecaca", color: "#991b1b", fontSize: 13 }}>
            {error}
          </div>
        )}
        {notice && (
          <div style={{ marginTop: 10, padding: "10px 12px", borderRadius: 8, background: "#ecfdf5", border: "1px solid #a7f3d0", color: "#065f46", fontSize: 13 }}>
            {notice}
          </div>
        )}
      </div>

      {/* Config & FX Drawer */}
      {showConfigModal && config && (
        <div className="cs2-card" style={{ padding: 16, background: "#f8fafc" }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 10 }}>
            <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Tenant Financial Configuration &amp; FX Rates</h3>
            <button type="button" className="cs2-btn cs2-btn-secondary" onClick={() => setShowConfigModal(false)}>Close</button>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(180px, 1fr))", gap: 10 }}>
            <label style={{ fontSize: 12 }}>
              Reporting Currency
              <input className="cs2-input" value={config.reportingCurrency} onChange={(e) => setConfig({ ...config, reportingCurrency: e.target.value.toUpperCase() })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Target Food Cost %
              <input className="cs2-input" type="number" value={config.targetFoodCostPercent} onChange={(e) => setConfig({ ...config, targetFoodCostPercent: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Target Beverage Cost %
              <input className="cs2-input" type="number" value={config.targetBeverageCostPercent} onChange={(e) => setConfig({ ...config, targetBeverageCostPercent: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Target Margin %
              <input className="cs2-input" type="number" value={config.targetEventContributionMarginPercent} onChange={(e) => setConfig({ ...config, targetEventContributionMarginPercent: Number(e.target.value) })} />
            </label>
          </div>
          <div style={{ marginTop: 10, fontSize: 12, color: "#475569" }}>
            Active FX Pairs: {config.exchangeRates.map((r) => `${r.sourceCurrency}->${r.targetCurrency}: ${r.rate}`).join(" | ")}
          </div>
          <div style={{ marginTop: 10 }}>
            <button
              type="button"
              className="cs2-btn cs2-btn-primary"
              disabled={busy}
              onClick={async () => {
                setBusy(true);
                try {
                  const saved = await saveFinancialConfig(config);
                  setConfig(saved);
                  setNotice("Financial configuration saved.");
                } catch (err) {
                  setError(err instanceof Error ? err.message : String(err));
                } finally {
                  setBusy(false);
                }
              }}
            >
              Save Tenant Financial Settings
            </button>
          </div>
        </div>
      )}

      {/* 12 KPI Cards (§50) */}
      <div className="cs2-kpi-grid" style={{ gridTemplateColumns: "repeat(auto-fit, minmax(175px, 1fr))" }}>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Net Event Revenue</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.netRevenue, ccy) : fmtNum(instantSim.netRevenueAed, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Estimated Total Cost</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.estimatedTotalCost, ccy) : fmtNum(instantSim.totalCostAed, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Procurement Commitments</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.attributableProcurementCommitment, ccy) : fmtNum(0, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Forecast-at-Completion</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.forecastAtCompletionCost, ccy) : fmtNum(instantSim.totalCostAed, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Actual Verified Cost</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.actualVerifiedCost, ccy) : fmtNum(0, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Event Contribution</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.contributionAfterOverhead, ccy) : fmtNum(instantSim.eventContributionAed, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Contribution Margin</div>
          <div className="cs2-kpi-value">{summary ? fmtPct(summary.contributionMarginPercent) : fmtPct(instantSim.contributionMarginPercent)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Food Cost %</div>
          <div className="cs2-kpi-value">{summary ? fmtPct(summary.foodCostPercent) : fmtPct(instantSim.foodCostPercent)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Beverage Cost %</div>
          <div className="cs2-kpi-value">{summary ? fmtPct(summary.beverageCostPercent) : fmtPct(instantSim.beverageCostPercent)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Cost Per Guest</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.costPerGuest, ccy) : fmtNum(instantSim.costPerGuestAed, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Remaining Budget</div>
          <div className="cs2-kpi-value">{summary ? fmtNum(summary.remainingBudget, ccy) : fmtNum(instantSim.costHeadroomAed, ccy)}</div>
        </div>
        <div className="cs2-kpi">
          <div className="cs2-kpi-label">Target Margin Variance</div>
          <div className="cs2-kpi-value">
            {summary ? fmtPct(summary.targetMarginVariancePercent) : fmtPct(instantSim.contributionMarginPercent - simReq.targetContributionMarginPercent)}
          </div>
        </div>
      </div>

      {/* 17 Contextual Tabs (§50) */}
      <div className="cs2-nav-tabs" role="tablist">
        {TAB_ITEMS.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            className={`cs2-nav-tab ${tab === t.id ? "is-active" : ""}`}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Tab Content */}
      {tab === "overview" && (
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(360px, 1fr))", gap: 16 }}>
          <div className="cs2-card" style={{ padding: 16 }}>
            <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>Revenue-to-Contribution Waterfall</h3>
            <div style={{ height: 280 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={waterfallChartData}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis tick={{ fontSize: 11 }} />
                  <Tooltip formatter={(v: number) => fmtNum(v, ccy)} />
                  <Bar dataKey="value" radius={[6, 6, 0, 0]}>
                    {waterfallChartData.map((entry, idx) => (
                      <Cell
                        key={`cell-${idx}`}
                        fill={
                          entry.kind === "REVENUE"
                            ? "#4f46e5"
                            : entry.kind === "CONTRIBUTION"
                              ? "#059669"
                              : "#f59e0b"
                        }
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="cs2-card" style={{ padding: 16 }}>
            <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>Portfolio &amp; Handover Readiness</h3>
            {dashboard ? (
              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 10, fontSize: 13 }}>
                <div>Total Planned Revenue: <strong>{fmtNum(dashboard.totalPlannedEventRevenue, dashboard.reportingCurrency)}</strong></div>
                <div>Total Estimated Cost: <strong>{fmtNum(dashboard.totalEstimatedEventCost, dashboard.reportingCurrency)}</strong></div>
                <div>Attributable Commitments: <strong>{fmtNum(dashboard.totalAttributableCommitments, dashboard.reportingCurrency)}</strong></div>
                <div>Weighted Avg Margin: <strong>{fmtPct(dashboard.weightedAverageEventMarginPercent)}</strong></div>
                <div>Events Below Target: <strong>{dashboard.eventsBelowTargetMarginCount}</strong></div>
                <div>Pending Approvals: <strong>{dashboard.pendingFinancialApprovalsCount}</strong></div>
                <div>Open Exceptions: <strong>{dashboard.unresolvedFinancialExceptionsCount}</strong></div>
                <div>Break-Even Guests: <strong>{summary?.breakEvenGuests ?? instantSim.breakEvenGuests ?? "N/A"}</strong></div>
              </div>
            ) : (
              <div style={{ fontSize: 13, color: "#64748b" }}>Launch or select a financial plan to inspect tenant portfolio metrics.</div>
            )}
            {detail?.phase3BHandover && (
              <div style={{ marginTop: 12, padding: 10, borderRadius: 8, background: "#eef2ff", fontSize: 12 }}>
                <strong>Phase 3B Analytics Handover:</strong> Ready ({String(detail.phase3BHandover.contractVersion ?? "PHASE3B-HANDOVER-1.0")}) &middot;{" "}
                <strong>Phase 4B Reconciliation Handover:</strong> Provisional Framework Active
              </div>
            )}
          </div>
        </div>
      )}

      {tab === "revenue" && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>Event Revenue Planning &amp; Guaranteed Billing</h3>
          {detail && (
            <>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginBottom: 12 }}>
                <select className="cs2-input" value={revCat} onChange={(e) => setRevCat(e.target.value)}>
                  <option value="EVENT_PACKAGE">Event Package</option>
                  <option value="FOOD_PACKAGE">Food Package</option>
                  <option value="BEVERAGE_PACKAGE">Beverage Package</option>
                  <option value="VENUE_RENTAL">Venue Rental</option>
                  <option value="EQUIPMENT_RENTAL">Equipment Rental</option>
                  <option value="ADDITIONAL_GUEST">Additional Guests</option>
                  <option value="DISCOUNT">Discount / Credit</option>
                </select>
                <input className="cs2-input" placeholder="Description" value={revDesc} onChange={(e) => setRevDesc(e.target.value)} style={{ minWidth: 220 }} />
                <input className="cs2-input" type="number" value={revQty} onChange={(e) => setRevQty(Number(e.target.value))} style={{ width: 90 }} />
                <input className="cs2-input" type="number" value={revPrice} onChange={(e) => setRevPrice(Number(e.target.value))} style={{ width: 110 }} />
                <input className="cs2-input" type="number" placeholder="Discount" value={revDisc} onChange={(e) => setRevDisc(Number(e.target.value))} style={{ width: 100 }} />
                <button type="button" className="cs2-btn cs2-btn-primary" disabled={busy} onClick={() => void handleAddRevenue()}>
                  Add Revenue Line
                </button>
              </div>
              <table className="cs2-table">
                <thead>
                  <tr>
                    <th>Category</th><th>Description</th><th>Status</th><th>Basis</th><th>Qty</th><th>Unit Price</th><th>Discount</th><th>Net Revenue</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.revenueLines.map((r) => (
                    <tr key={r.id}>
                      <td>{r.category}</td><td>{r.description}</td><td>{r.revenueStatus}</td><td>{r.billingBasis}</td>
                      <td>{r.quantity}</td><td>{fmtNum(r.unitPrice, r.currency)}</td><td>{fmtNum(r.discountAmount, r.currency)}</td>
                      <td><strong>{fmtNum(r.netRevenue, ccy)}</strong></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </div>
      )}

      {(tab === "food-cost" || tab === "beverage-cost") && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>
            {tab === "food-cost" ? "Food Cost Management & Material Price Impact" : "Beverage Cost Management"}
          </h3>
          {detail && (
            <table className="cs2-table" style={{ marginBottom: 16 }}>
              <thead>
                <tr><th>Code</th><th>Description</th><th>Behavior</th><th>Estimated</th><th>Committed</th><th>Actual</th><th>Status</th></tr>
              </thead>
              <tbody>
                {detail.expenseLines
                  .filter((e) => (tab === "food-cost" ? e.category === "FOOD" : e.category === "BEVERAGE"))
                  .map((e) => (
                    <tr key={e.id}>
                      <td>{e.expenseCode}</td><td>{e.description}</td><td>{e.costBehavior}</td>
                      <td>{fmtNum(e.estimatedAmount, ccy)}</td><td>{fmtNum(e.committedAmount, ccy)}</td>
                      <td>{e.actualVerifiedAmount != null ? fmtNum(e.actualVerifiedAmount, ccy) : "PROVISIONAL"}</td>
                      <td>{e.status}</td>
                    </tr>
                  ))}
              </tbody>
            </table>
          )}
          {tab === "food-cost" && (
            <div style={{ paddingTop: 12, borderTop: "1px solid #e2e8f0" }}>
              <div style={{ fontSize: 13, fontWeight: 700, marginBottom: 8 }}>Ingredient Price Impact Simulator (§48)</div>
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 10 }}>
                <input className="cs2-input" placeholder="Material Code" value={impactMatCode} onChange={(e) => setImpactMatCode(e.target.value)} />
                <input className="cs2-input" type="number" placeholder="New Unit Price" value={impactNewPrice} onChange={(e) => setImpactNewPrice(Number(e.target.value))} />
                <button
                  type="button"
                  className="cs2-btn cs2-btn-primary"
                  disabled={busy}
                  onClick={async () => {
                    setBusy(true);
                    try {
                      setImpactResult(await analyzeMaterialPriceImpact({ materialCode: impactMatCode, newUnitPrice: impactNewPrice }));
                    } catch (err) {
                      setError(err instanceof Error ? err.message : String(err));
                    } finally {
                      setBusy(false);
                    }
                  }}
                >
                  Run Price Impact Analysis
                </button>
              </div>
              {impactResult && (
                <div style={{ fontSize: 13 }}>
                  <strong>{impactResult.materialDescription}</strong> ({impactResult.materialCode}): Current{" "}
                  {fmtNum(impactResult.currentUnitPrice, impactResult.reportingCurrency)} &rarr; New{" "}
                  {fmtNum(impactResult.newUnitPrice, impactResult.reportingCurrency)} &middot; Net Portfolio Impact:{" "}
                  <strong>{fmtNum(impactResult.totalCostDelta, impactResult.reportingCurrency)}</strong> across {impactResult.affectedItems.length} recipes/events.
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {(tab === "manpower" || tab === "expenses") && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>
            {tab === "manpower" ? "Manpower Costing (RBAC Salary-Protected)" : "Event Expense & Overhead Entry"}
          </h3>
          {detail && !detail.salariesVisible && tab === "manpower" && (
            <div style={{ marginBottom: 10, padding: 10, borderRadius: 8, background: "#fffbeb", border: "1px solid #fde68a", color: "#92400e", fontSize: 12.5 }}>
              Individual employee hourly rates are masked for your role (§25, §70). Aggregate manpower totals remain available where authorized.
            </div>
          )}
          {detail && (
            <>
              <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginBottom: 12 }}>
                <select className="cs2-input" value={expCat} onChange={(e) => setExpCat(e.target.value)}>
                  <option value="MANPOWER">Manpower</option>
                  <option value="DECORATION">Decoration</option>
                  <option value="EQUIPMENT">Equipment</option>
                  <option value="EXTERNAL_SERVICE">External Service</option>
                  <option value="OVERHEAD">Allocated Overhead</option>
                  <option value="OTHER">Other Expense</option>
                </select>
                <input className="cs2-input" placeholder="Description" value={expDesc} onChange={(e) => setExpDesc(e.target.value)} style={{ minWidth: 200 }} />
                <select className="cs2-input" value={expBehavior} onChange={(e) => setExpBehavior(e.target.value)}>
                  <option value="FIXED">Fixed</option>
                  <option value="VARIABLE_PER_GUEST">Variable / Guest</option>
                  <option value="STEP_FIXED">Step-Fixed</option>
                </select>
                {expCat === "MANPOWER" ? (
                  <>
                    <input className="cs2-input" type="number" placeholder="Staff" value={expStaff} onChange={(e) => setExpStaff(Number(e.target.value))} style={{ width: 80 }} />
                    <input className="cs2-input" type="number" placeholder="Hours" value={expHours} onChange={(e) => setExpHours(Number(e.target.value))} style={{ width: 80 }} />
                    <input className="cs2-input" type="number" placeholder="Rate/Hr" value={expHourlyRate} onChange={(e) => setExpHourlyRate(Number(e.target.value))} style={{ width: 90 }} />
                  </>
                ) : (
                  <>
                    <input className="cs2-input" type="number" placeholder="Qty" value={expQty} onChange={(e) => setExpQty(Number(e.target.value))} style={{ width: 80 }} />
                    <input className="cs2-input" type="number" placeholder="Rate" value={expRate} onChange={(e) => setExpRate(Number(e.target.value))} style={{ width: 100 }} />
                    <input className="cs2-input" placeholder="Overhead Pool Ref" value={expPoolId} onChange={(e) => setExpPoolId(e.target.value)} style={{ width: 140 }} />
                  </>
                )}
                <label style={{ display: "inline-flex", alignItems: "center", gap: 4, fontSize: 12 }}>
                  <input type="checkbox" checked={expActual} onChange={(e) => setExpActual(e.target.checked)} />
                  Verified Actual
                </label>
                <button type="button" className="cs2-btn cs2-btn-primary" disabled={busy} onClick={() => void handleAddExpense()}>
                  Add Expense
                </button>
              </div>
              <table className="cs2-table">
                <thead>
                  <tr><th>Code</th><th>Category</th><th>Description</th><th>Behavior</th><th>Staff/Hrs</th><th>Estimated</th><th>Status</th><th>Action</th></tr>
                </thead>
                <tbody>
                  {detail.expenseLines
                    .filter((e) => (tab === "manpower" ? e.category === "MANPOWER" : true))
                    .map((e) => (
                      <tr key={e.id}>
                        <td>{e.expenseCode}</td><td>{e.category}</td><td>{e.description}</td><td>{e.costBehavior}</td>
                        <td>
                          {e.staffCount ? `${e.staffCount} × ${e.plannedHours ?? 0}h` : `${e.quantity} ${e.uom}`}
                          {e.salaryMasked ? " (Rate Masked)" : e.hourlyRate ? ` @ ${e.hourlyRate}` : ""}
                        </td>
                        <td>{fmtNum(e.estimatedAmount, ccy)}</td>
                        <td>{e.status}</td>
                        <td>
                          {e.status !== "APPROVED" && (
                            <button
                              type="button"
                              className="cs2-btn cs2-btn-secondary"
                              disabled={busy}
                              onClick={async () => {
                                setBusy(true);
                                try {
                                  setDetail(await approveEventExpenseLine(detail.summary.id, e.id));
                                } catch (err) {
                                  setError(err instanceof Error ? err.message : String(err));
                                } finally {
                                  setBusy(false);
                                }
                              }}
                            >
                              Approve
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </>
          )}
        </div>
      )}

      {(tab === "procurement" || tab === "allocation") && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>
            {tab === "procurement" ? "Procurement Commitment vs Consumption Separation (§30–32)" : "Multi-Event Cost Allocation & Cost Objects (§28, §33)"}
          </h3>
          {detail && detail.procurementLines.length > 0 ? (
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>Material</th><th>PO</th><th>Status</th><th>Purchased</th><th>Event Alloc</th><th>Other Events</th><th>Unalloc Inv</th>
                  <th>Purchase Val</th><th>Event Attributable</th><th>Remaining Inv</th>
                </tr>
              </thead>
              <tbody>
                {detail.procurementLines.map((p, idx) => (
                  <tr key={`${p.materialCode}-${idx}`}>
                    <td>{p.materialCode} &middot; {p.materialDescription}</td>
                    <td>{p.poNumber || "-"}</td><td>{p.poStatus}</td>
                    <td>{p.purchasedQty} {p.uom}</td><td>{p.eventAllocatedQty}</td><td>{p.otherEventsAllocatedQty}</td><td>{p.unallocatedInventoryQty}</td>
                    <td>{fmtNum(p.purchaseValue, ccy)}</td>
                    <td><strong>{fmtNum(p.attributableEventCommitmentValue, ccy)}</strong></td>
                    <td>{fmtNum(p.remainingInventoryValue, ccy)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div style={{ fontSize: 13, color: "#64748b" }}>
              No linked PO allocations on this plan yet. Link an Event Procurement Plan (Phase 2C) to reconcile Purchase Expenditure vs Attributable Event Consumption vs Remaining Inventory Value.
            </div>
          )}
        </div>
      )}

      {(tab === "estimated-pnl" || tab === "forecast-pnl" || tab === "actual-pnl" || tab === "variance") && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>
            {tab === "estimated-pnl"
              ? "Interactive Estimated P&L Statement (§51)"
              : tab === "forecast-pnl"
                ? "Forecast-at-Completion P&L (§31)"
                : tab === "actual-pnl"
                  ? "Provisional Actual P&L Framework (§34)"
                  : "Financial Variance, Change Impact & Cancellation Exposure (§45–47, §63–64)"}
          </h3>
          {detail && (
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>Section</th><th>Line Item</th><th>Original Budget</th><th>Revised Budget</th><th>Estimated</th>
                  <th>Attributable Commitments</th><th>Forecast-at-Completion</th><th>Actual Verified</th><th>Variance</th><th>Status</th>
                </tr>
              </thead>
              <tbody>
                {detail.statementRows.map((r) => (
                  <tr key={r.rowCode} style={{ fontWeight: r.section === "RESULT" ? 700 : 400, background: r.section === "RESULT" ? "#f8fafc" : undefined }}>
                    <td>{r.section}</td><td>{r.label}</td>
                    <td>{fmtNum(r.originalBudget, ccy)}</td><td>{fmtNum(r.revisedBudget, ccy)}</td>
                    <td>{fmtNum(r.estimated, ccy)}</td><td>{fmtNum(r.attributableCommitments, ccy)}</td>
                    <td>{fmtNum(r.forecastAtCompletion, ccy)}</td>
                    <td>{r.actualVerified != null ? fmtNum(r.actualVerified, ccy) : "PROVISIONAL"}</td>
                    <td>{fmtNum(r.varianceAmount, ccy)}</td>
                    <td>{r.verificationStatus}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          {tab === "variance" && detail && (
            <div style={{ marginTop: 16, paddingTop: 12, borderTop: "1px solid #e2e8f0", display: "flex", flexWrap: "wrap", gap: 10, alignItems: "center" }}>
              <span style={{ fontSize: 12.5, fontWeight: 700 }}>Simulate Approved Event Guest Change (Non-Destructive Revision):</span>
              <input className="cs2-input" type="number" value={changeGuests} onChange={(e) => setChangeGuests(Number(e.target.value))} style={{ width: 100 }} />
              <button
                type="button"
                className="cs2-btn cs2-btn-secondary"
                disabled={busy}
                onClick={async () => {
                  setBusy(true);
                  try {
                    const rev = await triggerFinancialChangeImpact(detail.summary.id, { updatedGuests: changeGuests });
                    setNotice(`Created Revision v${rev.summary.versionNumber} (${rev.summary.planCode}) preserving approved baseline.`);
                    await loadAll(rev.summary.id);
                  } catch (err) {
                    setError(err instanceof Error ? err.message : String(err));
                  } finally {
                    setBusy(false);
                  }
                }}
              >
                Create Revision From Guest Change
              </button>
              <button
                type="button"
                className="cs2-btn cs2-btn-secondary"
                disabled={busy}
                onClick={async () => {
                  setBusy(true);
                  try {
                    setCancelExposure(await getFinancialCancellationExposure(detail.summary.id));
                  } catch (err) {
                    setError(err instanceof Error ? err.message : String(err));
                  } finally {
                    setBusy(false);
                  }
                }}
              >
                Inspect Cancellation Exposure
              </button>
              {cancelExposure && (
                <div style={{ width: "100%", marginTop: 8, padding: 10, borderRadius: 8, background: "#fef2f2", fontSize: 12.5 }}>
                  Net Cancellation Exposure: <strong>{fmtNum(Number(cancelExposure.netCancellationExposure ?? 0), ccy)}</strong> &middot; Released PO Exposure:{" "}
                  {fmtNum(Number(cancelExposure.releasedPoExposure ?? 0), ccy)} &middot; Recoverable Inventory:{" "}
                  {fmtNum(Number(cancelExposure.recoverableInventoryValue ?? 0), ccy)}
                </div>
              )}
            </div>
          )}
        </div>
      )}

      {tab === "simulator" && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>Deterministic Financial Simulator &amp; Scenario Comparison (§52–54)</h3>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(175px, 1fr))", gap: 10, marginBottom: 12 }}>
            <label style={{ fontSize: 12 }}>
              Planned Guests ({simReq.plannedGuests})
              <input type="range" min={50} max={1500} step={10} value={simReq.plannedGuests} onChange={(e) => setSimReq({ ...simReq, plannedGuests: Number(e.target.value), guaranteedGuests: Number(e.target.value) })} style={{ width: "100%" }} />
            </label>
            <label style={{ fontSize: 12 }}>
              Selling Price / Guest
              <input className="cs2-input" type="number" value={simReq.sellingPricePerGuest} onChange={(e) => setSimReq({ ...simReq, sellingPricePerGuest: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Discount %
              <input className="cs2-input" type="number" value={simReq.discountPercent} onChange={(e) => setSimReq({ ...simReq, discountPercent: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Food Cost / Guest
              <input className="cs2-input" type="number" value={simReq.foodCostPerGuest} onChange={(e) => setSimReq({ ...simReq, foodCostPerGuest: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Beverage Cost / Guest
              <input className="cs2-input" type="number" value={simReq.beverageCostPerGuest} onChange={(e) => setSimReq({ ...simReq, beverageCostPerGuest: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Ingredient Price Δ %
              <input className="cs2-input" type="number" value={simReq.ingredientPriceChangePercent} onChange={(e) => setSimReq({ ...simReq, ingredientPriceChangePercent: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Manpower Cost
              <input className="cs2-input" type="number" value={simReq.manpowerCost} onChange={(e) => setSimReq({ ...simReq, manpowerCost: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Decoration + Equip + Other
              <input className="cs2-input" type="number" value={simReq.decorationCost + simReq.equipmentCost + simReq.otherDirectCost} onChange={(e) => setSimReq({ ...simReq, decorationCost: Number(e.target.value), equipmentCost: 0, otherDirectCost: 0 })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Target Margin %
              <input className="cs2-input" type="number" value={simReq.targetContributionMarginPercent} onChange={(e) => setSimReq({ ...simReq, targetContributionMarginPercent: Number(e.target.value) })} />
            </label>
            <label style={{ fontSize: 12 }}>
              Scenario Name
              <input className="cs2-input" value={simReq.scenarioName || ""} onChange={(e) => setSimReq({ ...simReq, scenarioName: e.target.value })} />
            </label>
          </div>

          <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 12 }}>
            {detail && (
              <>
                <button type="button" className="cs2-btn cs2-btn-secondary" disabled={busy} onClick={() => void handleRunServerSimulation(false)}>
                  Run Exact Decimal Engine
                </button>
                <button type="button" className="cs2-btn cs2-btn-primary" disabled={busy} onClick={() => void handleRunServerSimulation(true)}>
                  Save Scenario
                </button>
              </>
            )}
          </div>

          <div className="cs2-kpi-grid" style={{ gridTemplateColumns: "repeat(auto-fit, minmax(160px, 1fr))", marginBottom: 14 }}>
            <div className="cs2-kpi"><div className="cs2-kpi-label">Sim Net Revenue</div><div className="cs2-kpi-value">{fmtNum(simServerResult?.netRevenue ?? instantSim.netRevenueAed, ccy)}</div></div>
            <div className="cs2-kpi"><div className="cs2-kpi-label">Sim Total Cost</div><div className="cs2-kpi-value">{fmtNum(simServerResult?.totalEventCost ?? instantSim.totalCostAed, ccy)}</div></div>
            <div className="cs2-kpi"><div className="cs2-kpi-label">Sim Contribution</div><div className="cs2-kpi-value">{fmtNum(simServerResult?.eventContribution ?? instantSim.eventContributionAed, ccy)}</div></div>
            <div className="cs2-kpi"><div className="cs2-kpi-label">Sim Margin %</div><div className="cs2-kpi-value">{fmtPct(simServerResult?.contributionMarginPercent ?? instantSim.contributionMarginPercent)}</div></div>
            <div className="cs2-kpi"><div className="cs2-kpi-label">Break-Even Guests</div><div className="cs2-kpi-value">{simServerResult?.breakEvenGuests ?? instantSim.breakEvenGuests ?? "No Finite"}</div></div>
            <div className="cs2-kpi"><div className="cs2-kpi-label">Max Allowable Cost</div><div className="cs2-kpi-value">{fmtNum(simServerResult?.maxAllowableCost ?? instantSim.maxAllowableCostAed, ccy)}</div></div>
          </div>

          {detail && detail.scenarios.length > 0 && (
            <table className="cs2-table">
              <thead>
                <tr><th>Scenario</th><th>Guests</th><th>Revenue</th><th>Total Cost</th><th>Contribution</th><th>Margin %</th><th>Break-Even</th><th>Action</th></tr>
              </thead>
              <tbody>
                {detail.scenarios.map((sc) => (
                  <tr key={sc.id}>
                    <td>{sc.name}</td><td>{sc.simulatedGuests}</td>
                    <td>{fmtNum(sc.simulatedRevenue, ccy)}</td><td>{fmtNum(sc.simulatedTotalCost, ccy)}</td>
                    <td>{fmtNum(sc.simulatedContribution, ccy)}</td><td>{fmtPct(sc.simulatedMarginPercent)}</td>
                    <td>{sc.simulatedBreakEvenGuests ?? "N/A"}</td>
                    <td>
                      <button type="button" className="cs2-btn cs2-btn-secondary" disabled={busy || sc.promotedToRevision} onClick={() => void handlePromoteScenario(sc.id)}>
                        {sc.promotedToRevision ? "Promoted" : "Promote to Revision"}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {tab === "ai" && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>AI Financial Intelligence Agent (§55–57)</h3>
          <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginBottom: 10 }}>
            {[
              "How profitable is this event?",
              "What should be the selling price for a 40% margin?",
              "How many guests are required to break even?",
              "Prepare a management summary for this event.",
            ].map((q) => (
              <button key={q} type="button" className="cs2-btn cs2-btn-secondary" onClick={() => setAiQuestion(q)}>
                {q}
              </button>
            ))}
          </div>
          <div style={{ display: "flex", gap: 8, marginBottom: 12 }}>
            <input className="cs2-input" style={{ flex: 1 }} value={aiQuestion} onChange={(e) => setAiQuestion(e.target.value)} />
            <button
              type="button"
              className="cs2-btn cs2-btn-primary"
              disabled={busy || !detail}
              onClick={async () => {
                if (!detail) return;
                setBusy(true);
                try {
                  setAiResponse(await analyzeFinancialPlanWithAi(detail.summary.id, aiQuestion));
                } catch (err) {
                  setError(err instanceof Error ? err.message : String(err));
                } finally {
                  setBusy(false);
                }
              }}
            >
              Ask Financial Agent
            </button>
          </div>
          {aiResponse && (
            <div style={{ padding: 12, borderRadius: 10, background: aiResponse.promptInjectionBlocked ? "#fef2f2" : "#f8fafc", border: "1px solid #e2e8f0" }}>
              <div style={{ fontSize: 11.5, fontWeight: 700, color: "#4f46e5", marginBottom: 6 }}>Provider: {aiResponse.providerUsed}</div>
              <div style={{ fontSize: 13.5, marginBottom: 8 }}>{aiResponse.answer}</div>
              {aiResponse.groundedFacts.length > 0 && (
                <ul style={{ margin: 0, paddingLeft: 18, fontSize: 12, color: "#475569" }}>
                  {aiResponse.groundedFacts.map((f, i) => <li key={i}>{f}</li>)}
                </ul>
              )}
            </div>
          )}
        </div>
      )}

      {tab === "approvals" && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>Financial Approvals &amp; Exception Resolution (§60–62)</h3>
          {detail && (
            <>
              <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 14 }}>
                <input className="cs2-input" placeholder="Approval / Decision Comment" value={approvalComment} onChange={(e) => setApprovalComment(e.target.value)} style={{ minWidth: 260 }} />
                <button
                  type="button"
                  className="cs2-btn cs2-btn-primary"
                  disabled={busy}
                  onClick={async () => {
                    setBusy(true);
                    try {
                      setDetail(await submitFinancialPlanForApproval(detail.summary.id));
                      setNotice("Submitted financial baseline for approval.");
                    } catch (err) {
                      setError(err instanceof Error ? err.message : String(err));
                    } finally {
                      setBusy(false);
                    }
                  }}
                >
                  Submit Baseline
                </button>
                <button
                  type="button"
                  className="cs2-btn cs2-btn-secondary"
                  disabled={busy}
                  onClick={async () => {
                    setBusy(true);
                    try {
                      setDetail(await decideFinancialPlanApproval(detail.summary.id, "APPROVE", approvalComment || "Approved"));
                      setNotice("Approved financial baseline.");
                    } catch (err) {
                      setError(err instanceof Error ? err.message : String(err));
                    } finally {
                      setBusy(false);
                    }
                  }}
                >
                  Approve
                </button>
                <button
                  type="button"
                  className="cs2-btn cs2-btn-secondary"
                  disabled={busy}
                  onClick={async () => {
                    setBusy(true);
                    try {
                      setDetail(await decideFinancialPlanApproval(detail.summary.id, "RETURN", approvalComment || "Revision requested"));
                      setNotice("Returned financial baseline for revision.");
                    } catch (err) {
                      setError(err instanceof Error ? err.message : String(err));
                    } finally {
                      setBusy(false);
                    }
                  }}
                >
                  Return
                </button>
              </div>
              <table className="cs2-table">
                <thead><tr><th>Severity</th><th>Type</th><th>Message</th><th>Status</th><th>Action</th></tr></thead>
                <tbody>
                  {detail.exceptions.map((ex) => (
                    <tr key={ex.id}>
                      <td>{ex.severity}</td><td>{ex.exceptionType}</td><td>{ex.message}</td><td>{ex.resolutionStatus}</td>
                      <td>
                        {ex.resolutionStatus === "OPEN" && (
                          <button
                            type="button"
                            className="cs2-btn cs2-btn-secondary"
                            disabled={busy}
                            onClick={async () => {
                              setBusy(true);
                              try {
                                await resolveFinancialException(detail.summary.id, ex.id, "RESOLVED", "Reviewed");
                                setDetail(await getEventFinancialPlan(detail.summary.id));
                              } catch (err) {
                                setError(err instanceof Error ? err.message : String(err));
                              } finally {
                                setBusy(false);
                              }
                            }}
                          >
                            Resolve
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </div>
      )}

      {tab === "reports" && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 10, gap: 10, flexWrap: "wrap" }}>
            <h3 style={{ margin: 0, fontSize: 15, fontWeight: 700 }}>Financial Report Studio ({catalog.length} Reports)</h3>
            <input className="cs2-input" placeholder="Search 48 financial reports..." value={reportFilter} onChange={(e) => setReportFilter(e.target.value)} style={{ minWidth: 260 }} />
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(250px, 1fr))", gap: 10, marginBottom: 14 }}>
            {filteredCatalog.map((r) => (
              <div key={r.reportCode} style={{ border: "1px solid #e2e8f0", borderRadius: 10, padding: 10, background: "#ffffff" }}>
                <div style={{ fontSize: 11, fontWeight: 700, color: "#4f46e5" }}>{r.category}</div>
                <div style={{ fontSize: 13, fontWeight: 700, margin: "4px 0" }}>{r.title}</div>
                <button type="button" className="cs2-btn cs2-btn-secondary" disabled={busy || !detail} onClick={() => void handleExportReport(r.reportCode)}>
                  Preview &amp; Export CSV
                </button>
              </div>
            ))}
          </div>
          {reportPreview && (
            <div style={{ paddingTop: 12, borderTop: "1px solid #e2e8f0" }}>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 8 }}>
                <strong>{reportPreview.title} ({reportPreview.fileName})</strong>
                <button type="button" className="cs2-btn cs2-btn-primary" onClick={() => downloadCsv(reportPreview.fileName, reportPreview.csvContent)}>
                  Download CSV
                </button>
              </div>
              <pre style={{ fontSize: 11.5, background: "#f8fafc", padding: 10, borderRadius: 8, overflowX: "auto", maxHeight: 220 }}>
                {reportPreview.csvContent}
              </pre>
            </div>
          )}
        </div>
      )}

      {tab === "history" && (
        <div className="cs2-card" style={{ padding: 16 }}>
          <h3 style={{ margin: "0 0 10px", fontSize: 15, fontWeight: 700 }}>Financial Snapshot &amp; Transaction Tracker History (§67)</h3>
          {detail && (
            <table className="cs2-table">
              <thead><tr><th>Timestamp (UTC)</th><th>Action</th><th>Details</th></tr></thead>
              <tbody>
                {detail.history.map((h) => (
                  <tr key={h.id}>
                    <td>{new Date(h.timestampUtc).toISOString()}</td>
                    <td><strong>{h.action}</strong></td>
                    <td>{h.comment || "-"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  );
};

export default CulinaryEventPnlStudioWorkspace;
