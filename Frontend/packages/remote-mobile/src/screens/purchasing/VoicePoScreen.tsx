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
import { Empty, ErrorNotice, Loading, Notice, type NoticeMessage } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { errorText } from '../../useLoad';
import VoiceAgentOverlay from './voiceAgent/VoiceAgentOverlay';
import './VoicePoScreen.css';

type RecState = 'idle' | 'recording' | 'paused';

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

/** Premium Voice PO panel: mic + transcript + draft review + explicit confirm. */
const VoicePoScreen: React.FC = () => {
  const { can, loading: accessLoading } = useAccess();
  const { location: mine } = useMyLocation();

  const [defaults, setDefaults] = useState<SilaPurchasingDefaultsResolved | null>(null);
  const [session, setSession] = useState<SilaVoicePoSession | null>(null);
  const [transcript, setTranscript] = useState('');
  const [recState, setRecState] = useState<RecState>('idle');
  const [level, setLevel] = useState(0);
  const [noiseStatus, setNoiseStatus] = useState('Mic idle');
  const [micDenied, setMicDenied] = useState(false);
  const [speechAvailable] = useState(Boolean(SpeechRecognitionCtor));
  const [ttsMuted, setTtsMuted] = useState(false);
  const [busy, setBusy] = useState(false);
  const [bootError, setBootError] = useState<string | null>(null);
  const [notice, setNotice] = useState<NoticeMessage | null>(null);

  const recognitionRef = useRef<SpeechRecognitionLike | null>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const audioCtxRef = useRef<AudioContext | null>(null);
  const analyserRef = useRef<AnalyserNode | null>(null);
  const rafRef = useRef<number | null>(null);
  const pauseRef = useRef(false);
  const spokenRef = useRef<string | null>(null);

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
    setRecState('idle');
  }, []);

  const speak = useCallback(
    (text: string | null | undefined) => {
      if (!text || ttsMuted || typeof window === 'undefined' || !window.speechSynthesis) return;
      window.speechSynthesis.cancel();
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.rate = 1;
      utterance.pitch = 1;
      window.speechSynthesis.speak(utterance);
    },
    [ttsMuted],
  );

  const interruptSpeech = useCallback(() => {
    if (typeof window !== 'undefined' && window.speechSynthesis) {
      window.speechSynthesis.cancel();
    }
  }, []);

  useEffect(() => {
    return () => {
      pauseRef.current = true;
      stopRecognition();
      stopMeter();
      interruptSpeech();
    };
  }, [interruptSpeech, stopMeter, stopRecognition]);

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
        if (data.message && !(data.profileFound || data.resolved)) {
          setNotice({ tone: 'warn', text: data.message });
        }
      })
      .catch((err: unknown) => {
        if (!active) return;
        // Keep the screen usable; show a notice instead of a hard boot block.
        setDefaults({ profileFound: false, resolved: false, validationErrors: [] });
        setNotice({ tone: 'warn', text: errorText(err) });
      });
    return () => {
      active = false;
    };
  }, [can.requestPurchase, mine?.id, mine?.locationType, mine?.propertyId]);

  useEffect(() => {
    const reply = session?.spokenReply?.trim();
    if (!reply || reply === spokenRef.current) return;
    spokenRef.current = reply;
    speak(reply);
  }, [session?.spokenReply, speak]);

  const startMeter = async () => {
    setMicDenied(false);
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
        },
      });
      streamRef.current = stream;
      const AudioContextCtor = window.AudioContext || (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      if (!AudioContextCtor) {
        setNoiseStatus('Level meter unavailable');
        return;
      }
      const ctx = new AudioContextCtor();
      audioCtxRef.current = ctx;
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
        setLevel(pct);
        if (pct < 8) setNoiseStatus('Quiet — speak closer');
        else if (pct < 35) setNoiseStatus('Noise suppressed · clear signal');
        else if (pct < 70) setNoiseStatus('Good level');
        else setNoiseStatus('Loud — move back slightly');
        rafRef.current = requestAnimationFrame(tick);
      };
      rafRef.current = requestAnimationFrame(tick);
    } catch {
      setMicDenied(true);
      setNoiseStatus('Microphone denied');
      throw new Error('Microphone access was denied.');
    }
  };

  const beginRecognition = () => {
    if (!SpeechRecognitionCtor) {
      setNotice({ tone: 'warning', text: 'Speech recognition is not available in this browser. Type your request instead.' });
      return;
    }
    stopRecognition();
    const recognition = new SpeechRecognitionCtor();
    recognition.continuous = true;
    recognition.interimResults = true;
    recognition.lang = 'en-US';
    recognition.onresult = (event) => {
      let finalText = '';
      let interim = '';
      for (let i = event.resultIndex; i < event.results.length; i += 1) {
        const result = event.results[i];
        const piece = result[0]?.transcript ?? '';
        if (result.isFinal) finalText += piece;
        else interim += piece;
      }
      setTranscript((current) => {
        const base = finalText ? `${current}${current && !current.endsWith(' ') ? ' ' : ''}${finalText}`.trimStart() : current;
        return interim ? `${base}${base && !base.endsWith(' ') ? ' ' : ''}${interim}`.trimStart() : base;
      });
    };
    recognition.onerror = (event) => {
      if (event.error === 'not-allowed') {
        setMicDenied(true);
        setNoiseStatus('Microphone denied');
      }
      setRecState('idle');
    };
    recognition.onend = () => {
      if (!pauseRef.current && recognitionRef.current === recognition) {
        setRecState('idle');
      }
    };
    recognitionRef.current = recognition;
    pauseRef.current = false;
    recognition.start();
    setRecState('recording');
  };

  const startRecording = async () => {
    setNotice(null);
    interruptSpeech();
    try {
      await startMeter();
      beginRecognition();
      setNoiseStatus(speechAvailable ? 'Listening…' : 'Mic open · type to send');
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
      stopMeter();
    }
  };

  const pauseRecording = () => {
    pauseRef.current = true;
    stopRecognition();
    setRecState('paused');
    setNoiseStatus('Paused');
  };

  const stopRecording = () => {
    pauseRef.current = true;
    stopRecognition();
    stopMeter();
    setNoiseStatus('Mic idle');
  };

  const ensureSession = async (): Promise<SilaVoicePoSession> => {
    if (session && openSession(session.status)) return session;
    const started = await startVoicePoSession({
      locationId: mine?.id ?? defaults?.locationId ?? null,
      propertyId: mine?.propertyId ?? defaults?.propertyId ?? null,
      outletId: mine?.locationType === 'OUTLET' ? mine.id : defaults?.outletId ?? null,
      purchasingProfileId: defaults?.profileId ?? null,
      language: 'en',
    });
    setSession(started);
    return started;
  };

  const sendTurn = async (goWithDefault = false) => {
    const text = transcript.trim();
    if (!text && !goWithDefault) {
      setNotice({ tone: 'error', text: 'Say or type what you want to order first.' });
      return;
    }
    setBusy(true);
    setNotice(null);
    interruptSpeech();
    try {
      const active = await ensureSession();
      const next = await sendVoicePoTurn(active.id, {
        transcript: goWithDefault ? (text || 'use my default') : text,
        channel: recState === 'recording' ? 'VOICE' : 'TEXT',
      });
      setSession(next);
      if (next.status === 'REVIEW' || (next.draft?.lines?.length ?? 0) > 0) {
        setNotice({ tone: 'success', text: 'Draft updated — review lines, then Confirm to create.' });
      }
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
    } finally {
      setBusy(false);
    }
  };

  const confirm = async () => {
    if (!session) return;
    setBusy(true);
    setNotice(null);
    interruptSpeech();
    try {
      const next = await confirmVoicePoDraft(session.id, {
        confirmationPhrase: 'confirm purchase order',
        idempotencyKey: `${session.id}-${Date.now()}`,
      });
      setSession(next);
      stopRecording();
      const po = next.createdPoNumber ? ` PO ${next.createdPoNumber}.` : '';
      setNotice({ tone: 'success', text: `Draft purchase order created.${po}` });
      speak(next.spokenReply || `Purchase order draft created${po}`);
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
    } finally {
      setBusy(false);
    }
  };

  const cancelConversation = async () => {
    if (!session) {
      setTranscript('');
      stopRecording();
      return;
    }
    setBusy(true);
    setNotice(null);
    interruptSpeech();
    try {
      const next = await cancelVoicePoSession(session.id);
      setSession(next);
      setTranscript('');
      stopRecording();
      setNotice({ tone: 'success', text: 'Conversation cancelled.' });
    } catch (err: unknown) {
      setNotice({ tone: 'error', text: errorText(err) });
    } finally {
      setBusy(false);
    }
  };

  const ctx = session?.purchasingContext ?? defaults;
  const lines = session?.draft?.lines ?? [];
  const canConfirm = Boolean(session && openSession(session.status) && lines.length > 0);
  const overlayState =
    busy ? 'thinking' : recState === 'recording' ? 'listening' : session?.spokenReply && !ttsMuted ? 'speaking' : 'idle';

  return (
    <>
      <ScreenHeader
        title="Create PO by Voice"
        eyebrow="Purchasing"
        back
        intro="Speak or type what you need. Review the draft, then Confirm to create — nothing posts without you."
        aside={
          <button
            type="button"
            className="sm-btn sm-btn--small"
            aria-pressed={ttsMuted}
            data-testid="vpo-tts-mute"
            onClick={() => {
              setTtsMuted((value) => !value);
              interruptSpeech();
            }}
          >
            {ttsMuted ? 'Unmute' : 'Mute'}
          </button>
        }
      />
      <div className="sm-screen vpo" data-testid="voice-po-screen">
        {!accessLoading && !can.requestPurchase && <Empty text="Voice purchase orders are not available for your role." />}
        {can.requestPurchase && (
          <>
            <section className="vpo-hero">
              <p className="vpo-hero__kicker">SILA purchasing</p>
              <h2 className="vpo-hero__title">Create PO by Voice</h2>
              <p className="vpo-hero__sub">Hands-free draft with your plant defaults. Confirm is always explicit.</p>
            </section>

            <VoiceAgentOverlay
              level={level}
              state={overlayState}
              transcript={transcript}
              reply={session?.spokenReply || undefined}
              intent={session?.status || undefined}
              muted={ttsMuted}
            />

            {bootError && <ErrorNotice message={bootError} onRetry={() => window.location.reload()} />}
            <Notice notice={notice} />

            <section className="vpo-card" aria-labelledby="vpo-context-title">
              <h2 id="vpo-context-title">Purchasing context</h2>
              {!defaults && !bootError ? (
                <Loading />
              ) : (
                <dl className="vpo-context">
                  <div>
                    <dt>Company</dt>
                    <dd>{ctx?.companyCode || '—'}</dd>
                  </div>
                  <div>
                    <dt>Plant</dt>
                    <dd>{ctx?.plantCode || '—'}</dd>
                  </div>
                  <div>
                    <dt>SLoc</dt>
                    <dd>{ctx?.storageLocation || '—'}</dd>
                  </div>
                  <div>
                    <dt>POrg</dt>
                    <dd>{ctx?.purchasingOrganization || '—'}</dd>
                  </div>
                  <div>
                    <dt>PGrp</dt>
                    <dd>{ctx?.purchasingGroup || '—'}</dd>
                  </div>
                  <div>
                    <dt>Acct / CC</dt>
                    <dd>
                      {ctx?.accountAssignmentCategory || '—'}
                      {ctx?.costCenter ? ` · ${ctx.costCenter}` : ''}
                    </dd>
                  </div>
                </dl>
              )}
              {defaults && !(defaults.profileFound || defaults.resolved) && (
                <p className="sm-meta" style={{ marginTop: '0.55rem' }}>
                  {defaults.message || (defaults.validationErrors?.length ? defaults.validationErrors.join(' ') : null) || 'No purchasing defaults resolved. Ask an admin to set PO Defaults for your user.'}
                </p>
              )}
            </section>

            <section className="vpo-card" aria-labelledby="vpo-rec-title" data-testid="vpo-recording-controls">
              <h2 id="vpo-rec-title">Recording controls</h2>
              <div className="vpo-meter-wrap">
                <div className="vpo-status">
                  {recState === 'recording' && <span className="vpo-pulse" aria-hidden="true" />}
                  <span className={`vpo-pill${micDenied ? ' vpo-pill--warn' : ''}`} data-testid="vpo-noise-status">
                    {noiseStatus}
                  </span>
                  {!speechAvailable && <span className="vpo-pill vpo-pill--muted">Text fallback</span>}
                  {session?.sessionNumber && <span className="vpo-pill vpo-pill--muted">{session.sessionNumber}</span>}
                </div>
                <div className="vpo-meter" aria-hidden="true">
                  <div
                    className={`vpo-meter__fill${level > 70 ? ' vpo-meter__fill--hot' : ''}`}
                    style={{ width: `${level}%` }}
                    data-testid="vpo-level-meter"
                  />
                </div>
              </div>
              {micDenied && (
                <p className="sm-meta" role="alert">
                  Microphone denied. Allow mic access in the browser, or type your request below.
                </p>
              )}
              <div className="vpo-rec-controls">
                <button
                  type="button"
                  className="sm-btn sm-btn--primary"
                  data-testid="vpo-start-recording"
                  disabled={busy || recState === 'recording'}
                  aria-pressed={recState === 'recording'}
                  onClick={() => void startRecording()}
                >
                  Start
                </button>
                <button
                  type="button"
                  className="sm-btn"
                  data-testid="vpo-pause-recording"
                  disabled={busy || recState !== 'recording'}
                  aria-pressed={recState === 'paused'}
                  onClick={pauseRecording}
                >
                  Pause
                </button>
                <button
                  type="button"
                  className="sm-btn"
                  data-testid="vpo-stop-recording"
                  disabled={busy || recState === 'idle'}
                  onClick={stopRecording}
                >
                  Stop
                </button>
              </div>
            </section>

            <section className="vpo-card" aria-labelledby="vpo-transcript-title">
              <h2 id="vpo-transcript-title">Transcript</h2>
              <label className="sm-visually-hidden" htmlFor="vpo-transcript">Editable transcript</label>
              <textarea
                id="vpo-transcript"
                className="vpo-transcript"
                data-testid="vpo-transcript"
                placeholder="e.g. Order 20 kg flour for the bakery from preferred supplier"
                value={transcript}
                onChange={(event) => setTranscript(event.target.value)}
              />
              <div className="vpo-actions">
                <button type="button" className="sm-btn sm-btn--primary" data-testid="vpo-send" disabled={busy} onClick={() => void sendTurn(false)}>
                  {busy ? 'Sending…' : 'Send'}
                </button>
                <button type="button" className="sm-btn" data-testid="vpo-go-default" disabled={busy} onClick={() => void sendTurn(true)}>
                  Uncertain — go with default
                </button>
              </div>
              {session?.spokenReply && (
                <p className="vpo-reply" data-testid="vpo-spoken-reply">
                  {session.spokenReply}
                </p>
              )}
              {session?.clarifications && session.clarifications.length > 0 && (
                <ul className="sm-list" style={{ marginTop: '0.55rem' }}>
                  {session.clarifications.map((item) => (
                    <li key={`${item.code}-${item.field ?? ''}-${item.message}`} className="sm-meta">
                      {item.message}
                    </li>
                  ))}
                </ul>
              )}
              {session?.validationErrors && session.validationErrors.length > 0 && (
                <ul className="sm-list" style={{ marginTop: '0.55rem' }}>
                  {session.validationErrors.map((item) => (
                    <li key={item} className="sm-meta" style={{ color: '#9a3412' }}>
                      {item}
                    </li>
                  ))}
                </ul>
              )}
            </section>

            <section className="vpo-card" aria-labelledby="vpo-draft-title">
              <h2 id="vpo-draft-title">Draft review</h2>
              {lines.length === 0 ? (
                <p className="sm-meta">No draft lines yet. Send a voice or text request to build the PO draft.</p>
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
                      <div className="vpo-line__meta">
                        Supplier: {line.supplierName || line.supplierCode || '—'}
                        {line.unitPrice != null ? ` · ${line.unitPrice}${line.currency ? ` ${line.currency}` : ''}` : ''}
                        <br />
                        Account: {line.accountAssignmentCategory || ctx?.accountAssignmentCategory || '—'}
                        {line.costCenter || ctx?.costCenter ? ` · CC ${line.costCenter || ctx?.costCenter}` : ''}
                        {line.glAccount || ctx?.glAccount ? ` · GL ${line.glAccount || ctx?.glAccount}` : ''}
                        {line.uncertain || line.resolved === false ? ' · Uncertain' : ''}
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
                  disabled={busy || !canConfirm}
                  onClick={() => void confirm()}
                >
                  Confirm — create draft PO
                </button>
                <button
                  type="button"
                  className="sm-btn sm-btn--block"
                  data-testid="vpo-cancel"
                  disabled={busy}
                  onClick={() => void cancelConversation()}
                >
                  Cancel conversation
                </button>
              </div>
            </section>
          </>
        )}
      </div>
    </>
  );
};

export default VoicePoScreen;
