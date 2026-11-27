using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuPlan : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public string PlanCode { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        public DateTime BusinessDate { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string? MealPeriod { get; set; }

        public string? EventName { get; set; }

        [Precision(18, 4)]
        public decimal? ExpectedGuests { get; set; }

        [Precision(18, 4)]
        public decimal? ExpectedCovers { get; set; }

        [Precision(18, 4)]
        public decimal? MealParticipation { get; set; }

        [Required]
        public string Status { get; set; } = "DRAFT";

        public string? Notes { get; set; }

        public Guid? CopiedFromPlanId { get; set; }

        public string? MenuType { get; set; }

        public string? ServiceFormat { get; set; }

        public string? VenueName { get; set; }

        public string? RotationRule { get; set; }

        public bool IsTemplate { get; set; }

        public string? TemplateCategory { get; set; }

        [Precision(9, 4)]
        public decimal? TargetFoodCostPercent { get; set; }

        [Precision(18, 4)]
        public decimal? TargetContributionMargin { get; set; }

        public string? DietaryRequirements { get; set; }

        public int VersionNumber { get; set; } = 1;

        public Guid? ParentPublishedPlanId { get; set; }

        public string? SourceType { get; set; }

        public Guid? SourceReferenceId { get; set; }

        public Guid? SubmittedByUserId { get; set; }

        public DateTime? SubmittedAtUtc { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }

        public Guid? PublishedByUserId { get; set; }

        public DateTime? PublishedAtUtc { get; set; }

        public string? ApprovalComment { get; set; }

        public bool PreventConflicts { get; set; } = true;

        public string? CategoriesOrderJson { get; set; }

        public ICollection<MenuPlanLine> Lines { get; set; } = new List<MenuPlanLine>();
    }
}
