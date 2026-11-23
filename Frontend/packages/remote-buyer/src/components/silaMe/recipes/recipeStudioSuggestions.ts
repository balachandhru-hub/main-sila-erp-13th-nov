import type { SilaIngredientOption, SilaRecipeMaster } from "../../../api/silaMe/silaRecipeToolsApi";
import type { SilaProductClass, SilaRecipeFormat, SilaRecipeListItem, SilaRecipeMode } from "../../../api/silaMe/silaRecipeApi";
import type { MenuItemDetail } from "../../../api/silaMe/silaMenuEngineeringApi";
import type { SilaLocation } from "../../../api/silaMe/silaInventoryApi";
import { toNumber, type IngredientLine, type LineCost } from "./recipeEditorModel";
import { costPerServing } from "./recipeFormat";
import { loadRecentMaterials } from "./recipeBuilderState";
import { RECIPE_TEMPLATES, type RecipeTemplate } from "./recipeHospitality";
import { resolveStudioKind, type StudioVisualKind } from "./recipeStudioVisuals";

const TARGET_FOOD_COST_KEY = "sila.recipeStudio.targetFoodCostPercent";

export const loadTargetFoodCostPercent = (): number => {
  try {
    const raw = localStorage.getItem(TARGET_FOOD_COST_KEY);
    const value = raw ? Number(raw) : 30;
    return Number.isFinite(value) && value > 0 && value < 100 ? value : 30;
  } catch {
    return 30;
  }
};

export const saveTargetFoodCostPercent = (value: number): void => {
  try {
    localStorage.setItem(TARGET_FOOD_COST_KEY, String(value));
  } catch {
    /* ignore */
  }
};

export interface ClassificationSuggestion {
  familyId: string;
  familyName: string;
  categoryId: string;
  categoryName: string;
  cuisine?: string;
  mealPeriod?: string;
  reason: string;
  /** Soft defaults from the first ingredients — user may override. */
  productClass?: SilaProductClass;
  recipeFormat?: SilaRecipeFormat;
  itemMode?: SilaRecipeMode;
  subcategory?: string;
  templateId?: string;
  templateLabel?: string;
}

const productClassFromText = (text: string): SilaProductClass => {
  if (/\b(tobacco|shisha|cigarette|cigar)\b/i.test(text)) return "TOBACCO";
  if (/\b(minibar)\b/i.test(text)) return "MINIBAR";
  if (/\b(beverage|drink|juice|coffee|tea|soda|wine|beer|spirit|liquor|cocktail|mixer)\b/i.test(text)) {
    return "BEVERAGE";
  }
  return "FOOD";
};

const templateForKind = (kind: StudioVisualKind, productClass: SilaProductClass, text: string): RecipeTemplate | null => {
  if (productClass === "TOBACCO") return RECIPE_TEMPLATES.find((item) => item.id === "shisha") ?? null;
  if (productClass === "MINIBAR") return RECIPE_TEMPLATES.find((item) => item.id === "minibar-item") ?? null;
  if (/\b(cocktail|spirit|liquor|gin|vodka|rum|whisky|whiskey)\b/i.test(text)) {
    return RECIPE_TEMPLATES.find((item) => item.id === "cocktail") ?? null;
  }
  if (/\b(coffee|espresso|latte|cappuccino)\b/i.test(text)) {
    return RECIPE_TEMPLATES.find((item) => item.id === "coffee") ?? null;
  }
  if (kind === "sauce" || /\b(sauce|gravy|dressing)\b/i.test(text)) {
    return RECIPE_TEMPLATES.find((item) => item.id === "batch-sauce") ?? null;
  }
  if (/\b(combo|bundle|meal)\b/i.test(text)) {
    return RECIPE_TEMPLATES.find((item) => item.id === "combo-meal") ?? null;
  }
  if (productClass === "BEVERAGE") {
    return RECIPE_TEMPLATES.find((item) => item.id === "cocktail") ?? null;
  }
  if (kind === "main" || kind === "meat" || kind === "seafood" || kind === "starter" || kind === "dessert") {
    return RECIPE_TEMPLATES.find((item) => item.id === "main-course") ?? null;
  }
  return RECIPE_TEMPLATES.find((item) => item.id === "main-course") ?? null;
};

export interface NameSuggestion {
  name: string;
  reason: string;
}

export interface PricingSuggestion {
  costPerPortion: number | null;
  suggestedSellingPrice: number | null;
  targetFoodCostPercent: number;
  actualFoodCostPercent: number | null;
  incomplete: boolean;
}

