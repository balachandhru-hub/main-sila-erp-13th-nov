/** Persisted id from SuccessResponseDto (`id` / `Id`); empty when the body has none. */
export const readSuccessId = (data: unknown): string => {
  if (!data || typeof data !== "object") return "";
  const record = data as Record<string, unknown>;
  const raw = record.id ?? record.Id;
  return typeof raw === "string" && raw.trim() ? raw.trim() : "";
};
