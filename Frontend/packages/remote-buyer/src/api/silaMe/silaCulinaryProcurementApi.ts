import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const PROC_BASE = "/api/v1/buyer/sila/culinary-procurement";

export interface SilaCulinaryProcurementPlanSummary {
  id: string;
  planCode: string;
  name: string;
  mrpRunId: string;
  mrpCode: string;
  mrpVersion: number;
  primaryEventId?: string | null;
  primaryEventCode?: string | null;
  primaryEventName?: string | null;
  propertyId?: string | null;
  propertyName?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  currency: string;
  status: string;
  versionNumber: number;
  isSimulation: boolean;
  eventDateUtc?: string | null;
  preparationDeadlineUtc?: string | null;
  plannedGuests: number;
  responsibleChef?: string | null;
  venueName?: string | null;
  requiredMaterialsCount: number;
  fullyReservedCount: number;
  incomingPoCoveredCount: number;
  transferCoveredCount: number;
  purchaseRequiredCount: number;
  criticalShortageCount: number;
  delayedDeliveryCount: number;
  pendingApprovalCount: number;
  estimatedNewPurchaseValue?: number | null;
  committedProcurementValue?: number | null;
  physicalReadinessPercent: number;
  projectedReadinessPercent: number;
  calculatedAtUtc: string;
}

export interface SilaCulinaryProcurementDashboard {
  totalPlans: number;
  upcomingEventRequirements: number;
  activeReservationsCount: number;
  materialsAwaitingTransfer: number;
  materialsAwaitingPurchase: number;
  pendingPoApprovals: number;
  openLinkedPos: number;
  overdueReceipts: number;
  averagePhysicalReadinessPercent: number;
  averageProjectedReadinessPercent: number;
  costsVisible: boolean;
  estimatedNewPurchaseValue?: number | null;
  committedProcurementValue?: number | null;
  openExceptionsCount: number;
  plans: SilaCulinaryProcurementPlanSummary[];
}

export interface SilaCulinaryProcurementPurchasingProfile {
  profileId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  storageLocation?: string | null;
  purchasingOrganization?: string | null;
  purchasingGroup?: string | null;
  accountAssignmentCategory?: string | null;
  costCenter?: string | null;
  glAccount?: string | null;
  profitCenter?: string | null;
  internalOrder?: string | null;
  wbsElement?: string | null;
  documentType?: string | null;
  itemCategory?: string | null;
  currency?: string | null;
  isComplete: boolean;
  validationMessage?: string | null;
}

export interface SilaCulinaryProcurementSupplierComparison {
  supplierId?: string | null;
  supplierCode: string;
  supplierName: string;
  contractReference?: string | null;
  contractMandatory: boolean;
  purchasingUom: string;
  unitPrice?: number | null;
  baseUnitPrice?: number | null;
  currency: string;
  packSizeQty: number;
  moqQty: number;
  leadTimeDays: number;
  estimatedDeliveryDateUtc?: string | null;
  latestOrderReleaseDateUtc?: string | null;
  recommendedPurchaseQty: number;
  expectedExcessQty: number;
  totalEstimatedCost?: number | null;
  deliveryFeasible: boolean;
  isSelected: boolean;
}

export interface SilaCulinaryProcurementFefoBatch {
  batchNumber?: string | null;
  expiryDateUtc?: string | null;
  remainingShelfLifeDays?: number | null;
  availableQty: number;
  recommendedQty: number;
  qualityStatus: string;
  locationName?: string | null;
  storageLocationCode?: string | null;
}

export interface SilaCulinaryProcurementItoCandidate {
  sourceLocationId: string;
  sourceLocationName: string;
  sourceStorageLocation?: string | null;
  sourcePlantCode?: string | null;
  sourceType: string;
  availableQty: number;
  safetyStockQty: number;
  competingReservationsQty: number;
  recommendedTransferQty: number;
  samePlant311Eligible: boolean;
  ineligibilityReason?: string | null;
}

