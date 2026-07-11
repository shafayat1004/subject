#!/bin/zsh
# Raw (Tier 1) Android observation helpers. See runbooks/android.md.
# usage: android-observe.sh {screenshot [out.png]|logcat [pattern]|clear|tap X Y|rotate {portrait|landscape}}
set -u
CMD="${1:-}"; shift 2>/dev/null || true
case "$CMD" in
  screenshot)
    OUT="${1:-${TMPDIR:-/tmp}/android-$(date +%H%M%S).png}"
    adb exec-out screencap -p > "$OUT" && echo "$OUT" ;;
  logcat)
    PAT="${1:-FATAL EXCEPTION|Uncaught|ReactNativeJS|error}"
    adb logcat -d | grep -iE "$PAT" | tail -200 ;;
  clear) adb logcat -c && echo "logcat cleared" ;;
  tap) adb shell input tap "$1" "$2" && echo "tapped $1,$2 (brittle; prefer Tier 2 testId)" ;;
  rotate)
    adb shell settings put system accelerometer_rotation 0
    [[ "${1:-portrait}" == "landscape" ]] && R=1 || R=0
    adb shell settings put system user_rotation $R && echo "rotation: ${1:-portrait}" ;;
  *) echo "usage: android-observe.sh {screenshot [out]|logcat [pattern]|clear|tap X Y|rotate {portrait|landscape}}"; exit 2 ;;
esac
