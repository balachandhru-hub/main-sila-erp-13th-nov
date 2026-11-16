using Buyer.Application.Features.Shared;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using MaterialEntity = Buyer.Domain.Entities.ItemBuyerMaster;

namespace Buyer.Application.Features.Queries.GetSilaMaterialDetail
{
    /// <summary>
    /// Material Master detail: header, plant/SLoc assignments, SILA ledger inventory by mapped location,
    /// UOM conversions, valuation fields, and integration exceptions. Does not overwrite ledger with SAP snapshots.
    /// </summary>
    public class GetSilaMaterialDetailQueryHandler : IRequestHandler<GetSilaMaterialDetailQuery, SilaMaterialDetailDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSilaMaterialDetailQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SilaMaterialDetailDto> Handle(GetSilaMaterialDetailQuery request, CancellationToken cancellationToken)
        {
            BuyerBusinessProfile buyer = await SilaAccess.GetBuyerAsync(_repository, _logger, request.OrganizationId);
            MaterialEntity? material = await _repository.ItemBuyerMaster
                .FindByCondition(x => x.Id == request.MaterialId && x.BuyerId == buyer.Id && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (material == null)
            {
                throw new NotFoundCustomException("Material not found.", "The material is missing or inactive.");
            }

            Dictionary<Guid, MaterialPriceChange> pending = await SilaMaterialRules.GetPendingChangesAsync(
                _repository, buyer.Id, new[] { material.Id }, cancellationToken);
            Dictionary<Guid, List<MaterialUomConversion>> conversions = await UomConverter.GetConversionsAsync(
                _repository, new[] { material.Id }, cancellationToken);
            SilaMaterialDto header = SilaMaterialRules.ToDto(
                material,
                conversions.GetValueOrDefault(material.Id),
                pending.GetValueOrDefault(material.Id));

            Dictionary<string, string> propertyByPlant = (await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken))
                .GroupBy(x => x.PlantCode.Trim().ToUpperInvariant())
                .ToDictionary(x => x.Key, x => x.First().PropertyName, StringComparer.OrdinalIgnoreCase);

            List<MaterialPlantAssignment> plants = await _repository.MaterialPlantAssignment
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialId == material.Id && x.IsActive)
                .OrderBy(x => x.PlantCode)
                .ToListAsync(cancellationToken);

