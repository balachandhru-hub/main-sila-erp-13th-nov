using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryEventRevenueLine : BaseModel
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
        public string Category { get; set; } = "FOOD_PACKAGE";

        [Required]
        [MaxLength(240)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(30)]
        public string RevenueStatus { get; set; } = "PLANNED"; // PLANNED | CONTRACTED | INVOICED | RECOGNIZED

        [MaxLength(30)]
        public string BillingBasis { get; set; } = "PER_GUEST"; // PER_GUEST | FIXED | GUARANTEED_RULE

        [Precision(18, 4)]
        public decimal Quantity { get; set; } = 1m;

        [Precision(18, 4)]
        public decimal UnitPrice { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        [Precision(18, 6)]
        public decimal ExchangeRate { get; set; } = 1m;

        [Precision(18, 2)]
        public decimal GrossAmount { get; set; }

        [Precision(18, 2)]
        public decimal DiscountAmount { get; set; }

        [Precision(18, 2)]
        public decimal TaxAmount { get; set; }

        [Precision(18, 2)]
        public decimal NetRevenue { get; set; }

        [MaxLength(120)]
        public string? SourceReference { get; set; }
    }
}
