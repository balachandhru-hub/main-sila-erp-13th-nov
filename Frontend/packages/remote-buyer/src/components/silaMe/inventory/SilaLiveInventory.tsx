import React, { useEffect, useMemo, useRef, useState } from "react";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  SILA_LIVE_PAGE_SIZE,
  formatQty,
  getInventoryHome,
  getLiveInventoryDetail,
  getMyLocations,
  getPendingOutletRequirements,
  getRecentAlerts,
  searchLiveInventory,
  silaLabel,
  type SilaInventoryHome,
  type SilaLiveInventoryDetail,
  type SilaLiveInventoryLocation,
  type SilaLiveInventoryRow,
  type SilaLocation,
} from "../../../api/silaMe/silaInventoryApi";
import type { SilaAlert } from "../../../api/silaMe/silaStockCountApi";
import { createQuickTransfer, createTransfer } from "../../../api/silaMe/silaMovementsApi";
import SilaSearchableSelect from "../SilaSearchableSelect";
import type { SilaMePermissions } from "../silaMePermissions";
import SilaLiveDecisionPanel from "./SilaLiveDecisionPanel";
import SilaLiveStockCountPanel from "./SilaLiveStockCountPanel";
import SilaPendingRequirementsPanel from "./SilaPendingRequirementsPanel";
import { resolveLiveInventoryLens } from "./liveInventoryLens";
import { buildInventoryPriorities } from "./liveInventoryPriorities";
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

const STATUS_TEXT: Record<string, string> = {
  NEGATIVE: "Negative",
  OUT: "Out of stock",
  LOW: "Low stock",
  HEALTHY: "Available",
  EXCESS: "Excess",
  IN_STOCK: "Available",
};

const STATUS_BADGE: Record<string, string> = {
  NEGATIVE: "liv-badge liv-badge--critical",
  OUT: "liv-badge liv-badge--critical",
  LOW: "liv-badge liv-badge--warn",
  HEALTHY: "liv-badge liv-badge--ok",
  EXCESS: "liv-badge liv-badge--info",
  IN_STOCK: "liv-badge liv-badge--ok",
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
  canPostGoodsIssue?: boolean;
  permissions?: SilaMePermissions;
  initialSearch?: string;
  initialHealth?: string;
  onNavigate?: (key: string) => void;
}

/**
 * Daylight Alabaster Live Inventory command center.
 * Reuses existing live inventory, transfer, and alert APIs — no duplicate ledger.
 */
