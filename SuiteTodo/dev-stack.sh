#!/usr/bin/env bash
# One-command SuiteTodo web dev. Two modes:
#   fake  (default) -- AppTodo dev-web only, in-memory fake todos, no SQL, no backend host.
#   real            -- DevelopmentHost (:5001) + AppTodo dev-web wired to it, backed by SQL.
#                      SQL source is either a local Docker container or an external server.
#
# Usage:
#   ./dev-stack.sh up                         # fake mode (fast; no SQL, no host)
#   ./dev-stack.sh up --real                  # real backend on local Docker SQL
#   ./dev-stack.sh up --real --sql=external \
#        --sql-server=192.168.2.231,1433 --sa-password='Th1sisasafepassword.'
#   ./dev-stack.sh up --real --sql=external --sql-conn='Server=...;Database=...;...'
#   ./dev-stack.sh down                       # stop host (+ Docker SQL if used)
#   ./dev-stack.sh status
#   ./dev-stack.sh validate
#
# Env overrides (same names, minus the --): MODE, SQL, SQL_SERVER, SA_PASSWORD, SQL_DB,
#   SQL_USER, SQL_CONN, BACKEND_PORT, APP_PORT.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$ROOT/.." && pwd)"
HOST_DIR="$ROOT/Launchers/Dev/DevelopmentHost/src"
APP_DIR="$ROOT/AppTodo"
PID_FILE="$ROOT/.dev-stack.pids"

# --- Defaults (overridable by env or flags) ---
MODE="${MODE:-fake}"                       # fake | real
SQL="${SQL:-docker}"                       # docker | external  (only used when MODE=real)
SQL_SERVER="${SQL_SERVER:-}"               # e.g. 192.168.2.231,1433  (external)
SA_PASSWORD="${SA_PASSWORD:-EggShell_Dev_123!}"
SQL_DB="${SQL_DB:-Todo_Dev}"
SQL_USER="${SQL_USER:-sa}"
SQL_CONN="${SQL_CONN:-}"                    # full connection string wins over the pieces above
BACKEND_PORT="${BACKEND_PORT:-5001}"
APP_PORT="${APP_PORT:-9080}"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
DOTNET="$DOTNET_ROOT/dotnet"
EGGSHELL="$REPO_ROOT/eggshell"

# --- Arg parsing (flags after the subcommand) ---
parse_flags () {
  while [[ $# -gt 0 ]]; do
    case "$1" in
      --fake)            MODE="fake" ;;
      --real)            MODE="real" ;;
      --sql=*)           SQL="${1#*=}" ;;
      --sql-server=*)    SQL_SERVER="${1#*=}"; SQL="external" ;;
      --sa-password=*)   SA_PASSWORD="${1#*=}" ;;
      --sql-db=*)        SQL_DB="${1#*=}" ;;
      --sql-user=*)      SQL_USER="${1#*=}" ;;
      --sql-conn=*)      SQL_CONN="${1#*=}"; SQL="external" ;;
      --backend-port=*)  BACKEND_PORT="${1#*=}" ;;
      --app-port=*)      APP_PORT="${1#*=}" ;;
      *) echo "Unknown flag: $1" >&2; exit 2 ;;
    esac
    shift
  done
}

# Build the SQL connection string for the chosen SQL source.
resolve_conn () {
  if [[ -n "$SQL_CONN" ]]; then
    echo "$SQL_CONN"
    return
  fi
  local server
  if [[ "$SQL" == "external" ]]; then
    [[ -n "$SQL_SERVER" ]] || { echo "external SQL requires --sql-server or --sql-conn" >&2; exit 2; }
    server="$SQL_SERVER"
  else
    server="localhost,1433"
  fi
  echo "Server=${server};Database=${SQL_DB};User ID=${SQL_USER};Password=${SA_PASSWORD};Trusted_Connection=False;Encrypt=False;TrustServerCertificate=True;MultipleActiveResultSets=True;"
}

