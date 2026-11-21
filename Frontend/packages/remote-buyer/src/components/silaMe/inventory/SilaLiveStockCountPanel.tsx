import React, { useEffect, useMemo, useRef, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  createLiveStockCount,
  formatQty,
  searchLiveInventory,
  searchMaterials,
  type SilaLiveInventoryRow,
} from "../../../api/silaMe/silaInventoryApi";

export type LiveStockCountTarget = {
  materialId?: string;
  materialCode: string;
  description?: string;
  onHandQty?: number | null;
  uom?: string | null;
};

interface SilaLiveStockCountPanelProps {
  open: boolean;
  locationId: string;
  locationLabel: string;
  /** Material number copied from Search inventory when no row targets. */
  initialCode?: string;
  /** Selected materials — counted one by one. */
  targets?: LiveStockCountTarget[];
  onClose: () => void;
  onPosted: () => void;
}

type ResolvedLine = {
  materialId: string | null;
  materialCode: string;
  materialName: string;
  uom: string;
  onHand: number;
};

/**
 * Live Inventory stock-count dialog: physical count → SAP 701/702 variance.
 * Walks a queue of materials one by one when several are selected.
 */
const SilaLiveStockCountPanel: React.FC<SilaLiveStockCountPanelProps> = ({
  open,
  locationId,
  locationLabel,
  initialCode = "",
  targets = [],
  onClose,
  onPosted,
}) => {
  const [queue, setQueue] = useState<LiveStockCountTarget[]>([]);
  const [index, setIndex] = useState(0);
  const [line, setLine] = useState<ResolvedLine | null>(null);
  const [physical, setPhysical] = useState("");
  const [resolving, setResolving] = useState(false);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [postedCount, setPostedCount] = useState(0);
  const physicalRef = useRef<HTMLInputElement>(null);

  const variance = useMemo(() => {
    if (!line || physical.trim() === "" || Number.isNaN(Number(physical))) return null;
    return Number(physical) - line.onHand;
  }, [line, physical]);

  const resolveTarget = async (target: LiveStockCountTarget): Promise<ResolvedLine> => {
    const term = target.materialCode.trim();
    if (!term) {
      throw new Error("Material code is required.");
    }

    let materialId = target.materialId ?? null;
    let materialCode = term;
    let materialName = target.description || term;
    let uom = (target.uom || "EA").trim() || "EA";
    let onHand = target.onHandQty;

    if (materialId == null || onHand == null) {
      const page = await searchMaterials(term);
      const material =
        page.find((row) => row.materialCode === term || row.barcode === term) ??
        page.find((row) => row.materialCode?.endsWith(term.replace(/^0+/, ""))) ??
        page[0];
      if (!material) {
        throw new Error(`No material matches ${term}.`);
      }
      materialId = material.id;
      materialCode = material.materialCode;
      materialName = material.description || material.materialCode;
      uom = material.baseUnitOfMeasure || uom;
    }

    if (onHand == null && locationId) {
      const rows: SilaLiveInventoryRow[] = await searchLiveInventory(materialCode, locationId, 0);
      const row =
        (materialId ? rows.find((item) => item.materialId === materialId) : undefined) ?? rows[0];
      onHand = row?.onHandQty ?? 0;
      if (row?.baseUom) uom = row.baseUom;
    }

    return {
      materialId,
      materialCode,
      materialName,
      uom,
      onHand: onHand ?? 0,
    };
  };

  const loadIndex = async (list: LiveStockCountTarget[], at: number) => {
    const target = list[at];
    if (!target) {
      setLine(null);
      return;
    }
    setResolving(true);
    setError(null);
    setPhysical("");
    try {
      const resolved = await resolveTarget(target);
      setLine(resolved);
      setPhysical(String(resolved.onHand));
      setTimeout(() => physicalRef.current?.focus(), 50);
    } catch (err: unknown) {
      setLine(null);
      setError(err instanceof Error ? err.message : "Could not resolve the material.");
    } finally {
      setResolving(false);
    }
  };

  useEffect(() => {
    if (!open) return;
    const fromTargets = targets
      .map((item) => ({
        ...item,
        materialCode: (item.materialCode || "").trim(),
      }))
      .filter((item) => item.materialCode.length > 0);
    const code = initialCode.trim();
    const list =
      fromTargets.length > 0
        ? fromTargets
        : code
          ? [{ materialCode: code }]
          : [];
    setQueue(list);
    setIndex(0);
    setPostedCount(0);
    setError(null);
    setLine(null);
    setPhysical("");
    if (list.length === 0) {
      setError("Select materials in Material availability, or enter a material in Search inventory.");
      return;
    }
    void loadIndex(list, 0);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initialCode, locationId, targets]);

  const finishAll = () => {
    if (postedCount > 0 || queue.length > 0) {
      onPosted();
    }
    onClose();
  };

  const advanceOrClose = (nextPosted: number) => {
    const next = index + 1;
    if (next < queue.length) {
      setIndex(next);
      void loadIndex(queue, next);
      return;
    }
    toastService.success(
      nextPosted === 1 ? "Stock count posted." : `${nextPosted} stock count variances posted.`,
    );
    onPosted();
    onClose();
  };

  const onPost = async () => {
    if (!locationId || !line || physical.trim() === "") {
      setError("Enter the physical count.");
      return;
    }
    const qty = Number(physical);
    if (Number.isNaN(qty) || qty < 0) {
      setError("Physical count must be zero or greater.");
      return;
    }
    if (variance === 0) {
      // No SAP post — skip to next material in the queue.
      toastService.info(`${line.materialCode}: no variance.`);
      advanceOrClose(postedCount);
      return;
    }
    setPosting(true);
    setError(null);
    try {
      const result = await createLiveStockCount({
        locationId,
        materialId: line.materialId,
        materialCodeOrBarcode: line.materialCode,
        physicalQty: qty,
        uom: line.uom,
        reason: `Live Inventory stock count at ${locationLabel}`,
      });
      toastService.success(
        `${result.materialCode}: variance ${formatQty(result.varianceQty)} → SAP ${result.movementType}`,
      );
      const nextPosted = postedCount + 1;
      setPostedCount(nextPosted);
      advanceOrClose(nextPosted);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not post the stock count.");
    } finally {
      setPosting(false);
    }
  };

  if (!open) return null;

  const progress =
    queue.length > 1 ? `Material ${Math.min(index + 1, queue.length)} of ${queue.length}` : null;

  return (
    <div className="liv-sc-backdrop" role="presentation" onClick={finishAll}>
      <div
        className="liv-sc-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="liv-sc-title"
        onClick={(event) => event.stopPropagation()}
      >
        <header className="liv-sc-head">
          <div>
            <h2 id="liv-sc-title">Stock Count</h2>
            <p>{locationLabel}{progress ? ` · ${progress}` : ""}</p>
          </div>
          <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={finishAll}>
            Close
          </button>
        </header>

        {resolving && <p className="liv-sc-sub">Resolving material…</p>}

        <div className="liv-sc-grid">
          <div>
            <span className="sila-label">Material</span>
            <strong className="liv-sc-value">{line?.materialCode || "—"}</strong>
            {line?.materialName && <p className="liv-sc-sub">{line.materialName}</p>}
          </div>
          <div>
            <span className="sila-label">Stock in hand</span>
            <strong className="liv-sc-value">{line == null ? "—" : formatQty(line.onHand)}</strong>
            <p className="liv-sc-sub">{line?.uom || ""}</p>
          </div>
          <label className="sila-field">
            <span className="sila-label">Physical stock count</span>
            <input
              ref={physicalRef}
              className="sila-input"
              type="number"
              min={0}
              step="any"
              value={physical}
              disabled={!line || posting}
              onChange={(event) => setPhysical(event.target.value)}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  void onPost();
                }
              }}
            />
          </label>
          <div>
            <span className="sila-label">Variance</span>
            <strong
              className={`liv-sc-value${
                variance != null && variance < 0
                  ? " liv-sc-value--neg"
                  : variance != null && variance > 0
                    ? " liv-sc-value--pos"
                    : ""
              }`}
            >
              {variance == null ? "—" : formatQty(variance)}
            </strong>
            <p className="liv-sc-sub">
              {variance == null
                ? "Enter physical count"
                : variance > 0
                  ? "SAP 701 (surplus)"
                  : variance < 0
                    ? "SAP 702 (shortage)"
                    : "No variance — skip to next"}
            </p>
          </div>
        </div>

        {error && <p className="liv-sc-error">{error}</p>}

        <footer className="liv-sc-foot">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={finishAll}>
            Cancel
          </button>
          {queue.length > 1 && index + 1 < queue.length && (
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              disabled={posting || resolving}
              onClick={() => {
                const next = index + 1;
                setIndex(next);
                void loadIndex(queue, next);
              }}
            >
              Skip
            </button>
          )}
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={posting || resolving || !line || variance == null}
            onClick={() => void onPost()}
          >
            {posting
              ? "Posting…"
              : variance === 0
                ? queue.length > 1 && index + 1 < queue.length
                  ? "Next material"
                  : "Done"
                : queue.length > 1 && index + 1 < queue.length
                  ? "Post & next"
                  : "Post variance"}
          </button>
        </footer>
      </div>
    </div>
  );
};

export default SilaLiveStockCountPanel;
