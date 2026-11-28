import React from "react";
import { ChevronDown, Sparkles } from "lucide-react";

export type StudioFeatureBadge =
  | "Preview"
  | "Configuration Required"
  | "Not Enabled"
  | "Temporarily Unavailable";

export interface StudioSubNavItem {
  id: string;
  label: string;
  badge?: StudioFeatureBadge;
  statusBadge?: string;
  statusTone?: string;
  active?: boolean;
  tooltip?: string;
  disabled?: boolean;
  testId?: string;
}

export interface StudioTopNavGroup {
  id: string;
  label: string;
  icon?: React.ReactNode;
  items?: StudioSubNavItem[];
  hasDropdown?: boolean;
  active?: boolean;
  testId?: string;
}

export type StudioNavGroup = StudioTopNavGroup;

export interface StudioTopNavigationProps {
  id?: string;
  panelTitle?: string;
  title?: string;
  statusSlot?: React.ReactNode;
  rightSlot?: React.ReactNode;
  rightContent?: React.ReactNode;
  groups: StudioTopNavGroup[];
  activeGroupId?: string | null;
  activeItemId?: string | null;
  onSelectGroup: (group: StudioTopNavGroup) => void;
  onSelectItem?: (itemId: string) => void;
  ariaLabel?: string;
  wrappedInPanel?: boolean;
  children?: React.ReactNode;
  testId?: string;
}

export const StudioTopNavigation: React.FC<StudioTopNavigationProps> = ({
  id,
  panelTitle,
  title,
  statusSlot,
  rightSlot,
  rightContent,
  groups,
  activeGroupId,
  onSelectGroup,
  ariaLabel = "Module navigation",
  wrappedInPanel = true,
  children,
  testId,
}) => {
  const resolvedTitle = panelTitle ?? title;
  const resolvedStatus = statusSlot ?? rightSlot ?? rightContent;

  const navBar = (
    <div className="sila-studio-top-nav" role="tablist" aria-label={ariaLabel}>
      {groups.map((group) => {
        const active = activeGroupId !== undefined ? group.id === activeGroupId : Boolean(group.active);
        const hasMultiple = group.hasDropdown ?? (group.items ? group.items.length > 1 : false);
        return (
          <button
            key={group.id}
            type="button"
            role="tab"
            aria-selected={active}
            className={`sila-studio-nav-btn ${active ? "is-active" : ""}`}
            onClick={() => onSelectGroup(group)}
            data-testid={group.testId ?? `studio-nav-group-${group.id}`}
          >
            {group.icon}
            <span>{group.label}</span>
            {hasMultiple && <ChevronDown size={13} strokeWidth={2} aria-hidden="true" />}
          </button>
        );
      })}
    </div>
  );

  if (!wrappedInPanel) {
    return (
      <div id={id} data-testid={testId}>
        {navBar}
        {children}
      </div>
    );
  }

  return (
    <section
      id={id}
      className="sila-studio-nav-panel"
      aria-label={resolvedTitle || ariaLabel}
      data-testid={testId}
    >
      {(resolvedTitle || resolvedStatus) && (
        <div className="sila-studio-nav-panel__head">
          {resolvedTitle ? (
            <h2 className="sila-studio-nav-panel__title">
              <Sparkles size={15} color="#087BB5" aria-hidden="true" />
              <span>{resolvedTitle}</span>
            </h2>
          ) : (
            <span />
          )}
          {resolvedStatus && <div className="sila-studio-nav-panel__status">{resolvedStatus}</div>}
        </div>
      )}
      {navBar}
      {children}
    </section>
  );
};

export interface StudioSubNavigationProps {
  groupLabel?: string;
  items: StudioSubNavItem[];
  activeItemId?: string | null;
  activeId?: string | null;
  onSelectItem?: (item: StudioSubNavItem) => void;
  onSelect?: (id: string) => void;
  ariaLabel?: string;
  testId?: string;
}

const badgeClass = (badge?: string): string => {
  switch (badge) {
    case "Preview":
      return "sila-studio-badge sila-studio-badge--info";
    case "Configuration Required":
      return "sila-studio-badge sila-studio-badge--warning";
    case "Not Enabled":
    case "Temporarily Unavailable":
      return "sila-studio-badge sila-studio-badge--neutral";
    default:
      return "sila-studio-badge sila-studio-badge--neutral";
  }
};

export const StudioSubNavigation: React.FC<StudioSubNavigationProps> = ({
  groupLabel,
  items,
  activeItemId,
  activeId,
  onSelectItem,
  onSelect,
  ariaLabel = "Workspace submenu",
  testId = "studio-subnav",
}) => {
  if (items.length === 0) return null;
  const currentId = activeItemId ?? activeId ?? null;

  return (
    <nav className="sila-studio-subnav" aria-label={ariaLabel} data-testid={testId}>
      {groupLabel && <span className="sila-studio-subnav__group-label">{groupLabel}</span>}
      {items.map((item) => {
        const active = currentId !== null ? item.id === currentId : Boolean(item.active);
        const chipText = item.badge ?? item.statusBadge;
        return (
          <button
            key={item.id}
            type="button"
            disabled={item.disabled}
            title={item.tooltip}
            aria-current={active ? "page" : undefined}
            className={`sila-studio-subnav-btn ${active ? "is-active" : ""}`}
            onClick={() => {
              onSelectItem?.(item);
              onSelect?.(item.id);
            }}
            data-testid={item.testId ?? `studio-subnav-${item.id}`}
          >
            <span>{item.label}</span>
            {chipText && (
              <span className={item.statusTone ? `sila-studio-badge sila-studio-badge--${item.statusTone}` : badgeClass(chipText)}>
                {chipText}
              </span>
            )}
          </button>
        );
      })}
    </nav>
  );
};
