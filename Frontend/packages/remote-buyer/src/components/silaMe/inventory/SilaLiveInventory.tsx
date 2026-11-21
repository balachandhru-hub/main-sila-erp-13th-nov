import React, { useEffect, useMemo, useRef, useState } from "react";
import { EmptyState, Loader, PageHeader, toastService } from "@vosox/shared-ui";
import {
  SILA_LIVE_PAGE_SIZE,
  formatQty,
  getInventoryHome,
  getLiveInventoryDetail,
  getMyLocations,
  searchLiveInventory,
  silaLabel,
  type SilaInventoryHome,
  type SilaLiveInventoryDetail,
  type SilaLiveInventoryLocation,
  type SilaLiveInventoryRow,
  type SilaLocation,
} from "../../../api/silaMe/silaInventoryApi";
import { createQuickTransfer, createTransfer } from "../../../api/silaMe/silaMovementsApi";
import SilaSearchableSelect from "../SilaSearchableSelect";
import SilaLiveDecisionPanel from "./SilaLiveDecisionPanel";
import "../silaMeTheme.css";
import "./SilaInventory.css";

const byCode = (locations: SilaLocation[], type: string): SilaLocation | undefined =>
  locations
    .filter((location) => location.locationType === type)
    .sort((a, b) => a.locationCode.localeCompare(b.locationCode))[0];

const homeLocation = (locations: SilaLocation[]): SilaLocation | undefined => {
  const assigned = locations.filter((location) => location.assigned);
  const pool = assigned.length > 0 ? assigned : locations;
  const stocked = pool.filter((location) => location.locationType !== "VENUE");
  if (stocked.length === 1) return stocked[0];
  return byCode(stocked, "STORE") ?? byCode(stocked, "OUTLET") ?? pool[0];
};

const destinationLocation = (locations: SilaLocation[]): SilaLocation | undefined => {
  const assigned = locations.filter((location) => location.assigned);
  const pool = assigned.length > 0 ? assigned : locations;
  return byCode(pool, "OUTLET") ?? homeLocation(locations);
};

const STATUS_TEXT: Record<string, string> = {
  NEGATIVE: "Negative",
  OUT: "Out of stock",
  LOW: "Low stock",
  HEALTHY: "Available",
  EXCESS: "Excess",
  IN_STOCK: "Available",
};

const STATUS_BADGE: Record<string, string> = {
  NEGATIVE: "sila-badge--danger",
  OUT: "sila-badge--danger",
  LOW: "sila-badge--warning",
  HEALTHY: "sila-badge--success",
  EXCESS: "sila-badge--info",
  IN_STOCK: "sila-badge--success",
};

type OptionalColumn = "onHand" | "reserved" | "inTransit" | "reorder" | "lastMovement";

const OPTIONAL_COLUMNS: { id: OptionalColumn; label: string }[] = [
  { id: "onHand", label: "On Hand" },
  { id: "reserved", label: "Reserved" },
  { id: "inTransit", label: "In Transit" },
  { id: "reorder", label: "Reorder Level" },
  { id: "lastMovement", label: "Last Movement" },
];

interface SilaLiveInventoryProps {
  canTransfer?: boolean;
  canRequestPurchase?: boolean;
  canManageCount?: boolean;
  initialSearch?: string;
  initialHealth?: string;
  onNavigate?: (key: string) => void;
}

/**
 * Premium Live Inventory home: compact location context, actionable KPIs, searchable stock table,
 * and dedicated material detail / transfer actions — reusing existing live + transfer APIs.
 */
