/**
 * Guard: Recipe Studio must stay in Recipe Management nav; dashboard is merged into Studio.
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeStudioNavGuard.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(__dirname, rel), "utf8");

const nav = read("../silaMeNav.tsx");
assert.match(nav, /label: "Recipe Studio"/);
assert.match(nav, /key: "silaRecipes"/);
assert.match(nav, /"silaPos", "POS Studio"/);
// Separate Recipe Dashboard nav entry must not return under Recipe Management.
const recipeMgmt = nav.slice(nav.indexOf('label: "Recipe Management"'), nav.indexOf('label: "Receiving"'));
assert.doesNotMatch(recipeMgmt, /label: "Dashboard"/);
assert.doesNotMatch(recipeMgmt, /key: "silaRecipeDashboard"/);

const workspace = read("../SilaMeWorkspace.tsx");
assert.match(workspace, /case "silaRecipes"/);
assert.match(workspace, /<SilaRecipes/);
assert.match(workspace, /folded into Recipe Studio/);

const recipes = read("SilaRecipes.tsx");
assert.match(recipes, /SILA Recipe Studio/);
assert.match(recipes, /SilaRecipeWorkspaceShell/);
assert.match(recipes, /getRecipeDashboard/);
assert.match(recipes, /recipe-studio-dashboard-kpis/);

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /SilaRecipeStudioPresentation/);

console.log("silaRecipeStudioNavGuard.test.ts: ok");
