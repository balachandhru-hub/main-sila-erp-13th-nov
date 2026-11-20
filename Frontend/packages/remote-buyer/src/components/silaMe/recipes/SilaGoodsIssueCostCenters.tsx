import React, { useCallback, useEffect, useMemo, useState } from "react";
import { EmptyState, Loader, Modal, toastService } from "@vosox/shared-ui";
import { getProperties, type Property } from "../../../api/propertyApi";
import { getLocations, type SilaLocation } from "../../../api/silaMe/silaInventoryApi";
import {
  deleteGoodsIssueCostCenter,
  getGoodsIssueCostCenters,
  saveGoodsIssueCostCenter,
  type GoodsIssueCostCenterScope,
  type SilaGoodsIssueCostCenter,
} from "../../../api/silaMe/silaGoodsIssueCostCenterApi";

interface SilaGoodsIssueCostCentersProps {
  canManage: boolean;
}

interface FormState {
  id?: string;
  propertyId: string;
  scope: GoodsIssueCostCenterScope;
  storageLocationCode: string;
  costCenter: string;
}

const emptyForm = (): FormState => ({
  propertyId: "",
  scope: "STORAGE",
  storageLocationCode: "",
  costCenter: "",
});

/**
 * Recipe setup — maintain Goods Issue (Z02) cost centers:
 * PLANT: Property → Plant → Cost Center
 * STORAGE: Property → Plant → Storage Location → Cost Center
 */
