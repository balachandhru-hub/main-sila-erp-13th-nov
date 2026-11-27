using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuEngineeringAgentRun : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(120)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string AgentId { get; set; } = "ME_STUDIO_ORCHESTRATOR";

        [Required]
        [MaxLength(30)]
        public string OperatingMode { get; set; } = "AGENTIC";

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "PENDING"; // PENDING | RUNNING | COMPLETED | CANCELLED | FAILED

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        public Guid? MenuPlanId { get; set; }

        public Guid? CreatedDraftMenuId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string ObjectiveText { get; set; } = string.Empty;

        [MaxLength(60)]
        public string? OptimizationMode { get; set; }

        public int StepCount { get; set; }

        public int CurrentStep { get; set; }

        public string StepsJson { get; set; } = "[]";

        public string ProposalJson { get; set; } = "{}";

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public Guid ActorUserId { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public DateTime? CancelledAtUtc { get; set; }
    }

    public class MenuEngineeringRecommendation : BaseModel
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
        public string RecommendationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string AgentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string Category { get; set; } = "MENU_OPTIMIZATION";

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        [MaxLength(60)]
        public string? TargetObjectType { get; set; }

        public Guid? TargetObjectId { get; set; }

        [MaxLength(200)]
        public string? TargetObjectLabel { get; set; }

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1500)]
        public string CurrentBaseline { get; set; } = string.Empty;

        [MaxLength(1500)]
        public string ProposedChange { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string SupportingEvidence { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal? EstimatedCostImpact { get; set; }

        [Precision(18, 4)]
        public decimal? EstimatedMarginImpact { get; set; }

        [Precision(18, 4)]
        public decimal? RealizedFinancialImpact { get; set; }

        [MaxLength(500)]
        public string? OperationalImpact { get; set; }

        [MaxLength(30)]
        public string DataQuality { get; set; } = "HIGH";

        [Precision(9, 4)]
        public decimal Confidence { get; set; } = 0.75m;

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "MEDIUM"; // CRITICAL | HIGH | MEDIUM | LOW

        public Guid? ResponsibleUserId { get; set; }

        public bool RequiresApproval { get; set; } = true;

        /// <summary>NEW | UNDER_REVIEW | ACCEPTED | REJECTED | APPROVED | IMPLEMENTED | OUTCOME_PENDING | OUTCOME_VALIDATED | CLOSED</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "NEW";

        public string? EditedProposalJson { get; set; }

        public string? RealizedOutcomeJson { get; set; }

        public int? FeedbackRating { get; set; }

        [MaxLength(1000)]
        public string? FeedbackNotes { get; set; }

        [MaxLength(180)]
        public string? DeduplicationKey { get; set; }

        public DateTime? CooldownUntilUtc { get; set; }

        public DateTime? ValidatedAtUtc { get; set; }
    }

    public class MenuEngineeringProactiveAlert : BaseModel
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
        public string TriggerType { get; set; } = string.Empty; // PRICE_CHANGE | POS_SYNC | STOCK_EXPIRY | FORECAST_DEVIATION | MARGIN_DETERIORATION | CONSUMPTION_ANOMALY | etc.

        [Required]
        [MaxLength(180)]
        public string DeduplicationKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "WARNING"; // CRITICAL | WARNING | INFO

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Summary { get; set; } = string.Empty;

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        [MaxLength(60)]
        public string? AffectedObjectType { get; set; }

        public Guid? AffectedObjectId { get; set; }

        public string MetricsDeltaJson { get; set; } = "{}";

        [MaxLength(1000)]
        public string? RecommendedAction { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "OPEN"; // OPEN | ACKNOWLEDGED | TASK_CREATED | RESOLVED

        public Guid? AcknowledgedByUserId { get; set; }

        public DateTime? AcknowledgedAtUtc { get; set; }

        public DateTime CooldownUntilUtc { get; set; }
    }

    public class MenuEngineeringLearningOutcome : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [MaxLength(40)]
        public string DatasetVersion { get; set; } = "ds-v1";

        [Required]
        [MaxLength(60)]
        public string ModelVersion { get; set; } = "me-model-v1";

        public Guid? MenuPlanId { get; set; }

        public Guid? MenuItemId { get; set; }

        public Guid? RecommendationId { get; set; }

        [Precision(9, 4)]
        public decimal? PredictedFoodCostPercent { get; set; }

        [Precision(9, 4)]
        public decimal? ActualFoodCostPercent { get; set; }

        [Precision(18, 4)]
        public decimal? PredictedCovers { get; set; }

        [Precision(18, 4)]
        public decimal? ActualCovers { get; set; }

        [Precision(18, 4)]
        public decimal? PredictedMargin { get; set; }

        [Precision(18, 4)]
        public decimal? ActualMargin { get; set; }

        [Precision(9, 4)]
        public decimal? ActualWastePercent { get; set; }

        public bool OutcomeValidated { get; set; }

        [Required]
        [MaxLength(60)]
        public string OutcomeSource { get; set; } = "VALIDATED_POS_RECONCILIATION";

        public string FeaturesJson { get; set; } = "{}";

        public DateTime CapturedAtUtc { get; set; }
    }

    public class MenuEngineeringModelEvaluation : BaseModel
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
        public string ModelName { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string ModelVersion { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string DatasetVersion { get; set; } = "ds-v1";

        [Precision(18, 4)]
        public decimal MetricMae { get; set; }

        [Precision(9, 4)]
        public decimal MetricWape { get; set; }

        [Precision(18, 4)]
        public decimal MetricBias { get; set; }

        [Precision(9, 4)]
        public decimal IntervalCoveragePercent { get; set; }

        public bool IsActiveModel { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime EvaluatedAtUtc { get; set; }
    }

    public class MenuEngineeringAiFeedback : BaseModel
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
        public string AgentId { get; set; } = string.Empty;

        [MaxLength(80)]
        public string? ContextScreen { get; set; }

        public Guid? TargetEntityId { get; set; }

        public int Rating { get; set; } // 1 = helpful, -1 = not helpful, 1..5

        public bool WasAccepted { get; set; }

        [MaxLength(1000)]
        public string? CorrectionNotes { get; set; }

        public Guid ActorUserId { get; set; }

        public DateTime SubmittedAtUtc { get; set; }
    }
}
