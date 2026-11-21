import type { SilaInventoryHome } from "../../../api/silaMe/silaInventoryApi";
import type { SilaAlert } from "../../../api/silaMe/silaStockCountApi";

export interface InventoryPriority {
  id: string;
  title: string;
  detail: string;
  actionLabel: string;
  /** Nav key or special action */
  action: "LOW" | "TRANSIT" | "PENDING" | "ALERTS" | "RECIPE" | "TRANSFERS" | "COUNTS";
  tone: "critical" | "warning" | "info";
}

/** Deterministic priorities from home KPIs + alerts — never auto-posts stock. */
export const buildInventoryPriorities = (
  home: SilaInventoryHome | null,
  alerts: SilaAlert[] | null,
): InventoryPriority[] => {
  const items: InventoryPriority[] = [];

  if ((home?.lowStockItems ?? 0) > 0) {
    items.push({
      id: "low-stock",
      title: `${home!.lowStockItems} material${home!.lowStockItems === 1 ? "" : "s"} below minimum`,
      detail: "Review low-stock items and request transfer or replenishment.",
      actionLabel: "View low stock",
      action: "LOW",
      tone: "warning",
    });
  }

  if ((home?.inTransitToReceive ?? 0) > 0) {
    items.push({
      id: "in-transit",
      title: `${home!.inTransitToReceive} incoming transfer${home!.inTransitToReceive === 1 ? "" : "s"}`,
      detail: "Stock dispatched but not yet received at this location.",
      actionLabel: "Open transfers",
      action: "TRANSIT",
      tone: "info",
    });
  }

  if ((home?.pendingActions ?? 0) > 0 || (home?.pendingTransferApprovals ?? 0) > 0) {
    const pending = home?.pendingActions ?? home?.pendingTransferApprovals ?? 0;
    items.push({
      id: "pending",
      title: `${pending} pending action${pending === 1 ? "" : "s"}`,
      detail: "Approvals, alerts, or open counts need attention.",
      actionLabel: "Review",
      action: "PENDING",
      tone: "critical",
    });
  }

  if ((home?.openStockCounts ?? 0) > 0) {
    items.push({
      id: "counts",
      title: `${home!.openStockCounts} open stock count${home!.openStockCounts === 1 ? "" : "s"}`,
      detail: "Complete or review physical inventory sessions.",
      actionLabel: "Stock counts",
      action: "COUNTS",
      tone: "info",
    });
  }

  for (const alert of (alerts ?? []).slice(0, 3)) {
    if (items.length >= 3) break;
    if (items.some((item) => item.id === `alert-${alert.id}`)) continue;
    items.push({
      id: `alert-${alert.id}`,
      title: alert.title || alert.alertType,
      detail: alert.message || alert.recommendedAction || "Open alerts for details.",
      actionLabel: alert.materialId ? "View material" : "Open alerts",
      action: "ALERTS",
      tone: alert.severity === "CRITICAL" || alert.severity === "HIGH" ? "critical" : "warning",
    });
  }

  return items.slice(0, 3);
};
