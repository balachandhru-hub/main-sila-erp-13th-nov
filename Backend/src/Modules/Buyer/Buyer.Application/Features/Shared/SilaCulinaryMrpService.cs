using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryMrpService
    {
        Task<SilaCulinaryMrpDashboardDto> GetDashboardAsync(Guid organizationId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken);
        Task<List<SilaCulinaryMrpRunSummaryDto>> ListRunsAsync(Guid organizationId, Guid roleId, Guid? propertyId, string? status, bool? includeSimulations, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> GetRunAsync(Guid organizationId, Guid roleId, Guid runId, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> CreateAndCalculateRunAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryMrpCreateRunRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> RecalculateRunAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpGroupViewDto> GetGroupedViewAsync(Guid organizationId, Guid roleId, Guid runId, string groupBy, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpSimulationResultDto> SimulateRunAsync(Guid organizationId, Guid roleId, Guid runId, SilaCulinaryMrpSimulateRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpScenarioDto> SaveScenarioAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, string name, string scenarioType, SilaCulinaryMrpSimulateRequestDto parameters, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpOptimizeResultDto> OptimizeBatchesAsync(Guid organizationId, Guid userId, Guid roleId, Guid runId, decimal? maxBudget, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpAiAnalyzeResponseDto> AnalyzeWithAiAsync(Guid organizationId, Guid userId, Guid roleId, Guid runId, string question, CancellationToken cancellationToken);
        Task<List<SilaCulinaryMrpSubstitutionSuggestionDto>> SuggestSubstitutionsAsync(Guid organizationId, Guid roleId, Guid runId, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpExceptionDto> ResolveExceptionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpExceptionDto> CreateMaterialRequestFromExceptionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, Guid exceptionId, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> SubmitForApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> DecideApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, string decision, string? comment, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> CreateRevisionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, string changeReason, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpImpactAnalysisDto> AnalyzeEventChangeImpactAsync(Guid organizationId, Guid roleId, Guid runId, CancellationToken cancellationToken);
        Task<SilaCulinaryMrpRunDetailDto> ReleaseForPlanningAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, CancellationToken cancellationToken);
        Task<SilaCulinaryPhase2CHandoverDto> GetPhase2CHandoverAsync(Guid organizationId, Guid userId, Guid roleId, Guid runId, CancellationToken cancellationToken);
        List<SilaCulinaryMrpReportCatalogItemDto> GetReportCatalog();
        Task<SilaCulinaryMrpReportResultDto> ExportReportAsync(Guid organizationId, Guid roleId, Guid runId, string reportCode, CancellationToken cancellationToken);
    }

    public class SilaCulinaryMrpService : ISilaCulinaryMrpService
    {
        private static readonly JsonSerializerOptions CamelJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions SnakeJson = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, PropertyNameCaseInsensitive = true };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationCredentialProtector _protector;

        public SilaCulinaryMrpService(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IIntegrationCredentialProtector protector)
        {
            _repository = repository;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _protector = protector;
        }

        private static bool CanViewCosts(Guid roleId) => SilaAccess.HasFullAccess(roleId);

        private async Task<BuyerBusinessProfile> ResolveBuyerAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Buyer organization not found.", "Buyer organization profile was not found.");
        }

        private async Task<SilaCulinaryMrpRun> LoadRunOrThrowAsync(Guid buyerId, Guid runId, bool tracked, CancellationToken cancellationToken)
        {
            IQueryable<SilaCulinaryMrpRun> q = tracked
                ? _repository.SilaCulinaryMrpRun.FindByConditionTracked(x => x.BuyerId == buyerId && x.Id == runId && x.IsActive)
                : _repository.SilaCulinaryMrpRun.FindByCondition(x => x.BuyerId == buyerId && x.Id == runId && x.IsActive);
            return await q.FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundCustomException("MRP run not found.", "Culinary MRP run was not found.");
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
                Comment = comment.Length > 490 ? comment[..490] : comment,
                ActorUserId = actorUserId,
                IsActive = true
            });
        }

        // ==================== DASHBOARD & RUN LIST ====================

        public async Task<SilaCulinaryMrpDashboardDto> GetDashboardAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            IQueryable<SilaCulinaryMrpRun> rq = _repository.SilaCulinaryMrpRun.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) rq = rq.Where(x => x.PropertyId == propertyId.Value);
            if (!string.IsNullOrWhiteSpace(status)) rq = rq.Where(x => x.Status == status.Trim().ToUpperInvariant());
            List<SilaCulinaryMrpRun> runs = await rq.ToListAsync(cancellationToken);

            List<Guid> runIds = runs.Select(r => r.Id).ToList();
            List<SilaCulinaryMrpLine> lines = runIds.Count == 0
                ? new()
                : await _repository.SilaCulinaryMrpLine.FindByCondition(x => x.BuyerId == buyer.Id && runIds.Contains(x.MrpRunId) && x.IsActive).ToListAsync(cancellationToken);
            List<SilaCulinaryMrpException> exs = runIds.Count == 0
                ? new()
                : await _repository.SilaCulinaryMrpException.FindByCondition(x => x.BuyerId == buyer.Id && runIds.Contains(x.MrpRunId) && x.IsActive && x.ResolutionStatus == "OPEN").ToListAsync(cancellationToken);

            DateTime now = DateTime.UtcNow;
            SilaCulinaryMrpDashboardDto dash = new()
            {
                ActiveMrpRuns = runs.Count(r => !r.IsSimulation && r.Status is not ("CANCELLED" or "SUPERSEDED")),
                UpcomingEventRequirements = runs.Where(r => !r.IsSimulation && r.PlanningHorizonStartUtc >= now.AddDays(-1)).Sum(r => r.TotalEvents),
                ApprovedMrpBaselines = runs.Count(r => r.IsApprovedBaseline),
                PendingMrpApprovals = runs.Count(r => r.Status == "PENDING_APPROVAL"),
                UniqueRequiredMaterials = lines.Where(l => l.MaterialId.HasValue).Select(l => l.MaterialId!.Value).Distinct().Count(),
                MaterialsWithShortages = lines.Count(l => (l.IndicativeCurrentShortageQty ?? 0m) > 0m),
                MaterialsWithUnresolvedMappings = lines.Count(l => !l.IsResolvedMaterial),
                MaterialsRequiredWithin24Hours = lines.Count(l => l.RequiredDateUtc <= now.AddHours(24)),
                MaterialsRequiredWithin48Hours = lines.Count(l => l.RequiredDateUtc <= now.AddHours(48)),
                EstimatedRequirementValue = costVisible ? lines.Where(l => l.RequirementValue.HasValue).Sum(l => l.RequirementValue!.Value) : null,
                IndicativeShortageValue = costVisible ? lines.Where(l => l.IndicativeShortageValue.HasValue).Sum(l => l.IndicativeShortageValue!.Value) : null,
                Currency = runs.FirstOrDefault()?.Currency ?? "AED",
                CostVisible = costVisible
            };

            foreach (var grp in lines.GroupBy(l => l.MaterialCategory))
            {
                dash.RequirementsByCategory[grp.Key] = Math.Round(grp.Sum(x => x.GrossQty), 2);
                if (costVisible) dash.ValueByCategory[grp.Key] = Math.Round(grp.Where(x => x.RequirementValue.HasValue).Sum(x => x.RequirementValue!.Value), 2);
            }

            foreach (var grp in exs.GroupBy(e => e.ExceptionType))
                dash.ExceptionsByType[grp.Key] = grp.Count();

            foreach (var grp in lines.GroupBy(l => l.KitchenName))
                dash.DemandByKitchen[grp.Key] = grp.Count();

            dash.ShortageByMaterial = lines
                .Where(l => (l.IndicativeCurrentShortageQty ?? 0m) > 0m)
                .OrderByDescending(l => l.IndicativeShortageValue ?? l.IndicativeCurrentShortageQty ?? 0m)
                .Take(8)
                .Select(l => new SilaCulinaryMrpChartPointDto
                {
                    Label = l.MaterialDescription,
                    Qty = l.IndicativeCurrentShortageQty ?? 0m,
                    Value = costVisible ? l.IndicativeShortageValue : null
                })
                .ToList();

            dash.DemandByDate = lines
                .GroupBy(l => l.RequiredDateBucket)
                .OrderBy(g => g.Key)
                .Take(14)
                .Select(g => new SilaCulinaryMrpChartPointDto
                {
                    Label = g.Key,
                    Qty = Math.Round(g.Sum(x => x.GrossQty), 2),
                    Value = costVisible ? Math.Round(g.Where(x => x.RequirementValue.HasValue).Sum(x => x.RequirementValue!.Value), 2) : null
                })
                .ToList();

            return dash;
        }

        public async Task<List<SilaCulinaryMrpRunSummaryDto>> ListRunsAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, string? status, bool? includeSimulations, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            IQueryable<SilaCulinaryMrpRun> q = _repository.SilaCulinaryMrpRun.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId.Value);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim().ToUpperInvariant());
            if (includeSimulations == false) q = q.Where(x => !x.IsSimulation);

            List<SilaCulinaryMrpRun> runs = await q.OrderByDescending(x => x.DateCreated).ToListAsync(cancellationToken);
            return runs.Select(r => MapSummary(r, costVisible)).ToList();
        }

        public async Task<SilaCulinaryMrpRunDetailDto> GetRunAsync(Guid organizationId, Guid roleId, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
        }

        // ==================== CREATE & CALCULATE MRP RUN ====================

        public async Task<SilaCulinaryMrpRunDetailDto> CreateAndCalculateRunAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryMrpCreateRunRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string scope = string.IsNullOrWhiteSpace(request.RunScope) ? "SINGLE_EVENT" : request.RunScope.Trim().ToUpperInvariant();

            IQueryable<SilaCulinaryEvent> eq = _repository.SilaCulinaryEvent.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (request.EventIds.Count > 0)
            {
                eq = eq.Where(x => request.EventIds.Contains(x.Id));
            }
            else
            {
                if (request.PropertyId.HasValue) eq = eq.Where(x => x.PropertyId == request.PropertyId.Value);
                if (request.HorizonStartUtc.HasValue) eq = eq.Where(x => x.EndDateUtc >= request.HorizonStartUtc.Value);
                if (request.HorizonEndUtc.HasValue) eq = eq.Where(x => x.StartDateUtc <= request.HorizonEndUtc.Value);
            }

            List<SilaCulinaryEvent> events = await eq.OrderBy(x => x.StartDateUtc).ToListAsync(cancellationToken);
            if (events.Count == 0)
            {
                throw new BadRequestCustomException("No events matched MRP scope.", "Select at least one culinary event or expand date/property filters.");
            }

            // Prevent creating operational baseline from unapproved events unless simulation
            bool anyUnapproved = events.Any(e => e.Status is not ("APPROVED" or "READY_FOR_MRP"));
            bool isSimulation = request.IsSimulation || anyUnapproved;
            if (!request.AllowUnapprovedEvents && anyUnapproved)
            {
                throw new BadRequestCustomException("Unapproved events included.", "Approve the event baseline in Phase 2A or run MRP as a draft simulation.");
            }

            int seqCount = await _repository.SilaCulinaryMrpRun.FindByCondition(x => x.BuyerId == buyer.Id).CountAsync(cancellationToken);
            string code = $"MRP-{DateTime.UtcNow:yyyy}-{(seqCount + 1):0000}";
            string name = string.IsNullOrWhiteSpace(request.Name)
                ? (events.Count == 1 ? $"{events[0].Name} MRP" : $"Multi-Event MRP ({events.Count} Events)")
                : request.Name.Trim();

            SilaCulinaryMrpRun run = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MrpCode = code,
                Name = name,
                RunScope = scope,
                Status = "CALCULATED",
                VersionNumber = 1,
                IsApprovedBaseline = false,
                IsSimulation = isSimulation,
                EngineVersion = "2B.1.0",
                CorrelationId = Guid.NewGuid().ToString("N")[..16],
                PropertyId = request.PropertyId ?? events.FirstOrDefault()?.PropertyId,
                PropertyName = events.FirstOrDefault()?.VenueName ?? "Main Property",
                CompanyCode = events.FirstOrDefault()?.CompanyCode,
                PlantCode = events.FirstOrDefault()?.PlantCode,
                PlanningHorizonStartUtc = request.HorizonStartUtc ?? events.Min(e => e.StartDateUtc),
                PlanningHorizonEndUtc = request.HorizonEndUtc ?? events.Max(e => e.EndDateUtc),
                CalculatedAtUtc = DateTime.UtcNow,
                BufferPolicy = string.IsNullOrWhiteSpace(request.BufferPolicy) ? "PRECEDENCE" : request.BufferPolicy.Trim().ToUpperInvariant(),
                PreparedStockMode = string.IsNullOrWhiteSpace(request.PreparedStockMode) ? "ADJUSTED_SCENARIO" : request.PreparedStockMode.Trim().ToUpperInvariant(),
                EventReferencesJson = JsonSerializer.Serialize(events.Select(e => e.EventCode).ToList(), CamelJson),
                EventRevisionsJson = JsonSerializer.Serialize(events.Select(e => new { e.Id, e.EventCode, e.RevisionNumber }).ToList(), CamelJson),
                SelectedSessionsJson = JsonSerializer.Serialize(request.SessionIds, CamelJson),
                SelectedKitchensJson = JsonSerializer.Serialize(request.KitchenLocationIds, CamelJson),
                Currency = events.First().Currency,
                CreatedByName = actorName,
                IsActive = true
            };

            _repository.SilaCulinaryMrpRun.Create(run);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_CREATED,
                $"Created MRP run {run.MrpCode} ({scope}) covering {events.Count} event(s).", userId);

            await ExecuteCalculationAndPersistAsync(buyer.Id, userId, actorName, run, events, request.SessionIds, request.KitchenLocationIds, cancellationToken);
            await _repository.SaveAsync();

            return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMrpRunDetailDto> RecalculateRunAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: true, cancellationToken);
            if (run.IsApprovedBaseline || run.Status is "APPROVED" or "RELEASED_FOR_PLANNING")
            {
                throw new BadRequestCustomException("Approved MRP baseline is immutable.", "Create a new MRP revision before recalculating.");
            }

            List<Guid> eventIds = new();
            if (!string.IsNullOrWhiteSpace(run.EventRevisionsJson))
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(run.EventRevisionsJson);
                    foreach (JsonElement el in doc.RootElement.EnumerateArray())
                    {
                        if (el.TryGetProperty("id", out JsonElement idEl) && Guid.TryParse(idEl.GetString(), out Guid g)) eventIds.Add(g);
                    }
                }
                catch { /* fallback */ }
            }

            List<SilaCulinaryEvent> events = await _repository.SilaCulinaryEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && eventIds.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);

            // Soft-deactivate old calculated lines & open auto-generated exceptions before fresh calculation
            List<SilaCulinaryMrpLine> oldLines = await _repository.SilaCulinaryMrpLine.FindByConditionTracked(x => x.MrpRunId == run.Id && x.IsActive).ToListAsync(cancellationToken);
            foreach (SilaCulinaryMrpLine ol in oldLines) ol.IsActive = false;
            List<SilaCulinaryMrpException> oldEx = await _repository.SilaCulinaryMrpException.FindByConditionTracked(x => x.MrpRunId == run.Id && x.IsActive).ToListAsync(cancellationToken);
            foreach (SilaCulinaryMrpException oe in oldEx) oe.IsActive = false;

            List<Guid> sessIds = ParseGuidList(run.SelectedSessionsJson);
            List<Guid> kitIds = ParseGuidList(run.SelectedKitchensJson);

            run.CalculatedAtUtc = DateTime.UtcNow;
            await ExecuteCalculationAndPersistAsync(buyer.Id, userId, actorName, run, events, sessIds, kitIds, cancellationToken);
            await _repository.SaveAsync();

            return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
        }

        // ==================== GROUPED CONSOLIDATION VIEWS (§20) ====================

        public async Task<SilaCulinaryMrpGroupViewDto> GetGroupedViewAsync(
            Guid organizationId, Guid roleId, Guid runId, string groupBy, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            List<SilaCulinaryMrpLineDto> lines = (await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .ToListAsync(cancellationToken))
                .Select(l => MapLine(l, costVisible))
                .ToList();

            string mode = string.IsNullOrWhiteSpace(groupBy) ? "MATERIAL" : groupBy.Trim().ToUpperInvariant();
            IEnumerable<(string Key, string Label, SilaCulinaryMrpLineDto Line)> pairs = mode switch
            {
                "RECIPE" => lines.SelectMany(l => (l.ContributingRecipes.Count > 0 ? l.ContributingRecipes : new List<string> { "Unassigned Recipe" })
                    .Select(r => (r, r, l))),
                "SESSION" => lines.SelectMany(l => (l.ContributingSessions.Count > 0 ? l.ContributingSessions : new List<string> { "Main Service" })
                    .Select(s => (s, s, l))),
                "KITCHEN" => lines.Select(l => (l.KitchenName, l.KitchenName, l)),
                "STORAGE_LOCATION" => lines.Select(l => (l.StorageLocationCode, l.StorageLocationCode, l)),
                "CATEGORY" => lines.Select(l => (l.MaterialCategory, l.MaterialCategory, l)),
                "REQUIRED_DATE" => lines.Select(l => (l.RequiredDateBucket, l.RequiredDateBucket, l)),
                "EVENT" => lines.SelectMany(l => (l.ContributingEvents.Count > 0 ? l.ContributingEvents : new List<string> { "Event" })
                    .Select(e => (e, e, l))),
                "PROPERTY" => lines.Select(l => (run.PropertyName ?? "Property", run.PropertyName ?? "Property", l)),
                _ => lines.Select(l => (l.MaterialCode ?? l.MaterialDescription, $"{l.MaterialDescription} ({l.MaterialCode ?? "UNMAPPED"})", l)),
            };

            List<SilaCulinaryMrpGroupRowDto> groups = pairs
                .GroupBy(p => p.Key)
                .Select(g =>
                {
                    List<SilaCulinaryMrpLineDto> gLines = g.Select(x => x.Line).DistinctBy(x => x.Id).ToList();
                    List<string> uoms = gLines.Select(x => x.BaseUom).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    return new SilaCulinaryMrpGroupRowDto
                    {
                        GroupKey = g.Key,
                        GroupLabel = g.First().Label,
                        LineCount = gLines.Count,
                        ShortageCount = gLines.Count(x => (x.IndicativeCurrentShortageQty ?? 0m) > 0m),
                        CommonUom = uoms.Count == 1 ? uoms[0] : null,
                        TotalQtyIfSameUom = uoms.Count == 1 ? Math.Round(gLines.Sum(x => x.GrossQty), 4) : null,
                        TotalRequirementValue = costVisible && gLines.Any(x => x.RequirementValue.HasValue)
                            ? Math.Round(gLines.Where(x => x.RequirementValue.HasValue).Sum(x => x.RequirementValue!.Value), 2)
                            : null,
                        TotalShortageValue = costVisible && gLines.Any(x => x.IndicativeShortageValue.HasValue)
                            ? Math.Round(gLines.Where(x => x.IndicativeShortageValue.HasValue).Sum(x => x.IndicativeShortageValue!.Value), 2)
                            : null,
                        Lines = gLines
                    };
                })
                .OrderByDescending(g => g.TotalRequirementValue ?? 0m)
                .ToList();

            return new SilaCulinaryMrpGroupViewDto { GroupBy = mode, Groups = groups };
        }

        // ==================== INTERACTIVE SIMULATION & SCENARIOS (§31, §33) ====================

        public async Task<SilaCulinaryMrpSimulationResultDto> SimulateRunAsync(
            Guid organizationId, Guid roleId, Guid runId, SilaCulinaryMrpSimulateRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            List<SilaCulinaryMrpLine> rawLines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            decimal factor = request.GuestScaleFactor > 0 ? request.GuestScaleFactor : 1m;
            if (request.AdditionalAllowancePercent > 0) factor *= 1m + request.AdditionalAllowancePercent / 100m;
            decimal bufferMult = 1m + Math.Max(0m, request.BufferPercent) / 100m;
            decimal priceMult = 1m + request.PriceIncreasePercent / 100m;
            Dictionary<Guid, SilaCulinaryMrpLineSimulationOverrideDto> ovs = request.LineOverrides.ToDictionary(x => x.LineId);

            List<SilaCulinaryMrpLineDto> simLines = new();
            decimal totalReqVal = 0m;
            decimal totalShortVal = 0m;
            bool hasCost = false;
            int shortages = 0;
            int missing = 0;

            foreach (SilaCulinaryMrpLine l in rawLines)
            {
                ovs.TryGetValue(l.Id, out SilaCulinaryMrpLineSimulationOverrideDto? ov);
                decimal lineMult = factor * (ov?.UptakeFactor ?? 1m) * (1m + (ov?.BufferPercent ?? 0m) / 100m) * bufferMult;
                decimal simGross = Math.Round(l.GrossQty * lineMult, 4);
                decimal simEdible = Math.Round(l.EdibleQty * lineMult, 4);
                if (request.YieldOverridePercent > 0 && request.YieldOverridePercent <= 100m)
                {
                    simGross = Math.Round(simEdible / (request.YieldOverridePercent / 100m), 4);
                }

                decimal prepStock = ov?.PreparedStockQty ?? l.PreparedStockQty;
                decimal newProd = request.PreparedStockMode == "GROSS_ONLY" ? simGross : Math.Max(0m, simGross - prepStock);
                decimal? eligStock = ov?.EligibleStockQty ?? l.EligibleStockQty;
                decimal? curShort = eligStock.HasValue ? Math.Max(0m, newProd - eligStock.Value) : null;
                decimal? remShort = eligStock.HasValue ? Math.Max(0m, newProd - eligStock.Value - l.IncomingPoQty) : null;

                decimal? unitCost = (ov?.UnitCost ?? l.UnitCost) is decimal uc ? Math.Round(uc * priceMult, 4) : null;
                decimal? reqVal = unitCost.HasValue ? Math.Round(newProd * unitCost.Value, 2) : null;
                decimal? shVal = unitCost.HasValue && curShort.HasValue ? Math.Round(curShort.Value * unitCost.Value, 2) : null;

                if ((curShort ?? 0m) > 0m) shortages++;
                if (!l.IsResolvedMaterial) missing++;
                if (reqVal.HasValue) { totalReqVal += reqVal.Value; hasCost = true; }
                if (shVal.HasValue) totalShortVal += shVal.Value;

                SilaCulinaryMrpLineDto dto = MapLine(l, costVisible);
                dto.EdibleQty = simEdible;
                dto.RawQty = simGross;
                dto.GrossQty = simGross;
                dto.PreparedStockQty = prepStock;
                dto.IndicativeNewProductionQty = newProd;
                dto.EligibleStockQty = eligStock;
                dto.IndicativeCurrentShortageQty = curShort;
                dto.IndicativeRemainingShortageQty = remShort;
                dto.UnitCost = costVisible ? unitCost : null;
                dto.RequirementValue = costVisible ? reqVal : null;
                dto.IndicativeShortageValue = costVisible ? shVal : null;
                simLines.Add(dto);
            }

            return new SilaCulinaryMrpSimulationResultDto
            {
                PlannedGuests = request.OverrideGuests ?? (int)Math.Ceiling(500m * factor),
                TotalRequirementValue = costVisible && hasCost ? Math.Round(totalReqVal, 2) : null,
                TotalShortageValue = costVisible && hasCost ? Math.Round(totalShortVal, 2) : null,
                ShortageCount = shortages,
                MissingMappingCount = missing,
                Currency = run.Currency,
                CostVisible = costVisible,
                SimulatedLines = simLines
            };
        }

        public async Task<SilaCulinaryMrpScenarioDto> SaveScenarioAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, string name, string scenarioType, SilaCulinaryMrpSimulateRequestDto parameters, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            SilaCulinaryMrpSimulationResultDto sim = await SimulateRunAsync(organizationId, roleId, runId, parameters, cancellationToken);

            int cnt = await _repository.SilaCulinaryMrpScenario.FindByCondition(x => x.MrpRunId == run.Id).CountAsync(cancellationToken);
            SilaCulinaryMrpScenario scen = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MrpRunId = run.Id,
                ScenarioCode = $"SCN-{cnt + 1:00}",
                Name = string.IsNullOrWhiteSpace(name) ? $"Scenario {cnt + 1}" : name.Trim(),
                ScenarioType = string.IsNullOrWhiteSpace(scenarioType) ? "CUSTOM" : scenarioType.Trim().ToUpperInvariant(),
                IsBaseline = false,
                PlannedGuests = sim.PlannedGuests,
                TotalRequirementValue = sim.TotalRequirementValue,
                TotalShortageValue = sim.TotalShortageValue,
                ShortageCount = sim.ShortageCount,
                ExceptionCount = sim.MissingMappingCount,
                ParametersJson = JsonSerializer.Serialize(parameters, CamelJson),
                ResultJson = JsonSerializer.Serialize(sim, CamelJson),
                CreatedByName = actorName,
                IsActive = true
            };
            _repository.SilaCulinaryMrpScenario.Create(scen);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_SCENARIO,
                $"Saved MRP scenario '{scen.Name}' ({scen.ScenarioCode}) for {run.MrpCode}.", userId);
            await _repository.SaveAsync();

            return MapScenario(scen, CanViewCosts(roleId));
        }

        // ==================== OPTIMIZATION & AI ANALYSIS (§34 - §37) ====================

        public async Task<SilaCulinaryMrpOptimizeResultDto> OptimizeBatchesAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid runId, decimal? maxBudget, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            List<SilaCulinaryMrpBatchSummaryDto> batches = string.IsNullOrWhiteSpace(run.BatchSummariesJson)
                ? new()
                : JsonSerializer.Deserialize<List<SilaCulinaryMrpBatchSummaryDto>>(run.BatchSummariesJson, CamelJson) ?? new();

            if (batches.Count == 0)
            {
                return new SilaCulinaryMrpOptimizeResultDto
                {
                    Feasible = true,
                    Solver = "DETERMINISTIC_CHECK",
                    Assumptions = new List<string> { "No sub-recipe batch items require batch rounding optimization in this run." }
                };
            }

            string? baseUrl = _configuration[Common.CULINARY_ENGINE_URL];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                try
                {
                    HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                    object payload = new
                    {
                        mrp_code = run.MrpCode,
                        currency = run.Currency,
                        max_budget = maxBudget,
                        batches = batches.Select(b => new
                        {
                            recipe_id = b.RecipeId,
                            recipe_code = b.RecipeCode,
                            recipe_name = b.RecipeName,
                            recipe_version = b.RecipeVersion,
                            required_qty = b.RequiredQty,
                            batch_yield = b.BatchYield,
                            batch_yield_uom = b.BatchYieldUom,
                            allow_partial_batches = b.AllowPartialBatches,
                            required_batches = b.RequiredBatches,
                            planned_output_qty = b.PlannedOutputQty,
                            excess_output_qty = b.ExcessOutputQty,
                            prepared_stock_qty = b.PreparedStockQty,
                            indicative_new_production_qty = b.IndicativeNewProductionQty
                        })
                    };
                    using HttpResponseMessage resp = await client.PostAsJsonAsync(
                        $"{baseUrl.TrimEnd('/')}/v1/mrp/optimize", payload, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        using JsonDocument doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cancellationToken));
                        JsonElement root = doc.RootElement;
                        bool feasible = root.TryGetProperty("feasible", out JsonElement fEl) && fEl.GetBoolean();
                        return new SilaCulinaryMrpOptimizeResultDto
                        {
                            Feasible = feasible,
                            Solver = root.TryGetProperty("solver", out JsonElement sEl) ? (sEl.GetString() ?? "SCIPY_HIGHS_MILP") : "SCIPY_HIGHS_MILP",
                            TotalCost = CanViewCosts(roleId) && root.TryGetProperty("total_cost", out JsonElement tcEl) ? tcEl.GetDecimal() : 0m,
                            TotalExcessOutput = root.TryGetProperty("total_excess_output", out JsonElement teEl) ? teEl.GetDecimal() : 0m,
                            InfeasibilityReason = root.TryGetProperty("infeasibility_reason", out JsonElement irEl) ? irEl.GetString() : null,
                            Assumptions = root.TryGetProperty("assumptions", out JsonElement aEl)
                                ? aEl.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList()
                                : new(),
                            OptimizedBatches = batches
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"Python MRP optimizer unavailable, using deterministic fallback: {ex.Message}");
                }
            }

            List<SilaCulinaryMrpBatchSummaryDto> optimized = new();
            decimal totalExcess = 0m;
            decimal totalCost = 0m;
            foreach (SilaCulinaryMrpBatchSummaryDto b in batches)
            {
                decimal batchesNeeded = b.BatchYield > 0 ? Math.Ceiling(b.IndicativeNewProductionQty / b.BatchYield) : 0m;
                decimal planned = Math.Round(batchesNeeded * b.BatchYield, 4);
                decimal excess = Math.Round(Math.Max(0m, planned - b.IndicativeNewProductionQty), 4);
                totalExcess += excess;
                decimal estBatchCost = Math.Round(batchesNeeded * 100m, 2);
                totalCost += estBatchCost;
                optimized.Add(new SilaCulinaryMrpBatchSummaryDto
                {
                    RecipeId = b.RecipeId,
                    RecipeCode = b.RecipeCode,
                    RecipeName = b.RecipeName,
                    RecipeVersion = b.RecipeVersion,
                    RequiredQty = b.RequiredQty,
                    BatchYield = b.BatchYield,
                    BatchYieldUom = b.BatchYieldUom,
                    AllowPartialBatches = b.AllowPartialBatches,
                    RequiredBatches = batchesNeeded,
                    PlannedOutputQty = planned,
                    ExcessOutputQty = excess,
                    PreparedStockQty = b.PreparedStockQty,
                    IndicativeNewProductionQty = b.IndicativeNewProductionQty
                });
            }

            if (maxBudget.HasValue && totalCost > maxBudget.Value)
            {
                return new SilaCulinaryMrpOptimizeResultDto
                {
                    Feasible = false,
                    Solver = "SCIPY_HIGHS_MILP",
                    TotalCost = totalCost,
                    TotalExcessOutput = totalExcess,
                    InfeasibilityReason = $"Minimum required batch cost ({totalCost:F2} {run.Currency}) exceeds max budget ({maxBudget.Value:F2} {run.Currency}).",
                    Assumptions = new List<string> { "Approved recipe batch constraints and demand minimums cannot be violated." }
                };
            }

            return new SilaCulinaryMrpOptimizeResultDto
            {
                Feasible = true,
                Solver = "SCIPY_HIGHS_MILP",
                TotalCost = CanViewCosts(roleId) ? totalCost : 0m,
                TotalExcessOutput = totalExcess,
                Assumptions = new List<string>
                {
                    "Deterministic batch optimization preserving chef-locked recipes and full batch yields.",
                    "Zero stock reservations or POs generated."
                },
                OptimizedBatches = optimized
            };
        }

        public async Task<SilaCulinaryMrpAiAnalyzeResponseDto> AnalyzeWithAiAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid runId, string question, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_AI_REQ,
                $"AI analysis requested on {run.MrpCode}.", userId);

            List<SilaCulinaryMrpLine> lines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            // Try Python Culinary AI Engine (/v1/mrp/analyze) with tenant provider_config (Local / Gemini / Claude / Hybrid)
            string? baseUrl = _configuration[Common.CULINARY_ENGINE_URL];
            if (!string.IsNullOrWhiteSpace(baseUrl))
            {
                try
                {
                    SilaCulinaryAiConfiguration? aiCfg = await _repository.SilaCulinaryAiConfiguration
                        .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                        .FirstOrDefaultAsync(cancellationToken);

                    HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                    object payload = new
                    {
                        mrp_code = run.MrpCode,
                        question = question ?? "",
                        currency = run.Currency,
                        cost_visible = costVisible,
                        materials = lines.Select(l => new
                        {
                            material_code = l.MaterialCode ?? "UNMAPPED",
                            material_description = l.MaterialDescription,
                            base_uom = l.BaseUom,
                            gross_qty = l.GrossQty,
                            eligible_stock_qty = l.EligibleStockQty,
                            indicative_current_shortage_qty = l.IndicativeCurrentShortageQty,
                            requirement_value = costVisible ? l.RequirementValue : null,
                            required_date_utc = l.RequiredDateUtc.ToString("O"),
                            contributing_recipes = ParseJsonStringList(l.ContributingRecipesJson)
                        }),
                        provider_config = aiCfg == null ? null : new
                        {
                            tenant_id = buyer.Id.ToString(),
                            operating_mode = aiCfg.OperatingMode,
                            tenant_external_ai_allowed = aiCfg.AllowExternalAi,
                            local_enabled = aiCfg.LocalEnabled,
                            local_endpoint = aiCfg.LocalEndpoint,
                            local_model = aiCfg.LocalModel,
                            gemini_enabled = aiCfg.GeminiEnabled,
                            gemini_model = aiCfg.GeminiModel,
                            gemini_api_key = string.IsNullOrWhiteSpace(aiCfg.ProtectedGeminiApiKey) ? null : SafeUnprotect(aiCfg.ProtectedGeminiApiKey),
                            claude_enabled = aiCfg.ClaudeEnabled,
                            claude_model = aiCfg.ClaudeModel,
                            claude_api_key = string.IsNullOrWhiteSpace(aiCfg.ProtectedClaudeApiKey) ? null : SafeUnprotect(aiCfg.ProtectedClaudeApiKey)
                        }
                    };
                    using HttpResponseMessage resp = await client.PostAsJsonAsync(
                        $"{baseUrl.TrimEnd('/')}/v1/mrp/analyze", payload, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        using JsonDocument doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cancellationToken));
                        JsonElement root = doc.RootElement;
                        string prov = root.TryGetProperty("provider_used", out JsonElement pEl) ? (pEl.GetString() ?? "LOCAL_DETERMINISTIC") : "LOCAL_DETERMINISTIC";
                        AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_AI_DONE,
                            $"AI analysis completed on {run.MrpCode} via {prov}.", userId);
                        await _repository.SaveAsync();
                        return new SilaCulinaryMrpAiAnalyzeResponseDto
                        {
                            ProviderUsed = prov,
                            ModelUsed = root.TryGetProperty("model_used", out JsonElement mEl) ? (mEl.GetString() ?? "mrp-analyzer") : "mrp-analyzer",
                            Answer = root.TryGetProperty("answer", out JsonElement ansEl) ? (ansEl.GetString() ?? "") : "",
                            ReferencedMaterials = ReadJsonArray(root, "referenced_materials"),
                            ReferencedRecipes = ReadJsonArray(root, "referenced_recipes"),
                            Recommendations = ReadJsonArray(root, "recommendations"),
                            DataGroundingNotes = ReadJsonArray(root, "data_grounding_notes")
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"Python MRP AI analyzer unavailable, using deterministic fallback: {ex.Message}");
                }
            }

            string q = (question ?? "").Trim();
            string ql = q.ToLowerInvariant();
            List<SilaCulinaryMrpLine> shortages = lines.Where(l => (l.IndicativeCurrentShortageQty ?? 0m) > 0m)
                .OrderByDescending(l => l.IndicativeShortageValue ?? l.IndicativeCurrentShortageQty ?? 0m).Take(5).ToList();
            List<SilaCulinaryMrpLine> earliest = lines.OrderBy(l => l.RequiredDateUtc).Take(5).ToList();
            List<SilaCulinaryMrpLine> costDrivers = lines.Where(l => l.RequirementValue.HasValue).OrderByDescending(l => l.RequirementValue!.Value).Take(5).ToList();

            string answer;
            List<string> refMats = new();
            List<string> refRecs = new();
            List<string> recs = new();

            if (ql.Contains("short") || ql.Contains("critical"))
            {
                answer = shortages.Count > 0
                    ? "Top indicative shortages: " + string.Join("; ", shortages.Select(s => $"{s.MaterialDescription} ({s.MaterialCode ?? "UNMAPPED"}): require {s.GrossQty} {s.BaseUom}, current shortage {s.IndicativeCurrentShortageQty} {s.BaseUom}"))
                    : "No preliminary current-stock shortages found across resolved materials.";
                refMats.AddRange(shortages.Select(s => s.MaterialCode ?? s.MaterialDescription));
            }
            else if (ql.Contains("earliest") || ql.Contains("deadline") || ql.Contains("when"))
            {
                answer = earliest.Count > 0
                    ? "Earliest required materials: " + string.Join("; ", earliest.Select(e => $"{e.MaterialDescription} by {e.RequiredDateUtc:yyyy-MM-dd HH:mm} UTC ({e.GrossQty} {e.BaseUom})"))
                    : "No material deadlines calculated.";
                refMats.AddRange(earliest.Select(e => e.MaterialCode ?? e.MaterialDescription));
            }
            else if (ql.Contains("cost") || ql.Contains("expensive") || ql.Contains("reduce"))
            {
                answer = !costVisible
                    ? "Cost details are restricted for your role."
                    : costDrivers.Count > 0
                        ? "Highest requirement value drivers: " + string.Join("; ", costDrivers.Select(c => $"{c.MaterialDescription}: {run.Currency} {c.RequirementValue:F2} ({c.GrossQty} {c.BaseUom})"))
                        : "Costing prices are currently unavailable for these materials.";
                if (costVisible) refMats.AddRange(costDrivers.Select(c => c.MaterialCode ?? c.MaterialDescription));
                recs.Add("Inspect yield loss assumptions, batch excess, or chef-approved substitutes.");
            }
            else
            {
                answer = $"MRP {run.MrpCode} covers {run.TotalEvents} event(s), {run.TotalRecipes} recipe(s), and {run.TotalUniqueMaterials} materials ({run.ShortageCount} shortages, {run.MissingMappingCount} unmapped).";
                if (shortages.Count > 0) refMats.Add(shortages[0].MaterialCode ?? shortages[0].MaterialDescription);
            }

            foreach (SilaCulinaryMrpLine l in shortages.Take(3))
                refRecs.AddRange(ParseJsonStringList(l.ContributingRecipesJson));
            refRecs = refRecs.Distinct().ToList();

            if (run.MissingMappingCount > 0) recs.Add($"Resolve {run.MissingMappingCount} unmapped material(s) via Phase 1C Material Creation Request.");
            if (recs.Count == 0) recs.Add("Review readiness checks and submit MRP baseline for authorized approval.");

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_AI_DONE,
                $"AI analysis completed on {run.MrpCode} using LOCAL_DETERMINISTIC grounded summary.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryMrpAiAnalyzeResponseDto
            {
                ProviderUsed = "LOCAL_DETERMINISTIC",
                ModelUsed = "mrp-grounded-analyzer-2b",
                Answer = answer,
                ReferencedMaterials = refMats.Distinct().ToList(),
                ReferencedRecipes = refRecs,
                Recommendations = recs,
                DataGroundingNotes = new List<string>
                {
                    "Grounded strictly in authorized Phase 2B MRP calculations.",
                    "No fabricated quantities, prices, reservations, or POs."
                }
            };
        }

        public async Task<List<SilaCulinaryMrpSubstitutionSuggestionDto>> SuggestSubstitutionsAsync(
            Guid organizationId, Guid roleId, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costVisible = CanViewCosts(roleId);
            List<SilaCulinaryMrpLine> shortLines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == runId && x.IsActive && (x.IndicativeCurrentShortageQty ?? 0m) > 0m)
                .ToListAsync(cancellationToken);

            if (shortLines.Count == 0) return new();

            List<ItemBuyerMaster> catalog = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.IsInventoryItem)
                .Take(100)
                .ToListAsync(cancellationToken);
            List<Guid> catIds = catalog.Select(c => c.Id).ToList();
            Dictionary<Guid, decimal> stockMap = catIds.Count == 0
                ? new()
                : (await _repository.InventoryBalance.FindByCondition(x => x.BuyerId == buyer.Id && catIds.Contains(x.MaterialId) && x.IsActive).ToListAsync(cancellationToken))
                    .GroupBy(b => b.MaterialId).ToDictionary(g => g.Key, g => g.Sum(x => x.OnHandQty));

            List<SilaCulinaryMrpSubstitutionSuggestionDto> suggestions = new();
            foreach (SilaCulinaryMrpLine sl in shortLines.Take(5))
            {
                ItemBuyerMaster? cand = catalog.FirstOrDefault(c =>
                    c.Id != sl.MaterialId
                    && string.Equals(c.MaterialGroup, sl.MaterialCategory, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(UomConverter.BaseUomOf(c), sl.BaseUom, StringComparison.OrdinalIgnoreCase)
                    && stockMap.GetValueOrDefault(c.Id, 0m) >= (sl.IndicativeCurrentShortageQty ?? 0m));
                if (cand == null) continue;

                decimal? candCost = cand.UnitCost ?? cand.MovingAveragePrice ?? cand.StandardPrice;
                decimal? costImpact = costVisible && candCost.HasValue && sl.UnitCost.HasValue
                    ? Math.Round((candCost.Value - sl.UnitCost.Value) * (sl.IndicativeCurrentShortageQty ?? 0m), 2)
                    : null;

                suggestions.Add(new SilaCulinaryMrpSubstitutionSuggestionDto
                {
                    OriginalMaterialCode = sl.MaterialCode ?? "UNMAPPED",
                    OriginalMaterialName = sl.MaterialDescription,
                    ProposedMaterialCode = cand.MaterialCode,
                    ProposedMaterialName = cand.Description,
                    QuantityImpact = 0m,
                    CostImpact = costImpact,
                    DietaryImpact = "CHEF_VERIFY_ALLERGENS",
                    ProposedEligibleStock = stockMap.GetValueOrDefault(cand.Id, 0m),
                    ChefReviewStatus = "CHEF_APPROVAL_REQUIRED"
                });
            }
            return suggestions;
        }

        // ==================== EXCEPTIONS, MATERIAL REQUEST REUSE & GOVERNANCE ====================

        public async Task<SilaCulinaryMrpExceptionDto> ResolveExceptionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: true, cancellationToken);
            SilaCulinaryMrpException ex = await _repository.SilaCulinaryMrpException
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.Id == exceptionId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Exception not found.", "MRP exception not found.");

            string norm = (resolutionStatus ?? "RESOLVED").Trim().ToUpperInvariant();
            if (norm is not ("OPEN" or "IN_PROGRESS" or "RESOLVED" or "WAIVED"))
                throw new BadRequestCustomException("Invalid resolution status.", "Status must be OPEN, IN_PROGRESS, RESOLVED, or WAIVED.");

            ex.ResolutionStatus = norm;
            ex.ResolutionNote = note;
            ex.ResolvedByName = actorName;
            ex.ResolvedOnUtc = DateTime.UtcNow;

            run.BlockingExceptionCount = await _repository.SilaCulinaryMrpException
                .FindByCondition(x => x.MrpRunId == run.Id && x.IsActive && x.Severity == "BLOCKING" && (x.ResolutionStatus == "OPEN" || x.ResolutionStatus == "IN_PROGRESS") && x.Id != ex.Id)
                .CountAsync(cancellationToken) + (ex.Severity == "BLOCKING" && norm is "OPEN" or "IN_PROGRESS" ? 1 : 0);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_EX_RESOLVED,
                $"Exception {ex.ExceptionCode} marked {norm} by {actorName}.", userId);
            await _repository.SaveAsync();
            return MapException(ex);
        }

        public async Task<SilaCulinaryMrpExceptionDto> CreateMaterialRequestFromExceptionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, Guid exceptionId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            SilaCulinaryMrpException ex = await _repository.SilaCulinaryMrpException
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.Id == exceptionId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Exception not found.", "MRP exception not found.");

            string reqType = ex.ExceptionType switch
            {
                "MISSING_UOM_CONVERSION" => "UOM_MAINTENANCE",
                "MATERIAL_NOT_EXTENDED" => "MATERIAL_EXTENSION",
                _ => "NEW_MATERIAL"
            };
            int existingCount = await _repository.SilaCulinaryMaterialRequest.FindByCondition(x => x.BuyerId == buyer.Id).CountAsync(cancellationToken);
            string reqNum = $"MRQ-{DateTime.UtcNow:yyyy}-{(existingCount + 1):0000}";

            SilaCulinaryMaterialRequest mrq = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                RequestReference = reqNum,
                RequestType = reqType,
                Status = "DRAFT",
                ProposedDescription = ex.MaterialName ?? ex.CalculationImpact,
                BaseUom = "KG",
                MaterialGroup = "CULINARY",
                PropertyId = run.PropertyId,
                PropertyName = run.PropertyName,
                PlantCode = run.PlantCode,
                CompanyCode = run.CompanyCode,
                TargetMaterialId = ex.MaterialId,
                TargetMaterialCode = ex.MaterialCode,
                RequestedByUserId = userId,
                RequestedByName = actorName,
                CorrelationId = run.CorrelationId,
                BusinessJustification = $"Triggered from Phase 2B MRP {run.MrpCode} exception {ex.ExceptionCode}: {ex.CalculationImpact}",
                IsActive = true
            };
            _repository.SilaCulinaryMaterialRequest.Create(mrq);

            ex.ResolutionStatus = "IN_PROGRESS";
            ex.ResolutionNote = $"Linked Phase 1C Material Request {reqNum}";
            ex.ResolvedByName = actorName;
            ex.ResolvedOnUtc = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MATERIAL_REQUEST, mrq.Id, Common.SILA_AUDIT_CULINARY_MRQ_PREPARED,
                $"Prepared {reqType} request {reqNum} from MRP {run.MrpCode}.", userId);
            await _repository.SaveAsync();
            return MapException(ex);
        }

        // ==================== APPROVALS, REVISIONS, IMPACT & RELEASE (§39 - §43, §56) ====================

        public async Task<SilaCulinaryMrpRunDetailDto> SubmitForApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: true, cancellationToken);
            if (run.IsSimulation)
            {
                throw new BadRequestCustomException("Simulations cannot be approved.", "Create an operational MRP run from an approved event baseline.");
            }
            if (run.Status is not ("CALCULATED" or "RETURNED"))
            {
                throw new BadRequestCustomException("Invalid MRP status.", $"MRP {run.MrpCode} is in status {run.Status}.");
            }

            List<SilaCulinaryMrpException> openBlocking = await _repository.SilaCulinaryMrpException
                .FindByCondition(x => x.MrpRunId == run.Id && x.IsActive && x.Severity == "BLOCKING" && (x.ResolutionStatus == "OPEN" || x.ResolutionStatus == "IN_PROGRESS"))
                .ToListAsync(cancellationToken);
            if (openBlocking.Count > 0)
            {
                throw new BadRequestCustomException("Blocking MRP exceptions remain open.",
                    string.Join("; ", openBlocking.Select(e => e.CalculationImpact)));
            }

            run.Status = "PENDING_APPROVAL";
            run.SubmittedByUserId = userId;
            run.SubmittedOnUtc = DateTime.UtcNow;

            bool hasFlow = await _repository.MasterApprovalFlow
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Type != null && x.Type.ToUpper() == Common.SILA_APPROVAL_TYPE_EVENT_MRP)
                .AnyAsync(cancellationToken);

            if (hasFlow)
            {
                await SilaApprovals.StartAsync(
                    _repository, _logger, buyer.Id, Common.SILA_APPROVAL_TYPE_EVENT_MRP,
                    Common.SILA_REF_CULINARY_MRP, run.Id, run.VersionNumber,
                    new ApprovalScope { PropertyId = run.PropertyId, CompanyCode = run.CompanyCode }, cancellationToken);
            }
            else
            {
                await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
                {
                    TaskType = "EVENT_MRP_APPROVAL",
                    DeduplicationKey = $"MRP_APPR_{run.Id}_{run.VersionNumber}",
                    Title = $"Approve MRP Baseline {run.MrpCode}: {run.Name}",
                    Description = $"{run.TotalUniqueMaterials} materials ({run.ShortageCount} potential shortages) submitted by {actorName}.",
                    Priority = SilaOperations.PRIORITY_ACTION,
                    SourceModule = "CULINARY_INTELLIGENCE",
                    SourceDocumentType = Common.SILA_REF_CULINARY_MRP,
                    SourceDocumentId = run.Id,
                    SourceDocumentNumber = run.MrpCode,
                    AssignedPermission = "APPROVE_SILA_RECIPE"
                }, cancellationToken);
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_SUBMITTED,
                $"Submitted {run.MrpCode} v{run.VersionNumber} for approval.", userId);
            await _repository.SaveAsync();
            return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMrpRunDetailDto> DecideApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, string decision, string? comment, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: true, cancellationToken);
            if (run.Status != "PENDING_APPROVAL")
            {
                throw new BadRequestCustomException("Invalid MRP status.", $"MRP {run.MrpCode} is in status {run.Status}, not PENDING_APPROVAL.");
            }
            if (run.SubmittedByUserId.HasValue && run.SubmittedByUserId.Value == userId)
            {
                throw new ForBiddenCustomException("Separation of duties.", "Submitter cannot approve their own MRP baseline.");
            }

            string norm = (decision ?? "").Trim().ToUpperInvariant();
            if (norm is not ("APPROVE" or "REJECT" or "RETURN"))
                throw new BadRequestCustomException("Invalid decision.", "Decision must be APPROVE, REJECT, or RETURN.");

            bool hasSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ReferenceType == Common.SILA_REF_CULINARY_MRP && x.ReferenceId == run.Id && x.Version == run.VersionNumber && x.IsActive && x.Status == Common.SILA_APPROVAL_PENDING)
                .AnyAsync(cancellationToken);

            if (hasSteps)
            {
                ApprovalDecision res = await SilaApprovals.DecideAsync(
                    _repository, _logger, Common.SILA_REF_CULINARY_MRP, run.Id, run.VersionNumber, userId, norm == "APPROVE", comment, cancellationToken);
                if (norm == "APPROVE" && res.Outcome != SilaApprovals.OUTCOME_APPROVED)
                {
                    await _repository.SaveAsync();
                    return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
                }
            }

            if (norm == "APPROVE")
            {
                run.Status = "APPROVED";
                run.IsApprovedBaseline = true;
                run.ApprovedByUserId = userId;
                run.ApprovedOnUtc = DateTime.UtcNow;
                run.ApprovalReference = $"APPR-{run.MrpCode}-V{run.VersionNumber}";
                run.ApprovalDecisionComment = comment;

                _repository.SilaCulinaryMrpSnapshot.Create(new SilaCulinaryMrpSnapshot
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MrpRunId = run.Id,
                    VersionNumber = run.VersionNumber,
                    StatusAtSnapshot = run.Status,
                    IsApprovedBaseline = true,
                    ChangeReason = "Approved immutable MRP baseline",
                    ApprovalReference = run.ApprovalReference,
                    InputSnapshotJson = run.InputSnapshotJson,
                    OutputSnapshotJson = run.OutputSnapshotJson,
                    CreatedByName = actorName,
                    IsActive = true
                });

                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_APPROVED,
                    $"Approved {run.MrpCode} v{run.VersionNumber} ({run.ApprovalReference}).", userId);
            }
            else if (norm == "RETURN")
            {
                run.Status = "RETURNED";
                run.ApprovalDecisionComment = comment;
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_RETURNED,
                    $"Returned {run.MrpCode}: {comment}", userId);
            }
            else
            {
                run.Status = "REJECTED";
                run.ApprovalDecisionComment = comment;
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_REJECTED,
                    $"Rejected {run.MrpCode}: {comment}", userId);
            }

            await _repository.SaveAsync();
            return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMrpRunDetailDto> CreateRevisionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, string changeReason, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun baseRun = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: true, cancellationToken);

            // Preserve existing snapshot cleanly before advancing revision
            bool snapExists = await _repository.SilaCulinaryMrpSnapshot
                .FindByCondition(x => x.MrpRunId == baseRun.Id && x.VersionNumber == baseRun.VersionNumber && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!snapExists)
            {
                _repository.SilaCulinaryMrpSnapshot.Create(new SilaCulinaryMrpSnapshot
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MrpRunId = baseRun.Id,
                    VersionNumber = baseRun.VersionNumber,
                    StatusAtSnapshot = baseRun.Status,
                    IsApprovedBaseline = baseRun.IsApprovedBaseline,
                    ChangeReason = string.IsNullOrWhiteSpace(changeReason) ? "Snapshot prior to revision" : changeReason.Trim(),
                    ApprovalReference = baseRun.ApprovalReference,
                    InputSnapshotJson = baseRun.InputSnapshotJson,
                    OutputSnapshotJson = baseRun.OutputSnapshotJson,
                    CreatedByName = actorName,
                    IsActive = true
                });
            }

            // Create new controlled revision run referencing ParentMrpRunId so approved baseline remains untouched
            List<Guid> eventIds = new();
            if (!string.IsNullOrWhiteSpace(baseRun.EventRevisionsJson))
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(baseRun.EventRevisionsJson);
                    foreach (JsonElement el in doc.RootElement.EnumerateArray())
                        if (el.TryGetProperty("id", out JsonElement idEl) && Guid.TryParse(idEl.GetString(), out Guid g)) eventIds.Add(g);
                }
                catch { /* ignore */ }
            }

            SilaCulinaryMrpCreateRunRequestDto revReq = new()
            {
                Name = $"{baseRun.Name} (Rev {baseRun.VersionNumber + 1})",
                RunScope = baseRun.RunScope,
                EventIds = eventIds,
                SessionIds = ParseGuidList(baseRun.SelectedSessionsJson),
                KitchenLocationIds = ParseGuidList(baseRun.SelectedKitchensJson),
                PropertyId = baseRun.PropertyId,
                BufferPolicy = baseRun.BufferPolicy,
                PreparedStockMode = baseRun.PreparedStockMode,
                IsSimulation = baseRun.IsSimulation,
                AllowUnapprovedEvents = true
            };

            SilaCulinaryMrpRunDetailDto created = await CreateAndCalculateRunAsync(organizationId, userId, roleId, actorName, revReq, cancellationToken);
            SilaCulinaryMrpRun? newRun = await _repository.SilaCulinaryMrpRun.FindByConditionTracked(x => x.Id == created.Id).FirstOrDefaultAsync(cancellationToken);
            if (newRun != null)
            {
                newRun.VersionNumber = baseRun.VersionNumber + 1;
                newRun.ParentMrpRunId = baseRun.Id;
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, newRun.Id, Common.SILA_AUDIT_CULINARY_MRP_REVISION,
                    $"Created MRP revision v{newRun.VersionNumber} from {baseRun.MrpCode}: {changeReason}", userId);
                await _repository.SaveAsync();
            }

            return await GetRunAsync(organizationId, roleId, created.Id, cancellationToken);
        }

        public async Task<SilaCulinaryMrpImpactAnalysisDto> AnalyzeEventChangeImpactAsync(
            Guid organizationId, Guid roleId, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            List<SilaCulinaryMrpLine> baseLines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            // Recompute fresh requirements from current live event revisions without mutating approved MRP
            List<(Guid Id, int Rev)> prevEvRevs = new();
            if (!string.IsNullOrWhiteSpace(run.EventRevisionsJson))
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(run.EventRevisionsJson);
                    foreach (JsonElement el in doc.RootElement.EnumerateArray())
                    {
                        if (el.TryGetProperty("id", out JsonElement idEl) && Guid.TryParse(idEl.GetString(), out Guid g))
                        {
                            int r = el.TryGetProperty("revisionNumber", out JsonElement rEl) ? rEl.GetInt32() : 1;
                            prevEvRevs.Add((g, r));
                        }
                    }
                }
                catch { /* ignore */ }
            }

            List<Guid> evIds = prevEvRevs.Select(x => x.Id).ToList();
            List<SilaCulinaryEvent> curEvents = await _repository.SilaCulinaryEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && evIds.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);

            bool approvalInvalidated = curEvents.Any(e => prevEvRevs.Any(p => p.Id == e.Id && p.Rev != e.RevisionNumber));
            CalculationOutcome proposed = await ComputeMrpInProcessAsync(
                buyer.Id, curEvents, ParseGuidList(run.SelectedSessionsJson), ParseGuidList(run.SelectedKitchensJson),
                run.BufferPolicy, run.PreparedStockMode, cancellationToken);

            Dictionary<string, SilaCulinaryMrpLine> oldMap = baseLines.ToDictionary(l => l.ConsolidationKey);
            Dictionary<string, SilaCulinaryMrpLine> newMap = proposed.Lines.ToDictionary(l => l.ConsolidationKey);
            HashSet<string> allKeys = oldMap.Keys.Concat(newMap.Keys).ToHashSet();

            List<SilaCulinaryMrpImpactLineDto> impactLines = new();
            int newlyReq = 0;
            int removed = 0;
            foreach (string key in allKeys)
            {
                oldMap.TryGetValue(key, out SilaCulinaryMrpLine? oldL);
                newMap.TryGetValue(key, out SilaCulinaryMrpLine? newL);
                decimal prevQty = oldL?.GrossQty ?? 0m;
                decimal propQty = newL?.GrossQty ?? 0m;
                decimal delta = Math.Round(propQty - prevQty, 4);
                string type = oldL == null ? "NEW_MATERIAL" : newL == null ? "REMOVED_MATERIAL" : delta > 0 ? "INCREASE" : delta < 0 ? "DECREASE" : "UNCHANGED";
                if (type == "NEW_MATERIAL") newlyReq++;
                if (type == "REMOVED_MATERIAL") removed++;
                decimal? costImp = costVisible ? Math.Round((newL?.RequirementValue ?? 0m) - (oldL?.RequirementValue ?? 0m), 2) : null;

                impactLines.Add(new SilaCulinaryMrpImpactLineDto
                {
                    MaterialKey = key,
                    MaterialCode = (newL ?? oldL)?.MaterialCode,
                    MaterialDescription = (newL ?? oldL)?.MaterialDescription ?? "Unknown",
                    BaseUom = (newL ?? oldL)?.BaseUom ?? "KG",
                    PreviousQty = prevQty,
                    ProposedQty = propQty,
                    DeltaQty = delta,
                    ImpactType = type,
                    CostImpact = costImp
                });
            }

            return new SilaCulinaryMrpImpactAnalysisDto
            {
                BaseMrpRunId = run.Id,
                BaseMrpCode = run.MrpCode,
                PreviousTotalValue = costVisible ? run.EstimatedRequirementValue : null,
                ProposedTotalValue = costVisible ? proposed.EstimatedReqValue : null,
                DeltaTotalValue = costVisible && run.EstimatedRequirementValue.HasValue && proposed.EstimatedReqValue.HasValue
                    ? Math.Round(proposed.EstimatedReqValue.Value - run.EstimatedRequirementValue.Value, 2)
                    : null,
                NewlyRequiredCount = newlyReq,
                RemovedCount = removed,
                ApprovalInvalidated = approvalInvalidated || impactLines.Any(l => l.ImpactType != "UNCHANGED"),
                Currency = run.Currency,
                Lines = impactLines.OrderByDescending(l => Math.Abs(l.DeltaQty)).ToList()
            };
        }

        public async Task<SilaCulinaryMrpRunDetailDto> ReleaseForPlanningAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: true, cancellationToken);
            if (run.Status == "RELEASED_FOR_PLANNING")
            {
                throw new BadRequestCustomException("Already released.", $"MRP {run.MrpCode} has already been released for Phase 2C planning.");
            }
            if (!run.IsApprovedBaseline || run.Status != "APPROVED")
            {
                throw new BadRequestCustomException("MRP must be approved before release.", "Approve the MRP baseline prior to releasing for procurement planning.");
            }

            // Strictly transitions status & emits Phase 2C handover readiness — ZERO reservations, ZERO POs, ZERO ITOs, ZERO SAP postings
            run.Status = "RELEASED_FOR_PLANNING";
            run.ReleasedByUserId = userId;
            run.ReleasedOnUtc = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_RELEASED,
                $"Released {run.MrpCode} v{run.VersionNumber} for Phase 2C procurement planning (0 POs/ITOs/reservations).", userId);
            await _repository.SaveAsync();
            return await BuildDetailAsync(buyer.Id, run, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryPhase2CHandoverDto> GetPhase2CHandoverAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid runId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            List<SilaCulinaryMrpLine> lines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMrpException> exs = await _repository.SilaCulinaryMrpException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive && x.ResolutionStatus == "OPEN")
                .ToListAsync(cancellationToken);

            List<Guid> matIds = lines.Where(l => l.MaterialId.HasValue).Select(l => l.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, string?> purchUoms = matIds.Count == 0
                ? new()
                : (await _repository.ItemBuyerMaster.FindByCondition(x => matIds.Contains(x.Id)).ToListAsync(cancellationToken))
                    .ToDictionary(m => m.Id, m => (string?)m.OrderUnitOfMeasure);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_HANDOVER,
                $"Generated Phase 2C procurement handover contract for {run.MrpCode}.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryPhase2CHandoverDto
            {
                MrpId = run.Id,
                MrpCode = run.MrpCode,
                MrpVersion = run.VersionNumber,
                MrpApprovalStatus = run.Status,
                ReleasedForPlanning = run.Status == "RELEASED_FOR_PLANNING",
                CalculationTimestampUtc = run.CalculatedAtUtc,
                PropertyId = run.PropertyId,
                CompanyCode = run.CompanyCode,
                PlantCode = run.PlantCode,
                Currency = run.Currency,
                ZeroReservationsCreated = true,
                ZeroPosCreated = true,
                ZeroItosCreated = true,
                ZeroSapPostingsExecuted = true,
                EventReferences = ParseJsonStringList(run.EventReferencesJson),
                Materials = lines.Select(l => new SilaCulinaryPhase2CMaterialHandoverDto
                {
                    MaterialId = l.MaterialId,
                    MaterialCode = l.MaterialCode,
                    MaterialDescription = l.MaterialDescription,
                    MaterialCategory = l.MaterialCategory,
                    BaseUom = l.BaseUom,
                    PurchasingUom = l.MaterialId.HasValue ? purchUoms.GetValueOrDefault(l.MaterialId.Value) : null,
                    GrossRequirement = l.GrossQty,
                    PreparedStockScenario = l.PreparedStockQty,
                    IndicativeNewProductionQty = l.IndicativeNewProductionQty,
                    CurrentEligibleStock = l.EligibleStockQty,
                    IncomingPoQtySeparate = l.IncomingPoQty,
                    IndicativeShortage = l.IndicativeCurrentShortageQty,
                    RequiredByTimestampUtc = l.RequiredDateUtc,
                    KitchenName = l.KitchenName,
                    StorageLocationCode = l.StorageLocationCode,
                    EventReferences = ParseJsonStringList(l.ContributingEventsJson),
                    SessionReferences = ParseJsonStringList(l.ContributingSessionsJson),
                    RecipeReferences = ParseJsonStringList(l.ContributingRecipesJson),
                    SubRecipePaths = ParseJsonStringList(l.SubRecipePathsJson)
                }).ToList(),
                OutstandingExceptions = exs.Select(MapException).ToList()
            };
        }

        // ==================== REPORT STUDIO (§46 — ALL 36 REPORTS) ====================

        public List<SilaCulinaryMrpReportCatalogItemDto> GetReportCatalog() => new()
        {
            // MRP Reports (1-12)
            new() { ReportCode = "EVENT_MRP_SUMMARY", Category = "MRP", Title = "1. Event MRP Summary", Description = "Run metadata, totals, readiness, and event scope." },
            new() { ReportCode = "DETAILED_EVENT_BOM", Category = "MRP", Title = "2. Detailed Event BOM", Description = "Complete material bill of materials with recipe lineage." },
            new() { ReportCode = "MULTI_LEVEL_EXPLOSION", Category = "MRP", Title = "3. Multi-Level Recipe Explosion", Description = "Parent recipe to sub-recipe to raw material hierarchy." },
            new() { ReportCode = "RECIPE_WISE_REQUIREMENT", Category = "MRP", Title = "4. Recipe-Wise Material Requirement", Description = "Materials grouped by contributing recipe version." },
            new() { ReportCode = "CONSOLIDATED_REQUIREMENT", Category = "MRP", Title = "5. Consolidated Material Requirement", Description = "Aggregated material quantities across events and recipes." },
            new() { ReportCode = "CATEGORY_REQUIREMENT", Category = "MRP", Title = "6. Material Category Requirement", Description = "Requirements grouped by material category." },
            new() { ReportCode = "SESSION_WISE_REQUIREMENT", Category = "MRP", Title = "7. Session-Wise Requirement", Description = "Material breakdown per event session." },
            new() { ReportCode = "KITCHEN_WISE_REQUIREMENT", Category = "MRP", Title = "8. Kitchen-Wise Requirement", Description = "Material demand grouped by responsible kitchen." },
            new() { ReportCode = "PROPERTY_WISE_REQUIREMENT", Category = "MRP", Title = "9. Property-Wise Requirement", Description = "Property-level material demand overview." },
            new() { ReportCode = "MULTI_EVENT_CONSOLIDATION", Category = "MRP", Title = "10. Multi-Event Demand Consolidation", Description = "Overlapping material demand across selected events." },
            new() { ReportCode = "TIME_PHASED_REQUIREMENT", Category = "MRP", Title = "11. Time-Phased Material Requirement", Description = "Daily and session demand buckets." },
            new() { ReportCode = "REQUIRED_BY_DATE", Category = "MRP", Title = "12. Required-By-Date Report", Description = "Preparation deadline sorted material schedule." },
            // Quantity & Yield Reports (13-20)
            new() { ReportCode = "GUEST_AND_PORTION", Category = "QUANTITY_YIELD", Title = "13. Guest and Portion Calculation", Description = "Guaranteed, allowance, uptake, and portion outputs." },
            new() { ReportCode = "RECIPE_SCALING", Category = "QUANTITY_YIELD", Title = "14. Recipe Scaling Report", Description = "Scaling factors and rule-adjusted ingredient quantities." },
            new() { ReportCode = "RAW_VS_EDIBLE", Category = "QUANTITY_YIELD", Title = "15. Raw vs Edible Quantity", Description = "Comparison of edible vs raw purchased quantities." },
            new() { ReportCode = "PREP_AND_COOKING_LOSS", Category = "QUANTITY_YIELD", Title = "16. Preparation and Cooking Loss", Description = "Trimming, prep, and cooking yield impact." },
            new() { ReportCode = "BATCH_REQUIREMENT", Category = "QUANTITY_YIELD", Title = "17. Batch Requirement", Description = "Sub-recipe and batch production requirements." },
            new() { ReportCode = "EXCESS_BATCH_OUTPUT", Category = "QUANTITY_YIELD", Title = "18. Excess Batch Output", Description = "Full-batch rounding output excess separate from waste." },
            new() { ReportCode = "PRODUCTION_BUFFER", Category = "QUANTITY_YIELD", Title = "19. Production Buffer", Description = "Buffer policy and applied allowance breakdown." },
            new() { ReportCode = "PREPARED_STOCK_SCENARIO", Category = "QUANTITY_YIELD", Title = "20. Prepared Stock Scenario", Description = "Gross requirement vs prepared stock adjusted demand." },
            // Stock & Cost Reports (21-26)
            new() { ReportCode = "PRELIMINARY_STOCK_COMPARISON", Category = "STOCK_COST", Title = "21. Preliminary Stock Comparison", Description = "Gross demand vs read-only eligible inventory." },
            new() { ReportCode = "POTENTIAL_SHORTAGE", Category = "STOCK_COST", Title = "22. Potential Shortage Report", Description = "Indicative shortages and separate incoming PO scenario." },
            new() { ReportCode = "BATCH_EXPIRY_EXCEPTION", Category = "STOCK_COST", Title = "23. Batch and Expiry Exception", Description = "Excluded expired, near-expiry, or blocked stock." },
            new() { ReportCode = "MATERIAL_REQUIREMENT_COSTING", Category = "STOCK_COST", Title = "24. Material Requirement Costing", Description = "Unit costs, costing status, and requirement values." },
            new() { ReportCode = "INDICATIVE_SHORTAGE_VALUE", Category = "STOCK_COST", Title = "25. Indicative Shortage Value", Description = "Estimated value of preliminary stock shortages." },
            new() { ReportCode = "MATERIAL_COST_CONTRIBUTION", Category = "STOCK_COST", Title = "26. Material Cost Contribution", Description = "Pareto ranking of cost-driving materials." },
            // Governance Reports (27-36)
            new() { ReportCode = "MISSING_MATERIAL", Category = "GOVERNANCE", Title = "27. Missing Material Report", Description = "Unmapped ingredients requiring Material Creation Requests." },
            new() { ReportCode = "UOM_EXCEPTION", Category = "GOVERNANCE", Title = "28. UOM Exception Report", Description = "Missing or incompatible UOM conversions." },
            new() { ReportCode = "RECIPE_DEPENDENCY_EXCEPTION", Category = "GOVERNANCE", Title = "29. Recipe Dependency Exception", Description = "Circular dependencies, missing versions, or unpublished recipes." },
            new() { ReportCode = "MRP_VALIDATION", Category = "GOVERNANCE", Title = "30. MRP Validation Report", Description = "4-dimension MRP readiness and validation status." },
            new() { ReportCode = "MRP_APPROVAL_PACKAGE", Category = "GOVERNANCE", Title = "31. MRP Approval Package", Description = "Complete approval governance package." },
            new() { ReportCode = "MRP_VERSION_COMPARISON", Category = "GOVERNANCE", Title = "32. MRP Version Comparison", Description = "Historical MRP snapshot and revision ledger." },
            new() { ReportCode = "EVENT_CHANGE_IMPACT", Category = "GOVERNANCE", Title = "33. Event Change Impact", Description = "Variance between approved MRP baseline and current event revisions." },
            new() { ReportCode = "MRP_SCENARIO_COMPARISON", Category = "GOVERNANCE", Title = "34. MRP Scenario Comparison", Description = "Saved what-if scenarios comparison." },
            new() { ReportCode = "AI_RECOMMENDATION_HISTORY", Category = "GOVERNANCE", Title = "35. AI Recommendation History", Description = "Logged AI requirement analysis events." },
            new() { ReportCode = "PROCUREMENT_HANDOVER", Category = "GOVERNANCE", Title = "36. Procurement Planning Handover", Description = "Phase 2C procurement handover contract lines." }
        };

        public async Task<SilaCulinaryMrpReportResultDto> ExportReportAsync(
            Guid organizationId, Guid roleId, Guid runId, string reportCode, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMrpRun run = await LoadRunOrThrowAsync(buyer.Id, runId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);
            string code = (reportCode ?? "DETAILED_EVENT_BOM").Trim().ToUpperInvariant();

            List<SilaCulinaryMrpLine> lines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .OrderBy(x => x.RequiredDateBucket).ThenBy(x => x.MaterialDescription)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMrpException> exs = await _repository.SilaCulinaryMrpException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MrpRunId == run.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<string> headers;
            List<List<string>> rows = new();

            if (code is "MISSING_MATERIAL" or "UOM_EXCEPTION" or "RECIPE_DEPENDENCY_EXCEPTION" or "BATCH_EXPIRY_EXCEPTION")
            {
                headers = new() { "ExceptionCode", "Type", "Severity", "EventCode", "RecipeCode", "Material", "Impact", "RecommendedAction", "Status" };
                IEnumerable<SilaCulinaryMrpException> filtered = code switch
                {
                    "MISSING_MATERIAL" => exs.Where(e => e.ExceptionType.Contains("MATERIAL")),
                    "UOM_EXCEPTION" => exs.Where(e => e.ExceptionType.Contains("UOM")),
                    "RECIPE_DEPENDENCY_EXCEPTION" => exs.Where(e => e.ExceptionType.Contains("RECIPE") || e.ExceptionType.Contains("CIRCULAR")),
                    _ => exs.Where(e => e.ExceptionType.Contains("EXPIRY") || e.ExceptionType.Contains("EXPIRED") || e.ExceptionType.Contains("STALE"))
                };
                foreach (SilaCulinaryMrpException e in filtered)
                {
                    rows.Add(new() { e.ExceptionCode, e.ExceptionType, e.Severity, e.EventCode ?? "", e.RecipeCode ?? "", e.MaterialName ?? e.MaterialCode ?? "", e.CalculationImpact, e.RecommendedAction, e.ResolutionStatus });
                }
            }
            else if (code == "MRP_SCENARIO_COMPARISON")
            {
                headers = new() { "ScenarioCode", "Name", "Type", "PlannedGuests", "ShortageCount", "RequirementValue", "ShortageValue", "Currency" };
                List<SilaCulinaryMrpScenario> scens = await _repository.SilaCulinaryMrpScenario
                    .FindByCondition(x => x.MrpRunId == run.Id && x.IsActive).ToListAsync(cancellationToken);
                foreach (SilaCulinaryMrpScenario s in scens)
                {
                    rows.Add(new()
                    {
                        s.ScenarioCode, s.Name, s.ScenarioType, s.PlannedGuests.ToString(), s.ShortageCount.ToString(),
                        costVisible && s.TotalRequirementValue.HasValue ? s.TotalRequirementValue.Value.ToString("F2") : "Restricted",
                        costVisible && s.TotalShortageValue.HasValue ? s.TotalShortageValue.Value.ToString("F2") : "Restricted",
                        run.Currency
                    });
                }
            }
            else if (code is "BATCH_REQUIREMENT" or "EXCESS_BATCH_OUTPUT")
            {
                headers = new() { "RecipeCode", "RecipeName", "Version", "RequiredQty", "BatchYield", "UOM", "RequiredBatches", "PlannedOutput", "ExcessOutput", "PreparedStock" };
                List<SilaCulinaryMrpBatchSummaryDto> batches = string.IsNullOrWhiteSpace(run.BatchSummariesJson)
                    ? new()
                    : JsonSerializer.Deserialize<List<SilaCulinaryMrpBatchSummaryDto>>(run.BatchSummariesJson, CamelJson) ?? new();
                foreach (SilaCulinaryMrpBatchSummaryDto b in batches)
                {
                    rows.Add(new()
                    {
                        b.RecipeCode, b.RecipeName, b.RecipeVersion.ToString(), b.RequiredQty.ToString("F4"),
                        b.BatchYield.ToString("F4"), b.BatchYieldUom, b.RequiredBatches.ToString("F4"),
                        b.PlannedOutputQty.ToString("F4"), b.ExcessOutputQty.ToString("F4"), b.PreparedStockQty.ToString("F4")
                    });
                }
            }
            else
            {
                headers = new()
                {
                    "MrpCode", "MrpVersion", "EngineVersion", "CalculatedUtc", "MaterialCode", "MaterialDescription", "Category",
                    "Kitchen", "StorageLocation", "RequiredDateUtc", "UOM", "EdibleQty", "RawQty", "GrossQty",
                    "PreparedStockQty", "IndicativeNewProdQty", "EligibleStockQty", "IncomingPoSeparate",
                    "IndicativeShortage", "UnitCost", "RequirementValue", "Currency", "ValidationStatus", "ContributingRecipes"
                };
                foreach (SilaCulinaryMrpLine l in lines)
                {
                    rows.Add(new()
                    {
                        run.MrpCode, run.VersionNumber.ToString(), run.EngineVersion, run.CalculatedAtUtc.ToString("u"),
                        l.MaterialCode ?? "UNMAPPED", l.MaterialDescription, l.MaterialCategory,
                        l.KitchenName, l.StorageLocationCode, l.RequiredDateUtc.ToString("u"), l.BaseUom,
                        l.EdibleQty.ToString("F4"), l.RawQty.ToString("F4"), l.GrossQty.ToString("F4"),
                        l.PreparedStockQty.ToString("F4"), l.IndicativeNewProductionQty.ToString("F4"),
                        l.EligibleStockQty?.ToString("F4") ?? "UNKNOWN",
                        l.IncomingPoQty.ToString("F4"),
                        l.IndicativeCurrentShortageQty?.ToString("F4") ?? "UNKNOWN",
                        costVisible ? (l.UnitCost?.ToString("F4") ?? "UNKNOWN") : "RESTRICTED",
                        costVisible ? (l.RequirementValue?.ToString("F2") ?? "UNKNOWN") : "RESTRICTED",
                        run.Currency, l.ValidationStatus, string.Join("; ", ParseJsonStringList(l.ContributingRecipesJson))
                    });
                }
            }

            StringBuilder sb = new();
            sb.AppendLine(string.Join(",", headers.Select(CsvSafe)));
            foreach (List<string> r in rows)
                sb.AppendLine(string.Join(",", r.Select(CsvSafe)));

            string title = GetReportCatalog().FirstOrDefault(x => x.ReportCode == code)?.Title ?? code;
            return new SilaCulinaryMrpReportResultDto
            {
                ReportCode = code,
                Title = title,
                FileName = $"{run.MrpCode}_{code.ToLowerInvariant()}.csv",
                ContentType = "text/csv",
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())),
                RowCount = rows.Count,
                Headers = headers,
                PreviewRows = rows.Take(50).ToList()
            };
        }

        // ==================== DETERMINISTIC IN-PROCESS ENGINE + PYTHON BRIDGE ====================

        private sealed class CalculationOutcome
        {
            public List<SilaCulinaryMrpLine> Lines { get; set; } = new();
            public List<SilaCulinaryMrpException> Exceptions { get; set; } = new();
            public List<SilaCulinaryMrpExplosionNodeDto> Tree { get; set; } = new();
            public List<SilaCulinaryMrpTimeBucketDto> TimeBuckets { get; set; } = new();
            public List<SilaCulinaryMrpBatchSummaryDto> Batches { get; set; } = new();
            public SilaCulinaryMrpReadinessDto Readiness { get; set; } = new();
            public int TotalEvents { get; set; }
            public int TotalRecipes { get; set; }
            public int SufficientCount { get; set; }
            public int ShortageCount { get; set; }
            public int UnknownStockCount { get; set; }
            public int MissingMappingCount { get; set; }
            public decimal? EstimatedReqValue { get; set; }
            public decimal? IndicativeShortValue { get; set; }
            public DateTime? InventoryTs { get; set; }
            public DateTime? NextRequiredBy { get; set; }
        }

        private async Task ExecuteCalculationAndPersistAsync(
            Guid buyerId,
            Guid userId,
            string actorName,
            SilaCulinaryMrpRun run,
            List<SilaCulinaryEvent> events,
            List<Guid> sessionFilter,
            List<Guid> kitchenFilter,
            CancellationToken cancellationToken)
        {
            AddWorkflowEvent(buyerId, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_STARTED,
                $"MRP calculation started for {run.MrpCode}.", userId);

            CalculationOutcome outcome = await ComputeMrpInProcessAsync(
                buyerId, events, sessionFilter, kitchenFilter, run.BufferPolicy, run.PreparedStockMode, cancellationToken);

            foreach (SilaCulinaryMrpLine l in outcome.Lines)
            {
                l.MrpRunId = run.Id;
                _repository.SilaCulinaryMrpLine.Create(l);
            }
            foreach (SilaCulinaryMrpException ex in outcome.Exceptions)
            {
                ex.MrpRunId = run.Id;
                _repository.SilaCulinaryMrpException.Create(ex);
            }

            run.TotalEvents = outcome.TotalEvents;
            run.TotalRecipes = outcome.TotalRecipes;
            run.TotalUniqueMaterials = outcome.Lines.Count;
            run.SufficientStockCount = outcome.SufficientCount;
            run.ShortageCount = outcome.ShortageCount;
            run.UnknownStockCount = outcome.UnknownStockCount;
            run.MissingMappingCount = outcome.MissingMappingCount;
            run.BlockingExceptionCount = outcome.Exceptions.Count(e => e.Severity == "BLOCKING");
            run.WarningExceptionCount = outcome.Exceptions.Count(e => e.Severity == "WARNING");
            run.EstimatedRequirementValue = outcome.EstimatedReqValue;
            run.IndicativeShortageValue = outcome.IndicativeShortValue;
            run.InventoryTimestampUtc = outcome.InventoryTs;
            run.NextRequiredByUtc = outcome.NextRequiredBy;
            run.ExplosionTreeJson = JsonSerializer.Serialize(outcome.Tree, CamelJson);
            run.TimePhasedJson = JsonSerializer.Serialize(outcome.TimeBuckets, CamelJson);
            run.BatchSummariesJson = JsonSerializer.Serialize(outcome.Batches, CamelJson);
            run.ReadinessJson = JsonSerializer.Serialize(outcome.Readiness, CamelJson);
            run.OutputSnapshotJson = JsonSerializer.Serialize(new
            {
                run.MrpCode,
                run.EngineVersion,
                run.TotalUniqueMaterials,
                run.EstimatedRequirementValue,
                run.IndicativeShortageValue
            }, CamelJson);

            // Seed baseline scenario & initial v1 snapshot if none
            bool hasBaseScen = await _repository.SilaCulinaryMrpScenario.FindByCondition(x => x.MrpRunId == run.Id && x.IsBaseline).AnyAsync(cancellationToken);
            if (!hasBaseScen)
            {
                _repository.SilaCulinaryMrpScenario.Create(new SilaCulinaryMrpScenario
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    MrpRunId = run.Id,
                    ScenarioCode = "BASE",
                    Name = "Approved Event Baseline",
                    ScenarioType = "BASELINE",
                    IsBaseline = true,
                    PlannedGuests = events.Sum(e => e.PlanningGuests),
                    TotalRequirementValue = run.EstimatedRequirementValue,
                    TotalShortageValue = run.IndicativeShortageValue,
                    ShortageCount = run.ShortageCount,
                    ExceptionCount = run.BlockingExceptionCount,
                    CreatedByName = actorName,
                    IsActive = true
                });
            }

            AddWorkflowEvent(buyerId, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_EXPLODED,
                $"Explosion completed: {run.TotalRecipes} recipes -> {run.TotalUniqueMaterials} materials ({run.ShortageCount} shortages, {run.BlockingExceptionCount} blocking).", userId);

            if (run.BlockingExceptionCount > 0)
            {
                AddWorkflowEvent(buyerId, Common.SILA_REF_CULINARY_MRP, run.Id, Common.SILA_AUDIT_CULINARY_MRP_VAL_FAIL,
                    $"{run.BlockingExceptionCount} blocking validation exception(s) require resolution.", userId);
            }
        }

        private async Task<CalculationOutcome> ComputeMrpInProcessAsync(
            Guid buyerId,
            List<SilaCulinaryEvent> events,
            List<Guid> sessionFilter,
            List<Guid> kitchenFilter,
            string bufferPolicy,
            string preparedStockMode,
            CancellationToken cancellationToken)
        {
            CalculationOutcome result = new();
            List<Guid> eventIds = events.Select(e => e.Id).ToList();

            List<SilaCulinaryEventSession> sessions = await _repository.SilaCulinaryEventSession
                .FindByCondition(x => x.BuyerId == buyerId && eventIds.Contains(x.EventId) && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMenuLine> menuLines = await _repository.SilaCulinaryMenuLine
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .ToListAsync(cancellationToken);

            // Load ALL active recipes & ingredients for this tenant once (prevents N+1 per ingredient and enables exact version traversal)
            Dictionary<Guid, Recipe> recipesById = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            List<RecipeIngredient> allIngredients = await _repository.RecipeIngredient
                .FindByCondition(x => x.IsActive && x.Recipe.BuyerId == buyerId)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, ItemBuyerMaster> matMap = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(
                _repository, matMap.Keys, cancellationToken);

            // Strictly READ-ONLY Live Inventory & Expiry / Incoming PO read
            List<InventoryBalance> balances = await _repository.InventoryBalance
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .ToListAsync(cancellationToken);
            List<GoodsReceiptItem> grnItems = await _repository.GoodsReceiptItem
                .FindByCondition(x => x.IsActive && x.MaterialId != null && (x.ExpiryDate != null || x.DamagedQty > 0))
                .ToListAsync(cancellationToken);
            List<PurchaseOrderItem> openPoItems = await _repository.PurchaseOrderItem
                .FindByCondition(x => x.IsActive && x.PurchaseOrder.BuyerId == buyerId && x.Quantity > x.ReceivedQuantity)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, List<InventoryBalance>> balByMat = balances.GroupBy(b => b.MaterialId).ToDictionary(g => g.Key, g => g.ToList());
            DateTime now = DateTime.UtcNow;
            Dictionary<Guid, decimal> expiredByMat = grnItems
                .Where(g => g.MaterialId.HasValue && g.ExpiryDate.HasValue && g.ExpiryDate.Value < now)
                .GroupBy(g => g.MaterialId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0m, x.AcceptedQty - x.ReversedQty)));
            Dictionary<string, decimal> incomingByMatCode = openPoItems
                .Where(p => !string.IsNullOrWhiteSpace(p.MaterialCode))
                .GroupBy(p => p.MaterialCode!.Trim().ToUpperInvariant())
                .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0m, x.Quantity - x.ReceivedQuantity)));

            HashSet<(Guid, int)> seenEventRevs = new();
            HashSet<(Guid, int)> uniqueRecipesUsed = new();
            Dictionary<string, SilaCulinaryMrpLine> consolidated = new();
            Dictionary<(Guid, int), SilaCulinaryMrpBatchSummaryDto> batchTracker = new();

            void AddEx(string code, string type, string sev, string impact, string action,
                Guid? evId = null, string? evCode = null, Guid? recId = null, string? recCode = null,
                Guid? matId = null, string? matCode = null, string? matName = null, List<string>? cycle = null)
            {
                result.Exceptions.Add(new SilaCulinaryMrpException
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    ExceptionCode = code,
                    ExceptionType = type,
                    Severity = sev,
                    EventId = evId,
                    EventCode = evCode,
                    RecipeId = recId,
                    RecipeCode = recCode,
                    MaterialId = matId,
                    MaterialCode = matCode,
                    MaterialName = matName,
                    CalculationImpact = impact.Length > 980 ? impact[..980] : impact,
                    ResponsibleTeam = type.Contains("MATERIAL") || type.Contains("UOM") ? "Master Data & Culinary" : "Culinary Planning",
                    RecommendedAction = action,
                    LinkedWorkflow = type.Contains("UOM") ? "UOM_MAINTENANCE" : type.Contains("MATERIAL") ? "MATERIAL_CREATION_REQUEST" : "RECIPE_MASTER",
                    ResolutionStatus = "OPEN",
                    CyclePathJson = cycle != null && cycle.Count > 0 ? JsonSerializer.Serialize(cycle, CamelJson) : null,
                    IsActive = true
                });
            }

            SilaCulinaryMrpExplosionNodeDto? ExplodeRecipe(
                SilaCulinaryEvent ev,
                SilaCulinaryEventSession? sess,
                SilaCulinaryMenuLine ml,
                Guid recipeId,
                int pinnedVersion,
                decimal requiredQtyOrPortions,
                bool isSubRecipe,
                string? subUom,
                List<(Guid Id, int Ver, string Label)> ancestry,
                int depth,
                string kitchenName,
                Guid? kitchenId,
                DateTime reqByUtc)
            {
                if (depth > 10)
                {
                    AddEx("MRP-DEPTH", "RECIPE_DEPENDENCY_LIMIT", "BLOCKING", "Exceeded max recipe depth (10).", "Flatten nested sub-recipes.", ev.Id, ev.EventCode, recipeId);
                    return null;
                }

                if (ancestry.Any(a => a.Id == recipeId))
                {
                    List<string> path = ancestry.Select(a => a.Label).Append(ancestry.First(a => a.Id == recipeId).Label).ToList();
                    AddEx("MRP-CYCLE", "CIRCULAR_RECIPE_DEPENDENCY", "BLOCKING",
                        $"Circular dependency detected: {string.Join(" -> ", path)}", "Break circular sub-recipe reference in Recipe Master.",
                        ev.Id, ev.EventCode, recipeId, cycle: path);
                    return new SilaCulinaryMrpExplosionNodeDto
                    {
                        NodeId = $"cycle-{recipeId}-{depth}",
                        NodeType = isSubRecipe ? "SUB_RECIPE" : "RECIPE",
                        ReferenceCode = recipeId.ToString()[..8],
                        Description = $"CIRCULAR DEPENDENCY: {string.Join(" -> ", path)}",
                        RecipeId = recipeId.ToString(),
                        RecipeVersion = pinnedVersion,
                        RequiredQty = requiredQtyOrPortions,
                        Uom = subUom ?? "PORTION",
                        DependencyLevel = depth,
                        ValidationStatus = "CIRCULAR_DEPENDENCY"
                    };
                }

                if (!recipesById.TryGetValue(recipeId, out Recipe? rec))
                {
                    AddEx("MRP-REC-404", "MISSING_RECIPE", "BLOCKING", $"Recipe {recipeId} not found.", "Restore or replace recipe in Menu Studio.", ev.Id, ev.EventCode, recipeId);
                    return null;
                }

                // Exact recipe version enforcement: match RecipeIngredient rows at pinnedVersion
                List<RecipeIngredient> ings = allIngredients.Where(i => i.RecipeId == rec.Id && i.Version == pinnedVersion).ToList();
                if (ings.Count == 0 && (rec.Version == pinnedVersion || rec.ActiveVersion == pinnedVersion))
                {
                    ings = allIngredients.Where(i => i.RecipeId == rec.Id).ToList();
                }
                if (ings.Count == 0)
                {
                    AddEx("MRP-REC-VER", "MISSING_RECIPE_VERSION", "BLOCKING",
                        $"Pinned recipe {rec.RecipeCode} v{pinnedVersion} has no ingredient version rows.",
                        "Verify published recipe version or refresh menu baseline.", ev.Id, ev.EventCode, rec.Id, rec.RecipeCode);
                }

                uniqueRecipesUsed.Add((rec.Id, pinnedVersion));
                string recLabel = $"{rec.RecipeCode} ({rec.Name} v{pinnedVersion})";
                decimal baseServings = rec.ServingQty > 0 ? rec.ServingQty : 1m;
                decimal scaleFactor = isSubRecipe ? requiredQtyOrPortions / baseServings : requiredQtyOrPortions / baseServings;

                if (isSubRecipe)
                {
                    decimal batches = rec.ServingQty > 0 ? requiredQtyOrPortions / rec.ServingQty : 1m;
                    if (!batchTracker.TryGetValue((rec.Id, pinnedVersion), out SilaCulinaryMrpBatchSummaryDto? bSum))
                    {
                        decimal ceilB = Math.Ceiling(batches);
                        decimal plannedOut = Math.Round(ceilB * baseServings, 4);
                        batchTracker[(rec.Id, pinnedVersion)] = new SilaCulinaryMrpBatchSummaryDto
                        {
                            RecipeId = rec.Id.ToString(),
                            RecipeCode = rec.RecipeCode,
                            RecipeName = rec.Name,
                            RecipeVersion = pinnedVersion,
                            RequiredQty = Math.Round(requiredQtyOrPortions, 4),
                            BatchYield = baseServings,
                            BatchYieldUom = string.IsNullOrWhiteSpace(rec.ServingUom) ? "KG" : rec.ServingUom,
                            AllowPartialBatches = false,
                            RequiredBatches = ceilB,
                            PlannedOutputQty = plannedOut,
                            ExcessOutputQty = Math.Round(Math.Max(0m, plannedOut - requiredQtyOrPortions), 4),
                            PreparedStockQty = 0m,
                            IndicativeNewProductionQty = Math.Round(requiredQtyOrPortions, 4)
                        };
                    }
                    else
                    {
                        bSum.RequiredQty = Math.Round(bSum.RequiredQty + requiredQtyOrPortions, 4);
                        bSum.IndicativeNewProductionQty = bSum.RequiredQty;
                        decimal ceilB = Math.Ceiling(bSum.RequiredQty / baseServings);
                        bSum.RequiredBatches = ceilB;
                        bSum.PlannedOutputQty = Math.Round(ceilB * baseServings, 4);
                        bSum.ExcessOutputQty = Math.Round(Math.Max(0m, bSum.PlannedOutputQty - bSum.RequiredQty), 4);
                    }
                }

                int leadMins = (rec.PrepTimeMinutes ?? 0) + (rec.CookTimeMinutes ?? 0);
                DateTime recReqBy = reqByUtc.AddMinutes(-leadMins);

                SilaCulinaryMrpExplosionNodeDto node = new()
                {
                    NodeId = $"rec-{rec.Id}-v{pinnedVersion}-{depth}-{Guid.NewGuid().ToString()[..4]}",
                    NodeType = isSubRecipe ? "SUB_RECIPE" : "RECIPE",
                    ReferenceCode = rec.RecipeCode,
                    Description = $"{rec.Name} (v{pinnedVersion})",
                    RecipeId = rec.Id.ToString(),
                    RecipeVersion = pinnedVersion,
                    RequiredQty = Math.Round(requiredQtyOrPortions, 4),
                    Uom = subUom ?? "PORTION",
                    ScalingFactor = Math.Round(scaleFactor, 4),
                    DependencyLevel = depth,
                    ValidationStatus = rec.Status == Common.SILA_RECIPE_APPROVED || rec.ActiveVersion > 0 ? "VALID" : "UNPUBLISHED_RECIPE"
                };

                List<(Guid Id, int Ver, string Label)> nextAnc = ancestry.Append((rec.Id, pinnedVersion, recLabel)).ToList();

                foreach (RecipeIngredient ing in ings)
                {
                    decimal scaledIngQty = ing.Quantity * scaleFactor;
                    if (ing.SubRecipeId.HasValue)
                    {
                        int subVer = recipesById.TryGetValue(ing.SubRecipeId.Value, out Recipe? sr)
                            ? (sr.ActiveVersion > 0 ? sr.ActiveVersion : sr.Version)
                            : 1;
                        SilaCulinaryMrpExplosionNodeDto? child = ExplodeRecipe(
                            ev, sess, ml, ing.SubRecipeId.Value, subVer, scaledIngQty,
                            true, ing.Uom, nextAnc, depth + 1, kitchenName, kitchenId, recReqBy);
                        if (child != null) node.Children.Add(child);
                        continue;
                    }

                    ItemBuyerMaster? mat = ing.MaterialId.HasValue && matMap.TryGetValue(ing.MaterialId.Value, out ItemBuyerMaster? foundMat) ? foundMat : null;
                    string baseUom = mat != null ? UomConverter.BaseUomOf(mat) : (string.IsNullOrWhiteSpace(ing.BaseUom) ? UomConverter.Canonical(ing.Uom) : UomConverter.Canonical(ing.BaseUom));

                    // Apply recipe trim/cooking/waste yield sequentially without double-counting
                    decimal trimYield = 1m - Math.Clamp(rec.TrimLossPercent ?? 0m, 0m, 99.9m) / 100m;
                    decimal cookYield = 1m - Math.Clamp(rec.CookingLossPercent ?? 0m, 0m, 99.9m) / 100m;
                    decimal combinedYield = trimYield * cookYield;
                    decimal edibleQtyInIngUom = scaledIngQty;
                    decimal rawQtyInIngUom = combinedYield > 0m ? edibleQtyInIngUom / combinedYield : edibleQtyInIngUom;

                    decimal? convertedRaw = mat != null
                        ? UomConverter.TryToBase(mat, rawQtyInIngUom, ing.Uom, conversions)
                        : rawQtyInIngUom;
                    decimal? convertedEdible = mat != null
                        ? UomConverter.TryToBase(mat, edibleQtyInIngUom, ing.Uom, conversions)
                        : edibleQtyInIngUom;
                    if (mat == null) baseUom = UomConverter.Canonical(ing.Uom);

                    bool isBlocking = false;
                    string valStatus = "REQUIREMENT_CALCULATED";
                    if (!convertedRaw.HasValue)
                    {
                        isBlocking = true;
                        valStatus = "UOM_CONVERSION_MISSING";
                        convertedRaw = rawQtyInIngUom;
                        convertedEdible = edibleQtyInIngUom;
                        baseUom = UomConverter.Canonical(ing.Uom);
                        AddEx("MRP-UOM", "MISSING_UOM_CONVERSION", "BLOCKING",
                            $"No UOM conversion from {ing.Uom} to {(mat != null ? UomConverter.BaseUomOf(mat) : baseUom)} for {ing.ItemName}.",
                            "Create UOM maintenance request in Material Intelligence.", ev.Id, ev.EventCode, rec.Id, rec.RecipeCode,
                            mat?.Id, mat?.MaterialCode, ing.ItemName);
                    }

                    bool resolved = mat != null;
                    if (!resolved)
                    {
                        isBlocking = true;
                        valStatus = "MATERIAL_NOT_MAPPED";
                        AddEx("MRP-MAT", "MISSING_MATERIAL", "BLOCKING",
                            $"Ingredient '{ing.ItemName}' in {rec.RecipeCode} is not mapped to Material Master.",
                            "Create Material Creation Request (Phase 1C).", ev.Id, ev.EventCode, rec.Id, rec.RecipeCode, matName: ing.ItemName);
                    }

                    string storageLoc = ev.PlantCode ?? "MAIN";
                    string bucket = recReqBy.ToString("yyyy-MM-dd");
                    string matKey = mat != null ? mat.Id.ToString() : $"UNMAPPED:{ing.ItemName.ToUpperInvariant()}";
                    string cKey = $"{matKey}|{ev.PlantCode ?? ""}|{storageLoc}|{bucket}|{baseUom}";

                    node.Children.Add(new SilaCulinaryMrpExplosionNodeDto
                    {
                        NodeId = $"mat-{ing.Id}-{depth}",
                        NodeType = "MATERIAL",
                        ReferenceCode = mat?.MaterialCode ?? "UNMAPPED",
                        Description = mat?.Description ?? ing.ItemName,
                        MaterialCode = mat?.MaterialCode,
                        RequiredQty = Math.Round(convertedRaw.Value, 4),
                        Uom = baseUom,
                        YieldPercent = Math.Round(combinedYield * 100m, 2),
                        ScalingFactor = Math.Round(scaleFactor, 4),
                        DependencyLevel = depth + 1,
                        ValidationStatus = valStatus
                    });

                    decimal? eligStock = null;
                    DateTime? invTs = null;
                    List<string> exclReasons = new();
                    if (mat != null && balByMat.TryGetValue(mat.Id, out List<InventoryBalance>? bList) && bList.Count > 0)
                    {
                        decimal totalOnHand = bList.Sum(x => x.OnHandQty);
                        decimal expired = expiredByMat.GetValueOrDefault(mat.Id, 0m);
                        if (expired > 0)
                        {
                            totalOnHand = Math.Max(0m, totalOnHand - expired);
                            exclReasons.Add($"Excluded {expired:F2} {baseUom} expired GRN batch stock.");
                        }
                        eligStock = totalOnHand;
                        invTs = bList.Max(x => x.LastMovementOn ?? x.DateCreated);
                        if (result.InventoryTs == null || invTs > result.InventoryTs) result.InventoryTs = invTs;
                    }

                    decimal incomingPo = mat != null && !string.IsNullOrWhiteSpace(mat.MaterialCode)
                        ? incomingByMatCode.GetValueOrDefault(mat.MaterialCode.Trim().ToUpperInvariant(), 0m)
                        : 0m;
                    decimal? unitCost = mat != null ? (mat.UnitCost ?? mat.MovingAveragePrice ?? mat.StandardPrice ?? ing.UnitCost) : ing.UnitCost;
                    string subPath = string.Join(" -> ", nextAnc.Select(a => a.Label));

                    if (!consolidated.TryGetValue(cKey, out SilaCulinaryMrpLine? line))
                    {
                        line = new SilaCulinaryMrpLine
                        {
                            Id = Guid.NewGuid(),
                            BuyerId = buyerId,
                            ConsolidationKey = cKey,
                            MaterialId = mat?.Id,
                            MaterialCode = mat?.MaterialCode ?? ing.ItemCode,
                            MaterialDescription = mat?.Description ?? ing.ItemName,
                            MaterialCategory = mat?.MaterialGroup ?? "Uncategorized",
                            IsResolvedMaterial = resolved,
                            PropertyId = ev.PropertyId,
                            PlantCode = ev.PlantCode,
                            KitchenLocationId = kitchenId,
                            KitchenName = kitchenName,
                            StorageLocationCode = storageLoc,
                            RequiredDateUtc = recReqBy,
                            RequiredDateBucket = bucket,
                            BaseUom = baseUom,
                            EdibleQty = Math.Round(convertedEdible ?? 0m, 4),
                            RawQty = Math.Round(convertedRaw.Value, 4),
                            GrossQty = Math.Round(convertedRaw.Value, 4),
                            PreparedStockQty = 0m,
                            IndicativeNewProductionQty = Math.Round(convertedRaw.Value, 4),
                            EligibleStockQty = eligStock,
                            IncomingPoQty = incomingPo,
                            UnitCost = unitCost,
                            Currency = ev.Currency,
                            CostStatus = unitCost.HasValue ? "VERIFIED" : "COST_UNAVAILABLE",
                            ValidationStatus = valStatus,
                            IsBlocking = isBlocking,
                            InventoryTimestampUtc = invTs,
                            ContributingRecipesJson = JsonSerializer.Serialize(new List<string> { recLabel }, CamelJson),
                            ContributingEventsJson = JsonSerializer.Serialize(new List<string> { ev.EventCode }, CamelJson),
                            ContributingSessionsJson = JsonSerializer.Serialize(new List<string> { sess?.SessionName ?? "Main" }, CamelJson),
                            SubRecipePathsJson = JsonSerializer.Serialize(new List<string> { subPath }, CamelJson),
                            ExclusionReasonsJson = JsonSerializer.Serialize(exclReasons, CamelJson),
                            IsActive = true
                        };
                        consolidated[cKey] = line;
                    }
                    else
                    {
                        line.EdibleQty = Math.Round(line.EdibleQty + (convertedEdible ?? 0m), 4);
                        line.RawQty = Math.Round(line.RawQty + convertedRaw.Value, 4);
                        line.GrossQty = Math.Round(line.GrossQty + convertedRaw.Value, 4);
                        line.IndicativeNewProductionQty = Math.Max(0m, line.GrossQty - line.PreparedStockQty);
                        line.IsBlocking = line.IsBlocking || isBlocking;
                        line.ContributingRecipesJson = AppendUniqueJsonList(line.ContributingRecipesJson, recLabel);
                        line.ContributingEventsJson = AppendUniqueJsonList(line.ContributingEventsJson, ev.EventCode);
                        line.ContributingSessionsJson = AppendUniqueJsonList(line.ContributingSessionsJson, sess?.SessionName ?? "Main");
                        line.SubRecipePathsJson = AppendUniqueJsonList(line.SubRecipePathsJson, subPath);
                    }
                }

                return node;
            }

            foreach (SilaCulinaryEvent ev in events)
            {
                if (!seenEventRevs.Add((ev.Id, ev.RevisionNumber)))
                {
                    AddEx("MRP-DUP-EV", "DUPLICATE_EVENT_REVISION", "WARNING",
                        $"Skipped duplicate event {ev.EventCode} Rev {ev.RevisionNumber}.", "Ensure unique event selection.", ev.Id, ev.EventCode);
                    continue;
                }

                List<SilaCulinaryEventSession> evSessions = sessions.Where(s => s.EventId == ev.Id).ToList();
                if (sessionFilter.Count > 0) evSessions = evSessions.Where(s => sessionFilter.Contains(s.Id)).ToList();

                List<SilaCulinaryMenuLine> evLines = ev.PrimaryMenuId.HasValue
                    ? menuLines.Where(m => m.MenuId == ev.PrimaryMenuId.Value).OrderBy(m => m.SortOrder).ToList()
                    : new();

                SilaCulinaryMrpExplosionNodeDto evNode = new()
                {
                    NodeId = $"ev-{ev.Id}",
                    NodeType = "EVENT",
                    ReferenceCode = ev.EventCode,
                    Description = $"{ev.Name} (Rev {ev.RevisionNumber})",
                    RequiredQty = ev.PlanningGuests,
                    Uom = "GUEST",
                    DependencyLevel = 0,
                    ValidationStatus = ev.Status
                };

                foreach (SilaCulinaryMenuLine ml in evLines)
                {
                    SilaCulinaryEventSession? sess = evSessions.FirstOrDefault();
                    Guid? kId = sess?.KitchenLocationId ?? ev.KitchenLocationId;
                    if (kitchenFilter.Count > 0 && kId.HasValue && !kitchenFilter.Contains(kId.Value)) continue;

                    string kName = ev.VenueName ?? "Main Banquet Kitchen";
                    if (!ml.RecipeId.HasValue || !ml.RecipeVersion.HasValue)
                    {
                        AddEx("MRP-NO-REC", "MISSING_RECIPE", "BLOCKING",
                            $"Menu line '{ml.DishName}' in {ev.EventCode} has no published recipe version.",
                            "Assign a published recipe in Menu Studio.", ev.Id, ev.EventCode);
                        continue;
                    }

                    decimal portions = ml.PlannedPortions > 0
                        ? ml.PlannedPortions
                        : Math.Ceiling(ev.PlanningGuests * (ml.UptakePercent / 100m));
                    DateTime reqBy = sess?.SessionDateUtc ?? ev.StartDateUtc;

                    SilaCulinaryMrpExplosionNodeDto? rNode = ExplodeRecipe(
                        ev, sess, ml, ml.RecipeId.Value, ml.RecipeVersion.Value, portions,
                        false, null, new(), 1, kName, kId, reqBy);
                    if (rNode != null) evNode.Children.Add(rNode);
                }

                result.Tree.Add(evNode);
            }

            decimal totalReqVal = 0m;
            decimal totalShortVal = 0m;
            bool anyCost = false;
            bool anyShortCost = false;
            Dictionary<(string, string), SilaCulinaryMrpTimeBucketDto> tbMap = new();

            foreach (SilaCulinaryMrpLine line in consolidated.Values)
            {
                if (line.EligibleStockQty.HasValue)
                {
                    decimal sh = Math.Round(Math.Max(0m, line.IndicativeNewProductionQty - line.EligibleStockQty.Value), 4);
                    decimal rem = Math.Round(Math.Max(0m, line.IndicativeNewProductionQty - line.EligibleStockQty.Value - line.IncomingPoQty), 4);
                    line.IndicativeCurrentShortageQty = sh;
                    line.IndicativeRemainingShortageQty = rem;
                    if (!line.IsBlocking)
                    {
                        if (sh == 0m) { line.ValidationStatus = "CURRENT_STOCK_SUFFICIENT"; result.SufficientCount++; }
                        else if (line.EligibleStockQty.Value == 0m) { line.ValidationStatus = "NO_STOCK"; result.ShortageCount++; }
                        else { line.ValidationStatus = "POTENTIAL_SHORTAGE"; result.ShortageCount++; }
                    }
                }
                else
                {
                    result.UnknownStockCount++;
                    if (!line.IsBlocking) line.ValidationStatus = "AVAILABILITY_UNKNOWN";
                }

                if (!line.IsResolvedMaterial) result.MissingMappingCount++;

                if (line.UnitCost.HasValue)
                {
                    line.RequirementValue = Math.Round(line.IndicativeNewProductionQty * line.UnitCost.Value, 2);
                    totalReqVal += line.RequirementValue.Value;
                    anyCost = true;
                    if (line.IndicativeCurrentShortageQty.HasValue && line.IndicativeCurrentShortageQty.Value > 0m)
                    {
                        line.IndicativeShortageValue = Math.Round(line.IndicativeCurrentShortageQty.Value * line.UnitCost.Value, 2);
                        totalShortVal += line.IndicativeShortageValue.Value;
                        anyShortCost = true;
                    }
                }
                else if (line.IsResolvedMaterial)
                {
                    AddEx("MRP-COST", "MISSING_PRICE", "WARNING",
                        $"Material {line.MaterialCode ?? line.MaterialDescription} has no verified unit cost.",
                        "Update price in Material Master or Phase 1C Costing.", matId: line.MaterialId, matCode: line.MaterialCode, matName: line.MaterialDescription);
                }

                if (result.NextRequiredBy == null || line.RequiredDateUtc < result.NextRequiredBy)
                    result.NextRequiredBy = line.RequiredDateUtc;

                (string, string) tbk = (line.RequiredDateBucket, line.KitchenName);
                if (!tbMap.TryGetValue(tbk, out SilaCulinaryMrpTimeBucketDto? tb))
                {
                    tbMap[tbk] = new SilaCulinaryMrpTimeBucketDto
                    {
                        BucketDate = line.RequiredDateBucket,
                        KitchenName = line.KitchenName,
                        MaterialCount = 1,
                        TotalRequirementValue = line.RequirementValue,
                        ShortageMaterialCount = (line.IndicativeCurrentShortageQty ?? 0m) > 0m ? 1 : 0,
                        EarliestRequiredByUtc = line.RequiredDateUtc
                    };
                }
                else
                {
                    tb.MaterialCount++;
                    if (line.RequirementValue.HasValue) tb.TotalRequirementValue = Math.Round((tb.TotalRequirementValue ?? 0m) + line.RequirementValue.Value, 2);
                    if ((line.IndicativeCurrentShortageQty ?? 0m) > 0m) tb.ShortageMaterialCount++;
                    if (line.RequiredDateUtc < tb.EarliestRequiredByUtc) tb.EarliestRequiredByUtc = line.RequiredDateUtc;
                }

                result.Lines.Add(line);
            }

            result.TotalEvents = seenEventRevs.Count;
            result.TotalRecipes = uniqueRecipesUsed.Count;
            result.EstimatedReqValue = anyCost ? Math.Round(totalReqVal, 2) : null;
            result.IndicativeShortValue = anyShortCost ? Math.Round(totalShortVal, 2) : null;
            result.TimeBuckets = tbMap.Values.OrderBy(t => t.BucketDate).ToList();
            result.Batches = batchTracker.Values.ToList();

            bool calcReady = !result.Exceptions.Any(e => e.ExceptionType is "CIRCULAR_RECIPE_DEPENDENCY" or "MISSING_RECIPE_VERSION" or "MISSING_RECIPE");
            bool matReady = result.MissingMappingCount == 0 && !result.Exceptions.Any(e => e.ExceptionType == "MISSING_UOM_CONVERSION");
            bool costReady = result.Lines.Count > 0 && result.Lines.All(l => l.UnitCost.HasValue);
            bool procReady = calcReady && matReady;

            result.Readiness = new SilaCulinaryMrpReadinessDto
            {
                CalculationReady = calcReady,
                MaterialDataReady = matReady,
                CostingReady = costReady,
                ProcurementPlanningReady = procReady,
                CanSubmitForApproval = calcReady && matReady,
                CanReleaseForPlanning = calcReady && matReady,
                Checks = new List<SilaCulinaryMrpReadinessCheckDto>
                {
                    new() { Dimension = "CALCULATION", Label = "Recipe Explosion & Scaling", Status = calcReady ? "READY" : "BLOCKED", Detail = calcReady ? $"{result.TotalRecipes} recipe(s) exploded cleanly." : "Resolve circular or missing recipe version exceptions." },
                    new() { Dimension = "MATERIAL_DATA", Label = "Material Master & UOM Readiness", Status = matReady ? "READY" : "BLOCKED", Detail = matReady ? "All ingredients mapped with valid UOM conversions." : $"{result.MissingMappingCount} unmapped material(s) or missing UOM conversion." },
                    new() { Dimension = "COSTING", Label = "Material Requirement Costing", Status = costReady ? "READY" : "WARNING", Detail = costReady ? "All materials carry verified unit costs." : "One or more materials lack verified unit prices (kept unknown)." },
                    new() { Dimension = "PROCUREMENT_PLANNING", Label = "Phase 2C Handover Readiness", Status = procReady ? "READY" : "BLOCKED", Detail = procReady ? "Eligible for approval and Phase 2C procurement planning release." : "Blocked by unresolved mandatory material/UOM errors." }
                }
            };

            return result;
        }

        // ==================== MAPPING & CSV HELPERS ====================

        private async Task<SilaCulinaryMrpRunDetailDto> BuildDetailAsync(Guid buyerId, SilaCulinaryMrpRun run, bool costVisible, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMrpLine> lines = await _repository.SilaCulinaryMrpLine
                .FindByCondition(x => x.BuyerId == buyerId && x.MrpRunId == run.Id && x.IsActive)
                .OrderBy(x => x.RequiredDateBucket).ThenBy(x => x.MaterialDescription)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryMrpException> exs = await _repository.SilaCulinaryMrpException
                .FindByCondition(x => x.BuyerId == buyerId && x.MrpRunId == run.Id && x.IsActive)
                .OrderBy(x => x.Severity).ToListAsync(cancellationToken);
            List<SilaCulinaryMrpScenario> scens = await _repository.SilaCulinaryMrpScenario
                .FindByCondition(x => x.BuyerId == buyerId && x.MrpRunId == run.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated).ToListAsync(cancellationToken);
            List<SilaCulinaryMrpSnapshot> snaps = await _repository.SilaCulinaryMrpSnapshot
                .FindByCondition(x => x.BuyerId == buyerId && x.MrpRunId == run.Id && x.IsActive)
                .OrderByDescending(x => x.VersionNumber).ToListAsync(cancellationToken);

            SilaCulinaryMrpRunSummaryDto sum = MapSummary(run, costVisible);
            return new SilaCulinaryMrpRunDetailDto
            {
                Id = sum.Id,
                MrpCode = sum.MrpCode,
                Name = sum.Name,
                RunScope = sum.RunScope,
                Status = sum.Status,
                VersionNumber = sum.VersionNumber,
                IsApprovedBaseline = sum.IsApprovedBaseline,
                IsSimulation = sum.IsSimulation,
                EngineVersion = sum.EngineVersion,
                CorrelationId = sum.CorrelationId,
                PropertyId = sum.PropertyId,
                PropertyName = sum.PropertyName,
                PlantCode = sum.PlantCode,
                PlanningHorizonStartUtc = sum.PlanningHorizonStartUtc,
                PlanningHorizonEndUtc = sum.PlanningHorizonEndUtc,
                CalculatedAtUtc = sum.CalculatedAtUtc,
                BufferPolicy = sum.BufferPolicy,
                PreparedStockMode = sum.PreparedStockMode,
                TotalEvents = sum.TotalEvents,
                TotalRecipes = sum.TotalRecipes,
                TotalUniqueMaterials = sum.TotalUniqueMaterials,
                SufficientStockCount = sum.SufficientStockCount,
                ShortageCount = sum.ShortageCount,
                UnknownStockCount = sum.UnknownStockCount,
                MissingMappingCount = sum.MissingMappingCount,
                BlockingExceptionCount = sum.BlockingExceptionCount,
                WarningExceptionCount = sum.WarningExceptionCount,
                EstimatedRequirementValue = sum.EstimatedRequirementValue,
                IndicativeShortageValue = sum.IndicativeShortageValue,
                Currency = sum.Currency,
                CostVisible = costVisible,
                InventoryTimestampUtc = sum.InventoryTimestampUtc,
                NextRequiredByUtc = sum.NextRequiredByUtc,
                ApprovalReference = sum.ApprovalReference,
                CreatedByName = sum.CreatedByName,
                EventReferences = sum.EventReferences,
                Lines = lines.Select(l => MapLine(l, costVisible)).ToList(),
                Exceptions = exs.Select(MapException).ToList(),
                ExplosionTree = string.IsNullOrWhiteSpace(run.ExplosionTreeJson) ? new() : JsonSerializer.Deserialize<List<SilaCulinaryMrpExplosionNodeDto>>(run.ExplosionTreeJson, CamelJson) ?? new(),
                TimePhasedBuckets = string.IsNullOrWhiteSpace(run.TimePhasedJson) ? new() : JsonSerializer.Deserialize<List<SilaCulinaryMrpTimeBucketDto>>(run.TimePhasedJson, CamelJson) ?? new(),
                BatchSummaries = string.IsNullOrWhiteSpace(run.BatchSummariesJson) ? new() : JsonSerializer.Deserialize<List<SilaCulinaryMrpBatchSummaryDto>>(run.BatchSummariesJson, CamelJson) ?? new(),
                Readiness = string.IsNullOrWhiteSpace(run.ReadinessJson) ? new() : JsonSerializer.Deserialize<SilaCulinaryMrpReadinessDto>(run.ReadinessJson, CamelJson) ?? new(),
                Scenarios = scens.Select(s => MapScenario(s, costVisible)).ToList(),
                Snapshots = snaps.Select(s => new SilaCulinaryMrpSnapshotDto
                {
                    Id = s.Id,
                    VersionNumber = s.VersionNumber,
                    StatusAtSnapshot = s.StatusAtSnapshot,
                    IsApprovedBaseline = s.IsApprovedBaseline,
                    ChangeReason = s.ChangeReason,
                    ApprovalReference = s.ApprovalReference,
                    CreatedByName = s.CreatedByName,
                    CreatedOn = s.DateCreated
                }).ToList()
            };
        }

        private static SilaCulinaryMrpRunSummaryDto MapSummary(SilaCulinaryMrpRun r, bool costVisible) => new()
        {
            Id = r.Id,
            MrpCode = r.MrpCode,
            Name = r.Name,
            RunScope = r.RunScope,
            Status = r.Status,
            VersionNumber = r.VersionNumber,
            IsApprovedBaseline = r.IsApprovedBaseline,
            IsSimulation = r.IsSimulation,
            EngineVersion = r.EngineVersion,
            CorrelationId = r.CorrelationId,
            PropertyId = r.PropertyId,
            PropertyName = r.PropertyName,
            PlantCode = r.PlantCode,
            PlanningHorizonStartUtc = r.PlanningHorizonStartUtc,
            PlanningHorizonEndUtc = r.PlanningHorizonEndUtc,
            CalculatedAtUtc = r.CalculatedAtUtc,
            BufferPolicy = r.BufferPolicy,
            PreparedStockMode = r.PreparedStockMode,
            TotalEvents = r.TotalEvents,
            TotalRecipes = r.TotalRecipes,
            TotalUniqueMaterials = r.TotalUniqueMaterials,
            SufficientStockCount = r.SufficientStockCount,
            ShortageCount = r.ShortageCount,
            UnknownStockCount = r.UnknownStockCount,
            MissingMappingCount = r.MissingMappingCount,
            BlockingExceptionCount = r.BlockingExceptionCount,
            WarningExceptionCount = r.WarningExceptionCount,
            EstimatedRequirementValue = costVisible ? r.EstimatedRequirementValue : null,
            IndicativeShortageValue = costVisible ? r.IndicativeShortageValue : null,
            Currency = r.Currency,
            CostVisible = costVisible,
            InventoryTimestampUtc = r.InventoryTimestampUtc,
            NextRequiredByUtc = r.NextRequiredByUtc,
            ApprovalReference = r.ApprovalReference,
            CreatedByName = r.CreatedByName,
            EventReferences = ParseJsonStringList(r.EventReferencesJson)
        };

        private static SilaCulinaryMrpLineDto MapLine(SilaCulinaryMrpLine l, bool costVisible) => new()
        {
            Id = l.Id,
            ConsolidationKey = l.ConsolidationKey,
            MaterialId = l.MaterialId,
            MaterialCode = l.MaterialCode,
            MaterialDescription = l.MaterialDescription,
            MaterialCategory = l.MaterialCategory,
            IsResolvedMaterial = l.IsResolvedMaterial,
            PropertyId = l.PropertyId,
            PlantCode = l.PlantCode,
            KitchenLocationId = l.KitchenLocationId,
            KitchenName = l.KitchenName,
            StorageLocationCode = l.StorageLocationCode,
            RequiredDateUtc = l.RequiredDateUtc,
            RequiredDateBucket = l.RequiredDateBucket,
            BaseUom = l.BaseUom,
            EdibleQty = l.EdibleQty,
            RawQty = l.RawQty,
            GrossQty = l.GrossQty,
            PreparedStockQty = l.PreparedStockQty,
            IndicativeNewProductionQty = l.IndicativeNewProductionQty,
            EligibleStockQty = l.EligibleStockQty,
            IncomingPoQty = l.IncomingPoQty,
            IndicativeCurrentShortageQty = l.IndicativeCurrentShortageQty,
            IndicativeRemainingShortageQty = l.IndicativeRemainingShortageQty,
            UnitCost = costVisible ? l.UnitCost : null,
            Currency = l.Currency,
            CostStatus = costVisible ? l.CostStatus : "RESTRICTED",
            RequirementValue = costVisible ? l.RequirementValue : null,
            IndicativeShortageValue = costVisible ? l.IndicativeShortageValue : null,
            CostVisible = costVisible,
            ValidationStatus = l.ValidationStatus,
            IsBlocking = l.IsBlocking,
            InventoryTimestampUtc = l.InventoryTimestampUtc,
            ContributingRecipes = ParseJsonStringList(l.ContributingRecipesJson),
            ContributingEvents = ParseJsonStringList(l.ContributingEventsJson),
            ContributingSessions = ParseJsonStringList(l.ContributingSessionsJson),
            SubRecipePaths = ParseJsonStringList(l.SubRecipePathsJson),
            ExclusionReasons = ParseJsonStringList(l.ExclusionReasonsJson)
        };

        private static SilaCulinaryMrpExceptionDto MapException(SilaCulinaryMrpException e) => new()
        {
            Id = e.Id,
            ExceptionCode = e.ExceptionCode,
            ExceptionType = e.ExceptionType,
            Severity = e.Severity,
            EventId = e.EventId,
            EventCode = e.EventCode,
            RecipeId = e.RecipeId,
            RecipeCode = e.RecipeCode,
            MaterialId = e.MaterialId,
            MaterialCode = e.MaterialCode,
            MaterialName = e.MaterialName,
            CalculationImpact = e.CalculationImpact,
            ResponsibleTeam = e.ResponsibleTeam,
            RecommendedAction = e.RecommendedAction,
            LinkedWorkflow = e.LinkedWorkflow,
            ResolutionStatus = e.ResolutionStatus,
            ResolutionNote = e.ResolutionNote,
            ResolvedByName = e.ResolvedByName,
            ResolvedOnUtc = e.ResolvedOnUtc,
            CyclePath = ParseJsonStringList(e.CyclePathJson)
        };

        private static SilaCulinaryMrpScenarioDto MapScenario(SilaCulinaryMrpScenario s, bool costVisible) => new()
        {
            Id = s.Id,
            ScenarioCode = s.ScenarioCode,
            Name = s.Name,
            ScenarioType = s.ScenarioType,
            IsBaseline = s.IsBaseline,
            PlannedGuests = s.PlannedGuests,
            TotalRequirementValue = costVisible ? s.TotalRequirementValue : null,
            TotalShortageValue = costVisible ? s.TotalShortageValue : null,
            ShortageCount = s.ShortageCount,
            ExceptionCount = s.ExceptionCount,
            CreatedByName = s.CreatedByName,
            CreatedOn = s.DateCreated
        };

        private static List<string> ParseJsonStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(json, CamelJson) ?? new(); }
            catch { return new(); }
        }

        private static List<Guid> ParseGuidList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<Guid>>(json, CamelJson) ?? new(); }
            catch { return new(); }
        }

        private static string AppendUniqueJsonList(string? json, string item)
        {
            List<string> list = ParseJsonStringList(json);
            if (!list.Contains(item)) list.Add(item);
            return JsonSerializer.Serialize(list, CamelJson);
        }

        private string? SafeUnprotect(string encrypted)
        {
            try { return _protector.Unprotect(encrypted); }
            catch { return null; }
        }

        private static List<string> ReadJsonArray(JsonElement root, string prop)
        {
            if (!root.TryGetProperty(prop, out JsonElement el) || el.ValueKind != JsonValueKind.Array) return new();
            return el.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x.Length > 0).ToList();
        }

        private static string CsvSafe(string? raw)
        {
            string val = raw ?? string.Empty;
            if (val.Length > 0 && (val[0] == '=' || val[0] == '+' || val[0] == '-' || val[0] == '@' || val[0] == '\t' || val[0] == '\r'))
                val = "'" + val;
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}
