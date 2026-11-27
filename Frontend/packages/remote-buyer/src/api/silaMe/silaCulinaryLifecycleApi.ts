import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/culinary-lifecycle";

export type MaterialRequestType =
  | "NEW_MATERIAL"
  | "MATERIAL_EXTENSION"
  | "MATERIAL_REACTIVATION"
  | "UOM_MAINTENANCE"
  | "SPECIFICATION_CORRECTION"
  | "CLASSIFICATION_MAINTENANCE"
  | "MANUAL_REVIEW";

export interface SilaCulinaryDuplicateCandidate {
  materialId?: string | null;
  materialCode: string;
  description: string;
  similarity: number;
  isActive: boolean;
  plantEligible: boolean;
  reason: string;
}

export interface SilaCulinaryMaterialRequestLink {
  id: string;
  draftId: string;
  draftReference?: string | null;
  recipeName?: string | null;
  ingredientTempRef: string;
  ingredientDescription: string;
  requiredQuantity: number;
  recipeUom: string;
  reconnected: boolean;
  chefConfirmed: boolean;
  reconnectedOnUtc?: string | null;
}

export interface SilaCulinaryMaterialRequestHistory {
  id: string;
  action: string;
  previousStatus?: string | null;
  newStatus: string;
  actorUserId: string;
  actorName?: string | null;
  comment?: string | null;
  correlationId?: string | null;
  dateCreated: string;
}

export interface SilaCulinaryMaterialRequest {
  id: string;
  buyerId: string;
  requestReference: string;
  requestType: MaterialRequestType | string;
  status: string;
  revisionNumber: number;
  approvedRevisionNumber?: number | null;
  proposedDescription: string;
  longDescription?: string | null;
  ingredientCategory?: string | null;
  materialType?: string | null;
  materialGroup?: string | null;
  productHierarchy?: string | null;
  brand?: string | null;
  manufacturer?: string | null;
  productSpecification?: string | null;
  barcode?: string | null;
  countryOfOrigin?: string | null;
  propertyId?: string | null;
  propertyName?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  storageLocation?: string | null;
  purchasingOrganization?: string | null;
  purchasingGroup?: string | null;
  baseUom: string;
  purchasingUom?: string | null;
  alternateUom?: string | null;
  packSize?: number | null;
  conversionNumerator?: number | null;
  conversionDenominator?: number | null;
  ingredientForm?: string | null;
  storageRequirements?: string | null;
  shelfLifeDays?: number | null;
  batchManaged: boolean;
  potentialAllergens?: string | null;
  dietaryClassification?: string | null;
  qualitySpecifications?: string | null;
  foodSafetyRequirements?: string | null;
  requiredQuantity?: number | null;
  requiredDateUtc?: string | null;
  requestedByUserId: string;
  requestedByName?: string | null;
  businessJustification?: string | null;
  suggestedSupplier?: string | null;
  targetMaterialId?: string | null;
  targetMaterialCode?: string | null;
  dataOrigin: string;
  idempotencyKey?: string | null;
  correlationId?: string | null;
  executionReference?: string | null;
  executionStatus: string;
  resultingMaterialId?: string | null;
  resultingMaterialCode?: string | null;
  lastError?: string | null;
  decisionComment?: string | null;
  isErpIntegratedTenant: boolean;
  hasErpCreationApi: boolean;
  duplicateCandidates: SilaCulinaryDuplicateCandidate[];
  attachments: string[];
  links: SilaCulinaryMaterialRequestLink[];
  history: SilaCulinaryMaterialRequestHistory[];
  submittedOnUtc?: string | null;
  approvedOnUtc?: string | null;
  materialAvailableOnUtc?: string | null;
  dateCreated: string;
  dateUpdated: string;
}

export interface SilaCulinaryMaterialRequestLinkInput {
  draftId: string;
  ingredientTempRef: string;
  ingredientDescription: string;
  requiredQuantity: number;
  recipeUom: string;
}

export interface SilaCulinaryMaterialRequestUpsert {
  requestType: string;
  proposedDescription: string;
  longDescription?: string | null;
  ingredientCategory?: string | null;
  materialType?: string | null;
  materialGroup?: string | null;
  brand?: string | null;
  manufacturer?: string | null;
  productSpecification?: string | null;
  barcode?: string | null;
  propertyId?: string | null;
  baseUom: string;
  purchasingUom?: string | null;
  alternateUom?: string | null;
  packSize?: number | null;
  conversionNumerator?: number | null;
  conversionDenominator?: number | null;
  storageRequirements?: string | null;
  shelfLifeDays?: number | null;
  batchManaged?: boolean;
  potentialAllergens?: string | null;
  businessJustification?: string | null;
  suggestedSupplier?: string | null;
  targetMaterialId?: string | null;
  targetMaterialCode?: string | null;
  dataOrigin?: string;
  idempotencyKey?: string | null;
  allowDuplicateOverride?: boolean;
  links?: SilaCulinaryMaterialRequestLinkInput[];
}

