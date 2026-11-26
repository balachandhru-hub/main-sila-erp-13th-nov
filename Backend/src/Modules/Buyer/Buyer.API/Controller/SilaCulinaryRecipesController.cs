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
    /// Phase 1A Culinary Intelligence — AI Recipe Planner, Generation, Refinement, Scaling, Documents, Images, and Draft Management.
    /// Does not modify Recipe Master, Material Master, or perform production publishing.
    /// </summary>
    [ApiController]
    public class SilaCulinaryRecipesController : BaseController
    {
        private readonly ISilaCulinaryRecipeService _service;

        public SilaCulinaryRecipesController(ISilaCulinaryRecipeService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-recipes/context")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryPlannerContext")]
        [SwaggerResponse(200, type: typeof(CulinaryPlannerContextDto))]
        public async Task<IActionResult> Context([FromQuery] Guid? locationId, CancellationToken cancellationToken)
        {
            CulinaryPlannerContextDto result = await _service.GetContextAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), locationId, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/generate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GenerateCulinaryRecipe")]
        [SwaggerResponse(200, type: typeof(CulinaryRecipeDraftDetailDto))]
        public async Task<IActionResult> Generate(
            [FromBody] CulinaryGenerateRecipeRequestDto request, CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto result = await _service.GenerateAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/refine")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RefineCulinaryRecipe")]
        [SwaggerResponse(200, type: typeof(CulinaryRefineResponseDto))]
        public async Task<IActionResult> Refine(
            [FromQuery] Guid? draftId,
            [FromBody] CulinaryRefineRequestDto request,
            CancellationToken cancellationToken)
        {
            CulinaryRefineResponseDto result = await _service.RefineAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/regenerate-section")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RegenerateCulinaryRecipeSection")]
        [SwaggerResponse(200, type: typeof(CulinaryRefineResponseDto))]
        public async Task<IActionResult> RegenerateSection(
            [FromQuery] Guid? draftId,
            [FromBody] CulinaryRegenerateSectionRequestDto request,
            CancellationToken cancellationToken)
        {
            CulinaryRefineResponseDto result = await _service.RegenerateSectionAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/scale")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ScaleCulinaryRecipeV2")]
        [SwaggerResponse(200, type: typeof(CulinaryScaleV2ResultDto))]
        public async Task<IActionResult> Scale(
            [FromQuery] Guid? draftId,
            [FromBody] CulinaryScaleV2RequestDto request,
            CancellationToken cancellationToken)
        {
            CulinaryScaleV2ResultDto result = await _service.ScaleV2Async(
                GetOrganizationId(), GetUserId(), draftId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/extract-document")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExtractCulinaryRecipeDocument")]
        [SwaggerResponse(200, type: typeof(CulinaryDocumentExtractDto))]
        public async Task<IActionResult> ExtractDocument(IFormFile file, CancellationToken cancellationToken)
        {
            CulinaryDocumentExtractDto result = await _service.ExtractDocumentAsync(
                GetOrganizationId(), GetUserId(), file, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/prepare-dish-image")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("PrepareCulinaryDishImage")]
        [SwaggerResponse(200, type: typeof(CulinaryDishImagePreparedDto))]
        public async Task<IActionResult> PrepareDishImage(IFormFile file, CancellationToken cancellationToken)
        {
            CulinaryDishImagePreparedDto result = await _service.PrepareDishImageAsync(
                GetOrganizationId(), GetUserId(), file, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/cancel-generation/{generationReference}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CancelCulinaryGeneration")]
        public async Task<IActionResult> CancelGeneration(string generationReference, CancellationToken cancellationToken)
        {
            await _service.CancelGenerationAsync(GetOrganizationId(), GetUserId(), generationReference, cancellationToken);
            return Ok(new { cancelled = true, generationReference });
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryRecipeDrafts")]
        [SwaggerResponse(200, type: typeof(List<CulinaryRecipeDraftSummaryDto>))]
        public async Task<IActionResult> ListDrafts(
            [FromQuery] string? status,
            [FromQuery] string? search,
            CancellationToken cancellationToken)
        {
            List<CulinaryRecipeDraftSummaryDto> result = await _service.ListDraftsAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), status, search, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts/{draftId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryRecipeDraft")]
        [SwaggerResponse(200, type: typeof(CulinaryRecipeDraftDetailDto))]
        public async Task<IActionResult> GetDraft(Guid draftId, CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto result = await _service.GetDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, cancellationToken);
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts/{draftId:guid}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SaveCulinaryRecipeDraft")]
        [SwaggerResponse(200, type: typeof(CulinaryRecipeDraftDetailDto))]
        public async Task<IActionResult> SaveDraft(
            Guid draftId,
            [FromBody] CulinarySaveDraftRequestDto request,
            CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto result = await _service.SaveDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, request, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts/{draftId:guid}/duplicate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DuplicateCulinaryRecipeDraft")]
        [SwaggerResponse(200, type: typeof(CulinaryRecipeDraftDetailDto))]
        public async Task<IActionResult> DuplicateDraft(Guid draftId, CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto result = await _service.DuplicateDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts/{draftId:guid}/restore/{revisionNumber:int}")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RestoreCulinaryRecipeDraftRevision")]
        [SwaggerResponse(200, type: typeof(CulinaryRecipeDraftDetailDto))]
        public async Task<IActionResult> RestoreRevision(Guid draftId, int revisionNumber, CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto result = await _service.RestoreRevisionAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, revisionNumber, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts/{draftId:guid}/cancel")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CancelCulinaryRecipeDraft")]
        [SwaggerResponse(200, type: typeof(CulinaryRecipeDraftDetailDto))]
        public async Task<IActionResult> CancelDraft(Guid draftId, CancellationToken cancellationToken)
        {
            CulinaryRecipeDraftDetailDto result = await _service.CancelDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), draftId, cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-recipes/drafts/{draftId:guid}/export")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportCulinaryRecipeDraft")]
        public async Task<IActionResult> ExportDraft(
            Guid draftId, [FromQuery] string format = "JSON", CancellationToken cancellationToken = default)
        {
            var (content, contentType, fileName) = await _service.ExportDraftAsync(
                GetOrganizationId(), GetUserId(), GetRoleId(), draftId, format, cancellationToken);
            return File(content, contentType, fileName);
        }
    }
}
