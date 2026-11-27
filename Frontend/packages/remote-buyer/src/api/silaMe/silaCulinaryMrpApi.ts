import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const MRP_BASE = "/api/v1/buyer/sila/culinary-mrp";

export interface SilaCulinaryMrpChartPoint {
  label: string;
  qty: number;
  value?: number | null;
}

export interface SilaCulinaryMrpDashboard {
  activeMrpRuns: number;
  upcomingEventRequirements: number;
  approvedMrpBaselines: number;
  pendingMrpApprovals: number;
  uniqueRequiredMaterials: number;
  materialsWithShortages: number;
  materialsWithUnresolvedMappings: number;
  materialsRequiredWithin24Hours: number;
  materialsRequiredWithin48Hours: number;
  estimatedRequirementValue?: number | null;
  indicativeShortageValue?: number | null;
  currency: string;
  costVisible: boolean;
  requirementsByCategory: Record<string, number>;
  valueByCategory: Record<string, number>;
  exceptionsByType: Record<string, number>;
  demandByKitchen: Record<string, number>;
  shortageByMaterial: SilaCulinaryMrpChartPoint[];
  demandByDate: SilaCulinaryMrpChartPoint[];
}

export interface SilaCulinaryMrpCreateRunRequest {
  name?: string;
  runScope: "SINGLE_EVENT" | "SELECTED_SESSIONS" | "SELECTED_KITCHENS" | "MULTI_EVENT" | "DATE_RANGE" | "PROPERTY" | string;
  eventIds: string[];
  sessionIds?: string[];
  kitchenLocationIds?: string[];
  propertyId?: string | null;
  horizonStartUtc?: string | null;
  horizonEndUtc?: string | null;
  bufferPolicy?: "PRECEDENCE" | "ADDITIVE_EXPLICIT";
  preparedStockMode?: "ADJUSTED_SCENARIO" | "GROSS_ONLY";
  isSimulation?: boolean;
  allowUnapprovedEvents?: boolean;
}

export interface SilaCulinaryMrpRunSummary {
  id: string;
  mrpCode: string;
  name: string;
  runScope: string;
  status: string;
  versionNumber: number;
  isApprovedBaseline: boolean;
  isSimulation: boolean;
  engineVersion: string;
  correlationId: string;
  propertyId?: string | null;
  propertyName?: string | null;
  plantCode?: string | null;
  planningHorizonStartUtc?: string | null;
  planningHorizonEndUtc?: string | null;
  calculatedAtUtc: string;
  bufferPolicy: string;
  preparedStockMode: string;
  totalEvents: number;
  totalRecipes: number;
  totalUniqueMaterials: number;
  sufficientStockCount: number;
  shortageCount: number;
  unknownStockCount: number;
  missingMappingCount: number;
  blockingExceptionCount: number;
  warningExceptionCount: number;
  estimatedRequirementValue?: number | null;
  indicativeShortageValue?: number | null;
  currency: string;
  costVisible: boolean;
  inventoryTimestampUtc?: string | null;
  nextRequiredByUtc?: string | null;
  approvalReference?: string | null;
  createdByName?: string | null;
  eventReferences: string[];
}

export interface SilaCulinaryMrpLine {
  id: string;
  consolidationKey: string;
  materialId?: string | null;
  materialCode?: string | null;
  materialDescription: string;
  materialCategory: string;
  isResolvedMaterial: boolean;
  propertyId?: string | null;
  plantCode?: string | null;
  kitchenLocationId?: string | null;
  kitchenName: string;
  storageLocationCode: string;
  requiredDateUtc: string;
  requiredDateBucket: string;
  baseUom: string;
  edibleQty: number;
  rawQty: number;
  grossQty: number;
  preparedStockQty: number;
  indicativeNewProductionQty: number;
  eligibleStockQty?: number | null;
  incomingPoQty: number;
  indicativeCurrentShortageQty?: number | null;
  indicativeRemainingShortageQty?: number | null;
  unitCost?: number | null;
  currency: string;
  costStatus: string;
  requirementValue?: number | null;
  indicativeShortageValue?: number | null;
  costVisible: boolean;
  validationStatus: string;
  isBlocking: boolean;
  inventoryTimestampUtc?: string | null;
  contributingRecipes: string[];
  contributingEvents: string[];
  contributingSessions: string[];
  subRecipePaths: string[];
  exclusionReasons: string[];
}

