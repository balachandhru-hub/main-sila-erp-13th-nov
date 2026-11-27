import axiosInstance from "../axiosInstance";
import { readError } from "../readError";
import type {
  MeLearningSummary,
  MeProactiveAlert,
  MeRecommendationCockpitItem,
  MenuEngineeringAgentDescriptor,
} from "./menuEngineeringAiApi";

const AUTO_BASE = "/api/v1/buyer/sila/me-studio/autonomous";

export interface BusinessEventDto {
  id: string;
  propertyId?: string | null;
  eventType: string;
  sourceSystem: string;
  objectType?: string | null;
  objectId?: string | null;
  objectLabel?: string | null;
  effectiveAtUtc: string;
  correlationId: string;
  idempotencyKey: string;
  materialityScore: number;
  processingStatus: string;
  retryCount: number;
  processedAtUtc?: string | null;
  deadLetterReason?: string | null;
  routedAgents: string[];
  payloadHighlights: string[];
}

export interface EventProcessingSummaryDto {
  pendingScanned: number;
  processedCount: number;
  skippedOrCooldownCount: number;
  failedOrRetriedCount: number;
  deadLetteredCount: number;
  actionPlansCreated: number;
  events: BusinessEventDto[];
}

export interface AgentAutonomyPolicyDto {
  id?: string | null;
  propertyId?: string | null;
  agentId: string;
  displayName: string;
  actionType: string;
  autonomyLevel: number;
  autonomyLevelLabel: string;
  maxFinancialThreshold: number;
  materialityThresholdPercent: number;
  cooldownMinutes: number;
  isPaused: boolean;
  policyVersion: number;
  allowedActions: string[];
}

export interface EmergencyControlStatusDto {
  autonomousModePaused: boolean;
  automatedExecutionDisabled: boolean;
  emergencyPauseReason?: string | null;
  emergencyPausedAtUtc?: string | null;
  pausedAgents: string[];
  suspendedIntegrations: string[];
  cancelledJobsCount: number;
}

export interface ActionItemDto {
  id: string;
  actionPlanId: string;
  sequenceOrder: number;
  dependsOnActionId?: string | null;
  agentId: string;
  toolId: string;
  actionType: string;
  title: string;
  riskClassification: "LOW" | "MEDIUM" | "HIGH" | string;
  policyDecision: string;
  policyReason: string;
  financialImpact: number;
  targetObjectType?: string | null;
  targetObjectId?: string | null;
  targetObjectVersion: number;
  approvalStatus: string;
  approvalChannel?: string | null;
  approvedAtUtc?: string | null;
  executionStatus: string;
  externalReference?: string | null;
  resultSummary?: string | null;
  executedAtUtc?: string | null;
}

export interface ActionPlanDto {
  id: string;
  propertyId?: string | null;
  triggerEventId?: string | null;
  correlationId: string;
  title: string;
  businessObjective: string;
  rootCauseAnalysis: string;
  affectedObjects: string[];
  supportingEvidence: string[];
  expectedFinancialImpact: number;
  expectedOperationalImpact: string;
  riskLevel: string;
  status: string;
  requiredApprovals: string[];
  executionDeadlineUtc?: string | null;
  isConsolidated: boolean;
  createdAtUtc: string;
  actions: ActionItemDto[];
}

export interface ActionExecutionResultDto {
  actionItemId: string;
  executionStatus: string;
  policyDecision: string;
  policyReason: string;
  wasIdempotentReplay: boolean;
  requiresReapproval: boolean;
  externalOutcomeReconciled: boolean;
  externalReference?: string | null;
  message: string;
}

export interface RealizedBenefitDto {
  id: string;
  propertyId?: string | null;
  recommendationId?: string | null;
  actionPlanId?: string | null;
  benefitCategory: string;
  baselineAmount: number;
  expectedBenefit: number;
  rawActualOutcome: number;
  adjustedRealizedBenefit: number;
  methodology: string;
  dataQuality: string;
  validationStatus: string;
  validatedAtUtc?: string | null;
  measurementStartUtc: string;
  measurementEndUtc: string;
}

