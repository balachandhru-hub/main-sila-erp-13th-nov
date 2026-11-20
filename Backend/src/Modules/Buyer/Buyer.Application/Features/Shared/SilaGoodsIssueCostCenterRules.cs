using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Maintain and resolve Goods Issue cost centers:
    /// PLANT = Property + Plant → CostCenter;
    /// STORAGE = Property + Plant + StorageLocation → CostCenter (preferred).
    /// </summary>
    public static class SilaGoodsIssueCostCenterRules
    {
        private const int MAX_CODE = 50;

        public static async Task<List<SilaGoodsIssueCostCenterDto>> ListAsync(
            IRepositoryWrapper repository, Guid buyerId, string? scope, CancellationToken cancellationToken)
        {
            IQueryable<GoodsIssueCostCenter> query = repository.GoodsIssueCostCenter
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive);
            if (!string.IsNullOrWhiteSpace(scope))
            {
                string normalized = scope.Trim().ToUpperInvariant();
                query = query.Where(x => x.Scope == normalized);
            }

            List<GoodsIssueCostCenter> rows = await query
                .OrderBy(x => x.PlantCode)
                .ThenBy(x => x.Scope)
                .ThenBy(x => x.StorageLocationCode)
                .ToListAsync(cancellationToken);
            Dictionary<Guid, BuyerProperty> properties = await repository.BuyerProperty
                .FindByCondition(x => x.BuyerId == buyerId)
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            return rows.Select(row => ToDto(row, properties.GetValueOrDefault(row.PropertyId))).ToList();
        }

        public static async Task<Guid> SaveAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid? id,
            SilaGoodsIssueCostCenterWriteDto request,
            CancellationToken cancellationToken)
        {
            (string scope, string storage, string costCenter, BuyerProperty property) = await ValidateAsync(
                repository, logger, buyerId, request, cancellationToken);

            GoodsIssueCostCenter? existing = id == null
                ? null
                : await repository.GoodsIssueCostCenter
                    .FindFirstByConditionAsync(x => x.Id == id.Value && x.BuyerId == buyerId && x.IsActive);
            if (id != null && existing == null)
            {
                logger.LogError($"Goods issue cost center not found. Id: {id}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Cost center mapping not found.", "Select an active Goods Issue cost center mapping.");
            }

            GoodsIssueCostCenter? duplicate = await repository.GoodsIssueCostCenter
                .FindFirstByConditionAsync(x => x.BuyerId == buyerId
                    && x.IsActive
                    && x.PropertyId == property.Id
                    && x.Scope == scope
                    && x.StorageLocationCode == storage
                    && (id == null || x.Id != id.Value));
            if (duplicate != null)
            {
                logger.LogError($"Duplicate goods issue cost center. PropertyId: {property.Id}, Scope: {scope}, Storage: {storage}");
                throw new ConflictCustomException(
                    "Cost center mapping already exists.",
                    scope == GoodsIssueCostCenter.SCOPE_PLANT
                        ? $"A plant-level cost center is already set for property {property.PropertyName} / plant {property.PlantCode}."
                        : $"A storage-level cost center is already set for plant {property.PlantCode} / storage {storage}.");
            }

            GoodsIssueCostCenter row = existing ?? new GoodsIssueCostCenter
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                IsActive = true
            };
            if (existing == null)
            {
                repository.GoodsIssueCostCenter.Create(row);
            }

            row.PropertyId = property.Id;
            row.PlantCode = property.PlantCode.Trim().ToUpperInvariant();
            row.Scope = scope;
            row.StorageLocationCode = storage;
            row.CostCenter = costCenter;
            row.IsActive = true;

            if (scope == GoodsIssueCostCenter.SCOPE_STORAGE)
            {
                await SyncLocationCostCentersAsync(repository, buyerId, property.Id, storage, costCenter, cancellationToken);
            }

            await repository.SaveAsync();
            return row.Id;
        }

        public static async Task DeleteAsync(
            IRepositoryWrapper repository, ILoggerManager logger, Guid buyerId, Guid id, CancellationToken cancellationToken)
        {
            GoodsIssueCostCenter? row = await repository.GoodsIssueCostCenter
                .FindFirstByConditionAsync(x => x.Id == id && x.BuyerId == buyerId && x.IsActive);
            if (row == null)
            {
                logger.LogError($"Goods issue cost center not found for delete. Id: {id}");
                throw new NotFoundCustomException("Cost center mapping not found.", "Select an active Goods Issue cost center mapping.");
            }

            row.IsActive = false;
            await repository.SaveAsync();
        }

        /// <summary>
        /// Resolve cost center for Z01/Z02: STORAGE mapping → PLANT mapping → location.CostCenter fallback.
        /// </summary>
        public static async Task<string?> ResolveAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid? propertyId,
            string? plantCode,
            string? storageLocationCode,
            string? locationCostCenterFallback,
            CancellationToken cancellationToken)
        {
            if (propertyId == null && string.IsNullOrWhiteSpace(plantCode))
            {
                return string.IsNullOrWhiteSpace(locationCostCenterFallback) ? null : locationCostCenterFallback.Trim();
            }

            string plant = (plantCode ?? string.Empty).Trim().ToUpperInvariant();
            string storage = (storageLocationCode ?? string.Empty).Trim().ToUpperInvariant();

            List<GoodsIssueCostCenter> rows = await repository.GoodsIssueCostCenter
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive
                    && (propertyId == null || x.PropertyId == propertyId)
                    && (plant.Length == 0 || x.PlantCode == plant))
                .ToListAsync(cancellationToken);

            if (storage.Length > 0)
            {
                GoodsIssueCostCenter? storageMatch = rows.FirstOrDefault(x =>
                    x.Scope == GoodsIssueCostCenter.SCOPE_STORAGE
                    && string.Equals(x.StorageLocationCode, storage, StringComparison.OrdinalIgnoreCase));
                if (storageMatch != null)
                {
                    return storageMatch.CostCenter;
                }
            }

            GoodsIssueCostCenter? plantMatch = rows.FirstOrDefault(x => x.Scope == GoodsIssueCostCenter.SCOPE_PLANT);
            if (plantMatch != null)
            {
                return plantMatch.CostCenter;
            }

            return string.IsNullOrWhiteSpace(locationCostCenterFallback) ? null : locationCostCenterFallback.Trim();
        }

        private static async Task<(string Scope, string Storage, string CostCenter, BuyerProperty Property)> ValidateAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            SilaGoodsIssueCostCenterWriteDto? request,
            CancellationToken cancellationToken)
        {
            if (request == null || request.PropertyId == Guid.Empty)
            {
                throw new BadRequestCustomException("Property is required.", "Select the property (plant) for this Goods Issue cost center.");
            }

            string scope = (request.Scope ?? string.Empty).Trim().ToUpperInvariant();
            if (scope != GoodsIssueCostCenter.SCOPE_PLANT && scope != GoodsIssueCostCenter.SCOPE_STORAGE)
            {
                throw new BadRequestCustomException("Scope is invalid.", "Use PLANT (property-plant-cost center) or STORAGE (property-plant-storage-cost center).");
            }

            string costCenter = (request.CostCenter ?? string.Empty).Trim();
            if (costCenter.Length == 0 || costCenter.Length > MAX_CODE)
            {
                throw new BadRequestCustomException("Cost center is required.", $"Enter a SAP cost center of at most {MAX_CODE} characters (e.g. 1060050124).");
            }

            string storage = (request.StorageLocationCode ?? string.Empty).Trim().ToUpperInvariant();
            if (scope == GoodsIssueCostCenter.SCOPE_PLANT)
            {
                storage = string.Empty;
            }
            else if (storage.Length == 0 || storage.Length > MAX_CODE)
            {
                throw new BadRequestCustomException(
                    "Storage location is required.",
                    "Enter the SAP storage location / outlet code (e.g. FL10) for a storage-level cost center.");
            }

            BuyerProperty? property = await repository.BuyerProperty
                .FindFirstByConditionAsync(x => x.Id == request.PropertyId && x.BuyerId == buyerId && x.IsActive);
            if (property == null)
            {
                logger.LogError($"Property not found for goods issue cost center. PropertyId: {request.PropertyId}");
                throw new NotFoundCustomException("Property not found.", "Select an active property of this organization.");
            }

            return (scope, storage, costCenter, property);
        }

        /// <summary>Keep InventoryLocation.CostCenter aligned for STORAGE mappings (outlet / SLoc).</summary>
        private static async Task SyncLocationCostCentersAsync(
            IRepositoryWrapper repository,
            Guid buyerId,
            Guid propertyId,
            string storageLocationCode,
            string costCenter,
            CancellationToken cancellationToken)
        {
            List<InventoryLocation> locations = await repository.InventoryLocation
                .FindByCondition(x => x.BuyerId == buyerId
                    && x.PropertyId == propertyId
                    && x.IsActive
                    && ((x.StorageLocationCode != null && x.StorageLocationCode.ToUpper() == storageLocationCode)
                        || x.LocationCode.ToUpper() == storageLocationCode))
                .ToListAsync(cancellationToken);
            foreach (InventoryLocation location in locations)
            {
                location.CostCenter = costCenter;
            }
        }

        private static SilaGoodsIssueCostCenterDto ToDto(GoodsIssueCostCenter row, BuyerProperty? property)
        {
            return new SilaGoodsIssueCostCenterDto
            {
                Id = row.Id,
                PropertyId = row.PropertyId,
                PropertyName = property?.PropertyName ?? string.Empty,
                CompanyCode = property?.CompanyCode ?? string.Empty,
                PlantCode = row.PlantCode,
                Scope = row.Scope,
                StorageLocationCode = row.StorageLocationCode,
                CostCenter = row.CostCenter,
                Status = row.IsActive ? "ACTIVE" : "INACTIVE",
                DateUpdated = row.DateUpdated
            };
        }
    }
}
