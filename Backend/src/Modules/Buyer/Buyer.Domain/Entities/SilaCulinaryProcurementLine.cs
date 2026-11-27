using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryProcurementLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("ProcurementPlan")]
        public Guid ProcurementPlanId { get; set; }

        public SilaCulinaryProcurementPlan ProcurementPlan { get; set; } = null!;

        public Guid? EventId { get; set; }

        [MaxLength(60)]
        public string? EventCode { get; set; }

        [MaxLength(200)]
        public string? EventName { get; set; }

        public Guid? MrpLineId { get; set; }

        public Guid? MaterialId { get; set; }

        [MaxLength(60)]
        public string? MaterialCode { get; set; }

        [Required]
        [MaxLength(200)]
        public string MaterialDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MaterialCategory { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? MaterialImageUrl { get; set; }

        [Required]
        [MaxLength(20)]
        public string BaseUom { get; set; } = "KG";

        [MaxLength(20)]
        public string? PurchasingUom { get; set; }

        public Guid? DestinationLocationId { get; set; }

        [MaxLength(120)]
        public string? DestinationKitchenName { get; set; }

        [MaxLength(40)]
        public string? DestinationStorageLocation { get; set; }

        public DateTime RequiredByUtc { get; set; }

        [Precision(18, 4)]
        public decimal GrossRequirementQty { get; set; }

        [Precision(18, 4)]
        public decimal? LocalOnHandQty { get; set; }

        [Precision(18, 4)]
        public decimal CompetingReservationsQty { get; set; }

        [Precision(18, 4)]
        public decimal SafetyStockQty { get; set; }

        [Precision(18, 4)]
        public decimal? EligibleLocalStockQty { get; set; }

        [Precision(18, 4)]
        public decimal ActiveEventReservationQty { get; set; }

        [Precision(18, 4)]
        public decimal ProposedReservationQty { get; set; }

        [Precision(18, 4)]
        public decimal PreparedStockCoverageQty { get; set; }

        [Precision(18, 4)]
        public decimal IncomingPoAllocatedQty { get; set; }

        [Precision(18, 4)]
        public decimal IncomingTransferAllocatedQty { get; set; }

        [Precision(18, 4)]
        public decimal ProposedItoQty { get; set; }

        [Precision(18, 4)]
        public decimal ConfirmedCoverageQty { get; set; }

        [Precision(18, 4)]
        public decimal RemainingConfirmedGapQty { get; set; }

        [Precision(18, 4)]
        public decimal NetUncoveredQty { get; set; }

        [Precision(18, 4)]
        public decimal RecommendedPurchaseQty { get; set; }

        [Precision(18, 4)]
        public decimal ExpectedExcessQty { get; set; }

        [Precision(18, 4)]
        public decimal? PackSizeQty { get; set; }

        [Precision(18, 4)]
        public decimal? MoqQty { get; set; }

        public int? NumberOfPacks { get; set; }

        public Guid? RecommendedSupplierId { get; set; }

        [MaxLength(60)]
        public string? RecommendedSupplierCode { get; set; }

        [MaxLength(200)]
        public string? RecommendedSupplierName { get; set; }

        [MaxLength(80)]
        public string? ContractReference { get; set; }

        [Precision(18, 4)]
        public decimal? SupplierUnitPrice { get; set; }

        [Precision(18, 2)]
        public decimal? EstimatedPurchaseValue { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        public DateTime? ExpectedDeliveryDateUtc { get; set; }

        public DateTime? LatestOrderReleaseDateUtc { get; set; }

        public bool IsDeliveryFeasible { get; set; } = true;

        [Required]
        [MaxLength(50)]
        public string CoverageStatus { get; set; } = "REQUIREMENT_VALIDATED";

        [Required]
        [MaxLength(50)]
        public string ReadinessStatus { get; set; } = "REQUIREMENT_VALIDATED";

        public bool WeeklyBucketLinked { get; set; }

        public Guid? WeeklyBucketItemId { get; set; }

        public string? SourceDetailsJson { get; set; }

        public string? SupplierComparisonsJson { get; set; }

        public string? FefoBatchesJson { get; set; }
    }
}
