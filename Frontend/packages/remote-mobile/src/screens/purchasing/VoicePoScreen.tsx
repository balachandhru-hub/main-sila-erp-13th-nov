import React, { useCallback, useEffect, useRef, useState } from 'react';
import {
  cancelVoicePoSession,
  confirmVoicePoDraft,
  resolveMyPurchasingDefaults,
  sendVoicePoTurn,
  startVoicePoSession,
  type SilaPurchasingDefaultsResolved,
  type SilaVoicePoSession,
} from '../../../../remote-buyer/src/api/silaMe/silaVoicePoApi';
import ScreenHeader from '../../components/ScreenHeader';
import { Empty, ErrorNotice, Notice, type NoticeMessage } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { errorText } from '../../useLoad';
import { VOICE2_CONFIG, type Voice2UiState } from './voiceAgent/conversationConfig';
import {
  isShortConversationalUtterance,
  resolveDialogueIntent,
} from './voiceAgent/dialogueIntents';
import {
  isConversationDonePhrase,
  looksIncompleteUtterance,
  speakNaturally,
  unlockSpeechPlayback,
  type SpeakHandle,
} from './voiceAgent/naturalTts';
import VoiceAgentOverlay from './voiceAgent/VoiceAgentOverlay';
import './VoicePoScreen.css';

type SpeechRecognitionLike = {
  continuous: boolean;
  interimResults: boolean;
  lang: string;
  onresult: ((event: SpeechRecognitionEventLike) => void) | null;
  onerror: ((event: { error?: string }) => void) | null;
  onend: (() => void) | null;
  start: () => void;
  stop: () => void;
  abort: () => void;
};

type SpeechRecognitionEventLike = {
  resultIndex: number;
  results: ArrayLike<{ 0: { transcript: string }; isFinal: boolean; length: number }>;
};

const SpeechRecognitionCtor: (new () => SpeechRecognitionLike) | null =
  typeof window !== 'undefined'
    ? ((window as unknown as { SpeechRecognition?: new () => SpeechRecognitionLike }).SpeechRecognition ??
        (window as unknown as { webkitSpeechRecognition?: new () => SpeechRecognitionLike }).webkitSpeechRecognition ??
        null)
    : null;

const openSession = (status?: string | null) => {
  const value = (status || '').toUpperCase();
  return value === 'OPEN' || value === 'REVIEW' || value === '';
};

/** Safari/WebKit SpeechRecognition is unreliable with continuous=true after TTS. */
const isSafariLike = (): boolean => {
  if (typeof navigator === 'undefined') return false;
  const ua = navigator.userAgent || '';
  const isSafari = /safari/i.test(ua) && !/chrome|chromium|crios|fxios|edg/i.test(ua);
  const isIos = /iphone|ipad|ipod/i.test(ua);
  return isSafari || isIos;
};

/** Chromium family (Chrome, Edge, Android Chrome). Keep Safari/iOS on voice for now. */
const chromeTextPrimary = (): boolean => {
  if (typeof navigator === 'undefined') return false;
  const ua = navigator.userAgent || '';
  return /chrome|chromium|crios|edg/i.test(ua) && !isSafariLike();
};

/**
 * SILA Voice 2.0 — continuous conversation on the existing Voice PO agent.
 * One activation → auto listen → end-of-speech → auto turn → natural TTS → resume listen.
 * Explicit Confirm still required for PO create.
 */
