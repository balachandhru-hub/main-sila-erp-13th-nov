import React, { useCallback, useEffect, useState } from "react";
import { EmptyState, Loader, Modal, PageHeader, toastService } from "@vosox/shared-ui";
import {
  approveRecipe,
  deactivateRecipe,
  getRecipe,
  rejectRecipe,
  submitRecipe,
  type SilaRecipeDetail,
} from "../../../api/silaMe/silaRecipeApi";
import { formatDateTime } from "../../cart/lineFormat";
import SilaRecipeApprovalTrail from "./SilaRecipeApprovalTrail";
import SilaRecipeCosting from "./SilaRecipeCosting";
import SilaRecipeReadinessPanel from "./SilaRecipeReadinessPanel";
import { formatCost, formatQty, readinessStatusBadgeClass, readinessText, recipeBadgeClass, recipeLabel, versionsSummary } from "./recipeFormat";
import SilaRecipeCostSummary from "./SilaRecipeCostSummary";
import { canStartRevision } from "./recipeWriteModel";
import SilaRecipePicture from "./SilaRecipePicture";
import "./SilaRecipeStudio.css";

interface SilaRecipeDetailViewProps {
  recipeId: string;
  /** Shows edit, submit and deactivate. */
  canManage: boolean;
  onBack: () => void;
  onEdit: (recipe: SilaRecipeDetail) => void;
  /** Called after a status change, so the list can refresh. */
  onChanged: () => void;
}

type Busy = "submit" | "approve" | "reject" | "deactivate" | null;

/**
 * One recipe at a version (latest by default): header, readiness, costing per outlet, approval trail; switch versions,
 * submit, deactivate and the approver's decision.
 */
