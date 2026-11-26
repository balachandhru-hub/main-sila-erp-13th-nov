/**
 * Unit tests for Phase 0 deterministic demonstration simulators.
 * Verifies exact spec examples (§16, §35, §37, §51).
 * Run: npx --yes tsx src/components/silaMe/recipes/culinary/culinarySimulatorsMath.test.ts
 */
import assert from "node:assert/strict";
import {
  calculateEventPnl,
  calculateRecipeScaling,
  computeMrpRow,
  DEMO_EVENT_MRP_ROWS,
  DEMO_EVENT_PNL_BASE,
  DEMO_SALMON_RECIPE,
} from "./culinarySimulatorsMath";

// 1. Recipe Scaling (§16) — Grilled Salmon with Lemon Butter Sauce, 10 servings, 0% buffers
const baseScaling = calculateRecipeScaling({
  baseServings: 10,
  targetServings: 10,
  baseServingSizeGrams: 180,
  targetServingSizeGrams: 180,
  prepBufferPercent: 0,
  wasteAllowancePercent: 0,
  ingredients: DEMO_SALMON_RECIPE.ingredients,
});
assert.equal(baseScaling.scaleFactor, 1);
assert.equal(baseScaling.baseTotalCostAed, 200.73); // 153 + 8 + 3.2 + 0.9 + 28 + 5.25 + 0.125 + 2.25 = 200.725 -> 200.73
assert.equal(baseScaling.unmatchedCount, 1); // Fresh dill

// Scaled up 5x (50 servings): spices/seasoning/liquid scale sub-linearly vs linear protein
const scaled50 = calculateRecipeScaling({
  baseServings: 10,
  targetServings: 50,
  baseServingSizeGrams: 180,
  targetServingSizeGrams: 180,
  prepBufferPercent: 10,
  wasteAllowancePercent: 0,
  ingredients: DEMO_SALMON_RECIPE.ingredients,
});
const salmon50 = scaled50.items.find((i) => i.id === "ing-salmon")!;
const salt50 = scaled50.items.find((i) => i.id === "ing-salt")!;
assert.equal(salmon50.scaledQuantity, 9); // linear 1.8 * 5
assert.equal(salmon50.grossQuantityWithBuffers, 9.9); // 9 * 1.10
assert.ok(salt50.scaledQuantity < 0.025 * 5, "Sea salt should scale sub-linearly");

// 2. Event MRP (§35 & §37) — Salmon: 500 * 60% = 300 portions * 180g = 54kg * 1.10 = 59.4kg / 0.90 = 66kg - 25 - 10 = 31kg shortage
const salmonMrp = computeMrpRow(500, DEMO_EVENT_MRP_ROWS[0]);
assert.equal(salmonMrp.expectedPortions, 300);
assert.equal(salmonMrp.edibleQty, 54);
assert.equal(salmonMrp.bufferedEdibleQty, 59.4);
assert.equal(salmonMrp.rawRequiredQty, 66);
assert.equal(salmonMrp.shortageQty, 31);
assert.equal(salmonMrp.roundedOrderQty, 35); // 7 * 5kg case
assert.equal(salmonMrp.status, "SHORTAGE");

const dillMrp = computeMrpRow(500, DEMO_EVENT_MRP_ROWS[6]);
assert.equal(dillMrp.status, "MATERIAL_CREATION_REQUIRED");
assert.equal(dillMrp.roundedOrderQty, 0);

// 3. Event P&L (§51) — 500 guests * AED 350 = 175,000; cost = 92,500; contribution = 82,500 (47.14%); food = 25.71%; BE = 149
const pnl = calculateEventPnl(DEMO_EVENT_PNL_BASE);
assert.equal(pnl.netRevenueAed, 175000);
assert.equal(pnl.foodCostAed, 45000);
assert.equal(pnl.beverageCostAed, 12500);
assert.equal(pnl.totalEventCostAed, 92500);
assert.equal(pnl.eventContributionAed, 82500);
assert.equal(pnl.contributionMarginPercent, 47.14);
assert.equal(pnl.foodCostPercent, 25.71);
assert.equal(pnl.breakEvenGuests, 149);

console.log("culinarySimulatorsMath.test.ts: ok");
