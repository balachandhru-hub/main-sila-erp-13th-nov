#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

# --- .NET 8 SDK (cloud backend APIs) ---
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 8.0 --install-dir "$HOME/.dotnet"
fi
sudo ln -sf "$HOME/.dotnet/dotnet" /usr/local/bin/dotnet
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"

# --- Node (frontend + mobile microfrontends) ---
if ! command -v node >/dev/null 2>&1; then
  echo "node is required on PATH" >&2
  exit 1
fi
sudo ln -sf "$(command -v node)" /usr/local/bin/node || true
sudo ln -sf "$(command -v npm)" /usr/local/bin/npm || true

# Dev TLS certs for Vite HTTPS (host + remotes including mobile)
CERT_DIR="$ROOT/Frontend/packages/host-app"
if [[ ! -f "$CERT_DIR/localhost.pem" || ! -f "$CERT_DIR/localhost-key.pem" ]]; then
  openssl req -x509 -newkey rsa:2048 \
    -keyout "$CERT_DIR/localhost-key.pem" \
    -out "$CERT_DIR/localhost.pem" \
    -days 365 -nodes -subj "/CN=localhost" >/dev/null 2>&1
fi

# Frontend monorepo (host, buyer, supplier, platform-user, mobile)
if [[ -f Frontend/package-lock.json ]]; then
  (cd Frontend && npm ci)
else
  (cd Frontend && npm install)
fi

# .NET restore for ProcurementSuite
if [[ -f Backend/ProcurementSuite.sln ]]; then
  (cd Backend && dotnet restore ProcurementSuite.sln)
fi

# Python OCR / assistant service deps
if [[ -f Backend/python-backend/requirements.txt ]]; then
  python3 -m pip install --user -r Backend/python-backend/requirements.txt
fi
