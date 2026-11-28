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
import SilaInventoryCommandCenter from "./SilaInventoryCommandCenter";
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
      viewMeStudio: false, createMenu: false, editDraftMenu: false, manageMenuItems: false,
      managePortionPlans: false, manageDemandPlans: false, viewPosSales: false, managePosMapping: false,
      viewMenuPerformance: false, viewMenuCost: false, viewMenuMargin: false, generateMenuPurchaseRec: false,
      useAiMenuIntelligence: false, runMenuSimulation: false, approveMenu: false, publishMenu: false, manageMenuSetup: false,
    })),
    [permissions, canTransfer, canPostGoodsIssue, canManageCount, canRequestPurchase],
  );

  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [locationsError, setLocationsError] = useState<string | null>(null);
  const [search, setSearch] = useState(initialSearch);
  const [health, setHealth] = useState(initialHealth || defaultLens.defaultHealth || "");
  const [productClass, setProductClass] = useState("");
  const [stockCountOpen, setStockCountOpen] = useState(false);
  const [stockCountTargets, setStockCountTargets] = useState<
    { materialId?: string; materialCode: string; description?: string; onHandQty?: number | null; uom?: string | null }[]
  >([]);
  const [stockCountCode, setStockCountCode] = useState("");
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
  /** Source location chosen on the material-view stock table (right side). */
  const [transferFromId, setTransferFromId] = useState("");
  const [requiredQty, setRequiredQty] = useState("");
  const [detailKey, setDetailKey] = useState(0);
  const [pickingId, setPickingId] = useState<string | null>(null);
  const [pendingCount, setPendingCount] = useState(0);
  const [showPending, setShowPending] = useState(false);
  /** KPI focus on this screen: transit / pending stay here (no route change). */
  const [panelFocus, setPanelFocus] = useState<"" | "transit" | "pending">("");
  const [lastSynced, setLastSynced] = useState<Date | null>(null);
  const [refreshTick, setRefreshTick] = useState(0);
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
        const data = await searchLiveInventory(search, locationId || null, 0, health || null, productClass || null);
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
  }, [search, locationId, health, productClass, canSearch, refreshTick]);

  useEffect(() => {
    if (!selectedId) {
      setTransferFromId("");
      return;
    }
    // Default destination to the command-center location when opening a material.
    if (!transferToId && locationId) setTransferToId(locationId);
    let active = true;
    setDetailError(null);
    setDetailLoading(true);
    const timer = window.setTimeout(async () => {
      try {
        const required = Number(requiredQty);
        const data = await getLiveInventoryDetail(selectedId, {
          requiredQty: Number.isFinite(required) && required > 0 ? required : undefined,
          // Destination drives shortage / transferable sources on the right.
          currentLocationId: transferToId || locationId || undefined,
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
      const data = await searchLiveInventory(search, locationId || null, rows.length, health || null, productClass || null);
      setRows((current) => [...current, ...data]);
      setHasMore(data.length === SILA_LIVE_PAGE_SIZE);
      setLastSynced(new Date());
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not search the inventory.");
    } finally {
      setLoadingMore(false);
    }
  };

  const focusTransitPanel = () => {
    setHealth("");
    setPanelFocus("transit");
    setSelectedId(null);
  };

  const focusPendingPanel = () => {
    setPanelFocus("pending");
    setSelectedId(null);
  };

  const runPriority = (action: string) => {
    if (action === "LOW") {
      setHealth("LOW");
      setPanelFocus("");
      setSelectedId(null);
      return;
    }
    if (action === "TRANSIT" || action === "TRANSFERS") {
      focusTransitPanel();
      return;
    }
    if (action === "PENDING" || action === "ALERTS") {
      focusPendingPanel();
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

  const openStockCount = (targets?: typeof stockCountTargets, code?: string) => {
    if (!locationId) {
      toastService.error("Select a location before stock count.");
      return;
    }
    const selected = targets ?? [];
    const fromSearch = (code ?? search).trim();
    setStockCountTargets(selected);
    setStockCountCode(selected.length > 0 ? "" : fromSearch);
    setStockCountOpen(true);
  };

  const destinationId = transferToId || locationId;

  const samePlantAsDestination = (row: SilaLiveInventoryLocation): boolean => {
    const destination = locations.find((location) => location.id === destinationId);
    const destDetail = detail?.locations?.find((location) => location.locationId === destinationId);
    const destPlant = (destination?.plantCode ?? destDetail?.propertyCode ?? "").trim().toUpperCase();
    const sourcePlant = (row.propertyCode ?? "").trim().toUpperCase();
    return Boolean(destPlant && sourcePlant && destPlant === sourcePlant);
  };

  const sendTransfer = async (row: SilaLiveInventoryLocation, quick: boolean) => {
    if (!canTransfer || !detail) return;
    const destination = locations.find((location) => location.id === destinationId);
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
    if (quick && !samePlantAsDestination(row)) {
      toastService.error("Quick ITO is same-plant store-to-store only. Use New Transfer for plant-to-plant (301).");
      return;
    }
    const required = Number(requiredQty) || 0;
    // Quick ITO always moves 1 (or less if transferable is below 1). New Transfer uses required / shortage / full transferable.
    const wanted = quick
      ? 1
      : (detail.shortage ?? 0) > 0
        ? (detail.shortage as number)
        : required > 0
          ? required
          : transferable;
    const quantity = Math.min(wanted, transferable);
    if (quantity <= 0) {
      toastService.error("Enter a quantity greater than zero for New Transfer.");
      return;
    }
    setPickingId(`${row.locationId}:${quick ? "quick" : "ito"}`);
    try {
      const payload = {
        fromLocationId: row.locationId,
        toLocationId: destination.id,
        reason: quick ? "Guest service" : `Transfer from ${row.locationName}`,
        requiredBy: null,
        items: [{ materialId: detail.materialId, quantity, uom: null }],
        addToLocation: !quick,
      };
      if (quick) await createQuickTransfer(payload);
      else await createTransfer(payload);
      toastService.success(
        `${quick ? "Quick ITO" : "New Transfer"} of ${formatQty(quantity)} ${detail.baseUom} from ${row.locationName} to ${destination.locationCode} raised.`,
      );
      setDetailKey((key) => key + 1);
      setTransferFromId("");
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not request the transfer.");
    } finally {
      setPickingId(null);
    }
  };

  const sourceRows = (detail?.locations ?? []).filter(
    (row) => row.onHandQty !== 0 || row.inTransitQty !== 0 || (row.reservedQty ?? 0) !== 0 || row.locationId === locationId,
  );

  const materialLabel = (code: string, description?: string | null) =>
    description && description !== code ? description : description || "—";

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
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => { setSelectedId(null); setDetail(null); }}>
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
                label={canTransfer ? "Destination (transfer / stock count)" : "Location (stock count)"}
                placeholder="Select destination"
                value={destinationId}
                clearable
                options={stockLocations.map((location) => ({
                  value: location.id,
                  label: `${location.locationCode} · ${location.locationName}${location.plantCode ? ` · Plant ${location.plantCode}` : ""}`,
                }))}
                onChange={(value) => {
                  // Destination only — source is chosen from the stock table on the right.
                  setTransferToId(value);
                  setTransferFromId("");
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
                <p>Select a source row, then Quick ITO (1 EA, same plant) or New Transfer (quantity / plant-to-plant 301).</p>
              </div>
              {sourceRows.length === 0 ? (
                <EmptyState title="No stock rows" description="This material has no balances in your locations." />
              ) : (
                <div className="sila-table-wrap liv-table-wrap">
                  <table className="sila-table liv-table">
                    <thead>
                      <tr>
                        {canTransfer && <th scope="col">Source</th>}
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
                        const canPick = row.transferEnabled && (row.transferableQty ?? 0) > 0 && row.locationId !== destinationId;
                        const selected = transferFromId === row.locationId;
                        const allowQuick = canPick && samePlantAsDestination(row);
                        return (
                          <tr
                            key={row.locationId}
                            className={selected ? "liv-row--source" : undefined}
                            onClick={() => {
                              if (!canTransfer || !canPick) return;
                              setTransferFromId(row.locationId);
                            }}
                            style={canTransfer && canPick ? { cursor: "pointer" } : undefined}
                          >
                            {canTransfer && (
                              <td>
                                <input
                                  type="radio"
                                  name="liv-transfer-source"
                                  checked={selected}
                                  disabled={!canPick}
                                  onChange={() => canPick && setTransferFromId(row.locationId)}
                                  onClick={(event) => event.stopPropagation()}
                                  aria-label={`Select ${row.locationCode} as transfer source`}
                                />
                              </td>
                            )}
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
                              <td onClick={(event) => event.stopPropagation()}>
                                {canPick && (
                                  <span className="sinv-inline">
                                    {allowQuick && (
                                      <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" disabled={pickingId !== null} onClick={() => sendTransfer(row, true)}>
                                        {pickingId === `${row.locationId}:quick` ? "…" : "Quick ITO"}
                                      </button>
                                    )}
                                    <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" disabled={pickingId !== null} onClick={() => sendTransfer(row, false)}>
                                      {pickingId === `${row.locationId}:ito` ? "…" : "New Transfer"}
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

  const pendingPanel =
    panelFocus === "pending" ? (
      <section className="liv-panel" data-testid="liv-pending-panel">
        <div className="liv-panel__head liv-panel__head--row">
          <div>
            <h2>Pending Actions</h2>
            <p>Approvals, alerts, and open work for this location — shown here without leaving Live Inventory.</p>
          </div>
          <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => setPanelFocus("")}>
            Clear
          </button>
        </div>
        {priorities.length === 0 && (alerts?.length ?? 0) === 0 ? (
          <EmptyState title="No pending actions" description="Nothing needs attention for this location right now." />
        ) : (
          <ul className="liv-priority__list" style={{ margin: "0 1rem 1rem" }}>
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
            {(alerts ?? []).slice(0, 5).map((alert) => (
              <li key={alert.id} className="liv-priority__item liv-priority__item--warning">
                <div>
                  <strong>{alert.title || alert.alertType}</strong>
                  <p>{alert.message || alert.recommendedAction || "Open the material for next steps."}</p>
                </div>
                {alert.materialId ? (
                  <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => setSelectedId(alert.materialId!)}>
                    View material
                  </button>
                ) : null}
              </li>
            ))}
          </ul>
        )}
      </section>
    ) : null;

  return (
    <div className="sila-me liv-page liv-page--command-center">
      <SilaInventoryCommandCenter
        stockLocations={stockLocations}
        locationId={locationId}
        currentLocation={currentLocation}
        locationsError={locationsError}
        home={home}
        rows={rows}
        loading={loading}
        loadingMore={loadingMore}
        hasMore={hasMore}
        error={error}
        canSearch={canSearch}
        freshness={freshness}
        pendingCount={pendingCount}
        productClass={productClass}
        health={health}
        canTransfer={canTransfer}
        canManageCount={canManageCount}
        canPostGoodsIssue={canPostGoodsIssue}
        canRequestPurchase={canRequestPurchase}
        initialSearch={initialSearch}
        onSearchChange={setSearch}
        onLocationChange={setLocationId}
        onProductClassChange={setProductClass}
        onHealthChange={(value) => {
          setHealth(value);
          setPanelFocus("");
        }}
        onLoadMore={loadMore}
        onRefresh={() => setRefreshTick((n) => n + 1)}
        onOpenMaterial={setSelectedId}
        onOpenPending={() => setShowPending(true)}
        onFocusPending={focusPendingPanel}
        onNavigate={onNavigate}
        pendingPanel={pendingPanel}
      />

      <SilaPendingRequirementsPanel
        open={showPending}
        onClose={() => setShowPending(false)}
        canTransfer={canTransfer}
        locations={locations}
        sourceLocationId={locationId}
        onCreated={() => setRefreshTick((n) => n + 1)}
      />
    </div>
  );
};

export default SilaLiveInventory;
