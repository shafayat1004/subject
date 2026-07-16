#!/bin/zsh
# iOS simulator observation helpers. See runbooks/ios.md.
# usage: ios-observe.sh {screenshot [out.png]|log [minutes]|launch <bundleid>|terminate <bundleid>|openurl <url>}
set -u
CMD="${1:-}"; shift 2>/dev/null || true
case "$CMD" in
  screenshot)
    OUT="${1:-${TMPDIR:-/tmp}/ios-$(date +%H%M%S).png}"
    xcrun simctl io booted screenshot "$OUT" && echo "$OUT" ;;
  log)
    MIN="${1:-2}"
    xcrun simctl spawn booted log show --last "${MIN}m" --style compact \
      | grep -iE "ReactNativeJS|error|crash|exception" | tail -200 ;;
  launch)
    [[ $# -ge 1 ]] || { echo "usage: ios-observe.sh launch <bundleid>"; exit 2; }
    xcrun simctl launch booted "$1" ;;
  terminate)
    [[ $# -ge 1 ]] || { echo "usage: ios-observe.sh terminate <bundleid>"; exit 2; }
    xcrun simctl terminate booted "$1" 2>/dev/null; echo "terminated $1" ;;
  openurl)
    [[ $# -ge 1 ]] || { echo "usage: ios-observe.sh openurl <url>"; exit 2; }
    xcrun simctl openurl booted "$1" ;;
  *) echo "usage: ios-observe.sh {screenshot [out]|log [minutes]|launch <id>|terminate <id>|openurl <url>}"; exit 2 ;;
esac
