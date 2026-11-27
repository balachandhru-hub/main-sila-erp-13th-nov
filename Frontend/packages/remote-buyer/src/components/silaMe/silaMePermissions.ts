import type { SilaMeRole } from "./silaMeNav";

/**
 * What the SILA ME screens offer. Live flags come from Identity permission codes.
 * Role templates are a fetch-fail fallback only. They never imply Invoice or Credit Memo from POST_SILA_GRN.
 */
export interface SilaMePermissions {
  manageLocation: boolean;
  manageMasterData: boolean;
  manageTransfer: boolean;
  approveTransfer: boolean;
  postGoodsIssue: boolean;
  postAdjustment: boolean;
  manageCount: boolean;
  /** Start a stock count for a chosen location. Outlet and store managers count, they do not start one. */
  initiateCount: boolean;
  approveCount: boolean;
  requestPurchase: boolean;
  manageRecipe: boolean;
  approveRecipe: boolean;
  approvePrice: boolean;
  managePos: boolean;
  /** VIEW_SILA_RECEIVING — Home / workbench visibility. */
  viewReceiving: boolean;
  /** VIEW_SILA_INVENTORY — inventory Home visibility. */
  viewInventory: boolean;
  postGrn: boolean;
  manageErpPosting: boolean;
  viewAudit: boolean;
  viewInvoice: boolean;
  processInvoice: boolean;
  approveInvoice: boolean;
  viewCreditMemo: boolean;
  processCreditMemo: boolean;
  approveCreditMemo: boolean;
  viewMenuEngineering: boolean;
  manageMenuEngineering: boolean;
  // Section 27 granular Menu Engineering permissions (derived cleanly from Identity codes or role fallback)
  viewMeStudio: boolean;
  createMenu: boolean;
  editDraftMenu: boolean;
  manageMenuItems: boolean;
  managePortionPlans: boolean;
  manageDemandPlans: boolean;
  viewPosSales: boolean;
  managePosMapping: boolean;
  viewMenuPerformance: boolean;
  viewMenuCost: boolean;
  viewMenuMargin: boolean;
  generateMenuPurchaseRec: boolean;
  useAiMenuIntelligence: boolean;
  runMenuSimulation: boolean;
  approveMenu: boolean;
  publishMenu: boolean;
  manageMenuSetup: boolean;
}

const NONE: SilaMePermissions = {
  manageLocation: false, manageMasterData: false, manageTransfer: false, approveTransfer: false, postGoodsIssue: false,
  postAdjustment: false, manageCount: false, initiateCount: false, approveCount: false, requestPurchase: false, manageRecipe: false,
  approveRecipe: false, approvePrice: false, managePos: false, viewReceiving: false, viewInventory: false, postGrn: false,
  manageErpPosting: false, viewAudit: false, viewInvoice: false, processInvoice: false, approveInvoice: false,
  viewCreditMemo: false, processCreditMemo: false, approveCreditMemo: false,
  viewMenuEngineering: false, manageMenuEngineering: false,
  viewMeStudio: false, createMenu: false, editDraftMenu: false, manageMenuItems: false, managePortionPlans: false,
  manageDemandPlans: false, viewPosSales: false, managePosMapping: false, viewMenuPerformance: false, viewMenuCost: false,
  viewMenuMargin: false, generateMenuPurchaseRec: false, useAiMenuIntelligence: false, runMenuSimulation: false,
  approveMenu: false, publishMenu: false, manageMenuSetup: false,
};

const allTrue = (): SilaMePermissions =>
  Object.fromEntries(Object.keys(NONE).map((key) => [key, true])) as unknown as SilaMePermissions;

const invoiceProcess = { viewInvoice: true, processInvoice: true, viewCreditMemo: true, processCreditMemo: true };
const invoiceApprove = { viewInvoice: true, approveInvoice: true, viewCreditMemo: true, approveCreditMemo: true };
const invoiceAll = { ...invoiceProcess, approveInvoice: true, approveCreditMemo: true };

const menuFull: Partial<SilaMePermissions> = {
  viewMenuEngineering: true, manageMenuEngineering: true,
  viewMeStudio: true, createMenu: true, editDraftMenu: true, manageMenuItems: true, managePortionPlans: true,
  manageDemandPlans: true, viewPosSales: true, managePosMapping: true, viewMenuPerformance: true, viewMenuCost: true,
  viewMenuMargin: true, generateMenuPurchaseRec: true, useAiMenuIntelligence: true, runMenuSimulation: true,
  approveMenu: true, publishMenu: true, manageMenuSetup: true,
};

