using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MenuEngineeringTwinSnapshot : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? OutletLocationId { get; set; }

        public Guid? MenuPlanId { get; set; }

        public Guid? EventId { get; set; }

        [Required]
        [MaxLength(40)]
        public string SnapshotType { get; set; } = "LIVE"; // LIVE | SCHEDULED | HISTORICAL | SCENARIO | EVENT | MENU

        public int VersionNumber { get; set; } = 1;

        [Required]
        [MaxLength(120)]
        public string SnapshotHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(40)]
        public string CalculationVersion { get; set; } = "me-calc-v3.0";

        [Required]
        [MaxLength(40)]
        public string ModelVersion { get; set; } = "me-twin-v3.0";

        [Precision(9, 4)]
        public decimal DataQualityScore { get; set; } = 100m;

        [Required]
        [MaxLength(30)]
        public string DataQualityLabel { get; set; } = "HIGH";

        public DateTime BusinessEffectiveDateUtc { get; set; }

        public DateTime SourceMaxUpdatedAtUtc { get; set; }

        public DateTime DataFreshnessUtc { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public bool IsStale { get; set; }

        public string TwinPayloadJson { get; set; } = "{}";

        public string LineageJson { get; set; } = "{}";

        public Guid? CapturedByUserId { get; set; }

        public DateTime CapturedAtUtc { get; set; }
    }

    public class MenuEngineeringTwinSimulationRun : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        public Guid? PropertyId { get; set; }

        public Guid? BaselineSnapshotId { get; set; }

        public Guid? MenuPlanId { get; set; }

        public Guid? ScenarioId { get; set; }

        [Required]
        [MaxLength(40)]
        public string RunType { get; set; } = "MONTE_CARLO"; // MONTE_CARLO | SENSITIVITY | STRESS_TEST | OPTIMIZATION

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "QUEUED"; // QUEUED | RUNNING | COMPLETED | CANCELLED | FAILED

        [Required]
        [MaxLength(120)]
        public string IdempotencyKey { get; set; } = string.Empty;

        public int RandomSeed { get; set; } = 42;

        public int IterationCount { get; set; } = 1000;

        [MaxLength(60)]
        public string? SolverUsed { get; set; }

        [Precision(18, 4)]
        public decimal RuntimeMs { get; set; }

        [Precision(18, 6)]
        public decimal? OptimalityGap { get; set; }

        public string ParametersJson { get; set; } = "{}";

        public string AssumptionsJson { get; set; } = "{}";

        public string ResultJson { get; set; } = "{}";

        [Required]
        [MaxLength(30)]
        public string DataQuality { get; set; } = "HIGH";

        public bool IsStale { get; set; }

        [MaxLength(500)]
        public string? ErrorMessage { get; set; }

        public Guid ActorUserId { get; set; }

        public DateTime StartedAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public DateTime? CancelledAtUtc { get; set; }
    }

    public class MenuEngineeringModelDriftEvent : BaseModel
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
        public string ModelName { get; set; } = "DEMAND_FORECAST";

        [Required]
        [MaxLength(60)]
        public string ModelVersion { get; set; } = "me-model-v3.0";

        [Required]
        [MaxLength(40)]
        public string DatasetVersion { get; set; } = "ds-v3";

        [Required]
        [MaxLength(40)]
        public string DriftType { get; set; } = "BIAS_DRIFT";

        [Precision(18, 4)]
        public decimal BaselineMetric { get; set; }

        [Precision(18, 4)]
        public decimal ObservedMetric { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "DETECTED"; // DETECTED | RETRAIN_SCHEDULED | EVALUATED | DEPLOYED | ROLLED_BACK

        [MaxLength(60)]
        public string? ChampionModelVersion { get; set; }

        [MaxLength(60)]
        public string? ChallengerModelVersion { get; set; }

        public string EvidenceJson { get; set; } = "{}";

        public DateTime DetectedAtUtc { get; set; }

        public Guid? ApprovedByUserId { get; set; }

        public DateTime? ApprovedAtUtc { get; set; }
    }
}
