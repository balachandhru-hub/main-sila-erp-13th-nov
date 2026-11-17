import React, { useState } from "react";
import { RECIPE_WORKSPACE_BACK_LABEL } from "./recipeWorkspaceShellCopy";
import "./SilaRecipeStudioPresentation.css";

export type RecipeStudioMode = "new" | "edit";

interface SilaRecipeStudioPresentationProps {
  mode: RecipeStudioMode;
  /** Route id in Edit mode only; never fetched in Step 3. */
  recipeId?: string | null;
  onBackToCatalog: () => void;
}

/** Design-only catalog chips — not live Material Master data. */
const CATALOG_CHIPS = ["All", "Spirits", "Produce", "Dairy", "Dry goods", "Recommended"] as const;

/** Placeholder material cards — structural UI only, no invented prices/stock. */
const MATERIAL_PLACEHOLDERS = [
  { name: "Material card", code: "Code —", family: "Family / category", uom: "Base UOM" },
  { name: "Material card", code: "Code —", family: "Family / category", uom: "Base UOM" },
  { name: "Material card", code: "Code —", family: "Family / category", uom: "Base UOM" },
] as const;

/**
 * Step 3 frontend-only Recipe Studio presentation.
 * No Recipe/Material API calls. Save/Submit disabled until later steps.
 */
