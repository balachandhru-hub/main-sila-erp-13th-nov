import React from 'react';
import { useParams } from 'react-router-dom';
import { silaLabel } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import { getAdjustment } from '../../../../remote-buyer/src/api/silaMe/silaMovementsApi';
import ScreenHeader from '../../components/ScreenHeader';
import { ErrorNotice, Loading } from '../../components/StateViews';
import { formatDate, formatDateTime } from '../../format';
import { useLoad } from '../../useLoad';

const AdjustmentDetailScreen: React.FC = () => {
  const { adjustmentId = '' } = useParams();
  const { data, loading, error, reload } = useLoad(() => getAdjustment(adjustmentId), adjustmentId ? `adj-${adjustmentId}` : null);
  const events = data?.events ?? [];
  const erpOk = (data?.erpStatus ?? '').toUpperCase() === 'POSTED' || Boolean(data?.erpReference);
  const erpFailed = (data?.erpStatus ?? '').toUpperCase().includes('FAIL');

  return (
    <>
      <ScreenHeader title={data?.adjustmentNumber ?? 'Adjustment'} eyebrow="Inventory" back />
      <div className="sm-screen" data-testid="mobile-adjustment-detail">
        {loading && <Loading />}
        {error && <ErrorNotice message={error} onRetry={reload} />}
        {data && (
          <>
            <p className="sm-meta">
              {silaLabel(data.adjustmentType)} · {data.locationName ?? '—'} · {formatDate(data.postedOn)}
            </p>
            {data.reason && <p>{data.reason}</p>}
            {(data.erpReference || data.erpStatus) && (
              <p className={erpFailed ? 'sm-notice sm-notice--error' : erpOk ? 'sm-notice sm-notice--success' : 'sm-meta'}>
                {erpOk && data.erpReference
                  ? `Successful. SAP material document ${data.erpReference}.`
                  : erpFailed
                    ? `ERP failed${data.erpReference ? ` (${data.erpReference})` : ''}.`
                    : `ERP: ${silaLabel(data.erpStatus ?? 'PENDING')}${data.erpReference ? ` · ${data.erpReference}` : ''}`}
              </p>
            )}
            <ul className="sm-list">
              {data.items.map((item) => (
                <li key={item.id} className="sm-card">
                  <strong>{item.materialName}</strong>
                  <span className="sm-meta">
                    {item.materialCode} · {item.quantity} {item.uom}
                  </span>
                </li>
              ))}
            </ul>

            <section className="sm-section" aria-labelledby="sm-adj-events">
              <h2 id="sm-adj-events">History</h2>
              {events.length === 0 ? (
                <p className="sm-muted">No events yet.</p>
              ) : (
                <ol className="sm-timeline">
                  {events.map((event, index) => (
                    <li key={`${event.on}-${index}`}>
                      <strong>{silaLabel(event.action)}</strong>
                      <p className="sm-muted">
                        {event.actorName} · {formatDateTime(event.on)}
                      </p>
                      {event.comment && <p>{event.comment}</p>}
                    </li>
                  ))}
                </ol>
              )}
            </section>
          </>
        )}
      </div>
    </>
  );
};

export default AdjustmentDetailScreen;
