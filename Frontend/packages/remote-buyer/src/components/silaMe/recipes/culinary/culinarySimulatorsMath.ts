/**
 * Deterministic Phase 0 demonstration calculation engines.
 * Mirrors Backend/sila-culinary-engine/app/engines/{scaling,mrp,pnl}.py
 * for instantaneous interactive client-side sliders without touching production DB/ERP.
 */

export type ScalingRule =
  | "LINEAR"
  | "SPICE_NONLINEAR"
  | "SEASONING_STEPPED"
  | "LIQUID_EVAPORATION"
  | "FIXED_BATCH";

export interface DemoRecipeIngredient {
  id: string;
  name: string;
  category: string;
  baseQuantity: number;
  uom: string;
  unitCostAed: number;
  yieldPercent: number;
  scalingRule: ScalingRule;
  matchStatus: "EXACT_MATCH" | "SUGGESTED_MATCH" | "UNMATCHED";
  demoMaterialRef: string | null;
  allergenHint?: string;
}

export interface RecipeScalingInput {
  baseServings: number;
  targetServings: number;
  baseServingSizeGrams: number;
  targetServingSizeGrams: number;
  prepBufferPercent: number;
  wasteAllowancePercent: number;
  yieldOverridePercent?: number | null;
  ingredients: DemoRecipeIngredient[];
}

export interface ScaledIngredientResult {
  id: string;
  name: string;
  category: string;
  uom: string;
  scalingRule: ScalingRule;
  matchStatus: DemoRecipeIngredient["matchStatus"];
  demoMaterialRef: string | null;
  allergenHint?: string;
  baseQuantity: number;
  scaledQuantity: number;
  grossQuantityWithBuffers: number;
  unitCostAed: number;
  baseExtendedCostAed: number;
  scaledExtendedCostAed: number;
}

export interface RecipeScalingResult {
  scaleFactor: number;
  baseTotalCostAed: number;
  scaledTotalCostAed: number;
  baseCostPerServingAed: number;
  scaledCostPerServingAed: number;
  unmatchedCount: number;
  items: ScaledIngredientResult[];
}

const round4 = (n: number) => Math.round((n + Number.EPSILON) * 10000) / 10000;
const round2 = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100;

export const DEMO_SALMON_RECIPE = {
  title: "Grilled Salmon with Lemon Butter Sauce",
  cuisine: "International / Modern European",
  category: "Main Course",
  recipeType: "RECIPE",
  baseServings: 10,
  baseServingSizeGrams: 180,
  prepMinutes: 20,
  cookMinutes: 18,
  currency: "AED",
  targetSellingPriceAed: 120,
  ingredients: [
    {
      id: "ing-salmon",
      name: "Salmon fillet",
      category: "Seafood",
      baseQuantity: 1.8,
      uom: "kg",
      unitCostAed: 85,
      yieldPercent: 90,
      scalingRule: "LINEAR",
      matchStatus: "EXACT_MATCH",
      demoMaterialRef: "DEMO-MAT-1001",
      allergenHint: "Fish",
    },
    {
      id: "ing-butter",
      name: "Unsalted butter",
      category: "Dairy",
      baseQuantity: 0.25,
      uom: "kg",
      unitCostAed: 32,
      yieldPercent: 100,
      scalingRule: "LINEAR",
      matchStatus: "EXACT_MATCH",
      demoMaterialRef: "DEMO-MAT-1002",
      allergenHint: "Milk",
    },
    {
      id: "ing-lemon",
      name: "Lemon",
      category: "Produce",
      baseQuantity: 0.4,
      uom: "kg",
      unitCostAed: 8,
      yieldPercent: 85,
      scalingRule: "LINEAR",
      matchStatus: "EXACT_MATCH",
      demoMaterialRef: "DEMO-MAT-1003",
    },
    {
      id: "ing-garlic",
      name: "Garlic",
      category: "Aromatics",
      baseQuantity: 0.06,
      uom: "kg",
      unitCostAed: 15,
      yieldPercent: 92,
      scalingRule: "SPICE_NONLINEAR",
      matchStatus: "EXACT_MATCH",
      demoMaterialRef: "DEMO-MAT-1004",
    },
    {
      id: "ing-asparagus",
      name: "Asparagus",
      category: "Produce",
      baseQuantity: 1.0,
      uom: "kg",
      unitCostAed: 28,
      yieldPercent: 88,
      scalingRule: "LINEAR",
      matchStatus: "EXACT_MATCH",
      demoMaterialRef: "DEMO-MAT-1005",
    },
    {
      id: "ing-olive-oil",
      name: "Olive oil",
      category: "Oils & Liquids",
      baseQuantity: 0.15,
      uom: "L",
      unitCostAed: 35,
      yieldPercent: 100,
      scalingRule: "LIQUID_EVAPORATION",
      matchStatus: "SUGGESTED_MATCH",
      demoMaterialRef: "DEMO-MAT-1006",
    },
    {
      id: "ing-salt",
      name: "Sea salt",
      category: "Seasoning",
      baseQuantity: 0.025,
      uom: "kg",
      unitCostAed: 5,
      yieldPercent: 100,
      scalingRule: "SEASONING_STEPPED",
      matchStatus: "EXACT_MATCH",
      demoMaterialRef: "DEMO-MAT-1007",
    },
    {
      id: "ing-dill",
      name: "Fresh dill",
      category: "Fresh Herbs",
      baseQuantity: 0.05,
      uom: "kg",
      unitCostAed: 45,
      yieldPercent: 80,
      scalingRule: "SPICE_NONLINEAR",
      matchStatus: "UNMATCHED",
      demoMaterialRef: null,
    },
  ] as DemoRecipeIngredient[],
};