export interface DataReadinessDimensionDto {
  dimension: string;
  status: string;
  completenessPercent: number;
  lastUpdatedUtc?: string | null;
  detail: string;
}

export interface DataReadinessAssessmentDto {
  overallClassification: string;
  overallScorePercent: number;
  highImpactRecommendationsBlocked: boolean;
  blockingReasons: string[];
  dimensions: DataReadinessDimensionDto[];
}

export interface IntegrationHealthItemDto {
  integrationType: string;
  classification: string;
  failureCategory?: string | null;
  technicalDetail: string;
  businessImpact: string;
  affectedAgents: string[];
  recommendedAction: string;
  isSuspended: boolean;
  checkedAtUtc: string;
}

export interface ExecutiveBriefingDto {
  id: string;
  propertyId?: string | null;
  persona: string;
  businessDateUtc: string;
  deliveryChannel: string;
  deliveryStatus: string;
  headline: string;
  keyHighlights: string[];
  urgentRisks: string[];
  pendingDecisions: string[];
}

export interface ExecutiveDashboardDto {
  persona: string;
  currency: string;
  currentFoodCostPercent: number;
  targetFoodCostPercent: number;
  predictedNextWeekFoodCostPercent: number;
  foodCostDeviationPercent: number;
  contributionMargin: number;
  forecastAccuracyPercent: number;
  shortageRiskScore: number;
  expiryExposureValue: number;
  wasteCostValue: number;
  potentialSavingsValue: number;
  validatedRealizedSavingsValue: number;
  majorRisks: string[];
  recommendedActions: string[];
  briefings: ExecutiveBriefingDto[];
}

export interface UsageObservabilityDto {
  totalAgentExecutions: number;
  successRatePercent: number;
  pendingEventQueueDepth: number;
  deadLetterEventCount: number;
  totalTokensUsedToday: number;
  dailyTokenBudget: number;
  estimatedMonthlyCostUsd: number;
  monthlyBudgetUsd: number;
  avgExecutionDurationMs: number;
}

export interface AiControlTowerDto {
  emergencyStatus: EmergencyControlStatusDto;
  dataReadiness: DataReadinessAssessmentDto;
  activeAgentsCount: number;
  recentExecutionsCount: number;
  openRecommendationsCount: number;
  criticalRisksCount: number;
  pendingApprovalsCount: number;
  failedActionsCount: number;
  estimatedFinancialExposure: number;
  potentialSavings: number;
  validatedRealizedSavings: number;
  forecastAccuracyPercent: number;
  modelHealthStatus: string;
  agents: MenuEngineeringAgentDescriptor[];
  autonomyPolicies: AgentAutonomyPolicyDto[];
  recentEvents: BusinessEventDto[];
  actionPlans: ActionPlanDto[];
  pendingApprovals: ActionItemDto[];
  risksAndExceptions: MeProactiveAlert[];
  opportunities: MeRecommendationCockpitItem[];
  realizedBenefits: RealizedBenefitDto[];
  integrationHealth: IntegrationHealthItemDto[];
  learningPerformance: MeLearningSummary;
  observability: UsageObservabilityDto;
}

export interface ProactiveMenuHealthDto {
  menuPlanId?: string | null;
  menuTitle: string;
  currentFoodCostPercent: number;
  targetFoodCostPercent: number;
  unavailableIngredientsCount: number;
  productionFeasible: boolean;
  alerts: string[];
  recommendedNextSteps: string[];
}

async function autoGet<T>(path: string, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await axiosInstance.get<T>(`${AUTO_BASE}${path}`, { params });
    return res.data;
  } catch (e) {
    throw new Error(readError(e, "Autonomous intelligence request failed."));
  }
}

