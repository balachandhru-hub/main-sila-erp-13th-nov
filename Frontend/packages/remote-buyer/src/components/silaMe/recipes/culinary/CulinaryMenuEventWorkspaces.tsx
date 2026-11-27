import React from "react";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import CulinaryEventPlannerWorkspace from "./CulinaryEventPlannerWorkspace";
import CulinaryMenuStudioWorkspace from "./CulinaryMenuStudioWorkspace";
import CulinaryProcurementStudioWorkspace from "./CulinaryProcurementStudioWorkspace";
import "./CulinarySuite.css";

interface CulinaryMenuEventWorkspacesProps {
  mode: "menus" | "events" | "procurement" | "food-cost";
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

const CulinaryMenuEventWorkspaces: React.FC<CulinaryMenuEventWorkspacesProps> = ({
  mode,
  onOpenWorkspace,
}) => {
  if (mode === "menus") {
    return <CulinaryMenuStudioWorkspace onOpenWorkspace={onOpenWorkspace} />;
  }

  if (mode === "events") {
    return <CulinaryEventPlannerWorkspace onOpenWorkspace={onOpenWorkspace} />;
  }

  if (mode === "procurement") {
    return <CulinaryProcurementStudioWorkspace onOpenWorkspace={onOpenWorkspace} />;
  }

  return (
    <div className="cs2-shell" data-testid="culinary-food-cost-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Food Cost Studio</h2>
          <p className="cs2-subtitle">
            Deterministic costing engine honoring cost source priority: Approved Contract → Catalog →
            Inventory Valuation → Latest Eligible Purchase → Standard Cost. Missing prices stay flagged as missing, never zero.
          </p>
        </div>
        <span className="cs2-badge cs2-badge--planned">Preview</span>
      </div>
      <div className="cs2-banner cs2-banner--phase" role="status">
        <div>
          <strong>COSTING SIMULATION AVAILABLE TODAY</strong>
          <div>
            Try yield-adjusted recipe costing in AI Recipe Planner or package food cost % in Event P&amp;L Simulator.
          </div>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("ai-planner")}>
            Recipe Costing Demo
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("event-pnl")}>
            Event P&amp;L Demo
          </button>
        </div>
      </div>
    </div>
  );
};

export default CulinaryMenuEventWorkspaces;
