namespace Buyer.Domain.Dtos
{
    public class SilaCulinaryAiFeatureFlagsDto
    {
        public bool CulinarySuiteEnabled { get; set; } = true;
        public bool OverviewEnabled { get; set; } = true;
        public bool AiRecipePlannerPreview { get; set; } = true;
        public bool RecipePlannerPreviewEnabled { get; set; } = true;
        public bool MaterialIntelligencePreview { get; set; } = true;
        public bool MaterialIntelligencePreviewEnabled { get; set; } = true;
        public bool MaterialRequestsPreview { get; set; } = true;
        public bool MenuStudioPreview { get; set; } = true;
        public bool EventPlannerPreview { get; set; } = true;
        public bool MenuEventPreviewEnabled { get; set; } = true;
        public bool MrpSimulatorEnabled { get; set; } = true;
        public bool EventProcurementPreview { get; set; } = true;
        public bool ProductionPlannerDemo { get; set; } = true;
        public bool FoodCostStudioPreview { get; set; } = true;
        public bool EventPnlSimulatorEnabled { get; set; } = true;
        public bool InteractiveSimulatorsEnabled { get; set; } = true;
        public bool CulinaryAnalyticsDemo { get; set; } = true;
        public bool ReportStudioPreview { get; set; } = true;
        public bool AnalyticsReportPreviewEnabled { get; set; } = true;
        public bool AiEngineSetupEnabled { get; set; } = true;
        public bool AiSetupEnabled { get; set; } = true;
        public bool MobileRecipeStudio2Enabled { get; set; } = true;
        public bool DemonstrationModeEnabled { get; set; } = true;
        // Phase 3B Culinary Analytics, Menu Engineering & AI Optimization flags (tenant-controlled, independent of procurement release)
        public bool CulinaryPhase3BEnabled { get; set; } = true;
        public bool MenuEngineeringMatrixEnabled { get; set; } = true;
        public bool WhatIfSimulationEnabled { get; set; } = true;
        public bool PythonOptimizationEnabled { get; set; } = true;
        public bool DemandForecastingEnabled { get; set; } = true;
        public bool WasteIntelligenceEnabled { get; set; } = true;
        public bool CulinaryCopilotEnabled { get; set; } = true;
        public bool CrossPropertyBenchmarkingEnabled { get; set; } = true;
        public bool RecommendationCockpitEnabled { get; set; } = true;
    }