export interface SilaCulinaryMaterialRequestAssistResult {
  recommendedRequestType: string;
  proposedDescription: string;
  proposedLongDescription: string;
  ingredientCategory: string;
  proposedMaterialGroup: string;
  proposedBaseUom: string;
  productSpecification: string;
  storageRequirement: string;
  potentialAllergens: string[];
  businessJustification: string;
  potentialDuplicates: SilaCulinaryDuplicateCandidate[];
  providerUsed: string;
  aiSuggested: boolean;
}

export interface SilaCulinaryMaterialRequestDashboard {
  totalRequests: number;
  draftRequests: number;
  pendingApprovalRequests: number;
  returnedRequests: number;
  approvedRequests: number;
  creationPendingRequests: number;
  creationFailedRequests: number;
  synchronizationPendingRequests: number;
  resolvedRequests: number;
  recipesBlockedByMissingMaterials: number;
  averageResolutionHours?: number | null;
  isErpIntegratedTenant: boolean;
  hasErpCreationApi: boolean;
  byType: Record<string, number>;
  agingByStage: Record<string, number>;
}

export interface SilaCulinaryCostingOverride {
  tempRef: string;
  quantity?: number | null;
  recipeUom?: string | null;
  quantityBasis?: string | null;
  yieldPercent?: number | null;
  prepLossPercent?: number | null;
  cookLossPercent?: number | null;
  wastagePercent?: number | null;
  bufferPercent?: number | null;
  manualUnitPrice?: number | null;
  manualPriceUnit?: number | null;
  manualPriceSource?: string | null;
}

export interface SilaCulinaryAdditionalCostInput {
  name: string;
  category: string;
  basis: string;
  amount: number;
  units?: number;
}

export interface SilaCulinaryCostingCalculateRequest {
  propertyId?: string | null;
  outletId?: string | null;
  servings?: number | null;
  portionSize?: number | null;
  sellingPricePerPortion?: number | null;
  targetFoodCostPercent?: number | null;
  globalBufferPercent?: number;
  globalPriceIncreasePercent?: number;
  costingBasis?: string | null;
  taxBasis?: string | null;
  scenarioName?: string | null;
  saveSnapshot?: boolean;
  isBaseline?: boolean;
  overrides?: SilaCulinaryCostingOverride[];
  additionalCosts?: SilaCulinaryAdditionalCostInput[];
}

export interface SilaCulinaryCostedLine {
  tempRef: string;
  description: string;
  materialId?: string | null;
  materialCode?: string | null;
  subRecipeId?: string | null;
  isSubRecipe: boolean;
  recipeQty: number;
  recipeUom: string;
  yieldPercent: number;
  effectiveGrossQty: number;
  costingQty: number;
  costingUom: string;
  unitPrice?: number | null;
  priceUnit: number;
  priceCurrency: string;
  priceSource: string;
  isEstimatedCost: boolean;
  priceTimestampUtc?: string | null;
  ingredientCost?: number | null;
  percentageOfTotal?: number | null;
  validationStatus: string;
  issueReason?: string | null;
}

export interface SilaCulinaryCostingResult {
  snapshotId?: string | null;
  costingReference?: string | null;
  draftId: string;
  draftRevision: number;
  recipeName: string;
  propertyId?: string | null;
  propertyName?: string | null;
  scenarioName: string;
  isBaseline: boolean;
  isApprovedSnapshot: boolean;
  approvalStatus: string;
  costingBasis: string;
  taxBasis: string;
  currency: string;
  servings: number;
  totalIngredientCost: number;
  subRecipeCost: number;
  additionalDirectCost: number;
  allocatedOverheadCost: number;
  totalRecipeCost: number;
  ingredientCostPerPortion: number;
  totalCostPerPortion: number;
  sellingPricePerPortion?: number | null;
  foodCostPercent?: number | null;
  targetFoodCostPercent?: number | null;
  targetVariancePercent?: number | null;
  recommendedSellingPriceNet?: number | null;
  recommendedSellingPriceMarkup?: number | null;
  estimatedRevenue?: number | null;
  grossProfit?: number | null;
  contributionAmount?: number | null;
  contributionMarginPercent?: number | null;
  estimatedNetProfitAfterOverhead?: number | null;
  costCompleteness: string;
  missingPriceCount: number;
  currencyMismatchCount: number;
  circularDependencyDetected: boolean;
  canViewCostDetail: boolean;
  lines: SilaCulinaryCostedLine[];
  additionalCosts: SilaCulinaryAdditionalCostInput[];
  warnings: string[];
  calculatedAtUtc: string;
}

