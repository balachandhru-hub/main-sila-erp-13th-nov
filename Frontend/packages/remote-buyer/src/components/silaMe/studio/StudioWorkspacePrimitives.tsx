import React, { useEffect } from "react";
import { AlertTriangle, Inbox, Loader2, Maximize2, Minimize2, Search, SlidersHorizontal, X } from "lucide-react";

export interface ActiveFilterChip {
  key: string;
  label: string;
  onRemove: () => void;
}

export interface StudioFilterBarProps {
  searchValue?: string;
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;
  searchAriaLabel?: string;
  ariaLabel?: string;
  left?: React.ReactNode;
  children?: React.ReactNode;
  rightSlot?: React.ReactNode;
  right?: React.ReactNode;
  onOpenAdvancedFilters?: () => void;
  advancedFiltersActiveCount?: number;
  activeChips?: ActiveFilterChip[] | React.ReactNode;
  onClearAll?: () => void;
  testId?: string;
}

export const StudioFilterBar: React.FC<StudioFilterBarProps> = ({
  searchValue,
  onSearchChange,
  searchPlaceholder = "Search...",
  searchAriaLabel = "Search",
  ariaLabel = "Filters",
  left,
  children,
  rightSlot,
  right,
  onOpenAdvancedFilters,
  advancedFiltersActiveCount = 0,
  activeChips,
  onClearAll,
  testId = "studio-filter-bar",
}) => {
  const resolvedRight = rightSlot ?? right;
  const chipArray = Array.isArray(activeChips) ? activeChips : null;
  const hasChipNode = !Array.isArray(activeChips) && Boolean(activeChips);

  return (
    <section className="sila-studio-filter-bar" aria-label={ariaLabel} data-testid={testId}>
      <div className="sila-studio-filter-bar__row">
        {onSearchChange && (
          <div className="sila-studio-search-wrap">
            <Search size={15} className="sila-studio-search-wrap__icon" aria-hidden="true" />
            <input
              type="search"
              className="sila-studio-input sila-studio-input--search"
              value={searchValue ?? ""}
              placeholder={searchPlaceholder}
              aria-label={searchAriaLabel}
              onChange={(e) => onSearchChange(e.target.value)}
            />
          </div>
        )}
        {left}
        {children}
        {onOpenAdvancedFilters && (
          <button
            type="button"
            className="sila-studio-btn sila-studio-btn--sm"
            onClick={onOpenAdvancedFilters}
          >
            <SlidersHorizontal size={14} aria-hidden="true" />
            <span>
              Filters{advancedFiltersActiveCount > 0 ? ` (${advancedFiltersActiveCount})` : ""}
            </span>
          </button>
        )}
        {resolvedRight}
      </div>
      {chipArray && chipArray.length > 0 && (
        <div className="sila-studio-chips" aria-label="Active filters">
          {chipArray.map((chip) => (
            <span key={chip.key} className="sila-studio-chip">
              <span>{chip.label}</span>
              <button type="button" onClick={chip.onRemove} aria-label={`Remove filter ${chip.label}`}>
                <X size={12} aria-hidden="true" />
              </button>
            </span>
          ))}
          {onClearAll && (
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--ghost sila-studio-btn--sm"
              onClick={onClearAll}
            >
              Reset
            </button>
          )}
        </div>
      )}
      {hasChipNode && (
        <div className="sila-studio-chips" aria-label="Active filters">
          {activeChips}
        </div>
      )}
    </section>
  );
};

export interface StudioContentGridProps {
  children: React.ReactNode;
  columns?: number;
  className?: string;
}

