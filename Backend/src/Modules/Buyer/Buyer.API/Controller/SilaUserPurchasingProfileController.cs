using Buyer.Application.Features.Commands.DeactivateSilaUserPurchasingProfile;
using Buyer.Application.Features.Commands.SetDefaultSilaUserPurchasingProfile;
using Buyer.Application.Features.Commands.UpsertSilaUserPurchasingProfile;
using Buyer.Application.Features.Queries.GetSilaUserPurchasingProfiles;
using Buyer.Application.Features.Queries.ResolveSilaUserPurchasingDefaults;
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
    /// <summary>User purchasing / account-assignment defaults for manual PO and Voice PO.</summary>
    [ApiController]
    public class SilaUserPurchasingProfileController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaUserPurchasingProfileController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/user-purchasing-profiles")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetSilaUserPurchasingProfiles")]
        [SwaggerResponse(200, type: typeof(List<SilaUserPurchasingProfileDto>))]
        public async Task<IActionResult> List([FromQuery] Guid? userId = null, [FromQuery] bool activeOnly = true)
        {
            _logger.LogDebug($"Listing purchasing profiles. UserId: {userId}");
            List<SilaUserPurchasingProfileDto> result = await _mediator.Send(new GetSilaUserPurchasingProfilesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                FilterUserId = userId,
                AdminScope = true,
                ActiveOnly = activeOnly
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/user-purchasing-profiles/me")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("GetMySilaUserPurchasingProfiles")]
        [SwaggerResponse(200, type: typeof(List<SilaUserPurchasingProfileDto>))]
        public async Task<IActionResult> ListMine()
        {
            List<SilaUserPurchasingProfileDto> result = await _mediator.Send(new GetSilaUserPurchasingProfilesQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                AdminScope = false,
                ActiveOnly = true
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/user-purchasing-profiles/resolve")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("ResolveSilaUserPurchasingDefaults")]
        [SwaggerResponse(200, type: typeof(SilaUserPurchasingDefaultsDto))]
        public async Task<IActionResult> Resolve([FromBody] ResolveSilaUserPurchasingDefaultsRequestDto request)
        {
            SilaUserPurchasingDefaultsDto result = await _mediator.Send(new ResolveSilaUserPurchasingDefaultsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request ?? new ResolveSilaUserPurchasingDefaultsRequestDto()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/user-purchasing-profiles")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertSilaUserPurchasingProfile")]
        [SwaggerResponse(200, type: typeof(SilaUserPurchasingProfileDto))]
        public async Task<IActionResult> Upsert([FromBody] SilaUserPurchasingProfileWriteDto request)
        {
            SilaUserPurchasingProfileDto result = await _mediator.Send(new UpsertSilaUserPurchasingProfileCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                AdminScope = true,
                Request = request ?? new SilaUserPurchasingProfileWriteDto()
            });
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/user-purchasing-profiles/{id}")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpdateSilaUserPurchasingProfile")]
        [SwaggerResponse(200, type: typeof(SilaUserPurchasingProfileDto))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SilaUserPurchasingProfileWriteDto request)
        {
            SilaUserPurchasingProfileDto result = await _mediator.Send(new UpsertSilaUserPurchasingProfileCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Id = id,
                AdminScope = true,
                Request = request ?? new SilaUserPurchasingProfileWriteDto()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/user-purchasing-profiles/me")]
        [ApiAuthorization(AnyOf = "CREATE_PURCHASE_ORDER|REQUEST_SILA_PURCHASE")]
        [SwaggerOperation("UpsertMySilaUserPurchasingProfile")]
        [SwaggerResponse(200, type: typeof(SilaUserPurchasingProfileDto))]
        public async Task<IActionResult> UpsertMine([FromBody] SilaUserPurchasingProfileWriteDto request)
        {
            SilaUserPurchasingProfileDto result = await _mediator.Send(new UpsertSilaUserPurchasingProfileCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                AdminScope = false,
                Request = request ?? new SilaUserPurchasingProfileWriteDto()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/user-purchasing-profiles/{id}/default")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SetDefaultSilaUserPurchasingProfile")]
        [SwaggerResponse(200, type: typeof(SilaUserPurchasingProfileDto))]
        public async Task<IActionResult> SetDefault([FromRoute] Guid id)
        {
            SilaUserPurchasingProfileDto result = await _mediator.Send(new SetDefaultSilaUserPurchasingProfileCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Id = id,
                AdminScope = true
            });
            return Ok(result);
        }

        [HttpDelete]
        [Route("api/v1/buyer/sila/user-purchasing-profiles/{id}")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DeactivateSilaUserPurchasingProfile")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            await _mediator.Send(new DeactivateSilaUserPurchasingProfileCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Id = id,
                AdminScope = true
            });
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Purchasing profile deactivated."
            });
        }
    }
}
