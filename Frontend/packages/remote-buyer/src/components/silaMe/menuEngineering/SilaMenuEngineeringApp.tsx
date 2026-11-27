import React, { useEffect, useMemo, useState } from "react";
import { Loader, PageHeader, toastService } from "@vosox/shared-ui";
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
import MeStudioWorkspace from "./MeStudioWorkspace";
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

const TITLES: Record<ViewKey, string> = {
  silaMenuStudio: "ME Studio",
  silaMenuDashboard: "ME Studio",
  silaMenuItems: "Menu Items",
  silaMenuPlanning: "Menu Planning",
  silaMenuPortions: "Portion Planning",
  silaMenuDemand: "Demand Planning",
  silaMenuPosSales: "POS Sales",
  silaMenuPosMapping: "POS Mapping",
  silaMenuPerformance: "Menu Performance & Matrix",
  silaMenuConsumption: "Consumption Analysis",
  silaMenuCost: "Cost & Margin Analysis",
  silaMenuRecommendations: "Purchase Recommendations",
  silaMenuIntelligence: "AI Menu Intelligence",
  silaMenuWhatIf: "What-If Simulation",
  silaMenuSetup: "Menu Engineering Setup",
};

const QUICK_TABS: { key: ViewKey; label: string }[] = [
  { key: "silaMenuStudio", label: "ME Studio" },
  { key: "silaMenuItems", label: "Menu Items" },
  { key: "silaMenuPlanning", label: "Menu Planning" },
  { key: "silaMenuPortions", label: "Portion Planning" },
  { key: "silaMenuDemand", label: "Demand Planning" },
  { key: "silaMenuPosSales", label: "POS Sales" },
  { key: "silaMenuPosMapping", label: "POS Mapping" },
  { key: "silaMenuPerformance", label: "Performance" },
  { key: "silaMenuConsumption", label: "Consumption" },
  { key: "silaMenuCost", label: "Cost & Margin" },
  { key: "silaMenuRecommendations", label: "Purchase Recs" },
  { key: "silaMenuIntelligence", label: "AI Intelligence" },
  { key: "silaMenuWhatIf", label: "What-If" },
  { key: "silaMenuSetup", label: "Setup" },
];

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

