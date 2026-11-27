using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("Draft")]
        public Guid DraftId { get; set; }

        public SilaCulinaryRecipeDraft Draft { get; set; } = null!;

        public int DraftRevision { get; set; } = 1;

        [Required]
        [MaxLength(80)]
        public string IngredientTempRef { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string OriginalDescription { get; set; } = string.Empty;

        [MaxLength(300)]
        public string NormalizedDescription { get; set; } = string.Empty;

        public Guid? MaterialId { get; set; }

        [MaxLength(80)]
        public string? MaterialCode { get; set; }

        [MaxLength(300)]
        public string? MaterialDescription { get; set; }

        [MaxLength(100)]
        public string? MaterialRevisionStamp { get; set; }

        [Precision(18, 4)]
        public decimal RecipeQuantity { get; set; }

        [Required]
        [MaxLength(30)]
        public string RecipeUom { get; set; } = "EA";

        [Precision(18, 4)]
        public decimal? ConvertedBaseQuantity { get; set; }

        [MaxLength(30)]
        public string? BaseUom { get; set; }

        [Precision(18, 6)]
        public decimal? ConversionFactor { get; set; }

        [MaxLength(50)]
        public string MatchMethod { get; set; } = "NONE";

        [Precision(8, 4)]
        public decimal MatchScore { get; set; }

        /// <summary>
        /// EXACT_MATCH, VERIFIED_ALIAS_MATCH, SUGGESTED_MATCH, MULTIPLE_CANDIDATES,
        /// SPECIFICATION_CONFLICT, UOM_CONFLICT, MATERIAL_INELIGIBLE, UNMATCHED,
        /// CONFIRMED, REJECTED, PENDING_MATERIAL_REQUEST.
        /// </summary>
        [Required]
        [MaxLength(40)]
        public string MappingStatus { get; set; } = "UNMATCHED";

        [MaxLength(50)]
        public string MissingClassification { get; set; } = "NONE";

        [MaxLength(50)]
        public string? RecommendedRequestType { get; set; }

        public bool SpecificationCompatible { get; set; } = true;

        public bool UomCompatible { get; set; } = true;

        [MaxLength(30)]
        public string StockStatus { get; set; } = "UNKNOWN";

        [MaxLength(30)]
        public string PriceStatus { get; set; } = "MISSING";

        public bool IsSubstitution { get; set; }

        [MaxLength(80)]
        public string? SubstitutedFromMaterialCode { get; set; }

        [Precision(18, 4)]
        public decimal? ReplacementRatio { get; set; }

        [MaxLength(500)]
        public string? OverrideReason { get; set; }

        [MaxLength(500)]
        public string? MissingReason { get; set; }

        public string? EvidenceJson { get; set; }

        public string? CandidatesJson { get; set; }

        public string? RequestDraftJson { get; set; }

        public int MappingRevision { get; set; } = 1;

        public Guid? ConfirmedByUserId { get; set; }

        [MaxLength(200)]
        public string? ConfirmedByName { get; set; }

        public DateTime? ConfirmedOnUtc { get; set; }

        public DateTime? LastMatchedOnUtc { get; set; }
    }
}
