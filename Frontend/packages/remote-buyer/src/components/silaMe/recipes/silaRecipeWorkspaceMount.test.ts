/**
 * Step 2: prove shell copy + route→mode contract used by the live mount path.
 * Also checks that host Vite (localhost:6001) transforms the current SilaRecipes source
 * (not a stale SilaRecipeEditor mount).
 *
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeWorkspaceMount.test.ts
 */
import assert from "node:assert/strict";
import {
  RECIPE_WORKSPACE_BACK_LABEL,
  recipeWorkspaceShellBody,
  recipeWorkspaceShellTitle,
} from "./recipeWorkspaceShellCopy";
import {
  parseSilaRecipePath,
  silaRecipeEditPath,
  silaRecipeNewPath,
} from "./silaRecipeRoutes";

const guid = "f4eb96d4-95fc-4cec-9da2-f45d2df284d2";

assert.equal(recipeWorkspaceShellTitle("new"), "New Recipe — Workspace Ready");
assert.equal(recipeWorkspaceShellBody("new"), "Recipe creation workspace is connected.");
assert.equal(recipeWorkspaceShellTitle("edit"), "Edit Recipe — Workspace Ready");
assert.equal(recipeWorkspaceShellBody("edit"), "Recipe editing workspace is connected.");
assert.equal(RECIPE_WORKSPACE_BACK_LABEL, "Back to Recipe Catalog");

assert.equal(silaRecipeNewPath(), "/sila-recipes/new");
assert.equal(silaRecipeEditPath(guid), `/sila-recipes/${guid}/edit`);
assert.equal(parseSilaRecipePath("/sila-recipes/new").kind, "new");
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}/edit`), { kind: "edit", recipeId: guid });
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}`), { kind: "detail", recipeId: guid });
assert.equal(parseSilaRecipePath("/sila-recipes").kind, "list");
// "new" is never a recipe id / edit route
assert.notEqual(parseSilaRecipePath("/sila-recipes/new").kind, "edit");
assert.notEqual(parseSilaRecipePath("/sila-recipes/new").kind, "detail");

async function assertHostServesShellMount(): Promise<void> {
  const { execFileSync } = await import("node:child_process");
  const url =
    "https://localhost:6001/@fs/workspace/Frontend/packages/remote-buyer/src/components/silaMe/recipes/SilaRecipes.tsx";
  let body = "";
  try {
    body = execFileSync("curl", ["-k", "-fsS", url], { encoding: "utf8", timeout: 15000 });
  } catch (err: unknown) {
    assert.fail(`host transform unreachable: ${err instanceof Error ? err.message : String(err)}`);
  }
  assert.match(body, /SilaRecipeWorkspaceShell/);
  assert.match(body, /new-recipe-shell/);
  assert.doesNotMatch(body, /import SilaRecipeEditor/);
  assert.match(body, /mode:\s*"new"|mode: "new"|\"new\"/);
}

await assertHostServesShellMount();
console.log("silaRecipeWorkspaceMount.test.ts: ok");
