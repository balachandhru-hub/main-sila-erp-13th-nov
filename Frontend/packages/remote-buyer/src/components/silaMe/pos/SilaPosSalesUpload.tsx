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
import SilaPosImportSummary from "./SilaPosImportSummary";
import SilaPosPreviewTable from "./SilaPosPreviewTable";
import { SilaPosConfirm, SilaPosSourceSelect } from "./SilaPosShared";
import { errorText } from "./posFormat";

interface SilaPosSalesUploadProps {
  sources: SilaPosSource[];
  canManage: boolean;
  /** Called after a batch is processed or discarded (refreshes the batch list). */
  onProcessed: () => void;
}

/** Two-step sales upload: validate the file (preview), then process the batch. */
const SilaPosSalesUpload: React.FC<SilaPosSalesUploadProps> = ({ sources, canManage, onProcessed }) => {
  const [sourceId, setSourceId] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState<"upload" | "process" | "discard" | "preview" | null>(null);
  const [preview, setPreview] = useState<SilaPosPreview | null>(null);
  const [consumption, setConsumption] = useState<SilaPosConsumptionPreview | null>(null);
  const [result, setResult] = useState<SilaPosImportResult | null>(null);
  const [confirmDiscard, setConfirmDiscard] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);

  const resetFile = () => {
    setFile(null);
    if (fileInput.current) fileInput.current.value = "";
  };

  const handleUpload = async () => {
    if (!file) return;
    setBusy("upload");
    setResult(null);
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
    try {
      const next = await previewPosConsumption(preview.batchId, { transactionIds });
      setConsumption(next);
      toastService.success(`Consumption preview for ${next.lines.length} line${next.lines.length === 1 ? "" : "s"}.`);
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not preview consumption."));
    } finally {
      setBusy(null);
    }
  };

  const handleProcess = async (transactionIds: string[]) => {
    if (!preview) return;
    setBusy("process");
    try {
      // QueueErpPosting follows server config (false in UAT until live SAP is authorized).
      const processed = await processPosBatch(preview.batchId, { transactionIds });
      setResult(processed);
      setPreview(null);
      setConsumption(null);
      onProcessed();
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
      onProcessed();
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
      <div className="srec-filters spos-toolbar">
        {sources.length > 0 && (
          <SilaPosSourceSelect id="spos-upload-source" sources={sources} value={sourceId} onChange={setSourceId} allowDefault />
        )}
        <div className="sila-field srec-grow">
          <label className="sila-label" htmlFor="spos-upload-file">Sales file (.xlsx or .csv)</label>
          <input
            id="spos-upload-file"
            ref={fileInput}
            className="sila-input"
            type="file"
            accept=".xlsx,.csv"
            disabled={preview !== null}
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
          <span className="sila-help">
            Mandatory: BusinessDate, TransactionId, LineId, PosCode, Qty, UOM, OutletCode, Currency, Amount.
            Optional: SourceSystem, TransactionType, OriginalTransactionId, OriginalLineId. XLSX uses the SalesData sheet.
          </span>
        </div>
        <div className="srec-actions">
          <button type="button" className="sila-btn sila-btn--ghost" onClick={handleTemplate}>Download POS Template</button>
          <button type="button" className="sila-btn sila-btn--primary" onClick={handleUpload} disabled={!file || busy !== null || preview !== null}>
            {busy === "upload" ? "Checking..." : "Upload and check"}
          </button>
        </div>
      </div>

      {preview && (
        <SilaPosPreviewTable
          preview={preview}
          busy={busy}
          consumption={consumption}
          onProcess={handleProcess}
          onPreviewConsumption={handlePreviewConsumption}
          onDiscard={() => setConfirmDiscard(true)}
        />
      )}

      {result && <SilaPosImportSummary result={result} onClose={() => setResult(null)} />}

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