export interface SilaCulinaryProcurementLine {
  id: string;
  eventId?: string | null;
  eventCode?: string | null;
  eventName?: string | null;
  materialId?: string | null;
  materialCode?: string | null;
  materialDescription: string;
  materialCategory: string;
  materialImageUrl?: string | null;
  baseUom: string;
  purchasingUom?: string | null;
  destinationLocationId?: string | null;
  destinationKitchenName?: string | null;
  destinationStorageLocation?: string | null;
  requiredByUtc: string;
  grossRequirementQty: number;
  localOnHandQty?: number | null;
  competingReservationsQty: number;
  safetyStockQty: number;
  eligibleLocalStockQty?: number | null;
  activeEventReservationQty: number;
  proposedReservationQty: number;
  preparedStockCoverageQty: number;
  incomingPoAllocatedQty: number;
  incomingTransferAllocatedQty: number;
  proposedItoQty: number;
  confirmedCoverageQty: number;
  remainingConfirmedGapQty: number;
  netUncoveredQty: number;
  recommendedPurchaseQty: number;
  expectedExcessQty: number;
  packSizeQty?: number | null;
  moqQty?: number | null;
  numberOfPacks?: number | null;
  recommendedSupplierId?: string | null;
  recommendedSupplierCode?: string | null;
  recommendedSupplierName?: string | null;
  contractReference?: string | null;
  supplierUnitPrice?: number | null;
  estimatedPurchaseValue?: number | null;
  currency: string;
  expectedDeliveryDateUtc?: string | null;
  latestOrderReleaseDateUtc?: string | null;
  isDeliveryFeasible: boolean;
  coverageStatus: string;
  readinessStatus: string;
  weeklyBucketLinked: boolean;
  weeklyBucketItemId?: string | null;
  supplierComparisons: SilaCulinaryProcurementSupplierComparison[];
  fefoBatches: SilaCulinaryProcurementFefoBatch[];
  itoCandidates: SilaCulinaryProcurementItoCandidate[];
}

export interface SilaCulinaryStockReservation {
  id: string;
  reservationCode: string;
  procurementPlanId: string;
  procurementLineId?: string | null;
  eventId: string;
  eventRevision: number;
  mrpRunId: string;
  mrpVersion: number;
  materialId: string;
  materialCode: string;
  materialDescription: string;
  locationId: string;
  storageLocationCode?: string | null;
  batchNumber?: string | null;
  batchExpiryUtc?: string | null;
  reservedQty: number;
  fulfilledQty: number;
  releasedQty: number;
  baseUom: string;
  requiredByUtc: string;
  expiresAtUtc?: string | null;
  status: string;
  isPreparedStock: boolean;
  safetyStockOverrideUsed: boolean;
  safetyOverrideReason?: string | null;
  priorityRank: number;
  approvalReference?: string | null;
}

export interface SilaCulinaryProcurementAllocation {
  id: string;
  procurementPlanId: string;
  procurementLineId?: string | null;
  eventId: string;
  mrpRunId: string;
  materialId?: string | null;
  materialCode?: string | null;
  materialDescription: string;
  allocationType: string;
  status: string;
  sourceLocationId?: string | null;
  sourceLocationName?: string | null;
  destinationLocationId?: string | null;
  destinationLocationName?: string | null;
  purchaseOrderId?: string | null;
  purchaseOrderItemId?: string | null;
  poNumber?: string | null;
  poItemNumber?: number | null;
  internalTransferOrderId?: string | null;
  itoNumber?: string | null;
  goodsReceiptId?: string | null;
  grnNumber?: string | null;
  sapMaterialDocument?: string | null;
  reversalMaterialDocument?: string | null;
  supplierId?: string | null;
  supplierName?: string | null;
  contractReference?: string | null;
  accountAssignmentCategory?: string | null;
  costCenter?: string | null;
  glAccount?: string | null;
  allocatedQty: number;
  receivedQty: number;
  reversedQty: number;
  remainingQty: number;
  uom: string;
  unitPrice?: number | null;
  estimatedValue?: number | null;
  currency: string;
  expectedDateUtc?: string | null;
  notes?: string | null;
}

