import React, { useEffect, useMemo, useState } from "react";
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
  /** Selected materials — physical counts entered together, variances posted in one go. */
  targets?: LiveStockCountTarget[];
  onClose: () => void;
  onPosted: () => void;
}

type CountLine = {
  key: string;
  materialId: string | null;
  materialCode: string;
  materialName: string;
  uom: string;
  onHand: number;
  physical: string;
};

/**
 * Live Inventory stock-count dialog: enter physical counts, post every variance (701/702) together.
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
  const [lines, setLines] = useState<CountLine[]>([]);
  const [resolving, setResolving] = useState(false);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const resolveTarget = async (target: LiveStockCountTarget): Promise<CountLine> => {
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

    const hand = onHand ?? 0;
    return {
      key: materialId || materialCode,
      materialId,
      materialCode,
      materialName,
      uom,
      onHand: hand,
      physical: String(hand),
    };
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

    setLines([]);
    setError(null);
    if (list.length === 0) {
      setError("Select materials in Material availability, or enter a material in Search inventory.");
      return;
    }

    let cancelled = false;
    setResolving(true);
    (async () => {
      try {
        const resolved: CountLine[] = [];
        for (const target of list) {
          resolved.push(await resolveTarget(target));
        }
        if (!cancelled) setLines(resolved);
      } catch (err: unknown) {
        if (!cancelled) {
          setLines([]);
          setError(err instanceof Error ? err.message : "Could not resolve the materials.");
        }
      } finally {
        if (!cancelled) setResolving(false);
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initialCode, locationId, targets]);

  const rows = useMemo(
    () =>
      lines.map((line) => {
        const qty = line.physical.trim() === "" ? NaN : Number(line.physical);
        const variance = Number.isNaN(qty) ? null : qty - line.onHand;
        return { line, variance, qty };
      }),
    [lines],
  );

  const varianceLines = rows.filter((row) => row.variance != null && row.variance !== 0);
  const allPhysicalValid = rows.every((row) => !Number.isNaN(row.qty) && (row.qty as number) >= 0);

  const setPhysical = (key: string, value: string) => {
    setLines((current) =>
      current.map((line) => (line.key === key ? { ...line, physical: value } : line)),
    );
  };

  const onPostAll = async () => {
    if (!locationId) {
      setError("Select a location before stock count.");
      return;
    }
    if (!allPhysicalValid) {
      setError("Enter a physical count (zero or greater) for every material.");
      return;
    }
    if (varianceLines.length === 0) {
      setError("No variances to post. Change a physical count that differs from stock in hand.");
      return;
    }

    setPosting(true);
    setError(null);
    const ok: string[] = [];
    const failed: string[] = [];
    try {
      for (const row of varianceLines) {
        try {
          const result = await createLiveStockCount({
            locationId,
            materialId: row.line.materialId,
            materialCodeOrBarcode: row.line.materialCode,
            physicalQty: row.qty as number,
            uom: row.line.uom,
            reason: `Live Inventory stock count at ${locationLabel}`,
          });
          ok.push(
            `${result.materialCode}: ${formatQty(result.varianceQty)} → SAP ${result.movementType}`,
          );
        } catch (err: unknown) {
          failed.push(
            `${row.line.materialCode}: ${err instanceof Error ? err.message : "post failed"}`,
          );
        }
      }

      if (ok.length > 0) {
        toastService.success(
          ok.length === 1
            ? ok[0]
            : `${ok.length} variances posted.${ok.length <= 3 ? ` ${ok.join("; ")}` : ""}`,
        );
        onPosted();
      }
      if (failed.length > 0) {
        setError(failed.join(" · "));
        if (ok.length === 0) {
          toastService.error("Stock count posting failed.");
        } else {
          toastService.error(`${failed.length} material(s) failed to post.`);
        }
        return;
      }
      onClose();
    } finally {
      setPosting(false);
    }
  };

  if (!open) return null;

  return (
    <div className="liv-sc-backdrop" role="presentation" onClick={onClose}>
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
            <p>
              {locationLabel}
              {lines.length > 1 ? ` · ${lines.length} materials` : ""}
            </p>
          </div>
          <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={onClose}>
            Close
          </button>
        </header>

        {resolving && <p className="liv-sc-sub">Resolving materials…</p>}

        {!resolving && lines.length > 0 && (
          <div className="liv-sc-batch">
            {rows.map(({ line, variance }) => (
              <div key={line.key} className="liv-sc-grid liv-sc-batch__row">
                <div>
                  <span className="sila-label">Material</span>
                  <strong className="liv-sc-value">{line.materialCode}</strong>
                  {line.materialName && <p className="liv-sc-sub">{line.materialName}</p>}
                </div>
                <div>
                  <span className="sila-label">Stock in hand</span>
                  <strong className="liv-sc-value">{formatQty(line.onHand)}</strong>
                  <p className="liv-sc-sub">{line.uom}</p>
                </div>
                <label className="sila-field">
                  <span className="sila-label">Physical stock count</span>
                  <input
                    className="sila-input"
                    type="number"
                    min={0}
                    step="any"
                    value={line.physical}
                    disabled={posting}
                    onChange={(event) => setPhysical(line.key, event.target.value)}
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
                          : "No variance"}
                  </p>
                </div>
              </div>
            ))}
          </div>
        )}

        {error && <p className="liv-sc-error">{error}</p>}

        <footer className="liv-sc-foot">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={posting || resolving || lines.length === 0 || !allPhysicalValid || varianceLines.length === 0}
            onClick={() => void onPostAll()}
          >
            {posting
              ? "Posting…"
              : varianceLines.length > 1
                ? `Post ${varianceLines.length} variances`
                : "Post variance"}
          </button>
        </footer>
      </div>
    </div>
  );
};

export default SilaLiveStockCountPanel;
