import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/culinary-production";

export interface SilaCulinaryKitchen {
  id: string;
  propertyId?: string | null;
  propertyName?: string | null;
  kitchenCode: string;
  kitchenName: string;
  kitchenType: string;
  associatedStoreLocationId?: string | null;
  responsibleExecutiveChef?: string | null;
  kitchenSupervisor?: string | null;
  dailyPortionCapacity: number;
  outletIds: string[];
  supportedMealPeriods: string[];
  supportedCategories: string[];
  effectiveFromUtc: string;
  effectiveToUtc?: string | null;
  isActive: boolean;
}

export interface SilaCulinaryWorkstation {
  id: string;
  kitchenId: string;
  workstationCode: string;
  workstationName: string;
  workstationType: string;
  concurrentCapacity: number;
}

export interface SilaCulinaryEquipment {
  id?: string;
  kitchenId: string;
  workstationId?: string | null;
  equipmentCode: string;
  equipmentName: string;
  equipmentType: string;
  batchCapacity: number;
  capacityUom: string;
  setupMinutes: number;
  cleaningMinutes: number;
  availableFromMinute: number;
  availableUntilMinute: number;
  underMaintenance: boolean;
  temperatureCapability?: string | null;
  allergenSegregated: boolean;
}

export interface SilaCulinaryStaffAssignment {
  id?: string;
  propertyId?: string | null;
  userId?: string;
  staffName: string;
  culinaryRole: string;
  kitchenId: string;
  workstationId?: string | null;
  shiftCode: string;
  skillClassification: string;
  supervisorName?: string | null;
  availableFromMinute: number;
  availableUntilMinute: number;
  isTemporary: boolean;
  isCrossKitchen: boolean;
  isAvailable: boolean;
}

export interface SilaCulinaryProductionDemand {
  id: string;
  sourceDocumentKey: string;
  demandSource: string;
  demandNature: string;
  propertyId?: string | null;
  kitchenId?: string | null;
  outletId?: string | null;
  eventId?: string | null;
  recipeId?: string | null;
  menuItemName: string;
  mealPeriod: string;
  productionDateUtc: string;
  requiredCompletionUtc: string;
  forecastPortions: number;
  adjustedPortions: number;
  priority: number;
  conversionStatus: string;
}

export interface SilaCulinaryProductionPlanLine {
  id: string;
  recipeId?: string | null;
  recipeCode: string;
  menuItemName: string;
  recipeVersion: number;
  requiredPortions: number;
  plannedProductionQty: number;
  productionUom: string;
  kitchenId?: string | null;
  workstationId?: string | null;
  plannedStartUtc: string;
  requiredCompletionUtc: string;
  priority: number;
  ingredientReadiness: string;
  equipmentReadiness: string;
  staffReadiness: string;
  estimatedFoodCost?: number | null;
  lineStatus: string;
}

export interface SilaCulinaryProductionPlan {
  id: string;
  planCode: string;
  propertyId?: string | null;
  propertyName?: string | null;
  productionDateUtc: string;
  planningPeriod: string;
  mealPeriod: string;
  eventId?: string | null;
  eventReference?: string | null;
  responsibleChef: string;
  sourceDemandType: string;
  status: string;
  versionNumber: number;
  approvedByName?: string | null;
  approvedAtUtc?: string | null;
  totalEstimatedCost?: number | null;
  lines: SilaCulinaryProductionPlanLine[];
}

export interface SilaCulinaryProductionOrderIngredient {
  id: string;
  materialId?: string | null;
  materialCode: string;
  materialName: string;
  grossRequiredQty: number;
  netUsableQty: number;
  yieldAdjustedQty: number;
  inventoryUom: string;
  availableEligibleStock: number;
  incomingBeforeProductionQty: number;
  existingCommitmentsQty: number;
  shortageQty: number;
  readinessStatus: string;
}

export interface SilaCulinaryProductionBatch {
  id: string;
  productionOrderId: string;
  batchNumber: string;
  batchType: string;
  plannedBatchQty: number;
  actualOutputQty: number;
  productionUom: string;
  kitchenId?: string | null;
  workstationId?: string | null;
  equipmentId?: string | null;
  assignedStaffUserId?: string | null;
  assignedChefName?: string | null;
  plannedStartUtc: string;
  plannedFinishUtc: string;
  actualStartUtc?: string | null;
  actualFinishUtc?: string | null;
  batchPriority: number;
  qualityStatus: string;
  batchStatus: string;
  parentBatchId?: string | null;
  useByUtc?: string | null;
  allergenSummary?: string | null;
  qrPayloadJson: string;
}

