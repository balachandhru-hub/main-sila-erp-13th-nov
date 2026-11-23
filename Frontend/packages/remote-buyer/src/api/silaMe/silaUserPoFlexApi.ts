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

export const listPoFlexConfigs = async (
  userId: string,
  configType = "PO_CREATE",
  activeOnly = true,
): Promise<SilaUserPoFlexConfig[]> => {
  try {
    const response = await axiosInstance.get<SilaUserPoFlexConfig[]>(BASE, {
      params: { userId, configType, activeOnly },
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
  try {
    const response = id
      ? await axiosInstance.put<SilaUserPoFlexConfig>(`${BASE}/${id}`, request)
      : await axiosInstance.post<SilaUserPoFlexConfig>(BASE, request);
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
