using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialRequestHistory : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("MaterialRequest")]
        public Guid MaterialRequestId { get; set; }

        public SilaCulinaryMaterialRequest MaterialRequest { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PreviousStatus { get; set; }

        [MaxLength(50)]
        public string NewStatus { get; set; } = string.Empty;

        public Guid ActorUserId { get; set; }

        [MaxLength(200)]
        public string? ActorName { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }

        [MaxLength(120)]
        public string? CorrelationId { get; set; }
    }
}
