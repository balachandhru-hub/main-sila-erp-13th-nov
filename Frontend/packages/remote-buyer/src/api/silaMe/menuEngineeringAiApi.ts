import axiosInstance from "../axiosInstance";
import { readError } from "../readError";
import type { MenuFilter } from "./silaMenuEngineeringApi";
import type { WhatIfSimulationResult } from "./menuEngineeringStudioApi";

const AI_BASE = "/api/v1/buyer/sila/me-studio/ai";

export type AiOperatingMode = "MANUAL" | "AI_ASSISTED" | "AGENTIC";

export interface MenuEngineeringAgentDescriptor {
  agentId: string;
  displayName: string;
  equivalentExistingAgent: string;
  supportedIntents: string[];
  requiredPermissions: string[];
  authorizedTools: string[];
  dataDependencies: string[];
  maxSteps: number;
  tokenBudget: number;
  timeoutSeconds: number;
  modelTier: string;
  riskLevel: string;
  requiresApprovalForMutations: boolean;
}

export interface MenuEngineeringToolDescriptor {
  toolId: string;
  displayName: string;
  category: string;
  requiredPermission: string;
  mutatesDraftState: boolean;
  requiresApproval: boolean;
  backingService: string;
}

export interface AiOperatingModeStatus {
  operatingMode: AiOperatingMode;
  canUseAiAssisted: boolean;
  canUseAgentic: boolean;
  aiProviderAvailable: boolean;
  providerMode: string;
  dailyUsageCount: number;
  dailyUsageLimit: number;
  dailyTokensUsed: number;
  dailyTokenBudget: number;
  monthlyEstimatedCostUsd: number;
  monthlyCostBudgetUsd: number;
  fallbackNotice?: string | null;
  agents: MenuEngineeringAgentDescriptor[];
  tools: MenuEngineeringToolDescriptor[];
}

export interface AiContextualResponse {
  agentId: string;
  screenKey: string;
  action: string;
  operatingMode: AiOperatingMode;
  valueTag: "ACTUAL" | "FORECAST" | "AI_RECOMMENDATION" | "SIMULATION" | string;
  summary: string;
  explanation: string;
  evidence: string[];
  warnings: string[];
  dataQuality: string;
  dataFreshness: string;
  confidence: number;
  financialRedacted: boolean;
  aiProviderUsed: boolean;
  proposedActions: {
    actionType: string;
    label: string;
    requiresApproval: boolean;
    targetEndpoint: string;
    payload: Record<string, unknown>;
  }[];
  invokedTools: string[];
}

export interface OptimizedScenarioOption {
  scenarioCode: string;
  label: string;
  strategy: string;
  feasible: boolean;
  conflictReason?: string | null;
  selectedMenuItemIds: string[];
  selectedDishNames: string[];
  foodCostPerGuest: number;
  totalFoodCost: number;
  expectedRevenue: number;
  foodCostPercent: number;
  contributionMargin: number;
  popularityIndex: number;
  inventoryUtilizationPercent: number;
  estimatedWastePercent: number;
  paretoOptimal: boolean;
  tradeOffSummary: string;
}

export interface MeStudioMultiAgentRunResult {
  runId: string;
  idempotencyKey: string;
  status: string;
  objective: string;
  serviceFormat: string;
  mealPeriod: string;
  expectedCovers: number;
  sellingPricePerGuest: number;
  maxFoodCostPercent: number;
  feasible: boolean;
  conflictExplanation?: string | null;
  recommendedScenarioCode: string;
  createdDraftMenuId?: string | null;
  autoPublished: boolean;
  requiresApprovalToPublish: boolean;
  steps: {
    stepNumber: number;
    agentId: string;
    toolId: string;
    title: string;
    status: string;
    outputSummary: string;
    durationMs: number;
  }[];
  scenarios: OptimizedScenarioOption[];
  explanation: string;
  procurementShortages: string[];
  productionConstraints: string[];
  startedAtUtc: string;
  completedAtUtc?: string | null;
}

