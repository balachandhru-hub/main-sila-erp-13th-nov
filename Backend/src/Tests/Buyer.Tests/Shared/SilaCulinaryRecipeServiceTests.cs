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
    public class SilaCulinaryRecipeServiceTests
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
            public string? LastRequestBody { get; private set; }
            public int CallCount { get; private set; }

            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
            {
                _handler = handler;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CallCount++;
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

        private static (BuyerBusinessProfile Buyer, SilaCulinaryAiConfiguration Config) SeedBuyer(TestDatabase db, bool allowExternal = false)
        {
            Guid orgId = Guid.NewGuid();
            Guid buyerId = Guid.NewGuid();
            BuyerBusinessProfile buyer = new()
            {
                Id = buyerId,
                OrganizationId = orgId,
                OrganizationName = "Grand Culinary Hotel",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "chef@grandhotel.test",
                Phone = "+971500000000",
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
            SilaCulinaryAiConfiguration cfg = SilaCulinaryAiSettings.CreateDefault(buyerId);
            cfg.Id = Guid.NewGuid();
            cfg.AllowExternalAi = allowExternal;
            cfg.GeminiEnabled = allowExternal;
            cfg.ProtectedGeminiApiKey = allowExternal ? "ENC::AIzaSySecretKeyTest123456" : null;
            cfg.IsActive = true;
            db.Seed(buyer, cfg);
            return (buyer, cfg);
        }

        private static string BuildSamplePythonGenerateJson(string recipeName = "Butter Chicken") => JsonSerializer.Serialize(new
        {
            recipe = new
            {
                header = new
                {
                    proposed_name = recipeName,
                    description = "Rich tomato butter curry.",
                    cuisine = "Indian",
                    category = "Main Course",
                    recipe_type = "STANDARD_RECIPE",
                    base_servings = 10,
                    serving_size = 200,
                    serving_uom = "G",
                    prep_duration_minutes = 30,
                    cook_duration_minutes = 45,
                    total_duration_minutes = 75,
                    dietary_tags = new[] { "GLUTEN_FREE" },
                    potential_allergens = new[] { "DAIRY" },
                    chef_verification_required = true,
                    nutrition_status = "UNAVAILABLE_OR_UNVERIFIED"
                },
                ingredients = new[]
                {
                    new
                    {
                        temp_ref = "ING-01",
                        description = "=Boneless chicken",
                        normalized_description = "boneless chicken",
                        category = "PROTEIN",
                        quantity = 1.8m,
                        recipe_uom = "KG",
                        normalized_uom = "KG",
                        uom_resolved = true,
                        quantity_basis = "RAW",
                        yield_percent = 100m,
                        scaling_method = "LINEAR",
                        material_mapping_status = "PENDING_MATERIAL_MAPPING",
                        material_reference = (string?)null
                    }
                },
                instructions = new[]
                {
                    new
                    {
                        step_number = 1,
                        step_title = "Simmer sauce",
                        instruction = "Simmer tomato puree with butter and spices.",
                        estimated_duration_minutes = 20
                    }
                },
                sub_recipes = Array.Empty<object>(),
                assumptions = new[] { "Standard hotel kitchen equipment." },
                warnings = Array.Empty<string>(),
                chef_notes = "",
                cost_validation_note = "Cost validation pending material matching."
            },
            ai_metadata = new
            {
                provider = "LOCAL",
                model = "qwen2.5:7b-instruct",
                prompt_version = "culinary-recipe-v1.0",
                generation_reference = "GEN-TEST-001"
            },
            validation_warnings = Array.Empty<object>(),
            validation_status = "VALID_WITH_CHEF_REVIEW",
            used_fallback = false,
            usage = new
            {
                input_tokens = 320,
                output_tokens = 480,
                total_tokens = 800,
                estimated_cost_usd = 0.002m,
                latency_ms = 650,
                validation_retries = 0
            }
        });

        [Fact]
        public async Task Generate_PersistsDraftRevisionUsageAndTrackerEvents()
        {
            using TestDatabase db = new();
            var (buyer, _) = SeedBuyer(db);
            StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildSamplePythonGenerateJson(), Encoding.UTF8, "application/json")
            });
            SilaCulinaryRecipeService service = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);

            Guid userId = Guid.NewGuid();
            CulinaryRecipeDraftDetailDto draft = await service.GenerateAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef@grandhotel.test",
                new CulinaryGenerateRecipeRequestDto { RecipeName = "Butter Chicken", Servings = 10, ServingSize = 200 },
                CancellationToken.None);

            Assert.StartsWith("AIR-", draft.DraftReference);
            Assert.Equal(1, draft.RevisionNumber);
            Assert.Equal("LOCAL", draft.Provider);
            Assert.Equal(800, draft.TotalTokens);
            Assert.Single(draft.Payload.Ingredients);
            Assert.Equal("PENDING_MATERIAL_MAPPING", draft.Payload.Ingredients[0].MaterialMappingStatus);
            Assert.Null(draft.Payload.Ingredients[0].MaterialReference);
            Assert.Single(db.Context.SilaCulinaryAiUsageLog);
            Assert.True(db.Context.InventoryWorkflowEvent.Count() >= 4);
        }

        [Fact]
        public async Task Generate_EnforcesDailyRequestLimitAndBlocksUnauthorizedProvider()
        {
            using TestDatabase db = new();
            var (buyer, cfg) = SeedBuyer(db, allowExternal: false);
            cfg.DailyRequestLimit = 1;
            db.Context.Update(cfg);
            db.Context.SaveChanges();
            db.Context.ChangeTracker.Clear();

            StubHttpMessageHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(BuildSamplePythonGenerateJson(), Encoding.UTF8, "application/json")
            });
            SilaCulinaryRecipeService service = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);

            // Unauthorized external provider blocked before calling Python
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                service.GenerateAsync(buyer.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, "chef",
                    new CulinaryGenerateRecipeRequestDto { RecipeName = "Pasta", PreferredProvider = "GEMINI" },
                    CancellationToken.None));

            // Non-admin/non-outlet-manager cannot override provider
            await Assert.ThrowsAsync<ForBiddenCustomException>(() =>
                service.GenerateAsync(buyer.OrganizationId, Guid.NewGuid(), Guid.NewGuid(), "junior",
                    new CulinaryGenerateRecipeRequestDto { RecipeName = "Pasta", PreferredProvider = "GEMINI" },
                    CancellationToken.None));

            // First generation succeeds
            await service.GenerateAsync(buyer.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, "chef",
                new CulinaryGenerateRecipeRequestDto { RecipeName = "Butter Chicken" },
                CancellationToken.None);

            // Second generation hits daily request limit
            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                service.GenerateAsync(buyer.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, "chef",
                    new CulinaryGenerateRecipeRequestDto { RecipeName = "Biryani" },
                    CancellationToken.None));
            Assert.Equal(1, handler.CallCount);
        }

        [Fact]
        public async Task Failure_PreservesExistingDraftAndMasksSecretsInLogs()
        {
            using TestDatabase db = new();
            var (buyer, _) = SeedBuyer(db);
            int call = 0;
            StubHttpMessageHandler handler = new(req =>
            {
                call++;
                if (call == 1)
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(BuildSamplePythonGenerateJson("Initial Pasta"), Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.BadGateway)
                {
                    Content = new StringContent("{\"detail\":\"Upstream error with key AIzaSySecretKey999999999999\"}", Encoding.UTF8, "application/json")
                };
            });

            SilaCulinaryRecipeService service = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);
            Guid userId = Guid.NewGuid();
            CulinaryRecipeDraftDetailDto created = await service.GenerateAsync(
                buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef",
                new CulinaryGenerateRecipeRequestDto { RecipeName = "Initial Pasta" },
                CancellationToken.None);

            BadRequestCustomException ex = await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                service.GenerateAsync(buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, "chef",
                    new CulinaryGenerateRecipeRequestDto { DraftId = created.Id, RecipeName = "Broken Update" },
                    CancellationToken.None));

            Assert.DoesNotContain("AIzaSySecretKey", ex.Description);
            CulinaryRecipeDraftDetailDto reloaded = await service.GetDraftAsync(buyer.OrganizationId, userId, Common.BUYER_ADMIN_ROLE_ID, created.Id, CancellationToken.None);
            Assert.Equal("Initial Pasta", reloaded.RecipeName);
            Assert.Equal(1, reloaded.RevisionNumber);
            SilaCulinaryAiUsageLog failLog = db.Context.SilaCulinaryAiUsageLog.First(x => !x.Success);
            Assert.DoesNotContain("AIzaSySecretKey", failLog.SanitizedError ?? "");
        }

        [Fact]
        public async Task SaveDraft_EnforcesConcurrency_TenantIsolation_AndCsvSanitization()
        {
            using TestDatabase db = new();
            var (buyerA, _) = SeedBuyer(db);
            var (buyerB, _) = SeedBuyer(db);

            StubHttpMessageHandler handler = new(req =>
            {
                if (req.RequestUri!.AbsolutePath.EndsWith("/v1/recipes/validate"))
                {
                    string body = BuildSamplePythonGenerateJson("Butter Chicken v2");
                    using JsonDocument doc = JsonDocument.Parse(body);
                    string recipeJson = doc.RootElement.GetProperty("recipe").GetRawText();
                    string resp = $"{{\"validation_status\":\"VALID_WITH_CHEF_REVIEW\",\"warnings\":[],\"recipe\":{recipeJson}}}";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(resp, Encoding.UTF8, "application/json")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BuildSamplePythonGenerateJson(), Encoding.UTF8, "application/json")
                };
            });

            SilaCulinaryRecipeService service = new(db.Repository, new FakeHttpClientFactory(handler), new FakeProtector(), BuildConfig(), db.Logger);
            Guid userA = Guid.NewGuid();
            CulinaryRecipeDraftDetailDto draft = await service.GenerateAsync(
                buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "chefA",
                new CulinaryGenerateRecipeRequestDto { RecipeName = "Butter Chicken" },
                CancellationToken.None);

            // Tenant B cannot open Tenant A draft
            await Assert.ThrowsAsync<NotFoundCustomException>(() =>
                service.GetDraftAsync(buyerB.OrganizationId, Guid.NewGuid(), Common.BUYER_ADMIN_ROLE_ID, draft.Id, CancellationToken.None));

            // Save succeeds at rev 1 -> rev 2
            CulinaryRecipeDraftDetailDto saved = await service.SaveDraftAsync(
                buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "chefA", draft.Id,
                new CulinarySaveDraftRequestDto
                {
                    ExpectedRevisionNumber = 1,
                    ChangeSummary = "Adjusted seasoning",
                    Payload = draft.Payload
                },
                CancellationToken.None);
            Assert.Equal(2, saved.RevisionNumber);

            // Stale revision 1 rejected
            await Assert.ThrowsAsync<BadRequestCustomException>(() =>
                service.SaveDraftAsync(
                    buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "chefA", draft.Id,
                    new CulinarySaveDraftRequestDto { ExpectedRevisionNumber = 1, Payload = draft.Payload },
                    CancellationToken.None));

            // Restore revision 1 -> rev 3
            CulinaryRecipeDraftDetailDto restored = await service.RestoreRevisionAsync(
                buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, "chefA", draft.Id, 1, CancellationToken.None);
            Assert.Equal(3, restored.RevisionNumber);

            // CSV export neutralizes leading '=' formula injection
            var (csvBytes, contentType, _) = await service.ExportDraftAsync(
                buyerA.OrganizationId, userA, Common.BUYER_ADMIN_ROLE_ID, draft.Id, "CSV", CancellationToken.None);
            Assert.Equal("text/csv", contentType);
            string csv = Encoding.UTF8.GetString(csvBytes);
            Assert.Contains("\"'=Boneless chicken\"", csv);
        }
    }
}
