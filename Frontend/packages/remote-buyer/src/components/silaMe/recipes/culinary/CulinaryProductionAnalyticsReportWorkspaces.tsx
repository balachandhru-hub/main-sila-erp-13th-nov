import React, { useEffect, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  listCulinaryRecipeDrafts,
  type CulinaryRecipeDraftSummary,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";
import {
  exportLifecycleReport,
  exportMaterialRequestsReport,
} from "../../../../api/silaMe/silaCulinaryLifecycleApi";
import {
  downloadEventReportResult,
  exportCulinaryEventReport,
  listCulinaryEvents,
  type SilaCulinaryEvent,
} from "../../../../api/silaMe/silaCulinaryEventMenuApi";
import {
  exportCulinaryAnalyticsReport,
  getCulinaryAnalyticsReportCatalog,
  type SilaCulinaryAnalyticsReportCatalogItem,
} from "../../../../api/silaMe/silaCulinaryAnalyticsApi";
import {
  exportProductionReport,
  getProductionReportCatalog,
  type SilaCulinaryProductionReportCatalogItem,
} from "../../../../api/silaMe/silaCulinaryProductionApi";
import CulinaryAnalyticsStudioWorkspace from "./CulinaryAnalyticsStudioWorkspace";
import CulinaryProductionStudioWorkspace from "./CulinaryProductionStudioWorkspace";
import { triggerBlobDownload } from "./culinaryRecipePlannerHelpers";
import "./CulinarySuite.css";

interface Props {
  mode: "production" | "analytics" | "reports";
}

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

const DEFAULT_PHASE3B_REPORTS: SilaCulinaryAnalyticsReportCatalogItem[] = [
  { reportCode: "MENU_ENGINEERING", name: "1. Menu Engineering Report", description: "STAR / PLOWHORSE / PUZZLE / DOG quadrants & thresholds.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "MENU_ITEM_PROFITABILITY", name: "2. Menu Item Profitability Report", description: "Gross/net price, variable costs, CM & fully loaded profit.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "FOOD_COST_ANALYSIS", name: "3. Food Cost Analysis Report", description: "Actual vs target food cost %, cost & revenue per cover.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "INGREDIENT_PRICE_VARIANCE", name: "4. Ingredient Price Variance Report", description: "Reverse dependency price shock across sub-recipes & events.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "RECIPE_COST_MOVEMENT", name: "5. Recipe Cost Movement Report", description: "Effective-dated recipe version cost changes.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "CONSUMPTION_VARIANCE", name: "6. Consumption Variance Report", description: "Theoretical vs reconciled actual consumption isolating wastage.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "BUFFET_EFFICIENCY", name: "7. Buffet Efficiency Report", description: "Initial batch, replenishment timing, leftovers & holding compliance.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "FOOD_WASTE_ANALYSIS", name: "8. Food Waste Analysis Report", description: "10 waste classifications by ingredient, outlet & cover.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "EVENT_PROFITABILITY", name: "9. Event Profitability Report", description: "Banquet & catering package P&L comparison.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "DEMAND_FORECAST", name: "10. Demand Forecast Report", description: "Forecast covers, prediction intervals & rolling backtest MAE/WAPE.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "PROPERTY_BENCHMARKING", name: "11. Property Benchmarking Report", description: "FX-normalized multi-property comparison.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "AI_RECOMMENDATION", name: "12. AI Recommendation Report", description: "Prioritized culinary actions with approval & evidence status.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "POTENTIAL_SAVINGS", name: "13. Potential Savings Report", description: "Approved and pending savings pipeline across engines.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "REALIZED_SAVINGS", name: "14. Realized Savings Report", description: "Implemented recommendations tracking expected vs realized impact.", formats: ["EXCEL", "CSV", "PDF"] },
  { reportCode: "WHAT_IF_COMPARISON", name: "15. What-if Scenario Comparison Report", description: "Baseline vs Scenario A/B/C & AI-recommended snapshots.", formats: ["EXCEL", "CSV", "PDF"] },
];

const CulinaryProductionAnalyticsReportWorkspaces: React.FC<Props> = ({ mode }) => {
  const [drafts, setDrafts] = useState<CulinaryRecipeDraftSummary[]>([]);
  const [reportDraftId, setReportDraftId] = useState("");
  const [reportType, setReportType] = useState("COSTING");
  const [eventsList, setEventsList] = useState<SilaCulinaryEvent[]>([]);
  const [reportEventId, setReportEventId] = useState("");
  const [eventReportCode, setEventReportCode] = useState("EVENT_SUMMARY");
  const [phase3BCatalog, setPhase3BCatalog] = useState<SilaCulinaryAnalyticsReportCatalogItem[]>(DEFAULT_PHASE3B_REPORTS);
  const [phase3BReportCode, setPhase3BReportCode] = useState("MENU_ENGINEERING");
  const [phase4ACatalog, setPhase4ACatalog] = useState<SilaCulinaryProductionReportCatalogItem[]>([]);
  const [phase4AReportCode, setPhase4AReportCode] = useState("DAILY_PRODUCTION_PLAN");
  const [exporting, setExporting] = useState(false);

  useEffect(() => {
    if (mode !== "reports") return;
    listCulinaryRecipeDrafts()
      .then((list) => {
        setDrafts(list);
        if (list.length > 0 && !reportDraftId) setReportDraftId(list[0].id);
      })
      .catch(() => {});
    listCulinaryEvents()
      .then((list) => {
        setEventsList(list);
        if (list.length > 0 && !reportEventId) setReportEventId(list[0].id);
      })
      .catch(() => {});
    getCulinaryAnalyticsReportCatalog()
      .then((cat) => {
        setPhase3BCatalog(cat);
        if (cat.length > 0) setPhase3BReportCode(cat[0].reportCode);
      })
      .catch(() => {});
    getProductionReportCatalog()
      .then((cat) => {
        setPhase4ACatalog(cat);
        if (cat.length > 0) setPhase4AReportCode(cat[0].reportCode);
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
    return <CulinaryProductionStudioWorkspace />;
  }

  if (mode === "analytics") {
    return <CulinaryAnalyticsStudioWorkspace />;
  }

  const handleExportPhase3BReport = async (format: "CSV" | "EXCEL" | "PDF") => {
    setExporting(true);
    try {
      const res = await exportCulinaryAnalyticsReport(phase3BReportCode, format);
      const blob = new Blob([res.content], { type: res.contentType });
      triggerBlobDownload(blob, res.fileName);
      toastService.success(`Exported ${res.fileName}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Analytics export failed.");
    } finally {
      setExporting(false);
    }
  };

  const handleExportPhase4AReport = async (format: "CSV" | "EXCEL" | "PDF") => {
    setExporting(true);
    try {
      const res = await exportProductionReport(phase4AReportCode, format);
      const blob = new Blob([res.content], { type: res.contentType });
      triggerBlobDownload(blob, res.fileName);
      toastService.success(`Exported ${res.fileName}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Production export failed.");
    } finally {
      setExporting(false);
    }
  };

  return (
    <div className="cs2-shell" data-testid="culinary-reports-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Culinary Intelligence Report Studio</h2>
          <p className="cs2-subtitle">
            Generate formula-injection-safe Excel, CSV, and PDF reports across Kitchen Production, Recipe Costing, Events, Menu Engineering, Consumption Variance, Waste &amp; Savings.
          </p>
        </div>
        <span className="cs2-badge cs2-badge--live">Reports Ready</span>
      </div>

      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Kitchen Production Reports (20 Reports)</h3>
          <span className="cs2-badge cs2-badge--live">RBAC &amp; Formula-Safe</span>
        </div>
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label">Production Report</label>
            <select className="sila-select" value={phase4AReportCode} onChange={(e) => setPhase4AReportCode(e.target.value)}>
              {phase4ACatalog.map((c) => (
                <option key={c.reportCode} value={c.reportCode}>
                  {c.name}
                </option>
              ))}
              {phase4ACatalog.length === 0 && <option value="DAILY_PRODUCTION_PLAN">1. Daily Production Plan</option>}
            </select>
          </div>
          <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 6 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={exporting} onClick={() => void handleExportPhase4AReport("EXCEL")}>
              Export Excel
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting} onClick={() => void handleExportPhase4AReport("CSV")}>
              CSV
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting} onClick={() => void handleExportPhase4AReport("PDF")}>
              PDF
            </button>
          </div>
        </div>
      </section>

      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Culinary Intelligence Reports (15 Reports)</h3>
          <span className="cs2-badge cs2-badge--live">RBAC &amp; Formula-Safe</span>
        </div>
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label">Analytics Report</label>
            <select className="sila-select" value={phase3BReportCode} onChange={(e) => setPhase3BReportCode(e.target.value)}>
              {phase3BCatalog.map((c) => (
                <option key={c.reportCode} value={c.reportCode}>
                  {c.name}
                </option>
              ))}
              {phase3BCatalog.length === 0 && <option value="MENU_ENGINEERING">1. Menu Engineering Report</option>}
            </select>
          </div>
          <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 6 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={exporting} onClick={() => void handleExportPhase3BReport("EXCEL")}>
              Export Excel
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting} onClick={() => void handleExportPhase3BReport("CSV")}>
              CSV
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={exporting} onClick={() => void handleExportPhase3BReport("PDF")}>
              PDF
            </button>
          </div>
        </div>
      </section>

      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Recipe &amp; Material Reports</h3>
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

      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Event &amp; Menu Reports</h3>
          <span className="cs2-badge cs2-badge--live">Active</span>
        </div>
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label">Event</label>
            <select className="sila-select" value={reportEventId} onChange={(e) => setReportEventId(e.target.value)}>
              {eventsList.length === 0 ? (
                <option value="">No events created yet</option>
              ) : (
                eventsList.map((ev) => (
                  <option key={ev.id} value={ev.id}>
                    {ev.eventCode} — {ev.name} ({ev.status})
                  </option>
                ))
              )}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label">Event Report Type</label>
            <select className="sila-select" value={eventReportCode} onChange={(e) => setEventReportCode(e.target.value)}>
              <option value="EVENT_SUMMARY">Event Summary Report</option>
              <option value="MENU_COSTING">Event Menu &amp; Costing Report</option>
              <option value="GUEST_PLANNING">Guest Planning Report</option>
              <option value="ALLERGEN_MATRIX">Event Allergen Matrix</option>
              <option value="MATERIAL_PREVIEW">Preliminary Material Preview</option>
              <option value="APPROVAL_PACKAGE">Event Approval Package</option>
            </select>
          </div>
          <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 6 }}>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              disabled={exporting || !reportEventId}
              onClick={async () => {
                setExporting(true);
                try {
                  const res = await exportCulinaryEventReport(reportEventId, eventReportCode);
                  downloadEventReportResult(res);
                  toastService.success(`Exported ${res.fileName}`);
                } catch (err: unknown) {
                  toastService.error(err instanceof Error ? err.message : "Export failed.");
                } finally {
                  setExporting(false);
                }
              }}
            >
              Export CSV (Formula-Safe)
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
            <span className="cs2-badge cs2-badge--indigo">Excel / PDF / Print</span>
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
