import type { SilaIngredientOption } from "../../../api/silaMe/silaRecipeToolsApi";
import {
  lineCost,
  materialFromOption,
  nextKey,
  toNumber,
  totalCostAt,
  type HeaderValues,
  type IngredientLine,
  type IngredientMaterial,
} from "./recipeEditorModel";

const RECENT_KEY = "sila.recipeBuilder.recentMaterials";
const MAX_RECENT = 12;
const MAX_UNDO = 40;

export interface BuilderSnapshot {
  lines: IngredientLine[];
  servingQty: string;
}

export interface RecipeChangeSummary {
  added: string[];
  removed: string[];
  replaced: string[];
  quantityChanged: string[];
  uomChanged: string[];
  totalCostBefore: number | null;
  totalCostAfter: number | null;
  costPerPortionBefore: number | null;
  costPerPortionAfter: number | null;
}

const cloneLines = (lines: IngredientLine[]): IngredientLine[] =>
  lines.map((line) => ({
    ...line,
    material: line.material ? { ...line.material, conversions: [...line.material.conversions], outletPrices: line.material.outletPrices ? [...line.material.outletPrices] : undefined } : undefined,
    subRecipe: line.subRecipe ? { ...line.subRecipe } : undefined,
  }));

export const snapshotOf = (lines: IngredientLine[], servingQty: string): BuilderSnapshot => ({
  lines: cloneLines(lines),
  servingQty,
});

export const pushUndo = (stack: BuilderSnapshot[], snap: BuilderSnapshot): BuilderSnapshot[] =>
  [...stack, snap].slice(-MAX_UNDO);

export const lineLabel = (line: IngredientLine): string => {
  if (line.material) return `${line.material.materialCode} ${line.material.description}`.trim();
  if (line.subRecipe) return `${line.subRecipe.recipeCode} ${line.subRecipe.name}`.trim();
  return "Ingredient";
};

export const materialLine = (option: SilaIngredientOption, quantity = "1"): IngredientLine => {
  const material = materialFromOption(option);
  return { key: nextKey(), material, quantity, uom: material.baseUom.toUpperCase() };
};

export const duplicateLine = (line: IngredientLine): IngredientLine => ({
  ...cloneLines([line])[0],
  key: nextKey(),
  ingredientCode: undefined,
});

export const replaceMaterialOnLine = (
  line: IngredientLine,
  option: SilaIngredientOption,
  quantity: string,
  uom: string,
): IngredientLine => ({
  ...line,
  material: materialFromOption(option),
  subRecipe: undefined,
  quantity,
  uom: uom.toUpperCase(),
});

/** Scale ingredient quantities by newPortions / oldPortions (draft only). */
export const scaleLines = (lines: IngredientLine[], fromPortions: number, toPortions: number): IngredientLine[] => {
  if (!(fromPortions > 0) || !(toPortions > 0) || fromPortions === toPortions) return cloneLines(lines);
  const factor = toPortions / fromPortions;
  return lines.map((line) => {
    const qty = toNumber(line.quantity);
    if (!(qty > 0)) return { ...line };
    const scaled = qty * factor;
    const text = Number.isInteger(scaled) ? String(scaled) : String(Number(scaled.toFixed(4)));
    return { ...line, quantity: text };
  });
};

export const costPerPortion = (total: number | null, portions: number): number | null =>
  total == null ? null : portions > 0 ? total / portions : total;

