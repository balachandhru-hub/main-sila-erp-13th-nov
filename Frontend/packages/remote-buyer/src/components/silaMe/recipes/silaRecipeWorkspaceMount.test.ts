/**
 * Step 4: host Vite must serve presentation UI on the Step 2 mount path.
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeWorkspaceMount.test.ts
 */
import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import {
  RECIPE_WORKSPACE_BACK_LABEL,
  recipeWorkspaceShellTitle,
} from "./recipeWorkspaceShellCopy";
import {
  parseSilaRecipePath,
  silaRecipeEditPath,
  silaRecipeNewPath,
} from "./silaRecipeRoutes";

const guid = "f4eb96d4-95fc-4cec-9da2-f45d2df284d2";

assert.equal(recipeWorkspaceShellTitle("new"), "Create New Recipe");
assert.equal(recipeWorkspaceShellTitle("edit"), "Edit Recipe");
assert.equal(RECIPE_WORKSPACE_BACK_LABEL, "Back to Recipe Catalog");
assert.equal(silaRecipeNewPath(), "/sila-recipes/new");
assert.equal(silaRecipeEditPath(guid), `/sila-recipes/${guid}/edit`);
assert.equal(parseSilaRecipePath("/sila-recipes/new").kind, "new");
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}/edit`), { kind: "edit", recipeId: guid });

function curlText(url: string): string {
  try {
    return execFileSync("curl", ["-k", "-fsS", url], { encoding: "utf8", timeout: 15000 });
  } catch (err: unknown) {
    assert.fail(`unreachable ${url}: ${err instanceof Error ? err.message : String(err)}`);
  }
}

const recipesUrl =
  "https://localhost:6001/@fs/workspace/Frontend/packages/remote-buyer/src/components/silaMe/recipes/SilaRecipes.tsx";
const shellUrl =
  "https://localhost:6001/@fs/workspace/Frontend/packages/remote-buyer/src/components/silaMe/recipes/SilaRecipeWorkspaceShell.tsx";
const presentationUrl =
  "https://localhost:6001/@fs/workspace/Frontend/packages/remote-buyer/src/components/silaMe/recipes/SilaRecipeStudioPresentation.tsx";

const recipesBody = curlText(recipesUrl);
assert.match(recipesBody, /SilaRecipeWorkspaceShell/);
assert.match(recipesBody, /new-recipe-shell/);
assert.doesNotMatch(recipesBody, /import SilaRecipeEditor/);

const shellBody = curlText(shellUrl);
assert.match(shellBody, /SilaRecipeStudioPresentation/);
assert.doesNotMatch(shellBody, /import SilaRecipeEditor/);

const presentationBody = curlText(presentationUrl);
assert.match(presentationBody, /Create New Recipe/);
assert.match(presentationBody, /Edit Recipe/);
assert.match(presentationBody, /My Recipe/);
assert.match(presentationBody, /Material Catalog/);
assert.match(presentationBody, /SilaRecipeMaterialLibrary/);
assert.match(presentationBody, /SilaRecipeCanvas/);
assert.match(presentationBody, /searchIngredients/);
assert.match(presentationBody, /createRecipe/);
assert.match(presentationBody, /getRecipe/);
assert.match(presentationBody, /updateRecipe/);
assert.match(presentationBody, /createRecipeRevision/);
assert.match(presentationBody, /setRecipePicture/);
assert.match(presentationBody, /recipe-studio-save-draft/);
assert.match(presentationBody, /recipe-studio-submit/);
assert.match(presentationBody, /COST PENDING/);
assert.match(presentationBody, /\bsubmitRecipe\b/);
assert.match(presentationBody, /Step 7 of 7/);
assert.doesNotMatch(presentationBody, /import SilaRecipeEditor/);

console.log("silaRecipeWorkspaceMount.test.ts: ok");
