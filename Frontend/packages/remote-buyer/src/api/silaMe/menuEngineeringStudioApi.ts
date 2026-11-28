import axiosInstance from "../axiosInstance";
import { readError } from "../readError";
import type { SilaPurchaseRecommendationList } from "./silaInventoryControlApi";
import type { MenuDemand, MenuFilter, MenuInsight, MenuLookups, MenuRequirementRow } from "./silaMenuEngineeringApi";

const BASE = "/api/v1/buyer/sila/me-studio";

export interface MenuEngineeringFeatureFlagsDto {
  menuEngineering: boolean;
  meStudio: boolean;
  aiMenuIntelligence: boolean;
  predictiveForecasting: boolean;
  mathematicalOptimization: boolean;
  culinaryDigitalTwin: boolean;
  autonomousAgents: boolean;
  silaVoice: boolean;
  posIntegration: boolean;
  pmsIntegration: boolean;
  enterpriseNotifications: boolean;
  mobileCulinaryOperations: boolean;
}

export interface MeStudioPermissionsDto {
  menuEngineeringEnabled: boolean;
  meStudioEnabled: boolean;
  featureFlags?: MenuEngineeringFeatureFlagsDto;
  viewMeStudio: boolean;
  createMenu: boolean;
  editDraftMenu: boolean;
  manageMenuItems: boolean;
  managePortionPlans: boolean;
  manageDemandPlans: boolean;
  viewPosSales: boolean;
  managePosMapping: boolean;
  viewMenuPerformance: boolean;
  viewMenuCost: boolean;
  viewMenuMargin: boolean;
  generatePurchaseRecommendation: boolean;
  useAiMenuIntelligence: boolean;
  runMenuSimulation: boolean;
  approveMenu: boolean;
  publishMenu: boolean;
  manageMenuSetup: boolean;
}

export interface MenuItemEnhanced {
  id?: string;
  menuItemCode: string;
  name: string;
  alternativeName?: string | null;
  description?: string | null;
  category?: string | null;
  subCategory?: string | null;
  cuisine?: string | null;
  classification?: string | null;
  mealPeriods?: string | null;
  serviceFormat?: string | null;
  sellingPrice?: number | null;
  currency?: string | null;
  taxRatePercent?: number | null;
  taxInclusive: boolean;
  netSellingPrice?: number | null;
  taxInclusiveSellingPrice?: number | null;
  linkedRecipeId?: string | null;
  recipeName?: string | null;
  approvedRecipeVersion?: number | null;
  recipeCombinationJson?: string | null;
  defaultPortionQty: number;
  portionUom: string;
  defaultParticipationRate: number;
  defaultBufferPercentage: number;
  prepTimeMinutes?: number | null;
  posItemReference?: string | null;
  dietaryTags?: string | null;
  allergenNotes?: string | null;
  imagePath?: string | null;
  propertyId?: string | null;
  outletLocationId?: string | null;
  isAvailable: boolean;
  seasonalAvailability?: string | null;
  effectiveFrom?: string | null;
  effectiveTo?: string | null;
  approvalStatus: string;
  versionNumber: number;
  status: string;
  costPerPortion?: number | null;
  foodCostPercent?: number | null;
  contributionMargin?: number | null;
  historicalSalesQty: number;
  popularityIndex?: number | null;
  matrixClassification?: string | null;
  ingredientAvailabilityStatus: string;
}

export interface MenuPlanLineEnhanced {
  id?: string;
  menuItemId: string;
  menuItemName: string;
  menuItemCode: string;
  recipeLinkStatus: string;
  recipeName?: string | null;
  imagePath?: string | null;
  allergenNotes?: string | null;
  categoryName?: string | null;
  sequenceOrder: number;
  recipeId?: string | null;
  recipeVersion?: number | null;
  portionSize?: number | null;
  portionUom?: string | null;
  expectedServingsPerGuest?: number | null;
  sellingPrice?: number | null;
  expectedCovers?: number | null;
  participationRate?: number | null;
  bufferPercentage?: number | null;
  sampledActualPortion?: number | null;
  lineNotes?: string | null;
  alternativesJson?: string | null;
  dietaryTags?: string | null;
  recommendedPortions: number;
  requiredUsableOutput: number;
  portionsConfirmed: boolean;
  plannedPortions?: number | null;
  actualPortions?: number | null;
  variance?: number | null;
  portionCost?: number | null;
  totalLineCost?: number | null;
  expectedLineRevenue?: number | null;
  foodCostPercent?: number | null;
  contributionMargin?: number | null;
  availabilityStatus: string;
  warning?: string | null;
}

