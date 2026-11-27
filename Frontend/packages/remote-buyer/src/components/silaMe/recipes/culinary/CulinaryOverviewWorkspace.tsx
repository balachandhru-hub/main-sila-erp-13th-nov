import React, { useEffect, useState } from "react";
import {
  AlertTriangle,
  BarChart3,
  Calendar,
  CheckCircle2,
  ChefHat,
  Clock,
  FileText,
  Package,
  ShoppingBag,
  Sparkles,
  TrendingUp,
} from "lucide-react";
import type { SilaRecipeDashboard } from "../../../../api/silaMe/silaRecipeToolsApi";
import {
  getLifecycleDashboard,
  getMaterialRequestDashboard,
  type SilaCulinaryLifecycleDashboard,
  type SilaCulinaryMaterialRequestDashboard,
} from "../../../../api/silaMe/silaCulinaryLifecycleApi";
import {
  getEventDashboard,
  type SilaCulinaryEventDashboard,
} from "../../../../api/silaMe/silaCulinaryEventMenuApi";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import { StudioKPIBar, StudioKPICard } from "../../studio";
import "./CulinarySuite.css";

interface CulinaryOverviewWorkspaceProps {
  dashboard: SilaRecipeDashboard | null;
  totalCatalogCount: number;
  demoEnabled: boolean;
  onOpenWorkspace: (target: "list" | "legacy-upload" | Exclude<CulinaryWorkspaceKind, "menus">) => void;
  onNavigateGlobal?: (navKey: string) => void;
}

