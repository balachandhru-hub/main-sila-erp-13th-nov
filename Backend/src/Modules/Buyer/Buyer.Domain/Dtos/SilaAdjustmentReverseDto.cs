namespace Buyer.Domain.Dtos
{
    /// <summary>Partial or full reverse of a waste/damage adjustment via FIVE_STOCK_WASTE_UPDATE Z02.</summary>
    public class SilaAdjustmentReverseDto
    {
        /// <summary>Quantity to reverse (positive). Defaults to the original line quantity when omitted.</summary>
        public decimal? Quantity { get; set; }
        /// <summary>Unit of the reverse quantity (e.g. ML). Defaults to the original line UOM.</summary>
        public string? Uom { get; set; }
        /// <summary>Optional material; defaults to the first (or only) line on the original adjustment.</summary>
        public Guid? MaterialId { get; set; }
        public string? Reason { get; set; }
    }
}
