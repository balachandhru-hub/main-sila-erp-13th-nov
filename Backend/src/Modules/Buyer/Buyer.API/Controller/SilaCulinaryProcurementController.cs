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
    /// Phase 2C Culinary Intelligence — Advanced Stock Availability, Reservations, Internal Transfers &amp; Procurement Intelligence.
    /// Reuses existing ITO (SAP 311/312), PO Draft (DeferErpUntilApproved=true), GRN reconciliation, and Approval services.
    /// </summary>
    [ApiController]
    public class SilaCulinaryProcurementController : BaseController
    {
        private readonly ISilaCulinaryProcurementService _service;

        public SilaCulinaryProcurementController(ISilaCulinaryProcurementService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-procurement/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryProcurementDashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.GetDashboardAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, status, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-procurement/plans")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryProcurementPlans")]
        public async Task<IActionResult> ListPlans([FromQuery] Guid? propertyId, [FromQuery] string? status, [FromQuery] bool? includeSimulations, CancellationToken cancellationToken)
            => Ok(await _service.ListPlansAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, status, includeSimulations, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryProcurementPlan")]
        public async Task<IActionResult> GetPlan([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetPlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryProcurementPlan")]
        public async Task<IActionResult> CreatePlan([FromBody] SilaCulinaryCreateProcurementPlanRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateAndCalculatePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/recalculate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RecalculateCulinaryProcurementPlan")]
        public async Task<IActionResult> Recalculate([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.RecalculatePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/reservations")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryStockReservation")]
        public async Task<IActionResult> CreateReservation([FromRoute] Guid planId, [FromBody] SilaCulinaryCreateReservationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateReservationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        public class TransitionReservationBody
        {
            public string Action { get; set; } = string.Empty;
            public string? Reason { get; set; }
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/reservations/{reservationId:guid}/transition")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("TransitionCulinaryStockReservation")]
        public async Task<IActionResult> TransitionReservation([FromRoute] Guid planId, [FromRoute] Guid reservationId, [FromBody] TransitionReservationBody body, CancellationToken cancellationToken)
            => Ok(await _service.TransitionReservationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, reservationId, body.Action, body.Reason, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/incoming-po-allocations")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AllocateIncomingPoForEvent")]
        public async Task<IActionResult> AllocateIncomingPo([FromRoute] Guid planId, [FromBody] SilaCulinaryAllocateIncomingPoRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AllocateIncomingPoAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/ito-drafts")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryItoDraft")]
        public async Task<IActionResult> CreateItoDraft([FromRoute] Guid planId, [FromBody] SilaCulinaryCreateItoDraftRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateItoDraftAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/po-drafts")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryPoDraft")]
        public async Task<IActionResult> CreatePoDraft([FromRoute] Guid planId, [FromBody] SilaCulinaryCreatePoDraftRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreatePoDraftAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/reconcile-grn")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ReconcileGrnForCulinaryPlan")]
        public async Task<IActionResult> ReconcileGrn([FromRoute] Guid planId, [FromBody] SilaCulinaryReconcileGrnRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.ReconcileGrnAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/reconcile-ito-reversal")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ReconcileItoReversalForCulinaryPlan")]
        public async Task<IActionResult> ReconcileItoReversal([FromRoute] Guid planId, [FromBody] SilaCulinaryReconcileItoReversalRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.ReconcileItoReversalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        public class LinkWeeklyBucketBody { public Guid WeeklyBucketItemId { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/lines/{lineId:guid}/link-weekly-bucket")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("LinkWeeklyBucketToProcurementLine")]
        public async Task<IActionResult> LinkWeeklyBucket([FromRoute] Guid planId, [FromRoute] Guid lineId, [FromBody] LinkWeeklyBucketBody body, CancellationToken cancellationToken)
            => Ok(await _service.LinkWeeklyBucketAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, lineId, body.WeeklyBucketItemId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/simulate")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SimulateCulinaryProcurement")]
        public async Task<IActionResult> Simulate([FromRoute] Guid planId, [FromBody] SilaCulinarySimulateProcurementRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.SimulateAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, request, cancellationToken));

        public class OptimizeBody { public decimal? MaxBudget { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/optimize")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("OptimizeCulinaryProcurement")]
        public async Task<IActionResult> Optimize([FromRoute] Guid planId, [FromBody] OptimizeBody body, CancellationToken cancellationToken)
            => Ok(await _service.OptimizeAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, body.MaxBudget, cancellationToken));

        public class AiAnalyzeBody { public string Question { get; set; } = string.Empty; }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/ai-analyze")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeCulinaryProcurementWithAi")]
        public async Task<IActionResult> AnalyzeWithAi([FromRoute] Guid planId, [FromBody] AiAnalyzeBody body, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeWithAiAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, body.Question, cancellationToken));

        public class ResolveExceptionBody { public string ResolutionStatus { get; set; } = "RESOLVED"; public string? Note { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/exceptions/{exceptionId:guid}/resolve")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ResolveCulinaryProcurementException")]
        public async Task<IActionResult> ResolveException([FromRoute] Guid planId, [FromRoute] Guid exceptionId, [FromBody] ResolveExceptionBody body, CancellationToken cancellationToken)
            => Ok(await _service.ResolveExceptionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, exceptionId, body.ResolutionStatus, body.Note, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/submit-approval")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SubmitCulinaryProcurementPlanForApproval")]
        public async Task<IActionResult> SubmitApproval([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.SubmitForApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        public class DecideApprovalBody { public string Decision { get; set; } = "APPROVE"; public string? Comment { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/decide-approval")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DecideCulinaryProcurementPlanApproval")]
        public async Task<IActionResult> DecideApproval([FromRoute] Guid planId, [FromBody] DecideApprovalBody body, CancellationToken cancellationToken)
            => Ok(await _service.DecideApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.Decision, body.Comment, cancellationToken));

        public class ChangeImpactBody { public Guid NewMrpRunId { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/change-impact")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("HandleCulinaryEventChangeImpact")]
        public async Task<IActionResult> HandleChangeImpact([FromRoute] Guid planId, [FromBody] ChangeImpactBody body, CancellationToken cancellationToken)
            => Ok(await _service.HandleEventChangeImpactAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.NewMrpRunId, cancellationToken));

        public class CancelPlanBody { public string Reason { get; set; } = "Event cancelled"; }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/cancel")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CancelCulinaryProcurementPlan")]
        public async Task<IActionResult> CancelPlan([FromRoute] Guid planId, [FromBody] CancelPlanBody body, CancellationToken cancellationToken)
            => Ok(await _service.CancelEventProcurementAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.Reason, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-procurement/reports")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryProcurementReportCatalog")]
        public IActionResult GetReports() => Ok(_service.GetReportCatalog());

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/reports/{reportCode}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportCulinaryProcurementReport")]
        public async Task<IActionResult> ExportReport([FromRoute] Guid planId, [FromRoute] string reportCode, CancellationToken cancellationToken)
            => Ok(await _service.ExportReportAsync(GetOrganizationId(), GetRoleId(), planId, reportCode, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-procurement/plans/{planId:guid}/phase3a-handover")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryPhase3AHandover")]
        public async Task<IActionResult> GetPhase3AHandover([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetPhase3AHandoverAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, cancellationToken));
    }
}
