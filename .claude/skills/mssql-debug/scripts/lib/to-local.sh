#!/bin/zsh
# Convert UTC ISO timestamps to local TZ.
# usage: to-local.sh <utc-iso>...
# usage: echo "2025-01-01T12:00:00Z" | to-local.sh
set -u

convert() {
  local ts="$1"
  local out
  if out=$(date -j -u -f '%Y-%m-%dT%H:%M:%S' "${ts:0:19}" +'%Y-%m-%dT%H:%M:%S%z' 2>/dev/null); then
    date -j -f '%Y-%m-%dT%H:%M:%S%z' "$out" +'%Y-%m-%d %H:%M:%S %Z (%z)'
  else
    date -d "$ts" +'%Y-%m-%d %H:%M:%S %Z (%z)' 2>/dev/null || echo "FAIL: cannot parse $ts" >&2
  fi
}

if [[ $# -eq 0 ]]; then
  while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    printf '%s -> %s\n' "$line" "$(convert "$line")"
  done
else
  for ts in "$@"; do
    printf '%s -> %s\n' "$ts" "$(convert "$ts")"
  done
fi
