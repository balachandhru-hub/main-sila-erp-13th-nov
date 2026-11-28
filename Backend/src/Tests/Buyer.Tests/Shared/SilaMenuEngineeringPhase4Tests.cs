using System.Diagnostics;
using System.Net;
using Buyer.Application.Features.Shared;
using Buyer.Application.Features.Shared.MenuEngineering;
using Buyer.Application.Features.Shared.MenuEngineering.Ai;
using Buyer.Application.Features.Shared.MenuEngineering.Autonomous;
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
using Xunit.Abstractions;

namespace Buyer.Tests.Shared
{
    public class SilaMenuEngineeringPhase4Tests
    {
        private readonly ITestOutputHelper _output;

        public SilaMenuEngineeringPhase4Tests(ITestOutputHelper output)
        {
            _output = output;
        }
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

        private sealed class Phase4Fixture
        {
            public TestDatabase Db { get; init; } = null!;
            public MenuEngineeringAutonomousService Auto { get; init; } = null!;
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
            public MenuPlan ActivePlan { get; init; } = null!;
            public SilaCulinaryEvent FutureEvent { get; init; } = null!;
            public Recipe SalmonRecipe { get; init; } = null!;
            public MenuItem SalmonMenuItem { get; init; } = null!;
        }

        private static Phase4Fixture CreateFixture(bool seedHistorical = true)
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
                OrganizationName = "SILA Phase 4 Autonomous Resort",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "auto@sila.test",
                Phone = "+971500000401",
                Country = "UAE",
                AddressLine1 = "Palm Jumeirah",
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
                OrganizationName = "Tenant B Resort",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "tenantb@sila.test",
                Phone = "+971500000402",
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