export interface SilaCulinaryCostOptimizationSuggestion {
  suggestionId: string;
  category: string;
  title: string;
  rationale: string;
  targetTempRef?: string | null;
  projectedIngredientCost: number;
  projectedCostSavings: number;
  projectedFoodCostPercent?: number | null;
  requiresChefConfirmation: boolean;
  allergenOrSafetyWarning?: string | null;
}

export interface SilaCulinaryCostOptimizationResult {
  draftId: string;
  providerUsed: string;
  baseline: SilaCulinaryCostingResult;
  suggestions: SilaCulinaryCostOptimizationSuggestion[];
  notes: string[];
}

export interface SilaCulinaryReadinessCheckItem {
  code: string;
  label: string;
  status: "PASSED" | "WARNING" | "BLOCKING" | string;
  responsibleTeam: string;
  detail: string;
  recommendedAction?: string | null;
}

export interface SilaCulinaryRecipeReadiness {
  draftId: string;
  draftReference: string;
  recipeName: string;
  draftRevision: number;
  lifecycleStatus: string;
  readyForApproval: boolean;
  readyForProductionId: boolean;
  readyForPublishing: boolean;
  passedCount: number;
  blockingCount: number;
  warningCount: number;
  checks: SilaCulinaryReadinessCheckItem[];
  productionRecipeId?: string | null;
  productionRecipeCode?: string | null;
  productionRecipeVersion?: number | null;
}

export interface SilaCulinaryRecipeLifecycleSummary {
  id: string;
  draftId: string;
  approvedDraftRevision?: number | null;
  costingSnapshotId?: string | null;
  productionRecipeId?: string | null;
  productionRecipeCode?: string | null;
  productionRecipeVersion?: number | null;
  productionActiveVersion?: number | null;
  productionStatus?: string | null;
  lifecycleStatus: string;
  imageAssigned: boolean;
  imageOrigin?: string | null;
  approvalComment?: string | null;
  rejectionReason?: string | null;
  changeReason?: string | null;
  approvedByName?: string | null;
  approvedOnUtc?: string | null;
  publishedOnUtc?: string | null;
  effectiveDateUtc?: string | null;
}

export interface SilaCulinaryApprovalPackage {
  draftId: string;
  draftReference: string;
  draftRevision: number;
  recipeName: string;
  description?: string | null;
  category?: string | null;
  cuisine?: string | null;
  baseServings: number;
  servingUom: string;
  allergens: string[];
  dietaryFlags: string[];
  preparationSteps: string[];
  costingSummary?: SilaCulinaryCostingResult | null;
  readiness: SilaCulinaryRecipeReadiness;
  materialRequests: SilaCulinaryMaterialRequest[];
  lifecycle: SilaCulinaryRecipeLifecycleSummary;
}

export interface SilaCulinaryRecipeVersionHistoryItem {
  version: number;
  status: string;
  isActiveSelling: boolean;
  totalCost: number;
  currency: string;
  changeReason?: string | null;
  dateCreated: string;
  approvedOn?: string | null;
}

export interface SilaCulinaryLifecycleDashboard {
  aiRecipeDrafts: number;
  materialMappingPending: number;
  missingMaterialRequests: number;
  awaitingMaterialSync: number;
  readyForCosting: number;
  costingIncomplete: number;
  aboveFoodCostTarget: number;
  pendingApproval: number;
  approvedRecipes: number;
  readyForPublishing: number;
  publishedRecipes: number;
  requiringRecosting: number;
  recipesWithNewVersions: number;
  lifecycleFunnel: Record<string, number>;
  recipesByCategory: Record<string, number>;
}

