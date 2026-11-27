using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuPlanLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("MenuPlan")]
        public Guid MenuPlanId { get; set; }

        public MenuPlan MenuPlan { get; set; } = null!;

        [Required]
        public Guid MenuItemId { get; set; }

        [Precision(18, 4)]
        public decimal? ParticipationRate { get; set; }

        [Precision(18, 4)]
        public decimal? BufferPercentage { get; set; }

        [Precision(18, 4)]
        public decimal? AdultFactor { get; set; }

        [Precision(18, 4)]
        public decimal? ChildFactor { get; set; }

        [Precision(18, 4)]
        public decimal RecommendedPortions { get; set; }

        [Precision(18, 4)]
        public decimal? PlannedPortions { get; set; }

        public bool PortionsConfirmed { get; set; }

        public string? OverrideReason { get; set; }

        [Precision(18, 4)]
        public decimal? ActualPortions { get; set; }

        public string? Warning { get; set; }

        public string? CategoryName { get; set; }

        public int SequenceOrder { get; set; }

        public Guid? RecipeId { get; set; }

        public int? RecipeVersion { get; set; }

        [Precision(18, 4)]
        public decimal? PortionSize { get; set; }

        public string? PortionUom { get; set; }

        [Precision(18, 4)]
        public decimal? ExpectedServingsPerGuest { get; set; }

        [Precision(18, 4)]
        public decimal? SellingPrice { get; set; }

        [Precision(18, 4)]
        public decimal? ExpectedCovers { get; set; }

        [Precision(18, 4)]
        public decimal? SampledActualPortion { get; set; }

        public string? LineNotes { get; set; }

        public string? AlternativesJson { get; set; }

        public string? DietaryTags { get; set; }
    }
}
