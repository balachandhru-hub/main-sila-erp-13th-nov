import React from "react";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface CulinaryMenuEventWorkspacesProps {
  mode: "menus" | "events" | "procurement" | "food-cost";
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

const DEMO_GALA_MENU = [
  { course: "Starters", items: ["Caesar Salad", "Arabic Mezze", "Mushroom Soup"] },
  { course: "Main Courses", items: ["Grilled Salmon", "Roast Chicken", "Vegetable Biryani", "Pasta"] },
  { course: "Desserts", items: ["Chocolate Mousse", "Cheesecake", "Fresh Fruit"] },
  { course: "Beverages", items: ["Fresh Juice", "Mocktails", "Soft Drinks", "Water"] },
];

const EVENT_READINESS = [
  { area: "Menu Readiness", status: "Defined in Demo", detail: "4 courses, 14 items (requires Phase 2A approval)" },
  { area: "Recipe Readiness", status: "1 Missing Material", detail: "Fresh Dill requires Material Creation Request" },
  { area: "Material Readiness", status: "Interactive Demo Ready", detail: "4 shortages computed in MRP Simulator" },
  { area: "Procurement Readiness", status: "Planned · Phase 2C", detail: "Reuses existing SILA PO & 311 Transfer APIs" },
  { area: "Kitchen Readiness", status: "Demo Timeline Ready", detail: "Banquet & Main Kitchens 08:00–22:00" },
  { area: "Financial Readiness", status: "Interactive P&L Ready", detail: "47.14% Est. Contribution (Phase 3A/4B live)" },
];

const CulinaryMenuEventWorkspaces: React.FC<CulinaryMenuEventWorkspacesProps> = ({
  mode,
  onOpenWorkspace,
}) => {
  if (mode === "menus") {
    return (
      <div className="cs2-shell" data-testid="culinary-menus-workspace">
        <div className="cs2-header">
          <div>
            <h2 className="cs2-title">Menu Studio (Phase 2A Preview)</h2>
            <p className="cs2-subtitle">
              Buffet, À la carte, Banquet, Corporate, Wedding, Seasonal &amp; Dietary menu builder with deterministic costing,
              OR-Tools constraint feasibility, allergen matrix, and customer-facing presentation (§24–§27).
            </p>
          </div>
          <span className="cs2-badge cs2-badge--planned">Planned · Phase 2A / 3B</span>
        </div>

        <div className="cs2-banner cs2-banner--phase" role="status">
          <div>
            <strong>DEMONSTRATION MENU PREVIEW — INTERNATIONAL BUFFET (500 GUESTS · TARGET FOOD COST &le; 28%)</strong>
            <div>Menu persistence, AI menu optimization, and menu engineering classification activate in Phases 2A and 3B.</div>
          </div>
          <span className="cs2-badge cs2-badge--demo">Demo Menu</span>
        </div>

        <div className="cs2-grid-2">
          {DEMO_GALA_MENU.map((grp) => (
            <section className="cs2-card" key={grp.course}>
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">{grp.course}</h3>
                <span className="cs2-badge cs2-badge--indigo">{grp.items.length} dishes</span>
              </div>
              <ul style={{ margin: 0, paddingLeft: 18, color: "#1e293b", lineHeight: 1.8 }}>
                {grp.items.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </section>
          ))}
        </div>
      </div>
    );
  }

  if (mode === "events") {
    return (
      <div className="cs2-shell" data-testid="culinary-events-workspace">
        <div className="cs2-header">
          <div>
            <h2 className="cs2-title">AI Event Planner — Corporate Gala Dinner (<code>EVT-2027-0001</code>)</h2>
            <p className="cs2-subtitle">
              Phase 2A workspace preview (§28–§32): 15-tab hospitality event lifecycle from Draft → Approved → Stock Reserved →
              Procurement → Production → Execution → Financial Closure.
            </p>
          </div>
          <div className="cs2-actions-row">
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("mrp")}>
              Open Event MRP Simulator
            </button>
            <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={() => onOpenWorkspace("event-pnl")}>
              Open Event P&amp;L Simulator
            </button>
          </div>
        </div>

        <div className="cs2-banner" role="status">
          <div>
            <strong>ILLUSTRATIVE EVENT RECORD — NOT STORED IN PRODUCTION TENANT DATABASE</strong>
            <div>
              Date: 15 January 2027 · Expected: 500 guests (Guaranteed: 480 + 20) · Service: International Buffet ·
              Price: AED 350/guest · Target Food Cost: 28% · Kitchen: Banquet Kitchen.
            </div>
          </div>
          <span className="cs2-badge cs2-badge--demo">EVT-2027-0001 (Demo)</span>
        </div>

        <div className="cs2-kpi-grid">
          {EVENT_READINESS.map((r) => (
            <div className="cs2-kpi cs2-kpi--accent" key={r.area}>
              <span className="cs2-kpi__label">{r.area}</span>
              <span className="cs2-kpi__value" style={{ fontSize: 16 }}>{r.status}</span>
              <span className="cs2-kpi__hint">{r.detail}</span>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (mode === "procurement") {
    return (
      <div className="cs2-shell" data-testid="culinary-procurement-workspace">
        <div className="cs2-header">
          <div>
            <h2 className="cs2-title">Event Procurement Intelligence (Phase 2C Preview)</h2>
            <p className="cs2-subtitle">
              Prioritized replenishment (§40–§44): Event Kitchen Stock → Alt Storage Location (311 Transfer) →
              Eligible Open POs → Approved Substitutes → Constrained PO Recommendations via existing SILA PO API.
            </p>
          </div>
          <span className="cs2-badge cs2-badge--planned">Planned · Phase 2C</span>
        </div>

        <div className="cs2-banner cs2-banner--phase" role="status">
          <div>
            <strong>REGRESSION GUARD ACTIVE — EXISTING PO &amp; 311/312 ITO APIS UNCHANGED</strong>
            <div>
              No live SAP postings, parallel PO APIs, or automatic PO releases are permitted. Event-linked PO drafts &amp;
              atomic stock reservations activate in Phase 2C.
            </div>
          </div>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => onOpenWorkspace("mrp")}>
            Simulate Shortages in MRP
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="cs2-shell" data-testid="culinary-food-cost-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Food Cost Studio (Phase 3A Preview)</h2>
          <p className="cs2-subtitle">
            Deterministic Decimal costing engine (§45) honoring cost source priority: Approved Contract → Catalog →
            Inventory Valuation → Latest Eligible Purchase → Standard Cost. Missing prices stay flagged as missing, never zero.
          </p>
        </div>
        <span className="cs2-badge cs2-badge--planned">Planned · Phase 3A</span>
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
