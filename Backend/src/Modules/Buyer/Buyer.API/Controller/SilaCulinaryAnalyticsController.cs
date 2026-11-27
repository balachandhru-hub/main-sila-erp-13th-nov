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
    /// Phase 3B Culinary Intelligence — Advanced Culinary Analytics, Menu Engineering,
    /// Isolated What-If Simulation, Mathematical Optimization, Waste Intelligence, Benchmarking,
    /// Recommendation Cockpit, and Grounded Culinary Copilot.
    /// </summary>
    [ApiController]
    public class SilaCulinaryAnalyticsController : BaseController
    {
        private readonly ISilaCulinaryAnalyticsService _service;

        public SilaCulinaryAnalyticsController(ISilaCulinaryAnalyticsService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/config")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryAnalyticsConfig")]
        public async Task<IActionResult> GetConfig([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.GetConfigAsync(GetOrganizationId(), GetRoleId(), propertyId, cancellationToken));

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-analytics/config")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SaveCulinaryAnalyticsConfig")]
        public async Task<IActionResult> SaveConfig([FromBody] SilaCulinarySaveAnalyticsConfigRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.SaveConfigAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryExecutiveDashboard")]
        public async Task<IActionResult> GetExecutiveDashboard([FromBody] SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken)
            => Ok(await _service.GetExecutiveDashboardAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/menu-engineering")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeCulinaryMenuEngineering")]
        public async Task<IActionResult> AnalyzeMenuEngineering([FromBody] SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeMenuEngineeringAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/consumption-variance")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("AnalyzeConsumptionVariance")]
        public async Task<IActionResult> AnalyzeConsumptionVariance([FromBody] SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeConsumptionVarianceAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/consumption-variance/task")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateVarianceInvestigationTask")]
        public async Task<IActionResult> CreateVarianceTask([FromBody] SilaCulinaryCreateVarianceTaskRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateVarianceInvestigationTaskAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/price-impact")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeIngredientPriceImpact")]
        public async Task<IActionResult> AnalyzePriceImpact([FromBody] SilaCulinaryReversePriceImpactRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzePriceImpactAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/simulations")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinarySimulations")]
        public async Task<IActionResult> ListSimulations([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.ListSimulationsAsync(GetOrganizationId(), GetRoleId(), propertyId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/simulations")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RunAndSaveCulinarySimulation")]
        public async Task<IActionResult> RunSimulation([FromBody] SilaCulinaryRunSimulationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.RunAndSaveSimulationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/simulations/{simulationId:guid}/duplicate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DuplicateCulinarySimulation")]
        public async Task<IActionResult> DuplicateSimulation([FromRoute] Guid simulationId, CancellationToken cancellationToken)
            => Ok(await _service.DuplicateSimulationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), simulationId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/optimize-menu")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("OptimizeMenu")]
        public async Task<IActionResult> OptimizeMenu([FromBody] SilaCulinaryOptimizationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.OptimizeMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/optimization-jobs")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListOptimizationJobs")]
        public async Task<IActionResult> ListOptimizationJobs(CancellationToken cancellationToken)
            => Ok(await _service.ListOptimizationJobsAsync(GetOrganizationId(), GetRoleId(), cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/optimization-jobs/{jobId:guid}/cancel")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CancelOptimizationJob")]
        public async Task<IActionResult> CancelOptimizationJob([FromRoute] Guid jobId, CancellationToken cancellationToken)
            => Ok(await _service.CancelOptimizationJobAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), jobId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/substitutions")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeIngredientSubstitutions")]
        public async Task<IActionResult> AnalyzeSubstitutions([FromBody] SilaCulinarySubstitutionRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeSubstitutionsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/buffet-optimize")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("OptimizeBuffetProduction")]
        public async Task<IActionResult> OptimizeBuffet([FromBody] SilaCulinaryBuffetOptimizeRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.OptimizeBuffetAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/event-optimize")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("OptimizeEventMenus")]
        public async Task<IActionResult> OptimizeEventMenus([FromBody] SilaCulinaryEventOptimizeRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.OptimizeEventMenusAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/forecast")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GenerateDemandForecast")]
        public async Task<IActionResult> GenerateForecast([FromBody] SilaCulinaryForecastRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.GenerateForecastAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/waste")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("GetWasteIntelligence")]
        public async Task<IActionResult> GetWasteIntelligence([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.GetWasteIntelligenceAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/waste")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("RecordCulinaryWaste")]
        public async Task<IActionResult> RecordWaste([FromBody] SilaCulinaryWasteRecordCreateDto request, CancellationToken cancellationToken)
            => Ok(await _service.RecordWasteAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/benchmark")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("BenchmarkProperties")]
        public async Task<IActionResult> BenchmarkProperties([FromQuery] string? reportingCurrency, CancellationToken cancellationToken)
            => Ok(await _service.BenchmarkPropertiesAsync(GetOrganizationId(), GetUserId(), GetRoleId(), reportingCurrency, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/recommendations")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryRecommendations")]
        public async Task<IActionResult> ListRecommendations([FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.ListRecommendationsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), status, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/recommendations/generate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GenerateCulinaryRecommendations")]
        public async Task<IActionResult> GenerateRecommendations([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.GenerateRecommendationsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), propertyId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/recommendations/{recommendationId:guid}/decide")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DecideCulinaryRecommendation")]
        public async Task<IActionResult> DecideRecommendation([FromRoute] Guid recommendationId, [FromBody] SilaCulinaryRecommendationDecisionDto decision, CancellationToken cancellationToken)
            => Ok(await _service.DecideRecommendationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), recommendationId, decision, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/copilot")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("QueryCulinaryCopilot")]
        public async Task<IActionResult> QueryCopilot([FromBody] SilaCulinaryCopilotRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.QueryCopilotAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/reports/catalog")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryAnalyticsReportCatalog")]
        public IActionResult GetReportCatalog()
            => Ok(_service.GetReportCatalog());

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-analytics/reports/{reportCode}/export")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportCulinaryAnalyticsReport")]
        public async Task<IActionResult> ExportReport([FromRoute] string reportCode, [FromQuery] string format = "CSV", [FromBody] SilaCulinaryExecutiveFilterDto? filter = null, CancellationToken cancellationToken = default)
            => Ok(await _service.ExportReportAsync(GetOrganizationId(), GetUserId(), GetRoleId(), reportCode, format, filter, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-analytics/mobile-summary")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("GetMobileCulinaryOpsSummary")]
        public async Task<IActionResult> GetMobileSummary([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.GetMobileSummaryAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, cancellationToken));
    }
}
