import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/culinary-analytics";

export interface SilaCulinaryAnalyticsConfig {
  id: string;
  buyerId: string;
  propertyId?: string | null;
  targetFoodCostPercent: number;
  targetContributionMargin: number;
  popularityThresholdFactor: number;
  menuProfitabilityThresholdOverride?: number | null;
  ingredientPriceAlertThresholdPercent: number;
  wasteTolerancePercent: number;
  consumptionVarianceTolerancePercent: number;
  defaultForecastHorizonDays: number;
  minimumHistoricalDataDays: number;
  minSalesForClassification: number;
  costMethodology: string;
  defaultCurrency: string;
  preferredAiProvider: string;
  preferredAiModel: string;
  dailyAiQueryLimit: number;
  recommendationApprovalMode: string;
  scheduledRefreshFrequency: string;
  allergenRestrictions: string[];
  optimizationObjectives: string[];
  configVersion: number;
  historyNotes: string[];
  canEdit: boolean;
}

export interface SilaCulinaryExecutiveFilter {
  propertyId?: string | null;
  outletName?: string;
  menuCategory?: string;
  mealPeriod?: string;
  eventType?: string;
  salesChannel?: string;
  fromUtc?: string;
  toUtc?: string;
  currency?: string;
  costMethodology?: string;
}

export interface SilaCulinaryMenuEngineeringItem {
  menuItemId: string;
  menuItemCode: string;
  name: string;
  category: string;
  mealPeriod: string;
  outletName: string;
  propertyName: string;
  quantitySold: number;
  grossSellingPrice: number;
  discountAmount: number;
  netSellingPrice: number;
  netSalesRevenue: number;
  standardRecipeCost?: number | null;
  actualIngredientCost?: number | null;
  garnishCost: number;
  packagingCost: number;
  directLaborCost: number;
  allocatedOverhead: number;
  totalProductionCost?: number | null;
  foodCostPercent?: number | null;
  contributionMarginPerItem?: number | null;
  contributionMarginPercent?: number | null;
  totalContributionMargin?: number | null;
  estimatedFullyLoadedProfit?: number | null;
  profitPerPortion?: number | null;
  popularityPercent: number;
  salesMixPercent: number;
  revenueContributionPercent: number;
  profitContributionPercent?: number | null;
  quadrant: "STAR" | "PLOWHORSE" | "PUZZLE" | "DOG" | "INSUFFICIENT_DATA";
  movement: string;
  costSource: string;
  recipeVersion: number;
  costTimestampUtc: string;
  recommendedAction: string;
}

export interface SilaCulinaryMenuEngineeringResponse {
  currency: string;
  popularityThresholdPercent: number;
  contributionMarginThreshold: number;
  weightedAverageContributionMargin: number;
  totalQuantitySold: number;
  totalNetRevenue: number;
  totalContributionMargin: number;
  counts: Record<string, number>;
  items: SilaCulinaryMenuEngineeringItem[];
  warnings: string[];
}

export interface SilaCulinaryConsumptionVarianceRow {
  materialId: string;
  materialCode: string;
  materialName: string;
  uom: string;
  recipeVersion: number;
  theoreticalQuantity: number;
  reconciledActualQuantity: number;
  recordedWastage: number;
  quantityVariance: number;
  unexplainedVariance: number;
  variancePercent?: number | null;
  financialVariance: number;
  yieldVarianceQty: number;
  portionVarianceQty: number;
  possibleCauses: string[];
  exceedsTolerance: boolean;
}

export interface SilaCulinaryConsumptionVarianceResponse {
  currency: string;
  totalTheoreticalQty: number;
  totalActualQty: number;
  totalRecordedWastage: number;
  totalUnexplainedQty: number;
  totalFinancialVariance: number;
  rows: SilaCulinaryConsumptionVarianceRow[];
  note: string;
}

export interface SilaCulinaryPriceImpactNode {
  nodeType: string;
  nodeId: string;
  nodeName: string;
  propertyName: string;
  outletName: string;
  viaSubRecipe?: string | null;
  previousUnitCost: number;
  revisedUnitCost: number;
  unitCostDelta: number;
  previousFoodCostPct?: number | null;
  revisedFoodCostPct?: number | null;
  marginImpactPerUnit: number;
  estimatedMonthlyImpact: number;
  estimatedAnnualImpact: number;
}