export interface SilaCulinaryConsolidatedEventShare {
  eventId: string;
  eventCode: string;
  allocatedQty: number;
}

export interface SilaCulinaryProcurementConsolidatedPurchase {
  consolidationKey: string;
  materialCode: string;
  materialDescription: string;
  supplierId?: string | null;
  supplierName: string;
  companyCode?: string | null;
  plantCode?: string | null;
  baseUom: string;
  combinedNetQty: number;
  recommendedPurchaseQty: number;
  expectedExcessQty: number;
  estimatedValue?: number | null;
  currency: string;
  eventShares: SilaCulinaryConsolidatedEventShare[];
}

export interface SilaCulinaryProcurementException {
  id: string;
  procurementPlanId: string;
  procurementLineId?: string | null;
  eventCode?: string | null;
  materialCode?: string | null;
  materialDescription?: string | null;
  exceptionType: string;
  severity: string;
  quantityImpact: number;
  requiredDateUtc?: string | null;
  responsibleRole?: string | null;
  message: string;
  recommendedAction: string;
  resolutionStatus: string;
  resolutionNote?: string | null;
}

export interface SilaCulinaryProcurementScenario {
  id: string;
  name: string;
  scenarioType: string;
  estimatedPurchaseValue?: number | null;
  projectedReadinessPercent: number;
  parametersJson: string;
  resultsJson: string;
  dateCreated: string;
}

export interface SilaCulinaryProcurementLedgerEntry {
  timestampUtc: string;
  materialCode: string;
  eventType: string;
  sourceReference: string;
  deltaQty: number;
  runningProjectedQty: number;
  confirmed: boolean;
  notes: string;
}

export interface SilaCulinaryProcurementApprovalStep {
  stepOrder: number;
  approverUserId: string;
  status: string;
  comment?: string | null;
  decidedOn?: string | null;
}

export interface SilaCulinaryProcurementHistoryItem {
  action: string;
  comment: string;
  dateCreated: string;
}

export interface SilaCulinaryProcurementPlanDetail {
  summary: SilaCulinaryProcurementPlanSummary;
  costsVisible: boolean;
  coveragePriority: string[];
  purchasingProfile?: SilaCulinaryProcurementPurchasingProfile | null;
  lines: SilaCulinaryProcurementLine[];
  reservations: SilaCulinaryStockReservation[];
  allocations: SilaCulinaryProcurementAllocation[];
  consolidatedPurchases: SilaCulinaryProcurementConsolidatedPurchase[];
  exceptions: SilaCulinaryProcurementException[];
  scenarios: SilaCulinaryProcurementScenario[];
  supplyDemandLedger: SilaCulinaryProcurementLedgerEntry[];
  approvalSteps: SilaCulinaryProcurementApprovalStep[];
  history: SilaCulinaryProcurementHistoryItem[];
}

export interface SilaCulinaryCreateProcurementPlanRequest {
  mrpRunId: string;
  name?: string;
  isSimulation?: boolean;
  includeProposedItoInNet?: boolean;
  allowSafetyStockOverride?: boolean;
  idempotencyKey?: string;
  coveragePriority?: string[];
}

export interface SilaCulinaryCreateReservationRequest {
  procurementLineId: string;
  locationId: string;
  quantity: number;
  batchNumber?: string;
  batchExpiryUtc?: string;
  activateImmediately?: boolean;
  allowSafetyStockOverride?: boolean;
  safetyOverrideReason?: string;
  priorityRank?: number;
  idempotencyKey?: string;
}

export interface SilaCulinaryAllocateIncomingPoRequest {
  procurementLineId: string;
  purchaseOrderItemId: string;
  quantity: number;
  idempotencyKey?: string;
}

