using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Material + plant + SAP storage location assignment. Maps to SILA InventoryLocation when StorageLocationCode matches.
    /// Unmapped SAP codes are recorded in MaterialIntegrationException — locations are not auto-created.
    /// </summary>
    public class MaterialStorageLocationAssignment : BaseModel
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

        /// <summary>Matched SILA inventory location when mapping exists; null when unmapped.</summary>
        public Guid? InventoryLocationId { get; set; }

        [MaxLength(40)]
        public string? AssignmentStatus { get; set; }

        public DateTime? LastSyncedOn { get; set; }

        [MaxLength(40)]
        public string? SourceSystem { get; set; }
    }
}
