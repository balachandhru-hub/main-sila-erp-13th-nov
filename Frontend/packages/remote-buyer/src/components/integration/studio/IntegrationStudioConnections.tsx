import React, { useMemo, useState } from "react";
import {
  SAP_MOVEMENT_PROFILES,
  matchSapMovementProfile,
  type IntegrationConfiguration,
  type SapMovementProfile,
} from "../../../api/integrationsApi";
import { formatDateTime } from "../integrationFormat";
import {
  connectionCornerStatus,
  directionChipClass,
  directionLabel,
  isInventorySystem,
} from "./istudioFormat";

type ConnectionsView = "grid" | "list";

interface IntegrationStudioConnectionsProps {
  rows: IntegrationConfiguration[];
  readOnly?: boolean;
  onOpen: (configurationId: string) => void;
  onTest: (configurationId: string) => void;
  onCopy: (row: IntegrationConfiguration) => void;
  onDelete: (row: IntegrationConfiguration) => void;
  /** Opens the configure wizard for an unconfigured SAP movement profile. */
  onConfigureProfile?: (profile: SapMovementProfile) => void;
  testingId?: string | null;
}

interface ConnectionItem {
  key: string;
  kind: "saved" | "missing";
  row?: IntegrationConfiguration;
  profile?: SapMovementProfile;
}

const profileForRow = (row: IntegrationConfiguration): SapMovementProfile | undefined =>
  SAP_MOVEMENT_PROFILES.find((profile) => matchSapMovementProfile(profile, [row]));

const IntegrationStudioConnections: React.FC<IntegrationStudioConnectionsProps> = ({
  rows,
  readOnly = false,
  onOpen,
  onTest,
  onCopy,
  onDelete,
  onConfigureProfile,
  testingId,
}) => {
  const [view, setView] = useState<ConnectionsView>("grid");

  const ordered = useMemo(() => {
    const inventory = rows.filter(isInventorySystem);
    const other = rows.filter((row) => !isInventorySystem(row));
    const byName = (a: IntegrationConfiguration, b: IntegrationConfiguration) =>
      a.name.localeCompare(b.name, undefined, { sensitivity: "base" });
    return [...inventory.sort(byName), ...other.sort(byName)];
  }, [rows]);

  const missingProfiles = useMemo(
    () => SAP_MOVEMENT_PROFILES.filter((profile) => !matchSapMovementProfile(profile, rows)),
    [rows],
  );

  const items = useMemo<ConnectionItem[]>(() => [
    ...ordered.map((row) => ({
      key: row.id,
      kind: "saved" as const,
      row,
      profile: profileForRow(row),
    })),
    ...missingProfiles.map((profile) => ({
      key: `missing-${profile.id}`,
      kind: "missing" as const,
      profile,
    })),
  ], [ordered, missingProfiles]);

  if (items.length === 0) {
    return <div className="istudio-empty">No saved API connections yet. Use the configuration wizard to add one.</div>;
  }

  return (
    <div className="srec-stack" data-testid="istudio-connections">
      <div className="istudio-view-bar">
        <span className="istudio-view-bar__count">{items.length} connections</span>
        <div className="istudio-view-toggle" role="group" aria-label="Connections view">
          <button
            type="button"
            className="istudio-view-toggle__btn"
            aria-pressed={view === "grid"}
            onClick={() => setView("grid")}
          >
            Grid
          </button>
          <button
            type="button"
            className="istudio-view-toggle__btn"
            aria-pressed={view === "list"}
            onClick={() => setView("list")}
          >
            List
          </button>
        </div>
      </div>

      {view === "grid" ? (
        <div className="istudio-grid-cards istudio-grid-cards--compact">
          {items.map((item) => (
            <ConnectionCard
              key={item.key}
              item={item}
              readOnly={readOnly}
              testingId={testingId}
              onOpen={onOpen}
              onTest={onTest}
              onCopy={onCopy}
              onDelete={onDelete}
              onConfigureProfile={onConfigureProfile}
            />
          ))}
        </div>
      ) : (
        <div className="istudio-list" role="table" aria-label="Connections list">
          <div className="istudio-list__head" role="row">
            <span role="columnheader">Name</span>
            <span role="columnheader">Process</span>
            <span role="columnheader">Entity</span>
            <span role="columnheader">Protocol</span>
            <span role="columnheader">Status</span>
            <span role="columnheader">Actions</span>
          </div>
          {items.map((item) => (
            <ConnectionListRow
              key={item.key}
              item={item}
              readOnly={readOnly}
              testingId={testingId}
              onOpen={onOpen}
              onTest={onTest}
              onCopy={onCopy}
              onDelete={onDelete}
              onConfigureProfile={onConfigureProfile}
            />
          ))}
        </div>
      )}
    </div>
  );
};

interface ItemActionsProps {
  item: ConnectionItem;
  readOnly?: boolean;
  testingId?: string | null;
  onOpen: (configurationId: string) => void;
  onTest: (configurationId: string) => void;
  onCopy: (row: IntegrationConfiguration) => void;
  onDelete: (row: IntegrationConfiguration) => void;
  onConfigureProfile?: (profile: SapMovementProfile) => void;
  compact?: boolean;
}

