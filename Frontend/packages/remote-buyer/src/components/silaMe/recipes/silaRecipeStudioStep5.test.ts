/**
 * Step 5: Save Draft payload/validation contract (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeStudioStep5.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  buildStudioCreateRequest,
  validateStudioDraft,
  verifyPersistedDraft,
} from "./recipeStudioDraftSave";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");

const lines = [
  {
    materialId: "162be6a1-4ffc-406c-baa4-c632b9eb0d1b",
    subRecipeId: null,
    quantity: "2",
    uom: "EA",
  },
];

assert.deepEqual(validateStudioDraft({
  name: "",
  familyId: "",
  categoryId: "",
  servingQty: "1",
  servingUom: "EA",
}, lines).name, "Enter the recipe name.");

assert.match(
  validateStudioDraft({
    name: "Demo",
    familyId: "",
    categoryId: "",
    servingQty: "0",
    servingUom: "EA",
  }, lines).serving ?? "",
  /greater than zero/i,
);

assert.match(
  validateStudioDraft({
    name: "Demo",
    familyId: "",
    categoryId: "",
    servingQty: "1",
    servingUom: "EA",
  }, []).ingredients ?? "",
  /at least one ingredient/i,
);

const request = buildStudioCreateRequest({
  name: "Step 5 Draft Cocktail",
  description: "Cuisine: Test",
  familyId: "",
  categoryId: "",
  servingQty: "4",
  servingUom: "EA",
}, lines);
assert.equal(request.name, "Step 5 Draft Cocktail");
assert.equal(request.itemMode, "RECIPE");
assert.equal(request.servingQty, 4);
assert.equal(request.ingredients.length, 1);
assert.equal(request.ingredients[0].materialId, "162be6a1-4ffc-406c-baa4-c632b9eb0d1b");
assert.equal(request.ingredients[0].quantity, 2);
assert.equal(request.ingredients[0].uom, "EA");
assert.equal(request.posCode, null);
assert.equal(request.posItem, null);
assert.deepEqual(request.outletPrices, []);

const withPos = buildStudioCreateRequest({
  name: "Step 5 Draft Cocktail",
  familyId: "",
  categoryId: "",
  servingQty: "4",
  servingUom: "EA",
  posCode: "34567",
  posItem: "34567",
}, lines, [{
  outletLocationId: "4ed928b7-d4d9-42bf-8f41-908e3b0fef54",
  menuPrice: 190,
  currency: "AED",
  posCode: "34567",
  posItem: "34567",
}]);
assert.equal(withPos.posCode, "34567");
assert.equal(withPos.posItem, "34567");
assert.equal(withPos.outletPrices.length, 1);
assert.equal(withPos.outletPrices[0].posCode, "34567");
assert.equal(withPos.outletPrices[0].posItem, "34567");

assert.equal(verifyPersistedDraft({
  name: "Step 5 Draft Cocktail",
  status: "DRAFT",
  ingredients: [{
    sequence: 1,
    materialId: "162be6a1-4ffc-406c-baa4-c632b9eb0d1b",
    quantity: 2,
    uom: "EA",
  }],
}, request).ok, true);

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /createRecipe/);
assert.match(presentation, /getRecipe/);
assert.match(presentation, /handleSaveDraft/);
assert.match(presentation, /recipe-studio-save-draft/);
assert.match(presentation, /createRecipe/);
assert.match(presentation, /updateRecipe/);

const recipes = read("SilaRecipes.tsx");
assert.match(recipes, /onDraftSaved=\{goDetail\}/);

console.log("silaRecipeStudioStep5.test.ts: ok");
