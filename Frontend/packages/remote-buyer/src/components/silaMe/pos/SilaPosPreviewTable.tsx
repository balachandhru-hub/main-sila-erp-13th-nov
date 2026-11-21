import React, { useEffect, useMemo, useState } from "react";
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
  onSelectedIdsChange?: (ids: string[]) => void;
  studioMode?: boolean;
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
  onSelectedIdsChange,
  studioMode = false,
}) => {
  const [filter, setFilter] = useState<RowFilter>("ALL");
  const [search, setSearch] = useState("");
  const [drawerRow, setDrawerRow] = useState<SilaPosPreviewRow | null>(null);
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

  useEffect(() => {
    onSelectedIdsChange?.(selectedIds);
  }, [selectedIds, onSelectedIdsChange]);

  const rows = useMemo(() => {
    const q = search.trim().toLowerCase();
    return preview.rows.filter((row) => {
      if (filter === "READY" && row.status !== "READY") return false;
      if (filter === "PROBLEMS" && row.status === "READY") return false;
      if (!q) return true;
      const hay = [
        row.businessDate,
        row.transactionId,
        row.posCode,
        row.outletCode,
        row.recipeCode,
        row.message,
        row.status,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return hay.includes(q);
    });
  }, [filter, preview.rows, search]);

  const notReady = preview.validRows - preview.readyToProcess;
  const toggleAllReady = (checked: boolean) => {
    setSelected((current) => {
      const next = { ...current };
      for (const id of selectableIds) next[id] = checked;
      return next;
    });
  };

  const consumptionFor = (row: SilaPosPreviewRow) =>
    consumption?.lines.find((line) => line.saleTransactionId === row.saleTransactionId) ?? null;

  return (
    <section className={studioMode ? "pstudio-panel" : "sila-card"} aria-live="polite" data-testid="pstudio-review-table">
      <div className={studioMode ? undefined : "sila-card-header"} style={studioMode ? { display: "flex", flexWrap: "wrap", justifyContent: "space-between", gap: "0.75rem" } : undefined}>
        <h2 className={studioMode ? "pstudio-panel__title" : "sila-card-title"}>
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

      <div className={studioMode ? "srec-stack" : "sila-card-body srec-stack"}>
        {studioMode ? (
          <div className="pstudio-kpis">
            <div className="pstudio-kpi pstudio-kpi--sky"><span>Uploaded lines</span><strong>{preview.totalRows}</strong></div>
            <div className="pstudio-kpi pstudio-kpi--mint"><span>Valid</span><strong>{preview.validRows}</strong></div>
            <div className="pstudio-kpi pstudio-kpi--amber"><span>Warning</span><strong>{Math.max(0, notReady)}</strong></div>
            <div className="pstudio-kpi pstudio-kpi--rose"><span>Invalid</span><strong>{preview.invalidRows}</strong></div>
            <div className="pstudio-kpi pstudio-kpi--blue"><span>Selected</span><strong>{selectedIds.length}</strong></div>
            <div className="pstudio-kpi pstudio-kpi--lavender"><span>Ready</span><strong>{preview.readyToProcess}</strong></div>
          </div>
        ) : (
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
        )}

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
          <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => toggleAllReady(false)}>
            Clear selection
          </button>
          <input
            className="sila-input"
            style={{ minWidth: "12rem", maxWidth: "18rem" }}
            placeholder="Search POS / outlet / recipe…"
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            aria-label="Search sales lines"
          />
          {(["ALL", "READY", "PROBLEMS"] as RowFilter[]).map((chip) => (
            <button
              key={chip}
              type="button"
              className="pstudio-tab"
              aria-selected={filter === chip}
              onClick={() => setFilter(chip)}
            >
              {chip === "ALL" ? "All" : chip === "READY" ? "Valid / ready" : "Warning / invalid"}
            </button>
          ))}
        </div>

        {rows.length === 0 ? (
          <div className="pstudio-empty">No lines match this filter.</div>
        ) : (
          <div className="sila-table-wrap">
            <table className="sila-table">
              <thead>
                <tr>
                  <th scope="col">Select</th>
                  <th scope="col">Business date</th>
                  <th scope="col">POS transaction</th>
                  <th scope="col">POS code</th>
                  <th scope="col">Menu / recipe</th>
                  <th scope="col">Outlet</th>
                  <th scope="col" className="srec-num">Qty</th>
                  <th scope="col">Sales amount</th>
                  <th scope="col">Validation</th>
                  <th scope="col">Message</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => {
                  const id = row.saleTransactionId || "";
                  const canSelect = isSelectable(row);
                  return (
                    <tr
                      key={row.rowNumber}
                      style={{ cursor: "pointer" }}
                      onClick={() => setDrawerRow(row)}
                    >
                      <td onClick={(event) => event.stopPropagation()}>
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
                      <td>{row.businessDate || "—"}</td>
                      <td>{row.transactionId || "—"} / {row.lineId || "1"}</td>
                      <td>{row.posCode || "—"}</td>
                      <td>
                        {row.recipeCode || "—"}
                        {row.recipeVersion != null && <span className="srec-sub">v{row.recipeVersion}</span>}
                      </td>
                      <td>
                        {row.outletCode || "—"}
                        {row.outletLocationName && <span className="srec-sub">{row.outletLocationName}</span>}
                      </td>
                      <td className="srec-num">{row.quantity || "—"} {row.uom || ""}</td>
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

        {consumption && !studioMode && (
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
                Preview only — stock is not reserved or deducted. Confirm posting deducts SILA inventory and posts the SAP material document via API.
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

      {drawerRow && (
        <>
          <div className="pstudio-drawer__backdrop" onClick={() => setDrawerRow(null)} aria-hidden />
          <aside className="pstudio-drawer" role="dialog" aria-label="POS line details" data-testid="pstudio-line-drawer">
            <div className="srec-actions" style={{ justifyContent: "space-between" }}>
              <h3 className="pstudio-panel__title">Line detail</h3>
              <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => setDrawerRow(null)}>Close</button>
            </div>
            <ul className="pstudio-list">
              <li className="pstudio-list__item"><div><strong>POS source</strong><span className="srec-sub">{drawerRow.transactionId} / {drawerRow.lineId} · {drawerRow.businessDate}</span></div></li>
              <li className="pstudio-list__item"><div><strong>Menu mapping</strong><span className="srec-sub">POS {drawerRow.posCode} → {drawerRow.recipeCode || "unmapped"}{drawerRow.recipeVersion != null ? ` v${drawerRow.recipeVersion}` : ""}</span></div></li>
              <li className="pstudio-list__item"><div><strong>Outlet</strong><span className="srec-sub">{drawerRow.outletCode || "—"}{drawerRow.outletLocationName ? ` · ${drawerRow.outletLocationName}` : ""}</span></div></li>
              <li className="pstudio-list__item"><div><strong>Validation</strong><span className="srec-sub">{posLabel(drawerRow.status)} · {drawerRow.message || "—"}</span></div></li>
              <li className="pstudio-list__item"><div><strong>Posting eligibility</strong><span className="srec-sub">{isSelectable(drawerRow) ? "Ready to select and post" : "Not selectable until mapping / UOM / recipe is fixed"}</span></div></li>
            </ul>
            {(() => {
              const line = consumptionFor(drawerRow);
              if (!line) {
                return <div className="pstudio-empty">Run Consumption preview for ingredient breakdown on this line.</div>;
              }
              return (
                <div className="srec-stack">
                  <h4 className="pstudio-panel__title">Ingredient breakdown</h4>
                  <ul className="pstudio-list">
                    {line.ingredients.map((ingredient) => (
                      <li key={ingredient.materialId} className="pstudio-list__item">
                        <div>
                          <strong>{ingredient.materialCode || ingredient.materialId}</strong>
                          <span className="srec-sub">{ingredient.materialDescription || "—"}</span>
                        </div>
                        <span>
                          {ingredient.quantity} {ingredient.baseUom || ""}
                          {ingredient.costPending ? " · COST PENDING" : ingredient.estimatedCost != null ? ` · ${ingredient.estimatedCost}` : ""}
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              );
            })()}
          </aside>
        </>
      )}
    </section>
  );
};

export default SilaPosPreviewTable;
