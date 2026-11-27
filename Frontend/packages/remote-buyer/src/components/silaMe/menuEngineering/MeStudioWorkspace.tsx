import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Loader, toastService } from "@vosox/shared-ui";
import {
  bulkImportMeMenuItems,
  compareMeScenarios,
  convertMeScenarioToDraft,
  createMeDraftPurchaseRec,
  createMePlanFromAi,
  createMePlanFromHistorical,
  getMeAiIntelligence,
  getMeWorkspace,
  runMeSimulation,
  saveMeMenuPlan,
  setMePreferredScenario,
  transitionMeMenuPlan,
  type AiMenuIntelligenceDto,
  type MeStudioWorkspaceDto,
  type MenuItemEnhanced,
  type MenuPlanEnhanced,
  type MenuPlanLineEnhanced,
  type ScenarioCompareResultDto,
  type WhatIfSimulationResult,
} from "../../../api/silaMe/menuEngineeringStudioApi";

import {
  runMeContextualAi,
  runMeNlSimulation,
  type AiContextualResponse,
  type AiNlSimulationResult,
  type AiOperatingMode,
} from "../../../api/silaMe/menuEngineeringAiApi";
import { AiMultiAgentCommandBar } from "./ai/AiMultiAgentCommandBar";
import { AiValueTag } from "./ai/AiSharedWidgets";

interface Props {
  propertyId?: string;
  outletLocationId?: string;
  operatingMode?: AiOperatingMode;
  onActiveContextChange?: (planId?: string, itemId?: string) => void;
  onOpenReports: () => void;
  onNavigate: (key: string) => void;
}

type StudioTab =
  | "DESIGN"
  | "COSTING"
  | "PORTION_DEMAND"
  | "PERFORMANCE"
  | "READINESS"
  | "HISTORICAL"
  | "AI_OPTIMIZATION"
  | "WHAT_IF"
  | "COMPARE"
  | "APPROVAL";

const MENU_TYPES = [
  "A_LA_CARTE",
  "BUFFET",
  "BRUNCH",
  "BANQUET",
  "CORPORATE_CATERING",
  "SET_MENU",
  "TASTING_MENU",
  "ROOM_SERVICE",
  "SEASONAL",
  "SPECIAL_EVENT",
];

const num = (v?: number | null, d = 2) => (v == null ? "—" : v.toFixed(d));
const pct = (v?: number | null) => (v == null ? "—" : `${v.toFixed(1)}%`);

