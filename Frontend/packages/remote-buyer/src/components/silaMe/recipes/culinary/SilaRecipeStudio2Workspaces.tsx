import React, { useEffect, useState } from "react";
import { ArrowUpRight, Maximize2, Minimize2 } from "lucide-react";
import type { SilaCulinaryAiFeatureFlags, SilaCulinaryAiStatus } from "../../../../api/silaMe/silaCulinaryAiApi";
import type { SilaRecipeDashboard } from "../../../../api/silaMe/silaRecipeToolsApi";
import type { SilaMePermissions } from "../../silaMePermissions";
import type { CulinaryWorkspaceKind, SilaRecipeRoute } from "../silaRecipeRoutes";
import { StudioEmptyState } from "../../studio";
import SilaRecipeStudio2Nav from "./SilaRecipeStudio2Nav";
import CulinaryOverviewWorkspace from "./CulinaryOverviewWorkspace";
import CulinaryRecipePlannerWorkspace from "./CulinaryRecipePlannerWorkspace";
import CulinaryMaterialIntelligenceWorkspace from "./CulinaryMaterialIntelligenceWorkspace";
import CulinaryMaterialRequestsWorkspace from "./CulinaryMaterialRequestsWorkspace";
import CulinaryFoodCostStudioWorkspace from "./CulinaryFoodCostStudioWorkspace";
import CulinaryMenuEventWorkspaces from "./CulinaryMenuEventWorkspaces";
import CulinaryEventMrpStudioWorkspace from "./CulinaryEventMrpStudioWorkspace";
import CulinaryEventPnlStudioWorkspace from "./CulinaryEventPnlStudioWorkspace";
import CulinaryProductionAnalyticsReportWorkspaces from "./CulinaryProductionAnalyticsReportWorkspaces";
import CulinaryAiEngineSetupWorkspace from "./CulinaryAiEngineSetupWorkspace";

interface SilaRecipeStudio2WorkspacesProps {
  route: SilaRecipeRoute;
  flags: SilaCulinaryAiFeatureFlags;
  aiStatus: SilaCulinaryAiStatus | null;
  dashboard: SilaRecipeDashboard | null;
  totalCatalogCount: number;
  permissions?: Partial<SilaMePermissions>;
  showOptions?: boolean;
  optionsPanelId?: string;
  onSelectRecipeRoute: (target: "list" | "legacy-upload" | CulinaryWorkspaceKind) => void;
  onNavigateGlobal?: (navKey: string) => void;
  onRefreshAiStatus: () => void;
}

export const isCulinaryWorkspaceRoute = (
  route: SilaRecipeRoute,
): route is { kind: CulinaryWorkspaceKind } =>
  route.kind !== "list" &&
  route.kind !== "new" &&
  route.kind !== "legacy-upload" &&
  route.kind !== "detail" &&
  route.kind !== "edit";

const ACTIVE_MAT_DRAFT_KEY = "sila.culinary.activeMaterialDraftId";

