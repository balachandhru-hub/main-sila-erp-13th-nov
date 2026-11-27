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
    /// Phase 4B Actual Consumption, Wastage, Returns, Production Settlement, Event Closure, ERP Reconciliation &amp; Historical Learning Foundation.
    /// Strictly tenant-aware, property-aware, and role-aware; does not disturb Procurement V1.
    /// </summary>
    [ApiController]
    public class SilaCulinaryReconciliationController : BaseController
    {
        private readonly ISilaCulinaryReconciliationService _service;

        public SilaCulinaryReconciliationController(ISilaCulinaryReconciliationService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        private bool HasPermission(string permissionCode) =>
            User.Claims.Any(c => (c.Type == "Permission" || c.Type == "Permissions" || c.Type == "role" || c.Type == ClaimTypes.Role)
                && c.Value.Contains(permissionCode, StringComparison.OrdinalIgnoreCase));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-reconciliation/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_INVENTORY|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("GetCulinaryReconciliationDashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.GetDashboardAsync(GetOrganizationId(), propertyId, HasPermission("APPROVE_SILA_RECIPE") || HasPermission("VIEW_SILA_COSTING"), cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/config")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertReconciliationConfig")]
        public async Task<IActionResult> UpsertConfig([FromBody] SilaCulinaryReconciliationConfigDto request, CancellationToken cancellationToken)
            => Ok(await _service.UpsertConfigAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/consumption")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("RecordActualConsumption")]
        public async Task<IActionResult> RecordConsumption([FromBody] SilaRecordConsumptionRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.RecordConsumptionAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/additional-material")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("CreateOrDecideAdditionalMaterialRequest")]
        public async Task<IActionResult> AdditionalMaterial([FromBody] SilaAdditionalMaterialRequestDto request, [FromQuery] bool isDecision = false, CancellationToken cancellationToken = default)
            => Ok(await _service.CreateOrDecideAdditionalRequestAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, isDecision, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/returns")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("RecordOrInspectMaterialReturn")]
        public async Task<IActionResult> MaterialReturn([FromBody] SilaMaterialReturnRequestDto request, [FromQuery] Guid? inspectReturnId = null, [FromQuery] bool? inspectionPassed = null, CancellationToken cancellationToken = default)
            => Ok(await _service.RecordOrInspectReturnAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, inspectReturnId, inspectionPassed, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/issue-reversals")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_INVENTORY|APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("ReverseMaterialIssue")]
        public async Task<IActionResult> ReverseIssue([FromBody] SilaMaterialIssueReversalRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.ReverseMaterialIssueAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/waste")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("RecordOrApproveWasteEvent")]
        public async Task<IActionResult> RecordWaste([FromBody] SilaRecordWasteEventRequestDto request, [FromQuery] bool? approveDecision = null, [FromQuery] Guid? wasteId = null, CancellationToken cancellationToken = default)
            => Ok(await _service.RecordWasteEventAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, approveDecision, wasteId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/leftovers")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("RecordOrUpdateLeftover")]
        public async Task<IActionResult> RecordLeftover([FromBody] SilaFoodLeftoverRequestDto request, [FromQuery] Guid? leftoverId = null, CancellationToken cancellationToken = default)
            => Ok(await _service.RecordOrDisposeLeftoverAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, leftoverId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/disposals")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("ConfirmWasteDisposal")]
        public async Task<IActionResult> ConfirmDisposal([FromBody] SilaWasteDisposalRequestDto request, [FromQuery] bool approveSupervisor = false, [FromQuery] Guid? disposalId = null, CancellationToken cancellationToken = default)
            => Ok(await _service.ConfirmDisposalAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, approveSupervisor, disposalId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/output-confirmations")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("ConfirmProductionOutput")]
        public async Task<IActionResult> ConfirmOutput([FromBody] SilaOutputConfirmationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.ConfirmProductionOutputAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/portion-samples")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_RECIPE")]
        [SwaggerOperation("RecordPortionSample")]
        public async Task<IActionResult> RecordPortionSample([FromBody] SilaPortionSampleRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.RecordPortionSampleAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/orders/{productionOrderId:guid}/reconcile")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("ReconcileProductionOrder")]
        public async Task<IActionResult> ReconcileOrder(Guid productionOrderId, [FromQuery] Guid? batchId, CancellationToken cancellationToken)
            => Ok(await _service.ReconcileProductionOrderAsync(GetOrganizationId(), GetUserId(), GetActorName(), productionOrderId, batchId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/reconciliations/{reconciliationId:guid}/settle")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("SettleProductionReconciliation")]
        public async Task<IActionResult> SettleProduction(Guid reconciliationId, [FromQuery] bool financialSettlement = false, CancellationToken cancellationToken = default)
            => Ok(await _service.SettleProductionAsync(GetOrganizationId(), GetUserId(), GetActorName(), reconciliationId, financialSettlement, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/labor")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("RecordLaborAllocation")]
        public async Task<IActionResult> RecordLabor([FromBody] SilaLaborAllocationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.RecordLaborAllocationAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/central-kitchen-allocate")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("AllocateCentralKitchenCost")]
        public async Task<IActionResult> AllocateCentralKitchen([FromBody] SilaCentralKitchenCostAllocationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.AllocateCentralKitchenCostAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/buffet-reconcile")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_RECIPE")]
        [SwaggerOperation("ReconcileBuffet")]
        public async Task<IActionResult> ReconcileBuffet([FromBody] SilaBuffetReconciliationRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.ReconcileBuffetAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/event-closures")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("UpsertEventClosure")]
        public async Task<IActionResult> UpsertEventClosure([FromBody] SilaEventClosureRequestDto request, [FromQuery] bool closeOperational = false, [FromQuery] bool closeFinancial = false, CancellationToken cancellationToken = default)
            => Ok(await _service.UpsertEventClosureAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, closeOperational, closeFinancial, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/event-closures/{closureId:guid}/reopen")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE")]
        [SwaggerOperation("ReopenEventClosure")]
        public async Task<IActionResult> ReopenEventClosure(Guid closureId, [FromBody] SilaReopenClosureRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.ReopenEventClosureAsync(GetOrganizationId(), GetUserId(), GetActorName(), closureId, request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/erp-reconciliations")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("UpsertErpReconciliationRef")]
        public async Task<IActionResult> UpsertErpRef([FromQuery] string silaTxRef, [FromQuery] string txType, [FromQuery] string erpSystem, [FromQuery] string? erpDocRef, [FromQuery] string postingStatus, [FromQuery] string? errorMsg, [FromQuery] string idempotencyKey, CancellationToken cancellationToken)
            => Ok(await _service.UpsertErpReconciliationAsync(GetOrganizationId(), GetUserId(), GetActorName(), silaTxRef, txType, erpSystem, erpDocRef, postingStatus, errorMsg, idempotencyKey, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/event-closures/{closureId:guid}/learning-snapshot")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_RECIPE")]
        [SwaggerOperation("CaptureHistoricalLearningSnapshot")]
        public async Task<IActionResult> CaptureLearningSnapshot(Guid closureId, CancellationToken cancellationToken)
            => Ok(await _service.CaptureHistoricalLearningSnapshotAsync(GetOrganizationId(), GetUserId(), GetActorName(), closureId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-reconciliation/copilot")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("AskCostControlCopilot")]
        public async Task<IActionResult> AskCopilot([FromBody] SilaReconciliationCopilotRequestDto request, CancellationToken cancellationToken)
        {
            if (request.Question.Contains("profit", StringComparison.OrdinalIgnoreCase) && !HasPermission("APPROVE_SILA_RECIPE") && !HasPermission("VIEW_SILA_COSTING"))
            {
                request.CanViewFinancialCost = false;
            }
            return Ok(await _service.AskCostControlCopilotAsync(GetOrganizationId(), request, cancellationToken));
        }

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-reconciliation/reports")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("BuildPhase4B27ReportsPayload")]
        public async Task<IActionResult> Get27Reports([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.Build27ReportsPayloadAsync(GetOrganizationId(), propertyId, HasPermission("APPROVE_SILA_RECIPE") || HasPermission("VIEW_SILA_COSTING"), cancellationToken));
    }
}
