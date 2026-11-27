import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const TWIN_BASE = "/api/v1/buyer/sila/me-studio/twin";

export interface TwinComponentSummary {
  componentKey: string;
  displayName: string;
  recordCount: number;
  summaryMetric: string;
  dataQuality: string;
  lastSourceUpdateUtc?: string | null;
}

export interface TwinSnapshotDto {
  snapshotId: string;
  buyerId: string;
  propertyId?: string | null;
  outletLocationId?: string | null;
  menuPlanId?: string | null;
  eventId?: string | null;
  snapshotType: string;
  versionNumber: number;
  snapshotHash: string;
  calculationVersion: string;
  modelVersion: string;
  dataQualityScore: number;
  dataQualityLabel: string;
  businessEffectiveDateUtc: string;
  sourceMaxUpdatedAtUtc: string;
  dataFreshnessUtc: string;
  expiresAtUtc?: string | null;
  isStale: boolean;
  stalenessReason?: string | null;
  capturedAtUtc: string;
  components: TwinComponentSummary[];
  metrics: Record<string, unknown>;
  sourceLineage: string[];
}

export interface TwinImpactedObject {
  nodeId: string;
  nodeType: string;
  label: string;
  hopDistance: number;
  baselineCost: number;
  shockedCost: number;
  costDelta: number;
  costDeltaPercent: number;
  marginDelta: number;
}

export interface TwinDependencyAnalysisResult {
  nodeCount: number;
  edgeCount: number;
  isAcyclic: boolean;
  cycles: string[][];
  integrityIssues: { issueType: string; severity: string; nodeIds: string[]; detail: string }[];
  shockNodeId?: string | null;
  priceChangePercent: number;
  impactedObjects: TwinImpactedObject[];
  affectedRecipes: string[];
  affectedMenuItems: string[];
  affectedMenus: string[];
  affectedEvents: string[];
  totalCostImpact: number;
  totalMarginImpact: number;
  engineVersion: string;
}

export interface MePhase3OptimizationSolution {
  scenarioLabel: string;
  paretoRank: number;
  solverStatus: string;
  isFeasible: boolean;
  isGloballyOptimal: boolean;
  optimalityGap?: number | null;
  solverRuntimeMs: number;
  totalRevenue: number;
  totalFoodCost: number;
  foodCostPercent: number;
  totalContributionMargin: number;
  expectedWasteCost: number;
  inventoryUtilizationPercent: number;
  totalPrepMinutes: number;
  hardConstraintsSatisfied: boolean;
  tradeOffExplanation: string[];
  selectedDishes: {
    dishId: string;
    name: string;
    category: string;
    plannedPortions: number;
    unitCost: number;
    totalCost: number;
    contributionMargin: number;
    prepMinutes: number;
    inventoryCoverage: number;
  }[];
}

export interface MePhase3OptimizationJob {
  jobId: string;
  runType: string;
  status: string;
  baselineSnapshotId?: string | null;
  solverUsed: string;
  runtimeMs: number;
  optimalityGap?: number | null;
  solutions: MePhase3OptimizationSolution[];
  infeasibilityReasons: string[];
  startedAtUtc: string;
  completedAtUtc?: string | null;
  isStale: boolean;
}

export interface MePhase3InventoryMenuResult {
  status: string;
  inventoryUtilizationPercent: number;
  avoidablePurchaseCost: number;
  expiryAvoidanceValue: number;
  foodCostPercent: number;
  expectedContributionMargin: number;
  remainingShortages: Record<string, number>;
  excludedStockSummary: Record<string, number>;
  selectedDishes: MePhase3OptimizationSolution["selectedDishes"];
}

export interface MePhase3ProductionFeasibility {
  menuPlanId?: string | null;
  isFeasible: boolean;
  activeKitchenCount: number;
  activeEquipmentCount: number;
  activeStaffCount: number;
  totalRequiredPrepMinutes: number;
  availableKitchenMinutes: number;
  utilizationPercent: number;
  bottlenecks: string[];
  recommendations: string[];
}

export interface MeMonteCarloResult {
  simulationRunId: string;
  seed: number;
  runs: number;
  expectedRevenue: number;
  expectedCost: number;
  medianCost: number;
  costP10: number;
  costP90: number;
  expectedFoodCostPercent: number;
  expectedContributionMargin: number;
  marginP10: number;
  marginP90: number;
  downsideVar95Margin: number;
  probExceedBudget: number;
  probExceedFoodCostTarget: number;
  probIngredientShortage: number;
  runtimeMs: number;
  assumptions: Record<string, unknown>;
}

export interface MeSensitivityResult {
  baselineMargin: number;
  baselineFoodCostPercent: number;
  breakEvenFoodCostCeiling: number;
  topRiskDrivers: string[];
  tornado: {
    variableName: string;
    category: string;
    lowMargin: number;
    highMargin: number;
    marginSwing: number;
    lowFoodCostPercent: number;
    highFoodCostPercent: number;
    foodCostSwingPercent: number;
  }[];
}

