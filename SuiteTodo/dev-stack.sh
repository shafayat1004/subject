#!/usr/bin/env bash
# One-command web dev: Docker SQL (optional) + Todo DevelopmentHost + AppTodo dev-web.
# Usage: ./dev-stack.sh up | down | status
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$ROOT/.." && pwd)"
HOST_DIR="$ROOT/Launchers/Dev/DevelopmentHost/src"
APP_DIR="$ROOT/AppTodo"
PID_FILE="$ROOT/.dev-stack.pids"
SA_PASSWORD="${SA_PASSWORD:-EggShell_Dev_123!}"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
EGGSHELL="$REPO_ROOT/eggshell"

ensure_appsettings () {
  local dest="$HOST_DIR/appsettings.Development.json"
  if [[ ! -f "$dest" ]]; then
    echo "Creating $dest from template..."
    cp "$HOST_DIR/template.appsettings.Development.json" "$dest"
  fi
}

ensure_app_initialized () {
  if [[ ! -d "$APP_DIR/node_modules" ]]; then
    echo "Running AppTodo/initialize (first time)..."
    (cd "$APP_DIR" && ./initialize)
  fi
}

start_sql () {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker not found — skipping SQL container. Use local SQL or install Docker."
    return 0
  fi
  echo "Starting SQL (docker compose)..."
  (cd "$ROOT" && docker compose up -d sql)
  echo "Waiting for SQL healthcheck..."
  (cd "$ROOT" && docker compose exec -T sql /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C \
    -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'Todo_Dev') CREATE DATABASE Todo_Dev;" \
    ) || {
      echo "SQL not ready yet; host may fail on first start. Retry ./dev-stack.sh up in a minute."
    }
}

stop_sql () {
  if command -v docker >/dev/null 2>&1 && [[ -f "$ROOT/docker-compose.yml" ]]; then
    (cd "$ROOT" && docker compose stop sql 2>/dev/null) || true
  fi
}

start_host () {
  ensure_appsettings
  echo "Starting DevelopmentHost on http://localhost:5001 ..."
  dotnet run --project "$HOST_DIR/DevelopmentHost.fsproj" &
  echo "host=$!" >> "$PID_FILE"
  sleep 3
}

stop_pids () {
  if [[ -f "$PID_FILE" ]]; then
    while IFS= read -r line; do
      [[ "$line" =~ ^host=(.+)$ ]] && kill "${BASH_REMATCH[1]}" 2>/dev/null || true
    done < "$PID_FILE"
    rm -f "$PID_FILE"
  fi
}

cmd_up () {
  rm -f "$PID_FILE"
  start_sql
  start_host
  ensure_app_initialized
  echo "Starting AppTodo dev-web (Ctrl+C stops webpack; run ./dev-stack.sh down to stop host/SQL)..."
  trap 'stop_pids' EXIT INT TERM
  (cd "$APP_DIR" && "$EGGSHELL" dev-web)
}

cmd_down () {
  stop_pids
  stop_sql
  echo "dev-stack stopped."
}

cmd_status () {
  if [[ -f "$PID_FILE" ]]; then
    echo "PID file:"
    cat "$PID_FILE"
  else
    echo "No dev-stack PID file (host not started by this script)."
  fi
  if command -v docker >/dev/null 2>&1; then
    (cd "$ROOT" && docker compose ps sql 2>/dev/null) || true
  fi
}

case "${1:-up}" in
  up)     cmd_up ;;
  down)   cmd_down ;;
  status) cmd_status ;;
  *)
    echo "Usage: $0 up|down|status"
    exit 1
    ;;
esac
