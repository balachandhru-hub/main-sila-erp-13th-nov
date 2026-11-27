using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>Phase 2A Multi-Day and Multi-Session Event Breakdown (§11).</summary>
    public class SilaCulinaryEventSession : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid EventId { get; set; }

        [Required]
        [MaxLength(60)]
        public string SessionCode { get; set; } = string.Empty;

        public int DayNumber { get; set; } = 1;

        public DateTime SessionDateUtc { get; set; }

        [MaxLength(16)]
        public string StartTime { get; set; } = "09:00";

        [MaxLength(16)]
        public string EndTime { get; set; } = "12:00";

        [Required]
        [MaxLength(160)]
        public string SessionName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? VenueName { get; set; }

        public Guid? KitchenLocationId { get; set; }

        [Required]
        [MaxLength(40)]
        public string ServiceStyle { get; set; } = "BUFFET";

        public int GuestCount { get; set; }

        public Guid? MenuId { get; set; }

        [Precision(18, 4)]
        public decimal? BudgetAllocation { get; set; }

        public DateTime? PrepDeadlineUtc { get; set; }

        public string? DietaryNotesJson { get; set; }

        public int SortOrder { get; set; }
    }
}
