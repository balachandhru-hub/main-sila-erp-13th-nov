using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// End-to-end Phase 2C traceability link connecting Event/MRP/Procurement line to Incoming PO,
    /// Incoming/Proposed ITO, Created PO Draft, GRN Receipt, Reversal, or Weekly Bucket demand.
    /// </summary>
    public class SilaCulinaryProcurementAllocation : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid ProcurementPlanId { get; set; }

        public Guid? ProcurementLineId { get; set; }

        public Guid EventId { get; set; }

        public int EventRevision { get; set; } = 1;

        public Guid MrpRunId { get; set; }

        public int MrpVersion { get; set; } = 1;

        public Guid? MaterialId { get; set; }

        [MaxLength(60)]
        public string? MaterialCode { get; set; }

        [MaxLength(200)]
        public string MaterialDescription { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string AllocationType { get; set; } = "INCOMING_PO"; // INCOMING_PO | INCOMING_ITO | PROPOSED_ITO | PO_DRAFT | WEEKLY_BUCKET | SUBSTITUTION

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "ALLOCATED"; // PROPOSED | ALLOCATED | DRAFT_CREATED | PENDING_APPROVAL | APPROVED | PARTIALLY_RECEIVED | RECEIVED | REVERSED | RELEASED | CANCELLED | EXCEPTION

        public Guid? SourceLocationId { get; set; }

        [MaxLength(120)]
        public string? SourceLocationName { get; set; }

        public Guid? DestinationLocationId { get; set; }

        [MaxLength(120)]
        public string? DestinationLocationName { get; set; }

        public Guid? PurchaseOrderId { get; set; }

        public Guid? PurchaseOrderItemId { get; set; }

        [MaxLength(60)]
        public string? PoNumber { get; set; }

        public int? PoItemNumber { get; set; }

        public Guid? InternalTransferOrderId { get; set; }

        [MaxLength(60)]
        public string? ItoNumber { get; set; }

        public Guid? GoodsReceiptId { get; set; }

        [MaxLength(60)]
        public string? GrnNumber { get; set; }

        [MaxLength(80)]
        public string? SapMaterialDocument { get; set; }

        [MaxLength(80)]
        public string? ReversalMaterialDocument { get; set; }

        public Guid? SupplierId { get; set; }

        [MaxLength(200)]
        public string? SupplierName { get; set; }

        [MaxLength(80)]
        public string? ContractReference { get; set; }

        [MaxLength(40)]
        public string? AccountAssignmentCategory { get; set; }

        [MaxLength(60)]
        public string? CostCenter { get; set; }

        [MaxLength(60)]
        public string? GlAccount { get; set; }

        [MaxLength(60)]
        public string? InternalOrder { get; set; }

        [MaxLength(60)]
        public string? WbsElement { get; set; }

        [MaxLength(60)]
        public string? ProfitCenter { get; set; }

        [Precision(18, 4)]
        public decimal AllocatedQty { get; set; }

        [Precision(18, 4)]
        public decimal ReceivedQty { get; set; }

        [Precision(18, 4)]
        public decimal ReversedQty { get; set; }

        [Precision(18, 4)]
        public decimal RemainingQty { get; set; }

        [Required]
        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Precision(18, 4)]
        public decimal? UnitPrice { get; set; }

        [Precision(18, 2)]
        public decimal? EstimatedValue { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        public DateTime? ExpectedDateUtc { get; set; }

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [MaxLength(300)]
        public string? Notes { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
