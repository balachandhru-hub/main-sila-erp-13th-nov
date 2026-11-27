using Buyer.Domain.Dtos;

namespace Buyer.Application.Features.Shared.MenuEngineering.Ai
{
    public static class MenuEngineeringAgentRegistry
    {
        public static readonly IReadOnlyList<MenuEngineeringAgentDescriptorDto> Agents = new List<MenuEngineeringAgentDescriptorDto>
        {
            new()
            {
                AgentId = "ME_STUDIO_ORCHESTRATOR",
                DisplayName = "ME Studio Orchestrator",
                EquivalentExistingAgent = "Phase 5 Orchestrator / SILA Voice MENU_ENGINEERING_AGENT",
                SupportedIntents = { "CREATE_MENU_FROM_OBJECTIVE", "OPTIMIZE_MENU", "CONVERSATIONAL_EDIT", "MULTI_AGENT_PLAN" },
                RequiredPermissions = { "VIEW_SILA_ME_STUDIO", "USE_SILA_AI_MENU_INTELLIGENCE" },
                AuthorizedTools = { "RetrievePropertyOutlet", "RetrieveApprovedMenuItems", "RetrieveHistoricalMenus", "RetrieveApprovedRecipes", "GenerateOptimizedScenarios", "RunWhatIfSimulation", "SaveDraftMenu", "SubmitMenuForApproval" },
                DataDependencies = { "MenuPlan", "MenuItem", "Recipe", "InventoryBalance", "MenuEngineeringSettings" },
                MaxSteps = 15,
                TokenBudget = 8000,
                TimeoutSeconds = 45,
                ModelTier = "ADVANCED",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(MeStudioMultiAgentRunResultDto)
            },
            new()
            {
                AgentId = "MENU_ITEM_INTELLIGENCE_AGENT",
                DisplayName = "Menu Item Intelligence Agent",
                EquivalentExistingAgent = "RecipeIntelligenceAgent",
                SupportedIntents = { "ASSIST_DISH_CREATION", "GENERATE_DESCRIPTION", "MATCH_RECIPE", "DETECT_DUPLICATE", "VALIDATE_ALLERGENS" },
                RequiredPermissions = { "MANAGE_SILA_MENU_ITEMS" },
                AuthorizedTools = { "RetrieveApprovedMenuItems", "RetrieveApprovedRecipes", "ValidateDietaryAllergens" },
                DataDependencies = { "MenuItem", "Recipe", "RecipeIngredient", "ItemBuyerMaster" },
                MaxSteps = 5,
                TokenBudget = 3000,
                TimeoutSeconds = 20,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(AiMenuItemAssistResultDto)
            },
            new()
            {
                AgentId = "MENU_PLANNING_AGENT",
                DisplayName = "Menu Planning Agent",
                EquivalentExistingAgent = "SilaCulinaryEventMenuService",
                SupportedIntents = { "GENERATE_WEEKLY_PLAN", "GENERATE_MONTHLY_PLAN", "DETECT_REPETITION", "ROTATE_BUFFET" },
                RequiredPermissions = { "CREATE_SILA_MENU", "EDIT_SILA_DRAFT_MENU" },
                AuthorizedTools = { "RetrieveApprovedMenuItems", "RetrieveHistoricalMenus", "RetrieveInventoryAvailability", "IdentifyProductionConstraints", "SaveDraftMenu" },
                DataDependencies = { "MenuPlan", "MenuPlanLine", "InventoryBalance", "SilaCulinaryEvent" },
                MaxSteps = 8,
                TokenBudget = 5000,
                TimeoutSeconds = 30,
                ModelTier = "ADVANCED",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(AiMenuCalendarGenerateResultDto)
            },
            new()
            {
                AgentId = "PORTION_OPTIMIZATION_AGENT",
                DisplayName = "Portion Optimization Agent",
                EquivalentExistingAgent = "SilaCulinaryReconciliationService",
                SupportedIntents = { "OPTIMIZE_PORTIONS", "DETECT_OVER_PORTIONING", "RECOMMEND_BUFFET_FACTORS" },
                RequiredPermissions = { "MANAGE_SILA_PORTION_PLANS" },
                AuthorizedTools = { "CalculatePortionsAndDemand", "RetrieveApprovedRecipes" },
                DataDependencies = { "MenuPlanLine", "SilaCulinaryPortionSample", "SilaCulinaryWasteEvent" },
                MaxSteps = 4,
                TokenBudget = 2500,
                TimeoutSeconds = 20,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(AiPortionOptimizeResultDto)
            },
            new()
            {
                AgentId = "DEMAND_FORECAST_AGENT",
                DisplayName = "Demand Forecast Agent",
                EquivalentExistingAgent = "SilaPmsAdapters / SilaCulinaryAnalyticsService",
                SupportedIntents = { "FORECAST_DEMAND", "BACKTEST_MODELS", "EXPLAIN_DRIVERS" },
                RequiredPermissions = { "MANAGE_SILA_DEMAND_PLANS" },
                AuthorizedTools = { "CalculatePortionsAndDemand", "RetrieveHistoricalMenus" },
                DataDependencies = { "PosSalesTransaction", "PmsDemandSnapshot", "SilaCulinaryEvent", "MenuDemandOverride" },
                MaxSteps = 5,
                TokenBudget = 3000,
                TimeoutSeconds = 25,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = false,
                OutputSchema = nameof(AiBacktestDemandResultDto)
            },
            new()
            {
                AgentId = "POS_SALES_INTELLIGENCE_AGENT",
                DisplayName = "POS Sales Intelligence Agent",
                EquivalentExistingAgent = "SilaPosProcessing",
                SupportedIntents = { "DETECT_SALES_ANOMALIES", "SUMMARIZE_DAILY_SALES", "EXPLAIN_SALES_TREND" },
                RequiredPermissions = { "VIEW_SILA_POS_SALES" },
                AuthorizedTools = { "RetrieveApprovedMenuItems" },
                DataDependencies = { "PosSalesTransaction", "PosSalesBatch", "MenuPosMapping" },
                MaxSteps = 4,
                TokenBudget = 2500,
                TimeoutSeconds = 20,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = false,
                OutputSchema = nameof(AiPosSalesIntelligenceDto)
            },
            new()
            {
                AgentId = "POS_MAPPING_AGENT",
                DisplayName = "POS Mapping Agent",
                EquivalentExistingAgent = "SilaPosMatching",
                SupportedIntents = { "SUGGEST_POS_MAPPINGS", "PARSE_COMBO_MODIFIERS", "DETECT_MAPPING_CONFLICTS" },
                RequiredPermissions = { "MANAGE_SILA_POS_MAPPING" },
                AuthorizedTools = { "SuggestPosMappings", "RetrieveApprovedMenuItems", "RetrieveApprovedRecipes" },
                DataDependencies = { "MenuPosMapping", "PosItemMapping", "PosSalesTransaction" },
                MaxSteps = 4,
                TokenBudget = 2500,
                TimeoutSeconds = 20,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(AiPosMappingSuggestionBatchDto)
            },
            new()
            {
                AgentId = "MENU_PERFORMANCE_AGENT",
                DisplayName = "Menu Performance Agent",
                EquivalentExistingAgent = "SilaCulinaryAnalyticsService (Matrix)",
                SupportedIntents = { "PREDICT_QUADRANT_MOVEMENT", "RECOMMEND_QUADRANT_PLAYBOOK" },
                RequiredPermissions = { "VIEW_SILA_MENU_PERFORMANCE" },
                AuthorizedTools = { "CalculateMenuCostAndMargin", "RetrieveHistoricalMenus" },
                DataDependencies = { "MenuItem", "PosSalesTransaction", "MenuPosMapping" },
                MaxSteps = 4,
                TokenBudget = 3000,
                TimeoutSeconds = 20,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = false,
                OutputSchema = nameof(MenuPerformanceMatrixDto)
            },
            new()
            {
                AgentId = "CONSUMPTION_INTELLIGENCE_AGENT",
                DisplayName = "Consumption Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryReconciliationService",
                SupportedIntents = { "DETECT_CONSUMPTION_ANOMALIES", "RANK_VARIANCE_ROOT_CAUSES" },
                RequiredPermissions = { "VIEW_SILA_MENU_ENGINEERING" },
                AuthorizedTools = { "DetectConsumptionAnomalies", "RetrieveApprovedRecipes", "RetrieveInventoryAvailability" },
                DataDependencies = { "InventoryTransaction", "PosSalesTransaction", "SilaCulinaryWasteEvent", "SilaCulinaryMaterialReturn" },
                MaxSteps = 5,
                TokenBudget = 3500,
                TimeoutSeconds = 25,
                ModelTier = "ADVANCED",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = false,
                OutputSchema = nameof(AiConsumptionRootCauseDto)
            },
            new()
            {
                AgentId = "COST_MARGIN_AGENT",
                DisplayName = "Cost and Margin Agent",
                EquivalentExistingAgent = "SilaCulinaryFinancialService",
                SupportedIntents = { "ANALYZE_PRICE_SHOCK", "RECOMMEND_PRICING", "EXPLAIN_MARGIN_EROSION" },
                RequiredPermissions = { "VIEW_SILA_MENU_COST", "VIEW_SILA_MENU_MARGIN" },
                AuthorizedTools = { "CalculateMenuCostAndMargin", "EvaluateIngredientPriceShock", "GeneratePricingRecommendations", "RetrieveIngredientPrices" },
                DataDependencies = { "Recipe", "RecipeIngredient", "ItemBuyerMaster", "MenuItem", "MenuPlan" },
                MaxSteps = 5,
                TokenBudget = 3500,
                TimeoutSeconds = 25,
                ModelTier = "ADVANCED",
                RiskLevel = "HIGH",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(AiPriceShockResultDto)
            },
            new()
            {
                AgentId = "PURCHASE_INTELLIGENCE_AGENT",
                DisplayName = "Purchase Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryProcurementService / SilaPurchaseRecommendation",
                SupportedIntents = { "PREDICT_SHORTAGES", "RECOMMEND_TRANSFERS_AND_SUBSTITUTES", "SUGGEST_PO_TIMING" },
                RequiredPermissions = { "GENERATE_SILA_MENU_PURCHASE_REC" },
                AuthorizedTools = { "IdentifyProcurementShortages", "RetrieveInventoryAvailability", "RetrieveIncomingSupply" },
                DataDependencies = { "InventoryBalance", "PurchaseOrder", "MenuPlanLine", "RecipeIngredient" },
                MaxSteps = 5,
                TokenBudget = 3000,
                TimeoutSeconds = 25,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "HIGH",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(MenuPurchasePlanDto)
            },
            new()
            {
                AgentId = "MENU_OPTIMIZATION_AGENT",
                DisplayName = "Menu Optimization Agent",
                EquivalentExistingAgent = "SilaCulinaryEventMenuService (MILP)",
                SupportedIntents = { "OPTIMIZE_FOOD_COST", "OPTIMIZE_MARGIN", "OPTIMIZE_INVENTORY_UTILIZATION", "GENERATE_7_SCENARIOS" },
                RequiredPermissions = { "USE_SILA_AI_MENU_INTELLIGENCE" },
                AuthorizedTools = { "GenerateOptimizedScenarios", "ValidateDietaryAllergens", "RetrieveInventoryAvailability", "CalculateMenuCostAndMargin" },
                DataDependencies = { "MenuItem", "Recipe", "InventoryBalance", "MenuEngineeringSettings" },
                MaxSteps = 6,
                TokenBudget = 5000,
                TimeoutSeconds = 30,
                ModelTier = "ADVANCED",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(List<OptimizedScenarioOptionDto>)
            },
            new()
            {
                AgentId = "SIMULATION_AGENT",
                DisplayName = "Simulation Agent",
                EquivalentExistingAgent = "SilaCulinaryAnalyticsService (What-If)",
                SupportedIntents = { "TRANSLATE_NL_SIMULATION", "RUN_WHAT_IF", "COMPARE_SCENARIOS" },
                RequiredPermissions = { "RUN_SILA_MENU_SIMULATION" },
                AuthorizedTools = { "RunWhatIfSimulation", "EvaluateIngredientPriceShock", "GenerateOptimizedScenarios" },
                DataDependencies = { "MenuPlan", "MenuEngineeringScenario", "RecipeIngredient" },
                MaxSteps = 4,
                TokenBudget = 3000,
                TimeoutSeconds = 20,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = false,
                OutputSchema = nameof(AiNlSimulationResultDto)
            },
            new()
            {
                AgentId = "CONFIGURATION_INTELLIGENCE_AGENT",
                DisplayName = "Configuration Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryAiSettings",
                SupportedIntents = { "SCORE_SETUP_READINESS", "SUGGEST_THRESHOLDS", "MONITOR_AI_USAGE" },
                RequiredPermissions = { "MANAGE_SILA_MENU_SETUP" },
                AuthorizedTools = { "EvaluateSetupReadiness" },
                DataDependencies = { "MenuEngineeringSettings", "SilaCulinaryAiConfiguration", "SilaCulinaryAiUsageLog" },
                MaxSteps = 3,
                TokenBudget = 2000,
                TimeoutSeconds = 15,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "HIGH",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(MeSetupWizardReadinessDto)
            },
            new()
            {
                AgentId = "RECIPE_INTELLIGENCE_AGENT",
                DisplayName = "Autonomous Recipe Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryRecipeService / ME.3 Dependency Graph",
                SupportedIntents = { "MONITOR_RECIPE_COST", "CASCADE_PRICE_SHOCK", "PROPOSE_APPROVED_SUBSTITUTION", "CHECK_ALLERGEN_SAFETY" },
                RequiredPermissions = { "VIEW_SILA_ME_STUDIO", "VIEW_SILA_MENU_COST" },
                AuthorizedTools = { "RetrieveApprovedRecipes", "EvaluateIngredientPriceShock", "CaptureDigitalTwinSnapshot", "ValidateDietaryAllergens" },
                DataDependencies = { "Recipe", "RecipeIngredient", "MenuItem", "MenuPlan", "SilaCulinaryEvent" },
                MaxSteps = 6,
                TokenBudget = 3500,
                TimeoutSeconds = 25,
                ModelTier = "ADVANCED",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(TwinDependencyAnalysisResultDto)
            },
            new()
            {
                AgentId = "INVENTORY_INTELLIGENCE_AGENT",
                DisplayName = "Autonomous Inventory Intelligence Agent",
                EquivalentExistingAgent = "InventoryBalance / ME.3 FEFO Optimizer",
                SupportedIntents = { "PREDICT_STOCKOUT_DATE", "DETECT_EXPIRY_RISK", "RECOMMEND_FEFO_UTILIZATION", "RECOMMEND_STOCK_TRANSFER" },
                RequiredPermissions = { "VIEW_SILA_ME_STUDIO" },
                AuthorizedTools = { "RetrieveInventoryAvailability", "RetrieveIncomingSupply", "RunInventoryDrivenOptimizer" },
                DataDependencies = { "InventoryBalance", "PurchaseOrder", "MenuPlanLine", "SilaCulinaryEvent" },
                MaxSteps = 5,
                TokenBudget = 3000,
                TimeoutSeconds = 25,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(MePhase3InventoryMenuResultDto)
            },
            new()
            {
                AgentId = "PRODUCTION_INTELLIGENCE_AGENT",
                DisplayName = "Autonomous Production Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryProductionService",
                SupportedIntents = { "DETECT_CAPACITY_CONFLICTS", "RECOMMEND_BATCH_SIZES", "OPTIMIZE_KITCHEN_ALLOCATION" },
                RequiredPermissions = { "VIEW_SILA_ME_STUDIO" },
                AuthorizedTools = { "IdentifyProductionConstraints", "RunOperationalStressTest", "CaptureDigitalTwinSnapshot" },
                DataDependencies = { "SilaCulinaryKitchen", "SilaCulinaryWorkstation", "SilaCulinaryProductionPlan", "MenuPlan" },
                MaxSteps = 5,
                TokenBudget = 3000,
                TimeoutSeconds = 25,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(MePhase3ProductionFeasibilityDto)
            },
            new()
            {
                AgentId = "WASTE_INTELLIGENCE_AGENT",
                DisplayName = "Autonomous Waste Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryReconciliationService / ME.3 Waste Predictor",
                SupportedIntents = { "DETECT_RECURRING_WASTE", "RECOMMEND_SMALLER_BATCHES", "QUANTIFY_WASTE_REDUCTION" },
                RequiredPermissions = { "VIEW_SILA_ME_STUDIO" },
                AuthorizedTools = { "PredictFoodWasteCategories", "DetectConsumptionAnomalies", "CalculatePortionsAndDemand" },
                DataDependencies = { "SilaCulinaryWasteEvent", "SilaCulinaryFoodLeftover", "SilaCulinaryPortionSample", "MenuPlanLine" },
                MaxSteps = 5,
                TokenBudget = 3000,
                TimeoutSeconds = 25,
                ModelTier = "LIGHTWEIGHT",
                RiskLevel = "LOW",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(MeWastePredictResultDto)
            },
            new()
            {
                AgentId = "EVENT_INTELLIGENCE_AGENT",
                DisplayName = "Autonomous Event Intelligence Agent",
                EquivalentExistingAgent = "SilaCulinaryEventMenuService / ME.3 Monte Carlo",
                SupportedIntents = { "EVALUATE_EVENT_READINESS", "CASCADE_GUEST_COUNT_CHANGE", "ASSESS_EVENT_BUDGET_RISK" },
                RequiredPermissions = { "VIEW_SILA_ME_STUDIO", "RUN_SILA_MENU_SIMULATION" },
                AuthorizedTools = { "CaptureDigitalTwinSnapshot", "RunMonteCarloSimulation", "RunOperationalStressTest", "GenerateConsolidatedActionPlan" },
                DataDependencies = { "SilaCulinaryEvent", "SilaCulinaryEventSession", "PmsDemandSnapshot", "MenuPlan" },
                MaxSteps = 8,
                TokenBudget = 4500,
                TimeoutSeconds = 30,
                ModelTier = "ADVANCED",
                RiskLevel = "MEDIUM",
                RequiresApprovalForMutations = true,
                OutputSchema = nameof(ActionPlanDto)
            }
        };

