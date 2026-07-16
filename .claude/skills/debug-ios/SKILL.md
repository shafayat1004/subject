---
name: debug-ios
description: Set up and drive the iOS simulator loop for this repo, including boot, install, launch, screenshots, log reading, and deep links. Use for any iOS on-device/simulator task, debugging a native defect, or observing UI state. Read runbooks/ios.md before improvising.
user-invocable: true
argument-hint: "[preflight|screenshot|log|launch <bundleid>]"
---

# debug-ios

## Preflight (always first)

`scripts/ios-preflight.sh` checks: a simulator is Booted (boots "iPhone 17 Pro Max" if not), Simulator app
open, Metro reachable on :8081.

## Install + launch

- First install (from the app dir): `npx react-native run-ios --simulator "iPhone 17 Pro Max" --no-packager`
- Subsequent runs: `scripts/ios-observe.sh terminate <bundleid> && scripts/ios-observe.sh launch <bundleid>`
- Bundle ids: AppTodo `com.eggshell.apptodo`; gallery: read it from
  `AppEggShellGallery/ios/AppEggshellGallery/Info.plist` (CFBundleIdentifier, a build variable;
  resolve via `xcrun simctl listapps booted | grep -i eggshell` when the app is installed).
- Metro (from the app dir): `npx react-native start --port 8081`
- Fable watch (from the app dir): `eggshell dev-native` (path-relative eggshell).

## Observe

`scripts/ios-observe.sh screenshot [out.png]`, `... log [minutes]`,
`... launch <bundleid>`, `... terminate <bundleid>`, `... openurl <url>` (deep links).

JS red-box / uncaught errors: `xcrun simctl spawn booted log show --last 3m --predicate
'eventMessage CONTAINS[c] "javascript"' | grep -v SecTrust`.

## Interact (tap / type / read hierarchy)

`simctl` and `ios-observe.sh` **cannot tap, swipe, type, or dump the view hierarchy** — no
`adb input`/`uiautomator dump` equivalent exists in the built-in iOS CLI. Raw tier can only
observe. To drive the UI (dismiss a LogBox, navigate its "Log 1 of 2" pager, tap a button):

- **Preferred:** Tier 2 Appium/xcuitest, already installed (WDA already built). From the app dir:
  `npm run observe -- doctor`, then `npm run observe -- snapshot -p ios` (screenshot + hierarchy +
  logs; captures full LogBox text) or `... add-todo "..." -p ios`.
- **Raw-CLI alternatives (not installed):** `idb` (`brew install idb-companion` + `pip3 install
  fb-idb`; `idb ui tap/swipe/text/describe-all`) or `axe` (`brew tap cameroncooke/axe && brew
  install axe`; `axe tap/type/describe-ui`, simulator-only).

See runbooks/ios.md#interact.

## Interaction debugging (when a tap "does nothing")

Hard-won technique for "control does nothing on tap" on native:

- **Presence in the a11y tree != receives touch.** Appium finding the element (count=1, real box) does
  NOT mean `onPress` fires — the accessibility tree and the touch-responder tree are separate. Prove the
  press actually fires; don't infer it from the element existing.
- **Bisect input-delivery vs handler-logic with a programmatic trigger.** Temporarily fire the handler's
  code path directly (e.g. a mount `useEffect` calling the same action), bypassing the gesture. If it
  now works → the bug is touch delivery (hit-testing, `opacity < 0.01`, overlapping view, gesture
  responder). If it still fails → the handler/downstream is broken. This one move usually halves the
  search space instantly.
- **Prefer coordinate taps over element `.click()` for RN.** WDA `el.click()` is unreliable on small or
  near-transparent overlays; a W3C `performActions` pointer tap at the element-box center is closer to a
  real finger and more reliable.
- **Byte-identical page-source across a burst = nothing fired.** After a tap, sample source/screenshots
  every ~150ms for ~2s. Unchanged source across the burst is definitive "the press did nothing"; it also
  catches open-then-instantly-dismiss flashes a single late screenshot would miss.
- **Post-Fabric, "it worked before" is not evidence.** Re-verify interaction primitives on iOS after any
  Paper->Fabric change; if the app was launch-crashing, iOS interaction was never exercised at all.

## Gotchas (from the runbook and engineering log)

- Always open the `.xcworkspace`, never the `.xcodeproj` (pods).
- Pod trouble: verify `platform :ios, '15.5'` in the Podfile; npm peer conflicts may need
  `--legacy-peer-deps`.
- JS `console.log` is **unreliable on RN 0.86 + Hermes**: it is routed to react-native-devtools, not
  the Metro terminal and not `simctl log show` (you may see the odd stray hit, do not depend on it).
  For a reliable runtime signal, diff Appium page-source / screenshots, or open react-native-devtools.
  F# `Log.Info` does not surface either.
- **`react-native run-ios` always fails with "Something went wrong while installing CocoaPods" on
  this repo**, even after a manual `pod install` succeeded — none of our apps has a root `Gemfile`,
  and the RN 0.86 CLI (`@react-native-community/cli-config-apple`) hard-requires one before it will
  drive CocoaPods, throwing before it ever gets to the actual `pod install` step. Workaround: after
  `pod install`, build and launch directly, bypassing the CLI's pod-install path entirely:
  ```bash
  xcodebuild -workspace ios/<Workspace>.xcworkspace -scheme <Scheme> -configuration Debug \
    -destination "platform=iOS Simulator,id=<SIMUDID>" -derivedDataPath ios/build_derived
  xcrun simctl install <SIMUDID> ios/build_derived/Build/Products/Debug-iphonesimulator/<App>.app
  xcrun simctl launch <SIMUDID> <bundleid>
  ```
  Metro (and `eggshell dev-native` for the Fable watch) must already be running for the JS bundle to load.

## Patch not showing?

Invoke the fable-rebuild-verify skill first, then relaunch the app.

## Doc refs

- runbooks/ios.md (boot, install, screenshot, logs, gotchas)
- runbooks/dev-loop.md
- runbooks/audit-toolkit.md

(All under AppEggShellGallery/public-dev/docs/.)
