import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  ArrowLeftRight,
  Camera,
  CigaretteOff,
  ConciergeBell,
  LayoutGrid,
  Minus,
  Package,
  Plus,
  RefreshCw,
  ScanEye,
  ScanLine,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Table2,
  Truck,
  Utensils,
  Wallet,
  Wine,
  X,
} from "lucide-react";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  createLiveStockCount,
  formatQty,
  getInventoryTransactions,
  getLiveInventoryDetail,
  silaLabel,
  type SilaInventoryHome,
  type SilaInventoryTransaction,
  type SilaLiveInventoryDetail,
  type SilaLiveInventoryRow,
  type SilaLocation,
  type SilaProductClass,
} from "../../../api/silaMe/silaInventoryApi";
import { createTransfer } from "../../../api/silaMe/silaMovementsApi";
import SilaBarcodeScanDialog from "./SilaBarcodeScanDialog";
import SilaVisionAiModal from "./SilaVisionAiModal";
import {
  PRODUCT_CLASSES,
  applyChip,
  composeQty,
  formatMoney,
  isBottleItem,
  isCommandQuery,
  matchesOmniQuery,
  openBottleStats,
  parCoveragePct,
  productClassLabel,
  splitSealedFraction,
  sparklinePoints,
  stockValueOf,
  weeklyIssued,
  type VisionApplication,
} from "./liveInventoryModel";
import "./SilaInventoryCommandCenter.css";

type Lens = "vault" | "shelf" | "ledger";
type ClientChip = "" | "TRANSIT" | "OPEN";

const STATUS_TEXT: Record<string, string> = {
  NEGATIVE: "Negative",
  OUT: "Out of stock",
  LOW: "Low stock",
  HEALTHY: "Healthy",
  EXCESS: "Excess",
  IN_STOCK: "Healthy",
};

const STATUS_TONE: Record<string, string> = {
  NEGATIVE: "red",
  OUT: "red",
  LOW: "amber",
  HEALTHY: "green",
  EXCESS: "blue",
  IN_STOCK: "green",
};

const CLASS_ICON: Record<SilaProductClass, React.ReactNode> = {
  BEVERAGE: <Wine size={16} />,
  FOOD: <Utensils size={16} />,
  MINIBAR: <ConciergeBell size={16} />,
  TOBACCO: <CigaretteOff size={16} />,
  GENERAL: <Package size={16} />,
};

const classOf = (row: Pick<SilaLiveInventoryRow, "productClass">): SilaProductClass => row.productClass ?? "GENERAL";

const statusOf = (row: SilaLiveInventoryRow) => {
  const code = row.stockStatus || (row.onHandQty <= 0 ? "OUT" : "IN_STOCK");
  return { label: STATUS_TEXT[code] ?? silaLabel(code), tone: STATUS_TONE[code] ?? "blue" };
};

const parOf = (row: SilaLiveInventoryRow): number | null => row.parLevel ?? row.reorderPoint ?? null;

interface TransferLine {
  materialId: string;
  qty: string;
}

export interface SilaInventoryCommandCenterProps {
  stockLocations: SilaLocation[];
  locationId: string;
  currentLocation?: SilaLocation;
  locationsError: string | null;
  home: SilaInventoryHome | null;
  rows: SilaLiveInventoryRow[];
  loading: boolean;
  loadingMore: boolean;
  hasMore: boolean;
  error: string | null;
  canSearch: boolean;
  freshness: string;
  pendingCount: number;
  productClass: string;
  health: string;
  canTransfer: boolean;
  canManageCount: boolean;
  canPostGoodsIssue: boolean;
  canRequestPurchase: boolean;
  /** Free-text omni-search; commands such as "open bottles" are kept out of the server search. */
  onSearchChange: (term: string) => void;
  onLocationChange: (id: string) => void;
  onProductClassChange: (value: string) => void;
  onHealthChange: (value: string) => void;
  onLoadMore: () => void;
  onRefresh: () => void;
  onOpenMaterial: (materialId: string) => void;
  onOpenPending: () => void;
  onFocusPending: () => void;
  onNavigate?: (key: string) => void;
  /** Pending actions panel shown under the KPI cards. */
  pendingPanel?: React.ReactNode;
  initialSearch?: string;
}

/**
 * Live Inventory command center: bento KPIs, omni-search (barcode scan, photo, Vision AI),
 * Spatial Vault / Shelf Count / Executive Ledger lenses and a material drawer.
 * All data comes from the existing live inventory, transfer and stock count APIs.
 */
