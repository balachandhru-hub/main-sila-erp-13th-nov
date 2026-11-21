namespace Buyer.Domain.Dtos
{
    /// <summary>Outcome of a sales file upload, POS API pull, or confirm-posting run (local → SAP → tracker).</summary>
    public class SilaPosImportResultDto
    {
        public Guid BatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public int Rows { get; set; }
        public int Accepted { get; set; }
        public int Duplicates { get; set; }
        public int Invalid { get; set; }
        /// <summary>Step 1: accepted lines whose stock was validated and deducted locally.</summary>
        public int Processed { get; set; }
        /// <summary>Accepted lines that failed matching or deduction; the tracker shows why.</summary>
        public int Failed { get; set; }
        /// <summary>Lines of the batch that were already deducted or posted before this run (not processed again).</summary>
        public int AlreadyPosted { get; set; }
        /// <summary>Step 1 alias: lines whose SILA inventory was updated in this run.</summary>
        public int LocalUpdated { get; set; }
        /// <summary>Step 2: lines whose SAP material document posted successfully.</summary>
        public int SapPosted { get; set; }
        /// <summary>Step 2: lines whose SAP material document failed.</summary>
        public int SapFailed { get; set; }
        /// <summary>Lines of the batch whose ERP posting outcome is unknown (reconcile under ERP postings).</summary>
        public int PostingUnknown { get; set; }
        public List<SilaPosRowErrorDto> Errors { get; set; } = new();
    }
}
