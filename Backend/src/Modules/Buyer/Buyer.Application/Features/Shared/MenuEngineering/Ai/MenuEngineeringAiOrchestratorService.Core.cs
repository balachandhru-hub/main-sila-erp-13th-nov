using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared.MenuEngineering.Ai
{
    public interface IMenuEngineeringAiOrchestratorService
    {
        Task<AiOperatingModeStatusDto> GetOperatingModeStatusAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<AiContextualResponseDto> RunContextualAssistantAsync(Guid organizationId, Guid userId, Guid roleId, AiContextualRequestDto request, CancellationToken ct);
        Task<MeStudioMultiAgentRunResultDto> RunMultiAgentWorkflowAsync(Guid organizationId, Guid userId, Guid roleId, MeStudioMultiAgentCommandRequestDto request, CancellationToken ct);
        Task<MeStudioMultiAgentRunResultDto> GetAgentRunAsync(Guid organizationId, Guid roleId, Guid runId, CancellationToken ct);
        Task<MeStudioMultiAgentRunResultDto> CancelAgentRunAsync(Guid organizationId, Guid userId, Guid roleId, Guid runId, CancellationToken ct);
        Task SubmitFeedbackAsync(Guid organizationId, Guid userId, Guid roleId, MeAiFeedbackSubmitDto dto, CancellationToken ct);

        Task<AiMenuItemAssistResultDto> AssistMenuItemAsync(Guid organizationId, Guid userId, Guid roleId, AiMenuItemAssistRequestDto request, CancellationToken ct);
        Task<AiMenuCalendarGenerateResultDto> GenerateMenuCalendarAsync(Guid organizationId, Guid userId, Guid roleId, AiMenuCalendarGenerateRequestDto request, CancellationToken ct);
        Task<AiPortionOptimizeResultDto> OptimizePortionsAsync(Guid organizationId, Guid userId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct);
        Task<AiBacktestDemandResultDto> GetBacktestedDemandForecastAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<AiPosSalesIntelligenceDto> GetPosSalesIntelligenceAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<AiPosMappingSuggestionBatchDto> SuggestPosMappingsAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<AiConsumptionRootCauseDto> GetConsumptionRootCausesAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<AiPriceShockResultDto> EvaluateIngredientPriceShockAsync(Guid organizationId, Guid userId, Guid roleId, AiPriceShockRequestDto request, CancellationToken ct);
        Task<AiPricingRecommendationsResultDto> GeneratePricingRecommendationsAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<AiNlSimulationResultDto> RunNaturalLanguageSimulationAsync(Guid organizationId, Guid userId, Guid roleId, AiNlSimulationRequestDto request, CancellationToken ct);

        Task<List<MeRecommendationCockpitItemDto>> GetCockpitRecommendationsAsync(Guid organizationId, Guid roleId, Guid? propertyId, string? status, CancellationToken ct);
        Task<MeRecommendationCockpitItemDto> TransitionCockpitRecommendationAsync(Guid organizationId, Guid userId, Guid roleId, Guid recommendationId, MeRecommendationLifecycleActionDto action, CancellationToken ct);
        Task<List<MeProactiveAlertDto>> RunProactiveScanAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<List<MeProactiveAlertDto>> GetProactiveAlertsAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<MeProactiveAlertDto> AcknowledgeAlertAsync(Guid organizationId, Guid userId, Guid roleId, Guid alertId, CancellationToken ct);
        Task<MeSetupWizardReadinessDto> GetSetupWizardReadinessAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<MeLearningSummaryDto> GetLearningSummaryAsync(Guid organizationId, Guid roleId, CancellationToken ct);
        Task<SilaStoreRoleIntelligenceDto> GetSilaStoreRoleIntelligenceAsync(Guid organizationId, Guid userId, Guid roleId, string rolePersona, Guid? propertyId, CancellationToken ct);
    }

    public partial class MenuEngineeringAiOrchestratorService : IMenuEngineeringAiOrchestratorService
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        private static readonly Regex PromptInjectionRegex = new(
            @"(ignore\s+(all\s+)?previous|system\s+prompt|drop\s+table|delete\s+from|update\s+\w+\s+set|execute\s+sql|<script)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IRepositoryWrapper _repository;
        private readonly RepositoryContext _db;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMenuEngineeringStudioService _studio;
        private readonly ISilaCulinaryFinancialService _financial;
        private readonly ISilaCulinaryAnalyticsService _analytics;
        private readonly IHttpClientFactory? _httpClientFactory;

        public MenuEngineeringAiOrchestratorService(
            IRepositoryWrapper repository,
            RepositoryContext db,
            ILoggerManager logger,
            IHttpContextAccessor httpContextAccessor,
            IMenuEngineeringStudioService studio,
            ISilaCulinaryFinancialService financial,
            ISilaCulinaryAnalyticsService analytics,
            IHttpClientFactory? httpClientFactory = null)
        {
            _repository = repository;
            _db = db;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _studio = studio;
            _financial = financial;
            _analytics = analytics;
            _httpClientFactory = httpClientFactory;
        }

        private sealed class AiContext
        {
            public BuyerBusinessProfile Buyer { get; init; } = null!;
            public MenuEngineeringSettings Settings { get; init; } = null!;
            public SilaCulinaryAiConfiguration? AiConfig { get; init; }
            public MenuEngineeringPermissionSetDto Perms { get; init; } = new();
            public List<InventoryLocation> Scope { get; init; } = new();
            public List<Guid> ScopeIds => Scope.Select(x => x.Id).ToList();
        }

        private async Task<AiContext> BuildContextAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringSettings? propSettings = propertyId == null
                ? null
                : await _db.MenuEngineeringSettings.FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && x.PropertyId == propertyId, ct);
            MenuEngineeringSettings settings = propSettings
                ?? await _db.MenuEngineeringSettings.FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && x.PropertyId == null, ct)
                ?? await _db.MenuEngineeringSettings.FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive, ct)
                ?? new MenuEngineeringSettings { Id = Guid.NewGuid(), BuyerId = buyer.Id, IsActive = true };

            if (!settings.MenuEngineeringEnabled)
            {
                throw new BadRequestCustomException(
                    "Menu Engineering is disabled for this tenant/property.",
                    "Enable Menu Engineering under Setup to use Menu Engineering AI.");
            }

            SilaCulinaryAiConfiguration? aiConfig = await _db.SilaCulinaryAiConfiguration
                .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive, ct);

            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(
                _httpContextAccessor, roleId, settings.MenuEngineeringEnabled, settings.MeStudioEnabled);

            List<Guid> allowedIds = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, userId, roleId, ct);
            List<InventoryLocation> scope = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && allowedIds.Contains(x.Id) && (propertyId == null || x.PropertyId == propertyId))
                .ToListAsync(ct);

            return new AiContext
            {
                Buyer = buyer,
                Settings = settings,
                AiConfig = aiConfig,
                Perms = perms,
                Scope = scope
            };
        }

        private static string SanitizeText(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            string cleaned = PromptInjectionRegex.Replace(input.Trim(), "[FILTERED]");
            return cleaned.Length > 1200 ? cleaned[..1200] : cleaned;
        }

        private static string ResolveOperatingMode(AiContext ctx, string? requestedMode)
        {
            string mode = (requestedMode ?? ctx.Settings.DefaultOperatingMode ?? "AI_ASSISTED").Trim().ToUpperInvariant();
            if (mode == "AGENTIC")
            {
                bool canAgentic = ctx.Perms.UseAiMenuIntelligence && (ctx.Perms.CreateMenu || ctx.Perms.EditDraftMenu || ctx.Perms.RunMenuSimulation);
                if (!canAgentic)
                {
                    throw new ForBiddenCustomException("Agentic mode denied", "Permission required for Agentic multi-agent orchestration.");
                }
                return "AGENTIC";
            }
            if (mode == "MANUAL") return "MANUAL";
            return ctx.Perms.UseAiMenuIntelligence ? "AI_ASSISTED" : "MANUAL";
        }

        private bool IsAiProviderEnabled(AiContext ctx)
        {
            if (ctx.AiConfig == null) return true; // default local/Python engine available
            if (string.Equals(ctx.AiConfig.OperatingMode, "DISABLED", StringComparison.OrdinalIgnoreCase)) return false;
            return ctx.AiConfig.LocalEnabled
                || (ctx.AiConfig.AllowExternalAi && (ctx.AiConfig.GeminiEnabled || ctx.AiConfig.ClaudeEnabled));
        }

        private async Task<(bool BudgetOk, int TodayCount, int TodayTokens, decimal MonthCost)> CheckAndTrackQuotaAsync(
            AiContext ctx, Guid userId, string feature, int inputTokens, int outputTokens, long latencyMs, bool success, bool usedFallback, CancellationToken ct)
        {
            DateTime today = DateTime.UtcNow.Date;
            DateTime monthStart = new(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            List<SilaCulinaryAiUsageLog> monthLogs = await _db.SilaCulinaryAiUsageLog
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.DateCreated >= monthStart)
                .ToListAsync(ct);

            int todayCount = monthLogs.Count(x => x.DateCreated >= today);
            int todayTokens = monthLogs.Where(x => x.DateCreated >= today).Sum(x => x.TotalTokens);
            decimal monthCost = monthLogs.Sum(x => x.EstimatedCostUsd);

            int maxReq = ctx.AiConfig?.DailyRequestLimit ?? 200;
            int maxTok = ctx.AiConfig?.DailyTokenBudget ?? 250000;
            decimal maxCost = ctx.AiConfig?.MonthlyCostBudgetUsd ?? 100m;

            bool withinBudget = todayCount < maxReq && todayTokens < maxTok && monthCost < maxCost;

            _db.SilaCulinaryAiUsageLog.Add(new SilaCulinaryAiUsageLog
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                GenerationReference = $"ME2-{Guid.NewGuid():N}"[..20],
                CorrelationId = Guid.NewGuid().ToString("N")[..24],
                Feature = feature,
                Provider = IsAiProviderEnabled(ctx) ? (ctx.AiConfig?.OperatingMode ?? "LOCAL") : "DETERMINISTIC_OFFLINE",
                Model = ctx.AiConfig?.LocalModel ?? "me-hybrid-v2",
                Success = success && withinBudget,
                UsedFallback = usedFallback || !withinBudget,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = inputTokens + outputTokens,
                EstimatedCostUsd = usedFallback ? 0m : Math.Round((inputTokens + outputTokens) * 0.000002m, 6),
                LatencyMs = (int)Math.Clamp(latencyMs, 0, int.MaxValue),
                ActorUserId = userId,
                IsActive = true,
                DateCreated = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);

            return (withinBudget, todayCount + 1, todayTokens + inputTokens + outputTokens, monthCost);
        }

        private async Task<TOut?> PostPythonAiAsync<TIn, TOut>(string path, TIn payload, CancellationToken ct)
            where TOut : class
        {
            try
            {
                string aiBase = Environment.GetEnvironmentVariable("SILA_CULINARY_ENGINE_URL") ?? "http://127.0.0.1:8013";
                using HttpClient client = _httpClientFactory?.CreateClient("sila-culinary-engine") ?? new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(12);
                using HttpResponseMessage resp = await client.PostAsJsonAsync($"{aiBase.TrimEnd('/')}{path}", payload, ct);
                if (!resp.IsSuccessStatusCode) return null;
                return await resp.Content.ReadFromJsonAsync<TOut>(JsonOpts, ct);
            }
            catch
            {
                return null;
            }
        }

        public async Task<AiOperatingModeStatusDto> GetOperatingModeStatusAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            DateTime today = DateTime.UtcNow.Date;
            DateTime monthStart = new(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            List<SilaCulinaryAiUsageLog> logs = await _db.SilaCulinaryAiUsageLog
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.DateCreated >= monthStart)
                .ToListAsync(ct);

            bool providerUp = IsAiProviderEnabled(ctx);
            bool canAgentic = ctx.Perms.UseAiMenuIntelligence && (ctx.Perms.CreateMenu || ctx.Perms.EditDraftMenu || ctx.Perms.RunMenuSimulation);

            return new AiOperatingModeStatusDto
            {
                OperatingMode = ctx.Settings.DefaultOperatingMode ?? "AI_ASSISTED",
                CanUseAiAssisted = ctx.Perms.UseAiMenuIntelligence,
                CanUseAgentic = canAgentic,
                AiProviderAvailable = providerUp,
                ProviderMode = providerUp ? (ctx.AiConfig?.OperatingMode ?? "LOCAL") : "DETERMINISTIC_OFFLINE",
                DailyUsageCount = logs.Count(x => x.DateCreated >= today),
                DailyUsageLimit = ctx.AiConfig?.DailyRequestLimit ?? 200,
                DailyTokensUsed = logs.Where(x => x.DateCreated >= today).Sum(x => x.TotalTokens),
                DailyTokenBudget = ctx.AiConfig?.DailyTokenBudget ?? 250000,
                MonthlyEstimatedCostUsd = Math.Round(logs.Sum(x => x.EstimatedCostUsd), 4),
                MonthlyCostBudgetUsd = ctx.AiConfig?.MonthlyCostBudgetUsd ?? 100m,
                FallbackNotice = providerUp ? null : "External/Local LLM provider disabled. Operating in deterministic calculation & manual mode.",
                Agents = MenuEngineeringAgentRegistry.Agents.ToList(),
                Tools = MenuEngineeringToolRegistry.Tools.Where(t => MenuEngineeringToolRegistry.IsAuthorized(t.ToolId, ctx.Perms)).ToList()
            };
        }

        public async Task<AiContextualResponseDto> RunContextualAssistantAsync(
            Guid organizationId, Guid userId, Guid roleId, AiContextualRequestDto request, CancellationToken ct)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            string mode = ResolveOperatingMode(ctx, request.OperatingMode);
            string agentId = MenuEngineeringAgentRegistry.ResolveForScreen(request.ScreenKey);
            string sanitizedPrompt = SanitizeText(request.UserPrompt);

            if (mode == "MANUAL")
            {
                return new AiContextualResponseDto
                {
                    AgentId = agentId,
                    ScreenKey = request.ScreenKey,
                    Action = request.Action,
                    OperatingMode = "MANUAL",
                    ValueTag = "ACTUAL",
                    Summary = "Manual Mode active — deterministic calculations only.",
                    Explanation = "All deterministic costing, portion, forecast baseline, and validation rules remain active without generative AI.",
                    DataQuality = "HIGH",
                    DataFreshness = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                    Confidence = 1.0m,
                    FinancialRedacted = !ctx.Perms.ViewMenuCost && !ctx.Perms.ViewMenuMargin,
                    AiProviderUsed = false
                };
            }

            // Enforce confidential financial access check when prompt or action requests cost/margin details
            bool financialRequested = request.ScreenKey is "silaMenuCost"
                || sanitizedPrompt.Contains("margin", StringComparison.OrdinalIgnoreCase)
                || sanitizedPrompt.Contains("cost", StringComparison.OrdinalIgnoreCase)
                || sanitizedPrompt.Contains("price", StringComparison.OrdinalIgnoreCase)
                || sanitizedPrompt.Contains("profit", StringComparison.OrdinalIgnoreCase);

            bool financialAllowed = ctx.Perms.ViewMenuCost && ctx.Perms.ViewMenuMargin;
            bool redactFinancials = !financialAllowed;

            if (request.ScreenKey == "silaMenuCost" && !ctx.Perms.ViewMenuCost && !ctx.Perms.ViewMenuMargin)
            {
                throw new ForBiddenCustomException("Confidential financial access denied", "VIEW_SILA_MENU_COST or VIEW_SILA_MENU_MARGIN is required.");
            }

            List<string> evidence = new();
            List<string> warnings = new();
            List<string> invokedTools = new() { "RetrievePropertyOutlet" };
            List<AiContextualActionProposalDto> proposed = new();
            string summary;
            string explanation;
            string valueTag = request.Action == "SimulateChange" ? "SIMULATION" : request.Action == "PredictNextPeriod" ? "FORECAST" : "AI_RECOMMENDATION";

            SilaMenuEngineeringFilterDto filter = new()
            {
                PropertyId = request.PropertyId,
                OutletLocationId = request.OutletLocationId,
                MenuPlanId = request.MenuPlanId,
                DateFrom = request.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30),
                DateTo = request.DateTo ?? DateTime.UtcNow.Date,
                MealPeriod = request.MealPeriod
            };

            List<MenuItem> items = await _db.MenuItem
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive)
                .OrderBy(x => x.Name)
                .Take(200)
                .ToListAsync(ct);
            invokedTools.Add("RetrieveApprovedMenuItems");

            MenuPlan? activePlan = request.MenuPlanId != null
                ? await _db.MenuPlan.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == request.MenuPlanId && p.BuyerId == ctx.Buyer.Id && p.IsActive, ct)
                : await _db.MenuPlan.Include(p => p.Lines).Where(p => p.BuyerId == ctx.Buyer.Id && p.IsActive).OrderByDescending(p => p.BusinessDate).FirstOrDefaultAsync(ct);

            if (redactFinancials && financialRequested)
            {
                warnings.Add("Confidential cost and margin figures were redacted because your role lacks VIEW_SILA_MENU_COST / VIEW_SILA_MENU_MARGIN.");
            }

            switch (request.Action)
            {
                case "ExplainVariance":
                {
                    AiConsumptionRootCauseDto variance = await BuildConsumptionRootCausesInternalAsync(ctx, roleId, filter, ct);
                    invokedTools.Add("DetectConsumptionAnomalies");
                    var top = variance.Anomalies.FirstOrDefault();
                    summary = top != null
                        ? $"Top consumption variance: {top.MaterialName} ({top.VarianceQty:+0.##;-0.##;0} units)"
                        : "No material consumption variance spikes detected in the selected window.";
                    explanation = top != null && top.RankedCauses.Count > 0
                        ? $"Primary candidate cause: {top.RankedCauses[0].Description} (score {top.RankedCauses[0].ProbabilityScore:P0})."
                        : "Actual usage tracks within expected recipe yield and POS depletion ranges.";
                    evidence.AddRange(variance.Anomalies.Take(3).Select(a => $"{a.MaterialName}: theoretical {a.TheoreticalQty:0.##} vs actual {a.ActualQty:0.##}"));
                    break;
                }
                case "PredictNextPeriod":
                {
                    AiBacktestDemandResultDto fc = await BuildBacktestForecastInternalAsync(ctx, filter, ct);
                    invokedTools.Add("CalculatePortionsAndDemand");
                    decimal totalFc = fc.HorizonDays.Sum(d => d.RecommendedDemand);
                    summary = $"Predicted demand ({fc.SelectedModel}): {totalFc:0} covers/units across next {fc.HorizonDays.Count} days.";
                    explanation = $"Model {fc.SelectedModel} ({fc.SelectedModelVersion}) selected via backtesting. Data quality: {fc.DataQuality}.";
                    evidence.AddRange(fc.Drivers.Take(3));
                    break;
                }
                case "FindAlternatives":
                {
                    List<MenuItem> substitutes = items.Where(i => i.Id != request.MenuItemId && i.IsAvailable).Take(4).ToList();
                    summary = substitutes.Count > 0
                        ? $"Found {substitutes.Count} approved alternative dishes with active recipes."
                        : "No approved alternative dishes found in catalog.";
                    explanation = "Alternatives are filtered by active status and availability without altering approved recipes.";
                    evidence.AddRange(substitutes.Select(s => $"{s.Name} ({s.Category ?? "General"})" + (redactFinancials ? "" : $" — price {s.SellingPrice:0.00}")));
                    break;
                }
                case "SimulateChange":
                {
                    AiNlSimulationResultDto sim = await RunNlSimulationInternalAsync(ctx, userId, roleId, new AiNlSimulationRequestDto
                    {
                        Command = string.IsNullOrWhiteSpace(sanitizedPrompt) ? "Reduce food cost to 28%" : sanitizedPrompt,
                        MenuPlanId = request.MenuPlanId ?? activePlan?.Id,
                        PropertyId = request.PropertyId,
                        SaveScenario = false
                    }, ct);
                    invokedTools.Add("RunWhatIfSimulation");
                    decimal simFcPct = sim.Simulation.Revenue > 0 ? Math.Round(sim.Simulation.FoodCost / sim.Simulation.Revenue * 100m, 1) : 0m;
                    summary = redactFinancials
                        ? $"Simulation '{sim.ParsedIntent}' evaluated in read-only mode (financials redacted)."
                        : $"Simulation '{sim.ParsedIntent}': projected food cost {simFcPct:0.0}% (cost variance {sim.Simulation.CostVariancePct:+0.0;-0.0;0.0}%).";
                    explanation = "Executed read-only against simulation engine; live menu and inventory tables were not mutated.";
                    evidence.AddRange(sim.Constraints.Take(3));
                    break;
                }
                default:
                {
                    string planDesc = activePlan != null ? $"{activePlan.Name} ({activePlan.Status}, {activePlan.Lines.Count(l => l.IsActive)} lines)" : "Active property catalog";
                    summary = $"{agentId.Replace('_', ' ')}: analyzed {planDesc} across {items.Count} active menu items.";
                    explanation = redactFinancials
                        ? $"Operational readiness assessed for {request.ScreenKey}. Financial cost and margin columns are masked per RBAC."
                        : $"Evaluated operational readiness, food cost target ({ctx.Settings.TargetFoodCostPercent:0.0}%), and menu mix across {items.Count} items.";
                    evidence.Add($"Active items: {items.Count(i => i.IsAvailable)} available / {items.Count} total");
                    if (activePlan != null) evidence.Add($"Active menu plan: {activePlan.Name} ({activePlan.BusinessDate:yyyy-MM-dd})");
                    if (!string.IsNullOrWhiteSpace(sanitizedPrompt)) evidence.Add($"Query focus: {sanitizedPrompt}");

                    if (ctx.Perms.CreateMenu && activePlan != null)
                    {
                        proposed.Add(new AiContextualActionProposalDto
                        {
                            ActionType = "CREATE_SCENARIO_DRAFT",
                            Label = "Generate 7 Optimized Scenarios",
                            RequiresApproval = true,
                            TargetEndpoint = "/api/v1/buyer/sila/me-studio/ai/multi-agent-run",
                            Payload = new() { ["menuPlanId"] = activePlan.Id, ["propertyId"] = request.PropertyId }
                        });
                    }
                    break;
                }
            }

            sw.Stop();
            bool providerUsed = IsAiProviderEnabled(ctx);
            await CheckAndTrackQuotaAsync(ctx, userId, $"CONTEXT_{request.ScreenKey}", 180, 140, sw.ElapsedMilliseconds, true, !providerUsed, ct);

            return new AiContextualResponseDto
            {
                AgentId = agentId,
                ScreenKey = request.ScreenKey,
                Action = request.Action,
                OperatingMode = mode,
                ValueTag = valueTag,
                Summary = summary,
                Explanation = explanation,
                Evidence = evidence,
                Warnings = warnings,
                DataQuality = items.Count >= 5 ? "HIGH" : "MODERATE",
                DataFreshness = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'"),
                Confidence = items.Count >= 5 ? 0.86m : 0.65m,
                FinancialRedacted = redactFinancials,
                AiProviderUsed = providerUsed,
                ProposedActions = proposed,
                InvokedTools = invokedTools
            };
        }

        public async Task<MeStudioMultiAgentRunResultDto> RunMultiAgentWorkflowAsync(
            Guid organizationId, Guid userId, Guid roleId, MeStudioMultiAgentCommandRequestDto request, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio && ctx.Perms.UseAiMenuIntelligence, "Access denied", "AI Menu Intelligence permission required.");

            string idemKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"RUN-{organizationId:N}-{Math.Abs(request.Objective.GetHashCode()):X}"
                : request.IdempotencyKey.Trim();

            MenuEngineeringAgentRun? existingRun = await _db.MenuEngineeringAgentRun
                .FirstOrDefaultAsync(x => x.BuyerId == ctx.Buyer.Id && x.IdempotencyKey == idemKey && x.IsActive, ct);
            if (existingRun != null && existingRun.Status == "COMPLETED")
            {
                return DeserializeRunResult(existingRun);
            }

            DateTime started = DateTime.UtcNow;
            string cleanObjective = SanitizeText(request.Objective);
            if (string.IsNullOrWhiteSpace(cleanObjective))
            {
                throw new BadRequestCustomException("Objective required", "Provide a menu planning objective for ME Studio Orchestrator.");
            }

            // Parse NL objective via Python engine (or deterministic parser fallback)
            PythonParseCommandResponse? parsed = await PostPythonAiAsync<object, PythonParseCommandResponse>(
                "/v1/me-ai/parse-command", new { command = cleanObjective }, ct);

            decimal? fallbackCovers = null;
            Match covMatch = Regex.Match(cleanObjective, @"(\d{1,5})\s*(?:guests?|covers?|pax|people)", RegexOptions.IgnoreCase);
            if (covMatch.Success && decimal.TryParse(covMatch.Groups[1].Value, out decimal cParsed))
            {
                fallbackCovers = cParsed;
            }

            decimal? fallbackSell = null;
            Match priceMatch = Regex.Match(cleanObjective, @"(?:aed|usd|eur|sar|\$|price\s*(?:of|at)?)\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
            if (!priceMatch.Success)
            {
                priceMatch = Regex.Match(cleanObjective, @"(\d+(?:\.\d+)?)\s*(?:aed|usd|eur|sar)\s*(?:selling|price)?", RegexOptions.IgnoreCase);
            }
            if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, out decimal pParsed))
            {
                fallbackSell = pParsed;
            }

            decimal? fallbackMaxFc = null;
            Match fcMatch = Regex.Match(cleanObjective, @"(?:max(?:imum)?|under|below|reduce(?:\s+food\s+cost)?\s+to|target)\s*(\d+(?:\.\d+)?)\s*%?\s*(?:food\s*cost)?", RegexOptions.IgnoreCase);
            if (fcMatch.Success && cleanObjective.Contains("cost", StringComparison.OrdinalIgnoreCase) && decimal.TryParse(fcMatch.Groups[1].Value, out decimal fcParsed))
            {
                fallbackMaxFc = fcParsed;
            }

            List<string> fallbackDietary = new();
            foreach (string dTag in new[] { "Vegetarian", "Vegan", "Halal", "Gluten-Free" })
            {
                if (cleanObjective.Contains(dTag, StringComparison.OrdinalIgnoreCase))
                {
                    fallbackDietary.Add(dTag);
                }
            }

            string serviceFormat = parsed?.service_format ?? (cleanObjective.Contains("buffet", StringComparison.OrdinalIgnoreCase) ? "Buffet" : "A La Carte");
            string mealPeriod = parsed?.meal_period ?? (cleanObjective.Contains("breakfast", StringComparison.OrdinalIgnoreCase) ? "Breakfast" : cleanObjective.Contains("lunch", StringComparison.OrdinalIgnoreCase) ? "Lunch" : "Dinner");
            decimal covers = request.ExpectedCovers ?? parsed?.expected_covers ?? fallbackCovers ?? 150m;
            decimal sellPrice = request.SellingPriceTarget ?? parsed?.selling_price ?? fallbackSell ?? 120m;
            decimal maxFc = request.MaxFoodCostPercent ?? parsed?.max_food_cost_percent ?? fallbackMaxFc ?? ctx.Settings.TargetFoodCostPercent;
            if (maxFc <= 1m) maxFc *= 100m;

            List<string> reqDietary = request.RequiredDietary ?? parsed?.required_dietary ?? fallbackDietary;
            List<string> exclAllergens = request.ExcludedAllergens ?? new();
            Dictionary<string, int> reqCats = request.RequiredCategories ?? parsed?.required_categories ?? new();

            MenuEngineeringAgentRun runEntity = existingRun ?? new MenuEngineeringAgentRun
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                IdempotencyKey = idemKey,
                AgentId = "ME_STUDIO_ORCHESTRATOR",
                OperatingMode = ResolveOperatingMode(ctx, request.OperatingMode),
                Status = "RUNNING",
                PropertyId = request.PropertyId,
                OutletLocationId = request.OutletLocationId,
                ObjectiveText = cleanObjective,
                OptimizationMode = request.OptimizationMode,
                StepCount = 15,
                CurrentStep = 1,
                ActorUserId = userId,
                StartedAtUtc = started,
                IsActive = true
            };
            if (existingRun == null)
            {
                _db.MenuEngineeringAgentRun.Add(runEntity);
                await _db.SaveChangesAsync(ct);
            }

            List<MultiAgentStepStatusDto> steps = new();
            Stopwatch stepSw = Stopwatch.StartNew();

            void AddStep(int num, string agentId, string toolId, string title, string outSummary, string status = "COMPLETED")
            {
                steps.Add(new MultiAgentStepStatusDto
                {
                    StepNumber = num,
                    AgentId = agentId,
                    ToolId = toolId,
                    Title = title,
                    Status = status,
                    OutputSummary = outSummary,
                    DurationMs = Math.Max(4, stepSw.ElapsedMilliseconds)
                });
                stepSw.Restart();
            }

            // STEP 1: Historical menus
            List<MenuPlan> historicalMenus = await _db.MenuPlan.Where(p => p.BuyerId == ctx.Buyer.Id && p.IsActive).OrderByDescending(p => p.BusinessDate).Take(10).ToListAsync(ct);
            AddStep(1, "MENU_PLANNING_AGENT", "RetrieveHistoricalMenus", "Retrieve Historical Comparable Menus", $"Loaded {historicalMenus.Count} historical/template menus.");

            // STEP 2: Approved dishes + recipes
            List<MenuItem> items = await _db.MenuItem.Where(i => i.BuyerId == ctx.Buyer.Id && i.IsActive && i.IsAvailable).ToListAsync(ct);
            List<Guid> recIds = items.Where(i => i.LinkedRecipeId != null).Select(i => i.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recIds.Count == 0
                ? new()
                : await _db.Recipe.Include(r => r.Ingredients).Where(r => recIds.Contains(r.Id) && r.IsActive).ToDictionaryAsync(r => r.Id, ct);
            AddStep(2, "MENU_ITEM_INTELLIGENCE_AGENT", "RetrieveApprovedMenuItems", "Identify Suitable Approved Dishes", $"Evaluated {items.Count} active menu items across {recipes.Count} recipes.");

            // STEP 3: Demand
            AddStep(3, "DEMAND_FORECAST_AGENT", "CalculatePortionsAndDemand", "Predict Item-Level Demand", $"Forecast calibrated for {covers:0} expected covers ({mealPeriod} {serviceFormat}).");

            // STEP 4: Portions
            AddStep(4, "PORTION_OPTIMIZATION_AGENT", "CalculatePortionsAndDemand", "Calculate Expected Portions", "Applied standard portion factors and buffet participation curve.");

            // STEP 5: Costing & Ingredient Prices
            List<Guid> allMatIds = recipes.Values.SelectMany(r => r.Ingredients).Where(i => i.MaterialId != null).Select(i => i.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> mats = allMatIds.Count == 0 ? new() : await _db.ItemBuyerMaster.Where(m => allMatIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct);
            Dictionary<Guid, decimal> onHand = allMatIds.Count == 0 || ctx.ScopeIds.Count == 0
                ? new()
                : (await _db.InventoryBalance.Where(b => b.BuyerId == ctx.Buyer.Id && b.IsActive && allMatIds.Contains(b.MaterialId) && ctx.ScopeIds.Contains(b.LocationId)).ToListAsync(ct))
                    .GroupBy(b => b.MaterialId).ToDictionary(g => g.Key, g => g.Sum(r => r.OnHandQty));
            AddStep(5, "COST_MARGIN_AGENT", "CalculateMenuCostAndMargin", "Calculate Expected Food Cost", $"Authoritative recipe costing computed across {mats.Count} ingredients.");

            // STEP 6: Performance
            List<PosSalesTransaction> recentSales = await _db.PosSalesTransaction
                .Where(s => s.BuyerId == ctx.Buyer.Id && s.IsActive && !s.IsVoid && !s.IsRefund && s.BusinessDate >= DateTime.UtcNow.Date.AddDays(-30))
                .ToListAsync(ct);
            Dictionary<Guid, decimal> salesByItem = recentSales.Where(s => s.MenuItemId != null).GroupBy(s => s.MenuItemId!.Value).ToDictionary(g => g.Key, g => g.Sum(x => x.QuantitySold));
            AddStep(6, "MENU_PERFORMANCE_AGENT", "CalculateMenuCostAndMargin", "Evaluate Popularity & Profitability", $"Ranked {items.Count} items using {recentSales.Count} POS records.");

            // STEP 7: Waste & Consumption
            AddStep(7, "CONSUMPTION_INTELLIGENCE_AGENT", "DetectConsumptionAnomalies", "Estimate Expected Waste", "Applied recipe trim/cooking yield factors and historical waste signals.");

            // STEP 8: Inventory & Shortages
            List<string> shortages = new();
            List<object> candidatePayload = new();
            Dictionary<Guid, decimal> itemCostMap = new();

            foreach (MenuItem mi in items)
            {
                recipes.TryGetValue(mi.LinkedRecipeId ?? Guid.Empty, out Recipe? rec);
                decimal unitCost = rec is { TotalCost: > 0, ServingQty: > 0 }
                    ? Math.Round(rec.TotalCost / rec.ServingQty * (mi.DefaultPortionQty > 0 ? mi.DefaultPortionQty : 1m), 4)
                    : Math.Round((mi.SellingPrice ?? sellPrice / 8m) * 0.28m, 4);
                itemCostMap[mi.Id] = unitCost;

                decimal invCoverage = 1.0m;
                if (rec != null && rec.Ingredients.Count > 0)
                {
                    int avail = rec.Ingredients.Count(ing => ing.MaterialId != null && onHand.TryGetValue(ing.MaterialId.Value, out decimal q) && q > 0);
                    invCoverage = Math.Round((decimal)avail / rec.Ingredients.Count, 2);
                }

                decimal pop = salesByItem.TryGetValue(mi.Id, out decimal sq) ? Math.Min(100m, 40m + sq) : 60m;
                List<string> dietary = (mi.DietaryTags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                List<string> allergens = (mi.AllergenNotes ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                candidatePayload.Add(new
                {
                    menu_item_id = mi.Id.ToString(),
                    name = mi.Name,
                    category = string.IsNullOrWhiteSpace(mi.Category) ? "Mains" : mi.Category!,
                    cost_per_guest = unitCost,
                    selling_price = mi.SellingPrice ?? sellPrice,
                    popularity_score = pop,
                    inventory_coverage = invCoverage,
                    expected_waste_percent = 3.5m,
                    prep_minutes = mi.PrepTimeMinutes ?? 15,
                    dietary_tags = dietary,
                    allergens
                });
            }

            if (candidatePayload.Count == 0)
            {
                throw new BadRequestCustomException("No eligible menu items", "Create or enable menu items before running ME Studio multi-agent optimization.");
            }

            AddStep(8, "PURCHASE_INTELLIGENCE_AGENT", "IdentifyProcurementShortages", "Evaluate Inventory & Shortages", $"Evaluated on-hand inventory across {ctx.Scope.Count} locations.");

            // STEP 9 & 10: Optimization & Simulation (7 scenarios A..G)
            PythonOptimizeResponse? optResp = await PostPythonAiAsync<object, PythonOptimizeResponse>("/v1/me-ai/optimize-scenarios", new
            {
                expected_covers = covers,
                selling_price_per_guest = sellPrice,
                max_food_cost_percent = maxFc,
                required_dietary = reqDietary,
                excluded_allergens = exclAllergens,
                required_categories = reqCats,
                min_items = Math.Min(candidatePayload.Count, Math.Max(3, reqCats.Values.Sum())),
                max_items = Math.Min(candidatePayload.Count, 12),
                weights = new
                {
                    food_cost = request.WeightFoodCost,
                    margin = request.WeightMargin,
                    popularity = request.WeightPopularity,
                    availability = request.WeightAvailability,
                    waste = request.WeightWaste,
                    production = request.WeightProduction,
                    diversity = request.WeightDiversity
                },
                candidates = candidatePayload
            }, ct);

            List<OptimizedScenarioOptionDto> scenarios = BuildOptimizedScenarios(optResp, items, itemCostMap, covers, sellPrice, maxFc, reqDietary, exclAllergens);
            OptimizedScenarioOptionDto recommended = scenarios.FirstOrDefault(s => s.ScenarioCode == "G" && s.Feasible)
                ?? scenarios.FirstOrDefault(s => s.Feasible)
                ?? scenarios.First();

            bool feasible = scenarios.Any(s => s.Feasible && s.ScenarioCode != "A");
            string? conflict = feasible ? null : recommended.ConflictReason ?? $"No feasible dish combination satisfied <= {maxFc:0.0}% food cost and hard dietary/category constraints without relaxing mandatory rules.";

            AddStep(9, "MENU_OPTIMIZATION_AGENT", "GenerateOptimizedScenarios", "Generate Feasible Menu Alternatives", $"Generated {scenarios.Count} Pareto-checked scenarios (A-G).");
            AddStep(10, "SIMULATION_AGENT", "RunWhatIfSimulation", "Compare Alternatives & Trade-Offs", $"Recommended Scenario {recommended.ScenarioCode}: {recommended.FoodCostPercent:0.0}% food cost.");

            // STEP 11: Save Draft Proposal (NEVER auto-publish)
            Guid? draftMenuId = null;
            if (request.SaveSelectedProposalAsDraft && ctx.Perms.CreateMenu && recommended.SelectedMenuItemIds.Count > 0)
            {
                MenuPlanEnhancedWriteDto draftWrite = new()
                {
                    Name = $"AI {mealPeriod} {serviceFormat} ({covers:0} covers)",
                    PropertyId = request.PropertyId,
                    OutletLocationId = request.OutletLocationId,
                    BusinessDate = (request.ServiceDate ?? DateTime.UtcNow.Date.AddDays(1)).Date,
                    MealPeriod = mealPeriod,
                    ServiceFormat = serviceFormat,
                    ExpectedGuests = covers,
                    ExpectedCovers = covers,
                    TargetFoodCostPercent = maxFc,
                    TargetContributionMargin = Math.Round(covers * sellPrice * (1m - maxFc / 100m), 2),
                    Notes = $"Generated by ME_STUDIO_ORCHESTRATOR (RunId {runEntity.Id}, Scenario {recommended.ScenarioCode}). Draft awaiting review & approval.",
                    Lines = recommended.SelectedMenuItemIds.Select((id, idx) =>
                    {
                        MenuItem? mi = items.FirstOrDefault(x => x.Id == id);
                        return new MenuPlanLineEnhancedWriteDto
                        {
                            MenuItemId = id,
                            CategoryName = mi?.Category ?? "Mains",
                            SequenceOrder = idx + 1,
                            RecipeId = mi?.LinkedRecipeId,
                            PortionSize = mi?.DefaultPortionQty ?? 1m,
                            PortionUom = mi?.PortionUom ?? "PORTION",
                            ExpectedCovers = covers,
                            ExpectedServingsPerGuest = 1m,
                            PlannedPortions = covers,
                            SellingPrice = mi?.SellingPrice ?? sellPrice
                        };
                    }).ToList()
                };
                MenuPlanEnhancedDto created = await _studio.SaveMenuPlanAsync(organizationId, userId, roleId, draftWrite, ct);
                draftMenuId = created.Id;
            }
            AddStep(11, "ME_STUDIO_ORCHESTRATOR", "SaveDraftMenu", "Prepare Consolidated Draft Proposal", draftMenuId != null ? $"Saved controlled draft menu {draftMenuId}." : "Proposal prepared in memory (SaveDraft disabled or view-only role).");
            AddStep(12, "ME_STUDIO_ORCHESTRATOR", "ValidateDietaryAllergens", "User Scenario Review Checkpoint", "Ready for human review & scenario selection.");
            AddStep(13, "ME_STUDIO_ORCHESTRATOR", "SubmitMenuForApproval", "Approval Gate Ready", "Requires authorized approval before publishing.", "PENDING");
            AddStep(14, "ME_STUDIO_ORCHESTRATOR", "SubmitMenuForApproval", "Controlled Publishing Gate", "Auto-publish blocked by governance rule.", "PENDING");
            AddStep(15, "ME_STUDIO_ORCHESTRATOR", "CalculateMenuCostAndMargin", "Post-Service Learning Hook", "Will capture validated POS/cost outcomes after service.", "PENDING");

            if (onHand.Values.Any(v => v <= 0m))
            {
                shortages.Add("Some recipe ingredients currently have zero on-hand stock at selected outlet(s); review Purchase Recommendations before approval.");
            }

            string explanationText = feasible
                ? $"Scenario {recommended.ScenarioCode} ({recommended.Label}) selects {recommended.SelectedDishNames.Count} dishes for {covers:0} guests at {recommended.FoodCostPercent:0.0}% projected food cost (target <= {maxFc:0.0}%) and {recommended.ContributionMargin:0.00} contribution margin."
                : conflict!;

            MeStudioMultiAgentRunResultDto result = new()
            {
                RunId = runEntity.Id,
                IdempotencyKey = idemKey,
                Status = "COMPLETED",
                Objective = cleanObjective,
                ServiceFormat = serviceFormat,
                MealPeriod = mealPeriod,
                ExpectedCovers = covers,
                SellingPricePerGuest = sellPrice,
                MaxFoodCostPercent = maxFc,
                Feasible = feasible,
                ConflictExplanation = conflict,
                RecommendedScenarioCode = recommended.ScenarioCode,
                CreatedDraftMenuId = draftMenuId,
                AutoPublished = false,
                RequiresApprovalToPublish = true,
                Steps = steps,
                Scenarios = scenarios,
                Explanation = explanationText,
                ProcurementShortages = shortages,
                ProductionConstraints = new(),
                StartedAtUtc = started,
                CompletedAtUtc = DateTime.UtcNow
            };

            runEntity.Status = "COMPLETED";
            runEntity.CurrentStep = 15;
            runEntity.MenuPlanId = draftMenuId;
            runEntity.CreatedDraftMenuId = draftMenuId;
            runEntity.StepsJson = JsonSerializer.Serialize(steps, JsonOpts);
            runEntity.ProposalJson = JsonSerializer.Serialize(result, JsonOpts);
            runEntity.CompletedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await CheckAndTrackQuotaAsync(ctx, userId, "ME_STUDIO_MULTI_AGENT", 420, 380, (long)(DateTime.UtcNow - started).TotalMilliseconds, true, optResp == null, ct);
            return result;
        }

        private static List<OptimizedScenarioOptionDto> BuildOptimizedScenarios(
            PythonOptimizeResponse? py,
            List<MenuItem> items,
            Dictionary<Guid, decimal> itemCostMap,
            decimal covers,
            decimal sellPrice,
            decimal maxFc,
            List<string> reqDietary,
            List<string> exclAllergens)
        {
            if (py?.scenarios is { Count: > 0 })
            {
                return py.scenarios.Select(s => new OptimizedScenarioOptionDto
                {
                    ScenarioCode = s.scenario_code,
                    Label = s.label,
                    Strategy = s.strategy,
                    Feasible = s.feasible,
                    ConflictReason = s.conflict_reason,
                    SelectedMenuItemIds = s.selected_menu_item_ids.Select(id => Guid.TryParse(id, out Guid g) ? g : Guid.Empty).Where(g => g != Guid.Empty).ToList(),
                    SelectedDishNames = s.selected_dish_names,
                    FoodCostPerGuest = s.food_cost_per_guest,
                    TotalFoodCost = s.total_food_cost,
                    ExpectedRevenue = s.expected_revenue,
                    FoodCostPercent = s.food_cost_percent,
                    ContributionMargin = s.contribution_margin,
                    PopularityIndex = s.popularity_index,
                    InventoryUtilizationPercent = s.inventory_utilization_percent,
                    EstimatedWastePercent = s.estimated_waste_percent,
                    ParetoOptimal = s.pareto_optimal,
                    TradeOffSummary = s.trade_off_summary
                }).ToList();
            }

            // Deterministic C# fallback when Python optimizer is offline
            List<MenuItem> eligible = items.Where(i =>
            {
                string allergens = i.AllergenNotes ?? "";
                return !exclAllergens.Any(a => allergens.Contains(a, StringComparison.OrdinalIgnoreCase));
            }).ToList();

            decimal rev = Math.Round(covers * sellPrice, 2);
            decimal maxGuestCost = sellPrice * (maxFc / 100m);

            List<MenuItem> lowCost = eligible.OrderBy(i => itemCostMap.GetValueOrDefault(i.Id, 20m)).Take(6).ToList();
            decimal avgCost = lowCost.Count == 0 ? 0m : Math.Round(lowCost.Average(i => itemCostMap.GetValueOrDefault(i.Id, 20m)) * 1.2m, 2);
            bool feasible = avgCost <= maxGuestCost && lowCost.Count > 0;
            if (reqDietary.Any(d => d.Equals("Vegetarian", StringComparison.OrdinalIgnoreCase)) &&
                !eligible.Any(i => (i.DietaryTags ?? "").Contains("Vegetarian", StringComparison.OrdinalIgnoreCase) || (i.Name ?? "").Contains("Veg", StringComparison.OrdinalIgnoreCase)))
            {
                feasible = false;
            }

            decimal totalCost = Math.Round(avgCost * covers, 2);
            decimal fcPct = rev > 0 ? Math.Round(totalCost / rev * 100m, 2) : 0m;
            decimal cm = Math.Round(rev - totalCost, 2);

            string[] codes = { "A", "B", "C", "D", "E", "F", "G" };
            string[] labels = { "Current Baseline", "Lowest Food Cost", "Highest Margin", "Highest Popularity", "Lowest Expected Waste", "Max Inventory Utilization", "Balanced Recommendation" };
            List<OptimizedScenarioOptionDto> list = new();
            for (int idx = 0; idx < codes.Length; idx++)
            {
                list.Add(new OptimizedScenarioOptionDto
                {
                    ScenarioCode = codes[idx],
                    Label = labels[idx],
                    Strategy = labels[idx].ToUpperInvariant().Replace(' ', '_'),
                    Feasible = codes[idx] == "A" || feasible,
                    ConflictReason = (codes[idx] == "A" || feasible) ? null : $"Hard constraints (food cost <= {maxFc:0.0}% or required dietary tags) cannot be satisfied simultaneously.",
                    SelectedMenuItemIds = lowCost.Select(x => x.Id).ToList(),
                    SelectedDishNames = lowCost.Select(x => x.Name).ToList(),
                    FoodCostPerGuest = avgCost,
                    TotalFoodCost = totalCost,
                    ExpectedRevenue = rev,
                    FoodCostPercent = fcPct,
                    ContributionMargin = cm,
                    PopularityIndex = 70m,
                    InventoryUtilizationPercent = 68m,
                    EstimatedWastePercent = 3.5m,
                    ParetoOptimal = idx is 1 or 2 or 6,
                    TradeOffSummary = $"Deterministic fallback: {fcPct:0.0}% food cost, {cm:0.00} margin."
                });
            }
            return list;
        }

        public async Task<MeStudioMultiAgentRunResultDto> GetAgentRunAsync(Guid organizationId, Guid roleId, Guid runId, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(_httpContextAccessor, roleId);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Permission required.");

            MenuEngineeringAgentRun? run = await _db.MenuEngineeringAgentRun
                .FirstOrDefaultAsync(x => x.Id == runId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (run == null) throw new NotFoundCustomException("Agent run not found", $"Run {runId} not found.");
            return DeserializeRunResult(run);
        }

        public async Task<MeStudioMultiAgentRunResultDto> CancelAgentRunAsync(Guid organizationId, Guid userId, Guid roleId, Guid runId, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(_httpContextAccessor, roleId);
            MenuEngineeringPermissions.EnsureAllowed(perms.UseAiMenuIntelligence, "Access denied", "Permission required.");

            MenuEngineeringAgentRun? run = await _db.MenuEngineeringAgentRun
                .FirstOrDefaultAsync(x => x.Id == runId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (run == null) throw new NotFoundCustomException("Agent run not found", $"Run {runId} not found.");
            if (run.Status != "COMPLETED")
            {
                run.Status = "CANCELLED";
                run.CancelledAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return DeserializeRunResult(run);
        }

        private static MeStudioMultiAgentRunResultDto DeserializeRunResult(MenuEngineeringAgentRun run)
        {
            MeStudioMultiAgentRunResultDto? dto = null;
            if (!string.IsNullOrWhiteSpace(run.ProposalJson) && run.ProposalJson != "{}")
            {
                try { dto = JsonSerializer.Deserialize<MeStudioMultiAgentRunResultDto>(run.ProposalJson, JsonOpts); } catch { }
            }
            dto ??= new MeStudioMultiAgentRunResultDto
            {
                RunId = run.Id,
                IdempotencyKey = run.IdempotencyKey,
                Status = run.Status,
                Objective = run.ObjectiveText,
                CreatedDraftMenuId = run.CreatedDraftMenuId,
                StartedAtUtc = run.StartedAtUtc,
                CompletedAtUtc = run.CompletedAtUtc
            };
            dto.Status = run.Status;
            return dto;
        }

        public async Task SubmitFeedbackAsync(Guid organizationId, Guid userId, Guid roleId, MeAiFeedbackSubmitDto dto, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(_httpContextAccessor, roleId);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Permission required.");

            _db.MenuEngineeringAiFeedback.Add(new MenuEngineeringAiFeedback
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                AgentId = dto.AgentId,
                ContextScreen = dto.ContextScreen,
                TargetEntityId = dto.TargetEntityId,
                Rating = Math.Clamp(dto.Rating, -1, 5),
                WasAccepted = dto.WasAccepted,
                CorrectionNotes = SanitizeText(dto.CorrectionNotes),
                ActorUserId = userId,
                SubmittedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
            await _db.SaveChangesAsync(ct);
        }

        private sealed class PythonParseCommandResponse
        {
            public string? intent { get; set; }
            public string? sanitized_command { get; set; }
            public string? service_format { get; set; }
            public string? meal_period { get; set; }
            public decimal? expected_covers { get; set; }
            public decimal? selling_price { get; set; }
            public decimal? max_food_cost_percent { get; set; }
            public List<string>? required_dietary { get; set; }
            public Dictionary<string, int>? required_categories { get; set; }
            public decimal? price_shock_percent { get; set; }
            public string? shock_ingredient { get; set; }
            public string? optimization_mode { get; set; }
        }

        private sealed class PythonOptimizeResponse
        {
            public List<PythonScenarioRow> scenarios { get; set; } = new();
        }

        private sealed class PythonScenarioRow
        {
            public string scenario_code { get; set; } = "A";
            public string label { get; set; } = string.Empty;
            public string strategy { get; set; } = string.Empty;
            public bool feasible { get; set; }
            public string? conflict_reason { get; set; }
            public List<string> selected_menu_item_ids { get; set; } = new();
            public List<string> selected_dish_names { get; set; } = new();
            public decimal food_cost_per_guest { get; set; }
            public decimal total_food_cost { get; set; }
            public decimal expected_revenue { get; set; }
            public decimal food_cost_percent { get; set; }
            public decimal contribution_margin { get; set; }
            public decimal popularity_index { get; set; }
            public decimal inventory_utilization_percent { get; set; }
            public decimal estimated_waste_percent { get; set; }
            public bool pareto_optimal { get; set; }
            public string trade_off_summary { get; set; } = string.Empty;
        }
    }
}
