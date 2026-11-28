/**
 * Phase 3B Advanced Culinary Analytics, Menu Engineering & AI Optimization
 * Frontend contract, Cloud Analytics Studio, 15-Report Export, Mobile Daily Ops & Guardrail tests.
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinaryPhase3B.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

// 1. Phase 3B Analytics API client exposes all modular intelligence endpoints & zero ERP auto-postings
const apiSrc = read("../../../../api/silaMe/silaCulinaryAnalyticsApi.ts");
for (const fn of [
  "getCulinaryAnalyticsConfig",
  "saveCulinaryAnalyticsConfig",
  "getExecutiveCulinaryDashboard",
  "analyzeMenuEngineering",
  "analyzeConsumptionVariance",
  "createVarianceInvestigationTask",
  "analyzeIngredientPriceImpact",
  "listCulinarySimulations",
  "runCulinarySimulation",
  "duplicateCulinarySimulation",
  "optimizeCulinaryMenu",
  "analyzeIngredientSubstitutions",
  "optimizeBuffetProduction",
  "optimizeEventMenus",
  "generateDemandForecast",
  "getWasteIntelligence",
  "recordCulinaryWaste",
  "benchmarkProperties",
  "listCulinaryRecommendations",
  "generateCulinaryRecommendations",
  "decideCulinaryRecommendation",
  "queryCulinaryCopilot",
  "exportCulinaryAnalyticsReport",
  "getMobileCulinaryOpsSummary",
]) {
  assert.ok(apiSrc.includes(fn), `Missing Phase 3B API helper: ${fn}`);
}
assert.doesNotMatch(apiSrc, /postSapLive|createSapAccountingDocument|postGlJournal|autoAdjustInventory/i);

// 2. Cloud Analytics Studio covers all 10 Phase 3B modules + 4 quadrants + INSUFFICIENT_DATA
const wsSrc = read("CulinaryAnalyticsStudioWorkspace.tsx");
for (const token of [
  "Executive Dashboard",
  "Menu Engineering & Profitability",
  "Consumption Variance & Price Impact",
  "What-If Simulation",
  "Optimization, Buffet & Events",
  "Forecasting & Waste",
  "Property Benchmarking",
  "Recommendation Cockpit",
  "Culinary Copilot",
  "Configuration",
  "STAR",
  "PLOWHORSE",
  "PUZZLE",
  "DOG",
  "INSUFFICIENT_DATA",
]) {
  assert.ok(wsSrc.includes(token), `Missing Cloud Analytics Studio token: ${token}`);
}

// 3. Reports Workspace wires all 15 Phase 3B reports + Excel/CSV/PDF exports & Nav shows Phase 3B Live
const reportsSrc = read("CulinaryProductionAnalyticsReportWorkspaces.tsx");
assert.ok(reportsSrc.includes("<CulinaryAnalyticsStudioWorkspace />"));
assert.ok(reportsSrc.includes("exportCulinaryAnalyticsReport"));
for (const rep of [
  "MENU_ENGINEERING",
  "MENU_ITEM_PROFITABILITY",
  "FOOD_COST_ANALYSIS",
  "INGREDIENT_PRICE_VARIANCE",
  "RECIPE_COST_MOVEMENT",
  "CONSUMPTION_VARIANCE",
  "BUFFET_EFFICIENCY",
  "FOOD_WASTE_ANALYSIS",
  "EVENT_PROFITABILITY",
  "DEMAND_FORECAST",
  "PROPERTY_BENCHMARKING",
  "AI_RECOMMENDATION",
  "POTENTIAL_SAVINGS",
  "REALIZED_SAVINGS",
  "WHAT_IF_COMPARISON",
]) {
  assert.ok(reportsSrc.includes(rep), `Missing Phase 3B report type: ${rep}`);
}

const navSrc = read("SilaRecipeStudio2Nav.tsx");
assert.ok(navSrc.includes('id: "analytics"'));

// 4. Mobile screen exposes Daily Culinary Ops with cost-masking, idempotent waste logging, and zero admin secret exposure
const mobileSrc = read("../../../../../../remote-mobile/src/screens/recipes/CulinarySuitePreviewScreen.tsx");
for (const token of [
  "getMobileCulinaryOpsSummary",
  "recordCulinaryWaste",
  "queryCulinaryCopilot",
  "decideCulinaryRecommendation",
  "Daily Culinary Ops (Phase 3B)",
  "opsSummary.canViewCosts",
  "idempotencyKey",
]) {
  assert.ok(mobileSrc.includes(token), `Missing Mobile Phase 3B token: ${token}`);
}
assert.doesNotMatch(mobileSrc, /saveCulinaryAiSettings|testCulinaryAiConnection|geminiApiKey|claudeApiKey/i);

console.log("culinaryPhase3B.test.ts: all Phase 3B frontend, mobile & guardrail checks passed.");
