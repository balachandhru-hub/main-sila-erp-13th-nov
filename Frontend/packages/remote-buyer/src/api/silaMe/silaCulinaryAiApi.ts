import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

const BASE = "/api/v1/buyer/sila/culinary-ai";

export type CulinaryAiMode = "LOCAL" | "GEMINI" | "CLAUDE" | "HYBRID";
export type CulinaryAiFallbackPolicy = "NONE" | "LOCAL_IF_ALLOWED" | "CONFIGURED_SECONDARY";

export interface SilaCulinaryAiFeatureFlags {
  culinarySuiteEnabled: boolean;
  overviewEnabled: boolean;
  recipePlannerPreviewEnabled: boolean;
  materialIntelligencePreviewEnabled: boolean;
  menuEventPreviewEnabled: boolean;
  interactiveSimulatorsEnabled: boolean;
  analyticsReportPreviewEnabled: boolean;
  aiSetupEnabled: boolean;
  demonstrationModeEnabled: boolean;
  culinaryPhase4AEnabled?: boolean;
  kitchenMasterEnabled?: boolean;
  productionPlanningEnabled?: boolean;
  productionSchedulingEnabled?: boolean;
  kdsDisplayEnabled?: boolean;
  qualityCheckpointsEnabled?: boolean;
  centralKitchenEnabled?: boolean;
}

export interface SilaCulinaryAiUsageSummary {
  trackingActive: boolean;
  note: string;
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  inputTokens: number;
  outputTokens: number;
  estimatedCostUsd: number;
  averageLatencyMs: number | null;
  fallbackCount: number;
  fallbackExecutions?: number;
  localInferenceStatus?: string;
}

export interface SilaCulinaryAiStatus {
  buyerId: string;
  configured: boolean;
  phase?: string;
  operatingMode: CulinaryAiMode;
  allowExternalAi: boolean;
  localEnabled: boolean;
  localConfigured?: boolean;
  geminiEnabled: boolean;
  geminiConfigured?: boolean;
  claudeEnabled: boolean;
  claudeConfigured?: boolean;
  canSelectProvider?: boolean;
  promptVersion?: string;
  featureFlags: SilaCulinaryAiFeatureFlags;
}

export interface SilaCulinaryMaterialConfig {
  exactMatchingEnabled: boolean;
  aliasMatchingEnabled: boolean;
  fuzzyMatchingEnabled: boolean;
  semanticMatchingEnabled: boolean;
  advancedRerankingEnabled: boolean;
  llmAssistedMatchingEnabled: boolean;
  candidateRetrievalLimit: number;
  candidateDisplayLimit: number;
  fuzzyThreshold: number;
  semanticThreshold: number;
  autoSuggestThreshold: number;
  ambiguityGapThreshold: number;
  mandatoryChefReview: boolean;
  embeddingModel: string;
  rerankingModel: string;
  executionDevice: string;
  embeddingBatchSize: number;
  maxIngredientsPerBulk: number;
}

export const DEFAULT_CULINARY_MATERIAL_CONFIG: SilaCulinaryMaterialConfig = {
  exactMatchingEnabled: true,
  aliasMatchingEnabled: true,
  fuzzyMatchingEnabled: true,
  semanticMatchingEnabled: true,
  advancedRerankingEnabled: true,
  llmAssistedMatchingEnabled: false,
  candidateRetrievalLimit: 15,
  candidateDisplayLimit: 5,
  fuzzyThreshold: 70,
  semanticThreshold: 0.66,
  autoSuggestThreshold: 0.72,
  ambiguityGapThreshold: 0.04,
  mandatoryChefReview: true,
  embeddingModel: "BAAI/bge-small-en-v1.5",
  rerankingModel: "Xenova/ms-marco-MiniLM-L-6-v2",
  executionDevice: "cpu",
  embeddingBatchSize: 32,
  maxIngredientsPerBulk: 100,
};

export interface SilaCulinaryCostingConfig {
  costSourceHierarchy: string[];
  reportingCurrency: string;
  taxBasis: string;
  defaultTargetFoodCostPercent: number;
  allowManualCostEstimate: boolean;
  blockPublishOnMissingCost: boolean;
  autoRecostOnLocalPriceChange: boolean;
  maxSubRecipeDepth: number;
  priceRoundingDecimals: number;
}

export const DEFAULT_CULINARY_COSTING_CONFIG: SilaCulinaryCostingConfig = {
  costSourceHierarchy: [
    "OUTLET_PRICE",
    "CONTRACT_PRICE",
    "CATALOG_PRICE",
    "MOVING_AVERAGE_PRICE",
    "STANDARD_PRICE",
    "UNIT_COST",
    "LATEST_PURCHASE_PRICE",
  ],
  reportingCurrency: "AED",
  taxBasis: "TAX_EXCLUSIVE",
  defaultTargetFoodCostPercent: 28,
  allowManualCostEstimate: true,
  blockPublishOnMissingCost: true,
  autoRecostOnLocalPriceChange: false,
  maxSubRecipeDepth: 5,
  priceRoundingDecimals: 2,
};

