/**
 * Phase 1A AI Recipe Generation Engine — Frontend deterministic math, diffing & guard tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase1A.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  BUTTER_CHICKEN_TEST_INGREDIENTS,
  compareRecipePayloads,
  scaleDraftLocally,
} from "./culinaryRecipePlannerHelpers";
import type { CulinaryStructuredRecipePayload } from "../../../../api/silaMe/silaCulinaryRecipesApi";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. §33 Illustrative Butter Chicken 10 -> 50 linear scaling test
const scaled50 = scaleDraftLocally({
  recipeName: "Butter Chicken",
  baseServings: 10,
  targetServings: 50,
  baseServingSize: 200,
  targetServingSize: 200,
  prepBufferPercent: 0,
  useNonlinearRules: true,
  ingredients: BUTTER_CHICKEN_TEST_INGREDIENTS,
});

assert.equal(scaled50.linear_factor, 5);
const expected50: Record<string, number> = {
  "Boneless chicken": 9,
  "Tomato purée": 6,
  Butter: 1.25,
  "Fresh cream": 2,
  "Ginger garlic paste": 0.6,
  Yogurt: 1.5,
  "Garam masala": 0.125,
  "Kashmiri chilli powder": 0.1,
  "Kasuri methi": 0.05,
  Salt: 0.125,
};

for (const item of scaled50.ingredients) {
  assert.equal(item.scaled_raw_qty, expected50[item.description], `Mismatch for ${item.description}`);
}
const garamMasala = scaled50.ingredients.find((i) => i.description === "Garam masala");
assert.equal(garamMasala?.requires_manual_review, true);

// 2. Scale 50 -> 10 roundtrip consistency (starting from 50-serving baseline)
const baseline50 = BUTTER_CHICKEN_TEST_INGREDIENTS.map((i) => ({
  ...i,
  quantity: expected50[i.description],
}));
const scaledBack10 = scaleDraftLocally({
  recipeName: "Butter Chicken",
  baseServings: 50,
  targetServings: 10,
  baseServingSize: 200,
  targetServingSize: 200,
  prepBufferPercent: 0,
  useNonlinearRules: false,
  ingredients: baseline50,
});
assert.equal(scaledBack10.linear_factor, 0.2);
assert.equal(scaledBack10.ingredients[0].scaled_raw_qty, 1.8);

// 3. Yield & Scaling Method checks (Edible 10 kg at 80% yield -> 12.5 kg raw, no double yield)
const yieldAndMethods = scaleDraftLocally({
  recipeName: "Yield & Modes",
  baseServings: 10,
  targetServings: 20,
  baseServingSize: 180,
  targetServingSize: 180,
  prepBufferPercent: 0,
  useNonlinearRules: false,
  ingredients: [
    {
      ...BUTTER_CHICKEN_TEST_INGREDIENTS[0],
      temp_ref: "Y-1",
      description: "Edible chicken breast",
      quantity: 5,
      quantity_basis: "EDIBLE",
      yield_percent: 80,
      scaling_method: "LINEAR",
    },
    {
      ...BUTTER_CHICKEN_TEST_INGREDIENTS[0],
      temp_ref: "F-1",
      description: "Bay leaf",
      quantity: 2,
      recipe_uom: "EA",
      scaling_method: "FIXED_QUANTITY",
    },
    {
      ...BUTTER_CHICKEN_TEST_INGREDIENTS[0],
      temp_ref: "B-1",
      description: "Brioche tray",
      quantity: 1,
      recipe_uom: "EA",
      scaling_method: "BATCH_BASED",
    },
  ],
});

const y1 = yieldAndMethods.ingredients.find((i) => i.temp_ref === "Y-1");
assert.equal(y1?.scaled_edible_qty, 10);
assert.equal(y1?.scaled_raw_qty, 12.5);

const f1 = yieldAndMethods.ingredients.find((i) => i.temp_ref === "F-1");
assert.equal(f1?.scaled_raw_qty, 2);

const b1 = yieldAndMethods.ingredients.find((i) => i.temp_ref === "B-1");
assert.equal(b1?.scaled_raw_qty, 2);

// 4. Revision diffing check
const basePayload: CulinaryStructuredRecipePayload = {
  header: {
    proposed_name: "Butter Chicken",
    description: "Classic",
    cuisine: "Indian",
    category: "Main Course",
    recipe_type: "STANDARD_RECIPE",
    base_servings: 10,
    serving_size: 180,
    serving_uom: "G",
    batch_size: null,
    expected_yield_qty: 1.8,
    expected_yield_uom: "KG",
    prep_duration_minutes: 30,
    cook_duration_minutes: 45,
    total_duration_minutes: 75,
    cooking_method: "Simmer",
    difficulty: "MEDIUM",
    dietary_tags: [],
    potential_allergens: ["Dairy"],
    cross_contact_warnings: [],
    storage_guidance: "Chill below 4C",
    shelf_life_suggestion: "48 hours",
    chef_verification_required: true,
    nutrition_status: "UNAVAILABLE_OR_UNVERIFIED",
  },
  ingredients: BUTTER_CHICKEN_TEST_INGREDIENTS,
  instructions: [],
  sub_recipes: [],
  assumptions: [],
  warnings: [],
  chef_notes: "",
  visually_observed_characteristics: [],
  inferred_uncertain_items: [],
  cost_validation_note: "Cost validation pending material matching.",
};
const modifiedPayload: CulinaryStructuredRecipePayload = {
  ...basePayload,
  header: { ...basePayload.header, base_servings: 50 },
  ingredients: basePayload.ingredients.map((i, idx) =>
    idx === 0 ? { ...i, description: "Paneer", quantity: 9 } : i
  ),
};
const diffs = compareRecipePayloads(basePayload, modifiedPayload);
assert.ok(diffs.some((d) => d.field === "Base Servings" && d.before === "10" && d.after === "50"));
assert.ok(diffs.some((d) => d.field === "Paneer"));

// 5. Cloud & Mobile security / handover guard checks
const cloudPlannerSrc = read("CulinaryRecipePlannerWorkspace.tsx");
assert.match(cloudPlannerSrc, /generateCulinaryRecipe/);
assert.match(cloudPlannerSrc, /refineCulinaryRecipe/);
assert.match(cloudPlannerSrc, /regenerateCulinaryRecipeSection/);
assert.match(cloudPlannerSrc, /extractCulinaryRecipeDocument/);
assert.match(cloudPlannerSrc, /prepareCulinaryDishImage/);
assert.match(cloudPlannerSrc, /Material Mapping/);

const mobileScreenSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
assert.match(mobileScreenSrc, /getCulinaryAiStatus/);
assert.match(mobileScreenSrc, /generateCulinaryRecipe/);
assert.doesNotMatch(mobileScreenSrc, /getCulinaryAiConfig/);
assert.doesNotMatch(mobileScreenSrc, /saveCulinaryAiConfig/);

console.log("culinaryPhase1A.test.ts: ok");
