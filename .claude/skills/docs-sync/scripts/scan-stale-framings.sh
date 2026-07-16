#!/bin/zsh
# Flag present-tense mentions of retired tech in the docs. Advisory: history/log pages may
# legitimately reference them; judge each hit.
# usage: scan-stale-framings.sh [docsroot]
set -u
DOCS="${1:-AppEggShellGallery/public-dev/docs}"
rg -n \
  --glob '!knowledge-base/engineering-log.md' \
  --glob '!modernization/reactxp-to-rnw.md' \
  -e 'ReactXP' -e '\bRX\.' -e 'Fable 4' -e '\.NET 7' -e 'react-native 0\.7' -e 'RNGH 2\b' \
  "$DOCS" && echo "(judge each: retired-tech mention may be historical)" || echo "OK: no stale framings"
