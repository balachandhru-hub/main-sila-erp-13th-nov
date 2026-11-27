import { FileCheckIcon, HomeIcon, LayoutGridIcon } from "@vosox/shared-ui";
import { silaMePermissions } from "./silaMePermissions";
import { silaHomeCapabilities, type SilaHomeCapabilities } from "./silaHomeCapabilities";

/** Who is using the SILA ME add-on; decides the menu and which actions the screens offer. */
export type SilaMeRole = "admin" | "store-manager" | "outlet-manager" | "buyer-user" | "cost-controller" | "fnb-director" | "finance-manager" | "ap-processor";

/** Model key of the SILA ME (hospitality) add-on in the organization's models. */
export const SILA_ME_MODEL_KEY = "SILA_ME";

/** Each SILA ME screen's URL segment (/sila-live-inventory …); merged into the dashboard's nav paths. */
export const SILA_ME_NAV_PATHS = {
  silaDashboard: "sila-dashboard",
  silaInventoryDashboard: "sila-inventory-dashboard",
  silaLiveInventory: "sila-live-inventory",
  silaInventoryStudio: "sila-inventory-studio",
  silaStockHealth: "sila-stock-health",
  silaRecipeAvailability: "sila-recipe-availability",
  silaInventoryAnalytics: "sila-inventory-analytics",
  silaTransfers: "sila-transfers",
  silaGoodsIssue: "sila-goods-issue",
  silaAdjustments: "sila-adjustments",
  silaStockCounts: "sila-stock-counts",
  silaEnquiries: "sila-enquiries",
  silaShortageReport: "sila-shortage-report",
  silaPhysicalInventory: "sila-physical-inventory",
  silaAlerts: "sila-alerts",
  silaPurchaseRequests: "sila-purchase-requests",
  silaRecommendations: "sila-recommendations",
  silaTransactions: "sila-transactions",
  silaRecipeDashboard: "sila-recipe-dashboard",
  silaRecipes: "sila-recipes",
  silaRecipeApprovals: "sila-recipe-approvals",
  silaSubstitutions: "sila-substitutions",
  silaPos: "sila-pos",
  silaTracker: "sila-transaction-tracker",
  silaMeTracker: "sila-me-transactions",
  silaReceivingDashboard: "sila-receiving-dashboard",
  silaPurchaseOrders: "sila-purchase-orders",
  silaGoodsReceipts: "sila-goods-receipts",
  silaInvoices: "sila-invoices",
  silaCreditMemos: "sila-credit-memos",
  silaErpPostings: "sila-erp-postings",
  silaPriceApprovals: "sila-price-approvals",
  silaAuditLog: "sila-audit-log",
  silaLocations: "sila-locations",
  silaMaterials: "sila-materials",
  silaRecipeMasterData: "sila-recipe-master-data",
  silaSuppliers: "sila-suppliers",
  silaCompanyCodes: "sila-company-codes",
  silaPoImport: "sila-po-import",
  silaQuickTransferPolicy: "sila-quick-transfer-policy",
  silaUserPoDefaults: "sila-user-po-defaults",
  silaOcrSettings: "sila-ocr-settings",
  silaApprovalFlows: "sila-approval-flows",
  silaMenuStudio: "sila-menu-studio",
  silaMenuDashboard: "sila-menu-dashboard",
  silaMenuItems: "sila-menu-items",
  silaMenuPlanning: "sila-menu-planning",
  silaMenuPortions: "sila-menu-portions",
  silaMenuDemand: "sila-menu-demand",
  silaMenuPosSales: "sila-menu-pos-sales",
  silaMenuPosMapping: "sila-menu-pos-mapping",
  silaMenuPerformance: "sila-menu-performance",
  silaMenuConsumption: "sila-menu-consumption",
  silaMenuCost: "sila-menu-cost",
  silaMenuRecommendations: "sila-menu-recommendations",
  silaMenuIntelligence: "sila-menu-intelligence",
  silaMenuWhatIf: "sila-menu-what-if",
  silaMenuSetup: "sila-menu-setup",
} as const;

export type SilaMeNavKey = keyof typeof SILA_ME_NAV_PATHS;

export const isSilaMeNav = (key: string): key is SilaMeNavKey => key in SILA_ME_NAV_PATHS;

interface NavLeaf {
  key: SilaMeNavKey;
  label: string;
}

interface NavChildGroup {
  label: string;
  items: NavLeaf[];
}

type NavChild = NavLeaf | NavChildGroup;

interface NavGroup {
  label: string;
  items: NavChild[];
}

type NavEntry = NavLeaf | NavGroup;

export const isSilaMeNavGroup = (entry: NavEntry): entry is NavGroup => "items" in entry;

const leaf = (show: boolean, key: SilaMeNavKey, label: string): NavLeaf[] => (show ? [{ key, label }] : []);

