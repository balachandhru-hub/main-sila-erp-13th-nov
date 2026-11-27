import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const FIN_BASE = "/api/v1/buyer/sila/culinary-financial";

export interface SilaCulinaryFxRate {
  sourceCurrency: string;
  targetCurrency: string;
  rate: number;
  rateType: string;
  source: string;
}

export interface SilaCulinaryOverheadRule {
  ruleCode: string;
  name: string;
  method: string;
  rateOrAmount: number;
  sourceCostPoolId?: string | null;
}

export interface SilaCulinaryFinancialConfig {
  id: string;
  reportingCurrency: string;
  propertyCurrency: string;
  financialPrecision: number;
  quantityPrecision: number;
  roundingPolicy: string;
  costSourcePriority: string[];
  costValuationPolicy: string;
  targetFoodCostPercent: number;
  targetBeverageCostPercent: number;
  targetEventContributionMarginPercent: number;
  budgetApprovalThresholdPercent: number;
  allowManualCostOverrides: boolean;
  excludeVatFromRevenue: boolean;
  excludeRecoverableVatFromCost: boolean;
  exchangeRates: SilaCulinaryFxRate[];
  overheadRules: SilaCulinaryOverheadRule[];
}

export interface SilaCulinaryFinancialRecipeLine {
  lineRef: string;
  materialCode: string;
  description: string;
  costClass: string;
  recipeQty: number;
  recipeUom: string;
  rawQty: number;
  baseUom: string;
  yieldPercent: number;
  unitPrice?: number | null;
  priceUnit: number;
  priceSource: string;
  sourceCurrency?: string | null;
  exchangeRate?: number | null;
  convertedUnitCost?: number | null;
  ingredientCost?: number | null;
  percentOfIngredientCost: number;
  isSubRecipe: boolean;
  subRecipeId?: string | null;
  costVerificationStatus: string;
  warning?: string | null;
}

export interface SilaCulinaryFinancialRecipeCost {
  recipeId: string;
  recipeCode: string;
  recipeName: string;
  recipeVersion: number;
  category: string;
  imageUrl?: string | null;
  reportingCurrency: string;
  standardBatchServings: number;
  plannedServings: number;
  plannedBatches: number;
  plannedOutputServings: number;
  excessServings: number;
  totalIngredientCost: number;
  foodIngredientCost: number;
  beverageIngredientCost: number;
  subRecipeCost: number;
  directProductionCost: number;
  allocatedOverheadCost: number;
  fullyAllocatedBatchCost: number;
  expectedEventConsumptionCost: number;
  remainingUsableOutputValue: number;
  ingredientCostPerServing: number;
  directCostPerServing: number;
  fullyAllocatedCostPerServing: number;
  netSellingPricePerServing?: number | null;
  foodCostPercent?: number | null;
  ingredientContributionPerServing?: number | null;
  ingredientContributionPercent?: number | null;
  directContributionPerServing?: number | null;
  fullyAllocatedProfitPerServing?: number | null;
  requiredSellingPriceTargetFoodCost: number;
  requiredSellingPriceTargetMarginDirect: number;
  requiredSellingPriceTargetMarginFull: number;
  costCompleteness: string;
  missingPriceCount: number;
  missingFxCount: number;
  uomConflictCount: number;
  circularDependencyDetected: boolean;
  lines: SilaCulinaryFinancialRecipeLine[];
}

export interface SilaCulinaryMenuCost {
  menuId: string;
  menuName: string;
  serviceStyle: string;
  plannedGuests: number;
  reportingCurrency: string;
  totalFoodCost: number;
  totalBeverageCost: number;
  totalMenuCost: number;
  foodCostPerGuest: number;
  beverageCostPerGuest: number;
  expectedCostPerGuest: number;
  totalPackageRevenue?: number | null;
  expectedContribution?: number | null;
  foodCostPercent?: number | null;
  foodCostBasisLabel: string;
  beverageCostPercent?: number | null;
  beverageCostBasisLabel: string;
  lines: Array<{
    lineRef: string;
    name: string;
    course: string;
    costClass: string;
    expectedPortions: number;
    costPerServing: number;
    expectedLineCost: number;
    costPerGuest: number;
  }>;
}

