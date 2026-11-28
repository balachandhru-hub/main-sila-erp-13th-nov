using Buyer.Domain.Dtos;

namespace Buyer.Application.Features.Shared.MenuEngineering.Autonomous
{
    public interface IMenuEngineeringAutonomousService
    {
        Task<BusinessEventDto> PublishBusinessEventAsync(Guid organizationId, Guid userId, Guid roleId, PublishBusinessEventRequestDto request, CancellationToken ct);
        Task<EventProcessingSummaryDto> ProcessPendingEventsAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default);
        Task<BusinessEventDto> ReplayEventAsync(Guid organizationId, Guid userId, Guid roleId, Guid eventId, CancellationToken ct);
        Task<List<BusinessEventDto>> GetBusinessEventsAsync(Guid organizationId, Guid roleId, string? status = null, CancellationToken ct = default);

        Task<List<AgentAutonomyPolicyDto>> GetAutonomyPoliciesAsync(Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default);
        Task<AgentAutonomyPolicyDto> UpsertAutonomyPolicyAsync(Guid organizationId, Guid userId, Guid roleId, UpsertAgentAutonomyPolicyRequestDto request, CancellationToken ct);
        Task<EmergencyControlStatusDto> SetEmergencyControlAsync(Guid organizationId, Guid userId, Guid roleId, EmergencyControlRequestDto request, CancellationToken ct);
        Task<EmergencyControlStatusDto> GetEmergencyStatusAsync(Guid organizationId, Guid roleId, CancellationToken ct);

        Task<List<ActionPlanDto>> GetActionPlansAsync(Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default);
        Task<ActionItemDto> ApproveActionItemAsync(Guid organizationId, Guid userId, Guid roleId, Guid actionItemId, ApproveActionItemRequestDto request, CancellationToken ct);
        Task<ActionExecutionResultDto> ExecuteActionItemAsync(Guid organizationId, Guid userId, Guid roleId, Guid actionItemId, ExecuteActionItemRequestDto request, CancellationToken ct);

        Task<MeRecommendationCockpitItemDto> TransitionRecommendationLifecycleAsync(Guid organizationId, Guid userId, Guid roleId, Guid recommendationId, ExtendedLifecycleTransitionDto request, CancellationToken ct);
        Task<RealizedBenefitDto> RecordRealizedBenefitAsync(Guid organizationId, Guid userId, Guid roleId, RecordRealizedBenefitRequestDto request, CancellationToken ct);
        Task<RealizedBenefitDto> ValidateRealizedBenefitAsync(Guid organizationId, Guid userId, Guid roleId, Guid benefitId, bool approved, CancellationToken ct);
        Task<List<RealizedBenefitDto>> GetRealizedBenefitsAsync(Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default);

