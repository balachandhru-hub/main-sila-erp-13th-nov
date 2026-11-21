import axiosInstance from "../axiosInstance";
import { readError } from "../readError";
import { readSuccessId } from "./readSuccessId";

export type GoodsIssueCostCenterScope = "PLANT" | "STORAGE";

export interface SilaGoodsIssueCostCenter {
  id: string;
  propertyId: string;
  propertyName: string;
  companyCode: string;
  plantCode: string;
  scope: GoodsIssueCostCenterScope | string;
  storageLocationCode: string;
  costCenter: string;
  status: string;
  dateUpdated?: string;
}

export interface SilaGoodsIssueCostCenterWrite {
  propertyId: string;
  scope: GoodsIssueCostCenterScope;
  storageLocationCode?: string | null;
  costCenter: string;
}

const BASE = "/api/v1/buyer/sila/recipe-setup/goods-issue-cost-centers";

export const getGoodsIssueCostCenters = async (scope?: string): Promise<SilaGoodsIssueCostCenter[]> => {
  try {
    const response = await axiosInstance.get<SilaGoodsIssueCostCenter[]>(BASE, {
      params: scope ? { scope } : undefined,
    });
    return Array.isArray(response.data) ? response.data : [];
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not load Goods Issue cost centers."));
  }
};

export const saveGoodsIssueCostCenter = async (
  request: SilaGoodsIssueCostCenterWrite,
  id?: string,
): Promise<string> => {
  try {
    const response = id
      ? await axiosInstance.put(`${BASE}/${id}`, request)
      : await axiosInstance.post(BASE, request);
    return readSuccessId(response.data) || id || "";
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not save the Goods Issue cost center."));
  }
};

export const deleteGoodsIssueCostCenter = async (id: string): Promise<void> => {
  try {
    await axiosInstance.delete(`${BASE}/${id}`);
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not delete the Goods Issue cost center."));
  }
};
