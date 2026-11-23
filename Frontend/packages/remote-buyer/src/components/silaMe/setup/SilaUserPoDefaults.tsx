import React, { useCallback, useEffect, useMemo, useState } from "react";
import { EmptyState, Loader, Modal, PageHeader, toastService } from "@vosox/shared-ui";
import {
  deactivatePoFlexConfig,
  listPoFlexConfigs,
  upsertPoFlexConfig,
  type SilaUserPoFlexCondition,
  type SilaUserPoFlexConfig,
  type SilaUserPoFlexConfigWrite,
  type SilaUserPoFlexParam,
} from "../../../api/silaMe/silaUserPoFlexApi";
import { useBuyerAuthStore } from "../../../store/useBuyerAuthStore";
import "../silaMeTheme.css";

interface SilaUserPoDefaultsProps {
  canManage?: boolean;
}

const CONFIG_TYPES = [{ value: "PO_CREATE", label: "PO Create" }] as const;

/** Suggested seed for SILA Admin PO Create — editable freeform rows, not a fixed schema. */
const SILA_ADMIN_SEED_PARAMS: SilaUserPoFlexParam[] = [
  { paramKey: "CompanyCode", paramValue: "1060", sortOrder: 0 },
  { paramKey: "PlantCode", paramValue: "1060", sortOrder: 1 },
  { paramKey: "StorageLocation", paramValue: "FL01", sortOrder: 2 },
  { paramKey: "PurchasingOrganization", paramValue: "1000", sortOrder: 3 },
  { paramKey: "PurchasingGroup", paramValue: "001", sortOrder: 4 },
  { paramKey: "AccountAssignmentCategory", paramValue: "K", sortOrder: 5 },
  { paramKey: "MaterialGroup", paramValue: "0127", sortOrder: 6 },
  { paramKey: "DefaultPoDocumentType", paramValue: "NB", sortOrder: 7 },
];

const SILA_ADMIN_SEED_CONDITIONS: SilaUserPoFlexCondition[] = [
  {
    whenParam: "AccountAssignmentCategory",
    whenOperator: "EQ",
    whenValue: "K",
    thenParam: "CostCenter",
    thenValue: "1060010108",
    sortOrder: 0,
  },
  {
    whenParam: "AccountAssignmentCategory",
    whenOperator: "EQ",
    whenValue: "K",
    thenParam: "GlAccount",
    thenValue: "3250000109",
    sortOrder: 1,
  },
];

interface EditorState {
  id?: string;
  userId: string;
  displayName: string;
  name: string;
  configType: string;
  isDefault: boolean;
  notes: string;
  parameters: SilaUserPoFlexParam[];
  conditions: SilaUserPoFlexCondition[];
}

const emptyEditor = (userId: string, configType: string): EditorState => ({
  userId,
  displayName: "",
  name: `${CONFIG_TYPES.find((t) => t.value === configType)?.label ?? configType} defaults`,
  configType,
  isDefault: true,
  notes: "",
  parameters: SILA_ADMIN_SEED_PARAMS.map((row) => ({ ...row })),
  conditions: SILA_ADMIN_SEED_CONDITIONS.map((row) => ({ ...row })),
});

const trim = (value?: string | null) => (value ?? "").trim();

/**
 * Flexi PO Defaults & Account Assignment — freeform param/value rows + if/then conditions.
 * Not a fixed-column profile table.
 */
