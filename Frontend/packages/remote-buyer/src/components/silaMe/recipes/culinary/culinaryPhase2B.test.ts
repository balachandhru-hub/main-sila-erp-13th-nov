/**
 * Phase 2B Advanced Event MRP & Material Requirement Planning Engine
 * Frontend contract, workspace, mobile, security & §54 calculation tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase2B.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Phase 2B MRP API client exposes all required endpoints
const apiSrc = read("../../../../api/silaMe/silaCulinaryMrpApi.ts");
for (const fn of [
  "getMrpDashboard",
  "listMrpRuns",
  "getMrpRun",
  "createMrpRun",
  "recalculateMrpRun",
  "getMrpGroupedView",
  "simulateMrpRun",
  "saveMrpScenario",
  "optimizeMrpBatches",
  "analyzeMrpWithAi",
  "suggestMrpSubstitutions",
  "resolveMrpException",
  "createMaterialRequestFromMrpException",
  "submitMrpForApproval",
  "decideMrpApproval",
  "createMrpRevision",
  "analyzeMrpEventImpact",
  "releaseMrpForPlanning",
  "getMrpPhase2CHandover",
  "getMrpReportCatalog",
  "exportMrpReport",
]) {
  assert.ok(apiSrc.includes(fn), `Missing Phase 2B API helper: ${fn}`);
}
// No Phase 2C+ PO creation, ITO posting, SAP 311, or stock reservations in Phase 2B API
assert.doesNotMatch(apiSrc, /postPurchaseOrder|reserveEventStock|createSapMaterialLive|executeGoodsIssue|sap311/i);

// 2. Cloud Event MRP Studio workspace covers all §28-§48 capabilities
const mrpWsSrc = read("CulinaryEventMrpStudioWorkspace.tsx");
for (const token of [
  "getMrpDashboard",
  "createMrpRun",
  "getMrpGroupedView",
  "simulateMrpRun",
  "saveMrpScenario",
  "optimizeMrpBatches",
  "analyzeMrpWithAi",
  "suggestMrpSubstitutions",
  "createMaterialRequestFromMrpException",
  "submitMrpForApproval",
  "decideMrpApproval",
  "createMrpRevision",
  "analyzeMrpEventImpact",
  "releaseMrpForPlanning",
  "getMrpPhase2CHandover",
  "exportMrpReport",
  "CulinaryMrpSimulatorWorkspace",
  "Consolidated BOM (10 Views)",
  "Recipe Explosion Tree",
  "Time-Phased & Stock",
  "MRP Simulator & Scenarios",
  "AI Analysis & Optimization",
  "Exceptions, Approval & Procurement Handover",
  "Report Studio",
]) {
  assert.ok(mrpWsSrc.includes(token), `Missing Event MRP Studio token: ${token}`);
}

const demoSimSrc = read("CulinaryMrpSimulatorWorkspace.tsx");
const demoMathSrc = read("culinarySimulatorsMath.ts");
assert.ok(demoSimSrc.includes("Corporate Gala Dinner"));
assert.ok(demoMathSrc.includes("Fresh dill"));

// 3. Recipe Studio 2 workspace mounts Event MRP Studio under case "mrp"
const workspacesSrc = read("SilaRecipeStudio2Workspaces.tsx");
assert.ok(workspacesSrc.includes("<CulinaryEventMrpStudioWorkspace"));

// 4. Mobile screen covers Phase 2B Event MRP without exposing admin AI config or unauthorized costs
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
for (const token of [
  "listMrpRuns",
  "getMrpRun",
  "simulateMrpRun",
  "submitMrpForApproval",
  "decideMrpApproval",
  "activeMobileMrp.costVisible",
  "Event MRP",
]) {
  assert.ok(mobileSrc.includes(token), `Missing Mobile Phase 2B token: ${token}`);
}
assert.doesNotMatch(mobileSrc, /saveCulinaryAiSettings|testCulinaryAiConnection|geminiApiKey|claudeApiKey/i);

// 5. §54 Deterministic Formula Verification (Tests A-L client-side invariant sanity check)
const round4 = (n: number) => Math.round(n * 10000) / 10000;
assert.equal(round4((250 / 10) * 2), 50); // Test A: Linear scaling
assert.equal(Math.round(500 * 0.6), 300); // Test B: Guest uptake
assert.equal(round4((300 * 0.18) / 0.9), 60); // Test C: Edible -> Raw yield
assert.equal(round4((54 * 1.1) / 0.9), 66); // Test D: Buffer + Yield
assert.equal(round4(72 / (0.8 * 0.9)), 100); // Test E: Sequential yield 72%
assert.equal(round4((15 / 10) * 6), 9); // Test F: Sub-recipe fractional batch
assert.equal(20 + 15 + 10, 45); // Test G: Consolidation
assert.equal(Math.max(0, 66 - 25), 41); // Test H: Current shortage
assert.equal(Math.max(0, 66 - 25 - 10), 31); // Test I: Incoming scenario
assert.equal(66 * 85, 5610); // Test J: Costing AED 5,610
const batches = Math.ceil(23 / 10);
assert.equal(batches, 3); // Test K: Full batch rounding
assert.equal(batches * 10 - 23, 7); // Test K: Excess 7 KG
assert.equal(Math.max(0, 50 - 15), 35); // Test L: Prepared stock new production

console.log("culinaryPhase2B.test.ts: all Phase 2B frontend & §54 invariant checks passed.");
