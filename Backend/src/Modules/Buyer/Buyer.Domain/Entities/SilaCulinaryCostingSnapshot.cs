using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Versioned / immutable Recipe Costing Snapshot or What-If Scenario (§34, §36).</summary>
    public class SilaCulinaryCostingSnapshot : BaseModel
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
        public string CostingReference { get; set; } = string.Empty;

        public Guid? DraftId { get; set; }

        public int? DraftRevision { get; set; }

        public Guid? ProductionRecipeId { get; set; }

        public int? RecipeVersion { get; set; }

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string ScenarioName { get; set; } = "Baseline";

        public bool IsBaseline { get; set; } = true;

        public bool IsApprovedSnapshot { get; set; }

        /// <summary>DRAFT, PENDING_REVIEW, APPROVED, SUPERSEDED.</summary>
        [Required]
        [MaxLength(40)]
        public string ApprovalStatus { get; set; } = "DRAFT";

        [MaxLength(60)]
        public string CostingBasis { get; set; } = "STANDARD_RECIPE_COST";

        [MaxLength(30)]
        public string TaxBasis { get; set; } = "TAX_EXCLUSIVE";

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Precision(18, 4)]
        public decimal Servings { get; set; } = 1m;

        [Precision(18, 4)]
        public decimal TotalIngredientCost { get; set; }

        [Precision(18, 4)]
        public decimal SubRecipeCost { get; set; }

        [Precision(18, 4)]
        public decimal AdditionalDirectCost { get; set; }

        [Precision(18, 4)]
        public decimal AllocatedOverheadCost { get; set; }

        [Precision(18, 4)]
        public decimal TotalRecipeCost { get; set; }

        [Precision(18, 4)]
        public decimal IngredientCostPerPortion { get; set; }

        [Precision(18, 4)]
        public decimal TotalCostPerPortion { get; set; }

        [Precision(18, 4)]
        public decimal? SellingPricePerPortion { get; set; }

        [Precision(8, 2)]
        public decimal? FoodCostPercent { get; set; }

        [Precision(8, 2)]
        public decimal? TargetFoodCostPercent { get; set; }

        [Precision(18, 4)]
        public decimal? RecommendedSellingPrice { get; set; }

        [Precision(18, 4)]
        public decimal? EstimatedRevenue { get; set; }

        [Precision(18, 4)]
        public decimal? ContributionAmount { get; set; }

        /// <summary>COMPLETE or PARTIAL (when any ingredient price/FX/UOM is missing).</summary>
        [Required]
        [MaxLength(30)]
        public string CostCompleteness { get; set; } = "COMPLETE";

        public int MissingPriceCount { get; set; }

        [Required]
        public string LinesJson { get; set; } = "[]";

        public string? AdditionalCostsJson { get; set; }

        public string? AssumptionsJson { get; set; }

        [MaxLength(200)]
        public string? CreatedByName { get; set; }
    }
}
