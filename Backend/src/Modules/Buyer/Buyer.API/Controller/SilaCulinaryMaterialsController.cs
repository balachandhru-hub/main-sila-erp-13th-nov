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
    /// Phase 1B Culinary Intelligence — Advanced Material Intelligence Engine, Candidate Matching,
    /// UOM/Stock/Price Enrichment, Chef Confirmation, Substitutions, Aliases, Index Admin, and Phase 1C Handover.
    /// Does not create production Material Master records, reservations, POs, or live SAP postings.
    /// </summary>
    [ApiController]
    public class SilaCulinaryMaterialsController : BaseController
    {
        private readonly ISilaCulinaryMaterialService _service;

        public SilaCulinaryMaterialsController(ISilaCulinaryMaterialService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/workspace")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMaterialWorkspace")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialWorkspaceDto))]
        public async Task<IActionResult> Workspace(Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialWorkspaceDto result = await _service.GetWorkspaceAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/match")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("MatchDraftIngredients")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialWorkspaceDto))]
        public async Task<IActionResult> MatchDraft(
            Guid draftId, [FromBody] SilaCulinaryMaterialMatchRequestDto? request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialWorkspaceDto result = await _service.MatchDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                request ?? new SilaCulinaryMaterialMatchRequestDto(), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/ingredients/{ingredientTempRef}/match")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("MatchSingleDraftIngredient")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialMappingLineDto))]
        public async Task<IActionResult> MatchSingle(
            Guid draftId, string ingredientTempRef,
            [FromBody] SilaCulinaryMaterialMatchRequestDto? request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialMappingLineDto result = await _service.MatchSingleIngredientAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                ingredientTempRef, request ?? new SilaCulinaryMaterialMatchRequestDto(), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/recalculate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RecalculateDraftMaterialRequirements")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialWorkspaceDto))]
        public async Task<IActionResult> Recalculate(
            Guid draftId, [FromBody] SilaCulinaryMaterialRecalcDto request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialWorkspaceDto result = await _service.RecalculateAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/ingredients/{ingredientTempRef}/confirm")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ConfirmIngredientMaterialMapping")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialMappingLineDto))]
        public async Task<IActionResult> Confirm(
            Guid draftId, string ingredientTempRef,
            [FromBody] SilaCulinaryMaterialActionDto? request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialMappingLineDto result = await _service.ConfirmMappingAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                ingredientTempRef, request ?? new SilaCulinaryMaterialActionDto(), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/bulk-accept-exact")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("BulkAcceptExactMaterialMappings")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialWorkspaceDto))]
        public async Task<IActionResult> BulkAcceptExact(
            Guid draftId, [FromBody] SilaCulinaryMaterialBulkAcceptDto? request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialWorkspaceDto result = await _service.BulkAcceptExactAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                request ?? new SilaCulinaryMaterialBulkAcceptDto(), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/ingredients/{ingredientTempRef}/reject")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RejectIngredientMaterialMapping")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialMappingLineDto))]
        public async Task<IActionResult> Reject(
            Guid draftId, string ingredientTempRef,
            [FromBody] SilaCulinaryMaterialActionDto? request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialMappingLineDto result = await _service.RejectMappingAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                ingredientTempRef, request ?? new SilaCulinaryMaterialActionDto(), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/ingredients/{ingredientTempRef}/request-draft")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("PreparePhase1CMaterialRequestDraft")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialMappingLineDto))]
        public async Task<IActionResult> PrepareRequestDraft(
            Guid draftId, string ingredientTempRef,
            [FromBody] SilaCulinaryMaterialRequestDraftCreateDto request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialMappingLineDto result = await _service.PrepareRequestDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                ingredientTempRef, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/ingredients/{ingredientTempRef}/substitutions")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SuggestIngredientSubstitutions")]
        public async Task<IActionResult> SuggestSubstitutions(
            Guid draftId, string ingredientTempRef, CancellationToken cancellationToken)
        {
            object result = await _service.SuggestSubstitutionsAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                ingredientTempRef, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/ingredients/{ingredientTempRef}/substitutions/apply")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ApplyIngredientSubstitutionToDraft")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialMappingLineDto))]
        public async Task<IActionResult> ApplySubstitution(
            Guid draftId, string ingredientTempRef,
            [FromBody] SilaCulinarySubstitutionApplyDto request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialMappingLineDto result = await _service.ApplySubstitutionAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId,
                ingredientTempRef, request, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/search")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("SearchMaterialsForCulinaryMapping")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryMaterialSearchItemDto>))]
        public async Task<IActionResult> Search(
            [FromQuery] string? q, [FromQuery] Guid? propertyId, [FromQuery] int limit = 20, CancellationToken cancellationToken = default)
        {
            List<SilaCulinaryMaterialSearchItemDto> result = await _service.SearchMaterialsAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), q, propertyId, limit, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/history")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMaterialMappingHistory")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryMaterialHistoryDto>))]
        public async Task<IActionResult> History(
            Guid draftId, [FromQuery] string? ingredientTempRef, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialHistoryDto> result = await _service.GetMappingHistoryAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, ingredientTempRef, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/aliases")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryMaterialAliases")]
        [SwaggerResponse(200, type: typeof(List<SilaCulinaryMaterialAliasDto>))]
        public async Task<IActionResult> ListAliases(
            [FromQuery] string? status, [FromQuery] string? search, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMaterialAliasDto> result = await _service.ListAliasesAsync(
                GetOrganizationId(), status, search, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/aliases")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryMaterialAlias")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialAliasDto))]
        public async Task<IActionResult> CreateAlias(
            [FromBody] SilaCulinaryMaterialAliasCreateDto request, CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialAliasDto result = await _service.CreateAliasAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/aliases/{aliasId:guid}/decision")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MASTER_DATA|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("DecideCulinaryMaterialAlias")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialAliasDto))]
        public async Task<IActionResult> DecideAlias(
            Guid aliasId, [FromQuery] bool approve = true, CancellationToken cancellationToken = default)
        {
            SilaCulinaryMaterialAliasDto result = await _service.ApproveAliasAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), aliasId, approve, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/index/status")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMaterialIndexStatus")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialIndexStatusDto))]
        public async Task<IActionResult> IndexStatus(CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialIndexStatusDto result = await _service.GetIndexStatusAsync(GetOrganizationId(), cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-materials/index/rebuild")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MASTER_DATA|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("RebuildCulinaryMaterialIndex")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialIndexStatusDto))]
        public async Task<IActionResult> RebuildIndex(
            [FromQuery] bool incrementalOnly = false, CancellationToken cancellationToken = default)
        {
            SilaCulinaryMaterialIndexStatusDto result = await _service.RebuildIndexAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), incrementalOnly, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMaterialDashboard")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryMaterialDashboardDto))]
        public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        {
            SilaCulinaryMaterialDashboardDto result = await _service.GetDashboardAsync(GetOrganizationId(), cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/phase1c-handover")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetPhase1CMaterialHandoverContract")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryPhase1CHandoverDto))]
        public async Task<IActionResult> Phase1CHandover(Guid draftId, CancellationToken cancellationToken)
        {
            SilaCulinaryPhase1CHandoverDto result = await _service.GetPhase1CHandoverAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-materials/drafts/{draftId:guid}/report/export")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportCulinaryMaterialReport")]
        public async Task<IActionResult> ExportReport(
            Guid draftId, [FromQuery] string format = "XLSX", [FromQuery] string? status = null, CancellationToken cancellationToken = default)
        {
            var (content, contentType, fileName) = await _service.ExportReportAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, format, status, cancellationToken);
            return File(content, contentType, fileName);
        }
    }
}
