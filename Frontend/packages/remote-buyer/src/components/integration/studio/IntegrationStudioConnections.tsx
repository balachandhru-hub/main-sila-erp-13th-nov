import React, { useMemo } from "react";
import {
  SAP_MOVEMENT_PROFILES,
  matchSapMovementProfile,
  type IntegrationConfiguration,
  type SapMovementProfile,
} from "../../../api/integrationsApi";
import { formatDateTime, statusBadgeClass, statusLabel } from "../integrationFormat";
import {
  activationLabel,
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
          const badge = profile ? activationLabel(profile.activationPolicy, row) : null;
          return (
            <article key={row.id} className="istudio-card" data-profile={profile?.profileCode}>
              <div>
                <p className="istudio-kicker">
                  {profile?.profileCode || (inventory ? "Inventory / SAP" : "Integration")}
                </p>
                <h3 className="istudio-card__title">{profile?.title || row.name}</h3>
              </div>
              <div className="istudio-card__meta">
                <span className={statusBadgeClass(row.status)}>{statusLabel(row.status)}</span>
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
                {badge ? <span className={badge.className}>{badge.text}</span> : null}
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
              <div className="istudio-card__actions">
                <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(row.id)}>
                  {readOnly ? "View" : profile ? "Edit mapping" : "Configure"}
                </button>
                {!readOnly ? (
                  <>
                    {!blocked ? (
                      <button
                        type="button"
                        className="sila-btn sila-btn--primary sila-btn--sm"
                        onClick={() => onTest(row.id)}
                        disabled={testingId === row.id}
                      >
                        {testingId === row.id ? "Testing…" : "Test connection"}
                      </button>
                    ) : (
                      <span className="istudio-chip istudio-chip--blocked">Activation blocked</span>
                    )}
                    <button type="button" className="sila-btn sila-btn--ghost sila-btn--sm" onClick={() => onCopy(row)}>
                      Copy
                    </button>
                    <button type="button" className="sila-btn sila-btn--danger sila-btn--sm" onClick={() => onDelete(row)}>
                      Delete
                    </button>
                  </>
                ) : null}
              </div>
            </article>
          );
        })}

        {missingProfiles.map((profile) => {
          const badge = activationLabel(profile.activationPolicy, null);
          const blocked = profile.activationPolicy === "blocked_unverified";
          return (
            <article key={`missing-${profile.id}`} className="istudio-card" data-profile={profile.profileCode}>
              <div>
                <p className="istudio-kicker">{profile.profileCode}</p>
                <h3 className="istudio-card__title">{profile.title}</h3>
              </div>
              <div className="istudio-card__meta">
                <span className={directionChipClass(profile.direction)}>{directionLabel(profile.direction)}</span>
                <span className="istudio-chip istudio-chip--muted">{profile.method}</span>
                {profile.goodsMovementType ? (
                  <span className="istudio-chip istudio-chip--muted">Type {profile.goodsMovementType}</span>
                ) : null}
                <span className={badge.className}>{badge.text}</span>
              </div>
              <dl className="istudio-card__rows">
                <dt>Configuration name</dt>
                <dd>{profile.configurationName}</dd>
                <dt>Resource path</dt>
                <dd>{profile.resourcePath}</dd>
                <dt>Purpose</dt>
                <dd>{profile.purpose}</dd>
                <dt>Saved status</dt>
                <dd>Not saved yet</dd>
                <dt>Note</dt>
                <dd>{profile.statusNote}</dd>
              </dl>
              <div className="istudio-card__actions">
                {!readOnly && !blocked && onConfigureProfile ? (
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary sila-btn--sm"
                    onClick={() => onConfigureProfile(profile)}
                  >
                    Configure profile
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
