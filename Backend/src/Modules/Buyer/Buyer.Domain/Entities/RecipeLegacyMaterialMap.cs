using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Material mapping line for a staged legacy recipe ingredient.</summary>
    public class RecipeLegacyMaterialMap : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BatchId { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string LegacyRecipeId { get; set; } = string.Empty;

        public int IngredientLine { get; set; }

        [MaxLength(100)]
        public string? LegacyMaterialNumber { get; set; }

        [MaxLength(100)]
        public string? MaterialNumber { get; set; }

        [MaxLength(260)]
        public string? MaterialDescription { get; set; }

        public Guid? MappedMaterialId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "UNMAPPED";
    }
}