const SilaLiveInventory: React.FC<SilaLiveInventoryProps> = ({
  canTransfer = true,
  canRequestPurchase = false,
  canManageCount = false,
  initialSearch = "",
  initialHealth = "",
  onNavigate,
}) => {
  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [locationsError, setLocationsError] = useState<string | null>(null);
  const [search, setSearch] = useState(initialSearch);
  const [health, setHealth] = useState(initialHealth);
  const [locationId, setLocationId] = useState("");
  const [home, setHome] = useState<SilaInventoryHome | null>(null);
  const [rows, setRows] = useState<SilaLiveInventoryRow[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [detail, setDetail] = useState<SilaLiveInventoryDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [transferToId, setTransferToId] = useState("");
  const [requiredQty, setRequiredQty] = useState("");
  const [detailKey, setDetailKey] = useState(0);
  const [pickingId, setPickingId] = useState<string | null>(null);
  const [menuFor, setMenuFor] = useState<string | null>(null);
  const [optionalCols, setOptionalCols] = useState<Set<OptionalColumn>>(new Set());
  const [showColumns, setShowColumns] = useState(false);
  const searchRef = useRef<HTMLInputElement>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    let active = true;
    getMyLocations()
      .then((data) => {
        if (!active) return;
        setLocations(data);
        const homeLoc = homeLocation(data);
        const destination = destinationLocation(data);
        if (homeLoc) setLocationId(homeLoc.id);
        if (destination) setTransferToId(destination.id);
      })
      .catch((err: unknown) => {
        if (active) setLocationsError(err instanceof Error ? err.message : "Could not load your locations.");
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    if (!locationId) {
      setHome(null);
      return;
    }
    let active = true;
    getInventoryHome(locationId)
      .then((data) => {
        if (active) setHome(data);
      })
      .catch(() => {
        if (active) setHome(null);
      });
    return () => {
      active = false;
    };
  }, [locationId]);

  const materialTerm = search.trim();
  const canSearch = Boolean(locationId) || materialTerm.length >= 2;

  useEffect(() => {
    if (!canSearch) {
      setRows([]);
      setHasMore(false);
      setError(null);
      return;
    }
    abortRef.current?.abort();
    const controller = new AbortController();
    abortRef.current = controller;
    let active = true;
    setLoading(true);
    setError(null);
    const timer = window.setTimeout(async () => {
      try {
        const data = await searchLiveInventory(search, locationId || null, 0, health || null);
        if (!active || controller.signal.aborted) return;
        setRows(data);
        setHasMore(data.length === SILA_LIVE_PAGE_SIZE);
      } catch (err: unknown) {
        if (active && !controller.signal.aborted) {
          setError(err instanceof Error ? err.message : "Could not search the inventory.");
        }
      } finally {
        if (active) setLoading(false);
      }
    }, locationId ? 120 : 300);
    return () => {
      active = false;
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [search, locationId, health, canSearch]);

  useEffect(() => {
    if (!selectedId) return;
    let active = true;
    setDetailError(null);
    setDetailLoading(true);
    const timer = window.setTimeout(async () => {
      try {
        const required = Number(requiredQty);
        const data = await getLiveInventoryDetail(selectedId, {
          requiredQty: Number.isFinite(required) && required > 0 ? required : undefined,
          currentLocationId: locationId || transferToId || undefined,
        });
        if (active) setDetail(data);
      } catch (err: unknown) {
        if (active) setDetailError(err instanceof Error ? err.message : "Could not load the stock of the material.");
      } finally {
        if (active) setDetailLoading(false);
      }
    }, 350);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [selectedId, locationId, transferToId, requiredQty, detailKey]);

  const stockLocations = useMemo(
    () => locations.filter((location) => location.locationType !== "VENUE"),
    [locations],
  );
  const currentLocation = locations.find((location) => location.id === locationId);

  const loadMore = async () => {
    setLoadingMore(true);
    try {
      const data = await searchLiveInventory(search, locationId || null, rows.length, health || null);
      setRows((current) => [...current, ...data]);
      setHasMore(data.length === SILA_LIVE_PAGE_SIZE);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not search the inventory.");
    } finally {
      setLoadingMore(false);
    }
  };

  const toggleColumn = (id: OptionalColumn) => {
    setOptionalCols((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const applyKpi = (filter: string) => {
    if (filter === "LOW") {
      setHealth("LOW");
      setSelectedId(null);
      return;
    }
    if (filter === "TRANSIT") {
      onNavigate?.("silaTransfers");
      return;
    }
    if (filter === "PENDING") {
      onNavigate?.("silaAlerts");
      return;
    }
    setHealth("");
  };

  const sendTransfer = async (row: SilaLiveInventoryLocation, quick: boolean) => {
    if (!canTransfer || !detail) return;
    const destination = locations.find((location) => location.id === transferToId);
    if (!destination) {
      toastService.error("Choose the destination in Transfer before sending stock.");
      return;
    }
    if (row.locationId === destination.id) return;
    const transferable = row.transferableQty ?? 0;
    if (!row.transferEnabled || transferable <= 0) {
      toastService.error(`${row.locationName} has no transferable stock.`);
      return;
    }
    const required = Number(requiredQty) || 0;
    const wanted = (detail.shortage ?? 0) > 0 ? (detail.shortage as number) : required > 0 ? required : 1;
    const quantity = Math.min(wanted, transferable);
    setPickingId(`${row.locationId}:${quick ? "quick" : "ito"}`);
    try {
      const payload = {
        fromLocationId: row.locationId,
        toLocationId: destination.id,
        reason: quick ? "Guest service" : `Transfer from ${row.locationName}`,
        requiredBy: null,
        items: [{ materialId: detail.materialId, quantity, uom: null }],
      };
      if (quick) await createQuickTransfer(payload);
      else await createTransfer(payload);
      toastService.success(
        `${quick ? "Quick ITO" : "ITO"} of ${formatQty(quantity)} ${detail.baseUom} from ${row.locationName} to ${destination.locationCode} raised.`,
      );
      setDetailKey((key) => key + 1);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not request the transfer.");
    } finally {
      setPickingId(null);
    }
  };

  const sourceRows = (detail?.locations ?? []).filter(
    (row) => row.onHandQty !== 0 || row.inTransitQty !== 0 || (row.reservedQty ?? 0) !== 0 || row.locationId === locationId,
  );

  const statusOf = (row: SilaLiveInventoryRow) => {
    const code = row.stockStatus || (row.onHandQty <= 0 ? "OUT" : "IN_STOCK");
    return {
      label: STATUS_TEXT[code] ?? silaLabel(code),
      badge: STATUS_BADGE[code] ?? "sila-badge--info",
    };
  };

  const kpis = [
    {
      key: "items",
      label: "Stock Items",
      value: home?.stockItems ?? "—",
      tone: "sinv-kpi--blue",
      onClick: () => applyKpi(""),
    },
    {
      key: "low",
      label: "Low Stock",
      value: home?.lowStockItems ?? "—",
      tone: "sinv-kpi--amber",
      onClick: () => applyKpi("LOW"),
    },
    {
      key: "transit",
      label: "In Transit",
      value: home?.inTransitToReceive ?? "—",
      tone: "sinv-kpi--lavender",
      onClick: () => applyKpi("TRANSIT"),
    },
    {
      key: "pending",
      label: "Pending Actions",
      value: home?.pendingActions ?? "—",
      tone: "sinv-kpi--mint",
      onClick: () => applyKpi("PENDING"),
    },
  ];

  if (selectedId) {
    return (
      <div className="sila-me sinv-page">
        <PageHeader
          className="pud-page-header"
          title="Material Details"
          description={detail ? `${detail.materialCode} · ${detail.description}` : "Loading material stock…"}
        />
        <div className="sinv-detail-bar">
          <button type="button" className="sila-btn sila-btn--ghost" onClick={() => { setSelectedId(null); setDetail(null); setMenuFor(null); }}>
            ← Back to Live Inventory
          </button>
          {canTransfer && (
            <div className="sinv-detail-dest">
              <SilaSearchableSelect
                label="Transfer destination"
                placeholder="Select destination"
                value={transferToId}
                clearable={false}
                options={stockLocations.map((location) => ({
                  value: location.id,
                  label: `${location.locationCode} · ${location.locationName}`,
                }))}
                onChange={setTransferToId}
              />
            </div>
          )}
          <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onNavigate?.("silaTransactions")}>
            Movement History
          </button>
          {canTransfer && (
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onNavigate?.("silaTransfers")}>
              Transfer Center
            </button>
          )}
        </div>
        {detailLoading && !detail ? (
          <Loader size={28} message="Loading material…" />
        ) : detailError ? (
          <EmptyState variant="error" title="Couldn't load material" description={detailError} />
        ) : detail ? (
          <div className="sinv-detail-grid">
            <section className="sila-card">
              <SilaLiveDecisionPanel
                detail={detail}
                locations={stockLocations}
                currentLocationId={transferToId || locationId}
                requiredQty={requiredQty}
                onRequiredQtyChange={setRequiredQty}
                canTransfer={canTransfer}
                canRequestPurchase={canRequestPurchase}
                onChanged={() => setDetailKey((key) => key + 1)}
              />
            </section>
            <section className="sila-card">
              <div className="sila-card-header">
                <h2 className="sila-card-title">Stock by location</h2>
              </div>
              {sourceRows.length === 0 ? (
                <EmptyState title="No stock rows" description="This material has no balances in your locations." />
              ) : (
                <div className="sila-table-wrap">
                  <table className="sila-table">
                    <thead>
                      <tr>
                        <th scope="col">Location</th>
                        <th scope="col">Plant</th>
                        <th scope="col" className="sinv-num">On hand</th>
                        <th scope="col" className="sinv-num">Available</th>
                        <th scope="col" className="sinv-num">Transferable</th>
                        <th scope="col">Status</th>
                        {canTransfer && <th scope="col">Actions</th>}
                      </tr>
                    </thead>
                    <tbody>
                      {sourceRows.map((row) => {
                        const code = row.stockStatus || "IN_STOCK";
                        const canPick = row.transferEnabled && (row.transferableQty ?? 0) > 0 && row.locationId !== transferToId;
                        return (
                          <tr key={row.locationId}>
                            <td>
                              <span className="sila-cell-strong">{row.locationName}</span>
                              <span className="sinv-sub">{row.locationCode} · {silaLabel(row.locationType)}</span>
                            </td>
                            <td>{row.propertyCode || "—"}</td>
                            <td className="sinv-num">{formatQty(row.onHandQty)}</td>
                            <td className="sinv-num">{formatQty(row.availableQty ?? row.onHandQty)}</td>
                            <td className="sinv-num">{row.transferableQty ? formatQty(row.transferableQty) : "—"}</td>
                            <td>
                              <span className={`sila-badge ${STATUS_BADGE[code] ?? "sila-badge--info"}`}>
                                {STATUS_TEXT[code] ?? silaLabel(code)}
                              </span>
                            </td>
                            {canTransfer && (
                              <td>
                                {canPick && (
                                  <span className="sinv-inline">
                                    <button
                                      type="button"
                                      className="sila-btn sila-btn--primary sila-btn--sm"
                                      disabled={pickingId !== null}
                                      onClick={() => sendTransfer(row, true)}
                                    >
                                      {pickingId === `${row.locationId}:quick` ? "…" : "Quick ITO"}
                                    </button>
                                    <button
                                      type="button"
                                      className="sila-btn sila-btn--secondary sila-btn--sm"
                                      disabled={pickingId !== null}
                                      onClick={() => sendTransfer(row, false)}
                                    >
                                      {pickingId === `${row.locationId}:ito` ? "…" : "ITO"}
                                    </button>
                                  </span>
                                )}
                              </td>
                            )}
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </section>
          </div>
        ) : null}
      </div>
    );
  }

  return (
    <div className="sila-me sinv-page">
      <PageHeader className="pud-page-header" title="Live Inventory" description="Operational stock at your location. Open a material for transfers, history, and recommendations." />

      <section className="sila-card sinv-live-header">
        <div className="sinv-live-context">
          <div>
            <span className="sila-label">Property / outlet</span>
            <strong className="sinv-live-title">
              {currentLocation
                ? `${currentLocation.locationCode} · ${currentLocation.locationName}`
                : locationsError ?? "Select a location"}
            </strong>
            <span className="sinv-sub">
              {[home?.propertyName || currentLocation?.propertyName, home?.plantCode || currentLocation?.plantCode, home?.storageLocationCode || currentLocation?.storageLocationCode]
                .filter(Boolean)
                .join(" · ") || "Plant and storage from Location Setup"}
            </span>
          </div>
          <div className="sinv-live-header-actions">
            <SilaSearchableSelect
              label="Location"
              placeholder="Select location"
              value={locationId}
              clearable
              options={stockLocations.map((location) => ({
                value: location.id,
                label: `${location.locationCode} · ${location.locationName}`,
              }))}
              onChange={(value) => setLocationId(value)}
            />
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onNavigate?.("silaStockHealth")}>
              Stock Health
            </button>
            <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onNavigate?.("silaTransfers")}>
              Transfer Center
            </button>
          </div>
        </div>
        <div className="sinv-live-search-row">
          <label className="sila-label" htmlFor="sinv-live-search">Search</label>
          <input
            ref={searchRef}
            id="sinv-live-search"
            className="sila-input sinv-live-search"
            type="search"
            placeholder="Material number, description, barcode…"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
          />
          {health && (
            <button type="button" className="sila-badge sila-badge--warning sinv-filter-chip" onClick={() => setHealth("")}>
              Filter: {STATUS_TEXT[health] ?? health} ×
            </button>
          )}
        </div>
      </section>

      <div className="sinv-kpi-row">
        {kpis.map((kpi) => (
          <button key={kpi.key} type="button" className={`sinv-kpi ${kpi.tone}`} onClick={kpi.onClick}>
            <span>{kpi.label}</span>
            <strong>{kpi.value}</strong>
          </button>
        ))}
      </div>

      <section className="sila-card" aria-live="polite">
        <div className="sila-card-header">
          <h2 className="sila-card-title">Materials</h2>
          <div className="sinv-inline">
            <div className="sinv-columns">
              <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => setShowColumns((v) => !v)}>
                Columns
              </button>
              {showColumns && (
                <div className="sinv-columns-menu" role="menu">
                  {OPTIONAL_COLUMNS.map((column) => (
                    <label key={column.id} className="sinv-columns-item">
                      <input type="checkbox" checked={optionalCols.has(column.id)} onChange={() => toggleColumn(column.id)} />
                      {column.label}
                    </label>
                  ))}
                </div>
              )}
            </div>
            {canTransfer && (
              <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onNavigate?.("silaTransfers")}>
                New Transfer
              </button>
            )}
          </div>
        </div>

        {!canSearch ? (
          locations.length === 0 && !locationsError ? (
            <Loader size={24} message="Loading your location…" />
          ) : (
            <EmptyState
              title={locationsError ? "Couldn't load locations" : "Choose a location or search"}
              description={locationsError ?? "Select a store/outlet, or search a material across locations you can open."}
            />
          )
        ) : loading ? (
          <div className="sinv-skeleton" aria-busy="true">
            <div className="sinv-skel-row" /><div className="sinv-skel-row" /><div className="sinv-skel-row" />
          </div>
        ) : error ? (
          <EmptyState variant="error" title="Couldn't load stock" description={error} />
        ) : rows.length === 0 ? (
          <EmptyState title="No materials found" description="Try another search or clear the health filter." />
        ) : (
          <>
            <div className="sila-table-wrap">
              <table className="sila-table">
                <thead>
                  <tr>
                    <th scope="col">Material</th>
                    <th scope="col">Description</th>
                    <th scope="col" className="sinv-num">Available</th>
                    <th scope="col">UOM</th>
                    <th scope="col">Stock Status</th>
                    {optionalCols.has("onHand") && <th scope="col" className="sinv-num">On Hand</th>}
                    {optionalCols.has("reserved") && <th scope="col" className="sinv-num">Reserved</th>}
                    {optionalCols.has("inTransit") && <th scope="col" className="sinv-num">In Transit</th>}
                    {optionalCols.has("reorder") && <th scope="col" className="sinv-num">Reorder</th>}
                    {optionalCols.has("lastMovement") && <th scope="col">Last Movement</th>}
                    {!locationId && <th scope="col" className="sinv-num">Locations</th>}
                    <th scope="col">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => {
                    const status = statusOf(row);
                    const available = row.availableQty ?? row.onHandQty;
                    return (
                      <tr key={row.materialId} className="sinv-row-button" onClick={() => setSelectedId(row.materialId)}>
                        <td><span className="sila-cell-strong">{row.materialCode}</span></td>
                        <td>{row.description && row.description !== row.materialCode ? row.description : row.description || "—"}</td>
                        <td className="sinv-num">{formatQty(available)}</td>
                        <td>{row.baseUom}</td>
                        <td><span className={`sila-badge ${status.badge}`}>{status.label}</span></td>
                        {optionalCols.has("onHand") && <td className="sinv-num">{formatQty(row.onHandQty)}</td>}
                        {optionalCols.has("reserved") && <td className="sinv-num">{formatQty(row.reservedQty ?? 0)}</td>}
                        {optionalCols.has("inTransit") && <td className="sinv-num">{formatQty(row.inTransitQty)}</td>}
                        {optionalCols.has("reorder") && <td className="sinv-num">{row.reorderPoint != null ? formatQty(row.reorderPoint) : "—"}</td>}
                        {optionalCols.has("lastMovement") && (
                          <td>{row.lastMovementOn ? new Date(row.lastMovementOn).toLocaleDateString() : "—"}</td>
                        )}
                        {!locationId && <td className="sinv-num">{row.locationCount}</td>}
                        <td onClick={(event) => event.stopPropagation()}>
                          <div className="sinv-actions">
                            <button
                              type="button"
                              className="sila-btn sila-btn--ghost sila-btn--sm"
                              aria-label="Actions"
                              onClick={() => setMenuFor(menuFor === row.materialId ? null : row.materialId)}
                            >
                              ···
                            </button>
                            {menuFor === row.materialId && (
                              <div className="sinv-actions-menu" role="menu">
                                <button type="button" role="menuitem" onClick={() => { setSelectedId(row.materialId); setMenuFor(null); }}>View Details</button>
                                {canTransfer && (
                                  <button type="button" role="menuitem" onClick={() => { setSelectedId(row.materialId); setMenuFor(null); }}>Transfer / ITO</button>
                                )}
                                {canRequestPurchase && (
                                  <button type="button" role="menuitem" onClick={() => { setSelectedId(row.materialId); setMenuFor(null); }}>Request Stock</button>
                                )}
                                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaTransactions"); setMenuFor(null); }}>Movement History</button>
                                {canManageCount && (
                                  <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaStockCounts"); setMenuFor(null); }}>Stock Count</button>
                                )}
                              </div>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {hasMore && (
              <div className="sinv-pager">
                <span>{rows.length} materials</span>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" disabled={loadingMore} onClick={loadMore}>
                  {loadingMore ? "Loading…" : "Load more"}
                </button>
              </div>
            )}
          </>
        )}
      </section>
    </div>
  );
};

export default SilaLiveInventory;
