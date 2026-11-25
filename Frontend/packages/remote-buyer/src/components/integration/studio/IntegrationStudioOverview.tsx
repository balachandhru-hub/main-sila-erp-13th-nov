import React from "react";
import {
  SAP_INVENTORY_CONNECTION,
  SAP_MOVEMENT_PROFILES,
  matchSapMovementProfile,
  type IntegrationConfiguration,
  type IntegrationExecution,
} from "../../../api/integrationsApi";
import { formatDateTime, statusLabel } from "../integrationFormat";
import { CONNECTED_STATUSES, activationLabel, isInventorySystem } from "./istudioFormat";

interface IntegrationStudioOverviewProps {
  rows: IntegrationConfiguration[];
  executions: IntegrationExecution[];
  onOpenConnections: () => void;
  onOpenWizard: () => void;
  onOpenMonitoring: () => void;
}

const IntegrationStudioOverview: React.FC<IntegrationStudioOverviewProps> = ({
  rows,
  executions,
  onOpenConnections,
  onOpenWizard,
  onOpenMonitoring,
}) => {
  const inventoryRows = rows.filter(isInventorySystem);
  const active = rows.filter((row) => row.status === "ACTIVE").length;
  const connected = rows.filter((row) => CONNECTED_STATUSES.includes(row.status)).length;
  const errors = rows.filter((row) => {
    const last = (row.lastErrorSafe || "").trim();
    return Boolean(last) || row.status === "ERROR" || row.status === "FAILED";
  }).length;
  const failedExec = executions.filter((item) => item.status === "FAILED" || item.status === "ERROR").length;
  const lastSuccess = [...executions]
    .filter((item) => item.status === "SUCCESS" || item.status === "SUCCEEDED")
    .sort((a, b) => new Date(b.completedAt || b.startedAt).getTime() - new Date(a.completedAt || a.startedAt).getTime())[0];

  const profileStates = SAP_MOVEMENT_PROFILES.map((profile) => ({
    profile,
    configured: matchSapMovementProfile(profile, rows),
  }));
  const attention = profileStates.filter(({ profile, configured }) =>
    profile.activationPolicy === "blocked_unverified"
    || profile.activationPolicy === "validation_required"
    || !configured
    || Boolean(configured?.lastErrorSafe));
  const inventorySystemPresent = inventoryRows.some(
    (row) => (row.systemName || "").toUpperCase().includes(SAP_INVENTORY_CONNECTION.connectionName),
  );

  return (
    <div className="srec-stack" data-testid="istudio-overview">
      <div className="istudio-kpis">
        <div className="istudio-kpi istudio-kpi--blue"><span>Total connections</span><strong>{rows.length}</strong></div>
        <div className="istudio-kpi istudio-kpi--mint"><span>Active integrations</span><strong>{active}</strong></div>
        <div className="istudio-kpi istudio-kpi--sky"><span>Connected (tested)</span><strong>{connected}</strong></div>
        <div className="istudio-kpi istudio-kpi--rose"><span>Connection errors</span><strong>{errors}</strong></div>
        <div className="istudio-kpi istudio-kpi--lavender"><span>SAP inventory APIs</span><strong>{inventoryRows.length}</strong></div>
        <div className="istudio-kpi istudio-kpi--amber"><span>Failed transactions</span><strong>{failedExec}</strong></div>
        <div className="istudio-kpi istudio-kpi--mint"><span>Last successful sync</span><strong>{lastSuccess ? formatDateTime(lastSuccess.completedAt || lastSuccess.startedAt) : "—"}</strong></div>
        <div className="istudio-kpi istudio-kpi--amber"><span>Attention required</span><strong>{attention.length}</strong></div>
      </div>

      <div className="istudio-grid-2">
        <section className="istudio-panel istudio-panel--lavender">
          <h3 className="istudio-panel__title">SAP inventory connection</h3>
          <p className="istudio-panel__hint">
            Shared Material Document service for POS and stock movements. Credentials stay in the existing secure store.
          </p>
          <dl className="istudio-card__rows">
            <dt>Connection name</dt>
            <dd>{SAP_INVENTORY_CONNECTION.connectionName}</dd>
            <dt>System type</dt>
            <dd>{SAP_INVENTORY_CONNECTION.systemType}</dd>
            <dt>Base URL</dt>
            <dd>{SAP_INVENTORY_CONNECTION.baseUrl}</dd>
            <dt>Service root</dt>
            <dd>{SAP_INVENTORY_CONNECTION.serviceRoot}</dd>
            <dt>Protocol</dt>
            <dd>{SAP_INVENTORY_CONNECTION.protocol}</dd>
            <dt>Studio status</dt>
            <dd>{inventorySystemPresent ? "Referenced on at least one saved API" : "Ready to configure — no live posting on save"}</dd>
          </dl>
          <div className="istudio-card__actions">
            <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={onOpenWizard}>Configure wizard</button>
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={onOpenConnections}>View connections</button>
          </div>
        </section>

        <section className="istudio-panel istudio-panel--amber">
          <h3 className="istudio-panel__title">Attention required</h3>
          <p className="istudio-panel__hint">Unverified mappings, missing APIs, and connection test failures.</p>
          {attention.length === 0 ? (
            <div className="istudio-empty">No attention items right now.</div>
          ) : (
            <ul className="pstudio-list">
              {attention.slice(0, 6).map(({ profile, configured }) => {
                const badge = activationLabel(profile.activationPolicy, configured);
                return (
                  <li key={profile.id} className="pstudio-list__item">
                    <div>
                      <strong>{profile.title}</strong>
                      <span className="srec-sub">{configured ? statusLabel(configured.status) : "Not configured"}</span>
                    </div>
                    <span className={badge.className}>{badge.text}</span>
                  </li>
                );
              })}
            </ul>
          )}
          <div className="istudio-card__actions">
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={onOpenConnections}>View connections</button>
            <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={onOpenMonitoring}>Transaction tracker</button>
          </div>
        </section>
      </div>

      <section className="istudio-panel">
        <h3 className="istudio-panel__title">Preserved integrations</h3>
        <p className="istudio-panel__hint">
          Existing GRN 101/102, invoice, supplier, and material master configurations remain on the shared integration engine.
          ERP schedulers stay PARKED. Activation never posts a material document.
        </p>
        <div className="istudio-banner istudio-banner--mint">
          Reuses IntegrationHttpExecutor, CSRF handshake, credential protector, SendOnceAsync posting, and Transaction Tracker.
          No parallel SAP engine.
        </div>
      </section>
    </div>
  );
};

export default IntegrationStudioOverview;
