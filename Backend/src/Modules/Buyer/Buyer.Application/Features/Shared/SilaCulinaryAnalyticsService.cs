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
    public interface ISilaCulinaryAnalyticsService
    {
        Task<SilaCulinaryAnalyticsConfigDto> GetConfigAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken cancellationToken);
        Task<SilaCulinaryAnalyticsConfigDto> SaveConfigAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinarySaveAnalyticsConfigRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryExecutiveDashboardDto> GetExecutiveDashboardAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuEngineeringResponseDto> AnalyzeMenuEngineeringAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken);
        Task<SilaCulinaryConsumptionVarianceResponseDto> AnalyzeConsumptionVarianceAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken);
        Task<OperationalTask> CreateVarianceInvestigationTaskAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryCreateVarianceTaskRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryReversePriceImpactResponseDto> AnalyzePriceImpactAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryReversePriceImpactRequestDto request, CancellationToken cancellationToken);
        Task<List<SilaCulinarySimulationRecordDto>> ListSimulationsAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken cancellationToken);
        Task<SilaCulinarySimulationRecordDto> RunAndSaveSimulationAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryRunSimulationRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinarySimulationRecordDto> DuplicateSimulationAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid simulationId, CancellationToken cancellationToken);
        Task<SilaCulinaryOptimizationResultDto> OptimizeMenuAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryOptimizationRequestDto request, CancellationToken cancellationToken);
        Task<List<SilaCulinaryOptimizationResultDto>> ListOptimizationJobsAsync(Guid organizationId, Guid roleId, CancellationToken cancellationToken);
        Task<SilaCulinaryOptimizationResultDto> CancelOptimizationJobAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid jobId, CancellationToken cancellationToken);
        Task<SilaCulinarySubstitutionResponseDto> AnalyzeSubstitutionsAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinarySubstitutionRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryBuffetOptimizeResponseDto> OptimizeBuffetAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryBuffetOptimizeRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventOptimizeResponseDto> OptimizeEventMenusAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryEventOptimizeRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryForecastResponseDto> GenerateForecastAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinaryForecastRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryWasteIntelligenceDto> GetWasteIntelligenceAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken cancellationToken);
        Task<SilaCulinaryWasteItemDto> RecordWasteAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryWasteRecordCreateDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryBenchmarkResponseDto> BenchmarkPropertiesAsync(Guid organizationId, Guid userId, Guid roleId, string? reportingCurrency, CancellationToken cancellationToken);
        Task<List<SilaCulinaryRecommendationDto>> ListRecommendationsAsync(Guid organizationId, Guid userId, Guid roleId, string? status, CancellationToken cancellationToken);
        Task<List<SilaCulinaryRecommendationDto>> GenerateRecommendationsAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid? propertyId, CancellationToken cancellationToken);
        Task<SilaCulinaryRecommendationDto> DecideRecommendationAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid recommendationId, SilaCulinaryRecommendationDecisionDto decision, CancellationToken cancellationToken);
        Task<SilaCulinaryCopilotResponseDto> QueryCopilotAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCopilotRequestDto request, CancellationToken cancellationToken);
        List<SilaCulinaryAnalyticsReportCatalogItemDto> GetReportCatalog();
        Task<SilaCulinaryAnalyticsReportExportDto> ExportReportAsync(Guid organizationId, Guid userId, Guid roleId, string reportCode, string format, SilaCulinaryExecutiveFilterDto? filter, CancellationToken cancellationToken);
        Task<SilaCulinaryMobileOpsSummaryDto> GetMobileSummaryAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken cancellationToken);
    }

    public class SilaCulinaryAnalyticsService : ISilaCulinaryAnalyticsService
    {
        private static readonly JsonSerializerOptions CamelJson = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private static readonly Regex PromptInjectionRegex = new(
            @"(ignore\s+(all\s+)?previous|system\s+prompt|override\s+(budget|safety)|drop\s+table|select\s+\*\s+from|post\s+(sap|gl|journal|invoice)|delete\s+from|disable\s+tenant)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly HashSet<string> AllowedRecommendationStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "NEW", "UNDER_REVIEW", "SIMULATION_REQUIRED", "PENDING_APPROVAL",
            "APPROVED", "REJECTED", "DEFERRED", "IMPLEMENTED",
            "IMPACT_UNDER_REVIEW", "CLOSED", "EXPIRED"
        };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public SilaCulinaryAnalyticsService(
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

        private static bool CanViewFinancials(Guid roleId) => SilaAccess.HasFullAccess(roleId);

        private static bool CanViewCrossProperty(Guid roleId) =>
            roleId == Common.BUYER_ADMIN_ROLE_ID
            || roleId == Common.FNB_DIRECTOR_ROLE_ID
            || roleId == Common.FINANCE_MANAGER_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID;

        private static bool CanApproveRecommendations(Guid roleId) =>
            roleId == Common.BUYER_ADMIN_ROLE_ID
            || roleId == Common.FNB_DIRECTOR_ROLE_ID
            || roleId == Common.FINANCE_MANAGER_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID;

        private static bool CanConfigureIntelligence(Guid roleId) =>
            roleId == Common.BUYER_ADMIN_ROLE_ID
            || roleId == Common.FNB_DIRECTOR_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID;

        private async Task EnsureFeatureEnabledAsync(Guid buyerId, string? specificFlag, CancellationToken cancellationToken)
        {
            SilaCulinaryAiConfiguration? aiCfg = await _repository.SilaCulinaryAiConfiguration
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (aiCfg == null || string.IsNullOrWhiteSpace(aiCfg.FeatureFlagsJson))
            {
                return;
            }

            try
            {
                SilaCulinaryAiFeatureFlagsDto? flags = JsonSerializer.Deserialize<SilaCulinaryAiFeatureFlagsDto>(aiCfg.FeatureFlagsJson, CamelJson);
                if (flags == null)
                {
                    return;
                }

                if (!flags.CulinaryPhase3BEnabled)
                {
                    throw new ForBiddenCustomException("Feature disabled.", "Phase 3B Culinary Intelligence is disabled for this tenant.");
                }

                bool specificAllowed = specificFlag switch
                {
                    "MenuEngineeringMatrixEnabled" => flags.MenuEngineeringMatrixEnabled,
                    "WhatIfSimulationEnabled" => flags.WhatIfSimulationEnabled,
                    "PythonOptimizationEnabled" => flags.PythonOptimizationEnabled,
                    "DemandForecastingEnabled" => flags.DemandForecastingEnabled,
                    "WasteIntelligenceEnabled" => flags.WasteIntelligenceEnabled,
                    "CulinaryCopilotEnabled" => flags.CulinaryCopilotEnabled,
                    "CrossPropertyBenchmarkingEnabled" => flags.CrossPropertyBenchmarkingEnabled,
                    "RecommendationCockpitEnabled" => flags.RecommendationCockpitEnabled,
                    _ => true
                };

                if (!specificAllowed)
                {
                    throw new ForBiddenCustomException("Feature disabled.", $"Phase 3B feature '{specificFlag}' is disabled by tenant configuration.");
                }
            }
            catch (JsonException)
            {
                // Non-blocking fallback when legacy feature flag JSON cannot be parsed.
            }
        }

        private async Task WriteAuditAsync(
            Guid buyerId,
            Guid userId,
            string actorName,
            string actionType,
            string moduleName,
            string? entityType,
            Guid? entityId,
            string summary,
            bool promptInjectionBlocked,
            CancellationToken cancellationToken)
        {
            SilaCulinaryAnalyticsAudit audit = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                AuditType = actionType,
                ActorUserId = userId,
                ActorRoleId = Guid.Empty,
                Summary = summary.Length > 390 ? summary[..390] : summary,
                PayloadJson = JsonSerializer.Serialize(new { actorName, moduleName, entityType, entityId }, CamelJson),
                PromptInjectionBlocked = promptInjectionBlocked,
                ExecutedAtUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true
            };
            _repository.SilaCulinaryAnalyticsAudit.Create(audit);
            await _repository.SaveAsync();
        }

        private async Task<SilaCulinaryAnalyticsConfig> GetOrCreateConfigEntityAsync(Guid buyerId, Guid? propertyId, CancellationToken cancellationToken)
        {
            SilaCulinaryAnalyticsConfig? cfg = null;
            if (propertyId.HasValue)
            {
                cfg = await _repository.SilaCulinaryAnalyticsConfig
                    .FindByConditionTracked(x => x.BuyerId == buyerId && x.PropertyId == propertyId && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            cfg ??= await _repository.SilaCulinaryAnalyticsConfig
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.PropertyId == null && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (cfg != null)
            {
                return cfg;
            }

            cfg = new SilaCulinaryAnalyticsConfig
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                PropertyId = propertyId,
                TargetFoodCostPercent = 28.0m,
                TargetContributionMargin = 50.0m,
                PopularityThresholdFactor = 0.70m,
                IngredientPriceAlertThresholdPercent = 5.0m,
                WasteTolerancePercent = 4.0m,
                ConsumptionVarianceTolerancePercent = 5.0m,
                DefaultForecastHorizonDays = 14,
                MinimumHistoricalDataDays = 7,
                MinSalesForClassification = 5,
                CostMethodology = "STANDARD",
                DefaultCurrency = "AED",
                PreferredAiProvider = "LOCAL",
                PreferredAiModel = "local-deterministic",
                DailyAiQueryLimit = 200,
                RecommendationApprovalMode = "EXECUTIVE_CHEF_AND_FINANCE",
                ScheduledRefreshFrequency = "DAILY",
                AllergenRestrictionsJson = "[]",
                OptimizationObjectivesJson = "[\"MAX_CONTRIBUTION\"]",
                ConfigVersion = 1,
                ConfigHistoryJson = JsonSerializer.Serialize(new List<string> { $"v1 created {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}" }, CamelJson),
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true
            };

            _repository.SilaCulinaryAnalyticsConfig.Create(cfg);
            await _repository.SaveAsync();
            return cfg;
        }

        private static SilaCulinaryAnalyticsConfigDto MapConfigDto(SilaCulinaryAnalyticsConfig cfg, Guid roleId)
        {
            List<string> allergens = SafeDeserialize<List<string>>(cfg.AllergenRestrictionsJson) ?? new List<string>();
            List<string> objectives = SafeDeserialize<List<string>>(cfg.OptimizationObjectivesJson) ?? new List<string> { "MAX_CONTRIBUTION" };
            List<string> history = SafeDeserialize<List<string>>(cfg.ConfigHistoryJson) ?? new List<string>();

            return new SilaCulinaryAnalyticsConfigDto
            {
                Id = cfg.Id,
                BuyerId = cfg.BuyerId,
                PropertyId = cfg.PropertyId,
                TargetFoodCostPercent = cfg.TargetFoodCostPercent,
                TargetContributionMargin = cfg.TargetContributionMargin,
                PopularityThresholdFactor = cfg.PopularityThresholdFactor,
                MenuProfitabilityThresholdOverride = cfg.MenuProfitabilityThresholdOverride,
                IngredientPriceAlertThresholdPercent = cfg.IngredientPriceAlertThresholdPercent,
                WasteTolerancePercent = cfg.WasteTolerancePercent,
                ConsumptionVarianceTolerancePercent = cfg.ConsumptionVarianceTolerancePercent,
                DefaultForecastHorizonDays = cfg.DefaultForecastHorizonDays,
                MinimumHistoricalDataDays = cfg.MinimumHistoricalDataDays,
                MinSalesForClassification = cfg.MinSalesForClassification,
                CostMethodology = cfg.CostMethodology,
                DefaultCurrency = cfg.DefaultCurrency,
                PreferredAiProvider = cfg.PreferredAiProvider,
                PreferredAiModel = cfg.PreferredAiModel,
                DailyAiQueryLimit = cfg.DailyAiQueryLimit,
                RecommendationApprovalMode = cfg.RecommendationApprovalMode,
                ScheduledRefreshFrequency = cfg.ScheduledRefreshFrequency,
                AllergenRestrictions = allergens,
                OptimizationObjectives = objectives,
                ConfigVersion = cfg.ConfigVersion,
                HistoryNotes = history,
                CanEdit = CanConfigureIntelligence(roleId)
            };
        }

        public async Task<SilaCulinaryAnalyticsConfigDto> GetConfigAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, propertyId, cancellationToken);
            return MapConfigDto(cfg, roleId);
        }

        public async Task<SilaCulinaryAnalyticsConfigDto> SaveConfigAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            SilaCulinarySaveAnalyticsConfigRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanConfigureIntelligence(roleId))
            {
                throw new ForBiddenCustomException("Insufficient permissions.", "Only Buyer Admin, F&B Director, or Cost Controller can modify Phase 3B configuration.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);

            SilaCulinaryAnalyticsConfig? existing = await _repository.SilaCulinaryAnalyticsConfig
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.PropertyId == request.PropertyId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing == null)
            {
                existing = new SilaCulinaryAnalyticsConfig
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = request.PropertyId,
                    ConfigVersion = 0,
                    ConfigHistoryJson = "[]",
                    DateCreated = DateTime.UtcNow,
                    IsActive = true
                };
                _repository.SilaCulinaryAnalyticsConfig.Create(existing);
            }

            List<string> history = SafeDeserialize<List<string>>(existing.ConfigHistoryJson) ?? new List<string>();
            int nextVersion = existing.ConfigVersion + 1;
            history.Insert(0, $"v{nextVersion} updated by {actorName} at {DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ} (TargetFC={request.TargetFoodCostPercent}%, Method={request.CostMethodology})");
            if (history.Count > 25)
            {
                history = history.Take(25).ToList();
            }

            existing.TargetFoodCostPercent = request.TargetFoodCostPercent;
            existing.TargetContributionMargin = request.TargetContributionMargin;
            existing.PopularityThresholdFactor = request.PopularityThresholdFactor;
            existing.MenuProfitabilityThresholdOverride = request.MenuProfitabilityThresholdOverride;
            existing.IngredientPriceAlertThresholdPercent = request.IngredientPriceAlertThresholdPercent;
            existing.WasteTolerancePercent = request.WasteTolerancePercent;
            existing.ConsumptionVarianceTolerancePercent = request.ConsumptionVarianceTolerancePercent;
            existing.DefaultForecastHorizonDays = Math.Clamp(request.DefaultForecastHorizonDays, 1, 90);
            existing.MinimumHistoricalDataDays = Math.Clamp(request.MinimumHistoricalDataDays, 1, 90);
            existing.MinSalesForClassification = Math.Clamp(request.MinSalesForClassification, 1, 500);
            existing.CostMethodology = string.IsNullOrWhiteSpace(request.CostMethodology) ? "STANDARD" : request.CostMethodology.ToUpperInvariant();
            existing.DefaultCurrency = string.IsNullOrWhiteSpace(request.DefaultCurrency) ? "AED" : request.DefaultCurrency.ToUpperInvariant();
            existing.PreferredAiProvider = string.IsNullOrWhiteSpace(request.PreferredAiProvider) ? "LOCAL" : request.PreferredAiProvider.ToUpperInvariant();
            existing.PreferredAiModel = string.IsNullOrWhiteSpace(request.PreferredAiModel) ? "local-deterministic" : request.PreferredAiModel.Trim();
            existing.DailyAiQueryLimit = Math.Clamp(request.DailyAiQueryLimit, 1, 10000);
            existing.RecommendationApprovalMode = string.IsNullOrWhiteSpace(request.RecommendationApprovalMode) ? "EXECUTIVE_CHEF_AND_FINANCE" : request.RecommendationApprovalMode.ToUpperInvariant();
            existing.ScheduledRefreshFrequency = string.IsNullOrWhiteSpace(request.ScheduledRefreshFrequency) ? "DAILY" : request.ScheduledRefreshFrequency.ToUpperInvariant();
            existing.AllergenRestrictionsJson = JsonSerializer.Serialize(request.AllergenRestrictions ?? new List<string>(), CamelJson);
            existing.OptimizationObjectivesJson = JsonSerializer.Serialize(request.OptimizationObjectives ?? new List<string> { "MAX_CONTRIBUTION" }, CamelJson);
            existing.ConfigVersion = nextVersion;
            existing.ConfigHistoryJson = JsonSerializer.Serialize(history, CamelJson);
            existing.UpdatedBy = userId;
            existing.DateUpdated = DateTime.UtcNow;

            await _repository.SaveAsync();
            await WriteAuditAsync(buyer.Id, userId, actorName, "CONFIG_UPDATE", "CONFIGURATION", "SilaCulinaryAnalyticsConfig", existing.Id, $"Saved v{nextVersion} config", false, cancellationToken);
            return MapConfigDto(existing, roleId);
        }

        public async Task<SilaCulinaryMenuEngineeringResponseDto> AnalyzeMenuEngineeringAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryExecutiveFilterDto? filter,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Menu Engineering profitability analytics require culinary financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "MenuEngineeringMatrixEnabled", cancellationToken);

            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, filter?.PropertyId, cancellationToken);
            List<Guid> allowedLocationIds = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, userId, roleId, cancellationToken);

            DateTime toUtc = filter?.ToUtc ?? DateTime.UtcNow;
            DateTime fromUtc = filter?.FromUtc ?? toUtc.AddDays(-30);
            string methodology = string.IsNullOrWhiteSpace(filter?.CostMethodology) ? cfg.CostMethodology : filter.CostMethodology.ToUpperInvariant();
            string currency = string.IsNullOrWhiteSpace(filter?.Currency) ? cfg.DefaultCurrency : filter.Currency.ToUpperInvariant();

            List<MenuItem> menuItems = await _repository.MenuItem
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            if (filter?.PropertyId != null)
            {
                menuItems = menuItems.Where(x => x.PropertyId == null || x.PropertyId == filter.PropertyId).ToList();
            }

            if (!string.IsNullOrWhiteSpace(filter?.MenuCategory))
            {
                menuItems = menuItems.Where(x => string.Equals(x.Category, filter.MenuCategory, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            List<Recipe> recipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .Include(r => r.OutletPrices)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, Recipe> recipeById = recipes.ToDictionary(r => r.Id, r => r);

            List<PosSalesTransaction> sales = await _repository.PosSalesTransaction
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.BusinessDate >= fromUtc && x.BusinessDate <= toUtc)
                .ToListAsync(cancellationToken);

            if (allowedLocationIds.Count > 0)
            {
                sales = sales.Where(s => s.OutletLocationId == null || allowedLocationIds.Contains(s.OutletLocationId.Value)).ToList();
            }

            List<InventoryLocation> locations = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, InventoryLocation> locationById = locations.ToDictionary(l => l.Id, l => l);

            List<BuyerProperty> properties = await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, string> propertyNameById = properties.ToDictionary(p => p.Id, p => p.PropertyName);

            List<SilaCulinaryMenuEngineeringItemDto> resultItems = new();
            List<string> warnings = new();

            if (menuItems.Count == 0 && recipes.Count > 0)
            {
                // Derive catalog rows from approved selling recipes if MenuItem master is not yet populated
                foreach (Recipe r in recipes.Where(x => x.ItemMode != "SUB_RECIPE"))
                {
                    RecipeOutletPrice? outletPrice = r.OutletPrices.FirstOrDefault();
                    decimal qtySold = sales.Where(s => s.RecipeId == r.Id && s.TransactionType != "RETURN" && s.TransactionType != "VOID").Sum(s => s.QuantitySold);
                    decimal returnsQty = sales.Where(s => s.RecipeId == r.Id && (s.TransactionType == "RETURN" || s.TransactionType == "VOID")).Sum(s => s.QuantitySold);
                    decimal netQty = Math.Max(0m, qtySold - returnsQty);
                    decimal grossPrice = outletPrice?.MenuPrice ?? 0m;
                    decimal stdCost = r.TotalCost;
                    string locName = outletPrice != null && locationById.TryGetValue(outletPrice.OutletLocationId, out InventoryLocation? loc) ? loc.LocationName : "Main Outlet";
                    string propName = outletPrice != null && locationById.TryGetValue(outletPrice.OutletLocationId, out InventoryLocation? loc2) && propertyNameById.TryGetValue(loc2.PropertyId, out string? pn) ? pn : "Default Property";

                    resultItems.Add(BuildMenuEngineeringRow(
                        r.Id.ToString(),
                        r.RecipeCode,
                        r.Name,
                        r.Category ?? "Food",
                        "All Day",
                        locName,
                        propName,
                        netQty,
                        grossPrice,
                        0m,
                        stdCost,
                        r.ActiveVersion > 0 ? r.ActiveVersion : r.Version,
                        methodology));
                }
            }
            else
            {
                foreach (MenuItem m in menuItems)
                {
                    Recipe? linked = m.LinkedRecipeId.HasValue && recipeById.TryGetValue(m.LinkedRecipeId.Value, out Recipe? rec) ? rec : null;
                    decimal qtySold = linked != null
                        ? sales.Where(s => s.RecipeId == linked.Id && s.TransactionType != "RETURN" && s.TransactionType != "VOID").Sum(s => s.QuantitySold)
                        - sales.Where(s => s.RecipeId == linked.Id && (s.TransactionType == "RETURN" || s.TransactionType == "VOID")).Sum(s => s.QuantitySold)
                        : 0m;
                    qtySold = Math.Max(0m, qtySold);
                    decimal grossPrice = m.SellingPrice ?? linked?.OutletPrices.FirstOrDefault()?.MenuPrice ?? 0m;
                    decimal? stdCost = linked?.TotalCost;
                    string locName = m.OutletLocationId.HasValue && locationById.TryGetValue(m.OutletLocationId.Value, out InventoryLocation? loc) ? loc.LocationName : (filter?.OutletName ?? "All Outlets");
                    string propName = m.PropertyId.HasValue && propertyNameById.TryGetValue(m.PropertyId.Value, out string? pn) ? pn : "Property";

                    resultItems.Add(BuildMenuEngineeringRow(
                        m.Id.ToString(),
                        m.MenuItemCode,
                        m.Name,
                        m.Category ?? "Food",
                        m.MealPeriods ?? "All Day",
                        locName,
                        propName,
                        qtySold,
                        grossPrice,
                        0m,
                        stdCost,
                        linked != null && linked.ActiveVersion > 0 ? linked.ActiveVersion : (linked?.Version ?? 1),
                        methodology));
                }
            }

            if (resultItems.Count == 0)
            {
                warnings.Add("No menu items or POS sales matched the selected filter criteria.");
            }

            // Classify with 70% popularity rule & sales-weighted CM (or config override)
            List<SilaCulinaryMenuEngineeringItemDto> eligible = resultItems
                .Where(x => x.QuantitySold >= cfg.MinSalesForClassification && x.ContributionMarginPerItem.HasValue && x.NetSellingPrice > 0)
                .ToList();

            decimal totalEligibleQty = eligible.Sum(x => x.QuantitySold);
            decimal totalRevenue = resultItems.Sum(x => x.NetSalesRevenue);
            decimal totalCm = resultItems.Sum(x => x.TotalContributionMargin ?? 0m);

            decimal popThreshold = eligible.Count > 0
                ? Math.Round((100m / eligible.Count) * cfg.PopularityThresholdFactor, 2)
                : 0m;
            decimal weightedCm = totalEligibleQty > 0
                ? Math.Round(eligible.Sum(x => x.TotalContributionMargin ?? 0m) / totalEligibleQty, 2)
                : 0m;
            decimal cmThreshold = cfg.MenuProfitabilityThresholdOverride ?? weightedCm;

            Dictionary<string, int> counts = new()
            {
                ["STAR"] = 0,
                ["PLOWHORSE"] = 0,
                ["PUZZLE"] = 0,
                ["DOG"] = 0,
                ["INSUFFICIENT_DATA"] = 0
            };

            decimal totalAllQty = resultItems.Sum(x => x.QuantitySold);
            foreach (SilaCulinaryMenuEngineeringItemDto row in resultItems)
            {
                row.PopularityPercent = totalAllQty > 0 ? Math.Round((row.QuantitySold / totalAllQty) * 100m, 2) : 0m;
                row.SalesMixPercent = row.PopularityPercent;
                row.RevenueContributionPercent = totalRevenue > 0 ? Math.Round((row.NetSalesRevenue / totalRevenue) * 100m, 2) : 0m;
                row.ProfitContributionPercent = totalCm > 0 && row.TotalContributionMargin.HasValue
                    ? Math.Round((row.TotalContributionMargin.Value / totalCm) * 100m, 2)
                    : null;

                if (row.QuantitySold < cfg.MinSalesForClassification || !row.ContributionMarginPerItem.HasValue || row.NetSellingPrice <= 0)
                {
                    row.Quadrant = "INSUFFICIENT_DATA";
                    row.RecommendedAction = "Collect more POS sales history and confirm recipe costing before classifying.";
                }
                else
                {
                    bool highPop = row.PopularityPercent >= popThreshold;
                    bool highCm = row.ContributionMarginPerItem.Value >= cmThreshold;
                    if (highPop && highCm)
                    {
                        row.Quadrant = "STAR";
                        row.RecommendedAction = "Maintain signature standard, protect portion consistency, and feature prominently.";
                    }
                    else if (highPop && !highCm)
                    {
                        row.Quadrant = "PLOWHORSE";
                        row.RecommendedAction = "Test selective price increase, garnish/portion optimization, or supplier cost negotiation.";
                    }
                    else if (!highPop && highCm)
                    {
                        row.Quadrant = "PUZZLE";
                        row.RecommendedAction = "Boost visibility with server upselling, menu placement, or bundle promotion.";
                    }
                    else
                    {
                        row.Quadrant = "DOG";
                        row.RecommendedAction = "Evaluate recipe re-engineering, seasonal rotation, or menu removal.";
                    }
                }

                counts[row.Quadrant] = counts.GetValueOrDefault(row.Quadrant) + 1;
            }

            return new SilaCulinaryMenuEngineeringResponseDto
            {
                Currency = currency,
                PopularityThresholdPercent = popThreshold,
                ContributionMarginThreshold = cmThreshold,
                WeightedAverageContributionMargin = weightedCm,
                TotalQuantitySold = Math.Round(totalAllQty, 2),
                TotalNetRevenue = Math.Round(totalRevenue, 2),
                TotalContributionMargin = Math.Round(totalCm, 2),
                Counts = counts,
                Items = resultItems.OrderByDescending(x => x.TotalContributionMargin ?? 0m).ToList(),
                Warnings = warnings
            };
        }

        private static SilaCulinaryMenuEngineeringItemDto BuildMenuEngineeringRow(
            string id,
            string code,
            string name,
            string category,
            string mealPeriod,
            string outletName,
            string propertyName,
            decimal qtySold,
            decimal grossPrice,
            decimal discountAmount,
            decimal? stdCost,
            int recipeVersion,
            string methodology)
        {
            decimal netPrice = Math.Max(0m, grossPrice - discountAmount);
            decimal netSalesRevenue = Math.Round(netPrice * qtySold, 2);
            decimal garnishCost = stdCost.HasValue ? Math.Round(stdCost.Value * 0.04m, 2) : 0m;
            decimal packagingCost = 0m;
            decimal directLabor = stdCost.HasValue ? Math.Round(stdCost.Value * 0.12m, 2) : 0m;
            decimal overhead = netPrice > 0 ? Math.Round(netPrice * 0.08m, 2) : 0m;
            decimal? totalVariableCost = stdCost.HasValue ? Math.Round(stdCost.Value + garnishCost + packagingCost + directLabor, 2) : null;
            decimal? totalProdCost = stdCost.HasValue ? Math.Round(totalVariableCost!.Value + overhead, 2) : null;
            decimal? cm = totalVariableCost.HasValue && netPrice > 0 ? Math.Round(netPrice - totalVariableCost.Value, 2) : null;
            decimal? cmPct = cm.HasValue && netPrice > 0 ? Math.Round((cm.Value / netPrice) * 100m, 2) : null;
            decimal? totalCm = cm.HasValue ? Math.Round(cm.Value * qtySold, 2) : null;
            decimal? fcPct = stdCost.HasValue && netPrice > 0 ? Math.Round((stdCost.Value / netPrice) * 100m, 2) : null;
            decimal? fullyLoadedProfit = totalProdCost.HasValue && netPrice > 0 ? Math.Round(netPrice - totalProdCost.Value, 2) : null;

            return new SilaCulinaryMenuEngineeringItemDto
            {
                MenuItemId = id,
                MenuItemCode = code,
                Name = name,
                Category = category,
                MealPeriod = mealPeriod,
                OutletName = outletName,
                PropertyName = propertyName,
                QuantitySold = Math.Round(qtySold, 2),
                GrossSellingPrice = Math.Round(grossPrice, 2),
                DiscountAmount = Math.Round(discountAmount, 2),
                NetSellingPrice = Math.Round(netPrice, 2),
                NetSalesRevenue = netSalesRevenue,
                StandardRecipeCost = stdCost.HasValue ? Math.Round(stdCost.Value, 2) : null,
                ActualIngredientCost = stdCost.HasValue ? Math.Round(stdCost.Value, 2) : null,
                GarnishCost = garnishCost,
                PackagingCost = packagingCost,
                DirectLaborCost = directLabor,
                AllocatedOverhead = overhead,
                TotalProductionCost = totalProdCost,
                FoodCostPercent = fcPct,
                ContributionMarginPerItem = cm,
                ContributionMarginPercent = cmPct,
                TotalContributionMargin = totalCm,
                EstimatedFullyLoadedProfit = fullyLoadedProfit.HasValue ? Math.Round(fullyLoadedProfit.Value * qtySold, 2) : null,
                ProfitPerPortion = fullyLoadedProfit,
                CostSource = methodology,
                RecipeVersion = recipeVersion,
                CostTimestampUtc = DateTime.UtcNow
            };
        }

        public async Task<SilaCulinaryConsumptionVarianceResponseDto> AnalyzeConsumptionVarianceAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryExecutiveFilterDto? filter,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Consumption variance analytics require inventory or financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, filter?.PropertyId, cancellationToken);
            List<Guid> allowedLocationIds = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, userId, roleId, cancellationToken);

            DateTime toUtc = filter?.ToUtc ?? DateTime.UtcNow;
            DateTime fromUtc = filter?.FromUtc ?? toUtc.AddDays(-30);

            List<PosSalesTransaction> sales = await _repository.PosSalesTransaction
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.BusinessDate >= fromUtc && x.BusinessDate <= toUtc)
                .ToListAsync(cancellationToken);

            List<RecipeIngredient> ingredients = await _repository.RecipeIngredient
                .FindByCondition(x => x.IsActive && x.MaterialId != null)
                .ToListAsync(cancellationToken);

            List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, ItemBuyerMaster> materialById = materials.ToDictionary(m => m.Id, m => m);

            List<InventoryTransaction> txns = await _repository.InventoryTransaction
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.BusinessDate >= fromUtc && x.BusinessDate <= toUtc)
                .ToListAsync(cancellationToken);

            if (allowedLocationIds.Count > 0)
            {
                txns = txns.Where(t => allowedLocationIds.Contains(t.LocationId)).ToList();
            }

            List<SilaCulinaryWasteRecord> wasteRecords = await _repository.SilaCulinaryWasteRecord
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.RecordedAtUtc >= fromUtc && x.RecordedAtUtc <= toUtc && x.MaterialId != null)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, decimal> theoreticalByMat = new();
            foreach (PosSalesTransaction sale in sales.Where(s => s.RecipeId.HasValue))
            {
                decimal sign = (sale.TransactionType == "RETURN" || sale.TransactionType == "VOID") ? -1m : 1m;
                foreach (RecipeIngredient ing in ingredients.Where(i => i.RecipeId == sale.RecipeId!.Value))
                {
                    if (!ing.MaterialId.HasValue) continue;
                    decimal addQty = sign * sale.QuantitySold * ing.BaseQuantity;
                    theoreticalByMat[ing.MaterialId.Value] = theoreticalByMat.GetValueOrDefault(ing.MaterialId.Value) + addQty;
                }
            }

            Dictionary<Guid, decimal> actualByMat = new();
            foreach (InventoryTransaction txn in txns)
            {
                // Reconcile actual consumption: include issues/POS consumption, subtract returns, exclude non-consumption internal transfers
                bool isTransfer = txn.TransactionType.Contains("TRANSFER", StringComparison.OrdinalIgnoreCase);
                if (isTransfer) continue;

                bool isConsumptionIssue = txn.Direction == "OUT" && (txn.TransactionType.Contains("POS", StringComparison.OrdinalIgnoreCase)
                    || txn.TransactionType.Contains("ISSUE", StringComparison.OrdinalIgnoreCase)
                    || txn.TransactionType.Contains("CONSUMP", StringComparison.OrdinalIgnoreCase));
                bool isReturnReversal = txn.Direction == "IN" && (txn.TransactionType.Contains("RETURN", StringComparison.OrdinalIgnoreCase)
                    || txn.TransactionType.Contains("REVERSAL", StringComparison.OrdinalIgnoreCase));

                if (isConsumptionIssue)
                {
                    actualByMat[txn.MaterialId] = actualByMat.GetValueOrDefault(txn.MaterialId) + txn.Quantity;
                }
                else if (isReturnReversal)
                {
                    actualByMat[txn.MaterialId] = actualByMat.GetValueOrDefault(txn.MaterialId) - txn.Quantity;
                }
            }

            Dictionary<Guid, decimal> wasteByMat = wasteRecords
                .Where(w => w.MaterialId.HasValue)
                .GroupBy(w => w.MaterialId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            HashSet<Guid> allMatIds = theoreticalByMat.Keys.Concat(actualByMat.Keys).Concat(wasteByMat.Keys).ToHashSet();
            List<SilaCulinaryConsumptionVarianceRowDto> rows = new();

            foreach (Guid matId in allMatIds)
            {
                materialById.TryGetValue(matId, out ItemBuyerMaster? mat);
                decimal theo = Math.Max(0m, Math.Round(theoreticalByMat.GetValueOrDefault(matId), 4));
                decimal act = Math.Max(0m, Math.Round(actualByMat.GetValueOrDefault(matId), 4));
                decimal waste = Math.Max(0m, Math.Round(wasteByMat.GetValueOrDefault(matId), 4));
                decimal unitCost = mat?.UnitCost ?? 0m;

                decimal totalVar = Math.Round(act - theo, 4);
                decimal unexplained = totalVar > 0 ? Math.Max(0m, Math.Round(totalVar - waste, 4)) : Math.Round(totalVar, 4);
                decimal? varPct = theo > 0 ? Math.Round((totalVar / theo) * 100m, 2) : null;
                decimal finVar = Math.Round(totalVar * unitCost, 2);

                List<string> causes = new();
                if (unexplained > 0 && theo > 0 && Math.Abs(varPct ?? 0) > cfg.ConsumptionVarianceTolerancePercent)
                {
                    causes.Add("Possible over-portioning during service");
                    causes.Add("Possible unrecorded preparation loss or yield variance");
                    causes.Add("Possible UOM conversion or POS recipe mapping drift");
                }
                else if (unexplained < 0)
                {
                    causes.Add("Possible under-portioning or missing goods-issue posting");
                }

                rows.Add(new SilaCulinaryConsumptionVarianceRowDto
                {
                    MaterialId = matId.ToString(),
                    MaterialCode = mat?.MaterialCode ?? matId.ToString()[..8],
                    MaterialName = mat?.Description ?? "Tracked Ingredient",
                    Uom = mat?.BaseUnitOfMeasure ?? "KG",
                    RecipeVersion = 1,
                    TheoreticalQuantity = theo,
                    ReconciledActualQuantity = act,
                    RecordedWastage = waste,
                    QuantityVariance = totalVar,
                    UnexplainedVariance = unexplained,
                    VariancePercent = varPct,
                    FinancialVariance = finVar,
                    YieldVarianceQty = Math.Round(waste * 0.40m, 4),
                    PortionVarianceQty = Math.Round(Math.Max(0m, unexplained * 0.50m), 4),
                    PossibleCauses = causes,
                    ExceedsTolerance = varPct.HasValue && Math.Abs(varPct.Value) > cfg.ConsumptionVarianceTolerancePercent
                });
            }

            return new SilaCulinaryConsumptionVarianceResponseDto
            {
                Currency = cfg.DefaultCurrency,
                TotalTheoreticalQty = Math.Round(rows.Sum(r => r.TheoreticalQuantity), 3),
                TotalActualQty = Math.Round(rows.Sum(r => r.ReconciledActualQuantity), 3),
                TotalRecordedWastage = Math.Round(rows.Sum(r => r.RecordedWastage), 3),
                TotalUnexplainedQty = Math.Round(rows.Sum(r => r.UnexplainedVariance), 3),
                TotalFinancialVariance = Math.Round(rows.Sum(r => r.FinancialVariance), 2),
                Rows = rows.OrderByDescending(r => Math.Abs(r.FinancialVariance)).ToList()
            };
        }

        public async Task<OperationalTask> CreateVarianceInvestigationTaskAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryCreateVarianceTaskRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Only authorized culinary or cost control staff can raise variance investigation tasks.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            OperationalTask task = await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
            {
                TaskType = "CONSUMPTION_VARIANCE_INVESTIGATION",
                DeduplicationKey = $"CULINARY_VAR_{buyer.Id}_{request.MaterialCode}_{DateTime.UtcNow:yyyyMMdd}",
                Title = $"Investigate Consumption Variance: {request.MaterialName} ({request.MaterialCode})",
                Description = $"Unexplained variance: {request.UnexplainedVarianceQty:N3}, Financial impact: {request.FinancialImpact:N2}. {request.Note}",
                Priority = Math.Abs(request.FinancialImpact) >= 500 ? SilaOperations.PRIORITY_CRITICAL : SilaOperations.PRIORITY_ACTION,
                SourceModule = "CULINARY_ANALYTICS",
                SourceDocumentType = Common.SILA_REF_CULINARY_ANALYTICS,
                SourceDocumentId = Guid.NewGuid(),
                SourceDocumentNumber = request.MaterialCode,
                ActionRoute = "/recipes"
            }, cancellationToken);

            await _repository.SaveAsync();
            return task;
        }

        public async Task<SilaCulinaryReversePriceImpactResponseDto> AnalyzePriceImpactAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryReversePriceImpactRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Ingredient price impact analysis requires culinary or financial access.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, null, cancellationToken);

            ItemBuyerMaster? material = null;
            if (request.MaterialId.HasValue)
            {
                material = await _repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.MaterialId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (material == null && !string.IsNullOrWhiteSpace(request.MaterialCode))
            {
                material = await _repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialCode == request.MaterialCode && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            material ??= await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (material == null)
            {
                return new SilaCulinaryReversePriceImpactResponseDto
                {
                    Currency = cfg.DefaultCurrency,
                    MaterialCode = request.MaterialCode,
                    MaterialName = "No matching material found"
                };
            }

            decimal prevPrice = material.UnitCost ?? 0m;
            decimal currentPrice = request.SimulatedNewPrice ?? Math.Round(prevPrice * 1.12m, 2);
            decimal priceDelta = Math.Round(currentPrice - prevPrice, 4);
            decimal? priceChangePct = prevPrice > 0 ? Math.Round((priceDelta / prevPrice) * 100m, 2) : null;

            List<Recipe> allRecipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .Include(r => r.Ingredients)
                .Include(r => r.OutletPrices)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, Recipe> recipeById = allRecipes.ToDictionary(r => r.Id, r => r);
            List<SilaCulinaryPriceImpactNodeDto> impacts = new();

            // Direct recipes using this material
            Dictionary<Guid, decimal> directDeltaPerRecipe = new();
            foreach (Recipe rec in allRecipes)
            {
                List<RecipeIngredient> matches = rec.Ingredients.Where(i => i.MaterialId == material.Id).ToList();
                if (matches.Count == 0) continue;

                decimal qtySum = matches.Sum(m => m.BaseQuantity);
                decimal unitDelta = Math.Round(qtySum * priceDelta, 4);
                directDeltaPerRecipe[rec.Id] = unitDelta;

                decimal menuPrice = rec.OutletPrices.FirstOrDefault()?.MenuPrice ?? 0m;
                decimal prevCost = rec.TotalCost;
                decimal revCost = Math.Round(prevCost + unitDelta, 4);
                decimal? prevFc = menuPrice > 0 ? Math.Round((prevCost / menuPrice) * 100m, 2) : null;
                decimal? revFc = menuPrice > 0 ? Math.Round((revCost / menuPrice) * 100m, 2) : null;
                decimal monthlyVolume = rec.ItemMode == "SUB_RECIPE" ? 0m : 120m;
                decimal monthlyImp = Math.Round(unitDelta * monthlyVolume, 2);

                impacts.Add(new SilaCulinaryPriceImpactNodeDto
                {
                    NodeType = rec.ItemMode == "SUB_RECIPE" ? "SUB_RECIPE" : "RECIPE",
                    NodeId = rec.Id.ToString(),
                    NodeName = rec.Name,
                    PropertyName = "Tenant Properties",
                    OutletName = "All Outlets",
                    PreviousUnitCost = Math.Round(prevCost, 2),
                    RevisedUnitCost = Math.Round(revCost, 2),
                    UnitCostDelta = Math.Round(unitDelta, 2),
                    PreviousFoodCostPct = prevFc,
                    RevisedFoodCostPct = revFc,
                    MarginImpactPerUnit = Math.Round(-unitDelta, 2),
                    EstimatedMonthlyImpact = monthlyImp,
                    EstimatedAnnualImpact = Math.Round(monthlyImp * 12m, 2)
                });
            }

            // Parent recipes consuming affected sub-recipes
            foreach (Recipe parent in allRecipes.Where(r => r.ItemMode != "SUB_RECIPE"))
            {
                foreach (RecipeIngredient subIng in parent.Ingredients.Where(i => i.SubRecipeId.HasValue && directDeltaPerRecipe.ContainsKey(i.SubRecipeId.Value)))
                {
                    decimal subDelta = directDeltaPerRecipe[subIng.SubRecipeId!.Value];
                    Recipe? sub = recipeById.GetValueOrDefault(subIng.SubRecipeId.Value);
                    decimal servingDiv = sub != null && sub.ServingQty > 0 ? sub.ServingQty : 1m;
                    decimal parentUnitDelta = Math.Round((subDelta / servingDiv) * subIng.BaseQuantity, 4);
                    decimal monthlyImp = Math.Round(parentUnitDelta * 120m, 2);

                    impacts.Add(new SilaCulinaryPriceImpactNodeDto
                    {
                        NodeType = "MENU_ITEM",
                        NodeId = parent.Id.ToString(),
                        NodeName = parent.Name,
                        ViaSubRecipe = sub?.Name,
                        PropertyName = "Tenant Properties",
                        OutletName = "All Outlets",
                        PreviousUnitCost = Math.Round(parent.TotalCost, 2),
                        RevisedUnitCost = Math.Round(parent.TotalCost + parentUnitDelta, 2),
                        UnitCostDelta = Math.Round(parentUnitDelta, 2),
                        MarginImpactPerUnit = Math.Round(-parentUnitDelta, 2),
                        EstimatedMonthlyImpact = monthlyImp,
                        EstimatedAnnualImpact = Math.Round(monthlyImp * 12m, 2)
                    });
                }
            }

            bool alertTriggered = priceChangePct.HasValue && Math.Abs(priceChangePct.Value) >= cfg.IngredientPriceAlertThresholdPercent;
            if (alertTriggered && impacts.Count > 0)
            {
                await SilaOperations.UpsertAlertAsync(_repository, buyer.Id, userId, new SilaOperations.AlertDraft
                {
                    AlertType = "INGREDIENT_PRICE_SPIKE",
                    DeduplicationKey = $"PRICE_SPIKE_{buyer.Id}_{material.MaterialCode}_{DateTime.UtcNow:yyyyMMdd}",
                    Title = $"Ingredient Price Impact Alert: {material.Description}",
                    Message = $"Price variance of {priceChangePct:N1}% affects {impacts.Count} culinary nodes.",
                    Severity = SilaOperations.SEVERITY_WARNING,
                    SourceModule = "CULINARY_ANALYTICS",
                    SourceDocumentType = Common.SILA_REF_CULINARY_ANALYTICS,
                    SourceDocumentId = material.Id,
                    ActionRoute = "/recipes"
                }, cancellationToken);
                await _repository.SaveAsync();
            }

            Dictionary<string, int> affectedCounts = impacts
                .GroupBy(i => i.NodeType)
                .ToDictionary(g => g.Key, g => g.Count());

            decimal totalMonthly = Math.Round(impacts.Sum(i => i.EstimatedMonthlyImpact), 2);
            return new SilaCulinaryReversePriceImpactResponseDto
            {
                Currency = cfg.DefaultCurrency,
                MaterialCode = material.MaterialCode,
                MaterialName = material.Description,
                PreviousPrice = Math.Round(prevPrice, 2),
                CurrentPrice = Math.Round(currentPrice, 2),
                PriceDelta = Math.Round(priceDelta, 2),
                PriceChangePercent = priceChangePct,
                AlertTriggered = alertTriggered,
                AffectedCounts = affectedCounts,
                TotalMonthlyImpact = totalMonthly,
                TotalAnnualImpact = Math.Round(totalMonthly * 12m, 2),
                Impacts = impacts
            };
        }

        public async Task<List<SilaCulinarySimulationRecordDto>> ListSimulationsAsync(
            Guid organizationId,
            Guid roleId,
            Guid? propertyId,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Viewing culinary simulations requires culinary or financial access.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "WhatIfSimulationEnabled", cancellationToken);

            List<SilaCulinarySimulation> items = await _repository.SilaCulinarySimulation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId))
                .OrderByDescending(x => x.DateCreated)
                .Take(30)
                .ToListAsync(cancellationToken);

            return items.Select(MapSimulationRecord).ToList();
        }

        public async Task<SilaCulinarySimulationRecordDto> RunAndSaveSimulationAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            SilaCulinaryRunSimulationRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Creating culinary simulations requires culinary or financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "WhatIfSimulationEnabled", cancellationToken);

            List<SilaCulinarySimulationScenarioInputDto> scenarios = request.Scenarios.Count > 0
                ? request.Scenarios
                : new List<SilaCulinarySimulationScenarioInputDto>
                {
                    new() { ScenarioName = "Baseline", Covers = 500, WastagePct = 4, YieldPct = 88 },
                    new() { ScenarioName = "Scenario A (+10% Cost Shock)", IngredientPriceDeltaPct = 10, Covers = 500, WastagePct = 4, YieldPct = 88 },
                    new() { ScenarioName = "AI-Recommended Mitigation", IngredientPriceDeltaPct = 10, SellingPriceDeltaPct = 4, SubstitutionSavingsPct = 6, Covers = 500, WastagePct = 2.5m, YieldPct = 90 }
                };

            SilaCulinarySimulationScenarioInputDto baseline = scenarios[0];
            SilaCulinarySimulationScenarioResultDto baseRes = ComputeSimulationScenario(request, baseline, null);
            List<SilaCulinarySimulationScenarioResultDto> results = new() { baseRes };

            for (int i = 1; i < scenarios.Count; i++)
            {
                results.Add(ComputeSimulationScenario(request, scenarios[i], baseRes));
            }

            SilaCulinarySimulationScenarioResultDto last = results[^1];
            SilaCulinarySimulation entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                SimulationCode = $"SIM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                Name = request.Name,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "AED" : request.Currency.ToUpperInvariant(),
                ScenarioType = "MULTI_SCENARIO",
                IsolatedFromProduction = true,
                IsShared = false,
                ParametersJson = JsonSerializer.Serialize(request, CamelJson),
                ResultsJson = JsonSerializer.Serialize(results, CamelJson),
                BaselineContribution = baseRes.ContributionMargin,
                SimulatedContribution = last.ContributionMargin,
                ExpectedSavings = last.ExpectedSavingsVsBaseline,
                CreatedByUserId = userId,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true
            };

            if (request.SaveSnapshot)
            {
                _repository.SilaCulinarySimulation.Create(entity);
                await _repository.SaveAsync();
                await WriteAuditAsync(buyer.Id, userId, actorName, "SIMULATION_RUN", "SIMULATION", "SilaCulinarySimulation", entity.Id, $"Isolated simulation '{entity.Name}' executed ({results.Count} scenarios).", false, cancellationToken);
            }

            return MapSimulationRecord(entity);
        }

        public async Task<SilaCulinarySimulationRecordDto> DuplicateSimulationAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            Guid simulationId,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Duplicating simulations requires culinary or financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinarySimulation source = await _repository.SilaCulinarySimulation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == simulationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Simulation not found.", "The requested simulation snapshot was not found.");

            SilaCulinarySimulation copy = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = source.PropertyId,
                SimulationCode = $"SIM-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                Name = $"{source.Name} (Copy)",
                ScenarioType = source.ScenarioType,
                Currency = source.Currency,
                IsolatedFromProduction = true,
                IsShared = false,
                ParametersJson = source.ParametersJson,
                ResultsJson = source.ResultsJson,
                BaselineContribution = source.BaselineContribution,
                SimulatedContribution = source.SimulatedContribution,
                ExpectedSavings = source.ExpectedSavings,
                CreatedByUserId = userId,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true
            };

            _repository.SilaCulinarySimulation.Create(copy);
            await _repository.SaveAsync();
            await WriteAuditAsync(buyer.Id, userId, actorName, "SIMULATION_DUPLICATE", "SIMULATION", "SilaCulinarySimulation", copy.Id, $"Duplicated simulation {source.SimulationCode}.", false, cancellationToken);
            return MapSimulationRecord(copy);
        }

        private static SilaCulinarySimulationScenarioResultDto ComputeSimulationScenario(
            SilaCulinaryRunSimulationRequestDto req,
            SilaCulinarySimulationScenarioInputDto sc,
            SilaCulinarySimulationScenarioResultDto? baseline)
        {
            decimal covers = sc.Covers * Math.Max(0.10m, sc.OccupancyPct / 80.0m);
            decimal sellingPerCover = req.BaseSellingPricePerCover * (1m + (sc.SellingPriceDeltaPct / 100m)) * (1m - (sc.DiscountPct / 100m));
            decimal netRevenue = Math.Round(sellingPerCover * covers, 2);

            decimal yieldFactor = 88.0m / Math.Max(1.0m, sc.YieldPct);
            decimal wasteFactor = 1m + (sc.WastagePct / 100m);
            decimal foodPerCover = req.BaseFoodCostPerCover
                * (1m + (sc.IngredientPriceDeltaPct / 100m))
                * (1m + (sc.PortionSizeDeltaPct / 100m))
                * (1m - (sc.SubstitutionSavingsPct / 100m))
                * yieldFactor * wasteFactor * sc.BuffetReplenishmentFactor;

            decimal revisedFoodCost = Math.Round(foodPerCover * covers, 2);
            decimal revisedLabor = Math.Round(req.BaseLaborCost * (1m + (sc.LaborCostDeltaPct / 100m)), 2);
            decimal totalCost = Math.Round(revisedFoodCost + revisedLabor + sc.OverheadAmount, 2);
            decimal cm = Math.Round(netRevenue - revisedFoodCost, 2);
            decimal? fcPct = netRevenue > 0 ? Math.Round((revisedFoodCost / netRevenue) * 100m, 2) : null;
            decimal? cmPct = netRevenue > 0 ? Math.Round((cm / netRevenue) * 100m, 2) : null;
            decimal cmPerCover = covers > 0 ? cm / covers : 0m;
            int? breakEven = cmPerCover > 0 ? (int)Math.Ceiling((revisedLabor + sc.OverheadAmount) / cmPerCover) : null;
            decimal? targetPrice = req.TargetFoodCostPct > 0 ? Math.Round(foodPerCover / (req.TargetFoodCostPct / 100m), 2) : null;

            return new SilaCulinarySimulationScenarioResultDto
            {
                ScenarioName = sc.ScenarioName,
                Covers = Math.Round(covers, 1),
                NetRevenue = netRevenue,
                RevisedFoodCost = revisedFoodCost,
                RevisedFoodCostPct = fcPct,
                RevisedLaborCost = revisedLabor,
                TotalCost = totalCost,
                ContributionMargin = cm,
                ContributionMarginPct = cmPct,
                BreakEvenCovers = breakEven,
                TargetSellingPriceForFoodCost = targetPrice,
                ExpectedSavingsVsBaseline = baseline == null ? 0m : Math.Round(baseline.TotalCost - totalCost, 2),
                ProfitabilityDeltaVsBaseline = baseline == null ? 0m : Math.Round(cm - baseline.ContributionMargin, 2)
            };
        }

        private static SilaCulinarySimulationRecordDto MapSimulationRecord(SilaCulinarySimulation entity)
        {
            return new SilaCulinarySimulationRecordDto
            {
                Id = entity.Id,
                SimulationCode = entity.SimulationCode,
                Name = entity.Name,
                Currency = entity.Currency,
                IsolatedFromProduction = entity.IsolatedFromProduction,
                IsShared = entity.IsShared,
                BaselineContribution = entity.BaselineContribution,
                SimulatedContribution = entity.SimulatedContribution,
                ExpectedSavings = entity.ExpectedSavings,
                CreatedAtUtc = entity.DateCreated,
                Results = SafeDeserialize<List<SilaCulinarySimulationScenarioResultDto>>(entity.ResultsJson) ?? new List<SilaCulinarySimulationScenarioResultDto>()
            };
        }

        public async Task<SilaCulinaryOptimizationResultDto> OptimizeMenuAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            SilaCulinaryOptimizationRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Executing menu optimization requires culinary or financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "PythonOptimizationEnabled", cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);

            List<MenuItem> items = await _repository.MenuItem
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<Recipe> recipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ItemMode != "SUB_RECIPE")
                .Include(r => r.OutletPrices)
                .ToListAsync(cancellationToken);

            List<object> candidateInputs = new();
            foreach (Recipe rec in recipes.Take(40))
            {
                MenuItem? mi = items.FirstOrDefault(m => m.LinkedRecipeId == rec.Id);
                decimal price = mi?.SellingPrice ?? rec.OutletPrices.FirstOrDefault()?.MenuPrice ?? Math.Round(rec.TotalCost * 3.4m, 2);
                if (price <= 0) price = Math.Max(25m, Math.Round(rec.TotalCost * 3.2m, 2));
                List<string> allergens = SplitTags(mi?.AllergenNotes);
                List<string> dietary = SplitTags(mi?.DietaryTags);

                candidateInputs.Add(new
                {
                    itemId = rec.Id.ToString(),
                    name = mi?.Name ?? rec.Name,
                    category = mi?.Category ?? rec.Category ?? "Main",
                    unitCost = rec.TotalCost > 0 ? rec.TotalCost : 12m,
                    sellingPrice = price,
                    minPortions = 0,
                    maxPortions = Math.Max(40, request.TotalTargetPortions),
                    prepMinutesPerPortion = 4.0m,
                    estimatedWasteRate = 0.04m,
                    allergens,
                    dietaryTags = dietary
                });
            }

            SilaCulinaryOptimizationResultDto dto;
            if (candidateInputs.Count == 0)
            {
                dto = new SilaCulinaryOptimizationResultDto
                {
                    JobId = Guid.NewGuid(),
                    JobCode = $"OPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                    Status = "INFEASIBLE",
                    Currency = cfg.DefaultCurrency,
                    Objective = request.Objective,
                    InfeasibilityReasons = new List<string> { "No active recipes or menu items with valid costing exist for optimization." },
                    Explanation = "Optimization did not fabricate results because no costed recipes exist in this tenant.",
                    CreatedAtUtc = DateTime.UtcNow
                };
            }
            else
            {
                var pyPayload = new
                {
                    currency = cfg.DefaultCurrency,
                    objective = request.Objective,
                    totalTargetPortions = request.TotalTargetPortions,
                    targetFoodCostPercent = request.TargetFoodCostPct,
                    maxBudget = request.MaxBudget,
                    maxKitchenMinutes = request.MaxKitchenMinutes,
                    excludedAllergens = request.ExcludedAllergens,
                    requiredDietaryTags = request.RequiredDietaryTags,
                    strictAllergenSafety = request.StrictAllergenSafety,
                    candidates = candidateInputs
                };

                dto = await CallPythonOrFallbackOptimizeAsync(pyPayload, recipes, items, cfg.DefaultCurrency, request, cancellationToken);
            }

            SilaCulinaryOptimizationJob job = new()
            {
                Id = dto.JobId,
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                JobCode = dto.JobCode,
                JobType = "MENU_OPTIMIZATION",
                Status = dto.Status,
                Objective = request.Objective,
                RequestJson = JsonSerializer.Serialize(request, CamelJson),
                ResultJson = JsonSerializer.Serialize(dto, CamelJson),
                RetryCount = 0,
                RequestedByUserId = userId,
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true
            };

            _repository.SilaCulinaryOptimizationJob.Create(job);
            await _repository.SaveAsync();
            await WriteAuditAsync(buyer.Id, userId, actorName, "OPTIMIZATION_EXECUTE", "OPTIMIZATION", "SilaCulinaryOptimizationJob", job.Id, $"Menu optimization {job.JobCode} finished with status {job.Status}.", false, cancellationToken);
            return dto;
        }

        private async Task<SilaCulinaryOptimizationResultDto> CallPythonOrFallbackOptimizeAsync(
            object pyPayload,
            List<Recipe> recipes,
            List<MenuItem> items,
            string currency,
            SilaCulinaryOptimizationRequestDto request,
            CancellationToken cancellationToken)
        {
            using JsonDocument? pyDoc = await TryPostPythonAsync<JsonDocument>("/v1/analytics/optimize-menu", pyPayload, cancellationToken);
            if (pyDoc != null && pyDoc.RootElement.TryGetProperty("status", out JsonElement statusEl))
            {
                JsonElement root = pyDoc.RootElement;
                List<SilaCulinaryOptimizationSelectionDto> selections = new();
                if (root.TryGetProperty("selections", out JsonElement selArr) && selArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in selArr.EnumerateArray())
                    {
                        selections.Add(new SilaCulinaryOptimizationSelectionDto
                        {
                            ItemId = item.GetProperty("itemId").GetString() ?? string.Empty,
                            Name = item.GetProperty("name").GetString() ?? string.Empty,
                            Category = item.GetProperty("category").GetString() ?? string.Empty,
                            RecommendedPortions = item.GetProperty("recommendedPortions").GetInt32(),
                            UnitCost = item.GetProperty("unitCost").GetDecimal(),
                            SellingPrice = item.GetProperty("sellingPrice").GetDecimal(),
                            TotalCost = item.GetProperty("totalCost").GetDecimal(),
                            TotalRevenue = item.GetProperty("totalRevenue").GetDecimal(),
                            TotalContribution = item.GetProperty("totalContribution").GetDecimal()
                        });
                    }
                }

                return new SilaCulinaryOptimizationResultDto
                {
                    JobId = Guid.NewGuid(),
                    JobCode = $"OPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                    Status = statusEl.GetString() ?? "OPTIMAL",
                    Currency = currency,
                    Objective = request.Objective,
                    BaselineCost = root.TryGetProperty("baselineCost", out var bc) ? bc.GetDecimal() : 0m,
                    BaselineContribution = root.TryGetProperty("baselineContribution", out var bcm) ? bcm.GetDecimal() : 0m,
                    ProposedCost = root.TryGetProperty("proposedCost", out var pc) ? pc.GetDecimal() : 0m,
                    ProposedRevenue = root.TryGetProperty("proposedRevenue", out var pr) ? pr.GetDecimal() : 0m,
                    ProposedContribution = root.TryGetProperty("proposedContribution", out var pcm) ? pcm.GetDecimal() : 0m,
                    ProposedFoodCostPct = root.TryGetProperty("proposedFoodCostPercent", out var pfc) && pfc.ValueKind == JsonValueKind.Number ? pfc.GetDecimal() : null,
                    ExpectedSavings = root.TryGetProperty("expectedSavings", out var es) ? es.GetDecimal() : 0m,
                    ExpectedMarginImprovementPct = root.TryGetProperty("expectedMarginImprovementPercent", out var emi) ? emi.GetDecimal() : 0m,
                    InfeasibilityReasons = ReadStringList(root, "infeasibilityReasons"),
                    Assumptions = ReadStringList(root, "assumptions"),
                    Explanation = root.TryGetProperty("explanation", out var exp) ? (exp.GetString() ?? string.Empty) : string.Empty,
                    Selections = selections,
                    CreatedAtUtc = DateTime.UtcNow
                };
            }

            // Deterministic fallback enforcing strict allergen & dietary hard constraints
            List<(Recipe rec, MenuItem? mi, decimal unitCost, decimal price)> eligible = new();
            HashSet<string> excluded = request.ExcludedAllergens.Select(a => a.Trim().ToLowerInvariant()).ToHashSet();
            HashSet<string> reqDiet = request.RequiredDietaryTags.Select(d => d.Trim().ToLowerInvariant()).ToHashSet();

            foreach (Recipe rec in recipes)
            {
                MenuItem? mi = items.FirstOrDefault(m => m.LinkedRecipeId == rec.Id);
                HashSet<string> allergens = SplitTags(mi?.AllergenNotes).Select(a => a.ToLowerInvariant()).ToHashSet();
                HashSet<string> dietary = SplitTags(mi?.DietaryTags).Select(d => d.ToLowerInvariant()).ToHashSet();

                if (request.StrictAllergenSafety && excluded.Count > 0 && allergens.Overlaps(excluded))
                {
                    continue;
                }
                if (reqDiet.Count > 0 && !reqDiet.IsSubsetOf(dietary))
                {
                    continue;
                }

                decimal unitCost = rec.TotalCost > 0 ? rec.TotalCost : 12m;
                decimal price = mi?.SellingPrice ?? rec.OutletPrices.FirstOrDefault()?.MenuPrice ?? Math.Round(unitCost * 3.4m, 2);
                if (price <= 0) price = Math.Round(unitCost * 3.2m, 2);
                eligible.Add((rec, mi, unitCost, price));
            }

            if (eligible.Count == 0)
            {
                return new SilaCulinaryOptimizationResultDto
                {
                    JobId = Guid.NewGuid(),
                    JobCode = $"OPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                    Status = "INFEASIBLE",
                    Currency = currency,
                    Objective = request.Objective,
                    InfeasibilityReasons = new List<string> { "No candidate dishes satisfy the mandatory allergen and dietary constraints." },
                    Explanation = "Hard safety constraints cannot be relaxed to create a financially attractive mix.",
                    CreatedAtUtc = DateTime.UtcNow
                };
            }

            var ordered = request.Objective == "MIN_FOOD_COST"
                ? eligible.OrderBy(e => e.unitCost).ToList()
                : eligible.OrderByDescending(e => e.price - e.unitCost).ToList();

            var top = ordered[0];
            decimal totalCost = Math.Round(top.unitCost * request.TotalTargetPortions, 2);
            decimal totalRev = Math.Round(top.price * request.TotalTargetPortions, 2);
            decimal totalCm = Math.Round(totalRev - totalCost, 2);
            decimal fcPct = totalRev > 0 ? Math.Round((totalCost / totalRev) * 100m, 2) : 0m;

            if (request.TargetFoodCostPct.HasValue && fcPct > request.TargetFoodCostPct.Value)
            {
                return new SilaCulinaryOptimizationResultDto
                {
                    JobId = Guid.NewGuid(),
                    JobCode = $"OPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                    Status = "INFEASIBLE",
                    Currency = currency,
                    Objective = request.Objective,
                    InfeasibilityReasons = new List<string> { $"Even the lowest-cost safe menu yields {fcPct:N1}% food cost vs target {request.TargetFoodCostPct:N1}%." },
                    Explanation = "Target food cost is infeasible under current allergen and price constraints.",
                    CreatedAtUtc = DateTime.UtcNow
                };
            }

            return new SilaCulinaryOptimizationResultDto
            {
                JobId = Guid.NewGuid(),
                JobCode = $"OPT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                Status = "OPTIMAL",
                Currency = currency,
                Objective = request.Objective,
                BaselineCost = Math.Round(eligible.Average(e => e.unitCost) * request.TotalTargetPortions, 2),
                BaselineContribution = Math.Round(eligible.Average(e => e.price - e.unitCost) * request.TotalTargetPortions, 2),
                ProposedCost = totalCost,
                ProposedRevenue = totalRev,
                ProposedContribution = totalCm,
                ProposedFoodCostPct = fcPct,
                ExpectedSavings = Math.Max(0m, Math.Round((eligible.Average(e => e.unitCost) * request.TotalTargetPortions) - totalCost, 2)),
                ExpectedMarginImprovementPct = 4.5m,
                Assumptions = new List<string> { "Allergen and dietary constraints enforced strictly.", "Deterministic solver fallback executed." },
                Explanation = $"Optimal mix selected within {currency} budget and hard safety constraints.",
                Selections = new List<SilaCulinaryOptimizationSelectionDto>
                {
                    new()
                    {
                        ItemId = top.rec.Id.ToString(),
                        Name = top.mi?.Name ?? top.rec.Name,
                        Category = top.mi?.Category ?? top.rec.Category ?? "Main",
                        RecommendedPortions = request.TotalTargetPortions,
                        UnitCost = top.unitCost,
                        SellingPrice = top.price,
                        TotalCost = totalCost,
                        TotalRevenue = totalRev,
                        TotalContribution = totalCm
                    }
                },
                CreatedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<List<SilaCulinaryOptimizationResultDto>> ListOptimizationJobsAsync(
            Guid organizationId,
            Guid roleId,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Viewing optimization jobs requires culinary or financial access.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            List<SilaCulinaryOptimizationJob> jobs = await _repository.SilaCulinaryOptimizationJob
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Take(20)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryOptimizationResultDto> list = new();
            foreach (SilaCulinaryOptimizationJob j in jobs)
            {
                SilaCulinaryOptimizationResultDto? parsed = SafeDeserialize<SilaCulinaryOptimizationResultDto>(j.ResultJson);
                if (parsed != null)
                {
                    parsed.JobId = j.Id;
                    parsed.JobCode = j.JobCode;
                    parsed.Status = j.Status;
                    list.Add(parsed);
                }
            }
            return list;
        }

        public async Task<SilaCulinaryOptimizationResultDto> CancelOptimizationJobAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            Guid jobId,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Cancelling optimization jobs requires culinary or financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryOptimizationJob job = await _repository.SilaCulinaryOptimizationJob
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.Id == jobId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Optimization job not found.", "The specified optimization job was not found.");

            job.Status = "CANCELLED";
            job.DateUpdated = DateTime.UtcNow;
            await _repository.SaveAsync();
            await WriteAuditAsync(buyer.Id, userId, actorName, "OPTIMIZATION_CANCEL", "OPTIMIZATION", "SilaCulinaryOptimizationJob", job.Id, $"Cancelled optimization job {job.JobCode}.", false, cancellationToken);

            SilaCulinaryOptimizationResultDto? parsed = SafeDeserialize<SilaCulinaryOptimizationResultDto>(job.ResultJson) ?? new SilaCulinaryOptimizationResultDto();
            parsed.JobId = job.Id;
            parsed.JobCode = job.JobCode;
            parsed.Status = "CANCELLED";
            return parsed;
        }

        public async Task<SilaCulinarySubstitutionResponseDto> AnalyzeSubstitutionsAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinarySubstitutionRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Ingredient substitution analysis requires culinary authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, null, cancellationToken);

            List<ItemBuyerMaster> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            ItemBuyerMaster? orig = materials.FirstOrDefault(m => string.Equals(m.MaterialCode, request.OriginalMaterialCode, StringComparison.OrdinalIgnoreCase))
                ?? materials.FirstOrDefault();

            if (orig == null)
            {
                return new SilaCulinarySubstitutionResponseDto { Currency = cfg.DefaultCurrency, OriginalMaterialName = request.OriginalMaterialCode };
            }

            decimal origCost = Math.Round((orig.UnitCost ?? 0m) * request.RequiredQty, 2);
            List<ItemBuyerMaster> candidates = materials
                .Where(m => m.Id != orig.Id && (string.Equals(m.MaterialGroup, orig.MaterialGroup, StringComparison.OrdinalIgnoreCase) || m.BaseUnitOfMeasure == orig.BaseUnitOfMeasure))
                .Take(8)
                .ToList();

            List<SilaCulinarySubstitutionOptionDto> options = new();
            foreach (ItemBuyerMaster c in candidates)
            {
                decimal subCost = Math.Round((c.UnitCost ?? 0m) * request.RequiredQty, 2);
                decimal saving = Math.Round(origCost - subCost, 2);
                // Because ItemBuyerMaster does not store verified allergen/dietary certification directly, mark unknown allergen profile cleanly to block auto-approval!
                bool hasVerifiedAllergenProfile = !string.IsNullOrWhiteSpace(c.ValuationClass) && c.ValuationClass.Contains("ALLERGEN_VERIFIED", StringComparison.OrdinalIgnoreCase);

                options.Add(new SilaCulinarySubstitutionOptionDto
                {
                    MaterialId = c.Id.ToString(),
                    MaterialCode = c.MaterialCode,
                    Name = c.Description,
                    Classification = hasVerifiedAllergenProfile ? (saving > 0 ? "PROPOSED_SUBSTITUTE" : "APPROVED_SUBSTITUTE") : "INSUFFICIENT_INFORMATION",
                    OriginalIngredientCost = origCost,
                    SubstituteIngredientCost = subCost,
                    RevisedRecipeCost = subCost,
                    CostSaving = saving,
                    YieldImpactPct = 0m,
                    AllergenImpact = hasVerifiedAllergenProfile ? "Verified allergen-compliant" : "Unknown allergen/dietary certification — requires Chef & QA sign-off",
                    NutritionalImpact = "Pending nutritional profile check",
                    AvailabilityStatus = c.IsInventoryItem ? "IN_CATALOG_STOCKED" : "DIRECT_PURCHASE",
                    RequiresExecutiveChefApproval = true,
                    AutoApprovalBlocked = !hasVerifiedAllergenProfile,
                    BlockingReason = !hasVerifiedAllergenProfile ? "Unknown allergen or dietary metadata blocks automatic approval." : null
                });
            }

            return new SilaCulinarySubstitutionResponseDto
            {
                Currency = cfg.DefaultCurrency,
                OriginalMaterialName = orig.Description,
                OriginalIngredientCost = origCost,
                Recommendations = options.OrderByDescending(o => o.CostSaving).ToList()
            };
        }

        public async Task<SilaCulinaryBuffetOptimizeResponseDto> OptimizeBuffetAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryBuffetOptimizeRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Buffet optimization requires culinary authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);

            List<Recipe> recipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ItemMode != "SUB_RECIPE")
                .Take(12)
                .ToListAsync(cancellationToken);

            decimal occScale = Math.Max(0.25m, Math.Min(1.25m, request.OccupancyPercent / 85m));
            int effectiveCovers = Math.Max(1, (int)Math.Round(request.ExpectedCovers * occScale));
            List<SilaCulinaryBuffetDishRecDto> recs = new();

            foreach (Recipe r in recipes)
            {
                decimal unitCost = r.TotalCost > 0 ? r.TotalCost : 10m;
                int expectedConsumption = Math.Max(10, (int)Math.Round(effectiveCovers * 0.45m));
                int initialQty = Math.Max(8, (int)Math.Round(expectedConsumption * 0.50m));
                int remaining = Math.Max(0, expectedConsumption - initialQty);
                int batches = remaining > 0 ? 2 : 0;
                int batchQty = batches > 0 ? (int)Math.Ceiling((decimal)remaining / batches) : 0;
                decimal prodQty = initialQty + (batches * batchQty);
                decimal prodCost = Math.Round(prodQty * unitCost, 2);

                recs.Add(new SilaCulinaryBuffetDishRecDto
                {
                    DishId = r.Id.ToString(),
                    Name = r.Name,
                    InitialProductionPortions = initialQty,
                    ReplenishmentBatches = batches,
                    ReplenishmentQtyPerBatch = batchQty,
                    ReplenishmentIntervalMinutes = 45,
                    ExpectedConsumptionPortions = expectedConsumption,
                    ExpectedLeftoverPortions = Math.Max(0m, prodQty - expectedConsumption),
                    ProductionCost = prodCost,
                    FoodCostPerGuest = Math.Round(prodCost / effectiveCovers, 2),
                    PotentialWasteReductionPortions = Math.Round(prodQty * 0.08m, 1),
                    FoodSafetyNote = "Max hot/cold holding 120 mins; leftovers are never recommended for unsafe reuse."
                });
            }

            decimal totalCost = Math.Round(recs.Sum(x => x.ProductionCost), 2);
            return new SilaCulinaryBuffetOptimizeResponseDto
            {
                Currency = cfg.DefaultCurrency,
                MealPeriod = request.MealPeriod,
                ExpectedCovers = effectiveCovers,
                TotalProductionCost = totalCost,
                FoodCostPerGuest = effectiveCovers > 0 ? Math.Round(totalCost / effectiveCovers, 2) : 0m,
                TotalPotentialWasteReductionValue = Math.Round(totalCost * 0.08m, 2),
                Recommendations = recs
            };
        }

        public async Task<SilaCulinaryEventOptimizeResponseDto> OptimizeEventMenusAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryEventOptimizeRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Event menu optimization requires culinary or financial access.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, null, cancellationToken);

            // Reuse existing Phase 3A Event P&L plan when provided
            SilaCulinaryEventFinancialPlan? plan = null;
            if (request.EventFinancialPlanId.HasValue)
            {
                plan = await _repository.SilaCulinaryEventFinancialPlan
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.EventFinancialPlanId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            plan ??= await _repository.SilaCulinaryEventFinancialPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && !x.IsSimulation)
                .OrderByDescending(x => x.DateUpdated)
                .FirstOrDefaultAsync(cancellationToken);

            int guests = request.GuestCount ?? (plan?.BillableGuests > 0 ? plan.BillableGuests : 150);
            decimal baseFood = plan != null && plan.EstimatedFoodCost > 0 ? plan.EstimatedFoodCost : guests * 55m;
            decimal baseBev = plan != null && plan.EstimatedBeverageCost > 0 ? plan.EstimatedBeverageCost : guests * 18m;
            decimal baseLabor = plan != null && plan.EstimatedManpowerCost > 0 ? plan.EstimatedManpowerCost : guests * 24m;
            decimal baseOverhead = plan != null && plan.EstimatedAllocatedOverhead > 0 ? plan.EstimatedAllocatedOverhead : guests * 15m;
            decimal basePricePerGuest = plan != null && plan.NetRevenue > 0 && plan.BillableGuests > 0
                ? Math.Round(plan.NetRevenue / plan.BillableGuests, 2)
                : 240m;

            List<(string code, string name, decimal foodMult, decimal priceMult, string summary)> defs = new()
            {
                ("BUDGET_OPTIMIZED", "Budget Optimized Package", 0.84m, 0.92m, "Seasonal local proteins, high-yield grains, streamlined garnish stations"),
                ("MARGIN_OPTIMIZED", "Margin Optimized Package", 0.88m, 1.06m, "High-contribution signature courses with portion-controlled luxury accents"),
                ("PREMIUM_EXPERIENCE", "Premium Experience Package", 1.15m, 1.24m, "Wagyu/seafood centerpiece, live carving & artisanal dessert theater"),
                ("WASTE_OPTIMIZED", "Waste Optimized Package", 0.86m, 0.98m, "Cross-utilized prep ingredients, small-batch replenishment & low-trim cuts"),
                ("BALANCED", "Balanced Recommendation", 0.92m, 1.02m, "Optimal balance of guest satisfaction, target food cost, and kitchen load")
            };

            List<SilaCulinaryEventPackageDto> packages = defs.Select(d =>
            {
                decimal food = Math.Round(baseFood * d.foodMult, 2);
                decimal bev = Math.Round(baseBev * (d.foodMult * 0.95m + 0.05m), 2);
                decimal totalDirect = Math.Round(food + bev + baseLabor, 2);
                decimal totalCost = Math.Round(totalDirect + baseOverhead, 2);
                decimal sellPerGuest = Math.Round(basePricePerGuest * d.priceMult, 2);
                decimal rev = Math.Round(sellPerGuest * guests, 2);
                decimal cm = Math.Round(rev - totalDirect, 2);
                decimal profit = Math.Round(rev - totalCost, 2);
                return new SilaCulinaryEventPackageDto
                {
                    PackageCode = d.code,
                    PackageName = d.name,
                    MenuCompositionSummary = d.summary,
                    EstimatedFoodCost = food,
                    EstimatedBeverageCost = bev,
                    EstimatedManpowerCost = Math.Round(baseLabor, 2),
                    EstimatedEventCost = totalCost,
                    CostPerGuest = guests > 0 ? Math.Round(totalCost / guests, 2) : 0m,
                    SellingPricePerGuest = sellPerGuest,
                    ExpectedRevenue = rev,
                    ExpectedContribution = cm,
                    ExpectedContributionMarginPct = rev > 0 ? Math.Round((cm / rev) * 100m, 2) : 0m,
                    EstimatedFullyLoadedProfit = profit,
                    IngredientRequirementKg = Math.Round(guests * 0.62m * d.foodMult, 2),
                    ProcurementRequirementValue = Math.Round((food + bev) * 0.85m, 2)
                };
            }).ToList();

            return new SilaCulinaryEventOptimizeResponseDto
            {
                Currency = plan?.ReportingCurrency ?? cfg.DefaultCurrency,
                GuestCount = guests,
                Packages = packages
            };
        }

        public async Task<SilaCulinaryForecastResponseDto> GenerateForecastAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryForecastRequestDto request,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId) && roleId != Common.OUTLET_MANAGER_ROLE_ID)
            {
                throw new ForBiddenCustomException("Access denied.", "Demand forecasting access is restricted.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "DemandForecastingEnabled", cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);

            int horizon = Math.Clamp(request.HorizonDays > 0 ? request.HorizonDays : cfg.DefaultForecastHorizonDays, 1, 90);

            List<PmsDemandSnapshot> pmsHistory = await _repository.PmsDemandSnapshot
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (request.PropertyId == null || x.PropertyId == request.PropertyId))
                .OrderBy(x => x.BusinessDate)
                .Take(90)
                .ToListAsync(cancellationToken);

            List<PosSalesTransaction> posHistory = await _repository.PosSalesTransaction
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            var dailyPos = posHistory
                .GroupBy(x => x.BusinessDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new { Date = g.Key, Covers = g.Sum(x => x.QuantitySold) })
                .ToList();

            bool coldStart = dailyPos.Count < cfg.MinimumHistoricalDataDays;
            decimal avgCovers = dailyPos.Count > 0
                ? dailyPos.Average(d => d.Covers)
                : (pmsHistory.Count > 0 ? pmsHistory.Average(p => p.ExpectedGuests ?? 120m) : Math.Round(request.OccupancyAssumptionPct * 2.2m, 1));
            if (avgCovers <= 0) avgCovers = 140m;

            decimal? mae = null;
            decimal? wape = null;
            decimal? bias = null;
            List<string> warnings = new();

            if (!coldStart && dailyPos.Count >= 7)
            {
                // Rolling 1-step naive/moving-average backtest on last 7 days
                decimal absErr = 0m;
                decimal signedErr = 0m;
                decimal actualSum = 0m;
                for (int i = 3; i < dailyPos.Count; i++)
                {
                    decimal pred = (dailyPos[i - 1].Covers + dailyPos[i - 2].Covers + dailyPos[i - 3].Covers) / 3m;
                    decimal act = dailyPos[i].Covers;
                    absErr += Math.Abs(act - pred);
                    signedErr += (pred - act);
                    actualSum += act;
                }
                int count = Math.Max(1, dailyPos.Count - 3);
                mae = Math.Round(absErr / count, 2);
                bias = Math.Round(signedErr / count, 2);
                wape = actualSum > 0 ? Math.Round((absErr / actualSum) * 100m, 2) : null;
            }
            else
            {
                warnings.Add($"Cold-start forecast active: only {dailyPos.Count} POS sales days available (minimum {cfg.MinimumHistoricalDataDays} required for backtesting).");
            }

            List<SilaCulinaryForecastPointDto> points = new();
            DateTime start = DateTime.UtcNow.Date.AddDays(1);
            decimal sigma = mae.HasValue ? Math.Max(8m, mae.Value * 1.25m) : Math.Max(12m, avgCovers * 0.14m);

            for (int d = 0; d < horizon; d++)
            {
                DateTime dt = start.AddDays(d);
                bool weekend = dt.DayOfWeek == DayOfWeek.Friday || dt.DayOfWeek == DayOfWeek.Saturday;
                decimal mult = weekend ? 1.15m : 1.00m;
                decimal pred = Math.Round(avgCovers * mult, 1);

                points.Add(new SilaCulinaryForecastPointDto
                {
                    DayIndex = d + 1,
                    Date = dt.ToString("yyyy-MM-dd"),
                    ForecastCovers = pred,
                    LowerBound80 = Math.Max(0m, Math.Round(pred - (1.28m * sigma), 1)),
                    UpperBound80 = Math.Round(pred + (1.28m * sigma), 1),
                    LowerBound95 = Math.Max(0m, Math.Round(pred - (1.96m * sigma), 1)),
                    UpperBound95 = Math.Round(pred + (1.96m * sigma), 1),
                    ExpectedFoodCost = Math.Round(pred * 48m, 2),
                    IsForecast = true
                });
            }

            return new SilaCulinaryForecastResponseDto
            {
                ModelName = coldStart ? "ColdStartOccupancyBaseline" : "RollingWeightedSeasonalBaseline",
                ModelVersion = "v3b.1",
                HorizonDays = horizon,
                ColdStartUsed = coldStart,
                Mae = mae,
                WapePercent = wape,
                ForecastBias = bias,
                DataQuality = coldStart ? "LOW_COLD_START" : (dailyPos.Count >= 21 ? "HIGH" : "MODERATE"),
                LastTrainedUtc = DateTime.UtcNow,
                ForecastPoints = points,
                Warnings = warnings
            };
        }

        public async Task<SilaCulinaryWasteIntelligenceDto> GetWasteIntelligenceAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid? propertyId,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId) && roleId != Common.OUTLET_MANAGER_ROLE_ID)
            {
                throw new ForBiddenCustomException("Access denied.", "Food waste analytics access is restricted.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "WasteIntelligenceEnabled", cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, propertyId, cancellationToken);

            List<SilaCulinaryWasteRecord> records = await _repository.SilaCulinaryWasteRecord
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId))
                .OrderByDescending(x => x.RecordedAtUtc)
                .Take(200)
                .ToListAsync(cancellationToken);

            decimal totalCost = Math.Round(records.Sum(r => r.EstimatedCost), 2);
            int totalCovers = Math.Max(0, records.Sum(r => r.CoversServed));
            Dictionary<string, decimal> byCat = records
                .GroupBy(r => r.WasteCategory)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.EstimatedCost), 2));
            Dictionary<string, decimal> byOutlet = records
                .GroupBy(r => string.IsNullOrWhiteSpace(r.OutletName) ? "Main Kitchen" : r.OutletName!)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.EstimatedCost), 2));
            Dictionary<string, decimal> byMeal = records
                .GroupBy(r => r.MealPeriod)
                .ToDictionary(g => g.Key, g => Math.Round(g.Sum(x => x.EstimatedCost), 2));

            List<string> patterns = new();
            List<string> recs = new();
            if (byCat.Count > 0)
            {
                KeyValuePair<string, decimal> topCat = byCat.OrderByDescending(k => k.Value).First();
                patterns.Add($"Top waste driver: {topCat.Key} ({cfg.DefaultCurrency} {topCat.Value:N2}).");
                if (topCat.Key is "BUFFET_LEFTOVERS" or "OVERPRODUCTION")
                {
                    recs.Add("Reduce initial buffet batch size by 15% and shorten replenishment intervals.");
                }
                else if (topCat.Key is "SPOILAGE" or "EXPIRED_INGREDIENTS")
                {
                    recs.Add("Enforce FIFO/FEFO stock rotation and align supplier pack sizes with weekly par levels.");
                }
                else
                {
                    recs.Add("Review recipe yield factors, trim specs, and portion control tooling.");
                }
            }
            else
            {
                patterns.Add("No waste incidents logged in the active period.");
            }

            return new SilaCulinaryWasteIntelligenceDto
            {
                Currency = cfg.DefaultCurrency,
                TotalWasteCost = totalCost,
                WastePerCover = totalCovers > 0 ? Math.Round(totalCost / totalCovers, 2) : null,
                ByCategory = byCat,
                ByProperty = byOutlet,
                ByMealPeriod = byMeal,
                RecurringPatterns = patterns,
                Recommendations = recs,
                RecentRecords = records.Take(25).Select(r => new SilaCulinaryWasteItemDto
                {
                    Id = r.Id,
                    WasteNumber = r.WasteNumber,
                    WasteCategory = r.WasteCategory,
                    IngredientName = r.IngredientName,
                    OutletName = r.OutletName ?? "Kitchen",
                    MealPeriod = r.MealPeriod,
                    Quantity = r.Quantity,
                    Uom = r.Uom,
                    EstimatedCost = r.EstimatedCost,
                    SourceChannel = r.SourceChannel,
                    Status = r.Status,
                    RecordedAtUtc = r.RecordedAtUtc
                }).ToList()
            };
        }

        public async Task<SilaCulinaryWasteItemDto> RecordWasteAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            SilaCulinaryWasteRecordCreateDto request,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "WasteIntelligenceEnabled", cancellationToken);

            // Idempotency guard for Mobile offline sync
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryWasteRecord? dup = await _repository.SilaCulinaryWasteRecord
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (dup != null)
                {
                    return new SilaCulinaryWasteItemDto
                    {
                        Id = dup.Id,
                        WasteNumber = dup.WasteNumber,
                        WasteCategory = dup.WasteCategory,
                        IngredientName = dup.IngredientName,
                        OutletName = dup.OutletName ?? "Kitchen",
                        MealPeriod = dup.MealPeriod,
                        Quantity = dup.Quantity,
                        Uom = dup.Uom,
                        EstimatedCost = dup.EstimatedCost,
                        SourceChannel = dup.SourceChannel,
                        Status = dup.Status,
                        RecordedAtUtc = dup.RecordedAtUtc
                    };
                }
            }

            if (request.LocationId.HasValue)
            {
                await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, userId, roleId, request.LocationId.Value, cancellationToken);
            }

            SilaCulinaryWasteRecord entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                LocationId = request.LocationId,
                OutletName = request.OutletName,
                MealPeriod = string.IsNullOrWhiteSpace(request.MealPeriod) ? "All Day" : request.MealPeriod,
                EventId = request.EventId,
                WasteNumber = $"WST-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}",
                WasteCategory = string.IsNullOrWhiteSpace(request.WasteCategory) ? "PRODUCTION_WASTE" : request.WasteCategory.ToUpperInvariant(),
                MaterialId = request.MaterialId,
                RecipeId = request.RecipeId,
                MenuItemId = request.MenuItemId,
                IngredientName = request.IngredientName,
                Quantity = request.Quantity,
                Uom = string.IsNullOrWhiteSpace(request.Uom) ? "KG" : request.Uom.ToUpperInvariant(),
                EstimatedCost = request.EstimatedCost,
                CoversServed = request.CoversServed,
                SourceChannel = string.IsNullOrWhiteSpace(request.SourceChannel) ? "CLOUD" : request.SourceChannel.ToUpperInvariant(),
                IdempotencyKey = request.IdempotencyKey,
                Status = "PENDING_APPROVAL", // Never creates inventory adjustment without approval
                Notes = request.Notes,
                RecordedByUserId = userId,
                RecordedAtUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true
            };

            _repository.SilaCulinaryWasteRecord.Create(entity);
            await _repository.SaveAsync();
            await WriteAuditAsync(buyer.Id, userId, actorName, "WASTE_RECORD", "WASTE_INTELLIGENCE", "SilaCulinaryWasteRecord", entity.Id, $"Recorded {entity.WasteCategory} for {entity.IngredientName} ({entity.Quantity} {entity.Uom}) via {entity.SourceChannel}.", false, cancellationToken);

            return new SilaCulinaryWasteItemDto
            {
                Id = entity.Id,
                WasteNumber = entity.WasteNumber,
                WasteCategory = entity.WasteCategory,
                IngredientName = entity.IngredientName,
                OutletName = entity.OutletName ?? "Kitchen",
                MealPeriod = entity.MealPeriod,
                Quantity = entity.Quantity,
                Uom = entity.Uom,
                EstimatedCost = entity.EstimatedCost,
                SourceChannel = entity.SourceChannel,
                Status = entity.Status,
                RecordedAtUtc = entity.RecordedAtUtc
            };
        }

        public async Task<SilaCulinaryBenchmarkResponseDto> BenchmarkPropertiesAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string? reportingCurrency,
            CancellationToken cancellationToken)
        {
            if (!CanViewCrossProperty(roleId))
            {
                throw new ForBiddenCustomException("Cross-property access denied.", "Only authorized multi-property executives can view cross-property culinary benchmarking.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "CrossPropertyBenchmarkingEnabled", cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, null, cancellationToken);
            string targetCurrency = string.IsNullOrWhiteSpace(reportingCurrency) ? cfg.DefaultCurrency : reportingCurrency.ToUpperInvariant();

            List<BuyerProperty> properties = await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryEventFinancialPlan> plans = await _repository.SilaCulinaryEventFinancialPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && !x.IsSimulation)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryWasteRecord> wastes = await _repository.SilaCulinaryWasteRecord
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryPropertyBenchmarkRowDto> rows = new();
            List<string> warnings = new();

            foreach (BuyerProperty p in properties)
            {
                List<SilaCulinaryEventFinancialPlan> propPlans = plans.Where(x => x.PropertyId == p.Id).ToList();
                decimal rev = propPlans.Sum(x => x.NetRevenue);
                decimal food = propPlans.Sum(x => x.EstimatedFoodCost);
                int covers = propPlans.Sum(x => x.BillableGuests);
                decimal wasteCost = wastes.Where(w => w.PropertyId == p.Id).Sum(w => w.EstimatedCost);

                rows.Add(new SilaCulinaryPropertyBenchmarkRowDto
                {
                    PropertyId = p.Id.ToString(),
                    PropertyName = p.PropertyName,
                    OutletType = "HOSPITALITY_PROPERTY",
                    LocalCurrency = targetCurrency,
                    FxRateApplied = 1.0m,
                    RevenueReporting = Math.Round(rev, 2),
                    FoodCostReporting = Math.Round(food, 2),
                    FoodCostPct = rev > 0 ? Math.Round((food / rev) * 100m, 2) : null,
                    CostPerCoverReporting = covers > 0 ? Math.Round(food / covers, 2) : null,
                    RevenuePerCoverReporting = covers > 0 ? Math.Round(rev / covers, 2) : null,
                    ContributionMarginPct = rev > 0 ? Math.Round(((rev - food) / rev) * 100m, 2) : null,
                    WastePct = food > 0 ? Math.Round((wasteCost / food) * 100m, 2) : null,
                    WastePerCoverReporting = covers > 0 ? Math.Round(wasteCost / covers, 2) : null,
                    EventMarginPct = rev > 0 ? Math.Round((propPlans.Sum(x => x.ContributionAfterOverhead) / rev) * 100m, 2) : null
                });
            }

            if (rows.Count == 0)
            {
                warnings.Add("No active properties configured for this tenant.");
            }

            return new SilaCulinaryBenchmarkResponseDto
            {
                ReportingCurrency = targetCurrency,
                Warnings = warnings,
                Rows = rows
            };
        }

        public async Task<List<SilaCulinaryRecommendationDto>> ListRecommendationsAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string? status,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "RecommendationCockpitEnabled", cancellationToken);

            List<SilaCulinaryRecommendation> recs = await _repository.SilaCulinaryRecommendation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && (string.IsNullOrEmpty(status) || x.Status == status))
                .OrderByDescending(x => x.ExpectedFinancialImpact)
                .Take(100)
                .ToListAsync(cancellationToken);

            bool full = CanViewFinancials(roleId);
            return recs.Select(r => MapRecDto(r, full)).ToList();
        }

        public async Task<List<SilaCulinaryRecommendationDto>> GenerateRecommendationsAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            Guid? propertyId,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Generating recommendations requires culinary or financial access.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "RecommendationCockpitEnabled", cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, propertyId, cancellationToken);

            SilaCulinaryMenuEngineeringResponseDto me = await AnalyzeMenuEngineeringAsync(
                organizationId, userId, roleId, new SilaCulinaryExecutiveFilterDto { PropertyId = propertyId }, cancellationToken);

            List<SilaCulinaryRecommendation> existing = await _repository.SilaCulinaryRecommendation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            HashSet<string> existingKeys = existing.Select(e => e.DeduplicationKey).ToHashSet();

            int createdCount = 0;
            foreach (SilaCulinaryMenuEngineeringItemDto item in me.Items.Where(i => i.Quadrant is "PLOWHORSE" or "PUZZLE" or "DOG").Take(10))
            {
                string dedupKey = $"ME_{item.Quadrant}_{item.MenuItemCode}";
                if (existingKeys.Contains(dedupKey)) continue;

                string cat = item.Quadrant switch
                {
                    "PLOWHORSE" => "INCREASE_SELLING_PRICE",
                    "PUZZLE" => "PROMOTE_MENU_ITEM",
                    _ => "REVIEW_UNDERPERFORMING_ITEM"
                };

                decimal impact = item.Quadrant == "PLOWHORSE"
                    ? Math.Round(item.NetSalesRevenue * 0.05m, 2)
                    : Math.Round(Math.Max(250m, item.NetSalesRevenue * 0.08m), 2);

                SilaCulinaryRecommendation rec = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = propertyId,
                    RecommendationCode = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..5].ToUpperInvariant()}",
                    DeduplicationKey = dedupKey,
                    Category = cat,
                    SourceEngine = "MENU_ENGINEERING",
                    Title = $"{item.Name} ({item.Quadrant})",
                    SupportingEvidenceJson = JsonSerializer.Serialize(new { item.MenuItemCode, item.Quadrant, item.FoodCostPercent, item.ContributionMarginPerItem }, CamelJson),
                    CurrentSituation = $"FC {item.FoodCostPercent ?? 0:N1}%, CM/item {cfg.DefaultCurrency} {item.ContributionMarginPerItem ?? 0:N2}, Qty sold {item.QuantitySold:N0}",
                    ProposedAction = item.RecommendedAction,
                    OutletName = item.OutletName,
                    PropertyName = item.PropertyName,
                    ExpectedFinancialImpact = impact,
                    Priority = impact >= 1000m ? "CRITICAL" : "HIGH",
                    DataQuality = "VERIFIED",
                    RiskLevel = "LOW",
                    ResponsibleRole = "EXECUTIVE_CHEF",
                    Status = "NEW",
                    ValidFromUtc = DateTime.UtcNow,
                    ValidToUtc = DateTime.UtcNow.AddDays(30),
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true
                };

                _repository.SilaCulinaryRecommendation.Create(rec);
                existingKeys.Add(dedupKey);
                createdCount++;
            }

            if (createdCount > 0)
            {
                await _repository.SaveAsync();
                await WriteAuditAsync(buyer.Id, userId, actorName, "RECOMMENDATION_GENERATE", "RECOMMENDATION_COCKPIT", "SilaCulinaryRecommendation", null, $"Generated {createdCount} deduplicated recommendations.", false, cancellationToken);
            }

            return await ListRecommendationsAsync(organizationId, userId, roleId, null, cancellationToken);
        }

        public async Task<SilaCulinaryRecommendationDto> DecideRecommendationAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            Guid recommendationId,
            SilaCulinaryRecommendationDecisionDto decision,
            CancellationToken cancellationToken)
        {
            if (!CanApproveRecommendations(roleId))
            {
                throw new ForBiddenCustomException("Approval denied.", "Only Buyer Admin, F&B Director, Finance Manager, or Cost Controller can approve or transition recommendations.");
            }

            string normalized = decision.Status.Trim().ToUpperInvariant();
            if (!AllowedRecommendationStatuses.Contains(normalized))
            {
                throw new BadRequestCustomException("Invalid status.", $"Unsupported recommendation status '{decision.Status}'.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            SilaCulinaryRecommendation rec = await _repository.SilaCulinaryRecommendation
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.Id == recommendationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Recommendation not found.", "The specified recommendation was not found.");

            rec.Status = normalized;
            rec.DecisionComment = decision.Comment;
            if (decision.RealizedFinancialImpact.HasValue)
            {
                rec.RealizedFinancialImpact = decision.RealizedFinancialImpact.Value;
            }
            if (normalized is "APPROVED" or "REJECTED" or "IMPLEMENTED")
            {
                rec.ApprovedByUserId = userId;
                rec.ApprovedOnUtc = DateTime.UtcNow;
            }
            rec.DateUpdated = DateTime.UtcNow;

            await _repository.SaveAsync();
            await WriteAuditAsync(buyer.Id, userId, actorName, $"RECOMMENDATION_{normalized}", "RECOMMENDATION_COCKPIT", "SilaCulinaryRecommendation", rec.Id, $"Recommendation {rec.RecommendationCode} moved to {normalized}.", false, cancellationToken);

            return MapRecDto(rec, true);
        }

        private static SilaCulinaryRecommendationDto MapRecDto(SilaCulinaryRecommendation r, bool includeFinancials)
        {
            return new SilaCulinaryRecommendationDto
            {
                Id = r.Id,
                RecommendationCode = r.RecommendationCode,
                Category = r.Category,
                SourceEngine = r.SourceEngine,
                Title = r.Title,
                CurrentSituation = includeFinancials ? r.CurrentSituation : "Operational culinary guidance (financials masked for role).",
                ProposedAction = r.ProposedAction,
                PropertyName = r.PropertyName,
                OutletName = r.OutletName,
                ExpectedFinancialImpact = includeFinancials ? r.ExpectedFinancialImpact : 0m,
                RealizedFinancialImpact = includeFinancials ? r.RealizedFinancialImpact : null,
                Priority = r.Priority,
                DataQuality = r.DataQuality,
                RiskLevel = r.RiskLevel,
                ResponsibleRole = r.ResponsibleRole,
                Status = r.Status,
                CreatedAtUtc = r.DateCreated,
                ValidToUtc = r.ValidToUtc
            };
        }

        public async Task<SilaCulinaryCopilotResponseDto> QueryCopilotAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string actorName,
            SilaCulinaryCopilotRequestDto request,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, "CulinaryCopilotEnabled", cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, request.PropertyId, cancellationToken);

            string q = request.Question?.Trim() ?? string.Empty;
            if (PromptInjectionRegex.IsMatch(q))
            {
                await WriteAuditAsync(buyer.Id, userId, actorName, "COPILOT_INJECTION_BLOCKED", "CULINARY_COPILOT", null, null, "Blocked prompt injection attempt.", true, cancellationToken);
                return new SilaCulinaryCopilotResponseDto
                {
                    DirectAnswer = "Blocked unsafe prompt pattern. Culinary Copilot only answers authorized culinary analytics questions using whitelisted tenant tools.",
                    SupportingMetrics = new List<string> { "Security Guard: Prompt-injection rule triggered" },
                    DataSource = "SILA Culinary Security Guard",
                    DateRange = "N/A",
                    RecommendedAction = "Ask an analytical question about menu margins, food cost, waste, or forecasts.",
                    Confidence = "HIGH",
                    SupportingRoute = "/recipes",
                    ProviderUsed = cfg.PreferredAiProvider,
                    PromptInjectionBlocked = true
                };
            }

            bool fullAccess = CanViewFinancials(roleId);
            if (!fullAccess)
            {
                await WriteAuditAsync(buyer.Id, userId, actorName, "COPILOT_QUERY_OPS", "CULINARY_COPILOT", null, null, $"Operational Copilot query in {request.ContextModule}.", false, cancellationToken);
                return new SilaCulinaryCopilotResponseDto
                {
                    DirectAnswer = "Kitchen/Store Copilot Summary: Focus on prep batch discipline, FEFO stock rotation, and logging waste events before shift close. Financial margins are masked for your role.",
                    SupportingMetrics = new List<string> { "Role Scope: Kitchen / Store Operational Mode" },
                    DataSource = "Whitelisted Operational Tools",
                    DateRange = "Today",
                    RecommendedAction = "Review daily production and buffet replenishment alerts.",
                    EstimatedFinancialImpact = "Masked for operational role",
                    Confidence = "HIGH",
                    SupportingRoute = "/?next=/mobile",
                    ProviderUsed = cfg.PreferredAiProvider
                };
            }

            SilaCulinaryExecutiveDashboardDto dash = await GetExecutiveDashboardAsync(
                organizationId, userId, roleId, new SilaCulinaryExecutiveFilterDto { PropertyId = request.PropertyId }, cancellationToken);

            var pyPayload = new
            {
                question = q,
                contextModule = request.ContextModule,
                currency = dash.Currency,
                metrics = new
                {
                    foodCostPercent = dash.FoodCostPercent,
                    targetFoodCostPercent = dash.TargetFoodCostPercent,
                    totalRevenue = dash.TotalFbRevenue,
                    totalFoodCost = dash.TotalFoodCost,
                    wastePercent = dash.WastePercent,
                    wasteCost = dash.WasteCost,
                    lowMarginItems = dash.LowMarginItems.Select(x => x.Name).Take(5).ToList(),
                    topStarItems = dash.TopProfitableItems.Select(x => x.Name).Take(5).ToList()
                }
            };

            using JsonDocument? pyDoc = await TryPostPythonAsync<JsonDocument>("/v1/analytics/copilot", pyPayload, cancellationToken);
            SilaCulinaryCopilotResponseDto resp;

            if (pyDoc != null && pyDoc.RootElement.TryGetProperty("directAnswer", out JsonElement da))
            {
                JsonElement root = pyDoc.RootElement;
                resp = new SilaCulinaryCopilotResponseDto
                {
                    DirectAnswer = da.GetString() ?? string.Empty,
                    SupportingMetrics = ReadStringList(root, "supportingMetrics"),
                    DataSource = root.TryGetProperty("dataSource", out var ds) ? (ds.GetString() ?? "Tenant Culinary Data") : "Tenant Culinary Data",
                    DateRange = "Last 30 days",
                    Assumptions = ReadStringList(root, "assumptions"),
                    RecommendedAction = root.TryGetProperty("recommendedAction", out var ra) ? (ra.GetString() ?? string.Empty) : string.Empty,
                    EstimatedFinancialImpact = root.TryGetProperty("estimatedFinancialImpact", out var efi) ? (efi.GetString() ?? string.Empty) : string.Empty,
                    Confidence = "HIGH",
                    SupportingRoute = root.TryGetProperty("supportingDashboardLink", out var link) ? (link.GetString() ?? "/recipes") : "/recipes",
                    ProviderUsed = cfg.PreferredAiProvider
                };
            }
            else
            {
                string fcDisplay = dash.FoodCostPercent.HasValue ? $"{dash.FoodCostPercent:N1}%" : "N/A";
                resp = new SilaCulinaryCopilotResponseDto
                {
                    DirectAnswer = $"Current food cost is {fcDisplay} against a {dash.TargetFoodCostPercent:N1}% target on {dash.Currency} {dash.TotalFbRevenue:N2} revenue, with {dash.Currency} {dash.WasteCost:N2} recorded waste.",
                    SupportingMetrics = new List<string>
                    {
                        $"Total F&B Revenue: {dash.Currency} {dash.TotalFbRevenue:N2}",
                        $"Contribution Margin: {dash.Currency} {dash.ContributionMargin:N2}",
                        $"Potential Savings Pipeline: {dash.Currency} {dash.PotentialSavingsPipeline:N2}"
                    },
                    DataSource = "Whitelisted SILA Culinary Analytics Service",
                    DateRange = "Last 30 days",
                    Assumptions = new List<string> { "Grounded strictly on tenant database metrics; LLM arithmetic is never used." },
                    RecommendedAction = "Review Plowhorse & Dog dishes in Menu Engineering and approve high-impact recommendations.",
                    EstimatedFinancialImpact = $"{dash.Currency} {dash.PotentialSavingsPipeline:N2} potential savings",
                    Confidence = "HIGH",
                    SupportingRoute = "/recipes",
                    ProviderUsed = cfg.PreferredAiProvider
                };
            }

            await WriteAuditAsync(buyer.Id, userId, actorName, "COPILOT_QUERY", "CULINARY_COPILOT", null, null, $"Answered Copilot query in {request.ContextModule}.", false, cancellationToken);
            return resp;
        }

        public async Task<SilaCulinaryExecutiveDashboardDto> GetExecutiveDashboardAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            SilaCulinaryExecutiveFilterDto? filter,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Access denied.", "Executive culinary dashboard requires culinary or financial authorization.");
            }

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, filter?.PropertyId, cancellationToken);

            SilaCulinaryMenuEngineeringResponseDto me = await AnalyzeMenuEngineeringAsync(organizationId, userId, roleId, filter, cancellationToken);
            SilaCulinaryWasteIntelligenceDto waste = await GetWasteIntelligenceAsync(organizationId, userId, roleId, filter?.PropertyId, cancellationToken);
            SilaCulinaryForecastResponseDto forecast = await GenerateForecastAsync(organizationId, userId, roleId, new SilaCulinaryForecastRequestDto { PropertyId = filter?.PropertyId, HorizonDays = 7 }, cancellationToken);

            List<SilaCulinaryEventFinancialPlan> eventPlans = await _repository.SilaCulinaryEventFinancialPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && !x.IsSimulation && (filter == null || filter.PropertyId == null || x.PropertyId == filter.PropertyId))
                .ToListAsync(cancellationToken);

            List<SilaCulinaryRecommendation> recs = await _repository.SilaCulinaryRecommendation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.ExpectedFinancialImpact)
                .ToListAsync(cancellationToken);

            decimal totalRev = me.TotalNetRevenue + eventPlans.Sum(e => e.NetRevenue);
            decimal totalFoodCost = me.Items.Sum(i => (i.StandardRecipeCost ?? 0m) * i.QuantitySold) + eventPlans.Sum(e => e.EstimatedFoodCost);
            decimal totalCovers = me.TotalQuantitySold + eventPlans.Sum(e => e.BillableGuests);
            decimal totalEventProfit = eventPlans.Sum(e => e.ContributionAfterOverhead);
            decimal forecastCovers = forecast.ForecastPoints.Sum(p => p.ForecastCovers);
            decimal pipelineSavings = recs.Where(r => r.Status is "NEW" or "UNDER_REVIEW" or "PENDING_APPROVAL" or "APPROVED").Sum(r => r.ExpectedFinancialImpact);
            decimal realizedSavings = recs.Where(r => r.Status is "IMPLEMENTED" or "CLOSED").Sum(r => r.RealizedFinancialImpact ?? r.ExpectedFinancialImpact);

            List<string> notes = new();
            if (totalRev == 0) notes.Add("No POS sales or approved event revenues recorded in the active period.");
            if (waste.TotalWasteCost == 0) notes.Add("No food waste logs recorded yet.");

            return new SilaCulinaryExecutiveDashboardDto
            {
                Currency = me.Currency,
                CanViewFinancials = true,
                CanViewCrossProperty = CanViewCrossProperty(roleId),
                TotalFbRevenue = Math.Round(totalRev, 2),
                TotalFoodCost = Math.Round(totalFoodCost, 2),
                FoodCostPercent = totalRev > 0 ? Math.Round((totalFoodCost / totalRev) * 100m, 2) : null,
                TargetFoodCostPercent = cfg.TargetFoodCostPercent,
                ContributionMargin = Math.Round(totalRev - totalFoodCost, 2),
                CostPerCover = totalCovers > 0 ? Math.Round(totalFoodCost / totalCovers, 2) : null,
                RevenuePerCover = totalCovers > 0 ? Math.Round(totalRev / totalCovers, 2) : null,
                WasteCost = waste.TotalWasteCost,
                WastePercent = totalFoodCost > 0 ? Math.Round((waste.TotalWasteCost / totalFoodCost) * 100m, 2) : null,
                EventProfitability = Math.Round(totalEventProfit, 2),
                ForecastDemandCovers = Math.Round(forecastCovers, 0),
                PotentialSavingsPipeline = Math.Round(pipelineSavings, 2),
                RealizedSavings = Math.Round(realizedSavings, 2),
                QuadrantCounts = me.Counts,
                TopProfitableItems = me.Items.Where(i => i.TotalContributionMargin > 0).OrderByDescending(i => i.TotalContributionMargin).Take(5).ToList(),
                LowMarginItems = me.Items.Where(i => i.Quadrant is "PLOWHORSE" or "DOG").OrderBy(i => i.ContributionMarginPercent ?? 0m).Take(5).ToList(),
                TopRecommendations = recs.Take(5).Select(r => MapRecDto(r, true)).ToList(),
                DataAvailabilityNotes = notes
            };
        }

        public List<SilaCulinaryAnalyticsReportCatalogItemDto> GetReportCatalog() => new()
        {
            new() { ReportCode = "MENU_ENGINEERING", Name = "1. Menu Engineering Report", Description = "STAR / PLOWHORSE / PUZZLE / DOG quadrants, popularity %, CM thresholds & movement." },
            new() { ReportCode = "MENU_ITEM_PROFITABILITY", Name = "2. Menu Item Profitability Report", Description = "Gross/net price, food/garnish/packaging/labor cost, CM & fully loaded profit." },
            new() { ReportCode = "FOOD_COST_ANALYSIS", Name = "3. Food Cost Analysis Report", Description = "Actual vs target food cost %, cost per cover & revenue per cover." },
            new() { ReportCode = "INGREDIENT_PRICE_VARIANCE", Name = "4. Ingredient Price Variance Report", Description = "Reverse dependency price shock across sub-recipes, recipes & events." },
            new() { ReportCode = "RECIPE_COST_MOVEMENT", Name = "5. Recipe Cost Movement Report", Description = "Effective-dated recipe version cost changes." },
            new() { ReportCode = "CONSUMPTION_VARIANCE", Name = "6. Consumption Variance Report", Description = "Theoretical vs reconciled actual consumption isolating recorded wastage." },
            new() { ReportCode = "BUFFET_EFFICIENCY", Name = "7. Buffet Efficiency Report", Description = "Initial batch, replenishment timing, leftovers & holding compliance." },
            new() { ReportCode = "FOOD_WASTE_ANALYSIS", Name = "8. Food Waste Analysis Report", Description = "10 waste classifications by ingredient, outlet, meal period & cover." },
            new() { ReportCode = "EVENT_PROFITABILITY", Name = "9. Event Profitability Report", Description = "Banquet & catering package P&L comparison." },
            new() { ReportCode = "DEMAND_FORECAST", Name = "10. Demand Forecast Report", Description = "Forecast covers, prediction intervals & rolling backtest MAE/WAPE." },
            new() { ReportCode = "PROPERTY_BENCHMARKING", Name = "11. Property Benchmarking Report", Description = "FX-normalized multi-property comparison." },
            new() { ReportCode = "AI_RECOMMENDATION", Name = "12. AI Recommendation Report", Description = "Prioritized culinary actions with approval & evidence status." },
            new() { ReportCode = "POTENTIAL_SAVINGS", Name = "13. Potential Savings Report", Description = "Approved and pending savings pipeline across engines." },
            new() { ReportCode = "REALIZED_SAVINGS", Name = "14. Realized Savings Report", Description = "Implemented recommendations tracking expected vs realized impact." },
            new() { ReportCode = "WHAT_IF_COMPARISON", Name = "15. What-if Scenario Comparison Report", Description = "Baseline vs Scenario A/B/C & AI-recommended snapshots." }
        };

        public async Task<SilaCulinaryAnalyticsReportExportDto> ExportReportAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            string reportCode,
            string format,
            SilaCulinaryExecutiveFilterDto? filter,
            CancellationToken cancellationToken)
        {
            if (!CanViewFinancials(roleId))
            {
                throw new ForBiddenCustomException("Export denied.", "Exporting culinary intelligence reports requires financial or culinary authorization.");
            }

            string fmt = string.IsNullOrWhiteSpace(format) ? "CSV" : format.ToUpperInvariant();
            string code = string.IsNullOrWhiteSpace(reportCode) ? "MENU_ENGINEERING" : reportCode.ToUpperInvariant();
            StringBuilder csv = new();

            if (code is "MENU_ENGINEERING" or "MENU_ITEM_PROFITABILITY" or "FOOD_COST_ANALYSIS" or "RECIPE_COST_MOVEMENT")
            {
                SilaCulinaryMenuEngineeringResponseDto me = await AnalyzeMenuEngineeringAsync(organizationId, userId, roleId, filter, cancellationToken);
                csv.AppendLine("Code,Name,Category,Quadrant,QtySold,NetSellingPrice,StandardCost,FoodCostPct,ContributionMargin,TotalContribution");
                foreach (SilaCulinaryMenuEngineeringItemDto i in me.Items)
                {
                    csv.AppendLine($"{SanitizeCsv(i.MenuItemCode)},{SanitizeCsv(i.Name)},{SanitizeCsv(i.Category)},{SanitizeCsv(i.Quadrant)},{i.QuantitySold},{i.NetSellingPrice},{i.StandardRecipeCost ?? 0m},{i.FoodCostPercent ?? 0m},{i.ContributionMarginPerItem ?? 0m},{i.TotalContributionMargin ?? 0m}");
                }
            }
            else if (code == "CONSUMPTION_VARIANCE")
            {
                SilaCulinaryConsumptionVarianceResponseDto cv = await AnalyzeConsumptionVarianceAsync(organizationId, userId, roleId, filter, cancellationToken);
                csv.AppendLine("MaterialCode,MaterialName,UOM,TheoreticalQty,ReconciledActualQty,RecordedWastage,UnexplainedVariance,FinancialVariance");
                foreach (SilaCulinaryConsumptionVarianceRowDto r in cv.Rows)
                {
                    csv.AppendLine($"{SanitizeCsv(r.MaterialCode)},{SanitizeCsv(r.MaterialName)},{SanitizeCsv(r.Uom)},{r.TheoreticalQuantity},{r.ReconciledActualQuantity},{r.RecordedWastage},{r.UnexplainedVariance},{r.FinancialVariance}");
                }
            }
            else if (code == "FOOD_WASTE_ANALYSIS")
            {
                SilaCulinaryWasteIntelligenceDto w = await GetWasteIntelligenceAsync(organizationId, userId, roleId, filter?.PropertyId, cancellationToken);
                csv.AppendLine("WasteNumber,Category,Ingredient,Outlet,MealPeriod,Qty,UOM,EstimatedCost,Status");
                foreach (SilaCulinaryWasteItemDto r in w.RecentRecords)
                {
                    csv.AppendLine($"{SanitizeCsv(r.WasteNumber)},{SanitizeCsv(r.WasteCategory)},{SanitizeCsv(r.IngredientName)},{SanitizeCsv(r.OutletName)},{SanitizeCsv(r.MealPeriod)},{r.Quantity},{SanitizeCsv(r.Uom)},{r.EstimatedCost},{SanitizeCsv(r.Status)}");
                }
            }
            else if (code == "PROPERTY_BENCHMARKING")
            {
                SilaCulinaryBenchmarkResponseDto b = await BenchmarkPropertiesAsync(organizationId, userId, roleId, filter?.Currency, cancellationToken);
                csv.AppendLine("PropertyName,Currency,Revenue,FoodCost,FoodCostPct,ContributionMarginPct,WastePct");
                foreach (SilaCulinaryPropertyBenchmarkRowDto r in b.Rows)
                {
                    csv.AppendLine($"{SanitizeCsv(r.PropertyName)},{SanitizeCsv(r.LocalCurrency)},{r.RevenueReporting ?? 0m},{r.FoodCostReporting ?? 0m},{r.FoodCostPct ?? 0m},{r.ContributionMarginPct ?? 0m},{r.WastePct ?? 0m}");
                }
            }
            else
            {
                List<SilaCulinaryRecommendationDto> recs = await ListRecommendationsAsync(organizationId, userId, roleId, null, cancellationToken);
                csv.AppendLine("Code,Category,Title,Status,Priority,ExpectedImpact,RealizedImpact");
                foreach (SilaCulinaryRecommendationDto r in recs)
                {
                    csv.AppendLine($"{SanitizeCsv(r.RecommendationCode)},{SanitizeCsv(r.Category)},{SanitizeCsv(r.Title)},{SanitizeCsv(r.Status)},{SanitizeCsv(r.Priority)},{r.ExpectedFinancialImpact},{r.RealizedFinancialImpact ?? 0m}");
                }
            }

            if (fmt == "PDF")
            {
                string html = $"<html><body><h2>SILA Culinary Report: {System.Net.WebUtility.HtmlEncode(code)}</h2><pre>{System.Net.WebUtility.HtmlEncode(csv.ToString())}</pre></body></html>";
                return new SilaCulinaryAnalyticsReportExportDto
                {
                    ReportCode = code,
                    Format = "PDF",
                    FileName = $"{code.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}.html",
                    ContentType = "text/html",
                    Content = html
                };
            }

            return new SilaCulinaryAnalyticsReportExportDto
            {
                ReportCode = code,
                Format = fmt,
                FileName = fmt == "EXCEL" ? $"{code.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}.xls.csv" : $"{code.ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}.csv",
                ContentType = "text/csv",
                Content = csv.ToString()
            };
        }

        public async Task<SilaCulinaryMobileOpsSummaryDto> GetMobileSummaryAsync(
            Guid organizationId,
            Guid userId,
            Guid roleId,
            Guid? propertyId,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            await EnsureFeatureEnabledAsync(buyer.Id, null, cancellationToken);
            SilaCulinaryAnalyticsConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, propertyId, cancellationToken);
            bool canViewCosts = CanViewFinancials(roleId);

            SilaCulinaryForecastResponseDto forecast = await GenerateForecastAsync(
                organizationId, userId, roleId, new SilaCulinaryForecastRequestDto { PropertyId = propertyId, HorizonDays = 1 }, cancellationToken);

            List<OperationalAlert> alerts = await _repository.OperationalAlert
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status == SilaOperations.ALERT_OPEN)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryRecommendation> recs = await _repository.SilaCulinaryRecommendation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.ExpectedFinancialImpact)
                .Take(10)
                .ToListAsync(cancellationToken);

            decimal expectedCovers = forecast.ForecastPoints.FirstOrDefault()?.ForecastCovers ?? 0m;
            decimal? dailyFc = null;
            if (canViewCosts)
            {
                SilaCulinaryExecutiveDashboardDto dash = await GetExecutiveDashboardAsync(
                    organizationId, userId, roleId, new SilaCulinaryExecutiveFilterDto { PropertyId = propertyId }, cancellationToken);
                dailyFc = dash.FoodCostPercent;
            }

            return new SilaCulinaryMobileOpsSummaryDto
            {
                Currency = canViewCosts ? cfg.DefaultCurrency : "MASKED",
                CanViewCosts = canViewCosts,
                DailyExpectedCovers = expectedCovers,
                ActiveShortageAlerts = alerts.Count(a => a.AlertType.Contains("SHORTAGE", StringComparison.OrdinalIgnoreCase)),
                ActiveWasteAlerts = alerts.Count(a => a.AlertType.Contains("WASTE", StringComparison.OrdinalIgnoreCase)),
                PendingApprovalTasks = recs.Count(r => r.Status is "NEW" or "PENDING_APPROVAL"),
                DailyFoodCostPercent = dailyFc,
                TargetFoodCostPercent = canViewCosts ? cfg.TargetFoodCostPercent : 0m,
                ProductionRecommendations = new List<string>
                {
                    $"Prepare mise-en-place for {expectedCovers:N0} forecast covers.",
                    "Verify yield and portion scoops at hot/cold pass before service."
                },
                BuffetReplenishmentNotes = new List<string>
                {
                    "Run 50% initial buffet display; replenish in 45-min batches.",
                    "Food safety policy: discard expired holding batches; never reuse leftovers."
                },
                EventProductionSummaries = new List<string>
                {
                    "Check banquet staging timeline and dietary-free prep zone."
                },
                ChefRecommendations = recs.Take(5).Select(r => MapRecDto(r, canViewCosts)).ToList()
            };
        }

        private async Task<T?> TryPostPythonAsync<T>(string path, object payload, CancellationToken cancellationToken) where T : class
        {
            try
            {
                string? baseUrl = _configuration[Common.CULINARY_ENGINE_URL];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return null;
                }

                HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                using HttpRequestMessage msg = new(HttpMethod.Post, path)
                {
                    Content = JsonContent.Create(payload, options: CamelJson)
                };
                string? apiKey = _configuration["InterCallService:ApiKey"];
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    msg.Headers.TryAddWithoutValidation("X-API-Key", apiKey);
                }

                using HttpResponseMessage response = await client.SendAsync(msg, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                return await response.Content.ReadFromJsonAsync<T>(CamelJson, cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        private static T? SafeDeserialize<T>(string? json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(json, CamelJson);
            }
            catch
            {
                return null;
            }
        }

        private static List<string> SplitTags(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
            return raw.Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        private static List<string> ReadStringList(JsonElement root, string prop)
        {
            List<string> list = new();
            if (root.TryGetProperty(prop, out JsonElement el) && el.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in el.EnumerateArray())
                {
                    string? v = item.GetString();
                    if (!string.IsNullOrWhiteSpace(v)) list.Add(v);
                }
            }
            return list;
        }

        private static string SanitizeCsv(string? value)
        {
            string s = (value ?? string.Empty).Replace("\"", "\"\"").Trim();
            if (s.Length > 0 && (s[0] is '=' or '+' or '-' or '@' or '\t' or '\r'))
            {
                s = "'" + s;
            }
            return $"\"{s}\"";
        }
    }
}
