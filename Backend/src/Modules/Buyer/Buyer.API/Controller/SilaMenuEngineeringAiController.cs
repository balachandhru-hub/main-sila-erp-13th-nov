using Buyer.Application.Features.Shared.MenuEngineering.Ai;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;

namespace Buyer.API.Controllers
{
    [ApiController]
    [Route("api/v1/buyer/sila/me-studio/ai")]
    public class SilaMenuEngineeringAiController : BaseController
    {
        private const string MeReadPerms =
            "VIEW_SILA_MENU_ENGINEERING|MANAGE_SILA_MENU_ENGINEERING|VIEW_SILA_ME_STUDIO|CREATE_SILA_MENU|EDIT_SILA_DRAFT_MENU|MANAGE_SILA_MENU_ITEMS|MANAGE_SILA_PORTION_PLANS|MANAGE_SILA_DEMAND_PLANS|VIEW_SILA_POS_SALES|MANAGE_SILA_POS_MAPPING|VIEW_SILA_MENU_PERFORMANCE|VIEW_SILA_MENU_COST|VIEW_SILA_MENU_MARGIN|GENERATE_SILA_MENU_PURCHASE_REC|USE_SILA_AI_MENU_INTELLIGENCE|RUN_SILA_MENU_SIMULATION|APPROVE_SILA_MENU|PUBLISH_SILA_MENU|MANAGE_SILA_MENU_SETUP";

        private readonly IMenuEngineeringAiOrchestratorService _aiService;

        public SilaMenuEngineeringAiController(IMenuEngineeringAiOrchestratorService aiService)
        {
            _aiService = aiService;
        }

        [HttpGet("status")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetStatus([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _aiService.GetOperatingModeStatusAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, ct));

        [HttpPost("contextual")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunContextual([FromBody] AiContextualRequestDto request, CancellationToken ct = default)
            => Ok(await _aiService.RunContextualAssistantAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpPost("multi-agent-run")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunMultiAgent([FromBody] MeStudioMultiAgentCommandRequestDto request, CancellationToken ct = default)
            => Ok(await _aiService.RunMultiAgentWorkflowAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("multi-agent-run/{runId:guid}")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetRun(Guid runId, CancellationToken ct = default)
            => Ok(await _aiService.GetAgentRunAsync(GetOrganizationId(), GetRoleId(), runId, ct));

        [HttpPost("multi-agent-run/{runId:guid}/cancel")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CancelRun(Guid runId, CancellationToken ct = default)
            => Ok(await _aiService.CancelAgentRunAsync(GetOrganizationId(), GetUserId(), GetRoleId(), runId, ct));

        [HttpPost("feedback")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SubmitFeedback([FromBody] MeAiFeedbackSubmitDto dto, CancellationToken ct = default)
        {
            await _aiService.SubmitFeedbackAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct);
            return Ok(new { saved = true });
        }

        [HttpPost("menu-items/assist")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> AssistMenuItem([FromBody] AiMenuItemAssistRequestDto request, CancellationToken ct = default)
            => Ok(await _aiService.AssistMenuItemAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpPost("menu-planning/generate")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GenerateMenuCalendar([FromBody] AiMenuCalendarGenerateRequestDto request, CancellationToken ct = default)
            => Ok(await _aiService.GenerateMenuCalendarAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("portions/optimize")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> OptimizePortions([FromQuery] Guid? menuPlanId = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _aiService.OptimizePortionsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuPlanId, propertyId, ct));

        [HttpGet("demand/backtest")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> BacktestDemand([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _aiService.GetBacktestedDemandForecastAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpGet("pos-sales/intelligence")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetPosSalesIntelligence([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _aiService.GetPosSalesIntelligenceAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpGet("pos-mapping/suggest")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SuggestPosMappings([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _aiService.SuggestPosMappingsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpGet("consumption/root-causes")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetConsumptionRootCauses([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _aiService.GetConsumptionRootCausesAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpPost("cost/price-shock")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> EvaluatePriceShock([FromBody] AiPriceShockRequestDto request, CancellationToken ct = default)
            => Ok(await _aiService.EvaluateIngredientPriceShockAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("pricing/recommendations")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GeneratePricingRecommendations([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _aiService.GeneratePricingRecommendationsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpPost("simulation/nl")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunNlSimulation([FromBody] AiNlSimulationRequestDto request, CancellationToken ct = default)
            => Ok(await _aiService.RunNaturalLanguageSimulationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("cockpit")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetCockpit([FromQuery] Guid? propertyId = null, [FromQuery] string? status = null, CancellationToken ct = default)
            => Ok(await _aiService.GetCockpitRecommendationsAsync(GetOrganizationId(), GetRoleId(), propertyId, status, ct));

        [HttpPost("cockpit/{recommendationId:guid}/action")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> TransitionCockpit(Guid recommendationId, [FromBody] MeRecommendationLifecycleActionDto action, CancellationToken ct = default)
            => Ok(await _aiService.TransitionCockpitRecommendationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), recommendationId, action, ct));

        [HttpGet("proactive-alerts")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetProactiveAlerts([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _aiService.GetProactiveAlertsAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("proactive-alerts/scan")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunProactiveScan([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _aiService.RunProactiveScanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, ct));

        [HttpPost("proactive-alerts/{alertId:guid}/acknowledge")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> AcknowledgeAlert(Guid alertId, CancellationToken ct = default)
            => Ok(await _aiService.AcknowledgeAlertAsync(GetOrganizationId(), GetUserId(), GetRoleId(), alertId, ct));

        [HttpGet("setup/wizard")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetSetupWizard([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _aiService.GetSetupWizardReadinessAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpGet("learning")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetLearning(CancellationToken ct = default)
            => Ok(await _aiService.GetLearningSummaryAsync(GetOrganizationId(), GetRoleId(), ct));

        [HttpGet("mobile-intelligence")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetMobileIntelligence([FromQuery] string rolePersona = "EXECUTIVE_CHEF", [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _aiService.GetSilaStoreRoleIntelligenceAsync(GetOrganizationId(), GetUserId(), GetRoleId(), rolePersona, propertyId, ct));
    }
}
