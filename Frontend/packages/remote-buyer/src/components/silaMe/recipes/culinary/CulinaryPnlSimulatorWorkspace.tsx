import React, { useMemo, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import {
  calculateEventPnl,
  DEMO_EVENT_PNL_BASE,
  type EventPnlInput,
} from "./culinarySimulatorsMath";
import { CulinaryKpis } from "./CulinaryWorkspaceKit";
import "./CulinarySuite.css";

interface SavedPnlScenario {
  label: string;
  input: EventPnlInput;
  netRevenueAed: number;
  contributionAed: number;
  marginPercent: number;
}

const CulinaryPnlSimulatorWorkspace: React.FC = () => {
  const [input, setInput] = useState<EventPnlInput>(DEMO_EVENT_PNL_BASE);
  const [saved, setSaved] = useState<SavedPnlScenario[]>([]);
  const goTo = (id: string) => document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });

  const result = useMemo(() => calculateEventPnl(input), [input]);

  const setNum = (key: keyof EventPnlInput, value: number) => {
    setInput((prev) => ({ ...prev, [key]: Math.max(0, value) }));
  };

  const applyPreset = (preset: "base" | "best" | "worst") => {
    if (preset === "base") {
      setInput(DEMO_EVENT_PNL_BASE);
    } else if (preset === "best") {
      setInput({
        ...DEMO_EVENT_PNL_BASE,
        guestCount: 560,
        sellingPricePerGuestAed: 375,
        foodCostPerGuestAed: 84,
        discountPercent: 0,
      });
    } else {
      setInput({
        ...DEMO_EVENT_PNL_BASE,
        guestCount: 420,
        sellingPricePerGuestAed: 325,
        foodCostPerGuestAed: 98,
        discountPercent: 5,
      });
    }
  };

  const saveScenario = () => {
    setSaved((prev) => [
      {
        label: `Scenario ${prev.length + 1} (${input.guestCount} guests)`,
        input: { ...input },
        netRevenueAed: result.netRevenueAed,
        contributionAed: result.eventContributionAed,
        marginPercent: result.contributionMarginPercent,
      },
      ...prev.slice(0, 4),
    ]);
  };

  const chartData = [
    { name: "Net Revenue", value: result.netRevenueAed },
    { name: "Food Cost", value: result.foodCostAed },
    { name: "Beverage Cost", value: result.beverageCostAed },
    { name: "Fixed & Staff", value: result.totalFixedCostAed },
    { name: "Event Contribution", value: result.eventContributionAed },
  ];

  return (
    <div className="cs2-shell" data-testid="culinary-pnl-workspace">
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">Event Profitability &amp; What-If P&amp;L Simulator</h2>
          <p className="cs2-subtitle">
            Interactive financial simulator. Models Estimated vs What-If event contribution, food cost %,
            break-even guests, required selling price, and maximum allowable food cost without altering production ledgers.
          </p>
        </div>
        <div className="cs2-actions-row">
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => applyPreset("base")}>
            Base Case (§51)
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => applyPreset("best")}>
            Best Case
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => applyPreset("worst")}>
            Worst Case
          </button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" onClick={saveScenario}>
            Save What-If Scenario
          </button>
        </div>
      </div>

      <div className="cs2-banner" role="status">
        <div>
          <strong>DEMONSTRATION FINANCIAL SIMULATION — DISTINGUISHES EVENT CONTRIBUTION FROM ACCOUNTING NET PROFIT</strong>
          <div>
            Baseline (§51): 500 guests × AED 350 = AED 175,000 revenue; AED 92,500 cost; AED 82,500 contribution (47.14%);
            25.71% package food cost; 149 break-even guests. Live Committed/Actual P&amp;L activate in Phases 3A/4B.
          </div>
        </div>
        <span className="cs2-badge cs2-badge--demo">Interactive Simulator</span>
      </div>

      <CulinaryKpis
        ariaLabel="Event P&L simulation metrics"
        testId="pnl-kpis"
        columns={5}
        items={[
          { id: "pnl-revenue", label: "Net Event Revenue", value: `AED ${result.netRevenueAed.toLocaleString(undefined, { minimumFractionDigits: 2 })}`, hint: `AED ${result.revenuePerGuestAed.toFixed(2)} / guest`, tone: "blue", drillLabel: "Show the revenue vs cost chart", onClick: () => goTo("pnl-sim-chart") },
          { id: "pnl-cost", label: "Total Event Cost", value: `AED ${result.totalEventCostAed.toLocaleString(undefined, { minimumFractionDigits: 2 })}`, hint: `Food AED ${result.foodCostAed.toLocaleString()} · Bev AED ${result.beverageCostAed.toLocaleString()}`, tone: "rose", drillLabel: "Show the editable cost assumptions", onClick: () => goTo("pnl-sim-inputs") },
          { id: "pnl-contribution", label: "Event Contribution", value: `AED ${result.eventContributionAed.toLocaleString(undefined, { minimumFractionDigits: 2 })}`, hint: `Margin: ${result.contributionMarginPercent.toFixed(2)}%`, tone: "green", drillLabel: "Show the revenue vs cost chart", onClick: () => goTo("pnl-sim-chart") },
          { id: "pnl-food-pct", label: "Package Food Cost %", value: `${result.foodCostPercent.toFixed(2)}%`, hint: `Beverage Cost %: ${result.beverageCostPercent.toFixed(2)}%`, tone: "sky", drillLabel: "Show the editable cost assumptions", onClick: () => goTo("pnl-sim-inputs") },
          { id: "pnl-break-even", label: "Break-Even Guests", value: result.breakEvenGuests ?? "N/A", hint: `Target Margin Req. Price: ${result.requiredSellingPriceForTargetMarginAed !== null ? `AED ${result.requiredSellingPriceForTargetMarginAed.toFixed(2)}` : "N/A"}`, tone: "amber", drillLabel: "Show saved what-if scenarios", onClick: () => goTo("pnl-sim-scenarios") },
        ]}
      />

      <div className="cs2-grid-2">
        <section className="cs2-card" id="pnl-sim-inputs" aria-label="What-If Inputs">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Editable P&amp;L Assumptions</h3>
            <span className="cs2-badge cs2-badge--indigo">Max Food Cost @ Target: AED {result.maxAllowableFoodCostAed.toLocaleString()}</span>
          </div>
          <div className="cs2-form-grid">
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-guests">Guest Count ({input.guestCount})</label>
              <input id="cs2-pnl-guests" type="range" min={50} max={1500} step={10} value={input.guestCount} onChange={(e) => setNum("guestCount", Number(e.target.value))} />
              <input type="number" className="sila-input" aria-label="P&L guest count" value={input.guestCount} onChange={(e) => setNum("guestCount", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-price">Selling Price / Guest (AED)</label>
              <input id="cs2-pnl-price" type="number" className="sila-input" value={input.sellingPricePerGuestAed} onChange={(e) => setNum("sellingPricePerGuestAed", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-food">Food Cost / Guest (AED)</label>
              <input id="cs2-pnl-food" type="number" className="sila-input" value={input.foodCostPerGuestAed} onChange={(e) => setNum("foodCostPerGuestAed", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-bev">Beverage Cost / Guest (AED)</label>
              <input id="cs2-pnl-bev" type="number" className="sila-input" value={input.beverageCostPerGuestAed} onChange={(e) => setNum("beverageCostPerGuestAed", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-fixed">Fixed Event Expenses (AED)</label>
              <input id="cs2-pnl-fixed" type="number" className="sila-input" value={input.fixedEventExpensesAed} onChange={(e) => setNum("fixedEventExpensesAed", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-staff">Manpower Cost (AED)</label>
              <input id="cs2-pnl-staff" type="number" className="sila-input" value={input.manpowerCostAed} onChange={(e) => setNum("manpowerCostAed", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-disc">Discount (%)</label>
              <input id="cs2-pnl-disc" type="number" className="sila-input" min={0} max={80} value={input.discountPercent} onChange={(e) => setNum("discountPercent", Number(e.target.value) || 0)} />
            </div>
            <div className="sila-field">
              <label className="sila-label" htmlFor="cs2-pnl-margin">Target Margin (%)</label>
              <input id="cs2-pnl-margin" type="number" className="sila-input" min={1} max={90} value={input.targetMarginPercent} onChange={(e) => setNum("targetMarginPercent", Number(e.target.value) || 45)} />
            </div>
          </div>
        </section>

        <section className="cs2-card" id="pnl-sim-chart" aria-label="P&L Waterfall Chart">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Revenue vs Cost &amp; Event Contribution (AED)</h3>
            <span className="cs2-badge cs2-badge--demo">Live Recharts</span>
          </div>
          <div style={{ width: "100%", height: 280 }}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 10, right: 16, left: 10, bottom: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                <XAxis dataKey="name" tick={{ fontSize: 12 }} />
                <YAxis tick={{ fontSize: 12 }} />
                <Tooltip formatter={(val: unknown) => `AED ${Number(val).toLocaleString()}`} />
                <Bar dataKey="value" fill="#087bb5" radius={[6, 6, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </section>
      </div>

      {saved.length > 0 && (
        <section className="cs2-card" id="pnl-sim-scenarios">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Saved What-If Scenarios</h3>
          </div>
          <div className="cs2-table-wrap">
            <table className="cs2-table">
              <thead>
                <tr>
                  <th>Scenario</th>
                  <th className="cs2-num">Guests</th>
                  <th className="cs2-num">Price / Guest</th>
                  <th className="cs2-num">Net Revenue</th>
                  <th className="cs2-num">Contribution</th>
                  <th className="cs2-num">Margin %</th>
                </tr>
              </thead>
              <tbody>
                {saved.map((sc, i) => (
                  <tr key={`${sc.label}-${i}`}>
                    <td><strong>{sc.label}</strong></td>
                    <td className="cs2-num">{sc.input.guestCount}</td>
                    <td className="cs2-num">AED {sc.input.sellingPricePerGuestAed}</td>
                    <td className="cs2-num">AED {sc.netRevenueAed.toLocaleString()}</td>
                    <td className="cs2-num">AED {sc.contributionAed.toLocaleString()}</td>
                    <td className="cs2-num">{sc.marginPercent.toFixed(2)}%</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  );
};

export default CulinaryPnlSimulatorWorkspace;
