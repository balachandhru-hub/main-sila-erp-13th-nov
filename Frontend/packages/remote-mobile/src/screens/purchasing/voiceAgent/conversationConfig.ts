/** Continuous conversation tuning (Voice 2.0). */

export const VOICE2_CONFIG = {
  /** Speech-start confirmation (ms of elevated energy). */
  speechStartMs: 200,
  /** End-of-speech silence before turn submit (ms). */
  endOfSpeechSilenceMs: 700,
  /** Extra silence when utterance looks incomplete (ms). */
  incompleteSilenceMs: 1400,
  /** Minimum spoken duration before a turn can complete (ms). */
  minUtteranceMs: 450,
  /** Shorter floor for wake / greeting phrases. */
  minGreetingUtteranceMs: 180,
  /** Ignore tiny RMS blips. */
  speechLevelThreshold: 10,
  /** Strong speech for barge-in while SILA is speaking. */
  bargeInLevelThreshold: 22,
  /** Ignore barge-in briefly after TTS starts (blocks self-echo). */
  bargeInMuteMs: 700,
  /** Inactivity before session soft-pauses listening (ms). */
  inactivityTimeoutMs: 120_000,
  continuousConversation: true,
  autoEndOfTurn: true,
  autoPlayback: true,
  bargeInEnabled: true,
} as const;

export type Voice2UiState =
  | 'idle'
  | 'activating'
  | 'acknowledging'
  | 'listening'
  | 'userSpeaking'
  | 'transcribing'
  | 'processing'
  | 'speaking'
  | 'awaitingConfirmation'
  | 'confirmTranscript'
  | 'reviewPo'
  | 'closing'
  | 'done'
  | 'error';
