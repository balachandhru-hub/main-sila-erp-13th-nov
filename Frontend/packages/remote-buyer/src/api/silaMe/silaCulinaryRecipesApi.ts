import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/culinary-recipes";

export type CulinaryGenerationMode =
  | "DISH_NAME"
  | "DETAILED_INSTRUCTIONS"
  | "EXISTING_RECIPE"
  | "AVAILABLE_INGREDIENTS"
  | "DOCUMENT"
  | "DISH_IMAGE"
  | "BUDGET";

export type CulinaryScalingMethod =
  | "LINEAR"
  | "FIXED_QUANTITY"
  | "BATCH_BASED"
  | "CHEF_DEFINED_FACTOR"
  | "CHEF_DEFINED_CURVE"
  | "MANUAL_REVIEW";

export interface CulinaryLocationContext {
  locationId: string;
  propertyId: string;
  locationCode: string;
  locationName: string;
  locationType: string;
  outletId: string | null;
  outletName: string | null;
}

export interface CulinaryExistingRecipeRef {
  recipeId: string;
  recipeCode: string;
  name: string;
  category: string | null;
  servingQty: number;
  servingUom: string;
  status: string;
  version: number;
}

export interface CulinaryAvailableStockItem {
  materialId: string;
  materialCode: string;
  description: string;
  onHandQty: number;
  baseUom: string;
}

export interface CulinaryPlannerContext {
  canSelectProvider: boolean;
  operatingMode: string;
  enabledProviders: string[];
  allowExternalAi: boolean;
  promptVersion: string;
  currency: string;
  locations: CulinaryLocationContext[];
  existingRecipes: CulinaryExistingRecipeRef[];
  availableStock: CulinaryAvailableStockItem[];
}

export interface CulinaryRecipeHeader {
  proposed_name: string;
  description: string;
  cuisine: string;
  category: string;
  recipe_type: string;
  base_servings: number;
  serving_size: number;
  serving_uom: string;
  batch_size: number | null;
  expected_yield_qty: number | null;
  expected_yield_uom: string | null;
  prep_duration_minutes: number;
  cook_duration_minutes: number;
  total_duration_minutes: number;
  cooking_method: string;
  difficulty: string;
  dietary_tags: string[];
  potential_allergens: string[];
  cross_contact_warnings: string[];
  storage_guidance: string;
  shelf_life_suggestion: string;
  chef_verification_required: boolean;
  nutrition_status: string;
}

export interface CulinaryIngredientLine {
  temp_ref: string;
  description: string;
  normalized_description: string;
  category: string;
  ingredient_form: string;
  brand_preference: string | null;
  quantity: number;
  recipe_uom: string;
  normalized_uom: string;
  uom_resolved: boolean;
  quantity_basis: string;
  raw_quantity: number | null;
  edible_quantity: number | null;
  yield_percent: number;
  yield_source: string;
  prep_loss_percent: number;
  cook_loss_percent: number;
  scaling_method: string;
  chef_scaling_factor: number | null;
  optional_indicator: boolean;
  preparation_notes: string;
  suggested_alternatives: string[];
  material_mapping_status: string;
  material_reference: string | null;
  provenance: string;
  locked: boolean;
}

export interface CulinaryInstructionStep {
  step_number: number;
  step_title: string;
  instruction: string;
  estimated_duration_minutes: number;
  equipment_suggestion: string;
  dependency_step: number | null;
  quality_checkpoint: string;
  food_safety_verification: string;
  provenance: string;
  locked: boolean;
}

export interface CulinarySubRecipe {
  sub_recipe_ref: string;
  sub_recipe_name: string;
  description: string;
  batch_yield_qty: number;
  batch_yield_uom: string;
  required_qty_in_parent: number;
  required_uom_in_parent: string;
  ingredients: CulinaryIngredientLine[];
  instructions: CulinaryInstructionStep[];
  existing_recipe_reference: string | null;
}