const SilaInventoryCommandCenter: React.FC<SilaInventoryCommandCenterProps> = ({
  stockLocations,
  locationId,
  currentLocation,
  locationsError,
  home,
  rows,
  loading,
  loadingMore,
  hasMore,
  error,
  canSearch,
  freshness,
  pendingCount,
  productClass,
  health,
  canTransfer,
  canManageCount,
  canPostGoodsIssue,
  canRequestPurchase,
  onSearchChange,
  onLocationChange,
  onProductClassChange,
  onHealthChange,
  onLoadMore,
  onRefresh,
  onOpenMaterial,
  onOpenPending,
  onFocusPending,
  onNavigate,
  pendingPanel,
  initialSearch = "",
}) => {
  const searchRef = useRef<HTMLInputElement>(null);
  const photoRef = useRef<HTMLInputElement>(null);
  const [lens, setLens] = useState<Lens>("vault");
  const [query, setQuery] = useState(initialSearch);
  const [clientChip, setClientChip] = useState<ClientChip>("");
  const [drawerId, setDrawerId] = useState<string | null>(null);
  const [scanOpen, setScanOpen] = useState(false);
  const [visionOpen, setVisionOpen] = useState(false);
  const [visionMaterialId, setVisionMaterialId] = useState<string | null>(null);
  const [visionFile, setVisionFile] = useState<File | null>(null);
  const [transferOpen, setTransferOpen] = useState(false);
  const [transferTo, setTransferTo] = useState("");
  const [transferLines, setTransferLines] = useState<TransferLine[]>([]);
  const [transferBusy, setTransferBusy] = useState(false);
  const [counts, setCounts] = useState<Record<string, number>>({});
  const [visionNotes, setVisionNotes] = useState<Record<string, string>>({});
  const [posting, setPosting] = useState(false);
  const [scanTarget, setScanTarget] = useState<string | null>(null);

  const commandQuery = isCommandQuery(query);

  useEffect(() => {
    onSearchChange(commandQuery ? "" : query.trim());
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [query, commandQuery]);

  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchRef.current?.focus();
      }
      if (event.key === "Escape") {
        setDrawerId(null);
        setTransferOpen(false);
        setScanOpen(false);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const activeChip: string = health === "LOW" ? "LOW" : clientChip || productClass || "ALL";

  const chooseChip = (chip: string) => {
    setClientChip("");
    if (chip === "ALL") {
      onProductClassChange("");
      onHealthChange("");
      setQuery("");
    } else if (chip === "LOW") {
      onProductClassChange("");
      onHealthChange("LOW");
    } else if (chip === "TRANSIT" || chip === "OPEN") {
      onProductClassChange("");
      onHealthChange("");
      setClientChip(chip);
    } else {
      onHealthChange("");
      onProductClassChange(chip);
    }
  };

  const visibleRows = useMemo(() => {
    let list = rows.filter((row) => matchesOmniQuery(row, commandQuery ? query : ""));
    if (clientChip) list = applyChip(list, clientChip);
    return list;
  }, [rows, commandQuery, query, clientChip]);

  const bottleRows = useMemo(() => rows.filter(isBottleItem), [rows]);
  const drawerRow = rows.find((row) => row.materialId === drawerId) ?? null;

  const physicalOf = useCallback(
    (row: SilaLiveInventoryRow): number => (counts[row.materialId] != null ? counts[row.materialId] : row.onHandQty),
    [counts],
  );
  const varianceOf = (row: SilaLiveInventoryRow) => Math.round((physicalOf(row) - row.onHandQty) * 1000) / 1000;
  const dirtyRows = useMemo(
    () => rows.filter((row) => counts[row.materialId] != null && Math.abs(counts[row.materialId] - row.onHandQty) > 0.0005),
    [rows, counts],
  );

  const setCount = (row: SilaLiveInventoryRow, qty: number) => {
    const next = Math.max(0, Math.round(qty * 1000) / 1000);
    setCounts((current) => ({ ...current, [row.materialId]: next }));
  };

  const resetToBook = (row: SilaLiveInventoryRow) => {
    setCounts((current) => {
      const next = { ...current };
      delete next[row.materialId];
      return next;
    });
    setVisionNotes((current) => {
      const next = { ...current };
      delete next[row.materialId];
      return next;
    });
  };

  const postCycleCount = async () => {
    if (!locationId || dirtyRows.length === 0) return;
    setPosting(true);
    const ok: string[] = [];
    const failed: string[] = [];
    const posted: string[] = [];
    for (const row of dirtyRows) {
      try {
        const note = visionNotes[row.materialId];
        const result = await createLiveStockCount({
          locationId,
          materialId: row.materialId,
          materialCodeOrBarcode: row.materialCode,
          physicalQty: counts[row.materialId],
          uom: row.baseUom,
          reason: `Live Inventory shelf count${note ? ` (${note})` : ""}`,
        });
        ok.push(`${result.materialCode}: ${formatQty(result.varianceQty)} → SAP ${result.movementType}`);
        posted.push(row.materialId);
      } catch (err: unknown) {
        failed.push(`${row.materialCode}: ${err instanceof Error ? err.message : "post failed"}`);
      }
    }
    setPosting(false);
    if (posted.length > 0) {
      setCounts((current) => {
        const next = { ...current };
        posted.forEach((id) => delete next[id]);
        return next;
      });
      setVisionNotes((current) => {
        const next = { ...current };
        posted.forEach((id) => delete next[id]);
        return next;
      });
      toastService.success(ok.length === 1 ? ok[0] : `${ok.length} counts posted.${ok.length <= 3 ? ` ${ok.join("; ")}` : ""}`);
      onRefresh();
    }
    if (failed.length > 0) toastService.error(failed.join("; "));
  };

  const applyVision = (row: SilaLiveInventoryRow, reading: VisionApplication) => {
    setCount(row, reading.total);
    setVisionNotes((current) => ({
      ...current,
      [row.materialId]: `Vision AI: ${reading.remainingMl} ml left, ${reading.consumedMl} ml consumed`,
    }));
    setVisionOpen(false);
    setVisionFile(null);
    setLens("shelf");
    toastService.success(`${row.materialCode}: ${reading.sealed} sealed + ${reading.fraction.toFixed(2)} open added to the shelf count. Review, then post.`);
  };

  const openVision = (materialId: string | null, file: File | null = null) => {
    setVisionMaterialId(materialId);
    setVisionFile(file);
    setVisionOpen(true);
  };

  useEffect(() => {
    if (!scanTarget) return;
    const hit = rows.find((row) => row.materialCode === scanTarget || (row.barcode && row.barcode === scanTarget));
    if (hit) {
      setDrawerId(hit.materialId);
      setScanTarget(null);
    } else if (!loading) {
      toastService.error(`No item at this location matches ${scanTarget}.`);
      setScanTarget(null);
    }
  }, [scanTarget, rows, loading]);

  const onScanned = (code: string) => {
    setScanOpen(false);
    const hit = rows.find((row) => row.materialCode === code || (row.barcode && row.barcode === code));
    if (hit) {
      setDrawerId(hit.materialId);
      return;
    }
    setQuery(code);
    setScanTarget(code);
  };

  const openTransfer = (materialId?: string) => {
    if (!canTransfer) return;
    setTransferLines(materialId ? [{ materialId, qty: "1" }] : []);
    setTransferTo("");
    setTransferOpen(true);
  };

  const transferDestination = stockLocations.find((location) => location.id === transferTo);
  const transferMovement =
    transferDestination && currentLocation
      ? (transferDestination.plantCode ?? "").trim().toUpperCase() === (currentLocation.plantCode ?? "").trim().toUpperCase() &&
        (currentLocation.plantCode ?? "").trim() !== ""
        ? "311"
        : "301"
      : null;

  const submitTransfer = async () => {
    if (!canTransfer || !locationId || !transferTo) {
      toastService.error("Choose the destination before creating the transfer.");
      return;
    }
    const items = transferLines
      .map((line) => ({ line, row: rows.find((row) => row.materialId === line.materialId), qty: Number(line.qty) }))
      .filter((entry) => entry.row);
    if (items.length === 0 || items.some((entry) => !(entry.qty > 0))) {
      toastService.error("Add at least one item with a quantity above zero.");
      return;
    }
    const over = items.find((entry) => entry.qty > (entry.row!.availableQty ?? entry.row!.onHandQty));
    if (over) {
      toastService.error(`${over.row!.materialCode}: only ${formatQty(over.row!.availableQty ?? over.row!.onHandQty)} available.`);
      return;
    }
    setTransferBusy(true);
    try {
      await createTransfer({
        fromLocationId: locationId,
        toLocationId: transferTo,
        reason: "Transfer from Live Inventory",
        requiredBy: null,
        addToLocation: true,
        items: items.map((entry) => ({ materialId: entry.row!.materialId, quantity: entry.qty, uom: entry.row!.baseUom ?? null })),
      });
      toastService.success(`Transfer created for ${items.length} item(s) (SAP ${transferMovement ?? "311/301"}).`);
      setTransferOpen(false);
      onRefresh();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not create the transfer.");
    } finally {
      setTransferBusy(false);
    }
  };

  const stockItems = home?.stockItems ?? 0;
  const lowItems = home?.lowStockItems ?? 0;
  const coverage = stockItems > 0 ? Math.max(0, Math.min(100, Math.round(((stockItems - lowItems) / stockItems) * 100))) : null;
  const ringLength = 2 * Math.PI * 28;
  const locationLine = currentLocation
    ? [home?.propertyName || currentLocation.propertyName, currentLocation.locationName].filter(Boolean).join(" · ")
    : locationsError ?? "Select an outlet";
  const live = freshness === "Live";

  return (
    <div className="lic-page" data-testid="inventory-command-center">
      <header className="lic-header">
        <div className="lic-brand">
          <span className="lic-logo" aria-hidden>S</span>
          <div>
            <p className="lic-eyebrow">Live Inventory</p>
            <h1>Inventory Command Center</h1>
          </div>
        </div>
        <div className="lic-header__right">
          <label className="lic-outlet" title={locationLine}>
            <span className="lic-dot" aria-hidden />
            <select value={locationId} onChange={(event) => onLocationChange(event.target.value)} aria-label="Outlet">
              <option value="">{locationsError ?? "Select an outlet"}</option>
              {stockLocations.map((location) => (
                <option key={location.id} value={location.id}>
                  {location.locationCode} · {location.locationName}
                </option>
              ))}
            </select>
          </label>
          <span className={`lic-live${live ? "" : " lic-live--idle"}`} title="Based on the last successful SAP-backed inventory query">
            <span className="lic-dot" aria-hidden /> SAP {live ? "Live" : freshness}
          </span>
          <button type="button" className="lic-icon-btn" onClick={onRefresh} aria-label="Refresh inventory" title="Refresh">
            <RefreshCw size={16} />
          </button>
          {canTransfer && (
            <button type="button" className="lic-btn lic-btn--primary" onClick={() => openTransfer()} disabled={!locationId}>
              <Plus size={16} /> New Transfer
            </button>
          )}
        </div>
      </header>

      <div className="lic-bento">
        <button type="button" className="lic-card lic-kpi lic-kpi--health" onClick={() => chooseChip("LOW")} data-testid="kpi-store-health">
          <div className="lic-kpi__head">
            <span className="lic-kpi__icon lic-kpi__icon--green"><ShieldCheck size={18} /></span>
            <span className="lic-kpi__label">Store health &amp; par coverage</span>
          </div>
          <div className="lic-kpi__body">
            <svg className="lic-ring" viewBox="0 0 70 70" aria-hidden>
              <circle cx="35" cy="35" r="28" className="lic-ring__track" />
              <circle
                cx="35"
                cy="35"
                r="28"
                className="lic-ring__value"
                strokeDasharray={ringLength}
                strokeDashoffset={ringLength * (1 - (coverage ?? 0) / 100)}
                transform="rotate(-90 35 35)"
              />
            </svg>
            <div>
              <strong className="lic-kpi__num">{coverage == null ? "—" : `${coverage}%`}</strong>
              <span className="lic-kpi__sub">{lowItems} critical / low of {stockItems || "—"} items</span>
            </div>
          </div>
        </button>

        <div className="lic-card lic-kpi lic-kpi--supply" data-testid="kpi-supply-chain">
          <div className="lic-kpi__head">
            <span className="lic-kpi__icon lic-kpi__icon--blue"><Truck size={18} /></span>
            <span className="lic-kpi__label">Supply chain &amp; movement</span>
          </div>
          <div className="lic-kpi__split">
            <button type="button" className="lic-stat" onClick={() => chooseChip("TRANSIT")}>
              <strong>{home?.inTransitToReceive ?? "—"}</strong>
              <span>In transit</span>
            </button>
            <button type="button" className="lic-stat" onClick={onFocusPending}>
              <strong>{home?.pendingActions ?? "—"}</strong>
              <span>Pending actions</span>
            </button>
            <button type="button" className="lic-stat" onClick={onOpenPending} data-testid="pending-requirements-indicator">
              <strong>{pendingCount}</strong>
              <span>Pending requirements</span>
            </button>
          </div>
          <div className={`lic-transit${(home?.inTransitToReceive ?? 0) > 0 ? " lic-transit--active" : ""}`} aria-hidden>
            <span />
          </div>
        </div>

        <div className="lic-card lic-kpi lic-kpi--value" data-testid="kpi-valuation">
          <div className="lic-kpi__head">
            <span className="lic-kpi__icon lic-kpi__icon--amber"><Wallet size={18} /></span>
            <span className="lic-kpi__label">Live outlet valuation &amp; sync</span>
          </div>
          <strong className="lic-kpi__money">{home?.stockValue != null ? formatMoney(home.stockValue, home.stockCurrency || "AED") : "—"}</strong>
          <span className="lic-kpi__sub">On hand at Item Master cost · {live ? "synced just now" : freshness.toLowerCase()}</span>
          {canManageCount && (
            <button type="button" className="lic-btn lic-btn--soft" onClick={() => setLens("shelf")}>
              <SlidersHorizontal size={15} /> Stock Count
            </button>
          )}
        </div>
      </div>

      {pendingPanel}

      <section className="lic-omni" aria-label="Search inventory">
        <div className="lic-omni__bar">
          <Search size={18} aria-hidden />
          <input
            ref={searchRef}
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder='Search item, code or barcode, or try "open bottles" / "in transit"'
            aria-label="Search inventory"
          />
          <kbd>⌘K</kbd>
        </div>
        <div className="lic-omni__actions">
          <button type="button" className="lic-btn lic-btn--ghost" onClick={() => setScanOpen(true)}>
            <ScanLine size={16} /> Scan barcode
          </button>
          <button type="button" className="lic-btn lic-btn--ghost" onClick={() => photoRef.current?.click()}>
            <Camera size={16} /> Take photo
          </button>
          <button type="button" className="lic-btn lic-btn--vision" onClick={() => openVision(null)} data-testid="open-vision-ai">
            <ScanEye size={16} /> Vision AI
          </button>
          <input
            ref={photoRef}
            type="file"
            accept="image/*"
            capture="environment"
            hidden
            onChange={(event) => {
              const picked = event.target.files?.[0];
              if (picked) openVision(null, picked);
              event.target.value = "";
            }}
          />
        </div>
      </section>

      <div className="lic-toolbar">
        <div className="lic-lens" role="tablist" aria-label="Inventory lens">
          {([
            ["vault", "Spatial Vault", <LayoutGrid size={15} key="v" />],
            ["shelf", "Shelf Count Mode", <SlidersHorizontal size={15} key="s" />],
            ["ledger", "Executive Ledger", <Table2 size={15} key="l" />],
          ] as [Lens, string, React.ReactNode][]).map(([id, label, icon]) => (
            <button key={id} type="button" role="tab" aria-selected={lens === id} className={lens === id ? "is-on" : ""} onClick={() => setLens(id)}>
              {icon} {label}
            </button>
          ))}
        </div>
        <div className="lic-chips" role="group" aria-label="Category">
          {[
            ["ALL", "All"],
            ...PRODUCT_CLASSES.map((entry) => [entry.id, entry.label]),
            ["LOW", "Low stock"],
            ["TRANSIT", "In transit"],
            ["OPEN", "Open bottles"],
          ].map(([id, label]) => (
            <button key={id} type="button" className={`lic-chip${activeChip === id ? " is-on" : ""}`} onClick={() => chooseChip(id)}>
              {label}
            </button>
          ))}
          {(activeChip !== "ALL" || query) && (
            <button type="button" className="lic-chip lic-chip--reset" onClick={() => chooseChip("ALL")}>
              <X size={13} /> Reset
            </button>
          )}
        </div>
      </div>

      {!canSearch ? (
        <EmptyState
          title={locationsError ? "Couldn't load locations" : "Choose an outlet or search"}
          description={locationsError ?? "Select an outlet, or search for an item across the locations you can open."}
        />
      ) : loading && rows.length === 0 ? (
        <Loader size={24} message="Loading live stock…" />
      ) : error ? (
        <EmptyState variant="error" title="Couldn't load stock" description={error} />
      ) : visibleRows.length === 0 ? (
        <EmptyState title="No items found" description="Try another search, or reset the filters." />
      ) : (
        <>
          {lens === "vault" && (
            <div className="lic-vault" data-testid="lens-vault">
              {visibleRows.map((row) => {
                const status = statusOf(row);
                const open = openBottleStats(row, row.availableQty ?? row.onHandQty);
                const par = parOf(row);
                const cover = parCoveragePct(row.onHandQty, par);
                return (
                  <button key={row.materialId} type="button" className={`lic-vault-card lic-class--${classOf(row).toLowerCase()}`} onClick={() => setDrawerId(row.materialId)}>
                    <span className="lic-vault-card__top">
                      <span className="lic-class-ico">{CLASS_ICON[classOf(row)]}</span>
                      <span className="lic-mono">{row.materialCode}</span>
                      <span className={`lic-pill lic-pill--${status.tone}`}>{status.label}</span>
                    </span>
                    <span className="lic-vault-card__name">{row.description || row.materialCode}</span>
                    <span className="lic-vault-card__qty">
                      <strong className="lic-mono">{formatQty(row.availableQty ?? row.onHandQty)}</strong>
                      <em>{row.baseUom}</em>
                    </span>
                    {open.openRemainingMl != null && open.packMl != null ? (
                      <span className="lic-open">
                        <span className="lic-open__bar"><i style={{ width: `${open.fractionPct}%` }} /></span>
                        <small>{open.sealed} sealed + open bottle {open.openRemainingMl} ml left · {open.openConsumedMl} ml used</small>
                      </span>
                    ) : (
                      <span className="lic-open lic-open--plain"><small>{productClassLabel(row.productClass)}{row.materialGroup ? ` · ${row.materialGroup}` : ""}</small></span>
                    )}
                    <span className="lic-vault-card__foot">
                      {cover != null ? (
                        <span className="lic-par" title={`Par ${formatQty(par as number)}`}>
                          <span className="lic-par__bar"><i className={cover < 40 ? "is-low" : ""} style={{ width: `${cover}%` }} /></span>
                          <small>{cover}% of par</small>
                        </span>
                      ) : (
                        <small>No par set</small>
                      )}
                      {(row.inTransitQty ?? 0) > 0 && <small className="lic-transit-tag"><Truck size={12} /> {formatQty(row.inTransitQty)}</small>}
                    </span>
                  </button>
                );
              })}
            </div>
          )}

          {lens === "shelf" && (
            <div className="lic-shelf" data-testid="lens-shelf">
              <div className="lic-shelf__head">
                <div>
                  <h2>Shelf Count Mode</h2>
                  <p>Count what is on the shelf. Sealed bottles by the stepper, the open bottle by its level. Nothing posts until you post the cycle count.</p>
                </div>
                <div className="lic-shelf__post">
                  <span>{dirtyRows.length} with variance</span>
                  <button type="button" className="lic-btn lic-btn--green" disabled={!canManageCount || dirtyRows.length === 0 || posting || !locationId} onClick={postCycleCount}>
                    {posting ? "Posting…" : "Post Cycle Count"}
                  </button>
                </div>
              </div>
              {!canManageCount && <p className="lic-shelf__note">You can review counts here. Posting needs the stock count permission.</p>}
              <div className="lic-shelf__list">
                {visibleRows.map((row) => {
                  const physical = physicalOf(row);
                  const variance = varianceOf(row);
                  const bottle = isBottleItem(row);
                  const split = bottle ? splitSealedFraction(physical) : { sealed: physical, fraction: 0 };
                  const open = openBottleStats(row, physical);
                  return (
                    <div key={row.materialId} className={`lic-shelf-row${variance !== 0 ? " is-dirty" : ""}`}>
                      <div className="lic-shelf-row__id">
                        <span className="lic-class-ico">{CLASS_ICON[classOf(row)]}</span>
                        <div>
                          <strong>{row.description || row.materialCode}</strong>
                          <small className="lic-mono">{row.materialCode} · book {formatQty(row.onHandQty)} {row.baseUom}</small>
                        </div>
                      </div>
                      <div className="lic-shelf-row__count">
                        <div className="lic-stepper" aria-label="Sealed count">
                          <button type="button" aria-label="Decrease" disabled={physical < 1} onClick={() => setCount(row, bottle ? composeQty(Math.max(0, split.sealed - 1), split.fraction) : physical - 1)}>
                            <Minus size={16} />
                          </button>
                          {bottle ? (
                            <span className="lic-mono">{split.sealed}</span>
                          ) : (
                            <input
                              type="number"
                              min={0}
                              step="any"
                              value={Number.isFinite(physical) ? physical : 0}
                              onChange={(event) => setCount(row, Number(event.target.value))}
                              aria-label={`Counted quantity for ${row.materialCode}`}
                            />
                          )}
                          <button type="button" aria-label="Increase" onClick={() => setCount(row, bottle ? composeQty(split.sealed + 1, split.fraction) : physical + 1)}>
                            <Plus size={16} />
                          </button>
                        </div>
                        {bottle && (
                          <div className="lic-openslider">
                            <label>
                              <span>Open bottle</span>
                              <input
                                type="range"
                                min={0}
                                max={0.999}
                                step={0.001}
                                value={split.fraction}
                                onChange={(event) => setCount(row, composeQty(split.sealed, Number(event.target.value)))}
                                aria-label={`Open bottle level for ${row.materialCode}`}
                              />
                            </label>
                            <small className="lic-mono">
                              {open.openRemainingMl != null ? `${open.openRemainingMl} ml left · ${open.openConsumedMl} ml used` : "No open bottle"}
                            </small>
                            <button type="button" className="lic-link" onClick={() => openVision(row.materialId)}>
                              <ScanEye size={13} /> Read with Vision AI
                            </button>
                          </div>
                        )}
                      </div>
                      <div className="lic-shelf-row__var">
                        <span className="lic-mono">{formatQty(physical)}</span>
                        <small className={variance === 0 ? "" : variance > 0 ? "is-pos" : "is-neg"}>
                          {variance === 0 ? "On book" : `${variance > 0 ? "+" : ""}${formatQty(variance)} ${row.baseUom}`}
                        </small>
                        {visionNotes[row.materialId] && <small className="lic-vision-note">{visionNotes[row.materialId]}</small>}
                        {variance !== 0 && (
                          <button type="button" className="lic-link" onClick={() => resetToBook(row)}>Reset to Book</button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {lens === "ledger" && (
            <div className="lic-ledger" data-testid="lens-ledger">
              <table>
                <thead>
                  <tr>
                    <th>Item</th>
                    <th className="is-num">On hand</th>
                    <th>Par coverage</th>
                    <th>7-day issues</th>
                    <th>Status</th>
                    <th className="is-num">Value</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {visibleRows.map((row) => {
                    const status = statusOf(row);
                    const par = parOf(row);
                    const cover = parCoveragePct(row.onHandQty, par);
                    const points = sparklinePoints(row.last7DayIssues);
                    const value = stockValueOf(row);
                    return (
                      <tr key={row.materialId} onClick={() => setDrawerId(row.materialId)}>
                        <td>
                          <span className="lic-ledger__item">
                            <span className="lic-class-ico">{CLASS_ICON[classOf(row)]}</span>
                            <span>
                              <strong>{row.description || row.materialCode}</strong>
                              <small className="lic-mono">{row.materialCode} · {productClassLabel(row.productClass)}</small>
                            </span>
                          </span>
                        </td>
                        <td className="is-num lic-mono">{formatQty(row.onHandQty)} {row.baseUom}</td>
                        <td>
                          {cover != null ? (
                            <span className="lic-par">
                              <span className="lic-par__bar"><i className={cover < 40 ? "is-low" : ""} style={{ width: `${cover}%` }} /></span>
                              <small>{cover}%</small>
                            </span>
                          ) : (
                            <small>No par</small>
                          )}
                        </td>
                        <td>
                          {points ? (
                            <span className="lic-spark" title={`${formatQty(weeklyIssued(row.last7DayIssues))} ${row.baseUom} issued in 7 days`}>
                              <svg viewBox="0 0 72 22" aria-hidden><polyline points={points} /></svg>
                              <small className="lic-mono">{formatQty(weeklyIssued(row.last7DayIssues))}</small>
                            </span>
                          ) : (
                            <small>—</small>
                          )}
                        </td>
                        <td><span className={`lic-pill lic-pill--${status.tone}`}>{status.label}</span></td>
                        <td className="is-num lic-mono">{value != null ? formatMoney(value, row.currency || "AED") : "—"}</td>
                        <td className="is-num" onClick={(event) => event.stopPropagation()}>
                          {canTransfer && (
                            <button type="button" className="lic-btn lic-btn--soft lic-btn--sm" onClick={() => openTransfer(row.materialId)}>
                              <ArrowLeftRight size={13} /> Transfer
                            </button>
                          )}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          <div className="lic-pager">
            <span>{visibleRows.length}{visibleRows.length !== rows.length ? ` of ${rows.length}` : ""} items</span>
            {hasMore && (
              <button type="button" className="lic-btn lic-btn--ghost lic-btn--sm" disabled={loadingMore} onClick={onLoadMore}>
                {loadingMore ? "Loading…" : "Load more"}
              </button>
            )}
          </div>
        </>
      )}

      {drawerRow && (
        <MaterialDrawer
          row={drawerRow}
          locationId={locationId}
          canTransfer={canTransfer}
          canManageCount={canManageCount}
          canPostGoodsIssue={canPostGoodsIssue}
          canRequestPurchase={canRequestPurchase}
          onClose={() => setDrawerId(null)}
          onTransfer={() => {
            setDrawerId(null);
            openTransfer(drawerRow.materialId);
          }}
          onCount={() => {
            setDrawerId(null);
            setLens("shelf");
            setQuery(drawerRow.materialCode);
          }}
          onVision={() => {
            setDrawerId(null);
            openVision(drawerRow.materialId);
          }}
          onOpenMaterial={() => onOpenMaterial(drawerRow.materialId)}
          onNavigate={onNavigate}
        />
      )}

      {transferOpen && (
        <div className="lic-modal-backdrop" role="presentation" onClick={() => setTransferOpen(false)}>
          <div className="lic-modal lic-modal--transfer" role="dialog" aria-modal="true" aria-label="New Transfer" data-testid="transfer-modal" onClick={(event) => event.stopPropagation()}>
            <header className="lic-modal__head">
              <div>
                <h2>New Transfer</h2>
                <p>SAP movement {transferMovement ?? "311 / 301"} · from {currentLocation?.locationName ?? "your outlet"}</p>
              </div>
              <button type="button" className="lic-icon-btn" onClick={() => setTransferOpen(false)} aria-label="Close transfer">
                <X size={18} />
              </button>
            </header>
            <label className="lic-field">
              <span>Destination</span>
              <select value={transferTo} onChange={(event) => setTransferTo(event.target.value)}>
                <option value="">Select destination</option>
                {stockLocations
                  .filter((location) => location.id !== locationId)
                  .map((location) => (
                    <option key={location.id} value={location.id}>
                      {location.locationCode} · {location.locationName}
                      {location.plantCode ? ` · Plant ${location.plantCode}` : ""}
                    </option>
                  ))}
              </select>
            </label>
            <div className="lic-transfer-lines">
              {transferLines.map((line, index) => {
                const row = rows.find((entry) => entry.materialId === line.materialId);
                return (
                  <div key={line.materialId} className="lic-transfer-line">
                    <div>
                      <strong>{row?.description ?? line.materialId}</strong>
                      <small className="lic-mono">{row?.materialCode} · {formatQty(row?.availableQty ?? row?.onHandQty ?? 0)} {row?.baseUom} available</small>
                    </div>
                    <input
                      type="number"
                      min={0}
                      step="any"
                      value={line.qty}
                      aria-label={`Quantity for ${row?.materialCode}`}
                      onChange={(event) => setTransferLines((current) => current.map((entry, i) => (i === index ? { ...entry, qty: event.target.value } : entry)))}
                    />
                    <button type="button" className="lic-icon-btn" aria-label="Remove line" onClick={() => setTransferLines((current) => current.filter((_, i) => i !== index))}>
                      <X size={15} />
                    </button>
                  </div>
                );
              })}
              <label className="lic-field">
                <span>Add item</span>
                <select
                  value=""
                  onChange={(event) => {
                    const id = event.target.value;
                    if (id && !transferLines.some((line) => line.materialId === id)) setTransferLines((current) => [...current, { materialId: id, qty: "1" }]);
                  }}
                >
                  <option value="">Choose from the loaded items</option>
                  {rows
                    .filter((row) => (row.availableQty ?? row.onHandQty) > 0 && !transferLines.some((line) => line.materialId === row.materialId))
                    .map((row) => (
                      <option key={row.materialId} value={row.materialId}>
                        {row.materialCode} · {row.description}
                      </option>
                    ))}
                </select>
              </label>
            </div>
            <footer className="lic-modal__foot">
              <button type="button" className="lic-btn lic-btn--ghost" onClick={() => setTransferOpen(false)}>Cancel</button>
              <button type="button" className="lic-btn lic-btn--primary" disabled={transferBusy || !transferTo || transferLines.length === 0} onClick={submitTransfer}>
                {transferBusy ? "Creating…" : "Create transfer"}
              </button>
            </footer>
          </div>
        </div>
      )}

      <SilaBarcodeScanDialog open={scanOpen} onClose={() => setScanOpen(false)} onCode={onScanned} />
      <SilaVisionAiModal
        open={visionOpen}
        bottleRows={bottleRows}
        initialMaterialId={visionMaterialId}
        initialFile={visionFile}
        canApply={canManageCount}
        onClose={() => {
          setVisionOpen(false);
          setVisionFile(null);
        }}
        onApply={applyVision}
      />
    </div>
  );
};

interface MaterialDrawerProps {
  row: SilaLiveInventoryRow;
  locationId: string;
  canTransfer: boolean;
  canManageCount: boolean;
  canPostGoodsIssue: boolean;
  canRequestPurchase: boolean;
  onClose: () => void;
  onTransfer: () => void;
  onCount: () => void;
  onVision: () => void;
  onOpenMaterial: () => void;
  onNavigate?: (key: string) => void;
}

const MaterialDrawer: React.FC<MaterialDrawerProps> = ({
  row,
  locationId,
  canTransfer,
  canManageCount,
  canPostGoodsIssue,
  onClose,
  onTransfer,
  onCount,
  onVision,
  onOpenMaterial,
  onNavigate,
}) => {
  const [detail, setDetail] = useState<SilaLiveInventoryDetail | null>(null);
  const [ledger, setLedger] = useState<SilaInventoryTransaction[] | null>(null);
  const [ledgerError, setLedgerError] = useState<string | null>(null);
  const [detailFailed, setDetailFailed] = useState(false);

  useEffect(() => {
    let active = true;
    setDetail(null);
    setLedger(null);
    setLedgerError(null);
    setDetailFailed(false);
    getLiveInventoryDetail(row.materialId, { currentLocationId: locationId || undefined })
      .then((data) => active && setDetail(data))
      .catch(() => active && setDetailFailed(true));
    getInventoryTransactions({ materialId: row.materialId, locationId: locationId || undefined, limit: 8 })
      .then((data) => active && setLedger(data))
      .catch((err: unknown) => active && setLedgerError(err instanceof Error ? err.message : "Could not load movements."));
    return () => {
      active = false;
    };
  }, [row.materialId, locationId]);

  const available = row.availableQty ?? row.onHandQty;
  const open = openBottleStats(row, available);
  const value = stockValueOf(row);
  const recipes = detail?.linkedRecipes ?? [];
  const bottle = isBottleItem(row);

  return (
    <div className="lic-drawer-backdrop" role="presentation" onClick={onClose}>
      <aside className="lic-drawer" role="dialog" aria-modal="true" aria-label={`${row.materialCode} details`} data-testid="material-drawer" onClick={(event) => event.stopPropagation()}>
        <header className="lic-drawer__head">
          <div>
            <p className="lic-eyebrow">{productClassLabel(row.productClass)}{row.materialGroup ? ` · ${row.materialGroup}` : ""}</p>
            <h2>{row.description || row.materialCode}</h2>
            <small className="lic-mono">{row.materialCode}{row.barcode ? ` · ${row.barcode}` : ""}</small>
          </div>
          <button type="button" className="lic-icon-btn" onClick={onClose} aria-label="Close details">
            <X size={18} />
          </button>
        </header>

        <div className="lic-drawer__stats">
          <div>
            <small>Available</small>
            <strong className="lic-mono">{formatQty(available)} <em>{row.baseUom}</em></strong>
          </div>
          <div>
            <small>{bottle ? "Open bottle" : "In transit"}</small>
            <strong className="lic-mono">
              {bottle ? (open.openRemainingMl != null ? `${open.openRemainingMl} ml` : "None") : formatQty(row.inTransitQty ?? 0)}
            </strong>
          </div>
          <div>
            <small>Line value</small>
            <strong className="lic-mono">{value != null ? formatMoney(value, row.currency || "AED") : "—"}</strong>
          </div>
        </div>

        {bottle && open.openRemainingMl != null && (
          <p className="lic-drawer__open">
            {open.sealed} sealed, open bottle {open.openRemainingMl} of {open.packMl} ml left ({open.openConsumedMl} ml consumed).
          </p>
        )}

        <section className="lic-drawer__section">
          <h3>Linked recipes</h3>
          {detailFailed ? (
            <small role="alert">Could not load the linked recipes.</small>
          ) : detail == null ? (
            <small>Loading…</small>
          ) : recipes.length === 0 ? (
            <small>Not used in any active recipe.</small>
          ) : (
            <div className="lic-tags">
              {recipes.map((code) => (
                <span key={code} className="lic-tag lic-mono">{code}</span>
              ))}
            </div>
          )}
        </section>

        <section className="lic-drawer__section">
          <h3>Movement ledger</h3>
          {ledgerError ? (
            <small role="alert">{ledgerError}</small>
          ) : ledger == null ? (
            <small>Loading…</small>
          ) : ledger.length === 0 ? (
            <small>No movements at this location.</small>
          ) : (
            <ul className="lic-ledger-list">
              {ledger.map((txn) => (
                <li key={txn.id}>
                  <span className={`lic-dir lic-dir--${txn.direction === "IN" ? "in" : "out"}`}>{txn.direction === "IN" ? "+" : "−"}</span>
                  <span>
                    <strong>{silaLabel(txn.transactionType)}</strong>
                    <small>{txn.referenceNumber || txn.transactionNumber} · {new Date(txn.businessDate || txn.dateCreated).toLocaleDateString()}</small>
                  </span>
                  <span className="lic-mono">{formatQty(txn.quantity)} {txn.baseUom}</span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <footer className="lic-drawer__actions">
          {canPostGoodsIssue && (
            <button type="button" className="lic-btn lic-btn--ghost" onClick={() => onNavigate?.("silaGoodsIssue")}>Goods Issue (Z01)</button>
          )}
          {canTransfer && (
            <button type="button" className="lic-btn lic-btn--primary" onClick={onTransfer}><ArrowLeftRight size={15} /> Transfer Stock</button>
          )}
          {canManageCount && <button type="button" className="lic-btn lic-btn--ghost" onClick={onCount}>Count</button>}
          {bottle && <button type="button" className="lic-btn lic-btn--vision" onClick={onVision}><ScanEye size={15} /> Vision AI</button>}
          <button type="button" className="lic-btn lic-btn--ghost" onClick={onOpenMaterial}>Full material view</button>
        </footer>
      </aside>
    </div>
  );
};

export default SilaInventoryCommandCenter;
