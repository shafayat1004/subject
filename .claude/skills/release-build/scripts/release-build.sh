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
    # iOS release = archive (UI, see SKILL.md) then wrap the signed .app into an IPA.
    # Run the deterministic preflight first; it must PASS before archiving.
    "$SCRIPT_DIR/ios-preflight.sh" "$APP"; RC=$?
    if [[ $RC -ne 0 ]]; then
      echo "RESULT: FAIL (preflight); fix every FAIL above, then re-run."
      exit 1
    fi
    # Read the target bundle id from the pbxproj so we can match the archive regardless of
    # app/dir/xcodeproj naming case (AppEggShellGallery dir vs AppEggshellGallery.xcworkspace).
    PBX=$(ls -d "$APP"/ios/*.xcodeproj/project.pbxproj 2>/dev/null | grep -v '/Pods.xcodeproj/' | head -1)
    BUNDLE=$(grep -m1 'PRODUCT_BUNDLE_IDENTIFIER' "$PBX" 2>/dev/null | sed -E 's/.*= *//;s/;.*//;s/"//g')
    [[ -n "$BUNDLE" ]] || { echo "RESULT: FAIL (could not read PRODUCT_BUNDLE_IDENTIFIER)"; exit 1; }
    # Find the newest .xcarchive whose .app has this bundle id. Use find (zsh nomatch aborts globs).
    # Sort by mtime, not by path: same-day archive filenames embed a 12h clock time
    # ("1.52 PM" vs "11.52 AM") that sorts wrong lexically, so a path sort can pick a
    # not-newest archive. `stat -f '%m'` (epoch mtime) orders them correctly.
    ARCH=""
    while IFS= read -r a; do
      APP_DIR=$(ls -d "$a/Products/Applications/"*.app 2>/dev/null | head -1)
      [[ -n "$APP_DIR" ]] || continue
      AB=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$APP_DIR/Info.plist" 2>/dev/null)
      if [[ "$AB" == "$BUNDLE" ]]; then ARCH="$a"; break; fi
    done < <(find "$HOME/Library/Developer/Xcode/Archives" -maxdepth 3 -name '*.xcarchive' 2>/dev/null \
      | while IFS= read -r a; do printf '%s %s\n' "$(stat -f '%m' "$a" 2>/dev/null)" "$a"; done \
      | sort -rn | cut -d' ' -f2-)
    if [[ -z "$ARCH" ]]; then
      WS=$(ls -d "$APP"/ios/*.xcworkspace 2>/dev/null | head -1)
      echo "RESULT: FAIL (no .xcarchive for bundle id $BUNDLE under ~/Library/Developer/Xcode/Archives)"
      echo "Open ${WS:-<appdir>/ios/*.xcworkspace} in Xcode, set destination 'Any iOS Device (arm64)', Product -> Archive, then re-run."
      exit 1
    fi
    APP_DIR=$(ls -d "$ARCH/Products/Applications/"*.app 2>/dev/null | head -1)
    codesign --verify --verbose=1 "$APP_DIR" >/dev/null 2>&1 || { echo "RESULT: FAIL ($APP_DIR not validly signed)"; exit 1; }
    [[ -f "$APP_DIR/main.jsbundle" ]] || { echo "RESULT: FAIL (no main.jsbundle in $(basename "$APP_DIR"); Bundle React Native phase did not run)"; exit 1; }
    [[ -f "$APP_DIR/embedded.mobileprovision" ]] || { echo "RESULT: FAIL (no embedded.mobileprovision in $(basename "$APP_DIR"))"; exit 1; }
    OUT_DIR="$APP/dist/ios"; mkdir -p "$OUT_DIR"
    OUT_IPA="$OUT_DIR/${BUNDLE}.ipa"
    WORK=$(mktemp -d); trap 'rm -rf "$WORK"' EXIT
    mkdir -p "$WORK/Payload"; cp -R "$APP_DIR" "$WORK/Payload/"
    ( cd "$WORK" && zip -qr -X "$OUT_IPA" Payload/ -x '*.DS_Store' )
    echo "archive: $ARCH"
    echo "artifact: $OUT_IPA ($(du -h "$OUT_IPA" | cut -f1))"
    echo "bundle id: $BUNDLE; signed; main.jsbundle $(du -h "$APP_DIR/main.jsbundle" | cut -f1)"
    if [[ "$INSTALL" == "--install" ]]; then
      UDID=$(xcrun devicectl list devices 2>/dev/null | awk '/available/{print $3; exit}')
      if [[ -n "$UDID" ]]; then
        xcrun devicectl device install app --device "$UDID" "$OUT_IPA" && echo "installed on $UDID"
      else
        echo "no available device found via 'xcrun devicectl list devices'; install via Xcode -> Window -> Devices and Simulators"
      fi
    fi
    echo "RESULT: PASS (install: drag into Xcode Devices window, or 'xcrun devicectl device install app --device <UDID> $OUT_IPA')" ;;
  web)
    ( cd "$APP" && "$REPO_ROOT/eggshell" package-web ) \
      && echo "RESULT: PASS (smoke: serve dist statically, confirm no dev-server refs / console errors)" \
      || { echo "RESULT: FAIL (package-web)"; exit 0; } ;;
  *) echo "usage: release-build.sh {android|ios|web} <appdir> [--install]"; exit 2 ;;
esac
