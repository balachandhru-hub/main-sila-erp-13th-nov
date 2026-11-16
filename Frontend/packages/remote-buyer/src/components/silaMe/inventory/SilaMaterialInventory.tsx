import React, { useCallback, useEffect, useMemo, useState } from "react";
import { EmptyState, Loader, PageHeader } from "@vosox/shared-ui";
import { getProperties, type Property } from "../../../api/propertyApi";
import { getMaterialsPage, SILA_PRICE_STATUSES, type SilaMaterial } from "../../../api/silaMe/silaInventoryApi";
import SilaMaterialDialog from "./SilaMaterialDialog";
import SilaMaterialCatalogGrid from "./SilaMaterialCatalogGrid";
import SilaMaterialPricePanel from "../materials/SilaMaterialPricePanel";
import SilaMaterialExcelPanel from "../materials/SilaMaterialExcelPanel";
import SilaMaterialErpPullPanel from "../materials/SilaMaterialErpPullPanel";
import SilaMaterialDetailPanel from "../materials/SilaMaterialDetailPanel";
import SilaPriceStatusBadge from "../materials/SilaPriceStatusBadge";
import { codeLabel, formatPrice, priceStatusLabel } from "../materials/materialFormat";
import "../silaMeTheme.css";
import "./SilaInventory.css";

interface SilaMaterialInventoryProps {
  /** Edit inventory fields and UOM conversions (MANAGE_SILA_LOCATION). */
  canManage: boolean;
  /** Request price changes, import the material Excel file and pull materials from the ERP (MANAGE_SILA_MASTER_DATA). */
  canManageMasterData?: boolean;
}

type InventoryViewMode = "catalog" | "list";

const PAGE_SIZE = 25;
const VIEW_PREF_KEY = "sila.itemMaster.viewMode";

const loadViewMode = (): InventoryViewMode => {
  try {
    return localStorage.getItem(VIEW_PREF_KEY) === "list" ? "list" : "catalog";
  } catch {
    return "catalog";
  }
};

const saveViewMode = (mode: InventoryViewMode) => {
  try {
    localStorage.setItem(VIEW_PREF_KEY, mode);
  } catch {
    /* ignore */
  }
};

const plantOptions = (properties: Property[]): { code: string; label: string }[] => {
  const byCode = new Map<string, string>();
  for (const property of properties) {
    const code = property.plantCode?.trim().toUpperCase();
    if (!code) continue;
    if (!byCode.has(code)) byCode.set(code, property.propertyName?.trim() || code);
  }
  return Array.from(byCode.entries())
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([code, name]) => ({ code, label: name === code ? code : `${code} · ${name}` }));
};

