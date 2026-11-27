/**
 * Phase 3A Advanced Food Cost Management, Event P&L & Financial Intelligence
 * Frontend contract, Cloud Studio, Mobile, security & §73 calculation invariant tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase3A.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { calculateEventPnl, DEMO_EVENT_PNL_BASE } from "./culinarySimulatorsMath";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Phase 3A Financial API client exposes all required endpoints
const apiSrc = read("../../../../api/silaMe/silaCulinaryFinancialApi.ts");
for (const fn of [
  "getFinancialConfig",
  "saveFinancialConfig",
  "calculateFinancialRecipeCost",
  "calculateFinancialMenuCost",
  "analyzeMaterialPriceImpact",
  "getFinancialDashboard",
  "listEventFinancialPlans",
  "getEventFinancialPlan",
  "createEventFinancialPlan",
  "recalculateEventFinancialPlan",
  "addEventRevenueLine",
  "addEventExpenseLine",
  "approveEventExpenseLine",
  "simulateEventFinancials",
  "promoteFinancialScenario",
  "resolveFinancialException",
  "submitFinancialPlanForApproval",
  "decideFinancialPlanApproval",
  "triggerFinancialChangeImpact",
  "getFinancialCancellationExposure",
  "analyzeFinancialPlanWithAi",
  "getFinancialReportCatalog",
  "exportFinancialReport",
  "getMobileFinancialSummary",
  "getPhase3BHandover",
  "getPhase4BHandover",
]) {
  assert.ok(apiSrc.includes(fn), `Missing Phase 3A API helper: ${fn}`);
}
assert.doesNotMatch(apiSrc, /postSapLive|createSapAccountingDocument|postGlJournal|createCustomerInvoice/i);

// 2. Cloud Event P&L Studio covers all 17 §50 tabs and core workflows
const wsSrc = read("CulinaryEventPnlStudioWorkspace.tsx");
for (const token of [
  "Financial Overview",
  "Revenue",
  "Food Cost",
  "Beverage Cost",
  "Manpower",
  "Expenses",
  "Procurement Commitments",
  "Cost Allocation",
  "Estimated P&L",
  "Forecast P&L",
  "Actual P&L",
  "Variance Analysis",
  "Financial Simulator",
  "AI Recommendations",
  "Approvals",
  "Reports",
  "History",
  "Revenue-to-Contribution Waterfall",
  "Launch Grand Corporate Gala Demo",
  "salariesVisible",
]) {
  assert.ok(wsSrc.includes(token), `Missing Cloud Event P&L Studio token: ${token}`);
}

// 3. Recipe Studio 2 nav & workspace router wire Phase 3A Live
const navSrc = read("SilaRecipeStudio2Nav.tsx");
assert.ok(navSrc.includes("Phase 3A Live"));
const routerSrc = read("SilaRecipeStudio2Workspaces.tsx");
assert.ok(routerSrc.includes("<CulinaryEventPnlStudioWorkspace"));

// 4. Mobile screen exposes chef-friendly Phase 3A Event P&L summary without exposing admin AI secrets or salaries
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
for (const token of [
  "getMobileFinancialSummary",
  "listEventFinancialPlans",
  "submitFinancialPlanForApproval",
  "decideFinancialPlanApproval",
  "Event Food Cost &amp; P&amp;L Summary",
]) {
  assert.ok(mobileSrc.includes(token), `Missing Mobile Phase 3A token: ${token}`);
}
assert.doesNotMatch(mobileSrc, /saveCulinaryAiSettings|testCulinaryAiConnection|geminiApiKey|claudeApiKey|hourlyRate|glAccount/i);

// 5. §53 & §73 Grand Corporate Gala Deterministic Arithmetic Verification
const gala = calculateEventPnl({ ...DEMO_EVENT_PNL_BASE, targetMarginPercent: 40 });
assert.equal(gala.netRevenueAed, 175000);
assert.equal(gala.foodCostAed, 45000);
assert.equal(gala.beverageCostAed, 12500);
assert.equal(gala.totalEventCostAed, 92500);
assert.equal(gala.eventContributionAed, 82500);
assert.equal(Number(gala.contributionMarginPercent.toFixed(2)), 47.14);
assert.equal(gala.breakEvenGuests, 149);
const maxAllowableTotalCost = gala.netRevenueAed * (1 - 0.4);
assert.equal(maxAllowableTotalCost, 105000);
assert.equal(maxAllowableTotalCost - gala.totalEventCostAed, 12500);

const discounted = calculateEventPnl({ ...DEMO_EVENT_PNL_BASE, discountPercent: 10 });
assert.equal(discounted.netRevenueAed, 157500);

console.log("culinaryPhase3A.test.ts: all Phase 3A frontend & §53/§73 invariant checks passed.");
