namespace Buyer.Domain.Dtos
{
    /// <summary>Candidate materials from SILA Material Recognition Engine. User must confirm before counting.</summary>
    public class SilaStockCountPhotoIdentifyDto
    {
        public bool Configured { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SilaStockCountPhotoCandidateDto> Candidates { get; set; } = new List<SilaStockCountPhotoCandidateDto>();
        /// <summary>True when visual match is ambiguous — UI should offer Scan Barcode Instead.</summary>
        public bool PreferBarcodeFallback { get; set; }
    }

    public class SilaStockCountPhotoCandidateDto
    {
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string BaseUom { get; set; } = string.Empty;
        /// <summary>Calibrated 0–1 confidence from recognition ranking (not a hardcoded display value).</summary>
        public double Confidence { get; set; }
        public string? Brand { get; set; }
        public string? Category { get; set; }
        public string? PackSize { get; set; }
    }
}
