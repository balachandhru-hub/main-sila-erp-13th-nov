# SILA ERP (Vosox) — Cloud + Mobile

Monorepo for the SILA / Vosox procurement platform:

- **Backend** — .NET 8 microservices (Identity, MasterData, Buyer, Supplier, Ocelot gateway) plus Python OCR/assistant
- **Frontend** — React/Vite microfrontend monorepo, including **remote-mobile** (port 6005)

## Local development

### Frontend (web + mobile remotes)

```bash
cd Frontend
npm ci
npm run dev
```

- Host: `https://localhost:6001`
- Mobile remote: `https://localhost:6005`

### Backend

Requires .NET 8 SDK. Use `ASPNETCORE_ENVIRONMENT=UAT` to point APIs at the shared SQL Server, or configure local connection strings.

```bash
cd Backend
dotnet restore ProcurementSuite.sln
dotnet build ProcurementSuite.sln
```

Cloud Agent bootstrap scripts live in `scripts/cloud-agent-install.sh` and `scripts/cloud-agent-start.sh`.
