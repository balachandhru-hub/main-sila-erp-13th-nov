# 21. Defect Register

**Menu Engineering P0 / P1 Open Defects:** **0** (All identified ME P0/P1 defects resolved during Phase ME.5).

## 1. Closed Phase ME.5 Defects
| ID | Severity | Component | Description | Resolution | Status |
|---|---|---|---|---|---|
| `ME5-001` | **P0 (Security)** | `MenuEngineeringStudioService.Core.cs` | `GetWorkspaceAsync(menuPlanId)` lacked explicit ownership check on `menuPlanId` against `ctx.Buyer.Id` | Added tenant ownership check throwing `NotFoundCustomException`; verified in `Gate5_Gate6` | **CLOSED** |
| `ME5-002` | **P0 (Security)** | `MenuEngineeringDigitalTwinService.Optimization.cs` | `RunMultiObjectiveOptimizationAsync` did not explicitly verify `request.MenuPlanId` / `BaselineSnapshotId` ownership | Added tenant ownership validation; verified in `Gate5_Gate6` | **CLOSED** |
| `ME5-003` | **P1 (Data Integrity)** | `MenuEngineeringStudioService.Core.cs` | `PreviewCustomerDataMigrationAsync` only matched `Code` vs `Name` inconsistently when checking approved master items | Matched both `MenuItemCode` and `Name` case-insensitively and read `Code`/`MenuItemCode`; verified in `Gate5_Gate6` | **CLOSED** |
| `ME5-004` | **P1 (AI Security)** | `MenuEngineeringAiNativeService.*`, `MenuEngineeringStudioService.AiAndSetup.cs` | Prompt-injection guard needed hardening across Contextual AI, NL Menu Generation, and NL What-If Simulation | Unified `IsAdversarialPrompt` guard + deterministic rejection (`Gate3_AiAccuracyAndSecurity`) | **CLOSED** |
| `ME5-005` | **P2 (API Docs)** | `Buyer.API/Program.cs` | Duplicate nested `ApprovalDecisionBody` class names crashed Swagger generation | Configured `CustomSchemaIds` with full nested type names | **CLOSED** |

## 2. Pre-Existing / External Non-ME Open Items (Documented Honestly)
| ID | Severity | Area | Description | Impact on ME.5 |
|---|---|---|---|---|
| `EXT-001` | **P2 (Pre-existing)** | `Buyer.Tests` (Non-ME) | 15 pre-existing non-ME tests fail (`SilaRecipeReadinessTests` 8, `SilaTransferPlant301302Tests` 2, `SilaTransferIto311312Tests` 2, `SilaTransferSapTests` 2, `SilaReceivingParityTests` 1) | Zero ME failures (ME suite 78/78 PASS) |
| `EXT-002` | **P2 (Pre-existing)** | `Buyer.API` Swagger | `SilaStockCountController.IdentifyPhoto` uses `[FromForm] IFormFile` without `[FromForm]` parameter wrapper, causing `/swagger/v1/swagger.json` HTTP 500 | Unrelated stock-count controller; ME controllers valid |
| `EXT-003` | **P3 (Pre-existing)** | Frontend `tsc -b` | Strict TS errors in legacy `remote-buyer` / `remote-mobile` / `host-app` files (`menuEngineeringTwinApi.ts`, `silaCulinaryFinancialApi.ts`, `silaMenuEngineeringApi.ts` `readError(e)` 0-arg helper) | `vite build` passes cleanly; 0 errors in Phase ME.5 edited files |
| `EXT-004` | **External Blocker** | ERP / POS / PMS | Live SAP S/4HANA, Ariba, OPERA PMS, MICROS POS, Teams connectivity unverified on isolated VM | Required before `READY FOR PRODUCTION APPROVAL` |
