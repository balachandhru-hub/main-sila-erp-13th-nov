using System.Net;
using Buyer.Application.Contracts;
using Buyer.Application.Features.Commands.CreatePurchaseOrder;
using Buyer.Application.Features.Commands.CreateSilaTransfer;
using Buyer.Application.Features.Shared;
using Buyer.Application.Services.Integration;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IServices;
using Buyer.Tests.Support;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Dtos;
using SharedKernel.Integration.Services;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaCulinaryProcurementServiceTests
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

        private sealed class FakeUserContext : IUserContext
        {
            private Guid _userId;
            public Guid GetCurrentUserId() => _userId;
            public void SetCurrentUserId(Guid userId) => _userId = userId;
        }

        private sealed class FakeBuyerPurchaseDocumentGateway : IBuyerPurchaseDocumentGateway
        {
            public int ErpCallCount { get; private set; }

            public Task<IntegrationApiDto?> FindPurchaseOrderApiAsync(Guid buyerOrganizationId, string? companyCode, CancellationToken cancellationToken)
                => Task.FromResult<IntegrationApiDto?>(null);

            public Task<ExternalCallResult> CreateBuyerPurchaseDocumentAsync(IntegrationApiDto api, BuyerPurchaseDocumentRequest request, CancellationToken cancellationToken)
            {
                ErpCallCount++;
                return Task.FromResult(new ExternalCallResult { Succeeded = false, ErrorMessage = "ERP calls must not run on deferred drafts." });
            }
        }

        private sealed class FakeSupplierApiClient : ISupplierApiClient
        {
            public Task<ExternalSupplierSessionDto?> ValidateExternalSessionToken(string sessionToken, Guid rfqId, CancellationToken ct = default) => Task.FromResult<ExternalSupplierSessionDto?>(null);
            public Task CreateSupplierRFQ(CreateSupplierRFQRequestDto rfq, CancellationToken ct = default) => Task.CompletedTask;
            public Task<GetAllSupplierQuotationDto> GetSupplierQuotation(Guid RFQId, CancellationToken ct = default) => Task.FromResult(new GetAllSupplierQuotationDto());
            public Task<SupplierProfileDto> GetSupplierById(Guid supplierId, CancellationToken ct = default) => Task.FromResult(new SupplierProfileDto());
            public Task<List<SupplierNameDto>> GetSupplierNamesByIds(List<Guid> supplierIds, CancellationToken ct = default) => Task.FromResult(new List<SupplierNameDto>());
            public Task<GetQuestionsAnswersForSupplierDto> GetQuestionsAnswersForSupplier(Guid requestId, CancellationToken ct = default) => Task.FromResult(new GetQuestionsAnswersForSupplierDto());
            public Task<SupplierRFQAnswerDto?> GetSupplierRFQAnswers(Guid buyerRFQId, CancellationToken ct = default) => Task.FromResult<SupplierRFQAnswerDto?>(null);
            public Task<Guid> GetSupplierId(CancellationToken ct = default) => Task.FromResult(Guid.Empty);
            public Task UpdateSupplierRFQStatus(Guid rfqId, string status, CancellationToken ct = default) => Task.CompletedTask;
            public Task SaveSupplierRFQAward(SupplierRFQAwardRequestDto request, CancellationToken ct = default) => Task.CompletedTask;
            public Task ResetSupplierRFQAward(Guid buyerRFQId, CancellationToken ct = default) => Task.CompletedTask;
            public Task<BidCompareResponseDto> GetBidCompare(Guid rfqId, CancellationToken ct = default) => Task.FromResult(new BidCompareResponseDto());
            public Task NotifyNewMessage(MessageResponseDto message, CancellationToken ct = default) => Task.CompletedTask;
            public Task<List<RFQTermsConditionDto>> GetRFQTermsCondition(Guid rfqId, CancellationToken ct = default) => Task.FromResult(new List<RFQTermsConditionDto>());
            public Task<List<RFQESignDto>> GetRFQESign(Guid rfqId, CancellationToken ct = default) => Task.FromResult(new List<RFQESignDto>());
            public Task<List<BuyerTermsAndConditionStatusDto>> GetBuyerTermsConditionStatus(Guid rfqId, CancellationToken ct = default) => Task.FromResult(new List<BuyerTermsAndConditionStatusDto>());
            public Task InviteSupplierForContract(Guid rfqId, Guid supplierId, CancellationToken ct = default) => Task.CompletedTask;
            public Task<BuyerCatalogItemDto?> GetBuyerCatalogById(Guid catalogId, CancellationToken ct = default) => Task.FromResult<BuyerCatalogItemDto?>(null);
            public Task<List<BuyerCatalogItemDto>> GetBuyerCatalogStock(List<Guid> catalogIds, CancellationToken ct = default) => Task.FromResult(new List<BuyerCatalogItemDto>());
            public Task<List<BuyerCatalogItemDto>> GetBuyerCatalogAlternatives(Guid catalogId, decimal quantity, CancellationToken ct = default) => Task.FromResult(new List<BuyerCatalogItemDto>());
        }

        private sealed class FakeSupplierSalesOrderClient : ISupplierSalesOrderClient
        {
            public int SupplierCallCount { get; private set; }
            public Task<SupplierSalesOrderResultDto> SendSalesOrderAsync(SupplierSalesOrderRequestDto order, CancellationToken ct = default)
            {
                SupplierCallCount++;
                return Task.FromResult(new SupplierSalesOrderResultDto { Succeeded = false });
            }
        }

        private sealed class DispatchingMediator : IMediator
        {
            private readonly TestDatabase _db;
            public FakeBuyerPurchaseDocumentGateway Gateway { get; } = new();
            public FakeSupplierSalesOrderClient SupplierClient { get; } = new();

            public DispatchingMediator(TestDatabase db) => _db = db;

            public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            {
                if (request is CreateSilaTransferCommand transferCmd)
                {
                    CreateSilaTransferCommandHandler handler = new(_db.Repository, _db.Logger);
                    Guid res = await handler.Handle(transferCmd, cancellationToken);
                    return (TResponse)(object)res;
                }
                if (request is CreatePurchaseOrderCommand poCmd)
                {
                    CreatePurchaseOrderCommandHandler handler = new(
                        _db.Repository,
                        new FakeSupplierApiClient(),
                        SupplierClient,
                        Gateway,
                        new FakeUserContext(),
                        _db.Logger);
                    PurchaseOrderProcessResultDto res = await handler.Handle(poCmd, cancellationToken);
                    return (TResponse)(object)res;
                }
                throw new NotSupportedException($"Unexpected request {request.GetType().Name}");
            }

            public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
                => Task.CompletedTask;

            public Task<object?> Send(object request, CancellationToken cancellationToken = default)
                => Task.FromResult<object?>(null);

            public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
        }

        private sealed record TestFixture(
            TestDatabase Db,
            SilaCulinaryProcurementService Service,
            DispatchingMediator Mediator,
            BuyerBusinessProfile Buyer,
            BuyerProperty Property,
            InventoryLocation MainKitchen,
            InventoryLocation OutletB,
            SilaSupplier Supplier,
            Guid AdminUserId);

        private static TestFixture CreateFixture()
        {
            TestDatabase db = new();
            Guid orgId = Guid.NewGuid();
            Guid buyerId = Guid.NewGuid();
            Guid adminUserId = Guid.NewGuid();

            BuyerBusinessProfile buyer = new()
            {
                Id = buyerId,
                OrganizationId = orgId,
                OrganizationName = "SILA Grand Hospitality",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "procurement@sila.test",
                Phone = "+971500000001",
                Country = "UAE",
                AddressLine1 = "Palm Jumeirah",
                City = "Dubai",
                State = "Dubai",
                PinCode = "00000",
                Industry = "Hospitality",
                BusinessType = "Resort",
                Currency = "AED",
                Status = "ACTIVE",
                IsActive = true,
            };

            BuyerProperty property = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                CompanyCode = "1000",
                PlantCode = "1060",
                PropertyName = "Grand Resort Dubai",
                IsActive = true,
            };

            InventoryLocation mainKitchen = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                PropertyId = property.Id,
                LocationCode = "KIT-MAIN",
                LocationName = "Main Banquet Kitchen",
                LocationType = "KITCHEN",
                StorageLocationCode = "FL01",
                InventoryEnabled = true,
                TransferEnabled = true,
                ConsumptionEnabled = true,
                IsActive = true,
            };

            InventoryLocation outletB = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                PropertyId = property.Id,
                LocationCode = "OUT-SEA",
                LocationName = "Seafood Grill Outlet",
                LocationType = Common.SILA_LOCATION_OUTLET,
                StorageLocationCode = "FL02",
                InventoryEnabled = true,
                TransferEnabled = true,
                ConsumptionEnabled = true,
                IsActive = true,
            };

            SilaSupplier supplier = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                SupplierCode = "SUP-SEAFOOD-01",
                Name = "Emirates Fresh Seafood LLC",
                Currency = "AED",
                Status = "ACTIVE",
                IsActive = true,
            };

            SilaUserPurchasingProfile profile = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                UserId = adminUserId,
                PropertyId = property.Id,
                CompanyCode = "1000",
                PlantCode = "1060",
                StorageLocation = "FL01",
                PurchasingOrganization = "1000",
                PurchasingGroup = "P01",
                AccountAssignmentCategory = "K",
                CostCenter = "CC-BANQ-01",
                GlAccount = "500100",
                DefaultPoDocumentType = "NB",
                DefaultCurrency = "AED",
                IsDefault = true,
                IsActive = true,
            };

            MasterApprovalFlow transferFlow = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ApprovalCode = "TRF-ALL",
                ApprovalName = "Default Transfer Approval",
                Type = Common.SILA_APPROVAL_TYPE_ITO,
                Currency = "AED",
                ScopeKind = Common.SILA_SCOPE_ALL,
                IsActive = true,
            };

            ApprovalFlowUserMapping transferApprover = new()
            {
                Id = Guid.NewGuid(),
                ApprovalFlowId = transferFlow.Id,
                UserId = adminUserId,
                Order = 1,
                IsActive = true,
            };

            db.Seed(buyer, property, mainKitchen, outletB, supplier, profile, transferFlow, transferApprover);

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();
            DispatchingMediator mediator = new(db);
            SilaCulinaryProcurementService svc = new(db.Repository, db.Logger, new FakeHttpClientFactory(), config, new FakeProtector(), mediator);
            return new TestFixture(db, svc, mediator, buyer, property, mainKitchen, outletB, supplier, adminUserId);
        }

        private static (SilaCulinaryEvent Ev, SilaCulinaryMrpRun Mrp, ItemBuyerMaster Material) SeedApprovedEventMrp(
            TestFixture fx,
            string eventCode,
            string eventName,
            string matCode,
            string matDesc,
            decimal grossQty,
            decimal unitCost,
            DateTime requiredByUtc,
            string mrpStatus = "APPROVED",
            bool isApprovedBaseline = true,
            string orderUom = "KG",
            decimal? packConversionQty = null,
            string? inventoryType = "STOCK")
        {
            ItemBuyerMaster mat = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MaterialCode = matCode,
                Description = matDesc,
                MaterialGroup = "Seafood",
                BaseUnitOfMeasure = "KG",
                OrderUnitOfMeasure = orderUom,
                UnitOfMeasureMapping = "KG",
                UnitCost = unitCost,
                Currency = "AED",
                InventoryType = inventoryType,
                IsInventoryItem = true,
                IsActive = true,
            };
            fx.Db.Seed(mat);

            if (packConversionQty.HasValue && packConversionQty.Value > 0 && orderUom != "KG")
            {
                fx.Db.Seed(new MaterialUomConversion
                {
                    Id = Guid.NewGuid(),
                    MaterialId = mat.Id,
                    FromUom = orderUom,
                    ToUom = "KG",
                    Factor = packConversionQty.Value,
                    IsActive = true,
                });
            }

            SilaCulinaryEvent ev = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                EventCode = eventCode,
                Name = eventName,
                EventType = "Corporate Gala",
                Status = "APPROVED",
                PropertyId = fx.Property.Id,
                CompanyCode = "1000",
                PlantCode = "1060",
                VenueName = "Grand Ballroom",
                ResponsibleChef = "Chef Tariq",
                StartDateUtc = requiredByUtc.AddHours(6),
                EndDateUtc = requiredByUtc.AddHours(12),
                PrepStartUtc = requiredByUtc,
                PlanningGuests = 250,
                GuaranteedGuests = 220,
                Currency = "AED",
                IsActive = true,
            };
            fx.Db.Seed(ev);

            SilaCulinaryMrpRun mrp = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MrpCode = $"MRP-{eventCode}",
                Name = $"MRP for {eventName}",
                RunScope = "SINGLE_EVENT",
                Status = mrpStatus,
                VersionNumber = 1,
                IsApprovedBaseline = isApprovedBaseline,
                PropertyId = fx.Property.Id,
                PropertyName = fx.Property.PropertyName,
                CompanyCode = "1000",
                PlantCode = "1060",
                Currency = "AED",
                EventReferencesJson = $"[\"{eventCode}\"]",
                TotalEvents = 1,
                TotalUniqueMaterials = 1,
                IsActive = true,
            };
            fx.Db.Seed(mrp);

            SilaCulinaryMrpLine line = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MrpRunId = mrp.Id,
                ConsolidationKey = $"{matCode}|{requiredByUtc:yyyy-MM-dd}",
                MaterialId = mat.Id,
                MaterialCode = matCode,
                MaterialDescription = matDesc,
                MaterialCategory = "Seafood",
                IsResolvedMaterial = true,
                PropertyId = fx.Property.Id,
                PlantCode = "1060",
                KitchenLocationId = fx.MainKitchen.Id,
                KitchenName = fx.MainKitchen.LocationName,
                StorageLocationCode = fx.MainKitchen.StorageLocationCode,
                RequiredDateUtc = requiredByUtc,
                RequiredDateBucket = requiredByUtc.ToString("yyyy-MM-dd"),
                BaseUom = "KG",
                EdibleQty = grossQty,
                RawQty = grossQty,
                GrossQty = grossQty,
                PreparedStockQty = 0m,
                IndicativeNewProductionQty = grossQty,
                UnitCost = unitCost,
                Currency = "AED",
                RequirementValue = decimal.Round(grossQty * unitCost, 2),
                ContributingEventsJson = $"[\"{eventCode}\"]",
                IsActive = true,
            };
            fx.Db.Seed(line);

            return (ev, mrp, mat);
        }

        [Fact]
        public async Task Section63_GrandCorporateGala_EndToEnd_ComputesExactCoverageAndExcludesUnapprovedItoFromConfirmed()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(4);
            var (_, mrp, mat) = SeedApprovedEventMrp(
                fx, "EVT-GALA-01", "Grand Corporate Gala", "MAT-SALMON", "Atlantic Salmon",
                grossQty: 100m, unitCost: 85m, requiredByUtc: reqBy, orderUom: "CASE", packConversionQty: 5m);

            // Local kitchen stock: 35 KG on hand, 10 KG competing reservation (via open ITO), 5 KG safety stock -> 20 KG eligible
            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.MainKitchen.Id,
                MaterialId = mat.Id,
                OnHandQty = 35m,
                BaseUom = "KG",
                UnitCost = 85m,
                IsActive = true,
            });
            fx.Db.Seed(new InventoryLocationMaterial
            {
                Id = Guid.NewGuid(),
                LocationId = fx.MainKitchen.Id,
                MaterialId = mat.Id,
                SafetyStock = 5m,
                IsActive = true,
            });
            InternalTransferOrder competingIto = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                ItoNumber = "ITO-COMP-01",
                FromLocationId = fx.MainKitchen.Id,
                ToLocationId = fx.OutletB.Id,
                Status = Common.SILA_ITO_APPROVED,
                IsActive = true,
            };
            fx.Db.Seed(competingIto);
            fx.Db.Seed(new InternalTransferOrderItem
            {
                Id = Guid.NewGuid(),
                InternalTransferOrderId = competingIto.Id,
                MaterialId = mat.Id,
                RequestedQty = 10m,
                ApprovedQty = 10m,
                IsActive = true,
            });

            // Confirmed incoming PO before required date: 20 KG
            PurchaseOrder incomingPo = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                BuyerOrganizationId = fx.Buyer.OrganizationId,
                PoNumber = "PO-INCOMING-01",
                SupplierId = fx.Supplier.Id,
                SupplierName = fx.Supplier.Name,
                Status = "APPROVED",
                DeliveryDate = reqBy.AddDays(-1),
                Currency = "AED",
                IsActive = true,
            };
            fx.Db.Seed(incomingPo);
            fx.Db.Seed(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = incomingPo.Id,
                LineNumber = 1,
                MaterialCode = "MAT-SALMON",
                ProductName = "Atlantic Salmon",
                Quantity = 20m,
                ReceivedQuantity = 0m,
                UnitPrice = 85m,
                Currency = "AED",
                IsActive = true,
            });

            // Eligible transferable stock at Outlet B: 15 KG
            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.OutletB.Id,
                MaterialId = mat.Id,
                OnHandQty = 15m,
                BaseUom = "KG",
                UnitCost = 85m,
                IsActive = true,
            });

            SilaCulinaryProcurementPlanDetailDto detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Procurement Lead",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id, IncludeProposedItoInNet = true },
                CancellationToken.None);

            SilaCulinaryProcurementLineDto line = Assert.Single(detail.Lines);
            Assert.Equal(100m, line.GrossRequirementQty);
            Assert.Equal(35m, line.LocalOnHandQty);
            Assert.Equal(10m, line.CompetingReservationsQty);
            Assert.Equal(5m, line.SafetyStockQty);
            Assert.Equal(20m, line.EligibleLocalStockQty);
            Assert.Equal(20m, line.ProposedReservationQty);
            Assert.Equal(20m, line.IncomingPoAllocatedQty);
            Assert.Equal(15m, line.ProposedItoQty);
            // Confirmed coverage = 20 local + 20 PO = 40 KG (does NOT count unapproved ITO 15 KG!)
            Assert.Equal(40m, line.ConfirmedCoverageQty);
            Assert.Equal(60m, line.RemainingConfirmedGapQty);
            // Net uncovered after proposed ITO = 45 KG, pack size 5 KG -> 45 KG @ AED 85 = AED 3,825
            Assert.Equal(45m, line.NetUncoveredQty);
            Assert.Equal(45m, line.RecommendedPurchaseQty);
            Assert.Equal(3825m, line.EstimatedPurchaseValue);
        }

        [Fact]
        public async Task ScenarioA_PackRounding_31KgIn5KgPacks_Recommends35KgAnd4KgExcess()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(5);
            var (_, mrp, _) = SeedApprovedEventMrp(
                fx, "EVT-PACK-01", "Pack Rounding Gala", "MAT-PRAWN", "Tiger Prawns",
                grossQty: 31m, unitCost: 60m, requiredByUtc: reqBy, orderUom: "BOX5", packConversionQty: 5m);

            SilaCulinaryProcurementPlanDetailDto detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id },
                CancellationToken.None);

            SilaCulinaryProcurementLineDto line = Assert.Single(detail.Lines);
            Assert.Equal(31m, line.NetUncoveredQty);
            Assert.Equal(5m, line.PackSizeQty);
            Assert.Equal(35m, line.RecommendedPurchaseQty);
            Assert.Equal(4m, line.ExpectedExcessQty);
            Assert.Equal(7, line.NumberOfPacks);
        }

        [Fact]
        public async Task ScenarioB_ConcurrentReservations_NeverExceeds100KgAvailable()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(4);
            var (_, mrpA, mat) = SeedApprovedEventMrp(fx, "EVT-CONC-A", "Event A", "MAT-BEEF", "Wagyu Tenderloin", 70m, 150m, reqBy);

            SilaCulinaryMrpRun mrpB = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MrpCode = "MRP-CONC-B",
                Name = "Event B MRP",
                RunScope = "SINGLE_EVENT",
                Status = "APPROVED",
                VersionNumber = 1,
                IsApprovedBaseline = true,
                PropertyId = fx.Property.Id,
                CompanyCode = "1000",
                PlantCode = "1060",
                Currency = "AED",
                IsActive = true,
            };
            fx.Db.Seed(mrpB);
            fx.Db.Seed(new SilaCulinaryMrpLine
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MrpRunId = mrpB.Id,
                ConsolidationKey = $"MAT-BEEF|{reqBy:yyyy-MM-dd}",
                MaterialId = mat.Id,
                MaterialCode = "MAT-BEEF",
                MaterialDescription = "Wagyu Tenderloin",
                IsResolvedMaterial = true,
                PropertyId = fx.Property.Id,
                PlantCode = "1060",
                KitchenLocationId = fx.MainKitchen.Id,
                KitchenName = fx.MainKitchen.LocationName,
                StorageLocationCode = fx.MainKitchen.StorageLocationCode,
                RequiredDateUtc = reqBy,
                RequiredDateBucket = reqBy.ToString("yyyy-MM-dd"),
                BaseUom = "KG",
                GrossQty = 60m,
                IndicativeNewProductionQty = 60m,
                UnitCost = 150m,
                IsActive = true,
            });

            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.MainKitchen.Id,
                MaterialId = mat.Id,
                OnHandQty = 100m,
                BaseUom = "KG",
                UnitCost = 150m,
                IsActive = true,
            });

            var planA = await fx.Service.CreateAndCalculatePlanAsync(fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "UserA",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrpA.Id }, CancellationToken.None);
            var planB = await fx.Service.CreateAndCalculatePlanAsync(fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "UserB",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrpB.Id }, CancellationToken.None);

            // Reserve 70 KG for A and 60 KG for B concurrently
            Task<SilaCulinaryStockReservationDto> t1 = fx.Service.CreateReservationAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "UserA", planA.Summary.Id,
                new SilaCulinaryCreateReservationRequestDto
                {
                    ProcurementLineId = planA.Lines[0].Id,
                    LocationId = fx.MainKitchen.Id,
                    Quantity = 70m,
                    ActivateImmediately = true,
                }, CancellationToken.None);

            Task<SilaCulinaryStockReservationDto> t2 = fx.Service.CreateReservationAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "UserB", planB.Summary.Id,
                new SilaCulinaryCreateReservationRequestDto
                {
                    ProcurementLineId = planB.Lines[0].Id,
                    LocationId = fx.MainKitchen.Id,
                    Quantity = 60m,
                    ActivateImmediately = true,
                }, CancellationToken.None);

            SilaCulinaryStockReservationDto[] results = await Task.WhenAll(t1, t2);
            decimal totalActive = results.Sum(r => r.ReservedQty);
            Assert.Equal(100m, totalActive);
            Assert.Equal(0, await fx.Db.Context.Set<InventoryTransaction>().CountAsync());
        }

        [Fact]
        public async Task ScenarioC_LateIncomingPo_ExcludedFromTimelyCoverage()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(3);
            var (_, mrp, _) = SeedApprovedEventMrp(fx, "EVT-LATE-01", "Late PO Test", "MAT-TRUFFLE", "Black Truffle", 40m, 400m, reqBy);

            PurchaseOrder latePo = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                BuyerOrganizationId = fx.Buyer.OrganizationId,
                PoNumber = "PO-LATE-01",
                SupplierId = fx.Supplier.Id,
                SupplierName = fx.Supplier.Name,
                Status = "APPROVED",
                DeliveryDate = reqBy.AddDays(2),
                IsActive = true,
            };
            fx.Db.Seed(latePo);
            fx.Db.Seed(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = latePo.Id,
                LineNumber = 1,
                MaterialCode = "MAT-TRUFFLE",
                ProductName = "Black Truffle",
                Quantity = 40m,
                ReceivedQuantity = 0m,
                UnitPrice = 400m,
                IsActive = true,
            });

            SilaCulinaryProcurementPlanDetailDto detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            SilaCulinaryProcurementLineDto line = Assert.Single(detail.Lines);
            Assert.Equal(0m, line.IncomingPoAllocatedQty);
            Assert.Equal(40m, line.NetUncoveredQty);
            Assert.Contains(detail.Exceptions, e => e.ExceptionType == "LATE_INCOMING_PO");
        }

        [Fact]
        public async Task ScenarioD_PartialGrnAndGrnReversal_ReconcileIncomingAllocationAndReservation()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(4);
            var (_, mrp, mat) = SeedApprovedEventMrp(fx, "EVT-GRN-01", "GRN Reconciliation", "MAT-LOBSTER", "Maine Lobster", 50m, 120m, reqBy);

            PurchaseOrder po = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                BuyerOrganizationId = fx.Buyer.OrganizationId,
                PoNumber = "PO-LOBSTER-50",
                SupplierId = fx.Supplier.Id,
                SupplierName = fx.Supplier.Name,
                Status = "APPROVED",
                DeliveryDate = reqBy.AddDays(-1),
                IsActive = true,
            };
            fx.Db.Seed(po);
            PurchaseOrderItem poItem = new()
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                LineNumber = 1,
                MaterialCode = "MAT-LOBSTER",
                ProductName = "Maine Lobster",
                Quantity = 50m,
                ReceivedQuantity = 0m,
                UnitPrice = 120m,
                IsActive = true,
            };
            fx.Db.Seed(poItem);

            var plan = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            Assert.Equal(50m, plan.Lines[0].IncomingPoAllocatedQty);

            // Simulate partial GRN of 20 KG
            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.MainKitchen.Id,
                MaterialId = mat.Id,
                OnHandQty = 20m,
                BaseUom = "KG",
                IsActive = true,
            });
            GoodsReceipt grn = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                GrnNumber = "GRN-0001",
                PurchaseOrderId = po.Id,
                LocationId = fx.MainKitchen.Id,
                Status = Common.SILA_GRN_POSTED,
                IsActive = true,
            };
            fx.Db.Seed(grn);
            GoodsReceiptItem grnItem = new()
            {
                Id = Guid.NewGuid(),
                GoodsReceiptId = grn.Id,
                PurchaseOrderItemId = poItem.Id,
                MaterialId = mat.Id,
                MaterialCode = "MAT-LOBSTER",
                AcceptedQty = 20m,
                ReversedQty = 0m,
                BatchNumber = "BATCH-L1",
                ExpiryDate = reqBy.AddDays(5),
                IsActive = true,
            };
            fx.Db.Seed(grnItem);

            var afterGrn = await fx.Service.ReconcileGrnAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Receiver",
                plan.Summary.Id,
                new SilaCulinaryReconcileGrnRequestDto
                {
                    GoodsReceiptId = grn.Id,
                    IsReversal = false,
                }, CancellationToken.None);

            var poAlloc = Assert.Single(afterGrn.Allocations, a => a.AllocationType == "INCOMING_PO");
            Assert.Equal(30m, poAlloc.RemainingQty);
            Assert.Equal("PARTIALLY_RECEIVED", poAlloc.Status);
            var activeRes = Assert.Single(afterGrn.Reservations, r => r.Status == "ACTIVE");
            Assert.Equal(20m, activeRes.ReservedQty);

            // Now mark GRN item reversed and reconcile GRN reversal
            var trackedGrnItem = await fx.Db.Context.Set<GoodsReceiptItem>().FirstAsync(x => x.Id == grnItem.Id);
            trackedGrnItem.ReversedQty = 20m;
            await fx.Db.Context.SaveChangesAsync();

            var afterRev = await fx.Service.ReconcileGrnAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Receiver",
                plan.Summary.Id,
                new SilaCulinaryReconcileGrnRequestDto
                {
                    GoodsReceiptId = grn.Id,
                    IsReversal = true,
                    ReversalMaterialDocument = "4900000102",
                }, CancellationToken.None);

            var allocAfterRev = Assert.Single(afterRev.Allocations, a => a.AllocationType == "INCOMING_PO");
            Assert.Equal(50m, allocAfterRev.RemainingQty);
            Assert.Contains(afterRev.Exceptions, e => e.ExceptionType == "GRN_REVERSAL");
        }

        [Fact]
        public async Task ScenarioE_MultiEventConsolidation_15Plus20Plus12_Recommends50KgWithEventShares()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(5);
            var (_, mrp, mat) = SeedApprovedEventMrp(
                fx, "EVT-C1", "Consolidation Event A", "MAT-FILLET", "Beef Fillet",
                15m, 95m, reqBy, orderUom: "PACK5", packConversionQty: 5m);

            fx.Db.Seed(new SilaCulinaryMrpLine
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MrpRunId = mrp.Id,
                ConsolidationKey = $"MAT-FILLET|{reqBy:yyyy-MM-dd}|B",
                MaterialId = mat.Id,
                MaterialCode = "MAT-FILLET",
                MaterialDescription = "Beef Fillet",
                IsResolvedMaterial = true,
                PropertyId = fx.Property.Id,
                PlantCode = "1060",
                KitchenLocationId = fx.MainKitchen.Id,
                KitchenName = fx.MainKitchen.LocationName,
                StorageLocationCode = fx.MainKitchen.StorageLocationCode,
                RequiredDateUtc = reqBy,
                RequiredDateBucket = reqBy.ToString("yyyy-MM-dd"),
                BaseUom = "KG",
                GrossQty = 20m,
                IndicativeNewProductionQty = 20m,
                UnitCost = 95m,
                ContributingEventsJson = "[\"EVT-C2\"]",
                IsActive = true,
            });
            fx.Db.Seed(new SilaCulinaryMrpLine
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                MrpRunId = mrp.Id,
                ConsolidationKey = $"MAT-FILLET|{reqBy:yyyy-MM-dd}|C",
                MaterialId = mat.Id,
                MaterialCode = "MAT-FILLET",
                MaterialDescription = "Beef Fillet",
                IsResolvedMaterial = true,
                PropertyId = fx.Property.Id,
                PlantCode = "1060",
                KitchenLocationId = fx.MainKitchen.Id,
                KitchenName = fx.MainKitchen.LocationName,
                StorageLocationCode = fx.MainKitchen.StorageLocationCode,
                RequiredDateUtc = reqBy,
                RequiredDateBucket = reqBy.ToString("yyyy-MM-dd"),
                BaseUom = "KG",
                GrossQty = 12m,
                IndicativeNewProductionQty = 12m,
                UnitCost = 95m,
                ContributingEventsJson = "[\"EVT-C3\"]",
                IsActive = true,
            });

            var detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Procurement",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            var bucket = Assert.Single(detail.ConsolidatedPurchases);
            Assert.Equal(47m, bucket.CombinedNetQty);
            Assert.Equal(3, bucket.EventShares.Count);
        }

        [Fact]
        public async Task ScenarioF_And_G_OutletToOutletSamePlant311ItoDraft_AndSap312ReversalReconciliation()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(4);
            var (_, mrp, mat) = SeedApprovedEventMrp(fx, "EVT-ITO-01", "ITO Transfer Gala", "MAT-CAVIAR", "Oscietra Caviar", 15m, 500m, reqBy);

            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.OutletB.Id,
                MaterialId = mat.Id,
                OnHandQty = 15m,
                BaseUom = "KG",
                IsActive = true,
            });

            var detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            var itoAlloc = await fx.Service.CreateItoDraftAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                detail.Summary.Id,
                new SilaCulinaryCreateItoDraftRequestDto
                {
                    ProcurementLineId = detail.Lines[0].Id,
                    FromLocationId = fx.OutletB.Id,
                    ToLocationId = fx.MainKitchen.Id,
                    Quantity = 15m,
                    IdempotencyKey = "ito-key-1",
                }, CancellationToken.None);

            Assert.Equal("PROPOSED_ITO", itoAlloc.AllocationType);
            Assert.Equal("PENDING_APPROVAL", itoAlloc.Status);
            Assert.NotNull(itoAlloc.InternalTransferOrderId);
            var createdIto = await fx.Db.Context.Set<InternalTransferOrder>().FirstAsync(x => x.Id == itoAlloc.InternalTransferOrderId);
            Assert.Equal(Common.SILA_ITO_PENDING_APPROVAL, createdIto.Status);

            // Cross-plant rejection check
            BuyerProperty otherPlant = new() { Id = Guid.NewGuid(), BuyerId = fx.Buyer.Id, CompanyCode = "1000", PlantCode = "2090", PropertyName = "Abu Dhabi Resort", IsActive = true };
            InventoryLocation otherLoc = new() { Id = Guid.NewGuid(), BuyerId = fx.Buyer.Id, PropertyId = otherPlant.Id, LocationCode = "AD-OUT", LocationName = "AD Outlet", LocationType = "OUTLET", StorageLocationCode = "AD01", InventoryEnabled = true, TransferEnabled = true, IsActive = true };
            fx.Db.Seed(otherPlant, otherLoc);
            await Assert.ThrowsAsync<BadRequestCustomException>(() => fx.Service.CreateItoDraftAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                detail.Summary.Id,
                new SilaCulinaryCreateItoDraftRequestDto
                {
                    ProcurementLineId = detail.Lines[0].Id,
                    FromLocationId = otherLoc.Id,
                    ToLocationId = fx.MainKitchen.Id,
                    Quantity = 5m,
                }, CancellationToken.None));

            // Scenario G: SAP 312 transfer reversal reconciliation
            var afterRev = await fx.Service.ReconcileItoReversalAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Store Mgr",
                detail.Summary.Id,
                new SilaCulinaryReconcileItoReversalRequestDto
                {
                    InternalTransferOrderId = createdIto.Id,
                    ReversedQty = 15m,
                    ReversalMaterialDocument = "4900000312",
                    Reason = "SAP 312 reversal test",
                }, CancellationToken.None);

            Assert.Contains(afterRev.Allocations, a => a.Status == "REVERSED" && a.ReversalMaterialDocument == "4900000312");
            Assert.Contains(afterRev.Exceptions, e => e.ExceptionType == "ITO_REVERSAL");
        }

        [Fact]
        public async Task ScenarioH_ExpiredStock_20Of50KgExpired_LimitsEligibleFefoTo30Kg()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(5);
            var (_, mrp, mat) = SeedApprovedEventMrp(fx, "EVT-EXP-01", "Expiry Check", "MAT-CREAM", "Heavy Cream", 45m, 25m, reqBy);

            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.MainKitchen.Id,
                MaterialId = mat.Id,
                OnHandQty = 50m,
                BaseUom = "KG",
                IsActive = true,
            });
            GoodsReceipt grn = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                GrnNumber = "GRN-EXP-1",
                LocationId = fx.MainKitchen.Id,
                Status = Common.SILA_GRN_POSTED,
                IsActive = true,
            };
            fx.Db.Seed(grn);
            fx.Db.Seed(
                new GoodsReceiptItem
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = grn.Id,
                    MaterialId = mat.Id,
                    BatchNumber = "BATCH-EXPIRED",
                    ExpiryDate = reqBy.AddDays(-1),
                    AcceptedQty = 20m,
                    ReversedQty = 0m,
                    IsActive = true,
                },
                new GoodsReceiptItem
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = grn.Id,
                    MaterialId = mat.Id,
                    BatchNumber = "BATCH-FRESH",
                    ExpiryDate = reqBy.AddDays(10),
                    AcceptedQty = 30m,
                    ReversedQty = 0m,
                    IsActive = true,
                });

            var detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            var line = Assert.Single(detail.Lines);
            var expiredBatch = Assert.Single(line.FefoBatches, b => b.BatchNumber == "BATCH-EXPIRED");
            var freshBatch = Assert.Single(line.FefoBatches, b => b.BatchNumber == "BATCH-FRESH");
            Assert.Equal("EXPIRED_EXCLUDED", expiredBatch.QualityStatus);
            Assert.Equal(0m, expiredBatch.RecommendedQty);
            Assert.Equal("ELIGIBLE", freshBatch.QualityStatus);
            Assert.Equal(30m, freshBatch.RecommendedQty);
        }

        [Fact]
        public async Task ScenarioI_PoDraftCreation_UsesRealHandlerDeferredErpAndPreventsDuplicatesAndPreservesStockAccountAssignment()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(5);
            var (_, mrp, _) = SeedApprovedEventMrp(
                fx, "EVT-PO-01", "PO Draft Test", "MAT-OYSTER", "Fine de Claire Oysters",
                30m, 110m, reqBy, inventoryType: "STOCK");

            var detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Procurement",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            SilaCulinaryCreatePoDraftRequestDto req = new()
            {
                ProcurementLineIds = new List<Guid> { detail.Lines[0].Id },
                SupplierId = fx.Supplier.Id,
                IdempotencyKey = "po-draft-idem-1",
            };

            var first = await fx.Service.CreatePoDraftAsync(fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Procurement", detail.Summary.Id, req, CancellationToken.None);
            var second = await fx.Service.CreatePoDraftAsync(fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Procurement", detail.Summary.Id, req, CancellationToken.None);

            Assert.Equal(first.Id, second.Id);
            Assert.Equal(first.PoNumber, second.PoNumber);
            Assert.Equal(1, await fx.Db.Context.Set<PurchaseOrder>().CountAsync());

            PurchaseOrder po = await fx.Db.Context.Set<PurchaseOrder>().FirstAsync();
            Assert.Equal(Common.PURCHASE_ORDER_PENDING_APPROVAL, po.Status);
            Assert.Equal(0, fx.Mediator.Gateway.ErpCallCount);
            Assert.Equal(0, fx.Mediator.SupplierClient.SupplierCallCount);
            Assert.Null(first.AccountAssignmentCategory);
        }

        [Fact]
        public async Task ScenarioJ_EventCancellation_ReleasesReservationsAndAllocations_NeverCancelsReleasedPos()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(5);
            var (_, mrp, mat) = SeedApprovedEventMrp(fx, "EVT-CANC-01", "Cancel Event", "MAT-DUCK", "Duck Breast", 40m, 75m, reqBy);

            fx.Db.Seed(new InventoryBalance
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                LocationId = fx.MainKitchen.Id,
                MaterialId = mat.Id,
                OnHandQty = 20m,
                BaseUom = "KG",
                IsActive = true,
            });
            PurchaseOrder releasedPo = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = fx.Buyer.Id,
                BuyerOrganizationId = fx.Buyer.OrganizationId,
                PoNumber = "PO-REL-99",
                SupplierId = fx.Supplier.Id,
                SupplierName = fx.Supplier.Name,
                Status = "APPROVED",
                DeliveryDate = reqBy.AddDays(-1),
                IsActive = true,
            };
            fx.Db.Seed(releasedPo);
            fx.Db.Seed(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = releasedPo.Id,
                LineNumber = 1,
                MaterialCode = "MAT-DUCK",
                ProductName = "Duck Breast",
                Quantity = 20m,
                ReceivedQuantity = 0m,
                IsActive = true,
            });

            var detail = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = mrp.Id }, CancellationToken.None);

            await fx.Service.CreateReservationAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                detail.Summary.Id,
                new SilaCulinaryCreateReservationRequestDto
                {
                    ProcurementLineId = detail.Lines[0].Id,
                    LocationId = fx.MainKitchen.Id,
                    Quantity = 20m,
                    ActivateImmediately = true,
                }, CancellationToken.None);

            var cancelled = await fx.Service.CancelEventProcurementAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Manager",
                detail.Summary.Id, "Client cancelled event", CancellationToken.None);

            Assert.Equal("CANCELLED", cancelled.Summary.Status);
            Assert.All(cancelled.Reservations, r => Assert.Equal("RELEASED", r.Status));
            Assert.All(cancelled.Allocations, a => Assert.Equal("RELEASED", a.Status));

            PurchaseOrder persistedPo = await fx.Db.Context.Set<PurchaseOrder>().FirstAsync(x => x.Id == releasedPo.Id);
            Assert.Equal("APPROVED", persistedPo.Status);
        }

        [Fact]
        public async Task Governance_HandoverValidation_CostMasking_ReportsFormulaSafety_AndPhase3AHandover()
        {
            TestFixture fx = CreateFixture();
            DateTime reqBy = DateTime.UtcNow.AddDays(5);

            var (_, draftMrp, _) = SeedApprovedEventMrp(
                fx, "EVT-DRAFT-01", "Draft Event", "MAT-SAFFRON", "Saffron", 2m, 500m, reqBy,
                mrpStatus: "CALCULATED", isApprovedBaseline: false);

            await Assert.ThrowsAsync<BadRequestCustomException>(() => fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = draftMrp.Id, IsSimulation = false }, CancellationToken.None));

            var simPlan = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Chef",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = draftMrp.Id, IsSimulation = true }, CancellationToken.None);
            Assert.True(simPlan.Summary.IsSimulation);

            var (_, appMrp, _) = SeedApprovedEventMrp(
                fx, "EVT-GOV-01", "Governance Gala", "MAT-GOLD", "=CMD('calc')", 5m, 200m, reqBy,
                mrpStatus: "APPROVED", isApprovedBaseline: true);

            var opPlan = await fx.Service.CreateAndCalculatePlanAsync(
                fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, "Admin",
                new SilaCulinaryCreateProcurementPlanRequestDto { MrpRunId = appMrp.Id }, CancellationToken.None);

            Guid restrictedChefRoleId = Guid.NewGuid();
            var maskedView = await fx.Service.GetPlanAsync(fx.Buyer.OrganizationId, fx.AdminUserId, restrictedChefRoleId, opPlan.Summary.Id, CancellationToken.None);
            Assert.False(maskedView.CostsVisible);
            Assert.Null(maskedView.Summary.EstimatedNewPurchaseValue);
            Assert.Null(maskedView.Lines[0].SupplierUnitPrice);

            // 34 reports + CSV formula injection escaping
            var catalog = fx.Service.GetReportCatalog();
            Assert.Equal(34, catalog.Count);
            var report = await fx.Service.ExportReportAsync(fx.Buyer.OrganizationId, Common.BUYER_ADMIN_ROLE_ID, opPlan.Summary.Id, "EVENT_MATERIAL_COVERAGE", CancellationToken.None);
            Assert.DoesNotContain("\n=CMD", report.CsvContent);
            Assert.Contains("'=CMD", report.CsvContent);

            // Phase 3A handover preserves distinction (FutureActualConsumptionCost is null)
            var handover = await fx.Service.GetPhase3AHandoverAsync(fx.Buyer.OrganizationId, fx.AdminUserId, Common.BUYER_ADMIN_ROLE_ID, opPlan.Summary.Id, CancellationToken.None);
            Assert.Null(handover.FutureActualConsumptionCost);
            Assert.Equal("3A.1.0", handover.ContractVersion);
            Assert.NotEmpty(handover.Materials);

            // Transaction Tracker events recorded
            Assert.True(await fx.Db.Context.Set<InventoryWorkflowEvent>().AnyAsync(x => x.ReferenceType == Common.SILA_REF_CULINARY_PROCUREMENT && x.ReferenceId == opPlan.Summary.Id));
        }
    }
}
