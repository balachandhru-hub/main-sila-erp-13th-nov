import React from "react";
import type { SilaCulinaryAiFeatureFlags, SilaCulinaryAiStatus } from "../../../../api/silaMe/silaCulinaryAiApi";
import type { SilaMePermissions } from "../../silaMePermissions";
import type { SilaRecipeRoute } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

export interface SilaRecipeStudio2NavProps {
  id?: string;
  activeRoute: SilaRecipeRoute;
  flags: SilaCulinaryAiFeatureFlags;
  aiStatus: SilaCulinaryAiStatus | null;
  permissions?: Partial<SilaMePermissions>;
  onSelectRecipeRoute: (target: "list" | "legacy-upload" | Exclude<SilaRecipeRoute["kind"], "list" | "new" | "detail" | "edit" | "legacy-upload">) => void;
  onNavigateGlobal?: (navKey: string) => void;
}

interface NavItem {
  id: string;
  label: string;
  badge: string;
  badgeTone: "live" | "demo" | "planned" | "indigo";
  routeKind?: "list" | "legacy-upload" | Exclude<SilaRecipeRoute["kind"], "list" | "new" | "detail" | "edit" | "legacy-upload">;
  globalNavKey?: string;
  visible: boolean;
  disabled?: boolean;
  tooltip?: string;
}