/** Item Master materials with catalog (grid) and list views. */
const SilaMaterialInventory: React.FC<SilaMaterialInventoryProps> = ({ canManage, canManageMasterData = false }) => {
  const [search, setSearch] = useState("");
  const [inventoryOnly, setInventoryOnly] = useState(false);
  const [priceStatus, setPriceStatus] = useState("");
  const [plantCode, setPlantCode] = useState("");
  const [properties, setProperties] = useState<Property[]>([]);
  const [viewMode, setViewMode] = useState<InventoryViewMode>(() => loadViewMode());
  const [pricing, setPricing] = useState<SilaMaterial | null>(null);
  const [page, setPage] = useState(0);
  const [materials, setMaterials] = useState<SilaMaterial[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [editing, setEditing] = useState<SilaMaterial | null>(null);
  const [detailId, setDetailId] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    getProperties()
      .then((rows) => {
        if (active) setProperties(rows);
      })
      .catch(() => {
        if (active) setProperties([]);
      });
    return () => {
      active = false;
    };
  }, []);

  const plants = useMemo(() => plantOptions(properties), [properties]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    const timer = window.setTimeout(async () => {
      try {
        const rows = await getMaterialsPage(search, page * PAGE_SIZE, PAGE_SIZE, inventoryOnly, priceStatus, plantCode);
        if (active) setMaterials(rows);
      } catch (err: unknown) {
        if (active) setError(err instanceof Error ? err.message : "Could not load the materials.");
      } finally {
        if (active) setLoading(false);
      }
    }, 300);
    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [search, inventoryOnly, priceStatus, plantCode, page, reloadKey]);

  const setMode = useCallback((mode: InventoryViewMode) => {
    setViewMode(mode);
    saveViewMode(mode);
  }, []);

  const reload = useCallback(() => setReloadKey((key) => key + 1), []);
  const closeDialog = useCallback(() => setEditing(null), []);
  const afterSave = useCallback(() => {
    setEditing(null);
    reload();
  }, [reload]);
  const closePricing = useCallback(() => setPricing(null), []);
  const afterPriceSubmit = useCallback(() => {
    setPricing(null);
    reload();
  }, [reload]);

  return (
    <div className="sila-me sinv-page">
      <PageHeader
        className="pud-page-header"
        title="Material Master"
        description="Item Master materials with their inventory controls, plant valuation, approved unit price and UOM conversions. Browse as a catalog grid or dense list."
      />

      <section className="sila-card">
        <div className="sila-card-header">
          <h2 className="sila-card-title">Item Master inventory</h2>
          <div className="sinv-header-actions">
            <div className="sinv-view-toggle" role="group" aria-label="Item Master view">
              <button
                type="button"
                className={`sila-btn sila-btn--sm ${viewMode === "catalog" ? "sila-btn--primary" : "sila-btn--secondary"}`}
                aria-pressed={viewMode === "catalog"}
                onClick={() => setMode("catalog")}
              >
                Catalog
              </button>
              <button
                type="button"
                className={`sila-btn sila-btn--sm ${viewMode === "list" ? "sila-btn--primary" : "sila-btn--secondary"}`}
                aria-pressed={viewMode === "list"}
                onClick={() => setMode("list")}
              >
                List
              </button>
            </div>
            <SilaMaterialExcelPanel
              canImport={canManageMasterData}
              search={search}
              priceStatus={priceStatus}
              inventoryOnly={inventoryOnly}
              onImported={reload}
            />
          </div>
        </div>
        <div className="sinv-filters">
          <div className="sila-field sinv-grow">
            <label className="sila-label" htmlFor="sinv-mat-search">Search description, code or barcode</label>
            <input
              id="sinv-mat-search"
              className="sila-input"
              type="search"
              placeholder="Description, material code or barcode"
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(0);
              }}
            />
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="sinv-mat-plant">Plant (valuation)</label>
            <select
              id="sinv-mat-plant"
              className="sila-input"
              value={plantCode}
              onChange={(event) => {
                setPlantCode(event.target.value);
                setPage(0);
              }}
            >
              <option value="">All plants</option>
              {plants.map((plant) => (
                <option key={plant.code} value={plant.code}>{plant.label}</option>
              ))}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="sinv-mat-price-status">Price status</label>
            <select
              id="sinv-mat-price-status"
              className="sila-input"
              value={priceStatus}
              onChange={(event) => {
                setPriceStatus(event.target.value);
                setPage(0);
              }}
            >
              <option value="">All</option>
              {SILA_PRICE_STATUSES.map((status) => <option key={status} value={status}>{priceStatusLabel(status)}</option>)}
            </select>
          </div>
          <label className="sila-choice">
            <input
              type="checkbox"
              checked={inventoryOnly}
              onChange={(event) => {
                setInventoryOnly(event.target.checked);
                setPage(0);
              }}
            />
            Inventory items only
          </label>
        </div>
        {plantCode && (
          <p className="sila-help sinv-filter-note">
            Showing materials assigned to plant <strong>{plantCode}</strong>. Valuation area follows the selected plant.
          </p>
        )}
        {loading ? (
          <Loader size={24} message="Loading materials..." />
        ) : error ? (
          <EmptyState
            variant="error"
            title="Couldn't load the materials"
            description={error}
            action={<button type="button" className="sila-btn sila-btn--secondary" onClick={reload}>Try again</button>}
          />
        ) : materials.length === 0 ? (
          <EmptyState title={page > 0 ? "No more materials" : "No materials found"} />
        ) : viewMode === "catalog" ? (
          <div className="sinv-catalog-wrap">
            <SilaMaterialCatalogGrid
              materials={materials}
              plantCode={plantCode}
              canManage={canManage}
              canManageMasterData={canManageMasterData}
              onDetail={setDetailId}
              onPrice={setPricing}
              onEdit={setEditing}
            />
          </div>
        ) : (
          <div className="sila-table-wrap">
            <table className="sila-table">
              <thead>
                <tr>
                  <th scope="col">Code</th>
                  <th scope="col">Description</th>
                  <th scope="col">Plants</th>
                  <th scope="col">Material group</th>
                  <th scope="col">Base UOM</th>
                  <th scope="col" className="sinv-num">Unit price</th>
                  <th scope="col">Price status</th>
                  <th scope="col">Barcode</th>
                  <th scope="col">Inventory</th>
                  <th scope="col">Conversions</th>
                  <th scope="col">Valuation</th>
                  <th scope="col">Source</th>
                  <th scope="col"><span className="sila-visually-hidden">Actions</span></th>
                </tr>
              </thead>
              <tbody>
                {materials.map((material) => {
                  const plantsList = material.plantCodes ?? [];
                  const valuationPlant = plantCode || (plantsList.length === 1 ? plantsList[0] : null);
                  return (
                    <tr key={material.id}>
                      <td>
                        <span className="sila-cell-strong">{material.materialCode}</span>
                      </td>
                      <td>
                        <span className="sila-cell-strong">{material.description || "—"}</span>
                      </td>
                      <td>
                        {plantsList.length === 0
                          ? "—"
                          : plantsList.map((code, index) => (
                              <span key={code} className="sinv-sub">
                                {code}
                                {material.plantPropertyNames?.[index] && material.plantPropertyNames[index] !== code
                                  ? ` · ${material.plantPropertyNames[index]}`
                                  : ""}
                              </span>
                            ))}
                      </td>
                      <td>{material.materialGroup || "—"}</td>
                      <td>{material.baseUom}</td>
                      <td className="sinv-num">
                        {material.unitCost === null || material.unitCost === undefined
                          ? <span className="sila-me-flag sila-me-flag--bad">Price missing</span>
                          : formatPrice(material.unitCost, material.currency, material.approvedPriceUom || material.baseUom)}
                        {material.proposedUnitCost !== null && material.proposedUnitCost !== undefined && (
                          <span className="sinv-sub">
                            Proposed {formatPrice(material.proposedUnitCost, material.currency, material.proposedPriceUom || material.baseUom)}
                          </span>
                        )}
                      </td>
                      <td><SilaPriceStatusBadge status={material.priceStatus} /></td>
                      <td>{material.barcode || "—"}</td>
                      <td>
                        {material.isInventoryItem
                          ? <span className="sila-badge sila-badge--success">{codeLabel(material.inventoryType || "STOCK")}</span>
                          : <span className="sila-badge sila-badge--neutral">{material.inventoryType ? codeLabel(material.inventoryType) : "Non-inventory"}</span>}
                        {(material.batchManaged || material.expiryManaged || material.serialManaged) && (
                          <span className="sinv-sub">
                            {[material.batchManaged && "Batch", material.expiryManaged && "Expiry", material.serialManaged && "Serial"]
                              .filter(Boolean)
                              .join(" · ")}
                          </span>
                        )}
                        {material.expiryManaged && <span className="sinv-sub">Shelf life {material.shelfLifeDays ?? "—"} days</span>}
                      </td>
                      <td>
                        {material.conversions.length === 0
                          ? "—"
                          : material.conversions.map((conversion) => (
                              <span key={`${conversion.fromUom}-${conversion.toUom}`} className="sinv-sub">
                                1 {conversion.fromUom} = {conversion.factor} {conversion.toUom}
                              </span>
                            ))}
                      </td>
                      <td>
                        {material.priceControl ? (material.priceControl === "S" ? "Standard price" : "Moving average") : "—"}
                        {(material.valuationClass || material.materialType) && (
                          <span className="sinv-sub">{[material.valuationClass, material.materialType].filter(Boolean).join(" · ")}</span>
                        )}
                        {valuationPlant
                          ? <span className="sinv-sub">Valuation area {valuationPlant}</span>
                          : plantsList.length > 1
                            ? <span className="sinv-sub">Valuation by plant — filter to one plant</span>
                            : null}
                        {material.companyCode && <span className="sinv-sub">Company {material.companyCode}</span>}
                      </td>
                      <td>
                        {material.source === "ERP" ? "ERP" : "Item Master"}
                        {material.updatedAt && <span className="sinv-sub">Updated {new Date(material.updatedAt).toLocaleDateString()}</span>}
                      </td>
                      <td className="sila-cell-actions">
                        <div className="sinv-inline">
                          <button
                            type="button"
                            className="sila-btn sila-btn--primary sila-btn--sm"
                            aria-label={`Open ${material.materialCode} detail`}
                            onClick={() => setDetailId(material.id)}
                          >
                            Detail
                          </button>
                          <button
                            type="button"
                            className="sila-btn sila-btn--secondary sila-btn--sm"
                            aria-label={`Unit price of ${material.materialCode}`}
                            onClick={() => setPricing(material)}
                          >
                            {canManageMasterData ? "Update unit price" : "Price"}
                          </button>
                          {canManage && (
                            <button
                              type="button"
                              className="sila-btn sila-btn--secondary sila-btn--sm"
                              aria-label={`Edit ${material.materialCode}`}
                              onClick={() => setEditing(material)}
                            >
                              Edit
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
        {!error && (page > 0 || materials.length === PAGE_SIZE) && (
          <div className="sinv-pager">
            <span>Page {page + 1}</span>
            <div className="sinv-inline">
              <button
                type="button"
                className="sila-btn sila-btn--secondary sila-btn--sm"
                disabled={page === 0 || loading}
                onClick={() => setPage((current) => Math.max(0, current - 1))}
              >
                Previous
              </button>
              <button
                type="button"
                className="sila-btn sila-btn--secondary sila-btn--sm"
                disabled={materials.length < PAGE_SIZE || loading}
                onClick={() => setPage((current) => current + 1)}
              >
                Next
              </button>
            </div>
          </div>
        )}
      </section>

      {canManageMasterData && <SilaMaterialErpPullPanel onPulled={reload} />}

      {editing && <SilaMaterialDialog material={editing} onClose={closeDialog} onSaved={afterSave} />}
      {detailId && <SilaMaterialDetailPanel materialId={detailId} onClose={() => setDetailId(null)} />}
      {pricing && (
        <SilaMaterialPricePanel
          material={pricing}
          canRequest={canManageMasterData}
          onClose={closePricing}
          onSubmitted={afterPriceSubmit}
        />
      )}
    </div>
  );
};

export default SilaMaterialInventory;