export interface AiMenuItemAssistResult {
  suggestedName: string;
  generatedDescription: string;
  multilingualDescriptions: Record<string, string>;
  suggestedCategory: string;
  suggestedCuisine: string;
  matchedRecipeId?: string | null;
  matchedRecipeName?: string | null;
  suggestDraftRecipeConcept: boolean;
  draftRecipeConceptStatus?: string | null;
  duplicateOrSimilarItems: string[];
  suggestedIngredients: string[];
  approvedSubstituteSuggestions: string[];
  validatedAllergens: string[];
  validatedDietaryTags: string[];
  allergenValidationSource?: string | null;
  recommendedPortionSize: number;
  recommendedPortionUom: string;
  estimatedFoodCost?: number | null;
  recommendedSellingPrice?: number | null;
  predictedPopularityScore: number;
  predictedMargin?: number | null;
  seasonalSuitability: string;
  imageIsIllustrativeOnly: boolean;
}

export interface AiMenuCalendarGenerateResult {
  horizon: string;
  days: {
    serviceDate: string;
    mealPeriod: string;
    recommendedMenuItemIds: string[];
    recommendedDishNames: string[];
    inventoryCoveragePercent: number;
    estimatedFoodCostPercent: number;
    warnings: string[];
    createdDraftPlanId?: string | null;
  }[];
  detectedRepetitionIssues: string[];
  expiryUtilizationOpportunities: string[];
  capacityConflicts: string[];
  lowPerformerWarnings: string[];
  requiresUserApproval: boolean;
}

export interface AiPortionOptimizeResult {
  rows: {
    menuItemId: string;
    menuItemName: string;
    standardPortion: number;
    currentPlannedPortion: number;
    avgActualSampledPortion?: number | null;
    recommendedPlannedPortion: number;
    recommendedParticipationRate: number;
    recommendedServingsPerGuest: number;
    recommendedBufferPercent: number;
    recommendedBatchSize: number;
    recommendedReplenishmentQty: number;
    variancePattern: string;
    expectedCostImpact: number;
    expectedWasteImpactPercent: number;
    confidence: number;
  }[];
  totalProjectedFoodCostDelta: number;
  totalProjectedWasteReductionKg: number;
  safetyGuardNotice: string;
}

export interface AiBacktestDemandResult {
  selectedModel: string;
  selectedModelVersion: string;
  dataQuality: string;
  coldStart: boolean;
  dataFreshness: string;
  horizonDays: {
    businessDate: string;
    historicalBaseline: number;
    forecastQuantity: number;
    lowerBound: number;
    upperBound: number;
    confirmedEventsQty: number;
    recommendedDemand: number;
  }[];
  modelEvaluations: {
    modelName: string;
    modelVersion: string;
    mae: number;
    wape: number;
    bias: number;
    intervalCoveragePercent: number;
    selected: boolean;
  }[];
  drivers: string[];
  warnings: string[];
}

export interface AiPosSalesIntelligence {
  dailySummary: string;
  weeklySummary: string;
  anomalies: {
    anomalyType: string;
    severity: string;
    posCode: string;
    title: string;
    actualValue: number;
    expectedValue: number;
    variancePercent: number;
    comparisonPeriod: string;
    evidence: string;
    possibleContributingFactors: string[];
  }[];
  revenueOpportunities: string[];
  disclaimer: string;
}

export interface AiPosMappingSuggestionBatch {
  queue: {
    posCode: string;
    posDescription: string;
    observedQuantity: number;
    candidateMenuItemId?: string | null;
    candidateMenuItemName?: string | null;
    candidateRecipeId?: string | null;
    confidence: number;
    suggestedPortionMultiplier: number;
    suggestedSizeVariant?: string | null;
    suggestedModifier?: string | null;
    additionalRecipeHint?: string | null;
    matchingEvidence: string;
    requiresHumanReview: boolean;
    alternatives: string[];
  }[];
  duplicateConflicts: string[];
  autoMappableDeterministicCount: number;
  humanReviewRequiredCount: number;
}

export interface AiConsumptionRootCauseResult {
  anomalies: {
    materialId: string;
    materialName: string;
    theoreticalQty: number;
    actualQty: number;
    varianceQty: number;
    varianceValue: number;
    rankedCauses: {
      causeCode: string;
      description: string;
      probabilityScore: number;
      supportingEvidence: string;
    }[];
  }[];
  ledgerGuardNotice: string;
}

export interface AiPriceShockResult {
  ingredientName: string;
  priceChangePercent: number;
  affectedRecipes: { recipeId: string; recipeName: string; currentBatchCost: number; predictedBatchCost: number; costDelta: number }[];
  affectedMenuItems: { menuItemId: string; menuItemName: string; currentUnitCost: number; predictedUnitCost: number; currentMargin: number; predictedMargin: number; predictedFoodCostPercent?: number | null }[];
  affectedMenus: { menuPlanId: string; menuName: string; lifecycleStatus: string; serviceDate: string; projectedMenuCostDelta: number }[];
  totalProjectedCostDelta: number;
  recommendedCorrectiveAction: string;
}

