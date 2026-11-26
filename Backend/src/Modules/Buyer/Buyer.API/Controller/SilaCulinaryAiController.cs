using Buyer.Application.Features.Commands.SaveSilaCulinaryAiConfig;
using Buyer.Application.Features.Commands.TestSilaCulinaryAiConnection;
using Buyer.Application.Features.Queries.GetSilaCulinaryAiConfig;
using Buyer.Application.Features.Queries.GetSilaCulinaryAiStatus;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>
    /// Culinary Intelligence — AI Engine Setup, Feature Flags, and Provider Connection Testing (Phase 0).
    /// Credentials are encrypted server-side via IIntegrationCredentialProtector and never exposed to Cloud/Mobile clients.
    /// </summary>
    [ApiController]
    public class SilaCulinaryAiController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaCulinaryAiController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-ai/status")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetSilaCulinaryAiStatus")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryAiStatusDto))]
        public async Task<IActionResult> Status()
        {
            SilaCulinaryAiStatusDto result = await _mediator.Send(new GetSilaCulinaryAiStatusQuery
            {
                OrganizationId = GetOrganizationId()
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-ai/config")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_MASTER_DATA|VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("GetSilaCulinaryAiConfig")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryAiConfigDto))]
        public async Task<IActionResult> Config()
        {
            _logger.LogDebug("Fetching Culinary AI Engine setup.");
            SilaCulinaryAiConfigDto result = await _mediator.Send(new GetSilaCulinaryAiConfigQuery
            {
                OrganizationId = GetOrganizationId(),
                RoleId = GetRoleId()
            });
            return Ok(result);
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-ai/config")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SaveSilaCulinaryAiConfig")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryAiConfigDto))]
        public async Task<IActionResult> SaveConfig([FromBody] SilaCulinaryAiConfigWriteDto request)
        {
            _logger.LogDebug($"Saving Culinary AI Engine setup. Mode: {request.OperatingMode}");
            SilaCulinaryAiConfigDto result = await _mediator.Send(new SaveSilaCulinaryAiConfigCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                RoleId = GetRoleId(),
                Request = request
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-ai/test-connection")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("TestSilaCulinaryAiConnection")]
        [SwaggerResponse(200, type: typeof(SilaCulinaryAiConnectionTestResultDto))]
        public async Task<IActionResult> TestConnection([FromBody] SilaCulinaryAiConnectionTestRequestDto request)
        {
            _logger.LogDebug($"Testing Culinary AI connection. Provider: {request.Provider}");
            SilaCulinaryAiConnectionTestResultDto result = await _mediator.Send(new TestSilaCulinaryAiConnectionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request
            });
            return Ok(result);
        }
    }
}
