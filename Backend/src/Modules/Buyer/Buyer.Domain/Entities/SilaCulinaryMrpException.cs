using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMrpException : BaseModel
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

        [Required]
        [MaxLength(60)]
        public string ExceptionCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string ExceptionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "WARNING"; // BLOCKING | WARNING | INFO

        public Guid? EventId { get; set; }

        [MaxLength(60)]
        public string? EventCode { get; set; }

        public Guid? RecipeId { get; set; }

        [MaxLength(60)]
        public string? RecipeCode { get; set; }

        public Guid? MaterialId { get; set; }

        [MaxLength(80)]
        public string? MaterialCode { get; set; }

        [MaxLength(200)]
        public string? MaterialName { get; set; }

        [Required]
        [MaxLength(1000)]
        public string CalculationImpact { get; set; } = string.Empty;

        [MaxLength(100)]
        public string ResponsibleTeam { get; set; } = "Culinary & Master Data";

        [Required]
        [MaxLength(500)]
        public string RecommendedAction { get; set; } = string.Empty;

        [MaxLength(60)]
        public string? LinkedWorkflow { get; set; }

        [Required]
        [MaxLength(30)]
        public string ResolutionStatus { get; set; } = "OPEN"; // OPEN | IN_PROGRESS | RESOLVED | WAIVED

        [MaxLength(500)]
        public string? ResolutionNote { get; set; }

        [MaxLength(160)]
        public string? ResolvedByName { get; set; }

        public DateTime? ResolvedOnUtc { get; set; }

        public string? CyclePathJson { get; set; }
    }
}
