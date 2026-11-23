/**
 * Dialogue intent contracts for SILA Voice 2.0 acknowledgement.
 * Run: npx --yes tsx src/screens/purchasing/voiceAgent/dialogueIntents.test.ts
 */
import assert from 'node:assert/strict';
import { isShortConversationalUtterance, resolveDialogueIntent } from './dialogueIntents.ts';

const hey = resolveDialogueIntent('Hey SILA');
assert.equal(hey.intent, 'WAKE_ACK');
assert.equal(hey.localOnly, true);
assert.match(hey.spokenReply || '', /here/i);
assert.equal(hey.businessTranscript, null);

const hi = resolveDialogueIntent('Hi SILA');
assert.equal(hi.intent, 'GREETING');
assert.equal(hi.localOnly, true);
assert.match(hi.spokenReply || '', /help/i);

const avail = resolveDialogueIntent('SILA, are you there?');
assert.equal(avail.intent, 'AVAILABILITY');
assert.match(avail.spokenReply || '', /here|hear you|go ahead/i);

const social = resolveDialogueIntent('How are you?');
assert.equal(social.intent, 'SOCIAL');
assert.equal(social.localOnly, true);

const combined = resolveDialogueIntent('Hey SILA, order ten bottles of Bombay Sapphire');
assert.equal(combined.intent, 'BUSINESS');
assert.equal(combined.localOnly, false);
assert.match(combined.businessTranscript || '', /order ten bottles/i);
assert.match(combined.spokenReply || '', /sure|got it|of course/i);

const farewell = resolveDialogueIntent("No, thank you");
assert.equal(farewell.intent, 'FAREWELL');
assert.equal(farewell.localOnly, true);

assert.equal(isShortConversationalUtterance('Hey SILA'), true);
assert.equal(isShortConversationalUtterance('order ten bottles of soda'), false);

const business = resolveDialogueIntent('order ten bottles of Bombay Sapphire');
assert.equal(business.intent, 'BUSINESS');
assert.equal(business.localOnly, false);
assert.equal(business.spokenReply, null);

console.log('dialogueIntents.test.ts: ok');
