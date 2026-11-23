using Buyer.Application.Features.Commands.CancelSilaVoicePoSession;
using Buyer.Application.Features.Commands.ConfirmSilaVoicePoDraft;
using Buyer.Application.Features.Commands.ProcessSilaVoicePoTurn;
using Buyer.Application.Features.Commands.StartSilaVoicePoSession;
using Buyer.Application.Features.Queries.GetSilaVoicePoSession;
using Buyer.Domain.Dtos;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;
using SharedKernel.LoggerServices;
using Swashbuckle.AspNetCore.Annotations;

namespace Buyer.API.Controllers
{
    /// <summary>Conversational Voice PO. Drafts confirm then call CreatePurchaseOrder (never invents codes).</summary>
    [ApiController]
    public class SilaVoicePoController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaVoicePoController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-po/sessions")]
        [ApiAuthorization(Name = "CREATE_PURCHASE_ORDER")]
        [SwaggerOperation("StartSilaVoicePoSession")]
        [SwaggerResponse(200, type: typeof(SilaVoicePoSessionDto))]
        public async Task<IActionResult> Start([FromBody] SilaVoicePoStartRequestDto request)
        {
            _logger.LogDebug("Starting Voice PO session.");
            SilaVoicePoSessionDto result = await _mediator.Send(new StartSilaVoicePoSessionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request ?? new SilaVoicePoStartRequestDto()
            });
            return Ok(result);
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/voice-po/sessions/{sessionId}")]
        [ApiAuthorization(Name = "CREATE_PURCHASE_ORDER")]
        [SwaggerOperation("GetSilaVoicePoSession")]
        [SwaggerResponse(200, type: typeof(SilaVoicePoSessionDto))]
        public async Task<IActionResult> Get([FromRoute] Guid sessionId)
        {
            SilaVoicePoSessionDto result = await _mediator.Send(new GetSilaVoicePoSessionQuery
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SessionId = sessionId
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-po/sessions/{sessionId}/turns")]
        [ApiAuthorization(Name = "CREATE_PURCHASE_ORDER")]
        [SwaggerOperation("ProcessSilaVoicePoTurn")]
        [SwaggerResponse(200, type: typeof(SilaVoicePoSessionDto))]
        public async Task<IActionResult> Turn([FromRoute] Guid sessionId, [FromBody] SilaVoicePoTurnRequestDto request)
        {
            SilaVoicePoSessionDto result = await _mediator.Send(new ProcessSilaVoicePoTurnCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SessionId = sessionId,
                Request = request ?? new SilaVoicePoTurnRequestDto()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-po/sessions/{sessionId}/confirm")]
        [ApiAuthorization(Name = "CREATE_PURCHASE_ORDER")]
        [SwaggerOperation("ConfirmSilaVoicePoDraft")]
        [SwaggerResponse(200, type: typeof(SilaVoicePoSessionDto))]
        public async Task<IActionResult> Confirm([FromRoute] Guid sessionId, [FromBody] SilaVoicePoConfirmRequestDto request)
        {
            SilaVoicePoSessionDto result = await _mediator.Send(new ConfirmSilaVoicePoDraftCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SessionId = sessionId,
                Request = request ?? new SilaVoicePoConfirmRequestDto()
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/voice-po/sessions/{sessionId}/cancel")]
        [ApiAuthorization(Name = "CREATE_PURCHASE_ORDER")]
        [SwaggerOperation("CancelSilaVoicePoSession")]
        [SwaggerResponse(200, type: typeof(SilaVoicePoSessionDto))]
        public async Task<IActionResult> Cancel([FromRoute] Guid sessionId)
        {
            SilaVoicePoSessionDto result = await _mediator.Send(new CancelSilaVoicePoSessionCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                SessionId = sessionId
            });
            return Ok(result);
        }
    }
}
