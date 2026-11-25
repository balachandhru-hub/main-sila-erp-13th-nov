import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  formatQty,
  searchLiveInventory,
  SILA_LIVE_PAGE_SIZE,
  silaLabel,
  type SilaLiveInventoryRow,
} from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import ScreenHeader from '../../components/ScreenHeader';
import { Empty, ErrorNotice, Loading } from '../../components/StateViews';
import { useMyLocation } from '../../location';
import { useGo } from '../../navigation';
import { errorText } from '../../useLoad';
import './liveInventoryMobile.css';

const MIN_SEARCH = 2;

const STATUS_CLASS: Record<string, string> = {
  NEGATIVE: 'sm-badge sm-badge--error',
  OUT: 'sm-badge sm-badge--error',
  LOW: 'sm-badge sm-badge--warning',
  HEALTHY: 'sm-badge sm-badge--success',
  EXCESS: 'sm-badge sm-badge--neutral',
  IN_STOCK: 'sm-badge sm-badge--success',
};

/** Card-based material availability — own location by default; search is cross-location. */
const LiveStockScreen: React.FC = () => {
  const go = useGo();
  const [params] = useSearchParams();
  const { location } = useMyLocation();
  const [search, setSearch] = useState<string>(params.get('q') ?? '');
  const [health, setHealth] = useState<string>(params.get('health') ?? '');
  const [rows, setRows] = useState<SilaLiveInventoryRow[]>([]);
  const [hasMore, setHasMore] = useState<boolean>(false);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState<number>(0);
  const text = search.trim();
  const searching = text.length >= MIN_SEARCH;
  const locationId = searching ? null : location?.id ?? null;

  useEffect(() => {
    const q = params.get('q');
    const h = params.get('health');
    if (q != null) setSearch(q);
    if (h != null) setHealth(h);
  }, [params]);

  useEffect(() => {
    if (!searching && !locationId) {
      setRows([]);
      setHasMore(false);
      setError(null);
      return;
    }
    let active = true;
    const timer = window.setTimeout(() => {
      setLoading(true);
      setError(null);
      searchLiveInventory(searching ? text : '', locationId, 0, locationId ? health || null : null)
        .then((found) => {
          if (!active) return;
          setRows(found);
          setHasMore(found.length === SILA_LIVE_PAGE_SIZE);
        })
        .catch((caught: unknown) => {
          if (active) setError(errorText(caught));
        })
        .finally(() => {
          if (active) setLoading(false);
        });
    }, searching ? 350 : 100);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [text, searching, locationId, health, attempt]);

  const loadMore = async () => {
    setLoading(true);
    try {
      const found = await searchLiveInventory(searching ? text : '', locationId, rows.length, locationId ? health || null : null);
      setRows((current) => [...current, ...found]);
      setHasMore(found.length === SILA_LIVE_PAGE_SIZE);
    } catch (caught: unknown) {
      setError(errorText(caught));
    } finally {
      setLoading(false);
    }
  };

  const descriptionOf = (row: SilaLiveInventoryRow) =>
    row.description && row.description !== row.materialCode ? row.description : row.description || '—';

  return (
    <>
      <ScreenHeader title="Find Stock" eyebrow="Live Inventory" back backTo="inventory" />
      <div className="sm-screen liv-m">
        <div className="liv-m-search">
          <input
            className="sm-input liv-m-search__input"
            type="search"
            aria-label="Search material"
            placeholder="Search or scan material ID, name, barcode…"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
        </div>
        {health && (
          <button type="button" className="sm-badge sm-badge--warning" onClick={() => setHealth('')}>
            Filter: {silaLabel(health)} ×
          </button>
        )}
        {location && !searching && (
          <p className="sm-muted">Stock at {location.locationName}. Search a material for cross-location availability.</p>
        )}
        {error && <ErrorNotice message={error} onRetry={() => setAttempt((value) => value + 1)} />}
        {!loading && !error && (searching || location) && rows.length === 0 && (
          <Empty text="No materials" description={searching ? `No matches for “${text}”.` : 'Nothing is in stock at this location.'} />
        )}
        <ul className="sm-list" style={{ gap: '0.55rem' }}>
          {rows.map((row) => {
            const status = row.stockStatus || (row.onHandQty <= 0 ? 'OUT' : 'IN_STOCK');
            const available = row.availableQty ?? row.onHandQty;
            return (
              <li key={row.materialId}>
                <button type="button" className="liv-m-card" onClick={() => go(`inventory/stock/${row.materialId}`)}>
                  <span className="sm-row">
                    <strong>{descriptionOf(row)}</strong>
                    <span className={STATUS_CLASS[status] || 'sm-badge sm-badge--neutral'}>{silaLabel(status)}</span>
                  </span>
                  <span className="sm-meta">{row.materialCode}</span>
                  <span className="sm-row">
                    <span className="liv-m-card__qty">
                      {formatQty(available)} {row.baseUom}
                    </span>
                    <span className="sm-meta">
                      {searching ? `${row.locationCount} location${row.locationCount === 1 ? '' : 's'}` : 'Tap for details'}
                    </span>
                  </span>
                </button>
              </li>
            );
          })}
        </ul>
        {loading && <Loading text={searching ? 'Searching…' : 'Loading stock…'} />}
        {hasMore && !loading && (
          <button type="button" className="sm-btn sm-btn--block" onClick={loadMore}>
            Load more
          </button>
        )}
      </div>
    </>
  );
};

export default LiveStockScreen;