export interface SilaCulinaryPhase2AHandover {
  productionRecipeId: string;
  productionRecipeCode: string;
  recipeVersion: number;
  recipeName: string;
  recipeCategory?: string | null;
  cuisine?: string | null;
  baseServings: number;
  servingUom: string;
  sellingUom: string;
  prepDurationMinutes?: number | null;
  cookDurationMinutes?: number | null;
  imageAssigned: boolean;
  pictureEndpoint?: string | null;
  allergens: string[];
  dietaryClassifications: string[];
  costingSnapshotId?: string | null;
  costingReference?: string | null;
  costPerPortion?: number | null;
  authorizedSellingPrice?: number | null;
  currency: string;
  publishingStatus: string;
  effectiveDateUtc?: string | null;
  ingredients: {
    materialCode: string;
    description: string;
    quantity: number;
    uom: string;
    conversionFactor: number;
    yieldPercent: number;
    scalable: boolean;
  }[];
}

// -----------------------------------------------------------------------------
// API Calls
// -----------------------------------------------------------------------------

export const listMaterialRequests = async (params?: {
  status?: string;
  requestType?: string;
  propertyId?: string;
  draftId?: string;
  search?: string;
}): Promise<SilaCulinaryMaterialRequest[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialRequest[]>(`${BASE}/material-requests`, { params });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load material requests."));
  }
};

export const assistMaterialRequest = async (input: {
  ingredientDescription: string;
  requiredQty?: number;
  recipeUom?: string;
  draftId?: string;
  propertyId?: string;
}): Promise<SilaCulinaryMaterialRequestAssistResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequestAssistResult>(`${BASE}/material-requests/assist`, input);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to run material request AI assist."));
  }
};

export const createMaterialRequest = async (
  input: SilaCulinaryMaterialRequestUpsert
): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest>(`${BASE}/material-requests`, input);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to create material request."));
  }
};

export const bulkPrepareMaterialRequests = async (input: {
  propertyId?: string;
  consolidateEquivalent?: boolean;
  businessJustification?: string;
  items: SilaCulinaryMaterialRequestLinkInput[];
}): Promise<SilaCulinaryMaterialRequest[]> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest[]>(`${BASE}/material-requests/bulk-prepare`, input);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to bulk-prepare material requests."));
  }
};

export const updateMaterialRequest = async (
  requestId: string,
  input: SilaCulinaryMaterialRequestUpsert
): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.put<SilaCulinaryMaterialRequest>(`${BASE}/material-requests/${requestId}`, input);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to update material request."));
  }
};

export const submitMaterialRequest = async (requestId: string): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest>(`${BASE}/material-requests/${requestId}/submit`, {});
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to submit material request."));
  }
};

export const decideMaterialRequest = async (
  requestId: string,
  decision: "APPROVE" | "REJECT" | "RETURN" | "CLARIFY" | "CANCEL",
  comment?: string,
  targetMaterialCode?: string
): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest>(`${BASE}/material-requests/${requestId}/decide`, {
      decision,
      comment,
      targetMaterialCode,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to record material request decision."));
  }
};

export const resubmitMaterialRequest = async (
  requestId: string,
  updates?: SilaCulinaryMaterialRequestUpsert
): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest>(`${BASE}/material-requests/${requestId}/resubmit`, updates ?? null);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to resubmit material request."));
  }
};

export const executeMaterialCreation = async (
  requestId: string,
  idempotencyKey?: string
): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest>(
      `${BASE}/material-requests/${requestId}/execute`,
      {},
      { params: idempotencyKey ? { idempotencyKey } : undefined }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to execute material creation."));
  }
};

export const verifyManualErpMaterial = async (
  requestId: string,
  materialCode: string,
  comment?: string
): Promise<SilaCulinaryMaterialRequest> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialRequest>(
      `${BASE}/material-requests/${requestId}/verify-erp-material`,
      { materialCode, comment, autoReconnectDrafts: true }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to verify ERP material reference."));
  }
};

export const getMaterialRequestDashboard = async (
  propertyId?: string
): Promise<SilaCulinaryMaterialRequestDashboard> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialRequestDashboard>(`${BASE}/material-requests/dashboard`, {
      params: propertyId ? { propertyId } : undefined,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load material request dashboard."));
  }
};

export const exportMaterialRequestsReport = async (
  format: "XLSX" | "CSV" | "PDF",
  status?: string
): Promise<{ blob: Blob; fileName: string }> => {
  const res = await axiosInstance.get(`${BASE}/material-requests/export`, {
    params: { format, status },
    responseType: "blob",
  });
  return {
    blob: res.data,
    fileName: `material-requests-report.${format.toLowerCase()}`,
  };
};

export const calculateDraftCosting = async (
  draftId: string,
  input: SilaCulinaryCostingCalculateRequest
): Promise<SilaCulinaryCostingResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryCostingResult>(`${BASE}/drafts/${draftId}/costing/calculate`, input);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to calculate recipe costing."));
  }
};