const menuOutletOps: Partial<SilaMePermissions> = {
  viewMenuEngineering: true, manageMenuEngineering: true,
  viewMeStudio: true, createMenu: true, editDraftMenu: true, manageMenuItems: true, managePortionPlans: true,
  manageDemandPlans: true, viewPosSales: true, managePosMapping: true, viewMenuPerformance: true, viewMenuCost: true,
  viewMenuMargin: false, generateMenuPurchaseRec: true, useAiMenuIntelligence: true, runMenuSimulation: true,
  approveMenu: false, publishMenu: false, manageMenuSetup: false,
};

const menuViewer: Partial<SilaMePermissions> = {
  viewMenuEngineering: true,
  viewMeStudio: true, viewPosSales: true, viewMenuPerformance: true, useAiMenuIntelligence: true, runMenuSimulation: true,
};

const menuCostController: Partial<SilaMePermissions> = {
  ...menuFull,
  manageMenuSetup: false,
};

const BY_ROLE: Record<SilaMeRole, SilaMePermissions> = {
  admin: allTrue(),
  "store-manager": {
    ...NONE, manageTransfer: true, approveTransfer: true, postGoodsIssue: true, postAdjustment: true, manageCount: true,
    approveCount: true, requestPurchase: true, manageRecipe: true, approveRecipe: true, approvePrice: true,
    viewReceiving: true, viewInventory: true, postGrn: true, manageErpPosting: true, ...invoiceAll,
    ...menuFull, approveMenu: false, publishMenu: false,
  },
  "outlet-manager": {
    ...NONE, manageTransfer: true, approveTransfer: true, postAdjustment: true, manageCount: true, requestPurchase: true,
    manageRecipe: true, approveRecipe: true, approvePrice: true, viewInventory: true, viewReceiving: true,
    viewInvoice: true, viewCreditMemo: true, approveInvoice: true, approveCreditMemo: true,
    ...menuOutletOps,
  },
  "buyer-user": {
    ...NONE, manageTransfer: true, manageCount: true, approveRecipe: true, approvePrice: true,
    viewReceiving: true, viewInventory: true, postGrn: true, ...invoiceAll,
    ...menuViewer,
  },
  "cost-controller": {
    ...NONE, manageTransfer: true, initiateCount: true, manageCount: true, approveCount: true, approveRecipe: true, approvePrice: true,
    viewReceiving: true, viewInventory: true, postGrn: true, viewAudit: true, ...invoiceAll,
    ...menuCostController,
  },
  "fnb-director": {
    ...NONE, manageTransfer: true, manageCount: true, initiateCount: true, approveRecipe: true, approvePrice: true,
    viewReceiving: true, viewInventory: true, ...invoiceProcess,
    ...menuFull,
  },
  "finance-manager": {
    ...NONE, viewAudit: true, ...invoiceApprove,
    viewMenuEngineering: true, viewMeStudio: true, viewMenuCost: true, viewMenuMargin: true, viewMenuPerformance: true,
    approveMenu: true, publishMenu: true,
  },
  "ap-processor": {
    ...NONE, ...invoiceProcess,
  },
};

