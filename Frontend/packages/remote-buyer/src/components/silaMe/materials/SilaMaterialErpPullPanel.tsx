import React, { useEffect, useMemo, useState } from "react";
import { Loader, toastService } from "@vosox/shared-ui";
import { getProperties, type Property } from "../../../api/propertyApi";
import {
  getMaterialErpRoute,
  pullMaterialsFromErp,
  type SilaMaterialErpPullResult,
  type SilaMaterialErpRoute,
} from "../../../api/silaMe/silaMaterialsApi";
import { formatDateTime } from "./materialFormat";
import "../silaMeTheme.css";
import "./SilaMaterials.css";

interface SilaMaterialErpPullPanelProps {
  /** Called after a pull changed the material master. */
  onPulled: () => void;
}

interface PlantOption {
  code: string;
  propertyName: string;
}

const plantsFor = (properties: Property[], companyCode: string): PlantOption[] => {
  const code = companyCode.trim().toUpperCase();
  const byPlant = new Map<string, string>();
  for (const property of properties) {
    if (code && property.companyCode?.trim().toUpperCase() !== code) continue;
    const plant = property.plantCode?.trim().toUpperCase();
    if (!plant) continue;
    if (!byPlant.has(plant)) byPlant.set(plant, property.propertyName?.trim() || plant);
  }
  return Array.from(byPlant.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([plantCode, propertyName]) => ({ code: plantCode, propertyName }));
};

