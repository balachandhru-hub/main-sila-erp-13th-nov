/**
 * Step 6: Edit load, access, update/revision/submit wiring (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeStudioStep6.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { parseStudioDescriptionNotes, studioEditAccess } from "./recipeStudioEditAccess";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");

assert.deepEqual(studioEditAccess("DRAFT"), { kind: "editable" });
assert.equal(studioEditAccess("PENDING_APPROVAL").kind, "readonly");
assert.equal(studioEditAccess("APPROVED").kind, "revision-required");
assert.equal(studioEditAccess("REJECTED").kind, "revision-required");
assert.equal(studioEditAccess("INACTIVE").kind, "readonly");

const notes = parseStudioDescriptionNotes(
  "Cuisine: Italian · Meal period: Dinner · Prep: 12 min · Menu item note: Spritz · Outlet note: Beach bar",
);
assert.equal(notes.cuisine, "Italian");
assert.equal(notes.mealPeriod, "Dinner");
assert.equal(notes.prepTime, "12 min");
assert.equal(notes.menuItem, "Spritz");
assert.equal(notes.outletNote, "Beach bar");

const loadHelper = read("recipeStudioEditLoad.ts");
assert.match(loadHelper, /export const formFromRecipeDetail/);
assert.match(loadHelper, /linesFrom\(detail/);
assert.match(loadHelper, /parseStudioDescriptionNotes/);
assert.match(loadHelper, /outletPrices/);

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /getRecipe/);
assert.match(presentation, /updateRecipe/);
assert.match(presentation, /createRecipeRevision/);
assert.match(presentation, /submitRecipe/);
assert.match(presentation, /studioEditAccess/);
assert.match(presentation, /formFromRecipeDetail/);
assert.match(presentation, /recipe-studio-save-draft/);
assert.match(presentation, /recipe-studio-submit/);
assert.match(presentation, /recipe-studio-create-revision/);
assert.match(presentation, /handleCreateRevision/);
assert.match(presentation, /handleSubmit/);
assert.match(presentation, /skipNavigate/);
assert.doesNotMatch(presentation, /Awaiting existing-recipe loading in Step 6/);
assert.doesNotMatch(presentation, /Submit — Available in Step 6/);
assert.match(presentation, /Step 6 of 7/);
assert.match(presentation, /locked=\{!canEdit\}/);

const api = read("../../../api/silaMe/silaRecipeApi.ts");
assert.match(api, /\/recipes\/\$\{recipeId\}/);
assert.match(api, /createRecipeRevision/);
assert.match(api, /\/recipes\/\$\{recipeId\}\/submit/);

const shell = read("SilaRecipeWorkspaceShell.tsx");
assert.match(shell, /SilaRecipeStudioPresentation/);
assert.doesNotMatch(shell, /import SilaRecipeEditor/);

console.log("silaRecipeStudioStep6.test.ts: ok");