const ConnectionActions: React.FC<ItemActionsProps> = ({
  item,
  readOnly = false,
  testingId,
  onOpen,
  onTest,
  onCopy,
  onDelete,
  onConfigureProfile,
  compact = false,
}) => {
  const blocked = item.profile?.activationPolicy === "blocked_unverified";
  const actionClass = compact
    ? "istudio-card__actions istudio-card__actions--compact"
    : "istudio-card__actions";

  if (item.kind === "missing") {
    return (
      <div className={`${actionClass} istudio-card__actions--single`}>
        {!readOnly && !blocked && item.profile && onConfigureProfile ? (
          <button
            type="button"
            className="sila-btn sila-btn--primary sila-btn--sm"
            onClick={() => onConfigureProfile(item.profile!)}
          >
            Config
          </button>
        ) : (
          <span className="istudio-chip istudio-chip--blocked">
            {blocked ? "Blocked" : "Not configured"}
          </span>
        )}
      </div>
    );
  }

  const row = item.row!;
  if (readOnly) {
    return (
      <div className={`${actionClass} istudio-card__actions--single`}>
        <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>
          View
        </button>
      </div>
    );
  }
  if (blocked) {
    return (
      <div className={`${actionClass} istudio-card__actions--blocked`}>
        <span className="istudio-chip istudio-chip--blocked">Blocked</span>
      </div>
    );
  }
  return (
    <div className={actionClass}>
      <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>
        Config
      </button>
      <button
        type="button"
        className="sila-btn sila-btn--primary sila-btn--sm"
        onClick={() => onTest(row.id)}
        disabled={testingId === row.id}
      >
        {testingId === row.id ? "…" : "Test"}
      </button>
      <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onCopy(row)}>
        Copy
      </button>
      <button type="button" className="sila-btn sila-btn--danger sila-btn--sm" onClick={() => onDelete(row)}>
        Delete
      </button>
    </div>
  );
};

const ConnectionCard: React.FC<ItemActionsProps> = (props) => {
  const { item } = props;
  const row = item.row;
  const profile = item.profile;
  const inventory = row ? isInventorySystem(row) : true;
  const blocked = profile?.activationPolicy === "blocked_unverified";
  const corner = connectionCornerStatus(row?.status, {
    blocked,
    missing: item.kind === "missing" && !blocked,
  });
  const title = profile?.title || row?.name || profile?.configurationName || "Connection";
  const kicker = profile?.profileCode || (inventory ? "Inventory / SAP" : "Integration");

  return (
    <article className="istudio-card istudio-card--compact" data-profile={profile?.profileCode}>
      <span className={corner.className} aria-label={`Status ${corner.label}`}>
        <span className="istudio-card__corner-dot" aria-hidden="true" />
        {corner.label}
      </span>
      <div className="istudio-card__header">
        <p className="istudio-kicker">{kicker}</p>
        <h3 className="istudio-card__title">{title}</h3>
      </div>
      <div className="istudio-card__meta">
        {profile ? (
          <span className={directionChipClass(profile.direction)}>{directionLabel(profile.direction)}</span>
        ) : null}
        <span className="istudio-chip istudio-chip--muted">{row?.protocol || profile?.method || "—"}</span>
        {profile?.goodsMovementType ? (
          <span className="istudio-chip istudio-chip--muted">Type {profile.goodsMovementType}</span>
        ) : null}
      </div>
      <dl className="istudio-card__rows istudio-card__rows--compact">
        <div>
          <dt>Config</dt>
          <dd>{row?.name || profile?.configurationName}</dd>
        </div>
        <div>
          <dt>Process</dt>
          <dd>{row?.processType || profile?.processType || "—"}</dd>
        </div>
        <div>
          <dt>Entity</dt>
          <dd>{row?.entityCode || "—"}</dd>
        </div>
        <div>
          <dt>System</dt>
          <dd>{row?.systemName || "FIVE_S4_INVENTORY_MOVEMENTS"}</dd>
        </div>
        <div>
          <dt>Last test</dt>
          <dd>{row?.testedAt ? formatDateTime(row.testedAt) : "Not tested"}</dd>
        </div>
      </dl>
      <ConnectionActions {...props} compact />
    </article>
  );
};

const ConnectionListRow: React.FC<ItemActionsProps> = (props) => {
  const { item } = props;
  const row = item.row;
  const profile = item.profile;
  const blocked = profile?.activationPolicy === "blocked_unverified";
  const corner = connectionCornerStatus(row?.status, {
    blocked,
    missing: item.kind === "missing" && !blocked,
  });
  const title = profile?.title || row?.name || profile?.configurationName || "Connection";

  return (
    <div className="istudio-list__row" role="row">
      <div className="istudio-list__cell istudio-list__cell--name" role="cell">
        <strong>{title}</strong>
        <span className="srec-sub">{row?.name || profile?.configurationName}</span>
      </div>
      <div className="istudio-list__cell" role="cell">{row?.processType || profile?.processType || "—"}</div>
      <div className="istudio-list__cell" role="cell">{row?.entityCode || "—"}</div>
      <div className="istudio-list__cell" role="cell">{row?.protocol || profile?.method || "—"}</div>
      <div className="istudio-list__cell" role="cell">
        <span className={`istudio-list__status ${corner.className.replace("istudio-card__corner ", "")}`}>
          <span className="istudio-card__corner-dot" aria-hidden="true" />
          {corner.label}
        </span>
      </div>
      <div className="istudio-list__cell istudio-list__cell--actions" role="cell">
        <ConnectionActions {...props} compact />
      </div>
    </div>
  );
};

export default IntegrationStudioConnections;