const SilaMenuEngineeringApp: React.FC<Props> = ({ navKey, permissions, canManage, canRequestPurchase, onNavigate }) => {
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
    Promise.all([
      getMePermissions().catch(() => null),
      getMenuEngineering("dashboard", {}).catch(() => null),
      getMeAiStatus(propertyId || undefined).catch(() => null),
      getMeProactiveAlerts(propertyId || undefined).catch(() => []),
    ])
      .then(([p, env, aiSt, alerts]) => {
        if (!active) return;
        if (p) setApiPerms(p);
        if (env?.lookups) setLookups(env.lookups);
        if (aiSt) {
          setAiStatus(aiSt);
          setOperatingMode(aiSt.operatingMode || "AI_ASSISTED");
        }
        if (Array.isArray(alerts)) setProactiveAlerts(alerts);
      })
      .finally(() => {
        if (active) setLoadingPerms(false);
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

  const isStudio = navKey === "silaMenuStudio" || navKey === "silaMenuDashboard";

  if (loadingPerms) return <Loader />;

  if (!effectivePerms.menuEngineeringEnabled && navKey !== "silaMenuSetup") {
    return (
      <div className="sila-me sme-page">
        <PageHeader
          className="pud-page-header"
          title="Menu Engineering"
          description="Menu Engineering is currently disabled by tenant feature flag."
        />
        <div className="sme-panel">
          <p>Procurement V1 and existing culinary workflows remain completely unaffected. Enable Menu Engineering in Setup if authorized.</p>
          {effectivePerms.manageMenuSetup && (
            <button type="button" className="sila-btn sila-btn--primary" onClick={() => onNavigate("silaMenuSetup")}>
              Open Setup
            </button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="sila-me sme-page">
      <PageHeader
        className="pud-page-header"
        title={TITLES[navKey] ?? "Menu Engineering"}
        description="Consolidated Menu Engineering workspace: commercial design, recipe-linked costing, demand & portion intelligence, POS analytics, simulation, and controlled publishing."
      />

      {/* Quick module switcher & Global filter bar */}
      <div className="sme-panel" style={{ padding: "0.7rem 0.9rem" }}>
        <div className="sme-tabs" style={{ marginBottom: "0.6rem" }}>
          {QUICK_TABS.map((t) => {
            const active = t.key === navKey || (t.key === "silaMenuStudio" && navKey === "silaMenuDashboard");
            return (
              <button
                key={t.key}
                type="button"
                className={`sme-tab ${active ? "sme-tab--active" : ""}`}
                onClick={() => {
                  setShowReports(false);
                  onNavigate(t.key);
                }}
              >
                {t.label}
              </button>
            );
          })}
          <button
            type="button"
            className={`sme-tab ${showReports ? "sme-tab--active" : ""}`}
            onClick={() => setShowReports((s) => !s)}
          >
            Reports (22)
          </button>
        </div>

        <div className="sme-filters">
          <div className="sila-field">
            <label className="sila-label">From</label>
            <input className="sila-input" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
          </div>
          <div className="sila-field">
            <label className="sila-label">To</label>
            <input className="sila-input" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
          </div>
          <div className="sila-field">
            <label className="sila-label">Property</label>
            <select className="sila-input" value={propertyId} onChange={(e) => setPropertyId(e.target.value)}>
              <option value="">All properties</option>
              {(lookups?.properties ?? []).map((row) => (
                <option key={row.id} value={row.id}>{row.name}</option>
              ))}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label">Outlet</label>
            <select className="sila-input" value={outletId} onChange={(e) => setOutletId(e.target.value)}>
              <option value="">All outlets</option>
              {(lookups?.outlets ?? []).map((row) => (
                <option key={row.id} value={row.id}>{row.name}</option>
              ))}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label">Meal Period</label>
            <select className="sila-input" value={mealPeriod} onChange={(e) => setMealPeriod(e.target.value)}>
              <option value="">All meal periods</option>
              {(lookups?.mealPeriods ?? ["Breakfast", "Lunch", "Dinner", "Brunch", "All-Day"]).map((row) => (
                <option key={row} value={row}>{row}</option>
              ))}
            </select>
          </div>
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={() => {
              onNavigate("silaRecipes");
              toastService.info("Opened Recipe Studio (Culinary Master)");
            }}
          >
            Recipe Studio ↗
          </button>
        </div>

        <AiOperatingModeSwitch
          mode={operatingMode}
          status={aiStatus}
          onChangeMode={setOperatingMode}
        />
      </div>

      {proactiveAlerts.length > 0 && (
        <div className="sme-ai-proactive-strip">
          <div>
            <strong>{proactiveAlerts.length} Proactive Intelligence Alert{proactiveAlerts.length > 1 ? "s" : ""}:</strong>{" "}
            {proactiveAlerts[0].title} — {proactiveAlerts[0].summary}
          </div>
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate("silaMenuIntelligence")}>
            Open AI Cockpit
          </button>
        </div>
      )}

      <div className="sme-shell-with-ai">
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
            />
          ) : (
            <MeOperationalViews
              navKey={navKey}
              filter={filter}
              lookups={lookups}
              permissions={effectivePerms}
              operatingMode={operatingMode}
              onOpenInStudio={(planId) => {
                if (planId) setActiveMenuPlanId(planId);
                onNavigate("silaMenuStudio");
              }}
              onNavigate={onNavigate}
            />
          )}
        </div>

        <AiContextualAssistantPanel
          screenKey={navKey}
          operatingMode={operatingMode}
          propertyId={filter.propertyId}
          outletLocationId={filter.outletLocationId}
          menuPlanId={activeMenuPlanId}
          menuItemId={activeMenuItemId}
          onTriggerAction={(target) => {
            if (target.includes("purchase")) onNavigate("silaMenuRecommendations");
            else if (target.includes("simulate")) onNavigate("silaMenuWhatIf");
            else onNavigate("silaMenuIntelligence");
          }}
        />
      </div>
    </div>
  );
};

export default SilaMenuEngineeringApp;