export interface SilaCulinaryReversePriceImpactResponse {
  currency: string;
  materialCode: string;
  materialName: string;
  previousPrice: number;
  currentPrice: number;
  priceDelta: number;
  priceChangePercent?: number | null;
  alertTriggered: boolean;
  affectedCounts: Record<string, number>;
  totalMonthlyImpact: number;
  totalAnnualImpact: number;
  impacts: SilaCulinaryPriceImpactNode[];
}

export interface SilaCulinarySimulationScenarioInput {
  scenarioName: string;
  ingredientPriceDeltaPct: number;
  sellingPriceDeltaPct: number;
  portionSizeDeltaPct: number;
  covers: number;
  occupancyPct: number;
  laborCostDeltaPct: number;
  wastagePct: number;
  yieldPct: number;
  discountPct: number;
  overheadAmount: number;
  buffetReplenishmentFactor: number;
  substitutionSavingsPct: number;
}

export interface SilaCulinarySimulationScenarioResult {
  scenarioName: string;
  covers: number;
  netRevenue: number;
  revisedFoodCost: number;
  revisedFoodCostPct?: number | null;
  revisedLaborCost: number;
  totalCost: number;
  contributionMargin: number;
  contributionMarginPct?: number | null;
  breakEvenCovers?: number | null;
  targetSellingPriceForFoodCost?: number | null;
  expectedSavingsVsBaseline: number;
  profitabilityDeltaVsBaseline: number;
}

export interface SilaCulinarySimulationRecord {
  id: string;
  simulationCode: string;
  name: string;
  currency: string;
  isolatedFromProduction: boolean;
  isShared: boolean;
  baselineContribution: number;
  simulatedContribution: number;
  expectedSavings: number;
  createdAtUtc: string;
  results: SilaCulinarySimulationScenarioResult[];
}

export interface SilaCulinaryOptimizationResult {
  jobId: string;
  jobCode: string;
  status: string;
  currency: string;
  objective: string;
  baselineCost: number;
  baselineContribution: number;
  proposedCost: number;
  proposedRevenue: number;
  proposedContribution: number;
  proposedFoodCostPct?: number | null;
  expectedSavings: number;
  expectedMarginImprovementPct: number;
  constraintViolations: string[];
  infeasibilityReasons: string[];
  assumptions: string[];
  explanation: string;
  selections: Array<{
    itemId: string;
    name: string;
    category: string;
    recommendedPortions: number;
    unitCost: number;
    sellingPrice: number;
    totalCost: number;
    totalRevenue: number;
    totalContribution: number;
  }>;
  createdAtUtc: string;
}

export interface SilaCulinarySubstitutionResponse {
  currency: string;
  originalMaterialName: string;
  originalIngredientCost: number;
  recommendations: Array<{
    materialId: string;
    materialCode: string;
    name: string;
    classification: string;
    originalIngredientCost: number;
    substituteIngredientCost: number;
    revisedRecipeCost: number;
    costSaving: number;
    yieldImpactPct: number;
    allergenImpact: string;
    nutritionalImpact: string;
    availabilityStatus: string;
    requiresExecutiveChefApproval: boolean;
    autoApprovalBlocked: boolean;
    blockingReason?: string | null;
  }>;
}

export interface SilaCulinaryBuffetOptimizeResponse {
  currency: string;
  mealPeriod: string;
  expectedCovers: number;
  totalProductionCost: number;
  foodCostPerGuest: number;
  totalPotentialWasteReductionValue: number;
  foodSafetyPolicy: string;
  recommendations: Array<{
    dishId: string;
    name: string;
    initialProductionPortions: number;
    replenishmentBatches: number;
    replenishmentQtyPerBatch: number;
    replenishmentIntervalMinutes: number;
    expectedConsumptionPortions: number;
    expectedLeftoverPortions: number;
    productionCost: number;
    foodCostPerGuest: number;
    potentialWasteReductionPortions: number;
    foodSafetyNote: string;
  }>;
}

