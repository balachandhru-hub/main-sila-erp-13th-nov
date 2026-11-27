namespace Buyer.Domain.Dtos
{
    public class PublishBusinessEventRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = "SILA_ME";
        public string? ObjectType { get; set; }
        public Guid? ObjectId { get; set; }
        public string? ObjectLabel { get; set; }
        public DateTime? EffectiveAtUtc { get; set; }
        public string? CorrelationId { get; set; }
        public string? IdempotencyKey { get; set; }
        public decimal MaterialityScore { get; set; } = 15m;
        public Dictionary<string, object?> Payload { get; set; } = new();
    }

    public class BusinessEventDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string SourceSystem { get; set; } = string.Empty;
        public string? ObjectType { get; set; }
        public Guid? ObjectId { get; set; }
        public string? ObjectLabel { get; set; }
        public DateTime EffectiveAtUtc { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public decimal MaterialityScore { get; set; }
        public string ProcessingStatus { get; set; } = string.Empty;
        public int RetryCount { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
        public string? DeadLetterReason { get; set; }
        public List<string> RoutedAgents { get; set; } = new();
        public List<string> PayloadHighlights { get; set; } = new();
    }

    public class EventProcessingSummaryDto
    {
        public int PendingScanned { get; set; }
        public int ProcessedCount { get; set; }
        public int SkippedOrCooldownCount { get; set; }
        public int FailedOrRetriedCount { get; set; }
        public int DeadLetteredCount { get; set; }
        public int ActionPlansCreated { get; set; }
        public List<BusinessEventDto> Events { get; set; } = new();
    }

    public class AgentAutonomyPolicyDto
    {
        public Guid? Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ActionType { get; set; } = "ALL";
        public int AutonomyLevel { get; set; } = 2; // 0..5
        public string AutonomyLevelLabel { get; set; } = "LEVEL_2_RECOMMEND";
        public decimal MaxFinancialThreshold { get; set; } = 2500m;
        public decimal MaterialityThresholdPercent { get; set; } = 5m;
        public int CooldownMinutes { get; set; } = 60;
        public bool IsPaused { get; set; }
        public int PolicyVersion { get; set; } = 1;
        public List<string> AllowedActions { get; set; } = new();
    }

    public class UpsertAgentAutonomyPolicyRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string ActionType { get; set; } = "ALL";
        public int AutonomyLevel { get; set; } = 2;
        public decimal MaxFinancialThreshold { get; set; } = 2500m;
        public decimal MaterialityThresholdPercent { get; set; } = 5m;
        public int CooldownMinutes { get; set; } = 60;
        public bool IsPaused { get; set; }
        public List<string> AllowedActions { get; set; } = new();
    }

    public class EmergencyControlRequestDto
    {
        public bool? AutonomousModePaused { get; set; }
        public bool? AutomatedExecutionDisabled { get; set; }
        public string? Reason { get; set; }
        public string? PauseAgentId { get; set; }
        public bool? PauseAgent { get; set; }
        public string? SuspendIntegrationType { get; set; }
        public bool? SuspendIntegration { get; set; }
        public bool CancelPendingJobs { get; set; }
    }

    public class EmergencyControlStatusDto
    {
        public bool AutonomousModePaused { get; set; }
        public bool AutomatedExecutionDisabled { get; set; }
        public string? EmergencyPauseReason { get; set; }
        public DateTime? EmergencyPausedAtUtc { get; set; }
        public List<string> PausedAgents { get; set; } = new();
        public List<string> SuspendedIntegrations { get; set; } = new();
        public int CancelledJobsCount { get; set; }
    }

    public class ActionItemDto
    {
        public Guid Id { get; set; }
        public Guid ActionPlanId { get; set; }
        public int SequenceOrder { get; set; }
        public Guid? DependsOnActionId { get; set; }
        public string AgentId { get; set; } = string.Empty;
        public string ToolId { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string RiskClassification { get; set; } = "MEDIUM";
        public string PolicyDecision { get; set; } = string.Empty;
        public string PolicyReason { get; set; } = string.Empty;
        public decimal FinancialImpact { get; set; }
        public string? TargetObjectType { get; set; }
        public Guid? TargetObjectId { get; set; }
        public int TargetObjectVersion { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string? ApprovalChannel { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }
        public string ExecutionStatus { get; set; } = string.Empty;
        public string? ExternalReference { get; set; }
        public string? ResultSummary { get; set; }
        public DateTime? ExecutedAtUtc { get; set; }
    }

    public class ActionPlanDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? TriggerEventId { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string BusinessObjective { get; set; } = string.Empty;
        public string RootCauseAnalysis { get; set; } = string.Empty;
        public List<string> AffectedObjects { get; set; } = new();
        public List<string> SupportingEvidence { get; set; } = new();
        public decimal ExpectedFinancialImpact { get; set; }
        public string ExpectedOperationalImpact { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "MEDIUM";
        public string Status { get; set; } = string.Empty;
        public List<string> RequiredApprovals { get; set; } = new();
        public DateTime? ExecutionDeadlineUtc { get; set; }
        public bool IsConsolidated { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<ActionItemDto> Actions { get; set; } = new();
    }

    public class ApproveActionItemRequestDto
    {
        public bool Approved { get; set; } = true;
        public string ApprovalChannel { get; set; } = "CLOUD";
        public string? Notes { get; set; }
    }

    public class ExecuteActionItemRequestDto
    {
        public string? IdempotencyKey { get; set; }
        public bool SimulateExternalTimeout { get; set; }
    }

    public class ActionExecutionResultDto
    {
        public Guid ActionItemId { get; set; }
        public string ExecutionStatus { get; set; } = string.Empty;
        public string PolicyDecision { get; set; } = string.Empty;
        public string PolicyReason { get; set; } = string.Empty;
        public bool WasIdempotentReplay { get; set; }
        public bool RequiresReapproval { get; set; }
        public bool ExternalOutcomeReconciled { get; set; }
        public string? ExternalReference { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ExtendedLifecycleTransitionDto
    {
        public string TargetStatus { get; set; } = string.Empty;
        public string? RejectionReasonCode { get; set; }
        public string? RejectionReasonDetail { get; set; }
        public Guid? SupersededById { get; set; }
    }

    public class RecordRealizedBenefitRequestDto
    {
        public Guid? PropertyId { get; set; }
        public Guid? RecommendationId { get; set; }
        public Guid? ActionPlanId { get; set; }
        public string BenefitCategory { get; set; } = "CONTRIBUTION_MARGIN";
        public decimal BaselineAmount { get; set; }
        public decimal ExpectedBenefit { get; set; }
        public decimal RawActualOutcome { get; set; }
        public decimal GuestCountAdjustmentFactor { get; set; } = 1.0m;
        public decimal PriceChangeAdjustmentFactor { get; set; } = 1.0m;
        public decimal SeasonalityAdjustmentFactor { get; set; } = 1.0m;
        public string Methodology { get; set; } = "COMPARABLE_BASELINE_ADJUSTED";
        public string DataQuality { get; set; } = "HIGH";
        public DateTime? MeasurementStartUtc { get; set; }
        public DateTime? MeasurementEndUtc { get; set; }
    }

    public class RealizedBenefitDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public Guid? RecommendationId { get; set; }
        public Guid? ActionPlanId { get; set; }
        public string BenefitCategory { get; set; } = string.Empty;
        public decimal BaselineAmount { get; set; }
        public decimal ExpectedBenefit { get; set; }
        public decimal RawActualOutcome { get; set; }
        public decimal AdjustedRealizedBenefit { get; set; }
        public string Methodology { get; set; } = string.Empty;
        public string DataQuality { get; set; } = string.Empty;
        public string ValidationStatus { get; set; } = string.Empty;
        public DateTime? ValidatedAtUtc { get; set; }
        public DateTime MeasurementStartUtc { get; set; }
        public DateTime MeasurementEndUtc { get; set; }
    }

    public class DataReadinessDimensionDto
    {
        public string Dimension { get; set; } = string.Empty;
        public string Status { get; set; } = "READY";
        public decimal CompletenessPercent { get; set; }
        public DateTime? LastUpdatedUtc { get; set; }
        public string Detail { get; set; } = string.Empty;
    }

    public class DataReadinessAssessmentDto
    {
        public string OverallClassification { get; set; } = "READY"; // READY | READY_WITH_LIMITATIONS | INSUFFICIENT_DATA | STALE_DATA | INTEGRATION_UNAVAILABLE
        public decimal OverallScorePercent { get; set; }
        public bool HighImpactRecommendationsBlocked { get; set; }
        public List<string> BlockingReasons { get; set; } = new();
        public List<DataReadinessDimensionDto> Dimensions { get; set; } = new();
    }

    public class IntegrationHealthItemDto
    {
        public string IntegrationType { get; set; } = string.Empty;
        public string Classification { get; set; } = "NOT_CONFIGURED"; // HEALTHY | DEGRADED | STALE | UNAVAILABLE | NOT_CONFIGURED | SUSPENDED
        public string? FailureCategory { get; set; }
        public string TechnicalDetail { get; set; } = string.Empty;
        public string BusinessImpact { get; set; } = string.Empty;
        public List<string> AffectedAgents { get; set; } = new();
        public string RecommendedAction { get; set; } = string.Empty;
        public bool IsSuspended { get; set; }
        public DateTime CheckedAtUtc { get; set; }
    }

    public class ExecutiveBriefingDto
    {
        public Guid Id { get; set; }
        public Guid? PropertyId { get; set; }
        public string Persona { get; set; } = string.Empty;
        public DateTime BusinessDateUtc { get; set; }
        public string DeliveryChannel { get; set; } = "CLOUD";
        public string DeliveryStatus { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public List<string> KeyHighlights { get; set; } = new();
        public List<string> UrgentRisks { get; set; } = new();
        public List<string> PendingDecisions { get; set; } = new();
    }

    public class ExecutiveDashboardDto
    {
        public string Persona { get; set; } = "GENERAL_MANAGER";
        public string Currency { get; set; } = "USD";
        public decimal CurrentFoodCostPercent { get; set; }
        public decimal TargetFoodCostPercent { get; set; }
        public decimal PredictedNextWeekFoodCostPercent { get; set; }
        public decimal FoodCostDeviationPercent { get; set; }
        public decimal ContributionMargin { get; set; }
        public decimal ForecastAccuracyPercent { get; set; }
        public decimal ShortageRiskScore { get; set; }
        public decimal ExpiryExposureValue { get; set; }
        public decimal WasteCostValue { get; set; }
        public decimal PotentialSavingsValue { get; set; }
        public decimal ValidatedRealizedSavingsValue { get; set; }
        public List<string> MajorRisks { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public List<ExecutiveBriefingDto> Briefings { get; set; } = new();
    }

    public class AiControlTowerDto
    {
        public EmergencyControlStatusDto EmergencyStatus { get; set; } = new();
        public DataReadinessAssessmentDto DataReadiness { get; set; } = new();
        public int ActiveAgentsCount { get; set; }
        public int RecentExecutionsCount { get; set; }
        public int OpenRecommendationsCount { get; set; }
        public int CriticalRisksCount { get; set; }
        public int PendingApprovalsCount { get; set; }
        public int FailedActionsCount { get; set; }
        public decimal EstimatedFinancialExposure { get; set; }
        public decimal PotentialSavings { get; set; }
        public decimal ValidatedRealizedSavings { get; set; }
        public decimal ForecastAccuracyPercent { get; set; }
        public string ModelHealthStatus { get; set; } = "HEALTHY";
        public List<MenuEngineeringAgentDescriptorDto> Agents { get; set; } = new();
        public List<AgentAutonomyPolicyDto> AutonomyPolicies { get; set; } = new();
        public List<BusinessEventDto> RecentEvents { get; set; } = new();
        public List<ActionPlanDto> ActionPlans { get; set; } = new();
        public List<ActionItemDto> PendingApprovals { get; set; } = new();
        public List<MeProactiveAlertDto> RisksAndExceptions { get; set; } = new();
        public List<MeRecommendationCockpitItemDto> Opportunities { get; set; } = new();
        public List<RealizedBenefitDto> RealizedBenefits { get; set; } = new();
        public List<IntegrationHealthItemDto> IntegrationHealth { get; set; } = new();
        public MeLearningSummaryDto LearningPerformance { get; set; } = new();
        public UsageObservabilityDto Observability { get; set; } = new();
    }

    public class UsageObservabilityDto
    {
        public int TotalAgentExecutions { get; set; }
        public decimal SuccessRatePercent { get; set; }
        public int PendingEventQueueDepth { get; set; }
        public int DeadLetterEventCount { get; set; }
        public int TotalTokensUsedToday { get; set; }
        public int DailyTokenBudget { get; set; }
        public decimal EstimatedMonthlyCostUsd { get; set; }
        public decimal MonthlyBudgetUsd { get; set; }
        public decimal AvgExecutionDurationMs { get; set; }
    }

    public class TeamsDispatchRequestDto
    {
        public Guid? PropertyId { get; set; }
        public string AlertType { get; set; } = "EXECUTIVE_BRIEFING";
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public Guid? ActionItemId { get; set; }
    }

    public class TeamsApprovalCallbackRequestDto
    {
        public Guid ActionItemId { get; set; }
        public Guid ActingUserId { get; set; }
        public Guid ActingRoleId { get; set; }
        public Guid? PropertyId { get; set; }
        public bool Approved { get; set; } = true;
        public DateTime? ExpiresAtUtc { get; set; }
        public string? TeamsMessageId { get; set; }
        public string? Notes { get; set; }
    }

    public class ProactiveMenuHealthDto
    {
        public Guid? MenuPlanId { get; set; }
        public string MenuTitle { get; set; } = string.Empty;
        public decimal CurrentFoodCostPercent { get; set; }
        public decimal TargetFoodCostPercent { get; set; }
        public int UnavailableIngredientsCount { get; set; }
        public bool ProductionFeasible { get; set; } = true;
        public List<string> Alerts { get; set; } = new();
        public List<string> RecommendedNextSteps { get; set; } = new();
    }
}
