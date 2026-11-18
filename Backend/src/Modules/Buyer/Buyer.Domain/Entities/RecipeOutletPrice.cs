using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RecipeOutletPrice : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Recipe")]
        public Guid RecipeId { get; set; }

        public Recipe Recipe { get; set; } = null!;

        public Guid OutletLocationId { get; set; }

        [Precision(18, 4)]
        public decimal MenuPrice { get; set; }

        public string? Currency { get; set; }

        /// <summary>POS code sold at this outlet (outlet-specific; defaults from recipe header when empty on write).</summary>
        public string? PosCode { get; set; }

        /// <summary>POS item name at this outlet (same identity as menu item when both are set).</summary>
        public string? PosItem { get; set; }

        // SILA ME release 2
        public int Version { get; set; }
    }
}
