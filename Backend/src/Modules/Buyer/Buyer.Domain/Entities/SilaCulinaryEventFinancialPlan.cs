using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryEventFinancialPlan : BaseModel
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

        public int VersionNumber { get; set; } = 1;

        public Guid? EventId { get; set; }

        [MaxLength(60)]
        public string? EventCode { get; set; }

        [MaxLength(200)]
        public string? EventName { get; set; }

        public int EventRevision { get; set; } = 1;

        public Guid? MenuId { get; set; }

        public int? MenuRevision { get; set; }

        public Guid? MrpRunId { get; set; }

        public int? MrpVersion { get; set; }

        public Guid? ProcurementPlanId { get; set; }

        public int? ProcurementPlanVersion { get; set; }

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        [MaxLength(40)]
        public string? CompanyCode { get; set; }

        [MaxLength(160)]
        public string? VenueName { get; set; }

        [MaxLength(160)]
        public string? CustomerReference { get; set; }

        public DateTime? EventDateUtc { get; set; }

        public int ExpectedGuests { get; set; }

        public int GuaranteedGuests { get; set; }

        public int PlannedGuests { get; set; }

        public int? ActualGuests { get; set; }

        public int BillableGuests { get; set; }

        [MaxLength(40)]
        public string BillablePolicy { get; set; } = "GUARANTEED_PLUS_ADDITIONAL";

        [MaxLength(40)]
        public string RevenueModel { get; set; } = "PER_GUEST_PACKAGE";

        [MaxLength(10)]
        public string ReportingCurrency { get; set; } = "AED";

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "DRAFT"; // DRAFT | CALCULATED | PENDING_APPROVAL | APPROVED | REJECTED | RETURNED | SUPERSEDED | CANCELLED

        public bool IsSimulation { get; set; }

        public bool IsDemo { get; set; }

        [Required]
        [MaxLength(80)]
        public string CorrelationId { get; set; } = string.Empty;

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [Precision(5, 2)]
        public decimal TargetFoodCostPercent { get; set; } = 28m;

        [Precision(5, 2)]
        public decimal TargetBeverageCostPercent { get; set; } = 22m;

        [Precision(5, 2)]
        public decimal TargetContributionMarginPercent { get; set; } = 40m;

        [Precision(18, 2)]
        public decimal OriginalBudget { get; set; }

        [Precision(18, 2)]
        public decimal RevisedBudget { get; set; }

        [Precision(18, 2)]
        public decimal GrossRevenue { get; set; }

        [Precision(18, 2)]
        public decimal DiscountTotal { get; set; }

        [Precision(18, 2)]
        public decimal NetRevenue { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedFoodCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedBeverageCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedManpowerCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedEquipmentCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedDecorationCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedExternalServiceCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedOtherDirectCost { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedAllocatedOverhead { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedTotalCost { get; set; }

        [Precision(18, 2)]
        public decimal ApprovedProcurementCommitment { get; set; }

        [Precision(18, 2)]
        public decimal AttributableProcurementCommitment { get; set; }

        [Precision(18, 2)]
        public decimal ExpectedConsumptionValueFromPo { get; set; }

        [Precision(18, 2)]
        public decimal RemainingUnallocatedInventoryValue { get; set; }

        [Precision(18, 2)]
        public decimal ActualVerifiedCost { get; set; }

        public bool ActualIsProvisional { get; set; } = true;

        [Precision(18, 2)]
        public decimal ForecastAtCompletionCost { get; set; }

        [Precision(18, 2)]
        public decimal DirectContribution { get; set; }

        [Precision(18, 2)]
        public decimal ContributionAfterOverhead { get; set; }

        [Precision(7, 2)]
        public decimal? ContributionMarginPercent { get; set; }

        [Precision(7, 2)]
        public decimal? FoodCostPercent { get; set; }

        [Precision(7, 2)]
        public decimal? BeverageCostPercent { get; set; }

        public int? BreakEvenGuests { get; set; }

        [Precision(18, 2)]
        public decimal MaxAllowableCost { get; set; }

        [Precision(18, 2)]
        public decimal RemainingCostHeadroom { get; set; }

        [MaxLength(60)]
        public string? CostCenter { get; set; }

        [MaxLength(60)]
        public string? ProfitCenter { get; set; }

        [MaxLength(60)]
        public string? InternalOrder { get; set; }

        [MaxLength(60)]
        public string? WbsElement { get; set; }

        [MaxLength(160)]
        public string? FinancialOwner { get; set; }

        [MaxLength(60)]
        public string CalculationEngineVersion { get; set; } = "PHASE3A-1.0";

        public string? SnapshotPayloadJson { get; set; }

        [Column("phase3b_handover_json")]
        public string? Phase3BHandoverJson { get; set; }

        [Column("phase4b_handover_json")]
        public string? Phase4BHandoverJson { get; set; }

        public Guid? SubmittedByUserId { get; set; }

        public DateTime? SubmittedOnUtc { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<SilaCulinaryEventRevenueLine> RevenueLines { get; set; } = new List<SilaCulinaryEventRevenueLine>();
        public ICollection<SilaCulinaryEventExpenseLine> ExpenseLines { get; set; } = new List<SilaCulinaryEventExpenseLine>();
        public ICollection<SilaCulinaryFinancialException> Exceptions { get; set; } = new List<SilaCulinaryFinancialException>();
        public ICollection<SilaCulinaryFinancialScenario> Scenarios { get; set; } = new List<SilaCulinaryFinancialScenario>();
    }
}
