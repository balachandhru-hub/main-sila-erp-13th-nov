using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryAnalyticsConfig : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [Precision(5, 2)]
        public decimal TargetFoodCostPercent { get; set; } = 28.0m;

        [Precision(18, 2)]
        public decimal TargetContributionMargin { get; set; } = 50.0m;

        [Precision(5, 4)]
        public decimal PopularityThresholdFactor { get; set; } = 0.70m;

        [Precision(18, 2)]
        public decimal? MenuProfitabilityThresholdOverride { get; set; }

        [Precision(5, 2)]
        public decimal IngredientPriceAlertThresholdPercent { get; set; } = 5.0m;

        [Precision(5, 2)]
        public decimal WasteTolerancePercent { get; set; } = 4.0m;

        [Precision(5, 2)]
        public decimal ConsumptionVarianceTolerancePercent { get; set; } = 5.0m;

        public int DefaultForecastHorizonDays { get; set; } = 14;

        public int MinimumHistoricalDataDays { get; set; } = 7;

        public int MinSalesForClassification { get; set; } = 5;

        [MaxLength(40)]
        public string CostMethodology { get; set; } = "STANDARD";

        [MaxLength(10)]
        public string DefaultCurrency { get; set; } = "AED";

        public string ApprovedSubstitutionRulesJson { get; set; } = "[]";

        public string NutritionalConstraintsJson { get; set; } = "{}";

        public string AllergenRestrictionsJson { get; set; } = "[]";

        public string OptimizationObjectivesJson { get; set; } = "[\"MAX_CONTRIBUTION\"]";

        [MaxLength(40)]
        public string PreferredAiProvider { get; set; } = "LOCAL";

        [MaxLength(120)]
        public string PreferredAiModel { get; set; } = "local-deterministic";

        public int DailyAiQueryLimit { get; set; } = 200;

        [MaxLength(40)]
        public string RecommendationApprovalMode { get; set; } = "EXECUTIVE_CHEF_AND_FINANCE";

        [MaxLength(40)]
        public string ScheduledRefreshFrequency { get; set; } = "DAILY";

        public int ConfigVersion { get; set; } = 1;

        public string ConfigHistoryJson { get; set; } = "[]";
    }

    public class SilaCulinarySimulation : BaseModel
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
        [MaxLength(60)]
        public string SimulationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(40)]
        public string ScenarioType { get; set; } = "SCENARIO_A";

        [MaxLength(10)]
        public string Currency { get; set; } = "AED";

        public bool IsolatedFromProduction { get; set; } = true;

        public bool IsShared { get; set; }

        public string ParametersJson { get; set; } = "{}";

        public string ResultsJson { get; set; } = "{}";

        [Precision(18, 2)]
        public decimal BaselineContribution { get; set; }

        [Precision(18, 2)]
        public decimal SimulatedContribution { get; set; }

        [Precision(18, 2)]
        public decimal ExpectedSavings { get; set; }

        public Guid CreatedByUserId { get; set; }
    }

    public class SilaCulinaryOptimizationJob : BaseModel
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
        [MaxLength(60)]
        public string JobCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string JobType { get; set; } = "MENU_OPTIMIZATION";

        [MaxLength(40)]
        public string Objective { get; set; } = "MAX_CONTRIBUTION";

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "COMPLETED";

        public int RetryCount { get; set; }

        public int TimeoutSeconds { get; set; } = 60;

        public string RequestJson { get; set; } = "{}";

        public string ResultJson { get; set; } = "{}";

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAtUtc { get; set; }

        public Guid RequestedByUserId { get; set; }
    }

    public class SilaCulinaryRecommendation : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        [MaxLength(160)]
        public string? PropertyName { get; set; }

        [MaxLength(160)]
        public string? OutletName { get; set; }

        [Required]
        [MaxLength(60)]
        public string RecommendationCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string Category { get; set; } = "REVIEW_SELLING_PRICE";

        [Required]
        [MaxLength(60)]
        public string SourceEngine { get; set; } = "MENU_ENGINEERING";

        [Required]
        [MaxLength(200)]
        public string DeduplicationKey { get; set; } = string.Empty;

        [MaxLength(240)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(600)]
        public string CurrentSituation { get; set; } = string.Empty;

        [MaxLength(600)]
        public string ProposedAction { get; set; } = string.Empty;

        public string SupportingEvidenceJson { get; set; } = "{}";

        [Precision(18, 2)]
        public decimal ExpectedFinancialImpact { get; set; }

        [Precision(18, 2)]
        public decimal? RealizedFinancialImpact { get; set; }

        [MaxLength(20)]
        public string Priority { get; set; } = "HIGH";

        [MaxLength(30)]
        public string DataQuality { get; set; } = "VERIFIED";

        [MaxLength(20)]
        public string RiskLevel { get; set; } = "LOW";

        [MaxLength(80)]
        public string ResponsibleRole { get; set; } = "EXECUTIVE_CHEF";

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = "NEW";

        public DateTime ValidFromUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ValidToUtc { get; set; }

        [MaxLength(120)]
        public string? ApprovalReference { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedOnUtc { get; set; }

        [MaxLength(400)]
        public string? DecisionComment { get; set; }
    }

    public class SilaCulinaryWasteRecord : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? LocationId { get; set; }

        [MaxLength(160)]
        public string? OutletName { get; set; }

        [MaxLength(60)]
        public string MealPeriod { get; set; } = "All Day";

        public Guid? EventId { get; set; }

        [Required]
        [MaxLength(60)]
        public string WasteNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(60)]
        public string WasteCategory { get; set; } = "PRODUCTION_WASTE";

        [Required]
        [MaxLength(200)]
        public string IngredientName { get; set; } = string.Empty;

        public Guid? MaterialId { get; set; }

        public Guid? RecipeId { get; set; }

        public Guid? MenuItemId { get; set; }

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        [MaxLength(20)]
        public string Uom { get; set; } = "KG";

        [Precision(18, 2)]
        public decimal EstimatedCost { get; set; }

        public int CoversServed { get; set; }

        [MaxLength(20)]
        public string SourceChannel { get; set; } = "CLOUD";

        [MaxLength(30)]
        public string Status { get; set; } = "RECORDED";

        [MaxLength(120)]
        public string? IdempotencyKey { get; set; }

        [MaxLength(400)]
        public string? Notes { get; set; }

        public Guid RecordedByUserId { get; set; }

        public DateTime RecordedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class SilaCulinaryAnalyticsAudit : BaseModel
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
        [MaxLength(60)]
        public string AuditType { get; set; } = string.Empty;

        public Guid ActorUserId { get; set; }

        public Guid ActorRoleId { get; set; }

        [MaxLength(400)]
        public string Summary { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = "{}";

        public bool PromptInjectionBlocked { get; set; }

        public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
