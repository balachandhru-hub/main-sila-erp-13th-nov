using Buyer.Application.Features.Commands.AssignSilaRecipeLegacyImages;
using Buyer.Application.Features.Commands.DecideSilaRecipeLegacyBatch;
using Buyer.Application.Features.Commands.PostSilaRecipeLegacyBatch;
using Buyer.Application.Features.Commands.PublishSilaRecipeLegacyBatch;
using Buyer.Application.Features.Commands.ReprocessSilaRecipeLegacyBatch;
using Buyer.Application.Features.Commands.SubmitSilaRecipeLegacyBatch;
using Buyer.Application.Features.Commands.UpdateSilaRecipeLegacyMaterialMaps;
using Buyer.Application.Features.Commands.UploadSilaRecipeLegacyBatch;
using Buyer.Application.Features.Commands.UploadSilaRecipeLegacyImages;
using Buyer.Application.Features.Commands.ValidateSilaRecipeLegacyBatch;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyBatch;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyBatches;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyImageReport;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyPostingReport;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyPublishReport;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyRecipe;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyRecipes;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyTemplate;
using Buyer.Application.Features.Queries.GetSilaRecipeLegacyValidationReport;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.Dto;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Recipe Studio legacy recipe Excel migration (staging → validate → approve → post DRAFT → images → publish).
    /// Additive APIs only; does not change the creation wizard or ImportSilaRecipes.
    /// </summary>
    [ApiController]
    public class SilaRecipeLegacyController : BaseController
    {
        private const long MAX_UPLOAD_BYTES = 10 * 1024 * 1024;
        private const long MAX_IMAGE_BYTES = 5 * 1024 * 1024;

        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaRecipeLegacyController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/template")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyTemplate")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> Template()
        {
            SilaRecipeFileDto file = await _mediator.Send(new GetSilaRecipeLegacyTemplateQuery());
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/upload")]
        [Consumes("multipart/form-data")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("UploadSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyBatchDetailDto))]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            byte[] content = await ReadUploadAsync(file, MAX_UPLOAD_BYTES, "Legacy recipe workbook");
            SilaRecipeLegacyBatchDetailDto result = await _mediator.Send(new UploadSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                FileName = file.FileName,
                Content = content
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyBatches")]
        [SwaggerResponse(200, type: typeof(List<SilaRecipeLegacyBatchListItemDto>))]
        public async Task<IActionResult> Batches([FromQuery] int index = 0, [FromQuery] int limit = 20)
        {
            List<SilaRecipeLegacyBatchListItemDto> result = await _mediator.Send(new GetSilaRecipeLegacyBatchesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                Index = index,
                Limit = limit
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyBatchDetailDto))]
        public async Task<IActionResult> Batch([FromRoute] Guid batchId)
        {
            SilaRecipeLegacyBatchDetailDto result = await _mediator.Send(new GetSilaRecipeLegacyBatchQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/recipes")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyRecipes")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyRecipePageDto))]
        public async Task<IActionResult> Recipes(
            [FromRoute] Guid batchId,
            [FromQuery] string? status,
            [FromQuery] string? search,
            [FromQuery] int index = 0,
            [FromQuery] int limit = 50)
        {
            SilaRecipeLegacyRecipePageDto result = await _mediator.Send(new GetSilaRecipeLegacyRecipesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                Status = status,
                Search = search,
                Index = index,
                Limit = limit
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/recipes/{recipeId}")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyRecipe")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyRecipeDetailDto))]
        public async Task<IActionResult> Recipe([FromRoute] Guid batchId, [FromRoute] Guid recipeId)
        {
            SilaRecipeLegacyRecipeDetailDto result = await _mediator.Send(new GetSilaRecipeLegacyRecipeQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                RecipeId = recipeId
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/validate")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("ValidateSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyBatchDetailDto))]
        public async Task<IActionResult> Validate([FromRoute] Guid batchId)
        {
            SilaRecipeLegacyBatchDetailDto result = await _mediator.Send(new ValidateSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/validation-report")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyValidationReport")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> ValidationReport([FromRoute] Guid batchId)
        {
            SilaRecipeFileDto file = await _mediator.Send(new GetSilaRecipeLegacyValidationReportQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/material-maps")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("UpdateSilaRecipeLegacyMaterialMaps")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> MaterialMaps([FromRoute] Guid batchId, [FromBody] List<SilaRecipeLegacyMaterialMapWriteDto> mappings)
        {
            await _mediator.Send(new UpdateSilaRecipeLegacyMaterialMapsCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                Mappings = mappings ?? new List<SilaRecipeLegacyMaterialMapWriteDto>()
            });
            return Ok(new SuccessResponseDto { Id = batchId.ToString(), StatusCode = 200, Message = "Success", Description = "Material maps updated." });
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/submit-approval")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("SubmitSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> SubmitApproval([FromRoute] Guid batchId)
        {
            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new SubmitSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/decide")]
        [ApiAuthorization(Name = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("DecideSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> Decide([FromRoute] Guid batchId, [FromBody] SilaRecipeLegacyDecideDto request)
        {
            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new DecideSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                Request = request ?? new SilaRecipeLegacyDecideDto()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/post")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("PostSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> Post([FromRoute] Guid batchId)
        {
            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new PostSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/posting-report")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyPostingReport")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> PostingReport([FromRoute] Guid batchId)
        {
            SilaRecipeFileDto file = await _mediator.Send(new GetSilaRecipeLegacyPostingReportQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/images")]
        [Consumes("multipart/form-data")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("UploadSilaRecipeLegacyImages")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> UploadImages([FromRoute] Guid batchId, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                throw new BadRequestCustomException("Images are required.", "Upload at least one JPEG, PNG or WebP image.");
            }

            List<(string FileName, byte[] Content)> payloads = new List<(string, byte[])>();
            foreach (IFormFile file in files)
            {
                payloads.Add((file.FileName, await ReadUploadAsync(file, MAX_IMAGE_BYTES, "Recipe image")));
            }

            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new UploadSilaRecipeLegacyImagesCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                Files = payloads
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/images/assign")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("AssignSilaRecipeLegacyImages")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> AssignImages([FromRoute] Guid batchId, [FromBody] List<SilaRecipeLegacyImageAssignDto>? assignments)
        {
            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new AssignSilaRecipeLegacyImagesCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                Assignments = assignments
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/image-report")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyImageReport")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> ImageReport([FromRoute] Guid batchId)
        {
            SilaRecipeFileDto file = await _mediator.Send(new GetSilaRecipeLegacyImageReportQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/publish")]
        [ApiAuthorization(Name = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("PublishSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> Publish([FromRoute] Guid batchId)
        {
            // APPROVE_SILA_RECIPE required to finalize ActiveVersion (see PublishBatchAsync).
            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new PublishSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId,
                CanApprove = true
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/publish-report")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaRecipeLegacyPublishReport")]
        [SwaggerResponse(200, type: typeof(FileContentResult))]
        public async Task<IActionResult> PublishReport([FromRoute] Guid batchId)
        {
            SilaRecipeFileDto file = await _mediator.Send(new GetSilaRecipeLegacyPublishReportQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return File(file.Content, file.ContentType, file.FileName);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-legacy/batches/{batchId}/reprocess-failed")]
        [ApiAuthorization(Name = "MANAGE_SILA_RECIPE")]
        [SwaggerOperation("ReprocessSilaRecipeLegacyBatch")]
        [SwaggerResponse(200, type: typeof(SilaRecipeLegacyActionResultDto))]
        public async Task<IActionResult> ReprocessFailed([FromRoute] Guid batchId)
        {
            SilaRecipeLegacyActionResultDto result = await _mediator.Send(new ReprocessSilaRecipeLegacyBatchCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                BatchId = batchId
            });
            return Ok(result);
        }

        private async Task<byte[]> ReadUploadAsync(IFormFile? file, long maxBytes, string label)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError($"{label} upload without a file.");
                throw new BadRequestCustomException("A file is required.", $"Choose a file to upload for {label.ToLowerInvariant()}.");
            }

            if (file.Length > maxBytes)
            {
                _logger.LogError($"{label} file too large. Bytes: {file.Length}");
                throw new BadRequestCustomException("The file is too large.", $"Upload a file of at most {maxBytes / (1024 * 1024)} MB.");
            }

            using MemoryStream stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return stream.ToArray();
        }
    }
}
