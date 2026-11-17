import React, { useEffect, useState } from "react";
import { EmptyState, Loader, Modal } from "@vosox/shared-ui";
import { getMaterialDetail, type SilaMaterialDetail } from "../../../api/silaMe/silaMaterialsApi";
import { formatDateTime, formatPrice } from "./materialFormat";
import { resolveStudioKind, StudioCover } from "../recipes/recipeStudioVisuals";
import "../silaMeTheme.css";
import "./SilaMaterials.css";

interface SilaMaterialDetailPanelProps {
  materialId: string;
  onClose: () => void;
}

/** Catalog-oriented Material Master detail: general, plants, SLoc, inventory, UOM, valuation, integration. */
const SilaMaterialDetailPanel: React.FC<SilaMaterialDetailPanelProps> = ({ materialId, onClose }) => {
  const [detail, setDetail] = useState<SilaMaterialDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    getMaterialDetail(materialId)
      .then((row) => {
        if (active) setDetail(row);
      })
      .catch((err: unknown) => {
        if (active) setError(err instanceof Error ? err.message : "Could not load material detail.");
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [materialId]);

  const kind = resolveStudioKind(detail?.materialGroup, detail?.category, detail?.description, detail?.materialCode);

  return (
    <Modal
      isOpen
      onClose={onClose}
      size="xl"
      headerProps={{
        heading: detail ? detail.materialCode : "Material detail",
        subHeading: detail?.description || "Plant, storage location, inventory and valuation",
      }}
      footerProps={{
        secondaryButton: { text: "Close", variant: "secondary", onClick: onClose },
      }}
    >
      <div className="sila-root sila-me smat-detail">
        {loading && <Loader size={24} message="Loading material…" />}
        {error && <EmptyState variant="error" title="Couldn't load detail" description={error} />}
        {detail && !loading && (
          <>
            <div className="smat-detail-hero">
              <StudioCover kind={kind} title={detail.description || detail.materialCode} />
              <div>
                <p className="sila-help">General</p>
                <h3>{detail.description}</h3>
                <p className="sila-help">
                  {detail.materialType || "Type —"} · {detail.materialGroup || "Group —"} · {detail.category || "Category —"}
                  {" · "}Base {detail.baseUom} · Source {detail.source || "—"}
                </p>
              </div>
            </div>

            <section className="sila-card">
              <div className="sila-card-header"><h2 className="sila-card-title">Plant assignments</h2></div>
              <div className="sila-card-body">
                {detail.plants.length === 0 ? (
                  <span className="sila-help">No plant assignments synced yet. Map Material.Plant on GET_MATERIAL and pull.</span>
                ) : (
                  <div className="sila-table-wrap">
                    <table className="sila-table">
                      <thead>
                        <tr>
                          <th>Plant</th><th>Property</th><th>Status</th><th>Purchasing</th><th>MRP</th><th>Reorder</th><th>Safety</th><th>Synced</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.plants.map((row) => (
                          <tr key={row.id}>
                            <td>{row.plantCode}</td>
                            <td>{row.propertyName || "—"}</td>
                            <td>{row.plantStatus || "—"}</td>
                            <td>{row.purchasingGroup || "—"}</td>
                            <td>{row.mrpType || "—"}</td>
                            <td>{row.reorderPoint ?? "—"}</td>
                            <td>{row.safetyStock ?? "—"}</td>
                            <td>{row.lastSyncedOn ? formatDateTime(row.lastSyncedOn) : "—"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </section>

            <section className="sila-card">
              <div className="sila-card-header"><h2 className="sila-card-title">Storage locations</h2></div>
              <div className="sila-card-body">
                {detail.storageLocations.length === 0 ? (
                  <span className="sila-help">No storage-location assignments synced. Unmapped SAP SLocs go to the exception worklist — locations are not auto-created.</span>
                ) : (
                  <div className="sila-table-wrap">
                    <table className="sila-table">
                      <thead>
                        <tr>
                          <th>Plant</th><th>SAP SLoc</th><th>SILA location</th><th>Type</th><th>Status</th><th>Mapped</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.storageLocations.map((row) => (
                          <tr key={row.id}>
                            <td>{row.plantCode}</td>
                            <td>{row.storageLocationCode}</td>
                            <td>{row.silaLocationName || "—"}</td>
                            <td>{row.silaLocationType || "—"}</td>
                            <td>{row.assignmentStatus || "—"}</td>
                            <td>{row.mapped ? "Yes" : "No"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </section>

            <section className="sila-card">
              <div className="sila-card-header"><h2 className="sila-card-title">Inventory (SILA ledger)</h2></div>
              <div className="sila-card-body">
                <p className="sila-help">On-hand comes from the Inventory ledger. ERP snapshots are shown for comparison only and never overwrite balances.</p>
                {detail.inventory.length === 0 ? (
                  <span className="sila-help">No ledger balances at mapped locations.</span>
                ) : (
                  <div className="sila-table-wrap">
                    <table className="sila-table">
                      <thead>
                        <tr>
                          <th>Location</th><th>Plant</th><th>SLoc</th><th>On hand</th><th>In transit</th><th>Available</th><th>ERP unrestricted</th><th>Freshness</th>
                        </tr>
                      </thead>
                      <tbody>
                        {detail.inventory.map((row) => (
                          <tr key={row.inventoryLocationId ?? `${row.locationName}-${row.storageLocationCode}`}>
                            <td>{row.locationName || "—"}</td>
                            <td>{row.plantCode || "—"}</td>
                            <td>{row.storageLocationCode || "—"}</td>
                            <td>{row.onHandQty} {row.baseUom}</td>
                            <td>{row.inTransitQty}</td>
                            <td>{row.availableQty}</td>
                            <td>{row.erpUnrestrictedQty ?? "—"}{row.erpSnapshotOn ? ` @ ${formatDateTime(row.erpSnapshotOn)}` : ""}</td>
                            <td>{row.stockFreshness}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </section>

            <section className="sila-card">
              <div className="sila-card-header"><h2 className="sila-card-title">UOM</h2></div>
              <div className="sila-card-body">
                <p className="sila-help">Base {detail.baseUom}. Conversions are material-specific.</p>
                {detail.conversions.length === 0 ? (
                  <span className="sila-help">Base UOM only.</span>
                ) : (
                  <ul className="srec-stack">
                    {detail.conversions.map((row) => (
                      <li key={`${row.fromUom}-${row.toUom}`}>1 {row.fromUom} = {row.factor} {row.toUom}</li>
                    ))}
                  </ul>
                )}
              </div>
            </section>

            <section className="sila-card">
              <div className="sila-card-header"><h2 className="sila-card-title">Valuation</h2></div>
              <div className="sila-card-body smat-detail-grid">
                <div><span className="sila-help">Approved unit cost</span><strong>{formatPrice(detail.valuation.approvedUnitCost, detail.valuation.currency, detail.baseUom)}</strong></div>
                <div><span className="sila-help">Standard</span><strong>{detail.valuation.standardPrice == null ? "—" : formatPrice(detail.valuation.standardPrice, detail.valuation.currency, detail.baseUom)}</strong></div>
                <div><span className="sila-help">Moving average</span><strong>{detail.valuation.movingAveragePrice == null ? "—" : formatPrice(detail.valuation.movingAveragePrice, detail.valuation.currency, detail.baseUom)}</strong></div>
                <div><span className="sila-help">Price control</span><strong>{detail.valuation.priceControl || "—"}</strong></div>
                <div><span className="sila-help">Valuation class</span><strong>{detail.valuation.valuationClass || "—"}</strong></div>
                <div><span className="sila-help">Valuation area</span><strong>{detail.valuation.valuationArea || "Not assumed = plant"}</strong></div>
              </div>
            </section>

            <section className="sila-card">
              <div className="sila-card-header"><h2 className="sila-card-title">Integration status</h2></div>
              <div className="sila-card-body smat-detail-grid">
                <div><span className="sila-help">Material sync</span><strong>{detail.integration.lastMaterialSync ? formatDateTime(detail.integration.lastMaterialSync) : "—"}</strong></div>
                <div><span className="sila-help">Stock sync</span><strong>{detail.integration.lastStockSync ? formatDateTime(detail.integration.lastStockSync) : "—"}</strong></div>
                <div><span className="sila-help">Valuation sync</span><strong>{detail.integration.lastValuationSync ? formatDateTime(detail.integration.lastValuationSync) : "—"}</strong></div>
                <div><span className="sila-help">Source</span><strong>{detail.integration.sourceSystem || "—"}</strong></div>
                <div><span className="sila-help">Open exceptions</span><strong>{detail.integration.openExceptions}</strong></div>
              </div>
              {detail.integration.exceptionMessages.length > 0 && (
                <ul className="srec-stack">
                  {detail.integration.exceptionMessages.map((message) => <li key={message} className="sila-help">{message}</li>)}
                </ul>
              )}
            </section>
          </>
        )}
      </div>
    </Modal>
  );
};

export default SilaMaterialDetailPanel;
