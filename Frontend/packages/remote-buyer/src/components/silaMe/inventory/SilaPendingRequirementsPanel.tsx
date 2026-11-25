import React, { useCallback, useEffect, useMemo, useState } from "react";
import { Loader, Modal, toastService } from "@vosox/shared-ui";
import {
  formatQty,
  getPendingOutletRequirements,
  type SilaLocation,
  type SilaPendingOutletRequirement,
} from "../../../api/silaMe/silaInventoryApi";
import { createTransfer } from "../../../api/silaMe/silaMovementsApi";
import SilaSearchableSelect from "../SilaSearchableSelect";

interface SilaPendingRequirementsPanelProps {
  open: boolean;
  onClose: () => void;
  canTransfer: boolean;
  locations: SilaLocation[];
  sourceLocationId: string;
  onCreated: () => void;
}

/**
 * Compact demand-based ITO drawer: pending Weekly Bucket / Wishlist requirements → Create ITO.
 * Reuses existing createTransfer; does not redesign Live Inventory.
 */
const SilaPendingRequirementsPanel: React.FC<SilaPendingRequirementsPanelProps> = ({
  open,
  onClose,
  canTransfer,
  locations,
  sourceLocationId,
  onCreated,
}) => {
  const [rows, setRows] = useState<SilaPendingOutletRequirement[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<Record<string, boolean>>({});
  const [qtys, setQtys] = useState<Record<string, string>>({});
  const [fromId, setFromId] = useState(sourceLocationId);
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getPendingOutletRequirements({ sourceLocationId: fromId || undefined });
      setRows(data.items);
      const nextQty: Record<string, string> = {};
      data.items.forEach((row) => {
        nextQty[row.requirementId] = String(row.availableForNewIto);
      });
      setQtys(nextQty);
      setSelected({});
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not load pending requirements.");
    } finally {
      setLoading(false);
    }
  }, [fromId]);

  useEffect(() => {
    if (open) {
      setFromId(sourceLocationId);
      load();
    }
  }, [open, sourceLocationId, load]);

  const chosen = useMemo(() => rows.filter((row) => selected[row.requirementId]), [rows, selected]);

  const createIto = async () => {
    if (!canTransfer || !fromId || chosen.length === 0) return;
    const destinations = new Set(chosen.map((row) => row.destinationLocationId).filter(Boolean));
    if (destinations.size !== 1) {
      toastService.error("Select requirements that share one destination location for a single ITO.");
      return;
    }
    const toLocationId = [...destinations][0] as string;
    if (toLocationId === fromId) {
      toastService.error("Source and destination must differ.");
      return;
    }
    const items = chosen.map((row) => {
      const quantity = Math.min(Number(qtys[row.requirementId]) || 0, row.availableForNewIto);
      return {
        materialId: row.materialId,
        quantity,
        uom: row.uom,
        weeklyBucketId: row.weeklyBucketId ?? null,
        weeklyBucketItemId: row.weeklyBucketItemId ?? null,
        wishlistItemId: row.wishlistItemId ?? null,
      };
    }).filter((line) => line.quantity > 0);
    if (items.length === 0) {
      toastService.error("Enter a positive transfer quantity.");
      return;
    }
    setSaving(true);
    try {
      await createTransfer({
        fromLocationId: fromId,
        toLocationId,
        reason: "Demand-based ITO from pending outlet requirements",
        requiredBy: null,
        requireSamePlant: true,
        addToLocation: true,
        items,
      });
      toastService.success("ITO created from pending requirements.");
      onCreated();
      onClose();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not create the ITO.");
    } finally {
      setSaving(false);
    }
  };

  if (!open) return null;

  return (
    <Modal isOpen onClose={onClose} title="Pending Outlet Requirements" size="lg">
      <div className="liv-req-panel" data-testid="pending-outlet-requirements">
        <p className="liv-sub">
          Outstanding Weekly Bucket and mapped Wishlist lines. Create ITO uses movement type 311 via FIVE S4 - Transfer Stock at execution.
        </p>
        <div className="liv-req-toolbar">
          <SilaSearchableSelect
            label="Source location"
            placeholder="Store"
            value={fromId}
            options={locations
              .filter((location) => location.locationType === "STORE" || location.transferEnabled)
              .map((location) => ({
                value: location.id,
                label: `${location.locationCode} · ${location.locationName}`,
              }))}
            onChange={setFromId}
          />
          <button type="button" className="sila-btn sila-btn--secondary" onClick={load} disabled={loading}>
            Refresh
          </button>
        </div>
        {loading ? <Loader size={24} message="Loading requirements..." /> : null}
        {error ? <p className="sila-error">{error}</p> : null}
        {!loading && !error && rows.length === 0 ? (
          <p className="liv-sub">No actionable pending requirements in your authorized scope.</p>
        ) : null}
        {rows.length > 0 && (
          <div className="liv-table-wrap">
            <table className="liv-table">
              <thead>
                <tr>
                  <th />
                  <th>Source</th>
                  <th>Outlet</th>
                  <th>Material</th>
                  <th>Available for ITO</th>
                  <th>Qty</th>
                  <th>Source stock</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.requirementId}>
                    <td>
                      <input
                        type="checkbox"
                        checked={!!selected[row.requirementId]}
                        onChange={(event) =>
                          setSelected((current) => ({ ...current, [row.requirementId]: event.target.checked }))
                        }
                      />
                    </td>
                    <td>
                      {row.requirementSource === "WEEKLY_BUCKET" ? "Weekly Bucket" : "Wishlist"}
                      <div className="liv-muted">{row.sourceReference}</div>
                    </td>
                    <td>{row.destinationOutletName || row.destinationLocationCode || "—"}</td>
                    <td>
                      {row.materialCode}
                      <div className="liv-muted">{row.materialDescription}</div>
                    </td>
                    <td>
                      {formatQty(row.availableForNewIto)} {row.uom}
                    </td>
                    <td>
                      <input
                        className="sila-input"
                        value={qtys[row.requirementId] ?? ""}
                        onChange={(event) => setQtys((current) => ({ ...current, [row.requirementId]: event.target.value }))}
                      />
                    </td>
                    <td>
                      {formatQty(row.availableSourceStock)} {row.uom}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <div className="liv-req-actions">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={onClose}>
            Close
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            disabled={!canTransfer || saving || chosen.length === 0}
            onClick={createIto}
          >
            {saving ? "Creating…" : "Create ITO"}
          </button>
        </div>
      </div>
    </Modal>
  );
};

export default SilaPendingRequirementsPanel;
