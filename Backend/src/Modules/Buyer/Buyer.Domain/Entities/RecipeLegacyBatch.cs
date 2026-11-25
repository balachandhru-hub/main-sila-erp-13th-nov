using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Staging batch for Recipe Studio legacy recipe Excel migration (additive; does not alter live Recipe rows until Post).
    /// </summary>
    public class RecipeLegacyBatch : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(30)]
        public string BatchNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(260)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(260)]
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = string.Empty;

        public Guid UploadedBy { get; set; }

        public int RecipeCount { get; set; }

        public int ValidCount { get; set; }

        public int WarningCount { get; set; }

        public int ErrorCount { get; set; }

        public int UnmappedCount { get; set; }

        public int DuplicateCount { get; set; }

        public int Revision { get; set; }

        public string? ApprovalComment { get; set; }

        public Guid? ApprovedBy { get; set; }

        public DateTime? ApprovedOn { get; set; }

        public Guid? RejectedBy { get; set; }

        public DateTime? RejectedOn { get; set; }

        public int PostedCount { get; set; }

        public int FailedPostCount { get; set; }

        public int ImageAssignedCount { get; set; }

        public int PublishedCount { get; set; }

        public string? ValidationReportJson { get; set; }

        public string? ApprovalSnapshotJson { get; set; }

        public string? PostingReportJson { get; set; }

        public string? ImageReportJson { get; set; }

        public string? PublishReportJson { get; set; }
    }
}