const SilaRecipeDetailView: React.FC<SilaRecipeDetailViewProps> = ({ recipeId, canManage, onBack, onEdit, onChanged }) => {
  const [recipe, setRecipe] = useState<SilaRecipeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<Busy>(null);
  const [comment, setComment] = useState("");
  const [confirmDeactivate, setConfirmDeactivate] = useState(false);
  /** The version to show; the latest when undefined. */
  const [version, setVersion] = useState<number | undefined>(undefined);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setRecipe(await getRecipe(recipeId, version));
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Could not load the recipe.");
    } finally {
      setLoading(false);
    }
  }, [recipeId, version]);

  useEffect(() => {
    load();
  }, [load]);

  const run = async (kind: Exclude<Busy, null>, action: () => Promise<void>, success: string) => {
    setBusy(kind);
    try {
      await action();
      toastService.success(success);
      setComment("");
      setConfirmDeactivate(false);
      onChanged();
      if (kind === "deactivate") {
        onBack();
        return;
      }
      if (version !== undefined) {
        setVersion(undefined);
      } else {
        await load();
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "The action failed.");
    } finally {
      setBusy(null);
    }
  };

  if (loading && !recipe) {
    return <Loader size={24} message="Loading recipe..." />;
  }

  if (error || !recipe) {
    return (
      <div className="sila-me srec-page">
        <PageHeader className="pud-page-header" title="Recipe" onBack={onBack} />
        <EmptyState
          variant="error"
          title="Couldn't load the recipe"
          description={error ?? undefined}
          action={<button type="button" className="sila-btn sila-btn--secondary" onClick={load}>Try again</button>}
        />
      </div>
    );
  }

  const latest = recipe.viewVersion === recipe.version;
  const editable = canManage && latest && recipe.status !== "PENDING_APPROVAL" && recipe.status !== "INACTIVE";
  const submittable = canManage && latest && recipe.status === "DRAFT";
  const deactivatable = canManage && recipe.status !== "INACTIVE";
  const showReadiness = latest && (recipe.status === "DRAFT" || recipe.status === "REJECTED");
  // Step 1: Edit / Create revision only navigates to the temporary workspace shell (no API mutate).
  const needsRevision = canStartRevision(recipe.status);

  const menuPrice = recipe.outletPrices.length === 0
    ? null
    : Math.min(...recipe.outletPrices.map((price) => price.menuPrice));
  const portionCost = recipe.readiness.costComplete ? (recipe.costPerServing ?? (recipe.servingQty > 0 ? recipe.totalCost / recipe.servingQty : recipe.totalCost)) : null;

  return (
    <div className="sila-me srec-page rstudio-page">
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm rstudio-back" onClick={onBack}>← Back to catalog</button>

      <header className="rstudio-detail-hero">
        <div className="rstudio-detail-hero__cover">
          <SilaRecipePicture
            recipeId={recipe.id}
            preferDraft={recipe.viewVersion === recipe.version && recipe.activeVersion > 0 && recipe.status !== "APPROVED"}
            hasPicture={Boolean(recipe.hasPicture)}
            placeholderTitle={recipe.name}
            placeholderCategory={recipe.categoryName || recipe.category}
            placeholderFamily={recipe.familyName}
            variant="detail"
            locked
          />
        </div>
        <div className="rstudio-detail-hero__copy">
          <p className="rstudio-kicker">{recipe.recipeCode}</p>
          <h1>{recipe.name}</h1>
          <div className="srec-badges">
            <span className={recipeBadgeClass(recipe.viewStatus)}>{recipeLabel(recipe.viewStatus)}</span>
            {latest && recipe.status !== "INACTIVE" && (
              <span className={readinessStatusBadgeClass(recipe.readiness.status, recipe.readiness.issues.length)}>
                {readinessText(recipe.readiness.status, recipe.readiness.issues.length)}
              </span>
            )}
            {recipe.versions.length > 1 ? (
              <select
                className="sila-select srec-version-select"
                aria-label="Version"
                value={recipe.viewVersion}
                onChange={(e) => setVersion(Number(e.target.value))}
              >
                {recipe.versions.map((item) => (
                  <option key={item.version} value={item.version}>
                    V{item.version} ({recipeLabel(item.status)})
                  </option>
                ))}
              </select>
            ) : (
              <span className="sila-badge sila-badge--neutral">V{recipe.viewVersion}</span>
            )}
          </div>
          <p className="rstudio-detail-hero__meta">
            {recipe.familyName || "—"} · {recipe.categoryName || recipe.category || "—"} · {recipeLabel(recipe.itemMode)} · Yield {formatQty(recipe.servingQty)} {recipe.servingUom}
          </p>
          {recipe.description && <p className="rstudio-detail-hero__desc">{recipe.description}</p>}
          <div className="srec-actions">
            {editable && (
              <button type="button" className="sila-btn sila-btn--primary" onClick={() => onEdit(recipe)} disabled={busy !== null}>
                {needsRevision ? `Open workspace (revision v${recipe.version + 1})` : "Open workspace"}
              </button>
            )}
            {submittable && (
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy !== null || !recipe.readiness.ready}
                title={recipe.readiness.ready ? undefined : "Fix the readiness issues first."}
                onClick={() => run("submit", () => submitRecipe(recipe.id), "Recipe sent for approval.")}
              >
                {busy === "submit" ? "Sending..." : "Submit for approval"}
              </button>
            )}
            {deactivatable && (
              <button type="button" className="sila-btn sila-btn--danger" onClick={() => setConfirmDeactivate(true)} disabled={busy !== null}>
                Delete
              </button>
            )}
          </div>
        </div>
      </header>

      <div className="rstudio-kpis">
        <div className="rstudio-kpi rstudio-kpi--blue"><span>Total recipe cost</span><strong>{recipe.readiness.costComplete ? formatCost(recipe.totalCost, recipe.currency) : "COST PENDING"}</strong></div>
        <div className="rstudio-kpi rstudio-kpi--mint"><span>Cost / portion</span><strong>{portionCost == null ? "COST PENDING" : formatCost(portionCost, recipe.currency)}</strong></div>
        <div className="rstudio-kpi rstudio-kpi--lavender"><span>Ingredients</span><strong>{recipe.ingredients.length}</strong></div>
        <div className="rstudio-kpi rstudio-kpi--amber"><span>Yield</span><strong>{formatQty(recipe.servingQty)} {recipe.servingUom}</strong></div>
        <div className="rstudio-kpi rstudio-kpi--sky"><span>Food cost %</span><strong>{portionCost != null && menuPrice ? `${((portionCost / menuPrice) * 100).toFixed(1)}%` : "—"}</strong></div>
        <div className="rstudio-kpi rstudio-kpi--blue"><span>Margin</span><strong>{portionCost != null && menuPrice ? formatCost(menuPrice - portionCost, recipe.currency) : "—"}</strong></div>
      </div>

      <section className="rstudio-cookbook-section">
        <h2>Ingredients</h2>
        <div className="rstudio-ingredient-grid">
          {recipe.ingredients.map((ingredient) => (
            <article key={ingredient.id} className="rstudio-ingredient-card">
              <StudioCover
                compact
                kind={resolveStudioKind(ingredient.itemName, ingredient.itemCode, ingredient.uom)}
                title={ingredient.itemName}
              />
              <div>
                <strong>{ingredient.itemName}</strong>
                <p className="srec-sub">{ingredient.itemCode} · {ingredient.ingredientCode}</p>
                <p>{formatQty(ingredient.quantity)} {ingredient.uom}</p>
                <p className="rstudio-ingredient-card__cost">
                  {ingredient.priceStatus === "MISSING" || ingredient.cost == null
                    ? "COST PENDING"
                    : formatCost(ingredient.cost, ingredient.currency || recipe.currency)}
                </p>
              </div>
            </article>
          ))}
        </div>
      </section>

      <section className="sila-card">
        <div className="sila-card-body">
          <div className="sila-meta-grid">
            {[
              ["Versions", versionsSummary(recipe.versions) || "—"],
              ["Mode", recipeLabel(recipe.itemMode)],
              ["Family", recipe.familyName || "—"],
              ["Category", recipe.categoryName || recipe.category || "—"],
              ["POS code", recipe.posCode || "—"],
              ["POS item", recipe.posItem || "—"],
              ["Last sale date", recipe.lastSaleDate ? recipe.lastSaleDate.slice(0, 10) : "—"],
              ["Serving", `${formatQty(recipe.servingQty)} ${recipe.servingUom}`],
              ["Selling unit", recipe.sellingUom],
              ["Submitted", recipe.submittedOn ? `${formatDateTime(recipe.submittedOn)}${recipe.submittedByName ? ` by ${recipe.submittedByName}` : ""}` : "—"],
              ["Approved", formatDateTime(recipe.approvedOn)],
            ].map(([label, value]) => (
              <div className="sila-meta-item" key={label}>
                <span className="sila-meta-label">{label}</span>
                <span className="sila-meta-value">{value}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {!latest && (
        <div className="sila-alert sila-alert--warning" role="status">
          You are viewing version {recipe.viewVersion}. The latest version is {recipe.version} ({recipeLabel(recipe.status)}).
        </div>
      )}

      {latest && recipe.activeVersion > 0 && recipe.activeVersion !== recipe.version && recipe.status !== "INACTIVE" && (
        <div className="sila-alert sila-alert--warning" role="status">
          POS sales keep using version {recipe.activeVersion} until version {recipe.version} is approved.
        </div>
      )}

      {showReadiness && <SilaRecipeReadinessPanel readiness={recipe.readiness} />}

      {recipe.canDecide && latest && (
        <section className="sila-card">
          <div className="sila-card-header">
            <h2 className="sila-card-title">Your decision</h2>
          </div>
          <div className="sila-card-body srec-stack">
            <div className="sila-field">
              <label className="sila-label" htmlFor="srec-decision-comment">Comment (required to reject)</label>
              <textarea
                id="srec-decision-comment"
                className="sila-textarea"
                rows={2}
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
            </div>
            <div className="srec-actions">
              <button
                type="button"
                className="sila-btn sila-btn--danger"
                disabled={busy !== null || !comment.trim()}
                onClick={() => run("reject", () => rejectRecipe(recipe.id, comment), "Recipe rejected.")}
              >
                {busy === "reject" ? "Rejecting..." : "Reject"}
              </button>
              <button
                type="button"
                className="sila-btn sila-btn--primary"
                disabled={busy !== null}
                onClick={() => run("approve", () => approveRecipe(recipe.id, comment), "Approval recorded.")}
              >
                {busy === "approve" ? "Approving..." : "Approve"}
              </button>
            </div>
          </div>
        </section>
      )}

      <SilaRecipeCostSummary
        totalCost={recipe.readiness.costComplete ? recipe.totalCost : null}
        servingQty={recipe.servingQty}
        servingUom={recipe.servingUom}
        currency={recipe.currency}
        pricedOutlets={recipe.outletPrices.length}
        menuPrice={recipe.outletPrices.length === 0 ? "" : String(Math.min(...recipe.outletPrices.map((price) => price.menuPrice)))}
        menuOutlet={recipe.outletPrices.slice().sort((a, b) => a.menuPrice - b.menuPrice)[0]?.locationName ?? undefined}
        priceStatuses={recipe.ingredients.filter((ingredient) => ingredient.materialId).map((ingredient) => ingredient.priceStatus)}
        breakup={recipe.ingredients.map((ingredient) => ({
          name: `${ingredient.itemCode} ${ingredient.itemName}`.trim(),
          cost: ingredient.cost,
        }))}
      />
      <SilaRecipeCosting recipe={recipe} />
      <SilaRecipeApprovalTrail recipe={recipe} />

      <Modal
        isOpen={confirmDeactivate}
        onClose={() => setConfirmDeactivate(false)}
        variant="danger"
        headerProps={{ heading: `Delete ${recipe.recipeCode}` }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setConfirmDeactivate(false), disabled: busy !== null },
          primaryButton: {
            text: "Delete",
            loading: busy === "deactivate",
            onClick: () => run("deactivate", () => deactivateRecipe(recipe.id), "Recipe deleted (deactivated)."),
          },
        }}
      >
        <p className="sila-modal-text">
          This deletes (deactivates) the recipe. POS sales with POS ID {recipe.posCode || "—"} will no longer be matched to it.
        </p>
      </Modal>
    </div>
  );
};

export default SilaRecipeDetailView;
