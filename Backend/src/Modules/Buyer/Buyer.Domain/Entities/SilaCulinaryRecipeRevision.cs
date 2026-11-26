using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryRecipeRevision : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("SilaCulinaryRecipeDraft")]
        public Guid DraftId { get; set; }

        public SilaCulinaryRecipeDraft Draft { get; set; } = null!;

        [Required]
        public Guid BuyerId { get; set; }

        public int RevisionNumber { get; set; }

        [Required]
        [MaxLength(60)]
        public string ChangeType { get; set; } = "GENERATED";

        [MaxLength(1000)]
        public string ChangeSummary { get; set; } = string.Empty;

        [Required]
        public string PayloadJson { get; set; } = "{}";

        [MaxLength(30)]
        public string? Provider { get; set; }

        [MaxLength(200)]
        public string? Model { get; set; }

        public Guid ActorUserId { get; set; }

        [MaxLength(200)]
        public string ActorName { get; set; } = string.Empty;
    }
}
