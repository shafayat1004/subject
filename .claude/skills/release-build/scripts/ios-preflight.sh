#!/bin/zsh
# Deterministic preflight for an iOS Release archive + IPA export.
# Run BEFORE `Product -> Archive` in Xcode. Every check prints PASS/FAIL/WARN
# with the exact value it tested, and a FAIL exits non-zero with an actionable
# fix. No improvisation: if a hard requirement is not met, stop and tell the
# operator what to do.
#
# usage: ios-preflight.sh <appdir>
# exit: 0 = all hard checks pass (WARN/ADVISORY do not fail); 1 = hard fail; 2 = usage
#
# Checks (in order):
#   1  .xcworkspace exists, pods installed
#   2  scheme resolves via `xcodebuild -list`
#   3  PRODUCT_BUNDLE_IDENTIFIER + DEVELOPMENT_TEAM extracted from pbxproj
#   4  Apple Development codesigning identity present in keychain (count + team IDs)
#   5  DEVELOPMENT_TEAM is logged into Xcode Accounts (else `-allowProvisioningUpdates`/UI archive cannot fetch a profile)
#   6  a provisioning profile for <TEAM>.<BUNDLE_ID> is cached locally (else first archive must generate it)
#   7  Podfile post_install disables pod-target signing (else archive fails "No signing certificate 'iOS Development'")
#   8  Fable native bundle fresh: .build/native/commonjs/Bootstrap.js newer than newest src/*.fs (else stale JS ships)
#   9  ADVISORY: index.js gates configSourceOverrides.native.js on __DEV__ (else Release ships dev config)
#  10  ADVISORY: report CFBundleDisplayName so the home-screen label is not mistaken for a wrong-app reference
set -u
APP="${1:-}"
[[ -n "$APP" && -d "$APP" ]] || { echo "usage: ios-preflight.sh <appdir>"; exit 2; }
[[ -d "$APP/ios" ]] || { echo "FAIL: no $APP/ios directory (not an RN app?)"; exit 1; }
APP_STEM=$(basename "$APP")

PASS=0; FAIL=0; WARN=0
ok()   { printf 'PASS  %s\n' "$1"; PASS=$((PASS+1)); }
no()   { printf 'FAIL  %s\n' "$1"; FAIL=$((FAIL+1)); }
warn() { printf 'WARN  %s\n' "$1"; WARN=$((WARN+1)); }
section() { printf '\n=== %s ===\n' "$1"; }

