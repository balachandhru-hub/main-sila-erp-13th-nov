import React, { useState } from "react";
import { Maximize2, Minimize2, Mic, Send, Sparkles } from "lucide-react";
import { StudioDrawer } from "./StudioWorkspacePrimitives";

export type AIResultKind =
  | "Recommendation"
  | "Prediction"
  | "Simulation"
  | "Approved action"
  | "Executed action"
  | "Realized outcome"
  | "Analysis"
  | "Insight"
  | "Risk";

const kindBadgeClass = (kind: string): string => {
  switch (kind) {
    case "Approved action":
    case "Executed action":
    case "Realized outcome":
      return "sila-studio-badge sila-studio-badge--success";
    case "Risk":
    case "warning":
      return "sila-studio-badge sila-studio-badge--error";
    case "Prediction":
    case "Simulation":
      return "sila-studio-badge sila-studio-badge--warning";
    default:
      return "sila-studio-badge sila-studio-badge--info";
  }
};

export interface AIInsightCardProps {
  title: string;
  kind?: AIResultKind;
  statusLabel?: string;
  statusTone?: string;
  summary?: React.ReactNode;
  explanation?: React.ReactNode;
  evidence?: string[] | string;
  impact?: string;
  meta?: React.ReactNode;
  actions?: React.ReactNode;
  action?: React.ReactNode;
  actionSlot?: React.ReactNode;
}

export const AIInsightCard: React.FC<AIInsightCardProps> = ({
  title,
  kind = "Insight",
  statusLabel,
  statusTone,
  summary,
  explanation,
  evidence = [],
  impact,
  meta,
  actions,
  action,
  actionSlot,
}) => {
  const evidenceList = Array.isArray(evidence) ? evidence : evidence ? [evidence] : [];
  const badgeText = statusLabel ?? kind;
  const badgeCls = statusTone ? `sila-studio-badge sila-studio-badge--${statusTone}` : kindBadgeClass(kind);
  const actionsNode = actions ?? action ?? actionSlot;

  return (
    <article className="sila-studio-ai-card">
      <div className="sila-studio-ai-card__head">
        <span className="sila-studio-ai-card__title-wrap">
          <Sparkles size={15} className="sila-studio-ai-card__icon" aria-hidden="true" />
          <span>{title}</span>
        </span>
        <span className={badgeCls}>{badgeText}</span>
      </div>
      {summary && <p className="sila-studio-ai-card__body">{summary}</p>}
      {explanation && <p className="sila-studio-ai-card__body" style={{ color: "#64748b" }}>{explanation}</p>}
      {evidenceList.length > 0 && (
        <div className="sila-studio-ai-card__meta">
          <span>Evidence: {evidenceList.join(" · ")}</span>
        </div>
      )}
      {impact && (
        <div className="sila-studio-ai-card__meta">
          <strong>Impact: {impact}</strong>
        </div>
      )}
      {meta && <div className="sila-studio-ai-card__meta">{meta}</div>}
      {actionsNode && <div className="sila-studio-ai-card__actions">{actionsNode}</div>}
    </article>
  );
};

export interface AIRecommendationCardProps extends AIInsightCardProps {
  recommendation?: React.ReactNode;
  impactText?: string;
  requiresApproval?: boolean;
  actionLabel?: string;
  onAction?: () => void;
}

export const AIRecommendationCard: React.FC<AIRecommendationCardProps> = ({
  recommendation,
  summary,
  impactText,
  impact,
  requiresApproval,
  actionLabel,
  onAction,
  meta,
  actions,
  ...rest
}) => {
  const resolvedImpact = impactText ?? impact;
  const resolvedActions =
    actions ??
    (actionLabel && onAction ? (
      <button type="button" className="sila-studio-btn sila-studio-btn--sm sila-studio-btn--primary" onClick={onAction}>
        {actionLabel}
      </button>
    ) : undefined);

  return (
    <AIInsightCard
      {...rest}
      summary={summary ?? recommendation}
      kind={rest.kind ?? "Recommendation"}
      actions={resolvedActions}
      meta={
        <>
          {resolvedImpact && <strong>Impact: {resolvedImpact}</strong>}
          {requiresApproval !== undefined && (
            <span>{requiresApproval ? "Approval required before applying" : "Advisory"}</span>
          )}
          {meta}
        </>
      }
    />
  );
};

export interface AICopilotQuickAction {
  key: string;
  label: string;
  onClick?: () => void;
}

export interface AICopilotDrawerProps {
  isOpen?: boolean;
  open?: boolean;
  onClose: () => void;
  title?: string;
  contextLabel?: string;
  subtitle?: string;
  quickActions?: AICopilotQuickAction[];
  promptValue: string;
  onPromptChange: (value: string) => void;
  onSubmitPrompt: (actionKey?: string) => void;
  onVoiceInput?: () => void;
  onVoiceToggle?: () => void;
  voiceListening?: boolean;
  voiceActive?: boolean;
  loading?: boolean;
  submitting?: boolean;
  children?: React.ReactNode;
}

