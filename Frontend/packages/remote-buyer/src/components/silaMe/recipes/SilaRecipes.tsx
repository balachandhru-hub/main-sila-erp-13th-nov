import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { EmptyState, Loader, Modal, Pagination, toastService } from "@vosox/shared-ui";
import { deactivateRecipe, getRecipes, type SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import {
  confirmRecipeImport,
  downloadRecipeTemplate,
  exportRecipes,
  getRecipeDashboard,
  getRecipeMasters,
  previewRecipeImport,
  type SilaRecipeDashboard,
  type SilaRecipeMaster,
} from "../../../api/silaMe/silaRecipeToolsApi";
import SilaRecipeCatalogGrid from "./SilaRecipeCatalogGrid";
import SilaRecipeDetailView from "./SilaRecipeDetailView";
import SilaRecipeExcelPanel from "./SilaRecipeExcelPanel";
import SilaRecipeTable from "./SilaRecipeTable";
import SilaRecipeWorkspaceShell from "./SilaRecipeWorkspaceShell";
import { RECIPE_STATUSES, formatCost, recipeLabel } from "./recipeFormat";
import {
  parseSilaRecipePath,
  silaRecipeCatalogPath,
  silaRecipeDetailPath,
  silaRecipeEditPath,
  silaRecipeNewPath,
} from "./silaRecipeRoutes";
import { loadStudioViewMode, resolveStudioKind, saveStudioViewMode, StudioCover, type StudioViewMode } from "./recipeStudioVisuals";
import "../silaMeTheme.css";
import "./SilaRecipes.css";
import "./SilaRecipeStudio.css";

interface SilaRecipesProps {
  canManage: boolean;
  /** Reserved for later rebuild steps (Material price requests). Unused in Step 1 shell. */
  canRequestPrice?: boolean;
}

const PAGE_SIZE = 48;

/**
 * Recipe Studio — catalog/detail + Step 1 temporary New/Edit workspace shell.
 * Canonical URLs under /sila-recipes; SilaRecipeEditor is not mounted in Step 1.
 */
const SilaRecipes: React.FC<SilaRecipesProps> = ({ canManage, canRequestPrice: _canRequestPrice = false }) => {
  const { pathname, key: locationKey } = useLocation();
  const navigate = useNavigate();
  const route = useMemo(() => parseSilaRecipePath(pathname), [pathname]);

  const [status, setStatus] = useState("");
  const [familyId, setFamilyId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [itemMode, setItemMode] = useState("");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [rows, setRows] = useState<SilaRecipeListItem[]>([]);
  const [total, setTotal] = useState(0);
  const [families, setFamilies] = useState<SilaRecipeMaster[]>([]);
  const [categories, setCategories] = useState<SilaRecipeMaster[]>([]);
  const [dashboard, setDashboard] = useState<SilaRecipeDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<StudioViewMode>(() => loadStudioViewMode());
  const [deleteTarget, setDeleteTarget] = useState<SilaRecipeListItem | null>(null);
  const [deleting, setDeleting] = useState(false);
  /** Remount New shell when New is clicked again while already on /sila-recipes/new. */
  const [newShellEpoch, setNewShellEpoch] = useState(0);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getRecipes({
        status,
        familyId,
        categoryId,
        itemMode: itemMode || undefined,
        search: appliedSearch,
        index: page - 1,
        limit: PAGE_SIZE,
      });
      setRows(result.items);
      setTotal(result.total);
    } catch (err: unknown) {
      setRows([]);
      setError(err instanceof Error ? err.message : "Could not load the recipes.");
    } finally {
      setLoading(false);
    }
  }, [status, familyId, categoryId, itemMode, appliedSearch, page]);

  useEffect(() => {
    if (route.kind === "list") load();
  }, [load, route.kind]);

  useEffect(() => {
    Promise.all([getRecipeMasters("families"), getRecipeMasters("categories"), getRecipeDashboard()])
      .then(([loadedFamilies, loadedCategories, dash]) => {
        setFamilies(loadedFamilies);
        setCategories(loadedCategories);
        setDashboard(dash);
      })
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setAppliedSearch(search.trim());
      setPage(1);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  const avgCost = useMemo(() => {
    const priced = rows.filter((row) => row.costComplete && row.totalCost > 0);
    if (priced.length === 0) return null;
    return priced.reduce((sum, row) => sum + row.totalCost, 0) / priced.length;
  }, [rows]);

  const goCatalog = () => navigate(silaRecipeCatalogPath());
  const goDetail = (recipeId: string) => navigate(silaRecipeDetailPath(recipeId));
  const goNew = () => {
    if (!canManage) {
      toastService.error("You do not have permission to create or edit recipes.");
      return;
    }
    // Already on New: remount shell — do not blank, do not invent a recipe id, do not enter Edit.
    if (route.kind === "new") {
      setNewShellEpoch((epoch) => epoch + 1);
      return;
    }
    navigate(silaRecipeNewPath(), { replace: false });
  };
  const goEdit = (recipeId: string) => {
    if (!canManage) {
      toastService.error("You do not have permission to create or edit recipes.");
      return;
    }
    const id = recipeId?.trim() ?? "";
    if (!id) {
      toastService.error("Cannot open Edit: recipe id is missing.");
      return;
    }
    navigate(silaRecipeEditPath(id));
  };

  const setMode = (mode: StudioViewMode) => {
    setViewMode(mode);
    saveStudioViewMode(mode);
  };

  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      await deactivateRecipe(deleteTarget.id);
      toastService.success(`Deleted ${deleteTarget.recipeCode}.`);
      setDeleteTarget(null);
      if (route.kind !== "list") goCatalog();
      else await load();
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Could not delete the recipe.");
    } finally {
      setDeleting(false);
    }
  };

  // --- New Recipe: temporary workspace shell (SilaRecipeEditor not mounted) ---
  if (route.kind === "new") {
    if (!canManage) {
      return (
        <div className="sila-me srec-page rstudio-page">
          <EmptyState
            variant="error"
            title="Cannot create recipe"
            description="You do not have permission to create recipes."
            action={<button type="button" className="sila-btn sila-btn--secondary" onClick={goCatalog}>Back to Recipe Catalog</button>}
          />
        </div>
      );
    }
    return (
      <SilaRecipeWorkspaceShell
        key={`new-recipe-shell-${locationKey}-${newShellEpoch}`}
        mode="new"
        onBackToCatalog={goCatalog}
      />
    );
  }

  // --- Edit Recipe: temporary workspace shell (id from URL; no editor load) ---
  if (route.kind === "edit") {
    if (!canManage) {
      return (
        <div className="sila-me srec-page rstudio-page">
          <EmptyState
            variant="error"
            title="Cannot edit recipe"
            description="You do not have permission to edit recipes."
            action={<button type="button" className="sila-btn sila-btn--secondary" onClick={goCatalog}>Back to Recipe Catalog</button>}
          />
        </div>
      );
    }
    return (
      <SilaRecipeWorkspaceShell
        key={`edit-recipe-shell-${locationKey}-${route.recipeId}`}
        mode="edit"
        recipeId={route.recipeId}
        onBackToCatalog={goCatalog}
      />
    );
  }

  // --- Detail ---
  if (route.kind === "detail") {
    return (
      <SilaRecipeDetailView
        recipeId={route.recipeId}
        canManage={canManage}
        onBack={goCatalog}
        onEdit={(recipe) => goEdit(recipe.id)}
        onChanged={load}
      />
    );
  }

  // --- Catalog ---
  const filtered = Boolean(status || familyId || categoryId || itemMode || appliedSearch);

  return (
    <div className="sila-me srec-page rstudio-page">
      <header className="rstudio-hero">
        <div className="rstudio-hero__copy">
          <p className="rstudio-kicker">SILA Recipe Studio</p>
          <h1 className="rstudio-hero__title">Culinary catalog &amp; recipe workspace</h1>
          <p className="rstudio-hero__sub">
            Browse recipes like a digital cookbook, then build ingredients from the Material Master shopping catalog.
          </p>
        </div>
        <div className="rstudio-hero__actions">
          <SilaRecipeExcelPanel
            noun="recipe"
            canImport={canManage}
            onTemplate={downloadRecipeTemplate}
            onExport={exportRecipes}
            onPreview={previewRecipeImport}
            onConfirm={confirmRecipeImport}
            onImported={load}
          />
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            onClick={goNew}
            disabled={!canManage}
            title={canManage ? undefined : "Requires MANAGE_SILA_RECIPE permission"}
          >
            New recipe
          </button>
        </div>
      </header>

      <div className="rstudio-kpis">
        <div className="rstudio-kpi rstudio-kpi--sky">
          <span>Total recipes</span>
          <strong>{dashboard?.totalRecipes ?? total}</strong>
        </div>
        <div className="rstudio-kpi rstudio-kpi--mint">
          <span>Active</span>
          <strong>{dashboard?.activeRecipes ?? "—"}</strong>
        </div>
        <div className="rstudio-kpi rstudio-kpi--lavender">
          <span>Draft</span>
          <strong>{dashboard?.draftRecipes ?? "—"}</strong>
        </div>
        <div className="rstudio-kpi rstudio-kpi--amber">
          <span>Pending approval</span>
          <strong>{dashboard?.pendingApprovalTotal ?? "—"}</strong>
        </div>
        <div className="rstudio-kpi rstudio-kpi--blue">
          <span>Avg recipe cost (page)</span>
          <strong>{avgCost == null ? "—" : formatCost(avgCost)}</strong>
        </div>
      </div>

      <nav className="rstudio-cats" aria-label="Recipe categories">
        <button
          type="button"
          className={`rstudio-chip${!categoryId ? " is-active" : ""}`}
          onClick={() => { setCategoryId(""); setPage(1); }}
        >
          <StudioCover kind="other" compact title="All" />
          All Recipes
        </button>
        {categories.map((category) => {
          const kind = resolveStudioKind(category.name, category.code);
          return (
            <button
              key={category.id}
              type="button"
              className={`rstudio-chip${categoryId === category.id ? " is-active" : ""}`}
              onClick={() => { setCategoryId(category.id); setPage(1); }}
            >
              <StudioCover kind={kind} compact title={category.name} />
              {category.name}
            </button>
          );
        })}
      </nav>

      <section className="rstudio-toolbar sila-card">
        <div className="srec-filters srec-filters--flush">
          <div className="sila-field srec-grow">
            <label className="sila-label" htmlFor="srec-search">Search recipes</label>
            <input
              id="srec-search"
              className="sila-input"
              type="search"
              placeholder="Name, code or POS"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="srec-status">Status</label>
            <select id="srec-status" className="sila-select" value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}>
              <option value="">All</option>
              {RECIPE_STATUSES.map((code) => (
                <option key={code} value={code}>{recipeLabel(code)}</option>
              ))}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="srec-family-filter">Family / cuisine</label>
            <select id="srec-family-filter" className="sila-select" value={familyId} onChange={(e) => { setFamilyId(e.target.value); setPage(1); }}>
              <option value="">All</option>
              {families.map((family) => (
                <option key={family.id} value={family.id}>{family.name}</option>
              ))}
            </select>
          </div>
          <div className="sila-field">
            <label className="sila-label" htmlFor="srec-mode-filter">Recipe type</label>
            <select id="srec-mode-filter" className="sila-select" value={itemMode} onChange={(e) => { setItemMode(e.target.value); setPage(1); }}>
              <option value="">All</option>
              <option value="RECIPE">Recipe</option>
              <option value="DIRECT">Direct item</option>
              <option value="BATCH">Batch</option>
            </select>
          </div>
          <div className="rstudio-view-toggle" role="group" aria-label="Catalog view">
            <button type="button" className={`sila-btn sila-btn--sm ${viewMode === "grid" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setMode("grid")}>Grid</button>
            <button type="button" className={`sila-btn sila-btn--sm ${viewMode === "list" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setMode("list")}>List</button>
          </div>
        </div>
      </section>

      {loading ? (
        <Loader size={24} message="Loading Recipe Studio..." />
      ) : error ? (
        <EmptyState
          variant="error"
          title="Couldn't load recipes"
          description={error}
          action={<button type="button" className="sila-btn sila-btn--secondary" onClick={load}>Try again</button>}
        />
      ) : rows.length === 0 && page === 1 ? (
        <EmptyState title={filtered ? "No recipe matches" : "No recipes yet"} description="Create a recipe to start building your culinary catalog." />
      ) : (
        <>
          {viewMode === "grid" ? (
            <SilaRecipeCatalogGrid
              rows={rows}
              canManage={canManage}
              onOpen={goDetail}
              onEdit={canManage ? goEdit : undefined}
              onDelete={canManage ? setDeleteTarget : undefined}
            />
          ) : (
            <section className="sila-card">
              <SilaRecipeTable
                rows={rows}
                onOpen={goDetail}
                onEdit={canManage ? goEdit : undefined}
                onDelete={canManage ? setDeleteTarget : undefined}
              />
            </section>
          )}
          <Pagination
            page={page}
            hasNext={page * PAGE_SIZE < total}
            onPrevious={() => setPage((current) => Math.max(1, current - 1))}
            onNext={() => setPage((current) => current + 1)}
            disabled={loading}
          />
        </>
      )}

      <Modal
        isOpen={Boolean(deleteTarget)}
        onClose={() => setDeleteTarget(null)}
        variant="danger"
        headerProps={{ heading: `Delete ${deleteTarget?.recipeCode || "recipe"}` }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setDeleteTarget(null), disabled: deleting },
          primaryButton: {
            text: "Delete",
            loading: deleting,
            onClick: confirmDelete,
          },
        }}
      >
        <p className="sila-modal-text">
          Delete {deleteTarget?.name}? This deactivates the recipe
          {deleteTarget?.posCode ? ` and stops POS matching for ${deleteTarget.posCode}` : ""}.
        </p>
      </Modal>
    </div>
  );
};

export default SilaRecipes;
