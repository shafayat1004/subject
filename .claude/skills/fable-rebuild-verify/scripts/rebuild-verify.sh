#!/bin/zsh
# Prove a Fable rebuild reached the build output. See runbooks/build-rebuild.md.
# usage: rebuild-verify.sh <projdir> <native|web> [--wipe] [changed.fs ...]
set -u
usage() { echo "usage: rebuild-verify.sh <projdir> <native|web> [--wipe] [changed.fs ...]"; exit 2; }
PROJ="${1:-}"; PLAT="${2:-}"
[[ -d "$PROJ" && ( "$PLAT" == "native" || "$PLAT" == "web" ) ]] || usage
shift 2
WIPE=0; typeset -a FILES; FILES=()
for a in "$@"; do [[ "$a" == "--wipe" ]] && WIPE=1 || FILES+=("$a"); done

"$(dirname "$0")/clean-stray-fable-output.sh" --check "$PROJ"

BUILD="$PROJ/.build/$PLAT"
if [[ $WIPE -eq 1 ]]; then
  echo "wiping $BUILD/fable"
  rm -rf "$BUILD/fable"
  echo "now restart the build (eggshell dev-$PLAT / build-lib), then re-run without --wipe"
  exit 0
fi
[[ ${#FILES[@]} -gt 0 ]] || { echo "no changed .fs files given; nothing to verify"; exit 2; }

OUT="$BUILD/commonjs"; [[ "$PLAT" == "web" ]] && OUT="$BUILD/fable"
[[ -d "$OUT" ]] || { echo "FAIL: build output dir $OUT missing (build never ran?)"; exit 0; }

for f in "${FILES[@]}"; do touch "$f"; done
echo "touched ${#FILES[@]} file(s); polling $OUT for fresher emitted JS (90s max)..."
FAIL=0
for f in "${FILES[@]}"; do
  base="$(basename "$f" .fs)"
  js="$(find "$OUT" -name "$base.js" 2>/dev/null | head -1)"
  if [[ -z "$js" ]]; then echo "FAIL: no emitted $base.js under $OUT"; FAIL=1; continue; fi
  ok=0
  for i in {1..45}; do
    [[ "$js" -nt "$f" ]] && { ok=1; break; }
    sleep 2
  done
  if [[ $ok -eq 1 ]]; then echo "PASS: $js newer than $f"
  else echo "FAIL: $js still older than $f after 90s (stale cache or watch not running)"; FAIL=1; fi
done
echo "reminder: also confirm the watch log printed 'Started Fable compilation...' and has no 'error FS'"
[[ $FAIL -eq 0 ]] && echo "RESULT: PASS" || echo "RESULT: FAIL (escalate: --wipe, then restart watch + metro --reset-cache)"
