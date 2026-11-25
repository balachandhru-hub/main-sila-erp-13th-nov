/**
 * Guard: premium Mobile Live Inventory + stock count camera/AI + quick transfer scan.
 * Run: npx --yes tsx src/screens/inventory/mobileLiveInventoryPremium.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const root = join(dir, "../..");
const read = (rel: string) => readFileSync(join(root, rel), "utf8");

const hub = read("screens/inventory/InventoryScreen.tsx");
assert.match(hub, /mobile-live-inventory/);
assert.match(hub, /liv-m-kpis/);
assert.match(hub, /liv-scan-barcode/);
assert.match(hub, /BarcodeScanner/);
assert.match(hub, /Scan material/);
assert.match(hub, /inventory\/scan/);
assert.match(hub, /inventory\/transactions/);
assert.match(hub, /Stock Count/);
assert.match(hub, /Quick Transfer/);
assert.doesNotMatch(hub, /SilaLiveInventory/); // cloud component not imported
assert.doesNotMatch(hub, /Transaction history.*tasks\/alerts/);

const scan = read("screens/inventory/ScanMaterialScreen.tsx");
assert.match(scan, /mobile-scan-material/);
assert.match(scan, /BarcodeScanner/);
assert.match(scan, /createQuickTransfer/);
assert.match(scan, /createLiveStockCount/);
assert.match(scan, /DAMAGE/);
assert.match(scan, /reverseTransfer/);
assert.match(scan, /identifyStockCountPhoto/);

const adj = read("screens/inventory/AdjustmentDetailScreen.tsx");
assert.match(adj, /History/);
assert.match(adj, /events/);
assert.match(adj, /erpReference/);

const tx = read("screens/inventory/InventoryTransactionsScreen.tsx");
assert.match(tx, /mobile-inventory-transactions/);
assert.match(tx, /getInventoryTransactions/);

const scanner = read("components/BarcodeScanner.tsx");
assert.match(scanner, /BarcodeDetector/);
assert.match(scanner, /ean_13/);
assert.match(scanner, /mobile-barcode-scanner/);

const photo = read("screens/count/CountPhotoIdentify.tsx");
assert.match(photo, /identifyStockCountPhoto/);
assert.match(photo, /hintText/);
assert.match(photo, /Scan Barcode Instead/);
assert.match(photo, /Take Photo — AI Identify/);

const barcode = read("screens/count/BarcodeEntry.tsx");
assert.match(barcode, /Open camera scanner/);
assert.match(barcode, /BarcodeScanner/);

const transfer = read("screens/inventory/TransferFormScreen.tsx");
assert.match(transfer, /transfer-scan-barcode/);
assert.match(transfer, /Recommended sources/);
assert.match(transfer, /getLiveInventoryDetail/);
assert.match(transfer, /createQuickTransfer/);

const api = readFileSync(
  join(root, "../../remote-buyer/src/api/silaMe/silaStockCountApi.ts"),
  "utf8",
);
assert.match(api, /FormData/);
assert.match(api, /preferBarcodeFallback/);
assert.match(api, /confidence/);

const engine = readFileSync(
  join(root, "../../../../Backend/src/Modules/Buyer/Buyer.Application/Features/Shared/SilaMaterialRecognitionEngine.cs"),
  "utf8",
);
assert.match(engine, /SilaMaterialRecognitionEngine/);
assert.match(engine, /PreferBarcodeFallback/);
assert.match(engine, /Confidence/);

console.log("mobileLiveInventoryPremium.test.ts: ok");