export interface SilaCulinaryEventOptimizeResponse {
  currency: string;
  guestCount: number;
  packages: Array<{
    packageCode: string;
    packageName: string;
    menuCompositionSummary: string;
    estimatedFoodCost: number;
    estimatedBeverageCost: number;
    estimatedManpowerCost: number;
    estimatedEventCost: number;
    costPerGuest: number;
    sellingPricePerGuest: number;
    expectedRevenue: number;
    expectedContribution: number;
    expectedContributionMarginPct: number;
    estimatedFullyLoadedProfit: number;
    ingredientRequirementKg: number;
    procurementRequirementValue: number;
  }>;
}

export interface SilaCulinaryForecastResponse {
  modelName: string;
  modelVersion: string;
  horizonDays: number;
  coldStartUsed: boolean;
  mae?: number | null;
  wapePercent?: number | null;
  forecastBias?: number | null;
  dataQuality: string;
  lastTrainedUtc: string;
  forecastPoints: Array<{
    dayIndex: number;
    date: string;
    forecastCovers: number;
    lowerBound80: number;
    upperBound80: number;
    lowerBound95: number;
    upperBound95: number;
    expectedFoodCost: number;
    isForecast: boolean;
  }>;
  warnings: string[];
}

export interface SilaCulinaryWasteIntelligence {
  currency: string;
  totalWasteCost: number;
  wastePercentOfFoodCost?: number | null;
  wastePerCover?: number | null;
  byCategory: Record<string, number>;
  byProperty: Record<string, number>;
  byMealPeriod: Record<string, number>;
  recurringPatterns: string[];
  recommendations: string[];
  recentRecords: Array<{
    id: string;
    wasteNumber: string;
    wasteCategory: string;
    ingredientName: string;
    outletName: string;
    mealPeriod: string;
    quantity: number;
    uom: string;
    estimatedCost: number;
    sourceChannel: string;
    status: string;
    recordedAtUtc: string;
  }>;
}

export interface SilaCulinaryBenchmarkResponse {
  reportingCurrency: string;
  warnings: string[];
  rows: Array<{
    propertyId: string;
    propertyName: string;
    outletType: string;
    localCurrency: string;
    fxRateApplied?: number | null;
    revenueReporting?: number | null;
    foodCostReporting?: number | null;
    foodCostPct?: number | null;
    costPerCoverReporting?: number | null;
    revenuePerCoverReporting?: number | null;
    contributionMarginPct?: number | null;
    wastePct?: number | null;
    wastePerCoverReporting?: number | null;
    eventMarginPct?: number | null;
    comparisonWarning?: string | null;
  }>;
}

export interface SilaCulinaryRecommendation {
  id: string;
  recommendationCode: string;
  category: string;
  sourceEngine: string;
  title: string;
  currentSituation: string;
  proposedAction: string;
  propertyName?: string | null;
  outletName?: string | null;
  expectedFinancialImpact: number;
  realizedFinancialImpact?: number | null;
  priority: string;
  dataQuality: string;
  riskLevel: string;
  responsibleRole: string;
  status: string;
  createdAtUtc: string;
  validToUtc?: string | null;
}

export interface SilaCulinaryCopilotResponse {
  directAnswer: string;
  supportingMetrics: string[];
  dataSource: string;
  dateRange: string;
  assumptions: string[];
  recommendedAction: string;
  estimatedFinancialImpact: string;
  confidence: string;
  supportingRoute: string;
  providerUsed: string;
  promptInjectionBlocked: boolean;
}

export interface SilaCulinaryExecutiveDashboard {
  currency: string;
  canViewFinancials: boolean;
  canViewCrossProperty: boolean;
  totalFbRevenue: number;
  totalFoodCost: number;
  foodCostPercent?: number | null;
  targetFoodCostPercent: number;
  contributionMargin: number;
  costPerCover?: number | null;
  revenuePerCover?: number | null;
  wasteCost: number;
  wastePercent?: number | null;
  eventProfitability: number;
  forecastDemandCovers: number;
  potentialSavingsPipeline: number;
  realizedSavings: number;
  quadrantCounts: Record<string, number>;
  topProfitableItems: SilaCulinaryMenuEngineeringItem[];
  lowMarginItems: SilaCulinaryMenuEngineeringItem[];
  topRecommendations: SilaCulinaryRecommendation[];
  dataAvailabilityNotes: string[];
}