/** The "SILA ME" sidebar entry for the role; a leaf is shown only when the role may use that screen. */
export const buildSilaMeNavItem = (role: SilaMeRole, access?: SilaHomeCapabilities) => {
  const can = silaMePermissions(role);
  const home = access ?? silaHomeCapabilities(true, can);
  const setup = home.admin || can.manageLocation || can.manageMasterData;
  const entries: NavEntry[] = [
    { key: "silaDashboard", label: "SILA ME" },
    {
      label: "Inventory",
      items: home.viewInventory ? [
        { key: "silaInventoryDashboard", label: "Dashboard" },
        { key: "silaLiveInventory", label: "Live Inventory" },
        { key: "silaTransactions", label: "Inventory Transactions" },
        // Stock health, transfers, counts, alerts, etc. live inside Inventory Studio.
        { key: "silaInventoryStudio", label: "Inventory Studio" },
      ] : [],
    },
    {
      label: "Menu Engineering",
      items: (home.hasSilaMe && (can.viewMeStudio || can.viewMenuEngineering || can.manageMenuEngineering || can.manageRecipe)) ? [
        { key: "silaMenuStudio", label: "ME Studio" },
        ...leaf(can.viewMenuEngineering || can.manageMenuItems || can.manageMenuEngineering, "silaMenuItems", "Menu Items"),
        ...leaf(can.viewMenuEngineering || can.createMenu || can.editDraftMenu || can.manageMenuEngineering, "silaMenuPlanning", "Menu Planning"),
        ...leaf(can.viewMenuEngineering || can.managePortionPlans || can.manageMenuEngineering, "silaMenuPortions", "Portion Planning"),
        ...leaf(can.viewMenuEngineering || can.manageDemandPlans || can.manageMenuEngineering, "silaMenuDemand", "Demand Planning"),
        ...leaf(can.viewPosSales || can.viewMenuEngineering, "silaMenuPosSales", "POS Sales"),
        ...leaf(can.managePosMapping || can.viewMenuEngineering || can.manageMenuEngineering, "silaMenuPosMapping", "POS Mapping"),
        ...leaf(can.viewMenuPerformance || can.viewMenuEngineering, "silaMenuPerformance", "Menu Performance"),
        ...leaf(can.viewMenuEngineering, "silaMenuConsumption", "Consumption Analysis"),
        ...leaf(can.viewMenuCost || can.viewMenuMargin, "silaMenuCost", "Cost & Margin Analysis"),
        ...leaf(can.generateMenuPurchaseRec || can.requestPurchase || can.viewMenuEngineering, "silaMenuRecommendations", "Purchase Recommendations"),
        ...leaf(can.useAiMenuIntelligence || can.viewMenuEngineering, "silaMenuIntelligence", "AI Menu Intelligence"),
        ...leaf(can.runMenuSimulation || can.viewMenuEngineering, "silaMenuWhatIf", "What-If Simulation"),
        ...leaf(can.manageMenuSetup || setup || can.manageMenuEngineering, "silaMenuSetup", "Setup"),
      ] : [],
    },
    {
      label: "Recipe Management",
      items: [
        // Recipe Studio owns catalog + former dashboard KPIs. Do not remove this entry
        // or reintroduce a separate Recipe Dashboard nav item.
        { key: "silaRecipes", label: "Recipe Studio" },
        ...leaf(can.approveRecipe, "silaRecipeApprovals", "Recipe Approvals"),
        { key: "silaSubstitutions", label: "Ingredient Substitutions" },
        ...leaf(can.managePos, "silaPos", "POS Studio"),
      ],
    },
    {
      label: "Receiving",
      items: home.viewReceiving ? [
        { key: "silaReceivingDashboard", label: "Dashboard" },
        { key: "silaPurchaseOrders", label: "Open Purchase Orders" },
        { key: "silaGoodsReceipts", label: "Goods Receipts" },
      ] : [],
    },
    {
      label: "Accounts Payable",
      items: [
        ...leaf(home.viewInvoices, "silaInvoices", "Invoices"),
        ...leaf(home.viewCreditMemos, "silaCreditMemos", "Credit Memos"),
      ],
    },
    {
      label: "Setup",
      items: [
        ...(setup
          ? [
              { key: "silaLocations" as const, label: "Location Master" },
              { key: "silaMaterials" as const, label: "Material Inventory" },
              { key: "silaRecipeMasterData" as const, label: "Recipe Master Data" },
              { key: "silaSuppliers" as const, label: "Suppliers" },
              { key: "silaCompanyCodes" as const, label: "Company Codes" },
              ...leaf(can.manageMasterData, "silaPoImport", "Purchase Order Import"),
              ...leaf(can.manageMasterData, "silaQuickTransferPolicy", "Quick Transfer Policy"),
              ...leaf(can.manageMasterData, "silaUserPoDefaults", "PO Defaults & Account Assignment"),
              ...leaf(can.manageMasterData, "silaOcrSettings", "Invoice OCR Settings"),
              { key: "silaApprovalFlows" as const, label: "Approval flow" },
            ]
          : []),
        // SILA ME Transactions live under Setup as Transaction Tracker (no Integration menu).
        ...leaf(can.manageErpPosting, "silaMeTracker", "Transaction Tracker"),
        ...leaf(can.approvePrice, "silaPriceApprovals", "Price Approvals"),
        ...leaf(can.viewAudit, "silaAuditLog", "Audit Log"),
      ],
    },
  ];

  return {
    key: "silaMe",
    icon: <LayoutGridIcon />,
    label: "SILA ME - Hospitality",
    subItems: entries.filter((entry) => !isSilaMeNavGroup(entry) || entry.items.length > 0),
  };
};

/**
 * The SILA ME site menu while the add-on is open: purchasing exit icons near the logo, then SILA ME menus.
 * SILA ME home is labeled Dashboard (text only, no icon).
 */
export const buildSilaMeModuleNav = (role: SilaMeRole, access?: SilaHomeCapabilities) => {
  const sections = (buildSilaMeNavItem(role, access).subItems ?? []).flatMap((entry) =>
    isSilaMeNavGroup(entry)
      ? [{ key: `sila-section-${entry.label}`, icon: <LayoutGridIcon />, label: entry.label, subItems: entry.items }]
      : entry.key === "silaDashboard"
        ? [{ key: entry.key, label: "Dashboard" }]
        : [{ key: entry.key, icon: <HomeIcon />, label: entry.label }],
  );
  return [
    { key: "dashboard", icon: <HomeIcon />, label: "Purchasing Home", iconOnly: true },
    { key: "purchasing", icon: <FileCheckIcon />, label: "Purchasing Dashboard", iconOnly: true },
    ...sections,
  ];
};