const VoicePoScreen: React.FC = () => {
  const { can, loading: accessLoading } = useAccess();
  const { location: mine } = useMyLocation();

  const [defaults, setDefaults] = useState<SilaPurchasingDefaultsResolved | null>(null);
  const [session, setSession] = useState<SilaVoicePoSession | null>(null);
  const [transcript, setTranscript] = useState('');
  const [confirmedTranscript, setConfirmedTranscript] = useState('');
  const [uiState, setUiState] = useState<Voice2UiState>('idle');
  const [level, setLevel] = useState(0);
  const [sessionActive, setSessionActive] = useState(false);
  const textPrimary = chromeTextPrimary();
  const [showType, setShowType] = useState(textPrimary);
  const [ttsMuted, setTtsMuted] = useState(textPrimary);
  const [busy, setBusy] = useState(false);
  const [bootError, setBootError] = useState<string | null>(null);
  const [notice, setNotice] = useState<NoticeMessage | null>(null);
  const [agentReply, setAgentReply] = useState<string | null>(null);

  const recognitionRef = useRef<SpeechRecognitionLike | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const audioCtxRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const rafRef = useRef<number | null>(null);
  const sessionActiveRef = useRef(false);
  const uiStateRef = useRef<Voice2UiState>('idle');
  const transcriptRef = useRef('');
  const finalChunkRef = useRef('');
  const speechStartedAtRef = useRef<number | null>(null);
  const lastSpeechAtRef = useRef(0);
  const silenceTimerRef = useRef<number | null>(null);
  const submittingRef = useRef(false);
  const speakHandleRef = useRef<SpeakHandle | null>(null);
  const turnIdRef = useRef(0);
  const spokenRef = useRef<string | null>(null);
  const levelRef = useRef(0);
  const sessionDataRef = useRef<SilaVoicePoSession | null>(null);
  const speakStartedAtRef = useRef(0);
  const recognitionRunningRef = useRef(false);
  const beginRecognitionRef = useRef<() => void>(() => undefined);

  const setUi = useCallback((next: Voice2UiState) => {
    uiStateRef.current = next;
    setUiState(next);
  }, []);

  const stopMeter = useCallback(() => {
    if (rafRef.current != null) {
      cancelAnimationFrame(rafRef.current);
      rafRef.current = null;
    }
    analyserRef.current = null;
    if (audioCtxRef.current) {
      void audioCtxRef.current.close().catch(() => undefined);
      audioCtxRef.current = null;
    }
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    }
    setLevel(0);
    levelRef.current = 0;
  }, []);

  const stopRecognition = useCallback(() => {
    const recognition = recognitionRef.current;
    recognitionRef.current = null;
    recognitionRunningRef.current = false;
    if (recognition) {
      recognition.onresult = null;
      recognition.onerror = null;
      recognition.onend = null;
      try {
        recognition.abort();
      } catch {
        try {
          recognition.stop();
        } catch {
          /* ignore */
        }
      }
    }
  }, []);

  const clearSilenceTimer = useCallback(() => {
    if (silenceTimerRef.current != null) {
      window.clearTimeout(silenceTimerRef.current);
      silenceTimerRef.current = null;
    }
  }, []);

  const interruptSpeech = useCallback(() => {
    speakHandleRef.current?.cancel();
    speakHandleRef.current = null;
    if (typeof window !== 'undefined' && window.speechSynthesis) {
      window.speechSynthesis.cancel();
    }
  }, []);

  useEffect(() => {
    transcriptRef.current = transcript;
  }, [transcript]);

  useEffect(() => {
    sessionDataRef.current = session;
  }, [session]);

  useEffect(() => {
    return () => {
      sessionActiveRef.current = false;
      clearSilenceTimer();
      stopRecognition();
      stopMeter();
      interruptSpeech();
    };
  }, [clearSilenceTimer, interruptSpeech, stopMeter, stopRecognition]);

  useEffect(() => {
    if (!can.requestPurchase) return;
    let active = true;
    setBootError(null);
    resolveMyPurchasingDefaults({
      locationId: mine?.id,
      propertyId: mine?.propertyId,
      outletId: mine?.locationType === 'OUTLET' ? mine.id : null,
    })
      .then((data) => {
        if (!active) return;
        setDefaults(data);
      })
      .catch((err: unknown) => {
        if (!active) return;
        setDefaults({ profileFound: false, resolved: false, validationErrors: [] });
        setNotice({ tone: 'warning', text: errorText(err) });
      });
    return () => {
      active = false;
    };
  }, [can.requestPurchase, mine?.id, mine?.locationType, mine?.propertyId]);

  // Chrome text-first: open a session and show the composer without requiring Voice.
  useEffect(() => {
    if (!can.requestPurchase || !textPrimary || session) return;
    let active = true;
    void (async () => {
      try {
        const started = await startVoicePoSession({
          locationId: mine?.id ?? null,
          propertyId: mine?.propertyId ?? null,
          outletId: mine?.locationType === 'OUTLET' ? mine.id : null,
          purchasingProfileId: defaults?.profileId ?? null,
          language: 'en',
        });
        if (!active) return;
        setSession(started);
        sessionDataRef.current = started;
        sessionActiveRef.current = true;
        setSessionActive(true);
        setShowType(true);
        setUi('listening');
        const greeting =
          "Type your order — for example 'order 25 kg specialty spice at 25 AED'. I will ask for any missing price or UOM.";
        setAgentReply(greeting);
        setNotice({ tone: 'info', text: greeting });
      } catch (err: unknown) {
        if (!active) return;
        setNotice({ tone: 'error', text: errorText(err) });
      }
    })();
    return () => {
      active = false;
    };
  }, [can.requestPurchase, defaults?.profileId, mine?.id, mine?.locationType, mine?.propertyId, session, textPrimary]);

  const ensureSession = useCallback(async (): Promise<SilaVoicePoSession> => {
    if (session && openSession(session.status)) return session;
    const started = await startVoicePoSession({
      locationId: mine?.id ?? defaults?.locationId ?? null,
      propertyId: mine?.propertyId ?? defaults?.propertyId ?? null,
      outletId: mine?.locationType === 'OUTLET' ? mine.id : defaults?.outletId ?? null,
      purchasingProfileId: defaults?.profileId ?? null,
      language: 'en',
    });
    setSession(started);
    sessionDataRef.current = started;
    return started;
  }, [defaults, mine, session]);

  const resumeListening = useCallback(() => {
    if (!sessionActiveRef.current) return;
    if (
      uiStateRef.current === 'processing' ||
      uiStateRef.current === 'transcribing' ||
      uiStateRef.current === 'confirmTranscript' ||
      uiStateRef.current === 'reviewPo' ||
      uiStateRef.current === 'done' ||
      uiStateRef.current === 'closing'
    ) {
      return;
    }
    // Leave speaking/acknowledging — caller finished TTS and wants the mic open again.
    setUi('listening');
    speechStartedAtRef.current = null;
    finalChunkRef.current = '';
    setTranscript('');
    transcriptRef.current = '';
    // Always hard-restart recognition after TTS — Safari ends the session and will not resume.
    const delay = isSafariLike() ? 280 : 120;
    window.setTimeout(() => {
      if (!sessionActiveRef.current) return;
      if (
        uiStateRef.current === 'speaking' ||
        uiStateRef.current === 'acknowledging' ||
        uiStateRef.current === 'processing' ||
        uiStateRef.current === 'transcribing' ||
        uiStateRef.current === 'confirmTranscript' ||
        uiStateRef.current === 'reviewPo' ||
        uiStateRef.current === 'done' ||
        uiStateRef.current === 'closing'
      ) {
        return;
      }
      beginRecognitionRef.current();
    }, delay);
  }, [setUi]);

  const enterTranscriptConfirmation = useCallback(
    async (seed?: string) => {
      turnIdRef.current += 1;
      sessionActiveRef.current = false;
      setSessionActive(false);
      clearSilenceTimer();
      interruptSpeech();
      stopRecognition();
      stopMeter();
      const current = sessionDataRef.current;
      const fromTurns =
        current?.turns
          ?.filter((turn) => (turn.role || '').toUpperCase() === 'USER' && turn.transcript)
          .map((turn) => turn.transcript)
          .join(' ') || '';
      const summary = (seed || transcriptRef.current || current?.latestTranscript || fromTurns || '').trim();
      setConfirmedTranscript(summary);
      setUi('confirmTranscript');
      setNotice({
        tone: 'success',
        text: 'Confirm the transcript, then review header and items before Submit.',
      });
      const handle = speakNaturally(
        'Okay. Please confirm the transcript on screen, then review the purchase order and press Submit.',
        ttsMuted,
      );
      speakHandleRef.current = handle;
      await handle.done;
      speakHandleRef.current = null;
    },
    [clearSilenceTimer, interruptSpeech, setUi, stopMeter, stopRecognition, ttsMuted],
  );

  const speakAndResume = useCallback(
    async (reply: string, turnId: number, nextUi: Voice2UiState = 'listening') => {
      const spoken = (reply || '').trim();
      if (!spoken) {
        if (nextUi === 'listening') resumeListening();
        else setUi(nextUi);
        return;
      }
      spokenRef.current = spoken;
      stopRecognition();
      setUi(nextUi === 'closing' ? 'closing' : 'acknowledging');
      speakStartedAtRef.current = Date.now();
      const handle = speakNaturally(spoken, ttsMuted);
      speakHandleRef.current = handle;
      await handle.done;
      speakHandleRef.current = null;
      if (turnId !== turnIdRef.current) return;
      if (nextUi === 'closing') {
        sessionActiveRef.current = false;
        setSessionActive(false);
        clearSilenceTimer();
        stopRecognition();
        stopMeter();
        setUi('idle');
        return;
      }
      if (!sessionActiveRef.current) return;
      resumeListening();
    },
    [clearSilenceTimer, resumeListening, setUi, stopMeter, stopRecognition, ttsMuted],
  );

  const submitTurn = useCallback(
    async (text: string, channel: 'VOICE' | 'TEXT' = 'VOICE') => {
      const cleaned = text.trim();
      if (!cleaned || submittingRef.current) return;
      // Voice turns require an active listen loop; Type fallback may run without Voice activated.
      if (channel === 'VOICE' && !sessionActiveRef.current) return;
      if (channel === 'TEXT' && !sessionActiveRef.current) {
        sessionActiveRef.current = true;
        setSessionActive(true);
      }
      submittingRef.current = true;
      clearSilenceTimer();
      const turnId = ++turnIdRef.current;
      setBusy(true);
      setNotice(null);
      interruptSpeech();
      // Safari: stop recognition before TTS / network so mic can re-arm cleanly afterward.
      if (channel === 'VOICE' || isSafariLike()) {
        stopRecognition();
      }
      const preferText = channel === 'TEXT' || textPrimary;
      try {
        // Local dialogue manager: wake / greeting / social / farewell before PO agent.
        const dialogue = resolveDialogueIntent(cleaned);
        if (dialogue.localOnly) {
          setTranscript('');
          transcriptRef.current = '';
          finalChunkRef.current = '';
          const localReply = dialogue.spokenReply || "Hi! I'm here. How can I help you today?";
          setAgentReply(localReply);
          if (dialogue.intent === 'FAREWELL') {
            if (preferText) {
              setNotice({ tone: 'info', text: localReply });
              sessionActiveRef.current = false;
              setSessionActive(false);
              setUi('idle');
              return;
            }
            await speakAndResume(localReply || "You're welcome. Have a great day!", turnId, 'closing');
            return;
          }
          if (preferText) {
            setNotice({ tone: 'info', text: localReply });
            setUi('listening');
            return;
          }
          setUi('acknowledging');
          await speakAndResume(localReply, turnId, 'listening');
          return;
        }

        const businessText = (dialogue.businessTranscript || cleaned).trim();
        if (dialogue.spokenReply && !preferText) {
          // Combined wake + command: acknowledge, then continue into PO agent.
          setUi('acknowledging');
          speakStartedAtRef.current = Date.now();
          const ack = speakNaturally(dialogue.spokenReply, ttsMuted);
          speakHandleRef.current = ack;
          await ack.done;
          if (turnId !== turnIdRef.current || !sessionActiveRef.current) return;
          speakHandleRef.current = null;
        }

        const lineCount = sessionDataRef.current?.draft?.lines?.length ?? session?.draft?.lines?.length ?? 0;
        if (isConversationDonePhrase(businessText) && lineCount > 0) {
          await enterTranscriptConfirmation(businessText);
          return;
        }

        setUi('processing');
        const active = await ensureSession();
        if (turnId !== turnIdRef.current) return;
        const next = await sendVoicePoTurn(active.id, {
          transcript: businessText,
          channel: preferText ? 'TEXT' : channel,
        });
        if (turnId !== turnIdRef.current) return;
        setSession(next);
        sessionDataRef.current = next;
        setTranscript('');
        transcriptRef.current = '';
        finalChunkRef.current = '';

        const reply = (next.spokenReply || '').trim();
        const clarifications = next.clarifications ?? [];
        const needsAsk =
          (next.validationStatus || '').toUpperCase() === 'NEEDS_CLARIFICATION' || clarifications.length > 0;
        if (reply) {
          setAgentReply(reply);
          spokenRef.current = reply;
        }
        if (needsAsk && reply) {
          setNotice({ tone: 'warning', text: reply });
        } else if (reply && preferText) {
          setNotice({ tone: 'info', text: reply });
        }
        const hasLines = (next.draft?.lines?.length ?? 0) > 0;
        if (isConversationDonePhrase(businessText) && hasLines) {
          if (reply && !preferText) {
            await speakAndResume(reply, turnId, 'listening');
          }
          await enterTranscriptConfirmation(businessText);
          return;
        }

        const awaiting = next.status === 'REVIEW' || hasLines;
        if (preferText) {
          // Text-first (Chrome): keep the composer open; do not speak or re-arm mic.
          setUi(needsAsk ? 'listening' : awaiting && /confirm/i.test(reply) ? 'awaitingConfirmation' : 'listening');
          setShowType(true);
          return;
        }

        if (reply) {
          setUi('speaking');
          speakStartedAtRef.current = Date.now();
          const handle = speakNaturally(reply, ttsMuted);
          speakHandleRef.current = handle;
          await handle.done;
          if (turnId !== turnIdRef.current || !sessionActiveRef.current) return;
          speakHandleRef.current = null;
          if (awaiting && /confirm/i.test(reply) && !needsAsk) {
            setUi('awaitingConfirmation');
          }
          resumeListening();
        } else {
          resumeListening();
        }
      } catch (err: unknown) {
        setNotice({ tone: 'error', text: errorText(err) });
        setUi('error');
        if (sessionActiveRef.current) {
          window.setTimeout(() => resumeListening(), 1200);
        }
      } finally {
        submittingRef.current = false;
        setBusy(false);
      }
    },
    [
      clearSilenceTimer,
      ensureSession,
      enterTranscriptConfirmation,
      interruptSpeech,
      resumeListening,
      session?.draft?.lines?.length,
      setUi,
      speakAndResume,
      stopRecognition,
      textPrimary,
      ttsMuted,
    ],
  );

  const scheduleEndOfTurn = useCallback(() => {
    clearSilenceTimer();
    if (!VOICE2_CONFIG.autoEndOfTurn || !sessionActiveRef.current) return;
    // Do not finalize a new user turn while SILA is acknowledging / speaking (except barge-in path).
    if (
      uiStateRef.current === 'acknowledging' ||
      uiStateRef.current === 'processing' ||
      uiStateRef.current === 'transcribing'
    ) {
      return;
    }
    const text = (finalChunkRef.current || transcriptRef.current).trim();
    if (!text) return;
    const conversational = isShortConversationalUtterance(text);
    const incomplete = !conversational && looksIncompleteUtterance(text);
    const wait = incomplete ? VOICE2_CONFIG.incompleteSilenceMs : VOICE2_CONFIG.endOfSpeechSilenceMs;
    silenceTimerRef.current = window.setTimeout(() => {
      const uttered = (finalChunkRef.current || transcriptRef.current).trim();
      const started = speechStartedAtRef.current;
      const duration = started ? Date.now() - started : 0;
      const shortOk = isShortConversationalUtterance(uttered);
      const minMs = shortOk ? VOICE2_CONFIG.minGreetingUtteranceMs : VOICE2_CONFIG.minUtteranceMs;
      if (!uttered || duration < minMs) {
        setUi('listening');
        return;
      }
      if (!shortOk && looksIncompleteUtterance(uttered)) {
        if (uttered.split(/\s+/).length < 3) {
          setUi('listening');
          return;
        }
      }
      setUi('transcribing');
      void submitTurn(uttered, 'VOICE');
    }, wait);
  }, [clearSilenceTimer, setUi, submitTurn]);

  const beginRecognition = useCallback(() => {
    if (!SpeechRecognitionCtor) {
      setShowType(true);
      setNotice({ tone: 'warning', text: 'Speech recognition is unavailable. Use Type as a fallback.' });
      return;
    }
    stopRecognition();
    recognitionRunningRef.current = false;
    const safari = isSafariLike();
    const recognition = new SpeechRecognitionCtor();
    // Safari: continuous sessions die after TTS; use one-shot and restart on end.
    recognition.continuous = !safari;
    recognition.interimResults = true;
    recognition.lang = 'en-US';
    recognition.onresult = (event) => {
      if (!sessionActiveRef.current) return;
      const sinceSpeak = Date.now() - speakStartedAtRef.current;
      const speakingNow =
        uiStateRef.current === 'speaking' || uiStateRef.current === 'acknowledging';
      // Barge-in while speaking (after mute window to avoid SILA hearing herself).
      if (
        speakingNow &&
        VOICE2_CONFIG.bargeInEnabled &&
        sinceSpeak >= VOICE2_CONFIG.bargeInMuteMs
      ) {
        interruptSpeech();
        setUi('userSpeaking');
      } else if (speakingNow && sinceSpeak < VOICE2_CONFIG.bargeInMuteMs) {
        return;
      }
      let finalText = '';
      let interim = '';
      for (let i = event.resultIndex; i < event.results.length; i += 1) {
        const result = event.results[i];
        const piece = result[0]?.transcript ?? '';
        if (result.isFinal) finalText += piece;
        else interim += piece;
      }
      if (finalText) {
        finalChunkRef.current = `${finalChunkRef.current}${finalChunkRef.current && !finalChunkRef.current.endsWith(' ') ? ' ' : ''}${finalText}`.trim();
        lastSpeechAtRef.current = Date.now();
        if (!speechStartedAtRef.current) speechStartedAtRef.current = Date.now();
        setUi('userSpeaking');
        scheduleEndOfTurn();
      }
      const live = `${finalChunkRef.current}${interim ? `${finalChunkRef.current ? ' ' : ''}${interim}` : ''}`.trim();
      setTranscript(live);
      transcriptRef.current = live;
      if (interim) {
        lastSpeechAtRef.current = Date.now();
        if (!speechStartedAtRef.current) speechStartedAtRef.current = Date.now();
        setUi('userSpeaking');
        scheduleEndOfTurn();
      }
    };
    recognition.onerror = (event) => {
      if (event.error === 'not-allowed') {
        setNotice({ tone: 'error', text: 'Microphone permission denied.' });
        setUi('error');
        sessionActiveRef.current = false;
        setSessionActive(false);
        recognitionRunningRef.current = false;
        return;
      }
      if (event.error === 'aborted' || event.error === 'no-speech') return;
    };
    recognition.onend = () => {
      recognitionRunningRef.current = false;
      if (!sessionActiveRef.current) return;
      // Defer restart while processing/speaking; resumeListening re-arms afterward.
      if (
        uiStateRef.current === 'processing' ||
        uiStateRef.current === 'transcribing' ||
        uiStateRef.current === 'speaking' ||
        uiStateRef.current === 'acknowledging'
      ) {
        return;
      }
      // Safari one-shot: recreate; Chrome continuous: restart same instance.
      if (safari) {
        window.setTimeout(() => {
          if (!sessionActiveRef.current) return;
          if (
            uiStateRef.current === 'speaking' ||
            uiStateRef.current === 'acknowledging' ||
            uiStateRef.current === 'processing' ||
            uiStateRef.current === 'transcribing'
          ) {
            return;
          }
          beginRecognitionRef.current();
        }, 200);
        return;
      }
      try {
        recognition.start();
        recognitionRunningRef.current = true;
      } catch {
        window.setTimeout(() => {
          if (!sessionActiveRef.current) return;
          try {
            recognition.start();
            recognitionRunningRef.current = true;
          } catch {
            beginRecognitionRef.current();
          }
        }, 250);
      }
    };
    recognitionRef.current = recognition;
    try {
      recognition.start();
      recognitionRunningRef.current = true;
    } catch {
      recognitionRunningRef.current = false;
      window.setTimeout(() => {
        if (!sessionActiveRef.current) return;
        try {
          recognition.start();
          recognitionRunningRef.current = true;
        } catch {
          /* ignore */
        }
      }, 300);
    }
  }, [interruptSpeech, scheduleEndOfTurn, setUi, stopRecognition]);

  useEffect(() => {
    beginRecognitionRef.current = beginRecognition;
  }, [beginRecognition]);

  const startMeter = useCallback(async () => {
    const stream = await navigator.mediaDevices.getUserMedia({
      audio: {
        echoCancellation: true,
        noiseSuppression: true,
        autoGainControl: true,
      },
    });
    streamRef.current = stream;
    const AudioContextCtor =
      window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!AudioContextCtor) return;
    const ctx = new AudioContextCtor();
    audioCtxRef.current = ctx;
    if (ctx.state === 'suspended') await ctx.resume();
    const source = ctx.createMediaStreamSource(stream);
    const analyser = ctx.createAnalyser();
    analyser.fftSize = 512;
    source.connect(analyser);
    analyserRef.current = analyser;
    const data = new Uint8Array(analyser.frequencyBinCount);
    const tick = () => {
      if (!analyserRef.current) return;
      analyserRef.current.getByteTimeDomainData(data);
      let sum = 0;
      for (let i = 0; i < data.length; i += 1) {
        const centered = (data[i] - 128) / 128;
        sum += centered * centered;
      }
      const rms = Math.sqrt(sum / data.length);
      const pct = Math.min(100, Math.round(rms * 220));
      levelRef.current = pct;
      setLevel(pct);

      // Energy VAD for barge-in / speaking detection alongside SpeechRecognition.
      const speakingNow =
        uiStateRef.current === 'speaking' || uiStateRef.current === 'acknowledging';
      const sinceSpeak = Date.now() - speakStartedAtRef.current;
      if (
        sessionActiveRef.current &&
        speakingNow &&
        VOICE2_CONFIG.bargeInEnabled &&
        sinceSpeak >= VOICE2_CONFIG.bargeInMuteMs &&
        pct >= VOICE2_CONFIG.bargeInLevelThreshold
      ) {
        interruptSpeech();
        setUi('userSpeaking');
      }
      rafRef.current = requestAnimationFrame(tick);
    };
    rafRef.current = requestAnimationFrame(tick);
  }, [interruptSpeech, setUi]);

  const activate = useCallback(async () => {
    setNotice(null);
    setConfirmedTranscript('');
    // Chrome: text is the primary path until Chrome voice is re-enabled.
    if (textPrimary) {
      setShowType(true);
      setTtsMuted(true);
      sessionActiveRef.current = true;
      setSessionActive(true);
      setUi('listening');
      try {
        const started = await ensureSession();
        setSession(started);
        const greeting =
          "Type your order below — for example 'order 25 kg specialty spice at 25 AED'. I will ask for any missing price or UOM.";
        setAgentReply(greeting);
        setNotice({ tone: 'info', text: greeting });
      } catch (err: unknown) {
        setNotice({ tone: 'error', text: errorText(err) });
        setUi('error');
        sessionActiveRef.current = false;
        setSessionActive(false);
      }
      return;
    }
    setUi('activating');
    interruptSpeech();
    stopRecognition();
    try {
      // Unlock audible TTS on the user gesture (required for Alexa-like talk-back).
      unlockSpeechPlayback();
      await startMeter();
      sessionActiveRef.current = true;
      setSessionActive(true);
      await ensureSession();
      // Speak greeting first, then open the mic — Safari cannot keep recognition alive under TTS.
      const greeting = "Hi! I'm here. How can I help you today?";
      spokenRef.current = greeting;
      setAgentReply(greeting);
      setUi('acknowledging');
      speakStartedAtRef.current = Date.now();
      const handle = speakNaturally(greeting, ttsMuted);
      speakHandleRef.current = handle;
      await handle.done;
      speakHandleRef.current = null;
      if (sessionActiveRef.current) {
        setUi('listening');
        resumeListening();
      }
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
      setUi('error');
      sessionActiveRef.current = false;
      setSessionActive(false);
      stopRecognition();
      stopMeter();
    }
  }, [
    ensureSession,
    interruptSpeech,
    resumeListening,
    setUi,
    startMeter,
    stopMeter,
    stopRecognition,
    textPrimary,
    ttsMuted,
  ]);

  const endSession = useCallback(async () => {
    const hasDraft = (session?.draft?.lines?.length ?? 0) > 0;
    if (hasDraft && (uiStateRef.current !== 'confirmTranscript' && uiStateRef.current !== 'reviewPo' && uiStateRef.current !== 'done')) {
      await enterTranscriptConfirmation();
      return;
    }
    turnIdRef.current += 1;
    sessionActiveRef.current = false;
    setSessionActive(false);
    clearSilenceTimer();
    interruptSpeech();
    stopRecognition();
    stopMeter();
    setTranscript('');
    setConfirmedTranscript('');
    setUi('idle');
    if (!session) return;
    setBusy(true);
    try {
      const next = await cancelVoicePoSession(session.id);
      setSession(next);
      setNotice({ tone: 'success', text: 'Voice session ended.' });
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
    } finally {
      setBusy(false);
    }
  }, [clearSilenceTimer, enterTranscriptConfirmation, interruptSpeech, session, setUi, stopMeter, stopRecognition]);

  const confirmTranscript = () => {
    if (!confirmedTranscript.trim()) {
      setNotice({ tone: 'error', text: 'Confirm or edit the transcript before continuing.' });
      return;
    }
    setUi('reviewPo');
    setNotice({
      tone: 'success',
      text: 'Review header and item values. Submit creates the draft PO using your defaults.',
    });
    const handle = speakNaturally(
      'Here is your purchase order summary. Review the header and items, then press Submit.',
      ttsMuted,
    );
    speakHandleRef.current = handle;
    void handle.done.then(() => {
      speakHandleRef.current = null;
    });
  };

  const submitPo = async () => {
    if (!session) return;
    setBusy(true);
    setNotice(null);
    interruptSpeech();
    setUi('processing');
    try {
      const next = await confirmVoicePoDraft(session.id, {
        confirmationPhrase: 'confirm purchase order',
        idempotencyKey: `${session.id}-${Date.now()}`,
      });
      setSession(next);
      const po = next.createdPoNumber ? ` Purchase order ${next.createdPoNumber}.` : '';
      setNotice({
        tone: 'success',
        text: `Purchase order submitted for approval.${po} It posts to SAP after approval.`,
      });
      const reply =
        next.spokenReply ||
        `Done. Purchase order submitted for approval.${po} It will post to SAP once approved.`;
      setUi('speaking');
      const handle = speakNaturally(reply, ttsMuted);
      speakHandleRef.current = handle;
      await handle.done;
      speakHandleRef.current = null;
      sessionActiveRef.current = false;
      setSessionActive(false);
      stopRecognition();
      stopMeter();
      setUi('done');
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
      setUi('error');
      setUi('reviewPo');
    } finally {
      setBusy(false);
    }
  };

  const lines = session?.draft?.lines ?? [];
  const draft = session?.draft;
  const canSubmit = Boolean(session && openSession(session.status) && lines.length > 0);
  const ctx = session?.purchasingContext ?? defaults;
  const turns = session?.turns ?? [];
  const inPostTalk = uiState === 'confirmTranscript' || uiState === 'reviewPo' || uiState === 'done';

  const clarifications = session?.clarifications ?? [];
  const missingHints = clarifications
    .map((item) => item.message || item.code)
    .filter(Boolean)
    .slice(0, 4);

  return (
    <>
      <ScreenHeader
        title="Create PO by Voice"
        eyebrow="Purchasing"
        back
        intro={
          textPrimary
            ? "Type your order in Chrome. SILA asks for missing price, UOM and other mandatory fields. When finished: confirm transcript → review → Submit for approval."
            : "Tap Voice once. SILA talks back like Alexa. When finished: confirm transcript → review header & items → Submit with your PO defaults."
        }
      />
      <div className="sm-screen vpo" data-testid="voice-po-screen" data-text-primary={textPrimary ? 'true' : 'false'}>
        {!accessLoading && !can.requestPurchase && <Empty text="Voice purchase orders are not available for your role." />}
        {can.requestPurchase && (
          <>
            {!inPostTalk && !textPrimary && (
              <VoiceAgentOverlay
                level={level}
                state={uiState}
                transcript={transcript}
                reply={agentReply || session?.spokenReply || undefined}
                muted={ttsMuted}
                sessionActive={sessionActive}
                activating={uiState === 'activating'}
                onActivate={() => void activate()}
                onEndSession={() => void endSession()}
                onToggleMute={() => {
                  setTtsMuted((value) => !value);
                  interruptSpeech();
                }}
                onShowType={() => setShowType((value) => !value)}
              />
            )}

            {bootError && <ErrorNotice message={bootError} onRetry={() => window.location.reload()} />}
            <Notice notice={notice} />

            {(showType || textPrimary) && !inPostTalk && (
              <section className="vpo-card" aria-labelledby="vpo-transcript-title" data-testid="vpo-type-fallback">
                <h2 id="vpo-transcript-title">{textPrimary ? 'Type your purchase order' : 'Type fallback'}</h2>
                {textPrimary && (
                  <p className="sm-meta" data-testid="vpo-chrome-text-note">
                    Chrome voice is paused for now. Type lines, answer price/UOM prompts, then say you are done.
                  </p>
                )}
                {(agentReply || session?.spokenReply) && (
                  <p className="vpo-agent-reply" data-testid="vpo-agent-reply">
                    {agentReply || session?.spokenReply}
                  </p>
                )}
                {missingHints.length > 0 && (
                  <ul className="vpo-asks" data-testid="vpo-mandatory-asks">
                    {missingHints.map((hint) => (
                      <li key={hint}>{hint}</li>
                    ))}
                  </ul>
                )}
                <label className="sm-visually-hidden" htmlFor="vpo-transcript">
                  Order text
                </label>
                <textarea
                  id="vpo-transcript"
                  className="vpo-transcript"
                  data-testid="vpo-transcript"
                  placeholder={
                    clarifications.some((c) => (c.code || '').toUpperCase() === 'UNIT_PRICE')
                      ? "e.g. 25 AED  or  use one AED"
                      : clarifications.some((c) => (c.code || '').toUpperCase() === 'UOM')
                        ? "e.g. KG  or  EA"
                        : "e.g. order 25 kg specialty spice at 25 AED"
                  }
                  value={transcript}
                  rows={textPrimary ? 4 : 3}
                  onChange={(event) => setTranscript(event.target.value)}
                  onKeyDown={(event) => {
                    if (event.key === 'Enter' && !event.shiftKey) {
                      event.preventDefault();
                      if (!busy && transcript.trim()) void submitTurn(transcript, 'TEXT');
                    }
                  }}
                />
                <div className="vpo-actions">
                  <button
                    type="button"
                    className="sm-btn sm-btn--primary"
                    data-testid="vpo-send"
                    disabled={busy || !transcript.trim()}
                    onClick={() => void submitTurn(transcript, 'TEXT')}
                  >
                    {busy ? 'Sending…' : 'Send'}
                  </button>
                  {textPrimary && !sessionActive && (
                    <button type="button" className="sm-btn" data-testid="vpo-start-text" disabled={busy} onClick={() => void activate()}>
                      Start
                    </button>
                  )}
                  {(sessionActive || (session?.draft?.lines?.length ?? 0) > 0) && (
                    <button
                      type="button"
                      className="sm-btn"
                      data-testid="vpo-text-done"
                      disabled={busy || (session?.draft?.lines?.length ?? 0) === 0}
                      onClick={() => void enterTranscriptConfirmation()}
                    >
                      Done — review
                    </button>
                  )}
                </div>
              </section>
            )}

            {uiState === 'confirmTranscript' && (
              <section className="vpo-card" aria-labelledby="vpo-confirm-transcript-title" data-testid="vpo-confirm-transcript">
                <h2 id="vpo-confirm-transcript-title">Confirm transcript</h2>
                <p className="sm-meta">Edit if needed, then continue to review header and item values.</p>
                {turns.length > 0 && (
                  <ul className="vpo-turns" data-testid="vpo-turn-list">
                    {turns.map((turn) => (
                      <li key={turn.id} className={`vpo-turn vpo-turn--${(turn.role || 'USER').toLowerCase()}`}>
                        <strong>{(turn.role || '').toUpperCase() === 'AGENT' ? 'Agent' : 'You'}</strong>
                        <span>{turn.transcript || turn.spokenText || '—'}</span>
                      </li>
                    ))}
                  </ul>
                )}
                <textarea
                  className="vpo-transcript"
                  data-testid="vpo-confirmed-transcript"
                  value={confirmedTranscript}
                  onChange={(event) => setConfirmedTranscript(event.target.value)}
                />
                <div className="vpo-confirm-row">
                  <button
                    type="button"
                    className="sm-btn sm-btn--primary sm-btn--block"
                    data-testid="vpo-confirm-transcript-btn"
                    disabled={busy}
                    onClick={confirmTranscript}
                  >
                    Confirm transcript — review PO
                  </button>
                  <button type="button" className="sm-btn sm-btn--block" data-testid="vpo-cancel" disabled={busy} onClick={() => void endSession()}>
                    Cancel conversation
                  </button>
                </div>
              </section>
            )}

            {uiState === 'reviewPo' && (
              <section className="vpo-card" aria-labelledby="vpo-review-title" data-testid="vpo-review-panel">
                <h2 id="vpo-review-title">Review header & items</h2>
                <p className="sm-meta">Values include your PO defaults. Press Submit to create the draft purchase order.</p>
                <h3 className="vpo-subhead">Header</h3>
                <dl className="vpo-context" data-testid="vpo-review-header">
                  <div>
                    <dt>Company</dt>
                    <dd>{draft?.companyCode || ctx?.companyCode || '—'}</dd>
                  </div>
                  <div>
                    <dt>Plant</dt>
                    <dd>{draft?.plantCode || ctx?.plantCode || '—'}</dd>
                  </div>
                  <div>
                    <dt>SLoc</dt>
                    <dd>{draft?.storageLocation || ctx?.storageLocation || '—'}</dd>
                  </div>
                  <div>
                    <dt>POrg</dt>
                    <dd>{draft?.purchasingOrganization || ctx?.purchasingOrganization || '—'}</dd>
                  </div>
                  <div>
                    <dt>PGrp</dt>
                    <dd>{draft?.purchasingGroup || ctx?.purchasingGroup || '—'}</dd>
                  </div>
                  <div>
                    <dt>Currency</dt>
                    <dd>{draft?.currency || ctx?.defaultCurrency || '—'}</dd>
                  </div>
                  <div>
                    <dt>Acct / CC / GL</dt>
                    <dd>
                      {draft?.accountAssignmentCategory || ctx?.accountAssignmentCategory || '—'}
                      {draft?.costCenter || ctx?.costCenter ? ` · ${draft?.costCenter || ctx?.costCenter}` : ''}
                      {draft?.glAccount || ctx?.glAccount ? ` · ${draft?.glAccount || ctx?.glAccount}` : ''}
                    </dd>
                  </div>
                  <div>
                    <dt>Delivery</dt>
                    <dd>{draft?.deliveryDate ? new Date(draft.deliveryDate).toLocaleDateString() : '—'}</dd>
                  </div>
                </dl>
                <h3 className="vpo-subhead">Items</h3>
                {lines.length === 0 ? (
                  <p className="sm-meta">No draft lines yet.</p>
                ) : (
                  <ul className="vpo-lines" data-testid="vpo-review-items">
                    {lines.map((line, index) => (
                      <li
                        key={`${line.materialCode ?? 'line'}-${index}`}
                        className={`vpo-line${line.uncertain || line.resolved === false ? ' vpo-line--uncertain' : ''}`}
                      >
                        <div className="vpo-line__title">
                          <span>
                            {line.materialName || line.description || line.materialCode || `Line ${index + 1}`}
                            {line.materialCode && (line.materialName || line.description) ? ` (${line.materialCode})` : ''}
                          </span>
                          <span>
                            {line.quantity ?? '—'} {line.uom || line.unitOfMeasure || ''}
                          </span>
                        </div>
                        <div className="vpo-line__meta">
                          Supplier: {line.supplierName || line.supplierCode || '—'}
                          {line.unitPrice != null ? ` · ${line.unitPrice}${line.currency ? ` ${line.currency}` : ''}` : ''}
                          <br />
                          Account: {line.accountAssignmentCategory || draft?.accountAssignmentCategory || ctx?.accountAssignmentCategory || '—'}
                          {line.costCenter || draft?.costCenter || ctx?.costCenter
                            ? ` · CC ${line.costCenter || draft?.costCenter || ctx?.costCenter}`
                            : ''}
                          {line.glAccount || draft?.glAccount || ctx?.glAccount
                            ? ` · GL ${line.glAccount || draft?.glAccount || ctx?.glAccount}`
                            : ''}
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
                <div className="vpo-confirm-row">
                  <button
                    type="button"
                    className="sm-btn sm-btn--primary sm-btn--block"
                    data-testid="vpo-confirm"
                    disabled={busy || !canSubmit}
                    onClick={() => void submitPo()}
                  >
                    {busy ? 'Submitting…' : 'Submit — create draft PO'}
                  </button>
                  <button type="button" className="sm-btn sm-btn--block" disabled={busy} onClick={() => void endSession()}>
                    Cancel conversation
                  </button>
                </div>
              </section>
            )}

            {uiState === 'done' && (
              <section className="vpo-card" data-testid="vpo-done-panel">
                <h2>Submitted</h2>
                <p className="sm-meta">
                  {session?.createdPoNumber
                    ? `Purchase order ${session.createdPoNumber} was created with your defaults.`
                    : 'Purchase order submitted for approval. It posts to SAP after approval.'}
                </p>
                <button
                  type="button"
                  className="sm-btn sm-btn--primary"
                  onClick={() => {
                    setSession(null);
                    setConfirmedTranscript('');
                    setTranscript('');
                    spokenRef.current = null;
                    setUi('idle');
                  }}
                >
                  Start another
                </button>
              </section>
            )}

            {!inPostTalk && (
              <section className="vpo-card" aria-labelledby="vpo-draft-title">
                <h2 id="vpo-draft-title">Live draft</h2>
                {lines.length === 0 ? (
                  <p className="sm-meta">No draft lines yet. Speak after activating Voice. Say “that’s all” or End Session when finished.</p>
                ) : (
                  <ul className="vpo-lines">
                    {lines.map((line, index) => (
                      <li
                        key={`${line.materialCode ?? 'line'}-${index}`}
                        className={`vpo-line${line.uncertain || line.resolved === false ? ' vpo-line--uncertain' : ''}`}
                      >
                        <div className="vpo-line__title">
                          <span>
                            {line.materialName || line.description || line.materialCode || `Line ${index + 1}`}
                            {line.materialCode && (line.materialName || line.description) ? ` (${line.materialCode})` : ''}
                          </span>
                          <span>
                            {line.quantity ?? '—'} {line.uom || line.unitOfMeasure || ''}
                          </span>
                        </div>
                      </li>
                    ))}
                  </ul>
                )}
                {lines.length > 0 && (
                  <div className="vpo-confirm-row">
                    <button
                      type="button"
                      className="sm-btn sm-btn--primary sm-btn--block"
                      data-testid="vpo-done-talking"
                      disabled={busy}
                      onClick={() => void enterTranscriptConfirmation()}
                    >
                      Done talking — confirm transcript
                    </button>
                    {sessionActive && (
                      <button
                        type="button"
                        className="sm-btn sm-btn--block"
                        data-testid="vpo-cancel"
                        disabled={busy}
                        onClick={() => void endSession()}
                      >
                        End Session
                      </button>
                    )}
                  </div>
                )}
              </section>
            )}
          </>
        )}
      </div>
    </>
  );
};

export default VoicePoScreen;
