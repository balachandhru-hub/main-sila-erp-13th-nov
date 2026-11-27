using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryProcurementPlan : BaseModel
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
        public string PlanCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public Guid MrpRunId { get; set; }

        [MaxLength(60)]
        public string MrpCode { get; set; } = string.Empty;

        public int MrpVersion { get; set; } = 1;

        public Guid? PrimaryEventId { get; set; }

        [MaxLength(60)]
        public string? PrimaryEventCode { get; set; }

        [MaxLength(200)]
        public string? PrimaryEventName { get; set; }

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        [MaxLength(40)]
        public string? CompanyCode { get; set; }

        [MaxLength(40)]
        public string? PlantCode { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "CALCULATED"; // DRAFT | CALCULATED | PENDING_APPROVAL | APPROVED | REJECTED | RETURNED | ACTIVE | SUPERSEDED | CANCELLED

        public int VersionNumber { get; set; } = 1;

        public bool IsSimulation { get; set; }

        [Required]
        [MaxLength(80)]
        public string CorrelationId { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        public DateTime? EventDateUtc { get; set; }

        public DateTime? PreparationDeadlineUtc { get; set; }

        public int PlannedGuests { get; set; }

        public int GuaranteedGuests { get; set; }

        [MaxLength(120)]
        public string? ResponsibleChef { get; set; }

        [MaxLength(160)]
        public string? VenueName { get; set; }

        [MaxLength(40)]
        public string CoveragePriorityJson { get; set; } = "LOCAL,PREPARED,PO,ITO,CENTRAL,OUTLET,SUBSTITUTE,CONTRACT,CATALOG";

        public string? EventReferencesJson { get; set; }

        public string? SupplyDemandLedgerJson { get; set; }

        public string? ConsolidatedPurchasesJson { get; set; }

        public string? ReadinessSummaryJson { get; set; }

        [Column("phase3a_handover_json")]
        public string? Phase3AHandoverJson { get; set; }

        public int RequiredMaterialsCount { get; set; }

        public int FullyReservedCount { get; set; }

        public int IncomingPoCoveredCount { get; set; }

        public int TransferCoveredCount { get; set; }

        public int PurchaseRequiredCount { get; set; }

        public int CriticalShortageCount { get; set; }

        public int DelayedDeliveryCount { get; set; }

        public int PendingApprovalCount { get; set; }

        [Precision(18, 2)]
        public decimal? EstimatedRequirementValue { get; set; }

        [Precision(18, 2)]
        public decimal? EstimatedNewPurchaseValue { get; set; }

        [Precision(18, 2)]
        public decimal? CommittedProcurementValue { get; set; }

        [Precision(5, 2)]
        public decimal PhysicalReadinessPercent { get; set; }

        [Precision(5, 2)]
        public decimal ProjectedReadinessPercent { get; set; }

        public Guid? SubmittedByUserId { get; set; }

        public DateTime? SubmittedOnUtc { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<SilaCulinaryProcurementLine> Lines { get; set; } = new List<SilaCulinaryProcurementLine>();
    }
}
