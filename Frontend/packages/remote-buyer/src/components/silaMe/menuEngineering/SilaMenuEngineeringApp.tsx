import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  BarChart3,
  BookOpen,
  CalendarRange,
  Bot,
  ChefHat,
  ClipboardList,
  DollarSign,
  FileBarChart2,
  Plus,
  Save,
  Settings,
  Sparkles,
  UtensilsCrossed,
} from "lucide-react";
import { Loader, toastService } from "@vosox/shared-ui";
import {
  getMenuEngineering,
  type MenuFilter,
  type MenuLookups,
} from "../../../api/silaMe/silaMenuEngineeringApi";
import {
  getMePermissions,
  type MeStudioPermissionsDto,
} from "../../../api/silaMe/menuEngineeringStudioApi";
import {
  getMeAiStatus,
  getMeProactiveAlerts,
  type AiOperatingMode,
  type AiOperatingModeStatus,
  type MeProactiveAlert,
} from "../../../api/silaMe/menuEngineeringAiApi";
import type { SilaMePermissions } from "../silaMePermissions";
import {
  StudioActionBar,
  StudioFilterBar,
  StudioHeroPanel,
  StudioIntelligenceToggle,
  StudioOverviewButton,
  StudioShell,
  StudioSubNavigation,
  StudioTopNavigation,
  type StudioActionItem,
  type StudioNavGroup,
  type StudioSubNavItem,
} from "../studio";
import MeStudioWorkspace, { type MeStudioHeroSnapshot } from "./MeStudioWorkspace";
import MeOperationalViews from "./MeOperationalViews";
import MenuReportsPanel from "./MenuReportsPanel";
import { AiOperatingModeSwitch } from "./ai/AiOperatingModeSwitch";
import { AiContextualAssistantPanel } from "./ai/AiContextualAssistantPanel";
import "../silaMeTheme.css";
import "./SilaMenuEngineering.css";

type ViewKey =
  | "silaMenuStudio"
  | "silaMenuDashboard"
  | "silaMenuItems"
  | "silaMenuPlanning"
  | "silaMenuPortions"
  | "silaMenuDemand"
  | "silaMenuPosSales"
  | "silaMenuPosMapping"
  | "silaMenuPerformance"
  | "silaMenuConsumption"
  | "silaMenuCost"
  | "silaMenuRecommendations"
  | "silaMenuIntelligence"
  | "silaMenuWhatIf"
  | "silaMenuSetup";

const SCREEN_TITLES: Record<ViewKey, string> = {
  silaMenuStudio: "ME Studio",
  silaMenuDashboard: "ME Studio",
  silaMenuItems: "Menu Items",
  silaMenuPlanning: "Menu Planning",
  silaMenuPortions: "Portion Planning",
  silaMenuDemand: "Demand Planning",
  silaMenuPosSales: "POS Sales",
  silaMenuPosMapping: "POS Mapping",
  silaMenuPerformance: "Menu Performance",
  silaMenuConsumption: "Consumption Analysis",
  silaMenuCost: "Cost & Margin Analysis",
  silaMenuRecommendations: "Purchase Recommendations",
  silaMenuIntelligence: "AI Menu Intelligence",
  silaMenuWhatIf: "What-If Simulation",
  silaMenuSetup: "Setup",
};

const ME_NAV_GROUPS: StudioNavGroup[] = [
  {
    id: "me-studio",
    label: "ME Studio",
    icon: <UtensilsCrossed size={14} aria-hidden="true" />,
    items: [{ id: "silaMenuStudio", label: "ME Studio" }],
  },
  {
    id: "planning",
    label: "Planning",
    icon: <CalendarRange size={14} aria-hidden="true" />,
    items: [
      { id: "silaMenuItems", label: "Menu Items" },
      { id: "silaMenuPlanning", label: "Menu Planning" },
      { id: "silaMenuPortions", label: "Portion Planning" },
      { id: "silaMenuDemand", label: "Demand Planning" },
    ],
  },
  {
    id: "sales-intel",
    label: "Sales Intelligence",
    icon: <BarChart3 size={14} aria-hidden="true" />,
    items: [
      { id: "silaMenuPosSales", label: "POS Sales" },
      { id: "silaMenuPosMapping", label: "POS Mapping" },
      { id: "silaMenuPerformance", label: "Menu Performance" },
    ],
  },
  {
    id: "cost-consumption",
    label: "Cost & Consumption",
    icon: <DollarSign size={14} aria-hidden="true" />,
    items: [
      { id: "silaMenuCost", label: "Cost & Margin Analysis" },
      { id: "silaMenuConsumption", label: "Consumption Analysis" },
      { id: "silaMenuRecommendations", label: "Purchase Recommendations" },
    ],
  },
  {
    id: "ai-simulation",
    label: "AI & Simulation",
    icon: <Sparkles size={14} aria-hidden="true" />,
    items: [
      { id: "silaMenuIntelligence", label: "AI Menu Intelligence" },
      { id: "silaMenuWhatIf", label: "What-If Simulation" },
    ],
  },
  {
    id: "setup",
    label: "Setup",
    icon: <Settings size={14} aria-hidden="true" />,
    items: [{ id: "silaMenuSetup", label: "Setup" }],
  },
];

