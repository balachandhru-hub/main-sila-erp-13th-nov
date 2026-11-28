import React from "react";
import type { AiOperatingMode, AiOperatingModeStatus } from "../../../../api/silaMe/menuEngineeringAiApi";

interface Props {
  mode: AiOperatingMode;
  status: AiOperatingModeStatus | null;
  onChangeMode: (next: AiOperatingMode) => void;
}

const MODE_LABELS: { key: AiOperatingMode; label: string; hint: string }[] = [
  { key: "MANUAL", label: "Manual", hint: "Deterministic business calculations only" },
  { key: "AI_ASSISTED", label: "AI Assisted", hint: "Contextual insights, predictions & smart suggestions" },
  { key: "AGENTIC", label: "Agentic", hint: "Permission-controlled multi-agent orchestration" },
];

export const AiOperatingModeSwitch: React.FC<Props> = ({ mode, status, onChangeMode }) => {
  return (
    <div className="sme-ai-mode-bar" role="group" aria-label="AI Operating Mode">
      <div className="sme-ai-mode-pills">
        {MODE_LABELS.map((item) => {
          const disabled =
            (item.key === "AI_ASSISTED" && status != null && !status.canUseAiAssisted) ||
            (item.key === "AGENTIC" && (status == null || !status.canUseAgentic));
          return (
            <button
              key={item.key}
              type="button"
              disabled={disabled}
              title={disabled ? "Insufficient permission for this mode" : item.hint}
              className={`sme-ai-mode-pill ${mode === item.key ? "sme-ai-mode-pill--active" : ""}`}
              onClick={() => onChangeMode(item.key)}
            >
              {item.label}
            </button>
          );
        })}
      </div>

      {status && (
        <div className="sme-ai-status-meta">
          <span className={`sme-ai-badge ${status.aiProviderAvailable ? "sme-ai-badge--ok" : "sme-ai-badge--warn"}`}>
            {status.aiProviderAvailable ? `AI: ${status.providerMode}` : "AI Offline (Deterministic Mode)"}
          </span>
          <span className="sme-ai-quota" title="Daily tenant AI requests and token budget">
            Usage {status.dailyUsageCount}/{status.dailyUsageLimit} · {status.dailyTokensUsed.toLocaleString()} tok
          </span>
        </div>
      )}
    </div>
  );
};
