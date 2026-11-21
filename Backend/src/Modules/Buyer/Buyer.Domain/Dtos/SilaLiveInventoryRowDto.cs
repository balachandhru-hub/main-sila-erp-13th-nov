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
    }
}
