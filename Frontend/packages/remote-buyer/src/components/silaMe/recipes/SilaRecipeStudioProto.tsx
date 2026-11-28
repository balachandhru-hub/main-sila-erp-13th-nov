import React, { useEffect, useRef, useState } from "react";
import {
  AlertTriangle,
  ArrowUpRight,
  CheckCircle2,
  ChefHat,
  ChevronDown,
  ChevronRight,
  Eye,
  Filter,
  Grid3x3,
  LayoutGrid,
  List,
  MoreHorizontal,
  Pencil,
  Search,
  SlidersHorizontal,
  TrendingUp,
  Trash2,
  X,
} from "lucide-react";
import { getRecipe } from "../../../api/silaMe/silaRecipeApi";
import type { SilaRecipeDetail, SilaRecipeListItem } from "../../../api/silaMe/silaRecipeApi";
import { costPerServing, formatCost, formatPercent, formatQty, recipeLabel } from "./recipeFormat";
import { formatProductClassLabel } from "./recipeHospitality";
import SilaRecipePicture from "./SilaRecipePicture";
import "./SilaRecipeStudioProto.css";
import "./SilaStudioProtoTheme.css";

export type RspViewMode = "grid" | "compact" | "list";

const cx = (...parts: Array<string | false | null | undefined>) => parts.filter(Boolean).join(" ");

/* ------------------------------------------------------------------ */
/* Header                                                              */
/* ------------------------------------------------------------------ */

export const RspHeader: React.FC<{
  title: string;
  subtitle: string;
  actions: React.ReactNode;
  adornment?: React.ReactNode;
  testId?: string;
  ariaLabel?: string;
}> = ({ title, subtitle, actions, adornment, testId = "recipe-studio-header", ariaLabel = "Recipe Studio actions" }) => (
  <header className="rsp-header" data-testid={testId}>
    <div className="rsp-header__main">
      <h1 className="rsp-title">{title}</h1>
      <p className="rsp-subtitle">{subtitle}</p>
      {adornment ? <div className="rsp-adornment">{adornment}</div> : null}
    </div>
    <div className="rsp-actions" role="toolbar" aria-label={ariaLabel}>
      {actions}
    </div>
  </header>
);

interface RspIconButtonProps {
  label: string;
  active?: boolean;
  expanded?: boolean;
  controlsId?: string;
  testId?: string;
  onClick: () => void;
  children: React.ReactNode;
}

export const RspIconButton: React.FC<RspIconButtonProps> = ({ label, active, expanded, controlsId, testId, onClick, children }) => (
  <button
    type="button"
    className={cx("rsp-glass", "rsp-icon-btn", active && "is-active")}
    title={label}
    aria-label={label}
    aria-pressed={active}
    aria-expanded={expanded}
    aria-controls={controlsId}
    data-testid={testId}
    onClick={onClick}
  >
    {children}
  </button>
);

interface RspButtonProps {
  tone: "glass" | "ai" | "primary";
  testId?: string;
  disabled?: boolean;
  title?: string;
  onClick: () => void;
  children: React.ReactNode;
}

export const RspButton: React.FC<RspButtonProps> = ({ tone, testId, disabled, title, onClick, children }) => (
  <button
    type="button"
    className={cx("rsp-btn", tone === "glass" && "rsp-glass rsp-btn--glass", tone === "ai" && "rsp-btn--ai", tone === "primary" && "rsp-btn--primary")}
    data-testid={testId}
    disabled={disabled}
    title={title}
    onClick={onClick}
  >
    {children}
  </button>
);

export const RspMenuItem: React.FC<{
  testId?: string;
  tone?: "sky" | "teal" | "emerald" | "slate";
  icon: React.ReactNode;
  onClick: () => void;
  children: React.ReactNode;
}> = ({ testId, tone = "slate", icon, onClick, children }) => (
  <button type="button" role="menuitem" className="rsp-menu__item" data-testid={testId} onClick={onClick}>
    <span className={`rsp-menu__icon rsp-menu__icon--${tone}`} aria-hidden="true">
      {icon}
    </span>
    <span>{children}</span>
  </button>
);

