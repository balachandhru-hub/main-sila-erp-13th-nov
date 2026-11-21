import React from "react";
import {
  SAP_MOVEMENT_PROFILES,
  matchSapMovementProfile,
  type IntegrationConfiguration,
} from "../../../api/integrationsApi";
import { statusLabel } from "../integrationFormat";
import { activationLabel, directionLabel } from "./istudioFormat";

interface IntegrationStudioMatrixProps {
  rows: IntegrationConfiguration[];
  onOpen?: (configurationId: string) => void;
}

const IntegrationStudioMatrix: React.FC<IntegrationStudioMatrixProps> = ({ rows, onOpen }) => (
  <section className="istudio-panel" data-testid="istudio-matrix">
    <h3 className="istudio-panel__title">SAP movement configuration matrix</h3>
    <p className="istudio-panel__hint">
      Confirmed mappings are protected from silent overwrite. Edit through the existing configuration workspace only.
    </p>
    <div className="istudio-table-wrap">
      <table className="istudio-table">
        <thead>
          <tr>
            <th scope="col">Movement profile</th>
            <th scope="col">Movement code</th>
            <th scope="col">Movement type</th>
            <th scope="col">Direction</th>
            <th scope="col">API / config name</th>
            <th scope="col">Status</th>
            <th scope="col">Actions</th>
          </tr>
        </thead>
        <tbody>
          {SAP_MOVEMENT_PROFILES.map((profile) => {
            const configured = matchSapMovementProfile(profile, rows);
            const badge = activationLabel(profile.activationPolicy, configured);
            return (
              <tr key={profile.id}>
                <td>
                  <div className="sila-cell-strong">{profile.title}</div>
                  <span className="srec-sub">{profile.profileCode}</span>
                </td>
                <td>{profile.goodsMovementCode || "—"}</td>
                <td>{profile.goodsMovementType || "—"}</td>
                <td>{directionLabel(profile.direction)}</td>
                <td>
                  {profile.configurationName}
                  <span className="srec-sub">{profile.processType} · {profile.method}</span>
                </td>
                <td>
                  <span className={badge.className}>{badge.text}</span>
                  {configured ? <span className="srec-sub">{statusLabel(configured.status)}</span> : null}
                </td>
                <td>
                  {configured && onOpen ? (
                    <button type="button" className="sila-btn sila-btn--secondary sila-btn--sm" onClick={() => onOpen(configured.id)}>
                      View mapping
                    </button>
                  ) : (
                    "—"
                  )}
                </td>
              </tr>
            );
          })}
          <tr>
            <td>
              <div className="sila-cell-strong">Z03 (reserved)</div>
              <span className="srec-sub">Unassigned</span>
            </td>
            <td>—</td>
            <td>Z03</td>
            <td>—</td>
            <td>Not configured</td>
            <td><span className="istudio-chip istudio-chip--muted">Kept unconfigured</span></td>
            <td>—</td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
);

export default IntegrationStudioMatrix;
