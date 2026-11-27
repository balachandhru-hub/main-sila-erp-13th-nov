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
    /// Phase 4A Kitchen Production Planning, Batch Scheduling &amp; Execution Management.
    /// Exposes tenant-aware, property-aware, and role-aware endpoints without disturbing Procurement V1 or Phases 0-3B.
    /// </summary>
    [ApiController]
    public class SilaCulinaryProductionController : BaseController
    {
        private readonly ISilaCulinaryProductionService _service;

        public SilaCulinaryProductionController(ISilaCulinaryProductionService service)
        {
            _service = service;
        }

        private string GetActorName() =>
            User.FindFirst(ClaimTypes.Email)?.Value
            ?? User.FindFirst("Email")?.Value
            ?? User.Identity?.Name
            ?? $"User-{GetUserId().ToString()[..8]}";

        // ---------------- Kitchen, Workstation, Equipment, Staff ----------------
        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/kitchens")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryKitchens")]
        public async Task<IActionResult> ListKitchens([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.ListKitchensAsync(GetOrganizationId(), propertyId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/kitchens")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertCulinaryKitchen")]
        public async Task<IActionResult> UpsertKitchen([FromBody] SilaCulinaryUpsertKitchenRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.UpsertKitchenAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/workstations")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryWorkstations")]
        public async Task<IActionResult> ListWorkstations([FromQuery] Guid? kitchenId, CancellationToken cancellationToken)
            => Ok(await _service.ListWorkstationsAsync(GetOrganizationId(), kitchenId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/workstations")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertCulinaryWorkstation")]
        public async Task<IActionResult> UpsertWorkstation([FromBody] SilaCulinaryWorkstationDto request, CancellationToken cancellationToken)
            => Ok(await _service.UpsertWorkstationAsync(GetOrganizationId(), GetUserId(), request.KitchenId, request.WorkstationCode, request.WorkstationName, request.WorkstationType, request.ConcurrentCapacity, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/equipment")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryEquipment")]
        public async Task<IActionResult> ListEquipment([FromQuery] Guid? kitchenId, CancellationToken cancellationToken)
            => Ok(await _service.ListEquipmentAsync(GetOrganizationId(), kitchenId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/equipment")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertCulinaryEquipment")]
        public async Task<IActionResult> UpsertEquipment([FromBody] SilaCulinaryEquipmentDto request, CancellationToken cancellationToken)
            => Ok(await _service.UpsertEquipmentAsync(GetOrganizationId(), GetUserId(), request, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/staff")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryStaff")]
        public async Task<IActionResult> ListStaff([FromQuery] Guid? kitchenId, CancellationToken cancellationToken)
            => Ok(await _service.ListStaffAssignmentsAsync(GetOrganizationId(), kitchenId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/staff")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("UpsertCulinaryStaff")]
        public async Task<IActionResult> UpsertStaff([FromBody] SilaCulinaryStaffAssignmentDto request, CancellationToken cancellationToken)
            => Ok(await _service.UpsertStaffAssignmentAsync(GetOrganizationId(), GetUserId(), request, cancellationToken));

        // ---------------- Demand & Production Plans ----------------
        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/demands")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryProductionDemands")]
        public async Task<IActionResult> ListDemands([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
            => Ok(await _service.ListDemandsAsync(GetOrganizationId(), propertyId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/demands")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryProductionDemand")]
        public async Task<IActionResult> CreateDemand([FromBody] SilaCulinaryProductionDemandDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreateDemandAsync(GetOrganizationId(), GetUserId(), request, cancellationToken));

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-production/demands/{demandId:guid}/adjust")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AdjustCulinaryProductionDemand")]
        public async Task<IActionResult> AdjustDemand(Guid demandId, [FromQuery] decimal portions, CancellationToken cancellationToken)
            => Ok(await _service.AdjustDemandAsync(GetOrganizationId(), GetUserId(), demandId, portions, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/plans")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ListCulinaryProductionPlans")]
        public async Task<IActionResult> ListPlans([FromQuery] Guid? propertyId, [FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.ListPlansAsync(GetOrganizationId(), GetRoleId(), propertyId, status, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/plans/{planId:guid}")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("GetCulinaryProductionPlan")]
        public async Task<IActionResult> GetPlan(Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.GetPlanAsync(GetOrganizationId(), GetRoleId(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/plans")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("CreateCulinaryProductionPlan")]
        public async Task<IActionResult> CreatePlan([FromBody] SilaCulinaryCreateProductionPlanRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.CreatePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/plans/{planId:guid}/explode-validate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ExplodeAndValidateProductionPlan")]
        public async Task<IActionResult> ExplodeAndValidate(Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.ExplodeAndValidatePlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/plans/{planId:guid}/submit-approval")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("SubmitProductionPlanApproval")]
        public async Task<IActionResult> SubmitPlanApproval(Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.SubmitPlanForApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        public class ProductionDecisionBody { public string Decision { get; set; } = "APPROVE"; public string? Comment { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/plans/{planId:guid}/decide-approval")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("DecideProductionPlanApproval")]
        public async Task<IActionResult> DecidePlanApproval(Guid planId, [FromBody] ProductionDecisionBody body, CancellationToken cancellationToken)
            => Ok(await _service.DecidePlanApprovalAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.Decision, body.Comment, cancellationToken));

        public class ProductionAmendBody { public string ChangeSummary { get; set; } = string.Empty; public List<SilaCulinaryCreatePlanLineInputDto> UpdatedLines { get; set; } = new(); }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/plans/{planId:guid}/amend")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("AmendReleasedProductionPlan")]
        public async Task<IActionResult> AmendPlan(Guid planId, [FromBody] ProductionAmendBody body, CancellationToken cancellationToken)
            => Ok(await _service.AmendReleasedPlanAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, body.ChangeSummary, body.UpdatedLines, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/plans/{planId:guid}/release")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ReleaseProductionPlanToOrders")]
        public async Task<IActionResult> ReleasePlan(Guid planId, CancellationToken cancellationToken)
            => Ok(await _service.ReleasePlanToOrdersAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), planId, cancellationToken));

        // ---------------- Orders, Batches, Scheduler & Calendar ----------------
        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/orders")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryProductionOrders")]
        public async Task<IActionResult> ListOrders([FromQuery] Guid? kitchenId, [FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.ListOrdersAsync(GetOrganizationId(), GetRoleId(), kitchenId, status, cancellationToken));

        public class RescheduleBatchBody { public DateTime PlannedStartUtc { get; set; } public DateTime PlannedFinishUtc { get; set; } public Guid? EquipmentId { get; set; } public Guid? WorkstationId { get; set; } }

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-production/batches/{batchId:guid}/reschedule")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("RescheduleCulinaryBatch")]
        public async Task<IActionResult> RescheduleBatch(Guid batchId, [FromBody] RescheduleBatchBody body, CancellationToken cancellationToken)
            => Ok(await _service.RescheduleBatchAsync(GetOrganizationId(), GetUserId(), GetActorName(), batchId, body.PlannedStartUtc, body.PlannedFinishUtc, body.EquipmentId, body.WorkstationId, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/schedule/optimize")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("OptimizeCulinaryProductionSchedule")]
        public async Task<IActionResult> OptimizeSchedule([FromBody] SilaCulinaryScheduleOptimizeRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.OptimizeScheduleAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/schedule/apply")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ApplyCulinaryProductionSchedule")]
        public async Task<IActionResult> ApplySchedule([FromBody] List<SilaCulinaryScheduledBatchDto> assignments, CancellationToken cancellationToken)
            => Ok(await _service.ApplyScheduleAsync(GetOrganizationId(), GetUserId(), GetActorName(), assignments, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/calendar")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("GetCulinaryProductionCalendar")]
        public async Task<IActionResult> GetCalendar([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, [FromQuery] Guid? kitchenId, CancellationToken cancellationToken)
            => Ok(await _service.GetProductionCalendarAsync(GetOrganizationId(), fromUtc, toUtc, kitchenId, cancellationToken));

        // ---------------- Picking & Material Issue ----------------
        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/orders/{orderId:guid}/picking-list")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("GenerateProductionPickingList")]
        public async Task<IActionResult> GeneratePickingList(Guid orderId, [FromQuery] string strategy = "FEFO", CancellationToken cancellationToken = default)
            => Ok(await _service.GeneratePickingListAsync(GetOrganizationId(), GetUserId(), GetActorName(), orderId, strategy, cancellationToken));

        public class ConfirmPickingBody { public decimal PickedQty { get; set; } public string? InventoryBatchNumber { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/picking-lines/{pickingLineId:guid}/confirm")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ConfirmProductionPickingLine")]
        public async Task<IActionResult> ConfirmPickingLine(Guid pickingLineId, [FromBody] ConfirmPickingBody body, CancellationToken cancellationToken)
            => Ok(await _service.ConfirmPickingLineAsync(GetOrganizationId(), GetUserId(), GetActorName(), pickingLineId, body.PickedQty, body.InventoryBatchNumber, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/material-issue")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_INVENTORY|MANAGE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("IssueMaterialToProduction")]
        public async Task<IActionResult> IssueMaterial([FromBody] SilaCulinaryIssueMaterialRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.IssueMaterialAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        // ---------------- KDS, Execution, Tasks, Quality & Traceability ----------------
        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/kds")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|VIEW_SILA_INVENTORY|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("GetCulinaryKdsFeed")]
        public async Task<IActionResult> GetKds([FromQuery] Guid? kitchenId, [FromQuery] string? viewMode, CancellationToken cancellationToken)
            => Ok(await _service.GetKdsFeedAsync(GetOrganizationId(), kitchenId, viewMode, cancellationToken));

        public class BatchActionBody { public string Action { get; set; } = string.Empty; public decimal? ActualOutputQty { get; set; } public string? Note { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/batches/{batchId:guid}/execute")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_RECIPE")]
        [SwaggerOperation("ExecuteCulinaryBatchAction")]
        public async Task<IActionResult> ExecuteBatchAction(Guid batchId, [FromBody] BatchActionBody body, CancellationToken cancellationToken)
            => Ok(await _service.ExecuteBatchActionAsync(GetOrganizationId(), GetUserId(), GetActorName(), batchId, body.Action, body.ActualOutputQty, body.Note, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/batches/{batchId:guid}/tasks")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryBatchTasks")]
        public async Task<IActionResult> ListBatchTasks(Guid batchId, CancellationToken cancellationToken)
            => Ok(await _service.ListBatchTasksAsync(GetOrganizationId(), batchId, cancellationToken));

        public class TaskStatusBody { public string Status { get; set; } = "COMPLETED"; public string? ExceptionReason { get; set; } }

        [HttpPut]
        [Route("api/v1/buyer/sila/culinary-production/tasks/{taskId:guid}/status")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_RECIPE")]
        [SwaggerOperation("UpdateCulinaryTaskStatus")]
        public async Task<IActionResult> UpdateTaskStatus(Guid taskId, [FromBody] TaskStatusBody body, CancellationToken cancellationToken)
            => Ok(await _service.UpdateTaskStatusAsync(GetOrganizationId(), GetUserId(), taskId, body.Status, body.ExceptionReason, cancellationToken));

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/quality")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("RecordCulinaryQualityCheck")]
        public async Task<IActionResult> RecordQualityCheck([FromBody] SilaCulinaryQualityRecordRequestDto request, CancellationToken cancellationToken)
            => Ok(await _service.RecordQualityCheckAsync(GetOrganizationId(), GetUserId(), GetActorName(), request, cancellationToken));

        public class QualityResolveBody { public string CorrectiveAction { get; set; } = string.Empty; public bool ApproveRelease { get; set; } = true; }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/quality/{qualityResultId:guid}/resolve-hold")]
        [ApiAuthorization(AnyOf = "APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ResolveCulinaryQualityHold")]
        public async Task<IActionResult> ResolveQualityHold(Guid qualityResultId, [FromBody] QualityResolveBody body, CancellationToken cancellationToken)
            => Ok(await _service.ResolveQualityHoldAsync(GetOrganizationId(), GetUserId(), GetRoleId(), GetActorName(), qualityResultId, body.CorrectiveAction, body.ApproveRelease, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/batches/{batchId:guid}/traceability")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("GetCulinaryBatchTraceability")]
        public async Task<IActionResult> GetBatchTraceability(Guid batchId, CancellationToken cancellationToken)
            => Ok(await _service.GetBatchTraceabilityAsync(GetOrganizationId(), batchId, cancellationToken));

        // ---------------- Central Kitchen, Buffet, Events, Cockpit, Exceptions, Copilot, Reports, Mobile ----------------
        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/central-kitchen/dispatches")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|VIEW_SILA_INVENTORY|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("ListCentralKitchenDispatches")]
        public async Task<IActionResult> ListDispatches([FromQuery] Guid? centralKitchenId, CancellationToken cancellationToken)
            => Ok(await _service.ListCentralKitchenDispatchesAsync(GetOrganizationId(), centralKitchenId, cancellationToken));

        public class CentralDispatchAllocateBody { public Guid CentralKitchenId { get; set; } public Guid BatchId { get; set; } public Guid DestinationOutletId { get; set; } public string DestinationOutletName { get; set; } = string.Empty; public decimal RequestedQty { get; set; } public DateTime? RequiredDeliveryUtc { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/central-kitchen/allocate")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("AllocateCentralKitchenDispatch")]
        public async Task<IActionResult> AllocateDispatch([FromBody] CentralDispatchAllocateBody body, CancellationToken cancellationToken)
            => Ok(await _service.AllocateCentralKitchenDispatchAsync(GetOrganizationId(), GetUserId(), GetActorName(), body.CentralKitchenId, body.BatchId, body.DestinationOutletId, body.DestinationOutletName, body.RequestedQty, body.RequiredDeliveryUtc, cancellationToken));

        public class CentralDispatchMoveBody { public string Action { get; set; } = "DISPATCH"; public decimal Qty { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/central-kitchen/dispatches/{dispatchId:guid}/move")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("MoveCentralKitchenDispatch")]
        public async Task<IActionResult> MoveDispatch(Guid dispatchId, [FromBody] CentralDispatchMoveBody body, CancellationToken cancellationToken)
            => Ok(await _service.UpdateDispatchMovementAsync(GetOrganizationId(), GetUserId(), GetActorName(), dispatchId, body.Action, body.Qty, cancellationToken));

        public class BuffetReplenishBody { public Guid KitchenId { get; set; } public string ItemName { get; set; } = string.Empty; public decimal Qty { get; set; } public Guid? RecipeId { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/buffet/replenish")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_RECIPE")]
        [SwaggerOperation("TriggerBuffetReplenishment")]
        public async Task<IActionResult> TriggerBuffetReplenishment([FromBody] BuffetReplenishBody body, CancellationToken cancellationToken)
            => Ok(await _service.TriggerBuffetReplenishmentAsync(GetOrganizationId(), GetUserId(), GetActorName(), body.KitchenId, body.ItemName, body.Qty, body.RecipeId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/events/{eventId:guid}/impact")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("AnalyzeEventProductionImpact")]
        public async Task<IActionResult> EventImpact(Guid eventId, [FromQuery] decimal revisedGuestCount, CancellationToken cancellationToken)
            => Ok(await _service.SyncEventProductionImpactAsync(GetOrganizationId(), GetRoleId(), eventId, revisedGuestCount, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/dashboard")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("GetCulinaryProductionDashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] Guid? propertyId, [FromQuery] Guid? kitchenId, CancellationToken cancellationToken)
            => Ok(await _service.GetDashboardAsync(GetOrganizationId(), GetRoleId(), propertyId, kitchenId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/exceptions")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("ListCulinaryProductionExceptions")]
        public async Task<IActionResult> ListExceptions([FromQuery] string? status, CancellationToken cancellationToken)
            => Ok(await _service.ListExceptionsAsync(GetOrganizationId(), status, cancellationToken));

        public class ReportExceptionBody { public Guid? OrderId { get; set; } public Guid? BatchId { get; set; } public string ExceptionType { get; set; } = "PRODUCTION_DELAY"; public string Severity { get; set; } = "HIGH"; public string Description { get; set; } = string.Empty; public string? RecommendedAction { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/exceptions")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|MANAGE_SILA_INVENTORY|VIEW_SILA_RECIPE")]
        [SwaggerOperation("ReportCulinaryProductionException")]
        public async Task<IActionResult> ReportException([FromBody] ReportExceptionBody body, CancellationToken cancellationToken)
            => Ok(await _service.ReportExceptionAsync(GetOrganizationId(), GetUserId(), GetActorName(), body.OrderId, body.BatchId, body.ExceptionType, body.Severity, body.Description, body.RecommendedAction, cancellationToken));

        public class ResolveExceptionBody { public string ResolutionStatus { get; set; } = "RESOLVED"; public string? Note { get; set; } }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/exceptions/{exceptionId:guid}/resolve")]
        [ApiAuthorization(AnyOf = "MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|MANAGE_SILA_MASTER_DATA")]
        [SwaggerOperation("ResolveCulinaryProductionException")]
        public async Task<IActionResult> ResolveException(Guid exceptionId, [FromBody] ResolveExceptionBody body, CancellationToken cancellationToken)
            => Ok(await _service.ResolveExceptionAsync(GetOrganizationId(), GetUserId(), GetActorName(), exceptionId, body.ResolutionStatus, body.Note, cancellationToken));

        public class CopilotQueryBody { public string Question { get; set; } = string.Empty; }

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/copilot")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("QueryCulinaryProductionCopilot")]
        public async Task<IActionResult> QueryCopilot([FromBody] CopilotQueryBody body, CancellationToken cancellationToken)
            => Ok(await _service.QueryCopilotAsync(GetOrganizationId(), GetUserId(), GetRoleId(), body.Question, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/reports/catalog")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("GetCulinaryProductionReportCatalog")]
        public IActionResult GetReportCatalog() => Ok(_service.GetReportCatalog());

        [HttpPost]
        [Route("api/v1/buyer/sila/culinary-production/reports/{reportCode}/export")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE|VIEW_SILA_INVENTORY")]
        [SwaggerOperation("ExportCulinaryProductionReport")]
        public async Task<IActionResult> ExportReport(string reportCode, [FromQuery] string format = "CSV", CancellationToken cancellationToken = default)
            => Ok(await _service.ExportReportAsync(GetOrganizationId(), GetRoleId(), reportCode, format, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/mobile/summary")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|VIEW_SILA_INVENTORY|MANAGE_SILA_INVENTORY")]
        [SwaggerOperation("GetMobileCulinaryProductionSummary")]
        public async Task<IActionResult> MobileSummary([FromQuery] Guid? kitchenId, CancellationToken cancellationToken)
            => Ok(await _service.GetMobileSummaryAsync(GetOrganizationId(), GetUserId(), GetRoleId(), kitchenId, cancellationToken));

        [HttpGet]
        [Route("api/v1/buyer/sila/culinary-production/phase4b-handover")]
        [ApiAuthorization(AnyOf = "VIEW_SILA_RECIPE|MANAGE_SILA_RECIPE|APPROVE_SILA_RECIPE")]
        [SwaggerOperation("GetPhase4BHandoverSummary")]
        public async Task<IActionResult> Phase4BHandover(CancellationToken cancellationToken)
            => Ok(await _service.GetPhase4BHandoverAsync(GetOrganizationId(), cancellationToken));
    }
}
