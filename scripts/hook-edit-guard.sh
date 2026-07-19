#!/usr/bin/env bash
# Claude Code PreToolUse hook: block forbidden file edits.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INPUT="$(cat)"
FILEPATH=$(echo "$INPUT" | jq -r '.tool_input.file_path // empty' 2>/dev/null || true)
[[ -n "$FILEPATH" ]] || exit 0

out=$(cd "$ROOT" && ./scripts/forbidden-path-guard.sh "$FILEPATH" 2>&1) || {
    echo "$out" >&2
    exit 2
}
exit 0
