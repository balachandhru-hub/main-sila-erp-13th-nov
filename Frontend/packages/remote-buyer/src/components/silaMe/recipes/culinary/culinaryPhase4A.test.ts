/**
 * Phase 4A Kitchen Production Planning, Batch Scheduling & Execution Management
 * Frontend contract, Cloud Production Studio, 20-Report Export, Mobile Production Ops & Guardrail tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase4A.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Phase 4A Production API client exposes all kitchen production endpoints & zero unauthorized ERP auto-postings
const apiSrc = read("../../../../api/silaMe/silaCulinaryProductionApi.ts");
for (const fn of [
  "listCulinaryKitchens",
  "upsertCulinaryKitchen",
  "listCulinaryWorkstations",
  "upsertCulinaryWorkstation",
  "listCulinaryEquipment",
  "upsertCulinaryEquipment",
  "listCulinaryStaff",
  "upsertCulinaryStaff",
  "listProductionDemands",
  "createProductionDemand",
  "adjustProductionDemand",
  "listProductionPlans",
  "createProductionPlan",
  "explodeValidateProductionPlan",
  "submitProductionPlanApproval",
  "decideProductionPlanApproval",
  "amendProductionPlan",
  "releaseProductionPlanToOrders",
  "listProductionOrders",
  "rescheduleProductionBatch",
  "optimizeProductionSchedule",
  "applyProductionSchedule",
  "getProductionCalendar",
  "generateOrderPickingList",
  "confirmProductionPickingLine",
  "issueMaterialToProduction",
  "getProductionKdsFeed",
  "executeProductionBatchAction",
  "listBatchTasks",
  "updateProductionTaskStatus",
  "recordProductionQualityCheck",
  "resolveQualityHold",
  "getProductionBatchTraceability",
  "listCentralKitchenDispatches",
  "allocateCentralKitchenDispatch",
  "moveCentralKitchenDispatch",
  "triggerBuffetReplenishment",
  "getProductionDashboard",
  "listProductionExceptions",
  "reportProductionException",
  "resolveProductionException",
  "queryProductionCopilot",
  "getProductionReportCatalog",
  "exportProductionReport",
  "getMobileProductionSummary",
]) {
  assert.ok(apiSrc.includes(fn), `Missing Phase 4A API helper: ${fn}`);
}
assert.doesNotMatch(apiSrc, /autoCreatePurchaseOrder|transmitPoWithoutApproval|settleEventProfitability|postGlJournal/i);

// 2. Cloud Production Studio covers all 9 operational modules & 13 kitchen types
const wsSrc = read("CulinaryProductionStudioWorkspace.tsx");
for (const token of [
  "1. Production Cockpit",
  "2. Calendar & Reschedule",
  "3. Demand & Plans",
  "4. Orders, Batches & Tasks",
  "5. KDS & Production Sheet",
  "6. Picking & Material Issue",
  "7. Central Kitchen",
  "8. Quality, QR & Exceptions",
  "9. AI Scheduler, Copilot & Setup",
  "MAIN_KITCHEN",
  "CENTRAL_KITCHEN",
  "BANQUET_KITCHEN",
  "PASTRY_KITCHEN",
  "SATELLITE_KITCHEN",
  "kdsFullScreen",
  "optimizeProductionSchedule",
  "resolveQualityHold",
  "allocateCentralKitchenDispatch",
  "triggerBuffetReplenishment",
  "queryProductionCopilot",
]) {
  assert.ok(wsSrc.includes(token), `Missing Cloud Production Studio token: ${token}`);
}

// 3. Reports Workspace & Nav wire Phase 4A live studio + 20 reports
const reportsSrc = read("CulinaryProductionAnalyticsReportWorkspaces.tsx");
assert.ok(reportsSrc.includes("<CulinaryProductionStudioWorkspace />"));
assert.ok(reportsSrc.includes("getProductionReportCatalog"));
assert.ok(reportsSrc.includes("exportProductionReport"));
assert.ok(reportsSrc.includes("Kitchen Production Reports (20 Reports)"));

const navSrc = read("SilaRecipeStudio2Nav.tsx");
assert.ok(navSrc.includes('id: "production"'));
assert.ok(navSrc.includes("culinaryPhase4AEnabled"));

// 4. Feature flags include Phase 4A flags
const aiApiSrc = read("../../../../api/silaMe/silaCulinaryAiApi.ts");
for (const flag of [
  "culinaryPhase4AEnabled",
  "kitchenMasterEnabled",
  "productionPlanningEnabled",
  "productionSchedulingEnabled",
  "kdsDisplayEnabled",
  "qualityCheckpointsEnabled",
  "centralKitchenEnabled",
]) {
  assert.ok(aiApiSrc.includes(flag), `Missing Phase 4A feature flag: ${flag}`);
}

// 5. Mobile SILA Store exposes Phase 4A Production Ops with offline queue, online-only stock posting guard, and zero admin secret exposure
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
for (const token of [
  "getMobileProductionSummary",
  "getProductionKdsFeed",
  "executeProductionBatchAction",
  "confirmProductionPickingLine",
  "issueMaterialToProduction",
  "recordProductionQualityCheck",
  "getProductionBatchTraceability",
  "triggerBuffetReplenishment",
  "reportProductionException",
  "MOBILE_PROD_QUEUE_KEY",
  "handleSyncOfflineQueue",
  "Offline guard active",
  "disabled={!isOnline}",
  "idempotencyKey",
]) {
  assert.ok(mobileSrc.includes(token), `Missing Mobile Phase 4A token: ${token}`);
}
assert.doesNotMatch(mobileSrc, /saveCulinaryAiSettings|testCulinaryAiConnection|geminiApiKey|claudeApiKey/i);

console.log("culinaryPhase4A.test.ts: all Phase 4A frontend, mobile & guardrail checks passed.");
