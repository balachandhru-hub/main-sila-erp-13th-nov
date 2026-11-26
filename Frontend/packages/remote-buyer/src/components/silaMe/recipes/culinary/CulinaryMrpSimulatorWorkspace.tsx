import React, { useMemo, useState } from "react";
import {
  computeMrpRow,
  DEMO_EVENT_MRP_ROWS,
  type ComputedMrpRow,
  type DemoMrpMaterialRow,
} from "./culinarySimulatorsMath";
import "./CulinarySuite.css";

interface SavedMrpScenario {
  name: string;
  guestCount: number;
  totalShortageCostAed: number;
  shortageLineCount: number;
}

const CulinaryMrpSimulatorWorkspace: React.FC = () => {
  const [guestCount, setGuestCount] = useState(500);
  const [rows, setRows] = useState<DemoMrpMaterialRow[]>(DEMO_EVENT_MRP_ROWS);
  const [groupFilter, setGroupFilter] = useState("ALL");
  const [statusFilter, setStatusFilter] = useState("ALL");
  const [savedScenarios, setSavedScenarios] = useState<SavedMrpScenario[]>([]);

  const computed: ComputedMrpRow[] = useMemo(
    () => rows.map((row) => computeMrpRow(guestCount, row)),
    [guestCount, rows]
  );

  const filtered = useMemo(
    () =>
      computed.filter((row) => {
        if (groupFilter !== "ALL" && row.materialGroup !== groupFilter) return false;
        if (statusFilter !== "ALL" && row.status !== statusFilter) return false;
        return true;
      }),
    [computed, groupFilter, statusFilter]
  );

  const summary = useMemo(() => {
    const shortageItems = computed.filter((r) => r.shortageQty > 0 && r.materialExists).length;
    const missingMaterials = computed.filter((r) => !r.materialExists).length;
    const totalProcurementCost = computed.reduce((s, r) => s + r.estimatedProcurementCostAed, 0);
    const totalTransferQty = computed.reduce((s, r) => s + r.recommendedTransferQty, 0);
    return { shortageItems, missingMaterials, totalProcurementCost, totalTransferQty };
  }, [computed]);

  const updateRowField = (
    id: string,
    field: "uptakePercent" | "availableStock" | "incomingPoQty" | "prepBufferPercent" | "usableYieldPercent" | "altStoreStock",
    value: number
  ) => {
    setRows((prev) => prev.map((r) => (r.id === id ? { ...r, [field]: Math.max(0, value) } : r)));
  };

  const saveScenario = () => {
    setSavedScenarios((prev) => [
      {
        name: `Scenario ${prev.length + 1} (${guestCount} guests)`,
        guestCount,
        totalShortageCostAed: Math.round(summary.totalProcurementCost * 100) / 100,
        shortageLineCount: summary.shortageItems,
      },
      ...prev.slice(0, 3),
    ]);
  };

  const resetDemo = () => {
    setGuestCount(500);
    setRows(DEMO_EVENT_MRP_ROWS);
    setGroupFilter("ALL");
    setStatusFilter("ALL");
  };

  const salmonRow = computed.find((r) => r.id === "mrp-salmon");

  return (
    <div className="cs2-shell" data-testid="culinary-mrp-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Interactive Event Material Requirement Planning (MRP) Simulator</h2>
          <p className="cs2-subtitle">
            Phase 0 interactive demonstration (§33–§37) for Corporate Gala Dinner (<code>EVT-2027-0001</code>).
            Sliders and inline assumptions recalculate net shortages, 311 transfer options, pack-size rounding, and procurement cost instantaneously.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={saveScenario}>
            Save Scenario Snapshot
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={resetDemo}>
            Reset to §35/§37 Baseline
          </button>
        </div>
      </div>

      <div className="cs2-banner" role="status">
        <div>
          <strong>DEMONSTRATION SIMULATION — NO STOCK RESERVATIONS OR POs CREATED</strong>
          <div>
            Moving sliders or editing assumptions only computes local deterministic MRP values. Production stock reservations,
            311 transfers, and PO drafts remain gated to Phases 2B/2C.
          </div>
        </div>
        <span className="cs2-badge cs2-badge--demo">Interactive Demo</span>
      </div>

      {/* Guest slider & Transparent Salmon Formula Walkthrough */}
      <div className="cs2-grid-2">
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Event Demand Parameters</h3>
            <span className="cs2-badge cs2-badge--indigo">EVT-2027-0001 · Banquet Kitchen</span>
          </div>
          <div className="cs2-slider-row">
            <label className="sila-label" htmlFor="cs2-mrp-guests">
              Expected Guests: <strong>{guestCount}</strong>
            </label>
            <input
              id="cs2-mrp-guests"
              type="range"
              min={50}
              max={1500}
              step={10}
              value={guestCount}
              onChange={(e) => setGuestCount(Number(e.target.value))}
            />
            <input
              type="number"
              className="sila-input"
              aria-label="Event expected guests"
              min={1}
              max={10000}
              value={guestCount}
              onChange={(e) => setGuestCount(Math.max(1, Number(e.target.value) || 1))}
            />
          </div>
          <div className="cs2-form-grid" style={{ marginTop: 12 }}>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mrp-grp">Material Group</label>
              <select id="cs2-mrp-grp" className="sila-select" value={groupFilter} onChange={(e) => setGroupFilter(e.target.value)}>
                <option value="ALL">All groups</option>
                {Array.from(new Set(rows.map((r) => r.materialGroup))).map((g) => (
                  <option key={g} value={g}>{g}</option>
                ))}
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-mrp-st">Requirement Status</label>
              <select id="cs2-mrp-st" className="sila-select" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="ALL">All statuses</option>
                <option value="SHORTAGE">Shortage (PO Needed)</option>
                <option value="OK">Covered by Stock / Incoming</option>
                <option value="MATERIAL_CREATION_REQUIRED">Missing Material</option>
              </select>
            </div>
          </div>
        </section>

        {salmonRow && (
          <section className="cs2-card" data-testid="mrp-salmon-trace">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Transparent Formula Trace — Atlantic Salmon (§35)</h3>
              <span className="cs2-badge cs2-badge--demo">No Double-Counting</span>
            </div>
            <div className="cs2-empty-note" style={{ lineHeight: 1.7, color: "#1e293b" }}>
              <div>1. Expected portions: <strong>{guestCount}</strong> × {salmonRow.uptakePercent}% = <strong>{salmonRow.expectedPortions} portions</strong></div>
              <div>2. Edible requirement: {salmonRow.expectedPortions} × {salmonRow.ediblePortionGrams}g = <strong>{salmonRow.edibleQty} kg</strong></div>
              <div>3. Buffered (+{salmonRow.prepBufferPercent}% prep): <strong>{salmonRow.bufferedEdibleQty} kg</strong></div>
              <div>4. Raw requirement (÷ {salmonRow.usableYieldPercent}% yield): <strong>{salmonRow.rawRequiredQty} kg</strong></div>
              <div>
                5. Remaining shortage: {salmonRow.rawRequiredQty} − {salmonRow.netAvailableStock} (stock) − {salmonRow.incomingPoQty} (PO) ={" "}
                <strong>{salmonRow.shortageQty} kg</strong> (Rounded PO: <strong>{salmonRow.roundedOrderQty} kg</strong>)
              </div>
            </div>
          </section>
        )}
      </div>

      <div className="cs2-kpi-grid">
        <div className="cs2-kpi cs2-kpi--rose">
          <span className="cs2-kpi__label">Materials w/ Shortage</span>
          <span className="cs2-kpi__value">{summary.shortageItems}</span>
          <span className="cs2-kpi__hint">Eligible for transfer / PO</span>
        </div>
        <div className="cs2-kpi cs2-kpi--amber">
          <span className="cs2-kpi__label">Missing Materials</span>
          <span className="cs2-kpi__value">{summary.missingMaterials}</span>
          <span className="cs2-kpi__hint">Cannot order until created</span>
        </div>
        <div className="cs2-kpi cs2-kpi--cyan">
          <span className="cs2-kpi__label">Recommended 311 Transfers</span>
          <span className="cs2-kpi__value">{summary.totalTransferQty.toFixed(1)}</span>
          <span className="cs2-kpi__hint">Same-plant alt-store units</span>
        </div>
        <div className="cs2-kpi cs2-kpi--accent">
          <span className="cs2-kpi__label">Est. Procurement Cost</span>
          <span className="cs2-kpi__value">AED {summary.totalProcurementCost.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
          <span className="cs2-kpi__hint">Includes pack-size rounding</span>
        </div>
      </div>

      {savedScenarios.length > 0 && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Saved Simulation Scenarios (Comparison)</h3>
          </div>
          <div className="cs2-actions-row">
            {savedScenarios.map((sc, idx) => (
              <div key={`${sc.name}-${idx}`} className="cs2-badge cs2-badge--indigo" style={{ padding: "6px 12px" }}>
                {sc.name}: {sc.shortageLineCount} shortages · AED {sc.totalShortageCostAed.toLocaleString()}
              </div>
            ))}
          </div>
        </section>
      )}

      {/* Consolidated MRP Table */}
      <section className="cs2-card">
        <div className="cs2-card__head">
          <h3 className="cs2-card__title">Consolidated Event Material Requirement Table (Editable Assumptions)</h3>
        </div>
        <div className="cs2-table-wrap">
          <table className="cs2-table" data-testid="mrp-table">
            <thead>
              <tr>
                <th>Material</th>
                <th className="cs2-num">Uptake %</th>
                <th className="cs2-num">Required</th>
                <th className="cs2-num">Avail. Stock</th>
                <th className="cs2-num">Alt-Store</th>
                <th className="cs2-num">Incoming PO</th>
                <th className="cs2-num">Shortage</th>
                <th className="cs2-num">Rec. Transfer</th>
                <th className="cs2-num">Rec. PO (Rounded)</th>
                <th className="cs2-num">Est. Cost</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((row) => (
                <tr key={row.id}>
                  <td>
                    <strong>{row.materialName}</strong>
                    <div className="cs2-empty-note">
                      {row.materialCode ?? "UNCREATED"} · {row.materialGroup} · Pack {row.packSize} {row.baseUom}
                    </div>
                  </td>
                  <td className="cs2-num">
                    <input
                      type="number"
                      className="sila-input"
                      style={{ width: 68, textAlign: "right" }}
                      min={0}
                      max={200}
                      aria-label={`Uptake % for ${row.materialName}`}
                      value={row.uptakePercent}
                      onChange={(e) => updateRowField(row.id, "uptakePercent", Number(e.target.value) || 0)}
                    />
                  </td>
                  <td className="cs2-num"><strong>{row.rawRequiredQty.toFixed(2)} {row.baseUom}</strong></td>
                  <td className="cs2-num">
                    <input
                      type="number"
                      className="sila-input"
                      style={{ width: 74, textAlign: "right" }}
                      min={0}
                      aria-label={`Available stock for ${row.materialName}`}
                      value={row.availableStock}
                      onChange={(e) => updateRowField(row.id, "availableStock", Number(e.target.value) || 0)}
                    />
                  </td>
                  <td className="cs2-num">
                    <input
                      type="number"
                      className="sila-input"
                      style={{ width: 68, textAlign: "right" }}
                      min={0}
                      aria-label={`Alternative store stock for ${row.materialName}`}
                      value={row.altStoreStock}
                      onChange={(e) => updateRowField(row.id, "altStoreStock", Number(e.target.value) || 0)}
                    />
                  </td>
                  <td className="cs2-num">
                    <input
                      type="number"
                      className="sila-input"
                      style={{ width: 74, textAlign: "right" }}
                      min={0}
                      aria-label={`Incoming PO for ${row.materialName}`}
                      value={row.incomingPoQty}
                      onChange={(e) => updateRowField(row.id, "incomingPoQty", Number(e.target.value) || 0)}
                    />
                  </td>
                  <td className="cs2-num"><strong>{row.shortageQty.toFixed(2)} {row.baseUom}</strong></td>
                  <td className="cs2-num">{row.recommendedTransferQty.toFixed(2)} {row.baseUom}</td>
                  <td className="cs2-num">
                    {row.materialExists
                      ? `${row.roundedOrderQty.toFixed(2)} ${row.baseUom}`
                      : "N/A"}
                  </td>
                  <td className="cs2-num">AED {row.estimatedProcurementCostAed.toLocaleString(undefined, { minimumFractionDigits: 2 })}</td>
                  <td>
                    {row.status === "OK" && <span className="cs2-badge cs2-badge--live">Covered</span>}
                    {row.status === "TRANSFER_READY" && <span className="cs2-badge cs2-badge--demo">311 Transfer</span>}
                    {row.status === "SHORTAGE" && <span className="cs2-badge cs2-badge--warn">PO Needed</span>}
                    {row.status === "MATERIAL_CREATION_REQUIRED" && (
                      <span className="cs2-badge cs2-badge--danger">Material Req.</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
};

export default CulinaryMrpSimulatorWorkspace;
