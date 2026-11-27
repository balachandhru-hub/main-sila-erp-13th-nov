using Buyer.Application.Features.Shared.MenuEngineering.Twin;
using Buyer.Domain.Dtos;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Attributes;
using SharedKernel.Controllers;

namespace Buyer.API.Controllers
{
    [ApiController]
    [Route("api/v1/buyer/sila/me-studio/twin")]
    public class SilaMenuEngineeringTwinController : BaseController
    {
        private const string MeReadPerms =
            "VIEW_SILA_MENU_ENGINEERING|MANAGE_SILA_MENU_ENGINEERING|VIEW_SILA_ME_STUDIO|CREATE_SILA_MENU|EDIT_SILA_DRAFT_MENU|MANAGE_SILA_MENU_ITEMS|MANAGE_SILA_PORTION_PLANS|MANAGE_SILA_DEMAND_PLANS|VIEW_SILA_POS_SALES|MANAGE_SILA_POS_MAPPING|VIEW_SILA_MENU_PERFORMANCE|VIEW_SILA_MENU_COST|VIEW_SILA_MENU_MARGIN|GENERATE_SILA_MENU_PURCHASE_REC|USE_SILA_AI_MENU_INTELLIGENCE|RUN_SILA_MENU_SIMULATION|APPROVE_SILA_MENU|PUBLISH_SILA_MENU|MANAGE_SILA_MENU_SETUP";

        private readonly IMenuEngineeringDigitalTwinService _twin;

        public SilaMenuEngineeringTwinController(IMenuEngineeringDigitalTwinService twin)
        {
            _twin = twin;
        }