export interface MeStressTestResult {
  baselineMargin: number;
  outcomes: {
    stressType: string;
    label: string;
    revisedGuests: number;
    revisedRevenue: number;
    revisedFoodCost: number;
    revisedFoodCostPercent: number;
    revisedMargin: number;
    revisedPrepMinutes: number;
    isOperationallyFeasible: boolean;
    shortageRisk: string;
    correctiveActions: string[];
  }[];
}

export interface MeScenarioLabItem {
  scenarioId: string;
  menuPlanId?: string | null;
  baselineSnapshotId?: string | null;
  baselineSnapshotVersion?: number | null;
  scenarioName: string;
  scenarioType: string;
  status: string;
  solverStatus: string;
  paretoRank?: number | null;
  isPreferred: boolean;
  isBaselineStale: boolean;
  calculationVersion: string;
  modelVersion: string;
  expectedRevenue: number;
  expectedFoodCostPercent: number;
  expectedContributionMargin: number;
  expectedWastePercent: number;
  inventoryUtilizationPercent: number;
  compositeRankScore: number;
  isFeasible: boolean;
  tradeOffExplanation: string;
  dateCreated: string;
}

export interface MeScenarioComparisonResult {
  calculationBasis: string;
  feasibleScenarios: MeScenarioLabItem[];
  infeasibleScenarios: MeScenarioLabItem[];
}

export interface MeWastePredictResult {
  yieldAndWasteSeparated: boolean;
  foodSafetyPriorityEnforced: boolean;
  totalPredictedWasteCost: number;
  items: {
    itemId: string;
    name: string;
    prepWasteQty: number;
    cookingYieldLossQty: number;
    overproductionQty: number;
    buffetLeftoverQty: number;
    expiryRiskQty: number;
    totalPredictedWasteQty: number;
    predictedWasteCost: number;
    recommendations: string[];
  }[];
}

export interface MeHierarchicalForecastResult {
  horizonDays: number;
  reconciliationMethod: string;
  leakageChecked: boolean;
  consistencyMaxGap: number;
  nodes: {
    nodeKey: string;
    level: string;
    parentKey?: string | null;
    selectedModel: string;
    reconciledForecast: number[];
    lowerP10: number[];
    upperP90: number[];
    mae: number;
    wape: number;
    bias: number;
    coveragePercent: number;
  }[];
}

export interface MeHistoricalSimilarityResult {
  targetTitle: string;
  matches: {
    recordId: string;
    title: string;
    similarityScore: number;
    matchingFactors: string[];
    importantDifferences: string[];
    actualFoodCostPercent: number;
    actualWastePercent: number;
    actualMargin: number;
  }[];
}

export interface MeModelGovernanceStatus {
  activeChampionModel: string;
  candidateChallengerModel?: string | null;
  evaluations: {
    modelName: string;
    modelVersion: string;
    mae: number;
    wape: number;
    bias: number;
    intervalCoveragePercent: number;
    selected: boolean;
  }[];
  driftEvents: {
    driftEventId: string;
    modelName: string;
    modelVersion: string;
    driftType: string;
    baselineMetric: number;
    observedMetric: number;
    status: string;
    championModelVersion?: string | null;
    challengerModelVersion?: string | null;
    detectedAtUtc: string;
  }[];
  recentOutcomeLineage: {
    menuPlanId: string;
    menuPlanName: string;
    predictedCovers: number;
    actualCovers: number;
    predictedFoodCostPercent: number;
    actualFoodCostPercent: number;
    forecastBiasPercent: number;
    driftDetected: boolean;
    capturedAtUtc: string;
  }[];
}

