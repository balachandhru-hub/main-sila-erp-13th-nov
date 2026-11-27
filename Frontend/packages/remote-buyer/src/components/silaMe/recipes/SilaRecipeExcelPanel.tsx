import React, { useRef, useState } from "react";
import { Download, FileDown, Upload } from "lucide-react";
import { Modal, toastService } from "@vosox/shared-ui";
import type { SilaImportPreview } from "../../../api/silaMe/silaRecipeToolsApi";

interface SilaRecipeExcelPanelProps {
  /** e.g. "recipe", "family", "category" — used in labels and messages. */
  noun: string;
  /** Shows template download and upload; export only needs view access. */
  canImport: boolean;
  onTemplate: () => Promise<void>;
  onExport: () => Promise<void>;
  onPreview: (file: File) => Promise<SilaImportPreview>;
  onConfirm: (file: File) => Promise<SilaImportPreview>;
  /** Called after the import was saved. */
  onImported: () => void;
  /** Render square icon buttons (tooltips and aria-labels keep the wording). */
  iconOnly?: boolean;
}

const MAX_BYTES = 5 * 1024 * 1024;

type Busy = "" | "template" | "export" | "preview" | "import";

/** Download template, export, and upload an Excel file (preview first, then an all-or-nothing import). */
const SilaRecipeExcelPanel: React.FC<SilaRecipeExcelPanelProps> = ({
  noun,
  canImport,
  onTemplate,
  onExport,
  onPreview,
  onConfirm,
  onImported,
  iconOnly = false,
}) => {
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState<Busy>("");
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<SilaImportPreview | null>(null);

  const download = async (kind: "template" | "export", action: () => Promise<void>) => {
    setBusy(kind);
    try {
      await action();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "The download failed.");
    } finally {
      setBusy("");
    }
  };

  const handleFile = async (chosen: File | undefined) => {
    if (inputRef.current) inputRef.current.value = "";
    if (!chosen) return;
    if (!chosen.name.toLowerCase().endsWith(".xlsx")) {
      toastService.error("Choose an .xlsx file (start from the template).");
      return;
    }
    if (chosen.size > MAX_BYTES) {
      toastService.error("The file is larger than 5 MB.");
      return;
    }
    setBusy("preview");
    try {
      const result = await onPreview(chosen);
      setFile(chosen);
      setPreview(result);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not read the file.");
    } finally {
      setBusy("");
    }
  };

  const close = () => {
    setPreview(null);
    setFile(null);
  };

  const handleConfirm = async () => {
    if (!file) return;
    setBusy("import");
    try {
      const result = await onConfirm(file);
      toastService.success(
        `Import done: ${result.newCount} new, ${result.changedCount} changed, ${result.unchangedCount} unchanged.`,
      );
      close();
      onImported();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not import the file.");
    } finally {
      setBusy("");
    }
  };

  const changes = preview ? preview.newCount + preview.changedCount : 0;
  const canConfirm = !!preview && preview.invalidRows === 0 && changes > 0;

  const iconClass = "sila-studio-overview-btn";
  const iconProps = { size: 17, strokeWidth: 2, "aria-hidden": true } as const;

  return (
    <>
      {canImport && (
        iconOnly ? (
          <button
            type="button"
            className={iconClass}
            disabled={busy !== ""}
            onClick={() => download("template", onTemplate)}
            title={busy === "template" ? "Downloading template..." : "Download Template"}
            aria-label="Download Template"
            data-testid="recipe-download-template"
          >
            <FileDown {...iconProps} />
          </button>
        ) : (
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--secondary"
            disabled={busy !== ""}
            onClick={() => download("template", onTemplate)}
          >
            <FileDown size={15} strokeWidth={1.9} aria-hidden="true" />
            <span>{busy === "template" ? "Downloading..." : "Download Template"}</span>
          </button>
        )
      )}
      {iconOnly ? (
        <button
          type="button"
          className={iconClass}
          disabled={busy !== ""}
          onClick={() => download("export", onExport)}
          title={busy === "export" ? "Exporting..." : "Download Excel"}
          aria-label="Download Excel"
          data-testid="recipe-download-excel"
        >
          <Download {...iconProps} />
        </button>
      ) : (
        <button
          type="button"
          className="sila-studio-btn sila-studio-btn--secondary"
          disabled={busy !== ""}
          onClick={() => download("export", onExport)}
        >
          <Download size={15} strokeWidth={1.9} aria-hidden="true" />
          <span>{busy === "export" ? "Exporting..." : "Download Excel"}</span>
        </button>
      )}
      {canImport && (
        <>
          {iconOnly ? (
            <button
              type="button"
              className={iconClass}
              disabled={busy !== ""}
              onClick={() => inputRef.current?.click()}
              title={busy === "preview" ? "Reading file..." : "Upload Excel"}
              aria-label="Upload Excel"
              data-testid="recipe-upload-excel"
            >
              <Upload {...iconProps} />
            </button>
          ) : (
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--secondary"
              disabled={busy !== ""}
              onClick={() => inputRef.current?.click()}
            >
              <Upload size={15} strokeWidth={1.9} aria-hidden="true" />
              <span>{busy === "preview" ? "Reading..." : "Upload Excel"}</span>
            </button>
          )}
          <input
            ref={inputRef}
            type="file"
            accept=".xlsx"
            hidden
            aria-label={`${noun} Excel file`}
            onChange={(event) => handleFile(event.target.files?.[0])}
          />
        </>
      )}

      {preview && (
        <Modal
          isOpen
          onClose={close}
          size="xl"
          headerProps={{ heading: `Import preview (${noun})`, subHeading: preview.fileName }}
          footerProps={{
            secondaryButton: { text: "Cancel", variant: "secondary", onClick: close, disabled: busy === "import" },
            primaryButton: { text: "Confirm import", onClick: handleConfirm, loading: busy === "import", disabled: !canConfirm },
          }}
        >
          <div className="sila-root sila-me srec-stack">
            <div className="srec-badges">
              <span className="sila-badge sila-badge--neutral">{preview.totalRows} rows</span>
              <span className="sila-badge sila-badge--success">{preview.validRows} valid</span>
              <span className={`sila-badge sila-badge--${preview.invalidRows > 0 ? "danger" : "neutral"}`}>{preview.invalidRows} invalid</span>
              <span className="sila-badge sila-badge--info">{preview.newCount} new</span>
              <span className="sila-badge sila-badge--info">{preview.changedCount} changed</span>
              <span className="sila-badge sila-badge--neutral">{preview.unchangedCount} unchanged</span>
            </div>
            {preview.invalidRows > 0 ? (
              <div className="sila-alert sila-alert--warning" role="status">
                The import is all-or-nothing: fix the rows below in the file and upload it again.
              </div>
            ) : changes === 0 ? (
              <div className="sila-alert sila-alert--success" role="status">Nothing to import: every record is unchanged.</div>
            ) : null}
            {preview.errors.length > 0 && (
              <div className="sila-table-wrap srec-error-list">
                <table className="sila-table">
                  <thead>
                    <tr>
                      <th scope="col">Sheet</th>
                      <th scope="col" className="srec-num">Row</th>
                      <th scope="col">Problem</th>
                    </tr>
                  </thead>
                  <tbody>
                    {preview.errors.map((error) => (
                      <tr key={`${error.sheet}-${error.row}-${error.message}`}>
                        <td>{error.sheet}</td>
                        <td className="srec-num">{error.row}</td>
                        <td className="srec-message">{error.message}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </Modal>
      )}
    </>
  );
};

export default SilaRecipeExcelPanel;
