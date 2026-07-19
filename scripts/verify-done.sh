#!/usr/bin/env bash
# Evidence-gated build-log check. Do not trust other tools' exit codes.
# usage: verify-done.sh <build-log-file>
set -euo pipefail

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

usage() { echo "usage: ${0##*/} <build-log-file>" >&2; exit 2; }
[[ $# -ge 1 ]] && [[ -f "$1" ]] || usage

LOG="$1"
has() { grep -qiE "$1" "$LOG"; }
fail() { echo "FAIL: $*" >&2; exit 1; }

if has 'error FS|error!'; then
    fail "log contains a compiler/build error"
fi

if has 'precompilation failed'; then
    fail "log contains a precompilation failure"
fi

if has 'Skipped compilation because all generated files are up-to-date'; then
    fail "stale-cache risk: Fable skipped compilation. Touch changed .fs or clear .build/<platform>/fable, then rebuild."
fi

# Only require fresh-compile evidence for Fable-flavoured logs.
if has 'fable'; then
    if ! has 'Started Fable compilation'; then
        fail "Fable log missing fresh-compile evidence ('Started Fable compilation...')"
    fi
    echo "PASS: Fable fresh-compile evidence found; errors absent."
else
    echo "PASS: non-Fable log; errors absent."
fi
