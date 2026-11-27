using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Phase 2A Event Master (§6) — multi-tenant, version-controlled culinary event record.</summary>
    public class SilaCulinaryEvent : BaseModel
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
        public string EventCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(80)]
        public string EventType { get; set; } = "Corporate Gala";

        [MaxLength(80)]
        public string? EventCategory { get; set; }

        [MaxLength(120)]
        public string? CustomerReference { get; set; }

        [MaxLength(200)]
        public string? CustomerName { get; set; }

        [MaxLength(120)]
        public string? BookingReference { get; set; }

        [MaxLength(120)]
        public string? ExternalEventReference { get; set; }

        [MaxLength(160)]
        public string? EventCoordinator { get; set; }

        [MaxLength(160)]
        public string? ResponsibleChef { get; set; }

        [MaxLength(160)]
        public string? ExecutiveChef { get; set; }

        [MaxLength(160)]
        public string? BanquetManager { get; set; }

        /// <summary>DRAFT, PLANNING_IN_PROGRESS, MENU_REQUIRED, MENU_REVIEW, RECIPE_RESOLUTION_PENDING, DIETARY_REVIEW_PENDING, COST_REVIEW_PENDING, READY_FOR_APPROVAL, PENDING_APPROVAL, RETURNED_FOR_CORRECTION, APPROVED, READY_FOR_MRP, REVISION_REQUIRED, CANCELLED.</summary>
        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "DRAFT";

        public int RevisionNumber { get; set; } = 1;

        public int? ApprovedRevisionNumber { get; set; }

        public Guid? PropertyId { get; set; }

        [MaxLength(40)]
        public string? CompanyCode { get; set; }

        [MaxLength(40)]
        public string? PlantCode { get; set; }

        [MaxLength(200)]
        public string? VenueName { get; set; }

        public Guid? OutletLocationId { get; set; }

        public Guid? KitchenLocationId { get; set; }

        [MaxLength(500)]
        public string? StorageLocationCodesJson { get; set; }

        public DateTime StartDateUtc { get; set; }

        public DateTime EndDateUtc { get; set; }

        [MaxLength(16)]
        public string StartTime { get; set; } = "18:00";

        [MaxLength(16)]
        public string EndTime { get; set; } = "23:00";

        public DateTime? PrepStartUtc { get; set; }

        public DateTime? MenuConfirmationDeadlineUtc { get; set; }

        public DateTime? GuestConfirmationDeadlineUtc { get; set; }

        public DateTime? MrpDeadlineUtc { get; set; }

        [MaxLength(80)]
        public string TimeZoneId { get; set; } = "Asia/Dubai";

        public int GuaranteedGuests { get; set; }

        public int ExpectedGuests { get; set; }

        public int? MaxCapacity { get; set; }

        [Precision(6, 2)]
        public decimal AdditionalAllowancePercent { get; set; }

        public int PlanningGuests { get; set; }

        public int BillableGuests { get; set; }

        [MaxLength(40)]
        public string BillablePolicy { get; set; } = "GUARANTEED_ONLY";

        public int AdultsCount { get; set; }

        public int ChildrenCount { get; set; }

        public int VipCount { get; set; }

        public int StaffMealsCount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Precision(18, 4)]
        public decimal NetPricePerGuest { get; set; }

        [Precision(18, 4)]
        public decimal? FixedPackagePrice { get; set; }

        [Precision(18, 4)]
        public decimal EstimatedFoodRevenue { get; set; }

        [Precision(18, 4)]
        public decimal EstimatedBeverageRevenue { get; set; }

        [Precision(18, 4)]
        public decimal OtherPlannedRevenue { get; set; }

        [Precision(6, 2)]
        public decimal TargetFoodCostPercent { get; set; } = 28m;

        [Precision(6, 2)]
        public decimal TargetBeverageCostPercent { get; set; } = 22m;

        [Precision(18, 4)]
        public decimal? CombinedMenuBudget { get; set; }

        [Precision(18, 4)]
        public decimal? ApprovedBudget { get; set; }

        [MaxLength(40)]
        public string ServiceStyle { get; set; } = "BUFFET";

        public Guid? PrimaryMenuId { get; set; }

        public Guid? SourceTemplateId { get; set; }

        public int WizardStepCompleted { get; set; } = 1;

        public string? DietaryRequirementsJson { get; set; }

        public bool DietaryReviewVerified { get; set; }

        [MaxLength(160)]
        public string? DietaryReviewerName { get; set; }

        public DateTime? DietaryVerifiedOnUtc { get; set; }

        public string? ReadinessSummaryJson { get; set; }

        public string? ApprovedBaselineJson { get; set; }

        [MaxLength(120)]
        public string? CorrelationId { get; set; }

        public Guid? SubmittedByUserId { get; set; }

        public DateTime? SubmittedOnUtc { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        [MaxLength(1000)]
        public string? ApprovalDecisionComment { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
