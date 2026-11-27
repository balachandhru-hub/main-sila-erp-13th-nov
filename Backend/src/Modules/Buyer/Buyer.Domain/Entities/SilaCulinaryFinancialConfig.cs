using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryFinancialConfig : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [MaxLength(10)]
        public string ReportingCurrency { get; set; } = "AED";

        [MaxLength(10)]
        public string PropertyCurrency { get; set; } = "AED";

        public int FinancialPrecision { get; set; } = 2;

        public int QuantityPrecision { get; set; } = 4;

        [MaxLength(30)]
        public string MonetaryRoundingPolicy { get; set; } = "ROUND_HALF_UP";

        [MaxLength(300)]
        public string CostSourcePriorityJson { get; set; } =
            "[\"SUPPLIER_CONTRACT\",\"SUPPLIER_CATALOG\",\"INVENTORY_VALUATION\",\"MOVING_AVERAGE\",\"LATEST_PURCHASE\",\"WEIGHTED_HISTORICAL\",\"STANDARD_COST\",\"MANUAL_ESTIMATE\"]";

        [MaxLength(40)]
        public string CostValuationPolicy { get; set; } = "MOVING_AVERAGE_PREFERRED";

        [Precision(5, 2)]
        public decimal TargetFoodCostPercent { get; set; } = 28m;

        [Precision(5, 2)]
        public decimal TargetBeverageCostPercent { get; set; } = 22m;

        [Precision(5, 2)]
        public decimal TargetEventContributionMarginPercent { get; set; } = 40m;

        [Precision(5, 2)]
        public decimal MaterialCostAlertThresholdPercent { get; set; } = 5m;

        [Precision(18, 2)]
        public decimal BudgetApprovalThresholdAmount { get; set; } = 10000m;

        public bool AllowManualCostOverrides { get; set; } = true;

        public bool ExcludeVatFromRevenue { get; set; } = true;

        [Precision(5, 2)]
        public decimal DefaultVatPercent { get; set; } = 5m;

        [MaxLength(40)]
        public string ExchangeRateSource { get; set; } = "TENANT_FX_TABLE";

        public string ExchangeRatesJson { get; set; } = "[]";

        public string OverheadRulesJson { get; set; } = "[]";

        public int FiscalYearStartMonth { get; set; } = 1;

        [MaxLength(30)]
        public string CostSnapshotPolicy { get; set; } = "IMMUTABLE_VERSIONED";
    }
}