export interface SilaCulinaryAiConfig {
  id: string;
  buyerId: string;
  saved: boolean;
  canEdit: boolean;
  operatingMode: CulinaryAiMode;
  allowExternalAi: boolean;
  localEnabled: boolean;
  localEndpoint: string | null;
  localModel: string | null;
  geminiEnabled: boolean;
  geminiModel: string | null;
  geminiKeyConfigured: boolean;
  geminiKeyMasked: string | null;
  claudeEnabled: boolean;
  claudeModel: string | null;
  claudeKeyConfigured: boolean;
  claudeKeyMasked: string | null;
  requestTimeoutSeconds: number;
  maxOutputTokens: number;
  temperature: number;
  dailyRequestLimit: number;
  monthlyRequestLimit: number;
  dailyTokenBudget: number;
  monthlyCostBudgetUsd: number;
  retryCount: number;
  fallbackPolicy: CulinaryAiFallbackPolicy;
  promptVersion?: string;
  taskRouting: Record<string, string>;
  featureFlags: SilaCulinaryAiFeatureFlags;
  materialConfig?: SilaCulinaryMaterialConfig;
  costingConfig?: SilaCulinaryCostingConfig;
  lastTestProvider: string | null;
  lastTestStatus: string | null;
  lastTestMessage: string | null;
  lastTestedOn: string | null;
  usageSummary: SilaCulinaryAiUsageSummary;
}

export interface SilaCulinaryAiConfigWrite {
  operatingMode: CulinaryAiMode;
  allowExternalAi: boolean;
  localEnabled: boolean;
  localEndpoint?: string | null;
  localModel?: string | null;
  geminiEnabled: boolean;
  geminiModel?: string | null;
  geminiApiKey?: string | null;
  clearGeminiKey?: boolean;
  claudeEnabled: boolean;
  claudeModel?: string | null;
  claudeApiKey?: string | null;
  clearClaudeKey?: boolean;
  requestTimeoutSeconds: number;
  maxOutputTokens: number;
  temperature: number;
  dailyRequestLimit: number;
  monthlyRequestLimit: number;
  dailyTokenBudget: number;
  monthlyCostBudgetUsd: number;
  retryCount: number;
  fallbackPolicy: CulinaryAiFallbackPolicy;
  taskRouting: Record<string, string>;
  featureFlags: SilaCulinaryAiFeatureFlags;
  materialConfig?: SilaCulinaryMaterialConfig;
  costingConfig?: SilaCulinaryCostingConfig;
}

export interface SilaCulinaryAiConnectionTestResult {
  provider: string;
  success: boolean;
  statusCode: string;
  message: string;
  modelChecked: string | null;
  latencyMs: number | null;
  testedAtUtc: string;
}

export const DEFAULT_CULINARY_FEATURE_FLAGS: SilaCulinaryAiFeatureFlags = {
  culinarySuiteEnabled: true,
  overviewEnabled: true,
  recipePlannerPreviewEnabled: true,
  materialIntelligencePreviewEnabled: true,
  menuEventPreviewEnabled: true,
  interactiveSimulatorsEnabled: true,
  analyticsReportPreviewEnabled: true,
  aiSetupEnabled: true,
  demonstrationModeEnabled: true,
  culinaryPhase4AEnabled: true,
  kitchenMasterEnabled: true,
  productionPlanningEnabled: true,
  productionSchedulingEnabled: true,
  kdsDisplayEnabled: true,
  qualityCheckpointsEnabled: true,
  centralKitchenEnabled: true,
};

export const CULINARY_TASK_LABELS: { key: string; label: string }[] = [
  { key: "recipe_generation", label: "Recipe generation" },
  { key: "recipe_refinement", label: "Recipe refinement" },
  { key: "recipe_document_interpretation", label: "Recipe document interpretation" },
  { key: "recipe_image_interpretation", label: "Dish photograph interpretation" },
  { key: "preparation_instructions", label: "Preparation instructions" },
  { key: "ingredient_substitution_suggestions", label: "Ingredient substitution suggestions" },
  { key: "dietary_analysis", label: "Dietary & allergen analysis" },
  { key: "culinary_explanation", label: "Culinary explanation" },
  { key: "menu_generation", label: "Menu generation" },
  { key: "event_planning", label: "Event planning" },
  { key: "ingredient_extraction", label: "Ingredient extraction" },
  { key: "material_semantic_matching", label: "Material semantic matching" },
  { key: "food_cost_recommendations", label: "Food cost recommendations" },
  { key: "management_insights", label: "Management insights" },
  { key: "report_summaries", label: "Report summaries" },
  { key: "voice_interpretation", label: "Voice interpretation" },
];

export const getCulinaryAiStatus = async (): Promise<SilaCulinaryAiStatus> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryAiStatus>(`${BASE}/status`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Culinary AI status."));
  }
};

export const getCulinaryAiConfig = async (): Promise<SilaCulinaryAiConfig> => {
  try {
    const res = await axiosInstance.get<SilaCulinaryAiConfig>(`${BASE}/config`);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to load Culinary AI configuration."));
  }
};

export const saveCulinaryAiConfig = async (
  payload: SilaCulinaryAiConfigWrite
): Promise<SilaCulinaryAiConfig> => {
  try {
    const res = await axiosInstance.put<SilaCulinaryAiConfig>(`${BASE}/config`, payload);
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Unable to save Culinary AI configuration."));
  }
};

export const testCulinaryAiConnection = async (
  provider: "LOCAL" | "GEMINI" | "CLAUDE"
): Promise<SilaCulinaryAiConnectionTestResult> => {
  try {
    const res = await axiosInstance.post<SilaCulinaryAiConnectionTestResult>(
      `${BASE}/test-connection`,
      { provider }
    );
    return res.data;
  } catch (err) {
    throw new Error(await readError(err, "Connection test failed."));
  }
};