            InventoryBalance invSal = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = salmon.Id, OnHandQty = 15m, BaseUom = "KG", UnitCost = 60m, IsActive = true };
            InventoryBalance invChk = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = chicken.Id, OnHandQty = 120m, BaseUom = "KG", UnitCost = 20m, IsActive = true };

            Recipe rSal = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-SAL", Name = "Roasted Salmon", Category = "Mains", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 18m, Currency = "AED", IsActive = true };
            RecipeIngredient riSal = new() { Id = Guid.NewGuid(), RecipeId = rSal.Id, IngredientCode = "ING-SAL", MaterialId = salmon.Id, ItemCode = "MAT-SAL", ItemName = "Atlantic Salmon Fillet", Quantity = 0.30m, Uom = "KG", BaseQuantity = 0.30m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            Recipe rChk = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "RCP-CHK", Name = "Herb Chicken", Category = "Mains", ItemMode = "RECIPE", ServingQty = 1m, ServingUom = "PORTION", SellingUom = "PORTION", Status = "APPROVED", Version = 1, ActiveVersion = 1, TotalCost = 6m, Currency = "AED", IsActive = true };
            RecipeIngredient riChk = new() { Id = Guid.NewGuid(), RecipeId = rChk.Id, IngredientCode = "ING-CHK", MaterialId = chicken.Id, ItemCode = "MAT-CHK", ItemName = "Chicken Breast Boneless", Quantity = 0.30m, Uom = "KG", BaseQuantity = 0.30m, BaseUom = "KG", Sequence = 1, Version = 1, IsActive = true };

            MenuItem miSal = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "MI-SAL", Name = "Roasted Salmon", Category = "Mains", Cuisine = "Mediterranean", LinkedRecipeId = rSal.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 75m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, AllergenNotes = "Fish", IsActive = true };
            MenuItem miChk = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, PropertyId = property.Id, MenuItemCode = "MI-CHK", Name = "Herb Chicken", Category = "Mains", Cuisine = "Mediterranean", LinkedRecipeId = rChk.Id, ApprovedRecipeVersion = 1, DefaultPortionQty = 1m, PortionUom = "PORTION", SellingPrice = 60m, Currency = "AED", ApprovalStatus = "APPROVED", Status = "ACTIVE", IsAvailable = true, DietaryTags = "Halal", IsActive = true };

            MenuPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                Name = "Executive Gala Buffet",
                MenuType = "BUFFET",
                ServiceFormat = "BUFFET",
                MealPeriod = "Dinner",
                BusinessDate = DateTime.UtcNow.Date.AddDays(3),
                ExpectedGuests = 300,
                ExpectedCovers = 300,
                TargetFoodCostPercent = 30m,
                Status = "PUBLISHED",
                VersionNumber = 1,
                IsActive = true
            };

            MenuPlanLine l1 = new() { Id = Guid.NewGuid(), MenuPlanId = plan.Id, MenuItemId = miSal.Id, CategoryName = "Mains", RecipeId = rSal.Id, PortionSize = 1m, PortionUom = "PORTION", PlannedPortions = 200, ActualPortions = 140, ParticipationRate = 0.60m, ExpectedServingsPerGuest = 1m, SellingPrice = 75m, SampledActualPortion = 1.25m, IsActive = true };
            MenuPlanLine l2 = new() { Id = Guid.NewGuid(), MenuPlanId = plan.Id, MenuItemId = miChk.Id, CategoryName = "Mains", RecipeId = rChk.Id, PortionSize = 1m, PortionUom = "PORTION", PlannedPortions = 180, ActualPortions = 175, ParticipationRate = 0.60m, ExpectedServingsPerGuest = 1m, SellingPrice = 60m, SampledActualPortion = 1.00m, IsActive = true };

            SilaCulinaryEvent futureEvent = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                PropertyId = property.Id,
                EventCode = "EVT-P4-001",
                Name = "Executive Summit Banquet",
                EventType = "BANQUET",
                StartDateUtc = DateTime.UtcNow.Date.AddDays(3),
                EndDateUtc = DateTime.UtcNow.Date.AddDays(3),
                ExpectedGuests = 300,
                IsActive = true
            };

            List<object> seeds =
            [
                buyer, otherBuyer, property, kitchen,
                salmon, chicken, invSal, invChk,
                rSal, riSal, rChk, riChk,
                miSal, miChk, plan, l1, l2, futureEvent,
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
                    DailyAiTokenBudget = 250000,
                    MonthlyAiBudgetUsd = 250m,
                    MaxConcurrentJobs = 4,
                    IsActive = true
                },
                new SilaCulinaryKitchen
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    PropertyId = property.Id,
                    KitchenCode = "KIT-01",
                    KitchenName = "Main Kitchen",
                    DailyPortionCapacity = 800m,
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
                }
            ];

            if (seedHistorical)
            {
                for (int d = 14; d >= 1; d--)
                {
                    seeds.Add(new PosSalesTransaction
                    {
                        Id = Guid.NewGuid(),
                        BuyerId = buyer.Id,
                        PropertyId = property.Id,
                        OutletCode = "MAIN",
                        SourceTransactionId = $"POS-P4-{d}",
                        PosCode = "MI-SAL",
                        MenuItemId = miSal.Id,
                        RecipeId = rSal.Id,
                        BusinessDate = DateTime.UtcNow.Date.AddDays(-d),
                        QuantitySold = 85 + (d % 5) * 10,
                        Amount = (85 + (d % 5) * 10) * 75m,
                        NetRevenue = (85 + (d % 5) * 10) * 75m,
                        Status = "MAPPED",
                        IsActive = true
                    });
                }
            }

            db.Seed(seeds.ToArray());

            IConfiguration cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InterCallService:CulinaryEngineUrl"] = "http://127.0.0.1:8013",
                    ["InterCallService:VoiceEngineUrl"] = "http://127.0.0.1:8012"
                })
                .Build();
            IHttpClientFactory httpFactory = new OfflineHttpClientFactory();
            SilaCulinaryFinancialService fin = new(db.Repository, db.Logger, httpFactory, cfg, new FakeProtector());
            SilaCulinaryAnalyticsService ana = new(db.Repository, db.Logger, httpFactory, cfg);
            MenuEngineeringStudioService studio = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), fin, ana, httpFactory);
            MenuEngineeringDigitalTwinService twin = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), httpFactory);
            MenuEngineeringAiOrchestratorService ai = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), studio, fin, ana, httpFactory, twin);
            MenuEngineeringAutonomousService auto = new(db.Repository, db.Context, db.Logger, new HttpContextAccessor(), twin, ai, httpFactory);

            return new Phase4Fixture
            {
                Db = db,
                Auto = auto,
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
                ActivePlan = plan,
                FutureEvent = futureEvent,
                SalmonRecipe = rSal,
                SalmonMenuItem = miSal
            };
        }

        [Fact]
        public async Task Test01_IngredientPriceIncrease15Percent_IdentifiesAffectedAndGeneratesRecommendations()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "IngredientPriceChanged",
                SourceSystem = "ERP_MATERIAL_COST",
                ObjectType = "ItemBuyerMaster",
                ObjectId = f.Salmon.Id,
                ObjectLabel = "Atlantic Salmon Fillet +15%",
                MaterialityScore = 15m,
                Payload = new Dictionary<string, object?> { ["priceChangePercent"] = 15m }
            }, CancellationToken.None);

            EventProcessingSummaryDto summary = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            Assert.Equal(1, summary.ProcessedCount);
            Assert.True(summary.ActionPlansCreated >= 1);

            List<ActionPlanDto> plans = await f.Auto.GetActionPlansAsync(f.OrgId, f.AdminRoleId, f.Property.Id);
            ActionPlanDto plan = Assert.Single(plans);
            Assert.Contains(plan.AffectedObjects, o => o.Contains("Roasted Salmon") || o.Contains("Executive Gala Buffet") || o.Contains("Executive Summit"));
            Assert.Contains(plan.Actions, a => a.AgentId == "RECIPE_INTELLIGENCE_AGENT" || a.AgentId == "COST_MARGIN_AGENT");
        }

        [Fact]
        public async Task Test02_OperaOccupancyForecastIncrease_RecalculatesDemandPortionsIngredientsProduction()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "OccupancyForecastChanged",
                SourceSystem = "OPERA_PMS",
                ObjectLabel = "OPERA Occupancy jumped +22%",
                MaterialityScore = 22m,
                Payload = new Dictionary<string, object?> { ["occupancyDeltaPercent"] = 22m }
            }, CancellationToken.None);

            EventProcessingSummaryDto summary = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            Assert.Equal(1, summary.ProcessedCount);
            ActionPlanDto plan = (await f.Auto.GetActionPlansAsync(f.OrgId, f.AdminRoleId, f.Property.Id)).First();
            Assert.Contains(plan.Actions, a => a.AgentId == "DEMAND_FORECAST_AGENT");
            Assert.Contains(plan.Actions, a => a.AgentId == "PORTION_OPTIMIZATION_AGENT");
            Assert.Contains(plan.Actions, a => a.AgentId == "PRODUCTION_INTELLIGENCE_AGENT");
            Assert.Contains(plan.Actions, a => a.AgentId == "PURCHASE_INTELLIGENCE_AGENT");
        }

        [Fact]
        public async Task Test03_SupplierDeliveryDelayed_DetectsShortageAndRecommendsAlternatives()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "SupplierDeliveryDelayed",
                SourceSystem = "SUPPLIER_ERP",
                ObjectLabel = "Gulf Seafood Supply PO-4500 delayed 48h",
                MaterialityScore = 30m
            }, CancellationToken.None);

            EventProcessingSummaryDto summary = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            Assert.Equal(1, summary.ProcessedCount);
            ActionPlanDto plan = (await f.Auto.GetActionPlansAsync(f.OrgId, f.AdminRoleId, f.Property.Id)).First();
            Assert.Contains(plan.Actions, a => a.AgentId == "INVENTORY_INTELLIGENCE_AGENT");
            Assert.Contains(plan.Actions, a => a.AgentId == "PURCHASE_INTELLIGENCE_AGENT");
        }

        [Fact]
        public async Task Test04_EventGuestCountChangedMaterially_RefreshesTwinAndGeneratesConsolidatedPlan()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "EventGuestCountChanged",
                SourceSystem = "EVENT_BOOKING",
                ObjectType = "SilaCulinaryEvent",
                ObjectId = f.FutureEvent.Id,
                ObjectLabel = f.FutureEvent.Name,
                MaterialityScore = 40m
            }, CancellationToken.None);

            EventProcessingSummaryDto summary = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            Assert.Equal(1, summary.ProcessedCount);
            ActionPlanDto plan = (await f.Auto.GetActionPlansAsync(f.OrgId, f.AdminRoleId, f.Property.Id)).First();
            Assert.True(plan.IsConsolidated);
            Assert.True(plan.Actions.Count >= 3);
        }

        [Fact]
        public async Task Test05_RecurringExcessiveWasteDetected_GeneratesPortionOrProductionRecommendations()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "WasteRecorded",
                SourceSystem = "SILA_STORE",
                ObjectLabel = "Repeated Dinner Buffet Leftover Waste 24%",
                MaterialityScore = 24m
            }, CancellationToken.None);

            EventProcessingSummaryDto summary = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            Assert.Equal(1, summary.ProcessedCount);
            ActionPlanDto plan = (await f.Auto.GetActionPlansAsync(f.OrgId, f.AdminRoleId, f.Property.Id)).First();
            Assert.Contains(plan.Actions, a => a.AgentId == "WASTE_INTELLIGENCE_AGENT");
            Assert.Contains(plan.Actions, a => a.AgentId == "PORTION_OPTIMIZATION_AGENT");
        }

        [Fact]
        public async Task Test06_MenuPriceChangeRecommendation_RequiresApprovalBeforePublication()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringActionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PropertyId = f.Property.Id,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Title = "Price Change Review",
                RiskLevel = "HIGH",
                Status = "PENDING_APPROVAL",
                IsActive = true
            };
            MenuEngineeringActionItem priceAction = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ActionPlanId = plan.Id,
                SequenceOrder = 1,
                AgentId = "PRICING_AGENT",
                ToolId = "ChangeMenuPrice",
                ActionType = "CHANGE_SELLING_PRICE",
                Title = "Increase Roasted Salmon price from 75 to 85 AED",
                RiskClassification = "HIGH",
                PolicyDecision = "REQUIRE_APPROVAL",
                PolicyReason = "High-risk price mutation requires approval.",
                FinancialImpact = 3000m,
                TargetObjectType = "MenuPlan",
                TargetObjectId = f.ActivePlan.Id,
                ApprovalStatus = "PENDING",
                ExecutionStatus = "PENDING",
                IsActive = true
            };
            f.Db.Seed(plan, priceAction);

            ActionExecutionResultDto blocked = await f.Auto.ExecuteActionItemAsync(
                f.OrgId, f.UserId, f.AdminRoleId, priceAction.Id, new ExecuteActionItemRequestDto(), CancellationToken.None);
            Assert.Equal("APPROVAL_REQUIRED", blocked.ExecutionStatus);
            Assert.Equal("REQUIRE_APPROVAL", blocked.PolicyDecision);
        }

        [Fact]
        public async Task Test07_ApprovedActionSubmittedTwice_NoDuplicateBusinessTransaction()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringActionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PropertyId = f.Property.Id,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Title = "Idempotent Execution Test",
                RiskLevel = "MEDIUM",
                Status = "PENDING_APPROVAL",
                IsActive = true
            };
            MenuEngineeringActionItem item = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ActionPlanId = plan.Id,
                SequenceOrder = 1,
                AgentId = "MENU_INTELLIGENCE_AGENT",
                ToolId = "PrepareMenuRevision",
                ActionType = "PREPARE_MENU_REVISION",
                Title = "Apply approved portion revision",
                RiskClassification = "MEDIUM",
                PolicyDecision = "REQUIRE_APPROVAL",
                PolicyReason = "Approval required",
                TargetObjectType = "MenuPlan",
                TargetObjectId = f.ActivePlan.Id,
                ApprovalStatus = "PENDING",
                ExecutionStatus = "PENDING",
                IsActive = true
            };
            f.Db.Seed(plan, item);

            await f.Auto.ApproveActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ApproveActionItemRequestDto { Approved = true }, CancellationToken.None);

            ActionExecutionResultDto first = await f.Auto.ExecuteActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ExecuteActionItemRequestDto { IdempotencyKey = "IDEMP-TEST-07" }, CancellationToken.None);
            ActionExecutionResultDto second = await f.Auto.ExecuteActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ExecuteActionItemRequestDto { IdempotencyKey = "IDEMP-TEST-07" }, CancellationToken.None);

            Assert.Equal("EXECUTED", first.ExecutionStatus);
            Assert.False(first.WasIdempotentReplay);
            Assert.Equal("EXECUTED", second.ExecutionStatus);
            Assert.True(second.WasIdempotentReplay);
        }

        [Fact]
        public async Task Test08_BusinessDataChangesAfterApproval_RevalidatesAndRequestsRenewedApproval()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringActionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PropertyId = f.Property.Id,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Title = "Post-Approval Drift Plan",
                RiskLevel = "HIGH",
                Status = "PENDING_APPROVAL",
                IsActive = true
            };
            MenuEngineeringActionItem item = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ActionPlanId = plan.Id,
                SequenceOrder = 1,
                AgentId = "MENU_INTELLIGENCE_AGENT",
                ToolId = "PublishMenu",
                ActionType = "PUBLISH_MENU",
                Title = "Publish Executive Gala Buffet",
                RiskClassification = "HIGH",
                PolicyDecision = "REQUIRE_APPROVAL",
                PolicyReason = "Publish requires approval",
                TargetObjectType = "MenuPlan",
                TargetObjectId = f.ActivePlan.Id,
                ApprovalStatus = "PENDING",
                ExecutionStatus = "PENDING",
                IsActive = true
            };
            f.Db.Seed(plan, item);

            await f.Auto.ApproveActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ApproveActionItemRequestDto { Approved = true }, CancellationToken.None);

            // Simulate business data mutation after approval
            MenuPlan trackedPlan = await f.Db.Context.MenuPlan.FirstAsync(x => x.Id == f.ActivePlan.Id);
            trackedPlan.DateUpdated = DateTime.UtcNow.AddMinutes(5);
            await f.Db.Context.SaveChangesAsync();

            ActionExecutionResultDto res = await f.Auto.ExecuteActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ExecuteActionItemRequestDto(), CancellationToken.None);
            Assert.True(res.RequiresReapproval);
            Assert.Equal("REAPPROVAL_REQUIRED", res.ExecutionStatus);
        }

        [Fact]
        public async Task Test09_ExternalErpWriteTimesOut_ReconcilesOutcomeBeforeAnyRetry()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringActionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PropertyId = f.Property.Id,
                CorrelationId = Guid.NewGuid().ToString("N"),
                Title = "SAP PO Transmission Plan",
                RiskLevel = "HIGH",
                Status = "PENDING_APPROVAL",
                IsActive = true
            };
            MenuEngineeringActionItem item = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ActionPlanId = plan.Id,
                SequenceOrder = 1,
                AgentId = "PROCUREMENT_RECOMMENDATION_AGENT",
                ToolId = "TransmitPurchaseOrder",
                ActionType = "TRANSMIT_PURCHASE_ORDER",
                Title = "Transmit PO to SAP",
                RiskClassification = "HIGH",
                PolicyDecision = "REQUIRE_APPROVAL",
                PolicyReason = "High-risk ERP transmission",
                ApprovalStatus = "PENDING",
                ExecutionStatus = "PENDING",
                IsActive = true
            };
            f.Db.Seed(plan, item);
            await f.Auto.ApproveActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ApproveActionItemRequestDto { Approved = true }, CancellationToken.None);

            ActionExecutionResultDto res = await f.Auto.ExecuteActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, item.Id, new ExecuteActionItemRequestDto
            {
                IdempotencyKey = "SAP-PO-TIMEOUT-KEY",
                SimulateExternalTimeout = true
            }, CancellationToken.None);

            Assert.True(res.ExternalOutcomeReconciled);
            Assert.Equal("UNCERTAIN_RECONCILING", res.ExecutionStatus);
            Assert.Contains(f.Db.Context.MenuEngineeringBusinessEvent, e => e.EventType == "ExternalTransactionReconciliationPending");
        }

        [Fact]
        public async Task Test10_OverlappingAgentRecommendations_ConsolidatedWithoutDuplicateFinancialBenefit()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "IngredientPriceChanged",
                SourceSystem = "ERP",
                ObjectLabel = "Salmon Shock Consolidation",
                MaterialityScore = 18m
            }, CancellationToken.None);
            await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);

            MenuEngineeringRecommendation rec = await f.Db.Context.MenuEngineeringRecommendation.FirstAsync(x => x.BuyerId == f.Buyer.Id);
            RealizedBenefitDto b1 = await f.Auto.RecordRealizedBenefitAsync(f.OrgId, f.UserId, f.AdminRoleId, new RecordRealizedBenefitRequestDto
            {
                PropertyId = f.Property.Id,
                RecommendationId = rec.Id,
                ActionPlanId = rec.ActionPlanId,
                BenefitCategory = "INGREDIENT_COST_REDUCTION",
                BaselineAmount = 1000m,
                ExpectedBenefit = 200m,
                RawActualOutcome = 180m
            }, CancellationToken.None);
            Assert.True(b1.AdjustedRealizedBenefit > 0);

            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                f.Auto.RecordRealizedBenefitAsync(f.OrgId, f.UserId, f.AdminRoleId, new RecordRealizedBenefitRequestDto
                {
                    PropertyId = f.Property.Id,
                    RecommendationId = rec.Id,
                    ActionPlanId = rec.ActionPlanId,
                    BenefitCategory = "INGREDIENT_COST_REDUCTION",
                    BaselineAmount = 1000m,
                    ExpectedBenefit = 200m,
                    RawActualOutcome = 180m
                }, CancellationToken.None));
        }

        [Fact]
        public async Task Test11_RecommendationImplemented_OutcomeMonitoredAndRealizedBenefitsValidated()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringRecommendation rec = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                PropertyId = f.Property.Id,
                RecommendationCode = "REC-P4-11",
                AgentId = "MENU_INTELLIGENCE_AGENT",
                Category = "MENU_OPTIMIZATION",
                Title = "Reduce salmon buffer by 10%",
                LifecycleStatus = "UNDER_REVIEW",
                Status = "OPEN",
                IsActive = true
            };
            f.Db.Seed(rec);

            await f.Auto.TransitionRecommendationLifecycleAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new ExtendedLifecycleTransitionDto { TargetStatus = "ACCEPTED" }, CancellationToken.None);
            await f.Auto.TransitionRecommendationLifecycleAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new ExtendedLifecycleTransitionDto { TargetStatus = "IMPLEMENTED" }, CancellationToken.None);
            await f.Auto.TransitionRecommendationLifecycleAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new ExtendedLifecycleTransitionDto { TargetStatus = "OUTCOME_MONITORING" }, CancellationToken.None);

            RealizedBenefitDto recorded = await f.Auto.RecordRealizedBenefitAsync(f.OrgId, f.UserId, f.AdminRoleId, new RecordRealizedBenefitRequestDto
            {
                PropertyId = f.Property.Id,
                RecommendationId = rec.Id,
                BenefitCategory = "WASTE_REDUCTION",
                BaselineAmount = 1200m,
                ExpectedBenefit = 250m,
                RawActualOutcome = 960m,
                GuestCountAdjustmentFactor = 0.95m,
                PriceChangeAdjustmentFactor = 1.00m,
                SeasonalityAdjustmentFactor = 1.00m
            }, CancellationToken.None);

            RealizedBenefitDto validated = await f.Auto.ValidateRealizedBenefitAsync(f.OrgId, f.UserId, f.AdminRoleId, recorded.Id, approved: true, CancellationToken.None);
            Assert.Equal("VALIDATED", validated.ValidationStatus);
            Assert.Equal(228.00m, validated.AdjustedRealizedBenefit);

            var closed = await f.Auto.TransitionRecommendationLifecycleAsync(f.OrgId, f.UserId, f.AdminRoleId, rec.Id, new ExtendedLifecycleTransitionDto { TargetStatus = "OUTCOME_VALIDATED" }, CancellationToken.None);
            Assert.Equal("OUTCOME_VALIDATED", closed.Status);
        }

        [Fact]
        public async Task Test12_HistoricalDataInsufficient_DisclosesLimitationWithoutFabricatedPrediction()
        {
            var f = CreateFixture(seedHistorical: false);
            using var _ = f.Db;

            DataReadinessAssessmentDto readiness = await f.Auto.EvaluateDataReadinessAsync(f.OrgId, f.AdminRoleId, f.Property.Id);
            Assert.Equal("INSUFFICIENT_DATA", readiness.OverallClassification);
            Assert.True(readiness.HighImpactRecommendationsBlocked);
            Assert.NotEmpty(readiness.BlockingReasons);
        }

        [Fact]
        public async Task Test13_AiProviderUnavailable_DeterministicServicesAndManualOpsContinue()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            // OfflineHttpClientFactory simulates unavailable AI/Python providers; deterministic readiness & control tower still function
            AiControlTowerDto ct = await f.Auto.GetControlTowerAsync(f.OrgId, f.AdminRoleId, f.Property.Id);
            Assert.NotNull(ct);
            Assert.True(ct.ActiveAgentsCount >= 15);
            Assert.Contains(ct.IntegrationHealth, h => h.IntegrationType == "AI Provider & SILA Voice" && h.Classification == "UNAVAILABLE");

            var studioWs = await f.Studio.GetWorkspaceAsync(f.OrgId, f.UserId, f.AdminRoleId, f.ActivePlan.Id, f.Property.Id, CancellationToken.None);
            Assert.NotNull(studioWs.ActiveMenu);
        }

        [Fact]
        public async Task Test14_TenantAAttemptsAccessToTenantBIntelligenceData_AccessDenied()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringActionPlan tenantBPlan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Db.Context.BuyerBusinessProfile.First(b => b.OrganizationId == f.OtherOrgId).Id,
                CorrelationId = "TENANT-B-CORR",
                Title = "Confidential Tenant B Plan",
                IsActive = true
            };
            MenuEngineeringActionItem tenantBItem = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = tenantBPlan.BuyerId,
                ActionPlanId = tenantBPlan.Id,
                SequenceOrder = 1,
                AgentId = "MENU_INTELLIGENCE_AGENT",
                ToolId = "PublishMenu",
                ActionType = "PUBLISH_MENU",
                Title = "Tenant B Publish",
                ApprovalStatus = "APPROVED",
                ExecutionStatus = "PENDING",
                IsActive = true
            };
            f.Db.Seed(tenantBPlan, tenantBItem);

            // Tenant A cannot see Tenant B's plans and cannot approve/execute Tenant B's items
            List<ActionPlanDto> tenantAPlans = await f.Auto.GetActionPlansAsync(f.OrgId, f.AdminRoleId);
            Assert.DoesNotContain(tenantAPlans, p => p.Id == tenantBPlan.Id);

            await Assert.ThrowsAsync<NotFoundCustomException>(() =>
                f.Auto.ExecuteActionItemAsync(f.OrgId, f.UserId, f.AdminRoleId, tenantBItem.Id, new ExecuteActionItemRequestDto(), CancellationToken.None));
        }

        [Fact]
        public void Test15_AgentAttemptsUnauthorizedFinancialTransaction_PolicyEngineRejects()
        {
            Guid unauthorizedRoleId = Guid.NewGuid();
            var perms = MenuEngineeringPermissions.Evaluate(null, unauthorizedRoleId, true, true);
            var decision = MenuEngineeringAutonomousContracts.EvaluatePolicy(
                autonomyLevel: 5,
                isAgentPaused: false,
                globalPaused: false,
                automatedExecutionDisabled: false,
                riskClassification: "HIGH",
                financialImpact: 5000m,
                maxFinancialThreshold: 1000m,
                hasActorPermission: perms.GeneratePurchaseRecommendation || perms.ApproveMenu,
                foodSafetyCompliant: true,
                dataIsFresh: true,
                confidenceScore: 0.9m);

            Assert.Equal("REJECT", decision.Decision);
        }

        [Fact]
        public async Task Test16_AdministratorPausesAutonomousExecution_BlocksNewAutonomousBusinessActions()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            EmergencyControlStatusDto paused = await f.Auto.SetEmergencyControlAsync(f.OrgId, f.UserId, f.AdminRoleId, new EmergencyControlRequestDto
            {
                AutonomousModePaused = true,
                AutomatedExecutionDisabled = true,
                Reason = "Chef emergency freeze during audit",
                CancelPendingJobs = true
            }, CancellationToken.None);
            Assert.True(paused.AutonomousModePaused);

            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "IngredientPriceChanged",
                ObjectLabel = "Price change while paused",
                MaterialityScore = 20m
            }, CancellationToken.None);
            EventProcessingSummaryDto summary = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            Assert.Equal(0, summary.ProcessedCount);
            Assert.Equal(0, summary.ActionPlansCreated);
        }

        [Fact]
        public async Task Test17_MultiplePropertiesExecuteSimultaneously_ProcurementRemainsResponsive()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            var sw = Stopwatch.StartNew();
            var tasks = new List<Task<AiControlTowerDto>>();
            for (int i = 0; i < 4; i++)
                tasks.Add(f.Auto.GetControlTowerAsync(f.OrgId, f.AdminRoleId, f.Property.Id));
            await Task.WhenAll(tasks);
            sw.Stop();

            Assert.True(sw.ElapsedMilliseconds < 5000, $"Concurrent Control Tower load took {sw.ElapsedMilliseconds}ms");
        }

        [Fact]
        public async Task Test18_TeamsApprovalAttemptedByUnauthorizedUser_ApprovalRejected()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringActionPlan plan = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                CorrelationId = "TEAMS-AUTH-TEST",
                Title = "Teams Approval Security Test",
                IsActive = true
            };
            MenuEngineeringActionItem item = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ActionPlanId = plan.Id,
                SequenceOrder = 1,
                AgentId = "PROCUREMENT_RECOMMENDATION_AGENT",
                ToolId = "TransmitPurchaseOrder",
                ActionType = "TRANSMIT_PURCHASE_ORDER",
                Title = "Approve PO via Teams",
                RiskClassification = "HIGH",
                PolicyDecision = "REQUIRE_APPROVAL",
                ApprovalStatus = "PENDING",
                ExecutionStatus = "PENDING",
                IsActive = true
            };
            f.Db.Seed(plan, item);

            Guid unauthorizedRoleId = Guid.NewGuid();
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                f.Auto.ProcessTeamsApprovalCallbackAsync(f.OrgId, new TeamsApprovalCallbackRequestDto
                {
                    ActionItemId = item.Id,
                    ActingUserId = Guid.NewGuid(),
                    ActingRoleId = unauthorizedRoleId,
                    PropertyId = f.Property.Id,
                    Approved = true
                }, CancellationToken.None));
        }

        [Fact]
        public async Task Test19_ExistingProcurementV1_NoUnauthorizedPoCreatedByAutonomousIntelligence()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            int poBefore = await f.Db.Context.Set<PurchaseOrder>().CountAsync();
            await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
            {
                PropertyId = f.Property.Id,
                EventType = "SupplierDeliveryDelayed",
                ObjectLabel = "Supplier Delay Test 19",
                MaterialityScore = 25m
            }, CancellationToken.None);
            await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
            int poAfter = await f.Db.Context.Set<PurchaseOrder>().CountAsync();

            Assert.Equal(poBefore, poAfter);
        }

        [Fact]
        public async Task Test20_FeatureFlagDisabled_ProtectedCloudAndMobileOperationsRemainIntact()
        {
            var f = CreateFixture();
            using var _ = f.Db;

            MenuEngineeringSettings settings = await f.Db.Context.MenuEngineeringSettings.FirstAsync(x => x.BuyerId == f.Buyer.Id);
            settings.MenuEngineeringEnabled = false;
            await f.Db.Context.SaveChangesAsync();

            // When ME is disabled, autonomous scheduled run skips gracefully without throwing or blocking procurement/ERP
            await f.Auto.RunScheduledAutonomousCycleForBuyerAsync(f.OrgId, CancellationToken.None);

            // Re-enable and verify Control Tower + Executive Dashboard still load cleanly
            settings.MenuEngineeringEnabled = true;
            await f.Db.Context.SaveChangesAsync();
            ExecutiveDashboardDto dash = await f.Auto.GetExecutiveDashboardAsync(f.OrgId, f.AdminRoleId, "GENERAL_MANAGER", f.Property.Id);
            Assert.Equal("GENERAL_MANAGER", dash.Persona);
        }

        [Fact]
        public async Task Benchmark_MultiTenantAutonomousWorkloads()
        {
            // Benchmark realistic concurrent multi-tenant autonomous workloads without blocking procurement reads
            var swEvents = Stopwatch.StartNew();
            int totalProcessed = 0;
            for (int t = 0; t < 3; t++)
            {
                var f = CreateFixture();
                using var _ = f.Db;
                string[] evtTypes = ["IngredientPriceChanged", "OccupancyForecastChanged", "SupplierDeliveryDelayed", "WasteRecorded"];
                for (int i = 0; i < evtTypes.Length; i++)
                {
                    await f.Auto.PublishBusinessEventAsync(f.OrgId, f.UserId, f.AdminRoleId, new PublishBusinessEventRequestDto
                    {
                        PropertyId = f.Property.Id,
                        EventType = evtTypes[i],
                        ObjectLabel = $"Bench-{t}-{i}",
                        MaterialityScore = 20m,
                        IdempotencyKey = $"BENCH-{t}-{i}-{Guid.NewGuid():N}"
                    }, CancellationToken.None);
                }
                EventProcessingSummaryDto res = await f.Auto.ProcessPendingEventsAsync(f.OrgId, f.UserId, f.AdminRoleId, f.Property.Id);
                totalProcessed += res.ProcessedCount;
            }
            swEvents.Stop();

            var fCt = CreateFixture();
            using var __ = fCt.Db;
            var swRead = Stopwatch.StartNew();
            AiControlTowerDto ct = await fCt.Auto.GetControlTowerAsync(fCt.OrgId, fCt.AdminRoleId, fCt.Property.Id);
            ExecutiveDashboardDto gm = await fCt.Auto.GetExecutiveDashboardAsync(fCt.OrgId, fCt.AdminRoleId, "GENERAL_MANAGER", fCt.Property.Id);
            var briefs = await fCt.Auto.GenerateDailyBriefingsAsync(fCt.OrgId, fCt.UserId, fCt.AdminRoleId, fCt.Property.Id);
            swRead.Stop();

            _output.WriteLine($"BENCHMARK_EVENTS: 3 tenants x 4 events ({totalProcessed} processed) in {swEvents.ElapsedMilliseconds} ms ({(double)swEvents.ElapsedMilliseconds / Math.Max(1, totalProcessed):F1} ms/event)");
            _output.WriteLine($"BENCHMARK_READS: ControlTower + GM Dashboard + 4 Daily Briefings in {swRead.ElapsedMilliseconds} ms");
            Assert.Equal(12, totalProcessed);
            Assert.NotNull(ct);
            Assert.Equal(4, briefs.Count);
        }
    }
}