/** Pulls materials via GET_MATERIAL; plant multi-select from BuyerProperty mappings. */
const SilaMaterialErpPullPanel: React.FC<SilaMaterialErpPullPanelProps> = ({ onPulled }) => {
  const [properties, setProperties] = useState<Property[]>([]);
  const [companyCode, setCompanyCode] = useState("");
  const [selectedPlants, setSelectedPlants] = useState<Record<string, boolean>>({});
  const [route, setRoute] = useState<SilaMaterialErpRoute | null>(null);
  const [routeLoading, setRouteLoading] = useState(false);
  const [routeError, setRouteError] = useState<string | null>(null);
  const [pulling, setPulling] = useState(false);
  const [result, setResult] = useState<SilaMaterialErpPullResult | null>(null);

  useEffect(() => {
    let active = true;
    getProperties()
      .then((rows) => {
        if (!active) return;
        setProperties(rows);
        const codes = Array.from(new Set(rows.map((property) => property.companyCode?.trim().toUpperCase()).filter(Boolean)));
        if (codes.length > 0) setCompanyCode((current) => current || codes[0]);
      })
      .catch(() => {
        if (active) setProperties([]);
      });
    return () => {
      active = false;
    };
  }, []);

  const companyCodes = useMemo(
    () => Array.from(new Set(properties.map((property) => property.companyCode?.trim().toUpperCase()).filter(Boolean))) as string[],
    [properties],
  );

  const plantOptions = useMemo(() => plantsFor(properties, companyCode), [properties, companyCode]);

  useEffect(() => {
    setSelectedPlants({});
  }, [companyCode]);

  useEffect(() => {
    const code = companyCode.trim();
    setResult(null);
    if (!code) {
      setRoute(null);
      return undefined;
    }
    let active = true;
    setRouteLoading(true);
    setRouteError(null);
    const timer = window.setTimeout(async () => {
      try {
        const found = await getMaterialErpRoute(code);
        if (active) setRoute(found);
      } catch (err: unknown) {
        if (active) {
          setRoute(null);
          setRouteError(err instanceof Error ? err.message : "Could not load the ERP material API.");
        }
      } finally {
        if (active) setRouteLoading(false);
      }
    }, 300);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [companyCode]);

  const selectedCount = useMemo(
    () => plantOptions.filter((plant) => selectedPlants[plant.code]).length,
    [plantOptions, selectedPlants],
  );

  const selectAllPlants = () => {
    const next: Record<string, boolean> = {};
    for (const plant of plantOptions) next[plant.code] = true;
    setSelectedPlants(next);
  };

  const clearPlants = () => setSelectedPlants({});

  const togglePlant = (code: string) => {
    setSelectedPlants((current) => ({ ...current, [code]: !current[code] }));
  };

  const handlePull = async () => {
    setPulling(true);
    try {
      const plants = plantOptions.map((plant) => plant.code).filter((code) => selectedPlants[code]);
      const pulled = await pullMaterialsFromErp(companyCode, plants);
      setResult(pulled);
      toastService.success(`${pulled.read} record(s) read: ${pulled.new} new, ${pulled.changed} changed, ${pulled.failed} failed.`);
      if (pulled.new > 0 || pulled.changed > 0) onPulled();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not pull the materials from the ERP.");
    } finally {
      setPulling(false);
    }
  };

  return (
    <section className="sila-card">
      <div className="sila-card-header">
        <h2 className="sila-card-title">Pull from ERP</h2>
      </div>
      <div className="smat-erp">
        <p className="sila-help">
          Pulls all material types for the company — stock, non-stock / non-inventory and service —
          scoped by the plants you select. Plant assignments and valuation follow the selected plants.
        </p>

        <div className="smat-toolbar">
          <div className="sila-field">
            <label className="sila-label" htmlFor="smat-erp-company">Company code *</label>
            <input
              id="smat-erp-company"
              className="sila-input"
              list="smat-erp-company-codes"
              maxLength={20}
              value={companyCode}
              onChange={(event) => setCompanyCode(event.target.value.toUpperCase())}
            />
            <datalist id="smat-erp-company-codes">
              {companyCodes.map((code) => <option key={code} value={code} />)}
            </datalist>
          </div>
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={pulling || routeLoading || !companyCode.trim() || !route?.configured}
            onClick={handlePull}
          >
            {pulling ? "Pulling..." : selectedCount > 0 ? `Pull materials (${selectedCount} plant${selectedCount === 1 ? "" : "s"})` : "Pull all plants"}
          </button>
        </div>

        {plantOptions.length > 0 ? (
          <div className="smat-plant-panel">
            <div className="smat-plant-panel__header">
              <div>
                <h3 className="smat-plant-panel__title">Plants</h3>
                <p className="sila-help">
                  Multi-select plants from property mappings. Leave none selected to apply every plant present in the pull payload.
                </p>
              </div>
              <div className="smat-plant-actions">
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={selectAllPlants}>
                  Select all
                </button>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={clearPlants} disabled={selectedCount === 0}>
                  Clear
                </button>
              </div>
            </div>
            <div className="smat-plant-grid" role="group" aria-label="Plant selection">
              {plantOptions.map((plant) => {
                const checked = Boolean(selectedPlants[plant.code]);
                return (
                  <label
                    key={plant.code}
                    className={`smat-plant-card${checked ? " smat-plant-card--selected" : ""}`}
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => togglePlant(plant.code)}
                    />
                    <span className="smat-plant-card__code">{plant.code}</span>
                    <span className="smat-plant-card__name">{plant.propertyName}</span>
                  </label>
                );
              })}
            </div>
            <p className="sila-help">
              {selectedCount === 0
                ? "No plant filter — all plants in the ERP payload will be applied."
                : `${selectedCount} of ${plantOptions.length} plant${plantOptions.length === 1 ? "" : "s"} selected.`}
            </p>
          </div>
        ) : companyCode.trim() ? (
          <div className="sila-alert sila-alert--warning" role="status">
            No plant codes are mapped on properties for {companyCode}. Map plants under Properties, or pull without a plant filter.
          </div>
        ) : null}

        {routeLoading ? (
          <Loader size={20} message="Checking the ERP material API..." />
        ) : routeError ? (
          <div className="sila-alert sila-alert--danger" role="alert">{routeError}</div>
        ) : route && !route.configured ? (
          <div className="sila-alert sila-alert--warning" role="status">
            No GET_MATERIAL API is active for {companyCode} or ALL. Configure it under Integration first.
          </div>
        ) : route ? (
          <dl className="smat-facts">
            <div><dt>API</dt><dd>{route.configurationName || "—"}</dd></div>
            <div><dt>System</dt><dd>{route.systemName || "—"}</dd></div>
            <div><dt>Applies to</dt><dd>{route.entityCode || "—"}</dd></div>
            <div><dt>Last sync</dt><dd>{formatDateTime(route.lastSyncAt)}</dd></div>
          </dl>
        ) : null}
        <span className="sila-help">
          Live SAP calls are not performed from this agent task. Pull uses the configured GET_MATERIAL route when authorized operators run it.
          Stock snapshots never overwrite the SILA inventory ledger.
        </span>
        {route?.lastError && !result && <span className="sila-help">Last error: {route.lastError}</span>}
        {result && (
          <>
            <div className="smat-counts">
              <span className="sila-badge sila-badge--neutral">{result.read} read</span>
              <span className="sila-badge sila-badge--success">{result.new} new</span>
              <span className="sila-badge sila-badge--info">{result.changed} changed</span>
              <span className="sila-badge sila-badge--neutral">{result.unchanged} unchanged</span>
              <span className={`sila-badge sila-badge--${result.failed > 0 ? "danger" : "neutral"}`}>{result.failed} failed</span>
              <span className="sila-badge sila-badge--warning">{result.priceChanges} price change(s) for approval</span>
            </div>
            {result.failures.length > 0 && (
              <ul className="smat-messages" aria-label="Pull messages">
                {result.failures.map((message, index) => <li key={`${index}-${message}`}>{message}</li>)}
              </ul>
            )}
          </>
        )}
      </div>
    </section>
  );
};

export default SilaMaterialErpPullPanel;
