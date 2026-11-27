import React, { useEffect, useMemo, useState } from "react";
import {
  BarChart3,
  Calculator,
  CalendarRange,
  ChefHat,
  Cpu,
  Factory,
  PackageSearch,
  ShieldCheck,
} from "lucide-react";
import type { SilaCulinaryAiFeatureFlags, SilaCulinaryAiStatus } from "../../../../api/silaMe/silaCulinaryAiApi";
import type { SilaMePermissions } from "../../silaMePermissions";
import type { CulinaryWorkspaceKind, SilaRecipeRoute } from "../silaRecipeRoutes";
import {
  StudioSubNavigation,
  StudioTopNavigation,
  type StudioNavGroup,
  type StudioSubNavItem,
} from "../../studio";

export interface SilaRecipeStudio2NavProps {
  id?: string;
  activeRoute: SilaRecipeRoute;
  flags: SilaCulinaryAiFeatureFlags;
  aiStatus: SilaCulinaryAiStatus | null;
  permissions?: Partial<SilaMePermissions>;
  onSelectRecipeRoute: (target: "list" | "legacy-upload" | Exclude<CulinaryWorkspaceKind, "menus">) => void;
  onNavigateGlobal?: (navKey: string) => void;
}

type RecipeTarget = "list" | "legacy-upload" | Exclude<CulinaryWorkspaceKind, "menus">;

interface GroupedSubItem extends StudioSubNavItem {
  routeKind?: RecipeTarget;
  globalNavKey?: string;
}

interface RecipeNavGroupConfig {
  id: string;
  label: string;
  icon: React.ReactNode;
  items: GroupedSubItem[];
}

function resolveGroupForRoute(route: SilaRecipeRoute): string {
  switch (route.kind) {
    case "list":
    case "new":
    case "detail":
    case "edit":
    case "legacy-upload":
    case "ai-planner":
      return "recipes";
    case "materials":
    case "material-requests":
      return "materials";
    case "events":
    case "mrp":
    case "procurement":
      return "events";
    case "production":
      return "production";
    case "food-cost":
    case "event-pnl":
      return "costing";
    case "analytics":
    case "reports":
      return "analytics";
    case "ai-setup":
      return "ai-setup";
    case "overview":
    default:
      return "";
  }
}

