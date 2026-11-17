/**
 * Bounded code-level checks for New Recipe create path (route parse + success id).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeCreatePath.test.ts
 */
import assert from "node:assert/strict";
import { readSuccessId } from "../../../api/silaMe/readSuccessId";
import {
  isRecipeId,
  parseSilaRecipePath,
  silaRecipeEditPath,
  silaRecipeNewPath,
} from "./silaRecipeRoutes";

const guid = "a1b2c3d4-e5f6-4789-a012-3456789abcde";

assert.equal(parseSilaRecipePath("/sila-recipes").kind, "list");
assert.equal(parseSilaRecipePath("/sila-recipes/new").kind, "new");
assert.equal(parseSilaRecipePath("/sila-recipes/NEW").kind, "new");
assert.equal(parseSilaRecipePath("/sila-recipes/new/").kind, "new");
assert.equal(parseSilaRecipePath("/sila-recipes/new?x=1").kind, "new");
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}`), { kind: "detail", recipeId: guid });
assert.deepEqual(parseSilaRecipePath(`/sila-recipes/${guid}/edit`), { kind: "edit", recipeId: guid });
// "new" must never be treated as a recipe id
assert.equal(isRecipeId("new"), false);
assert.equal(parseSilaRecipePath("/sila-recipes/undefined").kind, "list");
assert.equal(parseSilaRecipePath("/sila-recipes/not-a-guid").kind, "list");

assert.equal(silaRecipeNewPath(), "/sila-recipes/new");
assert.equal(silaRecipeEditPath(guid), `/sila-recipes/${guid}/edit`);

assert.equal(readSuccessId({ id: guid }), guid);
assert.equal(readSuccessId({ Id: guid }), guid);
assert.equal(readSuccessId({ id: "  " }), "");
assert.equal(readSuccessId({ statusCode: 200, message: "Success" }), "");
assert.equal(readSuccessId(null), "");
assert.equal(readSuccessId("not-json"), "");

console.log("silaRecipeCreatePath.test.ts: all assertions passed");
