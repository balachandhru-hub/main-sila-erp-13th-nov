import type { SilaRecipeDetail } from '../../../../remote-buyer/src/api/silaMe/silaRecipeApi';
import { getLiveInventoryDetail } from '../../../../remote-buyer/src/api/silaMe/silaInventoryApi';
import { scaleIngredients } from './recipeStudioMath';

export {
  PRODUCT_CHIPS,
  readFavoriteRecipeIds,
  readRecentRecipeIds,
  rememberRecipe,
  scaleIngredients,
  toggleFavoriteRecipe,
  type ScaledIngredient,
} from './recipeStudioMath';

export interface IngredientAvailability {
  materialId: string;
  itemCode: string;
  itemName: string;
  required: number;
  available: number;
  uom: string;
  possibleServings: number;
  limiting: boolean;
  status: 'OK' | 'LOW' | 'OUT';
}

/** Theoretical servings from approved ingredients + live inventory. No stock posting. */
export const loadIngredientAvailability = async (
  recipe: SilaRecipeDetail,
  locationId: string,
  servings = recipe.servingQty || 1,
): Promise<{ lines: IngredientAvailability[]; possibleServings: number }> => {
  const scaled = scaleIngredients(recipe, servings);
  const lines: IngredientAvailability[] = [];
  for (const row of scaled) {
    const materialId = row.ingredient.materialId;
    if (!materialId || row.scaledQty <= 0) continue;
    const stock = await getLiveInventoryDetail(materialId, {
      requiredQty: row.scaledQty,
      currentLocationId: locationId,
    });
    const available =
      stock.localAvailable ??
      stock.locations.find((x) => x.locationId === locationId)?.availableQty ??
      0;
    const perServing = recipe.servingQty > 0 ? row.ingredient.quantity : row.scaledQty;
    const possible = perServing > 0 ? Math.floor(available / perServing) : 0;
    lines.push({
      materialId,
      itemCode: row.ingredient.itemCode || stock.materialCode,
      itemName: row.ingredient.itemName || stock.description,
      required: row.scaledQty,
      available,
      uom: row.ingredient.uom || stock.baseUom,
      possibleServings: possible,
      limiting: false,
      status: available <= 0 ? 'OUT' : available < row.scaledQty ? 'LOW' : 'OK',
    });
  }
  const possibleServings = lines.length === 0 ? 0 : Math.min(...lines.map((x) => x.possibleServings));
  return {
    lines: lines.map((line) => ({ ...line, limiting: line.possibleServings === possibleServings })),
    possibleServings,
  };
};
