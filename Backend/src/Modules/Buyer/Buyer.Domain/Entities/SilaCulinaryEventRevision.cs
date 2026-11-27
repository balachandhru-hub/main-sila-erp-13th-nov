using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Phase 2A Immutable Event Baseline and Revision History (§44, §45).</summary>
    public class SilaCulinaryEventRevision : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid EventId { get; set; }

        public int RevisionNumber { get; set; }

        [Required]
        [MaxLength(40)]
        public string StatusAtRevision { get; set; } = "APPROVED";

        public bool IsApprovedBaseline { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        public int GuaranteedGuestsAtRevision { get; set; }

        public int PlanningGuestsAtRevision { get; set; }

        [Precision(18, 4)]
        public decimal MenuCostAtRevision { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        [Required]
        public string SnapshotJson { get; set; } = "{}";

        [MaxLength(160)]
        public string? CreatedByName { get; set; }
    }
}
