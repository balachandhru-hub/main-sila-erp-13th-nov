import React, { useCallback, useState } from "react";
import { EmptyState, Loader, Modal, PageHeader, toastService } from "@vosox/shared-ui";
import {
  deactivatePurchasingProfile,
  listPurchasingProfiles,
  setDefaultPurchasingProfile,
  upsertPurchasingProfile,
  type SilaUserPurchasingProfile,
  type SilaUserPurchasingProfileWrite,
} from "../../../api/silaMe/silaVoicePoApi";
import "../silaMeTheme.css";

interface SilaUserPoDefaultsProps {
  /** MANAGE_SILA_MASTER_DATA: form is read-only without it. */
  canManage?: boolean;
}

interface FormState extends SilaUserPurchasingProfileWrite {
  id?: string;
}

/** SILA Admin default purchasing / account-assignment seed for new profiles. */
const SILA_ADMIN_PO_DEFAULTS = {
  companyCode: "1060",
  plantCode: "1060",
  storageLocation: "FL01",
  purchasingOrganization: "1000",
  purchasingGroup: "001",
  accountAssignmentCategory: "K",
  costCenter: "1060010108",
  glAccount: "3250000109",
  materialGroup: "0127",
  defaultPoDocumentType: "NB",
} as const;

const emptyForm = (userId: string): FormState => ({
  userId,
  isDefault: true,
  companyCode: SILA_ADMIN_PO_DEFAULTS.companyCode,
  plantCode: SILA_ADMIN_PO_DEFAULTS.plantCode,
  storageLocation: SILA_ADMIN_PO_DEFAULTS.storageLocation,
  purchasingOrganization: SILA_ADMIN_PO_DEFAULTS.purchasingOrganization,
  purchasingGroup: SILA_ADMIN_PO_DEFAULTS.purchasingGroup,
  accountAssignmentCategory: SILA_ADMIN_PO_DEFAULTS.accountAssignmentCategory,
  costCenter: SILA_ADMIN_PO_DEFAULTS.costCenter,
  glAccount: SILA_ADMIN_PO_DEFAULTS.glAccount,
  materialGroup: SILA_ADMIN_PO_DEFAULTS.materialGroup,
  profitCenter: "",
  preferredSupplierCode: "",
  defaultCurrency: "",
  defaultPoDocumentType: SILA_ADMIN_PO_DEFAULTS.defaultPoDocumentType,
  notes: "",
});

const trimOrNull = (value?: string | null): string | null => {
  const text = (value ?? "").trim();
  return text ? text : null;
};

/**
 * Compact Setup admin: PO Defaults & Account Assignment per user.
 * Style matches other SILA setup tables (sila-root / sila-table / sila-field).
 */
