using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared.MenuEngineering.Twin
{
    public interface IMenuEngineeringDigitalTwinService
    {
        Task<TwinSnapshotDto> CreateSnapshotAsync(Guid organizationId, Guid userId, Guid roleId, CreateTwinSnapshotRequestDto request, CancellationToken ct);
        Task<List<TwinSnapshotDto>> GetSnapshotsAsync(Guid organizationId, Guid roleId, Guid? menuPlanId = null, CancellationToken ct = default);
        Task<TwinSnapshotDto> GetSnapshotAsync(Guid organizationId, Guid roleId, Guid snapshotId, CancellationToken ct);
        Task<TwinSnapshotDto> RefreshSnapshotAsync(Guid organizationId, Guid userId, Guid roleId, Guid snapshotId, CancellationToken ct);
        Task<TwinSnapshotComparisonDto> CompareSnapshotsAsync(Guid organizationId, Guid roleId, Guid leftSnapshotId, Guid rightSnapshotId, CancellationToken ct);
        Task<TwinDependencyAnalysisResultDto> AnalyzeDependencyGraphAsync(Guid organizationId, Guid userId, Guid roleId, TwinDependencyAnalysisRequestDto request, CancellationToken ct);

        Task<MePhase3OptimizationJobDto> RunMultiObjectiveOptimizationAsync(Guid organizationId, Guid userId, Guid roleId, MePhase3OptimizeRequestDto request, CancellationToken ct);
        Task<MePhase3OptimizationJobDto> GetOptimizationJobAsync(Guid organizationId, Guid roleId, Guid jobId, CancellationToken ct);
        Task<MePhase3OptimizationJobDto> CancelOptimizationJobAsync(Guid organizationId, Guid userId, Guid roleId, Guid jobId, CancellationToken ct);
        Task<MePhase3InventoryMenuResultDto> OptimizeInventoryDrivenMenuAsync(Guid organizationId, Guid userId, Guid roleId, MePhase3InventoryMenuRequestDto request, CancellationToken ct);
        Task<MePhase3ProductionFeasibilityDto> EvaluateProductionFeasibilityAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct);
        Task<MePhase3PricingOptimizeResultDto> OptimizePricingPhase3Async(Guid organizationId, Guid roleId, Guid? propertyId, Guid? menuPlanId, CancellationToken ct);

        Task<MeMonteCarloResultDto> RunMonteCarloAsync(Guid organizationId, Guid userId, Guid roleId, MeMonteCarloRequestDto request, CancellationToken ct);
        Task<MeSensitivityResultDto> RunSensitivityAnalysisAsync(Guid organizationId, Guid roleId, MeSensitivityRequestDto request, CancellationToken ct);
        Task<MeStressTestResultDto> RunStressTestAsync(Guid organizationId, Guid roleId, MeStressTestRequestDto request, CancellationToken ct);

        Task<MeHierarchicalForecastResultDto> ForecastHierarchicalDemandAsync(Guid organizationId, Guid roleId, Guid? propertyId, int horizonDays, CancellationToken ct);
        Task<MeColdStartPredictResultDto> PredictColdStartItemAsync(Guid organizationId, Guid roleId, MeColdStartPredictRequestDto request, CancellationToken ct);
        Task<MeWastePredictResultDto> PredictFoodWasteAsync(Guid organizationId, Guid roleId, Guid? propertyId, Guid? menuPlanId, CancellationToken ct);
        Task<MePricePredictResultDto> PredictIngredientPricesAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<MeSubstitutionCascadeResultDto> EvaluateSubstitutionCascadeAsync(Guid organizationId, Guid roleId, MeSubstitutionCascadeRequestDto request, CancellationToken ct);

        Task<MeScenarioLabItemDto> CreateScenarioAsync(Guid organizationId, Guid userId, Guid roleId, MeScenarioCreateRequestDto request, CancellationToken ct);
        Task<MeScenarioComparisonDto> GetAndCompareScenariosAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, CancellationToken ct);
        Task<MeScenarioLabItemDto> CopyScenarioAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, string? newName, CancellationToken ct);
        Task<MeScenarioLabItemDto> ArchiveScenarioAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, CancellationToken ct);
        Task<Guid> ConvertScenarioToDraftMenuAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, CancellationToken ct);

        Task<MeHistoricalSimilarityResultDto> GetHistoricalSimilarityAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, CancellationToken ct);
        Task<MeEventOutcomeComparisonDto> CaptureCompletedEventOutcomeAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuPlanId, CancellationToken ct);
        Task<MeModelGovernanceStatusDto> GetModelGovernanceStatusAsync(Guid organizationId, Guid roleId, CancellationToken ct);
        Task<MeDriftEventDto> TransitionDriftGovernanceAsync(Guid organizationId, Guid userId, Guid roleId, Guid driftEventId, MeDriftActionRequestDto request, CancellationToken ct);
    }

    public partial class MenuEngineeringDigitalTwinService : IMenuEngineeringDigitalTwinService
    {
        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
        private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> TenantJobQuotas = new();

        private readonly IRepositoryWrapper _repository;
        private readonly RepositoryContext _db;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor? _httpContextAccessor;
        private readonly IHttpClientFactory? _httpClientFactory;

        public MenuEngineeringDigitalTwinService(
            IRepositoryWrapper repository,
            RepositoryContext db,
            ILoggerManager logger,
            IHttpContextAccessor? httpContextAccessor = null,
            IHttpClientFactory? httpClientFactory = null)
        {
            _repository = repository;
            _db = db;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        private SemaphoreSlim GetTenantQuotaLock(Guid buyerId) =>
            TenantJobQuotas.GetOrAdd(buyerId, _ => new SemaphoreSlim(3, 3));

        private async Task<(BuyerBusinessProfile Buyer, MenuEngineeringPermissionSetDto Perms, List<InventoryLocation> Scope)> BuildScopeAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(_httpContextAccessor, roleId);
            List<InventoryLocation> scope = await _db.InventoryLocation
                .AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId))
                .ToListAsync(ct);
            return (buyer, perms, scope);
        }

        private async Task<TOut?> PostPythonTwinAsync<TIn, TOut>(string path, TIn payload, CancellationToken ct)
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

        private async Task<DateTime> ComputeSourceMaxUpdatedUtcAsync(Guid buyerId, Guid? menuPlanId, CancellationToken ct)
        {
            List<DateTime> dates = new();
            DateTime? maxItem = await _db.MenuItem.Where(x => x.BuyerId == buyerId && x.IsActive).MaxAsync(x => (DateTime?)x.DateUpdated, ct);
            DateTime? maxPlan = await _db.MenuPlan.Where(x => x.BuyerId == buyerId && x.IsActive && (menuPlanId == null || x.Id == menuPlanId)).MaxAsync(x => (DateTime?)x.DateUpdated, ct);
            DateTime? maxRec = await _db.Recipe.Where(x => x.BuyerId == buyerId && x.IsActive).MaxAsync(x => (DateTime?)x.DateUpdated, ct);
            DateTime? maxInv = await _db.InventoryBalance.Where(x => x.BuyerId == buyerId && x.IsActive).MaxAsync(x => (DateTime?)x.DateUpdated, ct);
            if (maxItem.HasValue) dates.Add(maxItem.Value);
            if (maxPlan.HasValue) dates.Add(maxPlan.Value);
            if (maxRec.HasValue) dates.Add(maxRec.Value);
            if (maxInv.HasValue) dates.Add(maxInv.Value);
            return dates.Count > 0 ? dates.Max() : DateTime.UtcNow;
        }

        public async Task<TwinSnapshotDto> CreateSnapshotAsync(Guid organizationId, Guid userId, Guid roleId, CreateTwinSnapshotRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, List<InventoryLocation> scope) =
                await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            List<Guid> scopeIds = scope.Select(s => s.Id).ToList();
            var items = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var plans = await _db.MenuPlan.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (request.MenuPlanId == null || x.Id == request.MenuPlanId)).ToListAsync(ct);
            var recipes = await _db.Recipe.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var invBalances = await _db.InventoryBalance.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (scopeIds.Count == 0 || scopeIds.Contains(x.LocationId))).ToListAsync(ct);
            var reservations = await _db.SilaCulinaryStockReservation.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && x.Status == "ACTIVE").ToListAsync(ct);
            var kitchens = await _db.SilaCulinaryKitchen.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var equipment = await _db.SilaCulinaryEquipment.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var suppliers = await _db.SilaSupplier.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var wasteEvents = await _db.SilaCulinaryWasteEvent.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var events = await _db.SilaCulinaryEvent.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var posSales = await _db.PosSalesTransaction.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);

            decimal totalOnHandVal = invBalances.Sum(b => Math.Max(0m, b.OnHandQty) * (b.UnitCost ?? 0m));
            decimal totalReservedQty = reservations.Sum(r => Math.Max(0m, r.ReservedQty - r.FulfilledQty));
            decimal totalKitchenCap = kitchens.Sum(k => k.DailyPortionCapacity);
            decimal totalWasteCost = wasteEvents.Sum(w => w.EstimatedWasteCost);
            decimal posRev = posSales.Sum(p => p.NetRevenue ?? p.Amount ?? 0m);
            decimal plannedCovers = plans.Sum(p => p.ExpectedCovers ?? p.ExpectedGuests ?? 0m);
            int approvedRecipes = recipes.Count(r => string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase));

            List<TwinComponentSummaryDto> components = new()
            {
                new() { ComponentKey = "MENU", DisplayName = "Menu Twin", RecordCount = plans.Count + items.Count, SummaryMetric = $"{plans.Count} menus, {items.Count} items", DataQuality = items.Count > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = plans.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "RECIPE", DisplayName = "Recipe Twin", RecordCount = recipes.Count, SummaryMetric = $"{approvedRecipes}/{recipes.Count} approved recipes", DataQuality = approvedRecipes > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = recipes.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "DEMAND", DisplayName = "Demand Twin", RecordCount = posSales.Count + plans.Count, SummaryMetric = $"{plannedCovers:F0} planned covers, {posSales.Count} POS rows", DataQuality = posSales.Count > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = posSales.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "INVENTORY", DisplayName = "Inventory Twin", RecordCount = invBalances.Count, SummaryMetric = $"On-hand AED {totalOnHandVal:F2}, reserved {totalReservedQty:F2}", DataQuality = invBalances.Count > 0 ? "HIGH" : "LOW", LastSourceUpdateUtc = invBalances.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "PRODUCTION", DisplayName = "Production Twin", RecordCount = kitchens.Count + equipment.Count, SummaryMetric = $"{kitchens.Count} kitchens, cap {totalKitchenCap:F0} portions/day", DataQuality = kitchens.Count > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = kitchens.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "FINANCIAL", DisplayName = "Financial Twin", RecordCount = items.Count, SummaryMetric = $"POS net sales AED {posRev:F2}", DataQuality = "HIGH", LastSourceUpdateUtc = DateTime.UtcNow },
                new() { ComponentKey = "SUPPLIER", DisplayName = "Supplier Twin", RecordCount = suppliers.Count, SummaryMetric = $"{suppliers.Count} active suppliers", DataQuality = suppliers.Count > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = suppliers.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "WASTE", DisplayName = "Waste Twin", RecordCount = wasteEvents.Count, SummaryMetric = $"{wasteEvents.Count} waste logs (AED {totalWasteCost:F2})", DataQuality = wasteEvents.Count > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = wasteEvents.Select(x => (DateTime?)x.DateUpdated).Max() },
                new() { ComponentKey = "EVENT", DisplayName = "Event Twin", RecordCount = events.Count, SummaryMetric = $"{events.Count} culinary events", DataQuality = events.Count > 0 ? "HIGH" : "MEDIUM", LastSourceUpdateUtc = events.Select(x => (DateTime?)x.DateUpdated).Max() }
            };

            decimal dqScore = Math.Round((decimal)components.Count(c => c.DataQuality == "HIGH") / components.Count * 100m, 2);
            string dqLabel = dqScore >= 75m ? "HIGH" : dqScore >= 45m ? "MEDIUM" : "LOW";
            DateTime sourceMax = await ComputeSourceMaxUpdatedUtcAsync(buyer.Id, request.MenuPlanId, ct);
            int nextVersion = (await _db.MenuEngineeringTwinSnapshot
                .Where(x => x.BuyerId == buyer.Id && x.MenuPlanId == request.MenuPlanId && x.IsActive)
                .MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;

            Dictionary<string, object> metrics = new()
            {
                ["menuPlans"] = plans.Count,
                ["menuItems"] = items.Count,
                ["approvedRecipes"] = approvedRecipes,
                ["plannedCovers"] = plannedCovers,
                ["inventoryOnHandValue"] = Math.Round(totalOnHandVal, 2),
                ["reservedStockQty"] = Math.Round(totalReservedQty, 2),
                ["dailyKitchenCapacity"] = Math.Round(totalKitchenCap, 2),
                ["recordedWasteCost"] = Math.Round(totalWasteCost, 2),
                ["posNetSales"] = Math.Round(posRev, 2),
                ["activeSuppliers"] = suppliers.Count,
                ["activeEvents"] = events.Count
            };

            List<string> lineage = new()
            {
                $"MenuPlan:{plans.Count}", $"MenuItem:{items.Count}", $"Recipe:{recipes.Count}",
                $"InventoryBalance:{invBalances.Count}", $"SilaCulinaryStockReservation:{reservations.Count}",
                $"SilaCulinaryKitchen:{kitchens.Count}", $"SilaSupplier:{suppliers.Count}",
                $"SilaCulinaryWasteEvent:{wasteEvents.Count}", $"SilaCulinaryEvent:{events.Count}"
            };

            var payloadObj = new { components, metrics };
            string payloadJson = JsonSerializer.Serialize(payloadObj, JsonOpts);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payloadJson))).ToLowerInvariant()[..24];
            DateTime now = DateTime.UtcNow;

            MenuEngineeringTwinSnapshot entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                OutletLocationId = request.OutletLocationId,
                MenuPlanId = request.MenuPlanId,
                EventId = request.EventId,
                SnapshotType = string.IsNullOrWhiteSpace(request.SnapshotType) ? "LIVE" : request.SnapshotType.Trim().ToUpperInvariant(),
                VersionNumber = nextVersion,
                SnapshotHash = hash,
                CalculationVersion = "me-calc-v3.0",
                ModelVersion = "me-twin-v3.0",
                DataQualityScore = dqScore,
                DataQualityLabel = dqLabel,
                BusinessEffectiveDateUtc = request.BusinessEffectiveDateUtc ?? now.Date,
                SourceMaxUpdatedAtUtc = sourceMax,
                DataFreshnessUtc = now,
                ExpiresAtUtc = request.ExpirationHours.HasValue ? now.AddHours(Math.Max(1, request.ExpirationHours.Value)) : null,
                IsStale = false,
                TwinPayloadJson = payloadJson,
                LineageJson = JsonSerializer.Serialize(lineage, JsonOpts),
                CapturedByUserId = userId,
                CapturedAtUtc = now,
                IsActive = true
            };

            _db.MenuEngineeringTwinSnapshot.Add(entity);
            await _db.SaveChangesAsync(ct);

            return MapSnapshotDto(entity, components, metrics, lineage, sourceMax);
        }

        public async Task<List<TwinSnapshotDto>> GetSnapshotsAsync(Guid organizationId, Guid roleId, Guid? menuPlanId = null, CancellationToken ct = default)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            List<MenuEngineeringTwinSnapshot> rows = await _db.MenuEngineeringTwinSnapshot
                .AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && (menuPlanId == null || x.MenuPlanId == menuPlanId))
                .OrderByDescending(x => x.CapturedAtUtc)
                .Take(30)
                .ToListAsync(ct);

            DateTime liveMax = await ComputeSourceMaxUpdatedUtcAsync(buyer.Id, menuPlanId, ct);
            return rows.Select(r => DeserializeSnapshot(r, liveMax)).ToList();
        }

        public async Task<TwinSnapshotDto> GetSnapshotAsync(Guid organizationId, Guid roleId, Guid snapshotId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            // Strictly enforce BuyerId tenant isolation
            MenuEngineeringTwinSnapshot? snap = await _db.MenuEngineeringTwinSnapshot
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == snapshotId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (snap is null)
                throw new NotFoundCustomException("Twin snapshot not found", $"Digital twin snapshot {snapshotId} was not found for this tenant.");

            DateTime liveMax = await ComputeSourceMaxUpdatedUtcAsync(buyer.Id, snap.MenuPlanId, ct);
            return DeserializeSnapshot(snap, liveMax);
        }

        public async Task<TwinSnapshotDto> RefreshSnapshotAsync(Guid organizationId, Guid userId, Guid roleId, Guid snapshotId, CancellationToken ct)
        {
            TwinSnapshotDto existing = await GetSnapshotAsync(organizationId, roleId, snapshotId, ct);
            return await CreateSnapshotAsync(organizationId, userId, roleId, new CreateTwinSnapshotRequestDto
            {
                PropertyId = existing.PropertyId,
                OutletLocationId = existing.OutletLocationId,
                MenuPlanId = existing.MenuPlanId,
                EventId = existing.EventId,
                SnapshotType = existing.SnapshotType,
                BusinessEffectiveDateUtc = DateTime.UtcNow.Date
            }, ct);
        }

        public async Task<TwinSnapshotComparisonDto> CompareSnapshotsAsync(Guid organizationId, Guid roleId, Guid leftSnapshotId, Guid rightSnapshotId, CancellationToken ct)
        {
            TwinSnapshotDto left = await GetSnapshotAsync(organizationId, roleId, leftSnapshotId, ct);
            TwinSnapshotDto right = await GetSnapshotAsync(organizationId, roleId, rightSnapshotId, ct);

            List<TwinDeltaMetricDto> deltas = new();
            foreach (string key in left.Metrics.Keys.Union(right.Metrics.Keys))
            {
                decimal lv = ConvertToDecimal(left.Metrics.GetValueOrDefault(key));
                decimal rv = ConvertToDecimal(right.Metrics.GetValueOrDefault(key));
                decimal diff = rv - lv;
                decimal pct = lv != 0m ? Math.Round(diff / lv * 100m, 2) : 0m;
                deltas.Add(new TwinDeltaMetricDto { MetricKey = key, LeftValue = lv, RightValue = rv, Delta = diff, DeltaPercent = pct });
            }

            return new TwinSnapshotComparisonDto
            {
                LeftSnapshotId = left.SnapshotId,
                RightSnapshotId = right.SnapshotId,
                LeftVersion = left.VersionNumber,
                RightVersion = right.VersionNumber,
                Deltas = deltas
            };
        }

        public async Task<TwinDependencyAnalysisResultDto> AnalyzeDependencyGraphAsync(Guid organizationId, Guid userId, Guid roleId, TwinDependencyAnalysisRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMenuCost || perms.ViewMeStudio, "Access denied", "Cost or Studio view permission required.");

            var recipes = await _db.Recipe.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).Include(r => r.Ingredients).ToListAsync(ct);
            var menuItems = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var menuPlans = await _db.MenuPlan.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (request.MenuPlanId == null || x.Id == request.MenuPlanId)).ToListAsync(ct);
            var planLines = await _db.MenuPlanLine.AsNoTracking().Where(x => menuPlans.Select(p => p.Id).Contains(x.MenuPlanId) && x.IsActive).ToListAsync(ct);

            var nodes = new List<object>();
            var edges = new List<object>();
            var addedNodeIds = new HashSet<string>();

            foreach (Recipe r in recipes)
            {
                string rId = $"rec_{r.Id}";
                if (addedNodeIds.Add(rId))
                {
                    nodes.Add(new
                    {
                        node_id = rId,
                        node_type = r.ItemMode == "SUB_RECIPE" ? "SUB_RECIPE" : "RECIPE",
                        label = r.Name,
                        unit_cost = (double)r.TotalCost,
                        is_approved = string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase),
                        planned_covers = 1.0
                    });
                }

                foreach (RecipeIngredient ing in r.Ingredients.Where(i => i.IsActive))
                {
                    string iId = ing.SubRecipeId.HasValue
                        ? $"rec_{ing.SubRecipeId.Value}"
                        : $"ing_{(ing.MaterialId?.ToString() ?? ing.ItemCode)}";
                    if (!ing.SubRecipeId.HasValue && addedNodeIds.Add(iId))
                    {
                        decimal uc = ing.UnitCost ?? (ing.Quantity > 0 ? ing.Cost / ing.Quantity : 0m);
                        nodes.Add(new
                        {
                            node_id = iId,
                            node_type = "INGREDIENT",
                            label = ing.ItemName,
                            unit_cost = (double)uc,
                            uom = ing.Uom,
                            is_approved = true,
                            planned_covers = 1.0
                        });
                    }
                    edges.Add(new
                    {
                        source_id = iId,
                        target_id = rId,
                        relation = "USED_IN",
                        quantity = (double)Math.Max(0.001m, ing.Quantity),
                        uom = ing.Uom,
                        yield_percent = 100.0,
                        uom_valid = !string.IsNullOrWhiteSpace(ing.Uom)
                    });
                }
            }

            foreach (MenuItem mi in menuItems)
            {
                string mId = $"item_{mi.Id}";
                decimal itemCovers = planLines.Where(l => l.MenuItemId == mi.Id).Sum(l => l.PlannedPortions ?? l.RecommendedPortions);
                if (itemCovers <= 0m) itemCovers = 1m;
                if (addedNodeIds.Add(mId))
                {
                    nodes.Add(new
                    {
                        node_id = mId,
                        node_type = "MENU_ITEM",
                        label = mi.Name,
                        selling_price = (double)(mi.SellingPrice ?? 0m),
                        is_approved = true,
                        planned_covers = (double)itemCovers
                    });
                }
                if (mi.LinkedRecipeId.HasValue && addedNodeIds.Contains($"rec_{mi.LinkedRecipeId.Value}"))
                {
                    edges.Add(new
                    {
                        source_id = $"rec_{mi.LinkedRecipeId.Value}",
                        target_id = mId,
                        relation = "PREPARES",
                        quantity = 1.0,
                        yield_percent = 100.0,
                        uom_valid = true
                    });
                }
            }

            foreach (MenuPlan mp in menuPlans)
            {
                string mpId = $"menu_{mp.Id}";
                if (addedNodeIds.Add(mpId))
                {
                    nodes.Add(new
                    {
                        node_id = mpId,
                        node_type = "MENU",
                        label = mp.Name,
                        is_approved = true,
                        planned_covers = (double)(mp.ExpectedCovers ?? mp.ExpectedGuests ?? 1m)
                    });
                }
                foreach (MenuPlanLine line in planLines.Where(l => l.MenuPlanId == mp.Id))
                {
                    if (addedNodeIds.Contains($"item_{line.MenuItemId}"))
                    {
                        edges.Add(new
                        {
                            source_id = $"item_{line.MenuItemId}",
                            target_id = mpId,
                            relation = "INCLUDED_IN",
                            quantity = 1.0,
                            yield_percent = 100.0,
                            uom_valid = true
                        });
                    }
                }
            }

            string? shockNodeId = null;
            if (request.ShockMaterialId.HasValue)
                shockNodeId = $"ing_{request.ShockMaterialId.Value}";
            else if (!string.IsNullOrWhiteSpace(request.ShockIngredientName))
            {
                var match = recipes.SelectMany(r => r.Ingredients)
                    .FirstOrDefault(i => i.ItemName.Contains(request.ShockIngredientName, StringComparison.OrdinalIgnoreCase));
                if (match is not null)
                    shockNodeId = match.SubRecipeId.HasValue ? $"rec_{match.SubRecipeId.Value}" : $"ing_{(match.MaterialId?.ToString() ?? match.ItemCode)}";
            }

            var pyPayload = new
            {
                tenant_id = buyer.Id.ToString(),
                property_id = (request.PropertyId ?? Guid.Empty).ToString(),
                nodes,
                edges,
                shock_node_id = shockNodeId,
                price_change_percent = (double)request.PriceChangePercent
            };

            PyTwinGraphResp? pyResp = await PostPythonTwinAsync<object, PyTwinGraphResp>("/v1/me-twin/dependency-graph", pyPayload, ct);
            if (pyResp is not null)
            {
                return new TwinDependencyAnalysisResultDto
                {
                    NodeCount = pyResp.Node_Count,
                    EdgeCount = pyResp.Edge_Count,
                    IsAcyclic = pyResp.Is_Acyclic,
                    Cycles = pyResp.Cycles,
                    IntegrityIssues = pyResp.Integrity_Issues.Select(i => new TwinIntegrityIssueDto
                    {
                        IssueType = i.Issue_Type,
                        Severity = i.Severity,
                        NodeIds = i.Node_Ids,
                        Detail = i.Detail
                    }).ToList(),
                    ShockNodeId = pyResp.Shock_Node_Id,
                    PriceChangePercent = (decimal)pyResp.Price_Change_Percent,
                    ImpactedObjects = pyResp.Impacted_Nodes.Select(n => new TwinImpactedObjectDto
                    {
                        NodeId = n.Node_Id,
                        NodeType = n.Node_Type,
                        Label = n.Label,
                        HopDistance = n.Hop_Distance,
                        BaselineCost = (decimal)n.Baseline_Cost,
                        ShockedCost = (decimal)n.Shocked_Cost,
                        CostDelta = (decimal)n.Cost_Delta,
                        CostDeltaPercent = (decimal)n.Cost_Delta_Percent,
                        MarginDelta = (decimal)n.Margin_Delta
                    }).ToList(),
                    AffectedRecipes = pyResp.Affected_Recipes,
                    AffectedMenuItems = pyResp.Affected_Menu_Items,
                    AffectedMenus = pyResp.Affected_Menus,
                    AffectedEvents = pyResp.Affected_Events,
                    TotalCostImpact = (decimal)pyResp.Total_Cost_Impact,
                    TotalMarginImpact = (decimal)pyResp.Total_Margin_Impact,
                    EngineVersion = pyResp.Engine_Version
                };
            }

            // Deterministic .NET graph fallback if Python service is unavailable
            List<TwinImpactedObjectDto> fallbackImpacted = new();
            List<string> recHits = new();
            List<string> itemHits = new();
            List<string> menuHits = new();
            decimal totDelta = 0m;

            if (!string.IsNullOrWhiteSpace(request.ShockIngredientName) || request.ShockMaterialId.HasValue)
            {
                foreach (Recipe r in recipes)
                {
                    var matchingIngs = r.Ingredients.Where(i =>
                        (request.ShockMaterialId.HasValue && i.MaterialId == request.ShockMaterialId.Value) ||
                        (!string.IsNullOrWhiteSpace(request.ShockIngredientName) && i.ItemName.Contains(request.ShockIngredientName, StringComparison.OrdinalIgnoreCase))).ToList();
                    if (matchingIngs.Count == 0) continue;

                    decimal recDelta = matchingIngs.Sum(i =>
                    {
                        decimal baseCost = i.Cost > 0m ? i.Cost : (i.UnitCost.HasValue && i.UnitCost.Value > 0m ? i.Quantity * i.UnitCost.Value : 0m);
                        if (baseCost <= 0m)
                            baseCost = r.TotalCost > 0m ? r.TotalCost / Math.Max(1, r.Ingredients.Count(x => x.IsActive)) : 10m;
                        return baseCost * (request.PriceChangePercent / 100m);
                    });
                    recHits.Add(r.Name);
                    fallbackImpacted.Add(new TwinImpactedObjectDto
                    {
                        NodeId = $"rec_{r.Id}",
                        NodeType = "RECIPE",
                        Label = r.Name,
                        HopDistance = 1,
                        BaselineCost = r.TotalCost,
                        ShockedCost = r.TotalCost + recDelta,
                        CostDelta = Math.Round(recDelta, 4),
                        CostDeltaPercent = r.TotalCost > 0 ? Math.Round(recDelta / r.TotalCost * 100m, 2) : 0m,
                        MarginDelta = Math.Round(-recDelta, 4)
                    });

                    foreach (MenuItem mi in menuItems.Where(m => m.LinkedRecipeId == r.Id))
                    {
                        decimal covers = Math.Max(1m, planLines.Where(l => l.MenuItemId == mi.Id).Sum(l => l.PlannedPortions ?? l.RecommendedPortions));
                        itemHits.Add(mi.Name);
                        totDelta += recDelta * covers;
                        fallbackImpacted.Add(new TwinImpactedObjectDto
                        {
                            NodeId = $"item_{mi.Id}",
                            NodeType = "MENU_ITEM",
                            Label = mi.Name,
                            HopDistance = 2,
                            BaselineCost = r.TotalCost,
                            ShockedCost = r.TotalCost + recDelta,
                            CostDelta = Math.Round(recDelta, 4),
                            CostDeltaPercent = r.TotalCost > 0 ? Math.Round(recDelta / r.TotalCost * 100m, 2) : 0m,
                            MarginDelta = Math.Round(-recDelta * covers, 4)
                        });
                        foreach (Guid mpId in planLines.Where(l => l.MenuItemId == mi.Id).Select(l => l.MenuPlanId).Distinct())
                        {
                            string mk = menuPlans.FirstOrDefault(p => p.Id == mpId)?.Name ?? $"menu_{mpId}";
                            if (!menuHits.Contains(mk)) menuHits.Add(mk);
                        }
                    }
                }
            }

            return new TwinDependencyAnalysisResultDto
            {
                NodeCount = nodes.Count,
                EdgeCount = edges.Count,
                IsAcyclic = true,
                ShockNodeId = shockNodeId,
                PriceChangePercent = request.PriceChangePercent,
                ImpactedObjects = fallbackImpacted,
                AffectedRecipes = recHits,
                AffectedMenuItems = itemHits,
                AffectedMenus = menuHits,
                TotalCostImpact = Math.Round(totDelta, 2),
                TotalMarginImpact = Math.Round(-totDelta, 2),
                EngineVersion = "me-twin-dotnet-fallback-3.0"
            };
        }

        private TwinSnapshotDto DeserializeSnapshot(MenuEngineeringTwinSnapshot snap, DateTime liveMaxSourceUtc)
        {
            List<TwinComponentSummaryDto> comps = new();
            Dictionary<string, object> metrics = new();
            List<string> lineage = new();
            try
            {
                using JsonDocument doc = JsonDocument.Parse(snap.TwinPayloadJson);
                if (doc.RootElement.TryGetProperty("components", out var cEl))
                    comps = JsonSerializer.Deserialize<List<TwinComponentSummaryDto>>(cEl.GetRawText(), JsonOpts) ?? new();
                if (doc.RootElement.TryGetProperty("metrics", out var mEl))
                    metrics = JsonSerializer.Deserialize<Dictionary<string, object>>(mEl.GetRawText(), JsonOpts) ?? new();
                lineage = JsonSerializer.Deserialize<List<string>>(snap.LineageJson, JsonOpts) ?? new();
            }
            catch { }
            return MapSnapshotDto(snap, comps, metrics, lineage, liveMaxSourceUtc);
        }

        private static TwinSnapshotDto MapSnapshotDto(
            MenuEngineeringTwinSnapshot snap,
            List<TwinComponentSummaryDto> comps,
            Dictionary<string, object> metrics,
            List<string> lineage,
            DateTime liveMaxSourceUtc)
        {
            bool stale = snap.IsStale || liveMaxSourceUtc > snap.SourceMaxUpdatedAtUtc.AddSeconds(1) || (snap.ExpiresAtUtc.HasValue && snap.ExpiresAtUtc.Value <= DateTime.UtcNow);
            string? reason = stale
                ? (liveMaxSourceUtc > snap.SourceMaxUpdatedAtUtc.AddSeconds(1)
                    ? $"Underlying operational data updated at {liveMaxSourceUtc:u} after snapshot capture."
                    : "Snapshot expired.")
                : null;

            return new TwinSnapshotDto
            {
                SnapshotId = snap.Id,
                BuyerId = snap.BuyerId,
                PropertyId = snap.PropertyId,
                OutletLocationId = snap.OutletLocationId,
                MenuPlanId = snap.MenuPlanId,
                EventId = snap.EventId,
                SnapshotType = snap.SnapshotType,
                VersionNumber = snap.VersionNumber,
                SnapshotHash = snap.SnapshotHash,
                CalculationVersion = snap.CalculationVersion,
                ModelVersion = snap.ModelVersion,
                DataQualityScore = snap.DataQualityScore,
                DataQualityLabel = snap.DataQualityLabel,
                BusinessEffectiveDateUtc = snap.BusinessEffectiveDateUtc,
                SourceMaxUpdatedAtUtc = snap.SourceMaxUpdatedAtUtc,
                DataFreshnessUtc = snap.DataFreshnessUtc,
                ExpiresAtUtc = snap.ExpiresAtUtc,
                IsStale = stale,
                StalenessReason = reason,
                CapturedAtUtc = snap.CapturedAtUtc,
                Components = comps,
                Metrics = metrics,
                SourceLineage = lineage
            };
        }

        private static decimal ConvertToDecimal(object? val)
        {
            if (val is null) return 0m;
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Number && je.TryGetDecimal(out decimal d)) return d;
                if (je.ValueKind == JsonValueKind.String && decimal.TryParse(je.GetString(), out decimal ds)) return ds;
                return 0m;
            }
            return decimal.TryParse(val.ToString(), out decimal parsed) ? parsed : 0m;
        }

        private sealed class PyTwinGraphResp
        {
            public int Node_Count { get; set; }
            public int Edge_Count { get; set; }
            public bool Is_Acyclic { get; set; }
            public List<List<string>> Cycles { get; set; } = new();
            public List<PyIssue> Integrity_Issues { get; set; } = new();
            public string? Shock_Node_Id { get; set; }
            public double Price_Change_Percent { get; set; }
            public List<PyImpact> Impacted_Nodes { get; set; } = new();
            public List<string> Affected_Recipes { get; set; } = new();
            public List<string> Affected_Menu_Items { get; set; } = new();
            public List<string> Affected_Menus { get; set; } = new();
            public List<string> Affected_Events { get; set; } = new();
            public double Total_Cost_Impact { get; set; }
            public double Total_Margin_Impact { get; set; }
            public string Engine_Version { get; set; } = "me-twin-nx-3.0";
        }
        private sealed class PyIssue
        {
            public string Issue_Type { get; set; } = string.Empty;
            public string Severity { get; set; } = string.Empty;
            public List<string> Node_Ids { get; set; } = new();
            public string Detail { get; set; } = string.Empty;
        }
        private sealed class PyImpact
        {
            public string Node_Id { get; set; } = string.Empty;
            public string Node_Type { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public int Hop_Distance { get; set; }
            public double Baseline_Cost { get; set; }
            public double Shocked_Cost { get; set; }
            public double Cost_Delta { get; set; }
            public double Cost_Delta_Percent { get; set; }
            public double Margin_Delta { get; set; }
        }
    }
}
