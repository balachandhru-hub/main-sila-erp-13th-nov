import React, { useRef, useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  discardPosBatch,
  previewPosConsumption,
  previewPosSales,
  processPosBatch,
  type SilaPosConsumptionPreview,
  type SilaPosImportResult,
  type SilaPosPreview,
} from "../../../api/silaMe/silaPosApi";
import { downloadPosTemplate, type SilaPosSource } from "../../../api/silaMe/silaPosMasterApi";
import SilaPosPreviewTable from "./SilaPosPreviewTable";
import { SilaPosConfirm, SilaPosSourceSelect } from "./SilaPosShared";
import { errorText } from "./posFormat";

interface SilaPosSalesUploadProps {
  sources: SilaPosSource[];
  canManage: boolean;
  /** Called after a batch is processed or discarded (refreshes the batch list). */
  onProcessed?: (result?: SilaPosImportResult) => void;
  /** POS Studio layout: drag-drop + info panel + controlled preview state. */
  studioMode?: boolean;
  /** Show only the review table (no upload dropzone). */
  reviewOnly?: boolean;
  preview?: SilaPosPreview | null;
  consumption?: SilaPosConsumptionPreview | null;
  onPreviewChange?: (preview: SilaPosPreview | null) => void;
  onConsumptionChange?: (consumption: SilaPosConsumptionPreview | null) => void;
  onSelectedIdsChange?: (ids: string[]) => void;
  onOpenPreview?: () => void;
  onOpenPost?: () => void;
}