export interface SilaCulinaryProductionOrder {
  id: string;
  orderNumber: string;
  productionPlanId?: string | null;
  propertyId?: string | null;
  kitchenId?: string | null;
  outletId?: string | null;
  eventId?: string | null;
  recipeId?: string | null;
  recipeVersion: number;
  recipeName: string;
  orderType: string;
  plannedOutputQty: number;
  actualOutputQty: number;
  outputUom: string;
  scheduledStartUtc: string;
  requiredCompletionUtc: string;
  responsibleChef: string;
  priority: number;
  estimatedProductionCost?: number | null;
  approvalStatus: string;
  executionStatus: string;
  ingredients: SilaCulinaryProductionOrderIngredient[];
  batches: SilaCulinaryProductionBatch[];
}

export interface SilaCulinaryProductionTask {
  id: string;
  productionBatchId: string;
  sequenceOrder: number;
  dependsOnTaskId?: string | null;
  taskType: string;
  instructions: string;
  workstationId?: string | null;
  equipmentId?: string | null;
  plannedDurationMinutes: number;
  actualDurationMinutes?: number | null;
  status: string;
  exceptionReason?: string | null;
}

export interface SilaCulinaryProductionPickingLine {
  id: string;
  productionOrderId: string;
  productionBatchId?: string | null;
  materialId?: string | null;
  materialCode: string;
  materialName: string;
  requiredQty: number;
  reservedQty: number;
  pickedQty: number;
  inventoryUom: string;
  storeLocationId?: string | null;
  inventoryBatchNumber?: string | null;
  expiryUtc?: string | null;
  pickingStrategy: string;
  status: string;
}

export interface SilaCulinaryQualityResult {
  id: string;
  productionBatchId: string;
  checkpointName: string;
  expectedValue: string;
  actualValue: string;
  measurementUom: string;
  passed: boolean;
  recordedByName: string;
  recordedAtUtc: string;
  comments?: string | null;
  correctiveAction?: string | null;
  supervisorApproved: boolean;
}

export interface SilaCulinaryProductionException {
  id: string;
  exceptionCode: string;
  productionOrderId?: string | null;
  productionBatchId?: string | null;
  exceptionType: string;
  severity: string;
  description: string;
  reportedByName: string;
  assignedOwner?: string | null;
  recommendedAction?: string | null;
  resolutionStatus: string;
  resolutionNote?: string | null;
}

export interface SilaCulinaryProductionDispatch {
  id: string;
  dispatchNumber: string;
  centralKitchenId: string;
  productionBatchId: string;
  destinationOutletId: string;
  destinationOutletName: string;
  requestedQty: number;
  allocatedQty: number;
  dispatchedQty: number;
  receivedQty: number;
  requiredDeliveryUtc: string;
  status: string;
}

export interface SilaCulinaryScheduledBatch {
  batchId: string;
  batchNumber: string;
  recipeName: string;
  assignedEquipmentId?: string | null;
  assignedStaffId?: string | null;
  plannedStartUtc: string;
  plannedFinishUtc: string;
  delayMinutes: number;
  holdingBufferMinutes: number;
}

export interface SilaCulinaryScheduleOptimizeResponse {
  status: string;
  solverUsed: string;
  assignments: SilaCulinaryScheduledBatch[];
  equipmentUtilizationPct: Record<string, number>;
  staffUtilizationPct: Record<string, number>;
  bottlenecks: string[];
  conflictingConstraints: string[];
  assumptions: string[];
  alternativeRecommendations: string[];
}

export interface SilaCulinaryProductionDashboard {
  plannedOrdersCount: number;
  releasedOrdersCount: number;
  activeBatchesCount: number;
  completedBatchesCount: number;
  delayedBatchesCount: number;
  ingredientShortagesCount: number;
  pendingQualityChecksCount: number;
  equipmentUtilizationPct: number;
  staffUtilizationPct: number;
  onTimeProductionRatePct: number;
  plannedOutputTotal: number;
  actualOutputTotal: number;
  orders: SilaCulinaryProductionOrder[];
  kdsBatches: SilaCulinaryProductionBatch[];
  openExceptions: SilaCulinaryProductionException[];
}

export interface SilaCulinaryProductionBatchTraceability {
  batchId: string;
  batchNumber: string;
  recipeName: string;
  recipeVersion: number;
  qrPayloadJson: string;
  backwardTraceSummary: string;
  forwardTraceSummary: string;
  ingredientBatches: string[];
  outletDispatches: SilaCulinaryProductionDispatch[];
  qualityResults: SilaCulinaryQualityResult[];
}