export interface MenuPlanEnhanced {
  id: string;
  planCode: string;
  name: string;
  propertyId?: string | null;
  propertyName?: string | null;
  outletLocationId?: string | null;
  outletName?: string | null;
  businessDate: string;
  startDate?: string | null;
  endDate?: string | null;
  mealPeriod?: string | null;
  eventName?: string | null;
  menuType?: string | null;
  serviceFormat?: string | null;
  venueName?: string | null;
  rotationRule?: string | null;
  isTemplate: boolean;
  templateCategory?: string | null;
  expectedGuests?: number | null;
  expectedCovers?: number | null;
  mealParticipation?: number | null;
  targetFoodCostPercent?: number | null;
  targetContributionMargin?: number | null;
  dietaryRequirements?: string | null;
  status: string;
  versionNumber: number;
  parentPublishedPlanId?: string | null;
  sourceType?: string | null;
  sourceReferenceId?: string | null;
  notes?: string | null;
  approvalComment?: string | null;
  submittedAtUtc?: string | null;
  approvedAtUtc?: string | null;
  publishedAtUtc?: string | null;
  preventConflicts: boolean;
  categoriesOrder: string[];
  lines: MenuPlanLineEnhanced[];
}

export interface AiMenuRecommendationCard {
  strategy: string;
  recommendation: string;
  supportingEvidence: string;
  currentBaseline: string;
  proposedChange: string;
  estimatedCostImpact?: number | null;
  estimatedMarginImpact?: number | null;
  operationalImpact: string;
  dataQuality: string;
  confidence: number;
  requiresApproval: boolean;
  targetMenuItemId?: string | null;
  suggestedRecipeId?: string | null;
  allergenImpact?: string | null;
}

export interface AiMenuStrategyOption {
  strategyKey: string;
  title: string;
  summary: string;
  recommendedMenuItemIds: string[];
  projectedFoodCostPercent?: number | null;
  projectedMargin?: number | null;
  confidence: number;
}

export interface AiMenuIntelligenceDto {
  providerMode: string;
  aiProviderAvailable: boolean;
  fallbackNotice?: string | null;
  recommendations: AiMenuRecommendationCard[];
  alternativeMenus: AiMenuStrategyOption[];
}

export interface MeStudioLiveIntelligence {
  totalMenuCost?: number | null;
  costPerGuest?: number | null;
  expectedRevenue: number;
  expectedFoodCostPercent?: number | null;
  expectedContributionMargin?: number | null;
  ingredientReadinessPercent: number;
  availableIngredients: number;
  shortIngredients: number;
  totalPurchaseRequirementValue: number;
  expectedWasteQty: number;
  historicalAvgMarginPercent?: number | null;
  riskIndicators: string[];
  aiRecommendations: AiMenuRecommendationCard[];
}

export interface MenuPerformanceMatrixRow {
  menuItemId: string;
  name: string;
  category?: string | null;
  cuisine?: string | null;
  mealPeriod?: string | null;
  qtySold: number;
  revenue: number;
  sellingPrice?: number | null;
  averageNetSellingPrice?: number | null;
  costPerPortion?: number | null;
  totalIngredientCost?: number | null;
  foodCostPercent?: number | null;
  contributionMargin?: number | null;
  totalContributionMargin?: number | null;
  marginPercent?: number | null;
  mixPercent: number;
  popularityPercent: number;
  expectedPopularityPercent?: number | null;
  categoryPopularityThresholdPercent?: number | null;
  categoryWeightedAvgMargin?: number | null;
  wasteImpactCost?: number | null;
  classification: string;
  exclusionReason?: string | null;
  costBasis: string;
  costSourceType: string;
  costState: string;
  recommendedAction?: string | null;
}

