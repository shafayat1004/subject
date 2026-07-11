#!/bin/zsh
# Capture a timed device session: logs for the whole window + before/after screenshots.
# usage: capture-session.sh {android|ios} [--url <deeplink>] [--app <package-or-bundleid>] [--duration N] [--out <dir>]
set -u
PLAT="${1:-}"; shift 2>/dev/null || true
[[ "$PLAT" == "android" || "$PLAT" == "ios" ]] || { echo "usage: capture-session.sh {android|ios} [--url u] [--app id] [--duration N] [--out dir]"; exit 2; }
URL=""; APPID=""; DUR=20; OUT="${TMPDIR:-/tmp}/capture-$(date +%H%M%S)"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --url) URL="$2"; shift 2 ;;
    --app) APPID="$2"; shift 2 ;;
    --duration) DUR="$2"; shift 2 ;;
    --out) OUT="$2"; shift 2 ;;
    *) shift ;;
  esac
done
mkdir -p "$OUT"
SKILLS="$(cd "$(dirname "$0")/../.." && pwd)"

if [[ "$PLAT" == "android" ]]; then
  adb logcat -c
  adb logcat > "$OUT/logcat.txt" & LOGPID=$!
  "$SKILLS/debug-android/scripts/android-observe.sh" screenshot "$OUT/before.png" > /dev/null
  [[ -n "$URL" ]] && adb shell am start -a android.intent.action.VIEW -d "$URL"
  [[ -n "$APPID" && -z "$URL" ]] && adb shell am start -n "$APPID/.MainActivity"
  echo "capturing ${DUR}s (interact with the device now)..."
  sleep "$DUR"
  "$SKILLS/debug-android/scripts/android-observe.sh" screenshot "$OUT/after.png" > /dev/null
  kill $LOGPID 2>/dev/null
  LOG="$OUT/logcat.txt"
else
  "$SKILLS/debug-ios/scripts/ios-observe.sh" screenshot "$OUT/before.png" > /dev/null
  [[ -n "$URL" ]] && xcrun simctl openurl booted "$URL"
  [[ -n "$APPID" && -z "$URL" ]] && xcrun simctl launch booted "$APPID"
  echo "capturing ${DUR}s (interact with the simulator now)..."
  sleep "$DUR"
  "$SKILLS/debug-ios/scripts/ios-observe.sh" screenshot "$OUT/after.png" > /dev/null
  xcrun simctl spawn booted log show --last "$((DUR / 60 + 1))m" --style compact > "$OUT/log.txt" 2>/dev/null
  LOG="$OUT/log.txt"
fi

echo "--- summary ($OUT) ---"
echo "screenshots: $OUT/before.png $OUT/after.png"
for bucket in "FATAL EXCEPTION" "Uncaught" "ReactNativeJS" "error"; do
  C=$(grep -ic "$bucket" "$LOG" 2>/dev/null || echo 0)
  echo "$bucket: $C"
done
echo "full log: $LOG (grep the buckets above for detail)"
