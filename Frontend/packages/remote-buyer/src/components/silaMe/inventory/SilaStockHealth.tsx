import React, { useEffect, useState } from "react";
import { EmptyState, Loader, PageHeader } from "@vosox/shared-ui";
import {
  formatQty,
  getMyLocations,
  searchLiveInventory,
  type SilaLiveInventoryRow,
  type SilaLocation,
} from "../../../api/silaMe/silaInventoryApi";
import { getInventoryDashboard } from "../../../api/silaMe/silaInventoryControlApi";
import SilaSearchableSelect from "../SilaSearchableSelect";
import SilaStockHealthCard from "./SilaStockHealthCard";
import "../silaMeTheme.css";
import "./SilaInventory.css";

interface Props {
  onOpenLive?: (health?: string) => void;
}

/** Stock health workspace: dashboard health bands + low/out lists from live inventory. */
const SilaStockHealth: React.FC<Props> = ({ onOpenLive }) => {
  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [locationId, setLocationId] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [health, setHealth] = useState<{ healthy: number; low: number; out: number; excess: number } | null>(null);
  const [month, setMonth] = useState(new Date().getUTCMonth() + 1);
  const [lowRows, setLowRows] = useState<SilaLiveInventoryRow[]>([]);
  const [outRows, setOutRows] = useState<SilaLiveInventoryRow[]>([]);

  useEffect(() => {
    getMyLocations()
      .then((data) => {
        setLocations(data.filter((x) => x.locationType !== "VENUE"));
        const assigned = data.find((x) => x.assigned && x.locationType !== "VENUE") ?? data.find((x) => x.locationType !== "VENUE");
        if (assigned) setLocationId(assigned.id);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load locations."));
  }, []);

  useEffect(() => {
    if (!locationId) return;
    let active = true;
    setLoading(true);
    setError(null);
    Promise.all([
      getInventoryDashboard({ locationId }),
      searchLiveInventory("", locationId, 0, "LOW"),
      searchLiveInventory("", locationId, 0, "OUT"),
    ])
      .then(([dash, low, out]) => {
        if (!active) return;
        setHealth(dash.health ?? null);
        setMonth(dash.month || new Date().getUTCMonth() + 1);
        setLowRows(low);
        setOutRows(out);
      })
      .catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : "Could not load stock health.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [locationId]);

  const renderTable = (title: string, rows: SilaLiveInventoryRow[], filter: string) => (
    <section className="sila-card">
      <div className="sila-card-header">
        <h2 className="sila-card-title">{title}</h2>
        {onOpenLive && (
          <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpenLive(filter)}>
            Open in Live Inventory
          </button>
        )}
      </div>
      {rows.length === 0 ? (
        <EmptyState title="None" description={`No ${title.toLowerCase()} items at this location.`} />
      ) : (
        <div className="sila-table-wrap">
          <table className="sila-table">
            <thead>
              <tr>
                <th scope="col">Material</th>
                <th scope="col">Description</th>
                <th scope="col" className="sinv-num">Available</th>
                <th scope="col">UOM</th>
                <th scope="col" className="sinv-num">Reorder</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.materialId}>
                  <td><span className="sila-cell-strong">{row.materialCode}</span></td>
                  <td>{row.description}</td>
                  <td className="sinv-num">{formatQty(row.availableQty ?? row.onHandQty)}</td>
                  <td>{row.baseUom}</td>
                  <td className="sinv-num">{row.reorderPoint != null ? formatQty(row.reorderPoint) : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );

  return (
    <div className="sila-me sinv-page">
      <PageHeader title="Stock Health" description="Low, out, and excess stock from maintained thresholds — not invented cut-offs." />
      <section className="sila-card sinv-filters">
        <SilaSearchableSelect
          label="Location"
          value={locationId}
          clearable={false}
          options={locations.map((location) => ({
            value: location.id,
            label: `${location.locationCode} · ${location.locationName}`,
          }))}
          onChange={setLocationId}
        />
      </section>
      {loading ? (
        <Loader size={28} message="Loading stock health…" />
      ) : error ? (
        <EmptyState variant="error" title="Couldn't load health" description={error} />
      ) : (
        <>
          {health && <SilaStockHealthCard health={health} month={month} />}
          {renderTable("Low stock", lowRows, "LOW")}
          {renderTable("Out of stock", outRows, "OUT")}
        </>
      )}
    </div>
  );
};

export default SilaStockHealth;