        Task<DataReadinessAssessmentDto> EvaluateDataReadinessAsync(Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default);
        Task<List<IntegrationHealthItemDto>> EvaluateIntegrationHealthAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId = null, bool persist = true, CancellationToken ct = default);

        Task<ExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid organizationId, Guid roleId, string persona = "GENERAL_MANAGER", Guid? propertyId = null, CancellationToken ct = default);
        Task<List<ExecutiveBriefingDto>> GenerateDailyBriefingsAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId = null, string deliveryChannel = "CLOUD", CancellationToken ct = default);
        Task<List<ExecutiveBriefingDto>> GetBriefingsAsync(Guid organizationId, Guid roleId, string? persona = null, Guid? propertyId = null, CancellationToken ct = default);

        Task<AiControlTowerDto> GetControlTowerAsync(Guid organizationId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default);
        Task<UsageObservabilityDto> GetUsageObservabilityAsync(Guid organizationId, Guid roleId, CancellationToken ct = default);
        Task<ProactiveMenuHealthDto> GetProactiveMenuHealthAsync(Guid organizationId, Guid roleId, Guid? menuPlanId = null, Guid? propertyId = null, CancellationToken ct = default);

        Task<bool> DispatchTeamsNotificationAsync(Guid organizationId, Guid userId, Guid roleId, TeamsDispatchRequestDto request, CancellationToken ct);
        Task<ActionItemDto> ProcessTeamsApprovalCallbackAsync(Guid organizationId, TeamsApprovalCallbackRequestDto request, CancellationToken ct);

        Task RunScheduledAutonomousCycleForBuyerAsync(Guid buyerId, CancellationToken ct);
    }

    public static class MenuEngineeringAutonomousContracts
    {
        public static readonly Dictionary<string, string[]> EventAgentRoutingMatrix = new(StringComparer.OrdinalIgnoreCase)
        {
            ["IngredientPriceChanged"] = new[] { "RECIPE_INTELLIGENCE_AGENT", "COST_MARGIN_AGENT", "MENU_OPTIMIZATION_AGENT", "EVENT_INTELLIGENCE_AGENT" },
            ["RecipeVersionApproved"] = new[] { "RECIPE_INTELLIGENCE_AGENT", "COST_MARGIN_AGENT", "MENU_PLANNING_AGENT" },
            ["MenuPublished"] = new[] { "MENU_PERFORMANCE_AGENT", "DEMAND_FORECAST_AGENT", "PRODUCTION_INTELLIGENCE_AGENT", "PURCHASE_INTELLIGENCE_AGENT" },
            ["MenuItemPriceChanged"] = new[] { "COST_MARGIN_AGENT", "MENU_PERFORMANCE_AGENT" },
            ["POSSalesImported"] = new[] { "POS_SALES_INTELLIGENCE_AGENT", "POS_MAPPING_AGENT", "DEMAND_FORECAST_AGENT", "MENU_PERFORMANCE_AGENT" },
            ["POSMappingUpdated"] = new[] { "POS_MAPPING_AGENT", "MENU_PERFORMANCE_AGENT", "CONSUMPTION_INTELLIGENCE_AGENT" },
            ["OccupancyForecastChanged"] = new[] { "DEMAND_FORECAST_AGENT", "PORTION_OPTIMIZATION_AGENT", "PRODUCTION_INTELLIGENCE_AGENT", "PURCHASE_INTELLIGENCE_AGENT" },
            ["EventGuestCountChanged"] = new[] { "EVENT_INTELLIGENCE_AGENT", "DEMAND_FORECAST_AGENT", "PORTION_OPTIMIZATION_AGENT", "PRODUCTION_INTELLIGENCE_AGENT", "PURCHASE_INTELLIGENCE_AGENT", "COST_MARGIN_AGENT" },
            ["InventoryLevelChanged"] = new[] { "INVENTORY_INTELLIGENCE_AGENT", "PURCHASE_INTELLIGENCE_AGENT" },
            ["InventoryExpiryRiskDetected"] = new[] { "INVENTORY_INTELLIGENCE_AGENT", "MENU_OPTIMIZATION_AGENT", "WASTE_INTELLIGENCE_AGENT" },
            ["SupplierDeliveryDelayed"] = new[] { "INVENTORY_INTELLIGENCE_AGENT", "PURCHASE_INTELLIGENCE_AGENT", "PRODUCTION_INTELLIGENCE_AGENT" },
            ["PurchaseOrderChanged"] = new[] { "PURCHASE_INTELLIGENCE_AGENT", "INVENTORY_INTELLIGENCE_AGENT" },
            ["GoodsReceiptPosted"] = new[] { "INVENTORY_INTELLIGENCE_AGENT", "COST_MARGIN_AGENT" },
            ["ProductionPlanChanged"] = new[] { "PRODUCTION_INTELLIGENCE_AGENT", "INVENTORY_INTELLIGENCE_AGENT" },
            ["ProductionCompleted"] = new[] { "PRODUCTION_INTELLIGENCE_AGENT", "CONSUMPTION_INTELLIGENCE_AGENT", "WASTE_INTELLIGENCE_AGENT" },
            ["ActualConsumptionRecorded"] = new[] { "CONSUMPTION_INTELLIGENCE_AGENT", "WASTE_INTELLIGENCE_AGENT", "PORTION_OPTIMIZATION_AGENT" },
            ["WasteRecorded"] = new[] { "WASTE_INTELLIGENCE_AGENT", "CONSUMPTION_INTELLIGENCE_AGENT", "PORTION_OPTIMIZATION_AGENT" },
            ["EventFinanciallyClosed"] = new[] { "EVENT_INTELLIGENCE_AGENT", "COST_MARGIN_AGENT" },
            ["ForecastDeviationDetected"] = new[] { "DEMAND_FORECAST_AGENT", "PORTION_OPTIMIZATION_AGENT", "PRODUCTION_INTELLIGENCE_AGENT" },
            ["IntegrationFailureDetected"] = new[] { "CONFIGURATION_INTELLIGENCE_AGENT" }
        };

        public static string ClassifyRisk(string actionType) => (actionType ?? "").Trim().ToUpperInvariant() switch
        {
            "GENERATE_REPORT" or "GENERATE_INSIGHT" or "RUN_SIMULATION" or "REFRESH_FORECAST" or "CREATE_INVESTIGATION_TASK" or "PREPARE_RECOMMENDATION" or "REVIEW_DEMAND" => "LOW",
            "PREPARE_MENU_REVISION" or "PREPARE_PURCHASE_PROPOSAL" or "PREPARE_PRODUCTION_REVISION" or "PROPOSE_PORTION_ADJUSTMENT" or "PROPOSE_SUPPLIER_SUBSTITUTION" or "APPROVE_STOCK_TRANSFER_DRAFT" => "MEDIUM",
            "PUBLISH_MENU" or "CHANGE_SELLING_PRICE" or "MODIFY_APPROVED_RECIPE" or "RELEASE_PRODUCTION_ORDER" or "TRANSMIT_PURCHASE_ORDER" or "POST_INVENTORY_TRANSACTION" or "POST_FINANCIAL_TRANSACTION" => "HIGH",
            _ => "MEDIUM"
        };

        public static string AutonomyLevelLabel(int level) => level switch
        {
            0 => "LEVEL_0_DISABLED",
            1 => "LEVEL_1_OBSERVE",
            2 => "LEVEL_2_RECOMMEND",
            3 => "LEVEL_3_PREPARE",
            4 => "LEVEL_4_APPROVAL_EXECUTION",
            5 => "LEVEL_5_POLICY_AUTOMATION",
            _ => "LEVEL_2_RECOMMEND"
        };

        public static (string Decision, string Reason) EvaluatePolicy(
            int autonomyLevel,
            bool isAgentPaused,
            bool globalPaused,
            bool automatedExecutionDisabled,
            string riskClassification,
            decimal financialImpact,
            decimal maxFinancialThreshold,
            bool hasActorPermission,
            bool foodSafetyCompliant,
            bool dataIsFresh,
            decimal confidenceScore)
        {
            if (globalPaused)
                return ("REJECT", "Global autonomous mode is paused by administrator emergency control.");
            if (isAgentPaused || autonomyLevel <= 0)
                return ("REJECT", "Agent is disabled or paused (Level 0).");
            if (!foodSafetyCompliant)
                return ("REJECT", "Food safety or allergen guardrail check failed; execution blocked.");
            if (!hasActorPermission)
                return ("REJECT", "Caller lacks required RBAC permission for this action.");

            if (string.Equals(riskClassification, "HIGH", StringComparison.OrdinalIgnoreCase))
            {
                if (automatedExecutionDisabled)
                    return ("REQUIRE_APPROVAL", "Automated execution is disabled; high-risk action requires manual approval.");
                return ("REQUIRE_APPROVAL", "High-risk business mutation always requires explicit authorized approval before execution.");
            }

            if (!dataIsFresh || confidenceScore < 0.50m)
                return ("ESCALATE", "Data freshness or prediction confidence is below threshold; escalated for human review.");

            if (Math.Abs(financialImpact) > maxFinancialThreshold)
                return ("REQUIRE_APPROVAL", $"Financial impact ({financialImpact:F2}) exceeds configured policy threshold ({maxFinancialThreshold:F2}).");

            if (autonomyLevel == 1)
                return ("ALLOW_OBSERVE", "Agent configured at Level 1 (Observe): insight recorded without draft or execution.");
            if (autonomyLevel == 2)
                return ("ALLOW_RECOMMEND", "Agent configured at Level 2 (Recommend): recommendation generated for review.");
            if (autonomyLevel == 3)
                return ("ALLOW_DRAFT", "Agent configured at Level 3 (Prepare): draft proposal prepared awaiting approval.");
            if (autonomyLevel == 4)
                return ("REQUIRE_APPROVAL", "Agent configured at Level 4: requires explicit user approval before controlled execution.");

            if (automatedExecutionDisabled)
                return ("REQUIRE_APPROVAL", "Automated execution is globally disabled; switched to approval-required mode.");

            return string.Equals(riskClassification, "LOW", StringComparison.OrdinalIgnoreCase)
                ? ("ALLOW_POLICY_EXECUTION", "Low-risk reversible action allowlisted under Level 5 policy controls.")
                : ("REQUIRE_APPROVAL", "Medium-risk proposal requires explicit approval before state change.");
        }
    }
}
