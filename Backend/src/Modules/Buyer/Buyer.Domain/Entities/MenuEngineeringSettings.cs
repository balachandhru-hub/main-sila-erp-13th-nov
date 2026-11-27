using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuEngineeringSettings : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Precision(18, 4)]
        public decimal DefaultMealParticipation { get; set; } = 0.70m;

        [Precision(18, 4)]
        public decimal DefaultItemParticipation { get; set; } = 0.20m;

        [Precision(18, 4)]
        public decimal DefaultBufferPercentage { get; set; } = 0.10m;

        [Precision(18, 4)]
        public decimal DefaultAdultFactor { get; set; } = 1m;

        [Precision(18, 4)]
        public decimal DefaultChildFactor { get; set; } = 0.6m;

        [Precision(18, 4)]
        public decimal PopularityThreshold { get; set; } = 0.70m;

        [Precision(18, 4)]
        public decimal HighFoodCostPercent { get; set; } = 0.35m;

        public int StaleHours { get; set; } = 24;

        public string? MealPeriodsJson { get; set; }

        public string? CategoriesJson { get; set; }

        public string? PmsProvider { get; set; } = "OPERA";

        public string? PmsConnectionName { get; set; }

        public string? PosConnectionName { get; set; }

        public string? AiNotes { get; set; }

        public bool MenuEngineeringEnabled { get; set; } = true;

        public bool MeStudioEnabled { get; set; } = true;

        public Guid? PropertyId { get; set; }

        public string? ServiceFormatsJson { get; set; }

        public string? CuisinesJson { get; set; }

        public string? DietaryTagsJson { get; set; }

        public string? CostingMethod { get; set; } = "CURRENT_COST";

        [Precision(9, 4)]
        public decimal TargetFoodCostPercent { get; set; } = 0.30m;

        [Precision(18, 4)]
        public decimal? TargetContributionMargin { get; set; }

        public string? CostRefreshFrequency { get; set; } = "REALTIME";

        public string? DefaultCurrency { get; set; } = "USD";

        public string? TaxTreatment { get; set; } = "EXCLUSIVE";

        public string? DefaultPortionUom { get; set; } = "PORTION";

        [Precision(9, 4)]
        public decimal PortionVarianceThresholdPercent { get; set; } = 10m;

        public int ForecastHorizonDays { get; set; } = 14;

        public string? ForecastMethod { get; set; } = "HYBRID_RIDGE";

        public int HistoricalLookbackDays { get; set; } = 90;

        [Precision(9, 4)]
        public decimal ForecastAccuracyThresholdPercent { get; set; } = 80m;

        public string? ProfitabilityBasis { get; set; } = "FOOD_COST_ONLY";

        [Precision(9, 4)]
        public decimal? ProfitabilityThresholdPercent { get; set; }

        public int MinimumSalesHistoryDays { get; set; } = 14;

        [Precision(18, 4)]
        public decimal MinimumSalesQuantity { get; set; } = 10m;

        public string? ComparableGrouping { get; set; } = "CATEGORY";

        public string? PosSyncScheduleCron { get; set; } = "0 */4 * * *";

        public int PosRetryMaxAttempts { get; set; } = 3;

        public string? AiEnabledAgentsJson { get; set; }

        [Precision(9, 4)]
        public decimal AiConfidenceThreshold { get; set; } = 0.70m;

        public int AiUsageLimitPerDay { get; set; } = 100;

        [Precision(9, 4)]
        public decimal ApprovalFoodCostThresholdPercent { get; set; } = 35m;

        [Precision(9, 4)]
        public decimal ApprovalPriceChangePercent { get; set; } = 10m;

        public bool RequireApprovalOnSubstitution { get; set; } = true;

        public bool RequireApprovalOnPublish { get; set; } = true;

        public string? ApprovalRolesJson { get; set; }

        public int ConfigVersion { get; set; } = 1;

        [MaxLength(30)]
        public string DefaultOperatingMode { get; set; } = "AI_ASSISTED";

        public bool ProactiveAlertsEnabled { get; set; } = true;

        public int ProactiveCooldownMinutes { get; set; } = 120;

        public bool AutonomousModePaused { get; set; }

        public bool AutomatedExecutionDisabled { get; set; }

        [MaxLength(500)]
        public string? EmergencyPauseReason { get; set; }

        public DateTime? EmergencyPausedAtUtc { get; set; }

        public Guid? EmergencyPausedByUserId { get; set; }

        public int MaxConcurrentJobs { get; set; } = 4;

        public int DailyAiTokenBudget { get; set; } = 250000;

        [Precision(18, 2)]
        public decimal MonthlyAiBudgetUsd { get; set; } = 100.00m;

        public bool TeamsNotificationsEnabled { get; set; }

        [MaxLength(500)]
        public string? TeamsWebhookUrl { get; set; }

        public string? EventScheduleConfigJson { get; set; }
    }
}
