using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Buyer.Application.Features.Commands.CreatePurchaseOrder;
using Buyer.Application.Features.Commands.CreateSilaTransfer;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryProcurementService
    {
        Task<SilaCulinaryProcurementDashboardDto> GetDashboardAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken);
        Task<List<SilaCulinaryProcurementPlanSummaryDto>> ListPlansAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, bool? includeSimulations, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> GetPlanAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> CreateAndCalculatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateProcurementPlanRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> RecalculatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryStockReservationDto> CreateReservationAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryCreateReservationRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryStockReservationDto> TransitionReservationAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid reservationId, string action, string? reason, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementAllocationDto> AllocateIncomingPoAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryAllocateIncomingPoRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementAllocationDto> CreateItoDraftAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryCreateItoDraftRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementAllocationDto> CreatePoDraftAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryCreatePoDraftRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> ReconcileGrnAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryReconcileGrnRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> ReconcileItoReversalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryReconcileItoReversalRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> LinkWeeklyBucketAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid lineId, Guid weeklyBucketItemId, CancellationToken cancellationToken);
        Task<SilaCulinarySimulateProcurementResultDto> SimulateAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, SilaCulinarySimulateProcurementRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementOptimizeResultDto> OptimizeAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, decimal? maxBudget, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementAiAnalyzeResponseDto> AnalyzeWithAiAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, string question, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementExceptionDto> ResolveExceptionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> SubmitForApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> DecideApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string decision, string? comment, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> HandleEventChangeImpactAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid newMrpRunId, CancellationToken cancellationToken);
        Task<SilaCulinaryProcurementPlanDetailDto> CancelEventProcurementAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string reason, CancellationToken cancellationToken);
        List<SilaCulinaryProcurementReportCatalogItemDto> GetReportCatalog();
        Task<SilaCulinaryProcurementReportResultDto> ExportReportAsync(Guid organizationId, Guid roleId, Guid planId, string reportCode, CancellationToken cancellationToken);
        Task<SilaCulinaryPhase3AHandoverDto> GetPhase3AHandoverAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken);
    }

    public class SilaCulinaryProcurementService : ISilaCulinaryProcurementService
    {
        private static readonly JsonSerializerOptions SnakeJson = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions CamelJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly object ReservationAtomicGate = new();
        private static readonly Regex PromptInjectionPattern = new(
            @"(ignore\s+previous|system\s+prompt|override|drop\s+table|post\s+sap|release\s+po\s+now|execute\s+sql)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationCredentialProtector _protector;
        private readonly IMediator? _mediator;

        public SilaCulinaryProcurementService(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IIntegrationCredentialProtector protector,
            IMediator? mediator = null)
        {
            _repository = repository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _protector = protector;
            _mediator = mediator;
        }

        private static bool CanViewCosts(Guid roleId) => SilaAccess.HasFullAccess(roleId);

        private async Task<BuyerBusinessProfile> ResolveBuyerAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Buyer organization not found.", "Buyer organization profile was not found.");
        }

        private async Task<SilaCulinaryProcurementPlan> LoadPlanOrThrowAsync(Guid buyerId, Guid planId, bool tracked, CancellationToken cancellationToken)
        {
            IQueryable<SilaCulinaryProcurementPlan> q = tracked
                ? _repository.SilaCulinaryProcurementPlan.FindByConditionTracked(x => x.BuyerId == buyerId && x.Id == planId && x.IsActive)
                : _repository.SilaCulinaryProcurementPlan.FindByCondition(x => x.BuyerId == buyerId && x.Id == planId && x.IsActive);
            return await q.FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Procurement plan not found.", "Culinary procurement plan was not found.");
        }

        private void AddWorkflowEvent(Guid buyerId, string refType, Guid refId, string action, string comment, Guid actorUserId)
        {
            _repository.InventoryWorkflowEvent.Create(new InventoryWorkflowEvent
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ReferenceType = refType,
                ReferenceId = refId,
                Action = action,
                Comment = comment,
                ActorUserId = actorUserId,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            });
        }

        private async Task UpsertNotificationTaskAsync(
            Guid buyerId,
            Guid actorUserId,
            string taskType,
            string deduplicationKey,
            string title,
            string description,
            string priority,
            Guid planId,
            string planCode,
            Guid? locationId,
            CancellationToken cancellationToken)
        {
            try
            {
                await SilaOperations.UpsertTaskAsync(_repository, buyerId, actorUserId, new SilaOperations.TaskDraft
                {
                    TaskType = taskType,
                    DeduplicationKey = deduplicationKey,
                    Title = title,
                    Description = description,
                    Priority = priority,
                    SourceModule = "CULINARY_PROCUREMENT",
                    SourceDocumentType = Common.SILA_REF_CULINARY_PROCUREMENT,
                    SourceDocumentId = planId,
                    SourceDocumentNumber = planCode,
                    LocationId = locationId,
                    ActionRoute = $"/sila/recipes-2?tab=procurement&planId={planId}",
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Phase 2C notification upsert skipped: {ex.Message}");
            }
        }

        private static decimal RoundPackQty(decimal netQty, decimal packSize, decimal moq, out int packs, out decimal excess)
        {
            if (netQty <= 0)
            {
                packs = 0;
                excess = 0;
                return 0;
            }

            decimal safePack = packSize > 0 ? packSize : 1m;
            decimal effective = Math.Max(netQty, Math.Max(0m, moq));
            packs = (int)Math.Ceiling(effective / safePack);
            decimal recommended = packs * safePack;
            excess = Math.Max(0m, recommended - netQty);
            return decimal.Round(recommended, 4);
        }

        private static bool IsDeliveryFeasible(DateTime nowUtc, DateTime requiredByUtc, int leadTimeDays, int inspectionHours, out DateTime expectedDeliveryUtc, out DateTime latestOrderUtc)
        {
            expectedDeliveryUtc = nowUtc.Date.AddDays(Math.Max(0, leadTimeDays)).AddHours(Math.Max(0, inspectionHours));
            latestOrderUtc = requiredByUtc.AddDays(-Math.Max(0, leadTimeDays)).AddHours(-Math.Max(0, inspectionHours));
            return expectedDeliveryUtc <= requiredByUtc && latestOrderUtc >= nowUtc.AddHours(-2);
        }

        public async Task<SilaCulinaryProcurementDashboardDto> GetDashboardAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costsVisible = CanViewCosts(roleId);

            List<SilaCulinaryProcurementPlanSummaryDto> plans = await ListPlansAsync(organizationId, userId, roleId, propertyId, status, false, cancellationToken);

            int activeReservations = await _repository.SilaCulinaryStockReservation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (x.Status == "ACTIVE" || x.Status == "PARTIALLY_FULFILLED"))
                .CountAsync(cancellationToken);

            int pendingPoApprovals = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.AllocationType == "PO_DRAFT" && x.Status == "DRAFT_CREATED")
                .CountAsync(cancellationToken);

            int openLinkedPos = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && (x.AllocationType == "INCOMING_PO" || x.AllocationType == "PO_DRAFT")
                    && x.RemainingQty > 0
                    && x.Status != "CANCELLED" && x.Status != "RELEASED")
                .CountAsync(cancellationToken);

            int overdueReceipts = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.AllocationType == "INCOMING_PO"
                    && x.RemainingQty > 0
                    && x.ExpectedDateUtc != null && x.ExpectedDateUtc < DateTime.UtcNow)
                .CountAsync(cancellationToken);

            int openExceptions = await _repository.SilaCulinaryProcurementException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ResolutionStatus == "OPEN")
                .CountAsync(cancellationToken);

            return new SilaCulinaryProcurementDashboardDto
            {
                TotalPlans = plans.Count,
                UpcomingEventRequirements = plans.Sum(p => p.RequiredMaterialsCount),
                ActiveReservationsCount = activeReservations,
                MaterialsAwaitingTransfer = plans.Sum(p => p.TransferCoveredCount),
                MaterialsAwaitingPurchase = plans.Sum(p => p.PurchaseRequiredCount),
                PendingPoApprovals = pendingPoApprovals,
                OpenLinkedPos = openLinkedPos,
                OverdueReceipts = overdueReceipts,
                AveragePhysicalReadinessPercent = plans.Count > 0 ? decimal.Round(plans.Average(p => p.PhysicalReadinessPercent), 2) : 0m,
                AverageProjectedReadinessPercent = plans.Count > 0 ? decimal.Round(plans.Average(p => p.ProjectedReadinessPercent), 2) : 0m,
                CostsVisible = costsVisible,
                EstimatedNewPurchaseValue = costsVisible ? plans.Sum(p => p.EstimatedNewPurchaseValue ?? 0m) : null,
                CommittedProcurementValue = costsVisible ? plans.Sum(p => p.CommittedProcurementValue ?? 0m) : null,
                OpenExceptionsCount = openExceptions,
                Plans = plans,
            };
        }

        public async Task<List<SilaCulinaryProcurementPlanSummaryDto>> ListPlansAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, bool? includeSimulations, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costsVisible = CanViewCosts(roleId);

            IQueryable<SilaCulinaryProcurementPlan> q = _repository.SilaCulinaryProcurementPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);

            if (propertyId.HasValue)
            {
                q = q.Where(x => x.PropertyId == propertyId.Value);
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                string st = status.Trim().ToUpperInvariant();
                q = q.Where(x => x.Status == st);
            }
            if (includeSimulations == false)
            {
                q = q.Where(x => !x.IsSimulation);
            }

            List<SilaCulinaryProcurementPlan> rows = await q.OrderByDescending(x => x.CalculatedAtUtc).Take(100).ToListAsync(cancellationToken);
            return rows.Select(r => ToSummaryDto(r, costsVisible)).ToList();
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> GetPlanAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> CreateAndCalculatePlanAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateProcurementPlanRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);

            // Idempotency check
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryProcurementPlan? existing = await _repository.SilaCulinaryProcurementPlan
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null)
                {
                    return await BuildDetailDtoAsync(buyer.Id, userId, roleId, existing, cancellationToken);
                }
            }

            SilaCulinaryMrpRun mrp = await _repository.SilaCulinaryMrpRun
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.MrpRunId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("MRP run not found.", "The referenced Phase 2B MRP run does not exist.");

            // §6 Phase 2B Handover validation
            if (mrp.Status is "SUPERSEDED" or "CANCELLED" or "REJECTED")
            {
                throw new BadRequestCustomException(
                    "Ineligible MRP release.",
                    $"MRP run {mrp.MrpCode} has status {mrp.Status} and cannot enter procurement planning.");
            }
            if (!request.IsSimulation && !mrp.IsApprovedBaseline && mrp.Status is not ("APPROVED" or "RELEASED_FOR_PLANNING"))
            {
                throw new BadRequestCustomException(
                    "MRP approval required.",
                    $"MRP run {mrp.MrpCode} must be approved or released for planning before operational procurement (simulation mode is available for draft runs).");
            }

            string code = $"PROC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            SilaCulinaryProcurementPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PlanCode = code,
                Name = string.IsNullOrWhiteSpace(request.Name) ? $"Procurement Plan — {mrp.Name}" : request.Name.Trim(),
                MrpRunId = mrp.Id,
                MrpCode = mrp.MrpCode,
                MrpVersion = mrp.VersionNumber,
                PropertyId = mrp.PropertyId,
                PropertyName = mrp.PropertyName,
                CompanyCode = mrp.CompanyCode,
                PlantCode = mrp.PlantCode,
                Currency = string.IsNullOrWhiteSpace(mrp.Currency) ? (buyer.Currency ?? "AED") : mrp.Currency,
                Status = request.IsSimulation ? "DRAFT" : "CALCULATED",
                VersionNumber = 1,
                IsSimulation = request.IsSimulation,
                CorrelationId = Guid.NewGuid().ToString("N"),
                IdempotencyKey = request.IdempotencyKey,
                CoveragePriorityJson = request.CoveragePriority != null && request.CoveragePriority.Count > 0
                    ? string.Join(",", request.CoveragePriority)
                    : "LOCAL,PREPARED,PO,ITO,CENTRAL,OUTLET,SUBSTITUTE,CONTRACT,CATALOG",
                CalculatedAtUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            };

            _repository.SilaCulinaryProcurementPlan.Create(plan);
            await CalculatePlanLinesAsync(buyer, userId, roleId, actorName, plan, mrp, request.IncludeProposedItoInNet, request.AllowSafetyStockOverride, cancellationToken);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_CREATED, $"Procurement plan {plan.PlanCode} created from MRP {mrp.MrpCode} v{mrp.VersionNumber}.", userId);
            await _repository.SaveAsync();

            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> RecalculatePlanAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            SilaCulinaryMrpRun mrp = await _repository.SilaCulinaryMrpRun
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == plan.MrpRunId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("MRP run not found.", "Source MRP run was not found.");

            // Soft-deactivate old calculated lines & open auto exceptions before recalculation
            List<SilaCulinaryProcurementLine> oldLines = await _repository.SilaCulinaryProcurementLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var ol in oldLines)
            {
                ol.IsActive = false;
                ol.DateUpdated = DateTime.UtcNow;
            }

            List<SilaCulinaryProcurementException> oldEx = await _repository.SilaCulinaryProcurementException
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive && x.ResolutionStatus == "OPEN")
                .ToListAsync(cancellationToken);
            foreach (var ox in oldEx)
            {
                ox.IsActive = false;
                ox.DateUpdated = DateTime.UtcNow;
            }

            plan.CalculatedAtUtc = DateTime.UtcNow;
            plan.DateUpdated = DateTime.UtcNow;
            await CalculatePlanLinesAsync(buyer, userId, roleId, actorName, plan, mrp, true, false, cancellationToken);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_AVAIL, $"Recalculated stock availability and procurement plan {plan.PlanCode}.", userId);
            await _repository.SaveAsync();

            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        private static List<string> ParseStringListJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json, CamelJson) ?? new List<string>(); }
            catch { return new List<string>(); }
        }

        private async Task CalculatePlanLinesAsync(
            BuyerBusinessProfile buyer,
            Guid userId,
            Guid roleId,
            string actorName,
            SilaCulinaryProcurementPlan plan,
            SilaCulinaryMrpRun mrp,
            bool includeProposedItoInNet,
            bool allowSafetyOverride,
            CancellationToken cancellationToken)
        {
            _pendingExceptions.Clear();
            List<SilaCulinaryMrpLine> mrpLines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == mrp.Id && x.IsActive)
                .OrderBy(x => x.RequiredDateUtc)
                .ToListAsync(cancellationToken);

            // Resolve event metadata via EventCodes in MRP & line ContributingEventsJson
            List<string> allEventCodes = ParseStringListJson(mrp.EventReferencesJson)
                .Concat(mrpLines.SelectMany(l => ParseStringListJson(l.ContributingEventsJson)))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Dictionary<string, SilaCulinaryEvent> eventByCodeMap = allEventCodes.Count > 0
                ? await _repository.SilaCulinaryEvent
                    .FindByCondition(x => x.BuyerId == buyer.Id && allEventCodes.Contains(x.EventCode) && x.IsActive)
                    .ToDictionaryAsync(x => x.EventCode, StringComparer.OrdinalIgnoreCase, cancellationToken)
                : new Dictionary<string, SilaCulinaryEvent>(StringComparer.OrdinalIgnoreCase);

            if (eventByCodeMap.Count > 0)
            {
                SilaCulinaryEvent firstEv = eventByCodeMap.Values.OrderBy(e => e.StartDateUtc).First();
                plan.PrimaryEventId = firstEv.Id;
                plan.PrimaryEventCode = firstEv.EventCode;
                plan.PrimaryEventName = firstEv.Name;
                plan.EventDateUtc = firstEv.StartDateUtc;
                plan.PreparationDeadlineUtc = firstEv.PrepStartUtc ?? firstEv.StartDateUtc.AddHours(-6);
                plan.PlannedGuests = eventByCodeMap.Values.Sum(e => e.PlanningGuests);
                plan.GuaranteedGuests = eventByCodeMap.Values.Sum(e => e.GuaranteedGuests);
                plan.ResponsibleChef = firstEv.ResponsibleChef;
                plan.VenueName = firstEv.VenueName;
            }

            List<InventoryLocation> locations = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (!plan.PropertyId.HasValue || x.PropertyId == plan.PropertyId.Value))
                .ToListAsync(cancellationToken);
            Dictionary<Guid, InventoryLocation> locMap = locations.ToDictionary(l => l.Id);
            List<Guid> locIds = locations.Select(l => l.Id).ToList();

            List<Guid> matIds = mrpLines.Where(l => l.MaterialId.HasValue).Select(l => l.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> matMap = matIds.Count > 0
                ? await _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, cancellationToken)
                : new Dictionary<Guid, ItemBuyerMaster>();
            Dictionary<Guid, List<MaterialUomConversion>> uomConversions = matIds.Count > 0
                ? await UomConverter.GetConversionsAsync(_repository, matIds, cancellationToken)
                : new Dictionary<Guid, List<MaterialUomConversion>>();

            List<InventoryBalance> balances = matIds.Count > 0 && locIds.Count > 0
                ? await _repository.InventoryBalance
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && matIds.Contains(x.MaterialId) && locIds.Contains(x.LocationId))
                    .ToListAsync(cancellationToken)
                : new List<InventoryBalance>();

            List<InventoryLocationMaterial> locMaterials = matIds.Count > 0 && locIds.Count > 0
                ? await _repository.InventoryLocationMaterial
                    .FindByCondition(x => x.IsActive && matIds.Contains(x.MaterialId) && locIds.Contains(x.LocationId))
                    .ToListAsync(cancellationToken)
                : new List<InventoryLocationMaterial>();

            List<SilaCulinaryStockReservation> activeReservations = matIds.Count > 0
                ? await _repository.SilaCulinaryStockReservation
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && matIds.Contains(x.MaterialId)
                        && (x.Status == "ACTIVE" || x.Status == "PARTIALLY_FULFILLED"))
                    .ToListAsync(cancellationToken)
                : new List<SilaCulinaryStockReservation>();

            // Open PO items
            List<string> matCodes = mrpLines.Where(l => !string.IsNullOrWhiteSpace(l.MaterialCode)).Select(l => l.MaterialCode!).Distinct().ToList();
            List<PurchaseOrderItem> openPoItems = matCodes.Count > 0
                ? await _repository.PurchaseOrderItem
                    .FindByCondition(x => x.IsActive && x.MaterialCode != null && matCodes.Contains(x.MaterialCode) && x.PurchaseOrder.BuyerId == buyer.Id && x.PurchaseOrder.IsActive)
                    .Include(x => x.PurchaseOrder)
                    .ToListAsync(cancellationToken)
                : new List<PurchaseOrderItem>();

            List<SilaCulinaryProcurementAllocation> existingPoAllocs = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.AllocationType == "INCOMING_PO" && x.Status != "CANCELLED" && x.Status != "RELEASED")
                .ToListAsync(cancellationToken);

            // GRN batch metadata (accepted - reversed)
            List<GoodsReceiptItem> grnBatches = matIds.Count > 0
                ? await _repository.GoodsReceiptItem
                    .FindByCondition(x => x.IsActive && x.MaterialId.HasValue && matIds.Contains(x.MaterialId.Value)
                        && x.GoodsReceipt.BuyerId == buyer.Id && x.GoodsReceipt.IsActive && x.GoodsReceipt.Status == Common.SILA_GRN_POSTED)
                    .Include(x => x.GoodsReceipt)
                    .ToListAsync(cancellationToken)
                : new List<GoodsReceiptItem>();

            // Suppliers + Contracts + Catalog mappings
            List<SilaSupplier> suppliers = await _repository.SilaSupplier
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status == "ACTIVE")
                .ToListAsync(cancellationToken);

            List<PredefinedContract> contracts = await _repository.PredefinedContract
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.EndDate >= DateTime.UtcNow.Date)
                .ToListAsync(cancellationToken);

            List<CatalogMaterialMapping> catMappings = matIds.Count > 0
                ? await _repository.CatalogMaterialMapping
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && matIds.Contains(x.MaterialId))
                    .ToListAsync(cancellationToken)
                : new List<CatalogMaterialMapping>();

            // Weekly bucket duplicate check
            List<WeeklyBucketItem> bucketItems = matIds.Count > 0
                ? await _repository.WeeklyBucketItem
                    .FindByCondition(x => x.IsActive && x.MaterialId.HasValue && matIds.Contains(x.MaterialId.Value)
                        && x.WeeklyBucket.BuyerId == buyer.Id && x.WeeklyBucket.IsActive
                        && (x.WeeklyBucket.Status == Common.WEEKLY_BUCKET_OPEN || x.WeeklyBucket.Status == Common.WEEKLY_BUCKET_PENDING_APPROVAL || x.WeeklyBucket.Status == Common.WEEKLY_BUCKET_APPROVED))
                    .Include(x => x.WeeklyBucket)
                    .ToListAsync(cancellationToken)
                : new List<WeeklyBucketItem>();

            // Mutable remaining pools across lines in this plan to avoid double counting
            Dictionary<(Guid LocationId, Guid MaterialId), decimal> remainingLocationStock = new();
            Dictionary<(Guid LocationId, Guid MaterialId), (decimal OnHand, decimal Competing, decimal Safety)> locationMeta = new();
            foreach (Guid mId in matIds)
            {
                Dictionary<Guid, decimal> itoReservedByLoc = locIds.Count > 0
                    ? await SilaLiveStockFigures.GetReservedAsync(_repository, buyer.Id, mId, locIds, cancellationToken)
                    : new Dictionary<Guid, decimal>();

                foreach (InventoryBalance bal in balances.Where(b => b.MaterialId == mId))
                {
                    decimal itoRes = itoReservedByLoc.GetValueOrDefault(bal.LocationId, 0m);
                    decimal culRes = activeReservations
                        .Where(r => r.LocationId == bal.LocationId && r.MaterialId == mId && r.ProcurementPlanId != plan.Id)
                        .Sum(r => Math.Max(0m, r.ReservedQty - r.ReleasedQty - r.FulfilledQty));
                    decimal safety = allowSafetyOverride
                        ? 0m
                        : (locMaterials.FirstOrDefault(lm => lm.LocationId == bal.LocationId && lm.MaterialId == mId)?.SafetyStock ?? 0m);
                    decimal competing = itoRes + culRes;
                    decimal eligible = Math.Max(0m, bal.OnHandQty - competing - safety);
                    remainingLocationStock[(bal.LocationId, mId)] = eligible;
                    locationMeta[(bal.LocationId, mId)] = (bal.OnHandQty, competing, safety);
                }
            }

            Dictionary<Guid, decimal> remainingPoPool = new();
            foreach (PurchaseOrderItem poi in openPoItems)
            {
                if (poi.PurchaseOrder.Status is "CANCELLED" or "REJECTED" or Common.SILA_PO_RECEIVED)
                {
                    remainingPoPool[poi.Id] = 0m;
                    continue;
                }
                decimal alreadyAlloc = existingPoAllocs
                    .Where(a => a.PurchaseOrderItemId == poi.Id && a.ProcurementPlanId != plan.Id)
                    .Sum(a => a.RemainingQty);
                decimal openQty = Math.Max(0m, poi.Quantity - poi.ReceivedQuantity - alreadyAlloc);
                remainingPoPool[poi.Id] = openQty;
            }

            List<SilaCulinaryProcurementLedgerEntryDto> ledgerEntries = new();
            Dictionary<string, SilaCulinaryProcurementConsolidatedPurchaseDto> consolidationMap = new();

            decimal totalReqVal = 0m;
            decimal totalNewPurchVal = 0m;
            decimal totalCommittedVal = 0m;
            int fullyReserved = 0;
            int poCovered = 0;
            int transferCovered = 0;
            int purchRequired = 0;
            int critShortages = 0;
            int delayedDeliveries = 0;
            decimal physReadyRatioSum = 0m;
            decimal projReadyRatioSum = 0m;

            DateTime nowUtc = DateTime.UtcNow;

            foreach (SilaCulinaryMrpLine ml in mrpLines)
            {
                DateTime reqBy = ml.RequiredDateUtc == default ? (plan.PreparationDeadlineUtc ?? nowUtc.AddDays(3)) : ml.RequiredDateUtc;
                List<string> lineEventCodes = ParseStringListJson(ml.ContributingEventsJson);
                string? lineEventCode = lineEventCodes.FirstOrDefault() ?? plan.PrimaryEventCode;
                SilaCulinaryEvent? lineEvent = !string.IsNullOrWhiteSpace(lineEventCode) && eventByCodeMap.TryGetValue(lineEventCode, out SilaCulinaryEvent? foundEv)
                    ? foundEv
                    : null;
                Guid? lineEventId = lineEvent?.Id ?? plan.PrimaryEventId;

                ItemBuyerMaster? mat = ml.MaterialId.HasValue ? matMap.GetValueOrDefault(ml.MaterialId.Value) : null;
                decimal gross = Math.Max(0m, ml.IndicativeNewProductionQty > 0 ? ml.IndicativeNewProductionQty : ml.GrossQty);
                decimal preparedCoverage = Math.Max(0m, ml.GrossQty - ml.IndicativeNewProductionQty);

                // Resolve destination kitchen / storage location
                InventoryLocation? destLoc = locations.FirstOrDefault(l =>
                    (!string.IsNullOrWhiteSpace(ml.StorageLocationCode) && l.StorageLocationCode == ml.StorageLocationCode)
                    || (!string.IsNullOrWhiteSpace(ml.KitchenName) && l.LocationName == ml.KitchenName))
                    ?? locations.FirstOrDefault(l => l.LocationType == Common.SILA_LOCATION_OUTLET || l.LocationType == "KITCHEN")
                    ?? locations.FirstOrDefault();

                Guid? destLocId = destLoc?.Id;

                decimal? localOnHand = null;
                decimal competingRes = 0m;
                decimal safetyStock = 0m;
                decimal? eligibleLocal = null;
                decimal proposedLocalRes = 0m;
                decimal activeEventRes = 0m;
                List<SilaCulinaryProcurementFefoBatchDto> fefoBatches = new();

                if (ml.MaterialId.HasValue && destLocId.HasValue && remainingLocationStock.ContainsKey((destLocId.Value, ml.MaterialId.Value)))
                {
                    var meta = locationMeta[(destLocId.Value, ml.MaterialId.Value)];
                    localOnHand = meta.OnHand;
                    competingRes = meta.Competing;
                    safetyStock = meta.Safety;
                    eligibleLocal = remainingLocationStock[(destLocId.Value, ml.MaterialId.Value)];

                    // Check existing active reservations already created for this plan/event
                    activeEventRes = activeReservations
                        .Where(r => r.ProcurementPlanId == plan.Id && r.MaterialId == ml.MaterialId.Value && r.LocationId == destLocId.Value)
                        .Sum(r => Math.Max(0m, r.ReservedQty - r.ReleasedQty));

                    decimal needAfterActive = Math.Max(0m, gross - activeEventRes);
                    decimal takeLocal = Math.Min(needAfterActive, eligibleLocal.Value);
                    remainingLocationStock[(destLocId.Value, ml.MaterialId.Value)] -= takeLocal;
                    proposedLocalRes = takeLocal;

                    // FEFO batches from GRN receipts for this material
                    var batchGroups = grnBatches
                        .Where(g => g.MaterialId == ml.MaterialId.Value && g.GoodsReceipt.LocationId == destLocId.Value && !string.IsNullOrWhiteSpace(g.BatchNumber))
                        .GroupBy(g => new { g.BatchNumber, g.ExpiryDate })
                        .Select(grp => new
                        {
                            grp.Key.BatchNumber,
                            grp.Key.ExpiryDate,
                            Qty = grp.Sum(x => Math.Max(0m, x.AcceptedQty - x.ReversedQty)),
                        })
                        .Where(x => x.Qty > 0)
                        .OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue)
                        .ToList();

                    decimal remFefo = takeLocal;
                    foreach (var bg in batchGroups)
                    {
                        bool expired = bg.ExpiryDate.HasValue && bg.ExpiryDate.Value <= reqBy;
                        decimal rec = expired ? 0m : Math.Min(remFefo, bg.Qty);
                        if (!expired) remFefo -= rec;
                        fefoBatches.Add(new SilaCulinaryProcurementFefoBatchDto
                        {
                            BatchNumber = bg.BatchNumber,
                            ExpiryDateUtc = bg.ExpiryDate,
                            RemainingShelfLifeDays = bg.ExpiryDate.HasValue ? (int)(bg.ExpiryDate.Value - reqBy).TotalDays : null,
                            AvailableQty = bg.Qty,
                            RecommendedQty = rec,
                            QualityStatus = expired ? "EXPIRED_EXCLUDED" : (bg.ExpiryDate.HasValue ? "ELIGIBLE" : "UNKNOWN_EXPIRY"),
                            LocationName = destLoc?.LocationName,
                            StorageLocationCode = destLoc?.StorageLocationCode,
                        });
                    }
                }
                else
                {
                    AddException(plan.Id, null, lineEventCode, ml.MaterialCode, ml.MaterialDescription,
                        "UNKNOWN_AVAILABILITY", "WARNING", gross, reqBy, "Store Manager",
                        $"No authoritative inventory balance found at destination {destLoc?.LocationName ?? "kitchen"} for {ml.MaterialDescription}.",
                        "Verify storage location balance before committing reservations.");
                }

                decimal remainingNeed = Math.Max(0m, gross - activeEventRes - proposedLocalRes);

                // Incoming PO evaluation (only POs arriving on/before required date)
                decimal poAllocated = 0m;
                if (remainingNeed > 0 && !string.IsNullOrWhiteSpace(ml.MaterialCode))
                {
                    foreach (PurchaseOrderItem poi in openPoItems
                        .Where(p => p.MaterialCode == ml.MaterialCode)
                        .OrderBy(p => p.PurchaseOrder.DeliveryDate ?? DateTime.MaxValue))
                    {
                        decimal availPo = remainingPoPool.GetValueOrDefault(poi.Id, 0m);
                        if (availPo <= 0) continue;

                        DateTime? delDate = poi.PurchaseOrder.DeliveryDate;
                        if (delDate.HasValue && delDate.Value > reqBy)
                        {
                            delayedDeliveries++;
                            AddException(plan.Id, null, lineEventCode, ml.MaterialCode, ml.MaterialDescription,
                                "LATE_INCOMING_PO", "WARNING", availPo, reqBy, "Procurement Manager",
                                $"Incoming PO {poi.PurchaseOrder.PoNumber} delivers on {delDate.Value:yyyy-MM-dd}, after deadline {reqBy:yyyy-MM-dd}; excluded from timely coverage.",
                                "Expedite supplier delivery or procure alternative supply.");
                            continue;
                        }

                        decimal takePo = Math.Min(remainingNeed, availPo);
                        remainingPoPool[poi.Id] -= takePo;
                        poAllocated += takePo;
                        remainingNeed -= takePo;

                        _repository.SilaCulinaryProcurementAllocation.Create(new SilaCulinaryProcurementAllocation
                        {
                            Id = Guid.NewGuid(),
                            BuyerId = buyer.Id,
                            ProcurementPlanId = plan.Id,
                            EventId = lineEventId ?? Guid.Empty,
                            MrpRunId = mrp.Id,
                            MrpVersion = mrp.VersionNumber,
                            MaterialId = ml.MaterialId,
                            MaterialCode = ml.MaterialCode,
                            MaterialDescription = ml.MaterialDescription,
                            AllocationType = "INCOMING_PO",
                            Status = "ALLOCATED",
                            DestinationLocationId = destLocId,
                            DestinationLocationName = destLoc?.LocationName,
                            PurchaseOrderId = poi.PurchaseOrderId,
                            PurchaseOrderItemId = poi.Id,
                            PoNumber = poi.PurchaseOrder.PoNumber,
                            PoItemNumber = poi.LineNumber,
                            SupplierId = poi.PurchaseOrder.SupplierId,
                            SupplierName = poi.PurchaseOrder.SupplierName,
                            AllocatedQty = takePo,
                            RemainingQty = takePo,
                            Uom = ml.BaseUom,
                            UnitPrice = poi.UnitPrice,
                            EstimatedValue = poi.UnitPrice.HasValue ? decimal.Round(takePo * poi.UnitPrice.Value, 2) : null,
                            Currency = poi.Currency ?? plan.Currency,
                            ExpectedDateUtc = delDate,
                            DateCreated = DateTime.UtcNow,
                            DateUpdated = DateTime.UtcNow,
                            IsActive = true,
                        });
                        totalCommittedVal += poi.UnitPrice.HasValue ? takePo * poi.UnitPrice.Value : 0m;
                        if (remainingNeed <= 0) break;
                    }
                }

                // Other location ITO candidates (central store, other outlets, kitchens)
                decimal proposedItoQty = 0m;
                List<SilaCulinaryProcurementItoCandidateDto> itoCandidates = new();
                if (ml.MaterialId.HasValue && destLoc != null)
                {
                    foreach (InventoryLocation srcLoc in locations.Where(l => l.Id != destLoc.Id))
                    {
                        if (!remainingLocationStock.TryGetValue((srcLoc.Id, ml.MaterialId.Value), out decimal srcAvail) || srcAvail <= 0)
                            continue;
                        var srcMeta = locationMeta[(srcLoc.Id, ml.MaterialId.Value)];
                        bool samePlant = true; // Within same property/plant for 311; cross-plant checked on draft
                        decimal takeIto = remainingNeed > 0 ? Math.Min(remainingNeed, srcAvail) : 0m;
                        if (takeIto > 0)
                        {
                            remainingLocationStock[(srcLoc.Id, ml.MaterialId.Value)] -= takeIto;
                            proposedItoQty += takeIto;
                            if (includeProposedItoInNet)
                            {
                                remainingNeed -= takeIto;
                            }
                        }

                        itoCandidates.Add(new SilaCulinaryProcurementItoCandidateDto
                        {
                            SourceLocationId = srcLoc.Id,
                            SourceLocationName = srcLoc.LocationName,
                            SourceStorageLocation = srcLoc.StorageLocationCode,
                            SourcePlantCode = plan.PlantCode,
                            SourceType = srcLoc.LocationType,
                            AvailableQty = srcAvail,
                            SafetyStockQty = srcMeta.Safety,
                            CompetingReservationsQty = srcMeta.Competing,
                            RecommendedTransferQty = takeIto,
                            SamePlant311Eligible = samePlant,
                        });
                    }
                }

                decimal confirmedCoverage = activeEventRes + proposedLocalRes + poAllocated;
                decimal remainingConfirmedGap = Math.Max(0m, gross - confirmedCoverage);
                decimal netPurchase = Math.Max(0m, gross - confirmedCoverage - (includeProposedItoInNet ? proposedItoQty : 0m));

                // Check Weekly Bucket deduplication (§33)
                WeeklyBucketItem? linkedBucket = ml.MaterialId.HasValue
                    ? bucketItems.FirstOrDefault(b => b.MaterialId == ml.MaterialId.Value && (!destLoc?.OutletId.HasValue ?? true || b.OutletId == destLoc!.OutletId))
                    : null;

                // Supplier comparison (§26-§30)
                List<SilaCulinaryProcurementSupplierComparisonDto> supComparisons = new();
                Guid? recSupId = null;
                string? recSupCode = null;
                string? recSupName = null;
                string? recContractRef = null;
                decimal? recUnitPrice = mat?.UnitCost ?? mat?.StandardPrice ?? ml.UnitCost;
                decimal recPurchaseQty = 0m;
                decimal expectedExcess = 0m;
                decimal? packSize = null;
                decimal? moq = null;
                int? numPacks = null;
                DateTime? expectedDelUtc = null;
                DateTime? latestOrderUtc = null;
                bool delFeasible = true;

                if (netPurchase > 0)
                {
                    foreach (SilaSupplier sup in suppliers)
                    {
                        PredefinedContract? activeContract = contracts.FirstOrDefault(c => c.SupplierId == sup.Id);
                        bool hasCatMap = ml.MaterialId.HasValue && catMappings.Any(cm => cm.MaterialId == ml.MaterialId.Value);
                        if (activeContract == null && !hasCatMap && suppliers.Count > 1 && sup != suppliers[0])
                        {
                            // Still include as eligible supplier candidate
                        }

                        decimal? price = recUnitPrice ?? ml.UnitCost;
                        decimal pSize = 1m;
                        if (mat != null && !string.IsNullOrWhiteSpace(mat.OrderUnitOfMeasure))
                        {
                            decimal? conv = UomConverter.TryToBase(mat, 1m, mat.OrderUnitOfMeasure, uomConversions);
                            if (conv.HasValue && conv.Value > 0m) pSize = conv.Value;
                        }
                        decimal minOrder = 0m;
                        int leadDays = 1;
                        decimal qty = RoundPackQty(netPurchase, pSize, minOrder, out int pCnt, out decimal exQty);
                        bool feas = IsDeliveryFeasible(nowUtc, reqBy, leadDays, 4, out DateTime expDel, out DateTime latOrd);

                        supComparisons.Add(new SilaCulinaryProcurementSupplierComparisonDto
                        {
                            SupplierId = sup.Id,
                            SupplierCode = sup.SupplierCode,
                            SupplierName = sup.Name,
                            ContractReference = activeContract?.ContractNumber,
                            ContractMandatory = activeContract != null,
                            PurchasingUom = mat?.OrderUnitOfMeasure ?? ml.BaseUom,
                            UnitPrice = price,
                            BaseUnitPrice = price,
                            Currency = sup.Currency ?? plan.Currency,
                            PackSizeQty = pSize,
                            MoqQty = minOrder,
                            LeadTimeDays = leadDays,
                            EstimatedDeliveryDateUtc = expDel,
                            LatestOrderReleaseDateUtc = latOrd,
                            RecommendedPurchaseQty = qty,
                            ExpectedExcessQty = exQty,
                            TotalEstimatedCost = price.HasValue ? decimal.Round(qty * price.Value, 2) : null,
                            DeliveryFeasible = feas,
                        });
                    }

                    // Select best feasible supplier: contract mandatory first, then feasible, then lowest cost
                    SilaCulinaryProcurementSupplierComparisonDto? best = supComparisons
                        .OrderBy(s => s.ContractMandatory ? 0 : 1)
                        .ThenBy(s => s.DeliveryFeasible ? 0 : 1)
                        .ThenBy(s => s.TotalEstimatedCost ?? decimal.MaxValue)
                        .FirstOrDefault();

                    if (best != null)
                    {
                        best.IsSelected = true;
                        recSupId = best.SupplierId;
                        recSupCode = best.SupplierCode;
                        recSupName = best.SupplierName;
                        recContractRef = best.ContractReference;
                        recUnitPrice = best.UnitPrice;
                        recPurchaseQty = best.RecommendedPurchaseQty;
                        expectedExcess = best.ExpectedExcessQty;
                        packSize = best.PackSizeQty;
                        moq = best.MoqQty;
                        numPacks = (int)Math.Ceiling(recPurchaseQty / (best.PackSizeQty > 0 ? best.PackSizeQty : 1m));
                        expectedDelUtc = best.EstimatedDeliveryDateUtc;
                        latestOrderUtc = best.LatestOrderReleaseDateUtc;
                        delFeasible = best.DeliveryFeasible;
                    }
                    else
                    {
                        recPurchaseQty = RoundPackQty(netPurchase, 1m, 0m, out int pCnt, out expectedExcess);
                        packSize = 1m;
                        moq = 0m;
                        numPacks = pCnt;
                        AddException(plan.Id, null, lineEventCode, ml.MaterialCode, ml.MaterialDescription,
                            "MISSING_SUPPLIER", "CRITICAL", netPurchase, reqBy, "Procurement Manager",
                            $"No active approved supplier configured for {ml.MaterialDescription}.",
                            "Assign an approved supplier or contract in Supplier Master.");
                    }

                    if (!delFeasible)
                    {
                        critShortages++;
                        AddException(plan.Id, null, lineEventCode, ml.MaterialCode, ml.MaterialDescription,
                            "DELIVERY_DEADLINE_INFEASIBLE", "BLOCKING", netPurchase, reqBy, "Procurement Manager",
                            $"Supplier {recSupName ?? "N/A"} cannot deliver {ml.MaterialDescription} before {reqBy:yyyy-MM-dd HH:mm} UTC.",
                            "Consider internal stock transfer, supplier expedition, or chef-approved substitution.");
                    }
                }

                decimal? estPurchValue = recUnitPrice.HasValue && recPurchaseQty > 0
                    ? decimal.Round(recPurchaseQty * recUnitPrice.Value, 2)
                    : null;
                if (estPurchValue.HasValue) totalNewPurchVal += estPurchValue.Value;
                if (ml.RequirementValue.HasValue) totalReqVal += ml.RequirementValue.Value;

                string coverageStatus = netPurchase == 0 && remainingConfirmedGap == 0
                    ? (poAllocated > 0 ? "INCOMING_PO_ALLOCATED" : "STOCK_RESERVED")
                    : (proposedItoQty > 0 && netPurchase == 0 ? "ITO_PROPOSED" : "PURCHASE_RECOMMENDED");

                string readinessStatus = (activeEventRes + proposedLocalRes) >= gross
                    ? "MATERIAL_READY"
                    : (confirmedCoverage >= gross ? "INCOMING_PO_ALLOCATED"
                    : (!delFeasible ? "DELIVERY_RISK" : "MATERIAL_SHORTAGE"));

                if ((activeEventRes + proposedLocalRes) >= gross) fullyReserved++;
                if (poAllocated > 0) poCovered++;
                if (proposedItoQty > 0) transferCovered++;
                if (netPurchase > 0) purchRequired++;
                if (netPurchase > 0 && !delFeasible) critShortages++;

                physReadyRatioSum += gross > 0 ? Math.Min(1m, (activeEventRes + proposedLocalRes) / gross) : 1m;
                projReadyRatioSum += gross > 0 ? Math.Min(1m, (confirmedCoverage + proposedItoQty + recPurchaseQty) / gross) : 1m;

                SilaCulinaryProcurementLine lineEntity = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ProcurementPlanId = plan.Id,
                    EventId = lineEventId,
                    EventCode = lineEventCode,
                    EventName = lineEvent?.Name,
                    MrpLineId = ml.Id,
                    MaterialId = ml.MaterialId,
                    MaterialCode = ml.MaterialCode,
                    MaterialDescription = ml.MaterialDescription,
                    MaterialCategory = ml.MaterialCategory,
                    BaseUom = ml.BaseUom,
                    PurchasingUom = mat?.OrderUnitOfMeasure ?? ml.BaseUom,
                    DestinationLocationId = destLocId,
                    DestinationKitchenName = destLoc?.LocationName ?? ml.KitchenName,
                    DestinationStorageLocation = destLoc?.StorageLocationCode ?? ml.StorageLocationCode,
                    RequiredByUtc = reqBy,
                    GrossRequirementQty = gross,
                    LocalOnHandQty = localOnHand,
                    CompetingReservationsQty = competingRes,
                    SafetyStockQty = safetyStock,
                    EligibleLocalStockQty = eligibleLocal,
                    ActiveEventReservationQty = activeEventRes,
                    ProposedReservationQty = proposedLocalRes,
                    PreparedStockCoverageQty = preparedCoverage,
                    IncomingPoAllocatedQty = poAllocated,
                    ProposedItoQty = proposedItoQty,
                    ConfirmedCoverageQty = confirmedCoverage,
                    RemainingConfirmedGapQty = remainingConfirmedGap,
                    NetUncoveredQty = netPurchase,
                    RecommendedPurchaseQty = recPurchaseQty,
                    ExpectedExcessQty = expectedExcess,
                    PackSizeQty = packSize,
                    MoqQty = moq,
                    NumberOfPacks = numPacks,
                    RecommendedSupplierId = recSupId,
                    RecommendedSupplierCode = recSupCode,
                    RecommendedSupplierName = recSupName,
                    ContractReference = recContractRef,
                    SupplierUnitPrice = recUnitPrice,
                    EstimatedPurchaseValue = estPurchValue,
                    Currency = plan.Currency,
                    ExpectedDeliveryDateUtc = expectedDelUtc,
                    LatestOrderReleaseDateUtc = latestOrderUtc,
                    IsDeliveryFeasible = delFeasible,
                    CoverageStatus = coverageStatus,
                    ReadinessStatus = readinessStatus,
                    WeeklyBucketLinked = linkedBucket != null,
                    WeeklyBucketItemId = linkedBucket?.Id,
                    SupplierComparisonsJson = JsonSerializer.Serialize(supComparisons, CamelJson),
                    FefoBatchesJson = JsonSerializer.Serialize(fefoBatches, CamelJson),
                    SourceDetailsJson = JsonSerializer.Serialize(itoCandidates, CamelJson),
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                };
                _repository.SilaCulinaryProcurementLine.Create(lineEntity);

                // Record ledger entries for time-phased auditability
                if (localOnHand.HasValue)
                {
                    ledgerEntries.Add(new SilaCulinaryProcurementLedgerEntryDto
                    {
                        TimestampUtc = nowUtc.ToString("O"),
                        MaterialCode = ml.MaterialCode ?? ml.MaterialDescription,
                        EventType = "ELIGIBLE_LOCAL_STOCK",
                        SourceReference = destLoc?.LocationName ?? "Local Store",
                        DeltaQty = eligibleLocal ?? 0m,
                        RunningProjectedQty = eligibleLocal ?? 0m,
                        Confirmed = true,
                        Notes = $"On-hand {localOnHand.Value:0.##} - Competing {competingRes:0.##} - Safety {safetyStock:0.##}",
                    });
                }
                if (poAllocated > 0)
                {
                    ledgerEntries.Add(new SilaCulinaryProcurementLedgerEntryDto
                    {
                        TimestampUtc = (expectedDelUtc ?? reqBy).ToString("O"),
                        MaterialCode = ml.MaterialCode ?? ml.MaterialDescription,
                        EventType = "INCOMING_PO_ALLOCATION",
                        SourceReference = "Open Purchase Orders",
                        DeltaQty = poAllocated,
                        RunningProjectedQty = (eligibleLocal ?? 0m) + poAllocated,
                        Confirmed = true,
                        Notes = "Confirmed incoming PO before required date",
                    });
                }

                // Multi-event consolidation bucket (§32)
                if (netPurchase > 0 && !string.IsNullOrWhiteSpace(ml.MaterialCode))
                {
                    string key = $"{ml.MaterialCode}|{recSupCode ?? "UNASSIGNED"}|{plan.CompanyCode ?? ""}|{plan.PlantCode ?? ""}|{plan.Currency}";
                    if (!consolidationMap.TryGetValue(key, out SilaCulinaryProcurementConsolidatedPurchaseDto? bucket))
                    {
                        bucket = new SilaCulinaryProcurementConsolidatedPurchaseDto
                        {
                            ConsolidationKey = key,
                            MaterialCode = ml.MaterialCode,
                            MaterialDescription = ml.MaterialDescription,
                            SupplierId = recSupId,
                            SupplierName = recSupName ?? "Unassigned Supplier",
                            CompanyCode = plan.CompanyCode,
                            PlantCode = plan.PlantCode,
                            BaseUom = ml.BaseUom,
                            Currency = plan.Currency,
                        };
                        consolidationMap[key] = bucket;
                    }
                    bucket.CombinedNetQty += netPurchase;
                    bucket.EventShares.Add(new SilaCulinaryConsolidatedEventShareDto
                    {
                        EventId = lineEventId ?? Guid.Empty,
                        EventCode = lineEventCode ?? plan.PlanCode,
                        AllocatedQty = netPurchase,
                    });
                }
            }

            FlushExceptions(buyer.Id);

            foreach (var c in consolidationMap.Values)
            {
                c.RecommendedPurchaseQty = RoundPackQty(c.CombinedNetQty, 1m, 0m, out _, out decimal ex);
                c.ExpectedExcessQty = ex;
            }

            int lineCount = Math.Max(1, mrpLines.Count);
            plan.RequiredMaterialsCount = mrpLines.Count;
            plan.FullyReservedCount = fullyReserved;
            plan.IncomingPoCoveredCount = poCovered;
            plan.TransferCoveredCount = transferCovered;
            plan.PurchaseRequiredCount = purchRequired;
            plan.CriticalShortageCount = critShortages;
            plan.DelayedDeliveryCount = delayedDeliveries;
            plan.EstimatedRequirementValue = decimal.Round(totalReqVal, 2);
            plan.EstimatedNewPurchaseValue = decimal.Round(totalNewPurchVal, 2);
            plan.CommittedProcurementValue = decimal.Round(totalCommittedVal, 2);
            plan.PhysicalReadinessPercent = decimal.Round((physReadyRatioSum / lineCount) * 100m, 2);
            plan.ProjectedReadinessPercent = decimal.Round((projReadyRatioSum / lineCount) * 100m, 2);
            plan.SupplyDemandLedgerJson = JsonSerializer.Serialize(ledgerEntries, CamelJson);
            plan.ConsolidatedPurchasesJson = JsonSerializer.Serialize(consolidationMap.Values.ToList(), CamelJson);
            plan.Phase3AHandoverJson = JsonSerializer.Serialize(BuildPhase3AHandoverSnapshot(plan, mrpLines), CamelJson);

            if (critShortages > 0)
            {
                await UpsertNotificationTaskAsync(
                    buyer.Id, userId, "CULINARY_CRITICAL_SHORTAGE",
                    $"CULINARY_PROC_SHORTAGE_{plan.Id}",
                    $"Critical shortages on {plan.PlanCode}",
                    $"{critShortages} material(s) have delivery or shortage risks requiring procurement review.",
                    SilaOperations.PRIORITY_CRITICAL,
                    plan.Id, plan.PlanCode, null, cancellationToken);
            }
        }

        private void AddException(
            Guid planId,
            Guid? lineId,
            string? eventCode,
            string? materialCode,
            string? materialDesc,
            string type,
            string severity,
            decimal qtyImpact,
            DateTime? reqDate,
            string role,
            string msg,
            string action)
        {
            // Lookup BuyerId from context via plan? We set BuyerId via tracked plan or pass buyerId cleanly.
            // Note: callers inside CalculatePlanLinesAsync have access to plan.BuyerId; wait—let's pass buyerId!
            _pendingExceptions.Add((planId, lineId, eventCode, materialCode, materialDesc, type, severity, qtyImpact, reqDate, role, msg, action));
        }

        private readonly List<(Guid PlanId, Guid? LineId, string? EventCode, string? MaterialCode, string? MaterialDesc, string Type, string Severity, decimal QtyImpact, DateTime? ReqDate, string Role, string Msg, string Action)> _pendingExceptions = new();

        private void FlushExceptions(Guid buyerId)
        {
            foreach (var e in _pendingExceptions)
            {
                _repository.SilaCulinaryProcurementException.Create(new SilaCulinaryProcurementException
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    ProcurementPlanId = e.PlanId,
                    ProcurementLineId = e.LineId,
                    EventCode = e.EventCode,
                    MaterialCode = e.MaterialCode,
                    MaterialDescription = e.MaterialDesc,
                    ExceptionType = e.Type,
                    Severity = e.Severity,
                    QuantityImpact = e.QtyImpact,
                    RequiredDateUtc = e.ReqDate,
                    ResponsibleRole = e.Role,
                    Message = e.Msg.Length > 500 ? e.Msg[..500] : e.Msg,
                    RecommendedAction = e.Action.Length > 500 ? e.Action[..500] : e.Action,
                    ResolutionStatus = "OPEN",
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                });
            }
            _pendingExceptions.Clear();
        }

        public async Task<SilaCulinaryStockReservationDto> CreateReservationAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryCreateReservationRequestDto request, CancellationToken cancellationToken)
        {
            if (request.Quantity <= 0)
                throw new BadRequestCustomException("Invalid quantity.", "Reservation quantity must be positive.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            if (plan.IsSimulation)
                throw new BadRequestCustomException("Simulation plan.", "Simulation plans cannot create operational stock reservations.");

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryStockReservation? existing = await _repository.SilaCulinaryStockReservation
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null) return ToReservationDto(existing);
            }

            SilaCulinaryProcurementLine line = await _repository.SilaCulinaryProcurementLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.Id == request.ProcurementLineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Procurement line not found.", "Procurement line was not found.");

            if (!line.MaterialId.HasValue)
                throw new BadRequestCustomException("Missing material.", "Material mapping is required before creating a stock reservation.");

            // Atomic concurrent reservation guard (§12): never allow total active reservations to exceed eligible stock
            SilaCulinaryStockReservation? createdReservation = null;
            lock (ReservationAtomicGate)
            {
                InventoryBalance? balance = _repository.InventoryBalance
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.LocationId == request.LocationId && x.MaterialId == line.MaterialId.Value && x.IsActive)
                    .FirstOrDefault();

                decimal onHand = balance?.OnHandQty ?? 0m;
                decimal safety = request.AllowSafetyStockOverride
                    ? 0m
                    : (_repository.InventoryLocationMaterial
                        .FindByCondition(x => x.LocationId == request.LocationId && x.MaterialId == line.MaterialId.Value && x.IsActive)
                        .Select(x => x.SafetyStock ?? 0m)
                        .FirstOrDefault());

                List<Guid> openItoIds = _repository.InternalTransferOrder
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.FromLocationId == request.LocationId
                        && (x.Status == Common.SILA_ITO_PENDING_APPROVAL || x.Status == Common.SILA_ITO_APPROVED))
                    .Select(x => x.Id)
                    .ToList();
                decimal itoOutReserved = openItoIds.Count > 0
                    ? _repository.InternalTransferOrderItem
                        .FindByCondition(x => x.IsActive && x.MaterialId == line.MaterialId.Value && openItoIds.Contains(x.InternalTransferOrderId))
                        .ToList()
                        .Sum(x => x.ApprovedQty > 0 ? x.ApprovedQty : x.RequestedQty)
                    : 0m;

                decimal existingActiveCulRes = _repository.SilaCulinaryStockReservation
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.LocationId == request.LocationId && x.MaterialId == line.MaterialId.Value && x.IsActive
                        && (x.Status == "ACTIVE" || x.Status == "PARTIALLY_FULFILLED"))
                    .ToList()
                    .Sum(x => Math.Max(0m, x.ReservedQty - x.ReleasedQty - x.FulfilledQty));

                decimal allocatable = Math.Max(0m, onHand - itoOutReserved - existingActiveCulRes - safety);
                decimal grantQty = request.ActivateImmediately ? Math.Min(request.Quantity, allocatable) : request.Quantity;

                if (request.ActivateImmediately && grantQty <= 0m)
                {
                    AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RESERVATION, plan.Id, Common.SILA_AUDIT_CULINARY_RES_CONFLICT,
                        $"Reservation conflict for {line.MaterialCode}: requested {request.Quantity:0.##}, allocatable {allocatable:0.##}.", userId);
                    _repository.Save();
                    throw new ConflictCustomException(
                        "Insufficient allocatable stock.",
                        $"No eligible stock remains at location for {line.MaterialDescription} (on-hand {onHand:0.##}, existing reservations {(itoOutReserved + existingActiveCulRes):0.##}, safety stock {safety:0.##}).");
                }

                string status = request.ActivateImmediately
                    ? (grantQty < request.Quantity ? "ACTIVE" : "ACTIVE")
                    : "PROPOSED";

                createdReservation = new SilaCulinaryStockReservation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ReservationCode = $"RES-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                    ProcurementPlanId = plan.Id,
                    ProcurementLineId = line.Id,
                    EventId = line.EventId ?? plan.PrimaryEventId ?? Guid.Empty,
                    EventRevision = 1,
                    MrpRunId = plan.MrpRunId,
                    MrpVersion = plan.MrpVersion,
                    MaterialId = line.MaterialId.Value,
                    MaterialCode = line.MaterialCode ?? line.MaterialDescription,
                    MaterialDescription = line.MaterialDescription,
                    PropertyId = plan.PropertyId,
                    PlantCode = plan.PlantCode,
                    LocationId = request.LocationId,
                    StorageLocationCode = line.DestinationStorageLocation,
                    BatchNumber = request.BatchNumber,
                    BatchExpiryUtc = request.BatchExpiryUtc,
                    ReservedQty = grantQty,
                    BaseUom = line.BaseUom,
                    RequiredByUtc = line.RequiredByUtc,
                    ExpiresAtUtc = line.RequiredByUtc.AddDays(1),
                    Status = status,
                    SafetyStockOverrideUsed = request.AllowSafetyStockOverride,
                    SafetyOverrideReason = request.SafetyOverrideReason,
                    PriorityRank = request.PriorityRank,
                    IdempotencyKey = request.IdempotencyKey,
                    CreatedByUserId = userId,
                    CreatedByName = actorName,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                };
                _repository.SilaCulinaryStockReservation.Create(createdReservation);

                if (status == "ACTIVE")
                {
                    line.ActiveEventReservationQty += grantQty;
                    line.ConfirmedCoverageQty = line.ActiveEventReservationQty + line.IncomingPoAllocatedQty;
                    line.RemainingConfirmedGapQty = Math.Max(0m, line.GrossRequirementQty - line.ConfirmedCoverageQty);
                    line.ReadinessStatus = line.ActiveEventReservationQty >= line.GrossRequirementQty ? "STOCK_RESERVED" : line.ReadinessStatus;
                    line.DateUpdated = DateTime.UtcNow;
                }

                if (grantQty < request.Quantity)
                {
                    _repository.SilaCulinaryProcurementException.Create(new SilaCulinaryProcurementException
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        ProcurementPlanId = plan.Id,
                        ProcurementLineId = line.Id,
                        EventCode = line.EventCode,
                        MaterialCode = line.MaterialCode,
                        MaterialDescription = line.MaterialDescription,
                        ExceptionType = "RESERVATION_CONFLICT",
                        Severity = "WARNING",
                        QuantityImpact = request.Quantity - grantQty,
                        RequiredDateUtc = line.RequiredByUtc,
                        ResponsibleRole = "Store Manager",
                        Message = $"Partial reservation created ({grantQty:0.##} of {request.Quantity:0.##} {line.BaseUom}) due to competing demand.",
                        RecommendedAction = "Source remaining gap via ITO or PO.",
                        ResolutionStatus = "OPEN",
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow,
                        IsActive = true,
                    });
                }

                AddWorkflowEvent(
                    buyer.Id,
                    Common.SILA_REF_CULINARY_RESERVATION,
                    createdReservation.Id,
                    status == "ACTIVE" ? Common.SILA_AUDIT_CULINARY_RES_ACTIVE : Common.SILA_AUDIT_CULINARY_RES_PROPOSED,
                    $"Reservation {createdReservation.ReservationCode} ({status}) for {grantQty:0.##} {line.BaseUom} of {line.MaterialDescription}.",
                    userId);

                _repository.Save();
            }

            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return ToReservationDto(createdReservation!);
        }

        public async Task<SilaCulinaryStockReservationDto> TransitionReservationAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid reservationId, string action, string? reason, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            SilaCulinaryStockReservation res = await _repository.SilaCulinaryStockReservation
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.Id == reservationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Reservation not found.", "Stock reservation was not found.");

            string act = (action ?? "").Trim().ToUpperInvariant();
            switch (act)
            {
                case "ACTIVATE" or "APPROVE":
                    res.Status = "ACTIVE";
                    res.ApprovalReference = $"APR-{actorName}-{DateTime.UtcNow:yyyyMMddHHmm}";
                    AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RESERVATION, res.Id, Common.SILA_AUDIT_CULINARY_RES_ACTIVE, $"Activated reservation {res.ReservationCode}.", userId);
                    break;
                case "RELEASE":
                    res.Status = "RELEASED";
                    res.ReleasedQty = Math.Max(0m, res.ReservedQty - res.FulfilledQty);
                    AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RESERVATION, res.Id, Common.SILA_AUDIT_CULINARY_RES_RELEASED, $"Released reservation {res.ReservationCode}: {reason}", userId);
                    break;
                case "CANCEL":
                    res.Status = "CANCELLED";
                    res.ReleasedQty = Math.Max(0m, res.ReservedQty - res.FulfilledQty);
                    AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_RESERVATION, res.Id, Common.SILA_AUDIT_CULINARY_RES_RELEASED, $"Cancelled reservation {res.ReservationCode}: {reason}", userId);
                    break;
                case "FULFILL":
                    res.Status = "FULFILLED";
                    res.FulfilledQty = res.ReservedQty - res.ReleasedQty;
                    break;
                default:
                    throw new BadRequestCustomException("Unsupported reservation action.", $"Action '{action}' is not valid.");
            }
            res.DateUpdated = DateTime.UtcNow;
            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return ToReservationDto(res);
        }

        public async Task<SilaCulinaryProcurementAllocationDto> AllocateIncomingPoAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryAllocateIncomingPoRequestDto request, CancellationToken cancellationToken)
        {
            if (request.Quantity <= 0)
                throw new BadRequestCustomException("Invalid quantity.", "Allocation quantity must be positive.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            if (plan.IsSimulation)
                throw new BadRequestCustomException("Simulation plan.", "Simulation plans cannot allocate operational POs.");

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryProcurementAllocation? existing = await _repository.SilaCulinaryProcurementAllocation
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null) return ToAllocationDto(existing);
            }

            SilaCulinaryProcurementLine line = await _repository.SilaCulinaryProcurementLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.Id == request.ProcurementLineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Procurement line not found.", "Procurement line was not found.");

            PurchaseOrderItem poItem = await _repository.PurchaseOrderItem
                .FindByCondition(x => x.Id == request.PurchaseOrderItemId && x.IsActive && x.PurchaseOrder.BuyerId == buyer.Id && x.PurchaseOrder.IsActive)
                .Include(x => x.PurchaseOrder)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("PO item not found.", "Purchase order item was not found.");

            if (poItem.PurchaseOrder.Status is "CANCELLED" or "REJECTED" or Common.SILA_PO_RECEIVED)
                throw new BadRequestCustomException("Ineligible PO status.", $"PO {poItem.PurchaseOrder.PoNumber} has status {poItem.PurchaseOrder.Status}.");

            decimal existingAllocated = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.PurchaseOrderItemId == poItem.Id && x.IsActive && x.Status != "CANCELLED" && x.Status != "RELEASED")
                .SumAsync(x => x.RemainingQty, cancellationToken);

            decimal availablePoQty = Math.Max(0m, poItem.Quantity - poItem.ReceivedQuantity - existingAllocated);
            if (request.Quantity > availablePoQty)
                throw new ConflictCustomException("PO over-allocation prevented.", $"Only {availablePoQty:0.##} {poItem.UnitOfMeasure ?? line.BaseUom} remains unallocated on PO {poItem.PurchaseOrder.PoNumber}.");

            SilaCulinaryProcurementAllocation alloc = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProcurementPlanId = plan.Id,
                ProcurementLineId = line.Id,
                EventId = line.EventId ?? plan.PrimaryEventId ?? Guid.Empty,
                MrpRunId = plan.MrpRunId,
                MrpVersion = plan.MrpVersion,
                MaterialId = line.MaterialId,
                MaterialCode = line.MaterialCode,
                MaterialDescription = line.MaterialDescription,
                AllocationType = "INCOMING_PO",
                Status = "ALLOCATED",
                DestinationLocationId = line.DestinationLocationId,
                DestinationLocationName = line.DestinationKitchenName,
                PurchaseOrderId = poItem.PurchaseOrderId,
                PurchaseOrderItemId = poItem.Id,
                PoNumber = poItem.PurchaseOrder.PoNumber,
                PoItemNumber = poItem.LineNumber,
                SupplierId = poItem.PurchaseOrder.SupplierId,
                SupplierName = poItem.PurchaseOrder.SupplierName,
                AllocatedQty = request.Quantity,
                RemainingQty = request.Quantity,
                Uom = line.BaseUom,
                UnitPrice = poItem.UnitPrice,
                EstimatedValue = poItem.UnitPrice.HasValue ? decimal.Round(request.Quantity * poItem.UnitPrice.Value, 2) : null,
                Currency = poItem.Currency ?? plan.Currency,
                ExpectedDateUtc = poItem.PurchaseOrder.DeliveryDate,
                IdempotencyKey = request.IdempotencyKey,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            };
            _repository.SilaCulinaryProcurementAllocation.Create(alloc);
            line.IncomingPoAllocatedQty += request.Quantity;
            line.ConfirmedCoverageQty = line.ActiveEventReservationQty + line.IncomingPoAllocatedQty;
            line.RemainingConfirmedGapQty = Math.Max(0m, line.GrossRequirementQty - line.ConfirmedCoverageQty);
            line.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PO_ALLOCATED,
                $"Allocated {request.Quantity:0.##} {line.BaseUom} from PO {poItem.PurchaseOrder.PoNumber} item {poItem.LineNumber}.", userId);
            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return ToAllocationDto(alloc);
        }

        public async Task<SilaCulinaryProcurementAllocationDto> CreateItoDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryCreateItoDraftRequestDto request, CancellationToken cancellationToken)
        {
            if (request.Quantity <= 0)
                throw new BadRequestCustomException("Invalid quantity.", "Transfer quantity must be positive.");
            if (request.FromLocationId == request.ToLocationId)
                throw new BadRequestCustomException("Same storage location.", "Source and destination storage locations must differ.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            if (plan.IsSimulation)
                throw new BadRequestCustomException("Simulation plan.", "Simulation plans cannot create operational ITO drafts.");

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryProcurementAllocation? existing = await _repository.SilaCulinaryProcurementAllocation
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null) return ToAllocationDto(existing);
            }

            SilaCulinaryProcurementLine line = await _repository.SilaCulinaryProcurementLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.Id == request.ProcurementLineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Procurement line not found.", "Procurement line was not found.");

            if (!line.MaterialId.HasValue)
                throw new BadRequestCustomException("Missing material.", "Material mapping is required for an ITO transfer.");

            // Reuse existing CreateSilaTransferCommand (same-plant 311 validated, creates PENDING_APPROVAL ITO, never posts SAP directly)
            SilaTransferWriteDto itoDto = new()
            {
                FromLocationId = request.FromLocationId,
                ToLocationId = request.ToLocationId,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? $"Culinary Event {plan.PlanCode} / {line.EventCode}" : request.Reason.Trim(),
                RequiredBy = request.RequiredByUtc ?? line.RequiredByUtc,
                RequireSamePlant = true,
                Items = new List<SilaTransferLineWriteDto>
                {
                    new()
                    {
                        MaterialId = line.MaterialId.Value,
                        Quantity = request.Quantity,
                    }
                }
            };

            Guid itoId;
            if (_mediator != null)
            {
                itoId = await _mediator.Send(new CreateSilaTransferCommand
                {
                    OrganizationId = organizationId,
                    UserId = userId,
                    RoleId = roleId,
                    Request = itoDto,
                }, cancellationToken);
            }
            else
            {
                CreateSilaTransferCommandHandler handler = new(_repository, _logger);
                itoId = await handler.Handle(new CreateSilaTransferCommand
                {
                    OrganizationId = organizationId,
                    UserId = userId,
                    RoleId = roleId,
                    Request = itoDto,
                }, cancellationToken);
            }

            InternalTransferOrder ito = await _repository.InternalTransferOrder
                .FindByCondition(x => x.Id == itoId && x.BuyerId == buyer.Id && x.IsActive)
                .FirstAsync(cancellationToken);

            InventoryLocation? fromLoc = await _repository.InventoryLocation.FindByCondition(x => x.Id == request.FromLocationId).FirstOrDefaultAsync(cancellationToken);
            InventoryLocation? toLoc = await _repository.InventoryLocation.FindByCondition(x => x.Id == request.ToLocationId).FirstOrDefaultAsync(cancellationToken);

            SilaCulinaryProcurementAllocation alloc = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProcurementPlanId = plan.Id,
                ProcurementLineId = line.Id,
                EventId = line.EventId ?? plan.PrimaryEventId ?? Guid.Empty,
                MrpRunId = plan.MrpRunId,
                MrpVersion = plan.MrpVersion,
                MaterialId = line.MaterialId,
                MaterialCode = line.MaterialCode,
                MaterialDescription = line.MaterialDescription,
                AllocationType = "PROPOSED_ITO",
                Status = "PENDING_APPROVAL",
                SourceLocationId = request.FromLocationId,
                SourceLocationName = fromLoc?.LocationName,
                DestinationLocationId = request.ToLocationId,
                DestinationLocationName = toLoc?.LocationName,
                InternalTransferOrderId = ito.Id,
                ItoNumber = ito.ItoNumber,
                AllocatedQty = request.Quantity,
                RemainingQty = request.Quantity,
                Uom = line.BaseUom,
                ExpectedDateUtc = request.RequiredByUtc ?? line.RequiredByUtc,
                IdempotencyKey = request.IdempotencyKey,
                Notes = "Created via existing ITO 311 draft workflow (pending approval, not posted to SAP).",
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            };
            _repository.SilaCulinaryProcurementAllocation.Create(alloc);
            line.ReadinessStatus = "ITO_AWAITING_APPROVAL";
            line.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_ITO_DRAFT,
                $"Created ITO draft {ito.ItoNumber} ({request.Quantity:0.##} {line.BaseUom}) from {fromLoc?.LocationName} to {toLoc?.LocationName}.", userId);
            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return ToAllocationDto(alloc);
        }

        public async Task<SilaCulinaryProcurementAllocationDto> CreatePoDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryCreatePoDraftRequestDto request, CancellationToken cancellationToken)
        {
            if (request.ProcurementLineIds == null || request.ProcurementLineIds.Count == 0)
                throw new BadRequestCustomException("Lines required.", "Select at least one procurement line to create a PO draft.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            if (plan.IsSimulation)
                throw new BadRequestCustomException("Simulation plan.", "Simulation plans cannot create operational purchase orders.");

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryProcurementAllocation? existing = await _repository.SilaCulinaryProcurementAllocation
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null) return ToAllocationDto(existing);
            }

            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && request.ProcurementLineIds.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0)
                throw new NotFoundCustomException("Procurement lines not found.", "No matching procurement lines were found.");

            // Check duplicate PO draft on any of the lines when no explicit idempotency key differed
            List<Guid> lineIds = lines.Select(l => l.Id).ToList();
            SilaCulinaryProcurementAllocation? dupLineAlloc = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.AllocationType == "PO_DRAFT"
                    && x.ProcurementLineId.HasValue && lineIds.Contains(x.ProcurementLineId.Value)
                    && x.Status != "CANCELLED" && x.Status != "RELEASED" && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (dupLineAlloc != null)
            {
                return ToAllocationDto(dupLineAlloc);
            }

            SilaSupplier supplier = await _repository.SilaSupplier
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.SupplierId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Supplier not found.", "Approved supplier was not found.");

            // Resolve user's purchasing profile (§35) without forcing expense account assignment onto STOCK materials
            SilaUserPurchasingProfile? profile = await SilaUserPurchasingProfileRules.FindBestAsync(
                _repository, buyer.Id, userId, null, plan.PropertyId, null, lines[0].DestinationLocationId, plan.PlantCode, cancellationToken);

            List<Guid> matIds = lines.Where(l => l.MaterialId.HasValue).Select(l => l.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> matMap = matIds.Count > 0
                ? await _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, cancellationToken)
                : new Dictionary<Guid, ItemBuyerMaster>();

            bool anyRequiresAcct = lines.Any(l => l.MaterialId.HasValue && matMap.TryGetValue(l.MaterialId.Value, out var m) && SilaUserPurchasingProfileRules.RequiresAccountAssignment(m.InventoryType));
            string? acctCat = !string.IsNullOrWhiteSpace(request.AccountAssignmentCategory)
                ? request.AccountAssignmentCategory
                : (anyRequiresAcct ? profile?.AccountAssignmentCategory : null);
            string? costCenter = !string.IsNullOrWhiteSpace(request.CostCenter) ? request.CostCenter : (anyRequiresAcct ? profile?.CostCenter : null);
            string? glAccount = !string.IsNullOrWhiteSpace(request.GlAccount) ? request.GlAccount : (anyRequiresAcct ? profile?.GlAccount : null);

            CreatePurchaseOrderRequestDto poDto = new()
            {
                SupplierId = supplier.Id,
                SupplierCode = supplier.SupplierCode,
                CompanyCode = profile?.CompanyCode ?? plan.CompanyCode,
                PlantCode = profile?.PlantCode ?? plan.PlantCode,
                StorageLocation = profile?.StorageLocation ?? lines[0].DestinationStorageLocation,
                PurchasingOrganization = profile?.PurchasingOrganization,
                PurchasingGroup = profile?.PurchasingGroup,
                PurchaseOrderType = profile?.DefaultPoDocumentType ?? "NB",
                Currency = supplier.Currency ?? profile?.DefaultCurrency ?? plan.Currency,
                DeliveryDate = request.DeliveryDateUtc ?? lines.Min(l => l.ExpectedDeliveryDateUtc ?? l.RequiredByUtc),
                AccountAssignmentCategory = acctCat,
                GlAccount = glAccount,
                Lines = lines.Select(l => new CreatePurchaseOrderLineDto
                {
                    MaterialCode = l.MaterialCode,
                    Description = l.MaterialDescription,
                    Quantity = l.RecommendedPurchaseQty > 0 ? l.RecommendedPurchaseQty : Math.Max(1m, l.NetUncoveredQty),
                    UnitOfMeasure = l.BaseUom,
                    UnitPrice = l.SupplierUnitPrice,
                    StorageLocation = l.DestinationStorageLocation ?? profile?.StorageLocation,
                    CostCenter = costCenter,
                }).ToList(),
            };

            if (_mediator == null)
                throw new BadRequestCustomException("PO service mediator unavailable.", "Configure MediatR to invoke existing CreatePurchaseOrderCommand.");

            PurchaseOrderProcessResultDto poResult = await _mediator.Send(new CreatePurchaseOrderCommand
            {
                OrganizationId = organizationId,
                UserId = userId,
                SupplierName = supplier.Name,
                DeferErpUntilApproved = true, // NEVER auto-release or post to SAP
                Request = poDto,
            }, cancellationToken);

            PurchaseOrder createdPo = await _repository.PurchaseOrder
                .FindByCondition(x => x.Id == poResult.Id && x.IsActive)
                .Include(x => x.Items)
                .FirstAsync(cancellationToken);

            SilaCulinaryProcurementAllocation? firstAlloc = null;
            for (int i = 0; i < lines.Count; i++)
            {
                SilaCulinaryProcurementLine ln = lines[i];
                PurchaseOrderItem? poItem = createdPo.Items.OrderBy(it => it.LineNumber).Skip(i).FirstOrDefault();
                decimal qty = poItem?.Quantity ?? (ln.RecommendedPurchaseQty > 0 ? ln.RecommendedPurchaseQty : ln.NetUncoveredQty);

                SilaCulinaryProcurementAllocation alloc = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ProcurementPlanId = plan.Id,
                    ProcurementLineId = ln.Id,
                    EventId = ln.EventId ?? plan.PrimaryEventId ?? Guid.Empty,
                    MrpRunId = plan.MrpRunId,
                    MrpVersion = plan.MrpVersion,
                    MaterialId = ln.MaterialId,
                    MaterialCode = ln.MaterialCode,
                    MaterialDescription = ln.MaterialDescription,
                    AllocationType = "PO_DRAFT",
                    Status = "DRAFT_CREATED",
                    DestinationLocationId = ln.DestinationLocationId,
                    DestinationLocationName = ln.DestinationKitchenName,
                    PurchaseOrderId = createdPo.Id,
                    PurchaseOrderItemId = poItem?.Id,
                    PoNumber = createdPo.PoNumber,
                    PoItemNumber = poItem?.LineNumber ?? (i + 1),
                    SupplierId = supplier.Id,
                    SupplierName = supplier.Name,
                    ContractReference = request.ContractReference ?? ln.ContractReference,
                    AccountAssignmentCategory = acctCat,
                    CostCenter = costCenter,
                    GlAccount = glAccount,
                    InternalOrder = request.InternalOrder ?? profile?.InternalOrder,
                    WbsElement = request.WbsElement ?? profile?.WbsElement,
                    ProfitCenter = request.ProfitCenter ?? profile?.ProfitCenter,
                    AllocatedQty = qty,
                    RemainingQty = qty,
                    Uom = ln.PurchasingUom ?? ln.BaseUom,
                    UnitPrice = ln.SupplierUnitPrice,
                    EstimatedValue = ln.SupplierUnitPrice.HasValue ? decimal.Round(qty * ln.SupplierUnitPrice.Value, 2) : null,
                    Currency = createdPo.Currency ?? plan.Currency,
                    ExpectedDateUtc = createdPo.DeliveryDate,
                    IdempotencyKey = request.IdempotencyKey,
                    Notes = "PO Draft created with DeferErpUntilApproved=true (awaiting approval, not released to SAP).",
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                };
                _repository.SilaCulinaryProcurementAllocation.Create(alloc);
                firstAlloc ??= alloc;

                ln.ReadinessStatus = "PO_DRAFT_CREATED";
                ln.DateUpdated = DateTime.UtcNow;
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PO_DRAFT,
                $"Created PO draft {createdPo.PoNumber} for supplier {supplier.Name} (DeferErpUntilApproved=true).", userId);
            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return ToAllocationDto(firstAlloc!);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> ReconcileGrnAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryReconcileGrnRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);

            GoodsReceipt grn = await _repository.GoodsReceipt
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.GoodsReceiptId && x.IsActive)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Goods receipt not found.", "GRN was not found.");

            List<SilaCulinaryProcurementAllocation> poAllocs = await _repository.SilaCulinaryProcurementAllocation
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive
                    && (x.AllocationType == "INCOMING_PO" || x.AllocationType == "PO_DRAFT")
                    && x.PurchaseOrderId == grn.PurchaseOrderId)
                .ToListAsync(cancellationToken);

            foreach (GoodsReceiptItem item in grn.Items.Where(i => i.IsActive))
            {
                SilaCulinaryProcurementAllocation? alloc = poAllocs.FirstOrDefault(a => a.PurchaseOrderItemId == item.PurchaseOrderItemId)
                    ?? poAllocs.FirstOrDefault(a => a.MaterialCode == item.MaterialCode);
                if (alloc == null) continue;

                if (!request.IsReversal)
                {
                    decimal netAccepted = Math.Max(0m, item.AcceptedQty - item.ReversedQty);
                    decimal applied = Math.Min(netAccepted, alloc.AllocatedQty);
                    alloc.ReceivedQty = applied;
                    alloc.RemainingQty = Math.Max(0m, alloc.AllocatedQty - applied);
                    alloc.GoodsReceiptId = grn.Id;
                    alloc.GrnNumber = grn.GrnNumber;
                    alloc.SapMaterialDocument = grn.MaterialDocument;
                    alloc.Status = alloc.RemainingQty == 0 ? "RECEIVED" : "PARTIALLY_RECEIVED";
                    alloc.DateUpdated = DateTime.UtcNow;

                    // Update line readiness
                    if (alloc.ProcurementLineId.HasValue)
                    {
                        SilaCulinaryProcurementLine? ln = await _repository.SilaCulinaryProcurementLine
                            .FindByConditionTracked(x => x.Id == alloc.ProcurementLineId.Value && x.IsActive)
                            .FirstOrDefaultAsync(cancellationToken);
                        if (ln != null)
                        {
                            ln.ReadinessStatus = alloc.RemainingQty == 0 ? "RECEIVED" : "PARTIALLY_RECEIVED";
                            ln.DateUpdated = DateTime.UtcNow;
                        }
                    }

                    // Convert eligible received stock to an ACTIVE physical reservation if inventory supports it
                    if (applied > 0 && alloc.MaterialId.HasValue && alloc.DestinationLocationId.HasValue)
                    {
                        _repository.SilaCulinaryStockReservation.Create(new SilaCulinaryStockReservation
                        {
                            Id = Guid.NewGuid(),
                            BuyerId = buyer.Id,
                            ReservationCode = $"RES-GRN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}",
                            ProcurementPlanId = plan.Id,
                            ProcurementLineId = alloc.ProcurementLineId,
                            EventId = alloc.EventId,
                            MrpRunId = alloc.MrpRunId,
                            MrpVersion = alloc.MrpVersion,
                            MaterialId = alloc.MaterialId.Value,
                            MaterialCode = alloc.MaterialCode ?? "",
                            MaterialDescription = alloc.MaterialDescription,
                            PropertyId = plan.PropertyId,
                            PlantCode = plan.PlantCode,
                            LocationId = alloc.DestinationLocationId.Value,
                            BatchNumber = item.BatchNumber,
                            BatchExpiryUtc = item.ExpiryDate,
                            ReservedQty = applied,
                            BaseUom = alloc.Uom,
                            RequiredByUtc = plan.PreparationDeadlineUtc ?? DateTime.UtcNow.AddDays(1),
                            Status = "ACTIVE",
                            ApprovalReference = $"GRN:{grn.GrnNumber}",
                            CreatedByUserId = userId,
                            CreatedByName = actorName,
                            DateCreated = DateTime.UtcNow,
                            DateUpdated = DateTime.UtcNow,
                            IsActive = true,
                        });
                    }
                }
                else
                {
                    // §43 GRN Reversal Reconciliation (affects only linked allocations, preserves historical GRN)
                    decimal revQty = item.ReversedQty > 0 ? item.ReversedQty : item.AcceptedQty;
                    decimal delta = Math.Min(revQty, alloc.ReceivedQty);
                    alloc.ReceivedQty = Math.Max(0m, alloc.ReceivedQty - delta);
                    alloc.ReversedQty += delta;
                    alloc.RemainingQty = Math.Max(0m, alloc.AllocatedQty - alloc.ReceivedQty);
                    alloc.ReversalMaterialDocument = request.ReversalMaterialDocument ?? grn.ReversalMaterialDocument;
                    alloc.Status = alloc.ReceivedQty == 0 ? "REVERSED" : "PARTIALLY_RECEIVED";
                    alloc.DateUpdated = DateTime.UtcNow;

                    // Release any GRN-derived reservations tied to this GRN
                    string grnTag = $"GRN:{grn.GrnNumber}";
                    List<SilaCulinaryStockReservation> linkedRes = await _repository.SilaCulinaryStockReservation
                        .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id
                            && x.ApprovalReference == grnTag && x.MaterialId == alloc.MaterialId && x.IsActive && x.Status == "ACTIVE")
                        .ToListAsync(cancellationToken);
                    foreach (var r in linkedRes)
                    {
                        r.Status = "RELEASED";
                        r.ReleasedQty = r.ReservedQty;
                        r.DateUpdated = DateTime.UtcNow;
                    }

                    if (alloc.ProcurementLineId.HasValue)
                    {
                        SilaCulinaryProcurementLine? ln = await _repository.SilaCulinaryProcurementLine
                            .FindByConditionTracked(x => x.Id == alloc.ProcurementLineId.Value && x.IsActive)
                            .FirstOrDefaultAsync(cancellationToken);
                        if (ln != null)
                        {
                            ln.ReadinessStatus = "MATERIAL_SHORTAGE";
                            ln.DateUpdated = DateTime.UtcNow;
                        }
                    }

                    _repository.SilaCulinaryProcurementException.Create(new SilaCulinaryProcurementException
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        ProcurementPlanId = plan.Id,
                        ProcurementLineId = alloc.ProcurementLineId,
                        EventCode = plan.PrimaryEventCode,
                        MaterialCode = alloc.MaterialCode,
                        MaterialDescription = alloc.MaterialDescription,
                        ExceptionType = "GRN_REVERSAL",
                        Severity = "CRITICAL",
                        QuantityImpact = delta,
                        ResponsibleRole = "Procurement Manager",
                        Message = $"GRN {grn.GrnNumber} reversed ({delta:0.##} {alloc.Uom}); coverage reopened.",
                        RecommendedAction = "Reallocate stock or issue replacement supply before event deadline.",
                        ResolutionStatus = "OPEN",
                        DateCreated = DateTime.UtcNow,
                        DateUpdated = DateTime.UtcNow,
                        IsActive = true,
                    });

                    await UpsertNotificationTaskAsync(
                        buyer.Id, userId, "CULINARY_GRN_REVERSAL",
                        $"CULINARY_GRN_REV_{grn.Id}_{alloc.Id}",
                        $"GRN {grn.GrnNumber} reversed — coverage reopened",
                        $"Reversed {delta:0.##} {alloc.Uom} of {alloc.MaterialDescription} for plan {plan.PlanCode}.",
                        SilaOperations.PRIORITY_CRITICAL, plan.Id, plan.PlanCode, alloc.DestinationLocationId, cancellationToken);
                }
            }

            AddWorkflowEvent(
                buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id,
                request.IsReversal ? Common.SILA_AUDIT_CULINARY_GRN_REV_REC : Common.SILA_AUDIT_CULINARY_GRN_REC,
                $"{(request.IsReversal ? "GRN reversal" : "GRN receipt")} reconciled for {grn.GrnNumber}.", userId);
            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> ReconcileItoReversalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryReconcileItoReversalRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);

            List<SilaCulinaryProcurementAllocation> itoAllocs = await _repository.SilaCulinaryProcurementAllocation
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id
                    && x.InternalTransferOrderId == request.InternalTransferOrderId && x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var alloc in itoAllocs)
            {
                decimal rev = Math.Min(request.ReversedQty > 0 ? request.ReversedQty : alloc.AllocatedQty, alloc.AllocatedQty);
                alloc.ReversedQty += rev;
                alloc.RemainingQty = Math.Max(0m, alloc.AllocatedQty - alloc.ReversedQty);
                alloc.ReversalMaterialDocument = request.ReversalMaterialDocument;
                alloc.Status = "REVERSED";
                alloc.Notes = $"SAP 312 ITO Reversal reconciled: {request.Reason}";
                alloc.DateUpdated = DateTime.UtcNow;

                if (alloc.ProcurementLineId.HasValue)
                {
                    SilaCulinaryProcurementLine? ln = await _repository.SilaCulinaryProcurementLine
                        .FindByConditionTracked(x => x.Id == alloc.ProcurementLineId.Value && x.IsActive)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (ln != null)
                    {
                        ln.IncomingTransferAllocatedQty = Math.Max(0m, ln.IncomingTransferAllocatedQty - rev);
                        ln.ReadinessStatus = "MATERIAL_SHORTAGE";
                        ln.CoverageStatus = "PURCHASE_RECOMMENDED";
                        ln.NetUncoveredQty += rev;
                        ln.RecommendedPurchaseQty = RoundPackQty(ln.NetUncoveredQty, ln.PackSizeQty ?? 1m, ln.MoqQty ?? 0m, out _, out decimal ex);
                        ln.ExpectedExcessQty = ex;
                        ln.DateUpdated = DateTime.UtcNow;
                    }
                }

                _repository.SilaCulinaryProcurementException.Create(new SilaCulinaryProcurementException
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ProcurementPlanId = plan.Id,
                    ProcurementLineId = alloc.ProcurementLineId,
                    EventCode = plan.PrimaryEventCode,
                    MaterialCode = alloc.MaterialCode,
                    MaterialDescription = alloc.MaterialDescription,
                    ExceptionType = "ITO_REVERSAL",
                    Severity = "CRITICAL",
                    QuantityImpact = rev,
                    ResponsibleRole = "Store Manager",
                    Message = $"SAP 312 transfer reversal ({request.ReversalMaterialDocument ?? "N/A"}) reopened {rev:0.##} {alloc.Uom} requirement.",
                    RecommendedAction = "Reallocate local stock, propose replacement ITO, or generate purchase recommendation.",
                    ResolutionStatus = "OPEN",
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                });

                await UpsertNotificationTaskAsync(
                    buyer.Id, userId, "CULINARY_ITO_REVERSAL",
                    $"CULINARY_ITO_REV_{request.InternalTransferOrderId}_{alloc.Id}",
                    $"SAP 312 transfer reversal — coverage reopened",
                    $"Reversed {rev:0.##} {alloc.Uom} of {alloc.MaterialDescription} for plan {plan.PlanCode}.",
                    SilaOperations.PRIORITY_CRITICAL, plan.Id, plan.PlanCode, alloc.DestinationLocationId, cancellationToken);
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_ITO_REV,
                $"Reconciled SAP 312 reversal ({request.ReversalMaterialDocument}) for ITO {request.InternalTransferOrderId}.", userId);
            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> LinkWeeklyBucketAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid lineId, Guid weeklyBucketItemId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            SilaCulinaryProcurementLine line = await _repository.SilaCulinaryProcurementLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.Id == lineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Line not found.", "Procurement line was not found.");

            WeeklyBucketItem wbItem = await _repository.WeeklyBucketItem
                .FindByCondition(x => x.Id == weeklyBucketItemId && x.IsActive && x.WeeklyBucket.BuyerId == buyer.Id)
                .Include(x => x.WeeklyBucket)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Weekly bucket item not found.", "Weekly bucket line was not found.");

            line.WeeklyBucketLinked = true;
            line.WeeklyBucketItemId = wbItem.Id;
            line.NetUncoveredQty = 0m; // Prevent duplicate demand in both Event Procurement and Weekly Bucket (§33)
            line.RecommendedPurchaseQty = 0m;
            line.CoverageStatus = "WEEKLY_BUCKET_CONSOLIDATED";
            line.DateUpdated = DateTime.UtcNow;

            _repository.SilaCulinaryProcurementAllocation.Create(new SilaCulinaryProcurementAllocation
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProcurementPlanId = plan.Id,
                ProcurementLineId = line.Id,
                EventId = line.EventId ?? plan.PrimaryEventId ?? Guid.Empty,
                MrpRunId = plan.MrpRunId,
                MrpVersion = plan.MrpVersion,
                MaterialId = line.MaterialId,
                MaterialCode = line.MaterialCode,
                MaterialDescription = line.MaterialDescription,
                AllocationType = "WEEKLY_BUCKET",
                Status = "ALLOCATED",
                AllocatedQty = wbItem.ApprovedQuantity > 0 ? wbItem.ApprovedQuantity : wbItem.RequestedQuantity,
                RemainingQty = wbItem.ApprovedQuantity > 0 ? wbItem.ApprovedQuantity : wbItem.RequestedQuantity,
                Uom = line.BaseUom,
                Notes = $"Linked to Weekly Bucket {wbItem.WeeklyBucket.BucketCode} (duplicate-demand prevented).",
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            });

            await _repository.SaveAsync();
            await RefreshPlanRollupsAsync(buyer.Id, plan.Id, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinarySimulateProcurementResultDto> SimulateAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, SilaCulinarySimulateProcurementRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            decimal scale = request.GuestScaleFactor > 0 ? request.GuestScaleFactor : 1m;
            decimal gross = lines.Sum(l => l.GrossRequirementQty) * scale;
            decimal localOnHand = request.LocalStockOverrideQty ?? lines.Sum(l => l.LocalOnHandQty ?? 0m);
            decimal competing = request.CompetingReservationOverrideQty ?? lines.Sum(l => l.CompetingReservationsQty);
            decimal safety = request.SafetyStockOverrideQty ?? lines.Sum(l => l.SafetyStockQty);
            decimal eligible = Math.Max(0m, localOnHand - competing - safety);
            decimal localCov = Math.Min(gross, eligible);

            decimal poAvail = request.IncomingPoQtyOverride ?? lines.Sum(l => l.IncomingPoAllocatedQty);
            bool poDelayed = (request.IncomingPoDelayDays ?? 0) > 0;
            decimal poCov = poDelayed ? 0m : Math.Min(Math.Max(0m, gross - localCov), poAvail);

            decimal itoAvail = request.TransferableStockOverrideQty ?? lines.Sum(l => l.ProposedItoQty);
            decimal itoCov = Math.Min(Math.Max(0m, gross - localCov - poCov), itoAvail);

            decimal net = Math.Max(0m, gross - localCov - poCov - itoCov);
            decimal pack = request.PackSizeOverride ?? (lines.FirstOrDefault(l => l.PackSizeQty > 0)?.PackSizeQty ?? 5m);
            decimal moq = request.MoqOverride ?? (lines.FirstOrDefault(l => l.MoqQty > 0)?.MoqQty ?? 0m);
            decimal recPurch = RoundPackQty(net, pack, moq, out int packs, out decimal excess);
            decimal? unitPrice = request.SupplierPriceOverride ?? lines.FirstOrDefault(l => l.SupplierUnitPrice.HasValue)?.SupplierUnitPrice;
            decimal? estCost = CanViewCosts(roleId) && unitPrice.HasValue ? decimal.Round(recPurch * unitPrice.Value, 2) : null;
            int leadDays = request.SupplierLeadTimeDaysOverride ?? 1;
            bool feasible = IsDeliveryFeasible(DateTime.UtcNow, plan.PreparationDeadlineUtc ?? DateTime.UtcNow.AddDays(3), leadDays, 4, out _, out _);
            decimal readiness = gross > 0 ? decimal.Round(Math.Min(100m, ((localCov + poCov + itoCov + recPurch) / gross) * 100m), 2) : 100m;

            Guid? savedScenarioId = null;
            if (request.SaveScenario)
            {
                SilaCulinaryProcurementScenario sc = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ProcurementPlanId = plan.Id,
                    Name = string.IsNullOrWhiteSpace(request.ScenarioName) ? $"Scenario {DateTime.UtcNow:HH:mm}" : request.ScenarioName.Trim(),
                    ScenarioType = string.IsNullOrWhiteSpace(request.ScenarioType) ? "CUSTOM" : request.ScenarioType.Trim().ToUpperInvariant(),
                    ParametersJson = JsonSerializer.Serialize(request, CamelJson),
                    ResultsJson = JsonSerializer.Serialize(new { gross, localCov, poCov, itoCov, net, recPurch, excess, estCost, readiness }, CamelJson),
                    EstimatedPurchaseValue = estCost,
                    ProjectedReadinessPercent = readiness,
                    CreatedByUserId = userId,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                };
                _repository.SilaCulinaryProcurementScenario.Create(sc);
                await _repository.SaveAsync();
                savedScenarioId = sc.Id;
            }

            return new SilaCulinarySimulateProcurementResultDto
            {
                GrossRequirementQty = decimal.Round(gross, 4),
                EligibleStockCoverageQty = decimal.Round(localCov, 4),
                ExpectedReceiptCoverageQty = decimal.Round(poCov, 4),
                TransferCoverageQty = decimal.Round(itoCov, 4),
                NetPurchaseRequirementQty = decimal.Round(net, 4),
                RecommendedPurchaseQty = recPurch,
                NumberOfPacks = packs,
                ExpectedExcessQty = excess,
                EstimatedPurchaseCost = estCost,
                DeliveryFeasible = feasible,
                ReadinessImpactPercent = readiness,
                SavedScenarioId = savedScenarioId,
            };
        }

        public async Task<SilaCulinaryProcurementOptimizeResultDto> OptimizeAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, decimal? maxBudget, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            bool costsVisible = CanViewCosts(roleId);

            List<SilaCulinaryProcurementConsolidatedPurchaseDto> consolidated = string.IsNullOrWhiteSpace(plan.ConsolidatedPurchasesJson)
                ? new List<SilaCulinaryProcurementConsolidatedPurchaseDto>()
                : JsonSerializer.Deserialize<List<SilaCulinaryProcurementConsolidatedPurchaseDto>>(plan.ConsolidatedPurchasesJson, CamelJson) ?? new();

            decimal totalCost = consolidated.Sum(c => c.EstimatedValue ?? 0m);
            bool budgetOk = !maxBudget.HasValue || totalCost <= maxBudget.Value;

            return new SilaCulinaryProcurementOptimizeResultDto
            {
                Feasible = budgetOk,
                SolverStatus = budgetOk ? "OPTIMAL_DETERMINISTIC" : "INFEASIBLE",
                SolverName = "SILA-Procurement-Optimizer-2C",
                TotalPurchaseCost = costsVisible ? totalCost : null,
                TotalExcessQty = consolidated.Sum(c => c.ExpectedExcessQty),
                InfeasibilityReasons = budgetOk ? new List<string>() : new List<string> { $"Estimated purchase cost {totalCost:0.00} exceeds max budget {maxBudget!.Value:0.00}." },
                SelectedPurchases = consolidated,
            };
        }

        public async Task<SilaCulinaryProcurementAiAnalyzeResponseDto> AnalyzeWithAiAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, string question, CancellationToken cancellationToken)
        {
            if (PromptInjectionPattern.IsMatch(question ?? ""))
            {
                return new SilaCulinaryProcurementAiAnalyzeResponseDto
                {
                    ProviderUsed = "GUARD",
                    Answer = "Refused: Culinary AI Procurement Intelligence is advisory only and cannot post SAP documents, release purchase orders, or alter inventory balances.",
                    AdvisoryOnly = true,
                };
            }

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            SilaCulinaryAiConfiguration? aiCfg = await _repository.SilaCulinaryAiConfiguration
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            string providerMode = aiCfg?.OperatingMode ?? "LOCAL";

            // Try Python engine when configured; gracefully fall back to deterministic grounded synthesis
            string? engineUrl = _configuration[Common.CULINARY_ENGINE_URL];
            if (!string.IsNullOrWhiteSpace(engineUrl))
            {
                try
                {
                    using HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                    var payload = new
                    {
                        tenant_id = organizationId.ToString(),
                        question,
                        plan_context = new
                        {
                            plan_code = plan.PlanCode,
                            lines = lines.Select(l => new
                            {
                                material_code = l.MaterialCode,
                                material_description = l.MaterialDescription,
                                net_uncovered_qty = l.NetUncoveredQty,
                                recommended_purchase_qty = l.RecommendedPurchaseQty,
                                base_uom = l.BaseUom,
                                recommended_supplier_name = l.RecommendedSupplierName,
                                expected_excess_qty = l.ExpectedExcessQty,
                            }).ToList()
                        }
                    };
                    using HttpResponseMessage resp = await client.PostAsJsonAsync($"{engineUrl.TrimEnd('/')}/v1/procurement/analyze", payload, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        var parsed = await resp.Content.ReadFromJsonAsync<SilaCulinaryProcurementAiAnalyzeResponseDto>(SnakeJson, cancellationToken);
                        if (parsed != null)
                        {
                            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_AI, $"AI analysis answered via {parsed.ProviderUsed}.", userId);
                            await _repository.SaveAsync();
                            return parsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"Python procurement AI fallback engaged: {ex.Message}");
                }
            }

            List<SilaCulinaryProcurementLine> shortages = lines.Where(l => l.NetUncoveredQty > 0).ToList();
            List<string> mats = shortages.Select(l => l.MaterialCode ?? l.MaterialDescription).Distinct().Take(10).ToList();
            List<string> sups = shortages.Where(l => !string.IsNullOrWhiteSpace(l.RecommendedSupplierName)).Select(l => l.RecommendedSupplierName!).Distinct().Take(10).ToList();
            string answer = shortages.Count == 0
                ? $"Plan {plan.PlanCode}: All {lines.Count} material requirements are covered by eligible local stock, incoming POs, or proposed transfers."
                : $"Plan {plan.PlanCode}: {shortages.Count} material(s) require procurement totaling {shortages.Sum(s => s.RecommendedPurchaseQty):0.##} units. Key items: "
                  + string.Join("; ", shortages.Take(5).Select(s => $"{s.MaterialDescription} (net {s.NetUncoveredQty:0.##}, order {s.RecommendedPurchaseQty:0.##} {s.BaseUom} from {s.RecommendedSupplierName ?? "TBD"})"));

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_AI, $"AI analysis answered via {providerMode} deterministic fallback.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryProcurementAiAnalyzeResponseDto
            {
                ProviderUsed = providerMode,
                Answer = answer,
                ReferencedMaterials = mats,
                ReferencedSuppliers = sups,
                AdvisoryOnly = true,
            };
        }

        public async Task<SilaCulinaryProcurementExceptionDto> ResolveExceptionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementException ex = await _repository.SilaCulinaryProcurementException
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == planId && x.Id == exceptionId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Exception not found.", "Procurement exception was not found.");

            ex.ResolutionStatus = (resolutionStatus ?? "RESOLVED").Trim().ToUpperInvariant();
            ex.ResolutionNote = note;
            ex.ResolvedByUserId = userId;
            ex.ResolvedOnUtc = DateTime.UtcNow;
            ex.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, planId, Common.SILA_AUDIT_CULINARY_PROC_EX_RES,
                $"Exception {ex.ExceptionType} marked {ex.ResolutionStatus} by {actorName}.", userId);
            await _repository.SaveAsync();
            return ToExceptionDto(ex);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> SubmitForApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            if (plan.IsSimulation)
                throw new BadRequestCustomException("Simulation plan.", "Simulation plans cannot be submitted for approval.");

            await SilaApprovals.StartAsync(
                _repository, _logger, buyer.Id,
                Common.SILA_APPROVAL_TYPE_PROCUREMENT,
                Common.SILA_REF_CULINARY_PROCUREMENT,
                plan.Id, plan.VersionNumber,
                new ApprovalScope { PropertyId = plan.PropertyId, CompanyCode = plan.CompanyCode },
                cancellationToken);

            plan.Status = "PENDING_APPROVAL";
            plan.SubmittedByUserId = userId;
            plan.SubmittedOnUtc = DateTime.UtcNow;
            plan.DateUpdated = DateTime.UtcNow;
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_SUBMITTED, $"Submitted procurement plan {plan.PlanCode} v{plan.VersionNumber} for approval.", userId);
            await _repository.SaveAsync();
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> DecideApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string decision, string? comment, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            string dec = (decision ?? "").Trim().ToUpperInvariant();
            if (dec is not ("APPROVE" or "REJECT" or "RETURN"))
                throw new BadRequestCustomException("Invalid decision.", "Decision must be APPROVE, REJECT, or RETURN.");

            bool hasSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ReferenceType == Common.SILA_REF_CULINARY_PROCUREMENT
                    && x.ReferenceId == plan.Id && x.Version == plan.VersionNumber && x.IsActive && x.Status == Common.SILA_APPROVAL_PENDING)
                .AnyAsync(cancellationToken);

            if (hasSteps)
            {
                ApprovalDecision res = await SilaApprovals.DecideAsync(
                    _repository, _logger, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, plan.VersionNumber,
                    userId, dec == "APPROVE", comment, cancellationToken);
                if (dec == "APPROVE" && res.Outcome != SilaApprovals.OUTCOME_APPROVED)
                {
                    plan.DateUpdated = DateTime.UtcNow;
                    await _repository.SaveAsync();
                    return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
                }
            }

            if (dec == "RETURN")
            {
                plan.Status = "RETURNED";
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_RETURNED, $"Returned {plan.PlanCode}: {comment}", userId);
            }
            else if (dec == "APPROVE")
            {
                plan.Status = "APPROVED";
                plan.ApprovedByUserId = userId;
                plan.ApprovedOnUtc = DateTime.UtcNow;
                plan.ApprovalReference = $"PROC-APR-{plan.PlanCode}-V{plan.VersionNumber}";
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_APPROVED, $"Approved {plan.PlanCode} v{plan.VersionNumber}.", userId);
            }
            else
            {
                plan.Status = "REJECTED";
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_REJECTED, $"Rejected {plan.PlanCode}: {comment}", userId);
            }

            plan.DateUpdated = DateTime.UtcNow;
            await _repository.SaveAsync();
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> HandleEventChangeImpactAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid newMrpRunId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan oldPlan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            oldPlan.Status = "SUPERSEDED";
            oldPlan.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, oldPlan.Id, Common.SILA_AUDIT_CULINARY_PROC_REVISED,
                $"Plan {oldPlan.PlanCode} superseded due to event/MRP revision; released POs and posted ITOs preserved.", userId);
            await _repository.SaveAsync();

            return await CreateAndCalculatePlanAsync(organizationId, userId, roleId, actorName, new SilaCulinaryCreateProcurementPlanRequestDto
            {
                MrpRunId = newMrpRunId,
                Name = $"{oldPlan.Name} (Rev {oldPlan.VersionNumber + 1})",
                IsSimulation = oldPlan.IsSimulation,
            }, cancellationToken);
        }

        public async Task<SilaCulinaryProcurementPlanDetailDto> CancelEventProcurementAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string reason, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);

            // §53 Event Cancellation: release reservations & incoming allocations; NEVER auto-cancel released POs or reverse posted ITOs
            List<SilaCulinaryStockReservation> reservations = await _repository.SilaCulinaryStockReservation
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive
                    && (x.Status == "PROPOSED" || x.Status == "PENDING_APPROVAL" || x.Status == "ACTIVE"))
                .ToListAsync(cancellationToken);
            foreach (var r in reservations)
            {
                r.Status = "RELEASED";
                r.ReleasedQty = Math.Max(0m, r.ReservedQty - r.FulfilledQty);
                r.DateUpdated = DateTime.UtcNow;
            }

            List<SilaCulinaryProcurementAllocation> allocs = await _repository.SilaCulinaryProcurementAllocation
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive
                    && x.Status != "CANCELLED" && x.Status != "RELEASED")
                .ToListAsync(cancellationToken);
            foreach (var a in allocs)
            {
                if (a.AllocationType == "INCOMING_PO")
                {
                    a.Status = "RELEASED";
                    a.RemainingQty = 0m;
                    a.Notes = $"Released on event cancellation: {reason}";
                }
                else
                {
                    // Preserve PO_DRAFT / PROPOSED_ITO / posted references for manual review (§53)
                    a.Notes = $"Event cancelled ({reason}) — manual review required; supplier commitment not auto-cancelled.";
                }
                a.DateUpdated = DateTime.UtcNow;
            }

            plan.Status = "CANCELLED";
            plan.DateUpdated = DateTime.UtcNow;
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_CANCELLED,
                $"Cancelled procurement plan {plan.PlanCode}: {reason}. Reservations & incoming allocations released; supplier commitments preserved.", userId);
            await _repository.SaveAsync();
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public List<SilaCulinaryProcurementReportCatalogItemDto> GetReportCatalog()
        {
            return new List<SilaCulinaryProcurementReportCatalogItemDto>
            {
                new() { ReportCode = "EVENT_MATERIAL_COVERAGE", Title = "1. Event Material Coverage Report", Category = "Coverage" },
                new() { ReportCode = "STOCK_RESERVATION", Title = "2. Stock Reservation Report", Category = "Reservations" },
                new() { ReportCode = "RESERVATION_UTILIZATION", Title = "3. Reservation Utilization Report", Category = "Reservations" },
                new() { ReportCode = "MULTI_EVENT_ALLOCATION", Title = "4. Multi-Event Allocation Report", Category = "Allocation" },
                new() { ReportCode = "COMPETING_DEMAND", Title = "5. Competing Demand Report", Category = "Allocation" },
                new() { ReportCode = "SAFETY_STOCK_EXCEPTION", Title = "6. Safety Stock Exception Report", Category = "Availability" },
                new() { ReportCode = "BATCH_EXPIRY_ALLOCATION", Title = "7. Batch and Expiry Allocation Report", Category = "Availability" },
                new() { ReportCode = "PREPARED_STOCK_ALLOCATION", Title = "8. Prepared Stock Allocation Report", Category = "Availability" },
                new() { ReportCode = "PROJECTED_AVAILABILITY", Title = "9. Projected Stock Availability Report", Category = "Availability" },
                new() { ReportCode = "INCOMING_PO_ALLOCATION", Title = "10. Incoming PO Allocation Report", Category = "Incoming Supply" },
                new() { ReportCode = "OPEN_PO_COVERAGE", Title = "11. Open PO Coverage Report", Category = "Incoming Supply" },
                new() { ReportCode = "OVERDUE_PO", Title = "12. Overdue PO Report", Category = "Incoming Supply" },
                new() { ReportCode = "INTERNAL_TRANSFER_RECOMMENDATION", Title = "13. Internal Transfer Recommendation Report", Category = "Transfers" },
                new() { ReportCode = "ITO_STATUS", Title = "14. ITO Status Report", Category = "Transfers" },
                new() { ReportCode = "OUTLET_TO_OUTLET_TRANSFER", Title = "15. Outlet-to-Outlet Transfer Report", Category = "Transfers" },
                new() { ReportCode = "NET_PURCHASE_REQUIREMENT", Title = "16. Net Purchase Requirement Report", Category = "Procurement" },
                new() { ReportCode = "SUPPLIER_COMPARISON", Title = "17. Supplier Comparison Report", Category = "Suppliers" },
                new() { ReportCode = "CONTRACT_UTILIZATION", Title = "18. Contract Utilization Report", Category = "Suppliers" },
                new() { ReportCode = "MOQ_PACK_ROUNDING", Title = "19. MOQ and Pack Rounding Report", Category = "Procurement" },
                new() { ReportCode = "PURCHASE_RECOMMENDATION", Title = "20. Purchase Recommendation Report", Category = "Procurement" },
                new() { ReportCode = "MULTI_EVENT_CONSOLIDATED_PURCHASE", Title = "21. Multi-Event Consolidated Purchase Report", Category = "Procurement" },
                new() { ReportCode = "EVENT_TO_PO_TRACEABILITY", Title = "22. Event-to-PO Traceability Report", Category = "Traceability" },
                new() { ReportCode = "EVENT_TO_ITO_TRACEABILITY", Title = "23. Event-to-ITO Traceability Report", Category = "Traceability" },
                new() { ReportCode = "GRN_RECONCILIATION", Title = "24. GRN Reconciliation Report", Category = "Receiving" },
                new() { ReportCode = "MATERIAL_READINESS", Title = "25. Material Readiness Report", Category = "Readiness" },
                new() { ReportCode = "DELIVERY_RISK", Title = "26. Delivery Risk Report", Category = "Risk" },
                new() { ReportCode = "PROCUREMENT_EXCEPTION", Title = "27. Procurement Exception Report", Category = "Risk" },
                new() { ReportCode = "PROCUREMENT_APPROVAL_PACKAGE", Title = "28. Procurement Approval Package", Category = "Governance" },
                new() { ReportCode = "PROCUREMENT_SCENARIO_COMPARISON", Title = "29. Procurement Scenario Comparison", Category = "Simulation" },
                new() { ReportCode = "EVENT_CHANGE_IMPACT", Title = "30. Event Change Impact Report", Category = "Change Control" },
                new() { ReportCode = "EVENT_CANCELLATION_COMMITMENT", Title = "31. Event Cancellation Commitment Report", Category = "Change Control" },
                new() { ReportCode = "AI_PROCUREMENT_HISTORY", Title = "32. AI Procurement Recommendation History", Category = "AI" },
                new() { ReportCode = "ESTIMATED_VS_COMMITTED_VALUE", Title = "33. Estimated vs Committed Procurement Value Report", Category = "Finance" },
                new() { ReportCode = "PHASE_3A_COST_HANDOVER", Title = "34. Phase 3A Cost Handover Report", Category = "Handover" },
            };
        }

        private static string CsvSafe(string? raw)
        {
            string val = raw ?? "";
            if (val.Length > 0 && (val[0] == '=' || val[0] == '+' || val[0] == '-' || val[0] == '@'))
                val = "'" + val;
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }

        public async Task<SilaCulinaryProcurementReportResultDto> ExportReportAsync(
            Guid organizationId, Guid roleId, Guid planId, string reportCode, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            bool costsVisible = CanViewCosts(roleId);

            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<string> headers = new()
            {
                "PlanCode", "EventCode", "MaterialCode", "Description", "UOM", "GrossQty",
                "LocalOnHand", "SafetyStock", "ActiveReserved", "IncomingPO", "ProposedITO",
                "NetPurchaseQty", "RecommendedPurchaseQty", "ExcessQty", "Supplier", "EstValue", "Readiness"
            };

            List<List<string>> rows = lines.Select(l => new List<string>
            {
                plan.PlanCode,
                l.EventCode ?? "",
                l.MaterialCode ?? "",
                l.MaterialDescription,
                l.BaseUom,
                l.GrossRequirementQty.ToString("0.####"),
                l.LocalOnHandQty?.ToString("0.####") ?? "UNKNOWN",
                l.SafetyStockQty.ToString("0.####"),
                l.ActiveEventReservationQty.ToString("0.####"),
                l.IncomingPoAllocatedQty.ToString("0.####"),
                l.ProposedItoQty.ToString("0.####"),
                l.NetUncoveredQty.ToString("0.####"),
                l.RecommendedPurchaseQty.ToString("0.####"),
                l.ExpectedExcessQty.ToString("0.####"),
                l.RecommendedSupplierName ?? "",
                costsVisible ? (l.EstimatedPurchaseValue?.ToString("0.00") ?? "0.00") : "MASKED",
                l.ReadinessStatus,
            }).ToList();

            StringBuilder csv = new();
            csv.AppendLine(string.Join(",", headers.Select(CsvSafe)));
            foreach (var r in rows) csv.AppendLine(string.Join(",", r.Select(CsvSafe)));

            string title = GetReportCatalog().FirstOrDefault(x => x.ReportCode == reportCode)?.Title ?? reportCode;
            return new SilaCulinaryProcurementReportResultDto
            {
                ReportCode = reportCode,
                Title = title,
                Currency = plan.Currency,
                GeneratedAtUtc = DateTime.UtcNow,
                Headers = headers,
                PreviewRows = rows.Take(50).ToList(),
                CsvContent = csv.ToString(),
            };
        }

        public async Task<SilaCulinaryPhase3AHandoverDto> GetPhase3AHandoverAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryProcurementPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            bool costsVisible = CanViewCosts(roleId);

            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryStockReservation> resList = await _repository.SilaCulinaryStockReservation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProcurementAllocation> allocList = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ProcurementPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            decimal actualReceivedVal = allocList
                .Where(a => a.ReceivedQty > 0 && a.UnitPrice.HasValue)
                .Sum(a => a.ReceivedQty * a.UnitPrice!.Value);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_PROCUREMENT, plan.Id, Common.SILA_AUDIT_CULINARY_PROC_HANDOVER,
                $"Generated Phase 3A Event P&L handover for {plan.PlanCode}.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryPhase3AHandoverDto
            {
                ProcurementPlanId = plan.Id,
                PlanCode = plan.PlanCode,
                MrpRunId = plan.MrpRunId,
                MrpCode = plan.MrpCode,
                MrpVersion = plan.MrpVersion,
                EventId = plan.PrimaryEventId,
                EventCode = plan.PrimaryEventCode,
                EventName = plan.PrimaryEventName,
                EventRevision = 1,
                PropertyId = plan.PropertyId,
                CompanyCode = plan.CompanyCode,
                PlantCode = plan.PlantCode,
                KitchenName = lines.FirstOrDefault()?.DestinationKitchenName,
                EventDateUtc = plan.EventDateUtc,
                PlannedGuests = plan.PlannedGuests,
                GuaranteedGuests = plan.GuaranteedGuests,
                Currency = plan.Currency,
                EstimatedMaterialRequirementCost = costsVisible ? plan.EstimatedRequirementValue : null,
                EstimatedNewPurchaseExpenditure = costsVisible ? plan.EstimatedNewPurchaseValue : null,
                ApprovedProcurementCommitment = costsVisible ? plan.CommittedProcurementValue : null,
                ActualReceivedMaterialValue = costsVisible ? decimal.Round(actualReceivedVal, 2) : null,
                FutureActualConsumptionCost = null, // Reconciled in Phase 3A
                Materials = lines.Select(l =>
                {
                    var lAllocs = allocList.Where(a => a.ProcurementLineId == l.Id || a.MaterialCode == l.MaterialCode).ToList();
                    return new SilaCulinaryPhase3AMaterialHandoverDto
                    {
                        MaterialCode = l.MaterialCode ?? "",
                        MaterialDescription = l.MaterialDescription,
                        GrossRequirement = l.GrossRequirementQty,
                        ExpectedConsumptionQty = l.GrossRequirementQty,
                        BaseUom = l.BaseUom,
                        EstimatedMaterialCost = costsVisible ? l.EstimatedPurchaseValue : null,
                        ReservationReferences = resList.Where(r => r.ProcurementLineId == l.Id).Select(r => r.ReservationCode).ToList(),
                        IncomingPoAllocations = lAllocs.Where(a => a.AllocationType == "INCOMING_PO" && a.PoNumber != null).Select(a => a.PoNumber!).Distinct().ToList(),
                        ItoReferences = lAllocs.Where(a => a.ItoNumber != null).Select(a => a.ItoNumber!).Distinct().ToList(),
                        PoReferences = lAllocs.Where(a => a.AllocationType == "PO_DRAFT" && a.PoNumber != null).Select(a => a.PoNumber!).Distinct().ToList(),
                        GrnReferences = lAllocs.Where(a => a.GrnNumber != null).Select(a => a.GrnNumber!).Distinct().ToList(),
                        OrderedQty = lAllocs.Where(a => a.AllocationType == "PO_DRAFT").Sum(a => a.AllocatedQty),
                        ReceivedQty = lAllocs.Sum(a => a.ReceivedQty),
                        OutstandingCommitmentQty = lAllocs.Sum(a => a.RemainingQty),
                        CostCenter = lAllocs.Select(a => a.CostCenter).FirstOrDefault(c => c != null),
                        GlAccount = lAllocs.Select(a => a.GlAccount).FirstOrDefault(c => c != null),
                        InternalOrder = lAllocs.Select(a => a.InternalOrder).FirstOrDefault(c => c != null),
                        WbsElement = lAllocs.Select(a => a.WbsElement).FirstOrDefault(c => c != null),
                    };
                }).ToList(),
            };
        }

        private async Task RefreshPlanRollupsAsync(Guid buyerId, Guid planId, CancellationToken cancellationToken)
        {
            SilaCulinaryProcurementPlan? plan = await _repository.SilaCulinaryProcurementPlan
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.Id == planId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (plan == null) return;

            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(x => x.BuyerId == buyerId && x.ProcurementPlanId == planId && x.IsActive)
                .ToListAsync(cancellationToken);
            if (lines.Count == 0) return;

            plan.FullyReservedCount = lines.Count(l => l.ActiveEventReservationQty >= l.GrossRequirementQty);
            plan.PhysicalReadinessPercent = decimal.Round(lines.Average(l => l.GrossRequirementQty > 0 ? Math.Min(1m, l.ActiveEventReservationQty / l.GrossRequirementQty) : 1m) * 100m, 2);
            plan.ProjectedReadinessPercent = decimal.Round(lines.Average(l => l.GrossRequirementQty > 0 ? Math.Min(1m, (l.ConfirmedCoverageQty + l.ProposedItoQty + l.RecommendedPurchaseQty) / l.GrossRequirementQty) : 1m) * 100m, 2);
            plan.DateUpdated = DateTime.UtcNow;
            await _repository.SaveAsync();
        }

        private SilaCulinaryPhase3AHandoverDto BuildPhase3AHandoverSnapshot(SilaCulinaryProcurementPlan plan, List<SilaCulinaryMrpLine> mrpLines)
        {
            return new SilaCulinaryPhase3AHandoverDto
            {
                ProcurementPlanId = plan.Id,
                PlanCode = plan.PlanCode,
                MrpRunId = plan.MrpRunId,
                MrpCode = plan.MrpCode,
                MrpVersion = plan.MrpVersion,
                EventId = plan.PrimaryEventId,
                EventCode = plan.PrimaryEventCode,
                EventName = plan.PrimaryEventName,
                PropertyId = plan.PropertyId,
                CompanyCode = plan.CompanyCode,
                PlantCode = plan.PlantCode,
                EventDateUtc = plan.EventDateUtc,
                PlannedGuests = plan.PlannedGuests,
                GuaranteedGuests = plan.GuaranteedGuests,
                Currency = plan.Currency,
                EstimatedMaterialRequirementCost = plan.EstimatedRequirementValue,
                EstimatedNewPurchaseExpenditure = plan.EstimatedNewPurchaseValue,
                ApprovedProcurementCommitment = plan.CommittedProcurementValue,
            };
        }

        private async Task<SilaCulinaryProcurementPlanDetailDto> BuildDetailDtoAsync(
            Guid buyerId, Guid userId, Guid roleId, SilaCulinaryProcurementPlan plan, CancellationToken cancellationToken)
        {
            FlushExceptions(buyerId);
            if (_repository != null) await _repository.SaveAsync();

            bool costsVisible = CanViewCosts(roleId);

            List<SilaCulinaryProcurementLine> lines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(x => x.BuyerId == buyerId && x.ProcurementPlanId == plan.Id && x.IsActive)
                .OrderBy(x => x.RequiredByUtc)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryStockReservation> reservations = await _repository.SilaCulinaryStockReservation
                .FindByCondition(x => x.BuyerId == buyerId && x.ProcurementPlanId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProcurementAllocation> allocations = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(x => x.BuyerId == buyerId && x.ProcurementPlanId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProcurementException> exceptions = await _repository.SilaCulinaryProcurementException
                .FindByCondition(x => x.BuyerId == buyerId && x.ProcurementPlanId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProcurementScenario> scenarios = await _repository.SilaCulinaryProcurementScenario
                .FindByCondition(x => x.BuyerId == buyerId && x.ProcurementPlanId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);

            List<SilaApprovalStep> approvalSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.BuyerId == buyerId && x.ReferenceType == Common.SILA_REF_CULINARY_PROCUREMENT && x.ReferenceId == plan.Id && x.IsActive)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);

            List<InventoryWorkflowEvent> history = await _repository.InventoryWorkflowEvent
                .FindByCondition(x => x.BuyerId == buyerId && x.ReferenceId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Take(100)
                .ToListAsync(cancellationToken);

            SilaUserPurchasingProfile? profile = await SilaUserPurchasingProfileRules.FindBestAsync(
                _repository, buyerId, userId, null, plan.PropertyId, null, lines.FirstOrDefault()?.DestinationLocationId, plan.PlantCode, cancellationToken);

            SilaCulinaryProcurementPurchasingProfileDto? profileDto = profile == null ? null : new SilaCulinaryProcurementPurchasingProfileDto
            {
                ProfileId = profile.Id,
                CompanyCode = profile.CompanyCode,
                PlantCode = profile.PlantCode,
                StorageLocation = profile.StorageLocation,
                PurchasingOrganization = profile.PurchasingOrganization,
                PurchasingGroup = profile.PurchasingGroup,
                AccountAssignmentCategory = profile.AccountAssignmentCategory,
                CostCenter = profile.CostCenter,
                GlAccount = profile.GlAccount,
                ProfitCenter = profile.ProfitCenter,
                InternalOrder = profile.InternalOrder,
                WbsElement = profile.WbsElement,
                DocumentType = profile.DefaultPoDocumentType,
                ItemCategory = profile.DefaultItemCategory,
                Currency = profile.DefaultCurrency,
                IsComplete = !string.IsNullOrWhiteSpace(profile.CompanyCode) && !string.IsNullOrWhiteSpace(profile.PlantCode),
            };

            return new SilaCulinaryProcurementPlanDetailDto
            {
                Summary = ToSummaryDto(plan, costsVisible),
                CostsVisible = costsVisible,
                CoveragePriority = plan.CoveragePriorityJson.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                PurchasingProfile = profileDto,
                Lines = lines.Select(l => ToLineDto(l, costsVisible)).ToList(),
                Reservations = reservations.Select(ToReservationDto).ToList(),
                Allocations = allocations.Select(a => ToAllocationDto(a, costsVisible)).ToList(),
                ConsolidatedPurchases = string.IsNullOrWhiteSpace(plan.ConsolidatedPurchasesJson)
                    ? new()
                    : (JsonSerializer.Deserialize<List<SilaCulinaryProcurementConsolidatedPurchaseDto>>(plan.ConsolidatedPurchasesJson, CamelJson) ?? new())
                        .Select(c => { if (!costsVisible) c.EstimatedValue = null; return c; }).ToList(),
                Exceptions = exceptions.Select(ToExceptionDto).ToList(),
                Scenarios = scenarios.Select(s => new SilaCulinaryProcurementScenarioDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    ScenarioType = s.ScenarioType,
                    EstimatedPurchaseValue = costsVisible ? s.EstimatedPurchaseValue : null,
                    ProjectedReadinessPercent = s.ProjectedReadinessPercent,
                    ParametersJson = s.ParametersJson,
                    ResultsJson = s.ResultsJson,
                    DateCreated = s.DateCreated,
                }).ToList(),
                SupplyDemandLedger = string.IsNullOrWhiteSpace(plan.SupplyDemandLedgerJson)
                    ? new()
                    : JsonSerializer.Deserialize<List<SilaCulinaryProcurementLedgerEntryDto>>(plan.SupplyDemandLedgerJson, CamelJson) ?? new(),
                ApprovalSteps = approvalSteps.Select(a => new SilaCulinaryProcurementApprovalStepDto
                {
                    StepOrder = a.Order,
                    ApproverUserId = a.UserId,
                    Status = a.Status,
                    Comment = a.Comment,
                    DecidedOn = a.ActedOn,
                }).ToList(),
                History = history.Select(h => new SilaCulinaryProcurementHistoryItemDto
                {
                    Action = h.Action,
                    Comment = h.Comment ?? "",
                    DateCreated = h.DateCreated,
                }).ToList(),
            };
        }

        private static SilaCulinaryProcurementPlanSummaryDto ToSummaryDto(SilaCulinaryProcurementPlan p, bool costsVisible) => new()
        {
            Id = p.Id,
            PlanCode = p.PlanCode,
            Name = p.Name,
            MrpRunId = p.MrpRunId,
            MrpCode = p.MrpCode,
            MrpVersion = p.MrpVersion,
            PrimaryEventId = p.PrimaryEventId,
            PrimaryEventCode = p.PrimaryEventCode,
            PrimaryEventName = p.PrimaryEventName,
            PropertyId = p.PropertyId,
            PropertyName = p.PropertyName,
            CompanyCode = p.CompanyCode,
            PlantCode = p.PlantCode,
            Currency = p.Currency,
            Status = p.Status,
            VersionNumber = p.VersionNumber,
            IsSimulation = p.IsSimulation,
            EventDateUtc = p.EventDateUtc,
            PreparationDeadlineUtc = p.PreparationDeadlineUtc,
            PlannedGuests = p.PlannedGuests,
            ResponsibleChef = p.ResponsibleChef,
            VenueName = p.VenueName,
            RequiredMaterialsCount = p.RequiredMaterialsCount,
            FullyReservedCount = p.FullyReservedCount,
            IncomingPoCoveredCount = p.IncomingPoCoveredCount,
            TransferCoveredCount = p.TransferCoveredCount,
            PurchaseRequiredCount = p.PurchaseRequiredCount,
            CriticalShortageCount = p.CriticalShortageCount,
            DelayedDeliveryCount = p.DelayedDeliveryCount,
            PendingApprovalCount = p.PendingApprovalCount,
            EstimatedNewPurchaseValue = costsVisible ? p.EstimatedNewPurchaseValue : null,
            CommittedProcurementValue = costsVisible ? p.CommittedProcurementValue : null,
            PhysicalReadinessPercent = p.PhysicalReadinessPercent,
            ProjectedReadinessPercent = p.ProjectedReadinessPercent,
            CalculatedAtUtc = p.CalculatedAtUtc,
        };

        private static SilaCulinaryProcurementLineDto ToLineDto(SilaCulinaryProcurementLine l, bool costsVisible)
        {
            var supComps = string.IsNullOrWhiteSpace(l.SupplierComparisonsJson)
                ? new List<SilaCulinaryProcurementSupplierComparisonDto>()
                : JsonSerializer.Deserialize<List<SilaCulinaryProcurementSupplierComparisonDto>>(l.SupplierComparisonsJson, CamelJson) ?? new();
            if (!costsVisible)
            {
                foreach (var sc in supComps)
                {
                    sc.UnitPrice = null;
                    sc.BaseUnitPrice = null;
                    sc.TotalEstimatedCost = null;
                }
            }

            return new SilaCulinaryProcurementLineDto
            {
                Id = l.Id,
                EventId = l.EventId,
                EventCode = l.EventCode,
                EventName = l.EventName,
                MaterialId = l.MaterialId,
                MaterialCode = l.MaterialCode,
                MaterialDescription = l.MaterialDescription,
                MaterialCategory = l.MaterialCategory,
                MaterialImageUrl = l.MaterialImageUrl,
                BaseUom = l.BaseUom,
                PurchasingUom = l.PurchasingUom,
                DestinationLocationId = l.DestinationLocationId,
                DestinationKitchenName = l.DestinationKitchenName,
                DestinationStorageLocation = l.DestinationStorageLocation,
                RequiredByUtc = l.RequiredByUtc,
                GrossRequirementQty = l.GrossRequirementQty,
                LocalOnHandQty = l.LocalOnHandQty,
                CompetingReservationsQty = l.CompetingReservationsQty,
                SafetyStockQty = l.SafetyStockQty,
                EligibleLocalStockQty = l.EligibleLocalStockQty,
                ActiveEventReservationQty = l.ActiveEventReservationQty,
                ProposedReservationQty = l.ProposedReservationQty,
                PreparedStockCoverageQty = l.PreparedStockCoverageQty,
                IncomingPoAllocatedQty = l.IncomingPoAllocatedQty,
                IncomingTransferAllocatedQty = l.IncomingTransferAllocatedQty,
                ProposedItoQty = l.ProposedItoQty,
                ConfirmedCoverageQty = l.ConfirmedCoverageQty,
                RemainingConfirmedGapQty = l.RemainingConfirmedGapQty,
                NetUncoveredQty = l.NetUncoveredQty,
                RecommendedPurchaseQty = l.RecommendedPurchaseQty,
                ExpectedExcessQty = l.ExpectedExcessQty,
                PackSizeQty = l.PackSizeQty,
                MoqQty = l.MoqQty,
                NumberOfPacks = l.NumberOfPacks,
                RecommendedSupplierId = l.RecommendedSupplierId,
                RecommendedSupplierCode = l.RecommendedSupplierCode,
                RecommendedSupplierName = l.RecommendedSupplierName,
                ContractReference = l.ContractReference,
                SupplierUnitPrice = costsVisible ? l.SupplierUnitPrice : null,
                EstimatedPurchaseValue = costsVisible ? l.EstimatedPurchaseValue : null,
                Currency = l.Currency,
                ExpectedDeliveryDateUtc = l.ExpectedDeliveryDateUtc,
                LatestOrderReleaseDateUtc = l.LatestOrderReleaseDateUtc,
                IsDeliveryFeasible = l.IsDeliveryFeasible,
                CoverageStatus = l.CoverageStatus,
                ReadinessStatus = l.ReadinessStatus,
                WeeklyBucketLinked = l.WeeklyBucketLinked,
                WeeklyBucketItemId = l.WeeklyBucketItemId,
                SupplierComparisons = supComps,
                FefoBatches = string.IsNullOrWhiteSpace(l.FefoBatchesJson) ? new() : JsonSerializer.Deserialize<List<SilaCulinaryProcurementFefoBatchDto>>(l.FefoBatchesJson, CamelJson) ?? new(),
                ItoCandidates = string.IsNullOrWhiteSpace(l.SourceDetailsJson) ? new() : JsonSerializer.Deserialize<List<SilaCulinaryProcurementItoCandidateDto>>(l.SourceDetailsJson, CamelJson) ?? new(),
            };
        }

        private static SilaCulinaryStockReservationDto ToReservationDto(SilaCulinaryStockReservation r) => new()
        {
            Id = r.Id,
            ReservationCode = r.ReservationCode,
            ProcurementPlanId = r.ProcurementPlanId,
            ProcurementLineId = r.ProcurementLineId,
            EventId = r.EventId,
            EventRevision = r.EventRevision,
            MrpRunId = r.MrpRunId,
            MrpVersion = r.MrpVersion,
            MaterialId = r.MaterialId,
            MaterialCode = r.MaterialCode,
            MaterialDescription = r.MaterialDescription,
            LocationId = r.LocationId,
            StorageLocationCode = r.StorageLocationCode,
            BatchNumber = r.BatchNumber,
            BatchExpiryUtc = r.BatchExpiryUtc,
            ReservedQty = r.ReservedQty,
            FulfilledQty = r.FulfilledQty,
            ReleasedQty = r.ReleasedQty,
            BaseUom = r.BaseUom,
            RequiredByUtc = r.RequiredByUtc,
            ExpiresAtUtc = r.ExpiresAtUtc,
            Status = r.Status,
            IsPreparedStock = r.IsPreparedStock,
            SafetyStockOverrideUsed = r.SafetyStockOverrideUsed,
            SafetyOverrideReason = r.SafetyOverrideReason,
            PriorityRank = r.PriorityRank,
            ApprovalReference = r.ApprovalReference,
        };

        private static SilaCulinaryProcurementAllocationDto ToAllocationDto(SilaCulinaryProcurementAllocation a, bool costsVisible = true) => new()
        {
            Id = a.Id,
            ProcurementPlanId = a.ProcurementPlanId,
            ProcurementLineId = a.ProcurementLineId,
            EventId = a.EventId,
            MrpRunId = a.MrpRunId,
            MaterialId = a.MaterialId,
            MaterialCode = a.MaterialCode,
            MaterialDescription = a.MaterialDescription,
            AllocationType = a.AllocationType,
            Status = a.Status,
            SourceLocationId = a.SourceLocationId,
            SourceLocationName = a.SourceLocationName,
            DestinationLocationId = a.DestinationLocationId,
            DestinationLocationName = a.DestinationLocationName,
            PurchaseOrderId = a.PurchaseOrderId,
            PurchaseOrderItemId = a.PurchaseOrderItemId,
            PoNumber = a.PoNumber,
            PoItemNumber = a.PoItemNumber,
            InternalTransferOrderId = a.InternalTransferOrderId,
            ItoNumber = a.ItoNumber,
            GoodsReceiptId = a.GoodsReceiptId,
            GrnNumber = a.GrnNumber,
            SapMaterialDocument = a.SapMaterialDocument,
            ReversalMaterialDocument = a.ReversalMaterialDocument,
            SupplierId = a.SupplierId,
            SupplierName = a.SupplierName,
            ContractReference = a.ContractReference,
            AccountAssignmentCategory = a.AccountAssignmentCategory,
            CostCenter = a.CostCenter,
            GlAccount = a.GlAccount,
            AllocatedQty = a.AllocatedQty,
            ReceivedQty = a.ReceivedQty,
            ReversedQty = a.ReversedQty,
            RemainingQty = a.RemainingQty,
            Uom = a.Uom,
            UnitPrice = costsVisible ? a.UnitPrice : null,
            EstimatedValue = costsVisible ? a.EstimatedValue : null,
            Currency = a.Currency,
            ExpectedDateUtc = a.ExpectedDateUtc,
            Notes = a.Notes,
        };

        private static SilaCulinaryProcurementExceptionDto ToExceptionDto(SilaCulinaryProcurementException e) => new()
        {
            Id = e.Id,
            ProcurementPlanId = e.ProcurementPlanId,
            ProcurementLineId = e.ProcurementLineId,
            EventCode = e.EventCode,
            MaterialCode = e.MaterialCode,
            MaterialDescription = e.MaterialDescription,
            ExceptionType = e.ExceptionType,
            Severity = e.Severity,
            QuantityImpact = e.QuantityImpact,
            RequiredDateUtc = e.RequiredDateUtc,
            ResponsibleRole = e.ResponsibleRole,
            Message = e.Message,
            RecommendedAction = e.RecommendedAction,
            ResolutionStatus = e.ResolutionStatus,
            ResolutionNote = e.ResolutionNote,
        };
    }
}
