#!/bin/zsh
# Release builds per platform. See .claude/skills/release-build/SKILL.md.
# usage: release-build.sh {android|ios|web} <appdir> [--install]
set -u
PLAT="${1:-}"; APP="${2:-}"; INSTALL="${3:-}"
[[ -n "$PLAT" && -d "$APP" ]] || { echo "usage: release-build.sh {android|ios|web} <appdir> [--install]"; exit 2; }
SCRIPT_DIR="$( cd "$( dirname "${(%):-%x}" )" >/dev/null 2>&1 && pwd )"
REPO_ROOT="$( cd "$SCRIPT_DIR/../../../.." >/dev/null 2>&1 && pwd )"
case "$PLAT" in
  android)
    ( cd "$APP/android" && ./gradlew assembleRelease \
        -PMYAPP_RELEASE_STORE_FILE=debug.keystore \
        -PMYAPP_RELEASE_STORE_PASSWORD=android \
        -PMYAPP_RELEASE_KEY_ALIAS=androiddebugkey \
        -PMYAPP_RELEASE_KEY_PASSWORD=android ) || { echo "RESULT: FAIL (gradle)"; exit 0; }
    APK="$APP/android/app/build/outputs/apk/release/app-release.apk"
    [[ -f "$APK" ]] && echo "artifact: $APK ($(du -h "$APK" | cut -f1))" || { echo "RESULT: FAIL (no apk)"; exit 0; }
    if [[ "$INSTALL" == "--install" ]]; then
      adb install -r "$APK" && echo "installed; launch the app WITHOUT Metro to smoke-test"
    fi
    echo "RESULT: PASS" ;;
  ios)
    WS=$(ls "$APP"/ios/*.xcworkspace 2>/dev/null | head -1)
    [[ -n "$WS" ]] || { echo "RESULT: FAIL (no .xcworkspace; pods installed?)"; exit 0; }
    SCHEME=$(xcodebuild -workspace "$WS" -list 2>/dev/null | awk '/Schemes:/{f=1;next} f&&NF{print $1;exit}')
    echo "workspace: $WS scheme: $SCHEME"
    xcodebuild -workspace "$WS" -scheme "$SCHEME" -configuration Release -sdk iphonesimulator build \
      && echo "RESULT: PASS (smoke: install+launch on booted sim with Metro stopped)" \
      || echo "RESULT: FAIL (xcodebuild)" ;;
  web)
    ( cd "$APP" && "$REPO_ROOT/eggshell" package-web ) \
      && echo "RESULT: PASS (smoke: serve dist statically, confirm no dev-server refs / console errors)" \
      || { echo "RESULT: FAIL (package-web)"; exit 0; } ;;
  *) echo "usage: release-build.sh {android|ios|web} <appdir> [--install]"; exit 2 ;;
esac