    public class SilaCulinaryAiUsageSummaryDto
    {
        public bool TrackingActive { get; set; } = true;
        public string Note { get; set; } = "Counters reflect recorded server executions only (no fabricated metrics).";
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }
        public decimal EstimatedCostUsd { get; set; }
        public int FallbackExecutions { get; set; }
        public int FallbackCount { get; set; }
        public double? AverageLatencyMs { get; set; }
        public string LocalInferenceStatus { get; set; } = "NOT_TESTED";
    }

    public class SilaCulinaryAiConfigDto
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
        public bool Saved { get; set; }
        public bool CanEdit { get; set; }
        public string OperatingMode { get; set; } = "LOCAL";
        public bool AllowExternalAi { get; set; }
        public bool LocalEnabled { get; set; } = true;
        public string? LocalEndpoint { get; set; }
        public string? LocalModel { get; set; }
        public bool GeminiEnabled { get; set; }
        public string? GeminiModel { get; set; }
        public bool GeminiKeyConfigured { get; set; }
        public string? GeminiKeyMasked { get; set; }
        public bool ClaudeEnabled { get; set; }
        public string? ClaudeModel { get; set; }
        public bool ClaudeKeyConfigured { get; set; }
        public string? ClaudeKeyMasked { get; set; }
        public int RequestTimeoutSeconds { get; set; } = 30;
        public int MaxOutputTokens { get; set; } = 2048;
        public decimal Temperature { get; set; } = 0.20m;
        public int DailyRequestLimit { get; set; } = 200;
        public int MonthlyRequestLimit { get; set; } = 5000;
        public int DailyTokenBudget { get; set; } = 250000;
        public decimal MonthlyCostBudgetUsd { get; set; } = 100.00m;
        public int RetryCount { get; set; } = 1;
        public string FallbackPolicy { get; set; } = "NONE";
        public string PromptVersion { get; set; } = "culinary-recipe-v1.0";
        public Dictionary<string, string> TaskRouting { get; set; } = new();
        public SilaCulinaryAiFeatureFlagsDto FeatureFlags { get; set; } = new();
        public SilaCulinaryMaterialConfigDto MaterialConfig { get; set; } = new();
        public SilaCulinaryCostingConfigDto CostingConfig { get; set; } = new();
        public string? LastTestProvider { get; set; }
        public string? LastTestStatus { get; set; }
        public string? LastTestMessage { get; set; }
        public DateTime? LastTestedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public SilaCulinaryAiUsageSummaryDto Usage { get; set; } = new();
        public SilaCulinaryAiUsageSummaryDto UsageSummary { get; set; } = new();
    }

    public class SilaCulinaryAiConfigWriteDto
    {
        public string OperatingMode { get; set; } = "LOCAL";
        public bool AllowExternalAi { get; set; }
        public bool LocalEnabled { get; set; } = true;
        public string? LocalEndpoint { get; set; }
        public string? LocalModel { get; set; }
        public bool GeminiEnabled { get; set; }
        public string? GeminiModel { get; set; }
        public string? GeminiApiKey { get; set; }
        public bool ClearGeminiKey { get; set; }
        public bool ClaudeEnabled { get; set; }
        public string? ClaudeModel { get; set; }
        public string? ClaudeApiKey { get; set; }
        public bool ClearClaudeKey { get; set; }
        public int RequestTimeoutSeconds { get; set; } = 30;
        public int MaxOutputTokens { get; set; } = 2048;
        public decimal Temperature { get; set; } = 0.20m;
        public int DailyRequestLimit { get; set; } = 200;
        public int MonthlyRequestLimit { get; set; } = 5000;
        public int DailyTokenBudget { get; set; } = 250000;
        public decimal MonthlyCostBudgetUsd { get; set; } = 100.00m;
        public int RetryCount { get; set; } = 1;
        public string FallbackPolicy { get; set; } = "NONE";
        public Dictionary<string, string>? TaskRouting { get; set; }
        public SilaCulinaryAiFeatureFlagsDto? FeatureFlags { get; set; }
        public SilaCulinaryMaterialConfigDto? MaterialConfig { get; set; }
        public SilaCulinaryCostingConfigDto? CostingConfig { get; set; }
    }

    public class SilaCulinaryMaterialConfigDto
    {
        public bool ExactMatchingEnabled { get; set; } = true;
        public bool AliasMatchingEnabled { get; set; } = true;
        public bool FuzzyMatchingEnabled { get; set; } = true;
        public bool SemanticMatchingEnabled { get; set; } = true;
        public bool AdvancedRerankingEnabled { get; set; } = true;
        public bool LlmAssistedMatchingEnabled { get; set; } = false;
        public int CandidateRetrievalLimit { get; set; } = 15;
        public int CandidateDisplayLimit { get; set; } = 5;
        public double FuzzyThreshold { get; set; } = 70.0;
        public double SemanticThreshold { get; set; } = 0.66;
        public double AutoSuggestThreshold { get; set; } = 0.72;
        public double AmbiguityGapThreshold { get; set; } = 0.04;
        public bool MandatoryChefReview { get; set; } = true;
        public string EmbeddingModel { get; set; } = "BAAI/bge-small-en-v1.5";
        public string RerankingModel { get; set; } = "Xenova/ms-marco-MiniLM-L-6-v2";
        public string ExecutionDevice { get; set; } = "cpu";
        public int EmbeddingBatchSize { get; set; } = 32;
        public int MaxIngredientsPerBulk { get; set; } = 100;
    }

    public class SilaCulinaryCostingConfigDto
    {
        public string ReportingCurrency { get; set; } = "AED";
        public string TaxBasis { get; set; } = "TAX_EXCLUSIVE";
        public decimal DefaultTargetFoodCostPercent { get; set; } = 28.0m;
        public List<string> CostSourceHierarchy { get; set; } = new()
        {
            "OUTLET_PRICE",
            "MOVING_AVERAGE_PRICE",
            "STANDARD_PRICE",
            "UNIT_COST",
            "LATEST_PURCHASE_PRICE",
            "MANUAL_ESTIMATE"
        };
        public bool AllowManualCostEstimate { get; set; } = true;
        public bool BlockPublishOnMissingCost { get; set; } = true;
        public bool RequireApprovalOnPriceExceedTarget { get; set; } = true;
        public string ExpiredPricePolicy { get; set; } = "WARN_AND_FALLBACK";
        public int MaxSubRecipeDepth { get; set; } = 5;
        public bool AutoRecostOnLocalPriceChange { get; set; } = false;
        public string DefaultMaterialRequestApprovalMode { get; set; } = "TENANT_FLOW_OR_ADMIN";
    }

    public class SilaCulinaryAiConnectionTestRequestDto
    {
        public string Provider { get; set; } = "LOCAL";
        public string? LocalEndpointOverride { get; set; }
        public string? ModelOverride { get; set; }
    }

    public class SilaCulinaryAiConnectionTestResultDto
    {
        public string Provider { get; set; } = "LOCAL";
        public bool Success { get; set; }
        public string StatusCode { get; set; } = "NOT_TESTED";
        public string Message { get; set; } = string.Empty;
        public string? ModelChecked { get; set; }
        public int LatencyMs { get; set; }
        public DateTime TestedAtUtc { get; set; }
        public List<string> AvailableModels { get; set; } = new();
    }

    public class SilaCulinaryAiStatusDto
    {
        public Guid BuyerId { get; set; }
        public bool Configured { get; set; }
        public string Phase { get; set; } = "PHASE_1A";
        public string OperatingMode { get; set; } = "LOCAL";
        public bool LocalConfigured { get; set; }
        public bool LocalEnabled { get; set; }
        public bool GeminiConfigured { get; set; }
        public bool GeminiEnabled { get; set; }
        public bool ClaudeConfigured { get; set; }
        public bool ClaudeEnabled { get; set; }
        public bool AllowExternalAi { get; set; }
        public bool CanSelectProvider { get; set; }
        public string PromptVersion { get; set; } = "culinary-recipe-v1.0";
        public SilaCulinaryAiFeatureFlagsDto FeatureFlags { get; set; } = new();
    }
}
