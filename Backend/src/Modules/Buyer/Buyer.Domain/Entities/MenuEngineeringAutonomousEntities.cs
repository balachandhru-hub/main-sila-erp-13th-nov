using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuEngineeringBusinessEvent : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(80)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string SourceSystem { get; set; } = "SILA_ME";

        [MaxLength(80)]
        public string? ObjectType { get; set; }

        public Guid? ObjectId { get; set; }

        [MaxLength(200)]
        public string? ObjectLabel { get; set; }

        public DateTime EffectiveAtUtc { get; set; }

        [Required]
        [MaxLength(120)]
        public string CorrelationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string SchemaVersion { get; set; } = "1.0";

        [Required]
        [MaxLength(180)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = "{}";

        [Precision(9, 4)]
        public decimal MaterialityScore { get; set; } = 1.0m;

        [Required]
        [MaxLength(30)]
        public string ProcessingStatus { get; set; } = "PENDING"; // PENDING | PROCESSING | PROCESSED | FAILED | DEAD_LETTER | SKIPPED

        public int RetryCount { get; set; }

        public DateTime? NextRetryAtUtc { get; set; }

        public DateTime? ProcessedAtUtc { get; set; }

        [MaxLength(1000)]
        public string? DeadLetterReason { get; set; }

        public string ProcessedAgentsJson { get; set; } = "[]";
    }

    public class MenuEngineeringAgentAutonomyPolicy : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(80)]
        public string AgentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string ActionType { get; set; } = "ALL";

        /// <summary>0=DISABLED, 1=OBSERVE, 2=RECOMMEND, 3=PREPARE, 4=APPROVAL_BASED_EXECUTION, 5=POLICY_CONTROLLED_AUTOMATION.</summary>
        public int AutonomyLevel { get; set; } = 2;

        [Precision(18, 4)]
        public decimal MaxFinancialThreshold { get; set; } = 2500m;

        [Precision(9, 4)]
        public decimal MaterialityThresholdPercent { get; set; } = 5m;

        public int CooldownMinutes { get; set; } = 60;

        public bool IsPaused { get; set; }

        public string AllowedActionsJson { get; set; } = "[]";

        public int PolicyVersion { get; set; } = 1;

        public Guid? UpdatedByUserId { get; set; }

        public DateTime UpdatedAtUtc { get; set; }
    }

    public class MenuEngineeringActionPlan : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? TriggerEventId { get; set; }

        [Required]
        [MaxLength(120)]
        public string CorrelationId { get; set; } = string.Empty;

        [Required]
        [MaxLength(180)]
        public string DeduplicationKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string BusinessObjective { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string RootCauseAnalysis { get; set; } = string.Empty;

        public string AffectedObjectsJson { get; set; } = "[]";

        public string SupportingEvidenceJson { get; set; } = "[]";

        [Precision(18, 4)]
        public decimal ExpectedFinancialImpact { get; set; }

        [MaxLength(1000)]
        public string ExpectedOperationalImpact { get; set; } = string.Empty;

        [MaxLength(20)]
        public string RiskLevel { get; set; } = "MEDIUM";

        /// <summary>DRAFT | APPROVAL_PENDING | APPROVED | EXECUTING | COMPLETED | PARTIALLY_FAILED | CANCELLED | PAUSED</summary>
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "DRAFT";

        public string RequiredApprovalsJson { get; set; } = "[]";

        public Guid? ResponsibleUserId { get; set; }

        public DateTime? ExecutionDeadlineUtc { get; set; }

        public bool IsConsolidated { get; set; } = true;
    }

    public class MenuEngineeringActionItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        [ForeignKey("ActionPlan")]
        public Guid ActionPlanId { get; set; }

        public MenuEngineeringActionPlan ActionPlan { get; set; } = null!;

        public int SequenceOrder { get; set; }

        public Guid? DependsOnActionId { get; set; }

        [Required]
        [MaxLength(80)]
        public string AgentId { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string ToolId { get; set; } = string.Empty;

        [Required]
        [MaxLength(80)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string RiskClassification { get; set; } = "MEDIUM"; // LOW | MEDIUM | HIGH

        [Required]
        [MaxLength(40)]
        public string PolicyDecision { get; set; } = "REQUIRE_APPROVAL";

        [MaxLength(1000)]
        public string PolicyReason { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal FinancialImpact { get; set; }

        [MaxLength(80)]
        public string? TargetObjectType { get; set; }

        public Guid? TargetObjectId { get; set; }

        public int TargetObjectVersion { get; set; } = 1;

        public DateTime? DataFreshnessSnapshotUtc { get; set; }

        public string PayloadJson { get; set; } = "{}";

        [Required]
        [MaxLength(128)]
        public string PayloadHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string ApprovalStatus { get; set; } = "PENDING"; // NOT_REQUIRED | PENDING | APPROVED | REJECTED | EXPIRED | REAPPROVAL_REQUIRED

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }

        [MaxLength(60)]
        public string? ApprovalChannel { get; set; } // CLOUD | MOBILE | TEAMS

        [Required]
        [MaxLength(180)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string ExecutionStatus { get; set; } = "PENDING"; // PENDING | EXECUTED | FAILED | UNCERTAIN_RECONCILING | REAPPROVAL_REQUIRED | CANCELLED

        public string? ResultJson { get; set; }

        [MaxLength(200)]
        public string? ExternalReference { get; set; }

        public int RetryCount { get; set; }

        public DateTime? ExecutedAtUtc { get; set; }
    }

    public class MenuEngineeringRealizedBenefit : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? RecommendationId { get; set; }

        public Guid? ActionPlanId { get; set; }

        [Required]
        [MaxLength(60)]
        public string BenefitCategory { get; set; } = "CONTRIBUTION_MARGIN";

        [Required]
        [MaxLength(180)]
        public string DeduplicationKey { get; set; } = string.Empty;

        [Precision(18, 4)]
        public decimal BaselineAmount { get; set; }

        [Precision(18, 4)]
        public decimal ExpectedBenefit { get; set; }

        [Precision(18, 4)]
        public decimal RawActualOutcome { get; set; }

        [Precision(18, 4)]
        public decimal AdjustedRealizedBenefit { get; set; }

        public string AdjustmentFactorsJson { get; set; } = "{}";

        [MaxLength(500)]
        public string Methodology { get; set; } = "COMPARABLE_BASELINE_ADJUSTED";

        [MaxLength(30)]
        public string DataQuality { get; set; } = "HIGH";

        [Required]
        [MaxLength(30)]
        public string ValidationStatus { get; set; } = "ESTIMATED"; // ESTIMATED | MONITORING | VALIDATED | REJECTED

        public Guid? ValidatedByUserId { get; set; }

        public DateTime? ValidatedAtUtc { get; set; }

        public DateTime MeasurementStartUtc { get; set; }

        public DateTime MeasurementEndUtc { get; set; }
    }

    public class MenuEngineeringExecutiveBriefing : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Persona { get; set; } = "GENERAL_MANAGER";

        public DateTime BusinessDateUtc { get; set; }

        [Required]
        [MaxLength(180)]
        public string DeduplicationKey { get; set; } = string.Empty;

        public string SummaryJson { get; set; } = "{}";

        [MaxLength(30)]
        public string DeliveryChannel { get; set; } = "CLOUD"; // CLOUD | STORE | TEAMS | EMAIL

        [MaxLength(30)]
        public string DeliveryStatus { get; set; } = "GENERATED";

        public DateTime? DeliveredAtUtc { get; set; }
    }

    public class MenuEngineeringIntegrationMonitorEvent : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Required]
        [MaxLength(50)]
        public string IntegrationType { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string HealthStatus { get; set; } = "NOT_CONFIGURED"; // HEALTHY | DEGRADED | STALE | UNAVAILABLE | NOT_CONFIGURED | SUSPENDED

        [MaxLength(60)]
        public string? FailureCategory { get; set; }

        [MaxLength(1000)]
        public string TechnicalDetail { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string BusinessImpact { get; set; } = string.Empty;

        public string AffectedAgentsJson { get; set; } = "[]";

        [MaxLength(500)]
        public string RecommendedAction { get; set; } = string.Empty;

        public bool IsSuspended { get; set; }

        public DateTime CheckedAtUtc { get; set; }
    }
}
