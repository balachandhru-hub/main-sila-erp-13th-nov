/**
 * Phase 1B Material Intelligence Engine — Frontend contract, workspace, mobile & security guard tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase1B.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Default Phase 1B matching configuration matches spec defaults
const aiApiSrc = read("../../../../api/silaMe/silaCulinaryAiApi.ts");
for (const expectedToken of [
  "exactMatchingEnabled: true",
  "aliasMatchingEnabled: true",
  "fuzzyMatchingEnabled: true",
  "semanticMatchingEnabled: true",
  "advancedRerankingEnabled: true",
  "mandatoryChefReview: true",
  'embeddingModel: "BAAI/bge-small-en-v1.5"',
  'rerankingModel: "Xenova/ms-marco-MiniLM-L-6-v2"',
]) {
  assert.ok(aiApiSrc.includes(expectedToken), `Missing default config token: ${expectedToken}`);
}

// 2. Material Intelligence API client exposes all 18 Phase 1B endpoints
const matApiSrc = read("../../../../api/silaMe/silaCulinaryMaterialsApi.ts");
for (const fn of [
  "getMaterialWorkspace",
  "matchDraftMaterials",
  "matchSingleIngredient",
  "recalculateMaterialRequirements",
  "confirmMaterialMapping",
  "bulkAcceptExactMappings",
  "rejectMaterialMapping",
  "prepareMaterialRequestDraft",
  "suggestIngredientSubstitutions",
  "applyIngredientSubstitution",
  "searchMaterialsForMapping",
  "getMaterialMappingHistory",
  "listMaterialAliases",
  "createMaterialAlias",
  "decideMaterialAlias",
  "getMaterialIndexStatus",
  "rebuildMaterialIndex",
  "getMaterialDashboard",
  "getPhase1CMaterialHandover",
  "exportMaterialReport",
]) {
  assert.ok(matApiSrc.includes(fn), `Missing Phase 1B API helper: ${fn}`);
}
// No SAP material creation, PO creation, or stock reservation endpoints
assert.doesNotMatch(matApiSrc, /createSapMaterial|postPurchaseOrder|reserveStock/i);

// 3. Cloud Material Intelligence workspace covers Phase 1B UX + Phase 1C handover + illustrative separation
const matWsSrc = read("CulinaryMaterialIntelligenceWorkspace.tsx");
assert.match(matWsSrc, /getMaterialWorkspace/);
assert.match(matWsSrc, /matchDraftMaterials/);
assert.match(matWsSrc, /recalculateMaterialRequirements/);
assert.match(matWsSrc, /confirmMaterialMapping/);
assert.match(matWsSrc, /bulkAcceptExactMappings/);
assert.match(matWsSrc, /rejectMaterialMapping/);
assert.match(matWsSrc, /suggestIngredientSubstitutions/);
assert.match(matWsSrc, /applyIngredientSubstitution/);
assert.match(matWsSrc, /prepareMaterialRequestDraft/);
assert.match(matWsSrc, /getPhase1CMaterialHandover/);
assert.match(matWsSrc, /getMaterialDashboard/);
assert.match(matWsSrc, /exportMaterialReport/);
assert.match(matWsSrc, /Illustrative/);
// Unknown stock/price must never be shown as 0
assert.match(matWsSrc, /stockStatus === "UNKNOWN"|Unknown/);

// 4. Phase 1A -> Phase 1B handover wired across Workspaces & Planner
const plannerSrc = read("CulinaryRecipePlannerWorkspace.tsx");
const workspacesSrc = read("SilaRecipeStudio2Workspaces.tsx");
assert.match(plannerSrc, /Continue to Material Mapping \(Phase 1B\)/);
assert.match(workspacesSrc, /handoverDraftId/);
assert.match(workspacesSrc, /sila\.culinary\.activeMaterialDraftId/);

// 5. AI Engine Setup includes bounded Phase 1B config & Index Admin
const aiSetupSrc = read("CulinaryAiEngineSetupWorkspace.tsx");
assert.match(aiSetupSrc, /materialConfig/);
assert.match(aiSetupSrc, /getMaterialIndexStatus/);
assert.match(aiSetupSrc, /rebuildMaterialIndex/);

// 6. Mobile Phase 1B screen parity & strict admin/credential isolation
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
assert.match(mobileSrc, /'materials'/);
assert.match(mobileSrc, /getMaterialWorkspace/);
assert.match(mobileSrc, /matchDraftMaterials/);
assert.match(mobileSrc, /confirmMaterialMapping/);
assert.match(mobileSrc, /rejectMaterialMapping/);
assert.match(mobileSrc, /searchMaterialsForMapping/);
assert.match(mobileSrc, /suggestIngredientSubstitutions/);
assert.match(mobileSrc, /applyIngredientSubstitution/);
assert.match(mobileSrc, /prepareMaterialRequestDraft/);
assert.doesNotMatch(mobileSrc, /getCulinaryAiConfig/);
assert.doesNotMatch(mobileSrc, /saveCulinaryAiConfig/);
assert.doesNotMatch(mobileSrc, /rebuildMaterialIndex/);

console.log("culinaryPhase1B.test.ts: ok");