/** Two-step sales upload: validate the file (preview), then process selected lines. */
const SilaPosSalesUpload: React.FC<SilaPosSalesUploadProps> = ({
  sources,
  canManage,
  onProcessed,
  studioMode = false,
  reviewOnly = false,
  preview: controlledPreview,
  consumption: controlledConsumption,
  onPreviewChange,
  onConsumptionChange,
  onSelectedIdsChange,
  onOpenPreview,
  onOpenPost,
}) => {
  const [sourceId, setSourceId] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const [busy, setBusy] = useState<"upload" | "process" | "discard" | "preview" | null>(null);
  const [localPreview, setLocalPreview] = useState<SilaPosPreview | null>(null);
  const [localConsumption, setLocalConsumption] = useState<SilaPosConsumptionPreview | null>(null);
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  const [confirmPost, setConfirmPost] = useState(false);
  const [pendingIds, setPendingIds] = useState<string[]>([]);
  const fileInput = useRef<HTMLInputElement>(null);

  const preview = controlledPreview !== undefined ? controlledPreview : localPreview;
  const consumption = controlledConsumption !== undefined ? controlledConsumption : localConsumption;

  const setPreview = (next: SilaPosPreview | null) => {
    if (onPreviewChange) onPreviewChange(next);
    else setLocalPreview(next);
  };

  const setConsumption = (next: SilaPosConsumptionPreview | null) => {
    if (onConsumptionChange) onConsumptionChange(next);
    else setLocalConsumption(next);
  };

  const resetFile = () => {
    setFile(null);
    if (fileInput.current) fileInput.current.value = "";
  };

  const assignFile = (next: File | null) => {
    if (!next) {
      setFile(null);
      return;
    }
    const name = next.name.toLowerCase();
    if (!name.endsWith(".xlsx") && !name.endsWith(".csv")) {
      toastService.error("Choose a .xlsx or .csv sales file.");
      return;
    }
    setFile(next);
  };

  const handleUpload = async () => {
    if (!file) return;
    setBusy("upload");
    try {
      const checked = await previewPosSales(file, sourceId || undefined);
      setPreview(checked);
      setConsumption(null);
      resetFile();
      toastService.success(`Batch ${checked.batchNumber}: ${checked.readyToProcess} of ${checked.totalRows} line${checked.totalRows === 1 ? "" : "s"} ready.`);
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not upload the sales file."));
    } finally {
      setBusy(null);
    }
  };

  const handlePreviewConsumption = async (transactionIds: string[]) => {
    if (!preview) return;
    setBusy("preview");
    onSelectedIdsChange?.(transactionIds);
    try {
      const next = await previewPosConsumption(preview.batchId, { transactionIds });
      setConsumption(next);
      toastService.success(`Consumption preview for ${next.lines.length} line${next.lines.length === 1 ? "" : "s"}.`);
      onOpenPreview?.();
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not preview consumption."));
    } finally {
      setBusy(null);
    }
  };

  const requestProcess = (transactionIds: string[]) => {
    if (busy !== null) return;
    onSelectedIdsChange?.(transactionIds);
    setPendingIds(transactionIds);
    setConfirmPost(true);
  };

  const handleProcess = async () => {
    if (!preview || busy !== null) return;
    setBusy("process");
    setConfirmPost(false);
    try {
      const processed = await processPosBatch(preview.batchId, { transactionIds: pendingIds, queueErpPosting: true });
      setPreview(null);
      setConsumption(null);
      onProcessed?.(processed);
      toastService.success(`Batch ${processed.batchNumber}: ${processed.processed} line${processed.processed === 1 ? "" : "s"} processed.`);
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not process the sales batch."));
    } finally {
      setBusy(null);
    }
  };

  const handleDiscard = async () => {
    if (!preview) return;
    setBusy("discard");
    try {
      await discardPosBatch(preview.batchId);
      toastService.success(`Upload ${preview.batchNumber} discarded.`);
      setPreview(null);
      setConsumption(null);
      setConfirmDiscard(false);
      onProcessed?.();
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not discard the upload."));
    } finally {
      setBusy(null);
    }
  };

  const handleTemplate = async () => {
    try {
      await downloadPosTemplate("SALES");
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not download the template."));
    }
  };

  if (!canManage) {
    return <div className="sila-alert sila-alert--warning" role="status">You do not have permission to upload POS sales.</div>;
  }

  return (
    <div className="srec-stack">
      {!reviewOnly && studioMode && (
        <div className="pstudio-grid-2">
          <section className="pstudio-panel pstudio-panel--mint">
            <h2 className="pstudio-panel__title">Upload sales file</h2>
            <p className="pstudio-panel__hint">XLSX (SalesData sheet) or CSV. Upload only stages a PREVIEW batch — nothing posts to inventory or SAP.</p>
            {sources.length > 0 && (
              <SilaPosSourceSelect id="spos-upload-source" sources={sources} value={sourceId} onChange={setSourceId} allowDefault />
            )}
            <div
              className="pstudio-drop"
              data-active={dragActive ? "true" : "false"}
              data-testid="pstudio-dropzone"
              onDragEnter={(event) => { event.preventDefault(); setDragActive(true); }}
              onDragOver={(event) => { event.preventDefault(); setDragActive(true); }}
              onDragLeave={() => setDragActive(false)}
              onDrop={(event) => {
                event.preventDefault();
                setDragActive(false);
                assignFile(event.dataTransfer.files?.[0] ?? null);
              }}
              onClick={() => fileInput.current?.click()}
              onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") fileInput.current?.click();
              }}
              role="button"
              tabIndex={0}
            >
              <strong>Drop POS sales file here</strong>
              <span>or choose .xlsx / .csv</span>
              {file && (
                <span className="srec-sub">
                  {file.name} · {(file.size / 1024).toFixed(1)} KB
                </span>
              )}
            </div>
            <input
              id="spos-upload-file"
              ref={fileInput}
              className="sila-input"
              type="file"
              accept=".xlsx,.csv"
              hidden
              disabled={preview !== null}
              onChange={(event) => assignFile(event.target.files?.[0] ?? null)}
            />
            <div className="srec-actions">
              <button type="button" className="sila-btn sila-btn--ghost" onClick={handleTemplate}>Download POS Template</button>
              <button type="button" className="sila-btn sila-btn--primary" onClick={handleUpload} disabled={!file || busy !== null || preview !== null}>
                {busy === "upload" ? "Checking..." : "Upload and validate"}
              </button>
            </div>
          </section>

          <section className="pstudio-panel pstudio-panel--lavender">
            <h2 className="pstudio-panel__title">File information</h2>
            <p className="pstudio-panel__hint">Mandatory nine columns stay valid. Optional enhanced columns are supported.</p>
            <ul className="pstudio-list">
              <li className="pstudio-list__item"><div><strong>Mandatory</strong><span className="srec-sub">BusinessDate, TransactionId, LineId, PosCode, Qty, UOM, OutletCode, Currency, Amount</span></div></li>
              <li className="pstudio-list__item"><div><strong>Optional</strong><span className="srec-sub">SourceSystem, TransactionType, OriginalTransactionId, OriginalLineId</span></div></li>
              <li className="pstudio-list__item"><div><strong>Example</strong><span className="srec-sub">2026-11-18 · 100045 · 34567 · 2 EA · FL10 · AED 380.00</span></div></li>
              <li className="pstudio-list__item"><div><strong>Safety</strong><span className="srec-sub">Upload never deducts stock or posts to SAP</span></div></li>
            </ul>
          </section>
        </div>
      )}

      {!reviewOnly && !studioMode && (
        <div className="srec-filters spos-toolbar">
          {sources.length > 0 && (
            <SilaPosSourceSelect id="spos-upload-source-classic" sources={sources} value={sourceId} onChange={setSourceId} allowDefault />
          )}
          <div className="sila-field srec-grow">
            <label className="sila-label" htmlFor="spos-upload-file-classic">Sales file (.xlsx or .csv)</label>
            <input
              id="spos-upload-file-classic"
              ref={fileInput}
              className="sila-input"
              type="file"
              accept=".xlsx,.csv"
              disabled={preview !== null}
              onChange={(event) => assignFile(event.target.files?.[0] ?? null)}
            />
            <span className="sila-help">
              Mandatory: BusinessDate, TransactionId, LineId, PosCode, Qty, UOM, OutletCode, Currency, Amount.
              Optional: SourceSystem, TransactionType, OriginalTransactionId, OriginalLineId.
            </span>
          </div>
          <div className="srec-actions">
            <button type="button" className="sila-btn sila-btn--ghost" onClick={handleTemplate}>Download POS Template</button>
            <button type="button" className="sila-btn sila-btn--primary" onClick={handleUpload} disabled={!file || busy !== null || preview !== null}>
              {busy === "upload" ? "Checking..." : "Upload and check"}
            </button>
          </div>
        </div>
      )}

      {preview && (
        <>
          {studioMode && (
            <div className="pstudio-kpis">
              <div className="pstudio-kpi pstudio-kpi--sky"><span>Total rows</span><strong>{preview.totalRows}</strong></div>
              <div className="pstudio-kpi pstudio-kpi--mint"><span>Valid</span><strong>{preview.validRows}</strong></div>
              <div className="pstudio-kpi pstudio-kpi--amber"><span>Warning / not ready</span><strong>{Math.max(0, preview.validRows - preview.readyToProcess)}</strong></div>
              <div className="pstudio-kpi pstudio-kpi--rose"><span>Invalid</span><strong>{preview.invalidRows}</strong></div>
              <div className="pstudio-kpi pstudio-kpi--lavender"><span>Duplicates</span><strong>{preview.duplicateRows}</strong></div>
              <div className="pstudio-kpi pstudio-kpi--blue"><span>Ready</span><strong>{preview.readyToProcess}</strong></div>
            </div>
          )}
          <SilaPosPreviewTable
            preview={preview}
            busy={busy}
            consumption={consumption}
            studioMode={studioMode}
            onProcess={requestProcess}
            onPreviewConsumption={handlePreviewConsumption}
            onDiscard={() => setConfirmDiscard(true)}
            onSelectedIdsChange={onSelectedIdsChange}
          />
          {studioMode && (
            <div className="srec-actions">
              <button type="button" className="sila-btn sila-btn--secondary" disabled={!consumption} onClick={() => onOpenPreview?.()}>
                Open consumption tab
              </button>
              <button type="button" className="sila-btn sila-btn--ghost" onClick={() => onOpenPost?.()}>
                Open post tab
              </button>
            </div>
          )}
        </>
      )}

      {confirmPost && preview && (
        <SilaPosConfirm
          heading="Confirm posting"
          content={`Process ${pendingIds.length} selected line${pendingIds.length === 1 ? "" : "s"} from batch ${preview.batchNumber}?`}
          description="Step 1 validates and updates SILA inventory locally. Step 2 calls the SAP API and posts. Step 3 updates the Transaction Tracker."
          confirmText="Confirm posting"
          busy={busy === "process"}
          onConfirm={handleProcess}
          onClose={() => setConfirmPost(false)}
        />
      )}

      {confirmDiscard && preview && (
        <SilaPosConfirm
          heading="Discard upload"
          content={`Discard upload ${preview.batchNumber}?`}
          description="Its lines are removed and nothing is deducted. You can upload a corrected file afterwards."
          confirmText="Discard"
          busy={busy === "discard"}
          onConfirm={handleDiscard}
          onClose={() => setConfirmDiscard(false)}
        />
      )}
    </div>
  );
};

export default SilaPosSalesUpload;