const SilaRecipeStudio2Workspaces: React.FC<SilaRecipeStudio2WorkspacesProps> = ({
  route,
  flags,
  aiStatus,
  dashboard,
  totalCatalogCount,
  permissions,
  showOptions = true,
  optionsPanelId = "cs2-culinary-options-panel",
  onSelectRecipeRoute,
  onNavigateGlobal,
  onRefreshAiStatus,
}) => {
  const [handoverDraftId, setHandoverDraftId] = React.useState<string | null>(() => {
    try {
      return typeof window !== "undefined" ? window.sessionStorage.getItem(ACTIVE_MAT_DRAFT_KEY) : null;
    } catch {
      return null;
    }
  });

  const [isFullscreen, setIsFullscreen] = useState(false);

  const supportsFullscreen =
    isCulinaryWorkspaceRoute(route) &&
    route.kind !== "overview" &&
    route.kind !== "menus" &&
    route.kind !== "ai-setup";

  useEffect(() => {
    setIsFullscreen(false);
  }, [route.kind]);

  useEffect(() => {
    if (!isFullscreen) return;
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsFullscreen(false);
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [isFullscreen]);

  useEffect(() => {
    if (route.kind === "menus" && onNavigateGlobal) {
      onNavigateGlobal("silaMenuStudio");
    }
  }, [route.kind, onNavigateGlobal]);

  const handleSelectMaterialDraft = React.useCallback((draftId: string | null) => {
    setHandoverDraftId(draftId);
    try {
      if (typeof window !== "undefined") {
        if (draftId) window.sessionStorage.setItem(ACTIVE_MAT_DRAFT_KEY, draftId);
        else window.sessionStorage.removeItem(ACTIVE_MAT_DRAFT_KEY);
      }
    } catch {
      // Ignore storage access restrictions
    }
  }, []);

  const handleContinueToMaterials = React.useCallback(
    (draftId: string) => {
      handleSelectMaterialDraft(draftId);
      onSelectRecipeRoute("materials");
    },
    [handleSelectMaterialDraft, onSelectRecipeRoute],
  );

  const nav = showOptions ? (
    <SilaRecipeStudio2Nav
      id={optionsPanelId}
      activeRoute={route}
      flags={flags}
      aiStatus={aiStatus}
      permissions={permissions}
      onSelectRecipeRoute={onSelectRecipeRoute}
      onNavigateGlobal={onNavigateGlobal}
    />
  ) : null;

  if (!isCulinaryWorkspaceRoute(route)) {
    return nav;
  }

  const renderBody = () => {
    switch (route.kind) {
      case "menus":
        return (
          <StudioEmptyState
            title="ME Studio lives under Menu Engineering"
            description="ME Studio has been consolidated exclusively under the Menu Engineering module."
            action={
              onNavigateGlobal ? (
                <button
                  type="button"
                  className="sila-studio-btn sila-studio-btn--primary"
                  onClick={() => onNavigateGlobal("silaMenuStudio")}
                >
                  <span>Open Menu Engineering</span>
                  <ArrowUpRight size={15} strokeWidth={1.9} aria-hidden="true" />
                </button>
              ) : (
                <button
                  type="button"
                  className="sila-studio-btn sila-studio-btn--secondary"
                  onClick={() => onSelectRecipeRoute("overview")}
                >
                  Return to Overview
                </button>
              )
            }
          />
        );
      case "overview":
        return (
          <CulinaryOverviewWorkspace
            dashboard={dashboard}
            totalCatalogCount={totalCatalogCount}
            demoEnabled={flags.demonstrationModeEnabled}
            onOpenWorkspace={onSelectRecipeRoute}
            onNavigateGlobal={onNavigateGlobal}
          />
        );
      case "ai-planner":
        return (
          <CulinaryRecipePlannerWorkspace
            onOpenWorkspace={onSelectRecipeRoute}
            onContinueToMaterialMapping={handleContinueToMaterials}
          />
        );
      case "materials":
        return (
          <CulinaryMaterialIntelligenceWorkspace
            mode="materials"
            selectedDraftId={handoverDraftId}
            onSelectDraftId={handleSelectMaterialDraft}
            onOpenWorkspace={onSelectRecipeRoute}
          />
        );
      case "material-requests":
        return (
          <CulinaryMaterialRequestsWorkspace
            selectedDraftId={handoverDraftId}
            onSelectDraftId={handleSelectMaterialDraft}
            onOpenWorkspace={onSelectRecipeRoute}
          />
        );
      case "events":
      case "procurement":
        return <CulinaryMenuEventWorkspaces mode={route.kind} onOpenWorkspace={onSelectRecipeRoute} />;
      case "food-cost":
        return (
          <CulinaryFoodCostStudioWorkspace
            selectedDraftId={handoverDraftId}
            onSelectDraftId={handleSelectMaterialDraft}
            onOpenWorkspace={onSelectRecipeRoute}
          />
        );
      case "mrp":
        return <CulinaryEventMrpStudioWorkspace onOpenWorkspace={onSelectRecipeRoute} />;
      case "event-pnl":
        return <CulinaryEventPnlStudioWorkspace />;
      case "production":
        return <CulinaryProductionAnalyticsReportWorkspaces mode="production" />;
      case "analytics":
        return <CulinaryProductionAnalyticsReportWorkspaces mode="analytics" />;
      case "reports":
        return <CulinaryProductionAnalyticsReportWorkspaces mode="reports" />;
      case "ai-setup":
        return <CulinaryAiEngineSetupWorkspace onConfigUpdated={onRefreshAiStatus} />;
    }
  };

  return (
    <div className="cs2-shell">
      {!isFullscreen && nav}
      <div className={`cs2-workspace-fs${isFullscreen ? " is-fullscreen" : ""}`}>
        {supportsFullscreen && (
          <div style={{ display: "flex", justifyContent: "flex-end" }}>
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--secondary"
              onClick={() => setIsFullscreen((fs) => !fs)}
              aria-label={isFullscreen ? "Exit Full-Screen Intelligence Mode" : "Maximize workspace"}
              title={isFullscreen ? "Exit full screen (Esc)" : "Full-Screen Intelligence Mode"}
              data-testid="culinary-fullscreen-toggle"
            >
              {isFullscreen ? <Minimize2 size={14} strokeWidth={1.9} aria-hidden="true" /> : <Maximize2 size={14} strokeWidth={1.9} aria-hidden="true" />}
              <span>{isFullscreen ? "Exit Full Screen" : "Full Screen"}</span>
            </button>
          </div>
        )}
        {renderBody()}
      </div>
    </div>
  );
};

export default SilaRecipeStudio2Workspaces;
