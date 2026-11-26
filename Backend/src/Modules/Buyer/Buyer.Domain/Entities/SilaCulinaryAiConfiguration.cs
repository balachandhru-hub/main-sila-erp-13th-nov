using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class SilaCulinaryAiConfiguration : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        /// <summary>LOCAL, GEMINI, CLAUDE, or HYBRID.</summary>
        [Required]
        [MaxLength(20)]
        public string OperatingMode { get; set; } = "LOCAL";

        /// <summary>Tenant consent flag before any tenant data can leave to an external LLM provider.</summary>
        public bool AllowExternalAi { get; set; }

        public bool LocalEnabled { get; set; } = true;

        [MaxLength(500)]
        public string? LocalEndpoint { get; set; }

        [MaxLength(200)]
        public string? LocalModel { get; set; }

        public bool GeminiEnabled { get; set; }

        [MaxLength(200)]
        public string? GeminiModel { get; set; }

        /// <summary>DataProtection / AES encrypted Gemini API key. Never returned in plaintext.</summary>
        [MaxLength(2000)]
        public string? ProtectedGeminiApiKey { get; set; }

        public bool ClaudeEnabled { get; set; }

        [MaxLength(200)]
        public string? ClaudeModel { get; set; }

        /// <summary>DataProtection / AES encrypted Claude API key. Never returned in plaintext.</summary>
        [MaxLength(2000)]
        public string? ProtectedClaudeApiKey { get; set; }

        public int RequestTimeoutSeconds { get; set; } = 30;

        public int MaxOutputTokens { get; set; } = 2048;

        [Precision(6, 2)]
        public decimal Temperature { get; set; } = 0.20m;

        public int DailyRequestLimit { get; set; } = 200;

        public int MonthlyRequestLimit { get; set; } = 5000;

        public int DailyTokenBudget { get; set; } = 250000;

        [Precision(18, 2)]
        public decimal MonthlyCostBudgetUsd { get; set; } = 100.00m;

        public int RetryCount { get; set; } = 1;

        /// <summary>NONE, LOCAL_ONLY, or CONFIGURED_ORDER.</summary>
        [Required]
        [MaxLength(30)]
        public string FallbackPolicy { get; set; } = "NONE";

        /// <summary>JSON dictionary mapping culinary task codes to LOCAL | GEMINI | CLAUDE | DISABLED.</summary>
        public string? TaskRoutingJson { get; set; }

        /// <summary>JSON object controlling Recipe Studio 2.0 Phase 0 feature flags.</summary>
        public string? FeatureFlagsJson { get; set; }

        [MaxLength(30)]
        public string? LastTestProvider { get; set; }

        [MaxLength(40)]
        public string? LastTestStatus { get; set; }

        [MaxLength(1000)]
        public string? LastTestMessage { get; set; }

        public DateTime? LastTestedOn { get; set; }
    }
}
