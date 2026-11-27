import React, { useCallback, useEffect, useState } from "react";
import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { EmptyState, Loader, toastService } from "@vosox/shared-ui";
import {
  listCulinaryRecipeDrafts,
  type CulinaryRecipeDraftSummary,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";
import {
  approveRecipeDraft,
  calculateDraftCosting,
  createNewProductionVersion,
  createProductionRecipeId,
  exportLifecycleReport,
  getApprovalPackage,
  getPhase2AHandover,
  getRecipeReadiness,
  getRecipeVersionHistory,
  listCostingSnapshots,
  optimizeDraftCosting,
  publishProductionRecipe,
  rejectRecipeDraft,
  submitRecipeForApproval,
  assignProductionRecipeImage,
  type SilaCulinaryApprovalPackage,
  type SilaCulinaryCostingResult,
  type SilaCulinaryCostOptimizationResult,
  type SilaCulinaryPhase2AHandover,
  type SilaCulinaryRecipeReadiness,
  type SilaCulinaryRecipeVersionHistoryItem,
} from "../../../../api/silaMe/silaCulinaryLifecycleApi";
import { triggerBlobDownload } from "./culinaryRecipePlannerHelpers";
import type { CulinaryWorkspaceKind } from "../silaRecipeRoutes";
import "./CulinarySuite.css";

interface Props {
  selectedDraftId?: string | null;
  onSelectDraftId?: (draftId: string | null) => void;
  onOpenWorkspace: (target: CulinaryWorkspaceKind) => void;
}

const CulinaryFoodCostStudioWorkspace: React.FC<Props> = ({
  selectedDraftId,
  onSelectDraftId,
  onOpenWorkspace,
}) => {
  const [drafts, setDrafts] = useState<CulinaryRecipeDraftSummary[]>([]);
  const [draftId, setDraftId] = useState<string>(selectedDraftId ?? "");
  const [costing, setCosting] = useState<SilaCulinaryCostingResult | null>(null);
  const [snapshots, setSnapshots] = useState<SilaCulinaryCostingResult[]>([]);
  const [optimization, setOptimization] = useState<SilaCulinaryCostOptimizationResult | null>(null);
  const [readiness, setReadiness] = useState<SilaCulinaryRecipeReadiness | null>(null);
  const [pkg, setPkg] = useState<SilaCulinaryApprovalPackage | null>(null);
  const [versions, setVersions] = useState<SilaCulinaryRecipeVersionHistoryItem[]>([]);
  const [handover2A, setHandover2A] = useState<SilaCulinaryPhase2AHandover | null>(null);

  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  // Interactive simulator inputs
  const [servings, setServings] = useState<string>("");
  const [sellingPrice, setSellingPrice] = useState<string>("65");
  const [targetFc, setTargetFc] = useState<string>("28");
  const [bufferPct, setBufferPct] = useState<string>("0");
  const [priceBumpPct, setPriceBumpPct] = useState<string>("0");
  const [laborPerPortion, setLaborPerPortion] = useState<string>("0");
  const [scenarioName, setScenarioName] = useState<string>("Baseline Simulation");
  const [approvalComment, setApprovalComment] = useState<string>("");
  const [versionReason, setVersionReason] = useState<string>("Controlled recipe update");
  const [subTab, setSubTab] = useState<"costing" | "scenarios" | "readiness" | "lifecycle">("costing");

  useEffect(() => {
    listCulinaryRecipeDrafts()
      .then((list) => {
        setDrafts(list);
        if (!draftId && list.length > 0) {
          setDraftId(list[0].id);
        }
      })
      .catch((err) => toastService.error(err instanceof Error ? err.message : "Failed to load drafts."))
      .finally(() => setLoading(false));
  }, []);

  const loadWorkspaceForDraft = useCallback(async (id: string) => {
    if (!id) return;
    setLoading(true);
    try {
      const [calc, snaps, rd, ap, vers] = await Promise.all([
        calculateDraftCosting(id, {
          sellingPricePerPortion: sellingPrice ? Number(sellingPrice) : undefined,
          targetFoodCostPercent: targetFc ? Number(targetFc) : undefined,
        }),
        listCostingSnapshots(id).catch(() => []),
        getRecipeReadiness(id),
        getApprovalPackage(id),
        getRecipeVersionHistory(id).catch(() => []),
      ]);
      setCosting(calc);
      setServings(String(calc.servings));
      setSnapshots(snaps);
      setReadiness(rd);
      setPkg(ap);
      setVersions(vers);
      setHandover2A(null);
      if (ap.lifecycle.productionRecipeId && ap.lifecycle.lifecycleStatus === "PUBLISHED") {
        getPhase2AHandover(ap.lifecycle.productionRecipeId).then(setHandover2A).catch(() => {});
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Unable to load Food Cost Studio.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (draftId) {
      loadWorkspaceForDraft(draftId);
      onSelectDraftId?.(draftId);
    }
  }, [draftId]);

  const runSimulation = async (saveSnapshot: boolean) => {
    if (!draftId) return;
    setBusy(true);
    try {
      const additionalCosts = Number(laborPerPortion) > 0
        ? [{ name: "Direct Kitchen Labor & Packaging", category: "DIRECT_OPERATING", basis: "PER_PORTION", amount: Number(laborPerPortion) }]
        : [];
      const res = await calculateDraftCosting(draftId, {
        servings: servings ? Number(servings) : undefined,
        sellingPricePerPortion: sellingPrice ? Number(sellingPrice) : undefined,
        targetFoodCostPercent: targetFc ? Number(targetFc) : undefined,
        globalBufferPercent: Number(bufferPct) || 0,
        globalPriceIncreasePercent: Number(priceBumpPct) || 0,
        scenarioName: scenarioName.trim() || "What-If Scenario",
        saveSnapshot,
        isBaseline: !saveSnapshot,
        additionalCosts,
      });
      setCosting(res);
      if (saveSnapshot) {
        const snaps = await listCostingSnapshots(draftId);
        setSnapshots(snaps);
        toastService.success(`Saved immutable costing snapshot ${res.costingReference}.`);
      }
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Costing simulation failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleResetBaseline = () => {
    setBufferPct("0");
    setPriceBumpPct("0");
    setLaborPerPortion("0");
    setScenarioName("Baseline");
    loadWorkspaceForDraft(draftId);
  };

  const handleOptimize = async () => {
    if (!draftId) return;
    setBusy(true);
    try {
      const res = await optimizeDraftCosting(draftId, {
        servings: servings ? Number(servings) : undefined,
        sellingPricePerPortion: sellingPrice ? Number(sellingPrice) : undefined,
        targetFoodCostPercent: targetFc ? Number(targetFc) : undefined,
      });
      setOptimization(res);
      toastService.success(`Generated ${res.suggestions.length} deterministically verified optimization option(s).`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Optimization failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !draftId) return;
    setBusy(true);
    try {
      await assignProductionRecipeImage(draftId, file, "CHEF_PHOTOGRAPH");
      toastService.success("Production recipe photograph assigned.");
      await loadWorkspaceForDraft(draftId);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Image upload failed.");
    } finally {
      setBusy(false);
    }
  };

  const handleExport = async (format: "XLSX" | "CSV" | "PDF") => {
    if (!draftId) return;
    try {
      const { blob, fileName } = await exportLifecycleReport(draftId, "COSTING", format);
      triggerBlobDownload(blob, fileName);
      toastService.success(`Exported ${fileName}`);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Export failed.");
    }
  };

  const runLifecycleAction = async (fn: () => Promise<unknown>, okMsg: string) => {
    setBusy(true);
    try {
      await fn();
      toastService.success(okMsg);
      await loadWorkspaceForDraft(draftId);
    } catch (err: unknown) {
      toastService.error(err instanceof Error ? err.message : "Lifecycle action failed.");
    } finally {
      setBusy(false);
    }
  };

  if (loading && !costing) {
    return <Loader size={24} message="Loading Food Cost Studio & Lifecycle..." />;
  }

  if (drafts.length === 0) {
    return (
      <EmptyState
        title="No AI Recipe Drafts yet"
        description="Generate or refine a recipe draft in AI Recipe Planner first, then map materials and open Food Cost Studio."
        action={
          <button type="button" className="sila-btn sila-btn--primary" onClick={() => onOpenWorkspace("ai-planner")}>
            Open AI Recipe Planner
          </button>
        }
      />
    );
  }

  const chartData = (costing?.lines ?? [])
    .filter((l) => (l.ingredientCost ?? 0) > 0)
    .map((l) => ({
      name: l.description.length > 18 ? `${l.description.slice(0, 18)}…` : l.description,
      cost: Number((l.ingredientCost ?? 0).toFixed(2)),
    }));

  const lc = pkg?.lifecycle;

  return (
    <div className="cs2-shell" data-testid="culinary-food-cost-workspace">
      {/* Header */}
      <div className="cs2-header">
        <div>
          <h2 className="cs2-title">
            Food Cost &amp; Recipe Lifecycle Studio (Phase 1C)
            {lc?.productionRecipeCode && <span className="cs2-badge cs2-badge--live" style={{ marginLeft: 8 }}>{lc.productionRecipeCode} v{lc.productionRecipeVersion}</span>}
          </h2>
          <p className="cs2-subtitle">
            Deterministic Decimal costing, What-If simulations, immutable snapshots, AI cost optimization,
            Readiness Checklist, Approval Package, Production Recipe ID creation, and Publishing.
          </p>
        </div>
        <div className="cs2-actions-row">
          <select
            className="sila-select"
            value={draftId}
            onChange={(e) => setDraftId(e.target.value)}
            aria-label="Select Recipe Draft"
          >
            {drafts.map((d) => (
              <option key={d.id} value={d.id}>
                {d.draftReference} — {d.recipeName}
              </option>
            ))}
          </select>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("XLSX")}>Excel</button>
          <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={() => handleExport("PDF")}>PDF</button>
        </div>
      </div>

      {/* Sub-tabs */}
      <div className="cs2-actions-row">
        <button type="button" className={`sila-btn sila-btn--sm ${subTab === "costing" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setSubTab("costing")}>
          Interactive Costing
        </button>
        <button type="button" className={`sila-btn sila-btn--sm ${subTab === "scenarios" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setSubTab("scenarios")}>
          Snapshots &amp; Scenarios ({snapshots.length})
        </button>
        <button type="button" className={`sila-btn sila-btn--sm ${subTab === "readiness" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setSubTab("readiness")}>
          Readiness ({readiness?.passedCount ?? 0}/{(readiness?.checks.length ?? 0)})
        </button>
        <button type="button" className={`sila-btn sila-btn--sm ${subTab === "lifecycle" ? "sila-btn--primary" : "sila-btn--secondary"}`} onClick={() => setSubTab("lifecycle")}>
          Approval, Recipe ID &amp; Publish ({lc?.lifecycleStatus ?? "DRAFT"})
        </button>
      </div>

      {/* KPI Cards */}
      {costing && (
        <div className="cs2-kpi-grid" data-testid="costing-kpis">
          <div className="cs2-kpi cs2-kpi--accent">
            <span className="cs2-kpi__label">Total Ingredient Cost</span>
            <span className="cs2-kpi__value">{costing.currency} {costing.totalIngredientCost.toFixed(2)}</span>
            <span className="cs2-kpi__hint">Completeness: {costing.costCompleteness} (Missing: {costing.missingPriceCount})</span>
          </div>
          <div className="cs2-kpi cs2-kpi--cyan">
            <span className="cs2-kpi__label">Ingredient Cost / Portion</span>
            <span className="cs2-kpi__value">{costing.currency} {costing.ingredientCostPerPortion.toFixed(3)}</span>
            <span className="cs2-kpi__hint">Servings: {costing.servings} · Total/portion: {costing.totalCostPerPortion.toFixed(2)}</span>
          </div>
          <div className={`cs2-kpi ${(costing.foodCostPercent ?? 0) > (costing.targetFoodCostPercent ?? 100) ? "cs2-kpi--warn" : "cs2-kpi--emerald"}`}>
            <span className="cs2-kpi__label">Food Cost % vs Target</span>
            <span className="cs2-kpi__value">
              {costing.foodCostPercent != null ? `${costing.foodCostPercent.toFixed(2)}%` : "N/A"}
            </span>
            <span className="cs2-kpi__hint">
              Target: {costing.targetFoodCostPercent ?? 28}% · Recommended Net: {costing.currency} {costing.recommendedSellingPriceNet?.toFixed(2) ?? "—"}
            </span>
          </div>
          <div className="cs2-kpi cs2-kpi--emerald">
            <span className="cs2-kpi__label">Estimated Contribution</span>
            <span className="cs2-kpi__value">
              {costing.contributionAmount != null ? `${costing.currency} ${costing.contributionAmount.toFixed(2)}` : "—"}
            </span>
            <span className="cs2-kpi__hint">
              Est. Revenue: {costing.estimatedRevenue != null ? `${costing.currency} ${costing.estimatedRevenue.toFixed(2)}` : "Set selling price"}
            </span>
          </div>
        </div>
      )}

      {/* TAB 1: INTERACTIVE COSTING */}
      {subTab === "costing" && costing && (
        <>
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Interactive Costing Simulator (Baseline Preserved)</h3>
              <div className="cs2-actions-row">
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" onClick={handleResetBaseline}>Reset Baseline</button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={busy} onClick={() => runSimulation(false)}>Recalculate</button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--primary" disabled={busy} onClick={() => runSimulation(true)}>Save Snapshot</button>
                <button type="button" className="sila-btn sila-btn--sm sila-btn--secondary" disabled={busy} onClick={handleOptimize}>Ask Culinary AI</button>
              </div>
            </div>
            <div className="cs2-form-grid">
              <div className="sila-field">
                <label className="sila-label">Scenario Name</label>
                <input className="sila-input" value={scenarioName} onChange={(e) => setScenarioName(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Servings</label>
                <input className="sila-input" type="number" min="1" value={servings} onChange={(e) => setServings(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Selling Price / Portion ({costing.currency} Net)</label>
                <input className="sila-input" type="number" step="0.01" value={sellingPrice} onChange={(e) => setSellingPrice(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Target Food Cost %</label>
                <input className="sila-input" type="number" step="0.5" value={targetFc} onChange={(e) => setTargetFc(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Production Buffer %</label>
                <input className="sila-input" type="number" step="0.5" value={bufferPct} onChange={(e) => setBufferPct(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Price Escalation %</label>
                <input className="sila-input" type="number" step="0.5" value={priceBumpPct} onChange={(e) => setPriceBumpPct(e.target.value)} />
              </div>
              <div className="sila-field">
                <label className="sila-label">Additional Direct Cost / Portion</label>
                <input className="sila-input" type="number" step="0.1" value={laborPerPortion} onChange={(e) => setLaborPerPortion(e.target.value)} />
              </div>
            </div>
          </section>

          {optimization && optimization.suggestions.length > 0 && (
            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Culinary AI Cost Optimization Recommendations (Chef Confirmation Required)</h3>
                <span className="cs2-badge cs2-badge--indigo">{optimization.providerUsed}</span>
              </div>
              {optimization.suggestions.map((s) => (
                <div key={s.suggestionId} style={{ padding: "8px 0", borderBottom: "1px solid #e2e8f0" }}>
                  <strong>{s.title}</strong> — Projected Savings: <strong>{costing.currency} {s.projectedCostSavings.toFixed(2)}</strong>
                  <div className="cs2-empty-note">{s.rationale}</div>
                </div>
              ))}
            </section>
          )}

          <div className="cs2-grid-2">
            <section className="cs2-card" style={{ overflowX: "auto" }}>
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Ingredient Cost Breakdown</h3>
              </div>
              <table className="sila-table" style={{ width: "100%" }}>
                <thead>
                  <tr>
                    <th>Ingredient</th>
                    <th>Material</th>
                    <th>Cost Qty</th>
                    <th>Unit Price</th>
                    <th>Source</th>
                    <th>Cost</th>
                    <th>Share</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {costing.lines.map((l) => (
                    <tr key={l.tempRef}>
                      <td>{l.description}</td>
                      <td><code>{l.materialCode ?? "—"}</code></td>
                      <td>{l.costingQty} {l.costingUom}</td>
                      <td>{l.unitPrice != null ? `${l.priceCurrency} ${l.unitPrice.toFixed(2)}` : "Missing"}</td>
                      <td>{l.priceSource}</td>
                      <td><strong>{l.ingredientCost != null ? `${costing.currency} ${l.ingredientCost.toFixed(2)}` : "Unknown"}</strong></td>
                      <td>{l.percentageOfTotal != null ? `${l.percentageOfTotal.toFixed(1)}%` : "—"}</td>
                      <td><span className={`cs2-badge ${l.validationStatus === "OK" ? "cs2-badge--live" : "cs2-badge--warn"}`}>{l.validationStatus}</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>

            <section className="cs2-card">
              <div className="cs2-card__head">
                <h3 className="cs2-card__title">Ingredient Cost Contribution ({costing.currency})</h3>
              </div>
              <div style={{ width: "100%", height: 280 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={chartData}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" />
                    <XAxis dataKey="name" />
                    <YAxis />
                    <Tooltip formatter={(v: unknown) => `${costing.currency} ${Number(v).toFixed(2)}`} />
                    <Bar dataKey="cost" name="Ingredient Cost" fill="#4f46e5" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </section>
          </div>
        </>
      )}

      {/* TAB 2: SNAPSHOTS & SCENARIOS */}
      {subTab === "scenarios" && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Costing Snapshots &amp; Scenario Comparison</h3>
          </div>
          {snapshots.length === 0 ? (
            <EmptyState title="No saved snapshots yet" description="Use 'Save Snapshot' on the Interactive Costing tab to preserve a versioned costing baseline or What-If scenario." />
          ) : (
            <table className="sila-table" style={{ width: "100%" }}>
              <thead>
                <tr>
                  <th>Ref</th>
                  <th>Scenario</th>
                  <th>Draft Rev</th>
                  <th>Total Cost</th>
                  <th>Per Portion</th>
                  <th>Selling Price</th>
                  <th>Food Cost %</th>
                  <th>Contribution</th>
                  <th>Completeness</th>
                  <th>Approval</th>
                </tr>
              </thead>
              <tbody>
                {snapshots.map((s) => (
                  <tr key={s.snapshotId}>
                    <td><code>{s.costingReference}</code></td>
                    <td><strong>{s.scenarioName}</strong></td>
                    <td>Rev {s.draftRevision}</td>
                    <td>{s.currency} {s.totalIngredientCost.toFixed(2)}</td>
                    <td>{s.currency} {s.ingredientCostPerPortion.toFixed(3)}</td>
                    <td>{s.sellingPricePerPortion != null ? `${s.currency} ${s.sellingPricePerPortion.toFixed(2)}` : "—"}</td>
                    <td>{s.foodCostPercent != null ? `${s.foodCostPercent.toFixed(2)}%` : "—"}</td>
                    <td>{s.contributionAmount != null ? `${s.currency} ${s.contributionAmount.toFixed(2)}` : "—"}</td>
                    <td>{s.costCompleteness}</td>
                    <td><span className="cs2-badge cs2-badge--indigo">{s.approvalStatus}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}

      {/* TAB 3: READINESS CHECKLIST */}
      {subTab === "readiness" && readiness && (
        <section className="cs2-card">
          <div className="cs2-card__head">
            <h3 className="cs2-card__title">Consolidated Recipe Readiness Checklist</h3>
            <span className={`cs2-badge ${readiness.readyForApproval ? "cs2-badge--live" : "cs2-badge--warn"}`}>
              {readiness.readyForApproval ? "Ready for Approval" : `${readiness.blockingCount} Blocking Issue(s)`}
            </span>
          </div>
          <table className="sila-table" style={{ width: "100%" }}>
            <thead>
              <tr>
                <th>Validation</th>
                <th>Owner</th>
                <th>Detail</th>
                <th>Action</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {readiness.checks.map((c) => (
                <tr key={c.code}>
                  <td><strong>{c.label}</strong></td>
                  <td>{c.responsibleTeam}</td>
                  <td>{c.detail}</td>
                  <td>{c.recommendedAction ?? "—"}</td>
                  <td>
                    <span className={`cs2-badge ${c.status === "PASSED" ? "cs2-badge--live" : c.status === "WARNING" ? "cs2-badge--warn" : "cs2-badge--danger"}`}>
                      {c.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      )}

      {/* TAB 4: APPROVAL, PRODUCTION RECIPE ID, IMAGE, PUBLISHING, VERSIONING & PHASE 2A */}
      {subTab === "lifecycle" && pkg && lc && (
        <div className="cs2-grid-2">
          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Controlled Recipe Approval &amp; Publishing</h3>
              <span className="cs2-badge cs2-badge--indigo">{lc.lifecycleStatus}</span>
            </div>

            <div style={{ fontSize: 13, lineHeight: 1.8, marginBottom: 12 }}>
              <div><strong>Draft:</strong> <code>{pkg.draftReference}</code> (Rev {pkg.draftRevision})</div>
              <div><strong>Production Recipe ID:</strong> <code>{lc.productionRecipeCode ?? "Not yet allocated"}</code></div>
              <div><strong>Version:</strong> {lc.productionRecipeVersion ?? "—"} (Active Selling: {lc.productionActiveVersion ?? 0})</div>
              <div><strong>Image Assigned:</strong> {lc.imageAssigned ? `Yes (${lc.imageOrigin})` : "No"}</div>
            </div>

            <div className="sila-field" style={{ marginBottom: 10 }}>
              <label className="sila-label">Approval / Review Comment</label>
              <input className="sila-input" value={approvalComment} onChange={(e) => setApprovalComment(e.target.value)} placeholder="Optional review / rejection note" />
            </div>

            <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
              {lc.lifecycleStatus === "DRAFT" && (
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  disabled={busy}
                  onClick={() => runLifecycleAction(() => submitRecipeForApproval(draftId, approvalComment), "Submitted recipe for approval.")}
                >
                  1. Submit for Approval
                </button>
              )}

              {lc.lifecycleStatus === "PENDING_APPROVAL" && (
                <>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--primary"
                    disabled={busy}
                    onClick={() => runLifecycleAction(() => approveRecipeDraft(draftId, approvalComment), "Recipe approved (revision-locked).")}
                  >
                    2. Approve Revision
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--sm sila-btn--secondary"
                    disabled={busy}
                    onClick={() => runLifecycleAction(() => rejectRecipeDraft(draftId, approvalComment), "Recipe returned/rejected.")}
                  >
                    Reject / Return
                  </button>
                </>
              )}

              {lc.lifecycleStatus === "APPROVED" && !lc.productionRecipeId && (
                <button
                  type="button"
                  className="sila-btn sila-btn--sm sila-btn--primary"
                  disabled={busy}
                  onClick={() =>
                    runLifecycleAction(
                      () => createProductionRecipeId(draftId, `${draftId}-rev-${pkg.draftRevision}`),
                      "Production Recipe ID created cleanly."
                    )
                  }
                >
                  3. Create Production Recipe ID
                </button>
              )}

              {lc.productionRecipeId && (
                <>
                  <label className="sila-btn sila-btn--sm sila-btn--secondary" style={{ cursor: "pointer" }}>
                    4. Upload Dish Photo
                    <input type="file" accept="image/jpeg,image/png,image/webp" hidden onChange={handleImageUpload} />
                  </label>

                  {lc.lifecycleStatus !== "PUBLISHED" && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--sm sila-btn--primary"
                      disabled={busy}
                      onClick={() => runLifecycleAction(() => publishProductionRecipe(draftId), "Recipe published to Recipe Master.")}
                    >
                      5. Publish Active Recipe
                    </button>
                  )}

                  {lc.lifecycleStatus === "PUBLISHED" && (
                    <div style={{ width: "100%", display: "flex", gap: 6, marginTop: 8 }}>
                      <input className="sila-input" value={versionReason} onChange={(e) => setVersionReason(e.target.value)} placeholder="Change reason for new version" />
                      <button
                        type="button"
                        className="sila-btn sila-btn--sm sila-btn--secondary"
                        disabled={busy}
                        onClick={() => runLifecycleAction(() => createNewProductionVersion(draftId, versionReason), "Started new controlled recipe version.")}
                      >
                        Create New Version
                      </button>
                    </div>
                  )}
                </>
              )}
            </div>
          </section>

          <section className="cs2-card">
            <div className="cs2-card__head">
              <h3 className="cs2-card__title">Version History &amp; Phase 2A Handover Contract</h3>
            </div>
            {versions.length > 0 && (
              <ul style={{ margin: "0 0 12px", paddingLeft: 18, fontSize: 13 }}>
                {versions.map((v) => (
                  <li key={v.version}>
                    <strong>v{v.version}</strong> — {v.status} {v.isActiveSelling ? "(Active Selling)" : ""} · {v.changeReason || "Initial release"}
                  </li>
                ))}
              </ul>
            )}

            {handover2A ? (
              <div style={{ background: "#f8fafc", border: "1px solid #e2e8f0", borderRadius: 8, padding: 12, fontSize: 13 }}>
                <strong>Phase 2A Menu &amp; Event Planner Handover Ready</strong>
                <div>Production ID: <code>{handover2A.productionRecipeCode}</code> (v{handover2A.recipeVersion})</div>
                <div>Cost / Portion: {handover2A.currency} {handover2A.costPerPortion?.toFixed(4) ?? "—"} · Snapshot: <code>{handover2A.costingReference ?? "—"}</code></div>
                <div>Ingredients pinned: {handover2A.ingredients.length}</div>
              </div>
            ) : (
              <div className="cs2-empty-note">
                Publish the Production Recipe ID to activate the read-only Phase 2A Menu &amp; Event Planner handover contract.
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
};

export default CulinaryFoodCostStudioWorkspace;
