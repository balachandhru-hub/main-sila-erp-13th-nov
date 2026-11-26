import type {
  CulinaryIngredientLine,
  CulinaryInstructionStep,
  CulinaryRecipeDiffItem,
  CulinaryScaleV2Result,
  CulinaryScaleV2ResultItem,
  CulinaryStructuredRecipePayload,
} from "../../../../api/silaMe/silaCulinaryRecipesApi";

export const BUTTER_CHICKEN_TEST_INGREDIENTS: CulinaryIngredientLine[] = [
  {
    temp_ref: "ING-01",
    description: "Boneless chicken",
    normalized_description: "boneless chicken",
    category: "PROTEIN",
    ingredient_form: "boneless",
    brand_preference: null,
    quantity: 1.8,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 1.8,
    edible_quantity: 1.8,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "Cut into bite-sized pieces",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-02",
    description: "Tomato purée",
    normalized_description: "tomato puree",
    category: "PRODUCE",
    ingredient_form: "puree",
    brand_preference: null,
    quantity: 1.2,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 1.2,
    edible_quantity: 1.2,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-03",
    description: "Butter",
    normalized_description: "butter",
    category: "DAIRY",
    ingredient_form: "unsalted",
    brand_preference: null,
    quantity: 0.25,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.25,
    edible_quantity: 0.25,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-04",
    description: "Fresh cream",
    normalized_description: "fresh cream",
    category: "DAIRY",
    ingredient_form: "liquid",
    brand_preference: null,
    quantity: 0.4,
    recipe_uom: "L",
    normalized_uom: "l",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.4,
    edible_quantity: 0.4,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-05",
    description: "Ginger garlic paste",
    normalized_description: "ginger garlic paste",
    category: "AROMATICS",
    ingredient_form: "paste",
    brand_preference: null,
    quantity: 0.12,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.12,
    edible_quantity: 0.12,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-06",
    description: "Yogurt",
    normalized_description: "yogurt",
    category: "DAIRY",
    ingredient_form: "plain",
    brand_preference: null,
    quantity: 0.3,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.3,
    edible_quantity: 0.3,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-07",
    description: "Garam masala",
    normalized_description: "garam masala",
    category: "SPICE",
    ingredient_form: "powder",
    brand_preference: null,
    quantity: 0.025,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.025,
    edible_quantity: 0.025,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-08",
    description: "Kashmiri chilli powder",
    normalized_description: "kashmiri chilli powder",
    category: "SPICE",
    ingredient_form: "powder",
    brand_preference: null,
    quantity: 0.02,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.02,
    edible_quantity: 0.02,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-09",
    description: "Kasuri methi",
    normalized_description: "kasuri methi",
    category: "HERB",
    ingredient_form: "dried",
    brand_preference: null,
    quantity: 0.01,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.01,
    edible_quantity: 0.01,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
  {
    temp_ref: "ING-10",
    description: "Salt",
    normalized_description: "salt",
    category: "SEASONING",
    ingredient_form: "fine",
    brand_preference: null,
    quantity: 0.025,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 0.025,
    edible_quantity: 0.025,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  },
];

const roundQty = (n: number): number => Math.round(n * 10000) / 10000;

export function scaleDraftLocally(params: {
  recipeName: string;
  baseServings: number;
  targetServings: number;
  baseServingSize: number;
  targetServingSize: number;
  prepBufferPercent: number;
  useNonlinearRules: boolean;
  ingredients: CulinaryIngredientLine[];
}): CulinaryScaleV2Result {
  const safeBaseServings = Math.max(0.001, params.baseServings || 1);
  const safeTargetServings = Math.max(0.001, params.targetServings || 1);
  const safeBaseSize = Math.max(0.001, params.baseServingSize || 1);
  const safeTargetSize = Math.max(0.001, params.targetServingSize || safeBaseSize);
  const linearFactor = (safeTargetServings / safeBaseServings) * (safeTargetSize / safeBaseSize);
  const bufferFactor = 1 + Math.max(0, params.prepBufferPercent || 0) / 100;

  const items: CulinaryScaleV2ResultItem[] = params.ingredients.map((line) => {
    const method = (line.scaling_method || "LINEAR").toUpperCase();
    const basis = (line.quantity_basis || "RAW").toUpperCase();
    const yieldPct = Math.min(100, Math.max(1, line.yield_percent || 100));

    let effectiveFactor = linearFactor;
    let requiresManualReview = method === "MANUAL_REVIEW";
    let note = "";

    if (method === "FIXED_QUANTITY") {
      effectiveFactor = 1;
      note = "Fixed quantity held constant across portion scaling.";
    } else if (method === "BATCH_BASED") {
      effectiveFactor = Math.max(1, Math.ceil(linearFactor));
      note = `Batch-based scaling rounded up to ${effectiveFactor} batch(es).`;
    } else if (method === "CHEF_DEFINED_FACTOR" || method === "CHEF_DEFINED_CURVE") {
      const cf = line.chef_scaling_factor ?? 1;
      effectiveFactor = Math.pow(linearFactor, cf);
      note = `Chef-defined exponent factor (${cf}).`;
    } else if (
      params.useNonlinearRules &&
      ["SPICE", "SEASONING", "LEAVENING", "THICKENER"].includes((line.category || "").toUpperCase())
    ) {
      if (linearFactor > 2) {
        requiresManualReview = true;
        note = "Spice/seasoning scaled linearly as baseline; chef taste verification recommended above 2x.";
      }
    }

    const scaledBase = line.quantity * effectiveFactor * bufferFactor;
    const scaledEdible = basis === "EDIBLE" ? scaledBase : scaledBase * (yieldPct / 100);
    const scaledRaw = basis === "EDIBLE" ? scaledBase / (yieldPct / 100) : scaledBase;

    return {
      temp_ref: line.temp_ref,
      description: line.description,
      uom: line.recipe_uom,
      scaling_method: method,
      quantity_basis: basis,
      base_qty: roundQty(line.quantity),
      scaled_edible_qty: roundQty(scaledEdible),
      scaled_raw_qty: roundQty(scaledRaw),
      yield_percent: yieldPct,
      requires_manual_review: requiresManualReview,
      note,
    };
  });

  return {
    recipe_name: params.recipeName,
    base_servings: params.baseServings,
    target_servings: params.targetServings,
    linear_factor: roundQty(linearFactor),
    ingredients: items,
  };
}

export function compareRecipePayloads(
  before: CulinaryStructuredRecipePayload,
  after: CulinaryStructuredRecipePayload
): CulinaryRecipeDiffItem[] {
  const diffs: CulinaryRecipeDiffItem[] = [];
  if (before.header.proposed_name !== after.header.proposed_name) {
    diffs.push({
      section: "Header",
      field: "Recipe Name",
      before: before.header.proposed_name,
      after: after.header.proposed_name,
    });
  }
  if (before.header.base_servings !== after.header.base_servings) {
    diffs.push({
      section: "Header",
      field: "Base Servings",
      before: String(before.header.base_servings),
      after: String(after.header.base_servings),
    });
  }
  if (before.header.cuisine !== after.header.cuisine) {
    diffs.push({
      section: "Header",
      field: "Cuisine",
      before: before.header.cuisine,
      after: after.header.cuisine,
    });
  }
  if (before.ingredients.length !== after.ingredients.length) {
    diffs.push({
      section: "Ingredients",
      field: "Ingredient Count",
      before: String(before.ingredients.length),
      after: String(after.ingredients.length),
    });
  }
  const beforeMap = new Map(before.ingredients.map((i) => [i.temp_ref, i]));
  for (const ing of after.ingredients) {
    const prev = beforeMap.get(ing.temp_ref);
    if (!prev) {
      diffs.push({
        section: "Ingredients",
        field: `Added ${ing.description}`,
        before: "—",
        after: `${ing.quantity} ${ing.recipe_uom}`,
      });
    } else if (
      prev.description !== ing.description ||
      prev.quantity !== ing.quantity ||
      prev.recipe_uom !== ing.recipe_uom
    ) {
      diffs.push({
        section: "Ingredients",
        field: ing.description,
        before: `${prev.description}: ${prev.quantity} ${prev.recipe_uom}`,
        after: `${ing.description}: ${ing.quantity} ${ing.recipe_uom}`,
      });
    }
  }
  for (const prev of before.ingredients) {
    if (!after.ingredients.some((i) => i.temp_ref === prev.temp_ref)) {
      diffs.push({
        section: "Ingredients",
        field: `Removed ${prev.description}`,
        before: `${prev.quantity} ${prev.recipe_uom}`,
        after: "Removed",
      });
    }
  }
  if (before.instructions.length !== after.instructions.length) {
    diffs.push({
      section: "Instructions",
      field: "Step Count",
      before: String(before.instructions.length),
      after: String(after.instructions.length),
    });
  }
  if (before.sub_recipes.length !== after.sub_recipes.length) {
    diffs.push({
      section: "Sub-Recipes",
      field: "Sub-Recipe Count",
      before: String(before.sub_recipes.length),
      after: String(after.sub_recipes.length),
    });
  }
  return diffs;
}

export function createBlankIngredient(nextIndex: number): CulinaryIngredientLine {
  return {
    temp_ref: `ING-${String(nextIndex).padStart(2, "0")}`,
    description: "",
    normalized_description: "",
    category: "OTHER",
    ingredient_form: "",
    brand_preference: null,
    quantity: 1,
    recipe_uom: "KG",
    normalized_uom: "kg",
    uom_resolved: true,
    quantity_basis: "RAW",
    raw_quantity: 1,
    edible_quantity: 1,
    yield_percent: 100,
    yield_source: "CHEF_CONFIRMED",
    prep_loss_percent: 0,
    cook_loss_percent: 0,
    scaling_method: "LINEAR",
    chef_scaling_factor: null,
    optional_indicator: false,
    preparation_notes: "",
    suggested_alternatives: [],
    material_mapping_status: "PENDING_MATERIAL_MAPPING",
    material_reference: null,
    provenance: "CHEF_EDITED",
    locked: false,
  };
}

export function createBlankStep(nextStepNumber: number): CulinaryInstructionStep {
  return {
    step_number: nextStepNumber,
    step_title: `Step ${nextStepNumber}`,
    instruction: "",
    estimated_duration_minutes: 5,
    equipment_suggestion: "",
    dependency_step: nextStepNumber > 1 ? nextStepNumber - 1 : null,
    quality_checkpoint: "",
    food_safety_verification: "",
    provenance: "CHEF_EDITED",
    locked: false,
  };
}

export function triggerBlobDownload(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
