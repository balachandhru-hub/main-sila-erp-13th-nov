using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryFinancialException : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        [ForeignKey("EventFinancialPlan")]
        public Guid EventFinancialPlanId { get; set; }

        public SilaCulinaryEventFinancialPlan EventFinancialPlan { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string ExceptionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "WARNING"; // INFO | WARNING | HIGH | CRITICAL

        [MaxLength(60)]
        public string? MaterialOrRecipeRef { get; set; }

        [Precision(18, 2)]
        public decimal? FinancialImpact { get; set; }

        [Required]
        [MaxLength(400)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? RecommendedAction { get; set; }

        [MaxLength(120)]
        public string? ResponsibleUser { get; set; }

        [MaxLength(30)]
        public string ResolutionStatus { get; set; } = "OPEN"; // OPEN | ACKNOWLEDGED | RESOLVED | IGNORED

        [MaxLength(300)]
        public string? ResolutionNote { get; set; }

        public Guid? ResolvedByUserId { get; set; }

        public DateTime? ResolvedOnUtc { get; set; }
    }
}
