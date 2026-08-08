#!/usr/bin/env bash
# em-dash-scan.sh -- detect the Unicode em-dash (U+2014) in Markdown prose.
# Exit 1 if any .md file under the repo contains the em-dash byte sequence
# (UTF-8: 0xE2 0x80 0x94), else exit 0. Lists offending files.
#
# Enforces AGENTS.md "No em-dash in prose". Called by the em-dash-scan
# custom check in policies/custom-checks.yaml. Lives outside the YAML so
# the byte literal is not mangled by the yaml_min double-quote escaper
# (which over-escapes \xHH sequences, producing a false green).
#
# usage: em-dash-scan.sh
set -uo pipefail
cd "$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Build the 3-byte UTF-8 em-dash via printf; portable across BSD/grep
# (macOS grep lacks -P, so a fixed-string literal is the only portable
# route). The $'...' form is supported by bash/zsh.
needle="$(printf '\xe2\x80\x94')"

hits=()
while IFS= read -r f; do
    if grep -qF "$needle" "$f" 2>/dev/null; then
        hits+=("$f")
    fi
done < <(git ls-files "*.md" 2>/dev/null)

if [[ ${#hits[@]} -gt 0 ]]; then
    echo "em-dash (U+2014) found in ${#hits[@]} file(s):"
    printf '  %s\n' "${hits[@]}"
    exit 1
fi
echo "OK: no em-dash in prose"
exit 0