export interface AiPricingRecommendationsResult {
  items: {
    menuItemId: string;
    menuItemName: string;
    currentNetPrice: number;
    currentUnitCost: number;
    minimumPriceForTargetMargin: number;
    targetFoodCostPrice: number;
    recommendedPriceLow: number;
    recommendedPriceHigh: number;
    expectedContributionDelta: number;
    elasticityValidated: boolean;
    assumptionNote: string;
  }[];
  elasticityNotice: string;
}

export interface AiNlSimulationResult {
  parsedIntent: string;
  sanitizedCommand: string;
  simulation: WhatIfSimulationResult;
  priceShock?: AiPriceShockResult | null;
  constraints: string[];
  dataLimitations: string[];
  liveDataMutated: boolean;
}

export interface MeRecommendationCockpitItem {
  id: string;
  recommendationCode: string;
  agentId: string;
  category: string;
  targetObjectType?: string | null;
  targetObjectId?: string | null;
  title: string;
  currentBaseline: string;
  proposedChange: string;
  supportingEvidence: string;
  estimatedCostImpact?: number | null;
  estimatedMarginImpact?: number | null;
  realizedFinancialImpact?: number | null;
  dataQuality: string;
  confidence: number;
  priority: string;
  requiresApproval: boolean;
  status: string;
  feedbackRating?: number | null;
  createdAtUtc: string;
}

export interface MeProactiveAlert {
  id: string;
  triggerType: string;
  severity: string;
  title: string;
  summary: string;
  affectedObjectType?: string | null;
  affectedObjectId?: string | null;
  recommendedAction?: string | null;
  status: string;
  createdAtUtc: string;
  cooldownUntilUtc: string;
}

export interface MeSetupWizardReadiness {
  overallReadinessScore: number;
  steps: { stepNumber: number; stepName: string; readinessPercent: number; status: string; detail: string }[];
  suggestedThresholds: { settingKey: string; currentValue: string; suggestedValue: string; rationale: string }[];
  adminApprovalRequiredToApply: boolean;
}

export interface MeLearningSummary {
  activeDatasetVersion: string;
  activeModelVersion: string;
  totalValidatedOutcomes: number;
  avgForecastErrorPercent?: number | null;
  avgRealizedMarginDelta?: number | null;
  modelEvaluations: AiBacktestDemandResult["modelEvaluations"];
  recentOutcomes: {
    id: string;
    datasetVersion: string;
    modelVersion: string;
    predictedFoodCostPercent?: number | null;
    actualFoodCostPercent?: number | null;
    predictedMargin?: number | null;
    actualMargin?: number | null;
    outcomeValidated: boolean;
    outcomeSource: string;
    capturedAtUtc: string;
  }[];
}

export interface SilaStoreRoleIntelligence {
  rolePersona: string;
  roleAlerts: MeProactiveAlert[];
  roleRecommendations: MeRecommendationCockpitItem[];
  highlights: string[];
  quickActions: string[];
}

async function aiGet<T>(path: string, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await axiosInstance.get<T>(`${AI_BASE}${path}`, { params });
    return res.data;
  } catch (e) {
    throw new Error(readError(e, "AI service request failed."));
  }
}

async function aiPost<T>(path: string, body?: unknown, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await axiosInstance.post<T>(`${AI_BASE}${path}`, body, { params });
    return res.data;
  } catch (e) {
    throw new Error(readError(e, "AI service request failed."));
  }
}

export const getMeAiStatus = (propertyId?: string) => aiGet<AiOperatingModeStatus>("/status", { propertyId });
export const runMeContextualAi = (dto: {
  screenKey: string;
  action: string;
  userPrompt?: string;
  propertyId?: string;
  outletLocationId?: string;
  menuPlanId?: string;
  menuItemId?: string;
  operatingMode?: AiOperatingMode;
}) => aiPost<AiContextualResponse>("/contextual", dto);

