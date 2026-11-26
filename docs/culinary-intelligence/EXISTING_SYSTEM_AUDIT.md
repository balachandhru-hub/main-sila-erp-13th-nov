# SILA ME — Existing System Audit (Step 1)

**Audit Date:** 2026-11-26  
**Baseline Branch:** `cursor/legacy-recipe-sheet-message-a9cc` (`3718d89`) → Extended on `cursor/recipe-studio-2-phase0-a9cc`

---

## 1. Frontend Architecture — SILA Cloud (`Frontend/packages/remote-buyer`)

| Area | Actual Verified Files | Reusable Capabilities & Notes |
|---|---|---|
| **Shell & Navigation** | `src/components/BuyerDashboard.tsx`, `src/components/dashboard/navConfig.tsx`, `src/components/silaMe/silaMeNav.tsx`, `src/components/silaMe/SilaMeWorkspace.tsx` | `BUYER_NAV_PATHS` merges `SILA_ME_NAV_PATHS`. `silaMeNav.tsx` defines `Recipe Management` (`silaRecipes`, `silaRecipeApprovals`, `silaSubstitutions`, `silaPos`), `Setup` (`silaMeTracker`, `silaOcrSettings`, etc.). **Guard tests** (`silaRecipeStudioNavGuard.test.ts`, `silaRecipeLegacyUpload.test.ts`, `silaStudiosRestore.test.ts`) enforce no duplicate top-level Recipe Dashboard/Legacy nav entries. |
| **Permissions** | `src/components/silaMe/silaMePermissions.ts`, `src/components/silaMe/silaHomeCapabilities.ts` | `SilaMePermissions` maps Identity permission codes (`MANAGE_SILA_RECIPE`, `VIEW_SILA_RECIPE`, `APPROVE_SILA_RECIPE`, `MANAGE_SILA_MASTER_DATA`, `VIEW_SILA_MENU_ENGINEERING`, `MANAGE_SILA_MENU_ENGINEERING`, `VIEW_SILA_AUDIT`, etc.) to typed boolean flags. |
| **Recipe Studio 1.0** | `src/components/silaMe/recipes/SilaRecipes.tsx`, `silaRecipeRoutes.ts`, `SilaRecipeWorkspaceShell.tsx`, `SilaRecipeStudioPresentation.tsx`, `SilaRecipeDetailView.tsx`, `SilaRecipeCatalogGrid.tsx`, `SilaRecipeTable.tsx`, `SilaRecipeCosting.tsx`, `SilaRecipeCostSummary.tsx`, `SilaRecipeApprovalTrail.tsx`, `SilaRecipePicture.tsx`, `SilaRecipeExcelPanel.tsx`, `SilaRecipeApprovals.tsx` | Full catalog, `/sila-recipes/new`, `/sila-recipes/:id`, `/sila-recipes/:id/edit`, `/sila-recipes/legacy-upload`, sub-recipes, batch recipes, versioning, costing, outlet prices, picture upload/retrieval, approval trail, and Excel import/export. |
| **Legacy Recipe Upload** | `src/components/silaMe/recipes/SilaRecipeLegacyUpload.tsx`, `src/api/silaMe/silaRecipeLegacyApi.ts` | Isolated migration workflow: Upload → Validation & Preview → Pending Approval → Ready to Post → Image Assignment → Publishing → Upload History. |
| **Menu Engineering** | `src/components/silaMe/menuEngineering/SilaMenuEngineeringApp.tsx`, `src/api/silaMe/silaMenuEngineeringApi.ts` | Existing Menu Engineering workspace (14 tabs including menu items, recipes, demand forecast, engineering classification, POS mapping, simulator, settings). |
| **AI / OCR Setup Pattern** | `src/components/silaMe/receiving/SilaOcrSettings.tsx`, `src/api/silaMe/silaReceivingApi.ts` | Existing buyer-scoped AI/OCR settings pattern (`GET/PUT /api/v1/buyer/sila/ocr-configuration`). |
| **Transaction Tracker** | `src/components/silaMe/trackers/SilaConsolidatedTracker.tsx`, `src/api/silaMe/silaTrackerApi.ts` | Unified `SILA_ME`, `INVENTORY`, `PURCHASING` transaction tracker with CSV export and ERP payload inspection. |
| **Design System & Charts** | `@vosox/shared-ui` (`PageHeader`, `KpiCard`, `KpiStrip`, `Charts`, `Modal`, `EmptyState`, `Loader`, `Pagination`, `Table`, `Card`, `toastService`), `recharts` `3.10.1`, `lucide-react` `1.29.0`, `src/components/silaMe/silaMeTheme.css` | Bright light hospitality enterprise styling scoped under `.sila-me` (`#0f172a` ink, `#ffffff` cards, `#2563eb`/`#0ea5e9`/`#6366f1` accents). |

---

## 2. Frontend Architecture — SILA Mobile (`Frontend/packages/remote-mobile`)

| Area | Actual Verified Files | Reusable Capabilities & Notes |
|---|---|---|
| **Router & Access** | `src/MobileApp.tsx`, `src/access.tsx`, `src/api.ts` | Bearer token auth, role/permission-gated navigation, offline banner support. |
| **Recipe Studio Mobile** | `src/screens/recipes/RecipeStudioHomeScreen.tsx`, `RecipesScreen.tsx`, `RecipeDetailScreen.tsx`, `RecipeDraftWizardScreen.tsx`, `RecipeDraftsScreen.tsx`, `RecipeApprovalsScreen.tsx`, `recipeStudioMath.ts`, `recipeStudioHelpers.ts` | Search, FOOD/BEVERAGE/MINIBAR/TOBACCO chips, quick draft wizard, read-only portion scaling, live stock availability check, picture upload, approvals. Guarded by `silaMobileRecipeStudio.test.ts`. |
| **Home / More / Voice** | `src/screens/home/HomeScreen.tsx`, `src/screens/more/MoreScreen.tsx`, `src/screens/purchasing/SilaVoicePoScreen.tsx` | Mobile quick actions and SILA Voice 2.0 PO assistant. |

