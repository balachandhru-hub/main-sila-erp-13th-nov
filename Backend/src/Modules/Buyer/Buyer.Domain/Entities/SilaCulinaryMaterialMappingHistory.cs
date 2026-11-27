using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialMappingHistory : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid MappingId { get; set; }

        [Required]
        public Guid DraftId { get; set; }

        [Required]
        [MaxLength(80)]
        public string IngredientTempRef { get; set; } = string.Empty;

        public int MappingRevision { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? PreviousStatus { get; set; }

        [Required]
        [MaxLength(40)]
        public string NewStatus { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? PreviousMaterialCode { get; set; }

        [MaxLength(80)]
        public string? NewMaterialCode { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        public Guid ActorUserId { get; set; }

        [MaxLength(200)]
        public string ActorName { get; set; } = string.Empty;

        public string? PayloadSnapshotJson { get; set; }
    }
}
