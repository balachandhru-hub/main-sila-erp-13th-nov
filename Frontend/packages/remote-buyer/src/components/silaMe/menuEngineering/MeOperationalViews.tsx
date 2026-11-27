import React, { useCallback, useEffect, useRef, useState } from "react";
import { LayoutGrid, Table2 } from "lucide-react";
import { Loader, toastService } from "@vosox/shared-ui";
import {
  AIRecommendationCard,
  MenuItemCard,
  RecipeGrid,
  ScenarioComparison,
  StudioChartContainer,
  StudioKPIBar,
  StudioKPICard,
} from "../studio";
import {
  bulkImportMeMenuItems,
  bulkPriceMeMenuItems,
  commitMeDataMigration,
  copyMeMenuItem,
  createMeDraftPurchaseRec,
  exportMeReport,
  getMeAiIntelligence,
  getMeConsumption,
  getMeCostMargin,
  getMeDataMigrationAudits,
  getMeDemand,
  getMeMenuItems,
  getMeMenuItemVersions,
  getMeMenuPlans,
  getMeMigrationTemplates,
  getMePerformanceMatrix,
  getMePortions,
  getMePosMapping,
  getMePosSales,
  getMePurchasePlan,
  getMeSetup,
  previewMeDataMigration,
  recordMePortionSample,
  runMeSimulation,
  saveMeDemandOverride,
  saveMeMenuItem,
  saveMePosMapping,
  saveMeSetup,
  transitionMeMenuPlan,
  type AiMenuIntelligenceDto,
  type ConsumptionVarianceDetailDto,
  type CostMarginAnalysisDto,
  type CustomerDataImportPreviewResultDto,
  type CustomerDataMigrationAuditDto,
  type CustomerDataTemplateDto,
  type DemandForecastBoard,
  type MenuEngineeringFeatureFlagsDto,
  type MenuEngineeringSetupDto,
  type MenuItemEnhanced,
  type MenuPerformanceMatrixDto,
  type MenuPlanEnhanced,
  type MenuPurchasePlanDto,
  type MeStudioPermissionsDto,
  type PortionPlanRow,
  type PosMappingWorkbenchDto,
  type PosSalesExtendedDto,
  type WhatIfSimulationResult,
} from "../../../api/silaMe/menuEngineeringStudioApi";
import { saveMenuEngineering, type MenuFilter, type MenuLookups } from "../../../api/silaMe/silaMenuEngineeringApi";
import type { AiOperatingMode } from "../../../api/silaMe/menuEngineeringAiApi";
import {
  AiCockpitFullPanel,
  ConsumptionRootCauseAiPanel,
  CostShockAndPricingAiPanel,
  DemandBacktestAiPanel,
  MenuItemAiAssistantPanel,
  MenuPlanningAiPanel,
  NlWhatIfAiPanel,
  PortionOptimizationAiPanel,
  PosMappingAiQueuePanel,
  PosSalesAiPanel,
  SetupWizardAiPanel,
} from "./ai/MeScreenAiPanels";
import { AiControlTowerPanel } from "./ai/AiControlTowerPanel";

interface Props {
  navKey: string;
  filter: MenuFilter;
  lookups?: MenuLookups;
  permissions: MeStudioPermissionsDto;
  operatingMode?: AiOperatingMode;
  externalCreateSignal?: number;
  onOpenInStudio?: (planId?: string) => void;
  onNavigate: (key: string) => void;
}

const DEFAULT_FEATURE_FLAGS: MenuEngineeringFeatureFlagsDto = {
  menuEngineering: true,
  meStudio: true,
  aiMenuIntelligence: true,
  predictiveForecasting: true,
  mathematicalOptimization: true,
  culinaryDigitalTwin: true,
  autonomousAgents: true,
  silaVoice: true,
  posIntegration: true,
  pmsIntegration: true,
  enterpriseNotifications: true,
  mobileCulinaryOperations: true,
};

const FEATURE_FLAG_LABELS: { key: keyof MenuEngineeringFeatureFlagsDto; label: string }[] = [
  { key: "menuEngineering", label: "Menu Engineering Core" },
  { key: "meStudio", label: "ME Studio Workspace" },
  { key: "aiMenuIntelligence", label: "AI Menu Intelligence" },
  { key: "predictiveForecasting", label: "Predictive Forecasting" },
  { key: "mathematicalOptimization", label: "Mathematical Optimization" },
  { key: "culinaryDigitalTwin", label: "Culinary Digital Twin" },
  { key: "autonomousAgents", label: "Autonomous Agents" },
  { key: "silaVoice", label: "SILA Voice" },
  { key: "posIntegration", label: "POS Integration" },
  { key: "pmsIntegration", label: "PMS Integration" },
  { key: "enterpriseNotifications", label: "Enterprise Notifications" },
  { key: "mobileCulinaryOperations", label: "Mobile Culinary Operations" },
];

const num = (v?: number | null, d = 2) => (v == null ? "—" : v.toFixed(d));
const pct = (v?: number | null) => (v == null ? "—" : `${v.toFixed(1)}%`);

