using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetSilaLiveInventory
{
    /// <summary>
    /// Searches materials and sums their stock over the caller's locations (or one of them). Without a location the search
    /// needs at least two characters; with a location, the materials holding a balance there are listed.
    /// When a location is scoped, reserved / available / stock status use the same rules as live detail.
    /// </summary>
    public class GetSilaLiveInventoryQueryHandler : IRequestHandler<GetSilaLiveInventoryQuery, List<SilaLiveInventoryRowDto>>
    {
        private const int MinimumSearchLength = 2;

        private const int SEARCH_LENGTH = 100;

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaLiveInventoryQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<SilaLiveInventoryRowDto>> Handle(GetSilaLiveInventoryQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Searching live inventory. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}, Search: {request.Search}, LocationId: {request.LocationId}, Health: {request.Health}");

            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            SilaInputRules.MaxLength(_logger, request.Search, SEARCH_LENGTH, "Search");
            string term = (request.Search ?? string.Empty).Trim();
            string? health = string.IsNullOrWhiteSpace(request.Health) ? null : request.Health.Trim().ToUpperInvariant();
            string? productClass = SilaProductClassifier.Normalize(request.ProductClass);
            bool hasLocation = request.LocationId != null && request.LocationId != Guid.Empty;
            InventoryLocation? scopedLocation = null;
            if (!hasLocation && term.Length < MinimumSearchLength)
            {
                _logger.LogInfo("Live inventory search skipped: search too short and no location.");
                return new List<SilaLiveInventoryRowDto>();
            }

            List<Guid> scope;
            if (hasLocation)
            {
                Guid locationId = request.LocationId!.Value;
                scopedLocation = await SilaAccess.GetLocationAsync(_repository, _logger, buyer.Id, locationId);
                await SilaAccess.EnsureLocationAccessAsync(_repository, _logger, buyer.Id, request.UserId, request.RoleId, locationId, cancellationToken);
                scope = new List<Guid> { locationId };
            }
            else
            {
                scope = await SilaAccess.GetLocationIdsAsync(_repository, buyer.Id, request.UserId, request.RoleId, cancellationToken);
            }

            int index = request.Index < 0 ? 0 : request.Index;
            int limit = SilaInputRules.Limit(request.Limit, 20);
            // Health filters need status before paging; fetch a wider window then page in memory.
            bool filterHealth = hasLocation && health != null;
            bool filterInMemory = filterHealth || productClass != null;
            int fetchSkip = filterInMemory ? 0 : index;
            int fetchTake = filterInMemory ? Math.Min(500, Math.Max(limit * 25, 100)) : limit;

            IQueryable<MaterialEntity> query = _repository.ItemBuyerMaster.FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive);
            if (hasLocation)
            {
                IQueryable<Guid> stockedIds = _repository.InventoryBalance
                    .FindByCondition(x => scope.Contains(x.LocationId) && x.IsActive)
                    .Select(x => x.MaterialId);
                query = query.Where(x => stockedIds.Contains(x.Id));
            }

            if (term.Length > 0)
            {
                query = query.Where(x =>
                    (x.MaterialCode != null && x.MaterialCode.Contains(term)) ||
                    (x.Description != null && x.Description.Contains(term)) ||
                    (x.Barcode != null && x.Barcode == term));
            }

            List<MaterialEntity> materials = await query
                .OrderBy(x => x.MaterialCode)
                .Skip(fetchSkip)
                .Take(fetchTake)
                .ToListAsync(cancellationToken);
            List<Guid> materialIds = materials.Select(x => x.Id).ToList();
            List<InventoryBalance> balances = await _repository.InventoryBalance
                .FindByCondition(x => materialIds.Contains(x.MaterialId) && scope.Contains(x.LocationId) && x.IsActive)
                .ToListAsync(cancellationToken);

            Dictionary<(Guid MaterialId, Guid LocationId), decimal> reserved = new Dictionary<(Guid, Guid), decimal>();
            Dictionary<Guid, InventoryLocationMaterial> stockingByMaterial = new Dictionary<Guid, InventoryLocationMaterial>();
            Dictionary<(Guid LocationMaterialId, int Month), LocationMaterialThreshold> thresholds =
                new Dictionary<(Guid, int), LocationMaterialThreshold>();
            int month = DateTime.UtcNow.Month;
            if (hasLocation && materialIds.Count > 0)
            {
                Guid locationId = scope[0];
                List<InventoryLocationMaterial> levels = await _repository.InventoryLocationMaterial
                    .FindByCondition(x => x.LocationId == locationId && materialIds.Contains(x.MaterialId) && x.IsActive)
                    .ToListAsync(cancellationToken);
                stockingByMaterial = levels.ToDictionary(x => x.MaterialId);
                List<Guid> levelIds = levels.Select(x => x.Id).ToList();
                if (levelIds.Count > 0)
                {
                    List<LocationMaterialThreshold> monthRows = await _repository.LocationMaterialThreshold
                        .FindByCondition(x => levelIds.Contains(x.LocationMaterialId) && x.Month == month && x.IsActive)
                        .ToListAsync(cancellationToken);
                    thresholds = monthRows.ToDictionary(x => (x.LocationMaterialId, x.Month));
                }

                // Batch reserved qty for all materials at this location (open outbound ITOs).
                Dictionary<Guid, Guid> openTransfers = await _repository.InternalTransferOrder
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive && x.FromLocationId == locationId
                        && (x.Status == Domain.Common.Common.SILA_ITO_PENDING_APPROVAL
                            || x.Status == Domain.Common.Common.SILA_ITO_APPROVED))
                    .ToDictionaryAsync(x => x.Id, x => x.FromLocationId, cancellationToken);
                if (openTransfers.Count > 0)
                {
                    List<Guid> transferIds = openTransfers.Keys.ToList();
                    List<InternalTransferOrderItem> items = await _repository.InternalTransferOrderItem
                        .FindByCondition(x => x.IsActive && materialIds.Contains(x.MaterialId) && transferIds.Contains(x.InternalTransferOrderId))
                        .ToListAsync(cancellationToken);
                    foreach (IGrouping<Guid, InternalTransferOrderItem> group in items.GroupBy(x => x.MaterialId))
                    {
                        reserved[(group.Key, locationId)] = group.Sum(item => item.ApprovedQty > 0 ? item.ApprovedQty : item.RequestedQty);
                    }
                }
            }

            List<SilaLiveInventoryRowDto> mapped = materials.Select(material =>
            {
                List<InventoryBalance> rows = balances.Where(x => x.MaterialId == material.Id).ToList();
                decimal onHand = rows.Sum(x => x.OnHandQty);
                decimal inTransit = rows.Sum(x => x.InTransitQty);
                string description = !string.IsNullOrWhiteSpace(material.Description)
                    ? material.Description.Trim()
                    : (material.MaterialCode ?? string.Empty);

                SilaLiveInventoryRowDto row = new SilaLiveInventoryRowDto
                {
                    MaterialId = material.Id,
                    MaterialCode = material.MaterialCode ?? string.Empty,
                    Description = description,
                    BaseUom = UomConverter.BaseUomOf(material),
                    OnHandQty = onHand,
                    InTransitQty = inTransit,
                    LocationCount = rows.Count(x => x.OnHandQty != 0 || x.InTransitQty != 0),
                    AvailableQty = onHand,
                    LastMovementOn = rows.Where(x => x.LastMovementOn != null).Select(x => x.LastMovementOn).DefaultIfEmpty().Max(),
                    MaterialGroup = string.IsNullOrWhiteSpace(material.MaterialGroup) ? null : material.MaterialGroup.Trim(),
                    ProductClass = SilaProductClassifier.Infer(material, scopedLocation?.StoreCategory, scopedLocation?.LocationName),
                    Barcode = string.IsNullOrWhiteSpace(material.Barcode) ? null : material.Barcode.Trim(),
                    UnitCost = material.UnitCost,
                    Currency = material.Currency
                };

                if (hasLocation)
                {
                    Guid locationId = scope[0];
                    decimal reservedQty = reserved.GetValueOrDefault((material.Id, locationId));
                    row.ReservedQty = reservedQty;
                    row.AvailableQty = Math.Max(0, onHand - reservedQty);
                    InventoryLocationMaterial? level = stockingByMaterial.GetValueOrDefault(material.Id);
                    row.StockStatus = SilaLiveStockFigures.StockStatus(onHand, level, thresholds, month);
                    if (level != null)
                    {
                        row.ParLevel = level.ParLevel;
                        row.MaximumStock = level.MaximumStock;
                        SilaEffectiveLevels levels = SilaStockLevels.Effective(level, thresholds, month);
                        row.ReorderPoint = levels.ReorderPoint ?? levels.MinimumStock;
                    }
                }

                return row;
            }).ToList();

            if (filterInMemory)
            {
                IEnumerable<SilaLiveInventoryRowDto> filtered = mapped;
                if (filterHealth)
                {
                    filtered = filtered.Where(x => string.Equals(x.StockStatus, health, StringComparison.OrdinalIgnoreCase));
                }

                if (productClass != null)
                {
                    filtered = filtered.Where(x => x.ProductClass == productClass);
                }

                mapped = filtered.Skip(index).Take(limit).ToList();
            }

            await AttachIssueTrendAsync(buyer.Id, scope, mapped, cancellationToken);

            _logger.LogInfo($"Live inventory searched. Count: {mapped.Count}, BuyerId: {buyer.Id}");
            return mapped;
        }

        /// <summary>Issues (OUT movements) per day over the last 7 days for the listed materials, oldest day first.</summary>
        private async Task AttachIssueTrendAsync(
            Guid buyerId, List<Guid> scope, List<SilaLiveInventoryRowDto> rows, CancellationToken cancellationToken)
        {
            if (rows.Count == 0)
            {
                return;
            }

            DateTime today = DateTime.UtcNow.Date;
            DateTime from = today.AddDays(-6);
            List<Guid> ids = rows.Select(x => x.MaterialId).ToList();
            var issues = await _repository.InventoryTransaction
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive && scope.Contains(x.LocationId)
                    && ids.Contains(x.MaterialId) && x.Direction == Domain.Common.Common.SILA_DIRECTION_OUT && x.BusinessDate >= from)
                .Select(x => new { x.MaterialId, x.BusinessDate, x.Quantity })
                .ToListAsync(cancellationToken);
            ILookup<Guid, (DateTime Day, decimal Quantity)> byMaterial = issues
                .ToLookup(x => x.MaterialId, x => (x.BusinessDate.Date, x.Quantity));
            foreach (SilaLiveInventoryRowDto row in rows)
            {
                List<(DateTime Day, decimal Quantity)> own = byMaterial[row.MaterialId].ToList();
                row.Last7DayIssues = Enumerable.Range(0, 7)
                    .Select(offset => own.Where(x => x.Day == from.AddDays(offset)).Sum(x => x.Quantity))
                    .ToList();
            }
        }
    }
}
