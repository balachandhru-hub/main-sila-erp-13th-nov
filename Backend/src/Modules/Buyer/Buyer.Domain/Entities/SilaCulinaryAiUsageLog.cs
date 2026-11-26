using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryAiUsageLog : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? DraftId { get; set; }

        [MaxLength(80)]
        public string? DraftReference { get; set; }

        [Required]
        [MaxLength(80)]
        public string GenerationReference { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CorrelationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string Feature { get; set; } = "RECIPE_GENERATE";

        [Required]
        [MaxLength(30)]
        public string Provider { get; set; } = "LOCAL";

        [MaxLength(200)]
        public string Model { get; set; } = string.Empty;

        public bool Success { get; set; }

        public bool UsedFallback { get; set; }

        public int InputTokens { get; set; }

        public int OutputTokens { get; set; }

        public int TotalTokens { get; set; }

        [Precision(18, 6)]
        public decimal EstimatedCostUsd { get; set; }

        public int LatencyMs { get; set; }

        public int ValidationRetries { get; set; }

        [MaxLength(60)]
        public string? ErrorCode { get; set; }

        [MaxLength(500)]
        public string? SanitizedError { get; set; }

        public Guid ActorUserId { get; set; }
    }
}