const SilaUserPoDefaults: React.FC<SilaUserPoDefaultsProps> = ({ canManage = true }) => {
  const currentUserId = useBuyerAuthStore((state) => state.personDetail?.userId || "");
  const [userId, setUserId] = useState("");
  const [configType, setConfigType] = useState<string>("PO_CREATE");
  const [loadedUserId, setLoadedUserId] = useState("");
  const [rows, setRows] = useState<SilaUserPoFlexConfig[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [busy, setBusy] = useState(false);
  const [busyId, setBusyId] = useState<string | null>(null);

  useEffect(() => {
    if (!userId && currentUserId) {
      setUserId(currentUserId);
    }
  }, [currentUserId, userId]);

  // Prefill + load signed-in user once so Add / Load are ready without pasting a GUID.
  useEffect(() => {
    if (!currentUserId || loadedUserId) return;
    setUserId((prev) => prev || currentUserId);
    void (async () => {
      setLoading(true);
      setError(null);
      try {
        const data = await listPoFlexConfigs(currentUserId, configType, true);
        setRows(data);
        setLoadedUserId(currentUserId);
      } catch (err: unknown) {
        setRows([]);
        setError(err instanceof Error ? err.message : "Could not load PO defaults configs.");
      } finally {
        setLoading(false);
      }
    })();
    // Only auto-load when signed-in user becomes available.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentUserId]);

  const typeLabel = useMemo(
    () => CONFIG_TYPES.find((t) => t.value === configType)?.label ?? configType,
    [configType],
  );

  const resolveTargetUserId = useCallback(() => {
    const candidate = (loadedUserId || userId || currentUserId || "").trim();
    return candidate;
  }, [currentUserId, loadedUserId, userId]);

  const load = useCallback(async (id: string, type: string) => {
    const trimmed = id.trim();
    if (!trimmed) {
      toastService.error("Enter a User ID (GUID) to load configs.");
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const data = await listPoFlexConfigs(trimmed, type, true);
      setRows(data);
      setLoadedUserId(trimmed);
      setUserId(trimmed);
    } catch (err: unknown) {
      setRows([]);
      setLoadedUserId("");
      setError(err instanceof Error ? err.message : "Could not load PO defaults configs.");
    } finally {
      setLoading(false);
    }
  }, []);

  const openCreate = () => {
    const target = resolveTargetUserId();
    if (!target) {
      toastService.error("Enter a User ID (GUID), or sign in, then Add.");
      return;
    }
    setUserId(target);
    setLoadedUserId(target);
    setEditor(emptyEditor(target, configType));
  };

  const openEdit = (row: SilaUserPoFlexConfig) => {
    setEditor({
      id: row.id,
      userId: row.userId,
      displayName: row.displayName ?? "",
      name: row.name ?? "",
      configType: row.configType || configType,
      isDefault: row.isDefault,
      notes: row.notes ?? "",
      parameters: (row.parameters?.length ? row.parameters : [{ paramKey: "", paramValue: "", sortOrder: 0 }]).map((p, i) => ({
        paramKey: p.paramKey,
        paramValue: p.paramValue ?? "",
        sortOrder: p.sortOrder ?? i,
      })),
      conditions: (row.conditions ?? []).map((c, i) => ({
        whenParam: c.whenParam,
        whenOperator: c.whenOperator || "EQ",
        whenValue: c.whenValue ?? "",
        thenParam: c.thenParam,
        thenValue: c.thenValue ?? "",
        sortOrder: c.sortOrder ?? i,
      })),
    });
  };

  const setParam = (index: number, patch: Partial<SilaUserPoFlexParam>) => {
    if (!editor) return;
    const parameters = editor.parameters.map((row, i) => (i === index ? { ...row, ...patch } : row));
    setEditor({ ...editor, parameters });
  };

  const addParam = () => {
    if (!editor) return;
    setEditor({
      ...editor,
      parameters: [...editor.parameters, { paramKey: "", paramValue: "", sortOrder: editor.parameters.length }],
    });
  };

  const removeParam = (index: number) => {
    if (!editor) return;
    setEditor({ ...editor, parameters: editor.parameters.filter((_, i) => i !== index) });
  };

  const setCondition = (index: number, patch: Partial<SilaUserPoFlexCondition>) => {
    if (!editor) return;
    const conditions = editor.conditions.map((row, i) => (i === index ? { ...row, ...patch } : row));
    setEditor({ ...editor, conditions });
  };

  const addCondition = () => {
    if (!editor) return;
    setEditor({
      ...editor,
      conditions: [
        ...editor.conditions,
        { whenParam: "", whenOperator: "EQ", whenValue: "", thenParam: "", thenValue: "", sortOrder: editor.conditions.length },
      ],
    });
  };

  const removeCondition = (index: number) => {
    if (!editor) return;
    setEditor({ ...editor, conditions: editor.conditions.filter((_, i) => i !== index) });
  };

  const handleSave = async () => {
    if (!editor) return;
    const parameters = editor.parameters
      .map((row, i) => ({
        paramKey: trim(row.paramKey),
        paramValue: trim(row.paramValue) || null,
        sortOrder: i,
      }))
      .filter((row) => row.paramKey);
    if (parameters.length === 0) {
      toastService.error("Add at least one parameter name and value.");
      return;
    }
    const conditions = editor.conditions
      .map((row, i) => ({
        whenParam: trim(row.whenParam),
        whenOperator: (trim(row.whenOperator) || "EQ").toUpperCase(),
        whenValue: trim(row.whenValue) || null,
        thenParam: trim(row.thenParam),
        thenValue: trim(row.thenValue) || null,
        sortOrder: i,
      }))
      .filter((row) => row.whenParam && row.thenParam);

    const payload: SilaUserPoFlexConfigWrite = {
      userId: editor.userId,
      displayName: trim(editor.displayName) || null,
      configType: editor.configType || "PO_CREATE",
      name: trim(editor.name) || null,
      isDefault: Boolean(editor.isDefault),
      notes: trim(editor.notes) || null,
      parameters,
      conditions,
    };

    setBusy(true);
    try {
      await upsertPoFlexConfig(payload, editor.id);
      toastService.success(editor.id ? "PO defaults updated." : "PO defaults saved.");
      setEditor(null);
      await load(editor.userId, editor.configType || configType);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not save PO defaults.");
    } finally {
      setBusy(false);
    }
  };

  const handleDeactivate = async (row: SilaUserPoFlexConfig) => {
    setBusyId(row.id);
    try {
      await deactivatePoFlexConfig(row.id);
      toastService.success("PO defaults config deactivated.");
      await load(loadedUserId, configType);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not deactivate.");
    } finally {
      setBusyId(null);
    }
  };

  return (
    <div className="sila-root sila-me" data-testid="sila-user-po-defaults">
      <PageHeader
        className="pud-page-header"
        title="PO Defaults & Account Assignment"
        description="Flexible parameter defaults by type. Add any parameter name and value, plus if/then conditions (e.g. if AccountAssignmentCategory = K then CostCenter = …)."
      />

      <section className="sila-card">
        <div className="sila-card-body">
          <div className="sila-form-grid" style={{ alignItems: "end" }}>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-user-id">User ID</label>
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
                    void load(userId, configType);
                  }
                }}
              />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-config-type">Type</label>
              <select
                id="spo-config-type"
                className="sila-select"
                value={configType}
                onChange={(event) => {
                  const next = event.target.value;
                  setConfigType(next);
                  if (loadedUserId) void load(loadedUserId, next);
                }}
              >
                {CONFIG_TYPES.map((type) => (
                  <option key={type.value} value={type.value}>{type.label}</option>
                ))}
              </select>
            </div>
            <div className="sila-field" style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
              {currentUserId && (
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary"
                  disabled={loading}
                  onClick={() => {
                    setUserId(currentUserId);
                    void load(currentUserId, configType);
                  }}
                >
                  Use signed-in user
                </button>
              )}
              <button type="button" className="sila-btn sila-btn--secondary" disabled={loading} onClick={() => void load(userId, configType)}>
                {loading ? "Loading…" : "Load"}
              </button>
              {canManage && (
                <button
                  type="button"
                  className="sila-btn sila-btn--primary"
                  data-testid="spo-add-config"
                  disabled={loading || !(loadedUserId || userId.trim() || currentUserId)}
                  onClick={openCreate}
                >
                  Add {typeLabel} config
                </button>
              )}
            </div>
          </div>
          <span className="sila-help">
            Choose type <strong>PO Create</strong>, then Add config. Edit freeform parameter names/values and if/then conditions (e.g. if AA = K then CostCenter = …).
          </span>
        </div>

        {loading && <Loader size={24} message="Loading PO defaults…" />}
        {!loading && error && (
          <EmptyState
            variant="error"
            title="Could not load configs"
            description={error}
            action={<button type="button" className="sila-btn sila-btn--secondary" onClick={() => void load(userId, configType)}>Try again</button>}
          />
        )}
        {!loading && !error && loadedUserId && rows.length === 0 && (
          <EmptyState
            title={`No ${typeLabel} configs`}
            description="Add a flexible defaults config with parameter names/values and optional if/then conditions."
          />
        )}
        {!loading && !error && rows.length > 0 && (
          <div className="sila-table-wrap">
            <table className="sila-table">
              <thead>
                <tr>
                  <th scope="col">Default</th>
                  <th scope="col">Name</th>
                  <th scope="col">Type</th>
                  <th scope="col">Params</th>
                  <th scope="col">Conditions</th>
                  <th scope="col">Updated</th>
                  {canManage && <th scope="col"><span className="sila-visually-hidden">Actions</span></th>}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>{row.isDefault ? "Yes" : "—"}</td>
                    <td className="sila-cell-strong">{row.name || "—"}</td>
                    <td>{row.configType}</td>
                    <td>{row.parameters?.length ?? 0}</td>
                    <td>{row.conditions?.length ?? 0}</td>
                    <td>{row.dateUpdated ? new Date(row.dateUpdated).toLocaleString() : "—"}</td>
                    {canManage && (
                      <td>
                        <div className="srcv-actions" style={{ display: "flex", gap: "0.35rem", flexWrap: "wrap" }}>
                          <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => openEdit(row)}>
                            Edit
                          </button>
                          <button
                            type="button"
                            className="sila-btn sila-btn--danger sila-btn--sm"
                            disabled={busyId === row.id}
                            onClick={() => void handleDeactivate(row)}
                          >
                            Deactivate
                          </button>
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
        isOpen={Boolean(editor)}
        onClose={() => !busy && setEditor(null)}
        size="lg"
        style={{ maxWidth: 820 }}
        headerProps={{ heading: editor?.id ? `Edit ${typeLabel} defaults` : `Add ${typeLabel} defaults` }}
      >
        {editor && (
          <div className="sila-root" style={{ display: "grid", gap: "1rem", minWidth: "min(92vw, 720px)" }}>
            <div className="sila-form-grid">
              <div className="sila-field">
                <label className="sila-label" htmlFor="spo-flex-user">User ID</label>
                <input id="spo-flex-user" className="sila-input" value={editor.userId} disabled readOnly />
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="spo-flex-type">Type</label>
                <select
                  id="spo-flex-type"
                  className="sila-select"
                  value={editor.configType}
                  disabled={busy}
                  onChange={(event) => setEditor({ ...editor, configType: event.target.value })}
                >
                  {CONFIG_TYPES.map((type) => (
                    <option key={type.value} value={type.value}>{type.label}</option>
                  ))}
                </select>
              </div>
              <div className="sila-field">
                <label className="sila-label" htmlFor="spo-flex-name">Name</label>
                <input
                  id="spo-flex-name"
                  className="sila-input"
                  value={editor.name}
                  disabled={busy}
                  onChange={(event) => setEditor({ ...editor, name: event.target.value })}
                />
              </div>
              <div className="sila-field">
                <label className="sila-choice" htmlFor="spo-flex-default">
                  <input
                    id="spo-flex-default"
                    type="checkbox"
                    checked={editor.isDefault}
                    disabled={busy}
                    onChange={(event) => setEditor({ ...editor, isDefault: event.target.checked })}
                  />
                  Default config
                </label>
              </div>
            </div>

            <section>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem" }}>
                <h3 className="sila-section-title" style={{ margin: 0 }}>Parameters</h3>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" disabled={busy} onClick={addParam}>
                  Add parameter
                </button>
              </div>
              <p className="sila-help">Add any parameter name and value (CompanyCode, PlantCode, CostCenter, custom keys, …).</p>
              <div className="sila-table-wrap">
                <table className="sila-table">
                  <thead>
                    <tr>
                      <th scope="col">Parameter</th>
                      <th scope="col">Value</th>
                      <th scope="col"><span className="sila-visually-hidden">Remove</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    {editor.parameters.map((row, index) => (
                      <tr key={`p-${index}`}>
                        <td>
                          <input
                            className="sila-input"
                            placeholder="e.g. CompanyCode"
                            value={row.paramKey}
                            disabled={busy}
                            onChange={(event) => setParam(index, { paramKey: event.target.value })}
                          />
                        </td>
                        <td>
                          <input
                            className="sila-input"
                            placeholder="e.g. 1060"
                            value={row.paramValue ?? ""}
                            disabled={busy}
                            onChange={(event) => setParam(index, { paramValue: event.target.value })}
                          />
                        </td>
                        <td>
                          <button type="button" className="sila-btn sila-btn--danger sila-btn--sm" disabled={busy || editor.parameters.length <= 1} onClick={() => removeParam(index)}>
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <section>
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem" }}>
                <h3 className="sila-section-title" style={{ margin: 0 }}>Conditions</h3>
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" disabled={busy} onClick={addCondition}>
                  Add condition
                </button>
              </div>
              <p className="sila-help">If parameter operator value → set parameter to value. Example: if AccountAssignmentCategory = K then pass CostCenter 1060010108.</p>
              <div className="sila-table-wrap">
                <table className="sila-table">
                  <thead>
                    <tr>
                      <th scope="col">When param</th>
                      <th scope="col">Op</th>
                      <th scope="col">When value</th>
                      <th scope="col">Then param</th>
                      <th scope="col">Then value</th>
                      <th scope="col"><span className="sila-visually-hidden">Remove</span></th>
                    </tr>
                  </thead>
                  <tbody>
                    {editor.conditions.length === 0 && (
                      <tr>
                        <td colSpan={6} className="sila-help">No conditions yet.</td>
                      </tr>
                    )}
                    {editor.conditions.map((row, index) => (
                      <tr key={`c-${index}`}>
                        <td>
                          <input className="sila-input" value={row.whenParam} disabled={busy}
                            placeholder="AccountAssignmentCategory"
                            onChange={(event) => setCondition(index, { whenParam: event.target.value })} />
                        </td>
                        <td>
                          <select className="sila-select" value={row.whenOperator || "EQ"} disabled={busy}
                            onChange={(event) => setCondition(index, { whenOperator: event.target.value })}>
                            <option value="EQ">=</option>
                            <option value="NE">≠</option>
                            <option value="CONTAINS">contains</option>
                          </select>
                        </td>
                        <td>
                          <input className="sila-input" value={row.whenValue ?? ""} disabled={busy}
                            placeholder="K"
                            onChange={(event) => setCondition(index, { whenValue: event.target.value })} />
                        </td>
                        <td>
                          <input className="sila-input" value={row.thenParam} disabled={busy}
                            placeholder="CostCenter"
                            onChange={(event) => setCondition(index, { thenParam: event.target.value })} />
                        </td>
                        <td>
                          <input className="sila-input" value={row.thenValue ?? ""} disabled={busy}
                            placeholder="1060010108"
                            onChange={(event) => setCondition(index, { thenValue: event.target.value })} />
                        </td>
                        <td>
                          <button type="button" className="sila-btn sila-btn--danger sila-btn--sm" disabled={busy} onClick={() => removeCondition(index)}>
                            Remove
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            <div className="sila-field">
              <label className="sila-label" htmlFor="spo-flex-notes">Notes</label>
              <textarea
                id="spo-flex-notes"
                className="sila-input"
                rows={2}
                value={editor.notes}
                disabled={busy}
                onChange={(event) => setEditor({ ...editor, notes: event.target.value })}
              />
            </div>

            <div className="sila-form-actions">
              <button type="button" className="sila-btn sila-btn--secondary" disabled={busy} onClick={() => setEditor(null)}>
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