const SilaRecipeStudio2Nav: React.FC<SilaRecipeStudio2NavProps> = ({
  id = "cs2-culinary-options-panel",
  activeRoute,
  flags,
  aiStatus,
  permissions,
  onSelectRecipeRoute,
  onNavigateGlobal,
}) => {
  const routeGroupId = useMemo(() => resolveGroupForRoute(activeRoute), [activeRoute]);
  const [selectedGroupId, setSelectedGroupId] = useState<string>(routeGroupId);

  useEffect(() => {
    setSelectedGroupId(routeGroupId);
  }, [routeGroupId]);

  if (!flags.culinarySuiteEnabled) {
    return null;
  }

  const canManageMasterData = permissions?.manageMasterData ?? true;
  const canApproveRecipe = permissions?.approveRecipe ?? true;
  const canViewAudit = permissions?.viewAudit ?? true;

  const groups: RecipeNavGroupConfig[] = [
    {
      id: "recipes",
      label: "Recipes",
      icon: <ChefHat size={15} strokeWidth={1.9} />,
      items: [
        { id: "list", label: "Recipe Master", routeKind: "list" },
        { id: "legacy-upload", label: "Legacy Recipe Upload", routeKind: "legacy-upload" },
        {
          id: "ai-planner",
          label: "AI Recipe Planner",
          routeKind: "ai-planner",
          statusBadge: flags.recipePlannerPreviewEnabled ? "Preview" : "Not Enabled",
          statusTone: flags.recipePlannerPreviewEnabled ? "info" : "neutral",
          disabled: !flags.recipePlannerPreviewEnabled,
        },
      ],
    },
    {
      id: "materials",
      label: "Materials",
      icon: <PackageSearch size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "materials",
          label: "Material Intelligence",
          routeKind: "materials",
          statusBadge: flags.materialIntelligencePreviewEnabled ? undefined : "Not Enabled",
          disabled: !flags.materialIntelligencePreviewEnabled,
        },
        {
          id: "material-requests",
          label: "Material Requests",
          routeKind: "material-requests",
          disabled: !flags.materialIntelligencePreviewEnabled,
        },
      ],
    },
    {
      id: "events",
      label: "Events",
      icon: <CalendarRange size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "events",
          label: "AI Event Planner",
          routeKind: "events",
          disabled: !flags.menuEventPreviewEnabled,
        },
        {
          id: "mrp",
          label: "Event Material Requirements",
          routeKind: "mrp",
          statusBadge: flags.interactiveSimulatorsEnabled ? undefined : "Not Enabled",
          disabled: !flags.interactiveSimulatorsEnabled,
        },
        {
          id: "procurement",
          label: "Event Procurement",
          routeKind: "procurement",
          disabled: !flags.menuEventPreviewEnabled,
        },
      ],
    },
    {
      id: "production",
      label: "Production",
      icon: <Factory size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "production",
          label: "Production Planner",
          routeKind: "production",
          disabled: !(flags.culinaryPhase4AEnabled ?? flags.menuEventPreviewEnabled),
        },
      ],
    },
    {
      id: "costing",
      label: "Costing",
      icon: <Calculator size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "food-cost",
          label: "Food Cost Studio",
          routeKind: "food-cost",
          disabled: !flags.menuEventPreviewEnabled,
        },
        {
          id: "event-pnl",
          label: "Event P&L",
          routeKind: "event-pnl",
          disabled: !flags.interactiveSimulatorsEnabled,
        },
      ],
    },
    {
      id: "analytics",
      label: "Analytics",
      icon: <BarChart3 size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "analytics",
          label: "Analytics",
          routeKind: "analytics",
          disabled: !flags.analyticsReportPreviewEnabled,
        },
        {
          id: "reports",
          label: "Report Studio",
          routeKind: "reports",
          disabled: !flags.analyticsReportPreviewEnabled,
        },
      ],
    },
    {
      id: "governance",
      label: "Governance",
      icon: <ShieldCheck size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "approvals",
          label: "Approvals",
          globalNavKey: "silaRecipeApprovals",
          disabled: !onNavigateGlobal || !canApproveRecipe,
          tooltip: canApproveRecipe ? "Open Recipe Approvals" : "Requires APPROVE_SILA_RECIPE permission",
        },
        {
          id: "tracker",
          label: "History & Tracker",
          globalNavKey: "silaMeTracker",
          disabled: !onNavigateGlobal || (!canViewAudit && permissions?.manageRecipe === false),
          tooltip: "Open SILA ME Consolidated Transaction Tracker",
        },
      ],
    },
    {
      id: "ai-setup",
      label: "AI Setup",
      icon: <Cpu size={15} strokeWidth={1.9} />,
      items: [
        {
          id: "ai-setup",
          label: "Culinary AI Engine Setup",
          routeKind: "ai-setup",
          statusBadge: !flags.aiSetupEnabled
            ? "Not Enabled"
            : !aiStatus?.configured && aiStatus?.operatingMode !== "LOCAL"
              ? "Configuration Required"
              : !canManageMasterData
                ? "Read Only"
                : undefined,
          statusTone: !aiStatus?.configured && aiStatus?.operatingMode !== "LOCAL" ? "warning" : "neutral",
          disabled: !flags.aiSetupEnabled,
        },
      ],
    },
  ];

  const topGroups: StudioNavGroup[] = groups.map((group) => ({
    id: group.id,
    label: group.label,
    icon: group.icon,
    items: group.items,
    hasDropdown: group.items.length > 1,
    active: selectedGroupId === group.id,
  }));

  const activeGroup = groups.find((g) => g.id === selectedGroupId) ?? null;

  const activeSubId = useMemo(() => {
    if (activeRoute.kind === "new" || activeRoute.kind === "detail" || activeRoute.kind === "edit") {
      return "list";
    }
    return activeRoute.kind;
  }, [activeRoute.kind]);

  const handleSelectGroup = (groupId: string) => {
    const group = groups.find((g) => g.id === groupId);
    if (!group) return;
    setSelectedGroupId(groupId);
    if (group.items.length === 1) {
      const only = group.items[0];
      if (!only.disabled) {
        if (only.globalNavKey && onNavigateGlobal) {
          onNavigateGlobal(only.globalNavKey);
        } else if (only.routeKind) {
          onSelectRecipeRoute(only.routeKind);
        }
      }
      return;
    }
    const firstEnabled = group.items.find((item) => !item.disabled && item.routeKind);
    if (firstEnabled?.routeKind && groupId !== routeGroupId) {
      onSelectRecipeRoute(firstEnabled.routeKind);
    }
  };

  const handleSelectSubItem = (subId: string) => {
    const item = activeGroup?.items.find((i) => i.id === subId);
    if (!item || item.disabled) return;
    if (item.globalNavKey && onNavigateGlobal) {
      onNavigateGlobal(item.globalNavKey);
    } else if (item.routeKind) {
      onSelectRecipeRoute(item.routeKind);
    }
  };

  const aiMode = aiStatus?.operatingMode ?? "LOCAL";
  const externalAiEnabled = Boolean(aiStatus?.allowExternalAi);
  const configRequired = Boolean(aiStatus && !aiStatus.configured && aiMode !== "LOCAL");

  return (
    <div id={id} data-testid="culinary-studio-nav">
      <StudioTopNavigation
        title="Culinary Intelligence"
        ariaLabel="Culinary Intelligence navigation"
        groups={topGroups}
        onSelectGroup={(group) => handleSelectGroup(group.id)}
        rightContent={
          <>
            <span className="sila-studio-status-pill sila-studio-status-pill--info">
              AI Mode: {aiMode}
            </span>
            <span className={`sila-studio-status-pill ${externalAiEnabled ? "sila-studio-status-pill--success" : "sila-studio-status-pill--neutral"}`}>
              External AI: {externalAiEnabled ? "Enabled" : "Disabled"}
            </span>
            {configRequired && (
              <span className="sila-studio-status-pill sila-studio-status-pill--warning">
                Configuration Required
              </span>
            )}
          </>
        }
      />

      {activeGroup && (
        <StudioSubNavigation
          ariaLabel={`${activeGroup.label} workspaces`}
          items={activeGroup.items}
          activeId={activeSubId}
          onSelect={handleSelectSubItem}
        />
      )}
    </div>
  );
};

export default SilaRecipeStudio2Nav;