const MeOperationalViews: React.FC<Props> = ({
  navKey,
  filter,
  lookups,
  permissions,
  operatingMode = "AI_ASSISTED",
  externalCreateSignal,
  onOpenInStudio,
  onNavigate,
}) => {
  const [itemsViewMode, setItemsViewMode] = useState<"cards" | "table">("cards");
  const [fullscreenView, setFullscreenView] = useState(false);
  const itemFormRef = useRef<HTMLDivElement | null>(null);
  const [loading, setLoading] = useState(true);
  const [items, setItems] = useState<MenuItemEnhanced[]>([]);
  const [plans, setPlans] = useState<MenuPlanEnhanced[]>([]);
  const [portions, setPortions] = useState<PortionPlanRow[]>([]);
  const [demand, setDemand] = useState<DemandForecastBoard | null>(null);
  const [posSales, setPosSales] = useState<PosSalesExtendedDto | null>(null);
  const [posMapping, setPosMapping] = useState<PosMappingWorkbenchDto | null>(null);
  const [matrix, setMatrix] = useState<MenuPerformanceMatrixDto | null>(null);
  const [consumption, setConsumption] = useState<ConsumptionVarianceDetailDto | null>(null);
  const [costMargin, setCostMargin] = useState<CostMarginAnalysisDto | null>(null);
  const [purchasePlan, setPurchasePlan] = useState<MenuPurchasePlanDto | null>(null);
  const [aiIntel, setAiIntel] = useState<AiMenuIntelligenceDto | null>(null);
  const [setup, setSetup] = useState<MenuEngineeringSetupDto | null>(null);

  // Local editor states
  const [editItem, setEditItem] = useState<Partial<MenuItemEnhanced>>({ menuItemCode: "", name: "", category: "Mains", cuisine: "International", portionUom: "PORTION", defaultPortionQty: 1, sellingPrice: 20, taxRatePercent: 5, status: "ACTIVE", isAvailable: true });
  const [selectedItemIds, setSelectedItemIds] = useState<string[]>([]);
  const [bulkPct, setBulkPct] = useState("5");
  const [bulkCsv, setBulkCsv] = useState("Code,Name,Category,Cuisine,SellingPrice,PortionUom\nME-BULK-1,Grilled Lamb Rack,Mains,Mediterranean,38.00,PORTION");
  const [versions, setVersions] = useState<{ id: string; versionNumber: number; status: string; sellingPrice?: number; changeSummary?: string; actionAtUtc: string }[]>([]);
  const [plannerView, setPlannerView] = useState<"LIST" | "CALENDAR">("CALENDAR");
  const [sampleInputs, setSampleInputs] = useState<Record<string, string>>({});
  const [overrideDate, setOverrideDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [overrideQty, setOverrideQty] = useState("220");
  const [overrideReason, setOverrideReason] = useState("Confirmed hotel group dinner booking");
  const [posCsv, setPosCsv] = useState("PosCode,Quantity,Amount,BusinessDate\nGRILLED_CHICKEN_LARGE,24,720.00," + new Date().toISOString().slice(0, 10));
  const [costBasis, setCostBasis] = useState<"FOOD_COST_ONLY" | "VARIABLE_COST">("FOOD_COST_ONLY");
  const [quadrantFilter, setQuadrantFilter] = useState<"" | "STAR" | "PLOW" | "PUZZLE" | "DOG">("");
  const [posRowFilter, setPosRowFilter] = useState<"all" | "unmapped">("all");
  const [consumptionFilter, setConsumptionFilter] = useState<"all" | "yield" | "waste" | "unexplained">("all");
  const scrollToId = (id: string) =>
    window.setTimeout(() => document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" }), 50);
  const [popThresh, setPopThresh] = useState("0.70");
  const [simForm, setSimForm] = useState({ scenarioName: "What-If Analysis", expectedCovers: "300", ingredientPriceChangePercent: "10", sellingPriceChangePercent: "0", portionSizeChangePercent: "0", discountPercent: "0" });
  const [simRes, setSimRes] = useState<WhatIfSimulationResult | null>(null);
  const [mappingDraft, setMappingDraft] = useState<Record<string, { menuItemId: string; mappingType: string; portionMultiplier: string; comboParentCode: string; modifierCode: string }>>({});
  const [itemSearch, setItemSearch] = useState("");
  const [itemCatFilter, setItemCatFilter] = useState("");
  const [migTemplates, setMigTemplates] = useState<CustomerDataTemplateDto[]>([]);
  const [migEntityType, setMigEntityType] = useState("MENU_ITEMS");
  const [migCsv, setMigCsv] = useState("");
  const [migAllowOverwrite, setMigAllowOverwrite] = useState(false);
  const [migPreview, setMigPreview] = useState<CustomerDataImportPreviewResultDto | null>(null);
  const [migAudits, setMigAudits] = useState<CustomerDataMigrationAuditDto[]>([]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      switch (navKey) {
        case "silaMenuItems":
          setItems(await getMeMenuItems(filter));
          break;
        case "silaMenuPlanning":
          setPlans(await getMeMenuPlans(filter));
          break;
        case "silaMenuPortions":
          setPortions(await getMePortions(filter.menuPlanId, filter.propertyId));
          break;
        case "silaMenuDemand":
          setDemand(await getMeDemand(filter));
          break;
        case "silaMenuPosSales":
          setPosSales(await getMePosSales(filter));
          break;
        case "silaMenuPosMapping":
          setPosMapping(await getMePosMapping(filter));
          setItems(await getMeMenuItems({}));
          break;
        case "silaMenuPerformance":
          setMatrix(await getMePerformanceMatrix(filter, costBasis, Number(popThresh) || 0.7));
          break;
        case "silaMenuConsumption":
          setConsumption(await getMeConsumption(filter));
          break;
        case "silaMenuCost":
          setCostMargin(await getMeCostMargin(filter));
          break;
        case "silaMenuRecommendations":
          setPurchasePlan(await getMePurchasePlan(filter));
          break;
        case "silaMenuIntelligence":
          setAiIntel(await getMeAiIntelligence(filter.menuPlanId, filter.propertyId));
          break;
        case "silaMenuWhatIf":
          setPlans(await getMeMenuPlans({}));
          break;
        case "silaMenuSetup": {
          const [s, tpls, auds] = await Promise.all([
            getMeSetup(filter.propertyId),
            getMeMigrationTemplates().catch(() => []),
            getMeDataMigrationAudits().catch(() => []),
          ]);
          setSetup(s);
          setMigTemplates(tpls);
          setMigAudits(auds);
          if (tpls.length > 0 && !migCsv) setMigCsv(tpls[0].sampleCsv);
          break;
        }
      }
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Could not load view.");
    } finally {
      setLoading(false);
    }
  }, [navKey, filter, costBasis, popThresh]);

  useEffect(() => {
    void load();
  }, [load]);

  useEffect(() => {
    if (!externalCreateSignal) return;
    setEditItem({ menuItemCode: "", name: "", category: "Mains", cuisine: "International", portionUom: "PORTION", defaultPortionQty: 1, sellingPrice: 20, taxRatePercent: 5, status: "ACTIVE", isAvailable: true });
    itemFormRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [externalCreateSignal]);

  useEffect(() => {
    setFullscreenView(false);
  }, [navKey]);

  useEffect(() => {
    if (!fullscreenView) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") setFullscreenView(false);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [fullscreenView]);

  if (loading) return <Loader />;

  const viewCost = permissions.viewMenuCost;
  const viewMargin = permissions.viewMenuMargin;

  // 1. MENU ITEMS
  if (navKey === "silaMenuItems") {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <MenuItemAiAssistantPanel
            dishName={editItem.name || ""}
            sellingPrice={editItem.sellingPrice ?? undefined}
            onApplySuggestion={(patch) =>
              setEditItem((prev) => ({
                ...prev,
                category: patch.category || prev.category,
                cuisine: patch.cuisine || prev.cuisine,
                description: patch.description || prev.description,
                sellingPrice: patch.sellingPrice ?? prev.sellingPrice,
                allergenNotes: patch.allergenNotes || prev.allergenNotes,
              }))
            }
          />
        )}
        {permissions.manageMenuItems && (
          <div className="sme-panel" ref={itemFormRef}>
            <div className="sme-panel__header">
              <h2 className="sme-panel__title">Menu Item Commercial Master</h2>
              <div style={{ display: "flex", gap: "0.4rem" }}>
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary"
                  onClick={async () => {
                    const exp = await exportMeReport("MENU_ITEM_MASTER", "EXCEL", filter);
                    const blob = new Blob([exp.textContent ?? ""], { type: exp.mimeType });
                    const url = URL.createObjectURL(blob);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = exp.fileName;
                    a.click();
                    URL.revokeObjectURL(url);
                  }}
                >
                  Export Excel
                </button>
              </div>
            </div>
            <div className="sme-form">
              <label className="sila-field"><span>Code</span><input className="sila-input" value={editItem.menuItemCode ?? ""} onChange={(e) => setEditItem({ ...editItem, menuItemCode: e.target.value })} /></label>
              <label className="sila-field"><span>Name</span><input className="sila-input" value={editItem.name ?? ""} onChange={(e) => setEditItem({ ...editItem, name: e.target.value })} /></label>
              <label className="sila-field"><span>Category</span><input className="sila-input" value={editItem.category ?? ""} onChange={(e) => setEditItem({ ...editItem, category: e.target.value })} /></label>
              <label className="sila-field"><span>Cuisine</span><input className="sila-input" value={editItem.cuisine ?? ""} onChange={(e) => setEditItem({ ...editItem, cuisine: e.target.value })} /></label>
              <label className="sila-field">
                <span>Linked Recipe</span>
                <select className="sila-input" value={editItem.linkedRecipeId ?? ""} onChange={(e) => setEditItem({ ...editItem, linkedRecipeId: e.target.value || null })}>
                  <option value="">— Select Approved Recipe —</option>
                  {(lookups?.recipes ?? []).map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
                </select>
              </label>
              <label className="sila-field"><span>Default Portion</span><input className="sila-input" type="number" step="0.1" value={editItem.defaultPortionQty ?? 1} onChange={(e) => setEditItem({ ...editItem, defaultPortionQty: Number(e.target.value) || 1 })} /></label>
              <label className="sila-field"><span>Selling Price</span><input className="sila-input" type="number" step="0.5" value={editItem.sellingPrice ?? 0} onChange={(e) => setEditItem({ ...editItem, sellingPrice: Number(e.target.value) || 0 })} /></label>
              <label className="sila-field"><span>Tax %</span><input className="sila-input" type="number" step="0.5" value={editItem.taxRatePercent ?? 0} onChange={(e) => setEditItem({ ...editItem, taxRatePercent: Number(e.target.value) || 0 })} /></label>
              <label className="sila-field"><span>Dietary</span><input className="sila-input" value={editItem.dietaryTags ?? ""} onChange={(e) => setEditItem({ ...editItem, dietaryTags: e.target.value })} /></label>
              <label className="sila-field"><span>Allergens</span><input className="sila-input" value={editItem.allergenNotes ?? ""} onChange={(e) => setEditItem({ ...editItem, allergenNotes: e.target.value })} /></label>
            </div>
            <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                onClick={async () => {
                  await saveMeMenuItem(editItem);
                  toastService.success("Menu item saved.");
                  await load();
                }}
              >
                Save Menu Item
              </button>
              <input className="sila-input" style={{ width: "6rem" }} value={bulkPct} onChange={(e) => setBulkPct(e.target.value)} />
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={selectedItemIds.length === 0}
                onClick={async () => {
                  const n = await bulkPriceMeMenuItems({ menuItemIds: selectedItemIds, percentageChange: Number(bulkPct) || 0, reason: "Bulk Menu Item price update" });
                  toastService.success(`Updated price on ${n} menu item(s).`);
                  await load();
                }}
              >
                Bulk Price Δ % ({selectedItemIds.length})
              </button>
            </div>
            <div className="sme-row">
              <textarea className="sila-input" rows={2} style={{ flex: 1 }} value={bulkCsv} onChange={(e) => setBulkCsv(e.target.value)} />
              <button type="button" className="sila-btn sila-btn--secondary" onClick={async () => { const c = await bulkImportMeMenuItems(bulkCsv); toastService.success(`Imported ${c} items.`); await load(); }}>
                Bulk Upload CSV
              </button>
            </div>
          </div>
        )}

        <div className="sme-panel">
          <div className="sme-row" style={{ marginBottom: "0.75rem", flexWrap: "wrap", alignItems: "center" }}>
            <input
              className="sila-input"
              style={{ flex: 1, minWidth: "14rem" }}
              placeholder="Search menu items by code, name, cuisine…"
              value={itemSearch}
              onChange={(e) => setItemSearch(e.target.value)}
            />
            <select className="sila-input" value={itemCatFilter} onChange={(e) => setItemCatFilter(e.target.value)}>
              <option value="">All Categories</option>
              {Array.from(new Set(items.map((x) => x.category).filter(Boolean))).map((c) => (
                <option key={c} value={c!}>{c}</option>
              ))}
            </select>
            <div style={{ display: "inline-flex", gap: "0.3rem" }}>
              <button
                type="button"
                className={`sila-studio-btn sila-studio-btn--sm ${itemsViewMode === "cards" ? "sila-studio-btn--primary" : "sila-studio-btn--secondary"}`}
                onClick={() => setItemsViewMode("cards")}
              >
                <LayoutGrid size={13} aria-hidden="true" />
                <span>Cards</span>
              </button>
              <button
                type="button"
                className={`sila-studio-btn sila-studio-btn--sm ${itemsViewMode === "table" ? "sila-studio-btn--primary" : "sila-studio-btn--secondary"}`}
                onClick={() => setItemsViewMode("table")}
              >
                <Table2 size={13} aria-hidden="true" />
                <span>Table</span>
              </button>
            </div>
          </div>

          {itemsViewMode === "cards" ? (
            <RecipeGrid ariaLabel="Menu items catalogue">
              {items
                .filter((it) => {
                  const q = itemSearch.trim().toLowerCase();
                  if (q && !it.name.toLowerCase().includes(q) && !it.menuItemCode.toLowerCase().includes(q) && !(it.cuisine ?? "").toLowerCase().includes(q)) return false;
                  if (itemCatFilter && it.category !== itemCatFilter) return false;
                  return true;
                })
                .map((it) => (
                  <MenuItemCard
                    key={it.id}
                    name={it.name}
                    code={it.menuItemCode}
                    linkedRecipeName={it.recipeName}
                    category={it.category}
                    cuisine={it.cuisine}
                    sellingPriceText={num(it.netSellingPrice ?? it.sellingPrice)}
                    foodCostText={viewCost ? num(it.costPerPortion) : undefined}
                    foodCostPctText={viewCost ? pct(it.foodCostPercent) : undefined}
                    contributionMarginText={viewMargin ? num(it.contributionMargin) : undefined}
                    availabilityLabel={`${it.status} · v${it.versionNumber}`}
                    classification={it.matrixClassification}
                    imageUrl={it.imagePath}
                    actions={
                      <>
                        <button type="button" className="sila-studio-btn sila-studio-btn--sm" onClick={() => { setEditItem(it); itemFormRef.current?.scrollIntoView({ behavior: "smooth" }); }}>Edit</button>
                        {permissions.manageMenuItems && (
                          <button type="button" className="sila-studio-btn sila-studio-btn--sm" onClick={async () => { await copyMeMenuItem(it.id!); toastService.success("Copied item."); await load(); }}>Copy</button>
                        )}
                        <button type="button" className="sila-studio-btn sila-studio-btn--sm" onClick={async () => setVersions(await getMeMenuItemVersions(it.id!))}>History</button>
                      </>
                    }
                  />
                ))}
            </RecipeGrid>
          ) : (
            <div className="sila-studio-table-wrap">
              <table className="sila-table sila-tabular">
                <thead>
                  <tr><th>Select</th><th>Code</th><th>Dish</th><th>Category</th><th>Recipe</th><th>Net Price</th>{viewCost && <th>Cost</th>}{viewCost && <th>FC %</th>}{viewMargin && <th>Margin</th>}<th>Status</th><th>Actions</th></tr>
                </thead>
                <tbody>
                  {items
                    .filter((it) => {
                      const q = itemSearch.trim().toLowerCase();
                      if (q && !it.name.toLowerCase().includes(q) && !it.menuItemCode.toLowerCase().includes(q) && !(it.cuisine ?? "").toLowerCase().includes(q)) return false;
                      if (itemCatFilter && it.category !== itemCatFilter) return false;
                      return true;
                    })
                    .map((it) => (
                    <tr key={it.id}>
                      <td><input type="checkbox" checked={selectedItemIds.includes(it.id!)} onChange={(e) => setSelectedItemIds((p) => (e.target.checked ? [...p, it.id!] : p.filter((x) => x !== it.id)))} /></td>
                      <td>{it.menuItemCode}</td>
                      <td><strong>{it.name}</strong><div className="sme-muted">{it.cuisine} • {it.dietaryTags || "Standard"}</div></td>
                      <td>{it.category}</td>
                      <td>{it.recipeName || "Unlinked"}</td>
                      <td>{num(it.netSellingPrice ?? it.sellingPrice)}</td>
                      {viewCost && <td>{num(it.costPerPortion)}</td>}
                      {viewCost && <td>{pct(it.foodCostPercent)}</td>}
                      {viewMargin && <td>{num(it.contributionMargin)}</td>}
                      <td><span className="sme-chip">{it.status} • {it.approvalStatus} v{it.versionNumber}</span></td>
                      <td style={{ display: "flex", gap: "0.3rem", flexWrap: "wrap" }}>
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setEditItem(it)}>Edit</button>
                        {permissions.manageMenuItems && (
                          <button type="button" className="sila-btn sila-btn--secondary" onClick={async () => { await copyMeMenuItem(it.id!); toastService.success("Copied item."); await load(); }}>Copy</button>
                        )}
                        {permissions.manageMenuItems && it.status !== "INACTIVE" && (
                          <button
                            type="button"
                            className="sila-btn sila-btn--secondary"
                            onClick={async () => {
                              await saveMeMenuItem({ ...it, status: "INACTIVE", isAvailable: false });
                              toastService.success(`Deactivated ${it.name}.`);
                              await load();
                            }}
                          >
                            Deactivate
                          </button>
                        )}
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={async () => setVersions(await getMeMenuItemVersions(it.id!))}>History</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {versions.length > 0 && (
            <div className="sme-kpi-box" style={{ marginTop: "0.5rem" }}>
              <strong>Version History</strong>
              {versions.map((v) => <div key={v.id} className="sme-muted">v{v.versionNumber} • {v.status} • Price {num(v.sellingPrice)} • {v.changeSummary}</div>)}
            </div>
          )}
        </div>
      </div>
    );
  }

  // 2. MENU PLANNING
  if (navKey === "silaMenuPlanning") {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <MenuPlanningAiPanel propertyId={filter.propertyId} />
        )}
        <div className="sme-panel">
        <div className="sme-panel__header">
          <h2 className="sme-panel__title">Menu Planning &amp; Calendar ({plans.length} Plans)</h2>
          <div style={{ display: "flex", gap: "0.4rem" }}>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setPlannerView(plannerView === "CALENDAR" ? "LIST" : "CALENDAR")}>
              {plannerView === "CALENDAR" ? "List View" : "Calendar View"}
            </button>
            <button type="button" className="sila-btn sila-btn--primary" onClick={() => (onOpenInStudio ? onOpenInStudio(plans[0]?.id) : onNavigate("silaMenuStudio"))}>Open in ME Studio</button>
          </div>
        </div>
        {plannerView === "CALENDAR" ? (
          <div className="sme-calendar">
            {plans.map((p) => (
              <div key={p.id} className="sme-cal-day">
                <div className="sme-muted">{p.businessDate.slice(0, 10)} • {p.mealPeriod}</div>
                <strong>{p.name}</strong>
                <div className="sme-muted">{p.serviceFormat} • {p.expectedCovers ?? p.expectedGuests} covers</div>
                <span className="sme-chip">{p.status} v{p.versionNumber}</span>
                <div style={{ display: "flex", gap: "0.3rem", marginTop: "0.35rem", flexWrap: "wrap" }}>
                  <button type="button" className="sila-btn sila-btn--secondary" onClick={() => (onOpenInStudio ? onOpenInStudio(p.id) : onNavigate("silaMenuStudio"))}>
                    Open in Studio
                  </button>
                  {p.status === "APPROVED" && permissions.publishMenu && (
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={async () => { await transitionMeMenuPlan(p.id, "PUBLISHED", "Published from Menu Planner", true); toastService.success("Published & authorized production handoff."); await load(); }}>
                      Publish
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        ) : (
          <table className="sila-table">
            <thead><tr><th>Plan</th><th>Type</th><th>Meal Period</th><th>Date</th><th>Covers</th><th>Dishes</th><th>Status</th><th>Action</th></tr></thead>
            <tbody>
              {plans.map((p) => (
                <tr key={p.id}>
                  <td><strong>{p.name}</strong></td>
                  <td>{p.menuType}</td>
                  <td>{p.mealPeriod}</td>
                  <td>{p.businessDate.slice(0, 10)}</td>
                  <td>{p.expectedCovers ?? p.expectedGuests}</td>
                  <td>{p.lines.length}</td>
                  <td>{p.status} v{p.versionNumber}</td>
                  <td>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={() => (onOpenInStudio ? onOpenInStudio(p.id) : onNavigate("silaMenuStudio"))}>
                      Open in Studio
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        </div>
      </div>
    );
  }

  // 3. PORTION PLANNING
  if (navKey === "silaMenuPortions") {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <PortionOptimizationAiPanel propertyId={filter.propertyId} menuPlanId={filter.menuPlanId} />
        )}
        <div className="sme-panel">
        <div className="sme-panel__header">
          <h2 className="sme-panel__title">Portion Planning &amp; Buffet Sampling</h2>
          <span className="sme-muted">Expected Portions = Covers × Participation Rate × Servings/Guest</span>
        </div>
        <table className="sila-table">
          <thead>
            <tr><th>Plan / Dish</th><th>Format</th><th>Covers</th><th>Part.</th><th>Planned Portion</th><th>Exp. Portions</th><th>Usable Output</th><th>Yield %</th><th>Sampled</th><th>Variance</th><th>Record Sample</th></tr>
          </thead>
          <tbody>
            {portions.map((r) => (
              <tr key={r.menuPlanLineId}>
                <td><strong>{r.menuItemName}</strong><div className="sme-muted">{r.planName}</div></td>
                <td>{r.serviceFormat}</td>
                <td>{r.expectedCovers}</td>
                <td>{pct(r.participationRate * 100)}</td>
                <td>{r.plannedPortion} {r.portionUom}</td>
                <td>{r.expectedPortions}</td>
                <td>{r.requiredUsableOutput} {r.portionUom}</td>
                <td>{pct(r.recipeYieldPercent)}</td>
                <td>{r.sampledActualPortion ?? "—"}</td>
                <td><span className={`sme-chip ${r.varianceStatus === "ON_TARGET" ? "sme-chip--ok" : "sme-chip--warn"}`}>{r.varianceStatus}</span></td>
                <td style={{ display: "flex", gap: "0.3rem" }}>
                  <input className="sila-input" style={{ width: "5rem" }} type="number" step="0.1" placeholder={String(r.plannedPortion)} value={sampleInputs[r.menuPlanLineId] ?? ""} onChange={(e) => setSampleInputs({ ...sampleInputs, [r.menuPlanLineId]: e.target.value })} />
                  {permissions.managePortionPlans && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--secondary"
                      onClick={async () => {
                        const val = Number(sampleInputs[r.menuPlanLineId]);
                        if (!val) return;
                        await recordMePortionSample(r.menuPlanLineId, val, "Kitchen portion sampling");
                        toastService.success("Recorded portion sample.");
                        await load();
                      }}
                    >
                      Save
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        </div>
      </div>
    );
  }

  // 4. DEMAND PLANNING
  if (navKey === "silaMenuDemand" && demand) {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <DemandBacktestAiPanel propertyId={filter.propertyId} outletLocationId={filter.outletLocationId} />
        )}
        {demand.coldStartLimited && (
          <div className="sme-panel" style={{ background: "#fffbeb", borderColor: "#fde68a" }}>
            <strong>Cold-Start Advisory:</strong> {demand.coldStartMessage}
          </div>
        )}
        {permissions.manageDemandPlans && (
          <div className="sme-panel">
            <h3 className="sme-panel__title">Manual Forecast Override (Audited)</h3>
            <div className="sme-form">
              <label className="sila-field"><span>Date</span><input className="sila-input" type="date" value={overrideDate} onChange={(e) => setOverrideDate(e.target.value)} /></label>
              <label className="sila-field"><span>Override Covers</span><input className="sila-input" type="number" value={overrideQty} onChange={(e) => setOverrideQty(e.target.value)} /></label>
              <label className="sila-field"><span>Audit Reason</span><input className="sila-input" value={overrideReason} onChange={(e) => setOverrideReason(e.target.value)} /></label>
            </div>
            <div>
              <button type="button" className="sila-btn sila-btn--primary" onClick={async () => { await saveMeDemandOverride({ businessDate: overrideDate, overrideQty: Number(overrideQty) || 100, reason: overrideReason }); toastService.success("Forecast override saved."); await load(); }}>
                Save Audited Override
              </button>
            </div>
          </div>
        )}
        <div className="sme-panel">
          <table className="sila-table">
            <thead>
              <tr><th>Date</th><th>Occ %</th><th>Historical</th><th>Baseline</th><th>AI Adj.</th><th>Confirmed (Deduped)</th><th>Recommended</th><th>Range</th><th>Override</th></tr>
            </thead>
            <tbody>
              {demand.rows.map((r) => (
                <tr key={r.businessDate}>
                  <td>{r.businessDate.slice(0, 10)}</td>
                  <td>{pct(r.occupancyPercent)}</td>
                  <td>{r.historicalActualDemand}</td>
                  <td>{r.baselineForecast}</td>
                  <td>{r.aiAdjustedForecast}</td>
                  <td>{r.confirmedDemandDeduped}</td>
                  <td><strong>{r.recommendedDemand}</strong></td>
                  <td>{r.lowerRange} – {r.upperRange}</td>
                  <td>{r.manualOverrideQty != null ? `${r.manualOverrideQty} (${r.overrideReason})` : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  // 5. POS SALES
  if (navKey === "silaMenuPosSales" && posSales) {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <PosSalesAiPanel propertyId={filter.propertyId} outletLocationId={filter.outletLocationId} />
        )}
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">POS Sales &amp; Synchronization</h2>
            <div style={{ display: "flex", gap: "0.4rem" }}>
              <button type="button" className="sila-btn sila-btn--secondary" onClick={async () => { await saveMenuEngineering({ action: "sync-pos" }); toastService.success("POS sync triggered."); await load(); }}>Sync POS</button>
            </div>
          </div>
          <StudioKPIBar ariaLabel="POS sales metrics">
            <StudioKPICard label="Qty Sold" value={posSales.qtySold} tone="blue" badge="Actual" drillLabel="Show all POS sales lines" selected={posRowFilter === "all"} onClick={() => { setPosRowFilter("all"); scrollToId("me-pos-table"); }} />
            <StudioKPICard label="Gross Sales" value={num(posSales.grossRevenue)} tone="sky" badge="Actual" drillLabel="Show all POS sales lines" onClick={() => { setPosRowFilter("all"); scrollToId("me-pos-table"); }} />
            <StudioKPICard label="Discounts / Tax" value={`${num(posSales.discountTotal)} / ${num(posSales.taxTotal)}`} tone="amber" badge="Actual" drillLabel="Show all POS sales lines" onClick={() => { setPosRowFilter("all"); scrollToId("me-pos-table"); }} />
            <StudioKPICard label="Net Revenue" value={num(posSales.netRevenue)} tone="green" badge="Actual" drillLabel="Show all POS sales lines" onClick={() => { setPosRowFilter("all"); scrollToId("me-pos-table"); }} />
            <StudioKPICard label="Unmapped Lines" value={posSales.unmappedCount} tone={posSales.unmappedCount > 0 ? "rose" : "green"} badge="Actual" drillLabel="Show POS lines without a mapped menu item" selected={posRowFilter === "unmapped"} onClick={() => { setPosRowFilter("unmapped"); scrollToId("me-pos-table"); }} />
          </StudioKPIBar>
          <div className="sme-row">
            <textarea className="sila-input" rows={2} style={{ flex: 1 }} value={posCsv} onChange={(e) => setPosCsv(e.target.value)} />
            <button type="button" className="sila-btn sila-btn--primary" onClick={async () => { await saveMenuEngineering({ action: "import-pos-csv", csvContent: posCsv }); toastService.success("POS CSV imported."); await load(); }}>
              Import POS File
            </button>
          </div>
        </div>
        <div className="sme-panel" id="me-pos-table">
          {posRowFilter === "unmapped" && (
            <div className="sila-studio-chips" style={{ marginBottom: 8 }} aria-label="Active POS filter">
              <span className="sila-studio-chip is-active">
                <span>Showing unmapped lines</span>
                <button type="button" aria-label="Clear POS filter" onClick={() => setPosRowFilter("all")}>×</button>
              </span>
            </div>
          )}
          <table className="sila-table">
            <thead><tr><th>Txn ID</th><th>Date</th><th>POS Code</th><th>Menu Item</th><th>Qty</th><th>Gross</th><th>Net Rev</th><th>Flags</th><th>Status</th></tr></thead>
            <tbody>
              {posSales.rows.filter((r) => posRowFilter === "all" || r.status === "UNMAPPED").map((r) => (
                <tr key={r.id}>
                  <td>{r.sourceTransactionId}</td>
                  <td>{r.businessDate.slice(0, 10)}</td>
                  <td><strong>{r.posCode}</strong></td>
                  <td>{r.menuItemName || "Unmapped"}</td>
                  <td>{r.quantity}</td>
                  <td>{num(r.grossSellingPrice)}</td>
                  <td>{num(r.netRevenue)}</td>
                  <td>{[r.isVoid && "VOID", r.isRefund && "REFUND", r.isComplimentary && "COMP", r.comboReference && `COMBO:${r.comboReference}`, r.modifierCode && `MOD:${r.modifierCode}`].filter(Boolean).join(", ") || "SALE"}</td>
                  <td><span className={`sme-chip ${r.status === "UNMAPPED" ? "sme-chip--warn" : "sme-chip--ok"}`}>{r.status}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  // 6. POS MAPPING
  if (navKey === "silaMenuPosMapping" && posMapping) {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <PosMappingAiQueuePanel propertyId={filter.propertyId} onApproved={() => void load()} />
        )}
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">POS Mapping Workbench (Mapped: {posMapping.mappedCount} • Unmapped: {posMapping.unmappedCount} • Combos: {posMapping.comboCount})</h2>
          </div>
          <div className="sme-muted">Supports 1-to-1, multi-POS, combo parent/component double-count prevention, modifier recipes, and UOM multipliers.</div>
          <table className="sila-table">
            <thead><tr><th>POS Code</th><th>Type</th><th>Multiplier</th><th>Combo Parent / Modifier</th><th>Target Menu Item</th><th>Status / Action</th></tr></thead>
            <tbody>
              {posMapping.mappings.map((m) => {
                const d = mappingDraft[m.posCode] ?? {
                  menuItemId: m.menuItemId || m.suggestedMenuItemId || "",
                  mappingType: m.mappingType || "ONE_TO_ONE",
                  portionMultiplier: String(m.portionMultiplier || 1),
                  comboParentCode: m.comboParentCode || "",
                  modifierCode: m.modifierCode || "",
                };
                return (
                  <tr key={m.posCode}>
                    <td><strong>{m.posCode}</strong><div className="sme-muted">{m.posDescription}</div></td>
                    <td>
                      <select className="sila-input" value={d.mappingType} onChange={(e) => setMappingDraft({ ...mappingDraft, [m.posCode]: { ...d, mappingType: e.target.value } })}>
                        <option value="ONE_TO_ONE">One-to-One</option>
                        <option value="MULTI_POS">Multi-POS</option>
                        <option value="COMBO">Combo Parent</option>
                        <option value="COMBO_COMPONENT">Combo Component</option>
                        <option value="MODIFIER">Modifier</option>
                        <option value="SIZE_VARIATION">Size Variation</option>
                      </select>
                    </td>
                    <td><input className="sila-input" style={{ width: "4.5rem" }} type="number" step="0.1" value={d.portionMultiplier} onChange={(e) => setMappingDraft({ ...mappingDraft, [m.posCode]: { ...d, portionMultiplier: e.target.value } })} /></td>
                    <td><input className="sila-input" placeholder="Combo/Mod…" value={d.comboParentCode || d.modifierCode} onChange={(e) => setMappingDraft({ ...mappingDraft, [m.posCode]: { ...d, comboParentCode: e.target.value } })} /></td>
                    <td>
                      <select className="sila-input" value={d.menuItemId} onChange={(e) => setMappingDraft({ ...mappingDraft, [m.posCode]: { ...d, menuItemId: e.target.value } })}>
                        <option value="">— Select Menu Item —</option>
                        {items.map((it) => <option key={it.id} value={it.id}>{it.name}</option>)}
                      </select>
                    </td>
                    <td>
                      {permissions.managePosMapping && (
                        <button
                          type="button"
                          className="sila-btn sila-btn--primary"
                          onClick={async () => {
                            if (!d.menuItemId) return toastService.error("Select a Menu Item first.");
                            await saveMePosMapping({
                              id: m.id === "00000000-0000-0000-0000-000000000000" ? undefined : m.id,
                              posCode: m.posCode,
                              menuItemId: d.menuItemId,
                              mappingType: d.mappingType,
                              portionMultiplier: Number(d.portionMultiplier) || 1,
                              comboParentCode: d.comboParentCode || undefined,
                              isComboComponent: d.mappingType === "COMBO_COMPONENT",
                              excludeComponentDoubleCount: d.mappingType === "COMBO_COMPONENT",
                              approveMapping: true,
                            });
                            toastService.success(`Approved mapping for ${m.posCode}.`);
                            await load();
                          }}
                        >
                          Approve Mapping
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  // 7. MENU PERFORMANCE & 4-QUADRANT MATRIX
  if (navKey === "silaMenuPerformance" && matrix) {
    return (
      <div className="sme-list">
        <StudioChartContainer
          title="Menu Performance & Engineering Matrix"
          subtitle="Stars, Plowhorses, Puzzles, and Dogs calculated from POS sales and recipe cost basis"
          fullScreen={fullscreenView}
          onToggleFullScreen={() => setFullscreenView((f) => !f)}
          actions={
            <div style={{ display: "flex", gap: "0.45rem", flexWrap: "wrap" }}>
              <select className="sila-input" value={costBasis} onChange={(e) => setCostBasis(e.target.value as "FOOD_COST_ONLY" | "VARIABLE_COST")}>
                <option value="FOOD_COST_ONLY">Food-Cost-Only Basis</option>
                <option value="VARIABLE_COST">Configured Variable-Cost Basis</option>
              </select>
              <input className="sila-input" style={{ width: "5.5rem" }} value={popThresh} onChange={(e) => setPopThresh(e.target.value)} title="Popularity threshold factor (default 0.70)" />
            </div>
          }
        >
          <div className="sme-quadrant-grid">
            {(
              [
                ["STAR", "star", `STARS (${matrix.stars})`, "High Popularity & High CM"],
                ["PLOW", "plow", `PLOWHORSES (${matrix.plowhorses})`, "High Popularity & Low CM"],
                ["PUZZLE", "puzzle", `PUZZLES (${matrix.puzzles})`, "Low Popularity & High CM"],
                ["DOG", "dog", `DOGS (${matrix.dogs})`, "Low Popularity & Low CM"],
              ] as const
            ).map(([key, cls, title, sub]) => (
              <button
                key={key}
                type="button"
                className={`sme-quadrant sme-quadrant--${cls} sme-quadrant--button${quadrantFilter === key ? " is-selected" : ""}`}
                aria-pressed={quadrantFilter === key}
                title={`Show ${title.split(" (")[0].toLowerCase()} only`}
                data-testid={`me-quadrant-${key.toLowerCase()}`}
                onClick={() => {
                  setQuadrantFilter(quadrantFilter === key ? "" : key);
                  scrollToId("me-matrix-table");
                }}
              >
                <strong>{title}</strong>
                <div className="sme-muted">{sub}</div>
              </button>
            ))}
          </div>
          {quadrantFilter && (
            <div className="sila-studio-chips" aria-label="Active quadrant filter">
              <span className="sila-studio-chip is-active">
                <span>Showing {quadrantFilter === "PLOW" ? "PLOWHORSE" : quadrantFilter} dishes</span>
                <button type="button" aria-label="Clear quadrant filter" onClick={() => setQuadrantFilter("")}>×</button>
              </span>
            </div>
          )}
          <div className="sila-studio-table-wrap" id="me-matrix-table">
            <table className="sila-table sila-tabular">
              <thead><tr><th>Dish</th><th>Category</th><th>Qty Sold</th><th>Net Rev</th><th>Mix %</th>{viewCost && <th>Cost/Portion</th>}{viewMargin && <th>Unit CM</th>}<th>Quadrant</th><th>Action</th></tr></thead>
              <tbody>
                {matrix.rows.filter((r) => !quadrantFilter || (r.classification ?? "").toUpperCase().startsWith(quadrantFilter)).map((r) => (
                  <tr key={r.menuItemId}>
                    <td><strong>{r.name}</strong></td>
                    <td>{r.category}</td>
                    <td>{r.qtySold}</td>
                    <td>{num(r.revenue)}</td>
                    <td>{pct(r.mixPercent)}</td>
                    {viewCost && <td>{num(r.costPerPortion)}</td>}
                    {viewMargin && <td>{num(r.contributionMargin)}</td>}
                    <td><span className="sme-chip">{r.classification}</span></td>
                    <td className="sme-muted">{r.recommendedAction}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {matrix.excludedRows.length > 0 && (
            <div className="sme-kpi-box">
              <strong>Excluded Items ({matrix.excludedCount}) — Not Misclassified as Dogs</strong>
              {matrix.excludedRows.map((x) => <div key={x.menuItemId} className="sme-muted">• {x.name}: {x.exclusionReason}</div>)}
            </div>
          )}
        </StudioChartContainer>
      </div>
    );
  }

  // 8. CONSUMPTION ANALYSIS
  if (navKey === "silaMenuConsumption" && consumption) {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <ConsumptionRootCauseAiPanel propertyId={filter.propertyId} outletLocationId={filter.outletLocationId} />
        )}
        <StudioChartContainer
          title="Consumption Analysis (Theoretical vs Actual & Waste)"
          subtitle="Double-counting prevented across POS sales and production posting"
          fullScreen={fullscreenView}
          onToggleFullScreen={() => setFullscreenView((f) => !f)}
          actions={<span className="sme-chip sme-chip--ok">Double-Counting Prevented</span>}
        >
          <StudioKPIBar ariaLabel="Consumption metrics">
            <StudioKPICard label="Theoretical" value={consumption.theoretical.toFixed(2)} tone="blue" badge="Planned" drillLabel="Show every ingredient" selected={consumptionFilter === "all"} onClick={() => { setConsumptionFilter("all"); scrollToId("me-consumption-table"); }} />
            <StudioKPICard label="Actual" value={consumption.actual.toFixed(2)} tone="sky" badge="Actual" drillLabel="Show every ingredient" onClick={() => { setConsumptionFilter("all"); scrollToId("me-consumption-table"); }} />
            <StudioKPICard label="Yield Loss" value={consumption.expectedYieldLoss.toFixed(2)} tone="amber" badge="Actual" drillLabel="Show ingredients with yield loss" selected={consumptionFilter === "yield"} onClick={() => { setConsumptionFilter("yield"); scrollToId("me-consumption-table"); }} />
            <StudioKPICard label="Recorded Waste" value={consumption.recordedWaste.toFixed(2)} tone="rose" badge="Actual" drillLabel="Show ingredients with recorded waste" selected={consumptionFilter === "waste"} onClick={() => { setConsumptionFilter("waste"); scrollToId("me-consumption-table"); }} />
            <StudioKPICard label="Unexplained Var" value={consumption.unexplainedVariance.toFixed(2)} tone="indigo" badge="Actual" drillLabel="Show ingredients with unexplained variance" selected={consumptionFilter === "unexplained"} onClick={() => { setConsumptionFilter("unexplained"); scrollToId("me-consumption-table"); }} />
          </StudioKPIBar>
          <div className="sila-studio-table-wrap" id="me-consumption-table">
            <table className="sila-table sila-tabular">
              <thead><tr><th>Ingredient</th><th>UOM</th><th>Theoretical</th><th>Actual</th><th>Yield Loss</th><th>Waste</th><th>Unexplained</th></tr></thead>
              <tbody>
                {consumption.rows
                  .filter((r) =>
                    consumptionFilter === "all" ||
                    (consumptionFilter === "yield" && r.expectedYieldLossQty > 0) ||
                    (consumptionFilter === "waste" && r.recordedWasteQty > 0) ||
                    (consumptionFilter === "unexplained" && r.unexplainedVarianceQty !== 0)
                  )
                  .map((r) => (
                  <tr key={r.materialId}>
                    <td><strong>{r.materialName}</strong></td>
                    <td>{r.uom}</td>
                    <td>{r.theoreticalQty}</td>
                    <td>{r.actualQty}</td>
                    <td>{r.expectedYieldLossQty}</td>
                    <td>{r.recordedWasteQty}</td>
                    <td><strong>{r.unexplainedVarianceQty}</strong></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </StudioChartContainer>
      </div>
    );
  }

  // 9. COST & MARGIN ANALYSIS
  if (navKey === "silaMenuCost" && costMargin) {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <CostShockAndPricingAiPanel propertyId={filter.propertyId} />
        )}
        <StudioChartContainer
          title="Cost & Margin Analysis"
          subtitle={costMargin.disclaimer || "Contribution Margin equals Net Revenue minus Applicable Variable Costs (not Net Profit)."}
          fullScreen={fullscreenView}
          onToggleFullScreen={() => setFullscreenView((f) => !f)}
        >
          <StudioKPIBar ariaLabel="Cost and margin metrics">
            <StudioKPICard label="Total Revenue" value={num(costMargin.totalRevenue)} tone="sky" badge="Actual" drillLabel="Open POS sales" onClick={() => onNavigate("silaMenuPosSales")} />
            {viewCost && <StudioKPICard label="Total Food Cost" value={num(costMargin.totalFoodCost)} tone="blue" badge="Actual" drillLabel="Open consumption analysis" onClick={() => onNavigate("silaMenuConsumption")} />}
            {viewCost && <StudioKPICard label="Food Cost %" value={pct(costMargin.overallFoodCostPercent)} tone="amber" badge="Actual" drillLabel="Open menu performance by dish" onClick={() => onNavigate("silaMenuPerformance")} />}
            {viewMargin && <StudioKPICard label="Contribution Margin" value={num(costMargin.totalContributionMargin)} tone="green" badge="Actual" drillLabel="Open menu performance by dish" onClick={() => onNavigate("silaMenuPerformance")} />}
          </StudioKPIBar>
          {costMargin.marginAlerts.map((a, i) => (
            <div key={i} className="sme-kpi-box" style={{ background: "#fff1f1", borderColor: "#f3cccc" }}>
              <strong>{a.title}</strong> — <span className="sme-muted">{a.recommendedAction}</span>
            </div>
          ))}
        </StudioChartContainer>
      </div>
    );
  }

  // 10. PURCHASE RECOMMENDATIONS
  if (navKey === "silaMenuRecommendations" && purchasePlan) {
    return (
      <div className="sme-panel">
        <div className="sme-panel__header">
          <h2 className="sme-panel__title">Menu Purchase Recommendations (Time-Phased)</h2>
          {permissions.generatePurchaseRecommendation && (
            <button type="button" className="sila-btn sila-btn--primary" onClick={async () => { const r = await createMeDraftPurchaseRec(filter); toastService.success(`Draft purchase plan ready (${r.items.length} line(s)). PO transmission disabled.`); }}>
              Create Draft Purchase Rec
            </button>
          )}
        </div>
        <div className="sme-muted">Supply arriving after the menu required date is excluded from eligible incoming supply. POs are never auto-transmitted.</div>
        <table className="sila-table">
          <thead><tr><th>Ingredient</th><th>Req Date</th><th>Gross Need</th><th>Eligible Avail</th><th>On-Time PO</th><th>Late PO (Excl)</th><th>Shortage</th><th>Rec. Buy</th></tr></thead>
          <tbody>
            {purchasePlan.rows.map((r, i) => (
              <tr key={i}>
                <td><strong>{r.materialName}</strong></td>
                <td>{r.requiredDate?.slice(0, 10) || "—"}</td>
                <td>{r.requirement} {r.uom}</td>
                <td>{r.eligibleAvailable}</td>
                <td>{r.eligibleIncomingOnTime}</td>
                <td>{r.lateOpenPoExcluded}</td>
                <td><strong>{r.shortage}</strong></td>
                <td>{r.recommendedPurchaseQty}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }

  // 11. AI MENU INTELLIGENCE & CONTROL TOWER
  if (navKey === "silaMenuIntelligence" && aiIntel) {
    return (
      <div className="sme-list">
        <AiControlTowerPanel propertyId={filter.propertyId} permissions={permissions} />
        <AiCockpitFullPanel propertyId={filter.propertyId} canApprove={permissions.approveMenu} />
        <StudioChartContainer
          title="AI Menu Intelligence"
          subtitle={aiIntel.fallbackNotice || "Contextual culinary & commercial recommendations backed by live evidence"}
          fullScreen={fullscreenView}
          onToggleFullScreen={() => setFullscreenView((f) => !f)}
          actions={
            <span className={`sme-chip ${aiIntel.aiProviderAvailable ? "sme-chip--ok" : "sme-chip--warn"}`}>
              {aiIntel.providerMode}
            </span>
          }
        >
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))", gap: "0.75rem" }}>
            {aiIntel.recommendations.map((r, i) => (
              <AIRecommendationCard
                key={i}
                title={r.strategy}
                recommendation={r.recommendation}
                statusLabel={`Confidence ${(r.confidence * 100).toFixed(0)}%`}
                evidence={`${r.supportingEvidence} · Data Quality: ${r.dataQuality}`}
                impact={`Allergens: ${r.allergenImpact ?? "Unchanged"}`}
                requiresApproval={r.requiresApproval}
              />
            ))}
          </div>
        </StudioChartContainer>
      </div>
    );
  }

  // 12. WHAT-IF SIMULATION
  if (navKey === "silaMenuWhatIf") {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.runMenuSimulation && (
          <NlWhatIfAiPanel menuPlanId={plans[0]?.id} propertyId={filter.propertyId} />
        )}
        <StudioChartContainer
          title="What-If Simulation Workbench"
          subtitle="Isolated scenario simulation — live menu and ERP data remain untouched"
          fullScreen={fullscreenView}
          onToggleFullScreen={() => setFullscreenView((f) => !f)}
        >
        <div className="sme-form">
          <label className="sila-field"><span>Scenario Name</span><input className="sila-input" value={simForm.scenarioName} onChange={(e) => setSimForm({ ...simForm, scenarioName: e.target.value })} /></label>
          <label className="sila-field"><span>Expected Covers</span><input className="sila-input" type="number" value={simForm.expectedCovers} onChange={(e) => setSimForm({ ...simForm, expectedCovers: e.target.value })} /></label>
          <label className="sila-field"><span>Ingredient Price Δ %</span><input className="sila-input" type="number" value={simForm.ingredientPriceChangePercent} onChange={(e) => setSimForm({ ...simForm, ingredientPriceChangePercent: e.target.value })} /></label>
          <label className="sila-field"><span>Selling Price Δ %</span><input className="sila-input" type="number" value={simForm.sellingPriceChangePercent} onChange={(e) => setSimForm({ ...simForm, sellingPriceChangePercent: e.target.value })} /></label>
          <label className="sila-field"><span>Portion Size Δ %</span><input className="sila-input" type="number" value={simForm.portionSizeChangePercent} onChange={(e) => setSimForm({ ...simForm, portionSizeChangePercent: e.target.value })} /></label>
          <label className="sila-field"><span>Discount %</span><input className="sila-input" type="number" value={simForm.discountPercent} onChange={(e) => setSimForm({ ...simForm, discountPercent: e.target.value })} /></label>
        </div>
        <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--primary"
            onClick={async () => {
              const res = await runMeSimulation({
                menuPlanId: plans[0]?.id,
                scenarioName: simForm.scenarioName,
                expectedCovers: Number(simForm.expectedCovers) || 300,
                ingredientPriceChangePercent: Number(simForm.ingredientPriceChangePercent) || 0,
                sellingPriceChangePercent: Number(simForm.sellingPriceChangePercent) || 0,
                portionSizeChangePercent: Number(simForm.portionSizeChangePercent) || 0,
                discountPercent: Number(simForm.discountPercent) || 0,
                saveScenario: false,
              });
              setSimRes(res);
            }}
          >
            Simulate (Live Data Untouched)
          </button>
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--secondary"
            onClick={async () => {
              const res = await runMeSimulation({
                menuPlanId: plans[0]?.id,
                scenarioName: simForm.scenarioName,
                expectedCovers: Number(simForm.expectedCovers) || 300,
                ingredientPriceChangePercent: Number(simForm.ingredientPriceChangePercent) || 0,
                sellingPriceChangePercent: Number(simForm.sellingPriceChangePercent) || 0,
                portionSizeChangePercent: Number(simForm.portionSizeChangePercent) || 0,
                discountPercent: Number(simForm.discountPercent) || 0,
                saveScenario: true,
              });
              setSimRes(res);
              toastService.success("Scenario saved.");
            }}
          >
            Save Scenario
          </button>
        </div>
        {simRes && (
          <ScenarioComparison
            title={simForm.scenarioName}
            subtitle={simRes.demandAssumptionDisclosure}
            rows={[
              ...(viewCost
                ? [{ label: "Food Cost", baseline: num(simRes.baselineCost), simulated: num(simRes.foodCost), delta: `${simRes.costVariancePct}%` }]
                : []),
              { label: "Expected Revenue", baseline: num(simRes.baselineRevenue), simulated: num(simRes.revenue), delta: `${simRes.revenueVariancePct}%` },
              ...(viewMargin
                ? [{ label: "Contribution Margin", baseline: num(simRes.baselineMargin), simulated: num(simRes.contributionMargin), delta: `${simRes.marginVariancePct}%` }]
                : []),
              { label: "Ingredient Requirement / Shortage", baseline: String(simRes.ingredientRequirement), simulated: String(simRes.shortage), delta: simRes.shortage > 0 ? "Shortage" : "Ready" },
            ]}
          />
        )}
        </StudioChartContainer>
      </div>
    );
  }

  // 13. SETUP (Sections A–H)
  if (navKey === "silaMenuSetup" && setup) {
    return (
      <div className="sme-list">
        {operatingMode !== "MANUAL" && permissions.useAiMenuIntelligence && (
          <SetupWizardAiPanel propertyId={filter.propertyId} />
        )}
        <div className="sme-panel">
        <div className="sme-panel__header">
          <h2 className="sme-panel__title">Menu Engineering Setup (Config v{setup.configVersion})</h2>
          {permissions.manageMenuSetup && (
            <button type="button" className="sila-btn sila-btn--primary" onClick={async () => { const saved = await saveMeSetup(setup); setSetup(saved); toastService.success(`Saved setup v${saved.configVersion}`); }}>
              Save Configuration
            </button>
          )}
        </div>
        <div className="sme-form">
          <label className="sila-field"><span>A. Feature Enabled</span><input type="checkbox" checked={setup.menuEngineeringEnabled} onChange={(e) => setSetup({ ...setup, menuEngineeringEnabled: e.target.checked })} /></label>
          <label className="sila-field"><span>A. ME Studio Enabled</span><input type="checkbox" checked={setup.meStudioEnabled} onChange={(e) => setSetup({ ...setup, meStudioEnabled: e.target.checked })} /></label>
          <label className="sila-field"><span>B. Costing Method</span><input className="sila-input" value={setup.costingMethod} onChange={(e) => setSetup({ ...setup, costingMethod: e.target.value })} /></label>
          <label className="sila-field"><span>B. Target Food Cost %</span><input className="sila-input" type="number" step="0.01" value={setup.targetFoodCostPercent} onChange={(e) => setSetup({ ...setup, targetFoodCostPercent: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>B. Currency / Tax</span><input className="sila-input" value={`${setup.defaultCurrency} (${setup.taxTreatment})`} onChange={(e) => setSetup({ ...setup, defaultCurrency: e.target.value.split(" ")[0] })} /></label>
          <label className="sila-field"><span>C. Portion Variance %</span><input className="sila-input" type="number" value={setup.portionVarianceThresholdPercent} onChange={(e) => setSetup({ ...setup, portionVarianceThresholdPercent: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>D. Forecast Horizon (Days)</span><input className="sila-input" type="number" value={setup.forecastHorizonDays} onChange={(e) => setSetup({ ...setup, forecastHorizonDays: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>E. Popularity Threshold</span><input className="sila-input" type="number" step="0.05" value={setup.popularityThreshold} onChange={(e) => setSetup({ ...setup, popularityThreshold: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>E. Profitability Basis</span>
            <select className="sila-input" value={setup.profitabilityBasis} onChange={(e) => setSetup({ ...setup, profitabilityBasis: e.target.value })}>
              <option value="FOOD_COST_ONLY">Food Cost Only</option>
              <option value="VARIABLE_COST">Configured Variable Cost</option>
            </select>
          </label>
          <label className="sila-field"><span>F. POS Sync Cron</span><input className="sila-input" value={setup.posSyncScheduleCron} onChange={(e) => setSetup({ ...setup, posSyncScheduleCron: e.target.value })} /></label>
          <label className="sila-field"><span>G. AI Confidence Threshold</span><input className="sila-input" type="number" step="0.05" value={setup.aiConfidenceThreshold} onChange={(e) => setSetup({ ...setup, aiConfidenceThreshold: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>H. Require Approval on Publish</span><input type="checkbox" checked={setup.requireApprovalOnPublish} onChange={(e) => setSetup({ ...setup, requireApprovalOnPublish: e.target.checked })} /></label>
          <label className="sila-field"><span>I. Autonomous Mode Paused</span><input type="checkbox" checked={Boolean(setup.autonomousModePaused)} onChange={(e) => setSetup({ ...setup, autonomousModePaused: e.target.checked })} /></label>
          <label className="sila-field"><span>I. Automated Execution Disabled</span><input type="checkbox" checked={Boolean(setup.automatedExecutionDisabled)} onChange={(e) => setSetup({ ...setup, automatedExecutionDisabled: e.target.checked })} /></label>
          <label className="sila-field"><span>I. Daily AI Token Budget</span><input className="sila-input" type="number" value={setup.dailyAiTokenBudget ?? 250000} onChange={(e) => setSetup({ ...setup, dailyAiTokenBudget: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>I. Monthly AI Budget (USD)</span><input className="sila-input" type="number" step="10" value={setup.monthlyAiBudgetUsd ?? 250} onChange={(e) => setSetup({ ...setup, monthlyAiBudgetUsd: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>I. Max Concurrent Jobs</span><input className="sila-input" type="number" value={setup.maxConcurrentJobs ?? 4} onChange={(e) => setSetup({ ...setup, maxConcurrentJobs: Number(e.target.value) })} /></label>
          <label className="sila-field"><span>J. Microsoft Teams Alerts</span><input type="checkbox" checked={Boolean(setup.teamsNotificationsEnabled)} onChange={(e) => setSetup({ ...setup, teamsNotificationsEnabled: e.target.checked })} /></label>
          <label className="sila-field"><span>J. Teams Webhook URL</span><input className="sila-input" value={setup.teamsWebhookUrl ?? ""} placeholder="https://..." onChange={(e) => setSetup({ ...setup, teamsWebhookUrl: e.target.value })} /></label>
        </div>
        {setup.history.length > 0 && (
          <div className="sme-kpi-box">
            <strong>Configuration History</strong>
            {setup.history.slice(0, 5).map((h) => <div key={h.id} className="sme-muted">v{h.configVersion} • {h.changeSummary} ({new Date(h.changedAtUtc).toLocaleString()})</div>)}
          </div>
        )}
        </div>

        {/* 12 INDEPENDENT FEATURE FLAGS */}
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">Independent Feature Flags (Tenant / Property Release Control)</h2>
            {permissions.manageMenuSetup && (
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                onClick={async () => {
                  const saved = await saveMeSetup(setup);
                  setSetup(saved);
                  toastService.success(`Saved feature flags (v${saved.configVersion}).`);
                }}
              >
                Save Feature Flags
              </button>
            )}
          </div>
          <div className="sme-muted" style={{ marginBottom: "0.6rem" }}>
            Procurement V1 remains fully operational when all 12 Menu Engineering flags are disabled.
          </div>
          <div className="sme-grid">
            {FEATURE_FLAG_LABELS.map(({ key, label }) => {
              const flags = setup.featureFlags ?? DEFAULT_FEATURE_FLAGS;
              return (
                <label key={key} className="sme-kpi-box" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", cursor: "pointer" }}>
                  <span><strong>{label}</strong></span>
                  <input
                    type="checkbox"
                    checked={Boolean(flags[key])}
                    disabled={!permissions.manageMenuSetup}
                    onChange={(e) => {
                      const nextFlags = { ...flags, [key]: e.target.checked };
                      setSetup({
                        ...setup,
                        menuEngineeringEnabled: key === "menuEngineering" ? e.target.checked : setup.menuEngineeringEnabled,
                        meStudioEnabled: key === "meStudio" ? e.target.checked : setup.meStudioEnabled,
                        featureFlags: nextFlags,
                      });
                    }}
                  />
                </label>
              );
            })}
          </div>
        </div>

        {/* STAGED CUSTOMER DATA MIGRATION */}
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">Staged Customer Data Migration &amp; Import Templates</h2>
            <div style={{ display: "flex", gap: "0.45rem", flexWrap: "wrap" }}>
              <select
                className="sila-input"
                value={migEntityType}
                onChange={(e) => {
                  const nextType = e.target.value;
                  setMigEntityType(nextType);
                  setMigPreview(null);
                  const tpl = migTemplates.find((t) => t.entityType === nextType);
                  if (tpl) setMigCsv(tpl.sampleCsv);
                }}
              >
                {(migTemplates.length > 0 ? migTemplates : [{ entityType: "MENU_ITEMS", title: "Menu Items", fileName: "template_menu_items.csv", columns: [], sampleCsv: "" }]).map((t) => (
                  <option key={t.entityType} value={t.entityType}>{t.title}</option>
                ))}
              </select>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                onClick={() => {
                  const tpl = migTemplates.find((t) => t.entityType === migEntityType);
                  if (!tpl) return;
                  const blob = new Blob([tpl.sampleCsv], { type: "text/csv;charset=utf-8" });
                  const url = URL.createObjectURL(blob);
                  const a = document.createElement("a");
                  a.href = url;
                  a.download = tpl.fileName;
                  a.click();
                  URL.revokeObjectURL(url);
                }}
              >
                Download Excel/CSV Template
              </button>
            </div>
          </div>
          <textarea
            className="sila-input"
            rows={5}
            style={{ width: "100%", fontFamily: "monospace", fontSize: "0.82rem" }}
            value={migCsv}
            onChange={(e) => setMigCsv(e.target.value)}
            placeholder="Paste CSV rows matching the selected template…"
          />
          <div style={{ display: "flex", gap: "0.5rem", alignItems: "center", flexWrap: "wrap", marginTop: "0.5rem" }}>
            <label style={{ display: "flex", gap: "0.35rem", alignItems: "center" }}>
              <input type="checkbox" checked={migAllowOverwrite} onChange={(e) => setMigAllowOverwrite(e.target.checked)} />
              <span>Authorize overwrite of existing approved master data (requires Setup Admin permission)</span>
            </label>
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              onClick={async () => {
                const res = await previewMeDataMigration({ entityType: migEntityType, format: "CSV", payload: migCsv, propertyId: filter.propertyId, allowOverwriteApproved: migAllowOverwrite });
                setMigPreview(res);
              }}
            >
              Preview &amp; Validate
            </button>
            {permissions.manageMenuSetup && (
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                disabled={!migPreview}
                onClick={async () => {
                  const res = await commitMeDataMigration({ entityType: migEntityType, format: "CSV", payload: migCsv, propertyId: filter.propertyId, allowOverwriteApproved: migAllowOverwrite });
                  toastService.success(`Committed ${res.createdCount} created, ${res.updatedCount} updated, ${res.skippedCount} skipped.`);
                  setMigAudits(await getMeDataMigrationAudits());
                  setMigPreview(null);
                }}
              >
                Commit Staged Import
              </button>
            )}
          </div>
          {migPreview && (
            <div className="sme-kpi-box" style={{ marginTop: "0.6rem" }}>
              <strong>Preview Summary:</strong> Total {migPreview.totalRows} • Valid {migPreview.validRows} • Warnings {migPreview.warningRows} • Errors {migPreview.errorRows} • Duplicates {migPreview.duplicateRows} • Protected Approved {migPreview.protectedApprovedRows}
              <table className="sila-table" style={{ marginTop: "0.4rem" }}>
                <thead><tr><th>Row</th><th>Key</th><th>Status</th><th>Detail</th></tr></thead>
                <tbody>
                  {migPreview.rows.slice(0, 15).map((r) => (
                    <tr key={r.rowNumber}><td>{r.rowNumber}</td><td>{r.key}</td><td>{r.status}</td><td>{r.message}</td></tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {migAudits.length > 0 && (
            <div className="sme-kpi-box" style={{ marginTop: "0.6rem" }}>
              <strong>Import Audit History</strong>
              {migAudits.slice(0, 5).map((a) => (
                <div key={a.id} className="sme-muted">{a.entityType} • {a.summary} ({new Date(a.importedAtUtc).toLocaleString()})</div>
              ))}
            </div>
          )}
        </div>
      </div>
    );
  }

  return null;
};

export default MeOperationalViews;
