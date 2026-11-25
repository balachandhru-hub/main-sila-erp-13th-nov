/**
 * Ingredient-first create/edit + Material Catalog expand guards.
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeIngredientFirst.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const dir = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(dir, name), "utf8");

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /classificationLocked/);
assert.match(presentation, /catalogExpanded/);
assert.match(presentation, /applyIngredientDefaults/);
assert.match(presentation, /data-testid="recipe-catalog-expand"/);
assert.match(presentation, /data-testid="recipe-ingredient-autofill-note"/);
assert.match(presentation, /Exit fullscreen/);
assert.match(presentation, /Start with ingredients/);
assert.match(presentation, /lockClassification/);

const css = read("SilaRecipeStudioPresentation.css");
assert.match(css, /\.rstudio-panel--catalog\.is-expanded/);
assert.match(css, /\.rstudio-panel--recipe > \.rstudio-section--canvas/);
assert.match(css, /\.rstudio-catalog-head/);

const suggestions = read("recipeStudioSuggestions.ts");
assert.match(suggestions, /templateForKind/);
assert.match(suggestions, /productClassFromText/);
assert.match(suggestions, /templateId\?:/);
assert.match(suggestions, /productClass\?:/);
assert.match(suggestions, /From ingredients →/);

console.log("silaRecipeIngredientFirst.test.ts: ok");
