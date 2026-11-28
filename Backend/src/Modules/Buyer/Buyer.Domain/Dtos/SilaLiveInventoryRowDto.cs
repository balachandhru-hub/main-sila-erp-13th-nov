namespace Buyer.Domain.Dtos
{
    /// <summary>A material with its stock summed over the locations searched.</summary>
    public class SilaLiveInventoryRowDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BaseUom { get; set; } = string.Empty;
        public decimal OnHandQty { get; set; }
        public decimal InTransitQty { get; set; }
        /// <summary>Locations holding a balance of the material.</summary>
        public int LocationCount { get; set; }
        /// <summary>On open outbound ITOs not yet dispatched (single-location search only).</summary>
        public decimal ReservedQty { get; set; }
        /// <summary>On hand minus reserved (single-location search only).</summary>
        public decimal AvailableQty { get; set; }
        /// <summary>NEGATIVE | OUT | LOW | HEALTHY | EXCESS | IN_STOCK when a location is scoped.</summary>
        public string? StockStatus { get; set; }
        /// <summary>Effective reorder point at the scoped location, when stocking is maintained.</summary>
        public decimal? ReorderPoint { get; set; }
        /// <summary>Last ledger movement at the scoped location (or newest across locations).</summary>
        public DateTime? LastMovementOn { get; set; }
        public string? MaterialGroup { get; set; }
        /// <summary>BEVERAGE | FOOD | MINIBAR | TOBACCO | GENERAL, derived from the Item Master and the location.</summary>
        public string ProductClass { get; set; } = "GENERAL";
        public string? Barcode { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Currency { get; set; }
        /// <summary>Par level at the scoped location, when maintained.</summary>
        public decimal? ParLevel { get; set; }
        public decimal? MaximumStock { get; set; }
        /// <summary>Quantity issued out of the scoped location on each of the last 7 days, oldest first.</summary>
        public List<decimal> Last7DayIssues { get; set; } = new();
    }
}
