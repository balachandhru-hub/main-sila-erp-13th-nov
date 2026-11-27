# 22. Customer Onboarding Guide

**Backend API:** `GET /api/v1/buyer/sila/me-studio/ai/setup/wizard`, `POST /api/v1/buyer/sila/me-studio/data-migration/templates`, `POST /api/v1/buyer/sila/me-studio/data-migration/preview`, `POST /api/v1/buyer/sila/me-studio/data-migration/commit`  
**Frontend UI:** `/menu-engineering/setup` (20-Step Guided Wizard + Staged Migration Panel + Feature Flag Controls)

## 1. 20-Step Readiness Checklist
1. Customer configuration  
2. Property configuration  
3. Outlet configuration  
4. Kitchen configuration  
5. Ingredient master readiness  
6. Supplier master readiness  
7. Recipe master readiness  
8. Menu item readiness  
9. Ingredient costing readiness  
10. POS configuration  
11. POS mapping readiness  
12. PMS configuration (optional; never blocks unrelated modules)  
13. Inventory configuration  
14. Historical data readiness  
15. Forecasting configuration  
16. AI provider configuration  
17. Agent enablement  
18. Approval configuration  
19. Notification configuration  
20. Production readiness assessment (computes live `OverallReadinessScore`)

## 2. Staged Customer Data Migration
1. Download schema templates from `/api/v1/buyer/sila/me-studio/data-migration/templates` (Ingredients, Suppliers, Recipes, Sub-Recipes, Menu Items, Menu Templates, Portion Standards, Selling Prices, Historical POS Sales, Consumption, Waste, Events).
2. Run **Preview** (`data-migration/preview`) to inspect field validation, UOM validation, duplicates, and protected approved records (`SKIP_EXISTING_APPROVED`).
3. Execute **Commit** (`data-migration/commit`) only after preview verification; `AllowOverwriteApproved` remains `false` by default and requires `ManageMenuSetup` authorization if enabled.
4. Review immutable migration audit records persisted in `data_migration_audits_json`.
