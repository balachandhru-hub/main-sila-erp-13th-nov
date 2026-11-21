import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

/** RECEIVED | INVENTORY_DEDUCTED | POSTED | FAILED */
export type SilaPosStatus = string;

/** DONE | FAILED | PENDING | SKIPPED | UNKNOWN */
export type SilaPosStepState = string;

export interface SilaPosRowError {
  row: number;
  message: string;
}

export interface SilaPosImportResult {
  batchId: string;
  batchNumber: string;
  rows: number;
  accepted: number;
  duplicates: number;
  invalid: number;
  processed: number;
  failed: number;
  /** Lines of the batch already deducted or posted before this run. */
  alreadyPosted?: number;
  /** Step 1: lines whose SILA inventory was validated and updated locally. */
  localUpdated?: number;
  /** Step 2: lines whose SAP material document posted. */
  sapPosted?: number;
  /** Step 2: lines whose SAP material document failed. */
  sapFailed?: number;
  /** Lines whose ERP posting outcome is unknown (reconcile under ERP postings). */
  postingUnknown?: number;
  errors: SilaPosRowError[];
}

export interface SilaPosPreviewRow {
  rowNumber: number;
  /** Stored sale transaction id when the row was accepted into the PREVIEW batch. */
  saleTransactionId?: string | null;
  /** READY | INVALID | DUPLICATE | UNMAPPED_POS_CODE | INVALID_OUTLET | INVALID_UOM | RECIPE_NOT_READY */
  status: string;
  businessDate?: string | null;
  transactionId?: string | null;
  lineId?: string | null;
  posCode?: string | null;
  quantity?: string | null;
  uom?: string | null;
  outletCode?: string | null;
  currency?: string | null;
  amount?: string | null;
  outletLocationName?: string | null;
  recipeCode?: string | null;
  recipeVersion?: number | null;
  sourceSystem?: string | null;
  transactionType?: string | null;
  originalTransactionId?: string | null;
  originalLineId?: string | null;
  message?: string | null;
}

export interface SilaPosConsumptionPreviewIngredient {
  materialId: string;
  materialCode?: string | null;
  materialDescription?: string | null;
  quantity: number;
  baseUom?: string | null;
  unitCost?: number | null;
  estimatedCost?: number | null;
  costPending: boolean;
}

export interface SilaPosConsumptionPreviewLine {
  saleTransactionId: string;
  businessDate?: string | null;
  transactionId: string;
  lineId: number;
  posCode: string;
  quantitySold: number;
  uom?: string | null;
  outletCode: string;
  outletLocationName?: string | null;
  plantCode?: string | null;
  storageLocationCode?: string | null;
  salesAmount?: number | null;
  currency?: string | null;
  recipeCode?: string | null;
  recipeVersion?: number | null;
  matchStatus: string;
  message?: string | null;
  ingredientCount: number;
  consumptionCost?: number | null;
  costPending: boolean;
  grossContribution?: number | null;
  foodCostPercent?: number | null;
  grossMarginPercent?: number | null;
  ingredients: SilaPosConsumptionPreviewIngredient[];
}

export interface SilaPosConsumptionPreview {
  batchId: string;
  batchNumber: string;
  lines: SilaPosConsumptionPreviewLine[];
  totalSalesAmount: number;
  totalConsumptionCost: number;
}

export interface SilaPosProcessRequest {
  transactionIds?: string[];
  /** false = deduct SILA stock but do not queue SAP (default from server config). */
  queueErpPosting?: boolean;
}

/** Validation of an uploaded sales file; the valid lines wait in a PREVIEW batch until processed. */
export interface SilaPosPreview {
  batchId: string;
  batchNumber: string;
  fileName: string;
  posSourceId?: string | null;
  posSourceName?: string | null;
  totalRows: number;
  validRows: number;
  invalidRows: number;
  duplicateRows: number;
  unmappedPosCodes: number;
  invalidOutlets: number;
  invalidUom: number;
  recipeNotReady: number;
  readyToProcess: number;
  rows: SilaPosPreviewRow[];
  rowsTruncated: boolean;
}

