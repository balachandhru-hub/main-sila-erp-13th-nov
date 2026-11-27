using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryProductionService
    {
        // Kitchen, Workstation, Equipment, Staff
        Task<List<SilaCulinaryKitchenDto>> ListKitchensAsync(Guid organizationId, Guid? propertyId, CancellationToken cancellationToken);
        Task<SilaCulinaryKitchenDto> UpsertKitchenAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryUpsertKitchenRequestDto request, CancellationToken cancellationToken);
        Task<List<SilaCulinaryWorkstationDto>> ListWorkstationsAsync(Guid organizationId, Guid? kitchenId, CancellationToken cancellationToken);
        Task<SilaCulinaryWorkstationDto> UpsertWorkstationAsync(Guid organizationId, Guid userId, Guid kitchenId, string code, string name, string workstationType, int concurrentCapacity, CancellationToken cancellationToken);
        Task<List<SilaCulinaryEquipmentDto>> ListEquipmentAsync(Guid organizationId, Guid? kitchenId, CancellationToken cancellationToken);
        Task<SilaCulinaryEquipmentDto> UpsertEquipmentAsync(Guid organizationId, Guid userId, SilaCulinaryEquipmentDto request, CancellationToken cancellationToken);
        Task<List<SilaCulinaryStaffAssignmentDto>> ListStaffAssignmentsAsync(Guid organizationId, Guid? kitchenId, CancellationToken cancellationToken);
        Task<SilaCulinaryStaffAssignmentDto> UpsertStaffAssignmentAsync(Guid organizationId, Guid userId, SilaCulinaryStaffAssignmentDto request, CancellationToken cancellationToken);

        // Demand & Production Planning
        Task<List<SilaCulinaryProductionDemandDto>> ListDemandsAsync(Guid organizationId, Guid? propertyId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionDemandDto> CreateDemandAsync(Guid organizationId, Guid userId, SilaCulinaryProductionDemandDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionDemandDto> AdjustDemandAsync(Guid organizationId, Guid userId, Guid demandId, decimal adjustedPortions, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProductionPlanDto>> ListPlansAsync(Guid organizationId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPlanDto> GetPlanAsync(Guid organizationId, Guid roleId, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPlanDto> CreatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateProductionPlanRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPlanDto> ExplodeAndValidatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPlanDto> SubmitPlanForApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPlanDto> DecidePlanApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string decision, string? comment, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPlanDto> AmendReleasedPlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string changeSummary, List<SilaCulinaryCreatePlanLineInputDto> updatedLines, CancellationToken cancellationToken);

        // Production Orders, Batches, Scheduling & Calendar
        Task<SilaCulinaryProductionPlanDto> ReleasePlanToOrdersAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProductionOrderDto>> ListOrdersAsync(Guid organizationId, Guid roleId, Guid? kitchenId, string? status, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionBatchDto> RescheduleBatchAsync(Guid organizationId, Guid userId, string actorName, Guid batchId, DateTime plannedStartUtc, DateTime plannedFinishUtc, Guid? equipmentId, Guid? workstationId, CancellationToken cancellationToken);
        Task<SilaCulinaryScheduleOptimizeResponseDto> OptimizeScheduleAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryScheduleOptimizeRequestDto request, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProductionBatchDto>> ApplyScheduleAsync(Guid organizationId, Guid userId, string actorName, List<SilaCulinaryScheduledBatchDto> assignments, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProductionBatchDto>> GetProductionCalendarAsync(Guid organizationId, DateTime? fromUtc, DateTime? toUtc, Guid? kitchenId, CancellationToken cancellationToken);

        // Picking & Material Issue
        Task<List<SilaCulinaryProductionPickingLineDto>> GeneratePickingListAsync(Guid organizationId, Guid userId, string actorName, Guid orderId, string strategy, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPickingLineDto> ConfirmPickingLineAsync(Guid organizationId, Guid userId, string actorName, Guid pickingLineId, decimal pickedQty, string? inventoryBatchNumber, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionMaterialIssueRef> IssueMaterialAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryIssueMaterialRequestDto request, CancellationToken cancellationToken);

        // Execution, KDS, Tasks, Quality, Traceability
        Task<List<SilaCulinaryProductionBatchDto>> GetKdsFeedAsync(Guid organizationId, Guid? kitchenId, string? viewMode, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionBatchDto> ExecuteBatchActionAsync(Guid organizationId, Guid userId, string actorName, Guid batchId, string action, decimal? actualOutputQty, string? note, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProductionTaskDto>> ListBatchTasksAsync(Guid organizationId, Guid batchId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionTaskDto> UpdateTaskStatusAsync(Guid organizationId, Guid userId, Guid taskId, string status, string? exceptionReason, CancellationToken cancellationToken);
        Task<SilaCulinaryQualityResultDto> RecordQualityCheckAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryQualityRecordRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryQualityResultDto> ResolveQualityHoldAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid qualityResultId, string correctiveAction, bool approveRelease, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionBatchTraceabilityDto> GetBatchTraceabilityAsync(Guid organizationId, Guid batchId, CancellationToken cancellationToken);

        // Central Kitchen, Buffet, Events, Cockpit, Exceptions, Copilot, Reports, Mobile, Phase 4B
        Task<List<SilaCulinaryProductionDispatchDto>> ListCentralKitchenDispatchesAsync(Guid organizationId, Guid? centralKitchenId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionDispatchDto> AllocateCentralKitchenDispatchAsync(Guid organizationId, Guid userId, string actorName, Guid centralKitchenId, Guid batchId, Guid destinationOutletId, string destinationOutletName, decimal requestedQty, DateTime? requiredDeliveryUtc, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionDispatchDto> UpdateDispatchMovementAsync(Guid organizationId, Guid userId, string actorName, Guid dispatchId, string action, decimal qty, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionBatchDto> TriggerBuffetReplenishmentAsync(Guid organizationId, Guid userId, string actorName, Guid kitchenId, string itemName, decimal qty, Guid? recipeId, CancellationToken cancellationToken);
        Task<object> SyncEventProductionImpactAsync(Guid organizationId, Guid roleId, Guid eventId, decimal revisedGuestCount, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionDashboardDto> GetDashboardAsync(Guid organizationId, Guid roleId, Guid? propertyId, Guid? kitchenId, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProductionExceptionDto>> ListExceptionsAsync(Guid organizationId, string? status, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionExceptionDto> ReportExceptionAsync(Guid organizationId, Guid userId, string actorName, Guid? orderId, Guid? batchId, string exceptionType, string severity, string description, string? recommendedAction, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionExceptionDto> ResolveExceptionAsync(Guid organizationId, Guid userId, string actorName, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionCopilotResponseDto> QueryCopilotAsync(Guid organizationId, Guid userId, Guid roleId, string question, CancellationToken cancellationToken);
        List<SilaCulinaryProductionReportCatalogItemDto> GetReportCatalog();
        Task<SilaCulinaryAnalyticsReportExportDto> ExportReportAsync(Guid organizationId, Guid roleId, string reportCode, string format, CancellationToken cancellationToken);
        Task<SilaCulinaryMobileProductionSummaryDto> GetMobileSummaryAsync(Guid organizationId, Guid userId, Guid roleId, Guid? kitchenId, CancellationToken cancellationToken);
        Task<SilaCulinaryProductionPhase4BHandoverDto> GetPhase4BHandoverAsync(Guid organizationId, CancellationToken cancellationToken);
    }

    public class SilaCulinaryProductionService : ISilaCulinaryProductionService
    {
        private static readonly JsonSerializerOptions CamelJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private static readonly HashSet<string> AllowedKitchenTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "MAIN_KITCHEN", "CENTRAL_KITCHEN", "BANQUET_KITCHEN", "PASTRY_KITCHEN", "BAKERY",
            "COLD_KITCHEN", "HOT_KITCHEN", "BUTCHERY", "BREAKFAST_KITCHEN", "SPECIALTY_KITCHEN",
            "CLOUD_KITCHEN", "STAFF_KITCHEN", "SATELLITE_KITCHEN"
        };

        private static readonly Regex PromptInjectionRegex = new(
            @"(ignore\s+(all\s+)?previous|system\s+prompt|override\s+(safety|food\s*safety|budget)|drop\s+table|post\s+sap|approve\s+all|delete\s+from)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SilaCulinaryProductionService(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        private static bool CanViewCosts(Guid roleId) => SilaAccess.HasFullAccess(roleId);

        private static bool CanApproveProduction(Guid roleId) =>
            roleId == Common.BUYER_ADMIN_ROLE_ID
            || roleId == Common.FNB_DIRECTOR_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID
            || roleId == Common.OUTLET_MANAGER_ROLE_ID
            || roleId == Common.STORE_MANAGER_ROLE_ID;

        private async Task<BuyerBusinessProfile> ResolveBuyerAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Buyer organization not found.", "Buyer organization profile was not found.");

            SilaCulinaryAiConfiguration? aiCfg = await _repository.SilaCulinaryAiConfiguration
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (aiCfg != null && !string.IsNullOrWhiteSpace(aiCfg.FeatureFlagsJson))
            {
                SilaCulinaryAiFeatureFlagsDto? flags = SafeDeserialize<SilaCulinaryAiFeatureFlagsDto>(aiCfg.FeatureFlagsJson);
                if (flags != null && !flags.CulinaryPhase4AEnabled)
                    throw new ForBiddenCustomException("Feature disabled.", "Phase 4A Kitchen Production is disabled for this tenant.");
            }
            return buyer;
        }

        private void RecordAudit(Guid buyerId, Guid userId, string actorName, string actionType, string entityType, Guid? entityId, string summary)
        {
            _repository.SilaCulinaryProductionAudit.Create(new SilaCulinaryProductionAudit
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ActorUserId = userId,
                ActorName = string.IsNullOrWhiteSpace(actorName) ? "System" : actorName,
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Summary = summary.Length > 590 ? summary[..590] : summary,
                ExecutedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
        }

        private static string SafeCsvCell(string? value)
        {
            string s = value ?? string.Empty;
            if (s.Length > 0 && (s[0] == '=' || s[0] == '+' || s[0] == '-' || s[0] == '@'))
            {
                s = "'" + s;
            }
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static T? SafeDeserialize<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonSerializer.Deserialize<T>(json, CamelJson); }
            catch { return null; }
        }

        // -------------------------------------------------------------------------
        // 4A.2 Kitchens, Workstations, Equipment & Staff Master
        // -------------------------------------------------------------------------
        public async Task<List<SilaCulinaryKitchenDto>> ListKitchensAsync(Guid organizationId, Guid? propertyId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryKitchen> q = _repository.SilaCulinaryKitchen.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId);
            List<SilaCulinaryKitchen> rows = await q.OrderBy(x => x.KitchenCode).ToListAsync(cancellationToken);
            return rows.Select(MapKitchen).ToList();
        }

        public async Task<SilaCulinaryKitchenDto> UpsertKitchenAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryUpsertKitchenRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.KitchenCode) || string.IsNullOrWhiteSpace(request.KitchenName))
                throw new BadRequestCustomException("Kitchen code and name are required.", "Provide a valid kitchen code and name.");

            string normalizedType = (request.KitchenType ?? "MAIN_KITCHEN").Trim().ToUpperInvariant();
            if (!AllowedKitchenTypes.Contains(normalizedType))
                throw new BadRequestCustomException($"Unsupported kitchen type '{normalizedType}'.", "Select one of the supported culinary kitchen types.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryKitchen? entity = null;
            if (request.Id.HasValue)
            {
                entity = await _repository.SilaCulinaryKitchen
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.Id.Value && x.IsActive)
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (entity == null)
            {
                bool dupCode = await _repository.SilaCulinaryKitchen
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.KitchenCode == request.KitchenCode.Trim() && x.IsActive)
                    .AnyAsync(cancellationToken);
                if (dupCode)
                    throw new BadRequestCustomException("Kitchen code already exists.", "Use a unique kitchen code within the tenant.");

                entity = new SilaCulinaryKitchen
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    EffectiveFromUtc = DateTime.UtcNow,
                    IsActive = true
                };
                _repository.SilaCulinaryKitchen.Create(entity);
            }
            else
            {
                _repository.SilaCulinaryKitchen.Update(entity);
            }

            entity.PropertyId = request.PropertyId;
            entity.PropertyName = request.PropertyName;
            entity.KitchenCode = request.KitchenCode.Trim().ToUpperInvariant();
            entity.KitchenName = request.KitchenName.Trim();
            entity.KitchenType = normalizedType;
            entity.AssociatedStoreLocationId = request.AssociatedStoreLocationId;
            entity.ResponsibleExecutiveChef = request.ResponsibleExecutiveChef;
            entity.KitchenSupervisor = request.KitchenSupervisor;
            entity.DailyPortionCapacity = request.DailyPortionCapacity > 0 ? request.DailyPortionCapacity : 1000m;
            entity.OutletAssociationsJson = JsonSerializer.Serialize(request.OutletIds ?? new List<Guid>(), CamelJson);
            entity.SupportedMealPeriodsJson = JsonSerializer.Serialize(request.SupportedMealPeriods ?? new List<string>(), CamelJson);
            entity.SupportedCategoriesJson = JsonSerializer.Serialize(request.SupportedCategories ?? new List<string>(), CamelJson);

            RecordAudit(buyer.Id, userId, actorName, "KITCHEN_UPSERTED", "KITCHEN", entity.Id, $"Saved kitchen {entity.KitchenCode} ({entity.KitchenType})");
            await _repository.SaveAsync();
            return MapKitchen(entity);
        }

        public async Task<List<SilaCulinaryWorkstationDto>> ListWorkstationsAsync(Guid organizationId, Guid? kitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryWorkstation> q = _repository.SilaCulinaryWorkstation.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (kitchenId.HasValue) q = q.Where(x => x.KitchenId == kitchenId.Value);
            List<SilaCulinaryWorkstation> list = await q.OrderBy(x => x.WorkstationCode).ToListAsync(cancellationToken);
            return list.Select(x => new SilaCulinaryWorkstationDto
            {
                Id = x.Id,
                KitchenId = x.KitchenId,
                WorkstationCode = x.WorkstationCode,
                WorkstationName = x.WorkstationName,
                WorkstationType = x.WorkstationType,
                ConcurrentCapacity = x.ConcurrentCapacity
            }).ToList();
        }

        public async Task<SilaCulinaryWorkstationDto> UpsertWorkstationAsync(Guid organizationId, Guid userId, Guid kitchenId, string code, string name, string workstationType, int concurrentCapacity, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool kitchenExists = await _repository.SilaCulinaryKitchen
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == kitchenId && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!kitchenExists)
                throw new NotFoundCustomException("Kitchen not found.", "Assign workstation to an active kitchen.");

            string normCode = code.Trim().ToUpperInvariant();
            SilaCulinaryWorkstation? entity = await _repository.SilaCulinaryWorkstation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.KitchenId == kitchenId && x.WorkstationCode == normCode && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);
            if (entity == null)
            {
                entity = new SilaCulinaryWorkstation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    KitchenId = kitchenId,
                    WorkstationCode = normCode,
                    IsActive = true
                };
                _repository.SilaCulinaryWorkstation.Create(entity);
            }
            else
            {
                _repository.SilaCulinaryWorkstation.Update(entity);
            }

            entity.WorkstationName = name.Trim();
            entity.WorkstationType = string.IsNullOrWhiteSpace(workstationType) ? "COOKING" : workstationType.Trim().ToUpperInvariant();
            entity.ConcurrentCapacity = Math.Max(1, concurrentCapacity);
            await _repository.SaveAsync();
            return new SilaCulinaryWorkstationDto
            {
                Id = entity.Id,
                KitchenId = entity.KitchenId,
                WorkstationCode = entity.WorkstationCode,
                WorkstationName = entity.WorkstationName,
                WorkstationType = entity.WorkstationType,
                ConcurrentCapacity = entity.ConcurrentCapacity
            };
        }

        public async Task<List<SilaCulinaryEquipmentDto>> ListEquipmentAsync(Guid organizationId, Guid? kitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryEquipment> q = _repository.SilaCulinaryEquipment.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (kitchenId.HasValue) q = q.Where(x => x.KitchenId == kitchenId.Value);
            List<SilaCulinaryEquipment> list = await q.OrderBy(x => x.EquipmentCode).ToListAsync(cancellationToken);
            return list.Select(MapEquipment).ToList();
        }

        public async Task<SilaCulinaryEquipmentDto> UpsertEquipmentAsync(Guid organizationId, Guid userId, SilaCulinaryEquipmentDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string code = request.EquipmentCode.Trim().ToUpperInvariant();
            SilaCulinaryEquipment? entity = await _repository.SilaCulinaryEquipment
                .FindByCondition(x => x.BuyerId == buyer.Id && x.KitchenId == request.KitchenId && (x.Id == request.Id || x.EquipmentCode == code) && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);
            if (entity == null)
            {
                entity = new SilaCulinaryEquipment
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    KitchenId = request.KitchenId,
                    EquipmentCode = code,
                    IsActive = true
                };
                _repository.SilaCulinaryEquipment.Create(entity);
            }
            else
            {
                _repository.SilaCulinaryEquipment.Update(entity);
            }

            entity.WorkstationId = request.WorkstationId;
            entity.EquipmentName = request.EquipmentName.Trim();
            entity.EquipmentType = string.IsNullOrWhiteSpace(request.EquipmentType) ? "COMBI_OVEN" : request.EquipmentType.Trim().ToUpperInvariant();
            entity.BatchCapacity = request.BatchCapacity > 0 ? request.BatchCapacity : 50m;
            entity.CapacityUom = string.IsNullOrWhiteSpace(request.CapacityUom) ? "PORTION" : request.CapacityUom.Trim().ToUpperInvariant();
            entity.SetupMinutes = Math.Max(0, request.SetupMinutes);
            entity.CleaningMinutes = Math.Max(0, request.CleaningMinutes);
            entity.AvailableFromMinute = request.AvailableFromMinute > 0 ? request.AvailableFromMinute : 360;
            entity.AvailableUntilMinute = request.AvailableUntilMinute > 0 ? request.AvailableUntilMinute : 1320;
            entity.UnderMaintenance = request.UnderMaintenance;
            entity.TemperatureCapability = request.TemperatureCapability;
            entity.AllergenSegregated = request.AllergenSegregated;
            await _repository.SaveAsync();
            return MapEquipment(entity);
        }

        public async Task<List<SilaCulinaryStaffAssignmentDto>> ListStaffAssignmentsAsync(Guid organizationId, Guid? kitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryStaffAssignment> q = _repository.SilaCulinaryStaffAssignment.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (kitchenId.HasValue) q = q.Where(x => x.KitchenId == kitchenId.Value);
            List<SilaCulinaryStaffAssignment> list = await q.OrderBy(x => x.StaffName).ToListAsync(cancellationToken);
            return list.Select(MapStaff).ToList();
        }

        public async Task<SilaCulinaryStaffAssignmentDto> UpsertStaffAssignmentAsync(Guid organizationId, Guid userId, SilaCulinaryStaffAssignmentDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryStaffAssignment? entity = null;
            if (request.Id != Guid.Empty)
            {
                entity = await _repository.SilaCulinaryStaffAssignment
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.Id && x.IsActive)
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (entity == null)
            {
                entity = new SilaCulinaryStaffAssignment
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    IsActive = true
                };
                _repository.SilaCulinaryStaffAssignment.Create(entity);
            }
            else
            {
                _repository.SilaCulinaryStaffAssignment.Update(entity);
            }

            entity.PropertyId = request.PropertyId;
            entity.UserId = request.UserId == Guid.Empty ? userId : request.UserId;
            entity.StaffName = request.StaffName.Trim();
            entity.CulinaryRole = string.IsNullOrWhiteSpace(request.CulinaryRole) ? "CHEF_DE_PARTIE" : request.CulinaryRole.Trim().ToUpperInvariant();
            entity.KitchenId = request.KitchenId;
            entity.WorkstationId = request.WorkstationId;
            entity.ShiftCode = string.IsNullOrWhiteSpace(request.ShiftCode) ? "AM" : request.ShiftCode.Trim().ToUpperInvariant();
            entity.SkillClassification = string.IsNullOrWhiteSpace(request.SkillClassification) ? "HOT_PRODUCTION" : request.SkillClassification.Trim().ToUpperInvariant();
            entity.SupervisorName = request.SupervisorName;
            entity.AvailableFromMinute = request.AvailableFromMinute > 0 ? request.AvailableFromMinute : 360;
            entity.AvailableUntilMinute = request.AvailableUntilMinute > 0 ? request.AvailableUntilMinute : 840;
            entity.IsTemporary = request.IsTemporary;
            entity.IsCrossKitchen = request.IsCrossKitchen;
            entity.IsAvailable = request.IsAvailable;
            await _repository.SaveAsync();
            return MapStaff(entity);
        }

        // -------------------------------------------------------------------------
        // 4A.3 Demand Conversion & Production Planning
        // -------------------------------------------------------------------------
        public async Task<List<SilaCulinaryProductionDemandDto>> ListDemandsAsync(Guid organizationId, Guid? propertyId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionDemand> q = _repository.SilaCulinaryProductionDemand.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId.Value);
            List<SilaCulinaryProductionDemand> rows = await q.OrderBy(x => x.RequiredCompletionUtc).ToListAsync(cancellationToken);
            return rows.Select(MapDemand).ToList();
        }

        public async Task<SilaCulinaryProductionDemandDto> CreateDemandAsync(Guid organizationId, Guid userId, SilaCulinaryProductionDemandDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            if (string.IsNullOrWhiteSpace(request.SourceDocumentKey))
                throw new BadRequestCustomException("Source document key is required.", "Provide a deterministic source document key to prevent duplicate conversion.");

            string key = request.SourceDocumentKey.Trim();
            bool exists = await _repository.SilaCulinaryProductionDemand
                .FindByCondition(x => x.BuyerId == buyer.Id && x.SourceDocumentKey == key && x.IsActive)
                .AnyAsync(cancellationToken);
            if (exists)
                throw new BadRequestCustomException($"Duplicate demand conversion blocked for source '{key}'.", "This source document has already been converted into production demand.");

            SilaCulinaryProductionDemand entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                KitchenId = request.KitchenId,
                OutletId = request.OutletId,
                EventId = request.EventId,
                SourceDocumentKey = key,
                DemandSource = string.IsNullOrWhiteSpace(request.DemandSource) ? "MANUAL" : request.DemandSource.Trim().ToUpperInvariant(),
                DemandNature = string.IsNullOrWhiteSpace(request.DemandNature) ? "CONFIRMED" : request.DemandNature.Trim().ToUpperInvariant(),
                RecipeId = request.RecipeId,
                MenuItemName = request.MenuItemName.Trim(),
                MealPeriod = string.IsNullOrWhiteSpace(request.MealPeriod) ? "DINNER" : request.MealPeriod.Trim().ToUpperInvariant(),
                ProductionDateUtc = request.ProductionDateUtc == default ? DateTime.UtcNow.Date : request.ProductionDateUtc,
                RequiredCompletionUtc = request.RequiredCompletionUtc == default ? DateTime.UtcNow.AddHours(6) : request.RequiredCompletionUtc,
                ForecastPortions = request.ForecastPortions,
                AdjustedPortions = request.AdjustedPortions > 0 ? request.AdjustedPortions : request.ForecastPortions,
                Priority = request.Priority > 0 ? request.Priority : 50,
                ConversionStatus = "OPEN",
                IsActive = true
            };
            _repository.SilaCulinaryProductionDemand.Create(entity);
            await _repository.SaveAsync();
            return MapDemand(entity);
        }

        public async Task<SilaCulinaryProductionDemandDto> AdjustDemandAsync(Guid organizationId, Guid userId, Guid demandId, decimal adjustedPortions, CancellationToken cancellationToken)
        {
            if (adjustedPortions <= 0)
                throw new BadRequestCustomException("Adjusted portions must be greater than zero.", "Provide a positive portion count.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionDemand entity = await _repository.SilaCulinaryProductionDemand
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == demandId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Demand record not found.", "Verify the demand ID.");

            if (entity.ConversionStatus == "CONVERTED")
                throw new BadRequestCustomException("Demand is already converted into an active plan.", "Use controlled plan amendment to adjust quantities after conversion.");

            entity.AdjustedPortions = adjustedPortions;
            entity.DemandNature = "REVISED";
            _repository.SilaCulinaryProductionDemand.Update(entity);
            await _repository.SaveAsync();
            return MapDemand(entity);
        }

        public async Task<List<SilaCulinaryProductionPlanDto>> ListPlansAsync(Guid organizationId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionPlan> q = _repository.SilaCulinaryProductionPlan.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim().ToUpperInvariant());

            List<SilaCulinaryProductionPlan> plans = await q.OrderByDescending(x => x.ProductionDateUtc).ToListAsync(cancellationToken);
            List<Guid> planIds = plans.Select(p => p.Id).ToList();
            List<SilaCulinaryProductionPlanLine> allLines = planIds.Count == 0
                ? new List<SilaCulinaryProductionPlanLine>()
                : await _repository.SilaCulinaryProductionPlanLine.FindByCondition(x => x.BuyerId == buyer.Id && planIds.Contains(x.ProductionPlanId) && x.IsActive).ToListAsync(cancellationToken);

            bool showCosts = CanViewCosts(roleId);
            return plans.Select(p => MapPlan(p, allLines.Where(l => l.ProductionPlanId == p.Id).ToList(), showCosts)).ToList();
        }

        public async Task<SilaCulinaryProductionPlanDto> GetPlanAsync(Guid organizationId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPlan plan = await _repository.SilaCulinaryProductionPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production plan not found.", "Verify the production plan ID.");
            List<SilaCulinaryProductionPlanLine> lines = await _repository.SilaCulinaryProductionPlanLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            return MapPlan(plan, lines, CanViewCosts(roleId));
        }

        public async Task<SilaCulinaryProductionPlanDto> CreatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateProductionPlanRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string code = await DocumentNumber.NextAsync(_repository, buyer.Id, "PRD-PLN", 5, cancellationToken);
            DateTime prodDate = request.ProductionDateUtc?.Date ?? DateTime.UtcNow.Date;

            SilaCulinaryProductionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PlanCode = code,
                PropertyId = request.PropertyId,
                PropertyName = request.PropertyName,
                ProductionDateUtc = prodDate,
                PlanningPeriod = string.IsNullOrWhiteSpace(request.PlanningPeriod) ? "DAILY" : request.PlanningPeriod.Trim().ToUpperInvariant(),
                MealPeriod = string.IsNullOrWhiteSpace(request.MealPeriod) ? "DINNER" : request.MealPeriod.Trim().ToUpperInvariant(),
                EventId = request.EventId,
                EventReference = request.EventReference,
                ResponsibleChef = string.IsNullOrWhiteSpace(request.ResponsibleChef) ? actorName : request.ResponsibleChef.Trim(),
                Status = "DEMAND_CALCULATED",
                VersionNumber = 1,
                IsActive = true
            };
            _repository.SilaCulinaryProductionPlan.Create(plan);

            List<SilaCulinaryProductionPlanLine> createdLines = new();
            if (request.DemandIds.Count > 0)
            {
                List<SilaCulinaryProductionDemand> demands = await _repository.SilaCulinaryProductionDemand
                    .FindByCondition(x => x.BuyerId == buyer.Id && request.DemandIds.Contains(x.Id) && x.IsActive)
                    .AsTracking()
                    .ToListAsync(cancellationToken);

                foreach (SilaCulinaryProductionDemand demand in demands)
                {
                    if (demand.ConversionStatus == "CONVERTED")
                        throw new BadRequestCustomException($"Demand '{demand.SourceDocumentKey}' is already converted.", "Duplicate demand conversion is not permitted.");

                    demand.ConversionStatus = "CONVERTED";
                    demand.ProductionPlanId = plan.Id;
                    _repository.SilaCulinaryProductionDemand.Update(demand);

                    createdLines.Add(new SilaCulinaryProductionPlanLine
                    {
                        Id = Guid.NewGuid(),
                        ProductionPlanId = plan.Id,
                        BuyerId = buyer.Id,
                        RecipeId = demand.RecipeId,
                        MenuItemName = demand.MenuItemName,
                        RequiredPortions = demand.AdjustedPortions > 0 ? demand.AdjustedPortions : demand.ForecastPortions,
                        PlannedProductionQty = demand.AdjustedPortions > 0 ? demand.AdjustedPortions : demand.ForecastPortions,
                        KitchenId = demand.KitchenId,
                        PlannedStartUtc = demand.RequiredCompletionUtc.AddHours(-3),
                        RequiredCompletionUtc = demand.RequiredCompletionUtc,
                        Priority = demand.Priority,
                        IsActive = true
                    });
                }
            }

            foreach (SilaCulinaryCreatePlanLineInputDto line in request.ManualLines ?? new List<SilaCulinaryCreatePlanLineInputDto>())
            {
                DateTime reqComp = line.RequiredCompletionUtc ?? prodDate.AddHours(18);
                createdLines.Add(new SilaCulinaryProductionPlanLine
                {
                    Id = Guid.NewGuid(),
                    ProductionPlanId = plan.Id,
                    BuyerId = buyer.Id,
                    RecipeId = line.RecipeId,
                    MenuItemName = line.MenuItemName.Trim(),
                    RequiredPortions = line.RequiredPortions,
                    PlannedProductionQty = line.RequiredPortions,
                    KitchenId = line.KitchenId,
                    WorkstationId = line.WorkstationId,
                    PlannedStartUtc = reqComp.AddHours(-3),
                    RequiredCompletionUtc = reqComp,
                    Priority = line.Priority,
                    IsActive = true
                });
            }

            // Enrich recipe code/version/estimated food cost from approved Recipes
            List<Guid> recipeIds = createdLines.Where(l => l.RecipeId.HasValue).Select(l => l.RecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipeMap = recipeIds.Count == 0
                ? new Dictionary<Guid, Recipe>()
                : await _repository.Recipe.FindByCondition(x => x.BuyerId == buyer.Id && recipeIds.Contains(x.Id) && x.IsActive)
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

            decimal totalCost = 0m;
            foreach (SilaCulinaryProductionPlanLine line in createdLines)
            {
                if (line.RecipeId.HasValue && recipeMap.TryGetValue(line.RecipeId.Value, out Recipe? rec))
                {
                    line.RecipeCode = rec.RecipeCode;
                    line.RecipeVersion = rec.ActiveVersion > 0 ? rec.ActiveVersion : rec.Version;
                    decimal perPortion = rec.ServingQty > 0 ? rec.TotalCost / rec.ServingQty : rec.TotalCost;
                    line.EstimatedFoodCost = Math.Round(perPortion * line.PlannedProductionQty, 2);
                    totalCost += line.EstimatedFoodCost;
                }
                _repository.SilaCulinaryProductionPlanLine.Create(line);
            }

            plan.TotalEstimatedCost = totalCost;
            _repository.SilaCulinaryProductionPlanVersion.Create(new SilaCulinaryProductionPlanVersion
            {
                Id = Guid.NewGuid(),
                ProductionPlanId = plan.Id,
                BuyerId = buyer.Id,
                VersionNumber = 1,
                ChangeSummary = "Initial production plan generated",
                SnapshotJson = JsonSerializer.Serialize(createdLines, CamelJson),
                IsActive = true
            });

            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_PLAN_CREATED, Common.SILA_REF_PRODUCTION_PLAN, plan.Id, $"Created production plan {plan.PlanCode} with {createdLines.Count} line(s)");
            await _repository.SaveAsync();
            return MapPlan(plan, createdLines, CanViewCosts(roleId));
        }

        // -------------------------------------------------------------------------
        // 4A.4 Multi-level Recipe Explosion, Circular Detection, Availability & PR Integration
        // -------------------------------------------------------------------------
        public async Task<SilaCulinaryProductionPlanDto> ExplodeAndValidatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPlan plan = await _repository.SilaCulinaryProductionPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production plan not found.", "Verify the production plan ID.");

            List<SilaCulinaryProductionPlanLine> lines = await _repository.SilaCulinaryProductionPlanLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionPlanId == plan.Id && x.IsActive)
                .AsTracking()
                .ToListAsync(cancellationToken);

            List<Recipe> allRecipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .Include(x => x.Ingredients)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, Recipe> recipeMap = allRecipes.ToDictionary(r => r.Id, r => r);

            List<InventoryBalance> balances = await _repository.InventoryBalance
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryStockReservation> activeReservations = await _repository.SilaCulinaryStockReservation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (x.Status == "ACTIVE" || x.Status == "PARTIALLY_FULFILLED"))
                .ToListAsync(cancellationToken);
            List<GoodsReceiptItem> grnBatches = await _repository.GoodsReceiptItem
                .FindByCondition(x => x.GoodsReceipt.BuyerId == buyer.Id && x.IsActive)
                .Include(x => x.GoodsReceipt)
                .ToListAsync(cancellationToken);
            List<PurchaseOrderItem> openPoItems = await _repository.PurchaseOrderItem
                .FindByCondition(x => x.PurchaseOrder.BuyerId == buyer.Id && x.IsActive && x.PurchaseOrder.Status != "CANCELLED")
                .Include(x => x.PurchaseOrder)
                .ToListAsync(cancellationToken);

            bool anyShortage = false;
            Dictionary<string, decimal> consolidatedReq = new(StringComparer.OrdinalIgnoreCase);

            foreach (SilaCulinaryProductionPlanLine line in lines)
            {
                if (line.RecipeId.HasValue && recipeMap.ContainsKey(line.RecipeId.Value))
                {
                    HashSet<Guid> visiting = new();
                    Dictionary<string, decimal> lineIngredients = new(StringComparer.OrdinalIgnoreCase);
                    ExplodeRecipeRecursive(line.RecipeId.Value, line.PlannedProductionQty, recipeMap, visiting, lineIngredients);

                    bool lineHasShortage = false;
                    bool lineHasExpected = false;
                    foreach (KeyValuePair<string, decimal> kvp in lineIngredients)
                    {
                        consolidatedReq[kvp.Key] = consolidatedReq.GetValueOrDefault(kvp.Key) + kvp.Value;
                        (decimal eligible, decimal incoming, decimal reserved, string readiness) = EvaluateMaterialAvailability(
                            kvp.Key, kvp.Value, line.RequiredCompletionUtc, materials, balances, activeReservations, grnBatches, openPoItems);
                        if (readiness is "SHORTAGE" or "QUALITY_BLOCKED" or "EXPIRED" or "DATA_UNAVAILABLE")
                            lineHasShortage = true;
                        else if (readiness == "EXPECTED_BEFORE_PRODUCTION")
                            lineHasExpected = true;
                    }

                    line.IngredientReadiness = lineHasShortage ? "SHORTAGE" : (lineHasExpected ? "EXPECTED_BEFORE_PRODUCTION" : "AVAILABLE");
                    if (lineHasShortage) anyShortage = true;
                }
                else
                {
                    line.IngredientReadiness = "AVAILABLE";
                }
                _repository.SilaCulinaryProductionPlanLine.Update(line);
            }

            // Integrate shortages with existing Purchase Recommendation / OperationalTask without auto-transmitting POs
            if (anyShortage)
            {
                foreach (KeyValuePair<string, decimal> req in consolidatedReq)
                {
                    (decimal eligible, decimal incoming, decimal reserved, string readiness) = EvaluateMaterialAvailability(
                        req.Key, req.Value, plan.ProductionDateUtc.AddHours(18), materials, balances, activeReservations, grnBatches, openPoItems);
                    decimal shortage = Math.Max(0m, req.Value - Math.Max(0m, eligible - reserved) - incoming);
                    if (shortage > 0m)
                    {
                        string dedupKey = $"PROD_SHORTAGE_{plan.PlanCode}_{req.Key}";
                        bool recExists = await _repository.SilaCulinaryRecommendation
                            .FindByCondition(x => x.BuyerId == buyer.Id && x.DeduplicationKey == dedupKey && x.IsActive)
                            .AnyAsync(cancellationToken);
                        if (!recExists)
                        {
                            _repository.SilaCulinaryRecommendation.Create(new SilaCulinaryRecommendation
                            {
                                Id = Guid.NewGuid(),
                                BuyerId = buyer.Id,
                                PropertyId = plan.PropertyId,
                                RecommendationCode = $"REC-P4A-{plan.PlanCode}-{req.Key}",
                                Category = "PRODUCTION_SHORTAGE",
                                Priority = "HIGH",
                                Title = $"Ingredient shortage for {plan.PlanCode}: {req.Key} ({shortage:N2})",
                                CurrentSituation = $"Eligible stock ({eligible:N2}) less commitments ({reserved:N2}) plus PO arriving before production ({incoming:N2}) is below required {req.Value:N2}.",
                                ProposedAction = "Create purchase recommendation, expedite existing open PO, or transfer from central store.",
                                DeduplicationKey = dedupKey,
                                Status = "NEW",
                                IsActive = true
                            });
                        }

                        await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
                        {
                            TaskType = "PRODUCTION_SHORTAGE",
                            DeduplicationKey = dedupKey,
                            Title = $"Resolve shortage {req.Key} ({shortage:N2}) for {plan.PlanCode}",
                            Description = "Trigger Purchase Recommendation or Stock Transfer (manual procurement approval required).",
                            Priority = SilaOperations.PRIORITY_ACTION,
                            SourceModule = "CULINARY_PRODUCTION",
                            SourceDocumentType = Common.SILA_REF_PRODUCTION_PLAN,
                            SourceDocumentId = plan.Id,
                            SourceDocumentNumber = plan.PlanCode
                        }, cancellationToken);
                    }
                }
            }

            plan.Status = anyShortage ? "SHORTAGE_IDENTIFIED" : "INGREDIENTS_VALIDATED";
            _repository.SilaCulinaryProductionPlan.Update(plan);
            await _repository.SaveAsync();
            return MapPlan(plan, lines, CanViewCosts(roleId));
        }

        private static void ExplodeRecipeRecursive(
            Guid recipeId,
            decimal targetQty,
            Dictionary<Guid, Recipe> recipeMap,
            HashSet<Guid> visiting,
            Dictionary<string, decimal> accumulator)
        {
            if (!visiting.Add(recipeId))
                throw new BadRequestCustomException("Circular recipe dependency detected.", $"Sub-recipe cycle detected involving recipe ID {recipeId}.");

            if (recipeMap.TryGetValue(recipeId, out Recipe? recipe))
            {
                decimal baseServing = recipe.ServingQty > 0 ? recipe.ServingQty : 1m;
                decimal scale = targetQty / baseServing;

                // Apply yield / loss factor once at this recipe level
                decimal lossPct = (recipe.TrimLossPercent ?? 0m) + (recipe.CookingLossPercent ?? 0m) + (recipe.WasteFactorPercent ?? 0m);
                decimal yieldMultiplier = lossPct > 0m && lossPct < 90m ? 1m / (1m - (lossPct / 100m)) : 1m;

                foreach (RecipeIngredient ing in recipe.Ingredients.Where(i => i.IsActive))
                {
                    decimal ingQty = (ing.BaseQuantity > 0 ? ing.BaseQuantity : ing.Quantity) * scale;
                    if (ing.SubRecipeId.HasValue && recipeMap.ContainsKey(ing.SubRecipeId.Value))
                    {
                        ExplodeRecipeRecursive(ing.SubRecipeId.Value, ingQty, recipeMap, visiting, accumulator);
                    }
                    else
                    {
                        decimal leafQty = Math.Round(ingQty * yieldMultiplier, 4);
                        string matCode = string.IsNullOrWhiteSpace(ing.ItemCode) ? ing.IngredientCode : ing.ItemCode;
                        accumulator[matCode] = accumulator.GetValueOrDefault(matCode) + leafQty;
                    }
                }
            }

            visiting.Remove(recipeId);
        }

        private static (decimal EligibleStock, decimal IncomingBeforeProduction, decimal ReservedCommitments, string Readiness) EvaluateMaterialAvailability(
            string materialCode,
            decimal requiredQty,
            DateTime requiredByUtc,
            List<ItemBuyerMaster> materials,
            List<InventoryBalance> balances,
            List<SilaCulinaryStockReservation> reservations,
            List<GoodsReceiptItem> grnBatches,
            List<PurchaseOrderItem> openPoItems)
        {
            decimal expiredQty = grnBatches
                .Where(g => string.Equals(g.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase)
                            && g.ExpiryDate.HasValue && g.ExpiryDate.Value <= DateTime.UtcNow)
                .Sum(g => Math.Max(0m, g.AcceptedQty - g.ReversedQty));

            decimal reserved = reservations
                .Where(r => string.Equals(r.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase))
                .Sum(r => Math.Max(0m, r.ReservedQty - r.ReleasedQty));

            decimal incomingBeforeProd = openPoItems
                .Where(p => string.Equals(p.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase)
                            && p.PurchaseOrder.DeliveryDate.HasValue
                            && p.PurchaseOrder.DeliveryDate.Value <= requiredByUtc)
                .Sum(p => Math.Max(0m, p.Quantity - p.ReceivedQuantity));

            // Match physical balances via ItemBuyerMaster MaterialCode and GRN material IDs
            List<Guid> matchedMatIds = materials
                .Where(m => string.Equals(m.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Id)
                .Concat(grnBatches
                    .Where(g => string.Equals(g.MaterialCode, materialCode, StringComparison.OrdinalIgnoreCase) && g.MaterialId.HasValue)
                    .Select(g => g.MaterialId!.Value))
                .Distinct()
                .ToList();
            decimal totalOnHand = matchedMatIds.Count > 0
                ? balances.Where(b => matchedMatIds.Contains(b.MaterialId)).Sum(b => b.OnHandQty)
                : 0m;

            decimal eligible = Math.Max(0m, totalOnHand - expiredQty);
            decimal netAvailable = Math.Max(0m, eligible - reserved);

            if (totalOnHand > 0 && eligible == 0 && expiredQty > 0)
                return (0m, incomingBeforeProd, reserved, "EXPIRED");
            if (netAvailable >= requiredQty)
                return (eligible, incomingBeforeProd, reserved, "AVAILABLE");
            if (netAvailable + incomingBeforeProd >= requiredQty)
                return (eligible, incomingBeforeProd, reserved, "EXPECTED_BEFORE_PRODUCTION");
            if (netAvailable > 0m)
                return (eligible, incomingBeforeProd, reserved, "PARTIALLY_AVAILABLE");
            return (eligible, incomingBeforeProd, reserved, "SHORTAGE");
        }

        public async Task<SilaCulinaryProductionPlanDto> SubmitPlanForApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPlan plan = await _repository.SilaCulinaryProductionPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production plan not found.", "Verify the production plan ID.");

            plan.Status = "PENDING_APPROVAL";
            _repository.SilaCulinaryProductionPlan.Update(plan);

            try
            {
                await SilaApprovals.StartAsync(
                    _repository, _logger, buyer.Id,
                    Common.SILA_APPROVAL_TYPE_PRODUCTION_PLAN,
                    Common.SILA_REF_PRODUCTION_PLAN,
                    plan.Id, plan.VersionNumber,
                    new ApprovalScope { PropertyId = plan.PropertyId },
                    cancellationToken);
            }
            catch (Exception)
            {
                await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
                {
                    TaskType = "PRODUCTION_PLAN_APPROVAL",
                    DeduplicationKey = $"PROD_APPROVAL_{plan.PlanCode}_v{plan.VersionNumber}",
                    Title = $"Approve Production Plan {plan.PlanCode} v{plan.VersionNumber}",
                    Priority = SilaOperations.PRIORITY_ACTION,
                    SourceModule = "CULINARY_PRODUCTION",
                    SourceDocumentType = Common.SILA_REF_PRODUCTION_PLAN,
                    SourceDocumentId = plan.Id,
                    SourceDocumentNumber = plan.PlanCode
                }, cancellationToken);
            }
            RecordAudit(buyer.Id, userId, actorName, "PRODUCTION_PLAN_SUBMITTED", Common.SILA_REF_PRODUCTION_PLAN, plan.Id, $"Submitted {plan.PlanCode} for approval");
            await _repository.SaveAsync();
            return await GetPlanAsync(organizationId, roleId, plan.Id, cancellationToken);
        }

        public async Task<SilaCulinaryProductionPlanDto> DecidePlanApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string decision, string? comment, CancellationToken cancellationToken)
        {
            if (!CanApproveProduction(roleId))
                throw new ForBiddenCustomException("Insufficient permission.", "Only authorized culinary or management roles can approve production plans.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPlan plan = await _repository.SilaCulinaryProductionPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production plan not found.", "Verify the production plan ID.");

            string norm = (decision ?? "APPROVE").Trim().ToUpperInvariant();
            bool hasSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ReferenceType == Common.SILA_REF_PRODUCTION_PLAN
                    && x.ReferenceId == plan.Id && x.Version == plan.VersionNumber
                    && x.Status == Common.SILA_APPROVAL_PENDING && x.IsActive)
                .AnyAsync(cancellationToken);
            if (hasSteps)
            {
                ApprovalDecision res = await SilaApprovals.DecideAsync(
                    _repository, _logger, Common.SILA_REF_PRODUCTION_PLAN, plan.Id, plan.VersionNumber,
                    userId, norm == "APPROVE", comment, cancellationToken);
                if (norm == "APPROVE" && res.Outcome != SilaApprovals.OUTCOME_APPROVED)
                {
                    await _repository.SaveAsync();
                    return await GetPlanAsync(organizationId, roleId, plan.Id, cancellationToken);
                }
            }

            if (norm == "APPROVE")
            {
                plan.Status = "APPROVED";
                plan.ApprovedByName = actorName;
                plan.ApprovedAtUtc = DateTime.UtcNow;
            }
            else
            {
                plan.Status = "DRAFT";
            }
            _repository.SilaCulinaryProductionPlan.Update(plan);
            RecordAudit(buyer.Id, userId, actorName, $"PRODUCTION_PLAN_{norm}", Common.SILA_REF_PRODUCTION_PLAN, plan.Id, $"{norm} plan {plan.PlanCode}: {comment}");
            await _repository.SaveAsync();
            return await GetPlanAsync(organizationId, roleId, plan.Id, cancellationToken);
        }

        public async Task<SilaCulinaryProductionPlanDto> AmendReleasedPlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string changeSummary, List<SilaCulinaryCreatePlanLineInputDto> updatedLines, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPlan plan = await _repository.SilaCulinaryProductionPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production plan not found.", "Verify the production plan ID.");

            if (string.IsNullOrWhiteSpace(changeSummary))
                throw new BadRequestCustomException("Amendment reason is required.", "Released production plans cannot be modified without an audit reason.");

            List<SilaCulinaryProductionPlanLine> existingLines = await _repository.SilaCulinaryProductionPlanLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionPlanId == plan.Id && x.IsActive)
                .AsTracking()
                .ToListAsync(cancellationToken);

            // Preserve previous version snapshot cleanly — never silently overwrite
            _repository.SilaCulinaryProductionPlanVersion.Create(new SilaCulinaryProductionPlanVersion
            {
                Id = Guid.NewGuid(),
                ProductionPlanId = plan.Id,
                BuyerId = buyer.Id,
                VersionNumber = plan.VersionNumber,
                ChangeSummary = $"Pre-amendment v{plan.VersionNumber}: {changeSummary}",
                SnapshotJson = JsonSerializer.Serialize(existingLines, CamelJson),
                IsActive = true
            });

            foreach (SilaCulinaryProductionPlanLine old in existingLines)
            {
                old.IsActive = false;
                _repository.SilaCulinaryProductionPlanLine.Update(old);
            }

            List<SilaCulinaryProductionPlanLine> newLines = new();
            foreach (SilaCulinaryCreatePlanLineInputDto inp in updatedLines)
            {
                DateTime reqComp = inp.RequiredCompletionUtc ?? plan.ProductionDateUtc.AddHours(18);
                SilaCulinaryProductionPlanLine nl = new()
                {
                    Id = Guid.NewGuid(),
                    ProductionPlanId = plan.Id,
                    BuyerId = buyer.Id,
                    RecipeId = inp.RecipeId,
                    MenuItemName = inp.MenuItemName.Trim(),
                    RequiredPortions = inp.RequiredPortions,
                    PlannedProductionQty = inp.RequiredPortions,
                    KitchenId = inp.KitchenId,
                    WorkstationId = inp.WorkstationId,
                    PlannedStartUtc = reqComp.AddHours(-3),
                    RequiredCompletionUtc = reqComp,
                    Priority = inp.Priority,
                    IsActive = true
                };
                newLines.Add(nl);
                _repository.SilaCulinaryProductionPlanLine.Create(nl);
            }

            plan.VersionNumber += 1;
            plan.Status = "PENDING_APPROVAL";
            _repository.SilaCulinaryProductionPlan.Update(plan);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_PLAN_AMENDED, Common.SILA_REF_PRODUCTION_PLAN, plan.Id, $"Amended {plan.PlanCode} to v{plan.VersionNumber}: {changeSummary}");
            await _repository.SaveAsync();
            return MapPlan(plan, newLines, CanViewCosts(roleId));
        }

        // -------------------------------------------------------------------------
        // 4A.5 & 4A.6 Production Orders, Batches, Scheduling & Calendar
        // -------------------------------------------------------------------------
        public async Task<SilaCulinaryProductionPlanDto> ReleasePlanToOrdersAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPlan plan = await _repository.SilaCulinaryProductionPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production plan not found.", "Verify the production plan ID.");

            if (plan.Status != "APPROVED" && plan.Status != "INGREDIENTS_VALIDATED")
                throw new BadRequestCustomException($"Plan status '{plan.Status}' cannot be released.", "Approve or validate the plan before releasing production orders.");

            List<SilaCulinaryProductionPlanLine> lines = await _repository.SilaCulinaryProductionPlanLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionPlanId == plan.Id && x.IsActive)
                .AsTracking()
                .ToListAsync(cancellationToken);
            List<Recipe> allRecipes = await _repository.Recipe.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive).Include(x => x.Ingredients).ToListAsync(cancellationToken);
            Dictionary<Guid, Recipe> recipeMap = allRecipes.ToDictionary(r => r.Id, r => r);
            List<SilaCulinaryEquipment> equipmentList = await _repository.SilaCulinaryEquipment.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && !x.UnderMaintenance).ToListAsync(cancellationToken);

            foreach (SilaCulinaryProductionPlanLine line in lines)
            {
                string orderNum = await DocumentNumber.NextAsync(_repository, buyer.Id, "PRD-ORD", 5, cancellationToken);
                Recipe? rec = line.RecipeId.HasValue ? recipeMap.GetValueOrDefault(line.RecipeId.Value) : null;
                string recipeSnapshot = JsonSerializer.Serialize(new
                {
                    RecipeId = rec?.Id,
                    RecipeCode = rec?.RecipeCode ?? line.RecipeCode,
                    Name = rec?.Name ?? line.MenuItemName,
                    Version = rec?.ActiveVersion > 0 ? rec.ActiveVersion : (rec?.Version ?? line.RecipeVersion),
                    ServingQty = rec?.ServingQty ?? 1m,
                    PreparationSteps = rec?.PreparationSteps ?? "Standard culinary SOP",
                    SnapshotCapturedAtUtc = DateTime.UtcNow
                }, CamelJson);

                SilaCulinaryProductionOrder order = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    OrderNumber = orderNum,
                    ProductionPlanId = plan.Id,
                    PropertyId = plan.PropertyId,
                    KitchenId = line.KitchenId,
                    EventId = plan.EventId,
                    RecipeId = line.RecipeId,
                    RecipeVersion = line.RecipeVersion,
                    RecipeName = line.MenuItemName,
                    OrderType = plan.EventId.HasValue ? "EVENT" : "INDIVIDUAL",
                    PlannedOutputQty = line.PlannedProductionQty,
                    OutputUom = line.ProductionUom,
                    ScheduledStartUtc = line.PlannedStartUtc,
                    RequiredCompletionUtc = line.RequiredCompletionUtc,
                    ResponsibleChef = plan.ResponsibleChef,
                    Priority = line.Priority,
                    EstimatedProductionCost = line.EstimatedFoodCost,
                    ApprovalStatus = "APPROVED",
                    ExecutionStatus = "RELEASED",
                    ImmutableRecipeSnapshotJson = recipeSnapshot,
                    IsActive = true
                };
                _repository.SilaCulinaryProductionOrder.Create(order);

                if (rec != null)
                {
                    Dictionary<string, decimal> exploded = new(StringComparer.OrdinalIgnoreCase);
                    ExplodeRecipeRecursive(rec.Id, line.PlannedProductionQty, recipeMap, new HashSet<Guid>(), exploded);
                    foreach (KeyValuePair<string, decimal> kvp in exploded)
                    {
                        _repository.SilaCulinaryProductionOrderIngredient.Create(new SilaCulinaryProductionOrderIngredient
                        {
                            Id = Guid.NewGuid(),
                            ProductionOrderId = order.Id,
                            BuyerId = buyer.Id,
                            MaterialCode = kvp.Key,
                            MaterialName = kvp.Key,
                            GrossRequiredQty = kvp.Value,
                            NetUsableQty = kvp.Value,
                            YieldAdjustedQty = kvp.Value,
                            ReadinessStatus = line.IngredientReadiness,
                            IsActive = true
                        });
                    }
                }

                // Split into equipment-capacity-sized executable batches
                SilaCulinaryEquipment? eq = equipmentList.FirstOrDefault(e => e.KitchenId == line.KitchenId) ?? equipmentList.FirstOrDefault();
                decimal batchCap = eq?.BatchCapacity > 0 ? eq.BatchCapacity : 50m;
                int batchCount = Math.Max(1, (int)Math.Ceiling(line.PlannedProductionQty / batchCap));
                decimal remaining = line.PlannedProductionQty;

                for (int b = 1; b <= batchCount; b++)
                {
                    decimal bQty = b == batchCount ? remaining : Math.Min(batchCap, remaining);
                    remaining -= bQty;
                    string batchNumber = $"{orderNum}-B{b:D2}";
                    DateTime bStart = order.ScheduledStartUtc.AddMinutes((b - 1) * 30);
                    DateTime bFinish = bStart.AddMinutes(45);
                    Guid batchId = Guid.NewGuid();

                    string qrJson = JsonSerializer.Serialize(new
                    {
                        batchId,
                        batchNumber,
                        orderNumber = orderNum,
                        recipeName = order.RecipeName,
                        recipeVersion = order.RecipeVersion,
                        plannedQty = bQty,
                        uom = order.OutputUom,
                        chef = order.ResponsibleChef,
                        productionDate = plan.ProductionDateUtc.ToString("yyyy-MM-dd"),
                        useByUtc = bFinish.AddHours(24)
                    }, CamelJson);

                    _repository.SilaCulinaryProductionBatch.Create(new SilaCulinaryProductionBatch
                    {
                        Id = batchId,
                        BuyerId = buyer.Id,
                        ProductionOrderId = order.Id,
                        BatchNumber = batchNumber,
                        BatchType = "EQUIPMENT_BASED",
                        PlannedBatchQty = bQty,
                        ProductionUom = order.OutputUom,
                        KitchenId = line.KitchenId,
                        WorkstationId = line.WorkstationId ?? eq?.WorkstationId,
                        EquipmentId = eq?.Id,
                        AssignedChefName = order.ResponsibleChef,
                        PlannedStartUtc = bStart,
                        PlannedFinishUtc = bFinish,
                        BatchPriority = order.Priority,
                        QualityStatus = "PENDING",
                        BatchStatus = "RELEASED",
                        UseByUtc = bFinish.AddHours(24),
                        QrPayloadJson = qrJson,
                        RecipeSnapshotJson = recipeSnapshot,
                        IsActive = true
                    });

                    // Create sequenced tasks
                    Guid prepId = Guid.NewGuid();
                    Guid cookId = Guid.NewGuid();
                    Guid qcId = Guid.NewGuid();
                    _repository.SilaCulinaryProductionTask.Create(new SilaCulinaryProductionTask
                    {
                        Id = prepId, ProductionBatchId = batchId, BuyerId = buyer.Id, SequenceOrder = 1,
                        TaskType = "INGREDIENT_PREP", Instructions = $"Mise-en-place & portion prep for {order.RecipeName}",
                        WorkstationId = line.WorkstationId, PlannedDurationMinutes = 20, Status = "PENDING", IsActive = true
                    });
                    _repository.SilaCulinaryProductionTask.Create(new SilaCulinaryProductionTask
                    {
                        Id = cookId, ProductionBatchId = batchId, BuyerId = buyer.Id, SequenceOrder = 2, DependsOnTaskId = prepId,
                        TaskType = "COOKING", Instructions = $"Cook / assemble batch {batchNumber} per approved SOP",
                        EquipmentId = eq?.Id, PlannedDurationMinutes = 25, Status = "PENDING", IsActive = true
                    });
                    _repository.SilaCulinaryProductionTask.Create(new SilaCulinaryProductionTask
                    {
                        Id = qcId, ProductionBatchId = batchId, BuyerId = buyer.Id, SequenceOrder = 3, DependsOnTaskId = cookId,
                        TaskType = "QUALITY_CHECK", Instructions = $"Verify temperature & sensory quality checkpoint for {batchNumber}",
                        PlannedDurationMinutes = 5, Status = "PENDING", IsActive = true
                    });
                }

                line.LineStatus = "RELEASED";
                _repository.SilaCulinaryProductionPlanLine.Update(line);
                RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_ORDER_RELEASED, Common.SILA_REF_PRODUCTION_ORDER, order.Id, $"Released production order {order.OrderNumber} ({batchCount} batch(es))");
            }

            plan.Status = "RELEASED";
            _repository.SilaCulinaryProductionPlan.Update(plan);
            await _repository.SaveAsync();
            return MapPlan(plan, lines, CanViewCosts(roleId));
        }

        public async Task<List<SilaCulinaryProductionOrderDto>> ListOrdersAsync(Guid organizationId, Guid roleId, Guid? kitchenId, string? status, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionOrder> q = _repository.SilaCulinaryProductionOrder.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (kitchenId.HasValue) q = q.Where(x => x.KitchenId == kitchenId);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.ExecutionStatus == status.Trim().ToUpperInvariant());

            List<SilaCulinaryProductionOrder> orders = await q.OrderBy(x => x.RequiredCompletionUtc).ToListAsync(cancellationToken);
            List<Guid> orderIds = orders.Select(o => o.Id).ToList();
            List<SilaCulinaryProductionOrderIngredient> allIngs = orderIds.Count == 0
                ? new()
                : await _repository.SilaCulinaryProductionOrderIngredient.FindByCondition(x => x.BuyerId == buyer.Id && orderIds.Contains(x.ProductionOrderId) && x.IsActive).ToListAsync(cancellationToken);
            List<SilaCulinaryProductionBatch> allBatches = orderIds.Count == 0
                ? new()
                : await _repository.SilaCulinaryProductionBatch.FindByCondition(x => x.BuyerId == buyer.Id && orderIds.Contains(x.ProductionOrderId) && x.IsActive).ToListAsync(cancellationToken);

            bool showCosts = CanViewCosts(roleId);
            return orders.Select(o => MapOrder(o, allIngs.Where(i => i.ProductionOrderId == o.Id).ToList(), allBatches.Where(b => b.ProductionOrderId == o.Id).ToList(), showCosts)).ToList();
        }

        public async Task<SilaCulinaryProductionBatchDto> RescheduleBatchAsync(Guid organizationId, Guid userId, string actorName, Guid batchId, DateTime plannedStartUtc, DateTime plannedFinishUtc, Guid? equipmentId, Guid? workstationId, CancellationToken cancellationToken)
        {
            if (plannedFinishUtc <= plannedStartUtc)
                throw new BadRequestCustomException("Invalid schedule window.", "Planned finish time must be after planned start time.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionBatch batch = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == batchId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production batch not found.", "Verify batch ID.");

            Guid? targetEq = equipmentId ?? batch.EquipmentId;
            if (targetEq.HasValue)
            {
                bool eqConflict = await _repository.SilaCulinaryProductionBatch
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id != batch.Id && x.IsActive
                                          && x.BatchStatus != "CANCELLED" && x.BatchStatus != "COMPLETED"
                                          && x.EquipmentId == targetEq.Value
                                          && x.PlannedStartUtc < plannedFinishUtc
                                          && x.PlannedFinishUtc > plannedStartUtc)
                    .AnyAsync(cancellationToken);
                if (eqConflict)
                    throw new BadRequestCustomException("Equipment scheduling conflict detected.", "Selected equipment is already reserved during that time window.");
            }

            batch.PlannedStartUtc = plannedStartUtc;
            batch.PlannedFinishUtc = plannedFinishUtc;
            if (equipmentId.HasValue) batch.EquipmentId = equipmentId;
            if (workstationId.HasValue) batch.WorkstationId = workstationId;
            _repository.SilaCulinaryProductionBatch.Update(batch);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_RESCHEDULED, Common.SILA_REF_PRODUCTION_BATCH, batch.Id, $"Rescheduled batch {batch.BatchNumber} to {plannedStartUtc:O} - {plannedFinishUtc:O}");
            await _repository.SaveAsync();
            return MapBatch(batch);
        }

        public async Task<SilaCulinaryScheduleOptimizeResponseDto> OptimizeScheduleAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryScheduleOptimizeRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            List<SilaCulinaryEquipment> equipment = await _repository.SilaCulinaryEquipment.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(cancellationToken);
            List<SilaCulinaryStaffAssignment> staff = await _repository.SilaCulinaryStaffAssignment.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.IsAvailable).ToListAsync(cancellationToken);
            List<SilaCulinaryProductionBatch> batches = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.BatchStatus != "COMPLETED" && x.BatchStatus != "CANCELLED")
                .OrderBy(x => x.PlannedFinishUtc)
                .ToListAsync(cancellationToken);

            if (batches.Count == 0)
            {
                return new SilaCulinaryScheduleOptimizeResponseDto
                {
                    Status = "OPTIMAL",
                    Assumptions = new List<string> { "No open production batches found for scheduling." }
                };
            }

            DateTime horizonStart = batches.Min(b => b.PlannedStartUtc);
            try
            {
                string? engineUrl = _configuration[Common.CULINARY_ENGINE_URL];
                if (!string.IsNullOrWhiteSpace(engineUrl))
                {
                    HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                    var payload = new
                    {
                        horizon_start_minute = 360,
                        horizon_end_minute = 1320,
                        objective = request.Objective,
                        enforce_food_safety = true,
                        equipment = equipment.Select(e => new
                        {
                            equipment_id = e.Id.ToString(),
                            equipment_name = e.EquipmentName,
                            kitchen_id = e.KitchenId.ToString(),
                            batch_capacity = (double)e.BatchCapacity,
                            setup_minutes = e.SetupMinutes,
                            cleaning_minutes = e.CleaningMinutes,
                            available_from_minute = e.AvailableFromMinute,
                            available_until_minute = e.AvailableUntilMinute,
                            under_maintenance = e.UnderMaintenance,
                            allergen_segregated = e.AllergenSegregated
                        }).ToList(),
                        staff = staff.Select(s => new
                        {
                            staff_id = s.UserId.ToString(),
                            staff_name = s.StaffName,
                            kitchen_id = s.KitchenId.ToString(),
                            role = s.CulinaryRole,
                            available_from_minute = s.AvailableFromMinute,
                            available_until_minute = s.AvailableUntilMinute
                        }).ToList(),
                        batches = batches.Select(b => new
                        {
                            batch_id = b.Id.ToString(),
                            batch_number = b.BatchNumber,
                            recipe_name = b.BatchNumber,
                            kitchen_id = (b.KitchenId ?? Guid.Empty).ToString(),
                            planned_qty = (double)b.PlannedBatchQty,
                            prep_minutes = 15,
                            cook_minutes = 30,
                            cooling_minutes = 0,
                            service_deadline_minute = Math.Max(420, Math.Min(1320, 360 + (int)(b.PlannedFinishUtc - horizonStart).TotalMinutes)),
                            ingredient_ready_minute = 360,
                            max_holding_minutes = 120,
                            priority = b.BatchPriority
                        }).ToList()
                    };

                    using HttpResponseMessage resp = await client.PostAsJsonAsync($"{engineUrl.TrimEnd('/')}/v1/production/schedule", payload, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        JsonElement body = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                        SilaCulinaryScheduleOptimizeResponseDto mapped = new()
                        {
                            Status = body.TryGetProperty("status", out JsonElement st) ? st.GetString() ?? "OPTIMAL" : "OPTIMAL",
                            SolverUsed = body.TryGetProperty("solver_used", out JsonElement su) ? su.GetString() ?? "scipy_highs_milp" : "scipy_highs_milp"
                        };
                        if (body.TryGetProperty("assignments", out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement item in arr.EnumerateArray())
                            {
                                Guid bid = Guid.Parse(item.GetProperty("batch_id").GetString()!);
                                int sMin = item.GetProperty("start_minute").GetInt32();
                                int eMin = item.GetProperty("end_minute").GetInt32();
                                mapped.Assignments.Add(new SilaCulinaryScheduledBatchDto
                                {
                                    BatchId = bid,
                                    BatchNumber = item.GetProperty("batch_number").GetString() ?? string.Empty,
                                    RecipeName = item.GetProperty("recipe_name").GetString() ?? string.Empty,
                                    AssignedEquipmentId = item.TryGetProperty("assigned_equipment_id", out JsonElement eqEl) ? eqEl.GetString() : null,
                                    AssignedStaffId = item.TryGetProperty("assigned_staff_id", out JsonElement sfEl) ? sfEl.GetString() : null,
                                    PlannedStartUtc = horizonStart.Date.AddMinutes(sMin),
                                    PlannedFinishUtc = horizonStart.Date.AddMinutes(eMin),
                                    DelayMinutes = item.GetProperty("delay_minutes").GetInt32(),
                                    HoldingBufferMinutes = item.GetProperty("holding_buffer_minutes").GetInt32()
                                });
                            }
                        }
                        if (body.TryGetProperty("bottlenecks", out JsonElement bks))
                            mapped.Bottlenecks = bks.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
                        if (body.TryGetProperty("conflicting_constraints", out JsonElement cc))
                            mapped.ConflictingConstraints = cc.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
                        if (body.TryGetProperty("assumptions", out JsonElement asmp))
                            mapped.Assumptions = asmp.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();
                        if (body.TryGetProperty("alternative_recommendations", out JsonElement alt))
                            mapped.AlternativeRecommendations = alt.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToList();

                        RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_SCHEDULED, Common.SILA_REF_PRODUCTION_BATCH, null, $"Ran Python AI production scheduler ({mapped.Status}) across {batches.Count} batches");
                        await _repository.SaveAsync();
                        return mapped;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Python production scheduler fallback engaged: {ex.Message}");
            }

            // Deterministic safety-preserving fallback schedule
            SilaCulinaryScheduleOptimizeResponseDto fallback = new()
            {
                Status = "FEASIBLE",
                SolverUsed = "deterministic_safety_scheduler",
                Assumptions = new List<string> { "Fallback deterministic scheduler preserving equipment capacity and mandatory food safety windows." }
            };
            int cursor = 360;
            foreach (SilaCulinaryProductionBatch b in batches)
            {
                SilaCulinaryEquipment? eq = equipment.FirstOrDefault(e => e.Id == b.EquipmentId && !e.UnderMaintenance)
                    ?? equipment.FirstOrDefault(e => e.KitchenId == b.KitchenId && !e.UnderMaintenance && e.BatchCapacity >= b.PlannedBatchQty);
                if (eq == null && equipment.Any())
                {
                    fallback.ConflictingConstraints.Add($"Batch {b.BatchNumber}: no active equipment satisfies capacity {b.PlannedBatchQty:N0}.");
                    fallback.Status = "INFEASIBLE";
                }
                fallback.Assignments.Add(new SilaCulinaryScheduledBatchDto
                {
                    BatchId = b.Id,
                    BatchNumber = b.BatchNumber,
                    RecipeName = b.BatchNumber,
                    AssignedEquipmentId = eq?.Id.ToString(),
                    AssignedStaffId = staff.FirstOrDefault(s => s.KitchenId == b.KitchenId)?.UserId.ToString(),
                    PlannedStartUtc = horizonStart.Date.AddMinutes(cursor),
                    PlannedFinishUtc = horizonStart.Date.AddMinutes(cursor + 45),
                    DelayMinutes = 0,
                    HoldingBufferMinutes = 30
                });
                cursor += 45;
            }
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_SCHEDULED, Common.SILA_REF_PRODUCTION_BATCH, null, $"Executed deterministic scheduler ({fallback.Status})");
            await _repository.SaveAsync();
            return fallback;
        }

        public async Task<List<SilaCulinaryProductionBatchDto>> ApplyScheduleAsync(Guid organizationId, Guid userId, string actorName, List<SilaCulinaryScheduledBatchDto> assignments, CancellationToken cancellationToken)
        {
            List<SilaCulinaryProductionBatchDto> updated = new();
            foreach (SilaCulinaryScheduledBatchDto item in assignments)
            {
                Guid? eqId = Guid.TryParse(item.AssignedEquipmentId, out Guid eParsed) ? eParsed : null;
                updated.Add(await RescheduleBatchAsync(organizationId, userId, actorName, item.BatchId, item.PlannedStartUtc, item.PlannedFinishUtc, eqId, null, cancellationToken));
            }
            return updated;
        }

        public async Task<List<SilaCulinaryProductionBatchDto>> GetProductionCalendarAsync(Guid organizationId, DateTime? fromUtc, DateTime? toUtc, Guid? kitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionBatch> q = _repository.SilaCulinaryProductionBatch.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (fromUtc.HasValue) q = q.Where(x => x.PlannedFinishUtc >= fromUtc.Value);
            if (toUtc.HasValue) q = q.Where(x => x.PlannedStartUtc <= toUtc.Value);
            if (kitchenId.HasValue) q = q.Where(x => x.KitchenId == kitchenId.Value);
            List<SilaCulinaryProductionBatch> list = await q.OrderBy(x => x.PlannedStartUtc).ToListAsync(cancellationToken);
            return list.Select(MapBatch).ToList();
        }

        // -------------------------------------------------------------------------
        // 4A.7 Ingredient Reservation, FEFO/FIFO Picking & Material Issue
        // -------------------------------------------------------------------------
        public async Task<List<SilaCulinaryProductionPickingLineDto>> GeneratePickingListAsync(Guid organizationId, Guid userId, string actorName, Guid orderId, string strategy, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionOrder order = await _repository.SilaCulinaryProductionOrder
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == orderId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production order not found.", "Verify production order ID.");

            List<SilaCulinaryProductionPickingLine> existing = await _repository.SilaCulinaryProductionPickingLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == order.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            if (existing.Count > 0)
                return existing.Select(MapPickingLine).ToList();

            List<SilaCulinaryProductionOrderIngredient> ingredients = await _repository.SilaCulinaryProductionOrderIngredient
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == order.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<GoodsReceiptItem> grnBatches = await _repository.GoodsReceiptItem
                .FindByCondition(x => x.GoodsReceipt.BuyerId == buyer.Id && x.IsActive && (!x.ExpiryDate.HasValue || x.ExpiryDate.Value > DateTime.UtcNow))
                .Include(x => x.GoodsReceipt)
                .ToListAsync(cancellationToken);

            string normStrategy = string.Equals(strategy, "FIFO", StringComparison.OrdinalIgnoreCase) ? "FIFO" : "FEFO";
            List<SilaCulinaryProductionPickingLine> created = new();

            foreach (SilaCulinaryProductionOrderIngredient ing in ingredients)
            {
                GoodsReceiptItem? bestBatch = normStrategy == "FEFO"
                    ? grnBatches.Where(g => string.Equals(g.MaterialCode, ing.MaterialCode, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(g => g.ExpiryDate ?? DateTime.MaxValue)
                                .ThenBy(g => g.DateCreated)
                                .FirstOrDefault()
                    : grnBatches.Where(g => string.Equals(g.MaterialCode, ing.MaterialCode, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(g => g.DateCreated)
                                .FirstOrDefault();

                SilaCulinaryProductionPickingLine pl = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ProductionOrderId = order.Id,
                    MaterialId = ing.MaterialId ?? bestBatch?.MaterialId,
                    MaterialCode = ing.MaterialCode,
                    MaterialName = ing.MaterialName,
                    RequiredQty = ing.YieldAdjustedQty,
                    ReservedQty = ing.YieldAdjustedQty,
                    PickedQty = 0m,
                    InventoryUom = ing.InventoryUom,
                    InventoryBatchNumber = bestBatch?.BatchNumber ?? "UNBATCHED-ELIGIBLE",
                    ExpiryUtc = bestBatch?.ExpiryDate,
                    PickingStrategy = normStrategy,
                    Status = "OPEN",
                    IsActive = true
                };
                created.Add(pl);
                _repository.SilaCulinaryProductionPickingLine.Create(pl);
            }

            order.ExecutionStatus = "INGREDIENTS_RESERVED";
            _repository.SilaCulinaryProductionOrder.Update(order);
            await _repository.SaveAsync();
            return created.Select(MapPickingLine).ToList();
        }

        public async Task<SilaCulinaryProductionPickingLineDto> ConfirmPickingLineAsync(Guid organizationId, Guid userId, string actorName, Guid pickingLineId, decimal pickedQty, string? inventoryBatchNumber, CancellationToken cancellationToken)
        {
            if (pickedQty <= 0)
                throw new BadRequestCustomException("Picked quantity must be positive.", "Enter a valid picked quantity.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionPickingLine line = await _repository.SilaCulinaryProductionPickingLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == pickingLineId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Picking line not found.", "Verify picking line ID.");

            if (line.ExpiryUtc.HasValue && line.ExpiryUtc.Value <= DateTime.UtcNow)
                throw new BadRequestCustomException("Cannot pick expired inventory batch.", "Select a valid non-expired FEFO batch.");

            line.PickedQty = pickedQty;
            if (!string.IsNullOrWhiteSpace(inventoryBatchNumber))
                line.InventoryBatchNumber = inventoryBatchNumber.Trim();
            line.Status = pickedQty >= line.RequiredQty ? "PICKED" : "PARTIAL";
            _repository.SilaCulinaryProductionPickingLine.Update(line);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_PICKED, Common.SILA_REF_PRODUCTION_ORDER, line.ProductionOrderId, $"Confirmed picking {pickedQty:N2} {line.InventoryUom} of {line.MaterialCode} ({line.InventoryBatchNumber})");
            await _repository.SaveAsync();
            return MapPickingLine(line);
        }

        public async Task<SilaCulinaryProductionMaterialIssueRef> IssueMaterialAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryIssueMaterialRequestDto request, CancellationToken cancellationToken)
        {
            if (request.IssueQty <= 0)
                throw new BadRequestCustomException("Issue quantity must be greater than zero.", "Provide a positive quantity to issue.");
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                throw new BadRequestCustomException("IdempotencyKey is required.", "Provide a unique IdempotencyKey to prevent duplicate inventory postings.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string idem = request.IdempotencyKey.Trim();

            // Strict idempotency guard preventing duplicate stock postings
            SilaCulinaryProductionMaterialIssueRef? existing = await _repository.SilaCulinaryProductionMaterialIssueRef
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == idem && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
                return existing;

            string issueNumber = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.GOODS_ISSUE, 6, cancellationToken);
            Guid? goodsIssueId = null;

            InventoryLocation? fromLoc = await _repository.InventoryLocation.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.FromStoreLocationId && x.IsActive).FirstOrDefaultAsync(cancellationToken);
            InventoryLocation? toLoc = await _repository.InventoryLocation.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.ToKitchenLocationId && x.IsActive).FirstOrDefaultAsync(cancellationToken);
            ItemBuyerMaster? mat = await _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialCode == request.MaterialCode && x.IsActive).FirstOrDefaultAsync(cancellationToken);

            if (fromLoc != null && toLoc != null && mat != null)
            {
                GoodsIssue gi = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    IssueNumber = issueNumber,
                    FromLocationId = fromLoc.Id,
                    ToLocationId = toLoc.Id,
                    IssuedBy = userId,
                    Comment = request.Comment ?? $"Production Issue for Order {request.ProductionOrderId}",
                    IsActive = true
                };
                _repository.GoodsIssue.Create(gi);
                _repository.GoodsIssueItem.Create(new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    GoodsIssueId = gi.Id,
                    MaterialId = mat.Id,
                    MaterialCode = mat.MaterialCode ?? request.MaterialCode,
                    MaterialName = mat.Description ?? request.MaterialCode,
                    Quantity = request.IssueQty,
                    Uom = request.Uom,
                    BaseQuantity = request.IssueQty,
                    IsActive = true
                });

                InventoryLedger ledger = new(_repository, buyer.Id, userId);
                await ledger.PostAsync(new InventoryMovement
                {
                    LocationId = fromLoc.Id, Material = mat, Direction = Common.SILA_DIRECTION_OUT,
                    TransactionType = Common.SILA_TXN_GOODS_ISSUE_OUT, BaseQuantity = request.IssueQty,
                    EnteredQuantity = request.IssueQty, EnteredUom = request.Uom,
                    ReferenceType = Common.SILA_REF_GOODS_ISSUE, ReferenceId = gi.Id, ReferenceNumber = gi.IssueNumber,
                    Reason = request.Comment
                }, cancellationToken);
                await ledger.PostAsync(new InventoryMovement
                {
                    LocationId = toLoc.Id, Material = mat, Direction = Common.SILA_DIRECTION_IN,
                    TransactionType = Common.SILA_TXN_GOODS_ISSUE_IN, BaseQuantity = request.IssueQty,
                    EnteredQuantity = request.IssueQty, EnteredUom = request.Uom,
                    ReferenceType = Common.SILA_REF_GOODS_ISSUE, ReferenceId = gi.Id, ReferenceNumber = gi.IssueNumber,
                    Reason = request.Comment
                }, cancellationToken);
                ledger.QueueErpPosting(Common.SILA_REF_GOODS_ISSUE, gi.Id, gi.IssueNumber, fromLoc.Id, Common.SILA_MOVEMENT_GOODS_ISSUE);
                goodsIssueId = gi.Id;
            }

            SilaCulinaryProductionMaterialIssueRef issueRef = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProductionOrderId = request.ProductionOrderId,
                ProductionBatchId = request.ProductionBatchId,
                GoodsIssueId = goodsIssueId,
                IssueNumber = issueNumber,
                MaterialCode = request.MaterialCode,
                IssuedQty = request.IssueQty,
                IssuedUom = request.Uom,
                FromStoreLocationId = request.FromStoreLocationId,
                ToKitchenId = request.ToKitchenLocationId,
                IdempotencyKey = idem,
                IssuedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryProductionMaterialIssueRef.Create(issueRef);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_ISSUED, Common.SILA_REF_PRODUCTION_ORDER, request.ProductionOrderId, $"Issued {request.IssueQty:N2} {request.Uom} of {request.MaterialCode} ({issueNumber})");
            await _repository.SaveAsync();
            return issueRef;
        }

        // -------------------------------------------------------------------------
        // 4A.8 & 4A.9 KDS, Execution, Tasks, Quality Checkpoints & Traceability
        // -------------------------------------------------------------------------
        public async Task<List<SilaCulinaryProductionBatchDto>> GetKdsFeedAsync(Guid organizationId, Guid? kitchenId, string? viewMode, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionBatch> q = _repository.SilaCulinaryProductionBatch.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (kitchenId.HasValue) q = q.Where(x => x.KitchenId == kitchenId.Value);

            List<SilaCulinaryProductionBatch> list = await q.OrderBy(x => x.PlannedFinishUtc).ToListAsync(cancellationToken);
            string vm = (viewMode ?? "ALL").Trim().ToUpperInvariant();
            DateTime now = DateTime.UtcNow;
            IEnumerable<SilaCulinaryProductionBatch> filtered = vm switch
            {
                "ACTIVE" => list.Where(b => b.BatchStatus is "PREPARING" or "COOKING" or "PAUSED"),
                "UPCOMING" => list.Where(b => b.BatchStatus is "CREATED" or "RELEASED"),
                "DELAYED" => list.Where(b => b.BatchStatus != "COMPLETED" && b.BatchStatus != "CANCELLED" && b.PlannedFinishUtc < now),
                "QUALITY_PENDING" => list.Where(b => b.QualityStatus == "PENDING" || b.BatchStatus == "QUALITY_CHECK" || b.BatchStatus == "ON_HOLD"),
                "READY" => list.Where(b => b.BatchStatus == "READY_FOR_DISPATCH"),
                "COMPLETED" => list.Where(b => b.BatchStatus == "COMPLETED"),
                _ => list
            };
            return filtered.Select(MapBatch).ToList();
        }

        public async Task<SilaCulinaryProductionBatchDto> ExecuteBatchActionAsync(Guid organizationId, Guid userId, string actorName, Guid batchId, string action, decimal? actualOutputQty, string? note, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionBatch batch = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == batchId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production batch not found.", "Verify batch ID.");

            string act = (action ?? string.Empty).Trim().ToUpperInvariant();
            if (batch.BatchStatus == "ON_HOLD" && act == "COMPLETE_BATCH")
                throw new BadRequestCustomException("Batch is ON_HOLD due to failed quality checkpoint.", "Authorized supervisor corrective action approval is required before completing this batch.");

            switch (act)
            {
                case "START_PREP":
                    batch.BatchStatus = "PREPARING";
                    batch.ActualStartUtc ??= DateTime.UtcNow;
                    break;
                case "START_COOKING":
                    batch.BatchStatus = "COOKING";
                    batch.ActualStartUtc ??= DateTime.UtcNow;
                    break;
                case "PAUSE":
                    batch.BatchStatus = "PAUSED";
                    break;
                case "RESUME":
                    batch.BatchStatus = "COOKING";
                    break;
                case "READY_FOR_QUALITY":
                    batch.BatchStatus = "QUALITY_CHECK";
                    break;
                case "COMPLETE_BATCH":
                    List<SilaCulinaryProductionQualityResult> qResults = await _repository.SilaCulinaryProductionQualityResult
                        .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionBatchId == batch.Id && x.IsActive)
                        .ToListAsync(cancellationToken);
                    if (qResults.Count == 0)
                        throw new BadRequestCustomException("Mandatory quality checkpoint not recorded.", "Record at least one passing quality checkpoint before completing the batch.");
                    if (qResults.Any(q => !q.Passed && !q.SupervisorApproved))
                        throw new BadRequestCustomException("Unresolved failed quality checkpoint blocks completion.", "Obtain supervisor corrective action approval first.");

                    batch.BatchStatus = "COMPLETED";
                    batch.QualityStatus = "PASSED";
                    batch.ActualFinishUtc = DateTime.UtcNow;
                    batch.ActualOutputQty = actualOutputQty ?? batch.PlannedBatchQty;
                    RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_BATCH_COMPLETED, Common.SILA_REF_PRODUCTION_BATCH, batch.Id, $"Completed batch {batch.BatchNumber} with output {batch.ActualOutputQty:N2} {batch.ProductionUom}");
                    break;
                default:
                    throw new BadRequestCustomException($"Unsupported batch action '{act}'.", "Use START_PREP, START_COOKING, PAUSE, RESUME, READY_FOR_QUALITY, or COMPLETE_BATCH.");
            }

            _repository.SilaCulinaryProductionBatch.Update(batch);
            await _repository.SaveAsync();
            return MapBatch(batch);
        }

        public async Task<List<SilaCulinaryProductionTaskDto>> ListBatchTasksAsync(Guid organizationId, Guid batchId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            List<SilaCulinaryProductionTask> tasks = await _repository.SilaCulinaryProductionTask
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionBatchId == batchId && x.IsActive)
                .OrderBy(x => x.SequenceOrder)
                .ToListAsync(cancellationToken);
            return tasks.Select(MapTask).ToList();
        }

        public async Task<SilaCulinaryProductionTaskDto> UpdateTaskStatusAsync(Guid organizationId, Guid userId, Guid taskId, string status, string? exceptionReason, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionTask task = await _repository.SilaCulinaryProductionTask
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == taskId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production task not found.", "Verify task ID.");

            string norm = (status ?? "IN_PROGRESS").Trim().ToUpperInvariant();
            if ((norm == "IN_PROGRESS" || norm == "COMPLETED") && task.DependsOnTaskId.HasValue)
            {
                SilaCulinaryProductionTask? pred = await _repository.SilaCulinaryProductionTask
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == task.DependsOnTaskId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (pred != null && pred.Status != "COMPLETED")
                    throw new BadRequestCustomException($"Predecessor task '{pred.TaskType}' must complete first.", "Mandatory recipe task sequencing cannot be bypassed.");
            }

            if (norm == "IN_PROGRESS") task.ActualStartUtc ??= DateTime.UtcNow;
            if (norm == "COMPLETED")
            {
                task.ActualStartUtc ??= DateTime.UtcNow;
                task.ActualCompletionUtc = DateTime.UtcNow;
                task.ActualDurationMinutes = Math.Max(1, (int)(task.ActualCompletionUtc.Value - task.ActualStartUtc.Value).TotalMinutes);
            }
            task.Status = norm;
            task.ExceptionReason = exceptionReason;
            _repository.SilaCulinaryProductionTask.Update(task);
            await _repository.SaveAsync();
            return MapTask(task);
        }

        public async Task<SilaCulinaryQualityResultDto> RecordQualityCheckAsync(Guid organizationId, Guid userId, string actorName, SilaCulinaryQualityRecordRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionBatch batch = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.ProductionBatchId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production batch not found.", "Verify batch ID.");

            SilaCulinaryProductionQualityResult result = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProductionBatchId = batch.Id,
                CheckpointId = request.CheckpointId,
                CheckpointName = request.CheckpointName.Trim(),
                ExpectedValue = request.ExpectedValue,
                ActualValue = request.ActualValue,
                MeasurementUom = request.MeasurementUom,
                Passed = request.Passed,
                RecordedByName = actorName,
                RecordedAtUtc = DateTime.UtcNow,
                Comments = request.Comments,
                CorrectiveAction = request.CorrectiveAction,
                SupervisorApproved = request.Passed,
                IsActive = true
            };
            _repository.SilaCulinaryProductionQualityResult.Create(result);

            if (!request.Passed)
            {
                batch.BatchStatus = "ON_HOLD";
                batch.QualityStatus = "FAILED_HOLD";
                _repository.SilaCulinaryProductionBatch.Update(batch);

                string excCode = $"QCH-{batch.BatchNumber}-{DateTime.UtcNow:HHmmss}";
                _repository.SilaCulinaryProductionException.Create(new SilaCulinaryProductionException
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ExceptionCode = excCode,
                    ProductionOrderId = batch.ProductionOrderId,
                    ProductionBatchId = batch.Id,
                    ExceptionType = "QUALITY_CHECKPOINT_FAILURE",
                    Severity = "CRITICAL",
                    Description = $"Failed quality checkpoint '{request.CheckpointName}': expected {request.ExpectedValue}, actual {request.ActualValue} {request.MeasurementUom}.",
                    ReportedByName = actorName,
                    RecommendedAction = "Review food safety deviation and document supervisor corrective action.",
                    ResolutionStatus = "OPEN",
                    IsActive = true
                });
            }
            else
            {
                batch.QualityStatus = "PASSED";
                _repository.SilaCulinaryProductionBatch.Update(batch);
            }

            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_QUALITY_RECORDED, Common.SILA_REF_PRODUCTION_QUALITY, result.Id, $"Quality check '{request.CheckpointName}' for {batch.BatchNumber}: {(request.Passed ? "PASS" : "FAIL -> ON_HOLD")}");
            await _repository.SaveAsync();
            return MapQualityResult(result);
        }

        public async Task<SilaCulinaryQualityResultDto> ResolveQualityHoldAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid qualityResultId, string correctiveAction, bool approveRelease, CancellationToken cancellationToken)
        {
            if (!CanApproveProduction(roleId))
                throw new ForBiddenCustomException("Supervisor authorization required.", "Only authorized supervisors can resolve failed food safety checkpoints.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionQualityResult qr = await _repository.SilaCulinaryProductionQualityResult
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == qualityResultId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Quality result not found.", "Verify quality result ID.");

            qr.CorrectiveAction = correctiveAction;
            qr.SupervisorApproved = approveRelease;
            _repository.SilaCulinaryProductionQualityResult.Update(qr);

            if (approveRelease)
            {
                SilaCulinaryProductionBatch? batch = await _repository.SilaCulinaryProductionBatch
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == qr.ProductionBatchId && x.IsActive)
                    .AsTracking()
                    .FirstOrDefaultAsync(cancellationToken);
                if (batch != null)
                {
                    batch.BatchStatus = "READY_FOR_DISPATCH";
                    batch.QualityStatus = "PASSED";
                    _repository.SilaCulinaryProductionBatch.Update(batch);
                }
            }

            await _repository.SaveAsync();
            return MapQualityResult(qr);
        }

        public async Task<SilaCulinaryProductionBatchTraceabilityDto> GetBatchTraceabilityAsync(Guid organizationId, Guid batchId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionBatch batch = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == batchId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Batch not found.", "Verify batch ID.");
            SilaCulinaryProductionOrder? order = await _repository.SilaCulinaryProductionOrder
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == batch.ProductionOrderId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            List<SilaCulinaryProductionPickingLine> pickLines = await _repository.SilaCulinaryProductionPickingLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionOrderId == batch.ProductionOrderId && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProductionDispatch> dispatches = await _repository.SilaCulinaryProductionDispatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionBatchId == batch.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProductionQualityResult> qc = await _repository.SilaCulinaryProductionQualityResult
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionBatchId == batch.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<string> ingBatches = pickLines.Select(p => $"{p.MaterialCode} [Batch: {p.InventoryBatchNumber ?? "N/A"}, Exp: {p.ExpiryUtc?.ToString("yyyy-MM-dd") ?? "N/A"}]").ToList();
            return new SilaCulinaryProductionBatchTraceabilityDto
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                RecipeName = order?.RecipeName ?? batch.BatchNumber,
                RecipeVersion = order?.RecipeVersion ?? 1,
                QrPayloadJson = batch.QrPayloadJson,
                BackwardTraceSummary = $"Batch {batch.BatchNumber} <- Order {order?.OrderNumber ?? "N/A"} <- Recipe v{order?.RecipeVersion ?? 1} <- {ingBatches.Count} ingredient batch(es)",
                ForwardTraceSummary = $"Batch {batch.BatchNumber} -> {dispatches.Count} outlet dispatch(es) ({dispatches.Sum(d => d.DispatchedQty):N2} dispatched)",
                IngredientBatches = ingBatches,
                OutletDispatches = dispatches.Select(MapDispatch).ToList(),
                QualityResults = qc.Select(MapQualityResult).ToList()
            };
        }

        // -------------------------------------------------------------------------
        // 4A.10 Central Kitchen, Buffet & Event Production
        // -------------------------------------------------------------------------
        public async Task<List<SilaCulinaryProductionDispatchDto>> ListCentralKitchenDispatchesAsync(Guid organizationId, Guid? centralKitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionDispatch> q = _repository.SilaCulinaryProductionDispatch.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (centralKitchenId.HasValue) q = q.Where(x => x.CentralKitchenId == centralKitchenId.Value);
            List<SilaCulinaryProductionDispatch> rows = await q.OrderByDescending(x => x.DateCreated).ToListAsync(cancellationToken);
            return rows.Select(MapDispatch).ToList();
        }

        public async Task<SilaCulinaryProductionDispatchDto> AllocateCentralKitchenDispatchAsync(Guid organizationId, Guid userId, string actorName, Guid centralKitchenId, Guid batchId, Guid destinationOutletId, string destinationOutletName, decimal requestedQty, DateTime? requiredDeliveryUtc, CancellationToken cancellationToken)
        {
            if (requestedQty <= 0)
                throw new BadRequestCustomException("Requested quantity must be positive.", "Enter a valid dispatch allocation quantity.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionBatch batch = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == batchId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production batch not found.", "Verify batch ID.");

            decimal batchCapacityQty = batch.ActualOutputQty > 0 ? batch.ActualOutputQty : batch.PlannedBatchQty;
            decimal alreadyAllocated = await _repository.SilaCulinaryProductionDispatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProductionBatchId == batch.Id && x.IsActive)
                .SumAsync(x => x.AllocatedQty, cancellationToken);

            decimal remainingAvailable = Math.Max(0m, batchCapacityQty - alreadyAllocated);
            if (requestedQty > remainingAvailable)
                throw new BadRequestCustomException($"Allocation exceeds available batch output ({remainingAvailable:N2} remaining).", "Cannot allocate beyond available central kitchen batch output.");

            string dispNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.PRODUCTION_DISPATCH, 5, cancellationToken);
            SilaCulinaryProductionDispatch dispatch = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DispatchNumber = dispNum,
                CentralKitchenId = centralKitchenId,
                ProductionBatchId = batch.Id,
                DestinationOutletId = destinationOutletId,
                DestinationOutletName = destinationOutletName.Trim(),
                RequestedQty = requestedQty,
                AllocatedQty = requestedQty,
                RequiredDeliveryUtc = requiredDeliveryUtc ?? DateTime.UtcNow.AddHours(3),
                Status = "ALLOCATED",
                IsActive = true
            };
            _repository.SilaCulinaryProductionDispatch.Create(dispatch);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_DISPATCHED, Common.SILA_REF_PRODUCTION_DISPATCH, dispatch.Id, $"Allocated {requestedQty:N2} from batch {batch.BatchNumber} to {destinationOutletName}");
            await _repository.SaveAsync();
            return MapDispatch(dispatch);
        }

        public async Task<SilaCulinaryProductionDispatchDto> UpdateDispatchMovementAsync(Guid organizationId, Guid userId, string actorName, Guid dispatchId, string action, decimal qty, CancellationToken cancellationToken)
        {
            if (qty <= 0)
                throw new BadRequestCustomException("Quantity must be positive.", "Enter a positive dispatch or receipt quantity.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionDispatch dispatch = await _repository.SilaCulinaryProductionDispatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == dispatchId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Dispatch record not found.", "Verify dispatch ID.");

            string act = (action ?? "DISPATCH").Trim().ToUpperInvariant();
            if (act == "DISPATCH")
            {
                if (dispatch.DispatchedQty + qty > dispatch.AllocatedQty)
                    throw new BadRequestCustomException("Dispatched quantity cannot exceed allocated quantity.", "Adjust quantity within allocated limit.");
                dispatch.DispatchedQty += qty;
                dispatch.Status = dispatch.DispatchedQty >= dispatch.AllocatedQty ? "DISPATCHED" : "PARTIALLY_DISPATCHED";
            }
            else if (act == "RECEIVE")
            {
                if (dispatch.ReceivedQty + qty > dispatch.DispatchedQty)
                    throw new BadRequestCustomException("Received quantity cannot exceed dispatched quantity.", "Verify received quantity.");
                dispatch.ReceivedQty += qty;
                dispatch.Status = dispatch.ReceivedQty >= dispatch.AllocatedQty ? "RECEIVED" : "PARTIALLY_RECEIVED";
            }

            _repository.SilaCulinaryProductionDispatch.Update(dispatch);
            await _repository.SaveAsync();
            return MapDispatch(dispatch);
        }

        public async Task<SilaCulinaryProductionBatchDto> TriggerBuffetReplenishmentAsync(Guid organizationId, Guid userId, string actorName, Guid kitchenId, string itemName, decimal qty, Guid? recipeId, CancellationToken cancellationToken)
        {
            if (qty <= 0)
                throw new BadRequestCustomException("Replenishment quantity must be positive.", "Provide a positive portion quantity.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string ordNum = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.PRODUCTION_ORDER, 5, cancellationToken);
            SilaCulinaryProductionOrder order = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                OrderNumber = ordNum,
                KitchenId = kitchenId,
                RecipeId = recipeId,
                RecipeName = itemName.Trim(),
                OrderType = "BUFFET",
                PlannedOutputQty = qty,
                ScheduledStartUtc = DateTime.UtcNow,
                RequiredCompletionUtc = DateTime.UtcNow.AddMinutes(45),
                ResponsibleChef = actorName,
                Priority = 10,
                ExecutionStatus = "RELEASED",
                IsActive = true
            };
            _repository.SilaCulinaryProductionOrder.Create(order);

            SilaCulinaryProductionBatch batch = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProductionOrderId = order.Id,
                BatchNumber = $"{ordNum}-R01",
                BatchType = "REPLENISHMENT",
                PlannedBatchQty = qty,
                KitchenId = kitchenId,
                AssignedChefName = actorName,
                PlannedStartUtc = DateTime.UtcNow,
                PlannedFinishUtc = DateTime.UtcNow.AddMinutes(45),
                BatchPriority = 10,
                BatchStatus = "RELEASED",
                UseByUtc = DateTime.UtcNow.AddHours(2),
                QrPayloadJson = JsonSerializer.Serialize(new { batchNumber = $"{ordNum}-R01", itemName, qty, type = "BUFFET_REPLENISHMENT" }, CamelJson),
                IsActive = true
            };
            _repository.SilaCulinaryProductionBatch.Create(batch);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_BATCH_GENERATED, Common.SILA_REF_PRODUCTION_BATCH, batch.Id, $"Generated buffet replenishment batch {batch.BatchNumber} ({qty:N0})");
            await _repository.SaveAsync();
            return MapBatch(batch);
        }

        public async Task<object> SyncEventProductionImpactAsync(Guid organizationId, Guid roleId, Guid eventId, decimal revisedGuestCount, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await _repository.SilaCulinaryEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == eventId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Event not found.", "Verify event ID.");

            decimal baselineGuests = ev.GuaranteedGuests > 0 ? ev.GuaranteedGuests : (ev.ExpectedGuests > 0 ? ev.ExpectedGuests : 100m);
            decimal ratio = baselineGuests > 0 ? revisedGuestCount / baselineGuests : 1m;
            List<SilaCulinaryProductionOrder> eventOrders = await _repository.SilaCulinaryProductionOrder
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventId == ev.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            decimal currentPlannedOutput = eventOrders.Sum(o => o.PlannedOutputQty);
            decimal projectedOutput = Math.Round(currentPlannedOutput * ratio, 2);
            decimal? currentEstCost = CanViewCosts(roleId) ? eventOrders.Sum(o => o.EstimatedProductionCost) : null;
            decimal? projectedEstCost = CanViewCosts(roleId) && currentEstCost.HasValue ? Math.Round(currentEstCost.Value * ratio, 2) : null;

            return new
            {
                EventId = ev.Id,
                EventCode = ev.EventCode,
                EventName = ev.Name,
                BaselineGuestCount = baselineGuests,
                RevisedGuestCount = revisedGuestCount,
                ScaleFactor = Math.Round(ratio, 4),
                AffectedOrdersCount = eventOrders.Count,
                CurrentPlannedOutput = currentPlannedOutput,
                ProjectedOutput = projectedOutput,
                CurrentEstimatedCost = currentEstCost,
                ProjectedEstimatedCost = projectedEstCost,
                RequiresPlanAmendment = Math.Abs(ratio - 1m) > 0.05m
            };
        }

        // -------------------------------------------------------------------------
        // 4A.11 Dashboard Cockpit, Exceptions, Copilot & 20 Reports
        // -------------------------------------------------------------------------
        public async Task<SilaCulinaryProductionDashboardDto> GetDashboardAsync(Guid organizationId, Guid roleId, Guid? propertyId, Guid? kitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionOrder> oq = _repository.SilaCulinaryProductionOrder.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) oq = oq.Where(x => x.PropertyId == propertyId.Value);
            if (kitchenId.HasValue) oq = oq.Where(x => x.KitchenId == kitchenId.Value);
            List<SilaCulinaryProductionOrder> orders = await oq.ToListAsync(cancellationToken);

            IQueryable<SilaCulinaryProductionBatch> bq = _repository.SilaCulinaryProductionBatch.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (kitchenId.HasValue) bq = bq.Where(x => x.KitchenId == kitchenId.Value);
            List<SilaCulinaryProductionBatch> batches = await bq.ToListAsync(cancellationToken);

            List<SilaCulinaryProductionOrderIngredient> ings = await _repository.SilaCulinaryProductionOrderIngredient
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ShortageQty > 0)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProductionException> excs = await _repository.SilaCulinaryProductionException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ResolutionStatus != "RESOLVED")
                .ToListAsync(cancellationToken);

            DateTime now = DateTime.UtcNow;
            int completedCount = batches.Count(b => b.BatchStatus == "COMPLETED");
            int onTimeCompleted = batches.Count(b => b.BatchStatus == "COMPLETED" && (!b.ActualFinishUtc.HasValue || b.ActualFinishUtc.Value <= b.PlannedFinishUtc));

            bool showCosts = CanViewCosts(roleId);
            return new SilaCulinaryProductionDashboardDto
            {
                PlannedOrdersCount = orders.Count,
                ReleasedOrdersCount = orders.Count(o => o.ExecutionStatus != "CREATED" && o.ExecutionStatus != "CANCELLED"),
                ActiveBatchesCount = batches.Count(b => b.BatchStatus is "PREPARING" or "COOKING" or "PAUSED"),
                CompletedBatchesCount = completedCount,
                DelayedBatchesCount = batches.Count(b => b.BatchStatus != "COMPLETED" && b.BatchStatus != "CANCELLED" && b.PlannedFinishUtc < now),
                IngredientShortagesCount = ings.Count,
                PendingQualityChecksCount = batches.Count(b => b.QualityStatus == "PENDING" || b.BatchStatus == "QUALITY_CHECK" || b.BatchStatus == "ON_HOLD"),
                EquipmentUtilizationPct = batches.Count == 0 ? 0m : Math.Min(100m, Math.Round(batches.Count(b => b.EquipmentId.HasValue) * 100m / Math.Max(1, batches.Count), 1)),
                StaffUtilizationPct = batches.Count == 0 ? 0m : Math.Min(100m, Math.Round(batches.Count(b => !string.IsNullOrWhiteSpace(b.AssignedChefName)) * 100m / Math.Max(1, batches.Count), 1)),
                OnTimeProductionRatePct = completedCount == 0 ? 100m : Math.Round(onTimeCompleted * 100m / completedCount, 1),
                PlannedOutputTotal = orders.Sum(o => o.PlannedOutputQty),
                ActualOutputTotal = batches.Sum(b => b.ActualOutputQty),
                Orders = orders.Take(30).Select(o => MapOrder(o, new(), batches.Where(b => b.ProductionOrderId == o.Id).ToList(), showCosts)).ToList(),
                KdsBatches = batches.OrderBy(b => b.PlannedFinishUtc).Take(40).Select(MapBatch).ToList(),
                OpenExceptions = excs.Select(MapException).ToList()
            };
        }

        public async Task<List<SilaCulinaryProductionExceptionDto>> ListExceptionsAsync(Guid organizationId, string? status, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryProductionException> q = _repository.SilaCulinaryProductionException.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.ResolutionStatus == status.Trim().ToUpperInvariant());
            List<SilaCulinaryProductionException> list = await q.OrderByDescending(x => x.DateCreated).ToListAsync(cancellationToken);
            return list.Select(MapException).ToList();
        }

        public async Task<SilaCulinaryProductionExceptionDto> ReportExceptionAsync(Guid organizationId, Guid userId, string actorName, Guid? orderId, Guid? batchId, string exceptionType, string severity, string description, string? recommendedAction, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string code = await DocumentNumber.NextAsync(_repository, buyer.Id, "PRD-EXC", 5, cancellationToken);
            SilaCulinaryProductionException exc = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ExceptionCode = code,
                ProductionOrderId = orderId,
                ProductionBatchId = batchId,
                ExceptionType = string.IsNullOrWhiteSpace(exceptionType) ? "PRODUCTION_DELAY" : exceptionType.Trim().ToUpperInvariant(),
                Severity = string.IsNullOrWhiteSpace(severity) ? "HIGH" : severity.Trim().ToUpperInvariant(),
                Description = description.Trim(),
                ReportedByName = actorName,
                RecommendedAction = recommendedAction,
                ResolutionStatus = "OPEN",
                IsActive = true
            };
            _repository.SilaCulinaryProductionException.Create(exc);
            await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
            {
                TaskType = "PRODUCTION_EXCEPTION",
                DeduplicationKey = $"PROD_EXC_{code}",
                Title = $"Production Exception {code}: {exc.ExceptionType}",
                Description = exc.Description,
                Priority = exc.Severity == "CRITICAL" ? SilaOperations.PRIORITY_CRITICAL : SilaOperations.PRIORITY_ACTION,
                SourceModule = "CULINARY_PRODUCTION",
                SourceDocumentType = Common.SILA_REF_PRODUCTION_EXCEPTION,
                SourceDocumentId = exc.Id,
                SourceDocumentNumber = code
            }, cancellationToken);
            RecordAudit(buyer.Id, userId, actorName, Common.SILA_AUDIT_PROD_EXCEPTION, Common.SILA_REF_PRODUCTION_EXCEPTION, exc.Id, $"Reported exception {code}: {exc.ExceptionType}");
            await _repository.SaveAsync();
            return MapException(exc);
        }

        public async Task<SilaCulinaryProductionExceptionDto> ResolveExceptionAsync(Guid organizationId, Guid userId, string actorName, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProductionException exc = await _repository.SilaCulinaryProductionException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == exceptionId && x.IsActive)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Production exception not found.", "Verify exception ID.");
            exc.ResolutionStatus = string.IsNullOrWhiteSpace(resolutionStatus) ? "RESOLVED" : resolutionStatus.Trim().ToUpperInvariant();
            exc.ResolutionNote = note;
            exc.AssignedOwner = actorName;
            _repository.SilaCulinaryProductionException.Update(exc);
            await _repository.SaveAsync();
            return MapException(exc);
        }

        public async Task<SilaCulinaryProductionCopilotResponseDto> QueryCopilotAsync(Guid organizationId, Guid userId, Guid roleId, string question, CancellationToken cancellationToken)
        {
            if (PromptInjectionRegex.IsMatch(question ?? string.Empty))
                throw new BadRequestCustomException("Unsafe prompt rejected.", "Culinary Production Copilot is strictly read-only and cannot execute overrides or postings.");

            SilaCulinaryProductionDashboardDto dash = await GetDashboardAsync(organizationId, roleId, null, null, cancellationToken);
            string qLower = (question ?? string.Empty).ToLowerInvariant();
            string answer;
            if (qLower.Contains("delay"))
                answer = $"There are {dash.DelayedBatchesCount} delayed batch(es) out of {dash.ActiveBatchesCount} active and {dash.CompletedBatchesCount} completed batches.";
            else if (qLower.Contains("shortage") || qLower.Contains("unavailable"))
                answer = $"Detected {dash.IngredientShortagesCount} ingredient shortage line(s) and {dash.OpenExceptions.Count} open production exception(s).";
            else if (qLower.Contains("capacity") || qLower.Contains("300") || qLower.Contains("guest"))
                answer = $"Current planned output is {dash.PlannedOutputTotal:N0} across {dash.PlannedOrdersCount} orders; equipment utilization is {dash.EquipmentUtilizationPct:N1}% and staff utilization is {dash.StaffUtilizationPct:N1}%.";
            else
                answer = $"Today's production cockpit: {dash.PlannedOrdersCount} planned orders ({dash.ReleasedOrdersCount} released), {dash.ActiveBatchesCount} active batches, {dash.PendingQualityChecksCount} pending quality checks, and {dash.OnTimeProductionRatePct:N1}% on-time rate.";

            return new SilaCulinaryProductionCopilotResponseDto
            {
                Answer = answer,
                DataTimestampUtc = DateTime.UtcNow,
                SupportingMetrics = new List<string>
                {
                    $"Planned Orders: {dash.PlannedOrdersCount}",
                    $"Delayed Batches: {dash.DelayedBatchesCount}",
                    $"Pending QC: {dash.PendingQualityChecksCount}",
                    $"Open Exceptions: {dash.OpenExceptions.Count}"
                },
                IdentifiedRisks = dash.OpenExceptions.Select(e => $"{e.ExceptionCode}: {e.Description}").Take(5).ToList(),
                RecommendedAction = dash.DelayedBatchesCount > 0 ? "Review delayed batches in AI Production Scheduler & reassign idle equipment." : "Continue standard KDS execution & mandatory quality checkpoints.",
                ExpectedOperationalImpact = "Maintains on-time service delivery without relaxing food safety rules.",
                ActionBlockedNotice = true
            };
        }

        public List<SilaCulinaryProductionReportCatalogItemDto> GetReportCatalog() => new()
        {
            new() { ReportCode = "DAILY_PRODUCTION_PLAN", Name = "1. Daily Production Plan", Description = "Daily approved production plans and line targets." },
            new() { ReportCode = "WEEKLY_PRODUCTION_PLAN", Name = "2. Weekly Production Plan", Description = "Weekly rolled-up kitchen production commitments." },
            new() { ReportCode = "KITCHEN_PRODUCTION_SCHEDULE", Name = "3. Kitchen Production Schedule", Description = "Time-slotted batch schedule by kitchen & equipment." },
            new() { ReportCode = "PRODUCTION_ORDER_STATUS", Name = "4. Production Order Status", Description = "Execution status and planned vs actual output per order." },
            new() { ReportCode = "PRODUCTION_BATCH_STATUS", Name = "5. Production Batch Status", Description = "Batch lifecycle, priority, quality state & deadlines." },
            new() { ReportCode = "INGREDIENT_REQUIREMENT", Name = "6. Ingredient Requirement Report", Description = "Multi-level recipe-exploded gross, net, and yield-adjusted requirements." },
            new() { ReportCode = "INGREDIENT_SHORTAGE", Name = "7. Ingredient Shortage Report", Description = "Shortage quantities excluding expired/blocked stock." },
            new() { ReportCode = "INGREDIENT_PICKING", Name = "8. Ingredient Picking Report", Description = "FEFO/FIFO picking lines with batch numbers and expiry dates." },
            new() { ReportCode = "MATERIAL_ISSUE", Name = "9. Material Issue Report", Description = "Idempotent material issue references linked to Goods Issues." },
            new() { ReportCode = "KITCHEN_CAPACITY", Name = "10. Kitchen Capacity Report", Description = "Configured daily portion capacity vs scheduled load." },
            new() { ReportCode = "EQUIPMENT_UTILIZATION", Name = "11. Equipment Utilization Report", Description = "Equipment capacity, setup/cleaning overhead & utilization." },
            new() { ReportCode = "STAFF_ALLOCATION", Name = "12. Staff Allocation Report", Description = "Shift assignments, culinary roles & cross-kitchen coverage." },
            new() { ReportCode = "PRODUCTION_DELAY", Name = "13. Production Delay Report", Description = "Batches exceeding planned finish deadlines." },
            new() { ReportCode = "EVENT_PRODUCTION", Name = "14. Event Production Report", Description = "Event & banquet production orders and guest count scaling." },
            new() { ReportCode = "BUFFET_PRODUCTION", Name = "15. Buffet Production Report", Description = "Initial buffet production and replenishment batches." },
            new() { ReportCode = "CENTRAL_KITCHEN_DISTRIBUTION", Name = "16. Central Kitchen Distribution Report", Description = "Outlet allocations, dispatches, and receipt confirmations." },
            new() { ReportCode = "BATCH_TRACEABILITY", Name = "17. Batch Traceability Report", Description = "End-to-end backward & forward batch genealogy." },
            new() { ReportCode = "QUALITY_CHECKPOINT", Name = "18. Quality Checkpoint Report", Description = "Temperature, hygiene, and corrective action records." },
            new() { ReportCode = "PRODUCTION_EXCEPTION", Name = "19. Production Exception Report", Description = "Operational exceptions, severity & resolution tracking." },
            new() { ReportCode = "PLANNED_VS_ACTUAL_OUTPUT", Name = "20. Planned vs Actual Output Report", Description = "Operational output comparison feeding Phase 4B." }
        };

        public async Task<SilaCulinaryAnalyticsReportExportDto> ExportReportAsync(Guid organizationId, Guid roleId, string reportCode, string format, CancellationToken cancellationToken)
        {
            SilaCulinaryProductionDashboardDto dash = await GetDashboardAsync(organizationId, roleId, null, null, cancellationToken);
            string fmt = string.IsNullOrWhiteSpace(format) ? "CSV" : format.Trim().ToUpperInvariant();
            string code = (reportCode ?? "DAILY_PRODUCTION_PLAN").Trim().ToUpperInvariant();

            StringBuilder sb = new();
            sb.AppendLine("ReportCode,OrderNumber,RecipeName,PlannedQty,ActualQty,Status");
            foreach (SilaCulinaryProductionOrderDto o in dash.Orders)
            {
                sb.AppendLine(string.Join(",",
                    SafeCsvCell(code),
                    SafeCsvCell(o.OrderNumber),
                    SafeCsvCell(o.RecipeName),
                    o.PlannedOutputQty.ToString("0.00"),
                    o.ActualOutputQty.ToString("0.00"),
                    SafeCsvCell(o.ExecutionStatus)));
            }

            string ext = fmt == "EXCEL" ? "xls.csv" : (fmt == "PDF" ? "html" : "csv");
            return new SilaCulinaryAnalyticsReportExportDto
            {
                ReportCode = code,
                Format = fmt,
                FileName = $"{code.ToLowerInvariant()}_{DateTime.UtcNow:yyyyMMdd_HHmm}.{ext}",
                ContentType = fmt == "PDF" ? "text/html" : "text/csv",
                Content = fmt == "PDF"
                    ? $"<!doctype html><html><head><meta charset=\"utf-8\"><title>{System.Net.WebUtility.HtmlEncode(code)}</title></head><body><h1>{System.Net.WebUtility.HtmlEncode(code)}</h1><pre>{System.Net.WebUtility.HtmlEncode(sb.ToString())}</pre></body></html>"
                    : sb.ToString()
            };
        }

        public async Task<SilaCulinaryMobileProductionSummaryDto> GetMobileSummaryAsync(Guid organizationId, Guid userId, Guid roleId, Guid? kitchenId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            List<SilaCulinaryProductionBatch> batches = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!kitchenId.HasValue || x.KitchenId == kitchenId.Value))
                .OrderBy(x => x.PlannedFinishUtc)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProductionTask> tasks = await _repository.SilaCulinaryProductionTask
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status != "COMPLETED")
                .OrderBy(x => x.SequenceOrder)
                .Take(30)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProductionPickingLine> picking = await _repository.SilaCulinaryProductionPickingLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status != "ISSUED")
                .Take(30)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProductionException> excs = await _repository.SilaCulinaryProductionException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ResolutionStatus != "RESOLVED")
                .Take(20)
                .ToListAsync(cancellationToken);

            return new SilaCulinaryMobileProductionSummaryDto
            {
                CanViewCosts = CanViewCosts(roleId),
                MyTasksCount = tasks.Count,
                ActiveBatchesCount = batches.Count(b => b.BatchStatus is "RELEASED" or "PREPARING" or "COOKING" or "QUALITY_CHECK" or "ON_HOLD"),
                PendingPickingCount = picking.Count(p => p.Status is "OPEN" or "PARTIAL"),
                PendingQualityChecksCount = batches.Count(b => b.QualityStatus == "PENDING" || b.BatchStatus == "ON_HOLD"),
                OpenExceptionsCount = excs.Count,
                MyTasks = tasks.Select(MapTask).ToList(),
                ActiveBatches = batches.Take(30).Select(MapBatch).ToList(),
                PickingLines = picking.Select(MapPickingLine).ToList(),
                Exceptions = excs.Select(MapException).ToList()
            };
        }

        public async Task<SilaCulinaryProductionPhase4BHandoverDto> GetPhase4BHandoverAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            List<SilaCulinaryProductionBatch> batches = await _repository.SilaCulinaryProductionBatch
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.BatchStatus == "COMPLETED")
                .ToListAsync(cancellationToken);
            int issueCount = await _repository.SilaCulinaryProductionMaterialIssueRef
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && !x.IsReversed)
                .CountAsync(cancellationToken);

            return new SilaCulinaryProductionPhase4BHandoverDto
            {
                CompletedBatchesCount = batches.Count,
                MaterialIssueReferencesCount = issueCount,
                TotalPlannedOutputQty = batches.Sum(b => b.PlannedBatchQty),
                TotalActualOutputQty = batches.Sum(b => b.ActualOutputQty)
            };
        }

        // -------------------------------------------------------------------------
        // DTO Mappers
        // -------------------------------------------------------------------------
        private static SilaCulinaryKitchenDto MapKitchen(SilaCulinaryKitchen k) => new()
        {
            Id = k.Id,
            PropertyId = k.PropertyId,
            PropertyName = k.PropertyName,
            KitchenCode = k.KitchenCode,
            KitchenName = k.KitchenName,
            KitchenType = k.KitchenType,
            AssociatedStoreLocationId = k.AssociatedStoreLocationId,
            ResponsibleExecutiveChef = k.ResponsibleExecutiveChef,
            KitchenSupervisor = k.KitchenSupervisor,
            DailyPortionCapacity = k.DailyPortionCapacity,
            OutletIds = SafeDeserialize<List<Guid>>(k.OutletAssociationsJson) ?? new(),
            SupportedMealPeriods = SafeDeserialize<List<string>>(k.SupportedMealPeriodsJson) ?? new(),
            SupportedCategories = SafeDeserialize<List<string>>(k.SupportedCategoriesJson) ?? new(),
            EffectiveFromUtc = k.EffectiveFromUtc,
            EffectiveToUtc = k.EffectiveToUtc,
            IsActive = k.IsActive
        };

        private static SilaCulinaryEquipmentDto MapEquipment(SilaCulinaryEquipment e) => new()
        {
            Id = e.Id,
            KitchenId = e.KitchenId,
            WorkstationId = e.WorkstationId,
            EquipmentCode = e.EquipmentCode,
            EquipmentName = e.EquipmentName,
            EquipmentType = e.EquipmentType,
            BatchCapacity = e.BatchCapacity,
            CapacityUom = e.CapacityUom,
            SetupMinutes = e.SetupMinutes,
            CleaningMinutes = e.CleaningMinutes,
            AvailableFromMinute = e.AvailableFromMinute,
            AvailableUntilMinute = e.AvailableUntilMinute,
            UnderMaintenance = e.UnderMaintenance,
            TemperatureCapability = e.TemperatureCapability,
            AllergenSegregated = e.AllergenSegregated
        };

        private static SilaCulinaryStaffAssignmentDto MapStaff(SilaCulinaryStaffAssignment s) => new()
        {
            Id = s.Id,
            PropertyId = s.PropertyId,
            UserId = s.UserId,
            StaffName = s.StaffName,
            CulinaryRole = s.CulinaryRole,
            KitchenId = s.KitchenId,
            WorkstationId = s.WorkstationId,
            ShiftCode = s.ShiftCode,
            SkillClassification = s.SkillClassification,
            SupervisorName = s.SupervisorName,
            AvailableFromMinute = s.AvailableFromMinute,
            AvailableUntilMinute = s.AvailableUntilMinute,
            IsTemporary = s.IsTemporary,
            IsCrossKitchen = s.IsCrossKitchen,
            IsAvailable = s.IsAvailable
        };

        private static SilaCulinaryProductionDemandDto MapDemand(SilaCulinaryProductionDemand d) => new()
        {
            Id = d.Id,
            SourceDocumentKey = d.SourceDocumentKey,
            DemandSource = d.DemandSource,
            DemandNature = d.DemandNature,
            PropertyId = d.PropertyId,
            KitchenId = d.KitchenId,
            OutletId = d.OutletId,
            EventId = d.EventId,
            RecipeId = d.RecipeId,
            MenuItemName = d.MenuItemName,
            MealPeriod = d.MealPeriod,
            ProductionDateUtc = d.ProductionDateUtc,
            RequiredCompletionUtc = d.RequiredCompletionUtc,
            ForecastPortions = d.ForecastPortions,
            AdjustedPortions = d.AdjustedPortions,
            Priority = d.Priority,
            ConversionStatus = d.ConversionStatus
        };

        private static SilaCulinaryProductionPlanDto MapPlan(SilaCulinaryProductionPlan p, List<SilaCulinaryProductionPlanLine> lines, bool showCosts) => new()
        {
            Id = p.Id,
            PlanCode = p.PlanCode,
            PropertyId = p.PropertyId,
            PropertyName = p.PropertyName,
            ProductionDateUtc = p.ProductionDateUtc,
            PlanningPeriod = p.PlanningPeriod,
            MealPeriod = p.MealPeriod,
            EventId = p.EventId,
            EventReference = p.EventReference,
            ResponsibleChef = p.ResponsibleChef,
            SourceDemandType = p.SourceDemandType,
            Status = p.Status,
            VersionNumber = p.VersionNumber,
            ApprovedByName = p.ApprovedByName,
            ApprovedAtUtc = p.ApprovedAtUtc,
            TotalEstimatedCost = showCosts ? p.TotalEstimatedCost : null,
            Lines = lines.Select(l => new SilaCulinaryProductionPlanLineDto
            {
                Id = l.Id,
                RecipeId = l.RecipeId,
                RecipeCode = l.RecipeCode,
                MenuItemName = l.MenuItemName,
                RecipeVersion = l.RecipeVersion,
                RequiredPortions = l.RequiredPortions,
                PlannedProductionQty = l.PlannedProductionQty,
                ProductionUom = l.ProductionUom,
                KitchenId = l.KitchenId,
                WorkstationId = l.WorkstationId,
                PlannedStartUtc = l.PlannedStartUtc,
                RequiredCompletionUtc = l.RequiredCompletionUtc,
                Priority = l.Priority,
                IngredientReadiness = l.IngredientReadiness,
                EquipmentReadiness = l.EquipmentReadiness,
                StaffReadiness = l.StaffReadiness,
                EstimatedFoodCost = showCosts ? l.EstimatedFoodCost : null,
                LineStatus = l.LineStatus
            }).ToList()
        };

        private static SilaCulinaryProductionOrderDto MapOrder(SilaCulinaryProductionOrder o, List<SilaCulinaryProductionOrderIngredient> ings, List<SilaCulinaryProductionBatch> batches, bool showCosts) => new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            ProductionPlanId = o.ProductionPlanId,
            PropertyId = o.PropertyId,
            KitchenId = o.KitchenId,
            OutletId = o.OutletId,
            EventId = o.EventId,
            RecipeId = o.RecipeId,
            RecipeVersion = o.RecipeVersion,
            RecipeName = o.RecipeName,
            OrderType = o.OrderType,
            PlannedOutputQty = o.PlannedOutputQty,
            ActualOutputQty = o.ActualOutputQty,
            OutputUom = o.OutputUom,
            ScheduledStartUtc = o.ScheduledStartUtc,
            RequiredCompletionUtc = o.RequiredCompletionUtc,
            ResponsibleChef = o.ResponsibleChef,
            Priority = o.Priority,
            EstimatedProductionCost = showCosts ? o.EstimatedProductionCost : null,
            ApprovalStatus = o.ApprovalStatus,
            ExecutionStatus = o.ExecutionStatus,
            Ingredients = ings.Select(i => new SilaCulinaryProductionOrderIngredientDto
            {
                Id = i.Id,
                MaterialId = i.MaterialId,
                MaterialCode = i.MaterialCode,
                MaterialName = i.MaterialName,
                GrossRequiredQty = i.GrossRequiredQty,
                NetUsableQty = i.NetUsableQty,
                YieldAdjustedQty = i.YieldAdjustedQty,
                InventoryUom = i.InventoryUom,
                AvailableEligibleStock = i.AvailableEligibleStock,
                IncomingBeforeProductionQty = i.IncomingBeforeProductionQty,
                ExistingCommitmentsQty = i.ExistingCommitmentsQty,
                ShortageQty = i.ShortageQty,
                ReadinessStatus = i.ReadinessStatus
            }).ToList(),
            Batches = batches.Select(MapBatch).ToList()
        };

        private static SilaCulinaryProductionBatchDto MapBatch(SilaCulinaryProductionBatch b) => new()
        {
            Id = b.Id,
            ProductionOrderId = b.ProductionOrderId,
            BatchNumber = b.BatchNumber,
            BatchType = b.BatchType,
            PlannedBatchQty = b.PlannedBatchQty,
            ActualOutputQty = b.ActualOutputQty,
            ProductionUom = b.ProductionUom,
            KitchenId = b.KitchenId,
            WorkstationId = b.WorkstationId,
            EquipmentId = b.EquipmentId,
            AssignedStaffUserId = b.AssignedStaffUserId,
            AssignedChefName = b.AssignedChefName,
            PlannedStartUtc = b.PlannedStartUtc,
            PlannedFinishUtc = b.PlannedFinishUtc,
            ActualStartUtc = b.ActualStartUtc,
            ActualFinishUtc = b.ActualFinishUtc,
            BatchPriority = b.BatchPriority,
            QualityStatus = b.QualityStatus,
            BatchStatus = b.BatchStatus,
            ParentBatchId = b.ParentBatchId,
            UseByUtc = b.UseByUtc,
            AllergenSummary = b.AllergenSummary,
            QrPayloadJson = b.QrPayloadJson
        };

        private static SilaCulinaryProductionTaskDto MapTask(SilaCulinaryProductionTask t) => new()
        {
            Id = t.Id,
            ProductionBatchId = t.ProductionBatchId,
            SequenceOrder = t.SequenceOrder,
            DependsOnTaskId = t.DependsOnTaskId,
            TaskType = t.TaskType,
            Instructions = t.Instructions,
            WorkstationId = t.WorkstationId,
            EquipmentId = t.EquipmentId,
            PlannedDurationMinutes = t.PlannedDurationMinutes,
            ActualDurationMinutes = t.ActualDurationMinutes,
            Status = t.Status,
            ExceptionReason = t.ExceptionReason
        };

        private static SilaCulinaryProductionPickingLineDto MapPickingLine(SilaCulinaryProductionPickingLine p) => new()
        {
            Id = p.Id,
            ProductionOrderId = p.ProductionOrderId,
            ProductionBatchId = p.ProductionBatchId,
            MaterialId = p.MaterialId,
            MaterialCode = p.MaterialCode,
            MaterialName = p.MaterialName,
            RequiredQty = p.RequiredQty,
            ReservedQty = p.ReservedQty,
            PickedQty = p.PickedQty,
            InventoryUom = p.InventoryUom,
            StoreLocationId = p.StoreLocationId,
            InventoryBatchNumber = p.InventoryBatchNumber,
            ExpiryUtc = p.ExpiryUtc,
            PickingStrategy = p.PickingStrategy,
            Status = p.Status
        };

        private static SilaCulinaryQualityResultDto MapQualityResult(SilaCulinaryProductionQualityResult q) => new()
        {
            Id = q.Id,
            ProductionBatchId = q.ProductionBatchId,
            CheckpointName = q.CheckpointName,
            ExpectedValue = q.ExpectedValue,
            ActualValue = q.ActualValue,
            MeasurementUom = q.MeasurementUom,
            Passed = q.Passed,
            RecordedByName = q.RecordedByName,
            RecordedAtUtc = q.RecordedAtUtc,
            Comments = q.Comments,
            CorrectiveAction = q.CorrectiveAction,
            SupervisorApproved = q.SupervisorApproved
        };

        private static SilaCulinaryProductionExceptionDto MapException(SilaCulinaryProductionException e) => new()
        {
            Id = e.Id,
            ExceptionCode = e.ExceptionCode,
            ProductionOrderId = e.ProductionOrderId,
            ProductionBatchId = e.ProductionBatchId,
            ExceptionType = e.ExceptionType,
            Severity = e.Severity,
            Description = e.Description,
            ReportedByName = e.ReportedByName,
            AssignedOwner = e.AssignedOwner,
            RecommendedAction = e.RecommendedAction,
            ResolutionStatus = e.ResolutionStatus,
            ResolutionNote = e.ResolutionNote
        };

        private static SilaCulinaryProductionDispatchDto MapDispatch(SilaCulinaryProductionDispatch d) => new()
        {
            Id = d.Id,
            DispatchNumber = d.DispatchNumber,
            CentralKitchenId = d.CentralKitchenId,
            ProductionBatchId = d.ProductionBatchId,
            DestinationOutletId = d.DestinationOutletId,
            DestinationOutletName = d.DestinationOutletName,
            RequestedQty = d.RequestedQty,
            AllocatedQty = d.AllocatedQty,
            DispatchedQty = d.DispatchedQty,
            ReceivedQty = d.ReceivedQty,
            RequiredDeliveryUtc = d.RequiredDeliveryUtc,
            Status = d.Status
        };
    }
}
