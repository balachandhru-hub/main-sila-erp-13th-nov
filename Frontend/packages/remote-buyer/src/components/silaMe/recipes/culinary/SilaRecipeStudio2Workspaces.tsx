import React from "react";
import type { SilaCulinaryAiFeatureFlags, SilaCulinaryAiStatus } from "../../../../api/silaMe/silaCulinaryAiApi";
import type { SilaRecipeDashboard } from "../../../../api/silaMe/silaRecipeToolsApi";
import type { SilaMePermissions } from "../../silaMePermissions";
import type { CulinaryWorkspaceKind, SilaRecipeRoute } from "../silaRecipeRoutes";
import SilaRecipeStudio2Nav from "./SilaRecipeStudio2Nav";
import CulinaryOverviewWorkspace from "./CulinaryOverviewWorkspace";
import CulinaryRecipePlannerWorkspace from "./CulinaryRecipePlannerWorkspace";
import CulinaryMaterialIntelligenceWorkspace from "./CulinaryMaterialIntelligenceWorkspace";
import CulinaryMenuEventWorkspaces from "./CulinaryMenuEventWorkspaces";
import CulinaryMrpSimulatorWorkspace from "./CulinaryMrpSimulatorWorkspace";
import CulinaryPnlSimulatorWorkspace from "./CulinaryPnlSimulatorWorkspace";
import CulinaryProductionAnalyticsReportWorkspaces from "./CulinaryProductionAnalyticsReportWorkspaces";
import CulinaryAiEngineSetupWorkspace from "./CulinaryAiEngineSetupWorkspace";

interface SilaRecipeStudio2WorkspacesProps {
  route: SilaRecipeRoute;
  flags: SilaCulinaryAiFeatureFlags;
  aiStatus: SilaCulinaryAiStatus | null;
  dashboard: SilaRecipeDashboard | null;
  totalCatalogCount: number;
  permissions?: Partial<SilaMePermissions>;
  onSelectRecipeRoute: (target: "list" | "legacy-upload" | CulinaryWorkspaceKind) => void;
  onNavigateGlobal?: (navKey: string) => void;
  onRefreshAiStatus: () => void;
}

export const isCulinaryWorkspaceRoute = (
  route: SilaRecipeRoute
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
    [handleSelectMaterialDraft, onSelectRecipeRoute]
  );

  const nav = (
    <SilaRecipeStudio2Nav
      activeRoute={route}
      flags={flags}
      aiStatus={aiStatus}
      permissions={permissions}
      onSelectRecipeRoute={onSelectRecipeRoute}
      onNavigateGlobal={onNavigateGlobal}
    />
  );

  if (!isCulinaryWorkspaceRoute(route)) {
    return nav;
  }

  const renderBody = () => {
    switch (route.kind) {
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
          <CulinaryMaterialIntelligenceWorkspace
            mode="material-requests"
            selectedDraftId={handoverDraftId}
            onSelectDraftId={handleSelectMaterialDraft}
            onOpenWorkspace={onSelectRecipeRoute}
          />
        );
      case "menus":
      case "events":
      case "procurement":
      case "food-cost":
        return <CulinaryMenuEventWorkspaces mode={route.kind} onOpenWorkspace={onSelectRecipeRoute} />;
      case "mrp":
        return <CulinaryMrpSimulatorWorkspace />;
      case "event-pnl":
        return <CulinaryPnlSimulatorWorkspace />;
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
    <div className="sila-me srec-page rstudio-page">
      {nav}
      <div style={{ marginTop: 16 }}>{renderBody()}</div>
    </div>
  );
};

export default SilaRecipeStudio2Workspaces;
