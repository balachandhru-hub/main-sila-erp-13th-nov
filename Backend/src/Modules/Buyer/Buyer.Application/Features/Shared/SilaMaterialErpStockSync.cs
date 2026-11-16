using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Shared
{
    /// <summary>
    /// Stores SAP stock snapshots without mutating InventoryBalance / InventoryLedger.
    /// Reconciliation compares snapshot vs ledger; never invents GRN/GI/adjustment postings.
    /// </summary>
    public static class SilaMaterialErpStockSync
    {
        public const string CATEGORY_UNRESTRICTED = "UNRESTRICTED";
        public const string CATEGORY_QUALITY = "QUALITY";
        public const string CATEGORY_BLOCKED = "BLOCKED";
        public const string CATEGORY_IN_TRANSFER = "IN_TRANSFER";
        public const string CATEGORY_IN_TRANSIT = "IN_TRANSIT";

        public static async Task UpsertSnapshotAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid buyerId,
            Guid materialId,
            string plantCode,
            string storageLocationCode,
            string stockCategory,
            decimal quantity,
            string baseUom,
            Guid? inventoryLocationId,
            string? batch,
            string? specialStock,
            CancellationToken cancellationToken)
        {
            string plant = plantCode.Trim().ToUpperInvariant();
            string sloc = storageLocationCode.Trim().ToUpperInvariant();
            string category = string.IsNullOrWhiteSpace(stockCategory) ? CATEGORY_UNRESTRICTED : stockCategory.Trim().ToUpperInvariant();
            DateTime now = DateTime.UtcNow;

            MaterialErpStockSnapshot? existing = await repository.MaterialErpStockSnapshot
                .FindByCondition(x => x.BuyerId == buyerId && x.MaterialId == materialId && x.IsActive
                    && x.PlantCode == plant && x.StorageLocationCode == sloc && x.StockCategory == category
                    && ((batch == null && x.Batch == null) || x.Batch == batch)
                    && ((specialStock == null && x.SpecialStock == null) || x.SpecialStock == specialStock))
                .FirstOrDefaultAsync(cancellationToken);

            if (existing != null)
            {
                existing.Quantity = quantity;
                existing.BaseUom = baseUom.Trim().ToUpperInvariant();
                existing.SnapshotOn = now;
                existing.InventoryLocationId = inventoryLocationId;
                existing.SourceSystem = SilaMaterialErpSync.SOURCE_ERP;
                repository.MaterialErpStockSnapshot.Update(existing);
                logger.LogInfo($"ERP stock snapshot updated. MaterialId: {materialId}, Plant: {plant}, SLoc: {sloc}, Category: {category}");
                return;
            }

            repository.MaterialErpStockSnapshot.Create(new MaterialErpStockSnapshot
            {
                Id = Guid.NewGuid(),
                BuyerId = buyerId,
                MaterialId = materialId,
                PlantCode = plant,
                StorageLocationCode = sloc,
                StockCategory = category,
                Batch = batch,
                SpecialStock = specialStock,
                Quantity = quantity,
                BaseUom = baseUom.Trim().ToUpperInvariant(),
                SnapshotOn = now,
                SourceSystem = SilaMaterialErpSync.SOURCE_ERP,
                InventoryLocationId = inventoryLocationId,
                IsActive = true
            });
            logger.LogInfo($"ERP stock snapshot created. MaterialId: {materialId}, Plant: {plant}, SLoc: {sloc}, Category: {category}");
        }
    }
}
