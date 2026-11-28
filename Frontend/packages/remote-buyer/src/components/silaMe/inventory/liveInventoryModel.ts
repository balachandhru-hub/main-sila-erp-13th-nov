import type { SilaBottleVision, SilaLiveInventoryRow, SilaProductClass } from "../../../api/silaMe/silaInventoryApi";

/** Pure helpers behind the Live Inventory command center (no React, no network). */

export const PRODUCT_CLASSES: { id: SilaProductClass; label: string }[] = [
  { id: "BEVERAGE", label: "Beverage" },
  { id: "FOOD", label: "Food" },
  { id: "MINIBAR", label: "Minibar" },
  { id: "TOBACCO", label: "Tobacco" },
  { id: "GENERAL", label: "General" },
];

export const productClassLabel = (value?: string | null): string =>
  PRODUCT_CLASSES.find((entry) => entry.id === value)?.label ?? "General";

const PACK_PATTERN = /(\d+(?:[.,]\d+)?)\s*(ml|cl|ltr|lt|l)\b/gi;

/** Pack size in ml from an item description such as "GIN 750ML", "VODKA 70 CL" or "WHISKY 1.75 L". */
export const parsePackSizeMl = (description?: string | null): number | null => {
  if (!description) return null;
  let found: number | null = null;
  for (const match of description.matchAll(PACK_PATTERN)) {
    const value = Number(match[1].replace(",", "."));
    const unit = match[2].toLowerCase();
    const ml = unit === "ml" ? value : unit === "cl" ? value * 10 : value * 1000;
    if (Number.isFinite(ml) && ml >= 10 && ml <= 5000) found = ml;
  }
  return found;
};

const COUNT_UOMS = new Set(["EA", "BTL", "BT", "BOT", "PC", "PCS", "NOS", "NO", "UN", "UNIT", "BOTTLE"]);

/** An item counted in whole bottles whose size is known, so an open bottle can be measured. */
export const isBottleItem = (row: Pick<SilaLiveInventoryRow, "description" | "baseUom" | "productClass">): boolean => {
  const liquid = row.productClass === "BEVERAGE" || row.productClass === "MINIBAR";
  return liquid && COUNT_UOMS.has((row.baseUom ?? "").trim().toUpperCase()) && parsePackSizeMl(row.description) != null;
};

const round3 = (value: number): number => Math.round(value * 1000) / 1000;

export const splitSealedFraction = (qty: number): { sealed: number; fraction: number } => {
  const safe = Number.isFinite(qty) ? Math.max(0, qty) : 0;
  const sealed = Math.floor(safe + 1e-9);
  return { sealed, fraction: round3(Math.max(0, safe - sealed)) };
};

export const composeQty = (sealed: number, fraction: number): number => round3(Math.max(0, sealed) + Math.min(1, Math.max(0, fraction)));

export interface OpenBottleStats {
  sealed: number;
  fraction: number;
  fractionPct: number;
  packMl: number | null;
  openRemainingMl: number | null;
  openConsumedMl: number | null;
}

export const openBottleStats = (row: Pick<SilaLiveInventoryRow, "description" | "baseUom" | "productClass">, qty: number): OpenBottleStats => {
  const bottle = isBottleItem(row);
  const { sealed, fraction } = bottle ? splitSealedFraction(qty) : { sealed: Math.floor(Math.max(0, qty)), fraction: 0 };
  const packMl = bottle ? parsePackSizeMl(row.description) : null;
  return {
    sealed,
    fraction,
    fractionPct: Math.round(fraction * 1000) / 10,
    packMl,
    openRemainingMl: packMl != null && fraction > 0 ? Math.round(packMl * fraction) : null,
    openConsumedMl: packMl != null && fraction > 0 ? Math.round(packMl * (1 - fraction)) : null,
  };
};

export const hasOpenBottle = (row: SilaLiveInventoryRow): boolean =>
  isBottleItem(row) && splitSealedFraction(row.availableQty ?? row.onHandQty).fraction > 0;

export const parCoveragePct = (qty: number, par?: number | null): number | null =>
  par != null && par > 0 ? Math.max(0, Math.min(100, Math.round((qty / par) * 100))) : null;

