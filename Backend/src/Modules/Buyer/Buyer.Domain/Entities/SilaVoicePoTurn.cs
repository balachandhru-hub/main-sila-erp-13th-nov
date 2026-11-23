using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>One turn in a Voice PO conversation (user speech/text or agent reply).</summary>
    public class SilaVoicePoTurn : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("SilaVoicePoSession")]
        public Guid SessionId { get; set; }

        public SilaVoicePoSession SilaVoicePoSession { get; set; } = null!;

        public int TurnNumber { get; set; }

        /// <summary>USER or AGENT.</summary>
        [Required]
        public string Role { get; set; } = "USER";

        public string? Channel { get; set; }
        public string? Transcript { get; set; }
        public string? SpokenText { get; set; }
        public string? StructuredJson { get; set; }
        public string? TranscriptionStatus { get; set; }
        public decimal? Confidence { get; set; }
    }
}
