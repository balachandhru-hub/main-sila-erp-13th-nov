using Buyer.Application.Features.Commands.CreateSilaRecipe;
using Buyer.Application.Features.Commands.DeactivateSilaRecipe;
using Buyer.Application.Features.Commands.DeleteSilaRecipeDraft;
using Buyer.Application.Features.Commands.DecideSilaRecipe;
using Buyer.Application.Features.Commands.RemoveSilaRecipePicture;
using Buyer.Application.Features.Commands.SetSilaRecipePicture;
using Buyer.Application.Features.Commands.SubmitSilaRecipe;
using Buyer.Application.Features.Commands.UpdateSilaRecipe;
using Buyer.Application.Features.Queries.GetSilaRecipe;
using Buyer.Application.Features.Queries.GetSilaRecipeApprovals;
using Buyer.Application.Features.Queries.GetSilaRecipePicture;
using Buyer.Application.Features.Queries.GetSilaRecipes;
using Buyer.Application.Features.Queries.GetSilaRecipeWorkflows;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// SILA ME recipes (menu items with versions, ingredients, outlet prices and costs) and their approval through the SILA approval engine.
    /// </summary>
    [ApiController]
    public class SilaRecipeController : BaseController
    {
        private const long PICTURE_UPLOAD_LIMIT = 6 * 1024 * 1024;

        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaRecipeController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipes")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipes")]
        [SwaggerResponse(200, type: typeof(SilaRecipePageDto))]
        public async Task<IActionResult> Recipes(
            [FromQuery] string? status,
            [FromQuery] string? itemMode,
            [FromQuery] string? search,
            [FromQuery] Guid? familyId,
            [FromQuery] Guid? categoryId,
            [FromQuery] bool? active,
            [FromQuery] int index = 0,
            [FromQuery] int limit = 50)
        {
            _logger.LogDebug($"Fetching recipes. Status: {status}, ItemMode: {itemMode}, Search: {search}, Index: {index}, Limit: {limit}");
            SilaRecipePageDto result = await _mediator.Send(new GetSilaRecipesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                Status = status,
                ItemMode = itemMode,
                Search = search,
                FamilyId = familyId,
                CategoryId = categoryId,
                Active = active,
                Index = index,
                Limit = limit
            });
            _logger.LogDebug($"Recipes fetched. Count: {result.Items.Count}, Total: {result.Total}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipes/{recipeId}")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SilaRecipeDetailDto))]
        public async Task<IActionResult> Recipe([FromRoute] Guid recipeId, [FromQuery] int? version)
        {
            _logger.LogDebug($"Fetching recipe. RecipeId: {recipeId}, Version: {version}");
            SilaRecipeDetailDto result = await _mediator.Send(new GetSilaRecipeQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId,
                Version = version
            });
            _logger.LogDebug($"Recipe fetched. RecipeId: {recipeId}, Status: {result.Status}, Version: {result.ViewVersion}");
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipes")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("CreateSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> CreateRecipe([FromBody] SilaRecipeWriteDto request)
        {
            _logger.LogDebug($"Creating recipe. Name: {request.Name}");
            Guid id = await _mediator.Send(new CreateSilaRecipeCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                Request = request
            });
            _logger.LogDebug($"Recipe created. RecipeId: {id}");
            return Ok(new SuccessResponseDto { Id = id.ToString(), StatusCode = 200, Message = "Success", Description = "Recipe created." });
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/recipes/{recipeId}")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("UpdateSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> UpdateRecipe([FromRoute] Guid recipeId, [FromBody] SilaRecipeWriteDto request)
        {
            _logger.LogDebug($"Updating recipe. RecipeId: {recipeId}");
            await _mediator.Send(new UpdateSilaRecipeCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId,
                Request = request
            });
            _logger.LogDebug($"Recipe updated. RecipeId: {recipeId}");
            return Ok(new SuccessResponseDto { Id = recipeId.ToString(), StatusCode = 200, Message = "Success", Description = "Recipe saved." });
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipes/{recipeId}/submit")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("SubmitSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> SubmitRecipe([FromRoute] Guid recipeId)
        {
            _logger.LogDebug($"Submitting recipe. RecipeId: {recipeId}");
            await _mediator.Send(new SubmitSilaRecipeCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId
            });
            _logger.LogDebug($"Recipe submitted. RecipeId: {recipeId}");
            return Ok(new SuccessResponseDto { Id = recipeId.ToString(), StatusCode = 200, Message = "Success", Description = "Recipe sent for approval." });
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipes/{recipeId}/approve")]
        [ApiAuthorization(Name = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ApproveSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> ApproveRecipe([FromRoute] Guid recipeId, [FromBody] SilaRecipeDecisionDto request)
        {
            _logger.LogDebug($"Approving recipe. RecipeId: {recipeId}");
            await _mediator.Send(new DecideSilaRecipeCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId,
                Approve = true,
                Request = request ?? new SilaRecipeDecisionDto()
            });
            _logger.LogDebug($"Recipe approval recorded. RecipeId: {recipeId}");
            return Ok(new SuccessResponseDto { Id = recipeId.ToString(), StatusCode = 200, Message = "Success", Description = "Approval recorded." });
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipes/{recipeId}/reject")]
        [ApiAuthorization(Name = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("RejectSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> RejectRecipe([FromRoute] Guid recipeId, [FromBody] SilaRecipeDecisionDto request)
        {
            _logger.LogDebug($"Rejecting recipe. RecipeId: {recipeId}");
            await _mediator.Send(new DecideSilaRecipeCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId,
                Approve = false,
                Request = request ?? new SilaRecipeDecisionDto()
            });
            _logger.LogDebug($"Recipe rejection recorded. RecipeId: {recipeId}");
            return Ok(new SuccessResponseDto { Id = recipeId.ToString(), StatusCode = 200, Message = "Success", Description = "Recipe rejected." });
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipes/{recipeId}/deactivate")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("DeactivateSilaRecipe")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DeactivateRecipe([FromRoute] Guid recipeId)
        {
            _logger.LogDebug($"Deactivating recipe. RecipeId: {recipeId}");
            await _mediator.Send(new DeactivateSilaRecipeCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId
            });
            _logger.LogDebug($"Recipe deactivated. RecipeId: {recipeId}");
            return Ok(new SuccessResponseDto { Id = recipeId.ToString(), StatusCode = 200, Message = "Success", Description = "Recipe deactivated." });
        }

        /// <summary>
        /// Deletes a Draft recipe (soft-delete) or discards a Draft revision of an approved recipe without touching the approved selling version.
        /// </summary>
        [HttpPost]
        [Route("api/v1/buyer/sila/recipes/{recipeId}/draft/delete")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("DeleteSilaRecipeDraft")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> DeleteDraft([FromRoute] Guid recipeId)
        {
            _logger.LogDebug($"Deleting recipe draft. RecipeId: {recipeId}");
            await _mediator.Send(new DeleteSilaRecipeDraftCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId
            });
            _logger.LogDebug($"Recipe draft deleted. RecipeId: {recipeId}");
            return Ok(new SuccessResponseDto { Id = recipeId.ToString(), StatusCode = 200, Message = "Success", Description = "Draft recipe deleted." });
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipes/{recipeId:guid}/picture")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(PICTURE_UPLOAD_LIMIT)]
        [SwaggerOperation("SetSilaRecipePicture")]
        [SwaggerResponse(200, type: typeof(SilaRecipePictureDto))]
        public async Task<IActionResult> SetPicture([FromRoute] Guid recipeId, IFormFile file)
        {
            _logger.LogDebug($"Setting recipe picture. RecipeId: {recipeId}, Bytes: {file?.Length}");
            byte[] content = Array.Empty<byte>();
            if (file != null && file.Length > 0)
            {
                using MemoryStream stream = new MemoryStream();
                await file.CopyToAsync(stream);
                content = stream.ToArray();
            }

            SilaRecipePictureDto result = await _mediator.Send(new SetSilaRecipePictureCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId,
                Request = new SilaRecipePictureUploadDto
                {
                    FileName = file?.FileName ?? string.Empty,
                    ContentType = file?.ContentType,
                    Content = content
                }
            });
            _logger.LogDebug($"Recipe picture saved. RecipeId: {recipeId}, Slot: {result.Slot}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipes/{recipeId:guid}/picture")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipePicture")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> GetPicture([FromRoute] Guid recipeId, [FromQuery] bool preferDraft = false)
        {
            _logger.LogDebug($"Fetching recipe picture. RecipeId: {recipeId}, PreferDraft: {preferDraft}");
            SilaRecipePictureFileDto result = await _mediator.Send(new GetSilaRecipePictureQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId,
                PreferDraft = preferDraft
            });
            _logger.LogDebug($"Recipe picture fetched. RecipeId: {recipeId}, Bytes: {result.Content.Length}");
            return File(result.Content, result.ContentType, result.FileName);
        }

        [HttpDelete]
        [Route("api/v1/buyer/sila/recipes/{recipeId:guid}/picture")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("RemoveSilaRecipePicture")]
        [SwaggerResponse(200, type: typeof(SilaRecipePictureDto))]
        public async Task<IActionResult> RemovePicture([FromRoute] Guid recipeId)
        {
            _logger.LogDebug($"Removing recipe picture. RecipeId: {recipeId}");
            SilaRecipePictureDto result = await _mediator.Send(new RemoveSilaRecipePictureCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                RecipeId = recipeId
            });
            _logger.LogDebug($"Recipe picture removed. RecipeId: {recipeId}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-approvals")]
        [ApiAuthorization(Name = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeApprovals")]
        [SwaggerResponse(200, type: typeof(List<SilaRecipeListItemDto>))]
        public async Task<IActionResult> RecipeApprovals([FromQuery] int index = 0, [FromQuery] int limit = 50)
        {
            _logger.LogDebug($"Fetching recipes pending my approval. Index: {index}, Limit: {limit}");
            List<SilaRecipeListItemDto> result = await _mediator.Send(new GetSilaRecipeApprovalsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                Index = index,
                Limit = limit
            });
            _logger.LogDebug($"Recipes pending my approval fetched. Count: {result.Count}");
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-approvals/workflows")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeWorkflows")]
        [SwaggerResponse(200, type: typeof(List<SilaApprovalWorkflowDto>))]
        public async Task<IActionResult> RecipeWorkflows()
        {
            _logger.LogDebug("Fetching recipe approval workflows.");
            List<SilaApprovalWorkflowDto> result = await _mediator.Send(new GetSilaRecipeWorkflowsQuery { OrganizationId = GetOrganizationId() });
            _logger.LogDebug($"Recipe approval workflows fetched. Count: {result.Count}");
            return Ok(result);
        }
    }
}
