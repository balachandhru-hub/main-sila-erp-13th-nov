import React, { useState } from "react";
import {
  runMeContextualAi,
  type AiContextualResponse,
  type AiOperatingMode,
} from "../../../../api/silaMe/menuEngineeringAiApi";
import { AICopilotDrawer } from "../../studio";
import {
  AiDataQualityIndicator,
  AiEvidencePanel,
  AiFeedbackControl,
  AiValueTag,
} from "./AiSharedWidgets";

interface Props {
  screenKey: string;
  operatingMode: AiOperatingMode;
  propertyId?: string;
  outletLocationId?: string;
  menuPlanId?: string;
  menuItemId?: string;
  open?: boolean;
  onClose?: () => void;
  onTriggerAction?: (targetEndpoint: string, payload: Record<string, unknown>) => void;
}

const CONTEXT_ACTIONS: { key: string; label: string }[] = [
  { key: "AnalyzeThis", label: "Analyze this" },
  { key: "ExplainVariance", label: "Explain variance" },
  { key: "RecommendImprovements", label: "Recommend improvements" },
  { key: "FindAlternatives", label: "Find alternatives" },
  { key: "Optimize", label: "Optimize" },
  { key: "PredictNextPeriod", label: "Predict next period" },
  { key: "CompareWithHistory", label: "Compare with history" },
  { key: "SimulateChange", label: "Simulate change" },
  { key: "GenerateDraft", label: "Generate draft" },
  { key: "ShowSupportingEvidence", label: "Show supporting evidence" },
];

export const AiContextualAssistantPanel: React.FC<Props> = ({
  screenKey,
  operatingMode,
  propertyId,
  outletLocationId,
  menuPlanId,
  menuItemId,
  open,
  onClose,
  onTriggerAction,
}) => {
  const [internalOpen, setInternalOpen] = useState(false);
  const isControlled = typeof open === "boolean";
  const isOpen = isControlled ? open : internalOpen;
  const handleClose = () => {
    if (isControlled) {
      onClose?.();
    } else {
      setInternalOpen(false);
    }
  };

  const [prompt, setPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [response, setResponse] = useState<AiContextualResponse | null>(null);
  const [listening, setListening] = useState(false);

  const invoke = async (actionKey: string) => {
    setLoading(true);
    setError(null);
    try {
      const res = await runMeContextualAi({
        screenKey,
        action: actionKey,
        userPrompt: prompt.trim() || undefined,
        propertyId,
        outletLocationId,
        menuPlanId,
        menuItemId,
        operatingMode,
      });
      setResponse(res);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Contextual AI request failed.");
    } finally {
      setLoading(false);
    }
  };

  const toggleVoiceInput = () => {
    const SpeechRec = (window as unknown as { SpeechRecognition?: new () => unknown; webkitSpeechRecognition?: new () => unknown }).SpeechRecognition
      || (window as unknown as { webkitSpeechRecognition?: new () => unknown }).webkitSpeechRecognition;
    if (!SpeechRec) {
      setError("Voice recognition is not supported in this browser.");
      return;
    }
    try {
      const rec = new SpeechRec() as {
        lang: string;
        interimResults: boolean;
        onresult: (ev: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void;
        onerror: () => void;
        onend: () => void;
        start: () => void;
      };
      rec.lang = "en-US";
      rec.interimResults = false;
      rec.onresult = (ev) => {
        const text = ev.results?.[0]?.[0]?.transcript ?? "";
        if (text) setPrompt(text);
      };
      rec.onerror = () => setListening(false);
      rec.onend = () => setListening(false);
      setListening(true);
      rec.start();
    } catch {
      setListening(false);
    }
  };

  return (
    <AICopilotDrawer
      open={isOpen}
      onClose={handleClose}
      title="SILA AI Copilot"
      subtitle={`${screenKey} · Mode: ${operatingMode}`}
      promptValue={prompt}
      onPromptChange={setPrompt}
      onSubmitPrompt={() => void invoke("AnalyzeThis")}
      submitting={loading}
      quickActions={CONTEXT_ACTIONS.map((a) => ({
        key: a.key,
        label: a.label,
        onClick: () => void invoke(a.key),
      }))}
      onVoiceToggle={toggleVoiceInput}
      voiceActive={listening}
    >
      {error && <div className="sme-ai-warning-item">{error}</div>}

      {!response && !error && (
        <div className="sila-studio-muted">
          Choose a quick prompt above or ask SILA AI about this workspace. Evidence, confidence, and approval controls appear here.
        </div>
      )}

      {response && (
        <div className="sme-ai-response-box">
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.4rem", flexWrap: "wrap" }}>
            <AiValueTag kind={response.valueTag} />
            <span className="sme-ai-agent-pill">{response.agentId}</span>
          </div>
          <p className="sme-ai-summary">{response.summary}</p>
          <p className="sme-ai-explanation">{response.explanation}</p>
          <AiDataQualityIndicator
            quality={response.dataQuality}
            freshness={response.dataFreshness}
            source={response.aiProviderUsed ? "AI + Business Services" : "Deterministic Fallback"}
          />
          <AiEvidencePanel evidence={response.evidence} warnings={response.warnings} tools={response.invokedTools} />
          {response.proposedActions.length > 0 && (
            <div className="sme-ai-proposed-actions">
              {response.proposedActions.map((pa, idx) => (
                <button
                  key={idx}
                  type="button"
                  className="sila-studio-btn sila-studio-btn--secondary"
                  onClick={() => onTriggerAction?.(pa.targetEndpoint, pa.payload)}
                >
                  {pa.label} {pa.requiresApproval ? "(Approval Gated)" : ""}
                </button>
              ))}
            </div>
          )}
          <AiFeedbackControl agentId={response.agentId} screenKey={screenKey} targetEntityId={menuPlanId || menuItemId} />
        </div>
      )}
    </AICopilotDrawer>
  );
};