export interface SilaCulinaryMrpExplosionNode {
  nodeId: string;
  nodeType: string;
  referenceCode: string;
  description: string;
  recipeId?: string | null;
  recipeVersion?: number | null;
  materialCode?: string | null;
  requiredQty: number;
  uom: string;
  yieldPercent?: number | null;
  scalingFactor: number;
  dependencyLevel: number;
  validationStatus: string;
  children: SilaCulinaryMrpExplosionNode[];
}

export interface SilaCulinaryMrpTimeBucket {
  bucketDate: string;
  kitchenName: string;
  materialCount: number;
  totalRequirementValue?: number | null;
  shortageMaterialCount: number;
  earliestRequiredByUtc: string;
}

export interface SilaCulinaryMrpBatchSummary {
  recipeId: string;
  recipeCode: string;
  recipeName: string;
  recipeVersion: number;
  requiredQty: number;
  batchYield: number;
  batchYieldUom: string;
  allowPartialBatches: boolean;
  requiredBatches: number;
  plannedOutputQty: number;
  excessOutputQty: number;
  preparedStockQty: number;
  indicativeNewProductionQty: number;
}

export interface SilaCulinaryMrpException {
  id: string;
  exceptionCode: string;
  exceptionType: string;
  severity: "BLOCKING" | "WARNING" | string;
  eventId?: string | null;
  eventCode?: string | null;
  recipeId?: string | null;
  recipeCode?: string | null;
  materialId?: string | null;
  materialCode?: string | null;
  materialName?: string | null;
  calculationImpact: string;
  responsibleTeam: string;
  recommendedAction: string;
  linkedWorkflow?: string | null;
  resolutionStatus: string;
  resolutionNote?: string | null;
  resolvedByName?: string | null;
  resolvedOnUtc?: string | null;
  cyclePath: string[];
}

export interface SilaCulinaryMrpReadinessCheck {
  dimension: string;
  label: string;
  status: string;
  detail: string;
}

export interface SilaCulinaryMrpReadiness {
  calculationReady: boolean;
  materialDataReady: boolean;
  costingReady: boolean;
  procurementPlanningReady: boolean;
  canSubmitForApproval: boolean;
  canReleaseForPlanning: boolean;
  checks: SilaCulinaryMrpReadinessCheck[];
}

export interface SilaCulinaryMrpScenario {
  id: string;
  scenarioCode: string;
  name: string;
  scenarioType: string;
  isBaseline: boolean;
  plannedGuests: number;
  totalRequirementValue?: number | null;
  totalShortageValue?: number | null;
  shortageCount: number;
  exceptionCount: number;
  createdByName?: string | null;
  createdOn: string;
}

export interface SilaCulinaryMrpSnapshot {
  id: string;
  versionNumber: number;
  statusAtSnapshot: string;
  isApprovedBaseline: boolean;
  changeReason?: string | null;
  approvalReference?: string | null;
  createdByName?: string | null;
  createdOn: string;
}

export interface SilaCulinaryMrpRunDetail extends SilaCulinaryMrpRunSummary {
  lines: SilaCulinaryMrpLine[];
  exceptions: SilaCulinaryMrpException[];
  explosionTree: SilaCulinaryMrpExplosionNode[];
  timePhasedBuckets: SilaCulinaryMrpTimeBucket[];
  batchSummaries: SilaCulinaryMrpBatchSummary[];
  readiness: SilaCulinaryMrpReadiness;
  scenarios: SilaCulinaryMrpScenario[];
  snapshots: SilaCulinaryMrpSnapshot[];
}

export interface SilaCulinaryMrpGroupRow {
  groupKey: string;
  groupLabel: string;
  lineCount: number;
  shortageCount: number;
  commonUom?: string | null;
  totalQtyIfSameUom?: number | null;
  totalRequirementValue?: number | null;
  totalShortageValue?: number | null;
  lines: SilaCulinaryMrpLine[];
}

export interface SilaCulinaryMrpGroupView {
  groupBy: string;
  groups: SilaCulinaryMrpGroupRow[];
}

export interface SilaCulinaryMrpSimulateRequest {
  overrideGuests?: number | null;
  guestScaleFactor: number;
  additionalAllowancePercent: number;
  bufferPercent: number;
  yieldOverridePercent: number;
  priceIncreasePercent: number;
  preparedStockMode: "ADJUSTED_SCENARIO" | "GROSS_ONLY" | string;
  lineOverrides: Array<{
    lineId: string;
    uptakeFactor?: number | null;
    bufferPercent?: number | null;
    preparedStockQty?: number | null;
    eligibleStockQty?: number | null;
    unitCost?: number | null;
  }>;
}

