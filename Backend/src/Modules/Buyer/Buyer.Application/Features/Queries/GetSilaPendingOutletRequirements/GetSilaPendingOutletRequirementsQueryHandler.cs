using Microsoft.EntityFrameworkCore;
using Buyer.Application.Features.Shared;
using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;

namespace Buyer.Application.Features.Queries.GetSilaPendingOutletRequirements
{
    /// <summary>
    /// Compact pending outlet requirements for Live Inventory: outstanding Weekly Bucket lines
    /// (approved/frozen) and recent wishlist lines that map to Item Master materials.
    /// </summary>
    public class GetSilaPendingOutletRequirementsQueryHandler
        : IRequestHandler<GetSilaPendingOutletRequirementsQuery, SilaPendingOutletRequirementsDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaPendingOutletRequirementsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaPendingOutletRequirementsDto> Handle(
            GetSilaPendingOutletRequirementsQuery request,
            CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            List<Guid> allowedLocations = await SilaAccess.GetLocationIdsAsync(
                _repository, buyer.Id, request.UserId, request.RoleId, cancellationToken);
            bool fullAccess = SilaAccess.HasFullAccess(request.RoleId);

            List<InventoryLocation> stores = fullAccess
                ? new List<InventoryLocation>()
                : await _repository.InventoryLocation
                    .FindByCondition(x => allowedLocations.Contains(x.Id) && x.IsActive && x.LocationType == Common.SILA_LOCATION_STORE)
                    .ToListAsync(cancellationToken);
            HashSet<Guid> storeProperties = stores.Select(x => x.PropertyId).ToHashSet();

            List<InventoryLocation> outletLocations = await _repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive
                    && x.LocationType == Common.SILA_LOCATION_OUTLET
                    && x.OutletId != null)
                .ToListAsync(cancellationToken);
            if (!fullAccess)
            {
                outletLocations = outletLocations
                    .Where(x => allowedLocations.Contains(x.Id) || storeProperties.Contains(x.PropertyId))
                    .ToList();
            }

            if (request.DestinationOutletId != null)
            {
                outletLocations = outletLocations.Where(x => x.OutletId == request.DestinationOutletId).ToList();
            }

            Dictionary<Guid, InventoryLocation> outletByOutletId = outletLocations
                .Where(x => x.OutletId != null)
                .GroupBy(x => x.OutletId!.Value)
                .ToDictionary(x => x.Key, x => x.First());

            List<SilaPendingOutletRequirementDto> items = new();
            items.AddRange(await LoadWeeklyBucketAsync(buyer.Id, outletByOutletId, request, cancellationToken));
            items.AddRange(await LoadWishlistAsync(buyer.Id, outletByOutletId, request, cancellationToken));

            if (request.MaterialId != null)
            {
                items = items.Where(x => x.MaterialId == request.MaterialId).ToList();
            }

            items = items.Where(x => x.AvailableForNewIto > 0).OrderBy(x => x.MaterialCode).ThenBy(x => x.SourceReference).ToList();

            if (!string.IsNullOrWhiteSpace(request.SourceLocationId)
                && Guid.TryParse(request.SourceLocationId, out Guid sourceId))
            {
                Dictionary<Guid, decimal> stock = await _repository.InventoryBalance
                    .FindByCondition(x => x.LocationId == sourceId && x.IsActive)
                    .ToDictionaryAsync(x => x.MaterialId, x => x.OnHandQty, cancellationToken);
                foreach (SilaPendingOutletRequirementDto row in items)
                {
                    row.AvailableSourceStock = stock.GetValueOrDefault(row.MaterialId);
                }
            }

