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
assert.match(screen, /speakNaturally/);
assert.match(screen, /scheduleEndOfTurn|endOfSpeechSilenceMs|VOICE2_CONFIG/);
assert.match(screen, /echoCancellation/);
assert.match(screen, /noiseSuppression/);
assert.match(screen, /autoGainControl/);
assert.match(screen, /Submit — create draft PO/);
assert.match(screen, /Confirm transcript/);
assert.match(screen, /Review header/);
assert.match(screen, /End Session/);
assert.match(screen, /enterTranscriptConfirmation/);
assert.match(screen, /unlockSpeechPlayback/);
assert.doesNotMatch(screen, /data-testid="vpo-recording-controls"/);
assert.doesNotMatch(screen, /data-testid="vpo-start-recording"/);

const overlay = readFileSync(join(dir, 'voiceAgent/VoiceAgentOverlay.tsx'), 'utf8');
assert.match(overlay, /sila-voice-agent-overlay/);
assert.match(overlay, /sva-waveform/);
assert.match(overlay, /I'm listening/);
assert.match(overlay, /End Session/);
assert.match(overlay, /sva-activate/);

const overlayCss = readFileSync(join(dir, 'voiceAgent/VoiceAgentOverlay.css'), 'utf8');
assert.match(overlayCss, /#e0f2fe/i);
assert.match(overlayCss, /#38bdf8/i);
assert.match(overlayCss, /#6366f1/i);
assert.match(overlayCss, /#312e81/i);
assert.match(overlayCss, /#818cf8/i);
assert.match(overlayCss, /#dc2626/i);

const naturalTts = readFileSync(join(dir, 'voiceAgent/naturalTts.ts'), 'utf8');
assert.match(naturalTts, /pickNaturalVoice/);
assert.match(naturalTts, /looksIncompleteUtterance/);
assert.match(naturalTts, /unlockSpeechPlayback/);
assert.match(naturalTts, /isConversationDonePhrase/);
assert.match(naturalTts, /startKeepAlive|keepAlive/);
assert.match(naturalTts, /synthesizeVoiceTts|fetchServerWav|probeBrowserSynth/);
assert.match(naturalTts, /playWavBlob/);
assert.match(engineApi, /synthesizeVoiceTts/);
assert.match(screen, /isSafariLike|resumeListening/);
assert.match(screen, /clarifications|validationStatus|NEEDS_CLARIFICATION/);

assert.match(screen, /resolveDialogueIntent/);
assert.match(screen, /isShortConversationalUtterance/);
assert.match(screen, /acknowledging/);
assert.match(screen, /beginRecognitionRef/);
assert.match(overlay, /Hi! I'm here/);
assert.match(overlay, /Say Hey SILA/);

const dialogue = readFileSync(join(dir, 'voiceAgent/dialogueIntents.ts'), 'utf8');
assert.match(dialogue, /WAKE_ACK/);
assert.match(dialogue, /AVAILABILITY/);
assert.match(dialogue, /resolveDialogueIntent/);
assert.match(dialogue, /isShortConversationalUtterance/);

const engineApi = readFileSync(join(dir, '../../../../remote-buyer/src/api/silaMe/silaVoiceEngineApi.ts'), 'utf8');
assert.match(engineApi, /\/api\/v1\/buyer\/sila\/voice-engine/);

assert.match(api, /export const resolveMyPurchasingDefaults/);
assert.match(api, /export const startVoicePoSession/);
assert.match(api, /export const sendVoicePoTurn/);
assert.match(api, /export const confirmVoicePoDraft/);
assert.match(api, /export const cancelVoicePoSession/);
assert.match(api, /\/api\/v1\/buyer\/sila\/voice-po\/sessions/);

assert.match(cloudNav, /silaUserPoDefaults/);
assert.match(cloudWorkspace, /SilaUserPoDefaults/);
assert.match(cloudDefaults, /listPoFlexConfigs/);
assert.match(cloudDefaults, /isOpen=\{Boolean\(editor\)\}/);
assert.match(cloudDefaults, /closeEditor/);
assert.match(cloudDefaults, /Add parameter/);
assert.match(cloudDefaults, /Add condition/);
const flexApi = readFileSync(join(dir, '../../../../remote-buyer/src/api/silaMe/silaUserPoFlexApi.ts'), 'utf8');
assert.match(flexApi, /asGuidOrNull|GUID_RE/);
assert.match(flexApi, /sanitizeWrite/);
const modal = readFileSync(join(dir, '../../../../shared-ui/src/components/Modal/Modal.tsx'), 'utf8');
assert.match(modal, /onCloseRef/);
assert.match(modal, /\[isOpen\]/);

assert.match(app, /VoicePoScreen/);
assert.match(app, /purchasing\/voice-po/);
assert.match(more, /Create PO by Voice/);
assert.match(more, /purchasing\/voice-po/);

const home = readFileSync(join(dir, '../home/HomeScreen.tsx'), 'utf8');
const inventory = readFileSync(join(dir, '../inventory/InventoryScreen.tsx'), 'utf8');
assert.match(home, /Create PO by Voice/);
assert.match(inventory, /Create PO by Voice/);
assert.match(screen, /submitted for approval/);
assert.match(screen, /posts to SAP after approval/);

const tasks = readFileSync(join(dir, '../tasks/TaskTabs.tsx'), 'utf8');
assert.match(tasks, /decidePurchaseOrder/);
assert.match(tasks, /Purchase order approvals/);
assert.match(tasks, /Approve to post Create PO to SAP/);

const receivingApi = readFileSync(join(dir, '../../../../remote-buyer/src/api/silaMe/silaReceivingApi.ts'), 'utf8');
assert.match(receivingApi, /export const decidePurchaseOrder/);
assert.match(receivingApi, /purchase-orders\/\$\{purchaseOrderId\}\/approval/);

console.log('silaVoicePoFrontend.test.ts: ok');
