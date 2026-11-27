import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/culinary-materials";

export interface SilaCulinaryMaterialStock {
  status: string;
  propertyId?: string | null;
  propertyName?: string | null;
  storageLocation?: string | null;
  onHandQty?: number | null;
  availableQty?: number | null;
  reservedQty?: number | null;
  inventoryUom?: string | null;
  stockTimestampUtc?: string | null;
}

export interface SilaCulinaryMaterialPrice {
  status: string;
  price?: number | null;
  currency?: string | null;
  priceUom?: string | null;
  priceSource?: string | null;
  supplierName?: string | null;
  effectiveDateUtc?: string | null;
  dataTimestampUtc?: string | null;
}

export interface SilaCulinaryMaterialCandidate {
  materialId: string;
  materialCode: string;
  description: string;
  materialGroup?: string | null;
  productType?: string | null;
  baseUom: string;
  orderUom?: string | null;
  alternateUom?: string | null;
  barcode?: string | null;
  imageUrl?: string | null;
  matchMethod: string;
  finalScore: number;
  exactScore: number;
  aliasScore: number;
  fuzzyScore: number;
  semanticScore: number;
  rerankScore: number;
  specificationCompatible: boolean;
  uomCompatible: boolean;
  materialActive: boolean;
  plantEligible: boolean;
  convertedBaseQuantity?: number | null;
  conversionFactor?: number | null;
  indicativePacksNeeded?: number | null;
  indicativeWholePacks?: number | null;
  excessAfterPackRounding?: number | null;
  uomReason?: string | null;
  stock?: SilaCulinaryMaterialStock | null;
  price?: SilaCulinaryMaterialPrice | null;
  evidence: string[];
  warnings: string[];
}

export interface SilaCulinaryMaterialRequestDraft {
  requestType: string;
  proposedDescription: string;
  proposedLongDescription?: string | null;
  ingredientCategory?: string | null;
  proposedMaterialGroup?: string | null;
  proposedBaseUom?: string | null;
  proposedPurchasingUom?: string | null;
  proposedPackSize?: string | null;
  brandPreference?: string | null;
  productSpecification?: string | null;
  storageRequirement?: string | null;
  potentialAllergens: string[];
  draftId: string;
  draftReference: string;
  ingredientTempRef: string;
  requiredQuantity: number;
  recipeUom: string;
  propertyId?: string | null;
  plantCode?: string | null;
  requestedBy?: string | null;
  reason: string;
  duplicateCheckEvidence: string[];
  candidateMaterialCodesChecked: string[];
  isAiSuggested: boolean;
}

export interface SilaCulinaryMaterialMappingLine {
  mappingId: string;
  mappingRevision: number;
  ingredientTempRef: string;
  originalDescription: string;
  normalizedDescription: string;
  ingredientImageUrl?: string | null;
  requiredQuantity: number;
  recipeUom: string;
  materialId?: string | null;
  materialCode?: string | null;
  materialDescription?: string | null;
  materialImageUrl?: string | null;
  baseUom?: string | null;
  purchasingUom?: string | null;
  convertedQuantity?: number | null;
  conversionFactor?: number | null;
  indicativePacksNeeded?: number | null;
  indicativeWholePacks?: number | null;
  excessAfterPackRounding?: number | null;
  availableStock?: number | null;
  onHandStock?: number | null;
  reservedStock?: number | null;
  stockUom?: string | null;
  stockStatus: string;
  indicativeShortage?: number | null;
  unitPrice?: number | null;
  currency?: string | null;
  priceUom?: string | null;
  priceSource?: string | null;
  extendedLineCost?: number | null;
  priceStatus: string;
  matchMethod: string;
  matchScore: number;
  mappingStatus: string;
  missingClassification: string;
  recommendedRequestType?: string | null;
  specificationCompatible: boolean;
  uomCompatible: boolean;
  requiresReview: boolean;
  isSubstitution: boolean;
  substitutedFromMaterialCode?: string | null;
  replacementRatio?: number | null;
  overrideReason?: string | null;
  missingReason?: string | null;
  confirmedByName?: string | null;
  confirmedOnUtc?: string | null;
  lastMatchedOnUtc?: string | null;
  extractedAttributes?: Record<string, unknown> | null;
  evidence: string[];
  warnings: string[];
  candidates: SilaCulinaryMaterialCandidate[];
  requestDraft?: SilaCulinaryMaterialRequestDraft | null;
}

