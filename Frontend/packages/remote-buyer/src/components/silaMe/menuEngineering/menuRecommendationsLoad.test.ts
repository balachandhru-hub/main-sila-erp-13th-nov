/**
 * Guard: Menu Engineering purchase recommendations never hang on a blank spinner.
 * Run: npx --yes tsx src/components/silaMe/menuEngineering/menuRecommendationsLoad.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const views = readFileSync(join(dir, "MeOperationalViews.tsx"), "utf8");
const css = readFileSync(join(dir, "../recipes/SilaStudioProtoTheme.css"), "utf8");

assert.match(views, /setLoadError\(message\)/);
assert.match(views, /data-testid="me-view-load-error"/);
assert.match(views, /data-testid="me-purchase-plan-empty"/);
assert.match(css, /\.rsp-page \.sme-ai-mode-bar \{[^}]*flex-wrap: nowrap/);
console.log("menuRecommendationsLoad.test.ts passed");
