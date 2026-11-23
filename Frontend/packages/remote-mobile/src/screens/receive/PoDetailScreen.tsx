import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  decidePurchaseOrder,
  getPurchaseOrder,
} from '../../../../remote-buyer/src/api/silaMe/silaReceivingApi';
import { formatQty } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import Card from '../../components/Card';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading, Notice, type NoticeMessage } from '../../components/StateViews';
import { formatDate, formatMoney } from '../../format';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import DocumentTrackCard from './DocumentTrackCard';

const PoDetailScreen: React.FC = () => {
  const { poId = '' } = useParams();
  const go = useGo();
  const { data, loading, error, reload } = useLoad(() => getPurchaseOrder(poId), poId || null);
  const [busy, setBusy] = useState(false);
  const [comment, setComment] = useState('');
  const [notice, setNotice] = useState<NoticeMessage | null>(null);
  const openLines = (data?.lines ?? []).filter((line) => line.openQty > 0);
  // Prototype "SERVICE PO": no line is a stocked material, so there is nothing to put in a store.
  const serviceOnly = (data?.lines ?? []).length > 0 && (data?.lines ?? []).every((line) => !line.materialId);
  const pendingApproval = (data?.status || '').toUpperCase() === 'PENDING_APPROVAL';
  const commentId = React.useId();

  const decide = async (approve: boolean) => {
    if (!data) return;
    setBusy(true);
    setNotice(null);
    try {
      const result = await decidePurchaseOrder(data.id, approve, comment);
      setNotice({
        tone: 'success',
        text: approve
          ? `Purchase order ${result.poNumber || data.poNumber} approved.${result.erpPurchaseOrderId ? ` SAP ${result.erpPurchaseOrderId}.` : ' It will post to SAP.'}`
          : `Purchase order ${result.poNumber || data.poNumber} rejected.`,
      });
      setComment('');
      reload();
    } catch (caught: unknown) {
      setNotice({ tone: 'error', text: errorText(caught) });
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <ScreenHeader title={data?.poNumber ?? 'Purchase order'} eyebrow="Purchase order" back />
      <div className="sm-screen">
        {loading && !data && <Loading />}
        {error && <ErrorNotice message={error} onRetry={reload} />}
        {!loading && !error && !data && <Empty text="Purchase order not found." />}
        <Notice notice={notice} />
        {data && (
          <>
            <Card title={data.supplierName ?? '—'} aside={<StatusBadge status={data.status} />}>
              <p className="sm-meta">
                Company {data.companyCode ?? '—'}
                {data.plantCode ? ` · Plant ${data.plantCode}` : ''}
              </p>
              <p className="sm-meta">
                Date {formatDate(data.orderDate)} · {data.currency ?? '—'} · Total {formatMoney(data.totalAmount, data.currency)}
              </p>
              {pendingApproval && (
                <p className="sm-meta">Waiting for approval before Create PO posts to SAP.</p>
              )}
            </Card>
            <DocumentTrackCard track={data.documentTrack} />
            {serviceOnly && <Empty text="SERVICE PO" description="None of its lines is a stocked material; it is not received into a store." />}
            <section className="sm-section" aria-labelledby="sm-po-lines">
              <h2 id="sm-po-lines">Items ({data.lines.length})</h2>
              <ul className="sm-list">
                {data.lines.map((line) => (
                  <li key={line.id} className="sm-card">
                    <strong className="sm-title">
                      {line.lineNumber} · {line.materialCode ?? 'No material code'}
                    </strong>
                    <span className="sm-meta">{line.productName}</span>
                    <span className="sm-meta">
                      Ordered {formatQty(line.orderedQty)} · Received {formatQty(line.receivedQty)} · Open {formatQty(line.openQty)} {line.uom ?? ''}
                    </span>
                    <span className="sm-meta">
                      {line.unitPrice !== null && line.unitPrice !== undefined ? `Unit ${formatMoney(line.unitPrice, data.currency)} · ` : ''}
                      {line.materialId ? 'Stocked material' : 'Not stocked'}
                    </span>
                  </li>
                ))}
              </ul>
            </section>

            {pendingApproval ? (
              <section className="sm-section sm-card" aria-labelledby="sm-po-approve" data-testid="po-approval-actions">
                <h2 id="sm-po-approve">Approve purchase order</h2>
                <p className="sm-meta">Approve to post Create PO to SAP, or reject to stop this draft.</p>
                <div className="sm-field">
                  <label htmlFor={commentId}>Comment (optional)</label>
                  <input
                    id={commentId}
                    className="sm-input"
                    value={comment}
                    maxLength={500}
                    disabled={busy}
                    onChange={(event) => setComment(event.target.value)}
                  />
                </div>
                <div className="sm-grid-2">
                  <button
                    type="button"
                    className="sm-btn sm-btn--success"
                    data-testid="po-approve"
                    disabled={busy}
                    onClick={() => void decide(true)}
                  >
                    {busy ? 'Saving…' : 'Approve'}
                  </button>
                  <button
                    type="button"
                    className="sm-btn sm-btn--danger"
                    data-testid="po-reject"
                    disabled={busy}
                    onClick={() => void decide(false)}
                  >
                    Reject
                  </button>
                </div>
                <button type="button" className="sm-btn sm-btn--block" disabled={busy} onClick={() => go('tasks?tab=approvals')}>
                  All purchase order approvals
                </button>
              </section>
            ) : (
              <button
                type="button"
                className="sm-btn sm-btn--primary sm-btn--block"
                disabled={openLines.length === 0}
                onClick={() => go(`receive/grn/new?poId=${encodeURIComponent(data.id)}`)}
              >
                {openLines.length === 0 ? 'Fully received' : 'Receive goods'}
              </button>
            )}
          </>
        )}
      </div>
    </>
  );
};

export default PoDetailScreen;