export interface SilaCulinaryMaterialPriceImpact {
  materialCode: string;
  materialDescription: string;
  reportingCurrency: string;
  currentUnitPrice: number;
  newUnitPrice: number;
  totalCurrentCost: number;
  totalProjectedCost: number;
  totalCostDelta: number;
  affectedItems: Array<{
    recipeOrEventRef: string;
    name: string;
    kind: string;
    requiredQty: number;
    uom: string;
    currentCost: number;
    projectedCost: number;
    costDelta: number;
  }>;
}

export interface SilaCulinaryEventRevenueLine {
  id: string;
  category: string;
  description: string;
  revenueStatus: string;
  billingBasis: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  exchangeRate: number;
  grossAmount: number;
  discountAmount: number;
  taxAmount: number;
  netRevenue: number;
  sourceReference?: string | null;
}

export interface SilaCulinaryEventExpenseLine {
  id: string;
  expenseCode: string;
  category: string;
  description: string;
  costBehavior: string;
  stepThresholdGuests?: number | null;
  stepIncrementCost?: number | null;
  supplierOrEmployeeRef?: string | null;
  employeeRole?: string | null;
  staffCount?: number | null;
  plannedHours?: number | null;
  actualHours?: number | null;
  hourlyRate?: number | null;
  overtimeHours?: number | null;
  overtimeRate?: number | null;
  quantity: number;
  uom: string;
  unitRate: number;
  currency: string;
  exchangeRate: number;
  budgetAmount: number;
  estimatedAmount: number;
  committedAmount: number;
  actualVerifiedAmount?: number | null;
  taxTreatment: string;
  costCenter?: string | null;
  glAccount?: string | null;
  internalOrder?: string | null;
  wbsElement?: string | null;
  supportingDocumentRef?: string | null;
  expenseDateUtc: string;
  approvalReference?: string | null;
  sourceTransaction?: string | null;
  sourceCostPoolId?: string | null;
  status: string;
  salaryMasked: boolean;
}

export interface SilaCulinaryPnlStatementRow {
  section: string;
  rowCode: string;
  label: string;
  originalBudget: number;
  revisedBudget: number;
  estimated: number;
  attributableCommitments: number;
  forecastAtCompletion: number;
  actualVerified?: number | null;
  varianceAmount: number;
  variancePercent?: number | null;
  verificationStatus: string;
  sourceReference?: string | null;
}

export interface SilaCulinaryProcurementFinancialLine {
  materialCode: string;
  materialDescription: string;
  poNumber?: string | null;
  poStatus: string;
  purchasedQty: number;
  eventAllocatedQty: number;
  otherEventsAllocatedQty: number;
  unallocatedInventoryQty: number;
  uom: string;
  unitCost: number;
  purchaseValue: number;
  attributableEventCommitmentValue: number;
  remainingInventoryValue: number;
  receivedValue: number;
  invoicedValue: number;
  costCenter?: string | null;
  glAccount?: string | null;
}

export interface SilaCulinaryFinancialException {
  id: string;
  exceptionType: string;
  severity: string;
  materialOrRecipeRef?: string | null;
  financialImpact?: number | null;
  message: string;
  recommendedAction?: string | null;
  responsibleUser?: string | null;
  resolutionStatus: string;
  resolutionNote?: string | null;
}

export interface SilaCulinaryFinancialScenario {
  id: string;
  name: string;
  scenarioType: string;
  simulatedGuests: number;
  simulatedRevenue: number;
  simulatedTotalCost: number;
  simulatedContribution: number;
  simulatedMarginPercent?: number | null;
  simulatedBreakEvenGuests?: number | null;
  promotedToRevision: boolean;
  dateCreated: string;
}

