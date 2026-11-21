import React from "react";
import type { SilaPosImportResult } from "../../../api/silaMe/silaPosApi";

interface SilaPosImportSummaryProps {
  result: SilaPosImportResult;
  onClose: () => void;
}

/** Counts of the last upload, pull, or confirm-posting run (local → SAP → tracker). */
const SilaPosImportSummary: React.FC<SilaPosImportSummaryProps> = ({ result, onClose }) => {
  const unknown = result.postingUnknown ?? 0;
  const localUpdated = result.localUpdated ?? result.processed;
  const sapPosted = result.sapPosted ?? 0;
  const sapFailed = result.sapFailed ?? 0;
  const outcome =
    result.failed > 0 && result.processed > 0
      ? "Partially completed"
      : result.failed > 0
        ? "Failed"
        : unknown > 0
          ? "Pending reconciliation"
          : sapPosted > 0
            ? "Local updated · SAP posted · Tracker ready"
            : result.processed > 0
              ? "Local updated (SAP pending)"
              : "Received";

  return (
    <section className="sila-card pstudio-panel" aria-live="polite" data-testid="pstudio-import-summary">
      <div className="sila-card-header">
        <h2 className="sila-card-title">Batch {result.batchNumber}</h2>
        <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={onClose} aria-label="Close the import summary">
          Close
        </button>
      </div>
      <div className="sila-card-body srec-stack">
        <div className="pstudio-kpis">
          <div className="pstudio-kpi pstudio-kpi--sky"><span>Selected / accepted</span><strong>{result.accepted}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--mint"><span>Step 1 · Local updated</span><strong>{localUpdated}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--lavender"><span>Step 2 · SAP posted</span><strong>{sapPosted}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--rose"><span>Failed</span><strong>{result.failed + sapFailed}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--amber"><span>Pending reconciliation</span><strong>{unknown}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--blue"><span>Outcome</span><strong>{outcome}</strong></div>
        </div>
        {unknown > 0 && (
          <div className="sila-alert sila-alert--warning" role="status">
            SAP posting may have succeeded for some lines while SILA reconciliation remains pending — not marked as general Success.
          </div>
        )}
        {result.failed > 0 && (
          <div className="sila-alert sila-alert--warning" role="status">
            {result.failed} line{result.failed === 1 ? "" : "s"} failed matching or deduction. Open the Transaction Tracker to see why and reprocess.
          </div>
        )}
        {sapFailed > 0 && (
          <div className="sila-alert sila-alert--warning" role="status">
            {sapFailed} line{sapFailed === 1 ? "" : "s"} failed at the SAP API. Open the Transaction Tracker for HTTP status, payloads, and reprocess.
          </div>
        )}
        {unknown > 0 && (
          <div className="sila-alert sila-alert--warning" role="status">
            {unknown} line{unknown === 1 ? "" : "s"} got no clear answer from the ERP. Reconcile them under ERP postings before reprocessing.
          </div>
        )}
        {result.errors.length > 0 && (
          <div className="sila-alert sila-alert--danger" role="alert">
            <div className="srec-stack">
              <span className="sila-alert-title">Invalid lines (not stored)</span>
              <div className="srec-error-list">
                {result.errors.map((error) => (
                  <span key={`${error.row}-${error.message}`}>Row {error.row}: {error.message}</span>
                ))}
              </div>
            </div>
          </div>
        )}
      </div>
    </section>
  );
};

export default SilaPosImportSummary;
