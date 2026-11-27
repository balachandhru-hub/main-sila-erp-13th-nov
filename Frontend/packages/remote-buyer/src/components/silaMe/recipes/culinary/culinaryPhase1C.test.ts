/**
 * Phase 1C Material Requests, Costing, Approvals, Recipe ID Creation & Publishing
 * Frontend contract, workspace, mobile & security guard tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase1C.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Default Phase 1C costing configuration matches spec defaults
const aiApiSrc = read("../../../../api/silaMe/silaCulinaryAiApi.ts");
for (const expectedToken of [
  "DEFAULT_CULINARY_COSTING_CONFIG",
  'reportingCurrency: "AED"',
  "defaultTargetFoodCostPercent: 28",
  'taxBasis: "TAX_EXCLUSIVE"',
  "maxSubRecipeDepth: 5",
]) {
  assert.ok(aiApiSrc.includes(expectedToken), `Missing Phase 1C costing config token: ${expectedToken}`);
}

// 2. Phase 1C Lifecycle API client exposes all required endpoints
const lcApiSrc = read("../../../../api/silaMe/silaCulinaryLifecycleApi.ts");
for (const fn of [
  "listMaterialRequests",
  "createMaterialRequest",
  "updateMaterialRequest",
  "bulkPrepareMaterialRequests",
  "assistMaterialRequest",
  "submitMaterialRequest",
  "resubmitMaterialRequest",
  "decideMaterialRequest",
  "executeMaterialCreation",
  "verifyManualErpMaterial",
  "getMaterialRequestDashboard",
  "exportMaterialRequestsReport",
  "calculateDraftCosting",
  "listCostingSnapshots",
  "optimizeDraftCosting",
  "bulkRecostRecipes",
  "getRecipeReadiness",
  "getApprovalPackage",
  "submitRecipeForApproval",
  "approveRecipeDraft",
  "rejectRecipeDraft",
  "createProductionRecipeId",
  "assignProductionRecipeImage",
  "publishProductionRecipe",
  "createNewProductionVersion",
  "getRecipeVersionHistory",
  "getLifecycleDashboard",
  "exportLifecycleReport",
  "getPhase2AHandover",
]) {
  assert.ok(lcApiSrc.includes(fn), `Missing Phase 1C API helper: ${fn}`);
}
// No Phase 2A+ Event Master, PO creation, or stock reservation endpoints
assert.doesNotMatch(lcApiSrc, /createEventMaster|postPurchaseOrder|reserveEventStock|createSapMaterialLive/i);

// 3. Cloud Material Requests & Food Cost Studio workspaces cover full Phase 1C UX
const mrqWsSrc = read("CulinaryMaterialRequestsWorkspace.tsx");
for (const token of [
  "NEW_MATERIAL",
  "MATERIAL_EXTENSION",
  "MATERIAL_REACTIVATION",
  "UOM_MAINTENANCE",
  "SPECIFICATION_CORRECTION",
  "CLASSIFICATION_MAINTENANCE",
  "MANUAL_REVIEW",
  "bulkPrepareMaterialRequests",
  "assistMaterialRequest",
  "submitMaterialRequest",
  "decideMaterialRequest",
  "executeMaterialCreation",
  "verifyManualErpMaterial",
  "exportMaterialRequestsReport",
]) {
  assert.ok(mrqWsSrc.includes(token), `Missing token in MaterialRequestsWorkspace: ${token}`);
}

const fcWsSrc = read("CulinaryFoodCostStudioWorkspace.tsx");
for (const token of [
  "calculateDraftCosting",
  "listCostingSnapshots",
  "optimizeDraftCosting",
  "getRecipeReadiness",
  "getApprovalPackage",
  "submitRecipeForApproval",
  "approveRecipeDraft",
  "rejectRecipeDraft",
  "createProductionRecipeId",
  "assignProductionRecipeImage",
  "publishProductionRecipe",
  "createNewProductionVersion",
  "getRecipeVersionHistory",
  "getPhase2AHandover",
  "exportLifecycleReport",
]) {
  assert.ok(fcWsSrc.includes(token), `Missing token in FoodCostStudioWorkspace: ${token}`);
}

// 4. Workspaces & Navigation wire Phase 1C workspaces cleanly
const workspacesSrc = read("SilaRecipeStudio2Workspaces.tsx");
const navSrc = read("SilaRecipeStudio2Nav.tsx");
assert.match(workspacesSrc, /CulinaryMaterialRequestsWorkspace/);
assert.match(workspacesSrc, /CulinaryFoodCostStudioWorkspace/);
assert.match(navSrc, /id:\s*"material-requests"/);
assert.match(navSrc, /id:\s*"food-cost"/);

// 5. AI Engine Setup exposes costingConfig
const aiSetupSrc = read("CulinaryAiEngineSetupWorkspace.tsx");
assert.match(aiSetupSrc, /costingConfig/);
assert.match(aiSetupSrc, /DEFAULT_CULINARY_COSTING_CONFIG/);

// 6. Mobile Phase 1C parity & strict admin/cost-visibility isolation
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
assert.match(mobileSrc, /'lifecycle'/);
assert.match(mobileSrc, /getRecipeReadiness/);
assert.match(mobileSrc, /calculateDraftCosting/);
assert.match(mobileSrc, /submitRecipeForApproval/);
assert.match(mobileSrc, /assignProductionRecipeImage/);
assert.match(mobileSrc, /createMaterialRequest/);
assert.match(mobileSrc, /canViewCostDetail === false/);
assert.doesNotMatch(mobileSrc, /getCulinaryAiConfig/);
assert.doesNotMatch(mobileSrc, /saveCulinaryAiConfig/);
assert.doesNotMatch(mobileSrc, /verifyManualErpMaterial/);

console.log("culinaryPhase1C.test.ts: ok");