export interface SilaCulinaryEventFinancialPlanSummary {
  id: string;
  planCode: string;
  name: string;
  versionNumber: number;
  eventId?: string | null;
  eventCode?: string | null;
  eventName?: string | null;
  eventRevision: number;
  propertyId?: string | null;
  propertyName?: string | null;
  companyCode?: string | null;
  venueName?: string | null;
  customerReference?: string | null;
  eventDateUtc?: string | null;
  plannedGuests: number;
  guaranteedGuests: number;
  actualGuests?: number | null;
  billableGuests: number;
  reportingCurrency: string;
  status: string;
  isSimulation: boolean;
  isDemo: boolean;
  originalBudget: number;
  revisedBudget: number;
  netRevenue?: number | null;
  estimatedFoodCost?: number | null;
  estimatedBeverageCost?: number | null;
  estimatedManpowerCost?: number | null;
  estimatedTotalCost?: number | null;
  approvedProcurementCommitment?: number | null;
  attributableProcurementCommitment?: number | null;
  actualVerifiedCost?: number | null;
  actualIsProvisional: boolean;
  forecastAtCompletionCost?: number | null;
  contributionAfterOverhead?: number | null;
  contributionMarginPercent?: number | null;
  foodCostPercent?: number | null;
  beverageCostPercent?: number | null;
  costPerGuest?: number | null;
  revenuePerGuest?: number | null;
  remainingBudget?: number | null;
  targetMarginVariancePercent?: number | null;
  breakEvenGuests?: number | null;
  maxAllowableCost?: number | null;
  remainingCostHeadroom?: number | null;
  approvalReference?: string | null;
  calculatedAtUtc: string;
}

export interface SilaCulinaryEventFinancialPlanDetail {
  summary: SilaCulinaryEventFinancialPlanSummary;
  revenueLines: SilaCulinaryEventRevenueLine[];
  expenseLines: SilaCulinaryEventExpenseLine[];
  procurementLines: SilaCulinaryProcurementFinancialLine[];
  statementRows: SilaCulinaryPnlStatementRow[];
  waterfall: Array<{ label: string; amount: number; kind: string }>;
  exceptions: SilaCulinaryFinancialException[];
  scenarios: SilaCulinaryFinancialScenario[];
  history: Array<{ id: string; action: string; comment?: string | null; actorUserId?: string | null; timestampUtc: string }>;
  costsVisible: boolean;
  salariesVisible: boolean;
  financialStatementsVisible: boolean;
  phase3BHandover?: Record<string, unknown> | null;
  phase4BHandover?: Record<string, unknown> | null;
}

export interface SilaCulinaryFinancialDashboard {
  reportingCurrency: string;
  totalPlannedEventRevenue?: number | null;
  totalEstimatedEventCost?: number | null;
  totalAttributableCommitments?: number | null;
  totalForecastEventContribution?: number | null;
  verifiedActualContribution?: number | null;
  weightedAverageEventMarginPercent?: number | null;
  weightedAverageFoodCostPercent?: number | null;
  weightedAverageBeverageCostPercent?: number | null;
  eventsBelowTargetMarginCount: number;
  eventsExceedingBudgetCount: number;
  pendingFinancialApprovalsCount: number;
  unresolvedFinancialExceptionsCount: number;
  plans: SilaCulinaryEventFinancialPlanSummary[];
}

export interface SilaCulinarySimulateFinancialRequest {
  plannedGuests: number;
  guaranteedGuests: number;
  sellingPricePerGuest: number;
  fixedEventRevenue: number;
  additionalGuestRevenue: number;
  discountPercent: number;
  foodCostPerGuest: number;
  beverageCostPerGuest: number;
  ingredientPriceChangePercent: number;
  manpowerCost: number;
  equipmentCost: number;
  decorationCost: number;
  externalServiceCost: number;
  otherDirectCost: number;
  allocatedOverhead: number;
  targetContributionMarginPercent: number;
  saveScenario?: boolean;
  scenarioName?: string;
  scenarioType?: string;
}

