import React, { useMemo } from "react";
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

  if (ordered.length === 0 && missingProfiles.length === 0) {
    return <div className="istudio-empty">No saved API connections yet. Use the configuration wizard to add one.</div>;
  }

  return (
    <div className="srec-stack" data-testid="istudio-connections">
      <div className="istudio-banner">
        SAP movement APIs live here with the rest of your connections. Z03 stays unconfigured; POS Return / Reversal
        cannot be activated until SAP confirms Z01 adjustment vs Cancel. Connection tests never post stock.
      </div>

      <div className="istudio-grid-cards">
        {ordered.map((row) => {
          const profile = profileForRow(row);
          const inventory = isInventorySystem(row);
          const blocked = profile?.activationPolicy === "blocked_unverified";
          const corner = connectionCornerStatus(row.status, { blocked });
          return (
            <article key={row.id} className="istudio-card" data-profile={profile?.profileCode}>
              <span className={corner.className} aria-label={`Status ${corner.label}`}>
                <span className="istudio-card__corner-dot" aria-hidden="true" />
                {corner.label}
              </span>
              <div className="istudio-card__header">
                <p className="istudio-kicker">
                  {profile?.profileCode || (inventory ? "Inventory / SAP" : "Integration")}
                </p>
                <h3 className="istudio-card__title">{profile?.title || row.name}</h3>
              </div>
              <div className="istudio-card__meta">
                {profile ? (
                  <span className={directionChipClass(profile.direction)}>{directionLabel(profile.direction)}</span>
                ) : null}
                <span className="istudio-chip istudio-chip--muted">{row.protocol}</span>
                {profile?.goodsMovementType ? (
                  <span className="istudio-chip istudio-chip--muted">Type {profile.goodsMovementType}</span>
                ) : null}
                {profile?.goodsMovementCode ? (
                  <span className="istudio-chip istudio-chip--muted">Code {profile.goodsMovementCode}</span>
                ) : null}
              </div>
              <dl className="istudio-card__rows">
                <dt>Configuration name</dt>
                <dd>{row.name}</dd>
                <dt>System type</dt>
                <dd>{row.systemName || "—"}</dd>
                <dt>Environment / entity</dt>
                <dd>{row.entityCode || "—"}</dd>
                <dt>Base URL</dt>
                <dd>{row.baseUrl || "—"}</dd>
                <dt>Process</dt>
                <dd>{row.processType}</dd>
                {profile ? (
                  <>
                    <dt>Resource path</dt>
                    <dd>{profile.resourcePath}</dd>
                    <dt>Purpose</dt>
                    <dd>{profile.purpose}</dd>
                  </>
                ) : null}
                <dt>Credentials</dt>
                <dd>{row.credentialStatus || "—"}</dd>
                <dt>Last test / activity</dt>
                <dd>
                  {row.testedAt ? formatDateTime(row.testedAt) : "Not tested"}
                  {row.lastSuccessfulRunAt ? ` · last success ${formatDateTime(row.lastSuccessfulRunAt)}` : ""}
                </dd>
                <dt>Last error</dt>
                <dd>{row.lastErrorSafe || "—"}</dd>
                {profile?.statusNote ? (
                  <>
                    <dt>Note</dt>
                    <dd>{profile.statusNote}</dd>
                  </>
                ) : null}
              </dl>
              {readOnly ? (
                <div className="istudio-card__actions istudio-card__actions--single">
                  <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>
                    View
                  </button>
                </div>
              ) : blocked ? (
                <div className="istudio-card__actions istudio-card__actions--blocked">
                  <span className="istudio-chip istudio-chip--blocked">Activation blocked</span>
                </div>
              ) : (
                <div className="istudio-card__actions">
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
              )}
            </article>
          );
        })}

        {missingProfiles.map((profile) => {
          const blocked = profile.activationPolicy === "blocked_unverified";
          const corner = connectionCornerStatus(null, { blocked, missing: !blocked });
          return (
            <article key={`missing-${profile.id}`} className="istudio-card" data-profile={profile.profileCode}>
              <span className={corner.className} aria-label={`Status ${corner.label}`}>
                <span className="istudio-card__corner-dot" aria-hidden="true" />
                {corner.label}
              </span>
              <div className="istudio-card__header">
                <p className="istudio-kicker">{profile.profileCode}</p>
                <h3 className="istudio-card__title">{profile.title}</h3>
              </div>
              <div className="istudio-card__meta">
                <span className={directionChipClass(profile.direction)}>{directionLabel(profile.direction)}</span>
                <span className="istudio-chip istudio-chip--muted">{profile.method}</span>
                {profile.goodsMovementType ? (
                  <span className="istudio-chip istudio-chip--muted">Type {profile.goodsMovementType}</span>
                ) : null}
              </div>
              <dl className="istudio-card__rows">
                <dt>Configuration name</dt>
                <dd>{profile.configurationName}</dd>
                <dt>Resource path</dt>
                <dd>{profile.resourcePath}</dd>
                <dt>Purpose</dt>
                <dd>{profile.purpose}</dd>
                <dt>Note</dt>
                <dd>{profile.statusNote}</dd>
              </dl>
              <div className="istudio-card__actions istudio-card__actions--single">
                {!readOnly && !blocked && onConfigureProfile ? (
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary sila-btn--sm"
                    onClick={() => onConfigureProfile(profile)}
                  >
                    Config
                  </button>
                ) : (
                  <span className="istudio-chip istudio-chip--blocked">
                    {blocked ? "Activation blocked" : "Not configured"}
                  </span>
                )}
              </div>
            </article>
          );
        })}
      </div>
    </div>
  );
};

export default IntegrationStudioConnections;
