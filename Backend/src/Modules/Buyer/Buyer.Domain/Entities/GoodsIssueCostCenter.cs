using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Goods Issue (Z02) cost center mapping for Recipe / inventory consumption.
    /// PLANT scope: Property + Plant → CostCenter.
    /// STORAGE scope: Property + Plant + StorageLocation → CostCenter (wins over plant).
    /// </summary>
    public class GoodsIssueCostCenter : BaseModel
    {
        public const string SCOPE_PLANT = "PLANT";
        public const string SCOPE_STORAGE = "STORAGE";

        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("BuyerProperty")]
        public Guid PropertyId { get; set; }

        public BuyerProperty BuyerProperty { get; set; } = null!;

        /// <summary>SAP plant code (denormalized from BuyerProperty.PlantCode for display and lookup).</summary>
        [Required]
        [MaxLength(50)]
        public string PlantCode { get; set; } = string.Empty;

        /// <summary>PLANT or STORAGE.</summary>
        [Required]
        [MaxLength(20)]
        public string Scope { get; set; } = SCOPE_PLANT;

        /// <summary>SAP storage location / outlet code. Empty for PLANT scope.</summary>
        [Required]
        [MaxLength(50)]
        public string StorageLocationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CostCenter { get; set; } = string.Empty;
    }
}
