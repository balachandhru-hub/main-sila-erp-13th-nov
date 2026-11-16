import React, { useEffect, useMemo, useState } from "react";
import { Loader, toastService } from "@vosox/shared-ui";
import { createProperty, getProperties, updateProperty, type Property } from "../../../api/propertyApi";
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

/** Plant-first GET_MATERIAL pull with inline property/plant create + mapping. */
const SilaMaterialErpPullPanel: React.FC<SilaMaterialErpPullPanelProps> = ({ onPulled }) => {
  const [properties, setProperties] = useState<Property[]>([]);
  const [plantDrafts, setPlantDrafts] = useState<Record<string, string>>({});
  const [selectedPropertyIds, setSelectedPropertyIds] = useState<Record<string, boolean>>({});
  const [savingId, setSavingId] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [newCompany, setNewCompany] = useState("");
  const [newPlant, setNewPlant] = useState("");
  const [quickCompany, setQuickCompany] = useState("");
  const [quickPlant, setQuickPlant] = useState("");
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
        if (rows.length > 0 && !quickCompany) {
          const firstCompany = rows.find((row) => row.companyCode?.trim())?.companyCode?.trim().toUpperCase() || "";
          if (firstCompany) setQuickCompany(firstCompany);
        }
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
    return quickCompany.trim().toUpperCase();
  }, [selectedForPull, quickCompany]);

  const routeEntity = derivedCompanyCode || quickCompany.trim().toUpperCase() || "ALL";

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

  const handleCreateProperty = async (event: React.FormEvent) => {
    event.preventDefault();
    const company = newCompany.trim().toUpperCase();
    const plant = (newPlant.trim() || company).toUpperCase();
    const name = newName.trim() || `Plant ${plant}`;
    if (!company || !plant) {
      toastService.error("Enter company code and plant code.");
      return;
    }
    setCreating(true);
    try {
      await createProperty({
        companyCode: company,
        plantCode: plant,
        propertyName: name,
        masterApprovalFlowId: null,
      });
      toastService.success(`Property created for plant ${plant}.`);
      setNewName("");
      setNewPlant("");
      setQuickCompany(company);
      setQuickPlant(plant);
      setReloadKey((key) => key + 1);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not create the property.");
    } finally {
      setCreating(false);
    }
  };

  const runPull = async (plants: string[], company: string) => {
    setPulling(true);
    try {
      const pulled = await pullMaterialsFromErp(plants, company);
      setResult(pulled);
      toastService.success(`${pulled.read} record(s) read: ${pulled.new} new, ${pulled.changed} changed, ${pulled.failed} failed.`);
      if (pulled.new > 0 || pulled.changed > 0) onPulled();
      setReloadKey((key) => key + 1);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not pull the materials from the ERP.");
    } finally {
      setPulling(false);
    }
  };

  const handlePull = async () => {
    if (selectedPlantCodes.length > 0) {
      await runPull(selectedPlantCodes, derivedCompanyCode);
      return;
    }
    const plant = quickPlant.trim().toUpperCase();
    const company = quickCompany.trim().toUpperCase() || plant;
    if (!plant) {
      toastService.error("Enter a plant code (and company code), or create/select a property first.");
      return;
    }
    await runPull([plant], company);
  };

  const mappedCount = sortedProperties.filter((property) => property.plantCode?.trim()).length;
  const canPullSelected = selectedPlantCodes.length > 0;
  const canPullQuick = quickPlant.trim().length > 0 && (quickCompany.trim().length > 0 || quickPlant.trim().length > 0);

  return (
    <section className="sila-card">
      <div className="sila-card-header">
        <h2 className="sila-card-title">Pull from ERP</h2>
      </div>
      <div className="smat-erp">
        <p className="sila-help">
          Enter company and plant codes here — no Properties screen required. New plants are mapped automatically on pull.
          Pull includes stock, non-stock / non-inventory and service materials.
        </p>

        {propertiesError && (
          <div className="sila-alert sila-alert--warning" role="status">
            Could not list properties ({propertiesError}). You can still enter company and plant below and pull — the plant will be mapped on pull.
          </div>
        )}

        <div className="smat-plant-panel">
          <div className="smat-plant-panel__header">
            <div>
              <h3 className="smat-plant-panel__title">Company &amp; plant</h3>
              <p className="sila-help">Type the SAP company and plant (often the same, e.g. 1060). Pull uses these even when no property exists yet.</p>
            </div>
          </div>
          <div className="smat-quick-plant">
            <div className="sila-field">
              <label className="sila-label" htmlFor="smat-quick-company">Company code *</label>
              <input
                id="smat-quick-company"
                className="sila-input"
                maxLength={20}
                placeholder="e.g. 1060"
                value={quickCompany}
                onChange={(event) => {
                  const value = event.target.value.toUpperCase();
                  setQuickCompany(value);
                  if (!quickPlant || quickPlant === quickCompany) setQuickPlant(value);
                }}
              />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="smat-quick-plant">Plant code *</label>
              <input
                id="smat-quick-plant"
                className="sila-input"
                maxLength={20}
                placeholder="e.g. 1060"
                value={quickPlant}
                onChange={(event) => setQuickPlant(event.target.value.toUpperCase())}
              />
            </div>
            <button
              type="button"
              className="sila-btn sila-btn--secondary sila-btn--sm"
              onClick={() => setQuickPlant(quickCompany.trim().toUpperCase())}
              disabled={!quickCompany.trim()}
            >
              Use company as plant
            </button>
          </div>
        </div>

        <form className="smat-plant-panel" onSubmit={handleCreateProperty}>
          <div className="smat-plant-panel__header">
            <div>
              <h3 className="smat-plant-panel__title">Add property (optional)</h3>
              <p className="sila-help">Creates a property record with company + plant for reuse in locations and later pulls.</p>
            </div>
          </div>
          <div className="smat-quick-plant">
            <div className="sila-field">
              <label className="sila-label" htmlFor="smat-new-name">Property name</label>
              <input
                id="smat-new-name"
                className="sila-input"
                placeholder="Auto: Plant 1060"
                value={newName}
                onChange={(event) => setNewName(event.target.value)}
              />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="smat-new-company">Company code *</label>
              <input
                id="smat-new-company"
                className="sila-input"
                maxLength={20}
                value={newCompany}
                onChange={(event) => {
                  const value = event.target.value.toUpperCase();
                  setNewCompany(value);
                  if (!newPlant || newPlant === newCompany) setNewPlant(value);
                }}
              />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="smat-new-plant">Plant code *</label>
              <input
                id="smat-new-plant"
                className="sila-input"
                maxLength={20}
                value={newPlant}
                onChange={(event) => setNewPlant(event.target.value.toUpperCase())}
              />
            </div>
            <button type="submit" className="sila-btn sila-btn--secondary" disabled={creating || !newCompany.trim()}>
              {creating ? "Creating..." : "Create property"}
            </button>
          </div>
        </form>

        {sortedProperties.length > 0 && (
          <div className="smat-plant-panel">
            <div className="smat-plant-panel__header">
              <div>
                <h3 className="smat-plant-panel__title">Existing properties</h3>
                <p className="sila-help">
                  Select mapped plants to pull.
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
          </div>
        )}

        <div className="smat-plant-meta">
          <span className="sila-help">
            {canPullSelected
              ? `${selectedPlantCodes.length} plant${selectedPlantCodes.length === 1 ? "" : "s"} selected: ${selectedPlantCodes.join(", ")}`
              : canPullQuick
                ? `Ready to pull plant ${quickPlant.trim().toUpperCase()} (company ${quickCompany.trim().toUpperCase() || quickPlant.trim().toUpperCase()})`
                : "Enter company and plant above, or select a mapped property."}
          </span>
          <span className="smat-plant-meta__company">
            Company for API: <strong>{routeEntity}</strong>
          </span>
        </div>

        <div className="smat-toolbar">
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={pulling || routeLoading || (!canPullSelected && !canPullQuick) || !route?.configured}
            onClick={handlePull}
          >
            {pulling
              ? "Pulling..."
              : canPullSelected
                ? `Pull materials (${selectedPlantCodes.length} plant${selectedPlantCodes.length === 1 ? "" : "s"})`
                : canPullQuick
                  ? `Pull plant ${quickPlant.trim().toUpperCase()}`
                  : "Enter plant to pull"}
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