export interface SilaCulinaryMaterialKpis {
  totalIngredients: number;
  exactMatches: number;
  aliasMatches: number;
  suggestedMatches: number;
  confirmedMappings: number;
  pendingReview: number;
  missingMaterials: number;
  uomConflicts: number;
  specificationConflicts: number;
  stockShortages: number;
  missingPrices: number;
}

export interface SilaCulinaryMaterialWorkspace {
  draftId: string;
  draftReference: string;
  draftRevision: number;
  recipeName: string;
  servings: number;
  servingSize?: string | null;
  propertyId?: string | null;
  propertyName?: string | null;
  kitchenName?: string | null;
  plantCode?: string | null;
  operatingMode: string;
  matchingEngine: string;
  canViewCost: boolean;
  canApproveAliases: boolean;
  lastMatchedOnUtc?: string | null;
  readinessStatus: string;
  kpis: SilaCulinaryMaterialKpis;
  lines: SilaCulinaryMaterialMappingLine[];
}

export interface SilaCulinaryMaterialSearchItem {
  materialId: string;
  materialCode: string;
  description: string;
  materialGroup?: string | null;
  productType?: string | null;
  baseUom: string;
  orderUom?: string | null;
  alternateUom?: string | null;
  barcode?: string | null;
  imageUrl?: string | null;
  isActive: boolean;
  plantEligible: boolean;
  stock?: SilaCulinaryMaterialStock | null;
  price?: SilaCulinaryMaterialPrice | null;
}

export interface SilaCulinarySubstitutionComparison {
  substitution_kind: "ALTERNATIVE_MATERIAL_MATCH" | "CULINARY_SUBSTITUTION";
  original_ingredient: string;
  original_material_code?: string | null;
  proposed_ingredient: string;
  proposed_material_id?: string | null;
  proposed_material_code?: string | null;
  proposed_material_description?: string | null;
  suggested_replacement_ratio?: number | null;
  ratio_requires_chef_input: boolean;
  converted_quantity?: number | null;
  target_uom?: string | null;
  uom_compatible: boolean;
  stock_available?: number | null;
  stock_status: string;
  estimated_cost_impact?: number | null;
  currency?: string | null;
  taste_impact: string;
  texture_impact: string;
  preparation_changes: string;
  dietary_impact: string;
  allergen_impact: string;
  chef_verification_requirements: string[];
  confidence_note: string;
}

export interface SilaCulinarySubstitutionResponse {
  temp_ref: string;
  original_ingredient: string;
  suggestions: SilaCulinarySubstitutionComparison[];
  llm_used: boolean;
  provider?: string | null;
  model?: string | null;
  warnings: string[];
}

export interface SilaCulinaryMaterialAlias {
  id: string;
  aliasText: string;
  normalizedAlias: string;
  materialId: string;
  materialCode: string;
  materialDescription?: string | null;
  propertyId?: string | null;
  language: string;
  source: string;
  approvalStatus: string;
  revision: number;
  createdByName: string;
  approvedByName?: string | null;
  approvedOnUtc?: string | null;
  createdOn: string;
}

export interface SilaCulinaryMaterialIndexStatus {
  buyerId: string;
  status: string;
  indexedMaterialCount: number;
  activeMaterialCount: number;
  pendingUpdatesCount: number;
  embeddingModelVersion: string;
  rerankingModelVersion: string;
  semanticBackend: string;
  buildDurationMs: number;
  lastBuildCompletedUtc?: string | null;
  lastIncrementalSyncUtc?: string | null;
  lastError?: string | null;
}

export interface SilaCulinaryChartPoint {
  label: string;
  count: number;
}

export interface SilaCulinaryMaterialDashboard {
  isRealTelemetry: boolean;
  totalIngredientsProcessed: number;
  exactMatches: number;
  aliasMatches: number;
  fuzzyMatches: number;
  semanticMatches: number;
  aiAssistedMatches: number;
  confirmedMappings: number;
  rejectedCandidates: number;
  pendingReview: number;
  missingMaterials: number;
  uomConflicts: number;
  activeAliases: number;
  localMatchingUtilizationPct: number;
  externalAiUtilizationPct: number;
  indexHealth: SilaCulinaryMaterialIndexStatus;
  methodDistribution: SilaCulinaryChartPoint[];
  statusDistribution: SilaCulinaryChartPoint[];
  unresolvedIngredients: SilaCulinaryChartPoint[];
}