/** Actions dropdown. Children stay mounted so hidden file inputs and import dialogs keep their state. */
export const RspActionsMenu: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return undefined;
    const onPointer = (event: MouseEvent) => {
      if (rootRef.current && !rootRef.current.contains(event.target as Node)) setOpen(false);
    };
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };
    document.addEventListener("mousedown", onPointer);
    document.addEventListener("keydown", onKey);
    return () => {
      document.removeEventListener("mousedown", onPointer);
      document.removeEventListener("keydown", onKey);
    };
  }, [open]);

  return (
    <div className="rsp-menu" ref={rootRef}>
      <RspButton tone="glass" testId="recipe-actions-menu" onClick={() => setOpen((value) => !value)}>
        <SlidersHorizontal size={14} aria-hidden="true" />
        <span>Actions</span>
        <ChevronDown size={13} aria-hidden="true" />
      </RspButton>
      <div className="rsp-menu__panel" role="menu" hidden={!open} onClick={() => setOpen(false)}>
        {children}
      </div>
    </div>
  );
};

/* ------------------------------------------------------------------ */
/* Bento KPI ribbon                                                    */
/* ------------------------------------------------------------------ */

export type RspKpiKey = "total" | "active" | "draft" | "pending" | "failedPos" | "consumption" | "avgCost";

interface RspKpiRibbonProps {
  values: Record<RspKpiKey, string>;
  hints: Partial<Record<RspKpiKey, string>>;
  selected: Partial<Record<RspKpiKey, boolean>>;
  failedPosCount: number;
  testId?: string;
  onSelect: (key: RspKpiKey) => void;
}

const KPI_TEST_IDS: Record<RspKpiKey, string> = {
  total: "kpi-total-recipes",
  active: "kpi-active-recipes",
  draft: "kpi-draft-recipes",
  pending: "kpi-pending-approval",
  failedPos: "kpi-failed-pos",
  consumption: "kpi-posted-consumption",
  avgCost: "kpi-average-cost",
};

const Stat: React.FC<{
  kpi: RspKpiKey;
  value: string;
  label: string;
  hint?: string;
  size?: "lg" | "sm" | "xs";
  tone?: "rose" | "slate";
  selected?: boolean;
  split?: boolean;
  onSelect: (key: RspKpiKey) => void;
}> = ({ kpi, value, label, hint, size = "lg", tone, selected, split, onSelect }) => (
  <button
    type="button"
    className={cx("rsp-stat", split && "rsp-stat--split-right", selected && "is-selected")}
    aria-pressed={selected}
    data-testid={KPI_TEST_IDS[kpi]}
    onClick={() => onSelect(kpi)}
  >
    <span className="rsp-stat__row">
      <span className={cx("rsp-stat__value", size === "sm" && "rsp-stat__value--sm", size === "xs" && "rsp-stat__value--xs", tone === "rose" && "rsp-stat__value--rose", tone === "slate" && "rsp-stat__value--slate")}>
        {value}
      </span>
      <span className={cx("rsp-stat__label", tone === "rose" && "rsp-stat__label--rose", tone === "slate" && "rsp-stat__label--slate")}>{label}</span>
    </span>
    {hint ? <span className={cx("rsp-stat__hint", tone === "rose" && "rsp-stat__hint--rose")}>{hint}</span> : null}
  </button>
);

