/** Minimal line shape for draft save (keeps this module free of axios-backed API imports). */
export interface StudioDraftLine {
  materialId?: string | null;
  subRecipeId?: string | null;
  quantity: string;
  uom: string;
}

export interface StudioDraftHeader {
  name: string;
  description?: string;
  familyId: string;
  categoryId: string;
  itemMode?: "DIRECT" | "RECIPE" | "BATCH";
  servingQty: string;
  servingUom: string;
  sellingUom?: string;
  currency?: string;
  /** POS identity — same as Linked menu item when both are set to the same value. */
  posCode?: string;
  posItem?: string;
}

export interface StudioCreateRequest {
  name: string;
  description: string | null;
  familyId: string | null;
  categoryId: string | null;
  itemMode: "DIRECT" | "RECIPE" | "BATCH";
  servingQty: number;
  servingUom: string;
  sellingUom: string;
  posCode: string | null;
  posItem: string | null;
  currency: string | null;
  ingredients: {
    materialId: string | null;
    subRecipeId: string | null;
    quantity: number;
    uom: string | null;
  }[];
  outletPrices: { outletLocationId: string; menuPrice: number; currency?: string | null }[];
}

export interface StudioSaveFieldErrors {
  name?: string;
  serving?: string;
  ingredients?: string;
  classification?: string;
}

const toNumber = (value: string): number => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
};

/** Client-side checks aligned with SilaRecipeRules.Validation (create draft). */
export const validateStudioDraft = (
  header: StudioDraftHeader,
  lines: StudioDraftLine[],
): StudioSaveFieldErrors => {
  const errors: StudioSaveFieldErrors = {};
  if (!header.name.trim()) errors.name = "Enter the recipe name.";
  if (!(toNumber(header.servingQty) > 0) || !header.servingUom.trim()) {
    errors.serving = "Enter a yield / portions greater than zero and a serving unit.";
  }
  if (lines.length === 0) {
    errors.ingredients = "Add at least one ingredient from the Material Catalog.";
  } else if (lines.some((line) => !(toNumber(line.quantity) > 0))) {
    errors.ingredients = "Enter a quantity greater than zero for every ingredient.";
  } else if (lines.some((line) => !line.materialId && !line.subRecipeId)) {
    errors.ingredients = "Each ingredient must reference a Material Master item.";
  } else if (lines.some((line) => !line.uom.trim())) {
    errors.ingredients = "Select a UOM for every ingredient.";
  }
  return errors;
};

export const buildStudioCreateRequest = (
  header: StudioDraftHeader,
  lines: StudioDraftLine[],
  outletPrices: { outletLocationId: string; menuPrice: number; currency?: string | null }[] = [],
): StudioCreateRequest => ({
  name: header.name.trim(),
  description: header.description?.trim() || null,
  familyId: header.familyId || null,
  categoryId: header.categoryId || null,
  itemMode: header.itemMode ?? "RECIPE",
  servingQty: toNumber(header.servingQty),
  servingUom: header.servingUom.trim() || "EA",
  sellingUom: (header.sellingUom ?? "").trim() || "EA",
  posCode: header.posCode?.trim() || null,
  posItem: header.posItem?.trim() || null,
  currency: header.currency?.trim() || null,
  ingredients: lines.map((line) => ({
    materialId: line.materialId ?? null,
    subRecipeId: line.subRecipeId ?? null,
    quantity: toNumber(line.quantity),
    uom: line.uom || null,
  })),
  outletPrices,
});

export interface StudioPersistCheck {
  ok: boolean;
  warning?: string;
}

export interface StudioPersistedIngredient {
  sequence: number;
  materialId?: string | null;
  quantity: number;
  uom?: string | null;
}

export interface StudioPersistedDetail {
  name: string;
  status: string;
  ingredients: StudioPersistedIngredient[];
}

/** Compare create payload to getRecipe detail after Save Draft. */
export const verifyPersistedDraft = (
  detail: StudioPersistedDetail,
  request: StudioCreateRequest,
): StudioPersistCheck => {
  const issues: string[] = [];
  if ((detail.name || "").trim() !== request.name.trim()) {
    issues.push("name mismatch");
  }
  if ((detail.status || "").toUpperCase() !== "DRAFT") {
    issues.push(`status is ${detail.status || "unknown"} (expected DRAFT)`);
  }
  const saved = [...(detail.ingredients ?? [])].sort((a, b) => a.sequence - b.sequence);
  if (saved.length !== request.ingredients.length) {
    issues.push(`ingredient count ${saved.length} vs ${request.ingredients.length}`);
  } else {
    request.ingredients.forEach((line, index) => {
      const row = saved[index];
      const materialId = (row.materialId || "").toLowerCase();
      const expectedId = (line.materialId || "").toLowerCase();
      if (expectedId && materialId !== expectedId) {
        issues.push(`ingredient ${index + 1} material id mismatch`);
      }
      if (Number(row.quantity) !== Number(line.quantity)) {
        issues.push(`ingredient ${index + 1} quantity mismatch`);
      }
      if ((row.uom || "").toUpperCase() !== (line.uom || "").toUpperCase()) {
        issues.push(`ingredient ${index + 1} UOM mismatch`);
      }
    });
  }
  if (issues.length === 0) return { ok: true };
  return { ok: false, warning: `Saved, but verification found: ${issues.join("; ")}.` };
};
