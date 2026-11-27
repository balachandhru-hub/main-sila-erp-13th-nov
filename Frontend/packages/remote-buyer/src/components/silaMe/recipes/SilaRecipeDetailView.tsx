import React, { useCallback, useEffect, useMemo, useState } from "react";
import { ArrowLeft, DollarSign, Layers, Percent, Scale, ShieldCheck, Utensils } from "lucide-react";
import { EmptyState, Loader, Modal, PageHeader, toastService } from "@vosox/shared-ui";
import {
  approveRecipe,
  deleteInactiveRecipe,
  deleteRecipeDraft,
  getRecipe,
  rejectRecipe,
  submitRecipe,
  type SilaRecipeDetail,
} from "../../../api/silaMe/silaRecipeApi";
import { formatDateTime } from "../../cart/lineFormat";
import SilaRecipeApprovalTrail from "./SilaRecipeApprovalTrail";
import SilaRecipeCosting from "./SilaRecipeCosting";
import SilaRecipeReadinessPanel from "./SilaRecipeReadinessPanel";
import {
  formatCost,
  formatQty,
  readinessStatusBadgeClass,
  readinessText,
  recipeBadgeClass,
  recipeLabel,
  versionsSummary,
} from "./recipeFormat";
import SilaRecipeCostSummary from "./SilaRecipeCostSummary";
import { canStartRevision } from "./recipeWriteModel";
import SilaRecipePicture from "./SilaRecipePicture";
import {
  canDeleteDraftRecipe,
  DELETE_DRAFT_BODY,
  DELETE_DRAFT_CONFIRM_LABEL,
  DELETE_DRAFT_SUCCESS,
  DELETE_DRAFT_TITLE,
} from "./recipeStudioDraftDelete";
import {
  canDeleteInactiveRecipe,
  DELETE_INACTIVE_BODY,
  DELETE_INACTIVE_CONFIRM_LABEL,
  DELETE_INACTIVE_SUCCESS,
  DELETE_INACTIVE_TITLE,
} from "./recipeInactiveDelete";
import { resolveRecipePos } from "./recipeStudioEditAccess";
import { resolveStudioKind, StudioCover } from "./recipeStudioVisuals";
import {
  AIInsightCard,
  StudioEmptyState,
  StudioKPIBar,
  StudioKPICard,
  StudioShell,
  StudioSubNavigation,
  type StudioSubNavItem,
} from "../studio";
import "./SilaRecipeStudio.css";

interface SilaRecipeDetailViewProps {
  recipeId: string;
  canManage: boolean;
  onBack: () => void;
  onEdit: (recipe: SilaRecipeDetail) => void;
  onChanged: () => void;
}

type Busy = "submit" | "approve" | "reject" | "delete-draft" | "delete-inactive" | null;
type DetailTabId =
  | "overview"
  | "ingredients"
  | "preparation"
  | "sub-recipes"
  | "cost-breakdown"
  | "ai-intelligence"
  | "version-history"
  | "approvals";

