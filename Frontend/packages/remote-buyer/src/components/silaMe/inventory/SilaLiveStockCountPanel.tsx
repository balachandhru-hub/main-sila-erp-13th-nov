import React, { useEffect, useMemo, useRef, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  createLiveStockCount,
  formatQty,
  searchLiveInventory,
  searchMaterials,
  type SilaLiveInventoryRow,
  type SilaMaterial,
} from "../../../api/silaMe/silaInventoryApi";

type ScanMode = "barcode" | "photo";

interface SilaLiveStockCountPanelProps {
  open: boolean;
  locationId: string;
  locationLabel: string;
  initialCode?: string;
  onClose: () => void;
  onPosted: () => void;
}

/**
 * Compact Live Inventory stock-count dialog: scan/photo → material code → physical count → 701/702 variance.
 */
const SilaLiveStockCountPanel: React.FC<SilaLiveStockCountPanelProps> = ({
  open,
  locationId,
  locationLabel,
  initialCode = "",
  onClose,
  onPosted,
}) => {
  const [scanMode, setScanMode] = useState<ScanMode>("barcode");
  const [scanInput, setScanInput] = useState("");
  const [materialCode, setMaterialCode] = useState("");
  const [materialId, setMaterialId] = useState<string | null>(null);
  const [materialName, setMaterialName] = useState("");
  const [uom, setUom] = useState("EA");
  const [onHand, setOnHand] = useState<number | null>(null);
  const [physical, setPhysical] = useState("");
  const [resolving, setResolving] = useState(false);
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const barcodeRef = useRef<HTMLInputElement>(null);
  const photoRef = useRef<HTMLInputElement>(null);

  const variance = useMemo(() => {
    if (onHand == null || physical.trim() === "" || Number.isNaN(Number(physical))) return null;
    return Number(physical) - onHand;
  }, [onHand, physical]);

  const applyMaterial = async (code: string) => {
    const term = code.trim();
    if (!term || !locationId) return;
    setResolving(true);
    setError(null);
    try {
      const page = await searchMaterials(term);
      const material =
        page.find((row) => row.materialCode === term || row.barcode === term) ??
        page.find((row) => row.materialCode?.endsWith(term.replace(/^0+/, ""))) ??
        page[0];
      if (!material) {
        throw new Error("No material matches that code or barcode.");
      }
      setMaterialCode(material.materialCode);
      setMaterialId(material.id);
      setMaterialName(material.description || material.materialCode);
      setUom(material.baseUnitOfMeasure || "EA");
      setScanInput(material.materialCode);

      const rows: SilaLiveInventoryRow[] = await searchLiveInventory(material.materialCode, locationId, 0);
      const row = rows.find((item) => item.materialId === material.id) ?? rows[0];
      setOnHand(row?.onHandQty ?? 0);
      setPhysical(String(row?.onHandQty ?? 0));
    } catch (err: unknown) {
      setMaterialCode("");
      setMaterialId(null);
      setOnHand(null);
      setError(err instanceof Error ? err.message : "Could not resolve the material.");
    } finally {
      setResolving(false);
    }
  };

  useEffect(() => {
    if (!open) return;
    setScanMode("barcode");
    setScanInput(initialCode.trim());
    setMaterialCode("");
    setMaterialId(null);
    setMaterialName("");
    setUom("EA");
    setOnHand(null);
    setPhysical("");
    setError(null);
    setTimeout(() => barcodeRef.current?.focus(), 50);
    if (initialCode.trim() && locationId) {
      void applyMaterial(initialCode.trim());
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, initialCode, locationId]);

  const onScanSubmit = (event: React.FormEvent) => {
    event.preventDefault();
    void applyMaterial(scanInput);
  };

  const onPhotoPicked = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;
    setScanMode("photo");
    setError(null);
    toastService.info("Photo captured. Enter or confirm the material code.");
    barcodeRef.current?.focus();
  };

  const onPost = async () => {
    if (!locationId || !materialCode || physical.trim() === "") {
      setError("Scan a material and enter the physical count.");
      return;
    }
    const qty = Number(physical);
    if (Number.isNaN(qty) || qty < 0) {
      setError("Physical count must be zero or greater.");
      return;
    }
    setPosting(true);
    setError(null);
    try {
      const result = await createLiveStockCount({
        locationId,
        materialId,
        materialCodeOrBarcode: materialCode,
        physicalQty: qty,
        uom,
        reason: `Live Inventory stock count at ${locationLabel}`,
      });
      toastService.success(
        `${result.materialCode}: variance ${formatQty(result.varianceQty)} → SAP ${result.movementType}`,
      );
      onPosted();
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not post the stock count.");
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
            <p>{locationLabel}</p>
          </div>
          <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={onClose}>
            Close
          </button>
        </header>

        <div className="liv-sc-modes" role="tablist" aria-label="Scan method">
          <button
            type="button"
            role="tab"
            aria-selected={scanMode === "barcode"}
            className={`liv-sc-mode${scanMode === "barcode" ? " liv-sc-mode--on" : ""}`}
            onClick={() => {
              setScanMode("barcode");
              barcodeRef.current?.focus();
            }}
          >
            Scan barcode
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={scanMode === "photo"}
            className={`liv-sc-mode${scanMode === "photo" ? " liv-sc-mode--on" : ""}`}
            onClick={() => photoRef.current?.click()}
          >
            Take photo
          </button>
          <input
            ref={photoRef}
            type="file"
            accept="image/*"
            capture="environment"
            className="liv-sc-file"
            onChange={onPhotoPicked}
          />
        </div>

        <form className="liv-sc-scan" onSubmit={onScanSubmit}>
          <label className="sila-field">
            <span className="sila-label">{scanMode === "photo" ? "Material code (from photo)" : "Barcode / material code"}</span>
            <input
              ref={barcodeRef}
              className="sila-input"
              value={scanInput}
              onChange={(event) => setScanInput(event.target.value)}
              placeholder={scanMode === "barcode" ? "Scan or type barcode / material…" : "Enter material code…"}
              autoComplete="off"
            />
          </label>
          <button type="submit" className="sila-btn sila-btn--secondary" disabled={resolving || !scanInput.trim()}>
            {resolving ? "Resolving…" : "Apply"}
          </button>
        </form>

        <div className="liv-sc-grid">
          <div>
            <span className="sila-label">Material</span>
            <strong className="liv-sc-value">{materialCode || "—"}</strong>
            {materialName && <p className="liv-sc-sub">{materialName}</p>}
          </div>
          <div>
            <span className="sila-label">Stock in hand</span>
            <strong className="liv-sc-value">{onHand == null ? "—" : formatQty(onHand)}</strong>
            <p className="liv-sc-sub">{uom}</p>
          </div>
          <label className="sila-field">
            <span className="sila-label">Physical stock count</span>
            <input
              className="sila-input"
              type="number"
              min={0}
              step="any"
              value={physical}
              disabled={!materialCode}
              onChange={(event) => setPhysical(event.target.value)}
            />
          </label>
          <div>
            <span className="sila-label">Variance</span>
            <strong className={`liv-sc-value${variance != null && variance < 0 ? " liv-sc-value--neg" : variance != null && variance > 0 ? " liv-sc-value--pos" : ""}`}>
              {variance == null ? "—" : formatQty(variance)}
            </strong>
            <p className="liv-sc-sub">
              {variance == null ? "Enter physical count" : variance > 0 ? "SAP 701 (surplus)" : variance < 0 ? "SAP 702 (shortage)" : "No variance"}
            </p>
          </div>
        </div>

        {error && <p className="liv-sc-error">{error}</p>}

        <footer className="liv-sc-foot">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={onClose}>
            Cancel
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={posting || !materialCode || variance == null || variance === 0}
            onClick={() => void onPost()}
          >
            {posting ? "Posting…" : "Post variance"}
          </button>
        </footer>
      </div>
    </div>
  );
};

export default SilaLiveStockCountPanel;