export const summarizeChanges = (
  before: BuilderSnapshot,
  after: BuilderSnapshot,
  outletId = "",
): RecipeChangeSummary => {
  const beforeKeys = new Map(before.lines.map((line) => [line.key, line]));
  const afterKeys = new Map(after.lines.map((line) => [line.key, line]));
  const added: string[] = [];
  const removed: string[] = [];
  const replaced: string[] = [];
  const quantityChanged: string[] = [];
  const uomChanged: string[] = [];

  after.lines.forEach((line) => {
    const prior = beforeKeys.get(line.key);
    if (!prior) {
      added.push(lineLabel(line));
      return;
    }
    const priorId = prior.material?.id ?? prior.subRecipe?.id ?? "";
    const nextId = line.material?.id ?? line.subRecipe?.id ?? "";
    if (priorId && nextId && priorId !== nextId) replaced.push(`${lineLabel(prior)} → ${lineLabel(line)}`);
    if (prior.quantity !== line.quantity) quantityChanged.push(lineLabel(line));
    if (prior.uom.toUpperCase() !== line.uom.toUpperCase()) uomChanged.push(lineLabel(line));
  });
  before.lines.forEach((line) => {
    if (!afterKeys.has(line.key)) removed.push(lineLabel(line));
  });

  const totalBefore = totalCostAt(before.lines, outletId);
  const totalAfter = totalCostAt(after.lines, outletId);
  return {
    added,
    removed,
    replaced,
    quantityChanged,
    uomChanged,
    totalCostBefore: totalBefore,
    totalCostAfter: totalAfter,
    costPerPortionBefore: costPerPortion(totalBefore, toNumber(before.servingQty)),
    costPerPortionAfter: costPerPortion(totalAfter, toNumber(after.servingQty)),
  };
};

export const builderWarnings = (lines: IngredientLine[], outletId = ""): string[] => {
  const warnings: string[] = [];
  const seen = new Map<string, number>();
  lines.forEach((line) => {
    const cost = lineCost(line, outletId);
    const label = lineLabel(line);
    if (cost.issue === "Price missing" || cost.priceStatus === "MISSING") warnings.push(`${label}: COST PENDING`);
    else if (cost.priceStatus === "PENDING_APPROVAL") warnings.push(`${label}: material price pending approval`);
    if (cost.issue?.includes("UOM conversion")) warnings.push(`${label}: missing UOM conversion`);
    const id = line.material?.id ?? line.subRecipe?.id;
    if (id) seen.set(id, (seen.get(id) ?? 0) + 1);
  });
  seen.forEach((count, id) => {
    if (count > 1) {
      const line = lines.find((x) => (x.material?.id ?? x.subRecipe?.id) === id);
      if (line) warnings.push(`${lineLabel(line)}: duplicate ingredient`);
    }
  });
  return warnings;
};

export const loadRecentMaterials = (): string[] => {
  try {
    const raw = localStorage.getItem(RECENT_KEY);
    const parsed = raw ? (JSON.parse(raw) as unknown) : [];
    return Array.isArray(parsed) ? parsed.filter((x): x is string => typeof x === "string") : [];
  } catch {
    return [];
  }
};

export const rememberMaterial = (materialId: string): void => {
  const next = [materialId, ...loadRecentMaterials().filter((id) => id.toLowerCase() !== materialId.toLowerCase())].slice(0, MAX_RECENT);
  try {
    localStorage.setItem(RECENT_KEY, JSON.stringify(next));
  } catch {
    /* ignore quota */
  }
};

export const materialInitials = (material: Pick<IngredientMaterial, "materialCode" | "description">): string => {
  const source = material.description?.trim() || material.materialCode;
  const parts = source.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) return `${parts[0][0] ?? ""}${parts[1][0] ?? ""}`.toUpperCase();
  return source.slice(0, 2).toUpperCase();
};

export const snapshotsEqual = (a: BuilderSnapshot, b: BuilderSnapshot): boolean =>
  a.servingQty === b.servingQty
  && a.lines.length === b.lines.length
  && a.lines.every((line, index) => {
    const other = b.lines[index];
    return (
      line.key === other.key
      && line.quantity === other.quantity
      && line.uom === other.uom
      && (line.material?.id ?? "") === (other.material?.id ?? "")
      && (line.subRecipe?.id ?? "") === (other.subRecipe?.id ?? "")
    );
  });

export type { HeaderValues };
