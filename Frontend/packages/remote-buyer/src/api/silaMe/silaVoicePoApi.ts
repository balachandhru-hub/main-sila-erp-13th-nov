import axiosInstance from "../axiosInstance";
import { readError } from "../readError";
import { readSuccessId } from "./readSuccessId";

/* ---------- Purchasing profiles (PO defaults & account assignment) ---------- */

/** ACTIVE | INACTIVE */
export type SilaPurchasingProfileStatus = string;

export interface SilaUserPurchasingProfile {
  id: string;
  buyerId?: string;
  userId: string;
  username?: string | null;
  displayName?: string | null;
  status: SilaPurchasingProfileStatus;
  profileVersion: number;
  isDefault: boolean;
  propertyId?: string | null;
  outletId?: string | null;
  locationId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  storageLocation?: string | null;
  purchasingOrganization?: string | null;
  purchasingGroup?: string | null;
  defaultDeliveryLocation?: string | null;
  accountAssignmentCategory?: string | null;
  costCenter?: string | null;
  glAccount?: string | null;
  materialGroup?: string | null;
  profitCenter?: string | null;
  businessArea?: string | null;
  internalOrder?: string | null;
  wbsElement?: string | null;
  preferredSupplierId?: string | null;
  preferredSupplierCode?: string | null;
  defaultCurrency?: string | null;
  defaultDeliveryLeadTimeDays?: number | null;
  defaultPoDocumentType?: string | null;
  defaultItemCategory?: string | null;
  notes?: string | null;
  dateUpdated?: string;
  /** Alias some list UIs already expect. */
  updatedOn?: string;
}

export interface SilaUserPurchasingProfileWrite {
  userId?: string | null;
  username?: string | null;
  displayName?: string | null;
  isDefault?: boolean;
  propertyId?: string | null;
  outletId?: string | null;
  locationId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  storageLocation?: string | null;
  purchasingOrganization?: string | null;
  purchasingGroup?: string | null;
  defaultDeliveryLocation?: string | null;
  accountAssignmentCategory?: string | null;
  costCenter?: string | null;
  glAccount?: string | null;
  materialGroup?: string | null;
  profitCenter?: string | null;
  businessArea?: string | null;
  internalOrder?: string | null;
  wbsElement?: string | null;
  preferredSupplierId?: string | null;
  preferredSupplierCode?: string | null;
  defaultCurrency?: string | null;
  defaultDeliveryLeadTimeDays?: number | null;
  defaultPoDocumentType?: string | null;
  defaultItemCategory?: string | null;
  notes?: string | null;
}

/** Resolved purchasing context for the signed-in user (Voice PO / mobile). */
export interface SilaPurchasingDefaultsResolved {
  profileId?: string | null;
  profileVersion?: number | null;
  profileFound: boolean;
  isDefault?: boolean;
  propertyId?: string | null;
  outletId?: string | null;
  locationId?: string | null;
  companyCode?: string | null;
  plantCode?: string | null;
  storageLocation?: string | null;
  purchasingOrganization?: string | null;
  purchasingGroup?: string | null;
  defaultDeliveryLocation?: string | null;
  accountAssignmentCategory?: string | null;
  costCenter?: string | null;
  glAccount?: string | null;
  materialGroup?: string | null;
  profitCenter?: string | null;
  businessArea?: string | null;
  internalOrder?: string | null;
  wbsElement?: string | null;
  preferredSupplierId?: string | null;
  preferredSupplierCode?: string | null;
  defaultCurrency?: string | null;
  defaultDeliveryLeadTimeDays?: number | null;
  defaultPoDocumentType?: string | null;
  defaultItemCategory?: string | null;
  inventoryType?: string | null;
  requiresAccountAssignment?: boolean;
  validationErrors?: string[];
  /** Convenience: same as profileFound. */
  resolved?: boolean;
  message?: string | null;
}

/* ---------- Voice PO sessions ---------- */

/** OPEN | REVIEW | CONFIRMED | CREATED | CANCELLED | FAILED */
export type SilaVoicePoSessionStatus = string;

