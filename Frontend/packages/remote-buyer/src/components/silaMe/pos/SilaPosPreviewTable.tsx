import React, { useMemo, useState } from "react";
import type {
  SilaPosConsumptionPreview,
  SilaPosPreview,
  SilaPosPreviewRow,
} from "../../../api/silaMe/silaPosApi";
import { posLabel, posStatusBadgeClass } from "./posFormat";

type RowFilter = "ALL" | "READY" | "PROBLEMS";

interface SilaPosPreviewTableProps {
  preview: SilaPosPreview;
  busy: "upload" | "process" | "discard" | "preview" | null;
  consumption?: SilaPosConsumptionPreview | null;
  onProcess: (transactionIds: string[]) => void;
  onPreviewConsumption: (transactionIds: string[]) => void;
  onDiscard: () => void;
}

const isSelectable = (row: SilaPosPreviewRow): boolean =>
  row.status === "READY" && Boolean(row.saleTransactionId);

/** Counts and rows of a checked sales file, with line selection, consumption preview, Process and Discard. */
const SilaPosPreviewTable: React.FC<SilaPosPreviewTableProps> = ({
  preview,
  busy,
  consumption,
  onProcess,
  onPreviewConsumption,
  onDiscard,
}) => {
  const [filter, setFilter] = useState<RowFilter>("ALL");
  const [selected, setSelected] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {};
    for (const row of preview.rows) {
      if (isSelectable(row) && row.saleTransactionId) initial[row.saleTransactionId] = true;
    }
    return initial;
  });

  const selectableIds = useMemo(
    () => preview.rows.filter(isSelectable).map((row) => row.saleTransactionId!).filter(Boolean),
    [preview.rows],
  );

  const selectedIds = useMemo(
    () => selectableIds.filter((id) => selected[id]),
    [selectableIds, selected],
  );

  const rows = useMemo(() => {
    if (filter === "READY") return preview.rows.filter((row) => row.status === "READY");
    if (filter === "PROBLEMS") return preview.rows.filter((row) => row.status !== "READY");
    return preview.rows;
  }, [filter, preview.rows]);

  const notReady = preview.validRows - preview.readyToProcess;
  const toggleAllReady = (checked: boolean) => {
    setSelected((current) => {
      const next = { ...current };
      for (const id of selectableIds) next[id] = checked;
      return next;
    });
  };

  return (
    <section className="sila-card" aria-live="polite">
      <div className="sila-card-header">
        <h2 className="sila-card-title">
          Batch {preview.batchNumber}
          {preview.posSourceName ? ` · ${preview.posSourceName}` : ""}
        </h2>
        <div className="srec-actions">
          <button type="button" className="sila-btn sila-btn--ghost" onClick={onDiscard} disabled={busy !== null}>
            Discard
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={() => onPreviewConsumption(selectedIds)}
            disabled={busy !== null || selectedIds.length === 0}
          >
            {busy === "preview" ? "Previewing..." : "Consumption preview"}
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            onClick={() => onProcess(selectedIds)}
            disabled={busy !== null || selectedIds.length === 0}
          >
            {busy === "process"
              ? "Processing..."
              : `Post selected (${selectedIds.length})`}
          </button>
        </div>
      </div>
      <div className="sila-card-body srec-stack">
        <div className="srec-counts">
          {[
            ["Total rows", preview.totalRows],
            ["Valid", preview.validRows],
            ["Invalid", preview.invalidRows],
            ["Duplicates", preview.duplicateRows],
            ["Unmapped POS codes", preview.unmappedPosCodes],
            ["Invalid outlets", preview.invalidOutlets],
            ["Invalid UOM", preview.invalidUom],
            ["Recipe not ready", preview.recipeNotReady],
            ["Ready to process", preview.readyToProcess],
            ["Selected", selectedIds.length],
          ].map(([label, value]) => (
            <div className="sila-meta-item" key={label}>
              <span className="sila-meta-label">{label}</span>
              <span className="sila-meta-value">{value}</span>
            </div>
          ))}
        </div>
        {notReady > 0 && (
          <div className="sila-alert sila-alert--warning" role="status">
            {notReady} stored line{notReady === 1 ? " is" : "s are"} not ready and cannot be selected for posting.
            Fix mapping or recipe, then re-upload or reprocess from the Transaction Tracker.
          </div>
        )}
        <div className="srec-actions" style={{ flexWrap: "wrap", gap: "0.75rem" }}>
          <label className="sila-choice" htmlFor="spos-select-all-ready">
            <input
              id="spos-select-all-ready"
              type="checkbox"
              checked={selectableIds.length > 0 && selectedIds.length === selectableIds.length}
              onChange={(event) => toggleAllReady(event.target.checked)}
            />
            Select all ready
          </label>
          <label className="sila-label" htmlFor="spos-row-filter">Show</label>
          <select
            id="spos-row-filter"
            className="sila-input"
            value={filter}
            onChange={(event) => setFilter(event.target.value as RowFilter)}
          >
            <option value="ALL">All</option>
            <option value="READY">Valid / ready</option>
            <option value="PROBLEMS">Warning / invalid</option>
          </select>
        </div>
        {rows.length === 0 ? (
          <p className="sila-cell-muted">No lines match this filter.</p>
        ) : (
          <div className="sila-table-wrap">
            <table className="sila-table">
              <thead>
                <tr>
                  <th scope="col">Select</th>
                  <th scope="col" className="srec-num">Row</th>
                  <th scope="col">Business date</th>
                  <th scope="col">Transaction / line</th>
                  <th scope="col">POS code</th>
                  <th scope="col">Type</th>
                  <th scope="col" className="srec-num">Qty</th>
                  <th scope="col">Outlet</th>
                  <th scope="col">Recipe</th>
                  <th scope="col">Amount</th>
                  <th scope="col">Status</th>
                  <th scope="col">Message</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const id = row.saleTransactionId || "";
                  const canSelect = isSelectable(row);
                  return (
                    <tr key={row.rowNumber}>
                      <td>
                        <input
                          type="checkbox"
                          aria-label={`Select row ${row.rowNumber}`}
                          disabled={!canSelect}
                          checked={canSelect && Boolean(selected[id])}
                          onChange={(event) => {
                            if (!id) return;
                            setSelected((current) => ({ ...current, [id]: event.target.checked }));
                          }}
                        />
                      </td>
                      <td className="srec-num">{row.rowNumber}</td>
                      <td>{row.businessDate || "—"}</td>
                      <td>{row.transactionId || "—"} / {row.lineId || "1"}</td>
                      <td>{row.posCode || "—"}</td>
                      <td>{row.transactionType || "SALE"}</td>
                      <td className="srec-num">{row.quantity || "—"} {row.uom || ""}</td>
                      <td>
                        {row.outletCode || "—"}
                        {row.outletLocationName && <span className="srec-sub">{row.outletLocationName}</span>}
                      </td>
                      <td>
                        {row.recipeCode || "—"}
                        {row.recipeVersion != null && <span className="srec-sub">v{row.recipeVersion}</span>}
                      </td>
                      <td>
                        {row.amount || "—"}
                        {row.currency && <span className="srec-sub">{row.currency}</span>}
                      </td>
                      <td><span className={posStatusBadgeClass(row.status)}>{posLabel(row.status)}</span></td>
                      <td className="srec-message">{row.message || "—"}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
        {preview.rowsTruncated && <span className="sila-help">Only the first 500 lines (problems first) are listed.</span>}

        {consumption && (
          <section className="sila-card" data-testid="pos-consumption-preview">
            <div className="sila-card-header">
              <h3 className="sila-card-title">Consumption preview · {consumption.batchNumber}</h3>
            </div>
            <div className="sila-card-body srec-stack">
              <div className="srec-counts">
                <div className="sila-meta-item">
                  <span className="sila-meta-label">Sales amount</span>
                  <span className="sila-meta-value">{consumption.totalSalesAmount}</span>
                </div>
                <div className="sila-meta-item">
                  <span className="sila-meta-label">Est. consumption cost</span>
                  <span className="sila-meta-value">{consumption.totalConsumptionCost}</span>
                </div>
              </div>
              <p className="sila-help">
                Preview only — stock is not reserved or deducted. Live SAP posting stays disabled until explicitly authorized
                (SilaPos:QueueErpPosting).
              </p>
              <div className="sila-table-wrap">
                <table className="sila-table">
                  <thead>
                    <tr>
                      <th scope="col">POS / recipe</th>
                      <th scope="col">Outlet / plant / SLoc</th>
                      <th scope="col" className="srec-num">Sold</th>
                      <th scope="col" className="srec-num">Sales</th>
                      <th scope="col" className="srec-num">Cost</th>
                      <th scope="col" className="srec-num">Food %</th>
                      <th scope="col">Ingredients</th>
                    </tr>
                  </thead>
                  <tbody>
                    {consumption.lines.map((line) => (
                      <tr key={line.saleTransactionId}>
                        <td>
                          <span className="sila-cell-strong">{line.posCode}</span>
                          <span className="srec-sub">
                            {line.recipeCode || "—"}
                            {line.recipeVersion != null ? ` v${line.recipeVersion}` : ""}
                          </span>
                        </td>
                        <td>
                          {line.outletCode}
                          {line.outletLocationName && <span className="srec-sub">{line.outletLocationName}</span>}
                          <span className="srec-sub">
                            {[line.plantCode, line.storageLocationCode].filter(Boolean).join(" / ") || "Plant/SLoc from outlet mapping"}
                          </span>
                        </td>
                        <td className="srec-num">{line.quantitySold} {line.uom || ""}</td>
                        <td className="srec-num">{line.salesAmount ?? "—"} {line.currency || ""}</td>
                        <td className="srec-num">
                          {line.costPending && !line.consumptionCost ? "COST PENDING" : (line.consumptionCost ?? "—")}
                        </td>
                        <td className="srec-num">{line.foodCostPercent != null ? `${line.foodCostPercent}%` : "—"}</td>
                        <td>
                          {line.ingredients.length === 0 ? (line.message || "—") : (
                            <ul className="srec-sub" style={{ margin: 0, paddingLeft: "1.1rem" }}>
                              {line.ingredients.map((ingredient) => (
                                <li key={ingredient.materialId}>
                                  {ingredient.materialCode} · {ingredient.quantity} {ingredient.baseUom || ""}
                                  {ingredient.costPending ? " · COST PENDING" : ""}
                                </li>
                              ))}
                            </ul>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </section>
        )}
      </div>
    </section>
  );
};

export default SilaPosPreviewTable;
