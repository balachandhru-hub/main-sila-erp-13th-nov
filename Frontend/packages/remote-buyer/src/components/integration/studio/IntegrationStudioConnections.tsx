import React from "react";
import type { IntegrationConfiguration } from "../../../api/integrationsApi";
import { formatDateTime, statusBadgeClass, statusLabel } from "../integrationFormat";
import { isInventorySystem } from "./istudioFormat";

interface IntegrationStudioConnectionsProps {
  rows: IntegrationConfiguration[];
  readOnly?: boolean;
  onOpen: (configurationId: string) => void;
  onTest: (configurationId: string) => void;
  onCopy: (row: IntegrationConfiguration) => void;
  onDelete: (row: IntegrationConfiguration) => void;
  testingId?: string | null;
}

const IntegrationStudioConnections: React.FC<IntegrationStudioConnectionsProps> = ({
  rows,
  readOnly = false,
  onOpen,
  onTest,
  onCopy,
  onDelete,
  testingId,
}) => {
  if (rows.length === 0) {
    return <div className="istudio-empty">No saved API connections yet. Use the configuration wizard to add one.</div>;
  }

  return (
    <div className="istudio-grid-cards" data-testid="istudio-connections">
      {rows.map((row) => {
        const inventory = isInventorySystem(row);
        return (
          <article key={row.id} className="istudio-card">
            <div>
              <p className="istudio-kicker">{inventory ? "Inventory / SAP" : "Integration"}</p>
              <h3 className="istudio-card__title">{row.name}</h3>
            </div>
            <div className="istudio-card__meta">
              <span className={statusBadgeClass(row.status)}>{statusLabel(row.status)}</span>
              <span className="istudio-chip istudio-chip--muted">{row.protocol}</span>
              <span className="istudio-chip istudio-chip--muted">{row.authenticationType}</span>
            </div>
            <dl className="istudio-card__rows">
              <dt>System type</dt>
              <dd>{row.systemName || "—"}</dd>
              <dt>Environment / entity</dt>
              <dd>{row.entityCode || "—"}</dd>
              <dt>Base URL</dt>
              <dd>{row.baseUrl || "—"}</dd>
              <dt>Process</dt>
              <dd>{row.processType}</dd>
              <dt>Credentials</dt>
              <dd>{row.credentialStatus || "—"}</dd>
              <dt>Last test / activity</dt>
              <dd>
                {row.testedAt ? formatDateTime(row.testedAt) : "Not tested"}
                {row.lastSuccessfulRunAt ? ` · last success ${formatDateTime(row.lastSuccessfulRunAt)}` : ""}
              </dd>
              <dt>Last error</dt>
              <dd>{row.lastErrorSafe || "—"}</dd>
            </dl>
            <div className="istudio-card__actions">
              <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>
                {readOnly ? "View" : "Configure"}
              </button>
              {!readOnly ? (
                <>
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary sila-btn--sm"
                    onClick={() => onTest(row.id)}
                    disabled={testingId === row.id}
                  >
                    {testingId === row.id ? "Testing…" : "Test connection"}
                  </button>
                  <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onCopy(row)}>
                    Copy
                  </button>
                  <button type="button" className="sila-btn sila-btn--danger sila-btn--sm" onClick={() => onDelete(row)}>
                    Delete
                  </button>
                </>
              ) : null}
            </div>
          </article>
        );
      })}
    </div>
  );
};

export default IntegrationStudioConnections;
