using System.Text.Json;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using SharedKernel.ExceptionHandler;
using SharedKernel.Integration.Services;

namespace Buyer.Application.Features.Shared
{
    public static class SilaCulinaryAiSettings
    {
        public const string MODE_LOCAL = "LOCAL";
        public const string MODE_GEMINI = "GEMINI";
        public const string MODE_CLAUDE = "CLAUDE";
        public const string MODE_HYBRID = "HYBRID";

        public const string FALLBACK_NONE = "NONE";
        public const string FALLBACK_LOCAL_ONLY = "LOCAL_ONLY";
        public const string FALLBACK_CONFIGURED_ORDER = "CONFIGURED_ORDER";

        public const string TASK_ROUTE_DISABLED = "DISABLED";
        public const string PROMPT_VERSION_V1 = "culinary-recipe-v1.0";

        public static readonly string[] OperatingModes = { MODE_LOCAL, MODE_GEMINI, MODE_CLAUDE, MODE_HYBRID };
        public static readonly string[] FallbackPolicies = { FALLBACK_NONE, FALLBACK_LOCAL_ONLY, FALLBACK_CONFIGURED_ORDER };

        public static readonly string[] SupportedTasks =
        {
            "RECIPE_GENERATION",
            "RECIPE_REFINEMENT",
            "RECIPE_DOCUMENT_INTERPRETATION",
            "RECIPE_IMAGE_INTERPRETATION",
            "INGREDIENT_SUBSTITUTION",
            "PREPARATION_INSTRUCTIONS",
            "DIETARY_ANALYSIS",
            "CULINARY_EXPLANATION",
            "MENU_GENERATION",
            "EVENT_PLANNING",
            "INGREDIENT_EXTRACTION",
            "MATERIAL_SEMANTIC_MATCHING",
            "FOOD_COST_RECOMMENDATIONS",
            "MANAGEMENT_INSIGHTS",
            "REPORT_SUMMARIES",
            "VOICE_INTERPRETATION",
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static Dictionary<string, string> DefaultTaskRouting()
        {
            Dictionary<string, string> map = new(StringComparer.OrdinalIgnoreCase);
            foreach (string task in SupportedTasks)
            {
                map[task] = MODE_LOCAL;
            }
            return map;
        }

        public static string NormalizeMode(string? mode)
        {
            string normalized = (mode ?? MODE_LOCAL).Trim().ToUpperInvariant();
            if (!OperatingModes.Contains(normalized))
            {
                throw new BadRequestCustomException(
                    "Invalid Culinary AI operating mode.",
                    "Select LOCAL, GEMINI, CLAUDE, or HYBRID.");
            }
            return normalized;
        }

        public static string NormalizeFallback(string? policy)
        {
            string normalized = (policy ?? FALLBACK_NONE).Trim().ToUpperInvariant();
            if (normalized == "LOCAL_IF_ALLOWED") normalized = FALLBACK_LOCAL_ONLY;
            if (normalized == "CONFIGURED_SECONDARY") normalized = FALLBACK_CONFIGURED_ORDER;
            if (!FallbackPolicies.Contains(normalized))
            {
                throw new BadRequestCustomException(
                    "Invalid fallback policy.",
                    "Select NONE, LOCAL_ONLY, or CONFIGURED_ORDER.");
            }
            return normalized;
        }

        public static void ValidateWrite(SilaCulinaryAiConfigWriteDto input)
        {
            _ = NormalizeMode(input.OperatingMode);
            _ = NormalizeFallback(input.FallbackPolicy);
            if (input.RequestTimeoutSeconds < 5 || input.RequestTimeoutSeconds > 120)
            {
                throw new BadRequestCustomException("Invalid timeout.", "Timeout must be between 5 and 120 seconds.");
            }
            if (input.MaxOutputTokens < 128 || input.MaxOutputTokens > 16384)
            {
                throw new BadRequestCustomException("Invalid max output tokens.", "Max output tokens must be between 128 and 16384.");
            }
            if (input.Temperature < 0m || input.Temperature > 1.0m)
            {
                throw new BadRequestCustomException("Invalid temperature.", "Temperature must be between 0.0 and 1.0.");
            }
            if (input.DailyRequestLimit < 0 || input.MonthlyRequestLimit < 0 || input.DailyTokenBudget < 0 || input.MonthlyCostBudgetUsd < 0)
            {
                throw new BadRequestCustomException("Invalid budget limit.", "Usage limits and budgets cannot be negative.");
            }
            if (input.RetryCount < 0 || input.RetryCount > 3)
            {
                throw new BadRequestCustomException("Invalid retry count.", "Retry count must be between 0 and 3.");
            }
        }

        public static Dictionary<string, string> NormalizeTaskRouting(Dictionary<string, string>? raw)
        {
            Dictionary<string, string> normalized = DefaultTaskRouting();
            if (raw == null) return normalized;

            Dictionary<string, string> ci = new(raw, StringComparer.OrdinalIgnoreCase);
            foreach (string task in SupportedTasks)
            {
                if (!ci.TryGetValue(task, out string? candidate) || string.IsNullOrWhiteSpace(candidate))
                {
                    continue;
                }
                string engine = candidate.Trim().ToUpperInvariant();
                if (engine == MODE_LOCAL || engine == MODE_GEMINI || engine == MODE_CLAUDE || engine == TASK_ROUTE_DISABLED)
                {
                    normalized[task] = engine;
                }
            }
            return normalized;
        }

        public static SilaCulinaryAiFeatureFlagsDto ParseFeatureFlags(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new SilaCulinaryAiFeatureFlagsDto();
            }
            try
            {
                return JsonSerializer.Deserialize<SilaCulinaryAiFeatureFlagsDto>(json, JsonOptions)
                    ?? new SilaCulinaryAiFeatureFlagsDto();
            }
            catch
            {
                return new SilaCulinaryAiFeatureFlagsDto();
            }
        }

