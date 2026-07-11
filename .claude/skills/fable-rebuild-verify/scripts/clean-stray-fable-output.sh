#!/bin/zsh
# Finds (and by default deletes) .fs.js/.fs.js.map emitted beside .fs sources by a raw
# `dotnet fable` invocation. Correct Fable output lives under .build/<platform>/.
# usage: clean-stray-fable-output.sh [--check] [root]
set -u
MODE="delete"
[[ "${1:-}" == "--check" ]] && { MODE="check"; shift; }
ROOT="${1:-$(git rev-parse --show-toplevel 2>/dev/null || pwd)}"
STRAYS=$(find "$ROOT" \( -name "*.fs.js" -o -name "*.fs.js.map" \) \
  -not -path "*/.build/*" -not -path "*/node_modules/*" -not -path "*/fable_modules/*" 2>/dev/null)
if [[ -z "$STRAYS" ]]; then
  echo "OK: no stray .fs.js beside sources"
  exit 0
fi
COUNT=$(echo "$STRAYS" | wc -l | tr -d ' ')
echo "FOUND $COUNT stray Fable output file(s) beside sources (symptom of raw 'dotnet fable' run):"
echo "$STRAYS"
if [[ "$MODE" == "delete" ]]; then
  echo "$STRAYS" | while read -r f; do rm "$f"; done
  echo "DELETED $COUNT file(s). Rebuild via eggshell (never 'dotnet fable' directly)."
fi
