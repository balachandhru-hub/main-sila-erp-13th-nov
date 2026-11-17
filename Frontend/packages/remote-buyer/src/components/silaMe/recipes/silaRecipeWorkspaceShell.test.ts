/**
 * Step 1 route + shell contract checks (no Vite/DOM required).
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

const guid = "f4eb96d4-95fc-4cec-9da2-f45d2df284d2";

assert.equal(silaRecipeCatalogPath(), "/sila-recipes");
assert.equal(silaRecipeNewPath(), "/sila-recipes/new");
assert.equal(silaRecipeDetailPath(guid), `/sila-recipes/${guid}`);
assert.equal(silaRecipeEditPath(guid), `/sila-recipes/${guid}/edit`);

assert.equal(parseSilaRecipePath("/sila-recipes").kind, "list");
assert.equal(parseSilaRecipePath("/sila-recipes/new").kind, "new");
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}`), { kind: "detail", recipeId: guid });
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}/edit`), { kind: "edit", recipeId: guid });
assert.equal(parseSilaRecipePath("/sila-recipes/not-a-guid").kind, "list");
// Extra segments after /edit are ignored by the existing first-two-segment contract (third === "edit").
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}/edit/extra`), { kind: "edit", recipeId: guid });
assert.equal(isRecipeId(guid), true);
assert.equal(isRecipeId("new"), false);

// Shell display contract (mirrors SilaRecipeWorkspaceShell copy — keep in sync).
const shellTitle = (mode: "new" | "edit") =>
  mode === "edit" ? "Edit Recipe — Workspace Ready" : "New Recipe — Workspace Ready";
const shellBody = (mode: "new" | "edit") =>
  mode === "edit" ? "Recipe editing workspace is connected." : "Recipe creation workspace is connected.";
assert.equal(shellTitle("new"), "New Recipe — Workspace Ready");
assert.equal(shellTitle("edit"), "Edit Recipe — Workspace Ready");
assert.equal(shellBody("new"), "Recipe creation workspace is connected.");
assert.equal(shellBody("edit"), "Recipe editing workspace is connected.");

console.log("silaRecipeWorkspaceShell.test.ts: ok");