        public static SilaCulinaryMaterialConfigDto ParseMaterialConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new SilaCulinaryMaterialConfigDto();
            }
            try
            {
                return JsonSerializer.Deserialize<SilaCulinaryMaterialConfigDto>(json, JsonOptions)
                    ?? new SilaCulinaryMaterialConfigDto();
            }
            catch
            {
                return new SilaCulinaryMaterialConfigDto();
            }
        }

        public static string SerializeMaterialConfig(SilaCulinaryMaterialConfigDto? cfg)
        {
            SilaCulinaryMaterialConfigDto value = cfg ?? new SilaCulinaryMaterialConfigDto();
            value.CandidateRetrievalLimit = Math.Clamp(value.CandidateRetrievalLimit, 3, 50);
            value.CandidateDisplayLimit = Math.Clamp(value.CandidateDisplayLimit, 1, 15);
            value.FuzzyThreshold = Math.Clamp(value.FuzzyThreshold, 30.0, 99.0);
            value.SemanticThreshold = Math.Clamp(value.SemanticThreshold, 0.25, 0.99);
            value.AutoSuggestThreshold = Math.Clamp(value.AutoSuggestThreshold, 0.30, 0.99);
            value.AmbiguityGapThreshold = Math.Clamp(value.AmbiguityGapThreshold, 0.0, 0.40);
            value.EmbeddingBatchSize = Math.Clamp(value.EmbeddingBatchSize, 4, 256);
            value.MaxIngredientsPerBulk = Math.Clamp(value.MaxIngredientsPerBulk, 5, 500);
            return JsonSerializer.Serialize(value, JsonOptions);
        }

        public static SilaCulinaryCostingConfigDto ParseCostingConfig(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new SilaCulinaryCostingConfigDto();
            }
            try
            {
                return JsonSerializer.Deserialize<SilaCulinaryCostingConfigDto>(json, JsonOptions)
                    ?? new SilaCulinaryCostingConfigDto();
            }
            catch
            {
                return new SilaCulinaryCostingConfigDto();
            }
        }

        public static string SerializeCostingConfig(SilaCulinaryCostingConfigDto? cfg)
        {
            SilaCulinaryCostingConfigDto value = cfg ?? new SilaCulinaryCostingConfigDto();
            value.DefaultTargetFoodCostPercent = Math.Clamp(value.DefaultTargetFoodCostPercent, 1.0m, 95.0m);
            value.MaxSubRecipeDepth = Math.Clamp(value.MaxSubRecipeDepth, 1, 10);
            if (value.CostSourceHierarchy == null || value.CostSourceHierarchy.Count == 0)
            {
                value.CostSourceHierarchy = new SilaCulinaryCostingConfigDto().CostSourceHierarchy;
            }
            return JsonSerializer.Serialize(value, JsonOptions);
        }

        public static Dictionary<string, string> ParseTaskRouting(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return DefaultTaskRouting();
            }
            try
            {
                Dictionary<string, string>? parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
                return NormalizeTaskRouting(parsed);
            }
            catch
            {
                return DefaultTaskRouting();
            }
        }

        public static string SerializeFeatureFlags(SilaCulinaryAiFeatureFlagsDto? flags)
        {
            return JsonSerializer.Serialize(flags ?? new SilaCulinaryAiFeatureFlagsDto(), JsonOptions);
        }

        public static string SerializeTaskRouting(Dictionary<string, string>? routing)
        {
            return JsonSerializer.Serialize(NormalizeTaskRouting(routing), JsonOptions);
        }

        public static string? MaskSecret(string? encryptedBlob, IIntegrationCredentialProtector protector)
        {
            if (string.IsNullOrWhiteSpace(encryptedBlob))
            {
                return null;
            }
            try
            {
                string? plain = protector.Unprotect(encryptedBlob);
                if (string.IsNullOrWhiteSpace(plain)) return "Configured";
                string trimmed = plain.Trim();
                if (trimmed.Length <= 8) return "••••••••";
                return $"••••{trimmed[^4..]}";
            }
            catch
            {
                return "Configured";
            }
        }

        public static bool IsMaskedPlaceholder(string? submitted)
        {
            if (string.IsNullOrWhiteSpace(submitted)) return true;
            return submitted.Contains("••••", StringComparison.Ordinal)
                || string.Equals(submitted.Trim(), "Configured", StringComparison.OrdinalIgnoreCase);
        }

        public static SilaCulinaryAiConfiguration CreateDefault(Guid buyerId) => new()
        {
            Id = Guid.Empty,
            BuyerId = buyerId,
            OperatingMode = MODE_LOCAL,
            AllowExternalAi = false,
            LocalEnabled = true,
            LocalEndpoint = "http://127.0.0.1:11434",
            LocalModel = "qwen2.5:7b-instruct",
            GeminiEnabled = false,
            GeminiModel = "gemini-2.5-flash",
            ClaudeEnabled = false,
            ClaudeModel = "claude-sonnet-4-20250514",
            RequestTimeoutSeconds = 30,
            MaxOutputTokens = 2048,
            Temperature = 0.20m,
            DailyRequestLimit = 200,
            MonthlyRequestLimit = 5000,
            DailyTokenBudget = 250000,
            MonthlyCostBudgetUsd = 100.00m,
            RetryCount = 1,
            FallbackPolicy = FALLBACK_NONE,
            TaskRoutingJson = SerializeTaskRouting(null),
            FeatureFlagsJson = SerializeFeatureFlags(null),
            MaterialConfigJson = SerializeMaterialConfig(null),
            CostingConfigJson = SerializeCostingConfig(null),
        };

        public static SilaCulinaryAiConfigDto ToDto(
            SilaCulinaryAiConfiguration? entity,
            Guid buyerId,
            bool canEdit,
            IIntegrationCredentialProtector protector,
            List<SilaCulinaryAiUsageLog>? usageLogs = null)
        {
            bool saved = entity != null && entity.Id != Guid.Empty;
            SilaCulinaryAiConfiguration cfg = entity ?? CreateDefault(buyerId);
            List<SilaCulinaryAiUsageLog> logs = usageLogs ?? new List<SilaCulinaryAiUsageLog>();
            int totalReq = logs.Count;
            int okReq = logs.Count(x => x.Success);
            int failReq = totalReq - okReq;
            int fallbackCount = logs.Count(x => x.UsedFallback);
            double? avgLatency = totalReq > 0 ? Math.Round(logs.Average(x => (double)x.LatencyMs), 1) : null;
            SilaCulinaryAiUsageSummaryDto usage = new()
            {
                TrackingActive = true,
                Note = totalReq > 0
                    ? "Counters reflect recorded Phase 1A server AI executions (zero fabricated metrics)."
                    : "Counters reflect recorded server executions only (no executions recorded yet).",
                TotalRequests = totalReq,
                SuccessfulRequests = okReq,
                FailedRequests = failReq,
                InputTokens = logs.Sum(x => (long)x.InputTokens),
                OutputTokens = logs.Sum(x => (long)x.OutputTokens),
                EstimatedCostUsd = logs.Sum(x => x.EstimatedCostUsd),
                FallbackExecutions = fallbackCount,
                FallbackCount = fallbackCount,
                AverageLatencyMs = avgLatency,
                LocalInferenceStatus = cfg.LastTestProvider == MODE_LOCAL && !string.IsNullOrWhiteSpace(cfg.LastTestStatus)
                    ? cfg.LastTestStatus
                    : (cfg.LocalEnabled ? "READY_TO_TEST" : "DISABLED")
            };

            return new SilaCulinaryAiConfigDto
            {
                Id = cfg.Id,
                BuyerId = buyerId,
                Saved = saved,
                CanEdit = canEdit,
                OperatingMode = cfg.OperatingMode,
                AllowExternalAi = cfg.AllowExternalAi,
                LocalEnabled = cfg.LocalEnabled,
                LocalEndpoint = cfg.LocalEndpoint,
                LocalModel = cfg.LocalModel,
                GeminiEnabled = cfg.GeminiEnabled,
                GeminiModel = cfg.GeminiModel,
                GeminiKeyConfigured = !string.IsNullOrWhiteSpace(cfg.ProtectedGeminiApiKey),
                GeminiKeyMasked = MaskSecret(cfg.ProtectedGeminiApiKey, protector),
                ClaudeEnabled = cfg.ClaudeEnabled,
                ClaudeModel = cfg.ClaudeModel,
                ClaudeKeyConfigured = !string.IsNullOrWhiteSpace(cfg.ProtectedClaudeApiKey),
                ClaudeKeyMasked = MaskSecret(cfg.ProtectedClaudeApiKey, protector),
                RequestTimeoutSeconds = cfg.RequestTimeoutSeconds,
                MaxOutputTokens = cfg.MaxOutputTokens,
                Temperature = cfg.Temperature,
                DailyRequestLimit = cfg.DailyRequestLimit,
                MonthlyRequestLimit = cfg.MonthlyRequestLimit,
                DailyTokenBudget = cfg.DailyTokenBudget,
                MonthlyCostBudgetUsd = cfg.MonthlyCostBudgetUsd,
                RetryCount = cfg.RetryCount,
                FallbackPolicy = cfg.FallbackPolicy,
                PromptVersion = PROMPT_VERSION_V1,
                TaskRouting = ParseTaskRouting(cfg.TaskRoutingJson),
                FeatureFlags = ParseFeatureFlags(cfg.FeatureFlagsJson),
                MaterialConfig = ParseMaterialConfig(cfg.MaterialConfigJson),
                CostingConfig = ParseCostingConfig(cfg.CostingConfigJson),
                LastTestProvider = cfg.LastTestProvider,
                LastTestStatus = cfg.LastTestStatus,
                LastTestMessage = cfg.LastTestMessage,
                LastTestedOn = cfg.LastTestedOn,
                UpdatedOn = saved ? cfg.DateUpdated : null,
                Usage = usage,
                UsageSummary = usage
            };
        }

        public static SilaCulinaryAiStatusDto ToStatusDto(
            SilaCulinaryAiConfiguration? entity,
            Guid buyerId,
            bool canSelectProvider = false)
        {
            SilaCulinaryAiConfiguration cfg = entity ?? CreateDefault(buyerId);
            bool localOk = cfg.LocalEnabled && !string.IsNullOrWhiteSpace(cfg.LocalEndpoint);
            bool geminiOk = cfg.AllowExternalAi && cfg.GeminiEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedGeminiApiKey);
            bool claudeOk = cfg.AllowExternalAi && cfg.ClaudeEnabled && !string.IsNullOrWhiteSpace(cfg.ProtectedClaudeApiKey);
            return new SilaCulinaryAiStatusDto
            {
                BuyerId = buyerId,
                Configured = entity != null && entity.Id != Guid.Empty,
                Phase = "PHASE_1A",
                OperatingMode = cfg.OperatingMode,
                LocalConfigured = localOk,
                LocalEnabled = cfg.LocalEnabled,
                GeminiConfigured = geminiOk,
                GeminiEnabled = geminiOk,
                ClaudeConfigured = claudeOk,
                ClaudeEnabled = claudeOk,
                AllowExternalAi = cfg.AllowExternalAi,
                CanSelectProvider = canSelectProvider,
                PromptVersion = PROMPT_VERSION_V1,
                FeatureFlags = ParseFeatureFlags(cfg.FeatureFlagsJson)
            };
        }
    }
}
