namespace Buyer.Domain.Dtos
{
    /// <summary>Read-only recipe explosion and theoretical cost before inventory deduction / SAP posting.</summary>
    public class SilaPosConsumptionPreviewDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public List<SilaPosConsumptionPreviewLineDto> Lines { get; set; } = new();
        public decimal TotalSalesAmount { get; set; }
        public decimal TotalConsumptionCost { get; set; }
    }

    public class SilaPosConsumptionPreviewLineDto
    {
        public Guid SaleTransactionId { get; set; }
        public string? BusinessDate { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public int LineId { get; set; }
        public string PosCode { get; set; } = string.Empty;
        public decimal QuantitySold { get; set; }
        public string? Uom { get; set; }
        public string OutletCode { get; set; } = string.Empty;
        public string? OutletLocationName { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocationCode { get; set; }
        public decimal? SalesAmount { get; set; }
        public string? Currency { get; set; }
        public string? RecipeCode { get; set; }
        public int? RecipeVersion { get; set; }
        public string MatchStatus { get; set; } = string.Empty;
        public string? Message { get; set; }
        public int IngredientCount { get; set; }
        public decimal? ConsumptionCost { get; set; }
        public bool CostPending { get; set; }
        public decimal? GrossContribution { get; set; }
        public decimal? FoodCostPercent { get; set; }
        public decimal? GrossMarginPercent { get; set; }
        public List<SilaPosConsumptionPreviewIngredientDto> Ingredients { get; set; } = new();
    }

    public class SilaPosConsumptionPreviewIngredientDto
    {
        public Guid MaterialId { get; set; }
        public string? MaterialCode { get; set; }
        public string? MaterialDescription { get; set; }
        /// <summary>Recipe ingredient quantity in entry UOM (what SAP posts, e.g. 60 ML).</summary>
        public decimal Quantity { get; set; }
        /// <summary>Recipe ingredient UOM for SAP (e.g. ML).</summary>
        public string? EntryUom { get; set; }
        /// <summary>Material base quantity for ME stock / costing.</summary>
        public decimal BaseQuantity { get; set; }
        public string? BaseUom { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? EstimatedCost { get; set; }
        public bool CostPending { get; set; }
    }
}