export interface MenuMatrixCategorySummary {
  category: string;
  eligibleItems: number;
  excludedItems: number;
  totalSalesQty: number;
  totalRevenue: number;
  expectedPopularityPercent: number;
  popularityThresholdPercent: number;
  weightedAvgContributionMargin?: number | null;
}

export interface MenuPerformanceMatrixDto {
  costBasis: string;
  popularityThresholdFactor: number;
  rows: MenuPerformanceMatrixRow[];
  excludedRows: MenuPerformanceMatrixRow[];
  categorySummaries: MenuMatrixCategorySummary[];
  stars: number;
  plowhorses: number;
  puzzles: number;
  dogs: number;
  excludedCount: number;
}

export interface HistoricalMenuSummary {
  sourceId: string;
  sourceKind: string;
  name: string;
  eventType?: string | null;
  businessDate: string;
  versionNumber: number;
  guestCount: number;
  actualSales: number;
  historicalFoodCostPercent?: number | null;
  historicalMargin?: number | null;
  actualWasteQty?: number | null;
  recalculatedCurrentCost?: number | null;
  recalculatedCurrentFoodCostPercent?: number | null;
  menuItemIds: string[];
}

export interface MenuEngineeringScenario {
  id: string;
  menuPlanId?: string | null;
  scenarioName: string;
  scenarioType: string;
  itemCount: number;
  expectedCovers: number;
  sellingPricePerGuest: number;
  ingredientCostPerGuest: number;
  totalFoodCost: number;
  foodCostPercent?: number | null;
  expectedRevenue: number;
  contributionMargin: number;
  ingredientAvailabilityPercent: number;
  purchaseRequirement: number;
  estimatedWaste: number;
  productionCapacityImpact: string;
  dietaryCoverage: string;
  isPreferred: boolean;
  notes?: string | null;
  createdAt: string;
}

export interface MenuEngineeringApprovalHistory {
  id: string;
  entityType: string;
  entityId: string;
  versionNumber: number;
  fromStatus: string;
  toStatus: string;
  actionByUserId?: string | null;
  approverRole?: string | null;
  reason?: string | null;
  actionAtUtc: string;
}

export interface MenuEngineeringSetupDto {
  propertyId?: string | null;
  menuEngineeringEnabled: boolean;
  meStudioEnabled: boolean;
  featureFlags?: MenuEngineeringFeatureFlagsDto;
  defaultMealParticipation: number;
  defaultItemParticipation: number;
  defaultBufferPercentage: number;
  highFoodCostPercent: number;
  popularityThreshold: number;
  mealPeriods: string[];
  categories: string[];
  serviceFormats: string[];
  cuisines: string[];
  dietaryTags: string[];
  costingMethod: string;
  targetFoodCostPercent: number;
  targetContributionMargin?: number | null;
  costRefreshFrequency: string;
  defaultCurrency: string;
  taxTreatment: string;
  defaultPortionUom: string;
  portionVarianceThresholdPercent: number;
  forecastHorizonDays: number;
  forecastMethod: string;
  historicalLookbackDays: number;
  forecastAccuracyThresholdPercent: number;
  profitabilityBasis: string;
  profitabilityThresholdPercent?: number | null;
  minimumSalesHistoryDays: number;
  minimumSalesQuantity: number;
  comparableGrouping: string;
  posSyncScheduleCron: string;
  posRetryMaxAttempts: number;
  aiEnabledAgents: string[];
  aiConfidenceThreshold: number;
  aiUsageLimitPerDay: number;
  approvalFoodCostThresholdPercent: number;
  approvalPriceChangePercent: number;
  requireApprovalOnSubstitution: boolean;
  requireApprovalOnPublish: boolean;
  approvalRoles: string[];
  autonomousModePaused?: boolean;
  automatedExecutionDisabled?: boolean;
  emergencyPauseReason?: string | null;
  maxConcurrentJobs?: number;
  dailyAiTokenBudget?: number;
  monthlyAiBudgetUsd?: number;
  teamsNotificationsEnabled?: boolean;
  teamsWebhookUrl?: string | null;
  eventScheduleConfigJson?: string | null;
  configVersion: number;
  changeSummary?: string | null;
  history: { id: string; configVersion: number; changeSummary?: string | null; changedAtUtc: string }[];
}

