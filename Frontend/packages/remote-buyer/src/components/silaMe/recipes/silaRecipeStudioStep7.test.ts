/**
 * Step 7: picture validation + Studio/catalog/detail wiring (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeStudioStep7.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import {
  detectRecipePictureKind,
  RECIPE_PICTURE_MAX_BYTES,
} from "./recipeStudioPicture";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");

assert.equal(RECIPE_PICTURE_MAX_BYTES, 5 * 1024 * 1024);
assert.equal(detectRecipePictureKind(Uint8Array.from([0xff, 0xd8, 0xff, 0xe0])), "JPEG");
assert.equal(detectRecipePictureKind(Uint8Array.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])), "PNG");
assert.equal(
  detectRecipePictureKind(Uint8Array.from([0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50])),
  "WEBP",
);
assert.equal(detectRecipePictureKind(Uint8Array.from([0x00, 0x01])), null);

const presentation = read("SilaRecipeStudioPresentation.tsx");
assert.match(presentation, /setRecipePicture/);
assert.match(presentation, /removeRecipePicture/);
assert.match(presentation, /SilaRecipePicture/);
assert.match(presentation, /Step 7 of 7/);
assert.match(presentation, /persistPendingPicture/);
assert.doesNotMatch(presentation, /import SilaRecipeEditor/);

const catalog = read("SilaRecipeCatalogGrid.tsx");
assert.match(catalog, /SilaRecipePicture/);
assert.match(catalog, /hasPicture/);

const detail = read("SilaRecipeDetailView.tsx");
assert.match(detail, /SilaRecipePicture/);
assert.match(detail, /hasPicture/);

const api = read("../../../api/silaMe/silaRecipeApi.ts");
assert.match(api, /\/recipes\/\$\{recipeId\}\/picture/);
assert.match(api, /setRecipePicture/);
assert.match(api, /getRecipePicture/);
assert.match(api, /removeRecipePicture/);

const backendRules = readFileSync(
  "/workspace/Backend/src/Modules/Buyer/Buyer.Application/Features/Shared/SilaRecipePictureRules.cs",
  "utf8",
);
assert.match(backendRules, /SLOT_ACTIVE/);
assert.match(backendRules, /SLOT_DRAFT/);
assert.match(backendRules, /MAX_PICTURE_BYTES = 5/);

console.log("silaRecipeStudioStep7.test.ts: ok");
