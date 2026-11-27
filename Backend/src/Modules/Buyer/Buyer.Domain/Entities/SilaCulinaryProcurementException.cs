using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryProcurementException : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid ProcurementPlanId { get; set; }

        public Guid? ProcurementLineId { get; set; }

        public Guid? EventId { get; set; }

        [MaxLength(60)]
        public string? EventCode { get; set; }

        [MaxLength(60)]
        public string? MaterialCode { get; set; }

        [MaxLength(200)]
        public string? MaterialDescription { get; set; }

        [Required]
        [MaxLength(60)]
        public string ExceptionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "WARNING"; // BLOCKING | CRITICAL | WARNING | INFO

        [Precision(18, 4)]
        public decimal QuantityImpact { get; set; }

        public DateTime? RequiredDateUtc { get; set; }

        [MaxLength(120)]
        public string? ResponsibleRole { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(500)]
        public string RecommendedAction { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string ResolutionStatus { get; set; } = "OPEN"; // OPEN | ACKNOWLEDGED | RESOLVED | IGNORED

        [MaxLength(300)]
        public string? ResolutionNote { get; set; }

        public Guid? ResolvedByUserId { get; set; }

        public DateTime? ResolvedOnUtc { get; set; }
    }
}
