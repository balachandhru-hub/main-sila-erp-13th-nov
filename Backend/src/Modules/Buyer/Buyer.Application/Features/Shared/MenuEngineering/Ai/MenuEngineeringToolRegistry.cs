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
            new() { ToolId = "EvaluateSetupReadiness", DisplayName = "Evaluate 10-Step ME Setup Readiness", Category = "SETUP", RequiredPermission = "MANAGE_SILA_MENU_SETUP", BackingService = "MenuEngineeringSettings + SilaCulinaryAiConfiguration" },
            new() { ToolId = "CaptureDigitalTwinSnapshot", DisplayName = "Capture / Refresh Digital Twin Snapshot", Category = "DIGITAL_TWIN", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/dependency-graph" },
            new() { ToolId = "RunMultiObjectiveOptimizer", DisplayName = "Run OR-Tools CP-SAT Multi-Objective Menu Optimizer", Category = "OPTIMIZATION", RequiredPermission = "USE_SILA_AI_MENU_INTELLIGENCE", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/optimize-multi" },
            new() { ToolId = "RunInventoryDrivenOptimizer", DisplayName = "Optimize Weekly Menu Using FEFO Eligible Stock", Category = "OPTIMIZATION", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/optimize-inventory-menu" },
            new() { ToolId = "RunMonteCarloSimulation", DisplayName = "Run Seeded Monte Carlo Risk Simulation", Category = "RISK", RequiredPermission = "RUN_SILA_MENU_SIMULATION", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/monte-carlo" },
            new() { ToolId = "RunSensitivityTornado", DisplayName = "Compute Tornado Sensitivity & Break-Even Risk", Category = "RISK", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/sensitivity" },
            new() { ToolId = "RunOperationalStressTest", DisplayName = "Run Guest Surge / Price Shock / Supply Delay Stress Tests", Category = "RISK", RequiredPermission = "RUN_SILA_MENU_SIMULATION", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/stress-test" },
            new() { ToolId = "PredictFoodWasteCategories", DisplayName = "Predict Separated Food Waste & Smaller-Batch Prep", Category = "WASTE", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/predict-waste" },
            new() { ToolId = "AnalyzeHistoricalSimilarity", DisplayName = "Retrieve Comparable Historical Menus & Events", Category = "LEARNING", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringDigitalTwinService + Python /v1/me-twin/historical-similarity" },
            new() { ToolId = "PublishBusinessEvent", DisplayName = "Publish Normalized Idempotent Business Event", Category = "AUTONOMOUS_EVENT", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringAutonomousService" },
            new() { ToolId = "GenerateConsolidatedActionPlan", DisplayName = "Consolidate Multi-Agent Findings into Action Plan", Category = "ACTION_PLAN", RequiredPermission = "USE_SILA_AI_MENU_INTELLIGENCE", MutatesDraftState = true, RequiresApproval = true, BackingService = "MenuEngineeringAutonomousService" },
            new() { ToolId = "ExecuteApprovedBusinessAction", DisplayName = "Execute Policy-Validated & Approved Action Item", Category = "EXECUTION", RequiredPermission = "APPROVE_SILA_MENU", MutatesDraftState = true, RequiresApproval = true, BackingService = "MenuEngineeringAutonomousService" },
            new() { ToolId = "TrackAndValidateRealizedBenefit", DisplayName = "Track Comparable-Baseline Adjusted Financial Benefits", Category = "BENEFIT", RequiredPermission = "VIEW_SILA_MENU_MARGIN", BackingService = "MenuEngineeringAutonomousService" },
            new() { ToolId = "MonitorIntegrationAndReadiness", DisplayName = "Evaluate SAP / OPERA / POS / Fatoora / Teams Health & Data Readiness", Category = "INTEGRATION", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringAutonomousService" },
            new() { ToolId = "GenerateExecutiveBriefing", DisplayName = "Generate Role-Aware Daily Executive Briefing", Category = "BRIEFING", RequiredPermission = "VIEW_SILA_ME_STUDIO", BackingService = "MenuEngineeringAutonomousService" }
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
