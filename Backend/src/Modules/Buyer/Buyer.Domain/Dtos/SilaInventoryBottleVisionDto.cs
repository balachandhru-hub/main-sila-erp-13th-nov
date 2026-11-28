namespace Buyer.Domain.Dtos
{
    /// <summary>Fill level of one photographed bottle, as estimated by the Vision AI engine.</summary>
    public class SilaInventoryBottleVisionDto
    {
        public bool BottleDetected { get; set; }
        /// <summary>Liquid left as a fraction of a sealed bottle, 0 to 1. Null when no level could be read.</summary>
        public double? FillRatio { get; set; }
        public double? RemainingMl { get; set; }
        public double? ConsumedMl { get; set; }
        public double NominalMl { get; set; }
        /// <summary>SEALED | OPENED | UNKNOWN</summary>
        public string SealStatus { get; set; } = "UNKNOWN";
        public bool? IsOpened { get; set; }
        public double Confidence { get; set; }
        /// <summary>CV_HEURISTIC | LLM_VISION | LLM_VISION+CV | NONE</summary>
        public string Method { get; set; } = "NONE";
        public string? Provider { get; set; }
        public string? Model { get; set; }
        public double? CvFillRatio { get; set; }
        public double? LlmFillRatio { get; set; }
        public double? LevelLineRatio { get; set; }
        public List<double>? BottleBox { get; set; }
        public string LabelText { get; set; } = string.Empty;
        public bool NeedsManualReview { get; set; } = true;
        public List<string> Warnings { get; set; } = new();
        public List<string> Notices { get; set; } = new();
    }
}
