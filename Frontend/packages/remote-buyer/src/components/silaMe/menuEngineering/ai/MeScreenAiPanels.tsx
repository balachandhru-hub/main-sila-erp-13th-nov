import React, { useState } from "react";
import { toastService } from "@vosox/shared-ui";
import {
  acknowledgeMeAlert,
  actOnMeCockpitRecommendation,
  assistMeMenuItem,
  backtestMeDemand,
  evaluateMePriceShock,
  generateMeMenuCalendar,
  getMeCockpitRecommendations,
  getMeConsumptionRootCauses,
  getMeLearningSummary,
  getMePosSalesIntelligence,
  getMePricingRecommendations,
  getMeProactiveAlerts,
  getMeSetupWizard,
  optimizeMePortions,
  runMeNlSimulation,
  scanMeProactiveAlerts,
  suggestMePosMappings,
  type AiBacktestDemandResult,
  type AiConsumptionRootCauseResult,
  type AiMenuCalendarGenerateResult,
  type AiMenuItemAssistResult,
  type AiNlSimulationResult,
  type AiPortionOptimizeResult,
  type AiPosMappingSuggestionBatch,
  type AiPosSalesIntelligence,
  type AiPriceShockResult,
  type AiPricingRecommendationsResult,
  type MeLearningSummary,
  type MeProactiveAlert,
  type MeRecommendationCockpitItem,
  type MeSetupWizardReadiness,
} from "../../../../api/silaMe/menuEngineeringAiApi";
import { saveMePosMapping } from "../../../../api/silaMe/menuEngineeringStudioApi";
import {
  AiDataQualityIndicator,
  AiRecommendationCard,
  AiRiskIndicator,
  AiValueTag,
} from "./AiSharedWidgets";

