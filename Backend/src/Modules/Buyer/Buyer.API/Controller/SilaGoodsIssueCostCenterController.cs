using Buyer.Application.Features.Commands.DeleteSilaGoodsIssueCostCenter;
using Buyer.Application.Features.Commands.SaveSilaGoodsIssueCostCenter;
using Buyer.Application.Features.Queries.GetSilaGoodsIssueCostCenters;
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
    /// Recipe setup: Goods Issue cost centers (Property-Plant and Property-Plant-StorageLocation).
    /// </summary>
    [ApiController]
    public class SilaGoodsIssueCostCenterController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public SilaGoodsIssueCostCenterController(IMediator mediator, ILoggerManager logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/recipe-setup/goods-issue-cost-centers")]
        [ApiAuthorization(Name = "VIEW_SILA_RECIPE")]
        [SwaggerOperation("GetSilaGoodsIssueCostCenters")]
        [SwaggerResponse(200, type: typeof(List<SilaGoodsIssueCostCenterDto>))]
        public async Task<IActionResult> List([FromQuery] string? scope = null)
        {
            _logger.LogDebug($"Fetching Goods Issue cost centers. Scope: {scope}");
            List<SilaGoodsIssueCostCenterDto> result = await _mediator.Send(new GetSilaGoodsIssueCostCentersQuery
            {
                OrganizationId = GetOrganizationId(),
                Scope = scope
            });
            return Ok(result);
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/recipe-setup/goods-issue-cost-centers")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateSilaGoodsIssueCostCenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Create([FromBody] SilaGoodsIssueCostCenterWriteDto request)
        {
            _logger.LogDebug($"Creating Goods Issue cost center. PropertyId: {request?.PropertyId}, Scope: {request?.Scope}");
            Guid id = await _mediator.Send(new SaveSilaGoodsIssueCostCenterCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Request = request ?? new SilaGoodsIssueCostCenterWriteDto()
            });
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Goods Issue cost center saved."
            });
        }

        [HttpPut]
        [Route("api/v1/buyer/sila/recipe-setup/goods-issue-cost-centers/{id}")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpdateSilaGoodsIssueCostCenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] SilaGoodsIssueCostCenterWriteDto request)
        {
            _logger.LogDebug($"Updating Goods Issue cost center. Id: {id}");
            await _mediator.Send(new SaveSilaGoodsIssueCostCenterCommand
            {
                OrganizationId = GetOrganizationId(),
                UserId = GetUserId(),
                Id = id,
                Request = request ?? new SilaGoodsIssueCostCenterWriteDto()
            });
            return Ok(new SuccessResponseDto
            {
                Id = id.ToString(),
                StatusCode = 200,
                Message = "Success",
                Description = "Goods Issue cost center updated."
            });
        }

        [HttpDelete]
        [Route("api/v1/buyer/sila/recipe-setup/goods-issue-cost-centers/{id}")]
        [ApiAuthorization(Name = "MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DeleteSilaGoodsIssueCostCenter")]
        [SwaggerResponse(200, type: typeof(SuccessResponseDto))]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            _logger.LogDebug($"Deleting Goods Issue cost center. Id: {id}");
            await _mediator.Send(new DeleteSilaGoodsIssueCostCenterCommand
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
                Description = "Goods Issue cost center deleted."
            });
        }
    }
}
