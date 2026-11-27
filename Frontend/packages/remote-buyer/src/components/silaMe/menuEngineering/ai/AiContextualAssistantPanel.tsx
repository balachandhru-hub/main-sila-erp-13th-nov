import React, { useState } from "react";
import {
  runMeContextualAi,
  type AiContextualResponse,
  type AiOperatingMode,
} from "../../../../api/silaMe/menuEngineeringAiApi";
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
  onTriggerAction,
}) => {
  const [collapsed, setCollapsed] = useState(false);
  const [prompt, setPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [response, setResponse] = useState<AiContextualResponse | null>(null);

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

  if (collapsed) {
    return (
      <aside className="sme-ai-drawer sme-ai-drawer--collapsed">
        <button type="button" className="sila-btn sila-btn--primary" onClick={() => setCollapsed(false)}>
          SILA AI Assistant ◀
        </button>
      </aside>
    );
  }

  return (
    <aside className="sme-ai-drawer" aria-label="SILA Contextual AI Assistant">
      <div className="sme-ai-drawer-head">
        <div>
          <strong>SILA Contextual AI</strong>
          <div className="sme-ai-drawer-sub">{screenKey} · Mode: {operatingMode}</div>
        </div>
        <button type="button" className="sme-ai-mini-btn" onClick={() => setCollapsed(true)}>Collapse ▸</button>
      </div>

      <div className="sme-ai-drawer-prompt">
        <input
          className="sila-input"
          placeholder="Ask SILA about this screen (e.g. 'Explain variance' or 'Find alternatives')..."
          value={prompt}
          onChange={(e) => setPrompt(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && prompt.trim()) void invoke("AnalyzeThis");
          }}
        />
        <button type="button" className="sila-btn sila-btn--primary" disabled={loading} onClick={() => void invoke("AnalyzeThis")}>
          {loading ? "Thinking..." : "Ask"}
        </button>
      </div>

      <div className="sme-ai-chip-grid">
        {CONTEXT_ACTIONS.map((a) => (
          <button
            key={a.key}
            type="button"
            disabled={loading}
            className="sme-ai-chip"
            onClick={() => void invoke(a.key)}
          >
            {a.label}
          </button>
        ))}
      </div>

      {error && <div className="sme-ai-warning-item">{error}</div>}

      {response && (
        <div className="sme-ai-response-box">
          <div className="sme-ai-rec-head">
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
                  className="sila-btn sila-btn--secondary"
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
    </aside>
  );
};
