/**
 * Step 3 shell/presentation contract checks.
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeWorkspaceShell.test.ts
 */
import assert from "node:assert/strict";
import {
  isRecipeId,
  parseSilaRecipePath,
  silaRecipeCatalogPath,
  silaRecipeDetailPath,
  silaRecipeEditPath,
  silaRecipeNewPath,
} from "./silaRecipeRoutes";
import {
  RECIPE_WORKSPACE_BACK_LABEL,
  recipeWorkspaceShellBody,
  recipeWorkspaceShellTitle,
} from "./recipeWorkspaceShellCopy";

const guid = "f4eb96d4-95fc-4cec-9da2-f45d2df284d2";

assert.equal(silaRecipeCatalogPath(), "/sila-recipes/master");
assert.equal(silaRecipeNewPath(), "/sila-recipes/new");
assert.equal(silaRecipeDetailPath(guid), `/sila-recipes/${guid}`);
assert.equal(silaRecipeEditPath(guid), `/sila-recipes/${guid}/edit`);

assert.equal(parseSilaRecipePath("/sila-recipes").kind, "overview");
assert.equal(parseSilaRecipePath("/sila-recipes/master").kind, "list");
assert.equal(parseSilaRecipePath("/sila-recipes/new").kind, "new");
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}`), { kind: "detail", recipeId: guid });
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}/edit`), { kind: "edit", recipeId: guid });
assert.equal(isRecipeId(guid), true);
assert.equal(isRecipeId("new"), false);

assert.equal(recipeWorkspaceShellTitle("new"), "Create New Recipe");
assert.equal(recipeWorkspaceShellTitle("edit"), "Edit Recipe");
assert.match(recipeWorkspaceShellBody("new"), /Blank local UI draft/);
assert.match(recipeWorkspaceShellBody("edit"), /Step 6/);
assert.equal(RECIPE_WORKSPACE_BACK_LABEL, "Back to Recipe Catalog");

console.log("silaRecipeWorkspaceShell.test.ts: ok");
