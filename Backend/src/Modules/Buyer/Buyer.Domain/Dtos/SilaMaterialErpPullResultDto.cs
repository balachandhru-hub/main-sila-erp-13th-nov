namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// Outcome of an ERP material pull: records read, materials created/changed, description refresh, and failures.
    /// </summary>
    public class SilaMaterialErpPullResultDto
    {
        public string ConfigurationName { get; set; } = string.Empty;
        public int Read { get; set; }
        public int New { get; set; }
        public int Changed { get; set; }
        public int Unchanged { get; set; }
        public int Failed { get; set; }
        /// <summary>Price change requests created for approval.</summary>
        public int PriceChanges { get; set; }

        /// <summary>Existing materials whose Description was refreshed from EN ProductDescription.</summary>
        public int DescriptionsUpdated { get; set; }

        /// <summary>Materials with no EN ProductDescription (new failed or existing preserved).</summary>
        public int DescriptionsMissing { get; set; }

        /// <summary>Product/plant scope not found or not eligible.</summary>
        public int SkippedNotFound { get; set; }

        public List<string> Failures { get; set; } = new();
    }
}
