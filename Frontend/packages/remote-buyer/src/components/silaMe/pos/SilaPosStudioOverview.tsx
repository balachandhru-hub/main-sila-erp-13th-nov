import React, { useEffect, useState } from "react";
import { EmptyState, Loader } from "@vosox/shared-ui";
import { getPosBatches, getPosTransactions, type SilaPosBatch } from "../../../api/silaMe/silaPosApi";
import { errorText } from "./posFormat";

interface SilaPosStudioOverviewProps {
  onOpenUpload: () => void;
  onOpenBatches: () => void;
  onOpenExceptions: () => void;
  onOpenTracker: () => void;
}

/** Overview KPIs and attention cards from live POS batch / transaction APIs. */
const SilaPosStudioOverview: React.FC<SilaPosStudioOverviewProps> = ({
  onOpenUpload,
  onOpenBatches,
  onOpenExceptions,
  onOpenTracker,
}) => {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [batches, setBatches] = useState<SilaPosBatch[]>([]);
  const [counts, setCounts] = useState({
    transactions: 0,
    failed: 0,
    deducted: 0,
    posted: 0,
    unknown: 0,
  });

  useEffect(() => {
    let active = true;
    (async () => {
      setLoading(true);
      setError(null);
      try {
        const [batchRows, failedPage, deductedPage, postedPage, unknownPage, allPage] = await Promise.all([
          getPosBatches(0, 12),
          getPosTransactions({ status: "FAILED", index: 0, limit: 1 }),
          getPosTransactions({ status: "INVENTORY_DEDUCTED", index: 0, limit: 1 }),
          getPosTransactions({ status: "POSTED", index: 0, limit: 1 }),
          getPosTransactions({ postingStatus: "UNKNOWN", index: 0, limit: 1 }),
          getPosTransactions({ index: 0, limit: 1 }),
        ]);
        if (!active) return;
        setBatches(batchRows);
        setCounts({
          transactions: allPage.total ?? 0,
          failed: failedPage.total ?? 0,
          deducted: deductedPage.total ?? 0,
          posted: postedPage.total ?? 0,
          unknown: unknownPage.total ?? 0,
        });
      } catch (err: unknown) {
        if (!active) return;
        setError(errorText(err, "Could not load POS overview."));
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, []);

  const uploadedLines = batches.reduce((sum, batch) => sum + (batch.rows || 0), 0);
  const validLines = batches.reduce((sum, batch) => sum + (batch.accepted || 0), 0);
  const pendingBatches = batches.filter((batch) => batch.status === "PREVIEW");
  const recent = batches.slice(0, 5);

  if (loading) return <Loader size={20} message="Loading POS overview..." />;
  if (error) {
    return (
      <EmptyState
        variant="error"
        title="Overview unavailable"
        description={error}
      />
    );
  }

  return (
    <div className="srec-stack">
      <div className="pstudio-kpis" data-testid="pstudio-overview-kpis">
        <div className="pstudio-kpi pstudio-kpi--blue"><span>POS transactions</span><strong>{counts.transactions}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--sky"><span>Uploaded lines (recent batches)</span><strong>{uploadedLines}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--mint"><span>Valid / accepted lines</span><strong>{validLines}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--amber"><span>Exceptions (failed)</span><strong>{counts.failed}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--lavender"><span>Pending posting (deducted)</span><strong>{counts.deducted}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--mint"><span>SAP posted (tracker)</span><strong>{counts.posted}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--amber"><span>SILA recon / ERP unknown</span><strong>{counts.unknown}</strong></div>
        <div className="pstudio-kpi pstudio-kpi--sky"><span>Preview batches waiting</span><strong>{pendingBatches.length}</strong></div>
      </div>

      <div className="pstudio-grid-2">
        <section className="pstudio-panel pstudio-panel--mint">
          <h3 className="pstudio-panel__title">Recent uploads</h3>
          <p className="pstudio-panel__hint">Live batch history from POS file / API ingestion.</p>
          {recent.length === 0 ? (
            <div className="pstudio-empty">No uploads yet. Start with Upload Sales.</div>
          ) : (
            <ul className="pstudio-list">
              {recent.map((batch) => (
                <li key={batch.id} className="pstudio-list__item">
                  <div>
                    <strong>{batch.batchNumber}</strong>
                    <span className="srec-sub">{batch.fileName || batch.source} · {batch.status}</span>
                  </div>
                  <span>{batch.accepted}/{batch.rows} accepted</span>
                </li>
              ))}
            </ul>
          )}
          <div className="srec-actions">
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={onOpenBatches}>Upload history</button>
            <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={onOpenUpload}>Upload sales</button>
          </div>
        </section>

        <section className="pstudio-panel pstudio-panel--amber">
          <h3 className="pstudio-panel__title">Attention required</h3>
          <p className="pstudio-panel__hint">Failed sales and ERP outcomes that need a decision.</p>
          <ul className="pstudio-list">
            <li className="pstudio-list__item">
              <div><strong>Failed POS lines</strong><span className="srec-sub">Match / deduct / post failures</span></div>
              <span>{counts.failed}</span>
            </li>
            <li className="pstudio-list__item">
              <div><strong>ERP unknown</strong><span className="srec-sub">Reconcile under ERP postings / tracker</span></div>
              <span>{counts.unknown}</span>
            </li>
            <li className="pstudio-list__item">
              <div><strong>Preview batches</strong><span className="srec-sub">Validated but not posted</span></div>
              <span>{pendingBatches.length}</span>
            </li>
          </ul>
          <div className="srec-actions">
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={onOpenExceptions}>Exceptions</button>
            <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={onOpenTracker}>Transaction tracker</button>
          </div>
        </section>
      </div>
    </div>
  );
};

export default SilaPosStudioOverview;
