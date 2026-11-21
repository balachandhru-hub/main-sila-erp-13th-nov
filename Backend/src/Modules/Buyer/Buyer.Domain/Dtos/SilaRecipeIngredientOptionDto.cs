namespace Buyer.Domain.Dtos
{
    /// <summary>A material offered as a recipe ingredient, with its approved price status and unit conversions.</summary>
    public class SilaRecipeIngredientOptionDto
    {
        public Guid Id { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? MaterialGroup { get; set; }
        /// <summary>Item Master product type.</summary>
        public string? Category { get; set; }
        public string BaseUom { get; set; } = string.Empty;
        public decimal? UnitCost { get; set; }
        public string? Currency { get; set; }
        /// <summary>Outlet prices that replace UnitCost at those outlets.</summary>
        public List<SilaMaterialOutletPriceDto> OutletPrices { get; set; } = new();
        /// <summary>APPROVED | MISSING | PENDING_APPROVAL</summary>
        public string PriceStatus { get; set; } = string.Empty;
        public List<SilaUomConversionDto> Conversions { get; set; } = new();
        /// <summary>Suppliers that delivered it on purchase orders.</summary>
        public List<string> Suppliers { get; set; } = new();

        /// <summary>SILA ledger on-hand at the requested outlet/location context; null when no outlet selected.</summary>
        public decimal? StockOnHand { get; set; }

        /// <summary>In-transit quantity at the requested location context.</summary>
        public decimal? StockInTransit { get; set; }

        /// <summary>Available = on-hand (ledger); quality/blocked are never treated as free stock here.</summary>
        public decimal? StockAvailable { get; set; }

        /// <summary>FRESH | STALE | UNKNOWN | NOT_DETERMINED — based on last movement / ERP snapshot age.</summary>
        public string StockFreshness { get; set; } = "NOT_DETERMINED";

        public string? StockLocationName { get; set; }
    }
}
