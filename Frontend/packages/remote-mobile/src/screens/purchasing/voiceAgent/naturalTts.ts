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

/** Speak with a warmer, less robotic cadence. Resolves when playback ends or is cancelled. */
export const speakNaturally = (text: string, muted = false): SpeakHandle => {
  let cancelled = false;
  let utterance: SpeechSynthesisUtterance | null = null;
  const done = new Promise<void>((resolve) => {
    if (!text || muted || typeof window === 'undefined' || !window.speechSynthesis) {
      resolve();
      return;
    }
    window.speechSynthesis.cancel();
    const run = () => {
      if (cancelled) {
        resolve();
        return;
      }
      utterance = new SpeechSynthesisUtterance(text);
      const voice = pickNaturalVoice();
      if (voice) utterance.voice = voice;
      utterance.rate = 0.96;
      utterance.pitch = 1.04;
      utterance.volume = 1;
      utterance.onend = () => resolve();
      utterance.onerror = () => resolve();
      window.speechSynthesis.speak(utterance);
    };
    // Chrome loads voices asynchronously.
    if (window.speechSynthesis.getVoices().length === 0) {
      const onVoices = () => {
        window.speechSynthesis.removeEventListener('voiceschanged', onVoices);
        run();
      };
      window.speechSynthesis.addEventListener('voiceschanged', onVoices);
      window.setTimeout(run, 250);
    } else {
      run();
    }
  });

  return {
    done,
    cancel: () => {
      cancelled = true;
      if (typeof window !== 'undefined' && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
    },
  };
};
