using Microsoft.EntityFrameworkCore;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Outstanding outlet requirements for demand-based ITOs (Weekly Bucket + mapped Wishlist).
    /// Reuses existing WeeklyBucketItem / PersonalWishlistItem / CatalogMaterialMapping models.
    /// </summary>
    public static class SilaTransferDemand
    {
        private static readonly string[] ActiveAllocationStatuses =
        {
            Common.SILA_ITO_PENDING_APPROVAL,
            Common.SILA_ITO_APPROVED,
            Common.SILA_ITO_DISPATCHED,
            Common.SILA_ITO_REVERSAL_PENDING
        };

        private static readonly string[] FulfilledStatuses =
        {
            Common.SILA_ITO_RECEIVED,
            Common.SILA_ITO_DISCREPANCY,
            Common.SILA_ITO_PARTIALLY_REVERSED,
            Common.SILA_ITO_FULLY_REVERSED
        };

        public static async Task EnsureSamePlant311Async(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            InventoryLocation from,
            InventoryLocation to)
        {
            BuyerProperty? fromProperty = await repository.BuyerProperty.FindFirstByConditionAsync(x => x.Id == from.PropertyId);
            BuyerProperty? toProperty = await repository.BuyerProperty.FindFirstByConditionAsync(x => x.Id == to.PropertyId);
            string? fromPlant = fromProperty?.PlantCode?.Trim();
            string? toPlant = toProperty?.PlantCode?.Trim();
            if (string.IsNullOrWhiteSpace(fromPlant) || string.IsNullOrWhiteSpace(toPlant))
            {
                logger.LogError($"Same-plant 311 blocked: missing plant. From: {from.Id}, To: {to.Id}");
                throw new BadRequestCustomException(
                    SilaInventorySap.SOURCE_SAP_LOCATION_MAPPING_MISSING,
                    "Source and destination must have SAP plant and storage location for movement type 311.");
            }

            if (!string.Equals(fromPlant, toPlant, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"Same-plant 311 blocked: cross-plant. FromPlant: {fromPlant}, ToPlant: {toPlant}");
                throw new BadRequestCustomException(
                    "CROSS_PLANT_311_NOT_ALLOWED",
                    "Movement type 311 supports storage location transfers within the same plant. Please select a valid destination storage location.");
            }
        }

        public static async Task AttachDemandAndValidateAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            List<InternalTransferOrderItem> items,
            List<SilaTransferLineWriteDto> lines,
            CancellationToken cancellationToken)
        {
            Dictionary<Guid, SilaTransferLineWriteDto> byMaterial = lines.ToDictionary(x => x.MaterialId);
            foreach (InternalTransferOrderItem item in items)
            {
                if (!byMaterial.TryGetValue(item.MaterialId, out SilaTransferLineWriteDto? line))
                {
                    continue;
                }

                item.WeeklyBucketId = line.WeeklyBucketId;
                item.WeeklyBucketItemId = line.WeeklyBucketItemId;
                item.WishlistItemId = line.WishlistItemId;

                if (line.WeeklyBucketItemId != null)
                {
                    WeeklyBucketItem? bucketItem = await repository.WeeklyBucketItem.FindFirstByConditionAsync(
                        x => x.Id == line.WeeklyBucketItemId.Value && x.IsActive);
                    if (bucketItem == null || bucketItem.MaterialId != item.MaterialId)
                    {
                        throw new BadRequestCustomException("Weekly bucket line not found.", "Refresh pending requirements and try again.");
                    }

                    item.WeeklyBucketId ??= bucketItem.WeeklyBucketId;
                    item.RequirementRequestedQty = bucketItem.ApprovedQuantity > 0 ? bucketItem.ApprovedQuantity : bucketItem.RequestedQuantity;
                    decimal available = await AvailableForNewItoAsync(
                        repository, buyerId, weeklyBucketItemId: bucketItem.Id, wishlistItemId: null, requested: item.RequirementRequestedQty.Value, cancellationToken);
                    if (item.RequestedQty > available + 0.0001m)
                    {
                        logger.LogError($"ITO over-allocates weekly bucket line. ItemId: {bucketItem.Id}, Requested: {item.RequestedQty}, Available: {available}");
                        throw new BadRequestCustomException(
                            "REQUIREMENT_OVERALLOCATED",
                            $"Only {available} {item.Uom} remains available for a new ITO on this weekly bucket requirement.");
                    }
                }
                else if (line.WishlistItemId != null)
                {
                    PersonalWishlistItem? wishItem = await repository.PersonalWishlistItem.FindFirstByConditionAsync(
                        x => x.Id == line.WishlistItemId.Value && x.IsActive);
                    if (wishItem == null)
                    {
                        throw new BadRequestCustomException("Wishlist line not found.", "Refresh pending requirements and try again.");
                    }

                    item.RequirementRequestedQty = wishItem.Quantity;
                    decimal available = await AvailableForNewItoAsync(
                        repository, buyerId, weeklyBucketItemId: null, wishlistItemId: wishItem.Id, requested: wishItem.Quantity, cancellationToken);
                    if (item.RequestedQty > available + 0.0001m)
                    {
                        throw new BadRequestCustomException(
                            "REQUIREMENT_OVERALLOCATED",
                            $"Only {available} {item.Uom} remains available for a new ITO on this wishlist requirement.");
                    }
                }
            }
        }

        public static async Task<decimal> AvailableForNewItoAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid? weeklyBucketItemId,
            Guid? wishlistItemId,
            decimal requested,
            CancellationToken cancellationToken)
        {
            (decimal fulfilled, decimal allocated) = await AllocationAsync(
                repository, buyerId, weeklyBucketItemId, wishlistItemId, cancellationToken);
            decimal outstanding = Math.Max(0, requested - fulfilled);
            return Math.Max(0, outstanding - allocated);
        }

        public static async Task<(decimal Fulfilled, decimal Allocated)> AllocationAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid? weeklyBucketItemId,
            Guid? wishlistItemId,
            CancellationToken cancellationToken)
        {
            IQueryable<InternalTransferOrderItem> query = repository.InternalTransferOrderItem
                .FindByCondition(x => x.IsActive
                    && (weeklyBucketItemId != null ? x.WeeklyBucketItemId == weeklyBucketItemId : x.WishlistItemId == wishlistItemId));

            List<InternalTransferOrderItem> lines = await query.ToListAsync(cancellationToken);
            if (lines.Count == 0)
            {
                return (0, 0);
            }

            List<Guid> transferIds = lines.Select(x => x.InternalTransferOrderId).Distinct().ToList();
            Dictionary<Guid, InternalTransferOrder> transfers = await repository.InternalTransferOrder
                .FindByCondition(x => transferIds.Contains(x.Id) && x.BuyerId == buyerId && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            decimal fulfilled = 0;
            decimal allocated = 0;
            foreach (InternalTransferOrderItem line in lines)
            {
                if (!transfers.TryGetValue(line.InternalTransferOrderId, out InternalTransferOrder? transfer))
                {
                    continue;
                }

                if (transfer.Status is Common.SILA_ITO_CANCELLED or Common.SILA_ITO_REJECTED)
                {
                    continue;
                }

                decimal net = Math.Max(0, PostedQty(line) - line.ReversedQty);
                if (FulfilledStatuses.Contains(transfer.Status))
                {
                    fulfilled += net;
                }
                else if (ActiveAllocationStatuses.Contains(transfer.Status))
                {
                    allocated += Math.Max(line.ApprovedQty > 0 ? line.ApprovedQty : line.RequestedQty, 0);
                }
            }

            return (fulfilled, allocated);
        }

        private static decimal PostedQty(InternalTransferOrderItem line)
        {
            if (line.ReceivedQty > 0)
            {
                return line.ReceivedQty;
            }

            return line.DispatchedQty > 0 ? line.DispatchedQty : 0;
        }
    }
}
