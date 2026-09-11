#!/usr/bin/env bash
# Run 33 Label locally with database + migrations (macOS/Linux).
# Usage:
#   ./scripts/run-local.sh            # Docker SQL Server (default on non-Windows)
#   ./scripts/run-local.sh sqlite     # Sqlite smoke (no Docker)
#   ./scripts/run-local.sh docker     # Docker SQL Server + migrations

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
MODE="${1:-docker}"
URLS="${URLS:-http://localhost:5072}"

echo "==> Restoring..."
dotnet restore Label33.sln

case "$MODE" in
  sqlite|Sqlite|SQLITE)
    export ASPNETCORE_ENVIRONMENT=Development
    PROFILE=http
    echo "==> Mode: Sqlite (EnsureCreated on startup)"
    ;;
  docker|DockerSql|sql|*)
    export ASPNETCORE_ENVIRONMENT=DockerSql
    PROFILE=DockerSql
    if ! command -v docker >/dev/null 2>&1; then
      echo "Docker not found. Use: ./scripts/run-local.sh sqlite"
      exit 1
    fi
    echo "==> Mode: Docker SQL Server + EF migrations"
    docker compose up -d sql
    echo "==> Waiting for SQL Server..."
    sleep 25
    ok=0
    for i in $(seq 1 20); do
      if dotnet ef database update \
          --project src/Label33.Infrastructure \
          --startup-project src/Label33.Web; then
        ok=1
        break
      fi
      echo "    retry $i/20..."
      sleep 5
    done
    if [[ "$ok" != "1" ]]; then
      echo "Could not apply migrations to Docker SQL Server."
      exit 1
    fi
    ;;
esac

echo ""
echo "Storefront:  $URLS"
echo "Ops Console: $URLS/ops-33-console/login"
echo "SuperAdmin:  superadmin@33label.local / ChangeMe_33Label!"
echo ""
echo "==> Running web app..."
dotnet run --project src/Label33.Web --launch-profile "$PROFILE" --urls "$URLS"
