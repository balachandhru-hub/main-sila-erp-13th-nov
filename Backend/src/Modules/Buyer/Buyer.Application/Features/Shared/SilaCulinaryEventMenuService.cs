using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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
    public interface ISilaCulinaryEventMenuService
    {
        Task<SilaCulinaryEventDashboardDto> GetDashboardAsync(Guid organizationId, Guid userId, Guid roleId, CancellationToken cancellationToken);
        Task<List<SilaCulinaryEventDto>> ListEventsAsync(Guid organizationId, Guid userId, Guid roleId, string? status, Guid? propertyId, DateTime? fromUtc, DateTime? toUtc, string? search, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> GetEventAsync(Guid organizationId, Guid userId, Guid roleId, Guid eventId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> CreateEventAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryUpsertEventRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> UpdateEventAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryUpsertEventRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> DuplicateEventAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> CancelEventAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string? reason, CancellationToken cancellationToken);

        // Templates (§10)
        Task<List<SilaCulinaryEventTemplateDto>> ListTemplatesAsync(Guid organizationId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventTemplateDto> CreateTemplateFromEventAsync(Guid organizationId, Guid userId, Guid roleId, Guid eventId, string templateName, string? description, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> CreateEventFromTemplateAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid templateId, SilaCulinaryUpsertEventRequestDto overrides, CancellationToken cancellationToken);

        // Multi-Day & Multi-Session (§11)
        Task<SilaCulinaryEventDto> UpsertSessionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryUpsertSessionRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> DeleteSessionAsync(Guid organizationId, Guid userId, Guid roleId, Guid eventId, Guid sessionId, CancellationToken cancellationToken);

        // Menu Studio & Composition (§13 - §16, §25)
        Task<List<SilaCulinaryMenuDto>> ListMenusAsync(Guid organizationId, Guid userId, Guid roleId, Guid? eventId, bool? reusableOnly, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> GetMenuAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuId, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> CreateMenuAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateMenuRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> UpsertMenuLineAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid menuId, SilaCulinaryUpsertMenuLineRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> RemoveMenuLineAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuId, Guid lineId, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> ReorderMenuLinesAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuId, List<Guid> orderedLineIds, CancellationToken cancellationToken);
        Task<List<SilaCulinaryPublishedRecipeSearchItemDto>> SearchPublishedRecipesAsync(Guid organizationId, Guid roleId, string? query, string? category, string? cuisine, string? dietary, string? excludeAllergens, decimal? maxCost, Guid? propertyId, CancellationToken cancellationToken);

        // Missing Recipe Handover (§20)
        Task<SilaCulinaryMenuDto> CreateMissingRecipeDraftAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid menuId, Guid lineId, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> SyncPublishedRecipeToMenuLineAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuId, Guid lineId, CancellationToken cancellationToken);

        // AI Menu Generation & Constraint Optimization (§17 - §19, §22, §36)
        Task<SilaCulinaryAiMenuGenerateResultDto> GenerateAiMenuAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryAiMenuGenerateRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuOptimizeResultDto> OptimizeEventMenuAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryMenuOptimizeRequestDto request, CancellationToken cancellationToken);

        // Dietary, Allergens, Material Preview & Read-Only Stock (§23, §24, §30, §31)
        Task<SilaCulinaryEventDto> VerifyDietaryReviewAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, bool verified, CancellationToken cancellationToken);
        Task<List<SilaCulinaryAllergenMatrixRowDto>> GetAllergenMatrixAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken);
        Task<List<SilaCulinaryMaterialPreviewLineDto>> GetPreliminaryMaterialPreviewAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken);

        // Simulation & Scenarios (§34, §35)
        Task<SilaCulinaryEventSimulationResultDto> SimulateEventAsync(Guid organizationId, Guid roleId, Guid eventId, SilaCulinaryEventSimulationRequestDto request, CancellationToken cancellationToken);
        Task<SilaCulinaryMenuDto> CreateScenarioAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string scenarioName, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> SelectScenarioAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, Guid scenarioMenuId, CancellationToken cancellationToken);

        // Customer Preview, Readiness, Approvals, Revisions, Reports & Phase 2B Handover (§37, §41 - §47, §57)
        Task<SilaCulinaryCustomerMenuPreviewDto> GetCustomerMenuPreviewAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventReadinessDto> GetEventReadinessAsync(Guid organizationId, Guid eventId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> SubmitEventForApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> DecideEventApprovalAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string decision, string? comment, CancellationToken cancellationToken);
        Task<SilaCulinaryEventDto> CreateRevisionAsync(Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string changeReason, CancellationToken cancellationToken);
        Task<List<SilaCulinaryEventRevisionDto>> ListRevisionsAsync(Guid organizationId, Guid roleId, Guid eventId, CancellationToken cancellationToken);
        Task<SilaCulinaryEventReportResultDto> ExportReportAsync(Guid organizationId, Guid roleId, Guid eventId, string reportCode, CancellationToken cancellationToken);
        Task<SilaCulinaryPhase2BHandoverDto> GetPhase2BHandoverAsync(Guid organizationId, Guid roleId, Guid eventId, CancellationToken cancellationToken);
    }

    public class SilaCulinaryEventMenuService : ISilaCulinaryEventMenuService
    {
        private static readonly JsonSerializerOptions CamelJson = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions SnakeJson = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, PropertyNameCaseInsensitive = true };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IIntegrationCredentialProtector _protector;

        public SilaCulinaryEventMenuService(
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

        private async Task<BuyerBusinessProfile> ResolveBuyerAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile? buyer = await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == organizationId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (buyer == null)
            {
                throw new NotFoundCustomException("Buyer not found.", "Buyer business profile not found for this organization.");
            }
            return buyer;
        }

        private static bool CanViewCosts(Guid roleId) => SilaAccess.HasFullAccess(roleId);

        public async Task<SilaCulinaryEventDashboardDto> GetDashboardAsync(
            Guid organizationId, Guid userId, Guid roleId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costVisible = CanViewCosts(roleId);
            List<SilaCulinaryEvent> events = await _repository.SilaCulinaryEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderBy(x => x.StartDateUtc)
                .ToListAsync(cancellationToken);

            List<Guid> menuIds = events.Where(e => e.PrimaryMenuId.HasValue).Select(e => e.PrimaryMenuId!.Value).Distinct().ToList();
            Dictionary<Guid, SilaCulinaryMenu> menus = menuIds.Count == 0
                ? new Dictionary<Guid, SilaCulinaryMenu>()
                : await _repository.SilaCulinaryMenu
                    .FindByCondition(x => x.BuyerId == buyer.Id && menuIds.Contains(x.Id) && x.IsActive)
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

            Dictionary<Guid, int> lineCounts = menuIds.Count == 0
                ? new Dictionary<Guid, int>()
                : await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && menuIds.Contains(x.MenuId) && x.IsActive)
                    .GroupBy(x => x.MenuId)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

            DateTime now = DateTime.UtcNow;
            DateTime weekEnd = now.AddDays(7);
            DateTime monthEnd = now.AddDays(30);

            SilaCulinaryEventDashboardDto dto = new();
            foreach (SilaCulinaryEvent ev in events)
            {
                if (ev.Status != "CANCELLED" && ev.EndDateUtc >= now.Date)
                {
                    dto.UpcomingEvents++;
                    if (ev.StartDateUtc <= weekEnd) dto.EventsThisWeek++;
                    if (ev.StartDateUtc <= monthEnd) dto.EventsThisMonth++;
                    dto.TotalPlannedGuests += ev.PlanningGuests;
                }
                if (ev.Status == "DRAFT") dto.DraftEvents++;
                if (ev.Status == "PENDING_APPROVAL") dto.AwaitingApprovalEvents++;
                if (ev.Status is "APPROVED" or "READY_FOR_MRP") dto.ApprovedEvents++;
                if (ev.Status == "READY_FOR_MRP") dto.ReadyForMrpEvents++;
                if (!ev.DietaryReviewVerified && !string.IsNullOrWhiteSpace(ev.DietaryRequirementsJson) && ev.DietaryRequirementsJson != "[]")
                {
                    dto.DietaryReviewPendingEvents++;
                }

                dto.EventsByType[ev.EventType] = dto.EventsByType.GetValueOrDefault(ev.EventType) + 1;
                dto.EventsByStatus[ev.Status] = dto.EventsByStatus.GetValueOrDefault(ev.Status) + 1;

                bool hasLines = ev.PrimaryMenuId.HasValue && lineCounts.GetValueOrDefault(ev.PrimaryMenuId.Value) > 0;
                if (!hasLines && ev.Status != "CANCELLED") dto.IncompleteMenuEvents++;

                decimal? estCost = ev.PrimaryMenuId.HasValue && menus.TryGetValue(ev.PrimaryMenuId.Value, out SilaCulinaryMenu? m)
                    ? m.TotalEstimatedCost
                    : null;
                decimal? budget = ev.CombinedMenuBudget ?? ev.ApprovedBudget;
                if (estCost.HasValue && budget.HasValue && estCost.Value > budget.Value)
                {
                    dto.AboveBudgetEvents++;
                }

                dto.BudgetVsCost.Add(new SilaCulinaryEventDashboardChartPointDto
                {
                    EventCode = ev.EventCode,
                    EventName = ev.Name,
                    Budget = costVisible ? budget : null,
                    EstimatedCost = costVisible ? estCost : null,
                    PlanningGuests = ev.PlanningGuests
                });
            }

            return dto;
        }

        public async Task<List<SilaCulinaryEventDto>> ListEventsAsync(
            Guid organizationId, Guid userId, Guid roleId, string? status, Guid? propertyId, DateTime? fromUtc, DateTime? toUtc, string? search, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryEvent> q = _repository.SilaCulinaryEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);

            if (!string.IsNullOrWhiteSpace(status))
            {
                string st = status.Trim().ToUpperInvariant();
                q = q.Where(x => x.Status == st);
            }
            if (propertyId.HasValue) q = q.Where(x => x.PropertyId == propertyId.Value);
            if (fromUtc.HasValue) q = q.Where(x => x.EndDateUtc >= fromUtc.Value);
            if (toUtc.HasValue) q = q.Where(x => x.StartDateUtc <= toUtc.Value);
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                q = q.Where(x => x.EventCode.ToLower().Contains(s) || x.Name.ToLower().Contains(s) || (x.VenueName != null && x.VenueName.ToLower().Contains(s)));
            }

            List<SilaCulinaryEvent> rows = await q.OrderBy(x => x.StartDateUtc).Take(200).ToListAsync(cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            List<SilaCulinaryEventDto> result = new(rows.Count);
            foreach (SilaCulinaryEvent ev in rows)
            {
                result.Add(await BuildEventDtoAsync(buyer.Id, ev, rows, costVisible, includeDetails: false, cancellationToken));
            }
            return result;
        }

        public async Task<SilaCulinaryEventDto> GetEventAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            List<SilaCulinaryEvent> allActive = await _repository.SilaCulinaryEvent
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Status != "CANCELLED")
                .ToListAsync(cancellationToken);
            return await BuildEventDtoAsync(buyer.Id, ev, allActive, CanViewCosts(roleId), includeDetails: true, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> CreateEventAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryUpsertEventRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new BadRequestCustomException("Invalid event request.", "Event name is required.");
            }
            if (request.EndDateUtc < request.StartDateUtc)
            {
                throw new BadRequestCustomException("Invalid event schedule.", "Event end date cannot precede start date.");
            }
            if (request.GuaranteedGuests < 0 || (request.MaxCapacity.HasValue && request.GuaranteedGuests > request.MaxCapacity.Value))
            {
                throw new BadRequestCustomException("Capacity exceeded.", "Guaranteed guests cannot exceed maximum event capacity.");
            }

            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string eventCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.EVENT, 6, cancellationToken);
            string menuCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.CULINARY_MENU, 6, cancellationToken);

            Guid eventId = Guid.NewGuid();
            Guid menuId = Guid.NewGuid();

            SilaCulinaryEvent ev = new()
            {
                Id = eventId,
                BuyerId = buyer.Id,
                EventCode = eventCode,
                Status = "DRAFT",
                RevisionNumber = 1,
                PrimaryMenuId = menuId,
                CorrelationId = Guid.NewGuid().ToString("N")[..12],
                IsActive = true
            };
            ApplyEventFields(ev, request);
            RecalculateGuestAndBudgetMetrics(ev);

            SilaCulinaryMenu menu = new()
            {
                Id = menuId,
                BuyerId = buyer.Id,
                MenuCode = menuCode,
                EventId = eventId,
                Name = $"{ev.Name} — Primary Menu",
                MenuCategory = "Event Banquet",
                PropertyId = ev.PropertyId,
                OutletLocationId = ev.OutletLocationId,
                ServiceStyle = ev.ServiceStyle,
                VersionNumber = 1,
                EffectiveDateUtc = ev.StartDateUtc,
                Status = "DRAFT",
                IsReusableTemplate = false,
                IsScenario = false,
                Currency = ev.Currency,
                TargetSellingPrice = ev.NetPricePerGuest > 0 ? ev.NetPricePerGuest : null,
                TargetFoodCostPercent = ev.TargetFoodCostPercent,
                IsActive = true
            };

            _repository.SilaCulinaryEvent.Create(ev);
            _repository.SilaCulinaryMenu.Create(menu);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_CREATED,
                $"Created event {ev.EventCode} ({ev.Name}) for {ev.PlanningGuests} planning guests.", userId);
            await _repository.SaveAsync();

            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> UpdateEventAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryUpsertEventRequestDto request, CancellationToken cancellationToken)
        {
            if (request.EndDateUtc < request.StartDateUtc)
            {
                throw new BadRequestCustomException("Invalid event schedule.", "Event end date cannot precede start date.");
            }
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);

            if (ev.Status is "APPROVED" or "READY_FOR_MRP")
            {
                throw new BadRequestCustomException(
                    "Approved event baseline is locked.",
                    "Create a new event revision via Change Management before modifying an approved event.");
            }

            ApplyEventFields(ev, request);
            RecalculateGuestAndBudgetMetrics(ev);
            if (ev.Status == "DRAFT" && ev.WizardStepCompleted >= 2)
            {
                ev.Status = "PLANNING_IN_PROGRESS";
            }

            if (ev.PrimaryMenuId.HasValue)
            {
                SilaCulinaryMenu? primary = await _repository.SilaCulinaryMenu
                    .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.Id == ev.PrimaryMenuId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (primary != null)
                {
                    await RecalculateMenuTotalsInternalAsync(buyer.Id, ev, primary, cancellationToken);
                }
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_UPDATED,
                $"Updated event {ev.EventCode} (Rev {ev.RevisionNumber}, Planning Guests: {ev.PlanningGuests}).", userId);
            await _repository.SaveAsync();

            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> DuplicateEventAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent src = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);

            string newCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.EVENT, 6, cancellationToken);
            string newMenuCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.CULINARY_MENU, 6, cancellationToken);

            Guid newEventId = Guid.NewGuid();
            Guid newMenuId = Guid.NewGuid();

            SilaCulinaryEvent copy = new()
            {
                Id = newEventId,
                BuyerId = buyer.Id,
                EventCode = newCode,
                Name = $"{src.Name} (Copy)",
                Description = src.Description,
                EventType = src.EventType,
                EventCategory = src.EventCategory,
                CustomerReference = src.CustomerReference,
                CustomerName = src.CustomerName,
                EventCoordinator = src.EventCoordinator,
                ResponsibleChef = src.ResponsibleChef,
                ExecutiveChef = src.ExecutiveChef,
                BanquetManager = src.BanquetManager,
                Status = "DRAFT",
                RevisionNumber = 1,
                PropertyId = src.PropertyId,
                CompanyCode = src.CompanyCode,
                PlantCode = src.PlantCode,
                VenueName = src.VenueName,
                OutletLocationId = src.OutletLocationId,
                KitchenLocationId = src.KitchenLocationId,
                StorageLocationCodesJson = src.StorageLocationCodesJson,
                StartDateUtc = src.StartDateUtc.AddDays(7),
                EndDateUtc = src.EndDateUtc.AddDays(7),
                StartTime = src.StartTime,
                EndTime = src.EndTime,
                TimeZoneId = src.TimeZoneId,
                GuaranteedGuests = src.GuaranteedGuests,
                ExpectedGuests = src.ExpectedGuests,
                MaxCapacity = src.MaxCapacity,
                AdditionalAllowancePercent = src.AdditionalAllowancePercent,
                BillablePolicy = src.BillablePolicy,
                AdultsCount = src.AdultsCount,
                ChildrenCount = src.ChildrenCount,
                VipCount = src.VipCount,
                StaffMealsCount = src.StaffMealsCount,
                Currency = src.Currency,
                NetPricePerGuest = src.NetPricePerGuest,
                FixedPackagePrice = src.FixedPackagePrice,
                TargetFoodCostPercent = src.TargetFoodCostPercent,
                TargetBeverageCostPercent = src.TargetBeverageCostPercent,
                CombinedMenuBudget = src.CombinedMenuBudget,
                ServiceStyle = src.ServiceStyle,
                PrimaryMenuId = newMenuId,
                DietaryRequirementsJson = src.DietaryRequirementsJson,
                WizardStepCompleted = 1,
                CorrelationId = Guid.NewGuid().ToString("N")[..12],
                IsActive = true
            };
            RecalculateGuestAndBudgetMetrics(copy);

            SilaCulinaryMenu newMenu = new()
            {
                Id = newMenuId,
                BuyerId = buyer.Id,
                MenuCode = newMenuCode,
                EventId = newEventId,
                Name = $"{copy.Name} — Primary Menu",
                MenuCategory = "Event Banquet",
                PropertyId = copy.PropertyId,
                OutletLocationId = copy.OutletLocationId,
                ServiceStyle = copy.ServiceStyle,
                VersionNumber = 1,
                EffectiveDateUtc = copy.StartDateUtc,
                Status = "DRAFT",
                Currency = copy.Currency,
                TargetFoodCostPercent = copy.TargetFoodCostPercent,
                IsActive = true
            };

            _repository.SilaCulinaryEvent.Create(copy);
            _repository.SilaCulinaryMenu.Create(newMenu);

            if (src.PrimaryMenuId.HasValue)
            {
                List<SilaCulinaryMenuLine> srcLines = await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == src.PrimaryMenuId.Value && x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(cancellationToken);
                foreach (SilaCulinaryMenuLine l in srcLines)
                {
                    _repository.SilaCulinaryMenuLine.Create(new SilaCulinaryMenuLine
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        MenuId = newMenuId,
                        CourseName = l.CourseName,
                        SortOrder = l.SortOrder,
                        ItemRole = l.ItemRole,
                        IsLocked = l.IsLocked,
                        RecipeId = l.RecipeId,
                        RecipeCode = l.RecipeCode,
                        RecipeVersion = l.RecipeVersion,
                        DishName = l.DishName,
                        CustomerDescription = l.CustomerDescription,
                        DishImageUrl = l.DishImageUrl,
                        LineStatus = l.LineStatus,
                        ServiceStyle = l.ServiceStyle,
                        UptakePercent = l.UptakePercent,
                        ServingFrequency = l.ServingFrequency,
                        PortionSize = l.PortionSize,
                        PortionUom = l.PortionUom,
                        AllocatedPortions = l.AllocatedPortions,
                        PlannedPortions = l.PlannedPortions,
                        CostingSnapshotId = l.CostingSnapshotId,
                        UnitCostPerPortion = l.UnitCostPerPortion,
                        EstimatedLineCost = l.EstimatedLineCost,
                        DietaryTagsJson = l.DietaryTagsJson,
                        VerifiedAllergensJson = l.VerifiedAllergensJson,
                        AllergenUnknown = l.AllergenUnknown,
                        AllergenVerified = l.AllergenVerified,
                        IsBeverage = l.IsBeverage,
                        ServingsPerGuest = l.ServingsPerGuest,
                        ServingVolumeMl = l.ServingVolumeMl,
                        ContainerSizeMl = l.ContainerSizeMl,
                        LiveStationName = l.LiveStationName,
                        VerifiedStationCapacityPerHour = l.VerifiedStationCapacityPerHour,
                        IsActive = true
                    });
                }
            }

            await _repository.SaveAsync();
            await RecalculateMenuTotalsInternalAsync(buyer.Id, copy, newMenu, cancellationToken);
            await _repository.SaveAsync();

            return await GetEventAsync(organizationId, userId, roleId, copy.Id, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> CancelEventAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string? reason, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);
            ev.Status = "CANCELLED";
            ev.ApprovalDecisionComment = string.IsNullOrWhiteSpace(reason) ? "Cancelled by authorized planner." : reason.Trim();
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_UPDATED,
                $"Cancelled event {ev.EventCode}: {ev.ApprovalDecisionComment}", userId);
            await _repository.SaveAsync();
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        // ==================== TEMPLATES (§10) ====================

        public async Task<List<SilaCulinaryEventTemplateDto>> ListTemplatesAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            return await _repository.SilaCulinaryEventTemplate
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .Select(t => new SilaCulinaryEventTemplateDto
                {
                    Id = t.Id,
                    TemplateCode = t.TemplateCode,
                    Name = t.Name,
                    Description = t.Description,
                    EventType = t.EventType,
                    DefaultServiceStyle = t.DefaultServiceStyle,
                    DefaultDurationDays = t.DefaultDurationDays,
                    DefaultAllowancePercent = t.DefaultAllowancePercent,
                    DefaultTargetFoodCostPercent = t.DefaultTargetFoodCostPercent,
                    CreatedOn = t.DateCreated
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SilaCulinaryEventTemplateDto> CreateTemplateFromEventAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid eventId, string templateName, string? description, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);

            // Never copy historical customer PII or historical prices into reusable templates (§10)
            List<object> templateLines = new();
            if (ev.PrimaryMenuId.HasValue)
            {
                List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == ev.PrimaryMenuId.Value && x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(cancellationToken);
                foreach (SilaCulinaryMenuLine l in lines)
                {
                    templateLines.Add(new
                    {
                        l.CourseName,
                        l.SortOrder,
                        l.ItemRole,
                        l.RecipeId,
                        l.DishName,
                        l.ServiceStyle,
                        l.UptakePercent,
                        l.ServingFrequency,
                        l.PortionSize,
                        l.PortionUom,
                        l.IsBeverage,
                        l.ServingsPerGuest,
                        l.ServingVolumeMl
                    });
                }
            }

            string code = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.EVENT_TEMPLATE, 6, cancellationToken);
            SilaCulinaryEventTemplate tpl = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                TemplateCode = code,
                Name = string.IsNullOrWhiteSpace(templateName) ? $"{ev.EventType} Template" : templateName.Trim(),
                Description = description,
                EventType = ev.EventType,
                DefaultServiceStyle = ev.ServiceStyle,
                DefaultDurationDays = Math.Max(1, (int)(ev.EndDateUtc.Date - ev.StartDateUtc.Date).TotalDays + 1),
                DefaultAllowancePercent = ev.AdditionalAllowancePercent,
                DefaultTargetFoodCostPercent = ev.TargetFoodCostPercent,
                TemplatePayloadJson = JsonSerializer.Serialize(new { lines = templateLines }, CamelJson),
                IsActive = true
            };
            _repository.SilaCulinaryEventTemplate.Create(tpl);
            await _repository.SaveAsync();

            return new SilaCulinaryEventTemplateDto
            {
                Id = tpl.Id,
                TemplateCode = tpl.TemplateCode,
                Name = tpl.Name,
                Description = tpl.Description,
                EventType = tpl.EventType,
                DefaultServiceStyle = tpl.DefaultServiceStyle,
                DefaultDurationDays = tpl.DefaultDurationDays,
                DefaultAllowancePercent = tpl.DefaultAllowancePercent,
                DefaultTargetFoodCostPercent = tpl.DefaultTargetFoodCostPercent,
                CreatedOn = tpl.DateCreated
            };
        }

        public async Task<SilaCulinaryEventDto> CreateEventFromTemplateAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid templateId, SilaCulinaryUpsertEventRequestDto overrides, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEventTemplate tpl = await _repository.SilaCulinaryEventTemplate
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == templateId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Template not found.", "Event template not found.");

            overrides.EventType = string.IsNullOrWhiteSpace(overrides.EventType) ? tpl.EventType : overrides.EventType;
            overrides.ServiceStyle = string.IsNullOrWhiteSpace(overrides.ServiceStyle) ? tpl.DefaultServiceStyle : overrides.ServiceStyle;
            if (overrides.AdditionalAllowancePercent == 0) overrides.AdditionalAllowancePercent = tpl.DefaultAllowancePercent;
            if (overrides.TargetFoodCostPercent == 0) overrides.TargetFoodCostPercent = tpl.DefaultTargetFoodCostPercent;

            SilaCulinaryEventDto created = await CreateEventAsync(organizationId, userId, roleId, actorName, overrides, cancellationToken);

            // Revalidate current published recipe versions & current costs from Recipe Master (§10)
            if (!string.IsNullOrWhiteSpace(tpl.TemplatePayloadJson) && created.PrimaryMenuId.HasValue)
            {
                using JsonDocument doc = JsonDocument.Parse(tpl.TemplatePayloadJson);
                if (doc.RootElement.TryGetProperty("lines", out JsonElement linesEl) && linesEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in linesEl.EnumerateArray())
                    {
                        Guid? rid = item.TryGetProperty("recipeId", out JsonElement rEl) && rEl.ValueKind == JsonValueKind.String && Guid.TryParse(rEl.GetString(), out Guid parsed)
                            ? parsed
                            : null;
                        await UpsertMenuLineAsync(organizationId, userId, roleId, actorName, created.PrimaryMenuId.Value, new SilaCulinaryUpsertMenuLineRequestDto
                        {
                            CourseName = item.TryGetProperty("courseName", out JsonElement cEl) ? cEl.GetString() ?? "Main Courses" : "Main Courses",
                            SortOrder = item.TryGetProperty("sortOrder", out JsonElement sEl) && sEl.TryGetInt32(out int so) ? so : 0,
                            ItemRole = item.TryGetProperty("itemRole", out JsonElement irEl) ? irEl.GetString() ?? "MANDATORY" : "MANDATORY",
                            RecipeId = rid,
                            DishName = item.TryGetProperty("dishName", out JsonElement dEl) ? dEl.GetString() : "Template Dish",
                            ServiceStyle = item.TryGetProperty("serviceStyle", out JsonElement ssEl) ? ssEl.GetString() ?? tpl.DefaultServiceStyle : tpl.DefaultServiceStyle,
                            UptakePercent = item.TryGetProperty("uptakePercent", out JsonElement uEl) && uEl.TryGetDecimal(out decimal up) ? up : 100m,
                            ServingFrequency = item.TryGetProperty("servingFrequency", out JsonElement sfEl) && sfEl.TryGetDecimal(out decimal sf) ? sf : 1m,
                            PortionSize = item.TryGetProperty("portionSize", out JsonElement psEl) && psEl.TryGetDecimal(out decimal ps) ? ps : 180m,
                            PortionUom = item.TryGetProperty("portionUom", out JsonElement puEl) ? puEl.GetString() ?? "G" : "G",
                            IsBeverage = item.TryGetProperty("isBeverage", out JsonElement ibEl) && ibEl.ValueKind == JsonValueKind.True,
                            ServingsPerGuest = item.TryGetProperty("servingsPerGuest", out JsonElement spgEl) && spgEl.TryGetDecimal(out decimal spg) ? spg : 1m,
                            ServingVolumeMl = item.TryGetProperty("servingVolumeMl", out JsonElement svEl) && svEl.TryGetDecimal(out decimal sv) ? sv : null
                        }, cancellationToken);
                    }
                }
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, created.Id, Common.SILA_AUDIT_CULINARY_TEMPLATE_APPLIED,
                $"Applied template {tpl.TemplateCode} to event {created.EventCode} with live recipe & cost revalidation.", userId);
            await _repository.SaveAsync();

            return await GetEventAsync(organizationId, userId, roleId, created.Id, cancellationToken);
        }

        // ==================== MULTI-DAY & MULTI-SESSION (§11) ====================

        public async Task<SilaCulinaryEventDto> UpsertSessionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryUpsertSessionRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);
            if (ev.Status is "APPROVED" or "READY_FOR_MRP")
            {
                throw new BadRequestCustomException("Approved baseline locked.", "Cannot edit sessions on an approved event baseline without creating a new revision.");
            }

            SilaCulinaryEventSession? session = request.SessionId.HasValue
                ? await _repository.SilaCulinaryEventSession
                    .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.EventId == ev.Id && x.Id == request.SessionId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            if (session == null)
            {
                int nextIdx = await _repository.SilaCulinaryEventSession
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.EventId == ev.Id)
                    .CountAsync(cancellationToken) + 1;

                Guid? sessionMenuId = request.MenuId;
                if (!sessionMenuId.HasValue)
                {
                    string mCode = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.CULINARY_MENU, 6, cancellationToken);
                    Guid mId = Guid.NewGuid();
                    _repository.SilaCulinaryMenu.Create(new SilaCulinaryMenu
                    {
                        Id = mId,
                        BuyerId = buyer.Id,
                        MenuCode = mCode,
                        EventId = ev.Id,
                        Name = $"{ev.Name} — {request.SessionName}",
                        ServiceStyle = request.ServiceStyle,
                        EffectiveDateUtc = request.SessionDateUtc,
                        Currency = ev.Currency,
                        TargetFoodCostPercent = ev.TargetFoodCostPercent,
                        Status = "DRAFT",
                        IsActive = true
                    });
                    sessionMenuId = mId;
                }

                session = new SilaCulinaryEventSession
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    EventId = ev.Id,
                    SessionCode = $"{ev.EventCode}-S{nextIdx:00}",
                    MenuId = sessionMenuId,
                    IsActive = true
                };
                _repository.SilaCulinaryEventSession.Create(session);
            }

            session.DayNumber = Math.Max(1, request.DayNumber);
            session.SessionDateUtc = request.SessionDateUtc;
            session.StartTime = request.StartTime;
            session.EndTime = request.EndTime;
            session.SessionName = request.SessionName.Trim();
            session.VenueName = request.VenueName;
            session.KitchenLocationId = request.KitchenLocationId;
            session.ServiceStyle = request.ServiceStyle;
            session.GuestCount = Math.Max(0, request.GuestCount);
            if (request.MenuId.HasValue) session.MenuId = request.MenuId;
            session.BudgetAllocation = request.BudgetAllocation;
            session.PrepDeadlineUtc = request.PrepDeadlineUtc;
            session.DietaryNotesJson = request.DietaryNotesJson;
            session.SortOrder = request.SortOrder;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_SESSION_SAVED,
                $"Saved session {session.SessionCode} ({session.SessionName}, {session.GuestCount} guests).", userId);
            await _repository.SaveAsync();

            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> DeleteSessionAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid eventId, Guid sessionId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            if (ev.Status is "APPROVED" or "READY_FOR_MRP")
            {
                throw new BadRequestCustomException("Approved baseline locked.", "Cannot delete sessions on an approved event baseline.");
            }
            SilaCulinaryEventSession? session = await _repository.SilaCulinaryEventSession
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.EventId == ev.Id && x.Id == sessionId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (session != null)
            {
                session.IsActive = false;
                await _repository.SaveAsync();
            }
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        // ==================== MENU STUDIO & COMPOSITION (§13 - §16, §25) ====================

        public async Task<List<SilaCulinaryMenuDto>> ListMenusAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid? eventId, bool? reusableOnly, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            IQueryable<SilaCulinaryMenu> q = _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (eventId.HasValue) q = q.Where(x => x.EventId == eventId.Value);
            if (reusableOnly == true) q = q.Where(x => x.IsReusableTemplate);

            List<SilaCulinaryMenu> menus = await q.OrderByDescending(x => x.DateCreated).Take(100).ToListAsync(cancellationToken);
            bool costVisible = CanViewCosts(roleId);
            List<SilaCulinaryMenuDto> list = new(menus.Count);
            foreach (SilaCulinaryMenu m in menus)
            {
                list.Add(await BuildMenuDtoAsync(buyer.Id, m, costVisible, cancellationToken));
            }
            return list;
        }

        public async Task<SilaCulinaryMenuDto> GetMenuAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid menuId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Menu not found.");
            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMenuDto> CreateMenuAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, SilaCulinaryCreateMenuRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new BadRequestCustomException("Invalid menu request.", "Menu name is required.");
            }
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            string code = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.CULINARY_MENU, 6, cancellationToken);
            SilaCulinaryMenu menu = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MenuCode = code,
                EventId = request.EventId,
                SessionId = request.SessionId,
                Name = request.Name.Trim(),
                Description = request.Description,
                MenuCategory = request.MenuCategory,
                Cuisine = request.Cuisine,
                PropertyId = request.PropertyId,
                OutletLocationId = request.OutletLocationId,
                ServiceStyle = request.ServiceStyle,
                VersionNumber = 1,
                EffectiveDateUtc = DateTime.UtcNow,
                Status = "DRAFT",
                IsReusableTemplate = request.IsReusableTemplate,
                Currency = request.Currency,
                TargetSellingPrice = request.TargetSellingPrice,
                TargetFoodCostPercent = request.TargetFoodCostPercent,
                IsActive = true
            };
            _repository.SilaCulinaryMenu.Create(menu);
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MENU, menu.Id, Common.SILA_AUDIT_CULINARY_MENU_SAVED,
                $"Created Menu Studio record {menu.MenuCode} ({menu.Name}).", userId);
            await _repository.SaveAsync();
            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMenuDto> UpsertMenuLineAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid menuId, SilaCulinaryUpsertMenuLineRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Menu not found.");

            SilaCulinaryEvent? ev = menu.EventId.HasValue
                ? await _repository.SilaCulinaryEvent
                    .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.Id == menu.EventId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            if (ev != null && ev.Status is "APPROVED" or "READY_FOR_MRP")
            {
                throw new BadRequestCustomException("Approved baseline locked.", "Approved event menu cannot be edited without creating a new revision.");
            }

            Recipe? recipe = null;
            RecipeMeta meta = new();
            if (request.RecipeId.HasValue)
            {
                recipe = await _repository.Recipe
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == request.RecipeId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (recipe == null)
                {
                    throw new BadRequestCustomException(
                        "Invalid Recipe ID.",
                        $"Recipe {request.RecipeId.Value} does not exist in this tenant's Recipe Master. Fictitious Recipe IDs are not permitted.");
                }
                meta = await ResolveRecipeMetadataAsync(buyer.Id, recipe, cancellationToken);
            }

            SilaCulinaryMenuLine? line = request.LineId.HasValue
                ? await _repository.SilaCulinaryMenuLine
                    .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.Id == request.LineId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken)
                : null;

            bool isNew = line == null;
            if (line == null)
            {
                int maxSort = await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.IsActive)
                    .Select(x => (int?)x.SortOrder)
                    .MaxAsync(cancellationToken) ?? 0;

                line = new SilaCulinaryMenuLine
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MenuId = menu.Id,
                    SortOrder = request.SortOrder ?? (maxSort + 10),
                    IsActive = true
                };
                _repository.SilaCulinaryMenuLine.Create(line);
            }
            else if (request.SortOrder.HasValue)
            {
                line.SortOrder = request.SortOrder.Value;
            }

            line.CourseName = string.IsNullOrWhiteSpace(request.CourseName) ? "Main Courses" : request.CourseName.Trim();
            line.ItemRole = string.IsNullOrWhiteSpace(request.ItemRole) ? "MANDATORY" : request.ItemRole.Trim().ToUpperInvariant();
            line.IsLocked = request.IsLocked;
            line.ServiceStyle = string.IsNullOrWhiteSpace(request.ServiceStyle) ? menu.ServiceStyle : request.ServiceStyle.Trim().ToUpperInvariant();
            line.UptakePercent = request.UptakePercent;
            line.ServingFrequency = request.ServingFrequency;
            line.PortionSize = request.PortionSize > 0 ? request.PortionSize : 180m;
            line.PortionUom = string.IsNullOrWhiteSpace(request.PortionUom) ? "G" : request.PortionUom.Trim().ToUpperInvariant();
            line.AllocatedPortions = request.AllocatedPortions;
            line.ManualPortionsOverride = request.ManualPortionsOverride;
            line.IsBeverage = request.IsBeverage || (recipe?.ProductClass == "BEVERAGE");
            line.ServingsPerGuest = request.ServingsPerGuest;
            line.ServingVolumeMl = request.ServingVolumeMl;
            line.ContainerSizeMl = request.ContainerSizeMl;
            line.LiveStationName = request.LiveStationName;
            line.VerifiedStationCapacityPerHour = request.VerifiedStationCapacityPerHour;
            line.CustomerDescription = request.CustomerDescription;
            line.Notes = request.Notes;

            if (recipe != null)
            {
                bool isPublished = string.Equals(recipe.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) || recipe.ActiveVersion > 0;
                line.RecipeId = recipe.Id;
                line.RecipeCode = recipe.RecipeCode;
                line.RecipeVersion = request.RecipeVersion ?? (recipe.ActiveVersion > 0 ? recipe.ActiveVersion : recipe.Version);
                line.DishName = recipe.Name;
                line.DishImageUrl = meta.ImageUrl;
                line.LineStatus = isPublished ? "PUBLISHED" : "DRAFT_PLACEHOLDER";
                line.CostingSnapshotId = meta.CostingSnapshotId;
                line.UnitCostPerPortion = request.UnitCostOverride ?? meta.CostPerPortion;
                line.DietaryTagsJson = JsonSerializer.Serialize(meta.DietaryTags, CamelJson);
                line.VerifiedAllergensJson = JsonSerializer.Serialize(meta.VerifiedAllergens, CamelJson);
                line.AllergenUnknown = meta.AllergenUnknown;
                line.AllergenVerified = !meta.AllergenUnknown;
            }
            else
            {
                line.RecipeId = null;
                line.RecipeCode = null;
                line.RecipeVersion = null;
                line.DishName = string.IsNullOrWhiteSpace(request.DishName) ? "New Recipe Placeholder" : request.DishName.Trim();
                line.LineStatus = "RECIPE_REQUIRED";
                line.UnitCostPerPortion = request.UnitCostOverride;
                line.AllergenUnknown = true;
                line.AllergenVerified = false;
            }

            await _repository.SaveAsync();
            await RecalculateMenuTotalsInternalAsync(buyer.Id, ev, menu, cancellationToken);
            await _repository.SaveAsync();

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MENU, menu.Id,
                isNew ? Common.SILA_AUDIT_CULINARY_MENU_LINE_ADDED : Common.SILA_AUDIT_CULINARY_MENU_LINE_REPLACED,
                $"{(isNew ? "Added" : "Updated")} '{line.DishName}' ({line.LineStatus}) in {menu.MenuCode}.", userId);
            await _repository.SaveAsync();

            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMenuDto> RemoveMenuLineAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid menuId, Guid lineId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Menu not found.");

            SilaCulinaryMenuLine? line = await _repository.SilaCulinaryMenuLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.Id == lineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (line != null)
            {
                line.IsActive = false;
                await _repository.SaveAsync();
                SilaCulinaryEvent? ev = menu.EventId.HasValue
                    ? await _repository.SilaCulinaryEvent.FindByConditionTracked(x => x.Id == menu.EventId.Value).FirstOrDefaultAsync(cancellationToken)
                    : null;
                await RecalculateMenuTotalsInternalAsync(buyer.Id, ev, menu, cancellationToken);
                await _repository.SaveAsync();
            }
            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMenuDto> ReorderMenuLinesAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid menuId, List<Guid> orderedLineIds, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Menu not found.");

            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, int> orderMap = orderedLineIds
                .Select((id, idx) => (id, idx))
                .ToDictionary(x => x.id, x => (x.idx + 1) * 10);

            foreach (SilaCulinaryMenuLine l in lines)
            {
                if (orderMap.TryGetValue(l.Id, out int pos))
                {
                    l.SortOrder = pos;
                }
            }
            await _repository.SaveAsync();
            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<List<SilaCulinaryPublishedRecipeSearchItemDto>> SearchPublishedRecipesAsync(
            Guid organizationId, Guid roleId, string? query, string? category, string? cuisine, string? dietary, string? excludeAllergens, decimal? maxCost, Guid? propertyId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            List<Recipe> recipes = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .OrderBy(x => x.Name)
                .Take(300)
                .ToListAsync(cancellationToken);

            List<string> reqDiet = ParseCsvList(dietary);
            List<string> exclAllergens = ParseCsvList(excludeAllergens);

            List<SilaCulinaryPublishedRecipeSearchItemDto> items = new();
            foreach (Recipe r in recipes)
            {
                bool isPublished = string.Equals(r.Status, "APPROVED", StringComparison.OrdinalIgnoreCase) || r.ActiveVersion > 0;
                if (!isPublished) continue;

                RecipeMeta meta = await ResolveRecipeMetadataAsync(buyer.Id, r, cancellationToken);
                if (!string.IsNullOrWhiteSpace(category) && !string.Equals(r.Category, category.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrWhiteSpace(cuisine) && !string.Equals(meta.Cuisine, cuisine.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                if (maxCost.HasValue && meta.CostPerPortion.HasValue && meta.CostPerPortion.Value > maxCost.Value) continue;

                if (reqDiet.Count > 0 && !reqDiet.All(d => meta.DietaryTags.Any(t => string.Equals(t, d, StringComparison.OrdinalIgnoreCase))))
                {
                    continue;
                }

                // Strict allergen safety: exclude known excluded allergens AND unknown allergen items when allergen exclusion is specified
                if (exclAllergens.Count > 0)
                {
                    if (meta.AllergenUnknown) continue;
                    if (exclAllergens.Any(ea => meta.VerifiedAllergens.Any(va => string.Equals(va, ea, StringComparison.OrdinalIgnoreCase)))) continue;
                }

                decimal score = 0.85m;
                string reason = "Published Recipe Master";
                if (!string.IsNullOrWhiteSpace(query))
                {
                    string q = query.Trim().ToLowerInvariant();
                    string hay = $"{r.RecipeCode} {r.Name} {r.Description} {r.Category} {meta.Cuisine} {string.Join(" ", meta.DietaryTags)}".ToLowerInvariant();
                    if (r.RecipeCode.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase) || r.Name.Equals(query.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        score = 1.00m;
                        reason = "Exact Recipe ID / Name match";
                    }
                    else if (hay.Contains(q))
                    {
                        score = 0.92m;
                        reason = "Matched Recipe Master attributes";
                    }
                    else
                    {
                        string[] qTokens = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        int hits = qTokens.Count(t => hay.Contains(t));
                        if (hits == 0) continue;
                        score = Math.Round((decimal)hits / qTokens.Length * 0.85m, 2);
                        reason = $"Semantic token overlap ({hits}/{qTokens.Length})";
                    }
                }

                items.Add(new SilaCulinaryPublishedRecipeSearchItemDto
                {
                    RecipeId = r.Id,
                    RecipeCode = r.RecipeCode,
                    RecipeVersion = r.ActiveVersion > 0 ? r.ActiveVersion : r.Version,
                    Name = r.Name,
                    Category = r.Category ?? "General",
                    Cuisine = meta.Cuisine,
                    ImageUrl = meta.ImageUrl,
                    ServingQty = r.ServingQty,
                    ServingUom = r.ServingUom,
                    CostPerPortion = costVisible ? meta.CostPerPortion : null,
                    Currency = r.Currency ?? "AED",
                    PrepTimeMinutes = r.PrepTimeMinutes,
                    CookTimeMinutes = r.CookTimeMinutes,
                    DietaryTags = meta.DietaryTags,
                    VerifiedAllergens = meta.VerifiedAllergens,
                    UnknownAllergens = meta.AllergenUnknown,
                    PropertyEligible = true,
                    MatchScore = score,
                    MatchReason = reason
                });
            }

            return items.OrderByDescending(x => x.MatchScore).ThenBy(x => x.Name).Take(50).ToList();
        }

        // ==================== MISSING RECIPE HANDOVER (§20) ====================

        public async Task<SilaCulinaryMenuDto> CreateMissingRecipeDraftAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid menuId, Guid lineId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Menu not found.");
            SilaCulinaryMenuLine line = await _repository.SilaCulinaryMenuLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.Id == lineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu line not found.", "Menu line not found.");

            if (line.RecipeDraftId.HasValue)
            {
                return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
            }

            string draftRef = $"DRF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            SilaCulinaryRecipeDraft draft = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                DraftReference = draftRef,
                RecipeName = line.DishName,
                GenerationMode = "EVENT_MENU_HANDOVER",
                Status = "DRAFT",
                PropertyId = menu.PropertyId,
                OutletId = menu.OutletLocationId,
                BaseServings = 10m,
                RevisionNumber = 1,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    name = line.DishName,
                    category = line.CourseName,
                    cuisine = menu.Cuisine,
                    baseServings = 10,
                    servingSize = line.PortionSize,
                    servingUom = line.PortionUom
                }, CamelJson),
                CreatedByUserId = userId,
                CreatedByName = actorName,
                IsActive = true
            };
            _repository.SilaCulinaryRecipeDraft.Create(draft);
            line.RecipeDraftId = draft.Id;
            line.LineStatus = "DRAFT_PLACEHOLDER";

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_MENU, menu.Id, Common.SILA_AUDIT_CULINARY_MISSING_RECIPE_LINKED,
                $"Linked missing recipe '{line.DishName}' to Phase 1A draft {draftRef}.", userId);
            await _repository.SaveAsync();

            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryMenuDto> SyncPublishedRecipeToMenuLineAsync(
            Guid organizationId, Guid userId, Guid roleId, Guid menuId, Guid lineId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryMenu menu = await _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == menuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Menu not found.");
            SilaCulinaryMenuLine line = await _repository.SilaCulinaryMenuLine
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.Id == lineId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu line not found.", "Menu line not found.");

            if (!line.RecipeDraftId.HasValue)
            {
                throw new BadRequestCustomException("Missing draft link.", "Menu line has no linked Phase 1A Recipe Draft.");
            }

            SilaCulinaryRecipeLifecycle? lifecycle = await _repository.SilaCulinaryRecipeLifecycle
                .FindByCondition(x => x.BuyerId == buyer.Id && x.DraftId == line.RecipeDraftId.Value && x.ProductionRecipeId != null && x.LifecycleStatus == "PUBLISHED" && x.IsActive)
                .OrderByDescending(x => x.PublishedOnUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (lifecycle?.ProductionRecipeId == null)
            {
                throw new BadRequestCustomException("Linked recipe draft is not yet published.", "Complete Phase 1C costing, approval, and publishing first.");
            }

            Recipe? prod = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == lifecycle.ProductionRecipeId.Value && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Recipe not found.", "Published production recipe not found.");

            RecipeMeta meta = await ResolveRecipeMetadataAsync(buyer.Id, prod, cancellationToken);
            line.RecipeId = prod.Id;
            line.RecipeCode = prod.RecipeCode;
            line.RecipeVersion = prod.ActiveVersion > 0 ? prod.ActiveVersion : prod.Version;
            line.DishName = prod.Name;
            line.LineStatus = "PUBLISHED";
            line.CostingSnapshotId = meta.CostingSnapshotId;
            line.UnitCostPerPortion = meta.CostPerPortion;
            line.VerifiedAllergensJson = JsonSerializer.Serialize(meta.VerifiedAllergens, CamelJson);
            line.AllergenUnknown = meta.AllergenUnknown;
            line.AllergenVerified = !meta.AllergenUnknown;

            await _repository.SaveAsync();
            SilaCulinaryEvent? ev = menu.EventId.HasValue
                ? await _repository.SilaCulinaryEvent.FindByConditionTracked(x => x.Id == menu.EventId.Value).FirstOrDefaultAsync(cancellationToken)
                : null;
            await RecalculateMenuTotalsInternalAsync(buyer.Id, ev, menu, cancellationToken);
            await _repository.SaveAsync();

            return await BuildMenuDtoAsync(buyer.Id, menu, CanViewCosts(roleId), cancellationToken);
        }

        // ==================== AI MENU GENERATION & OPTIMIZATION (§17 - §19, §22, §36) ====================

        public async Task<SilaCulinaryAiMenuGenerateResultDto> GenerateAiMenuAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryAiMenuGenerateRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            List<SilaCulinaryPublishedRecipeSearchItemDto> catalog = await SearchPublishedRecipesAsync(
                organizationId, roleId: Common.BUYER_ADMIN_ROLE_ID, query: null, category: null, cuisine: null, dietary: null,
                excludeAllergens: string.Join(",", request.ExcludedAllergens), maxCost: null, propertyId: ev.PropertyId, cancellationToken);

            List<Guid> lockedIds = new();
            if (ev.PrimaryMenuId.HasValue)
            {
                lockedIds = await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == ev.PrimaryMenuId.Value && x.IsActive && x.IsLocked && x.RecipeId != null)
                    .Select(x => x.RecipeId!.Value)
                    .ToListAsync(cancellationToken);
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_AI_MENU_REQ,
                $"Requested AI menu generation for {ev.EventCode} ({ev.PlanningGuests} guests).", userId);

            SilaCulinaryAiMenuGenerateResultDto? result = await TryCallPythonMenuGenerateAsync(buyer.Id, ev, request, catalog, lockedIds, cancellationToken);
            if (result == null)
            {
                result = DeterministicFallbackMenuGenerate(ev, request, catalog, lockedIds);
            }

            // Always verify every returned RecipeId exists in authorized tenant catalog (zero hallucinated IDs)
            Dictionary<Guid, SilaCulinaryPublishedRecipeSearchItemDto> validMap = catalog.ToDictionary(x => x.RecipeId);
            foreach (SilaCulinaryMenuLineDto line in result.ProposedLines)
            {
                if (line.RecipeId.HasValue && !validMap.ContainsKey(line.RecipeId.Value))
                {
                    line.RecipeId = null;
                    line.RecipeCode = null;
                    line.LineStatus = "RECIPE_REQUIRED";
                }
            }

            if (request.ApplyToMenu && ev.PrimaryMenuId.HasValue)
            {
                foreach (SilaCulinaryMenuLineDto p in result.ProposedLines)
                {
                    if (p.RecipeId.HasValue && lockedIds.Contains(p.RecipeId.Value)) continue;
                    await UpsertMenuLineAsync(organizationId, userId, roleId, actorName, ev.PrimaryMenuId.Value, new SilaCulinaryUpsertMenuLineRequestDto
                    {
                        CourseName = p.CourseName,
                        RecipeId = p.RecipeId,
                        RecipeVersion = p.RecipeVersion,
                        DishName = p.DishName,
                        ServiceStyle = ev.ServiceStyle,
                        UptakePercent = p.UptakePercent,
                        Notes = p.Notes
                    }, cancellationToken);
                }
            }

            if (!costVisible)
            {
                result.EstimatedTotalCost = null;
                foreach (SilaCulinaryMenuLineDto l in result.ProposedLines)
                {
                    l.UnitCostPerPortion = null;
                    l.EstimatedLineCost = null;
                    l.CostVisible = false;
                }
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_AI_MENU_DONE,
                $"AI menu generated via {result.ProviderUsed} ({result.ProposedLines.Count} items, {result.MissingRecipeCount} missing recipes).", userId);
            await _repository.SaveAsync();
            return result;
        }

        public async Task<SilaCulinaryMenuOptimizeResultDto> OptimizeEventMenuAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, SilaCulinaryMenuOptimizeRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            SilaCulinaryMenu? primary = ev.PrimaryMenuId.HasValue
                ? await _repository.SilaCulinaryMenu.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == ev.PrimaryMenuId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken)
                : null;

            List<SilaCulinaryMenuLine> currentLines = primary == null
                ? new()
                : await _repository.SilaCulinaryMenuLine.FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == primary.Id && x.IsActive).ToListAsync(cancellationToken);

            decimal beforeCost = currentLines.Sum(x => x.EstimatedLineCost ?? 0m);
            decimal maxBudget = request.MaxBudget ?? ev.CombinedMenuBudget ?? ev.ApprovedBudget ?? (ev.EstimatedFoodRevenue * (ev.TargetFoodCostPercent / 100m));
            if (maxBudget <= 0) maxBudget = 10000m;

            List<SilaCulinaryPublishedRecipeSearchItemDto> catalog = await SearchPublishedRecipesAsync(
                organizationId, roleId: Common.BUYER_ADMIN_ROLE_ID, query: null, category: null, cuisine: null, dietary: null,
                excludeAllergens: string.Join(",", request.ExcludedAllergens), maxCost: null, propertyId: ev.PropertyId, cancellationToken);

            List<Guid> lockedIds = currentLines.Where(l => l.IsLocked && l.RecipeId.HasValue).Select(l => l.RecipeId!.Value).ToList();
            Dictionary<string, int> requiredCourses = request.RequiredCourses is { Count: > 0 }
                ? request.RequiredCourses
                : currentLines.GroupBy(l => l.CourseName).ToDictionary(g => g.Key, g => g.Count());
            if (requiredCourses.Count == 0) requiredCourses = new Dictionary<string, int> { ["Main Courses"] = 1 };

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_MENU_OPT_REQ,
                $"Requested MILP menu optimization for {ev.EventCode} (budget {maxBudget:F2} {ev.Currency}).", userId);

            SilaCulinaryMenuOptimizeResultDto? optRes = await TryCallPythonMenuOptimizeAsync(
                ev, maxBudget, requiredCourses, lockedIds, request, catalog, beforeCost, cancellationToken);
            if (optRes == null)
            {
                optRes = DeterministicFallbackOptimize(ev, maxBudget, requiredCourses, lockedIds, request, catalog, beforeCost);
            }

            if (request.ApplyChanges && optRes.Feasible && primary != null)
            {
                List<SilaCulinaryMenuLine> tracked = await _repository.SilaCulinaryMenuLine
                    .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.MenuId == primary.Id && x.IsActive && !x.IsLocked)
                    .ToListAsync(cancellationToken);
                foreach (SilaCulinaryMenuLine t in tracked) t.IsActive = false;

                foreach (SilaCulinaryMenuLineDto chosen in optRes.OptimizedLines)
                {
                    if (chosen.RecipeId.HasValue && lockedIds.Contains(chosen.RecipeId.Value)) continue;
                    await UpsertMenuLineAsync(organizationId, userId, roleId, actorName, primary.Id, new SilaCulinaryUpsertMenuLineRequestDto
                    {
                        CourseName = chosen.CourseName,
                        RecipeId = chosen.RecipeId,
                        RecipeVersion = chosen.RecipeVersion,
                        DishName = chosen.DishName,
                        ServiceStyle = ev.ServiceStyle,
                        UptakePercent = chosen.UptakePercent
                    }, cancellationToken);
                }
            }

            if (!CanViewCosts(roleId))
            {
                optRes.BeforeCost = null;
                optRes.AfterCost = null;
                optRes.Budget = null;
                optRes.Savings = null;
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_MENU_OPT_DONE,
                $"Menu optimization {(optRes.Feasible ? "succeeded" : "reported infeasible")} for {ev.EventCode}.", userId);
            await _repository.SaveAsync();
            return optRes;
        }

        // ==================== DIETARY, ALLERGENS, MATERIAL PREVIEW & READ-ONLY STOCK (§23, §24, §30, §31) ====================

        public async Task<SilaCulinaryEventDto> VerifyDietaryReviewAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, bool verified, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);
            ev.DietaryReviewVerified = verified;
            ev.DietaryReviewerName = verified ? actorName : null;
            ev.DietaryVerifiedOnUtc = verified ? DateTime.UtcNow : null;

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_ALLERGEN_REVIEWED,
                $"Dietary & allergen review marked {(verified ? "VERIFIED" : "PENDING")} by {actorName}.", userId);
            await _repository.SaveAsync();
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<List<SilaCulinaryAllergenMatrixRowDto>> GetAllergenMatrixAsync(
            Guid organizationId, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            if (!ev.PrimaryMenuId.HasValue) return new();

            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == ev.PrimaryMenuId.Value && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            List<SilaCulinaryAllergenMatrixRowDto> rows = new(lines.Count);
            foreach (SilaCulinaryMenuLine l in lines)
            {
                List<string> verified = ParseJsonStringList(l.VerifiedAllergensJson);
                List<string> subAllergens = new();
                if (l.RecipeId.HasValue)
                {
                    subAllergens = await CollectSubRecipeAllergensAsync(buyer.Id, l.RecipeId.Value, new HashSet<Guid>(), cancellationToken);
                }
                rows.Add(new SilaCulinaryAllergenMatrixRowDto
                {
                    MenuLineId = l.Id,
                    DishName = l.DishName,
                    RecipeCode = l.RecipeCode,
                    RecipeVersion = l.RecipeVersion,
                    VerifiedAllergens = verified,
                    SubRecipeAllergens = subAllergens,
                    AllergenUnknown = l.AllergenUnknown,
                    CrossContactRisk = l.AllergenUnknown ? "UNKNOWN" : (verified.Count > 0 || subAllergens.Count > 0 ? "PRESENT" : "LOW"),
                    VerificationStatus = l.AllergenVerified ? "VERIFIED" : (l.AllergenUnknown ? "UNKNOWN_REQUIRES_REVIEW" : "PENDING")
                });
            }
            return rows;
        }

        public async Task<List<SilaCulinaryMaterialPreviewLineDto>> GetPreliminaryMaterialPreviewAsync(
            Guid organizationId, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            if (!ev.PrimaryMenuId.HasValue) return new();

            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == ev.PrimaryMenuId.Value && x.IsActive && x.RecipeId != null)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            List<Guid> recipeIds = lines.Select(x => x.RecipeId!.Value).Distinct().ToList();
            if (recipeIds.Count == 0) return new();

            Dictionary<Guid, Recipe> recipeMap = await _repository.Recipe
                .FindByCondition(x => x.BuyerId == buyer.Id && recipeIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            List<RecipeIngredient> allIngredients = await _repository.RecipeIngredient
                .FindByCondition(x => recipeIds.Contains(x.RecipeId) && x.IsActive)
                .ToListAsync(cancellationToken);

            List<Guid> matIds = allIngredients.Where(i => i.MaterialId.HasValue).Select(i => i.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, ItemBuyerMaster> matMap = matIds.Count == 0
                ? new()
                : await _repository.ItemBuyerMaster
                    .FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.Id) && x.IsActive)
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

            // Strictly READ-ONLY Live Inventory query (§31 — never reserves or mutates stock)
            List<InventoryBalance> balances = matIds.Count == 0
                ? new()
                : await _repository.InventoryBalance
                    .FindByCondition(x => x.BuyerId == buyer.Id && matIds.Contains(x.MaterialId) && x.IsActive)
                    .ToListAsync(cancellationToken);

            Dictionary<Guid, (decimal OnHand, DateTime? LastMove)> stockByMaterial = balances
                .GroupBy(b => b.MaterialId)
                .ToDictionary(
                    g => g.Key,
                    g => (g.Sum(b => b.OnHandQty), (DateTime?)g.Max(b => b.LastMovementOn ?? b.DateCreated)));

            List<SilaCulinaryMaterialPreviewLineDto> preview = new();
            foreach (SilaCulinaryMenuLine ml in lines)
            {
                if (!ml.RecipeId.HasValue || !recipeMap.TryGetValue(ml.RecipeId.Value, out Recipe? rec)) continue;
                decimal baseServings = rec.ServingQty > 0 ? rec.ServingQty : 1m;
                decimal scale = ml.PlannedPortions / baseServings;

                foreach (RecipeIngredient ing in allIngredients.Where(i => i.RecipeId == rec.Id))
                {
                    decimal reqQty = Math.Round(ing.Quantity * scale, 3);
                    decimal baseQty = Math.Round(ing.BaseQuantity * scale, 3);
                    bool nested = ing.SubRecipeId.HasValue;

                    ItemBuyerMaster? mat = ing.MaterialId.HasValue && matMap.TryGetValue(ing.MaterialId.Value, out ItemBuyerMaster? m) ? m : null;
                    decimal? onHand = ing.MaterialId.HasValue && stockByMaterial.TryGetValue(ing.MaterialId.Value, out var sb) ? sb.OnHand : null;
                    DateTime? ts = ing.MaterialId.HasValue && stockByMaterial.TryGetValue(ing.MaterialId.Value, out var sb2) ? sb2.LastMove : null;

                    string indicator = nested
                        ? "REVIEW_REQUIRED"
                        : !onHand.HasValue
                            ? "UNKNOWN"
                            : onHand.Value <= 0
                                ? "UNAVAILABLE"
                                : onHand.Value >= baseQty
                                    ? "SUFFICIENT"
                                    : "POTENTIAL_SHORTAGE";

                    preview.Add(new SilaCulinaryMaterialPreviewLineDto
                    {
                        DishName = ml.DishName,
                        RecipeCode = ml.RecipeCode,
                        RecipeVersion = ml.RecipeVersion,
                        IngredientName = ing.ItemName,
                        MaterialId = ing.MaterialId,
                        MaterialCode = mat?.MaterialCode ?? ing.ItemCode,
                        MaterialDescription = mat?.Description ?? ing.ItemName,
                        RequiredQty = reqQty,
                        RecipeUom = ing.Uom,
                        BaseQty = baseQty,
                        BaseUom = ing.BaseUom,
                        OnHandStock = onHand,
                        StockIndicator = indicator,
                        StockTimestampUtc = ts,
                        ScalingIncomplete = nested
                    });
                }
            }

            return preview;
        }

        // ==================== SIMULATION & SCENARIOS (§34, §35) ====================

        public async Task<SilaCulinaryEventSimulationResultDto> SimulateEventAsync(
            Guid organizationId, Guid roleId, Guid eventId, SilaCulinaryEventSimulationRequestDto request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);

            int guaranteed = request.GuaranteedGuests ?? ev.GuaranteedGuests;
            decimal allowance = request.AdditionalAllowancePercent ?? ev.AdditionalAllowancePercent;
            int planning = (int)Math.Ceiling(guaranteed * (1m + allowance / 100m));
            int expected = request.ExpectedGuests ?? ev.ExpectedGuests;
            if (allowance == 0m && expected > planning) planning = expected;

            int billable = ev.BillablePolicy == "EXPECTED_OR_GUARANTEED" ? Math.Max(guaranteed, planning) : guaranteed;
            decimal pricePerGuest = request.NetPricePerGuest ?? ev.NetPricePerGuest;
            decimal estRevenue = ev.FixedPackagePrice.HasValue && ev.FixedPackagePrice.Value > 0
                ? ev.FixedPackagePrice.Value
                : billable * pricePerGuest;
            decimal targetFc = request.TargetFoodCostPercent ?? ev.TargetFoodCostPercent;
            decimal budget = request.MenuBudget ?? ev.CombinedMenuBudget ?? ev.ApprovedBudget ?? Math.Round(estRevenue * (targetFc / 100m), 2);

            List<SilaCulinaryMenuLine> lines = ev.PrimaryMenuId.HasValue
                ? await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == ev.PrimaryMenuId.Value && x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(cancellationToken)
                : new();

            Dictionary<Guid, SilaCulinaryMenuLineSimulationOverrideDto> ovMap = request.LineOverrides.ToDictionary(x => x.LineId);
            decimal foodCost = 0m;
            decimal bevCost = 0m;
            List<SilaCulinaryMenuLineDto> simLines = new();
            List<string> warnings = new();

            foreach (SilaCulinaryMenuLine l in lines)
            {
                ovMap.TryGetValue(l.Id, out SilaCulinaryMenuLineSimulationOverrideDto? ov);
                decimal uptake = ov?.UptakePercent ?? l.UptakePercent;
                decimal portionSz = ov?.PortionSize ?? l.PortionSize;
                decimal? unitCost = ov?.UnitCostOverride ?? l.UnitCostPerPortion;

                int portions = l.ManualPortionsOverride ?? (l.AllocatedPortions ?? ComputePlannedPortions(planning, l.IsBeverage, l.ServiceStyle, uptake, l.ServingFrequency, l.ServingsPerGuest));
                decimal? lineCost = unitCost.HasValue ? Math.Round(portions * unitCost.Value, 2) : null;
                if (lineCost.HasValue)
                {
                    if (l.IsBeverage) bevCost += lineCost.Value;
                    else foodCost += lineCost.Value;
                }
                else
                {
                    warnings.Add($"'{l.DishName}' has no verified unit cost.");
                }

                simLines.Add(ToMenuLineDto(l, costVisible, portions, unitCost, lineCost, portionSz, uptake));
            }

            decimal totalCost = foodCost + bevCost;
            return new SilaCulinaryEventSimulationResultDto
            {
                GuaranteedGuests = guaranteed,
                PlanningGuests = planning,
                BillableGuests = billable,
                Currency = ev.Currency,
                EstimatedRevenue = costVisible ? estRevenue : null,
                TotalFoodCost = costVisible ? foodCost : null,
                TotalBeverageCost = costVisible ? bevCost : null,
                TotalMenuCost = costVisible ? totalCost : null,
                CostPerGuaranteedGuest = costVisible && guaranteed > 0 ? Math.Round(totalCost / guaranteed, 2) : null,
                CostPerPlannedGuest = costVisible && planning > 0 ? Math.Round(totalCost / planning, 2) : null,
                MenuCostPercent = costVisible && estRevenue > 0 ? Math.Round((totalCost / estRevenue) * 100m, 2) : null,
                MenuBudget = costVisible ? budget : null,
                RemainingMenuBudget = costVisible ? budget - totalCost : null,
                BudgetExceeded = totalCost > budget,
                Warnings = warnings,
                SimulatedLines = simLines
            };
        }

        public async Task<SilaCulinaryMenuDto> CreateScenarioAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string scenarioName, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            if (!ev.PrimaryMenuId.HasValue) throw new BadRequestCustomException("No primary menu.", "Event has no primary menu to clone into a scenario.");

            SilaCulinaryMenu src = await _repository.SilaCulinaryMenu
                .FindByCondition(x => x.BuyerId == buyer.Id && x.Id == ev.PrimaryMenuId.Value && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Menu not found.", "Primary menu not found.");

            string code = await DocumentNumber.NextAsync(_repository, buyer.Id, DocumentNumber.CULINARY_MENU, 6, cancellationToken);
            Guid newId = Guid.NewGuid();
            SilaCulinaryMenu scenario = new()
            {
                Id = newId,
                BuyerId = buyer.Id,
                MenuCode = code,
                EventId = ev.Id,
                Name = $"{ev.Name} — {scenarioName}",
                MenuCategory = src.MenuCategory,
                Cuisine = src.Cuisine,
                PropertyId = src.PropertyId,
                ServiceStyle = src.ServiceStyle,
                VersionNumber = 1,
                EffectiveDateUtc = src.EffectiveDateUtc,
                Status = "DRAFT",
                IsScenario = true,
                ScenarioName = scenarioName.Trim(),
                Currency = src.Currency,
                TargetSellingPrice = src.TargetSellingPrice,
                TargetFoodCostPercent = src.TargetFoodCostPercent,
                TotalFoodCost = src.TotalFoodCost,
                TotalBeverageCost = src.TotalBeverageCost,
                TotalEstimatedCost = src.TotalEstimatedCost,
                CostPerGuest = src.CostPerGuest,
                IsActive = true
            };
            _repository.SilaCulinaryMenu.Create(scenario);

            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == src.Id && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (SilaCulinaryMenuLine l in lines)
            {
                _repository.SilaCulinaryMenuLine.Create(new SilaCulinaryMenuLine
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MenuId = newId,
                    CourseName = l.CourseName,
                    SortOrder = l.SortOrder,
                    ItemRole = l.ItemRole,
                    IsLocked = l.IsLocked,
                    RecipeId = l.RecipeId,
                    RecipeCode = l.RecipeCode,
                    RecipeVersion = l.RecipeVersion,
                    DishName = l.DishName,
                    LineStatus = l.LineStatus,
                    ServiceStyle = l.ServiceStyle,
                    UptakePercent = l.UptakePercent,
                    ServingFrequency = l.ServingFrequency,
                    PortionSize = l.PortionSize,
                    PortionUom = l.PortionUom,
                    AllocatedPortions = l.AllocatedPortions,
                    PlannedPortions = l.PlannedPortions,
                    CostingSnapshotId = l.CostingSnapshotId,
                    UnitCostPerPortion = l.UnitCostPerPortion,
                    EstimatedLineCost = l.EstimatedLineCost,
                    DietaryTagsJson = l.DietaryTagsJson,
                    VerifiedAllergensJson = l.VerifiedAllergensJson,
                    AllergenUnknown = l.AllergenUnknown,
                    AllergenVerified = l.AllergenVerified,
                    IsBeverage = l.IsBeverage,
                    ServingsPerGuest = l.ServingsPerGuest,
                    ServingVolumeMl = l.ServingVolumeMl,
                    IsActive = true
                });
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_SCENARIO_CREATED,
                $"Created menu scenario '{scenarioName}' ({code}) for event {ev.EventCode}.", userId);
            await _repository.SaveAsync();
            return await BuildMenuDtoAsync(buyer.Id, scenario, CanViewCosts(roleId), cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> SelectScenarioAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, Guid scenarioMenuId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);
            if (ev.Status is "APPROVED" or "READY_FOR_MRP")
            {
                throw new BadRequestCustomException("Approved baseline locked.", "Cannot switch menu scenario on an approved event without creating a new revision.");
            }
            SilaCulinaryMenu scenario = await _repository.SilaCulinaryMenu
                .FindByConditionTracked(x => x.BuyerId == buyer.Id && x.EventId == ev.Id && x.Id == scenarioMenuId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundCustomException("Scenario not found.", "Scenario menu not found for this event.");

            scenario.IsSelectedScenario = true;
            ev.PrimaryMenuId = scenario.Id;
            await RecalculateMenuTotalsInternalAsync(buyer.Id, ev, scenario, cancellationToken);
            await _repository.SaveAsync();
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        // ==================== CUSTOMER MENU, READINESS, APPROVALS, REVISIONS, REPORTS & PHASE 2B HANDOVER ====================

        public async Task<SilaCulinaryCustomerMenuPreviewDto> GetCustomerMenuPreviewAsync(
            Guid organizationId, Guid eventId, CancellationToken cancellationToken)
        {
            // Strictly excludes internal material codes, supplier prices, unit costs, and margins (§37)
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            string? propertyName = ev.PropertyId.HasValue
                ? await _repository.BuyerProperty.FindByCondition(x => x.Id == ev.PropertyId.Value).Select(x => x.PropertyName).FirstOrDefaultAsync(cancellationToken)
                : null;

            SilaCulinaryMenu? menu = ev.PrimaryMenuId.HasValue
                ? await _repository.SilaCulinaryMenu.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == ev.PrimaryMenuId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken)
                : null;

            List<SilaCulinaryMenuLine> lines = menu == null
                ? new()
                : await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.IsActive)
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync(cancellationToken);

            List<SilaCulinaryCustomerMenuCourseDto> courses = lines
                .GroupBy(l => l.CourseName)
                .Select(g => new SilaCulinaryCustomerMenuCourseDto
                {
                    CourseName = g.Key,
                    Dishes = g.Select(d => new SilaCulinaryCustomerMenuDishDto
                    {
                        DishName = d.DishName,
                        Description = d.CustomerDescription,
                        ImageUrl = d.DishImageUrl,
                        DietaryTags = ParseJsonStringList(d.DietaryTagsJson),
                        AllergenDisclosures = ParseJsonStringList(d.VerifiedAllergensJson),
                        AllergenPendingVerification = d.AllergenUnknown
                    }).ToList()
                })
                .ToList();

            return new SilaCulinaryCustomerMenuPreviewDto
            {
                EventName = ev.Name,
                PropertyName = propertyName,
                VenueName = ev.VenueName,
                StartDateUtc = ev.StartDateUtc,
                MenuTitle = menu?.Name ?? $"{ev.Name} Menu",
                ServiceStyle = ev.ServiceStyle,
                IsProposedOnly = ev.Status is not ("APPROVED" or "READY_FOR_MRP"),
                Courses = courses
            };
        }

        public async Task<SilaCulinaryEventReadinessDto> GetEventReadinessAsync(
            Guid organizationId, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);

            List<SilaCulinaryMenuLine> lines = ev.PrimaryMenuId.HasValue
                ? await _repository.SilaCulinaryMenuLine
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == ev.PrimaryMenuId.Value && x.IsActive)
                    .ToListAsync(cancellationToken)
                : new();

            List<SilaCulinaryEventReadinessItemDto> checks = new();
            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "HEADER_SCHEDULE",
                Label = "Event Header & Schedule",
                Status = !string.IsNullOrWhiteSpace(ev.Name) && ev.EndDateUtc >= ev.StartDateUtc ? "PASSED" : "BLOCKED",
                Detail = $"{ev.EventCode}: {ev.StartDateUtc:yyyy-MM-dd} to {ev.EndDateUtc:yyyy-MM-dd} ({ev.TimeZoneId})"
            });

            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "GUESTS",
                Label = "Guest Planning",
                Status = ev.PlanningGuests > 0 ? "PASSED" : "BLOCKED",
                Detail = $"Guaranteed: {ev.GuaranteedGuests}, Planning: {ev.PlanningGuests}, Billable: {ev.BillableGuests}"
            });

            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "MENU_COMPLETENESS",
                Label = "Menu Composition",
                Status = lines.Count > 0 ? "PASSED" : "BLOCKED",
                Detail = lines.Count > 0 ? $"{lines.Count} menu item(s) configured." : "Menu has no recipe lines.",
                RecommendedAction = lines.Count == 0 ? "Add published recipes in Menu Studio." : null
            });

            int unresolvedRecipes = lines.Count(l => l.ItemRole == "MANDATORY" && (l.LineStatus != "PUBLISHED" || l.RecipeId == null));
            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "PUBLISHED_RECIPES",
                Label = "Published Recipe Eligibility",
                Status = unresolvedRecipes == 0 ? "PASSED" : "BLOCKED",
                Detail = unresolvedRecipes == 0 ? "All mandatory dishes reference published Recipe IDs & versions." : $"{unresolvedRecipes} mandatory line(s) reference unpublished/missing recipes.",
                RecommendedAction = unresolvedRecipes > 0 ? "Replace placeholders or publish linked Phase 1A drafts." : null
            });

            bool hasDietaryRequirements = !string.IsNullOrWhiteSpace(ev.DietaryRequirementsJson) && ev.DietaryRequirementsJson != "[]";
            int unknownAllergens = lines.Count(l => l.AllergenUnknown);
            string allergenStatus = hasDietaryRequirements && (unknownAllergens > 0 || !ev.DietaryReviewVerified)
                ? "BLOCKED"
                : unknownAllergens > 0 ? "WARNING" : "PASSED";
            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "DIETARY_ALLERGENS",
                Label = "Dietary & Allergen Safety",
                Status = allergenStatus,
                Detail = $"Dietary review verified: {ev.DietaryReviewVerified}. Items with unknown allergen data: {unknownAllergens}.",
                RecommendedAction = allergenStatus != "PASSED" ? "Complete allergen verification before approval." : null
            });

            int missingCosts = lines.Count(l => !l.UnitCostPerPortion.HasValue);
            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "MENU_COSTING",
                Label = "Menu Cost Completeness",
                Status = missingCosts == 0 && lines.Count > 0 ? "PASSED" : "WARNING",
                Detail = missingCosts == 0 ? "All menu items have verified recipe portion costs." : $"{missingCosts} menu item(s) missing verified cost."
            });

            decimal totalCost = lines.Sum(l => l.EstimatedLineCost ?? 0m);
            decimal? budget = ev.CombinedMenuBudget ?? ev.ApprovedBudget;
            bool overBudget = budget.HasValue && budget.Value > 0 && totalCost > budget.Value;
            checks.Add(new SilaCulinaryEventReadinessItemDto
            {
                CheckCode = "BUDGET_CHECK",
                Label = "Budget Validation",
                Status = overBudget ? "WARNING" : "PASSED",
                Detail = budget.HasValue ? $"Estimated cost {totalCost:F2} vs budget {budget.Value:F2} {ev.Currency}." : "No fixed budget cap configured."
            });

            int blocked = checks.Count(c => c.Status == "BLOCKED");
            int warn = checks.Count(c => c.Status == "WARNING");
            int pass = checks.Count(c => c.Status == "PASSED");

            return new SilaCulinaryEventReadinessDto
            {
                EventId = ev.Id,
                EventCode = ev.EventCode,
                ReadyForApproval = blocked == 0,
                ReadyForMrp = blocked == 0 && ev.Status is "APPROVED" or "READY_FOR_MRP",
                PassedCount = pass,
                WarningCount = warn,
                BlockedCount = blocked,
                Checks = checks
            };
        }

        public async Task<SilaCulinaryEventDto> SubmitEventForApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);
            SilaCulinaryEventReadinessDto readiness = await GetEventReadinessAsync(organizationId, eventId, cancellationToken);
            if (!readiness.ReadyForApproval)
            {
                string blockers = string.Join("; ", readiness.Checks.Where(c => c.Status == "BLOCKED").Select(c => c.Detail));
                throw new BadRequestCustomException("Event is not ready for approval.", blockers);
            }

            ev.Status = "PENDING_APPROVAL";
            ev.SubmittedByUserId = userId;
            ev.SubmittedOnUtc = DateTime.UtcNow;

            bool hasFlow = await _repository.MasterApprovalFlow
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.Type != null && x.Type.ToUpper() == Common.SILA_APPROVAL_TYPE_EVENT_PLAN)
                .AnyAsync(cancellationToken);

            if (hasFlow)
            {
                await SilaApprovals.StartAsync(
                    _repository, _logger, buyer.Id, Common.SILA_APPROVAL_TYPE_EVENT_PLAN,
                    Common.SILA_REF_CULINARY_EVENT, ev.Id, ev.RevisionNumber,
                    new ApprovalScope { PropertyId = ev.PropertyId, CompanyCode = ev.CompanyCode }, cancellationToken);
            }
            else
            {
                await SilaOperations.UpsertTaskAsync(_repository, buyer.Id, userId, new SilaOperations.TaskDraft
                {
                    TaskType = "EVENT_PLAN_APPROVAL",
                    DeduplicationKey = $"EVT_APPR_{ev.Id}_{ev.RevisionNumber}",
                    Title = $"Approve Event Plan {ev.EventCode}: {ev.Name}",
                    Description = $"{ev.EventType} ({ev.PlanningGuests} guests) submitted by {actorName}.",
                    Priority = SilaOperations.PRIORITY_ACTION,
                    SourceModule = "CULINARY_INTELLIGENCE",
                    SourceDocumentType = Common.SILA_REF_CULINARY_EVENT,
                    SourceDocumentId = ev.Id,
                    SourceDocumentNumber = ev.EventCode,
                    AssignedPermission = "APPROVE_SILA_RECIPE"
                }, cancellationToken);
            }

            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_SUBMITTED,
                $"Submitted event {ev.EventCode} (Rev {ev.RevisionNumber}) for approval.", userId);
            await _repository.SaveAsync();
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> DecideEventApprovalAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string decision, string? comment, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);
            if (ev.Status != "PENDING_APPROVAL")
            {
                throw new BadRequestCustomException("Invalid event status.", $"Event {ev.EventCode} is in status {ev.Status}, not PENDING_APPROVAL.");
            }
            if (ev.SubmittedByUserId.HasValue && ev.SubmittedByUserId.Value == userId)
            {
                throw new ForBiddenCustomException("Separation of duties.", "Submitter cannot approve their own event.");
            }

            string norm = (decision ?? "").Trim().ToUpperInvariant();
            if (norm is not ("APPROVE" or "REJECT" or "RETURN"))
            {
                throw new BadRequestCustomException("Invalid approval decision.", "Decision must be APPROVE, REJECT, or RETURN.");
            }

            bool hasSteps = await _repository.SilaApprovalStep
                .FindByCondition(x => x.BuyerId == buyer.Id && x.ReferenceType == Common.SILA_REF_CULINARY_EVENT && x.ReferenceId == ev.Id && x.Version == ev.RevisionNumber && x.IsActive && x.Status == Common.SILA_APPROVAL_PENDING)
                .AnyAsync(cancellationToken);

            if (hasSteps)
            {
                ApprovalDecision res = await SilaApprovals.DecideAsync(
                    _repository, _logger, Common.SILA_REF_CULINARY_EVENT, ev.Id, ev.RevisionNumber, userId, norm == "APPROVE", comment, cancellationToken);
                if (norm == "APPROVE" && res.Outcome != SilaApprovals.OUTCOME_APPROVED)
                {
                    await _repository.SaveAsync();
                    return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
                }
            }

            if (norm == "APPROVE")
            {
                ev.Status = "READY_FOR_MRP";
                ev.ApprovedRevisionNumber = ev.RevisionNumber;
                ev.ApprovedByUserId = userId;
                ev.ApprovedOnUtc = DateTime.UtcNow;
                ev.ApprovalReference = $"APPR-{ev.EventCode}-R{ev.RevisionNumber}";
                ev.ApprovalDecisionComment = comment;

                SilaCulinaryEventDto baselineSnap = await BuildEventDtoAsync(buyer.Id, ev, new List<SilaCulinaryEvent> { ev }, true, includeDetails: true, cancellationToken);
                string snapJson = JsonSerializer.Serialize(baselineSnap, CamelJson);
                ev.ApprovedBaselineJson = snapJson;

                _repository.SilaCulinaryEventRevision.Create(new SilaCulinaryEventRevision
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    EventId = ev.Id,
                    RevisionNumber = ev.RevisionNumber,
                    StatusAtRevision = ev.Status,
                    IsApprovedBaseline = true,
                    ChangeReason = "Approved baseline established",
                    GuaranteedGuestsAtRevision = ev.GuaranteedGuests,
                    PlanningGuestsAtRevision = ev.PlanningGuests,
                    MenuCostAtRevision = baselineSnap.TotalEstimatedMenuCost ?? 0m,
                    ApprovalReference = ev.ApprovalReference,
                    SnapshotJson = snapJson,
                    CreatedByName = actorName,
                    IsActive = true
                });

                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_APPROVED,
                    $"Approved event {ev.EventCode} baseline (Rev {ev.RevisionNumber}, Ref {ev.ApprovalReference}).", userId);
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_READY_MRP,
                    $"Event {ev.EventCode} baseline ready for Phase 2B Event MRP handover.", userId);
            }
            else if (norm == "RETURN")
            {
                ev.Status = "RETURNED_FOR_CORRECTION";
                ev.ApprovalDecisionComment = comment;
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_RETURNED,
                    $"Returned event {ev.EventCode} for correction: {comment}", userId);
            }
            else
            {
                ev.Status = "RETURNED_FOR_CORRECTION";
                ev.ApprovalDecisionComment = comment;
                AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_REJECTED,
                    $"Rejected event {ev.EventCode}: {comment}", userId);
            }

            await _repository.SaveAsync();
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<SilaCulinaryEventDto> CreateRevisionAsync(
            Guid organizationId, Guid userId, Guid roleId, string actorName, Guid eventId, string changeReason, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: true, cancellationToken);

            // Snapshot current state before incrementing revision (§44, §45)
            SilaCulinaryEventDto snap = await BuildEventDtoAsync(buyer.Id, ev, new List<SilaCulinaryEvent> { ev }, true, includeDetails: true, cancellationToken);
            bool alreadyStored = await _repository.SilaCulinaryEventRevision
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventId == ev.Id && x.RevisionNumber == ev.RevisionNumber && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!alreadyStored)
            {
                _repository.SilaCulinaryEventRevision.Create(new SilaCulinaryEventRevision
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    EventId = ev.Id,
                    RevisionNumber = ev.RevisionNumber,
                    StatusAtRevision = ev.Status,
                    IsApprovedBaseline = ev.Status is "APPROVED" or "READY_FOR_MRP",
                    ChangeReason = "Pre-change revision snapshot",
                    GuaranteedGuestsAtRevision = ev.GuaranteedGuests,
                    PlanningGuestsAtRevision = ev.PlanningGuests,
                    MenuCostAtRevision = snap.TotalEstimatedMenuCost ?? 0m,
                    ApprovalReference = ev.ApprovalReference,
                    SnapshotJson = JsonSerializer.Serialize(snap, CamelJson),
                    CreatedByName = actorName,
                    IsActive = true
                });
            }

            ev.RevisionNumber += 1;
            ev.Status = "REVISION_REQUIRED";
            AddWorkflowEvent(buyer.Id, Common.SILA_REF_CULINARY_EVENT, ev.Id, Common.SILA_AUDIT_CULINARY_EVENT_REVISION,
                $"Created revision {ev.RevisionNumber} for {ev.EventCode}: {changeReason}", userId);
            await _repository.SaveAsync();
            return await GetEventAsync(organizationId, userId, roleId, ev.Id, cancellationToken);
        }

        public async Task<List<SilaCulinaryEventRevisionDto>> ListRevisionsAsync(
            Guid organizationId, Guid roleId, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            bool costVisible = CanViewCosts(roleId);
            return await _repository.SilaCulinaryEventRevision
                .FindByCondition(x => x.BuyerId == buyer.Id && x.EventId == eventId && x.IsActive)
                .OrderByDescending(x => x.RevisionNumber)
                .Select(r => new SilaCulinaryEventRevisionDto
                {
                    Id = r.Id,
                    RevisionNumber = r.RevisionNumber,
                    StatusAtRevision = r.StatusAtRevision,
                    IsApprovedBaseline = r.IsApprovedBaseline,
                    ChangeReason = r.ChangeReason,
                    GuaranteedGuestsAtRevision = r.GuaranteedGuestsAtRevision,
                    PlanningGuestsAtRevision = r.PlanningGuestsAtRevision,
                    MenuCostAtRevision = costVisible ? r.MenuCostAtRevision : null,
                    ApprovalReference = r.ApprovalReference,
                    CreatedByName = r.CreatedByName,
                    CreatedOn = r.DateCreated
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SilaCulinaryEventReportResultDto> ExportReportAsync(
            Guid organizationId, Guid roleId, Guid eventId, string reportCode, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            bool costVisible = CanViewCosts(roleId);
            string code = (reportCode ?? "EVENT_SUMMARY").Trim().ToUpperInvariant();

            StringBuilder sb = new();
            int rowCount = 0;

            if (code == "CUSTOMER_MENU")
            {
                SilaCulinaryCustomerMenuPreviewDto cust = await GetCustomerMenuPreviewAsync(organizationId, eventId, cancellationToken);
                sb.AppendLine("Course,DishName,Description,DietaryTags,AllergenDisclosures");
                foreach (SilaCulinaryCustomerMenuCourseDto c in cust.Courses)
                {
                    foreach (SilaCulinaryCustomerMenuDishDto d in c.Dishes)
                    {
                        rowCount++;
                        sb.AppendLine(string.Join(",",
                            CsvSafe(c.CourseName),
                            CsvSafe(d.DishName),
                            CsvSafe(d.Description ?? ""),
                            CsvSafe(string.Join("; ", d.DietaryTags)),
                            CsvSafe(d.AllergenPendingVerification ? "Pending verification" : string.Join("; ", d.AllergenDisclosures))));
                    }
                }
            }
            else if (code == "MATERIAL_PREVIEW")
            {
                List<SilaCulinaryMaterialPreviewLineDto> mats = await GetPreliminaryMaterialPreviewAsync(organizationId, eventId, cancellationToken);
                sb.AppendLine("Dish,RecipeCode,Ingredient,MaterialCode,RequiredQty,RecipeUom,BaseQty,BaseUom,OnHandStock,StockIndicator");
                foreach (SilaCulinaryMaterialPreviewLineDto m in mats)
                {
                    rowCount++;
                    sb.AppendLine(string.Join(",",
                        CsvSafe(m.DishName), CsvSafe(m.RecipeCode ?? ""), CsvSafe(m.IngredientName),
                        CsvSafe(m.MaterialCode ?? ""), m.RequiredQty, CsvSafe(m.RecipeUom),
                        m.BaseQty?.ToString() ?? "", CsvSafe(m.BaseUom ?? ""),
                        m.OnHandStock?.ToString() ?? "", CsvSafe(m.StockIndicator)));
                }
            }
            else
            {
                SilaCulinaryMenu? menu = ev.PrimaryMenuId.HasValue
                    ? await _repository.SilaCulinaryMenu.FindByCondition(x => x.BuyerId == buyer.Id && x.Id == ev.PrimaryMenuId.Value && x.IsActive).FirstOrDefaultAsync(cancellationToken)
                    : null;
                List<SilaCulinaryMenuLine> lines = menu == null
                    ? new()
                    : await _repository.SilaCulinaryMenuLine.FindByCondition(x => x.BuyerId == buyer.Id && x.MenuId == menu.Id && x.IsActive).OrderBy(x => x.SortOrder).ToListAsync(cancellationToken);

                sb.AppendLine("EventCode,EventName,Course,DishName,RecipeCode,RecipeVersion,ServiceStyle,UptakePct,PlannedPortions,UnitCost,LineCost,Allergens");
                foreach (SilaCulinaryMenuLine l in lines)
                {
                    rowCount++;
                    sb.AppendLine(string.Join(",",
                        CsvSafe(ev.EventCode), CsvSafe(ev.Name), CsvSafe(l.CourseName), CsvSafe(l.DishName),
                        CsvSafe(l.RecipeCode ?? "RECIPE_REQUIRED"), l.RecipeVersion?.ToString() ?? "",
                        CsvSafe(l.ServiceStyle), l.UptakePercent, l.PlannedPortions,
                        costVisible ? (l.UnitCostPerPortion?.ToString("F2") ?? "") : "RESTRICTED",
                        costVisible ? (l.EstimatedLineCost?.ToString("F2") ?? "") : "RESTRICTED",
                        CsvSafe(l.AllergenUnknown ? "UNKNOWN" : string.Join("; ", ParseJsonStringList(l.VerifiedAllergensJson)))));
                }
            }

            return new SilaCulinaryEventReportResultDto
            {
                ReportCode = code,
                FileName = $"{ev.EventCode}_{code.ToLowerInvariant()}.csv",
                ContentType = "text/csv",
                Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(sb.ToString())),
                RowCount = rowCount
            };
        }

        public async Task<SilaCulinaryPhase2BHandoverDto> GetPhase2BHandoverAsync(
            Guid organizationId, Guid roleId, Guid eventId, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await ResolveBuyerAsync(organizationId, cancellationToken);
            SilaCulinaryEvent ev = await LoadEventOrThrowAsync(buyer.Id, eventId, tracked: false, cancellationToken);
            SilaCulinaryEventDto dto = await BuildEventDtoAsync(buyer.Id, ev, new List<SilaCulinaryEvent> { ev }, CanViewCosts(roleId), includeDetails: true, cancellationToken);
            SilaCulinaryEventReadinessDto readiness = await GetEventReadinessAsync(organizationId, eventId, cancellationToken);
            List<SilaCulinaryMaterialPreviewLineDto> matPreview = await GetPreliminaryMaterialPreviewAsync(organizationId, eventId, cancellationToken);

            return new SilaCulinaryPhase2BHandoverDto
            {
                EventId = ev.Id,
                EventCode = ev.EventCode,
                EventName = ev.Name,
                EventRevision = ev.RevisionNumber,
                ApprovedRevision = ev.ApprovedRevisionNumber,
                PropertyId = ev.PropertyId,
                PlantCode = ev.PlantCode,
                VenueName = ev.VenueName,
                KitchenLocationId = ev.KitchenLocationId,
                StartDateUtc = ev.StartDateUtc,
                EndDateUtc = ev.EndDateUtc,
                MrpDeadlineUtc = ev.MrpDeadlineUtc,
                GuaranteedGuests = ev.GuaranteedGuests,
                PlanningGuests = ev.PlanningGuests,
                Currency = ev.Currency,
                EventMenuBudget = dto.CombinedMenuBudget,
                TotalEstimatedMenuCost = dto.TotalEstimatedMenuCost,
                ApprovalReference = ev.ApprovalReference,
                ApprovalStatus = ev.Status,
                BaselineTimestampUtc = ev.ApprovedOnUtc,
                DietaryVerified = ev.DietaryReviewVerified,
                MrpReady = readiness.ReadyForMrp,
                OutstandingWarnings = readiness.Checks.Where(c => c.Status != "PASSED").Select(c => c.Detail).ToList(),
                Sessions = dto.Sessions,
                PinnedMenuLines = dto.PrimaryMenu?.Lines ?? new(),
                PreliminaryMaterialRequirements = matPreview
            };
        }

        // ==================== INTERNAL HELPERS ====================

        private async Task<SilaCulinaryEvent> LoadEventOrThrowAsync(Guid buyerId, Guid eventId, bool tracked, CancellationToken cancellationToken)
        {
            IQueryable<SilaCulinaryEvent> q = tracked
                ? _repository.SilaCulinaryEvent.FindByConditionTracked(x => x.BuyerId == buyerId && x.Id == eventId && x.IsActive)
                : _repository.SilaCulinaryEvent.FindByCondition(x => x.BuyerId == buyerId && x.Id == eventId && x.IsActive);
            return await q.FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundCustomException("Event not found.", "Culinary event not found.");
        }

        private static void ApplyEventFields(SilaCulinaryEvent ev, SilaCulinaryUpsertEventRequestDto req)
        {
            ev.Name = req.Name.Trim();
            ev.Description = req.Description;
            ev.EventType = string.IsNullOrWhiteSpace(req.EventType) ? "Corporate Gala" : req.EventType.Trim();
            ev.EventCategory = req.EventCategory;
            ev.CustomerReference = req.CustomerReference;
            ev.CustomerName = req.CustomerName;
            ev.BookingReference = req.BookingReference;
            ev.ExternalEventReference = req.ExternalEventReference;
            ev.EventCoordinator = req.EventCoordinator;
            ev.ResponsibleChef = req.ResponsibleChef;
            ev.ExecutiveChef = req.ExecutiveChef;
            ev.BanquetManager = req.BanquetManager;
            ev.PropertyId = req.PropertyId;
            ev.CompanyCode = req.CompanyCode;
            ev.PlantCode = req.PlantCode;
            ev.VenueName = req.VenueName;
            ev.OutletLocationId = req.OutletLocationId;
            ev.KitchenLocationId = req.KitchenLocationId;
            if (req.StorageLocationCodes != null) ev.StorageLocationCodesJson = JsonSerializer.Serialize(req.StorageLocationCodes, CamelJson);
            ev.StartDateUtc = req.StartDateUtc;
            ev.EndDateUtc = req.EndDateUtc;
            ev.StartTime = req.StartTime;
            ev.EndTime = req.EndTime;
            ev.PrepStartUtc = req.PrepStartUtc;
            ev.MenuConfirmationDeadlineUtc = req.MenuConfirmationDeadlineUtc;
            ev.GuestConfirmationDeadlineUtc = req.GuestConfirmationDeadlineUtc;
            ev.MrpDeadlineUtc = req.MrpDeadlineUtc;
            ev.TimeZoneId = string.IsNullOrWhiteSpace(req.TimeZoneId) ? "Asia/Dubai" : req.TimeZoneId.Trim();
            ev.GuaranteedGuests = Math.Max(0, req.GuaranteedGuests);
            ev.ExpectedGuests = Math.Max(0, req.ExpectedGuests);
            ev.MaxCapacity = req.MaxCapacity;
            ev.AdditionalAllowancePercent = Math.Clamp(req.AdditionalAllowancePercent, 0m, 100m);
            ev.BillablePolicy = string.IsNullOrWhiteSpace(req.BillablePolicy) ? "GUARANTEED_ONLY" : req.BillablePolicy.Trim().ToUpperInvariant();
            ev.AdultsCount = Math.Max(0, req.AdultsCount);
            ev.ChildrenCount = Math.Max(0, req.ChildrenCount);
            ev.VipCount = Math.Max(0, req.VipCount);
            ev.StaffMealsCount = Math.Max(0, req.StaffMealsCount);
            ev.Currency = string.IsNullOrWhiteSpace(req.Currency) ? "AED" : req.Currency.Trim().ToUpperInvariant();
            ev.NetPricePerGuest = Math.Max(0m, req.NetPricePerGuest);
            ev.FixedPackagePrice = req.FixedPackagePrice;
            ev.TargetFoodCostPercent = req.TargetFoodCostPercent > 0 ? req.TargetFoodCostPercent : 28m;
            ev.TargetBeverageCostPercent = req.TargetBeverageCostPercent > 0 ? req.TargetBeverageCostPercent : 22m;
            ev.CombinedMenuBudget = req.CombinedMenuBudget;
            ev.ApprovedBudget = req.ApprovedBudget;
            ev.ServiceStyle = string.IsNullOrWhiteSpace(req.ServiceStyle) ? "BUFFET" : req.ServiceStyle.Trim().ToUpperInvariant();
            ev.WizardStepCompleted = Math.Clamp(req.WizardStepCompleted, 1, 10);
            if (req.DietaryRequirements != null)
            {
                ev.DietaryRequirementsJson = JsonSerializer.Serialize(req.DietaryRequirements, CamelJson);
                ev.DietaryReviewVerified = false;
            }
        }

        private static void RecalculateGuestAndBudgetMetrics(SilaCulinaryEvent ev)
        {
            int planning = (int)Math.Ceiling(ev.GuaranteedGuests * (1m + ev.AdditionalAllowancePercent / 100m));
            if (ev.AdditionalAllowancePercent == 0m && ev.ExpectedGuests > planning)
            {
                planning = ev.ExpectedGuests;
            }
            ev.PlanningGuests = planning;
            ev.BillableGuests = ev.BillablePolicy == "EXPECTED_OR_GUARANTEED" ? Math.Max(ev.GuaranteedGuests, planning) : ev.GuaranteedGuests;

            decimal totalRev = ev.FixedPackagePrice.HasValue && ev.FixedPackagePrice.Value > 0
                ? ev.FixedPackagePrice.Value
                : ev.BillableGuests * ev.NetPricePerGuest;
            ev.EstimatedFoodRevenue = Math.Round(totalRev, 2);
            if (!ev.CombinedMenuBudget.HasValue && totalRev > 0)
            {
                ev.CombinedMenuBudget = Math.Round(totalRev * (ev.TargetFoodCostPercent / 100m), 2);
            }
        }

        private static int ComputePlannedPortions(
            int planningGuests, bool isBeverage, string serviceStyle, decimal uptakePct, decimal servingFreq, decimal servingsPerGuest)
        {
            if (isBeverage || serviceStyle is "BEVERAGE_RECEPTION" or "COFFEE_BREAK")
            {
                return (int)Math.Ceiling(planningGuests * servingsPerGuest * (uptakePct / 100m));
            }
            return (int)Math.Ceiling(planningGuests * (uptakePct / 100m) * servingFreq);
        }

        private async Task RecalculateMenuTotalsInternalAsync(
            Guid buyerId, SilaCulinaryEvent? ev, SilaCulinaryMenu menu, CancellationToken cancellationToken)
        {
            int guests = ev?.PlanningGuests ?? 100;
            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByConditionTracked(x => x.BuyerId == buyerId && x.MenuId == menu.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            decimal foodCost = 0m;
            decimal bevCost = 0m;
            foreach (SilaCulinaryMenuLine l in lines)
            {
                int portions = l.ManualPortionsOverride
                    ?? (l.ServiceStyle == "PLATED" && l.AllocatedPortions.HasValue
                        ? l.AllocatedPortions.Value
                        : ComputePlannedPortions(guests, l.IsBeverage, l.ServiceStyle, l.UptakePercent, l.ServingFrequency, l.ServingsPerGuest));
                l.PlannedPortions = portions;
                l.EstimatedLineCost = l.UnitCostPerPortion.HasValue ? Math.Round(portions * l.UnitCostPerPortion.Value, 2) : null;
                if (l.EstimatedLineCost.HasValue)
                {
                    if (l.IsBeverage) bevCost += l.EstimatedLineCost.Value;
                    else foodCost += l.EstimatedLineCost.Value;
                }
            }

            menu.TotalFoodCost = Math.Round(foodCost, 2);
            menu.TotalBeverageCost = Math.Round(bevCost, 2);
            menu.TotalEstimatedCost = Math.Round(foodCost + bevCost, 2);
            menu.CostPerGuest = guests > 0 ? Math.Round(menu.TotalEstimatedCost / guests, 2) : 0m;
        }

        private async Task<SilaCulinaryEventDto> BuildEventDtoAsync(
            Guid buyerId, SilaCulinaryEvent ev, List<SilaCulinaryEvent> activeEvents, bool costVisible, bool includeDetails, CancellationToken cancellationToken)
        {
            List<string> conflicts = activeEvents
                .Where(other => other.Id != ev.Id
                    && other.Status != "CANCELLED"
                    && !string.IsNullOrWhiteSpace(ev.VenueName)
                    && string.Equals(other.VenueName, ev.VenueName, StringComparison.OrdinalIgnoreCase)
                    && other.PropertyId == ev.PropertyId
                    && other.StartDateUtc.Date <= ev.EndDateUtc.Date
                    && other.EndDateUtc.Date >= ev.StartDateUtc.Date)
                .Select(x => x.EventCode)
                .ToList();

            decimal? totalMenuCost = null;
            SilaCulinaryMenuDto? primaryMenuDto = null;
            if (ev.PrimaryMenuId.HasValue)
            {
                SilaCulinaryMenu? m = await _repository.SilaCulinaryMenu
                    .FindByCondition(x => x.BuyerId == buyerId && x.Id == ev.PrimaryMenuId.Value && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (m != null)
                {
                    totalMenuCost = m.TotalEstimatedCost;
                    if (includeDetails) primaryMenuDto = await BuildMenuDtoAsync(buyerId, m, costVisible, cancellationToken);
                }
            }

            List<SilaCulinaryEventSessionDto> sessions = includeDetails
                ? await _repository.SilaCulinaryEventSession
                    .FindByCondition(x => x.BuyerId == buyerId && x.EventId == ev.Id && x.IsActive)
                    .OrderBy(x => x.DayNumber).ThenBy(x => x.SortOrder)
                    .Select(s => new SilaCulinaryEventSessionDto
                    {
                        Id = s.Id,
                        EventId = s.EventId,
                        SessionCode = s.SessionCode,
                        DayNumber = s.DayNumber,
                        SessionDateUtc = s.SessionDateUtc,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        SessionName = s.SessionName,
                        VenueName = s.VenueName,
                        KitchenLocationId = s.KitchenLocationId,
                        ServiceStyle = s.ServiceStyle,
                        GuestCount = s.GuestCount,
                        MenuId = s.MenuId,
                        BudgetAllocation = costVisible ? s.BudgetAllocation : null,
                        PrepDeadlineUtc = s.PrepDeadlineUtc,
                        DietaryNotesJson = s.DietaryNotesJson,
                        SortOrder = s.SortOrder
                    })
                    .ToListAsync(cancellationToken)
                : new();

            List<SilaCulinaryMenuDto> scenarios = new();
            if (includeDetails)
            {
                List<SilaCulinaryMenu> scMenus = await _repository.SilaCulinaryMenu
                    .FindByCondition(x => x.BuyerId == buyerId && x.EventId == ev.Id && x.IsScenario && x.IsActive)
                    .ToListAsync(cancellationToken);
                foreach (SilaCulinaryMenu sm in scMenus)
                {
                    scenarios.Add(await BuildMenuDtoAsync(buyerId, sm, costVisible, cancellationToken));
                }
            }

            decimal totalRev = ev.EstimatedFoodRevenue + ev.EstimatedBeverageRevenue + ev.OtherPlannedRevenue;
            decimal? budget = ev.CombinedMenuBudget ?? ev.ApprovedBudget;
            decimal? pct = totalRev > 0 && totalMenuCost.HasValue ? Math.Round((totalMenuCost.Value / totalRev) * 100m, 2) : null;

            return new SilaCulinaryEventDto
            {
                Id = ev.Id,
                EventCode = ev.EventCode,
                Name = ev.Name,
                Description = ev.Description,
                EventType = ev.EventType,
                EventCategory = ev.EventCategory,
                CustomerReference = ev.CustomerReference,
                CustomerName = ev.CustomerName,
                BookingReference = ev.BookingReference,
                ExternalEventReference = ev.ExternalEventReference,
                EventCoordinator = ev.EventCoordinator,
                ResponsibleChef = ev.ResponsibleChef,
                ExecutiveChef = ev.ExecutiveChef,
                BanquetManager = ev.BanquetManager,
                Status = ev.Status,
                RevisionNumber = ev.RevisionNumber,
                ApprovedRevisionNumber = ev.ApprovedRevisionNumber,
                PropertyId = ev.PropertyId,
                CompanyCode = ev.CompanyCode,
                PlantCode = ev.PlantCode,
                VenueName = ev.VenueName,
                OutletLocationId = ev.OutletLocationId,
                KitchenLocationId = ev.KitchenLocationId,
                StorageLocationCodes = ParseJsonStringList(ev.StorageLocationCodesJson),
                StartDateUtc = ev.StartDateUtc,
                EndDateUtc = ev.EndDateUtc,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                PrepStartUtc = ev.PrepStartUtc,
                MenuConfirmationDeadlineUtc = ev.MenuConfirmationDeadlineUtc,
                GuestConfirmationDeadlineUtc = ev.GuestConfirmationDeadlineUtc,
                MrpDeadlineUtc = ev.MrpDeadlineUtc,
                TimeZoneId = ev.TimeZoneId,
                GuaranteedGuests = ev.GuaranteedGuests,
                ExpectedGuests = ev.ExpectedGuests,
                MaxCapacity = ev.MaxCapacity,
                AdditionalAllowancePercent = ev.AdditionalAllowancePercent,
                PlanningGuests = ev.PlanningGuests,
                BillableGuests = ev.BillableGuests,
                BillablePolicy = ev.BillablePolicy,
                AdultsCount = ev.AdultsCount,
                ChildrenCount = ev.ChildrenCount,
                VipCount = ev.VipCount,
                StaffMealsCount = ev.StaffMealsCount,
                Currency = ev.Currency,
                NetPricePerGuest = costVisible ? ev.NetPricePerGuest : null,
                FixedPackagePrice = costVisible ? ev.FixedPackagePrice : null,
                EstimatedFoodRevenue = costVisible ? ev.EstimatedFoodRevenue : null,
                EstimatedBeverageRevenue = costVisible ? ev.EstimatedBeverageRevenue : null,
                OtherPlannedRevenue = costVisible ? ev.OtherPlannedRevenue : null,
                TotalEstimatedRevenue = costVisible ? totalRev : null,
                TargetFoodCostPercent = ev.TargetFoodCostPercent,
                TargetBeverageCostPercent = ev.TargetBeverageCostPercent,
                CombinedMenuBudget = costVisible ? ev.CombinedMenuBudget : null,
                ApprovedBudget = costVisible ? ev.ApprovedBudget : null,
                TotalEstimatedMenuCost = costVisible ? totalMenuCost : null,
                MenuCostPercent = costVisible ? pct : null,
                RemainingMenuBudget = costVisible && budget.HasValue && totalMenuCost.HasValue ? budget.Value - totalMenuCost.Value : null,
                CostVisible = costVisible,
                ServiceStyle = ev.ServiceStyle,
                PrimaryMenuId = ev.PrimaryMenuId,
                SourceTemplateId = ev.SourceTemplateId,
                WizardStepCompleted = ev.WizardStepCompleted,
                DietaryRequirements = ParseDietaryList(ev.DietaryRequirementsJson),
                DietaryReviewVerified = ev.DietaryReviewVerified,
                DietaryReviewerName = ev.DietaryReviewerName,
                DietaryVerifiedOnUtc = ev.DietaryVerifiedOnUtc,
                HasVenueConflict = conflicts.Count > 0,
                ConflictingEventCodes = conflicts,
                ApprovalReference = ev.ApprovalReference,
                SubmittedOnUtc = ev.SubmittedOnUtc,
                ApprovedOnUtc = ev.ApprovedOnUtc,
                ApprovalDecisionComment = ev.ApprovalDecisionComment,
                CreatedOn = ev.DateCreated,
                UpdatedOn = ev.DateUpdated,
                PrimaryMenu = primaryMenuDto,
                Sessions = sessions,
                Scenarios = scenarios
            };
        }

        private async Task<SilaCulinaryMenuDto> BuildMenuDtoAsync(
            Guid buyerId, SilaCulinaryMenu menu, bool costVisible, CancellationToken cancellationToken)
        {
            List<SilaCulinaryMenuLine> lines = await _repository.SilaCulinaryMenuLine
                .FindByCondition(x => x.BuyerId == buyerId && x.MenuId == menu.Id && x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            List<Guid> recipeIds = lines.Where(l => l.RecipeId.HasValue).Select(l => l.RecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, int> latestVersionMap = recipeIds.Count == 0
                ? new()
                : await _repository.Recipe
                    .FindByCondition(x => x.BuyerId == buyerId && recipeIds.Contains(x.Id) && x.IsActive)
                    .ToDictionaryAsync(x => x.Id, x => x.ActiveVersion > 0 ? x.ActiveVersion : x.Version, cancellationToken);

            return new SilaCulinaryMenuDto
            {
                Id = menu.Id,
                MenuCode = menu.MenuCode,
                EventId = menu.EventId,
                SessionId = menu.SessionId,
                Name = menu.Name,
                Description = menu.Description,
                MenuCategory = menu.MenuCategory,
                Cuisine = menu.Cuisine,
                PropertyId = menu.PropertyId,
                OutletLocationId = menu.OutletLocationId,
                ServiceStyle = menu.ServiceStyle,
                VersionNumber = menu.VersionNumber,
                EffectiveDateUtc = menu.EffectiveDateUtc,
                MenuImageUrl = menu.MenuImageUrl,
                Status = menu.Status,
                IsReusableTemplate = menu.IsReusableTemplate,
                IsScenario = menu.IsScenario,
                ScenarioName = menu.ScenarioName,
                IsSelectedScenario = menu.IsSelectedScenario,
                Currency = menu.Currency,
                TargetSellingPrice = costVisible ? menu.TargetSellingPrice : null,
                TargetFoodCostPercent = menu.TargetFoodCostPercent,
                TotalFoodCost = costVisible ? menu.TotalFoodCost : null,
                TotalBeverageCost = costVisible ? menu.TotalBeverageCost : null,
                TotalEstimatedCost = costVisible ? menu.TotalEstimatedCost : null,
                CostPerGuest = costVisible ? menu.CostPerGuest : null,
                CostVisible = costVisible,
                AiRecommendationNotes = menu.AiRecommendationNotes,
                Lines = lines.Select(l =>
                {
                    int? latestVer = l.RecipeId.HasValue && latestVersionMap.TryGetValue(l.RecipeId.Value, out int lv) ? lv : null;
                    SilaCulinaryMenuLineDto dto = ToMenuLineDto(l, costVisible, l.PlannedPortions, l.UnitCostPerPortion, l.EstimatedLineCost, l.PortionSize, l.UptakePercent);
                    dto.LatestPublishedRecipeVersion = latestVer;
                    dto.RecipeVersionOutdated = latestVer.HasValue && l.RecipeVersion.HasValue && latestVer.Value != l.RecipeVersion.Value;
                    return dto;
                }).ToList()
            };
        }

        private static SilaCulinaryMenuLineDto ToMenuLineDto(
            SilaCulinaryMenuLine l, bool costVisible, int plannedPortions, decimal? unitCost, decimal? lineCost, decimal portionSize, decimal uptake)
        {
            decimal? bevLiters = l.IsBeverage && l.ServingVolumeMl.HasValue
                ? Math.Round(plannedPortions * l.ServingVolumeMl.Value / 1000m, 3)
                : null;
            return new SilaCulinaryMenuLineDto
            {
                Id = l.Id,
                MenuId = l.MenuId,
                CourseName = l.CourseName,
                SortOrder = l.SortOrder,
                ItemRole = l.ItemRole,
                IsLocked = l.IsLocked,
                RecipeId = l.RecipeId,
                RecipeCode = l.RecipeCode,
                RecipeVersion = l.RecipeVersion,
                RecipeDraftId = l.RecipeDraftId,
                DishName = l.DishName,
                CustomerDescription = l.CustomerDescription,
                DishImageUrl = l.DishImageUrl,
                LineStatus = l.LineStatus,
                ServiceStyle = l.ServiceStyle,
                UptakePercent = uptake,
                ServingFrequency = l.ServingFrequency,
                PortionSize = portionSize,
                PortionUom = l.PortionUom,
                AllocatedPortions = l.AllocatedPortions,
                ManualPortionsOverride = l.ManualPortionsOverride,
                PlannedPortions = plannedPortions,
                CostingSnapshotId = l.CostingSnapshotId,
                UnitCostPerPortion = costVisible ? unitCost : null,
                EstimatedLineCost = costVisible ? lineCost : null,
                CostVisible = costVisible,
                DietaryTags = ParseJsonStringList(l.DietaryTagsJson),
                VerifiedAllergens = ParseJsonStringList(l.VerifiedAllergensJson),
                AllergenUnknown = l.AllergenUnknown,
                AllergenVerified = l.AllergenVerified,
                IsBeverage = l.IsBeverage,
                ServingsPerGuest = l.ServingsPerGuest,
                ServingVolumeMl = l.ServingVolumeMl,
                ContainerSizeMl = l.ContainerSizeMl,
                EstimatedBeverageLiters = bevLiters,
                LiveStationName = l.LiveStationName,
                VerifiedStationCapacityPerHour = l.VerifiedStationCapacityPerHour,
                Notes = l.Notes
            };
        }

        private sealed class RecipeMeta
        {
            public string? Cuisine { get; set; }
            public string? ImageUrl { get; set; }
            public Guid? CostingSnapshotId { get; set; }
            public decimal? CostPerPortion { get; set; }
            public List<string> DietaryTags { get; set; } = new();
            public List<string> VerifiedAllergens { get; set; } = new();
            public bool AllergenUnknown { get; set; } = true;
        }

        private async Task<RecipeMeta> ResolveRecipeMetadataAsync(Guid buyerId, Recipe recipe, CancellationToken cancellationToken)
        {
            RecipeMeta meta = new();
            SilaCulinaryCostingSnapshot? snap = await _repository.SilaCulinaryCostingSnapshot
                .FindByCondition(x => x.BuyerId == buyerId && x.ProductionRecipeId == recipe.Id && x.IsApprovedSnapshot && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .FirstOrDefaultAsync(cancellationToken);
            if (snap != null)
            {
                meta.CostingSnapshotId = snap.Id;
                meta.CostPerPortion = snap.TotalCostPerPortion;
            }
            else if (recipe.TotalCost > 0 && recipe.ServingQty > 0)
            {
                meta.CostPerPortion = Math.Round(recipe.TotalCost / recipe.ServingQty, 4);
            }

            SilaCulinaryRecipeLifecycle? lc = await _repository.SilaCulinaryRecipeLifecycle
                .FindByCondition(x => x.BuyerId == buyerId && x.ProductionRecipeId == recipe.Id && x.IsActive)
                .OrderByDescending(x => x.DateCreated)
                .FirstOrDefaultAsync(cancellationToken);

            if (lc != null)
            {
                SilaCulinaryRecipeRevision? rev = await _repository.SilaCulinaryRecipeRevision
                    .FindByCondition(x => x.BuyerId == buyerId && x.DraftId == lc.DraftId && x.RevisionNumber == lc.ApprovedDraftRevision && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);
                if (rev != null && !string.IsNullOrWhiteSpace(rev.PayloadJson))
                {
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(rev.PayloadJson);
                        if (doc.RootElement.TryGetProperty("cuisine", out JsonElement cEl)) meta.Cuisine = cEl.GetString();
                        if (doc.RootElement.TryGetProperty("imageUrl", out JsonElement iEl)) meta.ImageUrl = iEl.GetString();
                        if (doc.RootElement.TryGetProperty("dietaryTags", out JsonElement dEl) && dEl.ValueKind == JsonValueKind.Array)
                        {
                            meta.DietaryTags = dEl.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                        }
                        if (doc.RootElement.TryGetProperty("allergens", out JsonElement aEl) && aEl.ValueKind == JsonValueKind.Array)
                        {
                            meta.VerifiedAllergens = aEl.EnumerateArray().Select(x => x.GetString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                            meta.AllergenUnknown = false;
                        }
                    }
                    catch { /* malformed snapshot json keeps AllergenUnknown = true */ }
                }
            }

            return meta;
        }

        private async Task<List<string>> CollectSubRecipeAllergensAsync(
            Guid buyerId, Guid recipeId, HashSet<Guid> visited, CancellationToken cancellationToken)
        {
            if (!visited.Add(recipeId)) return new();
            List<Guid> subIds = await _repository.RecipeIngredient
                .FindByCondition(x => x.RecipeId == recipeId && x.SubRecipeId != null && x.IsActive)
                .Select(x => x.SubRecipeId!.Value)
                .ToListAsync(cancellationToken);

            HashSet<string> collected = new(StringComparer.OrdinalIgnoreCase);
            foreach (Guid subId in subIds)
            {
                Recipe? sub = await _repository.Recipe.FindByCondition(x => x.BuyerId == buyerId && x.Id == subId && x.IsActive).FirstOrDefaultAsync(cancellationToken);
                if (sub == null) continue;
                RecipeMeta subMeta = await ResolveRecipeMetadataAsync(buyerId, sub, cancellationToken);
                foreach (string a in subMeta.VerifiedAllergens) collected.Add(a);
                foreach (string deeper in await CollectSubRecipeAllergensAsync(buyerId, subId, visited, cancellationToken)) collected.Add(deeper);
            }
            return collected.ToList();
        }

        private async Task<SilaCulinaryAiMenuGenerateResultDto?> TryCallPythonMenuGenerateAsync(
            Guid buyerId, SilaCulinaryEvent ev, SilaCulinaryAiMenuGenerateRequestDto request,
            List<SilaCulinaryPublishedRecipeSearchItemDto> catalog, List<Guid> lockedIds, CancellationToken cancellationToken)
        {
            string? baseUrl = _configuration[Common.CULINARY_ENGINE_URL];
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            try
            {
                SilaCulinaryAiConfiguration? aiCfg = await _repository.SilaCulinaryAiConfiguration
                    .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                object payload = new
                {
                    event_type = ev.EventType,
                    event_theme = request.EventTheme ?? ev.Description ?? "",
                    natural_language_brief = request.NaturalLanguageBrief ?? "",
                    guaranteed_guests = ev.GuaranteedGuests,
                    additional_allowance_percent = ev.AdditionalAllowancePercent,
                    service_style = ev.ServiceStyle,
                    cuisine_preferences = request.CuisinePreferences,
                    required_courses = request.RequiredCourses,
                    required_dietary_tags = request.RequiredDietaryTags,
                    excluded_allergens = request.ExcludedAllergens,
                    menu_budget = ev.CombinedMenuBudget,
                    target_food_cost_percent = ev.TargetFoodCostPercent,
                    strategy = request.Strategy,
                    allow_missing_recipe_suggestions = request.AllowMissingRecipeSuggestions,
                    locked_recipe_ids = lockedIds.Select(x => x.ToString()),
                    catalog = catalog.Select(c => new
                    {
                        recipe_id = c.RecipeId.ToString(),
                        recipe_code = c.RecipeCode,
                        recipe_version = c.RecipeVersion,
                        name = c.Name,
                        category = c.Category,
                        cuisine = c.Cuisine ?? "",
                        course_hint = c.Category,
                        cost_per_portion = c.CostPerPortion,
                        currency = c.Currency,
                        dietary_tags = c.DietaryTags,
                        verified_allergens = c.VerifiedAllergens,
                        unknown_allergens = c.UnknownAllergens,
                        is_published = true,
                        property_eligible = c.PropertyEligible
                    }),
                    provider_config = aiCfg == null ? null : new
                    {
                        tenant_id = buyerId.ToString(),
                        operating_mode = aiCfg.OperatingMode,
                        tenant_external_ai_allowed = aiCfg.AllowExternalAi,
                        local_enabled = aiCfg.LocalEnabled,
                        local_endpoint = aiCfg.LocalEndpoint,
                        local_model = aiCfg.LocalModel,
                        gemini_enabled = aiCfg.GeminiEnabled,
                        gemini_model = aiCfg.GeminiModel,
                        gemini_api_key = string.IsNullOrWhiteSpace(aiCfg.ProtectedGeminiApiKey) ? null : SafeUnprotect(aiCfg.ProtectedGeminiApiKey),
                        claude_enabled = aiCfg.ClaudeEnabled,
                        claude_model = aiCfg.ClaudeModel,
                        claude_api_key = string.IsNullOrWhiteSpace(aiCfg.ProtectedClaudeApiKey) ? null : SafeUnprotect(aiCfg.ProtectedClaudeApiKey)
                    }
                };

                using HttpResponseMessage resp = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/v1/menus/generate", payload, cancellationToken);
                if (!resp.IsSuccessStatusCode) return null;
                string raw = await resp.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument doc = JsonDocument.Parse(raw);
                JsonElement root = doc.RootElement;
                List<SilaCulinaryMenuLineDto> proposed = new();
                if (root.TryGetProperty("lines", out JsonElement linesEl))
                {
                    foreach (JsonElement el in linesEl.EnumerateArray())
                    {
                        Guid? rid = el.TryGetProperty("recipe_id", out JsonElement rEl) && rEl.ValueKind == JsonValueKind.String && Guid.TryParse(rEl.GetString(), out Guid parsed) ? parsed : null;
                        proposed.Add(new SilaCulinaryMenuLineDto
                        {
                            CourseName = el.GetProperty("course_name").GetString() ?? "Main Courses",
                            DishName = el.GetProperty("dish_name").GetString() ?? "",
                            RecipeId = rid,
                            RecipeCode = el.TryGetProperty("recipe_code", out JsonElement rcEl) ? rcEl.GetString() : null,
                            RecipeVersion = el.TryGetProperty("recipe_version", out JsonElement rvEl) && rvEl.TryGetInt32(out int rv) ? rv : null,
                            LineStatus = el.GetProperty("line_status").GetString() ?? "PUBLISHED",
                            UptakePercent = el.GetProperty("uptake_percent").GetDecimal(),
                            PlannedPortions = el.GetProperty("planned_portions").GetInt32(),
                            UnitCostPerPortion = el.TryGetProperty("unit_cost_per_portion", out JsonElement ucEl) && ucEl.ValueKind == JsonValueKind.Number ? ucEl.GetDecimal() : null,
                            EstimatedLineCost = el.TryGetProperty("estimated_line_cost", out JsonElement lcEl) && lcEl.ValueKind == JsonValueKind.Number ? lcEl.GetDecimal() : null,
                            Notes = el.TryGetProperty("rationale", out JsonElement raEl) ? raEl.GetString() : null
                        });
                    }
                }
                return new SilaCulinaryAiMenuGenerateResultDto
                {
                    MenuTitle = root.GetProperty("menu_title").GetString() ?? ev.Name,
                    MenuDescription = root.GetProperty("menu_description").GetString() ?? "",
                    PlanningGuests = root.GetProperty("planning_guests").GetInt32(),
                    EstimatedTotalCost = root.GetProperty("estimated_total_cost").GetDecimal(),
                    Currency = ev.Currency,
                    ProviderUsed = root.GetProperty("provider_used").GetString() ?? "LOCAL",
                    ModelUsed = root.GetProperty("model_used").GetString() ?? "sila-menu-planner-v1",
                    MissingRecipeCount = root.GetProperty("missing_recipe_count").GetInt32(),
                    Explanation = root.GetProperty("explanation").GetString() ?? "",
                    ProposedLines = proposed
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<SilaCulinaryMenuOptimizeResultDto?> TryCallPythonMenuOptimizeAsync(
            SilaCulinaryEvent ev, decimal maxBudget, Dictionary<string, int> requiredCourses, List<Guid> lockedIds,
            SilaCulinaryMenuOptimizeRequestDto request, List<SilaCulinaryPublishedRecipeSearchItemDto> catalog, decimal beforeCost, CancellationToken cancellationToken)
        {
            string? baseUrl = _configuration[Common.CULINARY_ENGINE_URL];
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;
            try
            {
                HttpClient client = _httpClientFactory.CreateClient(Common.HTTP_CLIENT_CULINARY_ENGINE);
                object payload = new
                {
                    planning_guests = Math.Max(1, ev.PlanningGuests),
                    max_budget = maxBudget,
                    required_courses = requiredCourses,
                    locked_recipe_ids = lockedIds.Select(x => x.ToString()),
                    required_dietary_tags = request.RequiredDietaryTags,
                    excluded_allergens = request.ExcludedAllergens,
                    strict_allergen_safety = true,
                    strategy = request.Strategy,
                    catalog = catalog.Select(c => new
                    {
                        recipe_id = c.RecipeId.ToString(),
                        recipe_code = c.RecipeCode,
                        recipe_version = c.RecipeVersion,
                        name = c.Name,
                        category = c.Category,
                        cuisine = c.Cuisine ?? "",
                        course_hint = c.Category,
                        cost_per_portion = c.CostPerPortion,
                        currency = c.Currency,
                        dietary_tags = c.DietaryTags,
                        verified_allergens = c.VerifiedAllergens,
                        unknown_allergens = c.UnknownAllergens,
                        is_published = true,
                        property_eligible = c.PropertyEligible
                    })
                };
                using HttpResponseMessage resp = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/v1/menus/optimize", payload, cancellationToken);
                if (!resp.IsSuccessStatusCode) return null;
                string raw = await resp.Content.ReadAsStringAsync(cancellationToken);
                using JsonDocument doc = JsonDocument.Parse(raw);
                JsonElement root = doc.RootElement;
                bool feasible = root.GetProperty("feasible").GetBoolean();
                decimal afterCost = root.GetProperty("total_estimated_cost").GetDecimal();
                List<SilaCulinaryMenuLineDto> lines = new();
                if (root.TryGetProperty("selected_items", out JsonElement itemsEl))
                {
                    foreach (JsonElement el in itemsEl.EnumerateArray())
                    {
                        lines.Add(new SilaCulinaryMenuLineDto
                        {
                            RecipeId = Guid.Parse(el.GetProperty("recipe_id").GetString()!),
                            RecipeCode = el.GetProperty("recipe_code").GetString(),
                            RecipeVersion = el.GetProperty("recipe_version").GetInt32(),
                            DishName = el.GetProperty("name").GetString() ?? "",
                            CourseName = el.GetProperty("course_name").GetString() ?? "Main Courses",
                            PlannedPortions = el.GetProperty("planned_portions").GetInt32(),
                            UnitCostPerPortion = el.GetProperty("unit_cost_per_portion").GetDecimal(),
                            EstimatedLineCost = el.GetProperty("estimated_line_cost").GetDecimal(),
                            IsLocked = el.GetProperty("locked").GetBoolean(),
                            UptakePercent = 100m
                        });
                    }
                }
                return new SilaCulinaryMenuOptimizeResultDto
                {
                    Feasible = feasible,
                    Solver = root.GetProperty("solver").GetString() ?? "SCIPY_HIGHS_MILP",
                    BeforeCost = beforeCost,
                    AfterCost = feasible ? afterCost : null,
                    Budget = maxBudget,
                    Savings = feasible ? Math.Round(beforeCost - afterCost, 2) : null,
                    ConflictingConstraints = root.GetProperty("conflicting_constraints").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                    SuggestedAdjustments = root.GetProperty("suggested_adjustments").EnumerateArray().Select(x => x.GetString() ?? "").ToList(),
                    Explanation = root.GetProperty("explanation").GetString() ?? "",
                    OptimizedLines = lines
                };
            }
            catch
            {
                return null;
            }
        }

        private static SilaCulinaryAiMenuGenerateResultDto DeterministicFallbackMenuGenerate(
            SilaCulinaryEvent ev, SilaCulinaryAiMenuGenerateRequestDto request,
            List<SilaCulinaryPublishedRecipeSearchItemDto> catalog, List<Guid> lockedIds)
        {
            List<SilaCulinaryMenuLineDto> lines = new();
            HashSet<Guid> used = new();
            int guests = Math.Max(1, ev.PlanningGuests);

            foreach (Guid lid in lockedIds)
            {
                SilaCulinaryPublishedRecipeSearchItemDto? item = catalog.FirstOrDefault(c => c.RecipeId == lid);
                if (item == null) continue;
                used.Add(item.RecipeId);
                decimal? cost = item.CostPerPortion.HasValue ? Math.Round(guests * item.CostPerPortion.Value, 2) : null;
                lines.Add(new SilaCulinaryMenuLineDto
                {
                    CourseName = item.Category,
                    RecipeId = item.RecipeId,
                    RecipeCode = item.RecipeCode,
                    RecipeVersion = item.RecipeVersion,
                    DishName = item.Name,
                    LineStatus = "PUBLISHED",
                    IsLocked = true,
                    UptakePercent = 100m,
                    PlannedPortions = guests,
                    UnitCostPerPortion = item.CostPerPortion,
                    EstimatedLineCost = cost,
                    DietaryTags = item.DietaryTags,
                    VerifiedAllergens = item.VerifiedAllergens,
                    Notes = "Locked recipe preserved."
                });
            }

            foreach (string course in request.RequiredCourses)
            {
                if (lines.Any(l => string.Equals(l.CourseName, course, StringComparison.OrdinalIgnoreCase))) continue;
                SilaCulinaryPublishedRecipeSearchItemDto? match = catalog
                    .Where(c => !used.Contains(c.RecipeId) && string.Equals(c.Category, course, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => request.CuisinePreferences.Any(cp => string.Equals(cp, c.Cuisine, StringComparison.OrdinalIgnoreCase)))
                    .ThenBy(c => c.CostPerPortion ?? 9999m)
                    .FirstOrDefault();

                if (match != null)
                {
                    used.Add(match.RecipeId);
                    decimal uptake = ev.ServiceStyle == "BUFFET" ? 60m : 100m;
                    int portions = (int)Math.Ceiling(guests * (uptake / 100m));
                    decimal? cost = match.CostPerPortion.HasValue ? Math.Round(portions * match.CostPerPortion.Value, 2) : null;
                    lines.Add(new SilaCulinaryMenuLineDto
                    {
                        CourseName = course,
                        RecipeId = match.RecipeId,
                        RecipeCode = match.RecipeCode,
                        RecipeVersion = match.RecipeVersion,
                        DishName = match.Name,
                        LineStatus = "PUBLISHED",
                        UptakePercent = uptake,
                        PlannedPortions = portions,
                        UnitCostPerPortion = match.CostPerPortion,
                        EstimatedLineCost = cost,
                        DietaryTags = match.DietaryTags,
                        VerifiedAllergens = match.VerifiedAllergens,
                        Notes = "Matched published Recipe Master."
                    });
                }
                else if (request.AllowMissingRecipeSuggestions)
                {
                    lines.Add(new SilaCulinaryMenuLineDto
                    {
                        CourseName = course,
                        DishName = $"Chef {course} Creation",
                        LineStatus = "RECIPE_REQUIRED",
                        UptakePercent = 100m,
                        PlannedPortions = guests,
                        AllergenUnknown = true,
                        Notes = "No published recipe found; hand over to Phase 1A Recipe Planner."
                    });
                }
            }

            decimal total = lines.Sum(l => l.EstimatedLineCost ?? 0m);
            int missing = lines.Count(l => l.LineStatus == "RECIPE_REQUIRED");
            return new SilaCulinaryAiMenuGenerateResultDto
            {
                MenuTitle = $"{ev.Name} — Proposed Menu",
                MenuDescription = $"Deterministic grounded menu for {guests} planning guests.",
                PlanningGuests = guests,
                EstimatedTotalCost = total,
                Currency = ev.Currency,
                ProviderUsed = "LOCAL_DETERMINISTIC",
                ModelUsed = "sila-menu-planner-v1",
                MissingRecipeCount = missing,
                Explanation = $"Generated {lines.Count} course items ({missing} pending Phase 1A recipe drafts).",
                ProposedLines = lines
            };
        }

        private static SilaCulinaryMenuOptimizeResultDto DeterministicFallbackOptimize(
            SilaCulinaryEvent ev, decimal maxBudget, Dictionary<string, int> requiredCourses, List<Guid> lockedIds,
            SilaCulinaryMenuOptimizeRequestDto request, List<SilaCulinaryPublishedRecipeSearchItemDto> catalog, decimal beforeCost)
        {
            int guests = Math.Max(1, ev.PlanningGuests);
            List<SilaCulinaryPublishedRecipeSearchItemDto> safeCatalog = catalog.Where(c => c.CostPerPortion.HasValue).ToList();
            List<SilaCulinaryMenuLineDto> selected = new();
            HashSet<Guid> used = new();
            decimal total = 0m;
            List<string> conflicts = new();

            foreach (Guid lid in lockedIds)
            {
                SilaCulinaryPublishedRecipeSearchItemDto? locked = safeCatalog.FirstOrDefault(c => c.RecipeId == lid);
                if (locked == null)
                {
                    conflicts.Add($"Locked recipe {lid} is missing verified cost or unsafe under allergen constraints.");
                    continue;
                }
                used.Add(locked.RecipeId);
                decimal lc = Math.Round(guests * locked.CostPerPortion!.Value, 2);
                total += lc;
                selected.Add(new SilaCulinaryMenuLineDto
                {
                    RecipeId = locked.RecipeId,
                    RecipeCode = locked.RecipeCode,
                    RecipeVersion = locked.RecipeVersion,
                    DishName = locked.Name,
                    CourseName = locked.Category,
                    PlannedPortions = guests,
                    UnitCostPerPortion = locked.CostPerPortion,
                    EstimatedLineCost = lc,
                    IsLocked = true,
                    UptakePercent = 100m
                });
            }

            foreach ((string course, int count) in requiredCourses)
            {
                int already = selected.Count(s => string.Equals(s.CourseName, course, StringComparison.OrdinalIgnoreCase));
                int needed = Math.Max(0, count - already);
                List<SilaCulinaryPublishedRecipeSearchItemDto> picks = safeCatalog
                    .Where(c => !used.Contains(c.RecipeId) && string.Equals(c.Category, course, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(c => c.CostPerPortion)
                    .Take(needed)
                    .ToList();
                if (picks.Count < needed)
                {
                    conflicts.Add($"Course '{course}' needs {count} safe published recipe(s), only {already + picks.Count} available.");
                }
                foreach (SilaCulinaryPublishedRecipeSearchItemDto p in picks)
                {
                    used.Add(p.RecipeId);
                    decimal lc = Math.Round(guests * p.CostPerPortion!.Value, 2);
                    total += lc;
                    selected.Add(new SilaCulinaryMenuLineDto
                    {
                        RecipeId = p.RecipeId,
                        RecipeCode = p.RecipeCode,
                        RecipeVersion = p.RecipeVersion,
                        DishName = p.Name,
                        CourseName = course,
                        PlannedPortions = guests,
                        UnitCostPerPortion = p.CostPerPortion,
                        EstimatedLineCost = lc,
                        UptakePercent = 100m
                    });
                }
            }

            if (total > maxBudget)
            {
                conflicts.Add($"Minimum feasible cost ({total:F2} {ev.Currency}) exceeds hard budget ({maxBudget:F2} {ev.Currency}).");
            }

            if (conflicts.Count > 0)
            {
                return new SilaCulinaryMenuOptimizeResultDto
                {
                    Feasible = false,
                    Solver = "DETERMINISTIC_CONSTRAINT_FALLBACK",
                    BeforeCost = beforeCost,
                    Budget = maxBudget,
                    ConflictingConstraints = conflicts,
                    SuggestedAdjustments = new List<string>
                    {
                        $"Increase menu budget to at least {total:F2} {ev.Currency} or unlock expensive mandatory dishes."
                    },
                    Explanation = "No feasible menu satisfies all hard budget, course, and locked dish constraints."
                };
            }

            return new SilaCulinaryMenuOptimizeResultDto
            {
                Feasible = true,
                Solver = "DETERMINISTIC_CONSTRAINT_FALLBACK",
                BeforeCost = beforeCost,
                AfterCost = total,
                Budget = maxBudget,
                Savings = Math.Round(beforeCost - total, 2),
                Explanation = $"Optimized menu within budget {maxBudget:F2} {ev.Currency} preserving locked recipes.",
                OptimizedLines = selected
            };
        }

        private string? SafeUnprotect(string encrypted)
        {
            try { return _protector.Unprotect(encrypted); }
            catch { return null; }
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
                Comment = comment.Length > 490 ? comment[..490] : comment,
                ActorUserId = actorUserId,
                IsActive = true
            });
        }

        private static List<string> ParseCsvList(string? csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? new()
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();

        private static List<string> ParseJsonStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<string>>(json, CamelJson) ?? new(); }
            catch { return new(); }
        }

        private static List<SilaCulinaryDietaryAllocationDto> ParseDietaryList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new();
            try { return JsonSerializer.Deserialize<List<SilaCulinaryDietaryAllocationDto>>(json, CamelJson) ?? new(); }
            catch { return new(); }
        }

        /// <summary>Spreadsheet formula-injection safe CSV cell (§47).</summary>
        private static string CsvSafe(string? raw)
        {
            string val = raw ?? "";
            if (val.Length > 0 && (val[0] == '=' || val[0] == '+' || val[0] == '-' || val[0] == '@' || val[0] == '\t' || val[0] == '\r'))
            {
                val = "'" + val;
            }
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
    }
}
