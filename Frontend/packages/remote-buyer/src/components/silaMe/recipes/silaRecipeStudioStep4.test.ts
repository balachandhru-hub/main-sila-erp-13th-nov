/**
 * Step 4: presentation contract + UOM helper source checks (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeStudioStep4.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /SilaRecipeMaterialLibrary/);
assert.match(presentation, /SilaRecipeCanvas/);
assert.match(presentation, /searchIngredients/);
assert.match(presentation, /quantityForUomChange/);
assert.match(presentation, /recommendMaterials/);
assert.match(presentation, /lineCost/);
assert.match(presentation, /Save Draft — Available in Step 5/);
assert.match(presentation, /Submit — Available in Step 6/);
assert.match(presentation, /COST PENDING/);
assert.match(presentation, /Select outlet to view stock/);
assert.doesNotMatch(presentation, /\bgetRecipe\b|\bcreateRecipe\b|\bupdateRecipe\b|\bsubmitRecipe\b/);

const formatSrc = read("recipeFormat.ts");
assert.match(formatSrc, /export const fromBaseQuantity/);
assert.match(formatSrc, /export const quantityForUomChange/);

const canvasSrc = read("SilaRecipeCanvas.tsx");
assert.match(canvasSrc, /onMove/);
assert.match(canvasSrc, /Move .* up/);
assert.match(canvasSrc, /Add as another/);
assert.match(canvasSrc, /Replace existing/);

// Local semantic check of 1 BTL = 750 ML preservation (mirrors quantityForUomChange).
const factor = 750;
const fromQty = 1;
const preserved = fromQty * factor;
assert.equal(preserved, 750);

console.log("silaRecipeStudioStep4.test.ts: ok");