export interface MeStudioWorkspaceDto {
  permissions: MeStudioPermissionsDto;
  lookups: MenuLookups;
  libraryItems: MenuItemEnhanced[];
  menus: MenuPlanEnhanced[];
  activeMenu?: MenuPlanEnhanced | null;
  liveIntelligence: MeStudioLiveIntelligence;
  ingredientRequirements: MenuRequirementRow[];
  performanceMatrix?: MenuPerformanceMatrixDto | null;
  historicalMenus: HistoricalMenuSummary[];
  scenarios: MenuEngineeringScenario[];
  approvalHistory: MenuEngineeringApprovalHistory[];
  setup: MenuEngineeringSetupDto;
}

export interface PortionPlanRow {
  menuPlanLineId: string;
  menuPlanId: string;
  planName: string;
  menuItemName: string;
  serviceFormat?: string | null;
  mealPeriod?: string | null;
  expectedCovers: number;
  participationRate: number;
  expectedServingsPerGuest: number;
  standardPortion: number;
  plannedPortion: number;
  sampledActualPortion?: number | null;
  portionUom: string;
  expectedPortions: number;
  requiredUsableOutput: number;
  productionBufferPercent: number;
  recipeYieldPercent: number;
  grossIngredientNeedFactor: number;
  historicalPortionVariancePercent?: number | null;
  varianceStatus: string;
  portionCost?: number | null;
}

export interface DemandForecastBoard {
  latestPms?: MenuDemand | null;
  rows: {
    businessDate: string;
    mealPeriod: string;
    occupancyPercent?: number | null;
    historicalActualDemand: number;
    baselineForecast: number;
    aiAdjustedForecast: number;
    confirmedReservations: number;
    confirmedEvents: number;
    confirmedDemandDeduped: number;
    recommendedDemand: number;
    lowerRange: number;
    upperRange: number;
    forecastAccuracyPercent?: number | null;
    forecastModelVersion: string;
    coldStartLimited: boolean;
    coldStartNote?: string | null;
    manualOverrideQty?: number | null;
    overrideReason?: string | null;
  }[];
  overrides: { id: string; businessDate: string; mealPeriod?: string | null; overrideQty: number; reason: string; overriddenAtUtc: string }[];
  coldStartLimited: boolean;
  coldStartMessage?: string | null;
}

export interface PosSalesExtendedDto {
  qtySold: number;
  grossRevenue: number;
  discountTotal: number;
  taxTotal: number;
  netRevenue: number;
  transactionCount: number;
  unmappedCount: number;
  voidOrRefundCount: number;
  lastSync?: string | null;
  syncStatus: string;
  rows: {
    id: string;
    businessDate: string;
    sourceTransactionId: string;
    posLineId?: string | null;
    posCode: string;
    outletName?: string | null;
    menuItemId?: string | null;
    menuItemName?: string | null;
    quantity: number;
    uom?: string | null;
    grossSellingPrice?: number | null;
    discountAmount?: number | null;
    taxAmount?: number | null;
    netRevenue?: number | null;
    currency: string;
    status: string;
    isVoid: boolean;
    isRefund: boolean;
    isComplimentary: boolean;
    modifierCode?: string | null;
    comboReference?: string | null;
    mealPeriod?: string | null;
  }[];
}

export interface PosMappingExtendedDto {
  id: string;
  posCode: string;
  posDescription?: string | null;
  outletLocationId?: string | null;
  outletName?: string | null;
  menuItemId?: string | null;
  menuItemName?: string | null;
  suggestedMenuItemId?: string | null;
  suggestedMenuItemName?: string | null;
  suggestionConfidence?: number | null;
  status: string;
  mappingSource: string;
  recipeId?: string | null;
  recipeName?: string | null;
  approvedRecipeVersion?: number | null;
  mappingType: string;
  portionMultiplier: number;
  uomCode?: string | null;
  uomConversionFactor: number;
  modifierCode?: string | null;
  modifierRecipeId?: string | null;
  modifierPortionMultiplier?: number | null;
  comboParentCode?: string | null;
  isComboComponent: boolean;
  excludeComponentDoubleCount: boolean;
  mappingNotes?: string | null;
  validationWarning?: string | null;
}

