using Buyer.Application.Features.Commands.DeactivateSilaUserPoFlexConfig;
using Buyer.Application.Features.Commands.UpsertSilaUserPoFlexConfig;
using Buyer.Application.Features.Queries.GetSilaUserPoFlexConfigs;
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
    /// <summary>Flexible PO Defaults & Account Assignment (param/value + conditions).</summary>
    [ApiController]
    public class SilaUserPoFlexConfigController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaUserPoFlexConfigController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/user-po-flex-configs")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetSilaUserPoFlexConfigs")]
        [SwaggerResponse(200, type: typeof(List<SilaUserPoFlexConfigDto>))]
        public async Task<IActionResult> List(
            [FromQuery] Guid? userId = null,
            [FromQuery] string? configType = "PO_CREATE",
            [FromQuery] bool activeOnly = true)
        {
            _logger.LogDebug($"Listing PO flex configs. UserId: {userId}, Type: {configType}");
            List<SilaUserPoFlexConfigDto> result = await _mediator.Send(new GetSilaUserPoFlexConfigsQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                FilterUserId = userId,
                ConfigType = configType,
                AdminScope = true,
                ActiveOnly = activeOnly
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/user-po-flex-configs")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertSilaUserPoFlexConfig")]
        [SwaggerResponse(200, type: typeof(SilaUserPoFlexConfigDto))]
        public async Task<IActionResult> Upsert([FromBody] SilaUserPoFlexConfigWriteDto request)
        {
            SilaUserPoFlexConfigDto result = await _mediator.Send(new UpsertSilaUserPoFlexConfigCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                AdminScope = true,
                Request = request ?? new SilaUserPoFlexConfigWriteDto()
            });
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/user-po-flex-configs/{id}")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpdateSilaUserPoFlexConfig")]
        [SwaggerResponse(200, type: typeof(SilaUserPoFlexConfigDto))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SilaUserPoFlexConfigWriteDto request)
        {
            SilaUserPoFlexConfigDto result = await _mediator.Send(new UpsertSilaUserPoFlexConfigCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Id = id,
                AdminScope = true,
                Request = request ?? new SilaUserPoFlexConfigWriteDto()
            });
            return Ok(result);
        }

        [HttpDelete]
        [Route("api/v1/buyer/sila/user-po-flex-configs/{id}")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DeactivateSilaUserPoFlexConfig")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Deactivate([FromRoute] Guid id)
        {
            await _mediator.Send(new DeactivateSilaUserPoFlexConfigCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Id = id
            });
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "PO defaults config deactivated."
            });
        }
    }
}
