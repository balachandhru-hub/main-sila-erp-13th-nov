using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;

namespace Buyer.Tests.Shared
{
    public class SilaPosReversalTests
    {
        [Fact]
        public void MovementForSale_UsesZ01ForReturnOrVoidEvenWhenQuantityIsPositive()
        {
            PosSalesTransaction sale = new PosSalesTransaction { TransactionType = "SALE", QuantitySold = 10m };
            PosSalesTransaction returned = new PosSalesTransaction
            {
                TransactionType = "RETURN",
                QuantitySold = 3m,
                OriginalTransactionId = "100045"
            };

            // Tenant profile: POS SALE consumption and RETURN both use Z01; API path differs by transaction type.
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaPosReversal.MovementForSale(null, sale));
            Assert.Equal(SilaInventorySap.TYPE_Z01, SilaPosReversal.MovementForSale(null, returned));
            Assert.False(SilaPosReversal.IsReversal(sale.TransactionType));
            Assert.True(SilaPosReversal.IsReversal(returned.TransactionType));
            Assert.Equal(Common.SILA_DIRECTION_IN, SilaPosReversal.LedgerDirection(returned));
            Assert.Equal(3m, SilaPosReversal.ExplosionQuantity(returned));
        }

        [Fact]
        public void ValidateAgainstOriginal_CapsPartialReversalAndBlocksDoubleRestore()
        {
            PosSalesTransaction original = new PosSalesTransaction
            {
                SourceTransactionId = "100045",
                LineNumber = 1,
                QuantitySold = 10m,
                Status = Common.SILA_POS_INVENTORY_DEDUCTED,
                TransactionType = "SALE"
            };
            PosSalesTransaction ok = new PosSalesTransaction
            {
                TransactionType = "RETURN",
                QuantitySold = 4m,
                OriginalTransactionId = "100045",
                OriginalLineId = 1
            };
            PosSalesTransaction excess = new PosSalesTransaction
            {
                TransactionType = "VOID",
                QuantitySold = 7m,
                OriginalTransactionId = "100045",
                OriginalLineId = 1
            };

            Assert.Null(SilaPosReversal.ValidateAgainstOriginal(ok, original, alreadyReversedQty: 0m));
            Assert.Contains("exceeds remaining", SilaPosReversal.ValidateAgainstOriginal(excess, original, alreadyReversedQty: 4m));
            Assert.Contains("fully reversed", SilaPosReversal.ValidateAgainstOriginal(ok, original, alreadyReversedQty: 10m));
            Assert.Contains("not found", SilaPosReversal.ValidateAgainstOriginal(ok, null, 0m));
        }
    }
}
