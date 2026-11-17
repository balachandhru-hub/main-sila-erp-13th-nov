import React, { useEffect, useMemo, useState } from "react";
import {
  getLocations,
  getMyLocations,
  type SilaLocation,
} from "../../../api/silaMe/silaInventoryApi";
import {
  getRecipeMasters,
  searchIngredients,
  type SilaIngredientOption,
  type SilaRecipeMaster,
} from "../../../api/silaMe/silaRecipeToolsApi";
import {
  duplicateLine,
  materialLine,
  rememberMaterial,
  replaceMaterialOnLine,
  costPerPortion as draftCostPerPortion,
} from "./recipeBuilderState";
import {
  lineCost,
  toNumber,
  totalCostAt,
  type IngredientLine,
} from "./recipeEditorModel";
import { formatCost, quantityForUomChange } from "./recipeFormat";
import {
  loadTargetFoodCostPercent,
  recommendMaterials,
  saveTargetFoodCostPercent,
  suggestClassification,
  suggestSellingPrice,
} from "./recipeStudioSuggestions";
import SilaRecipeCanvas from "./SilaRecipeCanvas";
import SilaRecipeMaterialLibrary from "./SilaRecipeMaterialLibrary";
import { RECIPE_WORKSPACE_BACK_LABEL } from "./recipeWorkspaceShellCopy";
import "./SilaRecipeStudioPresentation.css";

export type RecipeStudioMode = "new" | "edit";

