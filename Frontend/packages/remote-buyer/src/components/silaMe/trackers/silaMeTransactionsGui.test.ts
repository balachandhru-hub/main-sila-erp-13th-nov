/**
 * SILA ME Transactions GUI restore guards (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/trackers/silaMeTransactionsGui.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const tracker = readFileSync(join(dir, "SilaConsolidatedTracker.tsx"), "utf8");
const css = readFileSync(join(dir, "SilaTracker.css"), "utf8");
const nav = readFileSync(join(dir, "../silaMeNav.tsx"), "utf8");
const workspace = readFileSync(join(dir, "../SilaMeWorkspace.tsx"), "utf8");

assert.match(tracker, /pstudio-hero/);
assert.match(tracker, /SILA ME Transactions/);
assert.match(tracker, /pstudio-kpis/);
assert.match(tracker, /pstudio-lifecycle/);
assert.match(tracker, /resolveRequestPayload|isCapturedSapRequest/);
assert.match(tracker, /SilaPosStudio\.css/);
assert.doesNotMatch(tracker, /SILA UI built this request view because technicalDetail was blank/);

assert.match(css, /strk-studio-panel/);
assert.match(css, /strk-studio-table/);
assert.match(css, /strk-studio-detail/);

assert.match(nav, /silaMeTracker/);
assert.match(nav, /SILA ME Transactions/);
assert.match(nav, /not Setup-gated|manageErpPosting/);

assert.match(workspace, /SilaConsolidatedTracker/);
assert.match(workspace, /silaMeTracker/);
assert.match(workspace, /tracker="SILA_ME"/);

console.log("silaMeTransactionsGui.test.ts: ok");
