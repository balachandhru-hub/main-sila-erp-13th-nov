using System.Net;
using Buyer.Application.Features.Shared;
using Buyer.Application.Features.Shared.MenuEngineering;
using Buyer.Application.Features.Shared.MenuEngineering.Ai;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaMenuEngineeringAiNativeTests
    {
        private sealed class FakeProtector : IIntegrationCredentialProtector
        {
            public string? Protect(string? value) => value == null ? null : "ENC:" + value;
            public string? Unprotect(string? value) => value != null && value.StartsWith("ENC:") ? value[4..] : value;
            public string State(SharedKernel.Integration.Entities.ApiIntegrationConfiguration configuration) => "Configured";
        }

        private sealed class OfflineHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        private sealed class OfflineHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(new OfflineHttpMessageHandler(), disposeHandler: false);
        }

        private sealed class AiNativeFixture
        {
            public TestDatabase Db { get; init; } = null!;
            public MenuEngineeringStudioService Studio { get; init; } = null!;
            public MenuEngineeringAiOrchestratorService Ai { get; init; } = null!;
            public SilaCulinaryFinancialService Financial { get; init; } = null!;
            public SilaCulinaryAnalyticsService Analytics { get; init; } = null!;
            public IHttpClientFactory HttpFactory { get; init; } = null!;
            public Guid OrgId { get; init; }
            public Guid UserId { get; init; }
            public Guid AdminRoleId { get; init; }
            public BuyerBusinessProfile Buyer { get; init; } = null!;
            public BuyerProperty Property { get; init; } = null!;
            public InventoryLocation Kitchen { get; init; } = null!;
            public ItemBuyerMaster Salmon { get; init; } = null!;
            public ItemBuyerMaster Chicken { get; init; } = null!;
            public ItemBuyerMaster Mango { get; init; } = null!;
            public MenuItem SalmonItem { get; init; } = null!;
            public MenuItem ChickenItem { get; init; } = null!;
            public MenuItem DessertItem { get; init; } = null!;
            public MenuItem SaladItem { get; init; } = null!;
            public MenuPlan ActivePlan { get; init; } = null!;
        }

        private static AiNativeFixture CreateFixture()
        {
            TestDatabase db = new();
            Guid orgId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid adminRoleId = Common.BUYER_ADMIN_ROLE_ID;

            BuyerBusinessProfile buyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                OrganizationName = "SILA AI Hospitality",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "ai@sila.test",
                Phone = "+971500000099",
                Country = "UAE",
                AddressLine1 = "Downtown",
                City = "Dubai",
                State = "Dubai",
                PinCode = "00000",
                Industry = "Hospitality",
                BusinessType = "Hotel",
                Currency = "AED",
                Status = "ACTIVE",
                IsActive = true
            };
            BuyerProperty property = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                CompanyCode = "1000",
                PlantCode = "1060",
                PropertyName = "SILA Grand Tower",
                IsActive = true
            };
            InventoryLocation kitchen = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                LocationCode = "KIT-01",
                LocationName = "Main Kitchen",
                LocationType = "KITCHEN",
                InventoryEnabled = true,
                ConsumptionEnabled = true,
                IsActive = true
            };

            ItemBuyerMaster salmon = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-SALMON",
                Description = "Atlantic Salmon Fillet",
                MaterialGroup = "Seafood",
                BaseUnitOfMeasure = "KG",
                OrderUnitOfMeasure = "KG",
                UnitCost = 60m,
                IsInventoryItem = true,
                IsActive = true
            };
            ItemBuyerMaster chicken = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-CHICK",
                Description = "Chicken Breast Boneless",
                MaterialGroup = "Poultry",
                BaseUnitOfMeasure = "KG",
                OrderUnitOfMeasure = "KG",
                UnitCost = 20m,
                IsInventoryItem = true,
                IsActive = true
            };
            ItemBuyerMaster mango = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-MANGO",
                Description = "Fresh Mango Puree",
                MaterialGroup = "Produce",
                BaseUnitOfMeasure = "KG",
                OrderUnitOfMeasure = "KG",
                UnitCost = 12m,
                IsInventoryItem = true,
                IsActive = true
            };

            InventoryBalance stockSalmon = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = salmon.Id, OnHandQty = 10m, BaseUom = "KG", UnitCost = 60m, IsActive = true };
            InventoryBalance stockChicken = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = chicken.Id, OnHandQty = 120m, BaseUom = "KG", UnitCost = 20m, IsActive = true };
            InventoryBalance stockMango = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = mango.Id, OnHandQty = 40m, BaseUom = "KG", UnitCost = 12m, IsActive = true };

            Recipe rSalmon = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-SAL", Name = "Roasted Salmon", Category = "Mains", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 15m, Currency = "AED", IsActive = true };
            RecipeIngredient riSalmon = new() { Id = Guid.NewGuid(), RecipeId = rSalmon.Id, IngredientCode = "ING-SAL", MaterialId = salmon.Id, ItemCode = "MAT-SALMON", ItemName = "Atlantic Salmon Fillet", Quantity = 0.25m, Uom = "KG", BaseQuantity = 0.25m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rChicken = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-CHK", Name = "Grilled Chicken", Category = "Mains", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 5m, Currency = "AED", IsActive = true };
            RecipeIngredient riChicken = new() { Id = Guid.NewGuid(), RecipeId = rChicken.Id, IngredientCode = "ING-CHK", MaterialId = chicken.Id, ItemCode = "MAT-CHICK", ItemName = "Chicken Breast Boneless", Quantity = 0.25m, Uom = "KG", BaseQuantity = 0.25m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rDessert = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-MNG", Name = "Mango Panna Cotta", Category = "Desserts", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 2.4m, Currency = "AED", IsActive = true };
            RecipeIngredient riDessert = new() { Id = Guid.NewGuid(), RecipeId = rDessert.Id, IngredientCode = "ING-MNG", MaterialId = mango.Id, ItemCode = "MAT-MANGO", ItemName = "Fresh Mango Puree", Quantity = 0.20m, Uom = "KG", BaseQuantity = 0.20m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rSalad = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-SLD", Name = "Garden Herb Salad", Category = "Appetizers", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 1.8m, Currency = "AED", IsActive = true };
            RecipeIngredient riSalad = new() { Id = Guid.NewGuid(), RecipeId = rSalad.Id, IngredientCode = "ING-SLD", MaterialId = mango.Id, ItemCode = "MAT-MANGO", ItemName = "Fresh Mango Puree", Quantity = 0.15m, Uom = "KG", BaseQuantity = 0.15m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            MenuItem salmonItem = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "ME-SALMON", Name = "Grilled Salmon", Category = "Mains", Cuisine = "International", MealPeriods = "Dinner", LinkedRecipeId = rSalmon.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 40m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, IsActive = true };
            MenuItem chickenItem = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "ME-CHICK", Name = "Grilled Chicken", Category = "Mains", Cuisine = "International", MealPeriods = "Dinner", LinkedRecipeId = rChicken.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 35m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, IsActive = true };
            MenuItem dessertItem = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "ME-DESSERT", Name = "Mango Panna Cotta", Category = "Desserts", Cuisine = "International", DietaryTags = "Vegetarian", MealPeriods = "Dinner", LinkedRecipeId = rDessert.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 22m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, IsActive = true };
            MenuItem saladItem = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "ME-SALAD", Name = "Garden Mango Salad", Category = "Appetizers", Cuisine = "International", DietaryTags = "Vegan,Vegetarian", MealPeriods = "Dinner", LinkedRecipeId = rSalad.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 20m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, IsActive = true };

            MenuPlan activePlan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                Name = "Grand Dinner Buffet",
                MenuType = "BUFFET",
                ServiceFormat = "BUFFET",
                MealPeriod = "Dinner",
                BusinessDate = DateTime.UtcNow.Date.AddDays(1),
                ExpectedGuests = 500,
                ExpectedCovers = 500,
                TargetFoodCostPercent = 30m,
                Status = "PUBLISHED",
                VersionNumber = 1,
                IsActive = true
            };
            MenuPlanLine lineSal = new() { Id = Guid.NewGuid(), MenuPlanId = activePlan.Id, MenuItemId = salmonItem.Id, CategoryName = "Mains", RecipeId = rSalmon.Id, PortionSize = 1m, PortionUom = "PORTION", PlannedPortions = 200, ParticipationRate = 0.40m, ExpectedServingsPerGuest = 1m, SellingPrice = 40m, SampledActualPortion = 1.25m, IsActive = true };
            MenuPlanLine lineChk = new() { Id = Guid.NewGuid(), MenuPlanId = activePlan.Id, MenuItemId = chickenItem.Id, CategoryName = "Mains", RecipeId = rChicken.Id, PortionSize = 1m, PortionUom = "PORTION", PlannedPortions = 300, ParticipationRate = 0.60m, ExpectedServingsPerGuest = 1m, SellingPrice = 35m, SampledActualPortion = 0.98m, IsActive = true };

            // Historical POS sales for demand backtesting & anomaly detection
            List<object> seedItems =
            [
                buyer, property, kitchen, salmon, chicken, mango,
                stockSalmon, stockChicken, stockMango,
                rSalmon, riSalmon, rChicken, riChicken, rDessert, riDessert, rSalad, riSalad,
                salmonItem, chickenItem, dessertItem, saladItem,
                activePlan, lineSal, lineChk,
                new MenuEngineeringSettings
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MenuEngineeringEnabled = true,
                    MeStudioEnabled = true,
                    DefaultOperatingMode = "AI_ASSISTED",
                    HighFoodCostPercent = 0.30m,
                    PopularityThreshold = 0.70m,
                    DefaultCurrency = "AED",
                    ProactiveAlertsEnabled = true,
                    ProactiveCooldownMinutes = 120,
                    IsActive = true
                },
                new SilaCulinaryWasteEvent
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    KitchenId = kitchen.Id,
                    MaterialId = salmon.Id,
                    WasteCode = "WST-SAL-01",
                    ItemCode = "MAT-SALMON",
                    ItemName = "Atlantic Salmon Fillet",
                    WasteQty = 8.5m,
                    Uom = "KG",
                    EstimatedWasteCost = 510m,
                    WasteReason = "Over-portioning buffet tray waste",
                    DateCreated = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    OutletCode = "MAIN-DINING",
                    SourceTransactionId = "POS-UNMAP-1",
                    PosCode = "CHKN GRL LRG + EX SAUCE",
                    BusinessDate = DateTime.UtcNow.Date.AddDays(-1),
                    QuantitySold = 45m,
                    Amount = 1890m,
                    GrossSellingPrice = 42m,
                    NetRevenue = 1890m,
                    Status = "UNMAPPED",
                    IsActive = true
                }
            ];

            for (int d = 28; d >= 1; d--)
            {
                DateTime bDate = DateTime.UtcNow.Date.AddDays(-d);
                seedItems.Add(new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    OutletCode = "MAIN-DINING",
                    SourceTransactionId = $"POS-HIST-{d}",
                    PosCode = "ME-CHICK",
                    MenuItemId = chickenItem.Id,
                    RecipeId = rChicken.Id,
                    BusinessDate = bDate,
                    QuantitySold = 100 + (d % 7) * 12,
                    Amount = (100 + (d % 7) * 12) * 35m,
                    GrossSellingPrice = 35m,
                    NetRevenue = (100 + (d % 7) * 12) * 35m,
                    Status = "MAPPED",
                    IsActive = true
                });
                seedItems.Add(new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    OutletCode = "MAIN-DINING",
                    SourceTransactionId = $"POS-SAL-{d}",
                    PosCode = "ME-SALMON",
                    MenuItemId = salmonItem.Id,
                    RecipeId = rSalmon.Id,
                    BusinessDate = bDate,
                    QuantitySold = d == 1 ? 10 : 60,
                    Amount = (d == 1 ? 10 : 60) * 40m,
                    GrossSellingPrice = 40m,
                    NetRevenue = (d == 1 ? 10 : 60) * 40m,
                    Status = "MAPPED",
                    IsActive = true
                });
            }

            db.Seed(seedItems.ToArray());

            IConfiguration cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["InterCallService:CulinaryEngineUrl"] = "http://127.0.0.1:8013" })
                .Build();
            IHttpClientFactory httpFactory = new OfflineHttpClientFactory();
            SilaCulinaryFinancialService fin = new(db.Repository, db.Logger, httpFactory, cfg, new FakeProtector());
            SilaCulinaryAnalyticsService ana = new(db.Repository, db.Logger, httpFactory, cfg);
            MenuEngineeringStudioService studio = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), fin, ana, httpFactory);
            MenuEngineeringAiOrchestratorService ai = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), studio, fin, ana, httpFactory);

            return new AiNativeFixture
            {
                Db = db,
                Studio = studio,
                Ai = ai,
                Financial = fin,
                Analytics = ana,
                HttpFactory = httpFactory,
                OrgId = orgId,
                UserId = userId,
                AdminRoleId = adminRoleId,
                Buyer = buyer,
                Property = property,
                Kitchen = kitchen,
                Salmon = salmon,
                Chicken = chicken,
                Mango = mango,
                SalmonItem = salmonItem,
                ChickenItem = chickenItem,
                DessertItem = dessertItem,
                SaladItem = saladItem,
                ActivePlan = activePlan
            };
        }

        [Fact]
        public async Task Test01_GenerateCompleteBuffetFromNaturalLanguage_NeverAutoPublishes()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeStudioMultiAgentRunResultDto res = await f.Ai.RunMultiAgentWorkflowAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MeStudioMultiAgentCommandRequestDto
                {
                    Objective = "Create an international dinner buffet for 500 guests with AED 150 selling price, maximum 30% food cost, vegetarian options, three desserts and maximum utilization of available inventory.",
                    OperatingMode = "AGENTIC",
                    OptimizationMode = "BALANCED",
                    PropertyId = f.Property.Id,
                    SaveSelectedProposalAsDraft = true
                }, CancellationToken.None);

            Assert.Equal("COMPLETED", res.Status);
            Assert.Equal(500m, res.ExpectedCovers);
            Assert.Equal(30m, res.MaxFoodCostPercent);
            Assert.False(res.AutoPublished);
            Assert.True(res.RequiresApprovalToPublish);
            Assert.NotNull(res.CreatedDraftMenuId);
            Assert.Equal(15, res.Steps.Count);

            MenuPlan? draft = await f.Db.Context.Set<MenuPlan>().FirstOrDefaultAsync(x => x.Id == res.CreatedDraftMenuId);
            Assert.NotNull(draft);
            Assert.Equal("DRAFT", draft!.Status);
        }

        [Fact]
        public async Task Test02_OptimizeMenuAgainst30PercentFoodCostTarget()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeStudioMultiAgentRunResultDto res = await f.Ai.RunMultiAgentWorkflowAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MeStudioMultiAgentCommandRequestDto
                {
                    Objective = "Optimize dinner menu with maximum 30% food cost for 300 guests at AED 120",
                    OperatingMode = "AGENTIC",
                    OptimizationMode = "BUDGET_OPTIMIZED",
                    PropertyId = f.Property.Id,
                    MaxFoodCostPercent = 30m,
                    ExpectedCovers = 300m,
                    SellingPriceTarget = 120m,
                    SaveSelectedProposalAsDraft = false
                }, CancellationToken.None);

            OptimizedScenarioOptionDto? budgetScenario = res.Scenarios.FirstOrDefault(s => s.ScenarioCode == "B" && s.Feasible);
            Assert.NotNull(budgetScenario);
            Assert.True(budgetScenario!.FoodCostPercent <= 30m);
        }

        [Fact]
        public async Task Test03_GenerateWeeklyMenuUsingEligibleAvailableInventory()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiMenuCalendarGenerateResultDto cal = await f.Ai.GenerateMenuCalendarAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new AiMenuCalendarGenerateRequestDto
                {
                    Horizon = "WEEKLY",
                    StartDate = DateTime.UtcNow.Date,
                    PropertyId = f.Property.Id,
                    MealPeriod = "Dinner",
                    ExpectedCoversPerDay = 200m,
                    PrioritizeAvailableInventory = true,
                    CreateDraftPlans = false
                }, CancellationToken.None);

            Assert.Equal(7, cal.Days.Count);
            Assert.True(cal.RequiresUserApproval);
            Assert.All(cal.Days, d => Assert.NotEmpty(d.RecommendedDishNames));
        }

        [Fact]
        public async Task Test04_PredictDemandUsingValidatedHistoricalSales_WithBacktestMetrics()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiBacktestDemandResultDto forecast = await f.Ai.GetBacktestedDemandForecastAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id },
                CancellationToken.None);

            Assert.NotEmpty(forecast.SelectedModel);
            Assert.NotEmpty(forecast.ModelEvaluations);
            Assert.Contains(forecast.ModelEvaluations, m => m.Selected);
            Assert.NotEmpty(forecast.HorizonDays);
            Assert.All(forecast.HorizonDays, d => Assert.True(d.UpperBound >= d.LowerBound));
        }

        [Fact]
        public async Task Test05_RecommendPortionSizesUsingActualConsumptionHistory_PreservesMasterRecipe()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiPortionOptimizeResultDto result = await f.Ai.OptimizePortionsAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                f.ActivePlan.Id, f.Property.Id, CancellationToken.None);

            Assert.NotEmpty(result.Rows);
            Assert.Contains(result.Rows, r => r.VariancePattern == "OVER_PORTIONING");
            Assert.Contains("Approved master recipe portions remain unchanged", result.SafetyGuardNotice);
        }

        [Fact]
        public async Task Test06_SuggestPosMappingsForUnmappedItems_RequiresHumanReviewWhenUncertain()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiPosMappingSuggestionBatchDto batch = await f.Ai.SuggestPosMappingsAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id },
                CancellationToken.None);

            Assert.NotEmpty(batch.Queue);
            AiPosMappingCandidateRowDto? row = batch.Queue.FirstOrDefault(q => q.PosCode.Contains("CHKN", StringComparison.OrdinalIgnoreCase));
            Assert.NotNull(row);
            Assert.Equal("Grilled Chicken", row!.CandidateMenuItemName);
            Assert.True(row.SuggestedPortionMultiplier >= 1.25m);
            Assert.False(string.IsNullOrWhiteSpace(row.SuggestedModifier));
        }

        [Fact]
        public async Task Test07_DetectAbnormalIngredientConsumption_NeverAutoAdjustsInventory()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            decimal beforeQty = (await f.Db.Context.Set<InventoryBalance>().FirstAsync(b => b.MaterialId == f.Salmon.Id)).OnHandQty;

            AiConsumptionRootCauseDto causes = await f.Ai.GetConsumptionRootCausesAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id },
                CancellationToken.None);

            decimal afterQty = (await f.Db.Context.Set<InventoryBalance>().FirstAsync(b => b.MaterialId == f.Salmon.Id)).OnHandQty;

            Assert.Equal(beforeQty, afterQty);
            Assert.Contains("never auto-adjusted", causes.LedgerGuardNotice, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(causes.Anomalies);
        }

        [Fact]
        public async Task Test08_IdentifyAllMenusAffectedByIngredientPriceIncrease()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiPriceShockResultDto shock = await f.Ai.EvaluateIngredientPriceShockAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new AiPriceShockRequestDto
                {
                    IngredientQuery = "Salmon",
                    PriceChangePercent = 20m,
                    PropertyId = f.Property.Id
                }, CancellationToken.None);

            Assert.Contains("Salmon", shock.IngredientName, StringComparison.OrdinalIgnoreCase);
            Assert.NotEmpty(shock.AffectedRecipes);
            Assert.NotEmpty(shock.AffectedMenuItems);
            Assert.NotEmpty(shock.AffectedMenus);
            Assert.True(shock.TotalProjectedCostDelta > 0m);
        }

        [Fact]
        public async Task Test09_GeneratePricingRecommendations_DisclosesElasticityAssumption()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            decimal? beforePrice = f.SalmonItem.SellingPrice;
            AiPricingRecommendationsResultDto pricing = await f.Ai.GeneratePricingRecommendationsAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id },
                CancellationToken.None);

            decimal? afterPrice = (await f.Db.Context.Set<MenuItem>().FirstAsync(x => x.Id == f.SalmonItem.Id)).SellingPrice;
            Assert.Equal(beforePrice, afterPrice);
            Assert.NotEmpty(pricing.Items);
            Assert.Contains("held constant", pricing.ElasticityNotice, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Test10_GeneratePurchaseRecommendationsUsingTimePhasedAvailability()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuPurchasePlanDto plan = await f.Studio.GetPurchasePlanAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id },
                CancellationToken.None);

            Assert.NotEmpty(plan.Rows);
            Assert.Contains(plan.Rows, r => r.MaterialId == f.Salmon.Id && r.Shortage > 0m);
        }

        [Fact]
        public async Task Test11_CompareMultipleOptimizedScenarios_A_Through_G()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeStudioMultiAgentRunResultDto res = await f.Ai.RunMultiAgentWorkflowAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MeStudioMultiAgentCommandRequestDto
                {
                    Objective = "Compare scenarios for 400 dinner guests at AED 140 max 32% food cost",
                    OperatingMode = "AGENTIC",
                    PropertyId = f.Property.Id,
                    SaveSelectedProposalAsDraft = false
                }, CancellationToken.None);

            Assert.Equal(7, res.Scenarios.Count);
            Assert.Equal(new[] { "A", "B", "C", "D", "E", "F", "G" }, res.Scenarios.Select(s => s.ScenarioCode).ToArray());
        }

        [Fact]
        public async Task Test12_GenerateProactiveMarginRiskAlert_WithCooldownDeduplication()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            List<MeProactiveAlertDto> firstScan = await f.Ai.RunProactiveScanAsync(
                f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id, CancellationToken.None);
            Assert.NotEmpty(firstScan);

            int countAfterFirst = await f.Db.Context.Set<MenuEngineeringProactiveAlert>().CountAsync();

            List<MeProactiveAlertDto> secondScan = await f.Ai.RunProactiveScanAsync(
                f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id, CancellationToken.None);
            int countAfterSecond = await f.Db.Context.Set<MenuEngineeringProactiveAlert>().CountAsync();

            Assert.Equal(countAfterFirst, countAfterSecond);
        }

        [Fact]
        public async Task Test13_CaptureAcceptedRecommendationsAndValidatedOutcomesForLearning()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringRecommendation rec = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                RecommendationCode = "REC-TEST-13",
                AgentId = "COST_MARGIN_AGENT",
                Category = "MARGIN_RISK",
                Title = "Rebalance Salmon Portion",
                CurrentBaseline = "FC 37.5%",
                ProposedChange = "Adjust garnish & price +5%",
                SupportingEvidence = "Historical margin variance",
                EstimatedMarginImpact = 1500m,
                Status = "NEW",
                DateCreated = DateTime.UtcNow,
                IsActive = true
            };
            f.Db.Context.Set<MenuEngineeringRecommendation>().Add(rec);
            await f.Db.Context.SaveChangesAsync();

            await f.Ai.TransitionCockpitRecommendationAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new MeRecommendationLifecycleActionDto { Action = "ACCEPT" }, CancellationToken.None);
            await f.Ai.TransitionCockpitRecommendationAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new MeRecommendationLifecycleActionDto { Action = "APPROVE" }, CancellationToken.None);
            await f.Ai.TransitionCockpitRecommendationAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new MeRecommendationLifecycleActionDto { Action = "IMPLEMENT" }, CancellationToken.None);
            var validated = await f.Ai.TransitionCockpitRecommendationAsync(
                f.OrgId, f.UserId, f.AdminRoleId, rec.Id,
                new MeRecommendationLifecycleActionDto
                {
                    Action = "VALIDATE_OUTCOME",
                    RealizedFinancialImpact = 1420m,
                    ActualFoodCostPercent = 29.4m,
                    ActualMargin = 1420m,
                    FeedbackRating = 5
                }, CancellationToken.None);

            Assert.Equal("OUTCOME_VALIDATED", validated.Status);
            Assert.Equal(1420m, validated.RealizedFinancialImpact);

            MeLearningSummaryDto summary = await f.Ai.GetLearningSummaryAsync(f.OrgId, f.AdminRoleId, CancellationToken.None);
            Assert.True(summary.TotalValidatedOutcomes >= 1);
        }

        [Fact]
        public async Task Test14_ExecuteMultiAgentWorkflowWithIdempotency_NoDuplicateDrafts()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeStudioMultiAgentCommandRequestDto req = new()
            {
                Objective = "Plan buffet for 250 guests at AED 120 max 30% food cost",
                IdempotencyKey = "idem-me2-test-14",
                OperatingMode = "AGENTIC",
                PropertyId = f.Property.Id,
                SaveSelectedProposalAsDraft = true
            };

            MeStudioMultiAgentRunResultDto first = await f.Ai.RunMultiAgentWorkflowAsync(f.OrgId, f.UserId, f.AdminRoleId, req, CancellationToken.None);
            MeStudioMultiAgentRunResultDto second = await f.Ai.RunMultiAgentWorkflowAsync(f.OrgId, f.UserId, f.AdminRoleId, req, CancellationToken.None);

            Assert.Equal(first.RunId, second.RunId);
            Assert.Equal(first.CreatedDraftMenuId, second.CreatedDraftMenuId);
            Assert.Equal(1, await f.Db.Context.Set<MenuEngineeringAgentRun>().CountAsync(r => r.IdempotencyKey == "idem-me2-test-14"));
        }

        [Fact]
        public async Task Test15_DisableAiProvider_ManualBusinessFunctionalityContinuesWorking()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            f.Db.Context.Set<SilaCulinaryAiConfiguration>().Add(new SilaCulinaryAiConfiguration
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                OperatingMode = "DISABLED",
                LocalEnabled = false,
                AllowExternalAi = false,
                IsActive = true
            });
            await f.Db.Context.SaveChangesAsync();

            AiOperatingModeStatusDto status = await f.Ai.GetOperatingModeStatusAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id, CancellationToken.None);
            Assert.False(status.AiProviderAvailable);
            Assert.Equal("DETERMINISTIC_OFFLINE", status.ProviderMode);

            MenuPlanEnhancedDto manualPlan = await f.Studio.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Manual Deterministic Dinner",
                MenuType = "A_LA_CARTE",
                ServiceFormat = "A_LA_CARTE",
                MealPeriod = "Dinner",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date,
                ExpectedCovers = 80,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonItem.Id, CategoryName = "Mains", ParticipationRate = 1m, BufferPercentage = 0m, PlannedPortions = 80m }]
            }, CancellationToken.None);

            Assert.Equal(15m, manualPlan.Lines[0].PortionCost);
            Assert.Equal(1200m, manualPlan.Lines[0].TotalLineCost);
        }

        [Fact]
        public async Task Test16_UnauthorizedFinancialDataAccessThroughAiAssistant_IsRedactedOrBlocked()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            // 1. Role without ME access is strictly blocked
            Guid unauthorizedRoleId = Guid.NewGuid();
            await Assert.ThrowsAsync<ForBiddenCustomException>(() => f.Ai.RunContextualAssistantAsync(
                f.OrgId, f.UserId, unauthorizedRoleId,
                new AiContextualRequestDto
                {
                    ScreenKey = "silaMenuCost",
                    Action = "AnalyzeThis",
                    UserPrompt = "Show exact food cost and contribution margin",
                    PropertyId = f.Property.Id,
                    MenuPlanId = f.ActivePlan.Id,
                    OperatingMode = "AI_ASSISTED"
                }, CancellationToken.None));

            // 2. Role with ME Studio View + AI permission, but WITHOUT Cost/Margin permission, gets financial redaction
            DefaultHttpContext httpCtx = new();
            httpCtx.Items["sila.permissions"] = new[] { "VIEW_SILA_ME_STUDIO", "USE_SILA_AI_MENU_INTELLIGENCE" };
            IHttpContextAccessor accessor = new HttpContextAccessor { HttpContext = httpCtx };
            var restrictedAi = new MenuEngineeringAiOrchestratorService(
                f.Db.Repository, f.Db.Context, f.Db.Logger, accessor, f.Studio, f.Financial, f.Analytics, f.HttpFactory);

            AiContextualResponseDto res = await restrictedAi.RunContextualAssistantAsync(
                f.OrgId, f.UserId, Common.OUTLET_MANAGER_ROLE_ID,
                new AiContextualRequestDto
                {
                    ScreenKey = "silaMeStudio",
                    Action = "AnalyzeThis",
                    UserPrompt = "Show exact food cost and contribution margin",
                    PropertyId = f.Property.Id,
                    MenuPlanId = f.ActivePlan.Id,
                    OperatingMode = "AI_ASSISTED"
                }, CancellationToken.None);

            Assert.True(res.FinancialRedacted);
            Assert.Contains("redacted", res.Summary + " " + string.Join(" ", res.Warnings), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Test17_CrossTenantDataAccess_IsBlocked()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeStudioMultiAgentRunResultDto run = await f.Ai.RunMultiAgentWorkflowAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MeStudioMultiAgentCommandRequestDto
                {
                    Objective = "Plan buffet for 200 guests",
                    PropertyId = f.Property.Id,
                    SaveSelectedProposalAsDraft = false
                }, CancellationToken.None);

            // Second tenant org
            Guid otherOrgId = Guid.NewGuid();
            BuyerBusinessProfile otherBuyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = otherOrgId,
                OrganizationName = "Competitor Hotel",
                SNID = "SN-COMP1",
                Email = "other@test.com",
                Phone = "+971500000111",
                Country = "UAE",
                AddressLine1 = "Marina",
                City = "Dubai",
                State = "Dubai",
                PinCode = "00000",
                Industry = "Hospitality",
                BusinessType = "Hotel",
                Currency = "AED",
                Status = "ACTIVE",
                IsActive = true
            };
            f.Db.Context.Set<BuyerBusinessProfile>().Add(otherBuyer);
            await f.Db.Context.SaveChangesAsync();

            await Assert.ThrowsAnyAsync<Exception>(() =>
                f.Ai.GetAgentRunAsync(otherOrgId, f.AdminRoleId, run.RunId, CancellationToken.None));
        }

        [Fact]
        public async Task Test18_SimulationsNeverModifyLiveData()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            decimal? origSellingPrice = f.SalmonItem.SellingPrice;
            decimal? origCovers = f.ActivePlan.ExpectedCovers;

            AiNlSimulationResultDto nlSim = await f.Ai.RunNaturalLanguageSimulationAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new AiNlSimulationRequestDto
                {
                    Command = "What happens if salmon cost increases by 20% and guest count increases to 650?",
                    MenuPlanId = f.ActivePlan.Id,
                    PropertyId = f.Property.Id,
                    SaveScenario = false
                }, CancellationToken.None);

            Assert.False(nlSim.LiveDataMutated);
            Assert.Equal(origSellingPrice, (await f.Db.Context.Set<MenuItem>().FirstAsync(x => x.Id == f.SalmonItem.Id)).SellingPrice);
            Assert.Equal(origCovers, (await f.Db.Context.Set<MenuPlan>().FirstAsync(x => x.Id == f.ActivePlan.Id)).ExpectedCovers);
        }

        [Fact]
        public async Task Test19_PreserveExistingProcurementV1_DraftRecommendationDoesNotTransmitPo()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            int poCountBefore = await f.Db.Context.Set<PurchaseOrder>().CountAsync();
            SilaPurchaseRecommendationListDto rec = await f.Studio.CreateDraftPurchaseRecommendationAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id },
                CancellationToken.None);
            int poCountAfter = await f.Db.Context.Set<PurchaseOrder>().CountAsync();

            Assert.Equal(poCountBefore, poCountAfter);
            Assert.NotNull(rec);
        }

        [Fact]
        public async Task Test20_CloudAndMobileRoleIntelligenceReadiness()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            foreach (string persona in new[] { "EXECUTIVE_CHEF", "STOREKEEPER", "COST_CONTROLLER", "FB_MANAGER" })
            {
                SilaStoreRoleIntelligenceDto roleIntel = await f.Ai.GetSilaStoreRoleIntelligenceAsync(
                    f.OrgId, f.UserId, f.AdminRoleId, persona, f.Property.Id, CancellationToken.None);

                Assert.Equal(persona, roleIntel.RolePersona);
                Assert.NotEmpty(roleIntel.Highlights);
                Assert.NotEmpty(roleIntel.QuickActions);
            }
        }
    }
}
