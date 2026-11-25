import React from "react";
import { EmptyState } from "@vosox/shared-ui";
import type { SilaTransferEvent } from "../../../api/silaMe/silaMovementsApi";
import { formatDateTime } from "../../cart/lineFormat";
import { movementLabel } from "./movementFormat";

interface SilaTxnErpHistoryProps {
  erpStatus?: string | null;
  erpReference?: string | null;
  /** Optional SAP material document already known on the document (e.g. ITO). */
  sapMaterialDocument?: string | null;
  events?: SilaTransferEvent[] | null;
  /** When false, only the ERP banner is rendered. */
  showHistory?: boolean;
}

/** ERP success/failure banner + optional History timeline for opened inventory documents. */
const SilaTxnErpHistory: React.FC<SilaTxnErpHistoryProps> = ({
  erpStatus,
  erpReference,
  sapMaterialDocument,
  events,
  showHistory = true,
}) => {
  const status = (erpStatus ?? "").toUpperCase();
  const doc = (erpReference || sapMaterialDocument || "").trim();
  const erpOk = status === "POSTED" || Boolean(doc);
  const erpFailed = status.includes("FAIL") || status === "SKIPPED";
  const showErp = Boolean(status || doc);

  const timeline = Array.isArray(events) ? events : [];

  return (
    <>
      {showErp && (
        <div
          className={`sila-alert ${erpFailed ? "sila-alert--danger" : erpOk ? "sila-alert--success" : "sila-alert--info"}`}
          role="status"
          data-testid="sila-txn-erp-banner"
        >
          {erpOk && doc
            ? `Successful. SAP material document ${doc}.`
            : erpFailed
              ? `ERP ${movementLabel(status || "FAILED")}${doc ? ` (${doc})` : ""}.`
              : `ERP: ${movementLabel(status || "PENDING")}${doc ? ` · ${doc}` : ""}`}
        </div>
      )}

      {showHistory && (
        <section aria-labelledby="smov-txn-history-title">
          <h3 id="smov-txn-history-title" className="sila-section-title">History</h3>
          {timeline.length === 0 ? (
            <EmptyState title="No events yet" />
          ) : (
            <ol className="smov-timeline">
              {timeline.map((event, index) => (
                <li key={`${event.on}-${index}`} className="smov-event">
                  <span className="sila-cell-strong">{movementLabel(event.action)}</span>
                  <span className="smov-sub">{event.actorName} · {formatDateTime(event.on)}</span>
                  {event.comment && <span className="smov-event-comment">{event.comment}</span>}
                </li>
              ))}
            </ol>
          )}
        </section>
      )}
    </>
  );
};

export default SilaTxnErpHistory;
