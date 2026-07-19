#!/usr/bin/env bash
# upstream-research.sh — list upstream issues matching a symptom across one or more OWNER/REPOs.
# Usage: upstream-research.sh "<symptom keywords>" OWNER1/REPO1 [OWNER2/REPO2 ...]
# Example: upstream-research.sh "F# discriminated union CS0122" dotnet/orleans dotnet/aspnetcore
#
# Part of the spike-driven skill (rule 14 in CLAUDE.md): run this BEFORE writing the .fsproj.
# Prints a one-row-per-match table. For each match, follow up with:
#   gh issue view N --repo OWNER/REPO --comments
# or pass the issue URL directly:
#   gh issue view https://github.com/OWNER/REPO/issues/N --comments
#
# Structured JSON for the catalog doc:
#   gh issue view N -R OWNER/REPO --json title,body,comments --jq '
#     "# " + .title + "\n\n" + .body + "\n\n## Comments\n" +
#     ([.comments[].body] | join("\n\n---\n\n"))'
#
# For private repos verify `gh auth status` first.

set -euo pipefail

if [[ $# -lt 2 ]]; then
    echo "Usage: $0 \"<symptom keywords>\" OWNER1/REPO1 [OWNER2/REPO2 ...]" >&2
    echo "Example: $0 \"F# discriminated union CS0122\" dotnet/orleans" >&2
    exit 2
fi

symptom="$1"
shift

echo "# upstream-research: \"$symptom\""
echo

total=0
for repo in "$@"; do
    echo "## $repo"
    # gh issue list --search supports the same syntax as the GitHub issue search UI.
    # --state all: include closed issues (the answer is often in a closed issue).
    # --limit 30: enough to scan, not so many it's noise.
    if ! matches=$(gh issue list --repo "$repo" --search "$symptom" --state all --limit 30 \
            --json number,title,state,url 2>/dev/null); then
        echo "  (gh issue list failed for $repo -- is gh authenticated? Run: gh auth status)" >&2
        echo
        continue
    fi

    count=$(echo "$matches" | jq 'length')
    if [[ "$count" == "0" ]]; then
        echo "  (no matches)"
        echo
        continue
    fi

    total=$((total + count))
    echo "$matches" | jq -r '.[] | "  - #\(.number) [\(.state)] \(.title)\n    \(.url)"' | sed 's/\\n/\n/g'
    echo
done

echo "# summary: $total match(es) across $# repo(s)"
echo
echo "# next step: read each match with"
echo "#   gh issue view N --repo OWNER/REPO --comments"
echo "# or pass the URL directly:"
echo "#   gh issue view https://github.com/OWNER/REPO/issues/N --comments"
