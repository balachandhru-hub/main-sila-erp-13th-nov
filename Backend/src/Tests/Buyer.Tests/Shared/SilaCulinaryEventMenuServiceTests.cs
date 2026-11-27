using System.Net;
using System.Text;
using System.Text.Json;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaCulinaryEventMenuServiceTests
    {
        private sealed class FakeProtector : IIntegrationCredentialProtector
        {
            public string? Protect(string? value) => value == null ? null : "ENC:" + value;
            public string? Unprotect(string? value) => value != null && value.StartsWith("ENC:") ? value[4..] : value;
            public string State(SharedKernel.Integration.Entities.ApiIntegrationConfiguration configuration) => "Configured";
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }

        private sealed class FakeHttpClientFactory : IHttpClientFactory
        {
            public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler(), disposeHandler: false);
        }

        private static IConfiguration BuildConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InterCallService:CulinaryEngineUrl"] = "http://127.0.0.1:8013"
                })
                .Build();

        private static (TestDatabase Db, SilaCulinaryEventMenuService Service, BuyerBusinessProfile Buyer, BuyerProperty Property, InventoryLocation Kitchen) CreateFixture()
        {
            TestDatabase db = new();
            Guid orgId = Guid.NewGuid();
            Guid buyerId = Guid.NewGuid();
            BuyerBusinessProfile buyer = new()
            {
                Id = buyerId,
                OrganizationId = orgId,
                OrganizationName = "SILA Luxury Hotel Group",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "chef@sila.test",
                Phone = "+971500000001",
                Country = "UAE",
                AddressLine1 = "Palm",
                City = "Dubai",
                State = "Dubai",
                PinCode = "00000",
                Industry = "Hospitality",
                BusinessType = "Resort",
                Currency = "AED",
                Status = "ACTIVE",
                IsActive = true
            };
            BuyerProperty property = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                CompanyCode = "1000",
                PlantCode = "1060",
                PropertyName = "Grand Resort Dubai",
                IsActive = true
            };
            InventoryLocation kitchen = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                PropertyId = property.Id,
                LocationCode = "KIT-MAIN",
                LocationName = "Main Banquet Kitchen",
                LocationType = "KITCHEN",
                StorageLocationCode = "1060",
                InventoryEnabled = true,
                ConsumptionEnabled = true,
                IsActive = true
            };
            db.Seed(buyer, property, kitchen);
            SilaCulinaryEventMenuService svc = new(db.Repository, db.Logger, new FakeHttpClientFactory(), BuildConfig(), new FakeProtector());
            return (db, svc, buyer, property, kitchen);
        }

        private static Guid SeedPublishedRecipe(
            TestDatabase db, Guid buyerId, string code, string name, string category, decimal costPerPortion,
            string[] dietaryTags, string[] allergens, bool allergenUnknown = false, Guid? materialId = null)
        {
            Guid recipeId = Guid.NewGuid();
            Guid draftId = Guid.NewGuid();
            Recipe recipe = new()
            {
                Id = recipeId,
                BuyerId = buyerId,
                RecipeCode = code,
                Name = name,
                Category = category,
                ItemMode = "RECIPE",
                ServingQty = 10m,
                ServingUom = "PORTION",
                SellingUom = "PORTION",
                Status = "APPROVED",
                Version = 1,
                ActiveVersion = 1,
                TotalCost = costPerPortion * 10m,
                Currency = "AED",
                IsActive = true
            };
            SilaCulinaryRecipeLifecycle lc = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                DraftId = draftId,
                ApprovedDraftRevision = 1,
                ProductionRecipeId = recipeId,
                ProductionRecipeCode = code,
                ProductionRecipeVersion = 1,
                LifecycleStatus = "PUBLISHED",
                IsActive = true
            };
            db.Seed(recipe, lc);
            if (!allergenUnknown)
            {
                db.Seed(new SilaCulinaryRecipeRevision
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    DraftId = draftId,
                    RevisionNumber = 1,
                    PayloadJson = JsonSerializer.Serialize(new
                    {
                        cuisine = "International",
                        dietaryTags,
                        allergens
                    }),
                    IsActive = true
                });
            }
            if (materialId.HasValue)
            {
                db.Seed(new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    RecipeId = recipeId,
                    IngredientCode = "ING001",
                    MaterialId = materialId.Value,
                    ItemCode = "MAT0001",
                    ItemName = "Atlantic Salmon Fillet",
                    Quantity = 1.8m,
                    Uom = "KG",
                    BaseQuantity = 1.8m,
                    BaseUom = "KG",
                    Sequence = 1,
                    Version = 1,
                    IsActive = true
                });
            }
            return recipeId;
        }

        private static readonly Guid AdminRole = Common.BUYER_ADMIN_ROLE_ID;

        [Fact]
        public async Task GrandCorporateGala_IllustrativeScenario_CalculatesExactPortionsAndCost()
        {
            (TestDatabase db, SilaCulinaryEventMenuService svc, BuyerBusinessProfile buyer, BuyerProperty property, InventoryLocation kitchen) = CreateFixture();
            Guid chefId = Guid.NewGuid();
            Guid orgId = buyer.OrganizationId;

            Guid r1 = SeedPublishedRecipe(db, buyer.Id, "RI000001", "Caesar Salad", "Starters", 8m, new[] { "Vegetarian" }, new[] { "Dairy" });
            Guid r2 = SeedPublishedRecipe(db, buyer.Id, "RI000002", "Wild Mushroom Soup", "Soups", 7m, new[] { "Vegetarian" }, new[] { "Dairy" });
            Guid r3 = SeedPublishedRecipe(db, buyer.Id, "RI000003", "Grilled Atlantic Salmon", "Main Courses", 28m, new[] { "Halal" }, new[] { "Fish" });
            Guid r4 = SeedPublishedRecipe(db, buyer.Id, "RI000004", "Butter Chicken", "Main Courses", 12m, new[] { "Halal" }, new[] { "Dairy" });
            Guid r5 = SeedPublishedRecipe(db, buyer.Id, "RI000005", "Vegetable Biryani", "Main Courses", 9m, new[] { "Vegetarian", "Vegan" }, Array.Empty<string>());
            Guid r6 = SeedPublishedRecipe(db, buyer.Id, "RI000006", "Chocolate Mousse", "Desserts", 10m, new[] { "Vegetarian" }, new[] { "Dairy" });
            Guid r7 = SeedPublishedRecipe(db, buyer.Id, "RI000007", "Fresh Fruit Platter", "Desserts", 6m, new[] { "Vegan" }, Array.Empty<string>());
            Guid r8 = SeedPublishedRecipe(db, buyer.Id, "RI000008", "Fresh Juice Selection", "Cold Beverages", 12m, new[] { "Vegan" }, Array.Empty<string>());

            SilaCulinaryEventDto ev = await svc.CreateEventAsync(orgId, chefId, AdminRole, "Chef", new SilaCulinaryUpsertEventRequestDto
            {
                Name = "Grand Corporate Gala",
                EventType = "Corporate Gala",
                PropertyId = property.Id,
                KitchenLocationId = kitchen.Id,
                VenueName = "Grand Ballroom",
                StartDateUtc = new DateTime(2026, 12, 15),
                EndDateUtc = new DateTime(2026, 12, 15),
                GuaranteedGuests = 500,
                AdditionalAllowancePercent = 5m,
                NetPricePerGuest = 350m,
                TargetFoodCostPercent = 28m,
                ServiceStyle = "BUFFET",
                Currency = "AED"
            }, CancellationToken.None);

            Assert.Equal(525, ev.PlanningGuests);
            Assert.Equal(500, ev.BillableGuests);
            Assert.Equal(175000m, ev.EstimatedFoodRevenue);
            Assert.Equal(49000m, ev.CombinedMenuBudget);

            Guid menuId = ev.PrimaryMenuId!.Value;
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Starters", RecipeId = r1, UptakePercent = 45m, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Soups", RecipeId = r2, UptakePercent = 35m, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Main Courses", RecipeId = r3, UptakePercent = 60m, IsLocked = true, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Main Courses", RecipeId = r4, UptakePercent = 65m, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Main Courses", RecipeId = r5, UptakePercent = 35m, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Desserts", RecipeId = r6, UptakePercent = 75m, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Desserts", RecipeId = r7, UptakePercent = 40m, ServiceStyle = "BUFFET" }, CancellationToken.None);
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", menuId, new() { CourseName = "Cold Beverages", RecipeId = r8, UptakePercent = 100m, IsBeverage = true, ServingsPerGuest = 1m, ServingVolumeMl = 200m, ServiceStyle = "BUFFET" }, CancellationToken.None);

            SilaCulinaryEventDto loaded = await svc.GetEventAsync(orgId, chefId, AdminRole, ev.Id, CancellationToken.None);
            Assert.Equal(new[] { 237, 184, 315, 342, 184, 394, 210, 525 }, loaded.PrimaryMenu!.Lines.Select(l => l.PlannedPortions).ToArray());
            Assert.Equal(29264m, loaded.TotalEstimatedMenuCost);
            Assert.Equal(16.72m, loaded.MenuCostPercent);
            Assert.Equal(19736m, loaded.RemainingMenuBudget);

            // Scenario A: Guest simulation 500 -> 750 without mutating baseline
            SilaCulinaryEventSimulationResultDto sim = await svc.SimulateEventAsync(orgId, AdminRole, ev.Id, new SilaCulinaryEventSimulationRequestDto
            {
                GuaranteedGuests = 750,
                AdditionalAllowancePercent = 0m
            }, CancellationToken.None);
            Assert.Equal(750, sim.PlanningGuests);
            Assert.Equal(315, loaded.PrimaryMenu.Lines[2].PlannedPortions);
        }

        [Fact]
        public async Task Approval_Baseline_PreservesRecipeVersion_And_BlocksSelfApproval_And_NoStockDeduction()
        {
            (TestDatabase db, SilaCulinaryEventMenuService svc, BuyerBusinessProfile buyer, BuyerProperty property, InventoryLocation kitchen) = CreateFixture();
            Guid chefId = Guid.NewGuid();
            Guid approverId = Guid.NewGuid();
            Guid orgId = buyer.OrganizationId;

            Guid matId = Guid.NewGuid();
            db.Seed(
                new ItemBuyerMaster { Id = matId, BuyerId = buyer.Id, MaterialCode = "MAT0001", Description = "Salmon", BaseUnitOfMeasure = "KG", MaterialGroup = "SEAFOOD", ProductType = "RAW", OrderUnitOfMeasure = "KG", AlternateUnitOfMeasure = "G", ValuationClass = "3000", UnitOfMeasureMapping = "1:1", IsActive = true },
                new InventoryBalance { Id = Guid.NewGuid(), BuyerId = buyer.Id, LocationId = kitchen.Id, MaterialId = matId, OnHandQty = 100m, BaseUom = "KG", IsActive = true }
            );

            Guid rSalmon = SeedPublishedRecipe(db, buyer.Id, "RI000010", "Grilled Salmon", "Main Courses", 28m, new[] { "Halal" }, new[] { "Fish" }, false, matId);
            SilaCulinaryEventDto ev = await svc.CreateEventAsync(orgId, chefId, AdminRole, "Chef", new SilaCulinaryUpsertEventRequestDto
            {
                Name = "VIP Dinner",
                PropertyId = property.Id,
                KitchenLocationId = kitchen.Id,
                StartDateUtc = new DateTime(2026, 12, 20),
                EndDateUtc = new DateTime(2026, 12, 20),
                GuaranteedGuests = 100,
                NetPricePerGuest = 300m,
                ServiceStyle = "PLATED"
            }, CancellationToken.None);

            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", ev.PrimaryMenuId!.Value, new()
            {
                CourseName = "Main Courses",
                RecipeId = rSalmon,
                ServiceStyle = "PLATED",
                AllocatedPortions = 100
            }, CancellationToken.None);

            await svc.SubmitEventForApprovalAsync(orgId, chefId, AdminRole, "Chef", ev.Id, CancellationToken.None);

            // Separation of duties: same user cannot approve
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                svc.DecideEventApprovalAsync(orgId, chefId, AdminRole, "Chef", ev.Id, "APPROVE", "Self", CancellationToken.None));

            SilaCulinaryEventDto approved = await svc.DecideEventApprovalAsync(orgId, approverId, AdminRole, "Exec Chef", ev.Id, "APPROVE", "Approved", CancellationToken.None);
            Assert.Equal("READY_FOR_MRP", approved.Status);
            Assert.Equal(1, approved.ApprovedRevisionNumber);

            // Scenario H: Recipe master updates to version 2 after approval; approved event retains version 1
            Recipe recTracked = await db.Context.Recipe.FirstAsync(r => r.Id == rSalmon);
            recTracked.ActiveVersion = 2;
            recTracked.Version = 2;
            await db.Context.SaveChangesAsync();

            SilaCulinaryPhase2BHandoverDto handover = await svc.GetPhase2BHandoverAsync(orgId, AdminRole, ev.Id, CancellationToken.None);
            Assert.True(handover.MrpReady);
            Assert.Equal(1, handover.PinnedMenuLines[0].RecipeVersion);
            Assert.True(handover.PinnedMenuLines[0].RecipeVersionOutdated);
            Assert.Single(handover.PreliminaryMaterialRequirements);
            Assert.Equal("SUFFICIENT", handover.PreliminaryMaterialRequirements[0].StockIndicator);
            Assert.Equal(100m, (await db.Context.InventoryBalance.FirstAsync(b => b.MaterialId == matId)).OnHandQty);
        }

        [Fact]
        public async Task MissingRecipeHandover_InvalidRecipeId_And_CustomerMenuSecurity_And_MultiSession()
        {
            (TestDatabase db, SilaCulinaryEventMenuService svc, BuyerBusinessProfile buyer, BuyerProperty property, InventoryLocation kitchen) = CreateFixture();
            Guid chefId = Guid.NewGuid();
            Guid orgId = buyer.OrganizationId;

            Guid rUnknownAllergen = SeedPublishedRecipe(db, buyer.Id, "RI000099", "Secret Dessert", "Desserts", 10m, Array.Empty<string>(), Array.Empty<string>(), allergenUnknown: true);

            SilaCulinaryEventDto ev = await svc.CreateEventAsync(orgId, chefId, AdminRole, "Chef", new SilaCulinaryUpsertEventRequestDto
            {
                Name = "Allergy & Conference Event",
                PropertyId = property.Id,
                KitchenLocationId = kitchen.Id,
                VenueName = "Ballroom A",
                StartDateUtc = new DateTime(2026, 12, 22),
                EndDateUtc = new DateTime(2026, 12, 24),
                GuaranteedGuests = 50,
                DietaryRequirements = new List<SilaCulinaryDietaryAllocationDto>
                {
                    new() { RequirementType = "Vegan", GuestCount = 10 }
                }
            }, CancellationToken.None);

            // Multi-day session support with distinct guest count
            SilaCulinaryEventDto withSession = await svc.UpsertSessionAsync(orgId, chefId, AdminRole, "Chef", ev.Id, new()
            {
                SessionName = "Day 2 Gala Dinner",
                DayNumber = 2,
                SessionDateUtc = new DateTime(2026, 12, 23),
                GuestCount = 120,
                ServiceStyle = "BUFFET",
                VenueName = "Ballroom B"
            }, CancellationToken.None);
            Assert.Equal(120, withSession.Sessions.First().GuestCount);

            // Invalid Recipe ID must be rejected
            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", ev.PrimaryMenuId!.Value, new() { RecipeId = Guid.NewGuid() }, CancellationToken.None));

            // Add dish with unknown allergen & missing recipe placeholder
            await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", ev.PrimaryMenuId!.Value, new()
            {
                CourseName = "Desserts",
                RecipeId = rUnknownAllergen
            }, CancellationToken.None);
            SilaCulinaryMenuDto menu = await svc.UpsertMenuLineAsync(orgId, chefId, AdminRole, "Chef", ev.PrimaryMenuId!.Value, new()
            {
                CourseName = "Starters",
                DishName = "Custom Vegan Mezze"
            }, CancellationToken.None);

            Guid missingLineId = menu.Lines.First(l => l.LineStatus == "RECIPE_REQUIRED").Id;
            menu = await svc.CreateMissingRecipeDraftAsync(orgId, chefId, AdminRole, "Chef", menu.Id, missingLineId, CancellationToken.None);
            Assert.NotNull(menu.Lines.First(l => l.Id == missingLineId).RecipeDraftId);

            // Readiness blocks approval due to unresolved draft & unverified unknown allergen on dietary event
            SilaCulinaryEventReadinessDto readiness = await svc.GetEventReadinessAsync(orgId, ev.Id, CancellationToken.None);
            Assert.False(readiness.ReadyForApproval);
            Assert.True(readiness.BlockedCount >= 2);

            // Customer-facing menu discloses pending allergen verification
            SilaCulinaryCustomerMenuPreviewDto cust = await svc.GetCustomerMenuPreviewAsync(orgId, ev.Id, CancellationToken.None);
            Assert.Contains(cust.Courses.SelectMany(c => c.Dishes), d => d.AllergenPendingVerification);

            // Non-admin role has CostVisible == false
            SilaCulinaryEventDto restricted = await svc.GetEventAsync(orgId, chefId, Guid.NewGuid(), ev.Id, CancellationToken.None);
            Assert.False(restricted.CostVisible);
            Assert.Null(restricted.TotalEstimatedMenuCost);

            // Template excludes customer info & stale prices
            SilaCulinaryEventTemplateDto tpl = await svc.CreateTemplateFromEventAsync(orgId, chefId, AdminRole, ev.Id, "Conference Standard", "Reusable 3-day", CancellationToken.None);
            Assert.Equal("Allergy & Conference Event", ev.Name);
            Assert.Equal("Conference Standard", tpl.Name);

            // CSV report prevents spreadsheet formula injection
            SilaCulinaryEventReportResultDto report = await svc.ExportReportAsync(orgId, AdminRole, ev.Id, "EVENT_SUMMARY", CancellationToken.None);
            string csv = Encoding.UTF8.GetString(Convert.FromBase64String(report.Base64Content));
            Assert.Contains("RI000099", csv);
        }
    }
}
