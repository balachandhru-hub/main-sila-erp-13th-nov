import React, { useState } from 'react';
import {
  SILA_TRANSACTION_TYPES,
  formatQty,
  getInventoryTransactions,
  silaLabel,
  type SilaInventoryTransaction,
} from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import LocationPicker from '../../components/LocationPicker';
import ScreenHeader from '../../components/ScreenHeader';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { formatDateTime } from '../../format';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { useLoad } from '../../useLoad';
import './liveInventoryMobile.css';

const linkFor = (row: SilaInventoryTransaction): string | null => {
  const type = (row.referenceType || '').toUpperCase();
  if (!row.referenceId) return null;
  if (type === 'ITO') return `inventory/transfers/${row.referenceId}`;
  if (type === 'ADJUSTMENT') return `inventory/adjustments/${row.referenceId}`;
  if (type === 'GOODS_ISSUE' || type === 'GI') return `inventory/goods-issues/${row.referenceId}`;
  if (type === 'STOCK_COUNT') return `inventory/counts/${row.referenceId}`;
  return null;
};

/** Mobile inventory ledger — newest first. Replaces the mistaken Alerts route for Transaction history. */
const InventoryTransactionsScreen: React.FC = () => {
  const go = useGo();
  const { locations, location } = useMyLocation();
  const [locationId, setLocationId] = useState(location?.id ?? '');
  const [type, setType] = useState('');
  const filterLocation = locationId || location?.id || '';
  const { data, loading, error, reload } = useLoad(
    () =>
      getInventoryTransactions({
        locationId: filterLocation || undefined,
        type: type || undefined,
        index: 0,
        limit: 80,
      }),
    `inv-txn-${filterLocation || 'all'}-${type || 'all'}`,
  );
  const rows = data ?? [];

  return (
    <>
      <ScreenHeader
        title="Transaction history"
        eyebrow="Inventory"
        back
        intro="Ledger movements for your locations. Open a row to see the source document."
      />
      <div className="sm-screen liv-m liv-m--premium" data-testid="mobile-inventory-transactions">
        <LocationPicker
          label="Location"
          locations={locations}
          value={filterLocation}
          onChange={setLocationId}
          placeholder="All my locations"
        />
        <div className="sm-field">
          <label htmlFor="sm-txn-type">Type</label>
          <select id="sm-txn-type" className="sm-select" value={type} onChange={(event) => setType(event.target.value)}>
            <option value="">All types</option>
            {SILA_TRANSACTION_TYPES.map((item) => (
              <option key={item} value={item}>
                {silaLabel(item)}
              </option>
            ))}
          </select>
        </div>

        {loading && <Loading text="Loading transactions…" />}
        {error && <ErrorNotice message={error} onRetry={reload} />}
        {!loading && !error && rows.length === 0 && (
          <Empty text="No transactions" description="Post a transfer, count, or adjustment to see the ledger here." />
        )}
        {!loading && rows.length > 0 && (
          <ul className="sm-list">
            {rows.map((row) => {
              const to = linkFor(row);
              return (
                <li key={row.id}>
                  <button
                    type="button"
                    className="sm-list-btn liv-m-card"
                    onClick={() => (to ? go(to) : undefined)}
                    disabled={!to}
                  >
                    <span className="sm-row">
                      <strong>{row.referenceNumber || row.transactionNumber}</strong>
                      <span className="sm-meta">{silaLabel(row.transactionType)}</span>
                    </span>
                    <span className="sm-meta">
                      {row.materialCode} · {row.direction} {formatQty(row.quantity)} {row.baseUom}
                    </span>
                    <span className="sm-meta">
                      {row.locationName ?? '—'} · {formatDateTime(row.dateCreated)}
                    </span>
                  </button>
                </li>
              );
            })}
          </ul>
        )}
      </div>
    </>
  );
};

export default InventoryTransactionsScreen;
