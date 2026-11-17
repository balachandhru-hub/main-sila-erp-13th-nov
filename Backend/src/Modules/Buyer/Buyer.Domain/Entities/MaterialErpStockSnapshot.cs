using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Last confirmed SAP stock snapshot at material/plant/storage-location (and optional stock category).
    /// Does NOT overwrite InventoryBalance / InventoryLedger. Used for reconciliation comparison only.
    /// </summary>
    public class MaterialErpStockSnapshot : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid MaterialId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PlantCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string StorageLocationCode { get; set; } = string.Empty;

        /// <summary>UNRESTRICTED | QUALITY | BLOCKED | IN_TRANSFER | IN_TRANSIT | OTHER.</summary>
        [Required]
        [MaxLength(40)]
        public string StockCategory { get; set; } = "UNRESTRICTED";

        [MaxLength(50)]
        public string? Batch { get; set; }

        [MaxLength(40)]
        public string? SpecialStock { get; set; }

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        [Required]
        [MaxLength(20)]
        public string BaseUom { get; set; } = string.Empty;

        public DateTime SnapshotOn { get; set; }

        [MaxLength(40)]
        public string? SourceSystem { get; set; }

        public Guid? InventoryLocationId { get; set; }
    }
}
