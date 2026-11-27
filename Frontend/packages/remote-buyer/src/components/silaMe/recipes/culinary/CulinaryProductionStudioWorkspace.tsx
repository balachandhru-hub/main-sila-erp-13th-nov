import React, { useCallback, useEffect, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  allocateCentralKitchenDispatch,
  amendProductionPlan,
  applyProductionSchedule,
  confirmProductionPickingLine,
  createProductionDemand,
  createProductionPlan,
  decideProductionPlanApproval,
  executeProductionBatchAction,
  explodeValidateProductionPlan,
  generateOrderPickingList,
  getProductionBatchTraceability,
  getProductionCalendar,
  getProductionDashboard,
  getProductionKdsFeed,
  issueMaterialToProduction,
  listBatchTasks,
  listCentralKitchenDispatches,
  listCulinaryEquipment,
  listCulinaryKitchens,
  listCulinaryStaff,
  listCulinaryWorkstations,
  listProductionDemands,
  listProductionExceptions,
  listProductionOrders,
  listProductionPlans,
  moveCentralKitchenDispatch,
  optimizeProductionSchedule,
  queryProductionCopilot,
  recordProductionQualityCheck,
  releaseProductionPlanToOrders,
  reportProductionException,
  rescheduleProductionBatch,
  resolveProductionException,
  resolveQualityHold,
  submitProductionPlanApproval,
  triggerBuffetReplenishment,
  updateProductionTaskStatus,
  upsertCulinaryEquipment,
  upsertCulinaryKitchen,
  upsertCulinaryStaff,
  upsertCulinaryWorkstation,
  type SilaCulinaryEquipment,
  type SilaCulinaryKitchen,
  type SilaCulinaryProductionBatch,
  type SilaCulinaryProductionBatchTraceability,
  type SilaCulinaryProductionCopilotResponse,
  type SilaCulinaryProductionDashboard,
  type SilaCulinaryProductionDemand,
  type SilaCulinaryProductionDispatch,
  type SilaCulinaryProductionException,
  type SilaCulinaryProductionOrder,
  type SilaCulinaryProductionPickingLine,
  type SilaCulinaryProductionPlan,
  type SilaCulinaryProductionTask,
  type SilaCulinaryScheduleOptimizeResponse,
  type SilaCulinaryStaffAssignment,
  type SilaCulinaryWorkstation,
} from "../../../../api/silaMe/silaCulinaryProductionApi";
import "./CulinarySuite.css";

type ProdTab =
  | "cockpit"
  | "calendar"
  | "plans"
  | "orders"
  | "kds"
  | "picking"
  | "central"
  | "quality"
  | "scheduler";

const KITCHEN_TYPES = [
  "MAIN_KITCHEN",
  "CENTRAL_KITCHEN",
  "BANQUET_KITCHEN",
  "PASTRY_KITCHEN",
  "BAKERY",
  "COLD_KITCHEN",
  "HOT_KITCHEN",
  "BUTCHERY",
  "BREAKFAST_KITCHEN",
  "SPECIALTY_KITCHEN",
  "CLOUD_KITCHEN",
  "STAFF_KITCHEN",
  "SATELLITE_KITCHEN",
];