export interface SilaPosBatch {
  id: string;
  batchNumber: string;
  /** FILE | API */
  source: string;
  fileName?: string | null;
  rows: number;
  accepted: number;
  duplicates: number;
  invalid: number;
  failed: number;
  /** Lines whose stock was deducted (posted to the ERP or not). */
  processed?: number;
  /** Deducted lines whose ERP posting outcome is unknown. */
  postingUnknown?: number;
  uploadedBy: string;
  uploadedByName?: string | null;
  dateCreated: string;
  /** PREVIEW | PROCESSED */
  status: string;
  posSourceId?: string | null;
  posSourceName?: string | null;
  businessDateFrom?: string | null;
  businessDateTo?: string | null;
}

export interface SilaPosTransaction {
  id: string;
  batchId?: string | null;
  batchNumber?: string | null;
  sourceTransactionId: string;
  lineNumber: number;
  businessDate: string;
  outletCode: string;
  outletLocationId?: string | null;
  outletLocationName?: string | null;
  posCode: string;
  recipeId?: string | null;
  recipeCode?: string | null;
  recipeName?: string | null;
  quantitySold: number;
  amount?: number | null;
  uom?: string | null;
  currency?: string | null;
  status: SilaPosStatus;
  /** MATCH | DEDUCT | POST */
  failedStep?: string | null;
  failureMessage?: string | null;
  receivedState: SilaPosStepState;
  deductState: SilaPosStepState;
  postState: SilaPosStepState;
  erpPostingId?: string | null;
  erpPostingStatus?: string | null;
  erpReference?: string | null;
  postedOn?: string | null;
  canReprocess: boolean;
  dateCreated: string;
  /** Recipe version consumed (active version at matching). */
  recipeVersion?: number | null;
  /** Recipe POS item, else its name. */
  menuItem?: string | null;
  /** Inventory consumption transaction number. */
  consumptionTransactionNumber?: string | null;
  locationType?: string | null;
  /** Step 1, SILA inventory: DONE | FAILED | PENDING. */
  step1Status?: string;
  step1Message?: string | null;
  /** Step 2, ERP: DONE | FAILED | PENDING | SKIPPED | UNKNOWN. */
  step2Status?: string;
  step2Message?: string | null;
  erpHttpStatus?: number | null;
  integrationSystem?: string | null;
  movementType?: string | null;
  /** MATCH_FAILED | DEDUCT_FAILED | ERP_FAILED | ERP_UNKNOWN */
  failureCode?: string | null;
}

export interface SilaPosTransactionPage {
  items: SilaPosTransaction[];
  total: number;
  index: number;
  limit: number;
}

export interface SilaPosConsumptionLine {
  id: string;
  transactionNumber: string;
  materialId: string;
  materialCode?: string | null;
  materialDescription?: string | null;
  quantity: number;
  uom: string;
  unitCost?: number | null;
  value?: number | null;
  dateCreated: string;
}

export interface SilaPosTimelineEvent {
  action: string;
  comment?: string | null;
  actorName?: string | null;
  occurredOn: string;
  /** RECEIVE | MATCH | DEDUCT | POST | REPROCESS */
  step?: string | null;
  /** DONE | FAILED | QUEUED | SKIPPED | UNKNOWN | REQUESTED */
  status?: string | null;
}

export interface SilaPosTransactionDetail {
  transaction: SilaPosTransaction;
  posSourceName?: string | null;
  batchStatus?: string | null;
  outletLocationCode?: string | null;
  recipeActiveVersion?: number | null;
  lines: SilaPosConsumptionLine[];
  timeline: SilaPosTimelineEvent[];
  erpMovementType?: string | null;
  erpAttempts: number;
  erpErrorMessage?: string | null;
  erpLastAttemptOn?: string | null;
  /** Last ERP request / response, credentials masked, at most 4000 characters. */
  erpRequest?: string | null;
  erpResponse?: string | null;
  erpHttpStatus?: number | null;
  failureCode?: string | null;
}

export interface SilaPosTransactionFilters {
  status?: string;
  /** PENDING | POSTED | FAILED | SKIPPED | UNKNOWN */
  postingStatus?: string;
  /** yyyy-MM-dd */
  businessDate?: string;
  outletLocationId?: string;
  materialId?: string;
  batchId?: string;
  search?: string;
  index: number;
  limit: number;
}

const BASE = "/api/v1/buyer/sila/pos";

const list = <T>(value: T[] | null | undefined): T[] => (Array.isArray(value) ? value : []);

