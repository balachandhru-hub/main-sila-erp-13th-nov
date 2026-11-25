import type { SilaMePermissions } from "../silaMePermissions";

/** Presentation lens only — never grants permissions. */
export type LiveInventoryLens =
  | "chef"
  | "store"
  | "fnb"
  | "procurement"
  | "cost"
  | "finance"
  | "gm"
  | "ops";

export interface LensProfile {
  id: LiveInventoryLens;
  label: string;
  priorityHint: string;
  defaultHealth?: string;
  emphasize: string[];
}

const PROFILES: Record<LiveInventoryLens, LensProfile> = {
  chef: {
    id: "chef",
    label: "Kitchen",
    priorityHint: "Ingredient availability and preparation shortages",
    defaultHealth: "LOW",
    emphasize: ["low", "recipe", "expiry"],
  },
  store: {
    id: "store",
    label: "Store",
    priorityHint: "Transfers, receipts, and stock counts",
    emphasize: ["transit", "pending", "count"],
  },
  fnb: {
    id: "fnb",
    label: "F&B",
    priorityHint: "Outlet health and high-risk shortages",
    emphasize: ["low", "pending", "velocity"],
  },
  procurement: {
    id: "procurement",
    label: "Procurement",
    priorityHint: "Reorder and incoming stock",
    defaultHealth: "LOW",
    emphasize: ["low", "po", "incoming"],
  },
  cost: {
    id: "cost",
    label: "Cost Control",
    priorityHint: "Variances, wastage, and adjustments",
    emphasize: ["variance", "adjust", "valuation"],
  },
  finance: {
    id: "finance",
    label: "Finance",
    priorityHint: "Valuation and document linkage",
    emphasize: ["valuation", "grn", "sap"],
  },
  gm: {
    id: "gm",
    label: "Executive",
    priorityHint: "Inventory health and critical exceptions",
    emphasize: ["pending", "critical", "health"],
  },
  ops: {
    id: "ops",
    label: "Operations",
    priorityHint: "Day-to-day stock availability",
    emphasize: ["stock", "transit", "pending"],
  },
};

/**
 * Pick a presentation lens from authenticated permissions.
 * Does not impersonate roles; only changes emphasis.
 */
export const resolveLiveInventoryLens = (can: SilaMePermissions): LensProfile => {
  if (can.manageRecipe && !can.manageErpPosting && !can.approveCount) return PROFILES.chef;
  if (can.requestPurchase && can.manageTransfer && !can.manageRecipe) return PROFILES.procurement;
  if (can.viewAudit && can.approveCount) return PROFILES.cost;
  if (can.viewInvoice && can.viewAudit && !can.manageTransfer) return PROFILES.finance;
  if (can.approveTransfer && can.approveCount && can.manageErpPosting) return PROFILES.gm;
  if (can.viewMenuEngineering && can.approveRecipe) return PROFILES.fnb;
  if (can.manageTransfer || can.manageCount) return PROFILES.store;
  return PROFILES.ops;
};

export const lensOptionsFor = (can: SilaMePermissions): LensProfile[] => {
  const primary = resolveLiveInventoryLens(can);
  const allowed = new Set<LiveInventoryLens>([primary.id, "ops"]);
  if (can.manageRecipe) allowed.add("chef");
  if (can.manageTransfer || can.manageCount) allowed.add("store");
  if (can.viewMenuEngineering) allowed.add("fnb");
  if (can.requestPurchase) allowed.add("procurement");
  if (can.viewAudit || can.approveCount) allowed.add("cost");
  if (can.viewInvoice && can.viewAudit) allowed.add("finance");
  if (can.manageErpPosting && can.approveTransfer) allowed.add("gm");
  return [...allowed].map((id) => PROFILES[id]);
};
