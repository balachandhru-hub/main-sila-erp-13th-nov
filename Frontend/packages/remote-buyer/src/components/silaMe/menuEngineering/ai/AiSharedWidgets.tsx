import React, { useState } from "react";
import type {
  MeRecommendationCockpitItem,
  OptimizedScenarioOption,
} from "../../../../api/silaMe/menuEngineeringAiApi";
import { submitMeAiFeedback } from "../../../../api/silaMe/menuEngineeringAiApi";

export const AiValueTag: React.FC<{
  kind: "ACTUAL" | "FORECAST" | "AI_RECOMMENDATION" | "SIMULATION" | "ESTIMATED" | "REALIZED" | string;
}> = ({ kind }) => {
  const labelMap: Record<string, string> = {
    ACTUAL: "Actual",
    FORECAST: "Forecast",
    AI_RECOMMENDATION: "AI Recommendation",
    SIMULATION: "Simulation",
    ESTIMATED: "Estimated Saving",
    REALIZED: "Realized Saving",
  };
  return <span className={`sme-ai-tag sme-ai-tag--${kind.toLowerCase()}`}>{labelMap[kind] ?? kind}</span>;
};

export const AiDataQualityIndicator: React.FC<{ quality: string; freshness?: string; source?: string }> = ({
  quality,
  freshness,
  source,
}) => (
  <span className="sme-ai-dq" title={source || "Data quality and freshness"}>
    Quality: <strong>{quality}</strong>
    {freshness ? ` · ${freshness}` : ""}
    {source ? ` · ${source}` : ""}
  </span>
);

export const AiRiskIndicator: React.FC<{ severity: string; text: string }> = ({ severity, text }) => (
  <div className={`sme-ai-risk sme-ai-risk--${severity.toLowerCase()}`}>
    <strong>{severity}</strong>: {text}
  </div>
);

export const AiEvidencePanel: React.FC<{ evidence: string[]; warnings?: string[]; tools?: string[] }> = ({
  evidence,
  warnings,
  tools,
}) => (
  <div className="sme-ai-evidence">
    {evidence.length > 0 && (
      <div>
        <div className="sme-ai-subtitle">Supporting Evidence</div>
        <ul>
          {evidence.map((e, i) => (
            <li key={i}>{e}</li>
          ))}
        </ul>
      </div>
    )}
    {warnings && warnings.length > 0 && (
      <div className="sme-ai-warnings">
        {warnings.map((w, i) => (
          <div key={i} className="sme-ai-warning-item">{w}</div>
        ))}
      </div>
    )}
    {tools && tools.length > 0 && (
      <div className="sme-ai-tools-used">Tools: {tools.join(" · ")}</div>
    )}
  </div>
);

export const AiFeedbackControl: React.FC<{
  agentId: string;
  screenKey: string;
  targetEntityId?: string;
}> = ({ agentId, screenKey, targetEntityId }) => {
  const [sent, setSent] = useState<number | null>(null);
  const send = async (rating: number, wasAccepted: boolean) => {
    setSent(rating);
    try {
      await submitMeAiFeedback({ agentId, contextScreen: screenKey, targetEntityId, rating, wasAccepted });
    } catch {
      // non-blocking
    }
  };
  if (sent !== null) {
    return <span className="sme-ai-feedback-done">Feedback recorded ({sent > 0 ? "Helpful" : "Needs review"})</span>;
  }
  return (
    <div className="sme-ai-feedback">
      <span>Helpful?</span>
      <button type="button" className="sme-ai-mini-btn" onClick={() => void send(5, true)}>Yes</button>
      <button type="button" className="sme-ai-mini-btn" onClick={() => void send(-1, false)}>No</button>
    </div>
  );
};

export const AiAgentProgressIndicator: React.FC<{
  steps: { stepNumber: number; agentId: string; title: string; status: string; outputSummary: string }[];
}> = ({ steps }) => (
  <div className="sme-ai-progress-list">
    {steps.map((s) => (
      <div key={s.stepNumber} className={`sme-ai-step sme-ai-step--${s.status.toLowerCase()}`}>
        <div className="sme-ai-step-head">
          <span>Step {s.stepNumber} · {s.agentId}</span>
          <strong>{s.status}</strong>
        </div>
        <div className="sme-ai-step-title">{s.title}</div>
        <div className="sme-ai-step-out">{s.outputSummary}</div>
      </div>
    ))}
  </div>
);