const SilaLiveInventory: React.FC<SilaLiveInventoryProps> = ({
  canTransfer = true,
  canRequestPurchase = false,
  canManageCount = false,
  canPostGoodsIssue = false,
  permissions,
  initialSearch = "",
  initialHealth = "",
  onNavigate,
}) => {
  const defaultLens = useMemo(
    () => (permissions ? resolveLiveInventoryLens(permissions) : resolveLiveInventoryLens({
      manageLocation: false, manageMasterData: false, manageTransfer: canTransfer, approveTransfer: false,
      postGoodsIssue: canPostGoodsIssue, postAdjustment: false, manageCount: canManageCount, initiateCount: false,
      approveCount: false, requestPurchase: canRequestPurchase, manageRecipe: false, approveRecipe: false,
      approvePrice: false, managePos: false, viewReceiving: false, viewInventory: true, postGrn: false,
      manageErpPosting: false, viewAudit: false, viewInvoice: false, processInvoice: false, approveInvoice: false,
      viewCreditMemo: false, processCreditMemo: false, approveCreditMemo: false,
      viewMenuEngineering: false, manageMenuEngineering: false,
    })),
    [permissions, canTransfer, canPostGoodsIssue, canManageCount, canRequestPurchase],
  );

  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [locationsError, setLocationsError] = useState<string | null>(null);
  const [search, setSearch] = useState(initialSearch);
  const [health, setHealth] = useState(initialHealth || defaultLens.defaultHealth || "");
  const [showFilters, setShowFilters] = useState(false);
  const [prioritiesOpen, setPrioritiesOpen] = useState(false);
  const [stockCountOpen, setStockCountOpen] = useState(false);
  const [stockCountTargets, setStockCountTargets] = useState<
    { materialId?: string; materialCode: string; description?: string; onHandQty?: number | null; uom?: string | null }[]
  >([]);
  const [stockCountCode, setStockCountCode] = useState("");
  const [scanMode, setScanMode] = useState<"barcode" | "photo">("barcode");
  const [locationId, setLocationId] = useState("");
  const [home, setHome] = useState<SilaInventoryHome | null>(null);
  const [alerts, setAlerts] = useState<SilaAlert[] | null>(null);
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
  const [showMore, setShowMore] = useState(false);
  const [pendingCount, setPendingCount] = useState(0);
  const [showPending, setShowPending] = useState(false);
  const [basket, setBasket] = useState<Record<string, SilaLiveInventoryRow>>({});
  const [itoToId, setItoToId] = useState("");
  const [creatingBasket, setCreatingBasket] = useState(false);
  const [lastSynced, setLastSynced] = useState<Date | null>(null);
  const [refreshTick, setRefreshTick] = useState(0);
  const searchRef = useRef<HTMLInputElement>(null);
  const scanPhotoRef = useRef<HTMLInputElement>(null);
  const abortRef = useRef<AbortController | null>(null);

  useEffect(() => {
    if (!initialHealth && defaultLens.defaultHealth) setHealth(defaultLens.defaultHealth);
  }, [defaultLens, initialHealth]);

  useEffect(() => {
    let active = true;
    getMyLocations()
      .then((data) => {
        if (!active) return;
        setLocations(data);
        const homeLoc = homeLocation(data);
        if (homeLoc) setLocationId(homeLoc.id);
      })
      .catch((err: unknown) => {
        if (active) setLocationsError(err instanceof Error ? err.message : "Could not load your locations.");
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    let active = true;
    getPendingOutletRequirements({ sourceLocationId: locationId || undefined, countOnly: true })
      .then((data) => {
        if (active) setPendingCount(data.count);
      })
      .catch(() => {
        if (active) setPendingCount(0);
      });
    return () => {
      active = false;
    };
  }, [locationId, refreshTick]);

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
    getRecentAlerts(5)
      .then((data) => {
        if (active) setAlerts(data);
      })
      .catch(() => {
        if (active) setAlerts(null);
      });
    return () => {
      active = false;
    };
  }, [locationId, refreshTick]);

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
        setLastSynced(new Date());
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
  }, [search, locationId, health, canSearch, refreshTick]);

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
  const priorities = useMemo(() => buildInventoryPriorities(home, alerts), [home, alerts]);

  const freshness = (() => {
    if (!lastSynced) return "Waiting for data";
    const seconds = Math.max(0, Math.round((Date.now() - lastSynced.getTime()) / 1000));
    if (seconds < 15) return "Live";
    if (seconds < 120) return `Updated ${seconds}s ago`;
    return "Refresh recommended";
  })();

  const loadMore = async () => {
    setLoadingMore(true);
    try {
      const data = await searchLiveInventory(search, locationId || null, rows.length, health || null);
      setRows((current) => [...current, ...data]);
      setHasMore(data.length === SILA_LIVE_PAGE_SIZE);
      setLastSynced(new Date());
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

  const runPriority = (action: string) => {
    if (action === "LOW") {
      setHealth("LOW");
      setSelectedId(null);
      return;
    }
    if (action === "TRANSIT" || action === "TRANSFERS") {
      onNavigate?.("silaTransfers");
      return;
    }
    if (action === "PENDING" || action === "ALERTS") {
      onNavigate?.("silaAlerts");
      return;
    }
    if (action === "COUNTS") {
      onNavigate?.("silaStockCounts");
      return;
    }
    if (action === "RECIPE") {
      onNavigate?.("silaRecipeAvailability");
    }
  };

  const toggleBasket = (row: SilaLiveInventoryRow) => {
    setBasket((current) => {
      const next = { ...current };
      if (next[row.materialId]) delete next[row.materialId];
      else next[row.materialId] = row;
      return next;
    });
  };

  const openStockCount = (targets?: typeof stockCountTargets, code?: string) => {
    if (!locationId) {
      toastService.error("Select a location before stock count.");
      return;
    }
    const selected =
      targets ??
      Object.values(basket).map((row) => ({
        materialId: row.materialId,
        materialCode: row.materialCode,
        description: row.description,
        onHandQty: row.onHandQty,
        uom: row.baseUom,
      }));
    const fromSearch = (code ?? search).trim();
    setStockCountTargets(selected);
    setStockCountCode(selected.length > 0 ? "" : fromSearch);
    setStockCountOpen(true);
  };

  const createBasketIto = async () => {
    if (!canTransfer || !locationId || !itoToId) {
      toastService.error("Choose source and destination before creating an ITO.");
      return;
    }
    const items = Object.values(basket)
      .filter((row) => (row.availableQty ?? row.onHandQty ?? 0) > 0)
      .map((row) => {
        const available = row.availableQty ?? row.onHandQty ?? 0;
        return {
          materialId: row.materialId,
          quantity: Math.max(0.001, available),
          uom: row.baseUom ?? null,
        };
      });
    if (items.length === 0) {
      toastService.error("Select materials with transferable stock.");
      return;
    }
    setCreatingBasket(true);
    try {
      await createTransfer({
        fromLocationId: locationId,
        toLocationId: itoToId,
        reason: "Material-based ITO from Live Inventory",
        requiredBy: null,
        requireSamePlant: true,
        addToLocation: true,
        items,
      });
      toastService.success(`ITO created for ${items.length} material(s).`);
      setBasket({});
      setRefreshTick((n) => n + 1);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not create the ITO.");
    } finally {
      setCreatingBasket(false);
    }
  };

  const sendTransfer = async (row: SilaLiveInventoryLocation, quick: boolean) => {
    if (!canTransfer || !detail) return;
    const destination = locations.find((location) => location.id === transferToId);
    if (!destination) {
      toastService.error("Choose the destination before sending stock.");
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
      badge: STATUS_BADGE[code] ?? "liv-badge liv-badge--info",
    };
  };

  const materialLabel = (code: string, description?: string | null) =>
    description && description !== code ? description : description || "—";

  const kpis = [
    {
      key: "items",
      label: "Stock Items",
      value: home?.stockItems ?? "—",
      tone: "liv-kpi--sky",
      onClick: () => { setHealth(""); setSelectedId(null); },
    },
    {
      key: "low",
      label: "Critical / Low Stock",
      value: home?.lowStockItems ?? "—",
      tone: "liv-kpi--amber",
      onClick: () => runPriority("LOW"),
    },
    {
      key: "transit",
      label: "In Transit",
      value: home?.inTransitToReceive ?? "—",
      tone: "liv-kpi--teal",
      onClick: () => runPriority("TRANSIT"),
    },
    {
      key: "pending",
      label: "Pending Actions",
      value: home?.pendingActions ?? "—",
      tone: "liv-kpi--rose",
      onClick: () => runPriority("PENDING"),
    },
  ];

  if (selectedId) {
    return (
      <div className="sila-me liv-page">
        <header className="liv-command">
          <div className="liv-command__copy">
            <p className="liv-eyebrow">Material Inventory</p>
            <h1 className="liv-title">{detail?.materialCode ?? "Material"}</h1>
            <p className="liv-sub">{detail ? materialLabel(detail.materialCode, detail.description) : "Loading…"}</p>
          </div>
          <div className="liv-command__actions">
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => { setSelectedId(null); setDetail(null); setMenuFor(null); }}>
              ← Live Inventory
            </button>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate?.("silaTransactions")}>
              Movement History
            </button>
            {canTransfer && (
              <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate?.("silaTransfers")}>
                Transfer Center
              </button>
            )}
            {canPostGoodsIssue && (
              <button type="button" className="sila-btn sila-btn--secondary" onClick={() => onNavigate?.("silaGoodsIssue")}>
                Goods Issue
              </button>
            )}
            {canManageCount && (
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={!transferToId && !locationId}
                onClick={() => {
                  const countLocationId = transferToId || locationId;
                  if (!countLocationId) {
                    toastService.error("Select a location in the dropdown before stock count.");
                    return;
                  }
                  const atLocation = sourceRows.find((row) => row.locationId === countLocationId);
                  openStockCount(
                    [
                      {
                        materialId: detail?.materialId ?? selectedId ?? undefined,
                        materialCode: detail?.materialCode ?? "",
                        description: detail?.description,
                        onHandQty: atLocation?.onHandQty ?? detail?.onHandQty,
                        uom: detail?.baseUom,
                      },
                    ].filter((item) => item.materialCode),
                  );
                }}
              >
                Stock Count
              </button>
            )}
          </div>
        </header>

        {(canTransfer || canManageCount) && (
          <section className="liv-panel liv-panel--flush">
            <div className="liv-transfer-dest">
              <SilaSearchableSelect
                label={canTransfer ? "Location (transfer / stock count)" : "Location (stock count)"}
                placeholder="Select location"
                value={transferToId || locationId}
                clearable
                options={stockLocations.map((location) => ({
                  value: location.id,
                  label: `${location.locationCode} · ${location.locationName}`,
                }))}
                onChange={(value) => {
                  setTransferToId(value);
                  if (value) setLocationId(value);
                }}
              />
            </div>
          </section>
        )}

        {detailLoading && !detail ? (
          <Loader size={28} message="Loading material…" />
        ) : detailError ? (
          <EmptyState variant="error" title="Couldn't load material" description={detailError} />
        ) : detail ? (
          <div className="liv-detail-grid">
            <section className="liv-panel">
              <div className="liv-panel__head">
                <h2>Overview</h2>
                <p>Available = on hand minus valid reservations. Incoming is not counted until receipt.</p>
              </div>
              <dl className="liv-facts">
                <div><dt>Material</dt><dd>{detail.materialCode}</dd></div>
                <div><dt>Description</dt><dd>{materialLabel(detail.materialCode, detail.description)}</dd></div>
                <div><dt>Base UOM</dt><dd>{detail.baseUom}</dd></div>
                <div><dt>On hand</dt><dd className="liv-num">{formatQty(detail.onHandQty)}</dd></div>
                <div><dt>Available</dt><dd className="liv-num">{formatQty(detail.localAvailable ?? detail.onHandQty)}</dd></div>
                <div><dt>In transit</dt><dd className="liv-num">{formatQty(detail.inTransitQty)}</dd></div>
              </dl>
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
            <section className="liv-panel">
              <div className="liv-panel__head">
                <h2>Stock by location</h2>
                <p>Authorized properties, plants, storage locations, and outlets.</p>
              </div>
              {sourceRows.length === 0 ? (
                <EmptyState title="No stock rows" description="This material has no balances in your locations." />
              ) : (
                <div className="sila-table-wrap liv-table-wrap">
                  <table className="sila-table liv-table">
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
                              <span className={STATUS_BADGE[code] ?? "liv-badge liv-badge--info"}>
                                {STATUS_TEXT[code] ?? silaLabel(code)}
                              </span>
                            </td>
                            {canTransfer && (
                              <td>
                                {canPick && (
                                  <span className="sinv-inline">
                                    <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" disabled={pickingId !== null} onClick={() => sendTransfer(row, true)}>
                                      {pickingId === `${row.locationId}:quick` ? "…" : "Quick ITO"}
                                    </button>
                                    <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" disabled={pickingId !== null} onClick={() => sendTransfer(row, false)}>
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

        {canManageCount && (
          <SilaLiveStockCountPanel
            open={stockCountOpen}
            locationId={transferToId || locationId}
            locationLabel={
              stockLocations.find((location) => location.id === (transferToId || locationId))
                ? `${stockLocations.find((location) => location.id === (transferToId || locationId))!.locationCode} · ${stockLocations.find((location) => location.id === (transferToId || locationId))!.locationName}`
                : "Selected location"
            }
            initialCode={stockCountCode}
            targets={stockCountTargets}
            onClose={() => setStockCountOpen(false)}
            onPosted={() => {
              setDetailKey((key) => key + 1);
              setRefreshTick((n) => n + 1);
            }}
          />
        )}
      </div>
    );
  }

  return (
    <div className="sila-me liv-page">
      {/* AREA A — Compact command bar */}
      <header className="liv-command">
        <div className="liv-command__copy">
          <p className="liv-eyebrow">Live Inventory</p>
          <h1 className="liv-title">Inventory Command Center</h1>
          <p className="liv-sub">
            {currentLocation
              ? [
                  home?.propertyName || currentLocation.propertyName,
                  `${currentLocation.locationCode} — ${currentLocation.locationName}`,
                  home?.plantCode || currentLocation.plantCode
                    ? `Plant ${home?.plantCode || currentLocation.plantCode}`
                    : null,
                  home?.storageLocationCode || currentLocation.storageLocationCode,
                ]
                  .filter(Boolean)
                  .join(" · ")
              : locationsError ?? "Select a property / outlet"}
          </p>
        </div>
        <div className="liv-command__actions">
          <div className="liv-location-select">
            <SilaSearchableSelect
              label="Location"
              placeholder="Store / outlet"
              value={locationId}
              clearable
              options={stockLocations.map((location) => ({
                value: location.id,
                label: `${location.locationCode} · ${location.locationName}`,
              }))}
              onChange={setLocationId}
            />
          </div>
          <button
            type="button"
            className="liv-chip"
            data-testid="pending-requirements-indicator"
            onClick={() => setShowPending(true)}
            title="Pending Weekly Bucket / Wishlist requirements"
          >
            Pending Requirements: {pendingCount}
          </button>
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => { setRefreshTick((n) => n + 1); searchRef.current?.focus(); }}>
            Refresh
          </button>
          <div className="liv-more">
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowMore((v) => !v)}>
              More
            </button>
            {showMore && (
              <div className="liv-more-menu" role="menu">
                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaStockHealth"); setShowMore(false); }}>Stock Health</button>
                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaTransfers"); setShowMore(false); }}>Transfer Center</button>
                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaRecipeAvailability"); setShowMore(false); }}>Recipe Availability</button>
                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaInventoryAnalytics"); setShowMore(false); }}>Analytics</button>
                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaTransactions"); setShowMore(false); }}>Movement History</button>
              </div>
            )}
          </div>
          <span className={`liv-fresh${freshness.startsWith("Refresh") ? " liv-fresh--stale" : ""}`} title="Based on last successful inventory query">
            {freshness}
          </span>
        </div>
      </header>

      {/* AREA B — Universal search + scan / stock count */}
      <section className="liv-search-panel">
        <label className="liv-search-label" htmlFor="liv-search">Search inventory</label>
        <div className="liv-search-row">
          <input
            ref={searchRef}
            id="liv-search"
            className="liv-search-input"
            type="search"
            placeholder="Material number, description, barcode…"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && search.trim()) {
                event.preventDefault();
                toastService.success(`Material ${search.trim()} ready for stock count`);
              }
            }}
          />
          <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setShowFilters((v) => !v)}>
            {showFilters ? "Hide filters" : "Filters"}
          </button>
          {canManageCount && (
            <>
              <div className="liv-scan-modes" role="group" aria-label="Scan method">
                <button
                  type="button"
                  className={`liv-chip${scanMode === "barcode" ? " liv-chip--on" : ""}`}
                  onClick={() => {
                    setScanMode("barcode");
                    searchRef.current?.focus();
                  }}
                >
                  Scan barcode
                </button>
                <button
                  type="button"
                  className={`liv-chip${scanMode === "photo" ? " liv-chip--on" : ""}`}
                  onClick={() => {
                    setScanMode("photo");
                    scanPhotoRef.current?.click();
                  }}
                >
                  Take photo
                </button>
                <input
                  ref={scanPhotoRef}
                  type="file"
                  accept="image/*"
                  capture="environment"
                  className="liv-sc-file"
                  onChange={(event) => {
                    if (event.target.files?.[0]) {
                      toastService.info("Photo captured. Enter the material number in Search inventory.");
                      searchRef.current?.focus();
                    }
                    event.target.value = "";
                  }}
                />
              </div>
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                disabled={!locationId}
                onClick={() => openStockCount()}
              >
                Stock Count
              </button>
            </>
          )}
        </div>
        {showFilters && (
          <div className="liv-filters">
            <label className="sila-field">
              <span className="sila-label">Stock status</span>
              <select className="sila-select" value={health} onChange={(event) => setHealth(event.target.value)}>
                <option value="">All</option>
                <option value="LOW">Low / critical</option>
                <option value="OUT">Out of stock</option>
                <option value="NEGATIVE">Negative</option>
                <option value="EXCESS">Excess</option>
                <option value="HEALTHY">Healthy</option>
              </select>
            </label>
            {health && (
              <button type="button" className="liv-chip" onClick={() => setHealth("")}>
                Clear status ×
              </button>
            )}
          </div>
        )}
      </section>

      {/* AREA C — Four KPIs */}
      <div className="liv-kpi-row">
        {kpis.map((kpi) => (
          <button key={kpi.key} type="button" className={`liv-kpi ${kpi.tone}`} onClick={kpi.onClick}>
            <span>{kpi.label}</span>
            <strong>{kpi.value}</strong>
          </button>
        ))}
      </div>

      {/* AREA E — Clean table */}
      <section className="liv-panel" aria-live="polite">
        <div className="liv-panel__head liv-panel__head--row">
          <div>
            <h2>Material availability</h2>
            <p>Default columns stay compact. Open a row for transfers, history, and linked workflows.</p>
          </div>
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
            {canTransfer && Object.keys(basket).length > 0 && (
              <>
                <SilaSearchableSelect
                  label="ITO destination"
                  placeholder="Destination"
                  value={itoToId}
                  options={stockLocations
                    .filter((location) => location.id !== locationId)
                    .map((location) => ({
                      value: location.id,
                      label: `${location.locationCode} · ${location.locationName}`,
                    }))}
                  onChange={setItoToId}
                />
                <button
                  type="button"
                  className="sila-btn sila-btn--primary sila-btn--sm"
                  disabled={creatingBasket || !itoToId}
                  onClick={createBasketIto}
                >
                  {creatingBasket ? "Creating…" : `Create ITO (${Object.keys(basket).length})`}
                </button>
              </>
            )}
            {canTransfer && (
              <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onNavigate?.("silaTransfers")}>
                New Transfer
              </button>
            )}
            {priorities.length > 0 && (
              <div className="liv-priority-alert">
                <button
                  type="button"
                  className={`liv-alert-icon${prioritiesOpen ? " liv-alert-icon--open" : ""}`}
                  aria-expanded={prioritiesOpen}
                  aria-label={`Inventory Priorities (${priorities.length})`}
                  title="Inventory Priorities"
                  onClick={() => setPrioritiesOpen((v) => !v)}
                >
                  <span aria-hidden>!</span>
                  <em>{priorities.length}</em>
                </button>
                {prioritiesOpen && (
                  <div className="liv-priority-pop" role="dialog" aria-label="Inventory Priorities">
                    <strong>Inventory Priorities</strong>
                    <ul className="liv-priority__list">
                      {priorities.map((item) => (
                        <li key={item.id} className={`liv-priority__item liv-priority__item--${item.tone}`}>
                          <div>
                            <strong>{item.title}</strong>
                            <p>{item.detail}</p>
                          </div>
                          <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => runPriority(item.action)}>
                            {item.actionLabel}
                          </button>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
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
          <EmptyState title="No materials found" description="Try another search or clear filters." />
        ) : (
          <>
            <div className="sila-table-wrap liv-table-wrap">
              <table className="sila-table liv-table">
                <thead>
                  <tr>
                    {(canTransfer || canManageCount) && <th scope="col">{canTransfer ? "ITO" : "Select"}</th>}
                    <th scope="col">Material</th>
                    <th scope="col">Description</th>
                    <th scope="col" className="sinv-num">Available</th>
                    <th scope="col">UOM</th>
                    <th scope="col">Status</th>
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
                        {(canTransfer || canManageCount) && (
                          <td onClick={(event) => event.stopPropagation()}>
                            <input
                              type="checkbox"
                              aria-label={`Select ${row.materialCode}`}
                              checked={!!basket[row.materialId]}
                              onChange={() => toggleBasket(row)}
                            />
                          </td>
                        )}
                        <td><span className="sila-cell-strong">{row.materialCode}</span></td>
                        <td>{materialLabel(row.materialCode, row.description)}</td>
                        <td className="sinv-num">{formatQty(available)}</td>
                        <td>{row.baseUom}</td>
                        <td><span className={status.badge}>{status.label}</span></td>
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
                                {canPostGoodsIssue && (
                                  <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaGoodsIssue"); setMenuFor(null); }}>Goods Issue</button>
                                )}
                                <button type="button" role="menuitem" onClick={() => { onNavigate?.("silaTransactions"); setMenuFor(null); }}>Movement History</button>
                                {canManageCount && (
                                  <button
                                    type="button"
                                    role="menuitem"
                                    onClick={() => {
                                      openStockCount([
                                        {
                                          materialId: row.materialId,
                                          materialCode: row.materialCode,
                                          description: row.description,
                                          onHandQty: row.onHandQty,
                                          uom: row.baseUom,
                                        },
                                      ]);
                                      setMenuFor(null);
                                    }}
                                  >
                                    Stock Count
                                  </button>
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
                <span className="liv-num">{rows.length} materials</span>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" disabled={loadingMore} onClick={loadMore}>
                  {loadingMore ? "Loading…" : "Load more"}
                </button>
              </div>
            )}
          </>
        )}
      </section>

      <SilaPendingRequirementsPanel
        open={showPending}
        onClose={() => setShowPending(false)}
        canTransfer={canTransfer}
        locations={locations}
        sourceLocationId={locationId}
        onCreated={() => setRefreshTick((n) => n + 1)}
      />

      {canManageCount && (
        <SilaLiveStockCountPanel
          open={stockCountOpen}
          locationId={locationId}
          locationLabel={
            stockLocations.find((location) => location.id === locationId)
              ? `${stockLocations.find((location) => location.id === locationId)!.locationCode} · ${stockLocations.find((location) => location.id === locationId)!.locationName}`
              : "Selected location"
          }
          initialCode={stockCountCode}
          targets={stockCountTargets}
          onClose={() => setStockCountOpen(false)}
          onPosted={() => setRefreshTick((n) => n + 1)}
        />
      )}
    </div>
  );
};

export default SilaLiveInventory;
