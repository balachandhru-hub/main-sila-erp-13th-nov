import React, { useCallback, useEffect, useState } from "react";
import { Loader, toastService } from "@vosox/shared-ui";
import {
  approveActionItem,
  dispatchTeamsAlert,
  executeActionItem,
  generateDailyBriefings,
  getControlTower,
  getExecutiveDashboard,
  processPendingEvents,
  publishBusinessEvent,
  replayBusinessEvent,
  setEmergencyControl,
  upsertAutonomyPolicy,
  validateRealizedBenefit,
  type AiControlTowerDto,
  type ExecutiveDashboardDto,
} from "../../../../api/silaMe/menuEngineeringAutonomousApi";
import type { MeStudioPermissionsDto } from "../../../../api/silaMe/menuEngineeringStudioApi";

type ControlTowerTab =
  | "OVERVIEW"
  | "LIVE_AGENTS"
  | "OPPORTUNITIES"
  | "RISKS"
  | "ACTION_PLANS"
  | "PENDING_APPROVALS"
  | "EXECUTION_MONITOR"
  | "REALIZED_BENEFITS"
  | "LEARNING"
  | "INTEGRATIONS";

const TABS: { key: ControlTowerTab; label: string }[] = [
  { key: "OVERVIEW", label: "1. Intelligence Overview" },
  { key: "LIVE_AGENTS", label: "2. Live Agent Activity" },
  { key: "OPPORTUNITIES", label: "3. Opportunities" },
  { key: "RISKS", label: "4. Risks & Exceptions" },
  { key: "ACTION_PLANS", label: "5. Business Action Plans" },
  { key: "PENDING_APPROVALS", label: "6. Pending Approvals" },
  { key: "EXECUTION_MONITOR", label: "7. Execution Monitoring" },
  { key: "REALIZED_BENEFITS", label: "8. Realized Benefits" },
  { key: "LEARNING", label: "9. Learning & Model Performance" },
  { key: "INTEGRATIONS", label: "10. Integration Health" },
];

const PERSONAS = [
  { key: "GENERAL_MANAGER", label: "General Manager" },
  { key: "EXECUTIVE_CHEF", label: "Executive Chef" },
  { key: "FB_DIRECTOR", label: "F&B Director" },
  { key: "FINANCE_CONTROLLER", label: "Finance Controller" },
  { key: "COST_CONTROLLER", label: "Cost Controller" },
  { key: "PROCUREMENT_MANAGER", label: "Procurement Manager" },
  { key: "STORE_MANAGER", label: "Store Manager" },
];

const AUTONOMY_LABELS: Record<number, string> = {
  0: "L0 Disabled",
  1: "L1 Observe",
  2: "L2 Recommend",
  3: "L3 Prepare Drafts",
  4: "L4 Approval Execution",
  5: "L5 Policy Automation",
};

interface Props {
  propertyId?: string;
  permissions: MeStudioPermissionsDto;
}

const num = (v?: number | null, d = 2) => (v == null ? "—" : v.toFixed(d));

