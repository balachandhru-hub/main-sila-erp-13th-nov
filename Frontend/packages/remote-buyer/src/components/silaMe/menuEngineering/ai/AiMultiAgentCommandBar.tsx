import React, { useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  cancelMeMultiAgentRun,
  runMeMultiAgentCommand,
  type AiOperatingMode,
  type MeStudioMultiAgentRunResult,
  type OptimizedScenarioOption,
} from "../../../../api/silaMe/menuEngineeringAiApi";
import { AiAgentProgressIndicator, AiScenarioComparison } from "./AiSharedWidgets";

interface Props {
  operatingMode: AiOperatingMode;
  propertyId?: string;
  outletLocationId?: string;
  onDraftCreated: (draftMenuId: string) => void;
}

const DESIGN_MODES = [
  { key: "BUDGET_OPTIMIZED", label: "Budget Optimized" },
  { key: "MARGIN_OPTIMIZED", label: "Margin Optimized" },
  { key: "PREMIUM_EXPERIENCE", label: "Premium Experience" },
  { key: "POPULARITY_OPTIMIZED", label: "Popularity Optimized" },
  { key: "WASTE_OPTIMIZED", label: "Waste Optimized" },
  { key: "INVENTORY_OPTIMIZED", label: "Inventory Optimized" },
  { key: "SEASONAL_MENU", label: "Seasonal Menu" },
  { key: "HEALTHY_MENU", label: "Healthy Menu" },
  { key: "HISTORICAL_BEST", label: "Historical Best Performer" },
  { key: "BALANCED", label: "Balanced Recommendation" },
  { key: "CUSTOM", label: "Custom Objective" },
];

export const AiMultiAgentCommandBar: React.FC<Props> = ({
  operatingMode,
  propertyId,
  outletLocationId,
  onDraftCreated,
}) => {
  const [command, setCommand] = useState(
    "Create an international dinner buffet for 500 guests with AED 150 selling price, maximum 30% food cost, vegetarian options, three desserts and maximum utilization of available inventory."
  );
  const [designMode, setDesignMode] = useState("BALANCED");
  const [running, setRunning] = useState(false);
  const [listening, setListening] = useState(false);
  const [showWeights, setShowWeights] = useState(false);
  const [weights, setWeights] = useState({
    foodCost: 25,
    margin: 25,
    popularity: 15,
    availability: 15,
    waste: 10,
    production: 5,
    diversity: 5,
  });
  const [runResult, setRunResult] = useState<MeStudioMultiAgentRunResult | null>(null);
  const [selectedScenario, setSelectedScenario] = useState<OptimizedScenarioOption | null>(null);

  const startVoiceCapture = () => {
    const SR = (window as unknown as { SpeechRecognition?: new () => any; webkitSpeechRecognition?: new () => any }).SpeechRecognition ||
      (window as unknown as { webkitSpeechRecognition?: new () => any }).webkitSpeechRecognition;
    if (!SR) {
      toastService.info("Browser speech recognition unavailable; type your command or use SILA Voice Mobile.");
      return;
    }
    const rec = new SR();
    rec.lang = "en-US";
    rec.onstart = () => setListening(true);
    rec.onend = () => setListening(false);
    rec.onresult = (ev: any) => {
      const txt = ev?.results?.[0]?.[0]?.transcript;
      if (txt) setCommand(txt);
    };
    rec.start();
  };

  const executeCommand = async () => {
    if (!command.trim()) return;
    setRunning(true);
    try {
      const res = await runMeMultiAgentCommand({
        objective: command.trim(),
        operatingMode: operatingMode === "MANUAL" ? "AI_ASSISTED" : operatingMode,
        optimizationMode: designMode,
        propertyId: propertyId || undefined,
        outletLocationId: outletLocationId || undefined,
        saveSelectedProposalAsDraft: true,
      });
      setRunResult(res);
      const rec = res.scenarios.find((s) => s.scenarioCode === res.recommendedScenarioCode) || res.scenarios[0] || null;
      setSelectedScenario(rec);
      if (res.createdDraftMenuId) {
        toastService.success("Multi-agent draft menu prepared (approval required before publishing).");
        onDraftCreated(res.createdDraftMenuId);
      } else if (!res.feasible) {
        toastService.warning("Constraints conflict — review conflict explanation below.");
      }
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Multi-agent execution failed.");
    } finally {
      setRunning(false);
    }
  };

  return (
    <div className="sme-panel sme-ai-command-bar">
      <div className="sme-ai-command-row">
        <div style={{ flex: 1 }}>
          <label className="sila-label">ME Studio AI Command Center (Natural Language &amp; SILA Voice)</label>
          <input
            className="sila-input"
            value={command}
            onChange={(e) => setCommand(e.target.value)}
            placeholder="Describe the menu objective (covers, selling price, max food cost %, dietary rules, inventory priority)..."
          />
        </div>
        <div className="sila-field" style={{ minWidth: 190 }}>
          <label className="sila-label">AI Design Mode</label>
          <select className="sila-input" value={designMode} onChange={(e) => setDesignMode(e.target.value)}>
            {DESIGN_MODES.map((m) => (
              <option key={m.key} value={m.key}>{m.label}</option>
            ))}
          </select>
        </div>
        <div className="sme-ai-cmd-buttons">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={startVoiceCapture}>
            {listening ? "Listening..." : "SILA Voice"}
          </button>
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowWeights((s) => !s)}>
            Weights
          </button>
          <button type="button" className="sila-btn sila-btn--primary" disabled={running} onClick={() => void executeCommand()}>
            {running ? "Orchestrating..." : "Run Multi-Agent Plan"}
          </button>
          {runResult && runResult.status === "RUNNING" && (
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              onClick={() => void cancelMeMultiAgentRun(runResult.runId).then(setRunResult)}
            >
              Cancel
            </button>
          )}
        </div>
      </div>

      {showWeights && (
        <div className="sme-ai-weights-grid">
          {Object.entries(weights).map(([k, v]) => (
            <label key={k} className="sme-ai-weight-item">
              <span>{k}: {v}%</span>
              <input
                type="range"
                min={0}
                max={60}
                value={v}
                onChange={(e) => setWeights((prev) => ({ ...prev, [k]: Number(e.target.value) }))}
              />
            </label>
          ))}
        </div>
      )}

      {runResult && (
        <div className="sme-ai-run-results">
          <div className="sme-ai-run-banner">
            <div>
              <strong>Orchestration {runResult.status}</strong> · {runResult.serviceFormat} ({runResult.mealPeriod}) ·{" "}
              {runResult.expectedCovers} covers · Target FC &le; {runResult.maxFoodCostPercent}% ·{" "}
              <span>Auto-Publish: {runResult.autoPublished ? "Yes" : "Blocked (Approval Required)"}</span>
            </div>
            <button type="button" className="sme-ai-mini-btn" onClick={() => setRunResult(null)}>Hide</button>
          </div>
          <p className="sme-ai-explanation">{runResult.explanation}</p>
          {!runResult.feasible && runResult.conflictExplanation && (
            <div className="sme-ai-warning-item">{runResult.conflictExplanation}</div>
          )}
          <AiScenarioComparison
            scenarios={runResult.scenarios}
            selectedCode={selectedScenario?.scenarioCode}
            onSelectScenario={setSelectedScenario}
          />
          <details style={{ marginTop: "0.6rem" }}>
            <summary style={{ cursor: "pointer", fontWeight: 600 }}>15-Step Multi-Agent Execution Trace ({runResult.steps.length} steps)</summary>
            <AiAgentProgressIndicator steps={runResult.steps} />
          </details>
        </div>
      )}
    </div>
  );
};