const CODE_FLAGS: [keyof SilaMePermissions, string[]][] = [
  ["manageLocation", ["MANAGE_SILA_LOCATION"]],
  ["manageMasterData", ["MANAGE_SILA_MASTER_DATA"]],
  ["manageTransfer", ["MANAGE_SILA_TRANSFER"]],
  ["approveTransfer", ["APPROVE_SILA_TRANSFER"]],
  ["postGoodsIssue", ["POST_SILA_GOODS_ISSUE"]],
  ["postAdjustment", ["POST_SILA_ADJUSTMENT"]],
  ["manageCount", ["MANAGE_SILA_STOCK_COUNT"]],
  ["initiateCount", ["MANAGE_SILA_STOCK_COUNT"]],
  ["approveCount", ["APPROVE_SILA_STOCK_COUNT"]],
  ["requestPurchase", ["REQUEST_SILA_PURCHASE"]],
  ["manageRecipe", ["MANAGE_SILA_RECIPE", "VIEW_SILA_RECIPE"]],
  ["approveRecipe", ["APPROVE_SILA_RECIPE"]],
  ["approvePrice", ["APPROVE_SILA_PRICE"]],
  ["managePos", ["MANAGE_SILA_POS"]],
  ["viewReceiving", ["VIEW_SILA_RECEIVING"]],
  ["viewInventory", ["VIEW_SILA_INVENTORY"]],
  ["postGrn", ["POST_SILA_GRN"]],
  ["manageErpPosting", ["MANAGE_SILA_ERP_POSTING"]],
  ["viewAudit", ["VIEW_SILA_AUDIT"]],
  ["viewInvoice", ["VIEW_SILA_INVOICE"]],
  ["processInvoice", ["PROCESS_SILA_INVOICE"]],
  ["approveInvoice", ["APPROVE_SILA_INVOICE"]],
  ["viewCreditMemo", ["VIEW_SILA_CREDIT_MEMO"]],
  ["processCreditMemo", ["PROCESS_SILA_CREDIT_MEMO"]],
  ["approveCreditMemo", ["APPROVE_SILA_CREDIT_MEMO"]],
  ["viewMenuEngineering", ["VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING", "VIEW_SILA_ME_STUDIO"]],
  ["manageMenuEngineering", ["MANAGE_SILA_MENU_ENGINEERING"]],
  ["viewMeStudio", ["VIEW_SILA_ME_STUDIO", "VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["createMenu", ["CREATE_SILA_MENU", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["editDraftMenu", ["EDIT_SILA_DRAFT_MENU", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["manageMenuItems", ["MANAGE_SILA_MENU_ITEMS", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["managePortionPlans", ["MANAGE_SILA_PORTION_PLANS", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["manageDemandPlans", ["MANAGE_SILA_DEMAND_PLANS", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["viewPosSales", ["VIEW_SILA_POS_SALES", "VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["managePosMapping", ["MANAGE_SILA_POS_MAPPING", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["viewMenuPerformance", ["VIEW_SILA_MENU_PERFORMANCE", "VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["viewMenuCost", ["VIEW_SILA_MENU_COST", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["viewMenuMargin", ["VIEW_SILA_MENU_MARGIN", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["generateMenuPurchaseRec", ["GENERATE_SILA_MENU_PURCHASE_REC", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["useAiMenuIntelligence", ["USE_SILA_AI_MENU_INTELLIGENCE", "VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["runMenuSimulation", ["RUN_SILA_MENU_SIMULATION", "VIEW_SILA_MENU_ENGINEERING", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["approveMenu", ["APPROVE_SILA_MENU", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["publishMenu", ["PUBLISH_SILA_MENU", "MANAGE_SILA_MENU_ENGINEERING"]],
  ["manageMenuSetup", ["MANAGE_SILA_MENU_SETUP", "MANAGE_SILA_MENU_ENGINEERING"]],
];

export const silaMePermissions = (role: SilaMeRole): SilaMePermissions => BY_ROLE[role] ?? NONE;

/** Map Identity feature keys. Unknown codes are ignored. POST_SILA_GRN does not grant invoice or credit memo. */
export const silaMePermissionsFromCodes = (codes: string[], fallback: SilaMePermissions = NONE): SilaMePermissions => {
  const set = new Set(codes.map((code) => code.toUpperCase()));
  const next = { ...NONE };
  for (const [flag, keys] of CODE_FLAGS) {
    next[flag] = keys.some((key) => set.has(key));
  }
  if (codes.length === 0) return fallback;
  // Granular cost/margin restriction: if explicit VIEW_SILA_MENU_ENGINEERING without MANAGE or VIEW_SILA_MENU_MARGIN,
  // fallback only grants viewMeStudio/viewMenuPerformance unless fallback allows margins.
  if (set.has("VIEW_SILA_MENU_ENGINEERING") && !set.has("MANAGE_SILA_MENU_ENGINEERING")) {
    if (!set.has("VIEW_SILA_MENU_COST")) next.viewMenuCost = fallback.viewMenuCost;
    if (!set.has("VIEW_SILA_MENU_MARGIN")) next.viewMenuMargin = fallback.viewMenuMargin;
  }
  return next;
};
