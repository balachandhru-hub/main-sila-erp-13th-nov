import React, { useEffect, useMemo, useState } from "react";
import type { SilaMePermissions } from "../silaMePermissions";
import SilaStockHealth from "./SilaStockHealth";
import SilaRecipeAvailability from "./SilaRecipeAvailability";
import SilaInventoryAnalytics from "./SilaInventoryAnalytics";
import SilaTransfers from "../movements/SilaTransfers";
import SilaGoodsIssue from "../movements/SilaGoodsIssue";
import SilaStockAdjustments from "../movements/SilaStockAdjustments";
import SilaStockCounts from "../stockCount/SilaStockCounts";
import SilaShortageEnquiries from "../stockCount/SilaShortageEnquiries";
import SilaAlerts from "../stockCount/SilaAlerts";
import SilaShortageReport from "../control/SilaShortageReport";
import SilaPhysicalInventoryRequests from "../control/SilaPhysicalInventoryRequests";
import SilaRecommendations from "../purchasing/SilaRecommendations";
import "../silaMeTheme.css";
import "../recipes/SilaRecipes.css";
import "../pos/SilaPosStudio.css";
import "./SilaInventoryStudio.css";

export type InventoryStudioTab =
  | "overview"
  | "stock-health"
  | "recipe-availability"
  | "analytics"
  | "transfers"
  | "goods-issue"
  | "adjustments"
  | "stock-counts"
  | "enquiries"
  | "shortage-report"
  | "physical-inventory"
  | "alerts"
  | "recommendations";

const TAB_FROM_NAV: Record<string, InventoryStudioTab> = {
  silaInventoryStudio: "overview",
  silaStockHealth: "stock-health",
  silaRecipeAvailability: "recipe-availability",
  silaInventoryAnalytics: "analytics",
  silaTransfers: "transfers",
  silaGoodsIssue: "goods-issue",
  silaAdjustments: "adjustments",
  silaStockCounts: "stock-counts",
  silaEnquiries: "enquiries",
  silaShortageReport: "shortage-report",
  silaPhysicalInventory: "physical-inventory",
  silaAlerts: "alerts",
  silaRecommendations: "recommendations",
};

export const inventoryStudioTabFromNav = (navKey: string): InventoryStudioTab =>
  TAB_FROM_NAV[navKey] ?? "overview";

interface SilaInventoryStudioProps {
  permissions: SilaMePermissions;
  showRecommendations?: boolean;
  /** Deep-link tab when opened from a legacy inventory nav key. */
  initialTab?: InventoryStudioTab;
  initialCountId?: string | null;
  /** Deep-link open ids from Inventory Transactions (ITO / ADJ / GI). */
  initialTransferId?: string | null;
  initialAdjustmentId?: string | null;
  initialGoodsIssueId?: string | null;
  onOpenLive?: (health?: string) => void;
  onOpenCount?: (stockCountId: string) => void;
  onOpenPhysicalInventory?: () => void;
  readOnlyRecommendations?: boolean;
}

/**
 * Inventory Studio — Recipe/POS Studio shell hosting inventory operations
 * (health, transfers, counts, alerts, etc.) while Dashboard / Live Inventory /
 * Inventory Transactions stay as top-level Inventory menu entries.
 */
