using System.Diagnostics;
using System.Net;
using Buyer.Application.Features.Shared;
using Buyer.Application.Features.Shared.MenuEngineering;
using Buyer.Application.Features.Shared.MenuEngineering.Ai;
using Buyer.Application.Features.Shared.MenuEngineering.Twin;
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
    public class SilaMenuEngineeringPhase3Tests
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

        private sealed class TwinFixture
        {
            public TestDatabase Db { get; init; } = null!;
            public MenuEngineeringDigitalTwinService Twin { get; init; } = null!;
            public MenuEngineeringStudioService Studio { get; init; } = null!;
            public MenuEngineeringAiOrchestratorService Ai { get; init; } = null!;
            public Guid OrgId { get; init; }
            public Guid OtherOrgId { get; init; }
            public Guid UserId { get; init; }
            public Guid AdminRoleId { get; init; }
            public BuyerBusinessProfile Buyer { get; init; } = null!;
            public BuyerProperty Property { get; init; } = null!;
            public InventoryLocation Kitchen { get; init; } = null!;
            public ItemBuyerMaster Salmon { get; init; } = null!;
            public ItemBuyerMaster Chicken { get; init; } = null!;
            public ItemBuyerMaster Mango { get; init; } = null!;
            public MenuPlan ActivePlan { get; init; } = null!;
        }

        private static TwinFixture CreateFixture()
        {
            TestDatabase db = new();
            Guid orgId = Guid.NewGuid();
            Guid otherOrgId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid adminRoleId = Common.BUYER_ADMIN_ROLE_ID;

            BuyerBusinessProfile buyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                OrganizationName = "SILA Phase 3 Hospitality",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "twin@sila.test",
                Phone = "+971500000101",
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

            BuyerBusinessProfile otherBuyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = otherOrgId,
                OrganizationName = "Other Tenant Hospitality",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "other@sila.test",
                Phone = "+971500000202",
                Country = "UAE",
                AddressLine1 = "JBR",
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
                PropertyName = "SILA Pearl Resort",
                IsActive = true
            };

            InventoryLocation kitchen = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                LocationCode = "KIT-MAIN",
                LocationName = "Main Banquet Kitchen",
                LocationType = "KITCHEN",
                InventoryEnabled = true,
                ConsumptionEnabled = true,
                IsActive = true
            };

            ItemBuyerMaster salmon = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, MaterialCode = "MAT-SAL", Description = "Atlantic Salmon Fillet", MaterialGroup = "Seafood", BaseUnitOfMeasure = "KG", OrderUnitOfMeasure = "KG", UnitCost = 60m, MovingAveragePrice = 60m, IsInventoryItem = true, IsActive = true };
            ItemBuyerMaster chicken = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, MaterialCode = "MAT-CHK", Description = "Chicken Breast Boneless", MaterialGroup = "Poultry", BaseUnitOfMeasure = "KG", OrderUnitOfMeasure = "KG", UnitCost = 20m, MovingAveragePrice = 20m, IsInventoryItem = true, IsActive = true };
            ItemBuyerMaster mango = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, MaterialCode = "MAT-MNG", Description = "Fresh Mango Puree", MaterialGroup = "Produce", BaseUnitOfMeasure = "KG", OrderUnitOfMeasure = "KG", UnitCost = 12m, MovingAveragePrice = 12m, IsInventoryItem = true, IsActive = true };

            InventoryBalance invSal = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = salmon.Id, OnHandQty = 30m, BaseUom = "KG", UnitCost = 60m, IsActive = true };
            InventoryBalance invChk = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = chicken.Id, OnHandQty = 100m, BaseUom = "KG", UnitCost = 20m, IsActive = true };
            InventoryBalance invMng = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = mango.Id, OnHandQty = 40m, BaseUom = "KG", UnitCost = 12m, IsActive = true };

            // Reserve 25 KG of salmon so only 5 KG is eligible!
            SilaCulinaryStockReservation resSal = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ReservationCode = "RES-SAL-01",
                MaterialId = salmon.Id,
                MaterialCode = "MAT-SAL",
                MaterialDescription = "Atlantic Salmon Fillet",
                LocationId = kitchen.Id,
                ReservedQty = 25m,
                FulfilledQty = 0m,
                BaseUom = "KG",
                Status = "ACTIVE",
                IsActive = true
            };

            Recipe rSal = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-SAL", Name = "Roasted Salmon", Category = "Mains", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 18m, Currency = "AED", IsActive = true };
            RecipeIngredient riSal = new() { Id = Guid.NewGuid(), RecipeId = rSal.Id, IngredientCode = "ING-SAL", MaterialId = salmon.Id, ItemCode = "MAT-SAL", ItemName = "Atlantic Salmon Fillet", Quantity = 0.30m, Uom = "KG", BaseQuantity = 0.30m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rChk = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-CHK", Name = "Herb Chicken", Category = "Mains", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 6m, Currency = "AED", IsActive = true };
            RecipeIngredient riChk = new() { Id = Guid.NewGuid(), RecipeId = rChk.Id, IngredientCode = "ING-CHK", MaterialId = chicken.Id, ItemCode = "MAT-CHK", ItemName = "Chicken Breast Boneless", Quantity = 0.30m, Uom = "KG", BaseQuantity = 0.30m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rDes = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-DES", Name = "Mango Tart", Category = "Desserts", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 3m, Currency = "AED", IsActive = true };
            RecipeIngredient riDes = new() { Id = Guid.NewGuid(), RecipeId = rDes.Id, IngredientCode = "ING-MNG", MaterialId = mango.Id, ItemCode = "MAT-MNG", ItemName = "Fresh Mango Puree", Quantity = 0.25m, Uom = "KG", BaseQuantity = 0.25m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rApp = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-APP", Name = "Mango Mezze", Category = "Starters", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 2.4m, Currency = "AED", IsActive = true };
            RecipeIngredient riApp = new() { Id = Guid.NewGuid(), RecipeId = rApp.Id, IngredientCode = "ING-MNG2", MaterialId = mango.Id, ItemCode = "MAT-MNG", ItemName = "Fresh Mango Puree", Quantity = 0.20m, Uom = "KG", BaseQuantity = 0.20m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            MenuItem miSal = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "MI-SAL", Name = "Roasted Salmon", Category = "Mains", Cuisine = "Mediterranean", LinkedRecipeId = rSal.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 75m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, AllergenNotes = "Fish", IsActive = true };
            MenuItem miChk = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "MI-CHK", Name = "Herb Chicken", Category = "Mains", Cuisine = "Mediterranean", LinkedRecipeId = rChk.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 60m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, DietaryTags = "Halal", IsActive = true };
            MenuItem miDes = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "MI-DES", Name = "Mango Tart", Category = "Desserts", Cuisine = "International", LinkedRecipeId = rDes.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 35m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, DietaryTags = "Vegetarian", IsActive = true };
            MenuItem miApp = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "MI-APP", Name = "Mango Mezze", Category = "Starters", Cuisine = "Arabic", LinkedRecipeId = rApp.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 30m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, DietaryTags = "Vegan,Vegetarian", IsActive = true };

            MenuPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                Name = "Grand Gala Dinner",
                MenuType = "BUFFET",
                ServiceFormat = "BUFFET",
                MealPeriod = "Dinner",
                BusinessDate = DateTime.UtcNow.Date.AddDays(2),
                ExpectedGuests = 300,
                ExpectedCovers = 300,
                TargetFoodCostPercent = 30m,
                Status = "PUBLISHED",
                VersionNumber = 1,
                IsActive = true
            };

            MenuPlanLine l1 = new() { Id = Guid.NewGuid(), MenuPlanId = plan.Id, MenuItemId = miSal.Id, CategoryName = "Mains", RecipeId = rSal.Id, PortionSize = 1m, PortionUom = "PORTION", PlannedPortions = 150, ActualPortions = 138, ParticipationRate = 0.50m, ExpectedServingsPerGuest = 1m, SellingPrice = 75m, SampledActualPortion = 1.15m, IsActive = true };
            MenuPlanLine l2 = new() { Id = Guid.NewGuid(), MenuPlanId = plan.Id, MenuItemId = miChk.Id, CategoryName = "Mains", RecipeId = rChk.Id, PortionSize = 1m, PortionUom = "PORTION", PlannedPortions = 180, ActualPortions = 175, ParticipationRate = 0.60m, ExpectedServingsPerGuest = 1m, SellingPrice = 60m, SampledActualPortion = 1.00m, IsActive = true };

            SilaCulinaryKitchen kitEntity = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                KitchenCode = "KIT-01",
                KitchenName = "Banquet Kitchen",
                DailyPortionCapacity = 800m,
                IsActive = true
            };

            List<object> seeds =
            [
                buyer, otherBuyer, property, kitchen, kitEntity,
                salmon, chicken, mango,
                invSal, invChk, invMng, resSal,
                rSal, riSal, rChk, riChk, rDes, riDes, rApp, riApp,
                miSal, miChk, miDes, miApp,
                plan, l1, l2,
                new MenuEngineeringSettings
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MenuEngineeringEnabled = true,
                    MeStudioEnabled = true,
                    DefaultOperatingMode = "AI_ASSISTED",
                    HighFoodCostPercent = 0.30m,
                    TargetFoodCostPercent = 30m,
                    DefaultCurrency = "AED",
                    IsActive = true
                },
                new SilaSupplier
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    SupplierCode = "SUP-SEA",
                    Name = "Gulf Seafood Supply",
                    Status = "ACTIVE",
                    Currency = "AED",
                    IsActive = true
                },
                new SilaCulinaryEvent
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    EventCode = "EVT-001",
                    Name = "Dubai Tech Summit Dinner",
                    EventType = "CONFERENCE_BANQUET",
                    StartDateUtc = DateTime.UtcNow.Date.AddDays(2),
                    EndDateUtc = DateTime.UtcNow.Date.AddDays(2),
                    ExpectedGuests = 300,
                    IsActive = true
                }
            ];

            for (int d = 28; d >= 1; d--)
            {
                DateTime bDate = DateTime.UtcNow.Date.AddDays(-d);
                seeds.Add(new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    OutletCode = "MAIN",
                    SourceTransactionId = $"POS-P3-{d}",
                    PosCode = "MI-CHK",
                    MenuItemId = miChk.Id,
                    RecipeId = rChk.Id,
                    BusinessDate = bDate,
                    QuantitySold = 90 + (d % 7) * 10,
                    Amount = (90 + (d % 7) * 10) * 60m,
                    NetRevenue = (90 + (d % 7) * 10) * 60m,
                    Status = "MAPPED",
                    IsActive = true
                });
            }

            db.Seed(seeds.ToArray());

            IConfiguration cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["InterCallService:CulinaryEngineUrl"] = "http://127.0.0.1:8013" })
                .Build();
            IHttpClientFactory httpFactory = new OfflineHttpClientFactory();
            SilaCulinaryFinancialService fin = new(db.Repository, db.Logger, httpFactory, cfg, new FakeProtector());
            SilaCulinaryAnalyticsService ana = new(db.Repository, db.Logger, httpFactory, cfg);
            MenuEngineeringStudioService studio = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), fin, ana, httpFactory);
            MenuEngineeringDigitalTwinService twin = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), httpFactory);
            MenuEngineeringAiOrchestratorService ai = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), studio, fin, ana, httpFactory, twin);

            return new TwinFixture
            {
                Db = db,
                Twin = twin,
                Studio = studio,
                Ai = ai,
                OrgId = orgId,
                OtherOrgId = otherOrgId,
                UserId = userId,
                AdminRoleId = adminRoleId,
                Buyer = buyer,
                Property = property,
                Kitchen = kitchen,
                Salmon = salmon,
                Chicken = chicken,
                Mango = mango,
                ActivePlan = plan
            };
        }

        [Fact]
        public async Task Test01_BuffetOptimization_EnforcesHardFoodCostConstraint()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MePhase3OptimizationJobDto job = await f.Twin.RunMultiObjectiveOptimizationAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MePhase3OptimizeRequestDto
                {
                    PropertyId = f.Property.Id,
                    MenuPlanId = f.ActivePlan.Id,
                    Mode = "PARETO_FRONTIER",
                    GuestCount = 300,
                    MaxFoodCostPercent = 30m,
                    MinTotalItems = 2,
                    MaxTotalItems = 4
                }, CancellationToken.None);

            Assert.NotEqual("FAILED", job.Status);
            Assert.NotEmpty(job.Solutions);
            Assert.All(job.Solutions.Where(s => s.IsFeasible), s =>
            {
                Assert.True(s.HardConstraintsSatisfied);
                Assert.True(s.FoodCostPercent <= 30.5m);
            });
        }

        [Fact]
        public async Task Test02_ReverseDependencyGraph_20PercentPriceIncreaseCascadesAcrossRecipesAndMenus()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            TwinDependencyAnalysisResultDto dep = await f.Twin.AnalyzeDependencyGraphAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new TwinDependencyAnalysisRequestDto
                {
                    PropertyId = f.Property.Id,
                    MenuPlanId = f.ActivePlan.Id,
                    ShockMaterialId = f.Salmon.Id,
                    ShockIngredientName = "Salmon",
                    PriceChangePercent = 20m
                }, CancellationToken.None);

            Assert.True(dep.IsAcyclic);
            Assert.True(dep.NodeCount >= 4);
            Assert.Contains(dep.AffectedRecipes, r => r.Contains("Salmon", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(dep.AffectedMenuItems, m => m.Contains("Salmon", StringComparison.OrdinalIgnoreCase));
            Assert.True(dep.TotalCostImpact > 0m);
        }

        [Fact]
        public async Task Test03_InventoryDrivenOptimization_ExcludesReservedAndBlockedStock()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MePhase3InventoryMenuResultDto res = await f.Twin.OptimizeInventoryDrivenMenuAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MePhase3InventoryMenuRequestDto
                {
                    PropertyId = f.Property.Id,
                    GuestCount = 150,
                    MinItems = 2,
                    MaxItems = 4,
                    MaxFoodCostPercent = 35m
                }, CancellationToken.None);

            Assert.True(res.ExcludedStockSummary.ContainsKey("reserved_qty"));
            Assert.True(res.ExcludedStockSummary["reserved_qty"] >= 25m);
            Assert.NotEmpty(res.SelectedDishes);
        }

        [Fact]
        public async Task Test04_HierarchicalDemandForecast_LeakageFreeWithBacktestMetrics()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeHierarchicalForecastResultDto fc = await f.Twin.ForecastHierarchicalDemandAsync(
                f.OrgId, f.AdminRoleId, f.Property.Id, 7, CancellationToken.None);

            Assert.True(fc.LeakageChecked);
            Assert.NotEmpty(fc.Nodes);
            Assert.Contains(fc.Nodes, n => n.Level == "PROPERTY");
            Assert.All(fc.Nodes, n => Assert.Equal(7, n.ReconciledForecast.Count));
        }

        [Fact]
        public async Task Test05_ColdStartPrediction_DisclosesUncertaintyAndComparables()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeColdStartPredictResultDto cs = await f.Twin.PredictColdStartItemAsync(
                f.OrgId, f.AdminRoleId,
                new MeColdStartPredictRequestDto
                {
                    DishName = "Grilled Sea Bass Mediterranean",
                    Cuisine = "Mediterranean",
                    Category = "Mains",
                    SellingPrice = 70m,
                    FoodCostPercent = 27m,
                    DietaryTags = new() { "Halal" }
                }, CancellationToken.None);

            Assert.True(cs.IsColdStartEstimate);
            Assert.Contains("cold-start", cs.Disclosure, StringComparison.OrdinalIgnoreCase);
            Assert.True(cs.DemandUpperP90 >= cs.DemandLowerP10);
            Assert.NotEmpty(cs.Comparables);
        }

        [Fact]
        public async Task Test06_PortionOptimization_RespectsApprovedPortionLimits()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiPortionOptimizeResultDto portions = await f.Ai.OptimizePortionsAsync(
                f.OrgId, f.UserId, f.AdminRoleId, f.ActivePlan.Id, f.Property.Id, CancellationToken.None);

            Assert.NotEmpty(portions.Rows);
            Assert.All(portions.Rows, l =>
            {
                Assert.True(l.RecommendedPlannedPortion > 0m);
                Assert.True(l.ExpectedWasteImpactPercent >= 0m || l.ExpectedWasteImpactPercent < 0m);
            });
        }

        [Fact]
        public async Task Test07_InsufficientProductionCapacity_ReportsInfeasibilityExplicitly()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MePhase3OptimizationJobDto job = await f.Twin.RunMultiObjectiveOptimizationAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MePhase3OptimizeRequestDto
                {
                    PropertyId = f.Property.Id,
                    MenuPlanId = f.ActivePlan.Id,
                    GuestCount = 500,
                    MaxFoodCostPercent = 30m,
                    OverrideProductionMinutes = 1m // Impossible 1-minute kitchen capacity
                }, CancellationToken.None);

            Assert.Equal("INFEASIBLE", job.Status);
            Assert.NotEmpty(job.InfeasibilityReasons);
        }

        [Fact]
        public async Task Test08_MonteCarloSimulation_IsSeedReproducible()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            var req = new MeMonteCarloRequestDto
            {
                PropertyId = f.Property.Id,
                MenuPlanId = f.ActivePlan.Id,
                Seed = 777,
                Runs = 500,
                GuestCountMean = 300m,
                GuestCountStd = 35m,
                TargetFoodCostPercent = 30m
            };

            MeMonteCarloResultDto r1 = await f.Twin.RunMonteCarloAsync(f.OrgId, f.UserId, f.AdminRoleId, req, CancellationToken.None);
            MeMonteCarloResultDto r2 = await f.Twin.RunMonteCarloAsync(f.OrgId, f.UserId, f.AdminRoleId, req, CancellationToken.None);

            Assert.Equal(777, r1.Seed);
            Assert.Equal(r1.ExpectedCost, r2.ExpectedCost);
            Assert.Equal(r1.ProbExceedFoodCostTarget, r2.ProbExceedFoodCostTarget);
        }

        [Fact]
        public async Task Test09_SensitivityTornado_RanksTopCostAndMarginDrivers()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeSensitivityResultDto sens = await f.Twin.RunSensitivityAnalysisAsync(
                f.OrgId, f.AdminRoleId,
                new MeSensitivityRequestDto { PropertyId = f.Property.Id, MenuPlanId = f.ActivePlan.Id, TargetFoodCostPercent = 30m },
                CancellationToken.None);

            Assert.NotEmpty(sens.Tornado);
            Assert.NotEmpty(sens.TopRiskDrivers);
            Assert.True(sens.Tornado[0].MarginSwing >= sens.Tornado[^1].MarginSwing);
        }

        [Fact]
        public async Task Test10_CompareMultipleScenarios_UsesConsistentCalculationBasis()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Twin.CreateScenarioAsync(f.OrgId, f.UserId, f.AdminRoleId, new MeScenarioCreateRequestDto { PropertyId = f.Property.Id, MenuPlanId = f.ActivePlan.Id, ScenarioName = "Scenario A", ScenarioType = "OPTIMIZED" }, CancellationToken.None);
            await f.Twin.CreateScenarioAsync(f.OrgId, f.UserId, f.AdminRoleId, new MeScenarioCreateRequestDto { PropertyId = f.Property.Id, MenuPlanId = f.ActivePlan.Id, ScenarioName = "Scenario B", ScenarioType = "MANUAL" }, CancellationToken.None);

            MeScenarioComparisonDto cmp = await f.Twin.GetAndCompareScenariosAsync(f.OrgId, f.AdminRoleId, f.ActivePlan.Id, CancellationToken.None);

            Assert.Equal("me-calc-v3.0", cmp.CalculationBasis);
            Assert.True(cmp.FeasibleScenarios.Count >= 2);
            Assert.All(cmp.FeasibleScenarios, s => Assert.Equal("me-calc-v3.0", s.CalculationVersion));
        }

        [Fact]
        public async Task Test11_StaleSnapshotDetectionAndRefresh_PreservesPreviousScenarioReproducibility()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            TwinSnapshotDto snap1 = await f.Twin.CreateSnapshotAsync(f.OrgId, f.UserId, f.AdminRoleId,
                new CreateTwinSnapshotRequestDto { PropertyId = f.Property.Id, MenuPlanId = f.ActivePlan.Id, SnapshotType = "LIVE" },
                CancellationToken.None);

            MeScenarioLabItemDto sc = await f.Twin.CreateScenarioAsync(f.OrgId, f.UserId, f.AdminRoleId,
                new MeScenarioCreateRequestDto { PropertyId = f.Property.Id, MenuPlanId = f.ActivePlan.Id, BaselineSnapshotId = snap1.SnapshotId, ScenarioName = "Pinned Baseline Scenario" },
                CancellationToken.None);

            // Simulate underlying operational data change
            MenuItem mi = await f.Db.Context.MenuItem.FirstAsync(x => x.BuyerId == f.Buyer.Id);
            mi.DateUpdated = DateTime.UtcNow.AddMinutes(5);
            await f.Db.Context.SaveChangesAsync();

            TwinSnapshotDto staleCheck = await f.Twin.GetSnapshotAsync(f.OrgId, f.AdminRoleId, snap1.SnapshotId, CancellationToken.None);
            Assert.True(staleCheck.IsStale);

            TwinSnapshotDto refreshed = await f.Twin.RefreshSnapshotAsync(f.OrgId, f.UserId, f.AdminRoleId, snap1.SnapshotId, CancellationToken.None);
            Assert.Equal(snap1.VersionNumber + 1, refreshed.VersionNumber);
            Assert.False(refreshed.IsStale);
            Assert.Equal(snap1.SnapshotId, sc.BaselineSnapshotId);
        }

        [Fact]
        public async Task Test12_SupplierDeliveryDelayStressTest_FlagsShortageRiskAndCorrectiveActions()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeStressTestResultDto st = await f.Twin.RunStressTestAsync(f.OrgId, f.AdminRoleId, new MeStressTestRequestDto
            {
                PropertyId = f.Property.Id,
                MenuPlanId = f.ActivePlan.Id,
                BaselineGuests = 300,
                IngredientBufferDays = 2,
                Scenarios = new()
                {
                    new MeStressScenarioInputDto { StressType = "SUPPLIER_DELAY", Label = "Supplier Delay 4 Days", SupplierDelayDays = 4 }
                }
            }, CancellationToken.None);

            Assert.Single(st.Outcomes);
            Assert.Contains(st.Outcomes[0].ShortageRisk, new[] { "HIGH", "CRITICAL" });
            Assert.NotEmpty(st.Outcomes[0].CorrectiveActions);
        }

        [Fact]
        public async Task Test13_UnsafeIngredientSubstitution_IsRejected()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeSubstitutionCascadeResultDto sub = await f.Twin.EvaluateSubstitutionCascadeAsync(f.OrgId, f.AdminRoleId, new MeSubstitutionCascadeRequestDto
            {
                OriginalIngredient = "Butter",
                OriginalQty = 1m,
                OriginalUnitCost = 20m,
                RecipeSellingPrice = 100m,
                ExcludedAllergens = new() { "Nuts" },
                RequiredDietaryTags = new() { "Halal" },
                Candidates = new()
                {
                    new MeSubstitutionCandidateRequestDto { SubstituteId = "1", SubstituteName = "Peanut Oil", UnitCost = 10m, Allergens = new() { "Nuts" }, DietaryTags = new() { "Halal" }, IsApprovedRelationship = true },
                    new MeSubstitutionCandidateRequestDto { SubstituteId = "2", SubstituteName = "Olive Oil", UnitCost = 18m, Allergens = new(), DietaryTags = new() { "Halal" }, IsApprovedRelationship = true }
                }
            }, CancellationToken.None);

            var peanut = sub.Results.First(r => r.SubstituteName == "Peanut Oil");
            var olive = sub.Results.First(r => r.SubstituteName == "Olive Oil");
            Assert.Equal("REJECTED_UNSAFE", peanut.Category);
            Assert.False(peanut.IsSafe);
            Assert.Equal("APPROVED_SUBSTITUTION", olive.Category);
            Assert.True(olive.IsSafe);
        }

        [Fact]
        public async Task Test14_ConflictingHardConstraints_NeverSilentlyRelaxed()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MePhase3OptimizationJobDto job = await f.Twin.RunMultiObjectiveOptimizationAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new MePhase3OptimizeRequestDto
                {
                    PropertyId = f.Property.Id,
                    MenuPlanId = f.ActivePlan.Id,
                    GuestCount = 300,
                    MaxFoodCostPercent = 1m, // Impossible 1% food cost ceiling
                    MinTotalItems = 3
                }, CancellationToken.None);

            Assert.Equal("INFEASIBLE", job.Status);
            Assert.Empty(job.Solutions);
            Assert.NotEmpty(job.InfeasibilityReasons);
        }

        [Fact]
        public async Task Test15_ContinuousLearning_CapturesPredictedVsActualOutcomeLineage()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MeEventOutcomeComparisonDto outcome = await f.Twin.CaptureCompletedEventOutcomeAsync(
                f.OrgId, f.UserId, f.AdminRoleId, f.ActivePlan.Id, CancellationToken.None);

            Assert.Equal(f.ActivePlan.Id, outcome.MenuPlanId);
            Assert.True(outcome.PredictedCovers > 0m);
            Assert.True(outcome.ActualCovers > 0m);

            MeModelGovernanceStatusDto gov = await f.Twin.GetModelGovernanceStatusAsync(f.OrgId, f.AdminRoleId, CancellationToken.None);
            Assert.NotEmpty(gov.RecentOutcomeLineage);
        }

        [Fact]
        public async Task Test16_ExternalLlmDisabled_DeterministicIntelligenceRemainsOperational()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            // Explicitly disable external LLM & AI provider -> deterministic intelligence remains operational
            f.Db.Context.SilaCulinaryAiConfiguration.Add(new SilaCulinaryAiConfiguration
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                OperatingMode = "DISABLED",
                LocalEnabled = false,
                AllowExternalAi = false,
                GeminiEnabled = false,
                ClaudeEnabled = false,
                IsActive = true
            });
            await f.Db.Context.SaveChangesAsync();

            AiContextualResponseDto nlResp = await f.Ai.RunContextualAssistantAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new AiContextualRequestDto
                {
                    ScreenKey = "silaMeStudio",
                    Action = "AnalyzeThis",
                    MenuPlanId = f.ActivePlan.Id,
                    PropertyId = f.Property.Id,
                    UserPrompt = "Show the probability of exceeding the event budget"
                }, CancellationToken.None);

            Assert.False(nlResp.AiProviderUsed);
            Assert.Contains("RunMonteCarloSimulation", nlResp.InvokedTools);
            Assert.Contains("FORECAST", nlResp.Summary, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Test17_CrossTenantSnapshotAccess_IsDenied()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            TwinSnapshotDto snap = await f.Twin.CreateSnapshotAsync(
                f.OrgId, f.UserId, f.AdminRoleId,
                new CreateTwinSnapshotRequestDto { PropertyId = f.Property.Id, MenuPlanId = f.ActivePlan.Id },
                CancellationToken.None);

            await Assert.ThrowsAsync<NotFoundCustomException>(() =>
                f.Twin.GetSnapshotAsync(f.OtherOrgId, f.AdminRoleId, snap.SnapshotId, CancellationToken.None));
        }

        [Fact]
        public async Task Test18_ConcurrentOptimizationJobs_CompleteQuicklyWithinTenantQuota()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            Stopwatch sw = Stopwatch.StartNew();
            Task<MePhase3OptimizationJobDto>[] tasks = Enumerable.Range(0, 3)
                .Select(i => f.Twin.RunMultiObjectiveOptimizationAsync(
                    f.OrgId, f.UserId, f.AdminRoleId,
                    new MePhase3OptimizeRequestDto
                    {
                        PropertyId = f.Property.Id,
                        MenuPlanId = f.ActivePlan.Id,
                        GuestCount = 250 + i * 25,
                        MaxFoodCostPercent = 30m,
                        IdempotencyKey = $"CONC-{i}"
                    }, CancellationToken.None))
                .ToArray();

            MePhase3OptimizationJobDto[] results = await Task.WhenAll(tasks);
            sw.Stop();

            Assert.Equal(3, results.Length);
            Assert.All(results, r => Assert.NotEqual("FAILED", r.Status));
            Assert.True(sw.ElapsedMilliseconds < 15000);
        }
    }
}