export const listCostingSnapshots = async (draftId: string): Promise<SilaCulinaryCostingResult[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryCostingResult[]>(`${BASE}/drafts/${draftId}/costing/snapshots`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load costing snapshots."));
  }
};

export const optimizeDraftCosting = async (
  draftId: string,
  costingRequest?: SilaCulinaryCostingCalculateRequest
): Promise<SilaCulinaryCostOptimizationResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryCostOptimizationResult>(`${BASE}/drafts/${draftId}/costing/optimize`, {
      costingRequest,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to optimize recipe cost."));
  }
};

export const bulkRecostRecipes = async (draftIds?: string[], saveNewSnapshot = true) => {
  try {
    const res = await axiosInstance.post(`${BASE}/costing/bulk-recost`, { draftIds, saveNewSnapshot });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to run bulk recosting."));
  }
};

export const getRecipeReadiness = async (draftId: string): Promise<SilaCulinaryRecipeReadiness> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryRecipeReadiness>(`${BASE}/drafts/${draftId}/readiness`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to evaluate recipe readiness."));
  }
};

export const getApprovalPackage = async (draftId: string): Promise<SilaCulinaryApprovalPackage> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryApprovalPackage>(`${BASE}/drafts/${draftId}/approval-package`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load approval package."));
  }
};

export const submitRecipeForApproval = async (
  draftId: string,
  comment?: string
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(`${BASE}/drafts/${draftId}/submit-approval`, {
      comment,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to submit recipe for approval."));
  }
};

export const approveRecipeDraft = async (
  draftId: string,
  comment?: string
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(`${BASE}/drafts/${draftId}/approve`, { comment });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to approve recipe."));
  }
};

export const rejectRecipeDraft = async (
  draftId: string,
  comment?: string
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(`${BASE}/drafts/${draftId}/reject`, { comment });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to reject recipe."));
  }
};

export const createProductionRecipeId = async (
  draftId: string,
  idempotencyKey?: string
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(`${BASE}/drafts/${draftId}/create-production-id`, {
      idempotencyKey,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to create Production Recipe ID."));
  }
};

export const assignProductionRecipeImage = async (
  draftId: string,
  file: File,
  imageOrigin = "CHEF_PHOTOGRAPH"
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const fd = new FormData();
    fd.append("file", file);
    fd.append("imageOrigin", imageOrigin);
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(
      `${BASE}/drafts/${draftId}/assign-image`,
      fd,
      { headers: { "Content-Type": "multipart/form-data" } }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to assign recipe image."));
  }
};

export const publishProductionRecipe = async (
  draftId: string,
  effectiveDateUtc?: string
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(`${BASE}/drafts/${draftId}/publish`, {
      effectiveDateUtc,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to publish recipe."));
  }
};

export const createNewProductionVersion = async (
  draftId: string,
  changeReason: string
): Promise<SilaCulinaryRecipeLifecycleSummary> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryRecipeLifecycleSummary>(`${BASE}/drafts/${draftId}/new-version`, {
      changeReason,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to create new recipe version."));
  }
};

export const getRecipeVersionHistory = async (draftId: string): Promise<SilaCulinaryRecipeVersionHistoryItem[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryRecipeVersionHistoryItem[]>(`${BASE}/drafts/${draftId}/versions`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load recipe version history."));
  }
};

export const getLifecycleDashboard = async (propertyId?: string): Promise<SilaCulinaryLifecycleDashboard> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryLifecycleDashboard>(`${BASE}/dashboard`, {
      params: propertyId ? { propertyId } : undefined,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load lifecycle dashboard."));
  }
};

export const exportLifecycleReport = async (
  draftId: string,
  reportType: string,
  format: "XLSX" | "CSV" | "PDF"
): Promise<{ blob: Blob; fileName: string }> => {
  const res = await axiosInstance.get(`${BASE}/drafts/${draftId}/reports/export`, {
    params: { reportType, format },
    responseType: "blob",
  });
  return {
    blob: res.data,
    fileName: `recipe-${reportType.toLowerCase()}-report.${format.toLowerCase()}`,
  };
};

export const getPhase2AHandover = async (
  productionRecipeId: string,
  version?: number
): Promise<SilaCulinaryPhase2AHandover> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryPhase2AHandover>(`${BASE}/phase2a-handover/${productionRecipeId}`, {
      params: version ? { version } : undefined,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Phase 2A handover contract."));
  }
};