export function effectiveScaleFactor(rawFactor: number, rule: ScalingRule): number {
  if (rawFactor <= 0) return 0;
  switch (rule) {
    case "FIXED_BATCH":
      return 1;
    case "SPICE_NONLINEAR":
      return rawFactor > 1 ? Math.pow(rawFactor, 0.85) : Math.pow(rawFactor, 0.9);
    case "SEASONING_STEPPED":
      return rawFactor > 1 ? Math.pow(rawFactor, 0.8) : Math.pow(rawFactor, 0.9);
    case "LIQUID_EVAPORATION":
      return rawFactor > 1 ? Math.pow(rawFactor, 0.92) : Math.pow(rawFactor, 0.95);
    case "LINEAR":
    default:
      return rawFactor;
  }
}

export function calculateRecipeScaling(input: RecipeScalingInput): RecipeScalingResult {
  const baseServings = Math.max(1, input.baseServings);
  const targetServings = Math.max(1, input.targetServings);
  const baseSize = Math.max(1, input.baseServingSizeGrams);
  const targetSize = Math.max(1, input.targetServingSizeGrams);

  const rawFactor = (targetServings / baseServings) * (targetSize / baseSize);
  const bufferMultiplier = 1 + Math.max(0, input.prepBufferPercent) / 100 + Math.max(0, input.wasteAllowancePercent) / 100;

  let baseTotalCostAed = 0;
  let scaledTotalCostAed = 0;
  let unmatchedCount = 0;

  const items: ScaledIngredientResult[] = input.ingredients.map((ing) => {
    const ruleFactor = effectiveScaleFactor(rawFactor, ing.scalingRule);
    const scaledQuantity = ing.baseQuantity * ruleFactor;
    const yieldPct = input.yieldOverridePercent && input.yieldOverridePercent > 0 ? input.yieldOverridePercent : ing.yieldPercent;
    const yieldAdjustment = yieldPct < 100 ? ing.yieldPercent / yieldPct : 1;
    const grossQuantityWithBuffers = scaledQuantity * bufferMultiplier * yieldAdjustment;

    const baseExtendedCostAed = ing.baseQuantity * ing.unitCostAed;
    const scaledExtendedCostAed = grossQuantityWithBuffers * ing.unitCostAed;

    baseTotalCostAed += baseExtendedCostAed;
    scaledTotalCostAed += scaledExtendedCostAed;
    if (ing.matchStatus === "UNMATCHED") unmatchedCount += 1;

    return {
      id: ing.id,
      name: ing.name,
      category: ing.category,
      uom: ing.uom,
      scalingRule: ing.scalingRule,
      matchStatus: ing.matchStatus,
      demoMaterialRef: ing.demoMaterialRef,
      allergenHint: ing.allergenHint,
      baseQuantity: round4(ing.baseQuantity),
      scaledQuantity: round4(scaledQuantity),
      grossQuantityWithBuffers: round4(grossQuantityWithBuffers),
      unitCostAed: round2(ing.unitCostAed),
      baseExtendedCostAed: round2(baseExtendedCostAed),
      scaledExtendedCostAed: round2(scaledExtendedCostAed),
    };
  });

  return {
    scaleFactor: round4(rawFactor),
    baseTotalCostAed: round2(baseTotalCostAed),
    scaledTotalCostAed: round2(scaledTotalCostAed),
    baseCostPerServingAed: round2(baseTotalCostAed / baseServings),
    scaledCostPerServingAed: round2(scaledTotalCostAed / targetServings),
    unmatchedCount,
    items,
  };
}

