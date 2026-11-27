import React, { useCallback, useEffect, useMemo, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  createCulinaryMenu,
  createMissingRecipeDraftFromMenuLine,
  getCulinaryMenu,
  listCulinaryMenus,
  removeCulinaryMenuLine,
  reorderCulinaryMenuLines,
  searchPublishedCulinaryRecipes,
  syncPublishedRecipeToMenuLine,
  upsertCulinaryMenuLine,
  type SilaCulinaryMenu,
  type SilaCulinaryMenuLine,
  type SilaCulinaryPublishedRecipeSearchItem,
} from "../../../../api/silaMe/silaCulinaryEventMenuApi";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface CulinaryMenuStudioWorkspaceProps {
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

const COURSES = [
  "Canapés",
  "Starters",
  "Soups",
  "Salads",
  "Main Courses",
  "Side Dishes",
  "Desserts",
  "Cold Beverages",
  "Hot Beverages",
  "Live Stations",
];

const SERVICE_STYLES = [
  "BUFFET",
  "PLATED",
  "FAMILY_STYLE",
  "LIVE_STATION",
  "COCKTAIL_RECEPTION",
  "COFFEE_BREAK",
  "CUSTOM",
];

const CulinaryMenuStudioWorkspace: React.FC<CulinaryMenuStudioWorkspaceProps> = ({
  onOpenWorkspace,
}) => {
  const [menus, setMenus] = useState<SilaCulinaryMenu[]>([]);
  const [selectedMenuId, setSelectedMenuId] = useState<string>("");
  const [activeMenu, setActiveMenu] = useState<SilaCulinaryMenu | null>(null);
  const [loading, setLoading] = useState(false);

  // Create menu form
  const [showCreate, setShowCreate] = useState(false);
  const [newName, setNewName] = useState("");
  const [newCategory, setNewCategory] = useState("Event Banquet");
  const [newCuisine, setNewCuisine] = useState("International");
  const [newStyle, setNewStyle] = useState("BUFFET");
  const [newReusable, setNewReusable] = useState(true);
  const [newTargetPrice, setNewTargetPrice] = useState("350");
  const [newTargetFc, setNewTargetFc] = useState("28");

  // Published recipe search
  const [searchQ, setSearchQ] = useState("");
  const [searchCat, setSearchCat] = useState("");
  const [searchDietary, setSearchDietary] = useState("");
  const [excludeAllergens, setExcludeAllergens] = useState("");
  const [maxCost, setMaxCost] = useState("");
  const [searchResults, setSearchResults] = useState<SilaCulinaryPublishedRecipeSearchItem[]>([]);
  const [searching, setSearching] = useState(false);

  // Selected course + quick custom/missing dish form
  const [addCourse, setAddCourse] = useState("Main Courses");
  const [customDishName, setCustomDishName] = useState("");
  const [customPortionSize, setCustomPortionSize] = useState("180");
  const [customUptake, setCustomUptake] = useState("60");
  const [customIsBeverage, setCustomIsBeverage] = useState(false);

  const loadMenus = useCallback(async (keepId?: string) => {
    setLoading(true);
    try {
      const list = await listCulinaryMenus();
      setMenus(list);
      const targetId = keepId || selectedMenuId || list[0]?.id || "";
      if (targetId) {
        setSelectedMenuId(targetId);
        const detail = await getCulinaryMenu(targetId);
        setActiveMenu(detail);
      } else {
        setActiveMenu(null);
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to load menus.");
    } finally {
      setLoading(false);
    }
  }, [selectedMenuId]);

  const runRecipeSearch = useCallback(async () => {
    setSearching(true);
    try {
      const items = await searchPublishedCulinaryRecipes({
        query: searchQ || undefined,
        category: searchCat || undefined,
        dietary: searchDietary || undefined,
        excludeAllergens: excludeAllergens || undefined,
        maxCost: maxCost ? Number(maxCost) : undefined,
      });
      setSearchResults(items);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Published recipe search failed.");
    } finally {
      setSearching(false);
    }
  }, [searchQ, searchCat, searchDietary, excludeAllergens, maxCost]);

  useEffect(() => {
    loadMenus();
    runRecipeSearch();
  }, []);

  const handleSelectMenu = async (id: string) => {
    setSelectedMenuId(id);
    if (!id) {
      setActiveMenu(null);
      return;
    }
    try {
      const detail = await getCulinaryMenu(id);
      setActiveMenu(detail);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to load menu.");
    }
  };

  const handleCreateMenu = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) {
      toastService.warning("Menu name is required.");
      return;
    }
    try {
      const created = await createCulinaryMenu({
        name: newName.trim(),
        menuCategory: newCategory,
        cuisine: newCuisine || undefined,
        serviceStyle: newStyle,
        isReusableTemplate: newReusable,
        currency: "AED",
        targetSellingPrice: newTargetPrice ? Number(newTargetPrice) : undefined,
        targetFoodCostPercent: Number(newTargetFc) || 28,
      });
      toastService.success(`Menu ${created.menuCode} created.`);
      setShowCreate(false);
      setNewName("");
      await loadMenus(created.id);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to create menu.");
    }
  };

  const handleAddPublishedRecipe = async (recipe: SilaCulinaryPublishedRecipeSearchItem) => {
    if (!activeMenu) {
      toastService.warning("Create or select a menu first.");
      return;
    }
    try {
      const updated = await upsertCulinaryMenuLine(activeMenu.id, {
        courseName: addCourse,
        itemRole: "MANDATORY",
        isLocked: false,
        recipeId: recipe.recipeId,
        recipeVersion: recipe.recipeVersion,
        dishName: recipe.name,
        serviceStyle: activeMenu.serviceStyle,
        uptakePercent: activeMenu.serviceStyle === "BUFFET" ? 60 : 100,
        servingFrequency: 1,
        portionSize: Number(customPortionSize) || 180,
        portionUom: "G",
        isBeverage: recipe.category.toLowerCase().includes("beverage"),
        servingsPerGuest: 1,
      });
      setActiveMenu(updated);
      toastService.success(`Added ${recipe.name} (v${recipe.recipeVersion}) to ${addCourse}.`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to add recipe.");
    }
  };

  const handleAddCustomDish = async () => {
    if (!activeMenu || !customDishName.trim()) {
      toastService.warning("Select a menu and enter a dish name.");
      return;
    }
    try {
      const updated = await upsertCulinaryMenuLine(activeMenu.id, {
        courseName: addCourse,
        itemRole: "MANDATORY",
        isLocked: false,
        dishName: customDishName.trim(),
        serviceStyle: activeMenu.serviceStyle,
        uptakePercent: Number(customUptake) || 100,
        servingFrequency: 1,
        portionSize: Number(customPortionSize) || 180,
        portionUom: customIsBeverage ? "ML" : "G",
        isBeverage: customIsBeverage,
        servingsPerGuest: 1,
        servingVolumeMl: customIsBeverage ? Number(customPortionSize) || 250 : undefined,
      });
      setActiveMenu(updated);
      setCustomDishName("");
      toastService.success("Added custom dish (flagged for Recipe Master drafting).");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to add dish.");
    }
  };

  const handleLinePatch = async (line: SilaCulinaryMenuLine, patch: Partial<SilaCulinaryMenuLine>) => {
    if (!activeMenu) return;
    try {
      const updated = await upsertCulinaryMenuLine(activeMenu.id, {
        lineId: line.id,
        courseName: patch.courseName ?? line.courseName,
        sortOrder: patch.sortOrder ?? line.sortOrder,
        itemRole: patch.itemRole ?? line.itemRole,
        isLocked: patch.isLocked ?? line.isLocked,
        recipeId: line.recipeId ?? undefined,
        recipeVersion: line.recipeVersion ?? undefined,
        dishName: patch.dishName ?? line.dishName,
        customerDescription: patch.customerDescription ?? line.customerDescription ?? undefined,
        serviceStyle: patch.serviceStyle ?? line.serviceStyle,
        uptakePercent: patch.uptakePercent ?? line.uptakePercent,
        servingFrequency: patch.servingFrequency ?? line.servingFrequency,
        portionSize: patch.portionSize ?? line.portionSize,
        portionUom: patch.portionUom ?? line.portionUom,
        allocatedPortions: patch.allocatedPortions ?? line.allocatedPortions ?? undefined,
        manualPortionsOverride: patch.manualPortionsOverride ?? line.manualPortionsOverride ?? undefined,
        isBeverage: line.isBeverage,
        servingsPerGuest: patch.servingsPerGuest ?? line.servingsPerGuest,
        servingVolumeMl: line.servingVolumeMl ?? undefined,
        containerSizeMl: line.containerSizeMl ?? undefined,
        liveStationName: line.liveStationName ?? undefined,
        verifiedStationCapacityPerHour: line.verifiedStationCapacityPerHour ?? undefined,
        notes: line.notes ?? undefined,
      });
      setActiveMenu(updated);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to update menu line.");
    }
  };

  const handleMoveLine = async (lineId: string, dir: -1 | 1) => {
    if (!activeMenu) return;
    const ids = activeMenu.lines.map((l) => l.id);
    const idx = ids.indexOf(lineId);
    const swap = idx + dir;
    if (idx < 0 || swap < 0 || swap >= ids.length) return;
    const next = [...ids];
    [next[idx], next[swap]] = [next[swap], next[idx]];
    try {
      const updated = await reorderCulinaryMenuLines(activeMenu.id, next);
      setActiveMenu(updated);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to reorder menu lines.");
    }
  };

  const groupedCourses = useMemo(() => {
    const map = new Map<string, SilaCulinaryMenuLine[]>();
    for (const line of activeMenu?.lines ?? []) {
      const arr = map.get(line.courseName) ?? [];
      arr.push(line);
      map.set(line.courseName, arr);
    }
    return Array.from(map.entries());
  }, [activeMenu]);

  return (
    <div className="cs2-shell" data-testid="culinary-menus-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Menu Studio — Course, Published Recipe &amp; Dietary Builder</h2>
          <p className="cs2-subtitle">
            Build reusable Banquet, Buffet, Plated &amp; Corporate menus from published Recipe Master versions with
            semantic search, allergen verification, portion sizing, and live costing.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--secondary"
            onClick={() => setShowCreate((v) => !v)}
          >
            {showCreate ? "Close New Menu" : "+ New Menu"}
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--primary"
            onClick={() => onOpenWorkspace("events")}
          >
            Open AI Event Planner
          </button>
        </div>
      </div>

      {showCreate && (
        <form className="cs2-card" onSubmit={handleCreateMenu} data-testid="menu-studio-create-form">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Create New Menu</h3>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Menu Name *</label>
              <input className="sila-input" value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="e.g. Grand International Gala Buffet" required />
            </div>
            <div className="sila-field">
              <label className="sila-label">Category</label>
              <input className="sila-input" value={newCategory} onChange={(e) => setNewCategory(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Cuisine</label>
              <input className="sila-input" value={newCuisine} onChange={(e) => setNewCuisine(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Service Style</label>
              <select className="sila-select" value={newStyle} onChange={(e) => setNewStyle(e.target.value)}>
                {SERVICE_STYLES.map((s) => (
                  <option key={s} value={s}>{s}</option>
                ))}
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label">Target Selling Price (AED)</label>
              <input type="number" className="sila-input" value={newTargetPrice} onChange={(e) => setNewTargetPrice(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Target Food Cost %</label>
              <input type="number" className="sila-input" value={newTargetFc} onChange={(e) => setNewTargetFc(e.target.value)} />
            </div>
            <div className="sila-field" style={{ display: "flex", alignItems: "center", gap: 8, paddingTop: 22 }}>
              <input id="cs2-menu-reusable" type="checkbox" checked={newReusable} onChange={(e) => setNewReusable(e.target.checked)} />
              <label htmlFor="cs2-menu-reusable">Reusable Menu Template</label>
            </div>
            <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
              <button type="submit" className="sila-btn sila-btn--sm sila-btn--primary">Create Menu</button>
            </div>
          </div>
        </form>
      )}

      <div className="cs2-card">
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label">Active Menu</label>
            <select
              className="sila-select"
              value={selectedMenuId}
              onChange={(e) => handleSelectMenu(e.target.value)}
              disabled={loading}
            >
              {menus.length === 0 ? (
                <option value="">No menus yet — click + New Menu</option>
              ) : (
                menus.map((m) => (
                  <option key={m.id} value={m.id}>
                    {m.menuCode} — {m.name} ({m.serviceStyle} · v{m.versionNumber} · {m.status})
                  </option>
                ))
              )}
            </select>
          </div>
          {activeMenu && (
            <>
              <div className="sila-field">
                <span className="sila-label">Total Estimated Cost</span>
                <div style={{ fontWeight: 700, fontSize: 16 }}>
                  {activeMenu.costVisible
                    ? `${activeMenu.currency} ${(activeMenu.totalEstimatedCost ?? 0).toLocaleString(undefined, { maximumFractionDigits: 2 })}`
                    : "Restricted"}
                </div>
              </div>
              <div className="sila-field">
                <span className="sila-label">Cost / Guest</span>
                <div style={{ fontWeight: 700, fontSize: 16 }}>
                  {activeMenu.costVisible
                    ? `${activeMenu.currency} ${(activeMenu.costPerGuest ?? 0).toFixed(2)}`
                    : "Restricted"}
                </div>
              </div>
              <div className="sila-field">
                <span className="sila-label">Dishes &amp; Status</span>
                <div style={{ display: "flex", gap: 6, flexWrap: "wrap", alignItems: "center" }}>
                  <span className="cs2-badge cs2-badge--indigo">{activeMenu.lines.length} dishes</span>
                  <span className="cs2-badge cs2-badge--live">{activeMenu.status}</span>
                  {activeMenu.isReusableTemplate && <span className="cs2-badge cs2-badge--demo">Reusable</span>}
                </div>
              </div>
            </>
          )}
        </div>
      </div>

      <div className="cs2-grid-2">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Published Recipe Master Search</h3>
            <span className="cs2-badge cs2-badge--live">Version-Pinned</span>
          </div>
          <div className="cs2-form-grid" style={{ marginBottom: 10 }}>
            <div className="sila-field">
              <label className="sila-label">Search Dish / Cuisine</label>
              <input className="sila-input" value={searchQ} onChange={(e) => setSearchQ(e.target.value)} placeholder="salmon, biryani, mousse..." />
            </div>
            <div className="sila-field">
              <label className="sila-label">Recipe Category</label>
              <input className="sila-input" value={searchCat} onChange={(e) => setSearchCat(e.target.value)} placeholder="Main Course, Dessert..." />
            </div>
            <div className="sila-field">
              <label className="sila-label">Target Course</label>
              <select className="sila-select" value={addCourse} onChange={(e) => setAddCourse(e.target.value)}>
                {COURSES.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label">Dietary Tag</label>
              <input className="sila-input" value={searchDietary} onChange={(e) => setSearchDietary(e.target.value)} placeholder="HALAL, VEGAN..." />
            </div>
            <div className="sila-field">
              <label className="sila-label">Exclude Allergens</label>
              <input className="sila-input" value={excludeAllergens} onChange={(e) => setExcludeAllergens(e.target.value)} placeholder="NUTS, SHELLFISH" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Max Cost / Portion</label>
              <input type="number" className="sila-input" value={maxCost} onChange={(e) => setMaxCost(e.target.value)} placeholder="e.g. 30" />
            </div>
            <div className="sila-field" style={{ display: "flex", alignItems: "flex-end" }}>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={runRecipeSearch} disabled={searching}>
                {searching ? "Searching..." : "Search Recipes"}
              </button>
            </div>
          </div>

          <div style={{ maxHeight: 360, overflowY: "auto", display: "grid", gap: 8 }}>
            {searchResults.length === 0 ? (
              <div style={{ color: "#64748b", fontSize: 13 }}>No published recipes matched the current filters.</div>
            ) : (
              searchResults.map((r) => (
                <div key={`${r.recipeId}-${r.recipeVersion}`} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", padding: 10, border: "1px solid #e2e8f0", borderRadius: 10, gap: 10 }}>
                  <div>
                    <div style={{ fontWeight: 600 }}>
                      {r.name} <code style={{ fontSize: 12 }}>{r.recipeCode} v{r.recipeVersion}</code>
                    </div>
                    <div style={{ fontSize: 12, color: "#475569" }}>
                      {r.category} {r.cuisine ? `· ${r.cuisine}` : ""} ·{" "}
                      {r.costPerPortion != null ? `${r.currency} ${r.costPerPortion.toFixed(2)}/portion` : "Cost unavailable"}
                      {r.dietaryTags.length > 0 ? ` · ${r.dietaryTags.join(", ")}` : ""}
                      {r.verifiedAllergens.length > 0 ? ` · Allergens: ${r.verifiedAllergens.join(", ")}` : ""}
                    </div>
                  </div>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    disabled={!activeMenu}
                    onClick={() => handleAddPublishedRecipe(r)}
                  >
                    + Add
                  </button>
                </div>
              ))
            )}
          </div>

          <hr style={{ margin: "14px 0", borderColor: "#e2e8f0" }} />
          <div className="cs2-card__head">
            <h4 className="cs2-card__title" style={{ fontSize: 14 }}>Add Custom / Missing-Recipe Dish</h4>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Dish Name</label>
              <input className="sila-input" value={customDishName} onChange={(e) => setCustomDishName(e.target.value)} placeholder="e.g. Saffron Pistachio Kulfi" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Portion ({customIsBeverage ? "ML" : "G"})</label>
              <input type="number" className="sila-input" value={customPortionSize} onChange={(e) => setCustomPortionSize(e.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label">Uptake %</label>
              <input type="number" className="sila-input" value={customUptake} onChange={(e) => setCustomUptake(e.target.value)} />
            </div>
            <div className="sila-field" style={{ display: "flex", alignItems: "flex-end", gap: 8 }}>
              <label style={{ fontSize: 12, display: "flex", alignItems: "center", gap: 4 }}>
                <input type="checkbox" checked={customIsBeverage} onChange={(e) => setCustomIsBeverage(e.target.checked)} />
                Beverage
              </label>
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={!activeMenu} onClick={handleAddCustomDish}>
                Add Missing-Recipe Line
              </button>
            </div>
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Menu Courses &amp; Line Details</h3>
            {activeMenu && <span className="cs2-badge cs2-badge--indigo">{activeMenu.menuCode}</span>}
          </div>

          {!activeMenu || activeMenu.lines.length === 0 ? (
            <div style={{ color: "#64748b", fontSize: 13 }}>
              Select or create a menu, then add published recipes or custom dishes from the left panel.
            </div>
          ) : (
            <div style={{ display: "grid", gap: 12 }}>
              {groupedCourses.map(([course, lines]) => (
                <div key={course} style={{ border: "1px solid #e2e8f0", borderRadius: 10, padding: 10 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 6 }}>
                    <strong>{course}</strong>
                    <span className="cs2-badge cs2-badge--indigo">{lines.length} items</span>
                  </div>
                  <div style={{ display: "grid", gap: 8 }}>
                    {lines.map((line) => (
                      <div key={line.id} style={{ padding: 8, background: "#f8fafc", borderRadius: 8, display: "grid", gap: 6 }}>
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, flexWrap: "wrap" }}>
                          <div>
                            <strong>{line.dishName}</strong>{" "}
                            {line.recipeCode ? (
                              <span className="cs2-badge cs2-badge--live">{line.recipeCode} v{line.recipeVersion}</span>
                            ) : (
                              <span className="cs2-badge cs2-badge--warn">Missing Recipe</span>
                            )}
                            {line.recipeVersionOutdated && (
                              <span className="cs2-badge cs2-badge--warn" style={{ marginLeft: 4 }}>
                                New v{line.latestPublishedRecipeVersion} available
                              </span>
                            )}
                          </div>
                          <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleMoveLine(line.id, -1)}>Up</button>
                            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleMoveLine(line.id, 1)}>Down</button>
                            {!line.recipeId && !line.recipeDraftId && (
                              <button
                                type="button"
                                className="sila-btn sila-btn--sm sila-btn--primary"
                                onClick={async () => {
                                  const res = await createMissingRecipeDraftFromMenuLine(activeMenu.id, line.id);
                                  toastService.success(`Created Recipe Draft (${res.draftId.slice(0, 8)}).`);
                                  await handleSelectMenu(activeMenu.id);
                                }}
                              >
                                Create Draft
                              </button>
                            )}
                            {line.recipeVersionOutdated && (
                              <button
                                type="button"
                                className="sila-btn sila-btn--sm sila-btn--secondary"
                                onClick={async () => {
                                  const updated = await syncPublishedRecipeToMenuLine(activeMenu.id, line.id);
                                  setActiveMenu(updated);
                                  toastService.success("Synced to latest published recipe version.");
                                }}
                              >
                                Sync Version
                              </button>
                            )}
                            <button
                              type="button"
                              className="sila-btn sila-btn--sm sila-btn--danger"
                              onClick={async () => {
                                const updated = await removeCulinaryMenuLine(activeMenu.id, line.id);
                                setActiveMenu(updated);
                              }}
                            >
                              Remove
                            </button>
                          </div>
                        </div>

                        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", fontSize: 12, alignItems: "center" }}>
                          <label>
                            Uptake %:{" "}
                            <input
                              type="number"
                              style={{ width: 64 }}
                              defaultValue={line.uptakePercent}
                              onBlur={(e) => handleLinePatch(line, { uptakePercent: Number(e.target.value) || 0 })}
                            />
                          </label>
                          <label>
                            Portion ({line.portionUom}):{" "}
                            <input
                              type="number"
                              style={{ width: 68 }}
                              defaultValue={line.portionSize}
                              onBlur={(e) => handleLinePatch(line, { portionSize: Number(e.target.value) || 1 })}
                            />
                          </label>
                          <span>Planned Portions: <strong>{line.plannedPortions}</strong></span>
                          {line.costVisible && (
                            <span>
                              Est. Cost:{" "}
                              <strong>
                                {line.estimatedLineCost != null
                                  ? `${activeMenu.currency} ${line.estimatedLineCost.toFixed(2)}`
                                  : "Cost unavailable"}
                              </strong>
                            </span>
                          )}
                          <label style={{ display: "flex", alignItems: "center", gap: 4 }}>
                            <input
                              type="checkbox"
                              checked={line.isLocked}
                              onChange={(e) => handleLinePatch(line, { isLocked: e.target.checked })}
                            />
                            Lock
                          </label>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
};

export default CulinaryMenuStudioWorkspace;
