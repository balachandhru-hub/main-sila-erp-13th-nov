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
using SharedKernel.Integration.Services;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    public interface ISilaCulinaryFinancialService
    {
        Task<SilaCulinaryFinancialConfigDto> GetConfigAsync(Guid organizationId, CancellationToken cancellationToken);
        Task<SilaCulinaryFinancialConfigDto> SaveConfigAsync(Guid organizationId, Guid userId, Guid roleId, SilaCulinarySaveFinancialConfigRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryFinancialRecipeCostDto> CalculateRecipeCostAsync(Guid organizationId, Guid userId, Guid roleId, Guid recipeId, SilaCulinaryFinancialRecipeCostRequestDto? request, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuCostDto> CalculateMenuCostAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuId, int? plannedGuestsOverride, CancellationToken cancellationToken);
        Task<SilaCulinaryMaterialPriceImpactDto> AnalyzeMaterialPriceImpactAsync(Guid organizationId, Guid roleId, SilaCulinaryMaterialPriceImpactRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryFinancialDashboardDto> GetDashboardAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken);
        Task<List<SilaCulinaryEventFinancialPlanSummaryDto>> ListPlansAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, bool? includeSimulations, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> GetPlanAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> CreateAndCalculatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateEventFinancialPlanRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> RecalculatePlanAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> AddRevenueLineAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryAddRevenueLineRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> AddExpenseLineAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryAddExpenseLineRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> ApproveExpenseLineAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid expenseId, CancellationToken cancellationToken);
        Task<SilaCulinarySimulateFinancialResultDto> SimulateAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, SilaCulinarySimulateFinancialRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> PromoteScenarioToRevisionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid scenarioId, CancellationToken cancellationToken);
        Task<SilaCulinaryFinancialExceptionDto> ResolveExceptionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> SubmitForApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> DecideApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string decision, string? comment, CancellationToken cancellationToken);
        Task<SilaCulinaryEventFinancialPlanDetailDto> HandleEventChangeImpactAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, int? updatedGuests, decimal? updatedPricePerGuest, CancellationToken cancellationToken);
        Task<SilaCulinaryEventCancellationExposureDto> GetCancellationExposureAsync(Guid organizationId, Guid roleId, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryFinancialAiResponseDto> AnalyzeWithAiAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, SilaCulinaryFinancialAiRequestDto request, CancellationToken cancellationToken);
        List<SilaCulinaryFinancialReportCatalogItemDto> GetReportCatalog();
        Task<SilaCulinaryFinancialReportResultDto> ExportReportAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, string reportCode, CancellationToken cancellationToken);
        Task<SilaCulinaryMobileFinancialSummaryDto> GetMobileSummaryAsync(Guid organizationId, Guid userId, Guid roleId, Guid? planId, CancellationToken cancellationToken);
        Task<SilaCulinaryPhase3BHandoverDto> GetPhase3BHandoverAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken);
        Task<SilaCulinaryPhase4BHandoverDto> GetPhase4BHandoverAsync(Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken);
    }

    public class SilaCulinaryFinancialService : ISilaCulinaryFinancialService
    {
        private static readonly JsonSerializerOptions CamelJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly Regex PromptInjectionPattern = new(
            @"(ignore\s+previous|system\s+prompt|override\s+budget|drop\s+table|post\s+(sap|gl|journal|invoice)|delete\s+from)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationCredentialProtector _protector;

        public SilaCulinaryFinancialService(
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

        private static bool CanViewEmployeeRates(Guid roleId) =>
            roleId == Common.BUYER_ADMIN_ROLE_ID
            || roleId == Common.FINANCE_MANAGER_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID
            || roleId == Common.FNB_DIRECTOR_ROLE_ID;

        private static bool CanApproveFinancial(Guid roleId) =>
            roleId == Common.BUYER_ADMIN_ROLE_ID
            || roleId == Common.FINANCE_MANAGER_ROLE_ID
            || roleId == Common.COST_CONTROLLER_ROLE_ID
            || roleId == Common.FNB_DIRECTOR_ROLE_ID;

        private async Task<BuyerBusinessProfile> ResolveBuyerAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            return await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Buyer not found.", "Buyer organization profile was not found.");
        }

        private async Task<SilaCulinaryFinancialConfig> GetOrCreateConfigEntityAsync(Guid buyerId, CancellationToken cancellationToken)
        {
            SilaCulinaryFinancialConfig? cfg = await _repository.SilaCulinaryFinancialConfig
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (cfg != null) return cfg;

            cfg = new SilaCulinaryFinancialConfig
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ReportingCurrency = "AED",
                PropertyCurrency = "AED",
                ExchangeRatesJson = JsonSerializer.Serialize(new List<SilaCulinaryFxRateDto>
                {
                    new() { SourceCurrency = "USD", TargetCurrency = "AED", Rate = 3.6725m, RateType = "PEGGED_OFFICIAL", Source = "TENANT_FX_TABLE" },
                    new() { SourceCurrency = "EUR", TargetCurrency = "AED", Rate = 3.9800m, RateType = "CORPORATE", Source = "TENANT_FX_TABLE" },
                    new() { SourceCurrency = "GBP", TargetCurrency = "AED", Rate = 4.6500m, RateType = "CORPORATE", Source = "TENANT_FX_TABLE" },
                    new() { SourceCurrency = "INR", TargetCurrency = "AED", Rate = 0.0440m, RateType = "CORPORATE", Source = "TENANT_FX_TABLE" },
                    new() { SourceCurrency = "MYR", TargetCurrency = "AED", Rate = 0.8250m, RateType = "CORPORATE", Source = "TENANT_FX_TABLE" },
                }, CamelJson),
                OverheadRulesJson = "[]",
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            };
            _repository.SilaCulinaryFinancialConfig.Create(cfg);
            await _repository.SaveAsync();
            return cfg;
        }

        private static SilaCulinaryFinancialConfigDto ToConfigDto(SilaCulinaryFinancialConfig c)
        {
            List<string> priority = DeserializeOrDefault(c.CostSourcePriorityJson, new List<string>
            {
                "SUPPLIER_CONTRACT","SUPPLIER_CATALOG","INVENTORY_VALUATION","MOVING_AVERAGE","LATEST_PURCHASE","WEIGHTED_HISTORICAL","STANDARD_COST","MANUAL_ESTIMATE"
            });
            List<SilaCulinaryFxRateDto> fx = DeserializeOrDefault(c.ExchangeRatesJson, new List<SilaCulinaryFxRateDto>());
            List<SilaCulinaryOverheadRuleDto> oh = DeserializeOrDefault(c.OverheadRulesJson, new List<SilaCulinaryOverheadRuleDto>());
            return new SilaCulinaryFinancialConfigDto
            {
                Id = c.Id,
                ReportingCurrency = c.ReportingCurrency,
                PropertyCurrency = c.PropertyCurrency,
                FinancialPrecision = c.FinancialPrecision,
                QuantityPrecision = c.QuantityPrecision,
                MonetaryRoundingPolicy = c.MonetaryRoundingPolicy,
                CostSourcePriority = priority,
                CostValuationPolicy = c.CostValuationPolicy,
                TargetFoodCostPercent = c.TargetFoodCostPercent,
                TargetBeverageCostPercent = c.TargetBeverageCostPercent,
                TargetEventContributionMarginPercent = c.TargetEventContributionMarginPercent,
                MaterialCostAlertThresholdPercent = c.MaterialCostAlertThresholdPercent,
                BudgetApprovalThresholdAmount = c.BudgetApprovalThresholdAmount,
                AllowManualCostOverrides = c.AllowManualCostOverrides,
                ExcludeVatFromRevenue = c.ExcludeVatFromRevenue,
                DefaultVatPercent = c.DefaultVatPercent,
                ExchangeRateSource = c.ExchangeRateSource,
                ExchangeRates = fx,
                OverheadRules = oh,
                FiscalYearStartMonth = c.FiscalYearStartMonth,
                CostSnapshotPolicy = c.CostSnapshotPolicy,
            };
        }

        private static T DeserializeOrDefault<T>(string? json, T fallback)
        {
            if (string.IsNullOrWhiteSpace(json)) return fallback;
            try { return JsonSerializer.Deserialize<T>(json, CamelJson) ?? fallback; }
            catch { return fallback; }
        }

        private static decimal? ResolveFxRate(string? sourceCurrency, string targetCurrency, List<SilaCulinaryFxRateDto> rates)
        {
            string src = (sourceCurrency ?? targetCurrency).Trim().ToUpperInvariant();
            string tgt = targetCurrency.Trim().ToUpperInvariant();
            if (src == tgt) return 1m;
            SilaCulinaryFxRateDto? direct = rates.FirstOrDefault(r =>
                r.SourceCurrency.Equals(src, StringComparison.OrdinalIgnoreCase) &&
                r.TargetCurrency.Equals(tgt, StringComparison.OrdinalIgnoreCase) &&
                r.Rate > 0);
            if (direct != null) return direct.Rate;
            SilaCulinaryFxRateDto? inverse = rates.FirstOrDefault(r =>
                r.SourceCurrency.Equals(tgt, StringComparison.OrdinalIgnoreCase) &&
                r.TargetCurrency.Equals(src, StringComparison.OrdinalIgnoreCase) &&
                r.Rate > 0);
            if (inverse != null) return 1m / inverse.Rate;
            return null;
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

        private async Task UpsertNotificationAsync(
            Guid buyerId, Guid actorUserId, string taskType, string dedupKey,
            string title, string description, string priority, Guid planId, string planCode, CancellationToken cancellationToken)
        {
            try
            {
                await SilaOperations.UpsertTaskAsync(_repository, buyerId, actorUserId, new SilaOperations.TaskDraft
                {
                    TaskType = taskType,
                    DeduplicationKey = dedupKey,
                    Title = title,
                    Description = description,
                    Priority = priority,
                    SourceModule = "CULINARY_FINANCIAL",
                    SourceDocumentType = Common.SILA_REF_CULINARY_FINANCIAL_PLAN,
                    SourceDocumentId = planId,
                    SourceDocumentNumber = planCode,
                    ActionRoute = $"/sila/recipes-2?tab=event-pnl&planId={planId}",
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"Financial notification skip: {ex.Message}");
            }
        }

        public async Task<SilaCulinaryFinancialConfigDto> GetConfigAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            return ToConfigDto(cfg);
        }

        public async Task<SilaCulinaryFinancialConfigDto> SaveConfigAsync(
            Guid organizationId, Guid userId, Guid roleId, SilaCulinarySaveFinancialConfigRequestDto request, CancellationToken cancellationToken)
        {
            if (!CanApproveFinancial(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Only authorized financial/culinary managers may modify financial configuration.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            cfg.ReportingCurrency = (request.ReportingCurrency ?? "AED").Trim().ToUpperInvariant();
            cfg.PropertyCurrency = (request.PropertyCurrency ?? cfg.ReportingCurrency).Trim().ToUpperInvariant();
            cfg.FinancialPrecision = Math.Clamp(request.FinancialPrecision, 0, 4);
            cfg.QuantityPrecision = Math.Clamp(request.QuantityPrecision, 0, 6);
            cfg.MonetaryRoundingPolicy = string.IsNullOrWhiteSpace(request.MonetaryRoundingPolicy) ? "ROUND_HALF_UP" : request.MonetaryRoundingPolicy.Trim().ToUpperInvariant();
            if (request.CostSourcePriority is { Count: > 0 })
                cfg.CostSourcePriorityJson = JsonSerializer.Serialize(request.CostSourcePriority, CamelJson);
            cfg.CostValuationPolicy = string.IsNullOrWhiteSpace(request.CostValuationPolicy) ? cfg.CostValuationPolicy : request.CostValuationPolicy.Trim().ToUpperInvariant();
            cfg.TargetFoodCostPercent = request.TargetFoodCostPercent > 0 ? request.TargetFoodCostPercent : 28m;
            cfg.TargetBeverageCostPercent = request.TargetBeverageCostPercent > 0 ? request.TargetBeverageCostPercent : 22m;
            cfg.TargetEventContributionMarginPercent = request.TargetEventContributionMarginPercent > 0 ? request.TargetEventContributionMarginPercent : 40m;
            cfg.MaterialCostAlertThresholdPercent = request.MaterialCostAlertThresholdPercent;
            cfg.BudgetApprovalThresholdAmount = request.BudgetApprovalThresholdAmount;
            cfg.AllowManualCostOverrides = request.AllowManualCostOverrides;
            cfg.ExcludeVatFromRevenue = request.ExcludeVatFromRevenue;
            cfg.DefaultVatPercent = request.DefaultVatPercent;
            cfg.ExchangeRateSource = string.IsNullOrWhiteSpace(request.ExchangeRateSource) ? "TENANT_FX_TABLE" : request.ExchangeRateSource.Trim().ToUpperInvariant();
            if (request.ExchangeRates != null)
                cfg.ExchangeRatesJson = JsonSerializer.Serialize(request.ExchangeRates, CamelJson);
            if (request.OverheadRules != null)
                cfg.OverheadRulesJson = JsonSerializer.Serialize(request.OverheadRules, CamelJson);
            cfg.FiscalYearStartMonth = Math.Clamp(request.FiscalYearStartMonth, 1, 12);
            cfg.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, cfg.Id, Common.SILA_AUDIT_CULINARY_FIN_REVISED,
                $"Financial configuration updated (Reporting currency {cfg.ReportingCurrency}, Target margin {cfg.TargetEventContributionMarginPercent}%).", userId);
            await _repository.SaveAsync();
            return ToConfigDto(cfg);
        }

        public async Task<SilaCulinaryFinancialRecipeCostDto> CalculateRecipeCostAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid recipeId, SilaCulinaryFinancialRecipeCostRequestDto? request, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Financial recipe costing visibility is restricted to authorized roles.");

            request ??= new SilaCulinaryFinancialRecipeCostRequestDto();
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            SilaCulinaryFinancialConfigDto cfgDto = ToConfigDto(cfg);

            HashSet<Guid> visited = new();
            SilaCulinaryFinancialRecipeCostDto result = await ComputeRecipeRecursiveAsync(
                buyer.Id, recipeId, cfgDto, request, visited, cancellationToken);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, recipeId, Common.SILA_AUDIT_CULINARY_FIN_RECIPE_CALC,
                $"Calculated Phase 3A recipe cost for {result.RecipeName} v{result.RecipeVersion}: {result.ReportingCurrency} {result.TotalIngredientCost:0.00} ({result.CostCompleteness}).", userId);
            await _repository.SaveAsync();
            return result;
        }

        private async Task<SilaCulinaryFinancialRecipeCostDto> ComputeRecipeRecursiveAsync(
            Guid buyerId,
            Guid recipeId,
            SilaCulinaryFinancialConfigDto cfg,
            SilaCulinaryFinancialRecipeCostRequestDto req,
            HashSet<Guid> visitedStack,
            CancellationToken cancellationToken)
        {
            Recipe recipe = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyerId && x.Id == recipeId && x.IsActive)
                .Include(x => x.Ingredients)
                .Include(x => x.OutletPrices)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Recipe not found.", "Recipe was not found.");

            decimal stdServings = recipe.ServingQty > 0 ? recipe.ServingQty : 1m;
            decimal plannedServings = req.PlannedServings.HasValue && req.PlannedServings.Value > 0 ? req.PlannedServings.Value : stdServings;
            decimal rawBatches = plannedServings / stdServings;
            decimal plannedBatches = req.AllowPartialBatch ? rawBatches : Math.Ceiling(rawBatches);
            decimal plannedOutputServings = plannedBatches * stdServings;
            decimal excessServings = Math.Max(0m, plannedOutputServings - plannedServings);

            List<RecipeIngredient> activeIngs = recipe.Ingredients.Where(i => i.IsActive && (i.Version == 0 || i.Version == recipe.Version || i.Version == recipe.ActiveVersion)).ToList();
            if (activeIngs.Count == 0)
                activeIngs = recipe.Ingredients.Where(i => i.IsActive).ToList();

            List<Guid> matIds = activeIngs.Where(i => i.MaterialId.HasValue).Select(i => i.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> materials = matIds.Count == 0
                ? new()
                : await _repository.ItemBuyerMaster.FindByCondition(m => m.BuyerId == buyerId && matIds.Contains(m.Id) && m.IsActive)
                    .ToDictionaryAsync(m => m.Id, cancellationToken);

            Dictionary<Guid, MaterialOutletPrice> outletPrices = new();
            if (req.OutletId.HasValue && matIds.Count > 0)
            {
                outletPrices = await _repository.MaterialOutletPrice
                    .FindByCondition(p => p.BuyerId == buyerId && p.OutletLocationId == req.OutletId.Value && matIds.Contains(p.MaterialId) && p.IsActive)
                    .ToDictionaryAsync(p => p.MaterialId, cancellationToken);
            }

            List<MaterialUomConversion> uomConvs = matIds.Count == 0
                ? new()
                : await _repository.MaterialUomConversion
                    .FindByCondition(u => u.BuyerId == buyerId && matIds.Contains(u.MaterialId) && u.IsActive)
                    .ToListAsync(cancellationToken);

            decimal scale = 1m + (req.GlobalPriceChangePercent / 100m);
            decimal totalIng = 0m, foodCost = 0m, bevCost = 0m, subCost = 0m;
            int missingPrice = 0, missingFx = 0, uomConflict = 0;
            bool circular = false;
            List<SilaCulinaryFinancialRecipeLineDto> lines = new();

            visitedStack.Add(recipeId);
            foreach (RecipeIngredient ing in activeIngs.OrderBy(i => i.Sequence))
            {
                if (ing.SubRecipeId.HasValue)
                {
                    if (visitedStack.Contains(ing.SubRecipeId.Value))
                    {
                        circular = true;
                        lines.Add(new SilaCulinaryFinancialRecipeLineDto
                        {
                            LineRef = ing.IngredientCode,
                            MaterialCode = ing.ItemCode,
                            Description = ing.ItemName,
                            IsSubRecipe = true,
                            RecipeQty = ing.Quantity * plannedBatches,
                            RecipeUom = ing.Uom,
                            RawQty = ing.Quantity * plannedBatches,
                            CostableBaseQty = ing.Quantity * plannedBatches,
                            BaseUom = ing.BaseUom,
                            Status = "CIRCULAR_DEPENDENCY",
                            Warning = "Circular sub-recipe dependency detected and blocked.",
                        });
                        continue;
                    }

                    SilaCulinaryFinancialRecipeCostDto subDto = await ComputeRecipeRecursiveAsync(
                        buyerId, ing.SubRecipeId.Value, cfg,
                        new SilaCulinaryFinancialRecipeCostRequestDto { OutletId = req.OutletId, AllowPartialBatch = true, GlobalPriceChangePercent = req.GlobalPriceChangePercent },
                        new HashSet<Guid>(visitedStack), cancellationToken);

                    decimal unitSubCost = subDto.IngredientCostPerServing;
                    decimal scaledQty = ing.Quantity * plannedBatches;
                    decimal lineVal = decimal.Round(scaledQty * unitSubCost, 4);
                    subCost += lineVal;
                    totalIng += lineVal;
                    if (subDto.BeverageIngredientCost > subDto.FoodIngredientCost) bevCost += lineVal;
                    else foodCost += lineVal;

                    lines.Add(new SilaCulinaryFinancialRecipeLineDto
                    {
                        LineRef = ing.IngredientCode,
                        MaterialCode = subDto.RecipeCode,
                        Description = $"{ing.ItemName} (Sub-Recipe v{subDto.RecipeVersion})",
                        CostClass = subDto.BeverageIngredientCost > subDto.FoodIngredientCost ? "BEVERAGE" : "FOOD",
                        IsSubRecipe = true,
                        RecipeQty = decimal.Round(scaledQty, 4),
                        RecipeUom = ing.Uom,
                        RawQty = decimal.Round(scaledQty, 4),
                        CostableBaseQty = decimal.Round(scaledQty, 4),
                        BaseUom = ing.BaseUom,
                        NormalizedUnitCost = decimal.Round(unitSubCost, 4),
                        PriceSource = "SUB_RECIPE_SNAPSHOT",
                        SourceCurrency = cfg.ReportingCurrency,
                        FxRateApplied = 1m,
                        IngredientCost = lineVal,
                        Status = subDto.CostCompleteness == "COMPLETE" ? "VERIFIED" : "ESTIMATED",
                    });
                    continue;
                }

                decimal batchQty = ing.Quantity * plannedBatches;
                decimal trimFactor = 1m - ((recipe.TrimLossPercent ?? 0m) / 100m);
                decimal yieldPct = trimFactor > 0 && trimFactor <= 1m ? decimal.Round(trimFactor * 100m, 2) : 100m;
                decimal baseQty = (ing.BaseQuantity > 0 ? ing.BaseQuantity : ing.Quantity) * plannedBatches;
                string baseUom = string.IsNullOrWhiteSpace(ing.BaseUom) ? ing.Uom : ing.BaseUom;
                bool uomOk = true;

                if (!ing.Uom.Equals(baseUom, StringComparison.OrdinalIgnoreCase) && ing.BaseQuantity <= 0 && ing.MaterialId.HasValue)
                {
                    MaterialUomConversion? conv = uomConvs.FirstOrDefault(u => u.MaterialId == ing.MaterialId.Value &&
                        u.FromUom.Equals(ing.Uom, StringComparison.OrdinalIgnoreCase) && u.ToUom.Equals(baseUom, StringComparison.OrdinalIgnoreCase));
                    if (conv != null && conv.Factor > 0)
                        baseQty = batchQty * conv.Factor;
                    else if (ing.Uom.Equals("G", StringComparison.OrdinalIgnoreCase) && baseUom.Equals("KG", StringComparison.OrdinalIgnoreCase))
                        baseQty = batchQty / 1000m;
                    else if (ing.Uom.Equals("ML", StringComparison.OrdinalIgnoreCase) && baseUom.Equals("L", StringComparison.OrdinalIgnoreCase))
                        baseQty = batchQty / 1000m;
                    else
                        uomOk = false;
                }

                if (!uomOk)
                {
                    uomConflict++;
                    lines.Add(new SilaCulinaryFinancialRecipeLineDto
                    {
                        LineRef = ing.IngredientCode,
                        MaterialCode = ing.ItemCode,
                        Description = ing.ItemName,
                        RecipeQty = decimal.Round(batchQty, 4),
                        RecipeUom = ing.Uom,
                        RawQty = decimal.Round(batchQty, 4),
                        CostableBaseQty = decimal.Round(batchQty, 4),
                        BaseUom = baseUom,
                        Status = "UOM_CONFLICT",
                        Warning = $"Missing UOM conversion {ing.Uom} -> {baseUom}.",
                    });
                    continue;
                }

                ItemBuyerMaster? mat = ing.MaterialId.HasValue && materials.TryGetValue(ing.MaterialId.Value, out ItemBuyerMaster? foundMat) ? foundMat : null;
                string costClass = ClassifyCostClass(mat?.MaterialGroup ?? recipe.ProductClass);

                decimal? unitPrice = null;
                string priceSrc = "MISSING";
                string? srcCcy = cfg.ReportingCurrency;

                if (req.ManualPriceOverrides != null && req.ManualPriceOverrides.TryGetValue(ing.ItemCode, out decimal manualOv))
                {
                    if (!cfg.AllowManualCostOverrides)
                        throw new ForBiddenCustomException("Overrides disabled.", "Tenant financial policy disables manual cost overrides.");
                    unitPrice = manualOv;
                    priceSrc = "MANUAL_ESTIMATE";
                }
                else if (ing.MaterialId.HasValue && outletPrices.TryGetValue(ing.MaterialId.Value, out MaterialOutletPrice? mop) && mop.UnitCost > 0)
                {
                    unitPrice = mop.UnitCost;
                    priceSrc = "OUTLET_APPROVED_PRICE";
                    srcCcy = string.IsNullOrWhiteSpace(mop.Currency) ? cfg.ReportingCurrency : mop.Currency;
                }
                else if (mat != null)
                {
                    if (string.Equals(req.CostingPolicy, "ACTUAL_CONSUMPTION", StringComparison.OrdinalIgnoreCase) && mat.MovingAveragePrice is > 0)
                    {
                        unitPrice = mat.MovingAveragePrice.Value;
                        priceSrc = "MOVING_AVERAGE";
                    }
                    else if (mat.MovingAveragePrice is > 0)
                    {
                        unitPrice = mat.MovingAveragePrice.Value;
                        priceSrc = "MOVING_AVERAGE";
                    }
                    else if (mat.UnitCost is > 0)
                    {
                        unitPrice = mat.UnitCost.Value;
                        priceSrc = "INVENTORY_VALUATION";
                    }
                    else if (mat.StandardPrice is > 0)
                    {
                        unitPrice = mat.StandardPrice.Value;
                        priceSrc = "STANDARD_COST";
                    }
                    srcCcy = string.IsNullOrWhiteSpace(mat.Currency) ? cfg.ReportingCurrency : mat.Currency;
                }
                else if (ing.UnitCost is > 0)
                {
                    unitPrice = ing.UnitCost.Value;
                    priceSrc = "RECIPE_LINE_SNAPSHOT";
                    srcCcy = string.IsNullOrWhiteSpace(recipe.Currency) ? cfg.ReportingCurrency : recipe.Currency;
                }

                if (!unitPrice.HasValue)
                {
                    missingPrice++;
                    lines.Add(new SilaCulinaryFinancialRecipeLineDto
                    {
                        LineRef = ing.IngredientCode,
                        MaterialCode = ing.ItemCode,
                        Description = ing.ItemName,
                        CostClass = costClass,
                        RecipeQty = decimal.Round(batchQty, 4),
                        RecipeUom = ing.Uom,
                        RawQty = decimal.Round(batchQty, 4),
                        CostableBaseQty = decimal.Round(baseQty, 4),
                        BaseUom = baseUom,
                        EffectiveYieldPercent = yieldPct,
                        Status = "MISSING_PRICE",
                        Warning = "No verified material price available (never treated as zero).",
                    });
                    continue;
                }

                decimal? fx = ResolveFxRate(srcCcy, cfg.ReportingCurrency, cfg.ExchangeRates);
                if (!fx.HasValue)
                {
                    missingFx++;
                    lines.Add(new SilaCulinaryFinancialRecipeLineDto
                    {
                        LineRef = ing.IngredientCode,
                        MaterialCode = ing.ItemCode,
                        Description = ing.ItemName,
                        CostClass = costClass,
                        RecipeQty = decimal.Round(batchQty, 4),
                        RecipeUom = ing.Uom,
                        RawQty = decimal.Round(batchQty, 4),
                        CostableBaseQty = decimal.Round(baseQty, 4),
                        BaseUom = baseUom,
                        PriceSource = priceSrc,
                        SourceCurrency = srcCcy,
                        Status = "MISSING_FX",
                        Warning = $"Missing exchange rate {srcCcy}->{cfg.ReportingCurrency}.",
                    });
                    continue;
                }

                decimal normUnit = unitPrice.Value * fx.Value * scale;
                decimal lineCost = decimal.Round(baseQty * normUnit, 4);
                totalIng += lineCost;
                if (costClass is "ALCOHOLIC_BEVERAGE" or "NON_ALCOHOLIC_BEVERAGE") bevCost += lineCost;
                else foodCost += lineCost;

                lines.Add(new SilaCulinaryFinancialRecipeLineDto
                {
                    LineRef = ing.IngredientCode,
                    MaterialCode = ing.ItemCode,
                    Description = ing.ItemName,
                    CostClass = costClass,
                    RecipeQty = decimal.Round(batchQty, 4),
                    RecipeUom = ing.Uom,
                    RawQty = decimal.Round(batchQty, 4),
                    CostableBaseQty = decimal.Round(baseQty, 4),
                    BaseUom = baseUom,
                    EffectiveYieldPercent = yieldPct,
                    NormalizedUnitCost = decimal.Round(normUnit, 4),
                    PriceSource = priceSrc,
                    SourceCurrency = srcCcy,
                    FxRateApplied = decimal.Round(fx.Value, 6),
                    IngredientCost = lineCost,
                    Status = priceSrc == "MANUAL_ESTIMATE" ? "ESTIMATED" : "VERIFIED",
                });
            }

            if (totalIng > 0)
            {
                foreach (SilaCulinaryFinancialRecipeLineDto l in lines)
                {
                    if (l.IngredientCost.HasValue)
                        l.ShareOfIngredientCostPct = decimal.Round((l.IngredientCost.Value / totalIng) * 100m, 2);
                }
            }

            decimal directProd = req.DirectProductionFixedCost * plannedBatches;
            decimal overhead = req.AllocatedOverheadFixedCost * plannedBatches;
            decimal fullBatch = totalIng + directProd + overhead;
            decimal ingPerServing = totalIng / plannedOutputServings;
            decimal directPerServing = (totalIng + directProd) / plannedOutputServings;
            decimal fullPerServing = fullBatch / plannedOutputServings;

            RecipeOutletPrice? rop = req.OutletId.HasValue
                ? recipe.OutletPrices.FirstOrDefault(p => p.IsActive && p.OutletLocationId == req.OutletId.Value)
                : recipe.OutletPrices.FirstOrDefault(p => p.IsActive);
            decimal? netSelling = null;
            if (rop != null && rop.MenuPrice > 0)
            {
                netSelling = cfg.ExcludeVatFromRevenue && cfg.DefaultVatPercent > 0
                    ? rop.MenuPrice / (1m + cfg.DefaultVatPercent / 100m)
                    : rop.MenuPrice;
            }

            decimal? fcPct = netSelling is > 0 ? decimal.Round((ingPerServing / netSelling.Value) * 100m, 2) : null;
            decimal? ingContrib = netSelling is > 0 ? decimal.Round(netSelling.Value - ingPerServing, 4) : null;
            decimal? ingContribPct = netSelling is > 0 && ingContrib.HasValue ? decimal.Round((ingContrib.Value / netSelling.Value) * 100m, 2) : null;
            decimal? dirContrib = netSelling is > 0 ? decimal.Round(netSelling.Value - directPerServing, 4) : null;
            decimal? fullProfit = netSelling is > 0 ? decimal.Round(netSelling.Value - fullPerServing, 4) : null;

            decimal targetFc = req.TargetFoodCostPercent is > 0 ? req.TargetFoodCostPercent.Value : cfg.TargetFoodCostPercent;
            decimal targetMargin = req.TargetContributionMarginPercent is > 0 ? req.TargetContributionMarginPercent.Value : cfg.TargetEventContributionMarginPercent;
            decimal reqPriceFc = targetFc > 0 ? decimal.Round(ingPerServing / (targetFc / 100m), 2) : 0m;
            decimal denom = 1m - (targetMargin / 100m);
            decimal reqPriceMarginDirect = denom > 0 ? decimal.Round(directPerServing / denom, 2) : 0m;
            decimal reqPriceMarginFull = denom > 0 ? decimal.Round(fullPerServing / denom, 2) : 0m;

            return new SilaCulinaryFinancialRecipeCostDto
            {
                RecipeId = recipe.Id,
                RecipeCode = recipe.RecipeCode,
                RecipeName = recipe.Name,
                RecipeVersion = recipe.Version,
                Category = recipe.Category ?? "Uncategorized",
                ReportingCurrency = cfg.ReportingCurrency,
                StandardBatchServings = stdServings,
                PlannedServings = plannedServings,
                PlannedBatches = decimal.Round(plannedBatches, 4),
                PlannedOutputServings = decimal.Round(plannedOutputServings, 4),
                ExcessServings = decimal.Round(excessServings, 4),
                TotalIngredientCost = decimal.Round(totalIng, 2),
                FoodIngredientCost = decimal.Round(foodCost, 2),
                BeverageIngredientCost = decimal.Round(bevCost, 2),
                SubRecipeCost = decimal.Round(subCost, 2),
                DirectProductionCost = decimal.Round(directProd, 2),
                AllocatedOverheadCost = decimal.Round(overhead, 2),
                FullyAllocatedBatchCost = decimal.Round(fullBatch, 2),
                ExpectedEventConsumptionCost = decimal.Round(directPerServing * plannedServings, 2),
                RemainingUsableOutputValue = decimal.Round(directPerServing * excessServings, 2),
                IngredientCostPerServing = decimal.Round(ingPerServing, 4),
                DirectCostPerServing = decimal.Round(directPerServing, 4),
                FullyAllocatedCostPerServing = decimal.Round(fullPerServing, 4),
                NetSellingPricePerServing = netSelling.HasValue ? decimal.Round(netSelling.Value, 2) : null,
                FoodCostPercent = fcPct,
                IngredientContributionPerServing = ingContrib,
                IngredientContributionPercent = ingContribPct,
                DirectContributionPerServing = dirContrib,
                FullyAllocatedProfitPerServing = fullProfit,
                RequiredSellingPriceTargetFoodCost = reqPriceFc,
                RequiredSellingPriceTargetMarginDirect = reqPriceMarginDirect,
                RequiredSellingPriceTargetMarginFull = reqPriceMarginFull,
                CostCompleteness = (missingPrice == 0 && missingFx == 0 && uomConflict == 0 && !circular) ? "COMPLETE" : "INCOMPLETE",
                MissingPriceCount = missingPrice,
                MissingFxCount = missingFx,
                UomConflictCount = uomConflict,
                CircularDependencyDetected = circular,
                Lines = lines,
            };
        }

        private static string ClassifyCostClass(string? raw)
        {
            string s = (raw ?? string.Empty).ToUpperInvariant();
            if (s.Contains("WINE") || s.Contains("BEER") || s.Contains("SPIRIT") || s.Contains("ALCOHOL"))
                return "ALCOHOLIC_BEVERAGE";
            if (s.Contains("BEVERAGE") || s.Contains("DRINK") || s.Contains("COFFEE") || s.Contains("TEA") || s.Contains("JUICE"))
                return "NON_ALCOHOLIC_BEVERAGE";
            if (s.Contains("TOBACCO")) return "TOBACCO";
            if (s.Contains("PACK")) return "PACKAGING";
            return "FOOD";
        }

        public async Task<SilaCulinaryMenuCostDto> CalculateMenuCostAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid menuId, int? plannedGuestsOverride, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Menu financial costing is restricted to authorized roles.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Culinary menu was not found.");

            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            SilaCulinaryEvent? evt = menu.EventId.HasValue
                ? await _repository.SilaCulinaryEvent.FindByCondition(e => e.BuyerId == buyer.Id && e.Id == menu.EventId.Value && e.IsActive).FirstOrDefaultAsync(cancellationToken)
                : null;

            int guests = plannedGuestsOverride is > 0 ? plannedGuestsOverride.Value : (evt?.PlanningGuests is > 0 ? evt.PlanningGuests : 100);
            decimal foodTotal = 0m, bevTotal = 0m;
            List<SilaCulinaryMenuCostLineDto> resultLines = new();

            foreach (SilaCulinaryMenuLine l in lines)
            {
                decimal unitCost = l.UnitCostPerPortion ?? 0m;
                if (unitCost == 0m && l.RecipeId.HasValue)
                {
                    Recipe? r = await _repository.Recipe.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == l.RecipeId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken);
                    if (r != null && r.ServingQty > 0)
                        unitCost = r.TotalCost / r.ServingQty;
                }
                decimal portions = guests * (l.UptakePercent / 100m) * (l.ServingFrequency > 0 ? l.ServingFrequency : 1m);
                decimal lineCost = decimal.Round(portions * unitCost, 2);
                string cls = l.CourseName.Contains("Beverage", StringComparison.OrdinalIgnoreCase) || l.CourseName.Contains("Drink", StringComparison.OrdinalIgnoreCase)
                    ? "BEVERAGE" : "FOOD";
                if (cls == "BEVERAGE") bevTotal += lineCost;
                else foodTotal += lineCost;

                resultLines.Add(new SilaCulinaryMenuCostLineDto
                {
                    LineRef = l.Id.ToString()[..8],
                    Name = l.DishName,
                    Course = l.CourseName,
                    CostClass = cls,
                    ExpectedPortions = decimal.Round(portions, 2),
                    CostPerServing = decimal.Round(unitCost, 4),
                    ExpectedLineCost = lineCost,
                    CostPerGuest = guests > 0 ? decimal.Round(lineCost / guests, 4) : 0m,
                });
            }

            decimal totalCost = foodTotal + bevTotal;
            decimal? pkgRev = menu.TargetSellingPrice.HasValue ? decimal.Round(menu.TargetSellingPrice.Value * guests, 2)
                : (evt != null && evt.NetPricePerGuest > 0 ? decimal.Round(evt.NetPricePerGuest * guests, 2) : null);

            decimal? foodPct = evt != null && evt.EstimatedFoodRevenue > 0
                ? decimal.Round((foodTotal / evt.EstimatedFoodRevenue) * 100m, 2)
                : (pkgRev is > 0 ? decimal.Round((foodTotal / pkgRev.Value) * 100m, 2) : null);
            string foodLabel = evt != null && evt.EstimatedFoodRevenue > 0 ? "Food Cost % of Allocated Food Revenue" : (pkgRev is > 0 ? "Food Cost % against Total Package Revenue" : "Revenue Unknown");

            decimal? bevPct = evt != null && evt.EstimatedBeverageRevenue > 0
                ? decimal.Round((bevTotal / evt.EstimatedBeverageRevenue) * 100m, 2)
                : (pkgRev is > 0 ? decimal.Round((bevTotal / pkgRev.Value) * 100m, 2) : null);
            string bevLabel = evt != null && evt.EstimatedBeverageRevenue > 0 ? "Beverage Cost % of Allocated Beverage Revenue" : (pkgRev is > 0 ? "Beverage Cost % against Total Package Revenue" : "Revenue Unknown");

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, menu.Id, Common.SILA_AUDIT_CULINARY_FIN_MENU_CALC,
                $"Calculated menu cost for {menu.Name} ({guests} guests): {cfg.ReportingCurrency} {totalCost:0.00}.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryMenuCostDto
            {
                MenuId = menu.Id,
                MenuName = menu.Name,
                ServiceStyle = menu.ServiceStyle,
                PlannedGuests = guests,
                ReportingCurrency = cfg.ReportingCurrency,
                TotalFoodCost = decimal.Round(foodTotal, 2),
                TotalBeverageCost = decimal.Round(bevTotal, 2),
                TotalMenuCost = decimal.Round(totalCost, 2),
                FoodCostPerGuest = decimal.Round(foodTotal / guests, 4),
                BeverageCostPerGuest = decimal.Round(bevTotal / guests, 4),
                ExpectedCostPerGuest = decimal.Round(totalCost / guests, 4),
                TotalPackageRevenue = pkgRev,
                ExpectedContribution = pkgRev.HasValue ? decimal.Round(pkgRev.Value - totalCost, 2) : null,
                FoodCostPercent = foodPct,
                FoodCostBasisLabel = foodLabel,
                BeverageCostPercent = bevPct,
                BeverageCostBasisLabel = bevLabel,
                Lines = resultLines,
            };
        }

        public async Task<SilaCulinaryMaterialPriceImpactDto> AnalyzeMaterialPriceImpactAsync(
            Guid organizationId, Guid roleId, SilaCulinaryMaterialPriceImpactRequestDto request, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Material price impact visibility is restricted.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);

            ItemBuyerMaster? mat = await _repository.ItemBuyerMaster
                .FindByCondition(m => m.BuyerId == buyer.Id && m.MaterialCode == request.MaterialCode && m.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            decimal currentPrice = mat?.MovingAveragePrice ?? mat?.UnitCost ?? mat?.StandardPrice ?? 0m;
            List<SilaCulinaryMaterialPriceImpactLineDto> affected = new();

            List<RecipeIngredient> recipeUses = await _repository.RecipeIngredient
                .FindByCondition(ri => ri.ItemCode == request.MaterialCode && ri.IsActive && ri.Recipe.BuyerId == buyer.Id && ri.Recipe.IsActive)
                .Include(ri => ri.Recipe)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (RecipeIngredient ri in recipeUses)
            {
                decimal qty = ri.BaseQuantity > 0 ? ri.BaseQuantity : ri.Quantity;
                decimal curCost = decimal.Round(qty * currentPrice, 2);
                decimal projCost = decimal.Round(qty * request.NewUnitPrice, 2);
                affected.Add(new SilaCulinaryMaterialPriceImpactLineDto
                {
                    RecipeOrEventRef = ri.Recipe.RecipeCode,
                    Name = $"{ri.Recipe.Name} (v{ri.Recipe.Version})",
                    Kind = "RECIPE",
                    RequiredQty = decimal.Round(qty, 4),
                    Uom = string.IsNullOrWhiteSpace(ri.BaseUom) ? ri.Uom : ri.BaseUom,
                    CurrentCost = curCost,
                    ProjectedCost = projCost,
                    CostDelta = projCost - curCost,
                });
            }

            List<SilaCulinaryMrpLine> mrpUses = await _repository.SilaCulinaryMrpLine
                .FindByCondition(ml => ml.BuyerId == buyer.Id && ml.MaterialCode == request.MaterialCode && ml.IsActive && ml.RawQty > 0)
                .Take(30)
                .ToListAsync(cancellationToken);

            foreach (SilaCulinaryMrpLine ml in mrpUses)
            {
                decimal curCost = decimal.Round(ml.RawQty * currentPrice, 2);
                decimal projCost = decimal.Round(ml.RawQty * request.NewUnitPrice, 2);
                affected.Add(new SilaCulinaryMaterialPriceImpactLineDto
                {
                    RecipeOrEventRef = ml.MrpRunId.ToString()[..8],
                    Name = $"Event MRP Line ({ml.RequiredDateBucket})",
                    Kind = "EVENT",
                    RequiredQty = decimal.Round(ml.RawQty, 4),
                    Uom = ml.BaseUom,
                    CurrentCost = curCost,
                    ProjectedCost = projCost,
                    CostDelta = projCost - curCost,
                });
            }

            decimal totCur = affected.Sum(a => a.CurrentCost);
            decimal totProj = affected.Sum(a => a.ProjectedCost);
            return new SilaCulinaryMaterialPriceImpactDto
            {
                MaterialCode = request.MaterialCode,
                MaterialDescription = mat?.Description ?? request.MaterialCode,
                ReportingCurrency = request.Currency ?? cfg.ReportingCurrency,
                CurrentUnitPrice = currentPrice,
                NewUnitPrice = request.NewUnitPrice,
                TotalCurrentCost = totCur,
                TotalProjectedCost = totProj,
                TotalCostDelta = totProj - totCur,
                AffectedItems = affected,
            };
        }

        public async Task<SilaCulinaryFinancialDashboardDto> GetDashboardAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            List<SilaCulinaryEventFinancialPlanSummaryDto> plans = await ListPlansAsync(organizationId, userId, roleId, propertyId, status, false, cancellationToken);
            bool costsVisible = CanViewCosts(roleId);

            int openExceptions = await _repository.SilaCulinaryFinancialException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.ResolutionStatus == "OPEN")
                .CountAsync(cancellationToken);

            decimal totRev = plans.Sum(p => p.NetRevenue ?? 0m);
            decimal totEstCost = plans.Sum(p => p.EstimatedTotalCost ?? 0m);
            decimal totFood = plans.Sum(p => p.EstimatedFoodCost ?? 0m);
            decimal totBev = plans.Sum(p => p.EstimatedBeverageCost ?? 0m);
            decimal totContrib = plans.Sum(p => p.ContributionAfterOverhead ?? 0m);
            decimal totCommit = plans.Sum(p => p.AttributableProcurementCommitment ?? 0m);
            decimal totActualContrib = plans.Where(p => !p.ActualIsProvisional).Sum(p => (p.NetRevenue ?? 0m) - (p.ActualVerifiedCost ?? 0m));

            return new SilaCulinaryFinancialDashboardDto
            {
                ReportingCurrency = cfg.ReportingCurrency,
                TotalPlannedEventRevenue = costsVisible ? totRev : null,
                TotalEstimatedEventCost = costsVisible ? totEstCost : null,
                TotalAttributableCommitments = costsVisible ? totCommit : null,
                TotalForecastEventContribution = costsVisible ? totContrib : null,
                VerifiedActualContribution = costsVisible ? totActualContrib : null,
                WeightedAverageEventMarginPercent = costsVisible && totRev > 0 ? decimal.Round((totContrib / totRev) * 100m, 2) : null,
                WeightedAverageFoodCostPercent = costsVisible && totRev > 0 ? decimal.Round((totFood / totRev) * 100m, 2) : null,
                WeightedAverageBeverageCostPercent = costsVisible && totRev > 0 ? decimal.Round((totBev / totRev) * 100m, 2) : null,
                EventsBelowTargetMarginCount = plans.Count(p => (p.TargetMarginVariancePercent ?? 0m) < 0m),
                EventsExceedingBudgetCount = plans.Count(p => p.RemainingBudget.HasValue && p.RemainingBudget.Value < 0m),
                PendingFinancialApprovalsCount = plans.Count(p => p.Status == "PENDING_APPROVAL"),
                UnresolvedFinancialExceptionsCount = openExceptions,
                Plans = plans,
            };
        }

        public async Task<List<SilaCulinaryEventFinancialPlanSummaryDto>> ListPlansAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, string? status, bool? includeSimulations, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryEventFinancialPlan> q = _repository.SilaCulinaryEventFinancialPlan
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId.Value);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status.Trim().ToUpperInvariant());
            if (includeSimulations != true) q = q.Where(x => !x.IsSimulation);

            List<SilaCulinaryEventFinancialPlan> rows = await q.OrderByDescending(x => x.CalculatedAtUtc).Take(100).ToListAsync(cancellationToken);
            bool costsVisible = CanViewCosts(roleId);
            return rows.Select(p => ToSummaryDto(p, costsVisible)).ToList();
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> GetPlanAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> CreateAndCalculatePlanAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateEventFinancialPlanRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);

            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                SilaCulinaryEventFinancialPlan? existing = await _repository.SilaCulinaryEventFinancialPlan
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IdempotencyKey == request.IdempotencyKey && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existing != null)
                    return await BuildDetailDtoAsync(buyer.Id, userId, roleId, existing, cancellationToken);
            }

            SilaCulinaryEvent? evt = request.EventId.HasValue
                ? await _repository.SilaCulinaryEvent.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.EventId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken)
                : null;

            SilaCulinaryProcurementPlan? procPlan = request.ProcurementPlanId.HasValue
                ? await _repository.SilaCulinaryProcurementPlan.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.ProcurementPlanId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken)
                : (evt != null ? await _repository.SilaCulinaryProcurementPlan.FindByCondition(x => x.BuyerId == buyer.Id && x.PrimaryEventId == evt.Id && x.IsActive).OrderByDescending(x => x.VersionNumber).FirstOrDefaultAsync(cancellationToken) : null);

            int guests = request.PlannedGuests > 0 ? request.PlannedGuests : (evt?.PlanningGuests is > 0 ? evt.PlanningGuests : 500);
            int guaranteed = request.GuaranteedGuests > 0 ? request.GuaranteedGuests : (evt?.GuaranteedGuests is > 0 ? evt.GuaranteedGuests : guests);
            decimal sellingPrice = request.SellingPricePerGuest > 0 ? request.SellingPricePerGuest : (evt?.NetPricePerGuest is > 0 ? evt.NetPricePerGuest : 350m);
            string ccy = string.IsNullOrWhiteSpace(request.ReportingCurrency) ? (evt?.Currency ?? cfg.ReportingCurrency) : request.ReportingCurrency.Trim().ToUpperInvariant();

            string code = $"FIN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
            SilaCulinaryEventFinancialPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PlanCode = code,
                Name = string.IsNullOrWhiteSpace(request.Name) ? (evt != null ? $"{evt.Name} Financial Plan" : "Grand Corporate Gala Financial Plan") : request.Name.Trim(),
                VersionNumber = request.VersionNumber is > 0 ? request.VersionNumber.Value : 1,
                EventId = evt?.Id ?? request.EventId,
                EventCode = evt?.EventCode ?? (request.IsDemo ? "EVT-GALA-DEMO" : null),
                EventName = evt?.Name ?? (request.IsDemo ? "Grand Corporate Gala" : request.Name),
                EventRevision = evt?.RevisionNumber ?? 1,
                MenuId = evt?.PrimaryMenuId,
                MrpRunId = procPlan?.MrpRunId,
                MrpVersion = procPlan?.MrpVersion,
                ProcurementPlanId = procPlan?.Id,
                ProcurementPlanVersion = procPlan?.VersionNumber,
                PropertyId = evt?.PropertyId ?? procPlan?.PropertyId,
                PropertyName = procPlan?.PropertyName ?? "SILA Grand Flagship",
                CompanyCode = evt?.CompanyCode ?? procPlan?.CompanyCode,
                VenueName = evt?.VenueName ?? procPlan?.VenueName ?? "Grand Ballroom",
                CustomerReference = evt?.CustomerReference ?? (request.IsDemo ? "CORP-GALA-2026" : null),
                EventDateUtc = evt?.StartDateUtc ?? DateTime.UtcNow.AddDays(14),
                ExpectedGuests = evt?.ExpectedGuests > 0 ? evt.ExpectedGuests : guests,
                GuaranteedGuests = guaranteed,
                PlannedGuests = guests,
                ActualGuests = request.ActualGuests,
                BillablePolicy = string.IsNullOrWhiteSpace(request.BillablePolicy) ? (evt?.BillablePolicy ?? "GUARANTEED_PLUS_ADDITIONAL") : request.BillablePolicy.Trim().ToUpperInvariant(),
                ReportingCurrency = ccy,
                Status = "CALCULATED",
                IsSimulation = request.IsSimulation,
                IsDemo = request.IsDemo,
                CorrelationId = Guid.NewGuid().ToString("N"),
                IdempotencyKey = request.IdempotencyKey,
                TargetFoodCostPercent = request.TargetFoodCostPercent ?? (evt?.TargetFoodCostPercent > 0 ? evt.TargetFoodCostPercent : cfg.TargetFoodCostPercent),
                TargetBeverageCostPercent = request.TargetBeverageCostPercent ?? (evt?.TargetBeverageCostPercent > 0 ? evt.TargetBeverageCostPercent : cfg.TargetBeverageCostPercent),
                TargetContributionMarginPercent = request.TargetContributionMarginPercent ?? cfg.TargetEventContributionMarginPercent,
                OriginalBudget = request.OriginalBudget ?? evt?.ApprovedBudget ?? 105000m,
                RevisedBudget = request.OriginalBudget ?? evt?.ApprovedBudget ?? 105000m,
                CostCenter = request.CostCenter,
                ProfitCenter = request.ProfitCenter,
                InternalOrder = request.InternalOrder,
                WbsElement = request.WbsElement,
                FinancialOwner = actorName,
                CalculatedAtUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            };

            _repository.SilaCulinaryEventFinancialPlan.Create(plan);

            // Initial Revenue Line
            decimal grossRev = guests * sellingPrice;
            decimal discAmt = decimal.Round(grossRev * (request.DiscountPercent / 100m), 2);
            _repository.SilaCulinaryEventRevenueLine.Create(new SilaCulinaryEventRevenueLine
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                EventFinancialPlanId = plan.Id,
                Category = "EVENT_PACKAGE",
                Description = $"{plan.EventName ?? plan.Name} Package ({guests} guests @ {ccy} {sellingPrice:0.##})",
                RevenueStatus = "PLANNED",
                BillingBasis = "GUARANTEED_RULE",
                Quantity = guests,
                UnitPrice = sellingPrice,
                Currency = ccy,
                ExchangeRate = 1m,
                GrossAmount = grossRev,
                DiscountAmount = discAmt,
                NetRevenue = grossRev - discAmt,
                SourceReference = evt?.EventCode ?? plan.PlanCode,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            });

            // Initial F&B + Direct Expenses (from Menu/MRP or §53 illustrative structure if demo/defaults)
            decimal foodCostGuest = request.FoodCostPerGuest ?? 90m;
            decimal bevCostGuest = request.BeverageCostPerGuest ?? 25m;
            if (evt?.PrimaryMenuId != null && !request.FoodCostPerGuest.HasValue)
            {
                SilaCulinaryMenu? menu = await _repository.SilaCulinaryMenu.FindByCondition(m => m.BuyerId == buyer.Id && m.Id == evt.PrimaryMenuId.Value && m.IsActive).FirstOrDefaultAsync(cancellationToken);
                if (menu != null && menu.TotalEstimatedCost > 0 && guests > 0)
                {
                    foodCostGuest = menu.TotalFoodCost / guests;
                    bevCostGuest = menu.TotalBeverageCost / guests;
                }
            }

            CreateSeedExpense(buyer.Id, plan.Id, "EXP-FOOD", "FOOD", "Food Ingredients (Menu/MRP Cost)", "VARIABLE_PER_GUEST", guests, foodCostGuest, ccy, request.CostCenter);
            CreateSeedExpense(buyer.Id, plan.Id, "EXP-BEV", "BEVERAGE", "Beverage Ingredients (Package Cost)", "VARIABLE_PER_GUEST", guests, bevCostGuest, ccy, request.CostCenter);

            if (request.IsDemo || evt == null)
            {
                CreateSeedExpense(buyer.Id, plan.Id, "EXP-MP", "MANPOWER", "Service & Kitchen Staff", "FIXED", 1, 15000m, ccy, request.CostCenter, staffCount: 25, hours: 8, hourlyRate: 75m);
                CreateSeedExpense(buyer.Id, plan.Id, "EXP-DEC", "DECORATION", "Stage & Floral Decoration", "FIXED", 1, 10000m, ccy, request.CostCenter);
                CreateSeedExpense(buyer.Id, plan.Id, "EXP-EQP", "EQUIPMENT", "AV, Lighting & Tableware", "FIXED", 1, 5000m, ccy, request.CostCenter);
                CreateSeedExpense(buyer.Id, plan.Id, "EXP-OTH", "OTHER", "Logistics & Permits", "FIXED", 1, 5000m, ccy, request.CostCenter);
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_PLAN_CREATED,
                $"Created Event Financial Plan {plan.PlanCode} for {guests} guests ({ccy}).", userId);
            await _repository.SaveAsync();

            await RecomputeAndPersistPlanAsync(buyer.Id, cfg, plan, userId, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        private void CreateSeedExpense(
            Guid buyerId, Guid planId, string code, string cat, string desc, string behavior,
            decimal qty, decimal rate, string ccy, string? costCenter,
            int? staffCount = null, decimal? hours = null, decimal? hourlyRate = null)
        {
            decimal est = decimal.Round(qty * rate, 2);
            _repository.SilaCulinaryEventExpenseLine.Create(new SilaCulinaryEventExpenseLine
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                EventFinancialPlanId = planId,
                ExpenseCode = code,
                Category = cat,
                Description = desc,
                CostBehavior = behavior,
                StaffCount = staffCount,
                PlannedHours = hours,
                HourlyRate = hourlyRate,
                Quantity = qty,
                Uom = behavior == "VARIABLE_PER_GUEST" ? "GUEST" : "LOT",
                UnitRate = rate,
                Currency = ccy,
                ExchangeRate = 1m,
                BudgetAmount = est,
                EstimatedAmount = est,
                CostCenter = costCenter,
                Status = "ESTIMATED",
                ExpenseDateUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            });
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> RecalculatePlanAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            await RecomputeAndPersistPlanAsync(buyer.Id, cfg, plan, userId, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        private async Task RecomputeAndPersistPlanAsync(
            Guid buyerId, SilaCulinaryFinancialConfig cfg, SilaCulinaryEventFinancialPlan plan, Guid actorUserId, CancellationToken cancellationToken)
        {
            SilaCulinaryFinancialConfigDto cfgDto = ToConfigDto(cfg);
            List<SilaCulinaryEventRevenueLine> revLines = await _repository.SilaCulinaryEventRevenueLine
                .FindByCondition(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryEventExpenseLine> expLines = await _repository.SilaCulinaryEventExpenseLine
                .FindByCondition(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            // Resolve billable guests
            int billable = plan.BillablePolicy switch
            {
                "GUARANTEED_ONLY" => plan.GuaranteedGuests > 0 ? plan.GuaranteedGuests : plan.PlannedGuests,
                "ACTUAL_ATTENDANCE" => plan.ActualGuests ?? plan.PlannedGuests,
                _ => Math.Max(plan.GuaranteedGuests > 0 ? plan.GuaranteedGuests : plan.PlannedGuests, plan.ActualGuests ?? plan.PlannedGuests),
            };
            plan.BillableGuests = billable;

            decimal grossRev = 0m, discTot = 0m, netRev = 0m;
            foreach (SilaCulinaryEventRevenueLine rl in revLines)
            {
                decimal q = rl.BillingBasis == "GUARANTEED_RULE" ? billable : rl.Quantity;
                decimal g = decimal.Round(q * rl.UnitPrice * rl.ExchangeRate, 2);
                decimal d = decimal.Round(rl.DiscountAmount * rl.ExchangeRate, 2);
                grossRev += g;
                discTot += d;
                netRev += Math.Max(0m, g - d);
            }

            decimal estFood = 0m, estBev = 0m, estMp = 0m, estEq = 0m, estDec = 0m, estExt = 0m, estOther = 0m, estOh = 0m;
            decimal fixedTotal = 0m, varTotal = 0m, actualExpenses = 0m, committedExpenses = 0m;
            bool hasActualExpenses = false;
            HashSet<string> seenPools = new();

            foreach (SilaCulinaryEventExpenseLine ex in expLines)
            {
                if (!string.IsNullOrWhiteSpace(ex.SourceCostPoolId))
                {
                    if (!seenPools.Add(ex.SourceCostPoolId))
                        continue; // Prevent duplicate overhead pool allocation (§27)
                }

                decimal amt = ex.EstimatedAmount * ex.ExchangeRate;
                if (ex.CostBehavior is "FIXED" or "MIXED") fixedTotal += amt;
                else varTotal += amt;

                switch (ex.Category.ToUpperInvariant())
                {
                    case "FOOD": estFood += amt; break;
                    case "BEVERAGE": estBev += amt; break;
                    case "MANPOWER": estMp += amt; break;
                    case "EQUIPMENT": estEq += amt; break;
                    case "DECORATION": estDec += amt; break;
                    case "EXTERNAL_SERVICE": estExt += amt; break;
                    case "OVERHEAD": estOh += amt; break;
                    default: estOther += amt; break;
                }

                committedExpenses += ex.CommittedAmount * ex.ExchangeRate;
                if (ex.ActualVerifiedAmount.HasValue)
                {
                    hasActualExpenses = true;
                    actualExpenses += ex.ActualVerifiedAmount.Value * ex.ExchangeRate;
                }
            }

            // Approved tenant overhead rules (duplicate-pool guarded)
            decimal estDirect = estFood + estBev + estMp + estEq + estDec + estExt + estOther;
            foreach (SilaCulinaryOverheadRuleDto rule in cfgDto.OverheadRules.Where(r => r.IsApproved))
            {
                string key = string.IsNullOrWhiteSpace(rule.SourcePoolId) ? rule.RuleId : rule.SourcePoolId;
                if (!seenPools.Add(key)) continue;
                decimal ohVal = rule.Method switch
                {
                    "PCT_NET_REVENUE" => netRev * (rule.RateOrAmount / 100m),
                    "PCT_DIRECT_COST" => estDirect * (rule.RateOrAmount / 100m),
                    "PER_GUEST" => plan.PlannedGuests * rule.RateOrAmount,
                    _ => rule.RateOrAmount,
                };
                estOh += decimal.Round(ohVal, 2);
                fixedTotal += decimal.Round(ohVal, 2);
            }

            // Procurement Commitments & Purchase-vs-Consumption Separation (Phase 2C handover)
            List<SilaCulinaryProcurementFinancialLineDto> procLines = await LoadProcurementCommitmentsAsync(buyerId, plan, CancellationToken.None);
            decimal approvedPoCommit = procLines.Where(p => p.PoStatus is "APPROVED" or "RELEASED" or "RECEIVED").Sum(p => p.PurchaseValue);
            decimal attrPoCommit = procLines.Where(p => p.PoStatus is "APPROVED" or "RELEASED" or "RECEIVED").Sum(p => p.AttributableEventCommitmentValue);
            decimal expConsPo = procLines.Sum(p => p.AttributableEventCommitmentValue);
            decimal remInvPo = procLines.Sum(p => p.RemainingInventoryValue);

            decimal estTotal = estDirect + estOh;
            decimal totalAttrCommit = attrPoCommit + committedExpenses;
            decimal remainingCommit = Math.Max(0m, totalAttrCommit - actualExpenses);
            decimal remainingUncommitted = Math.Max(0m, estTotal - actualExpenses - remainingCommit);
            decimal fac = hasActualExpenses ? (actualExpenses + remainingCommit + remainingUncommitted) : estTotal;

            decimal directContrib = netRev - estDirect;
            decimal contribAfterOh = netRev - estTotal;

            plan.GrossRevenue = decimal.Round(grossRev, 2);
            plan.DiscountTotal = decimal.Round(discTot, 2);
            plan.NetRevenue = decimal.Round(netRev, 2);
            plan.EstimatedFoodCost = decimal.Round(estFood, 2);
            plan.EstimatedBeverageCost = decimal.Round(estBev, 2);
            plan.EstimatedManpowerCost = decimal.Round(estMp, 2);
            plan.EstimatedEquipmentCost = decimal.Round(estEq, 2);
            plan.EstimatedDecorationCost = decimal.Round(estDec, 2);
            plan.EstimatedExternalServiceCost = decimal.Round(estExt, 2);
            plan.EstimatedOtherDirectCost = decimal.Round(estOther, 2);
            plan.EstimatedAllocatedOverhead = decimal.Round(estOh, 2);
            plan.EstimatedTotalCost = decimal.Round(estTotal, 2);
            plan.ApprovedProcurementCommitment = decimal.Round(approvedPoCommit + committedExpenses, 2);
            plan.AttributableProcurementCommitment = decimal.Round(totalAttrCommit, 2);
            plan.ExpectedConsumptionValueFromPo = decimal.Round(expConsPo, 2);
            plan.RemainingUnallocatedInventoryValue = decimal.Round(remInvPo, 2);
            plan.ActualVerifiedCost = decimal.Round(actualExpenses, 2);
            plan.ActualIsProvisional = !hasActualExpenses;
            plan.ForecastAtCompletionCost = decimal.Round(fac, 2);
            plan.DirectContribution = decimal.Round(directContrib, 2);
            plan.ContributionAfterOverhead = decimal.Round(contribAfterOh, 2);
            plan.ContributionMarginPercent = netRev > 0 ? decimal.Round((contribAfterOh / netRev) * 100m, 2) : null;
            plan.FoodCostPercent = netRev > 0 ? decimal.Round((estFood / netRev) * 100m, 2) : null;
            plan.BeverageCostPercent = netRev > 0 ? decimal.Round((estBev / netRev) * 100m, 2) : null;

            decimal pricePerGuest = billable > 0 ? netRev / billable : 0m;
            decimal varPerGuest = plan.PlannedGuests > 0 ? varTotal / plan.PlannedGuests : 0m;
            decimal unitContrib = pricePerGuest - varPerGuest;
            plan.BreakEvenGuests = unitContrib > 0 ? (int)Math.Ceiling(fixedTotal / unitContrib) : null;

            decimal denom = 1m - (plan.TargetContributionMarginPercent / 100m);
            plan.MaxAllowableCost = decimal.Round(netRev * denom, 2);
            plan.RemainingCostHeadroom = decimal.Round(plan.MaxAllowableCost - estTotal, 2);
            plan.CalculatedAtUtc = DateTime.UtcNow;
            plan.DateUpdated = DateTime.UtcNow;

            // Refresh open system exceptions
            await SyncSystemExceptionsAsync(buyerId, plan, actorUserId, cancellationToken);
            await _repository.SaveAsync();
        }

        private async Task<List<SilaCulinaryProcurementFinancialLineDto>> LoadProcurementCommitmentsAsync(
            Guid buyerId, SilaCulinaryEventFinancialPlan plan, CancellationToken cancellationToken)
        {
            if (!plan.ProcurementPlanId.HasValue) return new();

            List<SilaCulinaryProcurementAllocation> allocs = await _repository.SilaCulinaryProcurementAllocation
                .FindByCondition(a => a.BuyerId == buyerId && a.ProcurementPlanId == plan.ProcurementPlanId.Value && a.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProcurementLine> procLines = await _repository.SilaCulinaryProcurementLine
                .FindByCondition(l => l.BuyerId == buyerId && l.ProcurementPlanId == plan.ProcurementPlanId.Value && l.IsActive)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryProcurementFinancialLineDto> result = new();
            foreach (SilaCulinaryProcurementLine pl in procLines)
            {
                decimal unitCost = pl.SupplierUnitPrice ?? 0m;
                decimal purchasedQty = pl.RecommendedPurchaseQty > 0 ? pl.RecommendedPurchaseQty : pl.IncomingPoAllocatedQty;
                decimal allocQty = Math.Min(purchasedQty, pl.GrossRequirementQty);
                decimal unallocQty = Math.Max(0m, purchasedQty - allocQty);
                SilaCulinaryProcurementAllocation? linkedAlloc = allocs.FirstOrDefault(a => a.ProcurementLineId == pl.Id);

                result.Add(new SilaCulinaryProcurementFinancialLineDto
                {
                    MaterialCode = pl.MaterialCode ?? pl.Id.ToString()[..8],
                    MaterialDescription = pl.MaterialDescription,
                    PoNumber = linkedAlloc?.PoNumber,
                    PoStatus = linkedAlloc?.Status ?? "PROPOSED",
                    PurchasedQty = purchasedQty,
                    EventAllocatedQty = allocQty,
                    OtherEventsAllocatedQty = 0m,
                    UnallocatedInventoryQty = unallocQty,
                    Uom = pl.BaseUom,
                    UnitCost = unitCost,
                    PurchaseValue = decimal.Round(purchasedQty * unitCost, 2),
                    AttributableEventCommitmentValue = decimal.Round(allocQty * unitCost, 2),
                    RemainingInventoryValue = decimal.Round(unallocQty * unitCost, 2),
                    ReceivedValue = linkedAlloc?.Status == "RECEIVED" ? decimal.Round(allocQty * unitCost, 2) : 0m,
                    InvoicedValue = 0m,
                    CostCenter = plan.CostCenter,
                });
            }
            return result;
        }

        private async Task SyncSystemExceptionsAsync(
            Guid buyerId, SilaCulinaryEventFinancialPlan plan, Guid actorUserId, CancellationToken cancellationToken)
        {
            List<SilaCulinaryFinancialException> existingOpen = await _repository.SilaCulinaryFinancialException
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive && x.ResolutionStatus == "OPEN")
                .ToListAsync(cancellationToken);
            foreach (SilaCulinaryFinancialException old in existingOpen)
            {
                old.IsActive = false;
                old.DateUpdated = DateTime.UtcNow;
            }

            List<(string Type, string Severity, decimal? Impact, string Msg, string Action)> candidates = new();
            if (plan.ContributionAfterOverhead < 0)
                candidates.Add(("NEGATIVE_CONTRIBUTION", "CRITICAL", plan.ContributionAfterOverhead, $"Event contribution is negative ({plan.ReportingCurrency} {plan.ContributionAfterOverhead:0.00}).", "Review selling price or direct costs."));
            if (plan.RevisedBudget > 0 && plan.EstimatedTotalCost > plan.RevisedBudget)
                candidates.Add(("BUDGET_OVERRUN", "HIGH", plan.EstimatedTotalCost - plan.RevisedBudget, $"Estimated cost exceeds budget by {plan.ReportingCurrency} {plan.EstimatedTotalCost - plan.RevisedBudget:0.00}.", "Submit budget revision for approval."));
            if (plan.ContributionMarginPercent.HasValue && plan.ContributionMarginPercent.Value < plan.TargetContributionMarginPercent)
                candidates.Add(("MARGIN_BELOW_TARGET", "WARNING", null, $"Contribution margin {plan.ContributionMarginPercent:0.00}% is below target {plan.TargetContributionMarginPercent:0.00}%.", "Simulate price/cost adjustments."));
            if (plan.ActualIsProvisional)
                candidates.Add(("ACTUAL_CONSUMPTION_UNAVAILABLE", "INFO", null, "Actual consumption reconciliation is provisional pending Phase 4B event closure.", "Enter verified actual expenses or await Phase 4B."));

            foreach (var c in candidates)
            {
                _repository.SilaCulinaryFinancialException.Create(new SilaCulinaryFinancialException
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    EventFinancialPlanId = plan.Id,
                    ExceptionType = c.Type,
                    Severity = c.Severity,
                    FinancialImpact = c.Impact,
                    Message = c.Msg,
                    RecommendedAction = c.Action,
                    ResponsibleUser = plan.FinancialOwner,
                    ResolutionStatus = "OPEN",
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                });

                if (c.Severity is "CRITICAL" or "HIGH")
                {
                    await UpsertNotificationAsync(buyerId, actorUserId, $"CULINARY_FIN_{c.Type}",
                        $"FIN_{plan.Id}_{c.Type}", $"{c.Type}: {plan.PlanCode}", c.Msg, c.Severity, plan.Id, plan.PlanCode, cancellationToken);
                }
            }
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> AddRevenueLineAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryAddRevenueLineRequestDto request, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Only authorized users may add revenue lines.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            string ccy = string.IsNullOrWhiteSpace(request.Currency) ? plan.ReportingCurrency : request.Currency.Trim().ToUpperInvariant();
            List<SilaCulinaryFxRateDto> fxRates = DeserializeOrDefault(cfg.ExchangeRatesJson, new List<SilaCulinaryFxRateDto>());
            decimal? fx = ResolveFxRate(ccy, plan.ReportingCurrency, fxRates);
            if (!fx.HasValue)
                throw new BadRequestCustomException("Missing FX rate.", $"No exchange rate configured from {ccy} to {plan.ReportingCurrency}.");

            decimal gross = decimal.Round(request.Quantity * request.UnitPrice * fx.Value, 2);
            decimal disc = decimal.Round(request.DiscountAmount * fx.Value, 2);
            decimal net = Math.Max(0m, gross - disc);

            _repository.SilaCulinaryEventRevenueLine.Create(new SilaCulinaryEventRevenueLine
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                EventFinancialPlanId = plan.Id,
                Category = request.Category.Trim().ToUpperInvariant(),
                Description = request.Description.Trim(),
                RevenueStatus = request.RevenueStatus.Trim().ToUpperInvariant(),
                BillingBasis = request.BillingBasis.Trim().ToUpperInvariant(),
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                Currency = ccy,
                ExchangeRate = fx.Value,
                GrossAmount = gross,
                DiscountAmount = disc,
                TaxAmount = request.TaxAmount,
                NetRevenue = net,
                SourceReference = request.SourceReference,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_REV_CREATED,
                $"Added revenue line '{request.Description}' ({plan.ReportingCurrency} {net:0.00}) by {actorName}.", userId);
            await _repository.SaveAsync();
            await RecomputeAndPersistPlanAsync(buyer.Id, cfg, plan, userId, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> AddExpenseLineAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, SilaCulinaryAddExpenseLineRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            string ccy = string.IsNullOrWhiteSpace(request.Currency) ? plan.ReportingCurrency : request.Currency.Trim().ToUpperInvariant();
            List<SilaCulinaryFxRateDto> fxRates = DeserializeOrDefault(cfg.ExchangeRatesJson, new List<SilaCulinaryFxRateDto>());
            decimal? fx = ResolveFxRate(ccy, plan.ReportingCurrency, fxRates);
            if (!fx.HasValue)
                throw new BadRequestCustomException("Missing FX rate.", $"No exchange rate configured from {ccy} to {plan.ReportingCurrency}.");

            if (!string.IsNullOrWhiteSpace(request.SourceCostPoolId))
            {
                bool dupPool = await _repository.SilaCulinaryEventExpenseLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.EventFinancialPlanId == plan.Id && x.IsActive && x.SourceCostPoolId == request.SourceCostPoolId)
                    .AnyAsync(cancellationToken);
                if (dupPool)
                    throw new BadRequestCustomException("Duplicate overhead pool.", $"Cost pool {request.SourceCostPoolId} is already allocated to this event.");
            }

            decimal est = request.EstimatedAmount ?? 0m;
            if (est == 0m && request.Category.Equals("MANPOWER", StringComparison.OrdinalIgnoreCase) && request.StaffCount.HasValue && request.PlannedHours.HasValue && request.HourlyRate.HasValue)
            {
                est = (request.StaffCount.Value * request.PlannedHours.Value * request.HourlyRate.Value)
                    + (request.StaffCount.Value * (request.OvertimeHours ?? 0m) * (request.OvertimeRate ?? 0m))
                    + (request.FixedShiftCost ?? 0m)
                    + (request.ExternalAgencyCharge ?? 0m);
            }
            else if (est == 0m)
            {
                est = request.Quantity * request.UnitRate;
            }

            string code = $"EXP-{DateTime.UtcNow:HHmmss}-{Guid.NewGuid().ToString()[..4].ToUpperInvariant()}";
            _repository.SilaCulinaryEventExpenseLine.Create(new SilaCulinaryEventExpenseLine
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                EventFinancialPlanId = plan.Id,
                ExpenseCode = code,
                Category = request.Category.Trim().ToUpperInvariant(),
                Description = request.Description.Trim(),
                CostBehavior = request.CostBehavior.Trim().ToUpperInvariant(),
                StepThresholdGuests = request.StepThresholdGuests,
                StepIncrementCost = request.StepIncrementCost,
                SupplierOrEmployeeRef = request.SupplierOrEmployeeRef,
                EmployeeRole = request.EmployeeRole,
                StaffCount = request.StaffCount,
                PlannedHours = request.PlannedHours,
                HourlyRate = request.HourlyRate,
                OvertimeHours = request.OvertimeHours,
                OvertimeRate = request.OvertimeRate,
                FixedShiftCost = request.FixedShiftCost,
                ExternalAgencyCharge = request.ExternalAgencyCharge,
                Quantity = request.Quantity,
                Uom = request.Uom,
                UnitRate = request.UnitRate,
                Currency = ccy,
                ExchangeRate = fx.Value,
                BudgetAmount = request.BudgetAmount > 0 ? request.BudgetAmount : est,
                EstimatedAmount = decimal.Round(est, 2),
                CommittedAmount = request.CommittedAmount,
                ActualVerifiedAmount = request.MarkAsActualVerified ? (request.ActualVerifiedAmount ?? est) : request.ActualVerifiedAmount,
                TaxTreatment = request.TaxTreatment,
                CostCenter = request.CostCenter ?? plan.CostCenter,
                GlAccount = request.GlAccount,
                InternalOrder = request.InternalOrder ?? plan.InternalOrder,
                WbsElement = request.WbsElement ?? plan.WbsElement,
                SupportingDocumentRef = request.SupportingDocumentRef,
                SourceCostPoolId = request.SourceCostPoolId,
                Status = request.MarkAsActualVerified ? "ACTUAL_VERIFIED" : "ESTIMATED",
                ExpenseDateUtc = DateTime.UtcNow,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow,
                IsActive = true,
            });

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_EXP_CREATED,
                $"Added expense {code} ({request.Category}: {request.Description}) by {actorName}.", userId);
            await _repository.SaveAsync();
            await RecomputeAndPersistPlanAsync(buyer.Id, cfg, plan, userId, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> ApproveExpenseLineAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid expenseId, CancellationToken cancellationToken)
        {
            if (!CanApproveFinancial(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Only authorized financial managers may approve event expenses.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialConfig cfg = await GetOrCreateConfigEntityAsync(buyer.Id, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            SilaCulinaryEventExpenseLine ex = await _repository.SilaCulinaryEventExpenseLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.EventFinancialPlanId == plan.Id && x.Id == expenseId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Expense not found.", "Expense line was not found.");

            ex.Status = "APPROVED";
            ex.ApprovalReference = $"EXP-APR-{ex.ExpenseCode}";
            ex.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_EXP_APPROVED,
                $"Approved expense {ex.ExpenseCode} by {actorName}.", userId);
            await _repository.SaveAsync();
            await RecomputeAndPersistPlanAsync(buyer.Id, cfg, plan, userId, cancellationToken);
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinarySimulateFinancialResultDto> SimulateAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, SilaCulinarySimulateFinancialRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);

            int guests = Math.Max(1, request.PlannedGuests);
            int billable = Math.Max(guests, request.GuaranteedGuests);
            decimal grossRev = (billable * request.SellingPricePerGuest) + request.FixedEventRevenue + request.AdditionalGuestRevenue;
            decimal disc = decimal.Round(grossRev * (request.DiscountPercent / 100m), 2);
            decimal netRev = Math.Max(0m, grossRev - disc);

            decimal priceScale = 1m + (request.IngredientPriceChangePercent / 100m);
            decimal varFood = decimal.Round(guests * request.FoodCostPerGuest * priceScale, 2);
            decimal varBev = decimal.Round(guests * request.BeverageCostPerGuest * priceScale, 2);
            decimal varTotal = varFood + varBev;
            decimal fixedTotal = request.ManpowerCost + request.EquipmentCost + request.DecorationCost + request.ExternalServiceCost + request.OtherDirectCost + request.AllocatedOverhead;
            decimal totCost = varTotal + fixedTotal;
            decimal contrib = netRev - totCost;
            decimal marginPct = netRev > 0 ? decimal.Round((contrib / netRev) * 100m, 2) : 0m;
            decimal foodPct = netRev > 0 ? decimal.Round((varFood / netRev) * 100m, 2) : 0m;
            decimal bevPct = netRev > 0 ? decimal.Round((varBev / netRev) * 100m, 2) : 0m;
            decimal costPerGuest = decimal.Round(totCost / guests, 2);
            decimal contribPerGuest = decimal.Round(contrib / guests, 2);

            decimal varPerGuest = varTotal / guests;
            decimal effPrice = netRev / billable;
            decimal unitContrib = effPrice - varPerGuest;
            int? beGuests = unitContrib > 0 ? (int)Math.Ceiling(fixedTotal / unitContrib) : null;
            decimal? beRev = beGuests.HasValue ? decimal.Round(beGuests.Value * effPrice, 2) : null;

            decimal denom = 1m - (request.TargetContributionMarginPercent / 100m);
            decimal reqPrice = denom > 0 ? decimal.Round((totCost / denom) / guests, 2) : 0m;
            decimal maxAllowable = decimal.Round(netRev * denom, 2);
            decimal headroom = decimal.Round(maxAllowable - totCost, 2);

            Guid? savedScenarioId = null;
            if (request.SaveScenario)
            {
                SilaCulinaryFinancialScenario sc = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    EventFinancialPlanId = plan.Id,
                    Name = string.IsNullOrWhiteSpace(request.ScenarioName) ? $"Scenario {DateTime.UtcNow:HH:mm}" : request.ScenarioName.Trim(),
                    ScenarioType = string.IsNullOrWhiteSpace(request.ScenarioType) ? "CUSTOM" : request.ScenarioType.Trim().ToUpperInvariant(),
                    SimulatedGuests = guests,
                    SimulatedRevenue = decimal.Round(netRev, 2),
                    SimulatedTotalCost = decimal.Round(totCost, 2),
                    SimulatedContribution = decimal.Round(contrib, 2),
                    SimulatedMarginPercent = marginPct,
                    SimulatedBreakEvenGuests = beGuests,
                    ParametersJson = JsonSerializer.Serialize(request, CamelJson),
                    ResultsJson = JsonSerializer.Serialize(new { netRev, totCost, contrib, marginPct, beGuests, reqPrice, maxAllowable, headroom }, CamelJson),
                    CreatedByUserId = userId,
                    DateCreated = DateTime.UtcNow,
                    DateUpdated = DateTime.UtcNow,
                    IsActive = true,
                };
                _repository.SilaCulinaryFinancialScenario.Create(sc);
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_SCENARIO,
                    $"Saved financial scenario '{sc.Name}' ({guests} guests, margin {marginPct:0.00}%).", userId);
                await _repository.SaveAsync();
                savedScenarioId = sc.Id;
            }

            return new SilaCulinarySimulateFinancialResultDto
            {
                SavedScenarioId = savedScenarioId,
                ReportingCurrency = plan.ReportingCurrency,
                PlannedGuests = guests,
                GrossRevenue = decimal.Round(grossRev, 2),
                DiscountTotal = disc,
                NetRevenue = decimal.Round(netRev, 2),
                VariableCosts = varTotal,
                FixedCosts = fixedTotal,
                TotalEventCost = totCost,
                EventContribution = contrib,
                ContributionMarginPercent = marginPct,
                FoodCostPercent = foodPct,
                BeverageCostPercent = bevPct,
                CostPerGuest = costPerGuest,
                ContributionPerGuest = contribPerGuest,
                BreakEvenGuests = beGuests,
                BreakEvenRevenue = beRev,
                RequiredSellingPricePerGuest = reqPrice,
                MaxAllowableCost = maxAllowable,
                BudgetHeadroom = headroom,
                Waterfall = new List<SilaCulinaryWaterfallStepDto>
                {
                    new() { Label = "Net Event Revenue", Amount = decimal.Round(netRev, 2), Kind = "REVENUE" },
                    new() { Label = "Food Cost", Amount = -varFood, Kind = "COST" },
                    new() { Label = "Beverage Cost", Amount = -varBev, Kind = "COST" },
                    new() { Label = "Manpower", Amount = -request.ManpowerCost, Kind = "COST" },
                    new() { Label = "Decoration", Amount = -request.DecorationCost, Kind = "COST" },
                    new() { Label = "Equipment", Amount = -request.EquipmentCost, Kind = "COST" },
                    new() { Label = "Other Direct", Amount = -(request.ExternalServiceCost + request.OtherDirectCost), Kind = "COST" },
                    new() { Label = "Allocated Overhead", Amount = -request.AllocatedOverhead, Kind = "OVERHEAD" },
                    new() { Label = "Event Contribution", Amount = contrib, Kind = "CONTRIBUTION" },
                },
            };
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> PromoteScenarioToRevisionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid scenarioId, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Only authorized users may promote financial scenarios.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan oldPlan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            SilaCulinaryFinancialScenario sc = await _repository.SilaCulinaryFinancialScenario
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.EventFinancialPlanId == oldPlan.Id && x.Id == scenarioId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Scenario not found.", "Financial scenario was not found.");

            SilaCulinarySimulateFinancialRequestDto p = DeserializeOrDefault(sc.ParametersJson, new SilaCulinarySimulateFinancialRequestDto());
            sc.PromotedToRevision = true;
            sc.DateUpdated = DateTime.UtcNow;

            if (oldPlan.Status == "APPROVED")
            {
                oldPlan.Status = "SUPERSEDED";
                oldPlan.DateUpdated = DateTime.UtcNow;
                await _repository.SaveAsync();
            }

            SilaCulinaryEventFinancialPlanDetailDto created = await CreateAndCalculatePlanAsync(
                organizationId, userId, roleId, actorName,
                new SilaCulinaryCreateEventFinancialPlanRequestDto
                {
                    EventId = oldPlan.EventId,
                    ProcurementPlanId = oldPlan.ProcurementPlanId,
                    Name = $"{oldPlan.Name} (From {sc.Name})",
                    VersionNumber = oldPlan.VersionNumber + 1,
                    PlannedGuests = p.PlannedGuests,
                    GuaranteedGuests = p.GuaranteedGuests,
                    SellingPricePerGuest = p.SellingPricePerGuest,
                    DiscountPercent = p.DiscountPercent,
                    FoodCostPerGuest = p.FoodCostPerGuest,
                    BeverageCostPerGuest = p.BeverageCostPerGuest,
                    OriginalBudget = oldPlan.OriginalBudget,
                    TargetContributionMarginPercent = p.TargetContributionMarginPercent,
                    ReportingCurrency = oldPlan.ReportingCurrency,
                }, cancellationToken);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, created.Summary.Id, Common.SILA_AUDIT_CULINARY_FIN_REVISED,
                $"Promoted scenario '{sc.Name}' from {oldPlan.PlanCode} to revision {created.Summary.PlanCode}.", userId);
            await _repository.SaveAsync();
            return created;
        }

        public async Task<SilaCulinaryFinancialExceptionDto> ResolveExceptionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, Guid exceptionId, string resolutionStatus, string? note, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryFinancialException ex = await _repository.SilaCulinaryFinancialException
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.EventFinancialPlanId == planId && x.Id == exceptionId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Exception not found.", "Financial exception was not found.");

            ex.ResolutionStatus = string.IsNullOrWhiteSpace(resolutionStatus) ? "RESOLVED" : resolutionStatus.Trim().ToUpperInvariant();
            ex.ResolutionNote = note;
            ex.ResolvedByUserId = userId;
            ex.ResolvedOnUtc = DateTime.UtcNow;
            ex.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, planId, Common.SILA_AUDIT_CULINARY_FIN_EX_RESOLVED,
                $"Exception {ex.ExceptionType} marked {ex.ResolutionStatus} by {actorName}.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryFinancialExceptionDto
            {
                Id = ex.Id,
                ExceptionType = ex.ExceptionType,
                Severity = ex.Severity,
                MaterialOrRecipeRef = ex.MaterialOrRecipeRef,
                FinancialImpact = ex.FinancialImpact,
                Message = ex.Message,
                RecommendedAction = ex.RecommendedAction,
                ResponsibleUser = ex.ResponsibleUser,
                ResolutionStatus = ex.ResolutionStatus,
                ResolutionNote = ex.ResolutionNote,
            };
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> SubmitForApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            if (plan.IsSimulation)
                throw new BadRequestCustomException("Simulation plan.", "Simulation plans cannot be submitted for financial approval.");

            bool hasCriticalOpen = await _repository.SilaCulinaryFinancialException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventFinancialPlanId == plan.Id && x.IsActive && x.ResolutionStatus == "OPEN" && x.Severity == "CRITICAL")
                .AnyAsync(cancellationToken);
            if (hasCriticalOpen)
                throw new BadRequestCustomException("Unresolved critical exception.", "Resolve critical financial exceptions before submitting for approval.");

            await SilaApprovals.StartAsync(
                _repository, _logger, buyer.Id,
                Common.SILA_APPROVAL_TYPE_FINANCIAL,
                Common.SILA_REF_CULINARY_FINANCIAL_PLAN,
                plan.Id, plan.VersionNumber,
                new ApprovalScope { PropertyId = plan.PropertyId, CompanyCode = plan.CompanyCode },
                cancellationToken);

            plan.Status = "PENDING_APPROVAL";
            plan.SubmittedByUserId = userId;
            plan.SubmittedOnUtc = DateTime.UtcNow;
            plan.DateUpdated = DateTime.UtcNow;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_SUBMITTED,
                $"Submitted financial plan {plan.PlanCode} v{plan.VersionNumber} for approval by {actorName}.", userId);
            await _repository.SaveAsync();
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> DecideApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, string decision, string? comment, CancellationToken cancellationToken)
        {
            if (!CanApproveFinancial(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Only authorized financial approvers may approve or reject financial baselines.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);
            string dec = (decision ?? string.Empty).Trim().ToUpperInvariant();
            if (dec is not ("APPROVE" or "REJECT" or "RETURN"))
                throw new BadRequestCustomException("Invalid decision.", "Decision must be APPROVE, REJECT, or RETURN.");

            bool hasSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ReferenceType == Common.SILA_REF_CULINARY_FINANCIAL_PLAN
                    && x.ReferenceId == plan.Id && x.Version == plan.VersionNumber && x.IsActive && x.Status == Common.SILA_APPROVAL_PENDING)
                .AnyAsync(cancellationToken);

            if (hasSteps)
            {
                ApprovalDecision res = await SilaApprovals.DecideAsync(
                    _repository, _logger, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, plan.VersionNumber,
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
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_RETURNED, $"Returned {plan.PlanCode}: {comment}", userId);
            }
            else if (dec == "APPROVE")
            {
                plan.Status = "APPROVED";
                plan.ApprovedByUserId = userId;
                plan.ApprovedOnUtc = DateTime.UtcNow;
                plan.ApprovalReference = $"FIN-APR-{plan.PlanCode}-V{plan.VersionNumber}";
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_APPROVED, $"Approved {plan.PlanCode} v{plan.VersionNumber} by {actorName}.", userId);
            }
            else
            {
                plan.Status = "REJECTED";
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_REJECTED, $"Rejected {plan.PlanCode}: {comment}", userId);
            }

            plan.DateUpdated = DateTime.UtcNow;
            await _repository.SaveAsync();
            return await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);
        }

        public async Task<SilaCulinaryEventFinancialPlanDetailDto> HandleEventChangeImpactAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid planId, int? updatedGuests, decimal? updatedPricePerGuest, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan oldPlan = await LoadPlanOrThrowAsync(buyer.Id, planId, true, cancellationToken);

            // Preserve approved snapshot immutably; create new proposed revision (§29, §49, §63)
            if (oldPlan.Status != "APPROVED")
            {
                oldPlan.Status = "SUPERSEDED";
                oldPlan.DateUpdated = DateTime.UtcNow;
            }
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, oldPlan.Id, Common.SILA_AUDIT_CULINARY_FIN_REVISED,
                $"Created proposed revision v{oldPlan.VersionNumber + 1} from {oldPlan.PlanCode} v{oldPlan.VersionNumber} due to event change; non-cancellable commitments preserved.", userId);
            await _repository.SaveAsync();

            int newGuests = updatedGuests is > 0 ? updatedGuests.Value : oldPlan.PlannedGuests;
            decimal price = updatedPricePerGuest is > 0 ? updatedPricePerGuest.Value : (oldPlan.BillableGuests > 0 ? oldPlan.NetRevenue / oldPlan.BillableGuests : 350m);
            return await CreateAndCalculatePlanAsync(
                organizationId, userId, roleId, actorName,
                new SilaCulinaryCreateEventFinancialPlanRequestDto
                {
                    EventId = oldPlan.EventId,
                    ProcurementPlanId = oldPlan.ProcurementPlanId,
                    Name = $"{oldPlan.Name} (Rev {oldPlan.VersionNumber + 1})",
                    VersionNumber = oldPlan.VersionNumber + 1,
                    IsDemo = oldPlan.IsDemo,
                    PlannedGuests = newGuests,
                    GuaranteedGuests = oldPlan.GuaranteedGuests,
                    SellingPricePerGuest = price,
                    FoodCostPerGuest = oldPlan.PlannedGuests > 0 ? oldPlan.EstimatedFoodCost / oldPlan.PlannedGuests : 90m,
                    BeverageCostPerGuest = oldPlan.PlannedGuests > 0 ? oldPlan.EstimatedBeverageCost / oldPlan.PlannedGuests : 25m,
                    OriginalBudget = oldPlan.OriginalBudget,
                    ReportingCurrency = oldPlan.ReportingCurrency,
                }, cancellationToken);
        }

        public async Task<SilaCulinaryEventCancellationExposureDto> GetCancellationExposureAsync(
            Guid organizationId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Cancellation exposure requires financial visibility.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            List<SilaCulinaryProcurementFinancialLineDto> proc = await LoadProcurementCommitmentsAsync(buyer.Id, plan, cancellationToken);

            decimal releasedPo = proc.Where(p => p.PoStatus is "APPROVED" or "RELEASED").Sum(p => p.PurchaseValue);
            decimal recInv = proc.Sum(p => p.ReceivedValue);
            decimal recoverable = proc.Sum(p => p.RemainingInventoryValue);
            decimal netExp = Math.Max(0m, (releasedPo + plan.ActualVerifiedCost) - recoverable);

            return new SilaCulinaryEventCancellationExposureDto
            {
                FinancialPlanId = plan.Id,
                PlanCode = plan.PlanCode,
                ReportingCurrency = plan.ReportingCurrency,
                CustomerDepositsOrInvoiced = 0m,
                NonRefundableSupplierCommitments = releasedPo,
                ReleasedPoExposure = releasedPo,
                ReceivedInventoryValue = recInv,
                RecoverableInventoryValue = recoverable,
                ActualIncurredExpenses = plan.ActualVerifiedCost,
                NetCancellationExposure = decimal.Round(netExp, 2),
                PolicyNotes = new List<string>
                {
                    "Deposits are NOT automatically recognized as revenue.",
                    "Customer refunds and supplier PO cancellations are NOT automatically executed.",
                    "Recoverable inventory reflects unallocated pack/MOQ stock subject to inspection.",
                },
            };
        }

        public async Task<SilaCulinaryFinancialAiResponseDto> AnalyzeWithAiAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, SilaCulinaryFinancialAiRequestDto request, CancellationToken cancellationToken)
        {
            if (PromptInjectionPattern.IsMatch(request.Question ?? string.Empty))
            {
                return new SilaCulinaryFinancialAiResponseDto
                {
                    Answer = "Blocked potential prompt injection or unauthorized SAP/budget posting instruction. Financial postings require authorized workflows.",
                    ProviderUsed = "DETERMINISTIC_GUARD",
                    PromptInjectionBlocked = true,
                    Warnings = new List<string> { "Prompt injection guard triggered." },
                };
            }

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            SilaCulinaryAiConfiguration? aiCfg = await _repository.SilaCulinaryAiConfiguration
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            string mode = aiCfg?.OperatingMode ?? "LOCAL";
            bool allowExternal = mode is "GEMINI" or "CLAUDE" or "HYBRID";

            string? engineUrl = _configuration[Common.CULINARY_ENGINE_URL];
            if (!string.IsNullOrWhiteSpace(engineUrl))
            {
                try
                {
                    using HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                    var payload = new
                    {
                        question = request.Question,
                        mode,
                        task_name = request.TaskName,
                        allow_external_ai = allowExternal,
                        event_context = new
                        {
                            currency = plan.ReportingCurrency,
                            net_revenue = plan.NetRevenue,
                            estimated_total_cost = plan.EstimatedTotalCost,
                            event_contribution = plan.ContributionAfterOverhead,
                            contribution_margin_percent = plan.ContributionMarginPercent,
                            target_margin_percent = plan.TargetContributionMarginPercent,
                            break_even_guests = plan.BreakEvenGuests,
                            remaining_cost_headroom = plan.RemainingCostHeadroom,
                            approved_procurement_commitment = plan.ApprovedProcurementCommitment,
                            forecast_at_completion_cost = plan.ForecastAtCompletionCost,
                            excess_purchase_value = plan.RemainingUnallocatedInventoryValue > 0 ? plan.RemainingUnallocatedInventoryValue : (decimal?)null,
                        },
                    };
                    using HttpResponseMessage resp = await client.PostAsJsonAsync($"{engineUrl.TrimEnd('/')}/v1/financial/analyze", payload, cancellationToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        var parsed = await resp.Content.ReadFromJsonAsync<SilaCulinaryFinancialAiResponseDto>(
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, PropertyNameCaseInsensitive = true }, cancellationToken);
                        if (parsed != null)
                        {
                            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_AI,
                                $"Financial AI query answered via {parsed.ProviderUsed}.", userId);
                            await _repository.SaveAsync();
                            return parsed;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogInfo($"Python financial AI fallback engaged: {ex.Message}");
                }
            }

            List<string> facts = new()
            {
                $"Event Plan: {plan.PlanCode} ({plan.PlannedGuests} guests, {plan.ReportingCurrency})",
                $"Net Revenue: {plan.ReportingCurrency} {plan.NetRevenue:0.00}",
                $"Estimated Total Cost: {plan.ReportingCurrency} {plan.EstimatedTotalCost:0.00}",
                $"Event Contribution: {plan.ReportingCurrency} {plan.ContributionAfterOverhead:0.00} ({plan.ContributionMarginPercent ?? 0:0.00}% margin)",
                $"Break-Even Guests: {(plan.BreakEvenGuests?.ToString() ?? "N/A")}",
                $"Remaining Cost Headroom (vs {plan.TargetContributionMarginPercent:0.##}% target): {plan.ReportingCurrency} {plan.RemainingCostHeadroom:0.00}",
                $"Procurement Commitment: {plan.ReportingCurrency} {plan.ApprovedProcurementCommitment:0.00}; Remaining Unallocated Stock: {plan.ReportingCurrency} {plan.RemainingUnallocatedInventoryValue:0.00}",
            };
            string answer = $"Deterministic Financial Intelligence ({plan.PlanCode}): " + string.Join(" | ", facts);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_AI,
                $"Financial AI query answered via {mode} deterministic grounding.", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryFinancialAiResponseDto
            {
                Answer = answer,
                ProviderUsed = $"{mode}_DETERMINISTIC",
                GroundedFacts = facts,
                PromptInjectionBlocked = false,
            };
        }

        public List<SilaCulinaryFinancialReportCatalogItemDto> GetReportCatalog() => ReportCatalog;

        public async Task<SilaCulinaryFinancialReportResultDto> ExportReportAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, string reportCode, CancellationToken cancellationToken)
        {
            if (!CanViewCosts(roleId))
                throw new ForBiddenCustomException("Forbidden.", "Financial report export requires authorized financial permissions.");

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            SilaCulinaryEventFinancialPlanDetailDto detail = await BuildDetailDtoAsync(buyer.Id, userId, roleId, plan, cancellationToken);

            SilaCulinaryFinancialReportCatalogItemDto item = ReportCatalog.FirstOrDefault(r => r.ReportCode.Equals(reportCode, StringComparison.OrdinalIgnoreCase))
                ?? throw new NotFoundCustomException("Report not found.", $"Report code '{reportCode}' is not in the Phase 3A catalog.");

            List<Dictionary<string, string>> rows = BuildReportRows(item.ReportCode, detail);
            string csv = BuildSafeCsv(rows);

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_FINANCIAL_PLAN, plan.Id, Common.SILA_AUDIT_CULINARY_FIN_REPORT,
                $"Generated report {item.ReportCode} ({item.Title}).", userId);
            await _repository.SaveAsync();

            return new SilaCulinaryFinancialReportResultDto
            {
                ReportCode = item.ReportCode,
                Title = item.Title,
                FileName = $"{item.ReportCode}_{plan.PlanCode}_v{plan.VersionNumber}.csv",
                ContentType = "text/csv",
                CsvContent = csv,
                PreviewRows = rows.Take(50).ToList(),
                GeneratedAtUtc = DateTime.UtcNow,
            };
        }

        public async Task<SilaCulinaryMobileFinancialSummaryDto> GetMobileSummaryAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan? plan = planId.HasValue
                ? await _repository.SilaCulinaryEventFinancialPlan.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == planId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken)
                : await _repository.SilaCulinaryEventFinancialPlan.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && !x.IsSimulation).OrderByDescending(x => x.CalculatedAtUtc).FirstOrDefaultAsync(cancellationToken);

            if (plan == null)
                return new SilaCulinaryMobileFinancialSummaryDto { EventName = "No active financial plan", FinancialReadiness = "PENDING" };

            int openEx = await _repository.SilaCulinaryFinancialException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventFinancialPlanId == plan.Id && x.IsActive && x.ResolutionStatus == "OPEN")
                .CountAsync(cancellationToken);

            bool fullVis = CanViewCosts(roleId);
            decimal util = plan.RevisedBudget > 0 ? decimal.Round((plan.EstimatedTotalCost / plan.RevisedBudget) * 100m, 2) : 0m;

            // Chef-friendly mobile payload: NEVER exposes salaries, supplier commercial terms, GL balances, or AI secrets (§69)
            return new SilaCulinaryMobileFinancialSummaryDto
            {
                FinancialPlanId = plan.Id,
                PlanCode = plan.PlanCode,
                EventName = plan.EventName ?? plan.Name,
                ReportingCurrency = plan.ReportingCurrency,
                PlannedGuests = plan.PlannedGuests,
                FoodCost = plan.EstimatedFoodCost,
                BeverageCost = plan.EstimatedBeverageCost,
                ApprovedBudget = plan.RevisedBudget,
                BudgetUtilizationPercent = util,
                EventContribution = fullVis ? plan.ContributionAfterOverhead : null,
                ContributionMarginPercent = fullVis ? plan.ContributionMarginPercent : null,
                OpenExceptionsCount = openEx,
                FinancialReadiness = openEx == 0 ? "READY" : "REVIEW_REQUIRED",
                Status = plan.Status,
            };
        }

        public async Task<SilaCulinaryPhase3BHandoverDto> GetPhase3BHandoverAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            return await BuildPhase3BHandoverAsync(buyer.Id, plan, cancellationToken);
        }

        public async Task<SilaCulinaryPhase4BHandoverDto> GetPhase4BHandoverAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid planId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventFinancialPlan plan = await LoadPlanOrThrowAsync(buyer.Id, planId, false, cancellationToken);
            return await BuildPhase4BHandoverAsync(buyer.Id, plan, cancellationToken);
        }

        private async Task<SilaCulinaryPhase3BHandoverDto> BuildPhase3BHandoverAsync(
            Guid buyerId, SilaCulinaryEventFinancialPlan plan, CancellationToken cancellationToken)
        {
            List<SilaCulinaryPhase3BRecipeSummaryDto> recipes = new();
            if (plan.MenuId.HasValue)
            {
                List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyerId && x.MenuId == plan.MenuId.Value && x.IsActive && x.RecipeId != null)
                    .ToListAsync(cancellationToken);
                foreach (SilaCulinaryMenuLine ml in lines.Take(50))
                {
                    recipes.Add(new SilaCulinaryPhase3BRecipeSummaryDto
                    {
                        RecipeId = ml.RecipeId!.Value,
                        RecipeName = ml.DishName,
                        RecipeVersion = ml.RecipeVersion ?? 1,
                        IngredientCost = ml.EstimatedLineCost ?? 0m,
                        CostPerServing = ml.UnitCostPerPortion ?? 0m,
                        PosSalesQuantity = ml.PlannedPortions,
                    });
                }
            }

            return new SilaCulinaryPhase3BHandoverDto
            {
                FinancialPlanId = plan.Id,
                PlanCode = plan.PlanCode,
                EventId = plan.EventId,
                EventCode = plan.EventCode,
                MenuId = plan.MenuId,
                ReportingCurrency = plan.ReportingCurrency,
                EventRevenue = plan.NetRevenue,
                EventCost = plan.EstimatedTotalCost,
                EventContribution = plan.ContributionAfterOverhead,
                EventMarginPercent = plan.ContributionMarginPercent,
                BudgetVariance = plan.EstimatedTotalCost - plan.RevisedBudget,
                VerificationStatus = plan.ActualIsProvisional ? "ESTIMATED" : "VERIFIED",
                Recipes = recipes,
            };
        }

        private async Task<SilaCulinaryPhase4BHandoverDto> BuildPhase4BHandoverAsync(
            Guid buyerId, SilaCulinaryEventFinancialPlan plan, CancellationToken cancellationToken)
        {
            List<string> poRefs = new();
            List<string> grnRefs = new();
            if (plan.ProcurementPlanId.HasValue)
            {
                List<SilaCulinaryProcurementAllocation> allocs = await _repository.SilaCulinaryProcurementAllocation
                    .FindByCondition(a => a.BuyerId == buyerId && a.ProcurementPlanId == plan.ProcurementPlanId.Value && a.IsActive)
                    .ToListAsync(cancellationToken);
                poRefs = allocs.Where(a => !string.IsNullOrWhiteSpace(a.PoNumber)).Select(a => a.PoNumber!).Distinct().ToList();
                grnRefs = allocs.Where(a => !string.IsNullOrWhiteSpace(a.GrnNumber)).Select(a => a.GrnNumber!).Distinct().ToList();
            }

            return new SilaCulinaryPhase4BHandoverDto
            {
                FinancialPlanId = plan.Id,
                PlanCode = plan.PlanCode,
                FinancialPlanVersion = plan.VersionNumber,
                EventId = plan.EventId,
                EventRevision = plan.EventRevision,
                MrpRunId = plan.MrpRunId,
                ProcurementPlanId = plan.ProcurementPlanId,
                ReportingCurrency = plan.ReportingCurrency,
                ApprovedFinancialBaselineCost = plan.EstimatedTotalCost,
                ExpectedConsumptionCost = plan.ExpectedConsumptionValueFromPo > 0 ? plan.ExpectedConsumptionValueFromPo : (plan.EstimatedFoodCost + plan.EstimatedBeverageCost),
                ActualConsumptionCost = plan.ActualVerifiedCost,
                ActualIsProvisional = true,
                EventFinanciallyClosed = false, // Mandatory Phase 3A rule: never close event in Phase 3A
                PlannedGuests = plan.PlannedGuests,
                ActualGuests = plan.ActualGuests,
                ActualRecognizedRevenue = 0m,
                OutstandingCommitments = plan.AttributableProcurementCommitment,
                PoReferences = poRefs,
                GrnReferences = grnRefs,
                ApprovalReference = plan.ApprovalReference,
            };
        }

        private async Task<SilaCulinaryEventFinancialPlan> LoadPlanOrThrowAsync(Guid buyerId, Guid planId, bool tracked, CancellationToken cancellationToken)
        {
            IQueryable<SilaCulinaryEventFinancialPlan> q = tracked
                ? _repository.SilaCulinaryEventFinancialPlan.FindByConditionTracked(x => x.BuyerId == buyerId && x.Id == planId && x.IsActive)
                : _repository.SilaCulinaryEventFinancialPlan.FindByCondition(x => x.BuyerId == buyerId && x.Id == planId && x.IsActive);
            return await q.FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Financial plan not found.", "Event financial plan was not found.");
        }

        private async Task<SilaCulinaryEventFinancialPlanDetailDto> BuildDetailDtoAsync(
            Guid buyerId, Guid userId, Guid roleId, SilaCulinaryEventFinancialPlan plan, CancellationToken cancellationToken)
        {
            bool costsVisible = CanViewCosts(roleId);
            bool salariesVisible = CanViewEmployeeRates(roleId);

            List<SilaCulinaryEventRevenueLine> rev = await _repository.SilaCulinaryEventRevenueLine
                .FindByCondition(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryEventExpenseLine> exp = await _repository.SilaCulinaryEventExpenseLine
                .FindByCondition(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryFinancialException> exs = await _repository.SilaCulinaryFinancialException
                .FindByCondition(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.Severity)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryFinancialScenario> scenarios = await _repository.SilaCulinaryFinancialScenario
                .FindByCondition(x => x.BuyerId == buyerId && x.EventFinancialPlanId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .ToListAsync(cancellationToken);
            List<InventoryWorkflowEvent> history = await _repository.InventoryWorkflowEvent
                .FindByCondition(x => x.BuyerId == buyerId && x.ReferenceType == Common.SILA_REF_CULINARY_FINANCIAL_PLAN && x.ReferenceId == plan.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Take(100)
                .ToListAsync(cancellationToken);
            List<SilaCulinaryProcurementFinancialLineDto> proc = await LoadProcurementCommitmentsAsync(buyerId, plan, cancellationToken);

            List<SilaCulinaryPnlStatementRowDto> rows = BuildInteractivePnlRows(plan);
            List<SilaCulinaryWaterfallStepDto> waterfall = new()
            {
                new() { Label = "Net Event Revenue", Amount = plan.NetRevenue, Kind = "REVENUE" },
                new() { Label = "Food Cost", Amount = -plan.EstimatedFoodCost, Kind = "COST" },
                new() { Label = "Beverage Cost", Amount = -plan.EstimatedBeverageCost, Kind = "COST" },
                new() { Label = "Manpower", Amount = -plan.EstimatedManpowerCost, Kind = "COST" },
                new() { Label = "Decoration", Amount = -plan.EstimatedDecorationCost, Kind = "COST" },
                new() { Label = "Equipment", Amount = -plan.EstimatedEquipmentCost, Kind = "COST" },
                new() { Label = "External Services", Amount = -plan.EstimatedExternalServiceCost, Kind = "COST" },
                new() { Label = "Other Direct", Amount = -plan.EstimatedOtherDirectCost, Kind = "COST" },
                new() { Label = "Direct Contribution", Amount = plan.DirectContribution, Kind = "SUBTOTAL" },
                new() { Label = "Allocated Overhead", Amount = -plan.EstimatedAllocatedOverhead, Kind = "OVERHEAD" },
                new() { Label = "Event Contribution", Amount = plan.ContributionAfterOverhead, Kind = "CONTRIBUTION" },
            };

            return new SilaCulinaryEventFinancialPlanDetailDto
            {
                Summary = ToSummaryDto(plan, costsVisible),
                RevenueLines = costsVisible ? rev.Select(r => new SilaCulinaryEventRevenueLineDto
                {
                    Id = r.Id, Category = r.Category, Description = r.Description, RevenueStatus = r.RevenueStatus,
                    BillingBasis = r.BillingBasis, Quantity = r.Quantity, UnitPrice = r.UnitPrice, Currency = r.Currency,
                    ExchangeRate = r.ExchangeRate, GrossAmount = r.GrossAmount, DiscountAmount = r.DiscountAmount,
                    TaxAmount = r.TaxAmount, NetRevenue = r.NetRevenue, SourceReference = r.SourceReference,
                }).ToList() : new(),
                ExpenseLines = exp.Select(e => new SilaCulinaryEventExpenseLineDto
                {
                    Id = e.Id, ExpenseCode = e.ExpenseCode, Category = e.Category, Description = e.Description,
                    CostBehavior = e.CostBehavior, StepThresholdGuests = e.StepThresholdGuests, StepIncrementCost = e.StepIncrementCost,
                    SupplierOrEmployeeRef = salariesVisible ? e.SupplierOrEmployeeRef : null,
                    EmployeeRole = e.EmployeeRole, StaffCount = e.StaffCount, PlannedHours = e.PlannedHours, ActualHours = e.ActualHours,
                    HourlyRate = salariesVisible ? e.HourlyRate : null, OvertimeHours = e.OvertimeHours,
                    OvertimeRate = salariesVisible ? e.OvertimeRate : null,
                    Quantity = e.Quantity, Uom = e.Uom, UnitRate = salariesVisible || e.Category != "MANPOWER" ? e.UnitRate : 0m,
                    Currency = e.Currency, ExchangeRate = e.ExchangeRate, BudgetAmount = e.BudgetAmount,
                    EstimatedAmount = e.EstimatedAmount, CommittedAmount = e.CommittedAmount, ActualVerifiedAmount = e.ActualVerifiedAmount,
                    TaxTreatment = e.TaxTreatment, CostCenter = e.CostCenter, GlAccount = costsVisible ? e.GlAccount : null,
                    InternalOrder = e.InternalOrder, WbsElement = e.WbsElement, SupportingDocumentRef = e.SupportingDocumentRef,
                    ExpenseDateUtc = e.ExpenseDateUtc, ApprovalReference = e.ApprovalReference, SourceTransaction = e.SourceTransaction,
                    SourceCostPoolId = e.SourceCostPoolId, Status = e.Status, SalaryMasked = !salariesVisible && e.Category == "MANPOWER",
                }).ToList(),
                ProcurementLines = costsVisible ? proc : new(),
                StatementRows = costsVisible ? rows : new(),
                Waterfall = costsVisible ? waterfall : new(),
                Exceptions = exs.Select(x => new SilaCulinaryFinancialExceptionDto
                {
                    Id = x.Id, ExceptionType = x.ExceptionType, Severity = x.Severity, MaterialOrRecipeRef = x.MaterialOrRecipeRef,
                    FinancialImpact = x.FinancialImpact, Message = x.Message, RecommendedAction = x.RecommendedAction,
                    ResponsibleUser = x.ResponsibleUser, ResolutionStatus = x.ResolutionStatus, ResolutionNote = x.ResolutionNote,
                }).ToList(),
                Scenarios = costsVisible ? scenarios.Select(s => new SilaCulinaryFinancialScenarioDto
                {
                    Id = s.Id, Name = s.Name, ScenarioType = s.ScenarioType, SimulatedGuests = s.SimulatedGuests,
                    SimulatedRevenue = s.SimulatedRevenue, SimulatedTotalCost = s.SimulatedTotalCost,
                    SimulatedContribution = s.SimulatedContribution, SimulatedMarginPercent = s.SimulatedMarginPercent,
                    SimulatedBreakEvenGuests = s.SimulatedBreakEvenGuests, PromotedToRevision = s.PromotedToRevision, DateCreated = s.DateCreated,
                }).ToList() : new(),
                History = history.Select(h => new SilaCulinaryAuditEntryDto
                {
                    Id = h.Id, Action = h.Action, Comment = h.Comment, ActorUserId = h.ActorUserId, TimestampUtc = h.DateCreated,
                }).ToList(),
                CostsVisible = costsVisible,
                SalariesVisible = salariesVisible,
                FinancialStatementsVisible = costsVisible,
                Phase3BHandover = costsVisible ? await BuildPhase3BHandoverAsync(buyerId, plan, cancellationToken) : null,
                Phase4BHandover = costsVisible ? await BuildPhase4BHandoverAsync(buyerId, plan, cancellationToken) : null,
            };
        }

        private static List<SilaCulinaryPnlStatementRowDto> BuildInteractivePnlRows(SilaCulinaryEventFinancialPlan p)
        {
            string ver = p.ActualIsProvisional ? "ESTIMATED" : "VERIFIED";
            decimal direct = p.EstimatedTotalCost - p.EstimatedAllocatedOverhead;
            return new List<SilaCulinaryPnlStatementRowDto>
            {
                new() { Section = "REVENUE", RowCode = "REV_NET", Label = "Net Event Revenue", OriginalBudget = p.NetRevenue, RevisedBudget = p.NetRevenue, Estimated = p.NetRevenue, AttributableCommitments = p.NetRevenue, ForecastAtCompletion = p.NetRevenue, ActualVerified = p.ActualIsProvisional ? null : p.NetRevenue, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_FOOD", Label = "Food Ingredients", OriginalBudget = p.EstimatedFoodCost, RevisedBudget = p.EstimatedFoodCost, Estimated = p.EstimatedFoodCost, AttributableCommitments = p.AttributableProcurementCommitment, ForecastAtCompletion = p.EstimatedFoodCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_BEV", Label = "Beverage Ingredients", OriginalBudget = p.EstimatedBeverageCost, RevisedBudget = p.EstimatedBeverageCost, Estimated = p.EstimatedBeverageCost, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedBeverageCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_MP", Label = "Direct Manpower", OriginalBudget = p.EstimatedManpowerCost, RevisedBudget = p.EstimatedManpowerCost, Estimated = p.EstimatedManpowerCost, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedManpowerCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_DEC", Label = "Decoration", OriginalBudget = p.EstimatedDecorationCost, RevisedBudget = p.EstimatedDecorationCost, Estimated = p.EstimatedDecorationCost, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedDecorationCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_EQP", Label = "Equipment", OriginalBudget = p.EstimatedEquipmentCost, RevisedBudget = p.EstimatedEquipmentCost, Estimated = p.EstimatedEquipmentCost, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedEquipmentCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_EXT", Label = "External Services", OriginalBudget = p.EstimatedExternalServiceCost, RevisedBudget = p.EstimatedExternalServiceCost, Estimated = p.EstimatedExternalServiceCost, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedExternalServiceCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "DIRECT_COST", RowCode = "COST_OTH", Label = "Other Direct Expenses", OriginalBudget = p.EstimatedOtherDirectCost, RevisedBudget = p.EstimatedOtherDirectCost, Estimated = p.EstimatedOtherDirectCost, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedOtherDirectCost, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "RESULT", RowCode = "TOT_DIRECT", Label = "Total Direct Cost", OriginalBudget = direct, RevisedBudget = direct, Estimated = direct, AttributableCommitments = p.AttributableProcurementCommitment, ForecastAtCompletion = direct, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "RESULT", RowCode = "CONTRIB_DIRECT", Label = "Direct Event Contribution", OriginalBudget = p.DirectContribution, RevisedBudget = p.DirectContribution, Estimated = p.DirectContribution, AttributableCommitments = p.NetRevenue - p.AttributableProcurementCommitment, ForecastAtCompletion = p.DirectContribution, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "RESULT", RowCode = "ALLOC_OH", Label = "Allocated Overhead", OriginalBudget = p.EstimatedAllocatedOverhead, RevisedBudget = p.EstimatedAllocatedOverhead, Estimated = p.EstimatedAllocatedOverhead, AttributableCommitments = 0m, ForecastAtCompletion = p.EstimatedAllocatedOverhead, VarianceAmount = 0m, VerificationStatus = ver },
                new() { Section = "RESULT", RowCode = "CONTRIB_NET", Label = "Event Contribution After Allocation", OriginalBudget = p.ContributionAfterOverhead, RevisedBudget = p.ContributionAfterOverhead, Estimated = p.ContributionAfterOverhead, AttributableCommitments = p.NetRevenue - p.AttributableProcurementCommitment, ForecastAtCompletion = p.NetRevenue - p.ForecastAtCompletionCost, ActualVerified = p.ActualIsProvisional ? null : p.NetRevenue - p.ActualVerifiedCost, VarianceAmount = p.ForecastAtCompletionCost - p.RevisedBudget, VerificationStatus = ver },
            };
        }

        private static SilaCulinaryEventFinancialPlanSummaryDto ToSummaryDto(SilaCulinaryEventFinancialPlan p, bool costsVisible) => new()
        {
            Id = p.Id,
            PlanCode = p.PlanCode,
            Name = p.Name,
            VersionNumber = p.VersionNumber,
            EventId = p.EventId,
            EventCode = p.EventCode,
            EventName = p.EventName,
            EventRevision = p.EventRevision,
            PropertyId = p.PropertyId,
            PropertyName = p.PropertyName,
            CompanyCode = p.CompanyCode,
            VenueName = p.VenueName,
            CustomerReference = p.CustomerReference,
            EventDateUtc = p.EventDateUtc,
            PlannedGuests = p.PlannedGuests,
            GuaranteedGuests = p.GuaranteedGuests,
            ActualGuests = p.ActualGuests,
            BillableGuests = p.BillableGuests,
            ReportingCurrency = p.ReportingCurrency,
            Status = p.Status,
            IsSimulation = p.IsSimulation,
            IsDemo = p.IsDemo,
            OriginalBudget = p.OriginalBudget,
            RevisedBudget = p.RevisedBudget,
            NetRevenue = costsVisible ? p.NetRevenue : null,
            EstimatedFoodCost = p.EstimatedFoodCost,
            EstimatedBeverageCost = p.EstimatedBeverageCost,
            EstimatedManpowerCost = costsVisible ? p.EstimatedManpowerCost : null,
            EstimatedTotalCost = costsVisible ? p.EstimatedTotalCost : null,
            ApprovedProcurementCommitment = costsVisible ? p.ApprovedProcurementCommitment : null,
            AttributableProcurementCommitment = costsVisible ? p.AttributableProcurementCommitment : null,
            ActualVerifiedCost = costsVisible ? p.ActualVerifiedCost : null,
            ActualIsProvisional = p.ActualIsProvisional,
            ForecastAtCompletionCost = costsVisible ? p.ForecastAtCompletionCost : null,
            ContributionAfterOverhead = costsVisible ? p.ContributionAfterOverhead : null,
            ContributionMarginPercent = costsVisible ? p.ContributionMarginPercent : null,
            FoodCostPercent = p.FoodCostPercent,
            BeverageCostPercent = p.BeverageCostPercent,
            CostPerGuest = costsVisible && p.PlannedGuests > 0 ? decimal.Round(p.EstimatedTotalCost / p.PlannedGuests, 2) : null,
            RevenuePerGuest = costsVisible && p.PlannedGuests > 0 ? decimal.Round(p.NetRevenue / p.PlannedGuests, 2) : null,
            RemainingBudget = p.RevisedBudget - p.EstimatedTotalCost,
            TargetMarginVariancePercent = costsVisible && p.ContributionMarginPercent.HasValue ? p.ContributionMarginPercent.Value - p.TargetContributionMarginPercent : null,
            BreakEvenGuests = costsVisible ? p.BreakEvenGuests : null,
            MaxAllowableCost = costsVisible ? p.MaxAllowableCost : null,
            RemainingCostHeadroom = costsVisible ? p.RemainingCostHeadroom : null,
            ApprovalReference = p.ApprovalReference,
            CalculatedAtUtc = p.CalculatedAtUtc,
        };

        private static List<Dictionary<string, string>> BuildReportRows(string reportCode, SilaCulinaryEventFinancialPlanDetailDto d)
        {
            var s = d.Summary;
            string ccy = s.ReportingCurrency;
            if (reportCode == "EVENT_FINANCIAL_EXCEPTIONS")
            {
                return d.Exceptions.Select(e => new Dictionary<string, string>
                {
                    ["PlanCode"] = s.PlanCode, ["Severity"] = e.Severity, ["ExceptionType"] = e.ExceptionType,
                    ["Message"] = e.Message, ["Status"] = e.ResolutionStatus, ["Currency"] = ccy,
                }).ToList();
            }
            if (reportCode is "PROCUREMENT_COMMITMENT" or "ATTRIBUTABLE_COMMITMENT" or "MULTI_EVENT_ALLOCATION")
            {
                return d.ProcurementLines.Select(p => new Dictionary<string, string>
                {
                    ["PlanCode"] = s.PlanCode, ["MaterialCode"] = p.MaterialCode, ["Description"] = p.MaterialDescription,
                    ["PoNumber"] = p.PoNumber ?? "-", ["PoStatus"] = p.PoStatus,
                    ["PurchasedQty"] = p.PurchasedQty.ToString("0.####"), ["EventAllocatedQty"] = p.EventAllocatedQty.ToString("0.####"),
                    ["PurchaseValue"] = p.PurchaseValue.ToString("0.00"), ["AttributableValue"] = p.AttributableEventCommitmentValue.ToString("0.00"),
                    ["RemainingInventoryValue"] = p.RemainingInventoryValue.ToString("0.00"), ["Currency"] = ccy,
                }).ToList();
            }
            if (reportCode == "EVENT_SCENARIO_COMPARISON")
            {
                return d.Scenarios.Select(sc => new Dictionary<string, string>
                {
                    ["PlanCode"] = s.PlanCode, ["Scenario"] = sc.Name, ["Type"] = sc.ScenarioType,
                    ["Guests"] = sc.SimulatedGuests.ToString(), ["Revenue"] = sc.SimulatedRevenue.ToString("0.00"),
                    ["TotalCost"] = sc.SimulatedTotalCost.ToString("0.00"), ["Contribution"] = sc.SimulatedContribution.ToString("0.00"),
                    ["MarginPct"] = (sc.SimulatedMarginPercent ?? 0m).ToString("0.00"), ["Currency"] = ccy,
                }).ToList();
            }
            if (reportCode == "REVENUE_BREAKDOWN")
            {
                return d.RevenueLines.Select(r => new Dictionary<string, string>
                {
                    ["PlanCode"] = s.PlanCode, ["EventName"] = s.Name, ["Category"] = r.Category, ["Description"] = r.Description,
                    ["Status"] = r.RevenueStatus, ["BillingBasis"] = r.BillingBasis, ["Quantity"] = r.Quantity.ToString("0.####"),
                    ["UnitPrice"] = r.UnitPrice.ToString("0.00"), ["Discount"] = r.DiscountAmount.ToString("0.00"),
                    ["NetRevenue"] = r.NetRevenue.ToString("0.00"), ["Currency"] = ccy, ["SnapshotTimestampUtc"] = s.CalculatedAtUtc.ToString("O"),
                }).ToList();
            }
            if (reportCode == "FINANCIAL_SNAPSHOT_HISTORY")
            {
                return d.History.Select(h => new Dictionary<string, string>
                {
                    ["PlanCode"] = s.PlanCode, ["EventName"] = s.Name, ["Version"] = s.VersionNumber.ToString(),
                    ["Action"] = h.Action, ["Comment"] = h.Comment ?? "-", ["TimestampUtc"] = h.TimestampUtc.ToString("O"),
                }).ToList();
            }
            return d.StatementRows.Select(r => new Dictionary<string, string>
            {
                ["ReportCode"] = reportCode, ["PlanCode"] = s.PlanCode, ["EventName"] = s.Name, ["Version"] = s.VersionNumber.ToString(),
                ["Section"] = r.Section, ["Line"] = r.Label, ["Currency"] = ccy,
                ["OriginalBudget"] = r.OriginalBudget.ToString("0.00"), ["Estimated"] = r.Estimated.ToString("0.00"),
                ["Commitments"] = r.AttributableCommitments.ToString("0.00"), ["ForecastAtCompletion"] = r.ForecastAtCompletion.ToString("0.00"),
                ["ActualVerified"] = r.ActualVerified?.ToString("0.00") ?? "PROVISIONAL", ["VerificationStatus"] = r.VerificationStatus,
                ["SnapshotTimestampUtc"] = s.CalculatedAtUtc.ToString("O"),
            }).ToList();
        }

        private static string BuildSafeCsv(List<Dictionary<string, string>> rows)
        {
            if (rows.Count == 0) return "PlanCode,Status\n\"-\",\"NO_DATA\"\n";
            List<string> headers = rows[0].Keys.ToList();
            StringBuilder sb = new();
            sb.AppendLine(string.Join(",", headers.Select(SanitizeCsvCell)));
            foreach (Dictionary<string, string> row in rows)
                sb.AppendLine(string.Join(",", headers.Select(h => SanitizeCsvCell(row.TryGetValue(h, out string? v) ? v : string.Empty))));
            return sb.ToString();
        }

        private static string SanitizeCsvCell(string? value)
        {
            string s = value ?? string.Empty;
            if (s.Length > 0 && (s[0] == '=' || s[0] == '+' || s[0] == '-' || s[0] == '@' || s[0] == '\t' || s[0] == '\r'))
                s = "'" + s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static readonly List<SilaCulinaryFinancialReportCatalogItemDto> ReportCatalog = new()
        {
            // 14 Recipe & Menu Financial Reports (§65)
            new() { ReportCode = "RECIPE_INGREDIENT_COST", Title = "Recipe Ingredient Cost Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "RECIPE_BATCH_COST", Title = "Recipe Batch Cost Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "RECIPE_PORTION_COST", Title = "Recipe Portion Cost Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "SUB_RECIPE_COST", Title = "Sub-Recipe Cost Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "YIELD_ADJUSTED_COST", Title = "Yield-Adjusted Cost Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "RECIPE_PROFITABILITY", Title = "Recipe Profitability Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "RECIPE_COST_HISTORY", Title = "Recipe Cost History", Category = "RECIPE_MENU" },
            new() { ReportCode = "RECIPE_PRICE_SOURCE", Title = "Recipe Price Source Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "RECIPE_TARGET_SELLING_PRICE", Title = "Recipe Target Selling Price Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "MENU_COST", Title = "Menu Cost Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "MENU_PROFITABILITY", Title = "Menu Profitability Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "FB_COST_CLASSIFICATION", Title = "Food and Beverage Cost Classification", Category = "RECIPE_MENU" },
            new() { ReportCode = "INGREDIENT_PRICE_IMPACT", Title = "Ingredient Price Impact Report", Category = "RECIPE_MENU" },
            new() { ReportCode = "HIGH_COST_INGREDIENT_PARETO", Title = "High-Cost Ingredient Pareto Report", Category = "RECIPE_MENU" },
            // 34 Event Financial Reports (§65) -> 48 total
            new() { ReportCode = "ESTIMATED_EVENT_PNL", Title = "Estimated Event P&L", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "PROCUREMENT_COMMITMENT", Title = "Procurement Commitment Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "ATTRIBUTABLE_COMMITMENT", Title = "Attributable Event Commitment Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "FORECAST_AT_COMPLETION_PNL", Title = "Forecast-at-Completion P&L", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "PROVISIONAL_ACTUAL_PNL", Title = "Provisional Actual Event P&L", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "REVENUE_BREAKDOWN", Title = "Revenue Breakdown Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_FOOD_COST", Title = "Food Cost Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_BEVERAGE_COST", Title = "Beverage Cost Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "MANPOWER_COST", Title = "Manpower Cost Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EQUIPMENT_EXPENSE", Title = "Equipment Expense Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EXTERNAL_SERVICE_EXPENSE", Title = "External Service Expense Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "OTHER_EXPENSE", Title = "Other Expense Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "OVERHEAD_ALLOCATION", Title = "Overhead Allocation Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_COST_PER_GUEST", Title = "Event Cost Per Guest", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_CONTRIBUTION_MARGIN", Title = "Event Contribution Margin Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "BREAK_EVEN_ANALYSIS", Title = "Break-Even Analysis", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_TARGET_SELLING_PRICE", Title = "Target Selling Price Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "MAX_ALLOWABLE_COST", Title = "Maximum Allowable Cost Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_BUDGET_UTILIZATION", Title = "Event Budget Utilization", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_FINANCIAL_VARIANCE", Title = "Event Financial Variance", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "INGREDIENT_PRICE_VARIANCE", Title = "Ingredient Price Variance", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "INGREDIENT_QUANTITY_VARIANCE", Title = "Ingredient Quantity Variance", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "GUEST_VOLUME_VARIANCE", Title = "Guest Volume Variance", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "MULTI_EVENT_ALLOCATION", Title = "Multi-Event Cost Allocation", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "PROPERTY_PROFITABILITY", Title = "Property Profitability Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_SCENARIO_COMPARISON", Title = "Event Financial Scenario Comparison", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_CHANGE_IMPACT", Title = "Event Change Financial Impact", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_CANCELLATION_EXPOSURE", Title = "Event Cancellation Financial Exposure", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "EVENT_FINANCIAL_EXCEPTIONS", Title = "Financial Exception Report", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "FINANCIAL_APPROVAL_PACKAGE", Title = "Financial Approval Package", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "FINANCIAL_SNAPSHOT_HISTORY", Title = "Financial Snapshot History", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "AI_RECOMMENDATION_HISTORY", Title = "AI Financial Recommendation History", Category = "EVENT_FINANCIAL" },
            new() { ReportCode = "PHASE_3B_ANALYTICS_HANDOVER", Title = "Phase 3B Analytics Handover", Category = "HANDOVER" },
            new() { ReportCode = "PHASE_4B_RECONCILIATION_HANDOVER", Title = "Phase 4B Actual Reconciliation Handover", Category = "HANDOVER" },
        };
    }
}
