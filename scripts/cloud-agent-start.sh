#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$HOME/.dotnet:$HOME/.local/bin:$PATH"

start_api() {
  local name="$1"
  local port="$2"
  local project="$3"
  local log="/tmp/${name}.log"
  if curl -fsS "http://localhost:${port}/swagger/index.html" >/dev/null 2>&1 || \
     curl -fsS "http://localhost:${port}/" >/dev/null 2>&1; then
    echo "${name} already up on :${port}"
    return 0
  fi
  if [[ ! -f "$project" ]]; then
    echo "skip ${name}: missing ${project}"
    return 0
  fi
  tmux has-session -t "$name" 2>/dev/null || \
    tmux new-session -d -s "$name" \
      "cd '$(dirname "$project")' && ASPNETCORE_ENVIRONMENT=UAT ASPNETCORE_URLS=http://localhost:${port} dotnet run --no-launch-profile 2>&1 | tee '${log}'"
  echo "started ${name} on :${port}"
}

# Identity must be up before Buyer/mobile permission calls (InterCallService → :8001).
start_api identity-api 8001 Backend/src/Platform/Identity/Identity.API/Identity.API.csproj
start_api buyer-api 8004 Backend/src/Modules/Buyer/Buyer.API/Buyer.API.csproj

# Frontend host + remotes (including mobile on :6005)
if curl -k -fsS https://localhost:6001/ >/dev/null 2>&1 || curl -fsS http://localhost:6001/ >/dev/null 2>&1; then
  echo "Frontend already up on :6001"
else
  tmux has-session -t frontend 2>/dev/null || \
    tmux new-session -d -s frontend "cd '$ROOT/Frontend' && npm run dev 2>&1 | tee /tmp/frontend-dev.log"
fi

for i in $(seq 1 60); do
  if curl -k -fsS https://localhost:6001/ >/dev/null 2>&1 || curl -fsS http://localhost:6001/ >/dev/null 2>&1; then
    echo "Frontend ready on :6001 (mobile remote :6005)"
    # Best-effort wait for Identity so mobile permission calls succeed
    for j in $(seq 1 30); do
      if curl -fsS http://localhost:8001/swagger/index.html >/dev/null 2>&1 || \
         grep -q 'Now listening on' /tmp/identity-api.log 2>/dev/null; then
        echo "Identity ready on :8001"
        exit 0
      fi
      sleep 2
    done
    echo "Frontend is up; Identity still starting — see /tmp/identity-api.log" >&2
    exit 0
  fi
  sleep 2
done
echo "Frontend did not become ready in time; see /tmp/frontend-dev.log" >&2
tail -n 80 /tmp/frontend-dev.log 2>/dev/null || true
exit 1