export const MenuItemAiAssistantPanel: React.FC<{
  dishName: string;
  sellingPrice?: number;
  onApplySuggestion: (patch: {
    category?: string;
    cuisine?: string;
    description?: string;
    sellingPrice?: number;
    allergenNotes?: string;
  }) => void;
}> = ({ dishName, sellingPrice, onApplySuggestion }) => {
  const [res, setRes] = useState<AiMenuItemAssistResult | null>(null);
  const [ingText, setIngText] = useState("Chicken Breast, Garlic, Olive Oil, Lemon");
  const [loading, setLoading] = useState(false);

  const run = async () => {
    setLoading(true);
    try {
      const data = await assistMeMenuItem({
        dishName: dishName || "Signature Grilled Chicken",
        ingredients: ingText
          .split(",")
          .map((x) => x.trim())
          .filter(Boolean),
        targetFoodCostPercent: sellingPrice && sellingPrice > 0 ? 30 : undefined,
        photographIsAiGenerated: true,
      });
      setRes(data);
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "AI Dish Assistant failed.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" />{" "}
          <strong>AI Dish Creation, Classification &amp; Multilingual Assistant</strong>
        </div>
        <button type="button" className="sila-btn sila-btn--primary" disabled={loading} onClick={() => void run()}>
          {loading ? "Analyzing..." : "Generate AI Dish Intelligence"}
        </button>
      </div>
      <div className="sme-row">
        <input
          className="sila-input"
          style={{ flex: 1 }}
          value={ingText}
          onChange={(e) => setIngText(e.target.value)}
          placeholder="Comma-separated ingredients for allergen & recipe check..."
        />
      </div>
      {res && (
        <div className="sme-list">
          <div className="sme-muted">
            Category: <strong>{res.suggestedCategory}</strong> · Cuisine: <strong>{res.suggestedCuisine}</strong> ·{" "}
            Portion: <strong>{res.recommendedPortionSize} {res.recommendedPortionUom}</strong> ·{" "}
            Recommended Price: <strong>{(res.recommendedSellingPrice ?? 0).toFixed(2)}</strong> ·{" "}
            Predicted Popularity: <strong>{(res.predictedPopularityScore * 100).toFixed(0)}%</strong> ·{" "}
            Seasonal: <strong>{res.seasonalSuitability}</strong>
          </div>
          <div><strong>EN:</strong> {res.generatedDescription}</div>
          {res.multilingualDescriptions.ar && <div><strong>AR:</strong> {res.multilingualDescriptions.ar}</div>}
          {res.multilingualDescriptions.fr && <div><strong>FR:</strong> {res.multilingualDescriptions.fr}</div>}
          <div className="sme-muted">
            Validated Allergens ({res.allergenValidationSource || "Ingredient Master"}):{" "}
            <strong>{res.validatedAllergens.join(", ") || "None"}</strong>
            {res.draftRecipeConceptStatus ? ` · Recipe Concept: ${res.draftRecipeConceptStatus} (Never auto-approved)` : ""}
          </div>
          {res.duplicateOrSimilarItems.length > 0 && (
            <AiRiskIndicator severity="WARNING" text={`Similar/duplicate candidate(s): ${res.duplicateOrSimilarItems.join(", ")}`} />
          )}
          {res.imageIsIllustrativeOnly && (
            <div className="sme-muted"><em>AI food images are illustrative previews only and are never represented as actual prepared dishes.</em></div>
          )}
          <div>
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              onClick={() =>
                onApplySuggestion({
                  category: res.suggestedCategory,
                  cuisine: res.suggestedCuisine,
                  description: res.generatedDescription,
                  sellingPrice: res.recommendedSellingPrice ?? undefined,
                  allergenNotes: res.validatedAllergens.join(", "),
                })
              }
            >
              Apply Suggested Classification &amp; Price to Form
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export const MenuPlanningAiPanel: React.FC<{ propertyId?: string }> = ({ propertyId }) => {
  const [horizon, setHorizon] = useState<"WEEKLY" | "MONTHLY">("WEEKLY");
  const [mealPeriod, setMealPeriod] = useState("Dinner");
  const [covers, setCovers] = useState("180");
  const [res, setRes] = useState<AiMenuCalendarGenerateResult | null>(null);

  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" /> <strong>AI Menu Calendar &amp; Rotation Planner</strong>
        </div>
        <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
          <select className="sila-input" value={horizon} onChange={(e) => setHorizon(e.target.value as "WEEKLY" | "MONTHLY")}>
            <option value="WEEKLY">Weekly Plan (7 days)</option>
            <option value="MONTHLY">Monthly Plan (30 days)</option>
          </select>
          <select className="sila-input" value={mealPeriod} onChange={(e) => setMealPeriod(e.target.value)}>
            <option value="Breakfast">Breakfast</option>
            <option value="Lunch">Lunch</option>
            <option value="Dinner">Dinner</option>
            <option value="All-Day">All-Day</option>
          </select>
          <input className="sila-input" style={{ width: "5.5rem" }} type="number" value={covers} onChange={(e) => setCovers(e.target.value)} />
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            onClick={async () => {
              const data = await generateMeMenuCalendar({
                propertyId,
                horizon,
                mealPeriod,
                expectedCoversPerDay: Number(covers) || 180,
                startDate: new Date().toISOString().slice(0, 10),
                prioritizeAvailableInventory: true,
                createDraftPlans: false,
              });
              setRes(data);
            }}
          >
            Generate Recommended Calendar
          </button>
        </div>
      </div>
      {res && (
        <div className="sme-list">
          {res.detectedRepetitionIssues.map((a, i) => (
            <AiRiskIndicator key={`rep-${i}`} severity="WARNING" text={a} />
          ))}
          {res.expiryUtilizationOpportunities.map((a, i) => (
            <AiRiskIndicator key={`exp-${i}`} severity="INFO" text={a} />
          ))}
          <div className="sme-calendar">
            {res.days.slice(0, 14).map((d) => (
              <div key={d.serviceDate} className="sme-cal-day">
                <div className="sme-muted">{d.serviceDate.slice(0, 10)} · {d.mealPeriod}</div>
                <strong>Inv {d.inventoryCoveragePercent.toFixed(0)}%</strong>
                <div className="sme-muted">{d.recommendedDishNames.join(", ")}</div>
                <div className="sme-muted">Est. FC: {d.estimatedFoodCostPercent.toFixed(1)}%</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export const PortionOptimizationAiPanel: React.FC<{ propertyId?: string; menuPlanId?: string }> = ({
  propertyId,
  menuPlanId,
}) => {
  const [res, setRes] = useState<AiPortionOptimizeResult | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" /> <strong>AI Portion Optimization (Approved Master Portions Preserved)</strong>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setRes(await optimizeMePortions(menuPlanId, propertyId))}
        >
          Analyze &amp; Recommend Portions
        </button>
      </div>
      {res && (
        <div className="sme-list">
          <div className="sme-muted">
            {res.safetyGuardNotice} · Est. Waste Reduction: <strong>{res.totalProjectedWasteReductionKg.toFixed(2)} kg</strong> ·{" "}
            <AiValueTag kind="ESTIMATED" /> Cost Delta: <strong>{res.totalProjectedFoodCostDelta.toFixed(2)}</strong>
          </div>
          <table className="sila-table">
            <thead>
              <tr><th>Dish</th><th>Std Portion</th><th>Rec. Portion</th><th>Rec. Part.</th><th>Batch / Replenish</th><th>Pattern</th><th>Waste Δ%</th></tr>
            </thead>
            <tbody>
              {res.rows.map((r) => (
                <tr key={r.menuItemId}>
                  <td><strong>{r.menuItemName}</strong></td>
                  <td>{r.standardPortion}</td>
                  <td><strong>{r.recommendedPlannedPortion}</strong></td>
                  <td>{(r.recommendedParticipationRate * 100).toFixed(0)}% ({r.recommendedServingsPerGuest}x)</td>
                  <td>{r.recommendedBatchSize} / +{r.recommendedReplenishmentQty}</td>
                  <td><span className="sme-chip">{r.variancePattern}</span></td>
                  <td>{r.expectedWasteImpactPercent.toFixed(1)}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export const DemandBacktestAiPanel: React.FC<{ propertyId?: string; outletLocationId?: string }> = ({
  propertyId,
  outletLocationId,
}) => {
  const [res, setRes] = useState<AiBacktestDemandResult | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="FORECAST" /> <strong>Backtested Multi-Model Demand Forecasting (Leakage-Free)</strong>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setRes(await backtestMeDemand({ propertyId, outletLocationId }))}
        >
          Run Model Backtest &amp; Forecast
        </button>
      </div>
      {res && (
        <div className="sme-list">
          <div className="sme-muted">
            Selected Model: <strong>{res.selectedModel}</strong> ({res.selectedModelVersion}) ·{" "}
            <AiDataQualityIndicator quality={res.dataQuality} freshness={res.dataFreshness} />
          </div>
          <div className="sme-grid">
            {res.modelEvaluations.map((m) => (
              <div key={m.modelName} className="sme-kpi-box" style={m.selected ? { borderColor: "#2563eb", background: "#eff6ff" } : undefined}>
                <strong>{m.modelName} {m.selected ? "★" : ""}</strong>
                <div className="sme-muted">MAE: {m.mae.toFixed(2)} · WAPE: {m.wape.toFixed(1)}%</div>
                <div className="sme-muted">Bias: {m.bias.toFixed(2)} · PI Coverage: {m.intervalCoveragePercent.toFixed(0)}%</div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export const PosSalesAiPanel: React.FC<{ propertyId?: string; outletLocationId?: string }> = ({
  propertyId,
  outletLocationId,
}) => {
  const [res, setRes] = useState<AiPosSalesIntelligence | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" /> <strong>AI POS Sales Intelligence &amp; Exception Worklist</strong>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setRes(await getMePosSalesIntelligence({ propertyId, outletLocationId }))}
        >
          Analyze POS Sales Anomalies
        </button>
      </div>
      {res && (
        <div className="sme-list">
          <div>{res.dailySummary}</div>
          <div className="sme-muted">{res.weeklySummary}</div>
          <div className="sme-muted"><em>{res.disclaimer}</em></div>
          {res.anomalies.map((a, i) => (
            <div key={i} className="sme-kpi-box">
              <strong>{a.anomalyType}</strong> ({a.severity}) — {a.title}
              <div className="sme-muted">Actual: {a.actualValue.toFixed(2)} vs Expected: {a.expectedValue.toFixed(2)} · Evidence: {a.evidence}</div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export const PosMappingAiQueuePanel: React.FC<{ propertyId?: string; onApproved: () => void }> = ({
  propertyId,
  onApproved,
}) => {
  const [res, setRes] = useState<AiPosMappingSuggestionBatch | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" /> <strong>AI POS Mapping Auto-Suggest &amp; Review Queue</strong>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setRes(await suggestMePosMappings({ propertyId }))}
        >
          Run AI Mapping Suggestions
        </button>
      </div>
      {res && (
        <table className="sila-table">
          <thead>
            <tr><th>POS Code / Description</th><th>Candidate Item</th><th>Size Mult.</th><th>Modifier</th><th>Confidence</th><th>Evidence</th><th>Review</th></tr>
          </thead>
          <tbody>
            {res.queue.map((s) => (
              <tr key={s.posCode}>
                <td><strong>{s.posCode}</strong><div className="sme-muted">{s.posDescription}</div></td>
                <td>{s.candidateMenuItemName || "No match"}</td>
                <td>{s.suggestedPortionMultiplier}x</td>
                <td>{s.suggestedModifier || "—"}</td>
                <td>{(s.confidence * 100).toFixed(0)}% {s.requiresHumanReview ? "(Human Review)" : "(Rule)"}</td>
                <td className="sme-muted">{s.matchingEvidence}</td>
                <td>
                  {s.candidateMenuItemId && (
                    <button
                      type="button"
                      className="sila-btn sila-btn--secondary"
                      onClick={async () => {
                        await saveMePosMapping({
                          posCode: s.posCode,
                          menuItemId: s.candidateMenuItemId!,
                          mappingType: s.suggestedModifier ? "MODIFIER" : s.suggestedPortionMultiplier !== 1 ? "SIZE_VARIATION" : "ONE_TO_ONE",
                          portionMultiplier: s.suggestedPortionMultiplier,
                          approveMapping: true,
                        });
                        toastService.success(`Approved AI mapping for ${s.posCode}.`);
                        onApproved();
                      }}
                    >
                      Approve
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export const ConsumptionRootCauseAiPanel: React.FC<{ propertyId?: string; outletLocationId?: string }> = ({
  propertyId,
  outletLocationId,
}) => {
  const [res, setRes] = useState<AiConsumptionRootCauseResult | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" /> <strong>AI Consumption Anomaly &amp; Root-Cause Ranker</strong>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setRes(await getMeConsumptionRootCauses({ propertyId, outletLocationId }))}
        >
          Rank Variance Root Causes
        </button>
      </div>
      {res && (
        <div className="sme-list">
          <div className="sme-muted">{res.ledgerGuardNotice}</div>
          {res.anomalies.map((inv) => (
            <div key={inv.materialId} className="sme-kpi-box">
              <strong>{inv.materialName}</strong> — Variance: <strong>{inv.varianceQty.toFixed(2)}</strong>
              {inv.rankedCauses.map((c, idx) => (
                <div key={idx} className="sme-muted">
                  #{idx + 1} [{(c.probabilityScore * 100).toFixed(0)}%] <strong>{c.causeCode}</strong>: {c.description} ({c.supportingEvidence})
                </div>
              ))}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export const CostShockAndPricingAiPanel: React.FC<{ propertyId?: string }> = ({ propertyId }) => {
  const [ingredient, setIngredient] = useState("Chicken");
  const [pctDelta, setPctDelta] = useState("15");
  const [shock, setShock] = useState<AiPriceShockResult | null>(null);
  const [pricing, setPricing] = useState<AiPricingRecommendationsResult | null>(null);

  return (
    <div className="sme-list">
      <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
        <div className="sme-panel__header">
          <div>
            <AiValueTag kind="SIMULATION" /> <strong>AI Ingredient Price Shock Cascader (Reverse Recipe Dependencies)</strong>
          </div>
        </div>
        <div className="sme-row">
          <input className="sila-input" value={ingredient} onChange={(e) => setIngredient(e.target.value)} placeholder="Ingredient name or code (e.g. Chicken, Salmon)" />
          <input className="sila-input" style={{ width: "6rem" }} type="number" value={pctDelta} onChange={(e) => setPctDelta(e.target.value)} />
          <button
            type="button"
            className="sila-btn sila-btn--primary"
            onClick={async () => setShock(await evaluateMePriceShock({ ingredientQuery: ingredient, priceChangePercent: Number(pctDelta) || 15, propertyId }))}
          >
            Run Price Shock Cascade
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            onClick={async () => setPricing(await getMePricingRecommendations({ propertyId }))}
          >
            Generate Intelligent Pricing Recommendations
          </button>
        </div>
        {shock && (
          <div className="sme-list">
            <div>
              <strong>{shock.ingredientName}</strong> ({shock.priceChangePercent > 0 ? "+" : ""}{shock.priceChangePercent}%):{" "}
              {shock.affectedRecipes.length} recipe(s), {shock.affectedMenuItems.length} menu item(s), {shock.affectedMenus.length} menu(s) affected.
              Total Projected Cost Delta: <strong>{shock.totalProjectedCostDelta.toFixed(2)}</strong>.
            </div>
            <div className="sme-muted">Recommended Action: {shock.recommendedCorrectiveAction}</div>
            {shock.affectedMenuItems.map((it) => (
              <div key={it.menuItemId} className="sme-muted">
                • {it.menuItemName}: Cost {it.currentUnitCost.toFixed(2)} → {it.predictedUnitCost.toFixed(2)} (FC {(it.predictedFoodCostPercent ?? 0).toFixed(1)}%)
              </div>
            ))}
          </div>
        )}
      </div>

      {pricing && (
        <div className="sme-panel">
          <div className="sme-panel__header">
            <div><AiValueTag kind="AI_RECOMMENDATION" /> <strong>Intelligent Pricing Recommendations</strong></div>
          </div>
          <div className="sme-muted">{pricing.elasticityNotice}</div>
          <table className="sila-table">
            <thead>
              <tr><th>Dish</th><th>Current Net Price</th><th>Unit Cost</th><th>Min Margin Price</th><th>Target FC Price</th><th>Recommended Range</th><th>Est. CM Delta</th></tr>
            </thead>
            <tbody>
              {pricing.items.map((p) => (
                <tr key={p.menuItemId}>
                  <td><strong>{p.menuItemName}</strong></td>
                  <td>{p.currentNetPrice.toFixed(2)}</td>
                  <td>{p.currentUnitCost.toFixed(2)}</td>
                  <td>{p.minimumPriceForTargetMargin.toFixed(2)}</td>
                  <td>{p.targetFoodCostPrice.toFixed(2)}</td>
                  <td><strong>{p.recommendedPriceLow.toFixed(2)} – {p.recommendedPriceHigh.toFixed(2)}</strong></td>
                  <td>+{p.expectedContributionDelta.toFixed(2)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
};

export const AiCockpitFullPanel: React.FC<{ propertyId?: string; canApprove: boolean }> = ({
  propertyId,
  canApprove,
}) => {
  const [recs, setRecs] = useState<MeRecommendationCockpitItem[] | null>(null);
  const [alerts, setAlerts] = useState<MeProactiveAlert[]>([]);
  const [learning, setLearning] = useState<MeLearningSummary | null>(null);

  const loadAll = async () => {
    const [r, a, l] = await Promise.all([
      getMeCockpitRecommendations(propertyId),
      getMeProactiveAlerts(propertyId),
      getMeLearningSummary(),
    ]);
    setRecs(r);
    setAlerts(a);
    setLearning(l);
  };

  return (
    <div className="sme-list">
      <div className="sme-panel">
        <div className="sme-panel__header">
          <h3 className="sme-panel__title">Recommendation Lifecycle Cockpit, Proactive Alerts &amp; Continuous Learning</h3>
          <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
            <button type="button" className="sila-btn sila-btn--secondary" onClick={() => void loadAll()}>
              Load Cockpit &amp; Learning
            </button>
            <button
              type="button"
              className="sila-btn sila-btn--primary"
              onClick={async () => {
                const scanned = await scanMeProactiveAlerts(propertyId);
                toastService.success(`Proactive scan complete (${scanned.length} active alert(s)).`);
                await loadAll();
              }}
            >
              Run Proactive Intelligence Scan
            </button>
          </div>
        </div>

        {alerts.length > 0 && (
          <div className="sme-list">
            <strong>Active Proactive Alerts ({alerts.length})</strong>
            {alerts.map((al) => (
              <div key={al.id} className="sme-ai-proactive-strip">
                <div>
                  <strong>[{al.severity}] {al.title}</strong> — {al.summary}
                </div>
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary"
                  onClick={async () => {
                    await acknowledgeMeAlert(al.id);
                    await loadAll();
                  }}
                >
                  Acknowledge
                </button>
              </div>
            ))}
          </div>
        )}

        {recs && (
          <div className="sme-list">
            {recs.map((rc) => (
              <AiRecommendationCard
                key={rc.id}
                item={rc}
                canApprove={canApprove}
                onAction={async (id, action) => {
                  await actOnMeCockpitRecommendation(id, { action });
                  toastService.success(`Recommendation transitioned: ${action}`);
                  await loadAll();
                }}
              />
            ))}
          </div>
        )}

        {learning && (
          <div className="sme-kpi-box">
            <strong>Continuous Historical Learning (Tenant-Isolated)</strong>
            <div className="sme-muted">
              Dataset: {learning.activeDatasetVersion} · Model: {learning.activeModelVersion} ·{" "}
              Validated Outcomes: {learning.totalValidatedOutcomes} ·{" "}
              Avg Forecast Error: {learning.avgForecastErrorPercent != null ? `${learning.avgForecastErrorPercent.toFixed(1)}%` : "—"} ·{" "}
              Avg Realized Margin Delta: {learning.avgRealizedMarginDelta != null ? learning.avgRealizedMarginDelta.toFixed(2) : "—"}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export const NlWhatIfAiPanel: React.FC<{ menuPlanId?: string; propertyId?: string }> = ({
  menuPlanId,
  propertyId,
}) => {
  const [command, setCommand] = useState("What happens if salmon cost increases by 20%?");
  const [res, setRes] = useState<AiNlSimulationResult | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div><AiValueTag kind="SIMULATION" /> <strong>Natural Language What-If Simulator</strong></div>
      </div>
      <div className="sme-row">
        <input className="sila-input" style={{ flex: 1 }} value={command} onChange={(e) => setCommand(e.target.value)} />
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setRes(await runMeNlSimulation({ command, menuPlanId, propertyId }))}
        >
          Run Natural Language Simulation
        </button>
      </div>
      {res && (
        <div className="sme-kpi-box">
          <div><strong>{res.parsedIntent}</strong></div>
          <div className="sme-muted">{res.sanitizedCommand}</div>
          <div className="sme-muted">
            Cost: {res.simulation.baselineCost.toFixed(2)} → <strong>{res.simulation.foodCost.toFixed(2)}</strong> ({res.simulation.costVariancePct}%) ·{" "}
            Margin: {res.simulation.baselineMargin.toFixed(2)} → <strong>{res.simulation.contributionMargin.toFixed(2)}</strong> ·{" "}
            Live Data Modified: <strong>{res.liveDataMutated ? "Yes" : "No (Read-Only Sandbox)"}</strong>
          </div>
        </div>
      )}
    </div>
  );
};

export const SetupWizardAiPanel: React.FC<{ propertyId?: string }> = ({ propertyId }) => {
  const [wiz, setWiz] = useState<MeSetupWizardReadiness | null>(null);
  return (
    <div className="sme-panel" style={{ background: "#f8fbff", borderColor: "#bfdbfe" }}>
      <div className="sme-panel__header">
        <div>
          <AiValueTag kind="AI_RECOMMENDATION" /> <strong>10-Step AI Setup Readiness Wizard &amp; Threshold Advisor</strong>
        </div>
        <button
          type="button"
          className="sila-btn sila-btn--primary"
          onClick={async () => setWiz(await getMeSetupWizard(propertyId))}
        >
          Run 10-Step Readiness Audit
        </button>
      </div>
      {wiz && (
        <div className="sme-list">
          <div className="sme-muted">
            Overall Readiness Score: <strong>{wiz.overallReadinessScore.toFixed(1)}%</strong> ·{" "}
            {wiz.adminApprovalRequiredToApply ? "Administrator approval required before applying threshold changes." : "Ready"}
          </div>
          <div className="sme-grid">
            {wiz.steps.map((s) => (
              <div key={s.stepNumber} className="sme-kpi-box">
                <strong>{s.stepNumber}. {s.stepName}</strong>
                <div>Score: <strong>{s.readinessPercent.toFixed(0)}%</strong> ({s.status})</div>
                <div className="sme-muted">{s.detail}</div>
              </div>
            ))}
          </div>
          {wiz.suggestedThresholds.map((sg, i) => (
            <div key={i} className="sme-muted">
              • Suggest <strong>{sg.settingKey}</strong>: {sg.currentValue} → <strong>{sg.suggestedValue}</strong> ({sg.rationale})
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