interface SilaRecipeStudioPresentationProps {
  mode: RecipeStudioMode;
  /** Route id in Edit mode only; recipe fetch is deferred to Step 6. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
}

/**
 * Step 4 Recipe Studio presentation.
 * Connects Material Catalog search, local ingredient draft, UOM, drag/drop and draft costing.
 * Does not call Recipe create/update/get APIs. Save/Submit remain disabled.
 */
const SilaRecipeStudioPresentation: React.FC<SilaRecipeStudioPresentationProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
}) => {
  const isEdit = mode === "edit";
  const idText = (recipeId ?? "").trim();

  const [name, setName] = useState("");
  const [familyId, setFamilyId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [cuisine, setCuisine] = useState("");
  const [mealPeriod, setMealPeriod] = useState("");
  const [yieldQty, setYieldQty] = useState("1");
  const [prepTime, setPrepTime] = useState("");
  const [targetFoodCost, setTargetFoodCost] = useState(() => String(loadTargetFoodCostPercent()));
  const [classConfirmed, setClassConfirmed] = useState(false);
  const [menuItem, setMenuItem] = useState("");
  const [outletNote, setOutletNote] = useState("");

  const [lines, setLines] = useState<IngredientLine[]>([]);
  const [replaceKey, setReplaceKey] = useState<string | null>(null);
  const [outlets, setOutlets] = useState<SilaLocation[]>([]);
  const [outletLocationId, setOutletLocationId] = useState("");
  const [families, setFamilies] = useState<SilaRecipeMaster[]>([]);
  const [categories, setCategories] = useState<SilaRecipeMaster[]>([]);
  const [catalogPool, setCatalogPool] = useState<SilaIngredientOption[]>([]);
  const [helperNotice, setHelperNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const boot = async () => {
      try {
        const [allLocations, myLocations, loadedFamilies, loadedCategories, browse] = await Promise.all([
          getLocations().catch(() => [] as SilaLocation[]),
          getMyLocations()
            .then((items) => ({ ok: true as const, items }))
            .catch(() => ({ ok: false as const, items: [] as SilaLocation[] })),
          getRecipeMasters("families").catch(() => [] as SilaRecipeMaster[]),
          getRecipeMasters("categories").catch(() => [] as SilaRecipeMaster[]),
          searchIngredients({ search: "", limit: 60 }).catch(() => ({
            items: [] as SilaIngredientOption[],
            total: 0,
            index: 0,
            limit: 60,
          })),
        ]);
        if (!active) return;
        const scoped = (myLocations.ok ? myLocations.items : allLocations)
          .filter((location) => location.locationType === "OUTLET");
        const sorted = scoped.length > 0
          ? scoped
          : allLocations.filter((location) => location.locationType === "OUTLET");
        setOutlets(sorted.sort((a, b) => a.locationName.localeCompare(b.locationName)));
        setFamilies(loadedFamilies);
        setCategories(loadedCategories);
        setCatalogPool(browse.items);
        setHelperNotice(null);
      } catch (err: unknown) {
        if (active) {
          setHelperNotice(err instanceof Error ? err.message : "Could not load Material Catalog helpers.");
        }
      }
    };
    void boot();
    return () => {
      active = false;
    };
  }, []);

  const costs = useMemo(() => lines.map((line) => lineCost(line, outletLocationId)), [lines, outletLocationId]);
  const totalCost = useMemo(() => totalCostAt(lines, outletLocationId), [lines, outletLocationId]);
  const currency = lines.find((line) => line.material?.currency)?.material?.currency ?? "";
  const portions = toNumber(yieldQty);
  const targetPercent = (() => {
    const value = Number(targetFoodCost);
    return Number.isFinite(value) && value > 0 && value < 100 ? value : 30;
  })();
  const pricing = useMemo(
    () => suggestSellingPrice(totalCost, portions > 0 ? portions : 1, targetPercent, costs),
    [totalCost, portions, targetPercent, costs],
  );
  const portionCost = draftCostPerPortion(totalCost, portions > 0 ? portions : 1);
  const materialRecs = useMemo(
    () => recommendMaterials(lines, [], catalogPool, 24),
    [lines, catalogPool],
  );
  const classification = useMemo(
    () => suggestClassification(lines, families, categories),
    [lines, families, categories],
  );
  const addedIds = useMemo(
    () => lines.map((line) => line.material?.id).filter((id): id is string => Boolean(id)),
    [lines],
  );
  const selectedOutlet = outlets.find((outlet) => outlet.id === outletLocationId) ?? null;
  const heading = isEdit ? "Edit Recipe" : "Create New Recipe";
  const familyName = families.find((item) => item.id === familyId)?.name ?? "";
  const categoryName = categories.find((item) => item.id === categoryId)?.name ?? "";

  const commitLines = (next: IngredientLine[] | ((current: IngredientLine[]) => IngredientLine[])) => {
    setLines((current) => (typeof next === "function" ? next(current) : next));
  };

  const addMaterial = (option: SilaIngredientOption) => {
    rememberMaterial(option.id);
    const line = materialLine(option);
    commitLines((current) => (current.some((x) => x.material?.id === line.material?.id) ? current : [...current, line]));
  };

  const addMaterials = (options: SilaIngredientOption[]) => {
    options.forEach((option) => rememberMaterial(option.id));
    commitLines((current) => {
      const next = [...current];
      options.forEach((option) => {
        if (!next.some((x) => x.material?.id === option.id)) next.push(materialLine(option));
      });
      return next;
    });
  };

  const changeLine = (key: string, change: Partial<IngredientLine>) => {
    commitLines((current) =>
      current.map((line) => {
        if (line.key !== key) return line;
        if (change.uom != null && line.material && change.uom !== line.uom) {
          const qty = toNumber(change.quantity ?? line.quantity);
          const converted = quantityForUomChange(line.material, qty, line.uom, change.uom);
          const qtyText = Number.isInteger(converted.quantity)
            ? String(converted.quantity)
            : String(Number(converted.quantity.toFixed(4)));
          return {
            ...line,
            ...change,
            quantity: change.quantity ?? (converted.convertible ? qtyText : line.quantity),
            uom: change.uom,
          };
        }
        return { ...line, ...change };
      }),
    );
  };

  const moveLine = (key: string, direction: -1 | 1) => {
    commitLines((current) => {
      const index = current.findIndex((line) => line.key === key);
      const target = index + direction;
      if (index < 0 || target < 0 || target >= current.length) return current;
      const next = [...current];
      const [row] = next.splice(index, 1);
      next.splice(target, 0, row);
      return next;
    });
  };

  const applyClassificationSuggestion = () => {
    if (!classification) return;
    setFamilyId(classification.familyId || familyId);
    setCategoryId(classification.categoryId || categoryId);
    if (classification.cuisine) setCuisine(classification.cuisine);
    if (classification.mealPeriod) setMealPeriod(classification.mealPeriod);
    setClassConfirmed(true);
  };

  const costLabel = (value: number | null | undefined): string =>
    value == null ? "COST PENDING" : formatCost(value, currency);

  return (
    <div className="rstudio-build" data-testid="recipe-studio-presentation" data-mode={mode}>
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={onBackToCatalog}>
        ← {RECIPE_WORKSPACE_BACK_LABEL}
      </button>

      <header className="rstudio-build__top">
        <div>
          <p className="rstudio-kicker">SILA Recipe Studio · Step 4 of 7</p>
          <h1>{heading}</h1>
          <p className="rstudio-build__top-meta">
            {isEdit
              ? "Same Studio canvas as Create. Existing recipe loading connects in Step 6 — no fabricated recipe details."
              : "Local ingredient draft with live Material Catalog. No recipe row until Save Draft in Step 5."}
          </p>
          <div className="rstudio-build__badges">
            <span className="rstudio-build__badge rstudio-build__badge--draft">Draft</span>
            <span className="rstudio-build__badge rstudio-build__badge--mode">{isEdit ? "Edit mode" : "Create mode"}</span>
            <span className="rstudio-build__badge rstudio-build__badge--step">Catalog live</span>
            {isEdit && (
              <span className="rstudio-build__badge rstudio-build__badge--mode" data-testid="recipe-studio-edit-id">
                ID {idText || "—"}
              </span>
            )}
          </div>
          {isEdit && (
            <p className="rstudio-build__top-meta" data-testid="recipe-studio-edit-pending">
              Awaiting existing-recipe loading in Step 6. Canvas below is a local draft only.
            </p>
          )}
        </div>
      </header>

      {helperNotice && (
        <div className="sila-alert sila-alert--warning" role="status">{helperNotice}</div>
      )}

      <div className="rstudio-build__panels">
        {/* LEFT — My Recipe */}
        <section className="rstudio-panel rstudio-panel--recipe" aria-label="My Recipe">
          <div>
            <h2 className="rstudio-panel__title">My Recipe</h2>
            <p className="rstudio-panel__hint">
              Ingredient-first canvas. Add from the catalog, edit quantity/UOM, and preview draft costs locally.
            </p>
          </div>

          <div className="rstudio-section rstudio-section--sky">
            <p className="rstudio-section__label">Recipe header</p>
            <div className="rstudio-header-grid">
              <div className="rstudio-fields">
                <div className="rstudio-field rstudio-field--wide">
                  <label htmlFor="rstudio-name">Recipe name</label>
                  <input
                    id="rstudio-name"
                    value={name}
                    placeholder="e.g. House citrus spritz"
                    onChange={(e) => setName(e.target.value)}
                    maxLength={200}
                  />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-code">Recipe code</label>
                  <input id="rstudio-code" value={isEdit ? "Loaded in Step 6" : "Assigned on save"} disabled readOnly />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-family">Family</label>
                  <select
                    id="rstudio-family"
                    value={familyId}
                    onChange={(e) => {
                      setFamilyId(e.target.value);
                      setClassConfirmed(true);
                    }}
                  >
                    <option value="">Select family</option>
                    {families.map((item) => (
                      <option key={item.id} value={item.id}>{item.name}</option>
                    ))}
                  </select>
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-category">Category</label>
                  <select
                    id="rstudio-category"
                    value={categoryId}
                    onChange={(e) => {
                      setCategoryId(e.target.value);
                      setClassConfirmed(true);
                    }}
                  >
                    <option value="">Select category</option>
                    {categories.map((item) => (
                      <option key={item.id} value={item.id}>{item.name}</option>
                    ))}
                  </select>
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-cuisine">Cuisine</label>
                  <input id="rstudio-cuisine" value={cuisine} placeholder="Optional" onChange={(e) => setCuisine(e.target.value)} />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-meal">Meal period</label>
                  <input id="rstudio-meal" value={mealPeriod} placeholder="Optional" onChange={(e) => setMealPeriod(e.target.value)} />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-yield">Yield / portions</label>
                  <input id="rstudio-yield" type="number" min="0" step="any" value={yieldQty} onChange={(e) => setYieldQty(e.target.value)} />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-prep">Preparation time</label>
                  <input id="rstudio-prep" value={prepTime} placeholder="e.g. 12 min" onChange={(e) => setPrepTime(e.target.value)} />
                </div>
              </div>
              <div className="rstudio-image-ph" aria-hidden="true">
                Recipe image
                <br />
                placeholder
              </div>
            </div>
            {classification && !classConfirmed && (
              <div className="rstudio-suggest" role="status">
                <span>
                  Suggested classification: {classification.familyName} / {classification.categoryName}
                  {classification.mealPeriod ? ` · ${classification.mealPeriod}` : ""} — {classification.reason}
                </span>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={applyClassificationSuggestion}>
                  Apply suggestion
                </button>
              </div>
            )}
            {(familyName || categoryName) && classConfirmed && (
              <p className="rstudio-panel__hint">Classification confirmed: {[familyName, categoryName].filter(Boolean).join(" / ")}</p>
            )}
          </div>

          <div className="rstudio-section rstudio-section--canvas">
            <p className="rstudio-section__label">Ingredient canvas</p>
            <SilaRecipeCanvas
              lines={lines}
              costs={costs}
              mode="RECIPE"
              currency={currency}
              onReorder={commitLines}
              onChange={changeLine}
              onRemove={(key) => commitLines((current) => current.filter((line) => line.key !== key))}
              onDuplicate={(key) =>
                commitLines((current) => {
                  const index = current.findIndex((line) => line.key === key);
                  if (index < 0) return current;
                  const copy = duplicateLine(current[index]);
                  const next = [...current];
                  next.splice(index + 1, 0, copy);
                  return next;
                })
              }
              onReplace={setReplaceKey}
              onAddMaterial={addMaterial}
              onReplaceMaterial={(key, material) => {
                rememberMaterial(material.id);
                setReplaceKey(null);
                commitLines((current) =>
                  current.map((line) =>
                    line.key === key ? replaceMaterialOnLine(line, material, line.quantity, material.baseUom) : line,
                  ),
                );
              }}
              onMove={moveLine}
            />
          </div>

          <div className="rstudio-section rstudio-section--amber">
            <p className="rstudio-section__label">Cost summary · draft preview</p>
            <div className="rstudio-cost-grid">
              <div className={`rstudio-cost-tile${totalCost == null ? " rstudio-cost-tile--pending" : ""}`}>
                <span>Total recipe cost</span>
                <strong>{costLabel(totalCost)}</strong>
              </div>
              <div className={`rstudio-cost-tile${portionCost == null ? " rstudio-cost-tile--pending" : ""}`}>
                <span>Cost per portion</span>
                <strong>{costLabel(portionCost)}</strong>
              </div>
              <div className="rstudio-cost-tile">
                <span>Target food cost %</span>
                <strong>
                  <input
                    aria-label="Target food cost percent"
                    value={targetFoodCost}
                    onChange={(e) => {
                      setTargetFoodCost(e.target.value);
                      const value = Number(e.target.value);
                      if (Number.isFinite(value) && value > 0 && value < 100) saveTargetFoodCostPercent(value);
                    }}
                    style={{ width: "4rem", border: "none", background: "transparent", fontWeight: 700 }}
                  />
                  %
                </strong>
              </div>
              <div className={`rstudio-cost-tile${pricing.suggestedSellingPrice == null ? " rstudio-cost-tile--pending" : ""}`}>
                <span>Suggested selling price</span>
                <strong>{costLabel(pricing.suggestedSellingPrice)}</strong>
              </div>
            </div>
            <p className="rstudio-panel__hint">
              Missing or unapproved material costs show COST PENDING — never treated as zero. Draft only; Menu Item prices are not updated.
            </p>
          </div>

          <div className="rstudio-section rstudio-section--lavender">
            <p className="rstudio-section__label">Menu and outlet</p>
            <div className="rstudio-fields">
              <div className="rstudio-field rstudio-field--wide">
                <label htmlFor="rstudio-stock-outlet">Outlet for stock / costing</label>
                <select
                  id="rstudio-stock-outlet"
                  value={outletLocationId}
                  onChange={(e) => setOutletLocationId(e.target.value)}
                  data-testid="recipe-studio-outlet"
                >
                  <option value="">Select outlet to view stock</option>
                  {outlets.map((outlet) => (
                    <option key={outlet.id} value={outlet.id}>
                      {outlet.locationName}
                      {outlet.plantCode || outlet.storageLocationCode
                        ? ` · ${[outlet.plantCode, outlet.storageLocationCode].filter(Boolean).join("/")}`
                        : ""}
                    </option>
                  ))}
                </select>
              </div>
              <div className="rstudio-field rstudio-field--wide">
                <label htmlFor="rstudio-menu">Linked menu item</label>
                <input
                  id="rstudio-menu"
                  value={menuItem}
                  placeholder="Placeholder — Menu Engineering later"
                  onChange={(e) => setMenuItem(e.target.value)}
                />
              </div>
              <div className="rstudio-field rstudio-field--wide">
                <label htmlFor="rstudio-outlets">Outlet assignments</label>
                <input
                  id="rstudio-outlets"
                  value={outletNote}
                  placeholder="Placeholder — outlet prices later"
                  onChange={(e) => setOutletNote(e.target.value)}
                />
              </div>
            </div>
            <p className="rstudio-panel__hint">
              {selectedOutlet
                ? `Stock uses inventory at ${selectedOutlet.locationName}. Out-of-stock materials can still be added.`
                : "Stock not determined until an authorized outlet is selected. No reservations or ledger writes."}
            </p>
          </div>
        </section>

        {/* RIGHT — Material Catalog */}
        <aside className="rstudio-panel rstudio-panel--catalog" aria-label="Material Catalog">
          <div>
            <h2 className="rstudio-panel__title">Material Catalog</h2>
            <p className="rstudio-panel__hint">
              Live ItemBuyerMaster search. Drag or tap Add. Stock reflects the selected outlet when available.
            </p>
          </div>
          <div className="rstudio-section rstudio-section--mint rstudio-section--catalog-live">
            <SilaRecipeMaterialLibrary
              addedIds={addedIds}
              onAdd={addMaterial}
              onAddMany={addMaterials}
              replaceTargetKey={replaceKey}
              onReplacePick={(material) => {
                if (!replaceKey) return;
                rememberMaterial(material.id);
                commitLines((current) =>
                  current.map((line) =>
                    line.key === replaceKey
                      ? replaceMaterialOnLine(line, material, line.quantity, material.baseUom)
                      : line,
                  ),
                );
                setReplaceKey(null);
              }}
              recommended={materialRecs.items}
              recommendReason={materialRecs.reason}
              outletLocationId={outletLocationId || null}
            />
          </div>
        </aside>
      </div>

      <div className="rstudio-actions" role="toolbar" aria-label="Recipe Studio actions">
        <button type="button" className="sila-btn sila-btn--secondary" onClick={onBackToCatalog}>
          {RECIPE_WORKSPACE_BACK_LABEL}
        </button>
        <button type="button" className="sila-btn sila-btn--secondary" disabled title="Available in Step 5">
          Save Draft — Available in Step 5
        </button>
        <button type="button" className="sila-btn sila-btn--primary" disabled title="Available in Step 6">
          Submit — Available in Step 6
        </button>
        <p className="rstudio-actions__note">
          Save Draft and Submit stay disabled. Ingredient changes remain local draft state — no recipe create/update API calls in Step 4.
        </p>
      </div>
    </div>
  );
};

export default SilaRecipeStudioPresentation;
