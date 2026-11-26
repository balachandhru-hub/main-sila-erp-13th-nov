import React from "react";
import "./CulinarySuite.css";

interface CulinaryMaterialIntelligenceWorkspaceProps {
  mode: "materials" | "material-requests";
}

const DEMO_MATCHING_ROWS = [
  {
    ingredient: "Atlantic Salmon Fillet (Skin-on, Fresh)",
    suggestedCode: "DEMO-MAT-1001",
    description: "SALMON FILLET FRESH NORWEGIAN KG",
    uom: "KG / Case 5 KG",
    stock: "25.0 kg",
    cost: "AED 85.00 / kg",
    status: "EXACT_MATCH",
    evidence: "Exact normalized description + Seafood category + KG UOM",
  },
  {
    ingredient: "Extra Virgin Olive Oil",
    suggestedCode: "DEMO-MAT-1006",
    description: "OLIVE OIL EXTRA VIRGIN MEDITERRANEAN 5L",
    uom: "L / Tin 5 L",
    stock: "10.0 L",
    cost: "AED 35.00 / L",
    status: "SUGGESTED_MATCH",
    evidence: "Fuzzy 91% + Semantic 0.94 (Requires Chef Confirmation)",
  },
  {
    ingredient: "Fresh Dill (Bunch)",
    suggestedCode: "—",
    description: "No authorized Material Master record found",
    uom: "KG / Pack 1 KG",
    stock: "0",
    cost: "AED 45.00 (Est.)",
    status: "MATERIAL_CREATION_REQUIRED",
    evidence: "Missing in Material Master — auto-drafted Material Creation Request",
  },
];

const CulinaryMaterialIntelligenceWorkspace: React.FC<CulinaryMaterialIntelligenceWorkspaceProps> = ({
  mode,
}) => {
  return (
    <div className="cs2-shell" data-testid={`culinary-${mode}-workspace`}>
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">
            {mode === "materials" ? "Material Intelligence Engine" : "Material Creation Requests"}
          </h2>
          <p className="cs2-subtitle">
            {mode === "materials"
              ? "Phase 1B preview (§19–§23): deterministic & semantic ingredient-to-Material-Master matching distinguishing fresh/frozen, raw/cooked, pack sizes, and allergens."
              : "Phase 1C preview (§22): automated Material Creation Request drafts with duplicate checks, Procurement & Master Data review, and recipe/event synchronization."}
          </p>
        </div>
        <span className="cs2-badge cs2-badge--planned">
          {mode === "materials" ? "Planned · Phase 1B" : "Planned · Phase 1C"}
        </span>
      </div>

      <div className="cs2-banner cs2-banner--phase" role="status">
        <div>
          <strong>PHASE 0 WORKSPACE PREVIEW — NO PRODUCTION MATERIAL WRITES</strong>
          <div>
            Existing Material Master &amp; SAP material integration remain unchanged. Matching execution, substitution persistence,
            and Material Creation Request submissions activate in Phases 1B and 1C.
          </div>
        </div>
        <span className="cs2-badge cs2-badge--demo">Illustrative Preview</span>
      </div>

      {mode === "materials" ? (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">13-Step Matching Pipeline Preview</h3>
            <span className="cs2-badge cs2-badge--indigo">Tenant-Isolated Material Search</span>
          </div>
          <p className="cs2-empty-note" style={{ marginBottom: 12 }}>
            Sequence: Exact Code → Normalized Desc → Aliases → Brand/Attributes → Category/Group → Form → Pack Size → UOM → RapidFuzz → Sentence-Transformers/FAISS → Ranked Candidates → Chef Confirmation.
          </p>
          <div className="cs2-table-wrap">
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>AI Ingredient</th>
                  <th>Suggested Material (Demo)</th>
                  <th>Base / Purchase UOM</th>
                  <th>Stock</th>
                  <th>Latest Approved Cost</th>
                  <th>Status &amp; Evidence</th>
                  <th>Actions (Phase 1B)</th>
                </tr>
              </thead>
              <tbody>
                {DEMO_MATCHING_ROWS.map((row) => (
                  <tr key={row.ingredient}>
                    <td><strong>{row.ingredient}</strong></td>
                    <td>
                      <div><code>{row.suggestedCode}</code></div>
                      <div className="cs2-empty-note">{row.description}</div>
                    </td>
                    <td>{row.uom}</td>
                    <td>{row.stock}</td>
                    <td>{row.cost}</td>
                    <td>
                      <span
                        className={`cs2-badge ${
                          row.status === "EXACT_MATCH"
                            ? "cs2-badge--live"
                            : row.status === "SUGGESTED_MATCH"
                              ? "cs2-badge--warn"
                              : "cs2-badge--danger"
                        }`}
                      >
                        {row.status}
                      </span>
                      <div className="cs2-empty-note" style={{ marginTop: 4 }}>{row.evidence}</div>
                    </td>
                    <td>
                      <div className="cs2-actions-row">
                        <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled>
                          Confirm (Phase 1B)
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Sample Material Creation Request Draft — Fresh Dill</h3>
            <span className="cs2-badge cs2-badge--warn">Workflow: Draft → Duplicate Check → Chef → Procurement → MDM → SAP/Sync</span>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label">Proposed Description (AI-Suggested)</label>
              <input className="sila-input" readOnly value="FRESH DILL HERB BUNCH 1KG" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Material Group / Base UOM</label>
              <input className="sila-input" readOnly value="Fresh Herbs / KG (Purchase: Pack 1 KG)" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Recipe / Event Reference</label>
              <input className="sila-input" readOnly value="Grilled Salmon (Demo) / EVT-2027-0001" />
            </div>
            <div className="sila-field">
              <label className="sila-label">Estimated Price &amp; Source</label>
              <input className="sila-input" readOnly value="AED 45.00 / KG (Chef Estimate — Unverified)" />
            </div>
          </div>
          <div className="cs2-actions-row" style={{ marginTop: 14 }}>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled>
              Submit Material Request (Phase 1C)
            </button>
            <span className="cs2-empty-note">
              No SAP material creation endpoint is invented; requests route to Master Data Task Queue when Phase 1C launches.
            </span>
          </div>
        </section>
      )}
    </div>
  );
};

export default CulinaryMaterialIntelligenceWorkspace;