const SilaUserPoDefaults: React.FC<SilaUserPoDefaultsProps> = ({ canManage = true }) => {
  const [userId, setUserId] = useState("");
  const [loadedUserId, setLoadedUserId] = useState("");
  const [rows, setRows] = useState<SilaUserPurchasingProfile[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<FormState | null>(null);
  const [busy, setBusy] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async (id: string) => {
    const trimmed = id.trim();
    if (!trimmed) {
      toastService.error("Enter a User ID to load profiles.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const data = await listPurchasingProfiles(trimmed);
      setRows(data);
      setLoadedUserId(trimmed);
    } catch (err: unknown) {
      setRows([]);
      setLoadedUserId("");
      setError(err instanceof Error ? err.message : "Could not load purchasing profiles.");
    } finally {
      setLoading(false);
    }
  }, []);

  const openCreate = () => {
    if (!loadedUserId) {
      toastService.error("Load a user first.");
      return;
    }
    setForm(emptyForm(loadedUserId));
  };

  const openEdit = (row: SilaUserPurchasingProfile) => {
    setForm({
      id: row.id,
      userId: row.userId,
      username: row.username ?? "",
      displayName: row.displayName ?? "",
      isDefault: row.isDefault,
      propertyId: row.propertyId ?? "",
      outletId: row.outletId ?? "",
      locationId: row.locationId ?? "",
      companyCode: row.companyCode ?? "",
      plantCode: row.plantCode ?? "",
      storageLocation: row.storageLocation ?? "",
      purchasingOrganization: row.purchasingOrganization ?? "",
      purchasingGroup: row.purchasingGroup ?? "",
      defaultDeliveryLocation: row.defaultDeliveryLocation ?? "",
      accountAssignmentCategory: row.accountAssignmentCategory ?? "",
      costCenter: row.costCenter ?? "",
      glAccount: row.glAccount ?? "",
      materialGroup: row.materialGroup ?? "",
      profitCenter: row.profitCenter ?? "",
      businessArea: row.businessArea ?? "",
      internalOrder: row.internalOrder ?? "",
      wbsElement: row.wbsElement ?? "",
      preferredSupplierId: row.preferredSupplierId ?? "",
      preferredSupplierCode: row.preferredSupplierCode ?? "",
      defaultCurrency: row.defaultCurrency ?? "",
      defaultDeliveryLeadTimeDays: row.defaultDeliveryLeadTimeDays ?? null,
      defaultPoDocumentType: row.defaultPoDocumentType ?? "",
      defaultItemCategory: row.defaultItemCategory ?? "",
      notes: row.notes ?? "",
    });
  };

  const handleSave = async () => {
    if (!form) return;
    if (!form.userId.trim()) {
      toastService.error("User ID is required.");
      return;
    }
    if (!trimOrNull(form.companyCode) && !trimOrNull(form.plantCode)) {
      toastService.error("Enter at least a company code or plant code.");
      return;
    }
    setBusy(true);
    try {
      const payload: SilaUserPurchasingProfileWrite = {
        userId: form.userId.trim(),
        username: trimOrNull(form.username),
        displayName: trimOrNull(form.displayName),
        isDefault: Boolean(form.isDefault),
        propertyId: trimOrNull(form.propertyId),
        outletId: trimOrNull(form.outletId),
        locationId: trimOrNull(form.locationId),
        companyCode: trimOrNull(form.companyCode),
        plantCode: trimOrNull(form.plantCode),
        storageLocation: trimOrNull(form.storageLocation),
        purchasingOrganization: trimOrNull(form.purchasingOrganization),
        purchasingGroup: trimOrNull(form.purchasingGroup),
        defaultDeliveryLocation: trimOrNull(form.defaultDeliveryLocation),
        accountAssignmentCategory: trimOrNull(form.accountAssignmentCategory),
        costCenter: trimOrNull(form.costCenter),
        glAccount: trimOrNull(form.glAccount),
        materialGroup: trimOrNull(form.materialGroup),
        profitCenter: trimOrNull(form.profitCenter),
        businessArea: trimOrNull(form.businessArea),
        internalOrder: trimOrNull(form.internalOrder),
        wbsElement: trimOrNull(form.wbsElement),
        preferredSupplierId: trimOrNull(form.preferredSupplierId),
        preferredSupplierCode: trimOrNull(form.preferredSupplierCode),
        defaultCurrency: trimOrNull(form.defaultCurrency),
        defaultDeliveryLeadTimeDays: form.defaultDeliveryLeadTimeDays ?? null,
        defaultPoDocumentType: trimOrNull(form.defaultPoDocumentType),
        defaultItemCategory: trimOrNull(form.defaultItemCategory),
        notes: trimOrNull(form.notes),
      };
      await upsertPurchasingProfile(payload, form.id);
      toastService.success(form.id ? "Purchasing profile updated." : "Purchasing profile saved.");
      setForm(null);
      await load(form.userId);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not save the profile.");
    } finally {
      setBusy(false);
    }
  };

  const handleSetDefault = async (row: SilaUserPurchasingProfile) => {
    setBusyId(row.id);
    try {
      await setDefaultPurchasingProfile(row.id);
      toastService.success("Default purchasing profile set.");
      await load(loadedUserId);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not set default.");
    } finally {
      setBusyId(null);
    }
  };

  const handleDeactivate = async (row: SilaUserPurchasingProfile) => {
    setBusyId(row.id);
    try {
      await deactivatePurchasingProfile(row.id);
      toastService.success("Purchasing profile deactivated.");
      await load(loadedUserId);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not deactivate.");
    } finally {
      setBusyId(null);
    }
  };

  const setField = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    if (!form) return;
    setForm({ ...form, [key]: value });
  };

  return (
    <div className="sila-root sila-me" data-testid="sila-user-po-defaults">
      <PageHeader
        className="pud-page-header"
        title="PO Defaults & Account Assignment"
        description="Authorized purchasing and account-assignment defaults per user for Voice PO and manual PO creation. New profiles seed SILA Admin defaults (1060 / FL01 / K)."
      />

      <section className="sila-card">
        <div className="sila-card-body">
          <div className="sila-field">
            <label className="sila-label" htmlFor="spo-user-id">User ID</label>
            <div style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
              <input
                id="spo-user-id"
                className="sila-input"
                type="text"
                placeholder="GUID of the user"
                value={userId}
                onChange={(event) => setUserId(event.target.value)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    void load(userId);
                  }
                }}
              />
              <button type="button" className="sila-btn sila-btn--secondary" disabled={loading} onClick={() => void load(userId)}>
                {loading ? "Loading…" : "Load"}
              </button>
              {canManage && (
                <button type="button" className="sila-btn sila-btn--primary" disabled={!loadedUserId || loading} onClick={openCreate}>
                  Add profile
                </button>
              )}
            </div>
            <span className="sila-help">Load by User ID, then add or edit authorized company / plant / SLoc / purchasing org / account assignment.</span>
          </div>
        </div>

        {loading && <Loader size={24} message="Loading purchasing profiles…" />}
        {!loading && error && (
          <EmptyState
            variant="error"
            title="Could not load profiles"
            description={error}
            action={<button type="button" className="sila-btn sila-btn--secondary" onClick={() => void load(userId)}>Try again</button>}
          />
        )}
        {!loading && !error && loadedUserId && rows.length === 0 && (
          <EmptyState
            title="No purchasing profiles"
            description="Add a default profile for this user so Voice PO can resolve company, plant and account assignment."
          />
        )}
        {!loading && !error && rows.length > 0 && (
          <div className="sila-table-wrap">
            <table className="sila-table">
              <thead>
                <tr>
                  <th scope="col">Default</th>
                  <th scope="col">Status</th>
                  <th scope="col">Company</th>
                  <th scope="col">Plant</th>
                  <th scope="col">SLoc</th>
                  <th scope="col">POrg</th>
                  <th scope="col">PGrp</th>
                  <th scope="col">Acct</th>
                  <th scope="col">Cost center</th>
                  <th scope="col">G/L</th>
                  <th scope="col">Mat grp</th>
                  <th scope="col">Supplier</th>
                  {canManage && <th scope="col"><span className="sila-visually-hidden">Actions</span></th>}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>{row.isDefault ? "Yes" : "—"}</td>
                    <td>{row.status}</td>
                    <td className="sila-cell-strong">{row.companyCode || "—"}</td>
                    <td>{row.plantCode || "—"}</td>
                    <td>{row.storageLocation || "—"}</td>
                    <td>{row.purchasingOrganization || "—"}</td>
                    <td>{row.purchasingGroup || "—"}</td>
                    <td>{row.accountAssignmentCategory || "—"}</td>
                    <td>{row.costCenter || "—"}</td>
                    <td>{row.glAccount || "—"}</td>
                    <td>{row.materialGroup || "—"}</td>
                    <td>{row.preferredSupplierCode || "—"}</td>
                    {canManage && (
                      <td>
                        <div className="srcv-actions" style={{ display: "flex", gap: "0.35rem", flexWrap: "wrap" }}>
                          <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => openEdit(row)}>
                            Edit
                          </button>
                          {!row.isDefault && row.status === "ACTIVE" && (
                            <button
                              type="button"
                              className="sila-btn sila-btn--secondary sila-btn--sm"
                              disabled={busyId === row.id}
                              onClick={() => void handleSetDefault(row)}
                            >
                              Set default
                            </button>
                          )}
                          {row.status === "ACTIVE" && (
                            <button
                              type="button"
                              className="sila-btn sila-btn--danger sila-btn--sm"
                              disabled={busyId === row.id}
                              onClick={() => void handleDeactivate(row)}
                            >
                              Deactivate
                            </button>
                          )}
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <Modal
        open={Boolean(form)}
        onClose={() => !busy && setForm(null)}
        headerProps={{ heading: form?.id ? "Edit purchasing profile" : "Add purchasing profile" }}
      >
        {form && (
          <div className="sila-root sila-form-grid">
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-user">User ID</label>
              <input id="spo-form-user" className="sila-input" value={form.userId} disabled readOnly />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-display">Display name</label>
              <input
                id="spo-form-display"
                className="sila-input"
                value={form.displayName ?? ""}
                disabled={busy}
                onChange={(event) => setField("displayName", event.target.value)}
              />
            </div>
            <div className="sila-field">
              <label className="sila-choice" htmlFor="spo-form-default">
                <input
                  id="spo-form-default"
                  type="checkbox"
                  checked={Boolean(form.isDefault)}
                  disabled={busy}
                  onChange={(event) => setField("isDefault", event.target.checked)}
                />
                Default profile
              </label>
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-cc">Company code</label>
              <input id="spo-form-cc" className="sila-input" value={form.companyCode ?? ""} disabled={busy}
                onChange={(event) => setField("companyCode", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-plant">Plant</label>
              <input id="spo-form-plant" className="sila-input" value={form.plantCode ?? ""} disabled={busy}
                onChange={(event) => setField("plantCode", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-sloc">Storage location</label>
              <input id="spo-form-sloc" className="sila-input" value={form.storageLocation ?? ""} disabled={busy}
                onChange={(event) => setField("storageLocation", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-porg">Purchasing organization</label>
              <input id="spo-form-porg" className="sila-input" value={form.purchasingOrganization ?? ""} disabled={busy}
                onChange={(event) => setField("purchasingOrganization", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-pgrp">Purchasing group</label>
              <input id="spo-form-pgrp" className="sila-input" value={form.purchasingGroup ?? ""} disabled={busy}
                onChange={(event) => setField("purchasingGroup", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-acct">Account assignment category</label>
              <input id="spo-form-acct" className="sila-input" value={form.accountAssignmentCategory ?? ""} disabled={busy}
                placeholder="e.g. K, U"
                onChange={(event) => setField("accountAssignmentCategory", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-cost">Cost center</label>
              <input id="spo-form-cost" className="sila-input" value={form.costCenter ?? ""} disabled={busy}
                onChange={(event) => setField("costCenter", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-gl">G/L account</label>
              <input id="spo-form-gl" className="sila-input" value={form.glAccount ?? ""} disabled={busy}
                onChange={(event) => setField("glAccount", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-matgrp">Material group</label>
              <input id="spo-form-matgrp" className="sila-input" value={form.materialGroup ?? ""} disabled={busy}
                placeholder="e.g. 0127"
                onChange={(event) => setField("materialGroup", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-pc">Profit center</label>
              <input id="spo-form-pc" className="sila-input" value={form.profitCenter ?? ""} disabled={busy}
                onChange={(event) => setField("profitCenter", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-supplier">Preferred supplier code</label>
              <input id="spo-form-supplier" className="sila-input" value={form.preferredSupplierCode ?? ""} disabled={busy}
                onChange={(event) => setField("preferredSupplierCode", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-currency">Currency</label>
              <input id="spo-form-currency" className="sila-input" value={form.defaultCurrency ?? ""} disabled={busy}
                placeholder="AED"
                onChange={(event) => setField("defaultCurrency", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-doctype">PO document type</label>
              <input id="spo-form-doctype" className="sila-input" value={form.defaultPoDocumentType ?? ""} disabled={busy}
                placeholder="NB"
                onChange={(event) => setField("defaultPoDocumentType", event.target.value)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-form-notes">Notes</label>
              <textarea
                id="spo-form-notes"
                className="sila-input"
                rows={2}
                value={form.notes ?? ""}
                disabled={busy}
                onChange={(event) => setField("notes", event.target.value)}
              />
            </div>
            <div className="sila-form-actions">
              <button type="button" className="sila-btn sila-btn--secondary" disabled={busy} onClick={() => setForm(null)}>
                Cancel
              </button>
              <button type="button" className="sila-btn sila-btn--primary" disabled={busy} onClick={() => void handleSave()}>
                {busy ? "Saving…" : "Save"}
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default SilaUserPoDefaults;