export const RspKpiRibbon: React.FC<RspKpiRibbonProps> = ({ values, hints, selected, failedPosCount, testId, onSelect }) => (
  <section className="rsp-bento-grid" aria-label="Recipe KPIs" data-testid={testId}>
    <article className="rsp-glass rsp-bento">
      <div className="rsp-bento__head">
        <span className="rsp-bento__eyebrow">
          <LayoutGrid size={13} aria-hidden="true" /> Total culinary portfolio
        </span>
        <button type="button" className="rsp-bento__link" onClick={() => onSelect("total")}>
          View all
        </button>
      </div>
      <div className="rsp-bento__body">
        <Stat kpi="total" value={values.total} label="Master recipes" hint={hints.total} selected={selected.total} onSelect={onSelect} />
        <div className="rsp-pill-row">
          <button
            type="button"
            className={cx("rsp-status-pill rsp-status-pill--emerald", selected.active && "is-selected")}
            data-testid="kpi-active-recipes"
            aria-pressed={selected.active}
            onClick={() => onSelect("active")}
          >
            <span className="rsp-status-pill__dot" aria-hidden="true" />
            {values.active} Active
          </button>
          <button
            type="button"
            className={cx("rsp-status-pill rsp-status-pill--slate", selected.draft && "is-selected")}
            data-testid="kpi-draft-recipes"
            aria-pressed={selected.draft}
            onClick={() => onSelect("draft")}
          >
            <span className="rsp-status-pill__dot" aria-hidden="true" />
            {values.draft} Draft
          </button>
          <button
            type="button"
            className={cx("rsp-status-pill rsp-status-pill--slate", selected.pending && "is-selected")}
            data-testid="kpi-pending-approval"
            aria-pressed={selected.pending}
            onClick={() => onSelect("pending")}
          >
            <span className="rsp-status-pill__dot" aria-hidden="true" />
            {values.pending} Pending
          </button>
        </div>
      </div>
    </article>

    <article className="rsp-glass rsp-bento">
      <div className="rsp-bento__head">
        <span className="rsp-bento__eyebrow rsp-bento__eyebrow--teal">
          <TrendingUp size={13} aria-hidden="true" /> Real-time costing engine
        </span>
        <span className="rsp-bento__tag">Live</span>
      </div>
      <div className="rsp-bento__body--split">
        <Stat
          kpi="avgCost"
          value={values.avgCost}
          label="Avg portion cost"
          hint={hints.avgCost}
          size="sm"
          selected={selected.avgCost}
          onSelect={onSelect}
        />
        <Stat
          kpi="consumption"
          value={values.consumption}
          label="Posted consumption today"
          hint={hints.consumption}
          size="sm"
          tone="slate"
          split
          selected={selected.consumption}
          onSelect={onSelect}
        />
      </div>
    </article>

    <article className={cx("rsp-glass rsp-bento", failedPosCount > 0 && "rsp-bento--alert")}>
      <div className="rsp-bento__head">
        <span className="rsp-bento__eyebrow rsp-bento__eyebrow--rose">
          {failedPosCount > 0 ? <span className="rsp-ping" aria-hidden="true" /> : <CheckCircle2 size={13} aria-hidden="true" />}
          POS sync status
        </span>
        <span className="rsp-bento__note">{failedPosCount > 0 ? "Action required" : "All clear"}</span>
      </div>
      <div className="rsp-bento__body--split">
        <Stat
          kpi="failedPos"
          value={values.failedPos}
          label="Failed POS items"
          hint={hints.failedPos}
          tone={failedPosCount > 0 ? "rose" : undefined}
          selected={selected.failedPos}
          onSelect={onSelect}
        />
      </div>
    </article>
  </section>
);

/* ------------------------------------------------------------------ */
/* Command & filter bar                                                */
/* ------------------------------------------------------------------ */

export interface RspChip {
  id: string;
  label: string;
  active: boolean;
  onClick: () => void;
  /** Chips with a new group value get a thin separator in front. */
  group: string;
}

export interface RspSortOption {
  value: string;
  label: string;
}

interface RspCommandBarProps {
  search: string;
  onSearch: (value: string) => void;
  searchRef: React.RefObject<HTMLInputElement | null>;
  sort: string;
  sortOptions: RspSortOption[];
  onSort: (value: string) => void;
  viewMode: RspViewMode;
  onViewMode: (mode: RspViewMode) => void;
  chips: RspChip[];
  advancedCount: number;
  advancedTestId?: string;
  advancedExpanded?: boolean;
  testId?: string;
  onAdvanced: () => void;
}

const VIEW_MODES: Array<{ id: RspViewMode; label: string; Icon: typeof LayoutGrid }> = [
  { id: "grid", label: "Grid", Icon: LayoutGrid },
  { id: "compact", label: "Compact", Icon: Grid3x3 },
  { id: "list", label: "List", Icon: List },
];

