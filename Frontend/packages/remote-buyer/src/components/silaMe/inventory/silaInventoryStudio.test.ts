/**
 * Inventory Studio nav + shell guards.
 * Run: npx --yes tsx src/components/silaMe/inventory/silaInventoryStudio.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), "utf8");

const studio = read("SilaInventoryStudio.tsx");
assert.match(studio, /data-testid="inventory-studio"/);
assert.match(studio, /Inventory Studio/);
assert.match(studio, /istudio-inv-hero--compact/);
assert.doesNotMatch(studio, /SILA Inventory/);
assert.doesNotMatch(studio, /pstudio-hero__sub/);
assert.match(studio, /Stock Health/);
assert.match(studio, /Recipe Availability/);
assert.match(studio, /Internal Transfers/);
assert.match(studio, /Stock Count/);
assert.match(studio, /Physical Inventory/);
assert.match(studio, /inventoryStudioTabFromNav/);
assert.match(read("SilaInventoryStudio.css"), /istudio-inv-page/);

const nav = readFileSync(join(dir, "../silaMeNav.tsx"), "utf8");
const inventoryBlock = nav.slice(nav.indexOf('label: "Inventory"'), nav.indexOf('label: "Menu Engineering"'));
assert.match(inventoryBlock, /Dashboard/);
assert.match(inventoryBlock, /Live Inventory/);
assert.match(inventoryBlock, /Inventory Transactions/);
assert.match(inventoryBlock, /Inventory Studio/);
assert.doesNotMatch(inventoryBlock, /Purchase Requests/);
assert.doesNotMatch(inventoryBlock, /Stock Health/);
assert.doesNotMatch(inventoryBlock, /Internal Transfers/);

const workspace = readFileSync(join(dir, "../SilaMeWorkspace.tsx"), "utf8");
assert.match(workspace, /SilaInventoryStudio/);
assert.match(workspace, /silaInventoryStudio/);
assert.match(workspace, /openLedgerDocument/);
assert.match(workspace, /onOpenDocument=\{openLedgerDocument\}/);
assert.match(workspace, /initialTransferId/);
assert.match(workspace, /initialAdjustmentId/);
assert.match(workspace, /initialGoodsIssueId/);

const transactions = read("SilaInventoryTransactions.tsx");
assert.match(transactions, /onOpenDocument/);
assert.match(transactions, /canOpenDocument/);
assert.match(transactions, /Open \$\{row\.referenceNumber/);

console.log("silaInventoryStudio.test.ts: ok");
