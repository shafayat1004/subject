#!/bin/zsh
# Conn string resolver. Usage: conn.sh [appsettings_path]
set -u
REPO_ROOT="/Volumes/HomeX/shafayat/Code/subject"
DEFAULT_SETTINGS="$REPO_ROOT/SuiteTodo/Launchers/Dev/DevelopmentHost/src/appsettings.Development.json"
SETTINGS="${1:-$DEFAULT_SETTINGS}"

if [[ -n "${MSSQL_CONN:-}" ]]; then
  echo "$MSSQL_CONN"
  exit 0
fi

if [[ ! -f "$SETTINGS" ]]; then
  echo "FAIL: no conn string. Set MSSQL_CONN or pass appsettings path. Searched: $SETTINGS" >&2
  exit 1
fi

if command -v jq >/dev/null 2>&1; then
  jq -r '.["Storage.SqlServer"].ConnectionString // empty' "$SETTINGS" 2>/dev/null | grep -v '^$' && exit 0
fi

# fallback grep+sed
grep -o '"ConnectionString"[[:space:]]*:[[:space:]]*"[^"]*"' "$SETTINGS" | head -1 | sed 's/"ConnectionString"[[:space:]]*:[[:space:]]*"//;s/"$//'
