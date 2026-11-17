namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// Material Master pull (GET_MATERIAL). Optional plant and/or Product ID filters.
    /// Company code is derived from BuyerProperty when omitted.
    /// </summary>
    public class SilaMaterialErpPullWriteDto
    {
        /// <summary>
        /// Optional when plants or ProductId are set — resolved from BuyerProperty.CompanyCode.
        /// Used for ResolveIntegration (entity then ALL).
        /// </summary>
        public string CompanyCode { get; set; } = string.Empty;

        /// <summary>
        /// Optional SAP plant codes from BuyerProperty mappings. Empty = authorized configured plants
        /// (or all plants in payload when ProductId is also blank).
        /// </summary>
        public List<string> PlantCodes { get; set; } = new();

        /// <summary>
        /// Optional SAP Product ID (string; leading zeros preserved). Filters A_Product at the SAP request.
        /// </summary>
        public string? ProductId { get; set; }
    }
}