export interface SilaCulinaryProductionCopilotResponse {
  answer: string;
  dataTimestampUtc: string;
  supportingMetrics: string[];
  identifiedRisks: string[];
  recommendedAction: string;
  expectedOperationalImpact: string;
  actionBlockedNotice: boolean;
}

export interface SilaCulinaryProductionReportCatalogItem {
  reportCode: string;
  name: string;
  description: string;
  formats: string[];
}

export interface SilaCulinaryProductionReportExport {
  reportCode: string;
  format: string;
  fileName: string;
  contentType: string;
  content: string;
}

export interface SilaCulinaryMobileProductionSummary {
  canViewCosts: boolean;
  myTasksCount: number;
  activeBatchesCount: number;
  pendingPickingCount: number;
  pendingQualityChecksCount: number;
  openExceptionsCount: number;
  myTasks: SilaCulinaryProductionTask[];
  activeBatches: SilaCulinaryProductionBatch[];
  pickingLines: SilaCulinaryProductionPickingLine[];
  exceptions: SilaCulinaryProductionException[];
}

export const listCulinaryKitchens = async (propertyId?: string): Promise<SilaCulinaryKitchen[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryKitchen[]>(`${BASE}/kitchens`, { params: { propertyId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load kitchens."));
  }
};

export const upsertCulinaryKitchen = async (payload: Partial<SilaCulinaryKitchen>): Promise<SilaCulinaryKitchen> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryKitchen>(`${BASE}/kitchens`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to save kitchen."));
  }
};

export const listCulinaryWorkstations = async (kitchenId?: string): Promise<SilaCulinaryWorkstation[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryWorkstation[]>(`${BASE}/workstations`, { params: { kitchenId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load workstations."));
  }
};

export const upsertCulinaryWorkstation = async (payload: Partial<SilaCulinaryWorkstation>): Promise<SilaCulinaryWorkstation> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryWorkstation>(`${BASE}/workstations`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to save workstation."));
  }
};

export const listCulinaryEquipment = async (kitchenId?: string): Promise<SilaCulinaryEquipment[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryEquipment[]>(`${BASE}/equipment`, { params: { kitchenId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load kitchen equipment."));
  }
};

export const upsertCulinaryEquipment = async (payload: Partial<SilaCulinaryEquipment>): Promise<SilaCulinaryEquipment> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryEquipment>(`${BASE}/equipment`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to save equipment."));
  }
};

export const listCulinaryStaff = async (kitchenId?: string): Promise<SilaCulinaryStaffAssignment[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryStaffAssignment[]>(`${BASE}/staff`, { params: { kitchenId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load kitchen staff."));
  }
};

export const upsertCulinaryStaff = async (payload: Partial<SilaCulinaryStaffAssignment>): Promise<SilaCulinaryStaffAssignment> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryStaffAssignment>(`${BASE}/staff`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to save kitchen staff assignment."));
  }
};

export const listProductionDemands = async (propertyId?: string): Promise<SilaCulinaryProductionDemand[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionDemand[]>(`${BASE}/demands`, { params: { propertyId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production demands."));
  }
};

export const createProductionDemand = async (payload: Partial<SilaCulinaryProductionDemand>): Promise<SilaCulinaryProductionDemand> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionDemand>(`${BASE}/demands`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to create production demand."));
  }
};

export const adjustProductionDemand = async (demandId: string, portions: number): Promise<SilaCulinaryProductionDemand> => {
  try {
    const res = await axiosInstance.put<SilaCulinaryProductionDemand>(`${BASE}/demands/${demandId}/adjust`, null, { params: { portions } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to adjust demand."));
  }
};

export const listProductionPlans = async (propertyId?: string, status?: string): Promise<SilaCulinaryProductionPlan[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionPlan[]>(`${BASE}/plans`, { params: { propertyId, status } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production plans."));
  }
};

export const createProductionPlan = async (payload: {
  propertyId?: string;
  propertyName?: string;
  productionDateUtc?: string;
  planningPeriod?: string;
  mealPeriod?: string;
  eventId?: string;
  eventReference?: string;
  responsibleChef?: string;
  demandIds?: string[];
  manualLines?: { recipeId?: string; menuItemName: string; requiredPortions: number; kitchenId?: string; workstationId?: string; requiredCompletionUtc?: string; priority?: number }[];
}): Promise<SilaCulinaryProductionPlan> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPlan>(`${BASE}/plans`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to create production plan."));
  }
};

export const explodeValidateProductionPlan = async (planId: string): Promise<SilaCulinaryProductionPlan> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPlan>(`${BASE}/plans/${planId}/explode-validate`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to explode & validate recipe requirements."));
  }
};

export const submitProductionPlanApproval = async (planId: string): Promise<SilaCulinaryProductionPlan> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPlan>(`${BASE}/plans/${planId}/submit-approval`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to submit plan for approval."));
  }
};

export const decideProductionPlanApproval = async (planId: string, decision: "APPROVE" | "REJECT", comment?: string): Promise<SilaCulinaryProductionPlan> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPlan>(`${BASE}/plans/${planId}/decide-approval`, { decision, comment });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to record plan approval decision."));
  }
};

export const amendProductionPlan = async (planId: string, changeSummary: string, updatedLines: { recipeId?: string; menuItemName: string; requiredPortions: number; kitchenId?: string; priority?: number }[]): Promise<SilaCulinaryProductionPlan> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPlan>(`${BASE}/plans/${planId}/amend`, { changeSummary, updatedLines });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to amend production plan."));
  }
};

export const releaseProductionPlanToOrders = async (planId: string): Promise<SilaCulinaryProductionPlan> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPlan>(`${BASE}/plans/${planId}/release`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to release production plan."));
  }
};

