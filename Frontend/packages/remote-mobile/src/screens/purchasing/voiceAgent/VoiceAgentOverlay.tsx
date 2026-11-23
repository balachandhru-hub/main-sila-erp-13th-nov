import React, { useEffect, useState } from 'react';
import {
  getVoiceEngineHealth,
  type SilaVoiceEngineHealth,
} from '../../../../../remote-buyer/src/api/silaMe/silaVoiceEngineApi';
import './VoiceAgentOverlay.css';

interface VoiceAgentOverlayProps {
  /** Real mic amplitude 0–100 from the parent capture pipeline. */
  level: number;
  state: 'idle' | 'listening' | 'thinking' | 'speaking';
  transcript?: string;
  reply?: string;
  intent?: string | null;
  muted?: boolean;
}

/**
 * Isolated premium Voice Agent overlay — does not redesign Voice PO layout.
 * Waveform uses real amplitude from the parent; never simulates mic activity.
 */
const VoiceAgentOverlay: React.FC<VoiceAgentOverlayProps> = ({
  level,
  state,
  transcript,
  reply,
  intent,
  muted,
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

  const bars = Array.from({ length: 16 }, (_, index) => {
    const wave = Math.max(6, Math.min(100, level * (0.55 + ((index % 5) * 0.12))));
    return wave;
  });

  return (
    <section className="sva-overlay" data-testid="sila-voice-agent-overlay" aria-label="SILA Voice Agent">
      <div className={`sva-orb sva-orb--${state}`} aria-hidden="true">
        <span className="sva-orb__core" />
      </div>
      <div className="sva-wave" aria-hidden="true" data-testid="sva-waveform">
        {bars.map((height, index) => (
          <span key={index} className="sva-wave__bar" style={{ height: `${height}%` }} />
        ))}
      </div>
      <p className="sva-status">
        {state === 'listening' && 'Listening'}
        {state === 'thinking' && 'Understanding'}
        {state === 'speaking' && (muted ? 'Speaking (muted)' : 'Speaking')}
        {state === 'idle' && (health ? 'Voice engine ready' : 'Voice engine standby')}
      </p>
      {intent && <p className="sva-intent">Intent: {intent}</p>}
      {transcript && (
        <p className="sva-transcript" data-testid="sva-live-transcript">
          {transcript}
        </p>
      )}
      {reply && (
        <p className="sva-reply" data-testid="sva-ai-reply">
          {reply}
        </p>
      )}
    </section>
  );
};

export default VoiceAgentOverlay;