export interface CulinaryStructuredRecipePayload {
  header: CulinaryRecipeHeader;
  ingredients: CulinaryIngredientLine[];
  instructions: CulinaryInstructionStep[];
  sub_recipes: CulinarySubRecipe[];
  assumptions: string[];
  warnings: string[];
  chef_notes: string;
  visually_observed_characteristics: string[];
  inferred_uncertain_items: string[];
  cost_validation_note: string;
}

export interface CulinaryValidationWarning {
  code: string;
  severity: string;
  field: string;
  message: string;
}

export interface CulinaryConversationTurn {
  role: string;
  message: string;
  provider?: string | null;
  model?: string | null;
  timestampUtc: string;
}

export interface CulinaryGenerateRecipeRequest {
  draftId?: string | null;
  generationMode: CulinaryGenerationMode;
  recipeName: string;
  recipeDescription?: string;
  cuisine?: string;
  category?: string;
  recipeType?: string;
  servings: number;
  servingSize: number;
  servingUom: string;
  preparationStyle?: string;
  cookingMethod?: string;
  spiceLevel?: string;
  dietaryPreferences?: string[];
  ingredientsToInclude?: string[];
  ingredientsToExclude?: string[];
  allergensToAvoid?: string[];
  preferredIngredients?: string[];
  targetIngredientBudget?: number | null;
  targetCostPerServing?: number | null;
  targetSellingPrice?: number | null;
  targetFoodCostPercent?: number | null;
  locationId?: string | null;
  sourceRecipeId?: string | null;
  sourceDocumentName?: string | null;
  extractedDocumentText?: string | null;
  imageBase64?: string | null;
  imageMimeType?: string | null;
  additionalInstructions?: string;
  preferredProvider?: string | null;
  generationReference?: string | null;
}

export interface CulinaryRefineRequest {
  instruction: string;
  currentPayload: CulinaryStructuredRecipePayload;
  preferredProvider?: string | null;
}

export interface CulinaryRecipeDiffItem {
  section: string;
  field: string;
  before: string;
  after: string;
}

export interface CulinaryRefineResponse {
  proposedPayload: CulinaryStructuredRecipePayload;
  diffSummary: CulinaryRecipeDiffItem[];
  validationWarnings: CulinaryValidationWarning[];
  provider: string;
  model: string;
  usedFallback: boolean;
  inputTokens: number;
  outputTokens: number;
  estimatedCostUsd: number;
  latencyMs: number;
  summaryMessage: string;
}

export interface CulinaryRegenerateSectionRequest {
  currentPayload: CulinaryStructuredRecipePayload;
  sections: string[];
  lockedFields: string[];
  lockedIngredientRefs: string[];
  chefGuidance?: string;
  preferredProvider?: string | null;
}

export interface CulinaryScaleIngredientV2 {
  temp_ref: string;
  description: string;
  uom: string;
  base_qty: number;
  quantity_basis: string;
  yield_percent: number;
  scaling_method: string;
  chef_scaling_factor: number | null;
}

export interface CulinaryScaleV2Request {
  recipeName: string;
  baseServings: number;
  targetServings: number;
  baseServingSize: number;
  targetServingSize: number;
  prepBufferPercent: number;
  useNonlinearRules: boolean;
  ingredients: CulinaryScaleIngredientV2[];
}

export interface CulinaryScaleV2ResultItem {
  temp_ref: string;
  description: string;
  uom: string;
  scaling_method: string;
  quantity_basis: string;
  base_qty: number;
  scaled_edible_qty: number;
  scaled_raw_qty: number;
  yield_percent: number;
  requires_manual_review: boolean;
  note: string;
}

export interface CulinaryScaleV2Result {
  recipe_name: string;
  base_servings: number;
  target_servings: number;
  linear_factor: number;
  ingredients: CulinaryScaleV2ResultItem[];
}

export interface CulinarySaveDraftRequest {
  expectedRevisionNumber: number;
  status?: string | null;
  changeType?: string;
  changeSummary?: string;
  payload: CulinaryStructuredRecipePayload;
  conversation?: CulinaryConversationTurn[];
  validationWarnings?: CulinaryValidationWarning[];
}

