using Buyer.Domain.Common;
using Buyer.Domain.Entities;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Controlled POS RETURN/VOID handling. Stock is restored with Z01 (positive adjustment).
    /// Cancellation of the original material document is not used for POS reversal.
    /// </summary>
    public static class SilaPosReversal
    {
        public const string TYPE_SALE = "SALE";
        public const string TYPE_RETURN = "RETURN";
        public const string TYPE_VOID = "VOID";

        public static bool IsReversal(string? transactionType)
        {
            string type = NormalizeType(transactionType);
            return type is TYPE_RETURN or TYPE_VOID;
        }

        public static string NormalizeType(string? transactionType)
        {
            string type = string.IsNullOrWhiteSpace(transactionType) ? TYPE_SALE : transactionType.Trim().ToUpperInvariant();
            return type is TYPE_RETURN or TYPE_VOID or TYPE_SALE ? type : TYPE_SALE;
        }

        /// <summary>
        /// Absolute sold quantity used for recipe explosion. RETURN/VOID always restore stock (IN / Z01)
        /// even when the file quantity is positive.
        /// </summary>
        public static decimal ExplosionQuantity(PosSalesTransaction transaction)
        {
            return Math.Abs(transaction.QuantitySold);
        }

        public static string LedgerDirection(PosSalesTransaction transaction)
        {
            return IsReversal(transaction.TransactionType) || transaction.QuantitySold < 0
                ? Common.SILA_DIRECTION_IN
                : Common.SILA_DIRECTION_OUT;
        }

        /// <summary>Z02 consumes; Z01 restores. Prefer stored movement, then RETURN/VOID, then quantity sign.</summary>
        public static string MovementForSale(string? movementType, PosSalesTransaction? sale)
        {
            if (movementType == SilaInventorySap.TYPE_Z01 || movementType == SilaInventorySap.TYPE_Z02)
            {
                return movementType;
            }

            if (sale != null && IsReversal(sale.TransactionType))
            {
                return SilaInventorySap.TYPE_Z01;
            }

            return SilaInventorySap.MovementForSale(null, sale?.QuantitySold ?? 0m);
        }

        /// <summary>
        /// Caps a RETURN/VOID quantity so it cannot restore more than the original net posted quantity.
        /// Returns an error message when the original is missing or already fully reversed.
        /// </summary>
        public static string? ValidateAgainstOriginal(
            PosSalesTransaction reversal,
            PosSalesTransaction? original,
            decimal alreadyReversedQty)
        {
            if (!IsReversal(reversal.TransactionType))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(reversal.OriginalTransactionId))
            {
                return "OriginalTransactionId is required for RETURN or VOID.";
            }

            if (original == null)
            {
                return $"Original POS sale {reversal.OriginalTransactionId} was not found.";
            }

            if (original.Status is not (Common.SILA_POS_INVENTORY_DEDUCTED or Common.SILA_POS_POSTED))
            {
                return $"Original POS sale {original.SourceTransactionId} has not been consumed yet (status {original.Status}).";
            }

            if (IsReversal(original.TransactionType))
            {
                return "OriginalTransactionId must point to a SALE line, not another RETURN/VOID.";
            }

            decimal originalQty = Math.Abs(original.QuantitySold);
            decimal remaining = originalQty - Math.Abs(alreadyReversedQty);
            if (remaining <= 0)
            {
                return $"Original POS sale {original.SourceTransactionId} line {original.LineNumber} is already fully reversed.";
            }

            decimal requested = Math.Abs(reversal.QuantitySold);
            if (requested > remaining)
            {
                return $"RETURN/VOID quantity {requested:0.####} exceeds remaining reversible quantity {remaining:0.####} on original sale {original.SourceTransactionId}.";
            }

            return null;
        }
    }
}
