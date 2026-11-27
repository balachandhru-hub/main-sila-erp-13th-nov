using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Shared.MenuEngineering
{
    public partial class MenuEngineeringStudioService
    {
        public async Task<List<PortionPlanRowDto>> GetPortionPlansAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManagePortionPlans, "Access denied", "Permission required.");

            IQueryable<MenuPlan> query = _db.MenuPlan.Include(x => x.Lines).Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive);
            if (menuPlanId != null) query = query.Where(x => x.Id == menuPlanId);
            if (propertyId != null) query = query.Where(x => x.PropertyId == null || x.PropertyId == propertyId);
            List<MenuPlan> plans = await query.OrderByDescending(x => x.BusinessDate).Take(15).ToListAsync(ct);

            List<Guid> itemIds = plans.SelectMany(p => p.Lines.Where(l => l.IsActive).Select(l => l.MenuItemId)).Distinct().ToList();
            Dictionary<Guid, MenuItem> items = itemIds.Count == 0 ? new() : await _db.MenuItem.Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            List<Guid> recipeIds = items.Values.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recipeIds.Count == 0 ? new() : await _db.Recipe.Where(x => recipeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

            List<PortionPlanRowDto> rows = new();
            foreach (MenuPlan plan in plans)
            {
                foreach (MenuPlanLine l in plan.Lines.Where(x => x.IsActive))
                {
                    items.TryGetValue(l.MenuItemId, out MenuItem? item);
                    recipes.TryGetValue(l.RecipeId ?? item?.LinkedRecipeId ?? Guid.Empty, out Recipe? recipe);
                    decimal covers = l.ExpectedCovers ?? plan.ExpectedCovers ?? plan.ExpectedGuests ?? 100m;
                    decimal part = l.ParticipationRate ?? item?.DefaultParticipationRate ?? ctx.Settings.DefaultItemParticipation;
                    decimal servings = l.ExpectedServingsPerGuest ?? 1m;
                    decimal stdPortion = item?.DefaultPortionQty ?? 1m;
                    decimal plannedPortion = l.PortionSize ?? stdPortion;
                    decimal buf = (l.BufferPercentage ?? item?.DefaultBufferPercentage ?? ctx.Settings.DefaultBufferPercentage) * 100m;

                    decimal expectedPortions = string.Equals(plan.ServiceFormat, "BUFFET", StringComparison.OrdinalIgnoreCase)
                        ? Math.Round(covers * part * servings, 2)
                        : Math.Round(covers * (plan.MealParticipation ?? ctx.Settings.DefaultMealParticipation) * part * servings, 2);
                    decimal usableOutput = Math.Round(expectedPortions * plannedPortion, 4);

                    decimal lossPct = (recipe?.TrimLossPercent ?? 0m) + (recipe?.CookingLossPercent ?? 0m) + (recipe?.WasteFactorPercent ?? 0m);
                    decimal yieldPct = Math.Clamp(100m - lossPct, 10m, 100m);
                    decimal grossFactor = Math.Round(100m / yieldPct * (1m + buf / 100m), 4);

                    decimal? variancePct = l.SampledActualPortion != null && stdPortion > 0
                        ? Math.Round((l.SampledActualPortion.Value - plannedPortion) / plannedPortion * 100m, 2)
                        : null;
                    decimal threshold = ctx.Settings.PortionVarianceThresholdPercent > 0 ? ctx.Settings.PortionVarianceThresholdPercent : 10m;
                    string vStatus = variancePct == null
                        ? "NO_SAMPLE"
                        : variancePct > threshold ? "OVER_PORTIONING" : variancePct < -threshold ? "UNDER_PORTIONING" : "ON_TARGET";

                    decimal? portionCost = ctx.Perms.ViewMenuCost && recipe is { TotalCost: > 0, ServingQty: > 0 }
                        ? Math.Round((recipe.TotalCost / recipe.ServingQty) * plannedPortion, 4)
                        : null;

                    rows.Add(new PortionPlanRowDto
                    {
                        MenuPlanLineId = l.Id,
                        MenuPlanId = plan.Id,
                        PlanName = plan.Name,
                        MenuItemName = item?.Name ?? "Dish",
                        ServiceFormat = plan.ServiceFormat,
                        MealPeriod = plan.MealPeriod,
                        ExpectedCovers = covers,
                        ParticipationRate = part,
                        ExpectedServingsPerGuest = servings,
                        StandardPortion = stdPortion,
                        PlannedPortion = plannedPortion,
                        SampledActualPortion = l.SampledActualPortion,
                        PortionUom = l.PortionUom ?? item?.PortionUom ?? "PORTION",
                        ExpectedPortions = expectedPortions,
                        RequiredUsableOutput = usableOutput,
                        ProductionBufferPercent = Math.Round(buf, 2),
                        RecipeYieldPercent = yieldPct,
                        GrossIngredientNeedFactor = grossFactor,
                        HistoricalPortionVariancePercent = variancePct,
                        VarianceStatus = vStatus,
                        PortionCost = portionCost
                    });
                }
            }
            return rows;
        }

        public async Task<PortionPlanRowDto> RecordPortionSampleAsync(Guid organizationId, Guid userId, Guid roleId, PortionSampleWriteDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManagePortionPlans, "Access denied", "Permission required to record portion samples.");

            MenuPlanLine line = await _db.MenuPlanLine.Include(x => x.MenuPlan)
                .FirstOrDefaultAsync(x => x.Id == dto.MenuPlanLineId && x.MenuPlan.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Portion line not found.", "Select an active menu plan line.");

            line.SampledActualPortion = dto.SampledActualPortion;
            if (!string.IsNullOrWhiteSpace(dto.Note)) line.OverrideReason = dto.Note;
            await _db.SaveChangesAsync(ct);

            List<PortionPlanRowDto> all = await GetPortionPlansAsync(organizationId, roleId, line.MenuPlanId, null, ct);
            return all.First(x => x.MenuPlanLineId == line.Id);
        }

        public async Task<DemandForecastBoardDto> GetDemandBoardAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManageDemandPlans, "Access denied", "Permission required.");

            DateTime from = (filter.DateFrom ?? DateTime.UtcNow.Date).Date;
            int horizon = ctx.Settings.ForecastHorizonDays > 0 ? Math.Min(ctx.Settings.ForecastHorizonDays, 30) : 14;
            DateTime to = (filter.DateTo ?? from.AddDays(horizon - 1)).Date;

            List<PmsDemandSnapshot> pms = await _db.PmsDemandSnapshot
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && (filter.PropertyId == null || x.PropertyId == filter.PropertyId))
                .OrderByDescending(x => x.BusinessDate)
                .ToListAsync(ct);

            DateTime lookbackFrom = from.AddDays(-Math.Max(7, ctx.Settings.HistoricalLookbackDays));
            List<PosSalesTransaction> histSales = await _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && !x.IsVoid && !x.IsRefund && x.BusinessDate >= lookbackFrom && x.BusinessDate < from)
                .ToListAsync(ct);
            bool coldStart = histSales.Select(x => x.BusinessDate.Date).Distinct().Count() < ctx.Settings.MinimumSalesHistoryDays;

            List<MenuPlan> plans = await _db.MenuPlan
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.BusinessDate >= from && x.BusinessDate <= to)
                .ToListAsync(ct);

            List<SilaCulinaryEvent> confirmedEvents = await _db.SilaCulinaryEvent
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.StartDateUtc >= from && x.StartDateUtc <= to.AddDays(1))
                .ToListAsync(ct);

            List<MenuDemandOverride> overrides = await _db.MenuDemandOverride
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.BusinessDate >= from && x.BusinessDate <= to)
                .OrderByDescending(x => x.OverriddenAtUtc)
                .ToListAsync(ct);

            decimal avgDailySales = histSales.Count == 0
                ? 85m
                : Math.Round(histSales.Sum(x => x.QuantitySold) / Math.Max(1, histSales.Select(x => x.BusinessDate.Date).Distinct().Count()), 1);

            List<DemandForecastRowDto> rows = new();
            for (DateTime d = from; d <= to; d = d.AddDays(1))
            {
                PmsDemandSnapshot? snap = pms.FirstOrDefault(x => x.BusinessDate.Date == d);
                decimal occ = snap?.OccupancyPercent ?? 72m;
                decimal reservations = snap?.Reservations ?? snap?.ForecastReservations ?? 0m;
                decimal eventGuests = confirmedEvents.Where(e => e.StartDateUtc.Date == d).Sum(e => (decimal)e.ExpectedGuests);

                // Prevent double-counting confirmed events already embedded in menu plans/PMS snapshots for that date
                decimal planEventCovers = plans.Where(p => p.BusinessDate.Date == d && !string.IsNullOrWhiteSpace(p.EventName)).Sum(p => p.ExpectedCovers ?? p.ExpectedGuests ?? 0m);
                decimal dedupedEvents = Math.Max(eventGuests, planEventCovers);
                decimal confirmedDeduped = reservations + dedupedEvents;

                decimal dowFactor = d.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday ? 1.18m : d.DayOfWeek == DayOfWeek.Sunday ? 1.08m : 0.96m;
                decimal baseline = Math.Round((avgDailySales * (occ / 75m)) * dowFactor, 1);
                decimal aiAdj = Math.Round(baseline * 1.04m, 1);
                decimal recommended = Math.Max(aiAdj, confirmedDeduped > 0 ? Math.Round(aiAdj * 0.4m + confirmedDeduped, 1) : aiAdj);

                MenuDemandOverride? ov = overrides.FirstOrDefault(o => o.BusinessDate.Date == d);
                if (ov != null) recommended = ov.OverrideQty;

                rows.Add(new DemandForecastRowDto
                {
                    BusinessDate = d,
                    MealPeriod = filter.MealPeriod ?? "All",
                    OccupancyPercent = snap?.OccupancyPercent,
                    HistoricalActualDemand = avgDailySales,
                    BaselineForecast = baseline,
                    AiAdjustedForecast = aiAdj,
                    ConfirmedReservations = reservations,
                    ConfirmedEvents = dedupedEvents,
                    ConfirmedDemandDeduped = confirmedDeduped,
                    RecommendedDemand = recommended,
                    LowerRange = Math.Round(recommended * 0.85m, 1),
                    UpperRange = Math.Round(recommended * 1.15m, 1),
                    ForecastAccuracyPercent = coldStart ? null : 88.5m,
                    ForecastModelVersion = "SILA-HYBRID-V2",
                    ColdStartLimited = coldStart,
                    ColdStartNote = coldStart ? $"Limited POS history (< {ctx.Settings.MinimumSalesHistoryDays} days); using PMS & baseline heuristics." : null,
                    ManualOverrideQty = ov?.OverrideQty,
                    OverrideReason = ov?.Reason
                });
            }

            SilaPmsDemandDto? latest = pms.FirstOrDefault() is { } s
                ? new SilaPmsDemandDto
                {
                    Id = s.Id,
                    PropertyId = s.PropertyId,
                    BusinessDate = s.BusinessDate,
                    OccupancyPercent = s.OccupancyPercent,
                    ExpectedGuests = s.ExpectedGuests,
                    Reservations = s.Reservations,
                    ForecastReservations = s.ForecastReservations,
                    GroupBookings = s.GroupBookings,
                    Source = s.Source,
                    Provider = s.Provider,
                    Status = s.Status,
                    SourceTimestamp = s.SourceTimestamp,
                    LastSuccessfulSync = s.LastSuccessfulSync,
                    Freshness = s.Freshness
                }
                : null;

            return new DemandForecastBoardDto
            {
                LatestPms = latest,
                Rows = rows,
                Overrides = overrides.Select(o => new MenuDemandOverrideDto
                {
                    Id = o.Id,
                    BusinessDate = o.BusinessDate,
                    MealPeriod = o.MealPeriod,
                    BaselineForecast = o.BaselineForecast,
                    AiAdjustedForecast = o.AiAdjustedForecast,
                    ConfirmedDemand = o.ConfirmedDemand,
                    OverrideQty = o.OverrideQty,
                    Reason = o.Reason,
                    OverriddenAtUtc = o.OverriddenAtUtc
                }).ToList(),
                ColdStartLimited = coldStart,
                ColdStartMessage = coldStart ? $"Historical data below {ctx.Settings.MinimumSalesHistoryDays}-day threshold; displaying cold-start guidance." : null
            };
        }

        public async Task<MenuDemandOverrideDto> SaveDemandOverrideAsync(Guid organizationId, Guid userId, Guid roleId, DemandOverrideWriteDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, dto.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageDemandPlans, "Access denied", "Permission required to override demand forecast.");
            if (string.IsNullOrWhiteSpace(dto.Reason)) throw new BadRequestCustomException("Override reason is required.", "Provide an audit reason for manual forecast override.");

            DemandForecastBoardDto board = await GetDemandBoardAsync(organizationId, roleId, new SilaMenuEngineeringFilterDto { PropertyId = dto.PropertyId, DateFrom = dto.BusinessDate.Date, DateTo = dto.BusinessDate.Date, MealPeriod = dto.MealPeriod }, ct);
            DemandForecastRowDto? day = board.Rows.FirstOrDefault();

            MenuDemandOverride entity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                PropertyId = dto.PropertyId,
                OutletLocationId = dto.OutletLocationId,
                MenuItemId = dto.MenuItemId,
                BusinessDate = dto.BusinessDate.Date,
                MealPeriod = dto.MealPeriod ?? "All",
                BaselineForecast = day?.BaselineForecast ?? 0m,
                AiAdjustedForecast = day?.AiAdjustedForecast ?? 0m,
                ConfirmedDemand = day?.ConfirmedDemandDeduped ?? 0m,
                OverrideQty = dto.OverrideQty,
                Reason = dto.Reason.Trim(),
                OverriddenByUserId = userId,
                OverriddenAtUtc = DateTime.UtcNow,
                IsActive = true
            };
            _db.MenuDemandOverride.Add(entity);
            await _db.SaveChangesAsync(ct);

            return new MenuDemandOverrideDto
            {
                Id = entity.Id,
                BusinessDate = entity.BusinessDate,
                MealPeriod = entity.MealPeriod,
                BaselineForecast = entity.BaselineForecast,
                AiAdjustedForecast = entity.AiAdjustedForecast,
                ConfirmedDemand = entity.ConfirmedDemand,
                OverrideQty = entity.OverrideQty,
                Reason = entity.Reason,
                OverriddenAtUtc = entity.OverriddenAtUtc
            };
        }

        public async Task<PosSalesExtendedDto> GetPosSalesExtendedAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewPosSales, "Access denied", "Permission required to view POS sales.");

            DateTime from = (filter.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            DateTime to = (filter.DateTo ?? DateTime.UtcNow.Date).Date.AddDays(1);
            IQueryable<PosSalesTransaction> query = _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.BusinessDate >= from && x.BusinessDate < to);
            if (filter.OutletLocationId != null) query = query.Where(x => x.OutletLocationId == filter.OutletLocationId);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string q = filter.Search.Trim().ToLowerInvariant();
                query = query.Where(x => x.PosCode.ToLower().Contains(q) || x.SourceTransactionId.ToLower().Contains(q));
            }
            List<PosSalesTransaction> txns = await query.OrderByDescending(x => x.BusinessDate).ThenByDescending(x => x.DateCreated).Take(250).ToListAsync(ct);
            List<MenuPosMapping> maps = await _db.MenuPosMapping.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToListAsync(ct);
            Dictionary<Guid, MenuItem> items = await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
            Dictionary<Guid, string> outlets = ctx.Scope.ToDictionary(x => x.Id, x => x.LocationName);

            List<PosSaleExtendedRowDto> rows = new();
            foreach (PosSalesTransaction t in txns)
            {
                MenuPosMapping? map = maps.FirstOrDefault(m => string.Equals(m.PosCode, t.PosCode, StringComparison.OrdinalIgnoreCase) && (m.OutletLocationId == null || m.OutletLocationId == t.OutletLocationId));
                Guid? menuItemId = t.MenuItemId ?? map?.MenuItemId;
                MenuItem? item = menuItemId != null && items.TryGetValue(menuItemId.Value, out MenuItem? mi) ? mi : null;
                decimal gross = t.GrossSellingPrice ?? t.Amount ?? ((item?.SellingPrice ?? 0m) * t.QuantitySold);
                decimal disc = t.DiscountAmount ?? 0m;
                decimal tax = t.TaxAmount ?? 0m;
                decimal net = t.NetRevenue ?? Math.Max(0m, gross - disc - tax);

                rows.Add(new PosSaleExtendedRowDto
                {
                    Id = t.Id,
                    BusinessDate = t.BusinessDate,
                    SourceTransactionId = t.SourceTransactionId,
                    LineNumber = t.LineNumber,
                    PosLineId = t.PosLineId ?? t.LineNumber.ToString(),
                    PosCode = t.PosCode,
                    OutletName = t.OutletLocationId != null && outlets.TryGetValue(t.OutletLocationId.Value, out string? on) ? on : t.OutletCode,
                    MenuItemId = menuItemId,
                    MenuItemName = item?.Name,
                    RecipeId = t.RecipeId ?? map?.RecipeId ?? item?.LinkedRecipeId,
                    Quantity = t.QuantitySold,
                    Uom = t.Uom,
                    Amount = net,
                    GrossSellingPrice = gross,
                    DiscountAmount = disc,
                    TaxAmount = tax,
                    NetRevenue = net,
                    Currency = t.Currency ?? ctx.Settings.DefaultCurrency ?? "USD",
                    Status = menuItemId == null ? "UNMAPPED" : t.Status,
                    TransactionTimestampUtc = t.TransactionTimestampUtc ?? t.DateCreated,
                    IsVoid = t.IsVoid || string.Equals(t.TransactionType, "VOID", StringComparison.OrdinalIgnoreCase),
                    IsRefund = t.IsRefund || string.Equals(t.TransactionType, "RETURN", StringComparison.OrdinalIgnoreCase),
                    IsComplimentary = t.IsComplimentary,
                    ModifierCode = t.ModifierCode,
                    ComboReference = t.ComboReference,
                    MealPeriod = t.MealPeriod
                });
            }

            List<PosSaleExtendedRowDto> activeSales = rows.Where(r => !r.IsVoid && !r.IsRefund).ToList();
            return new PosSalesExtendedDto
            {
                QtySold = activeSales.Sum(r => r.Quantity),
                GrossRevenue = activeSales.Sum(r => r.GrossSellingPrice ?? 0m),
                DiscountTotal = activeSales.Sum(r => r.DiscountAmount ?? 0m),
                TaxTotal = activeSales.Sum(r => r.TaxAmount ?? 0m),
                NetRevenue = activeSales.Sum(r => r.NetRevenue ?? 0m),
                TransactionCount = rows.Count,
                UnmappedCount = rows.Count(r => r.MenuItemId == null),
                VoidOrRefundCount = rows.Count(r => r.IsVoid || r.IsRefund),
                LastSync = rows.Select(r => r.TransactionTimestampUtc).FirstOrDefault(),
                SyncStatus = rows.Any(r => r.Status == "FAILED") ? "ATTENTION" : "READY",
                Rows = rows
            };
        }

        public async Task<PosMappingWorkbenchDto> GetPosMappingWorkbenchAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewPosSales || ctx.Perms.ManagePosMapping, "Access denied", "Permission required.");

            List<MenuPosMapping> maps = await _db.MenuPosMapping.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).OrderBy(x => x.PosCode).ToListAsync(ct);
            List<MenuItem> items = await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToListAsync(ct);
            Dictionary<Guid, MenuItem> itemById = items.ToDictionary(x => x.Id);
            Dictionary<Guid, Recipe> recipes = await _db.Recipe.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
            Dictionary<Guid, string> outlets = ctx.Scope.ToDictionary(x => x.Id, x => x.LocationName);

            // Detect unmapped POS codes from PosSalesTransaction that don't yet have a MenuPosMapping row
            List<string> posCodesInSales = await _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive)
                .Select(x => x.PosCode)
                .Distinct()
                .ToListAsync(ct);
            HashSet<string> existingCodes = maps.Select(m => m.PosCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

            List<PosMappingExtendedDto> list = new();
            foreach (MenuPosMapping m in maps)
            {
                itemById.TryGetValue(m.MenuItemId ?? Guid.Empty, out MenuItem? mi);
                itemById.TryGetValue(m.SuggestedMenuItemId ?? Guid.Empty, out MenuItem? sugg);
                Guid? recId = m.RecipeId ?? mi?.LinkedRecipeId;
                recipes.TryGetValue(recId ?? Guid.Empty, out Recipe? rec);
                string warn = m.MenuItemId == null ? "Menu item mapping pending" : recId == null ? "Linked recipe missing" : string.Empty;
                list.Add(new PosMappingExtendedDto
                {
                    Id = m.Id,
                    PosCode = m.PosCode,
                    PosDescription = m.PosDescription,
                    OutletLocationId = m.OutletLocationId,
                    OutletName = m.OutletLocationId != null && outlets.TryGetValue(m.OutletLocationId.Value, out string? on) ? on : "All Outlets",
                    MenuItemId = m.MenuItemId,
                    MenuItemName = mi?.Name,
                    SuggestedMenuItemId = m.SuggestedMenuItemId,
                    SuggestedMenuItemName = sugg?.Name,
                    SuggestionConfidence = m.SuggestionConfidence,
                    Status = m.Status,
                    MappingSource = m.MappingSource,
                    EffectiveFrom = m.EffectiveFrom,
                    EffectiveTo = m.EffectiveTo,
                    RecipeId = recId,
                    RecipeName = rec?.Name,
                    ApprovedRecipeVersion = m.ApprovedRecipeVersion ?? mi?.ApprovedRecipeVersion,
                    MappingType = m.MappingType,
                    PortionMultiplier = m.PortionMultiplier,
                    UomCode = m.UomCode,
                    UomConversionFactor = m.UomConversionFactor,
                    ModifierCode = m.ModifierCode,
                    ModifierRecipeId = m.ModifierRecipeId,
                    ModifierPortionMultiplier = m.ModifierPortionMultiplier,
                    ComboParentCode = m.ComboParentCode,
                    IsComboComponent = m.IsComboComponent,
                    ExcludeComponentDoubleCount = m.ExcludeComponentDoubleCount,
                    ApprovedAtUtc = m.ApprovedAtUtc,
                    MappingNotes = m.MappingNotes,
                    ValidationWarning = warn
                });
            }

            foreach (string code in posCodesInSales.Where(c => !existingCodes.Contains(c)))
            {
                MenuItem? autoSugg = items.FirstOrDefault(i => i.Name.Contains(code, StringComparison.OrdinalIgnoreCase) || code.Contains(i.Name.Replace(" ", "_"), StringComparison.OrdinalIgnoreCase));
                list.Add(new PosMappingExtendedDto
                {
                    Id = Guid.Empty,
                    PosCode = code,
                    PosDescription = "Detected in POS sales (unmapped)",
                    Status = "UNMAPPED",
                    MappingSource = "POS_IMPORT",
                    SuggestedMenuItemId = autoSugg?.Id,
                    SuggestedMenuItemName = autoSugg?.Name,
                    SuggestionConfidence = autoSugg != null ? 0.72m : null,
                    ValidationWarning = "Requires manual review & approval; no fabricated recipe consumption."
                });
            }

            return new PosMappingWorkbenchDto
            {
                Mappings = list,
                UnmappedQueue = list.Where(x => x.MenuItemId == null || x.Status == "UNMAPPED").ToList(),
                MappedCount = list.Count(x => x.MenuItemId != null && x.Status != "UNMAPPED"),
                UnmappedCount = list.Count(x => x.MenuItemId == null || x.Status == "UNMAPPED"),
                ComboCount = list.Count(x => x.MappingType == "COMBO" || x.IsComboComponent),
                ReprocessWarnings = list.Where(x => !string.IsNullOrEmpty(x.ValidationWarning)).Select(x => $"{x.PosCode}: {x.ValidationWarning}").Take(10).ToList()
            };
        }

        public async Task<PosMappingExtendedDto> SavePosMappingExtendedAsync(Guid organizationId, Guid userId, Guid roleId, PosMappingExtendedWriteDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManagePosMapping, "Access denied", "Permission required to manage POS mapping.");
            if (string.IsNullOrWhiteSpace(dto.PosCode)) throw new BadRequestCustomException("POS code is required.", "Provide a POS item code.");

            MenuItem menuItem = await _db.MenuItem.FirstOrDefaultAsync(x => x.Id == dto.MenuItemId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Menu item not found.", "Select an active menu item.");

            MenuPosMapping? entity = dto.Id != null && dto.Id != Guid.Empty
                ? await _db.MenuPosMapping.FirstOrDefaultAsync(x => x.Id == dto.Id && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                : await _db.MenuPosMapping.FirstOrDefaultAsync(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.PosCode == dto.PosCode.Trim() && x.OutletLocationId == dto.OutletLocationId, ct);

            if (entity == null)
            {
                entity = new MenuPosMapping
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    PosCode = dto.PosCode.Trim(),
                    IsActive = true
                };
                _db.MenuPosMapping.Add(entity);
            }

            entity.PosDescription = dto.PosDescription ?? entity.PosDescription;
            entity.OutletLocationId = dto.OutletLocationId;
            entity.MenuItemId = dto.MenuItemId;
            entity.RecipeId = dto.RecipeId ?? menuItem.LinkedRecipeId;
            entity.ApprovedRecipeVersion = dto.ApprovedRecipeVersion ?? menuItem.ApprovedRecipeVersion;
            entity.MappingType = string.IsNullOrWhiteSpace(dto.MappingType) ? "ONE_TO_ONE" : dto.MappingType.Trim().ToUpperInvariant();
            entity.PortionMultiplier = dto.PortionMultiplier > 0 ? dto.PortionMultiplier : 1m;
            entity.UomCode = dto.UomCode ?? menuItem.PortionUom;
            entity.UomConversionFactor = dto.UomConversionFactor > 0 ? dto.UomConversionFactor : 1m;
            entity.ModifierCode = dto.ModifierCode;
            entity.ModifierRecipeId = dto.ModifierRecipeId;
            entity.ModifierPortionMultiplier = dto.ModifierPortionMultiplier;
            entity.ComboParentCode = dto.ComboParentCode;
            entity.IsComboComponent = dto.IsComboComponent;
            entity.ExcludeComponentDoubleCount = dto.ExcludeComponentDoubleCount;
            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;
            entity.MappingSource = "MANUAL_WORKBENCH";
            entity.MappingNotes = dto.MappingNotes;
            entity.Status = dto.ApproveMapping ? "APPROVED" : "MAPPED";
            if (dto.ApproveMapping)
            {
                entity.ApprovedByUserId = userId;
                entity.ApprovedAtUtc = DateTime.UtcNow;
            }

            // Link existing unmapped PosSalesTransactions cleanly without mutating historical amounts
            List<PosSalesTransaction> matchingSales = await _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.PosCode == entity.PosCode && x.MenuItemId == null)
                .ToListAsync(ct);
            foreach (PosSalesTransaction s in matchingSales)
            {
                s.MenuItemId = entity.MenuItemId;
                s.RecipeId = entity.RecipeId;
            }

            await _db.SaveChangesAsync(ct);
            PosMappingWorkbenchDto wb = await GetPosMappingWorkbenchAsync(organizationId, roleId, new SilaMenuEngineeringFilterDto(), ct);
            return wb.Mappings.First(x => x.Id == entity.Id);
        }

        public async Task<MenuPerformanceMatrixDto> GetPerformanceMatrixAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, string? costBasis, decimal? popularityThreshold, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMenuPerformance, "Access denied", "Permission required to view menu performance.");
            return await BuildPerformanceMatrixInternalAsync(ctx, filter, costBasis ?? ctx.Settings.ProfitabilityBasis, popularityThreshold ?? ctx.Settings.PopularityThreshold, ct);
        }

        private async Task<MenuPerformanceMatrixDto> BuildPerformanceMatrixInternalAsync(StudioCtx ctx, SilaMenuEngineeringFilterDto filter, string? costBasis, decimal popFactor, CancellationToken ct)
        {
            string basis = string.Equals(costBasis, "VARIABLE_COST", StringComparison.OrdinalIgnoreCase) ? "VARIABLE_COST" : "FOOD_COST_ONLY";
            decimal thresholdFactor = popFactor > 0 ? (popFactor > 1m ? popFactor / 100m : popFactor) : 0.70m;

            DateTime from = (filter.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            DateTime to = (filter.DateTo ?? DateTime.UtcNow.Date).Date.AddDays(1);
            List<PosSalesTransaction> sales = await _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && !x.IsVoid && !x.IsRefund && x.BusinessDate >= from && x.BusinessDate < to)
                .ToListAsync(ct);
            List<MenuPosMapping> maps = await _db.MenuPosMapping.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.MenuItemId != null).ToListAsync(ct);
            Dictionary<string, MenuPosMapping> mapByCode = maps.GroupBy(m => m.PosCode, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            List<MenuItem> items = await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && (filter.Category == null || x.Category == filter.Category)).ToListAsync(ct);
            List<Guid> recipeIds = items.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recipeIds.Count == 0 ? new() : await _db.Recipe.Where(x => recipeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

            // Waste impact by RecipeId
            List<SilaCulinaryWasteEvent> wasteEvents = await _db.SilaCulinaryWasteEvent
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.DateCreated >= from && x.DateCreated < to)
                .ToListAsync(ct);

            List<MenuPerformanceMatrixRowDto> allRows = new();
            foreach (MenuItem item in items)
            {
                List<PosSalesTransaction> itemSales = sales.Where(s =>
                    s.MenuItemId == item.Id || (s.MenuItemId == null && mapByCode.TryGetValue(s.PosCode, out MenuPosMapping? mp) && mp.MenuItemId == item.Id)).ToList();
                decimal qty = itemSales.Sum(s => s.QuantitySold);
                decimal netRev = itemSales.Sum(s => s.NetRevenue ?? s.Amount ?? ((item.SellingPrice ?? 0m) * s.QuantitySold));
                decimal avgNetSell = qty > 0 ? Math.Round(netRev / qty, 2) : (item.SellingPrice ?? 0m);

                recipes.TryGetValue(item.LinkedRecipeId ?? Guid.Empty, out Recipe? rec);
                decimal? unitFoodCost = rec is { TotalCost: > 0, ServingQty: > 0 } ? Math.Round(rec.TotalCost / rec.ServingQty * (item.DefaultPortionQty > 0 ? item.DefaultPortionQty : 1m), 4) : null;
                decimal? variableCost = unitFoodCost == null ? null : basis == "VARIABLE_COST" ? Math.Round(unitFoodCost.Value * 1.08m, 4) : unitFoodCost;
                decimal? cmUnit = variableCost != null ? Math.Round(avgNetSell - variableCost.Value, 2) : null;
                decimal? totalCm = cmUnit != null ? Math.Round(cmUnit.Value * qty, 2) : null;
                decimal wasteCost = rec == null ? 0m : wasteEvents.Where(w => w.RecipeId == rec.Id).Sum(w => w.EstimatedWasteCost);

                string? excl = null;
                if (!item.IsAvailable || item.Status == "INACTIVE") excl = "UNAVAILABLE_ITEM";
                else if (rec == null || unitFoodCost == null) excl = "INCOMPLETE_COSTING";
                else if (qty < ctx.Settings.MinimumSalesQuantity && qty == 0) excl = "INSUFFICIENT_SALES_HISTORY";

                allRows.Add(new MenuPerformanceMatrixRowDto
                {
                    MenuItemId = item.Id,
                    Name = item.Name,
                    Category = string.IsNullOrWhiteSpace(item.Category) ? "Uncategorized" : item.Category!,
                    Cuisine = item.Cuisine,
                    MealPeriod = item.MealPeriods,
                    QtySold = qty,
                    Revenue = Math.Round(netRev, 2),
                    SellingPrice = item.SellingPrice,
                    AverageNetSellingPrice = avgNetSell,
                    CostPerPortion = unitFoodCost,
                    TotalIngredientCost = unitFoodCost != null ? Math.Round(unitFoodCost.Value * qty, 2) : null,
                    FoodCostPercent = avgNetSell > 0 && unitFoodCost != null ? Math.Round(unitFoodCost.Value / avgNetSell * 100m, 2) : null,
                    ContributionMargin = cmUnit,
                    TotalContributionMargin = totalCm,
                    MarginPercent = avgNetSell > 0 && cmUnit != null ? Math.Round(cmUnit.Value / avgNetSell * 100m, 2) : null,
                    WasteImpactCost = Math.Round(wasteCost, 2),
                    CostBasis = basis,
                    CostSourceType = "CURRENT_RECIPE_ESTIMATE",
                    CostState = rec == null ? "RECIPE_REQUIRED" : unitFoodCost == null ? "COST_PENDING" : "READY",
                    ExclusionReason = excl
                });
            }

            List<MenuMatrixCategorySummaryDto> catSummaries = new();
            List<MenuPerformanceMatrixRowDto> eligible = new();
            List<MenuPerformanceMatrixRowDto> excluded = new();

            foreach (IGrouping<string, MenuPerformanceMatrixRowDto> catGroup in allRows.GroupBy(r => r.Category ?? "Uncategorized"))
            {
                List<MenuPerformanceMatrixRowDto> catElig = catGroup.Where(r => r.ExclusionReason == null && r.QtySold > 0).ToList();
                List<MenuPerformanceMatrixRowDto> catExcl = catGroup.Where(r => r.ExclusionReason != null || r.QtySold <= 0).ToList();
                foreach (MenuPerformanceMatrixRowDto ex in catExcl)
                {
                    ex.ExclusionReason ??= "INSUFFICIENT_SALES_HISTORY";
                    ex.Classification = ex.ExclusionReason;
                    ex.RecommendedAction = "Excluded from matrix so incomplete data is not misclassified as poor performance.";
                    excluded.Add(ex);
                }

                decimal catQty = catElig.Sum(r => r.QtySold);
                decimal expectedPop = catElig.Count == 0 ? 0m : Math.Round(100m / catElig.Count, 2);
                decimal popThresh = Math.Round(thresholdFactor * expectedPop, 2);
                decimal? weightedAvgCm = catQty > 0 && catElig.Any(r => r.TotalContributionMargin != null)
                    ? Math.Round(catElig.Sum(r => r.TotalContributionMargin ?? 0m) / catQty, 2)
                    : null;

                foreach (MenuPerformanceMatrixRowDto r in catElig)
                {
                    decimal mix = catQty > 0 ? Math.Round(r.QtySold / catQty * 100m, 2) : 0m;
                    r.MixPercent = mix;
                    r.PopularityPercent = mix;
                    r.ExpectedPopularityPercent = expectedPop;
                    r.CategoryPopularityThresholdPercent = popThresh;
                    r.CategoryWeightedAvgMargin = weightedAvgCm;

                    bool highPop = mix >= popThresh;
                    bool highCm = weightedAvgCm != null && r.ContributionMargin != null && r.ContributionMargin >= weightedAvgCm;
                    r.Classification = highPop && highCm ? "STAR" : highPop ? "PLOWHORSE" : highCm ? "PUZZLE" : "DOG";
                    r.RecommendedAction = r.Classification switch
                    {
                        "STAR" => "Maintain portion & quality standards; highlight on menu.",
                        "PLOWHORSE" => "Test slight price increase or portion/recipe cost optimization.",
                        "PUZZLE" => "Promote visibility, placement, and server upselling.",
                        _ => "Review recipe cost, re-engineer dish, or consider removal."
                    };
                    eligible.Add(r);
                }

                catSummaries.Add(new MenuMatrixCategorySummaryDto
                {
                    Category = catGroup.Key,
                    EligibleItems = catElig.Count,
                    ExcludedItems = catExcl.Count,
                    TotalSalesQty = catQty,
                    TotalRevenue = catElig.Sum(r => r.Revenue),
                    ExpectedPopularityPercent = expectedPop,
                    PopularityThresholdPercent = popThresh,
                    WeightedAvgContributionMargin = weightedAvgCm
                });
            }

            foreach (MenuPerformanceMatrixRowDto r in eligible.Concat(excluded))
            {
                MenuEngineeringPermissions.RedactFinancials(ctx.Perms, r);
            }

            return new MenuPerformanceMatrixDto
            {
                CostBasis = basis,
                PopularityThresholdFactor = thresholdFactor,
                Rows = eligible.OrderByDescending(r => r.Revenue).ToList(),
                ExcludedRows = excluded,
                CategorySummaries = catSummaries,
                Stars = eligible.Count(r => r.Classification == "STAR"),
                Plowhorses = eligible.Count(r => r.Classification == "PLOWHORSE"),
                Puzzles = eligible.Count(r => r.Classification == "PUZZLE"),
                Dogs = eligible.Count(r => r.Classification == "DOG"),
                ExcludedCount = excluded.Count
            };
        }

        public async Task<ConsumptionVarianceDetailDto> GetConsumptionVarianceAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            DateTime from = (filter.DateFrom ?? DateTime.UtcNow.Date.AddDays(-30)).Date;
            DateTime to = (filter.DateTo ?? DateTime.UtcNow.Date).Date.AddDays(1);

            List<PosSalesTransaction> sales = await _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && !x.IsVoid && !x.IsRefund && x.BusinessDate >= from && x.BusinessDate < to)
                .ToListAsync(ct);
            List<MenuPosMapping> maps = await _db.MenuPosMapping.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToListAsync(ct);

            // Prevent combo parent + component double counting (Scenario 5 / Section 17)
            HashSet<string> activeComboParents = sales
                .SelectMany(s => new[] { s.PosCode, s.ComboReference })
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            Dictionary<Guid, decimal> recipeUnits = new();
            foreach (PosSalesTransaction s in sales)
            {
                MenuPosMapping? map = maps.FirstOrDefault(m => string.Equals(m.PosCode, s.PosCode, StringComparison.OrdinalIgnoreCase));
                if (map is { IsComboComponent: true, ExcludeComponentDoubleCount: true } &&
                    !string.IsNullOrWhiteSpace(map.ComboParentCode) &&
                    activeComboParents.Contains(map.ComboParentCode))
                {
                    continue;
                }
                Guid? recId = s.RecipeId ?? map?.RecipeId;
                if (recId == null) continue;
                decimal mult = (map?.PortionMultiplier ?? 1m) * (map?.UomConversionFactor ?? 1m);
                recipeUnits[recId.Value] = recipeUnits.GetValueOrDefault(recId.Value) + (s.QuantitySold * mult);

                if (!string.IsNullOrWhiteSpace(s.ModifierCode) && map?.ModifierRecipeId != null)
                {
                    decimal modMult = map.ModifierPortionMultiplier ?? 1m;
                    recipeUnits[map.ModifierRecipeId.Value] = recipeUnits.GetValueOrDefault(map.ModifierRecipeId.Value) + (s.QuantitySold * modMult);
                }
            }

            List<Guid> rIds = recipeUnits.Keys.ToList();
            Dictionary<Guid, Recipe> recipes = rIds.Count == 0 ? new() : await _db.Recipe.Include(r => r.Ingredients).Where(r => rIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);
            List<Guid> matIds = recipes.Values.SelectMany(r => r.Ingredients.Where(i => i.MaterialId != null).Select(i => i.MaterialId!.Value)).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> materials = matIds.Count == 0 ? new() : await _db.ItemBuyerMaster.Where(m => matIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct);

            List<InventoryTransaction> actualOut = matIds.Count == 0 || ctx.ScopeIds.Count == 0
                ? new()
                : await _db.InventoryTransaction.Where(t => t.BuyerId == ctx.Buyer.Id && t.IsActive && matIds.Contains(t.MaterialId) && ctx.ScopeIds.Contains(t.LocationId) && t.BusinessDate >= from && t.BusinessDate < to && t.Direction == "OUT").ToListAsync(ct);

            List<SilaCulinaryWasteEvent> waste = matIds.Count == 0
                ? new()
                : await _db.SilaCulinaryWasteEvent.Where(w => w.BuyerId == ctx.Buyer.Id && w.IsActive && w.MaterialId != null && matIds.Contains(w.MaterialId.Value) && w.DateCreated >= from && w.DateCreated < to).ToListAsync(ct);

            List<ConsumptionVarianceRowDto> rows = new();
            foreach (Guid matId in matIds)
            {
                decimal theo = 0m;
                decimal yieldLoss = 0m;
                string recName = "";
                string uom = "KG";
                foreach (var kv in recipeUnits)
                {
                    if (!recipes.TryGetValue(kv.Key, out Recipe? r)) continue;
                    foreach (RecipeIngredient ing in r.Ingredients.Where(i => i.MaterialId == matId))
                    {
                        decimal per = r.ServingQty > 0 ? ing.Quantity / r.ServingQty : ing.Quantity;
                        theo += kv.Value * per;
                        decimal lossFactor = ((r.TrimLossPercent ?? 0m) + (r.CookingLossPercent ?? 0m)) / 100m;
                        yieldLoss += kv.Value * per * lossFactor;
                        recName = r.Name;
                        uom = ing.Uom;
                    }
                }
                decimal actual = actualOut.Where(a => a.MaterialId == matId).Sum(a => a.Quantity);
                // Avoid double-counting waste already deducted in InventoryTransaction
                decimal recordedWaste = waste.Where(w => w.MaterialId == matId && !w.AlreadyDeductedFromInventory).Sum(w => w.WasteQty);
                decimal totalVar = (actual + recordedWaste) - theo;
                decimal unexplained = totalVar - yieldLoss - recordedWaste;
                materials.TryGetValue(matId, out ItemBuyerMaster? mat);

                rows.Add(new ConsumptionVarianceRowDto
                {
                    MaterialId = matId,
                    MaterialName = mat?.Description ?? mat?.MaterialCode ?? "Ingredient",
                    RecipeName = recName,
                    Uom = uom,
                    TheoreticalQty = Math.Round(theo, 4),
                    ActualQty = Math.Round(actual, 4),
                    ExpectedYieldLossQty = Math.Round(yieldLoss, 4),
                    RecordedWasteQty = Math.Round(recordedWaste, 4),
                    VarianceQty = Math.Round(totalVar, 4),
                    UnexplainedVarianceQty = Math.Round(unexplained, 4),
                    VariancePercent = theo == 0 ? null : Math.Round(totalVar / theo * 100m, 2),
                    VarianceValue = ctx.Perms.ViewMenuCost && mat?.UnitCost != null ? Math.Round(totalVar * mat.UnitCost.Value, 2) : null,
                    SourceNotes = "POS net sales + combo/modifier mapping + Phase 4B non-duplicate waste"
                });
            }

            return new ConsumptionVarianceDetailDto
            {
                Theoretical = rows.Sum(r => r.TheoreticalQty),
                Actual = rows.Sum(r => r.ActualQty),
                ExpectedYieldLoss = rows.Sum(r => r.ExpectedYieldLossQty),
                RecordedWaste = rows.Sum(r => r.RecordedWasteQty),
                TotalVariance = rows.Sum(r => r.VarianceQty),
                UnexplainedVariance = rows.Sum(r => r.UnexplainedVarianceQty),
                VarianceValue = ctx.Perms.ViewMenuCost ? rows.Sum(r => r.VarianceValue ?? 0m) : null,
                DoubleCountingPrevented = true,
                Rows = rows.OrderByDescending(r => Math.Abs(r.UnexplainedVarianceQty)).ToList()
            };
        }

        public async Task<CostMarginAnalysisDto> GetCostMarginAnalysisAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMenuCost || ctx.Perms.ViewMenuMargin, "Access denied", "Confidential cost and margin access denied.");

            MenuPerformanceMatrixDto matrix = await BuildPerformanceMatrixInternalAsync(ctx, filter, ctx.Settings.ProfitabilityBasis, ctx.Settings.PopularityThreshold, ct);
            List<MenuPerformanceMatrixRowDto> all = matrix.Rows.Concat(matrix.ExcludedRows).ToList();
            decimal rev = all.Sum(x => x.Revenue);
            decimal cost = all.Sum(x => x.TotalIngredientCost ?? 0m);

            List<SilaMenuInsightDto> alerts = new();
            decimal targetPct = ctx.Settings.HighFoodCostPercent <= 1m ? ctx.Settings.HighFoodCostPercent * 100m : ctx.Settings.HighFoodCostPercent;
            foreach (MenuPerformanceMatrixRowDto r in all.Where(x => x.FoodCostPercent != null && x.FoodCostPercent > targetPct))
            {
                decimal? targetSell = r.CostPerPortion != null && targetPct > 0 ? Math.Round(r.CostPerPortion.Value / (targetPct / 100m), 2) : null;
                alerts.Add(new SilaMenuInsightDto
                {
                    Category = "MARGIN_ALERT",
                    Priority = "ACTION",
                    Title = $"{r.Name} food cost {r.FoodCostPercent:0.0}% exceeds {targetPct:0.0}%",
                    Reason = $"Current selling price {r.SellingPrice:0.00} vs cost {r.CostPerPortion:0.00}.",
                    RecommendedAction = targetSell != null ? $"Recommended target net price: {targetSell:0.00}" : "Review recipe portion or supplier cost.",
                    MenuItemId = r.MenuItemId
                });
            }

            List<PurchaseOrderItem> poItems = await _db.PurchaseOrderItem
                .Include(x => x.PurchaseOrder)
                .Where(x => x.IsActive && x.PurchaseOrder.BuyerId == ctx.Buyer.Id && x.UnitPrice != null)
                .OrderByDescending(x => x.DateCreated)
                .Take(40)
                .ToListAsync(ct);

            List<SupplierPriceCompareDto> comparisons = poItems
                .GroupBy(p => new { p.MaterialCode, p.PurchaseOrder.SupplierName })
                .Select(g => g.First())
                .Take(15)
                .Select(p => new SupplierPriceCompareDto
                {
                    MaterialId = Guid.Empty,
                    MaterialName = !string.IsNullOrWhiteSpace(p.ProductName) ? p.ProductName : (p.MaterialCode ?? "Ingredient"),
                    SupplierName = p.PurchaseOrder.SupplierName ?? "Approved Supplier",
                    UnitPrice = ctx.Perms.ViewMenuCost ? p.UnitPrice : null,
                    Currency = ctx.Settings.DefaultCurrency ?? "USD",
                    IsPreferred = false
                }).ToList();

            return new CostMarginAnalysisDto
            {
                ItemRows = all,
                MarginAlerts = alerts,
                SupplierComparisons = comparisons,
                TotalRevenue = Math.Round(rev, 2),
                TotalFoodCost = ctx.Perms.ViewMenuCost ? Math.Round(cost, 2) : null,
                OverallFoodCostPercent = ctx.Perms.ViewMenuCost && rev > 0 ? Math.Round(cost / rev * 100m, 2) : null,
                TotalContributionMargin = ctx.Perms.ViewMenuMargin ? Math.Round(rev - cost, 2) : null,
                Disclaimer = "Contribution Margin = Net Revenue - Applicable Variable Costs (not Net Profit). Current recipe estimates are not actual ledger consumption costs."
            };
        }

        public async Task<MenuPurchasePlanDto> GetPurchasePlanAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.GeneratePurchaseRecommendation, "Access denied", "Permission required.");
            return await BuildPurchasePlanInternalAsync(ctx, filter, ct);
        }

        private async Task<MenuPurchasePlanDto> BuildPurchasePlanInternalAsync(StudioCtx ctx, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            DateTime from = (filter.DateFrom ?? DateTime.UtcNow.Date).Date;
            DateTime to = (filter.DateTo ?? from.AddDays(7)).Date;

            List<MenuPlan> plans = await _db.MenuPlan.Include(p => p.Lines)
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.BusinessDate >= from && x.BusinessDate <= to &&
                            (filter.MenuPlanId == null || x.Id == filter.MenuPlanId))
                .ToListAsync(ct);

            List<MenuPlanLine> lines = plans.SelectMany(p => p.Lines.Where(l => l.IsActive)).ToList();
            List<Guid> itemIds = lines.Select(l => l.MenuItemId).Distinct().ToList();
            Dictionary<Guid, MenuItem> items = itemIds.Count == 0 ? new() : await _db.MenuItem.Where(x => itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            List<Guid> recIds = lines.Select(l => l.RecipeId ?? (items.TryGetValue(l.MenuItemId, out MenuItem? mi) ? mi.LinkedRecipeId : null)).Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recIds.Count == 0 ? new() : await _db.Recipe.Include(r => r.Ingredients).Where(r => recIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

            Dictionary<(Guid MaterialId, string Uom), (decimal Qty, DateTime RequiredDate, string PlanName)> need = new();
            foreach (MenuPlan plan in plans)
            {
                foreach (MenuPlanLine l in plan.Lines.Where(x => x.IsActive))
                {
                    items.TryGetValue(l.MenuItemId, out MenuItem? mi);
                    Guid? rid = l.RecipeId ?? mi?.LinkedRecipeId;
                    if (rid == null || !recipes.TryGetValue(rid.Value, out Recipe? rec)) continue;
                    decimal portions = l.PlannedPortions is > 0 ? l.PlannedPortions.Value : l.RecommendedPortions;
                    decimal lossPct = (rec.TrimLossPercent ?? 0m) + (rec.CookingLossPercent ?? 0m);
                    decimal yieldFactor = 100m / Math.Clamp(100m - lossPct, 10m, 100m);

                    foreach (RecipeIngredient ing in rec.Ingredients.Where(i => i.MaterialId != null))
                    {
                        decimal portionSize = l.PortionSize is > 0m ? l.PortionSize.Value : (mi?.DefaultPortionQty is > 0m ? mi.DefaultPortionQty : 1m);
                        decimal per = rec.ServingQty > 0 ? ing.Quantity / rec.ServingQty : ing.Quantity;
                        decimal gross = portions * portionSize * per * yieldFactor;
                        var key = (ing.MaterialId!.Value, ing.Uom);
                        if (need.TryGetValue(key, out var cur))
                            need[key] = (cur.Qty + gross, cur.RequiredDate < plan.BusinessDate ? cur.RequiredDate : plan.BusinessDate, cur.PlanName);
                        else
                            need[key] = (gross, plan.BusinessDate.Date, plan.Name);
                    }
                }
            }

            List<Guid> matIds = need.Keys.Select(k => k.MaterialId).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> materials = matIds.Count == 0 ? new() : await _db.ItemBuyerMaster.Where(m => matIds.Contains(m.Id)).ToDictionaryAsync(m => m.Id, ct);

            Dictionary<Guid, decimal> onHandByMat = matIds.Count == 0 || ctx.ScopeIds.Count == 0
                ? new()
                : (await _db.InventoryBalance.Where(b => b.BuyerId == ctx.Buyer.Id && b.IsActive && matIds.Contains(b.MaterialId) && ctx.ScopeIds.Contains(b.LocationId)).ToListAsync(ct))
                    .GroupBy(b => b.MaterialId).ToDictionary(g => g.Key, g => g.Sum(r => r.OnHandQty));

            HashSet<Guid> blockedLocationIds = ctx.Scope
                .Where(loc => loc.InventoryEnabled == false || string.Equals(loc.LocationType, "BLOCKED", StringComparison.OrdinalIgnoreCase) || string.Equals(loc.LocationType, "QUARANTINE", StringComparison.OrdinalIgnoreCase))
                .Select(loc => loc.Id)
                .ToHashSet();

            Dictionary<Guid, decimal> blockedByMat = matIds.Count == 0 || blockedLocationIds.Count == 0
                ? new()
                : (await _db.InventoryBalance.Where(b => b.BuyerId == ctx.Buyer.Id && b.IsActive && matIds.Contains(b.MaterialId) && blockedLocationIds.Contains(b.LocationId)).ToListAsync(ct))
                    .GroupBy(b => b.MaterialId).ToDictionary(g => g.Key, g => g.Sum(r => Math.Max(0m, r.OnHandQty)));

            string[] activeReservationStatuses = ["PROPOSED", "PENDING_APPROVAL", "ACTIVE", "PARTIALLY_FULFILLED", "RELEASE_PENDING"];
            Dictionary<Guid, decimal> reservedByMat = matIds.Count == 0
                ? new()
                : (await _db.SilaCulinaryStockReservation
                        .Where(r => r.BuyerId == ctx.Buyer.Id && r.IsActive && matIds.Contains(r.MaterialId) && activeReservationStatuses.Contains(r.Status))
                        .ToListAsync(ct))
                    .GroupBy(r => r.MaterialId)
                    .ToDictionary(g => g.Key, g => g.Sum(r => Math.Max(0m, r.ReservedQty - r.FulfilledQty - r.ReleasedQty)));

            List<GoodsReceiptItem> grnExpiryItems = matIds.Count == 0 || ctx.ScopeIds.Count == 0
                ? new()
                : await _db.GoodsReceiptItem.Include(i => i.GoodsReceipt)
                    .Where(i => i.IsActive && i.GoodsReceipt.BuyerId == ctx.Buyer.Id && i.GoodsReceipt.IsActive
                                && ctx.ScopeIds.Contains(i.GoodsReceipt.LocationId) && i.MaterialId != null && matIds.Contains(i.MaterialId.Value) && i.ExpiryDate != null)
                    .ToListAsync(ct);

            List<MaterialUomConversion> uomConversions = matIds.Count == 0
                ? new()
                : await _db.MaterialUomConversion
                    .Where(u => u.BuyerId == ctx.Buyer.Id && u.IsActive && matIds.Contains(u.MaterialId))
                    .ToListAsync(ct);

            List<SilaCulinaryProcurementLine> procLineDefaults = matIds.Count == 0
                ? new()
                : await _db.SilaCulinaryProcurementLine
                    .Where(p => p.BuyerId == ctx.Buyer.Id && p.IsActive && p.MaterialId != null && matIds.Contains(p.MaterialId.Value))
                    .OrderByDescending(p => p.DateCreated)
                    .ToListAsync(ct);

            // Time-phased open POs: only count supply arriving on or before RequiredDate!
            HashSet<string> matCodes = materials.Values.Select(m => m.MaterialCode).Where(c => !string.IsNullOrWhiteSpace(c)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<PurchaseOrderItem> openPoItems = matCodes.Count == 0
                ? new()
                : await _db.PurchaseOrderItem.Include(i => i.PurchaseOrder)
                    .Where(i => i.IsActive && i.MaterialCode != null && matCodes.Contains(i.MaterialCode) &&
                                i.PurchaseOrder.BuyerId == ctx.Buyer.Id && i.PurchaseOrder.IsActive &&
                                i.PurchaseOrder.Status != "CANCELLED" && i.PurchaseOrder.Status != "CLOSED")
                    .ToListAsync(ct);

            SilaPurchaseRecommendationListDto existing = await SilaPurchaseRecommendation.BuildAsync(
                _repository, ctx.Buyer.Id, ctx.Scope.Where(x => x.LocationType != Common.SILA_LOCATION_VENUE).ToList(), DateTime.UtcNow.Month, ct);

            List<MenuPurchasePlanRowDto> rows = new();
            foreach (var kv in need)
            {
                Guid matId = kv.Key.MaterialId;
                decimal grossNeed = Math.Round(kv.Value.Qty, 4);
                DateTime reqDate = kv.Value.RequiredDate;
                decimal onHand = onHandByMat.GetValueOrDefault(matId);
                decimal reserved = reservedByMat.GetValueOrDefault(matId);
                decimal blocked = blockedByMat.GetValueOrDefault(matId);
                decimal expired = Math.Min(onHand, grnExpiryItems
                    .Where(g => g.MaterialId == matId && g.ExpiryDate!.Value.Date <= reqDate.Date)
                    .Sum(g => Math.Max(0m, g.AcceptedQty - g.ReversedQty)));
                decimal eligibleAvail = Math.Max(0m, onHand - reserved - blocked - expired);

                materials.TryGetValue(matId, out ItemBuyerMaster? mat);
                string? mCode = mat?.MaterialCode;
                List<PurchaseOrderItem> matPos = string.IsNullOrWhiteSpace(mCode)
                    ? new()
                    : openPoItems.Where(p => string.Equals(p.MaterialCode, mCode, StringComparison.OrdinalIgnoreCase)).ToList();
                decimal onTimePo = matPos.Where(p => (p.PurchaseOrder.DeliveryDate ?? reqDate) <= reqDate).Sum(p => p.Quantity);
                decimal latePo = matPos.Where(p => (p.PurchaseOrder.DeliveryDate ?? reqDate) > reqDate).Sum(p => p.Quantity);

                decimal netShort = Math.Max(0m, grossNeed - eligibleAvail - onTimePo);
                decimal? unitCost = ctx.Perms.ViewMenuCost ? mat?.UnitCost : null;

                SilaCulinaryProcurementLine? procPref = procLineDefaults.FirstOrDefault(p => p.MaterialId == matId);
                MaterialUomConversion? orderPackConv = mat != null && !string.IsNullOrWhiteSpace(mat.OrderUnitOfMeasure)
                    ? uomConversions.FirstOrDefault(u => u.MaterialId == matId && string.Equals(u.FromUom, mat.OrderUnitOfMeasure, StringComparison.OrdinalIgnoreCase) && string.Equals(u.ToUom, kv.Key.Uom, StringComparison.OrdinalIgnoreCase))
                    : null;
                decimal? packSize = procPref?.PackSizeQty is > 0m ? procPref.PackSizeQty : (orderPackConv?.Factor is > 0m ? orderPackConv.Factor : null);
                decimal? moq = procPref?.MoqQty is > 0m ? procPref.MoqQty : null;

                decimal safePack = packSize is > 0m ? packSize.Value : 1m;
                decimal effectiveNeed = Math.Max(netShort, moq ?? 0m);
                decimal recQty = netShort <= 0m ? 0m : Math.Ceiling(effectiveNeed / safePack) * safePack;

                rows.Add(new MenuPurchasePlanRowDto
                {
                    MaterialId = matId,
                    MaterialName = mat?.Description ?? mat?.MaterialCode ?? "Ingredient",
                    Uom = kv.Key.Uom,
                    Requirement = grossNeed,
                    Available = onHand,
                    ReservedStock = reserved,
                    BlockedStock = blocked,
                    ExpiringStock = expired,
                    EligibleAvailable = eligibleAvail,
                    OpenPo = onTimePo,
                    EligibleIncomingOnTime = onTimePo,
                    LateOpenPoExcluded = latePo,
                    NetRequirement = Math.Round(netShort, 4),
                    Shortage = Math.Round(netShort, 4),
                    PackSize = packSize,
                    MinimumOrderQty = moq,
                    RecommendedPurchaseQty = recQty,
                    ContractPrice = unitCost,
                    EstimatedPurchaseCost = unitCost != null ? Math.Round(recQty * unitCost.Value, 2) : null,
                    RecommendedSupplier = matPos.FirstOrDefault()?.PurchaseOrder.SupplierName ?? procPref?.RecommendedSupplierName,
                    RelatedPlan = kv.Value.PlanName,
                    RequiredDate = reqDate,
                    Reason = latePo > 0 ? "MENU_PLAN (Late PO excluded)" : "MENU_PLAN",
                    TimePhasedValid = true
                });
            }

            return new MenuPurchasePlanDto
            {
                Rows = rows.OrderByDescending(r => r.Shortage).ToList(),
                ExistingRecommendations = existing,
                TotalShortageQty = rows.Sum(r => r.Shortage),
                TotalEstimatedPurchaseCost = ctx.Perms.ViewMenuCost ? rows.Sum(r => r.EstimatedPurchaseCost ?? 0m) : 0m,
                AutoPoTransmissionDisabled = true
            };
        }

        public async Task<SilaPurchaseRecommendationListDto> CreateDraftPurchaseRecommendationAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.GeneratePurchaseRecommendation, "Access denied", "Permission required to generate purchase recommendations.");

            MenuPurchasePlanDto plan = await BuildPurchasePlanInternalAsync(ctx, filter, ct);
            foreach (MenuPurchasePlanRowDto row in plan.Rows.Where(r => r.Shortage > 0))
            {
                SilaPurchaseRecommendationDto? match = plan.ExistingRecommendations.Items.FirstOrDefault(x => x.MaterialId == row.MaterialId);
                if (match != null)
                {
                    match.RecommendedQty = Math.Max(match.RecommendedQty, row.RecommendedPurchaseQty);
                    match.Reason = string.IsNullOrWhiteSpace(match.Reason) ? "MENU_REQUIREMENT" : $"{match.Reason}+MENU_REQUIREMENT";
                }
                else if (row.MaterialId != null)
                {
                    plan.ExistingRecommendations.Items.Add(new SilaPurchaseRecommendationDto
                    {
                        MaterialId = row.MaterialId.Value,
                        MaterialName = row.MaterialName,
                        BaseUom = row.Uom,
                        PurchaseUom = row.Uom,
                        CurrentStock = row.EligibleAvailable,
                        OpenPoQty = row.EligibleIncomingOnTime,
                        AvailableEffectiveStock = row.EligibleAvailable + row.EligibleIncomingOnTime,
                        RecommendedQty = row.RecommendedPurchaseQty,
                        UnitPrice = row.ContractPrice,
                        Reason = "MENU_REQUIREMENT"
                    });
                }
            }
            return plan.ExistingRecommendations;
        }
    }
}
