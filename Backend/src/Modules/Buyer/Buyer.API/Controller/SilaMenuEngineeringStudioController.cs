using Buyer.Application.Features.Shared.MenuEngineering;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;

namespace Buyer.API.Controllers
{
    [ApiController]
    [Route("api/v1/buyer/sila/me-studio")]
    public class SilaMenuEngineeringStudioController : BaseController
    {
        private const string MeReadPerms =
            "VIEW_SILA_MENU_ENGINEERING|MANAGE_SILA_MENU_ENGINEERING|VIEW_SILA_ME_STUDIO|CREATE_SILA_MENU|EDIT_SILA_DRAFT_MENU|MANAGE_SILA_MENU_ITEMS|MANAGE_SILA_PORTION_PLANS|MANAGE_SILA_DEMAND_PLANS|VIEW_SILA_POS_SALES|MANAGE_SILA_POS_MAPPING|VIEW_SILA_MENU_PERFORMANCE|VIEW_SILA_MENU_COST|VIEW_SILA_MENU_MARGIN|GENERATE_SILA_MENU_PURCHASE_REC|USE_SILA_AI_MENU_INTELLIGENCE|RUN_SILA_MENU_SIMULATION|APPROVE_SILA_MENU|PUBLISH_SILA_MENU|MANAGE_SILA_MENU_SETUP";

        private readonly IMenuEngineeringStudioService _service;

        public SilaMenuEngineeringStudioController(IMenuEngineeringStudioService service)
        {
            _service = service;
        }

