import React, { useMemo, useState } from "react";
import {
  calculateRecipeScaling,
  DEMO_SALMON_RECIPE,
  type DemoRecipeIngredient,
  type ScalingRule,
} from "./culinarySimulatorsMath";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface CulinaryRecipePlannerWorkspaceProps {
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

const RULE_LABELS: Record<ScalingRule, string> = {
  LINEAR: "Linear",
  SPICE_NONLINEAR: "Non-linear Spice (0.85^)",
  SEASONING_STEPPED: "Stepped Seasoning (0.80^)",
  LIQUID_EVAPORATION: "Liquid Evaporation (0.92^)",
  FIXED_BATCH: "Fixed Batch",
};

const CulinaryRecipePlannerWorkspace: React.FC<CulinaryRecipePlannerWorkspaceProps> = ({
  onOpenWorkspace,
}) => {
  const [targetServings, setTargetServings] = useState(10);
  const [targetServingSizeGrams, setTargetServingSizeGrams] = useState(180);
  const [prepBufferPercent, setPrepBufferPercent] = useState(0);
  const [wasteAllowancePercent, setWasteAllowancePercent] = useState(0);
  const [dishPrompt, setDishPrompt] = useState("Grilled Salmon with Lemon Butter Sauce");
  const [cuisine, setCuisine] = useState("International / Modern European");
  const [dietaryAvoid, setDietaryAvoid] = useState("Shellfish, Tree Nuts");
  const [availableStockOnly, setAvailableStockOnly] = useState(false);
  const [ingredients, setIngredients] = useState<DemoRecipeIngredient[]>(DEMO_SALMON_RECIPE.ingredients);

  const result = useMemo(
    () =>
      calculateRecipeScaling({
        baseServings: DEMO_SALMON_RECIPE.baseServings,
        targetServings,
        baseServingSizeGrams: DEMO_SALMON_RECIPE.baseServingSizeGrams,
        targetServingSizeGrams,
        prepBufferPercent,
        wasteAllowancePercent,
        ingredients,
      }),
    [targetServings, targetServingSizeGrams, prepBufferPercent, wasteAllowancePercent, ingredients]
  );

  const updateIngredientRule = (id: string, rule: ScalingRule) => {
    setIngredients((prev) => prev.map((row) => (row.id === id ? { ...row, scalingRule: rule } : row)));
  };

  const resetDemo = () => {
    setTargetServings(10);
    setTargetServingSizeGrams(180);
    setPrepBufferPercent(0);
    setWasteAllowancePercent(0);
    setIngredients(DEMO_SALMON_RECIPE.ingredients);
  };

  return (
    <div className="cs2-shell" data-testid="culinary-recipe-planner-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">AI Recipe Planner &amp; Interactive Scaling Simulator</h2>
          <p className="cs2-subtitle">
            Phase 0 interactive demonstration workspace (§12–§18). Live LLM recipe generation activates in Phase 1A;
            the deterministic scaling &amp; costing engine below runs in real time on the illustrative demonstration recipe.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={resetDemo}>
            Reset Demonstration
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("material-requests")}>
            View Missing Material Flow
          </button>
        </div>
      </div>

      <div className="cs2-banner" role="status">
        <div>
          <strong>DEMONSTRATION DATA — ILLUSTRATIVE RECIPE ONLY ({DEMO_SALMON_RECIPE.title})</strong>
          <div>
            Material codes shown (`DEMO-MAT-*`) are illustrative demonstration references, not live customer Material Master items.
            Live AI recipe generation &amp; Recipe Master draft persistence are scheduled for Phase 1A/1C.
          </div>
        </div>
        <span className="cs2-badge cs2-badge--demo">Phase 0 Interactive Demo</span>
      </div>

      {/* Phase 1A AI Generation Workspace Shell (Explicitly Disabled Execution in Phase 0) */}
      <section className="cs2-card" aria-label="AI Recipe Generation Inputs Preview">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">AI Recipe Generation Brief (Phase 1A Workspace Preview)</h3>
          <span className="cs2-badge cs2-badge--planned">AI Generation Activates Phase 1A</span>
        </div>
        <div className="cs2-form-grid">
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-rp-dish">Dish name / chef prompt</label>
            <input id="cs2-rp-dish" className="sila-input" value={dishPrompt} onChange={(e) => setDishPrompt(e.target.value)} />
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-rp-cuisine">Cuisine</label>
            <input id="cs2-rp-cuisine" className="sila-input" value={cuisine} onChange={(e) => setCuisine(e.target.value)} />
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-rp-allergen">Allergens to avoid</label>
            <input id="cs2-rp-allergen" className="sila-input" value={dietaryAvoid} onChange={(e) => setDietaryAvoid(e.target.value)} />
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-rp-stock">Stock mode</label>
            <label className="cs2-checkbox-row" style={{ marginTop: 6 }}>
              <input type="checkbox" checked={availableStockOnly} onChange={(e) => setAvailableStockOnly(e.target.checked)} />
              Available-stock-only preference
            </label>
          </div>
        </div>
        <div className="cs2-actions-row" style={{ marginTop: 12 }}>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--primary"
            disabled
            title="Live AI recipe generation is gated to Phase 1A"
          >
            Generate Recipe with AI (Phase 1A)
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--sm sila-btn--secondary"
            disabled
            title="Saving AI drafts to Recipe Master is gated to Phase 1C"
          >
            Submit for Recipe Approval (Phase 1C)
          </button>
          <span className="cs2-empty-note">
            No unauthorized external LLM calls or production recipe writes occur in Phase 0.
          </span>
        </div>
      </section>

      {/* Interactive Scaling Controls */}
      <section className="cs2-card" aria-label="Interactive Recipe Scaling Controls">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Interactive Recipe Scaling Controls</h3>
          <span className="cs2-badge cs2-badge--demo">Scale Factor: {result.scaleFactor}x</span>
        </div>

        <div className="cs2-form-grid">
          <div className="cs2-slider-row">
            <label className="sila-label" htmlFor="cs2-scale-servings">
              Target Servings: <strong>{targetServings}</strong> (base: {DEMO_SALMON_RECIPE.baseServings})
            </label>
            <input
              id="cs2-scale-servings"
              type="range"
              min={1}
              max={500}
              value={targetServings}
              onChange={(e) => setTargetServings(Number(e.target.value))}
            />
            <input
              type="number"
              className="sila-input"
              aria-label="Target servings numeric input"
              min={1}
              max={5000}
              value={targetServings}
              onChange={(e) => setTargetServings(Math.max(1, Number(e.target.value) || 1))}
            />
          </div>

          <div className="cs2-slider-row">
            <label className="sila-label" htmlFor="cs2-scale-size">
              Portion Size (g): <strong>{targetServingSizeGrams} g</strong> (base: 180 g)
            </label>
            <input
              id="cs2-scale-size"
              type="range"
              min={80}
              max={400}
              step={10}
              value={targetServingSizeGrams}
              onChange={(e) => setTargetServingSizeGrams(Number(e.target.value))}
            />
            <input
              type="number"
              className="sila-input"
              aria-label="Serving size grams"
              min={20}
              max={1000}
              value={targetServingSizeGrams}
              onChange={(e) => setTargetServingSizeGrams(Math.max(20, Number(e.target.value) || 180))}
            />
          </div>

          <div className="cs2-slider-row">
            <label className="sila-label" htmlFor="cs2-scale-prep">
              Preparation Buffer: <strong>{prepBufferPercent}%</strong>
            </label>
            <input
              id="cs2-scale-prep"
              type="range"
              min={0}
              max={30}
              step={1}
              value={prepBufferPercent}
              onChange={(e) => setPrepBufferPercent(Number(e.target.value))}
            />
          </div>

          <div className="cs2-slider-row">
            <label className="sila-label" htmlFor="cs2-scale-waste">
              Waste Allowance: <strong>{wasteAllowancePercent}%</strong>
            </label>
            <input
              id="cs2-scale-waste"
              type="range"
              min={0}
              max={25}
              step={1}
              value={wasteAllowancePercent}
              onChange={(e) => setWasteAllowancePercent(Number(e.target.value))}
            />
          </div>
        </div>
      </section>

      <div className="cs2-kpi-grid" data-testid="recipe-scaling-kpis">
        <div className="cs2-kpi cs2-kpi--accent">
          <span className="cs2-kpi__label">Base Recipe Cost (10 srv)</span>
          <span className="cs2-kpi__value">AED {result.baseTotalCostAed.toFixed(2)}</span>
          <span className="cs2-kpi__hint">AED {result.baseCostPerServingAed.toFixed(2)} / serving</span>
        </div>
        <div className="cs2-kpi cs2-kpi--cyan">
          <span className="cs2-kpi__label">Scaled Recipe Cost ({targetServings} srv)</span>
          <span className="cs2-kpi__value">AED {result.scaledTotalCostAed.toFixed(2)}</span>
          <span className="cs2-kpi__hint">Includes prep &amp; waste buffers</span>
        </div>
        <div className="cs2-kpi cs2-kpi--emerald">
          <span className="cs2-kpi__label">Scaled Cost / Serving</span>
          <span className="cs2-kpi__value">AED {result.scaledCostPerServingAed.toFixed(2)}</span>
          <span className="cs2-kpi__hint">
            Food cost at AED {DEMO_SALMON_RECIPE.targetSellingPriceAed}:{" "}
            {((result.scaledCostPerServingAed / DEMO_SALMON_RECIPE.targetSellingPriceAed) * 100).toFixed(1)}%
          </span>
        </div>
        <div className="cs2-kpi cs2-kpi--rose">
          <span className="cs2-kpi__label">Missing Materials</span>
          <span className="cs2-kpi__value">{result.unmatchedCount}</span>
          <span className="cs2-kpi__hint">Requires Material Creation Request</span>
        </div>
      </div>

      {/* Ingredient Table */}
      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Scaled Ingredient Cost Sheet — {DEMO_SALMON_RECIPE.title}</h3>
          <span className="cs2-empty-note">Adjust scaling rule per ingredient to model non-linear spices &amp; liquids</span>
        </div>
        <div className="cs2-table-wrap">
          <table className="cs2-table" data-testid="recipe-scaling-table">
            <thead>
              <tr>
                <th>Ingredient</th>
                <th>Match Status (Demo)</th>
                <th>Scaling Rule</th>
                <th className="cs2-num">Base Qty</th>
                <th className="cs2-num">Scaled Qty</th>
                <th className="cs2-num">Gross w/ Buffers</th>
                <th>UOM</th>
                <th className="cs2-num">Unit Cost</th>
                <th className="cs2-num">Scaled Ext. Cost</th>
              </tr>
            </thead>
            <tbody>
              {result.items.map((item) => (
                <tr key={item.id}>
                  <td>
                    <strong>{item.name}</strong>
                    <div className="cs2-empty-note">
                      {item.category}
                      {item.allergenHint ? ` · Allergen hint: ${item.allergenHint}` : ""}
                    </div>
                  </td>
                  <td>
                    {item.matchStatus === "EXACT_MATCH" && (
                      <span className="cs2-badge cs2-badge--live">{item.demoMaterialRef}</span>
                    )}
                    {item.matchStatus === "SUGGESTED_MATCH" && (
                      <span className="cs2-badge cs2-badge--warn">{item.demoMaterialRef} (Suggested)</span>
                    )}
                    {item.matchStatus === "UNMATCHED" && (
                      <span className="cs2-badge cs2-badge--danger">Unmatched · Material Request Req.</span>
                    )}
                  </td>
                  <td>
                    <select
                      className="sila-select"
                      aria-label={`Scaling rule for ${item.name}`}
                      value={item.scalingRule}
                      onChange={(e) => updateIngredientRule(item.id, e.target.value as ScalingRule)}
                    >
                      {(Object.keys(RULE_LABELS) as ScalingRule[]).map((ruleKey) => (
                        <option key={ruleKey} value={ruleKey}>
                          {RULE_LABELS[ruleKey]}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="cs2-num">{item.baseQuantity.toFixed(3)}</td>
                  <td className="cs2-num">{item.scaledQuantity.toFixed(3)}</td>
                  <td className="cs2-num"><strong>{item.grossQuantityWithBuffers.toFixed(3)}</strong></td>
                  <td>{item.uom}</td>
                  <td className="cs2-num">AED {item.unitCostAed.toFixed(2)}</td>
                  <td className="cs2-num"><strong>AED {item.scaledExtendedCostAed.toFixed(2)}</strong></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};

export default CulinaryRecipePlannerWorkspace;