const DEMO_OVERVIEW_METRICS = {
  totalRecipes: 1412,
  publishedRecipes: 1248,
  draftRecipes: 118,
  pendingRecipeApprovals: 46,
  pendingMaterialMappings: 14,
  materialCreationRequests: 6,
  upcomingEvents: 24,
  eventsWithShortages: 5,
  avgFoodCostPct: "26.8%",
  eventContributionAed: "AED 1,772,250",
  contributionMarginPct: "42.5%",
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
  const [lifecycleDash, setLifecycleDash] = useState<SilaCulinaryLifecycleDashboard | null>(null);
  const [mrqDash, setMrqDash] = useState<SilaCulinaryMaterialRequestDashboard | null>(null);
  const [eventDash, setEventDash] = useState<SilaCulinaryEventDashboard | null>(null);

  useEffect(() => {
    if (demoMode) return;
    getLifecycleDashboard().then(setLifecycleDash).catch(() => {});
    getMaterialRequestDashboard().then(setMrqDash).catch(() => {});
    getEventDashboard().then(setEventDash).catch(() => {});
  }, [demoMode]);

  return (
    <div className="cs2-shell" data-testid="culinary-overview-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Culinary Intelligence Overview</h2>
          <p className="cs2-subtitle">
            Unified culinary command center across Recipe Master, Material Intelligence, Event Planning,
            Event MRP, Procurement, Kitchen Production, Food Costing, Event P&amp;L, Analytics, and Governance.
          </p>
        </div>
        <div className="cs2-actions-row">
          {demoEnabled && (
            <button
              type="button"
              className={`sila-studio-btn ${demoMode ? "sila-studio-btn--primary" : "sila-studio-btn--secondary"}`}
              data-testid="overview-demo-toggle"
              onClick={() => setDemoMode((v) => !v)}
            >
              {demoMode ? "Sample Dataset: Active" : "View Sample Dataset"}
            </button>
          )}
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--secondary"
            onClick={() => onOpenWorkspace("list")}
          >
            Open Recipe Master
          </button>
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--primary"
            onClick={() => onOpenWorkspace("ai-planner")}
          >
            Open AI Recipe Planner
          </button>
        </div>
      </div>

      {demoMode ? (
        <div className="cs2-banner" role="status" data-testid="overview-demo-banner">
          <div>
            <strong>SAMPLE DATASET — ILLUSTRATIVE HOSPITALITY METRICS</strong>
            <div>
              Showing isolated preview metrics. No sample recipes, events, reservations, or purchase orders are written to any tenant database.
            </div>
          </div>
          <span className="cs2-badge cs2-badge--demo">Preview Dataset</span>
        </div>
      ) : (
        <div className="cs2-banner cs2-banner--phase" role="status">
          <div>
            <strong>LIVE PRODUCTION TENANT SUMMARY</strong>
            <div>
              Metrics reflect your authorized SILA tenant database across Recipe Master, Material Requests, Events, and POS Consumption.
            </div>
          </div>
          <span className="cs2-badge cs2-badge--live">Live Tenant Data</span>
        </div>
      )}

      <section className="cs2-card" aria-label="Overview Filters">
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-prop">Property</label>
            <select id="cs2-ov-prop" className="sila-studio-select" style={{ width: "100%" }} value={propertyFilter} onChange={(e) => setPropertyFilter(e.target.value)}>
              <option value="ALL">All authorized properties</option>
              <option value="FLAGSHIP">Flagship Hotel &amp; Convention</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-outlet">Outlet</label>
            <select id="cs2-ov-outlet" className="sila-studio-select" style={{ width: "100%" }} value={outletFilter} onChange={(e) => setOutletFilter(e.target.value)}>
              <option value="ALL">All outlets</option>
              <option value="BANQUET">Grand Ballroom &amp; Banquets</option>
              <option value="ALL_DAY">All-Day Dining</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-kitchen">Kitchen</label>
            <select id="cs2-ov-kitchen" className="sila-studio-select" style={{ width: "100%" }} value={kitchenFilter} onChange={(e) => setKitchenFilter(e.target.value)}>
              <option value="ALL">All kitchens</option>
              <option value="BANQUET">Banquet Kitchen</option>
              <option value="MAIN">Main Kitchen</option>
              <option value="PASTRY">Pastry Kitchen</option>
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-ov-etype">Event Type</label>
            <select id="cs2-ov-etype" className="sila-studio-select" style={{ width: "100%" }} value={eventTypeFilter} onChange={(e) => setEventTypeFilter(e.target.value)}>
              <option value="ALL">All event types</option>
              <option value="CORPORATE">Corporate Gala / Conference</option>
              <option value="WEDDING">Wedding &amp; Social Banquet</option>
            </select>
          </div>
        </div>
      </section>

      <StudioKPIBar ariaLabel="Culinary Overview Primary Metrics" testId="culinary-overview-kpis">
        <StudioKPICard
          label="Total Recipes"
          value={demoMode ? DEMO_OVERVIEW_METRICS.totalRecipes : (dashboard?.totalRecipes ?? totalCatalogCount)}
          hint={demoMode ? "Sample dataset" : "Live Recipe Master"}
          tone="blue"
          icon={<ChefHat size={15} />}
        />
        <StudioKPICard
          label="Active Recipes"
          value={demoMode ? DEMO_OVERVIEW_METRICS.publishedRecipes : (dashboard?.activeRecipes ?? 0)}
          hint={demoMode ? "Sample dataset" : "Published & selling"}
          tone="green"
          icon={<CheckCircle2 size={15} />}
        />
        <StudioKPICard
          label="Draft Recipes"
          value={demoMode ? DEMO_OVERVIEW_METRICS.draftRecipes : (dashboard?.draftRecipes ?? 0)}
          hint={demoMode ? "Sample dataset" : "In authoring"}
          tone="lavender"
          icon={<FileText size={15} />}
        />
        <StudioKPICard
          label="Pending Approvals"
          value={demoMode ? DEMO_OVERVIEW_METRICS.pendingRecipeApprovals : (dashboard?.pendingApprovalTotal ?? 0)}
          hint={demoMode ? "Sample dataset" : "Live Approval Queue"}
          tone="amber"
          icon={<Clock size={15} />}
        />
        <StudioKPICard
          label="AI Recipe Drafts"
          value={demoMode ? "Preview" : (lifecycleDash?.aiRecipeDrafts ?? 0)}
          hint={demoMode ? "Sample dataset" : `Published: ${lifecycleDash?.publishedRecipes ?? 0}`}
          tone="indigo"
          icon={<Sparkles size={15} />}
        />
        <StudioKPICard
          label="Material Mappings"
          value={demoMode ? DEMO_OVERVIEW_METRICS.pendingMaterialMappings : (lifecycleDash?.materialMappingPending ?? 0)}
          hint={`Costing Ready: ${lifecycleDash?.readyForCosting ?? 0}`}
          tone="amber"
          icon={<Package size={15} />}
        />
        <StudioKPICard
          label="Material Requests"
          value={demoMode ? DEMO_OVERVIEW_METRICS.materialCreationRequests : (mrqDash?.totalRequests ?? 0)}
          hint={`Pending: ${mrqDash?.pendingApprovalRequests ?? 0}`}
          tone="sky"
          icon={<ShoppingBag size={15} />}
        />
        <StudioKPICard
          label="Upcoming Events"
          value={demoMode ? DEMO_OVERVIEW_METRICS.upcomingEvents : (eventDash?.upcomingEvents ?? 0)}
          hint={demoMode ? "Sample dataset" : `Approved: ${eventDash?.approvedEvents ?? 0}`}
          tone="blue"
          icon={<Calendar size={15} />}
        />
        <StudioKPICard
          label="Above Food Cost Target"
          value={demoMode ? DEMO_OVERVIEW_METRICS.avgFoodCostPct : (lifecycleDash?.aboveFoodCostTarget ?? 0)}
          hint={demoMode ? "Target: 28.0%" : `Incomplete: ${lifecycleDash?.costingIncomplete ?? 0}`}
          tone="green"
          icon={<TrendingUp size={15} />}
        />
        <StudioKPICard
          label="POS Failed / Posted"
          value={demoMode ? "0 / 184" : `${dashboard?.failedPosSales ?? 0} / ${dashboard?.consumptionPostedToday ?? 0}`}
          hint={demoMode ? "Sample dataset" : "POS Consumption Today"}
          tone="rose"
          icon={<AlertTriangle size={15} />}
        />
      </StudioKPIBar>

      <div className="cs2-grid-3">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Planning &amp; Simulation Tools</h3>
            <span className="cs2-badge cs2-badge--demo">Interactive</span>
          </div>
          <p className="cs2-empty-note">
            Run recipe scaling, event material requirements, and event P&amp;L simulations before committing operational records.
          </p>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-studio-btn sila-studio-btn--primary" onClick={() => onOpenWorkspace("ai-planner")}>
              AI Recipe Planner
            </button>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("mrp")}>
              Event Material Req.
            </button>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("event-pnl")}>
              Event P&amp;L
            </button>
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Core Operations &amp; Governance</h3>
            <span className="cs2-badge cs2-badge--live">Active</span>
          </div>
          <p className="cs2-empty-note">
            Navigate directly to recipe catalogue management, legacy uploads, approvals, and consolidated transaction history.
          </p>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("list")}>
              Recipe Master
            </button>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("legacy-upload")}>
              Legacy Recipe Upload
            </button>
            {onNavigateGlobal && (
              <>
                <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onNavigateGlobal("silaRecipeApprovals")}>
                  Approvals
                </button>
                <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onNavigateGlobal("silaMeTracker")}>
                  History &amp; Tracker
                </button>
              </>
            )}
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Analytics &amp; AI Configuration</h3>
            <span className="cs2-badge cs2-badge--live">Configured</span>
          </div>
          <p className="cs2-empty-note">
            Inspect culinary analytics, export executive reports, or configure local and external AI engine policies.
          </p>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("analytics")}>
              <BarChart3 size={14} strokeWidth={1.9} aria-hidden="true" />
              <span>Analytics</span>
            </button>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("reports")}>
              Report Studio
            </button>
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onOpenWorkspace("ai-setup")}>
              AI Engine Setup
            </button>
          </div>
        </section>
      </div>
    </div>
  );
};

export default CulinaryOverviewWorkspace;
