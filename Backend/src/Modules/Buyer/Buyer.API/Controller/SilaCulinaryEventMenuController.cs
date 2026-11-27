using System.Security.Claims;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Phase 2A Culinary Intelligence — Advanced AI Menu Studio and Event Planner:
    /// Event Master, Calendar, Templates, Multi-Day/Multi-Session Planning, Guest Planning,
    /// Menu Studio, Published Recipe Search, AI Menu Generation, MILP Constraint Optimization,
    /// Dietary & Allergen Intelligence, Read-Only Stock & Material Preview, Cost Simulation,
    /// Customer Menu Preview, Readiness, Approvals, Revisions, Reports, and Phase 2B Handover.
    /// </summary>
    [ApiController]
    public class SilaCulinaryEventMenuController : BaseController
    {
        private readonly ISilaCulinaryEventMenuService _service;

        public SilaCulinaryEventMenuController(ISilaCulinaryEventMenuService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        // ---------------------------------------------------------------------
        // Dashboard & Event Master (§6 - §9, §38 - §40)
        // ---------------------------------------------------------------------

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryEventDashboard")]
        public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        {
            return Ok(await _service.GetDashboardAsync(GetOrganizationId(), GetUserId(), GetRoleId(), cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryEvents")]
        public async Task<IActionResult> ListEvents(
            [FromQuery] string? status,
            [FromQuery] Guid? propertyId,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.ListEventsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), status, propertyId, fromUtc, toUtc, search, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryEvent")]
        public async Task<IActionResult> GetEvent(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), eventId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateCulinaryEvent")]
        public async Task<IActionResult> CreateEvent([FromBody] SilaCulinaryUpsertEventRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("UpdateCulinaryEvent")]
        public async Task<IActionResult> UpdateEvent(Guid eventId, [FromBody] SilaCulinaryUpsertEventRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.UpdateEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, request, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/duplicate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("DuplicateCulinaryEvent")]
        public async Task<IActionResult> DuplicateEvent(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.DuplicateEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/cancel")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CancelCulinaryEvent")]
        public async Task<IActionResult> CancelEvent(Guid eventId, [FromBody] EventReasonBody? body, CancellationToken cancellationToken)
        {
            return Ok(await _service.CancelEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, body?.Reason, cancellationToken));
        }

        // ---------------------------------------------------------------------
        // Event Templates (§10)
        // ---------------------------------------------------------------------

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/templates")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ListCulinaryEventTemplates")]
        public async Task<IActionResult> ListTemplates(CancellationToken cancellationToken)
        {
            return Ok(await _service.ListTemplatesAsync(GetOrganizationId(), cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/save-template")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateCulinaryEventTemplate")]
        public async Task<IActionResult> SaveTemplate(Guid eventId, [FromBody] CreateTemplateBody body, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateTemplateFromEventAsync(GetOrganizationId(), GetUserId(), GetRoleId(), eventId, body.Name, body.Description, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/templates/{templateId:guid}/create-event")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateCulinaryEventFromTemplate")]
        public async Task<IActionResult> CreateFromTemplate(Guid templateId, [FromBody] SilaCulinaryUpsertEventRequestDto overrides, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateEventFromTemplateAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), templateId, overrides, cancellationToken));
        }

        // ---------------------------------------------------------------------
        // Multi-Day & Multi-Session Planning (§11)
        // ---------------------------------------------------------------------

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/sessions")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("UpsertCulinaryEventSession")]
        public async Task<IActionResult> UpsertSession(Guid eventId, [FromBody] SilaCulinaryUpsertSessionRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.UpsertSessionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, request, cancellationToken));
        }

        [HttpDelete]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/sessions/{sessionId:guid}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("DeleteCulinaryEventSession")]
        public async Task<IActionResult> DeleteSession(Guid eventId, Guid sessionId, CancellationToken cancellationToken)
        {
            return Ok(await _service.DeleteSessionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), eventId, sessionId, cancellationToken));
        }

        // ---------------------------------------------------------------------
        // Menu Studio & Recipe Search (§13 - §16, §20, §25)
        // ---------------------------------------------------------------------

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-menus")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ListCulinaryMenus")]
        public async Task<IActionResult> ListMenus([FromQuery] Guid? eventId, [FromQuery] bool? reusableOnly, CancellationToken cancellationToken)
        {
            return Ok(await _service.ListMenusAsync(GetOrganizationId(), GetUserId(), GetRoleId(), eventId, reusableOnly, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-menus/{menuId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryMenu")]
        public async Task<IActionResult> GetMenu(Guid menuId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-menus")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateCulinaryMenu")]
        public async Task<IActionResult> CreateMenu([FromBody] SilaCulinaryCreateMenuRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-menus/{menuId:guid}/lines")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("UpsertCulinaryMenuLine")]
        public async Task<IActionResult> UpsertMenuLine(Guid menuId, [FromBody] SilaCulinaryUpsertMenuLineRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.UpsertMenuLineAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), menuId, request, cancellationToken));
        }

        [HttpDelete]
        [Route("api/v1/buyer/sila/culinary-menus/{menuId:guid}/lines/{lineId:guid}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("RemoveCulinaryMenuLine")]
        public async Task<IActionResult> RemoveMenuLine(Guid menuId, Guid lineId, CancellationToken cancellationToken)
        {
            return Ok(await _service.RemoveMenuLineAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuId, lineId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-menus/{menuId:guid}/reorder")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ReorderCulinaryMenuLines")]
        public async Task<IActionResult> ReorderMenuLines(Guid menuId, [FromBody] ReorderLinesBody body, CancellationToken cancellationToken)
        {
            return Ok(await _service.ReorderMenuLinesAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuId, body.OrderedLineIds, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-menus/published-recipes")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("SearchPublishedCulinaryRecipes")]
        public async Task<IActionResult> SearchPublishedRecipes(
            [FromQuery] string? query,
            [FromQuery] string? category,
            [FromQuery] string? cuisine,
            [FromQuery] string? dietary,
            [FromQuery] string? excludeAllergens,
            [FromQuery] decimal? maxCost,
            [FromQuery] Guid? propertyId,
            CancellationToken cancellationToken)
        {
            return Ok(await _service.SearchPublishedRecipesAsync(GetOrganizationId(), GetRoleId(), query, category, cuisine, dietary, excludeAllergens, maxCost, propertyId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-menus/{menuId:guid}/lines/{lineId:guid}/create-recipe-draft")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateMissingRecipeDraftFromMenuLine")]
        public async Task<IActionResult> CreateMissingRecipeDraft(Guid menuId, Guid lineId, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateMissingRecipeDraftAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), menuId, lineId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-menus/{menuId:guid}/lines/{lineId:guid}/sync-published")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("SyncPublishedRecipeToMenuLine")]
        public async Task<IActionResult> SyncPublishedRecipe(Guid menuId, Guid lineId, CancellationToken cancellationToken)
        {
            return Ok(await _service.SyncPublishedRecipeToMenuLineAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuId, lineId, cancellationToken));
        }

        // ---------------------------------------------------------------------
        // AI Generation, MILP Optimization, Dietary, Stock Preview & Simulation
        // ---------------------------------------------------------------------

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/ai-generate-menu")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GenerateCulinaryAiMenu")]
        public async Task<IActionResult> GenerateAiMenu(Guid eventId, [FromBody] SilaCulinaryAiMenuGenerateRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.GenerateAiMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, request, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/optimize-menu")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("OptimizeCulinaryEventMenu")]
        public async Task<IActionResult> OptimizeMenu(Guid eventId, [FromBody] SilaCulinaryMenuOptimizeRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.OptimizeEventMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, request, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/dietary-verify")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("VerifyCulinaryEventDietary")]
        public async Task<IActionResult> VerifyDietary(Guid eventId, [FromBody] VerifyDietaryBody body, CancellationToken cancellationToken)
        {
            return Ok(await _service.VerifyDietaryReviewAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, body.Verified, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/allergen-matrix")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryEventAllergenMatrix")]
        public async Task<IActionResult> GetAllergenMatrix(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetAllergenMatrixAsync(GetOrganizationId(), eventId, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/material-preview")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryEventMaterialPreview")]
        public async Task<IActionResult> GetMaterialPreview(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetPreliminaryMaterialPreviewAsync(GetOrganizationId(), eventId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/simulate")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("SimulateCulinaryEvent")]
        public async Task<IActionResult> SimulateEvent(Guid eventId, [FromBody] SilaCulinaryEventSimulationRequestDto request, CancellationToken cancellationToken)
        {
            return Ok(await _service.SimulateEventAsync(GetOrganizationId(), GetRoleId(), eventId, request, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/scenarios")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateCulinaryEventScenario")]
        public async Task<IActionResult> CreateScenario(Guid eventId, [FromBody] CreateScenarioBody body, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateScenarioAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, body.ScenarioName, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/scenarios/{scenarioMenuId:guid}/select")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("SelectCulinaryEventScenario")]
        public async Task<IActionResult> SelectScenario(Guid eventId, Guid scenarioMenuId, CancellationToken cancellationToken)
        {
            return Ok(await _service.SelectScenarioAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, scenarioMenuId, cancellationToken));
        }

        // ---------------------------------------------------------------------
        // Customer Preview, Readiness, Approvals, Revisions, Reports & Phase 2B
        // ---------------------------------------------------------------------

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/customer-menu")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryCustomerMenuPreview")]
        public async Task<IActionResult> GetCustomerMenu(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetCustomerMenuPreviewAsync(GetOrganizationId(), eventId, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/readiness")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryEventReadiness")]
        public async Task<IActionResult> GetReadiness(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetEventReadinessAsync(GetOrganizationId(), eventId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/submit")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("SubmitCulinaryEventForApproval")]
        public async Task<IActionResult> SubmitEvent(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.SubmitEventForApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/decide")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("DecideCulinaryEventApproval")]
        public async Task<IActionResult> DecideApproval(Guid eventId, [FromBody] ApprovalDecisionBody body, CancellationToken cancellationToken)
        {
            return Ok(await _service.DecideEventApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, body.Decision, body.Comment, cancellationToken));
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/revisions")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateCulinaryEventRevision")]
        public async Task<IActionResult> CreateRevision(Guid eventId, [FromBody] EventReasonBody body, CancellationToken cancellationToken)
        {
            return Ok(await _service.CreateRevisionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), eventId, body.Reason ?? "Change management revision", cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/revisions")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ListCulinaryEventRevisions")]
        public async Task<IActionResult> ListRevisions(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.ListRevisionsAsync(GetOrganizationId(), GetRoleId(), eventId, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/reports/{reportCode}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ExportCulinaryEventReport")]
        public async Task<IActionResult> ExportReport(Guid eventId, string reportCode, CancellationToken cancellationToken)
        {
            return Ok(await _service.ExportReportAsync(GetOrganizationId(), GetRoleId(), eventId, reportCode, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-events/{eventId:guid}/phase2b-handover")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryEventPhase2BHandover")]
        public async Task<IActionResult> GetPhase2BHandover(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _service.GetPhase2BHandoverAsync(GetOrganizationId(), GetRoleId(), eventId, cancellationToken));
        }

        public record EventReasonBody(string? Reason);
        public record CreateTemplateBody(string Name, string? Description);
        public record ReorderLinesBody(List<Guid> OrderedLineIds);
        public record VerifyDietaryBody(bool Verified);
        public record CreateScenarioBody(string ScenarioName);
        public record ApprovalDecisionBody(string Decision, string? Comment);
    }
}