export interface PosMappingWorkbenchDto {
  mappings: PosMappingExtendedDto[];
  unmappedQueue: PosMappingExtendedDto[];
  mappedCount: number;
  unmappedCount: number;
  comboCount: number;
  reprocessWarnings: string[];
}

export interface ConsumptionVarianceDetailDto {
  theoretical: number;
  actual: number;
  expectedYieldLoss: number;
  recordedWaste: number;
  totalVariance: number;
  unexplainedVariance: number;
  varianceValue?: number | null;
  doubleCountingPrevented: boolean;
  rows: {
    materialId: string;
    materialName: string;
    recipeName?: string | null;
    uom: string;
    theoreticalQty: number;
    actualQty: number;
    expectedYieldLossQty: number;
    recordedWasteQty: number;
    varianceQty: number;
    unexplainedVarianceQty: number;
    variancePercent?: number | null;
    varianceValue?: number | null;
    sourceNotes?: string | null;
  }[];
}

export interface CostMarginAnalysisDto {
  itemRows: MenuPerformanceMatrixRow[];
  marginAlerts: MenuInsight[];
  supplierComparisons: { materialId: string; materialName: string; supplierName: string; unitPrice?: number | null; currency: string; isPreferred: boolean }[];
  totalRevenue: number;
  totalFoodCost?: number | null;
  overallFoodCostPercent?: number | null;
  totalContributionMargin?: number | null;
  disclaimer?: string;
}

export interface MenuPurchasePlanDto {
  rows: (MenuRequirementRow & {
    reservedStock: number;
    blockedStock: number;
    expiringStock: number;
    eligibleAvailable: number;
    eligibleIncomingOnTime: number;
    lateOpenPoExcluded: number;
    recommendedPurchaseQty: number;
    contractPrice?: number | null;
    estimatedPurchaseCost?: number | null;
    recommendedSupplier?: string | null;
    substituteSuggestion?: string | null;
    stockTransferSuggestion?: string | null;
    timePhasedValid: boolean;
  })[];
  existingRecommendations: SilaPurchaseRecommendationList;
  totalShortageQty: number;
  totalEstimatedPurchaseCost: number;
  autoPoTransmissionDisabled: boolean;
}

export interface WhatIfSimulationResult {
  scenarioId?: string | null;
  scenarioName: string;
  recommendedPortions: number;
  plannedPortions: number;
  ingredientRequirement: number;
  shortage: number;
  purchaseRequirement: number;
  foodCost: number;
  revenue: number;
  contributionMargin: number;
  marginPercent?: number | null;
  baselineCost: number;
  baselineRevenue: number;
  baselineMargin: number;
  baselineRequirement: number;
  baselineShortage: number;
  costVarianceAbs: number;
  costVariancePct: number;
  revenueVarianceAbs: number;
  revenueVariancePct: number;
  marginVarianceAbs: number;
  marginVariancePct: number;
  expectedWasteQty: number;
  fixedDemandAssumed: boolean;
  demandAssumptionDisclosure: string;
}

export interface ScenarioCompareResultDto {
  scenarios: MenuEngineeringScenario[];
  lowestFoodCostScenarioId?: string | null;
  highestMarginScenarioId?: string | null;
  highestReadinessScenarioId?: string | null;
  governanceNote: string;
}

export interface MenuEngineeringReportItem {
  reportKey: string;
  title: string;
  category: string;
  supportedFormats: string[];
}

export interface MenuEngineeringReportExport {
  reportKey: string;
  title: string;
  format: string;
  fileName: string;
  mimeType: string;
  columns: string[];
  rows: Record<string, unknown>[];
  textContent?: string | null;
}

async function get<T>(path: string, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await axiosInstance.get<T>(`${BASE}${path}`, { params });
    return res.data;
  } catch (e) {
    throw new Error(readError(e, "Request failed."));
  }
}

async function post<T>(path: string, body?: unknown, params?: Record<string, unknown>): Promise<T> {
  try {
    const res = await axiosInstance.post<T>(`${BASE}${path}`, body, { params });
    return res.data;
  } catch (e) {
    throw new Error(readError(e, "Request failed."));
  }
}

