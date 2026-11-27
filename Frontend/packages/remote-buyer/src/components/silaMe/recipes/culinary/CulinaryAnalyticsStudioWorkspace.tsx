import React, { useEffect, useState } from "react";
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ReferenceLine,
  ResponsiveContainer,
  Scatter,
  ScatterChart,
  Tooltip,
  XAxis,
  YAxis,
  ZAxis,
} from "recharts";
import { toastService } from "@vosox/shared-ui";
import {
  analyzeConsumptionVariance,
  analyzeIngredientPriceImpact,
  analyzeIngredientSubstitutions,
  analyzeMenuEngineering,
  benchmarkProperties,
  createVarianceInvestigationTask,
  decideCulinaryRecommendation,
  duplicateCulinarySimulation,
  generateCulinaryRecommendations,
  generateDemandForecast,
  getCulinaryAnalyticsConfig,
  getExecutiveCulinaryDashboard,
  getWasteIntelligence,
  listCulinaryRecommendations,
  listCulinarySimulations,
  optimizeBuffetProduction,
  optimizeCulinaryMenu,
  optimizeEventMenus,
  queryCulinaryCopilot,
  recordCulinaryWaste,
  runCulinarySimulation,
  saveCulinaryAnalyticsConfig,
  type SilaCulinaryAnalyticsConfig,
  type SilaCulinaryBenchmarkResponse,
  type SilaCulinaryBuffetOptimizeResponse,
  type SilaCulinaryConsumptionVarianceResponse,
  type SilaCulinaryCopilotResponse,
  type SilaCulinaryEventOptimizeResponse,
  type SilaCulinaryExecutiveDashboard,
  type SilaCulinaryExecutiveFilter,
  type SilaCulinaryForecastResponse,
  type SilaCulinaryMenuEngineeringItem,
  type SilaCulinaryMenuEngineeringResponse,
  type SilaCulinaryOptimizationResult,
  type SilaCulinaryRecommendation,
  type SilaCulinaryReversePriceImpactResponse,
  type SilaCulinarySimulationRecord,
  type SilaCulinarySubstitutionResponse,
  type SilaCulinaryWasteIntelligence,
} from "../../../../api/silaMe/silaCulinaryAnalyticsApi";
import { CulinaryKpis, CulinaryTabs, CulinaryWorkspaceHeader } from "./CulinaryWorkspaceKit";
import "./CulinarySuite.css";

export type AnalyticsSubTab =
  | "executive"
  | "menu-engineering"
  | "variance"
  | "simulation"
  | "optimization"
  | "forecasting"
  | "benchmarking"
  | "cockpit"
  | "copilot"
  | "config";

const QUADRANT_COLORS: Record<string, string> = {
  STAR: "#10b981",
  PLOWHORSE: "#f59e0b",
  PUZZLE: "#6366f1",
  DOG: "#ef4444",
  INSUFFICIENT_DATA: "#64748b",
};

const ANALYTICS_SUBTABS: Array<{ id: AnalyticsSubTab; label: string }> = [
  { id: "executive", label: "Executive Dashboard" },
  { id: "menu-engineering", label: "Menu Engineering & Profitability" },
  { id: "variance", label: "Consumption Variance & Price Impact" },
  { id: "simulation", label: "What-If Simulation" },
  { id: "optimization", label: "Optimization, Buffet & Events" },
  { id: "forecasting", label: "Forecasting & Waste" },
  { id: "benchmarking", label: "Property Benchmarking" },
  { id: "cockpit", label: "Recommendation Cockpit" },
  { id: "copilot", label: "Culinary Copilot" },
  { id: "config", label: "Configuration" },
];

