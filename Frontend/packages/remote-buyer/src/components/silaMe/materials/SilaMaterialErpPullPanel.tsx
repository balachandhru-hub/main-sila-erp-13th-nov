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
  companyCode: string;
}

const plantOptionsFrom = (properties: Property[]): PlantOption[] => {
  const byPlant = new Map<string, PlantOption>();
  for (const property of properties) {
    const plant = property.plantCode?.trim().toUpperCase();
    if (!plant) continue;
    if (byPlant.has(plant)) continue;
    byPlant.set(plant, {
      code: plant,
      propertyName: property.propertyName?.trim() || plant,
      companyCode: property.companyCode?.trim().toUpperCase() || "",
    });
  }
  return Array.from(byPlant.values()).sort((a, b) => a.code.localeCompare(b.code));
};

/** Plant-first GET_MATERIAL pull; company code is derived from selected property plants. */
const SilaMaterialErpPullPanel: React.FC<SilaMaterialErpPullPanelProps> = ({ onPulled }) => {
  const [properties, setProperties] = useState<Property[]>([]);
  const [selectedPlants, setSelectedPlants] = useState<Record<string, boolean>>({});
  const [route, setRoute] = useState<SilaMaterialErpRoute | null>(null);
  const [routeLoading, setRouteLoading] = useState(false);
  const [routeError, setRouteError] = useState<string | null>(null);
  const [pulling, setPulling] = useState(false);
  const [result, setResult] = useState<SilaMaterialErpPullResult | null>(null);
  const [propertiesError, setPropertiesError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    getProperties()
      .then((rows) => {
        if (!active) return;
        setProperties(rows);
        setPropertiesError(null);
      })
      .catch((err: unknown) => {
        if (!active) return;
        setProperties([]);
        setPropertiesError(err instanceof Error ? err.message : "Could not load properties.");
      });
    return () => {
      active = false;
    };
  }, []);

  const plantOptions = useMemo(() => plantOptionsFrom(properties), [properties]);

  const selectedPlantCodes = useMemo(
    () => plantOptions.map((plant) => plant.code).filter((code) => selectedPlants[code]),
    [plantOptions, selectedPlants],
  );

  const derivedCompanyCode = useMemo(() => {
    const companies = Array.from(
      new Set(
        selectedPlantCodes
          .map((code) => plantOptions.find((plant) => plant.code === code)?.companyCode || "")
          .filter(Boolean),
      ),
    );
    if (companies.length === 1) return companies[0];
    if (companies.length > 1) return "ALL";
    return "";
  }, [selectedPlantCodes, plantOptions]);

  const routeEntity = derivedCompanyCode || "ALL";

  useEffect(() => {
    setResult(null);
    let active = true;
    setRouteLoading(true);
    setRouteError(null);
    const timer = window.setTimeout(async () => {
      try {
        const found = await getMaterialErpRoute(routeEntity);
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
  }, [routeEntity]);

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
    if (selectedPlantCodes.length === 0) {
      toastService.error("Select one or more plants mapped under Properties.");
      return;
    }
    setPulling(true);
    try {
      const pulled = await pullMaterialsFromErp(selectedPlantCodes, derivedCompanyCode);
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
          Select plants first. The company code for the GET_MATERIAL route is taken from the property mapping of those plants.
          Pull includes all material types — stock, non-stock / non-inventory and service — and maps Description into the Description column.
        </p>

        {propertiesError && (
          <div className="sila-alert sila-alert--danger" role="alert">{propertiesError}</div>
        )}

        {plantOptions.length === 0 && !propertiesError ? (
          <div className="sila-alert sila-alert--warning" role="status">
            No plant codes are mapped on Properties yet. Set Plant code on each property, then return here to pull by plant.
          </div>
        ) : (
          <div className="smat-plant-panel">
            <div className="smat-plant-panel__header">
              <div>
                <h3 className="smat-plant-panel__title">Plants *</h3>
                <p className="sila-help">Multi-select plants from property mappings. Valuation and plant assignment follow the selection.</p>
              </div>
              <div className="smat-plant-actions">
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={selectAllPlants} disabled={plantOptions.length === 0}>
                  Select all
                </button>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={clearPlants} disabled={selectedPlantCodes.length === 0}>
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
                    <span className="smat-plant-card__name">
                      {plant.propertyName}
                      {plant.companyCode ? ` · Co. ${plant.companyCode}` : ""}
                    </span>
                  </label>
                );
              })}
            </div>
            <div className="smat-plant-meta">
              <span className="sila-help">
                {selectedPlantCodes.length === 0
                  ? "Select at least one plant to pull."
                  : `${selectedPlantCodes.length} of ${plantOptions.length} plant${plantOptions.length === 1 ? "" : "s"} selected.`}
              </span>
              <span className="smat-plant-meta__company">
                Company for API: <strong>{derivedCompanyCode || "—"}</strong>
              </span>
            </div>
          </div>
        )}

        <div className="smat-toolbar">
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={pulling || routeLoading || selectedPlantCodes.length === 0 || !route?.configured}
            onClick={handlePull}
          >
            {pulling
              ? "Pulling..."
              : selectedPlantCodes.length > 0
                ? `Pull materials (${selectedPlantCodes.length} plant${selectedPlantCodes.length === 1 ? "" : "s"})`
                : "Select plants to pull"}
          </button>
        </div>

        {routeLoading ? (
          <Loader size={20} message="Checking the ERP material API..." />
        ) : routeError ? (
          <div className="sila-alert sila-alert--danger" role="alert">{routeError}</div>
        ) : route && !route.configured ? (
          <div className="sila-alert sila-alert--warning" role="status">
            No GET_MATERIAL API is active for {routeEntity} or ALL. Configure it under Integration first, and map Material.Description to to_Description/results/0/ProductDescription (or rely on the automatic description fallback).
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
          Prefer $expand=to_Description on the Product path so Description fills the Description column. Stock snapshots never overwrite the SILA inventory ledger.
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