export interface SilaCulinaryAnalyticsReportCatalogItem {
  reportCode: string;
  name: string;
  description: string;
  formats: string[];
}

export interface SilaCulinaryMobileOpsSummary {
  currency: string;
  canViewCosts: boolean;
  dailyExpectedCovers: number;
  activeShortageAlerts: number;
  activeWasteAlerts: number;
  pendingApprovalTasks: number;
  dailyFoodCostPercent?: number | null;
  targetFoodCostPercent: number;
  productionRecommendations: string[];
  buffetReplenishmentNotes: string[];
  eventProductionSummaries: string[];
  chefRecommendations: SilaCulinaryRecommendation[];
}

export async function getCulinaryAnalyticsConfig(propertyId?: string | null): Promise<SilaCulinaryAnalyticsConfig> {
  try {
    const res = await axiosInstance.get<SilaCulinaryAnalyticsConfig>(`${BASE}/config`, { params: propertyId ? { propertyId } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function saveCulinaryAnalyticsConfig(payload: Partial<SilaCulinaryAnalyticsConfig>): Promise<SilaCulinaryAnalyticsConfig> {
  try {
    const res = await axiosInstance.put<SilaCulinaryAnalyticsConfig>(`${BASE}/config`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function getExecutiveCulinaryDashboard(filter?: SilaCulinaryExecutiveFilter): Promise<SilaCulinaryExecutiveDashboard> {
  try {
    const res = await axiosInstance.post<SilaCulinaryExecutiveDashboard>(`${BASE}/dashboard`, filter ?? {});
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function analyzeMenuEngineering(filter?: SilaCulinaryExecutiveFilter): Promise<SilaCulinaryMenuEngineeringResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenuEngineeringResponse>(`${BASE}/menu-engineering`, filter ?? {});
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function analyzeConsumptionVariance(filter?: SilaCulinaryExecutiveFilter): Promise<SilaCulinaryConsumptionVarianceResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryConsumptionVarianceResponse>(`${BASE}/consumption-variance`, filter ?? {});
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function createVarianceInvestigationTask(payload: { materialCode: string; materialName: string; unexplainedVarianceQty: number; financialImpact: number; note: string }) {
  try {
    const res = await axiosInstance.post(`${BASE}/consumption-variance/task`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function analyzeIngredientPriceImpact(payload: { materialCode: string; simulatedNewPrice?: number }): Promise<SilaCulinaryReversePriceImpactResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryReversePriceImpactResponse>(`${BASE}/price-impact`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function listCulinarySimulations(propertyId?: string | null): Promise<SilaCulinarySimulationRecord[]> {
  try {
    const res = await axiosInstance.get<SilaCulinarySimulationRecord[]>(`${BASE}/simulations`, { params: propertyId ? { propertyId } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function runCulinarySimulation(payload: {
  propertyId?: string | null;
  name: string;
  currency: string;
  baseSellingPricePerCover: number;
  baseFoodCostPerCover: number;
  baseLaborCost: number;
  baseFixedExpenses: number;
  targetFoodCostPct: number;
  saveSnapshot: boolean;
  scenarios: SilaCulinarySimulationScenarioInput[];
}): Promise<SilaCulinarySimulationRecord> {
  try {
    const res = await axiosInstance.post<SilaCulinarySimulationRecord>(`${BASE}/simulations`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function duplicateCulinarySimulation(simulationId: string): Promise<SilaCulinarySimulationRecord> {
  try {
    const res = await axiosInstance.post<SilaCulinarySimulationRecord>(`${BASE}/simulations/${simulationId}/duplicate`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function optimizeCulinaryMenu(payload: {
  propertyId?: string | null;
  objective: string;
  totalTargetPortions: number;
  targetFoodCostPct?: number | null;
  maxBudget?: number | null;
  maxKitchenMinutes?: number | null;
  excludedAllergens: string[];
  requiredDietaryTags: string[];
  strictAllergenSafety: boolean;
}): Promise<SilaCulinaryOptimizationResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryOptimizationResult>(`${BASE}/optimize-menu`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function listOptimizationJobs(): Promise<SilaCulinaryOptimizationResult[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryOptimizationResult[]>(`${BASE}/optimization-jobs`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function cancelOptimizationJob(jobId: string): Promise<SilaCulinaryOptimizationResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryOptimizationResult>(`${BASE}/optimization-jobs/${jobId}/cancel`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function analyzeIngredientSubstitutions(payload: {
  originalMaterialCode: string;
  requiredQty: number;
  excludedAllergens: string[];
  requiredDietaryTags: string[];
}): Promise<SilaCulinarySubstitutionResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinarySubstitutionResponse>(`${BASE}/substitutions`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function optimizeBuffetProduction(payload: {
  propertyId?: string | null;
  mealPeriod: string;
  expectedCovers: number;
  occupancyPercent: number;
}): Promise<SilaCulinaryBuffetOptimizeResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryBuffetOptimizeResponse>(`${BASE}/buffet-optimize`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function optimizeEventMenus(payload: {
  eventFinancialPlanId?: string | null;
  guestCount?: number | null;
  eventBudget?: number | null;
  targetRevenue?: number | null;
}): Promise<SilaCulinaryEventOptimizeResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventOptimizeResponse>(`${BASE}/event-optimize`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function generateDemandForecast(payload: {
  propertyId?: string | null;
  horizonDays: number;
  occupancyAssumptionPct: number;
}): Promise<SilaCulinaryForecastResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryForecastResponse>(`${BASE}/forecast`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function getWasteIntelligence(propertyId?: string | null): Promise<SilaCulinaryWasteIntelligence> {
  try {
    const res = await axiosInstance.get<SilaCulinaryWasteIntelligence>(`${BASE}/waste`, { params: propertyId ? { propertyId } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function recordCulinaryWaste(payload: {
  propertyId?: string | null;
  locationId?: string | null;
  outletName?: string;
  mealPeriod: string;
  wasteCategory: string;
  ingredientName: string;
  quantity: number;
  uom: string;
  estimatedCost: number;
  coversServed: number;
  sourceChannel: string;
  idempotencyKey?: string;
  notes?: string;
}) {
  try {
    const res = await axiosInstance.post(`${BASE}/waste`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function benchmarkProperties(reportingCurrency?: string): Promise<SilaCulinaryBenchmarkResponse> {
  try {
    const res = await axiosInstance.get<SilaCulinaryBenchmarkResponse>(`${BASE}/benchmark`, { params: reportingCurrency ? { reportingCurrency } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function listCulinaryRecommendations(status?: string): Promise<SilaCulinaryRecommendation[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryRecommendation[]>(`${BASE}/recommendations`, { params: status ? { status } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function generateCulinaryRecommendations(propertyId?: string | null): Promise<SilaCulinaryRecommendation[]> {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecommendation[]>(`${BASE}/recommendations/generate`, null, { params: propertyId ? { propertyId } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function decideCulinaryRecommendation(recommendationId: string, payload: { status: string; comment?: string; realizedFinancialImpact?: number }): Promise<SilaCulinaryRecommendation> {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecommendation>(`${BASE}/recommendations/${recommendationId}/decide`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function queryCulinaryCopilot(payload: { question: string; contextModule: string; propertyId?: string | null }): Promise<SilaCulinaryCopilotResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryCopilotResponse>(`${BASE}/copilot`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function getCulinaryAnalyticsReportCatalog(): Promise<SilaCulinaryAnalyticsReportCatalogItem[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryAnalyticsReportCatalogItem[]>(`${BASE}/reports/catalog`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function exportCulinaryAnalyticsReport(reportCode: string, format: "CSV" | "EXCEL" | "PDF", filter?: SilaCulinaryExecutiveFilter) {
  try {
    const res = await axiosInstance.post<{ reportCode: string; format: string; fileName: string; contentType: string; content: string }>(
      `${BASE}/reports/${reportCode}/export`,
      filter ?? {},
      { params: { format } },
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}

export async function getMobileCulinaryOpsSummary(propertyId?: string | null): Promise<SilaCulinaryMobileOpsSummary> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMobileOpsSummary>(`${BASE}/mobile-summary`, { params: propertyId ? { propertyId } : undefined });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Culinary analytics request failed."));
  }
}