const mealPeriodFromKind = (kind: string): string => {
  if (kind === "breakfast") return "Breakfast";
  if (kind === "dessert" || kind === "beverages") return "All day";
  if (kind === "starter") return "Lunch/Dinner";
  return "Lunch/Dinner";
};

/** Rule-based recipe name from leading ingredient descriptions (advisory). */
export const suggestRecipeNames = (lines: IngredientLine[]): NameSuggestion[] => {
  const materials = lines.map((line) => line.material?.description?.trim()).filter((name): name is string => Boolean(name));
  if (materials.length === 0) return [];
  const top = materials.slice(0, 3);
  const joined = top.length === 1 ? top[0] : `${top.slice(0, -1).join(", ")} & ${top[top.length - 1]}`;
  const kind = resolveStudioKind(...materials, ...lines.map((line) => line.material?.materialCode));
  const style =
    kind === "main" || kind === "meat" || kind === "seafood" ? "Plate"
      : kind === "dessert" ? "Dessert"
        : kind === "beverages" ? "Drink"
          : kind === "sauce" ? "Sauce"
            : kind === "starter" ? "Starter"
              : "Recipe";
  return [
    { name: joined, reason: "From selected ingredients" },
    { name: `${joined} ${style}`, reason: `Category cue: ${kind}` },
  ].filter((item, index, all) => all.findIndex((x) => x.name.toLowerCase() === item.name.toLowerCase()) === index);
};

/** Suggest recipe family/category/class/template from ingredient composition vs configured masters. */
export const suggestClassification = (
  lines: IngredientLine[],
  families: SilaRecipeMaster[],
  categories: SilaRecipeMaster[],
): ClassificationSuggestion | null => {
  if (lines.length === 0 || (families.length === 0 && categories.length === 0)) return null;
  const text = lines
    .map((line) => `${line.material?.description ?? ""} ${line.material?.materialCode ?? ""}`)
    .join(" ");
  const kind = resolveStudioKind(text);
  const productClass = productClassFromText(text);
  const template = templateForKind(kind, productClass, text);
  const category =
    categories.find(
      (item) =>
        (!item.productClass || item.productClass === productClass)
        && (
          (template && item.name.toLowerCase() === template.subcategory.toLowerCase())
          || resolveStudioKind(item.name, item.code) === kind
          || item.name.toLowerCase().includes(kind)
        ),
    )
    ?? categories.find((item) => !item.productClass || item.productClass === productClass)
    ?? categories.find((item) => resolveStudioKind(item.name, item.code) === kind)
    ?? categories.find((item) => item.name.toLowerCase().includes(kind))
    ?? categories[0];
  const family =
    families.find((item) => {
      const blob = `${item.name} ${item.code}`;
      if (productClass === "BEVERAGE") return /beverage|drink|bar|fnb|f&b/i.test(blob);
      if (productClass === "TOBACCO") return /tobacco|shisha/i.test(blob);
      if (productClass === "MINIBAR") return /minibar/i.test(blob);
      return /food|fnb|f&b|culinary/i.test(blob);
    })
    ?? families.find((item) => /food|beverage|fnb|f&b/i.test(`${item.name} ${item.code}`))
    ?? families[0];
  if (!family && !category && !template) return null;
  return {
    familyId: family?.id ?? "",
    familyName: family?.name ?? "Not Classified",
    categoryId: category?.id ?? "",
    categoryName: category?.name ?? template?.subcategory ?? "Not Classified",
    cuisine: family?.name,
    mealPeriod: mealPeriodFromKind(kind),
    productClass,
    recipeFormat: template?.recipeFormat,
    itemMode: template?.itemMode,
    subcategory: template?.subcategory ?? category?.name,
    templateId: template?.id,
    templateLabel: template?.label,
    reason: template
      ? `From ingredients → ${template.label} (${kind})`
      : `Based on ingredient mix (${kind})`,
  };
};

export const suggestSellingPrice = (
  totalCost: number | null,
  servingQty: number,
  targetFoodCostPercent: number,
  costs: LineCost[],
): PricingSuggestion => {
  const incomplete = costs.length === 0 || costs.some((cost) => cost.cost == null) || totalCost == null;
  const portion = incomplete || totalCost == null ? null : costPerServing(totalCost, servingQty);
  const ratio = targetFoodCostPercent > 0 ? targetFoodCostPercent / 100 : 0.3;
  const suggested = portion == null || ratio <= 0 ? null : Number((portion / ratio).toFixed(2));
  return {
    costPerPortion: portion,
    suggestedSellingPrice: suggested,
    targetFoodCostPercent,
    actualFoodCostPercent: portion != null && suggested != null && suggested > 0 ? Number(((portion / suggested) * 100).toFixed(2)) : null,
    incomplete,
  };
};

