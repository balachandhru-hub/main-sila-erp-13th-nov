import React from "react";
import type { SilaMaterial } from "../../../api/silaMe/silaInventoryApi";
import SilaPriceStatusBadge from "../materials/SilaPriceStatusBadge";
import { codeLabel, formatPrice } from "../materials/materialFormat";

interface SilaMaterialCatalogGridProps {
  materials: SilaMaterial[];
  plantCode: string;
  canManage: boolean;
  canManageMasterData: boolean;
  onDetail: (materialId: string) => void;
  onPrice: (material: SilaMaterial) => void;
  onEdit: (material: SilaMaterial) => void;
}

/** Catalog (grid) cards for Item Master inventory — description-first browsing. */
const SilaMaterialCatalogGrid: React.FC<SilaMaterialCatalogGridProps> = ({
  materials,
  plantCode,
  canManage,
  canManageMasterData,
  onDetail,
  onPrice,
  onEdit,
}) => (
  <div className="sinv-catalog-grid" role="list">
    {materials.map((material) => {
      const plantsList = material.plantCodes ?? [];
      const valuationPlant = plantCode || (plantsList.length === 1 ? plantsList[0] : null);
      const inventoryLabel = material.isInventoryItem
        ? codeLabel(material.inventoryType || "STOCK")
        : material.inventoryType
          ? codeLabel(material.inventoryType)
          : "Non-inventory";

      return (
        <article key={material.id} className="sinv-mat-card" role="listitem">
          <button
            type="button"
            className="sinv-mat-card__hero"
            onClick={() => onDetail(material.id)}
            aria-label={`Open ${material.materialCode} detail`}
          >
            <span className="sinv-mat-card__code">{material.materialCode}</span>
            <span className="sinv-mat-card__description">{material.description || "No description"}</span>
            <span className="sinv-mat-card__badges">
              <span className={`sila-badge ${material.isInventoryItem ? "sila-badge--success" : "sila-badge--neutral"}`}>
                {inventoryLabel}
              </span>
              <SilaPriceStatusBadge status={material.priceStatus} />
            </span>
          </button>

          <div className="sinv-mat-card__body">
            <div className="sinv-mat-card__row">
              <span className="sila-help">Unit price</span>
              <strong className="sinv-mat-card__price">
                {material.unitCost === null || material.unitCost === undefined
                  ? "Price missing"
                  : formatPrice(material.unitCost, material.currency, material.approvedPriceUom || material.baseUom)}
              </strong>
            </div>
            <div className="sinv-mat-card__row">
              <span className="sila-help">Base UOM</span>
              <span>{material.baseUom || "—"}</span>
            </div>
            <div className="sinv-mat-card__row">
              <span className="sila-help">Group</span>
              <span>{material.materialGroup || "—"}</span>
            </div>
            <div className="sinv-mat-card__row">
              <span className="sila-help">Plants</span>
              <span>
                {plantsList.length === 0
                  ? "—"
                  : plantsList.slice(0, 3).join(", ") + (plantsList.length > 3 ? ` +${plantsList.length - 3}` : "")}
              </span>
            </div>
            {(valuationPlant || material.valuationClass || material.priceControl) && (
              <p className="sinv-sub sinv-mat-card__valuation">
                {[
                  material.priceControl === "S" ? "Standard" : material.priceControl === "V" ? "Moving avg" : null,
                  material.valuationClass,
                  valuationPlant ? `Area ${valuationPlant}` : null,
                ].filter(Boolean).join(" · ")}
              </p>
            )}
            <div className="sinv-mat-card__actions">
              <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onDetail(material.id)}>
                Detail
              </button>
              <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onPrice(material)}>
                {canManageMasterData ? "Update unit price" : "Price"}
              </button>
              {canManage && (
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onEdit(material)}>
                  Edit
                </button>
              )}
            </div>
          </div>
        </article>
      );
    })}
  </div>
);

export default SilaMaterialCatalogGrid;
