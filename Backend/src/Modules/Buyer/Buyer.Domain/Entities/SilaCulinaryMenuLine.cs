using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Phase 2A Menu Course Line (§14, §15, §25-§29) — pins exact Recipe ID & version or Phase 1A draft placeholder.</summary>
    public class SilaCulinaryMenuLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid MenuId { get; set; }

        [Required]
        [MaxLength(80)]
        public string CourseName { get; set; } = "Main Courses";

        public int SortOrder { get; set; }

        /// <summary>MANDATORY, OPTIONAL, ALTERNATIVE.</summary>
        [Required]
        [MaxLength(30)]
        public string ItemRole { get; set; } = "MANDATORY";

        public bool IsLocked { get; set; }

        public Guid? RecipeId { get; set; }

        [MaxLength(60)]
        public string? RecipeCode { get; set; }

        public int? RecipeVersion { get; set; }

        public Guid? RecipeDraftId { get; set; }

        [Required]
        [MaxLength(200)]
        public string DishName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CustomerDescription { get; set; }

        [MaxLength(500)]
        public string? DishImageUrl { get; set; }

        /// <summary>PUBLISHED, DRAFT_PLACEHOLDER, RECIPE_REQUIRED.</summary>
        [Required]
        [MaxLength(30)]
        public string LineStatus { get; set; } = "PUBLISHED";

        [Required]
        [MaxLength(40)]
        public string ServiceStyle { get; set; } = "BUFFET";

        [Precision(6, 2)]
        public decimal UptakePercent { get; set; } = 100m;

        [Precision(6, 2)]
        public decimal ServingFrequency { get; set; } = 1m;

        [Precision(12, 3)]
        public decimal PortionSize { get; set; } = 180m;

        [MaxLength(20)]
        public string PortionUom { get; set; } = "G";

        public int? AllocatedPortions { get; set; }

        public int? ManualPortionsOverride { get; set; }

        public int PlannedPortions { get; set; }

        public Guid? CostingSnapshotId { get; set; }

        [Precision(18, 4)]
        public decimal? UnitCostPerPortion { get; set; }

        [Precision(18, 4)]
        public decimal? EstimatedLineCost { get; set; }

        public string? DietaryTagsJson { get; set; }

        public string? VerifiedAllergensJson { get; set; }

        public bool AllergenUnknown { get; set; }

        public bool AllergenVerified { get; set; }

        public bool IsBeverage { get; set; }

        [Precision(6, 2)]
        public decimal ServingsPerGuest { get; set; } = 1m;

        [Precision(12, 3)]
        public decimal? ServingVolumeMl { get; set; }

        [Precision(12, 3)]
        public decimal? ContainerSizeMl { get; set; }

        [MaxLength(120)]
        public string? LiveStationName { get; set; }

        public int? VerifiedStationCapacityPerHour { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
