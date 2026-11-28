# 24. Deployment Runbook

## 1. Pre-Deployment Checklist
1. Confirm full SQL Server backup of `SilaBuyerDb` and `sila_platform` control plane (`RPO <= 15 min`).
2. Confirm TLS certificates, JWT signing secrets, and AI provider secrets via environment/secret store (never hardcoded).
3. Verify existing SAP/POS/OPERA customer configuration remains intact (`ApiIntegrationConfiguration`).

## 2. Controlled Rollout Steps
1. **Apply Additive DB Migration:**
   ```bash
   dotnet ef database update --project Backend/src/Modules/Buyer/Buyer.Infrastructure --startup-project Backend/src/Modules/Buyer/Buyer.API
   ```
   Confirm `20261204010000_MenuEngineeringPhase5Readiness` recorded in `__EFMigrationsHistory`.
2. **Start / Restart Python Intelligence Services:**
   - `sila-culinary-engine` on `:8013` -> verify `GET http://127.0.0.1:8013/health` returns `HTTP 200`.
   - `sila-voice-engine` on `:8012` -> verify `GET http://127.0.0.1:8012/health` returns `HTTP 200`.
3. **Deploy & Start `Buyer.API` (`.NET 8`):**
   - Build & start `Buyer.API` on `:8004` (`ASPNETCORE_ENVIRONMENT=Production` or `UAT`).
   - Verify Quartz jobs (`PosSalesPullJob`, `MenuEngineeringAutonomousJob`) register cleanly and protected ME endpoints return `401` without auth.
4. **Deploy Frontend Micro-Frontends (`host-app`, `remote-buyer`, `remote-mobile`):**
   - Build production bundles (`vite build`) and verify `https://<host>/` (Cloud) and `https://<host>/?next=/mobile` (SILA Store) return `HTTP 200`.
5. **Feature Flag Activation (Per Tenant / Property):**
   - Enable `MenuEngineeringEnabled` and validated sub-flags gradually per pilot tenant via `/menu-engineering/setup` without impacting Procurement V1.