export const StudioContentGrid: React.FC<StudioContentGridProps> = ({
  children,
  columns,
  className = "",
}) => (
  <div
    className={`sila-studio-card-grid ${className}`.trim()}
    style={columns ? { gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))` } : undefined}
  >
    {children}
  </div>
);

export interface StudioDataTableProps {
  children: React.ReactNode;
  ariaLabel?: string;
  className?: string;
}

export const StudioDataTable: React.FC<StudioDataTableProps> = ({
  children,
  ariaLabel,
  className = "",
}) => (
  <div className="sila-studio-table-wrap">
    <table className={`sila-studio-table ${className}`.trim()} aria-label={ariaLabel}>
      {children}
    </table>
  </div>
);

export interface StudioChartContainerProps {
  title: string;
  subtitle?: string;
  actions?: React.ReactNode;
  fullscreen?: boolean;
  fullScreen?: boolean;
  onToggleFullscreen?: () => void;
  onToggleFullScreen?: () => void;
  children: React.ReactNode;
  testId?: string;
}

export const StudioChartContainer: React.FC<StudioChartContainerProps> = ({
  title,
  subtitle,
  actions,
  fullscreen,
  fullScreen,
  onToggleFullscreen,
  onToggleFullScreen,
  children,
  testId,
}) => {
  const isFs = Boolean(fullscreen ?? fullScreen);
  const toggleFs = onToggleFullscreen ?? onToggleFullScreen;

  return (
    <section
      className={`sila-studio-panel ${isFs ? "sila-studio-shell is-fullscreen" : ""}`}
      data-testid={testId}
    >
      <div className="sila-studio-panel__header">
        <div>
          <h3 className="sila-studio-panel__title">{title}</h3>
          {subtitle && <p className="sila-studio-panel__subtitle">{subtitle}</p>}
        </div>
        <div style={{ display: "inline-flex", alignItems: "center", gap: "0.4rem", flexWrap: "wrap" }}>
          {actions}
          {toggleFs && (
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--sm"
              onClick={toggleFs}
              aria-label={isFs ? "Exit full screen" : "Maximize workspace"}
              title={isFs ? "Exit full screen" : "Maximize workspace"}
            >
              {isFs ? <Minimize2 size={14} aria-hidden="true" /> : <Maximize2 size={14} aria-hidden="true" />}
              <span>{isFs ? "Exit Full Screen" : "Maximize"}</span>
            </button>
          )}
        </div>
      </div>
      {children}
    </section>
  );
};

export interface StudioDrawerProps {
  isOpen?: boolean;
  open?: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  icon?: React.ReactNode;
  side?: "left" | "right";
  width?: number | "sm" | "md" | "lg";
  headerControls?: React.ReactNode;
  footer?: React.ReactNode;
  children: React.ReactNode;
  showBackdrop?: boolean;
  testId?: string;
}

const resolveDrawerWidth = (w: number | "sm" | "md" | "lg"): number => {
  if (typeof w === "number") return w;
  if (w === "sm") return 360;
  if (w === "lg") return 560;
  return 440;
};

export const StudioDrawer: React.FC<StudioDrawerProps> = ({
  isOpen,
  open,
  onClose,
  title,
  subtitle,
  icon,
  side = "right",
  width = 400,
  headerControls,
  footer,
  children,
  showBackdrop = true,
  testId,
}) => {
  const activeOpen = Boolean(isOpen ?? open);

  useEffect(() => {
    if (!activeOpen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [activeOpen, onClose]);

  if (!activeOpen) return null;

  return (
    <>
      {showBackdrop && (
        <div
          className="sila-studio-drawer-backdrop"
          onClick={onClose}
          aria-hidden="true"
        />
      )}
      <aside
        className={`sila-studio-drawer sila-studio-drawer--${side}`}
        style={{ width: `${resolveDrawerWidth(width)}px` }}
        role="dialog"
        aria-label={title}
        data-testid={testId}
      >
        <div className="sila-studio-drawer__head">
          <div>
            <h2 className="sila-studio-drawer__title">
              {icon}
              <span>{title}</span>
            </h2>
            {subtitle && (
              <p className="sila-studio-panel__subtitle" style={{ margin: "0.15rem 0 0" }}>
                {subtitle}
              </p>
            )}
          </div>
          <div className="sila-studio-drawer__controls">
            {headerControls}
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--sm"
              onClick={onClose}
              aria-label={`Close ${title}`}
              title="Close (Esc)"
            >
              <X size={14} aria-hidden="true" />
            </button>
          </div>
        </div>
        <div className="sila-studio-drawer__body">{children}</div>
        {footer && (
          <div style={{ padding: "0.75rem 0.95rem", borderTop: "1px solid var(--sila-studio-divider)" }}>
            {footer}
          </div>
        )}
      </aside>
    </>
  );
};

export interface StudioEmptyStateProps {
  title: string;
  description?: string;
  action?: React.ReactNode;
  icon?: React.ReactNode;
}

export const StudioEmptyState: React.FC<StudioEmptyStateProps> = ({
  title,
  description,
  action,
  icon,
}) => (
  <div className="sila-studio-panel" style={{ alignItems: "center", textAlign: "center", padding: "2rem 1rem" }}>
    <div style={{ color: "#087BB5" }}>{icon ?? <Inbox size={26} aria-hidden="true" />}</div>
    <h3 className="sila-studio-panel__title">{title}</h3>
    {description && <p className="sila-studio-panel__subtitle">{description}</p>}
    {action}
  </div>
);

export const StudioLoadingState: React.FC<{ message?: string }> = ({ message = "Loading workspace..." }) => (
  <div className="sila-studio-panel" style={{ alignItems: "center", textAlign: "center", padding: "2rem 1rem" }} role="status">
    <Loader2 size={22} className="animate-spin" color="#087BB5" aria-hidden="true" />
    <span className="sila-studio-panel__subtitle">{message}</span>
  </div>
);

export const StudioErrorState: React.FC<{
  title?: string;
  message: string;
  onRetry?: () => void;
}> = ({ title = "Unable to load workspace", message, onRetry }) => (
  <div className="sila-studio-panel" style={{ borderColor: "#f3cccc", background: "#fff8f8" }} role="alert">
    <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", color: "#c24141" }}>
      <AlertTriangle size={18} aria-hidden="true" />
      <h3 className="sila-studio-panel__title" style={{ color: "#c24141" }}>{title}</h3>
    </div>
    <p className="sila-studio-panel__subtitle">{message}</p>
    {onRetry && (
      <div>
        <button type="button" className="sila-studio-btn sila-studio-btn--sm" onClick={onRetry}>
          Try Again
        </button>
      </div>
    )}
  </div>
);
