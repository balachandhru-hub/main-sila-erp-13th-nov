import React, { useEffect, useMemo, useState } from "react";
import { Loader, toastService } from "@vosox/shared-ui";
import { getProperties, updateProperty, type Property } from "../../../api/propertyApi";
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

/** Plant-first GET_MATERIAL pull with inline property → plant mapping. */
const SilaMaterialErpPullPanel: React.FC<SilaMaterialErpPullPanelProps> = ({ onPulled }) => {
  const [properties, setProperties] = useState<Property[]>([]);
  const [plantDrafts, setPlantDrafts] = useState<Record<string, string>>({});
  const [selectedPropertyIds, setSelectedPropertyIds] = useState<Record<string, boolean>>({});
  const [savingId, setSavingId] = useState<string | null>(null);
  const [route, setRoute] = useState<SilaMaterialErpRoute | null>(null);
  const [routeLoading, setRouteLoading] = useState(false);
  const [routeError, setRouteError] = useState<string | null>(null);
  const [pulling, setPulling] = useState(false);
  const [result, setResult] = useState<SilaMaterialErpPullResult | null>(null);
  const [propertiesError, setPropertiesError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;
    getProperties()
      .then((rows) => {
        if (!active) return;
        setProperties(rows);
        const drafts: Record<string, string> = {};
        for (const property of rows) {
          drafts[property.id] = (property.plantCode || property.companyCode || "").trim().toUpperCase();
        }
        setPlantDrafts(drafts);
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
  }, [reloadKey]);

  const sortedProperties = useMemo(
    () => [...properties].sort((a, b) => (a.propertyName || "").localeCompare(b.propertyName || "")),
    [properties],
  );

  const selectedForPull = useMemo(() => {
    return sortedProperties.filter((property) => {
      if (!selectedPropertyIds[property.id]) return false;
      const plant = (plantDrafts[property.id] || property.plantCode || "").trim().toUpperCase();
      return plant.length > 0 && property.plantCode?.trim().toUpperCase() === plant;
    });
  }, [sortedProperties, selectedPropertyIds, plantDrafts]);

  const selectedPlantCodes = useMemo(
    () => Array.from(new Set(selectedForPull.map((property) => property.plantCode.trim().toUpperCase()))),
    [selectedForPull],
  );

  const derivedCompanyCode = useMemo(() => {
    const companies = Array.from(
      new Set(selectedForPull.map((property) => property.companyCode?.trim().toUpperCase()).filter(Boolean)),
    );
    if (companies.length === 1) return companies[0];
    if (companies.length > 1) return "ALL";
    return "";
  }, [selectedForPull]);

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

  const setDraft = (propertyId: string, value: string) => {
    setPlantDrafts((current) => ({ ...current, [propertyId]: value.toUpperCase() }));
  };

  const toggleProperty = (propertyId: string) => {
    setSelectedPropertyIds((current) => ({ ...current, [propertyId]: !current[propertyId] }));
  };

  const selectAllMapped = () => {
    const next: Record<string, boolean> = {};
    for (const property of sortedProperties) {
      if (property.plantCode?.trim()) next[property.id] = true;
    }
    setSelectedPropertyIds(next);
  };

  const clearSelection = () => setSelectedPropertyIds({});

  const savePlantMapping = async (property: Property) => {
    const plant = (plantDrafts[property.id] || "").trim().toUpperCase();
    if (!plant) {
      toastService.error("Enter a plant code for this property.");
      return;
    }
    if (!property.companyCode?.trim() || !property.propertyName?.trim()) {
      toastService.error("Property needs a company code and name before mapping a plant.");
      return;
    }
    setSavingId(property.id);
    try {
      await updateProperty(property.id, {
        companyCode: property.companyCode.trim(),
        plantCode: plant,
        propertyName: property.propertyName.trim(),
        masterApprovalFlowId: property.masterApprovalFlowId ?? null,
      });
      toastService.success(`Mapped plant ${plant} to ${property.propertyName}.`);
      setReloadKey((key) => key + 1);
      setSelectedPropertyIds((current) => ({ ...current, [property.id]: true }));
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not save the plant mapping.");
    } finally {
      setSavingId(null);
    }
  };

  const useCompanyAsPlant = (property: Property) => {
    const company = property.companyCode?.trim().toUpperCase() || "";
    if (!company) {
      toastService.error("This property has no company code to copy.");
      return;
    }
    setDraft(property.id, company);
  };

  const handlePull = async () => {
    if (selectedPlantCodes.length === 0) {
      toastService.error("Select properties with a saved plant code, or map plant codes below first.");
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

  const mappedCount = sortedProperties.filter((property) => property.plantCode?.trim()).length;

  return (
    <section className="sila-card">
      <div className="sila-card-header">
        <h2 className="sila-card-title">Pull from ERP</h2>
      </div>
      <div className="smat-erp">
        <p className="sila-help">
          Map each property’s company / property to a SAP plant code, then multi-select plants to pull.
          Pull includes stock, non-stock / non-inventory and service materials. Description maps into the Description column.
        </p>

        {propertiesError && (
          <div className="sila-alert sila-alert--danger" role="alert">{propertiesError}</div>
        )}

        {sortedProperties.length === 0 && !propertiesError ? (
          <div className="sila-alert sila-alert--warning" role="status">
            No properties found. Create a property with company and plant codes under Properties first.
          </div>
        ) : (
          <div className="smat-plant-panel">
            <div className="smat-plant-panel__header">
              <div>
                <h3 className="smat-plant-panel__title">Map property → plant</h3>
                <p className="sila-help">
                  Enter the SAP plant for each property (often the same as company code). Save the mapping, then select plants to pull.
                  {mappedCount > 0 ? ` ${mappedCount} of ${sortedProperties.length} mapped.` : ""}
                </p>
              </div>
              <div className="smat-plant-actions">
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={selectAllMapped} disabled={mappedCount === 0}>
                  Select mapped
                </button>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={clearSelection} disabled={selectedPlantCodes.length === 0}>
                  Clear
                </button>
              </div>
            </div>

            <div className="smat-plant-map" role="list" aria-label="Property to plant mapping">
              {sortedProperties.map((property) => {
                const draft = plantDrafts[property.id] ?? "";
                const savedPlant = property.plantCode?.trim().toUpperCase() || "";
                const dirty = draft.trim().toUpperCase() !== savedPlant;
                const checked = Boolean(selectedPropertyIds[property.id]) && !!savedPlant && !dirty;
                return (
                  <div key={property.id} className={`smat-plant-map-row${checked ? " smat-plant-map-row--selected" : ""}`} role="listitem">
                    <label className="smat-plant-map-row__check">
                      <input
                        type="checkbox"
                        checked={Boolean(selectedPropertyIds[property.id])}
                        disabled={!savedPlant || dirty}
                        onChange={() => toggleProperty(property.id)}
                        aria-label={`Select plant for ${property.propertyName}`}
                      />
                    </label>
                    <div className="smat-plant-map-row__identity">
                      <span className="smat-plant-map-row__name">{property.propertyName || "—"}</span>
                      <span className="sila-help">
                        Company {property.companyCode || "—"}
                        {savedPlant ? ` · Saved plant ${savedPlant}` : " · Plant not mapped"}
                      </span>
                    </div>
                    <div className="sila-field smat-plant-map-row__plant">
                      <label className="sila-label" htmlFor={`smat-plant-${property.id}`}>Plant code</label>
                      <input
                        id={`smat-plant-${property.id}`}
                        className="sila-input"
                        maxLength={20}
                        placeholder={property.companyCode ? `e.g. ${property.companyCode}` : "SAP plant"}
                        value={draft}
                        onChange={(event) => setDraft(property.id, event.target.value)}
                      />
                    </div>
                    <div className="smat-plant-map-row__actions">
                      <button
                        type="button"
                        className="sila-btn sila-btn--secondary sila-btn--sm"
                        onClick={() => useCompanyAsPlant(property)}
                        disabled={!property.companyCode?.trim()}
                      >
                        Use company
                      </button>
                      <button
                        type="button"
                        className="sila-btn sila-btn--primary sila-btn--sm"
                        disabled={savingId === property.id || !draft.trim() || !dirty}
                        onClick={() => savePlantMapping(property)}
                      >
                        {savingId === property.id ? "Saving..." : dirty ? "Save plant" : "Saved"}
                      </button>
                    </div>
                  </div>
                );
              })}
            </div>

            <div className="smat-plant-meta">
              <span className="sila-help">
                {selectedPlantCodes.length === 0
                  ? "Save plant codes and select properties to pull."
                  : `${selectedPlantCodes.length} plant${selectedPlantCodes.length === 1 ? "" : "s"} selected: ${selectedPlantCodes.join(", ")}`}
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
                : "Map and select plants to pull"}
          </button>
        </div>

        {routeLoading ? (
          <Loader size={20} message="Checking the ERP material API..." />
        ) : routeError ? (
          <div className="sila-alert sila-alert--danger" role="alert">{routeError}</div>
        ) : route && !route.configured ? (
          <div className="sila-alert sila-alert--warning" role="status">
            No GET_MATERIAL API is active for {routeEntity} or ALL. Configure it under Integration first.
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
          Prefer $expand=to_Description on the Product path so Description fills the Description column (not the material number).
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