const CulinaryAnalyticsStudioWorkspace: React.FC = () => {
  const [subTab, setSubTab] = useState<AnalyticsSubTab>("executive");
  const [loading, setLoading] = useState(false);
  const [errorMsg, setErrorMsg] = useState<string | null>(null);

  const [filter, setFilter] = useState<SilaCulinaryExecutiveFilter>({
    currency: "AED",
    costMethodology: "STANDARD",
    menuCategory: "",
    mealPeriod: "",
  });

  const [dashboard, setDashboard] = useState<SilaCulinaryExecutiveDashboard | null>(null);
  const [meData, setMeData] = useState<SilaCulinaryMenuEngineeringResponse | null>(null);
  const [selectedDish, setSelectedDish] = useState<SilaCulinaryMenuEngineeringItem | null>(null);
  const [varianceData, setVarianceData] = useState<SilaCulinaryConsumptionVarianceResponse | null>(null);
  const [priceCode, setPriceCode] = useState("");
  const [priceSimNew, setPriceSimNew] = useState("");
  const [priceImpact, setPriceImpact] = useState<SilaCulinaryReversePriceImpactResponse | null>(null);

  const [simList, setSimList] = useState<SilaCulinarySimulationRecord[]>([]);
  const [simName, setSimName] = useState("Q1 Commodity Price Shock (+12%)");
  const [simShockPct, setSimShockPct] = useState(12);
  const [simSellDeltaPct, setSimSellDeltaPct] = useState(4);
  const [simCovers, setSimCovers] = useState(500);
  const [activeSim, setActiveSim] = useState<SilaCulinarySimulationRecord | null>(null);

  const [optObjective, setOptObjective] = useState("MAX_CONTRIBUTION");
  const [optPortions, setOptPortions] = useState(200);
  const [optTargetFc, setOptTargetFc] = useState(30);
  const [optAllergens, setOptAllergens] = useState("");
  const [optResult, setOptResult] = useState<SilaCulinaryOptimizationResult | null>(null);

  const [subMaterialCode, setSubMaterialCode] = useState("");
  const [subResult, setSubResult] = useState<SilaCulinarySubstitutionResponse | null>(null);
  const [buffetMeal, setBuffetMeal] = useState("DINNER");
  const [buffetCovers, setBuffetCovers] = useState(220);
  const [buffetResult, setBuffetResult] = useState<SilaCulinaryBuffetOptimizeResponse | null>(null);
  const [eventGuests, setEventGuests] = useState(180);
  const [eventResult, setEventResult] = useState<SilaCulinaryEventOptimizeResponse | null>(null);

  const [horizonDays, setHorizonDays] = useState(14);
  const [forecastData, setForecastData] = useState<SilaCulinaryForecastResponse | null>(null);
  const [wasteData, setWasteData] = useState<SilaCulinaryWasteIntelligence | null>(null);
  const [wasteIng, setWasteIng] = useState("");
  const [wasteQty, setWasteQty] = useState(2.5);
  const [wasteCost, setWasteCost] = useState(45);
  const [wasteCat, setWasteCat] = useState("OVERPRODUCTION");

  const [benchmarkData, setBenchmarkData] = useState<SilaCulinaryBenchmarkResponse | null>(null);
  const [recs, setRecs] = useState<SilaCulinaryRecommendation[]>([]);
  const [copilotQ, setCopilotQ] = useState("Why has food cost increased and which dishes should we revise?");
  const [copilotResp, setCopilotResp] = useState<SilaCulinaryCopilotResponse | null>(null);
  const [config, setConfig] = useState<SilaCulinaryAnalyticsConfig | null>(null);

  const loadExecutive = async () => {
    setLoading(true);
    setErrorMsg(null);
    try {
      const [d, me] = await Promise.all([
        getExecutiveCulinaryDashboard(filter),
        analyzeMenuEngineering(filter),
      ]);
      setDashboard(d);
      setMeData(me);
      if (me.items.length > 0 && !selectedDish) setSelectedDish(me.items[0]);
    } catch (err: unknown) {
      setErrorMsg(err instanceof Error ? err.message : "Failed to load Phase 3B culinary intelligence.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadExecutive();
  }, []);

  useEffect(() => {
    if (subTab === "variance" && !varianceData) {
      analyzeConsumptionVariance(filter).then(setVarianceData).catch((e) => toastService.error(e.message));
    } else if (subTab === "simulation" && simList.length === 0) {
      listCulinarySimulations(filter.propertyId).then((list) => {
        setSimList(list);
        if (list.length > 0) setActiveSim(list[0]);
      }).catch(() => {});
    } else if (subTab === "forecasting") {
      if (!forecastData) generateDemandForecast({ horizonDays, occupancyAssumptionPct: 82 }).then(setForecastData).catch(() => {});
      if (!wasteData) getWasteIntelligence(filter.propertyId).then(setWasteData).catch(() => {});
    } else if (subTab === "benchmarking" && !benchmarkData) {
      benchmarkProperties(filter.currency).then(setBenchmarkData).catch((e) => setErrorMsg(e.message));
    } else if (subTab === "cockpit" && recs.length === 0) {
      listCulinaryRecommendations().then(setRecs).catch(() => {});
    } else if (subTab === "config" && !config) {
      getCulinaryAnalyticsConfig(filter.propertyId).then(setConfig).catch(() => {});
    }
  }, [subTab]);

  const ccy = dashboard?.currency || filter.currency || "AED";

  return (
    <div className="cs2-shell" data-testid="culinary-analytics-workspace">
      <CulinaryWorkspaceHeader
        title="Culinary Intelligence Analytics"
        subtitle="Enterprise Menu Engineering, Consumption Variance, Isolated What-If Simulation, Optimization, Demand Forecasting, Benchmarking & Grounded Copilot."
        actions={
          <>
            <span className="cs2-badge cs2-badge--live">Live Analytics</span>
            <button type="button" className="sila-studio-btn sila-studio-btn--primary" disabled={loading} onClick={() => void loadExecutive()}>
              {loading ? "Refreshing..." : "Refresh Analytics"}
            </button>
          </>
        }
      />

      <CulinaryTabs<AnalyticsSubTab>
        ariaLabel="Analytics modules"
        testId="analytics-tabs"
        active={subTab}
        onChange={setSubTab}
        tabs={ANALYTICS_SUBTABS}
      />

      {/* Global Filters */}
      <section className="cs2-card" style={{ padding: "12px 16px" }}>
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label">Category Filter</label>
            <input
              className="sila-input"
              placeholder="All categories (e.g. Main)"
              value={filter.menuCategory ?? ""}
              onChange={(e) => setFilter((f) => ({ ...f, menuCategory: e.target.value }))}
            />
          </div>
          <div className="sila-field">
            <label className="sila-label">Meal Period</label>
            <select className="sila-select" value={filter.mealPeriod ?? ""} onChange={(e) => setFilter((f) => ({ ...f, mealPeriod: e.target.value }))}>
              <option value="">All Meal Periods</option>
              <option value="Breakfast">Breakfast</option>
              <option value="Lunch">Lunch</option>
              <option value="Dinner">Dinner</option>
              <option value="Brunch">Brunch</option>
              <option value="Banquet">Banquet / Events</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label">Costing Methodology</label>
            <select className="sila-select" value={filter.costMethodology ?? "STANDARD"} onChange={(e) => setFilter((f) => ({ ...f, costMethodology: e.target.value }))}>
              <option value="STANDARD">Standard Recipe Cost</option>
              <option value="ACTUAL">Actual Ingredient Cost</option>
              <option value="WEIGHTED_AVERAGE">Weighted Average Cost</option>
              <option value="LATEST_PURCHASE">Latest Purchase Price</option>
              <option value="CONTRACT">Contract Price</option>
              <option value="FORECAST">Forecast Cost</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label">Reporting Currency</label>
            <select className="sila-select" value={filter.currency ?? "AED"} onChange={(e) => setFilter((f) => ({ ...f, currency: e.target.value }))}>
              <option value="AED">AED</option>
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
              <option value="INR">INR</option>
            </select>
          </div>
          <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => void loadExecutive()}>
              Apply Filters
            </button>
          </div>
        </div>
      </section>

      {errorMsg && (
        <div className="cs2-banner" role="alert" style={{ borderColor: "#ef4444", background: "#fef2f2" }}>
          <div><strong>Notice:</strong> {errorMsg}</div>
        </div>
      )}

      {/* 1. EXECUTIVE DASHBOARD */}
      {subTab === "executive" && (
        <>
          <CulinaryKpis
            ariaLabel="Executive culinary analytics metrics"
            testId="analytics-kpis"
            columns={4}
            items={[
              { id: "an-revenue", label: "Total F&B Revenue", value: `${ccy} ${(dashboard?.totalFbRevenue ?? 0).toLocaleString()}`, tone: "blue", drillLabel: "Open menu engineering and profitability", onClick: () => setSubTab("menu-engineering") },
              { id: "an-food-cost", label: "Total Food Cost", value: `${ccy} ${(dashboard?.totalFoodCost ?? 0).toLocaleString()}`, tone: "sky", drillLabel: "Open consumption variance and price impact", onClick: () => setSubTab("variance") },
              { id: "an-food-cost-pct", label: "Food Cost % vs Target", value: dashboard?.foodCostPercent != null ? `${dashboard.foodCostPercent}%` : "No POS Data", hint: `Target: ${dashboard?.targetFoodCostPercent ?? 28}%`, tone: "green", drillLabel: "Open consumption variance", onClick: () => setSubTab("variance") },
              { id: "an-margin", label: "Contribution Margin", value: `${ccy} ${(dashboard?.contributionMargin ?? 0).toLocaleString()}`, tone: "green", drillLabel: "Open menu engineering and profitability", onClick: () => setSubTab("menu-engineering") },
              { id: "an-cover", label: "Cost / Revenue per Cover", value: `${dashboard?.costPerCover != null ? `${ccy} ${dashboard.costPerCover}` : "—"} / ${dashboard?.revenuePerCover != null ? `${ccy} ${dashboard.revenuePerCover}` : "—"}`, tone: "lavender", drillLabel: "Open menu engineering and profitability", onClick: () => setSubTab("menu-engineering") },
              { id: "an-waste", label: "Waste Cost & %", value: `${ccy} ${(dashboard?.wasteCost ?? 0).toLocaleString()}`, hint: dashboard?.wastePercent != null ? `${dashboard.wastePercent}% of food cost` : "No waste logged", tone: "amber", drillLabel: "Open forecasting and waste", onClick: () => setSubTab("forecasting") },
              { id: "an-event", label: "Event Profitability (7d)", value: `${ccy} ${(dashboard?.eventProfitability ?? 0).toLocaleString()}`, hint: `Forecast covers: ${(dashboard?.forecastDemandCovers ?? 0).toLocaleString()}`, tone: "indigo", drillLabel: "Open optimization, buffet and events", onClick: () => setSubTab("optimization") },
              { id: "an-savings", label: "Savings Pipeline", value: `${ccy} ${(dashboard?.potentialSavingsPipeline ?? 0).toLocaleString()}`, hint: `Realized: ${ccy} ${(dashboard?.realizedSavings ?? 0).toLocaleString()}`, tone: "rose", drillLabel: "Open recommendation cockpit", onClick: () => setSubTab("cockpit") },
            ]}
          />

          {dashboard?.dataAvailabilityNotes && dashboard.dataAvailabilityNotes.length > 0 && (
            <div className="cs2-banner" role="status">
              <div>
                <strong>Data Readiness Indicators:</strong> {dashboard.dataAvailabilityNotes.join(" · ")}
              </div>
            </div>
          )}

          <div className="cs2-grid-2">
            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Menu Engineering Quadrant Distribution</h3>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => setSubTab("menu-engineering")}>
                  Drill Down
                </button>
              </div>
              <div style={{ width: "100%", height: 240 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={Object.entries(dashboard?.quadrantCounts ?? {}).map(([quadrant, count]) => ({ quadrant, count }))}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                    <XAxis dataKey="quadrant" />
                    <YAxis allowDecimals={false} />
                    <Tooltip />
                    <Bar dataKey="count" name="Menu Items">
                      {Object.keys(dashboard?.quadrantCounts ?? {}).map((q) => (
                        <Cell key={q} fill={QUADRANT_COLORS[q] ?? "#6366f1"} />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </section>

            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Top Profitable vs Low Margin Menu Items</h3>
              </div>
              <table className="sila-table" style={{ width: "100%", fontSize: 12 }}>
                <thead>
                  <tr>
                    <th>Dish</th>
                    <th>Quadrant</th>
                    <th>FC %</th>
                    <th>CM / Item</th>
                    <th>Total CM</th>
                  </tr>
                </thead>
                <tbody>
                  {(meData?.items ?? []).slice(0, 6).map((item) => (
                    <tr key={item.menuItemId}>
                      <td><strong>{item.name}</strong></td>
                      <td><span className="cs2-badge">{item.quadrant}</span></td>
                      <td>{item.foodCostPercent != null ? `${item.foodCostPercent}%` : "—"}</td>
                      <td>{item.contributionMarginPerItem != null ? `${ccy} ${item.contributionMarginPerItem}` : "—"}</td>
                      <td>{item.totalContributionMargin != null ? `${ccy} ${item.totalContributionMargin}` : "—"}</td>
                    </tr>
                  ))}
                  {(!meData || meData.items.length === 0) && (
                    <tr><td colSpan={5}>No costed menu items with sales history yet.</td></tr>
                  )}
                </tbody>
              </table>
            </section>
          </div>
        </>
      )}

      {/* 2. MENU ENGINEERING & PROFITABILITY */}
      {subTab === "menu-engineering" && (
        <div className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">4-Quadrant Menu Engineering Matrix</h3>
              <span className="cs2-badge cs2-badge--indigo">
                Pop ≥ {meData?.popularityThresholdPercent ?? 0}% · CM ≥ {ccy} {meData?.contributionMarginThreshold ?? 0}
              </span>
            </div>
            <div style={{ width: "100%", height: 320 }}>
              <ResponsiveContainer width="100%" height="100%">
                <ScatterChart margin={{ top: 10, right: 20, bottom: 20, left: 10 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis type="number" dataKey="popularityPercent" name="Popularity %" unit="%" />
                  <YAxis type="number" dataKey="contributionMarginPerItem" name="CM / Item" unit={` ${ccy}`} />
                  <ZAxis type="number" dataKey="quantitySold" range={[80, 600]} name="Qty Sold" />
                  <Tooltip cursor={{ strokeDasharray: "3 3" }} />
                  {meData && <ReferenceLine x={meData.popularityThresholdPercent} stroke="#6366f1" strokeDasharray="4 4" />}
                  {meData && <ReferenceLine y={meData.contributionMarginThreshold} stroke="#10b981" strokeDasharray="4 4" />}
                  <Scatter
                    name="Menu Items"
                    data={(meData?.items ?? []).filter((x) => x.contributionMarginPerItem != null)}
                    onClick={(node: unknown) => {
                      const item = node as SilaCulinaryMenuEngineeringItem;
                      if (item?.menuItemId) setSelectedDish(item);
                    }}
                  >
                    {(meData?.items ?? []).filter((x) => x.contributionMarginPerItem != null).map((entry) => (
                      <Cell key={entry.menuItemId} fill={QUADRANT_COLORS[entry.quadrant] ?? "#6366f1"} />
                    ))}
                  </Scatter>
                </ScatterChart>
              </ResponsiveContainer>
            </div>
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Menu Item Profitability Drill-Down</h3>
              {selectedDish && <span className="cs2-badge">{selectedDish.quadrant} · v{selectedDish.recipeVersion}</span>}
            </div>
            {selectedDish ? (
              <div style={{ fontSize: 13, lineHeight: 1.8 }}>
                <div><strong>{selectedDish.name}</strong> ({selectedDish.menuItemCode}) — {selectedDish.category}</div>
                <div>Gross Price: <strong>{ccy} {selectedDish.grossSellingPrice}</strong> · Discounts: {ccy} {selectedDish.discountAmount} · Net Selling Price: <strong>{ccy} {selectedDish.netSellingPrice}</strong></div>
                <div>Standard Recipe Cost ({selectedDish.costSource}): <strong>{selectedDish.standardRecipeCost != null ? `${ccy} ${selectedDish.standardRecipeCost}` : "Uncosted"}</strong></div>
                <div>Garnish: {ccy} {selectedDish.garnishCost} · Packaging: {ccy} {selectedDish.packagingCost} · Direct Labor: {ccy} {selectedDish.directLaborCost}</div>
                <div>Contribution Margin (Variable Only): <strong>{selectedDish.contributionMarginPerItem != null ? `${ccy} ${selectedDish.contributionMarginPerItem} (${selectedDish.contributionMarginPercent}%)` : "—"}</strong></div>
                <div>Allocated Fixed Overhead (Separated): {ccy} {selectedDish.allocatedOverhead} · Fully Loaded Profit/Portion: <strong>{selectedDish.profitPerPortion != null ? `${ccy} ${selectedDish.profitPerPortion}` : "—"}</strong></div>
                <div style={{ marginTop: 8, padding: 8, background: "#f8fafc", borderRadius: 6 }}>
                  <strong>Recommended Action:</strong> {selectedDish.recommendedAction}
                </div>
              </div>
            ) : (
              <div className="cs2-empty-note">Select a menu item from the matrix or table to inspect profitability layers.</div>
            )}
          </section>
        </div>
      )}

      {/* 3. CONSUMPTION VARIANCE & REVERSE PRICE IMPACT */}
      {subTab === "variance" && (
        <div className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Theoretical vs Actual Consumption Variance</h3>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => analyzeConsumptionVariance(filter).then(setVarianceData).catch((e) => toastService.error(e.message))}>
                Recalculate
              </button>
            </div>
            <p className="cs2-empty-note">{varianceData?.note ?? "Reconciles returns and excludes internal transfers; recorded wastage is subtracted to prevent double-counting."}</p>
            <table className="sila-table" style={{ width: "100%", fontSize: 12 }}>
              <thead>
                <tr>
                  <th>Material</th>
                  <th>Theo</th>
                  <th>Actual</th>
                  <th>Waste</th>
                  <th>Unexplained</th>
                  <th>Fin. Var</th>
                  <th>Task</th>
                </tr>
              </thead>
              <tbody>
                {(varianceData?.rows ?? []).map((r) => (
                  <tr key={r.materialId}>
                    <td><strong>{r.materialName}</strong> ({r.uom})</td>
                    <td>{r.theoreticalQuantity}</td>
                    <td>{r.reconciledActualQuantity}</td>
                    <td>{r.recordedWastage}</td>
                    <td>{r.unexplainedVariance}</td>
                    <td>{ccy} {r.financialVariance}</td>
                    <td>
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--secondary"
                        onClick={async () => {
                          await createVarianceInvestigationTask({
                            materialCode: r.materialCode,
                            materialName: r.materialName,
                            unexplainedVarianceQty: r.unexplainedVariance,
                            financialImpact: r.financialVariance,
                            note: r.possibleCauses.join("; "),
                          });
                          toastService.success(`Investigation task raised for ${r.materialName}`);
                        }}
                      >
                        Create Task
                      </button>
                    </td>
                  </tr>
                ))}
                {(!varianceData || varianceData.rows.length === 0) && (
                  <tr><td colSpan={7}>No consumption variance records in selected period.</td></tr>
                )}
              </tbody>
            </table>
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Reverse Ingredient Price Impact Analyzer</h3>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Material Code (optional)</label>
                <input className="sila-input" placeholder="e.g. MAT-SALMON" value={priceCode} onChange={(e) => setPriceCode(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Simulated New Unit Price</label>
                <input className="sila-input" type="number" placeholder="Optional override" value={priceSimNew} onChange={(e) => setPriceSimNew(e.target.value)} />
              </div>
              <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  onClick={async () => {
                    const res = await analyzeIngredientPriceImpact({
                      materialCode: priceCode,
                      simulatedNewPrice: priceSimNew ? Number(priceSimNew) : undefined,
                    });
                    setPriceImpact(res);
                  }}
                >
                  Trace Impact
                </button>
              </div>
            </div>
            {priceImpact && (
              <div style={{ marginTop: 12, fontSize: 13 }}>
                <div>
                  <strong>{priceImpact.materialName}</strong>: {ccy} {priceImpact.previousPrice} → {ccy} {priceImpact.currentPrice} ({priceImpact.priceChangePercent ?? 0}%)
                  {" · "}Monthly Impact: <strong>{ccy} {priceImpact.totalMonthlyImpact}</strong> · Annual: <strong>{ccy} {priceImpact.totalAnnualImpact}</strong>
                </div>
                <ul style={{ marginTop: 8, paddingLeft: 18 }}>
                  {priceImpact.impacts.map((imp, idx) => (
                    <li key={`${imp.nodeId}-${idx}`}>
                      [{imp.nodeType}] {imp.nodeName}{imp.viaSubRecipe ? ` (via ${imp.viaSubRecipe})` : ""}: Δ {ccy} {imp.unitCostDelta}/unit · Annual {ccy} {imp.estimatedAnnualImpact}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </section>
        </div>
      )}

      {/* 4. ISOLATED WHAT-IF SIMULATION */}
      {subTab === "simulation" && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Isolated What-If Simulation Engine</h3>
            <span className="cs2-badge cs2-badge--indigo">100% Isolated From Production Masters</span>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Simulation Name</label>
              <input className="sila-input" value={simName} onChange={(e) => setSimName(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Ingredient Price Shock (%)</label>
              <input className="sila-input" type="number" value={simShockPct} onChange={(e) => setSimShockPct(Number(e.target.value))} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Menu Price Adjustment (%)</label>
              <input className="sila-input" type="number" value={simSellDeltaPct} onChange={(e) => setSimSellDeltaPct(Number(e.target.value))} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Expected Covers</label>
              <input className="sila-input" type="number" value={simCovers} onChange={(e) => setSimCovers(Number(e.target.value))} />
            </div>
            <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 8 }}>
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--primary"
                onClick={async () => {
                  const res = await runCulinarySimulation({
                    propertyId: filter.propertyId,
                    name: simName,
                    currency: ccy,
                    baseSellingPricePerCover: 180,
                    baseFoodCostPerCover: 54,
                    baseLaborCost: 15000,
                    baseFixedExpenses: 20000,
                    targetFoodCostPct: 28,
                    saveSnapshot: true,
                    scenarios: [
                      { scenarioName: "Baseline", ingredientPriceDeltaPct: 0, sellingPriceDeltaPct: 0, portionSizeDeltaPct: 0, covers: simCovers, occupancyPct: 80, laborCostDeltaPct: 0, wastagePct: 4, yieldPct: 88, discountPct: 0, overheadAmount: 10000, buffetReplenishmentFactor: 1, substitutionSavingsPct: 0 },
                      { scenarioName: "Scenario A (Price Shock)", ingredientPriceDeltaPct: simShockPct, sellingPriceDeltaPct: 0, portionSizeDeltaPct: 0, covers: simCovers, occupancyPct: 80, laborCostDeltaPct: 0, wastagePct: 4, yieldPct: 88, discountPct: 0, overheadAmount: 10000, buffetReplenishmentFactor: 1, substitutionSavingsPct: 0 },
                      { scenarioName: "Scenario B (Mitigated)", ingredientPriceDeltaPct: simShockPct, sellingPriceDeltaPct: simSellDeltaPct, portionSizeDeltaPct: -3, covers: simCovers, occupancyPct: 82, laborCostDeltaPct: 0, wastagePct: 2.5, yieldPct: 90, discountPct: 0, overheadAmount: 10000, buffetReplenishmentFactor: 0.95, substitutionSavingsPct: 5 },
                    ],
                  });
                  setActiveSim(res);
                  setSimList((prev) => [res, ...prev]);
                  toastService.success(`Saved isolated simulation ${res.simulationCode}`);
                }}
              >
                Run &amp; Save Snapshot
              </button>
              {activeSim && (
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--secondary"
                  onClick={async () => {
                    const dup = await duplicateCulinarySimulation(activeSim.id);
                    setActiveSim(dup);
                    setSimList((prev) => [dup, ...prev]);
                    toastService.success(`Duplicated ${dup.simulationCode}`);
                  }}
                >
                  Duplicate
                </button>
              )}
            </div>
          </div>

          {activeSim && (
            <table className="sila-table" style={{ width: "100%", marginTop: 14, fontSize: 12 }}>
              <thead>
                <tr>
                  <th>Scenario</th>
                  <th>Covers</th>
                  <th>Net Revenue</th>
                  <th>Food Cost</th>
                  <th>FC %</th>
                  <th>Contribution Margin</th>
                  <th>Break-even</th>
                  <th>Savings vs Baseline</th>
                </tr>
              </thead>
              <tbody>
                {activeSim.results.map((r) => (
                  <tr key={r.scenarioName}>
                    <td><strong>{r.scenarioName}</strong></td>
                    <td>{r.covers}</td>
                    <td>{ccy} {r.netRevenue.toLocaleString()}</td>
                    <td>{ccy} {r.revisedFoodCost.toLocaleString()}</td>
                    <td>{r.revisedFoodCostPct ?? "—"}%</td>
                    <td>{ccy} {r.contributionMargin.toLocaleString()}</td>
                    <td>{r.breakEvenCovers ?? "—"}</td>
                    <td>{ccy} {r.expectedSavingsVsBaseline.toLocaleString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}

      {/* 5. PYTHON MILP OPTIMIZATION, SUBSTITUTION, BUFFET & EVENTS */}
      {subTab === "optimization" && (
        <div className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Python MILP Menu Optimization</h3>
              <span className="cs2-badge cs2-badge--live">Hard Allergen Safety</span>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Objective</label>
                <select className="sila-select" value={optObjective} onChange={(e) => setOptObjective(e.target.value)}>
                  <option value="MAX_CONTRIBUTION">Maximize Contribution Margin</option>
                  <option value="MIN_FOOD_COST">Minimize Food Cost</option>
                  <option value="MIN_WASTE">Minimize Ingredient Wastage</option>
                  <option value="MAX_UTILIZATION">Maximize Ingredient Utilization</option>
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label">Target Portions</label>
                <input className="sila-input" type="number" value={optPortions} onChange={(e) => setOptPortions(Number(e.target.value))} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Target Food Cost (%)</label>
                <input className="sila-input" type="number" value={optTargetFc} onChange={(e) => setOptTargetFc(Number(e.target.value))} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Excluded Allergens (comma-sep)</label>
                <input className="sila-input" placeholder="e.g. Peanut, Shellfish" value={optAllergens} onChange={(e) => setOptAllergens(e.target.value)} />
              </div>
            </div>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              style={{ marginTop: 10 }}
              onClick={async () => {
                const res = await optimizeCulinaryMenu({
                  propertyId: filter.propertyId,
                  objective: optObjective,
                  totalTargetPortions: optPortions,
                  targetFoodCostPct: optTargetFc,
                  excludedAllergens: optAllergens.split(",").map((x) => x.trim()).filter(Boolean),
                  requiredDietaryTags: [],
                  strictAllergenSafety: true,
                });
                setOptResult(res);
              }}
            >
              Run Optimization
            </button>
            {optResult && (
              <div style={{ marginTop: 12, fontSize: 13 }}>
                <div><strong>Status:</strong> <span className="cs2-badge">{optResult.status}</span> · {optResult.explanation}</div>
                {optResult.infeasibilityReasons.length > 0 && (
                  <ul style={{ color: "#b91c1c" }}>
                    {optResult.infeasibilityReasons.map((r) => <li key={r}>{r}</li>)}
                  </ul>
                )}
              </div>
            )}
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Controlled Substitutions, Buffet &amp; Event Packages</h3>
            </div>
            <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginBottom: 10 }}>
              <input className="sila-input" style={{ width: 150 }} placeholder="Material Code" value={subMaterialCode} onChange={(e) => setSubMaterialCode(e.target.value)} />
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                onClick={() => analyzeIngredientSubstitutions({ originalMaterialCode: subMaterialCode, requiredQty: 10, excludedAllergens: [], requiredDietaryTags: [] }).then(setSubResult)}
              >
                Check Substitutes
              </button>
              <select className="sila-select" style={{ width: 120 }} value={buffetMeal} onChange={(e) => setBuffetMeal(e.target.value)}>
                <option value="BREAKFAST">Breakfast</option>
                <option value="LUNCH">Lunch</option>
                <option value="DINNER">Dinner</option>
                <option value="BRUNCH">Brunch</option>
              </select>
              <input className="sila-input" style={{ width: 90 }} type="number" title="Buffet Covers" value={buffetCovers} onChange={(e) => setBuffetCovers(Number(e.target.value))} />
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                onClick={() => optimizeBuffetProduction({ propertyId: filter.propertyId, mealPeriod: buffetMeal, expectedCovers: buffetCovers, occupancyPercent: 85 }).then(setBuffetResult)}
              >
                Optimize Buffet ({buffetCovers})
              </button>
              <input className="sila-input" style={{ width: 90 }} type="number" title="Event Guests" value={eventGuests} onChange={(e) => setEventGuests(Number(e.target.value))} />
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--secondary"
                onClick={() => optimizeEventMenus({ guestCount: eventGuests }).then(setEventResult)}
              >
                Generate 5 Event Packages ({eventGuests})
              </button>
            </div>
            {subResult && (
              <div style={{ fontSize: 12, marginBottom: 10 }}>
                <strong>Substitutions for {subResult.originalMaterialName}:</strong>
                {subResult.recommendations.map((s) => (
                  <div key={s.materialId}>• {s.name} [{s.classification}] — Saving {ccy} {s.costSaving} · {s.allergenImpact}</div>
                ))}
              </div>
            )}
            {buffetResult && (
              <div style={{ fontSize: 12, marginBottom: 10 }}>
                <strong>Buffet ({buffetResult.mealPeriod}):</strong> {ccy} {buffetResult.foodCostPerGuest}/guest · {buffetResult.foodSafetyPolicy}
              </div>
            )}
            {eventResult && (
              <div style={{ fontSize: 12 }}>
                <strong>Event Packages ({eventResult.guestCount} guests):</strong>
                {eventResult.packages.map((p) => (
                  <div key={p.packageCode}>• <strong>{p.packageName}</strong>: Sell {ccy} {p.sellingPricePerGuest}/g · CM {p.expectedContributionMarginPct}% · Profit {ccy} {p.estimatedFullyLoadedProfit}</div>
                ))}
              </div>
            )}
          </section>
        </div>
      )}

      {/* 6. DEMAND FORECASTING & FOOD WASTE INTELLIGENCE */}
      {subTab === "forecasting" && (
        <div className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Demand Forecasting &amp; Backtest Quality</h3>
              <select className="sila-select" style={{ width: 130 }} value={horizonDays} onChange={(e) => {
                const h = Number(e.target.value);
                setHorizonDays(h);
                generateDemandForecast({ propertyId: filter.propertyId, horizonDays: h, occupancyAssumptionPct: 82 }).then(setForecastData);
              }}>
                <option value={1}>Next 1 Day</option>
                <option value={7}>Next 7 Days</option>
                <option value={14}>Next 14 Days</option>
                <option value={30}>Next 30 Days</option>
              </select>
            </div>
            <div style={{ fontSize: 12, marginBottom: 8 }}>
              Model: <strong>{forecastData?.modelName}</strong> · Quality: <strong>{forecastData?.dataQuality}</strong> · MAE: <strong>{forecastData?.mae ?? "Cold-Start"}</strong> · WAPE: <strong>{forecastData?.wapePercent != null ? `${forecastData.wapePercent}%` : "—"}</strong>
            </div>
            <div style={{ width: "100%", height: 240 }}>
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={forecastData?.forecastPoints ?? []}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="date" />
                  <YAxis />
                  <Tooltip />
                  <Area type="monotone" dataKey="upperBound80" stroke="#93c5fd" fill="#dbeafe" name="80% Upper" />
                  <Area type="monotone" dataKey="forecastCovers" stroke="#087bb5" fill="#dceeff" name="Forecast Covers" />
                </AreaChart>
              </ResponsiveContainer>
            </div>
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Food Waste Intelligence (Approval-Gated)</h3>
              <span className="cs2-badge">{ccy} {wasteData?.totalWasteCost ?? 0}</span>
            </div>
            <div className="cs2-form-grid">
              <input className="sila-input" placeholder="Ingredient name" value={wasteIng} onChange={(e) => setWasteIng(e.target.value)} />
              <select className="sila-select" value={wasteCat} onChange={(e) => setWasteCat(e.target.value)}>
                <option value="PREPARATION_WASTE">Preparation Waste</option>
                <option value="PRODUCTION_WASTE">Production Waste</option>
                <option value="OVERPRODUCTION">Overproduction</option>
                <option value="BUFFET_LEFTOVERS">Buffet Leftovers</option>
                <option value="PLATE_WASTE">Plate Waste</option>
                <option value="SPOILAGE">Spoilage</option>
                <option value="EXPIRED_INGREDIENTS">Expired Ingredients</option>
                <option value="STORAGE_DAMAGE">Storage Damage</option>
                <option value="YIELD_LOSS">Yield Loss</option>
                <option value="UNCLASSIFIED_WASTE">Unclassified Waste</option>
              </select>
              <input className="sila-input" type="number" placeholder="Qty (KG)" value={wasteQty} onChange={(e) => setWasteQty(Number(e.target.value))} />
              <input className="sila-input" type="number" placeholder="Cost" value={wasteCost} onChange={(e) => setWasteCost(Number(e.target.value))} />
              <button
                type="button"
                className="sila-btn sila-btn--sm sila-btn--primary"
                onClick={async () => {
                  if (!wasteIng.trim()) return;
                  await recordCulinaryWaste({
                    propertyId: filter.propertyId,
                    mealPeriod: filter.mealPeriod || "Dinner",
                    wasteCategory: wasteCat,
                    ingredientName: wasteIng,
                    quantity: wasteQty,
                    uom: "KG",
                    estimatedCost: wasteCost,
                    coversServed: 100,
                    sourceChannel: "CLOUD",
                  });
                  toastService.success("Waste logged (pending approval; no automatic stock adjustment posted).");
                  setWasteIng("");
                  getWasteIntelligence(filter.propertyId).then(setWasteData);
                }}
              >
                Log Waste
              </button>
            </div>
            <ul style={{ fontSize: 12, marginTop: 10, paddingLeft: 18 }}>
              {(wasteData?.recommendations ?? []).map((r) => <li key={r}>{r}</li>)}
            </ul>
          </section>
        </div>
      )}

      {/* 7. CROSS-PROPERTY BENCHMARKING */}
      {subTab === "benchmarking" && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Authorized Cross-Property Benchmarking ({benchmarkData?.reportingCurrency ?? ccy})</h3>
          </div>
          <table className="sila-table" style={{ width: "100%", fontSize: 12 }}>
            <thead>
              <tr>
                <th>Property</th>
                <th>Format</th>
                <th>Revenue</th>
                <th>Food Cost</th>
                <th>FC %</th>
                <th>CM %</th>
                <th>Waste %</th>
                <th>Event Margin %</th>
              </tr>
            </thead>
            <tbody>
              {(benchmarkData?.rows ?? []).map((r) => (
                <tr key={r.propertyId}>
                  <td><strong>{r.propertyName}</strong></td>
                  <td>{r.outletType}</td>
                  <td>{ccy} {r.revenueReporting ?? 0}</td>
                  <td>{ccy} {r.foodCostReporting ?? 0}</td>
                  <td>{r.foodCostPct ?? "—"}%</td>
                  <td>{r.contributionMarginPct ?? "—"}%</td>
                  <td>{r.wastePct ?? "—"}%</td>
                  <td>{r.eventMarginPct ?? "—"}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {/* 8. RECOMMENDATION COCKPIT */}
      {subTab === "cockpit" && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Culinary Recommendation Cockpit</h3>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              onClick={async () => {
                const list = await generateCulinaryRecommendations(filter.propertyId);
                setRecs(list);
                toastService.success("Recommendation pipeline refreshed (deduplicated).");
              }}
            >
              Generate Deduplicated Recommendations
            </button>
          </div>
          <table className="sila-table" style={{ width: "100%", fontSize: 12 }}>
            <thead>
              <tr>
                <th>Code</th>
                <th>Recommendation</th>
                <th>Category</th>
                <th>Expected Impact</th>
                <th>Realized</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {recs.map((r) => (
                <tr key={r.id}>
                  <td>{r.recommendationCode}</td>
                  <td><strong>{r.title}</strong><div className="cs2-empty-note">{r.proposedAction}</div></td>
                  <td>{r.category}</td>
                  <td>{ccy} {r.expectedFinancialImpact}</td>
                  <td>{r.realizedFinancialImpact != null ? `${ccy} ${r.realizedFinancialImpact}` : "—"}</td>
                  <td><span className="cs2-badge">{r.status}</span></td>
                  <td style={{ display: "flex", gap: 4 }}>
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={async () => {
                      const updated = await decideCulinaryRecommendation(r.id, { status: "APPROVED", comment: "Approved via Cockpit" });
                      setRecs((prev) => prev.map((x) => (x.id === r.id ? updated : x)));
                    }}>Approve</button>
                    <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={async () => {
                      const updated = await decideCulinaryRecommendation(r.id, { status: "IMPLEMENTED", realizedFinancialImpact: r.expectedFinancialImpact });
                      setRecs((prev) => prev.map((x) => (x.id === r.id ? updated : x)));
                    }}>Mark Implemented</button>
                  </td>
                </tr>
              ))}
              {recs.length === 0 && <tr><td colSpan={7}>No recommendations yet. Click Generate Deduplicated Recommendations.</td></tr>}
            </tbody>
          </table>
        </section>
      )}

      {/* 9. CULINARY COPILOT */}
      {subTab === "copilot" && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Grounded Culinary AI Copilot</h3>
            <span className="cs2-badge cs2-badge--live">Whitelisted Tools · Injection Shield</span>
          </div>
          <div style={{ display: "flex", gap: 8 }}>
            <input className="sila-input" style={{ flex: 1 }} value={copilotQ} onChange={(e) => setCopilotQ(e.target.value)} />
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              onClick={async () => {
                const res = await queryCulinaryCopilot({ question: copilotQ, contextModule: "EXECUTIVE_DASHBOARD", propertyId: filter.propertyId });
                setCopilotResp(res);
              }}
            >
              Ask Copilot
            </button>
          </div>
          {copilotResp && (
            <div style={{ marginTop: 14, padding: 12, borderRadius: 8, background: copilotResp.promptInjectionBlocked ? "#fef2f2" : "#f8fafc" }}>
              <div><strong>Answer:</strong> {copilotResp.directAnswer}</div>
              <div style={{ fontSize: 12, marginTop: 6 }}>
                <strong>Metrics:</strong> {copilotResp.supportingMetrics.join(" · ")}
              </div>
              <div style={{ fontSize: 12, marginTop: 4 }}>
                <strong>Source:</strong> {copilotResp.dataSource} ({copilotResp.dateRange}) · <strong>Action:</strong> {copilotResp.recommendedAction} · <strong>Impact:</strong> {copilotResp.estimatedFinancialImpact}
              </div>
            </div>
          )}
        </section>
      )}

      {/* 10. CONFIGURATION */}
      {subTab === "config" && config && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Culinary Intelligence Configuration (v{config.configVersion})</h3>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Target Food Cost %</label>
              <input className="sila-input" type="number" value={config.targetFoodCostPercent} onChange={(e) => setConfig({ ...config, targetFoodCostPercent: Number(e.target.value) })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Popularity Threshold Factor</label>
              <input className="sila-input" type="number" step="0.05" value={config.popularityThresholdFactor} onChange={(e) => setConfig({ ...config, popularityThresholdFactor: Number(e.target.value) })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Price Alert Threshold (%)</label>
              <input className="sila-input" type="number" value={config.ingredientPriceAlertThresholdPercent} onChange={(e) => setConfig({ ...config, ingredientPriceAlertThresholdPercent: Number(e.target.value) })} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Refresh Schedule</label>
              <select className="sila-select" value={config.scheduledRefreshFrequency} onChange={(e) => setConfig({ ...config, scheduledRefreshFrequency: e.target.value })}>
                <option value="HOURLY">Hourly</option>
                <option value="DAILY">Daily</option>
                <option value="WEEKLY">Weekly</option>
                <option value="MANUAL">Manual Only</option>
              </select>
            </div>
          </div>
          {config.canEdit && (
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              style={{ marginTop: 10 }}
              onClick={async () => {
                const updated = await saveCulinaryAnalyticsConfig(config);
                setConfig(updated);
                toastService.success(`Saved Intelligence Config v${updated.configVersion}`);
              }}
            >
              Save Configuration
            </button>
          )}
          <div style={{ fontSize: 12, marginTop: 10 }}>
            <strong>Version History:</strong> {config.historyNotes.slice(0, 5).join(" | ")}
          </div>
        </section>
      )}
    </div>
  );
};

export default CulinaryAnalyticsStudioWorkspace;
