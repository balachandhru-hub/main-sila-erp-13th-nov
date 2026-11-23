import React, { useState } from "react";
import type { SilaMeNavKey, SilaMeRole } from "./silaMeNav";
import { silaMePermissions, type SilaMePermissions } from "./silaMePermissions";
import SilaHomeDashboard from "./SilaHomeDashboard";
import {
  silaHomeCapabilities,
  silaHomePersonaCapabilities,
  type SilaHomeCapabilities,
  type SilaHomePersona,
} from "./silaHomeCapabilities";
import SilaInventoryDashboard from "./inventory/SilaInventoryDashboard";
import SilaLiveInventory from "./inventory/SilaLiveInventory";
import SilaInventoryStudio, { inventoryStudioTabFromNav } from "./inventory/SilaInventoryStudio";
import SilaInventoryTransactions from "./inventory/SilaInventoryTransactions";
import SilaLocationMaster from "./inventory/SilaLocationMaster";
import SilaMaterialInventory from "./inventory/SilaMaterialInventory";
import SilaPurchaseRequests from "./inventory/SilaPurchaseRequests";
import SilaQuickTransferPolicy from "./inventory/SilaQuickTransferPolicy";
import SilaAuditLog from "./control/SilaAuditLog";
import SilaRecipes from "./recipes/SilaRecipes";
import SilaRecipeApprovals from "./recipes/SilaRecipeApprovals";
import SilaRecipeMasterData from "./recipes/SilaRecipeMasterData";
import SilaSubstitutions from "./substitutions/SilaSubstitutions";
import SilaPosIntegration from "./pos/SilaPosIntegration";
import SilaConsolidatedTracker from "./trackers/SilaConsolidatedTracker";
import SilaReceivingDashboard from "./receiving/SilaReceivingDashboard";
import SilaPurchaseOrdersOpen from "./receiving/SilaPurchaseOrdersOpen";
import SilaGoodsReceipts from "./receiving/SilaGoodsReceipts";
import SilaInvoices from "./receiving/SilaInvoices";
import SilaCreditMemos from "./receiving/SilaCreditMemos";
import SilaSuppliers from "./receiving/SilaSuppliers";
import SilaCompanyCodes from "./receiving/SilaCompanyCodes";
import SilaPoImport from "./receiving/SilaPoImport";
import SilaOcrSettings from "./receiving/SilaOcrSettings";
import SilaApprovalFlows from "./setup/SilaApprovalFlows";
import SilaPriceApprovals from "./materials/SilaPriceApprovals";
import SilaMenuEngineeringApp from "./menuEngineering/SilaMenuEngineeringApp";
import "./SilaMeWorkspace.css";

interface SilaMeWorkspaceProps {
  navKey: SilaMeNavKey;
  role: SilaMeRole;
  permissions?: SilaMePermissions;
  capabilities?: SilaHomeCapabilities;
  /** Opens another SILA ME screen or navigation route (e.g. 'models'). */
  onNavigate: (key: any) => void;
}

/**
 * The SILA ME (hospitality add-on) screens. The role decides which actions a screen offers (silaMePermissions);
 * every action is authorised again by the API.
 */
