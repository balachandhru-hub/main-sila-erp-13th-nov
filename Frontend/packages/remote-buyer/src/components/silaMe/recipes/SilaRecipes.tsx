import React, { useCallback, useEffect, useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import {
  BookOpen,
  CheckCircle2,
  ChefHat,
  Clock,
  DollarSign,
  FileSpreadsheet,
  FileText,
  Filter,
  Flame,
  Grid3x3,
  LayoutGrid,
  List,
  Plus,
  Search,
  ShoppingBag,
  Sparkles,
  X,
} from "lucide-react";
import { EmptyState, Loader, Modal, Pagination, toastService } from "@vosox/shared-ui";
import {
  deleteInactiveRecipe,
  deleteRecipeDraft,
  getRecipes,
  type SilaRecipeListItem,
} from "../../../api/silaMe/silaRecipeApi";
import {
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
import SilaRecipeLegacyUpload from "./SilaRecipeLegacyUpload";
import SilaRecipePosPanel, { type SilaRecipePosPanelMode } from "./SilaRecipePosPanel";
import SilaRecipeTable from "./SilaRecipeTable";
import SilaRecipeWorkspaceShell from "./SilaRecipeWorkspaceShell";
import { RECIPE_STATUSES, formatCost, recipeLabel } from "./recipeFormat";
import { PRODUCT_CLASSES } from "./recipeHospitality";
import {
  parseSilaRecipePath,
  silaRecipeCatalogPath,
  silaRecipeCulinaryPath,
  silaRecipeDetailPath,
  silaRecipeEditPath,
  silaRecipeLegacyUploadPath,
  silaRecipeNewPath,
  silaRecipeOverviewPath,
  type CulinaryWorkspaceKind,
} from "./silaRecipeRoutes";
import {
  DEFAULT_CULINARY_FEATURE_FLAGS,
  getCulinaryAiStatus,
  type SilaCulinaryAiStatus,
} from "../../../api/silaMe/silaCulinaryAiApi";
import type { SilaMePermissions } from "../silaMePermissions";
import SilaRecipeStudio2Workspaces, {
  isCulinaryWorkspaceRoute,
} from "./culinary/SilaRecipeStudio2Workspaces";
import {
  loadStudioViewMode,
  resolveStudioKind,
  saveStudioViewMode,
  StudioCover,
} from "./recipeStudioVisuals";
import {
  StudioActionBar,
  StudioDrawer,
  StudioFilterBar,
  StudioHeroPanel,
  StudioIntelligenceToggle,
  StudioKPIBar,
  StudioKPICard,
  StudioOverviewButton,
  StudioShell,
} from "../studio";
import "../silaMeTheme.css";
import "./SilaRecipes.css";
import "./SilaRecipeStudio.css";

interface SilaRecipesProps {
  canManage: boolean;
  /** Approvers can decide legacy migration batches and publish. */
  canApprove?: boolean;
  canRequestPrice?: boolean;
  permissions?: Partial<SilaMePermissions>;
  onNavigate?: (key: string) => void;
}

const PAGE_SIZE = 48;
const COMPACT_PAGE_SIZE = 96;
const ACTIVE_FILTER = "ACTIVE";

type CatalogViewOption = "grid" | "compact" | "list";
type CatalogSortOption = "default" | "name-asc" | "cost-desc" | "cost-asc";

/**
 * Recipe Studio — Unified SILA White & Blue workspace with Overview landing screen,
 * expandable 8-group Culinary Intelligence navigation, 7 pastel KPI boxes,
 * single-line filter toolbar, responsive 6/5/4/3/2/1 card catalogue, and detail/editor shells.
 */
const SilaRecipes: React.FC<SilaRecipesProps> = ({
  canManage,
  canApprove = false,
  canRequestPrice: _canRequestPrice = false,
  permissions,
  onNavigate,
}) => {
  const { pathname, search: locationSearch, key: locationKey } = useLocation();
  const navigate = useNavigate();
  const route = useMemo(() => parseSilaRecipePath(pathname), [pathname]);

  const [status, setStatus] = useState("");
  const [familyId, setFamilyId] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [productClass, setProductClass] = useState("");
  const [itemMode, setItemMode] = useState("");
  const [sortBy, setSortBy] = useState<CatalogSortOption>("default");
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
  const [viewMode, setViewMode] = useState<CatalogViewOption>(() => loadStudioViewMode());
  const [showFiltersDrawer, setShowFiltersDrawer] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<SilaRecipeListItem | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [newShellEpoch, setNewShellEpoch] = useState(0);
  const [aiStatus, setAiStatus] = useState<SilaCulinaryAiStatus | null>(null);
  const [panelView, setPanelView] = useState<SilaRecipePosPanelMode | "recipes">("recipes");
  const [showCulinaryOptions, setShowCulinaryOptions] = useState<boolean>(() =>
    route.kind !== "overview" && isCulinaryWorkspaceRoute(route),
  );

  const culinaryFlags = aiStatus?.featureFlags ?? DEFAULT_CULINARY_FEATURE_FLAGS;
  const effectiveRoute = useMemo(() => {
    if (route.kind === "overview" && !culinaryFlags.overviewEnabled) {
      return { kind: "list" as const };
    }
    return route;
  }, [route, culinaryFlags.overviewEnabled]);

  const refreshAiStatus = useCallback(() => {
    getCulinaryAiStatus()
      .then((res) => setAiStatus(res))
      .catch(() => undefined);
  }, []);

  const pageSize = viewMode === "compact" ? COMPACT_PAGE_SIZE : PAGE_SIZE;

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const result = await getRecipes({
        status: status === ACTIVE_FILTER ? "" : status,
        active: status === ACTIVE_FILTER ? true : undefined,
        familyId,
        categoryId,
        productClass: productClass || undefined,
        itemMode: itemMode || undefined,
        search: appliedSearch,
        index: page - 1,
        limit: pageSize,
      });
      setRows(result.items);
      setTotal(result.total);
    } catch (err: unknown) {
      setRows([]);
      setError(err instanceof Error ? err.message : "Could not load the recipes.");
    } finally {
      setLoading(false);
    }
  }, [status, familyId, categoryId, productClass, itemMode, appliedSearch, page, pageSize]);

  useEffect(() => {
    if (effectiveRoute.kind === "list" && panelView === "recipes") load();
  }, [load, effectiveRoute.kind, panelView]);

  useEffect(() => {
    if (route.kind !== "list") return;
    const query = new URLSearchParams(locationSearch);
    const view = query.get("view");
    setPanelView(view === "failed-pos" || view === "posted-consumption" ? view : "recipes");
    if (query.has("status")) {
      setStatus(query.get("status") ?? "");
      setPage(1);
    }
  }, [locationSearch, route.kind]);

  useEffect(() => {
    Promise.all([getRecipeMasters("families"), getRecipeMasters("categories"), getRecipeDashboard()])
      .then(([loadedFamilies, loadedCategories, dash]) => {
        setFamilies(loadedFamilies);
        setCategories(loadedCategories);
        setDashboard(dash);
      })
      .catch(() => undefined);
    refreshAiStatus();
  }, [refreshAiStatus]);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setAppliedSearch(search.trim());
      setPage(1);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  const sortedRows = useMemo(() => {
    if (sortBy === "default") return rows;
    const copy = rows.slice();
    if (sortBy === "name-asc") {
      copy.sort((a, b) => a.name.localeCompare(b.name));
    } else if (sortBy === "cost-desc") {
      copy.sort((a, b) => (b.costPerServing ?? b.totalCost) - (a.costPerServing ?? a.totalCost));
    } else if (sortBy === "cost-asc") {
      copy.sort((a, b) => (a.costPerServing ?? a.totalCost) - (b.costPerServing ?? b.totalCost));
    }
    return copy;
  }, [rows, sortBy]);

  const avgCost = useMemo(() => {
    const priced = rows.filter((row) => row.costComplete && row.totalCost > 0);
    if (priced.length === 0) return null;
    return priced.reduce((sum, row) => sum + row.totalCost, 0) / priced.length;
  }, [rows]);

  const goOverview = () => navigate(silaRecipeOverviewPath());
  const goCatalog = () => navigate(silaRecipeCatalogPath());
  const clearRecipeFilters = () => {
    setStatus("");
    setFamilyId("");
    setItemMode("");
    setProductClass("");
    setCategoryId("");
    setSearch("");
    setAppliedSearch("");
    setPage(1);
  };
  const openRecipesWithStatus = (nextStatus: string) => {
    setPanelView("recipes");
    setStatus(nextStatus);
    setPage(1);
    if (route.kind !== "list" || locationSearch) {
      navigate(silaRecipeCatalogPath(nextStatus ? { status: nextStatus } : undefined), { replace: route.kind === "list" });
    }
  };
  const openTotalRecipes = () => {
    clearRecipeFilters();
    setSortBy("default");
    setPanelView("recipes");
    if (route.kind !== "list" || locationSearch) navigate(silaRecipeCatalogPath(), { replace: route.kind === "list" });
  };
  const openPosPanel = (view: SilaRecipePosPanelMode) => {
    setPanelView(view);
    navigate(silaRecipeCatalogPath({ view }), { replace: route.kind === "list" });
  };
  const openCostRanking = () => {
    setPanelView("recipes");
    setSortBy("cost-desc");
    setMode("list");
    if (route.kind !== "list" || locationSearch) navigate(silaRecipeCatalogPath(), { replace: route.kind === "list" });
  };
  const toggleOverview = () => (route.kind === "overview" ? goCatalog() : goOverview());
  const goDetail = (recipeId: string) => navigate(silaRecipeDetailPath(recipeId));
  const goNew = () => {
    if (!canManage) {
      toastService.error("You do not have permission to create or edit recipes.");
      return;
    }
    if (effectiveRoute.kind === "new") {
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

  const setMode = (mode: CatalogViewOption) => {
    setViewMode(mode);
    saveStudioViewMode(mode);
    setPage(1);
  };

  const deletingInactive = canDeleteInactiveRecipe(deleteTarget?.status);
  const confirmDelete = async () => {
    if (!deleteTarget) return;
    setDeleting(true);
    try {
      if (canDeleteInactiveRecipe(deleteTarget.status)) {
        await deleteInactiveRecipe(deleteTarget.id);
        toastService.success(DELETE_INACTIVE_SUCCESS);
      } else {
        await deleteRecipeDraft(deleteTarget.id);
        toastService.success(DELETE_DRAFT_SUCCESS);
      }
      setDeleteTarget(null);
      await load();
      if (effectiveRoute.kind !== "list") goCatalog();
    } catch (err: unknown) {
      toastService.error(
        err instanceof Error
          ? err.message
          : deletingInactive
            ? "Could not permanently delete the inactive recipe."
            : "Could not delete the draft recipe.",
      );
    } finally {
      setDeleting(false);
    }
  };

  const selectCulinaryRoute = (target: "list" | "legacy-upload" | CulinaryWorkspaceKind) => {
    if (target === "menus") {
      onNavigate?.("silaMenuStudio");
    } else if (target === "list") {
      goCatalog();
    } else if (target === "legacy-upload") {
      navigate(silaRecipeLegacyUploadPath());
    } else {
      navigate(silaRecipeCulinaryPath(target));
    }
  };

  // --- Legacy Recipe Upload (isolated migration workspace) ---
  if (effectiveRoute.kind === "legacy-upload") {
    return (
      <SilaRecipeLegacyUpload
        canManage={canManage}
        canApprove={canApprove}
        onBack={goCatalog}
        onOpenRecipe={(recipeId) => navigate(silaRecipeDetailPath(recipeId))}
      />
    );
  }

  // --- New Recipe: Recipe Studio workspace ---
  if (effectiveRoute.kind === "new") {
    if (!canManage) {
      return (
        <StudioShell className="sila-me srec-page rstudio-page">
          <EmptyState
            variant="error"
            title="Cannot create recipe"
            description="You do not have permission to create recipes."
            action={
              <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={goCatalog}>
                Back to Recipe Master
              </button>
            }
          />
        </StudioShell>
      );
    }
    return (
      <SilaRecipeWorkspaceShell
        key={`new-recipe-shell-${locationKey}-${newShellEpoch}`}
        mode="new"
        onBackToCatalog={goCatalog}
        onDraftSaved={goDetail}
      />
    );
  }

  // --- Edit Recipe: Recipe Studio workspace ---
  if (effectiveRoute.kind === "edit") {
    if (!canManage) {
      return (
        <StudioShell className="sila-me srec-page rstudio-page">
          <EmptyState
            variant="error"
            title="Cannot edit recipe"
            description="You do not have permission to edit recipes."
            action={
              <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={goCatalog}>
                Back to Recipe Master
              </button>
            }
          />
        </StudioShell>
      );
    }
    return (
      <SilaRecipeWorkspaceShell
        key={`edit-recipe-shell-${locationKey}-${effectiveRoute.recipeId}`}
        mode="edit"
        recipeId={effectiveRoute.recipeId}
        onBackToCatalog={goCatalog}
      />
    );
  }

  // --- Detail ---
  if (effectiveRoute.kind === "detail") {
    return (
      <SilaRecipeDetailView
        recipeId={effectiveRoute.recipeId}
        canManage={canManage}
        onBack={goCatalog}
        onEdit={(recipe) => goEdit(recipe.id)}
        onChanged={load}
      />
    );
  }

  const isOverview = effectiveRoute.kind === "overview";
  const isCulinaryRoute = isCulinaryWorkspaceRoute(effectiveRoute);

  const heroSection = (
    <StudioHeroPanel
      title="Recipe Studio"
      subtitle="Intelligent Culinary Workspace"
      topRight={
        <>
          <SilaRecipeExcelPanel
            noun="recipe"
            canImport={canManage}
            iconOnly
            onTemplate={downloadRecipeTemplate}
            onExport={exportRecipes}
            onPreview={previewRecipeImport}
            onConfirm={confirmRecipeImport}
            onImported={load}
          />
          <StudioOverviewButton
            active={false}
            tooltip="Legacy Recipe Upload"
            ariaLabel="Legacy Recipe Upload"
            testId="recipe-legacy-upload-entry"
            icon={<FileSpreadsheet size={17} strokeWidth={2} aria-hidden="true" />}
            onClick={() => navigate(silaRecipeLegacyUploadPath())}
          />
          {culinaryFlags.culinarySuiteEnabled && (
            <StudioIntelligenceToggle
              iconOnly
              label="Culinary Intelligence"
              expanded={showCulinaryOptions}
              controlsId="cs2-culinary-options-panel"
              testId="culinary-flexi-ribbon"
              icon={<Sparkles size={18} strokeWidth={2} />}
              onClick={() => setShowCulinaryOptions((open) => !open)}
            />
          )}
          <StudioOverviewButton
            active={isOverview}
            tooltip={isOverview ? "Show Recipes" : "Show Overview"}
            ariaLabel={isOverview ? "Show Recipes" : "Show Overview"}
            testId="recipe-studio-overview-btn"
            icon={
              isOverview ? (
                <BookOpen size={18} strokeWidth={2} aria-hidden="true" />
              ) : (
                <LayoutGrid size={18} strokeWidth={2} aria-hidden="true" />
              )
            }
            onClick={toggleOverview}
          />
        </>
      }
      actions={
        <StudioActionBar ariaLabel="Recipe Studio primary actions">
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--primary"
            onClick={goNew}
            disabled={!canManage}
            title={canManage ? undefined : "Requires MANAGE_SILA_RECIPE permission"}
            data-testid="recipe-new-btn"
          >
            <Plus size={15} strokeWidth={2.1} aria-hidden="true" />
            <span>New Recipe</span>
          </button>
        </StudioActionBar>
      }
    />
  );

  // --- Culinary Intelligence Workspaces (including default Overview landing screen) ---
  if (isCulinaryRoute) {
    return (
      <StudioShell className="sila-me srec-page rstudio-page">
        {heroSection}
        <SilaRecipeStudio2Workspaces
          route={effectiveRoute}
          flags={culinaryFlags}
          aiStatus={aiStatus}
          dashboard={dashboard}
          totalCatalogCount={total}
          permissions={permissions}
          showOptions={showCulinaryOptions}
          onSelectRecipeRoute={selectCulinaryRoute}
          onNavigateGlobal={onNavigate}
          onOpenRecipeView={(query) => navigate(silaRecipeCatalogPath(query))}
          onRefreshAiStatus={refreshAiStatus}
        />
      </StudioShell>
    );
  }

  // --- Recipe Master Catalog ---
  const filtered = Boolean(status || familyId || categoryId || productClass || itemMode || appliedSearch);
  const activeProductClassLabel = PRODUCT_CLASSES.find((cls) => cls.id === productClass)?.label;
  const activeCategoryName = categories.find((cat) => cat.id === categoryId)?.name;

  return (
    <StudioShell className="sila-me srec-page rstudio-page">
      {heroSection}

      <SilaRecipeStudio2Workspaces
        route={effectiveRoute}
        flags={culinaryFlags}
        aiStatus={aiStatus}
        dashboard={dashboard}
        totalCatalogCount={total}
        permissions={permissions}
        showOptions={showCulinaryOptions}
        onSelectRecipeRoute={selectCulinaryRoute}
        onNavigateGlobal={onNavigate}
        onOpenRecipeView={(query) => navigate(silaRecipeCatalogPath(query))}
        onRefreshAiStatus={refreshAiStatus}
      />

      <StudioKPIBar ariaLabel="Recipe Studio overview metrics" testId="recipe-studio-dashboard-kpis">
        <StudioKPICard
          label="Total Recipes"
          value={dashboard?.totalRecipes ?? total}
          tone="blue"
          icon={<ChefHat size={15} strokeWidth={1.9} />}
          testId="kpi-total-recipes"
          drillLabel="Show all recipes"
          selected={panelView === "recipes" && !status && sortBy === "default"}
          onClick={openTotalRecipes}
        />
        <StudioKPICard
          label="Active Recipes"
          value={dashboard?.activeRecipes ?? "—"}
          tone="green"
          icon={<CheckCircle2 size={15} strokeWidth={1.9} />}
          testId="kpi-active-recipes"
          drillLabel="Show active (selling) recipes"
          selected={panelView === "recipes" && status === ACTIVE_FILTER}
          onClick={() => openRecipesWithStatus(ACTIVE_FILTER)}
        />
        <StudioKPICard
          label="Draft Recipes"
          value={dashboard?.draftRecipes ?? "—"}
          tone="lavender"
          icon={<FileText size={15} strokeWidth={1.9} />}
          testId="kpi-draft-recipes"
          drillLabel="Show draft recipes"
          selected={panelView === "recipes" && status === "DRAFT"}
          onClick={() => openRecipesWithStatus("DRAFT")}
        />
        <StudioKPICard
          label="Pending Approval"
          value={dashboard?.pendingApprovalTotal ?? "—"}
          tone="amber"
          icon={<Clock size={15} strokeWidth={1.9} />}
          testId="kpi-pending-approval"
          drillLabel="Show recipes pending approval"
          selected={panelView === "recipes" && status === "PENDING_APPROVAL"}
          onClick={() => openRecipesWithStatus("PENDING_APPROVAL")}
        />
        <StudioKPICard
          label="Failed POS Sales"
          value={dashboard?.failedPosSales ?? "—"}
          tone="rose"
          icon={<Flame size={15} strokeWidth={1.9} />}
          testId="kpi-failed-pos"
          drillLabel="Show failed POS sale lines"
          selected={panelView === "failed-pos"}
          onClick={() => openPosPanel("failed-pos")}
        />
        <StudioKPICard
          label="Posted Consumption Today"
          value={dashboard?.consumptionPostedToday ?? "—"}
          tone="indigo"
          icon={<ShoppingBag size={15} strokeWidth={1.9} />}
          testId="kpi-posted-consumption"
          drillLabel="Show consumption posted today"
          selected={panelView === "posted-consumption"}
          onClick={() => openPosPanel("posted-consumption")}
        />
        <StudioKPICard
          label="Average Recipe Cost (Page)"
          value={avgCost == null ? "—" : formatCost(avgCost)}
          tone="sky"
          icon={<DollarSign size={15} strokeWidth={1.9} />}
          testId="kpi-average-cost"
          drillLabel="Show recipes ranked by cost"
          selected={panelView === "recipes" && sortBy === "cost-desc"}
          onClick={openCostRanking}
        />
      </StudioKPIBar>

      {panelView !== "recipes" && (
        <SilaRecipePosPanel
          mode={panelView}
          onBackToRecipes={openTotalRecipes}
          onOpenPosStudio={onNavigate ? () => onNavigate("silaPos") : undefined}
          onOpenRecipe={goDetail}
        />
      )}

      {panelView === "recipes" && (
        <>
        <StudioFilterBar
          ariaLabel="Recipe library search and filters"
          testId="recipe-filter-toolbar"
          left={
            <>
              <div className="sila-studio-search" style={{ minWidth: "220px", flex: "1 1 240px" }}>
                <Search size={14} strokeWidth={1.9} className="sila-studio-search__icon" aria-hidden="true" />
                <input
                  id="srec-search"
                  className="sila-studio-input"
                  type="search"
                  aria-label="Search recipes"
                  placeholder="Search recipe name, code or POS..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>

              <select
                id="srec-status"
                aria-label="Status"
                className="sila-studio-select"
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value);
                  setPage(1);
                }}
              >
                <option value="">All Statuses</option>
                  <option value={ACTIVE_FILTER}>Active (selling)</option>
                {RECIPE_STATUSES.map((code) => (
                  <option key={code} value={code}>
                    {recipeLabel(code)}
                  </option>
                ))}
              </select>

              <select
                id="srec-family-filter"
                aria-label="Recipe family and cuisine"
                className="sila-studio-select"
                value={familyId}
                onChange={(e) => {
                  setFamilyId(e.target.value);
                  setPage(1);
                }}
              >
                <option value="">All Families / Cuisines</option>
                {families.map((family) => (
                  <option key={family.id} value={family.id}>
                    {family.name}
                  </option>
                ))}
              </select>

              <select
                id="srec-mode-filter"
                aria-label="Recipe type"
                className="sila-studio-select"
                value={itemMode}
                onChange={(e) => {
                  setItemMode(e.target.value);
                  setPage(1);
                }}
              >
                <option value="">All Types</option>
                <option value="RECIPE">Recipe</option>
                <option value="DIRECT">Direct Item</option>
                <option value="BATCH">Batch</option>
              </select>

              <select
                id="srec-sort"
                aria-label="Sort recipes"
                className="sila-studio-select"
                value={sortBy}
                onChange={(e) => setSortBy(e.target.value as CatalogSortOption)}
              >
                <option value="default">Sort: Default</option>
                <option value="name-asc">Sort: Name (A–Z)</option>
                <option value="cost-desc">Sort: Cost (High–Low)</option>
                <option value="cost-asc">Sort: Cost (Low–High)</option>
              </select>

              <button
                type="button"
                className={`sila-studio-btn ${showFiltersDrawer || productClass || categoryId ? "sila-studio-btn--subtle" : "sila-studio-btn--secondary"}`}
                data-testid="recipe-category-filters-toggle"
                aria-expanded={showFiltersDrawer}
                aria-controls="recipe-advanced-filters-drawer"
                onClick={() => setShowFiltersDrawer(true)}
              >
                <Filter size={14} strokeWidth={1.9} aria-hidden="true" />
                <span>Advanced Filters</span>
                {(productClass || categoryId) && (
                  <span className="sila-studio-badge sila-studio-badge--info">
                    {[productClass, categoryId].filter(Boolean).length}
                  </span>
                )}
              </button>
            </>
          }
          right={
            <div className="sila-studio-segmented" role="group" aria-label="Catalog view mode">
              <button
                type="button"
                className={`sila-studio-segmented__btn${viewMode === "grid" ? " is-active" : ""}`}
                onClick={() => setMode("grid")}
                title="Standard Grid"
              >
                <LayoutGrid size={14} strokeWidth={1.9} aria-hidden="true" />
                <span>Grid</span>
              </button>
              <button
                type="button"
                className={`sila-studio-segmented__btn${viewMode === "compact" ? " is-active" : ""}`}
                onClick={() => setMode("compact")}
                title="Compact Grid"
              >
                <Grid3x3 size={14} strokeWidth={1.9} aria-hidden="true" />
                <span>Compact</span>
              </button>
              <button
                type="button"
                className={`sila-studio-segmented__btn${viewMode === "list" ? " is-active" : ""}`}
                onClick={() => setMode("list")}
                title="List View"
              >
                <List size={14} strokeWidth={1.9} aria-hidden="true" />
                <span>List</span>
              </button>
            </div>
          }
          activeChips={
            filtered ? (
              <>
                {activeProductClassLabel && (
                  <button
                    type="button"
                    className="sila-studio-chip is-active"
                    onClick={() => {
                      setProductClass("");
                      setCategoryId("");
                      setPage(1);
                    }}
                  >
                    <span>Family: {activeProductClassLabel}</span>
                    <X size={12} aria-hidden="true" />
                  </button>
                )}
                {activeCategoryName && (
                  <button
                    type="button"
                    className="sila-studio-chip is-active"
                    onClick={() => {
                      setCategoryId("");
                      setPage(1);
                    }}
                  >
                    <span>Category: {activeCategoryName}</span>
                    <X size={12} aria-hidden="true" />
                  </button>
                )}
                {(status || familyId || itemMode || appliedSearch) && (
                  <button
                    type="button"
                    className="sila-studio-chip"
                    onClick={() => {
                      setStatus("");
                      setFamilyId("");
                      setItemMode("");
                      setProductClass("");
                      setCategoryId("");
                      setSearch("");
                      setAppliedSearch("");
                      setPage(1);
                    }}
                  >
                    <span>Clear all filters</span>
                    <X size={12} aria-hidden="true" />
                  </button>
                )}
              </>
            ) : null
          }
        />

        <StudioDrawer
          open={showFiltersDrawer}
          onClose={() => setShowFiltersDrawer(false)}
          title="Advanced Recipe Filters"
          subtitle="Filter recipes by hospitality class and culinary category"
          testId="recipe-category-filters"
          width="md"
          footer={
            <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem" }}>
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--secondary"
                onClick={() => {
                  setProductClass("");
                  setCategoryId("");
                  setPage(1);
                }}
              >
                Reset Class &amp; Category
              </button>
              <button
                type="button"
                className="sila-studio-btn sila-studio-btn--primary"
                onClick={() => setShowFiltersDrawer(false)}
              >
                Apply Filters
              </button>
            </div>
          }
        >
          <div id="recipe-advanced-filters-drawer" className="srec-stack">
            <div>
              <h4 className="sila-studio-card__title" style={{ marginBottom: "0.5rem" }}>
                Hospitality Product Class
              </h4>
              <div className="sila-studio-chip-row" data-testid="recipe-product-class-tabs">
                <button
                  type="button"
                  className={`sila-studio-chip${!productClass ? " is-active" : ""}`}
                  onClick={() => {
                    setProductClass("");
                    setCategoryId("");
                    setPage(1);
                  }}
                >
                  <StudioCover kind="other" compact title="All" />
                  <span>All</span>
                </button>
                {PRODUCT_CLASSES.map((cls) => (
                  <button
                    key={cls.id}
                    type="button"
                    className={`sila-studio-chip${productClass === cls.id ? " is-active" : ""}`}
                    onClick={() => {
                      setProductClass(cls.id);
                      setCategoryId("");
                      setPage(1);
                    }}
                  >
                    <StudioCover kind={resolveStudioKind(cls.label, cls.id)} compact title={cls.label} />
                    <span>{cls.label}</span>
                  </button>
                ))}
              </div>
            </div>

            <div>
              <h4 className="sila-studio-card__title" style={{ marginBottom: "0.5rem" }}>
                Recipe Category / Subcategory
              </h4>
              <div className="sila-studio-chip-row">
                <button
                  type="button"
                  className={`sila-studio-chip${!categoryId ? " is-active" : ""}`}
                  onClick={() => {
                    setCategoryId("");
                    setPage(1);
                  }}
                >
                  All subcategories
                </button>
                {categories
                  .filter((category) => !productClass || !category.productClass || category.productClass === productClass)
                  .map((category) => {
                    const kind = resolveStudioKind(category.name, category.code);
                    return (
                      <button
                        key={category.id}
                        type="button"
                        className={`sila-studio-chip${categoryId === category.id ? " is-active" : ""}`}
                        onClick={() => {
                          setCategoryId(category.id);
                          setPage(1);
                        }}
                      >
                        <StudioCover kind={kind} compact title={category.name} />
                        <span>{category.name}</span>
                      </button>
                    );
                  })}
              </div>
            </div>
          </div>
        </StudioDrawer>

        {loading ? (
          <Loader size={24} message="Loading Recipe Studio..." />
        ) : error ? (
          <EmptyState
            variant="error"
            title="Couldn't load recipes"
            description={error}
            action={
              <button type="button" className="sila-studio-btn sila-studio-btn--secondary" onClick={load}>
                Try again
              </button>
            }
          />
        ) : sortedRows.length === 0 && page === 1 ? (
          <EmptyState
            title={filtered ? "No recipe matches" : "No recipes yet"}
            description="Create a recipe or adjust your search criteria to view your culinary catalogue."
          />
        ) : (
          <>
            {viewMode === "list" ? (
              <section className="sila-studio-card">
                <SilaRecipeTable
                  rows={sortedRows}
                  onOpen={goDetail}
                  onEdit={canManage ? goEdit : undefined}
                  onDelete={canManage ? setDeleteTarget : undefined}
                />
              </section>
            ) : (
              <SilaRecipeCatalogGrid
                rows={sortedRows}
                canManage={canManage}
                variant={viewMode === "compact" ? "compact" : "standard"}
                onOpen={goDetail}
                onEdit={canManage ? goEdit : undefined}
                onDelete={canManage ? setDeleteTarget : undefined}
              />
            )}
            <Pagination
              page={page}
              hasNext={page * pageSize < total}
              onPrevious={() => setPage((current) => Math.max(1, current - 1))}
              onNext={() => setPage((current) => current + 1)}
              disabled={loading}
            />
          </>
        )}
        </>
      )}

      <Modal
        isOpen={Boolean(deleteTarget)}
        onClose={() => {
          if (!deleting) setDeleteTarget(null);
        }}
        variant="danger"
        headerProps={{ heading: deletingInactive ? DELETE_INACTIVE_TITLE : DELETE_DRAFT_TITLE }}
        footerProps={{
          secondaryButton: { text: "Cancel", onClick: () => setDeleteTarget(null), disabled: deleting },
          primaryButton: {
            text: deletingInactive ? DELETE_INACTIVE_CONFIRM_LABEL : DELETE_DRAFT_CONFIRM_LABEL,
            loading: deleting,
            disabled: deleting,
            onClick: confirmDelete,
          },
        }}
      >
        <p className="sila-modal-text">{deletingInactive ? DELETE_INACTIVE_BODY : DELETE_DRAFT_BODY}</p>
        <p className="sila-modal-text">
          <strong>{deleteTarget?.name || "—"}</strong>
          {" · "}
          {deleteTarget?.recipeCode || "—"}
        </p>
      </Modal>
    </StudioShell>
  );
};

export default SilaRecipes;
