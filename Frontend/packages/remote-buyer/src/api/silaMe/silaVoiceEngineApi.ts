import axiosInstance from "../axiosInstance";
import { readError } from "../readError";

/** Thin client for Buyer-proxied SILA Voice Engine (does not replace Voice PO APIs). */

const BASE = "/api/v1/buyer/sila/voice-engine";

export interface SilaVoiceEngineHealth {
  status?: string;
  asrProvider?: string;
  ttsProvider?: string;
  allowedAgents?: string[];
  activeAgents?: string[];
  reservedAgents?: string[];
}

export interface SilaVoiceEngineSessionStart {
  language?: string | null;
  locationId?: string | null;
  propertyId?: string | null;
  outletId?: string | null;
  purchasingProfileId?: string | null;
}

export interface SilaVoiceEngineTurn {
  transcript?: string | null;
  audioBase64?: string | null;
  sampleRate?: number;
  channel?: string;
  bargeIn?: boolean;
  confirm?: boolean;
  idempotencyKey?: string | null;
}

export interface SilaVoiceEngineTurnResult {
  ok?: boolean;
  sessionId?: string;
  state?: string;
  intent?: string;
  agentId?: string | null;
  spokenReply?: string | null;
  ttsText?: string | null;
  asrStatus?: string | null;
  silaVoicePoSessionId?: string | null;
  draft?: unknown;
  askToRepeat?: boolean;
  processingDurationMs?: number;
  message?: string;
}

export const getVoiceEngineHealth = async (): Promise<SilaVoiceEngineHealth | null> => {
  try {
    const response = await axiosInstance.get(`${BASE}/health`);
    return response.data as SilaVoiceEngineHealth;
  } catch {
    return null;
  }
};

export const startVoiceEngineSession = async (
  request: SilaVoiceEngineSessionStart = {},
): Promise<SilaVoiceEngineTurnResult> => {
  try {
    const response = await axiosInstance.post(`${BASE}/sessions`, request);
    return response.data as SilaVoiceEngineTurnResult;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not start the Voice Engine session."));
  }
};

export const sendVoiceEngineTurn = async (
  sessionId: string,
  request: SilaVoiceEngineTurn,
): Promise<SilaVoiceEngineTurnResult> => {
  try {
    const response = await axiosInstance.post(`${BASE}/sessions/${sessionId}/turns`, request);
    return response.data as SilaVoiceEngineTurnResult;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not process the Voice Engine turn."));
  }
};

export const cancelVoiceEngineSession = async (sessionId: string): Promise<SilaVoiceEngineTurnResult> => {
  try {
    const response = await axiosInstance.post(`${BASE}/sessions/${sessionId}/cancel`);
    return response.data as SilaVoiceEngineTurnResult;
  } catch (error: unknown) {
    throw new Error(readError(error, "Could not cancel the Voice Engine session."));
  }
};