export interface SilaVoicePoDraftLine {
  lineNumber?: number;
  materialId?: string | null;
  materialCode?: string | null;
  description?: string | null;
  /** Display alias used by the mobile panel. */
  materialName?: string | null;
  quantity?: number;
  unitOfMeasure?: string | null;
  /** Display alias for unitOfMeasure. */
  uom?: string | null;
  unitPrice?: number | null;
  supplierId?: string | null;
  supplierCode?: string | null;
  supplierName?: string | null;
  storageLocation?: string | null;
  costCenter?: string | null;
  accountAssignmentCategory?: string | null;
  glAccount?: string | null;
  materialGroup?: string | null;
  inventoryType?: string | null;
  resolved?: boolean;
  resolutionNote?: string | null;
  /** Inverse of resolved for UI highlighting. */
  uncertain?: boolean | null;
}

export interface SilaVoicePoDraft {
  companyCode?: string | null;
  plantCode?: string | null;
  storageLocation?: string | null;
  purchasingOrganization?: string | null;
  purchasingGroup?: string | null;
  currency?: string | null;
  accountAssignmentCategory?: string | null;
  costCenter?: string | null;
  glAccount?: string | null;
  materialGroup?: string | null;
  deliveryDate?: string | null;
  lines: SilaVoicePoDraftLine[];
}

export interface SilaVoicePoClarification {
  code: string;
  message: string;
  field?: string | null;
}

export interface SilaVoicePoCreatedOrder {
  purchaseOrderId: string;
  poNumber?: string | null;
  supplierId?: string;
  supplierName?: string | null;
}

export interface SilaVoicePoTurn {
  id: string;
  turnNumber: number;
  /** USER | AGENT */
  role: string;
  channel?: string | null;
  transcript?: string | null;
  spokenText?: string | null;
  structuredJson?: string | null;
  transcriptionStatus?: string | null;
  confidence?: number | null;
  dateCreated?: string;
}

export interface SilaVoicePoSession {
  id: string;
  sessionNumber: string;
  status: SilaVoicePoSessionStatus;
  userId?: string;
  locationId?: string | null;
  propertyId?: string | null;
  outletId?: string | null;
  purchasingProfileId?: string | null;
  purchasingProfileVersion?: number | null;
  latestTranscript?: string | null;
  draft?: SilaVoicePoDraft | null;
  clarifications?: SilaVoicePoClarification[];
  spokenReply?: string | null;
  validationStatus?: string | null;
  validationErrors?: string[];
  idempotencyKey?: string | null;
  createdPurchaseOrderId?: string | null;
  createdPoNumber?: string | null;
  createdOrders?: SilaVoicePoCreatedOrder[];
  language?: string | null;
  turns?: SilaVoicePoTurn[];
  dateUpdated?: string;
  purchasingContext?: SilaPurchasingDefaultsResolved | null;
}

export interface SilaVoicePoStartRequest {
  locationId?: string | null;
  propertyId?: string | null;
  outletId?: string | null;
  purchasingProfileId?: string | null;
  language?: string | null;
  idempotencyKey?: string | null;
  initialTranscript?: string | null;
}

export interface SilaVoicePoTurnRequest {
  transcript?: string | null;
  /** VOICE | TEXT */
  channel?: string | null;
  language?: string | null;
  confidence?: number | null;
  transcriptionStatus?: string | null;
}

export interface SilaVoicePoConfirmRequest {
  /** Explicit phrase e.g. "confirm purchase order" (yes/ok rejected by backend). */
  confirmationPhrase?: string | null;
  idempotencyKey?: string | null;
}

const PROFILES = "/api/v1/buyer/sila/user-purchasing-profiles";
const VOICE = "/api/v1/buyer/sila/voice-po/sessions";

const list = <T>(value: T[] | null | undefined): T[] => (Array.isArray(value) ? value : []);

const parseJsonObject = <T>(raw: unknown): T | null => {
  if (raw == null) return null;
  if (typeof raw === "object") return raw as T;
  if (typeof raw !== "string" || !raw.trim()) return null;
  try {
    return JSON.parse(raw) as T;
  } catch {
    return null;
  }
};

const normalizeLine = (line: SilaVoicePoDraftLine): SilaVoicePoDraftLine => ({
  ...line,
  materialName: line.materialName ?? line.description ?? null,
  uom: line.uom ?? line.unitOfMeasure ?? null,
  uncertain: line.uncertain ?? (line.resolved === false),
});

