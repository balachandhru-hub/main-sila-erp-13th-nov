import type { SilaRecipeDetail, SilaRecipeWrite } from "../../../api/silaMe/silaRecipeApi";

const list = <T>(value: T[] | null | undefined): T[] => (Array.isArray(value) ? value : []);

/** Build a write payload from a loaded recipe detail (latest version rows). */
export const writeFromDetail = (detail: SilaRecipeDetail): SilaRecipeWrite => ({
  name: detail.name,
  description: detail.description ?? null,
  familyId: detail.familyId ?? null,
  categoryId: detail.categoryId ?? null,
  itemMode: detail.itemMode,
  servingQty: detail.servingQty,
  servingUom: detail.servingUom,
  sellingUom: detail.sellingUom || "EA",
  posCode: detail.posCode ?? null,
  posItem: detail.posItem ?? null,
  currency: detail.currency ?? null,
  ingredients: list(detail.ingredients).map((ingredient) => ({
    materialId: ingredient.materialId ?? null,
    subRecipeId: ingredient.subRecipeId ?? null,
    quantity: ingredient.quantity,
    uom: ingredient.uom || null,
  })),
  outletPrices: list(detail.outletPrices).map((price) => ({
    outletLocationId: price.outletLocationId,
    menuPrice: price.menuPrice,
    currency: price.currency ?? null,
  })),
});

/** True when saving / Create revision should bump Version (approved or rejected latest). */
export const canStartRevision = (status?: string | null): boolean => {
  const value = (status || "").toUpperCase();
  return value === "APPROVED" || value === "REJECTED";
};
