using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Phase 2C stock reservation record. Reserves eligible physical stock for a culinary event/MRP without
    /// writing physical InventoryTransaction ledger movements. Protected by [Timestamp] concurrency token.
    /// </summary>
    public class SilaCulinaryStockReservation : BaseModel
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
        public string ReservationCode { get; set; } = string.Empty;

        public Guid ProcurementPlanId { get; set; }

        public Guid? ProcurementLineId { get; set; }

        public Guid EventId { get; set; }

        public int EventRevision { get; set; } = 1;

        public Guid? SessionId { get; set; }

        public Guid MrpRunId { get; set; }

        public int MrpVersion { get; set; } = 1;

        public Guid MaterialId { get; set; }

        [Required]
        [MaxLength(60)]
        public string MaterialCode { get; set; } = string.Empty;

        [MaxLength(200)]
        public string MaterialDescription { get; set; } = string.Empty;

        public Guid? PropertyId { get; set; }

        [MaxLength(40)]
        public string? PlantCode { get; set; }

        public Guid LocationId { get; set; }

        [MaxLength(40)]
        public string? StorageLocationCode { get; set; }

        [MaxLength(80)]
        public string? BatchNumber { get; set; }

        public DateTime? BatchExpiryUtc { get; set; }

        [Precision(18, 4)]
        public decimal ReservedQty { get; set; }

        [Precision(18, 4)]
        public decimal FulfilledQty { get; set; }

        [Precision(18, 4)]
        public decimal ReleasedQty { get; set; }

        [Required]
        [MaxLength(20)]
        public string BaseUom { get; set; } = "KG";

        public DateTime RequiredByUtc { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "PROPOSED"; // PROPOSED | PENDING_APPROVAL | ACTIVE | PARTIALLY_FULFILLED | FULFILLED | RELEASE_PENDING | RELEASED | EXPIRED | CANCELLED | EXCEPTION

        public bool IsPreparedStock { get; set; }

        public bool SafetyStockOverrideUsed { get; set; }

        [MaxLength(300)]
        public string? SafetyOverrideReason { get; set; }

        public int PriorityRank { get; set; } = 100;

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public Guid CreatedByUserId { get; set; }

        [MaxLength(120)]
        public string CreatedByName { get; set; } = string.Empty;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