// ---------------------------------------------------------------------------
// Interactive Event MRP Simulator (Section H, §34–37)
// ---------------------------------------------------------------------------

export interface DemoMrpMaterialRow {
  id: string;
  materialCode: string | null;
  materialName: string;
  materialGroup: string;
  recipeRef: string;
  menuItemRef: string;
  baseUom: string;
  purchasingUom: string;
  packSize: number;
  uptakePercent: number;
  ediblePortionGrams: number;
  prepBufferPercent: number;
  usableYieldPercent: number;
  existingPreparedQty: number;
  availableStock: number;
  altStoreStock: number;
  existingReservations: number;
  incomingPoQty: number;
  incomingTransferQty: number;
  safetyStock: number;
  unitCostAed: number;
  supplierName: string;
  materialExists: boolean;
}

export interface ComputedMrpRow extends DemoMrpMaterialRow {
  expectedPortions: number;
  edibleQty: number;
  bufferedEdibleQty: number;
  rawRequiredQty: number;
  netAvailableStock: number;
  projectedAvailableStock: number;
  shortageQty: number;
  recommendedTransferQty: number;
  recommendedPurchaseQty: number;
  roundedOrderQty: number;
  excessAfterRounding: number;
  estimatedProcurementCostAed: number;
  status: "OK" | "TRANSFER_READY" | "SHORTAGE" | "MATERIAL_CREATION_REQUIRED";
}