        public static MenuEngineeringAgentDescriptorDto? Find(string agentId)
            => Agents.FirstOrDefault(a => string.Equals(a.AgentId, agentId, StringComparison.OrdinalIgnoreCase));

        public static string ResolveForScreen(string? screenKey) => (screenKey ?? "").Trim() switch
        {
            "silaMenuItems" => "MENU_ITEM_INTELLIGENCE_AGENT",
            "silaMenuPlanning" => "MENU_PLANNING_AGENT",
            "silaMenuPortions" => "PORTION_OPTIMIZATION_AGENT",
            "silaMenuDemand" => "DEMAND_FORECAST_AGENT",
            "silaMenuPosSales" => "POS_SALES_INTELLIGENCE_AGENT",
            "silaMenuPosMapping" => "POS_MAPPING_AGENT",
            "silaMenuPerformance" => "MENU_PERFORMANCE_AGENT",
            "silaMenuConsumption" => "CONSUMPTION_INTELLIGENCE_AGENT",
            "silaMenuCost" => "COST_MARGIN_AGENT",
            "silaMenuRecommendations" => "PURCHASE_INTELLIGENCE_AGENT",
            "silaMenuIntelligence" => "MENU_OPTIMIZATION_AGENT",
            "silaMenuWhatIf" => "SIMULATION_AGENT",
            "silaMenuSetup" => "CONFIGURATION_INTELLIGENCE_AGENT",
            _ => "ME_STUDIO_ORCHESTRATOR"
        };
    }
}
