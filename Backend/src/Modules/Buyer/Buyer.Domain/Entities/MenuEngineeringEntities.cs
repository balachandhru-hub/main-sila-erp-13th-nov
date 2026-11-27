using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuItemVersion : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid MenuItemId { get; set; }

        public int VersionNumber { get; set; }

        [Required]
        public string Status { get; set; } = "DRAFT";

        [Precision(18, 4)]
        public decimal? SellingPrice { get; set; }

        [Precision(18, 4)]
        public decimal? IngredientCost { get; set; }

        public Guid? LinkedRecipeId { get; set; }

        public int? ApprovedRecipeVersion { get; set; }

        public string? ChangeSummary { get; set; }

        public string SnapshotJson { get; set; } = "{}";

        public Guid? ActionByUserId { get; set; }

        public DateTime ActionAtUtc { get; set; }
    }

    public class MenuEngineeringScenario : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? MenuPlanId { get; set; }

        public Guid? PropertyId { get; set; }

        [Required]
        public string ScenarioName { get; set; } = string.Empty;

        [Required]
        public string ScenarioType { get; set; } = "CUSTOM";

        public string VariablesJson { get; set; } = "{}";

        public string BaselineSnapshotJson { get; set; } = "{}";

        public string ResultSnapshotJson { get; set; } = "{}";

        public bool FixedDemandDisclosed { get; set; } = true;

        public string? Notes { get; set; }

        public string? SharedWithJson { get; set; }

        public bool IsPreferred { get; set; }

        public Guid? CreatedByUserId { get; set; }

        // Phase ME.3 additive scenario lineage & delta columns
        public Guid? BaselineSnapshotId { get; set; }

        public int? BaselineSnapshotVersion { get; set; }

        [MaxLength(40)]
        public string? CalculationVersion { get; set; }

        [MaxLength(40)]
        public string? ModelVersion { get; set; }

        [MaxLength(40)]
        public string? SolverStatus { get; set; }

        public int? ParetoRank { get; set; }

        public Guid? ParentScenarioId { get; set; }

        [MaxLength(30)]
        public string? Status { get; set; } = "ACTIVE";

        public string? DeltaOverridesJson { get; set; }
    }

    public class MenuEngineeringApprovalHistory : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public string EntityType { get; set; } = "MENU_PLAN";

        [Required]
        public Guid EntityId { get; set; }

        public int VersionNumber { get; set; } = 1;

        [Required]
        public string FromStatus { get; set; } = string.Empty;

        [Required]
        public string ToStatus { get; set; } = string.Empty;

        public Guid? ActionByUserId { get; set; }

        public string? ApproverRole { get; set; }

        public string? Reason { get; set; }

        public string? SnapshotJson { get; set; }

        public DateTime ActionAtUtc { get; set; }
    }

    public class MenuDemandOverride : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        public Guid? MenuItemId { get; set; }

        public DateTime BusinessDate { get; set; }

        public string? MealPeriod { get; set; }

        [Precision(18, 4)]
        public decimal BaselineForecast { get; set; }

        [Precision(18, 4)]
        public decimal AiAdjustedForecast { get; set; }

        [Precision(18, 4)]
        public decimal ConfirmedDemand { get; set; }

        [Precision(18, 4)]
        public decimal OverrideQty { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;

        public Guid? OverriddenByUserId { get; set; }

        public DateTime OverriddenAtUtc { get; set; }
    }

    public class MenuEngineeringConfigHistory : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public int ConfigVersion { get; set; }

        public Guid? ChangedByUserId { get; set; }

        public string? ChangeSummary { get; set; }

        public string PreviousConfigJson { get; set; } = "{}";

        public string NewConfigJson { get; set; } = "{}";

        public DateTime ChangedAtUtc { get; set; }
    }
}
