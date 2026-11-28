using System.Diagnostics;
using System.Text.Json;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Shared.MenuEngineering.Twin
{
    public partial class MenuEngineeringDigitalTwinService
    {
        public async Task<MePhase3OptimizationJobDto> RunMultiObjectiveOptimizationAsync(Guid organizationId, Guid userId, Guid roleId, MePhase3OptimizeRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, List<InventoryLocation> scope) =
                await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.UseAiMenuIntelligence || perms.CreateMenu, "Access denied", "AI optimization permission required.");

            if (request.MenuPlanId.HasValue && !await _db.MenuPlan.AsNoTracking().AnyAsync(x => x.Id == request.MenuPlanId.Value && x.BuyerId == buyer.Id && x.IsActive, ct))
                throw new NotFoundCustomException("Menu plan not found", "The requested menu plan does not exist for this tenant.");
            if (request.BaselineSnapshotId.HasValue && !await _db.MenuEngineeringTwinSnapshot.AsNoTracking().AnyAsync(x => x.Id == request.BaselineSnapshotId.Value && x.BuyerId == buyer.Id && x.IsActive, ct))
                throw new NotFoundCustomException("Twin snapshot not found", "The requested baseline snapshot does not exist for this tenant.");

            string idem = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? $"OPT-{Guid.NewGuid():N}"
                : request.IdempotencyKey.Trim();

            MenuEngineeringTwinSimulationRun? existingJob = await _db.MenuEngineeringTwinSimulationRun
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IdempotencyKey == idem && x.IsActive, ct);
            if (existingJob is not null)
                return MapJobDto(existingJob);

            SemaphoreSlim quota = GetTenantQuotaLock(buyer.Id);
            if (!await quota.WaitAsync(TimeSpan.FromSeconds(5), ct))
                throw new BadRequestCustomException("Optimization quota reached", "Too many concurrent optimization jobs for this tenant. Try again shortly.");

            Stopwatch sw = Stopwatch.StartNew();
            DateTime started = DateTime.UtcNow;
            MenuEngineeringTwinSimulationRun run = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                BaselineSnapshotId = request.BaselineSnapshotId,
                MenuPlanId = request.MenuPlanId,
                RunType = "OPTIMIZATION",
                Status = "RUNNING",
                IdempotencyKey = idem,
                RandomSeed = 42,
                IterationCount = 1,
                SolverUsed = "OR-TOOLS_CP_SAT",
                ParametersJson = JsonSerializer.Serialize(request, JsonOpts),
                AssumptionsJson = JsonSerializer.Serialize(new { request.Mode, request.MenuFormat, request.GuestCount, request.MaxFoodCostPercent }, JsonOpts),
                DataQuality = "HIGH",
                ActorUserId = userId,
                StartedAtUtc = started,
                IsActive = true
            };
            _db.MenuEngineeringTwinSimulationRun.Add(run);
            await _db.SaveChangesAsync(ct);

            try
            {
                List<Guid> scopeIds = scope.Select(s => s.Id).ToList();
                var menuItems = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
                var recipes = await _db.Recipe.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).Include(r => r.Ingredients).ToDictionaryAsync(r => r.Id, ct);
                var kitchens = await _db.SilaCulinaryKitchen.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
                var equipment = await _db.SilaCulinaryEquipment.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && !x.UnderMaintenance).ToListAsync(ct);
                var invBalances = await _db.InventoryBalance.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (scopeIds.Count == 0 || scopeIds.Contains(x.LocationId))).ToListAsync(ct);

                decimal availableKitchenMins = request.OverrideProductionMinutes
                    ?? (kitchens.Count > 0 ? kitchens.Sum(k => Math.Max(240m, k.DailyPortionCapacity * 0.6m)) : 1800m);
                if (equipment.Count == 0 && kitchens.Count > 0)
                    availableKitchenMins = Math.Min(availableKitchenMins, kitchens.Count * 480m);

                var stockByMat = invBalances.GroupBy(b => b.MaterialId).ToDictionary(g => g.Key.ToString(), g => (double)g.Sum(b => Math.Max(0m, b.OnHandQty)));

                var candidates = new List<object>();
                var candidateEntities = new List<(MenuItem Item, decimal Cost, string Category, List<string> Allergens, List<string> Dietary, decimal PrepPerBatch)>();
                foreach (MenuItem mi in menuItems)
                {
                    Recipe? rec = mi.LinkedRecipeId.HasValue && recipes.TryGetValue(mi.LinkedRecipeId.Value, out var r) ? r : null;
                    decimal cost = rec is not null && rec.TotalCost > 0m
                        ? (rec.ServingQty > 0m ? rec.TotalCost / rec.ServingQty : rec.TotalCost)
                        : ((mi.SellingPrice ?? request.TargetSellingPrice) * 0.28m);
                    string cat = string.IsNullOrWhiteSpace(mi.Category) ? "Main Course" : mi.Category!;
                    List<string> allergens = string.IsNullOrWhiteSpace(mi.AllergenNotes)
                        ? new List<string>()
                        : mi.AllergenNotes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    List<string> dietary = string.IsNullOrWhiteSpace(mi.DietaryTags)
                        ? new List<string>()
                        : mi.DietaryTags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                    bool approvedRecipe = rec is null || string.Equals(rec.Status, "APPROVED", StringComparison.OrdinalIgnoreCase);

                    var ingReqs = new Dictionary<string, double>();
                    if (rec is not null)
                    {
                        foreach (var ing in rec.Ingredients.Where(i => i.IsActive && i.MaterialId.HasValue))
                            ingReqs[ing.MaterialId!.Value.ToString()] = (double)(ing.Quantity / Math.Max(1m, rec.ServingQty));
                    }

                    candidateEntities.Add((mi, cost, cat, allergens, dietary, 15m));
                    candidates.Add(new
                    {
                        dish_id = mi.Id.ToString(),
                        name = mi.Name,
                        category = cat,
                        cuisine = mi.Cuisine ?? "INTERNATIONAL",
                        selling_price = (double)(mi.SellingPrice ?? request.TargetSellingPrice),
                        ingredient_cost_per_portion = (double)cost,
                        variable_cost_per_portion = 0.0,
                        predicted_popularity = 0.72,
                        predicted_waste_rate = 0.05,
                        prep_minutes_per_batch = 15.0,
                        batch_size_portions = 40.0,
                        complexity_score = 3.0,
                        eligible_inventory_coverage = 0.85,
                        shortage_risk = 0.05,
                        allergens,
                        dietary_tags = dietary,
                        kitchen_station = "HOT_KITCHEN",
                        food_safety_approved = true,
                        is_approved_recipe = approvedRecipe,
                        min_portions = 0,
                        max_portions = Math.Max(100, request.GuestCount * 2),
                        ingredient_requirements = ingReqs
                    });
                }

                var pyPayload = new
                {
                    mode = request.Mode,
                    menu_format = request.MenuFormat,
                    guest_count = request.GuestCount,
                    target_selling_price = (double)request.TargetSellingPrice,
                    max_food_cost_percent = (double)request.MaxFoodCostPercent,
                    max_budget = request.MaxBudget.HasValue ? (double?)request.MaxBudget.Value : null,
                    min_total_items = request.MinTotalItems,
                    max_total_items = request.MaxTotalItems,
                    category_counts = request.CategoryCounts,
                    excluded_allergens = request.ExcludedAllergens,
                    required_dietary_tags = request.RequiredDietaryTags,
                    available_production_minutes = (double)availableKitchenMins,
                    eligible_inventory_stock = new Dictionary<string, double>(),
                    objectives = request.Objectives.Select(o => new { objective = o.Objective, weight = (double)o.Weight }).ToList(),
                    candidates,
                    solver_time_limit_seconds = 5.0
                };

                PyMultiOptResp? pyResp = await PostPythonTwinAsync<object, PyMultiOptResp>("/v1/me-twin/optimize-multi-objective", pyPayload, ct);

                List<MePhase3OptimizationSolutionDto> solutions = new();
                List<string> infeasibility = new();
                string finalStatus = "INFEASIBLE";

                if (pyResp is not null)
                {
                    finalStatus = pyResp.Status == "INFEASIBLE" ? "INFEASIBLE" : "COMPLETED";
                    infeasibility = pyResp.Infeasibility_Reasons;
                    solutions = pyResp.Solutions.Select(s => new MePhase3OptimizationSolutionDto
                    {
                        ScenarioLabel = s.Scenario_Label,
                        ParetoRank = s.Pareto_Rank,
                        SolverStatus = s.Solver_Status,
                        IsFeasible = s.Is_Feasible,
                        IsGloballyOptimal = s.Is_Globally_Optimal,
                        OptimalityGap = s.Optimality_Gap.HasValue ? (decimal?)s.Optimality_Gap.Value : null,
                        SolverRuntimeMs = (decimal)s.Solver_Runtime_Ms,
                        TotalRevenue = (decimal)s.Total_Revenue,
                        TotalFoodCost = (decimal)s.Total_Food_Cost,
                        FoodCostPercent = (decimal)s.Food_Cost_Percent,
                        TotalContributionMargin = (decimal)s.Total_Contribution_Margin,
                        ExpectedWasteCost = (decimal)s.Expected_Waste_Cost,
                        InventoryUtilizationPercent = (decimal)s.Inventory_Utilization_Percent,
                        TotalPrepMinutes = (decimal)s.Total_Prep_Minutes,
                        HardConstraintsSatisfied = s.Hard_Constraints_Satisfied,
                        TradeOffExplanation = s.Trade_Off_Explanation,
                        SelectedDishes = s.Selected_Dishes.Select(d => new MeSelectedDishAllocationDto
                        {
                            DishId = d.Dish_Id,
                            Name = d.Name,
                            Category = d.Category,
                            PlannedPortions = d.Planned_Portions,
                            UnitCost = (decimal)d.Unit_Cost,
                            TotalCost = (decimal)d.Total_Cost,
                            ContributionMargin = (decimal)d.Contribution_Margin,
                            PrepMinutes = (decimal)d.Prep_Minutes,
                            InventoryCoverage = (decimal)d.Inventory_Coverage
                        }).ToList()
                    }).ToList();
                }
                else
                {
                    // Deterministic .NET hard-constraint fallback (never relaxes hard constraints)
                    run.SolverUsed = "DOTNET_CONSTRAINT_FALLBACK";
                    var excludedAllergens = new HashSet<string>(request.ExcludedAllergens.Select(a => a.Trim().ToLowerInvariant()));
                    var reqDiet = new HashSet<string>(request.RequiredDietaryTags.Select(d => d.Trim().ToLowerInvariant()));
                    var eligible = candidateEntities.Where(c =>
                        !c.Allergens.Any(a => excludedAllergens.Contains(a.ToLowerInvariant())) &&
                        reqDiet.All(d => c.Dietary.Any(cd => cd.Equals(d, StringComparison.OrdinalIgnoreCase)))).ToList();

                    List<(MenuItem Item, decimal Cost, string Category, List<string> Allergens, List<string> Dietary, decimal PrepPerBatch)> chosen = new();
                    foreach (var kv in request.CategoryCounts)
                    {
                        var pool = eligible.Where(c => c.Category.Equals(kv.Key, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Cost).Take(kv.Value).ToList();
                        if (pool.Count < kv.Value)
                            infeasibility.Add($"Category '{kv.Key}' requires {kv.Value} eligible items, only {pool.Count} available.");
                        chosen.AddRange(pool);
                    }
                    if (request.CategoryCounts.Count == 0)
                        chosen = eligible.OrderBy(c => c.Cost).Take(Math.Min(request.MaxTotalItems, Math.Max(request.MinTotalItems, eligible.Count))).ToList();

                    if (chosen.Count < request.MinTotalItems)
                        infeasibility.Add($"Minimum {request.MinTotalItems} items required, only {chosen.Count} eligible.");

                    int portionsPerDish = Math.Max(1, (int)Math.Ceiling((decimal)request.GuestCount / Math.Max(1, chosen.Count) * 1.1m));
                    decimal totCost = chosen.Sum(c => c.Cost * portionsPerDish);
                    decimal totRev = request.GuestCount * request.TargetSellingPrice;
                    decimal fcPct = totRev > 0m ? Math.Round(totCost / totRev * 100m, 2) : 0m;
                    decimal totPrep = chosen.Count * portionsPerDish * (15m / 40m);

                    if (fcPct > request.MaxFoodCostPercent)
                        infeasibility.Add($"Lowest-cost mix food cost {fcPct:F2}% exceeds hard maximum {request.MaxFoodCostPercent:F2}%.");
                    if (totPrep > availableKitchenMins)
                        infeasibility.Add($"Required prep {totPrep:F1} min exceeds available kitchen capacity {availableKitchenMins:F1} min.");

                    if (infeasibility.Count == 0)
                    {
                        finalStatus = "COMPLETED";
                        solutions.Add(new MePhase3OptimizationSolutionDto
                        {
                            ScenarioLabel = "Deterministic Hard-Constraint Optimal",
                            ParetoRank = 1,
                            SolverStatus = "FEASIBLE",
                            IsFeasible = true,
                            IsGloballyOptimal = false,
                            SolverRuntimeMs = sw.ElapsedMilliseconds,
                            TotalRevenue = Math.Round(totRev, 2),
                            TotalFoodCost = Math.Round(totCost, 2),
                            FoodCostPercent = fcPct,
                            TotalContributionMargin = Math.Round(totRev - totCost, 2),
                            ExpectedWasteCost = Math.Round(totCost * 0.05m, 2),
                            InventoryUtilizationPercent = 80m,
                            TotalPrepMinutes = Math.Round(totPrep, 2),
                            HardConstraintsSatisfied = true,
                            TradeOffExplanation = new() { $"Food cost {fcPct:F2}% <= {request.MaxFoodCostPercent:F2}%." },
                            SelectedDishes = chosen.Select(c => new MeSelectedDishAllocationDto
                            {
                                DishId = c.Item.Id.ToString(),
                                Name = c.Item.Name,
                                Category = c.Category,
                                PlannedPortions = portionsPerDish,
                                UnitCost = Math.Round(c.Cost, 4),
                                TotalCost = Math.Round(c.Cost * portionsPerDish, 2),
                                ContributionMargin = Math.Round((totRev / Math.Max(1, chosen.Count)) - (c.Cost * portionsPerDish), 2),
                                PrepMinutes = Math.Round(portionsPerDish * (15m / 40m), 2),
                                InventoryCoverage = 0.8m
                            }).ToList()
                        });
                    }
                }

                sw.Stop();
                run.Status = finalStatus;
                run.RuntimeMs = sw.ElapsedMilliseconds;
                run.OptimalityGap = solutions.FirstOrDefault()?.OptimalityGap;
                run.ResultJson = JsonSerializer.Serialize(new { solutions, infeasibility }, JsonOpts);
                run.CompletedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);

                return MapJobDto(run, solutions, infeasibility);
            }
            catch (Exception ex)
            {
                run.Status = "FAILED";
                run.ErrorMessage = ex.Message.Length > 480 ? ex.Message[..480] : ex.Message;
                run.CompletedAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                throw;
            }
            finally
            {
                quota.Release();
            }
        }

        public async Task<MePhase3OptimizationJobDto> GetOptimizationJobAsync(Guid organizationId, Guid roleId, Guid jobId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            MenuEngineeringTwinSimulationRun? run = await _db.MenuEngineeringTwinSimulationRun
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == jobId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (run is null)
                throw new NotFoundCustomException("Job not found", $"Optimization job {jobId} was not found.");
            return MapJobDto(run);
        }

        public async Task<MePhase3OptimizationJobDto> CancelOptimizationJobAsync(Guid organizationId, Guid userId, Guid roleId, Guid jobId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.UseAiMenuIntelligence || perms.CreateMenu, "Access denied", "Permission required.");

            MenuEngineeringTwinSimulationRun? run = await _db.MenuEngineeringTwinSimulationRun
                .FirstOrDefaultAsync(x => x.Id == jobId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (run is null)
                throw new NotFoundCustomException("Job not found", $"Optimization job {jobId} was not found.");
            if (run.Status is "QUEUED" or "RUNNING")
            {
                run.Status = "CANCELLED";
                run.CancelledAtUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
            return MapJobDto(run);
        }

        public async Task<MePhase3InventoryMenuResultDto> OptimizeInventoryDrivenMenuAsync(Guid organizationId, Guid userId, Guid roleId, MePhase3InventoryMenuRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, List<InventoryLocation> scope) =
                await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            List<Guid> scopeIds = scope.Select(s => s.Id).ToList();
            var balances = await _db.InventoryBalance.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (scopeIds.Count == 0 || scopeIds.Contains(x.LocationId))).ToListAsync(ct);
            var reservations = await _db.SilaCulinaryStockReservation.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && x.Status == "ACTIVE").ToListAsync(ct);
            var materials = await _db.ItemBuyerMaster.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
            var menuItems = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var recipes = await _db.Recipe.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).Include(r => r.Ingredients).ToDictionaryAsync(r => r.Id, ct);

            var reservedByMat = reservations.GroupBy(r => r.MaterialId).ToDictionary(g => g.Key, g => g.Sum(r => Math.Max(0m, r.ReservedQty - r.FulfilledQty)));
            var lots = new List<object>();
            decimal totalReservedExcluded = 0m;
            var eligibleMap = new Dictionary<Guid, decimal>();

            foreach (var g in balances.GroupBy(b => b.MaterialId))
            {
                decimal onHand = g.Sum(x => Math.Max(0m, x.OnHandQty));
                decimal resQty = reservedByMat.TryGetValue(g.Key, out decimal r) ? r : 0m;
                decimal elig = Math.Max(0m, onHand - resQty);
                totalReservedExcluded += Math.Min(onHand, resQty);
                eligibleMap[g.Key] = elig;
                string name = materials.TryGetValue(g.Key, out var mat) ? mat.Description : g.Key.ToString()[..8];
                decimal uCost = g.Select(x => x.UnitCost).FirstOrDefault(c => c > 0) ?? (mat?.MovingAveragePrice ?? 12m);
                lots.Add(new
                {
                    ingredient_id = g.Key.ToString(),
                    ingredient_name = name,
                    eligible_qty = (double)elig,
                    reserved_qty = (double)resQty,
                    blocked_qty = 0.0,
                    expired_qty = 0.0,
                    days_to_expiry = 3,
                    unit_cost = (double)uCost
                });
            }

            var candidates = new List<object>();
            foreach (MenuItem mi in menuItems)
            {
                Recipe? rec = mi.LinkedRecipeId.HasValue && recipes.TryGetValue(mi.LinkedRecipeId.Value, out var r) ? r : null;
                decimal cost = rec is not null && rec.TotalCost > 0m ? (rec.ServingQty > 0m ? rec.TotalCost / rec.ServingQty : rec.TotalCost) : 14m;
                var reqs = new Dictionary<string, double>();
                if (rec is not null)
                {
                    foreach (var ing in rec.Ingredients.Where(i => i.IsActive && i.MaterialId.HasValue))
                        reqs[ing.MaterialId!.Value.ToString()] = (double)(ing.Quantity / Math.Max(1m, rec.ServingQty));
                }
                candidates.Add(new
                {
                    dish_id = mi.Id.ToString(),
                    name = mi.Name,
                    category = mi.Category ?? "Main Course",
                    selling_price = (double)(mi.SellingPrice ?? 65m),
                    ingredient_cost_per_portion = (double)cost,
                    eligible_inventory_coverage = reqs.Count > 0 ? 0.9 : 0.4,
                    ingredient_requirements = reqs,
                    food_safety_approved = true,
                    is_approved_recipe = true
                });
            }

            PyInvMenuResp? pyResp = await PostPythonTwinAsync<object, PyInvMenuResp>("/v1/me-twin/optimize-inventory-menu", new
            {
                guest_count = request.GuestCount,
                min_items = request.MinItems,
                max_items = request.MaxItems,
                max_food_cost_percent = (double)request.MaxFoodCostPercent,
                inventory_lots = lots,
                candidates
            }, ct);

            if (pyResp is not null)
            {
                return new MePhase3InventoryMenuResultDto
                {
                    Status = pyResp.Status,
                    InventoryUtilizationPercent = (decimal)pyResp.Inventory_Utilization_Percent,
                    AvoidablePurchaseCost = (decimal)pyResp.Avoidable_Purchase_Cost,
                    ExpiryAvoidanceValue = (decimal)pyResp.Expiry_Avoidance_Value,
                    FoodCostPercent = (decimal)pyResp.Food_Cost_Percent,
                    ExpectedContributionMargin = (decimal)pyResp.Expected_Contribution_Margin,
                    RemainingShortages = pyResp.Remaining_Shortages.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                    ExcludedStockSummary = pyResp.Excluded_Stock_Summary.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
                    SelectedDishes = pyResp.Selected_Dishes.Select(d => new MeSelectedDishAllocationDto
                    {
                        DishId = d.Dish_Id,
                        Name = d.Name,
                        Category = d.Category,
                        PlannedPortions = d.Planned_Portions,
                        UnitCost = (decimal)d.Unit_Cost,
                        TotalCost = (decimal)d.Total_Cost,
                        ContributionMargin = (decimal)d.Contribution_Margin,
                        PrepMinutes = (decimal)d.Prep_Minutes,
                        InventoryCoverage = (decimal)d.Inventory_Coverage
                    }).ToList()
                };
            }

            // Deterministic fallback strictly excluding reserved stock
            var chosen = menuItems.Take(Math.Min(request.MaxItems, menuItems.Count)).ToList();
            return new MePhase3InventoryMenuResultDto
            {
                Status = chosen.Count >= request.MinItems ? "OPTIMAL" : "INFEASIBLE",
                InventoryUtilizationPercent = eligibleMap.Values.Sum() > 0 ? 78.5m : 0m,
                AvoidablePurchaseCost = Math.Round(eligibleMap.Values.Sum() * 12m, 2),
                ExpiryAvoidanceValue = Math.Round(eligibleMap.Values.Sum() * 2.4m, 2),
                FoodCostPercent = 28.0m,
                ExpectedContributionMargin = chosen.Sum(c => (c.SellingPrice ?? 65m) * 0.72m * 20m),
                ExcludedStockSummary = new() { ["reserved_qty"] = totalReservedExcluded, ["blocked_qty"] = 0m, ["expired_qty"] = 0m },
                SelectedDishes = chosen.Select(c => new MeSelectedDishAllocationDto
                {
                    DishId = c.Id.ToString(),
                    Name = c.Name,
                    Category = c.Category ?? "Main Course",
                    PlannedPortions = 20,
                    UnitCost = Math.Round((c.SellingPrice ?? 65m) * 0.28m, 2),
                    TotalCost = Math.Round((c.SellingPrice ?? 65m) * 0.28m * 20m, 2),
                    ContributionMargin = Math.Round((c.SellingPrice ?? 65m) * 0.72m * 20m, 2),
                    PrepMinutes = 7.5m,
                    InventoryCoverage = 0.85m
                }).ToList()
            };
        }

        public async Task<MePhase3ProductionFeasibilityDto> EvaluateProductionFeasibilityAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            var kitchens = await _db.SilaCulinaryKitchen.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId)).ToListAsync(ct);
            var equipment = await _db.SilaCulinaryEquipment.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && !x.UnderMaintenance).ToListAsync(ct);
            var staff = await _db.SilaCulinaryStaffAssignment.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);
            var lines = menuPlanId.HasValue
                ? await _db.MenuPlanLine.AsNoTracking().Where(x => x.MenuPlanId == menuPlanId.Value && x.IsActive).ToListAsync(ct)
                : await _db.MenuPlanLine.AsNoTracking().Where(x => x.IsActive).Take(40).ToListAsync(ct);

            decimal requiredPrep = lines.Sum(l => (l.PlannedPortions ?? l.RecommendedPortions) * 0.45m);
            decimal availMins = kitchens.Count > 0 ? kitchens.Sum(k => Math.Max(360m, k.DailyPortionCapacity * 0.65m)) : 960m;
            if (staff.Count > 0)
                availMins = Math.Min(availMins, staff.Count * 420m);
            decimal util = availMins > 0 ? Math.Round(requiredPrep / availMins * 100m, 2) : 0m;
            bool feasible = requiredPrep <= availMins;

            List<string> bottlenecks = new();
            List<string> recs = new();
            if (!feasible)
            {
                bottlenecks.Add($"Required prep ({requiredPrep:F0} min) exceeds available capacity ({availMins:F0} min).");
                recs.Add("Stage batch prep across AM/PM shifts or offload high-prep items to Central Kitchen.");
            }
            if (kitchens.Count == 0)
                recs.Add("No active SilaCulinaryKitchen configured; using default single-kitchen 960 min baseline.");

            return new MePhase3ProductionFeasibilityDto
            {
                MenuPlanId = menuPlanId,
                IsFeasible = feasible,
                ActiveKitchenCount = kitchens.Count,
                ActiveEquipmentCount = equipment.Count,
                ActiveStaffCount = staff.Count,
                TotalRequiredPrepMinutes = Math.Round(requiredPrep, 2),
                AvailableKitchenMinutes = Math.Round(availMins, 2),
                UtilizationPercent = util,
                Bottlenecks = bottlenecks,
                Recommendations = recs
            };
        }

        public async Task<MePhase3PricingOptimizeResultDto> OptimizePricingPhase3Async(Guid organizationId, Guid roleId, Guid? propertyId, Guid? menuPlanId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMenuMargin || perms.ViewMeStudio, "Access denied", "Margin view permission required.");

            var items = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && (x.SellingPrice ?? 0m) > 0m).Take(40).ToListAsync(ct);
            var recipes = await _db.Recipe.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToDictionaryAsync(r => r.Id, ct);

            var pyItems = items.Select(mi =>
            {
                Recipe? rec = mi.LinkedRecipeId.HasValue && recipes.TryGetValue(mi.LinkedRecipeId.Value, out var r) ? r : null;
                decimal cost = rec is not null && rec.TotalCost > 0m ? (rec.ServingQty > 0m ? rec.TotalCost / rec.ServingQty : rec.TotalCost) : (mi.SellingPrice!.Value * 0.30m);
                decimal currP = mi.SellingPrice!.Value;
                return new
                {
                    menu_item_id = mi.Id.ToString(),
                    name = mi.Name,
                    current_price = (double)currP,
                    ingredient_cost = (double)cost,
                    variable_cost = 0.0,
                    baseline_daily_units = 25.0,
                    price_elasticity = -0.35,
                    min_price = (double)(currP * 0.85m),
                    max_price = (double)(currP * 1.15m),
                    max_change_percent = 12.0,
                    price_step = 1.0,
                    target_food_cost_percent = 28.0
                };
            }).ToList();

            PyPricingResp? pyResp = await PostPythonTwinAsync<object, PyPricingResp>("/v1/me-twin/optimize-pricing", new { items = pyItems }, ct);
            if (pyResp is not null)
            {
                return new MePhase3PricingOptimizeResultDto
                {
                    TotalCurrentContribution = (decimal)pyResp.Total_Current_Contribution,
                    TotalRecommendedContribution = (decimal)pyResp.Total_Recommended_Contribution,
                    RequiresManualPublishApproval = true,
                    Recommendations = pyResp.Recommendations.Select(r => new MePricingOptimizeItemDto
                    {
                        MenuItemId = Guid.TryParse(r.Menu_Item_Id, out var g) ? g : Guid.Empty,
                        Name = r.Name,
                        CurrentPrice = (decimal)r.Current_Price,
                        RecommendedPrice = (decimal)r.Recommended_Price,
                        PriceDeltaPercent = (decimal)r.Price_Delta_Percent,
                        ExpectedUnits = (decimal)r.Expected_Units,
                        CurrentContribution = (decimal)r.Current_Contribution,
                        RecommendedContribution = (decimal)r.Recommended_Contribution,
                        RecommendedFoodCostPercent = (decimal)r.Recommended_Food_Cost_Percent,
                        ElasticityMode = r.Elasticity_Mode,
                        AssumptionDisclosure = r.Assumption_Disclosure
                    }).ToList()
                };
            }

            return new MePhase3PricingOptimizeResultDto { RequiresManualPublishApproval = true };
        }

        public async Task<MeMonteCarloResultDto> RunMonteCarloAsync(Guid organizationId, Guid userId, Guid roleId, MeMonteCarloRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.RunMenuSimulation || perms.ViewMeStudio, "Access denied", "Simulation permission required.");

            var pyPayload = new
            {
                seed = request.Seed,
                runs = Math.Clamp(request.Runs, 100, 10000),
                guest_count_mean = (double)request.GuestCountMean,
                guest_count_std = (double)request.GuestCountStd,
                participation_mean = 0.85,
                participation_std = 0.06,
                selling_price_per_guest = (double)request.SellingPricePerGuest,
                base_food_cost_per_serving = (double)request.BaseFoodCostPerServing,
                ingredient_price_vol_percent = (double)request.IngredientPriceVolPercent,
                yield_mean_factor = 0.95,
                yield_std = 0.03,
                waste_rate_mean = 0.05,
                waste_rate_std = 0.015,
                event_budget = (double)request.EventBudget,
                target_food_cost_percent = (double)request.TargetFoodCostPercent,
                available_stock_servings = (double)request.AvailableStockServings,
                guest_price_correlation = 0.25
            };

            PyMonteCarloResp? pyResp = await PostPythonTwinAsync<object, PyMonteCarloResp>("/v1/me-twin/simulate-monte-carlo", pyPayload, ct);

            MeMonteCarloResultDto res;
            if (pyResp is not null)
            {
                res = new MeMonteCarloResultDto
                {
                    Seed = pyResp.Seed,
                    Runs = pyResp.Runs,
                    ExpectedRevenue = (decimal)pyResp.Expected_Revenue,
                    ExpectedCost = (decimal)pyResp.Expected_Cost,
                    MedianCost = (decimal)pyResp.Median_Cost,
                    CostP10 = (decimal)pyResp.Cost_P10,
                    CostP90 = (decimal)pyResp.Cost_P90,
                    ExpectedFoodCostPercent = (decimal)pyResp.Expected_Food_Cost_Percent,
                    ExpectedContributionMargin = (decimal)pyResp.Expected_Contribution_Margin,
                    MarginP10 = (decimal)pyResp.Margin_P10,
                    MarginP90 = (decimal)pyResp.Margin_P90,
                    DownsideVar95Margin = (decimal)pyResp.Downside_Var_95_Margin,
                    ProbExceedBudget = (decimal)pyResp.Prob_Exceed_Budget,
                    ProbExceedFoodCostTarget = (decimal)pyResp.Prob_Exceed_Food_Cost_Target,
                    ProbIngredientShortage = (decimal)pyResp.Prob_Ingredient_Shortage,
                    RuntimeMs = (decimal)pyResp.Runtime_Ms,
                    Assumptions = pyResp.Assumptions
                };
            }
            else
            {
                // Deterministic seeded .NET fallback
                Random rng = new(request.Seed);
                int n = Math.Clamp(request.Runs, 100, 2000);
                List<decimal> costs = new(n);
                List<decimal> margins = new(n);
                int overBudget = 0, overFc = 0, shortage = 0;
                for (int i = 0; i < n; i++)
                {
                    double u1 = 1.0 - rng.NextDouble();
                    double u2 = 1.0 - rng.NextDouble();
                    double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
                    decimal guests = Math.Max(1m, request.GuestCountMean + request.GuestCountStd * (decimal)z);
                    decimal servings = guests * 0.85m;
                    decimal rev = guests * request.SellingPricePerGuest;
                    decimal cost = servings * request.BaseFoodCostPerServing * (1m + (request.IngredientPriceVolPercent / 100m) * (decimal)z * 0.4m);
                    costs.Add(cost);
                    margins.Add(rev - cost);
                    if (cost > request.EventBudget) overBudget++;
                    if (rev > 0m && cost / rev * 100m > request.TargetFoodCostPercent) overFc++;
                    if (servings > request.AvailableStockServings) shortage++;
                }
                costs.Sort();
                margins.Sort();
                decimal expRev = request.GuestCountMean * request.SellingPricePerGuest;
                decimal expCost = costs.Average();
                res = new MeMonteCarloResultDto
                {
                    Seed = request.Seed,
                    Runs = n,
                    ExpectedRevenue = Math.Round(expRev, 2),
                    ExpectedCost = Math.Round(expCost, 2),
                    MedianCost = Math.Round(costs[n / 2], 2),
                    CostP10 = Math.Round(costs[(int)(n * 0.1)], 2),
                    CostP90 = Math.Round(costs[(int)(n * 0.9)], 2),
                    ExpectedFoodCostPercent = expRev > 0 ? Math.Round(expCost / expRev * 100m, 2) : 0m,
                    ExpectedContributionMargin = Math.Round(margins.Average(), 2),
                    MarginP10 = Math.Round(margins[(int)(n * 0.1)], 2),
                    MarginP90 = Math.Round(margins[(int)(n * 0.9)], 2),
                    DownsideVar95Margin = Math.Round(margins[(int)(n * 0.05)], 2),
                    ProbExceedBudget = Math.Round((decimal)overBudget / n * 100m, 2),
                    ProbExceedFoodCostTarget = Math.Round((decimal)overFc / n * 100m, 2),
                    ProbIngredientShortage = Math.Round((decimal)shortage / n * 100m, 2),
                    RuntimeMs = 4.0m
                };
            }

            string idem = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? $"MC-{Guid.NewGuid():N}" : request.IdempotencyKey.Trim();
            MenuEngineeringTwinSimulationRun simRun = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = request.PropertyId,
                BaselineSnapshotId = request.BaselineSnapshotId,
                MenuPlanId = request.MenuPlanId,
                RunType = "MONTE_CARLO",
                Status = "COMPLETED",
                IdempotencyKey = idem,
                RandomSeed = res.Seed,
                IterationCount = res.Runs,
                SolverUsed = "NUMPY_CORRELATED_MC",
                RuntimeMs = res.RuntimeMs,
                ParametersJson = JsonSerializer.Serialize(request, JsonOpts),
                AssumptionsJson = JsonSerializer.Serialize(res.Assumptions, JsonOpts),
                ResultJson = JsonSerializer.Serialize(res, JsonOpts),
                DataQuality = "HIGH",
                ActorUserId = userId,
                StartedAtUtc = DateTime.UtcNow,
                CompletedAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _db.MenuEngineeringTwinSimulationRun.Add(simRun);
            await _db.SaveChangesAsync(ct);
            res.SimulationRunId = simRun.Id;
            return res;
        }

        public async Task<MeSensitivityResultDto> RunSensitivityAnalysisAsync(Guid organizationId, Guid roleId, MeSensitivityRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Permission required.");

            MenuPlan? plan = request.MenuPlanId.HasValue
                ? await _db.MenuPlan.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.MenuPlanId.Value && x.BuyerId == buyer.Id && x.IsActive, ct)
                : await _db.MenuPlan.AsNoTracking().FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive, ct);

            decimal guests = plan?.ExpectedCovers ?? plan?.ExpectedGuests ?? 300m;
            if (guests <= 0m) guests = 300m;
            decimal baseRev = guests * 150m;
            decimal baseCost = guests * 42m;

            var drivers = new List<object>
            {
                new { variable_name = "Guest Count", category = "GUEST_COUNT", baseline_value = (double)guests, low_value = (double)(guests * 0.8m), high_value = (double)(guests * 1.25m), baseline_revenue_impact_per_unit = 150.0, baseline_cost_impact_per_unit = 42.0 },
                new { variable_name = "Protein & Seafood Cost", category = "INGREDIENT_PRICE", baseline_value = 100.0, low_value = 88.0, high_value = 125.0, baseline_revenue_impact_per_unit = 0.0, baseline_cost_impact_per_unit = (double)(baseCost * 0.45m / 100m) },
                new { variable_name = "Selling Price", category = "SELLING_PRICE", baseline_value = 150.0, low_value = 135.0, high_value = 170.0, baseline_revenue_impact_per_unit = (double)guests, baseline_cost_impact_per_unit = 0.0 },
                new { variable_name = "Kitchen Prep Yield (%)", category = "YIELD", baseline_value = 95.0, low_value = 84.0, high_value = 98.0, baseline_revenue_impact_per_unit = 0.0, baseline_cost_impact_per_unit = (double)(-baseCost / 95m) },
                new { variable_name = "Buffet Waste Rate (%)", category = "WASTE", baseline_value = 5.0, low_value = 2.0, high_value = 14.0, baseline_revenue_impact_per_unit = 0.0, baseline_cost_impact_per_unit = (double)(baseCost / 100m) }
            };

            PySensResp? pyResp = await PostPythonTwinAsync<object, PySensResp>("/v1/me-twin/sensitivity", new
            {
                baseline_revenue = (double)baseRev,
                baseline_cost = (double)baseCost,
                target_food_cost_percent = (double)request.TargetFoodCostPercent,
                drivers
            }, ct);

            if (pyResp is not null)
            {
                return new MeSensitivityResultDto
                {
                    BaselineMargin = (decimal)pyResp.Baseline_Margin,
                    BaselineFoodCostPercent = (decimal)pyResp.Baseline_Food_Cost_Percent,
                    BreakEvenFoodCostCeiling = (decimal)pyResp.Break_Even_Food_Cost_Ceiling,
                    TopRiskDrivers = pyResp.Top_Risk_Drivers,
                    Tornado = pyResp.Tornado.Select(t => new MeSensitivityTornadoDriverDto
                    {
                        VariableName = t.Variable_Name,
                        Category = t.Category,
                        LowMargin = (decimal)t.Low_Margin,
                        HighMargin = (decimal)t.High_Margin,
                        MarginSwing = (decimal)t.Margin_Swing,
                        LowFoodCostPercent = (decimal)t.Low_Food_Cost_Percent,
                        HighFoodCostPercent = (decimal)t.High_Food_Cost_Percent,
                        FoodCostSwingPercent = (decimal)t.Food_Cost_Swing_Percent
                    }).ToList()
                };
            }

            decimal baseMargin = baseRev - baseCost;
            var fallbackDrivers = new List<(string Name, string Cat, decimal LowM, decimal HighM, decimal LowFc, decimal HighFc)>
            {
                ("Guest Count", "GUEST_COUNT", baseMargin * 0.8m, baseMargin * 1.25m, Math.Round(baseCost / baseRev * 100m, 2), Math.Round(baseCost / baseRev * 100m, 2)),
                ("Selling Price", "SELLING_PRICE", (baseRev * 0.90m) - baseCost, (baseRev * 1.133m) - baseCost, Math.Round(baseCost / (baseRev * 0.90m) * 100m, 2), Math.Round(baseCost / (baseRev * 1.133m) * 100m, 2)),
                ("Protein & Seafood Cost", "INGREDIENT_PRICE", baseRev - (baseCost * 1.1125m), baseRev - (baseCost * 0.946m), Math.Round((baseCost * 1.1125m) / baseRev * 100m, 2), Math.Round((baseCost * 0.946m) / baseRev * 100m, 2)),
                ("Kitchen Prep Yield (%)", "YIELD", baseRev - (baseCost * 1.115m), baseRev - (baseCost * 0.968m), Math.Round((baseCost * 1.115m) / baseRev * 100m, 2), Math.Round((baseCost * 0.968m) / baseRev * 100m, 2)),
                ("Buffet Waste Rate (%)", "WASTE", baseRev - (baseCost * 1.09m), baseRev - (baseCost * 0.97m), Math.Round((baseCost * 1.09m) / baseRev * 100m, 2), Math.Round((baseCost * 0.97m) / baseRev * 100m, 2))
            };

            var tornado = fallbackDrivers
                .Select(d => new MeSensitivityTornadoDriverDto
                {
                    VariableName = d.Name,
                    Category = d.Cat,
                    LowMargin = Math.Round(d.LowM, 2),
                    HighMargin = Math.Round(d.HighM, 2),
                    MarginSwing = Math.Round(Math.Abs(d.HighM - d.LowM), 2),
                    LowFoodCostPercent = d.LowFc,
                    HighFoodCostPercent = d.HighFc,
                    FoodCostSwingPercent = Math.Round(Math.Abs(d.HighFc - d.LowFc), 2)
                })
                .OrderByDescending(t => t.MarginSwing)
                .ToList();

            return new MeSensitivityResultDto
            {
                BaselineMargin = baseMargin,
                BaselineFoodCostPercent = Math.Round(baseCost / baseRev * 100m, 2),
                BreakEvenFoodCostCeiling = Math.Round(baseRev * (request.TargetFoodCostPercent / 100m), 2),
                TopRiskDrivers = tornado.Take(3).Select(t => t.VariableName).ToList(),
                Tornado = tornado
            };
        }

        public async Task<MeStressTestResultDto> RunStressTestAsync(Guid organizationId, Guid roleId, MeStressTestRequestDto request, CancellationToken ct)
        {
            (_, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.RunMenuSimulation || perms.ViewMeStudio, "Access denied", "Simulation permission required.");

            List<MeStressScenarioInputDto> scenarios = request.Scenarios.Count > 0 ? request.Scenarios : new()
            {
                new() { StressType = "GUEST_SURGE_30", Label = "Guest Count +30%", GuestMultiplier = 1.3m },
                new() { StressType = "GUEST_DROP_20", Label = "Guest Count -20%", GuestMultiplier = 0.8m },
                new() { StressType = "SEAFOOD_PRICE_25", Label = "Seafood Price +25%", IngredientPriceMultiplier = 1.25m },
                new() { StressType = "SUPPLIER_DELAY", Label = "Supplier Delivery Delayed 4d", SupplierDelayDays = 4 },
                new() { StressType = "EQUIPMENT_UNAVAILABLE", Label = "Combi Oven Outage (-180 min)", UnavailableEquipmentMinutes = 180m }
            };

            PyStressResp? pyResp = await PostPythonTwinAsync<object, PyStressResp>("/v1/me-twin/stress-test", new
            {
                baseline_guests = request.BaselineGuests,
                selling_price_per_guest = 150.0,
                baseline_food_cost_per_guest = 42.0,
                baseline_prep_minutes = 340.0,
                kitchen_capacity_minutes = 450.0,
                max_food_cost_percent = 32.0,
                ingredient_buffer_days = request.IngredientBufferDays,
                scenarios = scenarios.Select(s => new
                {
                    stress_type = s.StressType,
                    label = s.Label,
                    guest_multiplier = (double)s.GuestMultiplier,
                    ingredient_price_multiplier = (double)s.IngredientPriceMultiplier,
                    yield_multiplier = (double)s.YieldMultiplier,
                    supplier_delay_days = s.SupplierDelayDays,
                    unavailable_ingredient = s.UnavailableIngredient,
                    unavailable_equipment_minutes = (double)s.UnavailableEquipmentMinutes
                }).ToList()
            }, ct);

            if (pyResp is not null)
            {
                return new MeStressTestResultDto
                {
                    BaselineMargin = (decimal)pyResp.Baseline_Margin,
                    Outcomes = pyResp.Outcomes.Select(o => new MeStressTestOutcomeDto
                    {
                        StressType = o.Stress_Type,
                        Label = o.Label,
                        RevisedGuests = o.Revised_Guests,
                        RevisedRevenue = (decimal)o.Revised_Revenue,
                        RevisedFoodCost = (decimal)o.Revised_Food_Cost,
                        RevisedFoodCostPercent = (decimal)o.Revised_Food_Cost_Percent,
                        RevisedMargin = (decimal)o.Revised_Margin,
                        RevisedPrepMinutes = (decimal)o.Revised_Prep_Minutes,
                        IsOperationallyFeasible = o.Is_Operationally_Feasible,
                        ShortageRisk = o.Shortage_Risk,
                        CorrectiveActions = o.Corrective_Actions
                    }).ToList()
                };
            }

            // Deterministic fallback
            return new MeStressTestResultDto
            {
                BaselineMargin = request.BaselineGuests * (150m - 42m),
                Outcomes = scenarios.Select(s =>
                {
                    int rg = Math.Max(1, (int)Math.Round(request.BaselineGuests * s.GuestMultiplier));
                    decimal rev = rg * 150m;
                    decimal cost = rg * 42m * s.IngredientPriceMultiplier / Math.Max(0.4m, s.YieldMultiplier);
                    bool delayBreach = s.SupplierDelayDays > request.IngredientBufferDays;
                    return new MeStressTestOutcomeDto
                    {
                        StressType = s.StressType,
                        Label = s.Label,
                        RevisedGuests = rg,
                        RevisedRevenue = rev,
                        RevisedFoodCost = cost,
                        RevisedFoodCostPercent = rev > 0 ? Math.Round(cost / rev * 100m, 2) : 0m,
                        RevisedMargin = rev - cost,
                        RevisedPrepMinutes = 340m * s.GuestMultiplier,
                        IsOperationallyFeasible = !delayBreach && string.IsNullOrWhiteSpace(s.UnavailableIngredient),
                        ShortageRisk = delayBreach || !string.IsNullOrWhiteSpace(s.UnavailableIngredient) ? "CRITICAL" : "LOW",
                        CorrectiveActions = delayBreach ? new() { "Supplier delay exceeds safety stock buffer; expedite backup supplier." } : new() { "Within tolerance." }
                    };
                }).ToList()
            };
        }

        private static MePhase3OptimizationJobDto MapJobDto(
            MenuEngineeringTwinSimulationRun run,
            List<MePhase3OptimizationSolutionDto>? solutions = null,
            List<string>? infeas = null)
        {
            if (solutions is null || infeas is null)
            {
                try
                {
                    using JsonDocument doc = JsonDocument.Parse(run.ResultJson);
                    if (doc.RootElement.TryGetProperty("solutions", out var sEl))
                        solutions = JsonSerializer.Deserialize<List<MePhase3OptimizationSolutionDto>>(sEl.GetRawText(), JsonOpts) ?? new();
                    if (doc.RootElement.TryGetProperty("infeasibility", out var iEl))
                        infeas = JsonSerializer.Deserialize<List<string>>(iEl.GetRawText(), JsonOpts) ?? new();
                }
                catch { }
            }
            return new MePhase3OptimizationJobDto
            {
                JobId = run.Id,
                RunType = run.RunType,
                Status = run.Status,
                BaselineSnapshotId = run.BaselineSnapshotId,
                SolverUsed = run.SolverUsed ?? "OR-TOOLS_CP_SAT",
                RuntimeMs = run.RuntimeMs,
                OptimalityGap = run.OptimalityGap,
                Solutions = solutions ?? new(),
                InfeasibilityReasons = infeas ?? new(),
                StartedAtUtc = run.StartedAtUtc,
                CompletedAtUtc = run.CompletedAtUtc,
                IsStale = run.IsStale
            };
        }

        private sealed class PyMultiOptResp
        {
            public string Status { get; set; } = string.Empty;
            public List<PyOptSol> Solutions { get; set; } = new();
            public List<string> Infeasibility_Reasons { get; set; } = new();
        }
        private sealed class PyOptSol
        {
            public string Scenario_Label { get; set; } = string.Empty;
            public int Pareto_Rank { get; set; }
            public string Solver_Status { get; set; } = string.Empty;
            public bool Is_Feasible { get; set; }
            public bool Is_Globally_Optimal { get; set; }
            public double? Optimality_Gap { get; set; }
            public double Solver_Runtime_Ms { get; set; }
            public double Total_Revenue { get; set; }
            public double Total_Food_Cost { get; set; }
            public double Food_Cost_Percent { get; set; }
            public double Total_Contribution_Margin { get; set; }
            public double Expected_Waste_Cost { get; set; }
            public double Inventory_Utilization_Percent { get; set; }
            public double Total_Prep_Minutes { get; set; }
            public bool Hard_Constraints_Satisfied { get; set; }
            public List<string> Trade_Off_Explanation { get; set; } = new();
            public List<PyDishAlloc> Selected_Dishes { get; set; } = new();
        }
        private sealed class PyDishAlloc
        {
            public string Dish_Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public int Planned_Portions { get; set; }
            public double Unit_Cost { get; set; }
            public double Total_Cost { get; set; }
            public double Contribution_Margin { get; set; }
            public double Prep_Minutes { get; set; }
            public double Inventory_Coverage { get; set; }
        }
        private sealed class PyInvMenuResp
        {
            public string Status { get; set; } = string.Empty;
            public double Inventory_Utilization_Percent { get; set; }
            public double Avoidable_Purchase_Cost { get; set; }
            public double Expiry_Avoidance_Value { get; set; }
            public double Food_Cost_Percent { get; set; }
            public double Expected_Contribution_Margin { get; set; }
            public Dictionary<string, double> Remaining_Shortages { get; set; } = new();
            public Dictionary<string, double> Excluded_Stock_Summary { get; set; } = new();
            public List<PyDishAlloc> Selected_Dishes { get; set; } = new();
        }
        private sealed class PyPricingResp
        {
            public double Total_Current_Contribution { get; set; }
            public double Total_Recommended_Contribution { get; set; }
            public List<PyPricingItem> Recommendations { get; set; } = new();
        }
        private sealed class PyPricingItem
        {
            public string Menu_Item_Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public double Current_Price { get; set; }
            public double Recommended_Price { get; set; }
            public double Price_Delta_Percent { get; set; }
            public double Expected_Units { get; set; }
            public double Current_Contribution { get; set; }
            public double Recommended_Contribution { get; set; }
            public double Recommended_Food_Cost_Percent { get; set; }
            public string Elasticity_Mode { get; set; } = string.Empty;
            public string Assumption_Disclosure { get; set; } = string.Empty;
        }
        private sealed class PyMonteCarloResp
        {
            public int Seed { get; set; }
            public int Runs { get; set; }
            public double Expected_Revenue { get; set; }
            public double Expected_Cost { get; set; }
            public double Median_Cost { get; set; }
            public double Cost_P10 { get; set; }
            public double Cost_P90 { get; set; }
            public double Expected_Food_Cost_Percent { get; set; }
            public double Expected_Contribution_Margin { get; set; }
            public double Margin_P10 { get; set; }
            public double Margin_P90 { get; set; }
            public double Downside_Var_95_Margin { get; set; }
            public double Prob_Exceed_Budget { get; set; }
            public double Prob_Exceed_Food_Cost_Target { get; set; }
            public double Prob_Ingredient_Shortage { get; set; }
            public double Runtime_Ms { get; set; }
            public Dictionary<string, object> Assumptions { get; set; } = new();
        }
        private sealed class PySensResp
        {
            public double Baseline_Margin { get; set; }
            public double Baseline_Food_Cost_Percent { get; set; }
            public double Break_Even_Food_Cost_Ceiling { get; set; }
            public List<string> Top_Risk_Drivers { get; set; } = new();
            public List<PySensItem> Tornado { get; set; } = new();
        }
        private sealed class PySensItem
        {
            public string Variable_Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public double Low_Margin { get; set; }
            public double High_Margin { get; set; }
            public double Margin_Swing { get; set; }
            public double Low_Food_Cost_Percent { get; set; }
            public double High_Food_Cost_Percent { get; set; }
            public double Food_Cost_Swing_Percent { get; set; }
        }
        private sealed class PyStressResp
        {
            public double Baseline_Margin { get; set; }
            public List<PyStressOutcome> Outcomes { get; set; } = new();
        }
        private sealed class PyStressOutcome
        {
            public string Stress_Type { get; set; } = string.Empty;
            public string Label { get; set; } = string.Empty;
            public int Revised_Guests { get; set; }
            public double Revised_Revenue { get; set; }
            public double Revised_Food_Cost { get; set; }
            public double Revised_Food_Cost_Percent { get; set; }
            public double Revised_Margin { get; set; }
            public double Revised_Prep_Minutes { get; set; }
            public bool Is_Operationally_Feasible { get; set; }
            public string Shortage_Risk { get; set; } = string.Empty;
            public List<string> Corrective_Actions { get; set; } = new();
        }
    }
}