export interface SilaCulinaryMrpSimulationResult {
  plannedGuests: number;
  totalRequirementValue?: number | null;
  totalShortageValue?: number | null;
  shortageCount: number;
  missingMappingCount: number;
  currency: string;
  costVisible: boolean;
  simulatedLines: SilaCulinaryMrpLine[];
}

export interface SilaCulinaryMrpImpactAnalysis {
  baseMrpRunId: string;
  baseMrpCode: string;
  previousTotalValue?: number | null;
  proposedTotalValue?: number | null;
  deltaTotalValue?: number | null;
  newlyRequiredCount: number;
  removedCount: number;
  approvalInvalidated: boolean;
  currency: string;
  lines: Array<{
    materialKey: string;
    materialCode?: string | null;
    materialDescription: string;
    baseUom: string;
    previousQty: number;
    proposedQty: number;
    deltaQty: number;
    impactType: string;
    costImpact?: number | null;
  }>;
}

export interface SilaCulinaryMrpAiAnalyzeResponse {
  providerUsed: string;
  modelUsed: string;
  answer: string;
  referencedMaterials: string[];
  referencedRecipes: string[];
  recommendations: string[];
  dataGroundingNotes: string[];
}

export interface SilaCulinaryMrpOptimizeResult {
  feasible: boolean;
  solver: string;
  totalCost: number;
  totalExcessOutput: number;
  infeasibilityReason?: string | null;
  assumptions: string[];
  optimizedBatches: SilaCulinaryMrpBatchSummary[];
}

export interface SilaCulinaryMrpSubstitutionSuggestion {
  originalMaterialCode: string;
  originalMaterialName: string;
  proposedMaterialCode: string;
  proposedMaterialName: string;
  quantityImpact: number;
  costImpact?: number | null;
  dietaryImpact: string;
  proposedEligibleStock?: number | null;
  chefReviewStatus: string;
}

export interface SilaCulinaryMrpReportCatalogItem {
  reportCode: string;
  category: string;
  title: string;
  description: string;
}

export interface SilaCulinaryMrpReportResult {
  reportCode: string;
  title: string;
  fileName: string;
  contentType: string;
  base64Content: string;
  rowCount: number;
  headers: string[];
  previewRows: string[][];
}

export interface SilaCulinaryPhase2CHandover {
  mrpId: string;
  mrpCode: string;
  mrpVersion: number;
  mrpApprovalStatus: string;
  releasedForPlanning: boolean;
  calculationTimestampUtc: string;
  propertyId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  currency: string;
  zeroReservationsCreated: boolean;
  zeroPosCreated: boolean;
  zeroItosCreated: boolean;
  zeroSapPostingsExecuted: boolean;
  eventReferences: string[];
  materials: Array<{
    materialId?: string | null;
    materialCode?: string | null;
    materialDescription: string;
    materialCategory: string;
    baseUom: string;
    purchasingUom?: string | null;
    grossRequirement: number;
    preparedStockScenario: number;
    indicativeNewProductionQty: number;
    currentEligibleStock?: number | null;
    incomingPoQtySeparate: number;
    indicativeShortage?: number | null;
    requiredByTimestampUtc: string;
    kitchenName: string;
    storageLocationCode: string;
    eventReferences: string[];
    sessionReferences: string[];
    recipeReferences: string[];
    subRecipePaths: string[];
  }>;
  outstandingExceptions: SilaCulinaryMrpException[];
}

export async function getMrpDashboard(propertyId?: string, status?: string): Promise<SilaCulinaryMrpDashboard> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpDashboard>(`${MRP_BASE}/dashboard`, { params: { propertyId, status } });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load MRP dashboard"));
  }
}

export async function listMrpRuns(params?: { propertyId?: string; status?: string; includeSimulations?: boolean }): Promise<SilaCulinaryMrpRunSummary[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpRunSummary[]>(`${MRP_BASE}/runs`, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to list MRP runs"));
  }
}

export async function getMrpRun(runId: string): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs/${runId}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load MRP run detail"));
  }
}

export async function createMrpRun(payload: SilaCulinaryMrpCreateRunRequest): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create MRP run"));
  }
}

