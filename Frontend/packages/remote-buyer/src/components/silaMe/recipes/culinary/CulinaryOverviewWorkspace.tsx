import React, { useState } from "react";
import type { SilaRecipeDashboard } from "../../../../api/silaMe/silaRecipeToolsApi";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface CulinaryOverviewWorkspaceProps {
  dashboard: SilaRecipeDashboard | null;
  totalCatalogCount: number;
  demoEnabled: boolean;
  onOpenWorkspace: (target: "list" | "legacy-upload" | CulinaryWorkspaceKind) => void;
  onNavigateGlobal?: (navKey: string) => void;
}

const DEMO_OVERVIEW_METRICS = {
  totalRecipes: 1412,
  publishedRecipes: 1248,
  draftRecipes: 118,
  pendingRecipeApprovals: 46,
  aiGeneratedRecipes: 0,
  pendingMaterialMappings: 14,
  materialCreationRequests: 6,
  upcomingEvents: 24,
  eventsInProgress: 3,
  eventsWithShortages: 5,
  eventsAwaitingProcurement: 4,
  avgFoodCostPct: "26.8%",
  avgRecipeCostAed: "AED 20.07",
  eventRevenueAed: "AED 4,170,000",
  eventContributionAed: "AED 1,772,250",
  contributionMarginPct: "42.5%",
  wastagePct: "2.9%",
};

