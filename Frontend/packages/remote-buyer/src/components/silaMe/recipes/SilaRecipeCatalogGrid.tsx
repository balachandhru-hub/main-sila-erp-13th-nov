import React from "react";
import type { SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import { costPerServing, formatCost, formatQty, recipeLabel } from "./recipeFormat";
import { formatProductClassLabel } from "./recipeHospitality";
import SilaRecipePicture from "./SilaRecipePicture";
import { RecipeCard, RecipeGrid } from "../studio";

interface SilaRecipeCatalogGridProps {
  rows: SilaRecipeListItem[];
  canManage: boolean;
  variant?: "standard" | "compact";
  onOpen: (recipeId: string) => void;
  onEdit?: (recipeId: string) => void;
  onDelete?: (recipe: SilaRecipeListItem) => void;
}

function mapStatusTone(status: string): "success" | "warning" | "danger" | "info" | "neutral" {
  switch (status) {
    case "ACTIVE":
      return "success";
    case "PENDING_APPROVAL":
      return "warning";
    case "REJECTED":
    case "INACTIVE":
      return "danger";
    case "DRAFT":
      return "info";
    default:
      return "neutral";
  }
}

/** Premium visual recipe catalog cards built on shared SILA Studio RecipeGrid & RecipeCard. */
const SilaRecipeCatalogGrid: React.FC<SilaRecipeCatalogGridProps> = ({
  rows,
  canManage,
  variant = "standard",
  onOpen,
  onEdit,
  onDelete,
}) => (
  <RecipeGrid variant={variant}>
    {rows.map((row) => {
      const portionCost =
        row.costComplete || row.activeVersion > 0
          ? (row.costPerServing ?? costPerServing(row.totalCost, row.servingQty))
          : null;
      const canEdit = canManage && Boolean(onEdit) && row.status !== "INACTIVE" && row.status !== "PENDING_APPROVAL";
      const canDeleteDraft = canManage && row.status === "DRAFT" && Boolean(onDelete);
      const canDeleteInactive = canManage && row.status === "INACTIVE" && Boolean(onDelete);

      const metaParts = [
        row.productClass ? formatProductClassLabel(row.productClass) : "",
        row.recipeFormat ? row.recipeFormat.replace(/_/g, " ") : "",
        row.category ?? "",
        row.familyName ?? "",
      ].filter(Boolean);

      const overflowActions = [];
      if (canDeleteDraft) {
        overflowActions.push({
          id: "delete-draft",
          label: "Delete Draft",
          danger: true,
          onClick: () => onDelete?.(row),
        });
      }
      if (canDeleteInactive) {
        overflowActions.push({
          id: "delete-permanent",
          label: "Delete Permanently",
          danger: true,
          onClick: () => onDelete?.(row),
        });
      }

      return (
        <RecipeCard
          key={row.id}
          compact={variant === "compact"}
          title={row.name}
          code={row.recipeCode}
          statusLabel={recipeLabel(row.status)}
          statusTone={mapStatusTone(row.status)}
          metaText={metaParts.join(" · ")}
          versionText={`v${row.version}${row.activeVersion > 0 && row.activeVersion !== row.version ? ` · selling v${row.activeVersion}` : ""}`}
          ingredientCount={row.ingredientCount}
          yieldText={`${formatQty(row.servingQty)} ${row.servingUom}`}
          costLabel={portionCost == null ? "COST PENDING" : formatCost(portionCost, row.currency)}
          posReference={row.posItem || row.posCode || undefined}
          media={
            <SilaRecipePicture
              recipeId={row.id}
              hasPicture={Boolean(row.hasPicture)}
              placeholderTitle={row.name}
              placeholderCategory={row.category}
              placeholderFamily={row.familyName}
              variant="catalog"
              locked
            />
          }
          onView={() => onOpen(row.id)}
          onEdit={canEdit ? () => onEdit?.(row.id) : undefined}
          moreActions={overflowActions}
        />
      );
    })}
  </RecipeGrid>
);

export default SilaRecipeCatalogGrid;