export async function recalculateMrpRun(runId: string): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs/${runId}/recalculate`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to recalculate MRP run"));
  }
}

export async function getMrpGroupedView(runId: string, groupBy: string): Promise<SilaCulinaryMrpGroupView> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpGroupView>(`${MRP_BASE}/runs/${runId}/views`, { params: { groupBy } });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load grouped MRP view"));
  }
}

export async function simulateMrpRun(runId: string, payload: SilaCulinaryMrpSimulateRequest): Promise<SilaCulinaryMrpSimulationResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpSimulationResult>(`${MRP_BASE}/runs/${runId}/simulate`, payload);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to run MRP simulation"));
  }
}

export async function saveMrpScenario(
  runId: string,
  name: string,
  scenarioType: string,
  parameters: SilaCulinaryMrpSimulateRequest
): Promise<SilaCulinaryMrpScenario> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpScenario>(`${MRP_BASE}/runs/${runId}/scenarios`, {
      name,
      scenarioType,
      parameters,
    });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to save MRP scenario"));
  }
}

export async function optimizeMrpBatches(runId: string, maxBudget?: number): Promise<SilaCulinaryMrpOptimizeResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpOptimizeResult>(`${MRP_BASE}/runs/${runId}/optimize`, null, {
      params: maxBudget !== undefined ? { maxBudget } : undefined,
    });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to optimize MRP batches"));
  }
}

export async function analyzeMrpWithAi(runId: string, question: string): Promise<SilaCulinaryMrpAiAnalyzeResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpAiAnalyzeResponse>(`${MRP_BASE}/runs/${runId}/ai-analyze`, { question });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to analyze MRP with AI"));
  }
}

export async function suggestMrpSubstitutions(runId: string): Promise<SilaCulinaryMrpSubstitutionSuggestion[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpSubstitutionSuggestion[]>(`${MRP_BASE}/runs/${runId}/substitutions`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load ingredient substitutions"));
  }
}

export async function resolveMrpException(
  runId: string,
  exceptionId: string,
  resolutionStatus: string,
  note?: string
): Promise<SilaCulinaryMrpException> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpException>(`${MRP_BASE}/runs/${runId}/exceptions/${exceptionId}/resolve`, {
      resolutionStatus,
      note,
    });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to update MRP exception"));
  }
}

export async function createMaterialRequestFromMrpException(runId: string, exceptionId: string): Promise<SilaCulinaryMrpException> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpException>(`${MRP_BASE}/runs/${runId}/exceptions/${exceptionId}/create-material-request`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create Material Request from exception"));
  }
}

export async function submitMrpForApproval(runId: string): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs/${runId}/submit`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to submit MRP for approval"));
  }
}

export async function decideMrpApproval(runId: string, decision: "APPROVE" | "REJECT" | "RETURN", comment?: string): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs/${runId}/decide`, { decision, comment });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to decide MRP approval"));
  }
}

export async function createMrpRevision(runId: string, changeReason: string): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs/${runId}/revisions`, { changeReason });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create MRP revision"));
  }
}

export async function analyzeMrpEventImpact(runId: string): Promise<SilaCulinaryMrpImpactAnalysis> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpImpactAnalysis>(`${MRP_BASE}/runs/${runId}/impact-analysis`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to analyze event change impact"));
  }
}

export async function releaseMrpForPlanning(runId: string): Promise<SilaCulinaryMrpRunDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryMrpRunDetail>(`${MRP_BASE}/runs/${runId}/release`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to release MRP for Phase 2C planning"));
  }
}

export async function getMrpPhase2CHandover(runId: string): Promise<SilaCulinaryPhase2CHandover> {
  try {
    const res = await axiosInstance.get<SilaCulinaryPhase2CHandover>(`${MRP_BASE}/runs/${runId}/phase2c-handover`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to fetch Phase 2C procurement handover"));
  }
}

export async function getMrpReportCatalog(): Promise<SilaCulinaryMrpReportCatalogItem[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpReportCatalogItem[]>(`${MRP_BASE}/reports/catalog`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load MRP report catalog"));
  }
}

export async function exportMrpReport(runId: string, reportCode: string): Promise<SilaCulinaryMrpReportResult> {
  try {
    const res = await axiosInstance.get<SilaCulinaryMrpReportResult>(`${MRP_BASE}/runs/${runId}/reports/${reportCode}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to export MRP report"));
  }
}
