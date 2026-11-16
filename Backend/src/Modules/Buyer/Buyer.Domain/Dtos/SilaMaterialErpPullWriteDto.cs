namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// Plant-first Material Master pull (GET_MATERIAL). Prefer <see cref="PlantCodes"/> from BuyerProperty mappings;
    /// company code is derived from those properties when omitted.
    /// </summary>
    public class SilaMaterialErpPullWriteDto
    {
        /// <summary>
        /// Optional when <see cref="PlantCodes"/> is set — resolved from BuyerProperty.CompanyCode of the selected plants.
        /// Used for ResolveIntegration (entity then ALL).
        /// </summary>
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// SAP plant codes from BuyerProperty mappings (preferred pull scope). Empty with a company code =
        /// all plants present in the pull payload. Does not invent plant numbers.
        /// </summary>
        public List<string> PlantCodes { get; set; } = new();
    }
}
