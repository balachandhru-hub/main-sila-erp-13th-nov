/**
 * Lightweight local dialogue intents for SILA Voice 2.0.
 * Greetings / wake / social / farewell are handled without the PO agent.
 * Business remainder (if any) is passed through unchanged.
 */

export type DialogueIntent =
  | 'WAKE_ACK'
  | 'GREETING'
  | 'AVAILABILITY'
  | 'SOCIAL'
  | 'GRATITUDE'
  | 'FAREWELL'
  | 'BUSINESS'
  | 'UNSUPPORTED';

export type DialogueResolution = {
  intent: DialogueIntent;
  /** Spoken acknowledgement for non-business intents (or wake prefix of a combined utterance). */
  spokenReply: string | null;
  /** Remaining text for the PO agent; null/empty means do not call the PO turn API. */
  businessTranscript: string | null;
  /** True when only conversational handling is needed (no PO turn). */
  localOnly: boolean;
};

const normalize = (text: string): string =>
  (text || '')
    .toLowerCase()
    .replace(/[^\p{L}\p{N}\s']/gu, ' ')
    .replace(/\s+/g, ' ')
    .trim();

/** Activation / greeting prefixes that should not be treated as purchasing requests. */
const WAKE_PREFIX =
  /^(?:hey|hi|hello|okay|ok|good\s+(?:morning|afternoon|evening))\s+sila\b[,!]?\s*/i;

const AVAILABILITY =
  /^(?:sila[,!]?\s+)?(?:are you there|can you hear me|you there)\??$/i;

const PURE_WAKE =
  /^(?:hey|hi|hello|okay|ok|good\s+(?:morning|afternoon|evening))\s+sila\b[,!]?$/i;

const GREETING_ONLY = /^(?:hi|hello|hey|good\s+(?:morning|afternoon|evening))\b[,!]?$/i;

const SOCIAL = /^(?:how are you|how'?s it going|what'?s up|how do you do)\b/i;

const GRATITUDE = /^(?:thanks|thank you|thx|cheers)\b/i;

const FAREWELL =
  /^(?:no[, ]?(?:thank you|thanks)?|that'?s all|nothing else|goodbye|good bye|bye(?:\s+sila)?|stop(?:\s+sila)?|end conversation|nope)\b/i;

const HEY_REPLIES = [
  "Hi! I'm here. How can I help you today?",
  "Hello! I'm listening — what do you need?",
  "Hi! Ready when you are.",
];

const HI_REPLIES = [
  'Hello! What can I help you with?',
  "Hi there! How can I help?",
  "Hello! I'm ready.",
];

const AVAIL_REPLIES = [
  "Yes, I'm here. Go ahead.",
  "I'm here — go ahead.",
  'Yes, I can hear you. What do you need?',
];

const SOCIAL_REPLIES = [
  "I'm doing well, thank you. How can I help with purchasing?",
  "Doing great — ready to help with your order.",
];

const GRATITUDE_REPLIES = ["You're welcome.", 'Glad to help.', 'Anytime.'];

const FAREWELL_REPLIES = [
  "You're welcome. Have a great day!",
  'Glad I could help. Have a great day!',
  'Okay — talk soon.',
];

const COMBINED_WAKE_ACK = [
  'Sure, let me check the material and your usual supplier.',
  'Got it — working on that for you.',
  'Of course. Let me take care of that.',
];

let replyCursor = 0;
const pick = (options: string[]): string => {
  const value = options[replyCursor % options.length];
  replyCursor += 1;
  return value;
};

const isWakeWithBusiness = (normalized: string): { wakeKind: 'hey' | 'hi' | 'other'; remainder: string } | null => {
  const match = normalized.match(
    /^(hey|hi|hello|okay|ok|good\s+(?:morning|afternoon|evening))\s+sila\b[,!]?\s+(.+)$/i,
  );
  if (!match) return null;
  const wake = (match[1] || '').toLowerCase();
  const remainder = (match[2] || '').trim();
  if (!remainder) return null;
  if (wake === 'hey') return { wakeKind: 'hey', remainder };
  if (wake === 'hi' || wake === 'hello') return { wakeKind: 'hi', remainder };
  return { wakeKind: 'other', remainder };
};

/**
 * Resolve conversational intent for a finalized utterance.
 * Wake + business command keeps the purchasing remainder for the existing PO agent.
 */
export const resolveDialogueIntent = (transcript: string): DialogueResolution => {
  const raw = (transcript || '').trim();
  const normalized = normalize(raw);
  if (!normalized) {
    return {
      intent: 'UNSUPPORTED',
      spokenReply: "I didn't catch that. Could you say that again?",
      businessTranscript: null,
      localOnly: true,
    };
  }

  // Combined: "Hey SILA, order ten bottles…"
  const combined = isWakeWithBusiness(normalized);
  if (combined) {
    return {
      intent: 'BUSINESS',
      spokenReply: pick(COMBINED_WAKE_ACK),
      businessTranscript: combined.remainder,
      localOnly: false,
    };
  }

  if (AVAILABILITY.test(normalized) || /^sila[,!]?\s+(?:are you there|can you hear me)\??$/i.test(normalized)) {
    return {
      intent: 'AVAILABILITY',
      spokenReply: pick(AVAIL_REPLIES),
      businessTranscript: null,
      localOnly: true,
    };
  }

  if (PURE_WAKE.test(normalized)) {
    const hey = /^hey\s+sila\b/i.test(normalized);
    const hi = /^(?:hi|hello)\s+sila\b/i.test(normalized);
    return {
      intent: hey ? 'WAKE_ACK' : 'GREETING',
      spokenReply: hey ? pick(HEY_REPLIES) : hi ? pick(HI_REPLIES) : pick(HEY_REPLIES),
      businessTranscript: null,
      localOnly: true,
    };
  }

  if (GREETING_ONLY.test(normalized)) {
    return {
      intent: 'GREETING',
      spokenReply: pick(HI_REPLIES),
      businessTranscript: null,
      localOnly: true,
    };
  }

  if (SOCIAL.test(normalized)) {
    return {
      intent: 'SOCIAL',
      spokenReply: pick(SOCIAL_REPLIES),
      businessTranscript: null,
      localOnly: true,
    };
  }

  if (FAREWELL.test(normalized)) {
    return {
      intent: 'FAREWELL',
      spokenReply: pick(FAREWELL_REPLIES),
      businessTranscript: null,
      localOnly: true,
    };
  }

  if (GRATITUDE.test(normalized)) {
    return {
      intent: 'GRATITUDE',
      spokenReply: pick(GRATITUDE_REPLIES),
      businessTranscript: null,
      localOnly: true,
    };
  }

  // Strip a leading wake prefix if ASR glued it to a business phrase without clear remainder match above.
  const stripped = normalized.replace(WAKE_PREFIX, '').trim();
  if (stripped && stripped !== normalized) {
    return {
      intent: 'BUSINESS',
      spokenReply: pick(COMBINED_WAKE_ACK),
      businessTranscript: stripped,
      localOnly: false,
    };
  }

  return {
    intent: 'BUSINESS',
    spokenReply: null,
    businessTranscript: raw,
    localOnly: false,
  };
};

/** Short wake / greeting utterances must not be dropped by incomplete/min-duration gates. */
export const isShortConversationalUtterance = (transcript: string): boolean => {
  const normalized = normalize(transcript);
  if (!normalized) return false;
  if (PURE_WAKE.test(normalized) || GREETING_ONLY.test(normalized) || AVAILABILITY.test(normalized)) return true;
  if (SOCIAL.test(normalized) || GRATITUDE.test(normalized) || FAREWELL.test(normalized)) return true;
  return false;
};
