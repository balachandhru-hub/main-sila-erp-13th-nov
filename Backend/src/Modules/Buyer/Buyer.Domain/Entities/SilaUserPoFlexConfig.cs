using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Flexible user PO defaults header. ConfigType e.g. PO_CREATE — parameters and conditions are child rows.
    /// </summary>
    public class SilaUserPoFlexConfig : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid UserId { get; set; }

        public string? Username { get; set; }
        public string? DisplayName { get; set; }

        /// <summary>PO_CREATE (extensible).</summary>
        [Required]
        public string ConfigType { get; set; } = "PO_CREATE";

        public string? Name { get; set; }

        /// <summary>ACTIVE or INACTIVE.</summary>
        [Required]
        public string Status { get; set; } = "ACTIVE";

        public int ConfigVersion { get; set; } = 1;
        public bool IsDefault { get; set; }
        public string? Notes { get; set; }

        public List<SilaUserPoFlexParam> Parameters { get; set; } = new();
        public List<SilaUserPoFlexCondition> Conditions { get; set; } = new();
    }
}
