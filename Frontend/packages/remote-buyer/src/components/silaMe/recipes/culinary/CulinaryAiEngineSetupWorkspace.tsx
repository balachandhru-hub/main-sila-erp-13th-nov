import React, { useCallback, useEffect, useState } from "react";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  CULINARY_TASK_LABELS,
  DEFAULT_CULINARY_FEATURE_FLAGS,
  DEFAULT_CULINARY_MATERIAL_CONFIG,
  getCulinaryAiConfig,
  saveCulinaryAiConfig,
  testCulinaryAiConnection,
  type CulinaryAiFallbackPolicy,
  type CulinaryAiMode,
  type SilaCulinaryAiConfig,
  type SilaCulinaryAiConnectionTestResult,
  type SilaCulinaryAiFeatureFlags,
  type SilaCulinaryMaterialConfig,
} from "../../../../api/silaMe/silaCulinaryAiApi";
import {
  getMaterialIndexStatus,
  rebuildMaterialIndex,
  type SilaCulinaryMaterialIndexStatus,
} from "../../../../api/silaMe/silaCulinaryMaterialsApi";
import "./CulinarySuite.css";

interface CulinaryAiEngineSetupWorkspaceProps {
  onConfigUpdated?: () => void;
}

const CulinaryAiEngineSetupWorkspace: React.FC<CulinaryAiEngineSetupWorkspaceProps> = ({
  onConfigUpdated,
}) => {
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testingProvider, setTestingProvider] = useState<"LOCAL" | "GEMINI" | "CLAUDE" | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [config, setConfig] = useState<SilaCulinaryAiConfig | null>(null);
  const [testResult, setTestResult] = useState<SilaCulinaryAiConnectionTestResult | null>(null);

  // Editable form state
  const [operatingMode, setOperatingMode] = useState<CulinaryAiMode>("LOCAL");
  const [allowExternalAi, setAllowExternalAi] = useState(false);
  const [localEnabled, setLocalEnabled] = useState(true);
  const [localEndpoint, setLocalEndpoint] = useState("http://127.0.0.1:11434");
  const [localModel, setLocalModel] = useState("llama3.1:8b");
  const [geminiEnabled, setGeminiEnabled] = useState(false);
  const [geminiModel, setGeminiModel] = useState("gemini-2.5-flash");
  const [geminiApiKey, setGeminiApiKey] = useState("");
  const [clearGeminiKey, setClearGeminiKey] = useState(false);
  const [claudeEnabled, setClaudeEnabled] = useState(false);
  const [claudeModel, setClaudeModel] = useState("claude-sonnet-4-5");
  const [claudeApiKey, setClaudeApiKey] = useState("");
  const [clearClaudeKey, setClearClaudeKey] = useState(false);
  const [requestTimeoutSeconds, setRequestTimeoutSeconds] = useState(45);
  const [maxOutputTokens, setMaxOutputTokens] = useState(2048);
  const [temperature, setTemperature] = useState(0.2);
  const [dailyRequestLimit, setDailyRequestLimit] = useState(500);
  const [monthlyRequestLimit, setMonthlyRequestLimit] = useState(10000);
  const [dailyTokenBudget, setDailyTokenBudget] = useState(250000);
  const [monthlyCostBudgetUsd, setMonthlyCostBudgetUsd] = useState(150);
  const [retryCount, setRetryCount] = useState(1);
  const [fallbackPolicy, setFallbackPolicy] = useState<CulinaryAiFallbackPolicy>("LOCAL_IF_ALLOWED");
  const [taskRouting, setTaskRouting] = useState<Record<string, string>>({});
  const [featureFlags, setFeatureFlags] = useState<SilaCulinaryAiFeatureFlags>(DEFAULT_CULINARY_FEATURE_FLAGS);
  const [materialConfig, setMaterialConfig] = useState<SilaCulinaryMaterialConfig>(DEFAULT_CULINARY_MATERIAL_CONFIG);
  const [indexStatus, setIndexStatus] = useState<SilaCulinaryMaterialIndexStatus | null>(null);
  const [indexBusy, setIndexBusy] = useState(false);

  const hydrate = (data: SilaCulinaryAiConfig) => {
    setConfig(data);
    setOperatingMode(data.operatingMode);
    setAllowExternalAi(data.allowExternalAi);
    setLocalEnabled(data.localEnabled);
    setLocalEndpoint(data.localEndpoint ?? "http://127.0.0.1:11434");
    setLocalModel(data.localModel ?? "llama3.1:8b");
    setGeminiEnabled(data.geminiEnabled);
    setGeminiModel(data.geminiModel ?? "gemini-2.5-flash");
    setGeminiApiKey(data.geminiKeyMasked ?? "");
    setClearGeminiKey(false);
    setClaudeEnabled(data.claudeEnabled);
    setClaudeModel(data.claudeModel ?? "claude-sonnet-4-5");
    setClaudeApiKey(data.claudeKeyMasked ?? "");
    setClearClaudeKey(false);
    setRequestTimeoutSeconds(data.requestTimeoutSeconds);
    setMaxOutputTokens(data.maxOutputTokens);
    setTemperature(data.temperature);
    setDailyRequestLimit(data.dailyRequestLimit);
    setMonthlyRequestLimit(data.monthlyRequestLimit);
    setDailyTokenBudget(data.dailyTokenBudget);
    setMonthlyCostBudgetUsd(data.monthlyCostBudgetUsd);
    setRetryCount(data.retryCount);
    setFallbackPolicy(data.fallbackPolicy);
    setTaskRouting(data.taskRouting);
    setFeatureFlags(data.featureFlags);
    setMaterialConfig(data.materialConfig ?? DEFAULT_CULINARY_MATERIAL_CONFIG);
  };

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [loaded, idx] = await Promise.all([
        getCulinaryAiConfig(),
        getMaterialIndexStatus().catch(() => null),
      ]);
      hydrate(loaded);
      setIndexStatus(idx);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not load Culinary AI Engine Setup.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await saveCulinaryAiConfig({
        operatingMode,
        allowExternalAi,
        localEnabled,
        localEndpoint: localEndpoint.trim() || null,
        localModel: localModel.trim() || null,
        geminiEnabled,
        geminiModel: geminiModel.trim() || null,
        geminiApiKey: geminiApiKey.trim() || null,
        clearGeminiKey,
        claudeEnabled,
        claudeModel: claudeModel.trim() || null,
        claudeApiKey: claudeApiKey.trim() || null,
        clearClaudeKey,
        requestTimeoutSeconds,
        maxOutputTokens,
        temperature,
        dailyRequestLimit,
        monthlyRequestLimit,
        dailyTokenBudget,
        monthlyCostBudgetUsd,
        retryCount,
        fallbackPolicy,
        taskRouting,
        featureFlags,
        materialConfig,
      });
      hydrate(updated);
      onConfigUpdated?.();
      toastService.success("Culinary AI Engine configuration saved.");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Failed to save Culinary AI configuration.");
    } finally {
      setSaving(false);
    }
  };

  const handleTest = async (provider: "LOCAL" | "GEMINI" | "CLAUDE") => {
    setTestingProvider(provider);
    try {
      const result = await testCulinaryAiConnection(provider);
      setTestResult(result);
      if (result.success) {
        toastService.success(result.message);
      } else {
        toastService.error(result.message);
      }
      await load();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Connection test failed.");
    } finally {
      setTestingProvider(null);
    }
  };

  const handleIndexAction = async (incrementalOnly: boolean) => {
    setIndexBusy(true);
    try {
      const status = await rebuildMaterialIndex(incrementalOnly);
      setIndexStatus(status);
      toastService.success(
        incrementalOnly
          ? `Incremental material index sync complete (${status.indexedMaterialCount} materials).`
          : `Material index rebuilt cleanly (${status.indexedMaterialCount} materials in ${Math.round(status.buildDurationMs)} ms).`
      );
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Material index operation failed.");
    } finally {
      setIndexBusy(false);
    }
  };

  if (loading) {
    return <Loader size={24} message="Loading Culinary Intelligence — AI Engine Setup..." />;
  }

  if (error || !config) {
    return (
      <EmptyState
        variant="error"
        title="Unable to load AI Engine Setup"
        description={error ?? "Configuration unavailable."}
        action={<button type="button" className="sila-btn sila-btn--secondary" onClick={load}>Retry</button>}
      />
    );
  }

  const readOnly = !config.canEdit;

  return (
    <form className="cs2-shell" onSubmit={handleSave} data-testid="culinary-ai-setup-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Culinary Intelligence — AI Engine Setup</h2>
          <p className="cs2-subtitle">
            Configure Local AI, Google Gemini, Anthropic Claude, or Hybrid task-based routing (§8).
            Credentials are encrypted server-side and masked here; raw API keys are never returned to Cloud or Mobile clients.
          </p>
        </div>
        <div className="cs2-actions-row">
          <span className={`cs2-badge ${readOnly ? "cs2-badge--warn" : "cs2-badge--live"}`}>
            {readOnly ? "Read-Only (Requires MANAGE_SILA_MASTER_DATA)" : "Administrator Write Access"}
          </span>
          {!readOnly && (
            <button type="submit" className="sila-btn sila-btn--sm sila-btn--primary" disabled={saving}>
              {saving ? "Saving..." : "Save AI Configuration"}
            </button>
          )}
        </div>
      </div>

      {/* Operating Mode & Tenant Privacy */}
      <div className="cs2-grid-2">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Operating Mode &amp; Tenant Privacy</h3>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-mode">Operating Mode</label>
              <select
                id="cs2-ai-mode"
                className="sila-select"
                disabled={readOnly}
                value={operatingMode}
                onChange={(e) => setOperatingMode(e.target.value as CulinaryAiMode)}
              >
                <option value="LOCAL">Local AI only</option>
                <option value="GEMINI">Google Gemini</option>
                <option value="CLAUDE">Anthropic Claude</option>
                <option value="HYBRID">Hybrid task-based selection</option>
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-fallback">Fallback Policy</label>
              <select
                id="cs2-ai-fallback"
                className="sila-select"
                disabled={readOnly}
                value={fallbackPolicy}
                onChange={(e) => setFallbackPolicy(e.target.value as CulinaryAiFallbackPolicy)}
              >
                <option value="LOCAL_IF_ALLOWED">Fallback to Local AI (Respect Privacy)</option>
                <option value="CONFIGURED_SECONDARY">Fallback to Authorized Secondary Provider</option>
                <option value="NONE">No Fallback (Fail Explicitly)</option>
              </select>
            </div>
          </div>
          <label className="cs2-checkbox-row" style={{ marginTop: 12 }}>
            <input
              type="checkbox"
              disabled={readOnly}
              checked={allowExternalAi}
              onChange={(e) => setAllowExternalAi(e.target.checked)}
            />
            <span>
              <strong>Tenant External AI Consent:</strong> Explicitly permit sending non-credential culinary prompts to configured external LLM providers (Gemini / Claude).
            </span>
          </label>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Limits, Timeouts &amp; Budgets</h3>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-timeout">Timeout (sec)</label>
              <input id="cs2-ai-timeout" type="number" className="sila-input" disabled={readOnly} min={5} max={180} value={requestTimeoutSeconds} onChange={(e) => setRequestTimeoutSeconds(Number(e.target.value) || 45)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-tokens">Max Output Tokens</label>
              <input id="cs2-ai-tokens" type="number" className="sila-input" disabled={readOnly} min={128} max={32768} value={maxOutputTokens} onChange={(e) => setMaxOutputTokens(Number(e.target.value) || 2048)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-temp">Temperature (0–1)</label>
              <input id="cs2-ai-temp" type="number" step="0.05" className="sila-input" disabled={readOnly} min={0} max={1} value={temperature} onChange={(e) => setTemperature(Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-retry">Retry Count</label>
              <input id="cs2-ai-retry" type="number" className="sila-input" disabled={readOnly} min={0} max={5} value={retryCount} onChange={(e) => setRetryCount(Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-dlimit">Daily Request Limit</label>
              <input id="cs2-ai-dlimit" type="number" className="sila-input" disabled={readOnly} min={1} value={dailyRequestLimit} onChange={(e) => setDailyRequestLimit(Number(e.target.value) || 500)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-ai-budget">Monthly Cost Budget (USD)</label>
              <input id="cs2-ai-budget" type="number" step="1" className="sila-input" disabled={readOnly} min={0} value={monthlyCostBudgetUsd} onChange={(e) => setMonthlyCostBudgetUsd(Number(e.target.value) || 0)} />
            </div>
          </div>
        </section>
      </div>

      {/* Providers */}
      <div className="cs2-grid-3">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Local AI (Ollama / Python Engine)</h3>
            <span className={`cs2-badge ${localEnabled ? "cs2-badge--live" : "cs2-badge--planned"}`}>
              {localEnabled ? "Enabled" : "Disabled"}
            </span>
          </div>
          <label className="cs2-checkbox-row" style={{ marginBottom: 10 }}>
            <input type="checkbox" disabled={readOnly} checked={localEnabled} onChange={(e) => setLocalEnabled(e.target.checked)} />
            Enable Local AI provider
          </label>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-local-ep">Local Endpoint</label>
            <input id="cs2-local-ep" className="sila-input" disabled={readOnly} value={localEndpoint} onChange={(e) => setLocalEndpoint(e.target.value)} />
          </div>
          <div className="sila-field" style={{ marginTop: 8 }}>
            <label className="sila-label" htmlFor="cs2-local-mdl">Local Model</label>
            <input id="cs2-local-mdl" className="sila-input" disabled={readOnly} value={localModel} onChange={(e) => setLocalModel(e.target.value)} />
          </div>
          <div style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={readOnly || testingProvider !== null} onClick={() => handleTest("LOCAL")}>
              {testingProvider === "LOCAL" ? "Testing..." : "Test Local Connection"}
            </button>
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Google Gemini</h3>
            <span className={`cs2-badge ${config.geminiKeyConfigured ? "cs2-badge--live" : "cs2-badge--planned"}`}>
              {config.geminiKeyConfigured ? `Key ${config.geminiKeyMasked}` : "No Key"}
            </span>
          </div>
          <label className="cs2-checkbox-row" style={{ marginBottom: 10 }}>
            <input type="checkbox" disabled={readOnly} checked={geminiEnabled} onChange={(e) => setGeminiEnabled(e.target.checked)} />
            Enable Gemini provider
          </label>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-gem-mdl">Gemini Model</label>
            <input id="cs2-gem-mdl" className="sila-input" disabled={readOnly} value={geminiModel} onChange={(e) => setGeminiModel(e.target.value)} />
          </div>
          <div className="sila-field" style={{ marginTop: 8 }}>
            <label className="sila-label" htmlFor="cs2-gem-key">API Credential (Masked on Reload)</label>
            <input id="cs2-gem-key" type="password" autoComplete="new-password" className="sila-input" disabled={readOnly} value={geminiApiKey} onChange={(e) => { setGeminiApiKey(e.target.value); setClearGeminiKey(false); }} />
          </div>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={readOnly || testingProvider !== null} onClick={() => handleTest("GEMINI")}>
              {testingProvider === "GEMINI" ? "Testing..." : "Test Gemini"}
            </button>
            {!readOnly && config.geminiKeyConfigured && (
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => { setGeminiApiKey(""); setClearGeminiKey(true); }}>
                Clear Key
              </button>
            )}
          </div>
        </section>

        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Anthropic Claude</h3>
            <span className={`cs2-badge ${config.claudeKeyConfigured ? "cs2-badge--live" : "cs2-badge--planned"}`}>
              {config.claudeKeyConfigured ? `Key ${config.claudeKeyMasked}` : "No Key"}
            </span>
          </div>
          <label className="cs2-checkbox-row" style={{ marginBottom: 10 }}>
            <input type="checkbox" disabled={readOnly} checked={claudeEnabled} onChange={(e) => setClaudeEnabled(e.target.checked)} />
            Enable Claude provider
          </label>
          <div className="sila-field">
            <label className="sila-label" htmlFor="cs2-cld-mdl">Claude Model</label>
            <input id="cs2-cld-mdl" className="sila-input" disabled={readOnly} value={claudeModel} onChange={(e) => setClaudeModel(e.target.value)} />
          </div>
          <div className="sila-field" style={{ marginTop: 8 }}>
            <label className="sila-label" htmlFor="cs2-cld-key">API Credential (Masked on Reload)</label>
            <input id="cs2-cld-key" type="password" autoComplete="new-password" className="sila-input" disabled={readOnly} value={claudeApiKey} onChange={(e) => { setClaudeApiKey(e.target.value); setClearClaudeKey(false); }} />
          </div>
          <div className="cs2-actions-row" style={{ marginTop: 12 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={readOnly || testingProvider !== null} onClick={() => handleTest("CLAUDE")}>
              {testingProvider === "CLAUDE" ? "Testing..." : "Test Claude"}
            </button>
            {!readOnly && config.claudeKeyConfigured && (
              <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => { setClaudeApiKey(""); setClearClaudeKey(true); }}>
                Clear Key
              </button>
            )}
          </div>
        </section>
      </div>

      {(testResult || config.lastTestStatus) && (
        <div className={`cs2-banner ${(testResult?.success ?? config.lastTestStatus === "OK") ? "" : "cs2-banner--warn"}`} role="status">
          <div>
            <strong>Last Provider Connection Check ({testResult?.provider ?? config.lastTestProvider}): </strong>
            {testResult?.message ?? config.lastTestMessage}
          </div>
          <span className="cs2-badge cs2-badge--indigo">
            {testResult?.statusCode ?? config.lastTestStatus}
            {testResult?.latencyMs != null ? ` · ${testResult.latencyMs} ms` : ""}
          </span>
        </div>
      )}

      {/* Task Routing + Feature Flags */}
      <div className="cs2-grid-2">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Task-Based Engine Selection ({CULINARY_TASK_LABELS.length} Tasks)</h3>
            {config.promptVersion && (
              <span className="cs2-badge cs2-badge--indigo">Prompt {config.promptVersion}</span>
            )}
          </div>
          <div className="cs2-table-wrap">
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>Culinary Task</th>
                  <th>Assigned Provider</th>
                </tr>
              </thead>
              <tbody>
                {CULINARY_TASK_LABELS.map((t) => (
                  <tr key={t.key}>
                    <td>{t.label}</td>
                    <td>
                      <select
                        className="sila-select"
                        disabled={readOnly}
                        aria-label={`Engine for ${t.label}`}
                        value={taskRouting[t.key] ?? "LOCAL"}
                        onChange={(e) => setTaskRouting((prev) => ({ ...prev, [t.key]: e.target.value }))}
                      >
                        <option value="LOCAL">Local AI</option>
                        <option value="GEMINI">Gemini</option>
                        <option value="CLAUDE">Claude</option>
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <div className="cs2-shell">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Recipe Studio 2.0 Feature Flags</h3>
            </div>
            {(
              [
                ["culinarySuiteEnabled", "Master Recipe Studio 2.0 Contextual Navigation"],
                ["overviewEnabled", "Culinary Intelligence Overview"],
                ["recipePlannerPreviewEnabled", "AI Recipe Planner & Scaling Simulator"],
                ["materialIntelligencePreviewEnabled", "Material Intelligence & Requests Preview"],
                ["menuEventPreviewEnabled", "Menu Studio, Event Planner & Production Preview"],
                ["interactiveSimulatorsEnabled", "Interactive Event MRP & P&L Simulators"],
                ["analyticsReportPreviewEnabled", "Culinary Analytics & Report Studio Preview"],
                ["demonstrationModeEnabled", "Isolated Demonstration Mode Toggle"],
              ] as [keyof SilaCulinaryAiFeatureFlags, string][]
            ).map(([key, label]) => (
              <label key={key} className="cs2-checkbox-row" style={{ marginBottom: 8 }}>
                <input
                  type="checkbox"
                  disabled={readOnly}
                  checked={featureFlags[key]}
                  onChange={(e) => setFeatureFlags((prev) => ({ ...prev, [key]: e.target.checked }))}
                />
                {label}
              </label>
            ))}
          </section>

          <section className="cs2-card" data-testid="culinary-ai-usage-dashboard">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">AI Usage Dashboard (Zero Fabricated Metrics)</h3>
              <span className="cs2-badge cs2-badge--live">Audited Live State</span>
            </div>
            <p className="cs2-empty-note" style={{ marginBottom: 10 }}>
              {config.usageSummary.note}
            </p>
            <div className="cs2-kpi-grid">
              <div className="cs2-kpi">
                <span className="cs2-kpi__label">Requests (OK / Fail)</span>
                <span className="cs2-kpi__value">
                  {config.usageSummary.totalRequests} ({config.usageSummary.successfulRequests} / {config.usageSummary.failedRequests})
                </span>
              </div>
              <div className="cs2-kpi">
                <span className="cs2-kpi__label">Tokens (In / Out)</span>
                <span className="cs2-kpi__value">
                  {config.usageSummary.inputTokens} / {config.usageSummary.outputTokens}
                </span>
              </div>
              <div className="cs2-kpi">
                <span className="cs2-kpi__label">Est. Cost (USD)</span>
                <span className="cs2-kpi__value">${config.usageSummary.estimatedCostUsd.toFixed(2)}</span>
                <span className="cs2-kpi__hint">
                  Fallbacks: {config.usageSummary.fallbackCount ?? 0}
                  {config.usageSummary.averageLatencyMs != null ? ` · Avg ${Math.round(config.usageSummary.averageLatencyMs)} ms` : ""}
                </span>
              </div>
              <div className="cs2-kpi">
                <span className="cs2-kpi__label">Local Runtime Status</span>
                <span className="cs2-kpi__value" style={{ fontSize: 15 }}>
                  {config.usageSummary.localInferenceStatus ?? "NOT_TESTED"}
                </span>
              </div>
            </div>
          </section>
        </div>
      </div>

      {/* Phase 1B Bounded Material Intelligence Configuration & Index Administration (§25) */}
      <div className="cs2-grid-2" data-testid="culinary-material-intelligence-setup">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Material Intelligence Matching &amp; Local Models (Phase 1B)</h3>
            <span className="cs2-badge cs2-badge--live">Local-First Engine</span>
          </div>

          <div className="cs2-form-grid" style={{ marginBottom: 12 }}>
            {(
              [
                ["exactMatchingEnabled", "Level 1–2: Exact code & normalized description"],
                ["aliasMatchingEnabled", "Level 3: Tenant approved aliases"],
                ["fuzzyMatchingEnabled", "Level 5: RapidFuzz lexical matching"],
                ["semanticMatchingEnabled", "Level 6: Semantic vector matching (FAISS/ONNX)"],
                ["advancedRerankingEnabled", "Level 7: CrossEncoder candidate reranking"],
                ["llmAssistedMatchingEnabled", "Level 8: Optional LLM disambiguation (bounded)"],
                ["mandatoryChefReview", "Level 9: Mandatory chef confirmation before lock"],
              ] as [keyof SilaCulinaryMaterialConfig, string][]
            ).map(([k, label]) => (
              <label key={k} className="cs2-checkbox-row">
                <input
                  type="checkbox"
                  disabled={readOnly}
                  checked={Boolean(materialConfig[k])}
                  onChange={(e) =>
                    setMaterialConfig((prev) => ({ ...prev, [k]: e.target.checked }))
                  }
                />
                {label}
              </label>
            ))}
          </div>

          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mat-emb">Embedding Model</label>
              <input
                id="cs2-mat-emb"
                className="sila-input"
                disabled={readOnly}
                value={materialConfig.embeddingModel}
                onChange={(e) => setMaterialConfig((p) => ({ ...p, embeddingModel: e.target.value }))}
              />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mat-rerank">CrossEncoder Reranker</label>
              <input
                id="cs2-mat-rerank"
                className="sila-input"
                disabled={readOnly}
                value={materialConfig.rerankingModel}
                onChange={(e) => setMaterialConfig((p) => ({ ...p, rerankingModel: e.target.value }))}
              />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mat-dev">Execution Mode</label>
              <select
                id="cs2-mat-dev"
                className="sila-select"
                disabled={readOnly}
                value={materialConfig.executionDevice}
                onChange={(e) => setMaterialConfig((p) => ({ ...p, executionDevice: e.target.value }))}
              >
                <option value="cpu">CPU (ONNX Runtime)</option>
                <option value="gpu">GPU (if available)</option>
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mat-ret">Candidate Retrieval / Display</label>
              <div style={{ display: "flex", gap: 6 }}>
                <input
                  id="cs2-mat-ret"
                  type="number"
                  min={5}
                  max={100}
                  className="sila-input"
                  disabled={readOnly}
                  value={materialConfig.candidateRetrievalLimit}
                  onChange={(e) =>
                    setMaterialConfig((p) => ({
                      ...p,
                      candidateRetrievalLimit: Number(e.target.value) || 15,
                    }))
                  }
                />
                <input
                  type="number"
                  min={1}
                  max={20}
                  aria-label="Candidate display limit"
                  className="sila-input"
                  disabled={readOnly}
                  value={materialConfig.candidateDisplayLimit}
                  onChange={(e) =>
                    setMaterialConfig((p) => ({
                      ...p,
                      candidateDisplayLimit: Number(e.target.value) || 5,
                    }))
                  }
                />
              </div>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mat-fuzzy">Fuzzy / Semantic Thresholds</label>
              <div style={{ display: "flex", gap: 6 }}>
                <input
                  id="cs2-mat-fuzzy"
                  type="number"
                  step="1"
                  min={0}
                  max={100}
                  className="sila-input"
                  disabled={readOnly}
                  value={materialConfig.fuzzyThreshold}
                  onChange={(e) =>
                    setMaterialConfig((p) => ({ ...p, fuzzyThreshold: Number(e.target.value) || 70 }))
                  }
                />
                <input
                  type="number"
                  step="0.01"
                  min={0}
                  max={1}
                  aria-label="Semantic threshold"
                  className="sila-input"
                  disabled={readOnly}
                  value={materialConfig.semanticThreshold}
                  onChange={(e) =>
                    setMaterialConfig((p) => ({ ...p, semanticThreshold: Number(e.target.value) || 0.66 }))
                  }
                />
              </div>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mat-gap">Ambiguity Margin / Max Bulk</label>
              <div style={{ display: "flex", gap: 6 }}>
                <input
                  id="cs2-mat-gap"
                  type="number"
                  step="0.01"
                  min={0}
                  max={0.5}
                  className="sila-input"
                  disabled={readOnly}
                  value={materialConfig.ambiguityGapThreshold}
                  onChange={(e) =>
                    setMaterialConfig((p) => ({
                      ...p,
                      ambiguityGapThreshold: Number(e.target.value) || 0.04,
                    }))
                  }
                />
                <input
                  type="number"
                  min={1}
                  max={500}
                  aria-label="Max ingredients per bulk"
                  className="sila-input"
                  disabled={readOnly}
                  value={materialConfig.maxIngredientsPerBulk}
                  onChange={(e) =>
                    setMaterialConfig((p) => ({
                      ...p,
                      maxIngredientsPerBulk: Number(e.target.value) || 100,
                    }))
                  }
                />
              </div>
            </div>
          </div>
        </section>

        <section className="cs2-card" data-testid="culinary-material-index-admin">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Tenant Material Index Administration</h3>
            <span
              className={`cs2-badge ${
                indexStatus?.status === "READY"
                  ? "cs2-badge--live"
                  : indexStatus?.status === "FAILED"
                    ? "cs2-badge--danger"
                    : "cs2-badge--warn"
              }`}
            >
              {indexStatus?.status ?? "NOT_BUILT"}
            </span>
          </div>
          <p className="cs2-empty-note" style={{ marginBottom: 10 }}>
            Derived tenant-isolated search index built strictly from authorized Material Master records. Existing SAP schedulers remain parked.
          </p>
          <div className="cs2-kpi-grid" style={{ marginBottom: 12 }}>
            <div className="cs2-kpi">
              <span className="cs2-kpi__label">Indexed / Active</span>
              <span className="cs2-kpi__value">
                {indexStatus?.indexedMaterialCount ?? 0} / {indexStatus?.activeMaterialCount ?? 0}
              </span>
            </div>
            <div className="cs2-kpi">
              <span className="cs2-kpi__label">Pending Updates</span>
              <span className="cs2-kpi__value">{indexStatus?.pendingUpdatesCount ?? 0}</span>
            </div>
            <div className="cs2-kpi">
              <span className="cs2-kpi__label">Semantic Backend</span>
              <span className="cs2-kpi__value" style={{ fontSize: 13 }}>
                {indexStatus?.semanticBackend ?? "FASTEMBED_ONNX"}
              </span>
            </div>
            <div className="cs2-kpi">
              <span className="cs2-kpi__label">Build Duration</span>
              <span className="cs2-kpi__value">
                {indexStatus ? `${Math.round(indexStatus.buildDurationMs)} ms` : "—"}
              </span>
            </div>
          </div>
          {indexStatus?.lastError && (
            <div className="cs2-banner cs2-banner--warn" style={{ marginBottom: 10 }}>
              <strong>Last Indexing Error:</strong> {indexStatus.lastError}
            </div>
          )}
          <div className="cs2-actions-row">
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--secondary"
              disabled={readOnly || indexBusy}
              onClick={() => handleIndexAction(true)}
            >
              {indexBusy ? "Syncing..." : "Refresh Changed Materials"}
            </button>
            <button
              type="button"
              className="sila-btn sila-btn--sm sila-btn--primary"
              disabled={readOnly || indexBusy}
              onClick={() => handleIndexAction(false)}
            >
              {indexBusy ? "Rebuilding..." : "Controlled Full Rebuild"}
            </button>
          </div>
        </section>
      </div>
    </form>
  );
};

export default CulinaryAiEngineSetupWorkspace;
