# 10. Enterprise Integration Matrix

**Evidence Classification Policy:** Mocked or offline unit/contract tests are **never** classified as live customer connectivity.

| System | Purpose | Adapter / Implementation | Automated Test Evidence | Evidence Class | Live Connectivity Status |
|---|---|---|---|---|---|
| SAP S/4HANA | Material, Supplier, PO, GRN, GRN Reversal, Invoice, Credit Memo, Transfers | `SilaGrnSap.cs`, `SilaInventoryDifferenceSap.cs`, `SilaTransferSap.cs`, `IntegrationSchedulerJob` | `SilaCreditMemoSapResponseTests`, `SilaCulinaryProcurementServiceTests`, `Gate9` | Unit & Contract Test | **BLOCKED BY EXTERNAL DEPENDENCY** (No live customer SAP tenant on VM) |
| SAP Ariba / Business Network | Procurement/ASN/Supplier SO sync | Existing `ApiIntegrationConfiguration` + `IntegrationEngine` | Existing Integration protocol tests (`IntegrationProtocolMapTests` 8/8) | Unit & Contract Test | **BLOCKED BY EXTERNAL DEPENDENCY** (Requires customer Ariba credentials) |
| OPERA PMS | Occupancy forecast, covers, event sync | `PmsDemandSnapshot`, `MenuEngineeringAutonomousService` | `Test02_OperaOccupancyForecastIncrease` | Unit / Simulated Snapshot Test | **BLOCKED BY EXTERNAL DEPENDENCY** (Requires customer OPERA endpoint) |
| MICROS / Simphony / POS | Sales sync, incremental pull, pagination | `PosSalesPullJob`, `MenuEngineeringStudioService.Analytics.cs` | `Scenario04And05`, `Gate3_PosSemanticMapping` | Unit & Batch Import Test | **BLOCKED BY EXTERNAL DEPENDENCY** (Requires live POS API credentials) |
| SILA Fatoora | E-invoicing compliance | Existing Fatoora integration services (untouched) | Preserved; non-ME regression unaffected | Preserved V1 | N/A for ME (Protected) |
| Microsoft Teams | Approval cards & alert notifications | `MenuEngineeringAutonomousService`, `TeamsNotificationsEnabled` | `Test18_TeamsApprovalAttemptedByUnauthorizedUser` | Unit / Authorization Test | **BLOCKED BY EXTERNAL DEPENDENCY** (Requires Teams webhook/bot secret) |
| Microsoft SharePoint | Document / workbook archival | Existing export/attachment config | N/A | Config Ready | **BLOCKED BY EXTERNAL DEPENDENCY** (Requires tenant SharePoint app) |
| Python Culinary Engine (`:8013`) | Optimization, forecasting, Monte Carlo | `sila-culinary-engine` FastAPI + OR-Tools | `pytest` **88/88 PASS** + live `/health` HTTP 200 | Live Local Integration Test | **PASS (Live on VM `:8013`)** |
| Python Voice Engine (`:8012`) | Culinary voice intents & transcription | `sila-voice-engine` FastAPI (`MENU_ENGINEERING_AGENT`) | `pytest` **10/10 PASS** + live `/health` HTTP 200 | Live Local Integration Test | **PASS (Live on VM `:8012`)** |

**Uncertain Write Reconciliation (`Test09_ExternalErpWriteTimesOut_ReconcilesOutcomeBeforeAnyRetry`):** Verified PASS — timed-out ERP writes enter reconciliation before any retry, preventing duplicate postings.