const SilaInventoryStudio: React.FC<SilaInventoryStudioProps> = ({
  permissions: can,
  showRecommendations = false,
  initialTab = "overview",
  initialCountId = null,
  initialTransferId = null,
  initialAdjustmentId = null,
  initialGoodsIssueId = null,
  onOpenLive,
  onOpenCount,
  onOpenPhysicalInventory,
  readOnlyRecommendations = false,
}) => {
  const tabs = useMemo(() => {
    const items: { key: InventoryStudioTab; label: string }[] = [
      { key: "overview", label: "Overview" },
      { key: "stock-health", label: "Stock Health" },
      { key: "recipe-availability", label: "Recipe Availability" },
      { key: "analytics", label: "Analytics" },
      { key: "transfers", label: "Internal Transfers" },
    ];
    if (can.postGoodsIssue) items.push({ key: "goods-issue", label: "Goods Issue" });
    if (can.postAdjustment) items.push({ key: "adjustments", label: "Waste & Adjustments" });
    items.push(
      { key: "stock-counts", label: "Stock Count" },
      { key: "enquiries", label: "Shortage & Enquiries" },
    );
    if (can.approveCount) items.push({ key: "shortage-report", label: "Shortage Report" });
    items.push(
      { key: "physical-inventory", label: "Physical Inventory" },
      { key: "alerts", label: "Alerts" },
    );
    if (showRecommendations) items.push({ key: "recommendations", label: "Recommendations" });
    return items;
  }, [can.approveCount, can.postAdjustment, can.postGoodsIssue, showRecommendations]);

  const [tab, setTab] = useState<InventoryStudioTab>(initialTab);

  useEffect(() => {
    setTab(initialTab);
  }, [initialTab]);

  useEffect(() => {
    if (!tabs.some((item) => item.key === tab)) {
      setTab("overview");
    }
  }, [tab, tabs]);

  return (
    <div className="sila-me srec-page rstudio-page pstudio-page istudio-inv-page" data-testid="inventory-studio">
      <header className="pstudio-hero istudio-inv-hero istudio-inv-hero--compact">
        <h1 className="pstudio-hero__title">Inventory Studio</h1>
      </header>

      <nav className="pstudio-tabs" role="tablist" aria-label="Inventory Studio">
        {tabs.map((item) => (
          <button
            key={item.key}
            type="button"
            role="tab"
            id={`istudio-inv-tab-${item.key}`}
            aria-controls={`istudio-inv-panel-${item.key}`}
            className="pstudio-tab"
            aria-selected={tab === item.key}
            onClick={() => setTab(item.key)}
          >
            {item.label}
          </button>
        ))}
      </nav>

      <div
        role="tabpanel"
        id={`istudio-inv-panel-${tab}`}
        aria-labelledby={`istudio-inv-tab-${tab}`}
        className="srec-stack istudio-inv-body"
      >
        {tab === "overview" && (
          <section className="pstudio-panel">
            <h2 className="pstudio-panel__title">Operations overview</h2>
            <p className="pstudio-panel__hint">
              Open a tab for stock health, transfers, goods issue, waste, counts, shortages, physical inventory, or alerts.
              Use Live Inventory from the Inventory menu for day-to-day availability and ITO creation.
            </p>
            <div className="istudio-inv-overview-grid">
              {tabs.filter((item) => item.key !== "overview").map((item) => (
                <button
                  key={item.key}
                  type="button"
                  className="istudio-inv-card"
                  onClick={() => setTab(item.key)}
                >
                  <strong>{item.label}</strong>
                  <span>Open in Studio</span>
                </button>
              ))}
            </div>
          </section>
        )}

        {tab === "stock-health" && (
          <SilaStockHealth
            onOpenLive={(health) => {
              onOpenLive?.(health);
            }}
          />
        )}
        {tab === "recipe-availability" && <SilaRecipeAvailability />}
        {tab === "analytics" && <SilaInventoryAnalytics />}
        {tab === "transfers" && (
          <SilaTransfers canApprove={can.approveTransfer} initialTransferId={initialTransferId} />
        )}
        {tab === "goods-issue" && can.postGoodsIssue && (
          <SilaGoodsIssue initialGoodsIssueId={initialGoodsIssueId} />
        )}
        {tab === "adjustments" && can.postAdjustment && (
          <SilaStockAdjustments initialAdjustmentId={initialAdjustmentId} />
        )}
        {tab === "stock-counts" && (
          <SilaStockCounts canApprove={can.approveCount} initialCountId={initialCountId} />
        )}
        {tab === "enquiries" && <SilaShortageEnquiries canReview={can.approveCount} />}
        {tab === "shortage-report" && can.approveCount && (
          <SilaShortageReport onOpenCount={(id) => onOpenCount?.(id)} />
        )}
        {tab === "physical-inventory" && (
          <SilaPhysicalInventoryRequests
            canRequest={can.manageCount}
            canSchedule={can.approveCount}
            onOpenCount={(id) => onOpenCount?.(id)}
          />
        )}
        {tab === "alerts" && (
          <SilaAlerts
            onCountRequested={(id) => onOpenCount?.(id)}
            onPhysicalInventoryRequested={() => {
              setTab("physical-inventory");
              onOpenPhysicalInventory?.();
            }}
          />
        )}
        {tab === "recommendations" && showRecommendations && (
          <SilaRecommendations readOnly={readOnlyRecommendations} />
        )}
      </div>
    </div>
  );
};

export default SilaInventoryStudio;
