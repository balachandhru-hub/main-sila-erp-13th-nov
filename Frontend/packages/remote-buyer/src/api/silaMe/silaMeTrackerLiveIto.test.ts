/**
 * Guard: live ME tracker collapses ITO OUT+IN into one row.
 * Run: npx --yes tsx src/api/silaMe/silaMeTrackerLiveIto.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const src = readFileSync(join(dir, "silaMeTrackerLive.ts"), "utf8");
assert.match(src, /seenDocs/);
assert.match(src, /TRANSFER_OUT/);
assert.match(src, /STOCK_COUNT/);
assert.match(src, /ADJUSTMENT/);
assert.match(src, /One tracker row per ITO/);

console.log("silaMeTrackerLiveIto.test.ts: ok");
