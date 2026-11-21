/**
 * Guard: Recipe Studio, POS Studio, and SILA ME Transactions GUI stay restored.
 * Run: npx --yes tsx src/components/silaMe/silaStudiosRestore.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

const nav = read("silaMeNav.tsx");
assert.match(nav, /label: "Recipe Studio"/);
assert.match(nav, /"POS Studio"/);
assert.match(nav, /Transaction Tracker/);
assert.match(nav, /silaMeTracker/);
assert.doesNotMatch(nav, /label: "Integration"/);
assert.match(nav.slice(nav.indexOf('label: "Setup"')), /silaMeTracker/);

const workspace = read("SilaMeWorkspace.tsx");
assert.match(workspace, /SilaRecipes/);
assert.match(workspace, /SilaPosIntegration/);
assert.match(workspace, /SilaConsolidatedTracker/);
assert.match(workspace, /tracker="SILA_ME"/);
assert.match(workspace, /Inventory Command Center|SilaLiveInventory/);

assert.match(read("recipes/SilaRecipes.tsx"), /SILA Recipe Studio|Recipe Studio/);
assert.match(read("pos/SilaPosIntegration.tsx"), /SILA POS Studio|POS Studio/);
assert.match(read("trackers/SilaConsolidatedTracker.tsx"), /strk-me-hero/);
assert.match(read("trackers/SilaConsolidatedTracker.tsx"), /data-testid="sila-me-transactions-studio"/);
assert.match(read("trackers/SilaTracker.css"), /strk-me-hero/);

// Missing these blanks the host Vite graph and /dashboard.
assert.match(read("../../api/silaMe/readSuccessId.ts"), /readSuccessId/);
assert.match(read("../../api/silaMe/silaGoodsIssueCostCenterApi.ts"), /getGoodsIssueCostCenters/);

console.log("silaStudiosRestore.test.ts: ok");
