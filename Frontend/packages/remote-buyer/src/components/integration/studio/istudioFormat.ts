import type {
  IntegrationConfiguration,
  SapMovementDirection,
  SapMovementProfile,
  SapProfileActivationPolicy,
} from "../../../api/integrationsApi";
import { statusLabel } from "../integrationFormat";

export const directionChipClass = (direction: SapMovementDirection): string => {
  switch (direction) {
    case "OUT": return "istudio-chip istudio-chip--out";
    case "IN": return "istudio-chip istudio-chip--in";
    case "TRANSFER": return "istudio-chip istudio-chip--transfer";
    case "READ": return "istudio-chip istudio-chip--read";
    case "CANCEL": return "istudio-chip istudio-chip--cancel";
    default: return "istudio-chip istudio-chip--muted";
  }
};

export const directionLabel = (direction: SapMovementDirection): string => {
  switch (direction) {
    case "OUT": return "Stock Out";
    case "IN": return "Stock In";
    case "TRANSFER": return "Transfer";
    case "READ": return "Read";
    case "CANCEL": return "Cancel";
    default: return direction;
  }
};

export const activationLabel = (
  policy: SapProfileActivationPolicy,
  configured?: IntegrationConfiguration | null,
): { text: string; className: string } => {
  if (policy === "blocked_unverified") {
    return { text: "Blocked — SAP verification required", className: "istudio-chip istudio-chip--blocked" };
  }
  if (policy === "unconfigured") {
    return { text: "Unconfigured", className: "istudio-chip istudio-chip--muted" };
  }
  if (!configured) {
    return { text: "Available — not configured", className: "istudio-chip istudio-chip--warn" };
  }
  if (policy === "validation_required" && configured.status === "ACTIVE") {
    return { text: "Active — validate mapping", className: "istudio-chip istudio-chip--warn" };
  }
  if (policy === "validation_required") {
    return { text: "Validation required", className: "istudio-chip istudio-chip--warn" };
  }
  if (configured.status === "ACTIVE") {
    return { text: "Active", className: "istudio-chip istudio-chip--ok" };
  }
  return { text: statusLabel(configured.status), className: "istudio-chip istudio-chip--muted" };
};

export const profileStatusSummary = (
  profile: SapMovementProfile,
  configured?: IntegrationConfiguration | null,
): string => {
  if (profile.activationPolicy === "blocked_unverified") return "Not production-ready";
  if (!configured) return "Configure connection";
  return statusLabel(configured.status);
};

export const CONNECTED_STATUSES = ["TESTED", "ACTIVE", "INACTIVE"];

export const isInventorySystem = (row: IntegrationConfiguration): boolean => {
  const system = (row.systemName || "").trim().toUpperCase();
  const name = (row.name || "").trim().toUpperCase();
  return system.includes("FIVE_S4_INVENTORY")
    || system.includes("SAP")
    || name.includes("FIVE")
    || name.includes("POS_")
    || name.includes("MATERIAL_DOCUMENT")
    || row.processType === "UPDATE_STOCK"
    || row.processType === "POST_GOODS_MOVEMENT"
    || row.processType === "POST_STOCK_ISSUE"
    || row.processType === "POST_POSITIVE_ADJ"
    || row.processType === "POST_TRANSFER_311"
    || row.processType === "POST_TRANSFER_301"
    || row.processType === "POST_POS_REVERSAL"
    || row.processType === "POST_GRN"
    || row.processType === "POST_GRN_REVERSAL"
    || row.processType === "GET_MATERIAL_DOCUMENT"
    || row.processType === "POST_MATERIAL_DOCUMENT_CANCEL";
};
