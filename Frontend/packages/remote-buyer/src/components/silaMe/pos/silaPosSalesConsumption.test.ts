/**
 * POS Sales Consumption contract checks (no Vite/axios runtime).
 * Run: npx --yes tsx src/components/silaMe/pos/silaPosSalesConsumption.test.ts
 */
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const read = (name: string) => readFileSync(join(__dirname, name), "utf8");
const readApi = () => readFileSync(join(__dirname, "../../../api/silaMe/silaPosApi.ts"), "utf8");
const readBackend = (rel: string) =>
  readFileSync(join(__dirname, "../../../../../../../Backend/src/Modules/Buyer", rel), "utf8");

const templateHandler = readBackend("Buyer.Application/Features/Queries/GetSilaPosTemplate/GetSilaPosTemplateQueryHandler.cs");
assert.match(templateHandler, /BuildSalesTemplate/);

const files = readBackend("Buyer.Application/Features/Shared/SilaPosFiles.cs");
assert.match(files, /BuildSalesTemplate/);
assert.match(files, /SourceSystem/);
assert.match(files, /TransactionType/);
assert.match(files, /OriginalTransactionId/);
assert.match(files, /FieldGuide/);
assert.match(files, /SalesData/);
assert.match(files, /34567/);
assert.match(files, /FL10/);

const rows = readBackend("Buyer.Application/Features/Shared/SilaPosRows.cs");
assert.match(rows, /Negative Qty requires TransactionType RETURN or VOID/);
assert.match(rows, /OriginalTransactionId is required for RETURN or VOID/);

const processCmd = readBackend("Buyer.Application/Features/Commands/ProcessSilaPosBatch/ProcessSilaPosBatchCommand.cs");
assert.match(processCmd, /TransactionIds/);
assert.match(processCmd, /QueueErpPosting/);

const controller = readBackend("Buyer.API/Controller/SilaPosController.cs");
assert.match(controller, /consumption-preview/);
assert.match(controller, /PreviewSilaPosConsumption/);

const api = readApi();
assert.match(api, /previewPosConsumption/);
assert.match(api, /saleTransactionId/);
assert.match(api, /queueErpPosting/);

const upload = read("SilaPosSalesUpload.tsx");
assert.match(upload, /Download POS Template/);
assert.match(upload, /handlePreviewConsumption/);
assert.match(upload, /TransactionType/);
assert.match(upload, /studioMode/);
assert.match(upload, /pstudio-drop/);

const previewTable = read("SilaPosPreviewTable.tsx");
assert.match(previewTable, /Select all ready/);
assert.match(previewTable, /Consumption preview/);
assert.match(previewTable, /Post selected/);
assert.match(previewTable, /pos-consumption-preview/);
assert.match(previewTable, /pstudio-line-drawer/);
assert.match(previewTable, /Clear selection/);

const integration = read("SilaPosIntegration.tsx");
assert.match(integration, /SILA POS Studio/);
assert.match(integration, /Confirm posting/);
assert.match(integration, /SilaPosStudioExceptions/);
assert.match(integration, /queueErpPosting:\s*true/);
assert.match(integration, /Validate & update locally/);
assert.match(integration, /Call SAP API/);
assert.match(integration, /Transaction Tracker/);
assert.match(integration, /FIVE_POS_UPDATE · Z01 \/ 03/);

const sap = readBackend("Buyer.Application/Features/Shared/SilaInventorySap.cs");
assert.match(sap, /PosConsumption = new Rule\(\s*\n?\s*SilaStockCountSap\.API_CONFIG_NAME/);
assert.match(sap, /TYPE_Z01/);
assert.doesNotMatch(sap, /PosConsumption = new Rule\([^)]*TYPE_Z02/);

const stockCount = readBackend("Buyer.Application/Features/Shared/SilaStockCountSap.cs");
assert.match(stockCount, /FIVE_POS_UPDATE/);

const posPayload = readBackend("Buyer.Application/Features/Shared/SilaPosErpPayload.cs");
assert.match(posPayload, /Actual SAP request was not captured/);
assert.match(posPayload, /IsCapturedSapBody/);
assert.match(posPayload, /RebuildRequestAsync/);
assert.match(posPayload, /to_MaterialDocumentItem/);
assert.doesNotMatch(posPayload, /\["note"\]\s*=/);
assert.doesNotMatch(posPayload, /\["saleId"\]/);
assert.doesNotMatch(posPayload, /\["posCode"\]/);

const profiles = readFileSync(join(__dirname, "../../../api/integrationsApi.ts"), "utf8");
assert.match(profiles, /configurationName: "FIVE_POS_UPDATE"/);
assert.match(profiles, /goodsMovementType: "Z01"/);
assert.doesNotMatch(profiles, /POS Sales Consumption \(Z02\)/);

const studioCss = read("SilaPosStudio.css");
assert.match(studioCss, /\.pstudio-page/);
assert.match(studioCss, /\.pstudio-drop/);
assert.match(studioCss, /\.pstudio-lifecycle/);

console.log("silaPosSalesConsumption.test.ts: ok");