export interface SilaCulinaryCreateItoDraftRequest {
  procurementLineId: string;
  fromLocationId: string;
  toLocationId: string;
  quantity: number;
  requiredByUtc?: string;
  reason?: string;
  idempotencyKey?: string;
}

export interface SilaCulinaryCreatePoDraftRequest {
  procurementLineIds: string[];
  supplierId: string;
  deliveryDateUtc?: string;
  accountAssignmentCategory?: string;
  costCenter?: string;
  glAccount?: string;
  internalOrder?: string;
  wbsElement?: string;
  profitCenter?: string;
  contractReference?: string;
  idempotencyKey?: string;
}

export interface SilaCulinaryReconcileGrnRequest {
  goodsReceiptId: string;
  isReversal?: boolean;
  reversalMaterialDocument?: string;
}

export interface SilaCulinaryReconcileItoReversalRequest {
  internalTransferOrderId: string;
  reversedQty: number;
  reversalMaterialDocument?: string;
  reason?: string;
}

export interface SilaCulinarySimulateProcurementRequest {
  guestScaleFactor?: number;
  localStockOverrideQty?: number | null;
  safetyStockOverrideQty?: number | null;
  competingReservationOverrideQty?: number | null;
  incomingPoQtyOverride?: number | null;
  incomingPoDelayDays?: number | null;
  transferableStockOverrideQty?: number | null;
  transferLeadTimeDays?: number | null;
  supplierPriceOverride?: number | null;
  packSizeOverride?: number | null;
  moqOverride?: number | null;
  supplierLeadTimeDaysOverride?: number | null;
  scenarioType?: string;
  scenarioName?: string;
  saveScenario?: boolean;
}

export interface SilaCulinarySimulateProcurementResult {
  grossRequirementQty: number;
  eligibleStockCoverageQty: number;
  expectedReceiptCoverageQty: number;
  transferCoverageQty: number;
  netPurchaseRequirementQty: number;
  recommendedPurchaseQty: number;
  numberOfPacks: number;
  expectedExcessQty: number;
  estimatedPurchaseCost?: number | null;
  deliveryFeasible: boolean;
  readinessImpactPercent: number;
  savedScenarioId?: string | null;
}

export interface SilaCulinaryProcurementOptimizeResult {
  feasible: boolean;
  solverStatus: string;
  solverName: string;
  totalPurchaseCost?: number | null;
  totalExcessQty: number;
  infeasibilityReasons: string[];
  selectedPurchases: SilaCulinaryProcurementConsolidatedPurchase[];
}

export interface SilaCulinaryProcurementAiAnalyzeResponse {
  providerUsed: string;
  answer: string;
  referencedMaterials: string[];
  referencedSuppliers: string[];
  advisoryOnly: boolean;
}

export interface SilaCulinaryProcurementReportCatalogItem {
  reportCode: string;
  title: string;
  category: string;
}

export interface SilaCulinaryProcurementReportResult {
  reportCode: string;
  title: string;
  currency: string;
  generatedAtUtc: string;
  headers: string[];
  previewRows: string[][];
  csvContent: string;
}

export interface SilaCulinaryPhase3AMaterialHandover {
  materialCode: string;
  materialDescription: string;
  grossRequirement: number;
  expectedConsumptionQty: number;
  baseUom: string;
  estimatedMaterialCost?: number | null;
  reservationReferences: string[];
  incomingPoAllocations: string[];
  itoReferences: string[];
  poReferences: string[];
  grnReferences: string[];
  orderedQty: number;
  receivedQty: number;
  outstandingCommitmentQty: number;
  costCenter?: string | null;
  glAccount?: string | null;
  internalOrder?: string | null;
  wbsElement?: string | null;
}