export const RspCommandBar: React.FC<RspCommandBarProps> = ({
  search,
  onSearch,
  searchRef,
  sort,
  sortOptions,
  onSort,
  viewMode,
  onViewMode,
  chips,
  advancedCount,
  advancedTestId,
  advancedExpanded,
  testId,
  onAdvanced,
}) => {
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchRef.current?.focus();
      }
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [searchRef]);

  return (
    <section className="rsp-glass rsp-command" aria-label="Recipe filters" data-testid={testId}>
      <div className="rsp-command__row">
        <div className="rsp-search">
          <Search size={16} className="rsp-search__icon" aria-hidden="true" />
          <input
            ref={searchRef}
            type="search"
            className="rsp-search__input"
            placeholder="Search recipes, codes, POS items..."
            aria-label="Search recipes"
            value={search}
            data-testid="recipe-search"
            onChange={(event) => onSearch(event.target.value)}
          />
          <div className="rsp-search__end">
            {search ? (
              <button type="button" className="rsp-search__clear" onClick={() => onSearch("")} aria-label="Clear search">
                <X size={13} aria-hidden="true" />
              </button>
            ) : null}
            <kbd className="rsp-kbd">Ctrl K</kbd>
          </div>
        </div>
        <div className="rsp-command__controls">
          <label className="rsp-select-wrap">
            <span className="sr-only" style={{ position: "absolute", width: 1, height: 1, overflow: "hidden", clip: "rect(0 0 0 0)" }}>
              Sort recipes
            </span>
            <select className="rsp-select" value={sort} onChange={(event) => onSort(event.target.value)} data-testid="recipe-sort">
              {sortOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
            <ChevronDown size={13} aria-hidden="true" />
          </label>
          <div className="rsp-segmented" role="group" aria-label="Catalogue view">
            {VIEW_MODES.map(({ id, label, Icon }) => (
              <button
                key={id}
                type="button"
                className={cx("rsp-segmented__btn", viewMode === id && "is-active")}
                aria-pressed={viewMode === id}
                data-testid={`recipe-view-${id}`}
                onClick={() => onViewMode(id)}
              >
                <Icon size={14} aria-hidden="true" />
                <span>{label}</span>
              </button>
            ))}
          </div>
        </div>
      </div>
      <div className="rsp-chips" role="group" aria-label="Quick filters">
        {chips.map((chip, index) => (
          <React.Fragment key={chip.id}>
            {index > 0 && chips[index - 1].group !== chip.group ? <span className="rsp-chip__sep" aria-hidden="true" /> : null}
            <button type="button" className={cx("rsp-chip", chip.active && "is-active")} aria-pressed={chip.active} onClick={chip.onClick}>
              {chip.label}
            </button>
          </React.Fragment>
        ))}
        <span className="rsp-chip__sep" aria-hidden="true" />
        <button type="button" className="rsp-chip rsp-chip--ghost" onClick={onAdvanced} data-testid={advancedTestId} aria-expanded={advancedExpanded}>
          <Filter size={12} aria-hidden="true" />
          Advanced filters{advancedCount > 0 ? ` (${advancedCount})` : ""}
        </button>
      </div>
    </section>
  );
};

export const RspFilterBanner: React.FC<{ text: string; onReset: () => void }> = ({ text, onReset }) => (
  <div className="rsp-banner" role="status" data-testid="recipe-filter-banner">
    <span className="rsp-banner__text">
      <Filter size={13} aria-hidden="true" />
      <span>{text}</span>
    </span>
    <button type="button" className="rsp-banner__reset" onClick={onReset}>
      Reset filters
    </button>
  </div>
);

/* ------------------------------------------------------------------ */
/* Catalogue (grid cards, compact cards, list rows)                    */
/* ------------------------------------------------------------------ */

const statusTone = (status: string): "success" | "warning" | "danger" | "info" | "neutral" => {
  switch (status) {
    case "ACTIVE":
    case "APPROVED":
      return "success";
    case "PENDING_APPROVAL":
      return "warning";
    case "REJECTED":
      return "danger";
    case "DRAFT":
      return "info";
    default:
      return "neutral";
  }
};

const titleCase = (value: string) =>
  value
    .replace(/_/g, " ")
    .toLowerCase()
    .replace(/\b\w/g, (letter) => letter.toUpperCase());

const portionCostOf = (row: SilaRecipeListItem): number | null =>
  row.costComplete || row.activeVersion > 0 ? (row.costPerServing ?? costPerServing(row.totalCost, row.servingQty)) : null;

const typeLabel = (row: SilaRecipeListItem) =>
  row.recipeFormat ? titleCase(row.recipeFormat) : row.productClass ? formatProductClassLabel(row.productClass) : "";

const metaLine = (row: SilaRecipeListItem) =>
  [typeLabel(row), row.familyName || row.category || "", `${row.ingredientCount} ingr.`, `v${row.version}`].filter(Boolean);

const posOf = (row: SilaRecipeListItem) => row.posItem || row.posCode || "";

const RspStatusBadge: React.FC<{ status: string }> = ({ status }) => (
  <span className={`rsp-badge rsp-badge--${statusTone(status)}`}>{recipeLabel(status)}</span>
);

const monogramOf = (name: string) => {
  const words = name.replace(/[^A-Za-z0-9 ]/g, " ").split(/\s+/).filter(Boolean);
  if (words.length === 0) return "--";
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return (words[0][0] + words[1][0]).toUpperCase();
};

