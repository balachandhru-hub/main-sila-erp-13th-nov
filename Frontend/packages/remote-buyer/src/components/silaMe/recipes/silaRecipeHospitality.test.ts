import assert from "node:assert/strict";
import {
  PRODUCT_CLASSES,
  RECIPE_TEMPLATES,
  defaultFormatForMode,
  defaultItemModeForClass,
  showChefYieldFields,
  showPourSizeHint,
} from "./recipeHospitality";
import { buildStudioCreateRequest, validateStudioDraft } from "./recipeStudioDraftSave";

assert.equal(PRODUCT_CLASSES.length, 4);
assert.equal(RECIPE_TEMPLATES.length, 7);
assert.equal(defaultItemModeForClass("MINIBAR"), "DIRECT");
assert.equal(defaultItemModeForClass("TOBACCO"), "DIRECT");
assert.equal(defaultItemModeForClass("FOOD"), "RECIPE");
assert.equal(defaultFormatForMode("DIRECT"), "DIRECT_ITEM");
assert.equal(defaultFormatForMode("BATCH"), "BATCH_RECIPE");
assert.equal(showChefYieldFields("FOOD", "STANDARD_RECIPE", "RECIPE"), true);
assert.equal(showChefYieldFields("MINIBAR", "DIRECT_ITEM", "DIRECT"), false);
assert.equal(showPourSizeHint("BEVERAGE"), true);
assert.equal(showPourSizeHint("FOOD"), false);

const quick = buildStudioCreateRequest({
  name: "Quick House Salad",
  familyId: "",
  categoryId: "",
  itemMode: "RECIPE",
  productClass: "FOOD",
  recipeFormat: "STANDARD_RECIPE",
  subcategory: "Appetizers",
  servingQty: "1",
  servingUom: "EA",
  trimLossPercent: "10",
  cookingLossPercent: "5",
  wasteFactorPercent: "2",
  prepTimeMinutes: "15",
  cookTimeMinutes: "20",
  preparationSteps: "1. Wash\n2. Toss",
}, [{
  materialId: "162be6a1-4ffc-406c-baa4-c632b9eb0d1b",
  quantity: "100",
  uom: "G",
}]);

assert.equal(quick.productClass, "FOOD");
assert.equal(quick.recipeFormat, "STANDARD_RECIPE");
assert.equal(quick.trimLossPercent, 10);
assert.equal(quick.cookingLossPercent, 5);
assert.equal(quick.wasteFactorPercent, 2);
assert.equal(quick.prepTimeMinutes, 15);
assert.equal(quick.cookTimeMinutes, 20);
assert.match(quick.preparationSteps ?? "", /Wash/);

const minibar = buildStudioCreateRequest({
  name: "Minibar Water",
  familyId: "",
  categoryId: "",
  itemMode: "DIRECT",
  productClass: "MINIBAR",
  recipeFormat: "DIRECT_ITEM",
  servingQty: "1",
  servingUom: "EA",
}, [{
  materialId: "162be6a1-4ffc-406c-baa4-c632b9eb0d1b",
  quantity: "1",
  uom: "EA",
}]);
assert.equal(minibar.itemMode, "DIRECT");
assert.equal(minibar.productClass, "MINIBAR");
assert.equal(Object.keys(validateStudioDraft({
  name: "x",
  familyId: "",
  categoryId: "",
  servingQty: "1",
  servingUom: "EA",
}, [{ materialId: "m1", quantity: "1", uom: "EA" }])).length, 0);

console.log("silaRecipeHospitality.test.ts OK");