async function autoPost<T>(path: string, body?: unknown, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await axiosInstance.post<T>(`${AUTO_BASE}${path}`, body, { params });
    return res.data;
  } catch (e) {
    throw new Error(readError(e, "Autonomous intelligence request failed."));
  }
}

export const getControlTower = (propertyId?: string) =>
  autoGet<AiControlTowerDto>("/control-tower", { propertyId });

export const getExecutiveDashboard = (persona = "GENERAL_MANAGER", propertyId?: string) =>
  autoGet<ExecutiveDashboardDto>("/executive-dashboard", { persona, propertyId });

export const publishBusinessEvent = (body: {
  propertyId?: string;
  eventType: string;
  sourceSystem?: string;
  objectType?: string;
  objectId?: string;
  objectLabel?: string;
  materialityScore?: number;
  payload?: Record<string, unknown>;
}) => autoPost<BusinessEventDto>("/events", body);

export const processPendingEvents = (propertyId?: string) =>
  autoPost<EventProcessingSummaryDto>("/events/process", {}, { propertyId });

export const replayBusinessEvent = (eventId: string) =>
  autoPost<BusinessEventDto>(`/events/${eventId}/replay`, {});

export const getAutonomyPolicies = (propertyId?: string) =>
  autoGet<AgentAutonomyPolicyDto[]>("/autonomy-policies", { propertyId });

export const upsertAutonomyPolicy = (body: {
  propertyId?: string;
  agentId: string;
  actionType?: string;
  autonomyLevel: number;
  maxFinancialThreshold: number;
  materialityThresholdPercent: number;
  cooldownMinutes: number;
  isPaused: boolean;
  allowedActions?: string[];
}) => autoPost<AgentAutonomyPolicyDto>("/autonomy-policies", body);

export const setEmergencyControl = (body: {
  autonomousModePaused?: boolean;
  automatedExecutionDisabled?: boolean;
  reason?: string;
  pauseAgentId?: string;
  pauseAgent?: boolean;
  suspendIntegrationType?: string;
  suspendIntegration?: boolean;
  cancelPendingJobs?: boolean;
}) => autoPost<EmergencyControlStatusDto>("/emergency-control", body);

export const approveActionItem = (actionItemId: string, approved = true, approvalChannel = "CLOUD", notes?: string) =>
  autoPost<ActionItemDto>(`/action-items/${actionItemId}/approve`, { approved, approvalChannel, notes });

export const executeActionItem = (actionItemId: string, idempotencyKey?: string, simulateExternalTimeout = false) =>
  autoPost<ActionExecutionResultDto>(`/action-items/${actionItemId}/execute`, { idempotencyKey, simulateExternalTimeout });

export const recordRealizedBenefit = (body: {
  propertyId?: string;
  recommendationId?: string;
  actionPlanId?: string;
  benefitCategory: string;
  baselineAmount: number;
  expectedBenefit: number;
  rawActualOutcome: number;
  guestCountAdjustmentFactor?: number;
  priceChangeAdjustmentFactor?: number;
  seasonalityAdjustmentFactor?: number;
}) => autoPost<RealizedBenefitDto>("/benefits", body);

export const validateRealizedBenefit = (benefitId: string, approved = true) =>
  autoPost<RealizedBenefitDto>(`/benefits/${benefitId}/validate`, {}, { approved });

export const generateDailyBriefings = (propertyId?: string, channel = "CLOUD") =>
  autoPost<ExecutiveBriefingDto[]>("/briefings/generate", {}, { propertyId, channel });

export const dispatchTeamsAlert = (body: {
  propertyId?: string;
  alertType: string;
  title: string;
  summary: string;
  actionItemId?: string;
}) => autoPost<{ delivered: boolean }>("/teams/notify", body);

export const getProactiveMenuHealth = (menuPlanId?: string, propertyId?: string) =>
  autoGet<ProactiveMenuHealthDto>("/menu-health", { menuPlanId, propertyId });