export interface SilaCulinaryPhase1CHandover {
  draftId: string;
  draftReference: string;
  draftRevision: number;
  recipeName: string;
  servings: number;
  servingSize?: string | null;
  readinessStatus: string;
  readyForPhase1C: boolean;
  outstandingValidationIssues: string[];
  ingredientLines: SilaCulinaryMaterialMappingLine[];
  materialRequestDrafts: SilaCulinaryMaterialRequestDraft[];
}

export interface SilaCulinaryMaterialHistory {
  id: string;
  ingredientTempRef: string;
  mappingRevision: number;
  action: string;
  previousStatus?: string | null;
  newStatus: string;
  previousMaterialCode?: string | null;
  newMaterialCode?: string | null;
  reason?: string | null;
  actorName: string;
  createdOn: string;
}

export const getMaterialWorkspace = async (
  draftId: string
): Promise<SilaCulinaryMaterialWorkspace> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialWorkspace>(
      `${BASE}/drafts/${draftId}/workspace`
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Material Intelligence workspace."));
  }
};

export const matchDraftMaterials = async (
  draftId: string,
  req: {
    ingredientRefs?: string[] | null;
    unresolvedOnly?: boolean;
    overwriteConfirmed?: boolean;
    useAiDisambiguation?: boolean;
    propertyId?: string | null;
  } = {},
  signal?: AbortSignal
): Promise<SilaCulinaryMaterialWorkspace> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialWorkspace>(
      `${BASE}/drafts/${draftId}/match`,
      req,
      { signal }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Material matching failed."));
  }
};

export const matchSingleIngredient = async (
  draftId: string,
  ingredientTempRef: string,
  req: {
    overwriteConfirmed?: boolean;
    useAiDisambiguation?: boolean;
    propertyId?: string | null;
  } = {},
  signal?: AbortSignal
): Promise<SilaCulinaryMaterialMappingLine> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialMappingLine>(
      `${BASE}/drafts/${draftId}/ingredients/${encodeURIComponent(ingredientTempRef)}/match`,
      req,
      { signal }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Ingredient rematch failed."));
  }
};

export const recalculateMaterialRequirements = async (
  draftId: string,
  req: { servings: number; servingSize?: string | null; propertyId?: string | null }
): Promise<SilaCulinaryMaterialWorkspace> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialWorkspace>(
      `${BASE}/drafts/${draftId}/recalculate`,
      req
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to recalculate material requirements."));
  }
};

export const confirmMaterialMapping = async (
  draftId: string,
  ingredientTempRef: string,
  req: {
    expectedMappingRevision?: number;
    materialId?: string | null;
    materialCode?: string | null;
    reason?: string | null;
    suggestAlias?: boolean;
  }
): Promise<SilaCulinaryMaterialMappingLine> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialMappingLine>(
      `${BASE}/drafts/${draftId}/ingredients/${encodeURIComponent(ingredientTempRef)}/confirm`,
      req
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to confirm material mapping."));
  }
};

export const bulkAcceptExactMappings = async (
  draftId: string,
  ingredientRefs?: string[]
): Promise<SilaCulinaryMaterialWorkspace> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialWorkspace>(
      `${BASE}/drafts/${draftId}/bulk-accept-exact`,
      { ingredientRefs: ingredientRefs ?? null }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Bulk accept exact matches failed."));
  }
};

export const rejectMaterialMapping = async (
  draftId: string,
  ingredientTempRef: string,
  req: {
    expectedMappingRevision?: number;
    reason?: string | null;
    feedbackType?: string | null;
  }
): Promise<SilaCulinaryMaterialMappingLine> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialMappingLine>(
      `${BASE}/drafts/${draftId}/ingredients/${encodeURIComponent(ingredientTempRef)}/reject`,
      req
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to reject candidate."));
  }
};

export const prepareMaterialRequestDraft = async (
  draftId: string,
  ingredientTempRef: string,
  req: {
    expectedMappingRevision?: number;
    requestType: string;
    proposedDescription: string;
    proposedLongDescription?: string | null;
    proposedMaterialGroup?: string | null;
    proposedBaseUom?: string | null;
    proposedPurchasingUom?: string | null;
    proposedPackSize?: string | null;
    brandPreference?: string | null;
    productSpecification?: string | null;
    storageRequirement?: string | null;
    potentialAllergens?: string[];
    reason?: string | null;
  }
): Promise<SilaCulinaryMaterialMappingLine> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialMappingLine>(
      `${BASE}/drafts/${draftId}/ingredients/${encodeURIComponent(ingredientTempRef)}/request-draft`,
      req
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to prepare Phase 1C material request draft."));
  }
};

