using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMrpScenario : BaseModel
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
        [MaxLength(60)]
        public string ScenarioCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string ScenarioType { get; set; } = "CUSTOM";

        public bool IsBaseline { get; set; }

        public int PlannedGuests { get; set; }

        [Precision(18, 2)]
        public decimal? TotalRequirementValue { get; set; }

        [Precision(18, 2)]
        public decimal? TotalShortageValue { get; set; }

        public int ShortageCount { get; set; }

        public int ExceptionCount { get; set; }

        public string? ParametersJson { get; set; }

        public string? ResultJson { get; set; }

        [MaxLength(160)]
        public string? CreatedByName { get; set; }
    }
}
