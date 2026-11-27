using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Tests.Support;
using Xunit;

namespace Buyer.Tests.Shared
{
    public class SilaCulinaryReconciliationServiceTests
    {
        private sealed record Fixture(
            TestDatabase Db,
            SilaCulinaryReconciliationService Service,
            BuyerBusinessProfile Buyer,
            BuyerBusinessProfile OtherBuyer,
            Guid UserId) : IDisposable
        {
            public void Dispose() => Db.Dispose();
        }

        private static Fixture CreateFixture()
        {
            TestDatabase db = new();
            Guid userId = Guid.NewGuid();
            BuyerBusinessProfile buyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "SILA Recon Hospitality",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "recon@sila.test",
                Phone = "+971500000402",
                Country = "UAE",
                AddressLine1 = "Downtown Dubai",
                City = "Dubai",
                State = "Dubai",
                PinCode = "00000",
                Industry = "Hospitality",
                BusinessType = "Resort",
                Currency = "AED",
                Status = "ACTIVE",
                IsActive = true
            };
            BuyerBusinessProfile otherBuyer = new()
            {
                Id = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                OrganizationName = "Other Tenant Hospitality",
                SNID = $"SN-{Guid.NewGuid().ToString("N")[..6]}",
                Email = "other@sila.test",
                Phone = "+971500000499",
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
            db.Context.BuyerBusinessProfile.AddRange(buyer, otherBuyer);
            db.Context.SaveChanges();
            return new Fixture(db, new SilaCulinaryReconciliationService(db.Repository), buyer, otherBuyer, userId);
        }

