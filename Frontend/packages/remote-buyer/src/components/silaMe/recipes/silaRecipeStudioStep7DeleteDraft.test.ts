/**
 * Step 7 Delete Draft: access rules + Studio/catalog/detail/API wiring (no Vite/axios/DB).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeStudioStep7DeleteDraft.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  canDeleteDraftRecipe,
  DELETE_DRAFT_BODY,
  DELETE_DRAFT_CONFIRM_LABEL,
  DELETE_DRAFT_SUCCESS,
  DELETE_DRAFT_TITLE,
  DISCARD_CHANGES_LABEL,
} from "./recipeStudioDraftDelete";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");

assert.equal(canDeleteDraftRecipe("DRAFT"), true);
assert.equal(canDeleteDraftRecipe("draft"), true);
assert.equal(canDeleteDraftRecipe("PENDING_APPROVAL"), false);
assert.equal(canDeleteDraftRecipe("APPROVED"), false);
assert.equal(canDeleteDraftRecipe("REJECTED"), false);
assert.equal(canDeleteDraftRecipe("INACTIVE"), false);
assert.equal(canDeleteDraftRecipe(null), false);
assert.equal(DELETE_DRAFT_TITLE, "Delete Draft Recipe?");
assert.match(DELETE_DRAFT_BODY, /Approved versions, if any, will remain unchanged/);
assert.equal(DELETE_DRAFT_CONFIRM_LABEL, "Delete Draft");
assert.equal(DELETE_DRAFT_SUCCESS, "Draft recipe deleted successfully");
assert.equal(DISCARD_CHANGES_LABEL, "Discard Changes");

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /deleteRecipeDraft/);
assert.match(presentation, /recipe-studio-delete-draft/);
assert.match(presentation, /recipe-studio-discard-changes/);
assert.match(presentation, /DELETE_DRAFT_TITLE/);
assert.match(presentation, /showDeleteDraft/);
assert.match(presentation, /showDiscardChanges/);
assert.doesNotMatch(presentation, /deactivateRecipe/);

const catalog = read("SilaRecipeCatalogGrid.tsx");
assert.match(catalog, /status === "DRAFT"/);
assert.match(catalog, /Delete Draft/);

const table = read("SilaRecipeTable.tsx");
assert.match(table, /status === "DRAFT"/);
assert.match(table, /Delete Draft/);

const detail = read("SilaRecipeDetailView.tsx");
assert.match(detail, /deleteRecipeDraft/);
assert.match(detail, /recipe-detail-delete-draft/);
assert.match(detail, /DELETE_DRAFT_TITLE/);
assert.doesNotMatch(detail, /deactivateRecipe/);

const recipes = read("SilaRecipes.tsx");
assert.match(recipes, /deleteRecipeDraft/);
assert.match(recipes, /DELETE_DRAFT_SUCCESS/);
assert.doesNotMatch(recipes, /deactivateRecipe/);

const api = read("../../../api/silaMe/silaRecipeApi.ts");
assert.match(api, /\/recipes\/\$\{recipeId\}\/draft\/delete/);
assert.match(api, /deleteRecipeDraft/);

const controller = readFileSync(
  "/workspace/Backend/src/Modules/Buyer/Buyer.API/Controller/SilaRecipeController.cs",
  "utf8",
);
assert.match(controller, /draft\/delete/);
assert.match(controller, /DeleteSilaRecipeDraftCommand/);
assert.match(controller, /MANAGE_SILA_RECIPE/);

const handler = readFileSync(
  "/workspace/Backend/src/Modules/Buyer/Buyer.Application/Features/Commands/DeleteSilaRecipeDraft/DeleteSilaRecipeDraftCommandHandler.cs",
  "utf8",
);
assert.match(handler, /SILA_RECIPE_DRAFT/);
assert.match(handler, /ActiveVersion > 0/);
assert.match(handler, /SILA_RECIPE_APPROVED/);
assert.match(handler, /SILA_RECIPE_INACTIVE/);
assert.match(handler, /EVENT_DRAFT_DELETED/);
assert.match(handler, /ClearDraft/);
assert.match(handler, /RetireVersionRowsAsync/);

console.log("silaRecipeStudioStep7DeleteDraft.test.ts: ok");
