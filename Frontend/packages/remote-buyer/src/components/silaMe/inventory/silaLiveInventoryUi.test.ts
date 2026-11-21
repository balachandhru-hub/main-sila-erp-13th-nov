/**
 * Live Inventory premium UI guards (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/inventory/silaLiveInventoryUi.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { buildInventoryPriorities } from "./liveInventoryPriorities";
import { resolveLiveInventoryLens } from "./liveInventoryLens";
import { silaMePermissions } from "../silaMePermissions";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), "utf8");

const live = read("SilaLiveInventory.tsx");
assert.match(live, /Inventory Command Center/);
assert.match(live, /liv-command/);
assert.match(live, /Critical \/ Low Stock/);
assert.match(live, /In Transit/);
assert.match(live, /Pending Actions/);
assert.match(live, /Stock Items/);
assert.match(live, /Inventory Priorities/);
assert.match(live, /liv-alert-icon/);
assert.match(live, /Stock Count/);
assert.match(live, /Scan barcode/);
assert.match(live, /Take photo/);
assert.match(live, /SilaLiveStockCountPanel/);
assert.match(live, /openStockCount/);
assert.match(live, /Search inventory/);
// Scanned / photo material goes into the main Search inventory box — no extra scan text box.
assert.doesNotMatch(live, /liv-scan-input/);
assert.doesNotMatch(live, /placeholder=\{scanMode === "barcode" \? "Scan barcode…"/);
assert.doesNotMatch(live, /liv-lenses/);
assert.doesNotMatch(live, /Day-to-day stock availability/);
assert.doesNotMatch(live, /Cost Control/);
assert.match(live, /optionalCols/);
assert.match(live, /searchLiveInventory\([\s\S]*health/);
assert.match(live, /getInventoryHome/);
assert.match(live, /getRecentAlerts/);
assert.match(live, /Location \(transfer \/ stock count\)|Location \(stock count\)/);
assert.doesNotMatch(live, /PageHeader/);
assert.doesNotMatch(live, /Your Location/);
// Location dropdown (transfer / stock count) must not sit on the home list permanently.
assert.match(live, /selectedId[\s\S]*Location \(transfer \/ stock count\)|if \(selectedId\)[\s\S]*Location \(transfer \/ stock count\)|if \(selectedId\)[\s\S]*Location \(stock count\)/);

const css = read("SilaInventory.css");
assert.match(css, /--liv-canvas:\s*#fafafb/i);
assert.match(css, /--liv-primary:\s*#0284c7/i);
assert.match(css, /liv-kpi--sky/);

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
assert.match(workspace, /permissions=\{can\}/);
assert.match(workspace, /canPostGoodsIssue/);

const chef = resolveLiveInventoryLens(silaMePermissions("outlet-manager"));
assert.ok(chef.id === "chef" || chef.id === "store" || chef.id === "fnb" || chef.id === "ops");

const priorities = buildInventoryPriorities(
  {
    pendingTransferApprovals: 1,
    inTransitToReceive: 2,
    openStockCounts: 0,
    openEnquiries: 0,
    openAlerts: 0,
    lowStockItems: 3,
    stockItems: 40,
    pendingActions: 1,
  },
  [],
);
assert.equal(priorities.length, 3);
assert.equal(priorities[0].action, "LOW");

const mobileHub = readFileSync(join(dir, "../../../../../remote-mobile/src/screens/inventory/InventoryScreen.tsx"), "utf8");
assert.match(mobileHub, /Floor mode/);
assert.match(mobileHub, /getInventoryHome/);
assert.match(mobileHub, /liv-m-outlet/);

const mobileStock = readFileSync(join(dir, "../../../../../remote-mobile/src/screens/inventory/LiveStockScreen.tsx"), "utf8");
assert.match(mobileStock, /liv-m-card/);
assert.match(mobileStock, /searchLiveInventory/);

console.log("silaLiveInventoryUi.test.ts: ok");