const normalizeDraft = (draft: SilaVoicePoDraft | null): SilaVoicePoDraft | null => {
  if (!draft) return null;
  return { ...draft, lines: list(draft.lines).map(normalizeLine) };
};

const normalizeDefaults = (raw: SilaPurchasingDefaultsResolved | null | undefined): SilaPurchasingDefaultsResolved | null => {
  if (!raw) return null;
  const profileFound = Boolean(raw.profileFound ?? raw.resolved);
  return {
    ...raw,
    profileFound,
    resolved: profileFound,
    validationErrors: list(raw.validationErrors),
  };
};

const normalizeSession = (raw: Record<string, unknown>): SilaVoicePoSession => {
  const draft =
    normalizeDraft((raw.draft as SilaVoicePoDraft | null | undefined) ?? null) ??
    normalizeDraft(parseJsonObject<SilaVoicePoDraft>(raw.draftJson));
  const clarifications =
    list(raw.clarifications as SilaVoicePoClarification[] | null | undefined).length > 0
      ? list(raw.clarifications as SilaVoicePoClarification[])
      : list(parseJsonObject<SilaVoicePoClarification[]>(raw.clarificationJson));
  const errorsRaw = raw.validationErrors;
  let validationErrors: string[] = [];
  if (Array.isArray(errorsRaw)) {
    validationErrors = errorsRaw.map(String);
  } else if (typeof errorsRaw === "string" && errorsRaw.trim()) {
    const parsed = parseJsonObject<string[]>(errorsRaw);
    validationErrors = parsed ?? errorsRaw.split(/\n|;/).map((part) => part.trim()).filter(Boolean);
  }
  return {
    id: String(raw.id ?? ""),
    sessionNumber: String(raw.sessionNumber ?? ""),
    status: String(raw.status ?? "OPEN"),
    userId: raw.userId != null ? String(raw.userId) : undefined,
    locationId: (raw.locationId as string | null | undefined) ?? null,
    propertyId: (raw.propertyId as string | null | undefined) ?? null,
    outletId: (raw.outletId as string | null | undefined) ?? null,
    purchasingProfileId: (raw.purchasingProfileId as string | null | undefined) ?? null,
    purchasingProfileVersion: (raw.purchasingProfileVersion as number | null | undefined) ?? null,
    latestTranscript: (raw.latestTranscript as string | null | undefined) ?? null,
    draft,
    clarifications,
    spokenReply: (raw.spokenReply as string | null | undefined) ?? null,
    validationStatus: (raw.validationStatus as string | null | undefined) ?? null,
    validationErrors,
    idempotencyKey: (raw.idempotencyKey as string | null | undefined) ?? null,
    createdPurchaseOrderId: (raw.createdPurchaseOrderId as string | null | undefined) ?? null,
    createdPoNumber: (raw.createdPoNumber as string | null | undefined) ?? null,
    createdOrders: list(raw.createdOrders as SilaVoicePoCreatedOrder[] | null | undefined),
    language: (raw.language as string | null | undefined) ?? null,
    turns: list(raw.turns as SilaVoicePoTurn[] | null | undefined),
    dateUpdated: (raw.dateUpdated as string | undefined) ?? undefined,
    purchasingContext: normalizeDefaults(raw.purchasingContext as SilaPurchasingDefaultsResolved | null | undefined),
  };
};

const asSession = (data: unknown): SilaVoicePoSession => {
  if (!data || typeof data !== "object") {
    throw new Error("Voice PO session response was empty.");
  }
  return normalizeSession(data as Record<string, unknown>);
};

const asProfile = (data: unknown): SilaUserPurchasingProfile => {
  if (!data || typeof data !== "object") {
    throw new Error("Purchasing profile response was empty.");
  }
  const raw = data as SilaUserPurchasingProfile;
  return {
    ...raw,
    updatedOn: raw.updatedOn ?? raw.dateUpdated,
  };
};