export const DEMO_EVENT_MRP_ROWS: DemoMrpMaterialRow[] = [
  {
    id: "mrp-salmon",
    materialCode: "DEMO-MAT-1001",
    materialName: "Atlantic Salmon Fillet",
    materialGroup: "Seafood",
    recipeRef: "Grilled Salmon with Lemon Butter",
    menuItemRef: "Main Course · Grilled Salmon",
    baseUom: "kg",
    purchasingUom: "Case (5 kg)",
    packSize: 5,
    uptakePercent: 60,
    ediblePortionGrams: 180,
    prepBufferPercent: 10,
    usableYieldPercent: 90,
    existingPreparedQty: 0,
    availableStock: 25,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 10,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 85,
    supplierName: "Gulf Marine Foods (Demo)",
    materialExists: true,
  },
  {
    id: "mrp-chicken",
    materialCode: "DEMO-MAT-2004",
    materialName: "Whole Roast Chicken",
    materialGroup: "Poultry",
    recipeRef: "Herb Roast Chicken",
    menuItemRef: "Main Course · Roast Chicken",
    baseUom: "kg",
    purchasingUom: "Carton (10 kg)",
    packSize: 10,
    uptakePercent: 50,
    ediblePortionGrams: 300,
    prepBufferPercent: 8,
    usableYieldPercent: 81,
    existingPreparedQty: 0,
    availableStock: 60,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 40,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 24,
    supplierName: "Al Ain Poultry Supply (Demo)",
    materialExists: true,
  },
  {
    id: "mrp-rice",
    materialCode: "DEMO-MAT-3012",
    materialName: "Aged Basmati Rice",
    materialGroup: "Dry Goods",
    recipeRef: "Vegetable Biryani",
    menuItemRef: "Main Course · Vegetable Biryani",
    baseUom: "kg",
    purchasingUom: "Bag (25 kg)",
    packSize: 25,
    uptakePercent: 50,
    ediblePortionGrams: 270,
    prepBufferPercent: 5,
    usableYieldPercent: 94.5,
    existingPreparedQty: 0,
    availableStock: 100,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 0,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 12,
    supplierName: "Emirates Dry Stores (Demo)",
    materialExists: true,
  },
  {
    id: "mrp-butter",
    materialCode: "DEMO-MAT-1002",
    materialName: "Unsalted Butter",
    materialGroup: "Dairy",
    recipeRef: "Lemon Butter Sauce & Mash",
    menuItemRef: "Multiple Main Courses",
    baseUom: "kg",
    purchasingUom: "Block (5 kg)",
    packSize: 5,
    uptakePercent: 100,
    ediblePortionGrams: 36,
    prepBufferPercent: 10,
    usableYieldPercent: 99,
    existingPreparedQty: 0,
    availableStock: 12,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 0,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 32,
    supplierName: "Oasis Dairy Traders (Demo)",
    materialExists: true,
  },
  {
    id: "mrp-olive-oil",
    materialCode: "DEMO-MAT-1006",
    materialName: "Extra Virgin Olive Oil",
    materialGroup: "Oils & Condiments",
    recipeRef: "Caesar Salad / Mezze / Salmon",
    menuItemRef: "Starters & Main Courses",
    baseUom: "L",
    purchasingUom: "Tin (5 L)",
    packSize: 5,
    uptakePercent: 100,
    ediblePortionGrams: 30,
    prepBufferPercent: 0,
    usableYieldPercent: 100,
    existingPreparedQty: 0,
    availableStock: 10,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 5,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 35,
    supplierName: "Mediterranean Pantry (Demo)",
    materialExists: true,
  },
  {
    id: "mrp-asparagus",
    materialCode: "DEMO-MAT-1005",
    materialName: "Green Asparagus Spears",
    materialGroup: "Fresh Produce",
    recipeRef: "Grilled Salmon with Asparagus",
    menuItemRef: "Main Course · Grilled Salmon",
    baseUom: "kg",
    purchasingUom: "Crate (5 kg)",
    packSize: 5,
    uptakePercent: 60,
    ediblePortionGrams: 120,
    prepBufferPercent: 10,
    usableYieldPercent: 88,
    existingPreparedQty: 0,
    availableStock: 15,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 0,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 28,
    supplierName: "Fresh Harvest Produce (Demo)",
    materialExists: true,
  },
  {
    id: "mrp-dill",
    materialCode: null,
    materialName: "Fresh Dill",
    materialGroup: "Fresh Herbs",
    recipeRef: "Lemon Butter Sauce",
    menuItemRef: "Main Course · Grilled Salmon",
    baseUom: "kg",
    purchasingUom: "Pack (1 kg)",
    packSize: 1,
    uptakePercent: 60,
    ediblePortionGrams: 8,
    prepBufferPercent: 10,
    usableYieldPercent: 88,
    existingPreparedQty: 0,
    availableStock: 0,
    altStoreStock: 0,
    existingReservations: 0,
    incomingPoQty: 0,
    incomingTransferQty: 0,
    safetyStock: 0,
    unitCostAed: 45,
    supplierName: "Pending Material Creation",
    materialExists: false,
  },
];

