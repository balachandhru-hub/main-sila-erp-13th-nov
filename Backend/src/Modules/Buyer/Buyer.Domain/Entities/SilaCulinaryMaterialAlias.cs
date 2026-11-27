using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialAlias : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string AliasText { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string NormalizedAlias { get; set; } = string.Empty;

        [Required]
        public Guid MaterialId { get; set; }

        [Required]
        [MaxLength(80)]
        public string MaterialCode { get; set; } = string.Empty;

        public Guid? PropertyId { get; set; }

        [MaxLength(20)]
        public string Language { get; set; } = "en";

        [MaxLength(60)]
        public string Source { get; set; } = "CHEF_SUBMITTED";

        /// <summary>PENDING, APPROVED, REJECTED, INACTIVE.</summary>
        [Required]
        [MaxLength(30)]
        public string ApprovalStatus { get; set; } = "PENDING";

        public int Revision { get; set; } = 1;

        public Guid CreatedByUserId { get; set; }

        [MaxLength(200)]
        public string CreatedByName { get; set; } = string.Empty;

        public Guid? ApprovedByUserId { get; set; }

        [MaxLength(200)]
        public string? ApprovedByName { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        public DateTime EffectiveFromUtc { get; set; } = DateTime.UtcNow;
    }
}
