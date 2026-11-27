using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Phase 2A Reusable Event Template (§10) — omits historical customer PII and stale prices.</summary>
    public class SilaCulinaryEventTemplate : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string TemplateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(80)]
        public string EventType { get; set; } = "Corporate Gala";

        [MaxLength(40)]
        public string DefaultServiceStyle { get; set; } = "BUFFET";

        public int DefaultDurationDays { get; set; } = 1;

        [Precision(6, 2)]
        public decimal DefaultAllowancePercent { get; set; } = 5m;

        [Precision(6, 2)]
        public decimal DefaultTargetFoodCostPercent { get; set; } = 28m;

        public string? TemplatePayloadJson { get; set; }
    }
}
