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
    /// Phase 2B Culinary Intelligence — Advanced Event MRP & Material Requirement Planning Engine.
    /// Zero stock reservations, zero PO creation, zero ITO creation, zero SAP postings.
    /// </summary>
    [ApiController]
    public class SilaCulinaryMrpController : BaseController
    {
        private readonly ISilaCulinaryMrpService _service;

        public SilaCulinaryMrpController(ISilaCulinaryMrpService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMrpDashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.GetDashboardAsync(GetOrganizationId(), GetRoleId(), propertyId, status, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryMrpRuns")]
        public async Task<IActionResult> ListRuns([FromQuery] Guid? propertyId, [FromQuery] string? status, [FromQuery] bool? includeSimulations, CancellationToken cancellationToken)
            => Ok(await _service.ListRunsAsync(GetOrganizationId(), GetRoleId(), propertyId, status, includeSimulations, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMrpRun")]
        public async Task<IActionResult> GetRun([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.GetRunAsync(GetOrganizationId(), GetRoleId(), runId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryMrpRun")]
        public async Task<IActionResult> CreateRun([FromBody] SilaCulinaryMrpCreateRunRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateAndCalculateRunAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/recalculate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RecalculateCulinaryMrpRun")]
        public async Task<IActionResult> RecalculateRun([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.RecalculateRunAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/views")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMrpGroupedView")]
        public async Task<IActionResult> GetGroupedView([FromRoute] Guid runId, [FromQuery] string groupBy = "MATERIAL", CancellationToken cancellationToken = default)
            => Ok(await _service.GetGroupedViewAsync(GetOrganizationId(), GetRoleId(), runId, groupBy, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/simulate")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SimulateCulinaryMrpRun")]
        public async Task<IActionResult> SimulateRun([FromRoute] Guid runId, [FromBody] SilaCulinaryMrpSimulateRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.SimulateRunAsync(GetOrganizationId(), GetRoleId(), runId, request, cancellationToken));

        public class SaveScenarioBody
        {
            public string Name { get; set; } = string.Empty;
            public string ScenarioType { get; set; } = "CUSTOM";
            public SilaCulinaryMrpSimulateRequestDto Parameters { get; set; } = new();
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/scenarios")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SaveCulinaryMrpScenario")]
        public async Task<IActionResult> SaveScenario([FromRoute] Guid runId, [FromBody] SaveScenarioBody body, CancellationToken cancellationToken)
            => Ok(await _service.SaveScenarioAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, body.Name, body.ScenarioType, body.Parameters, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/optimize")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("OptimizeCulinaryMrpBatches")]
        public async Task<IActionResult> OptimizeBatches([FromRoute] Guid runId, [FromQuery] decimal? maxBudget, CancellationToken cancellationToken)
            => Ok(await _service.OptimizeBatchesAsync(GetOrganizationId(), GetUserId(), GetRoleId(), runId, maxBudget, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/ai-analyze")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeCulinaryMrpWithAi")]
        public async Task<IActionResult> AnalyzeWithAi([FromRoute] Guid runId, [FromBody] SilaCulinaryMrpAiAnalyzeRequestDto body, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeWithAiAsync(GetOrganizationId(), GetUserId(), GetRoleId(), runId, body.Question, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/substitutions")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SuggestCulinaryMrpSubstitutions")]
        public async Task<IActionResult> SuggestSubstitutions([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.SuggestSubstitutionsAsync(GetOrganizationId(), GetRoleId(), runId, cancellationToken));

        public class ResolveExceptionBody
        {
            public string ResolutionStatus { get; set; } = "RESOLVED";
            public string? Note { get; set; }
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/exceptions/{exceptionId:guid}/resolve")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ResolveCulinaryMrpException")]
        public async Task<IActionResult> ResolveException([FromRoute] Guid runId, [FromRoute] Guid exceptionId, [FromBody] ResolveExceptionBody body, CancellationToken cancellationToken)
            => Ok(await _service.ResolveExceptionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, exceptionId, body.ResolutionStatus, body.Note, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/exceptions/{exceptionId:guid}/create-material-request")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateMaterialRequestFromMrpException")]
        public async Task<IActionResult> CreateMaterialRequest([FromRoute] Guid runId, [FromRoute] Guid exceptionId, CancellationToken cancellationToken)
            => Ok(await _service.CreateMaterialRequestFromExceptionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, exceptionId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/submit")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SubmitCulinaryMrpForApproval")]
        public async Task<IActionResult> SubmitForApproval([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.SubmitForApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, cancellationToken));

        public class DecideApprovalBody
        {
            public string Decision { get; set; } = "APPROVE";
            public string? Comment { get; set; }
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/decide")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DecideCulinaryMrpApproval")]
        public async Task<IActionResult> DecideApproval([FromRoute] Guid runId, [FromBody] DecideApprovalBody body, CancellationToken cancellationToken)
            => Ok(await _service.DecideApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, body.Decision, body.Comment, cancellationToken));

        public class CreateRevisionBody
        {
            public string ChangeReason { get; set; } = "Controlled revision";
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/revisions")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryMrpRevision")]
        public async Task<IActionResult> CreateRevision([FromRoute] Guid runId, [FromBody] CreateRevisionBody body, CancellationToken cancellationToken)
            => Ok(await _service.CreateRevisionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, body.ChangeReason, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/impact-analysis")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeCulinaryMrpEventChangeImpact")]
        public async Task<IActionResult> AnalyzeImpact([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeEventChangeImpactAsync(GetOrganizationId(), GetRoleId(), runId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/release")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ReleaseCulinaryMrpForPlanning")]
        public async Task<IActionResult> ReleaseForPlanning([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.ReleaseForPlanningAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), runId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/phase2c-handover")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMrpPhase2CHandover")]
        public async Task<IActionResult> GetPhase2CHandover([FromRoute] Guid runId, CancellationToken cancellationToken)
            => Ok(await _service.GetPhase2CHandoverAsync(GetOrganizationId(), GetUserId(), GetRoleId(), runId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/reports/catalog")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMrpReportCatalog")]
        public IActionResult GetReportCatalog()
            => Ok(_service.GetReportCatalog());

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-mrp/runs/{runId:guid}/reports/{reportCode}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportCulinaryMrpReport")]
        public async Task<IActionResult> ExportReport([FromRoute] Guid runId, [FromRoute] string reportCode, CancellationToken cancellationToken)
            => Ok(await _service.ExportReportAsync(GetOrganizationId(), GetRoleId(), runId, reportCode, cancellationToken));
    }
}
