import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/user-po-flex-configs";

export type SilaPoFlexConfigType = "PO_CREATE" | string;

export interface SilaUserPoFlexParam {
  id?: string | null;
  paramKey: string;
  paramValue?: string | null;
  sortOrder?: number;
}

export interface SilaUserPoFlexCondition {
  id?: string | null;
  whenParam: string;
  whenOperator?: "EQ" | "NE" | "CONTAINS" | string;
  whenValue?: string | null;
  thenParam: string;
  thenValue?: string | null;
  sortOrder?: number;
}

export interface SilaUserPoFlexConfig {
  id: string;
  buyerId?: string;
  userId: string;
  username?: string | null;
  displayName?: string | null;
  configType: SilaPoFlexConfigType;
  name?: string | null;
  status: string;
  configVersion: number;
  isDefault: boolean;
  notes?: string | null;
  parameters: SilaUserPoFlexParam[];
  conditions: SilaUserPoFlexCondition[];
  dateUpdated?: string;
}

export interface SilaUserPoFlexConfigWrite {
  userId?: string | null;
  username?: string | null;
  displayName?: string | null;
  configType?: SilaPoFlexConfigType;
  name?: string | null;
  isDefault?: boolean;
  notes?: string | null;
  parameters: SilaUserPoFlexParam[];
  conditions: SilaUserPoFlexCondition[];
}

const list = <T>(value: T[] | null | undefined): T[] => (Array.isArray(value) ? value : []);

/** ASP.NET Guid? rejects "" — only send well-formed GUIDs (or omit). */
const GUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const asGuidOrNull = (value?: string | null): string | null => {
  const trimmed = (value ?? "").trim();
  return GUID_RE.test(trimmed) ? trimmed : null;
};

/** Build a body with no empty-string Guid fields (ASP.NET Guid? rejects ""). */
const sanitizeWrite = (request: SilaUserPoFlexConfigWrite): SilaUserPoFlexConfigWrite => {
  const userId = asGuidOrNull(request.userId);
  const body: SilaUserPoFlexConfigWrite = {
    username: request.username ?? null,
    displayName: request.displayName ?? null,
    configType: request.configType || "PO_CREATE",
    name: request.name ?? null,
    isDefault: Boolean(request.isDefault),
    notes: request.notes ?? null,
    parameters: list(request.parameters).map((row) => {
      const id = asGuidOrNull(row.id);
      return {
        paramKey: row.paramKey,
        paramValue: row.paramValue ?? null,
        sortOrder: row.sortOrder ?? 0,
        ...(id ? { id } : {}),
      };
    }),
    conditions: list(request.conditions).map((row) => {
      const id = asGuidOrNull(row.id);
      return {
        whenParam: row.whenParam,
        whenOperator: row.whenOperator || "EQ",
        whenValue: row.whenValue ?? null,
        thenParam: row.thenParam,
        thenValue: row.thenValue ?? null,
        sortOrder: row.sortOrder ?? 0,
        ...(id ? { id } : {}),
      };
    }),
  };
  if (userId) {
    body.userId = userId;
  }
  return body;
};

export const listPoFlexConfigs = async (
  userId: string,
  configType = "PO_CREATE",
  activeOnly = true,
): Promise<SilaUserPoFlexConfig[]> => {
  const safeUserId = asGuidOrNull(userId);
  if (!safeUserId) {
    throw new Error("Enter a valid User ID (GUID) to load configs.");
  }
  try {
    const response = await axiosInstance.get<SilaUserPoFlexConfig[]>(BASE, {
      params: { userId: safeUserId, configType, activeOnly },
    });
    return list(response.data).map((row) => ({
      ...row,
      parameters: list(row.parameters),
      conditions: list(row.conditions),
    }));
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load PO defaults configs."));
  }
};

export const upsertPoFlexConfig = async (
  request: SilaUserPoFlexConfigWrite,
  id?: string,
): Promise<SilaUserPoFlexConfig> => {
  const payload = sanitizeWrite(request);
  if (!payload.userId) {
    throw new Error("A valid User ID (GUID) is required to save PO defaults.");
  }
  const safeId = asGuidOrNull(id);
  try {
    const response = safeId
      ? await axiosInstance.put<SilaUserPoFlexConfig>(`${BASE}/${safeId}`, payload)
      : await axiosInstance.post<SilaUserPoFlexConfig>(BASE, payload);
    const data = response.data;
    return {
      ...data,
      parameters: list(data?.parameters),
      conditions: list(data?.conditions),
    };
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not save the PO defaults config."));
  }
};

export const deactivatePoFlexConfig = async (id: string): Promise<void> => {
  try {
    await axiosInstance.delete(`${BASE}/${id}`);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not deactivate the PO defaults config."));
  }
};
