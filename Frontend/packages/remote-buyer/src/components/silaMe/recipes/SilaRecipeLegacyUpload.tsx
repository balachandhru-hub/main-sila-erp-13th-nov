import React, { useCallback, useEffect, useMemo, useState } from "react";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  assignLegacyRecipeImages,
  decideLegacyBatch,
  downloadLegacyImageReport,
  downloadLegacyPostingReport,
  downloadLegacyPublishReport,
  downloadLegacyRecipeTemplate,
  downloadLegacyValidationReport,
  getLegacyRecipe,
  getLegacyRecipeBatch,
  listLegacyRecipeBatches,
  listLegacyRecipes,
  postLegacyBatch,
  publishLegacyBatch,
  reprocessLegacyFailed,
  submitLegacyBatchApproval,
  updateLegacyMaterialMaps,
  uploadLegacyRecipeBatch,
  uploadLegacyRecipeImages,
  validateLegacyRecipeBatch,
  type SilaRecipeLegacyBatchDetail,
  type SilaRecipeLegacyBatchListItem,
  type SilaRecipeLegacyRecipeDetail,
  type SilaRecipeLegacyRecipeListItem,
} from "../../../api/silaMe/silaRecipeLegacyApi";
import { silaRecipeCatalogPath, silaRecipeDetailPath } from "./silaRecipeRoutes";
import "../silaMeTheme.css";
import "./SilaRecipes.css";
import "./SilaRecipeStudio.css";
import "./SilaRecipeLegacyUpload.css";

type LegacyTab =
  | "upload"
  | "validation"
  | "approval"
  | "post"
  | "images"
  | "publishing"
  | "history";

const TABS: { key: LegacyTab; label: string }[] = [
  { key: "upload", label: "New Upload" },
  { key: "validation", label: "Validation & Preview" },
  { key: "approval", label: "Pending Approval" },
  { key: "post", label: "Ready to Post" },
  { key: "images", label: "Image Assignment" },
  { key: "publishing", label: "Publishing" },
  { key: "history", label: "Upload History" },
];

interface SilaRecipeLegacyUploadProps {
  canManage: boolean;
  canApprove: boolean;
  onBack: () => void;
  onOpenRecipe?: (recipeId: string) => void;
}

const statusClass = (status: string): string => {
  const key = status.trim().toLowerCase().replace(/\s+/g, "_");
  return `rlegacy-status rlegacy-status--${key}`;
};

const errText = (err: unknown, fallback: string): string =>
  err instanceof Error && err.message ? err.message : fallback;

/**
 * Isolated Legacy Recipe Upload workspace inside Recipe Studio.
 * Additive only — does not alter the creation wizard or catalog layout beyond the entry route.
 */
