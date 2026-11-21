using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    public class SilaPosTrackerTests
    {
        [Fact]
        public void ToDto_DeductedWithoutErpPosting_ShowsNotStartedAndCanReprocess()
        {
            PosSalesTransaction transaction = new PosSalesTransaction
            {
                Id = Guid.NewGuid(),
                SourceTransactionId = "0833B2E7-CF27-43C1-AEF7-A9A39852B56",
                LineNumber = 1,
                OutletCode = "FL10",
                PosCode = "100048",
                QuantitySold = 1m,
                Status = Common.SILA_POS_INVENTORY_DEDUCTED,
                ErpPostingId = null
            };

            SilaPosTransactionDto dto = SilaPosTracker.ToDto(transaction, posting: null, locationName: "FL10", recipe: null, batchNumber: "PS000002");

            Assert.Equal("NOT_STARTED", dto.PostState);
            Assert.Equal("NOT_STARTED", dto.Step2Status);
            Assert.True(dto.CanReprocess);
            Assert.Contains("ERP is not started", dto.Step2Message);
            Assert.Contains("ERP is not started", dto.FailureMessage);
        }

        [Fact]
        public void EventStep_MapsErpPostingSkipped()
        {
            (string? step, string? status) = SilaPosTracker.EventStep("ERP_POSTING_SKIPPED");
            Assert.Equal("POST", step);
            Assert.Equal("SKIPPED", status);
        }
    }
}
