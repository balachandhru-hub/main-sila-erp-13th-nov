import React from "react";
import type { IntegrationConfiguration, IntegrationExecution } from "../../../api/integrationsApi";
import { formatDateTime, statusBadgeClass, statusLabel } from "../integrationFormat";

interface IntegrationStudioMonitoringProps {
  rows: IntegrationConfiguration[];
  executions: IntegrationExecution[];
  onOpenTracker: () => void;
  onOpenConfiguration: (configurationId: string) => void;
}

const MOVEMENT_LABELS: { match: RegExp; label: string }[] = [
  { match: /GRN REVERSAL|POST_GRN_REVERSAL/i, label: "GRN Reversal" },
  { match: /GRN|POST_GRN/i, label: "GRN Posting" },
  { match: /POS_UPDATE|UPDATE_STOCK|POS Sales/i, label: "POS Consumption" },
  { match: /POS_CONSUMPTION_REVERSAL|Return/i, label: "POS Reversal" },
  { match: /Positive Stock Adjustment/i, label: "Positive Stock Adjustment" },
  { match: /Goods Issue/i, label: "Stock Issue" },
  { match: /Transfer Stock(?!.*Plant)/i, label: "Storage Location Transfer" },
  { match: /Plant Transfer/i, label: "Plant Transfer" },
];

const movementLabel = (row: IntegrationConfiguration): string => {
  const haystack = `${row.name} ${row.processType}`;
  return MOVEMENT_LABELS.find((item) => item.match.test(haystack))?.label || row.processType;
};

const IntegrationStudioMonitoring: React.FC<IntegrationStudioMonitoringProps> = ({
  rows,
  executions,
  onOpenTracker,
  onOpenConfiguration,
}) => {
  const recent = [...executions]
    .sort((a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime())
    .slice(0, 20);

  return (
    <div className="srec-stack" data-testid="istudio-monitoring">
      <section className="istudio-panel istudio-panel--lavender">
        <h3 className="istudio-panel__title">Integration monitoring</h3>
        <p className="istudio-panel__hint">
          Reuses the existing Transaction Tracker and integration execution history. No second tracker is created.
        </p>
        <div className="istudio-card__actions">
          <button type="button" className="sila-btn sila-btn--primary" onClick={onOpenTracker}>
            Open SILA ME Transaction Tracker
          </button>
        </div>
      </section>

      <section className="istudio-panel">
        <h3 className="istudio-panel__title">Configured movement APIs</h3>
        <div className="istudio-table-wrap">
          <table className="istudio-table">
            <thead>
              <tr>
                <th scope="col">Transaction type</th>
                <th scope="col">SAP connection</th>
                <th scope="col">API profile</th>
                <th scope="col">Status</th>
                <th scope="col">Last error</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {rows.length === 0 ? (
                <tr><td colSpan={6}>No integrations saved yet.</td></tr>
              ) : rows.map((row) => (
                <tr key={row.id}>
                  <td>{movementLabel(row)}</td>
                  <td>{row.systemName || "—"}</td>
                  <td>{row.name}<span className="srec-sub">{row.processType}</span></td>
                  <td><span className={statusBadgeClass(row.status)}>{statusLabel(row.status)}</span></td>
                  <td>{row.lastErrorSafe || "—"}</td>
                  <td>
                    <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpenConfiguration(row.id)}>
                      Drill down
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="istudio-panel">
        <h3 className="istudio-panel__title">Recent integration executions</h3>
        <p className="istudio-panel__hint">Source reference, posting status, and retry eligibility remain on the Transaction Tracker detail screens.</p>
        <div className="istudio-table-wrap">
          <table className="istudio-table">
            <thead>
              <tr>
                <th scope="col">Started</th>
                <th scope="col">Trigger</th>
                <th scope="col">Status</th>
                <th scope="col">Records</th>
                <th scope="col">Error</th>
              </tr>
            </thead>
            <tbody>
              {recent.length === 0 ? (
                <tr><td colSpan={5}>No execution history yet.</td></tr>
              ) : recent.map((item) => (
                <tr key={item.id}>
                  <td>{formatDateTime(item.startedAt)}</td>
                  <td>{item.trigger}</td>
                  <td><span className={statusBadgeClass(item.status)}>{statusLabel(item.status)}</span></td>
                  <td>{item.recordsRead}/{item.recordsCreated}/{item.recordsFailed}</td>
                  <td>{item.errorMessageSafe || item.errorCode || "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};

export default IntegrationStudioMonitoring;
