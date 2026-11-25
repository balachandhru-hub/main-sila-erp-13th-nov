using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class InternalTransferOrderItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("InternalTransferOrder")]
        public Guid InternalTransferOrderId { get; set; }

        public InternalTransferOrder InternalTransferOrder { get; set; } = null!;

        public Guid MaterialId { get; set; }

        [Required]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        public string MaterialName { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal RequestedQty { get; set; }

        [Precision(18, 4)]
        public decimal ApprovedQty { get; set; }

        [Precision(18, 4)]
        public decimal DispatchedQty { get; set; }

        [Precision(18, 4)]
        public decimal ReceivedQty { get; set; }

        /// <summary>Quantity already reversed via movement 312 (base UOM).</summary>
        [Precision(18, 4)]
        public decimal ReversedQty { get; set; }

        /// <summary>Quantity reserved by an in-flight 312 reversal request (cleared on success/failure).</summary>
        [Precision(18, 4)]
        public decimal PendingReverseQty { get; set; }

        [Required]
        public string Uom { get; set; } = string.Empty;

        /// <summary>Optional demand link: weekly bucket line this ITO allocates.</summary>
        public Guid? WeeklyBucketId { get; set; }

        public Guid? WeeklyBucketItemId { get; set; }

        /// <summary>Optional demand link: personal/outlet wishlist line this ITO allocates.</summary>
        public Guid? WishlistItemId { get; set; }

        /// <summary>Requested quantity on the source requirement (base UOM), when demand-linked.</summary>
        [Precision(18, 4)]
        public decimal? RequirementRequestedQty { get; set; }
    }
}
