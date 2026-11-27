using System.Security.Claims;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Phase 1C Culinary Intelligence — Material Creation/Extension/Reactivation/UOM/Spec Requests,
    /// Deterministic Recipe Costing & Profitability Simulation, Costing Snapshots, AI Cost Optimization,
    /// Recipe Readiness, Revision-Controlled Approvals, Production Recipe ID Creation, Image Assignment,
    /// Publishing, Version Management, Dashboards, Reports, and Phase 2A Handover.
    /// </summary>
    [ApiController]
    public class SilaCulinaryLifecycleController : BaseController
    {
        private readonly ISilaCulinaryLifecycleService _service;

        public SilaCulinaryLifecycleController(ISilaCulinaryLifecycleService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        // ---------------------------------------------------------------------
        // Material Requests (§5 - §17, §39)
        // ---------------------------------------------------------------------

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryMaterialRequests")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryMaterialRequestDto>))]
        public async Task<IActionResult> ListMaterialRequests(
            [FromQuery] string? status,
            [FromQuery] string? requestType,
            [FromQuery] Guid? propertyId,
            [FromQuery] Guid? draftId,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialRequestDto> res = await _service.ListMaterialRequestsAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), status, requestType, propertyId, draftId, search, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMaterialRequest")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> GetMaterialRequest(Guid requestId, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.GetMaterialRequestAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), requestId, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/assist")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AssistMaterialRequestPreparation")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestAssistResultDto))]
        public async Task<IActionResult> AssistMaterialRequest(
            [FromBody] SilaCulinaryMaterialRequestAssistInputDto input, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestAssistResultDto res = await _service.AssistMaterialRequestAsync(
                GetOrganizationId(), input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryMaterialRequest")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> CreateMaterialRequest(
            [FromBody] SilaCulinaryMaterialRequestUpsertDto input, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.CreateMaterialRequestAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/bulk-prepare")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("BulkPrepareMaterialRequests")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryMaterialRequestDto>))]
        public async Task<IActionResult> BulkPrepare(
            [FromBody] SilaCulinaryBulkMaterialRequestDto input, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialRequestDto> res = await _service.BulkPrepareMaterialRequestsAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), input, cancellationToken);
            return Ok(res);
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpdateCulinaryMaterialRequest")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> UpdateMaterialRequest(
            Guid requestId, [FromBody] SilaCulinaryMaterialRequestUpsertDto input, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.UpdateMaterialRequestAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), requestId, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}/submit")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SubmitCulinaryMaterialRequest")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> SubmitMaterialRequest(Guid requestId, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.SubmitMaterialRequestAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), requestId, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}/decide")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MASTER_DATA|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("DecideCulinaryMaterialRequest")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> DecideMaterialRequest(
            Guid requestId, [FromBody] SilaCulinaryMaterialRequestDecisionDto input, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.DecideMaterialRequestAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), requestId, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}/resubmit")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ResubmitCulinaryMaterialRequest")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> ResubmitMaterialRequest(
            Guid requestId, [FromBody] SilaCulinaryMaterialRequestUpsertDto? updates, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.ResubmitMaterialRequestAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), requestId, updates, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}/execute")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExecuteMaterialCreationOrMaintenance")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> ExecuteMaterialCreation(
            Guid requestId, [FromQuery] string? idempotencyKey, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.ExecuteMaterialCreationAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), requestId, idempotencyKey, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/{requestId:guid}/verify-erp-material")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("VerifyManualErpMaterialReference")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDto))]
        public async Task<IActionResult> VerifyManualErpMaterial(
            Guid requestId, [FromBody] SilaCulinaryManualErpVerifyDto input, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDto res = await _service.VerifyManualErpMaterialAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), requestId, input, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMaterialRequestDashboard")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialRequestDashboardDto))]
        public async Task<IActionResult> MaterialRequestDashboard(
            [FromQuery] Guid? propertyId, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialRequestDashboardDto res = await _service.GetMaterialRequestDashboardAsync(
                GetOrganizationId(), propertyId, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/material-requests/export")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportMaterialRequestsReport")]
        public async Task<IActionResult> ExportMaterialRequests(
            [FromQuery] string format = "XLSX",
            [FromQuery] string? status = null,
            CancellationToken cancellationToken = default)
        {
            (byte[] content, string contentType, string fileName) = await _service.ExportMaterialRequestsReportAsync(
                GetOrganizationId(), format, status, cancellationToken);
            return File(content, contentType, fileName);
        }

        // ---------------------------------------------------------------------
        // Recipe Costing, Scenarios, Snapshots & Optimization (§19 - §38)
        // ---------------------------------------------------------------------

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/costing/calculate")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_PRICE")]
        [SwaggerOperation("CalculateCulinaryRecipeCosting")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryCostingResultDto))]
        public async Task<IActionResult> CalculateCosting(
            Guid draftId, [FromBody] SilaCulinaryCostingCalculateRequestDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryCostingResultDto res = await _service.CalculateDraftCostingAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                input ?? new SilaCulinaryCostingCalculateRequestDto(), cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/costing/snapshots")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_PRICE|APPROVE_SILA_PRICE")]
        [SwaggerOperation("ListCostingSnapshots")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryCostingResultDto>))]
        public async Task<IActionResult> ListSnapshots(Guid draftId, CancellationToken cancellationToken)
        {
            List<SilaCulinaryCostingResultDto> res = await _service.ListCostingSnapshotsAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/costing/optimize")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_PRICE")]
        [SwaggerOperation("OptimizeCulinaryRecipeCost")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryCostOptimizationResultDto))]
        public async Task<IActionResult> OptimizeCosting(
            Guid draftId, [FromBody] SilaCulinaryCostOptimizationRequestDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryCostOptimizationResultDto res = await _service.OptimizeDraftCostingAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                input ?? new SilaCulinaryCostOptimizationRequestDto(), cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/costing/bulk-recost")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|APPROVE_SILA_PRICE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("BulkRecostCulinaryRecipes")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryBulkRecostResultDto))]
        public async Task<IActionResult> BulkRecost(
            [FromBody] SilaCulinaryBulkRecostRequestDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryBulkRecostResultDto res = await _service.BulkRecostAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(),
                input ?? new SilaCulinaryBulkRecostRequestDto(), cancellationToken);
            return Ok(res);
        }

        // ---------------------------------------------------------------------
        // Readiness, Approvals, Recipe ID, Image, Publishing & Phase 2A (§40 - §59)
        // ---------------------------------------------------------------------

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/readiness")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetRecipeReadinessChecklist")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeReadinessDto))]
        public async Task<IActionResult> Readiness(Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeReadinessDto res = await _service.GetRecipeReadinessAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/approval-package")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetRecipeApprovalPackage")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryApprovalPackageDto))]
        public async Task<IActionResult> ApprovalPackage(Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryApprovalPackageDto res = await _service.GetApprovalPackageAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/submit-approval")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("SubmitRecipeForApproval")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> SubmitRecipe(
            Guid draftId, [FromBody] SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.SubmitRecipeForApprovalAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/approve")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ApproveCulinaryRecipeDraft")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> ApproveRecipe(
            Guid draftId, [FromBody] SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.DecideRecipeApprovalAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, true, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/reject")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("RejectCulinaryRecipeDraft")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> RejectRecipe(
            Guid draftId, [FromBody] SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.DecideRecipeApprovalAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, false, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/create-production-id")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("CreateProductionRecipeIdFromDraft")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> CreateProductionId(
            Guid draftId, [FromBody] SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.CreateProductionRecipeAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/assign-image")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("AssignProductionRecipeImage")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> AssignImage(
            Guid draftId, IFormFile file, [FromForm] string? imageOrigin, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No image file uploaded.");
            }
            using MemoryStream ms = new();
            await file.CopyToAsync(ms, cancellationToken);
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.AssignRecipeImageAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, ms.ToArray(), imageOrigin, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/publish")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("PublishCulinaryProductionRecipe")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> Publish(
            Guid draftId, [FromBody] SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.PublishProductionRecipeAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, input, cancellationToken);
            return Ok(res);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/new-version")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("CreateNewProductionRecipeVersion")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryRecipeLifecycleSummaryDto))]
        public async Task<IActionResult> CreateNewVersion(
            Guid draftId, [FromBody] SilaCulinaryRecipeApproveActionDto? input, CancellationToken cancellationToken)
        {
            SilaCulinaryRecipeLifecycleSummaryDto res = await _service.CreateNewProductionVersionAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, input, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/versions")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetRecipeVersionHistory")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryRecipeVersionHistoryItemDto>))]
        public async Task<IActionResult> VersionHistory(Guid draftId, CancellationToken cancellationToken)
        {
            List<SilaCulinaryRecipeVersionHistoryItemDto> res = await _service.GetVersionHistoryAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetCulinaryLifecycleDashboard")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryLifecycleDashboardDto))]
        public async Task<IActionResult> LifecycleDashboard([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
        {
            SilaCulinaryLifecycleDashboardDto res = await _service.GetLifecycleDashboardAsync(
                GetOrganizationId(), propertyId, cancellationToken);
            return Ok(res);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/drafts/{draftId:guid}/reports/export")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ExportCulinaryLifecycleReport")]
        public async Task<IActionResult> ExportLifecycleReport(
            Guid draftId,
            [FromQuery] string reportType = "COSTING",
            [FromQuery] string format = "XLSX",
            CancellationToken cancellationToken = default)
        {
            (byte[] content, string contentType, string fileName) = await _service.ExportLifecycleReportAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, reportType, format, cancellationToken);
            return File(content, contentType, fileName);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-lifecycle/phase2a-handover/{productionRecipeId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetPhase2AMenuPlannerHandover")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryPhase2AHandoverDto))]
        public async Task<IActionResult> Phase2AHandover(
            Guid productionRecipeId, [FromQuery] int? version, CancellationToken cancellationToken)
        {
            SilaCulinaryPhase2AHandoverDto res = await _service.GetPhase2AHandoverAsync(
                GetOrganizationId(), productionRecipeId, version, cancellationToken);
            return Ok(res);
        }
    }
}
