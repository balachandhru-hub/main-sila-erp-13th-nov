import React, { useMemo, useState } from "react";
import type { SilaIngredientOption } from "../../../api/silaMe/silaRecipeToolsApi";
import { formatCost, materialUoms } from "./recipeFormat";
import { lineCost, materialFromOption, type IngredientLine } from "./recipeEditorModel";
import { lineLabel } from "./recipeBuilderState";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";
import SilaRecipeMaterialLibrary from "./SilaRecipeMaterialLibrary";

interface SilaRecipeReplacePanelProps {
  line: IngredientLine;
  outletId: string;
  currency: string;
  onCancel: () => void;
  onConfirm: (option: SilaIngredientOption, quantity: string, uom: string) => void;
}

/** Compact replace flow: pick replacement, compare cost, confirm. */
const SilaRecipeReplacePanel: React.FC<SilaRecipeReplacePanelProps> = ({ line, outletId, currency, onCancel, onConfirm }) => {
  const [candidate, setCandidate] = useState<SilaIngredientOption | null>(null);
  const [quantity, setQuantity] = useState(line.quantity);
  const [uom, setUom] = useState(line.uom);
  const existingCost = lineCost(line, outletId);

  const proposedLine = useMemo(() => {
    if (!candidate) return null;
    return {
      ...line,
      material: materialFromOption(candidate),
      subRecipe: undefined,
      quantity,
      uom: uom.toUpperCase(),
    } as IngredientLine;
  }, [candidate, line, quantity, uom]);

  const proposedCost = proposedLine ? lineCost(proposedLine, outletId) : null;
  const delta =
    existingCost.cost != null && proposedCost?.cost != null ? proposedCost.cost - existingCost.cost : null;
  const uoms = candidate
    ? Array.from(new Set([uom.toUpperCase(), ...materialUoms(materialFromOption(candidate))]))
    : [uom];

  return (
    <div className="srec-replace" role="dialog" aria-label="Replace ingredient">
      <header className="srec-replace__head">
        <h3>Replace ingredient</h3>
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={onCancel}>Cancel</button>
      </header>
      <div className="srec-replace__compare">
        <div className="rstudio-ingredient-card">
          <StudioCover compact kind={resolveStudioKind(line.material?.description, line.material?.materialCode)} title={lineLabel(line)} />
          <div>
          <span className="sila-label">Current ingredient</span>
          <p><strong>{lineLabel(line)}</strong></p>
          <p className="sila-help">{line.quantity} {line.uom} · {existingCost.cost == null ? "COST PENDING" : formatCost(existingCost.cost, currency)}</p>
          </div>
        </div>
        <div className="rstudio-ingredient-card">
          <StudioCover
            compact
            kind={resolveStudioKind(candidate?.description, candidate?.materialCode, candidate?.materialGroup)}
            title={candidate?.description || "Pick"}
          />
          <div>
          <span className="sila-label">Replacement ingredient</span>
          {candidate ? (
            <>
              <p><strong>{candidate.description}</strong></p>
              <p className="sila-help">{candidate.materialCode}</p>
              <div className="srec-replace__qty">
                <input className="sila-input" type="number" min="0" step="any" value={quantity} onChange={(e) => setQuantity(e.target.value)} aria-label="Replacement quantity" />
                <select className="sila-select" value={uom} onChange={(e) => setUom(e.target.value)} aria-label="Replacement UOM">
                  {uoms.map((unit) => <option key={unit} value={unit}>{unit}</option>)}
                </select>
              </div>
              <p className="sila-help">
                {proposedCost?.cost == null ? "COST PENDING" : formatCost(proposedCost.cost, currency)}
                {delta != null && ` · Δ ${formatCost(delta, currency)}`}
              </p>
              {proposedCost?.issue && <p className="sila-me-flag srec-negative">{proposedCost.issue}</p>}
            </>
          ) : (
            <p className="sila-help">Choose a material below.</p>
          )}
          </div>
        </div>
      </div>
      {!candidate ? (
        <SilaRecipeMaterialLibrary
          addedIds={[]}
          replaceTargetKey={line.key}
          onAdd={() => undefined}
          onAddMany={() => undefined}
          onReplacePick={(material) => {
            setCandidate(material);
            setUom(material.baseUom.toUpperCase());
            setQuantity(line.quantity);
          }}
        />
      ) : (
        <div className="srec-replace__actions">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setCandidate(null)}>Choose another</button>
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={!(Number(quantity) > 0)}
            onClick={() => onConfirm(candidate, quantity, uom)}
          >
            Confirm replace
          </button>
        </div>
      )}
    </div>
  );
};

export default SilaRecipeReplacePanel;
