namespace Buyer.Domain.Dtos
{
    /// <summary>Create or edit a recipe: header, ingredients and the menu price of each outlet.</summary>
    public class SilaRecipeWriteDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        /// <summary>Ignored when CategoryId is set (the category name is written).</summary>
        public string? Category { get; set; }
        public Guid? FamilyId { get; set; }
        public Guid? CategoryId { get; set; }
        /// <summary>DIRECT | RECIPE | BATCH</summary>
        public string ItemMode { get; set; } = string.Empty;
        /// <summary>FOOD | BEVERAGE | MINIBAR | TOBACCO</summary>
        public string? ProductClass { get; set; }
        /// <summary>STANDARD_RECIPE | DIRECT_ITEM | BATCH_RECIPE | SUB_RECIPE | COMBO | VARIANT_RECIPE</summary>
        public string? RecipeFormat { get; set; }
        public string? Subcategory { get; set; }
        public decimal ServingQty { get; set; }
        public string ServingUom { get; set; } = string.Empty;
        /// <summary>Unit the POS sells in; EA when empty.</summary>
        public string? SellingUom { get; set; }
        public string? PosCode { get; set; }
        /// <summary>POS item description (display only).</summary>
        public string? PosItem { get; set; }
        public string? Currency { get; set; }
        public decimal? TrimLossPercent { get; set; }
        public decimal? CookingLossPercent { get; set; }
        public decimal? WasteFactorPercent { get; set; }
        public int? PrepTimeMinutes { get; set; }
        public int? CookTimeMinutes { get; set; }
        /// <summary>JSON array of steps or plain text chef notes.</summary>
        public string? PreparationSteps { get; set; }
        public List<SilaRecipeIngredientWriteDto> Ingredients { get; set; } = new();
        public List<SilaRecipeOutletPriceWriteDto> OutletPrices { get; set; } = new();
    }
}
