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
    /// Phase 3A Culinary Intelligence — Advanced Food Cost Management, Event P&amp;L and Financial Intelligence.
    /// Zero live SAP postings, zero automatic GL/invoice documents, zero inventory valuation overwrites.
    /// </summary>
    [ApiController]
    public class SilaCulinaryFinancialController : BaseController
    {
        private readonly ISilaCulinaryFinancialService _service;

        public SilaCulinaryFinancialController(ISilaCulinaryFinancialService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/config")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryFinancialConfig")]
        public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
            => Ok(await _service.GetConfigAsync(GetOrganizationId(), cancellationToken));

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-financial/config")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SaveCulinaryFinancialConfig")]
        public async Task<IActionResult> SaveConfig([FromBody] SilaCulinarySaveFinancialConfigRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.SaveConfigAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/recipes/{recipeId:guid}/cost")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CalculateCulinaryRecipeFinancialCost")]
        public async Task<IActionResult> CalculateRecipeCost([FromRoute] Guid recipeId, [FromBody] SilaCulinaryFinancialRecipeCostRequestDto? request, CancellationToken cancellationToken)
            => Ok(await _service.CalculateRecipeCostAsync(GetOrganizationId(), GetUserId(), GetRoleId(), recipeId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/menus/{menuId:guid}/cost")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CalculateCulinaryMenuCost")]
        public async Task<IActionResult> CalculateMenuCost([FromRoute] Guid menuId, [FromQuery] int? plannedGuests, CancellationToken cancellationToken)
            => Ok(await _service.CalculateMenuCostAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuId, plannedGuests, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/materials/price-impact")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeMaterialPriceImpact")]
        public async Task<IActionResult> AnalyzePriceImpact([FromBody] SilaCulinaryMaterialPriceImpactRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeMaterialPriceImpactAsync(GetOrganizationId(), GetRoleId(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryFinancialDashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.GetDashboardAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, status, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/plans")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryEventFinancialPlans")]
        public async Task<IActionResult> ListPlans([FromQuery] Guid? propertyId, [FromQuery] string? status, [FromQuery] bool? includeSimulations, CancellationToken cancellationToken)
            => Ok(await _service.ListPlansAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, status, includeSimulations, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryEventFinancialPlan")]
        public async Task<IActionResult> GetPlan([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetPlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryEventFinancialPlan")]
        public async Task<IActionResult> CreatePlan([FromBody] SilaCulinaryCreateEventFinancialPlanRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateAndCalculatePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/recalculate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RecalculateCulinaryEventFinancialPlan")]
        public async Task<IActionResult> RecalculatePlan([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.RecalculatePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/revenue-lines")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AddCulinaryEventRevenueLine")]
        public async Task<IActionResult> AddRevenueLine([FromRoute] Guid planId, [FromBody] SilaCulinaryAddRevenueLineRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AddRevenueLineAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/expense-lines")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AddCulinaryEventExpenseLine")]
        public async Task<IActionResult> AddExpenseLine([FromRoute] Guid planId, [FromBody] SilaCulinaryAddExpenseLineRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AddExpenseLineAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/expense-lines/{expenseId:guid}/approve")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ApproveCulinaryEventExpenseLine")]
        public async Task<IActionResult> ApproveExpenseLine([FromRoute] Guid planId, [FromRoute] Guid expenseId, CancellationToken cancellationToken)
            => Ok(await _service.ApproveExpenseLineAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, expenseId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/simulate")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SimulateCulinaryEventFinancials")]
        public async Task<IActionResult> Simulate([FromRoute] Guid planId, [FromBody] SilaCulinarySimulateFinancialRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.SimulateAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/scenarios/{scenarioId:guid}/promote")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("PromoteFinancialScenarioToRevision")]
        public async Task<IActionResult> PromoteScenario([FromRoute] Guid planId, [FromRoute] Guid scenarioId, CancellationToken cancellationToken)
            => Ok(await _service.PromoteScenarioToRevisionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, scenarioId, cancellationToken));

        public class ResolveExceptionBody
        {
            public string ResolutionStatus { get; set; } = "RESOLVED";
            public string? Note { get; set; }
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/exceptions/{exceptionId:guid}/resolve")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ResolveCulinaryFinancialException")]
        public async Task<IActionResult> ResolveException([FromRoute] Guid planId, [FromRoute] Guid exceptionId, [FromBody] ResolveExceptionBody body, CancellationToken cancellationToken)
            => Ok(await _service.ResolveExceptionAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, exceptionId, body.ResolutionStatus, body.Note, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/submit")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SubmitCulinaryFinancialPlan")]
        public async Task<IActionResult> Submit([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.SubmitForApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        public class ApprovalDecisionBody
        {
            public string Decision { get; set; } = "APPROVE";
            public string? Comment { get; set; }
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/decide")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DecideCulinaryFinancialPlan")]
        public async Task<IActionResult> Decide([FromRoute] Guid planId, [FromBody] ApprovalDecisionBody body, CancellationToken cancellationToken)
            => Ok(await _service.DecideApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.Decision, body.Comment, cancellationToken));

        public class ChangeImpactBody
        {
            public int? UpdatedGuests { get; set; }
            public decimal? UpdatedPricePerGuest { get; set; }
        }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/change-impact")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("HandleCulinaryFinancialChangeImpact")]
        public async Task<IActionResult> ChangeImpact([FromRoute] Guid planId, [FromBody] ChangeImpactBody body, CancellationToken cancellationToken)
            => Ok(await _service.HandleEventChangeImpactAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.UpdatedGuests, body.UpdatedPricePerGuest, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/cancellation-exposure")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryEventCancellationExposure")]
        public async Task<IActionResult> CancellationExposure([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetCancellationExposureAsync(GetOrganizationId(), GetRoleId(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/analyze")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AnalyzeCulinaryFinancialPlanWithAi")]
        public async Task<IActionResult> AnalyzeWithAi([FromRoute] Guid planId, [FromBody] SilaCulinaryFinancialAiRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AnalyzeWithAiAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/reports/catalog")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryFinancialReportCatalog")]
        public IActionResult GetReportCatalog() => Ok(_service.GetReportCatalog());

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/reports/{reportCode}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExportCulinaryFinancialReport")]
        public async Task<IActionResult> ExportReport([FromRoute] Guid planId, [FromRoute] string reportCode, CancellationToken cancellationToken)
            => Ok(await _service.ExportReportAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, reportCode, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/mobile-summary")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryMobileFinancialSummary")]
        public async Task<IActionResult> GetMobileSummary([FromQuery] Guid? planId, CancellationToken cancellationToken)
            => Ok(await _service.GetMobileSummaryAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/handover/phase3b")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryPhase3BHandover")]
        public async Task<IActionResult> GetPhase3BHandover([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetPhase3BHandoverAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-financial/plans/{planId:guid}/handover/phase4b")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryPhase4BHandover")]
        public async Task<IActionResult> GetPhase4BHandover([FromRoute] Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetPhase4BHandoverAsync(GetOrganizationId(), GetUserId(), GetRoleId(), planId, cancellationToken));
    }
}
