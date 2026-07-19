#!/usr/bin/env bash
# Claude Code PreToolUse hook: never run `dotnet fable` directly.
set -euo pipefail

INPUT="$(cat)"
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty' 2>/dev/null || true)
[[ -n "$COMMAND" ]] || exit 0

if echo "$COMMAND" | grep -qE '(^|[[:space:];|&])dotnet[[:space:]]+fable([[:space:]]|$)'; then
    echo "BLOCKED: never run 'dotnet fable' directly. Use ./eggshell build-lib, eggshell dev-web, or eggshell dev-native." >&2
    exit 2
fi
exit 0
