using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Conditional override: when WhenParam WhenOperator WhenValue → set ThenParam = ThenValue.
    /// Example: when AccountAssignmentCategory EQ K → set CostCenter = 1060010108.
    /// </summary>
    public class SilaUserPoFlexCondition : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("SilaUserPoFlexConfig")]
        public Guid ConfigId { get; set; }

        public SilaUserPoFlexConfig SilaUserPoFlexConfig { get; set; } = null!;

        [Required]
        public string WhenParam { get; set; } = string.Empty;

        /// <summary>EQ | NE | CONTAINS (default EQ).</summary>
        [Required]
        public string WhenOperator { get; set; } = "EQ";

        public string? WhenValue { get; set; }

        [Required]
        public string ThenParam { get; set; } = string.Empty;

        public string? ThenValue { get; set; }

        public int SortOrder { get; set; }
    }
}