const MeStudioWorkspace: React.FC<Props> = ({
  propertyId,
  outletLocationId,
  operatingMode = "AI_ASSISTED",
  onActiveContextChange,
  onOpenReports,
  onNavigate,
}) => {
  const [ws, setWs] = useState<MeStudioWorkspaceDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [activeTab, setActiveTab] = useState<StudioTab>("DESIGN");
  const [bottomExpanded, setBottomExpanded] = useState(true);
  const [selectedPlanId, setSelectedPlanId] = useState<string>("");

  // Library filters
  const [libSearch, setLibSearch] = useState("");
  const [libCat, setLibCat] = useState("");
  const [libCuisine, setLibCuisine] = useState("");
  const [libDietary, setLibDietary] = useState("");
  const [libAllergenExcl, setLibAllergenExcl] = useState("");
  const [libAvailOnly, setLibAvailOnly] = useState(false);
  const [libClass, setLibClass] = useState("");
  const [libMaxCost, setLibMaxCost] = useState("");

  // Active draft menu editor state
  const [draftName, setDraftName] = useState("New Executive Menu");
  const [draftType, setDraftType] = useState("A_LA_CARTE");
  const [draftFormat, setDraftFormat] = useState("A_LA_CARTE");
  const [draftMeal, setDraftMeal] = useState("Dinner");
  const [draftDate, setDraftDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [draftCovers, setDraftCovers] = useState("150");
  const [draftTargetFc, setDraftTargetFc] = useState("30");
  const [draftTargetCm, setDraftTargetCm] = useState("");
  const [draftDietary, setDraftDietary] = useState("");
  const [draftNotes, setDraftNotes] = useState("");
  const [draftStatus, setDraftStatus] = useState("DRAFT");
  const [draftVersion, setDraftVersion] = useState(1);
  const [categoriesOrder, setCategoriesOrder] = useState<string[]>(["Appetizers", "Soups & Salads", "Mains", "Sides", "Desserts", "Beverages"]);
  const [lines, setLines] = useState<MenuPlanLineEnhanced[]>([]);
  const [dragDishId, setDragDishId] = useState<string | null>(null);

  // Creation / import modals
  const [showCreateMode, setShowCreateMode] = useState<"" | "HISTORICAL" | "AI" | "IMPORT">("");
  const [csvImport, setCsvImport] = useState("Code,Name,Category,Cuisine,SellingPrice,PortionUom\nME-NEW-1,Pan Seared Sea Bass,Mains,Mediterranean,34.00,PORTION");

  // AI / What-If / Compare / Approval state
  const [aiData, setAiData] = useState<AiMenuIntelligenceDto | null>(null);
  const [simReq, setSimReq] = useState({
    scenarioName: "Covers & Cost Stress Test",
    expectedCovers: "200",
    ingredientPriceChangePercent: "10",
    sellingPriceChangePercent: "0",
    portionSizeChangePercent: "0",
    discountPercent: "0",
    wastePercentOverride: "4",
    yieldPercentOverride: "92",
    bufferPercentOverride: "0",
  });
  const [simResult, setSimResult] = useState<WhatIfSimulationResult | null>(null);
  const [compareIds, setCompareIds] = useState<string[]>([]);
  const [compareResult, setCompareResult] = useState<ScenarioCompareResultDto | null>(null);
  const [approvalComment, setApprovalComment] = useState("");
  const [itemAiFeedback, setItemAiFeedback] = useState<{ itemName: string; response: AiContextualResponse } | null>(null);
  const [nlQuery, setNlQuery] = useState("What happens if salmon cost increases by 20%?");
  const [nlSimResult, setNlSimResult] = useState<AiNlSimulationResult | null>(null);
  const debounceRef = useRef<number | null>(null);

  const hydrateFromPlan = useCallback((p?: MenuPlanEnhanced | null) => {
    if (!p) return;
    setSelectedPlanId(p.id);
    onActiveContextChange?.(p.id, undefined);
    setDraftName(p.name);
    setDraftType(p.menuType || "A_LA_CARTE");
    setDraftFormat(p.serviceFormat || p.menuType || "A_LA_CARTE");
    setDraftMeal(p.mealPeriod || "Dinner");
    setDraftDate(p.businessDate.slice(0, 10));
    setDraftCovers(String(p.expectedCovers ?? p.expectedGuests ?? 100));
    setDraftTargetFc(String(p.targetFoodCostPercent ?? 30));
    setDraftTargetCm(p.targetContributionMargin != null ? String(p.targetContributionMargin) : "");
    setDraftDietary(p.dietaryRequirements ?? "");
    setDraftNotes(p.notes ?? "");
    setDraftStatus(p.status);
    setDraftVersion(p.versionNumber);
    if (p.categoriesOrder?.length) setCategoriesOrder(p.categoriesOrder);
    setLines(p.lines || []);
  }, []);

  const loadWorkspace = useCallback(
    async (planId?: string) => {
      setLoading(true);
      try {
        const res = await getMeWorkspace(planId || undefined, propertyId || undefined);
        setWs(res);
        if (res.activeMenu) hydrateFromPlan(res.activeMenu);
      } catch (e) {
        toastService.error(e instanceof Error ? e.message : "Failed to load ME Studio.");
      } finally {
        setLoading(false);
      }
    },
    [hydrateFromPlan, propertyId]
  );

  useEffect(() => {
    void loadWorkspace(selectedPlanId || undefined);
  }, [propertyId]); // eslint-disable-line react-hooks/exhaustive-deps

  // Debounced live simulation preview when draft covers or lines change
  useEffect(() => {
    if (!ws?.permissions.runMenuSimulation || !selectedPlanId) return;
    if (debounceRef.current) window.clearTimeout(debounceRef.current);
    debounceRef.current = window.setTimeout(() => {
      void runMeSimulation({
        menuPlanId: selectedPlanId,
        expectedCovers: Number(draftCovers) || 100,
        saveScenario: false,
      })
        .then(setSimResult)
        .catch(() => undefined);
    }, 450);
    return () => {
      if (debounceRef.current) window.clearTimeout(debounceRef.current);
    };
  }, [draftCovers, lines.length, selectedPlanId, ws?.permissions.runMenuSimulation]);

  const filteredLibrary = useMemo(() => {
    const all = ws?.libraryItems ?? [];
    const q = libSearch.trim().toLowerCase();
    const maxC = Number(libMaxCost);
    return all.filter((it) => {
      if (q && !it.name.toLowerCase().includes(q) && !it.menuItemCode.toLowerCase().includes(q) && !(it.cuisine ?? "").toLowerCase().includes(q)) return false;
      if (libCat && it.category !== libCat) return false;
      if (libCuisine && it.cuisine !== libCuisine) return false;
      if (libDietary && !(it.dietaryTags ?? "").toLowerCase().includes(libDietary.toLowerCase())) return false;
      if (libAllergenExcl && (it.allergenNotes ?? "").toLowerCase().includes(libAllergenExcl.toLowerCase())) return false;
      if (libAvailOnly && it.ingredientAvailabilityStatus !== "READY" && !it.isAvailable) return false;
      if (libClass && it.engineeringClassification !== libClass) return false;
      if (libMaxCost && !Number.isNaN(maxC) && (it.costPerPortion ?? 0) > maxC) return false;
      return true;
    });
  }, [ws?.libraryItems, libSearch, libCat, libCuisine, libDietary, libAllergenExcl, libAvailOnly, libClass, libMaxCost]);

  // Real-time client-side rollups matching backend calculation basis while user edits canvas
  const liveCalc = useMemo(() => {
    const covers = Math.max(1, Number(draftCovers) || 100);
    let totalCost = 0;
    let totalRev = 0;
    for (const l of lines) {
      const part = l.participationRate ?? 0.5;
      const portions = covers * part * (l.expectedServingsPerGuest ?? 1);
      totalCost += (l.portionCost ?? 0) * portions;
      totalRev += (l.sellingPrice ?? 0) * portions;
    }
    const fcPct = totalRev > 0 ? (totalCost / totalRev) * 100 : null;
    const cm = totalRev - totalCost;
    return {
      covers,
      totalCost,
      costPerGuest: totalCost / covers,
      totalRev,
      fcPct,
      cm,
    };
  }, [draftCovers, lines]);

  const addDishToCategory = (dish: MenuItemEnhanced, categoryName?: string) => {
    const cat = categoryName || dish.category || categoriesOrder[0] || "Mains";
    if (!categoriesOrder.includes(cat)) setCategoriesOrder((prev) => [...prev, cat]);
    const covers = Math.max(1, Number(draftCovers) || 100);
    const nextLine: MenuPlanLineEnhanced = {
      menuItemId: dish.id!,
      menuItemName: dish.name,
      menuItemCode: dish.menuItemCode,
      recipeLinkStatus: dish.linkedRecipeId ? "LINKED" : "MISSING",
      recipeName: dish.linkedRecipeName,
      imagePath: dish.imagePath,
      allergenNotes: dish.allergenNotes,
      categoryName: cat,
      sequenceOrder: lines.length,
      recipeId: dish.linkedRecipeId,
      recipeVersion: dish.approvedRecipeVersion,
      portionSize: dish.defaultPortionQty || 1,
      portionUom: dish.portionUom || "PORTION",
      expectedServingsPerGuest: 1,
      sellingPrice: dish.sellingPrice ?? 0,
      expectedCovers: covers,
      participationRate: dish.defaultParticipationRate || 0.5,
      bufferPercentage: dish.defaultBufferPercentage || 0.05,
      dietaryTags: dish.dietaryTags,
      recommendedPortions: Math.round(covers * (dish.defaultParticipationRate || 0.5)),
      requiredUsableOutput: Math.round(covers * (dish.defaultParticipationRate || 0.5) * (dish.defaultPortionQty || 1)),
      portionsConfirmed: false,
      portionCost: dish.costPerPortion,
      totalLineCost: dish.costPerPortion != null ? dish.costPerPortion * covers * (dish.defaultParticipationRate || 0.5) : null,
      expectedLineRevenue: (dish.sellingPrice ?? 0) * covers * (dish.defaultParticipationRate || 0.5),
      foodCostPercent: dish.foodCostPercent,
      contributionMargin: dish.contributionMargin,
      availabilityStatus: dish.ingredientAvailabilityStatus || "READY",
    };
    setLines((prev) => [...prev, nextLine]);
  };

  const moveCategory = (idx: number, dir: -1 | 1) => {
    const target = idx + dir;
    if (target < 0 || target >= categoriesOrder.length) return;
    const copy = [...categoriesOrder];
    const [removed] = copy.splice(idx, 1);
    copy.splice(target, 0, removed);
    setCategoriesOrder(copy);
  };

  const handleSaveMenu = async (copyAsNew = false, asTemplate = false) => {
    if (!draftName.trim()) return toastService.error("Menu name is required.");
    setSaving(true);
    try {
      const saved = await saveMeMenuPlan({
        id: copyAsNew || asTemplate ? undefined : selectedPlanId || undefined,
        copyFromPlanId: copyAsNew ? selectedPlanId || undefined : undefined,
        name: asTemplate ? `${draftName} (Template)` : copyAsNew ? `${draftName} (Copy)` : draftName.trim(),
        propertyId: propertyId || undefined,
        businessDate: draftDate,
        mealPeriod: draftMeal,
        menuType: draftType,
        serviceFormat: draftFormat,
        isTemplate: asTemplate,
        expectedGuests: Number(draftCovers) || 100,
        expectedCovers: Number(draftCovers) || 100,
        targetFoodCostPercent: Number(draftTargetFc) || 30,
        targetContributionMargin: draftTargetCm ? Number(draftTargetCm) : undefined,
        dietaryRequirements: draftDietary || undefined,
        notes: draftNotes || undefined,
        categoriesOrder,
        lines: lines.map((l, idx) => ({
          menuItemId: l.menuItemId,
          categoryName: l.categoryName || "Mains",
          sequenceOrder: idx,
          recipeId: l.recipeId,
          recipeVersion: l.recipeVersion,
          portionSize: l.portionSize ?? 1,
          portionUom: l.portionUom || "PORTION",
          expectedServingsPerGuest: l.expectedServingsPerGuest ?? 1,
          sellingPrice: l.sellingPrice,
          expectedCovers: Number(draftCovers) || 100,
          participationRate: l.participationRate ?? 0.5,
          bufferPercentage: l.bufferPercentage ?? 0.05,
          lineNotes: l.lineNotes,
          alternativesJson: l.alternativesJson,
          dietaryTags: l.dietaryTags,
        })),
      });
      toastService.success(
        saved.parentPublishedPlanId
          ? `Published version preserved; created controlled Draft v${saved.versionNumber}.`
          : `Saved ${saved.name} (v${saved.versionNumber}).`
      );
      await loadWorkspace(saved.id);
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Could not save menu.");
    } finally {
      setSaving(false);
    }
  };

  const handleTransition = async (targetStatus: string, handoff = false) => {
    if (!selectedPlanId) return;
    setSaving(true);
    try {
      const updated = await transitionMeMenuPlan(selectedPlanId, targetStatus, approvalComment || undefined, handoff);
      toastService.success(`Menu moved to ${updated.status} (v${updated.versionNumber}).`);
      setApprovalComment("");
      await loadWorkspace(updated.id);
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Lifecycle transition failed.");
    } finally {
      setSaving(false);
    }
  };

  const handleRunSim = async (saveScenario = false) => {
    try {
      const res = await runMeSimulation({
        menuPlanId: selectedPlanId || undefined,
        scenarioName: simReq.scenarioName,
        expectedCovers: Number(simReq.expectedCovers) || 100,
        ingredientPriceChangePercent: Number(simReq.ingredientPriceChangePercent) || 0,
        sellingPriceChangePercent: Number(simReq.sellingPriceChangePercent) || 0,
        portionSizeChangePercent: Number(simReq.portionSizeChangePercent) || 0,
        discountPercent: Number(simReq.discountPercent) || 0,
        wastePercentOverride: Number(simReq.wastePercentOverride) || 4,
        yieldPercentOverride: Number(simReq.yieldPercentOverride) || 92,
        bufferPercentOverride: Number(simReq.bufferPercentOverride) || 0,
        saveScenario,
      });
      setSimResult(res);
      if (saveScenario) {
        toastService.success("Scenario saved (live data untouched).");
        await loadWorkspace(selectedPlanId || undefined);
      }
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Simulation failed.");
    }
  };

  const invokeDishAi = async (menuItemId: string, itemName: string, action: string, customPrompt?: string) => {
    onActiveContextChange?.(selectedPlanId || undefined, menuItemId);
    try {
      const res = await runMeContextualAi({
        screenKey: "silaMenuStudio",
        action,
        userPrompt: customPrompt,
        propertyId,
        outletLocationId,
        menuPlanId: selectedPlanId || undefined,
        menuItemId,
        operatingMode,
      });
      setItemAiFeedback({ itemName, response: res });
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Dish AI action failed.");
    }
  };

  if (loading && !ws) return <Loader />;
  if (!ws) return null;

  const perms = ws.permissions;
  const viewCost = perms.viewMenuCost;
  const viewMargin = perms.viewMenuMargin;

  return (
    <div className="sme-page">
      {/* HERO & TOP WORKFLOW ACTIONS */}
      <div className="sme-hero">
        <div>
          <p className="sme-kicker">SILA Menu Engineering • Flagship Design Workspace</p>
          <h1 className="sme-hero__title">ME Studio</h1>
          <div style={{ display: "flex", gap: "0.45rem", flexWrap: "wrap", marginTop: "0.35rem" }}>
            <span className="sme-chip">{draftStatus} • v{draftVersion}</span>
            <span className="sme-chip sme-chip--ok">{lines.length} Dishes</span>
            <span className="sme-chip">{draftType.replace(/_/g, " ")}</span>
          </div>
        </div>
        <div className="sme-hero__actions">
          <select
            className="sila-input"
            style={{ minWidth: "13rem" }}
            value={selectedPlanId}
            onChange={(e) => {
              const id = e.target.value;
              setSelectedPlanId(id);
              if (id) void loadWorkspace(id);
            }}
          >
            <option value="">— Select Active Menu —</option>
            {ws.menus.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name} ({m.status} v{m.versionNumber})
              </option>
            ))}
          </select>
          {perms.createMenu && (
            <>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                onClick={() => {
                  setSelectedPlanId("");
                  setDraftName("New Menu Draft");
                  setDraftStatus("DRAFT");
                  setDraftVersion(1);
                  setLines([]);
                }}
              >
                + Blank Menu
              </button>
              <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleSaveMenu(true, false)} disabled={!selectedPlanId || saving}>
                Copy Menu
              </button>
              <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleSaveMenu(false, true)} disabled={saving}>
                Save Template
              </button>
              <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowCreateMode(showCreateMode === "HISTORICAL" ? "" : "HISTORICAL")}>
                From Historical
              </button>
              {perms.useAiMenuIntelligence && (
                <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowCreateMode(showCreateMode === "AI" ? "" : "AI")}>
                  From AI Strategy
                </button>
              )}
              <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowCreateMode(showCreateMode === "IMPORT" ? "" : "IMPORT")}>
                Import Items
              </button>
            </>
          )}
          <button type="button" className="sila-btn sila-btn--secondary" onClick={onOpenReports}>
            Reports (22)
          </button>
          {(perms.editDraftMenu || perms.createMenu) && (
            <button type="button" className="sila-btn sila-btn--primary" onClick={() => handleSaveMenu(false, false)} disabled={saving}>
              {saving ? "Saving…" : draftStatus === "PUBLISHED" ? "Save as New Draft v" + (draftVersion + 1) : "Save Draft"}
            </button>
          )}
        </div>
      </div>

      {operatingMode !== "MANUAL" && perms.useAiMenuIntelligence && (
        <AiMultiAgentCommandBar
          operatingMode={operatingMode}
          propertyId={propertyId}
          outletLocationId={outletLocationId}
          onDraftCreated={(draftId) => {
            void loadWorkspace(draftId);
          }}
        />
      )}

      {itemAiFeedback && (
        <div className="sme-panel" style={{ borderColor: "#93c5fd", background: "#f8fbff" }}>
          <div className="sme-panel__header">
            <div>
              <AiValueTag kind={itemAiFeedback.response.valueTag} />{" "}
              <strong>{itemAiFeedback.itemName}</strong> · {itemAiFeedback.response.agentId}
            </div>
            <button type="button" className="sme-ai-mini-btn" onClick={() => setItemAiFeedback(null)}>Close</button>
          </div>
          <p className="sme-ai-summary">{itemAiFeedback.response.summary}</p>
          <p className="sme-ai-explanation">{itemAiFeedback.response.explanation}</p>
          {itemAiFeedback.response.evidence.length > 0 && (
            <div className="sme-muted">Evidence: {itemAiFeedback.response.evidence.join(" · ")}</div>
          )}
        </div>
      )}

      {/* CREATION DRAWERS: HISTORICAL / AI / BULK IMPORT */}
      {showCreateMode === "HISTORICAL" && (
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h3 className="sme-panel__title">Create Menu from Historical Performance (Recosted at Current Approved Recipes & Prices)</h3>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowCreateMode("")}>Close</button>
          </div>
          <div className="sme-grid">
            {ws.historicalMenus.slice(0, 6).map((h) => (
              <div key={h.sourceId} className="sme-kpi-box">
                <strong>{h.name}</strong>
                <div className="sme-muted">{h.eventType} • {h.businessDate.slice(0, 10)} • {h.guestCount} guests</div>
                {viewCost && <div className="sme-muted">Current Recost: {num(h.recalculatedCurrentCost)} ({pct(h.recalculatedCurrentFoodCostPercent)})</div>}
                <button
                  type="button"
                  className="sila-btn sila-btn--primary"
                  style={{ marginTop: "0.45rem" }}
                  onClick={async () => {
                    try {
                      const created = await createMePlanFromHistorical(h.sourceId, h.sourceKind, propertyId || undefined);
                      toastService.success(`Created draft '${created.name}' with current recipe versions & prices.`);
                      setShowCreateMode("");
                      await loadWorkspace(created.id);
                    } catch (e) {
                      toastService.error(e instanceof Error ? e.message : "Failed to create from historical.");
                    }
                  }}
                >
                  Use as New Draft
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {showCreateMode === "AI" && (
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h3 className="sme-panel__title">Create Menu from AI Recommendation Strategy</h3>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowCreateMode("")}>Close</button>
          </div>
          <div className="sme-grid">
            {[
              ["BALANCED", "Balanced Culinary & Margin"],
              ["PROFITABILITY", "Profitability Optimized"],
              ["POPULARITY", "Popularity Optimized"],
              ["BUDGET", "Budget Optimized"],
              ["PREMIUM", "Premium Guest Experience"],
              ["WASTE", "Waste Optimized"],
              ["INVENTORY", "Inventory Utilization"],
            ].map(([key, label]) => (
              <button
                key={key}
                type="button"
                className="sila-btn sila-btn--secondary"
                onClick={async () => {
                  try {
                    const created = await createMePlanFromAi(key, Number(draftCovers) || 120, propertyId || undefined);
                    toastService.success(`Generated AI draft: ${created.name}`);
                    setShowCreateMode("");
                    await loadWorkspace(created.id);
                  } catch (e) {
                    toastService.error(e instanceof Error ? e.message : "Failed to generate AI menu.");
                  }
                }}
              >
                {label}
              </button>
            ))}
          </div>
        </div>
      )}

      {showCreateMode === "IMPORT" && (
        <div className="sme-panel">
          <div className="sme-panel__header">
            <h3 className="sme-panel__title">Bulk Import Menu Items (CSV)</h3>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowCreateMode("")}>Close</button>
          </div>
          <textarea className="sila-input" rows={4} value={csvImport} onChange={(e) => setCsvImport(e.target.value)} />
          <div>
            <button
              type="button"
              className="sila-btn sila-btn--primary"
              onClick={async () => {
                try {
                  const count = await bulkImportMeMenuItems(csvImport, "CSV");
                  toastService.success(`Imported ${count} menu item(s).`);
                  setShowCreateMode("");
                  await loadWorkspace(selectedPlanId || undefined);
                } catch (e) {
                  toastService.error(e instanceof Error ? e.message : "Bulk import failed.");
                }
              }}
            >
              Import into Library
            </button>
          </div>
        </div>
      )}

      {/* THREE-PANEL DESKTOP WORKSPACE */}
      <div className="sme-studio-shell">
        {/* LEFT PANEL: MENU ITEM LIBRARY */}
        <aside className="sme-panel">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">Menu Item Library ({filteredLibrary.length})</h2>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate("silaMenuItems")}>Master</button>
          </div>
          <input className="sila-input" placeholder="Search dishes, code, cuisine…" value={libSearch} onChange={(e) => setLibSearch(e.target.value)} />
          <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "0.4rem" }}>
            <select className="sila-input" value={libCat} onChange={(e) => setLibCat(e.target.value)}>
              <option value="">All Categories</option>
              {ws.lookups.categories.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
            <select className="sila-input" value={libClass} onChange={(e) => setLibClass(e.target.value)}>
              <option value="">All Quadrants</option>
              <option value="STAR">Stars</option>
              <option value="PLOWHORSE">Plowhorses</option>
              <option value="PUZZLE">Puzzles</option>
              <option value="DOG">Dogs</option>
            </select>
            <input className="sila-input" placeholder="Cuisine…" value={libCuisine} onChange={(e) => setLibCuisine(e.target.value)} />
            <input className="sila-input" placeholder="Dietary (Vegan…)" value={libDietary} onChange={(e) => setLibDietary(e.target.value)} />
            <input className="sila-input" placeholder="Exclude allergen…" value={libAllergenExcl} onChange={(e) => setLibAllergenExcl(e.target.value)} />
            {viewCost && <input className="sila-input" type="number" placeholder="Max cost…" value={libMaxCost} onChange={(e) => setLibMaxCost(e.target.value)} />}
          </div>
          <label className="sme-muted" style={{ display: "flex", gap: "0.4rem", alignItems: "center" }}>
            <input type="checkbox" checked={libAvailOnly} onChange={(e) => setLibAvailOnly(e.target.checked)} />
            Ingredient available only
          </label>

          <div className="sme-library-list">
            {filteredLibrary.map((dish) => (
              <div
                key={dish.id}
                className="sme-dish-card"
                draggable
                onDragStart={() => setDragDishId(dish.id ?? null)}
              >
                <div className="sme-dish-thumb">
                  {dish.imagePath ? <img src={dish.imagePath} alt={dish.name} /> : dish.name.slice(0, 2).toUpperCase()}
                </div>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", gap: "0.25rem" }}>
                    <strong style={{ fontSize: "0.84rem", color: "#0f172a" }}>{dish.name}</strong>
                    {dish.engineeringClassification && <span className="sme-chip">{dish.engineeringClassification}</span>}
                  </div>
                  <div className="sme-muted">
                    {dish.category || "Mains"} • {dish.linkedRecipeName ? `Recipe: ${dish.linkedRecipeName}` : "No recipe linked"}
                  </div>
                  <div className="sme-muted">
                    Sell: {num(dish.sellingPrice)} {viewCost ? `• Cost: ${num(dish.costPerPortion)}` : ""} {viewMargin ? `• CM: ${num(dish.contributionMargin)}` : ""}
                  </div>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: "0.35rem" }}>
                    <span className={`sme-chip ${dish.ingredientAvailabilityStatus === "READY" ? "sme-chip--ok" : "sme-chip--warn"}`}>
                      {dish.ingredientAvailabilityStatus}
                    </span>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={() => addDishToCategory(dish)}>
                      + Add
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </aside>

        {/* CENTER PANEL: MENU DESIGN CANVAS */}
        <section className="sme-panel">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">Menu Design Canvas</h2>
            <span className="sme-muted">Drag dishes from Library into any category or reorder categories</span>
          </div>

          <div className="sme-form">
            <label className="sila-field">
              <span>Menu Name</span>
              <input className="sila-input" value={draftName} onChange={(e) => setDraftName(e.target.value)} />
            </label>
            <label className="sila-field">
              <span>Menu Type</span>
              <select className="sila-input" value={draftType} onChange={(e) => { setDraftType(e.target.value); setDraftFormat(e.target.value); }}>
                {MENU_TYPES.map((t) => <option key={t} value={t}>{t.replace(/_/g, " ")}</option>)}
              </select>
            </label>
            <label className="sila-field">
              <span>Meal Period</span>
              <select className="sila-input" value={draftMeal} onChange={(e) => setDraftMeal(e.target.value)}>
                {ws.lookups.mealPeriods.map((m) => <option key={m} value={m}>{m}</option>)}
              </select>
            </label>
            <label className="sila-field">
              <span>Service Date</span>
              <input className="sila-input" type="date" value={draftDate} onChange={(e) => setDraftDate(e.target.value)} />
            </label>
            <label className="sila-field">
              <span>Expected Covers</span>
              <input className="sila-input" type="number" value={draftCovers} onChange={(e) => setDraftCovers(e.target.value)} />
            </label>
            <label className="sila-field">
              <span>Target Food Cost %</span>
              <input className="sila-input" type="number" value={draftTargetFc} onChange={(e) => setDraftTargetFc(e.target.value)} />
            </label>
            <label className="sila-field">
              <span>Target CM</span>
              <input className="sila-input" type="number" value={draftTargetCm} onChange={(e) => setDraftTargetCm(e.target.value)} />
            </label>
            <label className="sila-field">
              <span>Dietary Coverage</span>
              <input className="sila-input" placeholder="Halal, Vegan, GF…" value={draftDietary} onChange={(e) => setDraftDietary(e.target.value)} />
            </label>
          </div>

          {/* CATEGORY GROUPS */}
          <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
            {categoriesOrder.map((cat, catIdx) => {
              const catLines = lines
                .map((l, idx) => ({ l, idx }))
                .filter(({ l }) => (l.categoryName || "Mains") === cat);
              return (
                <div
                  key={cat}
                  className="sme-cat-group"
                  onDragOver={(e) => e.preventDefault()}
                  onDrop={() => {
                    if (!dragDishId) return;
                    const dish = ws.libraryItems.find((d) => d.id === dragDishId);
                    if (dish) addDishToCategory(dish, cat);
                    setDragDishId(null);
                  }}
                >
                  <div className="sme-cat-header">
                    <strong style={{ color: "#12355b" }}>{cat} ({catLines.length})</strong>
                    <div style={{ display: "flex", gap: "0.3rem" }}>
                      <button type="button" className="sila-btn sila-btn--secondary" onClick={() => moveCategory(catIdx, -1)} disabled={catIdx === 0}>↑</button>
                      <button type="button" className="sila-btn sila-btn--secondary" onClick={() => moveCategory(catIdx, 1)} disabled={catIdx === categoriesOrder.length - 1}>↓</button>
                    </div>
                  </div>
                  {catLines.length === 0 ? (
                    <div className="sme-muted" style={{ padding: "0.45rem", border: "1px dashed #cbd5e1", borderRadius: "0.5rem" }}>
                      Drop a dish here or click + Add in Library.
                    </div>
                  ) : (
                    catLines.map(({ l, idx }) => (
                      <div key={`${l.menuItemId}-${idx}`} className="sme-canvas-line">
                        <div>
                          <strong style={{ fontSize: "0.85rem" }}>{l.menuItemName}</strong>
                          <div className="sme-muted">
                            {l.recipeName ? `Recipe: ${l.recipeName}` : "No recipe"} • {l.availabilityStatus}
                            {l.allergenNotes ? ` • Allergens: ${l.allergenNotes}` : ""}
                          </div>
                          <input
                            className="sila-input"
                            style={{ marginTop: "0.25rem", fontSize: "0.75rem", padding: "0.2rem 0.4rem" }}
                            placeholder="Notes / alternatives…"
                            value={l.lineNotes ?? ""}
                            onChange={(e) => setLines((prev) => prev.map((x, i) => (i === idx ? { ...x, lineNotes: e.target.value } : x)))}
                          />
                          {operatingMode !== "MANUAL" && (
                            <div className="sme-ai-item-actions">
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "Optimize", `Optimize portion for ${l.menuItemName}`)}>Optimize portion</button>
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "FindAlternatives", `Suggest alternative for ${l.menuItemName}`)}>Suggest alternative</button>
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "ShowSupportingEvidence", `Explain cost of ${l.menuItemName}`)}>Explain cost</button>
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "PredictNextPeriod", `Predict popularity of ${l.menuItemName}`)}>Predict popularity</button>
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "AnalyzeThis", `Analyze margin of ${l.menuItemName}`)}>Analyze margin</button>
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "RecommendImprovements", `Check availability for ${l.menuItemName}`)}>Check availability</button>
                              <button type="button" className="sme-ai-mini-btn" onClick={() => void invokeDishAi(l.menuItemId, l.menuItemName, "CompareWithHistory", `Compare historical performance of ${l.menuItemName}`)}>Compare history</button>
                            </div>
                          )}
                        </div>
                        <label className="sila-field">
                          <span>Portion</span>
                          <input
                            className="sila-input"
                            type="number"
                            step="0.1"
                            value={l.portionSize ?? 1}
                            onChange={(e) => setLines((prev) => prev.map((x, i) => (i === idx ? { ...x, portionSize: Number(e.target.value) || 1 } : x)))}
                          />
                        </label>
                        <label className="sila-field">
                          <span>Part. Rate</span>
                          <input
                            className="sila-input"
                            type="number"
                            step="0.05"
                            value={l.participationRate ?? 0.5}
                            onChange={(e) => setLines((prev) => prev.map((x, i) => (i === idx ? { ...x, participationRate: Number(e.target.value) || 0.1 } : x)))}
                          />
                        </label>
                        <label className="sila-field">
                          <span>Sell Price</span>
                          <input
                            className="sila-input"
                            type="number"
                            step="0.5"
                            value={l.sellingPrice ?? 0}
                            onChange={(e) => setLines((prev) => prev.map((x, i) => (i === idx ? { ...x, sellingPrice: Number(e.target.value) || 0 } : x)))}
                          />
                        </label>
                        <div className="sme-muted">
                          <div>Portions: <strong>{Math.round(liveCalc.covers * (l.participationRate ?? 0.5))}</strong></div>
                          {viewCost && <div>Cost: {num(l.portionCost)}</div>}
                        </div>
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setLines((prev) => prev.filter((_, i) => i !== idx))}>
                          Remove
                        </button>
                      </div>
                    ))
                  )}
                </div>
              );
            })}
          </div>
        </section>

        {/* RIGHT PANEL: LIVE INTELLIGENCE */}
        <aside className="sme-panel sme-studio-right">
          <div className="sme-panel__header">
            <h2 className="sme-panel__title">Live Intelligence</h2>
            <span className="sme-chip sme-chip--ok">Real-Time</span>
          </div>
          <div className="sme-kpi-mini">
            {viewCost && (
              <div className="sme-kpi-box">
                <div className="sme-kpi-box__label">Total Menu Cost</div>
                <div className="sme-kpi-box__val">{num(liveCalc.totalCost)}</div>
              </div>
            )}
            {viewCost && (
              <div className="sme-kpi-box">
                <div className="sme-kpi-box__label">Cost / Guest</div>
                <div className="sme-kpi-box__val">{num(liveCalc.costPerGuest)}</div>
              </div>
            )}
            <div className="sme-kpi-box">
              <div className="sme-kpi-box__label">Expected Revenue</div>
              <div className="sme-kpi-box__val">{num(liveCalc.totalRev)}</div>
            </div>
            {viewCost && (
              <div className="sme-kpi-box">
                <div className="sme-kpi-box__label">Food Cost %</div>
                <div className="sme-kpi-box__val">{pct(liveCalc.fcPct)}</div>
              </div>
            )}
            {viewMargin && (
              <div className="sme-kpi-box">
                <div className="sme-kpi-box__label">Contrib. Margin</div>
                <div className="sme-kpi-box__val">{num(liveCalc.cm)}</div>
              </div>
            )}
            <div className="sme-kpi-box">
              <div className="sme-kpi-box__label">Ing. Readiness</div>
              <div className="sme-kpi-box__val">{pct(ws.liveIntelligence.ingredientReadinessPercent)}</div>
            </div>
            <div className="sme-kpi-box">
              <div className="sme-kpi-box__label">Purchase Req.</div>
              <div className="sme-kpi-box__val">{num(ws.liveIntelligence.totalPurchaseRequirementValue)}</div>
            </div>
            <div className="sme-kpi-box">
              <div className="sme-kpi-box__label">Expected Waste</div>
              <div className="sme-kpi-box__val">{num(ws.liveIntelligence.expectedWasteQty, 1)}</div>
            </div>
          </div>

          {ws.liveIntelligence.riskIndicators.length > 0 && (
            <div className="sme-cat-group">
              <strong style={{ fontSize: "0.82rem", color: "#991b1b" }}>Risk Indicators</strong>
              {ws.liveIntelligence.riskIndicators.map((r, i) => (
                <div key={i} className="sme-muted" style={{ color: "#b91c1c" }}>• {r}</div>
              ))}
            </div>
          )}

          <div className="sme-cat-group">
            <strong style={{ fontSize: "0.82rem", color: "#12355b" }}>AI Recommendations</strong>
            {ws.liveIntelligence.aiRecommendations.slice(0, 3).map((rec, i) => (
              <div key={i} style={{ fontSize: "0.78rem", borderBottom: "1px solid #eef2f7", paddingBottom: "0.35rem" }}>
                <strong>{rec.strategy}</strong>: {rec.recommendation}
              </div>
            ))}
          </div>
        </aside>
      </div>

      {/* BOTTOM EXPANDABLE 10-TAB WORKBENCH */}
      <div className="sme-panel">
        <div className="sme-panel__header">
          <h2 className="sme-panel__title">ME Studio Deep Workbench (Shared Active Menu Context)</h2>
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setBottomExpanded((v) => !v)}>
            {bottomExpanded ? "Collapse" : "Expand"}
          </button>
        </div>

        <div className="sme-tabs">
          {(
            [
              ["DESIGN", "1. Design"],
              ["COSTING", "2. Live Costing"],
              ["PORTION_DEMAND", "3. Portion & Demand"],
              ["PERFORMANCE", "4. Menu Performance"],
              ["READINESS", "5. Ingredient Readiness"],
              ["HISTORICAL", "6. Historical Intelligence"],
              ["AI_OPTIMIZATION", "7. AI Optimization"],
              ["WHAT_IF", "8. What-If Simulation"],
              ["COMPARE", "9. Compare Scenarios"],
              ["APPROVAL", "10. Approval & Publish"],
            ] as [StudioTab, string][]
          ).map(([tabKey, label]) => (
            <button
              key={tabKey}
              type="button"
              className={`sme-tab ${activeTab === tabKey ? "active" : ""}`}
              onClick={async () => {
                setActiveTab(tabKey);
                setBottomExpanded(true);
                if (tabKey === "AI_OPTIMIZATION" && !aiData) {
                  try {
                    setAiData(await getMeAiIntelligence(selectedPlanId || undefined, propertyId || undefined));
                  } catch {
                    // handled gracefully
                  }
                }
              }}
            >
              {label}
            </button>
          ))}
        </div>

        {bottomExpanded && (
          <div style={{ paddingTop: "0.5rem" }}>
            {activeTab === "DESIGN" && (
              <div className="sme-muted">
                Active menu <strong>{draftName}</strong> ({draftType}, {draftCovers} covers, {lines.length} dishes). Use the 3-panel canvas above to compose dishes, adjust portions/prices, and save draft or controlled new version.
              </div>
            )}

            {activeTab === "COSTING" && (
              <table className="sila-table">
                <thead>
                  <tr>
                    <th>Dish</th>
                    <th>Category</th>
                    <th>Portions</th>
                    <th>Sell Price</th>
                    {viewCost && <th>Unit Cost</th>}
                    {viewCost && <th>Total Cost</th>}
                    <th>Exp. Revenue</th>
                    {viewCost && <th>Food Cost %</th>}
                    {viewMargin && <th>Contrib. Margin</th>}
                  </tr>
                </thead>
                <tbody>
                  {lines.map((l, idx) => (
                    <tr key={idx}>
                      <td>{l.menuItemName}</td>
                      <td>{l.categoryName}</td>
                      <td>{Math.round(liveCalc.covers * (l.participationRate ?? 0.5))}</td>
                      <td>{num(l.sellingPrice)}</td>
                      {viewCost && <td>{num(l.portionCost)}</td>}
                      {viewCost && <td>{num((l.portionCost ?? 0) * liveCalc.covers * (l.participationRate ?? 0.5))}</td>}
                      <td>{num((l.sellingPrice ?? 0) * liveCalc.covers * (l.participationRate ?? 0.5))}</td>
                      {viewCost && <td>{pct(l.foodCostPercent)}</td>}
                      {viewMargin && <td>{num(l.contributionMargin)}</td>}
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === "PORTION_DEMAND" && (
              <div>
                <div className="sme-row" style={{ marginBottom: "0.5rem" }}>
                  <span className="sme-muted">Buffet & service portion math: Expected Portions = Covers × Participation × Servings/Guest.</span>
                  <div style={{ display: "flex", gap: "0.5rem" }}>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate("silaMenuPortions")}>Open Portion Planning</button>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate("silaMenuDemand")}>Open Demand Planning</button>
                  </div>
                </div>
                <table className="sila-table">
                  <thead>
                    <tr><th>Dish</th><th>Covers</th><th>Participation</th><th>Portion Size</th><th>Expected Portions</th><th>Usable Output</th></tr>
                  </thead>
                  <tbody>
                    {lines.map((l, idx) => {
                      const ep = Math.round(liveCalc.covers * (l.participationRate ?? 0.5) * (l.expectedServingsPerGuest ?? 1));
                      return (
                        <tr key={idx}>
                          <td>{l.menuItemName}</td>
                          <td>{liveCalc.covers}</td>
                          <td>{pct((l.participationRate ?? 0.5) * 100)}</td>
                          <td>{l.portionSize ?? 1} {l.portionUom || "PORTION"}</td>
                          <td>{ep}</td>
                          <td>{(ep * (l.portionSize ?? 1)).toFixed(2)} {l.portionUom || "PORTION"}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}

            {activeTab === "PERFORMANCE" && ws.performanceMatrix && (
              <div>
                <div className="sme-quadrant-grid">
                  <div className="sme-quadrant sme-quadrant--star"><strong>STARS ({ws.performanceMatrix.stars})</strong><div className="sme-muted">High popularity & high margin</div></div>
                  <div className="sme-quadrant sme-quadrant--plow"><strong>PLOWHORSES ({ws.performanceMatrix.plowhorses})</strong><div className="sme-muted">High popularity, lower margin</div></div>
                  <div className="sme-quadrant sme-quadrant--puzzle"><strong>PUZZLES ({ws.performanceMatrix.puzzles})</strong><div className="sme-muted">Low popularity, high margin</div></div>
                  <div className="sme-quadrant sme-quadrant--dog"><strong>DOGS ({ws.performanceMatrix.dogs})</strong><div className="sme-muted">Low popularity & low margin</div></div>
                </div>
                <div className="sme-muted" style={{ marginTop: "0.5rem" }}>
                  Excluded incomplete/new items: {ws.performanceMatrix.excludedCount} • Basis: {ws.performanceMatrix.costBasis}
                </div>
              </div>
            )}

            {activeTab === "READINESS" && (
              <div>
                <div className="sme-row" style={{ marginBottom: "0.5rem" }}>
                  <span className="sme-muted">Time-phased against required service date; late open POs excluded.</span>
                  {perms.generatePurchaseRecommendation && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--primary"
                      onClick={async () => {
                        try {
                          const rec = await createMeDraftPurchaseRec({ menuPlanId: selectedPlanId || undefined, propertyId: propertyId || undefined });
                          toastService.success(`Generated ${rec.items.length} draft purchase recommendation(s).`);
                        } catch (e) {
                          toastService.error(e instanceof Error ? e.message : "Could not generate draft recommendation.");
                        }
                      }}
                    >
                      Generate Draft Purchase Recommendation
                    </button>
                  )}
                </div>
                <table className="sila-table">
                  <thead>
                    <tr><th>Ingredient</th><th>UOM</th><th>Required</th><th>Available</th><th>Open PO</th><th>Shortage</th><th>Status</th></tr>
                  </thead>
                  <tbody>
                    {ws.ingredientRequirements.map((r, i) => (
                      <tr key={i}>
                        <td>{r.materialName}</td>
                        <td>{r.uom}</td>
                        <td>{r.requirement}</td>
                        <td>{r.available}</td>
                        <td>{r.openPo}</td>
                        <td><strong>{r.shortage}</strong></td>
                        <td>{r.shortage > 0 ? "SHORTAGE" : "READY"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}

            {activeTab === "HISTORICAL" && (
              <table className="sila-table">
                <thead>
                  <tr><th>Historical Menu</th><th>Type</th><th>Date</th><th>Guests</th><th>Actual Sales</th>{viewCost && <th>Current Recost</th>}<th>Action</th></tr>
                </thead>
                <tbody>
                  {ws.historicalMenus.map((h) => (
                    <tr key={h.sourceId}>
                      <td>{h.name} (v{h.versionNumber})</td>
                      <td>{h.eventType}</td>
                      <td>{h.businessDate.slice(0, 10)}</td>
                      <td>{h.guestCount}</td>
                      <td>{num(h.actualSales)}</td>
                      {viewCost && <td>{num(h.recalculatedCurrentCost)} ({pct(h.recalculatedCurrentFoodCostPercent)})</td>}
                      <td>
                        <button
                          type="button"
                          className="sila-btn sila-btn--secondary"
                          onClick={async () => {
                            const created = await createMePlanFromHistorical(h.sourceId, h.sourceKind, propertyId || undefined);
                            toastService.success(`Copied historical menu into draft '${created.name}' at current prices.`);
                            await loadWorkspace(created.id);
                          }}
                        >
                          Copy to Draft
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}

            {activeTab === "AI_OPTIMIZATION" && (
              <div className="sme-list">
                <div className="sme-muted">
                  Mode: <strong>{aiData?.providerMode ?? "LOCAL_DETERMINISTIC"}</strong> {aiData?.fallbackNotice ? `• ${aiData.fallbackNotice}` : ""}
                </div>
                {(aiData?.recommendations ?? ws.liveIntelligence.aiRecommendations).map((r, i) => (
                  <div key={i} className="sme-kpi-box">
                    <div className="sme-row">
                      <strong>{r.strategy} — {r.recommendation}</strong>
                      <span className="sme-chip">Confidence {(r.confidence * 100).toFixed(0)}%</span>
                    </div>
                    <div className="sme-muted">Evidence: {r.supportingEvidence}</div>
                    <div className="sme-muted">Baseline: {r.currentBaseline} → Proposed: {r.proposedChange}</div>
                    <div className="sme-muted">
                      Operational: {r.operationalImpact} • Allergens: {r.allergenImpact ?? "Unchanged"} • Approval: {r.requiresApproval ? "Required" : "Optional"}
                    </div>
                  </div>
                ))}
              </div>
            )}

            {activeTab === "WHAT_IF" && (
              <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
                {operatingMode !== "MANUAL" && (
                  <div className="sme-kpi-box" style={{ background: "#f0f7ff" }}>
                    <div className="sme-row">
                      <input
                        className="sila-input"
                        style={{ flex: 1 }}
                        value={nlQuery}
                        onChange={(e) => setNlQuery(e.target.value)}
                        placeholder="Natural language What-If (e.g. 'What happens if salmon cost increases by 20%?')"
                      />
                      <button
                        type="button"
                        className="sila-btn sila-btn--primary"
                        onClick={async () => {
                          try {
                            const r = await runMeNlSimulation({
                              command: nlQuery,
                              menuPlanId: selectedPlanId || undefined,
                              propertyId,
                            });
                            setNlSimResult(r);
                          } catch (e) {
                            toastService.error(e instanceof Error ? e.message : "NL simulation failed.");
                          }
                        }}
                      >
                        Run NL Simulation
                      </button>
                    </div>
                    {nlSimResult && (
                      <div style={{ marginTop: "0.5rem" }}>
                        <AiValueTag kind="SIMULATION" /> <strong>{nlSimResult.parsedIntent}</strong>
                        <div className="sme-muted">{nlSimResult.sanitizedCommand}</div>
                        <div className="sme-muted">
                          Baseline Cost: {num(nlSimResult.simulation.baselineCost)} → Simulated: {num(nlSimResult.simulation.foodCost)} ·{" "}
                          Margin: {num(nlSimResult.simulation.baselineMargin)} → {num(nlSimResult.simulation.contributionMargin)} ·{" "}
                          Live Data Modified: {nlSimResult.liveDataMutated ? "Yes" : "No (Read-Only Sandbox)"}
                        </div>
                      </div>
                    )}
                  </div>
                )}
                <div className="sme-form">
                  <label className="sila-field"><span>Scenario Name</span><input className="sila-input" value={simReq.scenarioName} onChange={(e) => setSimReq({ ...simReq, scenarioName: e.target.value })} /></label>
                  <label className="sila-field"><span>Guest Count</span><input className="sila-input" type="number" value={simReq.expectedCovers} onChange={(e) => setSimReq({ ...simReq, expectedCovers: e.target.value })} /></label>
                  <label className="sila-field"><span>Ingredient Price Δ %</span><input className="sila-input" type="number" value={simReq.ingredientPriceChangePercent} onChange={(e) => setSimReq({ ...simReq, ingredientPriceChangePercent: e.target.value })} /></label>
                  <label className="sila-field"><span>Selling Price Δ %</span><input className="sila-input" type="number" value={simReq.sellingPriceChangePercent} onChange={(e) => setSimReq({ ...simReq, sellingPriceChangePercent: e.target.value })} /></label>
                  <label className="sila-field"><span>Portion Size Δ %</span><input className="sila-input" type="number" value={simReq.portionSizeChangePercent} onChange={(e) => setSimReq({ ...simReq, portionSizeChangePercent: e.target.value })} /></label>
                  <label className="sila-field"><span>Discount %</span><input className="sila-input" type="number" value={simReq.discountPercent} onChange={(e) => setSimReq({ ...simReq, discountPercent: e.target.value })} /></label>
                </div>
                <div style={{ display: "flex", gap: "0.5rem" }}>
                  <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleRunSim(false)}>Run Isolated Simulation</button>
                  <button type="button" className="sila-btn sila-btn--primary" onClick={() => handleRunSim(true)}>Save Scenario</button>
                </div>
                {simResult && (
                  <div className="sme-kpi-box">
                    <div className="sme-muted">{simResult.demandAssumptionDisclosure}</div>
                    <div className="sme-grid" style={{ marginTop: "0.5rem" }}>
                      {viewCost && <div>Baseline Cost: {num(simResult.baselineCost)} → <strong>{num(simResult.foodCost)}</strong> ({simResult.costVariancePct}%)</div>}
                      <div>Baseline Rev: {num(simResult.baselineRevenue)} → <strong>{num(simResult.revenue)}</strong> ({simResult.revenueVariancePct}%)</div>
                      {viewMargin && <div>Baseline Margin: {num(simResult.baselineMargin)} → <strong>{num(simResult.contributionMargin)}</strong> ({simResult.marginVariancePct}%)</div>}
                      <div>Requirement: {simResult.ingredientRequirement} • Waste Qty: {simResult.expectedWasteQty}</div>
                    </div>
                  </div>
                )}
              </div>
            )}

            {activeTab === "COMPARE" && (
              <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
                <div className="sme-row">
                  <span className="sme-muted">Select 2+ scenarios or menus to compare side-by-side on identical calculation basis.</span>
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    onClick={async () => {
                      const res = await compareMeScenarios(compareIds);
                      setCompareResult(res);
                    }}
                  >
                    Compare Selected ({compareIds.length || "All"})
                  </button>
                </div>
                <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
                  {ws.scenarios.map((s) => (
                    <label key={s.id} className="sme-chip" style={{ cursor: "pointer" }}>
                      <input
                        type="checkbox"
                        checked={compareIds.includes(s.id)}
                        onChange={(e) => setCompareIds((prev) => (e.target.checked ? [...prev, s.id] : prev.filter((x) => x !== s.id)))}
                      />
                      {s.scenarioName} {s.isPreferred ? "★" : ""}
                    </label>
                  ))}
                </div>
                {compareResult && (
                  <table className="sila-table">
                    <thead>
                      <tr><th>Scenario</th><th>Covers</th><th>Items</th><th>Revenue</th>{viewCost && <th>Food Cost</th>}{viewCost && <th>FC %</th>}{viewMargin && <th>Margin</th>}<th>Readiness</th><th>Action</th></tr>
                    </thead>
                    <tbody>
                      {compareResult.scenarios.map((s) => (
                        <tr key={s.id}>
                          <td><strong>{s.scenarioName}</strong> {s.isPreferred && <span className="sme-chip sme-chip--star">Preferred</span>}</td>
                          <td>{s.expectedCovers}</td>
                          <td>{s.itemCount}</td>
                          <td>{num(s.expectedRevenue)}</td>
                          {viewCost && <td>{num(s.totalFoodCost)}</td>}
                          {viewCost && <td>{pct(s.foodCostPercent)}</td>}
                          {viewMargin && <td>{num(s.contributionMargin)}</td>}
                          <td>{pct(s.ingredientAvailabilityPercent)}</td>
                          <td style={{ display: "flex", gap: "0.35rem" }}>
                            <button type="button" className="sila-btn sila-btn--secondary" onClick={async () => { await setMePreferredScenario(s.id); await loadWorkspace(selectedPlanId || undefined); }}>Select Preferred</button>
                            <button type="button" className="sila-btn sila-btn--primary" onClick={async () => { const d = await convertMeScenarioToDraft(s.id); await loadWorkspace(d.id); }}>Convert to Draft</button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </div>
            )}

            {activeTab === "APPROVAL" && (
              <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
                <div className="sme-row">
                  <input className="sila-input" style={{ flex: 1 }} placeholder="Approval / review comment…" value={approvalComment} onChange={(e) => setApprovalComment(e.target.value)} />
                  <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleTransition("UNDER_REVIEW")}>Submit for Review</button>
                    <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleTransition("PENDING_APPROVAL")}>Request Approval</button>
                    {perms.approveMenu && (
                      <>
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleTransition("CHANGES_REQUESTED")}>Request Changes</button>
                        <button type="button" className="sila-btn sila-btn--primary" onClick={() => handleTransition("APPROVED")}>Approve</button>
                      </>
                    )}
                    {perms.publishMenu && (
                      <>
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleTransition("SCHEDULED")}>Schedule</button>
                        <button type="button" className="sila-btn sila-btn--primary" onClick={() => handleTransition("PUBLISHED")}>Publish Menu</button>
                      </>
                    )}
                    {perms.approveMenu && (
                      <>
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleTransition("SUSPENDED")}>Suspend</button>
                        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => handleTransition("ARCHIVED")}>Archive</button>
                      </>
                    )}
                  </div>
                </div>
                <table className="sila-table">
                  <thead><tr><th>Version</th><th>From</th><th>To</th><th>Role</th><th>Reason</th><th>Timestamp</th></tr></thead>
                  <tbody>
                    {ws.approvalHistory.map((h) => (
                      <tr key={h.id}>
                        <td>v{h.versionNumber}</td>
                        <td>{h.fromStatus}</td>
                        <td><strong>{h.toStatus}</strong></td>
                        <td>{h.approverRole || "—"}</td>
                        <td>{h.reason || "—"}</td>
                        <td>{new Date(h.actionAtUtc).toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default MeStudioWorkspace;
