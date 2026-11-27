import React from "react";

export type StudioKPITone =
  | "blue"
  | "green"
  | "lavender"
  | "amber"
  | "rose"
  | "indigo"
  | "sky";

export interface StudioKPIItem {
  id?: string;
  label: string;
  value: React.ReactNode;
  meta?: React.ReactNode;
  hint?: React.ReactNode;
  badge?: React.ReactNode;
  tone?: StudioKPITone;
  icon?: React.ReactNode;
  testId?: string;
}

export interface StudioKPICardProps extends StudioKPIItem {}

export const StudioKPICard: React.FC<StudioKPICardProps> = ({
  label,
  value,
  meta,
  hint,
  badge,
  tone = "blue",
  icon,
  testId,
}) => {
  const metaNode = meta ?? hint;
  return (
    <article className={`sila-studio-kpi sila-studio-kpi--${tone}`} data-testid={testId}>
      <div className="sila-studio-kpi__head">
        <span className="sila-studio-kpi__label">{label}</span>
        {icon && <span className="sila-studio-kpi__icon" aria-hidden="true">{icon}</span>}
      </div>
      <div className="sila-studio-kpi__value sila-tabular">{value}</div>
      {(metaNode || badge) && (
        <div className="sila-studio-kpi__meta" style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: "0.3rem" }}>
          {metaNode && <span>{metaNode}</span>}
          {badge && <span className="sila-studio-badge sila-studio-badge--neutral" style={{ fontSize: "0.62rem" }}>{badge}</span>}
        </div>
      )}
    </article>
  );
};

export interface StudioKPIBarProps {
  items?: StudioKPIItem[];
  children?: React.ReactNode;
  ariaLabel?: string;
  testId?: string;
}

export const StudioKPIBar: React.FC<StudioKPIBarProps> = ({
  items,
  children,
  ariaLabel = "Key performance indicators",
  testId = "studio-kpi-bar",
}) => {
  const count = items ? items.length : React.Children.count(children);
  return (
    <section
      className="sila-studio-kpi-bar"
      style={{ "--kpi-cols": Math.max(1, Math.min(count, 8)) } as React.CSSProperties}
      aria-label={ariaLabel}
      data-testid={testId}
    >
      {items
        ? items.map((item, idx) => <StudioKPICard key={item.id ?? `${item.label}-${idx}`} {...item} />)
        : children}
    </section>
  );
};