const CulinaryOverviewWorkspace: React.FC<CulinaryOverviewWorkspaceProps> = ({
  dashboard,
  totalCatalogCount,
  demoEnabled,
  onOpenWorkspace,
  onNavigateGlobal,
}) => {
  const [demoMode, setDemoMode] = useState(false);
  const [propertyFilter, setPropertyFilter] = useState("ALL");
  const [outletFilter, setOutletFilter] = useState("ALL");
  const [kitchenFilter, setKitchenFilter] = useState("ALL");
  const [eventTypeFilter, setEventTypeFilter] = useState("ALL");

  return (
    <div className="cs2-shell" data-testid="culinary-overview-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Culinary Intelligence Overview</h2>
          <p className="cs2-subtitle">
            Unified Recipe Studio 2.0 command center across Recipe Master, Material Intelligence, Menu Studio,
            Event MRP, Procurement, Kitchen Production, Event P&amp;L, and AI Governance.
          </p>
        </div>
        <div className="cs2-actions-row">
          {demoEnabled && (
            <button
              type="button"
              className={`sila-btn sila-btn--sm ${demoMode ? "sila-btn--primary" : "sila-btn--secondary"}`}
              data-testid="overview-demo-toggle"
              onClick={() => setDemoMode((v) => !v)}
            >
              {demoMode ? "Demonstration Mode: ON (Illustrative)" : "Switch to Demonstration Mode"}
            </button>
          )}
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("list")}>
            Open Recipe Master
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("ai-planner")}>
            Launch Recipe Scaling Demo
          </button>
        </div>
      </div>

      {demoMode ? (
        <div className="cs2-banner" role="status" data-testid="overview-demo-banner">
          <div>
            <strong>DEMONSTRATION MODE — ILLUSTRATIVE HOSPITALITY DATA ONLY</strong>
            <div>
              Showing isolated Phase 0 demonstration metrics (§11 / §64). No demonstration recipes, events, reservations,
              or purchase orders are written to any production tenant database.
            </div>
          </div>
          <span className="cs2-badge cs2-badge--demo">Isolated Demo Dataset</span>
        </div>
      ) : (
        <div className="cs2-banner cs2-banner--phase" role="status">
          <div>
            <strong>LIVE PRODUCTION TENANT SUMMARY</strong>
            <div>
              Recipe Master metrics come directly from your authorized SILA tenant database. Event, P&amp;L, and AI generation
              counts remain 0 / Planned until Phases 1A–4B are activated.
            </div>
          </div>
          <span className="cs2-badge cs2-badge--live">Live Tenant Data</span>
        </div>
      )}

      <section className="cs2-card" aria-label="Overview Filters">
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-prop">Property</label>
            <select id="cs2-ov-prop" className="sila-select" value={propertyFilter} onChange={(e) => setPropertyFilter(e.target.value)}>
              <option value="ALL">All authorized properties</option>
              <option value="FLAGSHIP">Flagship Hotel &amp; Convention (Demo Filter)</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-outlet">Outlet</label>
            <select id="cs2-ov-outlet" className="sila-select" value={outletFilter} onChange={(e) => setOutletFilter(e.target.value)}>
              <option value="ALL">All outlets</option>
              <option value="BANQUET">Grand Ballroom &amp; Banquets</option>
              <option value="ALL_DAY">All-Day Dining</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-kitchen">Kitchen</label>
            <select id="cs2-ov-kitchen" className="sila-select" value={kitchenFilter} onChange={(e) => setKitchenFilter(e.target.value)}>
              <option value="ALL">All kitchens</option>
              <option value="BANQUET">Banquet Kitchen</option>
              <option value="MAIN">Main Kitchen</option>
              <option value="PASTRY">Pastry Kitchen</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-etype">Event Type</label>
            <select id="cs2-ov-etype" className="sila-select" value={eventTypeFilter} onChange={(e) => setEventTypeFilter(e.target.value)}>
              <option value="ALL">All event types</option>
              <option value="CORPORATE">Corporate Gala / Conference</option>
              <option value="WEDDING">Wedding &amp; Social Banquet</option>
            </select>
          </div>
        </div>
      </section>

      <div className="cs2-kpi-grid" data-testid="culinary-overview-kpis">
        <div className="cs2-kpi cs2-kpi--accent">
          <span className="cs2-kpi__label">Total Recipes</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.totalRecipes : (dashboard?.totalRecipes ?? totalCatalogCount)}</span>
          <span className="cs2-kpi__hint">{demoMode ? "Illustrative demo" : "Live Recipe Master"}</span>
        </div>
        <div className="cs2-kpi cs2-kpi--emerald">
          <span className="cs2-kpi__label">Published / Active</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.publishedRecipes : (dashboard?.activeRecipes ?? 0)}</span>
          <span className="cs2-kpi__hint">{demoMode ? "Illustrative demo" : "Live Recipe Master"}</span>
        </div>
        <div className="cs2-kpi cs2-kpi--cyan">
          <span className="cs2-kpi__label">Draft Recipes</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.draftRecipes : (dashboard?.draftRecipes ?? 0)}</span>
          <span className="cs2-kpi__hint">{demoMode ? "Illustrative demo" : "Live Recipe Master"}</span>
        </div>
        <div className="cs2-kpi cs2-kpi--amber">
          <span className="cs2-kpi__label">Pending Approvals</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.pendingRecipeApprovals : (dashboard?.pendingApprovalTotal ?? 0)}</span>
          <span className="cs2-kpi__hint">{demoMode ? "Illustrative demo" : "Live Approval Queue"}</span>
        </div>
        <div className="cs2-kpi cs2-kpi--accent">
          <span className="cs2-kpi__label">AI-Generated Recipes</span>
          <span className="cs2-kpi__value">{demoMode ? "Demo Preview" : "0"}</span>
          <span className="cs2-kpi__hint">Activates in Phase 1A</span>
        </div>
        <div className="cs2-kpi cs2-kpi--amber">
          <span className="cs2-kpi__label">Pending Material Mappings</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.pendingMaterialMappings : "0"}</span>
          <span className="cs2-kpi__hint">Activates in Phase 1B</span>
        </div>
        <div className="cs2-kpi cs2-kpi--cyan">
          <span className="cs2-kpi__label">Material Creation Requests</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.materialCreationRequests : "0"}</span>
          <span className="cs2-kpi__hint">Activates in Phase 1C</span>
        </div>
        <div className="cs2-kpi cs2-kpi--accent">
          <span className="cs2-kpi__label">Upcoming Events</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.upcomingEvents : "0"}</span>
          <span className="cs2-kpi__hint">Activates in Phase 2A</span>
        </div>
        <div className="cs2-kpi cs2-kpi--rose">
          <span className="cs2-kpi__label">Events w/ Material Shortages</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.eventsWithShortages : "0"}</span>
          <span className="cs2-kpi__hint">Interactive MRP Demo ready</span>
        </div>
        <div className="cs2-kpi cs2-kpi--emerald">
          <span className="cs2-kpi__label">Avg Food Cost %</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.avgFoodCostPct : "—"}</span>
          <span className="cs2-kpi__hint">{demoMode ? "Target: 28.0%" : "Requires Phase 3A costing"}</span>
        </div>
        <div className="cs2-kpi cs2-kpi--emerald">
          <span className="cs2-kpi__label">Event Contribution Margin</span>
          <span className="cs2-kpi__value">{demoMode ? DEMO_OVERVIEW_METRICS.contributionMarginPct : "—"}</span>
          <span className="cs2-kpi__hint">{demoMode ? DEMO_OVERVIEW_METRICS.eventContributionAed : "Requires Phase 3A P&L"}</span>
        </div>
        <div className="cs2-kpi cs2-kpi--rose">
          <span className="cs2-kpi__label">POS Failed / Today Posted</span>
          <span className="cs2-kpi__value">
            {demoMode ? "0 / 184" : `${dashboard?.failedPosSales ?? 0} / ${dashboard?.consumptionPostedToday ?? 0}`}
          </span>
          <span className="cs2-kpi__hint">{demoMode ? "Illustrative demo" : "Live POS Consumption"}</span>
        </div>
      </div>

      <div className="cs2-grid-3">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Interactive Phase 0 Simulators</h3>
            <span className="cs2-badge cs2-badge--demo">Deterministic Local Engine</span>
          </div>
          <p className="cs2-empty-note">
            Test live culinary math today without creating production recipes, reservations, or POs.
          </p>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("ai-planner")}>
              Recipe Scaling Demo
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("mrp")}>
              Event MRP Demo
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("event-pnl")}>
              Event P&amp;L Simulator
            </button>
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Live SILA Operational Links</h3>
            <span className="cs2-badge cs2-badge--live">Production Ready</span>
          </div>
          <p className="cs2-empty-note">
            Jump straight into existing approved SILA workflows preserved in full.
          </p>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("legacy-upload")}>
              Legacy Recipe Upload
            </button>
            {onNavigateGlobal && (
              <>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onNavigateGlobal("silaRecipeApprovals")}>
                  Recipe Approvals
                </button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onNavigateGlobal("silaMeTracker")}>
                  Transaction Tracker
                </button>
              </>
            )}
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Governance &amp; AI Setup</h3>
            <span className="cs2-badge cs2-badge--live">Phase 0 Active</span>
          </div>
          <p className="cs2-empty-note">
            Configure Local AI, Gemini, Claude, or Hybrid task routing, tenant external-AI consent, budgets, and feature flags.
          </p>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("ai-setup")}>
              Open AI Engine Setup
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("reports")}>
              Report Studio Catalog
            </button>
          </div>
        </section>
      </div>
    </div>
  );
};

export default CulinaryOverviewWorkspace;
