/**
 * Voice PO mobile panel + Cloud PO defaults wiring (no Vite/axios runtime).
 * Run: npx --yes tsx src/screens/purchasing/silaVoicePoFrontend.test.ts
 */
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), 'utf8');
const api = readFileSync(join(dir, '../../../../remote-buyer/src/api/silaMe/silaVoicePoApi.ts'), 'utf8');
const cloudNav = readFileSync(join(dir, '../../../../remote-buyer/src/components/silaMe/silaMeNav.tsx'), 'utf8');
const cloudWorkspace = readFileSync(join(dir, '../../../../remote-buyer/src/components/silaMe/SilaMeWorkspace.tsx'), 'utf8');
const cloudDefaults = readFileSync(
  join(dir, '../../../../remote-buyer/src/components/silaMe/setup/SilaUserPoDefaults.tsx'),
  'utf8',
);
const app = readFileSync(join(dir, '../../MobileApp.tsx'), 'utf8');
const more = readFileSync(join(dir, '../more/MoreScreen.tsx'), 'utf8');
const screen = read('VoicePoScreen.tsx');

assert.match(screen, /Create PO by Voice/);
assert.match(screen, /VoiceAgentOverlay/);
const overlay = readFileSync(join(dir, 'voiceAgent/VoiceAgentOverlay.tsx'), 'utf8');
assert.match(overlay, /sila-voice-agent-overlay/);
assert.match(overlay, /sva-waveform/);
const engineApi = readFileSync(join(dir, '../../../../remote-buyer/src/api/silaMe/silaVoiceEngineApi.ts'), 'utf8');
assert.match(engineApi, /\/api\/v1\/buyer\/sila\/voice-engine/);
assert.match(screen, /data-testid="vpo-recording-controls"/);
assert.match(screen, /data-testid="vpo-start-recording"/);
assert.match(screen, /data-testid="vpo-pause-recording"/);
assert.match(screen, /data-testid="vpo-stop-recording"/);
assert.match(screen, /data-testid="vpo-level-meter"/);
assert.match(screen, /echoCancellation/);
assert.match(screen, /noiseSuppression/);
assert.match(screen, /autoGainControl/);
assert.match(screen, /speechSynthesis/);
assert.match(screen, /Confirm — create draft PO/);
assert.match(screen, /Cancel conversation/);
assert.match(screen, /Uncertain — go with default/);

assert.match(api, /export const listPurchasingProfiles/);
assert.match(api, /export const upsertPurchasingProfile/);
assert.match(api, /export const setDefaultPurchasingProfile/);
assert.match(api, /export const deactivatePurchasingProfile/);
assert.match(api, /export const resolveMyPurchasingDefaults/);
assert.match(api, /export const startVoicePoSession/);
assert.match(api, /export const sendVoicePoTurn/);
assert.match(api, /export const confirmVoicePoDraft/);
assert.match(api, /export const cancelVoicePoSession/);
assert.match(api, /export const getVoicePoSession/);
assert.match(api, /\/api\/v1\/buyer\/sila\/user-purchasing-profiles/);
assert.match(api, /\/api\/v1\/buyer\/sila\/voice-po\/sessions/);

assert.match(cloudNav, /silaUserPoDefaults/);
assert.match(cloudNav, /PO Defaults & Account Assignment/);
assert.match(cloudWorkspace, /SilaUserPoDefaults/);
assert.match(cloudWorkspace, /silaUserPoDefaults/);
assert.match(cloudDefaults, /PO Defaults & Account Assignment/);
assert.match(cloudDefaults, /listPoFlexConfigs/);
assert.match(cloudDefaults, /upsertPoFlexConfig/);
assert.match(cloudDefaults, /isOpen=\{Boolean\(editor\)\}/);
assert.match(cloudDefaults, /Add parameter/);
assert.match(cloudDefaults, /Add condition/);

assert.match(app, /VoicePoScreen/);
assert.match(app, /purchasing\/voice-po/);
assert.match(more, /Create PO by Voice/);
assert.match(more, /purchasing\/voice-po/);

const home = readFileSync(join(dir, '../home/HomeScreen.tsx'), 'utf8');
const inventory = readFileSync(join(dir, '../inventory/InventoryScreen.tsx'), 'utf8');
assert.match(home, /Create PO by Voice/);
assert.match(home, /purchasing\/voice-po/);
assert.match(inventory, /Create PO by Voice/);
assert.match(inventory, /purchasing\/voice-po/);

console.log('silaVoicePoFrontend.test.ts: ok');
