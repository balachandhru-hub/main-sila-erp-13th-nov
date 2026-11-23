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
  const [showType, setShowType] = useState(false);
  const [ttsMuted, setTtsMuted] = useState(false);
  const [busy, setBusy] = useState(false);
  const [bootError, setBootError] = useState<string | null>(null);
  const [notice, setNotice] = useState<NoticeMessage | null>(null);

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
        setNotice({ tone: 'warn', text: errorText(err) });
      });
    return () => {
      active = false;
    };
  }, [can.requestPurchase, mine?.id, mine?.locationType, mine?.propertyId]);

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
      uiStateRef.current === 'done'
    ) {
      return;
    }
    setUi('listening');
    speechStartedAtRef.current = null;
    finalChunkRef.current = '';
    setTranscript('');
    transcriptRef.current = '';
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

  const submitTurn = useCallback(
    async (text: string, channel: 'VOICE' | 'TEXT' = 'VOICE') => {
      const cleaned = text.trim();
      if (!cleaned || submittingRef.current || !sessionActiveRef.current) return;
      submittingRef.current = true;
      clearSilenceTimer();
      const turnId = ++turnIdRef.current;
      setBusy(true);
      setUi('processing');
      setNotice(null);
      interruptSpeech();
      try {
        const lineCount = session?.draft?.lines?.length ?? 0;
        if (isConversationDonePhrase(cleaned) && lineCount > 0) {
          await enterTranscriptConfirmation(cleaned);
          return;
        }

        const active = await ensureSession();
        if (turnId !== turnIdRef.current) return;
        const next = await sendVoicePoTurn(active.id, {
          transcript: cleaned,
          channel,
        });
        if (turnId !== turnIdRef.current) return;
        setSession(next);
        sessionDataRef.current = next;
        setTranscript('');
        transcriptRef.current = '';
        finalChunkRef.current = '';

        const reply = (next.spokenReply || '').trim();
        const hasLines = (next.draft?.lines?.length ?? 0) > 0;
        if (isConversationDonePhrase(cleaned) && hasLines) {
          if (reply && reply !== spokenRef.current) {
            spokenRef.current = reply;
            setUi('speaking');
            const handle = speakNaturally(reply, ttsMuted);
            speakHandleRef.current = handle;
            await handle.done;
            speakHandleRef.current = null;
          }
          await enterTranscriptConfirmation(cleaned);
          return;
        }

        const awaiting = next.status === 'REVIEW' || hasLines;
        if (awaiting && /confirm/i.test(reply)) {
          setUi('awaitingConfirmation');
        }

        if (reply && reply !== spokenRef.current) {
          spokenRef.current = reply;
          setUi('speaking');
          const handle = speakNaturally(reply, ttsMuted);
          speakHandleRef.current = handle;
          await handle.done;
          if (turnId !== turnIdRef.current || !sessionActiveRef.current) return;
          speakHandleRef.current = null;
          if (awaiting && /confirm/i.test(reply)) {
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
      ttsMuted,
    ],
  );

  const scheduleEndOfTurn = useCallback(() => {
    clearSilenceTimer();
    if (!VOICE2_CONFIG.autoEndOfTurn || !sessionActiveRef.current) return;
    const text = (finalChunkRef.current || transcriptRef.current).trim();
    if (!text) return;
    const incomplete = looksIncompleteUtterance(text);
    const wait = incomplete ? VOICE2_CONFIG.incompleteSilenceMs : VOICE2_CONFIG.endOfSpeechSilenceMs;
    silenceTimerRef.current = window.setTimeout(() => {
      const uttered = (finalChunkRef.current || transcriptRef.current).trim();
      const started = speechStartedAtRef.current;
      const duration = started ? Date.now() - started : 0;
      if (!uttered || duration < VOICE2_CONFIG.minUtteranceMs) {
        setUi('listening');
        return;
      }
      if (looksIncompleteUtterance(uttered)) {
        // Still incomplete after extended wait — ask naturally via agent if short, else submit.
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
    const recognition = new SpeechRecognitionCtor();
    recognition.continuous = true;
    recognition.interimResults = true;
    recognition.lang = 'en-US';
    recognition.onresult = (event) => {
      if (!sessionActiveRef.current) return;
      // Barge-in while speaking
      if (uiStateRef.current === 'speaking' && VOICE2_CONFIG.bargeInEnabled) {
        interruptSpeech();
        setUi('userSpeaking');
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
        return;
      }
      if (event.error === 'aborted' || event.error === 'no-speech') return;
    };
    recognition.onend = () => {
      if (!sessionActiveRef.current) return;
      // Keep recognition alive for continuous conversation.
      if (uiStateRef.current === 'processing' || uiStateRef.current === 'transcribing' || uiStateRef.current === 'speaking') {
        return;
      }
      try {
        recognition.start();
      } catch {
        window.setTimeout(() => {
          if (!sessionActiveRef.current) return;
          try {
            recognition.start();
          } catch {
            /* ignore */
          }
        }, 250);
      }
    };
    recognitionRef.current = recognition;
    recognition.start();
  }, [interruptSpeech, scheduleEndOfTurn, setUi, stopRecognition]);

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
      if (
        sessionActiveRef.current &&
        uiStateRef.current === 'speaking' &&
        VOICE2_CONFIG.bargeInEnabled &&
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
    setUi('activating');
    interruptSpeech();
    try {
      // Unlock audible TTS on the user gesture (required for Alexa-like talk-back).
      unlockSpeechPlayback();
      await startMeter();
      sessionActiveRef.current = true;
      setSessionActive(true);
      await ensureSession();
      beginRecognition();
      setUi('listening');
      const greeting = "Hi, I'm SILA, your purchasing agent. What would you like to order?";
      spokenRef.current = greeting;
      setUi('speaking');
      const handle = speakNaturally(greeting, ttsMuted);
      speakHandleRef.current = handle;
      await handle.done;
      speakHandleRef.current = null;
      if (sessionActiveRef.current) resumeListening();
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
      setUi('error');
      sessionActiveRef.current = false;
      setSessionActive(false);
      stopRecognition();
      stopMeter();
    }
  }, [
    beginRecognition,
    ensureSession,
    interruptSpeech,
    resumeListening,
    setUi,
    startMeter,
    stopMeter,
    stopRecognition,
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
      setNotice({ tone: 'success', text: `Draft purchase order created with your defaults.${po}` });
      const reply = next.spokenReply || `Done. Draft purchase order created.${po}`;
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

  return (
    <>
      <ScreenHeader
        title="Create PO by Voice"
        eyebrow="Purchasing"
        back
        intro="Tap Voice once. SILA talks back like Alexa. When finished: confirm transcript → review header & items → Submit with your PO defaults."
      />
      <div className="sm-screen vpo" data-testid="voice-po-screen">
        {!accessLoading && !can.requestPurchase && <Empty text="Voice purchase orders are not available for your role." />}
        {can.requestPurchase && (
          <>
            {!inPostTalk && (
              <VoiceAgentOverlay
                level={level}
                state={uiState}
                transcript={transcript}
                reply={session?.spokenReply || undefined}
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

            {showType && !inPostTalk && (
              <section className="vpo-card" aria-labelledby="vpo-transcript-title" data-testid="vpo-type-fallback">
                <h2 id="vpo-transcript-title">Type fallback</h2>
                <label className="sm-visually-hidden" htmlFor="vpo-transcript">
                  Editable transcript
                </label>
                <textarea
                  id="vpo-transcript"
                  className="vpo-transcript"
                  data-testid="vpo-transcript"
                  placeholder="Type if voice is unavailable…"
                  value={transcript}
                  onChange={(event) => setTranscript(event.target.value)}
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
                    : 'Draft purchase order created with your defaults.'}
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