export const stockValueOf = (row: Pick<SilaLiveInventoryRow, "onHandQty" | "unitCost">): number | null =>
  row.unitCost != null ? Math.round(row.onHandQty * row.unitCost * 100) / 100 : null;

export type CommandChip = "ALL" | SilaProductClass | "TRANSIT" | "LOW" | "OPEN";

/**
 * Omni-search over already loaded rows. "open bottles" and "in transit" are commands, as in the prototype;
 * anything else is matched against the material code, description, class, group, barcode and linked UOM.
 */
export const matchesOmniQuery = (row: SilaLiveInventoryRow, query: string): boolean => {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  if (q.includes("open bottle")) return hasOpenBottle(row);
  if (q.includes("transit")) return (row.inTransitQty ?? 0) > 0;
  const blob = [row.materialCode, row.description, row.baseUom, row.productClass, row.materialGroup, row.barcode]
    .filter(Boolean)
    .join(" ")
    .toLowerCase();
  return blob.includes(q);
};

export const isCommandQuery = (query: string): boolean => {
  const q = query.trim().toLowerCase();
  return q.includes("open bottle") || q.includes("transit");
};

export const applyChip = (rows: SilaLiveInventoryRow[], chip: CommandChip): SilaLiveInventoryRow[] => {
  switch (chip) {
    case "ALL":
      return rows;
    case "TRANSIT":
      return rows.filter((row) => (row.inTransitQty ?? 0) > 0);
    case "LOW":
      return rows.filter((row) => ["LOW", "OUT", "NEGATIVE"].includes(row.stockStatus ?? ""));
    case "OPEN":
      return rows.filter(hasOpenBottle);
    default:
      return rows.filter((row) => (row.productClass ?? "GENERAL") === chip);
  }
};

/** Points of a 76x24 sparkline for a 7 day series; flat line when nothing was issued. */
export const sparklinePoints = (series: number[] | undefined, width = 72, height = 22): string => {
  const values = series && series.length > 1 ? series : [];
  if (values.length === 0) return "";
  const max = Math.max(...values);
  const step = width / (values.length - 1);
  return values
    .map((value, index) => {
      const y = max > 0 ? height - (value / max) * (height - 4) : height - 2;
      return `${(index * step).toFixed(1)},${y.toFixed(1)}`;
    })
    .join(" ");
};

export const weeklyIssued = (series: number[] | undefined): number =>
  Math.round((series ?? []).reduce((sum, value) => sum + value, 0) * 1000) / 1000;

export interface VisionApplication {
  /** The open bottle's level as a fraction, 0 to 1, rounded to the nearest 0.01. */
  fraction: number;
  sealed: number;
  total: number;
  remainingMl: number;
  consumedMl: number;
}

/** What a confirmed reading means for the shelf count: the sealed count stays as counted, the open bottle takes the level. */
export const applyVisionReading = (vision: Pick<SilaBottleVision, "fillRatio" | "nominalMl">, sealedCount: number): VisionApplication | null => {
  if (vision.fillRatio == null || !Number.isFinite(vision.fillRatio)) return null;
  const fraction = Math.round(Math.min(1, Math.max(0, vision.fillRatio)) * 100) / 100;
  return {
    fraction,
    sealed: Math.max(0, Math.floor(sealedCount)),
    total: composeQty(sealedCount, fraction),
    remainingMl: Math.round(vision.nominalMl * fraction),
    consumedMl: Math.round(vision.nominalMl * (1 - fraction)),
  };
};

/** Ml consumed and left for a given fill ratio and pack size, used by the manual slider in the Vision modal. */
export const mlFromFill = (fillRatio: number, nominalMl: number): { remainingMl: number; consumedMl: number } => {
  const fill = Math.min(1, Math.max(0, fillRatio));
  return { remainingMl: Math.round(nominalMl * fill), consumedMl: Math.round(nominalMl * (1 - fill)) };
};

export const formatMoney = (value: number | null | undefined, currency = "AED"): string =>
  value == null ? "—" : `${currency} ${value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
