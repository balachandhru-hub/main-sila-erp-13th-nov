import React, { useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  testIntegration,
  type IntegrationConfiguration,
  type IntegrationSide,
  type IntegrationTestResult,
} from "../../../api/integrationsApi";
import { errorMessage, formatDateTime, statusLabel } from "../integrationFormat";

interface IntegrationStudioTestingProps {
  rows: IntegrationConfiguration[];
  side?: IntegrationSide;
  selectedId?: string | null;
  readOnly?: boolean;
}

const IntegrationStudioTesting: React.FC<IntegrationStudioTestingProps> = ({
  rows,
  side = "buyer",
  selectedId = null,
  readOnly = false,
}) => {
  const [configurationId, setConfigurationId] = useState(selectedId || rows[0]?.id || "");
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<IntegrationTestResult | null>(null);
  const [localError, setLocalError] = useState<string | null>(null);

  const selected = rows.find((row) => row.id === configurationId) || null;

  const runSafeTest = async () => {
    if (!configurationId || readOnly) return;
    setBusy(true);
    setLocalError(null);
    setResult(null);
    try {
      const next = await testIntegration(side, configurationId);
      setResult(next);
      if (next.success) toastService.success(next.message || "Connection test succeeded.");
      else toastService.error(next.message || "Connection test failed.");
    } catch (err: unknown) {
      const message = errorMessage(err, "The connection test could not be completed.");
      setLocalError(message);
      toastService.error(message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="istudio-panel" data-testid="istudio-testing">
      <h3 className="istudio-panel__title">API testing workspace</h3>
      <p className="istudio-panel__hint">
        Safe tests only: host connectivity, authentication, CSRF acquisition, and read/metadata probes via the existing
        Test connection endpoint. Posting tests require an isolated SAP test environment and explicit authorization — not available here.
      </p>

      <div className="istudio-banner">
        Secrets and sensitive headers are masked by the API response. Normal connection testing never posts stock movements.
      </div>

      <div className="istudio-form-grid">
        <div className="istudio-field istudio-field--full">
          <label htmlFor="istudio-test-api">Configuration</label>
          <select
            id="istudio-test-api"
            className="sila-select"
            value={configurationId}
            onChange={(event) => {
              setConfigurationId(event.target.value);
              setResult(null);
              setLocalError(null);
            }}
          >
            <option value="">Select a saved API</option>
            {rows.map((row) => (
              <option key={row.id} value={row.id}>{row.name} · {statusLabel(row.status)}</option>
            ))}
          </select>
        </div>
      </div>

      {selected ? (
        <dl className="istudio-card__rows">
          <dt>Request URL</dt>
          <dd>{selected.baseUrl}/{selected.resourcePath || ""}</dd>
          <dt>HTTP method</dt>
          <dd>{selected.httpMethod || (selected.processType.startsWith("POST") || selected.processType === "UPDATE_STOCK" ? "POST" : "GET")} (test probe may use GET)</dd>
          <dt>Authentication</dt>
          <dd>{selected.authenticationType} · credentials {selected.credentialStatus}</dd>
        </dl>
      ) : (
        <div className="istudio-empty">Save a configuration first, then run a safe connection test.</div>
      )}

      <div className="istudio-card__actions">
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          disabled={!configurationId || busy || readOnly}
          onClick={runSafeTest}
        >
          {busy ? "Testing…" : "Run safe connection test"}
        </button>
      </div>

      {localError ? <div className="istudio-banner istudio-banner--rose">{localError}</div> : null}

      {result ? (
        <div className="istudio-test-result" data-testid="istudio-test-result">
          {`Success: ${result.success}\nHTTP status: ${result.httpStatus ?? "—"}\nTested at: ${formatDateTime(result.testedAt)}\nMessage: ${result.message}\nCorrelation: returned by execution history when available`}
        </div>
      ) : null}
    </section>
  );
};

export default IntegrationStudioTesting;