# ---------- 1. workspace + pods ----------
section "1. workspace & pods"
WS=$(ls -d "$APP"/ios/*.xcworkspace 2>/dev/null | head -1)
if [[ -z "$WS" ]]; then no "no .xcworkspace under $APP/ios (run 'pod install' first)"; else ok "workspace: $WS"; fi
if [[ ! -d "$APP/ios/Pods" ]]; then no "Pods/ missing -- run '(cd ios && pod install)'"; else ok "Pods/ installed"; fi

# ---------- 2. scheme ----------
section "2. scheme"
# Prefer the scheme that matches the app/workspace stem (AppTodo, AppEggshellGallery);
# `xcodebuild -list` prints schemes alphabetically, so the first is often a pod like AppCenter.
WS_STEM=$(basename "$WS" .xcworkspace)
ALL_SCHEMES=$(xcodebuild -workspace "$WS" -list 2>/dev/null | awk '/Schemes:/{f=1;next} /^[A-Za-z]/{if(f)exit} f&&NF{print $1}')
SCHEME=$(printf '%s\n' "$ALL_SCHEMES" | grep -Fx "$WS_STEM" | head -1)
[[ -z "$SCHEME" ]] && SCHEME=$(printf '%s\n' "$ALL_SCHEMES" | grep -Fix "$APP_STEM" | head -1)
[[ -z "$SCHEME" ]] && SCHEME=$(printf '%s\n' "$ALL_SCHEMES" | grep -vE '^(AppCenter|FB|Firebase|Google|hermes|nano|Promises|RCT|React|RN|Yoga|Pods)' | head -1)
[[ -z "$SCHEME" ]] && SCHEME=$(printf '%s\n' "$ALL_SCHEMES" | head -1)
if [[ -z "$SCHEME" ]]; then no "no scheme resolved from 'xcodebuild -workspace $WS -list'"; else ok "scheme: $SCHEME"; fi

# ---------- 3. bundle id + team from pbxproj ----------
section "3. identity (pbxproj)"
# zsh does not glob the RHS of an assignment by default, so resolve explicitly.
# Exclude Pods.xcodeproj; the app project shares the workspace's name stem.
PBX=$(ls -d "$APP"/ios/*.xcodeproj/project.pbxproj 2>/dev/null | grep -v '/Pods.xcodeproj/' | head -1)
if [[ -z "$PBX" ]]; then no "no app .xcodeproj/project.pbxproj under $APP/ios"; else
  BUNDLE=$(grep -m1 'PRODUCT_BUNDLE_IDENTIFIER' "$PBX" 2>/dev/null | sed -E 's/.*= *//;s/;.*//;s/"//g')
  TEAM=$(grep -m1 'DEVELOPMENT_TEAM' "$PBX" 2>/dev/null | sed -E 's/.*= *//;s/;.*//;s/"//g')
fi
if [[ -z "$BUNDLE" ]]; then no "PRODUCT_BUNDLE_IDENTIFIER not found in pbxproj"; else ok "bundle id: $BUNDLE"; fi
if [[ -z "$TEAM" ]]; then no "DEVELOPMENT_TEAM not set in pbxproj (set it in Xcode Signing & Capabilities)"; else ok "team: $TEAM"; fi

# ---------- 4. codesigning identities ----------
section "4. keychain signing identities"
CERTS=$(security find-identity -v -p codesigning 2>/dev/null)
CERT_COUNT=$(printf '%s\n' "$CERTS" | grep -cE 'Apple Development|iPhone Developer')
if [[ "$CERT_COUNT" -eq 0 ]]; then
  no "no 'Apple Development' identity in keychain. Open Xcode -> Settings -> Accounts -> your team -> Manage Certificates -> + (Apple Development). First archive via Xcode UI also generates it for a free personal team."
else
  ok "$CERT_COUNT signing identity(ies) present"
  printf '%s\n' "$CERTS" | grep -E 'Apple Development|iPhone Developer' | sed 's/^/      /'
fi

# ---------- 5. team logged into Xcode ----------
section "5. Apple ID logged into Xcode"
TEAMS_JSON=$(defaults read com.apple.dt.Xcode IDEProvisioningTeamByIdentifier 2>/dev/null)
if printf '%s' "$TEAMS_JSON" | grep -q "$TEAM"; then
  ok "team $TEAM is logged into Xcode Accounts"
else
  no "team $TEAM is NOT logged into Xcode Accounts. Open Xcode -> Settings -> Accounts, add the Apple ID that owns team $TEAM. Without this, '-allowProvisioningUpdates' and UI archive cannot fetch a provisioning profile and fail with 'No Account for Team' / 'No profiles for $BUNDLE were found'."
fi

# ---------- 6. cached provisioning profile ----------
section "6. provisioning profile (cached)"
PROF_DIR="$HOME/Library/Developer/Xcode/UserData/Provisioning Profiles"
PROF_MATCH=""
if [[ -d "$PROF_DIR" ]]; then
  for p in "$PROF_DIR"/*.mobileprovision; do
    [[ -f "$p" ]] || continue
    if security cms -D -i "$p" 2>/dev/null | grep -qa "$TEAM.$BUNDLE"; then PROF_MATCH="$p"; break; fi
  done
fi
if [[ -n "$PROF_MATCH" ]]; then
  ok "profile cached: $(basename "$PROF_MATCH") ($TEAM.$BUNDLE)"
  # report provisioned devices so the operator can confirm the target iPhone is included
  DEVS=$(security cms -D -i "$PROF_MATCH" 2>/dev/null | awk '/ProvisionedDevices/{f=1;next} /\/array/{f=0} f&&/<string>/{gsub(/[^0-9A-F-]/,""); print}' | head -10)
  printf '      provisioned devices:\n'; printf '%s\n' "$DEVS" | sed '/^$/d;s/^/        /'
else
  warn "no cached profile for $TEAM.$BUNDLE. The first archive (Xcode UI, Product -> Archive) will generate one via '-allowProvisioningUpdates'. Re-run this preflight after that archive succeeds."
fi

# ---------- 7. Podfile pod-target signing disabled ----------
section "7. Podfile pod-target signing"
PF="$APP/ios/Podfile"
if [[ -f "$PF" ]] && grep -q "CODE_SIGNING_ALLOWED" "$PF"; then
  ok "Podfile disables pod-target signing (CODE_SIGNING_ALLOWED present)"
else
  no "Podfile post_install does not disable pod-target signing. Pod targets inherit the legacy 'iOS Development' identity which no modern cert matches, so archive fails with 'No signing certificate \"iOS Development\" found' on every RN pod. Add this to post_install in $PF:

      installer.pods_project.targets.each do |t|
        t.build_configurations.each do |c|
          c.build_settings['CODE_SIGNING_ALLOWED'] = 'NO'
          c.build_settings['CODE_SIGNING_REQUIRED'] = 'NO'
          c.build_settings['CODE_SIGN_IDENTITY'] = ''
        end
      end
  then '(cd ios && pod install)'. The app target re-signs embedded frameworks at the [CP] Embed Pods Frameworks step, so pods never need their own signing."
fi

# ---------- 8. Fable native bundle freshness ----------
section "8. Fable native bundle freshness"
BOOT="$APP/.build/native/commonjs/Bootstrap.js"
if [[ ! -f "$BOOT" ]]; then
  no ".build/native/commonjs/Bootstrap.js missing -- run '(cd $APP && ../../eggshell build-native)' before archiving. The Release 'Bundle React Native code and images' phase bundles from .build/native; a stale/missing bundle ships the wrong JS."
else
  BOOT_MT=$(stat -f '%m' "$BOOT")
  NEWEST=$(find "$APP/src" -name '*.fs' -exec stat -f '%m %N' {} \; 2>/dev/null | sort -rn | head -1)
  NEWEST_MT=$(printf '%s' "$NEWEST" | awk '{print $1}')
  NEWEST_NAME=$(printf '%s' "$NEWEST" | cut -d' ' -f2-)
  if [[ "$BOOT_MT" -ge "$NEWEST_MT" ]]; then
    ok "bundle fresh: Bootstrap.js ($BOOT_MT) >= newest src ($NEWEST_MT, $NEWEST_NAME)"
  else
    no "bundle stale: Bootstrap.js ($BOOT_MT) < newest src ($NEWEST_MT, $NEWEST_NAME). Run '(cd $APP && ../../eggshell build-native)'."
  fi
fi

# ---------- 9. ADVISORY: dev-config gating in index.js ----------
section "9. config gating in index.js (advisory)"
IDX="$APP/index.js"
if [[ -f "$IDX" ]] && grep -q '__DEV__' "$IDX" && grep -q 'configSourceOverrides.native.prod.js' "$IDX"; then
  ok "index.js gates prod config behind __DEV__"
else
  warn "index.js does not gate configSourceOverrides.native.js on __DEV__. A Release build (where __DEV__ is false) will still load the dev override, shipping dev config (e.g. BackendUrl=http://localhost:5000). Mirror the gallery pattern:

      if (__DEV__) {
        require(\"./configSourceOverrides.native.js\");
      } else {
        require(\"./configSourceOverrides.native.prod.js\");
      }
  (Advisory -- does not block the archive, but the shipped app will use dev config.)"
fi

# ---------- 10. ADVISORY: display name ----------
section "10. display name (advisory)"
PLIST=$(ls -d "$APP"/ios/*/Info.plist 2>/dev/null | head -1)
if [[ -n "$PLIST" && -f "$PLIST" ]]; then
  DISP=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleDisplayName' "$PLIST" 2>/dev/null || echo '?')
  NAME=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleName' "$PLIST" 2>/dev/null || echo '?')
  warn "home-screen label = '$DISP' (CFBundleDisplayName), bundle name = '$NAME'. These are cosmetic and are NOT a reference to another app; edit $PLIST to change them."
fi

# ---------- summary ----------
section "summary"
printf 'PASS=%d  WARN=%d  FAIL=%d\n' "$PASS" "$WARN" "$FAIL"
if [[ "$FAIL" -gt 0 ]]; then
  printf '\nRESULT: FAIL -- fix every FAIL above before archiving.\n'
  exit 1
fi
printf '\nRESULT: PASS -- ready to archive. Open %s in Xcode, scheme %s, destination "Any iOS Device (arm64)", Product -> Archive.\n' "$WS" "$SCHEME"
exit 0