export function computeMrpRow(guestCount: number, row: DemoMrpMaterialRow): ComputedMrpRow {
  const guests = Math.max(0, guestCount);
  const expectedPortions = guests * (row.uptakePercent / 100);
  const edibleQty = (expectedPortions * row.ediblePortionGrams) / 1000;
  const bufferedEdibleQty = edibleQty * (1 + row.prepBufferPercent / 100);
  const yieldRatio = Math.max(0.01, row.usableYieldPercent / 100);
  const grossRaw = bufferedEdibleQty / yieldRatio;
  const rawRequiredQty = Math.max(0, grossRaw - row.existingPreparedQty);

  const netAvailableStock = Math.max(0, row.availableStock - row.existingReservations - row.safetyStock);
  const projectedAvailableStock = netAvailableStock + row.incomingPoQty + row.incomingTransferQty;
  const shortageQty = Math.max(0, rawRequiredQty - projectedAvailableStock);

  const recommendedTransferQty = row.materialExists
    ? Math.min(shortageQty, Math.max(0, row.altStoreStock))
    : 0;
  const recommendedPurchaseQty = Math.max(0, shortageQty - recommendedTransferQty);

  let roundedOrderQty = 0;
  let excessAfterRounding = 0;
  if (recommendedPurchaseQty > 0 && row.materialExists) {
    const pack = Math.max(0.001, row.packSize);
    const packs = Math.ceil(recommendedPurchaseQty / pack);
    roundedOrderQty = packs * pack;
    excessAfterRounding = Math.max(0, roundedOrderQty - recommendedPurchaseQty);
  }

  const estimatedProcurementCostAed = row.materialExists ? roundedOrderQty * row.unitCostAed : 0;

  let status: ComputedMrpRow["status"] = "OK";
  if (!row.materialExists) {
    status = "MATERIAL_CREATION_REQUIRED";
  } else if (recommendedPurchaseQty > 0) {
    status = "SHORTAGE";
  } else if (recommendedTransferQty > 0) {
    status = "TRANSFER_READY";
  }

  return {
    ...row,
    expectedPortions: round2(expectedPortions),
    edibleQty: round2(edibleQty),
    bufferedEdibleQty: round2(bufferedEdibleQty),
    rawRequiredQty: round2(rawRequiredQty),
    netAvailableStock: round2(netAvailableStock),
    projectedAvailableStock: round2(projectedAvailableStock),
    shortageQty: round2(shortageQty),
    recommendedTransferQty: round2(recommendedTransferQty),
    recommendedPurchaseQty: round2(recommendedPurchaseQty),
    roundedOrderQty: round2(roundedOrderQty),
    excessAfterRounding: round2(excessAfterRounding),
    estimatedProcurementCostAed: round2(estimatedProcurementCostAed),
    status,
  };
}

// ---------------------------------------------------------------------------
// Interactive Event P&L & What-If Simulator (Section J, §50–51)
// ---------------------------------------------------------------------------

export interface EventPnlInput {
  guestCount: number;
  guaranteedGuests: number;
  sellingPricePerGuestAed: number;
  fixedRevenueAed: number;
  foodCostPerGuestAed: number;
  beverageCostPerGuestAed: number;
  otherVariableCostPerGuestAed: number;
  manpowerCostAed: number;
  fixedEventExpensesAed: number;
  discountPercent: number;
  targetMarginPercent: number;
}

export interface EventPnlResult {
  grossRevenueAed: number;
  netRevenueAed: number;
  foodCostAed: number;
  beverageCostAed: number;
  otherVariableCostAed: number;
  totalFixedCostAed: number;
  totalEventCostAed: number;
  eventContributionAed: number;
  contributionMarginPercent: number;
  foodCostPercent: number;
  beverageCostPercent: number;
  costPerGuestAed: number;
  revenuePerGuestAed: number;
  contributionPerGuestAed: number;
  breakEvenGuests: number | null;
  requiredSellingPriceForTargetMarginAed: number | null;
  maxAllowableFoodCostAed: number;
}

export const DEMO_EVENT_PNL_BASE: EventPnlInput = {
  guestCount: 500,
  guaranteedGuests: 480,
  sellingPricePerGuestAed: 350,
  fixedRevenueAed: 0,
  foodCostPerGuestAed: 90,
  beverageCostPerGuestAed: 25,
  otherVariableCostPerGuestAed: 0,
  manpowerCostAed: 0,
  fixedEventExpensesAed: 35000,
  discountPercent: 0,
  targetMarginPercent: 45,
};

