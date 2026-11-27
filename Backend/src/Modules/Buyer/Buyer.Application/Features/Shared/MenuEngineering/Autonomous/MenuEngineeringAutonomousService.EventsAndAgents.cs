using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Buyer.Application.Features.Shared.MenuEngineering.Ai;
using Buyer.Application.Features.Shared.MenuEngineering.Twin;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared.MenuEngineering.Autonomous
{
    public partial class MenuEngineeringAutonomousService : IMenuEngineeringAutonomousService
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private readonly IRepositoryWrapper _repository;
        private readonly RepositoryContext _db;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IMenuEngineeringDigitalTwinService? _twin;
        private readonly IMenuEngineeringAiOrchestratorService? _aiOrchestrator;
        private readonly IHttpClientFactory? _httpClientFactory;

        public MenuEngineeringAutonomousService(
            IRepositoryWrapper repository,
            RepositoryContext db,
            ILoggerManager logger,
            IHttpContextAccessor? httpContextAccessor = null,
            IMenuEngineeringDigitalTwinService? twin = null,
            IMenuEngineeringAiOrchestratorService? aiOrchestrator = null,
            IHttpClientFactory? httpClientFactory = null)
        {
            _repository = repository;
            _db = db;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _twin = twin;
            _aiOrchestrator = aiOrchestrator;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<(BuyerBusinessProfile Buyer, MenuEngineeringPermissionSetDto Perms, MenuEngineeringSettings Settings)> ResolveContextAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringSettings? settings = await _db.MenuEngineeringSettings
                .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId || x.PropertyId == null), ct);
            if (settings is null)
            {
                settings = new MenuEngineeringSettings
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = propertyId,
                    MenuEngineeringEnabled = true,
                    MeStudioEnabled = true,
                    IsActive = true,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow
                };
                _db.MenuEngineeringSettings.Add(settings);
                await _db.SaveChangesAsync(ct);
            }

            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(
                _httpContextAccessor,
                roleId,
                settings.MenuEngineeringEnabled,
                settings.MeStudioEnabled);
            return (buyer, perms, settings);
        }

        private static string ComputeSha256(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        public async Task<BusinessEventDto> PublishBusinessEventAsync(
            Guid organizationId, Guid userId, Guid roleId, PublishBusinessEventRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering permission required.");

            string eventType = string.IsNullOrWhiteSpace(request.EventType) ? "IngredientPriceChanged" : request.EventType.Trim();
            string idemp = !string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? request.IdempotencyKey.Trim()
                : $"{eventType}:{request.ObjectId?.ToString() ?? "GLOBAL"}:{(request.EffectiveAtUtc ?? DateTime.UtcNow):yyyyMMddHHmmss}";

            MenuEngineeringBusinessEvent? existing = await _db.MenuEngineeringBusinessEvent
                .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == idemp && x.IsActive, ct);
            if (existing is not null)
            {
                return MapEventDto(existing);
            }

            string correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
                ? $"corr-{Guid.NewGuid():N}"[..20]
                : request.CorrelationId.Trim();

            MenuEngineeringBusinessEvent ev = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                EventType = eventType,
                SourceSystem = string.IsNullOrWhiteSpace(request.SourceSystem) ? "SILA_ME" : request.SourceSystem.Trim(),
                ObjectType = request.ObjectType,
                ObjectId = request.ObjectId,
                ObjectLabel = request.ObjectLabel,
                EffectiveAtUtc = request.EffectiveAtUtc ?? DateTime.UtcNow,
                CorrelationId = correlationId,
                SchemaVersion = "1.0",
                IdempotencyKey = idemp,
                PayloadJson = JsonSerializer.Serialize(request.Payload ?? new(), JsonOpts),
                MaterialityScore = request.MaterialityScore,
                ProcessingStatus = "PENDING",
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            };

            _db.MenuEngineeringBusinessEvent.Add(ev);
            await _db.SaveChangesAsync(ct);
            return MapEventDto(ev);
        }

        public async Task<List<BusinessEventDto>> GetBusinessEventsAsync(
            Guid organizationId, Guid roleId, string? status = null, CancellationToken ct = default)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering permission required.");

            IQueryable<MenuEngineeringBusinessEvent> q = _db.MenuEngineeringBusinessEvent.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => x.ProcessingStatus == status.Trim().ToUpperInvariant());

            List<MenuEngineeringBusinessEvent> rows = await q.OrderByDescending(x => x.EffectiveAtUtc).Take(100).ToListAsync(ct);
            return rows.Select(MapEventDto).ToList();
        }

        public async Task<BusinessEventDto> ReplayEventAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid eventId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await ResolveContextAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.UseAiMenuIntelligence || perms.ManageMenuSetup, "Access denied", "Permission required to replay events.");

            MenuEngineeringBusinessEvent ev = await _db.MenuEngineeringBusinessEvent
                .FirstOrDefaultAsync(x => x.Id == eventId && x.BuyerId == buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Event not found", $"Business event {eventId} was not found for this tenant.");

            ev.ProcessingStatus = "PENDING";
            ev.DeadLetterReason = null;
            ev.NextRetryAtUtc = null;
            await _db.SaveChangesAsync(ct);
            await ProcessPendingEventsAsync(organizationId, userId, roleId, ev.PropertyId, ct);
            return MapEventDto(ev);
        }

        public async Task<EventProcessingSummaryDto> ProcessPendingEventsAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? propertyId = null, CancellationToken ct = default)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, MenuEngineeringSettings settings) =
                await ResolveContextAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering permission required.");

            EventProcessingSummaryDto summary = new();
            if (!settings.MenuEngineeringEnabled || settings.AutonomousModePaused)
            {
                return summary;
            }

            DateTime now = DateTime.UtcNow;
            List<MenuEngineeringBusinessEvent> pending = await _db.MenuEngineeringBusinessEvent
                .Where(x => x.BuyerId == buyer.Id && x.IsActive &&
                            (x.ProcessingStatus == "PENDING" || (x.ProcessingStatus == "FAILED" && (x.NextRetryAtUtc == null || x.NextRetryAtUtc <= now))) &&
                            (propertyId == null || x.PropertyId == propertyId || x.PropertyId == null))
                .OrderBy(x => x.EffectiveAtUtc)
                .Take(25)
                .ToListAsync(ct);

            summary.PendingScanned = pending.Count;
            List<MenuEngineeringAgentAutonomyPolicy> policies = await _db.MenuEngineeringAgentAutonomyPolicy
                .AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(ct);

            foreach (MenuEngineeringBusinessEvent ev in pending)
            {
                // Out-of-order guard: if a newer processed event exists for the exact same object & event type, mark skipped.
                if (ev.ObjectId.HasValue)
                {
                    bool newerProcessed = await _db.MenuEngineeringBusinessEvent.AsNoTracking().AnyAsync(
                        x => x.BuyerId == buyer.Id && x.IsActive && x.Id != ev.Id &&
                             x.EventType == ev.EventType && x.ObjectId == ev.ObjectId &&
                             x.ProcessingStatus == "PROCESSED" && x.EffectiveAtUtc > ev.EffectiveAtUtc, ct);
                    if (newerProcessed)
                    {
                        ev.ProcessingStatus = "SKIPPED";
                        ev.ProcessedAtUtc = now;
                        ev.DeadLetterReason = "Out-of-order stale event superseded by newer effective timestamp.";
                        summary.SkippedOrCooldownCount++;
                        summary.Events.Add(MapEventDto(ev));
                        continue;
                    }
                }

                try
                {
                    ev.ProcessingStatus = "PROCESSING";
                    string[] routedAgents = MenuEngineeringAutonomousContracts.EventAgentRoutingMatrix.TryGetValue(ev.EventType, out string[]? list)
                        ? list
                        : new[] { "ME_STUDIO_ORCHESTRATOR" };

                    List<string> activeAgents = new();
                    foreach (string agentId in routedAgents)
                    {
                        MenuEngineeringAgentAutonomyPolicy? pol = policies.FirstOrDefault(p => p.AgentId == agentId && p.PropertyId == ev.PropertyId)
                            ?? policies.FirstOrDefault(p => p.AgentId == agentId && p.PropertyId == null);

                        int level = pol?.AutonomyLevel ?? 2;
                        bool paused = pol?.IsPaused ?? false;
                        decimal materialityGate = pol?.MaterialityThresholdPercent ?? 5m;
                        if (paused || level == 0 || ev.MaterialityScore < materialityGate) continue;

                        // Cooldown / anti-loop check
                        int cooldownMins = pol?.CooldownMinutes ?? 60;
                        DateTime cutoff = now.AddMinutes(-cooldownMins);
                        bool inCooldown = await _db.MenuEngineeringBusinessEvent.AsNoTracking().AnyAsync(
                            x => x.BuyerId == buyer.Id && x.IsActive && x.Id != ev.Id &&
                                 x.EventType == ev.EventType && x.ObjectId == ev.ObjectId &&
                                 x.ProcessingStatus == "PROCESSED" && x.ProcessedAtUtc >= cutoff, ct);
                        if (!inCooldown) activeAgents.Add(agentId);
                    }

                    if (activeAgents.Count == 0)
                    {
                        ev.ProcessingStatus = "SKIPPED";
                        ev.ProcessedAtUtc = now;
                        ev.ProcessedAgentsJson = "[]";
                        summary.SkippedOrCooldownCount++;
                        summary.Events.Add(MapEventDto(ev));
                        continue;
                    }

                    int plansCreated = await ExecuteRoutedAgentsAndConsolidatePlanAsync(
                        organizationId, userId, roleId, buyer.Id, settings, policies, ev, activeAgents, ct);
                    summary.ActionPlansCreated += plansCreated;

                    ev.ProcessedAgentsJson = JsonSerializer.Serialize(activeAgents, JsonOpts);
                    ev.ProcessingStatus = "PROCESSED";
                    ev.ProcessedAtUtc = now;
                    summary.ProcessedCount++;
                    summary.Events.Add(MapEventDto(ev));
                }
                catch (Exception ex)
                {
                    ev.RetryCount++;
                    if (ev.RetryCount >= 3)
                    {
                        ev.ProcessingStatus = "DEAD_LETTER";
                        ev.DeadLetterReason = SilaLogText.Short(ex.Message);
                        summary.DeadLetteredCount++;
                    }
                    else
                    {
                        ev.ProcessingStatus = "FAILED";
                        ev.NextRetryAtUtc = now.AddMinutes(Math.Pow(2, ev.RetryCount));
                        ev.DeadLetterReason = SilaLogText.Short(ex.Message);
                        summary.FailedOrRetriedCount++;
                    }
                    summary.Events.Add(MapEventDto(ev));
                }
            }

            await _db.SaveChangesAsync(ct);
            return summary;
        }

        private async Task<int> ExecuteRoutedAgentsAndConsolidatePlanAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid buyerId,
            MenuEngineeringSettings settings,
            List<MenuEngineeringAgentAutonomyPolicy> policies,
            MenuEngineeringBusinessEvent ev,
            List<string> activeAgents,
            CancellationToken ct)
        {
            Dictionary<string, JsonElement> payload = new();
            try
            {
                Dictionary<string, JsonElement>? parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(ev.PayloadJson, JsonOpts);
                if (parsed is not null) payload = parsed;
            }
            catch { }

            string dedupKey = $"PLAN:{ev.EventType}:{ev.ObjectId?.ToString() ?? "GLOBAL"}:{ev.EffectiveAtUtc:yyyyMMdd}";
            MenuEngineeringActionPlan? existingPlan = await _db.MenuEngineeringActionPlan
                .FirstOrDefaultAsync(x => x.BuyerId == buyerId && x.DeduplicationKey == dedupKey && x.IsActive, ct);
            if (existingPlan is not null)
            {
                return 0;
            }

            string title = $"Autonomous Action Plan — {ev.EventType}";
            string rootCause = $"Triggered by {ev.EventType} from {ev.SourceSystem} (materiality {ev.MaterialityScore:F1}%).";
            List<string> affectedObjects = new();
            List<string> evidence = new() { $"Event {ev.EventType} (Correlation: {ev.CorrelationId})" };
            decimal expectedFinancialImpact = 0m;
            string operationalImpact = "Multi-agent impact evaluated against Digital Twin baseline.";
            List<(string AgentId, string ToolId, string ActionType, string Title, decimal Impact, string? TargetType, Guid? TargetId, string PayloadJson)> steps = new();

            if (string.Equals(ev.EventType, "IngredientPriceChanged", StringComparison.OrdinalIgnoreCase))
            {
                decimal pct = ReadDecimal(payload, "priceChangePercent", 15m);
                string ingQuery = ReadString(payload, "ingredientName", ev.ObjectLabel ?? "Salmon");
                if (_twin is not null)
                {
                    TwinDependencyAnalysisResultDto cascade = await _twin.AnalyzeDependencyGraphAsync(
                        organizationId, userId, roleId,
                        new TwinDependencyAnalysisRequestDto { PropertyId = ev.PropertyId, ShockMaterialId = ev.ObjectId, ShockIngredientName = ingQuery, PriceChangePercent = pct }, ct);
                    affectedObjects.AddRange(cascade.AffectedRecipes.Take(10));
                    affectedObjects.AddRange(cascade.AffectedMenuItems.Take(10));
                    affectedObjects.AddRange(cascade.AffectedMenus.Take(10));
                    affectedObjects.AddRange(cascade.AffectedEvents.Take(10));
                    expectedFinancialImpact = cascade.TotalMarginImpact;
                    evidence.Add($"Affected recipes: {cascade.AffectedRecipes.Count}, menu items: {cascade.AffectedMenuItems.Count}, menus: {cascade.AffectedMenus.Count}, cost delta: {cascade.TotalCostImpact:F2}");
                }
                title = $"Price Shock Response: {ingQuery} ({pct:+0.##;-0.##}%)";
                operationalImpact = "Review impacted recipe costs, approve substitution or menu price revision.";
                steps.Add(("RECIPE_INTELLIGENCE_AGENT", "EvaluateIngredientPriceShock", "PREPARE_RECOMMENDATION", $"Review {ingQuery} cost cascade across affected recipes & events", Math.Abs(expectedFinancialImpact), "Ingredient", ev.ObjectId, ev.PayloadJson));
                steps.Add(("COST_MARGIN_AGENT", "GeneratePricingRecommendations", "PREPARE_MENU_REVISION", $"Prepare draft menu price / portion revision for {ingQuery} impact", Math.Abs(expectedFinancialImpact), "MenuPlan", null, ev.PayloadJson));
            }
            else if (string.Equals(ev.EventType, "OccupancyForecastChanged", StringComparison.OrdinalIgnoreCase))
            {
                decimal occPct = ReadDecimal(payload, "occupancyPercent", 88m);
                decimal coversDelta = ReadDecimal(payload, "coversDelta", 60m);
                title = $"Occupancy Shift Plan ({occPct:F0}% occupancy, {coversDelta:+0;-0} covers)";
                expectedFinancialImpact = Math.Round(coversDelta * 45m, 2);
                evidence.Add($"OPERA PMS occupancy update: {occPct:F1}%, covers delta {coversDelta:+0;-0}");
                operationalImpact = "Recalculate breakfast/outlet covers, portions, ingredient demand, and kitchen capacity.";
                steps.Add(("DEMAND_FORECAST_AGENT", "CalculatePortionsAndDemand", "REVIEW_DEMAND", "Review revised outlet & breakfast demand forecast", 0m, "PmsDemandSnapshot", ev.ObjectId, ev.PayloadJson));
                steps.Add(("PORTION_OPTIMIZATION_AGENT", "CalculatePortionsAndDemand", "PROPOSE_PORTION_ADJUSTMENT", "Approve revised planned portions & buffet buffers", Math.Abs(expectedFinancialImpact) * 0.25m, "MenuPlan", null, ev.PayloadJson));
                steps.Add(("PURCHASE_INTELLIGENCE_AGENT", "IdentifyProcurementShortages", "PREPARE_PURCHASE_PROPOSAL", "Approve net ingredient purchase proposal", Math.Abs(expectedFinancialImpact) * 0.35m, "PurchaseProposal", null, ev.PayloadJson));
                steps.Add(("PRODUCTION_INTELLIGENCE_AGENT", "IdentifyProductionConstraints", "PREPARE_PRODUCTION_REVISION", "Approve revised kitchen batch production schedule", 0m, "ProductionPlan", null, ev.PayloadJson));
            }
            else if (string.Equals(ev.EventType, "SupplierDeliveryDelayed", StringComparison.OrdinalIgnoreCase))
            {
                int delayDays = (int)ReadDecimal(payload, "delayDays", 2m);
                string itemName = ReadString(payload, "ingredientName", ev.ObjectLabel ?? "Critical Ingredient");
                title = $"Supplier Delivery Delay Mitigation: {itemName} (+{delayDays}d)";
                expectedFinancialImpact = -450m * delayDays;
                evidence.Add($"Delay of {delayDays} days detected on {itemName}; time-phased shortage window evaluated.");
                operationalImpact = "Prevent stockout via stock transfer, approved substitution, or PO expediting.";
                steps.Add(("INVENTORY_INTELLIGENCE_AGENT", "RetrieveInventoryAvailability", "APPROVE_STOCK_TRANSFER_DRAFT", $"Prepare inter-outlet stock transfer draft for {itemName}", 450m, "InventoryTransfer", ev.ObjectId, ev.PayloadJson));
                steps.Add(("PURCHASE_INTELLIGENCE_AGENT", "IdentifyProcurementShortages", "PREPARE_PURCHASE_PROPOSAL", $"Prepare alternate supplier / expedited PO proposal for {itemName}", 900m, "PurchaseOrder", ev.ObjectId, ev.PayloadJson));
            }
            else if (string.Equals(ev.EventType, "EventGuestCountChanged", StringComparison.OrdinalIgnoreCase))
            {
                int guestCount = (int)ReadDecimal(payload, "newGuestCount", 350m);
                title = $"Event Guest Count Revision Plan ({guestCount} guests)";
                if (_twin is not null)
                {
                    await _twin.CreateSnapshotAsync(organizationId, userId, roleId, new CreateTwinSnapshotRequestDto
                    {
                        PropertyId = ev.PropertyId,
                        EventId = ev.ObjectId,
                        SnapshotType = "EVENT_SPECIFIC"
                    }, ct);
                }
                expectedFinancialImpact = Math.Round(guestCount * 35m, 2);
                evidence.Add($"Refreshed Event Digital Twin for {guestCount} confirmed guests.");
                operationalImpact = "Consolidated demand, portion, inventory, production, and purchasing adjustments.";
                steps.Add(("EVENT_INTELLIGENCE_AGENT", "CaptureDigitalTwinSnapshot", "RUN_SIMULATION", $"Refresh Event Digital Twin & risk profile ({guestCount} guests)", 0m, "Event", ev.ObjectId, ev.PayloadJson));
                steps.Add(("PORTION_OPTIMIZATION_AGENT", "CalculatePortionsAndDemand", "PROPOSE_PORTION_ADJUSTMENT", "Approve updated event portion & batch plan", 500m, "MenuPlan", null, ev.PayloadJson));
                steps.Add(("PURCHASE_INTELLIGENCE_AGENT", "IdentifyProcurementShortages", "PREPARE_PURCHASE_PROPOSAL", "Approve incremental event purchase proposal", Math.Round(guestCount * 12m, 2), "PurchaseProposal", ev.ObjectId, ev.PayloadJson));
            }
            else if (string.Equals(ev.EventType, "WasteRecorded", StringComparison.OrdinalIgnoreCase))
            {
                decimal wasteQty = ReadDecimal(payload, "wasteQty", 15m);
                string dish = ReadString(payload, "itemName", ev.ObjectLabel ?? "Buffet Item");
                title = $"Waste Reduction Plan: {dish} ({wasteQty:F1} units)";
                expectedFinancialImpact = Math.Round(wasteQty * 14m, 2);
                evidence.Add($"Recurring excess waste ({wasteQty:F1}) recorded on {dish}; smaller-batch prep recommended.");
                operationalImpact = "Reduce overproduction buffer and shift to smaller progressive batches.";
                steps.Add(("WASTE_INTELLIGENCE_AGENT", "PredictFoodWasteCategories", "PROPOSE_PORTION_ADJUSTMENT", $"Identify waste pattern and quantify savings for {dish}", expectedFinancialImpact, "MenuPlanLine", ev.ObjectId, ev.PayloadJson));
                steps.Add(("PORTION_OPTIMIZATION_AGENT", "CalculatePortionsAndDemand", "PROPOSE_PORTION_ADJUSTMENT", $"Reduce prep batch size and buffet buffer for {dish}", 0m, "MenuPlanLine", ev.ObjectId, ev.PayloadJson));
            }
            else
            {
                steps.Add((activeAgents[0], "CaptureDigitalTwinSnapshot", "PREPARE_RECOMMENDATION", $"Evaluate {ev.EventType} across Digital Twin", 0m, ev.ObjectType, ev.ObjectId, ev.PayloadJson));
            }

            // Consolidate into ONE deduplicated recommendation + ONE ActionPlan (prevents overlapping duplicate recommendations/benefits)
            string recDedup = $"REC:{ev.EventType}:{ev.ObjectId?.ToString() ?? "GLOBAL"}:{ev.EffectiveAtUtc:yyyyMMdd}";
            MenuEngineeringRecommendation? existingRec = await _db.MenuEngineeringRecommendation
                .FirstOrDefaultAsync(x => x.BuyerId == buyerId && x.DeduplicationKey == recDedup && x.IsActive, ct);

            MenuEngineeringActionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                PropertyId = ev.PropertyId,
                TriggerEventId = ev.Id,
                CorrelationId = ev.CorrelationId,
                DeduplicationKey = dedupKey,
                Title = title,
                BusinessObjective = $"Mitigate risk and optimize margin following {ev.EventType}.",
                RootCauseAnalysis = rootCause,
                AffectedObjectsJson = JsonSerializer.Serialize(affectedObjects.Distinct().Take(30), JsonOpts),
                SupportingEvidenceJson = JsonSerializer.Serialize(evidence, JsonOpts),
                ExpectedFinancialImpact = expectedFinancialImpact,
                ExpectedOperationalImpact = operationalImpact,
                RiskLevel = steps.Any(s => MenuEngineeringAutonomousContracts.ClassifyRisk(s.ActionType) == "HIGH") ? "HIGH" : "MEDIUM",
                Status = "APPROVAL_PENDING",
                RequiredApprovalsJson = JsonSerializer.Serialize(new[] { "F&B Director", "Cost Controller" }, JsonOpts),
                ResponsibleUserId = userId == Guid.Empty ? null : userId,
                ExecutionDeadlineUtc = DateTime.UtcNow.AddHours(24),
                IsConsolidated = true,
                IsActive = true,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            };
            _db.MenuEngineeringActionPlan.Add(plan);

            if (existingRec is null)
            {
                _db.MenuEngineeringRecommendation.Add(new MenuEngineeringRecommendation
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    RecommendationCode = $"REC-A4-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                    AgentId = activeAgents[0],
                    Category = ev.EventType,
                    PropertyId = ev.PropertyId,
                    TargetObjectType = ev.ObjectType,
                    TargetObjectId = ev.ObjectId,
                    TargetObjectLabel = ev.ObjectLabel ?? title,
                    Title = title,
                    CurrentBaseline = rootCause,
                    ProposedChange = operationalImpact,
                    SupportingEvidence = string.Join(" | ", evidence),
                    EstimatedMarginImpact = expectedFinancialImpact,
                    OperationalImpact = operationalImpact,
                    DataQuality = "HIGH",
                    Confidence = 0.85m,
                    Priority = Math.Abs(expectedFinancialImpact) > 1000m ? "HIGH" : "MEDIUM",
                    RequiresApproval = true,
                    Status = "NEW",
                    LifecycleStatus = "RECOMMENDATION_GENERATED",
                    ActionPlanId = plan.Id,
                    DeduplicationKey = recDedup,
                    CooldownUntilUtc = DateTime.UtcNow.AddMinutes(settings.ProactiveCooldownMinutes),
                    IsActive = true,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow
                });
            }

            Guid? prevId = null;
            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                MenuEngineeringAgentAutonomyPolicy? pol = policies.FirstOrDefault(p => p.AgentId == step.AgentId && p.PropertyId == ev.PropertyId)
                    ?? policies.FirstOrDefault(p => p.AgentId == step.AgentId && p.PropertyId == null);
                int level = pol?.AutonomyLevel ?? 2;
                decimal maxFin = pol?.MaxFinancialThreshold ?? 2500m;
                string risk = MenuEngineeringAutonomousContracts.ClassifyRisk(step.ActionType);
                (string decision, string reason) = MenuEngineeringAutonomousContracts.EvaluatePolicy(
                    level, pol?.IsPaused ?? false, settings.AutonomousModePaused, settings.AutomatedExecutionDisabled,
                    risk, step.Impact, maxFin, true, true, true, 0.85m);

                MenuEngineeringActionItem item = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    ActionPlanId = plan.Id,
                    SequenceOrder = i + 1,
                    DependsOnActionId = prevId,
                    AgentId = step.AgentId,
                    ToolId = step.ToolId,
                    ActionType = step.ActionType,
                    Title = step.Title,
                    RiskClassification = risk,
                    PolicyDecision = decision,
                    PolicyReason = reason,
                    FinancialImpact = step.Impact,
                    TargetObjectType = step.TargetType,
                    TargetObjectId = step.TargetId,
                    TargetObjectVersion = 1,
                    DataFreshnessSnapshotUtc = DateTime.UtcNow,
                    PayloadJson = step.PayloadJson,
                    PayloadHash = ComputeSha256(step.PayloadJson),
                    ApprovalStatus = decision is "ALLOW_OBSERVE" or "ALLOW_RECOMMEND" or "ALLOW_DRAFT" or "ALLOW_POLICY_EXECUTION" ? "NOT_REQUIRED" : "PENDING",
                    IdempotencyKey = $"ACT:{plan.Id:N}:{i + 1}",
                    ExecutionStatus = "PENDING",
                    IsActive = true,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow
                };
                _db.MenuEngineeringActionItem.Add(item);
                prevId = item.Id;
            }

            await _db.SaveChangesAsync(ct);
            return 1;
        }

        private static decimal ReadDecimal(Dictionary<string, JsonElement> dict, string key, decimal fallback)
        {
            if (dict.TryGetValue(key, out JsonElement el))
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out decimal val)) return val;
                if (el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), out decimal parsed)) return parsed;
            }
            return fallback;
        }

        private static string ReadString(Dictionary<string, JsonElement> dict, string key, string fallback)
            => dict.TryGetValue(key, out JsonElement el) && el.ValueKind == JsonValueKind.String ? (el.GetString() ?? fallback) : fallback;

        private static BusinessEventDto MapEventDto(MenuEngineeringBusinessEvent ev)
        {
            List<string> agents = new();
            try { agents = JsonSerializer.Deserialize<List<string>>(ev.ProcessedAgentsJson, JsonOpts) ?? new(); } catch { }
            return new BusinessEventDto
            {
                Id = ev.Id,
                PropertyId = ev.PropertyId,
                EventType = ev.EventType,
                SourceSystem = ev.SourceSystem,
                ObjectType = ev.ObjectType,
                ObjectId = ev.ObjectId,
                ObjectLabel = ev.ObjectLabel,
                EffectiveAtUtc = ev.EffectiveAtUtc,
                CorrelationId = ev.CorrelationId,
                IdempotencyKey = ev.IdempotencyKey,
                MaterialityScore = ev.MaterialityScore,
                ProcessingStatus = ev.ProcessingStatus,
                RetryCount = ev.RetryCount,
                ProcessedAtUtc = ev.ProcessedAtUtc,
                DeadLetterReason = ev.DeadLetterReason,
                RoutedAgents = agents,
                PayloadHighlights = string.IsNullOrWhiteSpace(ev.PayloadJson) ? new() : new() { ev.PayloadJson }
            };
        }
    }
}
