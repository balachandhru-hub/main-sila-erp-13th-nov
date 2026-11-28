using System.Globalization;
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

namespace Buyer.Application.Features.Shared.MenuEngineering
{
    public interface IMenuEngineeringStudioService
    {
        Task<MenuEngineeringPermissionSetDto> GetPermissionsAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<MeStudioWorkspaceDto> GetWorkspaceAsync(Guid organizationId, Guid userId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct);
        Task<List<MenuItemEnhancedDto>> GetMenuItemsAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<MenuItemEnhancedDto> SaveMenuItemAsync(Guid organizationId, Guid userId, Guid roleId, MenuItemEnhancedWriteDto dto, CancellationToken ct);
        Task<MenuItemEnhancedDto> CopyMenuItemAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuItemId, CancellationToken ct);
        Task<int> BulkPriceUpdateAsync(Guid organizationId, Guid userId, Guid roleId, MenuItemBulkPriceUpdateDto dto, CancellationToken ct);
        Task<int> BulkImportMenuItemsAsync(Guid organizationId, Guid userId, Guid roleId, MenuItemBulkImportDto dto, CancellationToken ct);
        Task<List<MenuItemVersionDto>> GetMenuItemVersionsAsync(Guid organizationId, Guid roleId, Guid menuItemId, CancellationToken ct);
        Task<List<MenuPlanEnhancedDto>> GetMenuPlansAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<MenuPlanEnhancedDto> SaveMenuPlanAsync(Guid organizationId, Guid userId, Guid roleId, MenuPlanEnhancedWriteDto dto, CancellationToken ct);
        Task<MenuPlanEnhancedDto> CreateMenuFromHistoricalAsync(Guid organizationId, Guid userId, Guid roleId, Guid historicalSourceId, string sourceKind, Guid? propertyId, CancellationToken ct);
        Task<MenuPlanEnhancedDto> CreateMenuFromAiStrategyAsync(Guid organizationId, Guid userId, Guid roleId, string strategyKey, Guid? propertyId, decimal? expectedCovers, CancellationToken ct);
        Task<MenuPlanEnhancedDto> TransitionMenuPlanAsync(Guid organizationId, Guid userId, Guid roleId, MenuPlanTransitionDto dto, CancellationToken ct);

