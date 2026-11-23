import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  decidePurchaseOrder,
  getPurchaseOrder,
} from '../../../../remote-buyer/src/api/silaMe/silaReceivingApi';
import { reprocessPurchaseOrder } from '../../../../remote-buyer/src/api/purchaseOrderApi';
import { formatQty } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import ScreenHeader from '../../components/ScreenHeader';
import StatusBadge from '../../components/StatusBadge';
import { Empty, ErrorNotice, Loading, Notice, type NoticeMessage } from '../../components/StateViews';
import { formatDate, formatMoney } from '../../format';
import { useGo } from '../../navigation';
import { errorText, useLoad } from '../../useLoad';
import DocumentTrackCard from './DocumentTrackCard';

const fact = (label: string, value: React.ReactNode) => (
  <div className="sm-fact" key={label}>
    <dt>{label}</dt>
    <dd>{value ?? '—'}</dd>
  </div>
);

const PoDetailScreen: React.FC = () => {
  const { poId = '' } = useParams();
  const go = useGo();
  const { data, loading, error, reload } = useLoad(() => getPurchaseOrder(poId), poId || null);
  const [busy, setBusy] = useState(false);
  const [comment, setComment] = useState('');
  const [notice, setNotice] = useState<NoticeMessage | null>(null);
  const openLines = (data?.lines ?? []).filter((line) => line.openQty > 0);
  const serviceOnly = (data?.lines ?? []).length > 0 && (data?.lines ?? []).every((line) => !line.materialId);
  const status = (data?.status || '').toUpperCase();
  // Already decided or already in SAP — do not show active Approve.
  const awaitingApproval = status === 'PENDING_APPROVAL' && !data?.erpPurchaseOrderId;
  const alreadyDecided = !awaitingApproval;
  const canChange = Boolean(data && !data.erpPurchaseOrderId && alreadyDecided && status !== 'REJECTED');
  const commentId = React.useId();

  const decide = async (approve: boolean) => {
    if (!data || !awaitingApproval) return;
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

  const changePo = async () => {
    if (!data) return;
    setBusy(true);
    setNotice(null);
    try {
      await reprocessPurchaseOrder(data.id);
      setNotice({ tone: 'success', text: `Change sent for ${data.poNumber}. SAP will update when the post succeeds.` });
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
            {/* 1) Header */}
            <section className="sm-section" aria-labelledby="sm-po-header" data-testid="po-header">
              <div className="sm-row">
                <h2 id="sm-po-header">Header</h2>
                <StatusBadge status={data.status} />
              </div>
              <dl className="sm-facts">
                {fact('PO number', data.poNumber)}
                {fact('Supplier', data.supplierName)}
                {fact('Supplier code', data.supplierCode)}
                {fact('Entity / company', data.entityCode || data.companyCode)}
                {fact('Plant', data.plantCode)}
                {fact('Purchasing org', data.purchasingOrganization)}
                {fact('Purchasing group', data.purchasingGroup)}
                {fact('PO type', data.purchaseOrderType)}
                {fact('Storage location', data.storageLocation)}
                {fact('Account assignment', data.accountAssignmentCategory)}
                {fact('GL account', data.glAccount)}
                {fact('Currency', data.currency)}
                {fact('Order date', formatDate(data.orderDate))}
                {fact('Expected delivery', data.deliveryDate ? formatDate(data.deliveryDate) : '—')}
                {fact('Total amount', formatMoney(data.totalAmount, data.currency))}
                {fact('Ordered qty', data.totalOrderedQuantity != null ? data.totalOrderedQuantity.toLocaleString() : '—')}
                {fact('Received qty', data.totalReceivedQuantity != null ? data.totalReceivedQuantity.toLocaleString() : '—')}
                {fact('Open qty', data.totalOpenQuantity != null ? data.totalOpenQuantity.toLocaleString() : '—')}
                {fact(
                  'Open lines',
                  data.openLineCount != null ? `${data.openLineCount} / ${data.lineCount ?? data.lines.length}` : '—',
                )}
                {fact('Source system', data.sourceSystem)}
                {fact('Source type', data.sourceType)}
                {fact('SAP PO number', data.erpPurchaseOrderId || '—')}
                {fact('Supplier SO', data.supplierErpSalesOrderNumber)}
                {fact('Bucket', data.bucketCode)}
              </dl>
              {awaitingApproval && (
                <p className="sm-meta">Waiting for approval before Create PO posts to SAP.</p>
              )}
              {alreadyDecided && data.erpPurchaseOrderId ? (
                <p className="sm-meta" data-testid="po-sap-created">
                  Posted to SAP as {data.erpPurchaseOrderId}.
                </p>
              ) : null}
            </section>

            {/* 2) Line items */}
            <section className="sm-section" aria-labelledby="sm-po-lines" data-testid="po-lines">
              <h2 id="sm-po-lines">Line items ({data.lines.length})</h2>
              {serviceOnly && (
                <p className="sm-meta">SERVICE PO — none of its lines is a stocked material.</p>
              )}
              <ul className="sm-list">
                {data.lines.map((line) => (
                  <li key={line.id} className="sm-card">
                    <strong className="sm-title">
                      {line.itemNumber || line.lineNumber} · {line.productName || '—'}
                    </strong>
                    <span className="sm-meta">
                      {line.materialCode ? `Material ${line.materialCode}` : 'Text / no material code'}
                      {line.sku ? ` · SKU ${line.sku}` : ''}
                      {line.materialGroup ? ` · Group ${line.materialGroup}` : ''}
                    </span>
                    <span className="sm-meta">
                      Ordered {formatQty(line.orderedQty)} · Received {formatQty(line.receivedQty)} · Open{' '}
                      {formatQty(line.openQty)} {line.uom ?? ''}
                    </span>
                    <span className="sm-meta">
                      Unit {line.unitPrice != null ? formatMoney(line.unitPrice, data.currency) : '—'}
                      {line.discountPercent != null ? ` · Disc ${line.discountPercent}%` : ''}
                      {line.lineAmount != null ? ` · Line ${formatMoney(line.lineAmount, data.currency)}` : ''}
                    </span>
                    <span className="sm-meta">
                      Tax {line.taxCode || '—'}
                      {line.storageLocation ? ` · SLoc ${line.storageLocation}` : ''}
                      {line.costCenter ? ` · CC ${line.costCenter}` : ''}
                      {` · GR ${line.goodsReceiptExpected === false ? 'No' : 'Yes'}`}
                      {line.status ? ` · ${line.status}` : ''}
                      {!line.materialId ? ' · Not stocked' : ' · Stocked'}
                    </span>
                  </li>
                ))}
              </ul>
            </section>

            {/* 3) History / document track */}
            <DocumentTrackCard track={data.documentTrack} defaultExpanded />

            {/* 4) Actions */}
            <section className="sm-section" aria-labelledby="sm-po-actions" data-testid="po-approval-actions">
              <h2 id="sm-po-actions">Actions</h2>
              {awaitingApproval ? (
                <>
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
                </>
              ) : (
                <div className="sm-grid-2">
                  <button type="button" className="sm-btn sm-btn--success" data-testid="po-approve" disabled title="Already decided">
                    Approve
                  </button>
                  <button type="button" className="sm-btn sm-btn--danger" data-testid="po-reject" disabled title="Already decided">
                    Reject
                  </button>
                </div>
              )}
              <div className="sm-grid-2" style={{ marginTop: '0.65rem' }}>
                <button
                  type="button"
                  className="sm-btn"
                  data-testid="po-change"
                  disabled={busy || !canChange}
                  onClick={() => void changePo()}
                >
                  {busy ? 'Working…' : 'Change'}
                </button>
                <button
                  type="button"
                  className="sm-btn sm-btn--primary"
                  disabled={busy || awaitingApproval || openLines.length === 0}
                  onClick={() => go(`receive/grn/new?poId=${encodeURIComponent(data.id)}`)}
                >
                  {openLines.length === 0 ? 'Fully received' : 'Receive goods'}
                </button>
              </div>
              <button type="button" className="sm-btn sm-btn--block" disabled={busy} onClick={() => go('tasks?tab=approvals')}>
                All purchase order approvals
              </button>
            </section>
          </>
        )}
      </div>
    </>
  );
};

export default PoDetailScreen;