const meshOf = (row: SilaRecipeListItem) => {
  const format = (row.recipeFormat ?? "").toUpperCase();
  if (format === "SUB_RECIPE") return "rsp-mesh--amber";
  if (format === "BATCH_RECIPE") return "rsp-mesh--teal";
  return "rsp-mesh--spirits";
};

/** Card / drawer visual: real photograph when present, otherwise the prototype mesh with a monogram tile. */
const RspMedia: React.FC<{ row: SilaRecipeListItem }> = ({ row }) =>
  row.hasPicture ? (
    <SilaRecipePicture
      recipeId={row.id}
      hasPicture
      placeholderTitle={row.name}
      placeholderCategory={row.category}
      placeholderFamily={row.familyName}
      variant="catalog"
      locked
    />
  ) : (
    <div className={cx("rsp-placeholder", meshOf(row))}>
      <div className="rsp-placeholder__tile">
        <ChefHat size={18} aria-hidden="true" />
        <span>{monogramOf(row.name)}</span>
      </div>
      <span className="rsp-placeholder__family">{row.familyName || row.category || "Recipe"}</span>
    </div>
  );

/** 44px rounded thumbnail used by compact cards and list rows (prototype `w-11 h-11 rounded-xl`). */
const RspThumb: React.FC<{ row: SilaRecipeListItem }> = ({ row }) => (
  <div className={cx("rsp-thumb", !row.hasPicture && meshOf(row))}>
    {row.hasPicture ? (
      <SilaRecipePicture
        recipeId={row.id}
        hasPicture
        placeholderTitle={row.name}
        placeholderCategory={row.category}
        placeholderFamily={row.familyName}
        variant="catalog"
        locked
      />
    ) : (
      <span className="rsp-thumb__mono">{monogramOf(row.name)}</span>
    )}
  </div>
);

interface RspCatalogProps {
  rows: SilaRecipeListItem[];
  viewMode: RspViewMode;
  canManage: boolean;
  onOpen: (row: SilaRecipeListItem) => void;
  onEdit: (row: SilaRecipeListItem) => void;
  onDelete: (row: SilaRecipeListItem) => void;
}

const canEditRow = (row: SilaRecipeListItem, canManage: boolean) =>
  canManage && row.status !== "INACTIVE" && row.status !== "PENDING_APPROVAL";

const canDeleteRow = (row: SilaRecipeListItem, canManage: boolean) =>
  canManage && (row.status === "DRAFT" || row.status === "INACTIVE");

const activateOnKey = (action: () => void) => (event: React.KeyboardEvent) => {
  if (event.target !== event.currentTarget) return;
  if (event.key === "Enter" || event.key === " ") {
    event.preventDefault();
    action();
  }
};

const RspCard: React.FC<{ row: SilaRecipeListItem } & Omit<RspCatalogProps, "rows" | "viewMode">> = ({
  row,
  canManage,
  onOpen,
  onEdit,
  onDelete,
}) => {
  const cost = portionCostOf(row);
  const pos = posOf(row);
  const stop = (action: () => void) => (event: React.MouseEvent) => {
    event.stopPropagation();
    action();
  };
  return (
    <article
      className="rsp-glass rsp-card"
      role="listitem"
      tabIndex={0}
      data-testid="recipe-card"
      aria-label={`${row.name}, ${recipeLabel(row.status)}`}
      onClick={() => onOpen(row)}
      onKeyDown={activateOnKey(() => onOpen(row))}
    >
      <div className="rsp-card__media">
        <RspMedia row={row} />
        <div className="rsp-card__status">
          <RspStatusBadge status={row.status} />
        </div>
        <div className="rsp-card__quick">
          <button type="button" className="rsp-quick-btn" title="Quick view" aria-label={`Quick view ${row.name}`} onClick={stop(() => onOpen(row))}>
            <Eye size={14} aria-hidden="true" />
          </button>
          {canEditRow(row, canManage) ? (
            <button type="button" className="rsp-quick-btn" title="Edit recipe" aria-label={`Edit ${row.name}`} onClick={stop(() => onEdit(row))}>
              <Pencil size={14} aria-hidden="true" />
            </button>
          ) : null}
          {canDeleteRow(row, canManage) ? (
            <button
              type="button"
              className="rsp-quick-btn rsp-quick-btn--danger"
              title={row.status === "DRAFT" ? "Delete draft" : "Delete permanently"}
              aria-label={`Delete ${row.name}`}
              onClick={stop(() => onDelete(row))}
            >
              <Trash2 size={14} aria-hidden="true" />
            </button>
          ) : null}
        </div>
      </div>
      <div className="rsp-card__body">
        <h3 className="rsp-card__title" title={row.name}>
          {row.name}
        </h3>
        <p className="rsp-card__meta" title={`${row.recipeCode} ${metaLine(row).join(" · ")}`}>
          {metaLine(row).map((part, index) => (
            <React.Fragment key={`${part}-${index}`}>
              {index > 0 ? <span className="rsp-dot">&bull;</span> : null}
              {part}
            </React.Fragment>
          ))}
        </p>
      </div>
      <div className="rsp-card__foot">
        <div>
          <span className="rsp-foot-label">Portion cost</span>
          <span className={cx("rsp-foot-value", cost == null && "rsp-foot-value--pending")}>
            {cost == null ? "Cost pending" : formatCost(cost, row.currency)}
          </span>
        </div>
        <div style={{ textAlign: "right" }}>
          <span className="rsp-foot-label">POS reference</span>
          {pos ? (
            <span className="rsp-pos-pill" title={pos}>
              {pos}
            </span>
          ) : (
            <span className="rsp-pos-none">Unmapped</span>
          )}
        </div>
      </div>
    </article>
  );
};