        private static SilaCulinaryProductionOrder SeedOrder(TestDatabase db, Guid buyerId, decimal qty = 100m)
        {
            SilaCulinaryProductionOrder order = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                OrderNumber = $"PRD-ORD-{Guid.NewGuid().ToString("N")[..6]}",
                RecipeName = "Braised Beef",
                PlannedOutputQty = qty,
                EstimatedProductionCost = 2000m,
                IsActive = true
            };
            db.Context.SilaCulinaryProductionOrder.Add(order);
            db.Context.SilaCulinaryProductionOrderIngredient.Add(new SilaCulinaryProductionOrderIngredient
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ProductionOrderId = order.Id,
                MaterialCode = "MAT-BEEF",
                MaterialName = "Beef Chuck",
                GrossRequiredQty = 100m,
                InventoryUom = "KG",
                IsActive = true
            });
            db.Context.SilaCulinaryProductionMaterialIssueRef.Add(new SilaCulinaryProductionMaterialIssueRef
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                ProductionOrderId = order.Id,
                IssueNumber = $"GI-{Guid.NewGuid().ToString("N")[..6]}",
                MaterialCode = "MAT-BEEF",
                IssuedQty = 100m,
                IssuedUom = "KG",
                IdempotencyKey = $"GI-INIT-{order.Id}",
                IsActive = true
            });
            db.Context.SaveChanges();
            return order;
        }

        [Fact]
        public async Task Scenario1_BalancedReconciliation_AllowsSettlement()
        {
            using Fixture f = CreateFixture();
            SilaCulinaryProductionOrder order = SeedOrder(f.Db, f.Buyer.Id);

            await f.Service.RecordConsumptionAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordConsumptionRequestDto
            {
                ProductionOrderId = order.Id,
                MaterialCode = "MAT-BEEF",
                MaterialName = "Beef Chuck",
                PlannedQty = 100m,
                IssuedQty = 100m,
                ActualConsumedQty = 80m,
                ConsumptionUom = "KG"
            }, CancellationToken.None);

            await f.Service.RecordWasteEventAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordWasteEventRequestDto
            {
                ProductionOrderId = order.Id,
                ItemDomain = "RAW_INGREDIENT",
                ItemCode = "MAT-BEEF",
                ItemName = "Beef Chuck",
                WasteCategory = "TRIMMING_WASTE",
                WasteQty = 5m,
                Uom = "KG",
                EstimatedWasteCost = 100m,
                WasteReason = "Trim fat"
            }, null, null, CancellationToken.None);

            await f.Service.RecordOrInspectReturnAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaMaterialReturnRequestDto
            {
                ProductionOrderId = order.Id,
                MaterialCode = "MAT-BEEF",
                MaterialName = "Beef Chuck",
                ReturnedQty = 15m,
                Uom = "KG",
                ReturnClassification = "UNOPENED_USABLE"
            }, null, null, CancellationToken.None);

            SilaProductionReconciliationDto recon = await f.Service.ReconcileProductionOrderAsync(f.Buyer.OrganizationId, f.UserId, "Chef", order.Id, null, CancellationToken.None);
            Assert.Equal("BALANCED", recon.ReconciliationStatus);
            Assert.False(recon.SettlementBlocked);
            Assert.Single(recon.Lines);
            Assert.Equal(0m, recon.Lines[0].UnexplainedQty);

            SilaProductionReconciliationDto settled = await f.Service.SettleProductionAsync(f.Buyer.OrganizationId, f.UserId, "Finance", recon.Id, true, CancellationToken.None);
            Assert.Equal("FINANCIALLY_SETTLED", settled.FinancialSettlementStatus);
            Assert.Equal("FINAL", settled.CostStatus);
        }

        [Fact]
        public async Task Scenario2_And_3_AdditionalIssueBalanced_And_UnexplainedBlocksSettlement()
        {
            using Fixture f = CreateFixture();
            SilaCulinaryProductionOrder order2 = SeedOrder(f.Db, f.Buyer.Id);

            SilaAdditionalMaterialRequestDto amr = await f.Service.CreateOrDecideAdditionalRequestAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaAdditionalMaterialRequestDto
            {
                ProductionOrderId = order2.Id,
                MaterialCode = "MAT-BEEF",
                MaterialName = "Beef Chuck",
                RequestedQty = 10m,
                Uom = "KG",
                ReasonCode = "INCREASED_GUEST_COUNT"
            }, false, CancellationToken.None);

            amr.ApprovalStatus = "APPROVED";
            amr.ApprovedQty = 10m;
            SilaAdditionalMaterialRequestDto decided = await f.Service.CreateOrDecideAdditionalRequestAsync(f.Buyer.OrganizationId, f.UserId, "ExecChef", amr, true, CancellationToken.None);
            Assert.True(decided.InventoryIssued);

            await f.Service.RecordConsumptionAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordConsumptionRequestDto
            {
                ProductionOrderId = order2.Id,
                MaterialCode = "MAT-BEEF",
                ActualConsumedQty = 85m
            }, CancellationToken.None);
            await f.Service.RecordWasteEventAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordWasteEventRequestDto
            {
                ProductionOrderId = order2.Id,
                ItemDomain = "RAW_INGREDIENT",
                ItemCode = "MAT-BEEF",
                WasteQty = 10m,
                EstimatedWasteCost = 150m
            }, null, null, CancellationToken.None);
            await f.Service.RecordOrInspectReturnAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaMaterialReturnRequestDto
            {
                ProductionOrderId = order2.Id,
                MaterialCode = "MAT-BEEF",
                ReturnedQty = 15m,
                ReturnClassification = "UNOPENED_USABLE"
            }, null, null, CancellationToken.None);

            SilaProductionReconciliationDto recon2 = await f.Service.ReconcileProductionOrderAsync(f.Buyer.OrganizationId, f.UserId, "Chef", order2.Id, null, CancellationToken.None);
            Assert.Equal("BALANCED", recon2.ReconciliationStatus);
            Assert.Equal(110m, recon2.Lines[0].NetIssuedQty);

            // Scenario 3: 5 kg unexplained variance -> Settlement blocked
            SilaCulinaryProductionOrder order3 = SeedOrder(f.Db, f.Buyer.Id);
            await f.Service.RecordConsumptionAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordConsumptionRequestDto
            {
                ProductionOrderId = order3.Id,
                MaterialCode = "MAT-BEEF",
                ActualConsumedQty = 80m
            }, CancellationToken.None);
            await f.Service.RecordWasteEventAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordWasteEventRequestDto
            {
                ProductionOrderId = order3.Id,
                ItemDomain = "RAW_INGREDIENT",
                ItemCode = "MAT-BEEF",
                WasteQty = 5m,
                EstimatedWasteCost = 80m
            }, null, null, CancellationToken.None);
            await f.Service.RecordOrInspectReturnAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaMaterialReturnRequestDto
            {
                ProductionOrderId = order3.Id,
                MaterialCode = "MAT-BEEF",
                ReturnedQty = 10m,
                ReturnClassification = "UNOPENED_USABLE"
            }, null, null, CancellationToken.None);

            SilaProductionReconciliationDto recon3 = await f.Service.ReconcileProductionOrderAsync(f.Buyer.OrganizationId, f.UserId, "Chef", order3.Id, null, CancellationToken.None);
            Assert.True(recon3.SettlementBlocked);
            Assert.Equal(5m, recon3.Lines[0].UnexplainedQty);
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                f.Service.SettleProductionAsync(f.Buyer.OrganizationId, f.UserId, "Finance", recon3.Id, true, CancellationToken.None));
        }

        [Fact]
        public async Task Scenario4_IssueReversalPreventsDoubleReturnAndReversal()
        {
            using Fixture f = CreateFixture();
            SilaCulinaryProductionOrder order = SeedOrder(f.Db, f.Buyer.Id);

            await f.Service.RecordOrInspectReturnAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaMaterialReturnRequestDto
            {
                ProductionOrderId = order.Id,
                MaterialCode = "MAT-BEEF",
                ReturnedQty = 20m,
                ReturnClassification = "UNOPENED_USABLE"
            }, null, null, CancellationToken.None);

            // Reversing 85 kg when 20 kg already returned must fail (only 80 kg reversible)
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                f.Service.ReverseMaterialIssueAsync(f.Buyer.OrganizationId, f.UserId, "Storekeeper", new SilaMaterialIssueReversalRequestDto
                {
                    ProductionOrderId = order.Id,
                    OriginalIssueNumber = "GI000001",
                    MaterialCode = "MAT-BEEF",
                    ReversalQty = 85m,
                    Reason = "Over-issued"
                }, CancellationToken.None));

            SilaMaterialIssueReversalDto rev = await f.Service.ReverseMaterialIssueAsync(f.Buyer.OrganizationId, f.UserId, "Storekeeper", new SilaMaterialIssueReversalRequestDto
            {
                ProductionOrderId = order.Id,
                OriginalIssueNumber = "GI000001",
                MaterialCode = "MAT-BEEF",
                ReversalQty = 80m,
                Reason = "Duplicate issue"
            }, CancellationToken.None);
            Assert.Equal("POSTED", rev.Status);
        }

        [Fact]
        public async Task Scenario6_7_8_9_10_11_12_13_14_15_ComprehensiveGuardrails()
        {
            using Fixture f = CreateFixture();

            // Scenario 6: Portion sample 220g vs 200g -> +10%
            SilaPortionSampleDto sample = await f.Service.RecordPortionSampleAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaPortionSampleRequestDto
            {
                RecipeCode = "RI0001",
                StandardPortionWeightGrams = 200m,
                SampledWeightsGrams = new() { 218m, 222m, 220m },
                PlannedPortions = 500m,
                ActualPortions = 450m,
                StandardPortionCost = 20m
            }, CancellationToken.None);
            Assert.Equal(10m, sample.PortionWeightVariancePercent);
            Assert.Contains("3 sampled", sample.SamplingBasisNote);

            // Scenario 7: Central kitchen allocation reconciles 100% without fake external revenue
            List<SilaCulinaryCentralKitchenAllocation> ck = await f.Service.AllocateCentralKitchenCostAsync(f.Buyer.OrganizationId, f.UserId, "CKChef", new SilaCentralKitchenCostAllocationRequestDto
            {
                SourceKitchenId = Guid.NewGuid(),
                ProductionBatchId = Guid.NewGuid(),
                TotalBatchProductionCost = 1000m,
                RemainingApprovedStockQty = 5m,
                OutletAllocations = new()
                {
                    new SilaCkOutletSliceDto { ReceivingOutletName = "Outlet A", DistributedQty = 40m, ReceivingConfirmed = true },
                    new SilaCkOutletSliceDto { ReceivingOutletName = "Outlet B", DistributedQty = 35m, ReceivingConfirmed = true },
                    new SilaCkOutletSliceDto { ReceivingOutletName = "Outlet C", DistributedQty = 20m, ReceivingConfirmed = true }
                }
            }, CancellationToken.None);
            Assert.Equal(950m, ck.Sum(x => x.AllocatedProductionCost)); // 50 stays with 5kg remaining stock
            Assert.All(ck, x => Assert.False(x.TreatedAsExternalRevenue));

            // Scenario 8 & 9: Operational closure succeeds with pending invoice -> Provisional Financial -> Reopen preserves snapshot
            Guid eventId = Guid.NewGuid();
            SilaCulinaryEventClosureSummaryDto closure = await f.Service.UpsertEventClosureAsync(f.Buyer.OrganizationId, f.UserId, "Controller", new SilaEventClosureRequestDto
            {
                EventId = eventId,
                EventName = "Gala Dinner",
                ExpectedGuestCount = 500,
                ActualGuestCount = 480,
                NetRevenueActual = 72000m,
                DirectMaterialCostActual = 20000m,
                DirectLaborCostActual = 8000m,
                ExternalExpenseActual = 5000m,
                AllocatedOverheadActual = 4000m,
                PendingAccruedCost = 1500m,
                AllServiceCompleted = true,
                AllMaterialsReconciled = true,
                AllInvoicesConfirmed = false
            }, closeOperational: true, closeFinancial: true, CancellationToken.None);
            Assert.Equal("OPERATIONALLY_CLOSED", closure.OperationalStatus);
            Assert.Equal("PROVISIONALLY_CLOSED", closure.FinancialStatus);

            SilaCulinaryEventClosureSummaryDto reopened = await f.Service.ReopenEventClosureAsync(f.Buyer.OrganizationId, f.UserId, "FinanceController", closure.Id, new SilaReopenClosureRequestDto
            {
                ReasonCode = "LATE_SUPPLIER_INVOICE",
                Notes = "Supplier floral invoice arrived",
                AdjustmentAmount = 1500m
            }, CancellationToken.None);
            Assert.Equal(2, reopened.SettlementVersion);
            Assert.Equal("REOPENED", reopened.FinancialStatus);

            // Scenario 10: ERP idempotent retry creates no duplicate
            SilaErpReconciliationRefDto erp1 = await f.Service.UpsertErpReconciliationAsync(f.Buyer.OrganizationId, f.UserId, "Sys", "GI0001", "MATERIAL_ISSUE", "SAP_S4HANA", null, "FAILED_RETRY", "Timeout", "ERP-KEY-1", CancellationToken.None);
            SilaErpReconciliationRefDto erp2 = await f.Service.UpsertErpReconciliationAsync(f.Buyer.OrganizationId, f.UserId, "Sys", "GI0001", "MATERIAL_ISSUE", "SAP_S4HANA", "4900001234", "CONFIRMED", null, "ERP-KEY-1", CancellationToken.None);
            Assert.Equal(erp1.Id, erp2.Id);
            Assert.Equal(1, erp2.RetryCount);
            Assert.Equal("MATCHED", erp2.ReconciliationStatus);

            // Scenario 11 & 13: Waste with AlreadyDeducted + Mobile idempotency
            SilaWasteEventDto w1 = await f.Service.RecordWasteEventAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordWasteEventRequestDto
            {
                ItemCode = "MAT-BEEF",
                WasteQty = 2.5m,
                AlreadyDeductedFromInventory = true,
                EstimatedWasteCost = 50m,
                SourceChannel = "MOBILE",
                IdempotencyKey = "MOB-WST-1"
            }, null, null, CancellationToken.None);
            SilaWasteEventDto w2 = await f.Service.RecordWasteEventAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaRecordWasteEventRequestDto
            {
                ItemCode = "MAT-BEEF",
                WasteQty = 2.5m,
                AlreadyDeductedFromInventory = true,
                EstimatedWasteCost = 50m,
                SourceChannel = "MOBILE",
                IdempotencyKey = "MOB-WST-1"
            }, null, null, CancellationToken.None);
            Assert.Equal(w1.Id, w2.Id);
            Assert.True(w1.SecondDeductionPrevented);

            // Scenario 12: Unauthorized user copilot denied without disclosing figures
            SilaReconciliationCopilotResponseDto denied = await f.Service.AskCostControlCopilotAsync(f.Buyer.OrganizationId, new SilaReconciliationCopilotRequestDto
            {
                Question = "What is event profit?",
                CanViewFinancialCost = false
            }, CancellationToken.None);
            Assert.True(denied.AccessDenied);
            Assert.Empty(denied.SupportingMetrics);

            // Scenario 14: Failed QC blocks production settlement
            SilaCulinaryProductionOrder orderQc = SeedOrder(f.Db, f.Buyer.Id);
            SilaCulinaryProductionBatch batchQc = new()
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ProductionOrderId = orderQc.Id,
                BatchNumber = $"BAT-{Guid.NewGuid().ToString("N")[..6]}",
                PlannedBatchQty = 500m,
                PlannedStartUtc = DateTime.UtcNow,
                PlannedFinishUtc = DateTime.UtcNow.AddHours(2),
                IsActive = true
            };
            f.Db.Context.SilaCulinaryProductionBatch.Add(batchQc);
            f.Db.Context.SilaCulinaryProductionQualityResult.Add(new SilaCulinaryProductionQualityResult
            {
                Id = Guid.NewGuid(),
                BuyerId = f.Buyer.Id,
                ProductionBatchId = batchQc.Id,
                CheckpointName = "Core Temp Check",
                Passed = false,
                SupervisorApproved = false,
                IsActive = true
            });
            f.Db.Context.SaveChanges();
            SilaProductionReconciliationDto reconQc = await f.Service.ReconcileProductionOrderAsync(f.Buyer.OrganizationId, f.UserId, "Chef", orderQc.Id, null, CancellationToken.None);
            Assert.True(reconQc.SettlementBlocked);

            // Scenario 15: Tenant B cannot see Tenant A reconciliation
            SilaPhase4BDashboardDto otherDash = await f.Service.GetDashboardAsync(f.OtherBuyer.OrganizationId, null, true, CancellationToken.None);
            Assert.Empty(otherDash.Reconciliations);

            // Food safety guardrail: unsafe leftover cannot be reused
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                f.Service.RecordOrDisposeLeftoverAsync(f.Buyer.OrganizationId, f.UserId, "Chef", new SilaFoodLeftoverRequestDto
                {
                    FinishedProductName = "Seafood Risotto",
                    QualityStatus = "UNSAFE_HOLD",
                    Disposition = "REDISTRIBUTION"
                }, null, CancellationToken.None));
        }
    }
}
