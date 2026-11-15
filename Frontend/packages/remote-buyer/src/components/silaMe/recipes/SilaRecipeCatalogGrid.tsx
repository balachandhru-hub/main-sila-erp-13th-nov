import React from "react";
import type { SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import { costPerServing, formatCost, formatQty, recipeBadgeClass, recipeLabel } from "./recipeFormat";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";

interface SilaRecipeCatalogGridProps {
  rows: SilaRecipeListItem[];
  canManage: boolean;
  onOpen: (recipeId: string) => void;
  onEdit?: (recipeId: string) => void;
}

/** Premium visual recipe catalog cards (default Recipe Studio grid). */
const SilaRecipeCatalogGrid: React.FC<SilaRecipeCatalogGridProps> = ({ rows, canManage, onOpen, onEdit }) => (
  <div className="rstudio-grid" role="list">
    {rows.map((row) => {
      const kind = resolveStudioKind(row.category, row.familyName, row.name);
      const portionCost = row.costComplete || row.activeVersion > 0
        ? (row.costPerServing ?? costPerServing(row.totalCost, row.servingQty))
        : null;
      return (
        <article key={row.id} className="rstudio-card" role="listitem">
          <button type="button" className="rstudio-card__media" onClick={() => onOpen(row.id)} aria-label={`View ${row.name}`}>
            <StudioCover kind={kind} title={row.name} subtitle={row.category || row.familyName || undefined} />
            <span className={`rstudio-card__status ${recipeBadgeClass(row.status)}`}>{recipeLabel(row.status)}</span>
          </button>
          <div className="rstudio-card__body">
            <h3 className="rstudio-card__title">
              <button type="button" className="rstudio-card__link" onClick={() => onOpen(row.id)}>{row.name}</button>
            </h3>
            <p className="rstudio-card__meta">
              {row.recipeCode}
              {row.category ? ` · ${row.category}` : ""}
              {row.familyName ? ` · ${row.familyName}` : ""}
            </p>
            <div className="rstudio-card__stats">
              <span>v{row.version}{row.activeVersion > 0 && row.activeVersion !== row.version ? ` · selling v${row.activeVersion}` : ""}</span>
              <span>{row.ingredientCount} ingredients</span>
              <span>{formatQty(row.servingQty)} {row.servingUom}</span>
            </div>
            <div className="rstudio-card__costs">
              <div>
                <em>Cost / portion</em>
                <strong>{portionCost == null ? "COST PENDING" : formatCost(portionCost, row.currency)}</strong>
              </div>
              <div>
                <em>Recipe cost</em>
                <strong>{row.costComplete || row.activeVersion > 0 ? formatCost(row.totalCost, row.currency) : "COST PENDING"}</strong>
              </div>
            </div>
            <div className="rstudio-card__actions">
              <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>View</button>
              {canManage && onEdit && (
                <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onEdit(row.id)}>Edit</button>
              )}
            </div>
          </div>
        </article>
      );
    })}
  </div>
);

export default SilaRecipeCatalogGrid;