const RspRow: React.FC<{ row: SilaRecipeListItem } & Omit<RspCatalogProps, "rows" | "viewMode">> = ({ row, onOpen }) => {
  const cost = portionCostOf(row);
  const pos = posOf(row);
  return (
    <article
      className="rsp-glass rsp-row"
      role="listitem"
      tabIndex={0}
      data-testid="recipe-row"
      aria-label={`${row.name}, ${recipeLabel(row.status)}`}
      onClick={() => onOpen(row)}
      onKeyDown={activateOnKey(() => onOpen(row))}
    >
      <div className="rsp-row__main">
        <RspThumb row={row} />
        <div style={{ minWidth: 0 }}>
          <div className="rsp-row__head">
            <h3 className="rsp-row__title" title={row.name}>
              {row.name}
            </h3>
            <RspStatusBadge status={row.status} />
          </div>
          <p className="rsp-row__meta">
            {row.recipeCode} <span className="rsp-dot">&bull;</span> {metaLine(row).join(" • ")}
          </p>
        </div>
      </div>
      <div className="rsp-row__side">
        <div className="rsp-row__pos">
          <span className="rsp-foot-label">POS reference</span>
          {pos ? (
            <span className="rsp-pos-pill" title={pos}>
              {pos}
            </span>
          ) : (
            <span className="rsp-pos-none">Unmapped</span>
          )}
        </div>
        <div className="rsp-row__cost">
          <span className="rsp-foot-label">Portion cost</span>
          <span className={cx("rsp-foot-value", cost == null && "rsp-foot-value--pending")}>
            {cost == null ? "Cost pending" : formatCost(cost, row.currency)}
          </span>
        </div>
        <ChevronRight size={16} className="rsp-row__chev" aria-hidden="true" />
      </div>
    </article>
  );
};

const RspCompactCard: React.FC<{ row: SilaRecipeListItem; onOpen: (row: SilaRecipeListItem) => void }> = ({ row, onOpen }) => {
  const cost = portionCostOf(row);
  const pos = posOf(row);
  const tone = statusTone(row.status);
  return (
    <article
      className="rsp-glass rsp-compact"
      role="listitem"
      tabIndex={0}
      data-testid="recipe-compact-card"
      aria-label={`${row.name}, ${recipeLabel(row.status)}`}
      onClick={() => onOpen(row)}
      onKeyDown={activateOnKey(() => onOpen(row))}
    >
      <div className="rsp-compact__head">
        <div className="rsp-compact__id">
          <RspThumb row={row} />
          <div className="rsp-compact__text">
            <h3 className="rsp-compact__title" title={row.name}>
              {row.name}
            </h3>
            <p className="rsp-compact__meta">
              {[typeLabel(row), `${row.ingredientCount} ingr`, `${formatQty(row.servingQty)} ${row.servingUom}`].filter(Boolean).join(" • ")}
            </p>
          </div>
        </div>
        <span className={`rsp-compact__dot rsp-compact__dot--${tone}`} title={recipeLabel(row.status)} aria-label={recipeLabel(row.status)} />
      </div>
      <div className="rsp-compact__foot">
        <span className="rsp-compact__cost">
          {cost == null ? "Cost pending" : formatCost(cost, row.currency)} <span>/ por</span>
        </span>
        <span className="rsp-compact__pos" title={pos}>
          {pos || "—"}
        </span>
      </div>
    </article>
  );
};

