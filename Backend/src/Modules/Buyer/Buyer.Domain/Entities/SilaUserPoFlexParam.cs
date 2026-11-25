using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Freeform parameter name + value for a flex PO defaults config.</summary>
    public class SilaUserPoFlexParam : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("SilaUserPoFlexConfig")]
        public Guid ConfigId { get; set; }

        public SilaUserPoFlexConfig SilaUserPoFlexConfig { get; set; } = null!;

        [Required]
        public string ParamKey { get; set; } = string.Empty;

        public string? ParamValue { get; set; }

        public int SortOrder { get; set; }
    }
}
