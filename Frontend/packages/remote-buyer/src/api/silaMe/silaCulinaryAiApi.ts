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
}

export interface SilaCulinaryAiStatus {
  buyerId: string;
  configured: boolean;
  operatingMode: CulinaryAiMode;
  allowExternalAi: boolean;
  localEnabled: boolean;
  geminiEnabled: boolean;
  claudeEnabled: boolean;
  featureFlags: SilaCulinaryAiFeatureFlags;
}

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
  taskRouting: Record<string, string>;
  featureFlags: SilaCulinaryAiFeatureFlags;
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
};

export const CULINARY_TASK_LABELS: { key: string; label: string }[] = [
  { key: "recipe_generation", label: "Recipe generation" },
  { key: "menu_generation", label: "Menu generation" },
  { key: "event_planning", label: "Event planning" },
  { key: "ingredient_extraction", label: "Ingredient extraction" },
  { key: "recipe_document_interpretation", label: "Recipe document interpretation" },
  { key: "material_semantic_matching", label: "Material semantic matching" },
  { key: "ingredient_substitution_suggestions", label: "Ingredient substitution suggestions" },
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
