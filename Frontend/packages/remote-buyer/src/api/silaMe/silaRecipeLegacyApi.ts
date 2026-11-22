import axios from "axios";
import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/recipe-legacy";

export interface SilaRecipeLegacyBatchListItem {
  id: string;
  batchNumber: string;
  fileName: string;
  status: string;
  recipeCount: number;
  validCount: number;
  warningCount: number;
  errorCount: number;
  unmappedCount: number;
  duplicateCount: number;
  postedCount: number;
  failedPostCount: number;
  imageAssignedCount: number;
  publishedCount: number;
  uploadedBy: string;
  dateCreated: string;
  dateUpdated: string;
}

export interface SilaRecipeLegacyBatchDetail extends SilaRecipeLegacyBatchListItem {
  storedFileName: string;
  revision: number;
  approvalComment?: string | null;
  approvedBy?: string | null;
  approvedOn?: string | null;
  rejectedBy?: string | null;
  rejectedOn?: string | null;
  validationReportJson?: string | null;
  approvalSnapshotJson?: string | null;
  postingReportJson?: string | null;
  imageReportJson?: string | null;
  publishReportJson?: string | null;
}

export interface SilaRecipeLegacyRecipeListItem {
  id: string;
  legacyRecipeId: string;
  recipeName: string;
  recipeFormat?: string | null;
  productClass?: string | null;
  category?: string | null;
  validationStatus: string;
  errorCount: number;
  warningCount: number;
  postingStatus: string;
  imageStatus?: string | null;
  publishStatus?: string | null;
  createdRecipeId?: string | null;
  createdRecipeCode?: string | null;
  isSelected: boolean;
  sourceRow: number;
}

export interface SilaRecipeLegacyRecipePage {
  items: SilaRecipeLegacyRecipeListItem[];
  total: number;
  index: number;
  limit: number;
}

export interface SilaRecipeLegacyMaterialMap {
  id: string;
  legacyRecipeId: string;
  ingredientLine: number;
  legacyMaterialNumber?: string | null;
  materialNumber?: string | null;
  materialDescription?: string | null;
  mappedMaterialId?: string | null;
  status: string;
}

export interface SilaRecipeLegacyRecipeDetail extends SilaRecipeLegacyRecipeListItem {
  batchId: string;
  description?: string | null;
  subcategory?: string | null;
  yieldQty?: number | null;
  yieldUom?: string | null;
  portionQty?: number | null;
  portionUom?: string | null;
  prepMinutes?: number | null;
  cookMinutes?: number | null;
  servingInstructions?: string | null;
  outletScope?: string | null;
  activeStatus?: string | null;
  legacyReference?: string | null;
  notes?: string | null;
  ingredientsJson?: string | null;
  instructionsJson?: string | null;
  subRecipesJson?: string | null;
  variantsJson?: string | null;
  findingsJson?: string | null;
  materialMappingsJson?: string | null;
  createdVersion?: number | null;
  postingError?: string | null;
  materialMaps: SilaRecipeLegacyMaterialMap[];
}

export interface SilaRecipeLegacyMaterialMapWrite {
  id?: string;
  legacyRecipeId: string;
  ingredientLine: number;
  materialNumber?: string | null;
  mappedMaterialId?: string | null;
  status: string;
}

export interface SilaRecipeLegacyActionResult {
  batchId: string;
  batchNumber: string;
  status: string;
  message: string;
  succeeded: number;
  failed: number;
  skipped: number;
  reportJson?: string | null;
}

export interface SilaRecipeLegacyImageAssign {
  imageId?: string;
  recipeId?: string;
  recipeCode?: string;
  replaceConfirmed?: boolean;
  isPrimary?: boolean;
}

const list = <T>(value: T[] | null | undefined): T[] => (Array.isArray(value) ? value : []);

const call = async <T>(run: () => Promise<{ data: T }>, fallback: string): Promise<T> => {
  try {
    const response = await run();
    return response.data;
  } catch (error: unknown) {
    throw new Error(readError(error, fallback));
  }
};

const readBlobError = async (error: unknown, fallback: string): Promise<string> => {
  if (axios.isAxiosError(error) && error.response?.data instanceof Blob) {
    try {
      const data = JSON.parse(await error.response.data.text()) as { message?: string; description?: string };
      return data.description || data.message || fallback;
    } catch {
      return fallback;
    }
  }
  return readError(error, fallback);
};

const download = async (url: string, fileName: string, fallback: string): Promise<void> => {
  try {
    const response = await axiosInstance.get<Blob>(url, { responseType: "blob" });
    const link = document.createElement("a");
    const href = URL.createObjectURL(response.data);
    link.href = href;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(href);
  } catch (error: unknown) {
    throw new Error(await readBlobError(error, fallback));
  }
};

export const downloadLegacyRecipeTemplate = (): Promise<void> =>
  download(`${BASE}/template`, "recipe-legacy-template.xlsx", "Could not download the legacy recipe template.");

