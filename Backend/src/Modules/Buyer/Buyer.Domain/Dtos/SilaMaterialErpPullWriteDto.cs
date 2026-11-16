namespace Buyer.Domain.Dtos
{
    /// <summary>Which company code's materials to pull from the ERP (GET_MATERIAL), optionally scoped to plants.</summary>
    public class SilaMaterialErpPullWriteDto
    {
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// Optional SAP plant codes from BuyerProperty mappings. Empty = all plants present in the pull payload.
        /// Does not invent plant numbers.
        /// </summary>
        public List<string> PlantCodes { get; set; } = new();
    }
}