const CulinaryProductionStudioWorkspace: React.FC = () => {
  const [tab, setTab] = useState<ProdTab>("cockpit");
  const [loading, setLoading] = useState(false);
  const [kdsFullScreen, setKdsFullScreen] = useState(false);
  const [selectedKitchenId, setSelectedKitchenId] = useState<string>("");

  // Master Data
  const [kitchens, setKitchens] = useState<SilaCulinaryKitchen[]>([]);
  const [workstations, setWorkstations] = useState<SilaCulinaryWorkstation[]>([]);
  const [equipment, setEquipment] = useState<SilaCulinaryEquipment[]>([]);
  const [staff, setStaff] = useState<SilaCulinaryStaffAssignment[]>([]);

  // Operational State
  const [dashboard, setDashboard] = useState<SilaCulinaryProductionDashboard | null>(null);
  const [demands, setDemands] = useState<SilaCulinaryProductionDemand[]>([]);
  const [plans, setPlans] = useState<SilaCulinaryProductionPlan[]>([]);
  const [orders, setOrders] = useState<SilaCulinaryProductionOrder[]>([]);
  const [calendarBatches, setCalendarBatches] = useState<SilaCulinaryProductionBatch[]>([]);
  const [kdsBatches, setKdsBatches] = useState<SilaCulinaryProductionBatch[]>([]);
  const [kdsViewMode, setKdsViewMode] = useState<string>("ALL");
  const [pickingLines, setPickingLines] = useState<SilaCulinaryProductionPickingLine[]>([]);
  const [selectedOrderId, setSelectedOrderId] = useState<string>("");
  const [selectedBatchId, setSelectedBatchId] = useState<string>("");
  const [batchTasks, setBatchTasks] = useState<SilaCulinaryProductionTask[]>([]);
  const [traceability, setTraceability] = useState<SilaCulinaryProductionBatchTraceability | null>(null);
  const [dispatches, setDispatches] = useState<SilaCulinaryProductionDispatch[]>([]);
  const [exceptions, setExceptions] = useState<SilaCulinaryProductionException[]>([]);
  const [scheduleResult, setScheduleResult] = useState<SilaCulinaryScheduleOptimizeResponse | null>(null);
  const [scheduleObjective, setScheduleObjective] = useState("MIN_DELAY");
  const [copilotQ, setCopilotQ] = useState("Which batches are active or delayed today?");
  const [copilotAns, setCopilotAns] = useState<SilaCulinaryProductionCopilotResponse | null>(null);

  // Forms
  const [kCode, setKCode] = useState("MAIN-KIT");
  const [kName, setKName] = useState("Main Culinary Kitchen");
  const [kType, setKType] = useState("MAIN_KITCHEN");
  const [kCap, setKCap] = useState(1200);

  const [wsCode, setWsCode] = useState("WS-HOT");
  const [wsName, setWsName] = useState("Hot Cooking Line");
  const [eqCode, setEqCode] = useState("COMBI-01");
  const [eqName, setEqName] = useState("Rational Combi Oven 20-Grid");
  const [eqCap, setEqCap] = useState(60);

  const [staffName, setStaffName] = useState("Chef de Partie Tariq");
  const [staffRole, setStaffRole] = useState("CHEF_DE_PARTIE");

  const [demSourceKey, setDemSourceKey] = useState(() => `EVT-DEM-${Date.now().toString().slice(-4)}`);
  const [demMenuItem, setDemMenuItem] = useState("Herb Crusted Sea Bass");
  const [demPortions, setDemPortions] = useState(120);

  const [planMealPeriod, setPlanMealPeriod] = useState("DINNER");
  const [planDishName, setPlanDishName] = useState("Herb Crusted Sea Bass");
  const [planPortions, setPlanPortions] = useState(120);

  const [bufItemName, setBufItemName] = useState("Live Buffet — Grilled Salmon");
  const [bufQty, setBufQty] = useState(30);

  const [qcName, setQcName] = useState("Core Cooking Temperature");
  const [qcExpected, setQcExpected] = useState(">= 75");
  const [qcActual, setQcActual] = useState("78");
  const [qcPassed, setQcPassed] = useState(true);

  const [excType, setExcType] = useState("PRODUCTION_DELAY");
  const [excDesc, setExcDesc] = useState("");

  const [ckOutletName, setCkOutletName] = useState("Grand Ballroom Banquet");
  const [ckQty, setCkQty] = useState(25);

  const [issueFromLoc, setIssueFromLoc] = useState("");
  const [issueToLoc, setIssueToLoc] = useState("");
  const [issueMatCode, setIssueMatCode] = useState("MAT-SALMON");
  const [issueQty, setIssueQty] = useState(5);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const kId = selectedKitchenId || undefined;
      const [ks, ws, eq, st, dash, dems, pls, ords, cal, kds, excs, dsps] = await Promise.all([
        listCulinaryKitchens().catch(() => []),
        listCulinaryWorkstations(kId).catch(() => []),
        listCulinaryEquipment(kId).catch(() => []),
        listCulinaryStaff(kId).catch(() => []),
        getProductionDashboard(undefined, kId).catch(() => null),
        listProductionDemands().catch(() => []),
        listProductionPlans().catch(() => []),
        listProductionOrders(kId).catch(() => []),
        getProductionCalendar({ kitchenId: kId }).catch(() => []),
        getProductionKdsFeed(kId, kdsViewMode).catch(() => []),
        listProductionExceptions().catch(() => []),
        listCentralKitchenDispatches(kId).catch(() => []),
      ]);
      setKitchens(ks);
      setWorkstations(ws);
      setEquipment(eq);
      setStaff(st);
      setDashboard(dash);
      setDemands(dems);
      setPlans(pls);
      setOrders(ords);
      setCalendarBatches(cal);
      setKdsBatches(kds);
      setExceptions(excs);
      setDispatches(dsps);
      if (ords.length > 0 && !selectedOrderId) {
        setSelectedOrderId(ords[0].id);
        if (ords[0].batches.length > 0 && !selectedBatchId) {
          setSelectedBatchId(ords[0].batches[0].id);
        }
      }
    } finally {
      setLoading(false);
    }
  }, [selectedKitchenId, kdsViewMode, selectedOrderId, selectedBatchId]);

  useEffect(() => {
    void loadAll();
  }, [loadAll]);

  useEffect(() => {
    if (!selectedBatchId) return;
    listBatchTasks(selectedBatchId).then(setBatchTasks).catch(() => setBatchTasks([]));
    getProductionBatchTraceability(selectedBatchId).then(setTraceability).catch(() => setTraceability(null));
  }, [selectedBatchId]);

  const allBatches = orders.flatMap((o) => o.batches);

  return (
    <div
      className="cs2-shell"
      data-testid="culinary-production-workspace"
      style={kdsFullScreen ? { position: "fixed", top: "var(--sila-topbar-height, 3.75rem)", left: 0, right: 0, bottom: 0, zIndex: 950, overflowY: "auto", background: "#ffffff", padding: 20 } : undefined}
    >
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Kitchen Production Planning, Batch Scheduling &amp; Execution</h2>
          <p className="cs2-subtitle">
            Multi-level recipe explosion, ingredient availability, scheduling optimization, KDS, FEFO picking, food safety checkpoints &amp; QR batch traceability.
          </p>
        </div>
        <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
          <select
            className="sila-select"
            value={selectedKitchenId}
            onChange={(e) => setSelectedKitchenId(e.target.value)}
            aria-label="Filter by Kitchen"
          >
            <option value="">All Kitchens</option>
            {kitchens.map((k) => (
              <option key={k.id} value={k.id}>
                {k.kitchenCode} — {k.kitchenName}
              </option>
            ))}
          </select>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => setKdsFullScreen((v) => !v)}>
            {kdsFullScreen ? "Exit KDS Fullscreen" : "KDS Fullscreen"}
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={loading} onClick={() => void loadAll()}>
            {loading ? "Refreshing..." : "Refresh Live"}
          </button>
        </div>
      </div>

      <div className="cs2-nav-tabs" role="tablist" aria-label="Kitchen Production Modules" style={{ marginBottom: 14 }}>
        {(
          [
            ["cockpit", "1. Production Cockpit"],
            ["calendar", "2. Calendar & Reschedule"],
            ["plans", "3. Demand & Plans"],
            ["orders", "4. Orders, Batches & Tasks"],
            ["kds", "5. KDS & Production Sheet"],
            ["picking", "6. Picking & Material Issue"],
            ["central", "7. Central Kitchen"],
            ["quality", "8. Quality, QR & Exceptions"],
            ["scheduler", "9. AI Scheduler, Copilot & Setup"],
          ] as [ProdTab, string][]
        ).map(([id, label]) => (
          <button
            key={id}
            type="button"
            role="tab"
            aria-selected={tab === id}
            data-testid={`prod-tab-${id}`}
            className={`cs2-nav-tab${tab === id ? " is-active" : ""}`}
            onClick={() => setTab(id)}
          >
            {label}
          </button>
        ))}
      </div>

      {tab === "cockpit" && (
        <div data-testid="prod-panel-cockpit">
          <div className="cs2-kpi-grid" style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(170px, 1fr))", gap: 10, marginBottom: 16 }}>
            {[
              ["Planned Orders", dashboard?.plannedOrdersCount ?? 0],
              ["Released Orders", dashboard?.releasedOrdersCount ?? 0],
              ["Active Batches", dashboard?.activeBatchesCount ?? 0],
              ["Completed Batches", dashboard?.completedBatchesCount ?? 0],
              ["Delayed Batches", dashboard?.delayedBatchesCount ?? 0],
              ["Ingredient Shortages", dashboard?.ingredientShortagesCount ?? 0],
              ["Pending Quality", dashboard?.pendingQualityChecksCount ?? 0],
              ["On-Time Rate", `${dashboard?.onTimeProductionRatePct ?? 100}%`],
              ["Equipment Util.", `${dashboard?.equipmentUtilizationPct ?? 0}%`],
              ["Staff Util.", `${dashboard?.staffUtilizationPct ?? 0}%`],
              ["Planned Output", dashboard?.plannedOutputTotal ?? 0],
              ["Actual Output", dashboard?.actualOutputTotal ?? 0],
            ].map(([label, value]) => (
              <div key={String(label)} className="cs2-card" style={{ padding: 12 }}>
                <div className="cs2-empty-note" style={{ fontSize: 12 }}>{label}</div>
                <div style={{ fontSize: 22, fontWeight: 700, marginTop: 4 }}>{value}</div>
              </div>
            ))}
          </div>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Buffet Replenishment Quick Control</h3>
              <span className="cs2-badge cs2-badge--live">Buffet Analytics Sync</span>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Kitchen</label>
                <select className="sila-select" value={selectedKitchenId || kitchens[0]?.id || ""} onChange={(e) => setSelectedKitchenId(e.target.value)}>
                  {kitchens.length === 0 && <option value="">Create a kitchen in Tab 9 first</option>}
                  {kitchens.map((k) => <option key={k.id} value={k.id}>{k.kitchenName}</option>)}
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label">Buffet Item</label>
                <input className="sila-input" value={bufItemName} onChange={(e) => setBufItemName(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Replenishment Portions</label>
                <input className="sila-input" type="number" min={1} value={bufQty} onChange={(e) => setBufQty(Number(e.target.value))} />
              </div>
              <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  disabled={!(selectedKitchenId || kitchens[0]?.id)}
                  onClick={async () => {
                    try {
                      const b = await triggerBuffetReplenishment({
                        kitchenId: selectedKitchenId || kitchens[0].id,
                        itemName: bufItemName,
                        qty: bufQty,
                      });
                      toastService.success(`Buffet replenishment batch ${b.batchNumber} created.`);
                      await loadAll();
                    } catch (err: unknown) {
                      toastService.error(err instanceof Error ? err.message : "Buffet replenishment failed.");
                    }
                  }}
                >
                  Trigger Buffet Batch
                </button>
              </div>
            </div>
          </section>
        </div>
      )}

      {tab === "calendar" && (
        <section className="cs2-card" data-testid="prod-panel-calendar">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Kitchen Production Calendar &amp; Interactive Rescheduling</h3>
            <span className="cs2-badge cs2-badge--live">{calendarBatches.length} Scheduled Batches</span>
          </div>
          {calendarBatches.length === 0 ? (
            <p className="cs2-empty-note">Release a production plan to populate the kitchen production calendar.</p>
          ) : (
            <div className="cs2-timeline">
              {calendarBatches.map((b) => (
                <div key={b.id} className="cs2-timeline-row">
                  <div>
                    <strong>{b.batchNumber}</strong>
                    <div className="cs2-empty-note">{b.plannedBatchQty} {b.productionUom} · {b.batchStatus}</div>
                  </div>
                  <div>
                    <div style={{ fontSize: 13 }}>
                      {new Date(b.plannedStartUtc).toLocaleString()} → {new Date(b.plannedFinishUtc).toLocaleString()}
                    </div>
                    <div className="cs2-timeline-bar-track" style={{ marginTop: 6 }}>
                      <div className="cs2-timeline-bar-fill" style={{ left: "10%", width: "55%" }} />
                    </div>
                  </div>
                  <div style={{ display: "flex", gap: 6, justifyContent: "flex-end" }}>
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--secondary"
                      onClick={async () => {
                        try {
                          const start = new Date(new Date(b.plannedStartUtc).getTime() + 60 * 60000).toISOString();
                          const finish = new Date(new Date(b.plannedFinishUtc).getTime() + 60 * 60000).toISOString();
                          await rescheduleProductionBatch(b.id, { plannedStartUtc: start, plannedFinishUtc: finish });
                          toastService.success(`Rescheduled ${b.batchNumber} +1h.`);
                          await loadAll();
                        } catch (err: unknown) {
                          toastService.error(err instanceof Error ? err.message : "Reschedule conflict.");
                        }
                      }}
                    >
                      Shift +1h
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {tab === "plans" && (
        <div data-testid="prod-panel-plans" className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Demand-to-Production Conversion</h3>
              <span className="cs2-badge cs2-badge--live">Duplicate Guarded</span>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Source Document Key</label>
                <input className="sila-input" value={demSourceKey} onChange={(e) => setDemSourceKey(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Menu Item</label>
                <input className="sila-input" value={demMenuItem} onChange={(e) => setDemMenuItem(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Forecast Portions</label>
                <input className="sila-input" type="number" min={1} value={demPortions} onChange={(e) => setDemPortions(Number(e.target.value))} />
              </div>
              <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={async () => {
                    try {
                      await createProductionDemand({
                        sourceDocumentKey: demSourceKey,
                        demandSource: "OPERA_RESERVATIONS",
                        demandNature: "CONFIRMED",
                        menuItemName: demMenuItem,
                        forecastPortions: demPortions,
                        kitchenId: selectedKitchenId || kitchens[0]?.id,
                      });
                      setDemSourceKey(`EVT-DEM-${Date.now().toString().slice(-4)}`);
                      toastService.success("Demand created.");
                      await loadAll();
                    } catch (err: unknown) {
                      toastService.error(err instanceof Error ? err.message : "Failed to create demand.");
                    }
                  }}
                >
                  Capture Demand
                </button>
              </div>
            </div>
            <ul style={{ marginTop: 12, paddingLeft: 18, fontSize: 13 }}>
              {demands.map((d) => (
                <li key={d.id}>
                  <strong>{d.sourceDocumentKey}</strong> — {d.menuItemName} ({d.adjustedPortions} portions · {d.conversionStatus})
                </li>
              ))}
            </ul>
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Kitchen Production Plans</h3>
              <span className="cs2-badge cs2-badge--live">Versioned &amp; Approved</span>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Meal Period</label>
                <select className="sila-select" value={planMealPeriod} onChange={(e) => setPlanMealPeriod(e.target.value)}>
                  <option value="BREAKFAST">BREAKFAST</option>
                  <option value="LUNCH">LUNCH</option>
                  <option value="DINNER">DINNER</option>
                  <option value="BANQUET">BANQUET</option>
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label">Menu Item</label>
                <input className="sila-input" value={planDishName} onChange={(e) => setPlanDishName(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Portions</label>
                <input className="sila-input" type="number" min={1} value={planPortions} onChange={(e) => setPlanPortions(Number(e.target.value))} />
              </div>
              <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={async () => {
                    try {
                      await createProductionPlan({
                        mealPeriod: planMealPeriod,
                        manualLines: [{ menuItemName: planDishName, requiredPortions: planPortions, kitchenId: selectedKitchenId || kitchens[0]?.id }],
                      });
                      toastService.success("Production plan created.");
                      await loadAll();
                    } catch (err: unknown) {
                      toastService.error(err instanceof Error ? err.message : "Failed to create plan.");
                    }
                  }}
                >
                  Create Plan
                </button>
              </div>
            </div>

            <div style={{ marginTop: 12, display: "flex", flexDirection: "column", gap: 8 }}>
              {plans.map((p) => (
                <div key={p.id} style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: 10 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
                    <strong>{p.planCode} v{p.versionNumber} · {p.mealPeriod} ({p.status})</strong>
                    <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await explodeValidateProductionPlan(p.id); await loadAll(); }}>
                        Explode &amp; Check Stock
                      </button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await submitProductionPlanApproval(p.id); await loadAll(); }}>
                        Submit
                      </button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await decideProductionPlanApproval(p.id, "APPROVE"); await loadAll(); }}>
                        Approve
                      </button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await amendProductionPlan(p.id, "Operational guest revision", p.lines.map((l) => ({ menuItemName: l.menuItemName, requiredPortions: l.requiredPortions + 10, kitchenId: l.kitchenId ?? undefined }))); await loadAll(); }}>
                        Amend (+10)
                      </button>
                      <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => { await releaseProductionPlanToOrders(p.id); await loadAll(); }}>
                        Release Orders
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </section>
        </div>
      )}

      {tab === "orders" && (
        <section className="cs2-card" data-testid="prod-panel-orders">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Production Orders, Batches &amp; Task Dependencies</h3>
            <span className="cs2-badge cs2-badge--live">{orders.length} Orders · {allBatches.length} Batches</span>
          </div>
          {orders.map((o) => (
            <div key={o.id} style={{ border: "1px solid #e2e8f0", borderRadius: 8, padding: 12, marginBottom: 10 }}>
              <div style={{ display: "flex", justifyContent: "space-between", flexWrap: "wrap" }}>
                <strong>{o.orderNumber} — {o.recipeName} (v{o.recipeVersion})</strong>
                <span className="cs2-badge cs2-badge--indigo">{o.executionStatus} · {o.plannedOutputQty} {o.outputUom}</span>
              </div>
              <div style={{ marginTop: 8, display: "flex", gap: 8, flexWrap: "wrap" }}>
                {o.batches.map((b) => (
                  <button
                    key={b.id}
                    type="button"
                    className={`sila-btn sila-btn--sm ${selectedBatchId === b.id ? "sila-btn--primary" : "sila-btn--secondary"}`}
                    onClick={() => setSelectedBatchId(b.id)}
                  >
                    {b.batchNumber} ({b.plannedBatchQty} {b.productionUom} · {b.batchStatus})
                  </button>
                ))}
              </div>
            </div>
          ))}

          {selectedBatchId && (
            <div style={{ marginTop: 12 }}>
              <h4>Batch Tasks (Mandatory Sequencing Enforced)</h4>
              {batchTasks.map((t) => (
                <div key={t.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "6px 0", borderBottom: "1px solid #f1f5f9" }}>
                  <span>#{t.sequenceOrder} <strong>{t.taskType}</strong> — {t.instructions} ({t.status})</span>
                  {t.status !== "COMPLETED" && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--primary"
                      onClick={async () => {
                        try {
                          await updateProductionTaskStatus(t.id, "COMPLETED");
                          const updated = await listBatchTasks(selectedBatchId);
                          setBatchTasks(updated);
                        } catch (err: unknown) {
                          toastService.error(err instanceof Error ? err.message : "Task sequencing blocked.");
                        }
                      }}
                    >
                      Complete Task
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {tab === "kds" && (
        <section className="cs2-card" data-testid="prod-panel-kds">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Kitchen Display System (KDS) &amp; Digital Production Sheet</h3>
            <div style={{ display: "flex", gap: 6, flexWrap: "wrap" }}>
              {["ALL", "ACTIVE", "DELAYED", "QUALITY_PENDING", "READY_FOR_DISPATCH", "COMPLETED"].map((vm) => (
                <button
                  key={vm}
                  type="button"
                  className={`sila-btn sila-btn--sm ${kdsViewMode === vm ? "sila-btn--primary" : "sila-btn--secondary"}`}
                  onClick={() => setKdsViewMode(vm)}
                >
                  {vm}
                </button>
              ))}
            </div>
          </div>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(260px, 1fr))", gap: 12 }}>
            {kdsBatches.map((b) => (
              <div key={b.id} style={{ border: "1px solid #cbd5e1", borderRadius: 10, padding: 14, background: b.batchStatus === "ON_HOLD" ? "#fef2f2" : "#ffffff" }}>
                <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                  <strong>{b.batchNumber}</strong>
                  <span className="cs2-badge cs2-badge--indigo">{b.batchStatus}</span>
                </div>
                <div style={{ fontSize: 13, marginTop: 6 }}>
                  Qty: <strong>{b.plannedBatchQty} {b.productionUom}</strong> · Quality: <strong>{b.qualityStatus}</strong>
                </div>
                <div style={{ fontSize: 12, color: "#475569", marginTop: 4 }}>
                  Deadline: {new Date(b.plannedFinishUtc).toLocaleTimeString()}
                </div>
                <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginTop: 10 }}>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await executeProductionBatchAction(b.id, "START_PREP"); await loadAll(); }}>Prep</button>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await executeProductionBatchAction(b.id, "START_COOKING"); await loadAll(); }}>Cook</button>
                  <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await executeProductionBatchAction(b.id, "READY_FOR_QUALITY"); await loadAll(); }}>QC Ready</button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    onClick={async () => {
                      try {
                        await executeProductionBatchAction(b.id, "COMPLETE_BATCH", b.plannedBatchQty);
                        toastService.success(`Batch ${b.batchNumber} completed.`);
                        await loadAll();
                      } catch (err: unknown) {
                        toastService.error(err instanceof Error ? err.message : "Completion blocked.");
                      }
                    }}
                  >
                    Complete
                  </button>
                </div>
              </div>
            ))}
          </div>
        </section>
      )}

      {tab === "picking" && (
        <div data-testid="prod-panel-picking" className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">FEFO / FIFO Ingredient Picking</h3>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Production Order</label>
                <select className="sila-select" value={selectedOrderId} onChange={(e) => setSelectedOrderId(e.target.value)}>
                  {orders.map((o) => <option key={o.id} value={o.id}>{o.orderNumber} — {o.recipeName}</option>)}
                </select>
              </div>
              <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 6 }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  disabled={!selectedOrderId}
                  onClick={async () => {
                    const list = await generateOrderPickingList(selectedOrderId, "FEFO");
                    setPickingLines(list);
                    toastService.success(`Generated ${list.length} FEFO picking line(s).`);
                  }}
                >
                  Generate FEFO Picking List
                </button>
              </div>
            </div>
            {pickingLines.map((pl) => (
              <div key={pl.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "6px 0", borderBottom: "1px solid #f1f5f9" }}>
                <span>{pl.materialCode} — Req {pl.requiredQty} {pl.inventoryUom} (Lot: {pl.inventoryBatchNumber}, {pl.status})</span>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={async () => {
                    const updated = await confirmProductionPickingLine(pl.id, pl.requiredQty);
                    setPickingLines((prev) => prev.map((x) => (x.id === updated.id ? updated : x)));
                  }}
                >
                  Confirm Pick
                </button>
              </div>
            ))}
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Idempotent Material Issue to Kitchen</h3>
              <span className="cs2-badge cs2-badge--live">InventoryLedger Sync</span>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Material Code</label>
                <input className="sila-input" value={issueMatCode} onChange={(e) => setIssueMatCode(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Qty (KG)</label>
                <input className="sila-input" type="number" min={0.1} step={0.5} value={issueQty} onChange={(e) => setIssueQty(Number(e.target.value))} />
              </div>
              <div className="sila-field">
                <label className="sila-label">From Store Location ID</label>
                <input className="sila-input" value={issueFromLoc} onChange={(e) => setIssueFromLoc(e.target.value)} placeholder="Store Location GUID" />
              </div>
              <div className="sila-field">
                <label className="sila-label">To Kitchen Location ID</label>
                <input className="sila-input" value={issueToLoc} onChange={(e) => setIssueToLoc(e.target.value)} placeholder="Kitchen Location GUID" />
              </div>
            </div>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              disabled={!selectedOrderId}
              onClick={async () => {
                try {
                  await issueMaterialToProduction({
                    productionOrderId: selectedOrderId,
                    productionBatchId: selectedBatchId || undefined,
                    fromStoreLocationId: issueFromLoc || "00000000-0000-0000-0000-000000000001",
                    toKitchenLocationId: issueToLoc || "00000000-0000-0000-0000-000000000002",
                    materialCode: issueMatCode,
                    issueQty,
                    uom: "KG",
                    idempotencyKey: `UI-${selectedOrderId}-${issueMatCode}-${Date.now()}`,
                  });
                  toastService.success("Material issued to kitchen.");
                } catch (err: unknown) {
                  toastService.error(err instanceof Error ? err.message : "Issue failed.");
                }
              }}
            >
              Post Idempotent Material Issue
            </button>
          </section>
        </div>
      )}

      {tab === "central" && (
        <section className="cs2-card" data-testid="prod-panel-central">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Central Kitchen Allocation &amp; Outlet Distribution</h3>
            <span className="cs2-badge cs2-badge--live">Over-Allocation Protected</span>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Destination Outlet</label>
              <input className="sila-input" value={ckOutletName} onChange={(e) => setCkOutletName(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Quantity</label>
              <input className="sila-input" type="number" min={1} value={ckQty} onChange={(e) => setCkQty(Number(e.target.value))} />
            </div>
            <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--primary"
                disabled={!selectedBatchId || kitchens.length === 0}
                onClick={async () => {
                  try {
                    await allocateCentralKitchenDispatch({
                      centralKitchenId: selectedKitchenId || kitchens[0].id,
                      batchId: selectedBatchId,
                      destinationOutletId: kitchens[0].outletIds[0] || kitchens[0].id,
                      destinationOutletName: ckOutletName,
                      requestedQty: ckQty,
                    });
                    toastService.success("Central kitchen dispatch allocated.");
                    await loadAll();
                  } catch (err: unknown) {
                    toastService.error(err instanceof Error ? err.message : "Allocation failed.");
                  }
                }}
              >
                Allocate Dispatch
              </button>
            </div>
          </div>
          {dispatches.map((d) => (
            <div key={d.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: "8px 0", borderBottom: "1px solid #f1f5f9" }}>
              <span><strong>{d.dispatchNumber}</strong> → {d.destinationOutletName}: Alloc {d.allocatedQty}, Disp {d.dispatchedQty}, Recv {d.receivedQty} ({d.status})</span>
              <div style={{ display: "flex", gap: 6 }}>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await moveCentralKitchenDispatch(d.id, "DISPATCH", d.allocatedQty - d.dispatchedQty || 1); await loadAll(); }}>Dispatch</button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => { await moveCentralKitchenDispatch(d.id, "RECEIVE", d.dispatchedQty - d.receivedQty || 1); await loadAll(); }}>Confirm Receipt</button>
              </div>
            </div>
          ))}
        </section>
      )}

      {tab === "quality" && (
        <div data-testid="prod-panel-quality" className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Food Safety Quality Checkpoints &amp; QR Traceability</h3>
              <span className="cs2-badge cs2-badge--live">Failed Check Auto-Holds Batch</span>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Checkpoint</label>
                <input className="sila-input" value={qcName} onChange={(e) => setQcName(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Expected</label>
                <input className="sila-input" value={qcExpected} onChange={(e) => setQcExpected(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Actual Value</label>
                <input className="sila-input" value={qcActual} onChange={(e) => setQcActual(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Result</label>
                <select className="sila-select" value={qcPassed ? "PASS" : "FAIL"} onChange={(e) => setQcPassed(e.target.value === "PASS")}>
                  <option value="PASS">PASS</option>
                  <option value="FAIL">FAIL (Hold Batch)</option>
                </select>
              </div>
            </div>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              disabled={!selectedBatchId}
              onClick={async () => {
                await recordProductionQualityCheck({
                  productionBatchId: selectedBatchId,
                  checkpointName: qcName,
                  expectedValue: qcExpected,
                  actualValue: qcActual,
                  measurementUom: "C",
                  passed: qcPassed,
                });
                toastService.success(qcPassed ? "Quality checkpoint passed." : "Batch placed ON_HOLD for corrective action.");
                await loadAll();
              }}
            >
              Record Quality Checkpoint
            </button>

            {traceability && (
              <div style={{ marginTop: 14, padding: 10, background: "#f8fafc", borderRadius: 8, fontSize: 12 }}>
                <div><strong>Batch Traceability ({traceability.batchNumber})</strong></div>
                <div>Backward: {traceability.backwardTraceSummary}</div>
                <div>Forward: {traceability.forwardTraceSummary}</div>
                <pre style={{ marginTop: 6, whiteSpace: "pre-wrap" }}>{traceability.qrPayloadJson}</pre>
                {traceability.qualityResults.filter((q) => !q.passed && !q.supervisorApproved).map((q) => (
                  <button
                    key={q.id}
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    onClick={async () => {
                      await resolveQualityHold(q.id, "Re-verified temperature & corrective action approved", true);
                      await loadAll();
                    }}
                  >
                    Supervisor Approve Corrective Release ({q.checkpointName})
                  </button>
                ))}
              </div>
            )}
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Production Exceptions</h3>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Exception Type</label>
                <select className="sila-select" value={excType} onChange={(e) => setExcType(e.target.value)}>
                  <option value="PRODUCTION_DELAY">PRODUCTION_DELAY</option>
                  <option value="INGREDIENT_UNAVAILABLE">INGREDIENT_UNAVAILABLE</option>
                  <option value="EQUIPMENT_BREAKDOWN">EQUIPMENT_BREAKDOWN</option>
                  <option value="QUALITY_CHECKPOINT_FAILURE">QUALITY_CHECKPOINT_FAILURE</option>
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label">Description</label>
                <input className="sila-input" value={excDesc} onChange={(e) => setExcDesc(e.target.value)} placeholder="Describe issue..." />
              </div>
            </div>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--secondary"
              onClick={async () => {
                if (!excDesc.trim()) return;
                await reportProductionException({
                  orderId: selectedOrderId || undefined,
                  batchId: selectedBatchId || undefined,
                  exceptionType: excType,
                  severity: "HIGH",
                  description: excDesc,
                });
                setExcDesc("");
                await loadAll();
              }}
            >
              Report Exception
            </button>
            <div style={{ marginTop: 10 }}>
              {exceptions.map((e) => (
                <div key={e.id} style={{ display: "flex", justifyContent: "space-between", padding: "6px 0", borderBottom: "1px solid #f1f5f9", fontSize: 13 }}>
                  <span><strong>{e.exceptionCode}</strong> ({e.severity}) — {e.description} [{e.resolutionStatus}]</span>
                  {e.resolutionStatus === "OPEN" && (
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => { await resolveProductionException(e.id, "RESOLVED", "Resolved operationally"); await loadAll(); }}>
                      Resolve
                    </button>
                  )}
                </div>
              ))}
            </div>
          </section>
        </div>
      )}

      {tab === "scheduler" && (
        <div data-testid="prod-panel-scheduler">
          <div className="cs2-grid-2" style={{ marginBottom: 14 }}>
            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">AI Production Scheduling Engine (HiGHS MILP / CP-SAT)</h3>
                <span className="cs2-badge cs2-badge--live">Food Safety Hard-Constrained</span>
              </div>
              <div style={{ display: "flex", gap: 8, alignItems: "center", flexWrap: "wrap" }}>
                <select className="sila-select" value={scheduleObjective} onChange={(e) => setScheduleObjective(e.target.value)}>
                  <option value="MIN_DELAY">Minimize Service Delays</option>
                  <option value="MAX_UTILIZATION">Maximize Equipment Utilization</option>
                  <option value="BALANCE_WORKLOAD">Balance Kitchen Workload</option>
                  <option value="REDUCE_EARLY_PRODUCTION">Reduce Early Production &amp; Waste</option>
                </select>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={async () => {
                    const res = await optimizeProductionSchedule({ objective: scheduleObjective, enforceFoodSafety: true });
                    setScheduleResult(res);
                  }}
                >
                  Run AI Schedule Optimizer
                </button>
                {scheduleResult && scheduleResult.assignments.length > 0 && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    onClick={async () => {
                      await applyProductionSchedule(scheduleResult.assignments);
                      toastService.success("Applied AI-recommended schedule.");
                      await loadAll();
                    }}
                  >
                    Approve &amp; Apply Schedule
                  </button>
                )}
              </div>
              {scheduleResult && (
                <div style={{ marginTop: 10, fontSize: 13 }}>
                  <div>Status: <strong>{scheduleResult.status}</strong> · Solver: <code>{scheduleResult.solverUsed}</code></div>
                  {scheduleResult.conflictingConstraints.map((c) => <div key={c} style={{ color: "#b91c1c" }}>Conflict: {c}</div>)}
                  {scheduleResult.bottlenecks.map((b) => <div key={b}>Bottleneck: {b}</div>)}
                </div>
              )}
            </section>

            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">AI Kitchen Production Copilot</h3>
                <span className="cs2-badge cs2-badge--indigo">Read-Only Guarded</span>
              </div>
              <div style={{ display: "flex", gap: 6 }}>
                <input className="sila-input" style={{ flex: 1 }} value={copilotQ} onChange={(e) => setCopilotQ(e.target.value)} />
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={async () => {
                    try {
                      setCopilotAns(await queryProductionCopilot(copilotQ));
                    } catch (err: unknown) {
                      toastService.error(err instanceof Error ? err.message : "Query blocked.");
                    }
                  }}
                >
                  Ask Copilot
                </button>
              </div>
              {copilotAns && (
                <div style={{ marginTop: 10, fontSize: 13 }}>
                  <div><strong>Answer:</strong> {copilotAns.answer}</div>
                  <div><strong>Action:</strong> {copilotAns.recommendedAction}</div>
                </div>
              )}
            </section>
          </div>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Kitchens, Workstations, Equipment &amp; Staff Master</h3>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Kitchen Code / Name</label>
                <div style={{ display: "flex", gap: 6 }}>
                  <input className="sila-input" value={kCode} onChange={(e) => setKCode(e.target.value)} style={{ width: 110 }} />
                  <input className="sila-input" value={kName} onChange={(e) => setKName(e.target.value)} />
                </div>
              </div>
              <div className="sila-field">
                <label className="sila-label">Kitchen Type</label>
                <select className="sila-select" value={kType} onChange={(e) => setKType(e.target.value)}>
                  {KITCHEN_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label">Daily Capacity (Portions)</label>
                <input className="sila-input" type="number" value={kCap} onChange={(e) => setKCap(Number(e.target.value))} />
              </div>
              <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={async () => {
                    await upsertCulinaryKitchen({ kitchenCode: kCode, kitchenName: kName, kitchenType: kType, dailyPortionCapacity: kCap });
                    toastService.success("Kitchen saved.");
                    await loadAll();
                  }}
                >
                  Save Kitchen
                </button>
              </div>
            </div>

            {kitchens.length > 0 && (
              <div className="cs2-form-grid" style={{ marginTop: 12 }}>
                <div className="sila-field">
                  <label className="sila-label">Add Workstation ({workstations.length})</label>
                  <div style={{ display: "flex", gap: 6 }}>
                    <input className="sila-input" value={wsCode} onChange={(e) => setWsCode(e.target.value)} style={{ width: 90 }} />
                    <input className="sila-input" value={wsName} onChange={(e) => setWsName(e.target.value)} />
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await upsertCulinaryWorkstation({ kitchenId: selectedKitchenId || kitchens[0].id, workstationCode: wsCode, workstationName: wsName, workstationType: "COOKING", concurrentCapacity: 2 }); await loadAll(); }}>Add WS</button>
                  </div>
                </div>
                <div className="sila-field">
                  <label className="sila-label">Add Equipment ({equipment.length})</label>
                  <div style={{ display: "flex", gap: 6 }}>
                    <input className="sila-input" value={eqCode} onChange={(e) => setEqCode(e.target.value)} style={{ width: 90 }} />
                    <input className="sila-input" value={eqName} onChange={(e) => setEqName(e.target.value)} />
                    <input className="sila-input" type="number" value={eqCap} onChange={(e) => setEqCap(Number(e.target.value))} style={{ width: 70 }} />
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await upsertCulinaryEquipment({ kitchenId: selectedKitchenId || kitchens[0].id, equipmentCode: eqCode, equipmentName: eqName, equipmentType: "COMBI_OVEN", batchCapacity: eqCap, capacityUom: "PORTION" }); await loadAll(); }}>Add Eq</button>
                  </div>
                </div>
                <div className="sila-field">
                  <label className="sila-label">Assign Chef / Staff ({staff.length})</label>
                  <div style={{ display: "flex", gap: 6 }}>
                    <input className="sila-input" value={staffName} onChange={(e) => setStaffName(e.target.value)} />
                    <select className="sila-select" value={staffRole} onChange={(e) => setStaffRole(e.target.value)}>
                      <option value="EXECUTIVE_CHEF">EXECUTIVE_CHEF</option>
                      <option value="SOUS_CHEF">SOUS_CHEF</option>
                      <option value="CHEF_DE_PARTIE">CHEF_DE_PARTIE</option>
                      <option value="PASTRY_CHEF">PASTRY_CHEF</option>
                      <option value="STOREKEEPER">STOREKEEPER</option>
                      <option value="QUALITY_INSPECTOR">QUALITY_INSPECTOR</option>
                    </select>
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => { await upsertCulinaryStaff({ kitchenId: selectedKitchenId || kitchens[0].id, staffName, culinaryRole: staffRole, shiftCode: "AM", skillClassification: "HOT_PRODUCTION", isAvailable: true }); await loadAll(); }}>Assign</button>
                  </div>
                </div>
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
};

export default CulinaryProductionStudioWorkspace;
