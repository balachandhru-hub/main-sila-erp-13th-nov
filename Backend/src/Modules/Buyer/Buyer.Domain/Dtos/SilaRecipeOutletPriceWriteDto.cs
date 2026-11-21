namespace Buyer.Domain.Dtos
{
    public class SilaRecipeOutletPriceWriteDto
    {
        /// <summary>An inventory location of type OUTLET.</summary>
        public Guid OutletLocationId { get; set; }
        public decimal MenuPrice { get; set; }
        public string? Currency { get; set; }
        /// <summary>POS code at this outlet; when empty, the recipe header POS code is used.</summary>
        public string? PosCode { get; set; }
        /// <summary>POS item at this outlet; when empty, the recipe header POS item is used.</summary>
        public string? PosItem { get; set; }
    }
}
