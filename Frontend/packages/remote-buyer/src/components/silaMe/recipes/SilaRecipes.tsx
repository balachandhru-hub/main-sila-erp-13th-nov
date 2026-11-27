import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { BookOpen, FileSpreadsheet, LayoutGrid, Plus, Sparkles } from "lucide-react";
import { EmptyState, Loader, Modal, toastService } from "@vosox/shared-ui";
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
import SilaRecipeDetailView from "./SilaRecipeDetailView";
import SilaRecipeExcelPanel from "./SilaRecipeExcelPanel";
import SilaRecipeLegacyUpload from "./SilaRecipeLegacyUpload";
import SilaRecipePosPanel, { type SilaRecipePosPanelMode } from "./SilaRecipePosPanel";
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
import { StudioDrawer, StudioShell } from "../studio";
import {
  RspActionsMenu,
  RspButton,
  RspCatalog,
  RspCommandBar,
  RspEmpty,
  RspFilterBanner,
  RspFooter,
  RspHeader,
  RspIconButton,
  RspKpiRibbon,
  RspMenuItem,
  RspQuickView,
  type RspChip,
  type RspKpiKey,
} from "./SilaRecipeStudioProto";
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
const titleLabel = (value: string) => value.charAt(0) + value.slice(1).toLowerCase();

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
  const [quickRow, setQuickRow] = useState<SilaRecipeListItem | null>(null);
  const searchRef = useRef<HTMLInputElement>(null);
  const closeQuickView = useCallback(() => setQuickRow(null), []);
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
    <RspHeader
      title="Recipe Studio"
      subtitle="Intelligent Culinary Workspace"
      actions={
        <>
          <RspIconButton
            label={isOverview ? "Show Recipes" : "Show Overview"}
            active={isOverview}
            testId="recipe-studio-overview-btn"
            onClick={toggleOverview}
          >
            {isOverview ? (
              <BookOpen size={17} strokeWidth={2} aria-hidden="true" />
            ) : (
              <LayoutGrid size={17} strokeWidth={2} aria-hidden="true" />
            )}
          </RspIconButton>
          {culinaryFlags.culinarySuiteEnabled && (
            <RspIconButton
              label="Culinary Intelligence"
              active={showCulinaryOptions}
              expanded={showCulinaryOptions}
              controlsId="cs2-culinary-options-panel"
              testId="culinary-flexi-ribbon"
              onClick={() => setShowCulinaryOptions((open) => !open)}
            >
              <Sparkles size={17} strokeWidth={2} aria-hidden="true" />
            </RspIconButton>
          )}
          <RspActionsMenu>
            <SilaRecipeExcelPanel
              noun="recipe"
              canImport={canManage}
              menu
              onTemplate={downloadRecipeTemplate}
              onExport={exportRecipes}
              onPreview={previewRecipeImport}
              onConfirm={confirmRecipeImport}
              onImported={load}
            />
            <hr className="rsp-menu__divider" />
            <RspMenuItem
              testId="recipe-legacy-upload-entry"
              tone="slate"
              icon={<FileSpreadsheet size={14} strokeWidth={2} />}
              onClick={() => navigate(silaRecipeLegacyUploadPath())}
            >
              Legacy Recipe Upload
            </RspMenuItem>
          </RspActionsMenu>
          {culinaryFlags.culinarySuiteEnabled && (
            <RspButton tone="ai" testId="recipe-ai-builder-btn" onClick={() => navigate(silaRecipeCulinaryPath("ai-planner"))}>
              <Sparkles size={14} aria-hidden="true" />
              <span>AI Recipe Builder</span>
            </RspButton>
          )}
          <RspButton
            tone="primary"
            testId="recipe-new-btn"
            disabled={!canManage}
            title={canManage ? "Create Recipe" : "Requires MANAGE_SILA_RECIPE permission"}
            onClick={goNew}
          >
            <Plus size={15} strokeWidth={2.4} aria-hidden="true" />
            <span>Create Recipe</span>
          </RspButton>
        </>
      }
    />
  );

  // --- Culinary Intelligence Workspaces (including default Overview landing screen) ---
  if (isCulinaryRoute) {
    return (
      <StudioShell className="sila-me srec-page rstudio-page rsp-page rsp-stack">
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

  const toggleFilter = (setter: (value: string) => void, current: string, value: string) => {
    setter(current === value ? "" : value);
    setPage(1);
  };
  const filterChips: RspChip[] = [
    {
      id: "all",
      label: "All",
      group: "all",
      active: !status && !familyId && !itemMode && !productClass && !categoryId,
      onClick: () => {
        setStatus("");
        setFamilyId("");
        setItemMode("");
        setProductClass("");
        setCategoryId("");
        setPage(1);
      },
    },
    {
      id: "status-active",
      label: "Active",
      group: "status",
      active: status === ACTIVE_FILTER,
      onClick: () => toggleFilter(setStatus, status, ACTIVE_FILTER),
    },
    ...RECIPE_STATUSES.filter((code) => code !== ACTIVE_FILTER).map((code) => ({
      id: `status-${code}`,
      label: recipeLabel(code),
      group: "status",
      active: status === code,
      onClick: () => toggleFilter(setStatus, status, code),
    })),
    ...[
      { code: "RECIPE", label: "Recipe" },
      { code: "DIRECT", label: "Direct Item" },
      { code: "BATCH", label: "Batch" },
    ].map(({ code, label }) => ({
      id: `type-${code}`,
      label,
      group: "type",
      active: itemMode === code,
      onClick: () => toggleFilter(setItemMode, itemMode, code),
    })),
    ...families.map((family) => ({
      id: `family-${family.id}`,
      label: family.name,
      group: "family",
      active: familyId === family.id,
      onClick: () => toggleFilter(setFamilyId, familyId, family.id),
    })),
  ];
  const filterSummary = `Filtered by ${[
    status ? (status === ACTIVE_FILTER ? "Active (selling)" : recipeLabel(status)) : "",
    itemMode ? titleLabel(itemMode) : "",
    familyId ? (families.find((family) => family.id === familyId)?.name ?? "Family") : "",
    activeProductClassLabel ?? "",
    activeCategoryName ?? "",
    appliedSearch ? `"${appliedSearch}"` : "",
  ]
    .filter(Boolean)
    .join(" • ")}`;
  const selectKpi = (key: RspKpiKey) => {
    switch (key) {
      case "total":
        return openTotalRecipes();
      case "active":
        return openRecipesWithStatus(ACTIVE_FILTER);
      case "draft":
        return openRecipesWithStatus("DRAFT");
      case "pending":
        return openRecipesWithStatus("PENDING_APPROVAL");
      case "failedPos":
        return openPosPanel("failed-pos");
      case "consumption":
        return openPosPanel("posted-consumption");
      default:
        return openCostRanking();
    }
  };

  return (
    <StudioShell className="sila-me srec-page rstudio-page rsp-page rsp-stack">
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

      <RspKpiRibbon
        values={{
          total: String(dashboard?.totalRecipes ?? total),
          active: String(dashboard?.activeRecipes ?? "—"),
          draft: String(dashboard?.draftRecipes ?? "—"),
          pending: String(dashboard?.pendingApprovalTotal ?? "—"),
          failedPos: String(dashboard?.failedPosSales ?? "—"),
          consumption: String(dashboard?.consumptionPostedToday ?? "—"),
          avgCost: avgCost == null ? "—" : formatCost(avgCost),
        }}
        hints={{
          total: "Click to show every recipe",
          failedPos: "Review failed sale lines",
          consumption: "Review posted consumption",
          avgCost: "Current page · ranks by cost",
        }}
        selected={{
          total: panelView === "recipes" && !status && sortBy === "default",
          active: panelView === "recipes" && status === ACTIVE_FILTER,
          draft: panelView === "recipes" && status === "DRAFT",
          pending: panelView === "recipes" && status === "PENDING_APPROVAL",
          failedPos: panelView === "failed-pos",
          consumption: panelView === "posted-consumption",
          avgCost: panelView === "recipes" && sortBy === "cost-desc",
        }}
        failedPosCount={Number(dashboard?.failedPosSales ?? 0) || 0}
        testId="recipe-studio-dashboard-kpis"
        onSelect={selectKpi}
      />

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
          <RspCommandBar
            search={search}
            onSearch={setSearch}
            searchRef={searchRef}
            sort={sortBy}
            sortOptions={[
              { value: "default", label: "Sort: Default" },
              { value: "name-asc", label: "Sort: Name (A–Z)" },
              { value: "cost-desc", label: "Sort: Cost (High–Low)" },
              { value: "cost-asc", label: "Sort: Cost (Low–High)" },
            ]}
            onSort={(value) => setSortBy(value as CatalogSortOption)}
            viewMode={viewMode}
            onViewMode={setMode}
            chips={filterChips}
            advancedCount={[productClass, categoryId].filter(Boolean).length}
            testId="recipe-filter-toolbar"
            advancedTestId="recipe-category-filters-toggle"
            advancedExpanded={showFiltersDrawer}
            onAdvanced={() => setShowFiltersDrawer(true)}
          />

          {filtered && <RspFilterBanner text={filterSummary} onReset={clearRecipeFilters} />}

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
            <RspEmpty
              title={filtered ? "No recipe matches" : "No recipes yet"}
              hint="Create a recipe or adjust your search criteria to view your culinary catalogue."
              action={
                filtered ? (
                  <RspButton tone="glass" onClick={clearRecipeFilters}>
                    Reset filters
                  </RspButton>
                ) : undefined
              }
            />
          ) : (
            <>
              <RspCatalog
                rows={sortedRows}
                viewMode={viewMode}
                canManage={canManage}
                onOpen={setQuickRow}
                onEdit={(row) => goEdit(row.id)}
                onDelete={setDeleteTarget}
              />
              <RspFooter
                shown={sortedRows.length}
                total={total}
                page={page}
                pageCount={Math.max(1, Math.ceil(total / pageSize))}
                onPage={(next) => !loading && setPage(Math.max(1, next))}
              />
            </>
          )}
        </>
      )}

      <RspQuickView
        row={quickRow}
        canManage={canManage}
        onClose={closeQuickView}
        onOpenFull={(row) => {
          setQuickRow(null);
          goDetail(row.id);
        }}
        onEdit={(row) => {
          setQuickRow(null);
          goEdit(row.id);
        }}
        onOpenPos={() => {
          setQuickRow(null);
          openPosPanel("failed-pos");
        }}
      />

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