const SilaGoodsIssueCostCenters: React.FC<SilaGoodsIssueCostCentersProps> = ({ canManage }) => {
  const [rows, setRows] = useState<SilaGoodsIssueCostCenter[]>([]);
  const [properties, setProperties] = useState<Property[]>([]);
  const [locations, setLocations] = useState<SilaLocation[]>([]);
  const [scopeFilter, setScopeFilter] = useState<"" | GoodsIssueCostCenterScope>("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [toDelete, setToDelete] = useState<SilaGoodsIssueCostCenter | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [costCenters, props, locs] = await Promise.all([
        getGoodsIssueCostCenters(scopeFilter || undefined),
        getProperties(),
        getLocations("ACTIVE"),
      ]);
      setRows(costCenters);
      setProperties(props);
      setLocations(locs);
    } catch (err: unknown) {
      setRows([]);
      setError(err instanceof Error ? err.message : "Could not load Goods Issue cost centers.");
    } finally {
      setLoading(false);
    }
  }, [scopeFilter]);

  useEffect(() => {
    load();
  }, [load]);

  const selectedProperty = useMemo(
    () => properties.find((item) => item.id === form?.propertyId) ?? null,
    [properties, form?.propertyId],
  );

  const storageOptions = useMemo(() => {
    if (!form?.propertyId) return [];
    const codes = new Set<string>();
    locations
      .filter((location) => location.propertyId === form.propertyId)
      .forEach((location) => {
        const storage = (location.storageLocationCode || location.locationCode || "").trim().toUpperCase();
        if (storage) codes.add(storage);
      });
    return Array.from(codes).sort();
  }, [locations, form?.propertyId]);

  const validate = (value: FormState): string | null => {
    if (!value.propertyId) return "Select a property (plant).";
    if (!value.costCenter.trim() || value.costCenter.trim().length > 50) {
      return "Enter a SAP cost center of up to 50 characters (e.g. 1060050124).";
    }
    if (value.scope === "STORAGE" && !value.storageLocationCode.trim()) {
      return "Enter or select the storage location / outlet (e.g. FL10).";
    }
    return null;
  };

  const handleSave = async () => {
    if (!form) return;
    const problem = validate(form);
    if (problem) {
      toastService.error(problem);
      return;
    }
    setBusy(true);
    try {
      await saveGoodsIssueCostCenter({
        propertyId: form.propertyId,
        scope: form.scope,
        storageLocationCode: form.scope === "STORAGE" ? form.storageLocationCode.trim().toUpperCase() : null,
        costCenter: form.costCenter.trim(),
      }, form.id);
      toastService.success(form.id ? "Cost center mapping updated." : "Cost center mapping saved.");
      setForm(null);
      await load();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not save.");
    } finally {
      setBusy(false);
    }
  };

  const handleDelete = async () => {
    if (!toDelete) return;
    setBusy(true);
    try {
      await deleteGoodsIssueCostCenter(toDelete.id);
      toastService.success("Cost center mapping deleted.");
      setToDelete(null);
      await load();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not delete.");
    } finally {
      setBusy(false);
    }
  };

  const openCreate = (scope: GoodsIssueCostCenterScope = "STORAGE") => {
    const property1060 = properties.find((item) => item.plantCode?.toUpperCase() === "1060");
    setForm({
      ...emptyForm(),
      scope,
      propertyId: property1060?.id ?? "",
      storageLocationCode: scope === "STORAGE" ? "FL10" : "",
      costCenter: "1060050124",
    });
  };

  return (
    <div className="srec-gi-cc" data-testid="goods-issue-cost-centers">
      <div className="srec-gi-cc__intro">
        <p>
          Maintain SAP cost centers used for Goods Issue (Z02) recipe / POS consumption.
          Storage-level mappings override plant-level. Example: Property <strong>1060</strong>, Plant{" "}
          <strong>1060</strong>, Storage <strong>FL10</strong> → Cost center <strong>1060050124</strong>.
        </p>
      </div>

      <div className="srec-filters srec-filters--flush">
        <div className="sila-field">
          <label className="sila-label" htmlFor="gi-cc-scope">Scope</label>
          <select
            id="gi-cc-scope"
            className="sila-select"
            value={scopeFilter}
            onChange={(e) => setScopeFilter(e.target.value as "" | GoodsIssueCostCenterScope)}
          >
            <option value="">All</option>
            <option value="PLANT">Plant (Property → Plant → Cost center)</option>
            <option value="STORAGE">Storage (Property → Plant → Storage → Cost center)</option>
          </select>
        </div>
        {canManage && (
          <div className="srec-gi-cc__actions">
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => openCreate("PLANT")}>
              Add plant cost center
            </button>
            <button type="button" className="sila-btn sila-btn--primary" onClick={() => openCreate("STORAGE")}>
              Add storage cost center
            </button>
          </div>
        )}
      </div>

      {loading && <Loader />}
      {!loading && error && (
        <EmptyState variant="error" title="Could not load cost centers" description={error} />
      )}
      {!loading && !error && rows.length === 0 && (
        <EmptyState
          title="No Goods Issue cost centers"
          description="Add a plant-level or storage-level mapping for Z02 postings."
        />
      )}
      {!loading && !error && rows.length > 0 && (
        <div className="sila-table-wrap">
          <table className="sila-table">
            <thead>
              <tr>
                <th>Scope</th>
                <th>Property</th>
                <th>Plant</th>
                <th>Storage location</th>
                <th>Cost center</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.id}>
                  <td>{row.scope}</td>
                  <td>{row.propertyName || row.companyCode || "—"}</td>
                  <td>{row.plantCode}</td>
                  <td>{row.scope === "STORAGE" ? (row.storageLocationCode || "—") : "—"}</td>
                  <td><strong>{row.costCenter}</strong></td>
                  <td className="sila-table__actions">
                    {canManage && (
                      <>
                        <button
                          type="button"
                          className="sila-btn sila-btn--secondary sila-btn--sm"
                          onClick={() => setForm({
                            id: row.id,
                            propertyId: row.propertyId,
                            scope: (row.scope === "PLANT" ? "PLANT" : "STORAGE"),
                            storageLocationCode: row.storageLocationCode || "",
                            costCenter: row.costCenter,
                          })}
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          className="sila-btn sila-btn--danger sila-btn--sm"
                          onClick={() => setToDelete(row)}
                        >
                          Delete
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Modal
        open={Boolean(form)}
        onClose={() => !busy && setForm(null)}
        headerProps={{ heading: form?.id ? "Edit Goods Issue cost center" : "Add Goods Issue cost center" }}
      >
        {form && (
          <div className="sila-form-grid">
            <div className="sila-field">
              <label className="sila-label" htmlFor="gi-form-scope">Scope</label>
              <select
                id="gi-form-scope"
                className="sila-select"
                value={form.scope}
                disabled={busy}
                onChange={(e) => setForm({
                  ...form,
                  scope: e.target.value as GoodsIssueCostCenterScope,
                  storageLocationCode: e.target.value === "PLANT" ? "" : form.storageLocationCode,
                })}
              >
                <option value="PLANT">PLANT — Property → Plant → Cost center</option>
                <option value="STORAGE">STORAGE — Property → Plant → Storage → Cost center</option>
              </select>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="gi-form-property">Property / Plant</label>
              <select
                id="gi-form-property"
                className="sila-select"
                value={form.propertyId}
                disabled={busy}
                onChange={(e) => setForm({ ...form, propertyId: e.target.value, storageLocationCode: "" })}
              >
                <option value="">Select property</option>
                {properties.map((property) => (
                  <option key={property.id} value={property.id}>
                    {property.propertyName} · Plant {property.plantCode} · CoCd {property.companyCode}
                  </option>
                ))}
              </select>
              {selectedProperty && (
                <span className="sila-hint">Plant code: {selectedProperty.plantCode}</span>
              )}
            </div>
            {form.scope === "STORAGE" && (
              <div className="sila-field">
                <label className="sila-label" htmlFor="gi-form-sloc">Storage location / outlet</label>
                <input
                  id="gi-form-sloc"
                  className="sila-input"
                  list="gi-form-sloc-list"
                  maxLength={50}
                  value={form.storageLocationCode}
                  disabled={busy}
                  placeholder="e.g. FL10"
                  onChange={(e) => setForm({ ...form, storageLocationCode: e.target.value.toUpperCase() })}
                />
                <datalist id="gi-form-sloc-list">
                  {storageOptions.map((code) => (
                    <option key={code} value={code} />
                  ))}
                </datalist>
              </div>
            )}
            <div className="sila-field">
              <label className="sila-label" htmlFor="gi-form-cc">Cost center</label>
              <input
                id="gi-form-cc"
                className="sila-input"
                maxLength={50}
                value={form.costCenter}
                disabled={busy}
                placeholder="e.g. 1060050124"
                onChange={(e) => setForm({ ...form, costCenter: e.target.value })}
              />
            </div>
            <div className="srec-gi-cc__form-actions">
              <button type="button" className="sila-btn sila-btn--secondary" disabled={busy} onClick={() => setForm(null)}>
                Cancel
              </button>
              <button type="button" className="sila-btn sila-btn--primary" disabled={busy} onClick={handleSave}>
                {busy ? "Saving…" : "Save"}
              </button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        open={Boolean(toDelete)}
        onClose={() => !busy && setToDelete(null)}
        headerProps={{ heading: "Delete cost center mapping" }}
      >
        <p>Remove mapping {toDelete?.plantCode}
          {toDelete?.scope === "STORAGE" ? ` / ${toDelete.storageLocationCode}` : ""}
          {" → "}{toDelete?.costCenter}?
        </p>
        <div className="srec-gi-cc__form-actions">
          <button type="button" className="sila-btn sila-btn--secondary" disabled={busy} onClick={() => setToDelete(null)}>
            Cancel
          </button>
          <button type="button" className="sila-btn sila-btn--danger" disabled={busy} onClick={handleDelete}>
            {busy ? "Deleting…" : "Delete"}
          </button>
        </div>
      </Modal>
    </div>
  );
};

export default SilaGoodsIssueCostCenters;