export interface CulinaryRecipeRevision {
  id: string;
  revisionNumber: number;
  changeType: string;
  changeSummary: string;
  provider: string | null;
  model: string | null;
  actorName: string;
  createdOnUtc: string;
  payload: CulinaryStructuredRecipePayload;
}

export interface CulinaryRecipeDraftSummary {
  id: string;
  draftReference: string;
  recipeName: string;
  generationMode: string;
  status: string;
  propertyName: string | null;
  kitchenName: string | null;
  baseServings: number;
  ingredientCount: number;
  provider: string;
  model: string;
  validationStatus: string;
  revisionNumber: number;
  totalTokens: number;
  estimatedCostUsd: number;
  createdByName: string;
  createdOnUtc: string;
  updatedOnUtc: string;
}

export interface CulinaryRecipeDraftDetail extends CulinaryRecipeDraftSummary {
  propertyId: string | null;
  locationId: string | null;
  outletId: string | null;
  outletName: string | null;
  sourceRecipeId: string | null;
  sourceDocumentName: string | null;
  promptVersion: string;
  generationReference: string;
  totalInputTokens: number;
  totalOutputTokens: number;
  payload: CulinaryStructuredRecipePayload;
  validationWarnings: CulinaryValidationWarning[];
  conversation: CulinaryConversationTurn[];
  revisions: CulinaryRecipeRevision[];
}

export interface CulinaryDocumentExtract {
  filename: string;
  file_type: string;
  extracted_text: string;
  line_count: number;
  detected_title: string | null;
  detected_servings: number | null;
  missing_quantities_detected: string[];
  unclear_units_detected: string[];
  warnings: string[];
}

export interface CulinaryDishImagePrepared {
  base64: string;
  mime_type: string;
  width: number;
  height: number;
}

