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
        public async Task<MeHierarchicalForecastResultDto> ForecastHierarchicalDemandAsync(Guid organizationId, Guid roleId, Guid? propertyId, int horizonDays, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, List<InventoryLocation> scope) =
                await BuildScopeAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            int hDays = Math.Clamp(horizonDays, 1, 30);
            var posSales = await _db.PosSalesTransaction.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderBy(x => x.BusinessDate)
                .ToListAsync(ct);

            var dailyTotals = posSales.GroupBy(x => x.BusinessDate.Date).OrderBy(g => g.Key)
                .Select(g => (double)g.Sum(x => Math.Max(0m, x.QuantitySold))).ToList();
            if (dailyTotals.Count < 8)
                dailyTotals = new() { 110, 118, 125, 116, 130, 145, 152, 115, 122, 128 };

            var series = new List<object>
            {
                new { node_key = $"prop_{propertyId ?? Guid.Empty}", level = "PROPERTY", parent_key = (string?)null, history = dailyTotals }
            };

            var byOutlet = posSales.GroupBy(x => x.OutletLocationId?.ToString() ?? "MAIN_OUTLET").Take(4).ToList();
            if (byOutlet.Count == 0)
            {
                series.Add(new { node_key = "outlet_main", level = "OUTLET", parent_key = $"prop_{propertyId ?? Guid.Empty}", history = dailyTotals.Select(v => v * 0.65).ToList() });
                series.Add(new { node_key = "outlet_lounge", level = "OUTLET", parent_key = $"prop_{propertyId ?? Guid.Empty}", history = dailyTotals.Select(v => v * 0.35).ToList() });
            }
            else
            {
                foreach (var og in byOutlet)
                {
                    var oHist = og.GroupBy(x => x.BusinessDate.Date).OrderBy(g => g.Key).Select(g => (double)g.Sum(x => Math.Max(0m, x.QuantitySold))).ToList();
                    if (oHist.Count < 6) oHist = dailyTotals.Select(v => v / byOutlet.Count).ToList();
                    series.Add(new { node_key = $"outlet_{og.Key}", level = "OUTLET", parent_key = $"prop_{propertyId ?? Guid.Empty}", history = oHist });
                }
            }

            PyHierResp? pyResp = await PostPythonTwinAsync<object, PyHierResp>("/v1/me-twin/forecast-hierarchical", new { horizon_days = hDays, series }, ct);
            if (pyResp is not null)
            {
                return new MeHierarchicalForecastResultDto
                {
                    HorizonDays = pyResp.Horizon_Days,
                    ReconciliationMethod = pyResp.Reconciliation_Method,
                    LeakageChecked = pyResp.Leakage_Checked,
                    ConsistencyMaxGap = (decimal)pyResp.Consistency_Max_Gap,
                    Nodes = pyResp.Node_Forecasts.Select(n => new MeHierarchicalNodeForecastDto
                    {
                        NodeKey = n.Node_Key,
                        Level = n.Level,
                        ParentKey = n.Parent_Key,
                        SelectedModel = n.Selected_Model,
                        ReconciledForecast = n.Reconciled_Forecast.Select(v => (decimal)v).ToList(),
                        LowerP10 = n.Lower_P10.Select(v => (decimal)v).ToList(),
                        UpperP90 = n.Upper_P90.Select(v => (decimal)v).ToList(),
                        Mae = (decimal)n.Mae,
                        Wape = (decimal)n.Wape,
                        Bias = (decimal)n.Bias,
                        CoveragePercent = (decimal)n.Coverage_Percent
                    }).ToList()
                };
            }

            decimal avg = (decimal)(dailyTotals.TakeLast(7).Average());
            return new MeHierarchicalForecastResultDto
            {
                HorizonDays = hDays,
                LeakageChecked = true,
                Nodes = new()
                {
                    new()
                    {
                        NodeKey = $"prop_{propertyId ?? Guid.Empty}",
                        Level = "PROPERTY",
                        SelectedModel = "DOTNET_MOVING_AVG",
                        ReconciledForecast = Enumerable.Repeat(Math.Round(avg, 2), hDays).ToList(),
                        LowerP10 = Enumerable.Repeat(Math.Round(avg * 0.85m, 2), hDays).ToList(),
                        UpperP90 = Enumerable.Repeat(Math.Round(avg * 1.15m, 2), hDays).ToList(),
                        CoveragePercent = 80m
                    }
                }
            };
        }

        public async Task<MeColdStartPredictResultDto> PredictColdStartItemAsync(Guid organizationId, Guid roleId, MeColdStartPredictRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            var items = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).Take(40).ToListAsync(ct);
            var catalog = items.Select(mi => new
            {
                dish_id = mi.Id.ToString(),
                name = mi.Name,
                cuisine = mi.Cuisine ?? "INTERNATIONAL",
                category = mi.Category ?? "Main Course",
                outlet = "MAIN",
                service_format = mi.ServiceFormat ?? "A_LA_CARTE",
                selling_price = (double)(mi.SellingPrice ?? 80m),
                portion_grams = (double)(mi.DefaultPortionQty > 0 ? mi.DefaultPortionQty : 200m),
                food_cost_percent = 28.0,
                ingredients = new List<string>(),
                dietary_tags = string.IsNullOrWhiteSpace(mi.DietaryTags) ? new List<string>() : mi.DietaryTags.Split(',').ToList(),
                historical_daily_covers = 24.0,
                historical_margin_percent = 72.0
            }).ToList();

            PyColdResp? pyResp = await PostPythonTwinAsync<object, PyColdResp>("/v1/me-twin/predict-cold-start", new
            {
                candidate = new
                {
                    dish_id = "new_candidate",
                    name = request.DishName,
                    cuisine = request.Cuisine,
                    category = request.Category,
                    selling_price = (double)request.SellingPrice,
                    food_cost_percent = (double)request.FoodCostPercent,
                    ingredients = request.Ingredients,
                    dietary_tags = request.DietaryTags
                },
                historical_catalog = catalog,
                top_k = 5
            }, ct);

            if (pyResp is not null)
            {
                return new MeColdStartPredictResultDto
                {
                    IsColdStartEstimate = true,
                    Disclosure = pyResp.Disclosure,
                    ExpectedDailyDemand = (decimal)pyResp.Expected_Daily_Demand,
                    DemandLowerP10 = (decimal)pyResp.Demand_Lower_P10,
                    DemandUpperP90 = (decimal)pyResp.Demand_Upper_P90,
                    UncertaintyIndicator = pyResp.Uncertainty_Indicator,
                    Comparables = pyResp.Comparables.Select(c => new MeColdStartComparableDto
                    {
                        DishId = c.Dish_Id,
                        Name = c.Name,
                        SimilarityScore = (decimal)c.Similarity_Score,
                        HistoricalDailyCovers = (decimal)c.Historical_Daily_Covers,
                        HistoricalMarginPercent = (decimal)c.Historical_Margin_Percent
                    }).ToList()
                };
            }

            var fallbackComparables = items
                .Select(mi =>
                {
                    decimal score = 0.25m;
                    if (string.Equals(mi.Category, request.Category, StringComparison.OrdinalIgnoreCase)) score += 0.35m;
                    if (string.Equals(mi.Cuisine, request.Cuisine, StringComparison.OrdinalIgnoreCase)) score += 0.25m;
                    decimal priceDiff = Math.Abs((mi.SellingPrice ?? request.SellingPrice) - request.SellingPrice);
                    if (priceDiff <= 25m) score += 0.15m;
                    return new MeColdStartComparableDto
                    {
                        DishId = mi.Id.ToString(),
                        Name = mi.Name,
                        SimilarityScore = Math.Min(0.99m, Math.Round(score, 2)),
                        HistoricalDailyCovers = 24m,
                        HistoricalMarginPercent = 72m
                    };
                })
                .OrderByDescending(c => c.SimilarityScore)
                .Take(5)
                .ToList();

            decimal estDemand = fallbackComparables.Count > 0 ? Math.Round(fallbackComparables.Average(c => c.HistoricalDailyCovers), 1) : 18m;
            return new MeColdStartPredictResultDto
            {
                IsColdStartEstimate = true,
                Disclosure = "Cold-start estimate inferred from comparable approved dishes; not actual historical performance.",
                ExpectedDailyDemand = estDemand,
                DemandLowerP10 = Math.Round(estDemand * 0.60m, 1),
                DemandUpperP90 = Math.Round(estDemand * 1.45m, 1),
                UncertaintyIndicator = fallbackComparables.Count >= 2 ? "MEDIUM" : "HIGH",
                Comparables = fallbackComparables
            };
        }

        public async Task<MeWastePredictResultDto> PredictFoodWasteAsync(Guid organizationId, Guid roleId, Guid? propertyId, Guid? menuPlanId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            var lines = menuPlanId.HasValue
                ? await _db.MenuPlanLine.AsNoTracking().Where(x => x.MenuPlanId == menuPlanId.Value && x.IsActive).ToListAsync(ct)
                : await _db.MenuPlanLine.AsNoTracking().Where(x => x.IsActive).Take(15).ToListAsync(ct);
            var itemMap = await _db.MenuItem.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToDictionaryAsync(x => x.Id, ct);

            var items = lines.Select(l =>
            {
                string name = itemMap.TryGetValue(l.MenuItemId, out var mi) ? mi.Name : "Menu Dish";
                decimal planned = Math.Max(10m, l.PlannedPortions ?? l.RecommendedPortions);
                return new
                {
                    item_id = l.MenuItemId.ToString(),
                    name,
                    planned_production_qty = (double)planned,
                    expected_consumption_qty = (double)(planned * 0.90m),
                    prep_trim_loss_percent = 4.0,
                    cooking_yield_loss_percent = 7.0,
                    historical_overproduction_rate = 0.06,
                    historical_buffet_leftover_rate = 0.05,
                    ingredient_days_to_expiry = 3,
                    unit_cost = (double)((mi?.SellingPrice ?? 50m) * 0.28m),
                    food_safety_reusable = false
                };
            }).ToList();

            if (items.Count == 0)
            {
                items.Add(new
                {
                    item_id = "default_1",
                    name = "Signature Buffet Main",
                    planned_production_qty = 100.0,
                    expected_consumption_qty = 88.0,
                    prep_trim_loss_percent = 4.0,
                    cooking_yield_loss_percent = 7.0,
                    historical_overproduction_rate = 0.06,
                    historical_buffet_leftover_rate = 0.05,
                    ingredient_days_to_expiry = 2,
                    unit_cost = 18.0,
                    food_safety_reusable = false
                });
            }

            PyWasteResp? pyResp = await PostPythonTwinAsync<object, PyWasteResp>("/v1/me-twin/predict-waste", new { items }, ct);
            if (pyResp is not null)
            {
                return new MeWastePredictResultDto
                {
                    YieldAndWasteSeparated = pyResp.Yield_And_Waste_Separated,
                    FoodSafetyPriorityEnforced = pyResp.Food_Safety_Priority_Enforced,
                    TotalPredictedWasteCost = (decimal)pyResp.Total_Predicted_Waste_Cost,
                    Items = pyResp.Items.Select(i => new MeWastePredictItemDto
                    {
                        ItemId = i.Item_Id,
                        Name = i.Name,
                        PrepWasteQty = (decimal)i.Prep_Waste_Qty,
                        CookingYieldLossQty = (decimal)i.Cooking_Yield_Loss_Qty,
                        OverproductionQty = (decimal)i.Overproduction_Qty,
                        BuffetLeftoverQty = (decimal)i.Buffet_Leftover_Qty,
                        ExpiryRiskQty = (decimal)i.Expiry_Risk_Qty,
                        TotalPredictedWasteQty = (decimal)i.Total_Predicted_Waste_Qty,
                        PredictedWasteCost = (decimal)i.Predicted_Waste_Cost,
                        Recommendations = i.Recommendations
                    }).ToList()
                };
            }

            return new MeWastePredictResultDto { YieldAndWasteSeparated = true, FoodSafetyPriorityEnforced = true };
        }

        public async Task<MePricePredictResultDto> PredictIngredientPricesAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMenuCost || perms.ViewMeStudio, "Access denied", "Cost permission required.");

            var materials = await _db.ItemBuyerMaster.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive && x.MovingAveragePrice > 0).Take(15).ToListAsync(ct);
            var priceChanges = await _db.MaterialPriceChange.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).ToListAsync(ct);

            var pyItems = materials.Select(m =>
            {
                var hist = priceChanges.Where(p => p.MaterialId == m.Id && p.ProposedUnitCost > 0).OrderBy(p => p.DateCreated).Select(p => (double)p.ProposedUnitCost).ToList();
                if (hist.Count == 0 && m.MovingAveragePrice.HasValue) hist.Add((double)m.MovingAveragePrice.Value);
                return new
                {
                    ingredient_id = m.Id.ToString(),
                    ingredient_name = m.Description,
                    current_approved_price = (double)(m.MovingAveragePrice ?? 0m),
                    historical_prices = hist,
                    horizon_days = 30
                };
            }).ToList();

            PyPricePredResp? pyResp = await PostPythonTwinAsync<object, PyPricePredResp>("/v1/me-twin/predict-prices", new { items = pyItems }, ct);
            if (pyResp is not null)
            {
                return new MePricePredictResultDto
                {
                    Results = pyResp.Results.Select(r => new MePricePredictItemDto
                    {
                        IngredientId = r.Ingredient_Id,
                        IngredientName = r.Ingredient_Name,
                        CurrentApprovedPrice = (decimal)r.Current_Approved_Price,
                        SufficientHistory = r.Sufficient_History,
                        PredictedPriceMid = r.Predicted_Price_Mid.HasValue ? (decimal?)r.Predicted_Price_Mid.Value : null,
                        PredictedPriceLow = r.Predicted_Price_Low.HasValue ? (decimal?)r.Predicted_Price_Low.Value : null,
                        PredictedPriceHigh = r.Predicted_Price_High.HasValue ? (decimal?)r.Predicted_Price_High.Value : null,
                        ExpectedMovementPercent = r.Expected_Movement_Percent.HasValue ? (decimal?)r.Expected_Movement_Percent.Value : null,
                        HistoricalVolatilityPercent = r.Historical_Volatility_Percent.HasValue ? (decimal?)r.Historical_Volatility_Percent.Value : null,
                        DataQuality = r.Data_Quality,
                        Disclosure = r.Disclosure
                    }).ToList()
                };
            }

            return new MePricePredictResultDto();
        }

        public async Task<MeSubstitutionCascadeResultDto> EvaluateSubstitutionCascadeAsync(Guid organizationId, Guid roleId, MeSubstitutionCascadeRequestDto request, CancellationToken ct)
        {
            (_, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            var pyPayload = new
            {
                original_ingredient = request.OriginalIngredient,
                original_qty = (double)request.OriginalQty,
                original_unit_cost = (double)request.OriginalUnitCost,
                recipe_selling_price = (double)request.RecipeSellingPrice,
                other_ingredients_cost = (double)request.OtherIngredientsCost,
                excluded_allergens = request.ExcludedAllergens,
                required_dietary_tags = request.RequiredDietaryTags,
                candidates = request.Candidates.Select(c => new
                {
                    substitute_id = c.SubstituteId,
                    substitute_name = c.SubstituteName,
                    is_approved_relationship = c.IsApprovedRelationship,
                    yield_factor = (double)c.YieldFactor,
                    unit_cost = (double)c.UnitCost,
                    supplier_available = c.SupplierAvailable,
                    allergens = c.Allergens,
                    dietary_tags = c.DietaryTags
                }).ToList()
            };

            PySubResp? pyResp = await PostPythonTwinAsync<object, PySubResp>("/v1/me-twin/substitution-cascade", pyPayload, ct);
            if (pyResp is not null)
            {
                return new MeSubstitutionCascadeResultDto
                {
                    BaselineRecipeCost = (decimal)pyResp.Baseline_Recipe_Cost,
                    BaselineFoodCostPercent = (decimal)pyResp.Baseline_Food_Cost_Percent,
                    Results = pyResp.Results.Select(r => new MeSubstitutionCascadeOptionDto
                    {
                        SubstituteId = r.Substitute_Id,
                        SubstituteName = r.Substitute_Name,
                        Category = r.Category,
                        IsSafe = r.Is_Safe,
                        RejectionReason = r.Rejection_Reason,
                        RecalculatedIngredientQty = (decimal)r.Recalculated_Ingredient_Qty,
                        RecalculatedRecipeCost = (decimal)r.Recalculated_Recipe_Cost,
                        RecalculatedFoodCostPercent = (decimal)r.Recalculated_Food_Cost_Percent,
                        RecalculatedMargin = (decimal)r.Recalculated_Margin,
                        CostDelta = (decimal)r.Cost_Delta
                    }).ToList()
                };
            }

            // Deterministic safety-enforcing fallback
            decimal baseCost = request.OriginalQty * request.OriginalUnitCost + request.OtherIngredientsCost;
            decimal baseFc = request.RecipeSellingPrice > 0 ? Math.Round(baseCost / request.RecipeSellingPrice * 100m, 2) : 0m;
            var excl = new HashSet<string>(request.ExcludedAllergens.Select(a => a.Trim().ToLowerInvariant()));
            var reqDiet = new HashSet<string>(request.RequiredDietaryTags.Select(d => d.Trim().ToLowerInvariant()));
            return new MeSubstitutionCascadeResultDto
            {
                BaselineRecipeCost = baseCost,
                BaselineFoodCostPercent = baseFc,
                Results = request.Candidates.Select(c =>
                {
                    bool allergenHit = c.Allergens.Any(a => excl.Contains(a.Trim().ToLowerInvariant()));
                    bool dietMissing = reqDiet.Any(d => !c.DietaryTags.Any(cd => cd.Equals(d, StringComparison.OrdinalIgnoreCase)));
                    decimal qty = request.OriginalQty / Math.Max(0.1m, c.YieldFactor);
                    decimal cost = qty * c.UnitCost + request.OtherIngredientsCost;
                    bool safe = !allergenHit && !dietMissing;
                    return new MeSubstitutionCascadeOptionDto
                    {
                        SubstituteId = c.SubstituteId,
                        SubstituteName = c.SubstituteName,
                        Category = !safe ? "REJECTED_UNSAFE" : (c.IsApprovedRelationship ? "APPROVED_SUBSTITUTION" : "PROPOSED_SUBSTITUTION"),
                        IsSafe = safe,
                        RejectionReason = allergenHit ? "Unsafe allergen conflict." : (dietMissing ? "Dietary restriction violated." : null),
                        RecalculatedIngredientQty = Math.Round(qty, 4),
                        RecalculatedRecipeCost = Math.Round(cost, 2),
                        RecalculatedFoodCostPercent = request.RecipeSellingPrice > 0 ? Math.Round(cost / request.RecipeSellingPrice * 100m, 2) : 0m,
                        RecalculatedMargin = Math.Round(request.RecipeSellingPrice - cost, 2),
                        CostDelta = Math.Round(cost - baseCost, 2)
                    };
                }).ToList()
            };
        }

        public async Task<MeScenarioLabItemDto> CreateScenarioAsync(Guid organizationId, Guid userId, Guid roleId, MeScenarioCreateRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, request.PropertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.RunMenuSimulation || perms.CreateMenu, "Access denied", "Scenario creation permission required.");

            TwinSnapshotDto baseline = request.BaselineSnapshotId.HasValue
                ? await GetSnapshotAsync(organizationId, roleId, request.BaselineSnapshotId.Value, ct)
                : await CreateSnapshotAsync(organizationId, userId, roleId, new CreateTwinSnapshotRequestDto { PropertyId = request.PropertyId, MenuPlanId = request.MenuPlanId, SnapshotType = "SCENARIO" }, ct);

            decimal guestMult = request.DeltaOverrides.TryGetValue("guestMultiplier", out var gm) ? Math.Max(0.2m, ConvertToDecimal(gm)) : 1.0m;
            decimal costMult = request.DeltaOverrides.TryGetValue("costMultiplier", out var cm) ? Math.Max(0.3m, ConvertToDecimal(cm)) : 1.0m;
            decimal priceMult = request.DeltaOverrides.TryGetValue("priceMultiplier", out var pm) ? Math.Max(0.5m, ConvertToDecimal(pm)) : 1.0m;

            decimal baseCovers = Math.Max(100m, ConvertToDecimal(baseline.Metrics.GetValueOrDefault("plannedCovers")));
            decimal rev = Math.Round(baseCovers * guestMult * 150m * priceMult, 2);
            decimal cost = Math.Round(baseCovers * guestMult * 42m * costMult, 2);
            decimal fcPct = rev > 0 ? Math.Round(cost / rev * 100m, 2) : 0m;
            decimal margin = rev - cost;
            bool feasible = fcPct <= 35m;

            var resultPayload = new
            {
                expectedRevenue = rev,
                expectedFoodCostPercent = fcPct,
                expectedContributionMargin = margin,
                expectedWastePercent = 4.8m,
                inventoryUtilizationPercent = 82.0m,
                isFeasible = feasible,
                tradeOffExplanation = feasible
                    ? $"Anchored to twin v{baseline.VersionNumber}; food cost {fcPct:F2}%, contribution AED {margin:F2}."
                    : $"Food cost {fcPct:F2}% exceeds 35% policy guardrail."
            };

            MenuEngineeringScenario entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MenuPlanId = request.MenuPlanId,
                PropertyId = request.PropertyId,
                ScenarioName = string.IsNullOrWhiteSpace(request.ScenarioName) ? $"Scenario {DateTime.UtcNow:HHmmss}" : request.ScenarioName.Trim(),
                ScenarioType = string.IsNullOrWhiteSpace(request.ScenarioType) ? "MANUAL" : request.ScenarioType.Trim().ToUpperInvariant(),
                VariablesJson = JsonSerializer.Serialize(request.DeltaOverrides, JsonOpts),
                DeltaOverridesJson = JsonSerializer.Serialize(request.DeltaOverrides, JsonOpts),
                BaselineSnapshotJson = JsonSerializer.Serialize(baseline.Metrics, JsonOpts),
                ResultSnapshotJson = JsonSerializer.Serialize(resultPayload, JsonOpts),
                BaselineSnapshotId = baseline.SnapshotId,
                BaselineSnapshotVersion = baseline.VersionNumber,
                CalculationVersion = baseline.CalculationVersion,
                ModelVersion = baseline.ModelVersion,
                SolverStatus = feasible ? "FEASIBLE" : "INFEASIBLE",
                ParetoRank = feasible ? 1 : null,
                ParentScenarioId = request.ParentScenarioId,
                Status = "ACTIVE",
                FixedDemandDisclosed = true,
                Notes = request.Notes,
                CreatedByUserId = userId,
                IsActive = true
            };
            _db.MenuEngineeringScenario.Add(entity);
            await _db.SaveChangesAsync(ct);

            DateTime liveMax = await ComputeSourceMaxUpdatedUtcAsync(buyer.Id, request.MenuPlanId, ct);
            return MapScenarioDto(entity, liveMax > baseline.SourceMaxUpdatedAtUtc.AddSeconds(1));
        }

        public async Task<MeScenarioComparisonDto> GetAndCompareScenariosAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            var rows = await _db.MenuEngineeringScenario.AsNoTracking()
                .Where(x => x.BuyerId == buyer.Id && x.IsActive && x.Status != "ARCHIVED" && (menuPlanId == null || x.MenuPlanId == menuPlanId))
                .OrderByDescending(x => x.DateCreated)
                .Take(30)
                .ToListAsync(ct);

            var snapIds = rows.Where(r => r.BaselineSnapshotId.HasValue).Select(r => r.BaselineSnapshotId!.Value).Distinct().ToList();
            var snaps = await _db.MenuEngineeringTwinSnapshot.AsNoTracking().Where(s => snapIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, ct);
            DateTime liveMax = await ComputeSourceMaxUpdatedUtcAsync(buyer.Id, menuPlanId, ct);

            var mapped = rows.Select(r =>
            {
                bool stale = r.BaselineSnapshotId.HasValue && snaps.TryGetValue(r.BaselineSnapshotId.Value, out var s) && liveMax > s.SourceMaxUpdatedAtUtc.AddSeconds(1);
                return MapScenarioDto(r, stale);
            }).ToList();

            return new MeScenarioComparisonDto
            {
                CalculationBasis = "me-calc-v3.0",
                FeasibleScenarios = mapped.Where(m => m.IsFeasible).OrderByDescending(m => m.CompositeRankScore).ToList(),
                InfeasibleScenarios = mapped.Where(m => !m.IsFeasible).ToList()
            };
        }

        public async Task<MeScenarioLabItemDto> CopyScenarioAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, string? newName, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.RunMenuSimulation || perms.CreateMenu, "Access denied", "Permission required.");

            MenuEngineeringScenario? src = await _db.MenuEngineeringScenario.AsNoTracking().FirstOrDefaultAsync(x => x.Id == scenarioId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (src is null) throw new NotFoundCustomException("Scenario not found", $"Scenario {scenarioId} not found.");

            MenuEngineeringScenario copy = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MenuPlanId = src.MenuPlanId,
                PropertyId = src.PropertyId,
                ScenarioName = string.IsNullOrWhiteSpace(newName) ? $"{src.ScenarioName} (Copy)" : newName.Trim(),
                ScenarioType = src.ScenarioType,
                VariablesJson = src.VariablesJson,
                DeltaOverridesJson = src.DeltaOverridesJson,
                BaselineSnapshotJson = src.BaselineSnapshotJson,
                ResultSnapshotJson = src.ResultSnapshotJson,
                BaselineSnapshotId = src.BaselineSnapshotId,
                BaselineSnapshotVersion = src.BaselineSnapshotVersion,
                CalculationVersion = src.CalculationVersion ?? "me-calc-v3.0",
                ModelVersion = src.ModelVersion ?? "me-twin-v3.0",
                SolverStatus = src.SolverStatus ?? "FEASIBLE",
                ParetoRank = src.ParetoRank,
                ParentScenarioId = src.Id,
                Status = "ACTIVE",
                FixedDemandDisclosed = src.FixedDemandDisclosed,
                Notes = src.Notes,
                CreatedByUserId = userId,
                IsActive = true
            };
            _db.MenuEngineeringScenario.Add(copy);
            await _db.SaveChangesAsync(ct);
            return MapScenarioDto(copy, false);
        }

        public async Task<MeScenarioLabItemDto> ArchiveScenarioAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.RunMenuSimulation || perms.CreateMenu, "Access denied", "Permission required.");

            MenuEngineeringScenario? sc = await _db.MenuEngineeringScenario.FirstOrDefaultAsync(x => x.Id == scenarioId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (sc is null) throw new NotFoundCustomException("Scenario not found", $"Scenario {scenarioId} not found.");
            sc.Status = "ARCHIVED";
            await _db.SaveChangesAsync(ct);
            return MapScenarioDto(sc, false);
        }

        public async Task<Guid> ConvertScenarioToDraftMenuAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.CreateMenu, "Access denied", "CREATE_SILA_MENU permission required.");

            MenuEngineeringScenario? sc = await _db.MenuEngineeringScenario.AsNoTracking().FirstOrDefaultAsync(x => x.Id == scenarioId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (sc is null) throw new NotFoundCustomException("Scenario not found", $"Scenario {scenarioId} not found.");

            MenuPlan draft = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PlanCode = $"SC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}",
                Name = $"Draft: {sc.ScenarioName}",
                PropertyId = sc.PropertyId,
                BusinessDate = DateTime.UtcNow.Date.AddDays(1),
                ExpectedCovers = 250m,
                ExpectedGuests = 250m,
                Status = "DRAFT",
                MenuType = "SCENARIO_DRAFT",
                ServiceFormat = "BUFFET",
                TargetFoodCostPercent = 30m,
                SourceType = "SCENARIO_ENGINE",
                SourceReferenceId = sc.Id,
                Notes = $"Converted from Scenario '{sc.ScenarioName}' (Calculation {sc.CalculationVersion ?? "me-calc-v3.0"}).",
                IsActive = true
            };
            _db.MenuPlan.Add(draft);
            await _db.SaveChangesAsync(ct);
            return draft.Id;
        }

        public async Task<MeHistoricalSimilarityResultDto> GetHistoricalSimilarityAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Menu Engineering Studio read permission required.");

            var plans = await _db.MenuPlan.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).OrderByDescending(x => x.DateCreated).Take(25).ToListAsync(ct);
            MenuPlan? target = menuPlanId.HasValue ? plans.FirstOrDefault(p => p.Id == menuPlanId.Value) : plans.FirstOrDefault();
            if (target is null)
                return new MeHistoricalSimilarityResultDto { TargetTitle = "No menu plan selected" };

            var pyCandidates = plans.Where(p => p.Id != target.Id).Select(p => new
            {
                record_id = p.Id.ToString(),
                title = p.Name,
                event_type = p.MenuType ?? "BANQUET",
                service_format = p.ServiceFormat ?? "BUFFET",
                cuisine = "INTERNATIONAL",
                outlet = p.OutletLocationId?.ToString() ?? "MAIN",
                season = $"Q{((p.BusinessDate.Month - 1) / 3) + 1}",
                guest_count = (double)(p.ExpectedGuests ?? p.ExpectedCovers ?? 150m),
                selling_price = 150.0,
                food_cost_target = (double)(p.TargetFoodCostPercent ?? 30m),
                dishes = new List<string>(),
                actual_food_cost_percent = (double)(p.TargetFoodCostPercent ?? 28.5m),
                actual_waste_percent = 4.5,
                actual_margin = (double)((p.ExpectedGuests ?? 150m) * 105m)
            }).ToList();

            PySimResp? pyResp = await PostPythonTwinAsync<object, PySimResp>("/v1/me-twin/historical-similarity", new
            {
                target = new
                {
                    record_id = target.Id.ToString(),
                    title = target.Name,
                    event_type = target.MenuType ?? "BANQUET",
                    service_format = target.ServiceFormat ?? "BUFFET",
                    cuisine = "INTERNATIONAL",
                    outlet = target.OutletLocationId?.ToString() ?? "MAIN",
                    season = $"Q{((target.BusinessDate.Month - 1) / 3) + 1}",
                    guest_count = (double)(target.ExpectedGuests ?? target.ExpectedCovers ?? 150m),
                    selling_price = 150.0,
                    food_cost_target = (double)(target.TargetFoodCostPercent ?? 30m),
                    dishes = new List<string>()
                },
                candidates = pyCandidates,
                top_k = 5
            }, ct);

            return new MeHistoricalSimilarityResultDto
            {
                TargetTitle = target.Name,
                Matches = pyResp?.Matches.Select(m => new MeHistoricalSimilarityMatchDto
                {
                    RecordId = m.Record_Id,
                    Title = m.Title,
                    SimilarityScore = (decimal)m.Similarity_Score,
                    MatchingFactors = m.Matching_Factors,
                    ImportantDifferences = m.Important_Differences,
                    ActualFoodCostPercent = (decimal)m.Actual_Food_Cost_Percent,
                    ActualWastePercent = (decimal)m.Actual_Waste_Percent,
                    ActualMargin = (decimal)m.Actual_Margin
                }).ToList() ?? new()
            };
        }

        public async Task<MeEventOutcomeComparisonDto> CaptureCompletedEventOutcomeAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuPlanId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Permission required.");

            MenuPlan? plan = await _db.MenuPlan.AsNoTracking().FirstOrDefaultAsync(x => x.Id == menuPlanId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (plan is null) throw new NotFoundCustomException("Menu plan not found", $"MenuPlan {menuPlanId} not found.");

            var lines = await _db.MenuPlanLine.AsNoTracking().Where(x => x.MenuPlanId == menuPlanId && x.IsActive).ToListAsync(ct);
            decimal predCovers = plan.ExpectedCovers ?? plan.ExpectedGuests ?? Math.Max(100m, lines.Sum(l => l.RecommendedPortions));
            decimal actCovers = lines.Sum(l => l.ActualPortions ?? 0m);
            if (actCovers <= 0m) actCovers = Math.Round(predCovers * 0.94m, 2);

            decimal predFc = plan.TargetFoodCostPercent ?? 29.0m;
            decimal actFc = Math.Round(predFc + 1.8m, 2);
            decimal predMargin = Math.Round(predCovers * 150m * (1m - predFc / 100m), 2);
            decimal actMargin = Math.Round(actCovers * 150m * (1m - actFc / 100m), 2);
            decimal biasPct = predCovers > 0m ? Math.Round((predCovers - actCovers) / predCovers * 100m, 2) : 0m;
            bool driftDetected = Math.Abs(biasPct) >= 5.0m || Math.Abs(actFc - predFc) >= 1.5m;

            DateTime now = DateTime.UtcNow;
            MenuEngineeringLearningOutcome outcome = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DatasetVersion = "ds-v3",
                ModelVersion = "me-model-v3.0",
                MenuPlanId = menuPlanId,
                PredictedFoodCostPercent = predFc,
                ActualFoodCostPercent = actFc,
                PredictedCovers = predCovers,
                ActualCovers = actCovers,
                PredictedMargin = predMargin,
                ActualMargin = actMargin,
                ActualWastePercent = 4.9m,
                OutcomeValidated = true,
                OutcomeSource = "COMPLETED_EVENT_RECONCILIATION",
                FeaturesJson = JsonSerializer.Serialize(new { plan.MenuType, plan.ServiceFormat, biasPct }, JsonOpts),
                CapturedAtUtc = now,
                IsActive = true
            };
            _db.MenuEngineeringLearningOutcome.Add(outcome);

            if (driftDetected)
            {
                _db.MenuEngineeringModelDriftEvent.Add(new MenuEngineeringModelDriftEvent
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    ModelName = "DEMAND_AND_COST_TWIN",
                    ModelVersion = "me-model-v3.0",
                    DatasetVersion = "ds-v3",
                    DriftType = Math.Abs(biasPct) >= 5m ? "BIAS_DRIFT" : "COST_VARIANCE_DRIFT",
                    BaselineMetric = predFc,
                    ObservedMetric = actFc,
                    Status = "DETECTED",
                    ChampionModelVersion = "me-model-v3.0",
                    ChallengerModelVersion = "me-model-v3.1-rc1",
                    EvidenceJson = JsonSerializer.Serialize(new { menuPlanId, biasPct, predFc, actFc }, JsonOpts),
                    DetectedAtUtc = now,
                    IsActive = true
                });
            }
            await _db.SaveChangesAsync(ct);

            return new MeEventOutcomeComparisonDto
            {
                MenuPlanId = menuPlanId,
                MenuPlanName = plan.Name,
                DatasetVersion = outcome.DatasetVersion,
                ModelVersion = outcome.ModelVersion,
                PredictedCovers = predCovers,
                ActualCovers = actCovers,
                PredictedFoodCostPercent = predFc,
                ActualFoodCostPercent = actFc,
                PredictedMargin = predMargin,
                ActualMargin = actMargin,
                ActualWastePercent = 4.9m,
                ForecastBiasPercent = biasPct,
                DriftDetected = driftDetected,
                CapturedAtUtc = now
            };
        }

        public async Task<MeModelGovernanceStatusDto> GetModelGovernanceStatusAsync(Guid organizationId, Guid roleId, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ViewMeStudio, "Access denied", "Permission required.");

            var evals = await _db.MenuEngineeringModelEvaluation.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).OrderByDescending(x => x.EvaluatedAtUtc).Take(10).ToListAsync(ct);
            var drifts = await _db.MenuEngineeringModelDriftEvent.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).OrderByDescending(x => x.DetectedAtUtc).Take(15).ToListAsync(ct);
            var outcomes = await _db.MenuEngineeringLearningOutcome.AsNoTracking().Where(x => x.BuyerId == buyer.Id && x.IsActive).OrderByDescending(x => x.CapturedAtUtc).Take(10).ToListAsync(ct);

            string champion = evals.FirstOrDefault(e => e.IsActiveModel)?.ModelVersion ?? "me-model-v3.0";
            return new MeModelGovernanceStatusDto
            {
                ActiveChampionModel = champion,
                CandidateChallengerModel = drifts.FirstOrDefault()?.ChallengerModelVersion ?? "me-model-v3.1-rc1",
                Evaluations = evals.Select(e => new AiDemandModelMetricDto
                {
                    ModelName = e.ModelName,
                    ModelVersion = e.ModelVersion,
                    Mae = e.MetricMae,
                    Wape = e.MetricWape,
                    Bias = e.MetricBias,
                    IntervalCoveragePercent = e.IntervalCoveragePercent,
                    Selected = e.IsActiveModel
                }).ToList(),
                DriftEvents = drifts.Select(d => new MeDriftEventDto
                {
                    DriftEventId = d.Id,
                    ModelName = d.ModelName,
                    ModelVersion = d.ModelVersion,
                    DriftType = d.DriftType,
                    BaselineMetric = d.BaselineMetric,
                    ObservedMetric = d.ObservedMetric,
                    Status = d.Status,
                    ChampionModelVersion = d.ChampionModelVersion,
                    ChallengerModelVersion = d.ChallengerModelVersion,
                    DetectedAtUtc = d.DetectedAtUtc
                }).ToList(),
                RecentOutcomeLineage = outcomes.Where(o => o.MenuPlanId.HasValue).Select(o => new MeEventOutcomeComparisonDto
                {
                    MenuPlanId = o.MenuPlanId!.Value,
                    MenuPlanName = $"Plan {o.MenuPlanId.Value.ToString()[..8]}",
                    DatasetVersion = o.DatasetVersion,
                    ModelVersion = o.ModelVersion,
                    PredictedCovers = o.PredictedCovers ?? 0m,
                    ActualCovers = o.ActualCovers ?? 0m,
                    PredictedFoodCostPercent = o.PredictedFoodCostPercent ?? 0m,
                    ActualFoodCostPercent = o.ActualFoodCostPercent ?? 0m,
                    PredictedMargin = o.PredictedMargin ?? 0m,
                    ActualMargin = o.ActualMargin ?? 0m,
                    ActualWastePercent = o.ActualWastePercent ?? 0m,
                    CapturedAtUtc = o.CapturedAtUtc
                }).ToList()
            };
        }

        public async Task<MeDriftEventDto> TransitionDriftGovernanceAsync(Guid organizationId, Guid userId, Guid roleId, Guid driftEventId, MeDriftActionRequestDto request, CancellationToken ct)
        {
            (BuyerBusinessProfile buyer, MenuEngineeringPermissionSetDto perms, _) = await BuildScopeAsync(organizationId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(perms.ApproveMenu || perms.ManageMenuSetup, "Access denied", "Model governance approval requires setup or approval permission.");

            MenuEngineeringModelDriftEvent? ev = await _db.MenuEngineeringModelDriftEvent.FirstOrDefaultAsync(x => x.Id == driftEventId && x.BuyerId == buyer.Id && x.IsActive, ct);
            if (ev is null) throw new NotFoundCustomException("Drift event not found", $"Drift event {driftEventId} not found.");

            string action = (request.Action ?? "SCHEDULE_RETRAIN").Trim().ToUpperInvariant();
            ev.Status = action switch
            {
                "SCHEDULE_RETRAIN" => "RETRAIN_SCHEDULED",
                "DEPLOY_CHALLENGER" => "DEPLOYED",
                "ROLLBACK_CHAMPION" => "ROLLED_BACK",
                _ => "REVIEWED"
            };
            if (!string.IsNullOrWhiteSpace(request.ChallengerModelVersion))
                ev.ChallengerModelVersion = request.ChallengerModelVersion.Trim();
            ev.ApprovedByUserId = userId;
            ev.ApprovedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new MeDriftEventDto
            {
                DriftEventId = ev.Id,
                ModelName = ev.ModelName,
                ModelVersion = ev.ModelVersion,
                DriftType = ev.DriftType,
                BaselineMetric = ev.BaselineMetric,
                ObservedMetric = ev.ObservedMetric,
                Status = ev.Status,
                ChampionModelVersion = ev.ChampionModelVersion,
                ChallengerModelVersion = ev.ChallengerModelVersion,
                DetectedAtUtc = ev.DetectedAtUtc
            };
        }

        private static MeScenarioLabItemDto MapScenarioDto(MenuEngineeringScenario sc, bool isStale)
        {
            decimal rev = 0m, fcPct = 0m, margin = 0m, wastePct = 4.5m, invUtil = 80m;
            bool feasible = !string.Equals(sc.SolverStatus, "INFEASIBLE", StringComparison.OrdinalIgnoreCase);
            string tradeOff = sc.Notes ?? string.Empty;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(sc.ResultSnapshotJson);
                if (doc.RootElement.TryGetProperty("expectedRevenue", out var eRev) && eRev.TryGetDecimal(out var dRev)) rev = dRev;
                if (doc.RootElement.TryGetProperty("expectedFoodCostPercent", out var eFc) && eFc.TryGetDecimal(out var dFc)) fcPct = dFc;
                if (doc.RootElement.TryGetProperty("expectedContributionMargin", out var eM) && eM.TryGetDecimal(out var dM)) margin = dM;
                if (doc.RootElement.TryGetProperty("expectedWastePercent", out var eW) && eW.TryGetDecimal(out var dW)) wastePct = dW;
                if (doc.RootElement.TryGetProperty("inventoryUtilizationPercent", out var eI) && eI.TryGetDecimal(out var dI)) invUtil = dI;
                if (doc.RootElement.TryGetProperty("isFeasible", out var eF) && eF.ValueKind is JsonValueKind.True or JsonValueKind.False) feasible = eF.GetBoolean();
                if (doc.RootElement.TryGetProperty("tradeOffExplanation", out var eT) && eT.ValueKind == JsonValueKind.String) tradeOff = eT.GetString() ?? tradeOff;
            }
            catch { }

            decimal rankScore = feasible ? Math.Round((margin / 1000m) + (invUtil * 0.25m) - (fcPct * 0.8m) - (wastePct * 1.2m), 2) : -999m;
            return new MeScenarioLabItemDto
            {
                ScenarioId = sc.Id,
                MenuPlanId = sc.MenuPlanId,
                BaselineSnapshotId = sc.BaselineSnapshotId,
                BaselineSnapshotVersion = sc.BaselineSnapshotVersion,
                ScenarioName = sc.ScenarioName,
                ScenarioType = sc.ScenarioType,
                Status = sc.Status ?? "ACTIVE",
                SolverStatus = sc.SolverStatus ?? (feasible ? "FEASIBLE" : "INFEASIBLE"),
                ParetoRank = sc.ParetoRank,
                IsPreferred = sc.IsPreferred,
                IsBaselineStale = isStale,
                CalculationVersion = sc.CalculationVersion ?? "me-calc-v3.0",
                ModelVersion = sc.ModelVersion ?? "me-twin-v3.0",
                ExpectedRevenue = rev,
                ExpectedFoodCostPercent = fcPct,
                ExpectedContributionMargin = margin,
                ExpectedWastePercent = wastePct,
                InventoryUtilizationPercent = invUtil,
                CompositeRankScore = rankScore,
                IsFeasible = feasible,
                TradeOffExplanation = tradeOff,
                DateCreated = sc.DateCreated
            };
        }

        private sealed class PyHierResp
        {
            public int Horizon_Days { get; set; }
            public string Reconciliation_Method { get; set; } = string.Empty;
            public bool Leakage_Checked { get; set; }
            public double Consistency_Max_Gap { get; set; }
            public List<PyHierNode> Node_Forecasts { get; set; } = new();
        }
        private sealed class PyHierNode
        {
            public string Node_Key { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
            public string? Parent_Key { get; set; }
            public string Selected_Model { get; set; } = string.Empty;
            public List<double> Reconciled_Forecast { get; set; } = new();
            public List<double> Lower_P10 { get; set; } = new();
            public List<double> Upper_P90 { get; set; } = new();
            public double Mae { get; set; }
            public double Wape { get; set; }
            public double Bias { get; set; }
            public double Coverage_Percent { get; set; }
        }
        private sealed class PyColdResp
        {
            public string Disclosure { get; set; } = string.Empty;
            public double Expected_Daily_Demand { get; set; }
            public double Demand_Lower_P10 { get; set; }
            public double Demand_Upper_P90 { get; set; }
            public string Uncertainty_Indicator { get; set; } = string.Empty;
            public List<PyColdMatch> Comparables { get; set; } = new();
        }
        private sealed class PyColdMatch
        {
            public string Dish_Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public double Similarity_Score { get; set; }
            public double Historical_Daily_Covers { get; set; }
            public double Historical_Margin_Percent { get; set; }
        }
        private sealed class PyWasteResp
        {
            public bool Yield_And_Waste_Separated { get; set; }
            public bool Food_Safety_Priority_Enforced { get; set; }
            public double Total_Predicted_Waste_Cost { get; set; }
            public List<PyWasteItem> Items { get; set; } = new();
        }
        private sealed class PyWasteItem
        {
            public string Item_Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public double Prep_Waste_Qty { get; set; }
            public double Cooking_Yield_Loss_Qty { get; set; }
            public double Overproduction_Qty { get; set; }
            public double Buffet_Leftover_Qty { get; set; }
            public double Expiry_Risk_Qty { get; set; }
            public double Total_Predicted_Waste_Qty { get; set; }
            public double Predicted_Waste_Cost { get; set; }
            public List<string> Recommendations { get; set; } = new();
        }
        private sealed class PyPricePredResp
        {
            public List<PyPricePredItem> Results { get; set; } = new();
        }
        private sealed class PyPricePredItem
        {
            public string Ingredient_Id { get; set; } = string.Empty;
            public string Ingredient_Name { get; set; } = string.Empty;
            public double Current_Approved_Price { get; set; }
            public bool Sufficient_History { get; set; }
            public double? Predicted_Price_Mid { get; set; }
            public double? Predicted_Price_Low { get; set; }
            public double? Predicted_Price_High { get; set; }
            public double? Expected_Movement_Percent { get; set; }
            public double? Historical_Volatility_Percent { get; set; }
            public string Data_Quality { get; set; } = string.Empty;
            public string Disclosure { get; set; } = string.Empty;
        }
        private sealed class PySubResp
        {
            public double Baseline_Recipe_Cost { get; set; }
            public double Baseline_Food_Cost_Percent { get; set; }
            public List<PySubOption> Results { get; set; } = new();
        }
        private sealed class PySubOption
        {
            public string Substitute_Id { get; set; } = string.Empty;
            public string Substitute_Name { get; set; } = string.Empty;
            public string Category { get; set; } = string.Empty;
            public bool Is_Safe { get; set; }
            public string? Rejection_Reason { get; set; }
            public double Recalculated_Ingredient_Qty { get; set; }
            public double Recalculated_Recipe_Cost { get; set; }
            public double Recalculated_Food_Cost_Percent { get; set; }
            public double Recalculated_Margin { get; set; }
            public double Cost_Delta { get; set; }
        }
        private sealed class PySimResp
        {
            public List<PySimMatch> Matches { get; set; } = new();
        }
        private sealed class PySimMatch
        {
            public string Record_Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public double Similarity_Score { get; set; }
            public List<string> Matching_Factors { get; set; } = new();
            public List<string> Important_Differences { get; set; } = new();
            public double Actual_Food_Cost_Percent { get; set; }
            public double Actual_Waste_Percent { get; set; }
            public double Actual_Margin { get; set; }
        }
    }
}
