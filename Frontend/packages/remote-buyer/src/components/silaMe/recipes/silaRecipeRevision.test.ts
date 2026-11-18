/**
 * Create-revision helpers: write payload from detail + guardrails.
 * Run: npx --yes tsx src/components/silaMe/recipes/silaRecipeRevision.test.ts
 */
import assert from "node:assert/strict";
import type { SilaRecipeDetail } from "../../../api/silaMe/silaRecipeApi";
import { canStartRevision, writeFromDetail } from "./recipeWriteModel";

const detail = {
  id: "f4eb96d4-95fc-4cec-9da2-f45d2df284d2",
  recipeCode: "RI00000003",
  name: "JSHOTS",
  description: null,
  familyId: "d02c392f-e7c3-48c6-8933-e8fdbd1cff8e",
  categoryId: "d5830f29-f77f-4aa2-8821-b820fe5e2b94",
  itemMode: "RECIPE",
  servingQty: 1,
  servingUom: "EA",
  sellingUom: "EA",
  posCode: null,
  posItem: null,
  currency: "AED",
  status: "APPROVED",
  version: 1,
  activeVersion: 1,
  viewVersion: 1,
  viewStatus: "ACTIVE",
  versions: [{ version: 1, status: "ACTIVE" }],
  totalCost: 51,
  readiness: { ready: true, costComplete: true, issues: [] },
  ingredients: [
    {
      id: "a",
      ingredientCode: "RI00000003I1",
      materialId: "30ced77a-98e6-4ac1-ab98-9f968719166a",
      itemCode: "1000001",
      itemName: "1000001",
      quantity: 30,
      uom: "ML",
      baseQuantity: 0.04,
      baseUom: "EA",
      cost: 13.6,
      sequence: 1,
      conversions: [],
    },
    {
      id: "b",
      ingredientCode: "RI00000003I2",
      materialId: "5f3081ba-0ae1-4f49-bf07-1eacf87340e1",
      itemCode: "1000000",
      itemName: "1000000",
      quantity: 60,
      uom: "ML",
      baseQuantity: 0.16,
      baseUom: "EA",
      cost: 37.44,
      sequence: 2,
      conversions: [],
    },
  ],
  outletPrices: [
    { id: "p1", outletLocationId: "b06ee0f5-4a82-40fa-be9f-8b580e43d43d", menuPrice: 170.13 },
    { id: "p2", outletLocationId: "04680017-e176-4b62-a864-0267cd546b3b", menuPrice: 170.13 },
  ],
  approvalSteps: [],
  history: [],
} as SilaRecipeDetail;

const write = writeFromDetail(detail);
assert.equal(write.name, "JSHOTS");
assert.equal(write.itemMode, "RECIPE");
assert.equal(write.ingredients.length, 2);
assert.equal(write.ingredients[0]?.materialId, "30ced77a-98e6-4ac1-ab98-9f968719166a");
assert.equal(write.ingredients[0]?.quantity, 30);
assert.equal(write.ingredients[0]?.uom, "ML");
assert.equal(write.outletPrices.length, 2);
assert.equal(write.familyId, detail.familyId);
assert.equal(write.categoryId, detail.categoryId);
assert.equal(canStartRevision("APPROVED"), true);
assert.equal(canStartRevision("REJECTED"), true);
assert.equal(canStartRevision("DRAFT"), false);
assert.equal(canStartRevision("PENDING_APPROVAL"), false);

console.log("silaRecipeRevision.test.ts: ok");