        [HttpPost("snapshots")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CreateSnapshot([FromBody] CreateTwinSnapshotRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.CreateSnapshotAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("snapshots")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetSnapshots([FromQuery] Guid? menuPlanId = null, CancellationToken ct = default)
            => Ok(await _twin.GetSnapshotsAsync(GetOrganizationId(), GetRoleId(), menuPlanId, ct));

        [HttpGet("snapshots/{snapshotId:guid}")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetSnapshot(Guid snapshotId, CancellationToken ct = default)
            => Ok(await _twin.GetSnapshotAsync(GetOrganizationId(), GetRoleId(), snapshotId, ct));

        [HttpPost("snapshots/{snapshotId:guid}/refresh")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RefreshSnapshot(Guid snapshotId, CancellationToken ct = default)
            => Ok(await _twin.RefreshSnapshotAsync(GetOrganizationId(), GetUserId(), GetRoleId(), snapshotId, ct));

        [HttpGet("snapshots/compare")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CompareSnapshots([FromQuery] Guid leftSnapshotId, [FromQuery] Guid rightSnapshotId, CancellationToken ct = default)
            => Ok(await _twin.CompareSnapshotsAsync(GetOrganizationId(), GetRoleId(), leftSnapshotId, rightSnapshotId, ct));

        [HttpPost("dependency-graph")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> AnalyzeDependencyGraph([FromBody] TwinDependencyAnalysisRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.AnalyzeDependencyGraphAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpPost("optimize")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunOptimization([FromBody] MePhase3OptimizeRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.RunMultiObjectiveOptimizationAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("optimize/{jobId:guid}")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetOptimizationJob(Guid jobId, CancellationToken ct = default)
            => Ok(await _twin.GetOptimizationJobAsync(GetOrganizationId(), GetRoleId(), jobId, ct));

        [HttpPost("optimize/{jobId:guid}/cancel")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CancelOptimizationJob(Guid jobId, CancellationToken ct = default)
            => Ok(await _twin.CancelOptimizationJobAsync(GetOrganizationId(), GetUserId(), GetRoleId(), jobId, ct));

        [HttpPost("optimize-inventory-menu")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> OptimizeInventoryMenu([FromBody] MePhase3InventoryMenuRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.OptimizeInventoryDrivenMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("production-feasibility")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> EvaluateProductionFeasibility([FromQuery] Guid? menuPlanId = null, [FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _twin.EvaluateProductionFeasibilityAsync(GetOrganizationId(), GetRoleId(), menuPlanId, propertyId, ct));

        [HttpGet("pricing/optimize")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> OptimizePricing([FromQuery] Guid? propertyId = null, [FromQuery] Guid? menuPlanId = null, CancellationToken ct = default)
            => Ok(await _twin.OptimizePricingPhase3Async(GetOrganizationId(), GetRoleId(), propertyId, menuPlanId, ct));

        [HttpPost("monte-carlo")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunMonteCarlo([FromBody] MeMonteCarloRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.RunMonteCarloAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpPost("sensitivity")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunSensitivity([FromBody] MeSensitivityRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.RunSensitivityAnalysisAsync(GetOrganizationId(), GetRoleId(), request, ct));

        [HttpPost("stress-test")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> RunStressTest([FromBody] MeStressTestRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.RunStressTestAsync(GetOrganizationId(), GetRoleId(), request, ct));

        [HttpGet("forecast/hierarchical")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ForecastHierarchical([FromQuery] Guid? propertyId = null, [FromQuery] int horizonDays = 7, CancellationToken ct = default)
            => Ok(await _twin.ForecastHierarchicalDemandAsync(GetOrganizationId(), GetRoleId(), propertyId, horizonDays, ct));

        [HttpPost("forecast/cold-start")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> PredictColdStart([FromBody] MeColdStartPredictRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.PredictColdStartItemAsync(GetOrganizationId(), GetRoleId(), request, ct));

        [HttpGet("predict/waste")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> PredictWaste([FromQuery] Guid? propertyId = null, [FromQuery] Guid? menuPlanId = null, CancellationToken ct = default)
            => Ok(await _twin.PredictFoodWasteAsync(GetOrganizationId(), GetRoleId(), propertyId, menuPlanId, ct));

        [HttpGet("predict/prices")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> PredictPrices([FromQuery] Guid? propertyId = null, CancellationToken ct = default)
            => Ok(await _twin.PredictIngredientPricesAsync(GetOrganizationId(), GetRoleId(), propertyId, ct));

        [HttpPost("substitution-cascade")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> EvaluateSubstitutionCascade([FromBody] MeSubstitutionCascadeRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.EvaluateSubstitutionCascadeAsync(GetOrganizationId(), GetRoleId(), request, ct));

        [HttpPost("scenarios")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CreateScenario([FromBody] MeScenarioCreateRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.CreateScenarioAsync(GetOrganizationId(), GetUserId(), GetRoleId(), request, ct));

        [HttpGet("scenarios")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetAndCompareScenarios([FromQuery] Guid? menuPlanId = null, CancellationToken ct = default)
            => Ok(await _twin.GetAndCompareScenariosAsync(GetOrganizationId(), GetRoleId(), menuPlanId, ct));

        [HttpPost("scenarios/{scenarioId:guid}/copy")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CopyScenario(Guid scenarioId, [FromQuery] string? newName = null, CancellationToken ct = default)
            => Ok(await _twin.CopyScenarioAsync(GetOrganizationId(), GetUserId(), GetRoleId(), scenarioId, newName, ct));

        [HttpPost("scenarios/{scenarioId:guid}/archive")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ArchiveScenario(Guid scenarioId, CancellationToken ct = default)
            => Ok(await _twin.ArchiveScenarioAsync(GetOrganizationId(), GetUserId(), GetRoleId(), scenarioId, ct));

        [HttpPost("scenarios/{scenarioId:guid}/convert-to-draft")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> ConvertScenarioToDraft(Guid scenarioId, CancellationToken ct = default)
            => Ok(new { menuPlanId = await _twin.ConvertScenarioToDraftMenuAsync(GetOrganizationId(), GetUserId(), GetRoleId(), scenarioId, ct) });

        [HttpGet("historical-similarity")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetHistoricalSimilarity([FromQuery] Guid? menuPlanId = null, CancellationToken ct = default)
            => Ok(await _twin.GetHistoricalSimilarityAsync(GetOrganizationId(), GetRoleId(), menuPlanId, ct));

        [HttpPost("learning/capture-outcome/{menuPlanId:guid}")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> CaptureOutcome(Guid menuPlanId, CancellationToken ct = default)
            => Ok(await _twin.CaptureCompletedEventOutcomeAsync(GetOrganizationId(), GetUserId(), GetRoleId(), menuPlanId, ct));

        [HttpGet("learning/governance")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> GetGovernanceStatus(CancellationToken ct = default)
            => Ok(await _twin.GetModelGovernanceStatusAsync(GetOrganizationId(), GetRoleId(), ct));

        [HttpPost("learning/drift/{driftEventId:guid}/transition")]
        [ApiAuthorization(AnyOf = MeReadPerms)]
        public async Task<IActionResult> TransitionDrift(Guid driftEventId, [FromBody] MeDriftActionRequestDto request, CancellationToken ct = default)
            => Ok(await _twin.TransitionDriftGovernanceAsync(GetOrganizationId(), GetUserId(), GetRoleId(), driftEventId, request, ct));
    }
}
