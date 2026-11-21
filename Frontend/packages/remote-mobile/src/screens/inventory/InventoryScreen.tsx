import React, { useState } from 'react';
import { getInventoryHome, getPendingOutletRequirements } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import ScreenHeader from '../../components/ScreenHeader';
import { Empty, ErrorNotice, OfflineBanner } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import './liveInventoryMobile.css';

interface QuickAction {
  label: string;
  detail: string;
  to: string;
  visible?: (can: ReturnType<typeof useAccess>['can']) => boolean;
}

const QUICK: QuickAction[] = [
  { label: 'Find Stock', detail: 'Search or scan materials', to: 'inventory/stock' },
  { label: 'Transfer', detail: 'Quick move between locations', to: 'inventory/transfers/new?mode=quick', visible: (can) => can.manageTransfer },
  { label: 'ITO', detail: 'Open transfer orders', to: 'inventory/transfers', visible: (can) => can.manageTransfer || can.approveTransfer },
  { label: 'Receive', detail: 'Incoming transfers & GRN', to: 'receive/pos?mode=receive', visible: (can) => can.postGrn },
  { label: 'Request Stock', detail: 'Raise a purchase request', to: 'inventory/purchase-requests', visible: (can) => can.requestPurchase },
  { label: 'Stock Count', detail: 'Count and review', to: 'inventory/counts', visible: (can) => can.manageCount || can.approveCount || can.initiateCount },
  { label: 'Goods Issue', detail: 'Issue to outlet / cost center', to: 'inventory/goods-issues', visible: (can) => can.postGoodsIssue },
];

/**
 * Touch-first Live Inventory hub — Alabaster mobile command center.
 * Reuses getInventoryHome and existing Mobile routes only.
 */
const InventoryScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { location, loading } = useMyLocation();
  const [query, setQuery] = useState('');
  const [floorMode, setFloorMode] = useState(false);
  const home = useLoad(() => getInventoryHome(location?.id), location ? `inv-home-${location.id}` : null);
  const pending = useLoad(
    () => getPendingOutletRequirements({ sourceLocationId: location?.id, countOnly: true }),
    location ? `inv-pending-${location.id}` : null,
  );
  const value = (count: number | undefined): string => (home.loading ? '…' : count === undefined ? '—' : String(count));

  const openSearch = () => {
    const term = query.trim();
    go(term.length >= 2 ? `inventory/stock?q=${encodeURIComponent(term)}` : 'inventory/stock');
  };

  return (
    <>
      <ScreenHeader title="Live Inventory" eyebrow="Stock" intro="Search, scan, and move stock at your outlet." />
      <div className={`sm-screen liv-m${floorMode ? ' liv-m--floor' : ''}`}>
        <OfflineBanner />

        <section className="liv-m-outlet" aria-label="Assigned outlet">
          {location ? (
            <>
              <p className="liv-m-kicker">Current outlet</p>
              <strong className="liv-m-outlet__name">{location.locationName}</strong>
              <span className="sm-meta">
                {[location.propertyName, location.plantCode, location.storageLocationCode || location.locationCode]
                  .filter(Boolean)
                  .join(' · ')}
              </span>
            </>
          ) : (
            !loading && <Empty text="No operational location assigned." />
          )}
          {home.error && <ErrorNotice message={home.error} onRetry={home.reload} />}
          {can.manageTransfer && (
            <button
              type="button"
              className="sm-btn sm-btn--secondary"
              style={{ marginTop: 8 }}
              onClick={() => go('inventory/transfers/new')}
            >
              Pending Requirements: {pending.loading ? '…' : pending.value?.count ?? 0}
            </button>
          )}
        </section>

        <section className="liv-m-search" aria-label="Search material">
          <input
            className="sm-input liv-m-search__input"
            type="search"
            enterKeyHint="search"
            placeholder="Material, description, or barcode"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') openSearch();
            }}
          />
          <button type="button" className="sm-btn sm-btn--primary liv-m-search__btn" onClick={openSearch}>
            Find
          </button>
        </section>

        {location && (
          <section className="liv-m-alerts" aria-label="Operational alerts">
            <button type="button" className="liv-m-stat liv-m-stat--warn" onClick={() => go('inventory/stock?health=LOW')}>
              <strong>{value(home.data?.lowStockItems)}</strong>
              <span>Low stock</span>
            </button>
            <button type="button" className="liv-m-stat" onClick={() => go('inventory/transfers?tab=in-transit')}>
              <strong>{value(home.data?.inTransitToReceive)}</strong>
              <span>Incoming</span>
            </button>
            <button type="button" className="liv-m-stat" onClick={() => go('tasks/alerts')}>
              <strong>
                {value(
                  home.data?.pendingActions ??
                    (home.data?.pendingTransferApprovals ?? 0) +
                      (home.data?.openAlerts ?? 0) +
                      (home.data?.openStockCounts ?? 0),
                )}
              </strong>
              <span>Pending</span>
            </button>
          </section>
        )}

        <section className="liv-m-quick" aria-labelledby="liv-m-quick-title">
          <div className="liv-m-section-head">
            <h2 id="liv-m-quick-title">Quick actions</h2>
            <button type="button" className="sm-btn" onClick={() => setFloorMode((value) => !value)}>
              {floorMode ? 'Standard' : 'Floor mode'}
            </button>
          </div>
          <div className="liv-m-quick__grid">
            {QUICK.filter((action) => !action.visible || action.visible(can)).map((action) => (
              <button key={action.label} type="button" className="liv-m-action" onClick={() => go(action.to)}>
                <strong>{action.label}</strong>
                <span>{action.detail}</span>
              </button>
            ))}
          </div>
        </section>

        <section className="sm-section">
          <h2>More</h2>
          <ul className="sm-list">
            {[
              { label: 'All transfers', to: 'inventory/transfers' },
              { label: 'Purchase requests', to: 'inventory/purchase-requests', show: can.requestPurchase },
              { label: 'Waste & adjustments', to: 'inventory/adjustments', show: can.postAdjustment },
            ]
              .filter((entry) => entry.show !== false)
              .map((entry) => (
                <li key={entry.label}>
                  <button type="button" className="sm-list-btn" onClick={() => go(entry.to)}>
                    <span className="sm-row">
                      <strong>{entry.label}</strong>
                      <span aria-hidden="true">›</span>
                    </span>
                  </button>
                </li>
              ))}
          </ul>
        </section>
      </div>
    </>
  );
};

export default InventoryScreen;
