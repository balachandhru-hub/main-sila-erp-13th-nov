import React, { useEffect, useState } from "react";
import { BarList, ChartCard, EmptyState, Loader, PageHeader } from "@vosox/shared-ui";
import { getMyLocations, type SilaLocation } from "../../../api/silaMe/silaInventoryApi";
import { getInventoryDashboard, type SilaInventoryDashboard } from "../../../api/silaMe/silaInventoryControlApi";
import SilaSearchableSelect from "../SilaSearchableSelect";
import "../silaMeTheme.css";
import "../../purchasing/Purchasing.css";
import "./SilaInventory.css";

/** Inventory analytics from the existing dashboard endpoint — no invented forecasts. */
const SilaInventoryAnalytics: React.FC = () => {
  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [locationId, setLocationId] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [dash, setDash] = useState<SilaInventoryDashboard | null>(null);

  useEffect(() => {
    getMyLocations()
      .then((data) => {
        const stocked = data.filter((x) => x.locationType !== "VENUE");
        setLocations(stocked);
        const home = stocked.find((x) => x.assigned) ?? stocked[0];
        if (home) setLocationId(home.id);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load locations."));
  }, []);

  useEffect(() => {
    if (!locationId) return;
    let active = true;
    setLoading(true);
    getInventoryDashboard({ locationId })
      .then((data) => {
        if (active) setDash(data);
      })
      .catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : "Could not load analytics.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [locationId]);

  return (
    <div className="sila-me sinv-page">
      <PageHeader title="Inventory Analytics" description="Consumption, movements, and value from the live inventory ledger." />
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
        <Loader size={28} message="Loading analytics…" />
      ) : error ? (
        <EmptyState variant="error" title="Couldn't load analytics" description={error} />
      ) : !dash ? (
        <EmptyState title="No data" description="Pick a location to see inventory analytics." />
      ) : (
        <div className="pdash-panels">
          <ChartCard title="Movement today" subtitle={dash.currency || undefined}>
            <BarList
              items={(dash.movementToday ?? []).map((row) => ({
                label: row.label || row.key,
                value: row.count,
              }))}
            />
          </ChartCard>
          <ChartCard title="Top consumption (7 days)">
            <BarList
              items={(dash.topConsumption ?? []).map((row) => ({
                label: row.materialCode || row.materialName || "Material",
                value: row.quantity,
              }))}
            />
          </ChartCard>
          <ChartCard title="Value by location">
            <BarList
              items={(dash.valueByLocation ?? []).map((row) => ({
                label: row.locationCode || row.locationName,
                value: row.value,
              }))}
            />
          </ChartCard>
          <ChartCard title="Transfer stages">
            <BarList
              items={(dash.transferStages ?? []).map((row) => ({
                label: row.label || row.tab || row.status,
                value: row.count,
              }))}
            />
          </ChartCard>
        </div>
      )}
    </div>
  );
};

export default SilaInventoryAnalytics;