export async function createTwinSnapshot(payload: {
  propertyId?: string;
  outletLocationId?: string;
  menuPlanId?: string;
  snapshotType?: string;
}): Promise<TwinSnapshotDto> {
  try {
    const { data } = await axiosInstance.post<TwinSnapshotDto>(`${TWIN_BASE}/snapshots`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getTwinSnapshots(menuPlanId?: string): Promise<TwinSnapshotDto[]> {
  try {
    const { data } = await axiosInstance.get<TwinSnapshotDto[]>(`${TWIN_BASE}/snapshots`, {
      params: menuPlanId ? { menuPlanId } : undefined,
    });
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function refreshTwinSnapshot(snapshotId: string): Promise<TwinSnapshotDto> {
  try {
    const { data } = await axiosInstance.post<TwinSnapshotDto>(`${TWIN_BASE}/snapshots/${snapshotId}/refresh`);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function analyzeTwinDependencies(payload: {
  propertyId?: string;
  menuPlanId?: string;
  shockIngredientName?: string;
  priceChangePercent: number;
}): Promise<TwinDependencyAnalysisResult> {
  try {
    const { data } = await axiosInstance.post<TwinDependencyAnalysisResult>(`${TWIN_BASE}/dependency-graph`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function runMultiObjectiveOptimization(payload: {
  propertyId?: string;
  menuPlanId?: string;
  baselineSnapshotId?: string;
  mode?: string;
  menuFormat?: string;
  guestCount?: number;
  targetSellingPrice?: number;
  maxFoodCostPercent?: number;
  minTotalItems?: number;
  maxTotalItems?: number;
  excludedAllergens?: string[];
  requiredDietaryTags?: string[];
}): Promise<MePhase3OptimizationJob> {
  try {
    const { data } = await axiosInstance.post<MePhase3OptimizationJob>(`${TWIN_BASE}/optimize`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function optimizeInventoryMenu(payload: {
  propertyId?: string;
  guestCount?: number;
  minItems?: number;
  maxItems?: number;
  maxFoodCostPercent?: number;
}): Promise<MePhase3InventoryMenuResult> {
  try {
    const { data } = await axiosInstance.post<MePhase3InventoryMenuResult>(`${TWIN_BASE}/optimize-inventory-menu`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getProductionFeasibility(menuPlanId?: string, propertyId?: string): Promise<MePhase3ProductionFeasibility> {
  try {
    const { data } = await axiosInstance.get<MePhase3ProductionFeasibility>(`${TWIN_BASE}/production-feasibility`, {
      params: { menuPlanId, propertyId },
    });
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function runMonteCarlo(payload: {
  propertyId?: string;
  menuPlanId?: string;
  baselineSnapshotId?: string;
  seed?: number;
  runs?: number;
  guestCountMean?: number;
  guestCountStd?: number;
  sellingPricePerGuest?: number;
  baseFoodCostPerServing?: number;
  ingredientPriceVolPercent?: number;
  eventBudget?: number;
  targetFoodCostPercent?: number;
}): Promise<MeMonteCarloResult> {
  try {
    const { data } = await axiosInstance.post<MeMonteCarloResult>(`${TWIN_BASE}/monte-carlo`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function runSensitivity(payload: {
  propertyId?: string;
  menuPlanId?: string;
  targetFoodCostPercent?: number;
}): Promise<MeSensitivityResult> {
  try {
    const { data } = await axiosInstance.post<MeSensitivityResult>(`${TWIN_BASE}/sensitivity`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function runStressTest(payload: {
  propertyId?: string;
  menuPlanId?: string;
  baselineGuests?: number;
}): Promise<MeStressTestResult> {
  try {
    const { data } = await axiosInstance.post<MeStressTestResult>(`${TWIN_BASE}/stress-test`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getScenarioLab(menuPlanId?: string): Promise<MeScenarioComparisonResult> {
  try {
    const { data } = await axiosInstance.get<MeScenarioComparisonResult>(`${TWIN_BASE}/scenarios`, {
      params: menuPlanId ? { menuPlanId } : undefined,
    });
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function createTwinScenario(payload: {
  propertyId?: string;
  menuPlanId?: string;
  baselineSnapshotId?: string;
  scenarioName: string;
  scenarioType: string;
  notes?: string;
}): Promise<MeScenarioLabItem> {
  try {
    const { data } = await axiosInstance.post<MeScenarioLabItem>(`${TWIN_BASE}/scenarios`, payload);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function copyTwinScenario(scenarioId: string): Promise<MeScenarioLabItem> {
  try {
    const { data } = await axiosInstance.post<MeScenarioLabItem>(`${TWIN_BASE}/scenarios/${scenarioId}/copy`);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function archiveTwinScenario(scenarioId: string): Promise<MeScenarioLabItem> {
  try {
    const { data } = await axiosInstance.post<MeScenarioLabItem>(`${TWIN_BASE}/scenarios/${scenarioId}/archive`);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function convertTwinScenarioToDraft(scenarioId: string): Promise<{ menuPlanId: string }> {
  try {
    const { data } = await axiosInstance.post<{ menuPlanId: string }>(`${TWIN_BASE}/scenarios/${scenarioId}/convert-to-draft`);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getWastePrediction(propertyId?: string, menuPlanId?: string): Promise<MeWastePredictResult> {
  try {
    const { data } = await axiosInstance.get<MeWastePredictResult>(`${TWIN_BASE}/predict/waste`, {
      params: { propertyId, menuPlanId },
    });
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getHierarchicalForecast(propertyId?: string, horizonDays = 7): Promise<MeHierarchicalForecastResult> {
  try {
    const { data } = await axiosInstance.get<MeHierarchicalForecastResult>(`${TWIN_BASE}/forecast/hierarchical`, {
      params: { propertyId, horizonDays },
    });
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getHistoricalSimilarity(menuPlanId?: string): Promise<MeHistoricalSimilarityResult> {
  try {
    const { data } = await axiosInstance.get<MeHistoricalSimilarityResult>(`${TWIN_BASE}/historical-similarity`, {
      params: menuPlanId ? { menuPlanId } : undefined,
    });
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function getModelGovernanceStatus(): Promise<MeModelGovernanceStatus> {
  try {
    const { data } = await axiosInstance.get<MeModelGovernanceStatus>(`${TWIN_BASE}/learning/governance`);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}

export async function captureOutcomeLearning(menuPlanId: string): Promise<unknown> {
  try {
    const { data } = await axiosInstance.post(`${TWIN_BASE}/learning/capture-outcome/${menuPlanId}`);
    return data;
  } catch (e) {
    throw new Error(readError(e));
  }
}
