using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Material-to-plant relationship (SAP plant assignment). One material header; many plants.
    /// Does not duplicate ItemBuyerMaster identity.
    /// </summary>
    public class MaterialPlantAssignment : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid MaterialId { get; set; }

        [Required]
        [MaxLength(50)]
        public string PlantCode { get; set; } = string.Empty;

        [MaxLength(40)]
        public string? PlantStatus { get; set; }

        [MaxLength(40)]
        public string? PurchasingGroup { get; set; }

        [MaxLength(40)]
        public string? MrpType { get; set; }

        [MaxLength(40)]
        public string? ProcurementType { get; set; }

        [Precision(18, 4)]
        public decimal? ReorderPoint { get; set; }

        [Precision(18, 4)]
        public decimal? SafetyStock { get; set; }

        [Precision(18, 4)]
        public decimal? MinLotSize { get; set; }

        [Precision(18, 4)]
        public decimal? MaxLotSize { get; set; }

        public int? PlannedDeliveryDays { get; set; }

        /// <summary>Last GET_MATERIAL plant sync UTC.</summary>
        public DateTime? LastSyncedOn { get; set; }

        [MaxLength(40)]
        public string? SourceSystem { get; set; }
    }
}
