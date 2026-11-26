using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryRecipeDraft : BaseModel
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
        public string DraftReference { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string RecipeName { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string GenerationMode { get; set; } = "DISH_NAME";

        /// <summary>DRAFT, GENERATING, GENERATED, VALIDATION_REQUIRED, CHEF_REVIEW, READY_FOR_MATERIAL_MAPPING, CANCELLED, FAILED.</summary>
        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "GENERATED";

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        public Guid? LocationId { get; set; }

        [MaxLength(160)]
        public string? KitchenName { get; set; }

        public Guid? OutletId { get; set; }

        [MaxLength(160)]
        public string? OutletName { get; set; }

        public Guid? SourceRecipeId { get; set; }

        [MaxLength(260)]
        public string? SourceDocumentName { get; set; }

        [Precision(18, 4)]
        public decimal BaseServings { get; set; } = 10m;

        public int IngredientCount { get; set; }

        [MaxLength(30)]
        public string Provider { get; set; } = "LOCAL";

        [MaxLength(200)]
        public string Model { get; set; } = string.Empty;

        [MaxLength(80)]
        public string PromptVersion { get; set; } = "culinary-recipe-v1.0";

        [MaxLength(80)]
        public string GenerationReference { get; set; } = string.Empty;

        [MaxLength(40)]
        public string ValidationStatus { get; set; } = "VALID";

        public int RevisionNumber { get; set; } = 1;

        [Required]
        public string PayloadJson { get; set; } = "{}";

        public string? ValidationWarningsJson { get; set; }

        public string? ConversationJson { get; set; }

        public long TotalInputTokens { get; set; }

        public long TotalOutputTokens { get; set; }

        [Precision(18, 6)]
        public decimal EstimatedCostUsd { get; set; }

        public Guid CreatedByUserId { get; set; }

        [MaxLength(200)]
        public string CreatedByName { get; set; } = string.Empty;
    }
}
