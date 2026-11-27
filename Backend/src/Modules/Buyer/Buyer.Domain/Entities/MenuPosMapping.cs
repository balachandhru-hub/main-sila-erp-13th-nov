using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>POS item to Menu Item. Separate from PosItemMapping (POS to Recipe).</summary>
    public class MenuPosMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public string PosCode { get; set; } = string.Empty;

        public string? PosDescription { get; set; }

        public Guid? OutletLocationId { get; set; }

        public Guid? MenuItemId { get; set; }

        public DateTime? EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        [Required]
        public string Status { get; set; } = "UNMAPPED";

        public string? MappingSource { get; set; }

        public decimal? SuggestionConfidence { get; set; }

        public Guid? SuggestedMenuItemId { get; set; }

        public Guid? RecipeId { get; set; }

        public int? ApprovedRecipeVersion { get; set; }

        [Required]
        public string MappingType { get; set; } = "ONE_TO_ONE";

        [Precision(18, 4)]
        public decimal PortionMultiplier { get; set; } = 1m;

        public string? UomCode { get; set; }

        [Precision(18, 4)]
        public decimal UomConversionFactor { get; set; } = 1m;

        public string? ModifierCode { get; set; }

        public Guid? ModifierRecipeId { get; set; }

        [Precision(18, 4)]
        public decimal? ModifierPortionMultiplier { get; set; }

        public string? ComboParentCode { get; set; }

        public bool IsComboComponent { get; set; }

        public bool ExcludeComponentDoubleCount { get; set; } = true;

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }

        public string? MappingNotes { get; set; }
    }
}