export interface SilaCulinaryPhase3AHandover {
  contractVersion: string;
  procurementPlanId: string;
  planCode: string;
  mrpRunId: string;
  mrpCode: string;
  mrpVersion: number;
  eventId?: string | null;
  eventCode?: string | null;
  eventName?: string | null;
  eventRevision: number;
  propertyId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  kitchenName?: string | null;
  eventDateUtc?: string | null;
  plannedGuests: number;
  guaranteedGuests: number;
  currency: string;
  estimatedMaterialRequirementCost?: number | null;
  estimatedNewPurchaseExpenditure?: number | null;
  approvedProcurementCommitment?: number | null;
  actualReceivedMaterialValue?: number | null;
  futureActualConsumptionCost?: number | null;
  note: string;
  materials: SilaCulinaryPhase3AMaterialHandover[];
}

export async function getProcurementDashboard(params?: {
  propertyId?: string;
  status?: string;
}): Promise<SilaCulinaryProcurementDashboard> {
  try {
    const res = await axiosInstance.get<SilaCulinaryProcurementDashboard>(`${PROC_BASE}/dashboard`, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load procurement dashboard"));
  }
}

export async function listProcurementPlans(params?: {
  propertyId?: string;
  status?: string;
  includeSimulations?: boolean;
}): Promise<SilaCulinaryProcurementPlanSummary[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryProcurementPlanSummary[]>(`${PROC_BASE}/plans`, { params });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to list procurement plans"));
  }
}

export async function getProcurementPlan(planId: string): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.get<SilaCulinaryProcurementPlanDetail>(`${PROC_BASE}/plans/${planId}`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load procurement plan"));
  }
}

export async function createProcurementPlan(
  request: SilaCulinaryCreateProcurementPlanRequest
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(`${PROC_BASE}/plans`, request);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create procurement plan"));
  }
}

export async function recalculateProcurementPlan(planId: string): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(`${PROC_BASE}/plans/${planId}/recalculate`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to recalculate procurement plan"));
  }
}

export async function createStockReservation(
  planId: string,
  request: SilaCulinaryCreateReservationRequest
): Promise<SilaCulinaryStockReservation> {
  try {
    const res = await axiosInstance.post<SilaCulinaryStockReservation>(`${PROC_BASE}/plans/${planId}/reservations`, request);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create stock reservation"));
  }
}

export async function transitionStockReservation(
  planId: string,
  reservationId: string,
  action: "ACTIVATE" | "RELEASE" | "CANCEL" | "FULFILL" | string,
  reason?: string
): Promise<SilaCulinaryStockReservation> {
  try {
    const res = await axiosInstance.post<SilaCulinaryStockReservation>(
      `${PROC_BASE}/plans/${planId}/reservations/${reservationId}/transition`,
      { action, reason }
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to update stock reservation"));
  }
}

export async function allocateIncomingPo(
  planId: string,
  request: SilaCulinaryAllocateIncomingPoRequest
): Promise<SilaCulinaryProcurementAllocation> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementAllocation>(
      `${PROC_BASE}/plans/${planId}/incoming-po-allocations`,
      request
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to allocate incoming PO"));
  }
}

export async function createProcurementItoDraft(
  planId: string,
  request: SilaCulinaryCreateItoDraftRequest
): Promise<SilaCulinaryProcurementAllocation> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementAllocation>(`${PROC_BASE}/plans/${planId}/ito-drafts`, request);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create internal transfer draft"));
  }
}

export async function createProcurementPoDraft(
  planId: string,
  request: SilaCulinaryCreatePoDraftRequest
): Promise<SilaCulinaryProcurementAllocation> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementAllocation>(`${PROC_BASE}/plans/${planId}/po-drafts`, request);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to create purchase order draft"));
  }
}

export async function reconcileProcurementGrn(
  planId: string,
  request: SilaCulinaryReconcileGrnRequest
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(`${PROC_BASE}/plans/${planId}/reconcile-grn`, request);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to reconcile GRN"));
  }
}

export async function reconcileProcurementItoReversal(
  planId: string,
  request: SilaCulinaryReconcileItoReversalRequest
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(
      `${PROC_BASE}/plans/${planId}/reconcile-ito-reversal`,
      request
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to reconcile ITO reversal"));
  }
}

