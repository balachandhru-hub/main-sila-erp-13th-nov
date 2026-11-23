using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Conversational Voice PO session. LLM never writes POs; drafts confirm then call CreatePurchaseOrder.
    /// </summary>
    public class SilaVoicePoSession : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        public string SessionNumber { get; set; } = string.Empty;

        /// <summary>OPEN, REVIEW, CONFIRMED, CREATED, CANCELLED, FAILED.</summary>
        [Required]
        public string Status { get; set; } = "OPEN";

        public Guid? LocationId { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? PurchasingProfileId { get; set; }
        public int? PurchasingProfileVersion { get; set; }
        public string? LatestTranscript { get; set; }
        public string? DraftJson { get; set; }
        public string? ClarificationJson { get; set; }
        public string? SpokenReply { get; set; }
        public string? ValidationStatus { get; set; }
        public string? ValidationErrors { get; set; }
        public string? IdempotencyKey { get; set; }
        public Guid? CreatedPurchaseOrderId { get; set; }
        public string? CreatedPoNumber { get; set; }
        public string? Language { get; set; }
        public DateTime? TranscriptRetainUntil { get; set; }
    }
}
