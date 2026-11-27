using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryMrpRun : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(60)]
        public string MrpCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string RunScope { get; set; } = "SINGLE_EVENT"; // SINGLE_EVENT | SELECTED_SESSIONS | SELECTED_KITCHENS | MULTI_EVENT | DATE_RANGE | PROPERTY

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "CALCULATED"; // DRAFT | CALCULATED | PENDING_APPROVAL | APPROVED | REJECTED | RETURNED | RELEASED_FOR_PLANNING | SUPERSEDED | CANCELLED

        public int VersionNumber { get; set; } = 1;

        public bool IsApprovedBaseline { get; set; }

        public bool IsSimulation { get; set; }

        [Required]
        [MaxLength(30)]
        public string EngineVersion { get; set; } = "2B.1.0";

        [Required]
        [MaxLength(80)]
        public string CorrelationId { get; set; } = string.Empty;

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        [MaxLength(40)]
        public string? CompanyCode { get; set; }

        [MaxLength(40)]
        public string? PlantCode { get; set; }

        public DateTime? PlanningHorizonStartUtc { get; set; }

        public DateTime? PlanningHorizonEndUtc { get; set; }

        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(30)]
        public string BufferPolicy { get; set; } = "PRECEDENCE"; // PRECEDENCE | ADDITIVE_EXPLICIT

        [MaxLength(30)]
        public string PreparedStockMode { get; set; } = "ADJUSTED_SCENARIO"; // GROSS_ONLY | ADJUSTED_SCENARIO

        public string? EventReferencesJson { get; set; }

        public string? EventRevisionsJson { get; set; }

        public string? SelectedSessionsJson { get; set; }

        public string? SelectedKitchensJson { get; set; }

        public string? InputSnapshotJson { get; set; }

        public string? OutputSnapshotJson { get; set; }

        public string? ExplosionTreeJson { get; set; }

        public string? TimePhasedJson { get; set; }

        public string? BatchSummariesJson { get; set; }

        public string? ReadinessJson { get; set; }

        public int TotalEvents { get; set; }

        public int TotalRecipes { get; set; }

        public int TotalUniqueMaterials { get; set; }

        public int SufficientStockCount { get; set; }

        public int ShortageCount { get; set; }

        public int UnknownStockCount { get; set; }

        public int MissingMappingCount { get; set; }

        public int BlockingExceptionCount { get; set; }

        public int WarningExceptionCount { get; set; }

        [Precision(18, 2)]
        public decimal? EstimatedRequirementValue { get; set; }

        [Precision(18, 2)]
        public decimal? IndicativeShortageValue { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        public DateTime? InventoryTimestampUtc { get; set; }

        public DateTime? NextRequiredByUtc { get; set; }

        public Guid? ParentMrpRunId { get; set; }

        public Guid? SubmittedByUserId { get; set; }

        public DateTime? SubmittedOnUtc { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        [MaxLength(1000)]
        public string? ApprovalDecisionComment { get; set; }

        public Guid? ReleasedByUserId { get; set; }

        public DateTime? ReleasedOnUtc { get; set; }

        [MaxLength(160)]
        public string? CreatedByName { get; set; }
    }
}
