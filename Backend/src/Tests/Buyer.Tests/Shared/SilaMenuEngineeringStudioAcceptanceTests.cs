using System.Net;
using Buyer.Application.Features.Shared;
using Buyer.Application.Features.Shared.MenuEngineering;
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
    public class SilaMenuEngineeringStudioAcceptanceTests
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

        private static (
            TestDatabase Db,
            MenuEngineeringStudioService Service,
            Guid OrgId,
            Guid UserId,
            Guid AdminRoleId,
            BuyerBusinessProfile Buyer,
            BuyerProperty Property,
            InventoryLocation Kitchen,
            ItemBuyerMaster Salmon,
            ItemBuyerMaster SauceMix,
            Recipe GrilledSalmonRecipe,
            Recipe PepperSauceRecipe,
            MenuItem SalmonMenuItem,
            MenuItem ChickenMenuItem) CreateFixture()
        {
            TestDatabase db = new();
            Guid orgId = Guid.NewGuid();
            Guid userId = Guid.NewGuid();
            Guid adminRoleId = Common.BUYER_ADMIN_ROLE_ID;

            BuyerBusinessProfile buyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                OrganizationName = "SILA Hospitality Group",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "chef@sila.test",
                Phone = "+971500000002",
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
            BuyerProperty property = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                CompanyCode = "1000",
                PlantCode = "1060",
                PropertyName = "SILA Grand Dubai",
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
                UnitCost = 40m,
                IsInventoryItem = true,
                IsActive = true
            };
            ItemBuyerMaster sauceMix = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-PEPPER",
                Description = "Cracked Pepper Sauce Base",
                MaterialGroup = "Dry Goods",
                BaseUnitOfMeasure = "KG",
                OrderUnitOfMeasure = "KG",
                UnitCost = 10m,
                IsInventoryItem = true,
                IsActive = true
            };
            InventoryBalance stockSalmon = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                LocationId = kitchen.Id,
                MaterialId = salmon.Id,
                OnHandQty = 20m,
                BaseUom = "KG",
                UnitCost = 40m,
                LastMovementOn = DateTime.UtcNow.AddHours(-1),
                IsActive = true
            };
            SilaCulinaryStockReservation resSalmon = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                LocationId = kitchen.Id,
                MaterialId = salmon.Id,
                ReservedQty = 5m,
                BaseUom = "KG",
                Status = "ACTIVE",
                IsActive = true
            };
            Recipe salmonRecipe = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                RecipeCode = "RCP-SALMON",
                Name = "Grilled Salmon Fillet V1",
                Category = "Mains",
                ItemMode = "RECIPE",
                ServingQty = 1m,
                ServingUom = "PORTION",
                SellingUom = "PORTION",
                Status = "APPROVED",
                Version = 1,
                ActiveVersion = 1,
                TotalCost = 10m,
                Currency = "AED",
                IsActive = true
            };
            RecipeIngredient salmonIng = new()
            {
                Id = Guid.NewGuid(),
                RecipeId = salmonRecipe.Id,
                IngredientCode = "ING-SALMON",
                MaterialId = salmon.Id,
                ItemCode = "MAT-SALMON",
                ItemName = "Atlantic Salmon Fillet",
                Quantity = 0.25m, // 0.25 KG * 40 AED = 10 AED per portion
                Uom = "KG",
                BaseQuantity = 0.25m,
                BaseUom = "KG",
                Sequence = 1,
                Version = 1,
                IsActive = true
            };
            Recipe sauceRecipe = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                RecipeCode = "RCP-SAUCE",
                Name = "Pepper Sauce",
                Category = "Mains",
                ItemMode = "RECIPE",
                ServingQty = 1m,
                ServingUom = "PORTION",
                SellingUom = "PORTION",
                Status = "APPROVED",
                Version = 1,
                ActiveVersion = 1,
                TotalCost = 1m,
                Currency = "AED",
                IsActive = true
            };
            RecipeIngredient sauceIng = new()
            {
                Id = Guid.NewGuid(),
                RecipeId = sauceRecipe.Id,
                IngredientCode = "ING-PEPPER",
                MaterialId = sauceMix.Id,
                ItemCode = "MAT-PEPPER",
                ItemName = "Cracked Pepper Sauce Base",
                Quantity = 0.1m, // 0.1 KG * 10 = 1 AED
                Uom = "KG",
                BaseQuantity = 0.1m,
                BaseUom = "KG",
                Sequence = 1,
                Version = 1,
                IsActive = true
            };
            MenuItem salmonItem = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                MenuItemCode = "ME-SALMON",
                Name = "Grilled Salmon",
                Category = "Mains",
                Cuisine = "Mediterranean",
                MealPeriods = "Dinner",
                ServiceFormat = "A_LA_CARTE",
                LinkedRecipeId = salmonRecipe.Id,
                ApprovedRecipeVersion = 1,
                DefaultPortionQty = 1m,
                PortionUom = "PORTION",
                SellingPrice = 40m,
                TaxRatePercent = 5m,
                Currency = "AED",
                ApprovalStatus = "APPROVED",
                Status = "ACTIVE",
                IsAvailable = true,
                IsActive = true
            };
            MenuItem chickenItem = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                MenuItemCode = "ME-CHICK",
                Name = "Herb Chicken",
                Category = "Mains",
                Cuisine = "Mediterranean",
                MealPeriods = "Dinner",
                ServiceFormat = "A_LA_CARTE",
                LinkedRecipeId = sauceRecipe.Id,
                ApprovedRecipeVersion = 1,
                DefaultPortionQty = 1m,
                PortionUom = "PORTION",
                SellingPrice = 25m,
                TaxRatePercent = 5m,
                Currency = "AED",
                ApprovalStatus = "APPROVED",
                Status = "ACTIVE",
                IsAvailable = true,
                IsActive = true
            };
            MenuEngineeringSettings settings = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MenuEngineeringEnabled = true,
                MeStudioEnabled = true,
                PopularityThreshold = 0.70m,
                HighFoodCostPercent = 0.35m,
                DefaultCurrency = "AED",
                IsActive = true
            };

            db.Seed(buyer, property, kitchen, salmon, sauceMix, stockSalmon, resSalmon, salmonRecipe, salmonIng, sauceRecipe, sauceIng, salmonItem, chickenItem, settings);

            IConfiguration cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["InterCallService:CulinaryEngineUrl"] = "http://127.0.0.1:8013" })
                .Build();
            IHttpClientFactory httpFactory = new OfflineHttpClientFactory();
            SilaCulinaryFinancialService fin = new(db.Repository, db.Logger, httpFactory, cfg, new FakeProtector());
            SilaCulinaryAnalyticsService ana = new(db.Repository, db.Logger, httpFactory, cfg);
            MenuEngineeringStudioService svc = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), fin, ana, httpFactory);

            return (db, svc, orgId, userId, adminRoleId, buyer, property, kitchen, salmon, sauceMix, salmonRecipe, sauceRecipe, salmonItem, chickenItem);
        }

        [Fact]
        public async Task Scenario01_CreateMenuFromApprovedRecipe_CalculatesRecipeCost()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuPlanEnhancedDto plan = await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Dinner A La Carte",
                MenuType = "A_LA_CARTE",
                ServiceFormat = "A_LA_CARTE",
                MealPeriod = "Dinner",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date,
                ExpectedCovers = 100,
                Lines =
                [
                    new MenuPlanLineEnhancedWriteDto
                    {
                        MenuItemId = f.SalmonMenuItem.Id,
                        CategoryName = "Mains",
                        PlannedPortions = 100,
                        ParticipationRate = 1m,
                        ExpectedServingsPerGuest = 1m
                    }
                ]
            }, CancellationToken.None);

            Assert.Single(plan.Lines);
            Assert.Equal(10.00m, plan.Lines[0].PortionCost);
            Assert.Equal(1000.00m, plan.Lines[0].TotalLineCost);
            Assert.True((plan.Lines[0].ExpectedLineRevenue ?? 0m) > 0m);
        }

        [Fact]
        public async Task Scenario02_ChangeGuestCount300To500_RecalculatesDemandPortionsAndCosts()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuPlanEnhancedDto plan = await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Buffet Dinner",
                MenuType = "BUFFET",
                ServiceFormat = "BUFFET",
                MealPeriod = "Dinner",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date,
                ExpectedCovers = 300,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonMenuItem.Id, ParticipationRate = 0.5m, ExpectedServingsPerGuest = 1m }]
            }, CancellationToken.None);

            WhatIfSimulationResultDto sim300 = await f.Service.RunSimulationAsync(f.OrgId, f.UserId, f.AdminRoleId, new WhatIfSimulationRequestDto
            {
                MenuPlanId = plan.Id,
                ScenarioName = "300 Covers",
                ExpectedCovers = 300
            }, CancellationToken.None);

            WhatIfSimulationResultDto sim500 = await f.Service.RunSimulationAsync(f.OrgId, f.UserId, f.AdminRoleId, new WhatIfSimulationRequestDto
            {
                MenuPlanId = plan.Id,
                ScenarioName = "500 Covers",
                ExpectedCovers = 500
            }, CancellationToken.None);

            Assert.True(sim500.RecommendedPortions > sim300.RecommendedPortions);
            Assert.True(sim500.IngredientRequirement > sim300.IngredientRequirement);
            Assert.True(sim500.FoodCost > sim300.FoodCost);
        }

        [Fact]
        public async Task Scenario03_IncreaseIngredientPrice10Pct_SimulationOnly_LeavesLiveDataUnchanged()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuPlanEnhancedDto plan = await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Live Dinner",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date,
                ExpectedCovers = 100,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonMenuItem.Id, PlannedPortions = 100 }]
            }, CancellationToken.None);

            WhatIfSimulationResultDto sim = await f.Service.RunSimulationAsync(f.OrgId, f.UserId, f.AdminRoleId, new WhatIfSimulationRequestDto
            {
                MenuPlanId = plan.Id,
                ScenarioName = "+10% Ingredient Cost",
                IngredientPriceChangePercent = 10m,
                SaveScenario = false
            }, CancellationToken.None);

            Assert.Equal(Math.Round(sim.BaselineCost * 1.10m, 2), sim.FoodCost);
            Assert.True(sim.ContributionMargin < sim.BaselineMargin);
            Assert.Contains("fixed-demand", sim.DemandAssumptionDisclosure, StringComparison.OrdinalIgnoreCase);

            // Live data untouched
            ItemBuyerMaster liveMat = await f.Db.Context.ItemBuyerMaster.FirstAsync(x => x.Id == f.Salmon.Id);
            Assert.Equal(40m, liveMat.UnitCost);
        }

        [Fact]
        public async Task Scenario04And05_PosImportUnmappedAndComboWithModifier_NoDoubleCount()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            f.Db.Seed(
                new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = f.Buyer.Id,
                    PropertyId = f.Property.Id,
                    SourceTransactionId = "TX-UNMAPPED-1",
                    PosLineId = "1",
                    LineNumber = 1,
                    OutletCode = "MAIN",
                    BusinessDate = DateTime.UtcNow.Date,
                    PosCode = "UNKNOWN_DISH_99",
                    QuantitySold = 5m,
                    GrossSellingPrice = 100m,
                    NetRevenue = 100m,
                    Status = "UNMAPPED",
                    IsActive = true
                },
                new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = f.Buyer.Id,
                    PropertyId = f.Property.Id,
                    SourceTransactionId = "TX-COMBO-1",
                    PosLineId = "1",
                    LineNumber = 1,
                    OutletCode = "MAIN",
                    BusinessDate = DateTime.UtcNow.Date,
                    PosCode = "COMBO_SALMON_MAIN",
                    ComboReference = "COMBO_1",
                    ModifierCode = "EXTRA_SAUCE",
                    MenuItemId = f.SalmonMenuItem.Id,
                    QuantitySold = 2m,
                    GrossSellingPrice = 80m,
                    NetRevenue = 80m,
                    Status = "MAPPED",
                    IsActive = true
                },
                new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = f.Buyer.Id,
                    PropertyId = f.Property.Id,
                    SourceTransactionId = "TX-COMBO-1",
                    PosLineId = "2",
                    LineNumber = 2,
                    OutletCode = "MAIN",
                    BusinessDate = DateTime.UtcNow.Date,
                    PosCode = "COMBO_SALMON_COMP",
                    ComboReference = "COMBO_1",
                    MenuItemId = f.SalmonMenuItem.Id,
                    QuantitySold = 2m,
                    GrossSellingPrice = 0m,
                    NetRevenue = 0m,
                    Status = "MAPPED",
                    IsActive = true
                });

            await f.Service.SavePosMappingExtendedAsync(f.OrgId, f.UserId, f.AdminRoleId, new PosMappingExtendedWriteDto
            {
                PosCode = "COMBO_SALMON_MAIN",
                MenuItemId = f.SalmonMenuItem.Id,
                MappingType = "COMBO",
                PortionMultiplier = 1m,
                ModifierCode = "EXTRA_SAUCE",
                ModifierRecipeId = f.PepperSauceRecipe.Id,
                ApproveMapping = true
            }, CancellationToken.None);

            await f.Service.SavePosMappingExtendedAsync(f.OrgId, f.UserId, f.AdminRoleId, new PosMappingExtendedWriteDto
            {
                PosCode = "COMBO_SALMON_COMP",
                MenuItemId = f.SalmonMenuItem.Id,
                MappingType = "COMBO_COMPONENT",
                ComboParentCode = "COMBO_SALMON_MAIN",
                IsComboComponent = true,
                ExcludeComponentDoubleCount = true,
                ApproveMapping = true
            }, CancellationToken.None);

            PosMappingWorkbenchDto wb = await f.Service.GetPosMappingWorkbenchAsync(f.OrgId, f.AdminRoleId, new SilaMenuEngineeringFilterDto(), CancellationToken.None);
            Assert.True(wb.UnmappedCount >= 1);
            Assert.Contains(wb.Mappings, m => m.PosCode == "UNKNOWN_DISH_99" && m.Status == "UNMAPPED");

            ConsumptionVarianceDetailDto cons = await f.Service.GetConsumptionVarianceAsync(f.OrgId, f.AdminRoleId, new SilaMenuEngineeringFilterDto(), CancellationToken.None);
            // Should count Salmon once (2 * 0.25 = 0.5 KG, NOT 4 * 0.25 = 1.0 KG) + Pepper Sauce modifier (2 * 0.1 = 0.2 KG)
            ConsumptionVarianceRowDto? salmonRow = cons.Rows.FirstOrDefault(r => r.MaterialId == f.Salmon.Id);
            ConsumptionVarianceRowDto? sauceRow = cons.Rows.FirstOrDefault(r => r.MaterialId == f.SauceMix.Id);
            Assert.NotNull(salmonRow);
            Assert.Equal(0.5m, salmonRow!.TheoreticalQty);
            Assert.NotNull(sauceRow);
            Assert.Equal(0.2m, sauceRow!.TheoreticalQty);
        }

        [Fact]
        public async Task Scenario06_MenuPerformanceMatrix_ClassifiesQuadrantsAndExcludesIncomplete()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuItem unlinkedNew = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PropertyId = f.Property.Id,
                MenuItemCode = "ME-NEW",
                Name = "New Seasonal Tart",
                Category = "Mains",
                SellingPrice = 30m,
                IsAvailable = true,
                IsActive = true
            };
            f.Db.Seed(unlinkedNew,
                new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = f.Buyer.Id,
                    PropertyId = f.Property.Id,
                    SourceTransactionId = "TX-PERF-1",
                    PosLineId = "1",
                    LineNumber = 1,
                    OutletCode = "MAIN",
                    BusinessDate = DateTime.UtcNow.Date,
                    PosCode = "SALMON",
                    MenuItemId = f.SalmonMenuItem.Id,
                    QuantitySold = 80m,
                    GrossSellingPrice = 3200m,
                    NetRevenue = 3200m,
                    Status = "MAPPED",
                    IsActive = true
                },
                new PosSalesTransaction
                {
                    Id = Guid.NewGuid(),
                    BuyerId = f.Buyer.Id,
                    PropertyId = f.Property.Id,
                    SourceTransactionId = "TX-PERF-2",
                    PosLineId = "1",
                    LineNumber = 1,
                    OutletCode = "MAIN",
                    BusinessDate = DateTime.UtcNow.Date,
                    PosCode = "CHICKEN",
                    MenuItemId = f.ChickenMenuItem.Id,
                    QuantitySold = 10m,
                    GrossSellingPrice = 250m,
                    NetRevenue = 250m,
                    Status = "MAPPED",
                    IsActive = true
                });

            MenuPerformanceMatrixDto matrix = await f.Service.GetPerformanceMatrixAsync(f.OrgId, f.AdminRoleId, new SilaMenuEngineeringFilterDto(), "FOOD_COST_ONLY", 0.70m, CancellationToken.None);
            Assert.Equal(2, matrix.Rows.Count);
            Assert.Contains(matrix.ExcludedRows, x => x.MenuItemId == unlinkedNew.Id);
            Assert.Contains(matrix.Rows, r => r.MenuItemId == f.SalmonMenuItem.Id && r.Classification == "STAR");
        }

        [Fact]
        public async Task Scenario07And10_HistoricalMenuCopyAndThreeScenarioComparison()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuPlanEnhancedDto orig = await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Gala Menu 2025",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date.AddDays(-10),
                ExpectedCovers = 200,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonMenuItem.Id, PlannedPortions = 200 }]
            }, CancellationToken.None);

            // Recost at current recipe/prices when copied from historical
            MenuPlanEnhancedDto draftCopy = await f.Service.CreateMenuFromHistoricalAsync(f.OrgId, f.UserId, f.AdminRoleId, orig.Id, "MENU_PLAN", f.Property.Id, CancellationToken.None);
            Assert.Equal("DRAFT", draftCopy.Status);
            Assert.Equal(10.00m, draftCopy.Lines[0].PortionCost);

            // Compare 3 scenarios side by side
            var s1 = await f.Service.RunSimulationAsync(f.OrgId, f.UserId, f.AdminRoleId, new WhatIfSimulationRequestDto { MenuPlanId = draftCopy.Id, ScenarioName = "Base", ExpectedCovers = 200, SaveScenario = true }, CancellationToken.None);
            var s2 = await f.Service.RunSimulationAsync(f.OrgId, f.UserId, f.AdminRoleId, new WhatIfSimulationRequestDto { MenuPlanId = draftCopy.Id, ScenarioName = "High Covers", ExpectedCovers = 350, SaveScenario = true }, CancellationToken.None);
            var s3 = await f.Service.RunSimulationAsync(f.OrgId, f.UserId, f.AdminRoleId, new WhatIfSimulationRequestDto { MenuPlanId = draftCopy.Id, ScenarioName = "Premium Price", ExpectedCovers = 200, SellingPriceChangePercent = 15m, SaveScenario = true }, CancellationToken.None);

            ScenarioCompareResultDto cmp = await f.Service.CompareScenariosAsync(f.OrgId, f.AdminRoleId, [s1.ScenarioId!.Value, s2.ScenarioId!.Value, s3.ScenarioId!.Value], CancellationToken.None);
            Assert.Equal(3, cmp.Scenarios.Count);
            Assert.Contains("identical", cmp.GovernanceNote, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Scenario08_DemandExceedsStock_ExcludesLatePoAndGeneratesDraftRec()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            DateTime requiredDate = DateTime.UtcNow.Date.AddDays(2);
            // Eligible on-hand is 20 - 5 reserved = 15 KG. Create an Open PO arriving AFTER requiredDate (Day +5)
            PurchaseOrder latePo = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PoNumber = "PO-LATE-1",
                Status = "APPROVED",
                DeliveryDate = requiredDate.AddDays(3),
                IsActive = true
            };
            f.Db.Seed(latePo, new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = latePo.Id,
                MaterialCode = f.Salmon.MaterialCode,
                ProductName = f.Salmon.Description,
                Quantity = 100m,
                ReceivedQuantity = 0m,
                UnitPrice = 40m,
                IsActive = true
            });

            await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Large Banquet",
                PropertyId = f.Property.Id,
                BusinessDate = requiredDate,
                ExpectedCovers = 200,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonMenuItem.Id, PlannedPortions = 200 }] // needs 200 * 0.25 = 50 KG
            }, CancellationToken.None);

            MenuPurchasePlanDto pp = await f.Service.GetPurchasePlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id, DateFrom = requiredDate, DateTo = requiredDate }, CancellationToken.None);
            MenuPurchasePlanRowDto? salmonLine = pp.Rows.FirstOrDefault(r => r.MaterialId == f.Salmon.Id);
            Assert.NotNull(salmonLine);
            Assert.Equal(100m, salmonLine!.LateOpenPoExcluded);
            Assert.Equal(0m, salmonLine.EligibleIncomingOnTime);
            Assert.Equal(35m, salmonLine.Shortage); // 50 needed - 15 net available

            var draftRec = await f.Service.CreateDraftPurchaseRecommendationAsync(f.OrgId, f.UserId, f.AdminRoleId, new SilaMenuEngineeringFilterDto { PropertyId = f.Property.Id, DateFrom = requiredDate, DateTo = requiredDate }, CancellationToken.None);
            Assert.NotEmpty(draftRec.Items);
        }

        [Fact]
        public async Task Scenario09And14_AiOfflineFallbackAndNoUnauthorizedRecipeMutation()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            AiMenuIntelligenceDto ai = await f.Service.GetAiMenuIntelligenceAsync(f.OrgId, f.AdminRoleId, null, f.Property.Id, CancellationToken.None);
            Assert.False(ai.AiProviderAvailable);
            Assert.Equal("DETERMINISTIC_OFFLINE", ai.ProviderMode);
            Assert.NotEmpty(ai.Recommendations);
            Assert.All(ai.Recommendations, r => Assert.True(r.RequiresApproval));

            // Verify recipe was not mutated
            Recipe recipe = await f.Db.Context.Recipe.FirstAsync(x => x.Id == f.GrilledSalmonRecipe.Id);
            Assert.Equal(1, recipe.Version);
        }

        [Fact]
        public async Task Scenario11_PublishedMenuEdited_CreatesControlledNewDraftVersion()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuPlanEnhancedDto p = await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Name = "Spring A La Carte",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date,
                ExpectedCovers = 150,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonMenuItem.Id, PlannedPortions = 150 }]
            }, CancellationToken.None);

            await f.Service.TransitionMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanTransitionDto { MenuPlanId = p.Id, TargetStatus = "UNDER_REVIEW" }, CancellationToken.None);
            await f.Service.TransitionMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanTransitionDto { MenuPlanId = p.Id, TargetStatus = "APPROVED" }, CancellationToken.None);
            MenuPlanEnhancedDto pub = await f.Service.TransitionMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanTransitionDto { MenuPlanId = p.Id, TargetStatus = "PUBLISHED" }, CancellationToken.None);
            Assert.Equal("PUBLISHED", pub.Status);

            MenuPlanEnhancedDto newVersion = await f.Service.SaveMenuPlanAsync(f.OrgId, f.UserId, f.AdminRoleId, new MenuPlanEnhancedWriteDto
            {
                Id = pub.Id,
                Name = "Spring A La Carte Rev",
                PropertyId = f.Property.Id,
                BusinessDate = DateTime.UtcNow.Date,
                ExpectedCovers = 180,
                Lines = [new MenuPlanLineEnhancedWriteDto { MenuItemId = f.SalmonMenuItem.Id, PlannedPortions = 180 }]
            }, CancellationToken.None);

            Assert.NotEqual(pub.Id, newVersion.Id);
            Assert.Equal(2, newVersion.VersionNumber);
            Assert.Equal("DRAFT", newVersion.Status);
            Assert.Equal(pub.Id, newVersion.ParentPublishedPlanId);

            // Original published remains untouched
            MenuPlan origDb = await f.Db.Context.MenuPlan.FirstAsync(x => x.Id == pub.Id);
            Assert.Equal("PUBLISHED", origDb.Status);
            Assert.Equal(150m, origDb.ExpectedCovers);
        }

        [Fact]
        public async Task Scenario12_UnauthorizedUserAccessesMenuMargins_DeniedWithoutDisclosure()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            Guid unauthorizedRoleId = Guid.NewGuid();
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                f.Service.GetCostMarginAnalysisAsync(f.OrgId, unauthorizedRoleId, new SilaMenuEngineeringFilterDto(), CancellationToken.None));
        }

        [Fact]
        public async Task Scenario13And15_ReportsAndFeatureFlagDisabledDoesNotBreakProcurement()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            List<MenuEngineeringReportItemDto> catalog = f.Service.GetReportCatalog();
            Assert.Equal(22, catalog.Count);
            MenuEngineeringReportExportDto exp = await f.Service.ExportReportAsync(f.OrgId, f.AdminRoleId, "MENU_ITEM_MASTER", "CSV", new SilaMenuEngineeringFilterDto(), CancellationToken.None);
            Assert.Contains("ME-SALMON", exp.TextContent);

            // Disable Menu Engineering via Setup feature flag
            MenuEngineeringSetupDto setup = await f.Service.GetSetupAsync(f.OrgId, f.AdminRoleId, null, CancellationToken.None);
            setup.MenuEngineeringEnabled = false;
            await f.Service.SaveSetupAsync(f.OrgId, f.UserId, f.AdminRoleId, setup, CancellationToken.None);

            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                f.Service.GetWorkspaceAsync(f.OrgId, f.UserId, f.AdminRoleId, null, null, CancellationToken.None));

            // Existing inventory/procurement data remains fully readable
            List<InventoryBalance> balances = await f.Db.Context.InventoryBalance.Where(x => x.BuyerId == f.Buyer.Id).ToListAsync();
            Assert.Single(balances);
        }
    }
}
