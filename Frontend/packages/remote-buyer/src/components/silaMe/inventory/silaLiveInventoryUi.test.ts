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
const cc = read("SilaInventoryCommandCenter.tsx");
const ccCss = read("SilaInventoryCommandCenter.css");
const vision = read("SilaVisionAiModal.tsx");

// The list view is the command center; the parent keeps data, material detail and pending panels.
assert.match(live, /SilaInventoryCommandCenter/);
assert.match(live, /SilaLiveStockCountPanel/);
assert.match(live, /openStockCount/);
assert.match(live, /SilaPendingRequirementsPanel/);
assert.match(live, /focusPendingPanel|liv-pending-panel/);
assert.match(live, /searchLiveInventory\([\s\S]*health/);
assert.match(live, /productClass/);
assert.match(live, /getInventoryHome/);
assert.match(live, /getRecentAlerts/);
assert.match(live, /Destination \(transfer \/ stock count\)|Location \(stock count\)/);
assert.doesNotMatch(live, /PageHeader/);
assert.doesNotMatch(live, /Your Location/);
assert.doesNotMatch(live, /liv-scan-input/);
// Destination dropdown (transfer / stock count) belongs to the material view only.
assert.match(live, /if \(selectedId\)[\s\S]*Destination \(transfer \/ stock count\)|if \(selectedId\)[\s\S]*Location \(stock count\)/);
assert.match(live, /Quick ITO/);

// Command center: prototype layout, scan, photo, Vision AI, lenses and product classes.
assert.match(cc, /Inventory Command Center/);
assert.match(cc, /Store health/);
assert.match(cc, /Supply chain/);
assert.match(cc, /Live outlet valuation/);
assert.match(cc, /Spatial Vault/);
assert.match(cc, /Shelf Count Mode/);
assert.match(cc, /Executive Ledger/);
assert.match(cc, /Post Cycle Count/);
assert.match(cc, /Reset to Book/);
assert.match(cc, /Scan barcode/);
assert.match(cc, /Take photo/);
assert.match(cc, /Vision AI/);
assert.match(cc, /New Transfer/);
assert.match(cc, /Goods Issue \(Z01\)/);
assert.match(cc, /Transfer Stock/);
assert.match(cc, /createLiveStockCount/);
assert.match(cc, /createTransfer/);
assert.match(cc, /PRODUCT_CLASSES/);
assert.match(cc, /data-testid="inventory-command-center"/);
assert.doesNotMatch(cc, /createTransfer\(\{[\s\S]*?requireSamePlant:\s*true/);
assert.match(vision, /analyzeBottleLevel/);
assert.match(vision, /Apply to shelf count/);
assert.match(read("SilaBarcodeScanDialog.tsx"), /BarcodeDetector/);
assert.match(ccCss, /JetBrains\+Mono/);
assert.match(ccCss, /family=Inter/);
assert.match(ccCss, /#111827/);

const inventoryApiSrc = readFileSync(join(dir, "../../../api/silaMe/silaInventoryApi.ts"), "utf8");
assert.match(inventoryApiSrc, /inventory\/vision\/bottle-level/);
assert.match(inventoryApiSrc, /productClass/);

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
assert.match(nav, /silaInventoryStudio/);
assert.match(nav, /Inventory Studio/);
assert.match(nav, /Inventory Transactions/);
assert.doesNotMatch(nav.slice(nav.indexOf('label: "Inventory"'), nav.indexOf('label: "Menu Engineering"')), /Purchase Requests/);
assert.doesNotMatch(nav.slice(nav.indexOf('label: "Inventory"'), nav.indexOf('label: "Menu Engineering"')), /silaStockHealth/);
const workspace = readFileSync(join(dir, "../SilaMeWorkspace.tsx"), "utf8");
assert.match(workspace, /SilaInventoryStudio/);
assert.match(workspace, /inventoryStudioTabFromNav/);
assert.match(workspace, /permissions=\{can\}/);
assert.match(workspace, /canPostGoodsIssue/);
assert.match(read("SilaInventoryStudio.tsx"), /data-testid="inventory-studio"/);
assert.match(read("SilaInventoryStudio.tsx"), /Stock Health/);
assert.match(read("SilaInventoryStudio.tsx"), /Internal Transfers/);
assert.match(read("SilaInventoryStudio.css"), /istudio-inv-page/);

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
