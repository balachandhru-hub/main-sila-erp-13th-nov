using System.Net;
using System.Text;
using System.Text.Json;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Tests.Support;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Entities;
using SharedKernel.Integration.Services;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaCulinaryMaterialServiceTests
    {
        private sealed class FakeProtector : IIntegrationCredentialProtector
        {
            public string? Protect(string? value) => string.IsNullOrWhiteSpace(value) ? null : $"ENC::{value}";
            public string? Unprotect(string? value) => value != null && value.StartsWith("ENC::", StringComparison.Ordinal) ? value[5..] : value;
            public string State(ApiIntegrationConfiguration configuration) => "Configured";
        }

        private sealed class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;
            public List<string> PathsCalled { get; } = new();
            public string? LastRequestBody { get; private set; }

            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                PathsCalled.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
                if (request.Content != null)
                {
                    LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
                }
                return _handler(request);
            }
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
                OrganizationName = "SILA Grand Resort",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "chef@grandresort.test",
                Phone = "+971500000001",
                Country = "UAE",
                AddressLine1 = "Palm Crescent",
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

        private static SilaCulinaryRecipeDraft SeedDraft(TestDatabase db, Guid buyerId, Guid propertyId, Guid kitchenId, params CulinaryIngredientLineDto[] lines)
        {
            CulinaryStructuredRecipePayloadDto payload = new()
            {
                Header = new CulinaryRecipeHeaderDto
                {
                    ProposedName = "Chef Test Dish",
                    Description = "Phase 1B integration test dish",
                    BaseServings = 10m,
                    ServingSize = 200m,
                    ServingUom = "G"
                },
                Ingredients = lines.ToList(),
                Instructions = new List<CulinaryInstructionStepDto>
                {
                    new() { StepNumber = 1, StepTitle = "Prep", Instruction = "Prepare ingredients." }
                }
            };
            SilaCulinaryRecipeDraft draft = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                DraftReference = $"AIR-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                RecipeName = payload.Header.ProposedName,
                GenerationMode = "DISH_NAME",
                BaseServings = 10m,
                IngredientCount = lines.Length,
                PropertyId = propertyId,
                LocationId = kitchenId,
                RevisionNumber = 1,
                Status = "CHEF_REVIEW",
                PayloadJson = JsonSerializer.Serialize(payload),
                IsActive = true
            };
            db.Seed(draft);
            return draft;
        }

        private static object BuildPyCandidate(
            string matId, string matCode, string desc, string baseUom, string method, double score,
            bool specCompatible = true, bool uomCompatible = true, decimal? convertedQty = null, decimal conversionFactor = 1m) => new
        {
            material_id = matId,
            material_code = matCode,
            description = desc,
            base_uom = baseUom,
            order_uom = baseUom,
            match_method = method,
            scores = new
            {
                exact = method.StartsWith("EXACT", StringComparison.Ordinal) ? score : 0.0,
                alias = method.Contains("ALIAS", StringComparison.Ordinal) ? score : 0.0,
                fuzzy_wratio = method == "FUZZY" ? score * 100.0 : 0.0,
                semantic = method == "SEMANTIC" ? score : 0.0,
                rerank_norm = score,
                composite_score = score
            },
            specification_compatible = specCompatible,
            material_active = true,
            plant_eligible = true,
            uom_validation = new
            {
                compatible = uomCompatible,
                status = uomCompatible ? "COMPATIBLE" : "UOM_CONFLICT",
                recipe_qty = 1m,
                recipe_uom = baseUom,
                base_uom = baseUom,
                converted_base_qty = convertedQty,
                conversion_factor = conversionFactor,
                warning = uomCompatible ? null : "Missing conversion",
                reason = uomCompatible ? "Valid conversion" : "Missing conversion"
            },
            evidence = new[] { $"Matched via {method}" },
            warnings = specCompatible ? Array.Empty<string>() : new[] { "Salted/unsalted specification mismatch" }
        };

        private static object BuildPyResult(
            string tempRef, string normalized, string method, string status, double score,
            object? selectedCandidate, object[]? candidates = null, string missingClass = "NONE", string? recReqType = null) => new
        {
            temp_ref = tempRef,
            original_description = normalized,
            normalized_description = normalized,
            match_method = method,
            status = status,
            missing_material_classification = missingClass,
            recommended_request_type = recReqType,
            confidence_score = score,
            recommended_candidate = selectedCandidate,
            candidates = candidates ?? (selectedCandidate != null ? new[] { selectedCandidate } : Array.Empty<object>())
        };

        private static HttpResponseMessage BuildMatchResponse(
            string tempRef,
            string original,
            string? topMatId,
            string? topMatCode,
            string? topMatDesc,
            string method,
            string status,
            decimal score,
            bool specCompatible = true,
            bool uomCompatible = true,
            decimal? convertedQty = null)
        {
            object? cand = topMatId != null
                ? BuildPyCandidate(topMatId, topMatCode!, topMatDesc!, "KG", method, (double)score, specCompatible, uomCompatible, convertedQty)
                : null;
            object result = BuildPyResult(
                tempRef, original.ToLowerInvariant(), method, status, (double)score,
                cand, cand != null ? new[] { cand } : Array.Empty<object>(),
                topMatId == null ? "NO_MATERIAL_FOUND" : "NONE",
                topMatId == null ? "NEW_MATERIAL" : null);

            string json = JsonSerializer.Serialize(new { results = new[] { result } });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        [Fact]
        public async Task MatchDraft_PreservesLeadingZeroCode_EnrichesStockPrice_AndConvertsUom()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);

            ItemBuyerMaster chicken = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "0000100234",
                Description = "CHICKEN BREAST BONELESS FRESH",
                MaterialGroup = "POULTRY",
                ProductType = "ROH",
                BaseUnitOfMeasure = "KG",
                OrderUnitOfMeasure = "CASE",
                AlternateUnitOfMeasure = "G",
                UnitOfMeasureMapping = "1 CASE = 10 KG",
                UnitCost = 24.50m,
                Currency = "AED",
                IsInventoryItem = true,
                IsActive = true
            };
            MaterialUomConversion conv = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialId = chicken.Id,
                FromUom = "CASE",
                ToUom = "KG",
                Factor = 10m,
                IsActive = true
            };
            InventoryBalance bal = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                LocationId = kitchen.Id,
                MaterialId = chicken.Id,
                OnHandQty = 15m,
                BaseUom = "KG",
                IsActive = true
            };
            InternalTransferOrder openIto = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                ItoNumber = "ITO-001",
                Mode = "DIRECT",
                FromLocationId = kitchen.Id,
                ToLocationId = Guid.NewGuid(),
                Status = Common.SILA_ITO_APPROVED,
                IsActive = true
            };
            InternalTransferOrderItem itoLine = new()
            {
                Id = Guid.NewGuid(),
                InternalTransferOrderId = openIto.Id,
                MaterialId = chicken.Id,
                MaterialCode = chicken.MaterialCode,
                MaterialName = chicken.Description,
                RequestedQty = 3m,
                ApprovedQty = 3m,
                Uom = "KG",
                IsActive = true
            };
            MaterialOutletPrice outletPrice = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialId = chicken.Id,
                OutletLocationId = kitchen.Id,
                UnitCost = 26.00m,
                Currency = "AED",
                IsActive = true
            };
            db.Seed(chicken, conv, bal, openIto, itoLine, outletPrice);

            SilaCulinaryRecipeDraft draft = SeedDraft(db, buyer.Id, prop.Id, kitchen.Id,
                new CulinaryIngredientLineDto
                {
                    TempRef = "ING-01",
                    Description = "Boneless Chicken Breast",
                    Quantity = 750m,
                    RecipeUom = "G"
                });

            StubHttpMessageHandler handler = new(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith("/v1/materials/match-bulk"))
                {
                    return BuildMatchResponse("ING-01", "Boneless Chicken Breast",
                        chicken.Id.ToString(), chicken.MaterialCode, chicken.Description,
                        "EXACT_DESC", "EXACT_MATCH", 1.0m, true, true, 0.75m);
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"indexed_count\":1,\"status\":\"READY\"}", Encoding.UTF8, "application/json")
                };
            });

            SilaCulinaryMaterialService svc = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);
            Guid userId = Guid.NewGuid();
            SilaCulinaryMaterialWorkspaceDto ws = await svc.MatchDraftAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id,
                new SilaCulinaryMaterialMatchRequestDto(), CancellationToken.None);

            SilaCulinaryMaterialMappingLineDto line = Assert.Single(ws.Lines);
            // Leading zeros preserved
            Assert.Equal("0000100234", line.MaterialCode);
            Assert.Equal("EXACT_MATCH", line.MappingStatus);
            // UOM conversion: 750 G -> 0.75 KG
            Assert.True(line.UomCompatible);
            Assert.Equal(0.75m, line.ConvertedQuantity);
            // Stock: 15 OnHand - 3 ITO reserved = 12 Available
            Assert.Equal(15m, line.OnHandStock);
            Assert.Equal(3m, line.ReservedStock);
            Assert.Equal(12m, line.AvailableStock);
            Assert.Equal("AVAILABLE", line.StockStatus);
            // Price: Outlet price 26.00 AED overrides ItemBuyerMaster 24.50
            Assert.True(ws.CanViewCost);
            Assert.Equal(26.00m, line.UnitPrice);
            Assert.Equal("OUTLET_PRICE", line.PriceSource);
            Assert.Equal(19.50m, line.ExtendedLineCost);

            // RBAC: non-cost-view role with kitchen access sees masked cost
            db.Seed(new InventoryLocationUserMapping
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LocationId = kitchen.Id,
                IsActive = true
            });
            SilaCulinaryMaterialWorkspaceDto chefWs = await svc.GetWorkspaceAsync(
                buyer.OrganizationId, userId, Guid.NewGuid(), draft.Id, CancellationToken.None);
            Assert.False(chefWs.CanViewCost);
            Assert.Null(chefWs.Lines[0].UnitPrice);
            Assert.Equal("RESTRICTED", chefWs.Lines[0].PriceStatus);
        }

        [Fact]
        public async Task Confirm_BulkAccept_Concurrency_Recalc_And_Invalidation_ProtectConfirmedMappings()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);

            ItemBuyerMaster salt = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-SALT-01",
                Description = "FINE SEA SALT KG",
                BaseUnitOfMeasure = "KG",
                IsActive = true
            };
            ItemBuyerMaster oil = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-OIL-01",
                Description = "OLIVE OIL EXTRA VIRGIN L",
                BaseUnitOfMeasure = "L",
                IsActive = true
            };
            db.Seed(salt, oil);

            SilaCulinaryRecipeDraft draft = SeedDraft(db, buyer.Id, prop.Id, kitchen.Id,
                new CulinaryIngredientLineDto { TempRef = "ING-01", Description = "Fine Sea Salt", Quantity = 100m, RecipeUom = "G" },
                new CulinaryIngredientLineDto { TempRef = "ING-02", Description = "Extra Virgin Olive Oil", Quantity = 500m, RecipeUom = "ML" });

            int matchCall = 0;
            StubHttpMessageHandler handler = new(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith("/v1/materials/match-bulk"))
                {
                    matchCall++;
                    object c1 = BuildPyCandidate(salt.Id.ToString(), salt.MaterialCode, salt.Description, "KG", "EXACT_DESC", 1.0, true, true, 0.1m, 0.001m);
                    object c2 = BuildPyCandidate(oil.Id.ToString(), oil.MaterialCode, oil.Description, "L", "FUZZY", 0.88, true, true, 0.5m, 0.001m);
                    string json = JsonSerializer.Serialize(new
                    {
                        results = new[]
                        {
                            BuildPyResult("ING-01", "fine sea salt", "EXACT_DESC", "EXACT_MATCH", 1.0, c1),
                            BuildPyResult("ING-02", "extra virgin olive oil", "FUZZY", "SUGGESTED_MATCH", 0.88, c2)
                        }
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            });

            SilaCulinaryMaterialService svc = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);
            Guid userId = Guid.NewGuid();
            await svc.MatchDraftAsync(buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id,
                new SilaCulinaryMaterialMatchRequestDto(), CancellationToken.None);

            // Bulk accept confirms ONLY unambiguous EXACT_MATCH (ING-01), leaves SUGGESTED_MATCH (ING-02) unconfirmed
            SilaCulinaryMaterialWorkspaceDto afterBulk = await svc.BulkAcceptExactAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id,
                new SilaCulinaryMaterialBulkAcceptDto(), CancellationToken.None);
            Assert.Equal("CONFIRMED", afterBulk.Lines.First(x => x.IngredientTempRef == "ING-01").MappingStatus);
            Assert.Equal("SUGGESTED_MATCH", afterBulk.Lines.First(x => x.IngredientTempRef == "ING-02").MappingStatus);

            // Concurrency check: stale revision on ING-01 rejected
            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                svc.ConfirmMappingAsync(buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id, "ING-01",
                    new SilaCulinaryMaterialActionDto { ExpectedMappingRevision = 1, MaterialId = salt.Id }, CancellationToken.None));

            // Quantity-only scaling recalculation does NOT call Python matching endpoint
            int callsBeforeRecalc = matchCall;
            SilaCulinaryMaterialWorkspaceDto scaled = await svc.RecalculateAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id,
                new SilaCulinaryMaterialRecalcDto { Servings = 50m }, CancellationToken.None);
            Assert.Equal(callsBeforeRecalc, matchCall);
            Assert.Equal(0.5m, scaled.Lines.First(x => x.IngredientTempRef == "ING-01").ConvertedQuantity); // 100g * 5 = 500g = 0.5kg

            // Rematch without overwriteConfirmed preserves ING-01 CONFIRMED status
            SilaCulinaryMaterialWorkspaceDto rematched = await svc.MatchDraftAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id,
                new SilaCulinaryMaterialMatchRequestDto { OverwriteConfirmed = false }, CancellationToken.None);
            Assert.Equal("CONFIRMED", rematched.Lines.First(x => x.IngredientTempRef == "ING-01").MappingStatus);
        }

        [Fact]
        public async Task UnknownStockStaysUnknown_MassToVolumeIsUomConflict_AndExportSanitizesFormulas()
        {
            using TestDatabase db = new();
            var (buyer, prop, kitchen, _) = SeedTenant(db);

            ItemBuyerMaster flour = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                MaterialCode = "MAT-FLOUR",
                Description = "ALL PURPOSE FLOUR KG",
                BaseUnitOfMeasure = "KG",
                IsActive = true
            };
            db.Seed(flour);

            SilaCulinaryRecipeDraft draft = SeedDraft(db, buyer.Id, prop.Id, kitchen.Id,
                new CulinaryIngredientLineDto { TempRef = "ING-01", Description = "=Flour", Quantity = 2m, RecipeUom = "L" },
                new CulinaryIngredientLineDto { TempRef = "ING-02", Description = "Kasuri Methi", Quantity = 25m, RecipeUom = "G" });

            StubHttpMessageHandler handler = new(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith("/v1/materials/match-bulk"))
                {
                    object c1 = BuildPyCandidate(flour.Id.ToString(), flour.MaterialCode, flour.Description, "KG", "EXACT_DESC", 1.0, true, false, null);
                    string json = JsonSerializer.Serialize(new
                    {
                        results = new[]
                        {
                            BuildPyResult("ING-01", "flour", "EXACT_DESC", "UOM_CONFLICT", 1.0, c1),
                            BuildPyResult("ING-02", "kasuri methi", "NONE", "UNMATCHED", 0.0, null, Array.Empty<object>(), "NO_MATERIAL_FOUND", "NEW_MATERIAL")
                        }
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") };
            });

            SilaCulinaryMaterialService svc = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);
            Guid userId = Guid.NewGuid();
            SilaCulinaryMaterialWorkspaceDto ws = await svc.MatchDraftAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id,
                new SilaCulinaryMaterialMatchRequestDto(), CancellationToken.None);

            // 2 L vs KG without density -> UOM_CONFLICT, stock stays UNKNOWN (never 0)
            SilaCulinaryMaterialMappingLineDto flourLine = ws.Lines.First(x => x.IngredientTempRef == "ING-01");
            Assert.False(flourLine.UomCompatible);
            Assert.Equal("UOM_CONFLICT", flourLine.MappingStatus);
            Assert.Equal("UNKNOWN", flourLine.StockStatus);
            Assert.Null(flourLine.AvailableStock);

            // Missing material -> Prepare Request Draft -> Phase 1C handover blocked until complete
            SilaCulinaryMaterialMappingLineDto methiLine = ws.Lines.First(x => x.IngredientTempRef == "ING-02");
            Assert.Equal("UNMATCHED", methiLine.MappingStatus);
            SilaCulinaryMaterialMappingLineDto reqLine = await svc.PrepareRequestDraftAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef", draft.Id, "ING-02",
                new SilaCulinaryMaterialRequestDraftCreateDto
                {
                    RequestType = "NEW_MATERIAL",
                    ProposedDescription = "DRIED FENUGREEK LEAVES (KASURI METHI) 500G",
                    ProposedBaseUom = "KG",
                    Reason = "Required for butter chicken finishing"
                },
                CancellationToken.None);
            Assert.Equal("PENDING_MATERIAL_REQUEST", reqLine.MappingStatus);

            SilaCulinaryPhase1CHandoverDto handover = await svc.GetPhase1CHandoverAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, draft.Id, CancellationToken.None);
            Assert.False(handover.ReadyForPhase1C);
            Assert.Single(handover.MaterialRequestDrafts);

            // CSV formula injection protection
            var (csvBytes, _, _) = await svc.ExportReportAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, draft.Id, "CSV", null, CancellationToken.None);
            string csv = Encoding.UTF8.GetString(csvBytes);
            Assert.Contains("'=Flour", csv);
        }

        [Fact]
        public async Task TenantIsolation_AliasConflict_And_SubstitutionApply_WorkSafely()
        {
            using TestDatabase db = new();
            var (buyerA, propA, kitchenA, _) = SeedTenant(db);
            var (buyerB, _, _, _) = SeedTenant(db);

            ItemBuyerMaster oliveOil = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerA.Id,
                MaterialCode = "MAT-OLIVE-01",
                Description = "EXTRA VIRGIN OLIVE OIL 1L",
                BaseUnitOfMeasure = "L",
                IsActive = true
            };
            ItemBuyerMaster eggplant = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerA.Id,
                MaterialCode = "MAT-EGGPLANT-01",
                Description = "EGGPLANT FRESH KG",
                BaseUnitOfMeasure = "KG",
                IsActive = true
            };
            db.Seed(oliveOil, eggplant);

            SilaCulinaryRecipeDraft draftA = SeedDraft(db, buyerA.Id, propA.Id, kitchenA.Id,
                new CulinaryIngredientLineDto { TempRef = "ING-01", Description = "Unsalted Butter", Quantity = 1m, RecipeUom = "KG" });

            StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });
            SilaCulinaryMaterialService svc = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);
            Guid userA = Guid.NewGuid();

            // Tenant B blocked from reading Tenant A workspace
            await Assert.ThrowsAsync<NotFoundCustomException>(() =>
                svc.GetWorkspaceAsync(buyerB.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, draftA.Id, CancellationToken.None));

            // Hydrate mapping lines for Tenant A
            await svc.GetWorkspaceAsync(buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, draftA.Id, CancellationToken.None);

            // Alias creation & duplicate alias prevention
            SilaCulinaryMaterialAliasDto alias = await svc.CreateAliasAsync(
                buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "admin",
                new SilaCulinaryMaterialAliasCreateDto { AliasText = "Aubergine", MaterialId = eggplant.Id, Language = "en" },
                CancellationToken.None);
            Assert.Equal("APPROVED", alias.ApprovalStatus);

            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                svc.CreateAliasAsync(
                    buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "admin",
                    new SilaCulinaryMaterialAliasCreateDto { AliasText = "Aubergine", MaterialId = oliveOil.Id, Language = "en" },
                    CancellationToken.None));

            // Apply culinary substitution to draft only
            SilaCulinaryMaterialMappingLineDto sub = await svc.ApplySubstitutionAsync(
                buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "chef", draftA.Id, "ING-01",
                new SilaCulinarySubstitutionApplyDto
                {
                    ProposedIngredient = "Extra Virgin Olive Oil",
                    ProposedMaterialId = oliveOil.Id,
                    ReplacementRatio = 0.85m,
                    Reason = "Dairy-free guest request"
                },
                CancellationToken.None);
            Assert.True(sub.IsSubstitution);
            Assert.Equal("CONFIRMED", sub.MappingStatus);
            Assert.Equal("MAT-OLIVE-01", sub.MaterialCode);
            Assert.Equal(0.85m, sub.ReplacementRatio);
        }
    }
}
