import React from "react";
import type { SilaPosImportResult } from "../../../api/silaMe/silaPosApi";

interface SilaPosImportSummaryProps {
  result: SilaPosImportResult;
  onClose: () => void;
}

/** Counts of the last upload or pull, with the reasons of the invalid lines. */
const SilaPosImportSummary: React.FC<SilaPosImportSummaryProps> = ({ result, onClose }) => {
  const unknown = result.postingUnknown ?? 0;
  const outcome =
    result.failed > 0 && result.processed > 0
      ? "Partially completed"
      : result.failed > 0
        ? "Failed"
        : unknown > 0
          ? "Pending reconciliation"
          : result.processed > 0
            ? "SILA posted (check SAP separately)"
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
          <div className="pstudio-kpi pstudio-kpi--mint"><span>Successfully processed</span><strong>{result.processed}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--rose"><span>Failed</span><strong>{result.failed}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--amber"><span>Pending reconciliation</span><strong>{unknown}</strong></div>
          <div className="pstudio-kpi pstudio-kpi--lavender"><span>Outcome</span><strong>{outcome}</strong></div>
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
