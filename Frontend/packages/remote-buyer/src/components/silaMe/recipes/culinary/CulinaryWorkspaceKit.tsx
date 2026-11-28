import React from "react";
import {
  StudioFilterBar,
  StudioKPIBar,
  StudioKPICard,
  StudioSubNavigation,
  type StudioKPITone,
} from "../../studio";

/**
 * Shared Recipe Studio chrome for every Culinary Intelligence workspace:
 * section header, sub-tabs, clickable pastel KPI boxes and a single-line filter toolbar.
 */

export interface CulinaryWorkspaceHeaderProps {
  title: string;
  subtitle?: React.ReactNode;
  actions?: React.ReactNode;
  testId?: string;
}

export const CulinaryWorkspaceHeader: React.FC<CulinaryWorkspaceHeaderProps> = ({
  title,
  subtitle,
  actions,
  testId,
}) => (
  <div className="cs2-header" data-testid={testId}>
    <div>
      <h2 className="cs2-title">{title}</h2>
      {subtitle ? <p className="cs2-subtitle">{subtitle}</p> : null}
    </div>
    {actions ? <div className="cs2-header__actions">{actions}</div> : null}
  </div>
);

export interface CulinaryTabItem<T extends string = string> {
  id: T;
  label: string;
  testId?: string;
  disabled?: boolean;
}

export interface CulinaryTabsProps<T extends string = string> {
  tabs: CulinaryTabItem<T>[];
  active: T;
  onChange: (id: T) => void;
  ariaLabel: string;
  testId?: string;
}

export function CulinaryTabs<T extends string = string>({
  tabs,
  active,
  onChange,
  ariaLabel,
  testId,
}: CulinaryTabsProps<T>): React.ReactElement {
  return (
    <StudioSubNavigation
      ariaLabel={ariaLabel}
      testId={testId}
      items={tabs.map((tab) => ({
        id: tab.id,
        label: tab.label,
        testId: tab.testId,
        disabled: tab.disabled,
      }))}
      activeId={active}
      onSelect={(id) => onChange(id as T)}
    />
  );
}

export interface CulinaryKpiItem {
  id: string;
  label: string;
  value: React.ReactNode;
  hint?: React.ReactNode;
  tone?: StudioKPITone;
  icon?: React.ReactNode;
  /** Opens the data behind the number (tab, filtered list or another workspace). */
  onClick?: () => void;
  drillLabel?: string;
  selected?: boolean;
}

const TONE_CYCLE: StudioKPITone[] = ["blue", "green", "lavender", "amber", "rose", "indigo", "sky"];

export const CulinaryKpis: React.FC<{
  items: CulinaryKpiItem[];
  ariaLabel: string;
  testId?: string;
  columns?: number;
}> = ({ items, ariaLabel, testId, columns }) => (
  <StudioKPIBar ariaLabel={ariaLabel} testId={testId} columns={columns ?? Math.min(items.length, 6)}>
    {items.map((item, index) => (
      <StudioKPICard
        key={item.id}
        label={item.label}
        value={item.value}
        hint={item.hint}
        icon={item.icon}
        tone={item.tone ?? TONE_CYCLE[index % TONE_CYCLE.length]}
        testId={`kpi-${item.id}`}
        onClick={item.onClick}
        drillLabel={item.drillLabel}
        selected={item.selected}
      />
    ))}
  </StudioKPIBar>
);

export const CulinaryFilterToolbar: React.FC<{
  ariaLabel: string;
  left?: React.ReactNode;
  right?: React.ReactNode;
  testId?: string;
}> = ({ ariaLabel, left, right, testId }) => (
  <StudioFilterBar ariaLabel={ariaLabel} left={left} right={right} testId={testId} />
);
