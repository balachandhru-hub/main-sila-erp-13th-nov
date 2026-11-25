import type { SilaProductClass, SilaRecipeFormat, SilaRecipeMode } from "../../../api/silaMe/silaRecipeApi";

export const PRODUCT_CLASSES: { id: SilaProductClass; label: string }[] = [
  { id: "FOOD", label: "Food" },
  { id: "BEVERAGE", label: "Beverages" },
  { id: "MINIBAR", label: "Minibar" },
  { id: "TOBACCO", label: "Tobacco" },
];

export const RECIPE_FORMATS: { id: SilaRecipeFormat; label: string; itemMode: SilaRecipeMode }[] = [
  { id: "STANDARD_RECIPE", label: "Standard recipe", itemMode: "RECIPE" },
  { id: "DIRECT_ITEM", label: "Direct item", itemMode: "DIRECT" },
  { id: "BATCH_RECIPE", label: "Batch recipe", itemMode: "BATCH" },
  { id: "SUB_RECIPE", label: "Sub-recipe", itemMode: "BATCH" },
  { id: "COMBO", label: "Combo / bundle", itemMode: "BATCH" },
  { id: "VARIANT_RECIPE", label: "Variant", itemMode: "RECIPE" },
];

export interface RecipeTemplate {
  id: string;
  label: string;
  productClass: SilaProductClass;
  recipeFormat: SilaRecipeFormat;
  itemMode: SilaRecipeMode;
  subcategory: string;
  servingQty: string;
  servingUom: string;
  prepHint?: string;
}

/** Editable starter templates — never create approved recipes. */
export const RECIPE_TEMPLATES: RecipeTemplate[] = [
  {
    id: "main-course",
    label: "Main Course",
    productClass: "FOOD",
    recipeFormat: "STANDARD_RECIPE",
    itemMode: "RECIPE",
    subcategory: "Main Course",
    servingQty: "1",
    servingUom: "EA",
    prepHint: "Food dish with ingredients, yield and prep steps.",
  },
  {
    id: "cocktail",
    label: "Cocktail",
    productClass: "BEVERAGE",
    recipeFormat: "STANDARD_RECIPE",
    itemMode: "RECIPE",
    subcategory: "Cocktail",
    servingQty: "1",
    servingUom: "EA",
    prepHint: "Spirits, mixers and garnish in ML/EA.",
  },
  {
    id: "coffee",
    label: "Coffee",
    productClass: "BEVERAGE",
    recipeFormat: "VARIANT_RECIPE",
    itemMode: "RECIPE",
    subcategory: "Coffee",
    servingQty: "1",
    servingUom: "EA",
    prepHint: "Base coffee recipe — create size variants as drafts.",
  },
  {
    id: "batch-sauce",
    label: "Batch Sauce",
    productClass: "FOOD",
    recipeFormat: "BATCH_RECIPE",
    itemMode: "BATCH",
    subcategory: "Sauces",
    servingQty: "10",
    servingUom: "KG",
    prepHint: "Batch yield with portion conversion.",
  },
  {
    id: "minibar-item",
    label: "Minibar Item",
    productClass: "MINIBAR",
    recipeFormat: "DIRECT_ITEM",
    itemMode: "DIRECT",
    subcategory: "Snacks",
    servingQty: "1",
    servingUom: "EA",
    prepHint: "Direct Material Master mapping — no ingredient screen required beyond one material.",
  },
  {
    id: "shisha",
    label: "Shisha",
    productClass: "TOBACCO",
    recipeFormat: "STANDARD_RECIPE",
    itemMode: "RECIPE",
    subcategory: "Shisha",
    servingQty: "1",
    servingUom: "EA",
    prepHint: "Tobacco blend plus charcoal/foil consumables.",
  },
  {
    id: "combo-meal",
    label: "Combo Meal",
    productClass: "FOOD",
    recipeFormat: "COMBO",
    itemMode: "BATCH",
    subcategory: "Combo Meal",
    servingQty: "1",
    servingUom: "EA",
    prepHint: "Multi-component bundle — map each component; avoid double POS consumption.",
  },
];

export const defaultItemModeForClass = (productClass: SilaProductClass | string): SilaRecipeMode => {
  if (productClass === "MINIBAR" || productClass === "TOBACCO") return "DIRECT";
  return "RECIPE";
};

export const defaultFormatForMode = (itemMode: SilaRecipeMode): SilaRecipeFormat => {
  if (itemMode === "DIRECT") return "DIRECT_ITEM";
  if (itemMode === "BATCH") return "BATCH_RECIPE";
  return "STANDARD_RECIPE";
};

/** Show yield/waste/prep panels for standard food/beverage recipes — hide for packaged direct items. */
export const showChefYieldFields = (productClass: string, recipeFormat: string, itemMode: string): boolean => {
  if (itemMode === "DIRECT" || recipeFormat === "DIRECT_ITEM") return false;
  if (productClass === "MINIBAR" && recipeFormat !== "COMBO") return false;
  return true;
};

export const showPourSizeHint = (productClass: string): boolean =>
  productClass === "BEVERAGE";

export const formatProductClassLabel = (value?: string | null): string => {
  const found = PRODUCT_CLASSES.find((item) => item.id === value);
  return found?.label ?? value ?? "—";
};
