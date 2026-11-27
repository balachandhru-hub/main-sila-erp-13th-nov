using Microsoft.EntityFrameworkCore;
using SharedKernel.Integration.Entities;
using Buyer.Domain.Entities;
using SharedKernel.Models;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using SharedKernel.Util;
using Buyer.Domain.Common;

namespace Buyer.Infrastructure.DbContext
{
    public class RepositoryContext : Microsoft.EntityFrameworkCore.DbContext
    {
        private readonly IConfiguration _configuration;

        public RepositoryContext(DbContextOptions<RepositoryContext> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }
        public DbSet<BuyerCategory> BuyerCategory { get; set; }
        public DbSet<BuyerDeliveryLocation> BuyerDeliveryLocation { get; set; }
        public DbSet<BuyerBankAccount> BuyerBankAccount { get; set; }
        public DbSet<BuyerBusinessProfile> BuyerBusinessProfile { get; set; }
        public DbSet<BuyerRegistration> BuyerRegistration { get; set; }
        public DbSet<Asset> Asset { get; set; }
        public DbSet<RFQ> RFQ { get; set; }
        public DbSet<RFQItem> RFQItem { get; set; }
        public DbSet<RFQAttachmentMapping> RFQAttachmentMapping { get; set; }
        public DbSet<RFQItemAttachmentMapping> RFQItemAttachmentMapping { get; set; }
        public DbSet<ItemBuyerMaster> ItemBuyerMaster { get; set; }
        public DbSet<RFQQuestionAnswer> RFQQuestionAnswer { get; set; }
        public DbSet<RFQQuestion> RFQQuestion { get; set; }
        public DbSet<RFQQuestionOption> RFQQuestionOption { get; set; }
        public DbSet<SupplierVerificationRequest> SupplierVerificationRequest { get; set; }
        public DbSet<VerificationAnswer> VerificationAnswer { get; set; }
        public DbSet<VerificationAnswerOption> VerificationAnswerOption { get; set; }
        public DbSet<VerificationTemplate> VerificationTemplate { get; set; }
        public DbSet<VerificationTemplateQuestion> VerificationTemplateQuestion { get; set; }
        public DbSet<VerificationTemplateQuestionOption> VerificationTemplateQuestionOption { get; set; }
        public DbSet<RFQAnswerOption> RFQAnswerOption { get; set; }
        public DbSet<BuyerCostCenter> BuyerCostCenter {get;set;}
        public DbSet<BuyerDepartment> BuyerDepartment {get;set;}
        public DbSet<BuyerSupplierMapping> BuyerSupplierMapping {get;set;}
        public DbSet<RFQSupplierMapping> RFQSupplierMapping {get;set;}
        public DbSet<DefaultVerificationTemplate> DefaultVerificationTemplate {get;set;}
        public DbSet<DefaultVerificationTemplateQuestion> DefaultVerificationTemplateQuestion {get;set;}
        public DbSet<RFQQuestionAttachmentMapping> RFQQuestionAttachmentMapping {get;set;}
        public DbSet<RFQBlockchainRecord> RFQBlockchainRecord {get;set;}
        public DbSet<RFQOrganizationUserMapping> RFQOrganizationUserMapping {get;set;}
        public DbSet<ExternalSupplier> ExternalSupplier {get;set;}
        public DbSet<RFQExternalSupplier> RFQExternalSupplier {get;set;}
        public DbSet<MessageThread> MessageThread {get;set;}
        public DbSet<Message> Message {get;set;}
        public DbSet<MessageAttachment> MessageAttachment {get;set;}
        public DbSet<PredefinedMaterial> PredefinedMaterial {get;set;}
        public DbSet<MasterApprovalFlow> MasterApprovalFlow {get;set;}
        public DbSet<ApprovalFlowUserMapping> ApprovalFlowUserMapping {get;set;}
        public DbSet<PredefinedMaterialApprovalFlowUserMapping> PredefinedMaterialApprovalFlowUserMapping {get;set;}
        public DbSet<ApprovalFlowPredefinedMaterialMapping> ApprovalFlowPredefinedMaterialMapping {get;set;}
        public DbSet<ExcelMaterialMaster> ExcelMaterialMaster {get;set;}
        public DbSet<RFQAward> RFQAward {get;set;}
        public DbSet<RFQAwardItem> RFQAwardItem {get;set;}
        public DbSet<PredefinedContract> PredefinedContract {get;set;}
        public DbSet<PredefinedContractAttachment> PredefinedContractAttachment {get;set;}
        public DbSet<PredefinedContractApprovalFlow> PredefinedContractApprovalFlow {get;set;}
        public DbSet<PredefinedContractApprovalUserMapping> PredefinedContractApprovalUserMapping {get;set;}
        public DbSet<ContractTemplate> ContractTemplate {get;set;}
        public DbSet<ContractDetails> ContractDetails {get;set;}
        public DbSet<ContractAttachment> ContractAttachment {get;set;}
        public DbSet<BuyerOutlet> BuyerOutlet { get; set; }
        public DbSet<BuyerOutletUserMapping> BuyerOutletUserMapping { get; set; }
        public DbSet<BuyerProperty> BuyerProperty { get; set; }
        public DbSet<CatalogMaterialMapping> CatalogMaterialMapping { get; set; }
        public DbSet<PersonalWishlist> PersonalWishlist { get; set; }
        public DbSet<PersonalWishlistItem> PersonalWishlistItem { get; set; }
        public DbSet<WeeklyBucket> WeeklyBucket { get; set; }
        public DbSet<WeeklyBucketItem> WeeklyBucketItem { get; set; }
        public DbSet<WeeklyBucketRecommendation> WeeklyBucketRecommendation { get; set; }
        public DbSet<WeeklyBucketApprovalFlow> WeeklyBucketApprovalFlow { get; set; }
        public DbSet<WeeklyBucketApprovalUserMapping> WeeklyBucketApprovalUserMapping { get; set; }
        public DbSet<WeeklyBucketAudit> WeeklyBucketAudit { get; set; }
        public DbSet<ErpIntegrationConfiguration> ErpIntegrationConfiguration { get; set; }
        public DbSet<PurchaseDocumentIntegration> PurchaseDocumentIntegration { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrder { get; set; }
        public DbSet<PurchaseOrderItem> PurchaseOrderItem { get; set; }
        public DbSet<ApiIntegrationConfiguration> ApiIntegrationConfiguration { get; set; }
        public DbSet<ApiFieldMapping> ApiFieldMapping { get; set; }
        public DbSet<ApiIntegrationExecution> ApiIntegrationExecution { get; set; }
        public DbSet<IntegrationSchemaSnapshot> IntegrationSchemaSnapshot { get; set; }
        public DbSet<MaterialUomConversion> MaterialUomConversion { get; set; }
        public DbSet<InventoryLocation> InventoryLocation { get; set; }
        public DbSet<InventoryLocationUserMapping> InventoryLocationUserMapping { get; set; }
        public DbSet<InventoryLocationMaterial> InventoryLocationMaterial { get; set; }
        public DbSet<InventoryBalance> InventoryBalance { get; set; }
        public DbSet<InventoryTransaction> InventoryTransaction { get; set; }
        public DbSet<InventoryErpPosting> InventoryErpPosting { get; set; }
        public DbSet<InternalTransferOrder> InternalTransferOrder { get; set; }
        public DbSet<InternalTransferOrderItem> InternalTransferOrderItem { get; set; }
        public DbSet<InventoryWorkflowEvent> InventoryWorkflowEvent { get; set; }
        public DbSet<GoodsIssue> GoodsIssue { get; set; }
        public DbSet<GoodsIssueItem> GoodsIssueItem { get; set; }
        public DbSet<StockAdjustment> StockAdjustment { get; set; }
        public DbSet<StockAdjustmentItem> StockAdjustmentItem { get; set; }
        public DbSet<StockCount> StockCount { get; set; }
        public DbSet<StockCountItem> StockCountItem { get; set; }
        public DbSet<StockShortageEnquiry> StockShortageEnquiry { get; set; }
        public DbSet<InventoryAlert> InventoryAlert { get; set; }
        public DbSet<Recipe> Recipe { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredient { get; set; }
        public DbSet<RecipeOutletPrice> RecipeOutletPrice { get; set; }
        public DbSet<PosSalesBatch> PosSalesBatch { get; set; }
        public DbSet<PosSalesTransaction> PosSalesTransaction { get; set; }
        public DbSet<GoodsReceipt> GoodsReceipt { get; set; }
        public DbSet<GoodsReceiptItem> GoodsReceiptItem { get; set; }
        public DbSet<Invoice> Invoice { get; set; }
        public DbSet<RecipeFamily> RecipeFamily { get; set; }
        public DbSet<RecipeCategory> RecipeCategory { get; set; }
        public DbSet<GoodsIssueCostCenter> GoodsIssueCostCenter { get; set; }
        public DbSet<SilaApprovalStep> SilaApprovalStep { get; set; }
        public DbSet<PosSource> PosSource { get; set; }
        public DbSet<PosOutletMapping> PosOutletMapping { get; set; }
        public DbSet<PosItemMapping> PosItemMapping { get; set; }
        public DbSet<MaterialPriceChange> MaterialPriceChange { get; set; }
        public DbSet<MaterialOutletPrice> MaterialOutletPrice { get; set; }
        public DbSet<InternalPurchaseRequest> InternalPurchaseRequest { get; set; }
        public DbSet<QuickTransferPolicy> QuickTransferPolicy { get; set; }
        public DbSet<PhysicalInventoryRequest> PhysicalInventoryRequest { get; set; }
        public DbSet<LocationMaterialThreshold> LocationMaterialThreshold { get; set; }
        public DbSet<SilaSupplier> SilaSupplier { get; set; }
        public DbSet<CompanyCodeMaster> CompanyCodeMaster { get; set; }
        public DbSet<InvoiceExtraction> InvoiceExtraction { get; set; }
        public DbSet<SilaOcrConfiguration> SilaOcrConfiguration { get; set; }
        public DbSet<RecipeSubstitutionProposal> RecipeSubstitutionProposal { get; set; }
        public DbSet<StockCountPhoto> StockCountPhoto { get; set; }
        public DbSet<SilaDocumentSequence> SilaDocumentSequence { get; set; }
        public DbSet<InvoiceItem> InvoiceItem { get; set; }
        public DbSet<CreditMemo> CreditMemo { get; set; }
        public DbSet<CreditMemoItem> CreditMemoItem { get; set; }
        public DbSet<OperationalTask> OperationalTask { get; set; }
        public DbSet<OperationalAlert> OperationalAlert { get; set; }
        public DbSet<MenuItem> MenuItem { get; set; }
        public DbSet<MenuPlan> MenuPlan { get; set; }
        public DbSet<MenuPlanLine> MenuPlanLine { get; set; }
        public DbSet<MenuPosMapping> MenuPosMapping { get; set; }
        public DbSet<PmsDemandSnapshot> PmsDemandSnapshot { get; set; }
        public DbSet<MenuEngineeringSettings> MenuEngineeringSettings { get; set; }
        public DbSet<MenuItemVersion> MenuItemVersion { get; set; }
        public DbSet<MenuEngineeringScenario> MenuEngineeringScenario { get; set; }
        public DbSet<MenuEngineeringApprovalHistory> MenuEngineeringApprovalHistory { get; set; }
        public DbSet<MenuDemandOverride> MenuDemandOverride { get; set; }
        public DbSet<MenuEngineeringConfigHistory> MenuEngineeringConfigHistory { get; set; }
        public DbSet<MenuEngineeringAgentRun> MenuEngineeringAgentRun { get; set; }
        public DbSet<MenuEngineeringRecommendation> MenuEngineeringRecommendation { get; set; }
        public DbSet<MenuEngineeringProactiveAlert> MenuEngineeringProactiveAlert { get; set; }
        public DbSet<MenuEngineeringLearningOutcome> MenuEngineeringLearningOutcome { get; set; }
        public DbSet<MenuEngineeringModelEvaluation> MenuEngineeringModelEvaluation { get; set; }
        public DbSet<MenuEngineeringAiFeedback> MenuEngineeringAiFeedback { get; set; }
        public DbSet<MenuEngineeringTwinSnapshot> MenuEngineeringTwinSnapshot { get; set; }
        public DbSet<MenuEngineeringTwinSimulationRun> MenuEngineeringTwinSimulationRun { get; set; }
        public DbSet<MenuEngineeringModelDriftEvent> MenuEngineeringModelDriftEvent { get; set; }
        public DbSet<RecipeLegacyBatch> RecipeLegacyBatch { get; set; }
        public DbSet<RecipeLegacyRecipe> RecipeLegacyRecipe { get; set; }
        public DbSet<RecipeLegacyMaterialMap> RecipeLegacyMaterialMap { get; set; }
        public DbSet<RecipeLegacyImage> RecipeLegacyImage { get; set; }
        public DbSet<SilaUserPurchasingProfile> SilaUserPurchasingProfile { get; set; }
        public DbSet<SilaUserPoFlexConfig> SilaUserPoFlexConfig { get; set; }
        public DbSet<SilaUserPoFlexParam> SilaUserPoFlexParam { get; set; }
        public DbSet<SilaUserPoFlexCondition> SilaUserPoFlexCondition { get; set; }
        public DbSet<SilaVoicePoSession> SilaVoicePoSession { get; set; }
        public DbSet<SilaVoicePoTurn> SilaVoicePoTurn { get; set; }
        public DbSet<SilaCulinaryAiConfiguration> SilaCulinaryAiConfiguration { get; set; }
        public DbSet<SilaCulinaryRecipeDraft> SilaCulinaryRecipeDraft { get; set; }
        public DbSet<SilaCulinaryRecipeRevision> SilaCulinaryRecipeRevision { get; set; }
        public DbSet<SilaCulinaryAiUsageLog> SilaCulinaryAiUsageLog { get; set; }
        public DbSet<SilaCulinaryMaterialMapping> SilaCulinaryMaterialMapping { get; set; }
        public DbSet<SilaCulinaryMaterialMappingHistory> SilaCulinaryMaterialMappingHistory { get; set; }
        public DbSet<SilaCulinaryMaterialAlias> SilaCulinaryMaterialAlias { get; set; }
        public DbSet<SilaCulinaryMaterialFeedback> SilaCulinaryMaterialFeedback { get; set; }
        public DbSet<SilaCulinaryMaterialIndexState> SilaCulinaryMaterialIndexState { get; set; }
        public DbSet<SilaCulinaryMaterialRequest> SilaCulinaryMaterialRequest { get; set; }
        public DbSet<SilaCulinaryMaterialRequestLink> SilaCulinaryMaterialRequestLink { get; set; }
        public DbSet<SilaCulinaryMaterialRequestHistory> SilaCulinaryMaterialRequestHistory { get; set; }
        public DbSet<SilaCulinaryCostingSnapshot> SilaCulinaryCostingSnapshot { get; set; }
        public DbSet<SilaCulinaryRecipeLifecycle> SilaCulinaryRecipeLifecycle { get; set; }
        public DbSet<SilaCulinaryEvent> SilaCulinaryEvent { get; set; }
        public DbSet<SilaCulinaryEventSession> SilaCulinaryEventSession { get; set; }
        public DbSet<SilaCulinaryEventTemplate> SilaCulinaryEventTemplate { get; set; }
        public DbSet<SilaCulinaryMenu> SilaCulinaryMenu { get; set; }
        public DbSet<SilaCulinaryMenuLine> SilaCulinaryMenuLine { get; set; }
        public DbSet<SilaCulinaryEventRevision> SilaCulinaryEventRevision { get; set; }
        public DbSet<SilaCulinaryMrpRun> SilaCulinaryMrpRun { get; set; }
        public DbSet<SilaCulinaryMrpLine> SilaCulinaryMrpLine { get; set; }
        public DbSet<SilaCulinaryMrpException> SilaCulinaryMrpException { get; set; }
        public DbSet<SilaCulinaryMrpScenario> SilaCulinaryMrpScenario { get; set; }
        public DbSet<SilaCulinaryMrpSnapshot> SilaCulinaryMrpSnapshot { get; set; }
        public DbSet<SilaCulinaryProcurementPlan> SilaCulinaryProcurementPlan { get; set; }
        public DbSet<SilaCulinaryProcurementLine> SilaCulinaryProcurementLine { get; set; }
        public DbSet<SilaCulinaryStockReservation> SilaCulinaryStockReservation { get; set; }
        public DbSet<SilaCulinaryProcurementAllocation> SilaCulinaryProcurementAllocation { get; set; }
        public DbSet<SilaCulinaryProcurementException> SilaCulinaryProcurementException { get; set; }
        public DbSet<SilaCulinaryProcurementScenario> SilaCulinaryProcurementScenario { get; set; }
        public DbSet<SilaCulinaryFinancialConfig> SilaCulinaryFinancialConfig { get; set; }
        public DbSet<SilaCulinaryEventFinancialPlan> SilaCulinaryEventFinancialPlan { get; set; }
        public DbSet<SilaCulinaryEventRevenueLine> SilaCulinaryEventRevenueLine { get; set; }
        public DbSet<SilaCulinaryEventExpenseLine> SilaCulinaryEventExpenseLine { get; set; }
        public DbSet<SilaCulinaryFinancialException> SilaCulinaryFinancialException { get; set; }
        public DbSet<SilaCulinaryFinancialScenario> SilaCulinaryFinancialScenario { get; set; }
        public DbSet<SilaCulinaryAnalyticsConfig> SilaCulinaryAnalyticsConfig { get; set; }
        public DbSet<SilaCulinarySimulation> SilaCulinarySimulation { get; set; }
        public DbSet<SilaCulinaryOptimizationJob> SilaCulinaryOptimizationJob { get; set; }
        public DbSet<SilaCulinaryRecommendation> SilaCulinaryRecommendation { get; set; }
        public DbSet<SilaCulinaryWasteRecord> SilaCulinaryWasteRecord { get; set; }
        public DbSet<SilaCulinaryAnalyticsAudit> SilaCulinaryAnalyticsAudit { get; set; }
        public DbSet<SilaCulinaryKitchen> SilaCulinaryKitchen { get; set; }
        public DbSet<SilaCulinaryWorkstation> SilaCulinaryWorkstation { get; set; }
        public DbSet<SilaCulinaryEquipment> SilaCulinaryEquipment { get; set; }
        public DbSet<SilaCulinaryStaffAssignment> SilaCulinaryStaffAssignment { get; set; }
        public DbSet<SilaCulinaryProductionDemand> SilaCulinaryProductionDemand { get; set; }
        public DbSet<SilaCulinaryProductionPlan> SilaCulinaryProductionPlan { get; set; }
        public DbSet<SilaCulinaryProductionPlanLine> SilaCulinaryProductionPlanLine { get; set; }
        public DbSet<SilaCulinaryProductionPlanVersion> SilaCulinaryProductionPlanVersion { get; set; }
        public DbSet<SilaCulinaryProductionOrder> SilaCulinaryProductionOrder { get; set; }
        public DbSet<SilaCulinaryProductionOrderIngredient> SilaCulinaryProductionOrderIngredient { get; set; }
        public DbSet<SilaCulinaryProductionBatch> SilaCulinaryProductionBatch { get; set; }
        public DbSet<SilaCulinaryProductionTask> SilaCulinaryProductionTask { get; set; }
        public DbSet<SilaCulinaryProductionPickingLine> SilaCulinaryProductionPickingLine { get; set; }
        public DbSet<SilaCulinaryProductionMaterialIssueRef> SilaCulinaryProductionMaterialIssueRef { get; set; }
        public DbSet<SilaCulinaryProductionQualityCheckpoint> SilaCulinaryProductionQualityCheckpoint { get; set; }
        public DbSet<SilaCulinaryProductionQualityResult> SilaCulinaryProductionQualityResult { get; set; }
        public DbSet<SilaCulinaryProductionException> SilaCulinaryProductionException { get; set; }
        public DbSet<SilaCulinaryProductionDispatch> SilaCulinaryProductionDispatch { get; set; }
        public DbSet<SilaCulinaryProductionAudit> SilaCulinaryProductionAudit { get; set; }
        public DbSet<SilaCulinaryReconciliationConfig> SilaCulinaryReconciliationConfig { get; set; }
        public DbSet<SilaCulinaryProductionConsumption> SilaCulinaryProductionConsumption { get; set; }
        public DbSet<SilaCulinaryAdditionalMaterialRequest> SilaCulinaryAdditionalMaterialRequest { get; set; }
        public DbSet<SilaCulinaryMaterialReturn> SilaCulinaryMaterialReturn { get; set; }
        public DbSet<SilaCulinaryMaterialIssueReversal> SilaCulinaryMaterialIssueReversal { get; set; }
        public DbSet<SilaCulinaryWasteEvent> SilaCulinaryWasteEvent { get; set; }
        public DbSet<SilaCulinaryFoodLeftover> SilaCulinaryFoodLeftover { get; set; }
        public DbSet<SilaCulinaryWasteDisposal> SilaCulinaryWasteDisposal { get; set; }
        public DbSet<SilaCulinaryProductionOutputConfirmation> SilaCulinaryProductionOutputConfirmation { get; set; }
        public DbSet<SilaCulinaryPortionSample> SilaCulinaryPortionSample { get; set; }
        public DbSet<SilaCulinaryProductionReconciliation> SilaCulinaryProductionReconciliation { get; set; }
        public DbSet<SilaCulinaryProductionReconciliationLine> SilaCulinaryProductionReconciliationLine { get; set; }
        public DbSet<SilaCulinaryLaborAllocation> SilaCulinaryLaborAllocation { get; set; }
        public DbSet<SilaCulinaryCentralKitchenAllocation> SilaCulinaryCentralKitchenAllocation { get; set; }
        public DbSet<SilaCulinaryBuffetReconciliation> SilaCulinaryBuffetReconciliation { get; set; }
        public DbSet<SilaCulinaryEventClosure> SilaCulinaryEventClosure { get; set; }
        public DbSet<SilaCulinaryErpReconciliationRef> SilaCulinaryErpReconciliationRef { get; set; }
        public DbSet<SilaCulinaryHistoricalLearningSnapshot> SilaCulinaryHistoricalLearningSnapshot { get; set; }
        public DbSet<SilaCulinaryReconciliationAudit> SilaCulinaryReconciliationAudit { get; set; }


        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            _ = modelBuilder.HasDefaultSchema(_configuration[Common.APPLICATION_SCHEMA]);
            _ = modelBuilder.Entity<BuyerCategory>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerBankAccount>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerBusinessProfile>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerDeliveryLocation>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerRegistration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Asset>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQ>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQAttachmentMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQItemAttachmentMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQQuestion>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQQuestionAnswer>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQAnswerOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RFQQuestionOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ItemBuyerMaster>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SupplierVerificationRequest>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationAnswer>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationAnswerOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationTemplate>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationTemplateQuestion>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<VerificationTemplateQuestionOption>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerDepartment>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<BuyerCostCenter>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<BuyerSupplierMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQSupplierMapping>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<DefaultVerificationTemplateQuestion>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<DefaultVerificationTemplate>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQQuestionAttachmentMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQBlockchainRecord>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQOrganizationUserMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ExternalSupplier>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQExternalSupplier>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<MessageThread>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<Message>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<MessageAttachment>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<PredefinedMaterial>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<MasterApprovalFlow>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ApprovalFlowUserMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<PredefinedMaterialApprovalFlowUserMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ApprovalFlowPredefinedMaterialMapping>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<ExcelMaterialMaster>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQAward>().HasIndex(a=>a.IsActive);
            _ =  modelBuilder.Entity<RFQAwardItem>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<PredefinedContract>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<PredefinedContractAttachment>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<PredefinedContractApprovalFlow>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<PredefinedContractApprovalUserMapping>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ContractTemplate>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ContractDetails>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<ContractAttachment>().HasIndex(a=>a.IsActive);
            _ = modelBuilder.Entity<BuyerOutlet>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerOutletUserMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerOutletUserMapping>().HasIndex(a => a.UserId);
            _ = modelBuilder.Entity<BuyerProperty>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<BuyerProperty>().HasIndex(a => new { a.BuyerId, a.PlantCode }).IsUnique();
            _ = modelBuilder.Entity<CatalogMaterialMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<CatalogMaterialMapping>().HasIndex(a => new { a.BuyerId, a.CatalogId }).IsUnique();
            _ = modelBuilder.Entity<PersonalWishlist>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PersonalWishlist>().HasIndex(a => a.OwnerUserId);
            _ = modelBuilder.Entity<PersonalWishlistItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<WeeklyBucket>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<WeeklyBucket>().HasIndex(a => new { a.BuyerId, a.PropertyId, a.Year, a.WeekNumber }).IsUnique();
            _ = modelBuilder.Entity<WeeklyBucketItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<WeeklyBucketRecommendation>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<WeeklyBucketRecommendation>().HasIndex(a => a.WeeklyBucketItemId);
            _ = modelBuilder.Entity<WeeklyBucketApprovalFlow>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<WeeklyBucketApprovalUserMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<WeeklyBucketAudit>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ErpIntegrationConfiguration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PurchaseDocumentIntegration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PurchaseDocumentIntegration>()
                .HasIndex(a => new { a.WeeklyBucketId, a.IntegrationType, a.SupplierOrganizationId })
                .HasFilter("[weekly_bucket_id] IS NOT NULL")
                .IsUnique();
            // One contract hand-off per contract: a second concurrent send cannot create a second row.
            _ = modelBuilder.Entity<PurchaseDocumentIntegration>()
                .HasIndex(a => new { a.ContractId, a.IntegrationType })
                .HasFilter("[integration_type] = 'CONTRACT_CREATE'")
                .IsUnique();
            _ = modelBuilder.Entity<PurchaseDocumentIntegration>()
                .HasIndex(a => new { a.PurchaseOrderId, a.IntegrationType })
                .HasFilter("[purchase_order_id] IS NOT NULL")
                .IsUnique();
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => a.SupplierId);
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => new { a.WeeklyBucketId, a.SupplierId })
                .HasFilter("[weekly_bucket_id] IS NOT NULL")
                .IsUnique();
            _ = modelBuilder.Entity<PurchaseOrder>().HasIndex(a => a.ContractId);
            _ = modelBuilder.Entity<PurchaseOrderItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().HasIndex(a => a.IsActive);
            // Stored as text so a new API type such as POST_GRN_REVERSAL does not fail the whole list.
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().Property(a => a.ProcessType).HasMaxLength(30).HasColumnType("nvarchar(30)");
            // A Buyer-side configuration belongs to a buyer (BuyerId); OrganizationId is the Supplier-side owner and has no column here.
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().Ignore(a => a.OrganizationId);
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().HasIndex(a => new { a.BuyerId, a.EntityCode, a.ProcessType }).IsUnique();
            _ = modelBuilder.Entity<ApiIntegrationConfiguration>().HasIndex(a => new { a.Status, a.NextRunAt });
            _ = modelBuilder.Entity<IntegrationSchemaSnapshot>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiFieldMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiFieldMapping>().HasIndex(a => new { a.ConfigurationId, a.SourceField, a.TargetField }).IsUnique();
            _ = modelBuilder.Entity<ApiIntegrationExecution>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<ApiIntegrationExecution>().HasIndex(a => new { a.ConfigurationId, a.StartedAt });
            _ = modelBuilder.Entity<MaterialUomConversion>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<MaterialUomConversion>().HasIndex(a => new { a.MaterialId, a.FromUom, a.ToUom }).IsUnique();
            _ = modelBuilder.Entity<InventoryLocation>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryLocation>().HasIndex(a => new { a.BuyerId, a.LocationCode }).IsUnique();
            _ = modelBuilder.Entity<InventoryLocation>().HasIndex(a => a.PropertyId);
            _ = modelBuilder.Entity<InventoryLocationUserMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryLocationUserMapping>().HasIndex(a => new { a.LocationId, a.UserId }).IsUnique();
            _ = modelBuilder.Entity<InventoryLocationUserMapping>().HasIndex(a => a.UserId);
            _ = modelBuilder.Entity<InventoryLocationMaterial>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryLocationMaterial>().HasIndex(a => new { a.LocationId, a.MaterialId }).IsUnique();
            _ = modelBuilder.Entity<InventoryBalance>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryBalance>().HasIndex(a => new { a.LocationId, a.MaterialId }).IsUnique();
            _ = modelBuilder.Entity<InventoryBalance>().HasIndex(a => new { a.BuyerId, a.MaterialId });
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => new { a.LocationId, a.MaterialId });
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => new { a.ReferenceType, a.ReferenceId });
            _ = modelBuilder.Entity<InventoryErpPosting>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryErpPosting>().HasIndex(a => a.Status);
            _ = modelBuilder.Entity<InventoryErpPosting>().HasIndex(a => new { a.ReferenceType, a.ReferenceId });
            _ = modelBuilder.Entity<InternalTransferOrder>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InternalTransferOrder>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<InternalTransferOrderItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryWorkflowEvent>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryWorkflowEvent>().HasIndex(a => new { a.ReferenceType, a.ReferenceId });
            _ = modelBuilder.Entity<GoodsIssue>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<GoodsIssueItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockAdjustment>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockAdjustmentItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockCount>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockCount>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<StockCountItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockShortageEnquiry>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<StockShortageEnquiry>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<StockShortageEnquiry>().HasIndex(a => a.StockCountId);
            _ = modelBuilder.Entity<InventoryAlert>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InventoryAlert>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<Recipe>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Recipe>().HasIndex(a => new { a.BuyerId, a.RecipeCode }).IsUnique();
            _ = modelBuilder.Entity<Recipe>().HasIndex(a => new { a.BuyerId, a.PosCode });
            _ = modelBuilder.Entity<RecipeIngredient>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeOutletPrice>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PosSalesBatch>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => new { a.BuyerId, a.SourceTransactionId, a.LineNumber }).IsUnique();
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => a.PurchaseOrderId);
            _ = modelBuilder.Entity<GoodsReceiptItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => new { a.BuyerId, a.SupplierName, a.InvoiceNumber });
            _ = modelBuilder.Entity<RecipeFamily>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeFamily>().HasIndex(a => new { a.BuyerId, a.Code }).IsUnique();
            _ = modelBuilder.Entity<RecipeCategory>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeCategory>().HasIndex(a => new { a.BuyerId, a.Code }).IsUnique();
            _ = modelBuilder.Entity<GoodsIssueCostCenter>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<GoodsIssueCostCenter>().HasIndex(a => new { a.BuyerId, a.PropertyId, a.Scope, a.StorageLocationCode }).IsUnique();
            _ = modelBuilder.Entity<SilaApprovalStep>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaApprovalStep>().HasIndex(a => new { a.ReferenceType, a.ReferenceId, a.Version });
            _ = modelBuilder.Entity<SilaApprovalStep>().HasIndex(a => new { a.UserId, a.Status });
            _ = modelBuilder.Entity<PosSource>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PosOutletMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PosOutletMapping>().HasIndex(a => new { a.PosSourceId, a.PosOutletCode }).IsUnique();
            _ = modelBuilder.Entity<PosItemMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PosItemMapping>().HasIndex(a => new { a.PosSourceId, a.PosItemCode }).IsUnique();
            _ = modelBuilder.Entity<MaterialPriceChange>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<MaterialPriceChange>().HasIndex(a => new { a.BuyerId, a.RequestNumber }).IsUnique();
            _ = modelBuilder.Entity<MaterialPriceChange>().HasIndex(a => new { a.MaterialId, a.Status });
            _ = modelBuilder.Entity<MaterialOutletPrice>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<MaterialOutletPrice>().HasIndex(a => new { a.BuyerId, a.MaterialId, a.OutletLocationId }).IsUnique();
            _ = modelBuilder.Entity<InternalPurchaseRequest>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InternalPurchaseRequest>().HasIndex(a => new { a.BuyerId, a.RequestNumber }).IsUnique();
            _ = modelBuilder.Entity<InternalPurchaseRequest>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<QuickTransferPolicy>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<QuickTransferPolicy>().HasIndex(a => a.BuyerId).IsUnique();
            _ = modelBuilder.Entity<PhysicalInventoryRequest>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<PhysicalInventoryRequest>().HasIndex(a => new { a.BuyerId, a.RequestNumber }).IsUnique();
            _ = modelBuilder.Entity<PhysicalInventoryRequest>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<LocationMaterialThreshold>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<LocationMaterialThreshold>().HasIndex(a => new { a.LocationMaterialId, a.Month }).IsUnique();
            _ = modelBuilder.Entity<SilaSupplier>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaSupplier>().HasIndex(a => new { a.BuyerId, a.SupplierCode }).IsUnique();
            _ = modelBuilder.Entity<CompanyCodeMaster>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<CompanyCodeMaster>().HasIndex(a => new { a.BuyerId, a.Code }).IsUnique();
            _ = modelBuilder.Entity<InvoiceExtraction>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<InvoiceExtraction>().HasIndex(a => new { a.InvoiceId, a.Attempt });
            _ = modelBuilder.Entity<SilaOcrConfiguration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaOcrConfiguration>().HasIndex(a => a.BuyerId).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryAiConfiguration>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryAiConfiguration>().HasIndex(a => a.BuyerId).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryRecipeDraft>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryRecipeDraft>().HasIndex(a => new { a.BuyerId, a.DraftReference }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryRecipeDraft>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryRecipeRevision>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryRecipeRevision>().HasIndex(a => new { a.DraftId, a.RevisionNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryAiUsageLog>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryAiUsageLog>().HasIndex(a => new { a.BuyerId, a.DateCreated });
            _ = modelBuilder.Entity<SilaCulinaryMaterialMapping>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialMapping>().HasIndex(a => new { a.BuyerId, a.DraftId, a.IngredientTempRef }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMaterialMapping>().HasIndex(a => new { a.BuyerId, a.MappingStatus });
            _ = modelBuilder.Entity<SilaCulinaryMaterialMappingHistory>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialMappingHistory>().HasIndex(a => new { a.BuyerId, a.DraftId, a.IngredientTempRef });
            _ = modelBuilder.Entity<SilaCulinaryMaterialAlias>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialAlias>().HasIndex(a => new { a.BuyerId, a.NormalizedAlias, a.PropertyId });
            _ = modelBuilder.Entity<SilaCulinaryMaterialAlias>().HasIndex(a => new { a.BuyerId, a.ApprovalStatus });
            _ = modelBuilder.Entity<SilaCulinaryMaterialFeedback>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialFeedback>().HasIndex(a => new { a.BuyerId, a.FeedbackType });
            _ = modelBuilder.Entity<SilaCulinaryMaterialIndexState>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialIndexState>().HasIndex(a => a.BuyerId).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequest>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequest>().HasIndex(a => new { a.BuyerId, a.RequestReference }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequest>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequestLink>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequestLink>().HasIndex(a => new { a.BuyerId, a.MaterialRequestId, a.DraftId, a.IngredientTempRef }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequestHistory>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequestHistory>().HasIndex(a => new { a.BuyerId, a.MaterialRequestId });
            _ = modelBuilder.Entity<SilaCulinaryCostingSnapshot>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryCostingSnapshot>().HasIndex(a => new { a.BuyerId, a.CostingReference }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryCostingSnapshot>().HasIndex(a => new { a.BuyerId, a.DraftId });
            _ = modelBuilder.Entity<SilaCulinaryRecipeLifecycle>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaCulinaryRecipeLifecycle>().HasIndex(a => new { a.BuyerId, a.DraftId }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryRecipeLifecycle>().HasIndex(a => new { a.BuyerId, a.ProductionRecipeId });
            _ = modelBuilder.Entity<RecipeSubstitutionProposal>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeSubstitutionProposal>().HasIndex(a => new { a.BuyerId, a.ProposalNumber }).IsUnique();
            _ = modelBuilder.Entity<RecipeSubstitutionProposal>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<StockCountPhoto>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaDocumentSequence>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaDocumentSequence>().HasIndex(a => new { a.BuyerId, a.Prefix }).IsUnique();
            _ = modelBuilder.Entity<InternalTransferOrder>().HasIndex(a => new { a.BuyerId, a.ItoNumber }).IsUnique();
            _ = modelBuilder.Entity<GoodsIssue>().HasIndex(a => new { a.BuyerId, a.IssueNumber }).IsUnique();
            _ = modelBuilder.Entity<StockAdjustment>().HasIndex(a => new { a.BuyerId, a.AdjustmentNumber }).IsUnique();
            _ = modelBuilder.Entity<StockCount>().HasIndex(a => new { a.BuyerId, a.CountNumber }).IsUnique();
            _ = modelBuilder.Entity<StockShortageEnquiry>().HasIndex(a => new { a.BuyerId, a.EnquiryNumber }).IsUnique();
            _ = modelBuilder.Entity<PosSalesBatch>().HasIndex(a => new { a.BuyerId, a.BatchNumber }).IsUnique();
            _ = modelBuilder.Entity<RecipeLegacyBatch>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeLegacyBatch>().HasIndex(a => new { a.BuyerId, a.BatchNumber }).IsUnique();
            _ = modelBuilder.Entity<RecipeLegacyRecipe>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeLegacyRecipe>().HasIndex(a => new { a.BuyerId, a.BatchId, a.LegacyRecipeId }).IsUnique();
            _ = modelBuilder.Entity<RecipeLegacyRecipe>().HasIndex(a => new { a.BatchId, a.ValidationStatus });
            _ = modelBuilder.Entity<RecipeLegacyMaterialMap>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeLegacyMaterialMap>().HasIndex(a => new { a.BatchId, a.LegacyRecipeId, a.IngredientLine });
            _ = modelBuilder.Entity<RecipeLegacyImage>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<RecipeLegacyImage>().HasIndex(a => new { a.BatchId, a.Status });
            _ = modelBuilder.Entity<SilaUserPurchasingProfile>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaUserPurchasingProfile>().HasIndex(a => new { a.BuyerId, a.UserId, a.Status });
            _ = modelBuilder.Entity<SilaUserPurchasingProfile>().HasIndex(a => new { a.BuyerId, a.UserId, a.IsDefault });
            _ = modelBuilder.Entity<SilaUserPoFlexConfig>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaUserPoFlexConfig>().HasIndex(a => new { a.BuyerId, a.UserId, a.ConfigType, a.Status });
            _ = modelBuilder.Entity<SilaUserPoFlexParam>().HasIndex(a => a.ConfigId);
            _ = modelBuilder.Entity<SilaUserPoFlexCondition>().HasIndex(a => a.ConfigId);
            _ = modelBuilder.Entity<SilaVoicePoSession>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaVoicePoSession>().HasIndex(a => new { a.BuyerId, a.SessionNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaVoicePoSession>().HasIndex(a => new { a.BuyerId, a.UserId, a.Status });
            _ = modelBuilder.Entity<SilaVoicePoSession>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey });
            _ = modelBuilder.Entity<SilaVoicePoTurn>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<SilaVoicePoTurn>().HasIndex(a => new { a.SessionId, a.TurnNumber }).IsUnique();
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => new { a.BuyerId, a.GrnNumber }).IsUnique();
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => new { a.BuyerId, a.TransactionNumber }).IsUnique();
            // SILA ME hardening: indexes for the location-scoped lists and the job lookups.
            _ = modelBuilder.Entity<InventoryTransaction>().HasIndex(a => new { a.BuyerId, a.BusinessDate });
            _ = modelBuilder.Entity<InternalTransferOrder>().HasIndex(a => new { a.BuyerId, a.FromLocationId, a.Status });
            _ = modelBuilder.Entity<InternalTransferOrder>().HasIndex(a => new { a.BuyerId, a.ToLocationId, a.Status });
            _ = modelBuilder.Entity<GoodsIssue>().HasIndex(a => new { a.BuyerId, a.FromLocationId });
            _ = modelBuilder.Entity<GoodsIssue>().HasIndex(a => new { a.BuyerId, a.ToLocationId });
            _ = modelBuilder.Entity<StockAdjustment>().HasIndex(a => new { a.BuyerId, a.LocationId });
            _ = modelBuilder.Entity<StockCount>().HasIndex(a => new { a.BuyerId, a.LocationId, a.Status });
            _ = modelBuilder.Entity<StockShortageEnquiry>().HasIndex(a => new { a.BuyerId, a.LocationId, a.Status });
            _ = modelBuilder.Entity<InventoryAlert>().HasIndex(a => new { a.LocationId, a.AlertType, a.MaterialId, a.Status });
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => new { a.BuyerId, a.BusinessDate });
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => a.BatchId);
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => a.ErpPostingId);
            _ = modelBuilder.Entity<PosSalesTransaction>().HasIndex(a => new { a.BuyerId, a.OriginalTransactionId });
            _ = modelBuilder.Entity<SilaApprovalStep>().HasIndex(a => new { a.BuyerId, a.ReferenceType, a.Status });
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<Invoice>().HasIndex(a => new { a.BuyerId, a.ContentHash });
            _ = modelBuilder.Entity<GoodsReceipt>().HasIndex(a => new { a.BuyerId, a.LocationId });
            _ = modelBuilder.Entity<PhysicalInventoryRequest>().HasIndex(a => new { a.BuyerId, a.LocationId, a.Status });
            _ = modelBuilder.Entity<InvoiceItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<CreditMemo>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<CreditMemo>().HasIndex(a => new { a.BuyerId, a.ReferenceInvoiceId });
            _ = modelBuilder.Entity<CreditMemoItem>().HasIndex(a => a.IsActive);
            _ = modelBuilder.Entity<CreditMemoItem>().HasIndex(a => a.CreditMemoId);
            _ = modelBuilder.Entity<OperationalTask>().HasIndex(a => new { a.BuyerId, a.DeduplicationKey }).IsUnique();
            _ = modelBuilder.Entity<OperationalTask>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<OperationalAlert>().HasIndex(a => new { a.BuyerId, a.DeduplicationKey }).IsUnique();
            _ = modelBuilder.Entity<OperationalAlert>().HasIndex(a => new { a.BuyerId, a.Status });

            _ = modelBuilder.HasSequence<long>(
                Common.PREDEFINED_CONTRACT_NUMBER_SEQUENCE,
                _configuration[Common.APPLICATION_SCHEMA]!);







            _ = modelBuilder.Entity<SilaCulinaryMaterialRequestLink>()
                .HasOne(x => x.MaterialRequest)
                .WithMany()
                .HasForeignKey(x => x.MaterialRequestId)
                .OnDelete(DeleteBehavior.NoAction);
            _ = modelBuilder.Entity<SilaCulinaryMaterialRequestHistory>()
                .HasOne(x => x.MaterialRequest)
                .WithMany()
                .HasForeignKey(x => x.MaterialRequestId)
                .OnDelete(DeleteBehavior.NoAction);

            _ = modelBuilder.Entity<SilaCulinaryEvent>().HasIndex(a => new { a.BuyerId, a.EventCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryEvent>().HasIndex(a => new { a.BuyerId, a.StartDateUtc, a.EndDateUtc });
            _ = modelBuilder.Entity<SilaCulinaryEvent>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryEventSession>().HasIndex(a => new { a.BuyerId, a.EventId, a.SessionDateUtc });
            _ = modelBuilder.Entity<SilaCulinaryEventTemplate>().HasIndex(a => new { a.BuyerId, a.TemplateCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMenu>().HasIndex(a => new { a.BuyerId, a.MenuCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMenu>().HasIndex(a => new { a.BuyerId, a.EventId });
            _ = modelBuilder.Entity<SilaCulinaryMenuLine>().HasIndex(a => new { a.BuyerId, a.MenuId, a.SortOrder });
            _ = modelBuilder.Entity<SilaCulinaryEventRevision>().HasIndex(a => new { a.BuyerId, a.EventId, a.RevisionNumber });
            _ = modelBuilder.Entity<SilaCulinaryMrpRun>().HasIndex(a => new { a.BuyerId, a.MrpCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMrpRun>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryMrpRun>().HasIndex(a => new { a.BuyerId, a.PropertyId });
            _ = modelBuilder.Entity<SilaCulinaryMrpLine>().HasIndex(a => new { a.BuyerId, a.MrpRunId, a.RequiredDateBucket });
            _ = modelBuilder.Entity<SilaCulinaryMrpLine>().HasIndex(a => new { a.BuyerId, a.MaterialId });
            _ = modelBuilder.Entity<SilaCulinaryMrpException>().HasIndex(a => new { a.BuyerId, a.MrpRunId, a.ResolutionStatus });
            _ = modelBuilder.Entity<SilaCulinaryMrpScenario>().HasIndex(a => new { a.BuyerId, a.MrpRunId });
            _ = modelBuilder.Entity<SilaCulinaryMrpSnapshot>().HasIndex(a => new { a.BuyerId, a.MrpRunId, a.VersionNumber });
            _ = modelBuilder.Entity<SilaCulinaryProcurementPlan>().HasIndex(a => new { a.BuyerId, a.PlanCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProcurementPlan>().HasIndex(a => new { a.BuyerId, a.MrpRunId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryProcurementPlan>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey });
            _ = modelBuilder.Entity<SilaCulinaryProcurementLine>().HasIndex(a => new { a.BuyerId, a.ProcurementPlanId, a.MaterialCode });
            _ = modelBuilder.Entity<SilaCulinaryStockReservation>().HasIndex(a => new { a.BuyerId, a.ReservationCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryStockReservation>().HasIndex(a => new { a.BuyerId, a.LocationId, a.MaterialId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryStockReservation>().HasIndex(a => new { a.BuyerId, a.EventId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryProcurementAllocation>().HasIndex(a => new { a.BuyerId, a.ProcurementPlanId, a.AllocationType });
            _ = modelBuilder.Entity<SilaCulinaryProcurementAllocation>().HasIndex(a => new { a.BuyerId, a.PurchaseOrderItemId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryProcurementAllocation>().HasIndex(a => new { a.BuyerId, a.InternalTransferOrderId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryProcurementAllocation>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey });
            _ = modelBuilder.Entity<SilaCulinaryProcurementException>().HasIndex(a => new { a.BuyerId, a.ProcurementPlanId, a.ResolutionStatus });
            _ = modelBuilder.Entity<SilaCulinaryProcurementScenario>().HasIndex(a => new { a.BuyerId, a.ProcurementPlanId });
            _ = modelBuilder.Entity<SilaCulinaryFinancialConfig>().HasIndex(a => a.BuyerId).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryEventFinancialPlan>().HasIndex(a => new { a.BuyerId, a.PlanCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryEventFinancialPlan>().HasIndex(a => new { a.BuyerId, a.EventId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryEventFinancialPlan>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey });
            _ = modelBuilder.Entity<SilaCulinaryEventRevenueLine>().HasIndex(a => new { a.BuyerId, a.EventFinancialPlanId });
            _ = modelBuilder.Entity<SilaCulinaryEventExpenseLine>().HasIndex(a => new { a.BuyerId, a.EventFinancialPlanId, a.Category });
            _ = modelBuilder.Entity<SilaCulinaryFinancialException>().HasIndex(a => new { a.BuyerId, a.EventFinancialPlanId, a.ResolutionStatus });
            _ = modelBuilder.Entity<SilaCulinaryFinancialScenario>().HasIndex(a => new { a.BuyerId, a.EventFinancialPlanId });
            _ = modelBuilder.Entity<SilaCulinaryAnalyticsConfig>().HasIndex(a => new { a.BuyerId, a.PropertyId }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinarySimulation>().HasIndex(a => new { a.BuyerId, a.SimulationCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryOptimizationJob>().HasIndex(a => new { a.BuyerId, a.JobCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryOptimizationJob>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryRecommendation>().HasIndex(a => new { a.BuyerId, a.RecommendationCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryRecommendation>().HasIndex(a => new { a.BuyerId, a.DeduplicationKey });
            _ = modelBuilder.Entity<SilaCulinaryRecommendation>().HasIndex(a => new { a.BuyerId, a.Status });
            _ = modelBuilder.Entity<SilaCulinaryWasteRecord>().HasIndex(a => new { a.BuyerId, a.WasteNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryWasteRecord>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey });
            _ = modelBuilder.Entity<SilaCulinaryAnalyticsAudit>().HasIndex(a => new { a.BuyerId, a.ExecutedAtUtc });
            _ = modelBuilder.Entity<SilaCulinaryKitchen>().HasIndex(a => new { a.BuyerId, a.KitchenCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryWorkstation>().HasIndex(a => new { a.BuyerId, a.KitchenId, a.WorkstationCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryEquipment>().HasIndex(a => new { a.BuyerId, a.KitchenId, a.EquipmentCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryStaffAssignment>().HasIndex(a => new { a.BuyerId, a.KitchenId, a.UserId });
            _ = modelBuilder.Entity<SilaCulinaryProductionDemand>().HasIndex(a => new { a.BuyerId, a.SourceDocumentKey }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionPlan>().HasIndex(a => new { a.BuyerId, a.PlanCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionOrder>().HasIndex(a => new { a.BuyerId, a.OrderNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionBatch>().HasIndex(a => new { a.BuyerId, a.BatchNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionMaterialIssueRef>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionException>().HasIndex(a => new { a.BuyerId, a.ExceptionCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionDispatch>().HasIndex(a => new { a.BuyerId, a.DispatchNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionAudit>().HasIndex(a => new { a.BuyerId, a.ExecutedAtUtc });
            _ = modelBuilder.Entity<SilaCulinaryProductionConsumption>().HasIndex(a => new { a.BuyerId, a.ConsumptionNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionConsumption>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryAdditionalMaterialRequest>().HasIndex(a => new { a.BuyerId, a.RequestNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMaterialReturn>().HasIndex(a => new { a.BuyerId, a.ReturnNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryMaterialIssueReversal>().HasIndex(a => new { a.BuyerId, a.ReversalNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryWasteEvent>().HasIndex(a => new { a.BuyerId, a.WasteCode }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryFoodLeftover>().HasIndex(a => new { a.BuyerId, a.LeftoverNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryWasteDisposal>().HasIndex(a => new { a.BuyerId, a.DisposalNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionOutputConfirmation>().HasIndex(a => new { a.BuyerId, a.ConfirmationNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryProductionReconciliation>().HasIndex(a => new { a.BuyerId, a.ReconciliationNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryEventClosure>().HasIndex(a => new { a.BuyerId, a.ClosureNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryErpReconciliationRef>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryHistoricalLearningSnapshot>().HasIndex(a => new { a.BuyerId, a.SnapshotNumber }).IsUnique();
            _ = modelBuilder.Entity<SilaCulinaryReconciliationAudit>().HasIndex(a => new { a.BuyerId, a.ExecutedAtUtc });
            _ = modelBuilder.Entity<MenuItemVersion>().HasIndex(a => new { a.BuyerId, a.MenuItemId, a.VersionNumber });
            _ = modelBuilder.Entity<MenuEngineeringScenario>().HasIndex(a => new { a.BuyerId, a.MenuPlanId });
            _ = modelBuilder.Entity<MenuEngineeringApprovalHistory>().HasIndex(a => new { a.BuyerId, a.EntityType, a.EntityId, a.ActionAtUtc });
            _ = modelBuilder.Entity<MenuDemandOverride>().HasIndex(a => new { a.BuyerId, a.BusinessDate, a.OutletLocationId });
            _ = modelBuilder.Entity<MenuEngineeringConfigHistory>().HasIndex(a => new { a.BuyerId, a.PropertyId, a.ConfigVersion });
            _ = modelBuilder.Entity<MenuEngineeringAgentRun>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey }).IsUnique();
            _ = modelBuilder.Entity<MenuEngineeringRecommendation>().HasIndex(a => new { a.BuyerId, a.Status, a.Priority });
            _ = modelBuilder.Entity<MenuEngineeringProactiveAlert>().HasIndex(a => new { a.BuyerId, a.DeduplicationKey });
            _ = modelBuilder.Entity<MenuEngineeringLearningOutcome>().HasIndex(a => new { a.BuyerId, a.DatasetVersion, a.CapturedAtUtc });
            _ = modelBuilder.Entity<MenuEngineeringModelEvaluation>().HasIndex(a => new { a.BuyerId, a.ModelName, a.EvaluatedAtUtc });
            _ = modelBuilder.Entity<MenuEngineeringAiFeedback>().HasIndex(a => new { a.BuyerId, a.AgentId, a.SubmittedAtUtc });
            _ = modelBuilder.Entity<MenuEngineeringTwinSnapshot>().HasIndex(a => new { a.BuyerId, a.SnapshotType, a.CapturedAtUtc });
            _ = modelBuilder.Entity<MenuEngineeringTwinSnapshot>().HasIndex(a => new { a.BuyerId, a.MenuPlanId, a.VersionNumber });
            _ = modelBuilder.Entity<MenuEngineeringTwinSimulationRun>().HasIndex(a => new { a.BuyerId, a.IdempotencyKey }).IsUnique();
            _ = modelBuilder.Entity<MenuEngineeringTwinSimulationRun>().HasIndex(a => new { a.BuyerId, a.RunType, a.Status });
            _ = modelBuilder.Entity<MenuEngineeringModelDriftEvent>().HasIndex(a => new { a.BuyerId, a.ModelName, a.DetectedAtUtc });

            base.OnModelCreating(modelBuilder);

            foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity in modelBuilder.Model.GetEntityTypes())
            {

                entity.SetTableName(entity.GetTableName()!.ConvertToSnakeCase());
                var storeObjectIdentifier = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableProperty property in entity.GetProperties())
                {

                    property.SetColumnName(property.GetColumnName(storeObjectIdentifier)!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableKey key in entity.GetKeys())
                {
                    key.SetName(key.GetName()!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey key in entity.GetForeignKeys())
                {
                    key.SetConstraintName(key.GetConstraintName()!.ConvertToSnakeCase());
                }

                foreach (Microsoft.EntityFrameworkCore.Metadata.IMutableIndex index in entity.GetIndexes())
                {
                    index.SetDatabaseName(index.GetDatabaseName()!.ConvertToSnakeCase());
                }
            }
        }
        public void OnBeforeSaving(Guid UserId)
        {
            System.Collections.Generic.IEnumerable<Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry> entries = ChangeTracker.Entries();
            foreach (Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry in entries)
            {
                if (entry.Entity is BaseModel trackable)
                {
                    DateTime now = DateTime.UtcNow;
                    Guid user = UserId;
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            trackable.DateUpdated = now;
                            trackable.UpdatedBy = user;
                            break;
                        case EntityState.Added:
                            trackable.DateCreated = now;
                            trackable.CreatedBy = user;
                            trackable.DateUpdated = now;
                            trackable.UpdatedBy = user;
                            trackable.IsActive = true;
                            break;
                        case EntityState.Detached:
                            break;
                        case EntityState.Unchanged:
                            break;
                        case EntityState.Deleted:
                            break;
                        default:
                            break;
                    }
                }
            }
        }




    }
}