using System.Text.Json;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Shared.MenuEngineering.Ai
{
    public partial class MenuEngineeringAiOrchestratorService
    {
        public async Task<List<MeRecommendationCockpitItemDto>> GetCockpitRecommendationsAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, string? status, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            List<MenuEngineeringRecommendation> list = await _db.MenuEngineeringRecommendation
                .Where(r => r.BuyerId == ctx.Buyer.Id && r.IsActive &&
                            (propertyId == null || r.PropertyId == null || r.PropertyId == propertyId) &&
                            (string.IsNullOrWhiteSpace(status) || r.Status == status))
                .OrderByDescending(r => r.DateCreated)
                .Take(100)
                .ToListAsync(ct);

            if (list.Count == 0)
            {
                // Seed deterministic cockpit recommendations from live AI Menu Intelligence without faking data
                AiMenuIntelligenceDto liveAi = await _studio.GetAiMenuIntelligenceAsync(organizationId, roleId, null, propertyId, ct);
                foreach (AiMenuRecommendationCardDto ins in liveAi.Recommendations.Take(8))
                {
                    MenuEngineeringRecommendation rec = new()
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = ctx.Buyer.Id,
                        RecommendationCode = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpperInvariant()}",
                        AgentId = "MENU_OPTIMIZATION_AGENT",
                        Category = ins.Strategy,
                        PropertyId = propertyId,
                        TargetObjectType = ins.TargetMenuItemId != null ? "MENU_ITEM" : "MENU_PLAN",
                        TargetObjectId = ins.TargetMenuItemId ?? ins.SuggestedRecipeId,
                        Title = ins.Recommendation,
                        CurrentBaseline = ins.CurrentBaseline,
                        ProposedChange = ins.ProposedChange,
                        SupportingEvidence = ins.SupportingEvidence,
                        EstimatedCostImpact = ctx.Perms.ViewMenuCost ? ins.EstimatedCostImpact : null,
                        EstimatedMarginImpact = ctx.Perms.ViewMenuMargin ? ins.EstimatedMarginImpact : null,
                        Confidence = ins.Confidence > 0 ? ins.Confidence : 0.82m,
                        Priority = "HIGH",
                        RequiresApproval = ins.RequiresApproval,
                        Status = "NEW",
                        DeduplicationKey = $"SEED-{ins.Strategy}-{ins.TargetMenuItemId ?? ins.SuggestedRecipeId}-{ins.Recommendation.GetHashCode():X}",
                        IsActive = true,
                        DateCreated = DateTime.UtcNow
                    };
                    _db.MenuEngineeringRecommendation.Add(rec);
                    list.Add(rec);
                }
                if (list.Count > 0) await _db.SaveChangesAsync(ct);
            }

            return list.Select(r => MapRecommendation(r, ctx.Perms)).ToList();
        }

        public async Task<MeRecommendationCockpitItemDto> TransitionCockpitRecommendationAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid recommendationId, MeRecommendationLifecycleActionDto action, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            MenuEngineeringRecommendation? rec = await _db.MenuEngineeringRecommendation
                .FirstOrDefaultAsync(r => r.Id == recommendationId && r.BuyerId == ctx.Buyer.Id && r.IsActive, ct);
            if (rec == null)
            {
                throw new NotFoundCustomException("Recommendation not found", $"Recommendation {recommendationId} not found.");
            }

            string act = (action.Action ?? "ACCEPT").Trim().ToUpperInvariant();
            switch (act)
            {
                case "REVIEW":
                    rec.Status = "UNDER_REVIEW";
                    break;
                case "ACCEPT":
                    rec.Status = "ACCEPTED";
                    break;
                case "REJECT":
                    rec.Status = "REJECTED";
                    break;
                case "APPROVE":
                    MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ApproveMenu, "Approval denied", "APPROVE_SILA_MENU permission required to approve recommendations.");
                    rec.Status = "APPROVED";
                    break;
                case "IMPLEMENT":
                    MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.EditDraftMenu || ctx.Perms.ApproveMenu, "Access denied", "Edit/Approval permission required.");
                    rec.Status = "IMPLEMENTED";
                    break;
                case "VALIDATE_OUTCOME":
                    rec.Status = "OUTCOME_VALIDATED";
                    rec.RealizedFinancialImpact = action.RealizedFinancialImpact;
                    rec.ValidatedAtUtc = DateTime.UtcNow;
                    rec.RealizedOutcomeJson = JsonSerializer.Serialize(action, JsonOpts);
                    // Capture validated actual outcome for continuous isolated tenant learning
                    _db.MenuEngineeringLearningOutcome.Add(new MenuEngineeringLearningOutcome
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = ctx.Buyer.Id,
                        DatasetVersion = $"ds-{DateTime.UtcNow:yyyyMMdd}",
                        ModelVersion = "me-hybrid-v2",
                        MenuPlanId = rec.TargetObjectType == "MENU_PLAN" ? rec.TargetObjectId : null,
                        MenuItemId = rec.TargetObjectType == "MENU_ITEM" ? rec.TargetObjectId : null,
                        RecommendationId = rec.Id,
                        ActualFoodCostPercent = action.ActualFoodCostPercent,
                        ActualCovers = action.ActualCovers,
                        ActualMargin = action.ActualMargin,
                        ActualWastePercent = action.ActualWastePercent,
                        OutcomeValidated = true,
                        OutcomeSource = "VALIDATED_RECOMMENDATION_OUTCOME",
                        CapturedAtUtc = DateTime.UtcNow,
                        IsActive = true
                    });
                    break;
                case "CLOSE":
                    rec.Status = "CLOSED";
                    break;
                case "ASSIGN":
                    rec.ResponsibleUserId = action.ResponsibleUserId;
                    break;
                case "EDIT_PROPOSAL":
                    rec.EditedProposalJson = action.EditedProposalJson;
                    if (!string.IsNullOrWhiteSpace(action.Notes)) rec.ProposedChange = SanitizeText(action.Notes);
                    break;
            }

            if (action.FeedbackRating != null) rec.FeedbackRating = action.FeedbackRating;
            if (!string.IsNullOrWhiteSpace(action.Notes)) rec.FeedbackNotes = SanitizeText(action.Notes);
            rec.DateUpdated = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return MapRecommendation(rec, ctx.Perms);
        }

        private static MeRecommendationCockpitItemDto MapRecommendation(MenuEngineeringRecommendation r, MenuEngineeringPermissionSetDto perms) => new()
        {
            Id = r.Id,
            RecommendationCode = r.RecommendationCode,
            AgentId = r.AgentId,
            Category = r.Category,
            PropertyId = r.PropertyId,
            OutletLocationId = r.OutletLocationId,
            TargetObjectType = r.TargetObjectType,
            TargetObjectId = r.TargetObjectId,
            TargetObjectLabel = r.TargetObjectLabel,
            Title = r.Title,
            CurrentBaseline = r.CurrentBaseline,
            ProposedChange = r.ProposedChange,
            SupportingEvidence = r.SupportingEvidence,
            EstimatedCostImpact = perms.ViewMenuCost ? r.EstimatedCostImpact : null,
            EstimatedMarginImpact = perms.ViewMenuMargin ? r.EstimatedMarginImpact : null,
            RealizedFinancialImpact = (perms.ViewMenuCost || perms.ViewMenuMargin) ? r.RealizedFinancialImpact : null,
            OperationalImpact = r.OperationalImpact,
            DataQuality = r.DataQuality,
            Confidence = r.Confidence,
            Priority = r.Priority,
            ResponsibleUserId = r.ResponsibleUserId,
            RequiresApproval = r.RequiresApproval,
            Status = r.Status,
            FeedbackRating = r.FeedbackRating,
            FeedbackNotes = r.FeedbackNotes,
            CreatedAtUtc = r.DateCreated
        };

        public async Task<List<MeProactiveAlertDto>> RunProactiveScanAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            int cooldownMins = Math.Max(5, ctx.Settings.ProactiveCooldownMinutes);
            DateTime now = DateTime.UtcNow;
            DateTime cooldownUntil = now.AddMinutes(cooldownMins);

            List<MenuEngineeringProactiveAlert> activeAlerts = await _db.MenuEngineeringProactiveAlert
                .Where(a => a.BuyerId == ctx.Buyer.Id && a.IsActive && a.CooldownUntilUtc > now)
                .ToListAsync(ct);

            List<MenuEngineeringProactiveAlert> created = new();

            async Task TryEmitAlertAsync(string triggerType, string dedupKey, string severity, string title, string summary, string? objType, Guid? objId, string recAction, string agentId, decimal? estImpact)
            {
                if (activeAlerts.Any(a => a.DeduplicationKey == dedupKey)) return;

                MenuEngineeringProactiveAlert alert = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    TriggerType = triggerType,
                    DeduplicationKey = dedupKey,
                    Severity = severity,
                    Title = title,
                    Summary = summary,
                    PropertyId = propertyId,
                    AffectedObjectType = objType,
                    AffectedObjectId = objId,
                    RecommendedAction = recAction,
                    Status = "OPEN",
                    CooldownUntilUtc = cooldownUntil,
                    IsActive = true,
                    DateCreated = now
                };
                _db.MenuEngineeringProactiveAlert.Add(alert);
                created.Add(alert);

                // Automatically link into Cockpit Recommendations with deduplication
                bool recExists = await _db.MenuEngineeringRecommendation
                    .AnyAsync(r => r.BuyerId == ctx.Buyer.Id && r.IsActive && r.DeduplicationKey == dedupKey && r.CooldownUntilUtc > now, ct);
                if (!recExists)
                {
                    _db.MenuEngineeringRecommendation.Add(new MenuEngineeringRecommendation
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = ctx.Buyer.Id,
                        RecommendationCode = $"PRO-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                        AgentId = agentId,
                        Category = triggerType,
                        PropertyId = propertyId,
                        TargetObjectType = objType,
                        TargetObjectId = objId,
                        Title = title,
                        CurrentBaseline = summary,
                        ProposedChange = recAction,
                        SupportingEvidence = $"Proactive deterministic rule trigger ({triggerType})",
                        EstimatedCostImpact = estImpact,
                        Priority = severity == "CRITICAL" ? "HIGH" : "MEDIUM",
                        Confidence = 0.85m,
                        RequiresApproval = true,
                        Status = "NEW",
                        DeduplicationKey = dedupKey,
                        CooldownUntilUtc = cooldownUntil,
                        IsActive = true,
                        DateCreated = now
                    });
                }
            }

            // 1. Margin Deterioration Trigger (deterministic threshold scan before LLM)
            decimal targetFc = ctx.Settings.HighFoodCostPercent <= 1m ? ctx.Settings.HighFoodCostPercent * 100m : ctx.Settings.HighFoodCostPercent;
            List<MenuItem> items = await _db.MenuItem.Where(i => i.BuyerId == ctx.Buyer.Id && i.IsActive && i.LinkedRecipeId != null && i.SellingPrice > 0).ToListAsync(ct);
            List<Guid> recIds = items.Select(i => i.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recIds.Count == 0 ? new() : await _db.Recipe.Where(r => recIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

            foreach (MenuItem mi in items)
            {
                if (!recipes.TryGetValue(mi.LinkedRecipeId!.Value, out Recipe? rec) || rec.ServingQty <= 0) continue;
                decimal unitCost = rec.TotalCost / rec.ServingQty * (mi.DefaultPortionQty > 0 ? mi.DefaultPortionQty : 1m);
                decimal fcPct = Math.Round(unitCost / mi.SellingPrice!.Value * 100m, 1);
                if (fcPct > targetFc)
                {
                    await TryEmitAlertAsync(
                        "MARGIN_DETERIORATION",
                        $"MARGIN-{mi.Id}",
                        fcPct > targetFc + 8m ? "CRITICAL" : "WARNING",
                        $"Margin deterioration risk: {mi.Name} food cost at {fcPct:0.0}%",
                        $"Current food cost {fcPct:0.0}% exceeds threshold {targetFc:0.0}% (unit cost {unitCost:0.00} vs price {mi.SellingPrice:0.00}).",
                        "MENU_ITEM",
                        mi.Id,
                        "Review portion size, recipe substitution, or submit controlled price adjustment.",
                        "COST_MARGIN_AGENT",
                        Math.Round(unitCost - (mi.SellingPrice.Value * targetFc / 100m), 2));
                }
            }

            // 2. Unmapped High-Volume POS Codes
            List<PosSalesTransaction> unmapped = await _db.PosSalesTransaction
                .Where(s => s.BuyerId == ctx.Buyer.Id && s.IsActive && !s.IsVoid && !s.IsRefund && s.MenuItemId == null)
                .ToListAsync(ct);
            foreach (var grp in unmapped.GroupBy(u => u.PosCode).Where(g => g.Sum(x => x.QuantitySold) >= 10))
            {
                await TryEmitAlertAsync(
                    "POS_UNMAPPED",
                    $"POS-UNMAPPED-{grp.Key}",
                    "WARNING",
                    $"Unmapped POS item {grp.Key} ({grp.Sum(x => x.QuantitySold):0} sold)",
                    "Sales are accumulating without a confirmed Menu Item mapping, affecting consumption accuracy.",
                    "POS_MAPPING",
                    null,
                    "Open POS Mapping AI Review Queue to confirm match.",
                    "POS_MAPPING_AGENT",
                    null);
            }

            // 3. Expiry-managed items on hand
            List<ItemBuyerMaster> expItems = await _db.ItemBuyerMaster
                .Where(m => m.BuyerId == ctx.Buyer.Id && m.IsActive && m.ExpiryManaged)
                .Take(5)
                .ToListAsync(ct);
            foreach (ItemBuyerMaster em in expItems)
            {
                decimal stock = ctx.ScopeIds.Count == 0 ? 0m : await _db.InventoryBalance
                    .Where(b => b.BuyerId == ctx.Buyer.Id && b.IsActive && b.MaterialId == em.Id && ctx.ScopeIds.Contains(b.LocationId))
                    .SumAsync(b => b.OnHandQty, ct);
                if (stock > 0)
                {
                    await TryEmitAlertAsync(
                        "INVENTORY_EXPIRY_RISK",
                        $"EXPIRY-{em.Id}",
                        "INFO",
                        $"Inventory utilization opportunity: {em.Description} ({stock:0.##} on hand)",
                        "Expiry-managed stock is available; prioritize in upcoming menu cycle or buffet specials.",
                        "MATERIAL",
                        em.Id,
                        "Run Inventory-Optimized Menu Planning to utilize stock before expiry.",
                        "MENU_PLANNING_AGENT",
                        null);
                }
            }

            if (created.Count > 0) await _db.SaveChangesAsync(ct);
            return await GetProactiveAlertsAsync(organizationId, roleId, propertyId, ct);
        }

        public async Task<List<MeProactiveAlertDto>> GetProactiveAlertsAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            List<MenuEngineeringProactiveAlert> rows = await _db.MenuEngineeringProactiveAlert
                .Where(a => a.BuyerId == ctx.Buyer.Id && a.IsActive && (propertyId == null || a.PropertyId == null || a.PropertyId == propertyId))
                .OrderByDescending(a => a.DateCreated)
                .Take(50)
                .ToListAsync(ct);

            return rows.Select(a => new MeProactiveAlertDto
            {
                Id = a.Id,
                TriggerType = a.TriggerType,
                Severity = a.Severity,
                Title = a.Title,
                Summary = ctx.Perms.ViewMenuCost ? a.Summary : a.TriggerType == "MARGIN_DETERIORATION" ? "Margin alert (cost details restricted by RBAC)" : a.Summary,
                AffectedObjectType = a.AffectedObjectType,
                AffectedObjectId = a.AffectedObjectId,
                RecommendedAction = a.RecommendedAction,
                Status = a.Status,
                CreatedAtUtc = a.DateCreated,
                CooldownUntilUtc = a.CooldownUntilUtc
            }).ToList();
        }

        public async Task<MeProactiveAlertDto> AcknowledgeAlertAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid alertId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            MenuEngineeringProactiveAlert? alert = await _db.MenuEngineeringProactiveAlert
                .FirstOrDefaultAsync(a => a.Id == alertId && a.BuyerId == ctx.Buyer.Id && a.IsActive, ct);
            if (alert == null) throw new NotFoundCustomException("Alert not found", $"Alert {alertId} not found.");

            alert.Status = "ACKNOWLEDGED";
            alert.AcknowledgedByUserId = userId;
            alert.AcknowledgedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new MeProactiveAlertDto
            {
                Id = alert.Id,
                TriggerType = alert.TriggerType,
                Severity = alert.Severity,
                Title = alert.Title,
                Summary = alert.Summary,
                AffectedObjectType = alert.AffectedObjectType,
                AffectedObjectId = alert.AffectedObjectId,
                RecommendedAction = alert.RecommendedAction,
                Status = alert.Status,
                CreatedAtUtc = alert.DateCreated,
                CooldownUntilUtc = alert.CooldownUntilUtc
            };
        }

        public async Task<MeSetupWizardReadinessDto> GetSetupWizardReadinessAsync(
            Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManageMenuSetup, "Access denied", "Permission required.");

            int properties = await _db.BuyerProperty.CountAsync(p => p.BuyerId == ctx.Buyer.Id && p.IsActive, ct);
            int outlets = ctx.Scope.Count(l => l.SalesEnabled || l.LocationType == Buyer.Domain.Common.Common.SILA_LOCATION_OUTLET || l.LocationType == Buyer.Domain.Common.Common.SILA_LOCATION_VENUE);
            int kitchens = ctx.Scope.Count(l => l.ConsumptionEnabled != false && !string.Equals(l.LocationType, Buyer.Domain.Common.Common.SILA_LOCATION_VENUE, StringComparison.OrdinalIgnoreCase));
            int totalIngredients = await _db.ItemBuyerMaster.CountAsync(i => i.BuyerId == ctx.Buyer.Id && i.IsActive, ct);
            int activeSuppliers = await _db.SilaSupplier.CountAsync(s => s.BuyerId == ctx.Buyer.Id && s.IsActive, ct);
            int totalRecipes = await _db.Recipe.CountAsync(r => r.BuyerId == ctx.Buyer.Id && r.IsActive, ct);
            int approvedRecipes = await _db.Recipe.CountAsync(r => r.BuyerId == ctx.Buyer.Id && r.IsActive && r.Status == "APPROVED", ct);
            int menuItems = await _db.MenuItem.CountAsync(m => m.BuyerId == ctx.Buyer.Id && m.IsActive, ct);
            int costedItems = await _db.ItemBuyerMaster.CountAsync(i => i.BuyerId == ctx.Buyer.Id && i.IsActive && i.UnitCost > 0, ct);
            int posTxns = await _db.PosSalesTransaction.CountAsync(s => s.BuyerId == ctx.Buyer.Id && s.IsActive, ct);
            int totalMaps = await _db.MenuPosMapping.CountAsync(m => m.BuyerId == ctx.Buyer.Id && m.IsActive, ct);
            int approvedMaps = await _db.MenuPosMapping.CountAsync(m => m.BuyerId == ctx.Buyer.Id && m.IsActive && m.MenuItemId != null, ct);
            int pmsSnaps = await _db.PmsDemandSnapshot.CountAsync(p => p.BuyerId == ctx.Buyer.Id && p.IsActive, ct);
            int invBalances = await _db.InventoryBalance.CountAsync(b => b.BuyerId == ctx.Buyer.Id && b.IsActive && b.OnHandQty > 0, ct);
            int evalCount = await _db.MenuEngineeringModelEvaluation.CountAsync(e => e.BuyerId == ctx.Buyer.Id && e.IsActive, ct);
            int activeAgents = await _db.MenuEngineeringAgentRun.CountAsync(a => a.BuyerId == ctx.Buyer.Id && a.IsActive, ct);

            decimal pct(int num, int den) => den == 0 ? (num > 0 ? 100m : 0m) : Math.Min(100m, Math.Round(100m * num / den, 1));
            string st(decimal p) => p >= 80m ? "READY" : p >= 35m ? "NEEDS_ATTENTION" : "BLOCKED";

            bool aiLive = IsAiProviderEnabled(ctx);
            bool notifReady = ctx.Settings.TeamsNotificationsEnabled || !string.IsNullOrWhiteSpace(ctx.Settings.EventScheduleConfigJson);

            List<MeSetupWizardStepDto> steps = new()
            {
                new() { StepNumber = 1, StepName = "Customer Configuration", Category = "TENANT", ReadinessPercent = 100m, Status = "READY", Detail = $"Buyer tenant '{ctx.Buyer.OrganizationName}' configured (Currency: {ctx.Settings.DefaultCurrency})." },
                new() { StepNumber = 2, StepName = "Property Configuration", Category = "TENANT", ReadinessPercent = properties > 0 ? 100m : 0m, Status = properties > 0 ? "READY" : "BLOCKED", Detail = $"{properties} active property/properties.", RequiredAction = properties > 0 ? null : "Configure at least one BuyerProperty." },
                new() { StepNumber = 3, StepName = "Outlet Configuration", Category = "TENANT", ReadinessPercent = outlets > 0 ? 100m : 35m, Status = outlets > 0 ? "READY" : "NEEDS_ATTENTION", Detail = $"{outlets} outlet location(s) authorized.", RequiredAction = outlets > 0 ? null : "Assign outlet locations under Property Setup." },
                new() { StepNumber = 4, StepName = "Kitchen Configuration", Category = "TENANT", ReadinessPercent = kitchens > 0 ? 100m : 0m, Status = kitchens > 0 ? "READY" : "BLOCKED", Detail = $"{kitchens} kitchen/production location(s) configured.", RequiredAction = kitchens > 0 ? null : "Add at least one kitchen/store location." },
                new() { StepNumber = 5, StepName = "Ingredient Master Readiness", Category = "MASTER_DATA", ReadinessPercent = totalIngredients > 0 ? 100m : 0m, Status = totalIngredients > 0 ? "READY" : "BLOCKED", Detail = $"{totalIngredients} active ingredients in ItemBuyerMaster.", RequiredAction = totalIngredients > 0 ? null : "Import or create ingredients via Customer Data Migration." },
                new() { StepNumber = 6, StepName = "Supplier Master Readiness", Category = "MASTER_DATA", ReadinessPercent = activeSuppliers > 0 ? 100m : 40m, Status = activeSuppliers > 0 ? "READY" : "NEEDS_ATTENTION", Detail = $"{activeSuppliers} active approved supplier(s).", RequiredAction = activeSuppliers > 0 ? null : "Import approved suppliers before generating POs." },
                new() { StepNumber = 7, StepName = "Recipe Master Readiness", Category = "MASTER_DATA", ReadinessPercent = pct(approvedRecipes, Math.Max(1, totalRecipes)), Status = st(pct(approvedRecipes, Math.Max(1, totalRecipes))), Detail = $"{approvedRecipes} approved / {totalRecipes} total recipes.", RequiredAction = approvedRecipes > 0 ? null : "Approve at least one recipe before publishing menus." },
                new() { StepNumber = 8, StepName = "Menu Item Readiness", Category = "MASTER_DATA", ReadinessPercent = menuItems > 0 ? 100m : 20m, Status = menuItems > 0 ? "READY" : "NEEDS_ATTENTION", Detail = $"{menuItems} active menu items in catalog.", RequiredAction = menuItems > 0 ? null : "Create or import menu items." },
                new() { StepNumber = 9, StepName = "Ingredient Costing Readiness", Category = "FINANCE", ReadinessPercent = totalIngredients > 0 ? pct(costedItems, totalIngredients) : 0m, Status = totalIngredients > 0 ? st(pct(costedItems, totalIngredients)) : "BLOCKED", Detail = $"{costedItems} / {totalIngredients} ingredients have unit costs.", RequiredAction = costedItems > 0 ? null : "Populate ingredient unit costs for accurate food cost %." },
                new() { StepNumber = 10, StepName = "POS Configuration", Category = "INTEGRATION", IsOptional = true, ReadinessPercent = !string.IsNullOrWhiteSpace(ctx.Settings.PosSyncScheduleCron) ? 100m : 50m, Status = !string.IsNullOrWhiteSpace(ctx.Settings.PosSyncScheduleCron) ? "READY" : "NEEDS_ATTENTION", Detail = $"POS sync schedule: {ctx.Settings.PosSyncScheduleCron} (max retries {ctx.Settings.PosRetryMaxAttempts})." },
                new() { StepNumber = 11, StepName = "POS Mapping Readiness", Category = "INTEGRATION", IsOptional = true, ReadinessPercent = totalMaps == 0 ? 60m : pct(approvedMaps, totalMaps), Status = totalMaps == 0 ? "NEEDS_ATTENTION" : st(pct(approvedMaps, totalMaps)), Detail = $"{approvedMaps} mapped / {totalMaps} POS codes.", RequiredAction = approvedMaps == totalMaps ? null : "Review unmapped POS codes in POS Mapping." },
                new() { StepNumber = 12, StepName = "PMS Configuration (Optional)", Category = "INTEGRATION", IsOptional = true, ReadinessPercent = pmsSnaps > 0 ? 100m : 75m, Status = pmsSnaps > 0 ? "READY" : "READY", Detail = pmsSnaps > 0 ? $"{pmsSnaps} OPERA PMS occupancy snapshot(s) active." : "Optional PMS integration not required for standalone restaurant operations." },
                new() { StepNumber = 13, StepName = "Inventory Configuration", Category = "OPERATIONS", ReadinessPercent = invBalances > 0 ? 100m : 45m, Status = invBalances > 0 ? "READY" : "NEEDS_ATTENTION", Detail = $"{invBalances} stocked inventory balance rows available.", RequiredAction = invBalances > 0 ? null : "Record opening stock counts or GRN receipts." },
                new() { StepNumber = 14, StepName = "Historical Data Readiness", Category = "ANALYTICS", IsOptional = true, ReadinessPercent = posTxns >= 15 ? 100m : posTxns > 0 ? 65m : 40m, Status = posTxns >= 15 ? "READY" : "NEEDS_ATTENTION", Detail = $"{posTxns} historical POS sales observations available." },
                new() { StepNumber = 15, StepName = "Forecasting Configuration", Category = "ANALYTICS", ReadinessPercent = evalCount > 0 || posTxns >= 5 ? 100m : 65m, Status = evalCount > 0 || posTxns >= 5 ? "READY" : "NEEDS_ATTENTION", Detail = $"Method: {ctx.Settings.ForecastMethod}, Horizon: {ctx.Settings.ForecastHorizonDays}d, Threshold: {ctx.Settings.ForecastAccuracyThresholdPercent:0}%." },
                new() { StepNumber = 16, StepName = "AI Provider Configuration", Category = "AI_GOVERNANCE", IsOptional = true, ReadinessPercent = aiLive ? 100m : 70m, Status = aiLive ? "READY" : "NEEDS_ATTENTION", Detail = $"Provider mode: {ctx.AiConfig?.OperatingMode ?? "LOCAL"} (Manual deterministic fallback always active)." },
                new() { StepNumber = 17, StepName = "Agent Enablement", Category = "AI_GOVERNANCE", ReadinessPercent = !ctx.Settings.AutonomousModePaused ? 100m : 60m, Status = !ctx.Settings.AutonomousModePaused ? "READY" : "NEEDS_ATTENTION", Detail = $"Autonomous mode {(ctx.Settings.AutonomousModePaused ? "PAUSED" : "ACTIVE")}; {activeAgents} recorded agent run(s)." },
                new() { StepNumber = 18, StepName = "Approval Configuration", Category = "GOVERNANCE", ReadinessPercent = ctx.Settings.RequireApprovalOnPublish ? 100m : 80m, Status = "READY", Detail = $"Publish approval={(ctx.Settings.RequireApprovalOnPublish ? "Required" : "Optional")}, FC trigger={ctx.Settings.ApprovalFoodCostThresholdPercent:0}%." },
                new() { StepNumber = 19, StepName = "Notification Configuration", Category = "GOVERNANCE", IsOptional = true, ReadinessPercent = notifReady ? 100m : 80m, Status = "READY", Detail = $"Teams alerts={(ctx.Settings.TeamsNotificationsEnabled ? "Enabled" : "In-app cockpit only")}." },
            };

            int blockedCore = steps.Count(s => !s.IsOptional && s.Status == "BLOCKED");
            decimal coreAvg = Math.Round(steps.Where(s => !s.IsOptional).Average(s => s.ReadinessPercent), 1);
            steps.Add(new()
            {
                StepNumber = 20,
                StepName = "Production Readiness Assessment",
                Category = "GO_LIVE",
                ReadinessPercent = blockedCore > 0 ? Math.Min(65m, coreAvg) : coreAvg,
                Status = blockedCore > 0 ? "BLOCKED" : coreAvg >= 80m ? "READY" : "NEEDS_ATTENTION",
                Detail = blockedCore > 0 ? $"{blockedCore} mandatory core step(s) blocked before go-live." : $"All mandatory core checks passed ({coreAvg:0.0}% average readiness).",
                RequiredAction = blockedCore > 0 ? "Resolve blocked mandatory steps 1-9, 13, 15, 17, 18." : null
            });

            List<MeAiSuggestedThresholdDto> suggestions = new()
            {
                new() { SettingKey = "TargetFoodCostPercent", CurrentValue = $"{ctx.Settings.TargetFoodCostPercent:0.0}%", SuggestedValue = "28.0%", Rationale = "Hospitality benchmark for multi-outlet dining with active waste monitoring." },
                new() { SettingKey = "HighFoodCostPercent", CurrentValue = $"{ctx.Settings.HighFoodCostPercent:0.0}%", SuggestedValue = "32.0%", Rationale = "Triggers proactive margin deterioration alerts before erosion compounds." },
                new() { SettingKey = "MinimumSalesQuantity", CurrentValue = $"{ctx.Settings.MinimumSalesQuantity:0}", SuggestedValue = posTxns >= 30 ? "10" : "3", Rationale = "Calibrated to observed POS transaction volume to prevent matrix over-exclusion." },
                new() { SettingKey = "ProactiveCooldownMinutes", CurrentValue = $"{ctx.Settings.ProactiveCooldownMinutes}", SuggestedValue = "60", Rationale = "Balances timely alerts with deduplication cooldown." }
            };

            return new MeSetupWizardReadinessDto
            {
                OverallReadinessScore = Math.Round(steps.Average(s => s.ReadinessPercent), 1),
                TotalSteps = steps.Count,
                CompletedCount = steps.Count(s => s.Status == "READY"),
                PendingCount = steps.Count(s => s.Status == "NEEDS_ATTENTION"),
                BlockedCount = steps.Count(s => s.Status == "BLOCKED"),
                Steps = steps,
                SuggestedThresholds = suggestions,
                ValidationErrors = steps.Where(s => s.Status == "BLOCKED").Select(s => $"Step {s.StepNumber} ({s.StepName}): {s.Detail}").ToList(),
                RequiredActions = steps.Where(s => !string.IsNullOrWhiteSpace(s.RequiredAction)).Select(s => $"Step {s.StepNumber}: {s.RequiredAction}").ToList(),
                AdminApprovalRequiredToApply = true
            };
        }

        public async Task<MeLearningSummaryDto> GetLearningSummaryAsync(
            Guid organizationId, Guid roleId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, null, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            // Strictly isolated by ctx.Buyer.Id!
            List<MenuEngineeringLearningOutcome> outcomes = await _db.MenuEngineeringLearningOutcome
                .Where(o => o.BuyerId == ctx.Buyer.Id && o.IsActive && o.OutcomeValidated)
                .OrderByDescending(o => o.CapturedAtUtc)
                .Take(40)
                .ToListAsync(ct);

            List<MenuEngineeringModelEvaluation> evals = await _db.MenuEngineeringModelEvaluation
                .Where(e => e.BuyerId == ctx.Buyer.Id && e.IsActive)
                .OrderByDescending(e => e.EvaluatedAtUtc)
                .Take(10)
                .ToListAsync(ct);

            return new MeLearningSummaryDto
            {
                ActiveDatasetVersion = outcomes.FirstOrDefault()?.DatasetVersion ?? "ds-v1",
                ActiveModelVersion = evals.FirstOrDefault(e => e.IsActiveModel)?.ModelVersion ?? "me-hybrid-v2",
                TotalValidatedOutcomes = outcomes.Count,
                AvgForecastErrorPercent = evals.FirstOrDefault(e => e.IsActiveModel)?.MetricWape,
                AvgRealizedMarginDelta = ctx.Perms.ViewMenuMargin && outcomes.Any(o => o.ActualMargin != null && o.PredictedMargin != null)
                    ? Math.Round(outcomes.Where(o => o.ActualMargin != null && o.PredictedMargin != null).Average(o => o.ActualMargin!.Value - o.PredictedMargin!.Value), 2)
                    : null,
                ModelEvaluations = evals.Select(e => new AiDemandModelMetricDto
                {
                    ModelName = e.ModelName,
                    ModelVersion = e.ModelVersion,
                    Mae = e.MetricMae,
                    Wape = e.MetricWape,
                    Bias = e.MetricBias,
                    IntervalCoveragePercent = e.IntervalCoveragePercent,
                    Selected = e.IsActiveModel
                }).ToList(),
                RecentOutcomes = outcomes.Select(o => new MeLearningOutcomeRowDto
                {
                    Id = o.Id,
                    DatasetVersion = o.DatasetVersion,
                    ModelVersion = o.ModelVersion,
                    MenuPlanId = o.MenuPlanId,
                    RecommendationId = o.RecommendationId,
                    PredictedFoodCostPercent = ctx.Perms.ViewMenuCost ? o.PredictedFoodCostPercent : null,
                    ActualFoodCostPercent = ctx.Perms.ViewMenuCost ? o.ActualFoodCostPercent : null,
                    PredictedMargin = ctx.Perms.ViewMenuMargin ? o.PredictedMargin : null,
                    ActualMargin = ctx.Perms.ViewMenuMargin ? o.ActualMargin : null,
                    OutcomeValidated = o.OutcomeValidated,
                    OutcomeSource = o.OutcomeSource,
                    CapturedAtUtc = o.CapturedAtUtc
                }).ToList()
            };
        }

        public async Task<SilaStoreRoleIntelligenceDto> GetSilaStoreRoleIntelligenceAsync(
            Guid organizationId, Guid userId, Guid roleId, string rolePersona, Guid? propertyId, CancellationToken ct)
        {
            AiContext ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");

            string persona = (rolePersona ?? "EXECUTIVE_CHEF").Trim().ToUpperInvariant();
            List<MeProactiveAlertDto> alerts = await GetProactiveAlertsAsync(organizationId, roleId, propertyId, ct);
            List<MeRecommendationCockpitItemDto> recs = await GetCockpitRecommendationsAsync(organizationId, roleId, propertyId, null, ct);

            List<string> highlights = new();
            List<string> quickActions = new();

            MeWastePredictResultDto? wasteSummary = null;
            MeHierarchicalForecastResultDto? demandSummary = null;
            if (_twin is not null)
            {
                try
                {
                    wasteSummary = await _twin.PredictFoodWasteAsync(organizationId, roleId, propertyId, null, ct);
                    demandSummary = await _twin.ForecastHierarchicalDemandAsync(organizationId, roleId, propertyId, 3, ct);
                }
                catch { /* Keep role intelligence resilient */ }
            }

            int smallerBatchDishes = wasteSummary?.Items.Count(i => i.Recommendations.Any(r => r.Contains("batch", StringComparison.OrdinalIgnoreCase))) ?? 0;
            decimal tomorrowCovers = demandSummary?.Nodes.FirstOrDefault(n => n.Level == "PROPERTY")?.ReconciledForecast.FirstOrDefault() ?? 0m;

            switch (persona)
            {
                case "STOREKEEPER":
                    highlights.Add($"{alerts.Count(a => a.TriggerType == "INVENTORY_EXPIRY_RISK")} expiry utilization opportunities available (FEFO priority).");
                    highlights.Add(tomorrowCovers > 0m
                        ? $"Predicted tomorrow demand: {tomorrowCovers:F0} covers — verify ingredient shortage buffer before shift."
                        : "Review predicted ingredient requirements & shortage buffer before today's shift.");
                    quickActions = new() { "View Predicted Shortages", "Check Expiry Priority Stock", "Review Stock Transfers" };
                    break;
                case "COST_CONTROLLER":
                    highlights.Add($"{alerts.Count(a => a.TriggerType == "MARGIN_DETERIORATION")} food cost / margin alerts pending review.");
                    highlights.Add($"{recs.Count(r => r.Status == "NEW" || r.Status == "UNDER_REVIEW")} AI recommendations & scenario summaries awaiting approval.");
                    quickActions = new() { "Review Margin Risks", "Inspect Consumption Anomalies", "Approve Recommendations" };
                    break;
                case "FB_MANAGER":
                    highlights.Add(tomorrowCovers > 0m
                        ? $"Digital Twin Demand Forecast: {tomorrowCovers:F0} expected guest covers tomorrow; Menu Engineering Matrix updated."
                        : "Menu Engineering Matrix & 7-day backtested demand forecast updated.");
                    highlights.Add($"{alerts.Count(a => a.TriggerType == "POS_UNMAPPED")} POS mapping exceptions impacting sales intelligence.");
                    quickActions = new() { "Inspect Menu Performance", "View Demand Forecast", "Review Pricing Ranges" };
                    break;
                default: // EXECUTIVE_CHEF
                    persona = "EXECUTIVE_CHEF";
                    highlights.Add(smallerBatchDishes > 0
                        ? $"Waste Twin recommends preparing {smallerBatchDishes} dishes in smaller batches to curb overproduction."
                        : "Today's service menus & AI portion buffers prepared.");
                    highlights.Add($"{recs.Count(r => r.Priority == "HIGH")} high-priority culinary optimizations ready for review.");
                    quickActions = new() { "Open Today's Menu", "Optimize Portions", "Check Production Readiness" };
                    break;
            }

            List<MenuEngineeringActionItem> pendingActions = await _db.MenuEngineeringActionItem.AsNoTracking()
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.ApprovalStatus == "PENDING")
                .OrderBy(x => x.SequenceOrder)
                .Take(8)
                .ToListAsync(ct);

            MenuEngineeringExecutiveBriefing? latestBriefing = await _db.MenuEngineeringExecutiveBriefing.AsNoTracking()
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId || x.PropertyId == null))
                .OrderByDescending(x => x.BusinessDateUtc)
                .FirstOrDefaultAsync(ct);

            ExecutiveBriefingDto? briefingDto = latestBriefing is null
                ? new ExecutiveBriefingDto
                {
                    Persona = persona,
                    PropertyId = propertyId,
                    BusinessDateUtc = DateTime.UtcNow.Date,
                    Headline = $"{persona.Replace("_", " ")} Daily Intelligence Briefing",
                    KeyHighlights = highlights,
                    DeliveryChannel = "SILA_STORE",
                    DeliveryStatus = "LIVE"
                }
                : new ExecutiveBriefingDto
                {
                    Id = latestBriefing.Id,
                    Persona = latestBriefing.Persona,
                    PropertyId = latestBriefing.PropertyId,
                    BusinessDateUtc = latestBriefing.BusinessDateUtc,
                    Headline = $"{latestBriefing.Persona.Replace("_", " ")} Daily Intelligence Briefing",
                    KeyHighlights = highlights,
                    DeliveryChannel = "SILA_STORE",
                    DeliveryStatus = latestBriefing.DeliveryStatus
                };

            List<ActionItemDto> pendingApprovalDtos = pendingActions.Select(a => new ActionItemDto
            {
                Id = a.Id,
                ActionPlanId = a.ActionPlanId,
                SequenceOrder = a.SequenceOrder,
                DependsOnActionId = a.DependsOnActionId,
                AgentId = a.AgentId,
                ToolId = a.ToolId,
                ActionType = a.ActionType,
                Title = a.Title,
                RiskClassification = a.RiskClassification,
                PolicyDecision = a.PolicyDecision,
                PolicyReason = a.PolicyReason,
                FinancialImpact = ctx.Perms.ViewMenuMargin ? a.FinancialImpact : 0m,
                TargetObjectType = a.TargetObjectType,
                TargetObjectId = a.TargetObjectId,
                TargetObjectVersion = a.TargetObjectVersion,
                ApprovalStatus = a.ApprovalStatus,
                ApprovalChannel = a.ApprovalChannel,
                ApprovedAtUtc = a.ApprovedAtUtc,
                ExecutionStatus = a.ExecutionStatus,
                ExecutedAtUtc = a.ExecutedAtUtc,
                ExternalReference = a.ExternalReference,
                ResultSummary = a.ResultJson
            }).ToList();

            List<string> assignedTasks = pendingActions.Select(a => $"[{a.RiskClassification}] {a.Title} ({a.ApprovalStatus})").ToList();
            if (assignedTasks.Count == 0 && alerts.Count > 0)
            {
                assignedTasks = alerts.Take(4).Select(a => $"Review {a.Severity} alert: {a.Title}").ToList();
            }

            return new SilaStoreRoleIntelligenceDto
            {
                RolePersona = persona,
                RoleAlerts = alerts.Take(6).ToList(),
                RoleRecommendations = recs.Take(6).ToList(),
                Highlights = highlights,
                QuickActions = quickActions,
                DailyBriefing = briefingDto,
                PendingApprovals = pendingApprovalDtos,
                AssignedTasks = assignedTasks
            };
        }
    }
}
