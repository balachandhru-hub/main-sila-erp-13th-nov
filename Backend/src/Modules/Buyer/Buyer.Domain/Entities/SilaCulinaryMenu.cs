using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Phase 2A Menu Studio Header (§13, §35) — standalone reusable menus, event menus, or scenario copies.</summary>
    public class SilaCulinaryMenu : BaseModel
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
        public string MenuCode { get; set; } = string.Empty;

        public Guid? EventId { get; set; }

        public Guid? SessionId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [MaxLength(80)]
        public string MenuCategory { get; set; } = "Event Banquet";

        [MaxLength(100)]
        public string? Cuisine { get; set; }

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        [Required]
        [MaxLength(40)]
        public string ServiceStyle { get; set; } = "BUFFET";

        public int VersionNumber { get; set; } = 1;

        public DateTime EffectiveDateUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? MenuImageUrl { get; set; }

        /// <summary>DRAFT, REVIEW, APPROVED, ARCHIVED.</summary>
        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "DRAFT";

        public bool IsReusableTemplate { get; set; }

        public bool IsScenario { get; set; }

        [MaxLength(120)]
        public string? ScenarioName { get; set; }

        public bool IsSelectedScenario { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Precision(18, 4)]
        public decimal? TargetSellingPrice { get; set; }

        [Precision(6, 2)]
        public decimal TargetFoodCostPercent { get; set; } = 28m;

        [Precision(18, 4)]
        public decimal TotalFoodCost { get; set; }

        [Precision(18, 4)]
        public decimal TotalBeverageCost { get; set; }

        [Precision(18, 4)]
        public decimal TotalEstimatedCost { get; set; }

        [Precision(18, 4)]
        public decimal CostPerGuest { get; set; }

        public string? DietarySummaryJson { get; set; }

        public string? AllergenMatrixJson { get; set; }

        public string? AiRecommendationNotes { get; set; }
    }
}
