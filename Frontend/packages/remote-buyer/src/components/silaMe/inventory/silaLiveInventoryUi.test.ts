import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), "utf8");

const live = read("SilaLiveInventory.tsx");
assert.match(live, /Stock Items/);
assert.match(live, /Low Stock/);
assert.match(live, /In Transit/);
assert.match(live, /Pending Actions/);
assert.match(live, /Material Details/);
assert.match(live, /optionalCols/);
assert.match(live, /searchLiveInventory\([\s\S]*health/);
assert.match(live, /getInventoryHome/);

assert.match(read("SilaStockHealth.tsx"), /Stock Health/);
assert.match(read("SilaRecipeAvailability.tsx"), /Recipe Availability/);
assert.match(read("SilaRecipeAvailability.tsx"), /no stock is posted/i);
assert.match(read("SilaInventoryAnalytics.tsx"), /Inventory Analytics/);
assert.match(read("SilaInventoryAnalytics.tsx"), /getInventoryDashboard/);

const nav = readFileSync(join(dir, "../silaMeNav.tsx"), "utf8");
assert.match(nav, /silaStockHealth/);
assert.match(nav, /silaRecipeAvailability/);
assert.match(nav, /silaInventoryAnalytics/);
const workspace = readFileSync(join(dir, "../SilaMeWorkspace.tsx"), "utf8");
assert.match(workspace, /SilaStockHealth/);
assert.match(workspace, /SilaRecipeAvailability/);
assert.match(workspace, /SilaInventoryAnalytics/);

console.log("silaLiveInventoryUi.test.ts: ok");
