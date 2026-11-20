import React, { useCallback, useEffect, useState } from "react";
import { EmptyState, toastService } from "@vosox/shared-ui";
import {
  processPosBatch,
  pullPosSales,
  type SilaPosConsumptionPreview,
  type SilaPosImportResult,
  type SilaPosPreview,
} from "../../../api/silaMe/silaPosApi";
import { getPosSources, type SilaPosSource } from "../../../api/silaMe/silaPosMasterApi";
import SilaPosBatches from "./SilaPosBatches";
import SilaPosImportSummary from "./SilaPosImportSummary";
import SilaPosItemMappings from "./SilaPosItemMappings";
import SilaPosOutletMappings from "./SilaPosOutletMappings";
import SilaPosSalesUpload from "./SilaPosSalesUpload";
import SilaPosSources from "./SilaPosSources";
import SilaPosStudioExceptions from "./SilaPosStudioExceptions";
import SilaPosStudioOverview from "./SilaPosStudioOverview";
import { SilaPosConfirm } from "./SilaPosShared";
import SilaTransactionTracker from "./SilaTransactionTracker";
import { errorText } from "./posFormat";
import "../silaMeTheme.css";
import "../recipes/SilaRecipes.css";
import "./SilaPos.css";
import "./SilaPosStudio.css";

interface SilaPosIntegrationProps {
  /** Shows the write actions (sources, mappings, upload, process, pull). The backend checks MANAGE_SILA_POS. */
  canManage: boolean;
}

type PosStudioTab =
  | "overview"
  | "upload"
  | "api"
  | "review"
  | "preview"
  | "post"
  | "results"
  | "exceptions"
  | "tracker"
  | "batches";

const TABS: { key: PosStudioTab; label: string }[] = [
  { key: "overview", label: "Overview" },
  { key: "upload", label: "Upload Sales" },
  { key: "api", label: "POS API Integration" },
  { key: "review", label: "Sales Review" },
  { key: "preview", label: "Consumption Preview" },
  { key: "post", label: "Post Consumption" },
  { key: "results", label: "Posting Results" },
  { key: "exceptions", label: "Exceptions" },
  { key: "tracker", label: "Transaction Tracker" },
  { key: "batches", label: "Upload History" },
];

type ApiSubTab = "sources" | "outlets" | "items";