export const AICopilotDrawer: React.FC<AICopilotDrawerProps> = ({
  isOpen,
  open,
  onClose,
  title = "SILA AI Copilot",
  contextLabel,
  subtitle,
  quickActions = [],
  promptValue,
  onPromptChange,
  onSubmitPrompt,
  onVoiceInput,
  onVoiceToggle,
  voiceListening,
  voiceActive,
  loading,
  submitting,
  children,
}) => {
  const [expanded, setExpanded] = useState(false);
  const activeOpen = Boolean(isOpen ?? open);
  const resolvedContext = contextLabel ?? subtitle;
  const handleVoice = onVoiceInput ?? onVoiceToggle;
  const isListening = Boolean(voiceListening ?? voiceActive);
  const isBusy = Boolean(loading ?? submitting);

  return (
    <StudioDrawer
      isOpen={activeOpen}
      onClose={onClose}
      title={title}
      icon={<Sparkles size={16} color="#087BB5" aria-hidden="true" />}
      side="right"
      width={expanded ? 640 : 420}
      testId="ai-copilot-drawer"
      headerControls={
        <button
          type="button"
          className="sila-studio-btn sila-studio-btn--sm"
          onClick={() => setExpanded((e) => !e)}
          title={expanded ? "Standard panel width" : "Expand AI Copilot"}
          aria-label={expanded ? "Restore AI Copilot width" : "Expand AI Copilot"}
        >
          {expanded ? <Minimize2 size={13} aria-hidden="true" /> : <Maximize2 size={13} aria-hidden="true" />}
        </button>
      }
    >
      {resolvedContext && (
        <div className="sila-studio-badge sila-studio-badge--info" style={{ alignSelf: "flex-start" }}>
          Context: {resolvedContext}
        </div>
      )}

      <div style={{ display: "flex", gap: "0.4rem", alignItems: "center" }}>
        <input
          type="text"
          className="sila-studio-input"
          style={{ flex: 1 }}
          placeholder="Ask SILA AI about this workspace..."
          value={promptValue}
          onChange={(e) => onPromptChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter" && promptValue.trim()) onSubmitPrompt();
          }}
        />
        {handleVoice && (
          <button
            type="button"
            className={`sila-studio-btn sila-studio-btn--sm ${isListening ? "sila-studio-btn--primary" : ""}`}
            onClick={handleVoice}
            aria-label="Voice input"
            title="Voice input"
          >
            <Mic size={14} aria-hidden="true" />
          </button>
        )}
        <button
          type="button"
          className="sila-studio-btn sila-studio-btn--primary sila-studio-btn--sm"
          disabled={isBusy}
          onClick={() => onSubmitPrompt()}
        >
          <Send size={13} aria-hidden="true" />
          <span>{isBusy ? "Thinking..." : "Ask"}</span>
        </button>
      </div>

      {quickActions.length > 0 && (
        <div style={{ display: "flex", flexWrap: "wrap", gap: "0.35rem" }}>
          {quickActions.map((act) => (
            <button
              key={act.key}
              type="button"
              disabled={isBusy}
              className="sila-studio-btn sila-studio-btn--sm"
              onClick={() => (act.onClick ? act.onClick() : onSubmitPrompt(act.key))}
            >
              {act.label}
            </button>
          ))}
        </div>
      )}

      {children}
    </StudioDrawer>
  );
};

export interface ScenarioComparisonColumn {
  id: string;
  name: string;
  badge?: string;
  metrics: { label: string; value: React.ReactNode }[];
  actions?: React.ReactNode;
}

export interface ScenarioComparisonRow {
  label: string;
  baseline: React.ReactNode;
  simulated: React.ReactNode;
  delta: React.ReactNode;
}

export interface ScenarioComparisonProps {
  title?: string;
  subtitle?: string;
  columns?: ScenarioComparisonColumn[];
  rows?: ScenarioComparisonRow[];
}

export const ScenarioComparison: React.FC<ScenarioComparisonProps> = ({
  title = "Scenario Comparison",
  subtitle,
  columns = [],
  rows = [],
}) => {
  if (columns.length === 0 && rows.length === 0) return null;
  return (
    <section className="sila-studio-panel" aria-label={title}>
      <div className="sila-studio-panel__header">
        <div>
          <h3 className="sila-studio-panel__title">{title}</h3>
          {subtitle && <p className="sila-studio-panel__subtitle">{subtitle}</p>}
        </div>
      </div>
      {rows.length > 0 && (
        <div className="sila-studio-table-wrap">
          <table className="sila-studio-table sila-tabular">
            <thead>
              <tr>
                <th>Metric</th>
                <th>Baseline</th>
                <th>Simulated</th>
                <th>Delta</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((r) => (
                <tr key={r.label}>
                  <td><strong>{r.label}</strong></td>
                  <td>{r.baseline}</td>
                  <td>{r.simulated}</td>
                  <td><span className="sila-studio-badge sila-studio-badge--info">{r.delta}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {columns.length > 0 && (
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))",
            gap: "0.75rem",
          }}
        >
          {columns.map((col) => (
            <article key={col.id} className="sila-studio-ai-card">
              <div className="sila-studio-ai-card__head">
                <strong style={{ fontSize: "0.88rem", color: "#123354" }}>{col.name}</strong>
                {col.badge && <span className="sila-studio-badge sila-studio-badge--info">{col.badge}</span>}
              </div>
              <div style={{ display: "flex", flexDirection: "column", gap: "0.3rem" }}>
                {col.metrics.map((m) => (
                  <div
                    key={m.label}
                    style={{
                      display: "flex",
                      justifyContent: "space-between",
                      fontSize: "0.8rem",
                      borderBottom: "1px solid #e1edf8",
                      paddingBottom: "0.22rem",
                    }}
                  >
                    <span style={{ color: "#64748b" }}>{m.label}</span>
                    <strong className="sila-tabular" style={{ color: "#123354" }}>{m.value}</strong>
                  </div>
                ))}
              </div>
              {col.actions && <div className="sila-studio-ai-card__actions">{col.actions}</div>}
            </article>
          ))}
        </div>
      )}
    </section>
  );
};
