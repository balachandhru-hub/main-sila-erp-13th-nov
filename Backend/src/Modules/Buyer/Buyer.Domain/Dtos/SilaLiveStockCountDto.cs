namespace Buyer.Domain.Dtos
{
    /// <summary>Live Inventory quick stock count: physical qty vs book stock → 701/702 variance.</summary>
    public class SilaLiveStockCountWriteDto
    {
        public Guid LocationId { get; set; }
        public Guid? MaterialId { get; set; }
        /// <summary>Material code or barcode when MaterialId is not supplied.</summary>
        public string? MaterialCodeOrBarcode { get; set; }
        public decimal PhysicalQty { get; set; }
        public string? Uom { get; set; }
        public string? Reason { get; set; }
    }

    public class SilaLiveStockCountPreviewDto
    {
        public Guid LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Uom { get; set; } = string.Empty;
        public decimal OnHandQty { get; set; }
        public decimal PhysicalQty { get; set; }
        public decimal VarianceQty { get; set; }
        public string? MovementType { get; set; }
    }

    public class SilaLiveStockCountResultDto
    {
        public Guid AdjustmentId { get; set; }
        public string AdjustmentNumber { get; set; } = string.Empty;
        public Guid? PostingId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public decimal OnHandQty { get; set; }
        public decimal PhysicalQty { get; set; }
        public decimal VarianceQty { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