const call = async <T>(request: () => Promise<{ data: T }>, fallback: string): Promise<T> => {
  try {
    return (await request()).data;
  } catch (error: unknown) {
    throw new Error(readError(error, fallback));
  }
};

const normalizeResult = (result: SilaPosImportResult): SilaPosImportResult => ({ ...result, errors: list(result.errors) });

export const getPosBatches = async (index = 0, limit = 20): Promise<SilaPosBatch[]> =>
  list(await call(() => axiosInstance.get<SilaPosBatch[]>(`${BASE}/batches`, { params: { index, limit } }), "Could not load the sales batches."));

export const getPosBatch = (batchId: string): Promise<SilaPosBatch> =>
  call(() => axiosInstance.get<SilaPosBatch>(`${BASE}/batches/${batchId}`), "Could not load the sales batch.");

/** Step 1: validates a .xlsx or .csv sales file and keeps its valid lines in a PREVIEW batch. */
export const previewPosSales = async (file: File, posSourceId?: string): Promise<SilaPosPreview> => {
  const form = new FormData();
  form.append("file", file);
  const preview = await call(
    () =>
      axiosInstance.post<SilaPosPreview>(`${BASE}/batches`, form, {
        params: posSourceId ? { posSourceId } : undefined,
        headers: { "Content-Type": "multipart/form-data" },
      }),
    "Could not upload the sales file.",
  );
  return { ...preview, rows: list(preview.rows) };
};

/** Read-only recipe explosion and theoretical cost for selected (or all) PREVIEW lines. */
export const previewPosConsumption = async (
  batchId: string,
  body: SilaPosProcessRequest = {},
): Promise<SilaPosConsumptionPreview> => {
  const preview = await call(
    () => axiosInstance.post<SilaPosConsumptionPreview>(`${BASE}/batches/${batchId}/consumption-preview`, body),
    "Could not preview consumption.",
  );
  return {
    ...preview,
    lines: list(preview.lines).map((line) => ({ ...line, ingredients: list(line.ingredients) })),
  };
};

/** Step 2: processes selected (or all) lines of a previewed batch (match, deduct stock, optional ERP queue). */
export const processPosBatch = async (batchId: string, body: SilaPosProcessRequest = {}): Promise<SilaPosImportResult> =>
  normalizeResult(
    await call(
      () => axiosInstance.post<SilaPosImportResult>(`${BASE}/batches/${batchId}/process`, body),
      "Could not process the sales batch.",
    ),
  );

/** Discards a previewed batch that was not processed. */
export const discardPosBatch = async (batchId: string): Promise<void> => {
  await call(() => axiosInstance.delete(`${BASE}/batches/${batchId}`), "Could not discard the upload.");
};

/** Pulls the sales from the POS sales API configured under Integration and processes them at once. */
export const pullPosSales = async (): Promise<SilaPosImportResult> =>
  normalizeResult(await call(() => axiosInstance.post<SilaPosImportResult>(`${BASE}/pull`), "Could not pull the sales from the POS API."));

export const getPosTransactions = async (filters: SilaPosTransactionFilters): Promise<SilaPosTransactionPage> => {
  const params: Record<string, string | number> = { index: filters.index, limit: filters.limit };
  if (filters.status) params.status = filters.status;
  if (filters.postingStatus) params.postingStatus = filters.postingStatus;
  if (filters.businessDate) params.businessDate = filters.businessDate;
  if (filters.outletLocationId) params.outletLocationId = filters.outletLocationId;
  if (filters.materialId) params.materialId = filters.materialId;
  if (filters.batchId) params.batchId = filters.batchId;
  if (filters.search?.trim()) params.search = filters.search.trim();
  const page = await call(() => axiosInstance.get<SilaPosTransactionPage>(`${BASE}/transactions`, { params }), "Could not load the POS transactions.");
  return { ...page, items: list(page?.items) };
};

export const getPosTransaction = async (transactionId: string): Promise<SilaPosTransactionDetail> => {
  const detail = await call(
    () => axiosInstance.get<SilaPosTransactionDetail>(`${BASE}/transactions/${transactionId}`),
    "Could not load the transaction.",
  );
  return { ...detail, lines: list(detail.lines), timeline: list(detail.timeline) };
};

export const reprocessPosTransaction = async (transactionId: string): Promise<void> => {
  await call(() => axiosInstance.post(`${BASE}/transactions/${transactionId}/reprocess`), "Could not reprocess the transaction.");
};
