using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryProcurementScenario : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid ProcurementPlanId { get; set; }

        [Required]
        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string ScenarioType { get; set; } = "CUSTOM"; // MAX_LOCAL | MIN_COST | EARLIEST | MIN_TRANSFER | CONTRACT_PREFERRED | MIN_ORDERS | MIN_EXCESS | SUPPLIER_DELAY | MULTI_EVENT | GUEST_COUNT | PREPARED_STOCK

        public string ParametersJson { get; set; } = "{}";

        public string ResultsJson { get; set; } = "{}";

        [Precision(18, 2)]
        public decimal? EstimatedPurchaseValue { get; set; }

        [Precision(5, 2)]
        public decimal ProjectedReadinessPercent { get; set; }

        public Guid CreatedByUserId { get; set; }
    }
}
