#!/usr/bin/env bash
set -euo pipefail

# symptom-match.sh — match error logs against troubleshooting.md and runbooks/*.md
# Usage: symptom-match.sh <error-log-file> or symptom-match.sh "verbatim error string"

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RUNBOOKS_DIR="$REPO_ROOT/AppEggShellGallery/public-dev/docs/runbooks"
TROUBLESHOOTING_FILE="$RUNBOOKS_DIR/troubleshooting.md"

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <error-log-file> or $0 \"verbatim error string\""
  exit 2
fi

INPUT="$1"
if [[ -f "$INPUT" ]]; then
  # Read file content
  CONTENT="$(cat "$INPUT")"
else
  # Treat as verbatim string
  CONTENT="$INPUT"
fi

# Extract candidate keywords: error codes (FS/CS), Exception names, quoted strings, tool names, and standalone words
KEYWORDS=$(echo "$CONTENT" | grep -oE '\b(FS|CS)[0-9]+\b|\b[A-Z][a-zA-Z]+Exception\b|\"[^"]+\"|\b(dotnet|fable|eggshell|metro|yarn|npm|gh|git|compilation|generated|files|up-to-date|skipped)\b' | tr '[:upper:]' '[:lower:]' | sort -u | tr '\n' '|' | sed 's/|$//')

if [[ -z "$KEYWORDS" ]]; then
  echo "No candidate keywords extracted from input."
  exit 0
fi

# Search troubleshooting.md and runbooks/*.md for matches
echo "Matching against keywords: ${KEYWORDS//|/, }"
echo

# Use ripgrep for fast searching
MATCHES=$(rg --no-heading --color never -i -e "$KEYWORDS" "$RUNBOOKS_DIR" || true)

if [[ -z "$MATCHES" ]]; then
  echo "no known symptom match — consider adding a troubleshooting entry after you fix this"
  exit 0
fi

# Group matches by file and count distinct keyword hits per section
declare -a FILE_HITS=()
declare -a SECTION_HITS=()

while IFS= read -r line; do
  FILE=$(echo "$line" | cut -d: -f1)
  LINE_NUM=$(echo "$line" | cut -d: -f2)
  TEXT=$(echo "$line" | cut -d: -f3-)
  
  # Extract section header (last line starting with ## or ### before this line)
  SECTION=$(awk -v line_num="$LINE_NUM" 'NR <= line_num && /^##+ / {sec=$0} END {print sec}' "$FILE" | sed 's/^##* //')
  
  if [[ -z "$SECTION" ]]; then
    SECTION="TOP"
  fi
  
  KEY="$FILE:$SECTION"
  
  # Count file hits
  FOUND_FILE=0
  for ((i=0; i<${#FILE_HITS[@]}; i+=2)); do
    if [[ "${FILE_HITS[$i]}" == "$FILE" ]]; then
      FILE_HITS[$i+1]=$((FILE_HITS[$i+1] + 1))
      FOUND_FILE=1
      break
    fi
  done
  if [[ $FOUND_FILE -eq 0 ]]; then
    FILE_HITS+=("$FILE" "1")
  fi
  
  # Count section hits
  FOUND_SECTION=0
  for ((i=0; i<${#SECTION_HITS[@]}; i+=2)); do
    if [[ "${SECTION_HITS[$i]}" == "$KEY" ]]; then
      SECTION_HITS[$i+1]=$((SECTION_HITS[$i+1] + 1))
      FOUND_SECTION=1
      break
    fi
  done
  if [[ $FOUND_SECTION -eq 0 ]]; then
    SECTION_HITS+=("$KEY" "1")
  fi

done <<< "$MATCHES"

# Sort sections by hit count (descending)
for ((i=0; i<${#SECTION_HITS[@]}; i+=2)); do
  KEY=${SECTION_HITS[$i]}
  COUNT=${SECTION_HITS[$i+1]}
  echo "$COUNT $KEY"
done | sort -nr | while read -r COUNT KEY; do
  FILE="${KEY%:*}"
  SECTION="${KEY#*:}"
  
  echo "=== $SECTION ($COUNT matches) ==="
  echo "File: $FILE"
  echo
  
  # Print matching lines for this section
  rg --no-heading -A2 -B2 -i -e "$KEYWORDS" "$FILE" | grep -i -e "$KEYWORDS" | while IFS= read -r line; do
    echo "  $line"
  done | head -20
  
  if [[ $(rg --no-heading -i -e "$KEYWORDS" "$FILE" | wc -l) -gt 20 ]]; then
    echo "  ... (truncated)"
  fi
  echo

done