export const AiScenarioComparison: React.FC<{
  scenarios: OptimizedScenarioOption[];
  selectedCode?: string;
  onSelectScenario?: (sc: OptimizedScenarioOption) => void;
}> = ({ scenarios, selectedCode, onSelectScenario }) => (
  <div className="sme-ai-scenario-grid">
    {scenarios.map((s) => (
      <div
        key={s.scenarioCode}
        className={`sme-ai-scenario-card ${selectedCode === s.scenarioCode ? "sme-ai-scenario-card--selected" : ""} ${!s.feasible ? "sme-ai-scenario-card--infeasible" : ""}`}
      >
        <div className="sme-ai-scenario-head">
          <strong>Scenario {s.scenarioCode}: {s.label}</strong>
          <AiValueTag kind="SIMULATION" />
        </div>
        {s.paretoOptimal && s.feasible && <span className="sme-ai-pareto">Pareto Optimal</span>}
        {!s.feasible ? (
          <div className="sme-ai-warning-item">{s.conflictReason || "Infeasible under mandatory constraints."}</div>
        ) : (
          <div className="sme-ai-scenario-metrics">
            <span>Food Cost: <strong>{s.foodCostPercent.toFixed(1)}%</strong></span>
            <span>Cost/Guest: <strong>{s.foodCostPerGuest.toFixed(2)}</strong></span>
            <span>Margin: <strong>{s.contributionMargin.toFixed(2)}</strong></span>
            <span>Stock Util: <strong>{s.inventoryUtilizationPercent.toFixed(0)}%</strong></span>
            <span>Est. Waste: <strong>{s.estimatedWastePercent.toFixed(1)}%</strong></span>
          </div>
        )}
        <div className="sme-ai-scenario-dishes">{s.selectedDishNames.slice(0, 4).join(", ")}</div>
        <div className="sme-ai-scenario-tradeoff">{s.tradeOffSummary}</div>
        {onSelectScenario && s.feasible && (
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onSelectScenario(s)}>
            Preview Scenario {s.scenarioCode}
          </button>
        )}
      </div>
    ))}
  </div>
);

export const AiRecommendationCard: React.FC<{
  item: MeRecommendationCockpitItem;
  canApprove: boolean;
  onAction: (id: string, action: string) => void;
}> = ({ item, canApprove, onAction }) => (
  <div className="sme-ai-rec-card">
    <div className="sme-ai-rec-head">
      <div>
        <AiValueTag kind="AI_RECOMMENDATION" /> <strong>{item.title}</strong>
      </div>
      <span className={`sme-status-pill sme-status-pill--${item.status.toLowerCase()}`}>{item.status}</span>
    </div>
    <div className="sme-ai-rec-meta">
      {item.recommendationCode} · {item.agentId} · Priority {item.priority} · Confidence {(item.confidence * 100).toFixed(0)}% · Quality {item.dataQuality}
    </div>
    <div className="sme-ai-rec-body">
      <div><strong>Baseline:</strong> {item.currentBaseline}</div>
      <div><strong>Proposal:</strong> {item.proposedChange}</div>
      <div><strong>Evidence:</strong> {item.supportingEvidence}</div>
      {item.estimatedMarginImpact != null && (
        <div><AiValueTag kind="ESTIMATED" /> Margin Impact: <strong>{item.estimatedMarginImpact.toFixed(2)}</strong></div>
      )}
      {item.realizedFinancialImpact != null && (
        <div><AiValueTag kind="REALIZED" /> Realized Impact: <strong>{item.realizedFinancialImpact.toFixed(2)}</strong></div>
      )}
    </div>
    <div className="sme-ai-rec-actions">
      {item.status === "NEW" && (
        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onAction(item.id, "ACCEPT")}>Accept</button>
      )}
      {item.status !== "REJECTED" && item.status !== "CLOSED" && (
        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onAction(item.id, "REJECT")}>Reject</button>
      )}
      {canApprove && (item.status === "ACCEPTED" || item.status === "UNDER_REVIEW" || item.status === "NEW") && (
        <button type="button" className="sila-btn sila-btn--primary" onClick={() => onAction(item.id, "APPROVE")}>Approve</button>
      )}
      {item.status === "APPROVED" && (
        <button type="button" className="sila-btn sila-btn--primary" onClick={() => onAction(item.id, "IMPLEMENT")}>Mark Implemented</button>
      )}
      {item.status === "IMPLEMENTED" && (
        <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onAction(item.id, "VALIDATE_OUTCOME")}>Validate Outcome</button>
      )}
    </div>
  </div>
);