            List<MaterialStorageLocationAssignment> slocs = await _repository.MaterialStorageLocationAssignment
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialId == material.Id && x.IsActive)
                .OrderBy(x => x.PlantCode).ThenBy(x => x.StorageLocationCode)
                .ToListAsync(cancellationToken);

            List<Guid> locationIds = slocs.Where(x => x.InventoryLocationId != null).Select(x => x.InventoryLocationId!.Value).Distinct().ToList();
            Dictionary<Guid, InventoryLocation> locations = locationIds.Count == 0
                ? new Dictionary<Guid, InventoryLocation>()
                : await _repository.InventoryLocation
                    .FindByCondition(x => locationIds.Contains(x.Id) && x.BuyerId == buyer.Id && x.IsActive)
                    .ToDictionaryAsync(x => x.Id, cancellationToken);

            List<InventoryBalance> balances = locationIds.Count == 0
                ? new List<InventoryBalance>()
                : await _repository.InventoryBalance
                    .FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialId == material.Id && locationIds.Contains(x.LocationId) && x.IsActive)
                    .ToListAsync(cancellationToken);

            List<MaterialErpStockSnapshot> snapshots = await _repository.MaterialErpStockSnapshot
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialId == material.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            List<MaterialIntegrationException> exceptions = await _repository.MaterialIntegrationException
                .FindByCondition(x => x.BuyerId == buyer.Id && x.MaterialId == material.Id && x.IsActive && !x.Resolved)
                .OrderByDescending(x => x.DateCreated)
                .Take(20)
                .ToListAsync(cancellationToken);

            SilaMaterialDetailDto detail = new SilaMaterialDetailDto
            {
                Id = header.Id,
                MaterialCode = header.MaterialCode,
                Description = header.Description,
                MaterialGroup = header.MaterialGroup,
                BaseUom = header.BaseUom,
                UnitCost = header.UnitCost,
                Currency = header.Currency,
                Barcode = header.Barcode,
                IsInventoryItem = header.IsInventoryItem,
                InventoryType = header.InventoryType,
                BatchManaged = header.BatchManaged,
                ExpiryManaged = header.ExpiryManaged,
                ShelfLifeDays = header.ShelfLifeDays,
                SerialManaged = header.SerialManaged,
                StandardPrice = header.StandardPrice,
                MovingAveragePrice = header.MovingAveragePrice,
                PriceStatus = header.PriceStatus,
                PendingPriceChangeId = header.PendingPriceChangeId,
                ProposedUnitCost = header.ProposedUnitCost,
                ProposedPriceUom = header.ProposedPriceUom,
                Conversions = header.Conversions,
                Source = header.Source,
                UpdatedAt = header.UpdatedAt,
                ApprovedPriceUom = header.ApprovedPriceUom,
                PriceApprovedOn = header.PriceApprovedOn,
                CompanyCode = header.CompanyCode,
                PriceControl = header.PriceControl,
                ValuationClass = header.ValuationClass,
                MaterialType = header.MaterialType,
                Category = header.Category,
                AlternateUom = header.AlternateUom,
                OrderUom = header.OrderUom,
                Plants = plants.Select(x => new SilaMaterialPlantAssignmentDto
                {
                    Id = x.Id,
                    PlantCode = x.PlantCode,
                    PropertyName = propertyByPlant.GetValueOrDefault(x.PlantCode.ToUpperInvariant()),
                    PlantStatus = x.PlantStatus,
                    PurchasingGroup = x.PurchasingGroup,
                    MrpType = x.MrpType,
                    ProcurementType = x.ProcurementType,
                    ReorderPoint = x.ReorderPoint,
                    SafetyStock = x.SafetyStock,
                    MinLotSize = x.MinLotSize,
                    MaxLotSize = x.MaxLotSize,
                    PlannedDeliveryDays = x.PlannedDeliveryDays,
                    LastSyncedOn = x.LastSyncedOn
                }).ToList(),
                StorageLocations = slocs.Select(x =>
                {
                    InventoryLocation? loc = x.InventoryLocationId != null && locations.TryGetValue(x.InventoryLocationId.Value, out InventoryLocation? found) ? found : null;
                    return new SilaMaterialStorageLocationAssignmentDto
                    {
                        Id = x.Id,
                        PlantCode = x.PlantCode,
                        StorageLocationCode = x.StorageLocationCode,
                        InventoryLocationId = x.InventoryLocationId,
                        SilaLocationName = loc?.LocationName,
                        SilaLocationType = loc?.LocationType,
                        AssignmentStatus = x.AssignmentStatus,
                        Mapped = x.InventoryLocationId != null,
                        LastSyncedOn = x.LastSyncedOn
                    };
                }).ToList(),
                Valuation = new SilaMaterialValuationDto
                {
                    ValuationArea = null, // Not assumed equal to plant; only shown when mapped/persisted later.
                    PriceControl = header.PriceControl,
                    StandardPrice = material.StandardPrice,
                    MovingAveragePrice = material.MovingAveragePrice,
                    ApprovedUnitCost = material.UnitCost,
                    Currency = material.Currency,
                    ValuationClass = material.ValuationClass,
                    LastValuationRefresh = plants.Max(x => x.LastSyncedOn) ?? material.DateUpdated
                },
                Integration = new SilaMaterialIntegrationStatusDto
                {
                    LastMaterialSync = plants.Max(x => x.LastSyncedOn) ?? (material.Source == SilaMaterialErpSync.SOURCE_ERP ? material.DateUpdated : null),
                    LastStockSync = snapshots.Count == 0 ? null : snapshots.Max(x => x.SnapshotOn),
                    LastValuationSync = plants.Max(x => x.LastSyncedOn),
                    SourceSystem = material.Source,
                    OpenExceptions = exceptions.Count,
                    ExceptionMessages = exceptions.Select(x => x.Message).ToList()
                }
            };

            Dictionary<Guid, string> propertyPlantById = (await _repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyer.Id && x.IsActive)
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.Id, x => x.PlantCode);

            foreach (InventoryBalance balance in balances)
            {
                InventoryLocation? loc = locations.GetValueOrDefault(balance.LocationId);
                string? plantCode = loc != null && propertyPlantById.TryGetValue(loc.PropertyId, out string? plant) ? plant : null;
                MaterialErpStockSnapshot? snap = snapshots
                    .Where(x => x.InventoryLocationId == balance.LocationId && x.StockCategory == "UNRESTRICTED")
                    .OrderByDescending(x => x.SnapshotOn)
                    .FirstOrDefault();
                detail.Inventory.Add(new SilaMaterialInventoryContextDto
                {
                    InventoryLocationId = balance.LocationId,
                    LocationName = loc?.LocationName,
                    PlantCode = plantCode,
                    StorageLocationCode = loc?.StorageLocationCode,
                    OnHandQty = balance.OnHandQty,
                    InTransitQty = balance.InTransitQty,
                    AvailableQty = balance.OnHandQty, // Quality/blocked never treated as free stock in SILA ledger model.
                    BaseUom = balance.BaseUom,
                    UnitCost = balance.UnitCost ?? material.UnitCost,
                    LastMovementOn = balance.LastMovementOn,
                    ErpSnapshotOn = snap?.SnapshotOn,
                    ErpUnrestrictedQty = snap?.Quantity,
                    StockFreshness = Freshness(balance.LastMovementOn, snap?.SnapshotOn)
                });
            }

            return detail;
        }

        private static string Freshness(DateTime? lastMovement, DateTime? snapshot)
        {
            DateTime? stamp = lastMovement ?? snapshot;
            if (stamp == null) return "UNKNOWN";
            return (DateTime.UtcNow - stamp.Value).TotalHours <= 24 ? "FRESH" : "STALE";
        }
    }
}