export interface SilaCulinarySimulateFinancialResult {
  savedScenarioId?: string | null;
  reportingCurrency: string;
  plannedGuests: number;
  grossRevenue: number;
  discountTotal: number;
  netRevenue: number;
  variableCosts: number;
  fixedCosts: number;
  totalEventCost: number;
  eventContribution: number;
  contributionMarginPercent: number;
  foodCostPercent: number;
  beverageCostPercent: number;
  costPerGuest: number;
  contributionPerGuest: number;
  breakEvenGuests?: number | null;
  breakEvenRevenue?: number | null;
  requiredSellingPricePerGuest: number;
  maxAllowableCost: number;
  budgetHeadroom: number;
  waterfall: Array<{ label: string; amount: number; kind: string }>;
}

export interface SilaCulinaryMobileFinancialSummary {
  financialPlanId?: string | null;
  planCode: string;
  eventName: string;
  reportingCurrency: string;
  plannedGuests: number;
  foodCost: number;
  beverageCost: number;
  approvedBudget: number;
  budgetUtilizationPercent: number;
  eventContribution?: number | null;
  contributionMarginPercent?: number | null;
  openExceptionsCount: number;
  financialReadiness: string;
  status: string;
}

export const getFinancialConfig = async (): Promise<SilaCulinaryFinancialConfig> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryFinancialConfig>(`${FIN_BASE}/config`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const saveFinancialConfig = async (payload: Partial<SilaCulinaryFinancialConfig>): Promise<SilaCulinaryFinancialConfig> => {
  try {
    const res = await axiosInstance.put<SilaCulinaryFinancialConfig>(`${FIN_BASE}/config`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const calculateFinancialRecipeCost = async (
  recipeId: string,
  payload?: Record<string, unknown>
): Promise<SilaCulinaryFinancialRecipeCost> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryFinancialRecipeCost>(`${FIN_BASE}/recipes/${recipeId}/cost`, payload ?? {});
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const calculateFinancialMenuCost = async (menuId: string, plannedGuests?: number): Promise<SilaCulinaryMenuCost> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMenuCost>(`${FIN_BASE}/menus/${menuId}/cost`, undefined, {
      params: { plannedGuests },
    });
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const analyzeMaterialPriceImpact = async (payload: {
  materialCode: string;
  newUnitPrice: number;
  currency?: string;
}): Promise<SilaCulinaryMaterialPriceImpact> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialPriceImpact>(`${FIN_BASE}/materials/price-impact`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getFinancialDashboard = async (params?: { propertyId?: string; status?: string }): Promise<SilaCulinaryFinancialDashboard> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryFinancialDashboard>(`${FIN_BASE}/dashboard`, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const listEventFinancialPlans = async (params?: {
  propertyId?: string;
  status?: string;
  includeSimulations?: boolean;
}): Promise<SilaCulinaryEventFinancialPlanSummary[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventFinancialPlanSummary[]>(`${FIN_BASE}/plans`, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getEventFinancialPlan = async (planId: string): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const createEventFinancialPlan = async (payload: {
  eventId?: string;
  procurementPlanId?: string;
  name?: string;
  plannedGuests?: number;
  guaranteedGuests?: number;
  sellingPricePerGuest?: number;
  discountPercent?: number;
  foodCostPerGuest?: number;
  beverageCostPerGuest?: number;
  originalBudget?: number;
  isDemo?: boolean;
  isSimulation?: boolean;
}): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const recalculateEventFinancialPlan = async (planId: string): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/recalculate`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const addEventRevenueLine = async (
  planId: string,
  payload: {
    category: string;
    description: string;
    revenueStatus?: string;
    billingBasis?: string;
    quantity: number;
    unitPrice: number;
    discountAmount?: number;
    taxAmount?: number;
    currency?: string;
  }
): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/revenue-lines`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const addEventExpenseLine = async (
  planId: string,
  payload: {
    category: string;
    description: string;
    costBehavior?: string;
    quantity: number;
    uom?: string;
    unitRate: number;
    currency?: string;
    budgetAmount?: number;
    staffCount?: number;
    plannedHours?: number;
    hourlyRate?: number;
    overtimeHours?: number;
    overtimeRate?: number;
    costCenter?: string;
    glAccount?: string;
    sourceCostPoolId?: string;
    markAsActualVerified?: boolean;
  }
): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/expense-lines`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const approveEventExpenseLine = async (planId: string, expenseId: string): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/expense-lines/${expenseId}/approve`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const simulateEventFinancials = async (
  planId: string,
  payload: SilaCulinarySimulateFinancialRequest
): Promise<SilaCulinarySimulateFinancialResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinarySimulateFinancialResult>(`${FIN_BASE}/plans/${planId}/simulate`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const promoteFinancialScenario = async (planId: string, scenarioId: string): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/scenarios/${scenarioId}/promote`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const resolveFinancialException = async (
  planId: string,
  exceptionId: string,
  resolutionStatus: string,
  note?: string
): Promise<void> => {
  try {
    await axiosInstance.post(`${FIN_BASE}/plans/${planId}/exceptions/${exceptionId}/resolve`, { resolutionStatus, note });
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const submitFinancialPlanForApproval = async (planId: string): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/submit`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const decideFinancialPlanApproval = async (
  planId: string,
  decision: "APPROVE" | "REJECT" | "RETURN",
  comment?: string
): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/decide`, { decision, comment });
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const triggerFinancialChangeImpact = async (
  planId: string,
  payload: { updatedGuests?: number; updatedPricePerGuest?: number }
): Promise<SilaCulinaryEventFinancialPlanDetail> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEventFinancialPlanDetail>(`${FIN_BASE}/plans/${planId}/change-impact`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getFinancialCancellationExposure = async (planId: string): Promise<Record<string, unknown>> => {
  try {
    const res = await axiosInstance.get<Record<string, unknown>>(`${FIN_BASE}/plans/${planId}/cancellation-exposure`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const analyzeFinancialPlanWithAi = async (
  planId: string,
  question: string,
  taskName?: string
): Promise<{
  answer: string;
  providerUsed: string;
  groundedFacts: string[];
  warnings: string[];
  promptInjectionBlocked: boolean;
}> => {
  try {
    const res = await axiosInstance.post(`${FIN_BASE}/plans/${planId}/analyze`, { question, taskName });
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getFinancialReportCatalog = async (): Promise<Array<{ reportCode: string; title: string; category: string; description: string }>> => {
  try {
    const res = await axiosInstance.get(`${FIN_BASE}/reports/catalog`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const exportFinancialReport = async (
  planId: string,
  reportCode: string
): Promise<{
  reportCode: string;
  title: string;
  fileName: string;
  csvContent: string;
  previewRows: Array<Record<string, string>>;
}> => {
  try {
    const res = await axiosInstance.post(`${FIN_BASE}/plans/${planId}/reports/${reportCode}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getMobileFinancialSummary = async (planId?: string): Promise<SilaCulinaryMobileFinancialSummary> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMobileFinancialSummary>(`${FIN_BASE}/mobile-summary`, {
      params: { planId },
    });
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getPhase3BHandover = async (planId: string): Promise<Record<string, unknown>> => {
  try {
    const res = await axiosInstance.get<Record<string, unknown>>(`${FIN_BASE}/plans/${planId}/handover/phase3b`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};

export const getPhase4BHandover = async (planId: string): Promise<Record<string, unknown>> => {
  try {
    const res = await axiosInstance.get<Record<string, unknown>>(`${FIN_BASE}/plans/${planId}/handover/phase4b`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err));
  }
};
