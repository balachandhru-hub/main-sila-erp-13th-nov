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
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Enums;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaCulinaryLifecycleServiceTests
    {
        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) => _handler = handler;
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                => Task.FromResult(_handler(request));
        }

        private sealed class FakeHttpClientFactory : IHttpClientFactory
        {
            private readonly StubHttpMessageHandler _handler;
            public FakeHttpClientFactory(StubHttpMessageHandler handler) => _handler = handler;
            public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
        }

        private static IConfiguration BuildConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["InterCallService:CulinaryEngineUrl"] = "http://127.0.0.1:8013"
                })
                .Build();

        private static (BuyerBusinessProfile Buyer, BuyerProperty Property, InventoryLocation Kitchen, SilaCulinaryAiConfiguration Config) SeedTenant(TestDatabase db)
        {
            Guid orgId = Guid.NewGuid();
            Guid buyerId = Guid.NewGuid();
            BuyerBusinessProfile buyer = new()
            {
                Id = buyerId,
                OrganizationId = orgId,
                OrganizationName = "SILA Hospitality Test",
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
                LocationName = "Main Hot Kitchen",
                LocationType = "KITCHEN",
                StorageLocationCode = "1060",
                InventoryEnabled = true,
                ConsumptionEnabled = true,
                IsActive = true
            };
            SilaCulinaryAiConfiguration cfg = SilaCulinaryAiSettings.CreateDefault(buyerId);
            cfg.Id = Guid.NewGuid();
            cfg.IsActive = true;
            db.Seed(buyer, property, kitchen, cfg);
            return (buyer, property, kitchen, cfg);
        }

        private static SilaCulinaryLifecycleService BuildService(TestDatabase db, Func<HttpRequestMessage, HttpResponseMessage>? http = null)
        {
            StubHttpMessageHandler handler = new(http ?? (_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));
            return new SilaCulinaryLifecycleService(db.Repository, new FakeHttpClientFactory(handler), BuildConfig(), db.Logger);
        }

        private static SilaCulinaryRecipeDraft SeedDraftWithMappings(
            TestDatabase db, Guid buyerId, Guid propertyId, Guid kitchenId,
            string recipeName, decimal servings,
            params (string TempRef, string Desc, decimal Qty, string Uom, ItemBuyerMaster? Mat, bool Confirmed)[] items)
        {
            CulinaryStructuredRecipePayloadDto payload = new()
            {
                Header = new CulinaryRecipeHeaderDto
                {
                    ProposedName = recipeName,
                    Description = $"{recipeName} test recipe",
                    Category = "Main Course",
                    Cuisine = "Indian",
                    RecipeType = "STANDARD",
                    BaseServings = servings,
                    ServingUom = "PORTION",
                    PrepDurationMinutes = 25,
                    CookDurationMinutes = 40
                },
                Ingredients = items.Select(i => new CulinaryIngredientLineDto
                {
                    TempRef = i.TempRef,
                    Description = i.Desc,
                    Quantity = i.Qty,
                    RecipeUom = i.Uom,
                    QuantityBasis = "RAW",
                    YieldPercent = 100m
                }).ToList(),
                Instructions = new List<CulinaryInstructionStepDto>
                {
                    new() { StepNumber = 1, StepTitle = "Prep", Instruction = "Prepare and cook ingredients to spec." }
                }
            };

            SilaCulinaryRecipeDraft draft = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                DraftReference = $"CRD-{Guid.NewGuid().ToString("N")[..6]}",
                RecipeName = recipeName,
                Status = "CONFIRMED",
                RevisionNumber = 1,
                BaseServings = servings,
                PropertyId = propertyId,
                PropertyName = "Grand Resort Dubai",
                LocationId = kitchenId,
                CreatedByUserId = Guid.NewGuid(),
                PayloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower }),
                IsActive = true
            };
            db.Seed(draft);

            foreach (var item in items)
            {
                db.Seed(new SilaCulinaryMaterialMapping
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyerId,
                    DraftId = draft.Id,
                    IngredientTempRef = item.TempRef,
                    OriginalDescription = item.Desc,
                    NormalizedDescription = item.Desc.ToLowerInvariant(),
                    RecipeQuantity = item.Qty,
                    RecipeUom = item.Uom,
                    MaterialId = item.Mat?.Id,
                    MaterialCode = item.Mat?.MaterialCode,
                    MaterialDescription = item.Mat?.Description,
                    BaseUom = item.Mat?.BaseUnitOfMeasure ?? item.Uom,
                    ConvertedBaseQuantity = item.Qty,
                    UomCompatible = true,
                    MappingStatus = item.Confirmed && item.Mat != null ? "CONFIRMED" : "MISSING_MATERIAL",
                    MatchMethod = item.Mat != null ? "EXACT" : "NONE",
                    IsActive = true
                });
            }

            return draft;
        }

        [Fact]
        public async Task MaterialRequest_ConsolidatesMultiRecipeLinks_EnforcesSeparationOfDuties_AndCreatesStandaloneMaterial()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);
            Guid chefId = Guid.NewGuid();
            Guid chefRoleId = Guid.NewGuid();
            Guid masterDataId = Guid.NewGuid();
            SilaCulinaryLifecycleService svc = BuildService(db);

            SilaCulinaryRecipeDraft d1 = SeedDraftWithMappings(db, buyer.Id, prop.Id, kitchen.Id, "Butter Chicken", 50m, ("ING-1", "Kasuri Methi", 0.05m, "KG", null, false));
            SilaCulinaryRecipeDraft d2 = SeedDraftWithMappings(db, buyer.Id, prop.Id, kitchen.Id, "Dal Makhani", 40m, ("ING-1", "Kasuri Methi", 0.04m, "KG", null, false));
            SilaCulinaryRecipeDraft d3 = SeedDraftWithMappings(db, buyer.Id, prop.Id, kitchen.Id, "Paneer Tikka", 30m, ("ING-1", "Kasuri Methi", 0.03m, "KG", null, false));

            List<SilaCulinaryMaterialRequestDto> bulk = await svc.BulkPrepareMaterialRequestsAsync(
                buyer.OrganizationId, chefId, chefRoleId, "Chef Arjun",
                new SilaCulinaryBulkMaterialRequestDto
                {
                    PropertyId = prop.Id,
                    ConsolidateEquivalent = true,
                    Items = new List<SilaCulinaryMaterialRequestLinkInputDto>
                    {
                        new() { DraftId = d1.Id, IngredientTempRef = "ING-1", IngredientDescription = "Kasuri Methi", RequiredQuantity = 0.05m, RecipeUom = "KG" },
                        new() { DraftId = d2.Id, IngredientTempRef = "ING-1", IngredientDescription = "Kasuri Methi", RequiredQuantity = 0.04m, RecipeUom = "KG" },
                        new() { DraftId = d3.Id, IngredientTempRef = "ING-1", IngredientDescription = "Kasuri Methi", RequiredQuantity = 0.03m, RecipeUom = "KG" }
                    }
                },
                CancellationToken.None);

            Assert.Single(bulk);
            Assert.Equal(3, bulk[0].Links.Count);
            Assert.Equal("NEW_MATERIAL", bulk[0].RequestType);

            // Submit for approval
            SilaCulinaryMaterialRequestDto submitted = await svc.SubmitMaterialRequestAsync(
                buyer.OrganizationId, chefId, chefRoleId, "Chef Arjun", bulk[0].Id, CancellationToken.None);
            Assert.Equal("PENDING_APPROVAL", submitted.Status);

            // Separation of duties blocks self-approval by non-admin requester
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                svc.DecideMaterialRequestAsync(
                    buyer.OrganizationId, chefId, chefRoleId, "Chef Arjun", submitted.Id,
                    new SilaCulinaryMaterialRequestDecisionDto { Decision = "APPROVE" }, CancellationToken.None));

            // Authorized master data approver approves
            SilaCulinaryMaterialRequestDto approved = await svc.DecideMaterialRequestAsync(
                buyer.OrganizationId, masterDataId, Common.BUYER_ADMIN_ROLE_ID, "Master Data Lead", submitted.Id,
                new SilaCulinaryMaterialRequestDecisionDto { Decision = "APPROVE", Comment = "Verified spec" }, CancellationToken.None);
            Assert.Equal("APPROVED", approved.Status);

            // Standalone tenant execution creates MAT######## and reconnects all 3 waiting recipe mappings
            SilaCulinaryMaterialRequestDto completed = await svc.ExecuteMaterialCreationAsync(
                buyer.OrganizationId, masterDataId, Common.BUYER_ADMIN_ROLE_ID, "Master Data Lead", approved.Id, "idem-1", CancellationToken.None);
            Assert.Equal("MATERIAL_AVAILABLE", completed.Status);
            Assert.StartsWith("MAT", completed.ResultingMaterialCode);
            Assert.All(completed.Links, l => Assert.True(l.Reconnected));

            List<SilaCulinaryMaterialMapping> reconnectedMaps = await db.Context.SilaCulinaryMaterialMapping
                .Where(m => m.BuyerId == buyer.Id && m.MaterialCode == completed.ResultingMaterialCode)
                .ToListAsync();
            Assert.Equal(3, reconnectedMaps.Count);
            Assert.All(reconnectedMaps, m => Assert.Equal("CANDIDATE_READY", m.MappingStatus));
        }

        [Fact]
        public async Task ErpIntegratedTenant_UsesControlledManualFallback_AndLeadingZeroVerification()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);
            Guid masterDataId = Guid.NewGuid();
            Guid requesterId = Guid.NewGuid();

            // Configure GET_MATERIAL integration (ERP-authoritative tenant with no POST_MATERIAL API)
            db.Seed(new ApiIntegrationConfiguration
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ProcessType = IntegrationProcessType.GET_MATERIAL.ToString(),
                Status = IntegrationConfigurationStatus.ACTIVE,
                IsActive = true
            });

            SilaCulinaryLifecycleService svc = BuildService(db);
            SilaCulinaryRecipeDraft draft = SeedDraftWithMappings(db, buyer.Id, prop.Id, kitchen.Id, "Saffron Risotto", 20m, ("ING-1", "Saffron Strands", 0.02m, "KG", null, false));

            SilaCulinaryMaterialRequestDto req = await svc.CreateMaterialRequestAsync(
                buyer.OrganizationId, requesterId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryMaterialRequestUpsertDto
                {
                    RequestType = "NEW_MATERIAL",
                    ProposedDescription = "SAFFRON STRANDS",
                    BaseUom = "KG",
                    PropertyId = prop.Id,
                    Links = new List<SilaCulinaryMaterialRequestLinkInputDto>
                    {
                        new() { DraftId = draft.Id, IngredientTempRef = "ING-1", IngredientDescription = "Saffron Strands", RequiredQuantity = 0.02m, RecipeUom = "KG" }
                    }
                },
                CancellationToken.None);

            await svc.SubmitMaterialRequestAsync(buyer.OrganizationId, requesterId, Common.BUYER_ADMIN_ROLE_ID, "Chef", req.Id, CancellationToken.None);
            await svc.DecideMaterialRequestAsync(buyer.OrganizationId, masterDataId, Common.BUYER_ADMIN_ROLE_ID, "MD Lead", req.Id,
                new SilaCulinaryMaterialRequestDecisionDto { Decision = "APPROVE" }, CancellationToken.None);

            // Execute moves to CREATION_PENDING instead of fabricating SAP material number
            SilaCulinaryMaterialRequestDto erpPending = await svc.ExecuteMaterialCreationAsync(
                buyer.OrganizationId, masterDataId, Common.BUYER_ADMIN_ROLE_ID, "MD Lead", req.Id, null, CancellationToken.None);
            Assert.Equal("CREATION_PENDING", erpPending.Status);
            Assert.Equal("PENDING_ERP_MANUAL", erpPending.ExecutionStatus);
            Assert.True(erpPending.IsErpIntegratedTenant);
            Assert.False(erpPending.HasErpCreationApi);

            // Seed authoritative SAP-synced ItemBuyerMaster with leading zeros
            db.Seed(new ItemBuyerMaster
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "000000000000100042",
                Description = "SAFFRON STRANDS SPANISH GRADE A",
                BaseUnitOfMeasure = "KG",
                MaterialGroup = "SPICES",
                ProductType = "ROH",
                IsActive = true
            });

            SilaCulinaryMaterialRequestDto verified = await svc.VerifyManualErpMaterialAsync(
                buyer.OrganizationId, masterDataId, Common.BUYER_ADMIN_ROLE_ID, "MD Lead", req.Id,
                new SilaCulinaryManualErpVerifyDto { MaterialCode = "100042", AutoReconnectDrafts = true }, CancellationToken.None);
            Assert.Equal("MATERIAL_AVAILABLE", verified.Status);
            Assert.Equal("000000000000100042", verified.ResultingMaterialCode);
        }

        [Fact]
        public async Task Costing_ButterChicken_50Servings_MatchesExactDeterministicBaseline_AndHandlesMissingPrices()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);
            Guid adminId = Guid.NewGuid();
            SilaCulinaryLifecycleService svc = BuildService(db);

            ItemBuyerMaster AddMat(string code, string desc, string uom, decimal price)
            {
                ItemBuyerMaster m = new()
                {
                    Id = Guid.NewGuid(),
                    BuyerId = buyer.Id,
                    MaterialCode = code,
                    Description = desc,
                    BaseUnitOfMeasure = uom,
                    MovingAveragePrice = price,
                    Currency = "AED",
                    IsActive = true
                };
                db.Seed(m);
                return m;
            }

            var chicken = AddMat("MAT-01", "Chicken Breast", "KG", 24m);
            var tomato = AddMat("MAT-02", "Tomato Puree", "KG", 12m);
            var butter = AddMat("MAT-03", "Unsalted Butter", "KG", 32m);
            var cream = AddMat("MAT-04", "Fresh Cream", "L", 18m);
            var gg = AddMat("MAT-05", "Ginger Garlic Paste", "KG", 18m);
            var yogurt = AddMat("MAT-06", "Yogurt", "KG", 8m);
            var garam = AddMat("MAT-07", "Garam Masala", "KG", 45m);
            var chilli = AddMat("MAT-08", "Chilli Powder", "KG", 40m);
            var kasuri = AddMat("MAT-09", "Kasuri Methi", "KG", 60m);
            var salt = AddMat("MAT-10", "Salt", "KG", 3m);

            SilaCulinaryRecipeDraft draft = SeedDraftWithMappings(db, buyer.Id, prop.Id, kitchen.Id, "Butter Chicken", 50m,
                ("I1", "Chicken breast", 9m, "KG", chicken, true),
                ("I2", "Tomato puree", 6m, "KG", tomato, true),
                ("I3", "Unsalted butter", 1.25m, "KG", butter, true),
                ("I4", "Fresh cream", 2m, "L", cream, true),
                ("I5", "Ginger garlic paste", 0.6m, "KG", gg, true),
                ("I6", "Yogurt", 1.5m, "KG", yogurt, true),
                ("I7", "Garam masala", 0.125m, "KG", garam, true),
                ("I8", "Chilli powder", 0.1m, "KG", chilli, true),
                ("I9", "Kasuri methi", 0.05m, "KG", kasuri, true),
                ("I10", "Salt", 0.125m, "KG", salt, true));

            SilaCulinaryCostingResultDto res = await svc.CalculateDraftCostingAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Cost Controller", draft.Id,
                new SilaCulinaryCostingCalculateRequestDto
                {
                    SellingPricePerPortion = 65m,
                    TargetFoodCostPercent = 20m,
                    SaveSnapshot = true,
                    IsBaseline = true,
                    AdditionalCosts = new List<SilaCulinaryAdditionalCostInputDto>
                    {
                        new() { Name = "Kitchen Labor & Packaging", Category = "DIRECT_OPERATING", Basis = "PER_PORTION", Amount = 8m }
                    }
                },
                CancellationToken.None);

            Assert.Equal("COMPLETE", res.CostCompleteness);
            Assert.Equal(399.80m, res.TotalIngredientCost);
            Assert.Equal(7.9960m, res.IngredientCostPerPortion);
            Assert.Equal(3250.00m, res.EstimatedRevenue);
            Assert.Equal(12.30m, res.FoodCostPercent);
            Assert.Equal(2450.20m, res.ContributionAmount);
            Assert.NotNull(res.SnapshotId);
            Assert.StartsWith("CST", res.CostingReference);

            // Zero selling price safe handling
            SilaCulinaryCostingResultDto zeroSell = await svc.CalculateDraftCostingAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Cost Controller", draft.Id,
                new SilaCulinaryCostingCalculateRequestDto { SellingPricePerPortion = 0m }, CancellationToken.None);
            Assert.Null(zeroSell.FoodCostPercent);
        }

        [Fact]
        public async Task EndToEnd_Approval_ProductionRecipeId_Idempotency_Publishing_Versioning_And_Phase2AHandover()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);
            Guid adminId = Guid.NewGuid();
            SilaCulinaryLifecycleService svc = BuildService(db);

            ItemBuyerMaster chicken = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-100",
                Description = "CHICKEN BREAST",
                BaseUnitOfMeasure = "KG",
                MovingAveragePrice = 24m,
                Currency = "AED",
                IsActive = true
            };
            db.Seed(chicken);

            SilaCulinaryRecipeDraft draft = SeedDraftWithMappings(db, buyer.Id, prop.Id, kitchen.Id, "Grilled Chicken", 10m,
                ("I1", "Chicken breast", 2m, "KG", chicken, true));

            SilaCulinaryRecipeReadinessDto readiness = await svc.GetRecipeReadinessAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, draft.Id, CancellationToken.None);
            Assert.True(readiness.ReadyForApproval);

            var submitted = await svc.SubmitRecipeForApprovalAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Chef", draft.Id, null, CancellationToken.None);
            Assert.Equal("PENDING_APPROVAL", submitted.LifecycleStatus);

            var approved = await svc.DecideRecipeApprovalAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", draft.Id, true, null, CancellationToken.None);
            Assert.Equal("APPROVED", approved.LifecycleStatus);

            // Idempotent Production Recipe ID creation
            var prod1 = await svc.CreateProductionRecipeAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", draft.Id,
                new SilaCulinaryRecipeApproveActionDto { IdempotencyKey = "rcp-create-1" }, CancellationToken.None);
            var prod2 = await svc.CreateProductionRecipeAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", draft.Id,
                new SilaCulinaryRecipeApproveActionDto { IdempotencyKey = "rcp-create-1" }, CancellationToken.None);

            Assert.NotNull(prod1.ProductionRecipeId);
            Assert.StartsWith("RI", prod1.ProductionRecipeCode);
            Assert.Equal(prod1.ProductionRecipeId, prod2.ProductionRecipeId);
            Assert.Equal(prod1.ProductionRecipeCode, prod2.ProductionRecipeCode);

            var published = await svc.PublishProductionRecipeAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Exec Chef", draft.Id, null, CancellationToken.None);
            Assert.Equal("PUBLISHED", published.LifecycleStatus);
            Assert.Equal(1, published.ProductionActiveVersion);

            // Phase 2A Handover Contract returns read-only published recipe + pinned costing snapshot
            SilaCulinaryPhase2AHandoverDto handover = await svc.GetPhase2AHandoverAsync(
                buyer.OrganizationId, prod1.ProductionRecipeId!.Value, null, CancellationToken.None);
            Assert.Equal(prod1.ProductionRecipeCode, handover.ProductionRecipeCode);
            Assert.Equal(1, handover.RecipeVersion);
            Assert.Single(handover.Ingredients);
            Assert.Equal(4.8000m, handover.CostPerPortion);

            // Create controlled new version while keeping v1 ActiveVersion intact
            var v2 = await svc.CreateNewProductionVersionAsync(
                buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, "Chef", draft.Id,
                new SilaCulinaryRecipeApproveActionDto { ChangeReason = "Seasonal garnish update" }, CancellationToken.None);
            Assert.Equal(2, v2.ProductionRecipeVersion);
            Assert.Equal(1, v2.ProductionActiveVersion);

            // Cross-tenant isolation check
            var otherTenant = SeedTenant(db);
            await Assert.ThrowsAsync<NotFoundCustomException>(() =>
                svc.GetRecipeReadinessAsync(otherTenant.Buyer.OrganizationId, adminId, Common.BUYER_ADMIN_ROLE_ID, draft.Id, CancellationToken.None));
        }
    }
}
