using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Integration worklist: unmapped plants/storage locations or sync issues (no auto-create of locations).</summary>
    public class MaterialIntegrationException : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? MaterialId { get; set; }

        [MaxLength(40)]
        public string? MaterialCode { get; set; }

        /// <summary>UNMAPPED_STORAGE_LOCATION | UNMAPPED_PLANT | SYNC_WARNING | VALUATION_SKIPPED.</summary>
        [Required]
        [MaxLength(60)]
        public string ExceptionType { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? PlantCode { get; set; }

        [MaxLength(50)]
        public string? StorageLocationCode { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public bool Resolved { get; set; }

        public DateTime? ResolvedOn { get; set; }
    }
}