export async function linkWeeklyBucketToProcurementLine(
  planId: string,
  lineId: string,
  weeklyBucketItemId: string
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(
      `${PROC_BASE}/plans/${planId}/lines/${lineId}/link-weekly-bucket`,
      { weeklyBucketItemId }
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to link weekly bucket item"));
  }
}

export async function simulateProcurementPlan(
  planId: string,
  request: SilaCulinarySimulateProcurementRequest
): Promise<SilaCulinarySimulateProcurementResult> {
  try {
    const res = await axiosInstance.post<SilaCulinarySimulateProcurementResult>(`${PROC_BASE}/plans/${planId}/simulate`, request);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to simulate procurement scenario"));
  }
}

export async function optimizeProcurementPlan(
  planId: string,
  maxBudget?: number | null
): Promise<SilaCulinaryProcurementOptimizeResult> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementOptimizeResult>(`${PROC_BASE}/plans/${planId}/optimize`, {
      maxBudget: maxBudget ?? null,
    });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to optimize procurement plan"));
  }
}

export async function analyzeProcurementWithAi(
  planId: string,
  question: string
): Promise<SilaCulinaryProcurementAiAnalyzeResponse> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementAiAnalyzeResponse>(
      `${PROC_BASE}/plans/${planId}/ai-analyze`,
      { question }
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to run AI procurement analysis"));
  }
}

export async function resolveProcurementException(
  planId: string,
  exceptionId: string,
  resolutionStatus: string,
  note?: string
): Promise<SilaCulinaryProcurementException> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementException>(
      `${PROC_BASE}/plans/${planId}/exceptions/${exceptionId}/resolve`,
      { resolutionStatus, note }
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to update procurement exception"));
  }
}

export async function submitProcurementPlanForApproval(planId: string): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(`${PROC_BASE}/plans/${planId}/submit-approval`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to submit procurement plan for approval"));
  }
}

export async function decideProcurementPlanApproval(
  planId: string,
  decision: "APPROVE" | "REJECT" | "RETURN",
  comment?: string
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(
      `${PROC_BASE}/plans/${planId}/decide-approval`,
      { decision, comment }
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to decide procurement approval"));
  }
}

export async function handleProcurementChangeImpact(
  planId: string,
  newMrpRunId: string
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(
      `${PROC_BASE}/plans/${planId}/change-impact`,
      { newMrpRunId }
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to apply MRP revision change impact"));
  }
}

export async function cancelProcurementPlan(
  planId: string,
  reason: string
): Promise<SilaCulinaryProcurementPlanDetail> {
  try {
    const res = await axiosInstance.post<SilaCulinaryProcurementPlanDetail>(`${PROC_BASE}/plans/${planId}/cancel`, { reason });
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to cancel event procurement plan"));
  }
}

export async function getProcurementReportCatalog(): Promise<SilaCulinaryProcurementReportCatalogItem[]> {
  try {
    const res = await axiosInstance.get<SilaCulinaryProcurementReportCatalogItem[]>(`${PROC_BASE}/reports`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load procurement report catalog"));
  }
}

export async function exportProcurementReport(
  planId: string,
  reportCode: string
): Promise<SilaCulinaryProcurementReportResult> {
  try {
    const res = await axiosInstance.get<SilaCulinaryProcurementReportResult>(
      `${PROC_BASE}/plans/${planId}/reports/${reportCode}`
    );
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to export procurement report"));
  }
}

export async function getPhase3AHandover(planId: string): Promise<SilaCulinaryPhase3AHandover> {
  try {
    const res = await axiosInstance.get<SilaCulinaryPhase3AHandover>(`${PROC_BASE}/plans/${planId}/phase3a-handover`);
    return res.data;
  } catch (err) {
    throw new Error(readError(err, "Failed to load Phase 3A cost handover"));
  }
}
