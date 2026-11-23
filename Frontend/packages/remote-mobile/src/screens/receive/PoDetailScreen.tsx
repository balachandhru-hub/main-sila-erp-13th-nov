import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  decidePurchaseOrder,
  getPurchaseOrder,
} from '../../../../remote-buyer/src/api/silaMe/silaReceivingApi';
import { reprocessPurchaseOrder } from '../../../../remote-buyer/src/api/purchaseOrderApi';
import { formatQty } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import Card from '../../components/Card';
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
  // Prototype "SERVICE PO": no line is a stocked material, so there is nothing to put in a store.
  const serviceOnly = (data?.lines ?? []).length > 0 && (data?.lines ?? []).every((line) => !line.materialId);
  const pendingApproval = (data?.status || '').toUpperCase() === 'PENDING_APPROVAL';
  const created = (data?.status || '').toUpperCase() === 'CREATED' || (data?.status || '').toUpperCase() === 'APPROVED';
  const canChange = Boolean(data && !data.erpPurchaseOrderId && !pendingApproval);
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
            <Card title={data.supplierName ?? '—'} aside={<StatusBadge status={data.status} />}>
              <p className="sm-meta">
                {data.erpPurchaseOrderId ? `SAP ${data.erpPurchaseOrderId}` : 'No SAP PO yet'}
                {data.supplierCode ? ` · Vendor ${data.supplierCode}` : ''}
              </p>
              {pendingApproval && (
                <p className="sm-meta">Waiting for approval before Create PO posts to SAP.</p>
              )}
            </Card>

            <section className="sm-section sm-card" aria-labelledby="sm-po-header">
              <h2 id="sm-po-header">Header</h2>
              <dl className="sm-facts">
                {fact('PO number', data.poNumber)}
                {fact('Status', <StatusBadge status={data.status} />)}
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
            </section>

            <DocumentTrackCard track={data.documentTrack} defaultExpanded />

            {serviceOnly && (
              <Empty text="SERVICE PO" description="None of its lines is a stocked material; it is not received into a store." />
            )}

            <section className="sm-section" aria-labelledby="sm-po-lines">
              <h2 id="sm-po-lines">Line items ({data.lines.length})</h2>
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

            <div className="sm-actions sm-grid-2" style={{ marginBottom: '0.75rem' }}>
              <button
                type="button"
                className="sm-btn"
                data-testid="po-change"
                disabled={busy || !canChange}
                onClick={() => void changePo()}
                title={
                  data.erpPurchaseOrderId
                    ? 'SAP PO already exists — change is not available here.'
                    : pendingApproval
                      ? 'Approve the PO before changing / posting to SAP.'
                      : 'Re-send this purchase order to SAP.'
                }
              >
                {busy ? 'Working…' : 'Change'}
              </button>
              {!pendingApproval && (
                <button
                  type="button"
                  className="sm-btn sm-btn--primary"
                  disabled={busy || openLines.length === 0}
                  onClick={() => go(`receive/grn/new?poId=${encodeURIComponent(data.id)}`)}
                >
                  {openLines.length === 0 ? 'Fully received' : 'Receive goods'}
                </button>
              )}
            </div>

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
            ) : null}

            {created && data.erpPurchaseOrderId ? (
              <p className="sm-meta" data-testid="po-sap-created">
                Posted to SAP as {data.erpPurchaseOrderId}.
              </p>
            ) : null}
          </>
        )}
      </div>
    </>
  );
};

export default PoDetailScreen;