            _logger.LogInfo($"Pending outlet requirements loaded. BuyerId: {buyer.Id}, Count: {items.Count}");
            return new SilaPendingOutletRequirementsDto
            {
                Count = items.Count,
                Items = request.CountOnly ? new List<SilaPendingOutletRequirementDto>() : items
            };
        }

        private async Task<List<SilaPendingOutletRequirementDto>> LoadWeeklyBucketAsync(
            Guid buyerId,
            Dictionary<Guid, InventoryLocation> outlets,
            GetSilaPendingOutletRequirementsQuery request,
            CancellationToken cancellationToken)
        {
            if (outlets.Count == 0)
            {
                return new List<SilaPendingOutletRequirementDto>();
            }

            List<string> frozen = new()
            {
                Common.WEEKLY_BUCKET_PENDING_APPROVAL,
                Common.WEEKLY_BUCKET_APPROVED,
                Common.WEEKLY_BUCKET_PO_CREATED,
                Common.WEEKLY_BUCKET_PO_FAILED
            };

            HashSet<Guid> propertyIds = outlets.Values.Select(x => x.PropertyId).ToHashSet();
            List<WeeklyBucket> buckets = await _repository.WeeklyBucket
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                    && propertyIds.Contains(x.PropertyId)
                    && frozen.Contains(x.Status)
                    && (request.WeeklyBucketId == null || x.Id == request.WeeklyBucketId))
                .ToListAsync(cancellationToken);
            if (buckets.Count == 0)
            {
                return new List<SilaPendingOutletRequirementDto>();
            }

            HashSet<Guid> bucketIds = buckets.Select(x => x.Id).ToHashSet();
            HashSet<Guid> outletIds = outlets.Keys.ToHashSet();
            List<WeeklyBucketItem> bucketItems = await _repository.WeeklyBucketItem
                .FindByCondition(x => bucketIds.Contains(x.WeeklyBucketId)
                    && x.IsActive
                    && outletIds.Contains(x.OutletId)
                    && x.MaterialId != null
                    && x.ApprovedQuantity > 0
                    && x.LineStatus != Common.LINE_EXCLUDED)
                .ToListAsync(cancellationToken);

            Dictionary<Guid, WeeklyBucket> bucketMap = buckets.ToDictionary(x => x.Id);
            List<Guid> materialIds = bucketItems.Select(x => x.MaterialId!.Value).Distinct().ToList();
            Dictionary<Guid, MaterialEntity> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => materialIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            List<SilaPendingOutletRequirementDto> rows = new();
            foreach (WeeklyBucketItem item in bucketItems)
            {
                if (!materials.TryGetValue(item.MaterialId!.Value, out MaterialEntity? material))
                {
                    continue;
                }

                if (!outlets.TryGetValue(item.OutletId, out InventoryLocation? destination))
                {
                    continue;
                }

                WeeklyBucket bucket = bucketMap[item.WeeklyBucketId];
                decimal requested = item.ApprovedQuantity > 0 ? item.ApprovedQuantity : item.RequestedQuantity;
                (decimal fulfilled, decimal allocated) = await SilaTransferDemand.AllocationAsync(
                    _repository, buyerId, item.Id, null, cancellationToken);
                decimal outstanding = Math.Max(0, requested - fulfilled);
                decimal available = Math.Max(0, outstanding - allocated);
                if (available <= 0)
                {
                    continue;
                }

                rows.Add(new SilaPendingOutletRequirementDto
                {
                    RequirementId = item.Id,
                    RequirementSource = Common.SILA_REQ_SOURCE_WEEKLY_BUCKET,
                    SourceReference = bucket.BucketCode,
                    WeeklyBucketId = bucket.Id,
                    WeeklyBucketItemId = item.Id,
                    RequestedDate = item.DateCreated,
                    DestinationOutletId = item.OutletId,
                    DestinationOutletName = destination.LocationName,
                    DestinationLocationId = destination.Id,
                    DestinationLocationCode = destination.LocationCode,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? string.Empty,
                    MaterialDescription = material.Description ?? material.MaterialCode ?? string.Empty,
                    RequestedQuantity = requested,
                    FulfilledQuantity = fulfilled,
                    OutstandingQuantity = outstanding,
                    AllocatedToActiveItos = allocated,
                    AvailableForNewIto = available,
                    Uom = UomConverter.BaseUomOf(material),
                    RequirementStatus = item.LineStatus ?? bucket.Status
                });
            }

            return rows;
        }

        private async Task<List<SilaPendingOutletRequirementDto>> LoadWishlistAsync(
            Guid buyerId,
            Dictionary<Guid, InventoryLocation> outlets,
            GetSilaPendingOutletRequirementsQuery request,
            CancellationToken cancellationToken)
        {
            // Personal wishlists map catalog → material; outlet destination uses first authorized outlet of the property when known.
            List<PersonalWishlist> wishlists = await _repository.PersonalWishlist
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                    && (request.WishlistId == null || x.Id == request.WishlistId))
                .OrderByDescending(x => x.DateCreated)
                .Take(40)
                .ToListAsync(cancellationToken);
            if (wishlists.Count == 0)
            {
                return new List<SilaPendingOutletRequirementDto>();
            }

            HashSet<Guid> wishlistIds = wishlists.Select(x => x.Id).ToHashSet();
            List<PersonalWishlistItem> wishItems = await _repository.PersonalWishlistItem
                .FindByCondition(x => wishlistIds.Contains(x.PersonalWishlistId) && x.IsActive && x.Quantity > 0)
                .ToListAsync(cancellationToken);
            if (wishItems.Count == 0)
            {
                return new List<SilaPendingOutletRequirementDto>();
            }

            HashSet<Guid> catalogIds = wishItems.Select(x => x.CatalogId).ToHashSet();
            List<CatalogMaterialMapping> mappings = await _repository.CatalogMaterialMapping
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && catalogIds.Contains(x.CatalogId))
                .ToListAsync(cancellationToken);
            Dictionary<Guid, Guid> catalogToMaterial = mappings
                .GroupBy(x => x.CatalogId)
                .ToDictionary(x => x.Key, x => x.First().MaterialId);

            List<Guid> materialIds = catalogToMaterial.Values.Distinct().ToList();
            Dictionary<Guid, MaterialEntity> materials = await _repository.ItemBuyerMaster
                .FindByCondition(x => materialIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            InventoryLocation? defaultOutlet = outlets.Values.OrderBy(x => x.LocationCode).FirstOrDefault();
            Dictionary<Guid, PersonalWishlist> wishMap = wishlists.ToDictionary(x => x.Id);
            List<SilaPendingOutletRequirementDto> rows = new();
            foreach (PersonalWishlistItem item in wishItems)
            {
                if (!catalogToMaterial.TryGetValue(item.CatalogId, out Guid materialId)
                    || !materials.TryGetValue(materialId, out MaterialEntity? material))
                {
                    continue;
                }

                (decimal fulfilled, decimal allocated) = await SilaTransferDemand.AllocationAsync(
                    _repository, buyerId, null, item.Id, cancellationToken);
                decimal outstanding = Math.Max(0, item.Quantity - fulfilled);
                decimal available = Math.Max(0, outstanding - allocated);
                if (available <= 0)
                {
                    continue;
                }

                PersonalWishlist wishlist = wishMap[item.PersonalWishlistId];
                rows.Add(new SilaPendingOutletRequirementDto
                {
                    RequirementId = item.Id,
                    RequirementSource = Common.SILA_REQ_SOURCE_WISHLIST,
                    SourceReference = wishlist.Name,
                    WishlistId = wishlist.Id,
                    WishlistItemId = item.Id,
                    RequestedDate = item.DateCreated,
                    DestinationOutletId = defaultOutlet?.OutletId,
                    DestinationOutletName = defaultOutlet?.LocationName,
                    DestinationLocationId = defaultOutlet?.Id,
                    DestinationLocationCode = defaultOutlet?.LocationCode,
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? string.Empty,
                    MaterialDescription = material.Description ?? item.ProductName,
                    RequestedQuantity = item.Quantity,
                    FulfilledQuantity = fulfilled,
                    OutstandingQuantity = outstanding,
                    AllocatedToActiveItos = allocated,
                    AvailableForNewIto = available,
                    Uom = UomConverter.BaseUomOf(material),
                    RequirementStatus = "OPEN"
                });
            }

            return rows;
        }
    }
}