export const AiControlTowerPanel: React.FC<Props> = ({ propertyId, permissions }) => {
  const [tab, setTab] = useState<ControlTowerTab>("OVERVIEW");
  const [tower, setTower] = useState<AiControlTowerDto | null>(null);
  const [persona, setPersona] = useState("GENERAL_MANAGER");
  const [execDash, setExecDash] = useState<ExecutiveDashboardDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [ct, ed] = await Promise.all([
        getControlTower(propertyId),
        getExecutiveDashboard(persona, propertyId),
      ]);
      setTower(ct);
      setExecDash(ed);
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Could not load AI Control Tower.");
    } finally {
      setLoading(false);
    }
  }, [propertyId, persona]);

  useEffect(() => {
    void load();
  }, [load]);

  if (loading && !tower) return <Loader />;
  if (!tower) return null;

  const showFinancial = permissions.viewMenuCost || permissions.viewMenuMargin;
  const em = tower.emergencyStatus;

  const runAction = async (fn: () => Promise<void>) => {
    setBusy(true);
    try {
      await fn();
      await load();
    } catch (e) {
      toastService.error(e instanceof Error ? e.message : "Operation failed.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="sme-panel">
      <div className="sme-panel__header" style={{ flexWrap: "wrap", gap: "0.5rem" }}>
        <div>
          <h2 className="sme-panel__title">AI Control Tower — Autonomous Culinary Intelligence</h2>
          <div className="sme-muted">
            Readiness: <strong>{tower.dataReadiness.overallClassification}</strong> ({num(tower.dataReadiness.overallScorePercent, 0)}%) • Model Health:{" "}
            <strong>{tower.modelHealthStatus}</strong> • Active Agents: <strong>{tower.activeAgentsCount}</strong>
          </div>
        </div>
        <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            disabled={busy}
            onClick={() =>
              void runAction(async () => {
                const res = await processPendingEvents(propertyId);
                toastService.success(`Processed ${res.processedCount} events; ${res.actionPlansCreated} plans created.`);
              })
            }
          >
            Process Event Queue ({tower.observability.pendingEventQueueDepth})
          </button>
          <button
            type="button"
            className="sila-btn sila-btn--secondary"
            disabled={busy}
            onClick={() =>
              void runAction(async () => {
                await generateDailyBriefings(propertyId, "CLOUD");
                toastService.success("Generated daily executive briefings.");
              })
            }
          >
            Generate Daily Briefings
          </button>
          {permissions.manageMenuSetup && (
            <button
              type="button"
              className={`sila-btn ${em.autonomousModePaused ? "sila-btn--primary" : "sila-btn--secondary"}`}
              disabled={busy}
              onClick={() =>
                void runAction(async () => {
                  await setEmergencyControl({
                    autonomousModePaused: !em.autonomousModePaused,
                    automatedExecutionDisabled: !em.autonomousModePaused,
                    reason: !em.autonomousModePaused ? "Administrator paused autonomous intelligence from Control Tower" : "Resumed by administrator",
                    cancelPendingJobs: !em.autonomousModePaused,
                  });
                  toastService.success(!em.autonomousModePaused ? "Autonomous execution paused." : "Autonomous intelligence resumed.");
                })
              }
            >
              {em.autonomousModePaused ? "Resume Autonomous Intelligence" : "Emergency Pause All"}
            </button>
          )}
        </div>
      </div>

      {em.autonomousModePaused && (
        <div className="sme-kpi-box" style={{ borderColor: "#dc2626", background: "#fef2f2", marginBottom: "0.65rem" }}>
          <strong>EMERGENCY PAUSE ACTIVE</strong> — {em.emergencyPauseReason || "Autonomous agent execution paused by administrator. Manual operations continue normally."}
        </div>
      )}

      {/* Control Tower KPI Ribbon */}
      <div className="sme-grid" style={{ marginBottom: "0.75rem" }}>
        <div className="sme-kpi-box"><span className="sme-muted">Open Recommendations</span><strong>{tower.openRecommendationsCount}</strong></div>
        <div className="sme-kpi-box"><span className="sme-muted">Critical Risks</span><strong>{tower.criticalRisksCount}</strong></div>
        <div className="sme-kpi-box"><span className="sme-muted">Pending Approvals</span><strong>{tower.pendingApprovalsCount}</strong></div>
        <div className="sme-kpi-box"><span className="sme-muted">Failed Actions</span><strong>{tower.failedActionsCount}</strong></div>
        <div className="sme-kpi-box"><span className="sme-muted">Forecast Accuracy</span><strong>{num(tower.forecastAccuracyPercent, 1)}%</strong></div>
        {showFinancial && (
          <>
            <div className="sme-kpi-box"><span className="sme-muted">Financial Exposure</span><strong>${num(tower.estimatedFinancialExposure)}</strong></div>
            <div className="sme-kpi-box"><span className="sme-muted">Potential Savings</span><strong>${num(tower.potentialSavings)}</strong></div>
            <div className="sme-kpi-box"><span className="sme-muted">Validated Realized Savings</span><strong>${num(tower.validatedRealizedSavings)}</strong></div>
          </>
        )}
      </div>

      {/* 10 Internal Control Tower Tabs */}
      <div style={{ display: "flex", gap: "0.35rem", flexWrap: "wrap", marginBottom: "0.75rem" }}>
        {TABS.map((t) => (
          <button
            key={t.key}
            type="button"
            className={`sila-btn ${tab === t.key ? "sila-btn--primary" : "sila-btn--secondary"}`}
            onClick={() => setTab(t.key)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* TAB 1: INTELLIGENCE OVERVIEW + EXECUTIVE DASHBOARD */}
      {tab === "OVERVIEW" && execDash && (
        <div className="sme-list">
          <div className="sme-panel" style={{ background: "#f8fafc" }}>
            <div className="sme-panel__header" style={{ flexWrap: "wrap", gap: "0.4rem" }}>
              <strong>Executive Intelligence Dashboard ({execDash.currency})</strong>
              <div style={{ display: "flex", gap: "0.3rem", flexWrap: "wrap" }}>
                {PERSONAS.map((p) => (
                  <button
                    key={p.key}
                    type="button"
                    className={`sila-btn ${persona === p.key ? "sila-btn--primary" : "sila-btn--secondary"}`}
                    onClick={() => setPersona(p.key)}
                  >
                    {p.label}
                  </button>
                ))}
              </div>
            </div>
            <div className="sme-grid">
              {showFinancial && (
                <>
                  <div className="sme-kpi-box">
                    <span className="sme-muted">Food Cost (Actual / Target / Next Week Pred.)</span>
                    <strong>{num(execDash.currentFoodCostPercent, 1)}% / {num(execDash.targetFoodCostPercent, 1)}% / {num(execDash.predictedNextWeekFoodCostPercent, 1)}%</strong>
                  </div>
                  <div className="sme-kpi-box"><span className="sme-muted">Contribution Margin</span><strong>{num(execDash.contributionMargin)}</strong></div>
                  <div className="sme-kpi-box"><span className="sme-muted">Waste / Expiry Exposure</span><strong>{num(execDash.wasteCostValue)} / {num(execDash.expiryExposureValue)}</strong></div>
                </>
              )}
              <div className="sme-kpi-box"><span className="sme-muted">Shortage Risk Score</span><strong>{num(execDash.shortageRiskScore, 1)}%</strong></div>
            </div>
            {execDash.majorRisks.length > 0 && (
              <div className="sme-kpi-box" style={{ marginTop: "0.5rem" }}>
                <strong>Major Risks</strong>
                {execDash.majorRisks.map((r, i) => <div key={i} className="sme-muted">• {r}</div>)}
              </div>
            )}
            {execDash.briefings.length > 0 && (
              <div className="sme-kpi-box" style={{ marginTop: "0.5rem" }}>
                <strong>Proactive Daily Briefing ({execDash.briefings[0].persona})</strong>
                <div>{execDash.briefings[0].headline}</div>
                {execDash.briefings[0].keyHighlights.map((h, i) => <div key={i} className="sme-muted">• {h}</div>)}
              </div>
            )}
          </div>
        </div>
      )}

      {/* TAB 2: LIVE AGENT ACTIVITY & AUTONOMY LEVELS */}
      {tab === "LIVE_AGENTS" && (
        <div className="sme-list">
          <div className="sme-muted">
            Executions: {tower.observability.totalAgentExecutions} • Success Rate: {num(tower.observability.successRatePercent, 1)}% •
            Tokens Today: {tower.observability.totalTokensUsedToday}/{tower.observability.dailyTokenBudget} •
            Monthly Cost: ${num(tower.observability.estimatedMonthlyCostUsd)}/${num(tower.observability.monthlyBudgetUsd)}
          </div>
          {tower.autonomyPolicies.map((pol) => (
            <div key={pol.agentId} className="sme-kpi-box" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem", flexWrap: "wrap" }}>
              <div>
                <strong>{pol.displayName}</strong> <span className="sme-muted">({pol.agentId})</span>
                <div className="sme-muted">
                  Level: <strong>{AUTONOMY_LABELS[pol.autonomyLevel] ?? pol.autonomyLevel}</strong> • Threshold: ${num(pol.maxFinancialThreshold, 0)} • Cooldown: {pol.cooldownMinutes}m • v{pol.policyVersion}
                </div>
              </div>
              {permissions.manageMenuSetup && (
                <div style={{ display: "flex", gap: "0.35rem", alignItems: "center" }}>
                  <select
                    className="sila-input"
                    value={pol.autonomyLevel}
                    disabled={busy}
                    onChange={(e) => {
                      const nextLevel = Number(e.target.value);
                      void runAction(async () => {
                        await upsertAutonomyPolicy({
                          propertyId,
                          agentId: pol.agentId,
                          actionType: pol.actionType,
                          autonomyLevel: nextLevel,
                          maxFinancialThreshold: pol.maxFinancialThreshold,
                          materialityThresholdPercent: pol.materialityThresholdPercent,
                          cooldownMinutes: pol.cooldownMinutes,
                          isPaused: pol.isPaused,
                          allowedActions: pol.allowedActions,
                        });
                        toastService.success(`Updated ${pol.displayName} to ${AUTONOMY_LABELS[nextLevel]}.`);
                      });
                    }}
                  >
                    {[0, 1, 2, 3, 4, 5].map((lv) => (
                      <option key={lv} value={lv}>{AUTONOMY_LABELS[lv]}</option>
                    ))}
                  </select>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    disabled={busy}
                    onClick={() =>
                      void runAction(async () => {
                        await setEmergencyControl({ pauseAgentId: pol.agentId, pauseAgent: !pol.isPaused });
                        toastService.success(pol.isPaused ? `Resumed ${pol.displayName}.` : `Paused ${pol.displayName}.`);
                      })
                    }
                  >
                    {pol.isPaused ? "Resume" : "Pause"}
                  </button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* TAB 3: OPPORTUNITIES */}
      {tab === "OPPORTUNITIES" && (
        <div className="sme-list">
          {tower.opportunities.length === 0 ? (
            <div className="sme-muted">No open AI recommendations or opportunities.</div>
          ) : (
            tower.opportunities.map((op) => (
              <div key={op.id} className="sme-kpi-box">
                <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem" }}>
                  <strong>{op.title}</strong>
                  <span className="sme-chip">{op.status} • {op.priority}</span>
                </div>
                <div>{op.proposedChange}</div>
                <div className="sme-muted">Agent: {op.agentId} • Evidence: {op.supportingEvidence}</div>
              </div>
            ))
          )}
        </div>
      )}

      {/* TAB 4: RISKS & EXCEPTIONS */}
      {tab === "RISKS" && (
        <div className="sme-list">
          {tower.risksAndExceptions.length === 0 ? (
            <div className="sme-muted">No active culinary risks or exceptions.</div>
          ) : (
            tower.risksAndExceptions.map((r) => (
              <div key={r.id} className="sme-kpi-box">
                <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem" }}>
                  <strong>{r.title}</strong>
                  <span className="sme-chip sme-chip--warn">{r.severity}</span>
                </div>
                <div>{r.summary}</div>
                <div className="sme-muted">Trigger: {r.triggerType} • Recommended: {r.recommendedAction}</div>
              </div>
            ))
          )}
        </div>
      )}

      {/* TAB 5: BUSINESS ACTION PLANS */}
      {tab === "ACTION_PLANS" && (
        <div className="sme-list">
          <div style={{ display: "flex", gap: "0.4rem", flexWrap: "wrap" }}>
            <button
              type="button"
              className="sila-btn sila-btn--secondary"
              disabled={busy}
              onClick={() =>
                void runAction(async () => {
                  await publishBusinessEvent({
                    propertyId,
                    eventType: "IngredientPriceChanged",
                    sourceSystem: "SILA_CONTROL_TOWER",
                    objectLabel: "Manual Price Shock Trigger (+15%)",
                    materialityScore: 15,
                    payload: { priceDeltaPercent: 15 },
                  });
                  await processPendingEvents(propertyId);
                  toastService.success("Published price change event & generated consolidated Business Action Plan.");
                })
              }
            >
              Simulate +15% Ingredient Price Event
            </button>
          </div>
          {tower.actionPlans.map((plan) => (
            <div key={plan.id} className="sme-kpi-box">
              <div style={{ display: "flex", justifyContent: "space-between", gap: "0.5rem" }}>
                <strong>{plan.title}</strong>
                <span className="sme-chip">{plan.status} • {plan.riskLevel}</span>
              </div>
              <div className="sme-muted">{plan.rootCauseAnalysis}</div>
              {showFinancial && <div className="sme-muted">Expected Impact: ${num(plan.expectedFinancialImpact)} • Operational: {plan.expectedOperationalImpact}</div>}
              <div className="sme-list" style={{ marginTop: "0.4rem" }}>
                {plan.actions.map((item) => (
                  <div key={item.id} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem", borderTop: "1px solid #e2e8f0", paddingTop: "0.3rem" }}>
                    <div>
                      <strong>#{item.sequenceOrder} {item.title}</strong>
                      <div className="sme-muted">
                        Agent: {item.agentId} • Risk: {item.riskClassification} • Approval: {item.approvalStatus} • Exec: {item.executionStatus}
                      </div>
                      <div className="sme-muted">Policy: {item.policyDecision} — {item.policyReason}</div>
                    </div>
                    <div style={{ display: "flex", gap: "0.3rem" }}>
                      {item.approvalStatus === "PENDING" && permissions.approveMenu && (
                        <button
                          type="button"
                          className="sila-btn sila-btn--secondary"
                          disabled={busy}
                          onClick={() =>
                            void runAction(async () => {
                              await approveActionItem(item.id, true, "CLOUD");
                              toastService.success("Action item approved.");
                            })
                          }
                        >
                          Approve
                        </button>
                      )}
                      {(item.approvalStatus === "APPROVED" || item.approvalStatus === "NOT_REQUIRED") && item.executionStatus === "PENDING" && (
                        <button
                          type="button"
                          className="sila-btn sila-btn--primary"
                          disabled={busy}
                          onClick={() =>
                            void runAction(async () => {
                              const res = await executeActionItem(item.id);
                              toastService.success(res.message);
                            })
                          }
                        >
                          Execute
                        </button>
                      )}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* TAB 6: PENDING APPROVALS */}
      {tab === "PENDING_APPROVALS" && (
        <div className="sme-list">
          {tower.pendingApprovals.length === 0 ? (
            <div className="sme-muted">No pending action item approvals.</div>
          ) : (
            tower.pendingApprovals.map((item) => (
              <div key={item.id} className="sme-kpi-box" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem", flexWrap: "wrap" }}>
                <div>
                  <strong>{item.title}</strong>
                  <div className="sme-muted">Risk: {item.riskClassification} • Financial Impact: ${num(item.financialImpact)} • Policy: {item.policyReason}</div>
                </div>
                <div style={{ display: "flex", gap: "0.35rem" }}>
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    disabled={busy || !permissions.approveMenu}
                    onClick={() =>
                      void runAction(async () => {
                        await approveActionItem(item.id, true, "CLOUD");
                        toastService.success("Approved action item.");
                      })
                    }
                  >
                    Approve
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    disabled={busy || !permissions.approveMenu}
                    onClick={() =>
                      void runAction(async () => {
                        await approveActionItem(item.id, false, "CLOUD", "Rejected via Control Tower");
                        toastService.success("Rejected action item.");
                      })
                    }
                  >
                    Reject
                  </button>
                  <button
                    type="button"
                    className="sila-btn sila-btn--secondary"
                    disabled={busy}
                    onClick={() =>
                      void runAction(async () => {
                        await dispatchTeamsAlert({
                          propertyId,
                          alertType: "PENDING_APPROVAL",
                          title: item.title,
                          summary: `Approval required (${item.riskClassification} risk, $${num(item.financialImpact)})`,
                          actionItemId: item.id,
                        });
                        toastService.success("Teams alert queued/dispatched.");
                      })
                    }
                  >
                    Send to Teams
                  </button>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {/* TAB 7: EXECUTION MONITORING & BUSINESS EVENTS */}
      {tab === "EXECUTION_MONITOR" && (
        <div className="sme-list">
          {tower.recentEvents.map((ev) => (
            <div key={ev.id} className="sme-kpi-box" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem", flexWrap: "wrap" }}>
              <div>
                <strong>{ev.eventType}</strong> <span className="sme-chip">{ev.processingStatus}</span>
                <div className="sme-muted">
                  Source: {ev.sourceSystem} • Materiality: {ev.materialityScore} • Routed: {ev.routedAgents.join(", ") || "None"} • Retries: {ev.retryCount}
                </div>
                {ev.deadLetterReason && <div className="sme-muted" style={{ color: "#b91c1c" }}>Dead-letter: {ev.deadLetterReason}</div>}
              </div>
              <button
                type="button"
                className="sila-btn sila-btn--secondary"
                disabled={busy}
                onClick={() =>
                  void runAction(async () => {
                    await replayBusinessEvent(ev.id);
                    toastService.success("Event queued for replay.");
                  })
                }
              >
                Replay
              </button>
            </div>
          ))}
        </div>
      )}

      {/* TAB 8: REALIZED BENEFITS */}
      {tab === "REALIZED_BENEFITS" && (
        <div className="sme-list">
          {tower.realizedBenefits.length === 0 ? (
            <div className="sme-muted">No realized benefit measurements recorded yet.</div>
          ) : (
            tower.realizedBenefits.map((b) => (
              <div key={b.id} className="sme-kpi-box" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem", flexWrap: "wrap" }}>
                <div>
                  <strong>{b.benefitCategory}</strong> <span className="sme-chip">{b.validationStatus}</span>
                  <div className="sme-muted">
                    Baseline: ${num(b.baselineAmount)} • Expected: ${num(b.expectedBenefit)} • Raw Outcome: ${num(b.rawActualOutcome)} • Adjusted Realized: <strong>${num(b.adjustedRealizedBenefit)}</strong>
                  </div>
                  <div className="sme-muted">Methodology: {b.methodology} • Data Quality: {b.dataQuality}</div>
                </div>
                {b.validationStatus === "PENDING_VALIDATION" && permissions.approveMenu && (
                  <button
                    type="button"
                    className="sila-btn sila-btn--primary"
                    disabled={busy}
                    onClick={() =>
                      void runAction(async () => {
                        await validateRealizedBenefit(b.id, true);
                        toastService.success("Realized benefit validated.");
                      })
                    }
                  >
                    Validate Benefit
                  </button>
                )}
              </div>
            ))
          )}
        </div>
      )}

      {/* TAB 9: LEARNING & MODEL PERFORMANCE */}
      {tab === "LEARNING" && (
        <div className="sme-list">
          <div className="sme-kpi-box">
            <strong>Dataset: {tower.learningPerformance.activeDatasetVersion} • Model: {tower.learningPerformance.activeModelVersion}</strong>
            <div className="sme-muted">
              Validated Outcomes: {tower.learningPerformance.totalValidatedOutcomes} • Avg Forecast Error: {num(tower.learningPerformance.avgForecastErrorPercent, 2)}% • Avg Realized Margin Delta: {num(tower.learningPerformance.avgRealizedMarginDelta, 2)}
            </div>
          </div>
          {tower.learningPerformance.recentOutcomes.map((o) => (
            <div key={o.id} className="sme-kpi-box">
              <strong>{o.outcomeSource}</strong> ({o.modelVersion})
              <div className="sme-muted">
                Food Cost Pred/Act: {num(o.predictedFoodCostPercent, 1)}% / {num(o.actualFoodCostPercent, 1)}% • Validated: {o.outcomeValidated ? "Yes" : "No"}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* TAB 10: INTEGRATION HEALTH & DATA READINESS */}
      {tab === "INTEGRATIONS" && (
        <div className="sme-list">
          <div className="sme-kpi-box">
            <strong>Data Readiness: {tower.dataReadiness.overallClassification} ({num(tower.dataReadiness.overallScorePercent, 0)}%)</strong>
            {tower.dataReadiness.blockingReasons.map((r, i) => <div key={i} className="sme-muted">• Blocking: {r}</div>)}
            <div className="sme-grid" style={{ marginTop: "0.4rem" }}>
              {tower.dataReadiness.dimensions.map((d) => (
                <div key={d.dimension} className="sme-kpi-box">
                  <strong>{d.dimension}</strong> <span className="sme-chip">{d.status}</span>
                  <div className="sme-muted">{num(d.completenessPercent, 0)}% • {d.detail}</div>
                </div>
              ))}
            </div>
          </div>
          {tower.integrationHealth.map((ih) => (
            <div key={ih.integrationType} className="sme-kpi-box" style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: "0.5rem", flexWrap: "wrap" }}>
              <div>
                <strong>{ih.integrationType}</strong> <span className={`sme-chip ${ih.classification === "HEALTHY" ? "sme-chip--ok" : "sme-chip--warn"}`}>{ih.classification}</span>
                <div className="sme-muted">{ih.technicalDetail}</div>
                <div className="sme-muted">Impact: {ih.businessImpact} • Action: {ih.recommendedAction}</div>
              </div>
              {permissions.manageMenuSetup && (
                <button
                  type="button"
                  className="sila-btn sila-btn--secondary"
                  disabled={busy}
                  onClick={() =>
                    void runAction(async () => {
                      await setEmergencyControl({ suspendIntegrationType: ih.integrationType, suspendIntegration: !ih.isSuspended });
                      toastService.success(ih.isSuspended ? `Resumed ${ih.integrationType}.` : `Suspended ${ih.integrationType}.`);
                    })
                  }
                >
                  {ih.isSuspended ? "Resume" : "Suspend"}
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
