import React, { useMemo, useState } from "react";
import { costPercent, costPerServing, formatCost, formatPercent, marginPercent } from "./recipeFormat";
import { toNumber, type IngredientLine, type LineCost } from "./recipeEditorModel";
import {
  builderWarnings,
  costPerPortion,
  scaleLines,
  summarizeChanges,
  type BuilderSnapshot,
  type RecipeChangeSummary,
} from "./recipeBuilderState";

interface SilaRecipeIntelligenceProps {
  lines: IngredientLine[];
  costs: LineCost[];
  servingQty: string;
  servingUom: string;
  currency: string;
  menuPrice: string;
  pricedOutlets: number;
  outletId: string;
  baseline: BuilderSnapshot;
  dirty: boolean;
  onServingQty: (value: string) => void;
  onApplyScale: (lines: IngredientLine[], servingQty: string) => void;
}

const Incomplete = () => <span className="sila-me-flag">Incomplete</span>;
const CostPending = () => <span className="sila-me-flag">COST PENDING</span>;

/** Right-panel recipe intelligence: yield, live costing, warnings, change summary, portion scaling. */
const SilaRecipeIntelligence: React.FC<SilaRecipeIntelligenceProps> = ({
  lines,
  costs,
  servingQty,
  servingUom,
  currency,
  menuPrice,
  pricedOutlets,
  outletId,
  baseline,
  dirty,
  onServingQty,
  onApplyScale,
}) => {
  const [scaleDraft, setScaleDraft] = useState(servingQty);
  const portions = toNumber(servingQty);
  const totalCost = costs.length > 0 && costs.every((cost) => cost.cost != null)
    ? costs.reduce((sum, cost) => sum + (cost.cost ?? 0), 0)
    : null;
  const hasMissingCost = costs.some((cost) => cost.cost == null);
  const price = Number(menuPrice);
  const hasPrice = menuPrice.trim() !== "" && Number.isFinite(price) && price > 0;
  const perServing = totalCost == null ? null : costPerServing(totalCost, portions);
  const foodCostPct = perServing == null || !hasPrice ? null : costPercent(perServing, price);
  const marginAmt = perServing == null || !hasPrice ? null : price - perServing;
  const marginPct = perServing == null || !hasPrice ? null : marginPercent(perServing, price);
  const warnings = useMemo(() => builderWarnings(lines, outletId), [lines, outletId]);
  const changes: RecipeChangeSummary = useMemo(
    () => summarizeChanges(baseline, { lines, servingQty }, outletId),
    [baseline, lines, servingQty, outletId],
  );

  const previewScale = () => {
    const next = toNumber(scaleDraft);
    if (!(next > 0) || !(portions > 0) || next === portions) return;
    onApplyScale(scaleLines(lines, portions, next), String(next));
  };

  return (
    <aside className="srec-builder-panel srec-intel" aria-label="Recipe intelligence">
      <header className="srec-builder-panel__head">
        <h2 className="srec-builder-panel__title">Recipe intelligence</h2>
        {dirty ? <span className="sila-badge sila-badge--warning">Unsaved changes</span> : <span className="sila-badge sila-badge--info">Live costing</span>}
      </header>

      <div className="srec-intel-block">
        <label className="sila-field" htmlFor="srec-intel-portions">
          <span className="sila-label">Portions / yield</span>
          <div className="srec-intel-scale">
            <input
              id="srec-intel-portions"
              className="sila-input"
              type="number"
              min="0"
              step="any"
              value={servingQty}
              onChange={(event) => {
                onServingQty(event.target.value);
                setScaleDraft(event.target.value);
              }}
            />
            <span className="sila-help">{servingUom || "EA"}</span>
          </div>
        </label>
        <label className="sila-field" htmlFor="srec-intel-scale-to">
          <span className="sila-label">Scale ingredients to portions</span>
          <div className="srec-intel-scale">
            <input
              id="srec-intel-scale-to"
              className="sila-input"
              type="number"
              min="0"
              step="any"
              value={scaleDraft}
              onChange={(event) => setScaleDraft(event.target.value)}
            />
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={previewScale}>
              Apply scale
            </button>
          </div>
        </label>
        <span className="sila-help">Scaling updates the draft only. Save or submit through the existing workflow.</span>
      </div>

      <dl className="srec-intel-metrics">
        <div><dt>Ingredients</dt><dd>{lines.length}</dd></div>
        <div><dt>Total ingredient cost</dt><dd>{hasMissingCost ? <CostPending /> : totalCost == null ? <Incomplete /> : formatCost(totalCost, currency)}</dd></div>
        <div><dt>Cost / portion</dt><dd>{hasMissingCost ? <CostPending /> : perServing == null ? <Incomplete /> : formatCost(perServing, currency)}</dd></div>
        <div><dt>Selling price</dt><dd>{hasPrice ? formatCost(price, currency) : <span className="sila-me-flag">No menu price</span>}</dd></div>
        <div><dt>Food cost %</dt><dd>{hasMissingCost ? <CostPending /> : foodCostPct == null ? <Incomplete /> : formatPercent(foodCostPct)}</dd></div>
        <div><dt>Contribution margin</dt><dd>{hasMissingCost ? <CostPending /> : marginAmt == null ? <Incomplete /> : `${formatCost(marginAmt, currency)} (${formatPercent(marginPct)})`}</dd></div>
        <div><dt>Priced outlets</dt><dd>{pricedOutlets}</dd></div>
        <div><dt>Cost / portion (live)</dt><dd>{formatCost(costPerPortion(totalCost, portions), currency)}</dd></div>
      </dl>

      {warnings.length > 0 && (
        <div className="srec-intel-block">
          <h3 className="srec-builder-panel__subtitle">Warnings</h3>
          <ul className="srec-intel-list">
            {warnings.map((warning) => <li key={warning}>{warning}</li>)}
          </ul>
        </div>
      )}

      {dirty && (
        <div className="srec-intel-block">
          <h3 className="srec-builder-panel__subtitle">Change summary</h3>
          <ul className="srec-intel-list">
            {changes.added.map((item) => <li key={`a-${item}`}>Added: {item}</li>)}
            {changes.removed.map((item) => <li key={`r-${item}`}>Removed: {item}</li>)}
            {changes.replaced.map((item) => <li key={`p-${item}`}>Replaced: {item}</li>)}
            {changes.quantityChanged.map((item) => <li key={`q-${item}`}>Qty changed: {item}</li>)}
            {changes.uomChanged.map((item) => <li key={`u-${item}`}>UOM changed: {item}</li>)}
            {(changes.added.length + changes.removed.length + changes.replaced.length + changes.quantityChanged.length + changes.uomChanged.length) === 0 && (
              <li>Header / portion edits only</li>
            )}
          </ul>
          <p className="sila-help">
            Cost {formatCost(changes.totalCostBefore, currency)} → {hasMissingCost ? "COST PENDING" : formatCost(changes.totalCostAfter, currency)}
            {" · "}
            /portion {formatCost(changes.costPerPortionBefore, currency)} → {hasMissingCost ? "COST PENDING" : formatCost(changes.costPerPortionAfter, currency)}
          </p>
        </div>
      )}

      <div className="srec-intel-block">
        <h3 className="srec-builder-panel__subtitle">Suggestions</h3>
        <ul className="srec-intel-list">
          {warnings.filter((w) => w.includes("COST PENDING") || w.includes("duplicate") || w.includes("UOM")).slice(0, 6).map((warning) => (
            <li key={`s-${warning}`}>{warning} — review before save.</li>
          ))}
          {warnings.length === 0 && <li>No advisory issues from current recipe data.</li>}
        </ul>
        <span className="sila-help">Suggestions are advisory only. Nothing is auto-replaced.</span>
      </div>
    </aside>
  );
};

export default SilaRecipeIntelligence;
