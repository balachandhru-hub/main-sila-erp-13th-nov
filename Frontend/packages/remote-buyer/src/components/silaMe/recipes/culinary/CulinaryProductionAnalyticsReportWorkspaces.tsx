import React, { useEffect, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  Line,
  LineChart,
  ReferenceLine,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { toastService } from "@vosox/shared-ui";
import {
  listCulinaryRecipeDrafts,
  type CulinaryRecipeDraftSummary,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";
import {
  exportLifecycleReport,
  exportMaterialRequestsReport,
} from "../../../../api/silaMe/silaCulinaryLifecycleApi";
import { triggerBlobDownload } from "./culinaryRecipePlannerHelpers";
import "./CulinarySuite.css";

interface Props {
  mode: "production" | "analytics" | "reports";
}

const DEMO_PRODUCTION_TIMELINE = [
  { window: "08:00–11:00", startPct: 0, widthPct: 21, kitchen: "Main Kitchen", task: "Marinades and sauces", status: "Demo Ready" },
  { window: "09:00–14:00", startPct: 7, widthPct: 36, kitchen: "Pastry Kitchen", task: "Dessert preparation", status: "Demo Ready" },
  { window: "10:00–13:00", startPct: 14, widthPct: 21, kitchen: "Cold Kitchen", task: "Vegetable preparation", status: "Demo Ready" },
  { window: "11:00–15:00", startPct: 21, widthPct: 29, kitchen: "Butchery", task: "Meat preparation", status: "Demo Ready" },
  { window: "14:00–18:00", startPct: 43, widthPct: 29, kitchen: "Hot Kitchen", task: "Main course cooking", status: "Chef Validation Req." },
  { window: "17:00–19:00", startPct: 64, widthPct: 14, kitchen: "Banquet Team", task: "Buffet setup", status: "Pre-Service" },
  { window: "19:00–22:00", startPct: 78, widthPct: 22, kitchen: "Banquet Kitchen", task: "Service & replenishment (Service 19:00)", status: "Live Service" },
];

const DEMO_MONTHLY_FINANCIALS = [
  { month: "Jan", revenue: 450000, cost: 285000, foodCostPct: 32.0 },
  { month: "Feb", revenue: 580000, cost: 350000, foodCostPct: 30.5 },
  { month: "Mar", revenue: 720000, cost: 430000, foodCostPct: 29.0 },
  { month: "Apr", revenue: 650000, cost: 380000, foodCostPct: 27.5 },
  { month: "May", revenue: 850000, cost: 480000, foodCostPct: 27.0 },
  { month: "Jun", revenue: 920000, cost: 520000, foodCostPct: 26.8 },
];

const RECIPE_REPORTS = [
  "1. Recipe Master Report", "2. Recipe Cost Sheet", "3. Ingredient Usage Report",
  "4. Material Matching Report", "5. Missing Material Report", "6. Material Creation Request Report",
  "7. Recipe Version History", "8. Recipe Approval Report", "9. Recipe Publishing Report",
  "10. Allergen Matrix", "11. Dietary Matrix", "12. Nutrition Estimate Report",
  "13. Recipe Profitability Report", "14. Menu Engineering Report", "15. Ingredient Price Impact Report",
  "16. Ingredient Substitution Report", "17. AI Recipe Generation History",
];

const EVENT_REPORTS = [
  "1. Event Summary Report", "2. Event Menu Report", "3. Event Guest Planning Report",
  "4. Event P&L Report", "5. Estimated vs Committed vs Actual P&L", "6. Event Material Requirement Report",
  "7. Stock Availability Report", "8. Material Shortage Report", "9. Procurement Recommendation Report",
  "10. Event Purchase Order Report", "11. Event GRN Report", "12. Event Stock Transfer Report",
  "13. Event Stock Reservation Report", "14. Kitchen Production Report", "15. Planned vs Actual Consumption",
  "16. Event Wastage Report", "17. Event Returns Report", "18. Guest-wise Cost Report",
  "19. Event Profitability Variance Report", "20. Multi-Event Consolidated Report", "21. Event Closure & Reconciliation Report",
  "22. Event Approval Report", "23. Event Transaction History",
];

const CulinaryProductionAnalyticsReportWorkspaces: React.FC<Props> = ({ mode }) => {
  const [drafts, setDrafts] = useState<CulinaryRecipeDraftSummary[]>([]);
  const [reportDraftId, setReportDraftId] = useState("");
  const [reportType, setReportType] = useState("COSTING");
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    if (mode !== "reports") return;
    listCulinaryRecipeDrafts()
      .then((list) => {
        setDrafts(list);
        if (list.length > 0 && !reportDraftId) setReportDraftId(list[0].id);
      })
      .catch(() => {});
  }, [mode]);

  const handleExportRecipeReport = async (format: "XLSX" | "CSV" | "PDF") => {
    if (!reportDraftId) {
      toastService.warning("Select a recipe draft first.");
      return;
    }
    setExporting(true);
    try {
      const { blob, fileName } = await exportLifecycleReport(reportDraftId, reportType, format);
      triggerBlobDownload(blob, fileName);
      toastService.success(`Exported ${fileName}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Export failed.");
    } finally {
      setExporting(false);
    }
  };

  const handleExportMrqReport = async (format: "XLSX" | "CSV" | "PDF") => {
    setExporting(true);
    try {
      const { blob, fileName } = await exportMaterialRequestsReport(format);
      triggerBlobDownload(blob, fileName);
      toastService.success(`Exported ${fileName}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Export failed.");
    } finally {
      setExporting(false);
    }
  };

  if (mode === "production") {
    return (
      <div className="cs2-shell" data-testid="culinary-production-workspace">
        <div className="cs2-header">
          <div>
            <h2 className="cs2-title">Kitchen Production Planner — Illustrative Timeline (§52–§55)</h2>
            <p className="cs2-subtitle">
              Gantt-style kitchen schedule for <code>EVT-2027-0001</code> (Service at 19:00). OR-Tools CP-SAT capacity &amp;
              food-safety holding validation activate in Phase 4A.
            </p>
          </div>
          <span className="cs2-badge cs2-badge--demo">Demo Timeline · Phase 4A</span>
        </div>

        <div className="cs2-banner" role="status">
          <div>
            <strong>ILLUSTRATIVE PRODUCTION SCHEDULE (08:00–22:00)</strong>
            <div>Actual production schedules require chef validation against kitchen capacity and food safety holding limits.</div>
          </div>
          <span className="cs2-badge cs2-badge--planned">CP-SAT Activates Phase 4A</span>
        </div>

        <section className="cs2-card">
          <div className="cs2-timeline">
            {DEMO_PRODUCTION_TIMELINE.map((item) => (
              <div className="cs2-timeline-row" key={item.window}>
                <div>
                  <strong>{item.window}</strong>
                  <div className="cs2-empty-note">{item.kitchen}</div>
                </div>
                <div>
                  <div style={{ fontSize: 13, fontWeight: 600, marginBottom: 6 }}>{item.task}</div>
                  <div className="cs2-timeline-bar-track">
                    <div
                      className="cs2-timeline-bar-fill"
                      style={{ left: `${item.startPct}%`, width: `${item.widthPct}%` }}
                    />
                  </div>
                </div>
                <div style={{ textAlign: "right" }}>
                  <span className="cs2-badge cs2-badge--indigo">{item.status}</span>
                </div>
              </div>
            ))}
          </div>
        </section>
      </div>
    );
  }

  if (mode === "analytics") {
    return (
      <div className="cs2-shell" data-testid="culinary-analytics-workspace">
        <div className="cs2-header">
          <div>
            <h2 className="cs2-title">Culinary Analytics — Demonstration Dashboard (§61–§64)</h2>
            <p className="cs2-subtitle">
              Executive hospitality intelligence across Recipe Performance, Event Revenue &amp; Cost, and Food Cost % vs 28% target.
            </p>
          </div>
          <span className="cs2-badge cs2-badge--demo">Illustrative Demo Data · Phase 3B</span>
        </div>

        <div className="cs2-banner" role="status">
          <div>
            <strong>DEMONSTRATION ANALYTICS — CLEARLY ISOLATED FROM LIVE CUSTOMER PERFORMANCE</strong>
            <div>Live multi-event and POS-linked profitability analytics activate in Phase 3B.</div>
          </div>
          <span className="cs2-badge cs2-badge--demo">§64 Demo Dataset</span>
        </div>

        <div className="cs2-kpi-grid">
          <div className="cs2-kpi cs2-kpi--accent">
            <span className="cs2-kpi__label">Published Recipes (Demo)</span>
            <span className="cs2-kpi__value">1,248</span>
          </div>
          <div className="cs2-kpi cs2-kpi--cyan">
            <span className="cs2-kpi__label">Upcoming Events (Demo)</span>
            <span className="cs2-kpi__value">24</span>
          </div>
          <div className="cs2-kpi cs2-kpi--emerald">
            <span className="cs2-kpi__label">Avg Food Cost (Demo)</span>
            <span className="cs2-kpi__value">26.8%</span>
            <span className="cs2-kpi__hint">Target: 28.0%</span>
          </div>
          <div className="cs2-kpi cs2-kpi--emerald">
            <span className="cs2-kpi__label">Event Contribution Margin</span>
            <span className="cs2-kpi__value">42.5%</span>
          </div>
        </div>

        <div className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Monthly Revenue vs Cost (AED · Demo)</h3>
            </div>
            <div style={{ width: "100%", height: 270 }}>
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={DEMO_MONTHLY_FINANCIALS}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="month" />
                  <YAxis />
                  <Tooltip formatter={(v: unknown) => `AED ${Number(v).toLocaleString()}`} />
                  <Legend />
                  <Bar dataKey="revenue" name="Revenue (AED)" fill="#4f46e5" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="cost" name="Cost (AED)" fill="#0ea5e9" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Food Cost % Trend vs 28% Target (Demo)</h3>
            </div>
            <div style={{ width: "100%", height: 270 }}>
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={DEMO_MONTHLY_FINANCIALS}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                  <XAxis dataKey="month" />
                  <YAxis domain={[24, 34]} />
                  <Tooltip formatter={(v: unknown) => `${v}%`} />
                  <ReferenceLine y={28} stroke="#dc2626" strokeDasharray="4 4" label="28% Target" />
                  <Line type="monotone" dataKey="foodCostPct" name="Food Cost %" stroke="#4f46e5" strokeWidth={3} />
                </LineChart>
              </ResponsiveContainer>
            </div>
          </section>
        </div>
      </div>
    );
  }

  return (
    <div className="cs2-shell" data-testid="culinary-reports-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Recipe Report Studio (Phase 1C Live Exports)</h2>
          <p className="cs2-subtitle">
            Generate formula-injection-safe Excel, CSV, and PDF reports for Recipe Costing, Readiness, Approval Packages,
            Material Creation Requests, and Phase 1C Lifecycle. Event Reports activate in Phase 2A–3B.
          </p>
        </div>
        <span className="cs2-badge cs2-badge--live">Phase 1C Recipe Reports Live</span>
      </div>

      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Export Live Phase 1C Recipe &amp; Material Reports</h3>
        </div>
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label">Recipe Draft</label>
            <select className="sila-select" value={reportDraftId} onChange={(e) => setReportDraftId(e.target.value)}>
              {drafts.length === 0 ? (
                <option value="">No recipe drafts found</option>
              ) : (
                drafts.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.draftReference} — {d.recipeName}
                  </option>
                ))
              )}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label">Recipe Report Type</label>
            <select className="sila-select" value={reportType} onChange={(e) => setReportType(e.target.value)}>
              <option value="COSTING">Detailed Recipe Costing</option>
              <option value="READINESS">Recipe Readiness Report</option>
              <option value="APPROVAL_PACKAGE">Recipe Approval Package</option>
              <option value="STANDARD_CARD">Standard Recipe Card</option>
              <option value="VERSION_COMPARISON">Recipe Version Comparison</option>
            </select>
          </div>
          <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 6 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={exporting || !reportDraftId} onClick={() => handleExportRecipeReport("XLSX")}>
              Export Excel
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting || !reportDraftId} onClick={() => handleExportRecipeReport("CSV")}>
              CSV
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting || !reportDraftId} onClick={() => handleExportRecipeReport("PDF")}>
              PDF
            </button>
          </div>
          <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 6 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting} onClick={() => handleExportMrqReport("XLSX")}>
              Export Material Requests (Excel)
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting} onClick={() => handleExportMrqReport("PDF")}>
              PDF
            </button>
          </div>
        </div>
      </section>

      <div className="cs2-grid-2">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Recipe Intelligence Reports (17)</h3>
            <span className="cs2-badge cs2-badge--live">Excel / CSV / PDF</span>
          </div>
          <ul style={{ margin: 0, paddingLeft: 18, lineHeight: 1.75, fontSize: 13 }}>
            {RECIPE_REPORTS.map((r) => (
              <li key={r}>{r}</li>
            ))}
          </ul>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Event Intelligence Reports (23)</h3>
            <span className="cs2-badge cs2-badge--indigo">Excel / PDF / Print (Phase 3B)</span>
          </div>
          <ul style={{ margin: 0, paddingLeft: 18, lineHeight: 1.75, fontSize: 13 }}>
            {EVENT_REPORTS.map((r) => (
              <li key={r}>{r}</li>
            ))}
          </ul>
        </section>
      </div>
    </div>
  );
};

export default CulinaryProductionAnalyticsReportWorkspaces;
