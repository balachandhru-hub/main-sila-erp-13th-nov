/**
 * Guards for Live Inventory demand/material ITO + reverse transfer wiring.
 * Run: npx --yes tsx src/components/silaMe/inventory/silaLiveInventoryIto.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

const live = read("SilaLiveInventory.tsx");
assert.match(live, /Pending Requirements/);
assert.match(live, /getPendingOutletRequirements/);
assert.match(live, /SilaPendingRequirementsPanel/);
assert.match(live, /Create ITO/);
assert.match(live, /requireSamePlant:\s*true/);

const panel = read("SilaPendingRequirementsPanel.tsx");
assert.match(panel, /data-testid="pending-outlet-requirements"/);
assert.match(panel, /createTransfer/);
assert.match(panel, /weeklyBucketItemId/);
assert.match(panel, /wishlistItemId/);

const detail = read("../movements/SilaTransferDetail.tsx");
assert.match(detail, /Reverse Transfer/);
assert.match(detail, /reverseTransfer/);
assert.match(detail, /REVERSE/);

const movementsApi = read("../../../api/silaMe/silaMovementsApi.ts");
assert.match(movementsApi, /requireSamePlant/);
assert.match(movementsApi, /\/transfers\/\$\{transferId\}\/reverse/);
assert.match(movementsApi, /weeklyBucketItemId/);

const inventoryApi = read("../../../api/silaMe/silaInventoryApi.ts");
assert.match(inventoryApi, /pending-requirements/);

console.log("silaLiveInventoryIto.test.ts: ok");
