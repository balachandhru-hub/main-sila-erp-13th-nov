import React, { useEffect, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  INTEGRATION_AUTH_TYPES,
  alignIntegrationConnection,
  getIntegration,
  testIntegration,
  updateIntegration,
  type IntegrationConfiguration,
  type IntegrationConfigurationWrite,
  type IntegrationHttpMethod,
  type IntegrationPayloadFormat,
  type IntegrationSide,
  type IntegrationTestResult,
} from "../../../api/integrationsApi";
import { blank, errorMessage, formatDateTime, statusLabel } from "../integrationFormat";

interface IntegrationStudioTestingProps {
  rows: IntegrationConfiguration[];
  side?: IntegrationSide;
  selectedId?: string | null;
  readOnly?: boolean;
  /** Refresh the studio list after credentials are saved for a test. */
  onConfigurationUpdated?: (configuration: IntegrationConfiguration) => void;
}

const secretPlaceholder = (configured: boolean): string =>
  configured ? "Leave blank to keep the stored secret" : "Enter secret for this test";

/**
 * Builds a write payload from a saved configuration, overlaying only the credential fields
 * supplied in the testing workspace. Omitted secrets stay null so the protector keeps the stored value.
 */
const writePayloadFrom = (
  selected: IntegrationConfiguration,
  credentials: {
    authenticationType: string;
    username: string;
    password: string;
    apiKeyHeader: string;
    apiKey: string;
    bearerToken: string;
    clientId: string;
    clientSecret: string;
    tokenEndpoint: string;
    tokenScope: string;
  },
): IntegrationConfigurationWrite => {
  const auth = credentials.authenticationType || selected.authenticationType;
  const usesBasic = auth === "BASIC";
  const usesApiKey = auth === "API_KEY";
  const usesBearer = auth === "BEARER_TOKEN";
  const usesOAuth = auth === "OAUTH2_CLIENT_CREDENTIALS";
  const usesCustomToken = auth === "CUSTOM_TOKEN_ENDPOINT";

  return {
    name: selected.name,
    entityCode: selected.entityCode,
    processType: selected.processType,
    systemName: selected.systemName,
    protocol: selected.protocol,
    baseUrl: selected.baseUrl,
    resourcePath: selected.resourcePath,
    httpMethod: (selected.httpMethod || "GET") as IntegrationHttpMethod,
    payloadFormat: (selected.payloadFormat || "JSON") as IntegrationPayloadFormat,
    requestBody: selected.requestBody,
    headers: selected.headers,
    authenticationType: auth,
    username: usesBasic ? blank(credentials.username) : null,
    password: usesBasic ? blank(credentials.password) : null,
    apiKeyHeader: usesApiKey ? blank(credentials.apiKeyHeader) || selected.apiKeyHeader : null,
    apiKey: usesApiKey ? blank(credentials.apiKey) : null,
    bearerToken: usesBearer ? blank(credentials.bearerToken) : null,
    clientId: usesOAuth || usesCustomToken ? blank(credentials.clientId) : null,
    clientSecret: usesOAuth || usesCustomToken ? blank(credentials.clientSecret) : null,
    tokenEndpoint: usesOAuth || usesCustomToken ? blank(credentials.tokenEndpoint) : null,
    tokenScope: usesOAuth || usesCustomToken ? blank(credentials.tokenScope) : null,
    timeoutSeconds: selected.timeoutSeconds,
    retryCount: selected.retryCount,
    pageSize: selected.pageSize,
    watermarkField: selected.watermarkField,
    scheduleCron: selected.scheduleCron,
  };
};