const SilaRecipeStudioPresentation: React.FC<SilaRecipeStudioPresentationProps> = ({
  mode,
  recipeId,
  onBackToCatalog,
}) => {
  const isEdit = mode === "edit";
  const idText = (recipeId ?? "").trim();

  // Local UI draft only — never persisted in Step 3.
  const [name, setName] = useState("");
  const [family, setFamily] = useState("");
  const [category, setCategory] = useState("");
  const [cuisine, setCuisine] = useState("");
  const [mealPeriod, setMealPeriod] = useState("");
  const [yieldQty, setYieldQty] = useState("1");
  const [prepTime, setPrepTime] = useState("");
  const [targetFoodCost, setTargetFoodCost] = useState("30");
  const [menuItem, setMenuItem] = useState("");
  const [outlets, setOutlets] = useState("");
  const [catalogSearch, setCatalogSearch] = useState("");
  const [activeChip, setActiveChip] = useState<(typeof CATALOG_CHIPS)[number]>("All");

  const heading = isEdit ? "Edit Recipe" : "Create New Recipe";

  return (
    <div className="rstudio-build" data-testid="recipe-studio-presentation" data-mode={mode}>
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={onBackToCatalog}>
        ← {RECIPE_WORKSPACE_BACK_LABEL}
      </button>

      <header className="rstudio-build__top">
        <div>
          <p className="rstudio-kicker">SILA Recipe Studio · Step 3 of 7</p>
          <h1>{heading}</h1>
          <p className="rstudio-build__top-meta">
            {isEdit
              ? "Same Studio layout as Create. Recipe data loading connects in Step 6 — no fabricated recipe details."
              : "Blank local UI draft. No database row until Save Draft is connected in Step 5."}
          </p>
          <div className="rstudio-build__badges">
            <span className="rstudio-build__badge rstudio-build__badge--draft">Draft</span>
            <span className="rstudio-build__badge rstudio-build__badge--mode">{isEdit ? "Edit mode" : "Create mode"}</span>
            <span className="rstudio-build__badge rstudio-build__badge--step">UI only</span>
            {isEdit && (
              <span className="rstudio-build__badge rstudio-build__badge--mode" data-testid="recipe-studio-edit-id">
                ID {idText || "—"}
              </span>
            )}
          </div>
        </div>
      </header>

      <div className="rstudio-build__panels">
        {/* LEFT — My Recipe */}
        <section className="rstudio-panel rstudio-panel--recipe" aria-label="My Recipe">
          <div>
            <h2 className="rstudio-panel__title">My Recipe</h2>
            <p className="rstudio-panel__hint">Ingredient-first canvas. Catalog APIs and costing connect in later steps.</p>
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
                  <input id="rstudio-family" value={family} placeholder="Placeholder" onChange={(e) => setFamily(e.target.value)} />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-category">Category</label>
                  <input id="rstudio-category" value={category} placeholder="Placeholder" onChange={(e) => setCategory(e.target.value)} />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-cuisine">Cuisine</label>
                  <input id="rstudio-cuisine" value={cuisine} placeholder="Placeholder" onChange={(e) => setCuisine(e.target.value)} />
                </div>
                <div className="rstudio-field">
                  <label htmlFor="rstudio-meal">Meal period</label>
                  <input id="rstudio-meal" value={mealPeriod} placeholder="Placeholder" onChange={(e) => setMealPeriod(e.target.value)} />
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
          </div>

          <div className="rstudio-section">
            <p className="rstudio-section__label">Ingredient canvas</p>
            <div className="rstudio-canvas-empty" data-testid="recipe-ingredient-canvas-empty">
              <div className="rstudio-canvas-empty__icon" aria-hidden="true">+</div>
              <strong>No ingredients yet</strong>
              <p>Drag ingredients here or add from Material Catalog. Quantity, UOM, remove and reorder connect in Step 4.</p>
            </div>
            <div className="rstudio-ingredient-ph" aria-hidden="true">
              <span>Ingredient card placeholder</span>
              <span>Qty</span>
              <span>UOM</span>
              <span>Remove / reorder</span>
            </div>
          </div>

          <div className="rstudio-section rstudio-section--amber">
            <p className="rstudio-section__label">Cost summary</p>
            <div className="rstudio-cost-grid">
              <div className="rstudio-cost-tile rstudio-cost-tile--pending">
                <span>Total recipe cost</span>
                <strong>COST PENDING</strong>
              </div>
              <div className="rstudio-cost-tile rstudio-cost-tile--pending">
                <span>Cost per portion</span>
                <strong>COST PENDING</strong>
              </div>
              <div className="rstudio-cost-tile">
                <span>Target food cost %</span>
                <strong>
                  <input
                    aria-label="Target food cost percent"
                    value={targetFoodCost}
                    onChange={(e) => setTargetFoodCost(e.target.value)}
                    style={{ width: "4rem", border: "none", background: "transparent", fontWeight: 700 }}
                  />
                  %
                </strong>
              </div>
              <div className="rstudio-cost-tile rstudio-cost-tile--pending">
                <span>Suggested selling price</span>
                <strong>COST PENDING</strong>
              </div>
            </div>
            <p className="rstudio-panel__hint">No material prices are invented in Step 3. Costing connects with catalog data later.</p>
          </div>

          <div className="rstudio-section rstudio-section--lavender">
            <p className="rstudio-section__label">Menu and outlet</p>
            <div className="rstudio-fields">
              <div className="rstudio-field rstudio-field--wide">
                <label htmlFor="rstudio-menu">Linked menu item</label>
                <input id="rstudio-menu" value={menuItem} placeholder="Placeholder — Menu Engineering later" onChange={(e) => setMenuItem(e.target.value)} />
              </div>
              <div className="rstudio-field rstudio-field--wide">
                <label htmlFor="rstudio-outlets">Outlet assignments</label>
                <input id="rstudio-outlets" value={outlets} placeholder="Placeholder — outlet prices later" onChange={(e) => setOutlets(e.target.value)} />
              </div>
            </div>
            <p className="rstudio-panel__hint">Category suggestions and outlet pricing remain presentation-only until later steps.</p>
          </div>
        </section>

        {/* RIGHT — Material Catalog */}
        <aside className="rstudio-panel rstudio-panel--catalog" aria-label="Material Catalog">
          <div>
            <h2 className="rstudio-panel__title">Material Catalog</h2>
            <p className="rstudio-panel__hint">Grocery-style layout. Live Material Master search connects in Step 4.</p>
          </div>

          <div className="rstudio-section rstudio-section--mint">
            <p className="rstudio-section__label">Search</p>
            <div className="rstudio-field">
              <label htmlFor="rstudio-catalog-search" className="sila-visually-hidden">Search materials</label>
              <input
                id="rstudio-catalog-search"
                value={catalogSearch}
                placeholder="Search materials (UI only)"
                onChange={(e) => setCatalogSearch(e.target.value)}
              />
            </div>
            <div className="rstudio-chips" role="group" aria-label="Catalog filters">
              {CATALOG_CHIPS.map((chip) => (
                <button
                  key={chip}
                  type="button"
                  className={`rstudio-chip${activeChip === chip ? " rstudio-chip--active" : ""}`}
                  onClick={() => setActiveChip(chip)}
                >
                  {chip}
                </button>
              ))}
            </div>
          </div>

          <div className="rstudio-section">
            <p className="rstudio-section__label">Recommended materials</p>
            <div className="rstudio-catalog-empty" role="status">
              No live materials in Step 3. Cards below are design placeholders only — not customer Item Master data.
            </div>
            <div className="rstudio-material-grid">
              {MATERIAL_PLACEHOLDERS.map((item, index) => (
                <article key={`material-ph-${index}`} className="rstudio-material-card">
                  <div>
                    <h3>{item.name}</h3>
                    <p>{item.code} · {item.family}</p>
                    <div className="rstudio-material-meta">
                      <span className="rstudio-pill rstudio-pill--uom">{item.uom}</span>
                      <span className="rstudio-pill rstudio-pill--stock">Stock —</span>
                      <span className="rstudio-pill rstudio-pill--cost">Cost —</span>
                    </div>
                  </div>
                  <span className="rstudio-add-ph" title="Add Ingredient available in Step 4">Add Ingredient · Step 4</span>
                </article>
              ))}
            </div>
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
          Save Draft and Submit stay disabled until their steps. No Recipe or Material API calls in Step 3.
        </p>
      </div>
    </div>
  );
};

export default SilaRecipeStudioPresentation;