export const getMePermissions = (propertyId?: string) => get<MeStudioPermissionsDto>("/permissions", { propertyId });
export const getMeWorkspace = (menuPlanId?: string, propertyId?: string) => get<MeStudioWorkspaceDto>("/workspace", { menuPlanId, propertyId });
export const getMeMenuItems = (filter: MenuFilter = {}) => get<MenuItemEnhanced[]>("/menu-items", filter as Record<string, unknown>);
export const saveMeMenuItem = (dto: Partial<MenuItemEnhanced>) => post<MenuItemEnhanced>("/menu-items", dto);
export const copyMeMenuItem = (id: string) => post<MenuItemEnhanced>(`/menu-items/${id}/copy`);
export const bulkPriceMeMenuItems = (dto: { menuItemIds: string[]; percentageChange?: number; fixedAmountChange?: number; targetFoodCostPercent?: number; reason?: string }) =>
  post<number>("/menu-items/bulk-price", dto);
export const bulkImportMeMenuItems = (payload: string, format = "CSV") => post<number>("/menu-items/bulk-import", { format, payload });
export const getMeMenuItemVersions = (id: string) => get<{ id: string; versionNumber: number; status: string; sellingPrice?: number; ingredientCost?: number; changeSummary?: string; actionAtUtc: string }[]>(`/menu-items/${id}/versions`);
export const getMeMenuPlans = (filter: MenuFilter = {}) => get<MenuPlanEnhanced[]>("/menu-plans", filter as Record<string, unknown>);
export const saveMeMenuPlan = (dto: Record<string, unknown>) => post<MenuPlanEnhanced>("/menu-plans", dto);
export const createMePlanFromHistorical = (sourceId: string, sourceKind = "MENU_PLAN", propertyId?: string) =>
  post<MenuPlanEnhanced>(`/menu-plans/from-historical/${sourceId}`, undefined, { sourceKind, propertyId });
export const createMePlanFromAi = (strategy = "BALANCED", expectedCovers = 100, propertyId?: string) =>
  post<MenuPlanEnhanced>("/menu-plans/from-ai", undefined, { strategy, expectedCovers, propertyId });
export const transitionMeMenuPlan = (menuPlanId: string, targetStatus: string, comment?: string, triggerProductionHandoff = false) =>
  post<MenuPlanEnhanced>(`/menu-plans/${menuPlanId}/transition`, { menuPlanId, targetStatus, comment, triggerProductionHandoff });
export const getMePortions = (menuPlanId?: string, propertyId?: string) => get<PortionPlanRow[]>("/portions", { menuPlanId, propertyId });
export const recordMePortionSample = (menuPlanLineId: string, sampledActualPortion: number, note?: string) =>
  post<PortionPlanRow>("/portions/sample", { menuPlanLineId, sampledActualPortion, note });
export const getMeDemand = (filter: MenuFilter = {}) => get<DemandForecastBoard>("/demand", filter as Record<string, unknown>);
export const saveMeDemandOverride = (dto: { propertyId?: string; outletLocationId?: string; menuItemId?: string; businessDate: string; mealPeriod?: string; overrideQty: number; reason: string }) =>
  post("/demand/override", dto);
export const getMePosSales = (filter: MenuFilter = {}) => get<PosSalesExtendedDto>("/pos-sales", filter as Record<string, unknown>);
export const getMePosMapping = (filter: MenuFilter = {}) => get<PosMappingWorkbenchDto>("/pos-mapping", filter as Record<string, unknown>);
export const saveMePosMapping = (dto: Record<string, unknown>) => post<PosMappingExtendedDto>("/pos-mapping", dto);
export const getMePerformanceMatrix = (filter: MenuFilter = {}, costBasis?: string, popularityThreshold?: number) =>
  get<MenuPerformanceMatrixDto>("/performance-matrix", { ...filter, costBasis, popularityThreshold });