export const RspCatalog: React.FC<RspCatalogProps> = (props) => {
  const { rows, viewMode, canManage, onOpen, onEdit, onDelete } = props;
  return (
    <div className={cx("rsp-grid", viewMode === "compact" && "rsp-grid--compact", viewMode === "list" && "rsp-grid--list")} role="list" data-testid="recipe-catalog">
      {rows.map((row) =>
        viewMode === "list" ? (
          <RspRow key={row.id} row={row} canManage={canManage} onOpen={onOpen} onEdit={onEdit} onDelete={onDelete} />
        ) : viewMode === "compact" ? (
          <RspCompactCard key={row.id} row={row} onOpen={onOpen} />
        ) : (
          <RspCard key={row.id} row={row} canManage={canManage} onOpen={onOpen} onEdit={onEdit} onDelete={onDelete} />
        ),
      )}
    </div>
  );
};

export const RspEmpty: React.FC<{ title: string; hint: string; action?: React.ReactNode }> = ({ title, hint, action }) => (
  <div className="rsp-glass rsp-empty" role="status">
    <MoreHorizontal size={28} color="#94a3b8" aria-hidden="true" />
    <h3>{title}</h3>
    <p>{hint}</p>
    {action ? <div style={{ marginTop: "1rem" }}>{action}</div> : null}
  </div>
);

interface RspFooterProps {
  shown: number;
  total: number;
  page: number;
  pageCount: number;
  onPage: (page: number) => void;
}

export const RspFooter: React.FC<RspFooterProps> = ({ shown, total, page, pageCount, onPage }) => (
  <footer className="rsp-footer" data-testid="recipe-footer">
    <span>
      Showing {shown} of {total} culinary formulations &bull; Page {page}
      {pageCount > 1 ? ` of ${pageCount}` : ""}
    </span>
    <div className="rsp-footer__pager">
      <button type="button" className="rsp-pager-btn" disabled={page <= 1} onClick={() => onPage(page - 1)}>
        Previous
      </button>
      <span className="rsp-pager-btn rsp-pager-btn--current" aria-current="page">
        {page}
      </span>
      <button type="button" className="rsp-pager-btn" disabled={page >= pageCount} onClick={() => onPage(page + 1)}>
        Next
      </button>
    </div>
  </footer>
);

/* ------------------------------------------------------------------ */
/* Quick-view slide-over                                               */
/* ------------------------------------------------------------------ */

interface RspQuickViewProps {
  row: SilaRecipeListItem | null;
  canManage: boolean;
  onClose: () => void;
  onOpenFull: (row: SilaRecipeListItem) => void;
  onEdit: (row: SilaRecipeListItem) => void;
  onOpenPos?: () => void;
}

