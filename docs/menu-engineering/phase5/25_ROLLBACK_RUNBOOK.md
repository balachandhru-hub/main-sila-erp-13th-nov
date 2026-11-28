# 25. Rollback Runbook

## 1. Immediate Feature-Flag Rollback (Zero Downtime — Recommended First Step)
If an issue is isolated to Menu Engineering after rollout, disable ME feature flags per tenant/property via `PUT /api/v1/buyer/sila/me-studio/setup` (or set `AutonomousModePaused = true`). Procurement V1 (`Catalog -> PO -> SO -> GRN -> Invoice -> Credit Memo`) continues uninterrupted (`Gate9` verified).

## 2. Application Rollback
1. Stop updated `Buyer.API`, `sila-culinary-engine`, `sila-voice-engine`, and frontend static bundles.
2. Restore previous release artifacts/containers and restart services.
3. Because `20261204010000_MenuEngineeringPhase5Readiness` only added two nullable columns (`feature_flags_json`, `data_migration_audits_json`), the prior `Buyer.API` release remains binary- and schema-compatible without mandatory DB rollback.

## 3. Database Schema Rollback (If Required)
Only if explicit schema reversion is authorized:
```bash
dotnet ef database update <PreviousMigrationName> --project Backend/src/Modules/Buyer/Buyer.Infrastructure --startup-project Backend/src/Modules/Buyer/Buyer.API
```
Do **not** drop production databases or execute destructive resets.
