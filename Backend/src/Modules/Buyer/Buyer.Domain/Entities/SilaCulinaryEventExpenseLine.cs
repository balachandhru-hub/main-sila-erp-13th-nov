using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryEventExpenseLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        [ForeignKey("EventFinancialPlan")]
        public Guid EventFinancialPlanId { get; set; }

        public SilaCulinaryEventFinancialPlan EventFinancialPlan { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string ExpenseCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string Category { get; set; } = "OTHER"; // FOOD | BEVERAGE | CONSUMABLES | PACKAGING | MANPOWER | EQUIPMENT | DECORATION | ENTERTAINMENT | EXTERNAL_SERVICE | UTILITIES | OTHER | OVERHEAD

        [Required]
        [MaxLength(240)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(30)]
        public string CostBehavior { get; set; } = "FIXED"; // FIXED | VARIABLE_PER_GUEST | VARIABLE_PER_SERVING | VARIABLE_PER_HOUR | VARIABLE_PER_BATCH | MIXED | STEP_FIXED

        public int? StepThresholdGuests { get; set; }

        [Precision(18, 2)]
        public decimal? StepIncrementCost { get; set; }

        [MaxLength(120)]
        public string? SupplierOrEmployeeRef { get; set; }

        [MaxLength(80)]
        public string? EmployeeRole { get; set; }

        public int? StaffCount { get; set; }

        [Precision(18, 2)]
        public decimal? PlannedHours { get; set; }

        [Precision(18, 2)]
        public decimal? ActualHours { get; set; }

        [Precision(18, 2)]
        public decimal? HourlyRate { get; set; }

        [Precision(18, 2)]
        public decimal? OvertimeHours { get; set; }

        [Precision(18, 2)]
        public decimal? OvertimeRate { get; set; }

        [Precision(18, 2)]
        public decimal? FixedShiftCost { get; set; }

        [Precision(18, 2)]
        public decimal? ExternalAgencyCharge { get; set; }

        [Precision(18, 4)]
        public decimal Quantity { get; set; } = 1m;

        [MaxLength(20)]
        public string Uom { get; set; } = "EA";

        [Precision(18, 4)]
        public decimal UnitRate { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Precision(18, 6)]
        public decimal ExchangeRate { get; set; } = 1m;

        [Precision(18, 2)]
        public decimal BudgetAmount { get; set; }

        [Precision(18, 2)]
        public decimal EstimatedAmount { get; set; }

        [Precision(18, 2)]
        public decimal CommittedAmount { get; set; }

        [Precision(18, 2)]
        public decimal? ActualVerifiedAmount { get; set; }

        [MaxLength(40)]
        public string TaxTreatment { get; set; } = "RECOVERABLE_VAT_EXCLUDED";

        [MaxLength(60)]
        public string? CostCenter { get; set; }

        [MaxLength(60)]
        public string? GlAccount { get; set; }

        [MaxLength(60)]
        public string? InternalOrder { get; set; }

        [MaxLength(60)]
        public string? WbsElement { get; set; }

        [MaxLength(200)]
        public string? SupportingDocumentRef { get; set; }

        public DateTime ExpenseDateUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        [MaxLength(120)]
        public string? SourceTransaction { get; set; }

        [MaxLength(120)]
        public string? SourceCostPoolId { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "ESTIMATED"; // ESTIMATED | PENDING_APPROVAL | APPROVED | REJECTED | ACTUAL_VERIFIED
    }
}
