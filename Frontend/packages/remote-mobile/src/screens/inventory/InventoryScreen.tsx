import React, { useState } from 'react';
import { getInventoryHome, getPendingOutletRequirements } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import BarcodeScanner from '../../components/BarcodeScanner';
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
  { label: 'Scan material', detail: 'Barcode / photo → action', to: 'inventory/scan' },
  { label: 'Stock Count', detail: 'Scan, photo, or search', to: 'inventory/counts', visible: (can) => can.manageCount || can.approveCount || can.initiateCount },
  { label: 'Quick Transfer', detail: 'Scan · check stock · move', to: 'inventory/transfers/new?mode=quick', visible: (can) => can.manageTransfer },
  { label: 'ITO', detail: 'Open transfer orders', to: 'inventory/transfers', visible: (can) => can.manageTransfer || can.approveTransfer },
  { label: 'Goods Issue', detail: 'Issue to cost center', to: 'inventory/goods-issues', visible: (can) => can.postGoodsIssue },
  { label: 'Find Stock', detail: 'Search materials', to: 'inventory/stock' },
  { label: 'Request Stock', detail: 'Purchase request', to: 'inventory/purchase-requests', visible: (can) => can.requestPurchase },
  { label: 'Adjustments', detail: 'Waste & corrections', to: 'inventory/adjustments', visible: (can) => can.postAdjustment },
  { label: 'Receive', detail: 'Incoming transfers & GRN', to: 'receive/pos?mode=receive', visible: (can) => can.postGrn },
];

/**
 * Premium Mobile Live Inventory hub — reuses getInventoryHome KPIs and existing routes only.
 * Camera scan opens Find Stock with the decoded barcode. Cloud Live Inventory GUI is unchanged.
 */