export const runMeMultiAgentCommand = (dto: {
  objective: string;
  operatingMode?: AiOperatingMode;
  optimizationMode?: string;
  propertyId?: string;
  outletLocationId?: string;
  serviceDate?: string;
  expectedCovers?: number;
  sellingPriceTarget?: number;
  maxFoodCostPercent?: number;
  requiredDietary?: string[];
  excludedAllergens?: string[];
  saveSelectedProposalAsDraft?: boolean;
}) => aiPost<MeStudioMultiAgentRunResult>("/multi-agent-run", dto);

export const cancelMeMultiAgentRun = (runId: string) => aiPost<MeStudioMultiAgentRunResult>(`/multi-agent-run/${runId}/cancel`);
export const submitMeAiFeedback = (dto: { agentId: string; contextScreen?: string; targetEntityId?: string; rating: number; wasAccepted: boolean; correctionNotes?: string }) =>
  aiPost<{ saved: boolean }>("/feedback", dto);

export const assistMeMenuItem = (dto: { dishName: string; description?: string; ingredients?: string[]; existingRecipeId?: string; targetFoodCostPercent?: number; photographIsAiGenerated?: boolean }) =>
  aiPost<AiMenuItemAssistResult>("/menu-items/assist", dto);

export const generateMeMenuCalendar = (dto: { horizon: "WEEKLY" | "MONTHLY"; startDate: string; propertyId?: string; mealPeriod?: string; expectedCoversPerDay?: number; prioritizeAvailableInventory?: boolean; createDraftPlans?: boolean }) =>
  aiPost<AiMenuCalendarGenerateResult>("/menu-planning/generate", dto);

export const optimizeMePortions = (menuPlanId?: string, propertyId?: string) =>
  aiGet<AiPortionOptimizeResult>("/portions/optimize", { menuPlanId, propertyId });

export const backtestMeDemand = (filter: MenuFilter = {}) =>
  aiGet<AiBacktestDemandResult>("/demand/backtest", filter as Record<string, unknown>);

export const getMePosSalesIntelligence = (filter: MenuFilter = {}) =>
  aiGet<AiPosSalesIntelligence>("/pos-sales/intelligence", filter as Record<string, unknown>);

export const suggestMePosMappings = (filter: MenuFilter = {}) =>
  aiGet<AiPosMappingSuggestionBatch>("/pos-mapping/suggest", filter as Record<string, unknown>);

export const getMeConsumptionRootCauses = (filter: MenuFilter = {}) =>
  aiGet<AiConsumptionRootCauseResult>("/consumption/root-causes", filter as Record<string, unknown>);

export const evaluateMePriceShock = (dto: { ingredientQuery: string; priceChangePercent: number; propertyId?: string }) =>
  aiPost<AiPriceShockResult>("/cost/price-shock", dto);

export const getMePricingRecommendations = (filter: MenuFilter = {}) =>
  aiGet<AiPricingRecommendationsResult>("/pricing/recommendations", filter as Record<string, unknown>);

export const runMeNlSimulation = (dto: { command: string; menuPlanId?: string; propertyId?: string }) =>
  aiPost<AiNlSimulationResult>("/simulation/nl", dto);

export const getMeCockpitRecommendations = (propertyId?: string, status?: string) =>
  aiGet<MeRecommendationCockpitItem[]>("/cockpit", { propertyId, status });

export const actOnMeCockpitRecommendation = (recommendationId: string, dto: { action: string; notes?: string; realizedFinancialImpact?: number; actualFoodCostPercent?: number; actualMargin?: number; feedbackRating?: number }) =>
  aiPost<MeRecommendationCockpitItem>(`/cockpit/${recommendationId}/action`, dto);

export const getMeProactiveAlerts = (propertyId?: string) =>
  aiGet<MeProactiveAlert[]>("/proactive-alerts", { propertyId });

export const scanMeProactiveAlerts = (propertyId?: string) =>
  aiPost<MeProactiveAlert[]>("/proactive-alerts/scan", undefined, { propertyId });

export const acknowledgeMeAlert = (alertId: string) =>
  aiPost<MeProactiveAlert>(`/proactive-alerts/${alertId}/acknowledge`);

export const getMeSetupWizard = (propertyId?: string) =>
  aiGet<MeSetupWizardReadiness>("/setup/wizard", { propertyId });

export const getMeLearningSummary = () =>
  aiGet<MeLearningSummary>("/learning");

export const getSilaStoreRoleIntelligence = (rolePersona: string, propertyId?: string) =>
  aiGet<SilaStoreRoleIntelligence>("/mobile-intelligence", { rolePersona, propertyId });
