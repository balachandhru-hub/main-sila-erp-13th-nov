using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Durable Phase 1C bridge and idempotency anchor linking an AI Recipe Draft revision, approved costing snapshot,
    /// production Recipe ID (RI########), image assignment, publishing status, and Phase 2A handover metadata (§43–§48, §59).
    /// </summary>
    public class SilaCulinaryRecipeLifecycle : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid DraftId { get; set; }

        public int ApprovedDraftRevision { get; set; }

        public Guid? CostingSnapshotId { get; set; }

        public Guid? ProductionRecipeId { get; set; }

        [MaxLength(60)]
        public string? ProductionRecipeCode { get; set; }

        public int? ProductionRecipeVersion { get; set; }

        /// <summary>DRAFT, READINESS_PENDING, PENDING_APPROVAL, APPROVED, RECIPE_ID_CREATED, PUBLISHED, SUPERSEDED, ARCHIVED, REJECTED.</summary>
        [Required]
        [MaxLength(50)]
        public string LifecycleStatus { get; set; } = "DRAFT";

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [MaxLength(120)]
        public string? CorrelationId { get; set; }

        public bool ImageAssigned { get; set; }

        [MaxLength(40)]
        public string? ImageOrigin { get; set; }

        [MaxLength(500)]
        public string? ApprovalComment { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        [MaxLength(200)]
        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        public DateTime? PublishedOnUtc { get; set; }

        public DateTime? EffectiveDateUtc { get; set; }

        public string? SubRecipeDependenciesJson { get; set; }

        public string? ReadinessSummaryJson { get; set; }
    }
}
