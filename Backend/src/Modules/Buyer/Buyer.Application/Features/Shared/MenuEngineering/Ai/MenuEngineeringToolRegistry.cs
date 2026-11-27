using Buyer.Domain.Dtos;

namespace Buyer.Application.Features.Shared.MenuEngineering.Ai
{
    public static class MenuEngineeringToolRegistry
    {
        public static readonly IReadOnlyList<MenuEngineeringBusinessToolDescriptorDto> Tools = new List<MenuEngineeringBusinessToolDescriptorDto>
        {
            new() { ToolId = "RetrievePropertyOutlet", DisplayName = "Resolve Property & Outlet", Category = "CONTEXT", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "InventoryLocation / BuyerOutlet" },
            new() { ToolId = "RetrieveApprovedMenuItems", DisplayName = "Fetch Approved Menu Items", Category = "MENU_ITEMS", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringStudioService" },
            new() { ToolId = "RetrieveHistoricalMenus", DisplayName = "Fetch Historical Comparable Menus", Category = "HISTORY", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringStudioService" },
            new() { ToolId = "RetrieveApprovedRecipes", DisplayName = "Fetch Approved Recipe Versions", Category = "CULINARY", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "Recipe / SilaRecipeRules" },
            new() { ToolId = "RetrieveIngredientPrices", DisplayName = "Fetch Current Ingredient Prices", Category = "COSTING", RequiredPermission = "VIEW_SILA_MENU_COST", BackingService = "ItemBuyerMaster / SilaMaterialPricing" },
            new() { ToolId = "RetrieveInventoryAvailability", DisplayName = "Check Live Inventory Availability", Category = "INVENTORY", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "InventoryBalance" },
            new() { ToolId = "RetrieveIncomingSupply", DisplayName = "Check Expected Incoming PO Supply", Category = "PROCUREMENT", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "PurchaseOrder" },
            new() { ToolId = "ValidateDietaryAllergens", DisplayName = "Validate Allergen & Dietary Constraints", Category = "SAFETY", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "RecipeIngredient / MenuItem" },
            new() { ToolId = "CalculatePortionsAndDemand", DisplayName = "Calculate Portions & Backtested Demand", Category = "DEMAND", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringStudioService + Python /v1/me-ai/backtest-forecast" },
            new() { ToolId = "CalculateMenuCostAndMargin", DisplayName = "Calculate Authoritative Cost & Margin", Category = "FINANCIAL", RequiredPermission = "VIEW_SILA_MENU_COST", BackingService = "SilaCulinaryFinancialService / MenuEngineeringStudioService" },
            new() { ToolId = "IdentifyProcurementShortages", DisplayName = "Compute Time-Phased Ingredient Shortages", Category = "PROCUREMENT", RequiredPermission = "GENERATE_SILA_MENU_PURCHASE_REC", BackingService = "SilaPurchaseRecommendation / SilaCulinaryProcurementService" },
            new() { ToolId = "IdentifyProductionConstraints", DisplayName = "Check Kitchen Production Constraints", Category = "PRODUCTION", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "SilaCulinaryProductionService" },
            new() { ToolId = "GenerateOptimizedScenarios", DisplayName = "Run Multi-Constraint MILP 7-Scenario Optimizer", Category = "OPTIMIZATION", RequiredPermission = "USE_SILA_AI_MENU_INTELLIGENCE", BackingService = "Python /v1/me-ai/optimize-scenarios" },
            new() { ToolId = "RunWhatIfSimulation", DisplayName = "Run Read-Only What-If Simulation", Category = "SIMULATION", RequiredPermission = "RUN_SILA_MENU_SIMULATION", BackingService = "MenuEngineeringStudioService" },
            new() { ToolId = "SaveDraftMenu", DisplayName = "Save Proposed Menu as Controlled Draft", Category = "LIFECYCLE", RequiredPermission = "CREATE_SILA_MENU", MutatesDraftState = true, RequiresApproval = false, BackingService = "MenuEngineeringStudioService" },
            new() { ToolId = "SubmitMenuForApproval", DisplayName = "Submit Draft Menu for Approval", Category = "APPROVAL", RequiredPermission = "EDIT_SILA_DRAFT_MENU", MutatesDraftState = true, RequiresApproval = true, BackingService = "MenuEngineeringStudioService" },
            new() { ToolId = "SuggestPosMappings", DisplayName = "Run Fuzzy+Semantic POS Mapping Suggestions", Category = "POS", RequiredPermission = "MANAGE_SILA_POS_MAPPING", BackingService = "Python /v1/me-ai/suggest-pos-mapping" },
            new() { ToolId = "DetectConsumptionAnomalies", DisplayName = "Rank Consumption Variance Root Causes", Category = "CONSUMPTION", RequiredPermission = "VIEW_SILA_MENU_ENGINEERING", BackingService = "SilaCulinaryReconciliationService" },
            new() { ToolId = "EvaluateIngredientPriceShock", DisplayName = "Cascade Reverse Recipe Price Shock", Category = "COSTING", RequiredPermission = "VIEW_SILA_MENU_COST", BackingService = "RecipeIngredient + MenuItem + MenuPlan" },
            new() { ToolId = "GeneratePricingRecommendations", DisplayName = "Compute Intelligent Pricing Ranges", Category = "PRICING", RequiredPermission = "VIEW_SILA_MENU_MARGIN", BackingService = "MenuEngineeringStudioService" },
            new() { ToolId = "EvaluateSetupReadiness", DisplayName = "Evaluate 10-Step ME Setup Readiness", Category = "SETUP", RequiredPermission = "MANAGE_SILA_MENU_SETUP", BackingService = "MenuEngineeringSettings + SilaCulinaryAiConfiguration" }
        };

        public static bool IsAuthorized(string toolId, MenuEngineeringPermissionSetDto perms)
        {
            MenuEngineeringBusinessToolDescriptorDto? tool = Tools.FirstOrDefault(t => string.Equals(t.ToolId, toolId, StringComparison.OrdinalIgnoreCase));
            if (tool is null) return false;
            return tool.RequiredPermission switch
            {
                "VIEW_SILA_ME_STUDIO" => perms.ViewMeStudio,
                "VIEW_SILA_MENU_COST" => perms.ViewMenuCost,
                "VIEW_SILA_MENU_MARGIN" => perms.ViewMenuMargin,
                "CREATE_SILA_MENU" => perms.CreateMenu,
                "EDIT_SILA_DRAFT_MENU" => perms.EditDraftMenu,
                "GENERATE_SILA_MENU_PURCHASE_REC" => perms.GeneratePurchaseRecommendation,
                "USE_SILA_AI_MENU_INTELLIGENCE" => perms.UseAiMenuIntelligence,
                "RUN_SILA_MENU_SIMULATION" => perms.RunMenuSimulation,
                "MANAGE_SILA_POS_MAPPING" => perms.ManagePosMapping,
                "MANAGE_SILA_MENU_SETUP" => perms.ManageMenuSetup,
                _ => perms.ViewMeStudio
            };
        }
    }
}