export const RspQuickView: React.FC<RspQuickViewProps> = ({ row, canManage, onClose, onOpenFull, onEdit, onOpenPos }) => {
  const [detail, setDetail] = useState<SilaRecipeDetail | null>(null);
  const [state, setState] = useState<"idle" | "loading" | "error">("idle");
  const [visible, setVisible] = useState(false);
  const recipeId = row?.id;

  useEffect(() => {
    if (!recipeId) {
      setVisible(false);
      return undefined;
    }
    const frame = window.requestAnimationFrame(() => setVisible(true));
    return () => window.cancelAnimationFrame(frame);
  }, [recipeId]);

  useEffect(() => {
    if (!recipeId) return undefined;
    let cancelled = false;
    setDetail(null);
    setState("loading");
    getRecipe(recipeId)
      .then((result) => {
        if (cancelled) return;
        setDetail(result);
        setState("idle");
      })
      .catch(() => {
        if (!cancelled) setState("error");
      });
    return () => {
      cancelled = true;
    };
  }, [recipeId]);

  useEffect(() => {
    if (!recipeId) return undefined;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [recipeId, onClose]);

  if (!row) return null;

  const cost = portionCostOf(row);
  const pos = posOf(row);
  const ingredients = detail?.ingredients ?? [];

  return (
    <>
      <div className={cx("rsp-drawer-backdrop", visible && "is-open")} onClick={onClose} aria-hidden="true" />
      <aside
        className={cx("rsp-drawer", visible && "is-open")}
        role="dialog"
        aria-modal="true"
        aria-label={`${row.name} quick view`}
        data-testid="recipe-quick-view"
      >
        <div className="rsp-drawer__head">
          <div className="rsp-drawer__head-left">
            <RspStatusBadge status={row.status} />
            <span className="rsp-drawer__version">
              {row.recipeCode} &bull; v{row.version}
            </span>
          </div>
          <button type="button" className="rsp-drawer-close" onClick={onClose} aria-label="Close quick view" data-testid="recipe-quick-view-close">
            <X size={18} aria-hidden="true" />
          </button>
        </div>

        <div className="rsp-drawer__body">
          <div className="rsp-drawer__hero">
            <RspMedia row={row} />
          </div>
          <div>
            <h2 className="rsp-drawer__title">{row.name}</h2>
            <p className="rsp-drawer__meta">{metaLine(row).join(" • ")}</p>
          </div>

          <div className="rsp-cost-grid">
            <div className="rsp-cost-tile">
              <span className="rsp-cost-tile__label">Portion cost</span>
              <div className="rsp-cost-tile__value">{cost == null ? "Pending" : formatCost(cost, row.currency)}</div>
            </div>
            <div className="rsp-cost-tile rsp-cost-tile--teal">
              <span className="rsp-cost-tile__label">Menu price</span>
              <div className="rsp-cost-tile__value">{row.menuPrice == null ? "—" : formatCost(row.menuPrice, row.currency)}</div>
            </div>
            <div className="rsp-cost-tile rsp-cost-tile--sky">
              <span className="rsp-cost-tile__label">Food cost %</span>
              <div className="rsp-cost-tile__value">{formatPercent(row.costPercent)}</div>
            </div>
          </div>

          {pos ? (
            <div className="rsp-pos-box rsp-pos-box--ok">
              <div>
                <h4 className="rsp-pos-box__title">
                  <CheckCircle2 size={14} aria-hidden="true" /> POS mapping active
                </h4>
                <p className="rsp-pos-box__note">{pos}</p>
              </div>
            </div>
          ) : (
            <div className="rsp-pos-box">
              <div>
                <h4 className="rsp-pos-box__title">
                  <AlertTriangle size={14} aria-hidden="true" /> POS mapping status
                </h4>
                <p className="rsp-pos-box__note">No POS item mapped to this recipe yet.</p>
              </div>
              {onOpenPos ? (
                <button type="button" className="rsp-pos-box__btn" onClick={onOpenPos}>
                  Review POS
                </button>
              ) : null}
            </div>
          )}

          <section aria-label="Ingredient breakdown">
            <div className="rsp-bom__head">
              <h4 className="rsp-bom__title">Ingredient breakdown</h4>
              <span className="rsp-bom__hint">{row.ingredientCount} lines</span>
            </div>
            <ul className="rsp-bom__list">
              {state === "loading" ? <li className="rsp-bom__empty">Loading ingredients...</li> : null}
              {state === "error" ? <li className="rsp-bom__empty">Ingredients could not be loaded. Open the full recipe to review them.</li> : null}
              {state === "idle" && ingredients.length === 0 ? <li className="rsp-bom__empty">No ingredients on this version.</li> : null}
              {ingredients.map((ingredient) => (
                <li className="rsp-bom__item" key={ingredient.id}>
                  <div>
                    <div className="rsp-bom__name">{ingredient.itemName}</div>
                    <div className="rsp-bom__qty">
                      {formatQty(ingredient.quantity)} {ingredient.uom}
                    </div>
                  </div>
                  <span className="rsp-bom__cost">{formatCost(ingredient.cost, ingredient.currency ?? row.currency)}</span>
                </li>
              ))}
            </ul>
          </section>
        </div>

        <div className="rsp-drawer__foot">
          <button type="button" className="rsp-link-btn" onClick={() => onOpenFull(row)} data-testid="recipe-quick-view-open">
            <ArrowUpRight size={13} aria-hidden="true" /> Open full recipe
          </button>
          <div className="rsp-drawer__head-right">
            {canEditRow(row, canManage) ? (
              <button type="button" className="rsp-btn rsp-btn--primary" onClick={() => onEdit(row)}>
                <Pencil size={13} aria-hidden="true" /> Edit recipe
              </button>
            ) : null}
          </div>
        </div>
      </aside>
    </>
  );
};
