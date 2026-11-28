using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Commercial menu item. Optional recipe link. Not a production recipe.</summary>
    public class MenuItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public string MenuItemCode { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Category { get; set; }

        public string? SubCategory { get; set; }

        public string? Cuisine { get; set; }

        public string? Classification { get; set; }

        public string? MealPeriods { get; set; }

        [Precision(18, 4)]
        public decimal? SellingPrice { get; set; }

        public string? Currency { get; set; }

        [Precision(18, 4)]
        public decimal DefaultPortionQty { get; set; }

        public string? PortionUom { get; set; }

        [Precision(18, 4)]
        public decimal? AdultPortionFactor { get; set; }

        [Precision(18, 4)]
        public decimal? ChildPortionFactor { get; set; }

        [Precision(18, 4)]
        public decimal? DefaultParticipationRate { get; set; }

        [Precision(18, 4)]
        public decimal? DefaultBufferPercentage { get; set; }

        public string? ServiceType { get; set; }

        public string? DietaryTags { get; set; }

        public string? AllergenNotes { get; set; }

        public string? ImagePath { get; set; }

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [Required]
        public string Status { get; set; } = "DRAFT";

        public Guid? LinkedRecipeId { get; set; }

        [Required]
        public string RecipeLinkStatus { get; set; } = "MENU_ITEM_ONLY";

        public string? AlternativeName { get; set; }

        public string? ServiceFormat { get; set; }

        public int? ApprovedRecipeVersion { get; set; }

        public string? RecipeCombinationJson { get; set; }

        [Precision(9, 4)]
        public decimal? TaxRatePercent { get; set; }

        public bool TaxInclusive { get; set; }

        public string? PosItemReference { get; set; }

        public int? PrepTimeMinutes { get; set; }

        public bool IsAvailable { get; set; } = true;

        public string? SeasonalAvailability { get; set; }

        [Required]
        public string ApprovalStatus { get; set; } = "DRAFT";

        public int VersionNumber { get; set; } = 1;
    }
}
