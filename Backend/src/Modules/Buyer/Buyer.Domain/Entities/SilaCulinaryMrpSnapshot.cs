using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMrpSnapshot : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("MrpRun")]
        public Guid MrpRunId { get; set; }

        public SilaCulinaryMrpRun MrpRun { get; set; } = null!;

        public int VersionNumber { get; set; }

        [Required]
        [MaxLength(40)]
        public string StatusAtSnapshot { get; set; } = string.Empty;

        public bool IsApprovedBaseline { get; set; }

        [MaxLength(500)]
        public string? ChangeReason { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        public string? InputSnapshotJson { get; set; }

        public string? OutputSnapshotJson { get; set; }

        [MaxLength(160)]
        public string? CreatedByName { get; set; }
    }
}
