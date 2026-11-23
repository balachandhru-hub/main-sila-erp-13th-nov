using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// User-scoped purchasing and account-assignment defaults for SILA PO creation (manual and Voice PO).
    /// Multiple authorized profiles per user (tenant + company + plant + storage + purchasing context).
    /// </summary>
    public class SilaUserPurchasingProfile : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        public string? Username { get; set; }
        public string? DisplayName { get; set; }

        /// <summary>ACTIVE or INACTIVE.</summary>
        [Required]
        public string Status { get; set; } = "ACTIVE";

        public int ProfileVersion { get; set; } = 1;
        public bool IsDefault { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? OutletId { get; set; }
        public Guid? LocationId { get; set; }
        public string? CompanyCode { get; set; }
        public string? PlantCode { get; set; }
        public string? StorageLocation { get; set; }
        public string? PurchasingOrganization { get; set; }
        public string? PurchasingGroup { get; set; }
        public string? DefaultDeliveryLocation { get; set; }
        public string? AccountAssignmentCategory { get; set; }
        public string? CostCenter { get; set; }
        public string? GlAccount { get; set; }
        /// <summary>Default SAP material group applied to Voice/manual PO lines when set (e.g. 0127).</summary>
        public string? MaterialGroup { get; set; }
        public string? ProfitCenter { get; set; }
        public string? BusinessArea { get; set; }
        public string? InternalOrder { get; set; }
        public string? WbsElement { get; set; }
        public Guid? PreferredSupplierId { get; set; }
        public string? PreferredSupplierCode { get; set; }
        public string? DefaultCurrency { get; set; }
        public int? DefaultDeliveryLeadTimeDays { get; set; }
        public string? DefaultPoDocumentType { get; set; }
        public string? DefaultItemCategory { get; set; }
        public string? Notes { get; set; }
    }
}