const InventoryScreen: React.FC = () => {
  const go = useGo();
  const { can } = useAccess();
  const { location, locations, loading, reload: reloadLocation, setLocationId } = useMyLocation();
  const [query, setQuery] = useState('');
  const [floorMode, setFloorMode] = useState(false);
  const [scannerOpen, setScannerOpen] = useState(false);
  const home = useLoad(() => getInventoryHome(location?.id), location ? `inv-home-${location.id}` : null);
  const pending = useLoad(
    () => getPendingOutletRequirements({ sourceLocationId: location?.id, countOnly: true }),
    location ? `inv-pending-${location.id}` : null,
  );
  const value = (count: number | undefined): string => (home.loading ? '…' : count === undefined ? '—' : String(count));

  const openSearch = (term?: string) => {
    const text = (term ?? query).trim();
    go(text.length >= 1 ? `inventory/stock?q=${encodeURIComponent(text)}` : 'inventory/stock');
  };

  const refresh = () => {
    home.reload();
    pending.reload();
    reloadLocation();
  };

  return (
    <>
      <ScreenHeader title="Live Inventory" eyebrow="SILA Store" />
      <div className={`sm-screen liv-m liv-m--premium${floorMode ? ' liv-m--floor' : ''}`} data-testid="mobile-live-inventory">
        <OfflineBanner />

        <header className="liv-m-header" aria-label="Location">
          <div className="liv-m-header__copy">
            {location ? (
              <>
                <p className="liv-m-kicker">{location.propertyName || 'Property'}</p>
                <strong className="liv-m-outlet__name">{location.locationName}</strong>
                <span className="sm-meta">
                  {[location.plantCode, location.storageLocationCode || location.locationCode].filter(Boolean).join(' · ')}
                </span>
              </>
            ) : (
              !loading && <Empty text="No operational location assigned." />
            )}
          </div>
          <div className="liv-m-header__actions">
            {locations.length > 1 && (
              <label className="liv-m-switcher">
                <span className="sm-meta">Location</span>
                <select
                  className="sm-input"
                  value={location?.id ?? ''}
                  onChange={(event) => setLocationId(event.target.value)}
                  aria-label="Switch outlet"
                >
                  {locations.map((row) => (
                    <option key={row.id} value={row.id}>
                      {row.locationName}
                    </option>
                  ))}
                </select>
              </label>
            )}
            <button type="button" className="sm-btn sm-btn--secondary" onClick={refresh} aria-label="Refresh">
              Refresh
            </button>
            <button type="button" className="sm-btn" onClick={() => go('tasks/alerts')} aria-label="Notifications">
              Alerts
            </button>
          </div>
        </header>

        {home.error && <ErrorNotice message={home.error} onRetry={home.reload} />}

        {location && (
          <section className="liv-m-kpis" aria-label="Inventory KPIs">
            <button type="button" className="liv-m-kpi" onClick={() => go('inventory/stock')}>
              <strong>{value(home.data?.stockItems)}</strong>
              <span>Total materials</span>
            </button>
            <button type="button" className="liv-m-kpi liv-m-kpi--ok" onClick={() => go('inventory/stock')}>
              <strong>{value(home.data?.stockItems)}</strong>
              <span>Available stock</span>
            </button>
            <button type="button" className="liv-m-kpi liv-m-kpi--warn" onClick={() => go('inventory/stock?health=LOW')}>
              <strong>{value(home.data?.lowStockItems)}</strong>
              <span>Low stock</span>
            </button>
            <button type="button" className="liv-m-kpi liv-m-kpi--danger" onClick={() => go('inventory/stock?health=OUT')}>
              <strong>{value(home.data?.lowStockItems)}</strong>
              <span>Out / critical</span>
            </button>
            <button type="button" className="liv-m-kpi" onClick={() => go('inventory/transfers/new')}>
              <strong>{pending.loading ? '…' : pending.value?.count ?? 0}</strong>
              <span>Pending requirements</span>
            </button>
            <button type="button" className="liv-m-kpi" onClick={() => go('inventory/transfers?tab=in-transit')}>
              <strong>{value((home.data?.inTransitToReceive ?? 0) + (home.data?.pendingTransferApprovals ?? 0))}</strong>
              <span>Pending transfers</span>
            </button>
            <button type="button" className="liv-m-kpi" onClick={() => go('inventory/counts')}>
              <strong>{value(home.data?.openStockCounts)}</strong>
              <span>Stock count pending</span>
            </button>
            <button type="button" className="liv-m-kpi" onClick={() => go('tasks/alerts')}>
              <strong>{value(home.data?.openAlerts ?? home.data?.pendingActions)}</strong>
              <span>Inventory variances</span>
            </button>
          </section>
        )}

        <section className="liv-m-search" aria-label="Material search">
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
          <button type="button" className="sm-btn sm-btn--secondary" onClick={() => setScannerOpen(true)} data-testid="liv-scan-barcode">
            Scan
          </button>
          <button type="button" className="sm-btn sm-btn--primary liv-m-search__btn" onClick={() => openSearch()}>
            Find
          </button>
        </section>

        <section className="liv-m-quick" aria-labelledby="liv-m-quick-title">
          <div className="liv-m-section-head">
            <h2 id="liv-m-quick-title">Primary actions</h2>
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

        <section className="sm-section liv-m-ops" aria-labelledby="liv-m-ops-title">
          <h2 id="liv-m-ops-title">Operational</h2>
          <ul className="sm-list">
            {[
              { label: 'Weekly bucket / wishlist requirements', to: 'inventory/transfers/new', show: can.manageTransfer },
              { label: 'Pending ITO & transfers', to: 'inventory/transfers' },
              { label: 'Stock count history', to: 'inventory/counts', show: can.manageCount || can.approveCount || can.initiateCount },
              { label: 'Stock health', to: 'inventory/stock?health=LOW' },
              { label: 'Transaction history', to: 'inventory/transactions' },
              { label: 'Recommendations', to: 'inventory/recommendations', show: can.requestPurchase },
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

      <BarcodeScanner
        open={scannerOpen}
        title="Scan material barcode"
        onClose={() => setScannerOpen(false)}
        onDetected={(scanned) => openSearch(scanned.rawValue)}
      />
    </>
  );
};

export default InventoryScreen;
