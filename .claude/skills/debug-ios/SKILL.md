---
name: debug-ios
description: Set up and drive the iOS simulator loop for this repo, including boot, install, launch, screenshots, log reading, and deep links. Use for any iOS on-device/simulator task, debugging a native defect, or observing UI state. Read runbooks/ios.md before improvising.
user-invocable: true
argument-hint: "[preflight|screenshot|log|launch <bundleid>]"
---

# debug-ios

## Preflight (always first)

`scripts/ios-preflight.sh` checks: a simulator is Booted (boots "iPhone 16" if not), Simulator app
open, Metro reachable on :8081.

## Install + launch

- First install (from the app dir): `npx react-native run-ios --simulator "iPhone 16" --no-packager`
- Subsequent runs: `scripts/ios-observe.sh terminate <bundleid> && scripts/ios-observe.sh launch <bundleid>`
- Bundle ids: AppTodo `com.eggshell.apptodo`; gallery: read it from
  `AppEggShellGallery/ios/AppEggshellGallery/Info.plist` (CFBundleIdentifier, a build variable;
  resolve via `xcrun simctl listapps booted | grep -i eggshell` when the app is installed).
- Metro (from the app dir): `npx react-native start --port 8081`
- Fable watch (from the app dir): `eggshell dev-native` (path-relative eggshell).

## Observe

`scripts/ios-observe.sh screenshot [out.png]`, `... log [minutes]`,
`... launch <bundleid>`, `... terminate <bundleid>`, `... openurl <url>` (deep links).

## Gotchas (from the runbook and engineering log)

- Always open the `.xcworkspace`, never the `.xcodeproj` (pods).
- Pod trouble: verify `platform :ios, '15.5'` in the Podfile; npm peer conflicts may need
  `--legacy-peer-deps`.
- JS `console.log` shows in the Metro terminal and in `log show` output; F# `Log.Info` does not.

## Patch not showing?

Invoke the fable-rebuild-verify skill first, then relaunch the app.

## Doc refs

- runbooks/ios.md (boot, install, screenshot, logs, gotchas)
- runbooks/dev-loop.md
- runbooks/audit-toolkit.md

(All under AppEggShellGallery/public-dev/docs/.)
