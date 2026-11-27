using System.Net;
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
    public class SilaCulinaryMrpServiceTests
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
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

        private static (TestDatabase Db, SilaCulinaryMrpService Service, BuyerBusinessProfile Buyer, BuyerProperty Property, InventoryLocation Kitchen) CreateFixture()
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
            SilaCulinaryMrpService svc = new(db.Repository, db.Logger, new FakeHttpClientFactory(), BuildConfig(), new FakeProtector());
            return (db, svc, buyer, property, kitchen);
        }

        private static ItemBuyerMaster SeedMaterial(TestDatabase db, Guid buyerId, Guid locationId, string code, string desc, string group, string uom, decimal? unitCost, decimal? onHand, decimal? openPoQty = null)
        {
            ItemBuyerMaster mat = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialCode = code,
                Description = desc,
                MaterialGroup = group,
                BaseUnitOfMeasure = uom,
                OrderUnitOfMeasure = uom,
                UnitCost = unitCost,
                IsInventoryItem = true,
                IsActive = true
            };
            db.Seed(mat);
            if (onHand.HasValue)
            {
                db.Seed(new InventoryBalance
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    LocationId = locationId,
                    MaterialId = mat.Id,
                    OnHandQty = onHand.Value,
                    BaseUom = uom,
                    UnitCost = unitCost ?? 0m,
                    LastMovementOn = DateTime.UtcNow.AddHours(-1),
                    IsActive = true
                });
            }
            if (openPoQty.HasValue && openPoQty.Value > 0)
            {
                PurchaseOrder po = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    PoNumber = $"PO-{code}",
                    Status = "APPROVED",
                    IsActive = true
                };
                db.Seed(po);
                db.Seed(new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    MaterialCode = code,
                    ProductName = desc,
                    Quantity = openPoQty.Value,
                    ReceivedQuantity = 0m,
                    UnitPrice = unitCost ?? 0m,
                    IsActive = true
                });
            }
            return mat;
        }

        private static (SilaCulinaryEvent Ev, SilaCulinaryEventSession Sess, Recipe MainRec, Recipe SubRec, ItemBuyerMaster Salmon, ItemBuyerMaster Tomato) SeedGalaEvent(
            TestDatabase db, Guid buyerId, BuyerProperty prop, InventoryLocation kitchen, bool includeUnmappedDill = false)
        {
            ItemBuyerMaster salmon = SeedMaterial(db, buyerId, kitchen.Id, "MAT-SALMON", "Atlantic Salmon Fillet", "Seafood", "KG", 85m, 25m, openPoQty: 10m);
            ItemBuyerMaster tomato = SeedMaterial(db, buyerId, kitchen.Id, "MAT-TOMATO", "Plum Tomatoes", "Vegetables", "KG", 8m, 100m);

            // Sub-recipe: Sauce (batch yield 10 KG, uses 6 KG tomato per batch)
            Recipe subRec = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                RecipeCode = "SUB-SAUCE",
                Name = "Tomato Butter Sauce",
                Category = "Sauce",
                ItemMode = "BATCH",
                ServingQty = 10m,
                ServingUom = "KG",
                Version = 1,
                ActiveVersion = 1,
                Status = Common.SILA_RECIPE_APPROVED,
                IsActive = true
            };
            RecipeIngredient subIng = new()
            {
                Id = Guid.NewGuid(),
                RecipeId = subRec.Id,
                Version = 1,
                MaterialId = tomato.Id,
                ItemCode = tomato.MaterialCode,
                ItemName = tomato.Description,
                Quantity = 6m,
                Uom = "KG",
                BaseUom = "KG",
                IsActive = true
            };

            // Main recipe: Grilled Salmon (1 portion = 180 G edible salmon + 0.05 KG sauce, trim loss 10% -> yield 90%)
            Recipe mainRec = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                RecipeCode = "REC-SALMON",
                Name = "Grilled Atlantic Salmon",
                Category = "Main",
                ItemMode = "RECIPE",
                ServingQty = 1m,
                ServingUom = "PORTION",
                TrimLossPercent = 10m,
                PrepTimeMinutes = 120,
                Version = 2,
                ActiveVersion = 2,
                Status = Common.SILA_RECIPE_APPROVED,
                IsActive = true
            };
            RecipeIngredient mainIngSalmon = new()
            {
                Id = Guid.NewGuid(),
                RecipeId = mainRec.Id,
                Version = 2,
                MaterialId = salmon.Id,
                ItemCode = salmon.MaterialCode,
                ItemName = salmon.Description,
                Quantity = 180m,
                Uom = "G",
                BaseUom = "KG",
                IsActive = true
            };
            RecipeIngredient mainIngSauce = new()
            {
                Id = Guid.NewGuid(),
                RecipeId = mainRec.Id,
                Version = 2,
                SubRecipeId = subRec.Id,
                ItemCode = subRec.RecipeCode,
                ItemName = subRec.Name,
                Quantity = 0.05m,
                Uom = "KG",
                BaseUom = "KG",
                IsActive = true
            };
            // Version 1 of mainRec uses something else to prove pinned version = 2 is respected
            RecipeIngredient mainV1Ing = new()
            {
                Id = Guid.NewGuid(),
                RecipeId = mainRec.Id,
                Version = 1,
                MaterialId = tomato.Id,
                ItemCode = tomato.MaterialCode,
                ItemName = "Old V1 Tomato",
                Quantity = 500m,
                Uom = "G",
                BaseUom = "KG",
                IsActive = true
            };

            db.Seed(subRec, subIng, mainRec, mainIngSalmon, mainIngSauce, mainV1Ing);

            if (includeUnmappedDill)
            {
                db.Seed(new RecipeIngredient
                {
                    Id = Guid.NewGuid(),
                    RecipeId = mainRec.Id,
                    Version = 2,
                    MaterialId = null,
                    ItemName = "Fresh Dill",
                    Quantity = 10m,
                    Uom = "G",
                    BaseUom = "KG",
                    IsActive = true
                });
            }

            SilaCulinaryMenu menu = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MenuCode = "MNU-GALA",
                Name = "Grand Gala Buffet",
                ServiceStyle = "BUFFET",
                Status = "APPROVED",
                IsActive = true
            };
            SilaCulinaryMenuLine ml = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MenuId = menu.Id,
                CourseName = "Main",
                DishName = "Grilled Atlantic Salmon",
                RecipeId = mainRec.Id,
                RecipeCode = mainRec.RecipeCode,
                RecipeVersion = 2,
                UptakePercent = 60m,
                PlannedPortions = 300,
                PortionSize = 180m,
                PortionUom = "G",
                IsActive = true
            };
            SilaCulinaryEvent ev = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                EventCode = "EVT-GALA-01",
                Name = "Grand Corporate Gala",
                PropertyId = prop.Id,
                CompanyCode = "1000",
                PlantCode = "1060",
                VenueName = "Grand Ballroom",
                KitchenLocationId = kitchen.Id,
                StartDateUtc = DateTime.UtcNow.AddDays(5),
                EndDateUtc = DateTime.UtcNow.AddDays(5).AddHours(4),
                GuaranteedGuests = 500,
                PlanningGuests = 500,
                PrimaryMenuId = menu.Id,
                Status = "APPROVED",
                RevisionNumber = 1,
                Currency = "AED",
                IsActive = true
            };
            SilaCulinaryEventSession sess = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                EventId = ev.Id,
                SessionName = "Dinner Gala",
                SessionDateUtc = ev.StartDateUtc,
                KitchenLocationId = kitchen.Id,
                IsActive = true
            };
            db.Seed(menu, ml, ev, sess);
            return (ev, sess, mainRec, subRec, salmon, tomato);
        }

        [Fact]
        public async Task EndToEnd_MrpCalculation_ExplodesSubRecipes_ConvertsUom_ComparesStock_ZeroSideEffects()
        {
            (TestDatabase db, SilaCulinaryMrpService svc, BuyerBusinessProfile buyer, BuyerProperty prop, InventoryLocation kitchen) = CreateFixture();
            (SilaCulinaryEvent ev, _, _, _, _, _) = SeedGalaEvent(db, buyer.Id, prop, kitchen);

            SilaCulinaryMrpRunDetailDto run = await svc.CreateAndCalculateRunAsync(
                buyer.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, "Chef Bala",
                new SilaCulinaryMrpCreateRunRequestDto { EventIds = new() { ev.Id } }, CancellationToken.None);

            Assert.Equal("CALCULATED", run.Status);
            Assert.Equal(2, run.TotalRecipes); // main + sub-recipe
            Assert.Equal(2, run.TotalUniqueMaterials); // Salmon + Tomato

            SilaCulinaryMrpLineDto salmonLine = run.Lines.First(l => l.MaterialCode == "MAT-SALMON");
            // 300 portions * 180 G = 54 KG edible; 90% yield -> 60 KG raw
            Assert.Equal(54m, salmonLine.EdibleQty);
            Assert.Equal(60m, salmonLine.RawQty);
            Assert.Equal(25m, salmonLine.EligibleStockQty);
            Assert.Equal(10m, salmonLine.IncomingPoQty); // displayed separately
            Assert.Equal(35m, salmonLine.IndicativeCurrentShortageQty);
            Assert.Equal(25m, salmonLine.IndicativeRemainingShortageQty);
            Assert.Equal(5100m, salmonLine.RequirementValue); // 60 * 85

            // Verify 10-way consolidation view works
            SilaCulinaryMrpGroupViewDto byRecipe = await svc.GetGroupedViewAsync(buyer.OrganizationId, Common.BUYER_ADMIN_ROLE_ID, run.Id, "RECIPE", CancellationToken.None);
            Assert.NotEmpty(byRecipe.Groups);

            // Verify zero inventory reservations, POs, or ITOs were created by MRP
            Assert.Equal(1, await db.Context.Set<PurchaseOrder>().CountAsync()); // only the pre-seeded open PO
            Assert.Equal(0, await db.Context.Set<InventoryTransaction>().CountAsync());
            Assert.Equal(0, await db.Context.Set<InventoryErpPosting>().CountAsync());
        }

        [Fact]
        public async Task CircularDependency_BlocksCalculation_WithCyclePath()
        {
            (TestDatabase db, SilaCulinaryMrpService svc, BuyerBusinessProfile buyer, BuyerProperty prop, InventoryLocation kitchen) = CreateFixture();
            Recipe a = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "REC-A", Name = "Recipe A", ItemMode = "RECIPE", ServingQty = 1, ServingUom = "KG", Version = 1, ActiveVersion = 1, Status = Common.SILA_RECIPE_APPROVED, IsActive = true };
            Recipe b = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "REC-B", Name = "Recipe B", ItemMode = "BATCH", ServingQty = 1, ServingUom = "KG", Version = 1, ActiveVersion = 1, Status = Common.SILA_RECIPE_APPROVED, IsActive = true };
            Recipe c = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, RecipeCode = "REC-C", Name = "Recipe C", ItemMode = "BATCH", ServingQty = 1, ServingUom = "KG", Version = 1, ActiveVersion = 1, Status = Common.SILA_RECIPE_APPROVED, IsActive = true };
            db.Seed(a, b, c,
                new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = a.Id, Version = 1, SubRecipeId = b.Id, ItemName = "B", Quantity = 1, Uom = "KG", BaseUom = "KG", IsActive = true },
                new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = b.Id, Version = 1, SubRecipeId = c.Id, ItemName = "C", Quantity = 1, Uom = "KG", BaseUom = "KG", IsActive = true },
                new RecipeIngredient { Id = Guid.NewGuid(), RecipeId = c.Id, Version = 1, SubRecipeId = a.Id, ItemName = "A", Quantity = 1, Uom = "KG", BaseUom = "KG", IsActive = true });

            SilaCulinaryMenu menu = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, MenuCode = "MNU-CYC", Name = "Cycle Menu", ServiceStyle = "PLATED", Status = "APPROVED", IsActive = true };
            SilaCulinaryMenuLine ml = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, MenuId = menu.Id, CourseName = "Main", DishName = "A", RecipeId = a.Id, RecipeVersion = 1, PlannedPortions = 10, IsActive = true };
            SilaCulinaryEvent ev = new() { Id = Guid.NewGuid(), BuyerId = buyer.Id, EventCode = "EVT-CYC", Name = "Cycle Event", PropertyId = prop.Id, KitchenLocationId = kitchen.Id, StartDateUtc = DateTime.UtcNow.AddDays(2), EndDateUtc = DateTime.UtcNow.AddDays(2), GuaranteedGuests = 10, PlanningGuests = 10, PrimaryMenuId = menu.Id, Status = "APPROVED", Currency = "AED", IsActive = true };
            db.Seed(menu, ml, ev);

            SilaCulinaryMrpRunDetailDto run = await svc.CreateAndCalculateRunAsync(
                buyer.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryMrpCreateRunRequestDto { EventIds = new() { ev.Id } }, CancellationToken.None);

            Assert.Contains(run.Exceptions, e => e.ExceptionType == "CIRCULAR_RECIPE_DEPENDENCY" && e.Severity == "BLOCKING");
            Assert.False(run.Readiness.CalculationReady);
            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                svc.SubmitForApprovalAsync(buyer.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, "Chef", run.Id, CancellationToken.None));
        }

        [Fact]
        public async Task MissingMaterial_CreatesPhase1CMaterialRequest_AndHidesCostForUnauthorizedRole()
        {
            (TestDatabase db, SilaCulinaryMrpService svc, BuyerBusinessProfile buyer, BuyerProperty prop, InventoryLocation kitchen) = CreateFixture();
            (SilaCulinaryEvent ev, _, _, _, _, _) = SeedGalaEvent(db, buyer.Id, prop, kitchen, includeUnmappedDill: true);

            Guid userId = Guid.NewGuid();
            Guid nonAdminRole = Guid.NewGuid(); // lacks full cost visibility
            SilaCulinaryMrpRunDetailDto run = await svc.CreateAndCalculateRunAsync(
                buyer.OrganizationId, userId, nonAdminRole, "Line Cook",
                new SilaCulinaryMrpCreateRunRequestDto { EventIds = new() { ev.Id } }, CancellationToken.None);

            Assert.False(run.CostVisible);
            Assert.Null(run.EstimatedRequirementValue);
            Assert.All(run.Lines, l => Assert.Null(l.UnitCost));

            SilaCulinaryMrpExceptionDto dillEx = run.Exceptions.First(e => e.ExceptionType == "MISSING_MATERIAL");
            SilaCulinaryMrpExceptionDto updatedEx = await svc.CreateMaterialRequestFromExceptionAsync(
                buyer.OrganizationId, userId, nonAdminRole, "Line Cook", run.Id, dillEx.Id, CancellationToken.None);
            Assert.Equal("IN_PROGRESS", updatedEx.ResolutionStatus);
            Assert.Equal(1, await db.Context.Set<SilaCulinaryMaterialRequest>().CountAsync());
        }

        [Fact]
        public async Task Approval_BaselinePreservation_ImpactAnalysis_Release_And_Phase2CHandover()
        {
            (TestDatabase db, SilaCulinaryMrpService svc, BuyerBusinessProfile buyer, BuyerProperty prop, InventoryLocation kitchen) = CreateFixture();
            (SilaCulinaryEvent ev, _, _, _, _, _) = SeedGalaEvent(db, buyer.Id, prop, kitchen);

            Guid submitter = Guid.NewGuid();
            Guid approver = Guid.NewGuid();
            SilaCulinaryMrpRunDetailDto run = await svc.CreateAndCalculateRunAsync(
                buyer.OrganizationId, submitter, Common.BUYER_ADMIN_ROLE_ID, "Chef Submitter",
                new SilaCulinaryMrpCreateRunRequestDto { EventIds = new() { ev.Id } }, CancellationToken.None);

            await svc.SubmitForApprovalAsync(buyer.OrganizationId, submitter, Common.BUYER_ADMIN_ROLE_ID, "Chef Submitter", run.Id, CancellationToken.None);

            // Separation of duties: submitter cannot approve own MRP
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                svc.DecideApprovalAsync(buyer.OrganizationId, submitter, Common.BUYER_ADMIN_ROLE_ID, "Chef Submitter", run.Id, "APPROVE", "Self", CancellationToken.None));

            SilaCulinaryMrpRunDetailDto approved = await svc.DecideApprovalAsync(
                buyer.OrganizationId, approver, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", run.Id, "APPROVE", "Approved baseline", CancellationToken.None);
            Assert.Equal("APPROVED", approved.Status);
            Assert.True(approved.IsApprovedBaseline);

            // Modify event menu portions -> Event Change Impact Analysis detects delta without mutating approved MRP
            SilaCulinaryMenuLine ml = await db.Context.Set<SilaCulinaryMenuLine>().FirstAsync();
            ml.PlannedPortions = 450;
            SilaCulinaryEvent trackedEv = await db.Context.Set<SilaCulinaryEvent>().FirstAsync();
            trackedEv.RevisionNumber = 2;
            await db.Context.SaveChangesAsync();

            SilaCulinaryMrpImpactAnalysisDto impact = await svc.AnalyzeEventChangeImpactAsync(buyer.OrganizationId, Common.BUYER_ADMIN_ROLE_ID, approved.Id, CancellationToken.None);
            Assert.True(impact.ApprovalInvalidated);
            Assert.Contains(impact.Lines, l => l.ImpactType == "INCREASE" && l.DeltaQty > 0);

            // Release for Phase 2C planning (no duplicate release allowed)
            SilaCulinaryMrpRunDetailDto released = await svc.ReleaseForPlanningAsync(
                buyer.OrganizationId, approver, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", approved.Id, CancellationToken.None);
            Assert.Equal("RELEASED_FOR_PLANNING", released.Status);
            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                svc.ReleaseForPlanningAsync(buyer.OrganizationId, approver, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", approved.Id, CancellationToken.None));

            // Phase 2C handover contract guarantees zero reservations/POs/ITOs/SAP postings
            SilaCulinaryPhase2CHandoverDto handover = await svc.GetPhase2CHandoverAsync(
                buyer.OrganizationId, approver, Common.BUYER_ADMIN_ROLE_ID, approved.Id, CancellationToken.None);
            Assert.True(handover.ReleasedForPlanning);
            Assert.True(handover.ZeroReservationsCreated);
            Assert.True(handover.ZeroPosCreated);
            Assert.True(handover.ZeroItosCreated);
            Assert.True(handover.ZeroSapPostingsExecuted);
            Assert.NotEmpty(handover.Materials);

            // All 36 reports & formula-injection-safe CSV export
            List<SilaCulinaryMrpReportCatalogItemDto> catalog = svc.GetReportCatalog();
            Assert.Equal(36, catalog.Count);
            SilaCulinaryMrpReportResultDto rpt = await svc.ExportReportAsync(buyer.OrganizationId, Common.BUYER_ADMIN_ROLE_ID, approved.Id, "DETAILED_EVENT_BOM", CancellationToken.None);
            Assert.True(rpt.RowCount >= 2);
            Assert.NotEmpty(rpt.Base64Content);

            // Transaction tracker recorded lifecycle events
            Assert.True(await db.Context.Set<InventoryWorkflowEvent>().CountAsync(x => x.ReferenceType == Common.SILA_REF_CULINARY_MRP) >= 4);
        }
    }
}