const IntegrationStudioTesting: React.FC<IntegrationStudioTestingProps> = ({
  rows,
  side = "buyer",
  selectedId = null,
  readOnly = false,
  onConfigurationUpdated,
}) => {
  const [configurationId, setConfigurationId] = useState(selectedId || rows[0]?.id || "");
  const [alignSourceId, setAlignSourceId] = useState(() => {
    const preferred = rows.find((row) => row.name === "FIVE S4 - GET MATERIAL" && row.status === "ACTIVE");
    return preferred?.id || rows.find((row) => row.status === "ACTIVE" && row.credentialStatus === "Configured")?.id || "";
  });
  const [busy, setBusy] = useState(false);
  const [aligning, setAligning] = useState(false);
  const [result, setResult] = useState<IntegrationTestResult | null>(null);
  const [localError, setLocalError] = useState<string | null>(null);
  const [authenticationType, setAuthenticationType] = useState("BASIC");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [apiKeyHeader, setApiKeyHeader] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [bearerToken, setBearerToken] = useState("");
  const [clientId, setClientId] = useState("");
  const [clientSecret, setClientSecret] = useState("");
  const [tokenEndpoint, setTokenEndpoint] = useState("");
  const [tokenScope, setTokenScope] = useState("");
  const [persistCredentials, setPersistCredentials] = useState(true);

  const selected = rows.find((row) => row.id === configurationId) || null;
  const secretsConfigured = selected?.credentialStatus === "Configured";

  useEffect(() => {
    if (selectedId && selectedId !== configurationId) {
      setConfigurationId(selectedId);
    }
  }, [selectedId]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (!selected) {
      setAuthenticationType("BASIC");
      setUsername("");
      setPassword("");
      setApiKeyHeader("");
      setApiKey("");
      setBearerToken("");
      setClientId("");
      setClientSecret("");
      setTokenEndpoint("");
      setTokenScope("");
      return;
    }
    setAuthenticationType(selected.authenticationType || "BASIC");
    setUsername(selected.username || "");
    setPassword("");
    setApiKeyHeader(selected.apiKeyHeader || "");
    setApiKey("");
    setBearerToken("");
    setClientId("");
    setClientSecret("");
    setTokenEndpoint("");
    setTokenScope("");
    setResult(null);
    setLocalError(null);
  }, [selected?.id]); // eslint-disable-line react-hooks/exhaustive-deps

  const credentialsEntered = Boolean(
    password.trim()
    || apiKey.trim()
    || bearerToken.trim()
    || clientSecret.trim()
    || (username.trim() && username.trim() !== (selected?.username || "").trim())
    || (authenticationType && authenticationType !== selected?.authenticationType),
  );

  const clearSecrets = () => {
    setPassword("");
    setApiKey("");
    setBearerToken("");
    setClientSecret("");
  };

  const alignSources = rows.filter((row) => row.id !== configurationId && row.credentialStatus === "Configured");

  const alignFromWorking = async () => {
    if (!configurationId || !alignSourceId || readOnly) return;
    setAligning(true);
    setLocalError(null);
    setResult(null);
    try {
      const aligned = await alignIntegrationConnection(configurationId, alignSourceId);
      onConfigurationUpdated?.(aligned);
      setAuthenticationType(aligned.authenticationType || "BASIC");
      setUsername(aligned.username || "");
      clearSecrets();
      toastService.success(`Connection settings copied from ${alignSources.find((row) => row.id === alignSourceId)?.name || "the working API"}.`);
    } catch (err: unknown) {
      const message = errorMessage(err, "Could not copy the connection settings.");
      setLocalError(message);
      toastService.error(message);
    } finally {
      setAligning(false);
    }
  };

  const runSafeTest = async () => {
    if (!configurationId || !selected || readOnly) return;
    setBusy(true);
    setLocalError(null);
    setResult(null);
    try {
      let working = selected;
      if (credentialsEntered) {
        if (!persistCredentials) {
          throw new Error("Enter credentials and keep “Save credentials before test” enabled so they use the existing secure store. Ephemeral in-memory secrets are not supported.");
        }
        const payload = writePayloadFrom(selected, {
          authenticationType,
          username,
          password,
          apiKeyHeader,
          apiKey,
          bearerToken,
          clientId,
          clientSecret,
          tokenEndpoint,
          tokenScope,
        });
        working = await updateIntegration(side, configurationId, payload);
        onConfigurationUpdated?.(working);
        clearSecrets();
        toastService.success("Credentials saved securely. Running connection test…");
      }

      const next = await testIntegration(side, configurationId);
      setResult(next);
      // Reload so lastErrorSafe / testedAt / credentialStatus refresh in the studio list.
      try {
        const refreshed = await getIntegration(side, configurationId);
        onConfigurationUpdated?.(refreshed);
      } catch {
        // Test result is still shown even if the refresh fails.
      }
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

  const auth = authenticationType;
  const usesBasic = auth === "BASIC";
  const usesApiKey = auth === "API_KEY";
  const usesBearer = auth === "BEARER_TOKEN";
  const usesOAuth = auth === "OAUTH2_CLIENT_CREDENTIALS";
  const usesCustomToken = auth === "CUSTOM_TOKEN_ENDPOINT";
  const needsSecrets = usesBasic || usesApiKey || usesBearer || usesOAuth || usesCustomToken;

  return (
    <section className="istudio-panel" data-testid="istudio-testing">
      <h3 className="istudio-panel__title">API testing workspace</h3>
      <p className="istudio-panel__hint">
        Safe tests only: host connectivity, authentication, CSRF acquisition, and read/metadata probes via the existing
        Test connection endpoint. Pass credentials here when the stored secret is missing or stale — they are written
        through the same secure store used by Integration Settings. Posting tests are not available here.
      </p>

      <div className="istudio-banner">
        Secrets are write-only and never returned by the API. Leave a secret blank to keep the value already stored.
        Connection testing never posts stock movements.
      </div>

      <div className="istudio-form-grid">
        <div className="istudio-field istudio-field--full">
          <label htmlFor="istudio-test-api">Configuration</label>
          <select
            id="istudio-test-api"
            className="sila-select"
            value={configurationId}
            disabled={readOnly}
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
        <>
          <dl className="istudio-card__rows">
            <dt>Request URL</dt>
            <dd>{selected.baseUrl}/{selected.resourcePath || ""}</dd>
            <dt>HTTP method</dt>
            <dd>{selected.httpMethod || (selected.processType.startsWith("POST") || selected.processType === "UPDATE_STOCK" ? "POST" : "GET")} (test probe may use GET)</dd>
            <dt>Stored credentials</dt>
            <dd>{selected.credentialStatus || "—"}{selected.username ? ` · user ${selected.username}` : ""}</dd>
          </dl>

          {!readOnly && alignSources.length > 0 ? (
            <div className="istudio-form-grid" data-testid="istudio-align-connection">
              <div className="istudio-field istudio-field--full">
                <label htmlFor="istudio-align-source">Copy connection from working API</label>
                <select
                  id="istudio-align-source"
                  className="sila-select"
                  value={alignSourceId}
                  onChange={(event) => setAlignSourceId(event.target.value)}
                >
                  <option value="">Select a working connection</option>
                  {alignSources.map((row) => (
                    <option key={row.id} value={row.id}>
                      {row.name} · {statusLabel(row.status)} · {row.username || row.authenticationType}
                    </option>
                  ))}
                </select>
              </div>
              <div className="istudio-banner istudio-field--full">
                Copies base URL, protocol, auth method, protected credentials, timeout and OData headers from the
                source. Keeps this API’s name and process type. Material Document paths without
                /A_MaterialDocumentHeader are corrected automatically.
              </div>
              <div className="istudio-card__actions istudio-field--full">
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary"
                  disabled={!alignSourceId || aligning || busy}
                  onClick={alignFromWorking}
                >
                  {aligning ? "Copying…" : "Align connection settings"}
                </button>
              </div>
            </div>
          ) : null}

          {needsSecrets && !readOnly ? (
            <div className="istudio-form-grid" data-testid="istudio-test-credentials">
              <div className="istudio-field">
                <label htmlFor="istudio-test-auth">Authentication method</label>
                <select
                  id="istudio-test-auth"
                  className="sila-select"
                  value={authenticationType}
                  onChange={(event) => setAuthenticationType(event.target.value)}
                >
                  {INTEGRATION_AUTH_TYPES.map((item) => (
                    <option key={item.value} value={item.value}>{item.label}</option>
                  ))}
                </select>
              </div>

              {usesBasic ? (
                <>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-user">Username</label>
                    <input
                      id="istudio-test-user"
                      className="sila-input"
                      autoComplete="off"
                      value={username}
                      onChange={(event) => setUsername(event.target.value)}
                      placeholder="SAP technical user"
                    />
                  </div>
                  <div className="istudio-field istudio-field--full">
                    <label htmlFor="istudio-test-pass">Password</label>
                    <input
                      id="istudio-test-pass"
                      className="sila-input"
                      type="password"
                      autoComplete="new-password"
                      value={password}
                      onChange={(event) => setPassword(event.target.value)}
                      placeholder={secretPlaceholder(secretsConfigured)}
                    />
                  </div>
                </>
              ) : null}

              {usesApiKey ? (
                <>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-key-header">API key header</label>
                    <input
                      id="istudio-test-key-header"
                      className="sila-input"
                      value={apiKeyHeader}
                      onChange={(event) => setApiKeyHeader(event.target.value)}
                      placeholder="X-API-KEY"
                    />
                  </div>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-key">API key</label>
                    <input
                      id="istudio-test-key"
                      className="sila-input"
                      type="password"
                      autoComplete="new-password"
                      value={apiKey}
                      onChange={(event) => setApiKey(event.target.value)}
                      placeholder={secretPlaceholder(secretsConfigured)}
                    />
                  </div>
                </>
              ) : null}

              {usesBearer ? (
                <div className="istudio-field istudio-field--full">
                  <label htmlFor="istudio-test-bearer">Bearer token</label>
                  <input
                    id="istudio-test-bearer"
                    className="sila-input"
                    type="password"
                    autoComplete="new-password"
                    value={bearerToken}
                    onChange={(event) => setBearerToken(event.target.value)}
                    placeholder={secretPlaceholder(secretsConfigured)}
                  />
                </div>
              ) : null}

              {(usesOAuth || usesCustomToken) ? (
                <>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-client-id">Client ID</label>
                    <input
                      id="istudio-test-client-id"
                      className="sila-input"
                      autoComplete="off"
                      value={clientId}
                      onChange={(event) => setClientId(event.target.value)}
                    />
                  </div>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-client-secret">Client secret</label>
                    <input
                      id="istudio-test-client-secret"
                      className="sila-input"
                      type="password"
                      autoComplete="new-password"
                      value={clientSecret}
                      onChange={(event) => setClientSecret(event.target.value)}
                      placeholder={secretPlaceholder(secretsConfigured)}
                    />
                  </div>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-token-endpoint">Token endpoint</label>
                    <input
                      id="istudio-test-token-endpoint"
                      className="sila-input"
                      value={tokenEndpoint}
                      onChange={(event) => setTokenEndpoint(event.target.value)}
                    />
                  </div>
                  <div className="istudio-field">
                    <label htmlFor="istudio-test-token-scope">Token scope</label>
                    <input
                      id="istudio-test-token-scope"
                      className="sila-input"
                      value={tokenScope}
                      onChange={(event) => setTokenScope(event.target.value)}
                    />
                  </div>
                </>
              ) : null}

              <div className="istudio-field istudio-field--full">
                <label htmlFor="istudio-test-persist" className="istudio-checkbox">
                  <input
                    id="istudio-test-persist"
                    type="checkbox"
                    checked={persistCredentials}
                    onChange={(event) => setPersistCredentials(event.target.checked)}
                  />
                  {" "}Save credentials to the existing secure store before testing
                </label>
              </div>
            </div>
          ) : null}
        </>
      ) : (
        <div className="istudio-empty">Save a configuration first, then pass credentials here and run a safe connection test.</div>
      )}

      <div className="istudio-card__actions">
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          disabled={!configurationId || busy || readOnly}
          onClick={runSafeTest}
        >
          {busy
            ? (credentialsEntered ? "Saving credentials & testing…" : "Testing…")
            : (credentialsEntered ? "Save credentials & test" : "Run safe connection test")}
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
