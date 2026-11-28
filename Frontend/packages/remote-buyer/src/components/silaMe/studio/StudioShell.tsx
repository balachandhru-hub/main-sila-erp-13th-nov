import React, { useEffect } from "react";
import { LayoutGrid, Maximize2, Minimize2, Sparkles, ChevronDown, ChevronUp } from "lucide-react";
import "./SilaStudioDesignSystem.css";

export interface StudioShellProps {
  children: React.ReactNode;
  className?: string;
  fullscreen?: boolean;
  fullscreenTitle?: string;
  onExitFullscreen?: () => void;
  testId?: string;
  ariaLabel?: string;
}

export const StudioShell: React.FC<StudioShellProps> = ({
  children,
  className = "",
  fullscreen = false,
  fullscreenTitle,
  onExitFullscreen,
  testId,
  ariaLabel,
}) => {
  useEffect(() => {
    if (!fullscreen || !onExitFullscreen) return;
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") onExitFullscreen();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [fullscreen, onExitFullscreen]);

  return (
    <div
      className={`sila-me sila-studio-shell ${fullscreen ? "is-fullscreen" : ""} ${className}`.trim()}
      data-testid={testId}
      aria-label={ariaLabel}
    >
      {fullscreen && (
        <div className="sila-studio-fullscreen-bar" role="region" aria-label="Full-Screen Intelligence Mode">
          <span className="sila-studio-fullscreen-bar__title">
            <Maximize2 size={15} aria-hidden="true" />
            <span>{fullscreenTitle || "Full-Screen Analytical Mode"}</span>
          </span>
          {onExitFullscreen && (
            <button
              type="button"
              className="sila-studio-btn sila-studio-btn--sm"
              onClick={onExitFullscreen}
            >
              <Minimize2 size={14} aria-hidden="true" />
              <span>Exit Full Screen</span>
            </button>
          )}
        </div>
      )}
      {children}
    </div>
  );
};

export interface StudioOverviewButtonProps {
  active: boolean;
  onClick: () => void;
  tooltip?: string;
  ariaLabel?: string;
  testId?: string;
  /** Custom glyph (defaults to the dashboard grid icon). */
  icon?: React.ReactNode;
}

export const StudioOverviewButton: React.FC<StudioOverviewButtonProps> = ({
  active,
  onClick,
  tooltip = "Return to Overview",
  ariaLabel = "Overview",
  testId = "studio-overview-btn",
  icon,
}) => (
  <button
    type="button"
    className={`sila-studio-overview-btn ${active ? "is-active" : ""}`}
    onClick={onClick}
    title={tooltip}
    aria-label={ariaLabel}
    aria-pressed={active}
    data-testid={testId}
  >
    {icon ?? <LayoutGrid size={18} strokeWidth={2} aria-hidden="true" />}
  </button>
);

export interface StudioIntelligenceToggleProps {
  label?: string;
  expanded: boolean;
  onClick: () => void;
  controlsId?: string;
  icon?: React.ReactNode;
  testId?: string;
  /** Render as a square icon button (label kept for tooltip and screen readers). */
  iconOnly?: boolean;
}

export const StudioIntelligenceToggle: React.FC<StudioIntelligenceToggleProps> = ({
  label = "Culinary Intelligence",
  expanded,
  onClick,
  controlsId,
  icon,
  testId = "studio-intelligence-toggle",
  iconOnly = false,
}) =>
  iconOnly ? (
    <button
      type="button"
      className={`sila-studio-overview-btn sila-studio-intel-icon ${expanded ? "is-active" : ""}`}
      onClick={onClick}
      aria-expanded={expanded}
      aria-controls={controlsId}
      aria-label={label}
      title={label}
      data-testid={testId}
    >
      {icon ?? <Sparkles size={18} strokeWidth={2} aria-hidden="true" />}
    </button>
  ) : (
    <button
      type="button"
      className={`sila-studio-intel-toggle ${expanded ? "is-expanded" : ""}`}
      onClick={onClick}
      aria-expanded={expanded}
      aria-controls={controlsId}
      data-testid={testId}
    >
      {icon ?? <Sparkles size={14} strokeWidth={2} aria-hidden="true" />}
      <span>{label}</span>
      {expanded ? (
        <ChevronUp size={14} strokeWidth={2.2} aria-hidden="true" />
      ) : (
        <ChevronDown size={14} strokeWidth={2.2} aria-hidden="true" />
      )}
    </button>
  );

export interface StudioActionItem {
  id: string;
  label: string;
  icon?: React.ReactNode;
  variant?: "primary" | "secondary" | "ghost";
  disabled?: boolean;
  title?: string;
  onClick: () => void;
  testId?: string;
}

export interface StudioActionBarProps {
  children?: React.ReactNode;
  actions?: StudioActionItem[];
  rightSlot?: React.ReactNode;
  ariaLabel?: string;
}

export const StudioActionBar: React.FC<StudioActionBarProps> = ({
  children,
  actions,
  rightSlot,
  ariaLabel = "Workspace actions",
}) => (
  <div className="sila-studio-action-bar" role="toolbar" aria-label={ariaLabel}>
    <div className="sila-studio-action-bar__group">
      {actions
        ? actions.map((act) => (
            <button
              key={act.id}
              type="button"
              disabled={act.disabled}
              title={act.title}
              data-testid={act.testId}
              className={`sila-studio-btn ${
                act.variant === "primary"
                  ? "sila-studio-btn--primary"
                  : act.variant === "ghost"
                    ? "sila-studio-btn--ghost"
                    : "sila-studio-btn--secondary"
              }`}
              onClick={act.onClick}
            >
              {act.icon}
              <span>{act.label}</span>
            </button>
          ))
        : children}
    </div>
    {rightSlot && <div className="sila-studio-action-bar__group">{rightSlot}</div>}
  </div>
);

export interface StudioHeroPanelProps {
  title: string;
  subtitle?: string;
  titleAdjacent?: React.ReactNode;
  titleAdornment?: React.ReactNode;
  metaSlot?: React.ReactNode;
  topRightSlot?: React.ReactNode;
  topRight?: React.ReactNode;
  overviewButton?: StudioOverviewButtonProps;
  actions?: React.ReactNode;
  actionsRightSlot?: React.ReactNode;
  testId?: string;
}

export const StudioHeroPanel: React.FC<StudioHeroPanelProps> = ({
  title,
  subtitle,
  titleAdjacent,
  titleAdornment,
  metaSlot,
  topRightSlot,
  topRight,
  overviewButton,
  actions,
  actionsRightSlot,
  testId,
}) => {
  const rightContent = topRightSlot ?? topRight;
  const adjacentContent = titleAdjacent ?? titleAdornment;
  return (
    <header className="sila-studio-hero" data-testid={testId}>
      <div className="sila-studio-hero__top">
        <div className="sila-studio-hero__main">
          <div className="sila-studio-hero__title-row">
            <h1 className="sila-studio-hero__title">{title}</h1>
            {adjacentContent}
          </div>
          {subtitle && <p className="sila-studio-hero__subtitle">{subtitle}</p>}
          {metaSlot && <div className="sila-studio-hero__meta">{metaSlot}</div>}
        </div>
        {(rightContent || overviewButton) && (
          <div className="sila-studio-hero__top-right">
            {rightContent}
            {overviewButton && <StudioOverviewButton {...overviewButton} />}
          </div>
        )}
      </div>
      {(actions || actionsRightSlot) && (
        <StudioActionBar rightSlot={actionsRightSlot}>{actions}</StudioActionBar>
      )}
    </header>
  );
};
