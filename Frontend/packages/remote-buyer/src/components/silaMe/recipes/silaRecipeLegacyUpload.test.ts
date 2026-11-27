/**
 * Legacy Recipe Upload — additive Recipe Studio migration workspace guards.
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeLegacyUpload.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  parseSilaRecipePath,
  silaRecipeLegacyUploadPath,
} from "./silaRecipeRoutes";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), "utf8");

assert.equal(silaRecipeLegacyUploadPath(), "/sila-recipes/legacy-upload");
assert.deepEqual(parseSilaRecipePath("/sila-recipes/legacy-upload"), { kind: "legacy-upload" });
assert.deepEqual(parseSilaRecipePath("/sila-recipes/new"), { kind: "new" });
assert.equal(parseSilaRecipePath("/sila-recipes").kind, "overview");
assert.equal(parseSilaRecipePath("/sila-recipes/master").kind, "list");

const studio = read("SilaRecipeLegacyUpload.tsx");
assert.match(studio, /data-testid="recipe-legacy-upload"/);
assert.match(studio, /Legacy Recipe Upload/);
assert.match(studio, /New Upload/);
assert.match(studio, /Validation & Preview/);
assert.match(studio, /Pending Approval/);
assert.match(studio, /Ready to Post/);
assert.match(studio, /Image Assignment/);
assert.match(studio, /Publishing/);
assert.match(studio, /Upload History/);
assert.match(studio, /downloadLegacyRecipeTemplate/);
assert.match(studio, /uploadLegacyRecipeBatch/);
assert.match(studio, /validateLegacyRecipeBatch/);
assert.match(studio, /submitLegacyBatchApproval/);
assert.match(studio, /decideLegacyBatch/);
assert.match(studio, /postLegacyBatch/);
assert.match(studio, /uploadLegacyRecipeImages/);
assert.match(studio, /assignLegacyRecipeImages/);
assert.match(studio, /publishLegacyBatch/);
assert.match(studio, /reprocessLegacyFailed/);

const recipes = read("SilaRecipes.tsx");
assert.match(recipes, /Legacy Recipe Upload/);
assert.match(recipes, /silaRecipeLegacyUploadPath/);
assert.match(recipes, /recipe-legacy-upload-entry/);
assert.match(recipes, /kind === "legacy-upload"/);
assert.match(recipes, /SilaRecipeLegacyUpload/);
// Preserve catalog Filters drawer toggle + compact brand hero.
assert.match(recipes, /showFiltersDrawer/);
assert.match(recipes, /recipe-category-filters-toggle/);
assert.match(recipes, /title="Recipe Studio"/);
assert.doesNotMatch(recipes, /Culinary catalog/);

const routes = read("silaRecipeRoutes.ts");
assert.match(routes, /legacy-upload/);
assert.match(routes, /silaRecipeLegacyUploadPath/);

const api = readFileSync(join(dir, "../../../api/silaMe/silaRecipeLegacyApi.ts"), "utf8");
assert.match(api, /\/api\/v1\/buyer\/sila\/recipe-legacy/);
assert.match(api, /\$\{BASE\}\/template/);
assert.match(api, /\$\{BASE\}\/upload/);
assert.match(api, /submit-approval/);
assert.match(api, /images\/assign/);
assert.match(api, /reprocess-failed/);

const nav = read("../silaMeNav.tsx");
const recipeMgmt = nav.slice(nav.indexOf('label: "Recipe Management"'), nav.indexOf('label: "Receiving"'));
assert.doesNotMatch(recipeMgmt, /Legacy Recipe Upload/);
assert.match(recipeMgmt, /label: "Recipe Studio"/);

const workspace = read("../SilaMeWorkspace.tsx");
assert.match(workspace, /canApprove=\{can\.approveRecipe\}/);

console.log("silaRecipeLegacyUpload.test.ts: ok");
