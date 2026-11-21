import type { SilaRecipeDetail, SilaRecipeIngredient } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';

export const PRODUCT_CHIPS = [
  { id: '', label: 'All' },
  { id: 'FOOD', label: 'Food' },
  { id: 'BEVERAGE', label: 'Beverage' },
  { id: 'MINIBAR', label: 'Minibar' },
  { id: 'TOBACCO', label: 'Tobacco' },
] as const;

export interface ScaledIngredient {
  ingredient: SilaRecipeIngredient;
  scaledQty: number;
}

/** Read-only portion scaling — does not mutate the approved recipe. */
export const scaleIngredients = (recipe: SilaRecipeDetail, servings: number): ScaledIngredient[] => {
  const base = recipe.servingQty > 0 ? recipe.servingQty : 1;
  const factor = servings > 0 ? servings / base : 1;
  return (recipe.ingredients ?? []).map((ingredient) => ({
    ingredient,
    scaledQty: Math.round(ingredient.quantity * factor * 10000) / 10000,
  }));
};

const RECENT_KEY = 'sila.mobile.recipe.recent';
const FAVORITE_KEY = 'sila.mobile.recipe.favorites';

export const readRecentRecipeIds = (): string[] => {
  try {
    const raw = localStorage.getItem(RECENT_KEY);
    const parsed = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string') : [];
  } catch {
    return [];
  }
};

export const rememberRecipe = (recipeId: string) => {
  const next = [recipeId, ...readRecentRecipeIds().filter((id) => id !== recipeId)].slice(0, 12);
  localStorage.setItem(RECENT_KEY, JSON.stringify(next));
};

export const readFavoriteRecipeIds = (): string[] => {
  try {
    const raw = localStorage.getItem(FAVORITE_KEY);
    const parsed = raw ? JSON.parse(raw) : [];
    return Array.isArray(parsed) ? parsed.filter((x) => typeof x === 'string') : [];
  } catch {
    return [];
  }
};

export const toggleFavoriteRecipe = (recipeId: string): string[] => {
  const current = readFavoriteRecipeIds();
  const next = current.includes(recipeId) ? current.filter((id) => id !== recipeId) : [recipeId, ...current].slice(0, 40);
  localStorage.setItem(FAVORITE_KEY, JSON.stringify(next));
  return next;
};
