#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$HOME/.dotnet:$HOME/.local/bin:$PATH"

# Idempotent: skip if host already listening
if curl -k -fsS https://localhost:6001/ >/dev/null 2>&1 || curl -fsS http://localhost:6001/ >/dev/null 2>&1; then
  echo "Frontend already up on :6001"
else
  tmux has-session -t frontend 2>/dev/null || \
    tmux new-session -d -s frontend "cd '$ROOT/Frontend' && npm run dev 2>&1 | tee /tmp/frontend-dev.log"
fi

# Optional local Buyer.API (UAT remote SQL) when solution builds succeed
if [[ -f Backend/src/Modules/Buyer/Buyer.API/Buyer.API.csproj ]]; then
  if ! curl -fsS http://localhost:8004/swagger/index.html >/dev/null 2>&1 && \
     ! curl -fsS http://localhost:8004/ >/dev/null 2>&1; then
    tmux has-session -t buyer-api 2>/dev/null || \
      tmux new-session -d -s buyer-api "cd '$ROOT/Backend/src/Modules/Buyer/Buyer.API' && ASPNETCORE_ENVIRONMENT=UAT ASPNETCORE_URLS=http://localhost:8004 dotnet run --no-launch-profile 2>&1 | tee /tmp/buyer-api.log"
  fi
fi

# Wait for frontend readiness
for i in $(seq 1 60); do
  if curl -k -fsS https://localhost:6001/ >/dev/null 2>&1 || curl -fsS http://localhost:6001/ >/dev/null 2>&1; then
    echo "Frontend ready on :6001 (mobile remote :6005)"
    exit 0
  fi
  sleep 2
done
echo "Frontend did not become ready in time; see /tmp/frontend-dev.log" >&2
tail -n 80 /tmp/frontend-dev.log 2>/dev/null || true
exit 1