export const getMeConsumption = (filter: MenuFilter = {}) => get<ConsumptionVarianceDetailDto>("/consumption", filter as Record<string, unknown>);
export const getMeCostMargin = (filter: MenuFilter = {}) => get<CostMarginAnalysisDto>("/cost-margin", filter as Record<string, unknown>);
export const getMePurchasePlan = (filter: MenuFilter = {}) => get<MenuPurchasePlanDto>("/purchase-plan", filter as Record<string, unknown>);
export const createMeDraftPurchaseRec = (filter: MenuFilter = {}) => post<SilaPurchaseRecommendationList>("/purchase-plan/draft", filter);
export const getMeAiIntelligence = (menuPlanId?: string, propertyId?: string) => get<AiMenuIntelligenceDto>("/ai-intelligence", { menuPlanId, propertyId });
export const getMeHistoricalMenus = (propertyId?: string) => get<HistoricalMenuSummary[]>("/historical-menus", { propertyId });
export const runMeSimulation = (dto: Record<string, unknown>) => post<WhatIfSimulationResult>("/simulate", dto);
export const getMeScenarios = (menuPlanId?: string) => get<MenuEngineeringScenario[]>("/scenarios", { menuPlanId });
export const compareMeScenarios = (ids: string[]) => post<ScenarioCompareResultDto>("/scenarios/compare", ids);
export const setMePreferredScenario = (id: string) => post<MenuEngineeringScenario>(`/scenarios/${id}/preferred`);
export const convertMeScenarioToDraft = (id: string) => post<MenuPlanEnhanced>(`/scenarios/${id}/convert-draft`);
export const getMeSetup = (propertyId?: string) => get<MenuEngineeringSetupDto>("/setup", { propertyId });
export const saveMeSetup = (dto: MenuEngineeringSetupDto) => post<MenuEngineeringSetupDto>("/setup", dto);
export interface SilaStoreMenuMobileDto {
  businessDate: string;
  permissions: MeStudioPermissionsDto;
  todayMenus: MenuPlanEnhanced[];
  upcomingMenus: MenuPlanEnhanced[];
  portionInstructions: PortionPlanRow[];
  ingredientReadiness: MenuRequirementRow[];
  pendingApprovals: MenuPlanEnhanced[];
}

export interface CustomerDataTemplateDto {
  entityType: string;
  title: string;
  fileName: string;
  columns: string[];
  sampleCsv: string;
}

export interface CustomerDataImportPreviewResultDto {
  entityType: string;
  totalRows: number;
  validRows: number;
  warningRows: number;
  errorRows: number;
  duplicateRows: number;
  protectedApprovedRows: number;
  readyToCommit: boolean;
  rows: { rowNumber: number; key: string; status: string; message: string; parsedFields: Record<string, string | null> }[];
}

export interface CustomerDataImportCommitResultDto {
  auditId: string;
  entityType: string;
  createdCount: number;
  updatedCount: number;
  skippedCount: number;
  errorCount: number;
  messages: string[];
  importedAtUtc: string;
}

export interface CustomerDataMigrationAuditDto {
  id: string;
  entityType: string;
  createdCount: number;
  updatedCount: number;
  skippedCount: number;
  errorCount: number;
  importedAtUtc: string;
  summary: string;
}

export const getMeMigrationTemplates = () => get<CustomerDataTemplateDto[]>("/data-migration/templates");
export const previewMeDataMigration = (dto: { entityType: string; format?: string; payload: string; propertyId?: string; allowOverwriteApproved?: boolean }) =>
  post<CustomerDataImportPreviewResultDto>("/data-migration/preview", dto);
export const commitMeDataMigration = (dto: { entityType: string; format?: string; payload: string; propertyId?: string; allowOverwriteApproved?: boolean }) =>
  post<CustomerDataImportCommitResultDto>("/data-migration/commit", dto);
export const getMeDataMigrationAudits = () => get<CustomerDataMigrationAuditDto[]>("/data-migration/audits");

export const getMeReportCatalog = () => get<MenuEngineeringReportItem[]>("/reports");
export const exportMeReport = (reportKey: string, format = "CSV", filter: MenuFilter = {}) =>
  get<MenuEngineeringReportExport>(`/reports/${reportKey}/export`, { format, ...filter });
export const getMeMobileSummary = (propertyId?: string) =>
  get<SilaStoreMenuMobileDto>("/mobile-summary", { propertyId });
