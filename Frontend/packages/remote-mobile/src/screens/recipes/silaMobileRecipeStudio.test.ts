/**
 * Mobile Recipe Studio wiring + portion scaling (no Vite/axios runtime).
 * Run: npx --yes tsx src/screens/recipes/silaMobileRecipeStudio.test.ts
 */
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { PRODUCT_CHIPS, scaleIngredients } from './recipeStudioMath';

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), 'utf8');
const app = readFileSync(join(dir, '../../MobileApp.tsx'), 'utf8');
const more = readFileSync(join(dir, '../more/MoreScreen.tsx'), 'utf8');
const home = readFileSync(join(dir, '../home/HomeScreen.tsx'), 'utf8');
const cloudNav = readFileSync(join(dir, '../../../../remote-buyer/src/components/silaMe/silaMeNav.tsx'), 'utf8');
const cloudWorkspace = readFileSync(join(dir, '../../../../remote-buyer/src/components/silaMe/SilaMeWorkspace.tsx'), 'utf8');

assert.deepEqual(
  PRODUCT_CHIPS.map((chip) => chip.id),
  ['', 'FOOD', 'BEVERAGE', 'MINIBAR', 'TOBACCO'],
);

const sample = {
  servingQty: 4,
  ingredients: [
    { id: 'i1', quantity: 200, uom: 'G', itemName: 'Flour', itemCode: 'M1', materialId: 'm1' },
    { id: 'i2', quantity: 50, uom: 'G', itemName: 'Butter', itemCode: 'M2', materialId: 'm2' },
  ],
} as Parameters<typeof scaleIngredients>[0];

const scaled = scaleIngredients(sample, 10);
assert.equal(scaled.length, 2);
assert.equal(scaled[0].scaledQty, 500);
assert.equal(scaled[1].scaledQty, 125);

assert.match(app, /RecipeStudioHomeScreen/);
assert.match(app, /recipes\/catalog/);
assert.match(app, /recipes\/draft\/new/);
assert.match(app, /recipes\/drafts/);
assert.match(app, /recipes\/approvals/);
assert.match(app, /approvalMode/);

assert.match(read('RecipeStudioHomeScreen.tsx'), /getRecipes/);
assert.match(read('RecipeStudioHomeScreen.tsx'), /getRecipeApprovals/);
assert.match(read('RecipeStudioHomeScreen.tsx'), /Quick draft/);

assert.match(read('RecipesScreen.tsx'), /productClass/);
assert.match(read('RecipesScreen.tsx'), /PRODUCT_CHIPS/);
assert.match(read('RecipesScreen.tsx'), /Food \/ Beverage \/ Minibar \/ Tobacco/);
assert.match(read('recipeStudioMath.ts'), /FOOD/);
assert.match(read('recipeStudioMath.ts'), /BEVERAGE/);
assert.match(read('recipeStudioMath.ts'), /MINIBAR/);
assert.match(read('recipeStudioMath.ts'), /TOBACCO/);

assert.match(read('RecipeDetailScreen.tsx'), /scaleIngredients/);
assert.match(read('RecipeDetailScreen.tsx'), /loadIngredientAvailability/);
assert.match(read('RecipeDetailScreen.tsx'), /submitRecipe/);
assert.match(read('RecipeDetailScreen.tsx'), /approveRecipe/);
assert.match(read('RecipeDetailScreen.tsx'), /rejectRecipe/);
assert.match(read('RecipeDetailScreen.tsx'), /deleteRecipeDraft/);
assert.match(read('RecipeDetailScreen.tsx'), /Duplicate draft/);
assert.match(read('RecipeDetailScreen.tsx'), /Open Live Inventory/);

assert.match(read('RecipeDraftWizardScreen.tsx'), /createRecipe/);
assert.match(read('RecipeDraftWizardScreen.tsx'), /searchMaterials/);
assert.match(read('RecipeDraftWizardScreen.tsx'), /Submit for approval/);
assert.match(read('RecipeDraftWizardScreen.tsx'), /setRecipePicture/);

assert.match(read('RecipeDraftsScreen.tsx'), /deleteRecipeDraft/);
assert.match(read('RecipeApprovalsScreen.tsx'), /getRecipeApprovals/);
assert.match(read('recipeStudioHelpers.ts'), /getLiveInventoryDetail/);
assert.match(read('recipeStudioMath.ts'), /Read-only portion scaling/);

assert.match(more, /Recipe Studio/);
assert.match(home, /Recipe Studio/);

assert.match(cloudNav, /Recipe Studio/);
assert.match(cloudNav, /POS Studio/);
assert.match(cloudWorkspace, /SilaRecipes/);
assert.match(cloudWorkspace, /SilaPosIntegration/);

console.log('silaMobileRecipeStudio.test.ts: ok');