export const suggestIngredientSubstitutions = async (
  draftId: string,
  ingredientTempRef: string
): Promise<SilaCulinarySubstitutionResponse> => {
  try {
    const res = await axiosInstance.post<SilaCulinarySubstitutionResponse>(
      `${BASE}/drafts/${draftId}/ingredients/${encodeURIComponent(ingredientTempRef)}/substitutions`
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to generate substitution suggestions."));
  }
};

export const applyIngredientSubstitution = async (
  draftId: string,
  ingredientTempRef: string,
  req: {
    expectedMappingRevision?: number;
    proposedIngredient: string;
    proposedMaterialId?: string | null;
    proposedMaterialCode?: string | null;
    replacementRatio?: number | null;
    reason?: string | null;
  }
): Promise<SilaCulinaryMaterialMappingLine> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialMappingLine>(
      `${BASE}/drafts/${draftId}/ingredients/${encodeURIComponent(ingredientTempRef)}/substitutions/apply`,
      req
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to apply substitution to recipe draft."));
  }
};

export const searchMaterialsForMapping = async (
  q: string,
  propertyId?: string | null,
  limit = 20
): Promise<SilaCulinaryMaterialSearchItem[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialSearchItem[]>(`${BASE}/search`, {
      params: { q, propertyId: propertyId ?? undefined, limit },
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Material Master search failed."));
  }
};

export const getMaterialMappingHistory = async (
  draftId: string,
  ingredientTempRef?: string
): Promise<SilaCulinaryMaterialHistory[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialHistory[]>(
      `${BASE}/drafts/${draftId}/history`,
      { params: { ingredientTempRef: ingredientTempRef || undefined } }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load mapping history."));
  }
};

export const listMaterialAliases = async (
  status?: string,
  search?: string
): Promise<SilaCulinaryMaterialAlias[]> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialAlias[]>(`${BASE}/aliases`, {
      params: { status: status || undefined, search: search || undefined },
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load material aliases."));
  }
};

export const createMaterialAlias = async (req: {
  aliasText: string;
  materialId: string;
  propertyId?: string | null;
  language?: string;
}): Promise<SilaCulinaryMaterialAlias> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialAlias>(`${BASE}/aliases`, req);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to create material alias."));
  }
};

export const decideMaterialAlias = async (
  aliasId: string,
  approve: boolean
): Promise<SilaCulinaryMaterialAlias> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialAlias>(
      `${BASE}/aliases/${aliasId}/decision`,
      null,
      { params: { approve } }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to update alias status."));
  }
};

export const getMaterialIndexStatus = async (): Promise<SilaCulinaryMaterialIndexStatus> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialIndexStatus>(`${BASE}/index/status`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Material Index status."));
  }
};

export const rebuildMaterialIndex = async (
  incrementalOnly = false
): Promise<SilaCulinaryMaterialIndexStatus> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryMaterialIndexStatus>(
      `${BASE}/index/rebuild`,
      null,
      { params: { incrementalOnly } }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Material index synchronization failed."));
  }
};

export const getMaterialDashboard = async (): Promise<SilaCulinaryMaterialDashboard> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryMaterialDashboard>(`${BASE}/dashboard`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Material Intelligence dashboard."));
  }
};

export const getPhase1CMaterialHandover = async (
  draftId: string
): Promise<SilaCulinaryPhase1CHandover> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryPhase1CHandover>(
      `${BASE}/drafts/${draftId}/phase1c-handover`
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Phase 1C handover contract."));
  }
};

export const exportMaterialReport = async (
  draftId: string,
  format: "CSV" | "XLSX" | "PDF",
  statusFilter?: string
): Promise<{ blob: Blob; filename: string }> => {
  try {
    const res = await axiosInstance.get(`${BASE}/drafts/${draftId}/report/export`, {
      params: { format, status: statusFilter || undefined },
      responseType: "blob",
    });
    const disposition = String(res.headers?.["content-disposition"] ?? "");
    const match = disposition.match(/filename="?([^";]+)"?/i);
    const filename =
      match?.[1] ?? `culinary-material-report-${draftId}.${format.toLowerCase()}`;
    return { blob: res.data as Blob, filename };
  } catch (err) {
    throw new Error(await readError(err, "Failed to export material matching report."));
  }
};
