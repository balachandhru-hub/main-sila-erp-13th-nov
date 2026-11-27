using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMaterialFeedback : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? DraftId { get; set; }

        [MaxLength(80)]
        public string? IngredientTempRef { get; set; }

        [Required]
        [MaxLength(300)]
        public string IngredientDescription { get; set; } = string.Empty;

        public Guid? SuggestedMaterialId { get; set; }

        [MaxLength(80)]
        public string? SuggestedMaterialCode { get; set; }

        public Guid? SelectedMaterialId { get; set; }

        [MaxLength(80)]
        public string? SelectedMaterialCode { get; set; }

        /// <summary>
        /// ACCEPTED, REJECTED, ALTERNATIVE_SELECTED, INCORRECT_BRAND, INCORRECT_FORM,
        /// INCORRECT_PACK_SIZE, INCORRECT_CATEGORY, INCORRECT_UOM, SPECIFICATION_MISMATCH.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string FeedbackType { get; set; } = "ACCEPTED";

        [MaxLength(500)]
        public string? Notes { get; set; }

        public Guid ActorUserId { get; set; }

        [MaxLength(200)]
        public string ActorName { get; set; } = string.Empty;
    }
}
