import React, { useEffect, useState } from 'react';
import {
  getVoiceEngineHealth,
  type SilaVoiceEngineHealth,
} from '../../../../../remote-buyer/src/api/silaMe/silaVoiceEngineApi';
import type { Voice2UiState } from './conversationConfig';
import './VoiceAgentOverlay.css';

interface VoiceAgentOverlayProps {
  /** Real mic / TTS amplitude 0–100. Never simulated. */
  level: number;
  state: Voice2UiState;
  transcript?: string;
  reply?: string;
  muted?: boolean;
  sessionActive?: boolean;
  onActivate?: () => void;
  onEndSession?: () => void;
  onToggleMute?: () => void;
  onShowType?: () => void;
  activating?: boolean;
}

const statusFor = (state: Voice2UiState, health: SilaVoiceEngineHealth | null, muted: boolean): string => {
  switch (state) {
    case 'activating':
      return 'Activating…';
    case 'listening':
      return "I'm listening…";
    case 'userSpeaking':
      return "I'm listening…";
    case 'transcribing':
      return 'Understanding…';
    case 'processing':
      return 'Understanding…';
    case 'speaking':
      return muted ? 'Speaking (muted)' : 'Speaking…';
    case 'awaitingConfirmation':
      return 'Please review and confirm';
    case 'error':
      return 'Something went wrong — tap Voice to retry';
    default:
      return health ? 'Tap Voice to start' : 'Voice standby — tap to activate';
  }
};

/**
 * SILA Voice 2.0 orb — frozen visual identity for the existing Voice Agent.
 * Visual state only; capture / ASR / LLM / TTS live in the parent screen.
 */
const VoiceAgentOverlay: React.FC<VoiceAgentOverlayProps> = ({
  level,
  state,
  transcript,
  reply,
  muted = false,
  sessionActive = false,
  onActivate,
  onEndSession,
  onToggleMute,
  onShowType,
  activating,
}) => {
  const [health, setHealth] = useState<SilaVoiceEngineHealth | null>(null);

  useEffect(() => {
    let active = true;
    getVoiceEngineHealth().then((data) => {
      if (active) setHealth(data);
    });
    return () => {
      active = false;
    };
  }, []);

  const bars = Array.from({ length: 12 }, (_, index) => {
    const wave = Math.max(12, Math.min(100, level * (0.5 + ((index % 5) * 0.14))));
    return wave;
  });

  const orbState =
    state === 'idle' || state === 'activating'
      ? state
      : state === 'userSpeaking'
        ? 'userSpeaking'
        : state;

  return (
    <section className="sva-overlay" data-testid="sila-voice-agent-overlay" aria-label="SILA Voice 2.0">
      <div className="sva-orb-wrap">
        <span className="sva-ring" aria-hidden="true" />
        <span className="sva-ring sva-ring--2" aria-hidden="true" />
        <span className="sva-ring sva-ring--3" aria-hidden="true" />
        <button
          type="button"
          className={`sva-orb sva-orb--${orbState}`}
          data-testid="sva-orb-toggle"
          aria-label={sessionActive ? 'Voice session active' : 'Activate SILA Voice'}
          disabled={activating || state === 'processing' || state === 'transcribing'}
          onClick={() => {
            if (!sessionActive) onActivate?.();
          }}
        >
          <div className="sva-wave" aria-hidden="true" data-testid="sva-waveform">
            {bars.map((height, index) => (
              <span key={index} className="sva-wave__bar" style={{ height: `${height}%` }} />
            ))}
          </div>
        </button>
      </div>

      <p className="sva-status" data-testid="sva-status">
        {statusFor(state, health, muted)}
      </p>

      {transcript ? (
        <p className="sva-transcript" data-testid="sva-live-transcript">
          {transcript}
        </p>
      ) : (
        !sessionActive && (
          <p className="sva-hint">One tap starts continuous conversation. Speak naturally — no Start or Send each turn.</p>
        )
      )}

      {reply && (
        <p className="sva-reply" data-testid="sva-ai-reply">
          {reply}
        </p>
      )}

      <div className="sva-controls">
        {!sessionActive ? (
          <button
            type="button"
            className="sva-btn sva-btn--voice"
            data-testid="sva-activate"
            disabled={activating}
            onClick={() => onActivate?.()}
          >
            {activating ? 'Starting…' : 'Voice'}
          </button>
        ) : (
          <button
            type="button"
            className="sva-btn sva-btn--end"
            data-testid="sva-end-session"
            onClick={() => onEndSession?.()}
          >
            End Session
          </button>
        )}
        <button type="button" className="sva-btn" data-testid="sva-mute" onClick={() => onToggleMute?.()}>
          {muted ? 'Unmute' : 'Mute'}
        </button>
        <button type="button" className="sva-btn" data-testid="sva-type" onClick={() => onShowType?.()}>
          Type
        </button>
      </div>
    </section>
  );
};

export default VoiceAgentOverlay;
