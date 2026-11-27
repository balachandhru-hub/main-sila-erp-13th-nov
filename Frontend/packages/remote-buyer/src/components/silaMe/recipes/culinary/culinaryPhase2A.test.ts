/**
 * Phase 2A Advanced AI Menu Studio & Event Planner
 * Frontend contract, workspace, mobile & security guard tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase2A.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Phase 2A Event & Menu API client exposes all required endpoints
const apiSrc = read("../../../../api/silaMe/silaCulinaryEventMenuApi.ts");
for (const fn of [
  "getEventDashboard",
  "listCulinaryEvents",
  "getCulinaryEvent",
  "createCulinaryEvent",
  "updateCulinaryEvent",
  "duplicateCulinaryEvent",
  "cancelCulinaryEvent",
  "listCulinaryEventTemplates",
  "saveCulinaryEventTemplate",
  "createEventFromTemplate",
  "upsertCulinaryEventSession",
  "deleteCulinaryEventSession",
  "listCulinaryMenus",
  "getCulinaryMenu",
  "createCulinaryMenu",
  "upsertCulinaryMenuLine",
  "removeCulinaryMenuLine",
  "reorderCulinaryMenuLines",
  "searchPublishedCulinaryRecipes",
  "createMissingRecipeDraftFromMenuLine",
  "syncPublishedRecipeToMenuLine",
  "generateCulinaryAiMenu",
  "optimizeCulinaryEventMenu",
  "verifyCulinaryEventDietary",
  "getCulinaryEventAllergenMatrix",
  "getCulinaryEventMaterialPreview",
  "simulateCulinaryEvent",
  "createCulinaryEventScenario",
  "selectCulinaryEventScenario",
  "getCulinaryCustomerMenuPreview",
  "getCulinaryEventReadiness",
  "submitCulinaryEventForApproval",
  "decideCulinaryEventApproval",
  "createCulinaryEventRevision",
  "listCulinaryEventRevisions",
  "exportCulinaryEventReport",
  "getCulinaryEventPhase2BHandover",
]) {
  assert.ok(apiSrc.includes(fn), `Missing Phase 2A API helper: ${fn}`);
}
// No Phase 2C+ PO creation, ITO posting, or stock reservations in Phase 2A API
assert.doesNotMatch(apiSrc, /postPurchaseOrder|reserveEventStock|createSapMaterialLive|executeGoodsIssue/i);

// 2. Cloud Menu Studio & Event Planner workspaces cover full Phase 2A UX
const menuWsSrc = read("CulinaryMenuStudioWorkspace.tsx");
for (const token of [
  "searchPublishedCulinaryRecipes",
  "createCulinaryMenu",
  "upsertCulinaryMenuLine",
  "reorderCulinaryMenuLines",
  "createMissingRecipeDraftFromMenuLine",
  "syncPublishedRecipeToMenuLine",
  "recipeVersionOutdated",
]) {
  assert.ok(menuWsSrc.includes(token), `Missing Menu Studio token: ${token}`);
}

const eventWsSrc = read("CulinaryEventPlannerWorkspace.tsx");
for (const token of [
  "getEventDashboard",
  "Event Calendar",
  "Guided Event Setup Wizard",
  "upsertCulinaryEventSession",
  "verifyCulinaryEventDietary",
  "generateCulinaryAiMenu",
  "optimizeCulinaryEventMenu",
  "getCulinaryEventMaterialPreview",
  "simulateCulinaryEvent",
  "createCulinaryEventScenario",
  "getCulinaryCustomerMenuPreview",
  "getCulinaryEventReadiness",
  "submitCulinaryEventForApproval",
  "decideCulinaryEventApproval",
  "createCulinaryEventRevision",
  "exportCulinaryEventReport",
  "getCulinaryEventPhase2BHandover",
  "hasVenueConflict",
]) {
  assert.ok(eventWsSrc.includes(token), `Missing Event Planner token: ${token}`);
}

// 3. Nav & Shell wiring preserve existing procurement & food-cost previews while activating Phase 2A
const navSrc = read("SilaRecipeStudio2Nav.tsx");
assert.ok(navSrc.includes('badge: "Phase 2A Live"'));
const wrapperSrc = read("CulinaryMenuEventWorkspaces.tsx");
assert.ok(wrapperSrc.includes("<CulinaryMenuStudioWorkspace"));
assert.ok(wrapperSrc.includes("<CulinaryEventPlannerWorkspace"));
assert.ok(wrapperSrc.includes("<CulinaryProcurementStudioWorkspace"));

// 4. Mobile screen covers Phase 2A Event Planner without admin AI settings/secrets
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
for (const token of [
  "listCulinaryEvents",
  "getCulinaryEvent",
  "simulateCulinaryEvent",
  "submitCulinaryEventForApproval",
  "decideCulinaryEventApproval",
]) {
  assert.ok(mobileSrc.includes(token), `Missing Mobile Phase 2A token: ${token}`);
}
assert.doesNotMatch(mobileSrc, /saveCulinaryAiSettings|testCulinaryAiConnection|geminiApiKey|claudeApiKey/i);

console.log("culinaryPhase2A.test.ts: all Phase 2A frontend checks passed.");
