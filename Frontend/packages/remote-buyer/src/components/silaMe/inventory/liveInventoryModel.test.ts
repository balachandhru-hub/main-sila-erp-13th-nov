/**
 * Live Inventory command center model guards.
 * Run: npx --yes tsx src/components/silaMe/inventory/liveInventoryModel.test.ts
 */
import assert from "node:assert/strict";
import {
  applyChip,
  applyVisionReading,
  composeQty,
  hasOpenBottle,
  isBottleItem,
  isCommandQuery,
  matchesOmniQuery,
  mlFromFill,
  openBottleStats,
  parCoveragePct,
  parsePackSizeMl,
  sparklinePoints,
  splitSealedFraction,
  stockValueOf,
  weeklyIssued,
} from "./liveInventoryModel";
import type { SilaLiveInventoryRow } from "../../../api/silaMe/silaInventoryApi";

const row = (over: Partial<SilaLiveInventoryRow>): SilaLiveInventoryRow => ({
  materialId: "m1",
  materialCode: "1000000",
  description: "BOMBAY SAPPHIRE GIN 750ML",
  baseUom: "EA",
  onHandQty: 5.733,
  inTransitQty: 0,
  locationCount: 1,
  productClass: "BEVERAGE",
  ...over,
});

assert.equal(parsePackSizeMl("GIN 750ML"), 750);
assert.equal(parsePackSizeMl("VODKA 70 CL"), 700);
assert.equal(parsePackSizeMl("WHISKY 1.75 L"), 1750);
assert.equal(parsePackSizeMl("WINE 0,75 L"), 750);
assert.equal(parsePackSizeMl("CHICKEN 1 KG"), null);
assert.equal(parsePackSizeMl(null), null);

assert.equal(isBottleItem(row({})), true);
assert.equal(isBottleItem(row({ baseUom: "L" })), false);
assert.equal(isBottleItem(row({ productClass: "FOOD" })), false);
assert.equal(isBottleItem(row({ description: "HOUSE POUR" })), false);

assert.deepEqual(splitSealedFraction(5.733), { sealed: 5, fraction: 0.733 });
assert.deepEqual(splitSealedFraction(6), { sealed: 6, fraction: 0 });
assert.deepEqual(splitSealedFraction(-1), { sealed: 0, fraction: 0 });
assert.equal(composeQty(5, 0.733), 5.733);
assert.equal(composeQty(5, 2), 6);

const stats = openBottleStats(row({}), 5.733);
assert.equal(stats.sealed, 5);
assert.equal(stats.fractionPct, 73.3);
assert.equal(stats.packMl, 750);
assert.equal(stats.openRemainingMl, 550);
assert.equal(stats.openConsumedMl, 200);
const food = openBottleStats(row({ productClass: "FOOD", description: "RICE 5KG" }), 3.5);
assert.equal(food.fraction, 0);
assert.equal(food.packMl, null);

assert.equal(hasOpenBottle(row({})), true);
assert.equal(hasOpenBottle(row({ onHandQty: 6 })), false);
assert.equal(hasOpenBottle(row({ productClass: "FOOD", onHandQty: 2.5 })), false);

assert.equal(parCoveragePct(4, 8), 50);
assert.equal(parCoveragePct(12, 8), 100);
assert.equal(parCoveragePct(4, null), null);
assert.equal(stockValueOf(row({ onHandQty: 2, unitCost: 68.5 })), 137);
assert.equal(stockValueOf(row({ unitCost: null })), null);

const rows = [
  row({ materialId: "a", materialCode: "1", description: "GIN 750ML", onHandQty: 2.5 }),
  row({ materialId: "b", materialCode: "2", description: "CHICKEN BREAST", baseUom: "KG", productClass: "FOOD", inTransitQty: 3 }),
  row({ materialId: "c", materialCode: "3", description: "MARLBORO", baseUom: "PK", productClass: "TOBACCO", stockStatus: "LOW", barcode: "6291003" }),
];
assert.equal(isCommandQuery("show open bottles"), true);
assert.equal(isCommandQuery("gin"), false);
assert.deepEqual(rows.filter((r) => matchesOmniQuery(r, "open bottles")).map((r) => r.materialId), ["a"]);
assert.deepEqual(rows.filter((r) => matchesOmniQuery(r, "in transit")).map((r) => r.materialId), ["b"]);
assert.deepEqual(rows.filter((r) => matchesOmniQuery(r, "6291003")).map((r) => r.materialId), ["c"]);
assert.deepEqual(rows.filter((r) => matchesOmniQuery(r, "tobacco")).map((r) => r.materialId), ["c"]);
assert.deepEqual(applyChip(rows, "FOOD").map((r) => r.materialId), ["b"]);
assert.deepEqual(applyChip(rows, "LOW").map((r) => r.materialId), ["c"]);
assert.deepEqual(applyChip(rows, "TRANSIT").map((r) => r.materialId), ["b"]);
assert.deepEqual(applyChip(rows, "OPEN").map((r) => r.materialId), ["a"]);
assert.equal(applyChip(rows, "ALL").length, 3);

assert.equal(sparklinePoints(undefined), "");
assert.equal(sparklinePoints([0, 0, 0]).split(" ").length, 3);
assert.equal(sparklinePoints([1, 2, 4], 20, 20), "0.0,16.0 10.0,12.0 20.0,4.0");
assert.equal(weeklyIssued([1, 0.5, 0.25]), 1.75);

// A 750 ml bottle read at 40% has 300 ml left and 450 ml consumed.
assert.deepEqual(mlFromFill(0.4, 750), { remainingMl: 300, consumedMl: 450 });
assert.deepEqual(mlFromFill(1.4, 750), { remainingMl: 750, consumedMl: 0 });
const applied = applyVisionReading({ fillRatio: 0.733, nominalMl: 750 }, 5);
assert.deepEqual(applied, { fraction: 0.73, sealed: 5, total: 5.73, remainingMl: 548, consumedMl: 203 });
assert.equal(applyVisionReading({ fillRatio: null, nominalMl: 750 }, 5), null);

console.log("liveInventoryModel.test.ts: ok");
