using Buyer.Application.Features.Shared.MenuEngineering.Autonomous;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;

namespace Buyer.API.Controllers
{
    [ApiController]
    [Route("api/v1/buyer/sila/me-studio/autonomous")]
    public class SilaMenuEngineeringAutonomousController : BaseController
    {
        private const string MeReadPerms =
            "VIEW_SILA_MENU_ENGINEERING|MANAGE_SILA_MENU_ENGINEERING|VIEW_SILA_ME_STUDIO|CREATE_SILA_MENU|EDIT_SILA_DRAFT_MENU|MANAGE_SILA_MENU_ITEMS|MANAGE_SILA_PORTION_PLANS|MANAGE_SILA_DEMAND_PLANS|VIEW_SILA_POS_SALES|MANAGE_SILA_POS_MAPPING|VIEW_SILA_MENU_PERFORMANCE|VIEW_SILA_MENU_COST|VIEW_SILA_MENU_MARGIN|GENERATE_SILA_MENU_PURCHASE_REC|USE_SILA_AI_MENU_INTELLIGENCE|RUN_SILA_MENU_SIMULATION|APPROVE_SILA_MENU|PUBLISH_SILA_MENU|MANAGE_SILA_MENU_SETUP";

        private readonly IMenuEngineeringAutonomousService _autonomous;

        public SilaMenuEngineeringAutonomousController(IMenuEngineeringAutonomousService autonomous)
        {
            _autonomous = autonomous;
        }

        [HttpPost("events")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> PublishEvent([FromBody] PublishBusinessEventRequestDto request, CancellationToken ct = default)
            => Ok(await _autonomous.PublishBusinessEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("events")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetEvents([FromQuery] string? status = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetBusinessEventsAsync(GetOrganizationId(), GetRoleId(), status, ct));

        [HttpPost("events/process")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ProcessEvents([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.ProcessPendingEventsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, ct));

        [HttpPost("events/{eventId:guid}/replay")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ReplayEvent(Guid eventId, CancellationToken ct = default)
            => Ok(await _autonomous.ReplayEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), eventId, ct));

        [HttpGet("autonomy-policies")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetAutonomyPolicies([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetAutonomyPoliciesAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("autonomy-policies")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MENU_SETUP|MANAGE_SILA_MENU_ENGINEERING")]
        public async Task<IActionResult> UpsertAutonomyPolicy([FromBody] UpsertAgentAutonomyPolicyRequestDto request, CancellationToken ct = default)
            => Ok(await _autonomous.UpsertAutonomyPolicyAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("emergency-control")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetEmergencyControl(CancellationToken ct = default)
            => Ok(await _autonomous.GetEmergencyStatusAsync(GetOrganizationId(), GetRoleId(), ct));

        [HttpPost("emergency-control")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MENU_SETUP|MANAGE_SILA_MENU_ENGINEERING")]
        public async Task<IActionResult> SetEmergencyControl([FromBody] EmergencyControlRequestDto request, CancellationToken ct = default)
            => Ok(await _autonomous.SetEmergencyControlAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("action-plans")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetActionPlans([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetActionPlansAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("action-items/{actionItemId:guid}/approve")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ApproveActionItem(Guid actionItemId, [FromBody] ApproveActionItemRequestDto request, CancellationToken ct = default)
            => Ok(await _autonomous.ApproveActionItemAsync(GetOrganizationId(), GetUserId(), GetRoleId(), actionItemId, request, ct));

        [HttpPost("action-items/{actionItemId:guid}/execute")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ExecuteActionItem(Guid actionItemId, [FromBody] ExecuteActionItemRequestDto? request = null, CancellationToken ct = default)
            => Ok(await _autonomous.ExecuteActionItemAsync(GetOrganizationId(), GetUserId(), GetRoleId(), actionItemId, request ?? new(), ct));

        [HttpPost("recommendations/{recommendationId:guid}/lifecycle")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> TransitionLifecycle(Guid recommendationId, [FromBody] ExtendedLifecycleTransitionDto request, CancellationToken ct = default)
            => Ok(await _autonomous.TransitionRecommendationLifecycleAsync(GetOrganizationId(), GetUserId(), GetRoleId(), recommendationId, request, ct));

        [HttpGet("benefits")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetRealizedBenefits([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetRealizedBenefitsAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("benefits")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RecordRealizedBenefit([FromBody] RecordRealizedBenefitRequestDto request, CancellationToken ct = default)
            => Ok(await _autonomous.RecordRealizedBenefitAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpPost("benefits/{benefitId:guid}/validate")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ValidateRealizedBenefit(Guid benefitId, [FromQuery] bool approved = true, CancellationToken ct = default)
            => Ok(await _autonomous.ValidateRealizedBenefitAsync(GetOrganizationId(), GetUserId(), GetRoleId(), benefitId, approved, ct));

        [HttpGet("readiness")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> EvaluateReadiness([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.EvaluateDataReadinessAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpGet("integrations/health")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetIntegrationHealth([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.EvaluateIntegrationHealthAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, persist: false, ct));

        [HttpGet("control-tower")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetControlTower([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetControlTowerAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpGet("executive-dashboard")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetExecutiveDashboard([FromQuery] string persona = "GENERAL_MANAGER", [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetExecutiveDashboardAsync(GetOrganizationId(), GetRoleId(), persona, propertyId, ct));

        [HttpPost("briefings/generate")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GenerateBriefings([FromQuery] Guid? propertyId = null, [FromQuery] string channel = "CLOUD", CancellationToken ct = default)
            => Ok(await _autonomous.GenerateDailyBriefingsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, channel, ct));

        [HttpGet("briefings")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetBriefings([FromQuery] string? persona = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetBriefingsAsync(GetOrganizationId(), GetRoleId(), persona, propertyId, ct));

        [HttpGet("observability")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetObservability(CancellationToken ct = default)
            => Ok(await _autonomous.GetUsageObservabilityAsync(GetOrganizationId(), GetRoleId(), ct));

        [HttpGet("menu-health")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetProactiveMenuHealth([FromQuery] Guid? menuPlanId = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _autonomous.GetProactiveMenuHealthAsync(GetOrganizationId(), GetRoleId(), menuPlanId, propertyId, ct));

        [HttpPost("teams/notify")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> NotifyTeams([FromBody] TeamsDispatchRequestDto request, CancellationToken ct = default)
            => Ok(new { delivered = await _autonomous.DispatchTeamsNotificationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct) });

        [HttpPost("teams/approval-callback")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> TeamsApprovalCallback([FromBody] TeamsApprovalCallbackRequestDto request, CancellationToken ct = default)
            => Ok(await _autonomous.ProcessTeamsApprovalCallbackAsync(GetOrganizationId(), request, ct));
    }
}
