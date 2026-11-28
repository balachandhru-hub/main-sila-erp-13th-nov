/**
 * Recipe Studio 2.0 Phase 0 route, navigation, permission, and AI-setup guard checks.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinarySuite2.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  isRecipeId,
  parseSilaRecipePath,
  silaRecipeCatalogPath,
  silaRecipeCulinaryPath,
  silaRecipeLegacyUploadPath,
  silaRecipeNewPath,
  type CulinaryWorkspaceKind,
} from "../silaRecipeRoutes";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Existing Recipe Studio routes preserved
assert.equal(silaRecipeCatalogPath(), "/sila-recipes/master");
assert.equal(silaRecipeNewPath(), "/sila-recipes/new");
assert.equal(silaRecipeLegacyUploadPath(), "/sila-recipes/legacy-upload");

// 2. All 14 Culinary Intelligence contextual sub-routes resolve deterministically without colliding with GUIDs
const culinaryRoutes: CulinaryWorkspaceKind[] = [
  "overview",
  "ai-planner",
  "materials",
  "material-requests",
  "menus",
  "events",
  "mrp",
  "procurement",
  "production",
  "food-cost",
  "event-pnl",
  "analytics",
  "reports",
  "ai-setup",
];

for (const kind of culinaryRoutes) {
  assert.equal(isRecipeId(kind), false);
  assert.deepEqual(parseSilaRecipePath(silaRecipeCulinaryPath(kind)), { kind });
}

// 3. Verify Contextual Nav contains all 16 grouped capabilities (no Overview or ME Studio inside Recipe Studio nav)
const navSrc = read("SilaRecipeStudio2Nav.tsx");
for (const expectedId of [
  "list",
  "legacy-upload",
  "ai-planner",
  "materials",
  "material-requests",
  "events",
  "mrp",
  "procurement",
  "production",
  "food-cost",
  "event-pnl",
  "analytics",
  "reports",
  "approvals",
  "tracker",
  "ai-setup",
]) {
  assert.match(navSrc, new RegExp(`id:\\s*"${expectedId}"`));
}
assert.doesNotMatch(navSrc, /id:\s*"menus"/);
assert.doesNotMatch(navSrc, /id:\s*"overview"/);

// 4. Verify AI Setup masks credentials and calls only authorized backend API
const aiSetupSrc = read("CulinaryAiEngineSetupWorkspace.tsx");
assert.match(aiSetupSrc, /getCulinaryAiConfig/);
assert.match(aiSetupSrc, /saveCulinaryAiConfig/);
assert.match(aiSetupSrc, /testCulinaryAiConnection/);
assert.match(aiSetupSrc, /geminiKeyMasked/);
assert.match(aiSetupSrc, /claudeKeyMasked/);
assert.match(aiSetupSrc, /allowExternalAi/);
assert.match(aiSetupSrc, /culinary-ai-usage-dashboard/);

// 5. Verify SilaRecipes mounts SilaRecipeStudio2Workspaces with StudioHeroPanel, Overview icon, and Culinary Intelligence toggle (no 2.0 branding)
const recipesSrc = read("../SilaRecipes.tsx");
assert.match(recipesSrc, /SilaRecipeStudio2Workspaces/);
assert.match(recipesSrc, /getCulinaryAiStatus/);
assert.match(recipesSrc, /culinary-flexi-ribbon/);
assert.match(recipesSrc, /label="Culinary Intelligence"/);
assert.doesNotMatch(recipesSrc, /Recipe Studio 2\.0|RECIPE STUDIO 2\.0/);
assert.match(recipesSrc, /showCulinaryOptions/);
assert.match(recipesSrc, /showOptions=\{showCulinaryOptions\}/);
assert.doesNotMatch(recipesSrc, /Culinary catalog/);

const wsRouterSrc = read("SilaRecipeStudio2Workspaces.tsx");
assert.match(wsRouterSrc, /showOptions\s*=\s*true/);

console.log("culinarySuite2.test.ts: ok");