export const listProductionOrders = async (kitchenId?: string, status?: string): Promise<SilaCulinaryProductionOrder[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionOrder[]>(`${BASE}/orders`, { params: { kitchenId, status } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production orders."));
  }
};

export const rescheduleProductionBatch = async (
  batchId: string,
  payload: { plannedStartUtc: string; plannedFinishUtc: string; equipmentId?: string; workstationId?: string }
): Promise<SilaCulinaryProductionBatch> => {
  try {
    const res = await axiosInstance.put<SilaCulinaryProductionBatch>(`${BASE}/batches/${batchId}/reschedule`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to reschedule batch."));
  }
};

export const optimizeProductionSchedule = async (payload: { productionPlanId?: string; objective?: string; enforceFoodSafety?: boolean }): Promise<SilaCulinaryScheduleOptimizeResponse> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryScheduleOptimizeResponse>(`${BASE}/schedule/optimize`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to optimize kitchen schedule."));
  }
};

export const applyProductionSchedule = async (assignments: SilaCulinaryScheduledBatch[]): Promise<SilaCulinaryProductionBatch[]> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionBatch[]>(`${BASE}/schedule/apply`, assignments);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to apply optimized schedule."));
  }
};

export const getProductionCalendar = async (params?: { fromUtc?: string; toUtc?: string; kitchenId?: string }): Promise<SilaCulinaryProductionBatch[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionBatch[]>(`${BASE}/calendar`, { params });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production calendar."));
  }
};

export const generateOrderPickingList = async (orderId: string, strategy: "FEFO" | "FIFO" = "FEFO"): Promise<SilaCulinaryProductionPickingLine[]> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPickingLine[]>(`${BASE}/orders/${orderId}/picking-list`, null, { params: { strategy } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to generate picking list."));
  }
};

export const confirmProductionPickingLine = async (pickingLineId: string, pickedQty: number, inventoryBatchNumber?: string): Promise<SilaCulinaryProductionPickingLine> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionPickingLine>(`${BASE}/picking-lines/${pickingLineId}/confirm`, { pickedQty, inventoryBatchNumber });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to confirm picking line."));
  }
};

export const issueMaterialToProduction = async (payload: {
  productionOrderId: string;
  productionBatchId?: string;
  fromStoreLocationId: string;
  toKitchenLocationId: string;
  materialCode: string;
  issueQty: number;
  uom: string;
  idempotencyKey: string;
  comment?: string;
}): Promise<unknown> => {
  try {
    const res = await axiosInstance.post(`${BASE}/material-issue`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to issue material to production."));
  }
};

export const getProductionKdsFeed = async (kitchenId?: string, viewMode?: string): Promise<SilaCulinaryProductionBatch[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionBatch[]>(`${BASE}/kds`, { params: { kitchenId, viewMode } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load KDS feed."));
  }
};

export const executeProductionBatchAction = async (
  batchId: string,
  action: "START_PREP" | "START_COOKING" | "PAUSE" | "RESUME" | "READY_FOR_QUALITY" | "COMPLETE_BATCH",
  actualOutputQty?: number,
  note?: string
): Promise<SilaCulinaryProductionBatch> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionBatch>(`${BASE}/batches/${batchId}/execute`, { action, actualOutputQty, note });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to execute batch action."));
  }
};

export const listBatchTasks = async (batchId: string): Promise<SilaCulinaryProductionTask[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionTask[]>(`${BASE}/batches/${batchId}/tasks`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load batch tasks."));
  }
};