const resolveGroupForKey = (key: ViewKey): string => {
  const normalized = key === "silaMenuDashboard" ? "silaMenuStudio" : key;
  const match = ME_NAV_GROUPS.find((g) => (g.items ?? []).some((it: StudioSubNavItem) => it.id === normalized));
  return match?.id ?? "me-studio";
};

interface Props {
  navKey: ViewKey;
  permissions?: SilaMePermissions;
  canManage: boolean;
  canRequestPurchase: boolean;
  canManageRecipe: boolean;
  onNavigate: (key: string) => void;
}

const today = (): string => new Date().toISOString().slice(0, 10);
const monthAgo = (): string => {
  const d = new Date();
  d.setDate(d.getDate() - 30);
  return d.toISOString().slice(0, 10);
};

export type MeStudioHeroCommand =
  | "new-menu"
  | "save-draft"
  | "optimize-ai"
  | "compare-scenarios"
  | "submit-approval"
  | "open-library";

const SilaMenuEngineeringApp: React.FC<Props> = ({
  navKey,
  permissions,
  canManage,
  canRequestPurchase,
  onNavigate,
}) => {
  const [dateFrom, setDateFrom] = useState(monthAgo());
  const [dateTo, setDateTo] = useState(today());
  const [outletId, setOutletId] = useState("");
  const [propertyId, setPropertyId] = useState("");
  const [mealPeriod, setMealPeriod] = useState("");
  const [lookups, setLookups] = useState<MenuLookups | undefined>(undefined);
  const [showReports, setShowReports] = useState(false);
  const [apiPerms, setApiPerms] = useState<MeStudioPermissionsDto | null>(null);
  const [loadingPerms, setLoadingPerms] = useState(true);
  const [operatingMode, setOperatingMode] = useState<AiOperatingMode>("AI_ASSISTED");
  const [aiStatus, setAiStatus] = useState<AiOperatingModeStatus | null>(null);
  const [proactiveAlerts, setProactiveAlerts] = useState<MeProactiveAlert[]>([]);
  const [activeMenuPlanId, setActiveMenuPlanId] = useState<string | undefined>(undefined);
  const [activeMenuItemId, setActiveMenuItemId] = useState<string | undefined>(undefined);
  const [copilotOpen, setCopilotOpen] = useState(false);
  const [studioSnapshot, setStudioSnapshot] = useState<MeStudioHeroSnapshot | null>(null);
  const studioCommandHandlerRef = useRef<((cmd: MeStudioHeroCommand) => void) | null>(null);
  const [operationalActionTick, setOperationalActionTick] = useState<number>(0);
  const [showOptions, setShowOptions] = useState<boolean>(() => navKey !== "silaMenuStudio" && navKey !== "silaMenuDashboard");

  const normalizedKey: ViewKey = navKey === "silaMenuDashboard" ? "silaMenuStudio" : navKey;
  const activeGroupId = resolveGroupForKey(normalizedKey);
  const activeGroup = useMemo(
    () => ME_NAV_GROUPS.find((g) => g.id === activeGroupId) ?? ME_NAV_GROUPS[0],
    [activeGroupId]
  );

  const filter = useMemo<MenuFilter>(
    () => ({
      dateFrom,
      dateTo,
      outletLocationId: outletId || undefined,
      propertyId: propertyId || undefined,
      mealPeriod: mealPeriod || undefined,
    }),
    [dateFrom, dateTo, outletId, propertyId, mealPeriod]
  );

  useEffect(() => {
    let active = true;
    setLoadingPerms(true);
    getMePermissions()
      .catch(() => null)
      .then((p) => {
        if (!active) return;
        if (p) setApiPerms(p);
      })
      .finally(() => {
        if (active) setLoadingPerms(false);
      });

    getMenuEngineering("items", { limit: 1 })
      .catch(() => null)
      .then((env) => {
        if (!active) return;
        if (env?.lookups) setLookups(env.lookups);
      });

    getMeAiStatus(propertyId || undefined)
      .catch(() => null)
      .then((aiSt) => {
        if (!active || !aiSt) return;
        setAiStatus(aiSt);
        setOperatingMode(aiSt.operatingMode || "AI_ASSISTED");
      });

    getMeProactiveAlerts(propertyId || undefined)
      .catch(() => [])
      .then((alerts) => {
        if (!active || !Array.isArray(alerts)) return;
        setProactiveAlerts(alerts);
      });

    return () => {
      active = false;
    };
  }, [propertyId]);

  const effectivePerms = useMemo<MeStudioPermissionsDto>(
    () =>
      apiPerms ?? {
        viewMeStudio: permissions?.viewMeStudio ?? true,
        createMenu: permissions?.createMenu ?? canManage,
        editDraftMenu: permissions?.editDraftMenu ?? canManage,
        manageMenuItems: permissions?.manageMenuItems ?? canManage,
        managePortionPlans: permissions?.managePortionPlans ?? canManage,
        manageDemandPlans: permissions?.manageDemandPlans ?? canManage,
        viewPosSales: permissions?.viewPosSales ?? true,
        managePosMapping: permissions?.managePosMapping ?? canManage,
        viewMenuPerformance: permissions?.viewMenuPerformance ?? true,
        viewMenuCost: permissions?.viewMenuCost ?? true,
        viewMenuMargin: permissions?.viewMenuMargin ?? true,
        generatePurchaseRecommendation: permissions?.generateMenuPurchaseRec ?? canRequestPurchase,
        useAiMenuIntelligence: permissions?.useAiMenuIntelligence ?? true,
        runMenuSimulation: permissions?.runMenuSimulation ?? true,
        approveMenu: permissions?.approveMenu ?? canManage,
        publishMenu: permissions?.publishMenu ?? canManage,
        manageMenuSetup: permissions?.manageMenuSetup ?? canManage,
        menuEngineeringEnabled: true,
        meStudioEnabled: true,
      },
    [apiPerms, permissions, canManage, canRequestPurchase]
  );

  const isStudio = normalizedKey === "silaMenuStudio";

  useEffect(() => {
    if (!isStudio) setShowOptions(true);
  }, [isStudio, normalizedKey]);

  const selectedPropertyName = useMemo(
    () => lookups?.properties.find((p) => p.id === propertyId)?.name ?? "All Properties",
    [lookups?.properties, propertyId]
  );

  const selectedOutletName = useMemo(
    () => lookups?.outlets.find((o) => o.id === outletId)?.name ?? "All Outlets",
    [lookups?.outlets, outletId]
  );

  const registerStudioCommandHandler = useCallback(
    (handler: ((cmd: MeStudioHeroCommand) => void) | null) => {
      studioCommandHandlerRef.current = handler;
    },
    []
  );

  const dispatchStudioCommand = (cmd: MeStudioHeroCommand) => {
    studioCommandHandlerRef.current?.(cmd);
  };

  const heroActions = useMemo<StudioActionItem[]>(() => {
    const items: StudioActionItem[] = [];

    if (isStudio && !showReports) {
      if (effectivePerms.createMenu) {
        items.push({
          id: "me-new-menu",
          label: "New Menu",
          icon: <Plus size={14} aria-hidden="true" />,
          variant: "secondary",
          onClick: () => dispatchStudioCommand("new-menu"),
          testId: "me-hero-new-menu",
        });
      }
      if (effectivePerms.editDraftMenu || effectivePerms.createMenu) {
        items.push({
          id: "me-save-draft",
          label: studioSnapshot?.saving ? "Saving..." : "Save Draft",
          icon: <Save size={14} aria-hidden="true" />,
          variant: "primary",
          disabled: studioSnapshot?.saving,
          onClick: () => dispatchStudioCommand("save-draft"),
          testId: "me-hero-save-draft",
        });
      }
      if (effectivePerms.useAiMenuIntelligence && operatingMode !== "MANUAL") {
        items.push({
          id: "me-optimize-ai",
          label: "Optimize with AI",
          icon: <Sparkles size={14} aria-hidden="true" />,
          variant: "secondary",
          onClick: () => dispatchStudioCommand("optimize-ai"),
          testId: "me-hero-optimize-ai",
        });
      }
      if (studioSnapshot?.selectedPlanId && (effectivePerms.approveMenu || effectivePerms.editDraftMenu)) {
        items.push({
          id: "me-submit-approval",
          label: "Submit for Approval",
          icon: <ClipboardList size={14} aria-hidden="true" />,
          variant: "secondary",
          onClick: () => dispatchStudioCommand("submit-approval"),
          testId: "me-hero-submit-approval",
        });
      }
      items.push({
        id: "me-dish-library",
        label: studioSnapshot?.dishLibraryOpen ? "Hide Dish Library" : "Dish Library",
        icon: <BookOpen size={14} aria-hidden="true" />,
        variant: "secondary",
        onClick: () => dispatchStudioCommand("open-library"),
        testId: "me-hero-dish-library",
      });
    } else if (!showReports) {
      if (normalizedKey === "silaMenuItems" && effectivePerms.manageMenuItems) {
        items.push({
          id: "me-new-item",
          label: "New Menu Item",
          icon: <Plus size={14} aria-hidden="true" />,
          variant: "primary",
          onClick: () => setOperationalActionTick((t) => t + 1),
          testId: "me-hero-new-item",
        });
      }
      if (normalizedKey === "silaMenuPlanning") {
        items.push({
          id: "me-open-studio",
          label: "Open in ME Studio",
          icon: <UtensilsCrossed size={14} aria-hidden="true" />,
          variant: "primary",
          onClick: () => onNavigate("silaMenuStudio"),
        });
      }
    }

    return items;
  }, [
    effectivePerms.approveMenu,
    effectivePerms.createMenu,
    effectivePerms.editDraftMenu,
    effectivePerms.manageMenuItems,
    effectivePerms.useAiMenuIntelligence,
    isStudio,
    normalizedKey,
    onNavigate,
    operatingMode,
    showReports,
    studioSnapshot?.dishLibraryOpen,
    studioSnapshot?.saving,
    studioSnapshot?.selectedPlanId,
  ]);

  const submenuItems = useMemo<StudioSubNavItem[]>(
    () =>
      (activeGroup.items ?? []).map((it: StudioSubNavItem) => ({
        id: it.id,
        label: it.label,
        active: !showReports && it.id === normalizedKey,
      })),
    [activeGroup.items, normalizedKey, showReports]
  );

  const aiStatusLabel = useMemo(() => {
    const modeLabel =
      operatingMode === "MANUAL"
        ? "Manual"
        : operatingMode === "AGENTIC"
          ? "Agentic"
          : "AI Assisted";
    const providerLabel = aiStatus?.aiProviderAvailable ? "External AI: Enabled" : "AI Mode: Local";
    return `${providerLabel} · ${modeLabel}`;
  }, [aiStatus?.aiProviderAvailable, operatingMode]);

  if (loadingPerms) return <Loader />;

  if (!effectivePerms.menuEngineeringEnabled && normalizedKey !== "silaMenuSetup") {
    return (
      <StudioShell>
        <StudioHeroPanel
          title="Menu Engineering"
          subtitle="Intelligent Menu Engineering Workspace"
          topRight={
            <StudioOverviewButton
              active={false}
              tooltip="Return to Menu Engineering"
              ariaLabel="Menu Engineering Overview"
              onClick={() => onNavigate("silaMenuStudio")}
            />
          }
        />
        <div className="sila-studio-panel">
          <p className="sila-studio-muted" style={{ margin: 0 }}>
            Menu Engineering is currently disabled by tenant feature flag. Existing procurement and culinary workflows remain unaffected.
          </p>
          {effectivePerms.manageMenuSetup && (
            <div>
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--primary"
                onClick={() => onNavigate("silaMenuSetup")}
              >
                Open Setup
              </button>
            </div>
          )}
        </div>
      </StudioShell>
    );
  }

  return (
    <StudioShell>
      {/* ROW 2: Shared Ice-Blue Studio Hero Panel */}
      <StudioHeroPanel
        title={isStudio && !showReports ? "ME Studio" : "Menu Engineering"}
        subtitle={
          isStudio && !showReports
            ? "AI-Powered Menu Design & Optimization"
            : `Intelligent Menu Engineering Workspace${showReports ? " · Reports" : ` · ${SCREEN_TITLES[normalizedKey] ?? ""}`}`
        }
        titleAdornment={
          isStudio && !showReports && studioSnapshot ? (
            <div style={{ display: "inline-flex", alignItems: "center", gap: "0.35rem", flexWrap: "wrap" }}>
              <span className="sila-studio-badge sila-studio-badge--info">{selectedPropertyName}</span>
              <span className="sila-studio-badge sila-studio-badge--neutral">{selectedOutletName}</span>
              <span className="sila-studio-badge sila-studio-badge--info">
                {studioSnapshot.menuName} · v{studioSnapshot.versionNumber}
              </span>
              <span
                className={`sila-studio-badge ${
                  studioSnapshot.status === "PUBLISHED" || studioSnapshot.status === "APPROVED"
                    ? "sila-studio-badge--success"
                    : "sila-studio-badge--warning"
                }`}
              >
                {studioSnapshot.status}
              </span>
              <span className="sila-studio-badge sila-studio-badge--neutral sila-tabular">
                {studioSnapshot.guestCount} guests · {studioSnapshot.serviceType.replace(/_/g, " ")}
              </span>
              {studioSnapshot.isDirty && (
                <span className="sila-studio-badge sila-studio-badge--warning">Unsaved changes</span>
              )}
            </div>
          ) : undefined
        }
        topRight={
          <>
            <StudioOverviewButton
              active={false}
              tooltip="Open Recipe Studio"
              ariaLabel="Open Recipe Studio"
              testId="me-recipe-studio-btn"
              icon={<ChefHat size={18} strokeWidth={2} aria-hidden="true" />}
              onClick={() => {
                onNavigate("silaRecipes");
                toastService.info("Opened Recipe Studio (Culinary Master)");
              }}
            />
            <StudioOverviewButton
              active={showReports}
              tooltip={showReports ? "Close Reports" : "Reports (22)"}
              ariaLabel={showReports ? "Close Reports" : "Open Reports"}
              testId="me-hero-reports"
              icon={<FileBarChart2 size={18} strokeWidth={2} aria-hidden="true" />}
              onClick={() => setShowReports((r) => !r)}
            />
            <StudioOverviewButton
              active={copilotOpen}
              tooltip={copilotOpen ? "Close AI Copilot" : "AI Copilot"}
              ariaLabel={copilotOpen ? "Close AI Copilot" : "Open AI Copilot"}
              testId="me-ai-copilot-toggle"
              icon={<Bot size={18} strokeWidth={2} aria-hidden="true" />}
              onClick={() => setCopilotOpen((o) => !o)}
            />
            <StudioIntelligenceToggle
              iconOnly
              label="Menu Engineering Intelligence"
              expanded={showOptions}
              controlsId="me-options-panel"
              testId="me-options-toggle"
              icon={<Sparkles size={18} strokeWidth={2} />}
              onClick={() => setShowOptions((o) => !o)}
            />
            <StudioOverviewButton
              active={isStudio && !showReports}
              tooltip="Return to ME Studio"
              ariaLabel="Return to ME Studio"
              testId="me-overview-button"
              onClick={() => {
                setShowReports(false);
                onNavigate("silaMenuStudio");
              }}
            />
          </>
        }
        actions={<StudioActionBar actions={heroActions} ariaLabel="Menu Engineering actions" />}
      />

      {showOptions && (
        <div id="me-options-panel" data-testid="me-options-panel">
          <StudioTopNavigation
            groups={ME_NAV_GROUPS}
            activeGroupId={activeGroupId}
            activeItemId={showReports ? undefined : normalizedKey}
            onSelectGroup={(group) => {
              setShowReports(false);
              const first = group.items?.[0];
              if (first) onNavigate(first.id);
            }}
            onSelectItem={(itemId: string) => {
              setShowReports(false);
              onNavigate(itemId);
            }}
            ariaLabel="Menu Engineering navigation"
            rightSlot={
              <span
                className={`sila-studio-badge ${
                  aiStatus?.aiProviderAvailable ? "sila-studio-badge--success" : "sila-studio-badge--info"
                }`}
              >
                {aiStatusLabel}
              </span>
            }
          />
          {activeGroup.items && activeGroup.items.length > 1 && (
            <StudioSubNavigation
              items={submenuItems}
              onSelect={(id: string) => {
                setShowReports(false);
                onNavigate(id);
              }}
              ariaLabel={`${activeGroup.label} submenu`}
            />
          )}
        </div>
      )}

      {/* ROW 5: Compact Global Filter Toolbar */}
      <StudioFilterBar>
        <label className="sila-field" style={{ minWidth: "120px" }}>
          <span className="sila-label">From</span>
          <input className="sila-input" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
        </label>
        <label className="sila-field" style={{ minWidth: "120px" }}>
          <span className="sila-label">To</span>
          <input className="sila-input" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
        </label>
        <label className="sila-field" style={{ minWidth: "145px" }}>
          <span className="sila-label">Property</span>
          <select className="sila-input" value={propertyId} onChange={(e) => setPropertyId(e.target.value)}>
            <option value="">All properties</option>
            {(lookups?.properties ?? []).map((row) => (
              <option key={row.id} value={row.id}>{row.name}</option>
            ))}
          </select>
        </label>
        <label className="sila-field" style={{ minWidth: "145px" }}>
          <span className="sila-label">Outlet</span>
          <select className="sila-input" value={outletId} onChange={(e) => setOutletId(e.target.value)}>
            <option value="">All outlets</option>
            {(lookups?.outlets ?? []).map((row) => (
              <option key={row.id} value={row.id}>{row.name}</option>
            ))}
          </select>
        </label>
        <label className="sila-field" style={{ minWidth: "135px" }}>
          <span className="sila-label">Meal Period</span>
          <select className="sila-input" value={mealPeriod} onChange={(e) => setMealPeriod(e.target.value)}>
            <option value="">All meal periods</option>
            {(lookups?.mealPeriods ?? ["Breakfast", "Lunch", "Dinner", "Brunch", "All-Day"]).map((row) => (
              <option key={row} value={row}>{row}</option>
            ))}
          </select>
        </label>
        <div style={{ marginLeft: "auto" }}>
          <AiOperatingModeSwitch
            mode={operatingMode}
            status={aiStatus}
            onChangeMode={setOperatingMode}
          />
        </div>
      </StudioFilterBar>

      {proactiveAlerts.length > 0 && (
        <div className="sme-ai-proactive-strip">
          <div>
            <strong>{proactiveAlerts.length} Proactive Intelligence Alert{proactiveAlerts.length > 1 ? "s" : ""}:</strong>{" "}
            {proactiveAlerts[0].title} — {proactiveAlerts[0].summary}
          </div>
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--secondary sila-studio-btn--sm"
            onClick={() => onNavigate("silaMenuIntelligence")}
          >
            Open AI Menu Intelligence
          </button>
        </div>
      )}

      {/* ROW 7: Full-Width Working Area (NO permanent sidebar) */}
      <div>
        {showReports ? (
          <MenuReportsPanel filter={filter} onClose={() => setShowReports(false)} />
        ) : isStudio ? (
          <MeStudioWorkspace
            initialPlanId={activeMenuPlanId}
            propertyId={filter.propertyId}
            outletLocationId={filter.outletLocationId}
            operatingMode={operatingMode}
            onActiveContextChange={(pId, itemId) => {
              if (pId) setActiveMenuPlanId(pId);
              if (itemId) setActiveMenuItemId(itemId);
            }}
            onOpenReports={() => setShowReports(true)}
            onNavigate={onNavigate}
            onHeroSnapshotChange={setStudioSnapshot}
            registerCommandHandler={registerStudioCommandHandler}
          />
        ) : (
          <MeOperationalViews
            navKey={normalizedKey}
            filter={filter}
            lookups={lookups}
            permissions={effectivePerms}
            operatingMode={operatingMode}
            externalCreateSignal={operationalActionTick}
            onOpenInStudio={(planId) => {
              if (planId) setActiveMenuPlanId(planId);
              onNavigate("silaMenuStudio");
            }}
            onNavigate={onNavigate}
          />
        )}
      </div>

      {/* Collapsed-by-default Temporary AI Copilot Drawer */}
      <AiContextualAssistantPanel
        screenKey={normalizedKey}
        operatingMode={operatingMode}
        propertyId={filter.propertyId}
        outletLocationId={filter.outletLocationId}
        menuPlanId={activeMenuPlanId}
        menuItemId={activeMenuItemId}
        open={copilotOpen}
        onClose={() => setCopilotOpen(false)}
        onTriggerAction={(target) => {
          if (target.includes("purchase")) onNavigate("silaMenuRecommendations");
          else if (target.includes("simulate")) onNavigate("silaMenuWhatIf");
          else onNavigate("silaMenuIntelligence");
        }}
      />
    </StudioShell>
  );
};

export default SilaMenuEngineeringApp;