export function calculateEventPnl(input: EventPnlInput): EventPnlResult {
  const guests = Math.max(0, input.guestCount);
  const grossRevenueAed = guests * input.sellingPricePerGuestAed + input.fixedRevenueAed;
  const discountFactor = 1 - Math.max(0, Math.min(100, input.discountPercent)) / 100;
  const netRevenueAed = grossRevenueAed * discountFactor;

  const foodCostAed = guests * input.foodCostPerGuestAed;
  const beverageCostAed = guests * input.beverageCostPerGuestAed;
  const otherVariableCostAed = guests * input.otherVariableCostPerGuestAed;
  const totalFixedCostAed = input.manpowerCostAed + input.fixedEventExpensesAed;
  const totalEventCostAed = foodCostAed + beverageCostAed + otherVariableCostAed + totalFixedCostAed;

  const eventContributionAed = netRevenueAed - totalEventCostAed;
  const contributionMarginPercent = netRevenueAed > 0 ? (eventContributionAed / netRevenueAed) * 100 : 0;
  const foodCostPercent = netRevenueAed > 0 ? (foodCostAed / netRevenueAed) * 100 : 0;
  const beverageCostPercent = netRevenueAed > 0 ? (beverageCostAed / netRevenueAed) * 100 : 0;

  const costPerGuestAed = guests > 0 ? totalEventCostAed / guests : 0;
  const revenuePerGuestAed = guests > 0 ? netRevenueAed / guests : 0;
  const contributionPerGuestAed = guests > 0 ? eventContributionAed / guests : 0;

  const netPricePerGuest = input.sellingPricePerGuestAed * discountFactor;
  const varCostPerGuest = input.foodCostPerGuestAed + input.beverageCostPerGuestAed + input.otherVariableCostPerGuestAed;
  const unitContribution = netPricePerGuest - varCostPerGuest;
  const remainingFixed = Math.max(0, totalFixedCostAed - input.fixedRevenueAed * discountFactor);
  const breakEvenGuests = unitContribution > 0 ? Math.ceil(remainingFixed / unitContribution) : null;

  let requiredSellingPriceForTargetMarginAed: number | null = null;
  if (guests > 0 && input.targetMarginPercent < 100) {
    const targetRev = totalEventCostAed / (1 - input.targetMarginPercent / 100);
    requiredSellingPriceForTargetMarginAed = Math.max(0, (targetRev / discountFactor - input.fixedRevenueAed) / guests);
  }

  const targetContribution = netRevenueAed * (input.targetMarginPercent / 100);
  const maxAllowableFoodCostAed = Math.max(
    0,
    netRevenueAed - targetContribution - beverageCostAed - otherVariableCostAed - totalFixedCostAed
  );

  return {
    grossRevenueAed: round2(grossRevenueAed),
    netRevenueAed: round2(netRevenueAed),
    foodCostAed: round2(foodCostAed),
    beverageCostAed: round2(beverageCostAed),
    otherVariableCostAed: round2(otherVariableCostAed),
    totalFixedCostAed: round2(totalFixedCostAed),
    totalEventCostAed: round2(totalEventCostAed),
    eventContributionAed: round2(eventContributionAed),
    contributionMarginPercent: round2(contributionMarginPercent),
    foodCostPercent: round2(foodCostPercent),
    beverageCostPercent: round2(beverageCostPercent),
    costPerGuestAed: round2(costPerGuestAed),
    revenuePerGuestAed: round2(revenuePerGuestAed),
    contributionPerGuestAed: round2(contributionPerGuestAed),
    breakEvenGuests,
    requiredSellingPriceForTargetMarginAed:
      requiredSellingPriceForTargetMarginAed === null ? null : round2(requiredSellingPriceForTargetMarginAed),
    maxAllowableFoodCostAed: round2(maxAllowableFoodCostAed),
  };
}