export const getCulinaryPlannerContext = async (
  locationId?: string | null
): Promise<CulinaryPlannerContext> => {
  try {
    const res = await axiosInstance.get<CulinaryPlannerContext>(`${BASE}/context`, {
      params: locationId ? { locationId } : undefined,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Culinary Recipe Planner context."));
  }
};

export const generateCulinaryRecipe = async (
  request: CulinaryGenerateRecipeRequest,
  signal?: AbortSignal
): Promise<CulinaryRecipeDraftDetail> => {
  try {
    const res = await axiosInstance.post<CulinaryRecipeDraftDetail>(`${BASE}/generate`, request, {
      timeout: 120000,
      signal,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Recipe generation failed."));
  }
};

export const refineCulinaryRecipe = async (
  request: CulinaryRefineRequest,
  draftId?: string | null,
  signal?: AbortSignal
): Promise<CulinaryRefineResponse> => {
  try {
    const res = await axiosInstance.post<CulinaryRefineResponse>(`${BASE}/refine`, request, {
      params: draftId ? { draftId } : undefined,
      timeout: 120000,
      signal,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Recipe refinement failed."));
  }
};

export const regenerateCulinaryRecipeSection = async (
  request: CulinaryRegenerateSectionRequest,
  draftId?: string | null,
  signal?: AbortSignal
): Promise<CulinaryRefineResponse> => {
  try {
    const res = await axiosInstance.post<CulinaryRefineResponse>(
      `${BASE}/regenerate-section`,
      request,
      {
        params: draftId ? { draftId } : undefined,
        timeout: 120000,
        signal,
      }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Selective regeneration failed."));
  }
};

export const scaleCulinaryRecipeV2 = async (
  request: CulinaryScaleV2Request,
  draftId?: string | null
): Promise<CulinaryScaleV2Result> => {
  try {
    const res = await axiosInstance.post<CulinaryScaleV2Result>(`${BASE}/scale`, request, {
      params: draftId ? { draftId } : undefined,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Deterministic scaling failed."));
  }
};

export const extractCulinaryRecipeDocument = async (
  file: File
): Promise<CulinaryDocumentExtract> => {
  try {
    const form = new FormData();
    form.append("file", file);
    const res = await axiosInstance.post<CulinaryDocumentExtract>(`${BASE}/extract-document`, form, {
      headers: { "Content-Type": "multipart/form-data" },
      timeout: 60000,
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Recipe document extraction failed."));
  }
};

export const prepareCulinaryDishImage = async (
  file: File
): Promise<CulinaryDishImagePrepared> => {
  try {
    const form = new FormData();
    form.append("file", file);
    const res = await axiosInstance.post<CulinaryDishImagePrepared>(
      `${BASE}/prepare-dish-image`,
      form,
      {
        headers: { "Content-Type": "multipart/form-data" },
        timeout: 60000,
      }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Dish photograph validation failed."));
  }
};

export const cancelCulinaryGeneration = async (generationReference: string): Promise<void> => {
  try {
    await axiosInstance.post(`${BASE}/cancel-generation/${encodeURIComponent(generationReference)}`);
  } catch {
    // Best-effort server cancellation
  }
};

export const listCulinaryRecipeDrafts = async (
  status?: string,
  search?: string
): Promise<CulinaryRecipeDraftSummary[]> => {
  try {
    const res = await axiosInstance.get<CulinaryRecipeDraftSummary[]>(`${BASE}/drafts`, {
      params: {
        ...(status ? { status } : {}),
        ...(search ? { search } : {}),
      },
    });
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load recipe draft history."));
  }
};

export const getCulinaryRecipeDraft = async (
  draftId: string
): Promise<CulinaryRecipeDraftDetail> => {
  try {
    const res = await axiosInstance.get<CulinaryRecipeDraftDetail>(`${BASE}/drafts/${draftId}`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load recipe draft."));
  }
};

export const saveCulinaryRecipeDraft = async (
  draftId: string,
  request: CulinarySaveDraftRequest
): Promise<CulinaryRecipeDraftDetail> => {
  try {
    const res = await axiosInstance.put<CulinaryRecipeDraftDetail>(
      `${BASE}/drafts/${draftId}`,
      request
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to save recipe draft."));
  }
};

export const duplicateCulinaryRecipeDraft = async (
  draftId: string
): Promise<CulinaryRecipeDraftDetail> => {
  try {
    const res = await axiosInstance.post<CulinaryRecipeDraftDetail>(
      `${BASE}/drafts/${draftId}/duplicate`
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to duplicate recipe draft."));
  }
};

export const restoreCulinaryRecipeDraftRevision = async (
  draftId: string,
  revisionNumber: number
): Promise<CulinaryRecipeDraftDetail> => {
  try {
    const res = await axiosInstance.post<CulinaryRecipeDraftDetail>(
      `${BASE}/drafts/${draftId}/restore/${revisionNumber}`
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to restore draft revision."));
  }
};

export const cancelCulinaryRecipeDraft = async (
  draftId: string
): Promise<CulinaryRecipeDraftDetail> => {
  try {
    const res = await axiosInstance.post<CulinaryRecipeDraftDetail>(
      `${BASE}/drafts/${draftId}/cancel`
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Failed to cancel recipe draft."));
  }
};

export const exportCulinaryRecipeDraft = async (
  draftId: string,
  format: "JSON" | "CSV"
): Promise<{ blob: Blob; filename: string }> => {
  try {
    const res = await axiosInstance.get(`${BASE}/drafts/${draftId}/export`, {
      params: { format },
      responseType: "blob",
    });
    const disposition = String(res.headers?.["content-disposition"] ?? "");
    const match = disposition.match(/filename="?([^";]+)"?/i);
    const filename = match?.[1] ?? `culinary-draft-${draftId}.${format.toLowerCase()}`;
    return { blob: res.data as Blob, filename };
  } catch (err) {
    throw new Error(await readError(err, "Failed to export recipe draft."));
  }
};
