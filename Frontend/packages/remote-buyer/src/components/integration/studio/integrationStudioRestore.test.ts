/**
 * Guard: API Config uses Integration Studio latest GUI.
 * Do not replace with IntegrationHub unless explicitly requested.
 * Run: npx --yes tsx src/components/integration/studio/integrationStudioRestore.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (rel: string) => readFileSync(join(dir, rel), "utf8");

const studio = read("IntegrationStudio.tsx");
assert.match(studio, /data-testid="integration-studio"/);
assert.match(studio, /SILA Integration Studio/);
assert.match(studio, /istudio-hero/);
assert.match(studio, /IntegrationStudioConnections/);
assert.match(studio, /IntegrationStudioWizard/);
assert.match(read("IntegrationStudio.css"), /\.istudio-hero/);

const api = readFileSync(join(dir, "../../../api/integrationsApi.ts"), "utf8");
assert.match(api, /SAP_MOVEMENT_PROFILES/);
assert.match(api, /alignIntegrationConnection/);
assert.match(api, /getIntegrationExecutions/);

const dash = readFileSync(
  join(dir, "../../../../../remote-platform-user/src/components/BuyerAdminDash.tsx"),
  "utf8",
);
assert.match(dash, /integration\/studio\/IntegrationStudio/);
const apiConfigBlock = dash.slice(dash.indexOf('activeNav === "apiConfiguration"'));
assert.match(apiConfigBlock.slice(0, 350), /IntegrationStudio/);
assert.doesNotMatch(apiConfigBlock.slice(0, 350), /IntegrationHub/);

console.log("integrationStudioRestore.test.ts: ok");
