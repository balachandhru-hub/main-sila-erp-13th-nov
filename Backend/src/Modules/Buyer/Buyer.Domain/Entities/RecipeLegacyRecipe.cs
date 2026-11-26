using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>One staged legacy recipe row within a migration batch.</summary>
    public class RecipeLegacyRecipe : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("RecipeLegacyBatch")]
        public Guid BatchId { get; set; }

        public RecipeLegacyBatch RecipeLegacyBatch { get; set; } = null!;

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string LegacyRecipeId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string RecipeName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Subcategory { get; set; }

        [MaxLength(30)]
        public string? RecipeFormat { get; set; }

        [MaxLength(20)]
        public string? ProductClass { get; set; }

        [Precision(18, 4)]
        public decimal? YieldQty { get; set; }

        [MaxLength(20)]
        public string? YieldUom { get; set; }

        [Precision(18, 4)]
        public decimal? PortionQty { get; set; }

        [MaxLength(20)]
        public string? PortionUom { get; set; }

        public int? PrepMinutes { get; set; }

        public int? CookMinutes { get; set; }

        public string? ServingInstructions { get; set; }

        [MaxLength(200)]
        public string? OutletScope { get; set; }

        [MaxLength(30)]
        public string? ActiveStatus { get; set; }

        [MaxLength(100)]
        public string? LegacyReference { get; set; }

        public string? Notes { get; set; }

        public string? IngredientsJson { get; set; }

        public string? InstructionsJson { get; set; }

        public string? SubRecipesJson { get; set; }

        public string? VariantsJson { get; set; }

        /// <summary>Fields of the recipe workbook format that have no column of their own (family, category, POS code, outlet prices).</summary>
        public string? ExtrasJson { get; set; }

        [Required]
        [MaxLength(20)]
        public string ValidationStatus { get; set; } = string.Empty;

        public int ErrorCount { get; set; }

        public int WarningCount { get; set; }

        public string? FindingsJson { get; set; }

        public string? MaterialMappingsJson { get; set; }

        public Guid? CreatedRecipeId { get; set; }

        [MaxLength(30)]
        public string? CreatedRecipeCode { get; set; }

        public int? CreatedVersion { get; set; }

        [Required]
        [MaxLength(20)]
        public string PostingStatus { get; set; } = "PENDING";

        public string? PostingError { get; set; }

        [MaxLength(20)]
        public string? ImageStatus { get; set; }

        [MaxLength(20)]
        public string? PublishStatus { get; set; }

        public int SourceRow { get; set; }

        public bool IsSelected { get; set; } = true;
    }
}