# Write appsettings.Development.json (gitignored) pointing the host at the chosen SQL.
write_appsettings () {
  local conn="$1"
  cat > "$HOST_DIR/appsettings.Development.json" <<EOF
{
  "Storage.SqlServer": {
    "ConnectionString": "${conn}"
  },
  "Orleans.Clustering": {
    "MembershipConnectionString": "${conn}",
    "DevHostSiloPort": 20042,
    "DevHostGatewayPort": 20043
  },
  "DevHost.UnrestrictedApiAccess": true,
  "Http": {
    "Urls": "http://localhost:${BACKEND_PORT}"
  }
}
EOF
}

# Write the frontend runtime config. AppTodo loads the copy under public-dev/ (static root
# referenced by public-dev/index.html); the app-root copy is kept in sync for other builds.
# Pass a backend URL to enable the real backend, or "" to run the in-memory fake service.
write_frontend_config () {
  local backend_url="$1"
  local backend_line
  if [[ -n "$backend_url" ]]; then
    backend_line="eggshell.AppTodo.configSourceOverrides.BackendUrl = \"${backend_url}\";"
  else
    backend_line="// eggshell.AppTodo.configSourceOverrides.BackendUrl = \"http://localhost:${BACKEND_PORT}\";"
  fi
  local dest
  for dest in "$APP_DIR/configSourceOverrides.dev.js" "$APP_DIR/public-dev/configSourceOverrides.dev.js"; do
    cat > "$dest" <<EOF
// Comment out BackendUrl to run using fake in-memory todos, without a backend.
${backend_line}

eggshell.AppTodo.configSourceOverrides.InitializeRnInDevMode = "true";
eggshell.AppTodo.configSourceOverrides.AppUrlBase = location.origin;
EOF
  done
}

ensure_app_initialized () {
  if [[ ! -d "$APP_DIR/node_modules" ]]; then
    echo "Running AppTodo/initialize (first time)..."
    (cd "$APP_DIR" && ./initialize)
  fi
}

start_sql_docker () {
  if ! command -v docker >/dev/null 2>&1; then
    echo "Docker not found. Install Docker or use --sql=external --sql-server=..." >&2
    exit 1
  fi
  echo "Starting SQL (docker compose)..."
  (cd "$ROOT" && docker compose up -d sql)
  echo "Waiting for SQL healthcheck + ensuring ${SQL_DB} exists..."
  (cd "$ROOT" && docker compose exec -T sql /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C \
    -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'${SQL_DB}') CREATE DATABASE [${SQL_DB}];" \
    ) || echo "SQL not ready yet; retry ./dev-stack.sh up --real in a minute."
}

# Best-effort database create for external SQL (needs sqlcmd on PATH; non-fatal otherwise).
ensure_external_db () {
  if command -v sqlcmd >/dev/null 2>&1 && [[ -n "$SQL_SERVER" ]]; then
    echo "Ensuring ${SQL_DB} exists on ${SQL_SERVER}..."
    sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SA_PASSWORD" -C \
      -Q "IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'${SQL_DB}') CREATE DATABASE [${SQL_DB}];" \
      || echo "  Could not auto-create ${SQL_DB}; create it manually if the host fails to start."
  else
    echo "Note: sqlcmd not found -- ensure database [${SQL_DB}] exists on ${SQL_SERVER} before starting."
  fi
}

stop_sql () {
  if command -v docker >/dev/null 2>&1 && [[ -f "$ROOT/docker-compose.yml" ]]; then
    (cd "$ROOT" && docker compose stop sql 2>/dev/null) || true
  fi
}