const SilaRecipeDetailView: React.FC<SilaRecipeDetailViewProps> = ({
  recipeId,
  canManage,
  onBack,
  onEdit,
  onChanged,
}) => {
  const [recipe, setRecipe] = useState<SilaRecipeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState<Busy>(null);
  const [comment, setComment] = useState("");
  const [confirmDeleteDraft, setConfirmDeleteDraft] = useState(false);
  const [confirmDeleteInactive, setConfirmDeleteInactive] = useState(false);
  const [version, setVersion] = useState<number | undefined>(undefined);
  const [activeTab, setActiveTab] = useState<DetailTabId>("overview");

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
      setConfirmDeleteDraft(false);
      setConfirmDeleteInactive(false);
      onChanged();
      if (kind === "delete-draft" || kind === "delete-inactive") {
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

  const subRecipes = useMemo(
    () => (recipe ? recipe.ingredients.filter((ing) => Boolean(ing.subRecipeId)) : []),
    [recipe],
  );

  if (loading && !recipe) {
    return <Loader size={24} message="Loading recipe..." />;
  }

  if (error || !recipe) {
    return (
      <StudioShell className="sila-me srec-page">
        <PageHeader className="pud-page-header" title="Recipe" onBack={onBack} />
        <EmptyState
          variant="error"
          title="Couldn't load the recipe"
          description={error ?? undefined}
          action={
            <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={load}>
              Try again
            </button>
          }
        />
      </StudioShell>
    );
  }

  const latest = recipe.viewVersion === recipe.version;
  const editable = canManage && latest && recipe.status !== "PENDING_APPROVAL" && recipe.status !== "INACTIVE";
  const submittable = canManage && latest && recipe.status === "DRAFT";
  const draftDeletable = canManage && latest && canDeleteDraftRecipe(recipe.status);
  const inactiveDeletable = canManage && latest && canDeleteInactiveRecipe(recipe.status);
  const showReadiness = latest && (recipe.status === "DRAFT" || recipe.status === "REJECTED");
  const needsRevision = canStartRevision(recipe.status);

  const menuPrice =
    recipe.outletPrices.length === 0 ? null : Math.min(...recipe.outletPrices.map((price) => price.menuPrice));
  const portionCost = recipe.readiness.costComplete
    ? (recipe.costPerServing ?? (recipe.servingQty > 0 ? recipe.totalCost / recipe.servingQty : recipe.totalCost))
    : null;
  const pos = resolveRecipePos(recipe);
  const foodCostPct = portionCost != null && menuPrice ? (portionCost / menuPrice) * 100 : null;
  const missingPriceLines = recipe.ingredients.filter(
    (ing) => ing.materialId && (ing.priceStatus === "MISSING" || ing.cost == null),
  );

  const tabs: StudioSubNavItem[] = [
    { id: "overview", label: "Overview" },
    { id: "ingredients", label: `Ingredients (${recipe.ingredients.length})` },
    { id: "preparation", label: "Preparation" },
    { id: "sub-recipes", label: `Sub-Recipes (${subRecipes.length})` },
    { id: "cost-breakdown", label: "Cost Breakdown" },
    { id: "ai-intelligence", label: "AI Intelligence" },
    { id: "version-history", label: `Version History (${recipe.versions.length})` },
    {
      id: "approvals",
      label: "Approvals",
      statusBadge: recipe.canDecide ? "Action Required" : undefined,
      statusTone: recipe.canDecide ? "warning" : undefined,
    },
  ];

  return (
    <StudioShell className="sila-me srec-page rstudio-page">
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "0.75rem" }}>
        <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={onBack}>
          <ArrowLeft size={14} strokeWidth={2} aria-hidden="true" />
          <span>Back to Recipe Master</span>
        </button>
      </div>

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
                className="sila-studio-select"
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
              <span className="sila-studio-badge sila-studio-badge--neutral">V{recipe.viewVersion}</span>
            )}
          </div>
          <p className="rstudio-detail-hero__meta">
            {recipe.familyName || "—"} · {recipe.categoryName || recipe.category || "—"} · {recipeLabel(recipe.itemMode)} · Yield{" "}
            <span className="sila-tabular">
              {formatQty(recipe.servingQty)} {recipe.servingUom}
            </span>
          </p>
          {recipe.description && <p className="rstudio-detail-hero__desc">{recipe.description}</p>}
          <div className="srec-actions">
            {editable && (
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--primary"
                onClick={() => onEdit(recipe)}
                disabled={busy !== null}
              >
                {needsRevision ? `Edit Recipe (Revision v${recipe.version + 1})` : "Edit Recipe"}
              </button>
            )}
            {submittable && (
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--secondary"
                disabled={busy !== null || !recipe.readiness.ready}
                title={recipe.readiness.ready ? undefined : "Fix the readiness issues first."}
                onClick={() => run("submit", () => submitRecipe(recipe.id), "Recipe sent for approval.")}
              >
                {busy === "submit" ? "Sending..." : "Submit for Approval"}
              </button>
            )}
            {draftDeletable && (
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--secondary"
                style={{ color: "#c24141", borderColor: "#f3cccc" }}
                onClick={() => setConfirmDeleteDraft(true)}
                disabled={busy !== null}
                data-testid="recipe-detail-delete-draft"
              >
                Delete Draft
              </button>
            )}
            {inactiveDeletable && (
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--secondary"
                style={{ color: "#c24141", borderColor: "#f3cccc" }}
                onClick={() => setConfirmDeleteInactive(true)}
                disabled={busy !== null}
                data-testid="recipe-detail-delete-inactive"
              >
                Delete Permanently
              </button>
            )}
          </div>
        </div>
      </header>

      <StudioKPIBar ariaLabel="Recipe key metrics">
        <StudioKPICard
          label="Total Recipe Cost"
          value={recipe.readiness.costComplete ? formatCost(recipe.totalCost, recipe.currency) : "COST PENDING"}
          tone="blue"
          icon={<DollarSign size={15} />}
        />
        <StudioKPICard
          label="Cost / Portion"
          value={portionCost == null ? "COST PENDING" : formatCost(portionCost, recipe.currency)}
          tone="green"
          icon={<Scale size={15} />}
        />
        <StudioKPICard
          label="Ingredients"
          value={recipe.ingredients.length}
          tone="lavender"
          icon={<Utensils size={15} />}
        />
        <StudioKPICard
          label="Yield"
          value={`${formatQty(recipe.servingQty)} ${recipe.servingUom}`}
          tone="amber"
          icon={<Layers size={15} />}
        />
        <StudioKPICard
          label="Food Cost %"
          value={foodCostPct != null ? `${foodCostPct.toFixed(1)}%` : "—"}
          tone="sky"
          icon={<Percent size={15} />}
        />
        <StudioKPICard
          label="Margin"
          value={portionCost != null && menuPrice ? formatCost(menuPrice - portionCost, recipe.currency) : "—"}
          tone="indigo"
          icon={<ShieldCheck size={15} />}
        />
      </StudioKPIBar>

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

      <StudioSubNavigation
        ariaLabel="Recipe detail sections"
        items={tabs}
        activeId={activeTab}
        onSelect={(id: string) => setActiveTab(id as DetailTabId)}
      />

      {activeTab === "overview" && (
        <section className="sila-studio-card">
          <div className="sila-studio-card__header">
            <h2 className="sila-studio-card__title">Recipe Summary &amp; Operational Metadata</h2>
          </div>
          <div className="sila-studio-card__body">
            <div className="sila-meta-grid">
              {[
                ["Versions", versionsSummary(recipe.versions) || "—"],
                ["Mode", recipeLabel(recipe.itemMode)],
                ["Family", recipe.familyName || "—"],
                ["Category", recipe.categoryName || recipe.category || "—"],
                ["POS Code", pos.posCode || "—"],
                ["POS Item", pos.posItem || "—"],
                ["Last Sale Date", recipe.lastSaleDate ? recipe.lastSaleDate.slice(0, 10) : "—"],
                ["Serving Yield", `${formatQty(recipe.servingQty)} ${recipe.servingUom}`],
                ["Selling Unit", recipe.sellingUom],
                [
                  "Submitted",
                  recipe.submittedOn
                    ? `${formatDateTime(recipe.submittedOn)}${recipe.submittedByName ? ` by ${recipe.submittedByName}` : ""}`
                    : "—",
                ],
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
      )}

      {activeTab === "ingredients" && (
        <section className="rstudio-cookbook-section">
          <h2>Ingredients ({recipe.ingredients.length})</h2>
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
                  <p className="srec-sub">
                    {ingredient.itemCode} · {ingredient.ingredientCode}
                  </p>
                  <p className="sila-tabular">
                    {formatQty(ingredient.quantity)} {ingredient.uom}
                  </p>
                  <p className="rstudio-ingredient-card__cost sila-tabular">
                    {ingredient.priceStatus === "MISSING" || ingredient.cost == null
                      ? "COST PENDING"
                      : formatCost(ingredient.cost, ingredient.currency || recipe.currency)}
                  </p>
                </div>
              </article>
            ))}
          </div>
        </section>
      )}

      {activeTab === "preparation" && (
        <section className="sila-studio-card">
          <div className="sila-studio-card__header">
            <h2 className="sila-studio-card__title">Preparation Method &amp; Kitchen Parameters</h2>
          </div>
          <div className="sila-studio-card__body srec-stack">
            <div className="sila-meta-grid">
              {[
                ["Prep Time", recipe.prepTimeMinutes != null ? `${recipe.prepTimeMinutes} min` : "—"],
                ["Cook Time", recipe.cookTimeMinutes != null ? `${recipe.cookTimeMinutes} min` : "—"],
                ["Trim Loss %", recipe.trimLossPercent != null ? `${recipe.trimLossPercent}%` : "—"],
                ["Cooking Loss %", recipe.cookingLossPercent != null ? `${recipe.cookingLossPercent}%` : "—"],
                ["Waste Factor %", recipe.wasteFactorPercent != null ? `${recipe.wasteFactorPercent}%` : "—"],
              ].map(([label, value]) => (
                <div className="sila-meta-item" key={label}>
                  <span className="sila-meta-label">{label}</span>
                  <span className="sila-meta-value sila-tabular">{value}</span>
                </div>
              ))}
            </div>
            {recipe.preparationSteps ? (
              <div style={{ whiteSpace: "pre-wrap", color: "#123354", fontSize: "0.875rem", lineHeight: 1.55 }}>
                {recipe.preparationSteps}
              </div>
            ) : (
              <StudioEmptyState
                title="No preparation instructions recorded"
                description="Add prep steps, prep/cook times, or yield loss factors in Edit Recipe."
              />
            )}
          </div>
        </section>
      )}

      {activeTab === "sub-recipes" && (
        <section className="sila-studio-card">
          <div className="sila-studio-card__header">
            <h2 className="sila-studio-card__title">Linked Sub-Recipes &amp; Batch Components</h2>
          </div>
          <div className="sila-studio-card__body">
            {subRecipes.length === 0 ? (
              <StudioEmptyState
                title="No sub-recipes linked"
                description="This recipe currently uses raw material lines only."
              />
            ) : (
              <div className="rstudio-ingredient-grid">
                {subRecipes.map((sub) => (
                  <article key={sub.id} className="rstudio-ingredient-card">
                    <StudioCover compact kind="main" title={sub.itemName} />
                    <div>
                      <strong>{sub.itemName}</strong>
                      <p className="srec-sub">
                        {sub.itemCode} · {sub.ingredientCode}
                      </p>
                      <p className="sila-tabular">
                        {formatQty(sub.quantity)} {sub.uom}
                      </p>
                      <p className="rstudio-ingredient-card__cost sila-tabular">
                        {sub.cost != null ? formatCost(sub.cost, sub.currency || recipe.currency) : "COST PENDING"}
                      </p>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </div>
        </section>
      )}

      {activeTab === "cost-breakdown" && (
        <div className="srec-stack">
          <SilaRecipeCostSummary
            totalCost={recipe.readiness.costComplete ? recipe.totalCost : null}
            servingQty={recipe.servingQty}
            servingUom={recipe.servingUom}
            currency={recipe.currency}
            pricedOutlets={recipe.outletPrices.length}
            menuPrice={
              recipe.outletPrices.length === 0
                ? ""
                : String(Math.min(...recipe.outletPrices.map((price) => price.menuPrice)))
            }
            menuOutlet={
              recipe.outletPrices.slice().sort((a, b) => a.menuPrice - b.menuPrice)[0]?.locationName ?? undefined
            }
            priceStatuses={recipe.ingredients
              .filter((ingredient) => ingredient.materialId)
              .map((ingredient) => ingredient.priceStatus)}
            breakup={recipe.ingredients.map((ingredient) => ({
              name: `${ingredient.itemCode} ${ingredient.itemName}`.trim(),
              cost: ingredient.cost,
            }))}
          />
          <SilaRecipeCosting recipe={recipe} />
        </div>
      )}

      {activeTab === "ai-intelligence" && (
        <div className="sila-studio-grid-2">
          <AIInsightCard
            title="Recipe Cost &amp; Readiness Diagnostics"
            kind="Analysis"
            summary={
              recipe.readiness.ready
                ? `All ${recipe.ingredients.length} ingredient lines are priced and recipe readiness checks pass.`
                : `${recipe.readiness.issues.length} readiness issue(s) require resolution before approval.`
            }
            evidence={[
              `Cost completeness: ${recipe.readiness.costComplete ? "Complete" : "Incomplete"}`,
              `Missing material prices: ${missingPriceLines.length} line(s)`,
              `Priced outlets: ${recipe.outletPrices.length}`,
            ]}
            impact={
              portionCost != null
                ? `Current cost per portion: ${formatCost(portionCost, recipe.currency)}`
                : "Cost pending material pricing"
            }
            action={
              editable ? (
                <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={() => onEdit(recipe)}>
                  Open in Recipe Workspace
                </button>
              ) : undefined
            }
          />
          <AIInsightCard
            title="Margin &amp; POS Benchmark"
            kind={foodCostPct != null && foodCostPct > 35 ? "Risk" : "Prediction"}
            summary={
              foodCostPct != null
                ? `Lowest outlet selling price yields ${foodCostPct.toFixed(1)}% food cost across ${recipe.outletPrices.length} outlet(s).`
                : "Assign at least one outlet menu price to evaluate live contribution margin and food cost percentage."
            }
            evidence={[
              `POS Code: ${pos.posCode || "Unmapped"}`,
              `POS Item: ${pos.posItem || "Unmapped"}`,
              `Last POS Sale: ${recipe.lastSaleDate ? recipe.lastSaleDate.slice(0, 10) : "No recent sales recorded"}`,
            ]}
            impact={
              portionCost != null && menuPrice
                ? `Unit contribution: ${formatCost(menuPrice - portionCost, recipe.currency)}`
                : undefined
            }
          />
        </div>
      )}

      {activeTab === "version-history" && (
        <section className="sila-studio-card">
          <div className="sila-studio-card__header">
            <h2 className="sila-studio-card__title">Version &amp; Activity History</h2>
          </div>
          <div className="sila-studio-card__body srec-stack">
            <div className="sila-studio-table-wrap">
              <table className="sila-studio-table">
                <thead>
                  <tr>
                    <th scope="col">Version</th>
                    <th scope="col">Status</th>
                    <th scope="col">Action</th>
                  </tr>
                </thead>
                <tbody>
                  {recipe.versions.map((ver) => (
                    <tr key={ver.version}>
                      <td className="sila-tabular">V{ver.version}</td>
                      <td>
                        <span className={recipeBadgeClass(ver.status)}>{recipeLabel(ver.status)}</span>
                      </td>
                      <td>
                        <button
                          type="button"
                          className="sila-studio-btn sila-studio-btn--secondary"
                          disabled={ver.version === recipe.viewVersion}
                          onClick={() => setVersion(ver.version)}
                        >
                          {ver.version === recipe.viewVersion ? "Viewing" : "Inspect Version"}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            {recipe.history && recipe.history.length > 0 && (
              <div className="sila-studio-table-wrap">
                <table className="sila-studio-table">
                  <thead>
                    <tr>
                      <th scope="col">Event</th>
                      <th scope="col">Actor</th>
                      <th scope="col">Timestamp</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recipe.history.map((ev, idx) => (
                      <tr key={`${ev.action}-${ev.dateCreated}-${idx}`}>
                        <td>{ev.action}</td>
                        <td>{ev.actorName || "—"}</td>
                        <td className="sila-tabular">{formatDateTime(ev.dateCreated)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </section>
      )}

      {activeTab === "approvals" && (
        <div className="srec-stack">
          {recipe.canDecide && latest && (
            <section className="sila-studio-card">
              <div className="sila-studio-card__header">
                <h2 className="sila-studio-card__title">Approval Decision</h2>
              </div>
              <div className="sila-studio-card__body srec-stack">
                <div className="sila-field">
                  <label className="sila-label" htmlFor="srec-decision-comment">
                    Comment (required to reject)
                  </label>
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
                    className="sila-studio-btn sila-studio-btn--secondary"
                    style={{ color: "#c24141", borderColor: "#f3cccc" }}
                    disabled={busy !== null || !comment.trim()}
                    onClick={() => run("reject", () => rejectRecipe(recipe.id, comment), "Recipe rejected.")}
                  >
                    {busy === "reject" ? "Rejecting..." : "Reject"}
                  </button>
                  <button
                    type="button"
                    className="sila-studio-btn sila-studio-btn--primary"
                    disabled={busy !== null}
                    onClick={() => run("approve", () => approveRecipe(recipe.id, comment), "Approval recorded.")}
                  >
                    {busy === "approve" ? "Approving..." : "Approve"}
                  </button>
                </div>
              </div>
            </section>
          )}
          <SilaRecipeApprovalTrail recipe={recipe} />
        </div>
      )}

      <Modal
        isOpen={confirmDeleteDraft}
        onClose={() => {
          if (busy === null) setConfirmDeleteDraft(false);
        }}
        variant="danger"
        headerProps={{ heading: DELETE_DRAFT_TITLE }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setConfirmDeleteDraft(false), disabled: busy !== null },
          primaryButton: {
            text: DELETE_DRAFT_CONFIRM_LABEL,
            loading: busy === "delete-draft",
            disabled: busy !== null,
            onClick: () => run("delete-draft", () => deleteRecipeDraft(recipe.id), DELETE_DRAFT_SUCCESS),
          },
        }}
      >
        <p className="sila-modal-text">{DELETE_DRAFT_BODY}</p>
        <p className="sila-modal-text">
          <strong>{recipe.name}</strong>
          {" · "}
          {recipe.recipeCode}
        </p>
      </Modal>

      <Modal
        isOpen={confirmDeleteInactive}
        onClose={() => {
          if (busy === null) setConfirmDeleteInactive(false);
        }}
        variant="danger"
        headerProps={{ heading: DELETE_INACTIVE_TITLE }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setConfirmDeleteInactive(false), disabled: busy !== null },
          primaryButton: {
            text: DELETE_INACTIVE_CONFIRM_LABEL,
            loading: busy === "delete-inactive",
            disabled: busy !== null,
            onClick: () => run("delete-inactive", () => deleteInactiveRecipe(recipe.id), DELETE_INACTIVE_SUCCESS),
          },
        }}
      >
        <p className="sila-modal-text">{DELETE_INACTIVE_BODY}</p>
        <p className="sila-modal-text">
          <strong>{recipe.name}</strong>
          {" · "}
          {recipe.recipeCode}
        </p>
      </Modal>
    </StudioShell>
  );
};

export default SilaRecipeDetailView;
