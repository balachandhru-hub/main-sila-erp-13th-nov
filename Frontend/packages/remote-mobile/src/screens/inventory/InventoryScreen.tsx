import React, { useState } from 'react';
import { getInventoryHome } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import ScreenHeader from '../../components/ScreenHeader';
import { Empty, ErrorNotice, OfflineBanner } from '../../components/StateViews';
import { useAccess } from '../../access';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';

interface QuickAction {
  label: string;
  to: string;
  visible?: (can: ReturnType<typeof useAccess>['can']) => boolean;
}

const QUICK: QuickAction[] = [
  { label: 'Find Stock', to: 'inventory/stock' },
  { label: 'Transfer', to: 'inventory/transfers/new?mode=quick', visible: (can) => can.manageTransfer },
  { label: 'ITO', to: 'inventory/transfers', visible: (can) => can.manageTransfer || can.approveTransfer },
  { label: 'Receive', to: 'receive/pos?mode=receive', visible: (can) => can.postGrn },
  { label: 'Stock Count', to: 'inventory/counts', visible: (can) => can.manageCount || can.approveCount || can.initiateCount },
];

/**
 * Touch-first Live Inventory hub: compact location, search/scan entry, permitted quick actions,
 * and operational indicators — reusing getInventoryHome and existing routes.
 */
const InventoryScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { location, loading } = useMyLocation();
  const [query, setQuery] = useState('');
  const home = useLoad(() => getInventoryHome(location?.id), location ? `inv-home-${location.id}` : null);
  const value = (count: number | undefined): string => (home.loading ? '…' : count === undefined ? '—' : String(count));

  const openSearch = () => {
    const term = query.trim();
    go(term.length >= 2 ? `inventory/stock?q=${encodeURIComponent(term)}` : 'inventory/stock');
  };

  return (
    <>
      <ScreenHeader title="Live Inventory" eyebrow="Stock" intro="Search, scan, and move stock at your location." />
      <div className="sm-screen">
        <OfflineBanner />
        <section className="sm-section" aria-labelledby="sm-live-loc">
          <h2 id="sm-live-loc">Location</h2>
          {location ? (
            <div className="sm-card">
              <strong className="sm-title">{location.locationName}</strong>
              <span className="sm-meta">
                {[location.propertyName, location.plantCode, location.storageLocationCode || location.locationCode]
                  .filter(Boolean)
                  .join(' · ')}
              </span>
            </div>
          ) : (
            !loading && <Empty text="No operational location assigned." />
          )}
          {home.error && <ErrorNotice message={home.error} onRetry={home.reload} />}
        </section>

        <section className="sm-section" aria-labelledby="sm-live-search">
          <h2 id="sm-live-search">Search and scan</h2>
          <div className="sm-row" style={{ gap: '0.5rem' }}>
            <input
              className="sm-input"
              type="search"
              enterKeyHint="search"
              placeholder="Material, description, or barcode"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === 'Enter') openSearch();
              }}
              style={{ flex: 1, minHeight: '2.75rem' }}
            />
            <button type="button" className="sm-btn sm-btn--primary" onClick={openSearch}>
              Find
            </button>
          </div>
          <p className="sm-muted">Use a barcode wedge in the search field, or open Find Stock for the full list.</p>
        </section>

        <section className="sm-section" aria-labelledby="sm-live-quick">
          <h2 id="sm-live-quick">Quick actions</h2>
          <div className="sm-grid-2">
            {QUICK.filter((action) => !action.visible || action.visible(can)).map((action) => (
              <button key={action.label} type="button" className="sm-action" onClick={() => go(action.to)}>
                <strong>{action.label}</strong>
              </button>
            ))}
          </div>
        </section>

        {location && (
          <section className="sm-section" aria-labelledby="sm-live-ops">
            <h2 id="sm-live-ops">Operational summary</h2>
            <div className="sm-grid-3">
              <button type="button" className="sm-stat" onClick={() => go('inventory/stock?health=LOW')}>
                <strong>{value(home.data?.lowStockItems)}</strong>
                <span className="sm-muted">Low Stock</span>
              </button>
              <button type="button" className="sm-stat" onClick={() => go('inventory/transfers?tab=in-transit')}>
                <strong>{value(home.data?.inTransitToReceive)}</strong>
                <span className="sm-muted">Incoming</span>
              </button>
              <button
                type="button"
                className="sm-stat"
                onClick={() => go('tasks/alerts')}
              >
                <strong>{value(home.data?.pendingActions ?? ((home.data?.pendingTransferApprovals ?? 0) + (home.data?.openAlerts ?? 0) + (home.data?.openStockCounts ?? 0)))}</strong>
                <span className="sm-muted">Pending</span>
              </button>
            </div>
          </section>
        )}

        <section className="sm-section">
          <h2>More</h2>
          <ul className="sm-list">
            {[
              { label: 'All transfers', to: 'inventory/transfers' },
              { label: 'Purchase requests', to: 'inventory/purchase-requests', show: can.requestPurchase },
              { label: 'Recipes', to: 'recipes', show: can.manageRecipe || can.approveRecipe },
              { label: 'Goods issue', to: 'inventory/goods-issues', show: can.postGoodsIssue },
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