/** SILA POS Studio: Recipe Studio visual language over the existing POS consumption workflow. */
const SilaPosIntegration: React.FC<SilaPosIntegrationProps> = ({ canManage }) => {
  const [tab, setTab] = useState<PosStudioTab>("overview");
  const [apiSubTab, setApiSubTab] = useState<ApiSubTab>("sources");
  const [sources, setSources] = useState<SilaPosSource[]>([]);
  const [sourcesLoading, setSourcesLoading] = useState(true);
  const [sourcesError, setSourcesError] = useState<string | null>(null);
  const [pulling, setPulling] = useState(false);
  const [pullResult, setPullResult] = useState<SilaPosImportResult | null>(null);
  const [batchesVersion, setBatchesVersion] = useState(0);
  const [preview, setPreview] = useState<SilaPosPreview | null>(null);
  const [consumption, setConsumption] = useState<SilaPosConsumptionPreview | null>(null);
  const [processResult, setProcessResult] = useState<SilaPosImportResult | null>(null);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [confirmPost, setConfirmPost] = useState(false);
  const [posting, setPosting] = useState(false);

  const loadSources = useCallback(async () => {
    setSourcesLoading(true);
    setSourcesError(null);
    try {
      setSources(await getPosSources());
    } catch (err: unknown) {
      setSources([]);
      setSourcesError(errorText(err, "Could not load the POS sources."));
    } finally {
      setSourcesLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSources();
  }, [loadSources]);

  const handlePull = async () => {
    if (!canManage) return;
    setPulling(true);
    try {
      const result = await pullPosSales();
      setPullResult(result);
      setProcessResult(result);
      setBatchesVersion((current) => current + 1);
      setTab("results");
      toastService.success(`Batch ${result.batchNumber}: ${result.accepted} line${result.accepted === 1 ? "" : "s"} received.`);
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not pull the sales from the POS API."));
    } finally {
      setPulling(false);
    }
  };

  const handleConfirmPost = async () => {
    if (!canManage || !preview || selectedIds.length === 0 || posting) return;
    setPosting(true);
    setConfirmPost(false);
    try {
      const result = await processPosBatch(preview.batchId, { transactionIds: selectedIds, queueErpPosting: true });
      setProcessResult(result);
      setPreview(null);
      setConsumption(null);
      setSelectedIds([]);
      setBatchesVersion((current) => current + 1);
      setTab("results");
      toastService.success(`Batch ${result.batchNumber}: ${result.processed} line${result.processed === 1 ? "" : "s"} processed.`);
    } catch (err: unknown) {
      toastService.error(errorText(err, "Could not process the sales batch."));
    } finally {
      setPosting(false);
    }
  };

  const sourceProps = { sources, sourcesLoading, sourcesError, onReloadSources: loadSources, canManage };

  const postSteps = [
    { key: "validate", label: "1 · Validate", done: Boolean(preview) },
    { key: "review", label: "2 · Review", done: selectedIds.length > 0 },
    { key: "confirm", label: "3 · Confirm", done: Boolean(consumption) },
    { key: "post", label: "4 · Post", done: Boolean(processResult) },
    { key: "reconcile", label: "5 · Reconcile", done: Boolean(processResult && (processResult.postingUnknown ?? 0) === 0 && processResult.failed === 0) },
  ];

  return (
    <div className="sila-me srec-page rstudio-page pstudio-page" data-testid="pstudio-root">
      <header className="pstudio-hero">
        <div>
          <p className="pstudio-kicker">SILA POS Studio</p>
          <h1 className="pstudio-hero__title">SILA POS Studio</h1>
          <p className="pstudio-hero__sub">
            Sales Intelligence · Recipe Consumption · Inventory · SAP Integration
          </p>
        </div>
        <div className="pstudio-hero__actions">
          {canManage && (
            <button type="button" className="sila-btn sila-btn--secondary" onClick={handlePull} disabled={pulling}>
              {pulling ? "Syncing..." : "Manual POS API sync"}
            </button>
          )}
          <button type="button" className="sila-btn sila-btn--primary" onClick={() => setTab("upload")}>
            Upload sales
          </button>
        </div>
      </header>

      {(pullResult || processResult) && tab === "results" && (
        <SilaPosImportSummary
          result={(processResult || pullResult)!}
          onClose={() => {
            setPullResult(null);
            if (processResult === pullResult) setProcessResult(null);
          }}
        />
      )}

      <nav className="pstudio-tabs" role="tablist" aria-label="POS Studio sections">
        {TABS.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            id={`pstudio-tab-${item.key}`}
            aria-controls={`pstudio-panel-${item.key}`}
            className="pstudio-tab"
            aria-selected={tab === item.key}
            onClick={() => setTab(item.key)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      <div role="tabpanel" id={`pstudio-panel-${tab}`} aria-labelledby={`pstudio-tab-${tab}`} className="srec-stack">
        {tab === "overview" && (
          <SilaPosStudioOverview
            onOpenUpload={() => setTab("upload")}
            onOpenBatches={() => setTab("batches")}
            onOpenExceptions={() => setTab("exceptions")}
            onOpenTracker={() => setTab("tracker")}
          />
        )}

        {tab === "upload" && (
          <SilaPosSalesUpload
            sources={sources}
            canManage={canManage}
            studioMode
            preview={preview}
            consumption={consumption}
            onPreviewChange={(next) => {
              setPreview(next);
              setConsumption(null);
              setProcessResult(null);
              setSelectedIds([]);
              if (next) setTab("review");
            }}
            onConsumptionChange={setConsumption}
            onSelectedIdsChange={setSelectedIds}
            onProcessed={(result) => {
              setProcessResult(result);
              setPreview(null);
              setConsumption(null);
              setBatchesVersion((current) => current + 1);
              setTab("results");
            }}
            onOpenPreview={() => setTab("preview")}
            onOpenPost={() => setTab("post")}
          />
        )}

        {tab === "api" && (
          <div className="srec-stack">
            <section className="pstudio-panel pstudio-panel--lavender">
              <h2 className="pstudio-panel__title">POS API Integration</h2>
              <p className="pstudio-panel__hint">
                Configure POS sources and mappings. Credentials stay in the integration framework — this screen never shows secrets.
                Opening this page does not activate a live connector.
              </p>
              <div className="pstudio-kpis">
                <div className="pstudio-kpi pstudio-kpi--lavender"><span>Configured sources</span><strong>{sources.length}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--mint"><span>Connection</span><strong>{sourcesLoading ? "…" : sourcesError ? "Error" : "Ready"}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--sky"><span>Manual sync</span><strong>{canManage ? "Available" : "View only"}</strong></div>
              </div>
              {canManage && (
                <div className="srec-actions">
                  <button type="button" className="sila-btn sila-btn--secondary" onClick={handlePull} disabled={pulling}>
                    {pulling ? "Syncing..." : "Manual sync now"}
                  </button>
                  <button type="button" className="sila-btn sila-btn--ghost" onClick={loadSources} disabled={sourcesLoading}>
                    Refresh sources
                  </button>
                </div>
              )}
            </section>
            <div className="pstudio-tabs" role="tablist" aria-label="API configuration">
              {([
                ["sources", "Sources"],
                ["outlets", "Outlet mapping"],
                ["items", "Item mapping"],
              ] as const).map(([key, label]) => (
                <button
                  key={key}
                  type="button"
                  role="tab"
                  className="pstudio-tab"
                  aria-selected={apiSubTab === key}
                  onClick={() => setApiSubTab(key)}
                >
                  {label}
                </button>
              ))}
            </div>
            <section className="pstudio-panel">
              {apiSubTab === "sources" && <SilaPosSources {...sourceProps} />}
              {apiSubTab === "outlets" && <SilaPosOutletMappings {...sourceProps} />}
              {apiSubTab === "items" && <SilaPosItemMappings {...sourceProps} />}
            </section>
          </div>
        )}

        {tab === "review" && (
          preview ? (
            <SilaPosSalesUpload
              sources={sources}
              canManage={canManage}
              studioMode
              reviewOnly
              preview={preview}
              consumption={consumption}
              onPreviewChange={(next) => {
                setPreview(next);
                if (!next) setTab("upload");
              }}
              onConsumptionChange={(next) => {
                setConsumption(next);
                if (next) setTab("preview");
              }}
              onSelectedIdsChange={setSelectedIds}
              onProcessed={(result) => {
                setProcessResult(result);
                setPreview(null);
                setConsumption(null);
                setBatchesVersion((current) => current + 1);
                setTab("results");
              }}
              onOpenPreview={() => setTab("preview")}
              onOpenPost={() => setTab("post")}
            />
          ) : (
            <EmptyState
              title="No sales batch in review"
              description="Upload a POS sales file first. Validated lines appear here for selection."
              action={<button type="button" className="sila-btn sila-btn--primary" onClick={() => setTab("upload")}>Upload sales</button>}
            />
          )
        )}

        {tab === "preview" && (
          consumption ? (
            <section className="pstudio-panel pstudio-panel--mint" data-testid="pstudio-consumption">
              <h2 className="pstudio-panel__title">Consumption preview</h2>
              <p className="pstudio-panel__hint">
                Theoretical recipe explosion and estimated cost only. These values are not completed stock movements.
              </p>
              <div className="pstudio-kpis">
                <div className="pstudio-kpi pstudio-kpi--mint"><span>Total selected sales</span><strong>{consumption.lines.length}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--sky"><span>Total ingredient lines</span><strong>{consumption.lines.reduce((n, line) => n + line.ingredientCount, 0)}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--lavender"><span>Estimated consumption cost</span><strong>{consumption.totalConsumptionCost}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--amber"><span>Cost pending</span><strong>{consumption.lines.filter((line) => line.costPending).length}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--blue"><span>Total sales amount</span><strong>{consumption.totalSalesAmount}</strong></div>
              </div>
              <div className="pstudio-grid-2">
                <div>
                  <h3 className="pstudio-panel__title">Selected sales</h3>
                  <ul className="pstudio-list">
                    {consumption.lines.map((line) => (
                      <li key={line.saleTransactionId} className="pstudio-list__item">
                        <div>
                          <strong>{line.posCode}</strong>
                          <span className="srec-sub">
                            {line.outletCode}
                            {line.plantCode ? ` · Plant ${line.plantCode}` : ""}
                            {line.storageLocationCode ? ` · SLoc ${line.storageLocationCode}` : ""}
                          </span>
                          <span className="srec-sub">{line.recipeCode || "—"}{line.recipeVersion != null ? ` v${line.recipeVersion}` : ""}</span>
                        </div>
                        <span>{line.quantitySold} {line.uom || ""} · {line.salesAmount ?? "—"} {line.currency || ""}</span>
                      </li>
                    ))}
                  </ul>
                </div>
                <div>
                  <h3 className="pstudio-panel__title">Ingredient consumption</h3>
                  <p className="pstudio-panel__hint">Current stock / remaining stock are not returned by the consumption-preview API — shown as unavailable.</p>
                  <div className="sila-table-wrap">
                    <table className="sila-table">
                      <thead>
                        <tr>
                          <th scope="col">Material</th>
                          <th scope="col">SAP code</th>
                          <th scope="col">Current stock</th>
                          <th scope="col" className="srec-num">Consumption qty</th>
                          <th scope="col">UOM</th>
                          <th scope="col">Expected remaining</th>
                          <th scope="col">Est. cost</th>
                          <th scope="col">Warning</th>
                        </tr>
                      </thead>
                      <tbody>
                        {consumption.lines.flatMap((line) =>
                          line.ingredients.map((ingredient) => (
                            <tr key={`${line.saleTransactionId}-${ingredient.materialId}`}>
                              <td>{ingredient.materialDescription || "—"}</td>
                              <td>{ingredient.materialCode || ingredient.materialId}</td>
                              <td>Unavailable</td>
                              <td className="srec-num">{ingredient.quantity}</td>
                              <td>{ingredient.baseUom || "—"}</td>
                              <td>Unavailable</td>
                              <td>{ingredient.costPending ? "COST PENDING" : (ingredient.estimatedCost ?? "—")}</td>
                              <td>{ingredient.costPending ? "Cost pending" : "—"}</td>
                            </tr>
                          )),
                        )}
                      </tbody>
                    </table>
                  </div>
                </div>
              </div>
              <div className="srec-actions">
                <button type="button" className="sila-btn sila-btn--ghost" onClick={() => setTab("review")}>Back to review</button>
                <button type="button" className="sila-btn sila-btn--primary" onClick={() => setTab("post")}>Continue to post</button>
              </div>
            </section>
          ) : (
            <EmptyState
              title="No consumption preview yet"
              description="Select ready lines in Sales Review and run Consumption preview."
              action={<button type="button" className="sila-btn sila-btn--primary" onClick={() => setTab("review")}>Open sales review</button>}
            />
          )
        )}

        {tab === "post" && (
          <section className="pstudio-panel" data-testid="pstudio-post">
            <h2 className="pstudio-panel__title">Post consumption</h2>
            <p className="pstudio-panel__hint">
              Controlled posting. Live SAP queue follows server configuration (currently gated). Confirming calls the existing process API for selected lines only.
            </p>
            <div className="pstudio-steps" aria-label="Posting steps">
              {postSteps.map((step) => (
                <span
                  key={step.key}
                  className="pstudio-step"
                  data-done={step.done ? "true" : "false"}
                  data-active={step.key === "post" && Boolean(preview) ? "true" : "false"}
                >
                  {step.label}
                </span>
              ))}
            </div>
            {!preview ? (
              <EmptyState
                title="Nothing ready to post"
                description="Upload and select valid lines, then open this step from Sales Review."
                action={<button type="button" className="sila-btn sila-btn--primary" onClick={() => setTab("upload")}>Upload sales</button>}
              />
            ) : (
              <>
                <div className="pstudio-kpis">
                  <div className="pstudio-kpi pstudio-kpi--blue"><span>Selected POS lines</span><strong>{selectedIds.length}</strong></div>
                  <div className="pstudio-kpi pstudio-kpi--mint"><span>Ready in batch</span><strong>{preview.readyToProcess}</strong></div>
                  <div className="pstudio-kpi pstudio-kpi--lavender"><span>Total materials</span><strong>{consumption?.lines.reduce((n, line) => n + line.ingredientCount, 0) ?? "Preview first"}</strong></div>
                  <div className="pstudio-kpi pstudio-kpi--sky"><span>Estimated cost</span><strong>{consumption?.totalConsumptionCost ?? "—"}</strong></div>
                  <div className="pstudio-kpi pstudio-kpi--amber"><span>SAP posting profile</span><strong>Server gated</strong></div>
                </div>
                <div className="sila-alert sila-alert--warning" role="status">
                  Confirm posting deducts SILA inventory and queues the SAP material-document posting for the selected lines. Nothing posts automatically from upload or preview.
                </div>
                <div className="srec-actions">
                  <button type="button" className="sila-btn sila-btn--ghost" onClick={() => setTab("preview")}>Back to preview</button>
                  <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setTab("review")}>Back to review</button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    disabled={!canManage || selectedIds.length === 0 || posting || preview.readyToProcess === 0}
                    onClick={() => setConfirmPost(true)}
                  >
                    {posting ? "Posting..." : "Confirm posting"}
                  </button>
                </div>
              </>
            )}
            {confirmPost && preview && (
              <SilaPosConfirm
                heading="Confirm posting"
                content={`Process ${selectedIds.length} selected line${selectedIds.length === 1 ? "" : "s"} from batch ${preview.batchNumber}?`}
                description="SILA inventory will be deducted for matched lines. SAP/ERP queue follows server configuration and stays disabled until authorized."
                confirmText="Confirm posting"
                busy={posting}
                onConfirm={handleConfirmPost}
                onClose={() => setConfirmPost(false)}
              />
            )}
          </section>
        )}

        {tab === "results" && (
          processResult ? (
            <section className="pstudio-panel pstudio-panel--mint" data-testid="pstudio-results">
              <h2 className="pstudio-panel__title">Posting results · {processResult.batchNumber}</h2>
              <div className="pstudio-kpis">
                <div className="pstudio-kpi pstudio-kpi--sky"><span>Selected / accepted</span><strong>{processResult.accepted}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--mint"><span>Processed</span><strong>{processResult.processed}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--rose"><span>Failed</span><strong>{processResult.failed}</strong></div>
                <div className="pstudio-kpi pstudio-kpi--amber"><span>Pending reconciliation (ERP unknown)</span><strong>{processResult.postingUnknown ?? 0}</strong></div>
              </div>
              {(processResult.postingUnknown ?? 0) > 0 && (
                <div className="sila-alert sila-alert--warning" role="status">
                  SAP/ERP outcome is unknown for some lines — not marked fully complete. Use Transaction Tracker / ERP postings to reconcile without reposting.
                </div>
              )}
              <SilaPosImportSummary result={processResult} onClose={() => setProcessResult(null)} />
              <div className="srec-actions">
                <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setTab("tracker")}>Transaction Tracker</button>
                <button type="button" className="sila-btn sila-btn--ghost" onClick={() => setTab("exceptions")}>Exceptions</button>
              </div>
            </section>
          ) : (
            <EmptyState
              title="No posting results yet"
              description="Results appear after you post selected lines or run a POS API sync."
            />
          )
        )}

        {tab === "exceptions" && (
          <SilaPosStudioExceptions
            onOpenApi={() => setTab("api")}
            onOpenTracker={() => setTab("tracker")}
          />
        )}

        {tab === "tracker" && (
          <section className="pstudio-panel" data-testid="pstudio-tracker">
            <h2 className="pstudio-panel__title">Transaction Tracker</h2>
            <p className="pstudio-panel__hint">Existing POS transaction lineage — upload → validate → deduct → ERP post → reconcile.</p>
            <div className="pstudio-lifecycle" aria-label="POS posting journey">
              {[
                ["Uploaded", "done"],
                ["Validated", "done"],
                ["Consumption", "pending"],
                ["Confirmed", "pending"],
                ["SAP Posted", "pending"],
                ["SILA Reconciled", "pending"],
                ["Completed", "pending"],
              ].map(([label, state], index, arr) => (
                <React.Fragment key={label}>
                  <span className="pstudio-lifecycle__node" data-state={state}>{label}</span>
                  {index < arr.length - 1 && <span className="pstudio-lifecycle__sep">→</span>}
                </React.Fragment>
              ))}
            </div>
            <SilaTransactionTracker />
          </section>
        )}

        {tab === "batches" && <SilaPosBatches key={batchesVersion} canManage={canManage} />}
      </div>
    </div>
  );
};

export default SilaPosIntegration;
