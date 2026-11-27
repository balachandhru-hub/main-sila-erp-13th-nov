using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMrpLine : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("MrpRun")]
        public Guid MrpRunId { get; set; }

        public SilaCulinaryMrpRun MrpRun { get; set; } = null!;

        [Required]
        [MaxLength(260)]
        public string ConsolidationKey { get; set; } = string.Empty;

        public Guid? MaterialId { get; set; }

        [MaxLength(80)]
        public string? MaterialCode { get; set; }

        [Required]
        [MaxLength(260)]
        public string MaterialDescription { get; set; } = string.Empty;

        [MaxLength(100)]
        public string MaterialCategory { get; set; } = "Uncategorized";

        public bool IsResolvedMaterial { get; set; }

        public Guid? PropertyId { get; set; }

        [MaxLength(40)]
        public string? PlantCode { get; set; }

        public Guid? KitchenLocationId { get; set; }

        [MaxLength(160)]
        public string KitchenName { get; set; } = "Main Banquet Kitchen";

        [MaxLength(60)]
        public string StorageLocationCode { get; set; } = "DEFAULT";

        public DateTime RequiredDateUtc { get; set; }

        [Required]
        [MaxLength(20)]
        public string RequiredDateBucket { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string BaseUom { get; set; } = "KG";

        [Precision(18, 4)]
        public decimal EdibleQty { get; set; }

        [Precision(18, 4)]
        public decimal RawQty { get; set; }

        [Precision(18, 4)]
        public decimal GrossQty { get; set; }

        [Precision(18, 4)]
        public decimal PreparedStockQty { get; set; }

        [Precision(18, 4)]
        public decimal IndicativeNewProductionQty { get; set; }

        [Precision(18, 4)]
        public decimal? EligibleStockQty { get; set; }

        [Precision(18, 4)]
        public decimal IncomingPoQty { get; set; }

        [Precision(18, 4)]
        public decimal? IndicativeCurrentShortageQty { get; set; }

        [Precision(18, 4)]
        public decimal? IndicativeRemainingShortageQty { get; set; }

        [Precision(18, 4)]
        public decimal? UnitCost { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [MaxLength(30)]
        public string CostStatus { get; set; } = "COST_UNAVAILABLE";

        [Precision(18, 2)]
        public decimal? RequirementValue { get; set; }

        [Precision(18, 2)]
        public decimal? IndicativeShortageValue { get; set; }

        [Required]
        [MaxLength(50)]
        public string ValidationStatus { get; set; } = "REQUIREMENT_CALCULATED";

        public bool IsBlocking { get; set; }

        public DateTime? InventoryTimestampUtc { get; set; }

        public string? ContributingRecipesJson { get; set; }

        public string? ContributingEventsJson { get; set; }

        public string? ContributingSessionsJson { get; set; }

        public string? SubRecipePathsJson { get; set; }

        public string? ExclusionReasonsJson { get; set; }
    }
}
