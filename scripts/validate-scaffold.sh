#!/usr/bin/env bash
set -euo pipefail

# validate-scaffold.sh — validate eggshell create-app (BLOCKED: not functional yet)

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "BLOCKED: eggshell create-app is not yet functional (Goal B)." >&2

echo "Intended procedure when unblocked:"
echo "  1. Scaffold throwaway app into /var/folders/zl/0l6kmjv55glg945ss8128djc0000gp/T/opencode"
echo "  2. Verify build (./eggshell build-lib)"
echo "  3. Verify web boot (./eggshell dev-web + HTTP 200)"
echo "  4. Delete throwaway"

exit 3