const SilaMeWorkspace: React.FC<SilaMeWorkspaceProps> = ({ navKey, role, permissions, capabilities, onNavigate }) => {
  const can = permissions ?? silaMePermissions(role);
  const homeCaps = capabilities ?? silaHomeCapabilities(true, can);
  const [previewPersona, setPreviewPersona] = useState<SilaHomePersona>("self");
  // Screens that open a specific record in another screen pass it through here.
  const [countToOpen, setCountToOpen] = useState<string | null>(null);
  const [transferToOpen, setTransferToOpen] = useState<string | null>(null);
  const [adjustmentToOpen, setAdjustmentToOpen] = useState<string | null>(null);
  const [goodsIssueToOpen, setGoodsIssueToOpen] = useState<string | null>(null);
  const [liveSearch, setLiveSearch] = useState<string | undefined>(undefined);
  const [liveHealth, setLiveHealth] = useState<string | undefined>(undefined);

  const openCount = (stockCountId: string) => {
    setCountToOpen(stockCountId);
    onNavigate("silaStockCounts");
  };

  /** Inventory Transactions row → existing source-document detail (ITO / ADJ / GI / stock count). */
  const openLedgerDocument = (referenceType: string, referenceId: string) => {
    if (!referenceId) return;
    const type = referenceType.trim().toUpperCase();
    if (type === "ITO" || type === "ITO_REV") {
      setTransferToOpen(referenceId);
      onNavigate("silaTransfers");
      return;
    }
    if (type === "ADJUSTMENT") {
      setAdjustmentToOpen(referenceId);
      onNavigate("silaAdjustments");
      return;
    }
    if (type === "GOODS_ISSUE" || type === "GI") {
      setGoodsIssueToOpen(referenceId);
      onNavigate("silaGoodsIssue");
      return;
    }
    if (type === "STOCK_COUNT") {
      openCount(referenceId);
    }
  };

  const renderInventoryStudio = () => (
    <SilaInventoryStudio
      permissions={can}
      showRecommendations={homeCaps.recommendation}
      initialTab={inventoryStudioTabFromNav(navKey)}
      initialCountId={countToOpen}
      initialTransferId={transferToOpen}
      initialAdjustmentId={adjustmentToOpen}
      initialGoodsIssueId={goodsIssueToOpen}
      onOpenLive={(health) => {
        setLiveHealth(health);
        setLiveSearch(undefined);
        onNavigate("silaLiveInventory");
      }}
      onOpenCount={openCount}
      onOpenPhysicalInventory={() => onNavigate("silaInventoryStudio")}
      readOnlyRecommendations={role === "admin" && previewPersona !== "self"}
    />
  );

  const renderContent = () => {
    switch (navKey) {
      case "silaDashboard": {
        const previewing = role === "admin" && previewPersona !== "self";
        return (
          <SilaHomeDashboard
            capabilities={silaHomePersonaCapabilities(homeCaps, role === "admin" ? previewPersona : "self")}
            previewPersona={role === "admin" ? previewPersona : "self"}
            onPreviewPersona={role === "admin" ? setPreviewPersona : undefined}
            onNavigate={onNavigate}
            readOnly={previewing}
          />
        );
      }
      case "silaInventoryDashboard":
        return (
          <SilaInventoryDashboard
            canTransfer={can.manageTransfer}
            canRequestPurchase={can.requestPurchase}
            onNavigate={(key, query) => {
              setLiveSearch(key === "silaLiveInventory" ? query : undefined);
              onNavigate(key);
            }}
          />
        );
      case "silaLiveInventory":
        return (
          <SilaLiveInventory
            canTransfer={can.manageTransfer}
            canRequestPurchase={can.requestPurchase}
            canManageCount={can.manageCount}
            canPostGoodsIssue={can.postGoodsIssue}
            permissions={can}
            initialSearch={liveSearch}
            initialHealth={liveHealth}
            onNavigate={onNavigate}
          />
        );
      case "silaTransactions":
        return <SilaInventoryTransactions onOpenDocument={openLedgerDocument} />;
      case "silaInventoryStudio":
      case "silaStockHealth":
      case "silaRecipeAvailability":
      case "silaInventoryAnalytics":
      case "silaTransfers":
      case "silaGoodsIssue":
      case "silaAdjustments":
      case "silaStockCounts":
      case "silaEnquiries":
      case "silaShortageReport":
      case "silaPhysicalInventory":
      case "silaAlerts":
      case "silaRecommendations":
        return renderInventoryStudio();
      case "silaPurchaseRequests":
        // Removed from Inventory menu; keep route for legacy deep links only.
        return <SilaPurchaseRequests canAddToBucket={role === "admin" || role === "store-manager"} />;
      case "silaRecipeDashboard":
        // Legacy dashboard route — permanently folded into Recipe Studio (do not reintroduce a separate dashboard UI).
        return (
          <SilaRecipes
            canManage={can.manageRecipe}
            canApprove={can.approveRecipe}
            canRequestPrice={can.manageMasterData}
          />
        );
      case "silaRecipes":
        return (
          <SilaRecipes
            canManage={can.manageRecipe}
            canApprove={can.approveRecipe}
            canRequestPrice={can.manageMasterData}
          />
        );
      case "silaRecipeApprovals":
        return <SilaRecipeApprovals />;
      case "silaSubstitutions":
        return <SilaSubstitutions canDecide={can.manageRecipe} />;
      case "silaPos":
        return <SilaPosIntegration canManage={can.managePos} />;
      case "silaTracker":
      case "silaErpPostings":
      case "silaMeTracker":
        return <SilaConsolidatedTracker tracker="SILA_ME" canReprocess={can.manageErpPosting} />;
      case "silaReceivingDashboard":
        return (
          <SilaReceivingDashboard
            canPost={can.postGrn}
            canScan={can.processInvoice}
            canApproveInvoice={can.approveInvoice}
            canProcessCreditMemo={can.processCreditMemo}
            canReprocess={can.manageErpPosting}
            supervisor={can.manageErpPosting || can.viewAudit}
          />
        );
      case "silaPurchaseOrders":
        return <SilaPurchaseOrdersOpen />;
      case "silaGoodsReceipts":
        return <SilaGoodsReceipts canPost={can.postGrn} canReprocess={can.manageErpPosting} />;
      case "silaInvoices":
        return (
          <SilaInvoices
            canProcess={can.processInvoice}
            canApprove={can.approveInvoice}
            canReceive={can.postGrn}
            canManageErp={can.manageErpPosting}
            canProcessCreditMemo={can.processCreditMemo}
          />
        );
      case "silaCreditMemos":
        return <SilaCreditMemos canProcess={can.processCreditMemo} canApprove={can.approveCreditMemo} />;
      case "silaPriceApprovals":
        return <SilaPriceApprovals />;
      case "silaAuditLog":
        return <SilaAuditLog />;
      case "silaLocations":
        return <SilaLocationMaster canManage={can.manageLocation} />;
      case "silaMaterials":
        return <SilaMaterialInventory canManage={can.manageLocation} canManageMasterData={can.manageMasterData} />;
      case "silaRecipeMasterData":
        return <SilaRecipeMasterData canManage={can.manageMasterData} />;
      case "silaSuppliers":
        return <SilaSuppliers canManage={can.manageMasterData} />;
      case "silaCompanyCodes":
        return <SilaCompanyCodes canManage={can.manageMasterData} />;
      case "silaPoImport":
        return <SilaPoImport canManage={can.manageMasterData} />;
      case "silaQuickTransferPolicy":
        return <SilaQuickTransferPolicy canManage={can.manageMasterData} />;
      case "silaOcrSettings":
        return <SilaOcrSettings canManage={can.manageMasterData} />;
      case "silaApprovalFlows":
        return <SilaApprovalFlows />;
      case "silaMenuDashboard":
      case "silaMenuItems":
      case "silaMenuPlanning":
      case "silaMenuPortions":
      case "silaMenuDemand":
      case "silaMenuPosSales":
      case "silaMenuPosMapping":
      case "silaMenuPerformance":
      case "silaMenuConsumption":
      case "silaMenuCost":
      case "silaMenuRecommendations":
      case "silaMenuIntelligence":
      case "silaMenuWhatIf":
      case "silaMenuSetup":
        return (
          <SilaMenuEngineeringApp
            navKey={navKey}
            canManage={can.manageMenuEngineering}
            canRequestPurchase={can.requestPurchase}
            canManageRecipe={can.manageRecipe}
            onNavigate={onNavigate}
          />
        );
      default:
        return null;
    }
  };

  const content = renderContent();
  if (!content) return null;

  return (
    <div className="sila-workspace-container">
      <div className="sila-workspace-body">{content}</div>
    </div>
  );
};

export default SilaMeWorkspace;