const SilaRecipeLegacyUpload: React.FC<SilaRecipeLegacyUploadProps> = ({
  canManage,
  canApprove,
  onBack,
  onOpenRecipe,
}) => {
  const [tab, setTab] = useState<LegacyTab>("upload");
  const [busy, setBusy] = useState(false);
  const [batch, setBatch] = useState<SilaRecipeLegacyBatchDetail | null>(null);
  const [history, setHistory] = useState<SilaRecipeLegacyBatchListItem[]>([]);
  const [recipes, setRecipes] = useState<SilaRecipeLegacyRecipeListItem[]>([]);
  const [recipeTotal, setRecipeTotal] = useState(0);
  const [recipePage, setRecipePage] = useState(0);
  const [statusFilter, setStatusFilter] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [detail, setDetail] = useState<SilaRecipeLegacyRecipeDetail | null>(null);
  const [decideComment, setDecideComment] = useState("");
  const [mapMaterialId, setMapMaterialId] = useState("");
  const [mapLine, setMapLine] = useState<number | null>(null);

  const refreshHistory = useCallback(async () => {
    try {
      setHistory(await listLegacyRecipeBatches(0, 50));
    } catch (err: unknown) {
      setHistory([]);
      toastService.error(errText(err, "Could not load upload history."));
    }
  }, []);

  const refreshBatch = useCallback(async (batchId: string) => {
    const next = await getLegacyRecipeBatch(batchId);
    setBatch(next);
    return next;
  }, []);

  const refreshRecipes = useCallback(
    async (batchId: string, index = 0) => {
      const page = await listLegacyRecipes(batchId, {
        status: statusFilter || undefined,
        search: appliedSearch || undefined,
        index,
        limit: 50,
      });
      setRecipes(page.items);
      setRecipeTotal(page.total);
      setRecipePage(page.index);
    },
    [appliedSearch, statusFilter],
  );

  useEffect(() => {
    refreshHistory();
  }, [refreshHistory]);

  useEffect(() => {
    if (!batch) return;
    refreshRecipes(batch.id, 0).catch((err: unknown) => {
      toastService.error(errText(err, "Could not load staged recipes."));
    });
  }, [batch?.id, refreshRecipes]);

  const openBatch = async (batchId: string, nextTab?: LegacyTab) => {
    setBusy(true);
    try {
      await refreshBatch(batchId);
      if (nextTab) setTab(nextTab);
    } catch (err: unknown) {
      toastService.error(errText(err, "Could not open the batch."));
    } finally {
      setBusy(false);
    }
  };

  const onUpload = async (file: File | null) => {
    if (!file || !canManage) return;
    setBusy(true);
    try {
      const uploaded = await uploadLegacyRecipeBatch(file);
      setBatch(uploaded);
      setTab("validation");
      toastService.success(`Uploaded ${uploaded.batchNumber}.`);
      await refreshHistory();
      await validateLegacyRecipeBatch(uploaded.id).then(async (validated) => {
        setBatch(validated);
        await refreshRecipes(validated.id, 0);
      });
    } catch (err: unknown) {
      toastService.error(errText(err, "Upload failed."));
    } finally {
      setBusy(false);
    }
  };

  const runAction = async (label: string, action: () => Promise<unknown>, nextTab?: LegacyTab) => {
    if (!batch) return;
    setBusy(true);
    try {
      await action();
      await refreshBatch(batch.id);
      await refreshRecipes(batch.id, recipePage);
      await refreshHistory();
      if (nextTab) setTab(nextTab);
      toastService.success(label);
    } catch (err: unknown) {
      toastService.error(errText(err, label));
    } finally {
      setBusy(false);
    }
  };

  const openDetail = async (row: SilaRecipeLegacyRecipeListItem) => {
    if (!batch) return;
    setBusy(true);
    try {
      setDetail(await getLegacyRecipe(batch.id, row.id));
    } catch (err: unknown) {
      toastService.error(errText(err, "Could not open recipe preview."));
    } finally {
      setBusy(false);
    }
  };

  const saveMapping = async () => {
    if (!batch || !detail || mapLine == null || !mapMaterialId.trim()) return;
    setBusy(true);
    try {
      await updateLegacyMaterialMaps(batch.id, [
        {
          legacyRecipeId: detail.legacyRecipeId,
          ingredientLine: mapLine,
          mappedMaterialId: mapMaterialId.trim(),
          status: "MAPPED",
        },
      ]);
      await validateLegacyRecipeBatch(batch.id);
      await refreshBatch(batch.id);
      setDetail(await getLegacyRecipe(batch.id, detail.id));
      setMapMaterialId("");
      setMapLine(null);
      toastService.success("Material mapping saved. Batch revalidated.");
    } catch (err: unknown) {
      toastService.error(errText(err, "Could not save mapping."));
    } finally {
      setBusy(false);
    }
  };

  const summary = useMemo(() => batch, [batch]);

  const recipeTable = (
    <div className="rlegacy-table-wrap">
      <table className="rlegacy-table">
        <thead>
          <tr>
            <th>Legacy ID</th>
            <th>Name</th>
            <th>Category</th>
            <th>Format</th>
            <th>Validation</th>
            <th>Post</th>
            <th>Image</th>
            <th>Publish</th>
            <th>SILA ID</th>
          </tr>
        </thead>
        <tbody>
          {recipes.map((row) => (
            <tr key={row.id} className="rlegacy-history-row" onClick={() => openDetail(row)}>
              <td>{row.legacyRecipeId}</td>
              <td>{row.recipeName}</td>
              <td>{row.category || "—"}</td>
              <td>{row.recipeFormat || "—"}</td>
              <td>
                <span className={statusClass(row.validationStatus)}>
                  {row.validationStatus}
                  {row.errorCount ? ` · ${row.errorCount}E` : ""}
                  {row.warningCount ? ` · ${row.warningCount}W` : ""}
                </span>
              </td>
              <td>
                <span className={statusClass(row.postingStatus)}>{row.postingStatus}</span>
              </td>
              <td>
                <span className={statusClass(row.imageStatus || "—")}>{row.imageStatus || "—"}</span>
              </td>
              <td>
                <span className={statusClass(row.publishStatus || "—")}>{row.publishStatus || "—"}</span>
              </td>
              <td>
                {row.createdRecipeId && row.createdRecipeCode ? (
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={(event) => {
                      event.stopPropagation();
                      onOpenRecipe?.(row.createdRecipeId!);
                    }}
                  >
                    {row.createdRecipeCode}
                  </button>
                ) : (
                  "—"
                )}
              </td>
            </tr>
          ))}
          {recipes.length === 0 ? (
            <tr>
              <td colSpan={9}>No staged recipes for this filter.</td>
            </tr>
          ) : null}
        </tbody>
      </table>
    </div>
  );

  return (
    <div className="sila-me srec-page rstudio-page rlegacy-page" data-testid="recipe-legacy-upload">
      <header className="rlegacy-hero">
        <h1 className="rlegacy-hero__title">Legacy Recipe Upload</h1>
        <div className="rlegacy-hero__actions">
          <button type="button" className="sila-btn sila-btn--secondary" onClick={onBack}>
            Back to catalog
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={() => downloadLegacyRecipeTemplate().catch((err) => toastService.error(errText(err, "Template download failed.")))}
          >
            Download template
          </button>
        </div>
      </header>

      <nav className="rlegacy-tabs" role="tablist" aria-label="Legacy Recipe Upload">
        {TABS.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            className="rlegacy-tab"
            aria-selected={tab === item.key}
            onClick={() => setTab(item.key)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      {busy ? <Loader /> : null}

      {tab === "upload" && (
        <section className="rlegacy-panel">
          <h2 className="rlegacy-panel__title">New Upload</h2>
          <p className="rlegacy-panel__hint">
            Upload the recipe workbook (.xlsx, sheets Recipes and OutletPrices). Rows are staged as they are and checked in the Validate step — no Recipe IDs are created until approved posting.
          </p>
          <div className="rlegacy-drop">
            <input
              type="file"
              accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
              disabled={!canManage || busy}
              onChange={(event) => onUpload(event.target.files?.[0] ?? null)}
            />
            {!canManage ? <span>Requires MANAGE_SILA_RECIPE.</span> : null}
          </div>
        </section>
      )}

      {tab === "history" && (
        <section className="rlegacy-panel">
          <h2 className="rlegacy-panel__title">Upload History</h2>
          <p className="rlegacy-panel__hint">Open a batch to continue validation, approval, posting, images, or publishing.</p>
          <div className="rlegacy-table-wrap">
            <table className="rlegacy-table">
              <thead>
                <tr>
                  <th>Batch</th>
                  <th>File</th>
                  <th>Status</th>
                  <th>Recipes</th>
                  <th>Valid</th>
                  <th>Errors</th>
                  <th>Posted</th>
                  <th>Published</th>
                  <th>Uploaded</th>
                </tr>
              </thead>
              <tbody>
                {history.map((row) => (
                  <tr
                    key={row.id}
                    className="rlegacy-history-row"
                    tabIndex={0}
                    onClick={() => openBatch(row.id, "validation")}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") openBatch(row.id, "validation");
                    }}
                  >
                    <td>{row.batchNumber}</td>
                    <td>{row.fileName}</td>
                    <td>
                      <span className={statusClass(row.status)}>{row.status}</span>
                    </td>
                    <td>{row.recipeCount}</td>
                    <td>{row.validCount}</td>
                    <td>{row.errorCount}</td>
                    <td>{row.postedCount}</td>
                    <td>{row.publishedCount}</td>
                    <td>{new Date(row.dateCreated).toLocaleString()}</td>
                  </tr>
                ))}
                {history.length === 0 ? (
                  <tr>
                    <td colSpan={9}>
                      <EmptyState title="No uploads yet" description="Start with New Upload and the recipe template." />
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {tab !== "upload" && tab !== "history" && !batch ? (
        <section className="rlegacy-panel">
          <EmptyState
            title="Select a batch"
            description="Upload a workbook or open a row from Upload History."
            action={
              <button type="button" className="sila-btn sila-btn--primary" onClick={() => setTab("history")}>
                Open history
              </button>
            }
          />
        </section>
      ) : null}

      {batch && tab !== "upload" && tab !== "history" ? (
        <section className="rlegacy-panel">
          <h2 className="rlegacy-panel__title">
            {batch.batchNumber} · <span className={statusClass(batch.status)}>{batch.status}</span>
          </h2>
          <p className="rlegacy-panel__hint">
            {batch.fileName} · revision {batch.revision}
            {summary?.approvalComment ? ` · ${summary.approvalComment}` : ""}
          </p>

          <div className="rlegacy-kpis" aria-label="Batch summary">
            <div className="rlegacy-kpi">
              <span>Recipes</span>
              <strong>{batch.recipeCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Valid</span>
              <strong>{batch.validCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Warnings</span>
              <strong>{batch.warningCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Errors</span>
              <strong>{batch.errorCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Unmapped</span>
              <strong>{batch.unmappedCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Duplicates</span>
              <strong>{batch.duplicateCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Posted</span>
              <strong>{batch.postedCount}</strong>
            </div>
            <div className="rlegacy-kpi">
              <span>Published</span>
              <strong>{batch.publishedCount}</strong>
            </div>
          </div>

          <div className="rlegacy-toolbar">
            <input
              type="search"
              placeholder="Search legacy id or name"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
            />
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setAppliedSearch(search.trim())}>
              Search
            </button>
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="">All validation</option>
              <option value="VALID">VALID</option>
              <option value="WARNING">WARNING</option>
              <option value="INVALID">INVALID</option>
              <option value="DUPLICATE">DUPLICATE</option>
              <option value="UNMAPPED">UNMAPPED</option>
            </select>
            {canManage ? (
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                onClick={() => runAction("Validated.", () => validateLegacyRecipeBatch(batch.id))}
              >
                Revalidate
              </button>
            ) : null}
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              onClick={() => downloadLegacyValidationReport(batch.id).catch((err) => toastService.error(errText(err, "Report failed.")))}
            >
              Validation report
            </button>
          </div>

          {tab === "validation" && (
            <>
              {canManage ? (
                <div className="rlegacy-toolbar">
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    disabled={batch.errorCount > 0 && batch.validCount === 0}
                    onClick={() =>
                      runAction("Submitted for approval.", () => submitLegacyBatchApproval(batch.id), "approval")
                    }
                  >
                    Submit for approval
                  </button>
                </div>
              ) : null}
              {recipeTable}
            </>
          )}

          {tab === "approval" && (
            <>
              {canApprove ? (
                <div className="rlegacy-toolbar">
                  <input
                    type="text"
                    placeholder="Decision comment (required for reject/return)"
                    value={decideComment}
                    onChange={(event) => setDecideComment(event.target.value)}
                  />
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    onClick={() =>
                      runAction("Approved.", () => decideLegacyBatch(batch.id, "approve", decideComment), "post")
                    }
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() =>
                      runAction("Rejected.", () => decideLegacyBatch(batch.id, "reject", decideComment), "history")
                    }
                  >
                    Reject
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() =>
                      runAction("Returned for correction.", () => decideLegacyBatch(batch.id, "return", decideComment), "validation")
                    }
                  >
                    Return for correction
                  </button>
                </div>
              ) : (
                <p className="rlegacy-panel__hint">Requires APPROVE_SILA_RECIPE to decide.</p>
              )}
              {recipeTable}
            </>
          )}

          {tab === "post" && (
            <>
              {canManage ? (
                <div className="rlegacy-toolbar">
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    onClick={() => runAction("Posting complete.", () => postLegacyBatch(batch.id), "images")}
                  >
                    Post recipes
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() => runAction("Reprocessed failed rows.", () => reprocessLegacyFailed(batch.id))}
                  >
                    Reprocess failed
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() =>
                      downloadLegacyPostingReport(batch.id).catch((err) => toastService.error(errText(err, "Report failed.")))
                    }
                  >
                    Posting report
                  </button>
                </div>
              ) : null}
              {recipeTable}
            </>
          )}

          {tab === "images" && (
            <>
              {canManage ? (
                <div className="rlegacy-toolbar">
                  <input
                    type="file"
                    accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
                    multiple
                    disabled={busy}
                    onChange={(event) => {
                      const files = Array.from(event.target.files ?? []);
                      if (!files.length) return;
                      runAction("Images uploaded.", () => uploadLegacyRecipeImages(batch.id, files));
                    }}
                  />
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    onClick={() => runAction("Images assigned.", () => assignLegacyRecipeImages(batch.id))}
                  >
                    Assign images
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() =>
                      downloadLegacyImageReport(batch.id).catch((err) => toastService.error(errText(err, "Report failed.")))
                    }
                  >
                    Image report
                  </button>
                </div>
              ) : null}
              <p className="rlegacy-panel__hint">
                Name files after the created Recipe Code (e.g. RI00000001.jpg). Existing primary images are not overwritten
                without replace confirmation.
              </p>
              {recipeTable}
            </>
          )}

          {tab === "publishing" && (
            <>
              {canApprove ? (
                <div className="rlegacy-toolbar">
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    onClick={() => runAction("Publishing complete.", () => publishLegacyBatch(batch.id))}
                  >
                    Publish eligible recipes
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() =>
                      downloadLegacyPublishReport(batch.id).catch((err) => toastService.error(errText(err, "Report failed.")))
                    }
                  >
                    Publish report
                  </button>
                </div>
              ) : (
                <p className="rlegacy-panel__hint">Publishing finalizes ActiveVersion and requires APPROVE_SILA_RECIPE.</p>
              )}
              {recipeTable}
            </>
          )}

          {detail ? (
            <div className="rlegacy-detail" data-testid="recipe-legacy-detail">
              <div className="rlegacy-toolbar">
                <strong>
                  {detail.legacyRecipeId} — {detail.recipeName}
                </strong>
                <button type="button" className="sila-btn sila-btn--secondary" onClick={() => setDetail(null)}>
                  Close preview
                </button>
                {detail.createdRecipeId ? (
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    onClick={() => onOpenRecipe?.(detail.createdRecipeId!)}
                  >
                    Open in Recipe Studio
                  </button>
                ) : null}
              </div>
              <p className="rlegacy-panel__hint">
                {detail.productClass || "—"} · {detail.recipeFormat || "—"} · yield {detail.yieldQty ?? "—"}{" "}
                {detail.yieldUom || ""} · portion {detail.portionQty ?? "—"} {detail.portionUom || ""}
              </p>
              {detail.findingsJson ? (
                <>
                  <div>Findings</div>
                  <pre>{detail.findingsJson}</pre>
                </>
              ) : null}
              {detail.ingredientsJson ? (
                <>
                  <div>Ingredients</div>
                  <pre>{detail.ingredientsJson}</pre>
                </>
              ) : null}
              {detail.instructionsJson ? (
                <>
                  <div>Instructions</div>
                  <pre>{detail.instructionsJson}</pre>
                </>
              ) : null}
              {canManage && (detail.validationStatus === "UNMAPPED" || detail.materialMaps.some((m) => m.status === "UNMAPPED")) ? (
                <div className="rlegacy-toolbar">
                  <input
                    type="number"
                    placeholder="Ingredient line"
                    value={mapLine ?? ""}
                    onChange={(event) => setMapLine(event.target.value ? Number(event.target.value) : null)}
                  />
                  <input
                    type="text"
                    placeholder="Mapped Material Guid"
                    value={mapMaterialId}
                    onChange={(event) => setMapMaterialId(event.target.value)}
                  />
                  <button type="button" className="sila-btn sila-btn--primary" onClick={saveMapping}>
                    Save material map
                  </button>
                </div>
              ) : null}
              {detail.materialMaps?.length ? (
                <>
                  <div>Material maps</div>
                  <pre>{JSON.stringify(detail.materialMaps, null, 2)}</pre>
                </>
              ) : null}
            </div>
          ) : null}

          {recipeTotal > recipes.length ? (
            <div className="rlegacy-toolbar">
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={recipePage <= 0}
                onClick={() => refreshRecipes(batch.id, Math.max(0, recipePage - 1))}
              >
                Previous
              </button>
              <span>
                Page {recipePage + 1} · {recipeTotal} recipes
              </span>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={(recipePage + 1) * 50 >= recipeTotal}
                onClick={() => refreshRecipes(batch.id, recipePage + 1)}
              >
                Next
              </button>
            </div>
          ) : null}
        </section>
      ) : null}
    </div>
  );
};

export default SilaRecipeLegacyUpload;

/** Helpers exported for route wiring without pulling React. */
export const legacyUploadCatalogHint = (): string => silaRecipeCatalogPath();
export const legacyCreatedRecipePath = (recipeId: string): string => silaRecipeDetailPath(recipeId);
