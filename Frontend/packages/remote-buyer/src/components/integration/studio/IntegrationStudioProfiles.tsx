import React from "react";
import {
  SAP_MOVEMENT_PROFILES,
  matchSapMovementProfile,
  type IntegrationConfiguration,
  type SapMovementProfile,
} from "../../../api/integrationsApi";
import { formatDateTime, statusLabel } from "../integrationFormat";
import { activationLabel, directionChipClass, directionLabel } from "./istudioFormat";

interface IntegrationStudioProfilesProps {
  rows: IntegrationConfiguration[];
  readOnly?: boolean;
  onOpen: (configurationId: string) => void;
  onConfigure: (profile: SapMovementProfile) => void;
  onTest: (configurationId: string) => void;
}

const IntegrationStudioProfiles: React.FC<IntegrationStudioProfilesProps> = ({
  rows,
  readOnly = false,
  onOpen,
  onConfigure,
  onTest,
}) => (
  <div className="srec-stack" data-testid="istudio-profiles">
    <div className="istudio-banner">
      Z03 is intentionally unconfigured. POS Return / Reversal cannot be activated until SAP confirms whether the correct
      operation is Z01 adjustment or cancellation of the original material document. Connection tests never post stock.
    </div>
    <div className="istudio-grid-cards">
      {SAP_MOVEMENT_PROFILES.map((profile) => {
        const configured = matchSapMovementProfile(profile, rows);
        const badge = activationLabel(profile.activationPolicy, configured);
        const blocked = profile.activationPolicy === "blocked_unverified";
        return (
          <article key={profile.id} className="istudio-card" data-profile={profile.profileCode}>
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
              {profile.goodsMovementCode ? (
                <span className="istudio-chip istudio-chip--muted">Code {profile.goodsMovementCode}</span>
              ) : null}
              <span className={badge.className}>{badge.text}</span>
            </div>
            <dl className="istudio-card__rows">
              <dt>System</dt>
              <dd>SAP S/4HANA · FIVE_S4_INVENTORY_MOVEMENTS</dd>
              <dt>Configuration name</dt>
              <dd>{profile.configurationName}</dd>
              <dt>Resource path</dt>
              <dd>{profile.resourcePath}</dd>
              <dt>Purpose</dt>
              <dd>{profile.purpose}</dd>
              <dt>Saved status</dt>
              <dd>
                {configured
                  ? `${statusLabel(configured.status)}${configured.testedAt ? ` · tested ${formatDateTime(configured.testedAt)}` : ""}`
                  : "Not saved yet"}
              </dd>
              <dt>Note</dt>
              <dd>{profile.statusNote}</dd>
            </dl>
            <div className="istudio-card__actions">
              {configured ? (
                <>
                  <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(configured.id)}>
                    {readOnly ? "View configuration" : "Edit mapping"}
                  </button>
                  {!readOnly && !blocked ? (
                    <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onTest(configured.id)}>
                      Test connectivity
                    </button>
                  ) : null}
                </>
              ) : (
                !readOnly && !blocked ? (
                  <button type="button" className="sila-btn sila-btn--primary sila-btn--sm" onClick={() => onConfigure(profile)}>
                    Configure profile
                  </button>
                ) : (
                  <span className="istudio-chip istudio-chip--blocked">Activation blocked</span>
                )
              )}
            </div>
          </article>
        );
      })}
    </div>
  </div>
);

export default IntegrationStudioProfiles;