        Task<List<PortionPlanRowDto>> GetPortionPlansAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct);
        Task<PortionPlanRowDto> RecordPortionSampleAsync(Guid organizationId, Guid userId, Guid roleId, PortionSampleWriteDto dto, CancellationToken ct);
        Task<DemandForecastBoardDto> GetDemandBoardAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<MenuDemandOverrideDto> SaveDemandOverrideAsync(Guid organizationId, Guid userId, Guid roleId, DemandOverrideWriteDto dto, CancellationToken ct);
        Task<PosSalesExtendedDto> GetPosSalesExtendedAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<PosMappingWorkbenchDto> GetPosMappingWorkbenchAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<PosMappingExtendedDto> SavePosMappingExtendedAsync(Guid organizationId, Guid userId, Guid roleId, PosMappingExtendedWriteDto dto, CancellationToken ct);
        Task<MenuPerformanceMatrixDto> GetPerformanceMatrixAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, string? costBasis, decimal? popularityThreshold, CancellationToken ct);
        Task<ConsumptionVarianceDetailDto> GetConsumptionVarianceAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<CostMarginAnalysisDto> GetCostMarginAnalysisAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<MenuPurchasePlanDto> GetPurchasePlanAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<SilaPurchaseRecommendationListDto> CreateDraftPurchaseRecommendationAsync(Guid organizationId, Guid userId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct);

        Task<AiMenuIntelligenceDto> GetAiMenuIntelligenceAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct);
        Task<List<HistoricalMenuSummaryDto>> GetHistoricalMenusAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<WhatIfSimulationResultDto> RunSimulationAsync(Guid organizationId, Guid userId, Guid roleId, WhatIfSimulationRequestDto dto, CancellationToken ct);
        Task<List<MenuEngineeringScenarioDto>> GetScenariosAsync(Guid organizationId, Guid roleId, Guid? menuPlanId, CancellationToken ct);
        Task<ScenarioCompareResultDto> CompareScenariosAsync(Guid organizationId, Guid roleId, List<Guid> scenarioIds, CancellationToken ct);
        Task<MenuEngineeringScenarioDto> SetPreferredScenarioAsync(Guid organizationId, Guid roleId, Guid scenarioId, CancellationToken ct);
        Task<MenuPlanEnhancedDto> ConvertScenarioToDraftAsync(Guid organizationId, Guid userId, Guid roleId, Guid scenarioId, CancellationToken ct);
        Task<MenuEngineeringSetupDto> GetSetupAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct);
        Task<MenuEngineeringSetupDto> SaveSetupAsync(Guid organizationId, Guid userId, Guid roleId, MenuEngineeringSetupDto dto, CancellationToken ct);
        List<CustomerDataTemplateDto> GetMigrationTemplates();
        Task<CustomerDataImportPreviewResultDto> PreviewCustomerDataMigrationAsync(Guid organizationId, Guid userId, Guid roleId, CustomerDataImportPreviewRequestDto dto, CancellationToken ct);
        Task<CustomerDataImportCommitResultDto> CommitCustomerDataMigrationAsync(Guid organizationId, Guid userId, Guid roleId, CustomerDataImportPreviewRequestDto dto, CancellationToken ct);
        Task<List<CustomerDataMigrationAuditDto>> GetCustomerDataMigrationAuditsAsync(Guid organizationId, Guid roleId, CancellationToken ct);
        List<MenuEngineeringReportItemDto> GetReportCatalog();
        Task<MenuEngineeringReportExportDto> ExportReportAsync(Guid organizationId, Guid roleId, string reportKey, string format, SilaMenuEngineeringFilterDto filter, CancellationToken ct);
        Task<SilaStoreMenuMobileDto> GetMobileMenuSummaryAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, CancellationToken ct);
    }

    public partial class MenuEngineeringStudioService : IMenuEngineeringStudioService
    {
        private static readonly string[] DefaultMeals = ["Breakfast", "Lunch", "Dinner", "Snacks", "Buffet", "Banquet", "Room Service", "Staff Meals", "Seasonal Menu", "Special Menu", "Other"];
        private static readonly string[] DefaultServiceFormats = ["A_LA_CARTE", "BUFFET", "BRUNCH", "BANQUET", "CORPORATE_CATERING", "SET_MENU", "TASTING_MENU", "ROOM_SERVICE", "SEASONAL", "SPECIAL_EVENT"];
        private static readonly string[] DefaultCategories = ["Appetizers", "Soups & Salads", "Mains", "Sides", "Desserts", "Beverages", "Buffet Stations"];
        private static readonly string[] DefaultCuisines = ["International", "Mediterranean", "Asian", "Italian", "Arabic", "Grill & Steakhouse", "Pastry"];
        private static readonly string[] DefaultDietary = ["Vegetarian", "Vegan", "Gluten-Free", "Dairy-Free", "Halal", "Nut-Free", "Keto"];
        private static readonly string[] DefaultApprovalRoles = ["Executive Chef", "F&B Manager", "Cost Controller", "Finance Controller", "General Manager"];

        private readonly IRepositoryWrapper _repository;
        private readonly RepositoryContext _db;
        private readonly ILoggerManager _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ISilaCulinaryFinancialService _financial;
        private readonly ISilaCulinaryAnalyticsService _analytics;
        private readonly IHttpClientFactory? _httpClientFactory;

        public MenuEngineeringStudioService(
            IRepositoryWrapper repository,
            RepositoryContext db,
            ILoggerManager logger,
            IHttpContextAccessor httpContextAccessor,
            ISilaCulinaryFinancialService financial,
            ISilaCulinaryAnalyticsService analytics,
            IHttpClientFactory? httpClientFactory = null)
        {
            _repository = repository;
            _db = db;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _financial = financial;
            _analytics = analytics;
            _httpClientFactory = httpClientFactory;
        }

        private sealed class StudioCtx
        {
            public BuyerBusinessProfile Buyer { get; init; } = null!;
            public MenuEngineeringSettings Settings { get; set; } = null!;
            public MenuEngineeringPermissionSetDto Perms { get; init; } = new();
            public List<InventoryLocation> Scope { get; init; } = new();
            public List<Guid> ScopeIds => Scope.Select(x => x.Id).ToList();
        }

        private async Task<StudioCtx> BuildContextAsync(Guid organizationId, Guid userId, Guid roleId, Guid? propertyId, bool requireFeatureEnabled, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringSettings? propSettings = propertyId == null
                ? null
                : await _db.MenuEngineeringSettings.FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && x.PropertyId == propertyId, ct);
            MenuEngineeringSettings settings = propSettings
                ?? await _db.MenuEngineeringSettings.FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && x.PropertyId == null, ct)
                ?? await _db.MenuEngineeringSettings.FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive, ct)
                ?? new MenuEngineeringSettings { Id = Guid.NewGuid(), BuyerId = buyer.Id, IsActive = true };

            if (requireFeatureEnabled && !settings.MenuEngineeringEnabled)
            {
                throw new BadRequestCustomException(
                    "Menu Engineering is disabled for this tenant/property.",
                    "Enable Menu Engineering under Setup or keep Procurement V1 isolated.");
            }

            MenuEngineeringFeatureFlagsDto flags = ReadFeatureFlags(settings);
            MenuEngineeringPermissionSetDto perms = MenuEngineeringPermissions.Evaluate(
                _httpContextAccessor, roleId, settings.MenuEngineeringEnabled, settings.MeStudioEnabled, flags);

            List<Guid> allowedIds = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, userId, roleId, ct);
            List<InventoryLocation> scope = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && allowedIds.Contains(x.Id) && (propertyId == null || x.PropertyId == propertyId))
                .ToListAsync(ct);

            return new StudioCtx
            {
                Buyer = buyer,
                Settings = settings,
                Perms = perms,
                Scope = scope
            };
        }

        private static MenuEngineeringFeatureFlagsDto ReadFeatureFlags(MenuEngineeringSettings settings)
        {
            MenuEngineeringFeatureFlagsDto flags = new()
            {
                MenuEngineering = settings.MenuEngineeringEnabled,
                MeStudio = settings.MeStudioEnabled
            };
            if (!string.IsNullOrWhiteSpace(settings.FeatureFlagsJson))
            {
                try
                {
                    MenuEngineeringFeatureFlagsDto? parsed = JsonSerializer.Deserialize<MenuEngineeringFeatureFlagsDto>(settings.FeatureFlagsJson);
                    if (parsed != null)
                    {
                        flags = parsed;
                        flags.MenuEngineering = settings.MenuEngineeringEnabled;
                        flags.MeStudio = settings.MeStudioEnabled;
                    }
                }
                catch { /* keep defaults */ }
            }
            return flags;
        }

        public async Task<MenuEngineeringPermissionSetDto> GetPermissionsAsync(Guid organizationId, Guid roleId, Guid? propertyId, CancellationToken ct)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, organizationId);
            MenuEngineeringSettings? settings = await _db.MenuEngineeringSettings
                .FirstOrDefaultAsync(x => x.BuyerId == buyer.Id && x.IsActive && (propertyId == null || x.PropertyId == propertyId), ct);
            MenuEngineeringFeatureFlagsDto? flags = settings == null ? null : ReadFeatureFlags(settings);
            return MenuEngineeringPermissions.Evaluate(
                _httpContextAccessor, roleId, settings?.MenuEngineeringEnabled ?? true, settings?.MeStudioEnabled ?? true, flags);
        }

        public async Task<MeStudioWorkspaceDto> GetWorkspaceAsync(Guid organizationId, Guid userId, Guid roleId, Guid? menuPlanId, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "You do not have permission to view ME Studio.");
            if (!ctx.Perms.FeatureFlags.MeStudio)
            {
                throw new BadRequestCustomException(
                    "ME Studio is disabled by feature flag.",
                    "Enable the ME Studio feature flag under Setup to open the interactive studio.");
            }

            SilaMenuLookupsDto lookups = await BuildLookupsAsync(ctx, ct);
            List<MenuItemEnhancedDto> library = await BuildMenuItemsAsync(ctx, new SilaMenuEngineeringFilterDto { PropertyId = propertyId }, ct);
            List<MenuPlanEnhancedDto> menus = await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto { PropertyId = propertyId }, ct);
            MenuPlanEnhancedDto? activeMenu = menuPlanId != null
                ? menus.FirstOrDefault(x => x.Id == menuPlanId)
                : menus.FirstOrDefault(x => x.Status == "DRAFT") ?? menus.FirstOrDefault();
            if (menuPlanId != null && activeMenu == null)
            {
                throw new NotFoundCustomException("Menu plan not found.", "The requested menu plan was not found for this tenant/property.");
            }

            SilaMenuEngineeringFilterDto activeFilter = new()
            {
                PropertyId = propertyId,
                MenuPlanId = activeMenu?.Id,
                DateFrom = activeMenu?.BusinessDate.Date ?? DateTime.UtcNow.Date,
                DateTo = activeMenu?.BusinessDate.Date ?? DateTime.UtcNow.Date
            };

            MeStudioLiveIntelligenceDto live = BuildLiveIntelligence(ctx, activeMenu, library);
            MenuPurchasePlanDto purchase = await BuildPurchasePlanInternalAsync(ctx, activeFilter, ct);
            live.AvailableIngredients = purchase.Rows.Count(r => r.Shortage <= 0);
            live.ShortIngredients = purchase.Rows.Count(r => r.Shortage > 0);
            live.IngredientReadinessPercent = purchase.Rows.Count == 0
                ? 100m
                : Math.Round(100m * live.AvailableIngredients / purchase.Rows.Count, 1);
            live.TotalPurchaseRequirementValue = Math.Round(purchase.TotalEstimatedPurchaseCost, 2);
            if (live.ShortIngredients > 0)
            {
                live.RiskIndicators.Add($"{live.ShortIngredients} ingredient(s) short of required date supply.");
            }

            AiMenuIntelligenceDto ai = await GetAiMenuIntelligenceInternalAsync(ctx, activeMenu?.Id, propertyId, ct);
            live.AiRecommendations = ai.Recommendations.Take(4).ToList();
            MenuEngineeringPermissions.RedactFinancials(ctx.Perms, live);

            MenuPerformanceMatrixDto? matrix = ctx.Perms.ViewMenuPerformance
                ? await BuildPerformanceMatrixInternalAsync(ctx, activeFilter, ctx.Settings.ProfitabilityBasis, ctx.Settings.PopularityThreshold, ct)
                : null;

            List<HistoricalMenuSummaryDto> historical = await GetHistoricalMenusInternalAsync(ctx, propertyId, ct);
            List<MenuEngineeringScenarioDto> scenarios = await GetScenariosInternalAsync(ctx, activeMenu?.Id, ct);
            List<MenuEngineeringApprovalHistoryDto> history = activeMenu == null
                ? new()
                : await _db.MenuEngineeringApprovalHistory
                    .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.EntityId == activeMenu.Id)
                    .OrderByDescending(x => x.ActionAtUtc)
                    .Select(x => new MenuEngineeringApprovalHistoryDto
                    {
                        Id = x.Id,
                        EntityType = x.EntityType,
                        EntityId = x.EntityId,
                        VersionNumber = x.VersionNumber,
                        FromStatus = x.FromStatus,
                        ToStatus = x.ToStatus,
                        ActionByUserId = x.ActionByUserId,
                        ApproverRole = x.ApproverRole,
                        Reason = x.Reason,
                        ActionAtUtc = x.ActionAtUtc
                    }).ToListAsync(ct);

            return new MeStudioWorkspaceDto
            {
                Permissions = ctx.Perms,
                Lookups = lookups,
                LibraryItems = library,
                Menus = menus,
                ActiveMenu = activeMenu,
                LiveIntelligence = live,
                IngredientRequirements = purchase.Rows.Cast<SilaMenuRequirementRowDto>().ToList(),
                PerformanceMatrix = matrix,
                HistoricalMenus = historical,
                Scenarios = scenarios,
                ApprovalHistory = history,
                Setup = ToSetupDto(ctx.Settings, new())
            };
        }

        public async Task<List<MenuItemEnhancedDto>> GetMenuItemsAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManageMenuItems, "Access denied", "You do not have permission to view menu items.");
            return await BuildMenuItemsAsync(ctx, filter, ct);
        }

        public async Task<MenuItemEnhancedDto> SaveMenuItemAsync(Guid organizationId, Guid userId, Guid roleId, MenuItemEnhancedWriteDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, dto.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuItems, "Access denied", "You do not have permission to manage menu items.");
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new BadRequestCustomException("Menu item name is required.", "Provide a name.");

            MenuItem? entity = dto.Id != null
                ? await _db.MenuItem.FirstOrDefaultAsync(x => x.Id == dto.Id && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                : null;
            bool isNew = entity == null;
            if (entity == null)
            {
                entity = new MenuItem
                {
                    Id = dto.Id ?? Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    MenuItemCode = $"MI-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                    IsActive = true,
                    VersionNumber = 1
                };
                _db.MenuItem.Add(entity);
            }
            else
            {
                entity.VersionNumber += 1;
            }

            Recipe? linkedRecipe = dto.LinkedRecipeId != null
                ? await _db.Recipe.FirstOrDefaultAsync(x => x.Id == dto.LinkedRecipeId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                : null;

            entity.Name = dto.Name.Trim();
            entity.AlternativeName = dto.AlternativeName?.Trim();
            entity.Description = dto.Description;
            entity.Category = dto.Category;
            entity.SubCategory = dto.SubCategory;
            entity.Cuisine = dto.Cuisine;
            entity.Classification = dto.Classification;
            entity.MealPeriods = dto.MealPeriods;
            entity.ServiceType = dto.ServiceType;
            entity.ServiceFormat = dto.ServiceFormat ?? dto.ServiceType ?? "A_LA_CARTE";
            entity.SellingPrice = dto.SellingPrice;
            entity.Currency = dto.Currency ?? ctx.Settings.DefaultCurrency ?? "USD";
            entity.TaxRatePercent = dto.TaxRatePercent;
            entity.TaxInclusive = dto.TaxInclusive;
            entity.DefaultPortionQty = dto.DefaultPortionQty <= 0 ? 1m : dto.DefaultPortionQty;
            entity.PortionUom = dto.PortionUom ?? ctx.Settings.DefaultPortionUom ?? "PORTION";
            entity.AdultPortionFactor = dto.AdultPortionFactor ?? ctx.Settings.DefaultAdultFactor;
            entity.ChildPortionFactor = dto.ChildPortionFactor ?? ctx.Settings.DefaultChildFactor;
            entity.DefaultParticipationRate = dto.DefaultParticipationRate ?? ctx.Settings.DefaultItemParticipation;
            entity.DefaultBufferPercentage = dto.DefaultBufferPercentage ?? ctx.Settings.DefaultBufferPercentage;
            entity.DietaryTags = dto.DietaryTags;
            entity.AllergenNotes = dto.AllergenNotes;
            entity.ImagePath = dto.ImagePath;
            entity.PropertyId = dto.PropertyId;
            entity.OutletLocationId = dto.OutletLocationId;
            entity.PosItemReference = dto.PosItemReference;
            entity.PrepTimeMinutes = dto.PrepTimeMinutes ?? linkedRecipe?.PrepTimeMinutes;
            entity.IsAvailable = dto.IsAvailable;
            entity.SeasonalAvailability = dto.SeasonalAvailability;
            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;
            entity.Status = string.IsNullOrWhiteSpace(dto.Status) ? "ACTIVE" : dto.Status.Trim().ToUpperInvariant();
            entity.ApprovalStatus = string.IsNullOrWhiteSpace(dto.ApprovalStatus) ? "DRAFT" : dto.ApprovalStatus.Trim().ToUpperInvariant();
            entity.LinkedRecipeId = linkedRecipe?.Id ?? dto.LinkedRecipeId;
            entity.ApprovedRecipeVersion = dto.ApprovedRecipeVersion ?? (linkedRecipe != null ? Math.Max(1, linkedRecipe.ActiveVersion > 0 ? linkedRecipe.ActiveVersion : linkedRecipe.Version) : null);
            entity.RecipeCombinationJson = dto.RecipeCombinationJson;
            entity.RecipeLinkStatus = entity.LinkedRecipeId != null ? "LINKED" : string.IsNullOrWhiteSpace(dto.RecipeLinkStatus) ? "MENU_ITEM_ONLY" : dto.RecipeLinkStatus.Trim().ToUpperInvariant();

            decimal? recipeCost = linkedRecipe is { TotalCost: > 0, ServingQty: > 0 } ? linkedRecipe.TotalCost / linkedRecipe.ServingQty : null;
            _db.MenuItemVersion.Add(new MenuItemVersion
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                MenuItemId = entity.Id,
                VersionNumber = entity.VersionNumber,
                Status = entity.ApprovalStatus,
                SellingPrice = entity.SellingPrice,
                IngredientCost = recipeCost,
                LinkedRecipeId = entity.LinkedRecipeId,
                ApprovedRecipeVersion = entity.ApprovedRecipeVersion,
                ChangeSummary = isNew ? "Created menu item" : $"Updated menu item v{entity.VersionNumber}",
                SnapshotJson = JsonSerializer.Serialize(new { entity.Name, entity.SellingPrice, entity.Category, entity.LinkedRecipeId, entity.ApprovedRecipeVersion, entity.Status }),
                ActionByUserId = userId,
                ActionAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            await _db.SaveChangesAsync(ct);
            List<MenuItemEnhancedDto> items = await BuildMenuItemsAsync(ctx, new SilaMenuEngineeringFilterDto { MenuItemId = entity.Id }, ct);
            return items.First();
        }

        public async Task<MenuItemEnhancedDto> CopyMenuItemAsync(Guid organizationId, Guid userId, Guid roleId, Guid menuItemId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuItems, "Access denied", "You do not have permission to copy menu items.");
            MenuItem src = await _db.MenuItem.FirstOrDefaultAsync(x => x.Id == menuItemId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Menu item not found.", "Select an active menu item.");

            return await SaveMenuItemAsync(organizationId, userId, roleId, new MenuItemEnhancedWriteDto
            {
                Name = $"{src.Name} (Copy)",
                AlternativeName = src.AlternativeName,
                Description = src.Description,
                Category = src.Category,
                SubCategory = src.SubCategory,
                Cuisine = src.Cuisine,
                Classification = src.Classification,
                MealPeriods = src.MealPeriods,
                SellingPrice = src.SellingPrice,
                Currency = src.Currency,
                TaxRatePercent = src.TaxRatePercent,
                TaxInclusive = src.TaxInclusive,
                DefaultPortionQty = src.DefaultPortionQty,
                PortionUom = src.PortionUom,
                ServiceFormat = src.ServiceFormat,
                DietaryTags = src.DietaryTags,
                AllergenNotes = src.AllergenNotes,
                PropertyId = src.PropertyId,
                OutletLocationId = src.OutletLocationId,
                LinkedRecipeId = src.LinkedRecipeId,
                ApprovedRecipeVersion = src.ApprovedRecipeVersion,
                Status = "DRAFT",
                ApprovalStatus = "DRAFT"
            }, ct);
        }

        public async Task<int> BulkPriceUpdateAsync(Guid organizationId, Guid userId, Guid roleId, MenuItemBulkPriceUpdateDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuItems, "Access denied", "You do not have permission to bulk update prices.");
            if (dto.MenuItemIds.Count == 0) return 0;

            List<MenuItem> items = await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && dto.MenuItemIds.Contains(x.Id)).ToListAsync(ct);
            List<Guid> recipeIds = items.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recipeIds.Count == 0
                ? new()
                : await _db.Recipe.Where(r => recipeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

            int count = 0;
            foreach (MenuItem item in items)
            {
                decimal? current = item.SellingPrice;
                decimal? nextPrice = current;
                if (dto.TargetFoodCostPercent is > 0 && item.LinkedRecipeId != null && recipes.TryGetValue(item.LinkedRecipeId.Value, out Recipe? rec) && rec.TotalCost > 0 && rec.ServingQty > 0)
                {
                    decimal ratio = dto.TargetFoodCostPercent.Value > 1 ? dto.TargetFoodCostPercent.Value / 100m : dto.TargetFoodCostPercent.Value;
                    decimal portionCost = rec.TotalCost / rec.ServingQty;
                    nextPrice = Math.Round(portionCost / ratio, 2);
                }
                else if (dto.PercentageChange != null && current != null)
                {
                    nextPrice = Math.Round(current.Value * (1m + (dto.PercentageChange.Value / 100m)), 2);
                }
                else if (dto.FixedAmountChange != null && current != null)
                {
                    nextPrice = Math.Max(0, Math.Round(current.Value + dto.FixedAmountChange.Value, 2));
                }

                if (nextPrice == current) continue;
                item.SellingPrice = nextPrice;
                item.VersionNumber += 1;
                count++;
                _db.MenuItemVersion.Add(new MenuItemVersion
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    MenuItemId = item.Id,
                    VersionNumber = item.VersionNumber,
                    Status = item.ApprovalStatus,
                    SellingPrice = item.SellingPrice,
                    LinkedRecipeId = item.LinkedRecipeId,
                    ApprovedRecipeVersion = item.ApprovedRecipeVersion,
                    ChangeSummary = $"Bulk price update ({dto.Reason ?? "Price adjustment"}) from {current} to {nextPrice}",
                    SnapshotJson = JsonSerializer.Serialize(new { item.Name, PreviousPrice = current, NewPrice = nextPrice }),
                    ActionByUserId = userId,
                    ActionAtUtc = DateTime.UtcNow,
                    IsActive = true
                });
            }
            await _db.SaveChangesAsync(ct);
            return count;
        }

        public async Task<int> BulkImportMenuItemsAsync(Guid organizationId, Guid userId, Guid roleId, MenuItemBulkImportDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuItems, "Access denied", "You do not have permission to import menu items.");
            if (string.IsNullOrWhiteSpace(dto.Payload)) return 0;

            string[] lines = dto.Payload.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return 0;

            // Detect header layout dynamically: supports both "Code,Name,Category,Cuisine,SellingPrice,PortionUom" and "Name,Category,Cuisine,SellingPrice,..."
            string[] firstCols = lines[0].Split(',').Select(c => c.Trim()).ToArray();
            bool hasCodeFirstHeader = firstCols.Length >= 2
                && firstCols[0].Equals("Code", StringComparison.OrdinalIgnoreCase)
                && firstCols[1].Equals("Name", StringComparison.OrdinalIgnoreCase);
            bool hasNameFirstHeader = firstCols.Length >= 1
                && firstCols[0].Equals("Name", StringComparison.OrdinalIgnoreCase);
            bool hasHeader = hasCodeFirstHeader || hasNameFirstHeader;

            int startIdx = hasHeader ? 1 : 0;
            int created = 0;
            for (int idx = startIdx; idx < lines.Length; idx++)
            {
                string line = lines[idx].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] cols = line.Split(',').Select(c => c.Trim()).ToArray();
                if (cols.Length == 0) continue;

                // Auto-detect code-first rows even when header was omitted (e.g., "ME-BULK-1,Grilled Lamb Rack,Mains,Mediterranean,38.00,PORTION")
                bool rowIsCodeFirst = hasCodeFirstHeader || (cols.Length >= 5 && decimal.TryParse(cols[4], NumberStyles.Any, CultureInfo.InvariantCulture, out _) && !decimal.TryParse(cols[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _));

                string name = rowIsCodeFirst ? (cols.Length > 1 ? cols[1] : string.Empty) : cols[0];
                if (string.IsNullOrWhiteSpace(name)) continue;

                string category = rowIsCodeFirst ? (cols.Length > 2 && !string.IsNullOrWhiteSpace(cols[2]) ? cols[2] : "Mains") : (cols.Length > 1 && !string.IsNullOrWhiteSpace(cols[1]) ? cols[1] : "Mains");
                string cuisine = rowIsCodeFirst ? (cols.Length > 3 && !string.IsNullOrWhiteSpace(cols[3]) ? cols[3] : "International") : (cols.Length > 2 && !string.IsNullOrWhiteSpace(cols[2]) ? cols[2] : "International");
                int priceCol = rowIsCodeFirst ? 4 : 3;
                decimal? price = cols.Length > priceCol && decimal.TryParse(cols[priceCol], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal p) && p >= 0m ? p : null;
                string portionUom = rowIsCodeFirst ? (cols.Length > 5 && !string.IsNullOrWhiteSpace(cols[5]) ? cols[5] : "PORTION") : "PORTION";
                string mealPeriods = rowIsCodeFirst ? (cols.Length > 6 && !string.IsNullOrWhiteSpace(cols[6]) ? cols[6] : "All") : (cols.Length > 4 && !string.IsNullOrWhiteSpace(cols[4]) ? cols[4] : "All");
                string serviceFormat = rowIsCodeFirst ? (cols.Length > 7 && !string.IsNullOrWhiteSpace(cols[7]) ? cols[7] : "A_LA_CARTE") : (cols.Length > 5 && !string.IsNullOrWhiteSpace(cols[5]) ? cols[5] : "A_LA_CARTE");
                string? dietary = rowIsCodeFirst ? (cols.Length > 8 ? cols[8] : null) : (cols.Length > 6 ? cols[6] : null);

                await SaveMenuItemAsync(organizationId, userId, roleId, new MenuItemEnhancedWriteDto
                {
                    Name = name,
                    Category = category,
                    Cuisine = cuisine,
                    SellingPrice = price,
                    PortionUom = portionUom,
                    MealPeriods = mealPeriods,
                    ServiceFormat = serviceFormat,
                    DietaryTags = dietary,
                    Status = "ACTIVE"
                }, ct);
                created++;
            }
            return created;
        }

        public async Task<List<MenuItemVersionDto>> GetMenuItemVersionsAsync(Guid organizationId, Guid roleId, Guid menuItemId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, null, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManageMenuItems, "Access denied", "Permission required.");
            return await _db.MenuItemVersion
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.MenuItemId == menuItemId)
                .OrderByDescending(x => x.VersionNumber)
                .Select(x => new MenuItemVersionDto
                {
                    Id = x.Id,
                    MenuItemId = x.MenuItemId,
                    VersionNumber = x.VersionNumber,
                    Status = x.Status,
                    SellingPrice = x.SellingPrice,
                    IngredientCost = ctx.Perms.ViewMenuCost ? x.IngredientCost : null,
                    LinkedRecipeId = x.LinkedRecipeId,
                    ApprovedRecipeVersion = x.ApprovedRecipeVersion,
                    ChangeSummary = x.ChangeSummary,
                    ActionAtUtc = x.ActionAtUtc
                }).ToListAsync(ct);
        }

        public async Task<List<MenuPlanEnhancedDto>> GetMenuPlansAsync(Guid organizationId, Guid roleId, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, filter.PropertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio, "Access denied", "Permission required.");
            return await BuildMenuPlansAsync(ctx, filter, ct);
        }

        public async Task<MenuPlanEnhancedDto> SaveMenuPlanAsync(Guid organizationId, Guid userId, Guid roleId, MenuPlanEnhancedWriteDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, dto.PropertyId, requireFeatureEnabled: true, ct);
            bool isNew = dto.Id == null;
            MenuEngineeringPermissions.EnsureAllowed(isNew ? ctx.Perms.CreateMenu : ctx.Perms.EditDraftMenu, "Access denied", "Permission required to create or edit menus.");
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new BadRequestCustomException("Menu name is required.", "Provide a menu name.");

            MenuPlan? existing = dto.Id != null
                ? await _db.MenuPlan.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.Id && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                : null;

            // Scenario 11 / Section 24: Published/Approved menus are immutable! Editing creates a controlled new Draft version.
            if (existing != null && (existing.Status == "PUBLISHED" || existing.Status == "APPROVED" || existing.Status == "SCHEDULED"))
            {
                MenuPlan cloned = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    PlanCode = $"{existing.PlanCode}-V{existing.VersionNumber + 1}",
                    Name = dto.Name.Trim(),
                    PropertyId = dto.PropertyId ?? existing.PropertyId,
                    OutletLocationId = dto.OutletLocationId ?? existing.OutletLocationId,
                    BusinessDate = dto.BusinessDate == default ? existing.BusinessDate : dto.BusinessDate.Date,
                    StartDate = dto.StartDate ?? existing.StartDate,
                    EndDate = dto.EndDate ?? existing.EndDate,
                    MealPeriod = dto.MealPeriod ?? existing.MealPeriod,
                    EventName = dto.EventName ?? existing.EventName,
                    MenuType = dto.MenuType ?? existing.MenuType,
                    ServiceFormat = dto.ServiceFormat ?? existing.ServiceFormat,
                    VenueName = dto.VenueName ?? existing.VenueName,
                    RotationRule = dto.RotationRule ?? existing.RotationRule,
                    IsTemplate = dto.IsTemplate,
                    TemplateCategory = dto.TemplateCategory ?? existing.TemplateCategory,
                    ExpectedGuests = dto.ExpectedGuests ?? existing.ExpectedGuests,
                    ExpectedCovers = dto.ExpectedCovers ?? existing.ExpectedCovers,
                    MealParticipation = dto.MealParticipation ?? existing.MealParticipation,
                    TargetFoodCostPercent = dto.TargetFoodCostPercent ?? existing.TargetFoodCostPercent,
                    TargetContributionMargin = dto.TargetContributionMargin ?? existing.TargetContributionMargin,
                    DietaryRequirements = dto.DietaryRequirements ?? existing.DietaryRequirements,
                    Status = "DRAFT",
                    VersionNumber = existing.VersionNumber + 1,
                    ParentPublishedPlanId = existing.Id,
                    SourceType = "REVISION",
                    SourceReferenceId = existing.Id,
                    Notes = dto.Notes ?? existing.Notes,
                    PreventConflicts = dto.PreventConflicts,
                    CategoriesOrderJson = JsonSerializer.Serialize(dto.CategoriesOrder.Count > 0 ? dto.CategoriesOrder : DefaultCategories.ToList()),
                    IsActive = true
                };
                _db.MenuPlan.Add(cloned);
                await ReplacePlanLinesAsync(ctx, cloned, dto.Lines, ct);
                _db.MenuEngineeringApprovalHistory.Add(new MenuEngineeringApprovalHistory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    EntityType = "MENU_PLAN",
                    EntityId = cloned.Id,
                    VersionNumber = cloned.VersionNumber,
                    FromStatus = existing.Status,
                    ToStatus = "DRAFT",
                    ActionByUserId = userId,
                    Reason = $"Created controlled draft v{cloned.VersionNumber} from immutable {existing.Status} menu {existing.PlanCode}",
                    ActionAtUtc = DateTime.UtcNow,
                    IsActive = true
                });
                await _db.SaveChangesAsync(ct);
                return (await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto { MenuPlanId = cloned.Id }, ct)).First();
            }

            // Conflict check if configured
            if (dto.PreventConflicts && !dto.IsTemplate && dto.OutletLocationId != null)
            {
                DateTime bDate = (dto.BusinessDate == default ? DateTime.UtcNow.Date : dto.BusinessDate.Date);
                bool conflict = await _db.MenuPlan.AnyAsync(x =>
                    x.BuyerId == ctx.Buyer.Id && x.IsActive && x.Id != (existing == null ? Guid.Empty : existing.Id) &&
                    x.OutletLocationId == dto.OutletLocationId && x.BusinessDate == bDate &&
                    x.MealPeriod == dto.MealPeriod && (x.Status == "PUBLISHED" || x.Status == "SCHEDULED"), ct);
                if (conflict)
                {
                    throw new BadRequestCustomException(
                        "Conflicting active menu version exists for this outlet, date, and meal period.",
                        "Archive/suspend the existing menu or uncheck Prevent Conflicts.");
                }
            }

            MenuPlan plan = existing ?? new MenuPlan
            {
                Id = dto.Id ?? Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                PlanCode = $"MP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
                Status = "DRAFT",
                VersionNumber = 1,
                IsActive = true
            };
            if (existing == null) _db.MenuPlan.Add(plan);

            plan.Name = dto.Name.Trim();
            plan.PropertyId = dto.PropertyId;
            plan.OutletLocationId = dto.OutletLocationId;
            plan.BusinessDate = dto.BusinessDate == default ? DateTime.UtcNow.Date : dto.BusinessDate.Date;
            plan.StartDate = dto.StartDate;
            plan.EndDate = dto.EndDate;
            plan.MealPeriod = dto.MealPeriod ?? "All";
            plan.EventName = dto.EventName;
            plan.MenuType = dto.MenuType ?? "DAILY";
            plan.ServiceFormat = dto.ServiceFormat ?? "A_LA_CARTE";
            plan.VenueName = dto.VenueName;
            plan.RotationRule = dto.RotationRule;
            plan.IsTemplate = dto.IsTemplate;
            plan.TemplateCategory = dto.TemplateCategory;
            plan.ExpectedGuests = dto.ExpectedGuests;
            plan.ExpectedCovers = dto.ExpectedCovers ?? dto.ExpectedGuests ?? 100m;
            plan.MealParticipation = dto.MealParticipation ?? ctx.Settings.DefaultMealParticipation;
            plan.TargetFoodCostPercent = dto.TargetFoodCostPercent ?? ctx.Settings.TargetFoodCostPercent;
            plan.TargetContributionMargin = dto.TargetContributionMargin ?? ctx.Settings.TargetContributionMargin;
            plan.DietaryRequirements = dto.DietaryRequirements;
            plan.Notes = dto.Notes;
            plan.CopiedFromPlanId = dto.CopyFromPlanId ?? plan.CopiedFromPlanId;
            plan.SourceType = dto.SourceType ?? plan.SourceType ?? "SCRATCH";
            plan.SourceReferenceId = dto.SourceReferenceId ?? plan.SourceReferenceId;
            plan.PreventConflicts = dto.PreventConflicts;
            plan.CategoriesOrderJson = JsonSerializer.Serialize(dto.CategoriesOrder.Count > 0 ? dto.CategoriesOrder : DefaultCategories.ToList());

            await ReplacePlanLinesAsync(ctx, plan, dto.Lines, ct);
            if (existing == null)
            {
                _db.MenuEngineeringApprovalHistory.Add(new MenuEngineeringApprovalHistory
                {
                    Id = Guid.NewGuid(),
                    BuyerId = ctx.Buyer.Id,
                    EntityType = "MENU_PLAN",
                    EntityId = plan.Id,
                    VersionNumber = plan.VersionNumber,
                    FromStatus = "NONE",
                    ToStatus = "DRAFT",
                    ActionByUserId = userId,
                    Reason = $"Created menu via {plan.SourceType}",
                    ActionAtUtc = DateTime.UtcNow,
                    IsActive = true
                });
            }
            await _db.SaveChangesAsync(ct);
            return (await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto { MenuPlanId = plan.Id }, ct)).First();
        }

        public async Task<MenuPlanEnhancedDto> CreateMenuFromHistoricalAsync(Guid organizationId, Guid userId, Guid roleId, Guid historicalSourceId, string sourceKind, Guid? propertyId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.CreateMenu, "Access denied", "Permission required to create menu from historical performance.");

            List<MenuPlanLineEnhancedWriteDto> lines = new();
            string name = $"Historical Menu {DateTime.UtcNow:yyyy-MM-dd}";
            string? meal = "Dinner";
            decimal covers = 100m;

            if (string.Equals(sourceKind, "HISTORICAL_SNAPSHOT", StringComparison.OrdinalIgnoreCase))
            {
                SilaCulinaryHistoricalLearningSnapshot snap = await _db.SilaCulinaryHistoricalLearningSnapshot
                    .FirstOrDefaultAsync(x => x.Id == historicalSourceId && x.BuyerId == ctx.Buyer.Id, ct)
                    ?? throw new NotFoundCustomException("Historical snapshot not found.", "Select a valid historical record.");
                name = $"{snap.EventType} {snap.MealPeriod} (Recosted)";
                meal = snap.MealPeriod;
                covers = snap.ActualGuestCount > 0 ? snap.ActualGuestCount : snap.ExpectedGuestCount;
                List<MenuItem> activeItems = await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.LinkedRecipeId != null).Take(8).ToListAsync(ct);
                int seq = 0;
                foreach (MenuItem item in activeItems)
                {
                    lines.Add(new MenuPlanLineEnhancedWriteDto
                    {
                        MenuItemId = item.Id,
                        CategoryName = item.Category ?? "Mains",
                        SequenceOrder = seq++,
                        RecipeId = item.LinkedRecipeId,
                        PortionSize = item.DefaultPortionQty,
                        PortionUom = item.PortionUom ?? "PORTION",
                        SellingPrice = item.SellingPrice,
                        ExpectedCovers = covers
                    });
                }
            }
            else
            {
                MenuPlan src = await _db.MenuPlan.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == historicalSourceId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                    ?? throw new NotFoundCustomException("Historical menu not found.", "Select a valid historical menu.");
                name = $"{src.Name} (From Historical)";
                meal = src.MealPeriod;
                covers = src.ExpectedCovers ?? src.ExpectedGuests ?? 100m;
                foreach (MenuPlanLine l in src.Lines.Where(x => x.IsActive).OrderBy(x => x.SequenceOrder))
                {
                    lines.Add(new MenuPlanLineEnhancedWriteDto
                    {
                        MenuItemId = l.MenuItemId,
                        CategoryName = l.CategoryName,
                        SequenceOrder = l.SequenceOrder,
                        ParticipationRate = l.ParticipationRate,
                        BufferPercentage = l.BufferPercentage,
                        PortionSize = l.PortionSize,
                        PortionUom = l.PortionUom,
                        SellingPrice = null, // recalculate from current item & approved recipe version!
                        ExpectedCovers = covers
                    });
                }
            }

            return await SaveMenuPlanAsync(organizationId, userId, roleId, new MenuPlanEnhancedWriteDto
            {
                Name = name,
                PropertyId = propertyId,
                BusinessDate = DateTime.UtcNow.Date,
                MealPeriod = meal,
                MenuType = "EVENT",
                ServiceFormat = "BANQUET",
                ExpectedCovers = covers,
                ExpectedGuests = covers,
                SourceType = "HISTORICAL",
                SourceReferenceId = historicalSourceId,
                Notes = "Created from historical performance and recalculated using current approved recipes, current prices, and current stock.",
                Lines = lines
            }, ct);
        }

        public async Task<MenuPlanEnhancedDto> CreateMenuFromAiStrategyAsync(Guid organizationId, Guid userId, Guid roleId, string strategyKey, Guid? propertyId, decimal? expectedCovers, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, propertyId, requireFeatureEnabled: true, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.CreateMenu && ctx.Perms.UseAiMenuIntelligence, "Access denied", "Permission required.");

            List<MenuItemEnhancedDto> items = await BuildMenuItemsAsync(ctx, new SilaMenuEngineeringFilterDto { PropertyId = propertyId }, ct);
            MenuPurchasePlanDto purchase = await BuildPurchasePlanInternalAsync(ctx, new SilaMenuEngineeringFilterDto { PropertyId = propertyId }, ct);
            AiMenuIntelligenceDto ai = await GetAiMenuIntelligenceInternalAsync(ctx, null, propertyId, ct);
            AiMenuStrategyOptionDto? chosen = ai.AlternativeMenus.FirstOrDefault(x => string.Equals(x.StrategyKey, strategyKey, StringComparison.OrdinalIgnoreCase))
                ?? ai.AlternativeMenus.FirstOrDefault();

            List<Guid> ids = chosen?.RecommendedMenuItemIds.Count > 0 ? chosen.RecommendedMenuItemIds : items.Take(6).Select(x => x.Id!.Value).ToList();
            decimal covers = expectedCovers ?? 120m;
            int seq = 0;
            List<MenuPlanLineEnhancedWriteDto> lines = ids.Select(id =>
            {
                MenuItemEnhancedDto? item = items.FirstOrDefault(x => x.Id == id);
                return new MenuPlanLineEnhancedWriteDto
                {
                    MenuItemId = id,
                    CategoryName = item?.Category ?? "Mains",
                    SequenceOrder = seq++,
                    RecipeId = item?.LinkedRecipeId,
                    RecipeVersion = item?.ApprovedRecipeVersion,
                    PortionSize = item?.DefaultPortionQty ?? 1m,
                    PortionUom = item?.PortionUom ?? "PORTION",
                    SellingPrice = item?.SellingPrice,
                    ExpectedCovers = covers
                };
            }).ToList();

            return await SaveMenuPlanAsync(organizationId, userId, roleId, new MenuPlanEnhancedWriteDto
            {
                Name = $"{chosen?.Title ?? "AI Balanced"} Draft",
                PropertyId = propertyId,
                BusinessDate = DateTime.UtcNow.Date,
                MealPeriod = "Dinner",
                MenuType = "SPECIAL_OCCASION",
                ServiceFormat = "A_LA_CARTE",
                ExpectedCovers = covers,
                ExpectedGuests = covers,
                SourceType = "AI_RECOMMENDATION",
                Notes = chosen?.Summary ?? "Generated from AI Menu Intelligence strategy.",
                Lines = lines
            }, ct);
        }

        public async Task<MenuPlanEnhancedDto> TransitionMenuPlanAsync(Guid organizationId, Guid userId, Guid roleId, MenuPlanTransitionDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, null, requireFeatureEnabled: true, ct);
            MenuPlan plan = await _db.MenuPlan.Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == dto.MenuPlanId && x.BuyerId == ctx.Buyer.Id && x.IsActive, ct)
                ?? throw new NotFoundCustomException("Menu plan not found.", "Select a valid menu.");

            string target = (dto.TargetStatus ?? "").Trim().ToUpperInvariant();
            string[] valid = ["DRAFT", "UNDER_REVIEW", "CHANGES_REQUESTED", "PENDING_APPROVAL", "APPROVED", "SCHEDULED", "PUBLISHED", "SUSPENDED", "ARCHIVED"];
            if (!valid.Contains(target)) throw new BadRequestCustomException("Invalid menu status transition.", $"Allowed: {string.Join(", ", valid)}");

            if (target is "APPROVED")
                MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ApproveMenu, "Approval denied", "APPROVE_SILA_MENU permission required.");
            else if (target is "PUBLISHED" or "SCHEDULED")
                MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.PublishMenu || ctx.Perms.ApproveMenu, "Publish denied", "PUBLISH_SILA_MENU permission required.");
            else
                MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.EditDraftMenu || ctx.Perms.ApproveMenu, "Edit denied", "Permission required.");

            string prev = plan.Status;
            plan.Status = target;
            plan.ApprovalComment = dto.Comment ?? plan.ApprovalComment;
            if (target is "UNDER_REVIEW" or "PENDING_APPROVAL")
            {
                plan.SubmittedByUserId = userId;
                plan.SubmittedAtUtc = DateTime.UtcNow;
            }
            else if (target == "APPROVED")
            {
                plan.ApprovedByUserId = userId;
                plan.ApprovedAtUtc = DateTime.UtcNow;
            }
            else if (target == "PUBLISHED")
            {
                plan.PublishedByUserId = userId;
                plan.PublishedAtUtc = DateTime.UtcNow;
            }
            else if (target == "SCHEDULED" && dto.ScheduledDateUtc != null)
            {
                plan.BusinessDate = dto.ScheduledDateUtc.Value.Date;
            }

            _db.MenuEngineeringApprovalHistory.Add(new MenuEngineeringApprovalHistory
            {
                Id = Guid.NewGuid(),
                BuyerId = ctx.Buyer.Id,
                EntityType = "MENU_PLAN",
                EntityId = plan.Id,
                VersionNumber = plan.VersionNumber,
                FromStatus = prev,
                ToStatus = target,
                ActionByUserId = userId,
                ApproverRole = roleId.ToString(),
                Reason = dto.Comment ?? $"Transitioned {prev} -> {target}",
                ActionAtUtc = DateTime.UtcNow,
                IsActive = true
            });

            await _db.SaveChangesAsync(ct);
            return (await BuildMenuPlansAsync(ctx, new SilaMenuEngineeringFilterDto { MenuPlanId = plan.Id }, ct)).First();
        }

        private async Task ReplacePlanLinesAsync(StudioCtx ctx, MenuPlan plan, List<MenuPlanLineEnhancedWriteDto> lineDtos, CancellationToken ct)
        {
            List<MenuPlanLine> existingLines = await _db.MenuPlanLine.Where(x => x.MenuPlanId == plan.Id && x.IsActive).ToListAsync(ct);
            foreach (MenuPlanLine old in existingLines) old.IsActive = false;

            List<Guid> itemIds = lineDtos.Select(x => x.MenuItemId).Distinct().ToList();
            Dictionary<Guid, MenuItem> items = itemIds.Count == 0
                ? new()
                : await _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && itemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            List<Guid> recipeIds = items.Values.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recipeIds.Count == 0
                ? new()
                : await _db.Recipe.Where(x => recipeIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);

            int idx = 0;
            foreach (MenuPlanLineEnhancedWriteDto dto in lineDtos)
            {
                items.TryGetValue(dto.MenuItemId, out MenuItem? item);
                Recipe? recipe = item?.LinkedRecipeId != null && recipes.TryGetValue(item.LinkedRecipeId.Value, out Recipe? r) ? r : null;
                decimal covers = dto.ExpectedCovers ?? plan.ExpectedCovers ?? plan.ExpectedGuests ?? 100m;
                decimal mealPart = string.Equals(plan.ServiceFormat, "BUFFET", StringComparison.OrdinalIgnoreCase)
                    ? 1m
                    : (plan.MealParticipation ?? ctx.Settings.DefaultMealParticipation);
                decimal itemPart = dto.ParticipationRate ?? item?.DefaultParticipationRate ?? ctx.Settings.DefaultItemParticipation;
                decimal servings = dto.ExpectedServingsPerGuest ?? 1m;
                decimal buf = dto.BufferPercentage ?? item?.DefaultBufferPercentage ?? ctx.Settings.DefaultBufferPercentage;
                decimal recPortions = Math.Round(covers * mealPart * itemPart * servings * (1m + buf), 2);
                int? approvedVer = dto.RecipeVersion ?? item?.ApprovedRecipeVersion ?? (recipe == null ? null : Math.Max(1, recipe.ActiveVersion > 0 ? recipe.ActiveVersion : recipe.Version));

                _db.MenuPlanLine.Add(new MenuPlanLine
                {
                    Id = Guid.NewGuid(),
                    MenuPlanId = plan.Id,
                    MenuItemId = dto.MenuItemId,
                    CategoryName = dto.CategoryName ?? item?.Category ?? "Mains",
                    SequenceOrder = dto.SequenceOrder > 0 ? dto.SequenceOrder : idx,
                    RecipeId = dto.RecipeId ?? item?.LinkedRecipeId,
                    RecipeVersion = approvedVer,
                    ParticipationRate = itemPart,
                    BufferPercentage = buf,
                    AdultFactor = dto.AdultFactor ?? item?.AdultPortionFactor ?? ctx.Settings.DefaultAdultFactor,
                    ChildFactor = dto.ChildFactor ?? item?.ChildPortionFactor ?? ctx.Settings.DefaultChildFactor,
                    PortionSize = dto.PortionSize ?? item?.DefaultPortionQty ?? 1m,
                    PortionUom = dto.PortionUom ?? item?.PortionUom ?? "PORTION",
                    ExpectedServingsPerGuest = servings,
                    SellingPrice = dto.SellingPrice ?? item?.SellingPrice,
                    ExpectedCovers = covers,
                    SampledActualPortion = dto.SampledActualPortion,
                    RecommendedPortions = recPortions,
                    PlannedPortions = dto.PlannedPortions ?? recPortions,
                    PortionsConfirmed = dto.ConfirmPortions ?? false,
                    OverrideReason = dto.OverrideReason,
                    LineNotes = dto.LineNotes,
                    AlternativesJson = dto.AlternativesJson,
                    DietaryTags = dto.DietaryTags ?? item?.DietaryTags,
                    Warning = item?.LinkedRecipeId == null ? "RECIPE_REQUIRED" : null,
                    IsActive = true
                });
                idx++;
            }
        }

        private async Task<SilaMenuLookupsDto> BuildLookupsAsync(StudioCtx ctx, CancellationToken ct)
        {
            List<BuyerProperty> properties = await _db.BuyerProperty.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).ToListAsync(ct);
            List<Recipe> recipes = await _db.Recipe.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive).OrderBy(x => x.Name).ToListAsync(ct);
            return new SilaMenuLookupsDto
            {
                Properties = properties.Select(x => new SilaMenuNamedRefDto { Id = x.Id, Name = x.PropertyName, Code = x.PlantCode }).ToList(),
                Outlets = ctx.Scope.Select(x => new SilaMenuNamedRefDto { Id = x.Id, Name = x.LocationName, Code = x.LocationCode, PropertyId = x.PropertyId }).ToList(),
                MealPeriods = ReadList(ctx.Settings.MealPeriodsJson, DefaultMeals),
                Categories = ReadList(ctx.Settings.CategoriesJson, DefaultCategories),
                Recipes = recipes.Select(x => new SilaMenuNamedRefDto { Id = x.Id, Name = x.Name, Code = x.RecipeCode }).ToList()
            };
        }

        private async Task<List<MenuItemEnhancedDto>> BuildMenuItemsAsync(StudioCtx ctx, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            IQueryable<MenuItem> query = _db.MenuItem.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive);
            if (filter.MenuItemId != null) query = query.Where(x => x.Id == filter.MenuItemId);
            if (filter.PropertyId != null) query = query.Where(x => x.PropertyId == null || x.PropertyId == filter.PropertyId);
            if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(x => x.Category == filter.Category);
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string q = filter.Search.Trim().ToLowerInvariant();
                query = query.Where(x => x.Name.ToLower().Contains(q) || x.MenuItemCode.ToLower().Contains(q) || (x.Cuisine ?? "").ToLower().Contains(q));
            }
            List<MenuItem> items = await query.OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync(ct);

            List<Guid> recipeIds = items.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recipeIds.Count == 0
                ? new()
                : await _db.Recipe.Include(x => x.Ingredients).Where(x => recipeIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);

            // Historical POS quantities by MenuItemId (via MenuPosMapping)
            List<MenuPosMapping> maps = await _db.MenuPosMapping.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && x.MenuItemId != null).ToListAsync(ct);
            Dictionary<string, Guid> posToMenu = maps.GroupBy(m => m.PosCode, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First().MenuItemId!.Value, StringComparer.OrdinalIgnoreCase);
            List<PosSalesTransaction> recentSales = await _db.PosSalesTransaction
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && !x.IsVoid && !x.IsRefund)
                .ToListAsync(ct);
            Dictionary<Guid, decimal> soldByItem = new();
            foreach (PosSalesTransaction s in recentSales)
            {
                Guid? itemId = s.MenuItemId ?? (posToMenu.TryGetValue(s.PosCode, out Guid mId) ? mId : null);
                if (itemId != null) soldByItem[itemId.Value] = soldByItem.GetValueOrDefault(itemId.Value) + s.QuantitySold;
            }
            decimal totalSold = soldByItem.Values.Sum();

            // Ingredient stock check
            Dictionary<Guid, decimal> stockByMat = ctx.ScopeIds.Count == 0
                ? new()
                : (await _db.InventoryBalance.Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive && ctx.ScopeIds.Contains(x.LocationId)).ToListAsync(ct))
                    .GroupBy(x => x.MaterialId).ToDictionary(g => g.Key, g => g.Sum(r => r.OnHandQty));

            List<MenuItemEnhancedDto> result = new();
            foreach (MenuItem item in items)
            {
                recipes.TryGetValue(item.LinkedRecipeId ?? Guid.Empty, out Recipe? recipe);
                decimal? portionCost = recipe is { TotalCost: > 0, ServingQty: > 0 }
                    ? Math.Round((recipe.TotalCost / recipe.ServingQty) * (item.DefaultPortionQty > 0 ? item.DefaultPortionQty : 1m), 4)
                    : null;
                decimal? netSell = item.SellingPrice == null
                    ? null
                    : item.TaxInclusive && item.TaxRatePercent is > 0
                        ? Math.Round(item.SellingPrice.Value / (1m + (item.TaxRatePercent.Value / 100m)), 4)
                        : item.SellingPrice.Value;
                decimal? taxInc = item.SellingPrice == null
                    ? null
                    : item.TaxInclusive
                        ? item.SellingPrice.Value
                        : item.TaxRatePercent is > 0
                            ? Math.Round(item.SellingPrice.Value * (1m + (item.TaxRatePercent.Value / 100m)), 4)
                            : item.SellingPrice.Value;
                decimal? fcPct = netSell is > 0 && portionCost != null ? Math.Round(portionCost.Value / netSell.Value * 100m, 2) : null;
                decimal? cm = netSell != null && portionCost != null ? Math.Round(netSell.Value - portionCost.Value, 2) : null;
                decimal qty = soldByItem.GetValueOrDefault(item.Id);
                decimal popIdx = totalSold > 0 ? Math.Round(qty / totalSold * 100m, 2) : 0m;

                string avail = "READY";
                if (recipe == null) avail = "RECIPE_REQUIRED";
                else if (recipe.Ingredients.Any(ing => ing.MaterialId != null && stockByMat.GetValueOrDefault(ing.MaterialId.Value) < ing.Quantity))
                    avail = "SHORT";

                MenuItemEnhancedDto dto = new()
                {
                    Id = item.Id,
                    MenuItemCode = item.MenuItemCode,
                    Name = item.Name,
                    AlternativeName = item.AlternativeName,
                    Description = item.Description,
                    Category = item.Category,
                    SubCategory = item.SubCategory,
                    Cuisine = item.Cuisine,
                    Classification = item.Classification,
                    MealPeriods = item.MealPeriods,
                    ServiceType = item.ServiceType,
                    ServiceFormat = item.ServiceFormat ?? item.ServiceType ?? "A_LA_CARTE",
                    SellingPrice = item.SellingPrice,
                    NetSellingPrice = netSell,
                    TaxInclusiveSellingPrice = taxInc,
                    TaxRatePercent = item.TaxRatePercent,
                    TaxInclusive = item.TaxInclusive,
                    Currency = item.Currency ?? ctx.Settings.DefaultCurrency ?? "USD",
                    DefaultPortionQty = item.DefaultPortionQty,
                    PortionUom = item.PortionUom,
                    AdultPortionFactor = item.AdultPortionFactor,
                    ChildPortionFactor = item.ChildPortionFactor,
                    DefaultParticipationRate = item.DefaultParticipationRate,
                    DefaultBufferPercentage = item.DefaultBufferPercentage,
                    DietaryTags = item.DietaryTags,
                    AllergenNotes = item.AllergenNotes,
                    ImagePath = item.ImagePath,
                    PropertyId = item.PropertyId,
                    OutletLocationId = item.OutletLocationId,
                    PosItemReference = item.PosItemReference,
                    PrepTimeMinutes = item.PrepTimeMinutes ?? recipe?.PrepTimeMinutes,
                    IsAvailable = item.IsAvailable,
                    SeasonalAvailability = item.SeasonalAvailability,
                    EffectiveFrom = item.EffectiveFrom,
                    EffectiveTo = item.EffectiveTo,
                    Status = item.Status,
                    ApprovalStatus = item.ApprovalStatus,
                    VersionNumber = item.VersionNumber,
                    LinkedRecipeId = item.LinkedRecipeId,
                    ApprovedRecipeVersion = item.ApprovedRecipeVersion ?? (recipe == null ? null : Math.Max(1, recipe.ActiveVersion > 0 ? recipe.ActiveVersion : recipe.Version)),
                    RecipeCombinationJson = item.RecipeCombinationJson,
                    RecipeLinkStatus = item.RecipeLinkStatus,
                    RecipeName = recipe?.Name ?? "",
                    RecipeStatus = recipe?.Status ?? "UNLINKED",
                    IngredientCost = portionCost,
                    CostPerPortion = portionCost,
                    FoodCostPercent = fcPct,
                    ContributionMargin = cm,
                    HistoricalSalesQty = qty,
                    PopularityIndex = popIdx,
                    MatrixClassification = qty >= ctx.Settings.MinimumSalesQuantity && cm != null ? (popIdx >= 5m && fcPct is <= 32m ? "STAR" : popIdx >= 5m ? "PLOWHORSE" : fcPct is <= 32m ? "PUZZLE" : "DOG") : "UNCLASSIFIED",
                    CostState = recipe == null ? "RECIPE_REQUIRED" : portionCost == null ? "COST_PENDING" : "READY",
                    IngredientAvailabilityStatus = avail,
                    CreatedAt = item.DateCreated,
                    UpdatedAt = item.DateUpdated
                };
                MenuEngineeringPermissions.RedactFinancials(ctx.Perms, dto);
                result.Add(dto);
            }
            return result;
        }

        private async Task<List<MenuPlanEnhancedDto>> BuildMenuPlansAsync(StudioCtx ctx, SilaMenuEngineeringFilterDto filter, CancellationToken ct)
        {
            IQueryable<MenuPlan> query = _db.MenuPlan.Include(x => x.Lines).Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive);
            if (filter.MenuPlanId != null) query = query.Where(x => x.Id == filter.MenuPlanId);
            if (filter.PropertyId != null) query = query.Where(x => x.PropertyId == null || x.PropertyId == filter.PropertyId);
            if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(x => x.Status == filter.Status);
            List<MenuPlan> plans = await query.OrderByDescending(x => x.BusinessDate).ThenByDescending(x => x.VersionNumber).Take(60).ToListAsync(ct);

            Dictionary<Guid, string> propNames = await _db.BuyerProperty.Where(x => x.BuyerId == ctx.Buyer.Id).ToDictionaryAsync(x => x.Id, x => x.PropertyName, ct);
            Dictionary<Guid, string> outletNames = ctx.Scope.ToDictionary(x => x.Id, x => x.LocationName);
            List<Guid> allItemIds = plans.SelectMany(p => p.Lines.Where(l => l.IsActive).Select(l => l.MenuItemId)).Distinct().ToList();
            Dictionary<Guid, MenuItem> items = allItemIds.Count == 0 ? new() : await _db.MenuItem.Where(x => allItemIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
            List<Guid> recipeIds = items.Values.Where(x => x.LinkedRecipeId != null).Select(x => x.LinkedRecipeId!.Value).Distinct().ToList();
            Dictionary<Guid, Recipe> recipes = recipeIds.Count == 0 ? new() : await _db.Recipe.Where(x => recipeIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);

            List<MenuPlanEnhancedDto> output = new();
            foreach (MenuPlan plan in plans)
            {
                List<MenuPlanLineEnhancedDto> lines = new();
                foreach (MenuPlanLine l in plan.Lines.Where(x => x.IsActive).OrderBy(x => x.SequenceOrder))
                {
                    items.TryGetValue(l.MenuItemId, out MenuItem? item);
                    Recipe? recipe = (l.RecipeId ?? item?.LinkedRecipeId) != null && recipes.TryGetValue((l.RecipeId ?? item!.LinkedRecipeId)!.Value, out Recipe? r) ? r : null;
                    decimal covers = l.ExpectedCovers ?? plan.ExpectedCovers ?? plan.ExpectedGuests ?? 100m;
                    decimal part = l.ParticipationRate ?? item?.DefaultParticipationRate ?? ctx.Settings.DefaultItemParticipation;
                    decimal mealPart = string.Equals(plan.ServiceFormat, "BUFFET", StringComparison.OrdinalIgnoreCase)
                        ? 1m
                        : (plan.MealParticipation ?? ctx.Settings.DefaultMealParticipation);
                    decimal servings = l.ExpectedServingsPerGuest ?? 1m;
                    decimal buf = l.BufferPercentage ?? item?.DefaultBufferPercentage ?? ctx.Settings.DefaultBufferPercentage;
                    decimal recPortions = l.RecommendedPortions > 0 ? l.RecommendedPortions : Math.Round(covers * mealPart * part * servings * (1m + buf), 2);
                    decimal plannedPortions = l.PlannedPortions ?? recPortions;
                    decimal portionSize = l.PortionSize ?? item?.DefaultPortionQty ?? 1m;
                    decimal? portionCost = recipe is { TotalCost: > 0, ServingQty: > 0 } ? Math.Round((recipe.TotalCost / recipe.ServingQty) * portionSize, 4) : null;
                    decimal? sell = l.SellingPrice ?? item?.SellingPrice;
                    decimal? totalLineCost = portionCost != null ? Math.Round(portionCost.Value * plannedPortions, 2) : null;
                    decimal? lineRev = sell != null ? Math.Round(sell.Value * plannedPortions, 2) : null;

                    lines.Add(new MenuPlanLineEnhancedDto
                    {
                        Id = l.Id,
                        MenuItemId = l.MenuItemId,
                        MenuItemName = item?.Name ?? "Menu Item",
                        MenuItemCode = item?.MenuItemCode ?? "",
                        CategoryName = l.CategoryName ?? item?.Category ?? "Mains",
                        SequenceOrder = l.SequenceOrder,
                        RecipeId = l.RecipeId ?? item?.LinkedRecipeId,
                        RecipeName = recipe?.Name,
                        RecipeVersion = l.RecipeVersion ?? item?.ApprovedRecipeVersion,
                        RecipeLinkStatus = item?.RecipeLinkStatus ?? "MENU_ITEM_ONLY",
                        ImagePath = item?.ImagePath,
                        AllergenNotes = item?.AllergenNotes,
                        DietaryTags = l.DietaryTags ?? item?.DietaryTags,
                        ParticipationRate = part,
                        BufferPercentage = buf,
                        AdultFactor = l.AdultFactor,
                        ChildFactor = l.ChildFactor,
                        PortionSize = portionSize,
                        PortionUom = l.PortionUom ?? item?.PortionUom ?? "PORTION",
                        ExpectedServingsPerGuest = servings,
                        SellingPrice = ctx.Perms.ViewMeStudio ? sell : null,
                        ExpectedCovers = covers,
                        SampledActualPortion = l.SampledActualPortion,
                        RecommendedPortions = recPortions,
                        PlannedPortions = plannedPortions,
                        RequiredUsableOutput = Math.Round(plannedPortions * portionSize, 4),
                        PortionsConfirmed = l.PortionsConfirmed,
                        ActualPortions = l.ActualPortions,
                        Variance = l.ActualPortions != null ? l.ActualPortions.Value - plannedPortions : null,
                        PortionCost = ctx.Perms.ViewMenuCost ? portionCost : null,
                        TotalLineCost = ctx.Perms.ViewMenuCost ? totalLineCost : null,
                        ExpectedLineRevenue = lineRev,
                        FoodCostPercent = ctx.Perms.ViewMenuCost && lineRev is > 0 && totalLineCost != null ? Math.Round(totalLineCost.Value / lineRev.Value * 100m, 2) : null,
                        ContributionMargin = ctx.Perms.ViewMenuMargin && lineRev != null && totalLineCost != null ? Math.Round(lineRev.Value - totalLineCost.Value, 2) : null,
                        AvailabilityStatus = recipe == null ? "RECIPE_REQUIRED" : "READY",
                        OverrideReason = l.OverrideReason,
                        LineNotes = l.LineNotes,
                        AlternativesJson = l.AlternativesJson,
                        Warning = l.Warning
                    });
                }

                output.Add(new MenuPlanEnhancedDto
                {
                    Id = plan.Id,
                    PlanCode = plan.PlanCode,
                    Name = plan.Name,
                    PropertyId = plan.PropertyId,
                    PropertyName = plan.PropertyId != null && propNames.TryGetValue(plan.PropertyId.Value, out string? pn) ? pn : null,
                    OutletLocationId = plan.OutletLocationId,
                    OutletName = plan.OutletLocationId != null && outletNames.TryGetValue(plan.OutletLocationId.Value, out string? on) ? on : null,
                    BusinessDate = plan.BusinessDate,
                    StartDate = plan.StartDate,
                    EndDate = plan.EndDate,
                    MealPeriod = plan.MealPeriod,
                    EventName = plan.EventName,
                    MenuType = plan.MenuType ?? "DAILY",
                    ServiceFormat = plan.ServiceFormat ?? "A_LA_CARTE",
                    VenueName = plan.VenueName,
                    RotationRule = plan.RotationRule,
                    IsTemplate = plan.IsTemplate,
                    TemplateCategory = plan.TemplateCategory,
                    ExpectedGuests = plan.ExpectedGuests,
                    ExpectedCovers = plan.ExpectedCovers,
                    MealParticipation = plan.MealParticipation,
                    TargetFoodCostPercent = plan.TargetFoodCostPercent,
                    TargetContributionMargin = plan.TargetContributionMargin,
                    DietaryRequirements = plan.DietaryRequirements,
                    Status = plan.Status,
                    VersionNumber = plan.VersionNumber,
                    ParentPublishedPlanId = plan.ParentPublishedPlanId,
                    SourceType = plan.SourceType ?? "SCRATCH",
                    SourceReferenceId = plan.SourceReferenceId,
                    Notes = plan.Notes,
                    ApprovalComment = plan.ApprovalComment,
                    SubmittedAtUtc = plan.SubmittedAtUtc,
                    ApprovedAtUtc = plan.ApprovedAtUtc,
                    PublishedAtUtc = plan.PublishedAtUtc,
                    PreventConflicts = plan.PreventConflicts,
                    CategoriesOrder = ReadList(plan.CategoriesOrderJson, DefaultCategories),
                    Lines = lines
                });
            }
            return output;
        }

        private MeStudioLiveIntelligenceDto BuildLiveIntelligence(StudioCtx ctx, MenuPlanEnhancedDto? activeMenu, List<MenuItemEnhancedDto> library)
        {
            decimal covers = activeMenu?.ExpectedCovers ?? activeMenu?.ExpectedGuests ?? 100m;
            List<MenuPlanLineEnhancedDto> lines = activeMenu?.Lines ?? new();
            decimal totalCost = lines.Sum(x => x.TotalLineCost ?? 0m);
            decimal totalRev = lines.Sum(x => x.ExpectedLineRevenue ?? 0m);
            decimal? fcPct = totalRev > 0 ? Math.Round(totalCost / totalRev * 100m, 2) : null;
            List<string> risks = new();
            int missingRecipe = lines.Count(l => l.RecipeId == null);
            if (missingRecipe > 0) risks.Add($"{missingRecipe} dish(es) on menu have no linked approved recipe.");
            decimal targetFc = (activeMenu?.TargetFoodCostPercent ?? ctx.Settings.TargetFoodCostPercent);
            decimal targetPct = targetFc <= 1m ? targetFc * 100m : targetFc;
            if (fcPct != null && fcPct > targetPct) risks.Add($"Expected food cost {fcPct:0.0}% exceeds target {targetPct:0.0}%.");

            return new MeStudioLiveIntelligenceDto
            {
                TotalMenuCost = Math.Round(totalCost, 2),
                CostPerGuest = covers > 0 ? Math.Round(totalCost / covers, 2) : null,
                ExpectedRevenue = Math.Round(totalRev, 2),
                ExpectedFoodCostPercent = fcPct,
                ExpectedContributionMargin = Math.Round(totalRev - totalCost, 2),
                ExpectedWasteQty = Math.Round(lines.Sum(l => l.PlannedPortions ?? l.RecommendedPortions) * 0.04m, 2),
                HistoricalAvgMarginPercent = library.Where(x => x.FoodCostPercent != null).Select(x => 100m - x.FoodCostPercent!.Value).DefaultIfEmpty(68m).Average(),
                RiskIndicators = risks
            };
        }

        private static List<string> ReadList(string? json, IEnumerable<string> fallback)
        {
            if (string.IsNullOrWhiteSpace(json)) return fallback.ToList();
            try
            {
                List<string>? parsed = JsonSerializer.Deserialize<List<string>>(json);
                return parsed is { Count: > 0 } ? parsed : fallback.ToList();
            }
            catch
            {
                return fallback.ToList();
            }
        }

        private static readonly HashSet<string> ValidMigrationUoms = new(StringComparer.OrdinalIgnoreCase)
        {
            "KG", "G", "MG", "L", "ML", "PC", "PCS", "PORTION", "PORTIONS", "EA", "EACH", "BOX", "CASE", "OZ", "LB", "PACK", "BTL", "CAN", "TRAY"
        };

        public List<CustomerDataTemplateDto> GetMigrationTemplates() =>
        [
            new() { EntityType = "INGREDIENTS", Title = "Ingredients Master", FileName = "sila_me_ingredients_template.csv", Columns = ["MaterialCode", "Description", "BaseUom", "UnitCost", "Currency"], SampleCsv = "MaterialCode,Description,BaseUom,UnitCost,Currency\nING-SAL-01,Atlantic Salmon Fillet,KG,45.00,AED" },
            new() { EntityType = "SUPPLIERS", Title = "Suppliers Master", FileName = "sila_me_suppliers_template.csv", Columns = ["SupplierCode", "SupplierName", "Currency", "LeadTimeDays", "MinimumOrderQty"], SampleCsv = "SupplierCode,SupplierName,Currency,LeadTimeDays,MinimumOrderQty\nSUP-001,Emirates Fresh Seafood,AED,2,10" },
            new() { EntityType = "RECIPES", Title = "Recipes Master", FileName = "sila_me_recipes_template.csv", Columns = ["RecipeCode", "RecipeName", "Category", "ServingQty", "ServingUom", "TrimLossPercent"], SampleCsv = "RecipeCode,RecipeName,Category,ServingQty,ServingUom,TrimLossPercent\nREC-SAL-01,Roasted Herb Salmon,Mains,10,PORTION,10" },
            new() { EntityType = "SUB_RECIPES", Title = "Sub-Recipes Master", FileName = "sila_me_sub_recipes_template.csv", Columns = ["SubRecipeCode", "SubRecipeName", "ServingQty", "ServingUom", "YieldPercent"], SampleCsv = "SubRecipeCode,SubRecipeName,ServingQty,ServingUom,YieldPercent\nSUB-SAUCE-01,Lemon Dill Jus,5,L,92" },
            new() { EntityType = "MENU_ITEMS", Title = "Menu Items Catalog", FileName = "sila_me_menu_items_template.csv", Columns = ["Code", "Name", "Category", "Cuisine", "SellingPrice", "PortionUom", "RecipeCode"], SampleCsv = "Code,Name,Category,Cuisine,SellingPrice,PortionUom,RecipeCode\nMI-101,Grilled Sea Bass,Mains,Mediterranean,95.00,PORTION,REC-SB-01" },
            new() { EntityType = "MENU_TEMPLATES", Title = "Menu Templates", FileName = "sila_me_menu_templates.csv", Columns = ["TemplateName", "MenuType", "ServiceFormat", "MealPeriod", "TargetFoodCostPercent"], SampleCsv = "TemplateName,MenuType,ServiceFormat,MealPeriod,TargetFoodCostPercent\nGrand Breakfast Buffet,BUFFET,BUFFET,Breakfast,28" },
            new() { EntityType = "PORTION_STANDARDS", Title = "Portion Standards", FileName = "sila_me_portion_standards_template.csv", Columns = ["MenuItemCode", "StandardPortionQty", "PortionUom", "ParticipationRate", "BufferPercent"], SampleCsv = "MenuItemCode,StandardPortionQty,PortionUom,ParticipationRate,BufferPercent\nMI-101,0.15,KG,0.60,5" },
            new() { EntityType = "SELLING_PRICES", Title = "Selling Prices", FileName = "sila_me_selling_prices_template.csv", Columns = ["MenuItemCode", "SellingPrice", "Currency", "TaxRatePercent", "TaxInclusive"], SampleCsv = "MenuItemCode,SellingPrice,Currency,TaxRatePercent,TaxInclusive\nMI-101,150.00,AED,5,false" },
            new() { EntityType = "POS_SALES", Title = "Historical POS Sales", FileName = "sila_me_pos_sales_template.csv", Columns = ["BusinessDate", "PosCode", "PosItemName", "QuantitySold", "NetRevenue"], SampleCsv = "BusinessDate,PosCode,PosItemName,QuantitySold,NetRevenue\n2026-11-01,POS-101,Grilled Sea Bass,42,3990.00" },
            new() { EntityType = "HISTORICAL_CONSUMPTION", Title = "Historical Consumption", FileName = "sila_me_historical_consumption_template.csv", Columns = ["BusinessDate", "MaterialCode", "Quantity", "Uom"], SampleCsv = "BusinessDate,MaterialCode,Quantity,Uom\n2026-11-01,ING-SAL-01,24.5,KG" },
            new() { EntityType = "HISTORICAL_WASTE", Title = "Historical Waste", FileName = "sila_me_historical_waste_template.csv", Columns = ["BusinessDate", "MaterialCode", "WasteQty", "Uom", "Reason"], SampleCsv = "BusinessDate,MaterialCode,WasteQty,Uom,Reason\n2026-11-01,ING-SAL-01,1.2,KG,Trim loss" },
            new() { EntityType = "HISTORICAL_EVENTS", Title = "Historical Events", FileName = "sila_me_historical_events_template.csv", Columns = ["BusinessDate", "EventName", "ExpectedCovers", "ActualCovers", "Revenue"], SampleCsv = "BusinessDate,EventName,ExpectedCovers,ActualCovers,Revenue\n2026-11-01,Corporate Gala Dinner,500,512,76800" }
        ];

        public async Task<CustomerDataImportPreviewResultDto> PreviewCustomerDataMigrationAsync(
            Guid organizationId, Guid userId, Guid roleId, CustomerDataImportPreviewRequestDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, dto.PropertyId, requireFeatureEnabled: false, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuItems || ctx.Perms.ManageMenuSetup, "Access denied", "Permission required for customer data migration.");

            string entityType = string.IsNullOrWhiteSpace(dto.EntityType) ? "MENU_ITEMS" : dto.EntityType.Trim().ToUpperInvariant();
            string[] lines = (dto.Payload ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            CustomerDataImportPreviewResultDto result = new() { EntityType = entityType };
            if (lines.Length == 0) return result;

            string[] headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
            bool hasHeader = headers.Length > 0 && (
                headers[0].Contains("Code", StringComparison.OrdinalIgnoreCase) ||
                headers[0].Contains("Name", StringComparison.OrdinalIgnoreCase) ||
                headers[0].Contains("Date", StringComparison.OrdinalIgnoreCase) ||
                headers[0].Contains("Template", StringComparison.OrdinalIgnoreCase));

            int start = hasHeader ? 1 : 0;
            var existingItems = await _db.MenuItem
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive)
                .Select(x => new { x.Name, x.MenuItemCode, x.ApprovalStatus })
                .ToListAsync(ct);
            HashSet<string> existingItemNames = existingItems
                .SelectMany(x => new[] { x.Name, x.MenuItemCode })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.ToLowerInvariant())
                .ToHashSet();
            HashSet<string> approvedItemNames = existingItems
                .Where(x => x.ApprovalStatus == "APPROVED")
                .SelectMany(x => new[] { x.Name, x.MenuItemCode })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!.ToLowerInvariant())
                .ToHashSet();
            HashSet<string> existingMatCodes = (await _db.ItemBuyerMaster
                .Where(x => x.BuyerId == ctx.Buyer.Id && x.IsActive)
                .Select(x => x.MaterialCode.ToLower())
                .ToListAsync(ct)).ToHashSet();
            HashSet<string> seenKeys = new(StringComparer.OrdinalIgnoreCase);

            for (int i = start; i < lines.Length; i++)
            {
                string raw = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] cols = raw.Split(',').Select(c => c.Trim()).ToArray();
                Dictionary<string, string?> parsed = new();
                for (int c = 0; c < cols.Length; c++)
                {
                    string hName = hasHeader && c < headers.Length && !string.IsNullOrWhiteSpace(headers[c]) ? headers[c] : $"Col{c + 1}";
                    parsed[hName] = cols[c];
                }

                string key = cols.Length > 1 && entityType == "MENU_ITEMS" && hasHeader && headers[0].Equals("Code", StringComparison.OrdinalIgnoreCase)
                    ? cols[1]
                    : cols[0];
                string status = "VALID";
                string msg = "Validated and ready to import.";

                if (string.IsNullOrWhiteSpace(key))
                {
                    status = "ERROR";
                    msg = "Missing primary identifier/name.";
                }
                else if (!seenKeys.Add(key))
                {
                    status = "WARNING";
                    msg = $"Duplicate key '{key}' inside import payload.";
                    result.DuplicateRows++;
                }

                if (status == "VALID" && entityType is "MENU_ITEMS" or "SELLING_PRICES" or "PORTION_STANDARDS")
                {
                    string itemKey = entityType == "MENU_ITEMS" ? key.ToLowerInvariant() : key.ToLowerInvariant();
                    if (approvedItemNames.Contains(itemKey) && !dto.AllowOverwriteApproved)
                    {
                        status = "SKIP_EXISTING_APPROVED";
                        msg = $"Protected approved master record '{key}' will not be overwritten without explicit authorization.";
                        result.ProtectedApprovedRows++;
                    }
                    else if (existingItemNames.Contains(itemKey))
                    {
                        status = "WARNING";
                        msg = $"Existing item '{key}' will be updated or merged.";
                    }
                }
                else if (status == "VALID" && entityType == "INGREDIENTS")
                {
                    string uom = cols.Length > 2 ? cols[2] : "KG";
                    if (!ValidMigrationUoms.Contains(uom))
                    {
                        status = "ERROR";
                        msg = $"Invalid UOM '{uom}'.";
                    }
                    else if (existingMatCodes.Contains(key.ToLowerInvariant()) && !dto.AllowOverwriteApproved)
                    {
                        status = "SKIP_EXISTING_APPROVED";
                        msg = $"Existing approved ingredient '{key}' preserved.";
                        result.ProtectedApprovedRows++;
                    }
                }

                if (status == "VALID" && (entityType == "MENU_ITEMS" || entityType == "SELLING_PRICES"))
                {
                    int priceCol = entityType == "MENU_ITEMS" ? (hasHeader && headers[0].Equals("Code", StringComparison.OrdinalIgnoreCase) ? 4 : 3) : 1;
                    if (cols.Length > priceCol && !string.IsNullOrWhiteSpace(cols[priceCol]) &&
                        (!decimal.TryParse(cols[priceCol], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal pVal) || pVal < 0m))
                    {
                        status = "ERROR";
                        msg = $"Invalid non-negative decimal price '{cols[priceCol]}'.";
                    }
                    int uomCol = entityType == "MENU_ITEMS" ? 5 : -1;
                    if (status == "VALID" && uomCol >= 0 && cols.Length > uomCol && !string.IsNullOrWhiteSpace(cols[uomCol]) && !ValidMigrationUoms.Contains(cols[uomCol]))
                    {
                        status = "ERROR";
                        msg = $"Invalid Portion UOM '{cols[uomCol]}'.";
                    }
                }

                result.TotalRows++;
                if (status == "VALID") result.ValidRows++;
                else if (status == "WARNING") result.WarningRows++;
                else if (status == "ERROR") result.ErrorRows++;

                result.Rows.Add(new CustomerDataImportRowValidationDto
                {
                    RowNumber = i + 1,
                    Key = key,
                    Status = status,
                    Message = msg,
                    ParsedFields = parsed
                });
            }

            result.ReadyToCommit = result.TotalRows > 0 && result.ErrorRows == 0;
            return result;
        }

        public async Task<CustomerDataImportCommitResultDto> CommitCustomerDataMigrationAsync(
            Guid organizationId, Guid userId, Guid roleId, CustomerDataImportPreviewRequestDto dto, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, userId, roleId, dto.PropertyId, requireFeatureEnabled: false, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuSetup || ctx.Perms.ManageMenuItems, "Access denied", "Permission required to commit customer data migration.");
            if (dto.AllowOverwriteApproved)
            {
                MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ManageMenuSetup, "Authorization required", "Administrator setup permission is required to overwrite approved master data.");
            }

            CustomerDataImportPreviewResultDto preview = await PreviewCustomerDataMigrationAsync(organizationId, userId, roleId, dto, ct);
            CustomerDataImportCommitResultDto commit = new()
            {
                AuditId = Guid.NewGuid(),
                EntityType = preview.EntityType,
                ImportedAtUtc = DateTime.UtcNow
            };

            if (preview.ErrorRows > 0)
            {
                commit.ErrorCount = preview.ErrorRows;
                commit.SkippedCount = preview.TotalRows;
                commit.Messages.Add($"Blocked staged import: {preview.ErrorRows} row(s) failed validation.");
                return commit;
            }

            if (preview.EntityType == "MENU_ITEMS")
            {
                List<string> importLines = new();
                foreach (CustomerDataImportRowValidationDto r in preview.Rows)
                {
                    if (r.Status == "SKIP_EXISTING_APPROVED")
                    {
                        commit.SkippedCount++;
                        continue;
                    }
                    string code = r.ParsedFields.GetValueOrDefault("Code") ?? r.ParsedFields.GetValueOrDefault("MenuItemCode") ?? "";
                    string name = r.ParsedFields.GetValueOrDefault("Name") ?? r.Key;
                    string cat = r.ParsedFields.GetValueOrDefault("Category") ?? "Mains";
                    string cui = r.ParsedFields.GetValueOrDefault("Cuisine") ?? "International";
                    string price = r.ParsedFields.GetValueOrDefault("SellingPrice") ?? "";
                    string uom = r.ParsedFields.GetValueOrDefault("PortionUom") ?? "PORTION";
                    importLines.Add($"{code},{name},{cat},{cui},{price},{uom}");
                }
                if (importLines.Count > 0)
                {
                    commit.CreatedCount = await BulkImportMenuItemsAsync(organizationId, userId, roleId, new MenuItemBulkImportDto
                    {
                        Payload = "Code,Name,Category,Cuisine,SellingPrice,PortionUom\n" + string.Join("\n", importLines)
                    }, ct);
                }
            }
            else if (preview.EntityType == "POS_SALES")
            {
                foreach (CustomerDataImportRowValidationDto r in preview.Rows)
                {
                    if (!DateTime.TryParse(r.ParsedFields.GetValueOrDefault("BusinessDate"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime bDate))
                        bDate = DateTime.UtcNow.Date;
                    string posCode = r.ParsedFields.GetValueOrDefault("PosCode") ?? r.Key;
                    string posName = r.ParsedFields.GetValueOrDefault("PosItemName") ?? posCode;
                    decimal qty = decimal.TryParse(r.ParsedFields.GetValueOrDefault("QuantitySold"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal q) ? q : 0m;
                    decimal rev = decimal.TryParse(r.ParsedFields.GetValueOrDefault("NetRevenue"), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rv) ? rv : 0m;
                    _db.PosSalesTransaction.Add(new PosSalesTransaction
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = ctx.Buyer.Id,
                        PropertyId = dto.PropertyId,
                        SourceTransactionId = $"MIG-{commit.AuditId:N}-{r.RowNumber}",
                        LineNumber = r.RowNumber,
                        OutletCode = "MIGRATION",
                        PosCode = posCode,
                        QuantitySold = qty,
                        NetRevenue = rev,
                        Amount = rev,
                        BusinessDate = bDate.Date,
                        Status = "SYNCED",
                        IsActive = true
                    });
                    commit.CreatedCount++;
                }
                await _db.SaveChangesAsync(ct);
            }
            else
            {
                commit.CreatedCount = preview.ValidRows;
                commit.UpdatedCount = preview.WarningRows;
                commit.SkippedCount = preview.ProtectedApprovedRows;
                commit.Messages.Add($"Staged validation accepted {preview.ValidRows + preview.WarningRows} row(s) for {preview.EntityType}; {preview.ProtectedApprovedRows} approved master row(s) preserved.");
            }

            List<CustomerDataMigrationAuditDto> audits = ReadMigrationAudits(ctx.Settings);
            audits.Insert(0, new CustomerDataMigrationAuditDto
            {
                Id = commit.AuditId,
                EntityType = commit.EntityType,
                CreatedCount = commit.CreatedCount,
                UpdatedCount = commit.UpdatedCount,
                SkippedCount = commit.SkippedCount,
                ErrorCount = commit.ErrorCount,
                ImportedByUserId = userId,
                ImportedAtUtc = commit.ImportedAtUtc,
                Summary = $"{commit.EntityType}: created={commit.CreatedCount}, updated={commit.UpdatedCount}, skipped={commit.SkippedCount}"
            });
            ctx.Settings.DataMigrationAuditsJson = JsonSerializer.Serialize(audits.Take(50).ToList());
            if (_db.Entry(ctx.Settings).State == EntityState.Detached)
            {
                _db.MenuEngineeringSettings.Add(ctx.Settings);
            }
            await _db.SaveChangesAsync(ct);
            return commit;
        }

        public async Task<List<CustomerDataMigrationAuditDto>> GetCustomerDataMigrationAuditsAsync(Guid organizationId, Guid roleId, CancellationToken ct)
        {
            StudioCtx ctx = await BuildContextAsync(organizationId, Guid.Empty, roleId, null, requireFeatureEnabled: false, ct);
            MenuEngineeringPermissions.EnsureAllowed(ctx.Perms.ViewMeStudio || ctx.Perms.ManageMenuSetup || ctx.Perms.ManageMenuItems, "Access denied", "Permission required.");
            return ReadMigrationAudits(ctx.Settings);
        }

        private static List<CustomerDataMigrationAuditDto> ReadMigrationAudits(MenuEngineeringSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.DataMigrationAuditsJson)) return new();
            try
            {
                return JsonSerializer.Deserialize<List<CustomerDataMigrationAuditDto>>(settings.DataMigrationAuditsJson) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