const SilaRecipeStudio2Nav: React.FC<SilaRecipeStudio2NavProps> = ({
  id,
  activeRoute,
  flags,
  aiStatus,
  permissions,
  onSelectRecipeRoute,
  onNavigateGlobal,
}) => {
  if (!flags.culinarySuiteEnabled) {
    return null;
  }

  const canManageMasterData = permissions?.manageMasterData ?? true;
  const canApproveRecipe = permissions?.approveRecipe ?? true;
  const canViewAudit = permissions?.viewAudit ?? true;

  const items: NavItem[] = [
    {
      id: "overview",
      label: "Overview",
      badge: "Phase 0",
      badgeTone: "indigo",
      routeKind: "overview",
      visible: flags.overviewEnabled,
    },
    {
      id: "list",
      label: "Recipe Master",
      badge: "Live",
      badgeTone: "live",
      routeKind: "list",
      visible: true,
    },
    {
      id: "legacy-upload",
      label: "Legacy Recipe Upload",
      badge: "Live",
      badgeTone: "live",
      routeKind: "legacy-upload",
      visible: true,
    },
    {
      id: "ai-planner",
      label: "AI Recipe Planner",
      badge: "Interactive Demo",
      badgeTone: "demo",
      routeKind: "ai-planner",
      visible: flags.recipePlannerPreviewEnabled,
    },
    {
      id: "materials",
      label: "Material Intelligence",
      badge: "Phase 1B Preview",
      badgeTone: "planned",
      routeKind: "materials",
      visible: flags.materialIntelligencePreviewEnabled,
    },
    {
      id: "material-requests",
      label: "Material Requests",
      badge: "Phase 1C Live",
      badgeTone: "live",
      routeKind: "material-requests",
      visible: flags.materialIntelligencePreviewEnabled,
    },
    {
      id: "menus",
      label: "Menu Studio",
      badge: "Phase 2A Live",
      badgeTone: "live",
      routeKind: "menus",
      visible: flags.menuEventPreviewEnabled,
    },
    {
      id: "events",
      label: "AI Event Planner",
      badge: "Phase 2A Live",
      badgeTone: "live",
      routeKind: "events",
      visible: flags.menuEventPreviewEnabled,
    },
    {
      id: "mrp",
      label: "Event Material Req.",
      badge: "Interactive Demo",
      badgeTone: "demo",
      routeKind: "mrp",
      visible: flags.interactiveSimulatorsEnabled,
    },
    {
      id: "procurement",
      label: "Event Procurement",
      badge: "Phase 2C Live",
      badgeTone: "live",
      routeKind: "procurement",
      visible: flags.menuEventPreviewEnabled,
    },
    {
      id: "production",
      label: "Production Planner",
      badge: "Phase 4A Live",
      badgeTone: "live",
      routeKind: "production",
      visible: flags.culinaryPhase4AEnabled ?? flags.menuEventPreviewEnabled,
    },
    {
      id: "food-cost",
      label: "Food Cost Studio",
      badge: "Phase 1C Live",
      badgeTone: "live",
      routeKind: "food-cost",
      visible: flags.menuEventPreviewEnabled,
    },
    {
      id: "event-pnl",
      label: "Event P&L",
      badge: "Phase 3A Live",
      badgeTone: "live",
      routeKind: "event-pnl",
      visible: flags.interactiveSimulatorsEnabled,
    },
    {
      id: "analytics",
      label: "Analytics",
      badge: "Phase 3B Live",
      badgeTone: "live",
      routeKind: "analytics",
      visible: flags.analyticsReportPreviewEnabled,
    },
    {
      id: "reports",
      label: "Report Studio",
      badge: "Phase 3B Live",
      badgeTone: "live",
      routeKind: "reports",
      visible: flags.analyticsReportPreviewEnabled,
    },
    {
      id: "approvals",
      label: "Approvals",
      badge: "Live",
      badgeTone: "live",
      globalNavKey: "silaRecipeApprovals",
      visible: Boolean(onNavigateGlobal),
      disabled: !canApproveRecipe,
      tooltip: canApproveRecipe ? "Open Recipe Approvals" : "Requires APPROVE_SILA_RECIPE permission",
    },
    {
      id: "tracker",
      label: "History & Tracker",
      badge: "Live",
      badgeTone: "live",
      globalNavKey: "silaMeTracker",
      visible: Boolean(onNavigateGlobal),
      disabled: !canViewAudit && permissions?.manageRecipe === false,
      tooltip: "Open SILA ME Consolidated Transaction Tracker",
    },
    {
      id: "ai-setup",
      label: "Culinary AI Engine Setup",
      badge: canManageMasterData ? "Live Setup" : "Read Only",
      badgeTone: canManageMasterData ? "live" : "planned",
      routeKind: "ai-setup",
      visible: flags.aiSetupEnabled,
      tooltip: canManageMasterData
        ? "Configure Local / Gemini / Claude / Hybrid AI providers"
        : "View AI Engine configuration (requires MANAGE_SILA_MASTER_DATA to edit)",
    },
  ];

  const isItemActive = (item: NavItem) => {
    if (!item.routeKind) return false;
    if (item.routeKind === "list") {
      return activeRoute.kind === "list" || activeRoute.kind === "new" || activeRoute.kind === "detail" || activeRoute.kind === "edit";
    }
    return activeRoute.kind === item.routeKind;
  };

  return (
    <nav id={id} className="cs2-nav-bar" aria-label="Recipe Studio 2.0 Culinary Intelligence Navigation" data-testid="culinary-studio-nav">
      <div className="cs2-nav-top">
        <div className="cs2-nav-brand">
          <span className="cs2-brand-pill">Recipe Studio 2.0 · Culinary Intelligence</span>
          <span className="cs2-brand-subtitle">
            Phase 0 Foundation Shell · Existing Recipe Studio, Material Master, Inventory &amp; PO APIs preserved
          </span>
        </div>
        <div className="cs2-nav-meta">
          <span className="cs2-badge cs2-badge--indigo">
            AI Mode: {aiStatus?.operatingMode ?? "LOCAL"}
          </span>
          <span className={`cs2-badge ${aiStatus?.allowExternalAi ? "cs2-badge--warn" : "cs2-badge--live"}`}>
            {aiStatus?.allowExternalAi ? "External AI Consented" : "Tenant External AI Off"}
          </span>
        </div>
      </div>

      <div className="cs2-nav-tabs" role="tablist" aria-label="Culinary modules">
        {items
          .filter((item) => item.visible)
          .map((item) => {
            const active = isItemActive(item);
            return (
              <button
                key={item.id}
                type="button"
                role="tab"
                aria-selected={active}
                disabled={item.disabled}
                title={item.tooltip}
                data-testid={`cs2-nav-${item.id}`}
                className={`cs2-nav-tab${active ? " is-active" : ""}`}
                onClick={() => {
                  if (item.globalNavKey && onNavigateGlobal) {
                    onNavigateGlobal(item.globalNavKey);
                  } else if (item.routeKind) {
                    onSelectRecipeRoute(item.routeKind);
                  }
                }}
              >
                <span>{item.label}</span>
                <span className={`cs2-badge cs2-badge--${item.badgeTone}`}>{item.badge}</span>
              </button>
            );
          })}
      </div>
    </nav>
  );
};

export default SilaRecipeStudio2Nav;
