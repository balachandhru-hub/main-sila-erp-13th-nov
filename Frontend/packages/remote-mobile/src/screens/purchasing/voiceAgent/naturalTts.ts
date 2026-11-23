/** Prefer natural neural-like browser voices over robotic defaults. */

export type SpeakHandle = {
  cancel: () => void;
  done: Promise<void>;
};

const PREFERRED_VOICE_PATTERNS = [
  /google uk english female/i,
  /google us english/i,
  /microsoft aria/i,
  /microsoft jenny/i,
  /microsoft guy/i,
  /microsoft sona/i,
  /natural/i,
  /neural/i,
  /samantha/i,
  /karen/i,
  /moira/i,
  /fiona/i,
  /tessa/i,
];

const CONTINUATION_TAIL =
  /\b(and|or|of|for|from|with|to|the|a|an|ten|twenty|thirty|forty|fifty|sixty|seventy|eighty|ninety|hundred|order|get|buy|need|want|please)\s*$/i;

let unlocked = false;
let keepAliveTimer: ReturnType<typeof setInterval> | null = null;

/** True when the transcript looks incomplete (do not auto-submit yet). */
export const looksIncompleteUtterance = (text: string): boolean => {
  const trimmed = text.trim();
  if (!trimmed) return true;
  if (/[.!?]"?$/.test(trimmed)) return false;
  if (CONTINUATION_TAIL.test(trimmed)) return true;
  if (trimmed.endsWith('...') || trimmed.endsWith('…')) return true;
  const words = trimmed.split(/\s+/).filter(Boolean);
  return words.length < 2;
};

/** Phrases that mean the user is finished ordering and ready to review. */
export const isConversationDonePhrase = (text: string | null | undefined): boolean => {
  const value = (text || '').trim().toLowerCase();
  if (!value) return false;
  if (value === 'done' || value === 'finished') return true;
  return /\b(that'?s all|i'?m done|im done|done ordering|finish(?:ed)?|review (?:the )?(?:order|draft|po)|show (?:the )?(?:draft|order)|ready to (?:submit|confirm)|submit when ready)\b/.test(
    value,
  );
};

export const pickNaturalVoice = (): SpeechSynthesisVoice | null => {
  if (typeof window === 'undefined' || !window.speechSynthesis) return null;
  const voices = window.speechSynthesis.getVoices();
  if (!voices.length) return null;
  const english = voices.filter((voice) => /^en([-_]|$)/i.test(voice.lang || ''));
  const pool = english.length ? english : voices;
  for (const pattern of PREFERRED_VOICE_PATTERNS) {
    const match = pool.find((voice) => pattern.test(voice.name));
    if (match) return match;
  }
  return pool.find((voice) => /female|woman/i.test(voice.name)) || pool[0] || null;
};

const stopKeepAlive = () => {
  if (keepAliveTimer != null) {
    clearInterval(keepAliveTimer);
    keepAliveTimer = null;
  }
};

const startKeepAlive = () => {
  stopKeepAlive();
  // Chromium pauses speechSynthesis after ~15s without a resume nudge.
  keepAliveTimer = setInterval(() => {
    if (typeof window === 'undefined' || !window.speechSynthesis) return;
    if (window.speechSynthesis.speaking) {
      window.speechSynthesis.pause();
      window.speechSynthesis.resume();
    }
  }, 9000);
};

/** Call inside a user gesture so later agent replies can play out loud. */
export const unlockSpeechPlayback = (): void => {
  if (typeof window === 'undefined' || !window.speechSynthesis) return;
  try {
    window.speechSynthesis.resume();
    // Do not cancel immediately — Chromium needs the gesture-tied utterance to start.
    const unlock = new SpeechSynthesisUtterance('Hi');
    unlock.volume = 0.001;
    unlock.rate = 2;
    unlock.pitch = 1;
    window.speechSynthesis.speak(unlock);
    unlocked = true;
  } catch {
    /* ignore */
  }
};

export const isSpeechUnlocked = (): boolean => unlocked;

/** Speak with a warmer, less robotic cadence. Resolves when playback ends or is cancelled. */
export const speakNaturally = (text: string, muted = false): SpeakHandle => {
  let cancelled = false;
  let settled = false;
  const done = new Promise<void>((resolve) => {
    const finish = () => {
      if (settled) return;
      settled = true;
      stopKeepAlive();
      resolve();
    };

    if (!text || muted || typeof window === 'undefined' || !window.speechSynthesis) {
      finish();
      return;
    }

    if (!unlocked) {
      unlockSpeechPlayback();
    }

    window.speechSynthesis.cancel();
    window.speechSynthesis.resume();

    const run = () => {
      if (cancelled) {
        finish();
        return;
      }
      const utterance = new SpeechSynthesisUtterance(text);
      const voice = pickNaturalVoice();
      if (voice) {
        utterance.voice = voice;
        utterance.lang = voice.lang || 'en-US';
      } else {
        utterance.lang = 'en-US';
      }
      utterance.rate = 0.96;
      utterance.pitch = 1.04;
      utterance.volume = 1;
      utterance.onstart = () => startKeepAlive();
      utterance.onend = finish;
      utterance.onerror = finish;
      try {
        window.speechSynthesis.resume();
        window.speechSynthesis.speak(utterance);
      } catch {
        finish();
      }
    };

    // Chrome loads voices asynchronously; brief defer also helps after cancel().
    const kick = () => window.setTimeout(run, 40);
    if (window.speechSynthesis.getVoices().length === 0) {
      const onVoices = () => {
        window.speechSynthesis.removeEventListener('voiceschanged', onVoices);
        kick();
      };
      window.speechSynthesis.addEventListener('voiceschanged', onVoices);
      window.setTimeout(kick, 250);
    } else {
      kick();
    }

    window.setTimeout(finish, Math.min(120_000, 4_000 + text.length * 80));
  });

  return {
    done,
    cancel: () => {
      cancelled = true;
      stopKeepAlive();
      if (typeof window !== 'undefined' && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
    },
  };
};