/** Materials that co-occur in approved recipes with current ingredients, else recent materials. */
export const recommendMaterials = (
  lines: IngredientLine[],
  approvedRecipes: SilaRecipeListItem[],
  catalogPool: SilaIngredientOption[],
  limit = 24,
): { items: SilaIngredientOption[]; reason: string } => {
  const currentIds = new Set(lines.map((line) => line.material?.id?.toLowerCase()).filter(Boolean) as string[]);
  const recent = loadRecentMaterials().map((id) => id.toLowerCase());
  const byId = new Map(catalogPool.map((item) => [item.id.toLowerCase(), item]));

  if (currentIds.size === 0) {
    const fromRecent = recent.map((id) => byId.get(id)).filter((item): item is SilaIngredientOption => Boolean(item));
    const filler = catalogPool.filter((item) => !fromRecent.some((x) => x.id === item.id));
    const items = [...fromRecent, ...filler].slice(0, limit);
    return {
      items,
      reason: items.length > 0
        ? "Recent and catalog materials (no recipe history yet)."
        : "Search or filter the Material Master to begin.",
    };
  }

  const kinds = new Set(lines.map((line) => resolveStudioKind(line.material?.description, line.material?.materialCode)));

  const scored = catalogPool
    .filter((item) => !currentIds.has(item.id.toLowerCase()))
    .map((item) => {
      let score = 0;
      if (kinds.has(resolveStudioKind(item.materialGroup, item.category, item.description))) score += 3;
      if (recent.includes(item.id.toLowerCase())) score += 2;
      if (item.priceStatus === "APPROVED") score += 1;
      const words = (item.description ?? "").toLowerCase().split(/\s+/).filter((w) => w.length > 3);
      if (words.some((word) => approvedRecipes.some((recipe) => recipe.name.toLowerCase().includes(word)))) score += 1;
      return { item, score };
    })
    .filter((row) => row.score > 0)
    .sort((a, b) => b.score - a.score)
    .map((row) => row.item);

  if (scored.length === 0) {
    return {
      items: catalogPool.filter((item) => !currentIds.has(item.id.toLowerCase())).slice(0, limit),
      reason: "No co-occurrence history available; showing Material Catalog.",
    };
  }
  return { items: scored.slice(0, limit), reason: "Suggested from ingredient families, recent use and similar recipe names." };
};

export const recommendMenuItems = (
  headerName: string,
  familyName: string | undefined,
  categoryName: string | undefined,
  candidates: MenuItemDetail[],
  limit = 8,
): MenuItemDetail[] => {
  const needle = `${headerName} ${familyName ?? ""} ${categoryName ?? ""}`.toLowerCase().trim();
  if (!needle) return candidates.slice(0, limit);
  return [...candidates]
    .map((item) => {
      const hay = `${item.name} ${item.menuItemCode} ${item.categoryName ?? ""}`.toLowerCase();
      let score = 0;
      needle.split(/\s+/).forEach((word) => {
        if (word.length > 2 && hay.includes(word)) score += 1;
      });
      if (categoryName && (item.categoryName ?? "").toLowerCase().includes(categoryName.toLowerCase())) score += 2;
      return { item, score };
    })
    .filter((row) => row.score > 0)
    .sort((a, b) => b.score - a.score)
    .slice(0, limit)
    .map((row) => row.item);
};

export const recommendOutlets = (
  outlets: SilaLocation[],
  pricedOutletIds: string[],
  mealPeriod?: string,
): SilaLocation[] => {
  if (outlets.length === 0) return [];
  const preferred = outlets.filter((outlet) => {
    const name = `${outlet.locationName} ${outlet.locationCode}`.toLowerCase();
    if (mealPeriod?.toLowerCase().includes("breakfast") && /breakfast|all.?day|dining|restaurant/i.test(name)) return true;
    if (mealPeriod?.toLowerCase().includes("lunch") && /dining|restaurant|outlet|cafe|café/i.test(name)) return true;
    return pricedOutletIds.includes(outlet.id);
  });
  return (preferred.length > 0 ? preferred : outlets).slice(0, 12);
};

export { toNumber };
