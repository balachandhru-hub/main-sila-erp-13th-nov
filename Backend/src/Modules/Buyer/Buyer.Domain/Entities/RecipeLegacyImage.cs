using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Uploaded image staged for assignment to a posted legacy recipe (match by recipe code stem).</summary>
    public class RecipeLegacyImage : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BatchId { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        public Guid? RecipeId { get; set; }

        [MaxLength(30)]
        public string? RecipeCode { get; set; }

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(260)]
        public string StoredName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "UNMATCHED";

        public string? Error { get; set; }

        public bool IsPrimary { get; set; }

        public bool ReplaceConfirmed { get; set; }
    }
}