start_host () {
  echo "Building + starting DevelopmentHost on http://localhost:${BACKEND_PORT} ..."
  local host_bin="$HOST_DIR/bin/Debug/net10.0"
  "$DOTNET" build "$HOST_DIR/DevelopmentHost.fsproj" -c Debug -v q
  if [[ ! -f "$host_bin/DevelopmentHost.dll" ]]; then
    echo "DevelopmentHost build output missing at $host_bin" >&2
    exit 1
  fi
  # ASPNETCORE_URLS is required: the "Http:Urls" appsettings key is NOT wired to Kestrel, so
  # without this the host binds the default :5000 (which also collides with macOS AirPlay).
  (cd "$host_bin" && ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:${BACKEND_PORT}" \
    "$DOTNET" exec DevelopmentHost.dll) &
  echo "host=$!" >> "$PID_FILE"

  echo "Waiting for backend to accept connections..."
  local i
  for i in $(seq 1 45); do
    if curl -sf -o /dev/null --max-time 2 -X POST "http://localhost:${BACKEND_PORT}/api/v1/realTime/negotiate?negotiateVersion=1"; then
      echo "  Backend up."
      return 0
    fi
    sleep 2
  done
  echo "  Backend did not respond in time; check its log output above." >&2
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
  ensure_app_initialized

  if [[ "$MODE" == "fake" ]]; then
    echo "Mode: FAKE (in-memory todos; no SQL, no backend host)."
    write_frontend_config ""
  else
    echo "Mode: REAL (DevelopmentHost + SQL: ${SQL})."
    local conn
    conn="$(resolve_conn)"
    if [[ "$SQL" == "docker" ]]; then
      start_sql_docker
    else
      ensure_external_db
    fi
    write_appsettings "$conn"
    start_host
    write_frontend_config "http://localhost:${BACKEND_PORT}"
  fi

  echo "Starting AppTodo dev-web on http://localhost:${APP_PORT} (Ctrl+C stops webpack; './dev-stack.sh down' stops host/SQL)..."
  trap 'stop_pids' EXIT INT TERM
  (cd "$APP_DIR" && APP_PORT="$APP_PORT" "$EGGSHELL" dev-web)
}

cmd_down () {
  stop_pids
  stop_sql
  echo "dev-stack stopped."
}

cmd_status () {
  if [[ -f "$PID_FILE" ]]; then
    echo "PID file:"; cat "$PID_FILE"
  else
    echo "No dev-stack PID file (host not started by this script)."
  fi
  echo "Frontend config BackendUrl:"
  grep -E "BackendUrl" "$APP_DIR/public-dev/configSourceOverrides.dev.js" 2>/dev/null || echo "  (none)"
  if command -v docker >/dev/null 2>&1; then
    (cd "$ROOT" && docker compose ps sql 2>/dev/null) || true
  fi
}

cmd_validate () {
  echo "Checking DevelopmentHost http://localhost:${BACKEND_PORT} ..."
  if curl -sf -o /dev/null -w "%{http_code}" -X POST "http://localhost:${BACKEND_PORT}/api/v1/realTime/negotiate?negotiateVersion=1" | grep -qE '^[24]'; then
    echo "  Backend negotiate: OK"
  else
    echo "  Backend not reachable. Run: ./dev-stack.sh up --real" >&2
    exit 1
  fi
  echo "Checking AppTodo http://127.0.0.1:${APP_PORT} ..."
  if curl -sf -o /dev/null "http://127.0.0.1:${APP_PORT}/"; then
    echo "  Frontend: OK"
  else
    echo "  Frontend not reachable on port ${APP_PORT}." >&2
    exit 1
  fi
  if [[ -f "$APP_DIR/node_modules/playwright/package.json" ]]; then
    echo "Running Playwright smoke audit..."
    (cd "$APP_DIR" && node audit/audit-todo-web.mjs "http://127.0.0.1:${APP_PORT}")
  else
    echo "Skipping Playwright (npm install in AppTodo first)."
  fi
  echo "Validation passed."
}

subcommand="${1:-up}"
shift || true
parse_flags "$@"

case "$subcommand" in
  up)       cmd_up ;;
  down)     cmd_down ;;
  status)   cmd_status ;;
  validate) cmd_validate ;;
  *)
    echo "Usage: $0 up|down|status|validate [--fake|--real] [--sql=docker|external] [--sql-server=...] [--sa-password=...] [--sql-conn=...]"
    exit 1
    ;;
esac
