import React from "react";
import type { SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import { costPerServing, formatCost, formatQty, recipeBadgeClass, recipeLabel } from "./recipeFormat";
import { formatProductClassLabel } from "./recipeHospitality";
import SilaRecipePicture from "./SilaRecipePicture";

interface SilaRecipeCatalogGridProps {
  rows: SilaRecipeListItem[];
  canManage: boolean;
  onOpen: (recipeId: string) => void;
  onEdit?: (recipeId: string) => void;
  onDelete?: (recipe: SilaRecipeListItem) => void;
}

/** Premium visual recipe catalog cards (default Recipe Studio grid). */
const SilaRecipeCatalogGrid: React.FC<SilaRecipeCatalogGridProps> = ({ rows, canManage, onOpen, onEdit, onDelete }) => (
  <div className="rstudio-grid" role="list">
    {rows.map((row) => {
      const portionCost = row.costComplete || row.activeVersion > 0
        ? (row.costPerServing ?? costPerServing(row.totalCost, row.servingQty))
        : null;
      const canDeleteDraft = canManage && row.status === "DRAFT" && Boolean(onDelete);
      const canDeleteInactive = canManage && row.status === "INACTIVE" && Boolean(onDelete);
      return (
        <article key={row.id} className="rstudio-card" role="listitem">
          <button type="button" className="rstudio-card__media" onClick={() => onOpen(row.id)} aria-label={`View ${row.name}`}>
            <SilaRecipePicture
              recipeId={row.id}
              hasPicture={Boolean(row.hasPicture)}
              placeholderTitle={row.name}
              placeholderCategory={row.category}
              placeholderFamily={row.familyName}
              variant="catalog"
              locked
            />
            <span className={`rstudio-card__status ${recipeBadgeClass(row.status)}`}>{recipeLabel(row.status)}</span>
          </button>
          <div className="rstudio-card__body">
            <h3 className="rstudio-card__title">
              <button type="button" className="rstudio-card__link" onClick={() => onOpen(row.id)}>{row.name}</button>
            </h3>
            <p className="rstudio-card__meta">
              {row.recipeCode}
              {row.productClass ? ` · ${formatProductClassLabel(row.productClass)}` : ""}
              {row.recipeFormat ? ` · ${row.recipeFormat.replace(/_/g, " ")}` : ""}
              {row.category ? ` · ${row.category}` : ""}
              {row.familyName ? ` · ${row.familyName}` : ""}
              {row.posCode ? ` · POS ${row.posCode}` : ""}
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
                <em>Menu item</em>
                <strong>{row.posItem || row.posCode || "—"}</strong>
              </div>
            </div>
            <div className="rstudio-card__actions">
              <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>View</button>
              {canManage && onEdit && row.status !== "INACTIVE" && row.status !== "PENDING_APPROVAL" && (
                <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onEdit(row.id)}>Edit</button>
              )}
              {canDeleteDraft && (
                <button type="button" className="sila-btn sila-btn--danger sila-btn--sm" onClick={() => onDelete?.(row)}>Delete Draft</button>
              )}
              {canDeleteInactive && (
                <button
                  type="button"
                  className="sila-btn sila-btn--danger sila-btn--sm"
                  onClick={() => onDelete?.(row)}
                  data-testid="recipe-catalog-delete-inactive"
                >
                  Delete permanently
                </button>
              )}
            </div>
          </div>
        </article>
      );
    })}
  </div>
);

export default SilaRecipeCatalogGrid;
