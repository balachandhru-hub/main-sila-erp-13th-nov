namespace Buyer.Domain.Dtos
{
    public class SilaMaterialPlantAssignmentDto
    {
        public Guid Id { get; set; }
        public string PlantCode { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string? PlantStatus { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? MrpType { get; set; }
        public string? ProcurementType { get; set; }
        public decimal? ReorderPoint { get; set; }
        public decimal? SafetyStock { get; set; }
        public decimal? MinLotSize { get; set; }
        public decimal? MaxLotSize { get; set; }
        public int? PlannedDeliveryDays { get; set; }
        public DateTime? LastSyncedOn { get; set; }
    }

    public class SilaMaterialStorageLocationAssignmentDto
    {
        public Guid Id { get; set; }
        public string PlantCode { get; set; } = string.Empty;
        public string StorageLocationCode { get; set; } = string.Empty;
        public Guid? InventoryLocationId { get; set; }
        public string? SilaLocationName { get; set; }
        public string? SilaLocationType { get; set; }
        public string? AssignmentStatus { get; set; }
        public bool Mapped { get; set; }
        public DateTime? LastSyncedOn { get; set; }
    }

    public class SilaMaterialInventoryContextDto
    {
        public Guid? InventoryLocationId { get; set; }
        public string? LocationName { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocationCode { get; set; }
        public decimal OnHandQty { get; set; }
        public decimal InTransitQty { get; set; }
        public decimal AvailableQty { get; set; }
        public string BaseUom { get; set; } = string.Empty;
        public decimal? UnitCost { get; set; }
        public DateTime? LastMovementOn { get; set; }
        public DateTime? ErpSnapshotOn { get; set; }
        public decimal? ErpUnrestrictedQty { get; set; }
        public string StockFreshness { get; set; } = "UNKNOWN";
    }

    public class SilaMaterialValuationDto
    {
        public string? ValuationArea { get; set; }
        public string? PriceControl { get; set; }
        public decimal? StandardPrice { get; set; }
        public decimal? MovingAveragePrice { get; set; }
        public decimal? ApprovedUnitCost { get; set; }
        public string? Currency { get; set; }
        public decimal? PriceUnit { get; set; }
        public string? ValuationClass { get; set; }
        public DateTime? LastValuationRefresh { get; set; }
    }

    public class SilaMaterialIntegrationStatusDto
    {
        public DateTime? LastMaterialSync { get; set; }
        public DateTime? LastStockSync { get; set; }
        public DateTime? LastValuationSync { get; set; }
        public string? SourceSystem { get; set; }
        public int OpenExceptions { get; set; }
        public List<string> ExceptionMessages { get; set; } = new();
    }

    /// <summary>Material Master detail with plant, SLoc, inventory, UOM, valuation and integration status.</summary>
    public class SilaMaterialDetailDto : SilaMaterialDto
    {
        public List<SilaMaterialPlantAssignmentDto> Plants { get; set; } = new();
        public List<SilaMaterialStorageLocationAssignmentDto> StorageLocations { get; set; } = new();
        public List<SilaMaterialInventoryContextDto> Inventory { get; set; } = new();
        public SilaMaterialValuationDto Valuation { get; set; } = new();
        public SilaMaterialIntegrationStatusDto Integration { get; set; } = new();
    }
}
