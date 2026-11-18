import type { SilaRecipeDetail, SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import { linesFrom, type IngredientLine } from "./recipeEditorModel";
import { parseStudioDescriptionNotes, resolveRecipePos } from "./recipeStudioEditAccess";

export interface StudioLoadedForm {
  name: string;
  recipeCode: string;
  familyId: string;
  categoryId: string;
  cuisine: string;
  mealPeriod: string;
  yieldQty: string;
  servingUom: string;
  prepTime: string;
  menuItem: string;
  posCode: string;
  posItem: string;
  outletNote: string;
  classConfirmed: boolean;
  lines: IngredientLine[];
  outletPrices: Record<string, string>;
  currency: string;
}

export const formFromRecipeDetail = (
  detail: SilaRecipeDetail,
  activeRecipes: SilaRecipeListItem[] = [],
): StudioLoadedForm => {
  const notes = parseStudioDescriptionNotes(detail.description);
  const pos = resolveRecipePos(detail);
  return {
    name: detail.name ?? "",
    recipeCode: detail.recipeCode ?? "",
    familyId: detail.familyId ?? "",
    categoryId: detail.categoryId ?? "",
    cuisine: notes.cuisine,
    mealPeriod: notes.mealPeriod,
    yieldQty: String(detail.servingQty ?? 1),
    servingUom: detail.servingUom || "EA",
    prepTime: notes.prepTime,
    menuItem: pos.menuItem || notes.menuItem,
    posCode: pos.posCode,
    posItem: pos.posItem,
    outletNote: notes.outletNote
      || (detail.outletPrices ?? [])
        .map((price) => price.locationName || price.locationCode || price.outletLocationId)
        .filter(Boolean)
        .join(", "),
    classConfirmed: Boolean(detail.familyId || detail.categoryId),
    lines: linesFrom(detail, activeRecipes),
    outletPrices: Object.fromEntries(
      (detail.outletPrices ?? []).map((price) => [price.outletLocationId, String(price.menuPrice)]),
    ),
    currency: detail.currency ?? "",
  };
};
