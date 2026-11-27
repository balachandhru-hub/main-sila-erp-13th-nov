/**
 * Permanent delete for inactive recipes: access rules + catalog/detail/API wiring (no Vite/axios/DB).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeInactiveDelete.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  canDeleteInactiveRecipe,
  DELETE_INACTIVE_BODY,
  DELETE_INACTIVE_CONFIRM_LABEL,
  DELETE_INACTIVE_SUCCESS,
  DELETE_INACTIVE_TITLE,
} from "./recipeInactiveDelete";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");

assert.equal(canDeleteInactiveRecipe("INACTIVE"), true);
assert.equal(canDeleteInactiveRecipe("inactive"), true);
assert.equal(canDeleteInactiveRecipe("DRAFT"), false);
assert.equal(canDeleteInactiveRecipe("APPROVED"), false);
assert.equal(canDeleteInactiveRecipe(null), false);
assert.equal(DELETE_INACTIVE_TITLE, "Delete inactive recipe permanently?");
assert.match(DELETE_INACTIVE_BODY, /cannot be undone/i);
assert.equal(DELETE_INACTIVE_CONFIRM_LABEL, "Delete permanently");
assert.equal(DELETE_INACTIVE_SUCCESS, "Inactive recipe permanently deleted");

const catalog = read("SilaRecipeCatalogGrid.tsx");
assert.match(catalog, /status === "INACTIVE"/);
assert.match(catalog, /Delete Permanently/);
assert.match(catalog, /delete-permanent/);

const table = read("SilaRecipeTable.tsx");
assert.match(table, /status === "INACTIVE"/);
assert.match(table, /Delete permanently/);
assert.match(table, /recipe-table-delete-inactive/);

const detail = read("SilaRecipeDetailView.tsx");
assert.match(detail, /deleteInactiveRecipe/);
assert.match(detail, /recipe-detail-delete-inactive/);
assert.match(detail, /DELETE_INACTIVE_TITLE/);

const recipes = read("SilaRecipes.tsx");
assert.match(recipes, /deleteInactiveRecipe/);
assert.match(recipes, /DELETE_INACTIVE_SUCCESS/);
assert.match(recipes, /canDeleteInactiveRecipe/);

const api = read("../../../api/silaMe/silaRecipeApi.ts");
assert.match(api, /deleteInactiveRecipe/);
assert.match(api, /axiosInstance\.post\(`\$\{BASE\}\/recipes\/\$\{recipeId\}\/inactive\/delete`\)/);

const controller = readFileSync(
  "/workspace/Backend/src/Modules/Buyer/Buyer.API/Controller/SilaRecipeController.cs",
  "utf8",
);
assert.match(controller, /DeleteSilaInactiveRecipeCommand/);
assert.match(controller, /inactive\/delete/);
assert.match(controller, /HttpPost/);
assert.match(controller, /MANAGE_SILA_RECIPE/);

const handler = readFileSync(
  "/workspace/Backend/src/Modules/Buyer/Buyer.Application/Features/Commands/DeleteSilaInactiveRecipe/DeleteSilaInactiveRecipeCommandHandler.cs",
  "utf8",
);
assert.match(handler, /SILA_RECIPE_INACTIVE/);
assert.match(handler, /DeleteRange/);
assert.match(handler, /EVENT_PERMANENTLY_DELETED/);

console.log("silaRecipeInactiveDelete.test.ts: ok");
