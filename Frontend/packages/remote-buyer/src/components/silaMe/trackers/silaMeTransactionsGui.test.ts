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

assert.match(tracker, /strk-me-toolbar/);
assert.match(tracker, /strk-me-hero--compact/);
assert.match(tracker, /data-testid="sila-me-transactions-studio"/);
assert.match(tracker, /TRANSACTION TRACKER/);
assert.match(tracker, /strk-me-hero__title--tracker/);
assert.doesNotMatch(tracker, /strk-me-panel__title--tracker/);
assert.match(tracker, /strk-me-kpis/);
assert.match(tracker, /strk-me-lifecycle/);
assert.match(tracker, /resolveRequestPayload|isCapturedSapRequest/);
assert.match(tracker, /Filters/);
assert.match(tracker, /aria-controls="strk-me-filters"/);
assert.match(tracker, /filtersOpen/);
assert.match(tracker, /ERP REF ID/);
assert.match(tracker, /erpRefId/);
assert.doesNotMatch(tracker, /Live inventory and ERP postings/);
assert.doesNotMatch(tracker, /Filter by type, plant, status, or integration/);
assert.doesNotMatch(tracker, /ERP invoice ID/);
assert.doesNotMatch(tracker, /SILA UI built this request view because technicalDetail was blank/);
// SILA_ME must not render the legacy PageHeader title chrome.
assert.doesNotMatch(tracker, /studio \? \(\s*<PageHeader/);

assert.match(css, /strk-me-toolbar/);
assert.match(css, /strk-me-kpi--rose/);
assert.match(css, /strk-studio-panel/);
assert.match(css, /strk-studio-table/);
assert.match(css, /strk-studio-detail/);
assert.match(css, /strk-studio-filters--grid/);

assert.match(nav, /silaMeTracker/);
assert.match(nav, /Transaction Tracker/);
assert.match(nav, /iconOnly:\s*true/);
assert.match(nav, /Purchasing Home/);
assert.match(nav, /Purchasing Dashboard/);
assert.match(nav, /label: "Dashboard"/);
assert.doesNotMatch(nav, /label: "Integration"/);
const setupBlock = nav.slice(nav.indexOf('label: "Setup"'));
assert.match(setupBlock, /silaMeTracker/);
assert.match(setupBlock, /Transaction Tracker/);

assert.match(workspace, /SilaConsolidatedTracker/);
assert.match(workspace, /silaMeTracker/);
assert.match(workspace, /tracker="SILA_ME"/);

console.log("silaMeTransactionsGui.test.ts: ok");
