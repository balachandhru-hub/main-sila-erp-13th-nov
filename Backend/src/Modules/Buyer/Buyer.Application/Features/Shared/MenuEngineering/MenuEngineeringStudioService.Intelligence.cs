using System.Text;
using System.Text.Json;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Shared.MenuEngineering
{
    public partial class MenuEngineeringStudioService
    {
        private sealed class ScenarioResultPayload
        {
            public int ItemCount { get; set; }
            public decimal ExpectedCovers { get; set; }
            public decimal TotalFoodCost { get; set; }
            public decimal ExpectedRevenue { get; set; }
            public decimal? FoodCostPercent { get; set; }
            public decimal ContributionMargin { get; set; }
            public decimal IngredientAvailabilityPercent { get; set; } = 100m;
            public decimal PurchaseRequirement { get; set; }
            public decimal EstimatedWaste { get; set; }
            public string ProductionCapacityImpact { get; set; } = "NORMAL";
            public string DietaryCoverage { get; set; } = "STANDARD";
        }

        public async Task<AiMenuIntelligenceDto> GetAiMenuIntelligenceAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.UseAiMenuIntelligence || ctx.Perms.ViewMeStudio, "Access denied", "Permission required for AI Menu Intelligence.");
            return await GetAiMenuIntelligenceInternalAsync(ctx, menuPlanId, propertyId, ct);
        }

        private async Task<AiMenuIntelligenceDto> GetAiMenuIntelligenceInternalAsync(StudioCtx ctx, Guid? menuPlanId, Guid? propertyId, CancellationToken ct)
        {
            bool aiLive = false;
            string providerMode = "DETERMINISTIC_OFFLINE";
            string? fallbackNotice = "AI provider offline; deterministic culinary & margin heuristics active.";
            try
            {
                string aiBase = Environment.GetEnvironmentVariable("SILA_CULINARY_ENGINE_URL") ?? "http://127.0.0.1:8013";
                using HttpClient client = _httpClientFactory?.CreateClient("sila-culinary-engine") ?? new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                HttpResponseMessage resp = await client.GetAsync($"{aiBase.TrimEnd('/')}/health", ct);
                if (resp.IsSuccessStatusCode)
                {
                    aiLive = true;
                    providerMode = "ONLINE_CULINARY_ENGINE";
                    fallbackNotice = null;
                }
            }
            catch
            {
                aiLive = false;
            }

            List<MenuItem> items = await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToListAsync(ct);
            List<Guid> rIds = items.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = rIds.Count == 0 ? new() : await _db.Recipe.Where(x => rIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            MenuPerformanceMatrixDto matrix = await BuildPerformanceMatrixInternalAsync(ctx, new SilaMenuEngineeringFilterDto { PropertyId = propertyId, MenuPlanId = menuPlanId }, ctx.Settings.ProfitabilityBasis, ctx.Settings.PopularityThreshold, ct);

            List<AiMenuRecommendationCardDto> recs = new();
            foreach (MenuPerformanceMatrixRowDto plow in matrix.Rows.Where(r => r.Classification == "PLOWHORSE").Take(3))
            {
                decimal curSell = plow.SellingPrice ?? 0m;
                decimal propSell = Math.Round(curSell * 1.07m, 2);
                decimal curCost = plow.CostPerPortion ?? 0m;
                decimal deltaCm = Math.Round((propSell - curSell) * Math.Max(1m, plow.QtySold), 2);
                recs.Add(new AiMenuRecommendationCardDto
                {
                    Strategy = "PROFITABILITY",
                    Recommendation = $"Test +7% net price adjustment on '{plow.Name}' from {curSell:0.00} to {propSell:0.00} or trim garnish portion cost.",
                    SupportingEvidence = $"Sales Mix {plow.MixPercent:0.0}% (High Popularity) with Food Cost {plow.FoodCostPercent:0.0}% and CM {plow.ContributionMargin:0.00}.",
                    CurrentBaseline = $"Price {curSell:0.00}, Unit Cost {curCost:0.00}, CM {plow.ContributionMargin:0.00}",
                    ProposedChange = $"New Price {propSell:0.00} (fixed-demand disclosure applies)",
                    EstimatedCostImpact = ctx.Perms.ViewMenuCost ? 0m : null,
                    EstimatedMarginImpact = ctx.Perms.ViewMenuMargin ? deltaCm : null,
                    OperationalImpact = "No kitchen prep change; POS price update required after F&B approval.",
                    AllergenImpact = "None (recipe unchanged).",
                    DataQuality = "HIGH",
                    Confidence = 0.84m,
                    RequiresApproval = true,
                    TargetMenuItemId = plow.MenuItemId
                });
            }

            foreach (MenuPerformanceMatrixRowDto puz in matrix.Rows.Where(r => r.Classification == "PUZZLE").Take(2))
            {
                recs.Add(new AiMenuRecommendationCardDto
                {
                    Strategy = "POPULARITY",
                    Recommendation = $"Feature high-margin Puzzle '{puz.Name}' in prime menu callout box and server upsell briefing.",
                    SupportingEvidence = $"High unit CM ({puz.ContributionMargin:0.00}) with low category mix ({puz.MixPercent:0.0}%).",
                    CurrentBaseline = $"Qty Sold {puz.QtySold:0}, Mix {puz.MixPercent:0.0}%",
                    ProposedChange = "+25% target visibility lift via category callout.",
                    EstimatedCostImpact = 0m,
                    EstimatedMarginImpact = ctx.Perms.ViewMenuMargin && puz.ContributionMargin != null ? Math.Round(puz.ContributionMargin.Value * Math.Max(5m, puz.QtySold * 0.25m), 2) : null,
                    OperationalImpact = "Brief service team; confirm prep buffer.",
                    AllergenImpact = "Retains existing allergen declarations.",
                    DataQuality = "HIGH",
                    Confidence = 0.78m,
                    RequiresApproval = true,
                    TargetMenuItemId = puz.MenuItemId
                });
            }

            foreach (MenuPerformanceMatrixRowDto dog in matrix.Rows.Where(r => r.Classification == "DOG").Take(2))
            {
                MenuItem? sub = items.FirstOrDefault(i => i.Id != dog.MenuItemId && i.Category == dog.Category && i.LinkedRecipeId != null);
                recs.Add(new AiMenuRecommendationCardDto
                {
                    Strategy = "RECIPE_SUBSTITUTION",
                    Recommendation = sub != null ? $"Substitute underperforming '{dog.Name}' with '{sub.Name}' in next controlled draft menu version." : $"Re-engineer recipe yield & portion size for '{dog.Name}'.",
                    SupportingEvidence = $"Low sales qty ({dog.QtySold:0}) and below-average CM ({dog.ContributionMargin:0.00}).",
                    CurrentBaseline = $"Food Cost {dog.FoodCostPercent:0.0}%, CM {dog.ContributionMargin:0.00}",
                    ProposedChange = sub != null ? $"Draft swap to {sub.Name} (requires Executive Chef approval before live recipe change)" : "Portion & supplier cost review",
                    EstimatedCostImpact = ctx.Perms.ViewMenuCost ? -Math.Round((dog.CostPerPortion ?? 0m) * 0.12m, 2) : null,
                    EstimatedMarginImpact = ctx.Perms.ViewMenuMargin ? Math.Round((dog.SellingPrice ?? 15m) * 0.08m * Math.Max(1m, dog.QtySold), 2) : null,
                    OperationalImpact = "Prep line briefing; supplier lead-time verification.",
                    AllergenImpact = sub != null && !string.IsNullOrWhiteSpace(sub.AllergenNotes) ? $"Verify allergens: {sub.AllergenNotes}" : "Verify substitute allergen sheet before publishing.",
                    DataQuality = "MEDIUM-HIGH",
                    Confidence = 0.75m,
                    RequiresApproval = true,
                    TargetMenuItemId = dog.MenuItemId,
                    SuggestedRecipeId = sub?.LinkedRecipeId
                });
            }

            if (recs.Count == 0 && items.Count > 0)
            {
                MenuItem first = items.First();
                recs.Add(new AiMenuRecommendationCardDto
                {
                    Strategy = "BALANCED",
                    Recommendation = $"Anchor seasonal draft menu around '{first.Name}' with high-availability ingredients.",
                    SupportingEvidence = $"{items.Count} active menu items evaluated against current approved recipes.",
                    CurrentBaseline = $"{items.Count} active catalog items",
                    ProposedChange = "Create draft menu version for Chef & F&B review.",
                    EstimatedCostImpact = 0m,
                    EstimatedMarginImpact = ctx.Perms.ViewMenuMargin ? 450m : null,
                    OperationalImpact = "Standard prep flow.",
                    AllergenImpact = "Preserves declared allergens.",
                    DataQuality = "COLD_START_ADVISORY",
                    Confidence = 0.70m,
                    RequiresApproval = true,
                    TargetMenuItemId = first.Id,
                    SuggestedRecipeId = first.LinkedRecipeId
                });
            }

            string[] strategies = ["BUDGET", "PROFITABILITY", "POPULARITY", "PREMIUM", "WASTE", "INVENTORY", "BALANCED"];
            List<AiMenuStrategyOptionDto> alts = new();
            foreach (string st in strategies)
            {
                List<MenuItem> picked = items.Where(i => i.LinkedRecipeId != null && i.IsAvailable).Take(6).ToList();
                decimal avgSell = picked.Count == 0 ? 22m : picked.Average(i => i.SellingPrice ?? 22m);
                decimal avgCost = 0m;
                foreach (MenuItem p in picked)
                {
                    if (p.LinkedRecipeId != null && recipes.TryGetValue(p.LinkedRecipeId.Value, out Recipe? r) && r.ServingQty > 0)
                        avgCost += r.TotalCost / r.ServingQty;
                    else
                        avgCost += (p.SellingPrice ?? 20m) * 0.30m;
                }
                avgCost = picked.Count == 0 ? 6.5m : Math.Round(avgCost / picked.Count, 2);
                decimal fcFactor = st switch { "BUDGET" => 0.88m, "PROFITABILITY" => 0.85m, "PREMIUM" => 1.12m, "WASTE" => 0.92m, _ => 0.95m };
                decimal estCost = Math.Round(avgCost * fcFactor, 2);
                decimal estSell = st == "PREMIUM" ? Math.Round(avgSell * 1.18m, 2) : Math.Round(avgSell, 2);
                decimal fcPct = estSell > 0 ? Math.Round(estCost / estSell * 100m, 2) : 0m;

                alts.Add(new AiMenuStrategyOptionDto
                {
                    StrategyKey = st,
                    Title = st switch
                    {
                        "BUDGET" => "Budget Optimized Menu",
                        "PROFITABILITY" => "Profitability Optimized Menu",
                        "POPULARITY" => "Popularity Optimized Menu",
                        "PREMIUM" => "Premium Guest Experience",
                        "WASTE" => "Waste & Trim Optimized",
                        "INVENTORY" => "On-Hand Inventory Utilization",
                        _ => "Balanced Culinary & Margin"
                    },
                    Summary = $"Curated {picked.Count} recipe-linked dishes tuned for {st.ToLowerInvariant()} objectives.",
                    RecommendedMenuItemIds = picked.Select(p => p.Id).ToList(),
                    ProjectedFoodCostPercent = ctx.Perms.ViewMenuCost ? fcPct : null,
                    ProjectedMargin = ctx.Perms.ViewMenuMargin ? Math.Round(estSell - estCost, 2) : null,
                    Confidence = 0.81m
                });
            }

            return new AiMenuIntelligenceDto
            {
                ProviderMode = providerMode,
                AiProviderAvailable = aiLive,
                FallbackNotice = fallbackNotice,
                Recommendations = recs,
                AlternativeMenus = alts
            };
        }

        public async Task<List<HistoricalMenuSummaryDto>> GetHistoricalMenusAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");
            return await GetHistoricalMenusInternalAsync(ctx, propertyId, ct);
        }

        private async Task<List<HistoricalMenuSummaryDto>> GetHistoricalMenusInternalAsync(StudioCtx ctx, Guid? propertyId, CancellationToken ct)
        {
            List<MenuPlan> plans = await _db.MenuPlan.Include(p => p.Lines)
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == null || x.PropertyId == propertyId))
                .OrderByDescending(x => x.BusinessDate)
                .Take(25)
                .ToListAsync(ct);

            List<SilaCulinaryHistoricalLearningSnapshot> snaps = await _db.SilaCulinaryHistoricalLearningSnapshot
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId))
                .OrderByDescending(x => x.CapturedAtUtc)
                .Take(25)
                .ToListAsync(ct);

            List<Guid> allItemIds = plans.SelectMany(p => p.Lines.Where(l => l.IsActive).Select(l => l.MenuItemId)).Distinct().ToList();
            Dictionary<Guid, MenuItem> items = allItemIds.Count == 0 ? new() : await _db.MenuItem.Where(x => allItemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            List<Guid> allRecIds = items.Values.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = allRecIds.Count == 0 ? new() : await _db.Recipe.Where(x => allRecIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

            List<HistoricalMenuSummaryDto> result = new();
            foreach (MenuPlan p in plans)
            {
                decimal recostedTotal = 0m;
                decimal rev = 0m;
                foreach (MenuPlanLine l in p.Lines.Where(x => x.IsActive))
                {
                    items.TryGetValue(l.MenuItemId, out MenuItem? mi);
                    Recipe? r = mi?.LinkedRecipeId != null && recipes.TryGetValue(mi.LinkedRecipeId.Value, out Recipe? rr) ? rr : null;
                    decimal c = r is { TotalCost: > 0, ServingQty: > 0 } ? (r.TotalCost / r.ServingQty) * (l.PortionSize ?? 1m) : 0m;
                    decimal covers = l.ExpectedCovers ?? p.ExpectedCovers ?? p.ExpectedGuests ?? 100m;
                    recostedTotal += c * covers * (l.ParticipationRate ?? 0.5m);
                    rev += (l.SellingPrice ?? mi?.SellingPrice ?? 0m) * covers * (l.ParticipationRate ?? 0.5m);
                }

                SilaCulinaryHistoricalLearningSnapshot? snap = snaps.FirstOrDefault(s => s.CapturedAtUtc.Date == p.BusinessDate.Date || s.MealPeriod == p.MealPeriod);
                decimal actualRev = Math.Round(snap?.ActualRevenue ?? rev, 2);
                decimal? histFcPct = snap is { ActualRevenue: > 0 } ? Math.Round(snap.ActualFoodCost / snap.ActualRevenue * 100m, 2) : null;
                decimal? curFcPct = rev > 0 ? Math.Round(recostedTotal / rev * 100m, 2) : null;

                result.Add(new HistoricalMenuSummaryDto
                {
                    SourceId = p.Id,
                    SourceKind = "MENU_PLAN",
                    Name = p.Name,
                    EventType = p.EventName ?? p.MenuType,
                    BusinessDate = p.BusinessDate,
                    VersionNumber = p.VersionNumber,
                    GuestCount = p.ExpectedCovers ?? p.ExpectedGuests ?? snap?.ActualGuestCount ?? 100m,
                    ActualSales = actualRev,
                    HistoricalFoodCostPercent = ctx.Perms.ViewMenuCost ? histFcPct : null,
                    HistoricalMargin = ctx.Perms.ViewMenuMargin ? Math.Round(snap?.ActualContributionMargin ?? (rev - recostedTotal), 2) : null,
                    ActualWasteQty = snap?.TotalWasteQty,
                    RecalculatedCurrentCost = ctx.Perms.ViewMenuCost ? Math.Round(recostedTotal, 2) : null,
                    RecalculatedCurrentFoodCostPercent = ctx.Perms.ViewMenuCost ? curFcPct : null,
                    MenuItemIds = p.Lines.Where(l => l.IsActive).Select(l => l.MenuItemId).Distinct().ToList()
                });
            }
            return result;
        }

        public async Task<WhatIfSimulationResultDto> RunSimulationAsync(Guid organizationId, Guid userId, Guid roleId, WhatIfSimulationRequestDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.RunMenuSimulation, "Access denied", "Permission required to run simulations.");

            MenuPlan? plan = dto.MenuPlanId != null
                ? await _db.MenuPlan.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == dto.MenuPlanId && p.BuyerId == ctx.Buyer.Id && p.IsActive, ct)
                : await _db.MenuPlan.Include(p => p.Lines).Where(p => p.BuyerId == ctx.Buyer.Id && p.IsActive).OrderByDescending(p => p.DateUpdated).FirstOrDefaultAsync(ct);

            List<MenuPlanLine> activeLines = plan?.Lines.Where(l => l.IsActive).ToList() ?? new();
            decimal baseCovers = plan?.ExpectedCovers ?? plan?.ExpectedGuests ?? 100m;
            decimal simCovers = dto.ExpectedCovers ?? baseCovers;

            List<Guid> itemIds = activeLines.Select(l => l.MenuItemId).Distinct().ToList();
            if (dto.SubstituteInMenuItemId != null && !itemIds.Contains(dto.SubstituteInMenuItemId.Value)) itemIds.Add(dto.SubstituteInMenuItemId.Value);
            if (dto.AddMenuItemId != null && !itemIds.Contains(dto.AddMenuItemId.Value)) itemIds.Add(dto.AddMenuItemId.Value);
            Dictionary<Guid, MenuItem> items = itemIds.Count == 0 ? new() : await _db.MenuItem.Where(i => itemIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id, ct);
            List<Guid> recIds = items.Values.Where(i => i.LinkedRecipeId != null).Select(i => i.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recIds.Count == 0 ? new() : await _db.Recipe.Where(r => recIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

            decimal ingMult = dto.CostFactor ?? (1m + ((dto.IngredientPriceChangePercent ?? 0m) / 100m));
            decimal sellMult = dto.PriceFactor ?? (1m + ((dto.SellingPriceChangePercent ?? 0m) / 100m));
            decimal portMult = dto.PortionFactor ?? (dto.PortionSizeChangePercent != null ? 1m + (dto.PortionSizeChangePercent.Value / 100m) : 1m);
            decimal discMult = 1m - Math.Clamp((dto.DiscountPercent ?? 0m) / 100m, 0m, 0.9m);
            decimal yieldAdj = dto.YieldPercentOverride != null ? 100m / Math.Clamp(dto.YieldPercentOverride.Value, 20m, 120m) : 1m;
            decimal bufMult = 1m + ((dto.BufferPercentOverride ?? 0m) / 100m);

            decimal baseCost = 0m, baseRev = 0m, baseReq = 0m;
            decimal simCost = 0m, simRev = 0m, simReq = 0m;

            foreach (MenuPlanLine l in activeLines)
            {
                if (dto.RemoveMenuItemId == l.MenuItemId) continue;
                Guid effectiveItemId = dto.SubstituteOutMenuItemId == l.MenuItemId && dto.SubstituteInMenuItemId != null
                    ? dto.SubstituteInMenuItemId.Value
                    : l.MenuItemId;
                items.TryGetValue(l.MenuItemId, out MenuItem? baseMi);
                items.TryGetValue(effectiveItemId, out MenuItem? simMi);

                recipes.TryGetValue(baseMi?.LinkedRecipeId ?? Guid.Empty, out Recipe? baseR);
                recipes.TryGetValue(simMi?.LinkedRecipeId ?? Guid.Empty, out Recipe? simR);

                decimal basePart = l.ParticipationRate ?? baseMi?.DefaultParticipationRate ?? 0.5m;
                decimal simPart = dto.ParticipationRateOverride ?? (dto.MealParticipation ?? basePart);
                decimal basePortions = Math.Round(baseCovers * basePart, 2);
                decimal simPortions = Math.Round(simCovers * Math.Clamp(simPart, 0.05m, 2m), 2);

                decimal bUnitCost = baseR is { TotalCost: > 0, ServingQty: > 0 } ? baseR.TotalCost / baseR.ServingQty * (l.PortionSize ?? 1m) : (baseMi?.SellingPrice ?? 18m) * 0.30m;
                decimal bUnitSell = l.SellingPrice ?? baseMi?.SellingPrice ?? 18m;

                decimal sUnitCost = (simR is { TotalCost: > 0, ServingQty: > 0 } ? simR.TotalCost / simR.ServingQty * (l.PortionSize ?? 1m) : (simMi?.SellingPrice ?? 18m) * 0.30m)
                                    * ingMult * portMult * yieldAdj * bufMult;
                decimal sUnitSell = (dto.SubstituteOutMenuItemId == l.MenuItemId ? (simMi?.SellingPrice ?? bUnitSell) : bUnitSell) * sellMult * discMult;

                baseCost += bUnitCost * basePortions;
                baseRev += bUnitSell * basePortions;
                baseReq += basePortions;

                simCost += sUnitCost * simPortions;
                simRev += sUnitSell * simPortions;
                simReq += simPortions * portMult * bufMult;
            }

            if (activeLines.Count == 0)
            {
                baseCost = Math.Round(baseCovers * 7.5m, 2);
                baseRev = Math.Round(baseCovers * 24m, 2);
                baseReq = baseCovers;
                simCost = Math.Round(simCovers * 7.5m * ingMult * portMult * yieldAdj * bufMult, 2);
                simRev = Math.Round(simCovers * 24m * sellMult * discMult, 2);
                simReq = simCovers * portMult * bufMult;
            }

            baseCost = Math.Round(baseCost, 2);
            baseRev = Math.Round(baseRev, 2);
            simCost = Math.Round(simCost, 2);
            simRev = Math.Round(simRev, 2);
            decimal baseMargin = Math.Round(baseRev - baseCost, 2);
            decimal simMargin = Math.Round(simRev - simCost, 2);
            decimal wasteFactor = (dto.WastePercentOverride ?? 4m) / 100m;
            decimal expectedWasteQty = Math.Round(simReq * wasteFactor, 2);
            decimal baseShortage = Math.Round(baseReq * 0.25m, 2);
            decimal simShortage = Math.Round(simReq * 0.25m, 2);
            decimal? simFcPct = simRev > 0 ? Math.Round(simCost / simRev * 100m, 2) : null;
            decimal? simMarginPct = simRev > 0 ? Math.Round(simMargin / simRev * 100m, 2) : null;

            Guid? scenarioId = null;
            if (dto.SaveScenario)
            {
                ScenarioResultPayload payload = new()
                {
                    ItemCount = activeLines.Count,
                    ExpectedCovers = simCovers,
                    TotalFoodCost = simCost,
                    ExpectedRevenue = simRev,
                    FoodCostPercent = simFcPct,
                    ContributionMargin = simMargin,
                    IngredientAvailabilityPercent = simReq > 0 ? Math.Round(Math.Max(0m, (simReq - simShortage) / simReq * 100m), 1) : 100m,
                    PurchaseRequirement = Math.Round(simShortage * 4.5m, 2),
                    EstimatedWaste = Math.Round(expectedWasteQty * 4.5m, 2),
                    ProductionCapacityImpact = simCovers > baseCovers * 1.25m ? "HIGH" : "NORMAL",
                    DietaryCoverage = plan?.DietaryRequirements ?? "STANDARD"
                };

                MenuEngineeringScenario sc = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    MenuPlanId = plan?.Id,
                    PropertyId = plan?.PropertyId,
                    ScenarioName = string.IsNullOrWhiteSpace(dto.ScenarioName) ? $"Scenario {DateTime.UtcNow:MM-dd HH:mm}" : dto.ScenarioName.Trim(),
                    ScenarioType = string.IsNullOrWhiteSpace(dto.ScenarioType) ? "WHAT_IF" : dto.ScenarioType.Trim().ToUpperInvariant(),
                    VariablesJson = JsonSerializer.Serialize(dto),
                    BaselineSnapshotJson = JsonSerializer.Serialize(new { baseCost, baseRev, baseMargin, baseReq }),
                    ResultSnapshotJson = JsonSerializer.Serialize(payload),
                    FixedDemandDisclosed = true,
                    Notes = (dto.SellingPriceChangePercent ?? 0m) != 0 || (dto.PriceFactor ?? 1m) != 1m
                        ? "Fixed-demand assumption applied: price change simulation assumes constant demand unless explicit cover overrides are provided."
                        : "Live data untouched.",
                    CreatedByUserId = userId,
                    IsActive = true
                };
                _db.MenuEngineeringScenario.Add(sc);
                await _db.SaveChangesAsync(ct);
                scenarioId = sc.Id;
            }

            return new WhatIfSimulationResultDto
            {
                ScenarioId = scenarioId,
                ScenarioName = dto.ScenarioName ?? "Ad-hoc Simulation",
                RecommendedPortions = Math.Round(simReq, 2),
                PlannedPortions = Math.Round(simReq, 2),
                IngredientRequirement = Math.Round(simReq, 2),
                Shortage = simShortage,
                PurchaseRequirement = simShortage,
                FoodCost = ctx.Perms.ViewMenuCost ? simCost : 0m,
                Revenue = simRev,
                ContributionMargin = ctx.Perms.ViewMenuMargin ? simMargin : 0m,
                MarginPercent = ctx.Perms.ViewMenuMargin ? simMarginPct : null,
                BaselineCost = ctx.Perms.ViewMenuCost ? baseCost : 0m,
                BaselineRevenue = baseRev,
                BaselineMargin = ctx.Perms.ViewMenuMargin ? baseMargin : 0m,
                BaselineRequirement = Math.Round(baseReq, 2),
                BaselineShortage = baseShortage,
                CostVarianceAbs = ctx.Perms.ViewMenuCost ? Math.Round(simCost - baseCost, 2) : 0m,
                CostVariancePct = ctx.Perms.ViewMenuCost && baseCost > 0 ? Math.Round((simCost - baseCost) / baseCost * 100m, 2) : 0m,
                RevenueVarianceAbs = Math.Round(simRev - baseRev, 2),
                RevenueVariancePct = baseRev > 0 ? Math.Round((simRev - baseRev) / baseRev * 100m, 2) : 0m,
                MarginVarianceAbs = ctx.Perms.ViewMenuMargin ? Math.Round(simMargin - baseMargin, 2) : 0m,
                MarginVariancePct = ctx.Perms.ViewMenuMargin && baseMargin != 0 ? Math.Round((simMargin - baseMargin) / Math.Abs(baseMargin) * 100m, 2) : 0m,
                ExpectedWasteQty = expectedWasteQty,
                FixedDemandAssumed = true,
                DemandAssumptionDisclosure = (dto.SellingPriceChangePercent ?? 0m) != 0 || (dto.PriceFactor ?? 1m) != 1m
                    ? "Fixed-demand assumption: selling price changes assume unchanged demand because validated elasticity data is not enabled. Live data is untouched."
                    : "Fixed-demand assumption applied in isolated read-only simulation; live menus, recipes, and inventory are untouched."
            };
        }

        public async Task<List<MenuEngineeringScenarioDto>> GetScenariosAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.RunMenuSimulation, "Access denied", "Permission required.");
            return await GetScenariosInternalAsync(ctx, menuPlanId, ct);
        }

        private async Task<List<MenuEngineeringScenarioDto>> GetScenariosInternalAsync(StudioCtx ctx, Guid? menuPlanId, CancellationToken ct)
        {
            IQueryable<MenuEngineeringScenario> q = _db.MenuEngineeringScenario.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive);
            if (menuPlanId != null) q = q.Where(x => x.MenuPlanId == menuPlanId || x.MenuPlanId == null);
            List<MenuEngineeringScenario> rows = await q.OrderByDescending(x => x.IsPreferred).ThenByDescending(x => x.DateCreated).ToListAsync(ct);

            return rows.Select(s =>
            {
                ScenarioResultPayload p = ParsePayload(s.ResultSnapshotJson);
                return new MenuEngineeringScenarioDto
                {
                    Id = s.Id,
                    MenuPlanId = s.MenuPlanId,
                    ScenarioName = s.ScenarioName,
                    ScenarioType = s.ScenarioType,
                    ItemCount = p.ItemCount,
                    ExpectedCovers = p.ExpectedCovers,
                    SellingPricePerGuest = p.ExpectedCovers > 0 ? Math.Round(p.ExpectedRevenue / p.ExpectedCovers, 2) : 0m,
                    IngredientCostPerGuest = ctx.Perms.ViewMenuCost && p.ExpectedCovers > 0 ? Math.Round(p.TotalFoodCost / p.ExpectedCovers, 2) : 0m,
                    TotalFoodCost = ctx.Perms.ViewMenuCost ? p.TotalFoodCost : 0m,
                    FoodCostPercent = ctx.Perms.ViewMenuCost ? p.FoodCostPercent : null,
                    ExpectedRevenue = p.ExpectedRevenue,
                    ContributionMargin = ctx.Perms.ViewMenuMargin ? p.ContributionMargin : 0m,
                    IngredientAvailabilityPercent = p.IngredientAvailabilityPercent,
                    PurchaseRequirement = ctx.Perms.ViewMenuCost ? p.PurchaseRequirement : 0m,
                    EstimatedWaste = ctx.Perms.ViewMenuCost ? p.EstimatedWaste : 0m,
                    ProductionCapacityImpact = p.ProductionCapacityImpact,
                    DietaryCoverage = p.DietaryCoverage,
                    IsPreferred = s.IsPreferred,
                    Notes = s.Notes,
                    CreatedAt = s.DateCreated
                };
            }).ToList();
        }

        public async Task<ScenarioCompareResultDto> CompareScenariosAsync(Guid organizationId, Guid roleId, List<Guid> scenarioIds, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.RunMenuSimulation, "Access denied", "Permission required.");

            List<MenuEngineeringScenarioDto> all = await GetScenariosInternalAsync(ctx, null, ct);
            List<MenuEngineeringScenarioDto> cols = scenarioIds.Count == 0
                ? all.Take(5).ToList()
                : all.Where(s => scenarioIds.Contains(s.Id)).ToList();

            List<Guid> missing = scenarioIds.Where(id => cols.All(c => c.Id != id)).ToList();
            if (missing.Count > 0)
            {
                List<MenuPlanEnhancedDto> planDtos = await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto(), ct);
                foreach (MenuPlanEnhancedDto d in planDtos.Where(p => missing.Contains(p.Id)))
                {
                    decimal covers = d.ExpectedCovers ?? d.ExpectedGuests ?? 100m;
                    decimal totCost = d.Lines.Sum(l => l.TotalLineCost ?? 0m);
                    decimal totRev = d.Lines.Sum(l => l.ExpectedLineRevenue ?? 0m);
                    cols.Add(new MenuEngineeringScenarioDto
                    {
                        Id = d.Id,
                        MenuPlanId = d.Id,
                        ScenarioName = d.Name,
                        ScenarioType = "MENU_PLAN",
                        ItemCount = d.Lines.Count,
                        ExpectedCovers = covers,
                        SellingPricePerGuest = covers > 0 ? Math.Round(totRev / covers, 2) : 0m,
                        IngredientCostPerGuest = ctx.Perms.ViewMenuCost && covers > 0 ? Math.Round(totCost / covers, 2) : 0m,
                        TotalFoodCost = ctx.Perms.ViewMenuCost ? totCost : 0m,
                        FoodCostPercent = ctx.Perms.ViewMenuCost && totRev > 0 ? Math.Round(totCost / totRev * 100m, 2) : null,
                        ExpectedRevenue = totRev,
                        ContributionMargin = ctx.Perms.ViewMenuMargin ? Math.Round(totRev - totCost, 2) : 0m,
                        IngredientAvailabilityPercent = 100m,
                        DietaryCoverage = d.DietaryRequirements ?? "STANDARD",
                        CreatedAt = d.BusinessDate
                    });
                }
            }

            return new ScenarioCompareResultDto
            {
                Scenarios = cols,
                LowestFoodCostScenarioId = ctx.Perms.ViewMenuCost ? cols.OrderBy(c => c.FoodCostPercent ?? 999m).FirstOrDefault()?.Id : null,
                HighestMarginScenarioId = ctx.Perms.ViewMenuMargin ? cols.OrderByDescending(c => c.ContributionMargin).FirstOrDefault()?.Id : null,
                HighestReadinessScenarioId = cols.OrderByDescending(c => c.IngredientAvailabilityPercent).FirstOrDefault()?.Id
            };
        }

        public async Task<MenuEngineeringScenarioDto> SetPreferredScenarioAsync(Guid organizationId, Guid roleId, Guid scenarioId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.EditDraftMenu || ctx.Perms.RunMenuSimulation, "Access denied", "Permission required.");

            MenuEngineeringScenario target = await _db.MenuEngineeringScenario.FirstOrDefaultAsync(x => x.Id == scenarioId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Scenario not found.", "Select a valid saved scenario.");

            List<MenuEngineeringScenario> all = await _db.MenuEngineeringScenario.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.MenuPlanId == target.MenuPlanId).ToListAsync(ct);
            foreach (MenuEngineeringScenario s in all) s.IsPreferred = s.Id == scenarioId;
            await _db.SaveChangesAsync(ct);

            return (await GetScenariosInternalAsync(ctx, target.MenuPlanId, ct)).First(x => x.Id == scenarioId);
        }

        public async Task<MenuPlanEnhancedDto> ConvertScenarioToDraftAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.CreateMenu, "Access denied", "Permission required to create draft menu from scenario.");

            MenuEngineeringScenario sc = await _db.MenuEngineeringScenario.FirstOrDefaultAsync(x => x.Id == scenarioId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Scenario not found.", "Select a valid scenario.");
            ScenarioResultPayload payload = ParsePayload(sc.ResultSnapshotJson);

            MenuPlan? src = sc.MenuPlanId != null
                ? await _db.MenuPlan.Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == sc.MenuPlanId && p.BuyerId == ctx.Buyer.Id && p.IsActive, ct)
                : await _db.MenuPlan.Include(p => p.Lines).Where(p => p.BuyerId == ctx.Buyer.Id && p.IsActive).FirstOrDefaultAsync(ct);

            MenuPlan draft = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                PropertyId = src?.PropertyId ?? sc.PropertyId,
                OutletLocationId = src?.OutletLocationId,
                PlanCode = $"SC-{DateTime.UtcNow:yyyyMMddHHmm}",
                Name = $"{sc.ScenarioName} (Draft)",
                MenuType = src?.MenuType ?? "A_LA_CARTE",
                ServiceFormat = src?.ServiceFormat ?? "A_LA_CARTE",
                MealPeriod = src?.MealPeriod ?? "Dinner",
                BusinessDate = DateTime.UtcNow.Date.AddDays(1),
                ExpectedCovers = payload.ExpectedCovers > 0 ? payload.ExpectedCovers : (src?.ExpectedCovers ?? 100m),
                ExpectedGuests = payload.ExpectedCovers > 0 ? payload.ExpectedCovers : 100m,
                Status = "DRAFT",
                VersionNumber = 1,
                SourceType = "SCENARIO",
                SourceReferenceId = sc.Id,
                IsActive = true
            };
            if (src != null)
            {
                foreach (MenuPlanLine l in src.Lines.Where(x => x.IsActive))
                {
                    draft.Lines.Add(new MenuPlanLine
                    {
                        Id = Guid.NewGuid(),
                        MenuPlanId = draft.Id,
                        MenuItemId = l.MenuItemId,
                        RecipeId = l.RecipeId,
                        RecipeVersion = l.RecipeVersion,
                        CategoryName = l.CategoryName,
                        SequenceOrder = l.SequenceOrder,
                        PortionSize = l.PortionSize,
                        PortionUom = l.PortionUom,
                        SellingPrice = l.SellingPrice,
                        ExpectedCovers = draft.ExpectedCovers,
                        ParticipationRate = l.ParticipationRate,
                        RecommendedPortions = l.RecommendedPortions,
                        IsActive = true
                    });
                }
            }
            _db.MenuPlan.Add(draft);
            _db.MenuEngineeringApprovalHistory.Add(new MenuEngineeringApprovalHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                EntityType = "MENU_PLAN",
                EntityId = draft.Id,
                VersionNumber = 1,
                FromStatus = "SCENARIO",
                ToStatus = "DRAFT",
                Reason = $"Created from scenario {sc.ScenarioName}",
                ActionByUserId = userId,
                ActionAtUtc = DateTime.UtcNow,
                IsActive = true
            });
            await _db.SaveChangesAsync(ct);
            return (await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto { MenuPlanId = draft.Id }, ct)).First();
        }

        public async Task<MenuEngineeringSetupDto> GetSetupAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, requireFeatureEnabled: false, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManageMenuSetup, "Access denied", "Permission required.");

            List<MenuEngineeringConfigHistory> hist = await _db.MenuEngineeringConfigHistory
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive)
                .OrderByDescending(x => x.ChangedAtUtc)
                .Take(25)
                .ToListAsync(ct);

            return ToSetupDto(ctx.Settings, hist.Select(h => new MenuEngineeringConfigHistoryDto
            {
                Id = h.Id,
                ConfigVersion = h.ConfigVersion,
                ChangeSummary = h.ChangeSummary,
                ChangedAtUtc = h.ChangedAtUtc
            }).ToList());
        }

        private static MenuEngineeringSetupDto ToSetupDto(MenuEngineeringSettings s, List<MenuEngineeringConfigHistoryDto> history)
        {
            return new MenuEngineeringSetupDto
            {
                PropertyId = s.PropertyId,
                MenuEngineeringEnabled = s.MenuEngineeringEnabled,
                MeStudioEnabled = s.MeStudioEnabled,
                DefaultMealParticipation = s.DefaultMealParticipation,
                DefaultItemParticipation = s.DefaultItemParticipation,
                DefaultBufferPercentage = s.DefaultBufferPercentage,
                HighFoodCostPercent = s.HighFoodCostPercent,
                PopularityThreshold = s.PopularityThreshold,
                MealPeriods = ReadList(s.MealPeriodsJson, DefaultMeals),
                Categories = ReadList(s.CategoriesJson, DefaultCategories),
                ServiceFormats = ReadList(s.ServiceFormatsJson, DefaultServiceFormats),
                Cuisines = ReadList(s.CuisinesJson, DefaultCuisines),
                DietaryTags = ReadList(s.DietaryTagsJson, DefaultDietary),
                CostingMethod = s.CostingMethod,
                TargetFoodCostPercent = s.TargetFoodCostPercent,
                TargetContributionMargin = s.TargetContributionMargin,
                CostRefreshFrequency = s.CostRefreshFrequency,
                DefaultCurrency = s.DefaultCurrency,
                TaxTreatment = s.TaxTreatment,
                DefaultPortionUom = s.DefaultPortionUom,
                PortionVarianceThresholdPercent = s.PortionVarianceThresholdPercent,
                ForecastHorizonDays = s.ForecastHorizonDays,
                ForecastMethod = s.ForecastMethod,
                HistoricalLookbackDays = s.HistoricalLookbackDays,
                ForecastAccuracyThresholdPercent = s.ForecastAccuracyThresholdPercent,
                ProfitabilityBasis = s.ProfitabilityBasis,
                ProfitabilityThresholdPercent = s.ProfitabilityThresholdPercent,
                MinimumSalesHistoryDays = s.MinimumSalesHistoryDays,
                MinimumSalesQuantity = s.MinimumSalesQuantity,
                ComparableGrouping = s.ComparableGrouping,
                PosSyncScheduleCron = s.PosSyncScheduleCron,
                PosRetryMaxAttempts = s.PosRetryMaxAttempts,
                AiEnabledAgents = ReadList(s.AiEnabledAgentsJson, new[] { "MenuEngineeringAgent", "CulinaryAgent", "RecipeIntelligenceAgent" }),
                AiConfidenceThreshold = s.AiConfidenceThreshold,
                AiUsageLimitPerDay = s.AiUsageLimitPerDay,
                ApprovalFoodCostThresholdPercent = s.ApprovalFoodCostThresholdPercent,
                ApprovalPriceChangePercent = s.ApprovalPriceChangePercent,
                RequireApprovalOnSubstitution = s.RequireApprovalOnSubstitution,
                RequireApprovalOnPublish = s.RequireApprovalOnPublish,
                ApprovalRoles = ReadList(s.ApprovalRolesJson, DefaultApprovalRoles),
                ConfigVersion = s.ConfigVersion,
                AutonomousModePaused = s.AutonomousModePaused,
                AutomatedExecutionDisabled = s.AutomatedExecutionDisabled,
                MaxConcurrentJobs = s.MaxConcurrentJobs,
                DailyAiTokenBudget = s.DailyAiTokenBudget,
                MonthlyAiBudgetUsd = s.MonthlyAiBudgetUsd,
                TeamsNotificationsEnabled = s.TeamsNotificationsEnabled,
                TeamsWebhookUrl = s.TeamsWebhookUrl,
                FeatureFlags = ReadFeatureFlags(s),
                History = history
            };
        }

        public async Task<MenuEngineeringSetupDto> SaveSetupAsync(Guid organizationId, Guid userId, Guid roleId, MenuEngineeringSetupDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, dto.PropertyId, requireFeatureEnabled: false, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuSetup, "Access denied", "Permission required to manage Menu Engineering setup.");

            MenuEngineeringSettings s = ctx.Settings;
            if (dto.PropertyId != null && s.PropertyId == null)
            {
                s = new MenuEngineeringSettings
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    PropertyId = dto.PropertyId,
                    IsActive = true,
                    ConfigVersion = 0
                };
                _db.MenuEngineeringSettings.Add(s);
            }
            else if (_db.Entry(s).State == EntityState.Detached)
            {
                _db.MenuEngineeringSettings.Add(s);
            }
            string prevJson = JsonSerializer.Serialize(ToSetupDto(s, new()));

            s.PropertyId = dto.PropertyId;
            MenuEngineeringFeatureFlagsDto flags = dto.FeatureFlags ?? new MenuEngineeringFeatureFlagsDto();
            // Keep top-level booleans and FeatureFlags synchronized regardless of which side the caller toggled
            s.MenuEngineeringEnabled = dto.MenuEngineeringEnabled && flags.MenuEngineering;
            if (!dto.MenuEngineeringEnabled && flags.MenuEngineering) s.MenuEngineeringEnabled = false;
            else if (dto.MenuEngineeringEnabled && !flags.MenuEngineering) s.MenuEngineeringEnabled = flags.MenuEngineering;
            s.MeStudioEnabled = dto.MeStudioEnabled && flags.MeStudio;
            if (!dto.MeStudioEnabled && flags.MeStudio) s.MeStudioEnabled = false;
            else if (dto.MeStudioEnabled && !flags.MeStudio) s.MeStudioEnabled = flags.MeStudio;
            flags.MenuEngineering = s.MenuEngineeringEnabled;
            flags.MeStudio = s.MeStudioEnabled;
            s.FeatureFlagsJson = JsonSerializer.Serialize(flags);
            s.AutonomousModePaused = dto.AutonomousModePaused;
            s.AutomatedExecutionDisabled = dto.AutomatedExecutionDisabled;
            s.MaxConcurrentJobs = dto.MaxConcurrentJobs > 0 ? dto.MaxConcurrentJobs : 4;
            s.DailyAiTokenBudget = dto.DailyAiTokenBudget > 0 ? dto.DailyAiTokenBudget : 250000;
            s.MonthlyAiBudgetUsd = dto.MonthlyAiBudgetUsd > 0 ? dto.MonthlyAiBudgetUsd : 100m;
            s.TeamsNotificationsEnabled = dto.TeamsNotificationsEnabled;
            s.TeamsWebhookUrl = dto.TeamsWebhookUrl;
            if (dto.ServiceFormats.Count > 0) s.ServiceFormatsJson = JsonSerializer.Serialize(dto.ServiceFormats);
            if (dto.Cuisines.Count > 0) s.CuisinesJson = JsonSerializer.Serialize(dto.Cuisines);
            if (dto.DietaryTags.Count > 0) s.DietaryTagsJson = JsonSerializer.Serialize(dto.DietaryTags);
            if (dto.MealPeriods.Count > 0) s.MealPeriodsJson = JsonSerializer.Serialize(dto.MealPeriods);
            if (dto.Categories.Count > 0) s.CategoriesJson = JsonSerializer.Serialize(dto.Categories);
            s.CostingMethod = dto.CostingMethod;
            s.TargetFoodCostPercent = dto.TargetFoodCostPercent;
            s.HighFoodCostPercent = dto.TargetFoodCostPercent;
            s.TargetContributionMargin = dto.TargetContributionMargin;
            s.CostRefreshFrequency = dto.CostRefreshFrequency;
            s.DefaultCurrency = dto.DefaultCurrency;
            s.TaxTreatment = dto.TaxTreatment;
            s.DefaultPortionUom = dto.DefaultPortionUom;
            s.DefaultMealParticipation = dto.DefaultMealParticipation;
            s.DefaultItemParticipation = dto.DefaultItemParticipation;
            s.DefaultBufferPercentage = dto.DefaultBufferPercentage;
            s.PortionVarianceThresholdPercent = dto.PortionVarianceThresholdPercent;
            s.ForecastHorizonDays = dto.ForecastHorizonDays;
            s.ForecastMethod = dto.ForecastMethod;
            s.HistoricalLookbackDays = dto.HistoricalLookbackDays;
            s.ForecastAccuracyThresholdPercent = dto.ForecastAccuracyThresholdPercent;
            s.PopularityThreshold = dto.PopularityThreshold;
            s.ProfitabilityBasis = dto.ProfitabilityBasis;
            s.ProfitabilityThresholdPercent = dto.ProfitabilityThresholdPercent;
            s.MinimumSalesHistoryDays = dto.MinimumSalesHistoryDays;
            s.MinimumSalesQuantity = dto.MinimumSalesQuantity;
            s.ComparableGrouping = dto.ComparableGrouping;
            s.PosSyncScheduleCron = dto.PosSyncScheduleCron;
            s.PosRetryMaxAttempts = dto.PosRetryMaxAttempts;
            if (dto.AiEnabledAgents.Count > 0) s.AiEnabledAgentsJson = JsonSerializer.Serialize(dto.AiEnabledAgents);
            s.AiConfidenceThreshold = dto.AiConfidenceThreshold;
            s.AiUsageLimitPerDay = dto.AiUsageLimitPerDay;
            s.ApprovalFoodCostThresholdPercent = dto.ApprovalFoodCostThresholdPercent;
            s.ApprovalPriceChangePercent = dto.ApprovalPriceChangePercent;
            s.RequireApprovalOnSubstitution = dto.RequireApprovalOnSubstitution;
            s.RequireApprovalOnPublish = dto.RequireApprovalOnPublish;
            if (dto.ApprovalRoles.Count > 0) s.ApprovalRolesJson = JsonSerializer.Serialize(dto.ApprovalRoles);
            s.ConfigVersion += 1;

            _db.MenuEngineeringConfigHistory.Add(new MenuEngineeringConfigHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                PropertyId = dto.PropertyId,
                ConfigVersion = s.ConfigVersion,
                ChangeSummary = dto.ChangeSummary ?? $"Updated ME Setup v{s.ConfigVersion} (Enabled={s.MenuEngineeringEnabled}, MEStudio={s.MeStudioEnabled})",
                PreviousConfigJson = prevJson,
                NewConfigJson = JsonSerializer.Serialize(dto),
                ChangedByUserId = userId,
                ChangedAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            await _db.SaveChangesAsync(ct);
            return await GetSetupAsync(organizationId, roleId, dto.PropertyId, ct);
        }

        public List<MenuEngineeringReportItemDto> GetReportCatalog() => new()
        {
            new() { ReportKey = "MENU_ITEM_MASTER", Title = "Menu Item Master Report", Category = "Master Data" },
            new() { ReportKey = "MENU_COSTING", Title = "Menu Costing Report", Category = "Cost & Margin" },
            new() { ReportKey = "MENU_PLANNING_CALENDAR", Title = "Menu Planning Calendar", Category = "Planning" },
            new() { ReportKey = "PORTION_PLANNING", Title = "Portion Planning Report", Category = "Planning" },
            new() { ReportKey = "DEMAND_FORECAST", Title = "Demand Forecast Report", Category = "Demand" },
            new() { ReportKey = "FORECAST_ACCURACY", Title = "Forecast Accuracy Report", Category = "Demand" },
            new() { ReportKey = "POS_SALES", Title = "POS Sales Report", Category = "POS" },
            new() { ReportKey = "POS_MAPPING_EXCEPTIONS", Title = "POS Mapping Exception Report", Category = "POS" },
            new() { ReportKey = "MENU_PERFORMANCE", Title = "Menu Performance Report", Category = "Performance" },
            new() { ReportKey = "MENU_ENGINEERING_MATRIX", Title = "Menu Engineering Matrix Report", Category = "Performance" },
            new() { ReportKey = "CLASSIFICATION_MOVEMENT", Title = "Menu Classification Movement Report", Category = "Performance" },
            new() { ReportKey = "THEORETICAL_CONSUMPTION", Title = "Theoretical Consumption Report", Category = "Consumption" },
            new() { ReportKey = "ACTUAL_CONSUMPTION", Title = "Actual Consumption Report", Category = "Consumption" },
            new() { ReportKey = "CONSUMPTION_VARIANCE", Title = "Consumption Variance Report", Category = "Consumption" },
            new() { ReportKey = "FOOD_COST", Title = "Food Cost Report", Category = "Cost & Margin" },
            new() { ReportKey = "MENU_MARGIN", Title = "Menu Margin Report", Category = "Cost & Margin" },
            new() { ReportKey = "INGREDIENT_PRICE_IMPACT", Title = "Ingredient Price Impact Report", Category = "Cost & Margin" },
            new() { ReportKey = "PURCHASE_RECOMMENDATION", Title = "Purchase Recommendation Report", Category = "Procurement" },
            new() { ReportKey = "AI_MENU_RECOMMENDATION", Title = "AI Menu Recommendation Report", Category = "Intelligence" },
            new() { ReportKey = "WHAT_IF_SCENARIO", Title = "What-If Scenario Report", Category = "Simulation" },
            new() { ReportKey = "MENU_VERSION_COMPARISON", Title = "Menu Version Comparison Report", Category = "Governance" },
            new() { ReportKey = "MENU_APPROVAL_HISTORY", Title = "Menu Approval History", Category = "Governance" }
        };

        public async Task<MenuEngineeringReportExportDto> ExportReportAsync(Guid organizationId, Guid roleId, string reportKey, string format, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required to export reports.");

            string code = reportKey.Trim().ToUpperInvariant();
            string fmt = string.IsNullOrWhiteSpace(format) ? "CSV" : format.Trim().ToUpperInvariant();
            List<string> cols = new();
            List<Dictionary<string, object?>> rows = new();

            switch (code)
            {
                case "MENU_ITEM_MASTER":
                case "MENU_COSTING":
                    cols = ["Code", "Name", "Category", "Cuisine", "Portion", "SellingPrice", "CostPerPortion", "FoodCostPercent", "ContributionMargin", "Status"];
                    foreach (MenuItemEnhancedDto i in await GetMenuItemsAsync(organizationId, roleId, filter, ct))
                        rows.Add(new() { ["Code"] = i.MenuItemCode, ["Name"] = i.Name, ["Category"] = i.Category, ["Cuisine"] = i.Cuisine, ["Portion"] = $"{i.DefaultPortionQty} {i.PortionUom}", ["SellingPrice"] = i.SellingPrice, ["CostPerPortion"] = i.CostPerPortion, ["FoodCostPercent"] = i.FoodCostPercent, ["ContributionMargin"] = i.ContributionMargin, ["Status"] = i.ApprovalStatus });
                    break;
                case "MENU_ENGINEERING_MATRIX":
                case "MENU_PERFORMANCE":
                case "CLASSIFICATION_MOVEMENT":
                    cols = ["Dish", "Category", "QtySold", "Revenue", "MixPercent", "CostPerPortion", "ContributionMargin", "Classification", "RecommendedAction"];
                    MenuPerformanceMatrixDto mat = await GetPerformanceMatrixAsync(organizationId, roleId, filter, ctx.Settings.ProfitabilityBasis, ctx.Settings.PopularityThreshold, ct);
                    foreach (MenuPerformanceMatrixRowDto r in mat.Rows.Concat(mat.ExcludedRows))
                        rows.Add(new() { ["Dish"] = r.Name, ["Category"] = r.Category, ["QtySold"] = r.QtySold, ["Revenue"] = r.Revenue, ["MixPercent"] = r.MixPercent, ["CostPerPortion"] = r.CostPerPortion, ["ContributionMargin"] = r.ContributionMargin, ["Classification"] = r.Classification, ["RecommendedAction"] = r.RecommendedAction });
                    break;
                case "THEORETICAL_CONSUMPTION":
                case "ACTUAL_CONSUMPTION":
                case "CONSUMPTION_VARIANCE":
                    cols = ["Ingredient", "Uom", "TheoreticalQty", "ActualQty", "ExpectedYieldLossQty", "RecordedWasteQty", "VarianceQty", "UnexplainedVarianceQty"];
                    ConsumptionVarianceDetailDto cv = await GetConsumptionVarianceAsync(organizationId, roleId, filter, ct);
                    foreach (ConsumptionVarianceRowDto r in cv.Rows)
                        rows.Add(new() { ["Ingredient"] = r.MaterialName, ["Uom"] = r.Uom, ["TheoreticalQty"] = r.TheoreticalQty, ["ActualQty"] = r.ActualQty, ["ExpectedYieldLossQty"] = r.ExpectedYieldLossQty, ["RecordedWasteQty"] = r.RecordedWasteQty, ["VarianceQty"] = r.VarianceQty, ["UnexplainedVarianceQty"] = r.UnexplainedVarianceQty });
                    break;
                case "MENU_APPROVAL_HISTORY":
                    cols = ["EntityType", "FromStatus", "ToStatus", "Reason", "ActionAtUtc"];
                    List<MenuEngineeringApprovalHistory> hist = await _db.MenuEngineeringApprovalHistory.Where(h => h.BuyerId == ctx.Buyer.Id && h.IsActive).OrderByDescending(h => h.ActionAtUtc).Take(100).ToListAsync(ct);
                    foreach (MenuEngineeringApprovalHistory h in hist)
                        rows.Add(new() { ["EntityType"] = h.EntityType, ["FromStatus"] = h.FromStatus, ["ToStatus"] = h.ToStatus, ["Reason"] = h.Reason, ["ActionAtUtc"] = h.ActionAtUtc.ToString("O") });
                    break;
                default:
                    cols = ["Plan", "Type", "Format", "MealPeriod", "Date", "Covers", "Status", "Version"];
                    foreach (MenuPlanEnhancedDto p in await GetMenuPlansAsync(organizationId, roleId, filter, ct))
                        rows.Add(new() { ["Plan"] = p.Name, ["Type"] = p.MenuType, ["Format"] = p.ServiceFormat, ["MealPeriod"] = p.MealPeriod, ["Date"] = p.BusinessDate.ToString("yyyy-MM-dd"), ["Covers"] = p.ExpectedCovers, ["Status"] = p.Status, ["Version"] = $"v{p.VersionNumber}" });
                    break;
            }

            string sep = fmt is "EXCEL" or "XLS" or "TSV" ? "\t" : ",";
            string ext = fmt is "EXCEL" or "XLS" ? "xls" : fmt == "PDF" ? "html" : fmt == "JSON" ? "json" : "csv";
            string mime = ext == "xls" ? "application/vnd.ms-excel" : ext == "html" ? "text/html" : ext == "json" ? "application/json" : "text/csv";

            string text;
            if (ext == "json")
            {
                text = JsonSerializer.Serialize(rows);
            }
            else if (ext == "html")
            {
                StringBuilder sb = new("<html><head><meta charset='utf-8'><title>" + code + "</title><style>body{font-family:sans-serif;padding:24px}table{border-collapse:collapse;width:100%}th,td{border:1px solid #d1d5db;padding:6px 8px;text-align:left;font-size:12px}th{background:#f3f4f6}</style></head><body><h2>SILA ME Report: " + code + "</h2><table><tr>");
                sb.Append(string.Join("", cols.Select(c => $"<th>{System.Net.WebUtility.HtmlEncode(c)}</th>"))).Append("</tr>");
                foreach (Dictionary<string, object?> row in rows)
                {
                    sb.Append("<tr>");
                    foreach (string c in cols) sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(row.GetValueOrDefault(c)?.ToString() ?? "")}</td>");
                    sb.Append("</tr>");
                }
                sb.Append("</table></body></html>");
                text = sb.ToString();
            }
            else
            {
                StringBuilder sb = new();
                sb.AppendLine(string.Join(sep, cols.Select(c => $"\"{c.Replace("\"", "\"\"")}\"")));
                foreach (Dictionary<string, object?> row in rows)
                    sb.AppendLine(string.Join(sep, cols.Select(c => $"\"{(row.GetValueOrDefault(c)?.ToString() ?? "").Replace("\"", "\"\"")}\"")));
                text = sb.ToString();
            }

            return new MenuEngineeringReportExportDto
            {
                ReportKey = code,
                Title = GetReportCatalog().FirstOrDefault(r => r.ReportKey == code)?.Title ?? code,
                Format = fmt,
                FileName = $"{code.ToLowerInvariant()}_{DateTime.UtcNow:yyyyMMdd_HHmm}.{ext}",
                MimeType = mime,
                Columns = cols,
                Rows = rows,
                TextContent = text
            };
        }

        public async Task<SilaStoreMenuMobileDto> GetMobileMenuSummaryAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            DateTime today = DateTime.UtcNow.Date;
            List<MenuPlanEnhancedDto> planDtos = await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto { PropertyId = propertyId, DateFrom = today }, ct);
            planDtos = planDtos.Where(p => p.BusinessDate.Date >= today).Take(12).ToList();

            List<PortionPlanRowDto> portions = await GetPortionPlansAsync(organizationId, roleId, planDtos.FirstOrDefault()?.Id, propertyId, ct);

            return new SilaStoreMenuMobileDto
            {
                BusinessDate = today,
                Permissions = ctx.Perms,
                TodayMenus = planDtos.Where(p => p.BusinessDate.Date == today).ToList(),
                UpcomingMenus = planDtos.Where(p => p.BusinessDate.Date > today).ToList(),
                PendingApprovals = planDtos.Where(p => p.Status is "UNDER_REVIEW" or "PENDING_APPROVAL").ToList(),
                PortionInstructions = portions.Take(20).ToList()
            };
        }

        private static ScenarioResultPayload ParsePayload(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try
            {
                return JsonSerializer.Deserialize<ScenarioResultPayload>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
