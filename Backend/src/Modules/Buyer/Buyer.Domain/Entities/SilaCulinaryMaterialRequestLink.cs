using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Links one consolidated Material Request to one or more Recipe Draft ingredients (§11).</summary>
    public class SilaCulinaryMaterialRequestLink : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("MaterialRequest")]
        public Guid MaterialRequestId { get; set; }

        public SilaCulinaryMaterialRequest MaterialRequest { get; set; } = null!;

        [Required]
        public Guid DraftId { get; set; }

        [Required]
        [MaxLength(80)]
        public string IngredientTempRef { get; set; } = string.Empty;

        [MaxLength(300)]
        public string IngredientDescription { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal RequiredQuantity { get; set; }

        [MaxLength(30)]
        public string RecipeUom { get; set; } = "KG";

        public bool Reconnected { get; set; }

        public bool ChefConfirmed { get; set; }

        public DateTime? ReconnectedOnUtc { get; set; }
    }
}