        [HttpGet("permissions")]
        [ApiAuthorization(TokenOnly = true)]
        public async Task<IActionResult> GetPermissions([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetPermissionsAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpGet("workspace")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetWorkspace([FromQuery] Guid? menuPlanId = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetWorkspaceAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuPlanId, propertyId, ct));

        [HttpGet("menu-items")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetMenuItems([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetMenuItemsAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpPost("menu-items")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SaveMenuItem([FromBody] MenuItemEnhancedWriteDto dto, CancellationToken ct = default)
            => Ok(await _service.SaveMenuItemAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpPost("menu-items/{menuItemId:guid}/copy")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CopyMenuItem(Guid menuItemId, CancellationToken ct = default)
            => Ok(await _service.CopyMenuItemAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuItemId, ct));

        [HttpPost("menu-items/bulk-price")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> BulkPriceUpdate([FromBody] MenuItemBulkPriceUpdateDto dto, CancellationToken ct = default)
            => Ok(await _service.BulkPriceUpdateAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpPost("menu-items/bulk-import")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> BulkImportMenuItems([FromBody] MenuItemBulkImportDto dto, CancellationToken ct = default)
            => Ok(await _service.BulkImportMenuItemsAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("menu-items/{menuItemId:guid}/versions")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetMenuItemVersions(Guid menuItemId, CancellationToken ct = default)
            => Ok(await _service.GetMenuItemVersionsAsync(GetOrganizationId(), GetRoleId(), menuItemId, ct));

        [HttpGet("menu-plans")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetMenuPlans([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetMenuPlansAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpPost("menu-plans")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SaveMenuPlan([FromBody] MenuPlanEnhancedWriteDto dto, CancellationToken ct = default)
            => Ok(await _service.SaveMenuPlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpPost("menu-plans/from-historical/{sourcePlanId:guid}")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CreateFromHistorical(Guid sourcePlanId, [FromQuery] string sourceKind = "MENU_PLAN", [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.CreateMenuFromHistoricalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), sourcePlanId, sourceKind, propertyId, ct));

        [HttpPost("menu-plans/from-ai")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CreateFromAi([FromQuery] string strategy = "BALANCED", [FromQuery] Guid? propertyId = null, [FromQuery] decimal? expectedCovers = 100m, CancellationToken ct = default)
            => Ok(await _service.CreateMenuFromAiStrategyAsync(GetOrganizationId(), GetUserId(), GetRoleId(), strategy, propertyId, expectedCovers, ct));

        [HttpPost("menu-plans/{menuPlanId:guid}/transition")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> TransitionPlan(Guid menuPlanId, [FromBody] MenuPlanTransitionDto dto, CancellationToken ct = default)
        {
            dto.MenuPlanId = menuPlanId;
            return Ok(await _service.TransitionMenuPlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));
        }

        [HttpGet("portions")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetPortions([FromQuery] Guid? menuPlanId = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetPortionPlansAsync(GetOrganizationId(), GetRoleId(), menuPlanId, propertyId, ct));

        [HttpPost("portions/sample")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RecordPortionSample([FromBody] PortionSampleWriteDto dto, CancellationToken ct = default)
            => Ok(await _service.RecordPortionSampleAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("demand")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetDemand([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetDemandBoardAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpPost("demand/override")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SaveDemandOverride([FromBody] DemandOverrideWriteDto dto, CancellationToken ct = default)
            => Ok(await _service.SaveDemandOverrideAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("pos-sales")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetPosSales([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetPosSalesExtendedAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpGet("pos-mapping")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetPosMapping([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetPosMappingWorkbenchAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpPost("pos-mapping")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SavePosMapping([FromBody] PosMappingExtendedWriteDto dto, CancellationToken ct = default)
            => Ok(await _service.SavePosMappingExtendedAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("performance-matrix")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetPerformanceMatrix([FromQuery] SilaMenuEngineeringFilterDto filter, [FromQuery] string? costBasis = null, [FromQuery] decimal? popularityThreshold = null, CancellationToken ct = default)
            => Ok(await _service.GetPerformanceMatrixAsync(GetOrganizationId(), GetRoleId(), filter, costBasis, popularityThreshold, ct));

        [HttpGet("consumption")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetConsumption([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetConsumptionVarianceAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpGet("cost-margin")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetCostMargin([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetCostMarginAnalysisAsync(GetOrganizationId(), GetRoleId(), filter, ct));

        [HttpGet("purchase-plan")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetPurchasePlan([FromQuery] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.GetPurchasePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpPost("purchase-plan/draft")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CreateDraftPurchaseRec([FromBody] SilaMenuEngineeringFilterDto filter, CancellationToken ct = default)
            => Ok(await _service.CreateDraftPurchaseRecommendationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), filter, ct));

        [HttpGet("ai-intelligence")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetAiIntelligence([FromQuery] Guid? menuPlanId = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetAiMenuIntelligenceAsync(GetOrganizationId(), GetRoleId(), menuPlanId, propertyId, ct));

        [HttpGet("historical-menus")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetHistoricalMenus([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetHistoricalMenusAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("simulate")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunSimulation([FromBody] WhatIfSimulationRequestDto dto, CancellationToken ct = default)
            => Ok(await _service.RunSimulationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("scenarios")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetScenarios([FromQuery] Guid? menuPlanId = null, CancellationToken ct = default)
            => Ok(await _service.GetScenariosAsync(GetOrganizationId(), GetRoleId(), menuPlanId, ct));

        [HttpPost("scenarios/compare")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CompareScenarios([FromBody] List<Guid> ids, CancellationToken ct = default)
            => Ok(await _service.CompareScenariosAsync(GetOrganizationId(), GetRoleId(), ids, ct));

        [HttpPost("scenarios/{scenarioId:guid}/preferred")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SetPreferredScenario(Guid scenarioId, CancellationToken ct = default)
            => Ok(await _service.SetPreferredScenarioAsync(GetOrganizationId(), GetRoleId(), scenarioId, ct));

        [HttpPost("scenarios/{scenarioId:guid}/convert-draft")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ConvertScenarioToDraft(Guid scenarioId, CancellationToken ct = default)
            => Ok(await _service.ConvertScenarioToDraftAsync(GetOrganizationId(), GetUserId(), GetRoleId(), scenarioId, ct));

        [HttpGet("setup")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetSetup([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetSetupAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("setup")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> SaveSetup([FromBody] MenuEngineeringSetupDto dto, CancellationToken ct = default)
            => Ok(await _service.SaveSetupAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("data-migration/templates")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public IActionResult GetMigrationTemplates() => Ok(_service.GetMigrationTemplates());

        [HttpPost("data-migration/preview")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> PreviewDataMigration([FromBody] CustomerDataImportPreviewRequestDto dto, CancellationToken ct = default)
            => Ok(await _service.PreviewCustomerDataMigrationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpPost("data-migration/commit")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CommitDataMigration([FromBody] CustomerDataImportPreviewRequestDto dto, CancellationToken ct = default)
            => Ok(await _service.CommitCustomerDataMigrationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), dto, ct));

        [HttpGet("data-migration/audits")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetDataMigrationAudits(CancellationToken ct = default)
            => Ok(await _service.GetCustomerDataMigrationAuditsAsync(GetOrganizationId(), GetRoleId(), ct));

        [HttpGet("reports")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public IActionResult GetReportCatalog() => Ok(_service.GetReportCatalog());

        [HttpGet("reports/{reportKey}/export")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ExportReport(string reportKey, [FromQuery] string format = "CSV", [FromQuery] SilaMenuEngineeringFilterDto? filter = null, CancellationToken ct = default)
            => Ok(await _service.ExportReportAsync(GetOrganizationId(), GetRoleId(), reportKey, format, filter ?? new(), ct));

        [HttpGet("mobile-summary")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetMobileSummary([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _service.GetMobileMenuSummaryAsync(GetOrganizationId(), GetUserId(), GetRoleId(), propertyId, ct));
    }
}
