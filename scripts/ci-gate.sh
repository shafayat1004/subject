#!/usr/bin/env bash
# Aggregated CI gate. Runs path guard, format check, style-leak audit, a11y audit.
# usage: ci-gate.sh --staged | <path...>
set -euo pipefail

cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

rm -f /tmp/eggshell-ci-*.out

files=()
if [[ "${1:-}" == "--staged" ]]; then
    while IFS= read -r line; do files+=("$line"); done < <(git diff --cached --name-only --diff-filter=ACMR)
else
    if [[ $# -eq 0 ]]; then
        echo "usage: ${0##*/} --staged | <file-or-dir...>" >&2
        exit 2
    fi
    files=("$@")
fi

fmt_files=()
for ((i = 0; i < ${#files[@]}; i++)); do
    f="${files[i]}"
    [[ "$f" == *.fs ]] && fmt_files+=("$f")
done

OVERALL=0

status_for_rc() {
    local rc="$1"
    if [[ $rc -eq 0 ]]; then
        echo PASS
    elif [[ $rc -eq 127 ]]; then
        echo SKIP
    else
        echo FAIL
    fi
}

run_cmd() {
    local label="$1"; shift
    set +e
    "$@" >/tmp/eggshell-ci-"$label".out 2>&1
    local rc=$?
    set -e
    echo "$rc"
}

fmt_available() {
    command -v dotnet >/dev/null 2>&1 && dotnet tool list --local 2>/dev/null | grep -q eggshell-fmt
}

node_available() {
    command -v node >/dev/null 2>&1
}

# Gate 1: forbidden path guard
if [[ ${#files[@]} -gt 0 ]]; then
    rc=$(run_cmd pathguard ./scripts/forbidden-path-guard.sh "${files[@]}")
else
    rc=0
fi
s1=$(status_for_rc "$rc")
[[ $rc -eq 0 || $rc -eq 127 ]] || OVERALL=1

# Gate 2: F# formatting
if [[ ${#fmt_files[@]} -gt 0 ]] && fmt_available; then
    rc=$(run_cmd fmt dotnet tool run eggshell-fmt -- --check "${fmt_files[@]}")
else
    rc=127
fi
s2=$(status_for_rc "$rc")
[[ $rc -eq 0 || $rc -eq 127 ]] || OVERALL=1

# Gate 3: style-leak audit (advisory; missing = SKIP)
STYLE_SCAN=".claude/skills/style-leak-audit/scripts/scan-style-leaks.mjs"
if [[ -f "$STYLE_SCAN" ]] && node_available && [[ ${#fmt_files[@]} -gt 0 ]]; then
    rc=$(run_cmd style node "$STYLE_SCAN" "${fmt_files[@]}")
else
    rc=127
fi
s3=$(status_for_rc "$rc")
[[ $rc -eq 0 || $rc -eq 127 ]] || OVERALL=1

# Gate 4: a11y audit (advisory; missing = SKIP)
A11Y_SCAN=".claude/skills/a11y-check/scripts/scan-a11y.mjs"
if [[ -f "$A11Y_SCAN" ]] && node_available && [[ ${#fmt_files[@]} -gt 0 ]]; then
    rc=$(run_cmd a11y node "$A11Y_SCAN" "${fmt_files[@]}")
else
    rc=127
fi
s4=$(status_for_rc "$rc")
[[ $rc -eq 0 || $rc -eq 127 ]] || OVERALL=1

printf "+------------------+--------+\n"
printf "| %-16s | %-6s |\n" "gate" "status"
printf "+------------------+--------+\n"
printf "| %-16s | %-6s |\n" "forbidden-path" "$s1"
printf "| %-16s | %-6s |\n" "eggshell-fmt" "$s2"
printf "| %-16s | %-6s |\n" "style-leak" "$s3"
printf "| %-16s | %-6s |\n" "a11y" "$s4"
printf "+------------------+--------+\n"

# Print scan output only when there are real findings.
for g in style a11y; do
    if [[ -f "/tmp/eggshell-ci-$g.out" ]] && [[ -s "/tmp/eggshell-ci-$g.out" ]] && ! grep -q '^OK:' "/tmp/eggshell-ci-$g.out"; then
        echo "--- $g scan output ---"
        cat "/tmp/eggshell-ci-$g.out"
    fi
done

if [[ $OVERALL -ne 0 ]]; then
    exit 1
fi