export const updateProductionTaskStatus = async (taskId: string, status: string, exceptionReason?: string): Promise<SilaCulinaryProductionTask> => {
  try {
    const res = await axiosInstance.put<SilaCulinaryProductionTask>(`${BASE}/tasks/${taskId}/status`, { status, exceptionReason });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to update task status."));
  }
};

export const recordProductionQualityCheck = async (payload: {
  productionBatchId: string;
  checkpointName: string;
  expectedValue: string;
  actualValue: string;
  measurementUom: string;
  passed: boolean;
  comments?: string;
  correctiveAction?: string;
}): Promise<SilaCulinaryQualityResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryQualityResult>(`${BASE}/quality`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to record quality check."));
  }
};

export const resolveQualityHold = async (qualityResultId: string, correctiveAction: string, approveRelease = true): Promise<SilaCulinaryQualityResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryQualityResult>(`${BASE}/quality/${qualityResultId}/resolve-hold`, { correctiveAction, approveRelease });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to resolve quality hold."));
  }
};

export const getProductionBatchTraceability = async (batchId: string): Promise<SilaCulinaryProductionBatchTraceability> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionBatchTraceability>(`${BASE}/batches/${batchId}/traceability`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load batch traceability."));
  }
};

export const listCentralKitchenDispatches = async (centralKitchenId?: string): Promise<SilaCulinaryProductionDispatch[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionDispatch[]>(`${BASE}/central-kitchen/dispatches`, { params: { centralKitchenId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load central kitchen dispatches."));
  }
};

export const allocateCentralKitchenDispatch = async (payload: {
  centralKitchenId: string;
  batchId: string;
  destinationOutletId: string;
  destinationOutletName: string;
  requestedQty: number;
  requiredDeliveryUtc?: string;
}): Promise<SilaCulinaryProductionDispatch> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionDispatch>(`${BASE}/central-kitchen/allocate`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to allocate central kitchen dispatch."));
  }
};

export const moveCentralKitchenDispatch = async (dispatchId: string, action: "DISPATCH" | "RECEIVE", qty: number): Promise<SilaCulinaryProductionDispatch> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionDispatch>(`${BASE}/central-kitchen/dispatches/${dispatchId}/move`, { action, qty });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to update dispatch movement."));
  }
};

export const triggerBuffetReplenishment = async (payload: { kitchenId: string; itemName: string; qty: number; recipeId?: string }): Promise<SilaCulinaryProductionBatch> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionBatch>(`${BASE}/buffet/replenish`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to trigger buffet replenishment."));
  }
};

export const getProductionDashboard = async (propertyId?: string, kitchenId?: string): Promise<SilaCulinaryProductionDashboard> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionDashboard>(`${BASE}/dashboard`, { params: { propertyId, kitchenId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production dashboard."));
  }
};

export const listProductionExceptions = async (status?: string): Promise<SilaCulinaryProductionException[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionException[]>(`${BASE}/exceptions`, { params: { status } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production exceptions."));
  }
};

export const reportProductionException = async (payload: {
  orderId?: string;
  batchId?: string;
  exceptionType: string;
  severity: string;
  description: string;
  recommendedAction?: string;
}): Promise<SilaCulinaryProductionException> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionException>(`${BASE}/exceptions`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to report exception."));
  }
};

export const resolveProductionException = async (exceptionId: string, resolutionStatus: string, note?: string): Promise<SilaCulinaryProductionException> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionException>(`${BASE}/exceptions/${exceptionId}/resolve`, { resolutionStatus, note });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to resolve exception."));
  }
};

export const queryProductionCopilot = async (question: string): Promise<SilaCulinaryProductionCopilotResponse> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionCopilotResponse>(`${BASE}/copilot`, { question });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to query production copilot."));
  }
};

export const getProductionReportCatalog = async (): Promise<SilaCulinaryProductionReportCatalogItem[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryProductionReportCatalogItem[]>(`${BASE}/reports/catalog`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load production report catalog."));
  }
};

export const exportProductionReport = async (reportCode: string, format: "CSV" | "EXCEL" | "PDF" = "CSV"): Promise<SilaCulinaryProductionReportExport> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryProductionReportExport>(`${BASE}/reports/${reportCode}/export`, null, { params: { format } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to export production report."));
  }
};

export const getMobileProductionSummary = async (kitchenId?: string): Promise<SilaCulinaryMobileProductionSummary> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMobileProductionSummary>(`${BASE}/mobile/summary`, { params: { kitchenId } });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load mobile production summary."));
  }
};