---

## 3. Backend Architecture — .NET 8 Buyer Module (`Backend/src/Modules/Buyer`)

| Domain | Actual Entities (`Buyer.Domain/Entities`) | Actual Controllers (`Buyer.API/Controller`) | Notes & Gaps |
|---|---|---|---|
| **Recipe Master & Legacy** | `Recipe`, `RecipeIngredient`, `RecipeOutletPrice`, `RecipeFamily`, `RecipeCategory`, `RecipeLegacyBatch`, `RecipeLegacyRecipe`, `RecipeLegacyMaterialMap`, `RecipeLegacyImage`, `RecipeSubstitutionProposal` | `SilaRecipeController`, `SilaRecipeCatalogController`, `SilaRecipeMasterController`, `SilaRecipeLegacyController`, `SilaSubstitutionController` | Authoritative recipe ID/code sequence (`RI00000001`), versioning, approval engine (`SilaApprovals`), picture storage, legacy migration staging. |
| **Material & Pricing** | `ItemBuyerMaster`, `MaterialOutletPrice`, `MaterialPriceChange`, `MaterialUomConversion`, `MaterialRecognitionMemory` | `SilaMaterialController`, `SilaMaterialRecognitionController`, `SilaMaterialPriceController` | Tenant-isolated material master, UOM conversion (`UomConverter`), price change approvals. *Gap (Phase 1C):* Dedicated `MaterialCreationRequest` workflow entity. |
| **Menu Engineering** | `MenuItem`, `MenuPlan`, `MenuPlanLine`, `MenuPosMapping`, `PmsDemandSnapshot`, `MenuEngineeringSettings` | `SilaMenuEngineeringController` | Existing menu plans & PMS demand snapshots. *Gap (Phase 2A):* Full Banquet/Event Master (`EventMaster`) and multi-course Menu Studio linking. |
| **Inventory & Transfers** | `InventoryLocation`, `InventoryBalance`, `InventoryTransaction`, `InventoryAlert`, `InternalTransferOrder`, `GoodsIssue`, `StockAdjustment`, `StockCount`, `PhysicalInventoryRequest` | `SilaInventoryController`, `SilaInventoryControlController`, `SilaTransferController`, `SilaGoodsIssueController`, `SilaStockCountController`, `SilaAdjustmentController` | Immutable `InventoryLedger`, 311/312 ITO transfers, Z01/Z02 wastage/adjustment mappings, batch/expiry tracking. *Gap (Phase 2C):* `StockReservation` entity for atomic event-level stock holds. |
| **Procurement & POs** | `PurchaseOrder`, `PurchaseOrderItem`, `InternalPurchaseRequest`, `WeeklyBucket`, `SilaUserPurchasingProfile`, `SilaUserPoFlexConfig` | `PurchaseOrderController`, `SilaUserPurchasingProfileController`, `SilaUserPoFlexDefaultsController`, `SilaErpPostingController` | Existing PO creation API, user purchasing defaults, cost center / G/L account assignment. |
| **AI, Voice & Security** | `SilaOcrConfiguration`, `ApiIntegrationConfiguration`, `InventoryWorkflowEvent` | `SilaPoImportController` (`/ocr-configuration`), `SilaVoiceEngineController`, `SilaVoicePoController`, `SilaTrackerController`, `IntegrationController` | `IIntegrationCredentialProtector` (`IDataProtectionProvider` persisted to `buyer/dataprotection-keys` + `Encryption:AesKey`), `IAesEncryption`, `InventoryWorkflowEvent` audit log. *Gap addressed in Phase 0:* `SilaCulinaryAiConfiguration` & Culinary AI provider router. |
| **Identity Permissions** | `Feature.csv` (`Backend/src/Platform/Identity/Identity.Infrastructure/Migrations/Feature.csv`) | `ApiAuthorizationAttribute` | Existing `VIEW_SILA_RECIPE`, `MANAGE_SILA_RECIPE`, `APPROVE_SILA_RECIPE`, `MANAGE_SILA_MASTER_DATA`, `VIEW_SILA_MENU_ENGINEERING`, `MANAGE_SILA_MENU_ENGINEERING`, `VIEW_SILA_AUDIT`. |

---

## 4. Python Services Infrastructure

| Service | Location | Runtime & Libraries | Role |
|---|---|---|---|
| **OCR & Assistant** | `Backend/python-backend/` | Python 3.12, FastAPI `0.139.2` | Invoice OCR & buyer assistant (`InterCallService:OcrUrl`, port `8010`). |
| **SILA Voice Engine** | `Backend/sila-voice-engine/` | Python 3.12, FastAPI `0.115.6`, Pydantic `2.10.4`, NumPy `2.2.1`, RapidFuzz `3.11.0`, httpx `0.28.1`, pytest `8.3.4` | Continuous Voice 2.0 engine (`InterCallService:VoiceEngineUrl`, port `8012`), `RulesLlm` + `OllamaLlm`. |
| **Culinary Intelligence Engine (New Phase 0)** | `Backend/sila-culinary-engine/` | Isolated modular FastAPI + Pydantic v2 + Decimal + NumPy + RapidFuzz + httpx | Deterministic scaling/MRP/P&L engines and AI Provider Router (`Local`, `Gemini`, `Claude`, `Hybrid`) without replacing SILA backend. |
