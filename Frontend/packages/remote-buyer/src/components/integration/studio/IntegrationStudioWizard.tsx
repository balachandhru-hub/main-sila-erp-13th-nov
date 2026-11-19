import React, { useMemo, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  INTEGRATION_AUTH_TYPES,
  SAP_INVENTORY_CONNECTION,
  SAP_MOVEMENT_PROFILES,
  createIntegration,
  type IntegrationConfiguration,
  type IntegrationSide,
  type SapMovementProfile,
} from "../../../api/integrationsApi";
import { errorMessage } from "../integrationFormat";

const STEPS = [
  "System",
  "Authentication",
  "API services",
  "Business mapping",
  "Validate",
  "Activate",
] as const;

interface IntegrationStudioWizardProps {
  side?: IntegrationSide;
  initialProfile?: SapMovementProfile | null;
  onSaved: (configuration: IntegrationConfiguration) => void;
  onCancel: () => void;
}

const IntegrationStudioWizard: React.FC<IntegrationStudioWizardProps> = ({
  side = "buyer",
  initialProfile = null,
  onSaved,
  onCancel,
}) => {
  const [step, setStep] = useState(0);
  const [saving, setSaving] = useState(false);
  const [profileCode, setProfileCode] = useState(initialProfile?.profileCode || SAP_MOVEMENT_PROFILES[0].profileCode);
  const [name, setName] = useState(initialProfile?.configurationName || "");
  const [entityCode, setEntityCode] = useState("ALL");
  const [baseUrl, setBaseUrl] = useState<string>(SAP_INVENTORY_CONNECTION.baseUrl);
  const [description, setDescription] = useState(initialProfile?.purpose || "");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [authType, setAuthType] = useState("BASIC");
  const [resourcePath, setResourcePath] = useState(
    initialProfile?.resourcePath || `${SAP_INVENTORY_CONNECTION.serviceRoot}/A_MaterialDocumentHeader`,
  );
  const [plantHint, setPlantHint] = useState("");
  const [outletHint, setOutletHint] = useState("");
  const [storageHint, setStorageHint] = useState("");
  const [validated, setValidated] = useState(false);

  const profile = useMemo(
    () => SAP_MOVEMENT_PROFILES.find((item) => item.profileCode === profileCode) || SAP_MOVEMENT_PROFILES[0],
    [profileCode],
  );

  const blocked = profile.activationPolicy === "blocked_unverified";

  const applyProfile = (next: SapMovementProfile) => {
    setProfileCode(next.profileCode);
    setName(next.configurationName);
    setDescription(next.purpose);
    setResourcePath(next.resourcePath);
    setValidated(false);
  };

  const canNext = (): boolean => {
    if (step === 0) return Boolean(name.trim() && baseUrl.trim());
    if (step === 1) return authType === "NONE" || Boolean(username.trim());
    if (step === 2) return Boolean(resourcePath.trim());
    if (step === 4) return validated || blocked;
    return true;
  };

  const saveDraft = async () => {
    if (blocked) {
      toastService.error("This POS reversal profile cannot be saved as an active integration until SAP verification is complete.");
      return;
    }
    setSaving(true);
    try {
      const created = await createIntegration(side, {
        name: name.trim(),
        entityCode: entityCode.trim() || "ALL",
        processType: profile.processType,
        systemName: SAP_INVENTORY_CONNECTION.connectionName,
        protocol: SAP_INVENTORY_CONNECTION.protocol,
        baseUrl: baseUrl.trim().replace(/\/+$/, ""),
        resourcePath: resourcePath.trim().replace(/^\/+/, ""),
        httpMethod: profile.method,
        payloadFormat: "JSON",
        requestBody: null,
        headers: {
          Accept: SAP_INVENTORY_CONNECTION.accept,
          "Content-Type": SAP_INVENTORY_CONNECTION.contentType,
          "x-csrf-token": "Fetch",
        },
        authenticationType: authType,
        username: username.trim() || null,
        password: password || null,
        timeoutSeconds: 30,
        retryCount: 2,
      });
      toastService.success("Draft configuration saved. Activation does not post stock.");
      onSaved(created);
    } catch (err: unknown) {
      const message = errorMessage(err, "Could not save the draft configuration.");
      // One process type per entity code is enforced by the existing integration engine.
      toastService.error(
        /already exists/i.test(message)
          ? `${message} Open the existing ${profile.processType} API for this entity, or choose another company code. Named inventory posters still resolve by configuration name.`
          : message,
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <section className="istudio-panel" data-testid="istudio-wizard">
      <h3 className="istudio-panel__title">Connection configuration wizard</h3>
      <p className="istudio-panel__hint">
        Guided setup for {SAP_INVENTORY_CONNECTION.connectionName}. Passwords are write-only and never returned to the browser.
        Saving creates a draft only — no material posting.
      </p>

      <div className="istudio-steps">
        {STEPS.map((label, index) => (
          <span
            key={label}
            className="istudio-step"
            data-active={index === step ? "true" : undefined}
            data-done={index < step ? "true" : undefined}
          >
            {index + 1}. {label}
          </span>
        ))}
      </div>

      {blocked ? (
        <div className="istudio-banner istudio-banner--rose">
          Profile {profile.profileCode} is blocked. Confirm with SAP whether reversal should use Z01 or Cancel before activation.
        </div>
      ) : null}

      {step === 0 && (
        <div className="istudio-form-grid">
          <div className="istudio-field istudio-field--full">
            <label htmlFor="istudio-profile">SAP API profile</label>
            <select
              id="istudio-profile"
              className="sila-select"
              value={profileCode}
              onChange={(event) => {
                const next = SAP_MOVEMENT_PROFILES.find((item) => item.profileCode === event.target.value);
                if (next) applyProfile(next);
              }}
            >
              {SAP_MOVEMENT_PROFILES.map((item) => (
                <option key={item.id} value={item.profileCode}>{item.title} ({item.profileCode})</option>
              ))}
            </select>
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-name">Integration name</label>
            <input id="istudio-name" className="sila-input" value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-entity">Environment / entity</label>
            <input id="istudio-entity" className="sila-input" value={entityCode} onChange={(e) => setEntityCode(e.target.value)} />
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-system">System type</label>
            <input id="istudio-system" className="sila-input" value={SAP_INVENTORY_CONNECTION.systemType} readOnly />
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-connection">Connection name</label>
            <input id="istudio-connection" className="sila-input" value={SAP_INVENTORY_CONNECTION.connectionName} readOnly />
          </div>
          <div className="istudio-field istudio-field--full">
            <label htmlFor="istudio-base">Base URL</label>
            <input id="istudio-base" className="sila-input" value={baseUrl} onChange={(e) => setBaseUrl(e.target.value)} />
          </div>
          <div className="istudio-field istudio-field--full">
            <label htmlFor="istudio-desc">Description</label>
            <textarea id="istudio-desc" className="sila-input" rows={2} value={description} onChange={(e) => setDescription(e.target.value)} />
          </div>
        </div>
      )}

      {step === 1 && (
        <div className="istudio-form-grid">
          <div className="istudio-field">
            <label htmlFor="istudio-auth">Authentication method</label>
            <select id="istudio-auth" className="sila-select" value={authType} onChange={(e) => setAuthType(e.target.value)}>
              {INTEGRATION_AUTH_TYPES.map((item) => (
                <option key={item.value} value={item.value}>{item.label}</option>
              ))}
            </select>
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-user">Username</label>
            <input id="istudio-user" className="sila-input" autoComplete="off" value={username} onChange={(e) => setUsername(e.target.value)} />
          </div>
          <div className="istudio-field istudio-field--full">
            <label htmlFor="istudio-pass">Password / secret</label>
            <input
              id="istudio-pass"
              className="sila-input"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Stored securely — never shown again"
            />
          </div>
          <div className="istudio-banner istudio-banner--mint istudio-field--full">
            Reuses the existing IntegrationCredentialProtector. Secrets are not exposed to the frontend after save.
          </div>
        </div>
      )}

      {step === 2 && (
        <div className="istudio-form-grid">
          <div className="istudio-field">
            <label htmlFor="istudio-service">Service root</label>
            <input id="istudio-service" className="sila-input" value={SAP_INVENTORY_CONNECTION.serviceRoot} readOnly />
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-method">HTTP method</label>
            <input id="istudio-method" className="sila-input" value={profile.method} readOnly />
          </div>
          <div className="istudio-field istudio-field--full">
            <label htmlFor="istudio-path">Resource path</label>
            <input id="istudio-path" className="sila-input" value={resourcePath} onChange={(e) => setResourcePath(e.target.value)} />
          </div>
          <div className="istudio-field">
            <label>Required headers</label>
            <div className="istudio-banner">Accept / Content-Type application/json · x-csrf-token Fetch</div>
          </div>
          <div className="istudio-field">
            <label>Process type</label>
            <div className="istudio-banner">{profile.processType}</div>
          </div>
        </div>
      )}

      {step === 3 && (
        <div className="istudio-form-grid">
          <div className="istudio-field">
            <label htmlFor="istudio-plant">Plant (mapping note)</label>
            <input id="istudio-plant" className="sila-input" value={plantHint} onChange={(e) => setPlantHint(e.target.value)} placeholder="Resolved from outlet mapping" />
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-outlet">Outlet</label>
            <input id="istudio-outlet" className="sila-input" value={outletHint} onChange={(e) => setOutletHint(e.target.value)} placeholder="e.g. FL10 — not hardcoded" />
          </div>
          <div className="istudio-field">
            <label htmlFor="istudio-sloc">Storage location</label>
            <input id="istudio-sloc" className="sila-input" value={storageHint} onChange={(e) => setStorageHint(e.target.value)} placeholder="From existing outlet mapping" />
          </div>
          <div className="istudio-field">
            <label>Movement code / type</label>
            <div className="istudio-banner">
              Code {profile.goodsMovementCode || "—"} · Type {profile.goodsMovementType || "—"}
            </div>
          </div>
          <div className="istudio-banner istudio-field--full">
            Business mapping notes are stored with the draft for operators. Live plant/SLoc resolution continues to use
            existing outlet and location masters — never invent material IDs or cost centers here.
          </div>
        </div>
      )}

      {step === 4 && (
        <div className="srec-stack">
          <div className="istudio-banner">
            Safe validation only: host connectivity, authentication, CSRF token acquisition, and metadata checks happen
            after the draft is saved via Test connection. This step does not post goods movements.
          </div>
          <ul className="pstudio-list">
            <li className="pstudio-list__item"><div><strong>Hostname</strong><span className="srec-sub">{baseUrl}</span></div><span>User-supplied</span></li>
            <li className="pstudio-list__item"><div><strong>Resource</strong><span className="srec-sub">{resourcePath}</span></div><span>{profile.method}</span></li>
            <li className="pstudio-list__item"><div><strong>Mapping warnings</strong><span className="srec-sub">{profile.statusNote}</span></div><span>{profile.activationPolicy}</span></li>
          </ul>
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setValidated(true)}>
            Mark validation checklist reviewed
          </button>
          {validated ? <div className="istudio-banner istudio-banner--mint">Checklist reviewed. Save draft, then use Test connection.</div> : null}
        </div>
      )}

      {step === 5 && (
        <div className="srec-stack">
          <dl className="istudio-card__rows">
            <dt>Configuration summary</dt>
            <dd>{name} · {profile.profileCode} · {SAP_INVENTORY_CONNECTION.connectionName}</dd>
            <dt>Activation status</dt>
            <dd>Draft save only. Activate later from the workspace after a successful connection test. Activation does not execute a material posting.</dd>
            <dt>Permission check</dt>
            <dd>Requires existing Integration administrator access. ERP schedulers remain PARKED.</dd>
          </dl>
        </div>
      )}

      <div className="istudio-card__actions">
        <button type="button" className="sila-btn sila-btn--ghost" onClick={onCancel}>Cancel</button>
        {step > 0 ? (
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setStep((value) => value - 1)}>Back</button>
        ) : null}
        {step < STEPS.length - 1 ? (
          <button type="button" className="sila-btn sila-btn--primary" disabled={!canNext()} onClick={() => setStep((value) => value + 1)}>
            Continue
          </button>
        ) : (
          <button type="button" className="sila-btn sila-btn--primary" disabled={saving || blocked || !validated} onClick={saveDraft}>
            {saving ? "Saving…" : "Save draft configuration"}
          </button>
        )}
      </div>
    </section>
  );
};

export default IntegrationStudioWizard;
