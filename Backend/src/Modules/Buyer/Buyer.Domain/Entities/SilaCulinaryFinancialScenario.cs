using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryFinancialScenario : BaseModel
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
        [MaxLength(140)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string ScenarioType { get; set; } = "CUSTOM"; // ORIGINAL_BUDGET | BEST_CASE | BASE_CASE | WORST_CASE | TARGET_MARGIN | CUSTOM

        public int SimulatedGuests { get; set; }

        [Precision(18, 2)]
        public decimal SimulatedRevenue { get; set; }

        [Precision(18, 2)]
        public decimal SimulatedTotalCost { get; set; }

        [Precision(18, 2)]
        public decimal SimulatedContribution { get; set; }

        [Precision(7, 2)]
        public decimal? SimulatedMarginPercent { get; set; }

        public int? SimulatedBreakEvenGuests { get; set; }

        public string ParametersJson { get; set; } = "{}";

        public string ResultsJson { get; set; } = "{}";

        public Guid CreatedByUserId { get; set; }

        public bool PromotedToRevision { get; set; }
    }
}
