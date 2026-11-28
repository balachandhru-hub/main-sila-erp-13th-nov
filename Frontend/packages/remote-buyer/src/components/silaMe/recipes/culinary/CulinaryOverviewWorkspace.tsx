import React, { useEffect, useState } from "react";
import {
  Activity,
  AlertTriangle,
  ArrowUpRight,
  BarChart3,
  ChefHat,
  Sparkles,
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
import type { CulinaryWorkspaceKind, SilaRecipeCatalogQuery } from "../silaRecipeRoutes";
import "./CulinarySuite.css";
import "../SilaRecipeStudioProto.css";
import "../SilaStudioProtoTheme.css";

interface CulinaryOverviewWorkspaceProps {
  dashboard: SilaRecipeDashboard | null;
  totalCatalogCount: number;
  demoEnabled: boolean;
  onOpenWorkspace: (target: "list" | "legacy-upload" | Exclude<CulinaryWorkspaceKind, "menus">) => void;
  onNavigateGlobal?: (navKey: string) => void;
  onOpenRecipeView?: (query: SilaRecipeCatalogQuery) => void;
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

type OverviewTone = "sky" | "emerald" | "slate" | "amber" | "rose" | "indigo";

interface OverviewPill {
  id: string;
  value: React.ReactNode;
  label: string;
  tone: OverviewTone;
  onClick: () => void;
  drill: string;
}

interface OverviewPrimary {
  id: string;
  label: string;
  value: React.ReactNode;
  hint: React.ReactNode;
  tone?: OverviewTone;
  onClick: () => void;
  drill: string;
}

const OverviewBento: React.FC<{
  title: string;
  icon: React.ReactNode;
  alert?: boolean;
  link?: { label: string; onClick: () => void };
  primary: OverviewPrimary;
  pills: OverviewPill[];
}> = ({ title, icon, alert, link, primary, pills }) => (
  <section className={`rsp-glass rsp-ov-card rsp-ov-bento-card${alert ? " rsp-ov-bento-card--alert" : ""}`} aria-label={title}>
    <div className="rsp-ov-card__head">
      <span className={`rsp-ov-eyebrow${alert ? " rsp-ov-eyebrow--alert" : ""}`}>
        {icon}
        {title}
      </span>
      {link && (
        <button type="button" className="rsp-ov-link" onClick={link.onClick}>
          {link.label}
        </button>
      )}
    </div>
    <div className="rsp-ov-bento-card__body">
      <button
        type="button"
        className={`rsp-ov-primary rsp-ov-primary--${primary.tone ?? "sky"}`}
        onClick={primary.onClick}
        aria-label={primary.drill}
        data-testid={`ov-kpi-${primary.id}`}
      >
        <span className="rsp-ov-primary__value">{primary.value}</span>
        <span className="rsp-ov-primary__label">{primary.label}</span>
        <span className="rsp-ov-primary__hint">
          {primary.hint}
          <ArrowUpRight size={12} strokeWidth={2} aria-hidden="true" />
        </span>
      </button>
      <div className="rsp-ov-pills">
        {pills.map((pill) => (
          <button
            key={pill.id}
            type="button"
            className={`rsp-ov-pill rsp-ov-pill--${pill.tone}`}
            onClick={pill.onClick}
            aria-label={pill.drill}
            data-testid={`ov-kpi-${pill.id}`}
          >
            <span className="rsp-ov-pill__dot" aria-hidden="true" />
            <span className="rsp-ov-pill__value">{pill.value}</span>
            <span>{pill.label}</span>
          </button>
        ))}
      </div>
    </div>
  </section>
);

const CulinaryOverviewWorkspace: React.FC<CulinaryOverviewWorkspaceProps> = ({
  dashboard,
  totalCatalogCount,
  demoEnabled,
  onOpenWorkspace,
  onNavigateGlobal,
  onOpenRecipeView,
}) => {
  const openRecipes = (query: SilaRecipeCatalogQuery) => {
    if (onOpenRecipeView) onOpenRecipeView(query);
    else onOpenWorkspace("list");
  };
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

  const totalRecipes = demoMode ? DEMO_OVERVIEW_METRICS.totalRecipes : (dashboard?.totalRecipes ?? totalCatalogCount);
  const activeRecipes = demoMode ? DEMO_OVERVIEW_METRICS.publishedRecipes : (dashboard?.activeRecipes ?? 0);
  const draftRecipes = demoMode ? DEMO_OVERVIEW_METRICS.draftRecipes : (dashboard?.draftRecipes ?? 0);
  const pendingApprovals = demoMode ? DEMO_OVERVIEW_METRICS.pendingRecipeApprovals : (dashboard?.pendingApprovalTotal ?? 0);
  const posFailed = demoMode ? 0 : (dashboard?.failedPosSales ?? 0);
  const posPosted = demoMode ? 184 : (dashboard?.consumptionPostedToday ?? 0);
  const aboveFoodCost = demoMode ? DEMO_OVERVIEW_METRICS.avgFoodCostPct : (lifecycleDash?.aboveFoodCostTarget ?? 0);
  const upcomingEvents = demoMode ? DEMO_OVERVIEW_METRICS.upcomingEvents : (eventDash?.upcomingEvents ?? 0);
  const aiDrafts = demoMode ? "Preview" : (lifecycleDash?.aiRecipeDrafts ?? 0);
  const materialMappings = demoMode ? DEMO_OVERVIEW_METRICS.pendingMaterialMappings : (lifecycleDash?.materialMappingPending ?? 0);
  const materialRequests = demoMode ? DEMO_OVERVIEW_METRICS.materialCreationRequests : (mrqDash?.totalRequests ?? 0);

  return (
    <div className="cs2-shell rsp-overview" data-testid="culinary-overview-workspace">
      <section className="rsp-glass rsp-ov-toolbar" aria-label="Overview data source">
        <div className="rsp-ov-toolbar__copy">
          {demoMode ? (
            <div className="rsp-ov-toolbar__banner" role="status" data-testid="overview-demo-banner">
              <span className="rsp-ov-eyebrow">Sample dataset — illustrative hospitality metrics</span>
              <p>
                Showing isolated preview metrics. No sample recipes, events, reservations, or purchase orders are written to any tenant database.
              </p>
            </div>
          ) : (
            <div className="rsp-ov-toolbar__banner" role="status">
              <span className="rsp-ov-eyebrow">Live production tenant summary</span>
              <p>
                Metrics reflect your authorized SILA tenant database across Recipe Master, Material Requests, Events, and POS Consumption.
              </p>
            </div>
          )}
          <span className={`cs2-badge ${demoMode ? "cs2-badge--demo" : "cs2-badge--live"}`}>
            {demoMode ? "Preview Dataset" : "Live Tenant Data"}
          </span>
        </div>
        <div className="rsp-ov-toolbar__actions" data-testid="culinary-overview-actions">
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
            <Sparkles size={14} strokeWidth={2} aria-hidden="true" />
            <span>Open AI Recipe Planner</span>
          </button>
        </div>
      </section>

      <section className="rsp-glass rsp-ov-filters" aria-label="Overview Filters">
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
      </section>

      <div className="rsp-ov-bento" data-testid="culinary-overview-kpis">
        <OverviewBento
          title="Catalog Health"
          icon={<ChefHat size={14} strokeWidth={2} aria-hidden="true" />}
          link={{ label: "View All", onClick: () => openRecipes({}) }}
          primary={{
            id: "total-recipes",
            label: "Total Recipes",
            value: totalRecipes,
            hint: demoMode ? "Sample dataset" : "Live Recipe Master",
            onClick: () => openRecipes({}),
            drill: "Open all recipes",
          }}
          pills={[
            { id: "active-recipes", value: activeRecipes, label: "Active", tone: "emerald", onClick: () => openRecipes({ status: "ACTIVE" }), drill: "Open active recipes" },
            { id: "draft-recipes", value: draftRecipes, label: "Drafts", tone: "slate", onClick: () => openRecipes({ status: "DRAFT" }), drill: "Open draft recipes" },
            { id: "pending-approvals", value: pendingApprovals, label: "Pending", tone: "amber", onClick: () => openRecipes({ status: "PENDING_APPROVAL" }), drill: "Open recipes pending approval" },
          ]}
        />
        <OverviewBento
          title="Action Required"
          icon={<AlertTriangle size={14} strokeWidth={2} aria-hidden="true" />}
          alert
          primary={{
            id: "pos-failed",
            label: "POS Failed",
            value: posFailed,
            hint: demoMode ? "Sample dataset" : "Needs mapping or stock posting",
            tone: "rose",
            onClick: () => openRecipes({ view: "failed-pos" }),
            drill: "Open failed POS sale lines",
          }}
          pills={[
            { id: "pos-posted-today", value: posPosted, label: "Posted today", tone: "indigo", onClick: () => openRecipes({ view: "posted-consumption" }), drill: "Open consumption posted today" },
            { id: "above-food-cost", value: aboveFoodCost, label: "Above food cost", tone: "amber", onClick: () => onOpenWorkspace("food-cost"), drill: "Open Food Cost Studio" },
          ]}
        />
        <OverviewBento
          title="Planning & Costing"
          icon={<Activity size={14} strokeWidth={2} aria-hidden="true" />}
          primary={{
            id: "upcoming-events",
            label: "Upcoming Events",
            value: upcomingEvents,
            hint: demoMode ? "Sample dataset" : `Approved: ${eventDash?.approvedEvents ?? 0}`,
            onClick: () => onOpenWorkspace("events"),
            drill: "Open Event Planner",
          }}
          pills={[
            { id: "ai-drafts", value: aiDrafts, label: "AI drafts", tone: "indigo", onClick: () => onOpenWorkspace("ai-planner"), drill: "Open AI Recipe Planner drafts" },
            { id: "material-mappings", value: materialMappings, label: "Mappings", tone: "amber", onClick: () => onOpenWorkspace("materials"), drill: "Open Material Intelligence" },
            { id: "material-requests", value: materialRequests, label: "Material requests", tone: "sky", onClick: () => onOpenWorkspace("material-requests"), drill: "Open Material Requests" },
          ]}
        />
      </div>

      <div className="rsp-ov-bento">
        <section className="rsp-glass rsp-ov-card">
          <div className="rsp-ov-card__head">
            <h3 className="rsp-ov-card__title">Planning &amp; Simulation Tools</h3>
            <span className="cs2-badge cs2-badge--demo">Interactive</span>
          </div>
          <p className="rsp-ov-card__text">
            Run recipe scaling, event material requirements, and event P&amp;L simulations before committing operational records.
          </p>
          <div className="rsp-ov-card__actions">
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

        <section className="rsp-glass rsp-ov-card">
          <div className="rsp-ov-card__head">
            <h3 className="rsp-ov-card__title">Core Operations &amp; Governance</h3>
            <span className="cs2-badge cs2-badge--live">Active</span>
          </div>
          <p className="rsp-ov-card__text">
            Navigate directly to recipe catalogue management, legacy uploads, approvals, and consolidated transaction history.
          </p>
          <div className="rsp-ov-card__actions">
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

        <section className="rsp-glass rsp-ov-card">
          <div className="rsp-ov-card__head">
            <h3 className="rsp-ov-card__title">Analytics &amp; AI Configuration</h3>
            <span className="cs2-badge cs2-badge--live">Configured</span>
          </div>
          <p className="rsp-ov-card__text">
            Inspect culinary analytics, export executive reports, or configure local and external AI engine policies.
          </p>
          <div className="rsp-ov-card__actions">
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
