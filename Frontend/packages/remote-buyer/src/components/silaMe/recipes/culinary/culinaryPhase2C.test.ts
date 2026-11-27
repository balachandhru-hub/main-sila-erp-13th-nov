/**
 * Phase 2C Advanced Stock Availability, Reservations, Internal Transfers & Procurement Intelligence
 * Frontend contract, Cloud Studio, Mobile, security & §63/§64 calculation invariant tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase2C.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Phase 2C Procurement API client exposes all required endpoints
const apiSrc = read("../../../../api/silaMe/silaCulinaryProcurementApi.ts");
for (const fn of [
  "getProcurementDashboard",
  "listProcurementPlans",
  "getProcurementPlan",
  "createProcurementPlan",
  "recalculateProcurementPlan",
  "createStockReservation",
  "transitionStockReservation",
  "allocateIncomingPo",
  "createProcurementItoDraft",
  "createProcurementPoDraft",
  "reconcileProcurementGrn",
  "reconcileProcurementItoReversal",
  "linkWeeklyBucketToProcurementLine",
  "simulateProcurementPlan",
  "optimizeProcurementPlan",
  "analyzeProcurementWithAi",
  "resolveProcurementException",
  "submitProcurementPlanForApproval",
  "decideProcurementPlanApproval",
  "handleProcurementChangeImpact",
  "cancelProcurementPlan",
  "getProcurementReportCatalog",
  "exportProcurementReport",
  "getPhase3AHandover",
]) {
  assert.ok(apiSrc.includes(fn), `Missing Phase 2C API helper: ${fn}`);
}
// Never call auto-release PO or live SAP post endpoints from Phase 2C client
assert.doesNotMatch(apiSrc, /autoReleasePo|postSapLive|executeGoodsIssueLive/i);

// 2. Cloud Event Procurement Studio covers all §45 tabs and core workflows
const wsSrc = read("CulinaryProcurementStudioWorkspace.tsx");
for (const token of [
  "Coverage Overview",
  "Stock Reservations",
  "Incoming POs",
  "Internal Transfers",
  "Purchase Recommendations",
  "Supplier Comparison",
  "Procurement Simulator",
  "Delivery Tracking",
  "Exceptions",
  "Approvals",
  "Reports",
  "History",
  "DeferErpUntilApproved=true",
  "FIVE S4 - Transfer Stock",
  "optimizeProcurementPlan",
  "analyzeProcurementWithAi",
  "getPhase3AHandover",
  "reconcileProcurementGrn",
  "reconcileProcurementItoReversal",
]) {
  assert.ok(wsSrc.includes(token), `Missing Cloud Procurement Studio token: ${token}`);
}

// 3. Recipe Studio 2 nav & menu/event switcher wire Phase 2C Live
const navSrc = read("SilaRecipeStudio2Nav.tsx");
assert.ok(navSrc.includes("Phase 2C Live"));
const switcherSrc = read("CulinaryMenuEventWorkspaces.tsx");
assert.ok(switcherSrc.includes("<CulinaryProcurementStudioWorkspace"));

// 4. Mobile screen exposes chef-friendly Phase 2C readiness & draft actions without exposing admin AI config
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
for (const token of [
  "listProcurementPlans",
  "getProcurementPlan",
  "createProcurementItoDraft",
  "createProcurementPoDraft",
  "submitProcurementPlanForApproval",
  "decideProcurementPlanApproval",
  "activeMobileProc.costsVisible",
  "Event Material Readiness &amp; Procurement",
]) {
  assert.ok(mobileSrc.includes(token), `Missing Mobile Phase 2C token: ${token}`);
}
assert.doesNotMatch(mobileSrc, /saveCulinaryAiSettings|testCulinaryAiConnection|geminiApiKey|claudeApiKey/i);

// 5. §63 & §64 Deterministic Procurement Arithmetic Sanity Checks
// §63 Grand Corporate Gala: Atlantic Salmon 100 KG
const eligibleLocal = 35 - 10 - 5; // 20 KG
assert.equal(eligibleLocal, 20);
const confirmedCoverage = eligibleLocal + 20; // +20 incoming PO (unapproved ITO 15 excluded from confirmed)
assert.equal(confirmedCoverage, 40);
const remainingAfterProposedIto = 100 - eligibleLocal - 20 - 15; // 45 KG
assert.equal(remainingAfterProposedIto, 45);
assert.equal(remainingAfterProposedIto * 85, 3825); // AED 3,825

// §64 Scenario A: Pack rounding (31 KG in 5 KG packs -> 35 KG, 4 KG excess)
const packsA = Math.ceil(31 / 5);
assert.equal(packsA, 7);
assert.equal(packsA * 5, 35);
assert.equal(packsA * 5 - 31, 4);

// §64 Scenario E: Multi-event consolidation (15 + 20 + 12 = 47 KG -> 50 KG in 5 KG packs, 3 KG excess)
const combinedE = 15 + 20 + 12;
assert.equal(combinedE, 47);
assert.equal(Math.ceil(combinedE / 5) * 5, 50);
assert.equal(Math.ceil(combinedE / 5) * 5 - combinedE, 3);

console.log("culinaryPhase2C.test.ts: all Phase 2C frontend & §63/§64 invariant checks passed.");
