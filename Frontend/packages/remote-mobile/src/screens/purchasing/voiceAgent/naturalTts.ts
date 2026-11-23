/** Prefer audible server WAV when browser speechSynthesis has no voices. */

import { synthesizeVoiceTts } from '../../../../../remote-buyer/src/api/silaMe/silaVoiceEngineApi';

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
let audioCtx: AudioContext | null = null;
let keepAliveTimer: ReturnType<typeof setInterval> | null = null;
let browserSynthUsable: boolean | null = null;
const audioCache = new Map<string, string>();

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
  keepAliveTimer = setInterval(() => {
    if (typeof window === 'undefined' || !window.speechSynthesis) return;
    if (window.speechSynthesis.speaking) {
      window.speechSynthesis.pause();
      window.speechSynthesis.resume();
    }
  }, 9000);
};

const ensureAudioContext = async (): Promise<AudioContext | null> => {
  if (typeof window === 'undefined') return null;
  const Ctor =
    window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!Ctor) return null;
  if (!audioCtx || audioCtx.state === 'closed') {
    audioCtx = new Ctor();
  }
  if (audioCtx.state === 'suspended') {
    try {
      await audioCtx.resume();
    } catch {
      /* ignore */
    }
  }
  return audioCtx;
};

/** Call inside a user gesture so later agent replies can play out loud. */
export const unlockSpeechPlayback = (): void => {
  unlocked = true;
  void ensureAudioContext().then((ctx) => {
    if (!ctx) return;
    try {
      const buffer = ctx.createBuffer(1, 1, ctx.sampleRate);
      const source = ctx.createBufferSource();
      source.buffer = buffer;
      source.connect(ctx.destination);
      source.start(0);
    } catch {
      /* ignore */
    }
  });
  if (typeof window !== 'undefined' && window.speechSynthesis) {
    try {
      window.speechSynthesis.resume();
      const unlock = new SpeechSynthesisUtterance(' ');
      unlock.volume = 0;
      unlock.rate = 2;
      window.speechSynthesis.speak(unlock);
    } catch {
      /* ignore */
    }
  }
};

export const isSpeechUnlocked = (): boolean => unlocked;

const probeBrowserSynth = async (): Promise<boolean> => {
  if (browserSynthUsable != null) return browserSynthUsable;
  if (typeof window === 'undefined' || !window.speechSynthesis) {
    browserSynthUsable = false;
    return false;
  }
  await new Promise((resolve) => setTimeout(resolve, 50));
  let voices = window.speechSynthesis.getVoices();
  if (!voices.length) {
    await new Promise<void>((resolve) => {
      const done = () => resolve();
      window.speechSynthesis.addEventListener('voiceschanged', done, { once: true });
      window.setTimeout(done, 400);
    });
    voices = window.speechSynthesis.getVoices();
  }
  if (!voices.length) {
    browserSynthUsable = false;
    return false;
  }
  browserSynthUsable = await new Promise<boolean>((resolve) => {
    const utterance = new SpeechSynthesisUtterance('ok');
    utterance.volume = 0;
    utterance.rate = 2;
    let settled = false;
    const finish = (ok: boolean) => {
      if (settled) return;
      settled = true;
      resolve(ok);
    };
    utterance.onstart = () => finish(true);
    utterance.onend = () => finish(true);
    utterance.onerror = () => finish(false);
    try {
      window.speechSynthesis.speak(utterance);
    } catch {
      finish(false);
    }
    window.setTimeout(() => finish(false), 800);
  });
  return browserSynthUsable;
};

const fetchServerWav = async (text: string): Promise<Blob> => {
  const cached = audioCache.get(text);
  if (cached) {
    const response = await fetch(cached);
    if (response.ok) return response.blob();
  }
  const blob = await synthesizeVoiceTts(text, 'en');
  if (!blob?.size) throw new Error('Server TTS returned empty audio');
  // Cache short greetings only.
  if (text.length <= 120 && audioCache.size < 24) {
    const url = URL.createObjectURL(blob);
    audioCache.set(text, url);
  }
  return blob;
};

const playWavBlob = async (blob: Blob, cancelled: () => boolean): Promise<void> => {
  const ctx = await ensureAudioContext();
  if (!ctx) {
    // Fallback HTMLAudioElement
    const url = URL.createObjectURL(blob);
    try {
      const audio = new Audio(url);
      audio.volume = 1;
      await new Promise<void>((resolve, reject) => {
        audio.onended = () => resolve();
        audio.onerror = () => reject(new Error('HTMLAudio playback failed'));
        void audio.play().catch(reject);
      });
    } finally {
      URL.revokeObjectURL(url);
    }
    return;
  }

  const arrayBuffer = await blob.arrayBuffer();
  if (cancelled()) return;
  const audioBuffer = await ctx.decodeAudioData(arrayBuffer.slice(0));
  if (cancelled()) return;
  await new Promise<void>((resolve) => {
    const source = ctx.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(ctx.destination);
    source.onended = () => resolve();
    try {
      source.start(0);
    } catch {
      resolve();
    }
    // Safety timeout
    window.setTimeout(resolve, Math.min(120_000, (audioBuffer.duration + 1) * 1000));
  });
};

const speakViaBrowser = (text: string, cancelled: () => boolean, finish: () => void): void => {
  if (!window.speechSynthesis) {
    finish();
    return;
  }
  window.speechSynthesis.cancel();
  window.speechSynthesis.resume();
  const run = () => {
    if (cancelled()) {
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
    utterance.onerror = () => {
      browserSynthUsable = false;
      finish();
    };
    try {
      window.speechSynthesis.speak(utterance);
    } catch {
      browserSynthUsable = false;
      finish();
    }
  };
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
};

/** Speak with server WAV fallback when browser speechSynthesis cannot produce audio. */
export const speakNaturally = (text: string, muted = false): SpeakHandle => {
  let cancelled = false;
  let settled = false;
  let htmlAudio: HTMLAudioElement | null = null;

  const done = new Promise<void>((resolve) => {
    const finish = () => {
      if (settled) return;
      settled = true;
      stopKeepAlive();
      resolve();
    };

    if (!text || muted || typeof window === 'undefined') {
      // Hard mute: cancel any leftover browser/server speech immediately.
      if (typeof window !== 'undefined' && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
      finish();
      return;
    }

    if (!unlocked) {
      unlockSpeechPlayback();
    }

    void (async () => {
      const useBrowser = await probeBrowserSynth();
      if (cancelled) {
        finish();
        return;
      }
      if (useBrowser) {
        speakViaBrowser(text, () => cancelled, finish);
        window.setTimeout(finish, Math.min(120_000, 4_000 + text.length * 80));
        return;
      }
      try {
        const blob = await fetchServerWav(text);
        if (cancelled) {
          finish();
          return;
        }
        await playWavBlob(blob, () => cancelled);
      } catch (error) {
        console.warn('[SILA Voice] audible TTS failed', error);
        // Last resort: still try browser synth even if probe failed.
        speakViaBrowser(text, () => cancelled, finish);
        window.setTimeout(finish, 2500);
        return;
      }
      finish();
    })();
  });

  return {
    done,
    cancel: () => {
      cancelled = true;
      stopKeepAlive();
      if (typeof window !== 'undefined' && window.speechSynthesis) {
        window.speechSynthesis.cancel();
      }
      if (htmlAudio) {
        try {
          htmlAudio.pause();
        } catch {
          /* ignore */
        }
      }
      if (audioCtx) {
        try {
          void audioCtx.suspend();
          void audioCtx.resume();
        } catch {
          /* ignore */
        }
      }
    },
  };
};
