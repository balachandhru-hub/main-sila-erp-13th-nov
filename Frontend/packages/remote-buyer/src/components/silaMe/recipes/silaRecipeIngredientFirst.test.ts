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
assert.match(presentation, /data-testid="recipe-material-catalog-grid"/);
assert.match(presentation, /data-testid="recipe-material-catalog-list"/);
assert.match(presentation, /catalogLayout/);
assert.match(presentation, /data-testid="recipe-ingredient-autofill-note"/);
assert.match(presentation, /Exit fullscreen/);
assert.match(presentation, /Start with ingredients/);
assert.match(presentation, /lockClassification/);

const library = read("SilaRecipeMaterialLibrary.tsx");
assert.match(library, /layout === "grid"/);
assert.match(library, /data-layout=\{layout\}/);
assert.match(library, /is-\$\{layout\}/);

const css = read("SilaRecipeStudioPresentation.css");
assert.match(css, /\.rstudio-panel--catalog\.is-expanded/);
assert.match(css, /\.rstudio-panel--recipe > \.rstudio-section--canvas/);
assert.match(css, /\.rstudio-catalog-head/);
assert.match(css, /\.rstudio-catalog-head__actions/);
assert.match(css, /\.rstudio-catalog-grid\.is-grid/);
assert.match(css, /\.rstudio-catalog-grid\.is-list/);
const suggestions = read("recipeStudioSuggestions.ts");
assert.match(suggestions, /templateForKind/);
assert.match(suggestions, /productClassFromText/);
assert.match(suggestions, /templateId\?:/);
assert.match(suggestions, /productClass\?:/);
assert.match(suggestions, /From ingredients →/);

console.log("silaRecipeIngredientFirst.test.ts: ok");