/** List purchasing profiles for a user (admin). */
export const listPurchasingProfiles = async (userId: string, activeOnly = false): Promise<SilaUserPurchasingProfile[]> => {
  try {
    const response = await axiosInstance.get<SilaUserPurchasingProfile[]>(PROFILES, {
      params: { userId, activeOnly },
    });
    return list(response.data).map((row) => ({ ...row, updatedOn: row.updatedOn ?? row.dateUpdated }));
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load purchasing profiles."));
  }
};

/** Create or update a purchasing profile. Returns the profile id. */
export const upsertPurchasingProfile = async (
  request: SilaUserPurchasingProfileWrite,
  id?: string,
): Promise<string> => {
  try {
    const response = id
      ? await axiosInstance.put(`${PROFILES}/${id}`, request)
      : await axiosInstance.post(PROFILES, request);
    const fromSuccess = readSuccessId(response.data);
    if (fromSuccess) return fromSuccess;
    if (response.data && typeof response.data === "object" && "id" in response.data) {
      return String((response.data as { id: string }).id || id || "");
    }
    return id || "";
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not save the purchasing profile."));
  }
};

/** Mark a profile as the user's default. */
export const setDefaultPurchasingProfile = async (id: string): Promise<SilaUserPurchasingProfile | void> => {
  try {
    const response = await axiosInstance.post(`${PROFILES}/${id}/default`);
    if (response.data && typeof response.data === "object" && "id" in response.data) {
      return asProfile(response.data);
    }
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not set the default purchasing profile."));
  }
};

/** Soft-deactivate a purchasing profile. */
export const deactivatePurchasingProfile = async (id: string): Promise<void> => {
  try {
    await axiosInstance.delete(`${PROFILES}/${id}`);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not deactivate the purchasing profile."));
  }
};

/** Resolve purchasing defaults for the signed-in user. */
export const resolveMyPurchasingDefaults = async (params?: {
  locationId?: string | null;
  propertyId?: string | null;
  outletId?: string | null;
  plantCode?: string | null;
  profileId?: string | null;
}): Promise<SilaPurchasingDefaultsResolved> => {
  try {
    const response = await axiosInstance.post<SilaPurchasingDefaultsResolved>(`${PROFILES}/resolve`, {
      locationId: params?.locationId || null,
      propertyId: params?.propertyId || null,
      outletId: params?.outletId || null,
      plantCode: params?.plantCode || null,
      profileId: params?.profileId || null,
    });
    return normalizeDefaults(response.data) ?? { profileFound: false, resolved: false, validationErrors: [] };
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not resolve purchasing defaults."));
  }
};

/** Start a Voice PO conversation. */
export const startVoicePoSession = async (request: SilaVoicePoStartRequest = {}): Promise<SilaVoicePoSession> => {
  try {
    const response = await axiosInstance.post(VOICE, request);
    return asSession(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not start the Voice PO session."));
  }
};

/** Send a spoken/typed turn. */
export const sendVoicePoTurn = async (
  sessionId: string,
  request: SilaVoicePoTurnRequest,
): Promise<SilaVoicePoSession> => {
  try {
    const response = await axiosInstance.post(`${VOICE}/${sessionId}/turns`, request);
    return asSession(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not process the Voice PO turn."));
  }
};

/** Explicit confirm → create draft PO (never auto). */
export const confirmVoicePoDraft = async (
  sessionId: string,
  request: SilaVoicePoConfirmRequest = {},
): Promise<SilaVoicePoSession> => {
  try {
    const response = await axiosInstance.post(`${VOICE}/${sessionId}/confirm`, {
      confirmationPhrase: request.confirmationPhrase || "confirm purchase order",
      idempotencyKey: request.idempotencyKey,
    });
    return asSession(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not confirm the Voice PO draft."));
  }
};

/** Cancel an open Voice PO conversation. */
export const cancelVoicePoSession = async (sessionId: string): Promise<SilaVoicePoSession> => {
  try {
    const response = await axiosInstance.post(`${VOICE}/${sessionId}/cancel`);
    return asSession(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not cancel the Voice PO session."));
  }
};

/** Load a Voice PO session by id. */
export const getVoicePoSession = async (sessionId: string): Promise<SilaVoicePoSession> => {
  try {
    const response = await axiosInstance.get(`${VOICE}/${sessionId}`);
    return asSession(response.data);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load the Voice PO session."));
  }
};