export const uploadLegacyRecipeBatch = async (file: File): Promise<SilaRecipeLegacyBatchDetail> => {
  const form = new FormData();
  form.append("file", file);
  return call(
    () => axiosInstance.post<SilaRecipeLegacyBatchDetail>(`${BASE}/upload`, form, { headers: { "Content-Type": "multipart/form-data" } }),
    "Could not upload the legacy recipe workbook.",
  );
};

export const listLegacyRecipeBatches = async (index = 0, limit = 20): Promise<SilaRecipeLegacyBatchListItem[]> => {
  const result = await call(
    () => axiosInstance.get<SilaRecipeLegacyBatchListItem[]>(`${BASE}/batches`, { params: { index, limit } }),
    "Could not load upload history.",
  );
  return list(result);
};

export const getLegacyRecipeBatch = (batchId: string): Promise<SilaRecipeLegacyBatchDetail> =>
  call(() => axiosInstance.get<SilaRecipeLegacyBatchDetail>(`${BASE}/batches/${batchId}`), "Could not load the upload batch.");

export const listLegacyRecipes = async (
  batchId: string,
  opts?: { status?: string; search?: string; index?: number; limit?: number },
): Promise<SilaRecipeLegacyRecipePage> => {
  const result = await call(
    () =>
      axiosInstance.get<SilaRecipeLegacyRecipePage>(`${BASE}/batches/${batchId}/recipes`, {
        params: {
          status: opts?.status || undefined,
          search: opts?.search || undefined,
          index: opts?.index ?? 0,
          limit: opts?.limit ?? 50,
        },
      }),
    "Could not load staged recipes.",
  );
  return { ...result, items: list(result.items) };
};

export const getLegacyRecipe = async (batchId: string, recipeId: string): Promise<SilaRecipeLegacyRecipeDetail> => {
  const result = await call(
    () => axiosInstance.get<SilaRecipeLegacyRecipeDetail>(`${BASE}/batches/${batchId}/recipes/${recipeId}`),
    "Could not load the staged recipe.",
  );
  return { ...result, materialMaps: list(result.materialMaps) };
};

export const validateLegacyRecipeBatch = (batchId: string): Promise<SilaRecipeLegacyBatchDetail> =>
  call(() => axiosInstance.post<SilaRecipeLegacyBatchDetail>(`${BASE}/batches/${batchId}/validate`), "Could not validate the batch.");

export const downloadLegacyValidationReport = (batchId: string): Promise<void> =>
  download(`${BASE}/batches/${batchId}/validation-report`, `legacy-validation-${batchId}.xlsx`, "Could not download the validation report.");

export const updateLegacyMaterialMaps = (batchId: string, mappings: SilaRecipeLegacyMaterialMapWrite[]): Promise<void> =>
  call(() => axiosInstance.post(`${BASE}/batches/${batchId}/material-maps`, mappings), "Could not save material mappings.").then(() => undefined);

export const submitLegacyBatchApproval = (batchId: string): Promise<SilaRecipeLegacyActionResult> =>
  call(() => axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/submit-approval`), "Could not submit for approval.");

export const decideLegacyBatch = (
  batchId: string,
  decision: "approve" | "reject" | "return",
  comment?: string,
): Promise<SilaRecipeLegacyActionResult> =>
  call(
    () => axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/decide`, { decision, comment }),
    "Could not record the approval decision.",
  );

export const postLegacyBatch = (batchId: string): Promise<SilaRecipeLegacyActionResult> =>
  call(() => axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/post`), "Could not post recipes.");

export const downloadLegacyPostingReport = (batchId: string): Promise<void> =>
  download(`${BASE}/batches/${batchId}/posting-report`, `legacy-posting-${batchId}.xlsx`, "Could not download the posting report.");

export const uploadLegacyRecipeImages = async (batchId: string, files: File[]): Promise<SilaRecipeLegacyActionResult> => {
  const form = new FormData();
  files.forEach((file) => form.append("files", file));
  return call(
    () =>
      axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/images`, form, {
        headers: { "Content-Type": "multipart/form-data" },
      }),
    "Could not upload recipe images.",
  );
};

export const assignLegacyRecipeImages = (
  batchId: string,
  assignments?: SilaRecipeLegacyImageAssign[],
): Promise<SilaRecipeLegacyActionResult> =>
  call(
    () => axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/images/assign`, assignments ?? []),
    "Could not assign recipe images.",
  );

export const downloadLegacyImageReport = (batchId: string): Promise<void> =>
  download(`${BASE}/batches/${batchId}/image-report`, `legacy-images-${batchId}.xlsx`, "Could not download the image report.");

export const publishLegacyBatch = (batchId: string): Promise<SilaRecipeLegacyActionResult> =>
  call(() => axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/publish`), "Could not publish recipes.");

export const downloadLegacyPublishReport = (batchId: string): Promise<void> =>
  download(`${BASE}/batches/${batchId}/publish-report`, `legacy-publish-${batchId}.xlsx`, "Could not download the publish report.");

export const reprocessLegacyFailed = (batchId: string): Promise<SilaRecipeLegacyActionResult> =>
  call(
    () => axiosInstance.post<SilaRecipeLegacyActionResult>(`${BASE}/batches/${batchId}/reprocess-failed`),
    "Could not reprocess failed records.",
  );
